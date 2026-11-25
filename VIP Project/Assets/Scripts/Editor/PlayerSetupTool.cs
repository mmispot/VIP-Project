#if UNITY_EDITOR
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace VIPProject.EditorTools
{
    /// <summary>
    /// Tools > VIP > Build Player + Test Level builds the player and a movement test
    /// level in the currently open scene. Safe to re-run: it replaces its own objects.
    /// </summary>
    public static class PlayerSetupTool
    {
        private const string PlayerName = "Player";
        private const string LevelName = "MovementTestLevel";

        [MenuItem("Tools/VIP/Build Player + Test Level")]
        public static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            RemoveExisting(PlayerName);
            RemoveExisting(LevelName);

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (actions == null)
            {
                Debug.LogError("Assets/InputSystem_Actions.inputactions not found.");
                return;
            }

            BuildLevel();
            BuildPlayer(actions, new Vector3(0f, 0.1f, -4f));

            // The default scene camera would fight with the player's camera.
            var oldCam = GameObject.Find("Main Camera");
            if (oldCam != null && oldCam.transform.parent == null)
                Undo.DestroyObjectImmediate(oldCam);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("VIP: Player + test level built. Save the scene, then press Play.");
        }

        /// <summary>
        /// Adds only the player (movement, look, head bob, sprint FOV) to the open scene.
        /// Spawns at the selected object's position, or the world origin if nothing is selected.
        /// Never touches existing level geometry, and never replaces an existing Player.
        /// </summary>
        [MenuItem("Tools/VIP/Add Player (Movement Only)")]
        public static void AddPlayerOnly()
        {
            Scene scene = SceneManager.GetActiveScene();
            var existing = GameObject.Find(PlayerName);
            if (existing != null)
            {
                Selection.activeGameObject = existing;
                Debug.LogWarning("VIP: This scene already has a 'Player'. Delete or rename it first, or use Build Player + Test Level to rebuild.");
                return;
            }

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (actions == null)
            {
                Debug.LogError("Assets/InputSystem_Actions.inputactions not found.");
                return;
            }

            Vector3 spawn = Selection.activeTransform != null ? Selection.activeTransform.position : Vector3.zero;
            GameObject player = BuildPlayer(actions, spawn);

            // A second camera/AudioListener would fight with the player's.
            var oldCam = GameObject.Find("Main Camera");
            if (oldCam != null && oldCam.transform.parent == null)
                Undo.DestroyObjectImmediate(oldCam);

            Selection.activeGameObject = player;
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("VIP: Player added. Make sure the scene has a floor with a collider, save, then press Play.");
        }

        /// <summary>Player spawn for the cutscene tool: 8 m in front of the door, dropped onto any floor below.</summary>
        private static Vector3 SpawnInFrontOf(Transform door)
        {
            Vector3 spawn = door.position - door.forward * 8f;
            spawn.y = door.position.y;
            if (Physics.Raycast(spawn + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 50f))
                spawn.y = hit.point.y + 0.1f;
            else
                spawn.y = 0.1f;
            return spawn;
        }

        /// <summary>
        /// Sets up the door cutscene in the open scene. Select the door object first; if nothing is
        /// selected a placeholder door is created. If the scene has no Player, one is built 8 m in front of the door.
        /// The cutscene camera is placed 5 m in front of the door, looking at it: move it in the
        /// Scene view to frame the shot you want.
        /// </summary>
        [MenuItem("Tools/VIP/Setup Door Cutscene")]
        public static void SetupDoorCutsceneMenu()
        {
            if (Object.FindFirstObjectByType<Cutscenes.DoorCutscene>() != null)
            {
                Debug.LogWarning("VIP: This scene already has a DoorCutscene. Use Tools > VIP > Door Cutscene Wizard to add another door.");
                return;
            }
            SetupDoorCutscene(Selection.activeTransform);
        }

        /// <summary>
        /// Creates a DoorCutscene + its cutscene camera for the given door and wires the scene's
        /// Brain and player components. With buildPlayerIfMissing = false (existing projects) it never
        /// creates a player, and adds a CinemachineBrain to Camera.main if the scene has none.
        /// </summary>
        internal static Cutscenes.DoorCutscene SetupDoorCutscene(Transform door, bool buildPlayerIfMissing = true, string rootName = "DoorCutscene")
        {
            Scene scene = SceneManager.GetActiveScene();

            // Resolve the door first: adding the player changes the selection.
            if (door == null)
            {
                var placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                placeholder.name = "Door";
                placeholder.transform.position = new Vector3(0f, 1.4f, 8f);
                placeholder.transform.localScale = new Vector3(2f, 2.8f, 0.3f);
                Undo.RegisterCreatedObjectUndo(placeholder, "Create Door");
                door = placeholder.transform;
            }

            var movement = Object.FindFirstObjectByType<Player.PlayerMovement>();
            if (movement == null && buildPlayerIfMissing)
            {
                // No player yet: build one in front of the door.
                var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
                if (actions == null)
                {
                    Debug.LogError("Assets/InputSystem_Actions.inputactions not found.");
                    return null;
                }
                BuildPlayer(actions, SpawnInFrontOf(door));
                var oldCam = GameObject.Find("Main Camera");
                if (oldCam != null && oldCam.transform.parent == null)
                    Undo.DestroyObjectImmediate(oldCam);
                movement = Object.FindFirstObjectByType<Player.PlayerMovement>();
            }

            var brain = Object.FindFirstObjectByType<CinemachineBrain>();
            var look = Object.FindFirstObjectByType<Player.PlayerLook>();
            if (brain == null && Camera.main != null)
            {
                brain = Undo.AddComponent<CinemachineBrain>(Camera.main.gameObject);
                brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1.5f);
                Debug.LogWarning("VIP: No CinemachineBrain in the scene, so one was added to '" + Camera.main.name +
                                 "'. The Brain drives that camera from a CinemachineCamera: make sure your player's view has one " +
                                 "(priority above 0, below the cutscene's 20), or the view will jump when the cutscene ends.");
            }
            if (brain == null)
            {
                Debug.LogError("VIP: No CinemachineBrain and no camera tagged MainCamera in the scene. Add a Brain to your camera and run this tool again.");
                return null;
            }

            var camGo = new GameObject(rootName + "Cam");
            Undo.RegisterCreatedObjectUndo(camGo, "Create Door Cutscene");
            Transform viewer = movement != null ? movement.transform : (Camera.main != null ? Camera.main.transform : null);
            Vector3 toPlayer = viewer != null ? viewer.position - door.position : -door.forward;
            toPlayer.y = 0f;
            Vector3 side = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : -door.forward;
            camGo.transform.position = door.position + side * 5f + Vector3.up * 0.4f;
            camGo.transform.rotation = Quaternion.LookRotation(door.position - camGo.transform.position, Vector3.up);
            var vcam = camGo.AddComponent<CinemachineCamera>();
            vcam.Priority = 0;
            LensSettings lens = vcam.Lens;
            lens.FieldOfView = 55f;
            vcam.Lens = lens;

            var root = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Door Cutscene");
            var cutscene = root.AddComponent<Cutscenes.DoorCutscene>();
            SetRef(cutscene, "brain", brain);
            SetRef(cutscene, "cutsceneCamera", vcam);
            SetRef(cutscene, "door", door);
            SetRef(cutscene, "playerMovement", movement);
            SetRef(cutscene, "playerLook", look);
            SetRef(cutscene, "playerInteractor", Object.FindFirstObjectByType<Player.PlayerInteractor>());

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"VIP: Door cutscene set up. Adjust '{camGo.name}' in the Scene view, press Play, then F9 to test.");
            return cutscene;
        }

        /// <summary>
        /// Builds the whole door-cutscene demo in the open scene (use CutScene-Dooropening):
        /// a room with a door, a pedestal with a pickup (ItemData asset), the player, the cutscene
        /// camera and an on-screen interaction prompt. The pickup's OnPickedUp event calls
        /// DoorCutscene.Play. Safe to re-run: it replaces its own objects.
        /// </summary>
        [MenuItem("Tools/VIP/Build Door Cutscene Scene")]
        public static void BuildDoorCutsceneScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            foreach (string n in new[] { PlayerName, "DoorRoom", "DoorCutscene", "DoorCutsceneCam", "KeyPickup", "InteractionPrompt" })
                RemoveExisting(n);

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (actions == null)
            {
                Debug.LogError("Assets/InputSystem_Actions.inputactions not found.");
                return;
            }

            Material wallMat = GetOrCreateMaterial("VIP_Wall", new Color(0.62f, 0.62f, 0.66f));
            Material floorMat = GetOrCreateMaterial("VIP_Floor", new Color(0.28f, 0.3f, 0.34f));
            Material doorMat = GetOrCreateMaterial("VIP_Door", new Color(0.45f, 0.27f, 0.12f));
            Material itemMat = GetOrCreateMaterial("VIP_Pickup", new Color(1f, 0.82f, 0.1f));

            // ---- Room: 20 wide, z -10..30, divided at z = 10 by a wall with a 2.4 m doorway ----
            var room = new GameObject("DoorRoom");
            Undo.RegisterCreatedObjectUndo(room, "Create Door Room");
            Transform t = room.transform;
            Box(t, "Floor", new Vector3(0f, -0.5f, 10f), new Vector3(20f, 1f, 40f), floorMat);
            Box(t, "WallFront", new Vector3(0f, 2f, -10f), new Vector3(20f, 4f, 0.4f), wallMat);
            Box(t, "WallBack", new Vector3(0f, 2f, 30f), new Vector3(20f, 4f, 0.4f), wallMat);
            Box(t, "WallLeft", new Vector3(-10f, 2f, 10f), new Vector3(0.4f, 4f, 40f), wallMat);
            Box(t, "WallRight", new Vector3(10f, 2f, 10f), new Vector3(0.4f, 4f, 40f), wallMat);
            Box(t, "DividerLeft", new Vector3(-5.6f, 2f, 10f), new Vector3(8.8f, 4f, 0.4f), wallMat);
            Box(t, "DividerRight", new Vector3(5.6f, 2f, 10f), new Vector3(8.8f, 4f, 0.4f), wallMat);
            Box(t, "DoorLintel", new Vector3(0f, 3.5f, 10f), new Vector3(2.4f, 1f, 0.4f), wallMat);

            // Door: slides up into the lintel. Must not be static (it moves).
            GameObject doorGo = Box(t, "Door", new Vector3(0f, 1.5f, 10f), new Vector3(2.4f, 3f, 0.3f), doorMat);
            GameObjectUtility.SetStaticEditorFlags(doorGo, 0);

            // Something to look at in the back room.
            Box(t, "Treasure", new Vector3(0f, 0.4f, 20f), new Vector3(0.8f, 0.8f, 0.8f), itemMat);

            // Pedestal for the pickup, off to the left so it never blocks the door shot.
            Box(t, "Pedestal", new Vector3(-3f, 0.5f, 6f), new Vector3(0.5f, 1f, 0.5f), wallMat);

            if (GameObject.Find("Directional Light") == null)
            {
                var l = new GameObject("Directional Light");
                l.AddComponent<Light>().type = LightType.Directional;
                l.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            Physics.SyncTransforms();

            // ---- Player + cutscene camera + DoorCutscene (builds the player in front of the door) ----
            Cutscenes.DoorCutscene cutscene = SetupDoorCutscene(doorGo.transform);
            if (cutscene == null) return;

            // ---- Pickup with a ScriptableObject ItemData ----
            Items.ItemData itemAsset = GetOrCreateItemData();
            var pickupGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickupGo.name = "KeyPickup";
            Undo.RegisterCreatedObjectUndo(pickupGo, "Create Pickup");
            pickupGo.transform.position = new Vector3(-3f, 1.25f, 6f);
            pickupGo.transform.localScale = Vector3.one * 0.3f;
            pickupGo.GetComponent<Renderer>().sharedMaterial = itemMat;
            pickupGo.GetComponent<SphereCollider>().radius = 1.2f; // generous aim target
            var pickup = pickupGo.AddComponent<Items.ItemPickup>();
            SetRef(pickup, "payload", itemAsset);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(pickup.OnPickedUp, cutscene.Play);
            EditorUtility.SetDirty(pickup);

            // ---- Interaction prompt + crosshair ----
            BuildPromptUI(Object.FindFirstObjectByType<Player.PlayerInteractor>());

            Selection.activeGameObject = pickupGo;
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("VIP: Door cutscene scene built. Save the scene, press Play, walk to the yellow ball, look at it and press E.");
        }

        private static Items.ItemData GetOrCreateItemData()
        {
            const string path = "Assets/Data/Items/Key.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Items.ItemData>(path);
            if (existing != null) return existing;

            EnsureFolder("Assets/Data");
            EnsureFolder("Assets/Data/Items");
            var item = ScriptableObject.CreateInstance<Items.ItemData>();
            var so = new SerializedObject(item);
            so.FindProperty("itemId").stringValue = "key_door_01";
            so.FindProperty("displayName").stringValue = "Brass Key";
            so.FindProperty("description").stringValue = "Opens the door in the first room.";
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(item, path);
            AssetDatabase.SaveAssets();
            return item;
        }

        internal static void BuildPromptUI(Player.PlayerInteractor interactor)
        {
            var canvasGo = new GameObject("InteractionPrompt", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Interaction Prompt");
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            labelGo.transform.SetParent(canvasGo.transform, false);
            var rect = (RectTransform)labelGo.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -90f);
            rect.sizeDelta = new Vector2(800f, 60f);
            var label = labelGo.GetComponent<UnityEngine.UI.Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 36;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            labelGo.AddComponent<UnityEngine.UI.Shadow>();

            var dotGo = new GameObject("Crosshair", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            dotGo.transform.SetParent(canvasGo.transform, false);
            var dotRect = (RectTransform)dotGo.transform;
            dotRect.anchorMin = dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.anchoredPosition = Vector2.zero;
            dotRect.sizeDelta = new Vector2(6f, 6f);
            var dot = dotGo.GetComponent<UnityEngine.UI.Image>();
            dot.color = new Color(1f, 1f, 1f, 0.8f);
            dot.raycastTarget = false;

            var ui = canvasGo.AddComponent<UI.InteractionPromptUI>();
            SetRef(ui, "interactor", interactor);
            SetRef(ui, "label", label);
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"Assets/Materials/VIP/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            EnsureFolder("Assets/Materials");
            EnsureFolder("Assets/Materials/VIP");
            Shader shader = Shader.Find("HDRP/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader) { name = name };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        private static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject go = Box(parent, name, pos, size);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static void RemoveExisting(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) Undo.DestroyObjectImmediate(existing);
        }

        private static GameObject BuildPlayer(InputActionAsset actions, Vector3 position)
        {
            var player = new GameObject(PlayerName);
            Undo.RegisterCreatedObjectUndo(player, "Create Player");
            player.transform.position = position;

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 45f;
            cc.skinWidth = 0.08f;

            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(player.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.65f, 0f);

            var camGo = new GameObject("Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(pivot.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();

            // The Brain drives the Camera from whichever CinemachineCamera has top priority.
            // Default blend = how the cutscene cuts to the door and back.
            var brain = camGo.AddComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1.5f);

            // Player vcam sits at the pivot, so it follows mouse look. HeadBob + SprintFov live on it
            // (the Brain would overwrite the real Camera's position and FOV every frame).
            var vcamGo = new GameObject("PlayerVCam");
            vcamGo.transform.SetParent(pivot.transform, false);
            var vcam = vcamGo.AddComponent<CinemachineCamera>();
            vcam.Priority = 10;
            LensSettings lens = vcam.Lens;
            lens.FieldOfView = 60f;
            lens.NearClipPlane = 0.05f;
            vcam.Lens = lens;

            var movement = player.AddComponent<Player.PlayerMovement>();
            var look = player.AddComponent<Player.PlayerLook>();
            var bob = vcamGo.AddComponent<Player.HeadBob>();
            var fov = vcamGo.AddComponent<Player.SprintFov>();
            var interactor = player.AddComponent<Player.PlayerInteractor>();

            SetRef(interactor, "inputActions", actions);
            SetRef(interactor, "cameraPivot", pivot.transform);
            SetRef(movement, "inputActions", actions);
            SetRef(movement, "cameraPivot", pivot.transform);
            SetRef(look, "inputActions", actions);
            SetRef(look, "cameraPivot", pivot.transform);
            SetRef(bob, "movement", movement);
            SetRef(fov, "movement", movement);
            return player;
        }

        internal static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildLevel()
        {
            var root = new GameObject(LevelName);
            Undo.RegisterCreatedObjectUndo(root, "Create Level");
            Transform t = root.transform;

            // Floor 30 x 30 (top surface at y = 0).
            Box(t, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(30f, 1f, 30f));

            // Stairs: 8 steps of 0.18 m rise, 0.35 m run, 2 m wide, on the right (+X).
            for (int i = 0; i < 8; i++)
            {
                float h = 0.18f * (i + 1);
                Box(t, $"Step{i}", new Vector3(6f, h * 0.5f, 2f + 0.35f * i + 0.175f), new Vector3(2f, h, 0.35f));
            }
            Box(t, "StairTop", new Vector3(6f, 0.72f, 2f + 0.35f * 8 + 1.5f), new Vector3(2f, 1.44f, 3f));

            // 25 degree ramp on the left (-X).
            var ramp = Box(t, "Ramp25", new Vector3(-6f, 0.9f, 4f), new Vector3(2.5f, 0.2f, 4f));
            ramp.transform.rotation = Quaternion.Euler(-25f, 0f, 0f);

            // Wall with a 0.9 m wide doorway at the back (+Z).
            Box(t, "WallLeft", new Vector3(-3.7f, 1.25f, 12f), new Vector3(8.6f, 2.5f, 0.3f));
            Box(t, "WallRight", new Vector3(3.7f, 1.25f, 12f), new Vector3(8.6f, 2.5f, 0.3f));
            Box(t, "DoorLintel", new Vector3(0f, 2.25f, 12f), new Vector3(0.9f, 0.5f, 0.3f));

            // Low beam: head-bump test (underside at 2.0 m, jump apex head = ~2.45 m).
            Box(t, "LowBeam", new Vector3(0f, 2.15f, -0.5f), new Vector3(2f, 0.3f, 1.5f));

            // Light so it's visible even without HDRP sky tweaks.
            var light = GameObject.Find("Directional Light");
            if (light == null)
            {
                var l = new GameObject("Directional Light");
                l.AddComponent<Light>().type = LightType.Directional;
                l.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }

        private static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
            return go;
        }
    }
}
#endif

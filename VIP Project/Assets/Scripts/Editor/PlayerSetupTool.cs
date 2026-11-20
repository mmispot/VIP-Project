#if UNITY_EDITOR
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
            BuildPlayer(actions);

            // The default scene camera would fight with the player's camera.
            var oldCam = GameObject.Find("Main Camera");
            if (oldCam != null && oldCam.transform.parent == null)
                Undo.DestroyObjectImmediate(oldCam);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("VIP: Player + test level built. Save the scene, then press Play.");
        }

        private static void RemoveExisting(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) Undo.DestroyObjectImmediate(existing);
        }

        private static void BuildPlayer(InputActionAsset actions)
        {
            var player = new GameObject(PlayerName);
            Undo.RegisterCreatedObjectUndo(player, "Create Player");
            player.transform.position = new Vector3(0f, 0.1f, -4f);

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

            var movement = player.AddComponent<Player.PlayerMovement>();
            var look = player.AddComponent<Player.PlayerLook>();
            var bob = camGo.AddComponent<Player.HeadBob>();

            SetRef(movement, "inputActions", actions);
            SetRef(look, "inputActions", actions);
            SetRef(look, "cameraPivot", pivot.transform);
            SetRef(bob, "movement", movement);
        }

        private static void SetRef(Object target, string field, Object value)
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

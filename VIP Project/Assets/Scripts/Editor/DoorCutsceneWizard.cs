#if UNITY_EDITOR
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VIPProject.EditorTools
{
    /// <summary>
    /// Tools > VIP > Door Cutscene Wizard: drop the door-opening cutscene into any scene.
    /// Pick a door, pick any ScriptableObject (and optionally an existing scene object) to act as the
    /// pickup, press Integrate. Picking it up plays the Zelda-style camera cutscene and opens the door.
    /// Never creates a player: it uses the scene's existing one.
    /// </summary>
    public class DoorCutsceneWizard : EditorWindow
    {
        private enum DoorMotion { SlideUp, SlideDown, SwingOpen90 }

        private Transform _door;
        private ScriptableObject _payload;
        private GameObject _pickupObject;
        private DoorMotion _motion = DoorMotion.SlideUp;
        private bool _addPromptUi = true;

        [MenuItem("Tools/VIP/Door Cutscene Wizard")]
        private static void Open()
        {
            var window = GetWindow<DoorCutsceneWizard>("Door Cutscene");
            window.minSize = new Vector2(360f, 300f);
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Picking up the pickup plays a cutscene: the camera blends to the door, the door opens, " +
                "then the camera blends back. Works in the currently open scene.", MessageType.Info);

            EditorGUILayout.Space();
            _door = (Transform)EditorGUILayout.ObjectField(
                new GUIContent("Door", "The scene object that opens. Its pivot is the hinge for Swing."),
                _door, typeof(Transform), true);
            _motion = (DoorMotion)EditorGUILayout.EnumPopup(
                new GUIContent("Open motion", "SlideUp / SlideDown move by the door's height. Swing rotates 90° around Y."), _motion);

            EditorGUILayout.Space();
            _payload = (ScriptableObject)EditorGUILayout.ObjectField(
                new GUIContent("Pickup ScriptableObject", "Any ScriptableObject: ItemData or your own type. Optional."),
                _payload, typeof(ScriptableObject), false);
            _pickupObject = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Pickup object (optional)", "Existing scene object to make the pickup. Leave empty to create a new one."),
                _pickupObject, typeof(GameObject), true);
            _addPromptUi = EditorGUILayout.ToggleLeft("Add '[E] Pick up' prompt UI if the scene has none", _addPromptUi);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(_door == null))
            {
                if (GUILayout.Button("Integrate", GUILayout.Height(32f)))
                    Integrate();
            }
            if (_door == null)
                EditorGUILayout.HelpBox("Assign the door first.", MessageType.None);
        }

        private void Integrate()
        {
            // 1. Cutscene (camera + DoorCutscene). Never builds a player here.
            Cutscenes.DoorCutscene cutscene = PlayerSetupTool.SetupDoorCutscene(_door, false, $"DoorCutscene_{_door.name}");
            if (cutscene == null) return;

            // 2. How the door opens.
            float height = DoorHeight(_door);
            var so = new SerializedObject(cutscene);
            Vector3 offset = Vector3.zero;
            Vector3 euler = Vector3.zero;
            switch (_motion)
            {
                case DoorMotion.SlideUp: offset = new Vector3(0f, height, 0f); break;
                case DoorMotion.SlideDown: offset = new Vector3(0f, -height, 0f); break;
                case DoorMotion.SwingOpen90: euler = new Vector3(0f, 90f, 0f); break;
            }
            so.FindProperty("openOffset").vector3Value = offset;
            so.FindProperty("openEuler").vector3Value = euler;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 3. The pickup.
            GameObject pickupGo = _pickupObject;
            if (pickupGo == null)
            {
                pickupGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pickupGo.name = (_payload != null ? _payload.name : "Door") + "Pickup";
                Undo.RegisterCreatedObjectUndo(pickupGo, "Create Pickup");
                pickupGo.transform.position = SpawnPoint();
                pickupGo.transform.localScale = Vector3.one * 0.3f;
                pickupGo.GetComponent<SphereCollider>().radius = 1.2f; // generous aim target
            }
            else if (!pickupGo.TryGetComponent<Collider>(out _))
            {
                Undo.AddComponent<BoxCollider>(pickupGo);
            }

            if (pickupGo.TryGetComponent(out Collider col) && col.isTrigger)
                Debug.LogWarning($"VIP: '{pickupGo.name}' has a trigger collider. PlayerInteractor ignores triggers, so untick Is Trigger.", pickupGo);

            if (!pickupGo.TryGetComponent(out Items.ItemPickup pickup))
                pickup = Undo.AddComponent<Items.ItemPickup>(pickupGo);
            PlayerSetupTool.SetRef(pickup, "payload", _payload);
            UnityEventTools.AddPersistentListener(pickup.OnPickedUp, cutscene.Play);
            EditorUtility.SetDirty(pickup);

            // 4. Interaction: needs a PlayerInteractor in the scene to press E on the pickup.
            var interactor = Object.FindFirstObjectByType<Player.PlayerInteractor>();
            if (interactor == null)
            {
                Debug.LogWarning("VIP: This scene has no PlayerInteractor, so nothing can pick it up yet. Add PlayerInteractor to your player " +
                                 "(assign Input Actions + Camera Pivot), or call ItemPickup.Interact(gameObject) from your own interaction code.");
            }
            else if (_addPromptUi && Object.FindFirstObjectByType<UI.InteractionPromptUI>() == null)
            {
                PlayerSetupTool.BuildPromptUI(interactor);
            }

            EditorSceneManager.MarkSceneDirty(pickupGo.scene);
            Selection.activeGameObject = pickupGo;
            EditorGUIUtility.PingObject(pickupGo);
            Debug.Log($"VIP: '{pickupGo.name}' now opens '{_door.name}' via the cutscene. Save the scene and press Play (F9 also triggers it).");
        }

        private static float DoorHeight(Transform door)
        {
            var renderer = door.GetComponentInChildren<Renderer>();
            return renderer != null ? renderer.bounds.size.y : 3f;
        }

        private static Vector3 SpawnPoint()
        {
            if (Camera.main != null)
                return Camera.main.transform.position + Camera.main.transform.forward * 2f;
            var view = SceneView.lastActiveSceneView;
            return view != null ? view.pivot : Vector3.zero;
        }
    }
}
#endif

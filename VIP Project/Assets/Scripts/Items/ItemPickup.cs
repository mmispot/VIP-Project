using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using VIPProject.Core;

namespace VIPProject.Items
{
    /// <summary>
    /// A world item the player can pick up. Holds any ScriptableObject as its payload; picking it up raises
    /// PickedUp (for code, e.g. a future inventory) and OnPickedUp (for Inspector wiring,
    /// e.g. DoorCutscene.Play), then hides the object.
    /// Needs a non-trigger Collider on a layer the PlayerInteractor's mask includes.
    /// </summary>
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        private Vector3 _basePosition;
        private string _prompt;
        private bool _collected;

        [Tooltip("Any ScriptableObject: an ItemData, or your own item/quest/key type. Optional.")]
        [FormerlySerializedAs("item")]
        [SerializeField] private ScriptableObject payload;
        [SerializeField] private string verb = "Pick up";
        [SerializeField] private UnityEvent onPickedUp;

        [Header("Idle motion")]
        [Tooltip("Degrees per second around Y. 0 = still.")]
        [SerializeField] private float spinSpeed = 60f;
        [SerializeField] private float bobHeight = 0.05f;
        [SerializeField] private float bobSpeed = 2f;

        public ScriptableObject Payload => payload;
        public string Prompt => _prompt;
        public UnityEvent OnPickedUp => onPickedUp;

        /// <summary>Raised once when collected. Subscribe from an inventory to receive the payload.</summary>
        public event Action<ScriptableObject> PickedUp;

        private void Awake()
        {
            _basePosition = transform.position;
            string label = payload is ItemData data ? data.DisplayName : (payload != null ? payload.name : null);
            _prompt = label != null ? $"{verb} {label}" : verb;
        }

        private void Update()
        {
            if (spinSpeed != 0f)
                transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            if (bobHeight > 0f)
                transform.position = _basePosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        }

        public void Interact(GameObject interactor)
        {
            if (_collected) return;
            _collected = true;

            PickedUp?.Invoke(payload);
            onPickedUp.Invoke();
            gameObject.SetActive(false);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VIPProject.Core;

namespace VIPProject.Player
{
    /// <summary>
    /// Looks along the camera pivot's forward each frame; if it hits an IInteractable within
    /// range it raises PromptChanged and, on the Interact action, calls Interact().
    /// One raycast per frame (non-allocating); walls on the mask block line of sight.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        private InputAction _interact;
        private IInteractable _target;
        private bool _locked;

        [Header("Input (assign InputSystem_Actions)")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("References")]
        [SerializeField] private Transform cameraPivot;

        [Header("Interaction")]
        [SerializeField] private float range = 2.5f;
        [Tooltip("Layers the ray can hit. Must include the interactables AND anything that should block the view.")]
        [SerializeField] private LayerMask interactMask = 1; // Default layer

        /// <summary>Prompt text of the looked-at interactable, or null when there is none.</summary>
        public event Action<string> PromptChanged;

        private void Awake()
        {
            if (inputActions == null || cameraPivot == null)
            {
                Debug.LogError("PlayerInteractor: inputActions and cameraPivot must be assigned.", this);
                enabled = false;
                return;
            }
            _interact = inputActions.FindAction("Player/Interact", true);
        }

        private void OnEnable() => _interact?.Enable();

        private void OnDisable()
        {
            _interact?.Disable();
            SetTarget(null);
        }

        private void Update()
        {
            IInteractable found = null;
            if (!_locked
                && Physics.Raycast(cameraPivot.position, cameraPivot.forward, out RaycastHit hit,
                                   range, interactMask, QueryTriggerInteraction.Ignore))
            {
                hit.collider.TryGetComponent(out found);
            }

            SetTarget(found);

            if (_target != null && _interact.WasPressedThisFrame())
                _target.Interact(gameObject);
        }

        public void SetLocked(bool locked) => _locked = locked;

        private void SetTarget(IInteractable target)
        {
            if (target == _target) return;
            _target = target;
            PromptChanged?.Invoke(target?.Prompt);
        }
    }
}

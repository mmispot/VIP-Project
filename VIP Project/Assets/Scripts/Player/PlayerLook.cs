using UnityEngine;
using UnityEngine.InputSystem;

namespace VIPProject.Player
{
    /// <summary>
    /// Mouse look. Yaw rotates the Player body, pitch rotates CameraPivot.
    /// A light smoothing pass removes mouse jitter without adding noticeable lag.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class PlayerLook : MonoBehaviour
    {
        private InputAction _look;
        private float _targetYaw;
        private float _targetPitch;
        private float _yaw;
        private float _pitch;
        private bool _locked;

        [Header("Input (assign InputSystem_Actions)")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("References")]
        [SerializeField] private Transform cameraPivot;

        [Header("Feel")]
        [Tooltip("Degrees per mouse unit.")]
        [SerializeField] private float lookSensitivity = 0.1f;
        [SerializeField] private float maxPitch = 80f;
        [Tooltip("Higher = snappier. ~25 is smooth but responsive; 0 disables smoothing.")]
        [SerializeField] private float smoothing = 25f;
        [SerializeField] private bool lockCursor = true;

        private void Awake()
        {
            if (inputActions == null || cameraPivot == null)
            {
                Debug.LogError("PlayerLook: inputActions and cameraPivot must be assigned.", this);
                enabled = false;
                return;
            }
            _look = inputActions.FindAction("Player/Look", true);
            _yaw = _targetYaw = transform.eulerAngles.y;
        }

        private void OnEnable()
        {
            _look?.Enable();
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnDisable()
        {
            _look?.Disable();
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void LateUpdate()
        {
            if (!_locked)
            {
                // Mouse delta is already per-frame: no deltaTime.
                Vector2 delta = _look.ReadValue<Vector2>() * lookSensitivity;
                _targetYaw += delta.x;
                _targetPitch = Mathf.Clamp(_targetPitch - delta.y, -maxPitch, maxPitch);
            }

            if (smoothing <= 0f)
            {
                _yaw = _targetYaw;
                _pitch = _targetPitch;
            }
            else
            {
                // Frame-rate independent exponential smoothing.
                float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
                _yaw = Mathf.Lerp(_yaw, _targetYaw, t);
                _pitch = Mathf.Lerp(_pitch, _targetPitch, t);
            }

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        public void SetLocked(bool locked) => _locked = locked;
    }
}

using Unity.Cinemachine;
using UnityEngine;

namespace VIPProject.Player
{
    /// <summary>
    /// Widens the field of view as the player speeds up toward sprint speed.
    /// Driven by actual horizontal speed (not the key press), so it follows the
    /// acceleration curve; a SmoothDamp on top removes any hard edges.
    /// Put this on the player's CinemachineCamera (a CinemachineBrain overwrites the
    /// real Camera's lens every frame). Falls back to a plain Camera if there is no vcam.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class SprintFov : MonoBehaviour
    {
        private CinemachineCamera _vcam;
        private Camera _camera;
        private float _baseFov;
        private float _currentFov;
        private float _fovVelocity;

        [SerializeField] private PlayerMovement movement;

        [Header("Live sliders (tweak in Play mode)")]
        [Tooltip("Extra degrees of FOV at full sprint speed.")]
        [Range(0f, 30f)]
        [SerializeField] private float sprintFovBoost = 14f;
        [Tooltip("Seconds to settle when entering/leaving sprint. Lower = punchier.")]
        [Range(0.05f, 0.6f)]
        [SerializeField] private float smoothTime = 0.18f;
        [Tooltip("Shapes the ramp. >1 keeps FOV near base during walking and kicks in near sprint.")]
        [Range(0.5f, 3f)]
        [SerializeField] private float curvePower = 1.5f;

        private void Awake()
        {
            if (!TryGetComponent(out _vcam) && !TryGetComponent(out _camera))
            {
                Debug.LogError("SprintFov: needs a CinemachineCamera or Camera on the same GameObject.", this);
                enabled = false;
                return;
            }
            _baseFov = _vcam != null ? _vcam.Lens.FieldOfView : _camera.fieldOfView;
            _currentFov = _baseFov;
        }

        private void OnEnable()
        {
            if (movement == null)
            {
                Debug.LogError("SprintFov: movement is not assigned.", this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            if (_vcam != null || _camera != null) ApplyFov(_baseFov);
        }

        private void LateUpdate()
        {
            // 0 at walk speed or slower, 1 at sprint speed.
            float t = Mathf.InverseLerp(movement.WalkSpeed, movement.SprintSpeed, movement.HorizontalSpeed);
            float target = _baseFov + Mathf.Pow(t, curvePower) * sprintFovBoost;
            _currentFov = Mathf.SmoothDamp(_currentFov, target, ref _fovVelocity, smoothTime);
            ApplyFov(_currentFov);
        }

        private void ApplyFov(float fov)
        {
            if (_vcam != null)
            {
                LensSettings lens = _vcam.Lens;
                lens.FieldOfView = fov;
                _vcam.Lens = lens;
            }
            else
            {
                _camera.fieldOfView = fov;
            }
        }
    }
}

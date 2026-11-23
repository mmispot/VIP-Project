using UnityEngine;

namespace VIPProject.Player
{
    /// <summary>
    /// Widens the Camera's field of view as the player speeds up toward sprint speed.
    /// Driven by actual horizontal speed (not the key press), so it follows the
    /// acceleration curve; a SmoothDamp on top removes any hard edges.
    /// Put this on the same GameObject as the Camera.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class SprintFov : MonoBehaviour
    {
        private Camera _camera;
        private float _baseFov;
        private float _fovVelocity;

        [SerializeField] private PlayerMovement movement;

        [Header("Live sliders (tweak in Play mode)")]
        [Tooltip("Extra degrees of FOV at full sprint speed. Was effectively 0 before.")]
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
            _camera = GetComponent<Camera>();
            _baseFov = _camera.fieldOfView;
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
            if (_camera != null) _camera.fieldOfView = _baseFov;
        }

        private void LateUpdate()
        {
            // 0 at walk speed or slower, 1 at sprint speed.
            float t = Mathf.InverseLerp(movement.WalkSpeed, movement.SprintSpeed, movement.HorizontalSpeed);
            float target = _baseFov + Mathf.Pow(t, curvePower) * sprintFovBoost;
            _camera.fieldOfView = Mathf.SmoothDamp(_camera.fieldOfView, target, ref _fovVelocity, smoothTime);
        }
    }
}

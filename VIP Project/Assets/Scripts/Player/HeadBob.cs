using UnityEngine;

namespace VIPProject.Player
{
    /// <summary>
    /// Walking head bob on the Camera's local position. Phase advances with distance
    /// walked (so steps match speed); amplitude eases in/out with speed so there is
    /// never a sudden camera pop. Also adds a small dip on landing.
    /// </summary>
    public class HeadBob : MonoBehaviour
    {
        private Vector3 _restLocalPosition;
        private float _phase;
        private float _amplitude;       // 0..1 smoothed
        private float _landOffset;      // current landing dip (negative = down)
        private float _landVelocity;
        private float _landTarget;

        [SerializeField] private PlayerMovement movement;

        [Header("Live sliders (tweak in Play mode)")]
        [Range(0f, 2f)]
        [Tooltip("Master strength of the bob. 0 = off, 1 = default, 2 = double.")]
        [SerializeField] private float bobIntensity = 1f;
        [Range(0.25f, 3f)]
        [Tooltip("How fast the bob cycles for a given walking speed. 1 = default.")]
        [SerializeField] private float bobSpeed = 1f;

        [Header("Bob")]
        [Tooltip("Vertical amplitude in metres at walk speed.")]
        [SerializeField] private float verticalAmount = 0.026f;
        [Tooltip("Sideways sway in metres at walk speed.")]
        [SerializeField] private float horizontalAmount = 0.014f;
        [Tooltip("Steps per metre walked. 1.0 = ~1.5 steps/s at walkSpeed; a relaxed human pace.")]
        [SerializeField] private float stepsPerMetre = 1.0f;
        [Tooltip("How quickly the bob fades in and out.")]
        [SerializeField] private float blendSpeed = 5f;
        [Tooltip("Bob strength at full sprint relative to walking. Below 1 = calmer bob while sprinting (the FOV kick sells the speed instead).")]
        [Range(0f, 1.5f)]
        [SerializeField] private float sprintAmplitudeMultiplier = 0.7f;

        [Header("Landing")]
        [SerializeField] private float landDipPerMetreSecond = 0.012f;
        [SerializeField] private float maxLandDip = 0.12f;
        [SerializeField] private float landRecoverTime = 0.12f;

        private void Awake()
        {
            _restLocalPosition = transform.localPosition;
        }

        private void OnEnable()
        {
            if (movement == null) { enabled = false; return; }
            movement.Landed += OnLanded;
        }

        private void OnDisable()
        {
            if (movement != null) movement.Landed -= OnLanded;
            transform.localPosition = _restLocalPosition;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            float speed = movement.IsGrounded ? movement.HorizontalSpeed : 0f;

            float targetAmp = Mathf.Clamp01(speed / movement.WalkSpeed);
            if (speed > movement.WalkSpeed * 1.05f)
                targetAmp = Mathf.Lerp(1f, sprintAmplitudeMultiplier,
                    Mathf.InverseLerp(movement.WalkSpeed, movement.SprintSpeed, speed));
            _amplitude = Mathf.MoveTowards(_amplitude, targetAmp, blendSpeed * dt);

            // Phase follows distance so footsteps stay in sync with speed.
            // One full cycle (2π) = two steps (left + right).
            _phase += speed * stepsPerMetre * bobSpeed * Mathf.PI * dt;
            if (_phase > Mathf.PI * 2f * 1000f) _phase -= Mathf.PI * 2f * 1000f;

            // Pure sine curves: no sharp "bounce" cusps, so it reads as a body rolling over
            // each foot. Vertical = one dip per step; sway = one swing per two steps.
            // The sway is delayed a quarter step so the head drifts toward the planted foot.
            float y = Mathf.Cos(_phase * 2f);
            float x = Mathf.Sin(_phase - 0.4f);

            // Landing dip: spring down fast, recover smoothly.
            _landOffset = Mathf.SmoothDamp(_landOffset, _landTarget, ref _landVelocity, landRecoverTime);
            _landTarget = Mathf.MoveTowards(_landTarget, 0f, dt / landRecoverTime * maxLandDip);

            transform.localPosition = _restLocalPosition + new Vector3(
                x * horizontalAmount * _amplitude * bobIntensity,
                y * verticalAmount * _amplitude * bobIntensity + _landOffset,
                0f);
        }

        private void OnLanded(float impactSpeed)
        {
            _landTarget = -Mathf.Min(maxLandDip, impactSpeed * landDipPerMetreSecond);
        }
    }
}

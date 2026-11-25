using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VIPProject.Player
{
    /// <summary>
    /// First-person walk / sprint / jump on a CharacterController.
    /// Velocity eases toward the target with MoveTowards so starts and stops feel human.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        private const float GroundStick = -2f;

        private CharacterController _controller;
        private InputAction _move;
        private InputAction _jump;
        private InputAction _sprint;
        private InputAction _crouch;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private bool _wasGrounded;
        private bool _locked;
        private bool _crouchToggled;
        private float _crouchAmount;    // 0 = standing, 1 = fully crouched (smoothed)
        private float _crouchVelocity;
        private float _standHeight;
        private float _standPivotY;

        [Header("Input (assign InputSystem_Actions)")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Ground")]
        [SerializeField] private float walkSpeed = 1.5f;
        [SerializeField] private float sprintSpeed = 3f;
        [Tooltip("m/s² while speeding up (~0.3 s to full speed at walkSpeed).")]
        [SerializeField] private float acceleration = 5f;
        [Tooltip("m/s² while slowing down (~0.2 s to a stop).")]
        [SerializeField] private float deceleration = 7f;

        [Header("Crouch (C / gamepad East)")]
        [Tooltip("Camera pivot to lower. Auto-found as a child named 'CameraPivot' if left empty.")]
        [SerializeField] private Transform cameraPivot;
        [Tooltip("CharacterController height while crouched. Eye height drops by the same amount.")]
        [SerializeField] private float crouchHeight = 1.2f;
        [Tooltip("Walk speed while fully crouched.")]
        [SerializeField] private float crouchSpeed = 0.8f;
        [Tooltip("Seconds to drop into a crouch. Quick, like bending the knees.")]
        [SerializeField] private float crouchDownTime = 0.22f;
        [Tooltip("Seconds to rise back up. Slower than going down, like pushing up against your own weight.")]
        [SerializeField] private float standUpTime = 0.34f;
        [Tooltip("Off = hold C to crouch. On = tap C to toggle.")]
        [SerializeField] private bool toggleCrouch;
        [Tooltip("What blocks standing up (ceilings, tables). Triggers are ignored.")]
        [SerializeField] private LayerMask headroomMask = ~0;

        [Header("Jump")]
        [SerializeField] private float jumpHeight = 0.8f;
        [SerializeField] private float gravity = -9.81f;
        [Range(0f, 1f)]
        [Tooltip("Fraction of ground acceleration available in the air.")]
        [SerializeField] private float airControl = 1f;

        public bool IsGrounded => _controller.isGrounded;
        public bool IsLocked => _locked;
        public float HorizontalSpeed => new Vector2(_horizontalVelocity.x, _horizontalVelocity.z).magnitude;
        public float SprintSpeed => sprintSpeed;
        public float WalkSpeed => walkSpeed;
        public bool IsCrouching => _crouchAmount > 0.5f;
        /// <summary>0 = standing, 1 = fully crouched. Smoothed, safe to drive animation/audio from.</summary>
        public float CrouchAmount => _crouchAmount;

        /// <summary>Raised on touchdown with the downward speed (m/s, positive) at impact.</summary>
        public event Action<float> Landed;
        public event Action Jumped;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (inputActions == null)
            {
                Debug.LogError("PlayerMovement: inputActions is not assigned.", this);
                enabled = false;
                return;
            }
            _move = inputActions.FindAction("Player/Move", true);
            _jump = inputActions.FindAction("Player/Jump", true);
            _sprint = inputActions.FindAction("Player/Sprint", true);
            _crouch = inputActions.FindAction("Player/Crouch", true);

            _standHeight = _controller.height;
            crouchHeight = Mathf.Clamp(crouchHeight, _controller.radius * 2f + 0.01f, _standHeight);
            if (cameraPivot == null) cameraPivot = transform.Find("CameraPivot");
            if (cameraPivot != null) _standPivotY = cameraPivot.localPosition.y;
            else Debug.LogWarning("PlayerMovement: no cameraPivot found, crouch will not lower the camera.", this);
        }

        private void OnEnable()
        {
            _move?.Enable();
            _jump?.Enable();
            _sprint?.Enable();
            _crouch?.Enable();
        }

        private void OnDisable()
        {
            _move?.Disable();
            _jump?.Disable();
            _sprint?.Disable();
            _crouch?.Disable();
            // Leave the body and camera standing so re-enabling never starts from a stale crouch.
            _crouchAmount = 0f;
            _crouchVelocity = 0f;
            _crouchToggled = false;
            ApplyCrouch();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            bool grounded = _controller.isGrounded;

            // Landing detection (before we reset vertical velocity).
            if (grounded && !_wasGrounded)
                Landed?.Invoke(Mathf.Max(0f, -_verticalVelocity));

            // Locked = input reads as zero, so the player eases to a stop.
            Vector2 input = _locked ? Vector2.zero : Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f);

            UpdateCrouch(grounded, dt);

            // No sprinting out of (or while in) a crouch. Speed blends with the crouch so
            // slowing down and speeding up are eased rather than stepped.
            bool sprinting = !_locked && _crouchAmount < 0.1f && _sprint.IsPressed();
            float targetSpeed = Mathf.Lerp(sprinting ? sprintSpeed : walkSpeed, crouchSpeed, _crouchAmount);

            Vector3 target = (transform.right * input.x + transform.forward * input.y) * targetSpeed;
            float rate = target.sqrMagnitude > 0.0001f ? acceleration : deceleration;
            if (!grounded) rate *= airControl;
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, target, rate * dt);

            if (grounded && _verticalVelocity < 0f)
                _verticalVelocity = GroundStick;

            // Can't jump from a crouch: you have to stand up first.
            if (grounded && !_locked && _crouchAmount < 0.3f && _jump.WasPressedThisFrame())
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                Jumped?.Invoke();
            }

            _verticalVelocity += gravity * dt;

            Vector3 velocity = _horizontalVelocity + Vector3.up * _verticalVelocity;
            CollisionFlags flags = _controller.Move(velocity * dt);

            if ((flags & CollisionFlags.Above) != 0 && _verticalVelocity > 0f)
                _verticalVelocity = 0f;

            _wasGrounded = grounded;
        }

        public void SetLocked(bool locked) => _locked = locked;

        private void UpdateCrouch(bool grounded, float dt)
        {
            bool held = !_locked && _crouch.IsPressed();
            if (toggleCrouch && !_locked && _crouch.WasPressedThisFrame())
                _crouchToggled = !_crouchToggled;
            if (_locked) _crouchToggled = false;

            bool want = toggleCrouch ? _crouchToggled : held;

            // Only start a crouch on the ground; one already in progress can finish in the air.
            if (want && !grounded && _crouchAmount <= 0f) want = false;

            // Under a low ceiling, stay down until there is room to stand.
            if (!want && _crouchAmount > 0f && !HasHeadroom())
                want = true;

            float target = want ? 1f : 0f;
            float smoothTime = want ? crouchDownTime : standUpTime;
            _crouchAmount = Mathf.SmoothDamp(_crouchAmount, target, ref _crouchVelocity, smoothTime, Mathf.Infinity, dt);
            if (Mathf.Abs(_crouchAmount - target) < 0.001f)
            {
                _crouchAmount = target;
                _crouchVelocity = 0f;
            }

            ApplyCrouch();
        }

        private void ApplyCrouch()
        {
            if (_controller == null || _standHeight <= 0f) return;

            float height = Mathf.Lerp(_standHeight, crouchHeight, _crouchAmount);
            // Centre at height/2 keeps the feet planted; only the top of the capsule moves.
            _controller.height = height;
            _controller.center = new Vector3(0f, height * 0.5f, 0f);

            if (cameraPivot != null)
            {
                Vector3 p = cameraPivot.localPosition;
                p.y = _standPivotY - (_standHeight - height);
                cameraPivot.localPosition = p;
            }
        }

        private bool HasHeadroom()
        {
            float radius = _controller.radius;
            float extra = _standHeight - _controller.height;
            if (extra <= 0.001f) return true;

            // Sweep the top of the capsule up to standing height. Starting inside our own
            // collider means it is ignored, so only real obstacles are hit.
            Vector3 origin = transform.position + Vector3.up * (_controller.height - radius);
            return !Physics.SphereCast(origin, radius * 0.9f, Vector3.up, out _, extra + 0.02f,
                headroomMask, QueryTriggerInteraction.Ignore);
        }
    }
}

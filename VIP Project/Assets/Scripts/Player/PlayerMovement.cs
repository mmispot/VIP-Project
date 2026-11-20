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
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private bool _wasGrounded;
        private bool _locked;

        [Header("Input (assign InputSystem_Actions)")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Ground")]
        [SerializeField] private float walkSpeed = 1.5f;
        [SerializeField] private float sprintSpeed = 3f;
        [Tooltip("m/s² while speeding up (~0.3 s to full speed at walkSpeed).")]
        [SerializeField] private float acceleration = 5f;
        [Tooltip("m/s² while slowing down (~0.2 s to a stop).")]
        [SerializeField] private float deceleration = 7f;

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
        }

        private void OnEnable()
        {
            _move?.Enable();
            _jump?.Enable();
            _sprint?.Enable();
        }

        private void OnDisable()
        {
            _move?.Disable();
            _jump?.Disable();
            _sprint?.Disable();
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
            bool sprinting = !_locked && _sprint.IsPressed();
            float targetSpeed = sprinting ? sprintSpeed : walkSpeed;

            Vector3 target = (transform.right * input.x + transform.forward * input.y) * targetSpeed;
            float rate = target.sqrMagnitude > 0.0001f ? acceleration : deceleration;
            if (!grounded) rate *= airControl;
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, target, rate * dt);

            if (grounded && _verticalVelocity < 0f)
                _verticalVelocity = GroundStick;

            if (grounded && !_locked && _jump.WasPressedThisFrame())
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
    }
}

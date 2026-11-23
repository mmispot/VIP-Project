using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using VIPProject.Player;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace VIPProject.Cutscenes
{
    /// <summary>
    /// Zelda-style "something opened over there" cutscene.
    /// Call Play() when the objective is done (hook it to a UnityEvent). The player is locked,
    /// the CinemachineBrain blends to the door camera (higher priority than the player vcam),
    /// the door opens, then we blend back and give control back.
    /// The blend style/time is the Brain's Default Blend, so tune the feel there.
    /// </summary>
    public class DoorCutscene : MonoBehaviour
    {
        private const float BlendTimeout = 5f;

        private Coroutine _routine;
        private bool _playing;
        private bool _hasPlayed;
        private Vector3 _camStartPosition;
        private Vector3 _camEndPosition;
        private float _pushElapsed;

        [Header("Cinemachine")]
        [SerializeField] private CinemachineBrain brain;
        [Tooltip("Camera that frames the door. Keep it lower priority than the player vcam (idle).")]
        [SerializeField] private CinemachineCamera cutsceneCamera;
        [Tooltip("Priority while the cutscene runs. Must be higher than the player vcam (10).")]
        [SerializeField] private int activePriority = 20;
        [SerializeField] private int idlePriority = 0;

        [Header("Door")]
        [SerializeField] private Transform door;
        [Tooltip("Door moves by this much (in its parent's space) when opened. Default slides it up.")]
        [SerializeField] private Vector3 openOffset = new Vector3(0f, 2.8f, 0f);
        [Tooltip("Extra rotation applied when opened (e.g. 0,90,0 for a hinged door).")]
        [SerializeField] private Vector3 openEuler = Vector3.zero;
        [SerializeField] private float openDuration = 1.6f;

        [Header("Player")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerLook playerLook;
        [SerializeField] private PlayerInteractor playerInteractor;

        [Header("Timing")]
        [Tooltip("Pause on the door after the blend arrives, before it starts opening.")]
        [SerializeField] private float holdBeforeOpen = 0.6f;
        [Tooltip("Pause on the open door before blending back to the player.")]
        [SerializeField] private float holdAfterOpen = 1.2f;
        [Tooltip("Slow drift of the cutscene camera toward the door. 0 = static shot.")]
        [SerializeField] private float pushInDistance = 0.8f;
        [Tooltip("Seconds the drift takes, counted from the start of the blend.")]
        [SerializeField] private float pushInDuration = 5f;
        [SerializeField] private bool playOnce = true;

        [Header("Events (hook sound, particles, etc.)")]
        [SerializeField] private UnityEvent onCutsceneStarted;
        [SerializeField] private UnityEvent onDoorOpening;
        [SerializeField] private UnityEvent onCutsceneFinished;

#if UNITY_EDITOR
        [Header("Editor testing")]
        [Tooltip("Press F9 in Play mode to trigger the cutscene.")]
        [SerializeField] private bool enableTestKey = true;
#endif

        public bool IsPlaying => _playing;

        private void Awake()
        {
            if (brain == null || cutsceneCamera == null || door == null)
            {
                Debug.LogError("DoorCutscene: brain, cutsceneCamera and door must be assigned.", this);
                enabled = false;
                return;
            }
            cutsceneCamera.Priority = idlePriority;
        }

        private void OnDisable()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            if (_playing) FinishImmediately();
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (enableTestKey && Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
                Play();
#endif
            if (_playing && pushInDistance > 0f)
            {
                _pushElapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_pushElapsed / pushInDuration));
                cutsceneCamera.transform.position = Vector3.Lerp(_camStartPosition, _camEndPosition, t);
            }
        }

        /// <summary>Hook this to the objective's completion event.</summary>
        [ContextMenu("Play Cutscene")]
        public void Play()
        {
            if (_playing || (playOnce && _hasPlayed)) return;
            _hasPlayed = true;
            _routine = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            _playing = true;
            onCutsceneStarted.Invoke();
            SetPlayerLocked(true);

            _camStartPosition = cutsceneCamera.transform.position;
            _camEndPosition = _camStartPosition + cutsceneCamera.transform.forward * pushInDistance;
            _pushElapsed = 0f;

            cutsceneCamera.Priority = activePriority;
            yield return WaitForBlend();

            yield return new WaitForSeconds(holdBeforeOpen);

            onDoorOpening.Invoke();
            yield return OpenDoor();

            yield return new WaitForSeconds(holdAfterOpen);

            // Drop priority: the Brain blends back to the player vcam.
            cutsceneCamera.Priority = idlePriority;
            yield return WaitForBlend();

            // Reset the drift only now that the cutscene camera is off screen.
            cutsceneCamera.transform.position = _camStartPosition;
            _playing = false;
            _routine = null;
            SetPlayerLocked(false);
            onCutsceneFinished.Invoke();
        }

        private IEnumerator WaitForBlend()
        {
            // Let the Brain notice the priority change before checking IsBlending.
            yield return null;
            yield return null;
            float timeout = BlendTimeout;
            while (brain.IsBlending && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator OpenDoor()
        {
            Vector3 fromPos = door.localPosition;
            Vector3 toPos = fromPos + openOffset;
            Quaternion fromRot = door.localRotation;
            Quaternion toRot = fromRot * Quaternion.Euler(openEuler);

            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / openDuration));
                door.localPosition = Vector3.Lerp(fromPos, toPos, t);
                door.localRotation = Quaternion.Slerp(fromRot, toRot, t);
                yield return null;
            }
            door.localPosition = toPos;
            door.localRotation = toRot;
        }

        private void FinishImmediately()
        {
            cutsceneCamera.Priority = idlePriority;
            cutsceneCamera.transform.position = _camStartPosition;
            _playing = false;
            SetPlayerLocked(false);
        }

        private void SetPlayerLocked(bool locked)
        {
            if (playerMovement != null) playerMovement.SetLocked(locked);
            if (playerLook != null) playerLook.SetLocked(locked);
            if (playerInteractor != null) playerInteractor.SetLocked(locked);
        }
    }
}

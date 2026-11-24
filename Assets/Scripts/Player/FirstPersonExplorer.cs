using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace LethalFactory.Player
{
    /// <summary>
    /// Minimal First-Person exploration controller for navigating the abandoned factory.
    /// Uses Unity's New Input System (WASD / Arrows to walk, Left Shift to run, Mouse to look,
    /// F to toggle suit flashlight, Esc to unlock cursor). Drives Player_Hazmat Idle/Walk/Run animations.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonExplorer : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3.6f;
        [SerializeField] private float runSpeed = 6.5f;
        [SerializeField] private float gravity = -18.0f;

        [Header("Mouse Look")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float mouseSensitivity = 0.14f;
        [SerializeField] private float maxPitchAngle = 84f;

        [Header("First-Person Rig & Visuals")]
        [SerializeField] private Animator characterAnimator;
        [SerializeField] private Renderer headMeshRenderer;
        [SerializeField] private Light suitFlashlight;
        [SerializeField] private bool enableHeadBob = true;
        [SerializeField] private float walkBobAmplitude = 0.025f;
        [SerializeField] private float runBobAmplitude = 0.048f;

        private CharacterController _controller;
        private float _pitch;
        private float _verticalVelocity;
        private float _smoothedSpeed;
        private float _bobTimer;
        private Vector3 _cameraDefaultLocalPos;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            if (cameraPivot == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    cameraPivot = cam.transform.parent != null && cam.transform.parent != transform
                        ? cam.transform.parent
                        : cam.transform;
                }
            }

            if (cameraPivot != null)
            {
                _cameraDefaultLocalPos = cameraPivot.localPosition;
            }

            if (characterAnimator == null)
            {
                characterAnimator = GetComponentInChildren<Animator>();
            }

            // Hide helmet geometry from the First-Person camera while keeping full shadows!
            if (headMeshRenderer == null)
            {
                foreach (SkinnedMeshRenderer smr in GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (smr.name.Contains("Head"))
                    {
                        headMeshRenderer = smr;
                        break;
                    }
                }
            }

            if (headMeshRenderer != null)
            {
                headMeshRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }

        private void Start()
        {
            LockCursor(true);
        }

        private void Update()
        {
            HandleCursorLock();
            HandleMouseLook();
            HandleMovement();
            HandleFlashlightToggle();
        }

        private void HandleCursorLock()
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                LockCursor(false);
            }
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor(true);
            }
        }

        private void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleMouseLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
                return;

            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            Vector2 delta = mouse.delta.ReadValue();
            float yaw = delta.x * mouseSensitivity;
            float pitchDelta = delta.y * mouseSensitivity;

            transform.Rotate(Vector3.up * yaw);

            if (cameraPivot != null)
            {
                _pitch = Mathf.Clamp(_pitch - pitchDelta, -maxPitchAngle, maxPitchAngle);
                cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            Keyboard kb = Keyboard.current;
            Vector2 moveInput = Vector2.zero;
            bool runHeld = false;

            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) moveInput.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) moveInput.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveInput.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveInput.x -= 1f;
                runHeld = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            }

            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }

            bool isMoving = moveInput.sqrMagnitude > 0.01f;
            bool isRunning = isMoving && runHeld;
            float targetSpeed = isMoving ? (isRunning ? runSpeed : walkSpeed) : 0f;

            Vector3 horizontalMove = (transform.right * moveInput.x + transform.forward * moveInput.y) * targetSpeed;

            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }
            _verticalVelocity += gravity * Time.deltaTime;

            Vector3 finalMove = horizontalMove + Vector3.up * _verticalVelocity;
            _controller.Move(finalMove * Time.deltaTime);

            // Smoothly update Animator (Player_Idle / Player_Walk / Player_Run)
            float normalizedSpeed = isMoving ? (isRunning ? 1.0f : 0.5f) : 0.0f;
            _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, normalizedSpeed, Time.deltaTime * 10f);

            if (characterAnimator != null)
            {
                characterAnimator.SetFloat(SpeedHash, _smoothedSpeed);
                characterAnimator.SetBool(IsRunningHash, isRunning);
            }

            // Subtle first-person step head-bob
            if (enableHeadBob && cameraPivot != null)
            {
                if (isMoving && _controller.isGrounded)
                {
                    float freq = isRunning ? 13.5f : 8.5f;
                    float amp = isRunning ? runBobAmplitude : walkBobAmplitude;
                    _bobTimer += Time.deltaTime * freq;
                    Vector3 bobOffset = new Vector3(
                        Mathf.Cos(_bobTimer * 0.5f) * amp * 0.45f,
                        Mathf.Sin(_bobTimer) * amp,
                        0f
                    );
                    cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, _cameraDefaultLocalPos + bobOffset, Time.deltaTime * 12f);
                }
                else
                {
                    _bobTimer = 0f;
                    cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, _cameraDefaultLocalPos, Time.deltaTime * 8f);
                }
            }
        }

        private void HandleFlashlightToggle()
        {
            if (suitFlashlight == null)
                return;

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.fKey.wasPressedThisFrame)
            {
                suitFlashlight.enabled = !suitFlashlight.enabled;
            }
        }
    }
}

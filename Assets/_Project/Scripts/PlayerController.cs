using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Min(0.1f)] public float walkSpeed = 2f;
    [Min(0.1f)] public float runSpeed = 5f;
    public float turnSmoothTime = 0.05f;
    public float gravity = -20f;
    [Min(0.1f)] public float jumpHeight = 1.4f;
    [Tooltip("Window to register a Space press before landing. Helps when isGrounded flickers.")]
    public float jumpBufferTime = 0.15f;
    [Tooltip("Window to allow a jump shortly after losing ground contact.")]
    public float coyoteTime = 0.12f;

    [Header("Camera reference (optional)")]
    [Tooltip("If set, WASD is interpreted relative to this transform's forward (typical for 3rd person). If null, world space is used.")]
    public Transform cameraReference;

    private CharacterController controller;
    private Animator animator;
    private float turnVelocity;
    private float verticalVelocity;
    private Vector3 airHorizontalVelocity;
    private float jumpBufferTimer;
    private float coyoteTimer;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        var kb = Keyboard.current;
        float h = 0f, v = 0f;
        bool isRunning = false;
        bool jumpPressed = false;
        if (kb != null && !GameplayInputLock.Locked)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    v += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  v -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  h -= 1f;
            isRunning = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            jumpPressed = kb.spaceKey.wasPressedThisFrame;
        }
        Vector3 inputDir = new Vector3(h, 0f, v);

        float maxSpeed = isRunning ? runSpeed : walkSpeed;

        bool grounded = controller.isGrounded;

        // Jump input buffer + coyote timer
        jumpBufferTimer = jumpPressed ? jumpBufferTime : Mathf.Max(0f, jumpBufferTimer - Time.deltaTime);
        coyoteTimer = grounded ? coyoteTime : Mathf.Max(0f, coyoteTimer - Time.deltaTime);

        Vector3 horizontalMotion;
        float currentSpeed;

        if (grounded)
        {
            // On ground: full control from input
            horizontalMotion = Vector3.zero;
            currentSpeed = 0f;
            if (inputDir.sqrMagnitude > 0.01f)
            {
                inputDir.Normalize();
                float refYaw = cameraReference != null ? cameraReference.eulerAngles.y : 0f;
                float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + refYaw;
                float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                horizontalMotion = moveDir * maxSpeed;
                currentSpeed = maxSpeed;
            }

            if (verticalVelocity < 0f) verticalVelocity = -1f;
            airHorizontalVelocity = horizontalMotion;
        }
        else
        {
            // In air: preserve horizontal momentum captured when leaving ground
            horizontalMotion = airHorizontalVelocity;
            currentSpeed = horizontalMotion.magnitude;
            verticalVelocity += gravity * Time.deltaTime;
        }

        // Buffered + coyote jump (works while walking/running too)
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            // Snapshot the horizontal motion at lift-off so we keep going forward
            airHorizontalVelocity = horizontalMotion;
            animator.SetTrigger(JumpHash);
        }

        Vector3 finalMotion = horizontalMotion + Vector3.up * verticalVelocity;
        controller.Move(finalMotion * Time.deltaTime);

        animator.SetFloat(SpeedHash, currentSpeed / runSpeed);
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

public class CameraOrbitInput : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Transform the pivot follows (typically the Player).")]
    public Transform target;
    [Tooltip("Offset above the target's pivot - usually around the head.")]
    public Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Mouse")]
    [Min(0.01f)] public float yawSpeed = 0.1f;
    [Min(0.01f)] public float pitchSpeed = 0.06f;
    public bool invertY = false;
    [Tooltip("Higher = smoother rotation. 0 = no smoothing.")]
    [Range(0f, 0.5f)] public float rotationSmoothTime = 0.08f;
    [Tooltip("Only rotate while the right mouse button is held.")]
    public bool requireRightMouse = true;

    [Header("Pitch clamp")]
    public float minPitch = -10f;
    public float maxPitch = 60f;

    private float yaw;
    private float pitch = 15f;
    private float yawTarget;
    private float pitchTarget = 15f;
    private float yawVelocity;
    private float pitchVelocity;
    private bool wasRotating;

    private void Start()
    {
        if (target != null)
        {
            yaw = target.eulerAngles.y;
            yawTarget = yaw;
        }
    }

    private void OnDisable()
    {
        SetCursorLocked(false);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        var mouse = Mouse.current;
        bool isRotating = !requireRightMouse || (mouse != null && mouse.rightButton.isPressed);

        if (isRotating != wasRotating)
        {
            SetCursorLocked(isRotating);
            wasRotating = isRotating;
        }

        if (isRotating && mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue();
            yawTarget += delta.x * yawSpeed;
            pitchTarget += (invertY ? delta.y : -delta.y) * pitchSpeed;
            pitchTarget = Mathf.Clamp(pitchTarget, minPitch, maxPitch);
        }

        if (rotationSmoothTime > 0f)
        {
            yaw = Mathf.SmoothDampAngle(yaw, yawTarget, ref yawVelocity, rotationSmoothTime);
            pitch = Mathf.SmoothDamp(pitch, pitchTarget, ref pitchVelocity, rotationSmoothTime);
        }
        else
        {
            yaw = yawTarget;
            pitch = pitchTarget;
        }

        transform.position = target.position + targetOffset;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}

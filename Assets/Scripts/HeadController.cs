using UnityEngine;

public class HeadController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Collider coll;
    [SerializeField] private StickTiltForce stickTiltForce;

    [Header("Head Follow")]
    [SerializeField, Min(0.01f)] private float followSharpness = 10f;

    [Header("Rotation Limits (Degrees, Relative To Initial Rotation)")]
    [SerializeField] private Vector2 pitchLimits = new Vector2(-35f, 35f);
    [SerializeField] private Vector2 yawLimits = new Vector2(-70f, 70f);
    [SerializeField] private Vector2 rollLimits = new Vector2(-10f, 10f);

    [Header("Gizmos")]
    [SerializeField] private bool drawLimitGizmos = true;
    [SerializeField, Min(0.1f)] private float gizmoDistance = 0.75f;
    [SerializeField, Range(4, 64)] private int gizmoSegments = 20;
    [SerializeField] private Color gizmoCenterColor = new Color(0.2f, 0.8f, 1f, 1f);
    [SerializeField] private Color gizmoBoundaryColor = new Color(1f, 0.55f, 0.2f, 1f);
    [SerializeField] private Color gizmoRollColor = new Color(0.7f, 1f, 0.3f, 1f);

    private Quaternion initialLocalRotation;
    private Quaternion initialWorldRotation;

    private void Awake()
    {
        initialLocalRotation = transform.localRotation;
        initialWorldRotation = transform.rotation;
        EnsureValidLimits();
    }

    private void OnValidate()
    {
        EnsureValidLimits();
    }

    private void OnEnable()
    {
        BindRetryEvents();
        SyncColliderState();
    }

    private void OnDisable()
    {
        UnbindRetryEvents();
    }

    private void BindRetryEvents()
    {
        if (stickTiltForce == null)
        {
            Debug.LogWarning("HeadController: stickTiltForce is not assigned.", this);
            return;
        }

        stickTiltForce.RetryStateChanged -= HandleRetryStateChanged;
        stickTiltForce.RetryStateChanged += HandleRetryStateChanged;
    }

    private void UnbindRetryEvents()
    {
        if (stickTiltForce == null)
        {
            return;
        }

        stickTiltForce.RetryStateChanged -= HandleRetryStateChanged;
    }

    private void HandleRetryStateChanged(bool isRetryRequired)
    {
        SetColliderEnabled(isRetryRequired);
    }

    private void SyncColliderState()
    {
        SetColliderEnabled(stickTiltForce != null && stickTiltForce.IsRetryRequired);
    }

    private void SetColliderEnabled(bool enabled)
    {
        if (coll != null && coll.enabled != enabled)
        {
            coll.enabled = enabled;
        }
    }

    private void LateUpdate()
    {
        UpdateHeadRotation();
    }

    private void UpdateHeadRotation()
    {
        if (target == null)
        {
            return;
        }

        Vector3 lookDirection = target.position - transform.position;
        if (lookDirection.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        Vector3 upAxis = transform.parent != null ? transform.parent.up : Vector3.up;
        Quaternion desiredWorldRotation = Quaternion.LookRotation(lookDirection.normalized, upAxis);
        Quaternion clampedTargetRotation = GetClampedTargetRotation(desiredWorldRotation);
        float lerpFactor = 1f - Mathf.Exp(-Mathf.Max(0.01f, followSharpness) * Time.deltaTime);

        if (transform.parent != null)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, clampedTargetRotation, lerpFactor);
            return;
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, clampedTargetRotation, lerpFactor);
    }

    private Quaternion GetClampedTargetRotation(Quaternion desiredWorldRotation)
    {
        if (transform.parent != null)
        {
            Quaternion desiredLocalRotation = Quaternion.Inverse(transform.parent.rotation) * desiredWorldRotation;
            return ClampRelativeRotation(initialLocalRotation, desiredLocalRotation);
        }

        return ClampRelativeRotation(initialWorldRotation, desiredWorldRotation);
    }

    private Quaternion ClampRelativeRotation(Quaternion initialRotation, Quaternion desiredRotation)
    {
        Vector3 relativeEuler = NormalizeEulerAngles((Quaternion.Inverse(initialRotation) * desiredRotation).eulerAngles);
        relativeEuler.x = Mathf.Clamp(relativeEuler.x, pitchLimits.x, pitchLimits.y);
        relativeEuler.y = Mathf.Clamp(relativeEuler.y, yawLimits.x, yawLimits.y);
        relativeEuler.z = Mathf.Clamp(relativeEuler.z, rollLimits.x, rollLimits.y);
        return initialRotation * Quaternion.Euler(relativeEuler);
    }

    private void EnsureValidLimits()
    {
        pitchLimits = SortLimits(pitchLimits);
        yawLimits = SortLimits(yawLimits);
        rollLimits = SortLimits(rollLimits);
    }

    private static Vector2 SortLimits(Vector2 limits)
    {
        if (limits.x <= limits.y)
        {
            return limits;
        }

        return new Vector2(limits.y, limits.x);
    }

    private static Vector3 NormalizeEulerAngles(Vector3 eulerAngles)
    {
        return new Vector3(
            NormalizeAngle(eulerAngles.x),
            NormalizeAngle(eulerAngles.y),
            NormalizeAngle(eulerAngles.z)
        );
    }

    private static float NormalizeAngle(float angle)
    {
        return Mathf.Repeat(angle + 180f, 360f) - 180f;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawLimitGizmos)
        {
            return;
        }

        EnsureValidLimits();

        Quaternion referenceRotation = GetGizmoReferenceRotation();
        Vector3 origin = transform.position;

        DrawCenterDirectionGizmo(origin, referenceRotation);
        DrawYawPitchBoundaryGizmo(origin, referenceRotation);
        DrawRollGizmo(origin, referenceRotation);
    }

    private Quaternion GetGizmoReferenceRotation()
    {
        if (Application.isPlaying)
        {
            if (transform.parent != null)
            {
                return transform.parent.rotation * initialLocalRotation;
            }

            return initialWorldRotation;
        }

        return transform.rotation;
    }

    private void DrawCenterDirectionGizmo(Vector3 origin, Quaternion referenceRotation)
    {
        Gizmos.color = gizmoCenterColor;
        Gizmos.DrawRay(origin, referenceRotation * Vector3.forward * gizmoDistance);
    }

    private void DrawYawPitchBoundaryGizmo(Vector3 origin, Quaternion referenceRotation)
    {
        Gizmos.color = gizmoBoundaryColor;

        int segments = Mathf.Max(4, gizmoSegments);
        float step = 1f / segments;

        Vector3 prevPitchMin = origin + GetForwardForAngles(referenceRotation, pitchLimits.x, yawLimits.x) * gizmoDistance;
        Vector3 prevPitchMax = origin + GetForwardForAngles(referenceRotation, pitchLimits.y, yawLimits.x) * gizmoDistance;
        for (int i = 1; i <= segments; i++)
        {
            float t = i * step;
            float yaw = Mathf.Lerp(yawLimits.x, yawLimits.y, t);

            Vector3 pitchMinPoint = origin + GetForwardForAngles(referenceRotation, pitchLimits.x, yaw) * gizmoDistance;
            Vector3 pitchMaxPoint = origin + GetForwardForAngles(referenceRotation, pitchLimits.y, yaw) * gizmoDistance;

            Gizmos.DrawLine(prevPitchMin, pitchMinPoint);
            Gizmos.DrawLine(prevPitchMax, pitchMaxPoint);
            prevPitchMin = pitchMinPoint;
            prevPitchMax = pitchMaxPoint;
        }

        Vector3 prevYawMin = origin + GetForwardForAngles(referenceRotation, pitchLimits.x, yawLimits.x) * gizmoDistance;
        Vector3 prevYawMax = origin + GetForwardForAngles(referenceRotation, pitchLimits.x, yawLimits.y) * gizmoDistance;
        for (int i = 1; i <= segments; i++)
        {
            float t = i * step;
            float pitch = Mathf.Lerp(pitchLimits.x, pitchLimits.y, t);

            Vector3 yawMinPoint = origin + GetForwardForAngles(referenceRotation, pitch, yawLimits.x) * gizmoDistance;
            Vector3 yawMaxPoint = origin + GetForwardForAngles(referenceRotation, pitch, yawLimits.y) * gizmoDistance;

            Gizmos.DrawLine(prevYawMin, yawMinPoint);
            Gizmos.DrawLine(prevYawMax, yawMaxPoint);
            prevYawMin = yawMinPoint;
            prevYawMax = yawMaxPoint;
        }
    }

    private void DrawRollGizmo(Vector3 origin, Quaternion referenceRotation)
    {
        Gizmos.color = gizmoRollColor;

        Vector3 rollOrigin = origin + referenceRotation * Vector3.forward * (gizmoDistance * 0.45f);
        float rollRadius = gizmoDistance * 0.16f;
        int segments = Mathf.Max(4, gizmoSegments / 2);

        Vector3 prevPoint = rollOrigin + GetUpForRoll(referenceRotation, rollLimits.x) * rollRadius;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float roll = Mathf.Lerp(rollLimits.x, rollLimits.y, t);
            Vector3 point = rollOrigin + GetUpForRoll(referenceRotation, roll) * rollRadius;
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }

        Vector3 minRollDir = GetUpForRoll(referenceRotation, rollLimits.x);
        Vector3 maxRollDir = GetUpForRoll(referenceRotation, rollLimits.y);
        Gizmos.DrawRay(rollOrigin, minRollDir * rollRadius);
        Gizmos.DrawRay(rollOrigin, maxRollDir * rollRadius);
    }

    private static Vector3 GetForwardForAngles(Quaternion referenceRotation, float pitch, float yaw)
    {
        Quaternion offset = Quaternion.Euler(pitch, yaw, 0f);
        return (referenceRotation * offset) * Vector3.forward;
    }

    private static Vector3 GetUpForRoll(Quaternion referenceRotation, float roll)
    {
        Quaternion offset = Quaternion.Euler(0f, 0f, roll);
        return (referenceRotation * offset) * Vector3.up;
    }
}

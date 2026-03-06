using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
public class CylinderTiltForce : MonoBehaviour
{
    public event Action<bool> RetryStateChanged;
    public event Action<bool> StartGateStateChanged;

    private enum TiltInputSource
    {
        None = 0,
        Accelerometer = 1
    }

    [Header("References")]
    [SerializeField] private Rigidbody rb;

    [Header("Tilt -> Force")]
    [SerializeField, Min(0f)] private float forceMultiplier = 20f;
    [SerializeField, Min(0f)] private float maxForce = 30f;
    [SerializeField] private bool invertX;
    [SerializeField] private bool invertZ = true;

    [Header("Nonlinear Response")]
    [SerializeField, Min(0.01f)] private float fullTiltForCurve = 0.28f;
    [SerializeField, Min(0f)] private float curveForceBoost = 36f;
    [SerializeField] private AnimationCurve tiltToForceCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.4f, 0.15f),
        new Keyframe(0.75f, 0.55f),
        new Keyframe(1f, 1f)
    );

    [Header("Filtering")]
    [SerializeField, Min(0f)] private float deadZone = 0.05f;
    [SerializeField, Min(0.01f)] private float smoothing = 12f;

    [Header("Calibration")]
    [SerializeField] private bool calibrateOnEnable = true;
    [SerializeField] private bool allowUnityRemoteInEditor = true;

    [Header("Start Gate")]
    [SerializeField] private bool requireHorizontalScreenUpOnStart = true;
    [SerializeField, Range(0f, 1f)] private float screenUpMinZ = 0.85f;
    [SerializeField, Min(0f)] private float maxFlatPlanarMagnitude = 0.2f;
    [SerializeField, Min(0f)] private float requiredFlatHoldSeconds = 0.2f;

    [Header("Retry Lock")]
    [SerializeField, Min(0f)] private float retryTiltAngleDegrees = 30f;
    [SerializeField, Min(0f)] private float retryRearmDelaySeconds = 0.25f;

    private TiltInputSource inputSource;
    private Vector2 baselineTilt;
    private Vector2 smoothedTilt;
    private bool retryRequired;
    private bool inputUnlocked;
    private float flatHoldTimer;
    private float retryRearmBlockedUntil;

    public bool IsRetryRequired => retryRequired;
    public bool IsInputUnlocked => inputUnlocked;

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    private void OnEnable()
    {
        bool useUnityRemoteInEditor = Application.isEditor && allowUnityRemoteInEditor;
        bool accelerometerAvailable = SystemInfo.supportsAccelerometer;
        inputSource = ResolveInputSource(accelerometerAvailable, useUnityRemoteInEditor);
        smoothedTilt = Vector2.zero;
        SetInputUnlocked(!requireHorizontalScreenUpOnStart);
        flatHoldTimer = 0f;
        retryRearmBlockedUntil = 0f;
        SetRetryState(false);

        if (inputSource == TiltInputSource.None)
        {
            Debug.LogWarning("CylinderTiltForce: accelerometer is unavailable. Tilt force is disabled.");
            return;
        }

        if (calibrateOnEnable && inputUnlocked)
        {
            CalibrateBaseline();
        }
    }

    [ContextMenu("Calibrate Tilt Baseline")]
    public void CalibrateBaseline()
    {
        baselineTilt = ReadTiltXY();
    }

    private void FixedUpdate()
    {
        if (inputSource == TiltInputSource.None || rb == null)
        {
            return;
        }

        if (!inputUnlocked)
        {
            if (!IsPhoneFlatScreenUp())
            {
                flatHoldTimer = 0f;
                smoothedTilt = Vector2.zero;
                return;
            }

            flatHoldTimer += Time.fixedDeltaTime;
            if (flatHoldTimer < requiredFlatHoldSeconds)
            {
                smoothedTilt = Vector2.zero;
                return;
            }

            SetInputUnlocked(true);
            if (calibrateOnEnable)
            {
                CalibrateBaseline();
            }

            smoothedTilt = Vector2.zero;
            return;
        }

        bool canRearmRetry = Time.time >= retryRearmBlockedUntil;
        if (!retryRequired && canRearmRetry)
        {
            float currentTiltAngle = Vector3.Angle(transform.up, Vector3.up);
            if (currentTiltAngle > retryTiltAngleDegrees)
            {
                SetRetryState(true);
            }
        }

        if (retryRequired)
        {
            smoothedTilt = Vector2.zero;
            return;
        }

        Vector2 tilt = ReadTiltXY() - baselineTilt;

        if (invertX)
        {
            tilt.x = -tilt.x;
        }

        if (invertZ)
        {
            tilt.y = -tilt.y;
        }

        float deadZoneValue = Mathf.Max(0f, deadZone);
        if (tilt.sqrMagnitude < deadZoneValue * deadZoneValue)
        {
            tilt = Vector2.zero;
        }

        float lerpFactor = 1f - Mathf.Exp(-Mathf.Max(0.01f, smoothing) * Time.fixedDeltaTime);
        smoothedTilt = Vector2.Lerp(smoothedTilt, tilt, lerpFactor);

        Vector3 planarTilt = new Vector3(smoothedTilt.x, 0f, smoothedTilt.y);
        float tiltMagnitude = planarTilt.magnitude;
        if (tiltMagnitude <= 0.00001f)
        {
            return;
        }

        Vector3 direction = planarTilt / tiltMagnitude;
        float linearForce = tiltMagnitude * Mathf.Max(0f, forceMultiplier);

        float normalizedTilt = Mathf.Clamp01(tiltMagnitude / Mathf.Max(0.01f, fullTiltForCurve));
        float curveValue = EvaluateForceCurve(normalizedTilt);
        float extraForce = curveValue * Mathf.Max(0f, curveForceBoost);

        float totalForceMagnitude = linearForce + extraForce;
        if (maxForce > 0f)
        {
            totalForceMagnitude = Mathf.Min(totalForceMagnitude, maxForce);
        }

        Vector3 force = direction * totalForceMagnitude;
        rb.AddForce(force, ForceMode.Acceleration);
    }

    private bool IsPhoneFlatScreenUp()
    {
        Vector3 acceleration = Input.acceleration;
        float screenUpZ = -acceleration.z;
        Vector2 planar = new Vector2(acceleration.x, acceleration.y);
        return screenUpZ >= screenUpMinZ && planar.magnitude <= maxFlatPlanarMagnitude;
    }

    public void ClearRetryRequirement()
    {
        SetRetryState(false);
        retryRearmBlockedUntil = Time.time + retryRearmDelaySeconds;
        smoothedTilt = Vector2.zero;
    }

    private void SetInputUnlocked(bool unlocked)
    {
        if (inputUnlocked == unlocked)
        {
            return;
        }

        inputUnlocked = unlocked;
        StartGateStateChanged?.Invoke(inputUnlocked);
    }

    private void SetRetryState(bool required)
    {
        if (retryRequired == required)
        {
            return;
        }

        retryRequired = required;
        RetryStateChanged?.Invoke(retryRequired);
    }

    private float EvaluateForceCurve(float normalizedTilt)
    {
        if (tiltToForceCurve == null || tiltToForceCurve.length == 0)
        {
            return normalizedTilt;
        }

        return Mathf.Max(0f, tiltToForceCurve.Evaluate(normalizedTilt));
    }

    private Vector2 ReadTiltXY()
    {
        switch (inputSource)
        {
            case TiltInputSource.Accelerometer:
                {
                    Vector3 acceleration = Input.acceleration;
                    return new Vector2(acceleration.x, acceleration.y);
                }
            default:
                return Vector2.zero;
        }
    }

    private static TiltInputSource ResolveInputSource(bool accelerometerAvailable, bool useUnityRemoteInEditor)
    {
        if (accelerometerAvailable || useUnityRemoteInEditor)
        {
            return TiltInputSource.Accelerometer;
        }

        return TiltInputSource.None;
    }
}

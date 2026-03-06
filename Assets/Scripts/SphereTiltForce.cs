using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SphereTiltForce : MonoBehaviour
{
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

    [Header("Filtering")]
    [SerializeField, Min(0f)] private float deadZone = 0.05f;
    [SerializeField, Min(0.01f)] private float smoothing = 12f;

    [Header("Calibration")]
    [SerializeField] private bool calibrateOnEnable = true;
    [SerializeField] private bool allowUnityRemoteInEditor = true;

    private TiltInputSource inputSource;
    private Vector2 baselineTilt;
    private Vector2 smoothedTilt;

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

        if (inputSource == TiltInputSource.None)
        {
            Debug.LogWarning("SphereTiltForce: accelerometer is unavailable. Tilt force is disabled.");
            return;
        }

        if (calibrateOnEnable)
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

        Vector3 force = new Vector3(smoothedTilt.x, 0f, smoothedTilt.y) * Mathf.Max(0f, forceMultiplier);
        if (maxForce > 0f && force.magnitude > maxForce)
        {
            force = force.normalized * maxForce;
        }

        rb.AddForce(force, ForceMode.Acceleration);
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

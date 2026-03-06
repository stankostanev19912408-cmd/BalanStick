using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class CubePhoneMotion : MonoBehaviour
{
    private enum PhoneMotionBinding
    {
        PhoneX = 0,
        PhoneNegativeX = 1,
        PhoneY = 2,
        PhoneNegativeY = 3,
        PhoneZ = 4,
        PhoneNegativeZ = 5
    }

    [Header("Phone -> Cube Motion Mapping")]
    [SerializeField] private PhoneMotionBinding cubeXFromPhone = PhoneMotionBinding.PhoneX;
    [SerializeField] private PhoneMotionBinding cubeZFromPhone = PhoneMotionBinding.PhoneY;
    [SerializeField] private float cubeXMultiplier = 1f;
    [SerializeField] private float cubeZMultiplier = 1f;

    [Header("Acceleration -> Motion")]
    [SerializeField] private float accelerationScale = 9.81f;
    [SerializeField] private float accelerationSmoothing = 16f;
    [SerializeField] private float deadZoneAcceleration = 0.05f;

    [Header("Velocity Control")]
    [SerializeField] private float accelerationToSpeed = 1.1f;
    [SerializeField] private float velocityResponse = 20f;
    [SerializeField] private float velocityDamping = 1.5f;
    [SerializeField] private float stationaryVelocityDamping = 14f;
    [SerializeField] private float maxSpeed = 4f;
    [SerializeField] private float stationaryTimeToZeroVelocity = 0.05f;
    [SerializeField] private bool instantStopWhenPhoneIsStill = true;
    [SerializeField] private bool suppressReverseImpulse = true;
    [SerializeField] private float reverseImpulseDotThreshold = -0.15f;

    [Header("Stationary Detection")]
    [SerializeField] private float stationaryAccelThreshold = 0.06f;
    [SerializeField] private float stationaryGyroThresholdDegPerSec = 3f;

    [Header("Calibration")]
    [SerializeField] private int calibrationSamples = 60;
    [SerializeField] private bool requireStillCalibrationBeforeMotion = false;
    [SerializeField] private float calibrationMaxWaitSeconds = 1.25f;

    [Header("Bounds")]
    [SerializeField] private bool keepInitialY = true;
    [SerializeField] private float maxOffsetFromStart = 3f;

    [Header("Gyroscope Init")]
    [SerializeField] private float gyroInitializationRetryDuration = 1.5f;
    [SerializeField] private float gyroInitializationRetryInterval = 0.2f;
    [SerializeField] private bool warnWhenGyroscopeUnavailable = true;
    [SerializeField] private bool warnInEditor = false;

    private Rigidbody rb;

    private bool gyroscopeAvailable;
    private bool gyroscopeWarningLogged;
    private bool previousCompensateSensors;
    private Coroutine gyroInitializationRoutine;

    private Vector3 startPosition;
    private Vector3 defaultPosition;
    private Vector3 velocityWorld;
    private Vector3 smoothedAccelerationWorld;
    private Vector3 accelerationBiasWorld;

    private bool isCalibrated;
    private int collectedCalibrationSamples;
    private Vector3 calibrationAccelerationSumWorld;
    private float stationaryTimer;
    private float elapsedSinceEnable;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ConfigureRigidbody();
        defaultPosition = rb.position;

        previousCompensateSensors = Input.compensateSensors;
        Input.compensateSensors = false;
    }

    private void OnEnable()
    {
        Input.compensateSensors = false;
        startPosition = rb.position;
        ResetMotionState();
        elapsedSinceEnable = 0f;
        StartGyroscopeInitialization();
    }

    private void OnDisable()
    {
        if (gyroInitializationRoutine != null)
        {
            StopCoroutine(gyroInitializationRoutine);
            gyroInitializationRoutine = null;
        }

        Input.compensateSensors = previousCompensateSensors;
    }

    [ContextMenu("Recalibrate Motion")]
    public void RecalibrateMotion()
    {
        startPosition = rb.position;
        ResetMotionState();
    }

    [ContextMenu("Reset To Default Position")]
    public void ResetCubeToDefaultPosition()
    {
        rb.position = defaultPosition;
        startPosition = defaultPosition;
        ResetMotionState();
    }

    private void FixedUpdate()
    {
        if (!gyroscopeAvailable)
        {
            return;
        }

        float dt = Time.fixedDeltaTime;
        elapsedSinceEnable += dt;
        Vector3 linearAccelerationDeviceG = Input.gyro.userAcceleration;
        Vector3 linearAccelerationWorld = BuildMappedPlanarAcceleration(linearAccelerationDeviceG) * accelerationScale;

        if (keepInitialY)
        {
            linearAccelerationWorld.y = 0f;
        }

        bool stationaryNow = IsStationary(linearAccelerationDeviceG);
        if (!isCalibrated)
        {
            TryCollectCalibrationSample(linearAccelerationWorld, stationaryNow);
            bool calibrationTimedOut = calibrationMaxWaitSeconds > 0f && elapsedSinceEnable >= calibrationMaxWaitSeconds;
            if (requireStillCalibrationBeforeMotion && !calibrationTimedOut)
            {
                return;
            }

            if (collectedCalibrationSamples > 0)
            {
                accelerationBiasWorld = calibrationAccelerationSumWorld / collectedCalibrationSamples;
            }

            isCalibrated = true;
        }

        Vector3 correctedAccelerationWorld = linearAccelerationWorld - accelerationBiasWorld;
        float effectiveDeadZoneAcceleration = Mathf.Min(deadZoneAcceleration, stationaryAccelThreshold * 0.8f);
        float deadZoneMs2 = Mathf.Max(0f, effectiveDeadZoneAcceleration) * accelerationScale;
        if (correctedAccelerationWorld.sqrMagnitude < deadZoneMs2 * deadZoneMs2)
        {
            correctedAccelerationWorld = Vector3.zero;
        }

        float smoothingFactor = 1f - Mathf.Exp(-Mathf.Max(0.01f, accelerationSmoothing) * dt);
        smoothedAccelerationWorld = Vector3.Lerp(smoothedAccelerationWorld, correctedAccelerationWorld, smoothingFactor);

        Vector3 targetVelocityWorld = Vector3.zero;
        if (!stationaryNow && smoothedAccelerationWorld.sqrMagnitude > 0.0000001f)
        {
            targetVelocityWorld = smoothedAccelerationWorld * Mathf.Max(0f, accelerationToSpeed);

            if (suppressReverseImpulse && velocityWorld.sqrMagnitude > 0.0001f)
            {
                float alignment = Vector3.Dot(velocityWorld.normalized, targetVelocityWorld.normalized);
                if (alignment < reverseImpulseDotThreshold)
                {
                    targetVelocityWorld = Vector3.zero;
                }
            }
        }

        float responseFactor = 1f - Mathf.Exp(-Mathf.Max(0.01f, velocityResponse) * dt);
        velocityWorld = Vector3.Lerp(velocityWorld, targetVelocityWorld, responseFactor);
        velocityWorld *= Mathf.Exp(-Mathf.Max(0f, velocityDamping) * dt);

        if (stationaryNow)
        {
            stationaryTimer += dt;
            if (instantStopWhenPhoneIsStill || stationaryTimer >= stationaryTimeToZeroVelocity)
            {
                velocityWorld = Vector3.zero;
                smoothedAccelerationWorld = Vector3.zero;
            }
            else
            {
                velocityWorld *= Mathf.Exp(-Mathf.Max(0f, stationaryVelocityDamping) * dt);
            }
        }
        else
        {
            stationaryTimer = 0f;
        }

        float clampedMaxSpeed = Mathf.Max(0f, maxSpeed);
        if (velocityWorld.magnitude > clampedMaxSpeed)
        {
            velocityWorld = velocityWorld.normalized * clampedMaxSpeed;
        }

        Vector3 nextPosition = rb.position + velocityWorld * dt;
        if (keepInitialY)
        {
            nextPosition.y = startPosition.y;
        }

        if (maxOffsetFromStart > 0f)
        {
            Vector3 offset = nextPosition - startPosition;
            if (keepInitialY)
            {
                offset.y = 0f;
            }

            float maxOffset = Mathf.Max(0f, maxOffsetFromStart);
            if (offset.magnitude > maxOffset)
            {
                Vector3 clampedOffset = offset.normalized * maxOffset;
                nextPosition = startPosition + clampedOffset;
                if (keepInitialY)
                {
                    nextPosition.y = startPosition.y;
                }

                velocityWorld = Vector3.ProjectOnPlane(velocityWorld, clampedOffset.normalized);
            }
        }

        rb.MovePosition(nextPosition);
    }

    private bool IsStationary(Vector3 linearAccelerationDeviceG)
    {
        float accelMagnitude = linearAccelerationDeviceG.magnitude;
        float gyroDegPerSec = Input.gyro.rotationRateUnbiased.magnitude * Mathf.Rad2Deg;
        return accelMagnitude <= stationaryAccelThreshold && gyroDegPerSec <= stationaryGyroThresholdDegPerSec;
    }

    private void TryCollectCalibrationSample(Vector3 linearAccelerationWorld, bool stationaryNow)
    {
        if (!stationaryNow)
        {
            collectedCalibrationSamples = 0;
            calibrationAccelerationSumWorld = Vector3.zero;
            return;
        }

        calibrationAccelerationSumWorld += linearAccelerationWorld;
        collectedCalibrationSamples++;
        if (collectedCalibrationSamples < Mathf.Max(1, calibrationSamples))
        {
            return;
        }

        accelerationBiasWorld = calibrationAccelerationSumWorld / collectedCalibrationSamples;
        isCalibrated = true;
    }

    private void ResetMotionState()
    {
        velocityWorld = Vector3.zero;
        smoothedAccelerationWorld = Vector3.zero;
        accelerationBiasWorld = Vector3.zero;
        stationaryTimer = 0f;
        elapsedSinceEnable = 0f;

        isCalibrated = false;
        collectedCalibrationSamples = 0;
        calibrationAccelerationSumWorld = Vector3.zero;
    }

    private Vector3 BuildMappedPlanarAcceleration(Vector3 phoneAcceleration)
    {
        float mappedX = ResolveBinding(phoneAcceleration, cubeXFromPhone) * cubeXMultiplier;
        float mappedZ = ResolveBinding(phoneAcceleration, cubeZFromPhone) * cubeZMultiplier;
        return new Vector3(mappedX, 0f, mappedZ);
    }

    private static float ResolveBinding(Vector3 phoneAcceleration, PhoneMotionBinding binding)
    {
        switch (binding)
        {
            case PhoneMotionBinding.PhoneX:
                return phoneAcceleration.x;
            case PhoneMotionBinding.PhoneNegativeX:
                return -phoneAcceleration.x;
            case PhoneMotionBinding.PhoneY:
                return phoneAcceleration.y;
            case PhoneMotionBinding.PhoneNegativeY:
                return -phoneAcceleration.y;
            case PhoneMotionBinding.PhoneZ:
                return phoneAcceleration.z;
            case PhoneMotionBinding.PhoneNegativeZ:
                return -phoneAcceleration.z;
            default:
                return 0f;
        }
    }

    private void ConfigureRigidbody()
    {
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private void StartGyroscopeInitialization()
    {
        if (gyroInitializationRoutine != null)
        {
            StopCoroutine(gyroInitializationRoutine);
            gyroInitializationRoutine = null;
        }

        gyroscopeAvailable = false;
        if (TryEnableGyroscope())
        {
            return;
        }

        float retryDuration = Mathf.Max(0f, gyroInitializationRetryDuration);
        if (retryDuration <= 0f)
        {
            LogGyroscopeUnavailableIfNeeded();
            return;
        }

        gyroInitializationRoutine = StartCoroutine(InitializeGyroscopeWithRetry(retryDuration));
    }

    private IEnumerator InitializeGyroscopeWithRetry(float retryDuration)
    {
        float elapsed = 0f;
        float retryInterval = Mathf.Max(0.02f, gyroInitializationRetryInterval);

        while (elapsed < retryDuration && !gyroscopeAvailable)
        {
            yield return new WaitForSecondsRealtime(retryInterval);
            elapsed += retryInterval;
            TryEnableGyroscope();
        }

        if (!gyroscopeAvailable)
        {
            LogGyroscopeUnavailableIfNeeded();
        }

        gyroInitializationRoutine = null;
    }

    private bool TryEnableGyroscope()
    {
        if (!SystemInfo.supportsGyroscope)
        {
            gyroscopeAvailable = false;
            return false;
        }

        Input.gyro.enabled = true;
        gyroscopeAvailable = Input.gyro.enabled;
        return gyroscopeAvailable;
    }

    private void LogGyroscopeUnavailableIfNeeded()
    {
        if (!warnWhenGyroscopeUnavailable || gyroscopeWarningLogged)
        {
            return;
        }

        bool shouldWarnInThisEnvironment = Application.isMobilePlatform || (warnInEditor && Application.isEditor);
        if (!shouldWarnInThisEnvironment)
        {
            return;
        }

        Debug.LogWarning("CubePhoneMotion: gyroscope is not available. Real-space phone motion tracking cannot run.");
        gyroscopeWarningLogged = true;
    }
}

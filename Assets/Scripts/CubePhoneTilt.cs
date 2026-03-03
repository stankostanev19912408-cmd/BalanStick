using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CubePhoneTilt : MonoBehaviour
{
    private enum PhoneRotationBinding
    {
        None = 0,
        PhoneX = 1,
        PhoneNegativeX = 2,
        PhoneY = 3,
        PhoneNegativeY = 4,
        PhoneZ = 5,
        PhoneNegativeZ = 6
    }

    [Header("Phone -> Cube Rotation Mapping")]
    [SerializeField] private PhoneRotationBinding cubeXFromPhone = PhoneRotationBinding.PhoneNegativeX;
    [SerializeField] private PhoneRotationBinding cubeYFromPhone = PhoneRotationBinding.PhoneNegativeY;
    [SerializeField] private PhoneRotationBinding cubeZFromPhone = PhoneRotationBinding.PhoneZ;

    [Header("Rotation Constraints")]
    [SerializeField] private bool lockRotationAroundY;

    [Header("Axis Multipliers")]
    [SerializeField] private float xRotationMultiplier = 1f;
    [SerializeField] private float yRotationMultiplier = 1f;
    [SerializeField] private float zRotationMultiplier = 1f;

    [Header("Stabilization")]
    [SerializeField] private float rotationSmoothing = 20f;
    [SerializeField] private float deadZoneDegrees = 0.15f;

    private Rigidbody platformRigidbody;

    private bool gyroscopeAvailable;
    private bool referenceCaptured;
    private bool previousCompensateSensors;
    private Quaternion cubeToPhoneRotation = Quaternion.identity;
    private Quaternion referenceMappedPhonePose = Quaternion.identity;
    private Quaternion referenceCubeRotation = Quaternion.identity;
    private float lockedYawDegrees;
    private bool wasYLockEnabled;
    private Quaternion filteredTargetWorldRotation = Quaternion.identity;

    private void Awake()
    {
        platformRigidbody = GetComponent<Rigidbody>();
        ConfigurePlatformRigidbody();
        RebuildAxisMapping();

        previousCompensateSensors = Input.compensateSensors;
        Input.compensateSensors = false;
        gyroscopeAvailable = SystemInfo.supportsGyroscope;
        if (gyroscopeAvailable)
        {
            Input.gyro.enabled = true;
        }
        else
        {
            Debug.LogWarning("CubePhoneTilt: gyroscope is not available on this device. Exact 3D phone rotation sync is unavailable.");
        }
    }

    private void OnEnable()
    {
        lockedYawDegrees = transform.rotation.eulerAngles.y;
        wasYLockEnabled = lockRotationAroundY;
        filteredTargetWorldRotation = platformRigidbody != null ? platformRigidbody.rotation : transform.rotation;
        Recalibrate();
    }

    private void OnDestroy()
    {
        Input.compensateSensors = previousCompensateSensors;
    }

    [ContextMenu("Recalibrate Tilt")]
    public void Recalibrate()
    {
        if (!gyroscopeAvailable)
        {
            return;
        }

        RebuildAxisMapping();
        Quaternion deviceWorld = GetGyroAttitudeUnity();
        Quaternion phonePose = BuildPhonePose(deviceWorld);
        Quaternion mappedPhonePose = phonePose * cubeToPhoneRotation;
        referenceMappedPhonePose = mappedPhonePose;
        referenceCubeRotation = transform.rotation;
        if (lockRotationAroundY)
        {
            lockedYawDegrees = transform.rotation.eulerAngles.y;
        }

        filteredTargetWorldRotation = platformRigidbody.rotation;
        referenceCaptured = true;
    }

    private void FixedUpdate()
    {
        if (!gyroscopeAvailable)
        {
            return;
        }

        Quaternion currentDeviceWorld = GetGyroAttitudeUnity();
        if (!referenceCaptured)
        {
            Recalibrate();
            return;
        }

        Quaternion phonePose = BuildPhonePose(currentDeviceWorld);
        Quaternion mappedPhonePose = phonePose * cubeToPhoneRotation;
        Quaternion phoneDelta = Quaternion.Inverse(referenceMappedPhonePose) * mappedPhonePose;
        Vector3 deltaEuler = NormalizeEuler(phoneDelta.eulerAngles);
        Vector3 scaledDeltaEuler = new Vector3(
            deltaEuler.x * xRotationMultiplier,
            deltaEuler.y * yRotationMultiplier,
            deltaEuler.z * zRotationMultiplier);
        Quaternion scaledDelta = Quaternion.Euler(scaledDeltaEuler);
        Quaternion targetWorldRotation = referenceCubeRotation * scaledDelta;

        if (lockRotationAroundY && !wasYLockEnabled)
        {
            lockedYawDegrees = platformRigidbody.rotation.eulerAngles.y;
        }

        if (lockRotationAroundY)
        {
            Vector3 targetEuler = targetWorldRotation.eulerAngles;
            targetWorldRotation = Quaternion.Euler(targetEuler.x, lockedYawDegrees, targetEuler.z);
        }

        if (Quaternion.Angle(filteredTargetWorldRotation, targetWorldRotation) <= deadZoneDegrees)
        {
            targetWorldRotation = filteredTargetWorldRotation;
        }
        else
        {
            filteredTargetWorldRotation = targetWorldRotation;
        }

        float smoothingFactor = 1f - Mathf.Exp(-Mathf.Max(0.01f, rotationSmoothing) * Time.fixedDeltaTime);
        Quaternion smoothedWorldRotation = Quaternion.Slerp(platformRigidbody.rotation, targetWorldRotation, smoothingFactor);

        wasYLockEnabled = lockRotationAroundY;
        platformRigidbody.MoveRotation(smoothedWorldRotation);
    }

    private static Vector3 NormalizeEuler(Vector3 euler)
    {
        return new Vector3(
            Mathf.DeltaAngle(0f, euler.x),
            Mathf.DeltaAngle(0f, euler.y),
            Mathf.DeltaAngle(0f, euler.z));
    }

    private static Vector3 BindingToPhoneAxis(PhoneRotationBinding binding)
    {
        switch (binding)
        {
            case PhoneRotationBinding.PhoneX:
                return Vector3.right;
            case PhoneRotationBinding.PhoneNegativeX:
                return Vector3.left;
            case PhoneRotationBinding.PhoneY:
                return Vector3.up;
            case PhoneRotationBinding.PhoneNegativeY:
                return Vector3.down;
            case PhoneRotationBinding.PhoneZ:
                return Vector3.forward;
            case PhoneRotationBinding.PhoneNegativeZ:
                return Vector3.back;
            default:
                return Vector3.zero;
        }
    }

    private void RebuildAxisMapping()
    {
        Vector3 xInPhone = BindingToPhoneAxis(cubeXFromPhone);
        Vector3 yInPhone = BindingToPhoneAxis(cubeYFromPhone);
        Vector3 zInPhone = BindingToPhoneAxis(cubeZFromPhone);

        if (xInPhone == Vector3.zero || yInPhone == Vector3.zero || zInPhone == Vector3.zero)
        {
            Debug.LogWarning("CubePhoneTilt: axis mapping contains None. Falling back to identity mapping.");
            cubeToPhoneRotation = Quaternion.identity;
            return;
        }

        float orthogonalityEpsilon = 0.001f;
        if (Mathf.Abs(Vector3.Dot(xInPhone, yInPhone)) > orthogonalityEpsilon
            || Mathf.Abs(Vector3.Dot(xInPhone, zInPhone)) > orthogonalityEpsilon
            || Mathf.Abs(Vector3.Dot(yInPhone, zInPhone)) > orthogonalityEpsilon)
        {
            Debug.LogWarning("CubePhoneTilt: axis mapping is not orthogonal. Falling back to identity mapping.");
            cubeToPhoneRotation = Quaternion.identity;
            return;
        }

        Quaternion candidate = Quaternion.LookRotation(zInPhone, yInPhone);
        Vector3 reconstructedX = candidate * Vector3.right;
        if (Vector3.Dot(reconstructedX, xInPhone) < 0.999f)
        {
            Debug.LogWarning("CubePhoneTilt: axis mapping is left-handed or inconsistent. Falling back to identity mapping.");
            cubeToPhoneRotation = Quaternion.identity;
            return;
        }

        cubeToPhoneRotation = candidate;
    }

    private static Quaternion BuildPhonePose(Quaternion deviceWorld)
    {
        Vector3 phoneTopWorld = deviceWorld * Vector3.up;
        Vector3 phoneNormalWorld = deviceWorld * Vector3.forward;
        return Quaternion.LookRotation(phoneTopWorld, phoneNormalWorld);
    }

    private static Quaternion GetGyroAttitudeUnity()
    {
        Quaternion q = Input.gyro.attitude;
        Quaternion converted = new Quaternion(q.x, q.y, -q.z, -q.w);
        return GetScreenOrientationCorrection() * converted;
    }

    private static Quaternion GetScreenOrientationCorrection()
    {
        switch (Screen.orientation)
        {
            case ScreenOrientation.PortraitUpsideDown:
                return Quaternion.Euler(0f, 0f, 180f);
            case ScreenOrientation.LandscapeLeft:
                return Quaternion.Euler(0f, 0f, -90f);
            case ScreenOrientation.LandscapeRight:
                return Quaternion.Euler(0f, 0f, 90f);
            default:
                return Quaternion.identity;
        }
    }

    private void ConfigurePlatformRigidbody()
    {
        platformRigidbody.isKinematic = true;
        platformRigidbody.useGravity = false;
        platformRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        platformRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        platformRigidbody.constraints = RigidbodyConstraints.FreezePositionX
            | RigidbodyConstraints.FreezePositionY
            | RigidbodyConstraints.FreezePositionZ;
    }
}

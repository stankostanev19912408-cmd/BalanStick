using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CubePhoneTilt : MonoBehaviour
{
    [SerializeField] private bool invertX;
    [SerializeField] private bool invertZ;
    [SerializeField] private float xOffsetDegrees;
    [SerializeField] private float zOffsetDegrees;
    [SerializeField] private float gravitySmoothing = 14f;
    [SerializeField] private float rotationSmoothing = 18f;
    [SerializeField] private float deadZone = 0.015f;

    private Quaternion initialLocalRotation;
    private Vector3 filteredGravity;
    private Rigidbody platformRigidbody;

    private void Awake()
    {
        initialLocalRotation = transform.localRotation;
        platformRigidbody = GetComponent<Rigidbody>();
        ConfigurePlatformRigidbody();

        Vector3 initialGravity = Input.acceleration;
        filteredGravity = initialGravity.sqrMagnitude > 0.0001f ? initialGravity.normalized : Vector3.back;
    }

    private void FixedUpdate()
    {
        Vector3 rawGravity = Input.acceleration;
        if (rawGravity.sqrMagnitude < 0.0001f)
        {
            return;
        }

        rawGravity.Normalize();

        float gravityLerp = 1f - Mathf.Exp(-Mathf.Max(0.01f, gravitySmoothing) * Time.fixedDeltaTime);
        filteredGravity = Vector3.Slerp(filteredGravity, rawGravity, gravityLerp).normalized;

        Vector3 gravity = filteredGravity;
        if (Mathf.Abs(gravity.x) < deadZone) gravity.x = 0f;
        if (Mathf.Abs(gravity.y) < deadZone) gravity.y = 0f;
        if (Mathf.Abs(gravity.z) < deadZone) gravity.z = 0f;
        if (gravity.sqrMagnitude < 0.0001f)
        {
            return;
        }

        // Angles relative to the phone lying flat on a horizontal surface.
        float tiltX = Mathf.Atan2(gravity.y, -gravity.z) * Mathf.Rad2Deg;
        float tiltZ = Mathf.Atan2(-gravity.x, -gravity.z) * Mathf.Rad2Deg;

        if (invertX)
        {
            tiltX = -tiltX;
        }

        if (invertZ)
        {
            tiltZ = -tiltZ;
        }

        tiltX -= xOffsetDegrees;
        tiltZ -= zOffsetDegrees;

        Quaternion targetLocalRotation = initialLocalRotation * Quaternion.Euler(tiltX, 0f, tiltZ);
        Quaternion targetWorldRotation = transform.parent != null
            ? transform.parent.rotation * targetLocalRotation
            : targetLocalRotation;

        float rotationLerp = 1f - Mathf.Exp(-Mathf.Max(0.01f, rotationSmoothing) * Time.fixedDeltaTime);
        Quaternion smoothedWorldRotation = Quaternion.Slerp(platformRigidbody.rotation, targetWorldRotation, rotationLerp);
        platformRigidbody.MoveRotation(smoothedWorldRotation);
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

using UnityEngine;

public class SphereForce : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField, Min(1f)] private float xMult, zMult;
    [SerializeField, Min(0f)] private float deadZoneXY = 0.03f;
    [SerializeField, Min(0.01f)] private float smoothing = 14f;
    [SerializeField, Min(0f)] private float brakeDamping = 10f;
    [SerializeField, Min(0.01f)] private float logIntervalSeconds = 0.2f;

    private bool gyroscopeAvailable;
    private Vector2 filteredAccelerationXY;
    private float nextLogTime;

    private void OnEnable()
    {
        gyroscopeAvailable = TryEnableGyroscope();
        filteredAccelerationXY = Vector2.zero;
        nextLogTime = 0f;

        if (!gyroscopeAvailable)
        {
            Debug.LogWarning("SphereForce: gyroscope is unavailable. Force input is disabled.");
        }
    }

    private void FixedUpdate()
    {
        if (!gyroscopeAvailable || rb == null)
        {
            return;
        }

        Vector3 userAcceleration = Input.gyro.userAcceleration;
        Vector2 inputXY = new Vector2(userAcceleration.x, userAcceleration.y);
        float deadZone = Mathf.Max(0f, deadZoneXY);
        bool isInDeadZone = inputXY.sqrMagnitude < deadZone * deadZone;
        if (isInDeadZone)
        {
            inputXY = Vector2.zero;
            ApplyBraking();
        }

        float lerpFactor = 1f - Mathf.Exp(-Mathf.Max(0.01f, smoothing) * Time.fixedDeltaTime);
        filteredAccelerationXY = Vector2.Lerp(filteredAccelerationXY, inputXY, lerpFactor);

        if (filteredAccelerationXY.sqrMagnitude < deadZone * deadZone)
        {
            filteredAccelerationXY = Vector2.zero;
        }

        Vector3 force = new Vector3(filteredAccelerationXY.x * xMult, 0f, filteredAccelerationXY.y * zMult);
        rb.AddForce(force, ForceMode.Acceleration);

        if (Time.unscaledTime >= nextLogTime)
        {
            float magnitudeXY = filteredAccelerationXY.magnitude;
            Debug.Log($"SphereForce filtered xy (g): x={filteredAccelerationXY.x:F3}, y={filteredAccelerationXY.y:F3}, |xy|={magnitudeXY:F3}");
            nextLogTime = Time.unscaledTime + logIntervalSeconds;
        }
    }

    private void ApplyBraking()
    {
        if (brakeDamping <= 0f)
        {
            return;
        }

        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (horizontalVelocity.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        float dampingFactor = Mathf.Exp(-brakeDamping * Time.fixedDeltaTime);
        Vector3 dampedHorizontalVelocity = horizontalVelocity * dampingFactor;
        rb.velocity = new Vector3(dampedHorizontalVelocity.x, rb.velocity.y, dampedHorizontalVelocity.z);
    }

    private static bool TryEnableGyroscope()
    {
        if (!SystemInfo.supportsGyroscope)
        {
            return false;
        }

        Input.gyro.enabled = true;
        return Input.gyro.enabled;
    }
}

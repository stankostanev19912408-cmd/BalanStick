using UnityEngine;

public class PhoneUserAccelerationLogger : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float minAccMagnitudeXY = 0.5f;

    private bool gyroscopeAvailable;

    private void OnEnable()
    {
        gyroscopeAvailable = TryEnableGyroscope();

        if (!gyroscopeAvailable)
        {
            Debug.LogWarning("PhoneUserAccelerationLogger: gyroscope is unavailable. userAcceleration logs are disabled.");
        }
    }

    private void Update()
    {
        if (!gyroscopeAvailable)
            return;

        Vector3 userAcceleration = Input.gyro.userAcceleration;
        Vector2 userAccelerationXY = new Vector2(userAcceleration.x, userAcceleration.y);
        float magnitudeXY = userAccelerationXY.magnitude;
        if (magnitudeXY < minAccMagnitudeXY)
            return;
        Debug.Log($"PhoneUserAccelerationLogger userAcceleration XY (g): x={userAcceleration.x:F3}, y={userAcceleration.y:F3}, |xy|={magnitudeXY:F3}");
    }

    private static bool TryEnableGyroscope()
    {
        if (!SystemInfo.supportsGyroscope)
            return false;

        Input.gyro.enabled = true;
        return Input.gyro.enabled;
    }
}

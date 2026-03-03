using UnityEngine;

public class CameraRootYawFromCube : MonoBehaviour
{
    [SerializeField] private Transform cubeTransform;

    private void LateUpdate()
    {
        if (cubeTransform == null)
        {
            return;
        }

        Vector3 cubeForwardOnPlane = Vector3.ProjectOnPlane(cubeTransform.forward, Vector3.up);
        if (cubeForwardOnPlane.sqrMagnitude < 0.000001f)
        {
            return;
        }

        cubeForwardOnPlane.Normalize();
        float yaw = Mathf.Atan2(cubeForwardOnPlane.x, cubeForwardOnPlane.z) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}

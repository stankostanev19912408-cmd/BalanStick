using UnityEngine;

public class CameraRootFollowCube : MonoBehaviour
{
    [SerializeField] private Transform cubeTransform;
    [SerializeField] private bool followYawAroundY = true;
    [SerializeField] private bool followPositionXZ = true;

    private Vector2 xzOffsetFromCube;
    private bool wasFollowingPositionXZ;

    private void OnEnable()
    {
        CaptureOffsetFromCube();
        wasFollowingPositionXZ = followPositionXZ;
    }

    private void LateUpdate()
    {
        if (cubeTransform == null)
        {
            return;
        }

        if (followYawAroundY)
        {
            Vector3 cubeForwardOnPlane = Vector3.ProjectOnPlane(cubeTransform.forward, Vector3.up);
            if (cubeForwardOnPlane.sqrMagnitude >= 0.000001f)
            {
                cubeForwardOnPlane.Normalize();
                float yaw = Mathf.Atan2(cubeForwardOnPlane.x, cubeForwardOnPlane.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }

        if (followPositionXZ && !wasFollowingPositionXZ)
        {
            CaptureOffsetFromCube();
        }

        if (followPositionXZ)
        {
            Vector3 currentPosition = transform.position;
            Vector3 cubePosition = cubeTransform.position;
            transform.position = new Vector3(
                cubePosition.x + xzOffsetFromCube.x,
                currentPosition.y,
                cubePosition.z + xzOffsetFromCube.y);
        }

        wasFollowingPositionXZ = followPositionXZ;
    }

    private void CaptureOffsetFromCube()
    {
        if (cubeTransform == null)
        {
            return;
        }

        Vector3 currentPosition = transform.position;
        Vector3 cubePosition = cubeTransform.position;
        xzOffsetFromCube = new Vector2(currentPosition.x - cubePosition.x, currentPosition.z - cubePosition.z);
    }
}

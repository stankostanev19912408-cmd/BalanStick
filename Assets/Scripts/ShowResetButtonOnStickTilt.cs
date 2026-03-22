using UnityEngine;

public class ShowResetButtonOnStickTilt : MonoBehaviour
{
    [SerializeField] private Transform stickTransform;
    [SerializeField] private Transform cubeTransform;
    [SerializeField] private GameObject resetButtonObject;
    [SerializeField] private float showButtonAngleDegrees = 25f;

    private void OnEnable()
    {
        UpdateButtonVisibility();
    }

    private void Update()
    {
        UpdateButtonVisibility();
    }

    private void UpdateButtonVisibility()
    {
        if (stickTransform == null || resetButtonObject == null)
        {
            return;
        }

        if (!stickTransform.gameObject.activeInHierarchy)
        {
            if (resetButtonObject.activeSelf)
            {
                resetButtonObject.SetActive(false);
            }

            return;
        }

        float stickTilt = Vector3.Angle(stickTransform.up, Vector3.up);
        bool isTiltOverLimit = stickTilt > showButtonAngleDegrees;
        bool isStickBelowCube = cubeTransform != null && GetWorldCenterY(stickTransform) < GetWorldCenterY(cubeTransform);
        bool shouldShowButton = isTiltOverLimit || isStickBelowCube;
        if (resetButtonObject.activeSelf != shouldShowButton)
        {
            resetButtonObject.SetActive(shouldShowButton);
        }
    }

    private static float GetWorldCenterY(Transform target)
    {
        Collider targetCollider = target.GetComponent<Collider>();
        if (targetCollider != null)
        {
            return targetCollider.bounds.center.y;
        }

        Renderer targetRenderer = target.GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            return targetRenderer.bounds.center.y;
        }

        return target.position.y;
    }
}

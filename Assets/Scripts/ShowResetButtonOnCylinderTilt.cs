using UnityEngine;

public class ShowResetButtonOnCylinderTilt : MonoBehaviour
{
    [SerializeField] private Transform cylinderTransform;
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
        if (cylinderTransform == null || resetButtonObject == null)
        {
            return;
        }

        if (!cylinderTransform.gameObject.activeInHierarchy)
        {
            if (resetButtonObject.activeSelf)
            {
                resetButtonObject.SetActive(false);
            }

            return;
        }

        float cylinderTilt = Vector3.Angle(cylinderTransform.up, Vector3.up);
        bool isTiltOverLimit = cylinderTilt > showButtonAngleDegrees;
        bool isCylinderBelowCube = cubeTransform != null && GetWorldCenterY(cylinderTransform) < GetWorldCenterY(cubeTransform);
        bool shouldShowButton = isTiltOverLimit || isCylinderBelowCube;
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

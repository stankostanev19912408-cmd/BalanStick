using UnityEngine;
using UnityEngine.UI;

public class CylinderResetUI : MonoBehaviour
{
    [SerializeField] private Button resetButton;

    private Vector3 defaultPosition;
    private Quaternion defaultRotation;
    private Rigidbody cachedRigidbody;

    private void Awake()
    {
        defaultPosition = transform.position;
        defaultRotation = transform.rotation;
        cachedRigidbody = GetComponent<Rigidbody>();
        ConfigureCylinderRigidbody();
    }

    private void Start()
    {
        BindButton();
    }

    private void OnDestroy()
    {
        UnbindButton();
    }

    public void ResetCylinder()
    {
        transform.SetPositionAndRotation(defaultPosition, defaultRotation);

        if (cachedRigidbody == null)
        {
            return;
        }

        cachedRigidbody.velocity = Vector3.zero;
        cachedRigidbody.angularVelocity = Vector3.zero;
        cachedRigidbody.WakeUp();
    }

    private void BindButton()
    {
        if (resetButton == null)
        {
            return;
        }

        resetButton.onClick.RemoveListener(ResetCylinder);
        resetButton.onClick.AddListener(ResetCylinder);
    }

    private void UnbindButton()
    {
        if (resetButton == null)
        {
            return;
        }

        resetButton.onClick.RemoveListener(ResetCylinder);
    }

    private void ConfigureCylinderRigidbody()
    {
        if (cachedRigidbody == null)
        {
            return;
        }

        cachedRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        cachedRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        cachedRigidbody.sleepThreshold = 0f;
        cachedRigidbody.solverIterations = 12;
        cachedRigidbody.solverVelocityIterations = 12;
    }
}

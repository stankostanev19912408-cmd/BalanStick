using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CylinderTiltForce))]
public class CylinderRetryButtonUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cylinderTransform;
    [SerializeField] private Rigidbody cylinderRigidbody;
    [SerializeField] private CylinderTiltForce cylinderTiltForce;
    [SerializeField] private GameObject retryButtonObject;
    [SerializeField] private Button retryButton;
    [SerializeField] private GameObject startHintObject;

    private void Reset()
    {
        cylinderTransform = transform;
        cylinderRigidbody = GetComponent<Rigidbody>();
        cylinderTiltForce = GetComponent<CylinderTiltForce>();
    }

    private void Awake()
    {
        if (cylinderTransform == null)
        {
            cylinderTransform = transform;
        }

        if (cylinderRigidbody == null)
        {
            cylinderRigidbody = GetComponent<Rigidbody>();
        }

        if (cylinderTiltForce == null)
        {
            cylinderTiltForce = GetComponent<CylinderTiltForce>();
        }
    }

    private void OnEnable()
    {
        BindButton();
        BindTiltForceEvents();
        SetRetryVisible(cylinderTiltForce != null && cylinderTiltForce.IsRetryRequired);
        SetStartHintVisible(cylinderTiltForce != null && !cylinderTiltForce.IsInputUnlocked);
    }

    private void OnDisable()
    {
        UnbindTiltForceEvents();
        UnbindButton();
    }

    public void ResetCylinderRotation()
    {
        if (cylinderTransform == null)
        {
            return;
        }

        Quaternion zeroRotation = Quaternion.identity;
        if (cylinderRigidbody != null)
        {
            cylinderRigidbody.position = Vector3.up;
            cylinderRigidbody.rotation = zeroRotation;
            cylinderRigidbody.velocity = Vector3.zero;
            cylinderRigidbody.angularVelocity = Vector3.zero;
            cylinderRigidbody.WakeUp();
        }
        else
        {
            cylinderTransform.position = Vector3.up;
            cylinderTransform.rotation = zeroRotation;
        }

        if (cylinderTiltForce != null)
        {
            cylinderTiltForce.ClearRetryRequirement();
        }
        else
        {
            SetRetryVisible(false);
        }
    }

    private void BindButton()
    {
        if (retryButton == null)
        {
            return;
        }

        retryButton.onClick.RemoveListener(ResetCylinderRotation);
        retryButton.onClick.AddListener(ResetCylinderRotation);
    }

    private void UnbindButton()
    {
        if (retryButton == null)
        {
            return;
        }

        retryButton.onClick.RemoveListener(ResetCylinderRotation);
    }

    private void BindTiltForceEvents()
    {
        if (cylinderTiltForce == null)
        {
            return;
        }

        cylinderTiltForce.RetryStateChanged -= HandleRetryStateChanged;
        cylinderTiltForce.RetryStateChanged += HandleRetryStateChanged;
        cylinderTiltForce.StartGateStateChanged -= HandleStartGateStateChanged;
        cylinderTiltForce.StartGateStateChanged += HandleStartGateStateChanged;
    }

    private void UnbindTiltForceEvents()
    {
        if (cylinderTiltForce == null)
        {
            return;
        }

        cylinderTiltForce.RetryStateChanged -= HandleRetryStateChanged;
        cylinderTiltForce.StartGateStateChanged -= HandleStartGateStateChanged;
    }

    private void HandleRetryStateChanged(bool isRetryRequired)
    {
        SetRetryVisible(isRetryRequired);
    }

    private void HandleStartGateStateChanged(bool isInputUnlocked)
    {
        SetStartHintVisible(!isInputUnlocked);
    }

    private void SetRetryVisible(bool isVisible)
    {
        if (retryButtonObject == null)
        {
            return;
        }

        if (retryButtonObject.activeSelf != isVisible)
        {
            retryButtonObject.SetActive(isVisible);
        }
    }

    private void SetStartHintVisible(bool isVisible)
    {
        if (startHintObject == null)
        {
            return;
        }

        if (startHintObject.activeSelf != isVisible)
        {
            startHintObject.SetActive(isVisible);
        }
    }
}

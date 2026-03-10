using UnityEngine;
using UnityEngine.UI;

public class CylinderRetryButtonUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CylinderRetryController cylinderRetryController;
    [SerializeField] private CylinderTiltForce cylinderTiltForce;
    [SerializeField] private GameObject retryButtonObject;
    [SerializeField] private Button retryButton;
    [SerializeField] private GameObject startHintObject;

    private void Awake()
    {
        if (cylinderRetryController == null)
        {
            Debug.LogWarning("CylinderRetryButtonUI: cylinderRetryController is not assigned.", this);
        }

        if (cylinderTiltForce == null)
        {
            Debug.LogWarning("CylinderRetryButtonUI: cylinderTiltForce is not assigned.", this);
        }

        if (retryButtonObject == null)
        {
            Debug.LogWarning("CylinderRetryButtonUI: retryButtonObject is not assigned.", this);
        }

        if (retryButton == null)
        {
            Debug.LogWarning("CylinderRetryButtonUI: retryButton is not assigned.", this);
        }

        if (startHintObject == null)
        {
            Debug.LogWarning("CylinderRetryButtonUI: startHintObject is not assigned.", this);
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

    private void HandleRetryButtonClicked()
    {
        if (cylinderRetryController == null)
        {
            return;
        }

        cylinderRetryController.ResetCylinderRotation();
    }

    private void BindButton()
    {
        if (retryButton == null)
        {
            return;
        }

        retryButton.onClick.RemoveListener(HandleRetryButtonClicked);
        retryButton.onClick.AddListener(HandleRetryButtonClicked);
    }

    private void UnbindButton()
    {
        if (retryButton == null)
        {
            return;
        }

        retryButton.onClick.RemoveListener(HandleRetryButtonClicked);
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

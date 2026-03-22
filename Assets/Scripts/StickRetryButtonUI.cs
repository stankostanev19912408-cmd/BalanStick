using UnityEngine;
using UnityEngine.UI;

public class StickRetryButtonUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StickRetryController stickRetryController;
    [SerializeField] private StickTiltForce stickTiltForce;
    [SerializeField] private GameObject retryButtonObject;
    [SerializeField] private Button retryButton;
    [SerializeField] private GameObject startHintObject;

    private void Awake()
    {
        if (stickRetryController == null)
        {
            Debug.LogWarning("StickRetryButtonUI: stickRetryController is not assigned.", this);
        }

        if (stickTiltForce == null)
        {
            Debug.LogWarning("StickRetryButtonUI: stickTiltForce is not assigned.", this);
        }

        if (retryButtonObject == null)
        {
            Debug.LogWarning("StickRetryButtonUI: retryButtonObject is not assigned.", this);
        }

        if (retryButton == null)
        {
            Debug.LogWarning("StickRetryButtonUI: retryButton is not assigned.", this);
        }

        if (startHintObject == null)
        {
            Debug.LogWarning("StickRetryButtonUI: startHintObject is not assigned.", this);
        }
    }

    private void OnEnable()
    {
        BindButton();
        BindTiltForceEvents();
        SetRetryVisible(stickTiltForce != null && stickTiltForce.IsRetryRequired);
        SetStartHintVisible(stickTiltForce != null && !stickTiltForce.IsInputUnlocked);
    }

    private void OnDisable()
    {
        UnbindTiltForceEvents();
        UnbindButton();
    }

    private void HandleRetryButtonClicked()
    {
        if (stickRetryController == null)
        {
            return;
        }

        stickRetryController.ResetStickRotation();
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
        if (stickTiltForce == null)
        {
            return;
        }

        stickTiltForce.RetryStateChanged -= HandleRetryStateChanged;
        stickTiltForce.RetryStateChanged += HandleRetryStateChanged;
        stickTiltForce.StartGateStateChanged -= HandleStartGateStateChanged;
        stickTiltForce.StartGateStateChanged += HandleStartGateStateChanged;
    }

    private void UnbindTiltForceEvents()
    {
        if (stickTiltForce == null)
        {
            return;
        }

        stickTiltForce.RetryStateChanged -= HandleRetryStateChanged;
        stickTiltForce.StartGateStateChanged -= HandleStartGateStateChanged;
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

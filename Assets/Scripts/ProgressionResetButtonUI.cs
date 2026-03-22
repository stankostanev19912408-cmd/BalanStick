using UnityEngine;
using UnityEngine.UI;

public class ProgressionResetButtonUI : MonoBehaviour
{
    [SerializeField] private Button resetButton;
    [SerializeField] private ProgressionManager progressionManager;

    private void Awake()
    {
        if (resetButton == null)
        {
            Debug.LogWarning("ProgressionResetButtonUI: resetButton is not assigned.", this);
        }

        if (progressionManager == null)
        {
            Debug.LogWarning("ProgressionResetButtonUI: progressionManager is not assigned.", this);
        }
    }

    private void OnEnable()
    {
        BindButton();
    }

    private void OnDisable()
    {
        UnbindButton();
    }

    private void BindButton()
    {
        if (resetButton == null)
        {
            return;
        }

        resetButton.onClick.RemoveListener(HandleResetButtonClicked);
        resetButton.onClick.AddListener(HandleResetButtonClicked);
    }

    private void UnbindButton()
    {
        if (resetButton == null)
        {
            return;
        }

        resetButton.onClick.RemoveListener(HandleResetButtonClicked);
    }

    private void HandleResetButtonClicked()
    {
        if (progressionManager == null)
        {
            return;
        }

        progressionManager.ResetSavedProgress();
    }
}

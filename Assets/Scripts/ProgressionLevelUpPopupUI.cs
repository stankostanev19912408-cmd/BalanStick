using System.Collections;
using TMPro;
using UnityEngine;

public class ProgressionLevelUpPopupUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ProgressionManager progressionManager;
    [SerializeField] private GameObject levelInfoRoot;
    [SerializeField] private CanvasGroup levelInfoCanvasGroup;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI detailsText;
    [SerializeField] private GameObject retryButtonObject;

    [Header("Timing")]
    [SerializeField, Min(0.05f)] private float fadeDuration = 0.25f;

    private Coroutine visibilityRoutine;
    private bool retryButtonStateCaptured;
    private bool retryButtonWasActive;

    private void Awake()
    {
        ValidateReferences();
        HideImmediately();
    }

    private void OnEnable()
    {
        ValidateReferences();

        if (progressionManager != null)
        {
            progressionManager.RunResultProcessed -= HandleRunResultProcessed;
            progressionManager.RunResultProcessed += HandleRunResultProcessed;
        }
    }

    private void OnDisable()
    {
        if (progressionManager != null)
        {
            progressionManager.RunResultProcessed -= HandleRunResultProcessed;
        }
    }

    private void HandleRunResultProcessed(ProgressionResult result)
    {
        if (result == null || !result.LevelIncreased)
        {
            return;
        }

        if (levelInfoRoot == null || titleText == null || levelText == null || detailsText == null)
        {
            Debug.LogWarning("ProgressionLevelUpPopupUI: LevelInfo references are not assigned.", this);
            return;
        }

        titleText.text = result.UnlockedLevels.Count > 1 ? "NEW LEVELS UNLOCKED!" : "NEW LEVEL UNLOCKED!";
        levelText.text = $"LEVEL {result.NewLevel}";
        detailsText.text = BuildDetailsText(result);

        if (visibilityRoutine != null)
        {
            StopCoroutine(visibilityRoutine);
        }

        visibilityRoutine = StartCoroutine(ShowPopupRoutine());
    }

    private string BuildDetailsText(ProgressionResult result)
    {
        return $"Run score: {result.FinalScore}";
    }

    private IEnumerator ShowPopupRoutine()
    {
        DisableRetryButtonTemporarily();
        SetPopupVisible(true);
        yield return FadeCanvas(0f, 1f, fadeDuration);
        yield return WaitForTap();
        yield return FadeCanvas(1f, 0f, fadeDuration);
        SetPopupVisible(false);
        RestoreRetryButtonState();
        visibilityRoutine = null;
    }

    private IEnumerator FadeCanvas(float from, float to, float duration)
    {
        if (levelInfoCanvasGroup == null)
        {
            yield break;
        }

        duration = Mathf.Max(0.0001f, duration);
        levelInfoCanvasGroup.alpha = from;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            levelInfoCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        levelInfoCanvasGroup.alpha = to;
    }

    private IEnumerator WaitForTap()
    {
        while (!WasScreenTapped())
        {
            yield return null;
        }
    }

    private static bool WasScreenTapped()
    {
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Began)
                {
                    return true;
                }
            }
        }

        return Input.GetMouseButtonDown(0);
    }

    private void HideImmediately()
    {
        if (levelInfoCanvasGroup != null)
        {
            levelInfoCanvasGroup.alpha = 0f;
        }

        SetPopupVisible(false);
        RestoreRetryButtonState();
    }

    private void SetPopupVisible(bool isVisible)
    {
        if (levelInfoRoot != null && levelInfoRoot.activeSelf != isVisible)
        {
            levelInfoRoot.SetActive(isVisible);
        }
    }

    private void ValidateReferences()
    {
        if (progressionManager == null)
        {
            Debug.LogWarning("ProgressionLevelUpPopupUI: progressionManager is not assigned.", this);
        }

        if (levelInfoRoot == null)
        {
            Debug.LogWarning("ProgressionLevelUpPopupUI: levelInfoRoot is not assigned.", this);
            return;
        }

        if (levelInfoCanvasGroup == null)
        {
            Debug.LogWarning("ProgressionLevelUpPopupUI: levelInfoCanvasGroup is not assigned.", this);
        }

        if (titleText == null)
        {
            Debug.LogWarning("ProgressionLevelUpPopupUI: titleText is not assigned.", this);
        }

        if (levelText == null)
        {
            Debug.LogWarning("ProgressionLevelUpPopupUI: levelText is not assigned.", this);
        }

        if (detailsText == null)
        {
            Debug.LogWarning("ProgressionLevelUpPopupUI: detailsText is not assigned.", this);
        }

        if (retryButtonObject == null)
        {
            Debug.LogWarning("ProgressionLevelUpPopupUI: retryButtonObject is not assigned.", this);
        }
    }

    private void DisableRetryButtonTemporarily()
    {
        if (retryButtonObject == null)
        {
            return;
        }

        if (!retryButtonStateCaptured)
        {
            retryButtonWasActive = retryButtonObject.activeSelf;
            retryButtonStateCaptured = true;
        }

        if (retryButtonObject.activeSelf)
        {
            retryButtonObject.SetActive(false);
        }
    }

    private void RestoreRetryButtonState()
    {
        if (retryButtonObject == null || !retryButtonStateCaptured)
        {
            return;
        }

        if (retryButtonObject.activeSelf != retryButtonWasActive)
        {
            retryButtonObject.SetActive(retryButtonWasActive);
        }

        retryButtonStateCaptured = false;
    }
}

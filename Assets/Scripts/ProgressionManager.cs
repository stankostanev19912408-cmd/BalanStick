using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ProgressionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ProgressionConfig progressionConfig;
    [FormerlySerializedAs("scoreCouter")]
    [SerializeField] private ScoreCounter scoreCounter;
    [SerializeField] private StickTiltForce stickTiltForce;
    [SerializeField] private PlayerProgressSaveManager playerProgressSaveManager;
    [SerializeField] private bool processRunResultOnRetry = true;

    [Header("Debug")]
    [SerializeField] private bool logRunResultsToConsole = true;

    private PlayerProgressData playerProgressData = new PlayerProgressData();
    private bool hasProcessedCurrentRun;

    public event Action<ProgressionResult> RunResultProcessed;

    public int CurrentLevel => playerProgressData != null ? playerProgressData.CurrentLevel : 0;

    public void GetUnlockedEffects(
        GameplayEffectPolarity polarity,
        List<WeightedGameplayEffect> destination)
    {
        if (destination == null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        if (progressionConfig == null)
        {
            destination.Clear();
            return;
        }

        progressionConfig.GetUnlockedEffects(CurrentLevel, polarity, destination);
    }

    private void Awake()
    {
        if (progressionConfig == null)
        {
            Debug.LogWarning("ProgressionManager: progressionConfig is not assigned.", this);
        }

        if (scoreCounter == null)
        {
            Debug.LogWarning("ProgressionManager: scoreCounter is not assigned.", this);
        }

        if (stickTiltForce == null)
        {
            Debug.LogWarning("ProgressionManager: stickTiltForce is not assigned.", this);
        }

        if (playerProgressSaveManager == null)
        {
            Debug.LogWarning("ProgressionManager: playerProgressSaveManager is not assigned.", this);
        }

        LoadProgress();
    }

    private void OnEnable()
    {
        hasProcessedCurrentRun = false;

        if (stickTiltForce == null)
        {
            return;
        }

        stickTiltForce.RetryStateChanged -= HandleRetryStateChanged;
        stickTiltForce.RetryStateChanged += HandleRetryStateChanged;
    }

    private void OnDisable()
    {
        if (stickTiltForce == null)
        {
            return;
        }

        stickTiltForce.RetryStateChanged -= HandleRetryStateChanged;
    }

    public ProgressionResult ProcessRunResult(int finalScore)
    {
        finalScore = Mathf.Max(0, finalScore);

        int previousLevel = CurrentLevel;
        int reachedLevel = progressionConfig != null ? progressionConfig.GetReachedLevel(finalScore) : previousLevel;
        int newLevel = Mathf.Max(previousLevel, reachedLevel);

        ProgressionResult result = new ProgressionResult(previousLevel, newLevel, finalScore);
        if (newLevel <= previousLevel || progressionConfig == null)
        {
            NotifyRunProcessed(result);
            return result;
        }

        for (int levelNumber = previousLevel + 1; levelNumber <= newLevel; levelNumber++)
        {
            ProgressionLevelDefinition definition = progressionConfig.GetLevelDefinition(levelNumber);
            result.AddUnlockedLevel(levelNumber, definition);
        }

        playerProgressData.CurrentLevel = newLevel;
        SaveProgress();
        NotifyRunProcessed(result);
        return result;
    }

    public void LoadProgress()
    {
        if (playerProgressSaveManager == null)
        {
            playerProgressData = new PlayerProgressData();
            return;
        }

        playerProgressData = playerProgressSaveManager.LoadProgress() ?? new PlayerProgressData();
    }

    [ContextMenu("Reset Saved Progress")]
    public void ResetSavedProgress()
    {
        playerProgressData = new PlayerProgressData();

        if (playerProgressSaveManager != null)
        {
            playerProgressSaveManager.DeleteProgress();
        }

        hasProcessedCurrentRun = false;
    }

    private void SaveProgress()
    {
        if (playerProgressSaveManager == null)
        {
            return;
        }

        playerProgressSaveManager.SaveProgress(playerProgressData);
    }

    private void HandleRetryStateChanged(bool retryRequired)
    {
        if (!processRunResultOnRetry)
        {
            hasProcessedCurrentRun = retryRequired;
            return;
        }

        if (!retryRequired)
        {
            hasProcessedCurrentRun = false;
            return;
        }

        if (hasProcessedCurrentRun)
        {
            return;
        }

        hasProcessedCurrentRun = true;
        int finalScore = scoreCounter != null ? scoreCounter.CurrentScore : 0;
        ProcessRunResult(finalScore);
    }

    private void NotifyRunProcessed(ProgressionResult result)
    {
        if (logRunResultsToConsole)
        {
            if (result.LevelIncreased)
            {
                Debug.Log(
                    $"ProgressionManager: run finished with score {result.FinalScore}. Level increased from {result.PreviousLevel} to {result.NewLevel}.",
                    this);
            }
            else
            {
                Debug.Log(
                    $"ProgressionManager: run finished with score {result.FinalScore}. Current level remains {result.PreviousLevel}.",
                    this);
            }
        }

        RunResultProcessed?.Invoke(result);
    }
}

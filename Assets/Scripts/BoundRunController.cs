using UnityEngine;

[DefaultExecutionOrder(200)]
[RequireComponent(typeof(ScoreCounter))]
[RequireComponent(typeof(PatternEventScheduler))]
public sealed class BoundRunController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameProgressionAsset gameProgression;
    [SerializeField] private ScoreCounter scoreCounter;
    [SerializeField] private PatternEventScheduler patternEventScheduler;
    [SerializeField] private StickTiltForce stickTiltForce;
    [SerializeField] private PlayerProgressSaveManager progressSaveManager;

    [Header("Visual Catch Up")]
    [SerializeField, Min(0.01f)] private float catchUpDurationSeconds = 1.5f;

    private int highestReachedBoundIndex;
    private bool isVisualCatchUpActive;
    private float catchUpTargetHeight;
    private float catchUpElapsedSeconds;
    private int replayBoundIndex;

    public int HighestReachedBoundIndex => highestReachedBoundIndex;
    public bool IsVisualCatchUpActive => isVisualCatchUpActive;

    private void Awake()
    {
        if (scoreCounter == null)
        {
            scoreCounter = GetComponent<ScoreCounter>();
        }

        if (patternEventScheduler == null)
        {
            patternEventScheduler = GetComponent<PatternEventScheduler>();
        }

        if (stickTiltForce == null)
        {
            stickTiltForce = FindObjectOfType<StickTiltForce>();
        }

        if (progressSaveManager == null)
        {
            progressSaveManager = GetComponentInChildren<PlayerProgressSaveManager>();
        }

        string errors = gameProgression == null ? "GameProgression is missing." : null;
        bool progressionValid = gameProgression != null && gameProgression.TryValidate(out errors);
        if (!progressionValid ||
            scoreCounter == null || patternEventScheduler == null ||
            stickTiltForce == null || progressSaveManager == null)
        {
            Debug.LogError($"BoundRunController: missing or invalid progression setup. {errors}", this);
            enabled = false;
            return;
        }

        highestReachedBoundIndex = Mathf.Clamp(
            progressSaveManager.LoadHighestReachedBoundIndex(), 0, gameProgression.Bounds.Count);
    }

    private void Start()
    {
        if (highestReachedBoundIndex > 0)
        {
            BeginVisualCatchUp();
        }
    }

    private void Update()
    {
        if (isVisualCatchUpActive)
        {
            if (!stickTiltForce.isActiveAndEnabled || !stickTiltForce.IsInputUnlocked)
            {
                return;
            }

            if (catchUpTargetHeight <= 0f)
            {
                CompleteVisualCatchUp();
                return;
            }

            catchUpElapsedSeconds += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(catchUpElapsedSeconds / Mathf.Max(0.01f, catchUpDurationSeconds));
            scoreCounter.SetVisualHeight(catchUpTargetHeight * Mathf.SmoothStep(0f, 1f, progress));
            if (progress >= 1f)
            {
                CompleteVisualCatchUp();
            }

            return;
        }

        if (!scoreCounter.IsScoringActive)
        {
            return;
        }

        float height = scoreCounter.CurrentScoreValue;
        int newHighest = highestReachedBoundIndex;
        int authoredBoundCount = gameProgression.Bounds.Count;
        for (int i = highestReachedBoundIndex + 1; i <= authoredBoundCount; i++)
        {
            float boundStartHeight = i == authoredBoundCount
                ? gameProgression.Bounds[authoredBoundCount - 1].HeightTo
                : gameProgression.Bounds[i].HeightFrom;
            if (height < boundStartHeight)
            {
                break;
            }

            newHighest = i;
        }

        if (newHighest > highestReachedBoundIndex)
        {
            highestReachedBoundIndex = newHighest;
            progressSaveManager.SaveHighestReachedBoundIndex(newHighest);
            Debug.Log($"BoundRunController: reached bound {newHighest + 1} at {height:F2} m.", this);
        }
    }

    public bool BeginRetry()
    {
        if (!isActiveAndEnabled)
        {
            return false;
        }

        if (isVisualCatchUpActive)
        {
            return true;
        }

        if (!stickTiltForce.IsRetryRequired)
        {
            return false;
        }

        BeginVisualCatchUp();
        stickTiltForce.ClearRetryRequirement();
        return true;
    }

    [ContextMenu("Reset Saved Bound Progress")]
    public void ResetSavedBoundProgress()
    {
        if (progressSaveManager == null)
        {
            return;
        }

        progressSaveManager.DeleteHighestReachedBoundIndex();
        highestReachedBoundIndex = 0;
    }

    private void BeginVisualCatchUp()
    {
        replayBoundIndex = Mathf.Max(0, highestReachedBoundIndex - 1);
        catchUpTargetHeight = gameProgression.Bounds[replayBoundIndex].HeightFrom;
        catchUpElapsedSeconds = 0f;
        isVisualCatchUpActive = true;
        stickTiltForce.SetGameplaySuspended(true);
        scoreCounter.BeginVisualCatchUp();
    }

    private void CompleteVisualCatchUp()
    {
        scoreCounter.SetVisualHeight(catchUpTargetHeight);
        patternEventScheduler.ResumeAtBound(replayBoundIndex, catchUpTargetHeight);
        stickTiltForce.SetGameplaySuspended(false);
        scoreCounter.CompleteVisualCatchUp();
        isVisualCatchUpActive = false;
    }

    private void OnDisable()
    {
        if (!isVisualCatchUpActive)
        {
            return;
        }

        stickTiltForce.SetGameplaySuspended(false);
        scoreCounter.CompleteVisualCatchUp();
        isVisualCatchUpActive = false;
    }
}

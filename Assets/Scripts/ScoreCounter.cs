using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public enum ScoringMode
{
    TimeToSkillTransition = 0,
    SkillOnly = 1
}

public class ScoreCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private ProgressionManager progressionManager;
    [SerializeField] private bool resetScoreOnEnable = true;

    [Header("Scoring Mode")]
    [SerializeField] private ScoringMode scoringMode = ScoringMode.TimeToSkillTransition;
    [FormerlySerializedAs("timePointsPerSecond")]
    [SerializeField, Min(0f)] private float pointsPerSecond = 10f;
    [FormerlySerializedAs("onlyTiltMaxPoints")]
    [SerializeField, Min(0.01f)] private float skillScoringTransitionScore = 1000f;

    [Header("Scoring by skill")]
    [SerializeField] private Transform stickTransform;
    [SerializeField] private Rigidbody stickRigidbody;
    [SerializeField] private StickTiltForce stickTiltForce;
    [SerializeField, Min(0f)] private float minSpeedForPoints = 0.2f;
    [SerializeField, Min(0.01f)] private float maxSpeedForMaxPoints = 12f;
    [SerializeField, Min(0f)] private float minTiltAngleForPoints = 5f;
    [SerializeField, Range(0f, 90f)] private float maxTiltAngleForMaxPoints = 45f;
    [SerializeField, Min(0f)] private float maxSpeedPointsPerSecond = 25f;
    [SerializeField, Min(0f)] private float maxTiltPointsPerSecond = 25f;
    [SerializeField] private AnimationCurve speedPointsCurve;
    [SerializeField] private AnimationCurve tiltPointsCurve;

    private float currentScore;
    private bool isRetryRequired;
    private bool isInputUnlocked = true;
    private GameplayEffectController gameplayEffectController;

    public int CurrentScore => Mathf.FloorToInt(currentScore);
    public float CurrentScoreValue => currentScore;

    public void SetGameplayEffectController(GameplayEffectController sourceGameplayEffectController)
    {
        gameplayEffectController = sourceGameplayEffectController;
    }

    private void Awake()
    {
        ResolveProgressionManager();

        if (stickTransform == null)
        {
            Debug.LogWarning("ScoreCounter: stickTransform is not assigned.", this);
        }

        if (stickRigidbody == null)
        {
            Debug.LogWarning("ScoreCounter: stickRigidbody is not assigned.", this);
        }

        if (stickTiltForce == null)
        {
            Debug.LogWarning("ScoreCounter: stickTiltForce is not assigned.", this);
        }

        if (progressionManager == null)
        {
            Debug.LogWarning("ScoreCounter: ProgressionManager was not found. Level multiplier defaults to x1.", this);
        }
    }

    private void OnValidate()
    {
        pointsPerSecond = Mathf.Max(0f, pointsPerSecond);
        skillScoringTransitionScore = Mathf.Max(0.01f, skillScoringTransitionScore);
    }

    private void OnEnable()
    {
        if (resetScoreOnEnable)
        {
            ResetScore();
        }

        if (stickTiltForce != null)
        {
            stickTiltForce.RetryStateChanged -= HandleRetryStateChanged;
            stickTiltForce.RetryStateChanged += HandleRetryStateChanged;
            stickTiltForce.StartGateStateChanged -= HandleStartGateStateChanged;
            stickTiltForce.StartGateStateChanged += HandleStartGateStateChanged;
            isRetryRequired = stickTiltForce.IsRetryRequired;
            isInputUnlocked = stickTiltForce.IsInputUnlocked;
        }

        UpdateScoreText();
    }

    private void OnDisable()
    {
        if (stickTiltForce == null)
        {
            return;
        }

        stickTiltForce.RetryStateChanged -= HandleRetryStateChanged;
        stickTiltForce.StartGateStateChanged -= HandleStartGateStateChanged;
    }

    private void Update()
    {
        if (stickTiltForce == null)
        {
            return;
        }

        bool isStickInactive = stickTransform != null && !stickTransform.gameObject.activeInHierarchy;
        if (isStickInactive || isRetryRequired || !isInputUnlocked)
        {
            UpdateScoreText();
            return;
        }

        float calculatedPointsPerSecond = CalculatePointsPerSecond();

        if (calculatedPointsPerSecond > 0f)
        {
            currentScore += calculatedPointsPerSecond * Time.deltaTime;
        }

        UpdateScoreText();
    }

    private float CalculatePointsPerSecond()
    {
        float levelMultiplier = GetLevelScoreMultiplier();
        float boostMultiplier = gameplayEffectController != null
            ? gameplayEffectController.CurrentFixedScoreRateMultiplier
            : 0f;

        if (boostMultiplier > 0f)
        {
            return pointsPerSecond * levelMultiplier * boostMultiplier;
        }

        float skillPointsPerSecond = EvaluatePointsFromStick();
        float modePointsPerSecond;
        switch (scoringMode)
        {
            case ScoringMode.SkillOnly:
                modePointsPerSecond = skillPointsPerSecond;
                break;
            default:
                float transitionScore = Mathf.Max(0.01f, skillScoringTransitionScore);
                float transitionProgress = Mathf.Clamp01(currentScore / transitionScore);
                modePointsPerSecond = Mathf.Lerp(pointsPerSecond, skillPointsPerSecond, transitionProgress);
                break;
        }

        return modePointsPerSecond * levelMultiplier;
    }

    private float GetLevelScoreMultiplier()
    {
        ResolveProgressionManager();
        return progressionManager != null ? progressionManager.CurrentLevel + 1f : 1f;
    }

    private void ResolveProgressionManager()
    {
        if (progressionManager == null)
        {
            progressionManager = FindObjectOfType<ProgressionManager>();
        }
    }

    private float EvaluatePointsFromStick()
    {
        if (stickTransform == null || stickRigidbody == null)
        {
            return 0f;
        }

        float speed = stickRigidbody.velocity.magnitude;
        float tiltAngle = Vector3.Angle(stickTransform.up, Vector3.up);
        return EvaluateSpeedPoints(speed) + EvaluateTiltPoints(tiltAngle);
    }

    private float EvaluateSpeedPoints(float speed)
    {
        float clampedMaxSpeed = Mathf.Max(minSpeedForPoints + 0.0001f, maxSpeedForMaxPoints);
        float normalizedSpeed = Mathf.InverseLerp(minSpeedForPoints, clampedMaxSpeed, speed);
        float curveValue = EvaluatePointsCurve(speedPointsCurve, normalizedSpeed);
        return curveValue * maxSpeedPointsPerSecond;
    }

    private float EvaluateTiltPoints(float tiltAngle)
    {
        float clampedMaxTilt = Mathf.Max(minTiltAngleForPoints + 0.0001f, maxTiltAngleForMaxPoints);
        float normalizedTilt = Mathf.InverseLerp(minTiltAngleForPoints, clampedMaxTilt, tiltAngle);
        float curveValue = EvaluatePointsCurve(tiltPointsCurve, normalizedTilt);
        return curveValue * maxTiltPointsPerSecond;
    }

    private float EvaluatePointsCurve(AnimationCurve curve, float normalizedValue)
    {
        if (curve == null || curve.length == 0)
        {
            return normalizedValue;
        }

        return Mathf.Max(0f, curve.Evaluate(normalizedValue));
    }

    private void HandleRetryStateChanged(bool retryRequired)
    {
        isRetryRequired = retryRequired;

        if (!retryRequired)
        {
            ResetScore();
        }
    }

    private void HandleStartGateStateChanged(bool inputUnlocked)
    {
        isInputUnlocked = inputUnlocked;
    }

    private void ResetScore()
    {
        currentScore = 0f;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreText == null)
        {
            return;
        }

        scoreText.text = CurrentScore.ToString();
    }
}

using TMPro;
using UnityEngine;

public class ScoreCouter : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private BoostChargeBar boostChargeBar;
    [SerializeField] private bool resetScoreOnEnable = true;
    [SerializeField, Min(0f)] private float timePointsPerSecond = 10f;
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
    [SerializeField] private float onlyTiltMaxPoints;

    private float currentScore;
    private bool isRetryRequired;
    private bool isInputUnlocked = true;

    public int CurrentScore => Mathf.FloorToInt(currentScore);
    public float CurrentScoreValue => currentScore;

    private void Awake()
    {
        if (stickTransform == null)
        {
            Debug.LogWarning("ScoreCouter: stickTransform is not assigned.", this);
        }

        if (stickRigidbody == null)
        {
            Debug.LogWarning("ScoreCouter: stickRigidbody is not assigned.", this);
        }

        if (stickTiltForce == null)
        {
            Debug.LogWarning("ScoreCouter: stickTiltForce is not assigned.", this);
        }

        if (scoreText == null)
        {
            Debug.LogWarning("ScoreCouter: scoreText is not assigned.", this);
        }

        if (boostChargeBar == null)
        {
            Debug.LogWarning("ScoreCouter: boostChargeBar is not assigned.", this);
        }
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
        if (stickTiltForce == null || scoreText == null)
        {
            return;
        }

        bool isStickInactive = stickTransform != null && !stickTransform.gameObject.activeInHierarchy;
        if (isStickInactive || isRetryRequired || !isInputUnlocked)
        {
            UpdateScoreText();
            return;
        }

        float pointsPerSecond = Mathf.Lerp(timePointsPerSecond, EvaluatePointsFromStick(), currentScore / onlyTiltMaxPoints);
        pointsPerSecond *= boostChargeBar != null ? boostChargeBar.CurrentScoreMultiplier : 1f;

        if (pointsPerSecond > 0f)
        {
            currentScore += pointsPerSecond * Time.deltaTime;
        }

        UpdateScoreText();
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

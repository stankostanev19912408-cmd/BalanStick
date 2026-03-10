using TMPro;
using UnityEngine;

public class ScoreCouter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cylinderTransform;
    [SerializeField] private Rigidbody cylinderRigidbody;
    [SerializeField] private CylinderTiltForce cylinderTiltForce;
    [SerializeField] private TMP_Text scoreText;

    [Header("Scoring")]
    [SerializeField, Min(0f)] private float minSpeedForPoints = 0.2f;
    [SerializeField, Min(0.01f)] private float maxSpeedForMaxPoints = 12f;
    [SerializeField, Min(0f)] private float minTiltAngleForPoints = 5f;
    [SerializeField, Range(0f, 90f)] private float maxTiltAngleForMaxPoints = 45f;
    [SerializeField, Min(0f)] private float maxSpeedPointsPerSecond = 25f;
    [SerializeField, Min(0f)] private float maxTiltPointsPerSecond = 25f;
    [SerializeField] private bool resetScoreOnEnable = true;

    private float currentScore;
    private bool isRetryRequired;

    public int CurrentScore => Mathf.FloorToInt(currentScore);
    public float CurrentScoreValue => currentScore;

    private void Awake()
    {
        if (cylinderTransform == null)
        {
            Debug.LogWarning("ScoreCouter: cylinderTransform is not assigned.", this);
        }

        if (cylinderRigidbody == null)
        {
            Debug.LogWarning("ScoreCouter: cylinderRigidbody is not assigned.", this);
        }

        if (cylinderTiltForce == null)
        {
            Debug.LogWarning("ScoreCouter: cylinderTiltForce is not assigned.", this);
        }

        if (scoreText == null)
        {
            Debug.LogWarning("ScoreCouter: scoreText is not assigned.", this);
        }
    }

    private void OnEnable()
    {
        if (resetScoreOnEnable)
        {
            ResetScore();
        }

        if (cylinderTiltForce != null)
        {
            cylinderTiltForce.RetryStateChanged -= HandleRetryStateChanged;
            cylinderTiltForce.RetryStateChanged += HandleRetryStateChanged;
            isRetryRequired = cylinderTiltForce.IsRetryRequired;
        }

        UpdateScoreText();
    }

    private void OnDisable()
    {
        if (cylinderTiltForce == null)
        {
            return;
        }

        cylinderTiltForce.RetryStateChanged -= HandleRetryStateChanged;
    }

    private void Update()
    {
        if (cylinderTransform == null || cylinderRigidbody == null || cylinderTiltForce == null || scoreText == null)
        {
            return;
        }

        if (!cylinderTransform.gameObject.activeInHierarchy || isRetryRequired)
        {
            UpdateScoreText();
            return;
        }

        float speed = cylinderRigidbody.velocity.magnitude;
        float tiltAngle = Vector3.Angle(cylinderTransform.up, Vector3.up);
        float pointsPerSecond = EvaluateSpeedPoints(speed) + EvaluateTiltPoints(tiltAngle);

        if (pointsPerSecond > 0f)
        {
            currentScore += pointsPerSecond * Time.deltaTime;
        }

        UpdateScoreText();
    }

    private float EvaluateSpeedPoints(float speed)
    {
        float clampedMaxSpeed = Mathf.Max(minSpeedForPoints + 0.0001f, maxSpeedForMaxPoints);
        float normalizedSpeed = Mathf.InverseLerp(minSpeedForPoints, clampedMaxSpeed, speed);
        return normalizedSpeed * maxSpeedPointsPerSecond;
    }

    private float EvaluateTiltPoints(float tiltAngle)
    {
        float clampedMaxTilt = Mathf.Max(minTiltAngleForPoints + 0.0001f, maxTiltAngleForMaxPoints);
        float normalizedTilt = Mathf.InverseLerp(minTiltAngleForPoints, clampedMaxTilt, tiltAngle);
        return normalizedTilt * maxTiltPointsPerSecond;
    }

    private void HandleRetryStateChanged(bool retryRequired)
    {
        isRetryRequired = retryRequired;

        if (!retryRequired)
        {
            ResetScore();
        }
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

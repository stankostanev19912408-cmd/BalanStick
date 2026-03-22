using TMPro;
using UnityEngine;

public class StickTiltScore : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform stickTransform;
    [SerializeField] private TMP_Text scoreText;

    [Header("Scoring")]
    [SerializeField] private float minAngleDegrees = 5f;
    [SerializeField] private float maxAngleDegrees = 45f;
    [SerializeField] private float minPointsPerSecond = 1f;
    [SerializeField] private float maxPointsPerSecond = 20f;
    [SerializeField] private bool resetScoreOnEnable = true;
    [SerializeField] private bool resetScoreWhenStickDisabled = true;

    private float currentScore;
    private bool wasStickActive;

    private void OnEnable()
    {
        if (resetScoreOnEnable)
        {
            currentScore = 0f;
        }

        wasStickActive = stickTransform != null && stickTransform.gameObject.activeInHierarchy;
        UpdateScoreText();
    }

    private void Update()
    {
        if (stickTransform == null || scoreText == null)
        {
            return;
        }

        bool isStickActive = stickTransform.gameObject.activeInHierarchy;
        if (resetScoreWhenStickDisabled && wasStickActive && !isStickActive)
        {
            currentScore = 0f;
        }

        wasStickActive = isStickActive;
        if (!isStickActive)
        {
            UpdateScoreText();
            return;
        }

        float tiltAngle = Vector3.Angle(stickTransform.up, Vector3.up);
        float pointsPerSecond = EvaluatePointsPerSecond(tiltAngle);
        if (pointsPerSecond > 0f)
        {
            currentScore += pointsPerSecond * Time.deltaTime;
        }

        UpdateScoreText();
    }

    private float EvaluatePointsPerSecond(float tiltAngle)
    {
        if (tiltAngle < minAngleDegrees || tiltAngle > maxAngleDegrees)
        {
            return 0f;
        }

        float clampedMaxAngle = Mathf.Max(minAngleDegrees, maxAngleDegrees);
        if (clampedMaxAngle - minAngleDegrees < 0.0001f)
        {
            return minPointsPerSecond;
        }

        float interpolation = Mathf.InverseLerp(minAngleDegrees, clampedMaxAngle, tiltAngle);
        return Mathf.Lerp(minPointsPerSecond, maxPointsPerSecond, interpolation);
    }

    private void UpdateScoreText()
    {
        scoreText.text = Mathf.FloorToInt(currentScore).ToString();
    }
}

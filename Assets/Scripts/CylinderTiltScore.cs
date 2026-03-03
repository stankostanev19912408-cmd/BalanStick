using TMPro;
using UnityEngine;

public class CylinderTiltScore : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cylinderTransform;
    [SerializeField] private TMP_Text scoreText;

    [Header("Scoring")]
    [SerializeField] private float minAngleDegrees = 5f;
    [SerializeField] private float maxAngleDegrees = 45f;
    [SerializeField] private float minPointsPerSecond = 1f;
    [SerializeField] private float maxPointsPerSecond = 20f;
    [SerializeField] private bool resetScoreOnEnable = true;
    [SerializeField] private bool resetScoreWhenCylinderDisabled = true;

    private float currentScore;
    private bool wasCylinderActive;

    private void OnEnable()
    {
        if (resetScoreOnEnable)
        {
            currentScore = 0f;
        }

        wasCylinderActive = cylinderTransform != null && cylinderTransform.gameObject.activeInHierarchy;
        UpdateScoreText();
    }

    private void Update()
    {
        if (cylinderTransform == null || scoreText == null)
        {
            return;
        }

        bool isCylinderActive = cylinderTransform.gameObject.activeInHierarchy;
        if (resetScoreWhenCylinderDisabled && wasCylinderActive && !isCylinderActive)
        {
            currentScore = 0f;
        }

        wasCylinderActive = isCylinderActive;
        if (!isCylinderActive)
        {
            UpdateScoreText();
            return;
        }

        float tiltAngle = Vector3.Angle(cylinderTransform.up, Vector3.up);
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

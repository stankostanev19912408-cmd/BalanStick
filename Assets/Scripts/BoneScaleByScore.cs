using UnityEngine;
using UnityEngine.Serialization;

public class BoneScaleByScore : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform bone001;
    [FormerlySerializedAs("scoreCouter")]
    [SerializeField] private ScoreCounter scoreCounter;

    [Header("Scale")]
    [SerializeField, Min(0.01f)] private float maxScore = 100f;
    [SerializeField, Range(0f, 1f)] private float scaleAtMaxScore = 0.2f;
    [SerializeField] private AnimationCurve scaleCurve;

    private const float StartScale = 1f;

    private void Awake()
    {
        ValidateReferences();
        ApplyScale();
    }

    private void OnEnable()
    {
        ApplyScale();
    }

    private void OnValidate()
    {
        maxScore = Mathf.Max(0.0001f, maxScore);
        scaleAtMaxScore = Mathf.Clamp01(scaleAtMaxScore);

        if (!Application.isPlaying)
        {
            ApplyScale();
        }
    }

    private void Update()
    {
        ApplyScale();
    }

    private void ValidateReferences()
    {
        if (bone001 == null)
        {
            Debug.LogWarning("BoneScaleByScore: bone001 is not assigned.", this);
        }

        if (scoreCounter == null)
        {
            Debug.LogWarning("BoneScaleByScore: scoreCounter is not assigned.", this);
        }
    }

    private float GetCurrentScore()
    {
        return scoreCounter != null ? Mathf.Max(0f, scoreCounter.CurrentScoreValue) : 0f;
    }

    private void ApplyScale()
    {
        if (bone001 == null || scoreCounter == null)
        {
            return;
        }

        float safeMaxScore = Mathf.Max(0.0001f, maxScore);
        float normalizedScore = scaleCurve.Evaluate(Mathf.Clamp01(GetCurrentScore() / safeMaxScore));
        float targetScale = Mathf.Lerp(StartScale, scaleAtMaxScore, normalizedScore);
        bone001.localScale = new Vector3(targetScale, targetScale, targetScale);
    }
}

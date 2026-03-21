using System;
using UnityEngine;

public class MapController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform quadTransform;
    [SerializeField] private MeshRenderer mapRenderer;
    [SerializeField] private ScoreCouter scoreCouter;
    [SerializeField] private Map[] mapTextures;

    [Header("Scale")]
    [SerializeField, Min(0.01f)] private float minScale = 10f;
    [SerializeField, Min(0.01f)] private float maxScale = 30f;
    [SerializeField, Min(1)] private int pointsPerScaleStep = 100;

    private Material runtimeMapMaterial;
    private int appliedTextureIndex = -1;

    private void Awake()
    { 
        if (quadTransform == null)
        {
            Debug.LogWarning("MapController: quadTransform is not assigned.", this);
        }

        if (mapRenderer == null)
        {
            Debug.LogWarning("MapController: mapRenderer is not assigned.", this);
        }

        if (scoreCouter == null)
        {
            Debug.LogWarning("MapController: scoreCouter is not assigned.", this);
        }

        if (mapTextures == null || mapTextures.Length == 0)
        {
            Debug.LogWarning("MapController: mapTextures is empty.", this);
        }
    }

    private void OnEnable()
    {
        appliedTextureIndex = -1;
        ApplyMapState();
    }

    private void Update()
    {
        ApplyMapState();
    }

    private void ApplyMapState()
    {
        if (quadTransform == null || scoreCouter == null)
        {
            return;
        }

        float score = Mathf.Max(0f, scoreCouter.CurrentScoreValue);
        if (mapTextures == null || mapTextures.Length == 0)
        {
            float scaleRange = Mathf.Max(0f, maxScale - minScale);
            if (scaleRange <= 0f)
            {
                ApplyScale(minScale);
                return;
            }

            float cycleProgress = Mathf.Clamp01(score / (scaleRange * Mathf.Max(1, pointsPerScaleStep)));
            ApplyScale(GetExponentialCycleScale(cycleProgress));
            return;
        }

        int lastTextureIndex = mapTextures.Length - 1;
        int activeTextureIndex = GetTextureIndexForScore(score);
        float rangeStartScore = GetTextureStartScore(activeTextureIndex);
        float rangeEndScore = GetTextureEndScore(activeTextureIndex);

        if (activeTextureIndex == lastTextureIndex && score > rangeEndScore)
        {
            float scoreOnLastTexture = Mathf.Max(0f, score - rangeEndScore);
            ApplyScale(GetAsymptoticScale(scoreOnLastTexture));
            ApplyTexture(lastTextureIndex);
            return;
        }

        float cycleProgress01 = GetRangeProgress(score, rangeStartScore, rangeEndScore);
        float targetScale = GetExponentialCycleScale(cycleProgress01);
        ApplyScale(targetScale);

        ApplyTexture(activeTextureIndex);
    }

    private float GetCyclePoints()
    {
        float scaleRange = Mathf.Max(0f, maxScale - minScale);
        return scaleRange * pointsPerScaleStep;
    }

    private float GetExponentialCycleScale(float cycleProgress01)
    {
        cycleProgress01 = Mathf.Clamp01(cycleProgress01);
        return maxScale * Mathf.Pow(minScale / maxScale, cycleProgress01);
    }

    private int GetTextureIndexForScore(float score)
    {
        int lastTextureIndex = mapTextures.Length - 1;
        for (int i = 0; i < lastTextureIndex; i++)
        {
            if (score <= GetTextureEndScore(i))
            {
                return i;
            }
        }

        return lastTextureIndex;
    }

    private float GetTextureStartScore(int textureIndex)
    {
        if (textureIndex <= 0)
        {
            return 0f;
        }

        return Mathf.Max(0f, mapTextures[textureIndex - 1].height);
    }

    private float GetTextureEndScore(int textureIndex)
    {
        float startScore = GetTextureStartScore(textureIndex);
        return Mathf.Max(startScore, mapTextures[textureIndex].height);
    }

    private float GetRangeProgress(float score, float startScore, float endScore)
    {
        if (endScore <= startScore)
        {
            return 1f;
        }

        return Mathf.Clamp01((score - startScore) / (endScore - startScore));
    }

    private float GetAsymptoticScale(float scoreOnLastTexture)
    {
        float scaleRange = Mathf.Max(0f, maxScale - minScale);
        if (scaleRange <= 0f)
        {
            return minScale;
        }

        float initialLinearStep = Mathf.Max(1, pointsPerScaleStep);
        float decayRate = 1f / (scaleRange * initialLinearStep);
        float targetScale = minScale + (scaleRange * Mathf.Exp(-scoreOnLastTexture * decayRate));
        return Mathf.Max(minScale + 0.001f, targetScale);
    }

    private void ApplyTexture(int completedCycles)
    {
        if (mapRenderer == null || mapTextures == null || mapTextures.Length == 0)
        {
            return;
        }

        int textureIndex = Mathf.Clamp(completedCycles, 0, mapTextures.Length - 1);
        if (textureIndex == appliedTextureIndex)
        {
            return;
        }

        if (runtimeMapMaterial == null)
        {
            runtimeMapMaterial = mapRenderer.material;
        }

        runtimeMapMaterial.mainTexture = mapTextures[textureIndex].texture;
        appliedTextureIndex = textureIndex;
    }

    private void ApplyScale(float targetScale)
    {
        quadTransform.localScale = new Vector3(targetScale, targetScale, targetScale);
    }
}

[Serializable]
public class Map
{
    public Texture texture;
    public float height;
}

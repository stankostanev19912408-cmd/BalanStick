using System;
using UnityEngine;

public class MapController : MonoBehaviour
{
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
    private static readonly int BaseMapPropertyId = Shader.PropertyToID("_BaseMap");

    [Header("References")]
    [SerializeField] private Transform quadTransform;
    [SerializeField] private MeshRenderer mapRenderer, bottomMapRenderer;
    [SerializeField] private ScoreCouter scoreCouter;
    [SerializeField] private Map[] mapTextures;

    [Header("Scale")]
    [SerializeField, Min(0.01f)] private float minScale = 10f;
    [SerializeField, Min(0.01f)] private float maxScale = 30f;
    [SerializeField, Min(1)] private int pointsPerScaleStep = 100; 

    private Material runtimeMapMaterial;
    private Material runtimeBottomMapMaterial;
    private int appliedTextureIndex = -1;
    private int appliedBottomTextureIndex = -1;

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

        if (bottomMapRenderer == null)
        {
            Debug.LogWarning("MapController: bottomMapRenderer is not assigned.", this);
        }

        if (mapTextures == null || mapTextures.Length == 0)
        {
            Debug.LogWarning("MapController: mapTextures is empty.", this);
        }
    }

    private void OnEnable()
    {
        appliedTextureIndex = -1;
        appliedBottomTextureIndex = -1;
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
                ApplyBottomTexture(-1);
                ApplyCrossFade(0f, false);
                return;
            }

            float cycleProgress = Mathf.Clamp01(score / (scaleRange * Mathf.Max(1, pointsPerScaleStep)));
            ApplyScale(GetExponentialCycleScale(cycleProgress));
            ApplyBottomTexture(-1);
            ApplyCrossFade(0f, false);
            return;
        }

        int lastTextureIndex = mapTextures.Length - 1;
        int activeTextureIndex = GetTextureIndexForScore(score);
        float rangeStartScore = GetTextureStartScore(activeTextureIndex);
        float rangeEndScore = GetTextureEndScore(activeTextureIndex);

        if (activeTextureIndex == lastTextureIndex && score > rangeEndScore)
        {
            ApplyScale(minScale);
            ApplyTexture(lastTextureIndex);
            ApplyBottomTexture(-1);
            ApplyCrossFade(0f, false);
            return;
        }

        float cycleProgress01 = GetRangeProgress(score, rangeStartScore, rangeEndScore);
        float targetScale = GetTextureScale(activeTextureIndex, cycleProgress01);
        ApplyScale(targetScale);

        ApplyTexture(activeTextureIndex);
        ApplyBottomTexture(GetNextTextureIndex(activeTextureIndex));
        ApplyCrossFade(cycleProgress01, activeTextureIndex < lastTextureIndex);
    }

    private float GetExponentialCycleScale(float cycleProgress01)
    {
        cycleProgress01 = Mathf.Clamp01(cycleProgress01);
        return maxScale * Mathf.Pow(minScale / maxScale, cycleProgress01);
    }

    private float GetTextureScale(int textureIndex, float cycleProgress01)
    {
        cycleProgress01 = Mathf.Clamp01(cycleProgress01);

        float scoreRange = GetTextureScoreRange(textureIndex);
        if (scoreRange <= 0f || minScale <= 0f || maxScale <= 0f)
        {
            return minScale;
        }

        float logMaxScale = Mathf.Log(maxScale);
        float logMinScale = Mathf.Log(minScale);
        float startLogSpeed = GetEdgeLogSpeed(textureIndex);
        float endLogSpeed = GetEdgeLogSpeed(textureIndex + 1);
        float deltaLogScale = logMinScale - logMaxScale;

        float startTangent = -startLogSpeed * scoreRange;
        float endTangent = -endLogSpeed * scoreRange;
        ClampHermiteTangents(deltaLogScale, ref startTangent, ref endTangent);

        float logScale = EvaluateHermite(logMaxScale, logMinScale, startTangent, endTangent, cycleProgress01);
        return Mathf.Exp(logScale);
    }

    private float GetEdgeLogSpeed(int edgeIndex)
    {
        int lastTextureIndex = mapTextures.Length - 1;
        if (edgeIndex <= 0)
        {
            return GetAverageTextureLogSpeed(0);
        }

        if (edgeIndex >= mapTextures.Length)
        {
            return GetAverageTextureLogSpeed(lastTextureIndex);
        }

        float previousSpeed = GetAverageTextureLogSpeed(edgeIndex - 1);
        float nextSpeed = GetAverageTextureLogSpeed(edgeIndex);
        return GetHarmonicMean(previousSpeed, nextSpeed);
    }

    private float GetAverageTextureLogSpeed(int textureIndex)
    {
        float scoreRange = GetTextureScoreRange(textureIndex);
        if (scoreRange <= 0f || minScale <= 0f || maxScale <= 0f)
        {
            return 0f;
        }

        return Mathf.Max(0f, Mathf.Log(maxScale) - Mathf.Log(minScale)) / scoreRange;
    }

    private float GetHarmonicMean(float a, float b)
    {
        if (a <= 0f || b <= 0f)
        {
            return 0f;
        }

        return 2f * a * b / (a + b);
    }

    private float GetTextureScoreRange(int textureIndex)
    {
        float startScore = GetTextureStartScore(textureIndex);
        float endScore = GetTextureEndScore(textureIndex);
        return Mathf.Max(0.0001f, endScore - startScore);
    }

    private void ClampHermiteTangents(float delta, ref float startTangent, ref float endTangent)
    {
        if (Mathf.Approximately(delta, 0f))
        {
            startTangent = 0f;
            endTangent = 0f;
            return;
        }

        float startRatio = startTangent / delta;
        float endRatio = endTangent / delta;
        if (startRatio < 0f || endRatio < 0f)
        {
            startTangent = 0f;
            endTangent = 0f;
            return;
        }

        float ratioSum = startRatio + endRatio;
        if (ratioSum <= 3f)
        {
            return;
        }

        float scale = 3f / ratioSum;
        startTangent *= scale;
        endTangent *= scale;
    }

    private float EvaluateHermite(float startValue, float endValue, float startTangent, float endTangent, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        float h00 = (2f * t3) - (3f * t2) + 1f;
        float h10 = t3 - (2f * t2) + t;
        float h01 = (-2f * t3) + (3f * t2);
        float h11 = t3 - t2;

        return (h00 * startValue) + (h10 * startTangent) + (h01 * endValue) + (h11 * endTangent);
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

    private int GetNextTextureIndex(int textureIndex)
    {
        int nextTextureIndex = textureIndex + 1;
        return nextTextureIndex < mapTextures.Length ? nextTextureIndex : -1;
    }

    private void ApplyTexture(int textureIndex)
    {
        if (mapRenderer == null || mapTextures == null || mapTextures.Length == 0)
        {
            return;
        }

        textureIndex = Mathf.Clamp(textureIndex, 0, mapTextures.Length - 1);
        if (textureIndex == appliedTextureIndex)
        {
            return;
        }

        EnsureTopMaterial();
        SetMaterialTexture(runtimeMapMaterial, mapTextures[textureIndex].texture);
        appliedTextureIndex = textureIndex;
    }

    private void ApplyBottomTexture(int textureIndex)
    {
        if (bottomMapRenderer == null)
        {
            return;
        }

        if (textureIndex == appliedBottomTextureIndex)
        {
            return;
        }

        EnsureBottomMaterial();

        if (textureIndex < 0 || mapTextures == null || mapTextures.Length == 0)
        {
            SetMaterialTexture(runtimeBottomMapMaterial, null);
            appliedBottomTextureIndex = -1;
            return;
        }

        textureIndex = Mathf.Clamp(textureIndex, 0, mapTextures.Length - 1);
        SetMaterialTexture(runtimeBottomMapMaterial, mapTextures[textureIndex].texture);
        appliedBottomTextureIndex = textureIndex;
    }

    private void ApplyCrossFade(float progress01, bool hasNextTexture)
    {
        EnsureTopMaterial();
        EnsureBottomMaterial();

        float topAlpha = 1f;
        float bottomAlpha = 0f;
        if (hasNextTexture)
        {
            progress01 = Mathf.Clamp01(progress01);
            topAlpha = 1f - progress01;
            bottomAlpha = progress01;
        }

        SetMaterialAlpha(runtimeMapMaterial, topAlpha);
        SetMaterialAlpha(runtimeBottomMapMaterial, bottomAlpha);
    }

    private void EnsureTopMaterial()
    {
        if (runtimeMapMaterial == null && mapRenderer != null)
        {
            runtimeMapMaterial = mapRenderer.material;
        }
    }

    private void EnsureBottomMaterial()
    {
        if (runtimeBottomMapMaterial == null && bottomMapRenderer != null)
        {
            runtimeBottomMapMaterial = bottomMapRenderer.material;
        }
    }

    private void SetMaterialTexture(Material material, Texture texture)
    {
        if (material == null)
        {
            return;
        }

        material.mainTexture = texture;
        if (material.HasProperty(BaseMapPropertyId))
        {
            material.SetTexture(BaseMapPropertyId, texture);
        }
    }

    private void SetMaterialAlpha(Material material, float alpha)
    {
        if (material == null)
        {
            return;
        }

        alpha = Mathf.Clamp01(alpha);
        if (material.HasProperty(ColorPropertyId))
        {
            Color color = material.GetColor(ColorPropertyId);
            color.a = alpha;
            material.SetColor(ColorPropertyId, color);
        }

        if (material.HasProperty(BaseColorPropertyId))
        {
            Color baseColor = material.GetColor(BaseColorPropertyId);
            baseColor.a = alpha;
            material.SetColor(BaseColorPropertyId, baseColor);
        }
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

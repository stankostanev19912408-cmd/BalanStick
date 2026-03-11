using UnityEngine;

public class MapController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform quadTransform;
    [SerializeField] private MeshRenderer mapRenderer;
    [SerializeField] private ScoreCouter scoreCouter;
    [SerializeField] private Texture[] mapTextures;

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
            ApplyScale(Mathf.Clamp(maxScale - (score / pointsPerScaleStep), minScale, maxScale));
            return;
        }

        float cyclePoints = GetCyclePoints();
        if (cyclePoints <= 0f)
        {
            ApplyScale(maxScale);
            return;
        }

        int completedCycles = Mathf.FloorToInt(score / cyclePoints);
        int lastTextureIndex = mapTextures.Length - 1;
        if (completedCycles >= lastTextureIndex)
        {
            float lastCycleStartScore = cyclePoints * lastTextureIndex;
            float scoreOnLastTexture = Mathf.Max(0f, score - lastCycleStartScore);
            ApplyScale(GetAsymptoticScale(scoreOnLastTexture));
            ApplyTexture(lastTextureIndex);
            return;
        }

        float currentCycleScore = Mathf.Repeat(score, cyclePoints);
        float scaleOffset = currentCycleScore / pointsPerScaleStep;
        float targetScale = Mathf.Clamp(maxScale - scaleOffset, minScale, maxScale);
        ApplyScale(targetScale);

        ApplyTexture(completedCycles);
    }

    private float GetCyclePoints()
    {
        float scaleRange = Mathf.Max(0f, maxScale - minScale);
        return scaleRange * pointsPerScaleStep;
    }

    private float GetAsymptoticScale(float scoreOnLastTexture)
    {
        float scaleRange = Mathf.Max(0f, maxScale - minScale);
        if (scaleRange <= 0f)
        {
            return minScale;
        }

        float normalizedProgress = scoreOnLastTexture / Mathf.Max(1, pointsPerScaleStep);
        float targetScale = minScale + (scaleRange / (1f + normalizedProgress));
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

        runtimeMapMaterial.mainTexture = mapTextures[textureIndex];
        appliedTextureIndex = textureIndex;
    }

    private void ApplyScale(float targetScale)
    {
        quadTransform.localScale = new Vector3(targetScale, targetScale, targetScale);
    }
}

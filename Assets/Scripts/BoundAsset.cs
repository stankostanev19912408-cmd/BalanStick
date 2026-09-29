using UnityEngine;

[CreateAssetMenu(fileName = "Bound", menuName = "BalanStick/Progression/Bound")]
public sealed class BoundAsset : ScriptableObject
{
    [SerializeField] private string boundName;
    [SerializeField, Min(0f)] private float heightFrom;
    [SerializeField, Min(0f)] private float heightTo;
    [SerializeField, Min(0)] private int patternCount = 1;
    [SerializeField, Min(0.0001f)] private float growthSpeedMultiplier = 1f;

    public string BoundName => boundName;
    public float HeightFrom => heightFrom;
    public float HeightTo => heightTo;
    public int PatternCount => patternCount;
    public float GrowthSpeedMultiplier => growthSpeedMultiplier;

    private void OnValidate()
    {
        if (float.IsNaN(heightFrom) || float.IsInfinity(heightFrom) ||
            float.IsNaN(heightTo) || float.IsInfinity(heightTo) ||
            heightFrom < 0f || heightTo <= heightFrom)
        {
            Debug.LogError($"Bound '{name}' has an invalid height range.", this);
        }

        if (float.IsNaN(growthSpeedMultiplier) || float.IsInfinity(growthSpeedMultiplier) ||
            growthSpeedMultiplier <= 0f)
        {
            Debug.LogError($"Bound '{name}' needs a positive growth speed multiplier.", this);
        }

        if (patternCount < 0)
        {
            Debug.LogError($"Bound '{name}' cannot have a negative pattern count.", this);
        }
    }
}

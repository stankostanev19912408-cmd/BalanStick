using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Bound", menuName = "BalanStick/Progression/Bound")]
public sealed class BoundAsset : ScriptableObject
{
    [SerializeField] private string boundName;
    [SerializeField, Min(0f)] private float heightFrom;
    [SerializeField, Min(0f)] private float heightTo;
    [SerializeField] private List<PatternAsset> patterns = new List<PatternAsset>();
    [SerializeField, Min(0.0001f)] private float growthSpeedMultiplier = 1f;

    public string BoundName => boundName;
    public float HeightFrom => heightFrom;
    public float HeightTo => heightTo;
    public IReadOnlyList<PatternAsset> Patterns => patterns;
    public int PatternCount => patterns != null ? patterns.Count : 0;
    public float GrowthSpeedMultiplier => growthSpeedMultiplier;
}

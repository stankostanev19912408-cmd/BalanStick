using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameProgression", menuName = "BalanStick/Progression/Game Progression")]
public sealed class GameProgressionAsset : ScriptableObject
{
    [SerializeField] private List<BoundAsset> bounds = new List<BoundAsset>();

    public IReadOnlyList<BoundAsset> Bounds => bounds;
}

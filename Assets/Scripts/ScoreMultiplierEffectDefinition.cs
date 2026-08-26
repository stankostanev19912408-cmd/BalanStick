using UnityEngine;

[CreateAssetMenu(menuName = "BalanStick/Effects/Score Multiplier", fileName = "ScoreMultiplierEffect")]
public sealed class ScoreMultiplierEffectDefinition : GameplayEffectDefinition
{
    [SerializeField, Min(1f)] private float multiplierPerStack = 2f;
    [SerializeField, Min(1)] private int maximumStackCount = 3;

    public override GameplayEffectRuntime CreateRuntime(GameplayEffectContext context)
    {
        return new ScoreMultiplierRuntime(
            this,
            context,
            Mathf.Max(1f, multiplierPerStack),
            Mathf.Max(1, maximumStackCount));
    }

    private sealed class ScoreMultiplierRuntime : GameplayEffectRuntime
    {
        private readonly float multiplierPerStack;
        private readonly int maximumStackCount;

        public ScoreMultiplierRuntime(
            GameplayEffectDefinition definition,
            GameplayEffectContext context,
            float multiplierPerStack,
            int maximumStackCount)
            : base(definition, context)
        {
            this.multiplierPerStack = multiplierPerStack;
            this.maximumStackCount = maximumStackCount;
        }

        public override float ScoreMultiplier => Mathf.Pow(multiplierPerStack, StackCount);
        public override int MaximumStackCount => maximumStackCount;
    }
}

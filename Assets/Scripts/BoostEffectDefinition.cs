using UnityEngine;

[CreateAssetMenu(menuName = "BalanStick/Effects/Boost", fileName = "Boost")]
public sealed class BoostEffectDefinition : GameplayEffectDefinition
{
    [SerializeField, Min(1f)] private float initialMultiplier = 3f;
    [SerializeField, Min(1f)] private float additionalStackMultiplier = 2f;
    [SerializeField, Min(1)] private int maximumStackCount = 3;

    public override GameplayEffectRuntime CreateRuntime(GameplayEffectContext context)
    {
        return new BoostRuntime(
            this,
            context,
            Mathf.Max(1f, initialMultiplier),
            Mathf.Max(1f, additionalStackMultiplier),
            Mathf.Max(1, maximumStackCount));
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        initialMultiplier = Mathf.Max(1f, initialMultiplier);
        additionalStackMultiplier = Mathf.Max(1f, additionalStackMultiplier);
        maximumStackCount = Mathf.Max(1, maximumStackCount);
    }

    private sealed class BoostRuntime : GameplayEffectRuntime
    {
        private readonly float initialMultiplier;
        private readonly float additionalStackMultiplier;
        private readonly int maximumStackCount;

        public BoostRuntime(
            GameplayEffectDefinition definition,
            GameplayEffectContext context,
            float initialMultiplier,
            float additionalStackMultiplier,
            int maximumStackCount)
            : base(definition, context)
        {
            this.initialMultiplier = initialMultiplier;
            this.additionalStackMultiplier = additionalStackMultiplier;
            this.maximumStackCount = maximumStackCount;
        }

        public override float FixedScoreRateMultiplier =>
            initialMultiplier * Mathf.Pow(additionalStackMultiplier, StackCount - 1);

        public override int MaximumStackCount => maximumStackCount;
    }
}

using UnityEngine;

[CreateAssetMenu(menuName = "BalanStick/Effects/Input Inversion", fileName = "InputInversionEffect")]
public sealed class InputInversionEffectDefinition : GameplayEffectDefinition
{
    [SerializeField] private bool invertX = true;
    [SerializeField] private bool invertZ = true;

    public override GameplayEffectRuntime CreateRuntime(GameplayEffectContext context)
    {
        return new InputInversionRuntime(this, context, invertX, invertZ);
    }

    private sealed class InputInversionRuntime : GameplayEffectRuntime
    {
        private readonly bool invertX;
        private readonly bool invertZ;

        public InputInversionRuntime(
            GameplayEffectDefinition definition,
            GameplayEffectContext context,
            bool invertX,
            bool invertZ)
            : base(definition, context)
        {
            this.invertX = invertX;
            this.invertZ = invertZ;
        }

        public override bool InvertInputX => invertX;
        public override bool InvertInputZ => invertZ;
    }
}

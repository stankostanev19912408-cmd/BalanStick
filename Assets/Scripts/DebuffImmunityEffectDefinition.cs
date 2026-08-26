using UnityEngine;

[CreateAssetMenu(menuName = "BalanStick/Effects/Debuff Immunity", fileName = "DebuffImmunityEffect")]
public sealed class DebuffImmunityEffectDefinition : GameplayEffectDefinition
{
    public override GameplayEffectRuntime CreateRuntime(GameplayEffectContext context)
    {
        return new DebuffImmunityRuntime(this, context);
    }

    private sealed class DebuffImmunityRuntime : GameplayEffectRuntime
    {
        public DebuffImmunityRuntime(GameplayEffectDefinition definition, GameplayEffectContext context)
            : base(definition, context)
        {
        }

        public override bool BlocksDebuffs => true;
    }
}

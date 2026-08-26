using UnityEngine;

[CreateAssetMenu(menuName = "BalanStick/Effects/Tilt Protection", fileName = "TiltProtectionEffect")]
public sealed class TiltProtectionEffectDefinition : GameplayEffectDefinition
{
    [SerializeField, Range(0f, 90f)] private float maximumTiltAngle = 10f;

    public override GameplayEffectRuntime CreateRuntime(GameplayEffectContext context)
    {
        return new TiltProtectionRuntime(this, context, Mathf.Clamp(maximumTiltAngle, 0f, 90f));
    }

    private sealed class TiltProtectionRuntime : GameplayEffectRuntime
    {
        private readonly float maximumTiltAngle;

        public TiltProtectionRuntime(
            GameplayEffectDefinition definition,
            GameplayEffectContext context,
            float maximumTiltAngle)
            : base(definition, context)
        {
            this.maximumTiltAngle = maximumTiltAngle;
        }

        public override float MaximumTiltAngle => maximumTiltAngle;
    }
}

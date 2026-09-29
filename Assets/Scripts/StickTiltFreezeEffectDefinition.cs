using UnityEngine;

[CreateAssetMenu(menuName = "BalanStick/Effects/Stick Tilt Freeze", fileName = "StickTiltFreezeEffect")]
public sealed class StickTiltFreezeEffectDefinition : GameplayEffectDefinition
{
    public override GameplayEffectRuntime CreateRuntime(GameplayEffectContext context)
    {
        return new StickTiltFreezeRuntime(this, context);
    }

    private sealed class StickTiltFreezeRuntime : GameplayEffectRuntime
    {
        private Quaternion frozenSwing;

        public StickTiltFreezeRuntime(GameplayEffectDefinition definition, GameplayEffectContext context)
            : base(definition, context)
        {
        }

        public override bool FreezesStickTilt => true;

        public override void OnApply()
        {
            Rigidbody body = Context.StickRigidbody;
            if (body == null)
            {
                return;
            }

            frozenSwing = Quaternion.FromToRotation(Vector3.up, Context.StickTransform.up);
            if (!body.isKinematic)
            {
                body.angularVelocity = Vector3.zero;
            }
        }

        public override void OnFixedTick(float fixedDeltaTime)
        {
            Rigidbody body = Context.StickRigidbody;
            if (body == null || body.isKinematic)
            {
                return;
            }

            body.angularVelocity = Vector3.zero;
            Quaternion currentSwing = Quaternion.FromToRotation(Vector3.up, Context.StickTransform.up);
            Quaternion currentTwist = Quaternion.Inverse(currentSwing) * body.rotation;
            body.MoveRotation(frozenSwing * currentTwist);
        }
    }
}

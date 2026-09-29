using UnityEngine;

[CreateAssetMenu(menuName = "BalanStick/Effects/Stick Stabilization", fileName = "StickStabilizationEffect")]
public sealed class StickStabilizationEffectDefinition : GameplayEffectDefinition
{
    [SerializeField, Min(0f)] private float transitionSeconds = 0.75f;

    public override GameplayEffectRuntime CreateRuntime(GameplayEffectContext context)
    {
        return new StickStabilizationRuntime(this, context, Mathf.Max(0f, transitionSeconds));
    }

    private sealed class StickStabilizationRuntime : GameplayEffectRuntime
    {
        private readonly float transitionSeconds;
        private Quaternion startRotation;
        private Quaternion targetRotation;
        private float elapsedSeconds;

        public StickStabilizationRuntime(
            GameplayEffectDefinition definition,
            GameplayEffectContext context,
            float transitionSeconds)
            : base(definition, context)
        {
            this.transitionSeconds = transitionSeconds;
        }

        public override bool StabilizesStick => true;

        public override void OnApply()
        {
            Rigidbody body = Context.StickRigidbody;
            if (body == null || Context.StickTransform == null)
            {
                return;
            }

            startRotation = body.rotation;
            targetRotation = Quaternion.FromToRotation(Context.StickTransform.up, Vector3.up) * startRotation;
            elapsedSeconds = 0f;
            if (!body.isKinematic)
            {
                body.velocity = Vector3.zero;
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

            elapsedSeconds += fixedDeltaTime;
            float t = transitionSeconds > 0f ? Mathf.Clamp01(elapsedSeconds / transitionSeconds) : 1f;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.MoveRotation(Quaternion.Slerp(startRotation, targetRotation, t));
        }
    }
}

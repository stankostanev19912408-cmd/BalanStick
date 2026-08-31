using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public sealed class GameplayEffectController : MonoBehaviour
{
    private sealed class ActiveEffect
    {
        public GameplayEffectRuntime Runtime;
        public float RemainingTime;
    }

    private readonly List<ActiveEffect> activeEffects = new List<ActiveEffect>();
    private StickTiltForce stickTiltForce;
    private Transform stickTransform;
    private Rigidbody stickRigidbody;

    public event Action EffectsChanged;

    public float CurrentFixedScoreRateMultiplier
    {
        get
        {
            float multiplier = 0f;
            for (int i = 0; i < activeEffects.Count; i++)
            {
                GameplayEffectRuntime runtime = activeEffects[i].Runtime;
                if (!IsSuppressedDebuff(runtime))
                {
                    multiplier = Mathf.Max(multiplier, runtime.FixedScoreRateMultiplier);
                }
            }

            return multiplier;
        }
    }

    public bool AreDebuffsBlocked
    {
        get
        {
            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i].Runtime.BlocksDebuffs)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public bool IsInputXInverted => HasInputInversion(true);
    public bool IsInputZInverted => HasInputInversion(false);

    public bool IsEffectActive(GameplayEffectDefinition definition)
    {
        return TryGetActiveEffect(definition, out _);
    }

    public bool TryGetEffectTiming(
        GameplayEffectDefinition definition,
        out float remainingTime,
        out float duration)
    {
        duration = definition != null ? definition.DurationSeconds : 0f;
        if (!TryGetActiveEffect(definition, out ActiveEffect activeEffect))
        {
            remainingTime = 0f;
            return false;
        }

        remainingTime = Mathf.Clamp(activeEffect.RemainingTime, 0f, duration);
        return true;
    }

    public void Configure(StickTiltForce sourceStickTiltForce)
    {
        if (stickTiltForce == sourceStickTiltForce)
        {
            return;
        }

        UnbindRetryEvents();
        stickTiltForce = sourceStickTiltForce;
        stickTransform = stickTiltForce != null ? stickTiltForce.transform : null;
        stickRigidbody = stickTransform != null ? stickTransform.GetComponent<Rigidbody>() : null;
        BindRetryEvents();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveEffect activeEffect = activeEffects[i];
            activeEffect.Runtime.OnTick(deltaTime);
            activeEffect.RemainingTime -= deltaTime;
            if (activeEffect.RemainingTime <= 0f)
            {
                RemoveAt(i);
            }
        }
    }

    private void FixedUpdate()
    {
        ApplyTiltProtection();
    }

    private void OnDestroy()
    {
        UnbindRetryEvents();
        ClearAll();
    }

    public bool TryApply(GameplayEffectDefinition definition)
    {
        if (definition == null)
        {
            return false;
        }

        if (definition.Polarity == GameplayEffectPolarity.Debuff && AreDebuffsBlocked)
        {
            return false;
        }

        for (int i = 0; i < activeEffects.Count; i++)
        {
            ActiveEffect activeEffect = activeEffects[i];
            if (activeEffect.Runtime.Definition != definition)
            {
                continue;
            }

            if (definition.StackingMode == GameplayEffectStackingMode.StackMultiplicativelyAndRefresh)
            {
                activeEffect.Runtime.AddStack();
            }

            activeEffect.RemainingTime = definition.DurationSeconds;
            EffectsChanged?.Invoke();
            return true;
        }

        GameplayEffectContext context = new GameplayEffectContext(stickTransform, stickRigidbody);
        GameplayEffectRuntime runtime = definition.CreateRuntime(context);
        if (runtime == null)
        {
            return false;
        }

        runtime.OnApply();
        if (definition.DurationSeconds <= 0f)
        {
            runtime.OnRemove();
            EffectsChanged?.Invoke();
            return true;
        }

        activeEffects.Add(new ActiveEffect
        {
            Runtime = runtime,
            RemainingTime = definition.DurationSeconds
        });
        EffectsChanged?.Invoke();
        return true;
    }

    public void ClearAll()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            activeEffects[i].Runtime.OnRemove();
        }

        if (activeEffects.Count == 0)
        {
            return;
        }

        activeEffects.Clear();
        EffectsChanged?.Invoke();
    }

    private bool TryGetActiveEffect(
        GameplayEffectDefinition definition,
        out ActiveEffect activeEffect)
    {
        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (activeEffects[i].Runtime.Definition == definition)
            {
                activeEffect = activeEffects[i];
                return true;
            }
        }

        activeEffect = null;
        return false;
    }

    private bool HasInputInversion(bool checkX)
    {
        for (int i = 0; i < activeEffects.Count; i++)
        {
            GameplayEffectRuntime runtime = activeEffects[i].Runtime;
            if (IsSuppressedDebuff(runtime))
            {
                continue;
            }

            if (checkX ? runtime.InvertInputX : runtime.InvertInputZ)
            {
                return true;
            }
        }

        return false;
    }

    private float GetMaximumTiltAngle()
    {
        float maximumTiltAngle = float.PositiveInfinity;
        for (int i = 0; i < activeEffects.Count; i++)
        {
            GameplayEffectRuntime runtime = activeEffects[i].Runtime;
            if (!IsSuppressedDebuff(runtime))
            {
                maximumTiltAngle = Mathf.Min(maximumTiltAngle, runtime.MaximumTiltAngle);
            }
        }

        return maximumTiltAngle;
    }

    private bool IsSuppressedDebuff(GameplayEffectRuntime runtime)
    {
        return AreDebuffsBlocked && runtime.Definition.Polarity == GameplayEffectPolarity.Debuff;
    }

    private void ApplyTiltProtection()
    {
        if (stickTransform == null || stickRigidbody == null)
        {
            return;
        }

        float maximumTiltAngle = GetMaximumTiltAngle();
        if (float.IsPositiveInfinity(maximumTiltAngle))
        {
            return;
        }

        Vector3 currentUp = stickTransform.up;
        float currentTiltAngle = Vector3.Angle(Vector3.up, currentUp);
        if (currentTiltAngle <= maximumTiltAngle)
        {
            return;
        }

        Quaternion currentSwing = Quaternion.FromToRotation(Vector3.up, currentUp);
        currentSwing.ToAngleAxis(out _, out Vector3 swingAxis);
        if (swingAxis.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        Quaternion twist = Quaternion.Inverse(currentSwing) * stickRigidbody.rotation;
        Quaternion clampedSwing = Quaternion.AngleAxis(maximumTiltAngle, swingAxis.normalized);
        stickRigidbody.MoveRotation(clampedSwing * twist);
        stickRigidbody.angularVelocity = Vector3.zero;
    }

    private void RemoveAt(int index)
    {
        activeEffects[index].Runtime.OnRemove();
        activeEffects.RemoveAt(index);
        EffectsChanged?.Invoke();
    }

    private void BindRetryEvents()
    {
        if (stickTiltForce == null)
        {
            return;
        }

        stickTiltForce.RetryStateChanged -= HandleRetryStateChanged;
        stickTiltForce.RetryStateChanged += HandleRetryStateChanged;
    }

    private void UnbindRetryEvents()
    {
        if (stickTiltForce != null)
        {
            stickTiltForce.RetryStateChanged -= HandleRetryStateChanged;
        }
    }

    private void HandleRetryStateChanged(bool retryRequired)
    {
        if (retryRequired)
        {
            ClearAll();
        }
    }
}

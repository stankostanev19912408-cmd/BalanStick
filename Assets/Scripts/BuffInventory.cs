using System;
using UnityEngine;

public sealed class BuffInventory : MonoBehaviour
{
    public const int SlotCount = 3;

    private readonly GameplayEffectDefinition[] slots = new GameplayEffectDefinition[SlotCount];
    private readonly bool[] activeSlots = new bool[SlotCount];
    private GameplayEffectController effectController;
    private StickTiltForce stickTiltForce;

    public event Action SlotsChanged;
    public GameplayEffectController EffectController => effectController;

    public void Configure(GameplayEffectController sourceEffectController, StickTiltForce sourceStickTiltForce)
    {
        if (effectController != sourceEffectController)
        {
            UnbindEffectEvents();
            effectController = sourceEffectController;
            BindEffectEvents();
        }

        if (stickTiltForce == sourceStickTiltForce)
        {
            return;
        }

        UnbindRetryEvents();
        stickTiltForce = sourceStickTiltForce;
        BindRetryEvents();
    }

    private void OnDestroy()
    {
        UnbindEffectEvents();
        UnbindRetryEvents();
    }

    public GameplayEffectDefinition GetSlot(int index)
    {
        return index >= 0 && index < slots.Length ? slots[index] : null;
    }

    public bool IsSlotActive(int index)
    {
        return index >= 0 && index < activeSlots.Length && activeSlots[index];
    }

    public bool TryAdd(GameplayEffectDefinition definition)
    {
        if (definition == null || definition.Polarity != GameplayEffectPolarity.Buff)
        {
            return false;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                continue;
            }

            slots[i] = definition;
            SlotsChanged?.Invoke();
            return true;
        }

        return false;
    }

    public bool ActivateSlot(int index)
    {
        if (index < 0 || index >= slots.Length || slots[index] == null ||
            activeSlots[index] || effectController == null)
        {
            return false;
        }

        GameplayEffectDefinition definition = slots[index];
        if (!effectController.TryApply(definition))
        {
            return false;
        }

        ClearPreviousActiveSlot(definition, index);
        if (effectController.IsEffectActive(definition))
        {
            activeSlots[index] = true;
        }
        else
        {
            slots[index] = null;
        }

        SlotsChanged?.Invoke();
        return true;
    }

    public void Clear()
    {
        bool hadBuffs = false;
        for (int i = 0; i < slots.Length; i++)
        {
            hadBuffs |= slots[i] != null || activeSlots[i];
            slots[i] = null;
            activeSlots[i] = false;
        }

        if (hadBuffs)
        {
            SlotsChanged?.Invoke();
        }
    }

    private void ClearPreviousActiveSlot(GameplayEffectDefinition definition, int exceptIndex)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (i == exceptIndex || !activeSlots[i] || slots[i] != definition)
            {
                continue;
            }

            slots[i] = null;
            activeSlots[i] = false;
        }
    }

    private void BindEffectEvents()
    {
        if (effectController != null)
        {
            effectController.EffectsChanged -= HandleEffectsChanged;
            effectController.EffectsChanged += HandleEffectsChanged;
        }
    }

    private void UnbindEffectEvents()
    {
        if (effectController != null)
        {
            effectController.EffectsChanged -= HandleEffectsChanged;
        }
    }

    private void HandleEffectsChanged()
    {
        bool changed = false;
        for (int i = 0; i < slots.Length; i++)
        {
            if (!activeSlots[i] || effectController.IsEffectActive(slots[i]))
            {
                continue;
            }

            slots[i] = null;
            activeSlots[i] = false;
            changed = true;
        }

        if (changed)
        {
            SlotsChanged?.Invoke();
        }
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
            Clear();
        }
    }
}

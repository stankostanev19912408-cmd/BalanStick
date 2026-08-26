using System;
using UnityEngine;

public sealed class BuffInventory : MonoBehaviour
{
    public const int SlotCount = 3;

    private readonly GameplayEffectDefinition[] slots = new GameplayEffectDefinition[SlotCount];
    private GameplayEffectController effectController;
    private StickTiltForce stickTiltForce;

    public event Action SlotsChanged;

    public void Configure(GameplayEffectController sourceEffectController, StickTiltForce sourceStickTiltForce)
    {
        effectController = sourceEffectController;
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
        UnbindRetryEvents();
    }

    public GameplayEffectDefinition GetSlot(int index)
    {
        return index >= 0 && index < slots.Length ? slots[index] : null;
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
        if (index < 0 || index >= slots.Length || slots[index] == null || effectController == null)
        {
            return false;
        }

        GameplayEffectDefinition definition = slots[index];
        if (!effectController.TryApply(definition))
        {
            return false;
        }

        slots[index] = null;
        SlotsChanged?.Invoke();
        return true;
    }

    public void Clear()
    {
        bool hadBuffs = false;
        for (int i = 0; i < slots.Length; i++)
        {
            hadBuffs |= slots[i] != null;
            slots[i] = null;
        }

        if (hadBuffs)
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

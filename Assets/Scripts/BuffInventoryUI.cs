using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class BuffInventoryUI : MonoBehaviour
{
    [SerializeField] private Button[] slotButtons = new Button[BuffInventory.SlotCount];
    [SerializeField] private TMP_Text[] slotLabels = new TMP_Text[BuffInventory.SlotCount];

    private readonly UnityAction[] slotClickHandlers = new UnityAction[BuffInventory.SlotCount];
    private readonly EffectButtonUI[] slotEffectViews = new EffectButtonUI[BuffInventory.SlotCount];
    private BuffInventory inventory;
    private bool listenersRegistered;

    public static BuffInventoryUI FindAndBind(BuffInventory sourceInventory)
    {
        if (sourceInventory == null)
        {
            return null;
        }

        BuffInventoryUI ui = Object.FindObjectOfType<BuffInventoryUI>(true);
        if (ui == null)
        {
            Debug.LogWarning("BuffInventoryUI: a configured UI instance was not found in the scene.");
            return null;
        }

        if (!ui.gameObject.activeSelf)
        {
            ui.gameObject.SetActive(true);
        }

        ui.Bind(sourceInventory);
        return ui;
    }

    private void Awake()
    {
        CacheEffectButtonViews();
        RegisterButtonListeners();
    }

    private void OnDestroy()
    {
        UnregisterButtonListeners();

        if (inventory != null)
        {
            inventory.SlotsChanged -= Refresh;
        }
    }

    private void Bind(BuffInventory sourceInventory)
    {
        CacheEffectButtonViews();
        if (!IsConfigured())
        {
            Debug.LogError(
                $"BuffInventoryUI: assign exactly {BuffInventory.SlotCount} buttons and labels, " +
                "and add EffectButtonUI to every button in the prefab.",
                this);
            return;
        }

        RegisterButtonListeners();

        if (inventory == sourceInventory)
        {
            Refresh();
            return;
        }

        if (inventory != null)
        {
            inventory.SlotsChanged -= Refresh;
        }

        inventory = sourceInventory;
        inventory.SlotsChanged += Refresh;
        Refresh();
    }

    private void RegisterButtonListeners()
    {
        if (listenersRegistered || !HasValidButtonArray())
        {
            return;
        }

        for (int i = 0; i < BuffInventory.SlotCount; i++)
        {
            int slotIndex = i;
            slotClickHandlers[i] = () => HandleSlotClicked(slotIndex);
            slotButtons[i].onClick.AddListener(slotClickHandlers[i]);
        }

        listenersRegistered = true;
    }

    private void UnregisterButtonListeners()
    {
        if (!listenersRegistered)
        {
            return;
        }

        for (int i = 0; i < BuffInventory.SlotCount; i++)
        {
            if (slotButtons[i] != null && slotClickHandlers[i] != null)
            {
                slotButtons[i].onClick.RemoveListener(slotClickHandlers[i]);
            }
        }

        listenersRegistered = false;
    }

    private void HandleSlotClicked(int slotIndex)
    {
        if (inventory != null && !inventory.IsSlotActive(slotIndex))
        {
            inventory.ActivateSlot(slotIndex);
        }
    }

    private void Refresh()
    {
        if (!IsConfigured())
        {
            return;
        }

        for (int i = 0; i < BuffInventory.SlotCount; i++)
        {
            GameplayEffectDefinition definition = inventory != null ? inventory.GetSlot(i) : null;
            bool hasBuff = definition != null;
            slotButtons[i].gameObject.SetActive(hasBuff);
            if (!hasBuff)
            {
                slotEffectViews[i].ResetView();
                continue;
            }

            if (slotButtons[i].targetGraphic != null)
            {
                slotButtons[i].targetGraphic.color = definition.UiColor;
            }

            slotLabels[i].text = definition.DisplayName;
            bool isActive = inventory.IsSlotActive(i);
            slotButtons[i].interactable = !isActive;
            if (isActive)
            {
                slotEffectViews[i].ShowActive(definition, inventory.EffectController);
            }
            else
            {
                slotEffectViews[i].ShowStored();
            }
        }
    }

    private bool IsConfigured()
    {
        if (!HasValidButtonArray() || !HasValidEffectViewArray() ||
            slotLabels == null || slotLabels.Length != BuffInventory.SlotCount)
        {
            return false;
        }

        for (int i = 0; i < BuffInventory.SlotCount; i++)
        {
            if (slotLabels[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private void CacheEffectButtonViews()
    {
        if (!HasValidButtonArray())
        {
            return;
        }

        for (int i = 0; i < BuffInventory.SlotCount; i++)
        {
            slotEffectViews[i] = slotButtons[i].GetComponent<EffectButtonUI>();
        }
    }

    private bool HasValidEffectViewArray()
    {
        for (int i = 0; i < slotEffectViews.Length; i++)
        {
            if (slotEffectViews[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private bool HasValidButtonArray()
    {
        if (slotButtons == null || slotButtons.Length != BuffInventory.SlotCount)
        {
            return false;
        }

        for (int i = 0; i < BuffInventory.SlotCount; i++)
        {
            if (slotButtons[i] == null)
            {
                return false;
            }
        }

        return true;
    }
}

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class EffectButtonUI : MonoBehaviour
{
    [SerializeField] private Image progressBar;

    private GameplayEffectDefinition activeEffect;
    private GameplayEffectController effectController;
    private bool isTrackingEffect;

    private void Awake()
    {
        SetFillAmount(1f);
    }

    private void Update()
    {
        if (!isTrackingEffect)
        {
            return;
        }

        UpdateProgress();
    }

    public void ShowStored()
    {
        StopTracking();
        SetFillAmount(1f);
    }

    public void ShowActive(
        GameplayEffectDefinition definition,
        GameplayEffectController sourceEffectController)
    {
        activeEffect = definition;
        effectController = sourceEffectController;
        isTrackingEffect = activeEffect != null && effectController != null;
        UpdateProgress();
    }

    public void ResetView()
    {
        StopTracking();
        SetFillAmount(1f);
    }

    private void UpdateProgress()
    {
        if (!isTrackingEffect ||
            !effectController.TryGetEffectTiming(activeEffect, out float remainingTime, out float duration))
        {
            SetFillAmount(0f);
            return;
        }

        SetFillAmount(duration > 0f ? remainingTime / duration : 0f);
    }

    private void StopTracking()
    {
        isTrackingEffect = false;
        activeEffect = null;
        effectController = null;
    }

    private void SetFillAmount(float value)
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = Mathf.Clamp01(value);
        }
    }
}

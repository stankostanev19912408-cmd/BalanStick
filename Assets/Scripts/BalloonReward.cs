using UnityEngine;

public enum BalloonRewardKind
{
    Currency = 0,
    Buff = 1,
    Debuff = 2
}

public readonly struct BalloonReward
{
    public BalloonReward(
        BalloonRewardKind kind,
        GameplayEffectDefinition effect,
        int currencyAmount,
        Color visualColor)
    {
        Kind = kind;
        Effect = effect;
        CurrencyAmount = Mathf.Max(0, currencyAmount);
        VisualColor = visualColor;
    }

    public BalloonRewardKind Kind { get; }
    public GameplayEffectDefinition Effect { get; }
    public int CurrencyAmount { get; }
    public Color VisualColor { get; }
}

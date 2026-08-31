using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ProgressionConfig", menuName = "Game/Progression Config")]
public class ProgressionConfig : ScriptableObject
{
    [SerializeField] private List<ProgressionLevelDefinition> levels = new List<ProgressionLevelDefinition>();

    public IReadOnlyList<ProgressionLevelDefinition> Levels => levels;

    public bool HasLevels => levels != null && levels.Count > 0;

    public int GetReachedLevel(int score)
    {
        if (!HasLevels)
        {
            return 0;
        }

        score = Mathf.Max(0, score);

        int reachedLevel = 0;
        for (int i = 0; i < levels.Count; i++)
        {
            if (score < levels[i].RequiredScore)
            {
                break;
            }

            reachedLevel = i + 1;
        }

        return reachedLevel;
    }

    public ProgressionLevelDefinition GetLevelDefinition(int levelNumber)
    {
        if (!HasLevels || levelNumber <= 0 || levelNumber > levels.Count)
        {
            return null;
        }

        return levels[levelNumber - 1];
    }

    public void GetUnlockedEffects(
        int unlockedLevel,
        GameplayEffectPolarity polarity,
        List<WeightedGameplayEffect> destination)
    {
        if (destination == null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        destination.Clear();
        if (!HasLevels)
        {
            return;
        }

        int unlockedLevelCount = Mathf.Clamp(unlockedLevel, 0, levels.Count);
        for (int i = 0; i < unlockedLevelCount; i++)
        {
            levels[i]?.AppendUnlockedEffects(polarity, destination);
        }
    }

    private void OnValidate()
    {
        if (levels == null)
        {
            levels = new List<ProgressionLevelDefinition>();
            return;
        }

        foreach (ProgressionLevelDefinition level in levels)
        {
            if (level == null)
            {
                continue;
            }

            level.ClampValues();
        }

        levels.Sort(CompareLevels);
    }

    private static int CompareLevels(ProgressionLevelDefinition left, ProgressionLevelDefinition right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        return left.RequiredScore.CompareTo(right.RequiredScore);
    }
}

[Serializable]
public class ProgressionLevelDefinition
{
    [SerializeField, Min(0)] private int requiredScore;
    [SerializeField] private string title;
    [SerializeField, TextArea] private string description;
    [SerializeField, Min(0)] private int softCurrencyReward;
    [SerializeField] private WeightedGameplayEffect[] unlockedBuffs = Array.Empty<WeightedGameplayEffect>();
    [SerializeField] private WeightedGameplayEffect[] unlockedDebuffs = Array.Empty<WeightedGameplayEffect>();

    public int RequiredScore => requiredScore;
    public string Title => title;
    public string Description => description;
    public int SoftCurrencyReward => softCurrencyReward;
    public IReadOnlyList<WeightedGameplayEffect> UnlockedBuffs => unlockedBuffs;
    public IReadOnlyList<WeightedGameplayEffect> UnlockedDebuffs => unlockedDebuffs;

    public void ClampValues()
    {
        requiredScore = Mathf.Max(0, requiredScore);
        softCurrencyReward = Mathf.Max(0, softCurrencyReward);

        unlockedBuffs ??= Array.Empty<WeightedGameplayEffect>();
        unlockedDebuffs ??= Array.Empty<WeightedGameplayEffect>();
    }

    public void AppendUnlockedEffects(
        GameplayEffectPolarity polarity,
        List<WeightedGameplayEffect> destination)
    {
        WeightedGameplayEffect[] source = polarity == GameplayEffectPolarity.Buff
            ? unlockedBuffs
            : unlockedDebuffs;

        if (source == null)
        {
            return;
        }

        for (int i = 0; i < source.Length; i++)
        {
            WeightedGameplayEffect candidate = source[i];
            if (candidate == null ||
                candidate.Effect == null ||
                candidate.Effect.Polarity != polarity ||
                ContainsEffect(destination, candidate.Effect))
            {
                continue;
            }

            destination.Add(candidate);
        }
    }

    private static bool ContainsEffect(
        List<WeightedGameplayEffect> effects,
        GameplayEffectDefinition definition)
    {
        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] != null && effects[i].Effect == definition)
            {
                return true;
            }
        }

        return false;
    }
}

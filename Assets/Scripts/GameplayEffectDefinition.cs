using UnityEngine;

public enum GameplayEffectPolarity
{
    Buff = 0,
    Debuff = 1
}

public enum GameplayEffectStackingMode
{
    RefreshDuration = 0,
    StackMultiplicativelyAndRefresh = 1
}

public abstract class GameplayEffectDefinition : ScriptableObject
{
    [Header("Presentation")]
    [SerializeField] private string displayName = "Effect";
    [SerializeField] private Color uiColor = Color.white;
    [SerializeField] private GameObject visualEffectPrefab;

    [Header("Runtime")]
    [SerializeField] private GameplayEffectPolarity polarity;
    [SerializeField, Min(0f)] private float durationSeconds = 10f;
    [SerializeField] private GameplayEffectStackingMode stackingMode;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public Color UiColor => uiColor;
    public GameObject VisualEffectPrefab => visualEffectPrefab;
    public GameplayEffectPolarity Polarity => polarity;
    public float DurationSeconds => Mathf.Max(0f, durationSeconds);
    public GameplayEffectStackingMode StackingMode => stackingMode;

    public abstract GameplayEffectRuntime CreateRuntime(GameplayEffectContext context);

    protected virtual void OnValidate()
    {
        durationSeconds = Mathf.Max(0f, durationSeconds);
    }
}

public sealed class GameplayEffectContext
{
    public GameplayEffectContext(Transform stickTransform, Rigidbody stickRigidbody)
    {
        StickTransform = stickTransform;
        StickRigidbody = stickRigidbody;
    }

    public Transform StickTransform { get; }
    public Rigidbody StickRigidbody { get; }
}

public abstract class GameplayEffectRuntime
{
    protected GameplayEffectRuntime(GameplayEffectDefinition definition, GameplayEffectContext context)
    {
        Definition = definition;
        Context = context;
    }

    public GameplayEffectDefinition Definition { get; }
    protected GameplayEffectContext Context { get; }
    public int StackCount { get; private set; } = 1;

    public virtual float FixedScoreRateMultiplier => 0f;
    public virtual bool BlocksDebuffs => false;
    public virtual bool InvertInputX => false;
    public virtual bool InvertInputZ => false;
    public virtual float MaximumTiltAngle => float.PositiveInfinity;
    public virtual int MaximumStackCount => int.MaxValue;

    public virtual void OnApply() { }
    public virtual void OnTick(float deltaTime) { }
    public virtual void OnRemove() { }

    public void AddStack()
    {
        StackCount = Mathf.Min(StackCount + 1, Mathf.Max(1, MaximumStackCount));
        OnStackCountChanged();
    }

    protected virtual void OnStackCountChanged() { }
}

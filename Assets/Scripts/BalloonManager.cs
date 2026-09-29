using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class BalloonManager : MonoBehaviour
{
    private const string MoneyTextName = "MoneyText";
    private const string ScoreTextName = "ScoreText";
    private const string MoneyTextRootName = "MoneyTextRoot";
    private const string ScoreTextRootName = "ScoreTextRoot";

    [Header("References")]
    [SerializeField] private Balloon balloonPrefab;
    [SerializeField] private Transform spawnRoot, targetSpawnRoot;
    [SerializeField] private StickTiltForce stickTiltForce;
    [FormerlySerializedAs("scoreCouter")]
    [SerializeField] private ScoreCounter scoreCounter;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private GameObject moneyTextRoot;
    [SerializeField] private GameplayEffectController gameplayEffectController;
    [SerializeField] private BuffInventory buffInventory;
    [SerializeField] private PatternEventScheduler patternEventScheduler;

    [Header("Balloon Settings")]
    [SerializeField] private AnimationCurve balloonSpeedCurve = AnimationCurve.Linear(0f, 0.6f, 1f, 0.6f);
    [SerializeField] private AnimationCurve balloonScaleCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    [SerializeField, Min(0f)] private float balloonSpeedMultiplier = 1f;
    [SerializeField, Min(0f)] private float balloonLifeTimeSeconds = 5f;
    [SerializeField, Min(0f)] private float indicatorWarningBeforeExpireSeconds = 1.5f;
    [SerializeField] private AnimationCurve indicatorScaleCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    [SerializeField, Min(0f)] private float stickPushForce = 2f;

    [Header("Rewards")]
    [SerializeField] private GameplayEffectDefinition[] availableBuffEffects;
    [SerializeField] private GameplayEffectDefinition[] availableDebuffEffects;

    [Header("Spawn Zone (Radial)")]
    [SerializeField] private Vector3 spawnAreaCenter = new Vector3(-1.5f, 0f, 0f);

    [Header("Retry")]
    [SerializeField] private bool clearBalloonsOnRetry = true;

    private bool isRetryRequired;
    private bool isInputUnlocked;
    private int touchedBalloonCount;

    private void Awake()
    {
        EnsureGameplayServices();
    }

    private void OnValidate()
    {
        balloonSpeedMultiplier = Mathf.Max(0f, balloonSpeedMultiplier);
        balloonLifeTimeSeconds = Mathf.Max(0f, balloonLifeTimeSeconds);
        indicatorWarningBeforeExpireSeconds = Mathf.Max(0f, indicatorWarningBeforeExpireSeconds);
        stickPushForce = Mathf.Max(0f, stickPushForce);
    }

    private void Start()
    {
        EnsureGameplayServices();
        ResolvePatternEventScheduler();
        ResolveUiReferences();
        BuffInventoryUI.FindAndBind(buffInventory);

        if (balloonPrefab == null)
        {
            Debug.LogWarning("BalloonManager: balloonPrefab is not assigned.", this);
        }

        if (stickTiltForce == null)
        {
            Debug.LogWarning("BalloonManager: stickTiltForce is not assigned.", this);
        }

        if (scoreCounter == null)
        {
            Debug.LogWarning("BalloonManager: scoreCounter is not assigned.", this);
        }

        if (spawnRoot == null)
        {
            Debug.LogWarning("BalloonManager: spawnRoot is not assigned.", this);
        }

        if (targetSpawnRoot == null)
        {
            Debug.LogWarning("BalloonManager: targetSpawnRoot is not assigned.", this);
        }

        if (moneyText == null)
        {
            Debug.LogWarning("BalloonManager: moneyText was not found.", this);
        }

        if (moneyTextRoot == null)
        {
            Debug.LogWarning("BalloonManager: moneyTextRoot was not found.", this);
        }

        if (patternEventScheduler == null)
        {
            Debug.LogError("BalloonManager: PatternEventScheduler was not found. Balloons cannot spawn.", this);
        }
    }

    private void OnEnable()
    {
        ResolveUiReferences();
        touchedBalloonCount = 0;
        ResolvePatternEventScheduler();
        if (patternEventScheduler != null)
        {
            patternEventScheduler.EventReached -= HandlePatternEventReached;
            patternEventScheduler.EventReached += HandlePatternEventReached;
        }

        if (stickTiltForce == null)
        {
            isRetryRequired = false;
            isInputUnlocked = false;
            UpdateMoneyText();
            UpdateMoneyTextVisibility();
            return;
        }

        stickTiltForce.RetryStateChanged -= HandleRetryStateChanged;
        stickTiltForce.RetryStateChanged += HandleRetryStateChanged;
        stickTiltForce.StartGateStateChanged -= HandleStartGateStateChanged;
        stickTiltForce.StartGateStateChanged += HandleStartGateStateChanged;

        isRetryRequired = stickTiltForce.IsRetryRequired;
        isInputUnlocked = stickTiltForce.IsInputUnlocked;
        UpdateMoneyText();
        UpdateMoneyTextVisibility();

        if (isRetryRequired && clearBalloonsOnRetry)
        {
            ClearSpawnedBalloons();
        }
    }

    private void OnDisable()
    {
        if (patternEventScheduler != null)
        {
            patternEventScheduler.EventReached -= HandlePatternEventReached;
        }

        if (stickTiltForce != null)
        {
            stickTiltForce.RetryStateChanged -= HandleRetryStateChanged;
            stickTiltForce.StartGateStateChanged -= HandleStartGateStateChanged;
        }

        UpdateMoneyTextVisibility();
    }

    private void HandleRetryStateChanged(bool retryRequired)
    {
        isRetryRequired = retryRequired;

        if (!retryRequired)
        {
            ResetTouchedBalloonCount();
            UpdateMoneyTextVisibility();
            return;
        }

        UpdateMoneyTextVisibility();

        if (clearBalloonsOnRetry)
        {
            ClearSpawnedBalloons();
        }
    }

    private void HandleStartGateStateChanged(bool inputUnlocked)
    {
        bool wasInputUnlocked = isInputUnlocked;
        isInputUnlocked = inputUnlocked;
        UpdateMoneyTextVisibility();

        if (!wasInputUnlocked && inputUnlocked)
        {
            ResetTouchedBalloonCount();
        }
    }

    private void HandleBalloonStickTouched(Balloon balloon, BalloonReward reward)
    {
        switch (reward.Kind)
        {
            case BalloonRewardKind.Currency:
                touchedBalloonCount += reward.CurrencyAmount;
                UpdateMoneyText();
                break;
            case BalloonRewardKind.Buff:
                if (buffInventory != null)
                {
                    buffInventory.TryAdd(reward.Effect);
                }
                break;
            case BalloonRewardKind.Debuff:
                if (gameplayEffectController != null)
                {
                    gameplayEffectController.TryApply(reward.Effect);
                }
                break;
        }
    }

    private void HandlePatternEventReached(ScheduledPatternEvent scheduledEvent)
    {
        if (balloonPrefab == null || spawnRoot == null || targetSpawnRoot == null ||
            stickTiltForce == null || isRetryRequired || !isInputUnlocked)
        {
            return;
        }

        if (!TryBuildReward(scheduledEvent.BalloonKind, out BalloonReward reward))
        {
            Debug.LogError($"BalloonManager: no available effect for {scheduledEvent.BalloonKind} " +
                           $"at {scheduledEvent.Height:F2} m. Balloon skipped.", this);
            return;
        }

        float angleRadians = scheduledEvent.AngleDegrees * Mathf.Deg2Rad;
        float radius = scheduledEvent.DistanceFromCenter;
        Vector3 spawnPosition = spawnAreaCenter + new Vector3(
            Mathf.Cos(angleRadians) * radius,
            0f,
            Mathf.Sin(angleRadians) * radius);

        Balloon spawnedBalloon = Instantiate(balloonPrefab, spawnRoot);
        Transform targetPoint = CreateTargetPoint(spawnPosition);

        spawnedBalloon.transform.position = spawnPosition;
        spawnedBalloon.StickTouched += HandleBalloonStickTouched;
        spawnedBalloon.Initialize(
            stickTiltForce,
            targetPoint,
            balloonSpeedCurve,
            balloonScaleCurve,
            balloonSpeedMultiplier,
            balloonLifeTimeSeconds,
            indicatorWarningBeforeExpireSeconds,
            indicatorScaleCurve,
            stickPushForce,
            reward);
    }

    private bool TryBuildReward(PatternBalloonKind kind, out BalloonReward reward)
    {
        if (kind <= PatternBalloonKind.Violet)
        {
            reward = new BalloonReward(BalloonRewardKind.Currency, null, 1, GetRainbowColor(kind));
            return true;
        }

        bool isBuff = kind == PatternBalloonKind.White ||
                      (kind == PatternBalloonKind.Gray && Random.value < 0.5f);
        GameplayEffectPolarity polarity = isBuff
            ? GameplayEffectPolarity.Buff
            : GameplayEffectPolarity.Debuff;
        GameplayEffectDefinition effect = ChooseEffect(
            isBuff ? availableBuffEffects : availableDebuffEffects, polarity);
        if (effect == null)
        {
            reward = default;
            return false;
        }

        Color color = kind == PatternBalloonKind.Gray
            ? Color.gray
            : isBuff ? Color.white : Color.black;
        reward = new BalloonReward(isBuff ? BalloonRewardKind.Buff : BalloonRewardKind.Debuff,
            effect, 0, color);
        return true;
    }

    private static GameplayEffectDefinition ChooseEffect(
        GameplayEffectDefinition[] options, GameplayEffectPolarity requiredPolarity)
    {
        if (options == null)
        {
            return null;
        }

        int validCount = 0;
        for (int i = 0; i < options.Length; i++)
        {
            if (options[i] != null && options[i].Polarity == requiredPolarity)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return null;
        }

        int selected = Random.Range(0, validCount);
        for (int i = 0; i < options.Length; i++)
        {
            GameplayEffectDefinition option = options[i];
            if (option == null || option.Polarity != requiredPolarity)
            {
                continue;
            }

            if (selected-- == 0)
            {
                return option;
            }
        }

        return null;
    }

    private static Color GetRainbowColor(PatternBalloonKind kind)
    {
        switch (kind)
        {
            case PatternBalloonKind.Red: return new Color(1f, 0.12f, 0.12f);
            case PatternBalloonKind.Orange: return new Color(1f, 0.48f, 0.08f);
            case PatternBalloonKind.Yellow: return new Color(1f, 0.9f, 0.08f);
            case PatternBalloonKind.Green: return new Color(0.12f, 0.8f, 0.2f);
            case PatternBalloonKind.Cyan: return new Color(0.08f, 0.8f, 0.9f);
            case PatternBalloonKind.Blue: return new Color(0.12f, 0.28f, 1f);
            case PatternBalloonKind.Violet: return new Color(0.65f, 0.18f, 0.9f);
            default: return Color.white;
        }
    }

    private Transform CreateTargetPoint(Vector3 worldPosition)
    {
        GameObject targetObject = new GameObject("Target");
        Transform targetTransform = targetObject.transform;
        targetTransform.SetParent(targetSpawnRoot, false);
        targetTransform.position = worldPosition;
        targetTransform.rotation = Quaternion.identity;
        targetTransform.localScale = Vector3.one;
        return targetTransform;
    }

    private void ClearSpawnedBalloons()
    {
        if (spawnRoot == null)
        {
            return;
        }

        for (int i = spawnRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = spawnRoot.GetChild(i);
            if (child.GetComponent<Balloon>() == null)
            {
                continue;
            }

            Destroy(child.gameObject);
        }
    }

    private void ResetTouchedBalloonCount()
    {
        touchedBalloonCount = 0;
        UpdateMoneyText();
    }

    private void UpdateMoneyText()
    {
        ResolveUiReferences();

        if (moneyText == null)
        {
            return;
        }

        moneyText.text = touchedBalloonCount.ToString();
    }

    private void UpdateMoneyTextVisibility()
    {
        ResolveUiReferences();

        if (moneyTextRoot == null)
        {
            return;
        }

        bool shouldBeVisible = isInputUnlocked && !isRetryRequired;
        if (moneyTextRoot.activeSelf != shouldBeVisible)
        {
            moneyTextRoot.SetActive(shouldBeVisible);
        }
    }

    private void EnsureGameplayServices()
    {
        if (gameplayEffectController == null)
        {
            gameplayEffectController = GetComponent<GameplayEffectController>();
        }

        if (gameplayEffectController == null)
        {
            gameplayEffectController = gameObject.AddComponent<GameplayEffectController>();
        }

        if (buffInventory == null)
        {
            buffInventory = GetComponent<BuffInventory>();
        }

        if (buffInventory == null)
        {
            buffInventory = gameObject.AddComponent<BuffInventory>();
        }

        gameplayEffectController.Configure(stickTiltForce);
        buffInventory.Configure(gameplayEffectController, stickTiltForce);

        if (stickTiltForce != null)
        {
            stickTiltForce.SetGameplayEffectController(gameplayEffectController);
        }

        if (scoreCounter != null)
        {
            scoreCounter.SetGameplayEffectController(gameplayEffectController);
        }
    }

    private void ResolvePatternEventScheduler()
    {
        if (patternEventScheduler == null)
        {
            patternEventScheduler = FindObjectOfType<PatternEventScheduler>();
        }
    }

    private void ResolveUiReferences()
    {
        if (moneyText == null)
        {
            moneyText = FindComponentByName<TMP_Text>(MoneyTextName);
            if (moneyText == null)
            {
                moneyText = FindComponentByName<TMP_Text>(ScoreTextName);
            }
        }

        if (moneyTextRoot == null)
        {
            Transform rootTransform = FindTransformByName(MoneyTextRootName);
            if (rootTransform == null)
            {
                rootTransform = FindTransformByName(ScoreTextRootName);
            }

            if (rootTransform != null)
            {
                moneyTextRoot = rootTransform.gameObject;
            }
        }
    }

    private static T FindComponentByName<T>(string objectName) where T : Component
    {
        Transform targetTransform = FindTransformByName(objectName);
        return targetTransform != null ? targetTransform.GetComponent<T>() : null;
    }

    private static Transform FindTransformByName(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = activeScene.GetRootGameObjects();
        for (int i = 0; i < rootObjects.Length; i++)
        {
            Transform result = FindTransformRecursive(rootObjects[i].transform, objectName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static Transform FindTransformRecursive(Transform current, string objectName)
    {
        if (current.name == objectName)
        {
            return current;
        }

        for (int i = 0; i < current.childCount; i++)
        {
            Transform child = current.GetChild(i);
            Transform result = FindTransformRecursive(child, objectName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}

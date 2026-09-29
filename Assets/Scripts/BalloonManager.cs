using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class BalloonManager : MonoBehaviour
{
    private const string MoneyTextName = "MoneyText";
    private const string MoneyTextRootName = "MoneyTextRoot";

    [Header("References")]
    [SerializeField] private Balloon balloonPrefab;
    [SerializeField] private BalloonColorPalette colorPalette;
    [SerializeField] private Transform spawnRoot, targetSpawnRoot;
    [SerializeField] private StickTiltForce stickTiltForce;
    [FormerlySerializedAs("scoreCouter")]
    [SerializeField] private ScoreCounter scoreCounter;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private GameObject moneyTextRoot;
    [SerializeField] private GameplayEffectController gameplayEffectController;
    [SerializeField] private BuffInventory buffInventory;
    [SerializeField] private PatternEventScheduler patternEventScheduler;
    [SerializeField] private PlayerProgressSaveManager progressSaveManager;

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

    private bool isRetryRequired;
    private bool isInputUnlocked;
    private int currencyBalance;

    private void Awake()
    {
        EnsureGameplayServices();
        ResolveProgressSaveManager();
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

        if (colorPalette == null)
        {
            Debug.LogError("BalloonManager: colorPalette is not assigned. Balloons cannot spawn.", this);
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

        if (progressSaveManager == null)
        {
            Debug.LogError("BalloonManager: PlayerProgressSaveManager was not found. Currency cannot be saved.", this);
        }
    }

    private void OnEnable()
    {
        ResolveUiReferences();
        ResolveProgressSaveManager();
        currencyBalance = progressSaveManager != null ? progressSaveManager.LoadCurrencyBalance() : 0;
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

        if (isRetryRequired)
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
            UpdateMoneyTextVisibility();
            return;
        }

        UpdateMoneyTextVisibility();

        ClearSpawnedBalloons();
    }

    private void HandleStartGateStateChanged(bool inputUnlocked)
    {
        isInputUnlocked = inputUnlocked;
        UpdateMoneyTextVisibility();
    }

    private void HandleBalloonStickTouched(Balloon balloon, BalloonReward reward)
    {
        switch (reward.Kind)
        {
            case BalloonRewardKind.Currency:
                ResolveProgressSaveManager();
                if (progressSaveManager == null)
                {
                    Debug.LogError("BalloonManager: currency reward cannot be saved.", this);
                    break;
                }

                currencyBalance = progressSaveManager.AddCurrency(reward.CurrencyAmount);
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
        if (balloonPrefab == null || colorPalette == null || spawnRoot == null || targetSpawnRoot == null ||
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
            reward = new BalloonReward(BalloonRewardKind.Currency, null, 1, colorPalette.GetColor(kind));
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

        Color color = colorPalette.GetColor(kind);
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

    private void UpdateMoneyText()
    {
        ResolveUiReferences();

        if (moneyText == null)
        {
            return;
        }

        moneyText.text = currencyBalance.ToString();
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

    private void ResolveProgressSaveManager()
    {
        if (progressSaveManager == null)
        {
            progressSaveManager = FindObjectOfType<PlayerProgressSaveManager>();
        }
    }

    private void ResolveUiReferences()
    {
        if (moneyText == null)
        {
            moneyText = FindComponentByName<TMP_Text>(MoneyTextName);
        }

        if (moneyTextRoot == null)
        {
            Transform rootTransform = FindTransformByName(MoneyTextRootName);
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

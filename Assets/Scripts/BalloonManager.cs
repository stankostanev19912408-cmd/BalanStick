using System.Collections.Generic;
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
    [SerializeField] private ProgressionManager progressionManager;

    [Header("Balloon Settings")]
    [SerializeField] private AnimationCurve balloonSpeedCurve = AnimationCurve.Linear(0f, 0.6f, 1f, 0.6f);
    [SerializeField] private AnimationCurve balloonScaleCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    [SerializeField, Min(0f)] private float balloonSpeedMultiplier = 1f;
    [SerializeField, Min(0f)] private float balloonLifeTimeSeconds = 5f;
    [SerializeField, Min(0f)] private float indicatorWarningBeforeExpireSeconds = 1.5f;
    [SerializeField] private AnimationCurve indicatorScaleCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    [SerializeField, Min(0f)] private float stickPushForce = 2f;

    [Header("Rewards")]
    [SerializeField, Min(0)] private int currencyPerBalloon = 1;
    [SerializeField] private Color currencyBalloonColor = new Color(0.1f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color buffBalloonColor = new Color(0.1f, 0.35f, 1f, 1f);
    [SerializeField] private Color debuffBalloonColor = new Color(0.03f, 0.03f, 0.03f, 1f);
    [SerializeField, Range(0f, 1f)] private float totalBuffChance = 0.3f;
    [SerializeField, Range(0f, 1f)] private float totalDebuffChance = 0.1f;

    [Header("Spawn Zone (Radial)")]
    [SerializeField] private Vector3 spawnAreaCenter = new Vector3(-1.5f, 0f, 0f);
    [SerializeField, Min(0f)] private float minSpawnDistance = 0.5f;
    [SerializeField, Min(0f)] private float maxSpawnDistance = 2f;
    [SerializeField, Range(0f, 180f)] private float oppositeAngleHalfRangeDegrees = 60f;

    [Header("Spawn Timing")]
    [SerializeField, Min(0f)] private float minSpawnIntervalSeconds = 1f;
    [SerializeField, Min(0f)] private float maxSpawnIntervalSeconds = 3f;
    [SerializeField] private bool clearBalloonsOnRetry = true;

    [Header("Score Range")]
    [SerializeField, Min(0f)] private float minSpawnScore;
    [SerializeField, Min(0f)] private float maxSpawnScore = 999999f;

    private bool isRetryRequired;
    private bool isInputUnlocked;
    private float spawnTimer;
    private int touchedBalloonCount;
    private bool hasPreviousSpawnAngle;
    private float previousSpawnAngleDegrees;
    private readonly List<WeightedGameplayEffect> unlockedBuffEffects = new List<WeightedGameplayEffect>();
    private readonly List<WeightedGameplayEffect> unlockedDebuffEffects = new List<WeightedGameplayEffect>();
    private int cachedProgressionLevel = -1;

    private void Awake()
    {
        EnsureGameplayServices();
    }

    private void OnValidate()
    {
        minSpawnDistance = Mathf.Max(0f, minSpawnDistance);
        maxSpawnDistance = Mathf.Max(minSpawnDistance, maxSpawnDistance);
        oppositeAngleHalfRangeDegrees = Mathf.Clamp(oppositeAngleHalfRangeDegrees, 0f, 180f);
        balloonSpeedMultiplier = Mathf.Max(0f, balloonSpeedMultiplier);
        balloonLifeTimeSeconds = Mathf.Max(0f, balloonLifeTimeSeconds);
        indicatorWarningBeforeExpireSeconds = Mathf.Max(0f, indicatorWarningBeforeExpireSeconds);
        stickPushForce = Mathf.Max(0f, stickPushForce);
        currencyPerBalloon = Mathf.Max(0, currencyPerBalloon);
        totalBuffChance = Mathf.Clamp01(totalBuffChance);
        totalDebuffChance = Mathf.Clamp(totalDebuffChance, 0f, 1f - totalBuffChance);
        minSpawnIntervalSeconds = Mathf.Max(0f, minSpawnIntervalSeconds);
        maxSpawnIntervalSeconds = Mathf.Max(minSpawnIntervalSeconds, maxSpawnIntervalSeconds);
        minSpawnScore = Mathf.Max(0f, minSpawnScore);
        maxSpawnScore = Mathf.Max(minSpawnScore, maxSpawnScore);
    }

    private void Start()
    {
        EnsureGameplayServices();
        ResolveProgressionManager();
        RefreshUnlockedEffectPools();
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

        if (progressionManager == null)
        {
            Debug.LogWarning("BalloonManager: ProgressionManager was not found. Special balloons are disabled.", this);
        }
    }

    private void OnEnable()
    {
        ResolveUiReferences();
        touchedBalloonCount = 0;
        ResetSpawnDirectionSequence();

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
        spawnTimer = GetRandomSpawnInterval();
        UpdateMoneyText();
        UpdateMoneyTextVisibility();

        if (isRetryRequired && clearBalloonsOnRetry)
        {
            ClearSpawnedBalloons();
        }
    }

    private void OnDisable()
    {
        if (stickTiltForce != null)
        {
            stickTiltForce.RetryStateChanged -= HandleRetryStateChanged;
            stickTiltForce.StartGateStateChanged -= HandleStartGateStateChanged;
        }

        UpdateMoneyTextVisibility();
    }

    private void Update()
    {
        if (balloonPrefab == null || spawnRoot == null || targetSpawnRoot == null || stickTiltForce == null || scoreCounter == null || isRetryRequired || !isInputUnlocked)
        {
            return;
        }

        if (!IsScoreInSpawnRange())
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
        {
            return;
        }

        SpawnBalloon();
        spawnTimer = GetRandomSpawnInterval();
    }

    private void OnDrawGizmosSelected()
    {
        float clampedMinDistance = Mathf.Max(0f, minSpawnDistance);
        float clampedMaxDistance = Mathf.Max(clampedMinDistance, maxSpawnDistance);
        DrawHorizontalCircle(spawnAreaCenter, clampedMaxDistance, Color.cyan);
        DrawHorizontalCircle(spawnAreaCenter, clampedMinDistance, Color.red);
    }

    private void HandleRetryStateChanged(bool retryRequired)
    {
        isRetryRequired = retryRequired;

        if (!retryRequired)
        {
            ResetTouchedBalloonCount();
            ResetSpawnDirectionSequence();
            spawnTimer = GetRandomSpawnInterval();
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
            ResetSpawnDirectionSequence();
            spawnTimer = GetRandomSpawnInterval();
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

    private void SpawnBalloon()
    {
        Vector3 spawnPosition = GetNextSpawnPosition();
        BalloonReward reward = RollReward();

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

    private BalloonReward RollReward()
    {
        RefreshUnlockedEffectPools();

        float roll = Random.value;
        float clampedBuffChance = Mathf.Clamp01(totalBuffChance);
        float clampedDebuffChance = Mathf.Clamp(totalDebuffChance, 0f, 1f - clampedBuffChance);

        if (roll < clampedBuffChance)
        {
            GameplayEffectDefinition buff = ChooseWeightedEffect(unlockedBuffEffects, GameplayEffectPolarity.Buff);
            if (buff != null)
            {
                return new BalloonReward(BalloonRewardKind.Buff, buff, 0, buffBalloonColor);
            }
        }
        else if (roll < clampedBuffChance + clampedDebuffChance)
        {
            GameplayEffectDefinition debuff = ChooseWeightedEffect(unlockedDebuffEffects, GameplayEffectPolarity.Debuff);
            if (debuff != null)
            {
                return new BalloonReward(BalloonRewardKind.Debuff, debuff, 0, debuffBalloonColor);
            }
        }

        return new BalloonReward(BalloonRewardKind.Currency, null, currencyPerBalloon, currencyBalloonColor);
    }

    private static GameplayEffectDefinition ChooseWeightedEffect(
        IReadOnlyList<WeightedGameplayEffect> options,
        GameplayEffectPolarity requiredPolarity)
    {
        if (options == null || options.Count == 0)
        {
            return null;
        }

        float totalWeight = 0f;
        for (int i = 0; i < options.Count; i++)
        {
            WeightedGameplayEffect option = options[i];
            if (option != null && option.Effect != null && option.Effect.Polarity == requiredPolarity)
            {
                totalWeight += option.Weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = Random.value * totalWeight;
        GameplayEffectDefinition lastValidEffect = null;
        for (int i = 0; i < options.Count; i++)
        {
            WeightedGameplayEffect option = options[i];
            if (option == null || option.Effect == null || option.Effect.Polarity != requiredPolarity || option.Weight <= 0f)
            {
                continue;
            }

            lastValidEffect = option.Effect;
            roll -= option.Weight;
            if (roll <= 0f)
            {
                return option.Effect;
            }
        }

        return lastValidEffect;
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

    private Vector3 GetNextSpawnPosition()
    {
        float nextAngleDegrees = GetNextSpawnAngleDegrees();
        float clampedMinDistance = Mathf.Max(0f, minSpawnDistance);
        float clampedMaxDistance = Mathf.Max(clampedMinDistance, maxSpawnDistance);
        float distance = Random.Range(clampedMinDistance, clampedMaxDistance);
        float angleRadians = nextAngleDegrees * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(angleRadians) * distance,
            0f,
            Mathf.Sin(angleRadians) * distance
        );

        previousSpawnAngleDegrees = nextAngleDegrees;
        hasPreviousSpawnAngle = true;
        return spawnAreaCenter + offset;
    }

    private float GetNextSpawnAngleDegrees()
    {
        if (!hasPreviousSpawnAngle)
        {
            return Random.Range(0f, 360f);
        }

        float oppositeAngle = Mathf.Repeat(previousSpawnAngleDegrees + 180f, 360f);
        float minAngle = oppositeAngle - oppositeAngleHalfRangeDegrees;
        float maxAngle = oppositeAngle + oppositeAngleHalfRangeDegrees;
        return Mathf.Repeat(Random.Range(minAngle, maxAngle), 360f);
    }

    private float GetRandomSpawnInterval()
    {
        float clampedMaxSpawnInterval = Mathf.Max(minSpawnIntervalSeconds, maxSpawnIntervalSeconds);
        return Random.Range(minSpawnIntervalSeconds, clampedMaxSpawnInterval);
    }

    private bool IsScoreInSpawnRange()
    {
        float currentScore = scoreCounter.CurrentScoreValue;
        return currentScore >= minSpawnScore && currentScore <= maxSpawnScore;
    }

    private void ResetSpawnDirectionSequence()
    {
        hasPreviousSpawnAngle = false;
        previousSpawnAngleDegrees = 0f;
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

    private void ResolveProgressionManager()
    {
        if (progressionManager == null)
        {
            progressionManager = FindObjectOfType<ProgressionManager>();
        }
    }

    private void RefreshUnlockedEffectPools()
    {
        ResolveProgressionManager();

        int progressionLevel = progressionManager != null ? progressionManager.CurrentLevel : 0;
        if (progressionLevel == cachedProgressionLevel)
        {
            return;
        }

        cachedProgressionLevel = progressionLevel;
        unlockedBuffEffects.Clear();
        unlockedDebuffEffects.Clear();

        if (progressionManager == null)
        {
            return;
        }

        progressionManager.GetUnlockedEffects(GameplayEffectPolarity.Buff, unlockedBuffEffects);
        progressionManager.GetUnlockedEffects(GameplayEffectPolarity.Debuff, unlockedDebuffEffects);
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

    private static void DrawHorizontalCircle(Vector3 center, float radius, Color color)
    {
        if (radius <= 0f)
        {
            return;
        }

        const int segments = 48;
        Gizmos.color = color;
        Vector3 previousPoint = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * (Mathf.PI * 2f / segments);
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }
}

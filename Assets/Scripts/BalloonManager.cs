using UnityEngine;

public class BalloonManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Balloon balloonPrefab;
    [SerializeField] private Transform spawnRoot, targetSpawnRoot;
    [SerializeField] private StickTiltForce stickTiltForce;
    [SerializeField] private ScoreCouter scoreCouter;

    [Header("Balloon Settings")]
    [SerializeField] private AnimationCurve balloonSpeedCurve = AnimationCurve.Linear(0f, 0.6f, 1f, 0.6f);
    [SerializeField] private AnimationCurve balloonScaleCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    [SerializeField, Min(0f)] private float balloonSpeedMultiplier = 1f;
    [SerializeField, Min(0f)] private float balloonLifeTimeSeconds = 5f;

    [Header("Spawn Zone")]
    [SerializeField] private Vector3 spawnAreaCenter = new Vector3(-1.5f, 0f, 0f);
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(3f, 0f, 3f);
    [SerializeField] private Vector3 notSpawnAreaSize = Vector3.zero;

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
    private bool missingSpawnSpaceLogged;

    private void OnValidate()
    {
        spawnAreaSize = new Vector3(
            Mathf.Max(0f, spawnAreaSize.x),
            Mathf.Max(0f, spawnAreaSize.y),
            Mathf.Max(0f, spawnAreaSize.z)
        );
        notSpawnAreaSize = new Vector3(
            Mathf.Max(0f, notSpawnAreaSize.x),
            Mathf.Max(0f, notSpawnAreaSize.y),
            Mathf.Max(0f, notSpawnAreaSize.z)
        );
        balloonSpeedMultiplier = Mathf.Max(0f, balloonSpeedMultiplier);
        balloonLifeTimeSeconds = Mathf.Max(0f, balloonLifeTimeSeconds);
        minSpawnIntervalSeconds = Mathf.Max(0f, minSpawnIntervalSeconds);
        maxSpawnIntervalSeconds = Mathf.Max(minSpawnIntervalSeconds, maxSpawnIntervalSeconds);
        minSpawnScore = Mathf.Max(0f, minSpawnScore);
        maxSpawnScore = Mathf.Max(minSpawnScore, maxSpawnScore);
    }

    private void Start()
    {
        if (balloonPrefab == null)
        {
            Debug.LogWarning("BalloonManager: balloonPrefab is not assigned.", this);
        }

        if (stickTiltForce == null)
        {
            Debug.LogWarning("BalloonManager: stickTiltForce is not assigned.", this);
        }

        if (scoreCouter == null)
        {
            Debug.LogWarning("BalloonManager: scoreCouter is not assigned.", this);
        }

        if (spawnRoot == null)
        {
            Debug.LogWarning("BalloonManager: spawnRoot is not assigned.", this);
        }

        if (targetSpawnRoot == null)
        {
            Debug.LogWarning("BalloonManager: targetSpawnRoot is not assigned.", this);
        }
    }

    private void OnEnable()
    {
        if (stickTiltForce == null)
        {
            isRetryRequired = false;
            isInputUnlocked = false;
            return;
        }

        stickTiltForce.RetryStateChanged -= HandleRetryStateChanged;
        stickTiltForce.RetryStateChanged += HandleRetryStateChanged;
        stickTiltForce.StartGateStateChanged -= HandleStartGateStateChanged;
        stickTiltForce.StartGateStateChanged += HandleStartGateStateChanged;

        isRetryRequired = stickTiltForce.IsRetryRequired;
        isInputUnlocked = stickTiltForce.IsInputUnlocked;
        spawnTimer = GetRandomSpawnInterval();

        if (isRetryRequired && clearBalloonsOnRetry)
        {
            ClearSpawnedBalloons();
        }
    }

    private void OnDisable()
    {
        if (stickTiltForce == null)
        {
            return;
        }

        stickTiltForce.RetryStateChanged -= HandleRetryStateChanged;
        stickTiltForce.StartGateStateChanged -= HandleStartGateStateChanged;
    }

    private void Update()
    {
        if (balloonPrefab == null || spawnRoot == null || targetSpawnRoot == null || stickTiltForce == null || scoreCouter == null || isRetryRequired || !isInputUnlocked)
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
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(spawnAreaCenter, notSpawnAreaSize);
    }

    private void HandleRetryStateChanged(bool retryRequired)
    {
        isRetryRequired = retryRequired;

        if (!retryRequired)
        {
            spawnTimer = GetRandomSpawnInterval();
            return;
        }

        if (clearBalloonsOnRetry)
        {
            ClearSpawnedBalloons();
        }
    }

    private void HandleStartGateStateChanged(bool inputUnlocked)
    {
        bool wasInputUnlocked = isInputUnlocked;
        isInputUnlocked = inputUnlocked;

        if (!wasInputUnlocked && inputUnlocked)
        {
            spawnTimer = GetRandomSpawnInterval();
        }
    }

    private void SpawnBalloon()
    {
        if (!TryGetRandomWorldSpawnPosition(out Vector3 spawnPosition))
        {
            if (!missingSpawnSpaceLogged)
            {
                Debug.LogWarning("BalloonManager: spawnArea does not contain any space outside notSpawnAreaSize.", this);
                missingSpawnSpaceLogged = true;
            }

            return;
        }

        missingSpawnSpaceLogged = false;

        Balloon spawnedBalloon = Instantiate(balloonPrefab, spawnRoot);
        Transform targetPoint = CreateTargetPoint(spawnPosition);

        spawnedBalloon.transform.position = spawnPosition;
        spawnedBalloon.Initialize(
            stickTiltForce,
            targetPoint,
            balloonSpeedCurve,
            balloonScaleCurve,
            balloonSpeedMultiplier,
            balloonLifeTimeSeconds);
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

    private bool TryGetRandomWorldSpawnPosition(out Vector3 spawnPosition)
    {
        Vector3 spawnHalfSize = spawnAreaSize * 0.5f;
        Vector3 notSpawnHalfSize = new Vector3(
            Mathf.Min(notSpawnAreaSize.x * 0.5f, spawnHalfSize.x),
            Mathf.Min(notSpawnAreaSize.y * 0.5f, spawnHalfSize.y),
            Mathf.Min(notSpawnAreaSize.z * 0.5f, spawnHalfSize.z)
        );

        for (int attempt = 0; attempt < 32; attempt++)
        {
            Vector3 candidate = spawnAreaCenter + new Vector3(
                Random.Range(-spawnHalfSize.x, spawnHalfSize.x),
                Random.Range(-spawnHalfSize.y, spawnHalfSize.y),
                Random.Range(-spawnHalfSize.z, spawnHalfSize.z)
            );

            if (!IsInsideNotSpawnArea(candidate, notSpawnHalfSize))
            {
                spawnPosition = candidate;
                return true;
            }
        }

        float xMargin = Mathf.Max(0f, spawnHalfSize.x - notSpawnHalfSize.x);
        float yMargin = Mathf.Max(0f, spawnHalfSize.y - notSpawnHalfSize.y);
        float zMargin = Mathf.Max(0f, spawnHalfSize.z - notSpawnHalfSize.z);

        if (xMargin <= 0f && yMargin <= 0f && zMargin <= 0f)
        {
            spawnPosition = spawnAreaCenter;
            return false;
        }

        spawnPosition = spawnAreaCenter + new Vector3(
            Random.Range(-spawnHalfSize.x, spawnHalfSize.x),
            Random.Range(-spawnHalfSize.y, spawnHalfSize.y),
            Random.Range(-spawnHalfSize.z, spawnHalfSize.z)
        );

        if (xMargin >= yMargin && xMargin >= zMargin && xMargin > 0f)
        {
            spawnPosition.x = spawnAreaCenter.x + RandomSign() * Random.Range(notSpawnHalfSize.x, spawnHalfSize.x);
        }
        else if (yMargin >= zMargin && yMargin > 0f)
        {
            spawnPosition.y = spawnAreaCenter.y + RandomSign() * Random.Range(notSpawnHalfSize.y, spawnHalfSize.y);
        }
        else
        {
            spawnPosition.z = spawnAreaCenter.z + RandomSign() * Random.Range(notSpawnHalfSize.z, spawnHalfSize.z);
        }

        return true;
    }

    private bool IsInsideNotSpawnArea(Vector3 candidate, Vector3 notSpawnHalfSize)
    {
        Vector3 offset = candidate - spawnAreaCenter;
        return Mathf.Abs(offset.x) <= notSpawnHalfSize.x
            && Mathf.Abs(offset.y) <= notSpawnHalfSize.y
            && Mathf.Abs(offset.z) <= notSpawnHalfSize.z;
    }

    private static float RandomSign()
    {
        return Random.value < 0.5f ? -1f : 1f;
    }

    private float GetRandomSpawnInterval()
    {
        float clampedMaxSpawnInterval = Mathf.Max(minSpawnIntervalSeconds, maxSpawnIntervalSeconds);
        return Random.Range(minSpawnIntervalSeconds, clampedMaxSpawnInterval);
    }

    private bool IsScoreInSpawnRange()
    {
        float currentScore = scoreCouter.CurrentScoreValue;
        return currentScore >= minSpawnScore && currentScore <= maxSpawnScore;
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
}

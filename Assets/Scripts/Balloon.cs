using System;
using UnityEngine;

public class Balloon : MonoBehaviour
{
    public event Action<Balloon> StickTouched;

    [SerializeField] private Transform indicatorTransform;

    private StickTiltForce stickTiltForce;
    private Transform targetPoint;
    private AnimationCurve speedCurve;
    private AnimationCurve scaleCurve;
    private AnimationCurve indicatorScaleCurve;
    private float speedMultiplier;
    private float lifeTimeSeconds;
    private float indicatorWarningBeforeExpireSeconds;
    private float stickPushForce;
    private Vector3 baseLocalScale;
    private Vector3 indicatorBaseLocalScale;

    private bool isRetryRequired;
    private bool isInputUnlocked = true;
    private bool wasTouchedByStick;
    private bool isIndicatorVisible;
    private bool hasIndicatorBaseLocalScale;
    private float elapsedLifeTime;
    private float remainingLifeTime;
    private float indicatorVisibleFromRemainingLifeTime;

    private void Awake()
    {
        baseLocalScale = transform.localScale;
        remainingLifeTime = lifeTimeSeconds;
        CacheIndicatorBaseScale();
        ResetIndicatorState();
    }

    private void Start()
    {
        if (stickTiltForce == null)
        {
            Debug.LogWarning("Balloon: stickTiltForce is not assigned.", this);
        }

        if (indicatorTransform == null)
        {
            Debug.LogWarning("Balloon: indicatorTransform is not assigned.", this);
        }
    }

    private void OnEnable()
    {
        elapsedLifeTime = 0f;
        remainingLifeTime = lifeTimeSeconds;
        wasTouchedByStick = false;
        CacheIndicatorBaseScale();
        ResetIndicatorState();
        BindTiltForceEvents();
    }

    private void OnDisable()
    {
        UnbindTiltForceEvents();
    }

    private void OnDestroy()
    {
        if (targetPoint != null)
        {
            Destroy(targetPoint.gameObject);
        }
    }

    private void LateUpdate()
    {
        if (stickTiltForce == null || isRetryRequired || !isInputUnlocked || transform.parent == null)
        {
            return;
        }

        if (lifeTimeSeconds > 0f)
        {
            if (remainingLifeTime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            remainingLifeTime = Mathf.Max(0f, remainingLifeTime - Time.deltaTime);

            if (remainingLifeTime <= 0f)
            {
                Destroy(gameObject);
                return;
            }
        }

        UpdateIndicatorState();

        float currentSpeed = EvaluateSpeed();
        Vector3 position = transform.position;
        if (targetPoint != null)
        {
            Vector3 targetPosition = targetPoint.position;
            position.x = targetPosition.x;
            position.z = targetPosition.z;
        }

        position += currentSpeed * Time.deltaTime * Vector3.up;
        transform.position = position;
        transform.localScale = baseLocalScale * EvaluateScaleMultiplier();
        elapsedLifeTime += lifeTimeSeconds > 0f ? Time.deltaTime / lifeTimeSeconds : Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (wasTouchedByStick || stickTiltForce == null || isRetryRequired || !isInputUnlocked)
        {
            return;
        }

        Transform stickTransform = stickTiltForce.transform;
        Transform otherTransform = other.transform;
        bool isStickCollision = otherTransform == stickTransform
            || otherTransform.IsChildOf(stickTransform)
            || stickTransform.IsChildOf(otherTransform);

        if (!isStickCollision)
        {
            return;
        }

        wasTouchedByStick = true;
        ApplyPushToStick();
        StickTouched?.Invoke(this);
        Destroy(gameObject);
    }

    public void Initialize(
        StickTiltForce sourceStickTiltForce,
        Transform sourceTargetPoint,
        AnimationCurve sourceSpeedCurve,
        AnimationCurve sourceScaleCurve,
        float sourceSpeedMultiplier,
        float sourceLifeTimeSeconds,
        float sourceIndicatorWarningBeforeExpireSeconds,
        AnimationCurve sourceIndicatorScaleCurve,
        float sourceStickPushForce)
    {
        UnbindTiltForceEvents();
        stickTiltForce = sourceStickTiltForce;
        targetPoint = sourceTargetPoint;
        speedCurve = CloneCurve(sourceSpeedCurve);
        scaleCurve = CloneCurve(sourceScaleCurve);
        indicatorScaleCurve = CloneCurve(sourceIndicatorScaleCurve);
        speedMultiplier = Mathf.Max(0f, sourceSpeedMultiplier);
        lifeTimeSeconds = Mathf.Max(0f, sourceLifeTimeSeconds);
        indicatorWarningBeforeExpireSeconds = Mathf.Max(0f, sourceIndicatorWarningBeforeExpireSeconds);
        stickPushForce = Mathf.Max(0f, sourceStickPushForce);
        elapsedLifeTime = 0f;
        remainingLifeTime = lifeTimeSeconds;
        CacheIndicatorBaseScale();
        ResetIndicatorState();
        BindTiltForceEvents();
    }

    private void ApplyPushToStick()
    {
        if (stickTiltForce == null || stickPushForce <= 0f)
        {
            return;
        }

        Vector3 directionToWorldCenter = new Vector3(-transform.position.x, 0f, -transform.position.z);
        if (directionToWorldCenter.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        stickTiltForce.ApplyExternalPush(directionToWorldCenter.normalized, stickPushForce);
    }

    private void HandleRetryStateChanged(bool retryRequired)
    {
        isRetryRequired = retryRequired;
    }

    private void HandleStartGateStateChanged(bool inputUnlocked)
    {
        isInputUnlocked = inputUnlocked;
    }

    private void BindTiltForceEvents()
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
    }

    private void UnbindTiltForceEvents()
    {
        if (stickTiltForce == null)
        {
            return;
        }

        stickTiltForce.RetryStateChanged -= HandleRetryStateChanged;
        stickTiltForce.StartGateStateChanged -= HandleStartGateStateChanged;
    }

    private float EvaluateSpeed()
    {
        if (speedCurve == null || speedCurve.length == 0)
        {
            return 0f;
        }

        return Mathf.Max(0f, speedCurve.Evaluate(elapsedLifeTime)) * speedMultiplier;
    }

    private float EvaluateScaleMultiplier()
    {
        if (scaleCurve == null || scaleCurve.length == 0)
        {
            return 1f;
        }

        return Mathf.Max(0f, scaleCurve.Evaluate(elapsedLifeTime));
    }

    private float EvaluateIndicatorScaleMultiplier(float normalizedLifetime)
    {
        if (indicatorScaleCurve == null || indicatorScaleCurve.length == 0)
        {
            return 1f;
        }

        return Mathf.Max(0f, indicatorScaleCurve.Evaluate(normalizedLifetime));
    }

    private void UpdateIndicatorState()
    {
        if (indicatorTransform == null || lifeTimeSeconds <= 0f || indicatorWarningBeforeExpireSeconds <= 0f)
        {
            ResetIndicatorState();
            return;
        }

        if (remainingLifeTime > indicatorWarningBeforeExpireSeconds)
        {
            ResetIndicatorState();
            return;
        }

        if (!isIndicatorVisible)
        {
            isIndicatorVisible = true;
            indicatorVisibleFromRemainingLifeTime = Mathf.Max(remainingLifeTime, Mathf.Epsilon);
            indicatorTransform.gameObject.SetActive(true);
        }

        float warningDuration = Mathf.Max(indicatorVisibleFromRemainingLifeTime, Mathf.Epsilon);
        float elapsedWarningTime = Mathf.Clamp(indicatorVisibleFromRemainingLifeTime - remainingLifeTime, 0f, warningDuration);
        float normalizedWarningTime = Mathf.Clamp01(elapsedWarningTime / warningDuration);
        float indicatorScaleMultiplier = EvaluateIndicatorScaleMultiplier(normalizedWarningTime);
        indicatorTransform.localScale = indicatorBaseLocalScale * indicatorScaleMultiplier;
    }

    private void ResetIndicatorState()
    {
        isIndicatorVisible = false;
        indicatorVisibleFromRemainingLifeTime = 0f;

        if (indicatorTransform == null)
        {
            return;
        }

        if (hasIndicatorBaseLocalScale)
        {
            indicatorTransform.localScale = indicatorBaseLocalScale;
        }

        if (indicatorTransform.gameObject.activeSelf)
        {
            indicatorTransform.gameObject.SetActive(false);
        }
    }

    private void CacheIndicatorBaseScale()
    {
        if (indicatorTransform != null && !hasIndicatorBaseLocalScale)
        {
            indicatorBaseLocalScale = indicatorTransform.localScale;
            hasIndicatorBaseLocalScale = true;
        }
    }

    private static AnimationCurve CloneCurve(AnimationCurve sourceCurve)
    {
        if (sourceCurve == null || sourceCurve.length == 0)
        {
            return new AnimationCurve();
        }

        AnimationCurve clonedCurve = new AnimationCurve(sourceCurve.keys)
        {
            preWrapMode = sourceCurve.preWrapMode,
            postWrapMode = sourceCurve.postWrapMode
        };

        return clonedCurve;
    }
}

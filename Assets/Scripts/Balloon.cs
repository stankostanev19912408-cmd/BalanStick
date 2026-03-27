using System;
using UnityEngine;

public class Balloon : MonoBehaviour
{
    public event Action<Balloon> StickTouched;

    private StickTiltForce stickTiltForce;
    private Transform targetPoint;
    private AnimationCurve speedCurve;
    private AnimationCurve scaleCurve;
    private float speedMultiplier;
    private float lifeTimeSeconds;
    private float stickPushForce;
    private Vector3 baseLocalScale;

    private bool isRetryRequired;
    private bool isInputUnlocked = true;
    private bool wasTouchedByStick;
    private float elapsedLifeTime;
    private float remainingLifeTime;

    private void Awake()
    {
        baseLocalScale = transform.localScale;
        remainingLifeTime = lifeTimeSeconds;
    }

    private void Start()
    {
        if (stickTiltForce == null)
        {
            Debug.LogWarning("Balloon: stickTiltForce is not assigned.", this);
        }
    }

    private void OnEnable()
    {
        elapsedLifeTime = 0f;
        remainingLifeTime = lifeTimeSeconds;
        wasTouchedByStick = false;
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
        float sourceStickPushForce)
    {
        UnbindTiltForceEvents();
        stickTiltForce = sourceStickTiltForce;
        targetPoint = sourceTargetPoint;
        speedCurve = CloneCurve(sourceSpeedCurve);
        scaleCurve = CloneCurve(sourceScaleCurve);
        speedMultiplier = Mathf.Max(0f, sourceSpeedMultiplier);
        lifeTimeSeconds = Mathf.Max(0f, sourceLifeTimeSeconds);
        stickPushForce = Mathf.Max(0f, sourceStickPushForce);
        elapsedLifeTime = 0f;
        remainingLifeTime = lifeTimeSeconds;
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

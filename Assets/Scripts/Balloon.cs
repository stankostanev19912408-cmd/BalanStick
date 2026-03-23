using UnityEngine;

public class Balloon : MonoBehaviour
{
    private StickTiltForce stickTiltForce;
    private Transform targetPoint;
    private AnimationCurve speedCurve;
    private AnimationCurve scaleCurve;
    private float speedMultiplier;
    private float lifeTimeSeconds;
    private Vector3 baseLocalScale;

    private bool isRetryRequired;
    private bool isInputUnlocked = true;
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

    public void Initialize(
        StickTiltForce sourceStickTiltForce,
        Transform sourceTargetPoint,
        AnimationCurve sourceSpeedCurve,
        AnimationCurve sourceScaleCurve,
        float sourceSpeedMultiplier,
        float sourceLifeTimeSeconds)
    {
        UnbindTiltForceEvents();
        stickTiltForce = sourceStickTiltForce;
        targetPoint = sourceTargetPoint;
        speedCurve = CloneCurve(sourceSpeedCurve);
        scaleCurve = CloneCurve(sourceScaleCurve);
        speedMultiplier = Mathf.Max(0f, sourceSpeedMultiplier);
        lifeTimeSeconds = Mathf.Max(0f, sourceLifeTimeSeconds);
        elapsedLifeTime = 0f;
        remainingLifeTime = lifeTimeSeconds;
        BindTiltForceEvents();
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

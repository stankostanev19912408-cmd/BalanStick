using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(StickTiltForce))]
public class StickLossDragByScore : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody stickRigidbody;
    [SerializeField] private StickTiltForce stickTiltForce;
    [SerializeField] private ScoreCouter scoreCouter;

    [Header("Drag")]
    [SerializeField, Min(0f)] private float defaultDrag = 5f;
    [SerializeField, Min(0f)] private float maxDrag = 20f;
    [SerializeField, Min(0f)] private float defaultAngularDrag = 5f;
    [SerializeField, Min(0f)] private float maxAngularDrag = 20f;
    [SerializeField, Min(0.01f)] private float maxScoreForMaxDrag = 300f;
    [SerializeField, Range(0.01f, 180f)] private float maxTiltAngleForMaxDrag = 90f;

    private bool isRetryRequired;
    private float lossStartTiltAngle;
    private float lossTargetDrag;
    private float lossTargetAngularDrag;

    private void Reset()
    {
        stickRigidbody = GetComponent<Rigidbody>();
        stickTiltForce = GetComponent<StickTiltForce>();
    }

    private void Awake()
    {
        if (stickRigidbody == null)
        {
            stickRigidbody = GetComponent<Rigidbody>();
        }

        if (stickTiltForce == null)
        {
            stickTiltForce = GetComponent<StickTiltForce>();
        }

        if (stickRigidbody == null)
        {
            Debug.LogWarning("StickLossDragByScore: stickRigidbody is not assigned.", this);
        }

        if (stickTiltForce == null)
        {
            Debug.LogWarning("StickLossDragByScore: stickTiltForce is not assigned.", this);
        }

        if (scoreCouter == null)
        {
            Debug.LogWarning("StickLossDragByScore: scoreCouter is not assigned.", this);
        }
    }

    private void OnEnable()
    {
        ResetDragToDefault();

        if (stickTiltForce == null)
        {
            return;
        }

        stickTiltForce.RetryStateChanged -= HandleRetryStateChanged;
        stickTiltForce.RetryStateChanged += HandleRetryStateChanged;
        isRetryRequired = stickTiltForce.IsRetryRequired;
    }

    private void OnDisable()
    {
        if (stickTiltForce == null)
        {
            return;
        }

        stickTiltForce.RetryStateChanged -= HandleRetryStateChanged;
    }

    private void OnValidate()
    {
        defaultDrag = Mathf.Max(0f, defaultDrag);
        maxDrag = Mathf.Max(defaultDrag, maxDrag);
        defaultAngularDrag = Mathf.Max(0f, defaultAngularDrag);
        maxAngularDrag = Mathf.Max(defaultAngularDrag, maxAngularDrag);
        maxScoreForMaxDrag = Mathf.Max(0.01f, maxScoreForMaxDrag);
        maxTiltAngleForMaxDrag = Mathf.Clamp(maxTiltAngleForMaxDrag, 0.01f, 180f);
    }

    private void FixedUpdate()
    {
        if (!isRetryRequired || stickRigidbody == null)
        {
            return;
        }

        ApplyDragByTiltAngle();
    }

    private void HandleRetryStateChanged(bool retryRequired)
    {
        isRetryRequired = retryRequired;

        if (retryRequired)
        {
            CacheLossState();
            ApplyDragByTiltAngle();
            return;
        }

        ResetDragToDefault();
    }

    private void CacheLossState()
    {
        lossStartTiltAngle = Vector3.Angle(transform.up, Vector3.up);

        float currentScore = scoreCouter != null ? Mathf.Max(0f, scoreCouter.CurrentScoreValue) : 0f;
        float normalizedScore = Mathf.Clamp01(currentScore / maxScoreForMaxDrag);

        lossTargetDrag = Mathf.Lerp(defaultDrag, maxDrag, normalizedScore);
        lossTargetAngularDrag = Mathf.Lerp(defaultAngularDrag, maxAngularDrag, normalizedScore);
    }

    private void ApplyDragByTiltAngle()
    {
        float currentTiltAngle = Vector3.Angle(transform.up, Vector3.up);
        float safeMaxTiltAngle = Mathf.Max(lossStartTiltAngle + 0.01f, maxTiltAngleForMaxDrag);
        float normalizedTilt = Mathf.InverseLerp(lossStartTiltAngle, safeMaxTiltAngle, currentTiltAngle);

        stickRigidbody.drag = Mathf.Lerp(defaultDrag, lossTargetDrag, normalizedTilt);
        stickRigidbody.angularDrag = Mathf.Lerp(defaultAngularDrag, lossTargetAngularDrag, normalizedTilt);
    }

    private void ResetDragToDefault()
    {
        lossStartTiltAngle = 0f;
        lossTargetDrag = defaultDrag;
        lossTargetAngularDrag = defaultAngularDrag;

        if (stickRigidbody != null)
        {
            stickRigidbody.drag = defaultDrag;
            stickRigidbody.angularDrag = defaultAngularDrag;
        }
    }
}

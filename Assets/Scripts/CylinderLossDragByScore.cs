using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CylinderTiltForce))]
public class CylinderLossDragByScore : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody cylinderRigidbody;
    [SerializeField] private CylinderTiltForce cylinderTiltForce;
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
        cylinderRigidbody = GetComponent<Rigidbody>();
        cylinderTiltForce = GetComponent<CylinderTiltForce>();
    }

    private void Awake()
    {
        if (cylinderRigidbody == null)
        {
            cylinderRigidbody = GetComponent<Rigidbody>();
        }

        if (cylinderTiltForce == null)
        {
            cylinderTiltForce = GetComponent<CylinderTiltForce>();
        }

        if (cylinderRigidbody == null)
        {
            Debug.LogWarning("CylinderLossDragByScore: cylinderRigidbody is not assigned.", this);
        }

        if (cylinderTiltForce == null)
        {
            Debug.LogWarning("CylinderLossDragByScore: cylinderTiltForce is not assigned.", this);
        }

        if (scoreCouter == null)
        {
            Debug.LogWarning("CylinderLossDragByScore: scoreCouter is not assigned.", this);
        }
    }

    private void OnEnable()
    {
        ResetDragToDefault();

        if (cylinderTiltForce == null)
        {
            return;
        }

        cylinderTiltForce.RetryStateChanged -= HandleRetryStateChanged;
        cylinderTiltForce.RetryStateChanged += HandleRetryStateChanged;
        isRetryRequired = cylinderTiltForce.IsRetryRequired;
    }

    private void OnDisable()
    {
        if (cylinderTiltForce == null)
        {
            return;
        }

        cylinderTiltForce.RetryStateChanged -= HandleRetryStateChanged;
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
        if (!isRetryRequired || cylinderRigidbody == null)
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

        cylinderRigidbody.drag = Mathf.Lerp(defaultDrag, lossTargetDrag, normalizedTilt);
        cylinderRigidbody.angularDrag = Mathf.Lerp(defaultAngularDrag, lossTargetAngularDrag, normalizedTilt);
    }

    private void ResetDragToDefault()
    {
        lossStartTiltAngle = 0f;
        lossTargetDrag = defaultDrag;
        lossTargetAngularDrag = defaultAngularDrag;

        if (cylinderRigidbody != null)
        {
            cylinderRigidbody.drag = defaultDrag;
            cylinderRigidbody.angularDrag = defaultAngularDrag;
        }
    }
}

using UnityEngine;
using UnityEngine.Events;

public class StartGameWhenCubeIsHorizontal : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cubeTransform;
    [SerializeField] private GameObject cylinderObject;
    [SerializeField] private GameObject startTextObject;

    [Header("Condition")]
    [SerializeField] private float maxTiltAngleDegrees = 10f;
    [SerializeField] private float checkStartDelaySeconds = 5f;

    [Header("Events")]
    [SerializeField] private UnityEvent onGameStarted;

    private bool started;
    private bool startTextMissingLogged;
    private float checkDelayRemaining;
    private Transform cylinderTransform;
    private Rigidbody cylinderRigidbody;
    private Vector3 defaultCylinderPosition;
    private Quaternion defaultCylinderRotation;
    private bool cylinderDefaultsCaptured;

    private void OnEnable()
    {
        ApplyOnEnableState(checkStartDelaySeconds);
    }

    public void ApplyOnEnableState(float delay)
    {
        started = false;
        startTextMissingLogged = false;
        checkDelayRemaining = Mathf.Max(0f, delay);

        ValidateReferences();
        EnsureCylinderDefaultStateCaptured();

        if (cylinderObject != null)
        {
            cylinderObject.SetActive(false);
        }

        SetStartTextVisible(true);
    }

    private void Update()
    {
        if (started || cubeTransform == null)
        {
            return;
        }

        if (checkDelayRemaining > 0f)
        {
            checkDelayRemaining -= Time.unscaledDeltaTime;
            return;
        }

        float tiltAngle = Vector3.Angle(cubeTransform.up, Vector3.up);
        if (tiltAngle <= maxTiltAngleDegrees)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        started = true;
        ApplyCylinderResetState();

        SetStartTextVisible(false);
        onGameStarted?.Invoke();
    }

    private void ValidateReferences()
    {
        if (cubeTransform == null)
        {
            Debug.LogWarning("StartGameWhenCubeIsHorizontal: cubeTransform is not assigned.");
        }

        if (cylinderObject == null)
        {
            Debug.LogWarning("StartGameWhenCubeIsHorizontal: cylinderObject is not assigned.");
        }
        else
        {
            cylinderTransform = cylinderObject.transform;
            if (cylinderRigidbody == null)
            {
                cylinderRigidbody = cylinderObject.GetComponent<Rigidbody>();
            }
        }

        if (startTextObject == null)
        {
            Debug.LogWarning("StartGameWhenCubeIsHorizontal: startTextObject is not assigned (expected Canvas/StartText).");
        }
    }

    private void SetStartTextVisible(bool visible)
    {
        if (startTextObject != null)
        {
            startTextObject.SetActive(visible);
            return;
        }

        if (!startTextMissingLogged)
        {
            Debug.LogWarning("StartGameWhenCubeIsHorizontal: StartText was not found. Expected object 'Canvas/StartText'.");
            startTextMissingLogged = true;
        }
    }

    private void EnsureCylinderDefaultStateCaptured()
    {
        if (cylinderDefaultsCaptured || cylinderTransform == null)
        {
            return;
        }

        defaultCylinderPosition = cylinderTransform.position;
        defaultCylinderRotation = cylinderTransform.rotation;
        cylinderDefaultsCaptured = true;
    }

    private void ApplyCylinderResetState()
    {
        if (cylinderObject == null || cylinderTransform == null)
        {
            return;
        }

        if (!cylinderDefaultsCaptured)
        {
            EnsureCylinderDefaultStateCaptured();
        }

        if (!cylinderObject.activeSelf)
        {
            cylinderObject.SetActive(true);
        }

        cylinderTransform.SetPositionAndRotation(defaultCylinderPosition, defaultCylinderRotation);

        if (cylinderRigidbody == null)
        {
            return;
        }

        cylinderRigidbody.velocity = Vector3.zero;
        cylinderRigidbody.angularVelocity = Vector3.zero;
        cylinderRigidbody.WakeUp();
    }
}

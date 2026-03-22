using UnityEngine;
using UnityEngine.Events;

public class StartGameWhenCubeIsHorizontal : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cubeTransform;
    [SerializeField] private GameObject stickObject;
    [SerializeField] private GameObject startTextObject;

    [Header("Condition")]
    [SerializeField] private float maxTiltAngleDegrees = 10f;
    [SerializeField] private float checkStartDelaySeconds = 5f;

    [Header("Events")]
    [SerializeField] private UnityEvent onGameStarted;

    private bool started;
    private bool startTextMissingLogged;
    private float checkDelayRemaining;
    private Transform stickTransform;
    private Rigidbody stickRigidbody;
    private Vector3 defaultStickPosition;
    private Quaternion defaultStickRotation;
    private bool stickDefaultsCaptured;

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
        EnsureStickDefaultStateCaptured();

        if (stickObject != null)
        {
            stickObject.SetActive(false);
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
        ApplyStickResetState();

        SetStartTextVisible(false);
        onGameStarted?.Invoke();
    }

    private void ValidateReferences()
    {
        if (cubeTransform == null)
        {
            Debug.LogWarning("StartGameWhenCubeIsHorizontal: cubeTransform is not assigned.");
        }

        if (stickObject == null)
        {
            Debug.LogWarning("StartGameWhenCubeIsHorizontal: stickObject is not assigned.");
        }
        else
        {
            stickTransform = stickObject.transform;
            if (stickRigidbody == null)
            {
                stickRigidbody = stickObject.GetComponent<Rigidbody>();
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

    private void EnsureStickDefaultStateCaptured()
    {
        if (stickDefaultsCaptured || stickTransform == null)
        {
            return;
        }

        defaultStickPosition = stickTransform.position;
        defaultStickRotation = stickTransform.rotation;
        stickDefaultsCaptured = true;
    }

    private void ApplyStickResetState()
    {
        if (stickObject == null || stickTransform == null)
        {
            return;
        }

        if (!stickDefaultsCaptured)
        {
            EnsureStickDefaultStateCaptured();
        }

        if (!stickObject.activeSelf)
        {
            stickObject.SetActive(true);
        }

        stickTransform.SetPositionAndRotation(defaultStickPosition, defaultStickRotation);

        if (stickRigidbody == null)
        {
            return;
        }

        stickRigidbody.velocity = Vector3.zero;
        stickRigidbody.angularVelocity = Vector3.zero;
        stickRigidbody.WakeUp();
    }
}

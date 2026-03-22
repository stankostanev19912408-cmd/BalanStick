using UnityEngine;
using UnityEngine.UI;

public class BoostChargeBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform stickTransform;
    [SerializeField] private Rigidbody stickRigidbody;
    [SerializeField] private StickTiltForce stickTiltForce;
    [SerializeField] private Image barFillImage;
    [SerializeField] private Button boostButton;

    [Header("Charge")]
    [SerializeField] private bool resetChargeOnEnable = true;
    [SerializeField, Min(0.01f)] private float maxCharge = 100f;
    [SerializeField, Min(0f)] private float minSpeedForCharge = 0.2f;
    [SerializeField, Min(0.01f)] private float maxSpeedForMaxCharge = 12f;
    [SerializeField, Min(0f)] private float minTiltAngleForCharge = 5f;
    [SerializeField, Range(0f, 90f)] private float maxTiltAngleForMaxCharge = 45f;
    [SerializeField, Min(0f)] private float maxSpeedChargePerSecond = 25f;
    [SerializeField, Min(0f)] private float maxTiltChargePerSecond = 25f;
    [SerializeField] private AnimationCurve speedChargeCurve;
    [SerializeField] private AnimationCurve tiltChargeCurve;

    [Header("Boost")]
    [SerializeField, Min(0.01f)] private float boostDrainDuration = 3f;
    [SerializeField, Min(1f)] private float boostScoreMultiplier = 3f;

    private float currentCharge;
    private bool isRetryRequired;
    private bool isInputUnlocked = true;
    private bool isBoostActive;

    public float CurrentChargeValue => currentCharge;
    public float CurrentFillAmount => Mathf.Clamp01(currentCharge / Mathf.Max(0.01f, maxCharge));
    public bool IsBoostActive => isBoostActive;
    public float CurrentScoreMultiplier => isBoostActive ? Mathf.Max(1f, boostScoreMultiplier) : 1f;

    private void Awake()
    {
        if (stickTransform == null)
        {
            Debug.LogWarning("BoostChargeBar: stickTransform is not assigned.", this);
        }

        if (stickRigidbody == null)
        {
            Debug.LogWarning("BoostChargeBar: stickRigidbody is not assigned.", this);
        }

        if (stickTiltForce == null)
        {
            Debug.LogWarning("BoostChargeBar: stickTiltForce is not assigned.", this);
        }

        if (barFillImage == null)
        {
            Debug.LogWarning("BoostChargeBar: barFillImage is not assigned.", this);
        }

        if (boostButton == null)
        {
            Debug.LogWarning("BoostChargeBar: boostButton is not assigned.", this);
        }
    }

    private void OnEnable()
    {
        if (resetChargeOnEnable)
        {
            ResetCharge();
        }

        if (stickTiltForce != null)
        {
            stickTiltForce.RetryStateChanged -= HandleRetryStateChanged;
            stickTiltForce.RetryStateChanged += HandleRetryStateChanged;
            stickTiltForce.StartGateStateChanged -= HandleStartGateStateChanged;
            stickTiltForce.StartGateStateChanged += HandleStartGateStateChanged;
            isRetryRequired = stickTiltForce.IsRetryRequired;
            isInputUnlocked = stickTiltForce.IsInputUnlocked;
        }

        BindButton();
        UpdateBoostButtonState();
        UpdateBarFill();
    }

    private void OnDisable()
    {
        UnbindButton();

        if (stickTiltForce == null)
        {
            return;
        }

        stickTiltForce.RetryStateChanged -= HandleRetryStateChanged;
        stickTiltForce.StartGateStateChanged -= HandleStartGateStateChanged;
    }

    private void OnValidate()
    {
        maxCharge = Mathf.Max(0.01f, maxCharge);
        minSpeedForCharge = Mathf.Max(0f, minSpeedForCharge);
        maxSpeedForMaxCharge = Mathf.Max(minSpeedForCharge + 0.0001f, maxSpeedForMaxCharge);
        minTiltAngleForCharge = Mathf.Max(0f, minTiltAngleForCharge);
        maxTiltAngleForMaxCharge = Mathf.Clamp(maxTiltAngleForMaxCharge, minTiltAngleForCharge + 0.0001f, 90f);
        maxSpeedChargePerSecond = Mathf.Max(0f, maxSpeedChargePerSecond);
        maxTiltChargePerSecond = Mathf.Max(0f, maxTiltChargePerSecond);
        boostDrainDuration = Mathf.Max(0.01f, boostDrainDuration);
        boostScoreMultiplier = Mathf.Max(1f, boostScoreMultiplier);
    }

    private void Update()
    {
        if (stickTiltForce == null)
        {
            return;
        }

        bool isStickInactive = stickTransform != null && !stickTransform.gameObject.activeInHierarchy;
        bool canCharge = !isStickInactive && !isRetryRequired && isInputUnlocked;

        if (isBoostActive)
        {
            DrainBoost();
        }
        else if (canCharge)
        {
            float chargePerSecond = EvaluateChargeFromStick();
            if (chargePerSecond > 0f)
            {
                currentCharge = Mathf.Min(maxCharge, currentCharge + (chargePerSecond * Time.deltaTime));
            }
        }

        UpdateBoostButtonState();
        UpdateBarFill();
    }

    private float EvaluateChargeFromStick()
    {
        if (stickTransform == null || stickRigidbody == null)
        {
            return 0f;
        }

        float speed = stickRigidbody.velocity.magnitude;
        float tiltAngle = Vector3.Angle(stickTransform.up, Vector3.up);
        return EvaluateSpeedCharge(speed) + EvaluateTiltCharge(tiltAngle);
    }

    private float EvaluateSpeedCharge(float speed)
    {
        float clampedMaxSpeed = Mathf.Max(minSpeedForCharge + 0.0001f, maxSpeedForMaxCharge);
        float normalizedSpeed = Mathf.InverseLerp(minSpeedForCharge, clampedMaxSpeed, speed);
        float curveValue = EvaluateChargeCurve(speedChargeCurve, normalizedSpeed);
        return curveValue * maxSpeedChargePerSecond;
    }

    private float EvaluateTiltCharge(float tiltAngle)
    {
        float clampedMaxTilt = Mathf.Max(minTiltAngleForCharge + 0.0001f, maxTiltAngleForMaxCharge);
        float normalizedTilt = Mathf.InverseLerp(minTiltAngleForCharge, clampedMaxTilt, tiltAngle);
        float curveValue = EvaluateChargeCurve(tiltChargeCurve, normalizedTilt);
        return curveValue * maxTiltChargePerSecond;
    }

    private float EvaluateChargeCurve(AnimationCurve curve, float normalizedValue)
    {
        if (curve == null || curve.length == 0)
        {
            return normalizedValue;
        }

        return Mathf.Max(0f, curve.Evaluate(normalizedValue));
    }

    private void HandleRetryStateChanged(bool retryRequired)
    {
        isRetryRequired = retryRequired;

        if (retryRequired)
        {
            StopBoost();
            UpdateBoostButtonState();
            return;
        }

        ResetCharge();
    }

    private void HandleStartGateStateChanged(bool inputUnlocked)
    {
        isInputUnlocked = inputUnlocked;
        UpdateBoostButtonState();
    }

    private void ResetCharge()
    {
        isBoostActive = false;
        currentCharge = 0f;
        UpdateBoostButtonState();
        UpdateBarFill();
    }

    private void UpdateBarFill()
    {
        if (barFillImage == null)
        {
            return;
        }

        barFillImage.fillAmount = CurrentFillAmount;
    }

    private void BindButton()
    {
        if (boostButton == null)
        {
            return;
        }

        boostButton.onClick.RemoveListener(HandleBoostButtonClicked);
        boostButton.onClick.AddListener(HandleBoostButtonClicked);
    }

    private void UnbindButton()
    {
        if (boostButton == null)
        {
            return;
        }

        boostButton.onClick.RemoveListener(HandleBoostButtonClicked);
    }

    private void HandleBoostButtonClicked()
    {
        if (!CanActivateBoost())
        {
            return;
        }

        isBoostActive = true;
        currentCharge = maxCharge;
        UpdateBoostButtonState();
        UpdateBarFill();
    }

    private void DrainBoost()
    {
        float drainPerSecond = maxCharge / Mathf.Max(0.01f, boostDrainDuration);
        currentCharge = Mathf.Max(0f, currentCharge - (drainPerSecond * Time.deltaTime));

        if (currentCharge <= 0f)
        {
            StopBoost();
        }
    }

    private void StopBoost()
    {
        isBoostActive = false;
        currentCharge = Mathf.Max(0f, currentCharge);
        UpdateBoostButtonState();
        UpdateBarFill();
    }

    private bool CanActivateBoost()
    {
        if (isBoostActive || isRetryRequired || !isInputUnlocked)
        {
            return false;
        }

        return CurrentFillAmount >= 0.9999f;
    }

    private void UpdateBoostButtonState()
    {
        if (boostButton == null)
        {
            return;
        }

        boostButton.interactable = CanActivateBoost();
    }
}

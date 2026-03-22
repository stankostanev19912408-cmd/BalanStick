using UnityEngine;
using UnityEngine.UI;

public class StickResetUI : MonoBehaviour
{
    [SerializeField] private Button resetButton;
    [SerializeField] private StartGameWhenCubeIsHorizontal startGameWhenCubeIsHorizontal;
    [SerializeField] private Transform cubeTransform;

    private Rigidbody cubeRigidbody;
    private Vector3 defaultCubePosition;
    private Quaternion defaultCubeRotation;
    private bool cubeDefaultsCaptured;

    private void Awake()
    {
        if (startGameWhenCubeIsHorizontal == null)
        {
            Debug.LogWarning("StickResetUI: startGameWhenCubeIsHorizontal is not assigned.");
        }

        if (cubeTransform == null)
        {
            Debug.LogWarning("StickResetUI: cubeTransform is not assigned.");
        }
        else
        {
            cubeRigidbody = cubeTransform.GetComponent<Rigidbody>();
            CaptureCubeDefaultsIfNeeded();
        }
    }

    private void Start()
    {
        BindButton();
    }

    private void OnDestroy()
    {
        UnbindButton();
    }

    public void ResetStick()
    {
        ResetCubeToDefaultTransform();

        if (startGameWhenCubeIsHorizontal == null)
        {
            return;
        }

        startGameWhenCubeIsHorizontal.ApplyOnEnableState(2f);
    }

    private void BindButton()
    {
        if (resetButton == null)
        {
            return;
        }

        resetButton.onClick.RemoveListener(ResetStick);
        resetButton.onClick.AddListener(ResetStick);
    }

    private void UnbindButton()
    {
        if (resetButton == null)
        {
            return;
        }

        resetButton.onClick.RemoveListener(ResetStick);
    }

    private void ResetCubeToDefaultTransform()
    {
        if (cubeTransform == null)
        {
            return;
        }

        CaptureCubeDefaultsIfNeeded();

        cubeTransform.SetPositionAndRotation(defaultCubePosition, defaultCubeRotation);

        if (cubeRigidbody == null)
        {
            return;
        }

        cubeRigidbody.velocity = Vector3.zero;
        cubeRigidbody.angularVelocity = Vector3.zero;
        cubeRigidbody.WakeUp();
    }

    private void CaptureCubeDefaultsIfNeeded()
    {
        if (cubeDefaultsCaptured || cubeTransform == null)
        {
            return;
        }

        defaultCubePosition = cubeTransform.position;
        defaultCubeRotation = cubeTransform.rotation;
        cubeDefaultsCaptured = true;
    }
}

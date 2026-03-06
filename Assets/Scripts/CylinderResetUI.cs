using UnityEngine;
using UnityEngine.UI;

public class CylinderResetUI : MonoBehaviour
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
            Debug.LogWarning("CylinderResetUI: startGameWhenCubeIsHorizontal is not assigned.");
        }

        if (cubeTransform == null)
        {
            Debug.LogWarning("CylinderResetUI: cubeTransform is not assigned.");
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

    public void ResetCylinder()
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

        resetButton.onClick.RemoveListener(ResetCylinder);
        resetButton.onClick.AddListener(ResetCylinder);
    }

    private void UnbindButton()
    {
        if (resetButton == null)
        {
            return;
        }

        resetButton.onClick.RemoveListener(ResetCylinder);
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

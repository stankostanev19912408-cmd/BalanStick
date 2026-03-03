using UnityEngine;
using UnityEngine.UI;

public class CylinderResetUI : MonoBehaviour
{
    [SerializeField] private Button resetButton;
    [SerializeField] private StartGameWhenCubeIsHorizontal startGameWhenCubeIsHorizontal;

    private void Awake()
    {
        if (startGameWhenCubeIsHorizontal == null)
        {
            Debug.LogWarning("CylinderResetUI: startGameWhenCubeIsHorizontal is not assigned.");
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
}

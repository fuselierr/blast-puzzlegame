using UnityEngine;
using UnityEngine.UI;

public class AccessibilitySettingsUI : MonoBehaviour
{
    [SerializeField] private Toggle colorblindToggle;
    [SerializeField] private Toggle highContrastToggle;

    private void Start()
    {
        if (AccessibilityPostProcessing.Instance == null)
        {
            Debug.LogError(
                "AccessibilityPostProcessing was not bootstrapped.",
                this);
            return;
        }

        colorblindToggle.SetIsOnWithoutNotify(
            AccessibilityPostProcessing.Instance.ColorblindEnabled);

        highContrastToggle.SetIsOnWithoutNotify(
            AccessibilityPostProcessing.Instance.HighContrastEnabled);

        colorblindToggle.onValueChanged.AddListener(
            AccessibilityPostProcessing.Instance.SetColorblindMode);

        highContrastToggle.onValueChanged.AddListener(
            AccessibilityPostProcessing.Instance.SetHighContrastMode);
    }

    private void OnDestroy()
    {
        if (AccessibilityPostProcessing.Instance == null)
        {
            return;
        }

        colorblindToggle.onValueChanged.RemoveListener(
            AccessibilityPostProcessing.Instance.SetColorblindMode);

        highContrastToggle.onValueChanged.RemoveListener(
            AccessibilityPostProcessing.Instance.SetHighContrastMode);
    }
}
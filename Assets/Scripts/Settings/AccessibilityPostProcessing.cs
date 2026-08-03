using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class AccessibilityPostProcessing : MonoBehaviour
{
    public static AccessibilityPostProcessing Instance { get; private set; }

    private const string ColorblindPref = "ColorblindMode";
    private const string HighContrastPref = "HighContrastMode";

    [Header("URP Volume")]
    [SerializeField] private Volume globalVolume;

    [Header("Optional UI Toggles")]
    [SerializeField] private Toggle colorblindToggle;
    [SerializeField] private Toggle highContrastToggle;

    [Header("High Contrast")]
    [SerializeField, Range(0f, 100f)]
    private float highContrastAmount = 35f;

    private ColorAdjustments colorAdjustments;
    private ChannelMixer channelMixer;

    private bool colorblindEnabled;
    private bool highContrastEnabled;

    public bool ColorblindEnabled => colorblindEnabled;
    public bool HighContrastEnabled => highContrastEnabled;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!InitializeVolume())
        {
            enabled = false;
            return;
        }

        LoadSettings();
        ConnectToggles();
        ApplySettings();
    }

    private bool InitializeVolume()
    {
        if (globalVolume == null)
        {
            globalVolume = FindObjectOfType<Volume>();
        }

        if (globalVolume == null || globalVolume.profile == null)
        {
            Debug.LogError(
                "AccessibilityPostProcessing requires a Global Volume.",
                this);

            return false;
        }

        VolumeProfile profile = globalVolume.profile;

        if (!profile.TryGet(out colorAdjustments))
        {
            colorAdjustments = profile.Add<ColorAdjustments>(true);
        }

        if (!profile.TryGet(out channelMixer))
        {
            channelMixer = profile.Add<ChannelMixer>(true);
        }

        return true;
    }

    private void LoadSettings()
    {
        colorblindEnabled =
            PlayerPrefs.GetInt(ColorblindPref, 0) == 1;

        highContrastEnabled =
            PlayerPrefs.GetInt(HighContrastPref, 0) == 1;

        if (colorblindToggle != null)
        {
            colorblindToggle.SetIsOnWithoutNotify(
                colorblindEnabled);
        }

        if (highContrastToggle != null)
        {
            highContrastToggle.SetIsOnWithoutNotify(
                highContrastEnabled);
        }
    }

    private void ConnectToggles()
    {
        if (colorblindToggle != null)
        {
            colorblindToggle.onValueChanged.RemoveListener(
                SetColorblindMode);

            colorblindToggle.onValueChanged.AddListener(
                SetColorblindMode);
        }

        if (highContrastToggle != null)
        {
            highContrastToggle.onValueChanged.RemoveListener(
                SetHighContrastMode);

            highContrastToggle.onValueChanged.AddListener(
                SetHighContrastMode);
        }
    }

    public void SetColorblindMode(bool enabled)
    {
        colorblindEnabled = enabled;

        PlayerPrefs.SetInt(
            ColorblindPref,
            enabled ? 1 : 0);

        PlayerPrefs.Save();
        ApplyColorblindMode();
    }

    public void SetHighContrastMode(bool enabled)
    {
        highContrastEnabled = enabled;

        PlayerPrefs.SetInt(
            HighContrastPref,
            enabled ? 1 : 0);

        PlayerPrefs.Save();
        ApplyHighContrastMode();
    }

    private void ApplySettings()
    {
        ApplyColorblindMode();
        ApplyHighContrastMode();
    }

    private void ApplyHighContrastMode()
    {
        colorAdjustments.active = highContrastEnabled;
        colorAdjustments.contrast.overrideState = true;

        colorAdjustments.contrast.value =
            highContrastEnabled
                ? highContrastAmount
                : 0f;
    }

    private void ApplyColorblindMode()
    {
        channelMixer.active = colorblindEnabled;

        SetChannel(
            channelMixer.redOutRedIn,
            channelMixer.redOutGreenIn,
            channelMixer.redOutBlueIn,
            colorblindEnabled ? 100f : 100f,
            colorblindEnabled ? 20f : 0f,
            0f);

        SetChannel(
            channelMixer.greenOutRedIn,
            channelMixer.greenOutGreenIn,
            channelMixer.greenOutBlueIn,
            0f,
            colorblindEnabled ? 80f : 100f,
            colorblindEnabled ? 20f : 0f);

        SetChannel(
            channelMixer.blueOutRedIn,
            channelMixer.blueOutGreenIn,
            channelMixer.blueOutBlueIn,
            colorblindEnabled ? 15f : 0f,
            colorblindEnabled ? 15f : 0f,
            colorblindEnabled ? 70f : 100f);
    }

    private static void SetChannel(
        ClampedFloatParameter red,
        ClampedFloatParameter green,
        ClampedFloatParameter blue,
        float redValue,
        float greenValue,
        float blueValue)
    {
        red.overrideState = true;
        green.overrideState = true;
        blue.overrideState = true;

        red.value = redValue;
        green.value = greenValue;
        blue.value = blueValue;
    }

    private void OnDestroy()
    {
        if (colorblindToggle != null)
        {
            colorblindToggle.onValueChanged.RemoveListener(
                SetColorblindMode);
        }

        if (highContrastToggle != null)
        {
            highContrastToggle.onValueChanged.RemoveListener(
                SetHighContrastMode);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
}
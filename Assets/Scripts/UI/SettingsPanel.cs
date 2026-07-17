using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    void Start()
    {
        SoundManager sound = SoundManager.GetOrCreate();

        // Initialize sliders to current saved values
        masterSlider.SetValueWithoutNotify(sound.CurrentMasterVolume);
        musicSlider.SetValueWithoutNotify(sound.CurrentMusicVolume);
        sfxSlider.SetValueWithoutNotify(sound.CurrentSfxVolume);

        // Hook up listeners
        masterSlider.onValueChanged.AddListener(sound.SetMasterVolume);
        musicSlider.onValueChanged.AddListener(sound.SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(sound.SetSfxVolume);
    }

}
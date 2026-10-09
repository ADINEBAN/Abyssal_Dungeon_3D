using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AbyssalDungeon.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Graphics Settings")]
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Toggle fullscreenToggle;

        [Header("Buttons")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;

        private void Start()
        {
            applyButton.onClick.AddListener(ApplySettings);
            resetButton.onClick.AddListener(ResetToDefault);
            
            LoadSettings();
        }

        private void LoadSettings()
        {
            masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVol", 1f);
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVol", 1f);
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVol", 1f);

            qualityDropdown.value = PlayerPrefs.GetInt("Quality", QualitySettings.GetQualityLevel());
            fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        }

        private void ApplySettings()
        {
            // Audio (Normally route to AudioMixer)
            PlayerPrefs.SetFloat("MasterVol", masterVolumeSlider.value);
            PlayerPrefs.SetFloat("MusicVol", musicVolumeSlider.value);
            PlayerPrefs.SetFloat("SFXVol", sfxVolumeSlider.value);

            // Graphics
            QualitySettings.SetQualityLevel(qualityDropdown.value);
            PlayerPrefs.SetInt("Quality", qualityDropdown.value);

            Screen.fullScreen = fullscreenToggle.isOn;
            PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);

            PlayerPrefs.Save();
            Debug.Log("Settings Applied and Saved.");
        }

        private void ResetToDefault()
        {
            masterVolumeSlider.value = 1f;
            musicVolumeSlider.value = 1f;
            sfxVolumeSlider.value = 1f;
            
            qualityDropdown.value = 2; // Medium/High
            fullscreenToggle.isOn = true;
            
            ApplySettings();
        }
    }
}

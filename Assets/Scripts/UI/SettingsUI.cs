using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AbyssGame;

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
            if (applyButton != null) applyButton.onClick.AddListener(ApplySettings);
            if (resetButton != null) resetButton.onClick.AddListener(ResetToDefault);

            PopulateQualityDropdown();
            LoadSettings();
        }

        private void PopulateQualityDropdown()
        {
            if (qualityDropdown == null) return;
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string> { "Low", "Medium", "High", "Ultra", "Cinematic" });
        }

        private void LoadSettings()
        {
            if (masterVolumeSlider != null) masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVol", 1f);
            if (musicVolumeSlider != null) musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVol", 1f);
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVol", 1f);

            int curTier = GraphicsManager.Instance != null
                ? (int)GraphicsManager.Instance.CurrentTier
                : PlayerPrefs.GetInt("Quality", QualitySettings.GetQualityLevel());

            if (qualityDropdown != null) qualityDropdown.value = Mathf.Clamp(curTier, 0, 4);
            if (fullscreenToggle != null) fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        }

        private void ApplySettings()
        {
            if (masterVolumeSlider != null) PlayerPrefs.SetFloat("MasterVol", masterVolumeSlider.value);
            if (musicVolumeSlider != null) PlayerPrefs.SetFloat("MusicVol", musicVolumeSlider.value);
            if (sfxVolumeSlider != null) PlayerPrefs.SetFloat("SFXVol", sfxVolumeSlider.value);

            int selected = qualityDropdown != null ? Mathf.Clamp(qualityDropdown.value, 0, 4) : 1;
            if (GraphicsManager.Instance != null)
            {
                GraphicsManager.Instance.SetQualityTier((GraphicsQualityTier)selected, isUserManualSelection: true, saveImmediately: true);
                if (fullscreenToggle != null)
                    GraphicsManager.Instance.SetFullscreen(fullscreenToggle.isOn);
            }
            else
            {
                QualitySettings.SetQualityLevel(selected);
                PlayerPrefs.SetInt("Quality", selected);
                if (fullscreenToggle != null)
                {
                    Screen.fullScreen = fullscreenToggle.isOn;
                    PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
                }
                PlayerPrefs.Save();
            }

            Debug.Log("Settings Applied and Saved.");
        }

        private void ResetToDefault()
        {
            if (masterVolumeSlider != null) masterVolumeSlider.value = 1f;
            if (musicVolumeSlider != null) musicVolumeSlider.value = 1f;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = 1f;

            if (GraphicsManager.Instance != null)
            {
                GraphicsManager.Instance.ApplyAutoRecommendedPreset();
                if (qualityDropdown != null)
                    qualityDropdown.value = (int)GraphicsManager.Instance.CurrentTier;
            }
            else if (qualityDropdown != null)
            {
                qualityDropdown.value = (int)GraphicsDetector.AnalyzeHardware().staticRecommendedTier;
            }

            if (fullscreenToggle != null) fullscreenToggle.isOn = true;
            ApplySettings();
        }
    }
}

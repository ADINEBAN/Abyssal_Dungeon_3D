using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace AbyssGame
{
    [Serializable]
    public class UserGraphicsSettingsData
    {
        public const int CurrentSchemaVersion = 2;

        public int schemaVersion = CurrentSchemaVersion;
        public int selectedTier = (int)GraphicsQualityTier.Medium;
        public bool hasUserManualSelection = false;
        public bool hasCustomSubOptionOverrides = false;

        // Display & Frame Rate
        public int resolutionWidth = 1920;
        public int resolutionHeight = 1080;
        public int refreshRate = 60;
        public int displayMode = (int)DisplayModeOption.Fullscreen;
        public bool fullscreen = true;
        public int targetFrameRate = 60; // 30, 60, 90, 120, or -1 (Unlimited)
        public bool vSyncEnabled = true;

        // Individual Granular Graphics Options (Section 8 UI Settings)
        public float renderScale = 1.0f;
        public int shadowQuality = (int)ShadowQualityOption.High;
        public int textureQuality = (int)TextureQualityOption.Full;
        public int antiAliasing = (int)AntiAliasingOption.MSAA2x_SMAA;
        public int ambientOcclusion = (int)AmbientOcclusionOption.HalfRes;
        public int bloomQuality = (int)BloomQualityOption.Medium;
        public int vfxQuality = (int)VfxQualityOption.High;
        public int viewDistance = (int)ViewDistanceOption.Far;
        public float effectsDensity = 1.0f;

        // Adaptive Quality Flags
        public bool adaptiveResolutionEnabled = true;
        public bool adaptiveQualityDowngradeEnabled = false;
        public bool allowAdaptiveDuringCinematic = false;

        public string lastDetectedGpu = "";

        public void SyncFromPreset(GraphicsPreset preset)
        {
            if (preset == null) return;
            selectedTier = (int)preset.tier;
            hasCustomSubOptionOverrides = false;
            renderScale = preset.renderScale;
            shadowQuality = (int)preset.shadowQualityOption;
            textureQuality = (int)preset.textureQualityOption;
            antiAliasing = (int)preset.antiAliasingOption;
            ambientOcclusion = (int)preset.ambientOcclusionOption;
            bloomQuality = (int)preset.bloomQualityOption;
            vfxQuality = (int)preset.vfxQualityOption;
            viewDistance = (int)preset.viewDistanceOption;
            effectsDensity = preset.particleDensityMultiplier;
        }
    }

    /// <summary>
    /// Atomic crash-safe persistence layer for UserGraphicsSettingsData (save-systems skill Pattern 2 & 3).
    /// Writes to .tmp, rotates previous good file to .bak, and renames to graphics_settings.json,
    /// with schema version migration and PlayerPrefs fallback mirror.
    /// </summary>
    public static class GraphicsSettingsPersistence
    {
        private const string SettingsFileName = "graphics_settings.json";
        public static string SettingsPath => Path.Combine(Application.persistentDataPath, SettingsFileName);

        public static void Save(UserGraphicsSettingsData data)
        {
            if (data == null) return;
            data.schemaVersion = UserGraphicsSettingsData.CurrentSchemaVersion;
            data.fullscreen = ((DisplayModeOption)data.displayMode) != DisplayModeOption.Windowed;

            try
            {
                string json = JsonUtility.ToJson(data, true);
                string tmp = SettingsPath + ".tmp";
                string bak = SettingsPath + ".bak";
                File.WriteAllText(tmp, json, Encoding.UTF8);
                if (File.Exists(SettingsPath))
                {
                    if (File.Exists(bak)) File.Delete(bak);
                    File.Move(SettingsPath, bak);
                }
                File.Move(tmp, SettingsPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GraphicsSettingsPersistence] Failed writing graphics_settings.json: " + e.Message);
            }

            PlayerPrefs.SetInt("Abyssal_Graphics_Schema", data.schemaVersion);
            PlayerPrefs.SetInt("Abyssal_Graphics_Tier", data.selectedTier);
            PlayerPrefs.SetInt("Abyssal_Graphics_Manual", data.hasUserManualSelection ? 1 : 0);
            PlayerPrefs.SetInt("Abyssal_Graphics_Custom", data.hasCustomSubOptionOverrides ? 1 : 0);
            PlayerPrefs.SetInt("Abyssal_Graphics_ResW", data.resolutionWidth);
            PlayerPrefs.SetInt("Abyssal_Graphics_ResH", data.resolutionHeight);
            PlayerPrefs.SetInt("Abyssal_Graphics_DisplayMode", data.displayMode);
            PlayerPrefs.SetInt("Abyssal_Graphics_FpsLimit", data.targetFrameRate);
            PlayerPrefs.SetInt("Abyssal_Graphics_VSync", data.vSyncEnabled ? 1 : 0);
            PlayerPrefs.SetFloat("Abyssal_Graphics_RenderScale", data.renderScale);
            PlayerPrefs.SetInt("Abyssal_Graphics_Shadow", data.shadowQuality);
            PlayerPrefs.SetInt("Abyssal_Graphics_Texture", data.textureQuality);
            PlayerPrefs.SetInt("Abyssal_Graphics_AA", data.antiAliasing);
            PlayerPrefs.SetInt("Abyssal_Graphics_AO", data.ambientOcclusion);
            PlayerPrefs.SetInt("Abyssal_Graphics_Bloom", data.bloomQuality);
            PlayerPrefs.SetInt("Abyssal_Graphics_VFX", data.vfxQuality);
            PlayerPrefs.SetInt("Abyssal_Graphics_ViewDist", data.viewDistance);
            PlayerPrefs.SetFloat("Abyssal_Graphics_Density", data.effectsDensity);
            PlayerPrefs.SetInt("Abyssal_Graphics_AdaptiveRes", data.adaptiveResolutionEnabled ? 1 : 0);
            PlayerPrefs.SetInt("Abyssal_Graphics_AutoDowngrade", data.adaptiveQualityDowngradeEnabled ? 1 : 0);
            PlayerPrefs.SetInt("Abyssal_Graphics_Fullscreen", data.fullscreen ? 1 : 0);
            PlayerPrefs.SetInt("Quality", data.selectedTier);
            PlayerPrefs.Save();
        }

        public static bool Load(out UserGraphicsSettingsData data)
        {
            data = CreateDefaultSettings();

            try
            {
                string[] candidates = { SettingsPath, SettingsPath + ".bak" };
                foreach (string path in candidates)
                {
                    if (!File.Exists(path)) continue;
                    string json = File.ReadAllText(path, Encoding.UTF8);
                    var parsed = JsonUtility.FromJson<UserGraphicsSettingsData>(json);
                    if (parsed != null)
                    {
                        MigrateAndValidate(parsed);
                        data = parsed;
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GraphicsSettingsPersistence] Failed reading graphics_settings.json: " + e.Message);
            }

            if (PlayerPrefs.HasKey("Abyssal_Graphics_Tier"))
            {
                data.selectedTier = PlayerPrefs.GetInt("Abyssal_Graphics_Tier", (int)GraphicsQualityTier.Medium);
                data.hasUserManualSelection = PlayerPrefs.GetInt("Abyssal_Graphics_Manual", 0) == 1;
                data.hasCustomSubOptionOverrides = PlayerPrefs.GetInt("Abyssal_Graphics_Custom", 0) == 1;
                data.resolutionWidth = PlayerPrefs.GetInt("Abyssal_Graphics_ResW", Screen.width);
                data.resolutionHeight = PlayerPrefs.GetInt("Abyssal_Graphics_ResH", Screen.height);
                data.displayMode = PlayerPrefs.GetInt("Abyssal_Graphics_DisplayMode", Screen.fullScreen ? 0 : 2);
                data.targetFrameRate = PlayerPrefs.GetInt("Abyssal_Graphics_FpsLimit", 60);
                data.vSyncEnabled = PlayerPrefs.GetInt("Abyssal_Graphics_VSync", 1) == 1;
                data.renderScale = PlayerPrefs.GetFloat("Abyssal_Graphics_RenderScale", 1.0f);
                data.shadowQuality = PlayerPrefs.GetInt("Abyssal_Graphics_Shadow", (int)ShadowQualityOption.High);
                data.textureQuality = PlayerPrefs.GetInt("Abyssal_Graphics_Texture", (int)TextureQualityOption.Full);
                data.antiAliasing = PlayerPrefs.GetInt("Abyssal_Graphics_AA", (int)AntiAliasingOption.MSAA2x_SMAA);
                data.ambientOcclusion = PlayerPrefs.GetInt("Abyssal_Graphics_AO", (int)AmbientOcclusionOption.HalfRes);
                data.bloomQuality = PlayerPrefs.GetInt("Abyssal_Graphics_Bloom", (int)BloomQualityOption.Medium);
                data.vfxQuality = PlayerPrefs.GetInt("Abyssal_Graphics_VFX", (int)VfxQualityOption.High);
                data.viewDistance = PlayerPrefs.GetInt("Abyssal_Graphics_ViewDist", (int)ViewDistanceOption.Far);
                data.effectsDensity = PlayerPrefs.GetFloat("Abyssal_Graphics_Density", 1.0f);
                data.adaptiveResolutionEnabled = PlayerPrefs.GetInt("Abyssal_Graphics_AdaptiveRes", 1) == 1;
                data.adaptiveQualityDowngradeEnabled = PlayerPrefs.GetInt("Abyssal_Graphics_AutoDowngrade", 0) == 1;
                data.fullscreen = PlayerPrefs.GetInt("Abyssal_Graphics_Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
                MigrateAndValidate(data);
                return true;
            }

            return false;
        }

        public static UserGraphicsSettingsData CreateDefaultSettings()
        {
            return new UserGraphicsSettingsData
            {
                schemaVersion = UserGraphicsSettingsData.CurrentSchemaVersion,
                resolutionWidth = Mathf.Max(1280, Screen.currentResolution.width > 0 ? Screen.currentResolution.width : Screen.width),
                resolutionHeight = Mathf.Max(720, Screen.currentResolution.height > 0 ? Screen.currentResolution.height : Screen.height),
                refreshRate = Mathf.Max(60, Screen.currentResolution.refreshRate),
                displayMode = Screen.fullScreen ? (int)DisplayModeOption.Fullscreen : (int)DisplayModeOption.Windowed,
                fullscreen = Screen.fullScreen,
                targetFrameRate = 60,
                vSyncEnabled = true
            };
        }

        private static void MigrateAndValidate(UserGraphicsSettingsData d)
        {
            if (d == null) return;
            if (d.schemaVersion < 2)
            {
                // Migrate Schema v1 -> v2
                d.displayMode = d.fullscreen ? (int)DisplayModeOption.Fullscreen : (int)DisplayModeOption.Windowed;
                if (d.resolutionWidth < 640) d.resolutionWidth = Mathf.Max(1280, Screen.width);
                if (d.resolutionHeight < 360) d.resolutionHeight = Mathf.Max(720, Screen.height);
                if (d.renderScale < 0.5f || d.renderScale > 1.5f) d.renderScale = 1.0f;
                if (d.effectsDensity < 0.2f || d.effectsDensity > 2.0f) d.effectsDensity = 1.0f;
                d.schemaVersion = 2;
            }

            d.selectedTier = Mathf.Clamp(d.selectedTier, 0, 4);
            d.resolutionWidth = Mathf.Clamp(d.resolutionWidth, 640, 7680);
            d.resolutionHeight = Mathf.Clamp(d.resolutionHeight, 360, 4320);
            d.displayMode = Mathf.Clamp(d.displayMode, 0, 2);
            d.fullscreen = ((DisplayModeOption)d.displayMode) != DisplayModeOption.Windowed;
            d.renderScale = Mathf.Clamp(d.renderScale, 0.50f, 1.50f);
            d.shadowQuality = Mathf.Clamp(d.shadowQuality, 0, 4);
            d.textureQuality = Mathf.Clamp(d.textureQuality, 0, 2);
            d.antiAliasing = Mathf.Clamp(d.antiAliasing, 0, 4);
            d.ambientOcclusion = Mathf.Clamp(d.ambientOcclusion, 0, 2);
            d.bloomQuality = Mathf.Clamp(d.bloomQuality, 0, 3);
            d.vfxQuality = Mathf.Clamp(d.vfxQuality, 0, 3);
            d.viewDistance = Mathf.Clamp(d.viewDistance, 0, 3);
            d.effectsDensity = Mathf.Clamp(d.effectsDensity, 0.25f, 1.50f);
        }
    }
}


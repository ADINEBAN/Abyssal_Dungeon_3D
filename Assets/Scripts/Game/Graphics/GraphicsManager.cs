using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace AbyssGame
{
    /// <summary>
    /// Central Scalable Graphics Quality Manager for Abyssal Dungeon 3D (Unity 2021.3.11f1 + URP 12.1.7).
    /// Orchestrates GraphicsPreset SOs, GraphicsDetector, GraphicsSettingsPersistence,
    /// PerformanceMonitor, AdaptiveQualityController, and VfxQualitySystem.
    /// Strictly local to each player machine — never synchronized over Multiplayer.
    /// </summary>
    public class GraphicsManager : MonoBehaviour
    {
        private static GraphicsManager _instance;
        public static GraphicsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GraphicsManager>();
                    if (_instance == null && Application.isPlaying)
                    {
                        var go = new GameObject("[GraphicsManager]");
                        DontDestroyOnLoad(go);
                        _instance = go.AddComponent<GraphicsManager>();
                        _instance.InitializeIfNeeded();
                    }
                }
                return _instance;
            }
        }

        public static GraphicsManager I => Instance;

        public event Action<GraphicsQualityTier, GraphicsPreset> OnQualityPresetChanged;

        public GraphicsQualityTier CurrentTier { get; private set; } = GraphicsQualityTier.Medium;
        public GraphicsPreset ActivePreset { get; private set; }
        public HardwareDetectionReport HardwareReport { get; private set; }
        public PerformanceMonitor PerfMonitor { get; private set; }
        public AdaptiveQualityController AdaptiveController { get; private set; }
        public GraphicsAutoDetect AutoDetector { get; private set; }
        public UserGraphicsSettingsData SettingsData { get; private set; } = new UserGraphicsSettingsData();

        public bool HasUserManualSelection => SettingsData != null && SettingsData.hasUserManualSelection;
        public bool AdaptiveResolutionEnabled => SettingsData != null && SettingsData.adaptiveResolutionEnabled;
        public bool AdaptiveQualityDowngradeEnabled => SettingsData != null && SettingsData.adaptiveQualityDowngradeEnabled;
        public bool VSyncEnabled => SettingsData != null && SettingsData.vSyncEnabled;

        private float _runtimeVfxDensityOverride = -1f;

        /// <summary>
        /// Local cosmetic particle multiplier (0.35x on Low .. 1.50x on Cinematic).
        /// Never affects hitboxes, cooldowns, AI, or multiplayer synchronization.
        /// </summary>
        public static float ParticleDensityMultiplier
        {
            get
            {
                if (_instance == null) return 1.0f;
                if (_instance._runtimeVfxDensityOverride > 0f) return _instance._runtimeVfxDensityOverride;
                if (_instance.SettingsData != null && _instance.SettingsData.hasCustomSubOptionOverrides)
                    return _instance.SettingsData.effectsDensity;
                return _instance.ActivePreset != null ? _instance.ActivePreset.particleDensityMultiplier : 1.0f;
            }
        }

        public static float CurrentViewDistanceMeters
        {
            get
            {
                if (_instance == null || _instance.ActivePreset == null) return 72f;
                return _instance.ActivePreset.viewDistanceCullMeters;
            }
        }

        private readonly Dictionary<GraphicsQualityTier, GraphicsPreset> _presets = new Dictionary<GraphicsQualityTier, GraphicsPreset>();
        private bool _initialized;

        // Cached Reflection Fields for URP 12.1.7 UniversalRenderPipelineAsset internal properties
        private static FieldInfo _fiMainShadowsSupported;
        private static FieldInfo _fiMainShadowResolution;
        private static FieldInfo _fiSoftShadowsSupported;
        private static FieldInfo _fiAddLightsRenderingMode;
        private static FieldInfo _fiAddLightShadowsSupported;
        private static FieldInfo _fiAddLightShadowResolution;
        private static FieldInfo _fiRendererDataList;
        private static FieldInfo _fiRendererFeatures;
        private static bool _reflectionCached;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoBootstrapGraphics()
        {
            _ = Instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);

            InitializeIfNeeded();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RestorePresetBaseUrpValues();
        }

        private void OnApplicationQuit()
        {
            RestorePresetBaseUrpValues();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyActivePresetToCurrentScene();
        }

        public void InitializeIfNeeded()
        {
            if (_initialized) return;
            _initialized = true;

            CacheUrpReflectionFields();
            LoadAllPresets();

            HardwareReport = GraphicsDetector.AnalyzeHardware();

            PerfMonitor = GetComponent<PerformanceMonitor>();
            if (PerfMonitor == null)
                PerfMonitor = gameObject.AddComponent<PerformanceMonitor>();

            AdaptiveController = GetComponent<AdaptiveQualityController>();
            if (AdaptiveController == null)
                AdaptiveController = gameObject.AddComponent<AdaptiveQualityController>();

            AutoDetector = GetComponent<GraphicsAutoDetect>();
            if (AutoDetector == null)
                AutoDetector = gameObject.AddComponent<GraphicsAutoDetect>();

            bool loadedSaved = GraphicsSettingsPersistence.Load(out UserGraphicsSettingsData loaded);
            SettingsData = loaded;

            bool gpuChanged = !string.IsNullOrEmpty(SettingsData.lastDetectedGpu) &&
                              !string.Equals(SettingsData.lastDetectedGpu, HardwareReport.gpuName, StringComparison.OrdinalIgnoreCase);

            SettingsData.lastDetectedGpu = HardwareReport.gpuName;

            if (!loadedSaved || !SettingsData.hasUserManualSelection || gpuChanged)
            {
                GraphicsQualityTier recommended = HardwareReport.staticRecommendedTier;
                var recPreset = GetPreset(recommended);
                SettingsData.selectedTier = (int)recommended;
                SettingsData.hasUserManualSelection = false;
                if (recPreset != null)
                {
                    SettingsData.SyncFromPreset(recPreset);
                    SettingsData.vSyncEnabled = recPreset.defaultVSyncCount > 0;
                    SettingsData.targetFrameRate = recPreset.targetFrameRate;
                }
                SetQualityTier(recommended, isUserManualSelection: false, saveImmediately: true);

                if (Application.isPlaying && AutoDetector != null)
                {
                    AutoDetector.StartRuntimeVerificationBenchmark(recommended, (verifiedTier, msg) =>
                    {
                        if (!SettingsData.hasUserManualSelection && verifiedTier != CurrentTier)
                        {
                            SetQualityTier(verifiedTier, isUserManualSelection: false, saveImmediately: true);
                        }
                    });
                }
            }
            else
            {
                GraphicsQualityTier savedTier = (GraphicsQualityTier)Mathf.Clamp(SettingsData.selectedTier, 0, 4);
                CurrentTier = savedTier;
                ActivePreset = GetPreset(savedTier);
                ApplyEffectiveConfiguration(saveImmediately: false);
            }

            Debug.Log($"[GraphicsManager] Initialized on '{HardwareReport.gpuName}' (Reported VRAM: {HardwareReport.reportedVramMB} MB, " +
                      $"Effective Budget: {HardwareReport.estimatedDedicatedVramMB} MB, SharedMem: {HardwareReport.isIntegratedOrSharedMemory}, " +
                      $"CPU: {HardwareReport.cpuCores} cores @ {HardwareReport.cpuFrequencyMHz} MHz). " +
                      $"Recommended: {HardwareReport.staticRecommendedTier} | Active Preset: {CurrentTier}.");
        }

        private void LoadAllPresets()
        {
            _presets.Clear();
            foreach (GraphicsQualityTier tier in Enum.GetValues(typeof(GraphicsQualityTier)))
            {
                string resPath = $"GraphicsPresets/GraphicsPreset_{tier}";
                GraphicsPreset asset = Resources.Load<GraphicsPreset>(resPath);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<GraphicsPreset>();
                    asset.name = $"GraphicsPreset_{tier}_Runtime";
                    asset.ApplyDefaultValuesForTier(tier);
                }
                _presets[tier] = asset;
            }
        }

        public GraphicsPreset GetPreset(GraphicsQualityTier tier)
        {
            if (_presets.Count == 0) LoadAllPresets();
            if (_presets.TryGetValue(tier, out GraphicsPreset p) && p != null)
                return p;
            return null;
        }

        public IReadOnlyDictionary<GraphicsQualityTier, GraphicsPreset> GetAllPresets()
        {
            if (_presets.Count == 0) LoadAllPresets();
            return _presets;
        }

        /// <summary>
        /// Switches to the specified quality preset, populates granular sub-options from that preset,
        /// and applies all URP, QualitySettings, Camera, Post-Processing, Light, and VFX settings.
        /// </summary>
        public void SetQualityTier(GraphicsQualityTier tier, bool isUserManualSelection = true, bool saveImmediately = true)
        {
            InitializeIfNeeded();
            RestorePresetBaseUrpValues();

            CurrentTier = tier;
            ActivePreset = GetPreset(tier);
            if (ActivePreset == null) return;

            SettingsData.selectedTier = (int)tier;
            SettingsData.hasUserManualSelection = isUserManualSelection;
            SettingsData.SyncFromPreset(ActivePreset);

            ApplyEffectiveConfiguration(saveImmediately);
        }

        /// <summary>
        /// Applies the current SettingsData (either pure preset or custom granular overrides) to URP, QualitySettings,
        /// Display/Resolution, VfxQualitySystem, and the active Scene.
        /// </summary>
        public void ApplyEffectiveConfiguration(bool saveImmediately = true)
        {
            InitializeIfNeeded();
            if (ActivePreset == null)
                ActivePreset = GetPreset(CurrentTier);
            if (ActivePreset == null) return;

            _runtimeVfxDensityOverride = -1f;

            // 1. Switch Unity QualitySettings level safely
            string[] qNames = QualitySettings.names;
            int targetQualityIdx = FindMatchingQualityLevelIndex(CurrentTier, ActivePreset.qualitySettingsIndex, qNames);
            if (targetQualityIdx >= 0 && targetQualityIdx < qNames.Length && QualitySettings.GetQualityLevel() != targetQualityIdx)
            {
                QualitySettings.SetQualityLevel(targetQualityIdx, applyExpensiveChanges: Application.isPlaying);
            }

            // 2. Compute effective granular parameters (allowing user sub-option overrides over the base preset)
            bool custom = SettingsData != null && SettingsData.hasCustomSubOptionOverrides;
            float effRenderScale = custom ? Mathf.Clamp(SettingsData.renderScale, 0.50f, 1.50f) : ActivePreset.renderScale;

            ShadowQualityOption shOpt = custom ? (ShadowQualityOption)SettingsData.shadowQuality : ActivePreset.shadowQualityOption;
            GetShadowParameters(shOpt, ActivePreset, out bool effMainShadows, out bool effSoftShadows, out int effShadowRes, out int effCascades, out float effShadowDist, out bool effAddShadows);

            TextureQualityOption texOpt = custom ? (TextureQualityOption)SettingsData.textureQuality : ActivePreset.textureQualityOption;
            int effMasterTexLimit = texOpt == TextureQualityOption.Full ? 0 : (texOpt == TextureQualityOption.Half ? 1 : 2);

            AntiAliasingOption aaOpt = custom ? (AntiAliasingOption)SettingsData.antiAliasing : ActivePreset.antiAliasingOption;
            GetAntiAliasingParameters(aaOpt, out int effMsaa, out AntialiasingMode effCamAa, out AntialiasingQuality effSmaaQual);

            AmbientOcclusionOption aoOpt = custom ? (AmbientOcclusionOption)SettingsData.ambientOcclusion : ActivePreset.ambientOcclusionOption;
            bool effSsao = aoOpt != AmbientOcclusionOption.Off;
            bool effSsaoDownsample = aoOpt == AmbientOcclusionOption.HalfRes;

            BloomQualityOption bloomOpt = custom ? (BloomQualityOption)SettingsData.bloomQuality : ActivePreset.bloomQualityOption;
            bool effBloom = bloomOpt != BloomQualityOption.Off;
            bool effBloomHq = bloomOpt == BloomQualityOption.High;
            float effBloomInt = bloomOpt == BloomQualityOption.Low ? 0.42f : (bloomOpt == BloomQualityOption.Medium ? 0.55f : 0.62f);

            VfxQualityOption vfxOpt = custom ? (VfxQualityOption)SettingsData.vfxQuality : ActivePreset.vfxQualityOption;
            float effDensity = custom ? Mathf.Clamp(SettingsData.effectsDensity, 0.25f, 1.50f) : ActivePreset.particleDensityMultiplier;

            ViewDistanceOption viewOpt = custom ? (ViewDistanceOption)SettingsData.viewDistance : ActivePreset.viewDistanceOption;
            GetViewDistanceParameters(viewOpt, ActivePreset, out float effLodBias, out int effMaxLod, out float effCullDist);

            // 3. Bind & configure URP Pipeline Asset
            UniversalRenderPipelineAsset urpAsset = ActivePreset.urpPipelineAsset;
            if (urpAsset == null)
            {
                urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset
                           ?? GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            }

            if (urpAsset != null)
            {
                QualitySettings.renderPipeline = urpAsset;
                GraphicsSettings.renderPipelineAsset = urpAsset;
                ApplyParametersToUrpAsset(
                    urpAsset,
                    effRenderScale,
                    ActivePreset.supportsHDR || effBloom,
                    effMsaa,
                    effMainShadows,
                    effSoftShadows,
                    effShadowRes,
                    effCascades,
                    effShadowDist,
                    ActivePreset.maxAdditionalLightsPerObject,
                    effAddShadows,
                    ActivePreset.additionalLightsShadowResolution,
                    effSsao);
            }

            // 4. Apply Unity QualitySettings overrides
            QualitySettings.shadows = effMainShadows
                ? (effSoftShadows ? UnityEngine.ShadowQuality.All : UnityEngine.ShadowQuality.HardOnly)
                : UnityEngine.ShadowQuality.Disable;
            QualitySettings.shadowDistance = effShadowDist;
            QualitySettings.shadowCascades = effCascades;
            QualitySettings.lodBias = effLodBias;
            QualitySettings.maximumLODLevel = effMaxLod;
            if (Application.isPlaying && QualitySettings.masterTextureLimit != effMasterTexLimit)
            {
                QualitySettings.masterTextureLimit = effMasterTexLimit;
            }
            QualitySettings.anisotropicFiltering = ActivePreset.anisotropicFiltering;
            QualitySettings.skinWeights = ActivePreset.skinWeights;
            QualitySettings.particleRaycastBudget = ActivePreset.particleRaycastBudget;
            QualitySettings.realtimeReflectionProbes = ActivePreset.enableRealtimeReflectionProbes;

            // 5. Apply Display Mode, Resolution, VSync & FPS Limit
            ApplyDisplayAndFrameRateSettings();

            // 6. Apply Scalable VFX Quality System
            VfxQualitySystem.ApplyQuality(vfxOpt, effDensity);

            // 7. Reset Adaptive Quality Baseline & Apply Scene Settings
            if (AutoDetector != null)
                AutoDetector.NotifySceneOrPresetChanged(effRenderScale, effShadowDist, effDensity);

            ApplyActivePresetToCurrentScene(
                effCamAa,
                effSmaaQual,
                effMainShadows,
                effSoftShadows,
                effShadowRes,
                effAddShadows,
                ActivePreset.enablePostProcessing || effBloom,
                effBloom,
                effBloomHq,
                effBloomInt,
                effDensity,
                effCullDist);

            if (saveImmediately)
                SavePersistentSettings();

            OnQualityPresetChanged?.Invoke(CurrentTier, ActivePreset);
        }

        private void ApplyDisplayAndFrameRateSettings()
        {
            if (SettingsData == null) return;

            QualitySettings.vSyncCount = SettingsData.vSyncEnabled ? 1 : 0;
            Application.targetFrameRate = SettingsData.vSyncEnabled ? -1 : SettingsData.targetFrameRate;

            if (!Application.isEditor)
            {
                FullScreenMode fsMode = FullScreenMode.FullScreenWindow;
                switch ((DisplayModeOption)SettingsData.displayMode)
                {
                    case DisplayModeOption.Fullscreen:
                        fsMode = FullScreenMode.ExclusiveFullScreen;
                        break;
                    case DisplayModeOption.BorderlessWindowed:
                        fsMode = FullScreenMode.FullScreenWindow;
                        break;
                    case DisplayModeOption.Windowed:
                        fsMode = FullScreenMode.Windowed;
                        break;
                }
                int w = Mathf.Max(640, SettingsData.resolutionWidth);
                int h = Mathf.Max(360, SettingsData.resolutionHeight);
                Screen.SetResolution(w, h, fsMode);
            }
        }

        /// <summary>
        /// Re-runs hardware detection + live 2.5s frame-time verification and applies the recommended preset.
        /// </summary>
        public void ApplyAutoRecommendedPreset(Action<GraphicsQualityTier, string> onBenchmarkVerified = null)
        {
            InitializeIfNeeded();
            HardwareReport = GraphicsDetector.AnalyzeHardware();
            GraphicsQualityTier rec = HardwareReport.staticRecommendedTier;

            SetQualityTier(rec, isUserManualSelection: false, saveImmediately: true);

            if (Application.isPlaying && AutoDetector != null)
            {
                AutoDetector.StartRuntimeVerificationBenchmark(rec, (verifiedTier, msg) =>
                {
                    if (verifiedTier != CurrentTier)
                        SetQualityTier(verifiedTier, isUserManualSelection: false, saveImmediately: true);
                    onBenchmarkVerified?.Invoke(verifiedTier, msg);
                });
            }
            else
            {
                onBenchmarkVerified?.Invoke(rec, HardwareReport.recommendationSummary);
            }
        }

        public void RestoreDefaultSettings()
        {
            InitializeIfNeeded();
            HardwareReport = GraphicsDetector.AnalyzeHardware();
            GraphicsQualityTier rec = HardwareReport.staticRecommendedTier;
            var preset = GetPreset(rec);
            if (preset != null)
            {
                SettingsData.SyncFromPreset(preset);
                SettingsData.vSyncEnabled = preset.defaultVSyncCount > 0;
                SettingsData.targetFrameRate = preset.targetFrameRate;
                SettingsData.adaptiveResolutionEnabled = true;
                SettingsData.adaptiveQualityDowngradeEnabled = false;
            }
            SetQualityTier(rec, isUserManualSelection: false, saveImmediately: true);
        }

        public void SetAdaptiveResolution(bool enabled)
        {
            InitializeIfNeeded();
            SettingsData.adaptiveResolutionEnabled = enabled;
            if (!enabled && ActivePreset != null)
            {
                float targetScale = SettingsData.hasCustomSubOptionOverrides ? SettingsData.renderScale : ActivePreset.renderScale;
                ApplyDynamicRenderScale(targetScale);
            }
            SavePersistentSettings();
        }

        public void SetAdaptiveQualityDowngrade(bool enabled)
        {
            InitializeIfNeeded();
            SettingsData.adaptiveQualityDowngradeEnabled = enabled;
            SavePersistentSettings();
        }

        public void SetVSync(bool enabled)
        {
            InitializeIfNeeded();
            SettingsData.vSyncEnabled = enabled;
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            Application.targetFrameRate = enabled ? -1 : SettingsData.targetFrameRate;
            SavePersistentSettings();
        }

        public void SetFpsLimit(int fpsLimit)
        {
            InitializeIfNeeded();
            SettingsData.targetFrameRate = fpsLimit;
            if (!SettingsData.vSyncEnabled)
                Application.targetFrameRate = fpsLimit;
            SavePersistentSettings();
        }

        public void SetDisplayMode(DisplayModeOption mode)
        {
            InitializeIfNeeded();
            SettingsData.displayMode = (int)mode;
            SettingsData.fullscreen = mode != DisplayModeOption.Windowed;
            ApplyDisplayAndFrameRateSettings();
            SavePersistentSettings();
        }

        public void SetFullscreen(bool fullscreen)
        {
            SetDisplayMode(fullscreen ? DisplayModeOption.Fullscreen : DisplayModeOption.Windowed);
        }

        public void SetResolution(int width, int height, int refreshRate = 60)
        {
            InitializeIfNeeded();
            SettingsData.resolutionWidth = Mathf.Max(640, width);
            SettingsData.resolutionHeight = Mathf.Max(360, height);
            SettingsData.refreshRate = Mathf.Max(30, refreshRate);
            ApplyDisplayAndFrameRateSettings();
            SavePersistentSettings();
        }

        public Resolution[] GetAvailableResolutions()
        {
            var list = new List<Resolution>
            {
                new Resolution { width = 1280, height = 720, refreshRate = 60 },
                new Resolution { width = 1366, height = 768, refreshRate = 60 },
                new Resolution { width = 1600, height = 900, refreshRate = 60 },
                new Resolution { width = 1920, height = 1080, refreshRate = 60 },
                new Resolution { width = 2560, height = 1440, refreshRate = 60 },
                new Resolution { width = 3840, height = 2160, refreshRate = 60 }
            };

            Resolution[] sys = Screen.resolutions;
            if (sys != null)
            {
                for (int i = 0; i < sys.Length; i++)
                {
                    Resolution r = sys[i];
                    if (r.width < 1024 || r.height < 576) continue;
                    bool exists = false;
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (list[j].width == r.width && list[j].height == r.height)
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                        list.Add(new Resolution { width = r.width, height = r.height, refreshRate = Mathf.Max(60, r.refreshRate) });
                }
            }

            list.Sort((a, b) => (a.width * a.height).CompareTo(b.width * b.height));
            return list.ToArray();
        }

        public int GetCurrentResolutionIndex()
        {
            Resolution[] list = GetAvailableResolutions();
            int w = SettingsData != null ? SettingsData.resolutionWidth : Screen.width;
            int h = SettingsData != null ? SettingsData.resolutionHeight : Screen.height;
            int bestIdx = 0;
            int bestDiff = int.MaxValue;
            for (int i = 0; i < list.Length; i++)
            {
                int diff = Mathf.Abs(list[i].width - w) + Mathf.Abs(list[i].height - h);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestIdx = i;
                }
            }
            return bestIdx;
        }

        public void SetRenderScale(float scale, bool saveImmediately = true)
        {
            InitializeIfNeeded();
            SettingsData.hasUserManualSelection = true;
            SettingsData.hasCustomSubOptionOverrides = true;
            SettingsData.renderScale = Mathf.Clamp(scale, 0.50f, 1.25f);
            ApplyEffectiveConfiguration(saveImmediately);
        }

        public void SetShadowQuality(ShadowQualityOption opt, bool saveImmediately = true)
        {
            InitializeIfNeeded();
            SettingsData.hasUserManualSelection = true;
            SettingsData.hasCustomSubOptionOverrides = true;
            SettingsData.shadowQuality = (int)opt;
            ApplyEffectiveConfiguration(saveImmediately);
        }

        public void SetTextureQuality(TextureQualityOption opt, bool saveImmediately = true)
        {
            InitializeIfNeeded();
            SettingsData.hasUserManualSelection = true;
            SettingsData.hasCustomSubOptionOverrides = true;
            SettingsData.textureQuality = (int)opt;
            ApplyEffectiveConfiguration(saveImmediately);
        }

        public void SetAntiAliasing(AntiAliasingOption opt, bool saveImmediately = true)
        {
            InitializeIfNeeded();
            SettingsData.hasUserManualSelection = true;
            SettingsData.hasCustomSubOptionOverrides = true;
            SettingsData.antiAliasing = (int)opt;
            ApplyEffectiveConfiguration(saveImmediately);
        }

        public void SetAmbientOcclusion(AmbientOcclusionOption opt, bool saveImmediately = true)
        {
            InitializeIfNeeded();
            SettingsData.hasUserManualSelection = true;
            SettingsData.hasCustomSubOptionOverrides = true;
            SettingsData.ambientOcclusion = (int)opt;
            ApplyEffectiveConfiguration(saveImmediately);
        }

        public void SetBloom(BloomQualityOption opt, bool saveImmediately = true)
        {
            InitializeIfNeeded();
            SettingsData.hasUserManualSelection = true;
            SettingsData.hasCustomSubOptionOverrides = true;
            SettingsData.bloomQuality = (int)opt;
            ApplyEffectiveConfiguration(saveImmediately);
        }

        public void SetVfxQuality(VfxQualityOption opt, bool saveImmediately = true)
        {
            InitializeIfNeeded();
            SettingsData.hasUserManualSelection = true;
            SettingsData.hasCustomSubOptionOverrides = true;
            SettingsData.vfxQuality = (int)opt;
            ApplyEffectiveConfiguration(saveImmediately);
        }

        public void SetViewDistance(ViewDistanceOption opt, bool saveImmediately = true)
        {
            InitializeIfNeeded();
            SettingsData.hasUserManualSelection = true;
            SettingsData.hasCustomSubOptionOverrides = true;
            SettingsData.viewDistance = (int)opt;
            ApplyEffectiveConfiguration(saveImmediately);
        }

        public void SetEffectsDensity(float density, bool saveImmediately = true)
        {
            InitializeIfNeeded();
            SettingsData.hasUserManualSelection = true;
            SettingsData.hasCustomSubOptionOverrides = true;
            SettingsData.effectsDensity = Mathf.Clamp(density, 0.35f, 1.35f);
            ApplyEffectiveConfiguration(saveImmediately);
        }

        public void ApplyAndSave()
        {
            ApplyEffectiveConfiguration(saveImmediately: true);
        }

        public void ApplyDynamicRenderScale(float scale)
        {
            var urp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset
                      ?? GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                urp.renderScale = Mathf.Clamp(scale, 0.50f, 1.50f);
            }
        }

        public void ApplyDynamicShadowDistance(float shadowDist)
        {
            float clamped = Mathf.Clamp(shadowDist, 15f, 150f);
            QualitySettings.shadowDistance = clamped;
            var urp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset
                      ?? GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                urp.shadowDistance = clamped;
            }
        }

        public void ApplyDynamicVfxDensity(float density)
        {
            _runtimeVfxDensityOverride = Mathf.Clamp(density, 0.25f, 1.50f);
            VfxQualityOption opt = SettingsData != null && SettingsData.hasCustomSubOptionOverrides
                ? (VfxQualityOption)SettingsData.vfxQuality
                : (ActivePreset != null ? ActivePreset.vfxQualityOption : VfxQualityOption.High);
            VfxQualitySystem.ApplyQuality(opt, _runtimeVfxDensityOverride);
        }

        private void RestorePresetBaseUrpValues()
        {
            if (ActivePreset != null && ActivePreset.urpPipelineAsset != null)
            {
                ActivePreset.urpPipelineAsset.renderScale = ActivePreset.renderScale;
                ActivePreset.urpPipelineAsset.shadowDistance = ActivePreset.shadowDistance;
            }
        }

        public void ApplyActivePresetToCurrentScene()
        {
            if (ActivePreset == null) return;
            bool custom = SettingsData != null && SettingsData.hasCustomSubOptionOverrides;

            ShadowQualityOption shOpt = custom ? (ShadowQualityOption)SettingsData.shadowQuality : ActivePreset.shadowQualityOption;
            GetShadowParameters(shOpt, ActivePreset, out bool effMainShadows, out bool effSoftShadows, out int effShadowRes, out _, out _, out bool effAddShadows);

            AntiAliasingOption aaOpt = custom ? (AntiAliasingOption)SettingsData.antiAliasing : ActivePreset.antiAliasingOption;
            GetAntiAliasingParameters(aaOpt, out _, out AntialiasingMode effCamAa, out AntialiasingQuality effSmaaQual);

            BloomQualityOption bloomOpt = custom ? (BloomQualityOption)SettingsData.bloomQuality : ActivePreset.bloomQualityOption;
            bool effBloom = bloomOpt != BloomQualityOption.Off;
            bool effBloomHq = bloomOpt == BloomQualityOption.High;
            float effBloomInt = bloomOpt == BloomQualityOption.Low ? 0.42f : (bloomOpt == BloomQualityOption.Medium ? 0.55f : 0.62f);

            float effDensity = ParticleDensityMultiplier;
            ViewDistanceOption viewOpt = custom ? (ViewDistanceOption)SettingsData.viewDistance : ActivePreset.viewDistanceOption;
            GetViewDistanceParameters(viewOpt, ActivePreset, out _, out _, out float effCullDist);

            ApplyActivePresetToCurrentScene(
                effCamAa,
                effSmaaQual,
                effMainShadows,
                effSoftShadows,
                effShadowRes,
                effAddShadows,
                ActivePreset.enablePostProcessing || effBloom,
                effBloom,
                effBloomHq,
                effBloomInt,
                effDensity,
                effCullDist);
        }

        private void ApplyActivePresetToCurrentScene(
            AntialiasingMode camAa,
            AntialiasingQuality smaaQual,
            bool enableMainShadows,
            bool softShadows,
            int mainShadowRes,
            bool enableAddShadows,
            bool enablePostProc,
            bool enableBloom,
            bool bloomHq,
            float bloomInt,
            float particleDensity,
            float cullDistMeters)
        {
            if (ActivePreset == null) return;

            // 1. Camera Setup (UniversalAdditionalCameraData + Far Clip / Layer Cull Distances)
            if (Camera.main != null)
            {
                Camera cam = Camera.main;
                cam.allowHDR = ActivePreset.supportsHDR || enableBloom;
                cam.allowMSAA = ActivePreset.msaaSampleCount > 1;

                var camData = cam.GetComponent<UniversalAdditionalCameraData>();
                if (camData == null)
                    camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();

                camData.renderPostProcessing = enablePostProc;
                camData.renderShadows = enableMainShadows;
                camData.antialiasing = camAa;
                camData.antialiasingQuality = smaaQual;
            }

            // 2. Global Post-Processing Volume Setup
            var volume = FindObjectOfType<Volume>();
            if (volume != null && volume.isGlobal && volume.sharedProfile != null)
            {
                volume.enabled = enablePostProc;
                VolumeProfile prof = volume.sharedProfile;

                if (prof.TryGet(out Tonemapping tone))
                    tone.active = ActivePreset.enableTonemapping;

                if (prof.TryGet(out Bloom bloom))
                {
                    bloom.active = enableBloom;
                    bloom.intensity.Override(bloomInt);
                    bloom.highQualityFiltering.Override(bloomHq);
                }

                if (prof.TryGet(out Vignette vig))
                    vig.active = ActivePreset.enableVignette;

                if (prof.TryGet(out ShadowsMidtonesHighlights smh))
                    smh.active = ActivePreset.enableSplitToning;
            }

            // 3. Scene Lights (Main Directional Light + Point Light Budget Culling)
            Light[] allLights = FindObjectsOfType<Light>(true);
            Vector3 camPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            var pointLights = new List<Light>();

            foreach (var l in allLights)
            {
                if (l == null) continue;
                if (l.type == LightType.Directional)
                {
                    if (!enableMainShadows)
                    {
                        l.shadows = LightShadows.None;
                    }
                    else
                    {
                        l.shadows = softShadows ? LightShadows.Soft : LightShadows.Hard;
                        l.shadowResolution = MapShadowResolutionEnum(mainShadowRes);
                    }
                }
                else if (l.type == LightType.Point || l.type == LightType.Spot)
                {
                    pointLights.Add(l);
                }
            }

            pointLights.Sort((a, b) =>
            {
                float da = (a.transform.position - camPos).sqrMagnitude;
                float db = (b.transform.position - camPos).sqrMagnitude;
                return da.CompareTo(db);
            });

            int maxPointLights = ActivePreset.maxActiveScenePointLights;
            float maxLightDistSq = cullDistMeters * cullDistMeters;
            for (int i = 0; i < pointLights.Count; i++)
            {
                Light pl = pointLights[i];
                float distSq = (pl.transform.position - camPos).sqrMagnitude;
                bool shouldEnable = i < maxPointLights && distSq <= maxLightDistSq;
                pl.enabled = shouldEnable;
                if (shouldEnable)
                {
                    pl.shadows = enableAddShadows ? LightShadows.Soft : LightShadows.None;
                }
            }

            foreach (var tf in FindObjectsOfType<TorchFlickerBehaviour>(true))
            {
                if (tf != null)
                    tf.enabled = ActivePreset.enableTorchFlickerMotion && (tf.TargetLight == null || tf.TargetLight.enabled);
            }

            // 4. Environmental Particles & Ground Mist Planes
            foreach (var ps in FindObjectsOfType<ParticleSystem>(true))
            {
                if (ps == null) continue;
                VfxQualitySystem.RegisterParticleSystem(ps);
                if (ps.gameObject.name == "AmbientDustEmberParticles")
                {
                    ps.gameObject.SetActive(ActivePreset.enableAmbientDustParticles);
                    if (ActivePreset.enableAmbientDustParticles)
                    {
                        var main = ps.main;
                        main.maxParticles = Mathf.RoundToInt(140 * particleDensity);
                        var em = ps.emission;
                        em.rateOverTime = 24f * particleDensity;
                    }
                }
            }

            foreach (var mr in FindObjectsOfType<MeshRenderer>(true))
            {
                if (mr != null && mr.gameObject.name == "GroundMistPlane")
                {
                    mr.gameObject.SetActive(ActivePreset.enableGroundMist);
                }
            }

            // 5. Ensure EnvironmentDistanceCuller is active in the scene (Phase 4 optimization)
            EnvironmentDistanceCuller.EnsureOnActiveScene(cullDistMeters, ActivePreset.distantAnimatorThrottleMeters, maxPointLights);
        }

        private static void GetShadowParameters(
            ShadowQualityOption opt,
            GraphicsPreset basePreset,
            out bool enableMain,
            out bool soft,
            out int mainRes,
            out int cascades,
            out float dist,
            out bool enableAdd)
        {
            switch (opt)
            {
                case ShadowQualityOption.Off:
                    enableMain = false; soft = false; mainRes = 512; cascades = 1; dist = 15f; enableAdd = false;
                    break;
                case ShadowQualityOption.Low:
                    enableMain = true; soft = false; mainRes = 1024; cascades = 1; dist = 25f; enableAdd = false;
                    break;
                case ShadowQualityOption.Medium:
                    enableMain = true; soft = true; mainRes = 1024; cascades = 2; dist = 35f; enableAdd = false;
                    break;
                case ShadowQualityOption.High:
                    enableMain = true; soft = true; mainRes = 2048; cascades = 2; dist = 50f; enableAdd = false;
                    break;
                default: // Ultra
                    enableMain = true; soft = true; mainRes = 4096; cascades = 4;
                    dist = basePreset != null && basePreset.tier == GraphicsQualityTier.Cinematic ? 120f : 80f;
                    enableAdd = true;
                    break;
            }
        }

        private static void GetAntiAliasingParameters(
            AntiAliasingOption opt,
            out int msaa,
            out AntialiasingMode camAa,
            out AntialiasingQuality smaaQual)
        {
            switch (opt)
            {
                case AntiAliasingOption.Off:
                    msaa = 1; camAa = AntialiasingMode.None; smaaQual = AntialiasingQuality.Low;
                    break;
                case AntiAliasingOption.FXAA:
                    msaa = 1; camAa = AntialiasingMode.FastApproximateAntialiasing; smaaQual = AntialiasingQuality.Low;
                    break;
                case AntiAliasingOption.SMAA:
                    msaa = 1; camAa = AntialiasingMode.SubpixelMorphologicalAntiAliasing; smaaQual = AntialiasingQuality.Medium;
                    break;
                case AntiAliasingOption.MSAA2x_SMAA:
                    msaa = 2; camAa = AntialiasingMode.SubpixelMorphologicalAntiAliasing; smaaQual = AntialiasingQuality.Medium;
                    break;
                default: // MSAA4x_SMAA
                    msaa = 4; camAa = AntialiasingMode.SubpixelMorphologicalAntiAliasing; smaaQual = AntialiasingQuality.High;
                    break;
            }
        }

        private static void GetViewDistanceParameters(
            ViewDistanceOption opt,
            GraphicsPreset basePreset,
            out float lodBias,
            out int maxLod,
            out float cullDist)
        {
            switch (opt)
            {
                case ViewDistanceOption.Near:
                    lodBias = 0.60f; maxLod = 1; cullDist = 38f;
                    break;
                case ViewDistanceOption.Medium:
                    lodBias = 0.85f; maxLod = 0; cullDist = 52f;
                    break;
                case ViewDistanceOption.Far:
                    lodBias = 1.00f; maxLod = 0; cullDist = 72f;
                    break;
                default: // Ultra
                    lodBias = basePreset != null && basePreset.tier == GraphicsQualityTier.Cinematic ? 2.00f : 1.50f;
                    maxLod = 0;
                    cullDist = basePreset != null && basePreset.tier == GraphicsQualityTier.Cinematic ? 135f : 100f;
                    break;
            }
        }

        private static LightShadowResolution MapShadowResolutionEnum(int res)
        {
            if (res >= 4096) return LightShadowResolution.VeryHigh;
            if (res >= 2048) return LightShadowResolution.High;
            if (res >= 1024) return LightShadowResolution.Medium;
            return LightShadowResolution.Low;
        }

        private static int FindMatchingQualityLevelIndex(GraphicsQualityTier tier, int preferredIdx, string[] names)
        {
            if (names == null || names.Length == 0) return 0;

            string tierName = tier.ToString();
            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(names[i], tierName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            if (names.Length == 3)
            {
                switch (tier)
                {
                    case GraphicsQualityTier.Low: return 0;
                    case GraphicsQualityTier.Medium: return 1;
                    default: return 2;
                }
            }

            return Mathf.Clamp(preferredIdx, 0, names.Length - 1);
        }

        private static void CacheUrpReflectionFields()
        {
            if (_reflectionCached) return;
            _reflectionCached = true;

            Type urpType = typeof(UniversalRenderPipelineAsset);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            _fiMainShadowsSupported = urpType.GetField("m_MainLightShadowsSupported", flags);
            _fiMainShadowResolution = urpType.GetField("m_MainLightShadowmapResolution", flags);
            _fiSoftShadowsSupported = urpType.GetField("m_SoftShadowsSupported", flags);
            _fiAddLightsRenderingMode = urpType.GetField("m_AdditionalLightsRenderingMode", flags);
            _fiAddLightShadowsSupported = urpType.GetField("m_AdditionalLightShadowsSupported", flags);
            _fiAddLightShadowResolution = urpType.GetField("m_AdditionalLightsShadowmapResolution", flags);
            _fiRendererDataList = urpType.GetField("m_RendererDataList", flags);
            _fiRendererFeatures = typeof(ScriptableRendererData).GetField("m_RendererFeatures", flags);
        }

        private static void ApplyParametersToUrpAsset(
            UniversalRenderPipelineAsset urpAsset,
            float renderScale,
            bool supportsHdr,
            int msaa,
            bool mainShadows,
            bool softShadows,
            int mainShadowRes,
            int cascades,
            float shadowDist,
            int maxAddLights,
            bool addLightShadows,
            int addLightShadowRes,
            bool enableSsao)
        {
            if (urpAsset == null) return;
            CacheUrpReflectionFields();

            urpAsset.renderScale = Mathf.Clamp(renderScale, 0.50f, 1.50f);
            urpAsset.supportsHDR = supportsHdr;
            urpAsset.msaaSampleCount = Mathf.Clamp(msaa, 1, 8);
            urpAsset.shadowDistance = shadowDist;
            urpAsset.shadowCascadeCount = Mathf.Clamp(cascades, 1, 4);
            urpAsset.maxAdditionalLightsCount = Mathf.Clamp(maxAddLights, 0, 8);
            urpAsset.useSRPBatcher = true;

            try
            {
                if (_fiAddLightsRenderingMode != null)
                {
                    object modeEnum = Enum.ToObject(_fiAddLightsRenderingMode.FieldType, maxAddLights > 0 ? 1 : 0);
                    _fiAddLightsRenderingMode.SetValue(urpAsset, modeEnum);
                }
                _fiMainShadowsSupported?.SetValue(urpAsset, mainShadows);
                if (_fiMainShadowResolution != null)
                {
                    object resEnum = Enum.ToObject(_fiMainShadowResolution.FieldType, mainShadowRes);
                    _fiMainShadowResolution.SetValue(urpAsset, resEnum);
                }
                _fiSoftShadowsSupported?.SetValue(urpAsset, softShadows);
                _fiAddLightShadowsSupported?.SetValue(urpAsset, addLightShadows);
                if (_fiAddLightShadowResolution != null)
                {
                    object addResEnum = Enum.ToObject(_fiAddLightShadowResolution.FieldType, addLightShadowRes);
                    _fiAddLightShadowResolution.SetValue(urpAsset, addResEnum);
                }

                if (_fiRendererDataList != null && _fiRendererFeatures != null)
                {
                    var rendererList = _fiRendererDataList.GetValue(urpAsset) as ScriptableRendererData[];
                    if (rendererList != null)
                    {
                        foreach (var rData in rendererList)
                        {
                            if (rData == null) continue;
                            var features = _fiRendererFeatures.GetValue(rData) as List<ScriptableRendererFeature>;
                            if (features == null) continue;
                            foreach (var feat in features)
                            {
                                if (feat == null) continue;
                                if (feat.name.IndexOf("SSAO", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    feat.GetType().Name.IndexOf("AmbientOcclusion", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    feat.SetActive(enableSsao);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GraphicsManager] URP internal reflection sync notice: " + e.Message);
            }
        }

        public void SavePersistentSettings()
        {
            GraphicsSettingsPersistence.Save(SettingsData);
        }
    }
}

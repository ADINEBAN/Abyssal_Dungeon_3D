using System;
using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Hysteresis-stabilized Adaptive Quality Controller (Section 4 & Phase 6).
    /// Monitors sustained frame time via PerformanceMonitor and adjusts safe graphics parameters
    /// in strict priority order:
    ///   1. Render Scale (Dynamic URP internal resolution)
    ///   2. Shadow Distance (URP shadowmap cull radius)
    ///   3. VFX & Particle Density (local cosmetic effects multiplier)
    ///   4. Quality Tier Fallback (only when user enables auto-downgrade)
    /// Never modifies gameplay, combat timing, hitboxes, or network sync, and respects Cinematic locks.
    /// </summary>
    public class AdaptiveQualityController : MonoBehaviour
    {
        public static AdaptiveQualityController Instance { get; private set; }

        [Header("Frame-Time Thresholds & Hysteresis Deadband (60 FPS = 16.67 ms)")]
        [Tooltip("Sustained frame time above this threshold (ms) triggers a safe reduction step (~52 FPS).")]
        public float downscaleFrameTimeThresholdMs = 19.2f;

        [Tooltip("Sustained frame time below this threshold (ms) allows a gradual recovery step (~71 FPS).")]
        public float upscaleFrameTimeThresholdMs = 14.0f;

        [Tooltip("Sustained severe frame time above this threshold (ms) triggers quality tier downgrade if enabled (~38 FPS).")]
        public float tierDowngradeThresholdMs = 26.0f;

        [Header("Hysteresis Windows & Cooldowns")]
        public float downscaleWindowSeconds = 3.0f;
        public float upscaleWindowSeconds = 5.0f;
        public float tierDowngradeWindowSeconds = 6.5f;
        public float adjustmentCooldownSeconds = 3.5f;

        [Header("Step Sizes")]
        public float renderScaleStep = 0.05f;
        public float shadowDistanceStepFactor = 0.15f;
        public float vfxDensityStep = 0.15f;

        public float CurrentRenderScale { get; private set; } = 1.0f;
        public float CurrentShadowDistance { get; private set; } = 50f;
        public float CurrentVfxDensity { get; private set; } = 1.0f;
        public bool IsInImportantCinematic { get; set; } = false;
        public string StatusSummary { get; private set; } = "Adaptive Quality: Ổn định.";

        private int _warmupFramesRemaining = 35;
        private float _highFrameTimeAccum;
        private float _lowFrameTimeAccum;
        private float _severeLagAccum;
        private float _cooldownTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public void ResetBaselineFromPreset(float baseRenderScale, float baseShadowDist, float baseVfxDensity)
        {
            CurrentRenderScale = Mathf.Clamp(baseRenderScale, 0.5f, 1.5f);
            CurrentShadowDistance = Mathf.Clamp(baseShadowDist, 15f, 150f);
            CurrentVfxDensity = Mathf.Clamp(baseVfxDensity, 0.25f, 1.5f);
            _warmupFramesRemaining = 35;
            _highFrameTimeAccum = 0f;
            _lowFrameTimeAccum = 0f;
            _severeLagAccum = 0f;
            _cooldownTimer = adjustmentCooldownSeconds;
            StatusSummary = $"Adaptive Quality: Khởi tạo chuẩn ({Mathf.RoundToInt(CurrentRenderScale * 100)}% Scale, Bóng {CurrentShadowDistance:0}m, VFX {Mathf.RoundToInt(CurrentVfxDensity * 100)}%).";
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0.0001f || dt > 0.20f)
                return;

            if (_warmupFramesRemaining > 0)
            {
                _warmupFramesRemaining--;
                return;
            }

            var mgr = GraphicsManager.Instance;
            if (mgr == null || mgr.ActivePreset == null)
                return;

            // Respect Cinematic lock if player has not permitted quality changes during important cinematics
            if (IsInImportantCinematic && (mgr.SettingsData == null || !mgr.SettingsData.allowAdaptiveDuringCinematic))
                return;

            if (_cooldownTimer > 0f)
                _cooldownTimer -= dt;

            var perf = PerformanceMonitor.Instance;
            float frameMs = perf != null ? perf.EffectiveFrameTimeMs : (dt * 1000f);
            float fps = frameMs > 0.1f ? (1000f / frameMs) : 60f;

            if (mgr.AdaptiveResolutionEnabled)
            {
                EvaluateAdaptiveSteps(mgr, dt, frameMs, fps);
            }

            if (mgr.AdaptiveQualityDowngradeEnabled)
            {
                EvaluateTierFallback(mgr, dt, frameMs, fps);
            }
        }

        private void EvaluateAdaptiveSteps(GraphicsManager mgr, float dt, float frameMs, float fps)
        {
            GraphicsPreset preset = mgr.ActivePreset;
            float baseScale = mgr.SettingsData != null && mgr.SettingsData.hasCustomSubOptionOverrides
                ? mgr.SettingsData.renderScale
                : preset.renderScale;
            float minScale = Mathf.Clamp(preset.minAdaptiveRenderScale, 0.50f, baseScale);
            float maxScale = Mathf.Clamp(baseScale, minScale, 1.50f);

            float maxShadowDist = preset.shadowDistance;
            float minShadowDist = Mathf.Max(18f, maxShadowDist * 0.60f);

            float maxVfxDensity = mgr.SettingsData != null && mgr.SettingsData.hasCustomSubOptionOverrides
                ? mgr.SettingsData.effectsDensity
                : preset.particleDensityMultiplier;
            float minVfxDensity = Mathf.Max(0.35f, maxVfxDensity * 0.55f);

            if (frameMs > downscaleFrameTimeThresholdMs)
            {
                _highFrameTimeAccum += dt;
                _lowFrameTimeAccum = Mathf.Max(0f, _lowFrameTimeAccum - dt * 2f);

                if (_highFrameTimeAccum >= downscaleWindowSeconds && _cooldownTimer <= 0f)
                {
                    _highFrameTimeAccum = 0f;

                    // Priority 1: Reduce Render Scale first
                    if (CurrentRenderScale > minScale + 0.01f)
                    {
                        CurrentRenderScale = Mathf.Max(minScale, CurrentRenderScale - renderScaleStep);
                        mgr.ApplyDynamicRenderScale(CurrentRenderScale);
                        _cooldownTimer = adjustmentCooldownSeconds;
                        StatusSummary = $"Adaptive [Bước 1]: Hạ Render Scale xuống {Mathf.RoundToInt(CurrentRenderScale * 100)}% ({fps:0} FPS).";
                    }
                    // Priority 2: Reduce Shadow Distance second
                    else if (preset.enableMainLightShadows && CurrentShadowDistance > minShadowDist + 1f)
                    {
                        CurrentShadowDistance = Mathf.Max(minShadowDist, CurrentShadowDistance * (1f - shadowDistanceStepFactor));
                        mgr.ApplyDynamicShadowDistance(CurrentShadowDistance);
                        _cooldownTimer = adjustmentCooldownSeconds;
                        StatusSummary = $"Adaptive [Bước 2]: Thu hẹp Bóng đổ xuống {CurrentShadowDistance:0}m ({fps:0} FPS).";
                    }
                    // Priority 3: Reduce Cosmetic VFX Density third
                    else if (CurrentVfxDensity > minVfxDensity + 0.02f)
                    {
                        CurrentVfxDensity = Mathf.Max(minVfxDensity, CurrentVfxDensity - vfxDensityStep);
                        mgr.ApplyDynamicVfxDensity(CurrentVfxDensity);
                        _cooldownTimer = adjustmentCooldownSeconds;
                        StatusSummary = $"Adaptive [Bước 3]: Giảm mật độ hạt VFX xuống {Mathf.RoundToInt(CurrentVfxDensity * 100)}% ({fps:0} FPS).";
                    }
                }
            }
            else if (frameMs < upscaleFrameTimeThresholdMs)
            {
                _lowFrameTimeAccum += dt;
                _highFrameTimeAccum = Mathf.Max(0f, _highFrameTimeAccum - dt * 2f);

                if (_lowFrameTimeAccum >= upscaleWindowSeconds && _cooldownTimer <= 0f)
                {
                    _lowFrameTimeAccum = 0f;

                    // Recover in reverse order: VFX Density -> Shadow Distance -> Render Scale
                    if (CurrentVfxDensity < maxVfxDensity - 0.02f)
                    {
                        CurrentVfxDensity = Mathf.Min(maxVfxDensity, CurrentVfxDensity + vfxDensityStep);
                        mgr.ApplyDynamicVfxDensity(CurrentVfxDensity);
                        _cooldownTimer = adjustmentCooldownSeconds;
                        StatusSummary = $"Adaptive [Khôi phục]: Tăng mật độ VFX lên {Mathf.RoundToInt(CurrentVfxDensity * 100)}% ({fps:0} FPS).";
                    }
                    else if (preset.enableMainLightShadows && CurrentShadowDistance < maxShadowDist - 1f)
                    {
                        CurrentShadowDistance = Mathf.Min(maxShadowDist, CurrentShadowDistance / (1f - shadowDistanceStepFactor));
                        mgr.ApplyDynamicShadowDistance(CurrentShadowDistance);
                        _cooldownTimer = adjustmentCooldownSeconds;
                        StatusSummary = $"Adaptive [Khôi phục]: Mở rộng Bóng đổ lên {CurrentShadowDistance:0}m ({fps:0} FPS).";
                    }
                    else if (CurrentRenderScale < maxScale - 0.01f)
                    {
                        CurrentRenderScale = Mathf.Min(maxScale, CurrentRenderScale + renderScaleStep);
                        mgr.ApplyDynamicRenderScale(CurrentRenderScale);
                        _cooldownTimer = adjustmentCooldownSeconds;
                        StatusSummary = $"Adaptive [Khôi phục]: Nâng Render Scale lên {Mathf.RoundToInt(CurrentRenderScale * 100)}% ({fps:0} FPS).";
                    }
                }
            }
            else
            {
                // Inside hysteresis deadband (14.0 ms .. 19.2 ms): decay accumulators
                _highFrameTimeAccum = Mathf.Max(0f, _highFrameTimeAccum - dt * 1.5f);
                _lowFrameTimeAccum = Mathf.Max(0f, _lowFrameTimeAccum - dt * 1.5f);
            }
        }

        private void EvaluateTierFallback(GraphicsManager mgr, float dt, float frameMs, float fps)
        {
            GraphicsPreset preset = mgr.ActivePreset;
            bool atMinScale = !mgr.AdaptiveResolutionEnabled || (CurrentRenderScale <= preset.minAdaptiveRenderScale + 0.02f);

            if (atMinScale && frameMs > tierDowngradeThresholdMs && mgr.CurrentTier > GraphicsQualityTier.Low)
            {
                _severeLagAccum += dt;
                if (_severeLagAccum >= tierDowngradeWindowSeconds && _cooldownTimer <= 0f)
                {
                    _severeLagAccum = 0f;
                    GraphicsQualityTier lowerTier = (GraphicsQualityTier)((int)mgr.CurrentTier - 1);
                    StatusSummary = $"Cảnh báo khung hình thấp kéo dài ({fps:0} FPS) -> Tự động hạ xuống Preset {lowerTier}.";
                    mgr.SetQualityTier(lowerTier, isUserManualSelection: false, saveImmediately: true);
                }
            }
            else
            {
                _severeLagAccum = Mathf.Max(0f, _severeLagAccum - dt * 1.5f);
            }
        }
    }
}


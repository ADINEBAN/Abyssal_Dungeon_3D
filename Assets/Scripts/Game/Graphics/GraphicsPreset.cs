using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AbyssGame
{
    /// <summary>
    /// Five scalable graphics quality tiers for Abyssal Dungeon 3D (Unity 2021.3.11f1 / URP 12.1.7).
    /// </summary>
    public enum GraphicsQualityTier
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3,
        Cinematic = 4
    }

    public enum ShadowQualityOption
    {
        Off = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Ultra = 4
    }

    public enum TextureQualityOption
    {
        Quarter = 0, // masterTextureLimit = 2
        Half = 1,    // masterTextureLimit = 1
        Full = 2     // masterTextureLimit = 0
    }

    public enum AntiAliasingOption
    {
        Off = 0,
        FXAA = 1,
        SMAA = 2,
        MSAA2x_SMAA = 3,
        MSAA4x_SMAA = 4
    }

    public enum AmbientOcclusionOption
    {
        Off = 0,
        HalfRes = 1,
        FullRes = 2
    }

    public enum BloomQualityOption
    {
        Off = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    public enum VfxQualityOption
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3
    }

    public enum ViewDistanceOption
    {
        Near = 0,   // 38m cull, LOD bias 0.60
        Medium = 1, // 52m cull, LOD bias 0.85
        Far = 2,    // 72m cull, LOD bias 1.00
        Ultra = 3   // 110m cull, LOD bias 1.50+
    }

    public enum DisplayModeOption
    {
        Fullscreen = 0,
        BorderlessWindowed = 1,
        Windowed = 2
    }

    /// <summary>
    /// ScriptableObject defining all rendering, shadow, URP pipeline, LOD, texture, lighting,
    /// post-processing, view distance, and VFX parameters for a single quality preset.
    /// Verified against URP 12.1.7 API.
    /// </summary>
    [CreateAssetMenu(fileName = "GraphicsPreset_New", menuName = "Abyssal Dungeon/Graphics Quality Preset", order = 10)]
    public class GraphicsPreset : ScriptableObject
    {
        [Header("1. Preset Identity")]
        public GraphicsQualityTier tier = GraphicsQualityTier.High;
        public string presetName = "High";
        public string vietnameseName = "Cao (High)";
        [TextArea(2, 3)]
        public string description = "Balanced 1080p60 target for GTX 1650-class discrete GPUs.";
        public int qualitySettingsIndex = 2;

        [Header("2. URP Pipeline Asset")]
        public UniversalRenderPipelineAsset urpPipelineAsset;

        [Header("3. Resolution & Anti-Aliasing")]
        [Range(0.5f, 1.5f)]
        public float renderScale = 1.0f;
        [Range(0.5f, 1.0f)]
        public float minAdaptiveRenderScale = 0.75f;
        [Range(0.75f, 1.5f)]
        public float maxAdaptiveRenderScale = 1.0f;
        [Tooltip("URP MSAA sample count: 1 (Off), 2, 4, or 8")]
        public int msaaSampleCount = 2;
        public AntialiasingMode cameraAntialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        public AntialiasingQuality smaaQuality = AntialiasingQuality.Medium;
        public AntiAliasingOption antiAliasingOption = AntiAliasingOption.MSAA2x_SMAA;
        public bool supportsHDR = true;

        [Header("4. Shadows (URP 12.1.7)")]
        public ShadowQualityOption shadowQualityOption = ShadowQualityOption.High;
        public bool enableMainLightShadows = true;
        public bool softShadows = true;
        public int mainLightShadowResolution = 2048;
        [Range(1, 4)]
        public int shadowCascadeCount = 2;
        [Range(15f, 150f)]
        public float shadowDistance = 50f;
        public bool enableAdditionalLightShadows = false;
        public int additionalLightsShadowResolution = 1024;

        [Header("5. Dynamic Lighting")]
        [Range(0, 8)]
        public int maxAdditionalLightsPerObject = 6;
        [Range(0, 24)]
        public int maxActiveScenePointLights = 10;
        public bool enableRealtimeReflectionProbes = false;

        [Header("6. Geometry, LOD, View Distance & Textures")]
        public ViewDistanceOption viewDistanceOption = ViewDistanceOption.Far;
        [Range(25f, 160f)]
        public float viewDistanceCullMeters = 72f;
        [Range(18f, 100f)]
        public float distantAnimatorThrottleMeters = 32f;
        [Range(0.3f, 2.5f)]
        public float lodBias = 1.0f;
        [Range(0, 2)]
        public int maximumLODLevel = 0;
        public TextureQualityOption textureQualityOption = TextureQualityOption.Full;
        [Tooltip("0 = Full Res, 1 = Half Res, 2 = Quarter Res")]
        [Range(0, 2)]
        public int masterTextureLimit = 0;
        public AnisotropicFiltering anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        public SkinWeights skinWeights = SkinWeights.FourBones;

        [Header("7. Scalable VFX & Particles (Local Cosmetic Only)")]
        public VfxQualityOption vfxQualityOption = VfxQualityOption.High;
        [Range(0.2f, 2.0f)]
        public float particleDensityMultiplier = 1.0f;
        public int particleRaycastBudget = 256;
        public bool enableVfxPointLights = true;
        public bool enableVfxShockwaveRings = true;
        public bool enableVfxSecondarySparks = true;
        public bool enableGroundMist = true;
        public bool enableAmbientDustParticles = true;
        public bool enableTorchFlickerMotion = true;

        [Header("8. Post-Processing & Renderer Features")]
        public bool enablePostProcessing = true;
        public bool enableTonemapping = true;
        public BloomQualityOption bloomQualityOption = BloomQualityOption.Medium;
        public bool enableBloom = true;
        public bool bloomHighQualityFiltering = false;
        [Range(0f, 1.5f)]
        public float bloomIntensity = 0.55f;
        public bool enableVignette = true;
        public bool enableSplitToning = true;
        public AmbientOcclusionOption ambientOcclusionOption = AmbientOcclusionOption.HalfRes;
        public bool enableSSAO = true;
        public bool ssaoDownsample = true;
        [Range(0.1f, 1.5f)]
        public float ssaoIntensity = 0.45f;
        [Range(4, 12)]
        public int ssaoSampleCount = 4;

        [Header("9. Frame Rate & Sync")]
        [Range(0, 2)]
        public int defaultVSyncCount = 1;
        public int targetFrameRate = 60;

        /// <summary>
        /// Populates default engineered values for each of the 5 tiers.
        /// </summary>
        public void ApplyDefaultValuesForTier(GraphicsQualityTier targetTier)
        {
            tier = targetTier;
            qualitySettingsIndex = (int)targetTier;

            switch (targetTier)
            {
                case GraphicsQualityTier.Low:
                    presetName = "Low";
                    vietnameseName = "Thấp (Low - iGPU)";
                    description = "Ưu tiên hiệu năng cho GPU tích hợp (Intel UHD / Iris Xe / Vega APU). Render Scale 80%, bóng 1024px (25m), tắt AO & Bloom, giảm overdraw.";
                    renderScale = 0.80f;
                    minAdaptiveRenderScale = 0.70f;
                    maxAdaptiveRenderScale = 0.85f;
                    msaaSampleCount = 1;
                    cameraAntialiasing = AntialiasingMode.None;
                    smaaQuality = AntialiasingQuality.Low;
                    antiAliasingOption = AntiAliasingOption.Off;
                    supportsHDR = false;

                    shadowQualityOption = ShadowQualityOption.Low;
                    enableMainLightShadows = true;
                    softShadows = false;
                    mainLightShadowResolution = 1024;
                    shadowCascadeCount = 1;
                    shadowDistance = 25f;
                    enableAdditionalLightShadows = false;
                    additionalLightsShadowResolution = 512;

                    maxAdditionalLightsPerObject = 2;
                    maxActiveScenePointLights = 3;
                    enableRealtimeReflectionProbes = false;

                    viewDistanceOption = ViewDistanceOption.Near;
                    viewDistanceCullMeters = 38f;
                    distantAnimatorThrottleMeters = 20f;
                    lodBias = 0.60f;
                    maximumLODLevel = 1;
                    textureQualityOption = TextureQualityOption.Half;
                    masterTextureLimit = 1;
                    anisotropicFiltering = AnisotropicFiltering.Disable;
                    skinWeights = SkinWeights.TwoBones;

                    vfxQualityOption = VfxQualityOption.Low;
                    particleDensityMultiplier = 0.35f;
                    particleRaycastBudget = 16;
                    enableVfxPointLights = false;
                    enableVfxShockwaveRings = false;
                    enableVfxSecondarySparks = false;
                    enableGroundMist = false;
                    enableAmbientDustParticles = false;
                    enableTorchFlickerMotion = false;

                    enablePostProcessing = false;
                    enableTonemapping = false;
                    bloomQualityOption = BloomQualityOption.Off;
                    enableBloom = false;
                    bloomHighQualityFiltering = false;
                    bloomIntensity = 0.35f;
                    enableVignette = false;
                    enableSplitToning = false;
                    ambientOcclusionOption = AmbientOcclusionOption.Off;
                    enableSSAO = false;
                    ssaoDownsample = true;
                    ssaoIntensity = 0.3f;
                    ssaoSampleCount = 4;

                    defaultVSyncCount = 0;
                    targetFrameRate = 60;
                    break;

                case GraphicsQualityTier.Medium:
                    presetName = "Medium";
                    vietnameseName = "Trung Bình (Medium)";
                    description = "Cân bằng hiệu năng & hình ảnh (Render Scale 90%, FXAA, bóng mềm 2-cascade 35m, Bloom nhẹ, VFX Trung bình).";
                    renderScale = 0.90f;
                    minAdaptiveRenderScale = 0.75f;
                    maxAdaptiveRenderScale = 0.95f;
                    msaaSampleCount = 1;
                    cameraAntialiasing = AntialiasingMode.FastApproximateAntialiasing;
                    smaaQuality = AntialiasingQuality.Low;
                    antiAliasingOption = AntiAliasingOption.FXAA;
                    supportsHDR = true;

                    shadowQualityOption = ShadowQualityOption.Medium;
                    enableMainLightShadows = true;
                    softShadows = true;
                    mainLightShadowResolution = 1024;
                    shadowCascadeCount = 2;
                    shadowDistance = 35f;
                    enableAdditionalLightShadows = false;
                    additionalLightsShadowResolution = 512;

                    maxAdditionalLightsPerObject = 4;
                    maxActiveScenePointLights = 6;
                    enableRealtimeReflectionProbes = false;

                    viewDistanceOption = ViewDistanceOption.Medium;
                    viewDistanceCullMeters = 52f;
                    distantAnimatorThrottleMeters = 28f;
                    lodBias = 0.85f;
                    maximumLODLevel = 0;
                    textureQualityOption = TextureQualityOption.Full;
                    masterTextureLimit = 0;
                    anisotropicFiltering = AnisotropicFiltering.Enable;
                    skinWeights = SkinWeights.FourBones;

                    vfxQualityOption = VfxQualityOption.Medium;
                    particleDensityMultiplier = 0.65f;
                    particleRaycastBudget = 64;
                    enableVfxPointLights = false;
                    enableVfxShockwaveRings = true;
                    enableVfxSecondarySparks = false;
                    enableGroundMist = true;
                    enableAmbientDustParticles = true;
                    enableTorchFlickerMotion = true;

                    enablePostProcessing = true;
                    enableTonemapping = true;
                    bloomQualityOption = BloomQualityOption.Low;
                    enableBloom = true;
                    bloomHighQualityFiltering = false;
                    bloomIntensity = 0.45f;
                    enableVignette = true;
                    enableSplitToning = true;
                    ambientOcclusionOption = AmbientOcclusionOption.Off;
                    enableSSAO = false;
                    ssaoDownsample = true;
                    ssaoIntensity = 0.4f;
                    ssaoSampleCount = 4;

                    defaultVSyncCount = 1;
                    targetFrameRate = 60;
                    break;

                case GraphicsQualityTier.High:
                    presetName = "High";
                    vietnameseName = "Cao (High - Chuẩn 1080p60)";
                    description = "Hướng tới 1080p 60 FPS trên GTX 1650 4GB / RTX 2050. Render Scale 100%, 2x MSAA + SMAA, bóng 2048px (50m), SSAO Half-Res, VFX Cao.";
                    renderScale = 1.00f;
                    minAdaptiveRenderScale = 0.80f;
                    maxAdaptiveRenderScale = 1.00f;
                    msaaSampleCount = 2;
                    cameraAntialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    smaaQuality = AntialiasingQuality.Medium;
                    antiAliasingOption = AntiAliasingOption.MSAA2x_SMAA;
                    supportsHDR = true;

                    shadowQualityOption = ShadowQualityOption.High;
                    enableMainLightShadows = true;
                    softShadows = true;
                    mainLightShadowResolution = 2048;
                    shadowCascadeCount = 2;
                    shadowDistance = 50f;
                    enableAdditionalLightShadows = false;
                    additionalLightsShadowResolution = 1024;

                    maxAdditionalLightsPerObject = 6;
                    maxActiveScenePointLights = 10;
                    enableRealtimeReflectionProbes = true;

                    viewDistanceOption = ViewDistanceOption.Far;
                    viewDistanceCullMeters = 72f;
                    distantAnimatorThrottleMeters = 38f;
                    lodBias = 1.00f;
                    maximumLODLevel = 0;
                    textureQualityOption = TextureQualityOption.Full;
                    masterTextureLimit = 0;
                    anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                    skinWeights = SkinWeights.FourBones;

                    vfxQualityOption = VfxQualityOption.High;
                    particleDensityMultiplier = 1.00f;
                    particleRaycastBudget = 256;
                    enableVfxPointLights = true;
                    enableVfxShockwaveRings = true;
                    enableVfxSecondarySparks = true;
                    enableGroundMist = true;
                    enableAmbientDustParticles = true;
                    enableTorchFlickerMotion = true;

                    enablePostProcessing = true;
                    enableTonemapping = true;
                    bloomQualityOption = BloomQualityOption.Medium;
                    enableBloom = true;
                    bloomHighQualityFiltering = false;
                    bloomIntensity = 0.55f;
                    enableVignette = true;
                    enableSplitToning = true;
                    ambientOcclusionOption = AmbientOcclusionOption.HalfRes;
                    enableSSAO = true;
                    ssaoDownsample = true;
                    ssaoIntensity = 0.45f;
                    ssaoSampleCount = 4;

                    defaultVSyncCount = 1;
                    targetFrameRate = 60;
                    break;

                case GraphicsQualityTier.Ultra:
                    presetName = "Ultra";
                    vietnameseName = "Rất Cao (Ultra)";
                    description = "Dành cho RTX 2060 / RTX 3060 / RX 6600 trở lên. 4x MSAA + SMAA High, bóng 4096px 4-cascade (80m), bóng đèn phụ, SSAO Full-Res.";
                    renderScale = 1.00f;
                    minAdaptiveRenderScale = 0.85f;
                    maxAdaptiveRenderScale = 1.00f;
                    msaaSampleCount = 4;
                    cameraAntialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    smaaQuality = AntialiasingQuality.High;
                    antiAliasingOption = AntiAliasingOption.MSAA4x_SMAA;
                    supportsHDR = true;

                    shadowQualityOption = ShadowQualityOption.Ultra;
                    enableMainLightShadows = true;
                    softShadows = true;
                    mainLightShadowResolution = 4096;
                    shadowCascadeCount = 4;
                    shadowDistance = 80f;
                    enableAdditionalLightShadows = true;
                    additionalLightsShadowResolution = 2048;

                    maxAdditionalLightsPerObject = 8;
                    maxActiveScenePointLights = 16;
                    enableRealtimeReflectionProbes = true;

                    viewDistanceOption = ViewDistanceOption.Ultra;
                    viewDistanceCullMeters = 100f;
                    distantAnimatorThrottleMeters = 55f;
                    lodBias = 1.50f;
                    maximumLODLevel = 0;
                    textureQualityOption = TextureQualityOption.Full;
                    masterTextureLimit = 0;
                    anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                    skinWeights = SkinWeights.Unlimited;

                    vfxQualityOption = VfxQualityOption.Ultra;
                    particleDensityMultiplier = 1.25f;
                    particleRaycastBudget = 1024;
                    enableVfxPointLights = true;
                    enableVfxShockwaveRings = true;
                    enableVfxSecondarySparks = true;
                    enableGroundMist = true;
                    enableAmbientDustParticles = true;
                    enableTorchFlickerMotion = true;

                    enablePostProcessing = true;
                    enableTonemapping = true;
                    bloomQualityOption = BloomQualityOption.High;
                    enableBloom = true;
                    bloomHighQualityFiltering = true;
                    bloomIntensity = 0.58f;
                    enableVignette = true;
                    enableSplitToning = true;
                    ambientOcclusionOption = AmbientOcclusionOption.FullRes;
                    enableSSAO = true;
                    ssaoDownsample = false;
                    ssaoIntensity = 0.50f;
                    ssaoSampleCount = 8;

                    defaultVSyncCount = 1;
                    targetFrameRate = 120;
                    break;

                case GraphicsQualityTier.Cinematic:
                    presetName = "Cinematic";
                    vietnameseName = "Điện Ảnh (Cinematic)";
                    description = "Chất lượng hình ảnh tối đa mà phần cứng hỗ trợ. Supersampling 115%, 4x MSAA, bóng 4096px (120m), SSAO 12-sample, VFX Cực đại.";
                    renderScale = 1.15f;
                    minAdaptiveRenderScale = 0.90f;
                    maxAdaptiveRenderScale = 1.15f;
                    msaaSampleCount = 4;
                    cameraAntialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    smaaQuality = AntialiasingQuality.High;
                    antiAliasingOption = AntiAliasingOption.MSAA4x_SMAA;
                    supportsHDR = true;

                    shadowQualityOption = ShadowQualityOption.Ultra;
                    enableMainLightShadows = true;
                    softShadows = true;
                    mainLightShadowResolution = 4096;
                    shadowCascadeCount = 4;
                    shadowDistance = 120f;
                    enableAdditionalLightShadows = true;
                    additionalLightsShadowResolution = 4096;

                    maxAdditionalLightsPerObject = 8;
                    maxActiveScenePointLights = 24;
                    enableRealtimeReflectionProbes = true;

                    viewDistanceOption = ViewDistanceOption.Ultra;
                    viewDistanceCullMeters = 135f;
                    distantAnimatorThrottleMeters = 75f;
                    lodBias = 2.00f;
                    maximumLODLevel = 0;
                    textureQualityOption = TextureQualityOption.Full;
                    masterTextureLimit = 0;
                    anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                    skinWeights = SkinWeights.Unlimited;

                    vfxQualityOption = VfxQualityOption.Ultra;
                    particleDensityMultiplier = 1.50f;
                    particleRaycastBudget = 2048;
                    enableVfxPointLights = true;
                    enableVfxShockwaveRings = true;
                    enableVfxSecondarySparks = true;
                    enableGroundMist = true;
                    enableAmbientDustParticles = true;
                    enableTorchFlickerMotion = true;

                    enablePostProcessing = true;
                    enableTonemapping = true;
                    bloomQualityOption = BloomQualityOption.High;
                    enableBloom = true;
                    bloomHighQualityFiltering = true;
                    bloomIntensity = 0.62f;
                    enableVignette = true;
                    enableSplitToning = true;
                    ambientOcclusionOption = AmbientOcclusionOption.FullRes;
                    enableSSAO = true;
                    ssaoDownsample = false;
                    ssaoIntensity = 0.55f;
                    ssaoSampleCount = 12;

                    defaultVSyncCount = 1;
                    targetFrameRate = -1;
                    break;
            }
        }
    }
}

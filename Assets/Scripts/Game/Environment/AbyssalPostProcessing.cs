using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AbyssGame
{
    /// <summary>
    /// Configures URP 12.1.7 Camera Anti-Aliasing, Shadow Rendering, and Global Post-Processing Volume
    /// (ACES Tonemapping, controlled Bloom, Vignette, ColorAdjustments, Cool-Shadow/Warm-Torch Grading)
    /// to achieve AAA/AA Dark Fantasy clarity without sacrificing combat readability or frame rate.
    /// </summary>
    public static class AbyssalPostProcessing
    {
        public static Volume EnsureGlobalVolumeAndCameraSetup(Transform sceneRoot = null)
        {
            if (Camera.main != null)
            {
                Camera cam = Camera.main;
                cam.allowHDR = true;
                var camData = cam.GetComponent<UniversalAdditionalCameraData>();
                if (camData == null)
                    camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();

                camData.renderPostProcessing = true;
                camData.renderShadows = true;
                camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                camData.antialiasingQuality = AntialiasingQuality.Medium;
            }

            foreach (var l in Object.FindObjectsOfType<Light>())
            {
                if (l.GetComponent<UniversalAdditionalLightData>() == null)
                    l.gameObject.AddComponent<UniversalAdditionalLightData>();
            }

            var existing = Object.FindObjectOfType<Volume>();
            if (existing != null && existing.isGlobal && existing.sharedProfile != null)
                return existing;

            var volGo = existing != null ? existing.gameObject : new GameObject("AbyssalGlobalVolume");
            if (sceneRoot != null && existing == null)
                volGo.transform.SetParent(sceneRoot, false);

            var volume = existing != null ? existing : volGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.weight = 1f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "AbyssalDarkFantasyProfile";

            // 1. ACES Filmic Tonemapping for rich shadow rolloff & highlight retention
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            // 2. Controlled Bloom (lights torches, crystals & skill VFX without washing out floor telegraphs)
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.55f);
            bloom.scatter.Override(0.64f);
            bloom.highQualityFiltering.Override(false);

            // 3. Subtle Dark-Fantasy Vignette
            var vignette = profile.Add<Vignette>(true);
            vignette.color.Override(new Color(0.02f, 0.02f, 0.04f, 1f));
            vignette.intensity.Override(0.27f);
            vignette.smoothness.Override(0.42f);

            // 4. Crisp Contrast & Exposure
            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.postExposure.Override(0.14f);
            colorAdj.contrast.Override(14f);
            colorAdj.saturation.Override(5f);

            // 5. Split-Toning: Cool Slate-Blue Shadows + Warm Torchlit Sandstone Midtones (Reference Image 4)
            var smh = profile.Add<ShadowsMidtonesHighlights>(true);
            smh.shadows.Override(new Vector4(0.88f, 0.92f, 1.06f, 0f));
            smh.midtones.Override(new Vector4(1.03f, 0.99f, 0.95f, 0f));
            smh.highlights.Override(new Vector4(1.04f, 1.01f, 0.96f, 0f));

            volume.sharedProfile = profile;
            return volume;
        }
    }
}


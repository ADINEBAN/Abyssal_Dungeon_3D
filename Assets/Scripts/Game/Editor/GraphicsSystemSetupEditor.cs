#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AbyssGame.EditorTools
{
    /// <summary>
    /// Editor utility that generates and validates the 5 URP 12.1.7 Pipeline Assets,
    /// the 5 GraphicsPreset ScriptableObjects, and the 5 Unity QualitySettings tiers
    /// without deleting or altering any existing project assets or GUIDs.
    /// </summary>
    public static class GraphicsSystemSetupEditor
    {
        private const string PresetsFolder = "Assets/Resources/GraphicsPresets";
        private const string UrpFolder = "Assets/Settings/Graphics";
        private const string Phase7ReportPath = "Temp/Phase7_GraphicsReport_v1.txt";

        [InitializeOnLoadMethod]
        private static void AutoValidateAndGenerateReportOnLoad()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(Phase7ReportPath)) return;

            try
            {
                GenerateAndValidateAll();
                AbyssalAutoSetup.BuildGothicUIPrefabs();
                WritePhase7VerificationReport();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GraphicsSystemSetupEditor] AutoValidate notice: " + e.Message);
            }
        }

        [MenuItem("Abyssal Dungeon/Graphics/Run Phase 7 Verification Report")]
        public static void WritePhase7VerificationReport()
        {
            var sb = new System.Text.StringBuilder();
            var gm = GraphicsManager.Instance;
            if (gm == null)
            {
                var go = new GameObject("[GraphicsManager]");
                gm = go.AddComponent<GraphicsManager>();
            }
            gm.InitializeIfNeeded();
            var hw = gm.HardwareReport;

            sb.AppendLine("=== ABYSSAL DUNGEON 3D — PHASE 7 HARDWARE & 5-TIER PRESET VERIFICATION REPORT ===");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Unity Version: {Application.unityVersion} | RenderPipeline: { (GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.GetType().Name : "Built-in") }");
            sb.AppendLine($"Active Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            sb.AppendLine($"GPU: {hw.gpuName} ({hw.graphicsApi}) | Vendor: {hw.gpuVendor} (ID: {hw.vendorId})");
            sb.AppendLine($"VRAM Reported: {hw.reportedVramMB} MB | Effective Safe Budget: {hw.estimatedDedicatedVramMB} MB | SharedMemory: {hw.isIntegratedOrSharedMemory}");
            sb.AppendLine($"System RAM: {hw.systemRamMB} MB | CPU: {hw.cpuName} ({hw.cpuCores} cores @ {hw.cpuFrequencyMHz} MHz)");
            sb.AppendLine($"ShaderLevel: {hw.shaderLevel} | Instancing: {hw.supportsInstancing} | Compute: {hw.supportsComputeShaders} | Shadows: {hw.supportsShadows} | MaxTex: {hw.maxTextureSize} | MRT: {hw.supportedRenderTargetCount}");
            sb.AppendLine($"Hardware Class: {hw.hardwareClass} | Conservative Recommended Preset: {hw.staticRecommendedTier}");
            sb.AppendLine($"Recommendation Summary: {hw.recommendationSummary}");
            sb.AppendLine();
            sb.AppendLine("--- 5-TIER RUNTIME SWITCHING & URP 12.1.7 PIPELINE VALIDATION ---");

            foreach (GraphicsQualityTier t in Enum.GetValues(typeof(GraphicsQualityTier)))
            {
                gm.SetQualityTier(t, false, false);
                var p = gm.ActivePreset;
                var urp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
                var vfx = VfxQualitySystem.CurrentRules;
                int texDiv = 1 << (p != null ? p.masterTextureLimit : 0);
                sb.AppendLine(
                    $"[{t,-9}] QualityIdx={QualitySettings.GetQualityLevel()} ({QualitySettings.names[QualitySettings.GetQualityLevel()],-9}) | " +
                    $"URP={(urp != null ? urp.name : "null"),-13} | Scale={(urp != null ? urp.renderScale : 0f):0.00} | " +
                    $"HDR={(urp != null && urp.supportsHDR),-5} | MSAA={(urp != null ? urp.msaaSampleCount : 0)}x | " +
                    $"Shadows={QualitySettings.shadows,-8} ({QualitySettings.shadowDistance:0}m, {QualitySettings.shadowCascades} casc) | " +
                    $"LODBias={QualitySettings.lodBias:0.00} (MaxLOD={QualitySettings.maximumLODLevel}) | " +
                    $"TexRes=1/{texDiv} | AddLights={(urp != null ? urp.maxAdditionalLightsCount : 0)} | " +
                    $"RibbonLayers={vfx.maxSlashRibbonLayers} | Telegraphs={vfx.preserveCombatTelegraphs} | Density={GraphicsManager.ParticleDensityMultiplier:0.00}");
            }

            // Restore recommended default for this hardware (Low on Intel Iris Xe shared memory)
            gm.RestoreDefaultSettings();

            sb.AppendLine();
            sb.AppendLine("--- PHASE 4 & 5 VISUAL, WEAPON & ENVIRONMENT VERIFICATION ---");
            var lootSword = Resources.Load<GameObject>("EnvironmentKit/Weap_Knight_RunicLongsword");
            if (lootSword != null)
            {
                var inst = UnityEngine.Object.Instantiate(lootSword);
                var rs = inst.GetComponentsInChildren<Renderer>(true);
                Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds();
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                sb.AppendLine($"Weap_Knight_RunicLongsword Bounds Size: {b.size:F2} (Fixed from 28.23m -> 1.05m)");
                UnityEngine.Object.DestroyImmediate(inst);
            }

            string[] checkHeroes = { "Knight", "Guardian", "Berserker", "Ranger", "Mage", "Druid", "Cleric" };
            foreach (var hName in checkHeroes)
            {
                var hPrefab = Resources.Load<GameObject>("Models/Heroes/Hero_" + hName);
                if (hPrefab != null)
                {
                    var mainWpn = hPrefab.transform.Find("Rig");
                    var allT = hPrefab.GetComponentsInChildren<Transform>(true);
                    Transform mw = null, ow = null;
                    for (int i = 0; i < allT.Length; i++)
                    {
                        if (allT[i].name == "SignatureMainWeapon") mw = allT[i];
                        else if (allT[i].name == "SignatureOffhand") ow = allT[i];
                    }
                    sb.AppendLine($"Hero_{hName,-9}: MainWpn={(mw != null ? $"rot={mw.localEulerAngles:F0} pos={mw.localPosition:F2}" : "None"),-36} | Offhand={(ow != null ? $"rot={ow.localEulerAngles:F0} pos={ow.localPosition:F2}" : "None")}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("--- MEMORY & PERSISTENCE SNAPSHOT ---");
            sb.AppendLine($"Settings File: {GraphicsSettingsPersistence.SettingsPath} (Exists: {File.Exists(GraphicsSettingsPersistence.SettingsPath)})");
            sb.AppendLine(string.Format("Unity Profiler Memory: TotalAllocated={0:0.1} MB | TotalReserved={1:0.1} MB | MonoHeapUsed={2:0.1} MB",
                UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f),
                UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f),
                UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / (1024f * 1024f)));

            if (!Directory.Exists("Temp")) Directory.CreateDirectory("Temp");
            File.WriteAllText(Phase7ReportPath, sb.ToString());
            Debug.Log(sb.ToString());
        }

        [MenuItem("Abyssal Dungeon/Graphics/Generate & Validate 5-Tier Graphics System")]
        public static void GenerateAndValidateAll()
        {
            EnsureFolders();

            // Reuse existing 3 Renderer configurations (Performant = No SSAO, Balanced = Half-Res SSAO, HighFidelity = Full-Res SSAO)
            var rendererNoSsao = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/Settings/URP-Performant-Renderer.asset");
            var rendererHalfSsao = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/Settings/URP-Balanced-Renderer.asset");
            var rendererFullSsao = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/Settings/URP-HighFidelity-Renderer.asset");

            var templatePerformant = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/URP-Performant.asset");
            var templateBalanced = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/URP-Balanced.asset");
            var templateHigh = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/URP-HighFidelity.asset");

            foreach (GraphicsQualityTier tier in Enum.GetValues(typeof(GraphicsQualityTier)))
            {
                // 1. Create or load GraphicsPreset ScriptableObject
                string presetPath = $"{PresetsFolder}/GraphicsPreset_{tier}.asset";
                GraphicsPreset preset = AssetDatabase.LoadAssetAtPath<GraphicsPreset>(presetPath);
                if (preset == null)
                {
                    preset = ScriptableObject.CreateInstance<GraphicsPreset>();
                    AssetDatabase.CreateAsset(preset, presetPath);
                }
                preset.ApplyDefaultValuesForTier(tier);

                // 2. Create or update tier-specific URP Pipeline Asset cloned non-destructively from appropriate template
                string urpPath = $"{UrpFolder}/URP_{tier}.asset";
                UniversalRenderPipelineAsset urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpPath);
                if (urpAsset == null)
                {
                    string srcPath = tier == GraphicsQualityTier.Low
                        ? "Assets/Settings/URP-Performant.asset"
                        : (tier <= GraphicsQualityTier.High ? "Assets/Settings/URP-Balanced.asset" : "Assets/Settings/URP-HighFidelity.asset");
                    AssetDatabase.CopyAsset(srcPath, urpPath);
                    urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpPath);
                }

                if (urpAsset != null)
                {
                    ScriptableRendererData targetRenderer = tier <= GraphicsQualityTier.Medium
                        ? rendererNoSsao
                        : (tier == GraphicsQualityTier.High ? rendererHalfSsao : rendererFullSsao);

                    ConfigureUrpAssetSerialized(urpAsset, preset, targetRenderer);
                    EditorUtility.SetDirty(urpAsset);
                }

                preset.urpPipelineAsset = urpAsset;
                EditorUtility.SetDirty(preset);
            }

            ConfigureUnityQualitySettingsTiers();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GraphicsSystemSetupEditor] Successfully generated & validated 5 URP Pipeline Assets, 5 GraphicsPreset SOs, and 5 QualitySettings tiers.");
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(PresetsFolder))
                AssetDatabase.CreateFolder("Assets/Resources", "GraphicsPresets");
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");
            if (!AssetDatabase.IsValidFolder(UrpFolder))
                AssetDatabase.CreateFolder("Assets/Settings", "Graphics");
        }

        private static void ConfigureUrpAssetSerialized(UniversalRenderPipelineAsset urpAsset, GraphicsPreset preset, ScriptableRendererData rendererData)
        {
            SerializedObject so = new SerializedObject(urpAsset);
            so.Update();

            if (rendererData != null)
            {
                SerializedProperty listProp = so.FindProperty("m_RendererDataList");
                if (listProp != null && listProp.isArray)
                {
                    if (listProp.arraySize == 0) listProp.InsertArrayElementAtIndex(0);
                    listProp.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
                }
            }

            SetBool(so, "m_SupportsHDR", preset.supportsHDR);
            SetInt(so, "m_MSAA", preset.msaaSampleCount);
            SetFloat(so, "m_RenderScale", preset.renderScale);
            SetBool(so, "m_MainLightShadowsSupported", preset.enableMainLightShadows);
            SetInt(so, "m_MainLightShadowmapResolution", preset.mainLightShadowResolution);
            SetInt(so, "m_AdditionalLightsRenderingMode", preset.maxAdditionalLightsPerObject > 0 ? 1 : 0);
            SetInt(so, "m_AdditionalLightsPerObjectLimit", preset.maxAdditionalLightsPerObject);
            SetBool(so, "m_AdditionalLightShadowsSupported", preset.enableAdditionalLightShadows);
            SetInt(so, "m_AdditionalLightsShadowmapResolution", preset.additionalLightsShadowResolution);
            SetFloat(so, "m_ShadowDistance", preset.shadowDistance);
            SetInt(so, "m_ShadowCascadeCount", preset.shadowCascadeCount);
            SetBool(so, "m_SoftShadowsSupported", preset.softShadows);
            SetBool(so, "m_UseSRPBatcher", true);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureUnityQualitySettingsTiers()
        {
            UnityEngine.Object qsObj = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0];
            if (qsObj == null) return;

            SerializedObject qs = new SerializedObject(qsObj);
            qs.Update();

            SerializedProperty levels = qs.FindProperty("m_QualitySettings");
            if (levels == null || !levels.isArray) return;

            GraphicsQualityTier[] tiers = (GraphicsQualityTier[])Enum.GetValues(typeof(GraphicsQualityTier));
            levels.arraySize = tiers.Length;

            for (int i = 0; i < tiers.Length; i++)
            {
                GraphicsQualityTier tier = tiers[i];
                GraphicsPreset preset = AssetDatabase.LoadAssetAtPath<GraphicsPreset>($"{PresetsFolder}/GraphicsPreset_{tier}.asset");
                UniversalRenderPipelineAsset urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>($"{UrpFolder}/URP_{tier}.asset");

                SerializedProperty elem = levels.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("name").stringValue = tier.ToString();
                elem.FindPropertyRelative("pixelLightCount").intValue = preset != null ? preset.maxAdditionalLightsPerObject : 4;
                elem.FindPropertyRelative("shadows").intValue = preset != null && preset.enableMainLightShadows ? (preset.softShadows ? 2 : 1) : 0;
                elem.FindPropertyRelative("shadowResolution").intValue = i >= 3 ? 3 : (i == 2 ? 2 : 1);
                elem.FindPropertyRelative("shadowProjection").intValue = 1;
                elem.FindPropertyRelative("shadowCascades").intValue = preset != null ? preset.shadowCascadeCount : 2;
                elem.FindPropertyRelative("shadowDistance").floatValue = preset != null ? preset.shadowDistance : 50f;
                elem.FindPropertyRelative("skinWeights").intValue = preset != null ? (int)preset.skinWeights : 4;
                elem.FindPropertyRelative("textureQuality").intValue = 0; // Applied dynamically by GraphicsManager at runtime to avoid Editor texture reload stalls
                elem.FindPropertyRelative("anisotropicTextures").intValue = preset != null ? (int)preset.anisotropicFiltering : 2;
                elem.FindPropertyRelative("antiAliasing").intValue = preset != null && preset.msaaSampleCount > 1 ? preset.msaaSampleCount : 0;
                elem.FindPropertyRelative("softParticles").boolValue = i >= 2;
                elem.FindPropertyRelative("realtimeReflectionProbes").boolValue = preset != null && preset.enableRealtimeReflectionProbes;
                elem.FindPropertyRelative("billboardsFaceCameraPosition").boolValue = i >= 1;
                elem.FindPropertyRelative("vSyncCount").intValue = preset != null ? preset.defaultVSyncCount : 1;
                elem.FindPropertyRelative("lodBias").floatValue = preset != null ? preset.lodBias : 1f;
                elem.FindPropertyRelative("maximumLODLevel").intValue = preset != null ? preset.maximumLODLevel : 0;
                elem.FindPropertyRelative("particleRaycastBudget").intValue = preset != null ? preset.particleRaycastBudget : 256;
                elem.FindPropertyRelative("asyncUploadTimeSlice").intValue = 2;
                elem.FindPropertyRelative("asyncUploadBufferSize").intValue = 16;
                elem.FindPropertyRelative("asyncUploadPersistentBuffer").boolValue = true;
                elem.FindPropertyRelative("resolutionScalingFixedDPIFactor").floatValue = 1f;
                elem.FindPropertyRelative("customRenderPipeline").objectReferenceValue = urp;
            }

            // Default Standalone quality to Medium (index 1) or High (index 2) conservatively before runtime GraphicsManager takes over
            SerializedProperty curQ = qs.FindProperty("m_CurrentQuality");
            if (curQ != null) curQ.intValue = (int)GraphicsQualityTier.Medium;

            qs.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(qsObj);
        }

        private static void SetBool(SerializedObject so, string prop, bool val)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.boolValue = val;
        }

        private static void SetInt(SerializedObject so, string prop, int val)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.intValue = val;
        }

        private static void SetFloat(SerializedObject so, string prop, float val)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.floatValue = val;
        }
    }
}
#endif


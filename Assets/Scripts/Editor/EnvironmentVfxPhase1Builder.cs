#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using AbyssGame;
using AbyssalDungeon.VFX;

namespace AbyssalDungeon.Editor
{
    /// <summary>
    /// Editor Asset Pipeline & Verification Tool for Environment VFX Phase 1.
    /// Configures Blender FBX & PNG texture import settings, bakes shared URP .mat assets,
    /// saves all 25 reusable VFX_Env_*.prefab assets to Assets/Resources/VFX/Prefabs/,
    /// and validates 0 missing references or colliders.
    /// </summary>
    [InitializeOnLoad]
    public static class EnvironmentVfxPhase1Builder
    {
        private const string MeshesFolder = "Assets/Resources/VFX/Meshes";
        private const string TexturesFolder = "Assets/Resources/VFX/Textures";
        private const string MaterialsFolder = "Assets/Resources/VFX/Materials";
        private const string PrefabsFolder = "Assets/Resources/VFX/Prefabs";
        private const string CurveModeV2Marker = "Assets/Resources/VFX/Prefabs/EnvVfxCurveModeV2.txt";
        private const string BenchmarkSessionKey = "Abyssal_Phase1_AutoPlayModeVerify";

        static EnvironmentVfxPhase1Builder()
        {
            EditorApplication.delayCall += RunAutoBuildIfUnbaked;
            EditorApplication.playModeStateChanged += st =>
            {
                if (st == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += RunAutoBuildIfUnbaked;
            };
            EditorApplication.update += PollPlayModeBenchmarkStop;
        }

        private static void RunAutoBuildIfUnbaked()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
                return;

            string probePath = $"{PrefabsFolder}/VFX_Env_TorchFire.prefab";
            GameObject probe = AssetDatabase.LoadAssetAtPath<GameObject>(probePath);
            if (!File.Exists(CurveModeV2Marker) || probe == null || probe.GetComponent<ScalableVfxResponder>() == null)
            {
                BuildAllPhase1Assets(logSummary: true);
                File.WriteAllText(CurveModeV2Marker, "VelocityCurveModeSyncedV2\n");
                AssetDatabase.Refresh();
                VerifyAllPhase1AssetsMenu();
            }
        }

        [MenuItem("Tools/Abyssal Dungeon/Build Environment VFX Phase 1 Prefabs")]
        public static void BuildAllPhase1PrefabsMenu()
        {
            BuildAllPhase1Assets(logSummary: true);
        }

        public static void BuildAllPhase1Assets(bool logSummary)
        {
            EnsureFolderHierarchy("Assets/Resources");
            EnsureFolderHierarchy("Assets/Resources/VFX");
            EnsureFolderHierarchy(MeshesFolder);
            EnsureFolderHierarchy(TexturesFolder);
            EnsureFolderHierarchy(MaterialsFolder);
            EnsureFolderHierarchy(PrefabsFolder);

            ConfigureVfxMeshImporters();
            ConfigureVfxTextureImporters();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            // 1. Bake shared URP materials as serialized .mat assets so saved prefabs never hold transient runtime materials
            Dictionary<string, Material> sharedMats = EnvironmentVfxFactory.BuildAllSharedMaterials();
            int matCount = 0;
            foreach (var kv in sharedMats)
            {
                if (kv.Value == null) continue;
                string matPath = $"{MaterialsFolder}/{kv.Key}.mat";
                Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(UnityEngine.Object.Instantiate(kv.Value), matPath);
                }
                else
                {
                    EditorUtility.CopySerialized(kv.Value, existing);
                    EditorUtility.SetDirty(existing);
                }
                matCount++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 2. Build & Save all 25 reusable Environment VFX prefabs
            int prefabCount = 0;
            foreach (VFXType type in EnvironmentVfxFactory.AllPhase1Types)
            {
                GameObject tempGo = EnvironmentVfxFactory.CreateEnvironmentVfx(type);
                if (tempGo == null) continue;

                // Ensure ParticleSystemRenderers point cleanly to the baked .mat assets on disk
                var renderers = tempGo.GetComponentsInChildren<ParticleSystemRenderer>(true);
                foreach (var r in renderers)
                {
                    if (r.sharedMaterial == null) continue;
                    Material diskMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/{r.sharedMaterial.name}.mat");
                    if (diskMat != null)
                        r.sharedMaterial = diskMat;
                }
                var meshRenderers = tempGo.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var mr in meshRenderers)
                {
                    if (mr.sharedMaterial == null) continue;
                    Material diskMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/{mr.sharedMaterial.name}.mat");
                    if (diskMat != null)
                        mr.sharedMaterial = diskMat;
                }

                string prefabPath = $"{PrefabsFolder}/VFX_{type}.prefab";
                PrefabUtility.SaveAsPrefabAsset(tempGo, prefabPath);
                UnityEngine.Object.DestroyImmediate(tempGo);
                prefabCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (logSummary)
            {
                Debug.Log($"[EnvironmentVfxPhase1Builder] Built {matCount} shared URP materials and {prefabCount}/25 Environment VFX prefabs under {PrefabsFolder}.");
            }
        }

        [MenuItem("Tools/Abyssal Dungeon/Verify Phase 1 Environment VFX Assets")]
        public static void VerifyAllPhase1AssetsMenu()
        {
            var sb = new StringBuilder();
            int passed = 0;
            int warnings = 0;

            foreach (VFXType type in EnvironmentVfxFactory.AllPhase1Types)
            {
                string prefabPath = $"{PrefabsFolder}/VFX_{type}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    sb.AppendLine($"[MISSING] {prefabPath}");
                    warnings++;
                    continue;
                }

                var responder = prefab.GetComponent<ScalableVfxResponder>();
                if (responder == null)
                {
                    sb.AppendLine($"[WARN] {type}: Missing ScalableVfxResponder");
                    warnings++;
                }

                if (prefab.GetComponentsInChildren<Collider>(true).Length > 0)
                {
                    sb.AppendLine($"[ERROR] {type}: Unexpected Collider inside VFX prefab!");
                    warnings++;
                }

                var psList = prefab.GetComponentsInChildren<ParticleSystem>(true);
                if (psList.Length == 0)
                {
                    sb.AppendLine($"[ERROR] {type}: Contains 0 ParticleSystems!");
                    warnings++;
                }

                foreach (var r in prefab.GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
                    if (r.sharedMaterial == null || r.sharedMaterial.shader == null ||
                        r.sharedMaterial.shader.name == "Hidden/InternalErrorShader")
                    {
                        sb.AppendLine($"[ERROR] {type} / {r.name}: Missing or error shader on material!");
                        warnings++;
                    }
                    if (r.renderMode == ParticleSystemRenderMode.Mesh && r.mesh == null)
                    {
                        sb.AppendLine($"[ERROR] {type} / {r.name}: Mesh render mode with null Mesh!");
                        warnings++;
                    }
                }

                passed++;
            }

            string result = $"[EnvironmentVfxPhase1Builder] Asset Audit Complete: Checked {passed}/{EnvironmentVfxFactory.AllPhase1Types.Length} prefabs, Issues={warnings}\n{sb}";
            if (warnings > 0)
                Debug.LogWarning(result);
            else
                Debug.Log(result);
        }

        [MenuItem("Tools/Abyssal Dungeon/Run Phase 1 Runtime Benchmark in PlayMode")]
        public static void RunPhase1PlayModeBenchmarkMenu()
        {
            if (EditorApplication.isPlaying)
                return;

            BuildAllPhase1Assets(logSummary: false);
            string reportPath = Path.Combine(Application.dataPath, "..", EnvironmentVfxPhase1Verifier.ReportFileName);
            if (File.Exists(reportPath)) File.Delete(reportPath);

            if (File.Exists("Assets/Scenes/Dungeon.unity"))
                EditorSceneManager.OpenScene("Assets/Scenes/Dungeon.unity");

            PlayerPrefs.SetInt(EnvironmentVfxPhase1Verifier.BenchmarkPrefKey, 1);
            PlayerPrefs.Save();
            SessionState.SetBool(BenchmarkSessionKey, true);
            SessionState.SetFloat(BenchmarkSessionKey + "_Start", (float)EditorApplication.timeSinceStartup);
            EditorApplication.isPlaying = true;
        }

        private static void PollPlayModeBenchmarkStop()
        {
            if (!SessionState.GetBool(BenchmarkSessionKey, false))
                return;

            if (!EditorApplication.isPlaying)
                return;

            string reportPath = Path.Combine(Application.dataPath, "..", EnvironmentVfxPhase1Verifier.ReportFileName);
            double elapsed = EditorApplication.timeSinceStartup - SessionState.GetFloat(BenchmarkSessionKey + "_Start", 0f);
            if (File.Exists(reportPath) && elapsed > 8.0)
            {
                SessionState.SetBool(BenchmarkSessionKey, false);
                EditorApplication.isPlaying = false;
                Debug.Log($"[EnvironmentVfxPhase1Builder] PlayMode Benchmark completed. Report saved to {reportPath}");
            }
            else if (elapsed > 30.0)
            {
                SessionState.SetBool(BenchmarkSessionKey, false);
                EditorApplication.isPlaying = false;
            }
        }

        private static void ConfigureVfxMeshImporters()
        {
            string[] fbxFiles = { "SM_Vfx_GothicCandleCluster.fbx", "SM_Vfx_StoneDebris.fbx", "SM_Vfx_VortexSpiral.fbx", "SM_Vfx_ShockCone.fbx" };
            foreach (string file in fbxFiles)
            {
                string path = $"{MeshesFolder}/{file}";
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;

                bool changed = false;
                if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
                {
                    importer.materialImportMode = ModelImporterMaterialImportMode.None;
                    changed = true;
                }
                if (importer.importCameras) { importer.importCameras = false; changed = true; }
                if (importer.importLights) { importer.importLights = false; changed = true; }
                if (importer.addCollider) { importer.addCollider = false; changed = true; }
                if (!importer.isReadable) { importer.isReadable = true; changed = true; }
                if (importer.meshCompression != ModelImporterMeshCompression.Medium)
                {
                    importer.meshCompression = ModelImporterMeshCompression.Medium;
                    changed = true;
                }
                if (changed)
                    importer.SaveAndReimport();
            }
        }

        private static void ConfigureVfxTextureImporters()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { TexturesFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool changed = false;
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
                if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; changed = true; }
                if (importer.maxTextureSize > 1024) { importer.maxTextureSize = 1024; changed = true; }
                if (changed)
                    importer.SaveAndReimport();
            }
        }

        private static void EnsureFolderHierarchy(string fullAssetPath)
        {
            string[] parts = fullAssetPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif

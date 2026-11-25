#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using AbyssGame;
using AbyssalDungeon.VFX;

namespace AbyssalDungeon.Editor
{
    /// <summary>
    /// Editor Asset Baker & QA Auditor for Blender 5.3.0 3D Weapon VFX (Phase 2).
    /// Configures the 8 SM_Vfx_Wpn*.fbx ModelImporters, bakes shared URP .mat assets,
    /// saves all 10 reusable VFX_Wpn_*.prefab assets to Assets/Resources/VFX/Prefabs/,
    /// and verifies 0 missing shaders, 0 missing meshes, and 0 colliders.
    /// </summary>
    [InitializeOnLoad]
    public static class WeaponVfxBuilder
    {
        private const string MeshesFolder = "Assets/Resources/VFX/Meshes";
        private const string TexturesFolder = "Assets/Resources/VFX/Textures";
        private const string MaterialsFolder = "Assets/Resources/VFX/Materials";
        private const string PrefabsFolder = "Assets/Resources/VFX/Prefabs";
        private const string WpnCurveV2Marker = "Assets/Resources/VFX/Prefabs/WpnVfxDurationFixedV3.txt";

        private static readonly string[] WeaponFbxFiles =
        {
            "SM_Vfx_WpnSlashCrescent.fbx",
            "SM_Vfx_WpnHeavyCleaveWave.fbx",
            "SM_Vfx_WpnSpinWhirlwind.fbx",
            "SM_Vfx_WpnThrustSpearCone.fbx",
            "SM_Vfx_WpnArrowBolt.fbx",
            "SM_Vfx_WpnSpellBoltCore.fbx",
            "SM_Vfx_WpnShieldAegis.fbx",
            "SM_Vfx_WpnBladeAuraSheath.fbx",
            "Assassin/SM_Asn_DualBladeSlash.fbx",
            "Assassin/SM_Asn_RapidComboTrails.fbx",
            "Assassin/SM_Asn_DashAfterimage.fbx",
            "Assassin/SM_Asn_BackstabImpact.fbx",
            "Assassin/SM_Asn_PoisonBladeImpact.fbx",
            "Assassin/SM_Asn_ShadowBurst.fbx",
            "Shared/SM_Shared_Slash_Thin.fbx",
            "Shared/SM_Shared_Slash_WideHeavy.fbx",
            "Shared/SM_Shared_Slash_Crescent.fbx",
            "Shared/SM_Shared_Slash_Double.fbx",
            "Shared/SM_Shared_Slash_Cross.fbx",
            "Shared/SM_Shared_Slash_MultiEnergy.fbx",
            "Shared/SM_Shared_Impact_WeaponStar.fbx",
            "Shared/SM_Shared_Impact_GroundBurst.fbx",
            "Shared/SM_Shared_Shockwave_Circular.fbx",
            "Shared/SM_Shared_Impact_RadialBurst.fbx",
            "Shared/SM_Shared_Impact_GroundCrack.fbx",
            "Shared/SM_Shared_Debris_Cluster.fbx",
            "Shared/SM_Shared_Rune_GothicCircle.fbx",
            "Shared/SM_Shared_Energy_ArcaneRing.fbx",
            "Shared/SM_Shared_Energy_Orb.fbx",
            "Shared/SM_Shared_Projectile_Core.fbx",
            "Shared/SM_Shared_Energy_Beam.fbx",
            "Shared/SM_Shared_Energy_Spiral.fbx",
            "Shared/SM_Shared_Rune_SummoningCircle.fbx"
        };

        static WeaponVfxBuilder()
        {
            EditorApplication.delayCall += RunAutoBuildIfUnbaked;
            EditorApplication.playModeStateChanged += st =>
            {
                if (st == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += RunAutoBuildIfUnbaked;
            };
        }

        private static void RunAutoBuildIfUnbaked()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
                return;

            string probePath = $"{PrefabsFolder}/VFX_Wpn_LightSlash.prefab";
            GameObject probe = AssetDatabase.LoadAssetAtPath<GameObject>(probePath);
            if (!File.Exists(WpnCurveV2Marker) || probe == null || probe.GetComponent<ScalableVfxResponder>() == null)
            {
                BuildAllWeaponVfxAssets(logSummary: true);
                File.WriteAllText(WpnCurveV2Marker, "WpnDurationFixedV2\n");
                AssetDatabase.Refresh();
                VerifyWeaponVfxAssetsMenu();
            }
        }

        [MenuItem("Tools/Abyssal Dungeon/Build Blender 3D Weapon VFX Prefabs")]
        public static void BuildAllWeaponVfxPrefabsMenu()
        {
            BuildAllWeaponVfxAssets(logSummary: true);
            VerifyWeaponVfxAssetsMenu();
        }

        public static void BuildAllWeaponVfxAssets(bool logSummary)
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/VFX");
            EnsureFolder(MeshesFolder);
            EnsureFolder(TexturesFolder);
            EnsureFolder(MaterialsFolder);
            EnsureFolder(PrefabsFolder);

            ConfigureWeaponMeshImporters();
            ConfigureWeaponTextureImporters();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            // 1. Bake shared URP weapon materials as serialized .mat assets
            Dictionary<string, Material> sharedMats = WeaponVfxFactory.BuildAllSharedWeaponMaterials();
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

            // 2. Build & Save all 10 reusable 3D Weapon VFX prefabs
            int prefabCount = 0;
            foreach (VFXType type in WeaponVfxFactory.AllWeaponVfxTypes)
            {
                GameObject tempGo = WeaponVfxFactory.CreateWeaponVfx(type);
                if (tempGo == null) continue;

                foreach (var r in tempGo.GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
                    if (r.sharedMaterial == null) continue;
                    Material diskMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/{r.sharedMaterial.name}.mat");
                    if (diskMat != null) r.sharedMaterial = diskMat;
                }
                foreach (var mr in tempGo.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (mr.sharedMaterial == null) continue;
                    Material diskMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/{mr.sharedMaterial.name}.mat");
                    if (diskMat != null) mr.sharedMaterial = diskMat;
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
                Debug.Log($"[WeaponVfxBuilder] Built {matCount} shared URP weapon materials and {prefabCount}/{WeaponVfxFactory.AllWeaponVfxTypes.Length} 3D Weapon VFX prefabs under {PrefabsFolder}.");
            }
        }

        [MenuItem("Tools/Abyssal Dungeon/Verify Blender 3D Weapon VFX Assets")]
        public static void VerifyWeaponVfxAssetsMenu()
        {
            var sb = new StringBuilder();
            int checkedMeshes = 0;
            int checkedPrefabs = 0;
            int issues = 0;

            foreach (string fbx in WeaponFbxFiles)
            {
                string meshPath = $"{MeshesFolder}/{fbx}";
                Mesh m = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (m == null)
                {
                    GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(meshPath);
                    if (go != null)
                    {
                        var mf = go.GetComponentInChildren<MeshFilter>(true);
                        if (mf != null) m = mf.sharedMesh;
                    }
                }
                if (m == null)
                {
                    sb.AppendLine($"[ERROR] Missing imported Blender mesh: {meshPath}");
                    issues++;
                }
                else
                {
                    checkedMeshes++;
                }
            }

            foreach (VFXType type in WeaponVfxFactory.AllWeaponVfxTypes)
            {
                string prefabPath = $"{PrefabsFolder}/VFX_{type}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    sb.AppendLine($"[MISSING] {prefabPath}");
                    issues++;
                    continue;
                }

                if (prefab.GetComponent<ScalableVfxResponder>() == null)
                {
                    sb.AppendLine($"[WARN] {type}: Missing ScalableVfxResponder");
                    issues++;
                }

                if (prefab.GetComponentsInChildren<Collider>(true).Length > 0)
                {
                    sb.AppendLine($"[ERROR] {type}: Unexpected Collider inside Weapon VFX prefab!");
                    issues++;
                }

                foreach (var r in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.sharedMaterial == null || r.sharedMaterial.shader == null ||
                        r.sharedMaterial.shader.name == "Hidden/InternalErrorShader")
                    {
                        sb.AppendLine($"[ERROR] {type} / {r.name}: Missing or error shader!");
                        issues++;
                    }
                    if (r is ParticleSystemRenderer psr && psr.renderMode == ParticleSystemRenderMode.Mesh && psr.mesh == null)
                    {
                        sb.AppendLine($"[ERROR] {type} / {r.name}: Mesh ParticleSystem with null mesh!");
                        issues++;
                    }
                }
                checkedPrefabs++;
            }

            string msg = $"[WeaponVfxBuilder] Audit Complete: Meshes={checkedMeshes}/{WeaponFbxFiles.Length}, Prefabs={checkedPrefabs}/{WeaponVfxFactory.AllWeaponVfxTypes.Length}, Issues={issues}\n{sb}";
            if (issues > 0) Debug.LogWarning(msg);
            else Debug.Log(msg);
        }

        private static void ConfigureWeaponMeshImporters()
        {
            foreach (string file in WeaponFbxFiles)
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

        private static void ConfigureWeaponTextureImporters()
        {
            string[] texFiles =
            {
                "T_Vfx_WpnRuneStreak.png", "T_Vfx_WpnSerratedArc.png", "T_Vfx_WpnAegisHex.png",
                "T_Shared_GothicSlashArc.png", "T_Shared_GothicRuneSeal.png", "T_Shared_ShockwaveBurst.png",
                "T_Asn_CrimsonShadowSlash.png", "T_Asn_VenomSplatter.png", "T_Asn_ShadowSmokeWisps.png", "T_Asn_BackstabSigil.png"
            };
            foreach (string file in texFiles)
            {
                string path = $"{TexturesFolder}/{file}";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool changed = false;
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
                if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; changed = true; }
                TextureWrapMode desiredWrap = file == "T_Vfx_WpnRuneStreak.png" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                if (importer.wrapMode != desiredWrap) { importer.wrapMode = desiredWrap; changed = true; }
                if (importer.maxTextureSize > 1024) { importer.maxTextureSize = 1024; changed = true; }
                if (changed)
                    importer.SaveAndReimport();
            }
        }

        private static void EnsureFolder(string fullAssetPath)
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

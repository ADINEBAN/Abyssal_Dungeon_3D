#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AbyssGame.EditorTools
{
    /// <summary>
    /// Imports bespoke Blender Gothic weapons from Assets/Art/Weapons/Batch{1,2,3},
    /// builds URP 12.1.7 PBR materials, reusable weapon prefabs (Assets/Art/Weapons/Prefabs + Resources/Weapons/Prefabs),
    /// data-driven WeaponAttachmentProfile & ClassWeaponLoadoutProfile assets,
    /// upgrades all 10 Hero Animator Controllers to eliminate transition/tail-lock jitter,
    /// and syncs Hero prefabs with the new weapons.
    /// </summary>
    public static class GothicWeaponAssetSetup
    {
        private const string ArtWeaponsRoot = "Assets/Art/Weapons";
        private const string ArtMaterialsDir = ArtWeaponsRoot + "/Materials";
        private const string ArtPrefabsDir = ArtWeaponsRoot + "/Prefabs";
        private const string ArtTexturesDir = ArtWeaponsRoot + "/Textures";
        private const string ResPrefabsDir = "Assets/Resources/Weapons/Prefabs";
        private const string ResProfilesDir = "Assets/Resources/Weapons/Profiles";
        private const string AutoRunFlagPath = "Temp/run_gothic_weapon_setup.flag";

        [InitializeOnLoadMethod]
        private static void CheckPendingAutoSetup()
        {
            if (!File.Exists(AutoRunFlagPath)) return;
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(AutoRunFlagPath) || EditorApplication.isPlayingOrWillChangePlaymode) return;
                try { File.Delete(AutoRunFlagPath); } catch { }
                BuildAll();
            };
        }

        [MenuItem("Tools/Abyssal Dungeon/3. Build Gothic Weapons, Profiles & Smooth Animators")]
        public static void BuildAll()
        {
            EnsureDirs();
            ConfigureWeaponTextures();
            var mats = BuildUrpWeaponMaterials();
            var prefabs = BuildAllWeaponPrefabs(mats);
            var loadouts = BuildAllAttachmentProfiles(prefabs);
            UpgradeAllHeroAnimators();
            SyncHeroPrefabsWithLoadouts(loadouts);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[GothicWeaponAssetSetup] Done! Prefabs={prefabs.Count}, Loadouts={loadouts.Count}, Materials={mats.Count}.");
        }

        private static void EnsureDirs()
        {
            foreach (var d in new[] { ArtWeaponsRoot, ArtMaterialsDir, ArtPrefabsDir, ResPrefabsDir, ResProfilesDir })
            {
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
            }
            AssetDatabase.Refresh();
        }

        private static void ConfigureWeaponTextures()
        {
            if (!Directory.Exists(ArtTexturesDir)) return;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var raw in Directory.GetFiles(ArtTexturesDir, "*.png"))
                {
                    string path = raw.Replace("\\", "/");
                    var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (ti == null) continue;
                    string fn = Path.GetFileNameWithoutExtension(path);
                    bool dirty = false;

                    if (fn.EndsWith("_Normal", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
                        if (ti.sRGBTexture) { ti.sRGBTexture = false; dirty = true; }
                    }
                    else if (fn.EndsWith("_Mask", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ti.textureType != TextureImporterType.Default) { ti.textureType = TextureImporterType.Default; dirty = true; }
                        if (ti.sRGBTexture) { ti.sRGBTexture = false; dirty = true; }
                    }
                    else
                    {
                        if (!ti.sRGBTexture) { ti.sRGBTexture = true; dirty = true; }
                    }

                    if (ti.anisoLevel < 4) { ti.anisoLevel = 4; dirty = true; }
                    if (ti.maxTextureSize > 1024) { ti.maxTextureSize = 1024; dirty = true; }
                    if (dirty) ti.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        private static Dictionary<string, Material> BuildUrpWeaponMaterials()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var dict = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);

            var specs = new (string name, string texPrefix, Color col, float metal, float smooth, Color emis, bool useRuneMap)[]
            {
                ("M_Wpn_DarkSteel",    "T_Wpn_DarkSteel",   new Color(0.68f, 0.72f, 0.80f, 1f), 0.90f, 0.68f, Color.black, false),
                ("M_Wpn_AntiqueGold",  "T_Wpn_AntiqueGold", new Color(0.95f, 0.78f, 0.38f, 1f), 0.88f, 0.66f, Color.black, false),
                ("M_Wpn_LeatherGrip",  "T_Wpn_LeatherWood", new Color(0.36f, 0.24f, 0.18f, 1f), 0.05f, 0.26f, Color.black, false),
                ("M_Wpn_DarkWood",     "T_Wpn_LeatherWood", new Color(0.42f, 0.28f, 0.18f, 1f), 0.06f, 0.34f, Color.black, false),
                ("M_Wpn_BowString",    null,                new Color(0.86f, 0.82f, 0.72f, 1f), 0.02f, 0.24f, Color.black, false),
                ("M_Wpn_RuneGold",     null,                new Color(1.00f, 0.86f, 0.44f, 1f), 0.25f, 0.78f, new Color(1.00f, 0.76f, 0.24f) * 2.2f, true),
                ("M_Wpn_RuneCrimson",  null,                new Color(1.00f, 0.26f, 0.16f, 1f), 0.20f, 0.76f, new Color(1.00f, 0.20f, 0.08f) * 2.5f, true),
                ("M_Wpn_RuneEmerald",  null,                new Color(0.36f, 0.98f, 0.56f, 1f), 0.18f, 0.76f, new Color(0.22f, 0.96f, 0.46f) * 2.2f, true),
                ("M_Wpn_RuneArcane",   null,                new Color(0.40f, 0.78f, 1.00f, 1f), 0.20f, 0.82f, new Color(0.28f, 0.68f, 1.00f) * 2.4f, true),
                ("M_Wpn_RuneViolet",   null,                new Color(0.76f, 0.36f, 1.00f, 1f), 0.20f, 0.82f, new Color(0.68f, 0.22f, 1.00f) * 2.4f, true),
                ("M_Wpn_RuneHoly",     null,                new Color(1.00f, 0.94f, 0.68f, 1f), 0.28f, 0.80f, new Color(1.00f, 0.90f, 0.48f) * 2.4f, true),
                ("M_Wpn_Parchment",    "T_Wpn_LeatherWood", new Color(0.84f, 0.76f, 0.58f, 1f), 0.02f, 0.18f, Color.black, false),
            };

            Texture2D runeEmis = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtTexturesDir}/T_Wpn_GothicRune_Emission.png");

            foreach (var sp in specs)
            {
                string path = $"{ArtMaterialsDir}/{sp.name}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(urpLit) { name = sp.name };
                    AssetDatabase.CreateAsset(mat, path);
                }
                else
                {
                    mat.shader = urpLit;
                }

                mat.enableInstancing = true;
                mat.color = sp.col;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", sp.col);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", sp.metal);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", sp.smooth);

                if (!string.IsNullOrEmpty(sp.texPrefix))
                {
                    var alb = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtTexturesDir}/{sp.texPrefix}_Albedo.png");
                    var nrm = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtTexturesDir}/{sp.texPrefix}_Normal.png");
                    var msk = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtTexturesDir}/{sp.texPrefix}_Mask.png");
                    if (alb != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", alb);
                    if (nrm != null && mat.HasProperty("_BumpMap"))
                    {
                        mat.SetTexture("_BumpMap", nrm);
                        mat.SetFloat("_BumpScale", 0.95f);
                        mat.EnableKeyword("_NORMALMAP");
                    }
                    if (msk != null && mat.HasProperty("_MetallicGlossMap"))
                    {
                        mat.SetTexture("_MetallicGlossMap", msk);
                        mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                    }
                }

                if (sp.emis.maxColorComponent > 0.01f)
                {
                    mat.EnableKeyword("_EMISSION");
                    if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", sp.emis);
                    if (sp.useRuneMap && runeEmis != null && mat.HasProperty("_EmissionMap"))
                        mat.SetTexture("_EmissionMap", runeEmis);
                }

                EditorUtility.SetDirty(mat);
                dict[sp.name] = mat;
            }
            return dict;
        }

        private static Dictionary<string, GameObject> BuildAllWeaponPrefabs(Dictionary<string, Material> mats)
        {
            var result = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
            string[] batchDirs = { "Batch1", "Batch2", "Batch3" };

            foreach (var bd in batchDirs)
            {
                string fullDir = $"{ArtWeaponsRoot}/{bd}";
                if (!Directory.Exists(fullDir)) continue;

                foreach (var raw in Directory.GetFiles(fullDir, "*.fbx"))
                {
                    string fbxPath = raw.Replace("\\", "/");
                    string wpnName = Path.GetFileNameWithoutExtension(fbxPath);

                    // Ensure clean FBX import settings (scale 1, normals calculated cleanly, no unused animation)
                    var mi = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
                    if (mi != null)
                    {
                        bool dirty = false;
                        if (mi.globalScale != 1f) { mi.globalScale = 1f; dirty = true; }
                        if (mi.importAnimation) { mi.importAnimation = false; dirty = true; }
                        if (mi.importCameras) { mi.importCameras = false; dirty = true; }
                        if (mi.importLights) { mi.importLights = false; dirty = true; }
                        if (mi.animationType != ModelImporterAnimationType.None) { mi.animationType = ModelImporterAnimationType.None; dirty = true; }
                        if (dirty) mi.SaveAndReimport();
                    }

                    GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                    if (fbxAsset == null)
                    {
                        Debug.LogWarning($"[GothicWeaponAssetSetup] Failed to load FBX: {fbxPath}");
                        continue;
                    }

                    var inst = (GameObject)UnityEngine.Object.Instantiate(fbxAsset);
                    inst.name = wpnName;
                    inst.transform.position = Vector3.zero;
                    inst.transform.rotation = Quaternion.identity;
                    inst.transform.localScale = Vector3.one;

                    // Flatten redundant outer wrapper if FBX root contains a single empty with the same name
                    if (inst.transform.childCount == 1 && inst.transform.GetChild(0).name == wpnName)
                    {
                        Transform inner = inst.transform.GetChild(0);
                        inner.localPosition = Vector3.zero;
                        inner.localRotation = Quaternion.identity;
                        inner.localScale = Vector3.one;
                    }

                    foreach (var col in inst.GetComponentsInChildren<Collider>(true))
                        UnityEngine.Object.DestroyImmediate(col);

                    // Bind persistent URP materials cleanly by name prefix
                    foreach (var mr in inst.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                        mr.receiveShadows = true;
                        var srcMats = mr.sharedMaterials;
                        var dstMats = new Material[srcMats.Length];
                        for (int i = 0; i < srcMats.Length; i++)
                        {
                            Material resolved = null;
                            if (srcMats[i] != null)
                            {
                                string mn = srcMats[i].name;
                                int dot = mn.IndexOf('.');
                                if (dot > 0) mn = mn.Substring(0, dot);
                                mats.TryGetValue(mn, out resolved);
                            }
                            dstMats[i] = resolved ?? mats["M_Wpn_DarkSteel"];
                        }
                        mr.sharedMaterials = dstMats;
                    }

                    // Attach BowStringRigAnimator directly on Recurve Bow prefab
                    if (wpnName.IndexOf("Bow", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var rig = inst.GetComponent<BowStringRigAnimator>();
                        if (rig == null) rig = inst.AddComponent<BowStringRigAnimator>();
                    }

                    string artPrefabPath = $"{ArtPrefabsDir}/{wpnName}.prefab";
                    string resPrefabPath = $"{ResPrefabsDir}/{wpnName}.prefab";
                    GameObject savedArt = PrefabUtility.SaveAsPrefabAsset(inst, artPrefabPath);
                    PrefabUtility.SaveAsPrefabAsset(inst, resPrefabPath);
                    UnityEngine.Object.DestroyImmediate(inst);

                    result[wpnName] = savedArt;
                }
            }
            return result;
        }

        private static WeaponAttachmentProfile SaveProfile(
            string assetName,
            string weaponId,
            GameObject prefab,
            WeaponGripType gripType,
            string primaryBone,
            string[] fallbacks,
            string secondaryBone,
            bool isFloating,
            Vector3 localPos,
            Vector3 localRot,
            Vector3 localScale,
            bool hasTrail,
            string tipSocket,
            string projSocket,
            string impactSocket,
            Vector3 fallbackTip,
            float trailW,
            float auraScale)
        {
            string path = $"{ResProfilesDir}/{assetName}.asset";
            var prof = AssetDatabase.LoadAssetAtPath<WeaponAttachmentProfile>(path);
            if (prof == null)
            {
                if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
                prof = ScriptableObject.CreateInstance<WeaponAttachmentProfile>();
                AssetDatabase.CreateAsset(prof, path);
            }
            prof.WeaponId = weaponId;
            prof.WeaponPrefab = prefab;
            prof.GripType = gripType;
            prof.PrimaryHandBone = primaryBone;
            prof.FallbackHandBones = fallbacks;
            prof.SecondaryHandBone = secondaryBone;
            prof.IsFloatingRelic = isFloating;
            prof.LocalPosition = localPos;
            prof.LocalRotation = localRot;
            prof.LocalScale = localScale;
            prof.GripOffset = Vector3.zero;
            prof.HasBladeTrail = hasTrail;
            prof.TipSocketName = tipSocket;
            prof.ProjectileSocketName = projSocket;
            prof.ImpactSocketName = impactSocket;
            prof.FallbackTipLocalOffset = fallbackTip;
            prof.TrailWidth = trailW;
            prof.AuraScale = auraScale;
            EditorUtility.SetDirty(prof);
            return prof;
        }

        private static ClassWeaponLoadoutProfile SaveLoadout(
            int classId, string className,
            WeaponAttachmentProfile mainP, WeaponAttachmentProfile offP,
            GameObject projPrefab,
            float basicWind, float basicAct, float heavyWind, float heavyAct, float skillWind)
        {
            string path = $"{ResProfilesDir}/Loadout_{className}.asset";
            var lo = AssetDatabase.LoadAssetAtPath<ClassWeaponLoadoutProfile>(path);
            if (lo == null)
            {
                if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
                lo = ScriptableObject.CreateInstance<ClassWeaponLoadoutProfile>();
                AssetDatabase.CreateAsset(lo, path);
            }
            lo.ClassId = classId;
            lo.ClassName = className;
            lo.MainHandProfile = mainP;
            lo.OffHandProfile = offP;
            lo.ProjectileVisualPrefab = projPrefab;
            lo.BasicWindup = basicWind;
            lo.BasicActive = basicAct;
            lo.HeavyWindup = heavyWind;
            lo.HeavyActive = heavyAct;
            lo.SkillCastWindup = skillWind;
            EditorUtility.SetDirty(lo);
            return lo;
        }

        private static Dictionary<string, ClassWeaponLoadoutProfile> BuildAllAttachmentProfiles(Dictionary<string, GameObject> prefabs)
        {
            var loadouts = new Dictionary<string, ClassWeaponLoadoutProfile>(StringComparer.OrdinalIgnoreCase);
            string[] rFallbacks = { "Bip001 R Hand", "RightHand" };
            string[] lFallbacks = { "Bip001 L Hand", "LeftHand" };

            GameObject GetWpn(string id) => prefabs.TryGetValue(id, out var go) ? go : null;

            // --- BATCH 1: KNIGHT, BERSERKER, RANGER ---
            if (GetWpn("Knight_Longsword") != null && GetWpn("Knight_HeaterShield") != null)
            {
                var pSw = SaveProfile("WAP_Knight_Longsword", "Knight_Longsword", GetWpn("Knight_Longsword"),
                    WeaponGripType.OneHandedSword, "RH_WP", rFallbacks, "", false,
                    Vector3.zero, Vector3.zero, Vector3.one,
                    true, "VfxTrailSocket", "", "BladeTip", new Vector3(0f, 0f, 1.18f), 0.28f, 0.95f);

                // Knight LH_WP measured in CharSpace: +Z=Up, +Y=Forward, -X=Outward Left.
                // Knight_HeaterShield authored with +Z=Height, +Y=Front Face -> aligns at identity rotation with clean slight forearm clearance!
                var pSh = SaveProfile("WAP_Knight_HeaterShield", "Knight_HeaterShield", GetWpn("Knight_HeaterShield"),
                    WeaponGripType.Shield, "LH_WP", lFallbacks, "Bip001 L Forearm", false,
                    new Vector3(0.02f, 0.03f, 0.0f), Vector3.zero, Vector3.one,
                    false, "", "", "ShieldFrontCenter", Vector3.zero, 0.2f, 0.72f);

                loadouts["Knight"] = SaveLoadout(0, "Knight", pSw, pSh, null, 0.09f, 0.14f, 0.16f, 0.18f, 0.14f);
            }

            if (GetWpn("Berserker_Greatsword") != null)
            {
                var pGs = SaveProfile("WAP_Berserker_Greatsword", "Berserker_Greatsword", GetWpn("Berserker_Greatsword"),
                    WeaponGripType.GreatswordTwoHanded, "RH_WP", rFallbacks, "Bip001 L Hand", false,
                    Vector3.zero, Vector3.zero, Vector3.one,
                    true, "VfxTrailSocket", "", "BladeTip", new Vector3(0f, 0f, 1.42f), 0.48f, 1.32f);

                loadouts["Berserker"] = SaveLoadout(2, "Berserker", pGs, null, null, 0.15f, 0.18f, 0.22f, 0.22f, 0.18f);
            }

            if (GetWpn("Ranger_RecurveBow") != null)
            {
                // Ranger LH_WP measured in CombatIdle: +X=(0.01,0.87,-0.49), +Y=(-0.30,0.47,0.83), +Z=(+0.95,0.14,0.27).
                // Clenched left fist cavity center in LH_WP = (0.000, -0.005, +0.016) so fingers & thumb wrap tightly around RangerBow_Grip!
                // Euler(304, 270, 354) keeps the bow 99.5% vertical (up=(-0.05, 0.995, 0.09)), aiming level forward with 9.1cm forearm & 28.4cm shoulder clearance.
                var pBow = SaveProfile("WAP_Ranger_RecurveBow", "Ranger_RecurveBow", GetWpn("Ranger_RecurveBow"),
                    WeaponGripType.Bow, "LH_WP", lFallbacks, "Bip001 R Hand", false,
                    new Vector3(0.000f, -0.005f, 0.016f), new Vector3(304f, 270f, 354f), Vector3.one * 0.85f,
                    false, "VfxTrailSocket", "ProjectileSpawn", "ProjectileSpawn", new Vector3(0f, 0.04f, 0.36f), 0.18f, 0.0f);

                loadouts["Ranger"] = SaveLoadout(4, "Ranger", null, pBow, GetWpn("Ranger_Arrow"), 0.12f, 0.10f, 0.20f, 0.12f, 0.16f);
            }

            // --- BATCH 2: GUARDIAN, ASSASSIN, MAGE ---
            if (GetWpn("Guardian_HeavyMace") != null && GetWpn("Guardian_TowerShield") != null)
            {
                var pMace = SaveProfile("WAP_Guardian_HeavyMace", "Guardian_HeavyMace", GetWpn("Guardian_HeavyMace"),
                    WeaponGripType.Hammer, "RH_WP", rFallbacks, "", false,
                    Vector3.zero, Vector3.zero, Vector3.one,
                    true, "VfxTrailSocket", "", "ImpactPoint", new Vector3(0f, 0f, 0.78f), 0.34f, 1.05f);

                var pTsh = SaveProfile("WAP_Guardian_TowerShield", "Guardian_TowerShield", GetWpn("Guardian_TowerShield"),
                    WeaponGripType.Shield, "LH_WP", lFallbacks, "Bip001 L Forearm", false,
                    new Vector3(0.03f, 0.05f, -0.06f), Vector3.zero, Vector3.one,
                    false, "", "", "ShieldFrontCenter", Vector3.zero, 0.2f, 0.85f);

                loadouts["Guardian"] = SaveLoadout(1, "Guardian", pMace, pTsh, null, 0.13f, 0.16f, 0.20f, 0.20f, 0.16f);
            }

            if (GetWpn("Assassin_DaggerR") != null && GetWpn("Assassin_DaggerL") != null)
            {
                var pDagR = SaveProfile("WAP_Assassin_DaggerR", "Assassin_DaggerR", GetWpn("Assassin_DaggerR"),
                    WeaponGripType.DualWield, "RH_WP", rFallbacks, "", false,
                    Vector3.zero, Vector3.zero, Vector3.one,
                    true, "VfxTrailSocket", "", "BladeTip", new Vector3(0f, 0f, 0.56f), 0.21f, 0.72f);

                var pDagL = SaveProfile("WAP_Assassin_DaggerL", "Assassin_DaggerL", GetWpn("Assassin_DaggerL"),
                    WeaponGripType.DualWield, "LH_WP", lFallbacks, "", false,
                    Vector3.zero, Vector3.zero, Vector3.one,
                    true, "VfxTrailSocket", "", "BladeTip", new Vector3(0f, 0f, 0.56f), 0.20f, 0.70f);

                loadouts["Assassin"] = SaveLoadout(3, "Assassin", pDagR, pDagL, null, 0.05f, 0.11f, 0.11f, 0.14f, 0.10f);
            }

            if (GetWpn("Mage_ArcaneStaff") != null)
            {
                var pStaff = SaveProfile("WAP_Mage_ArcaneStaff", "Mage_ArcaneStaff", GetWpn("Mage_ArcaneStaff"),
                    WeaponGripType.Staff, "RH_WP", rFallbacks, "", false,
                    Vector3.zero, Vector3.zero, Vector3.one,
                    true, "MagicEmitSocket", "MagicEmitSocket", "MagicEmitSocket", new Vector3(0f, 0f, 1.02f), 0.26f, 0.95f);

                loadouts["Mage"] = SaveLoadout(5, "Mage", pStaff, null, null, 0.13f, 0.10f, 0.20f, 0.12f, 0.16f);
            }

            // --- BATCH 3: WARLOCK, MONK, DRUID, CLERIC ---
            if (GetWpn("Warlock_RitualSword") != null && GetWpn("Warlock_Grimoire") != null)
            {
                var pWSw = SaveProfile("WAP_Warlock_RitualSword", "Warlock_RitualSword", GetWpn("Warlock_RitualSword"),
                    WeaponGripType.OneHandedSword, "RH_WP", rFallbacks, "", false,
                    Vector3.zero, Vector3.zero, Vector3.one,
                    true, "VfxTrailSocket", "MagicEmitSocket", "BladeTip", new Vector3(0f, 0f, 0.72f), 0.24f, 0.88f);

                var pBook = SaveProfile("WAP_Warlock_Grimoire", "Warlock_Grimoire", GetWpn("Warlock_Grimoire"),
                    WeaponGripType.BookOffhand, "", Array.Empty<string>(), "", true,
                    new Vector3(-0.50f, 1.44f, 0.24f), new Vector3(16f, -24f, 0f), Vector3.one,
                    false, "", "MagicEmitSocket", "MagicEmitSocket", Vector3.zero, 0.2f, 0.82f);

                loadouts["Warlock"] = SaveLoadout(6, "Warlock", pWSw, pBook, null, 0.12f, 0.12f, 0.18f, 0.15f, 0.16f);
            }

            if (GetWpn("Monk_GauntletR") != null && GetWpn("Monk_GauntletL") != null)
            {
                var pGR = SaveProfile("WAP_Monk_GauntletR", "Monk_GauntletR", GetWpn("Monk_GauntletR"),
                    WeaponGripType.FistWeapon, "RH_WP", rFallbacks, "", false,
                    new Vector3(0.02f, 0f, 0f), Vector3.zero, Vector3.one,
                    true, "ImpactPoint", "", "ImpactPoint", new Vector3(0f, 0f, 0.18f), 0.24f, 0.78f);

                var pGL = SaveProfile("WAP_Monk_GauntletL", "Monk_GauntletL", GetWpn("Monk_GauntletL"),
                    WeaponGripType.FistWeapon, "LH_WP", lFallbacks, "", false,
                    new Vector3(0.02f, 0f, 0f), Vector3.zero, Vector3.one,
                    true, "ImpactPoint", "", "ImpactPoint", new Vector3(0f, 0f, 0.18f), 0.24f, 0.78f);

                loadouts["Monk"] = SaveLoadout(7, "Monk", pGR, pGL, null, 0.05f, 0.12f, 0.12f, 0.15f, 0.11f);
            }

            if (GetWpn("Druid_NatureStaff") != null)
            {
                var pDruid = SaveProfile("WAP_Druid_NatureStaff", "Druid_NatureStaff", GetWpn("Druid_NatureStaff"),
                    WeaponGripType.Staff, "RH_WP", rFallbacks, "", false,
                    Vector3.zero, Vector3.zero, Vector3.one,
                    true, "MagicEmitSocket", "MagicEmitSocket", "MagicEmitSocket", new Vector3(0f, 0f, 1.04f), 0.28f, 0.96f);

                loadouts["Druid"] = SaveLoadout(8, "Druid", pDruid, null, null, 0.12f, 0.12f, 0.18f, 0.14f, 0.16f);
            }

            if (GetWpn("Cleric_HolyHammer") != null && GetWpn("Cleric_RelicShield") != null)
            {
                var pHam = SaveProfile("WAP_Cleric_HolyHammer", "Cleric_HolyHammer", GetWpn("Cleric_HolyHammer"),
                    WeaponGripType.Hammer, "RH_WP", rFallbacks, "", false,
                    Vector3.zero, Vector3.zero, Vector3.one,
                    true, "VfxTrailSocket", "MagicEmitSocket", "ImpactPoint", new Vector3(0f, 0f, 0.84f), 0.30f, 0.96f);

                var pRelic = SaveProfile("WAP_Cleric_RelicShield", "Cleric_RelicShield", GetWpn("Cleric_RelicShield"),
                    WeaponGripType.Shield, "LH_WP", lFallbacks, "Bip001 L Forearm", false,
                    new Vector3(0.02f, 0.03f, 0f), Vector3.zero, Vector3.one,
                    false, "", "", "ShieldFrontCenter", Vector3.zero, 0.2f, 0.76f);

                loadouts["Cleric"] = SaveLoadout(9, "Cleric", pHam, pRelic, null, 0.11f, 0.14f, 0.18f, 0.16f, 0.15f);
            }

            return loadouts;
        }

        /// <summary>
        /// Phase 3 Animation Jitter Fix across all 10 Hero Animator Controllers:
        /// 1. Normalizes extreme Cast/Heavy clip lengths (3.2s-5.3s) so characters never freeze-slide after casting.
        /// 2. Adds smooth locomotion-cancel exit transitions (Speed > 0.18 after active hit frame) with NextState interruption.
        /// 3. Disables self-retrigger jitter on Dodge/Jump/TakeDamage and prevents TakeDamage from canceling attacks/dodges.
        /// </summary>
        public static void UpgradeAllHeroAnimators()
        {
            string[] classes = { "Knight", "Guardian", "Berserker", "Assassin", "Ranger", "Mage", "Warlock", "Monk", "Druid", "Cleric" };
            int upgraded = 0;

            foreach (var cls in classes)
            {
                string path = $"Assets/Resources/Animators/HeroAnimator_{cls}.controller";
                var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                if (ctrl == null || ctrl.layers.Length == 0) continue;

                var sm = ctrl.layers[0].stateMachine;
                AnimatorState locoState = null;
                foreach (var cs in sm.states)
                {
                    if (cs.state.name == "Locomotion") { locoState = cs.state; break; }
                }
                if (locoState == null) continue;

                // Ensure smooth AnyState transition rules (no self-stutter, clean interruption)
                foreach (var t in sm.anyStateTransitions)
                {
                    if (t == null || t.destinationState == null) continue;
                    string dst = t.destinationState.name;
                    t.interruptionSource = TransitionInterruptionSource.Destination;
                    t.orderedInterruption = true;
                    if (dst == "Dodge" || dst == "Jump" || dst == "Death")
                    {
                        t.canTransitionToSelf = false;
                        t.duration = dst == "Dodge" ? 0.05f : 0.08f;
                    }
                    else if (dst == "TakeDamage")
                    {
                        t.canTransitionToSelf = false;
                        t.duration = 0.06f;
                    }
                    else if (dst.StartsWith("Attack") || dst == "HeavyAttack" || dst == "Cast" || dst == "SkillAlt")
                    {
                        t.duration = 0.06f;
                    }
                }

                // Calibrate action state speeds & early locomotion recovery exits
                foreach (var cs in sm.states)
                {
                    var st = cs.state;
                    if (st == null) continue;
                    string sn = st.name;

                    bool isAttack = sn == "Attack" || sn == "Attack2" || sn == "Attack3";
                    bool isHeavy = sn == "HeavyAttack";
                    bool isSkill = sn == "Cast" || sn == "SkillAlt";
                    bool isDodge = sn == "Dodge";

                    if (!isAttack && !isHeavy && !isSkill && !isDodge) continue;

                    float clipLen = (st.motion is AnimationClip ac) ? ac.length : 1.0f;
                    if (isAttack)
                    {
                        // Keep attack cadence punchy (0.72s-1.05s) matched to weapon weight
                        float targetDur = (cls == "Assassin" || cls == "Monk") ? 0.68f
                                        : (cls == "Berserker" || cls == "Guardian") ? 1.02f
                                        : (cls == "Ranger") ? 0.62f : 0.88f;
                        st.speed = Mathf.Clamp(clipLen / targetDur, 0.95f, 1.85f);
                    }
                    else if (isHeavy)
                    {
                        float targetDur = (cls == "Berserker" || cls == "Guardian") ? 1.18f : 0.98f;
                        st.speed = Mathf.Clamp(clipLen / targetDur, 0.95f, 2.60f);
                    }
                    else if (isSkill)
                    {
                        // Clamp ultra-long 3.2s-5.3s cast animations so skills finish briskly (~1.15s) instead of locking locomotion!
                        float targetDur = 1.15f;
                        st.speed = Mathf.Clamp(clipLen / targetDur, 0.95f, 3.20f);
                    }
                    else if (isDodge)
                    {
                        st.speed = Mathf.Clamp(clipLen / 0.42f, 1.0f, 2.2f);
                    }

                    // Rebuild clean exit transitions back to Locomotion:
                    // 1) Early smooth blend into Walk/Run once player moves after active hit window
                    // 2) Full follow-through blend when standing idle
                    var toRemove = st.transitions.Where(tr => tr.destinationState == locoState).ToArray();
                    foreach (var tr in toRemove) st.RemoveTransition(tr);

                    var moveCancel = st.AddTransition(locoState);
                    moveCancel.hasExitTime = true;
                    moveCancel.exitTime = isDodge ? 0.58f : (isAttack ? 0.56f : 0.52f);
                    moveCancel.duration = 0.14f;
                    moveCancel.interruptionSource = TransitionInterruptionSource.Destination;
                    moveCancel.AddCondition(AnimatorConditionMode.Greater, 0.18f, "Speed");

                    var fullExit = st.AddTransition(locoState);
                    fullExit.hasExitTime = true;
                    fullExit.exitTime = isDodge ? 0.78f : 0.86f;
                    fullExit.duration = 0.15f;
                    fullExit.interruptionSource = TransitionInterruptionSource.Destination;
                }

                EditorUtility.SetDirty(ctrl);
                upgraded++;
            }
            Debug.Log($"[GothicWeaponAssetSetup] Upgraded {upgraded} Hero Animator Controllers (smooth early-exit + speed calibration).");
        }

        /// <summary>
        /// Updates Hero_*.prefab in Resources/Models/Heroes, Prefabs/Player, Prefabs/Classes (and legacy Paladin/Vanguard/Kachujin aliases)
        /// for every class that has an authored LoadoutProfile, so Edit Mode and Play Mode show identical bespoke Gothic weapons.
        /// </summary>
        private static void SyncHeroPrefabsWithLoadouts(Dictionary<string, ClassWeaponLoadoutProfile> loadouts)
        {
            foreach (var kv in loadouts)
            {
                string cls = kv.Key;
                var lo = kv.Value;
                string[] prefabPaths = GetHeroPrefabPaths(cls);

                foreach (var pPath in prefabPaths)
                {
                    if (!File.Exists(pPath)) continue;
                    GameObject contents = PrefabUtility.LoadPrefabContents(pPath);
                    if (contents == null) continue;

                    try
                    {
                        Transform rig = contents.transform.Find("Rig") ?? contents.transform;
                        ApplyProfileToPrefabRig(contents.transform, rig, lo.MainHandProfile, "SignatureMainWeapon");
                        ApplyProfileToPrefabRig(contents.transform, rig, lo.OffHandProfile, "SignatureOffhand");

                        var anim = contents.GetComponentInChildren<Animator>();
                        if (anim != null)
                        {
                            anim.applyRootMotion = false;
                            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                        }

                        // Strip any stale missing script references before saving prefab
                        foreach (var tr in contents.GetComponentsInChildren<Transform>(true))
                            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(tr.gameObject);

                        PrefabUtility.SaveAsPrefabAsset(contents, pPath);
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(contents);
                    }
                }
            }
        }

        private static void ApplyProfileToPrefabRig(Transform root, Transform rig, WeaponAttachmentProfile profile, string slotName)
        {
            if (profile == null || profile.WeaponPrefab == null) return;

            Transform targetParent;
            if (profile.IsFloatingRelic)
            {
                targetParent = root;
                slotName = "WarlockFloatingGrimoire";
            }
            else
            {
                targetParent = FindDeep(rig, profile.PrimaryHandBone);
                if (targetParent == null && profile.FallbackHandBones != null)
                {
                    foreach (var fb in profile.FallbackHandBones)
                    {
                        targetParent = FindDeep(rig, fb);
                        if (targetParent != null) break;
                    }
                }
                if (targetParent == null) return;
            }

            // Clear old mounted weapon in this slot
            var old = targetParent.Find(slotName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(profile.WeaponPrefab);
            if (inst == null) inst = UnityEngine.Object.Instantiate(profile.WeaponPrefab);
            inst.name = slotName;
            inst.transform.SetParent(targetParent, false);

            Quaternion rotQ = Quaternion.Euler(profile.LocalRotation);
            Vector3 localGripShift = profile.GripOffset;
            Transform grip = FindDeep(inst.transform, "GripPoint");
            if (grip != null)
                localGripShift -= inst.transform.InverseTransformPoint(grip.position);

            inst.transform.localRotation = rotQ;
            inst.transform.localScale = profile.LocalScale;
            inst.transform.localPosition = profile.LocalPosition + (rotQ * Vector3.Scale(localGripShift, profile.LocalScale));
        }

        private static string[] GetHeroPrefabPaths(string cls)
        {
            var list = new List<string>
            {
                $"Assets/Resources/Models/Heroes/Hero_{cls}.prefab",
                $"Assets/Prefabs/Player/Player_{cls}.prefab",
                $"Assets/Prefabs/Classes/Hero_{cls}.prefab"
            };
            if (cls == "Knight") list.Add("Assets/Resources/Models/Paladin.prefab");
            else if (cls == "Berserker") list.Add("Assets/Resources/Models/Vanguard.prefab");
            else if (cls == "Assassin") list.Add("Assets/Resources/Models/Kachujin.prefab");
            return list.ToArray();
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (string.Equals(all[i].name, name, StringComparison.OrdinalIgnoreCase))
                    return all[i];
            return null;
        }
    }
}
#endif

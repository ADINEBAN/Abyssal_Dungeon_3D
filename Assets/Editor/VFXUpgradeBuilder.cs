#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using AbyssGame;
using AbyssalDungeon.VFX;
using AbyssalDungeon.Classes;

namespace AbyssalDungeon.EditorTools
{
    public static class VFXUpgradeBuilder
    {
        private struct VfxSpec
        {
            public string name;
            public VFXAnimationStyle style;
            public float duration;
            public bool isLoop;
            public Mesh mesh1;
            public Material mat1;
            public Vector3 scale1;
            public Vector3 rotSpeed1;
            public Mesh mesh2;
            public Material mat2;
            public Vector3 scale2;
            public Vector3 rotSpeed2;
            public Material decalMat;
            public float decalSize;
            public float decalSpin;
            public Material partMat;
            public Color partColor;
            public int maxParticles;
            public float gravityMod;
        }

        [MenuItem("Abyssal Dungeon/Upgrade 3D Skill VFX & Bind Characters")]
        public static string RunFullSetup()
        {
            string prefabDir = "Assets/Prefabs/VFX";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            string resPrefabDir = "Assets/Resources/VFX/Prefabs";
            if (!Directory.Exists(resPrefabDir)) Directory.CreateDirectory(resPrefabDir);

            Mesh meshHolyPillar = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/VFX/Meshes/VFX_HolyPillar.fbx");
            Mesh meshEnergyDome = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/VFX/Meshes/VFX_EnergyDome.fbx");
            Mesh meshAngelWings = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/VFX/Meshes/VFX_AngelWings.fbx");
            Mesh meshShockwaveSlam = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/VFX/Meshes/VFX_ShockwaveSlam.fbx");
            Mesh meshEntangleRoots = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/VFX/Meshes/VFX_EntangleRoots.fbx");
            Mesh meshSpiralVortex = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/VFX/Meshes/VFX_SpiralVortex.fbx");

            GameObject tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Mesh quadMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(tempQuad);

            Material mClericFlow = LoadMat("Mat_VFX_Cleric_GoldFlow");
            Material mClericDome = LoadMat("Mat_VFX_Cleric_PureDome");
            Material mClericRune = LoadMat("Mat_VFX_Cleric_Rune");
            Material mClericPart = LoadMat("Mat_VFX_Cleric_Particle");

            Material mKnightSlam = LoadMat("Mat_VFX_Knight_GoldSlam");
            Material mKnightDome = LoadMat("Mat_VFX_Knight_SteelDome");
            Material mKnightCracks = LoadMat("Mat_VFX_Knight_Cracks");
            Material mKnightRune = LoadMat("Mat_VFX_Knight_Rune");
            Material mKnightPart = LoadMat("Mat_VFX_Knight_Particle");

            Material mMageFlow = LoadMat("Mat_VFX_Mage_ArcaneFlow");
            Material mMageFrost = LoadMat("Mat_VFX_Mage_FrostBlue");
            Material mMageRune = LoadMat("Mat_VFX_Mage_Rune");
            Material mMageCracks = LoadMat("Mat_VFX_Mage_Cracks");
            Material mMagePart = LoadMat("Mat_VFX_Mage_Particle");

            Material mRangerFlow = LoadMat("Mat_VFX_Ranger_EmeraldFlow");
            Material mRangerRoots = LoadMat("Mat_VFX_Ranger_NatureRoots");
            Material mRangerRune = LoadMat("Mat_VFX_Ranger_Rune");
            Material mRangerCracks = LoadMat("Mat_VFX_Ranger_Cracks");
            Material mRangerPart = LoadMat("Mat_VFX_Ranger_Particle");

            List<VFXPoolManager.PoolEntry> createdEntries = new List<VFXPoolManager.PoolEntry>();
            Dictionary<string, GameObject> prefabMap = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

            List<VfxSpec> specs = new List<VfxSpec>
            {
                // 1. CLERIC (Arthur)
                new VfxSpec {
                    name = "VFX_Cleric_Smite", style = VFXAnimationStyle.HolyPillar, duration = 1.15f, isLoop = false,
                    mesh1 = meshHolyPillar, mat1 = mClericFlow, scale1 = new Vector3(0.75f, 0.95f, 0.75f), rotSpeed1 = new Vector3(0f, 110f, 0f),
                    mesh2 = meshShockwaveSlam, mat2 = mClericDome, scale2 = new Vector3(0.7f, 0.7f, 0.7f), rotSpeed2 = new Vector3(0f, -90f, 0f),
                    decalMat = mClericRune, decalSize = 2.4f, decalSpin = 45f,
                    partMat = mClericPart, partColor = new Color(1f, 0.92f, 0.55f, 1f), maxParticles = 35, gravityMod = -0.35f
                },
                new VfxSpec {
                    name = "VFX_Cleric_GroupHeal", style = VFXAnimationStyle.HolyPillar, duration = 1.45f, isLoop = false,
                    mesh1 = meshHolyPillar, mat1 = mClericFlow, scale1 = new Vector3(1.25f, 1.0f, 1.25f), rotSpeed1 = new Vector3(0f, 75f, 0f),
                    mesh2 = meshEnergyDome, mat2 = mClericDome, scale2 = new Vector3(1.1f, 0.95f, 1.1f), rotSpeed2 = new Vector3(0f, -95f, 0f),
                    decalMat = mClericRune, decalSize = 3.8f, decalSpin = 32f,
                    partMat = mClericPart, partColor = new Color(1f, 0.96f, 0.68f, 1f), maxParticles = 40, gravityMod = -0.45f
                },
                new VfxSpec {
                    name = "VFX_Cleric_Purify", style = VFXAnimationStyle.EnergyDome, duration = 1.35f, isLoop = false,
                    mesh1 = meshEnergyDome, mat1 = mClericDome, scale1 = new Vector3(1.0f, 1.0f, 1.0f), rotSpeed1 = new Vector3(0f, 70f, 0f),
                    mesh2 = meshSpiralVortex, mat2 = mClericFlow, scale2 = new Vector3(1.15f, 0.75f, 1.15f), rotSpeed2 = new Vector3(0f, -145f, 0f),
                    decalMat = mClericRune, decalSize = 2.8f, decalSpin = -40f,
                    partMat = mClericPart, partColor = new Color(1f, 0.98f, 0.82f, 1f), maxParticles = 35, gravityMod = -0.30f
                },
                new VfxSpec {
                    name = "VFX_Cleric_Resurrection", style = VFXAnimationStyle.AngelWings, duration = 1.85f, isLoop = false,
                    mesh1 = meshAngelWings, mat1 = mClericFlow, scale1 = new Vector3(1.35f, 1.35f, 1.35f), rotSpeed1 = Vector3.zero,
                    mesh2 = meshHolyPillar, mat2 = mClericDome, scale2 = new Vector3(1.1f, 1.1f, 1.1f), rotSpeed2 = new Vector3(0f, 90f, 0f),
                    decalMat = mClericRune, decalSize = 4.2f, decalSpin = 50f,
                    partMat = mClericPart, partColor = new Color(1f, 0.94f, 0.62f, 1f), maxParticles = 45, gravityMod = -0.50f
                },

                // 2. KNIGHT (Lyra)
                new VfxSpec {
                    name = "VFX_Knight_ShieldBash", style = VFXAnimationStyle.ShockwaveSlam, duration = 0.95f, isLoop = false,
                    mesh1 = meshShockwaveSlam, mat1 = mKnightSlam, scale1 = new Vector3(0.95f, 1.0f, 0.95f), rotSpeed1 = new Vector3(0f, 120f, 0f),
                    mesh2 = null, mat2 = null, scale2 = Vector3.one, rotSpeed2 = Vector3.zero,
                    decalMat = mKnightCracks, decalSize = 2.6f, decalSpin = 0f,
                    partMat = mKnightPart, partColor = new Color(1f, 0.76f, 0.32f, 1f), maxParticles = 30, gravityMod = 0.35f
                },
                new VfxSpec {
                    name = "VFX_Knight_IronGuard", style = VFXAnimationStyle.EnergyDome, duration = 1.45f, isLoop = false,
                    mesh1 = meshEnergyDome, mat1 = mKnightDome, scale1 = new Vector3(1.05f, 1.05f, 1.05f), rotSpeed1 = new Vector3(0f, 65f, 0f),
                    mesh2 = meshSpiralVortex, mat2 = mKnightSlam, scale2 = new Vector3(1.2f, 0.75f, 1.2f), rotSpeed2 = new Vector3(0f, -135f, 0f),
                    decalMat = mKnightRune, decalSize = 2.8f, decalSpin = 35f,
                    partMat = mKnightPart, partColor = new Color(0.55f, 0.85f, 1.0f, 1f), maxParticles = 35, gravityMod = -0.20f
                },
                new VfxSpec {
                    name = "VFX_Knight_Taunt", style = VFXAnimationStyle.ShockwaveSlam, duration = 1.20f, isLoop = false,
                    mesh1 = meshShockwaveSlam, mat1 = mKnightSlam, scale1 = new Vector3(1.25f, 1.0f, 1.25f), rotSpeed1 = new Vector3(0f, 95f, 0f),
                    mesh2 = meshEnergyDome, mat2 = mKnightDome, scale2 = new Vector3(0.95f, 0.95f, 0.95f), rotSpeed2 = new Vector3(0f, -110f, 0f),
                    decalMat = mKnightRune, decalSize = 3.6f, decalSpin = -45f,
                    partMat = mKnightPart, partColor = new Color(1f, 0.68f, 0.25f, 1f), maxParticles = 35, gravityMod = 0.15f
                },
                new VfxSpec {
                    name = "VFX_Knight_Judgement", style = VFXAnimationStyle.ShockwaveSlam, duration = 1.55f, isLoop = false,
                    mesh1 = meshShockwaveSlam, mat1 = mKnightSlam, scale1 = new Vector3(1.45f, 1.25f, 1.45f), rotSpeed1 = new Vector3(0f, 110f, 0f),
                    mesh2 = meshHolyPillar, mat2 = mKnightDome, scale2 = new Vector3(0.95f, 0.95f, 0.95f), rotSpeed2 = new Vector3(0f, -130f, 0f),
                    decalMat = mKnightCracks, decalSize = 4.0f, decalSpin = 0f,
                    partMat = mKnightPart, partColor = new Color(1f, 0.78f, 0.35f, 1f), maxParticles = 45, gravityMod = 0.40f
                },

                // 3. MAGE (Kael)
                new VfxSpec {
                    name = "VFX_Mage_Fireball", style = VFXAnimationStyle.SpiralVortex, duration = 1.10f, isLoop = false,
                    mesh1 = meshSpiralVortex, mat1 = mMageFlow, scale1 = new Vector3(1.15f, 0.95f, 1.15f), rotSpeed1 = new Vector3(0f, 220f, 0f),
                    mesh2 = meshShockwaveSlam, mat2 = mMageFrost, scale2 = new Vector3(0.85f, 0.85f, 0.85f), rotSpeed2 = new Vector3(0f, -150f, 0f),
                    decalMat = mMageCracks, decalSize = 3.0f, decalSpin = 0f,
                    partMat = mMagePart, partColor = new Color(0.68f, 0.38f, 1.0f, 1f), maxParticles = 35, gravityMod = -0.25f
                },
                new VfxSpec {
                    name = "VFX_Mage_FrostNova", style = VFXAnimationStyle.ShockwaveSlam, duration = 1.30f, isLoop = false,
                    mesh1 = meshShockwaveSlam, mat1 = mMageFrost, scale1 = new Vector3(1.30f, 1.05f, 1.30f), rotSpeed1 = new Vector3(0f, 140f, 0f),
                    mesh2 = meshEnergyDome, mat2 = mMageFlow, scale2 = new Vector3(0.95f, 0.95f, 0.95f), rotSpeed2 = new Vector3(0f, -120f, 0f),
                    decalMat = mMageRune, decalSize = 3.6f, decalSpin = 55f,
                    partMat = mMagePart, partColor = new Color(0.35f, 0.82f, 1.0f, 1f), maxParticles = 40, gravityMod = -0.15f
                },
                new VfxSpec {
                    name = "VFX_Mage_ArcanePulse", style = VFXAnimationStyle.SpiralVortex, duration = 1.05f, isLoop = false,
                    mesh1 = meshSpiralVortex, mat1 = mMageFlow, scale1 = new Vector3(1.0f, 1.0f, 1.0f), rotSpeed1 = new Vector3(0f, 260f, 0f),
                    mesh2 = null, mat2 = null, scale2 = Vector3.one, rotSpeed2 = Vector3.zero,
                    decalMat = mMageRune, decalSize = 2.5f, decalSpin = -65f,
                    partMat = mMagePart, partColor = new Color(0.62f, 0.42f, 1.0f, 1f), maxParticles = 35, gravityMod = -0.30f
                },
                new VfxSpec {
                    name = "VFX_Mage_Meteor", style = VFXAnimationStyle.HolyPillar, duration = 1.65f, isLoop = false,
                    mesh1 = meshHolyPillar, mat1 = mMageFlow, scale1 = new Vector3(1.30f, 1.10f, 1.30f), rotSpeed1 = new Vector3(0f, 160f, 0f),
                    mesh2 = meshShockwaveSlam, mat2 = mMageFrost, scale2 = new Vector3(1.45f, 1.15f, 1.45f), rotSpeed2 = new Vector3(0f, -140f, 0f),
                    decalMat = mMageCracks, decalSize = 4.4f, decalSpin = 0f,
                    partMat = mMagePart, partColor = new Color(0.55f, 0.45f, 1.0f, 1f), maxParticles = 45, gravityMod = 0.25f
                },

                // 4. RANGER (Seraphina)
                new VfxSpec {
                    name = "VFX_Ranger_PiercingShot", style = VFXAnimationStyle.SpiralVortex, duration = 0.95f, isLoop = false,
                    mesh1 = meshSpiralVortex, mat1 = mRangerFlow, scale1 = new Vector3(0.95f, 0.95f, 0.95f), rotSpeed1 = new Vector3(0f, 280f, 0f),
                    mesh2 = null, mat2 = null, scale2 = Vector3.one, rotSpeed2 = Vector3.zero,
                    decalMat = mRangerRune, decalSize = 2.3f, decalSpin = 60f,
                    partMat = mRangerPart, partColor = new Color(0.28f, 0.96f, 0.58f, 1f), maxParticles = 30, gravityMod = -0.20f
                },
                new VfxSpec {
                    name = "VFX_Ranger_FrostTrap", style = VFXAnimationStyle.EntangleRoots, duration = 1.45f, isLoop = false,
                    mesh1 = meshEntangleRoots, mat1 = mRangerRoots, scale1 = new Vector3(1.10f, 1.15f, 1.10f), rotSpeed1 = new Vector3(0f, 25f, 0f),
                    mesh2 = meshSpiralVortex, mat2 = mRangerFlow, scale2 = new Vector3(0.85f, 0.65f, 0.85f), rotSpeed2 = new Vector3(0f, 160f, 0f),
                    decalMat = mRangerCracks, decalSize = 2.8f, decalSpin = 0f,
                    partMat = mRangerPart, partColor = new Color(0.35f, 0.98f, 0.65f, 1f), maxParticles = 35, gravityMod = -0.25f
                },
                new VfxSpec {
                    name = "VFX_Ranger_Volley", style = VFXAnimationStyle.SpiralVortex, duration = 1.15f, isLoop = false,
                    mesh1 = meshSpiralVortex, mat1 = mRangerFlow, scale1 = new Vector3(1.20f, 0.95f, 1.20f), rotSpeed1 = new Vector3(0f, 240f, 0f),
                    mesh2 = meshEntangleRoots, mat2 = mRangerRoots, scale2 = new Vector3(0.85f, 0.80f, 0.85f), rotSpeed2 = new Vector3(0f, -90f, 0f),
                    decalMat = mRangerRune, decalSize = 3.0f, decalSpin = -55f,
                    partMat = mRangerPart, partColor = new Color(0.32f, 0.95f, 0.60f, 1f), maxParticles = 35, gravityMod = -0.30f
                },
                new VfxSpec {
                    name = "VFX_Ranger_ArrowStorm", style = VFXAnimationStyle.HolyPillar, duration = 1.65f, isLoop = false,
                    mesh1 = meshHolyPillar, mat1 = mRangerFlow, scale1 = new Vector3(1.25f, 1.05f, 1.25f), rotSpeed1 = new Vector3(0f, 190f, 0f),
                    mesh2 = meshEntangleRoots, mat2 = mRangerRoots, scale2 = new Vector3(1.30f, 1.15f, 1.30f), rotSpeed2 = new Vector3(0f, -80f, 0f),
                    decalMat = mRangerRune, decalSize = 4.2f, decalSpin = 70f,
                    partMat = mRangerPart, partColor = new Color(0.35f, 1.0f, 0.68f, 1f), maxParticles = 45, gravityMod = -0.35f
                },

                // 5. SHARED PROJECTILES, TELEGRAPH & DASH
                new VfxSpec {
                    name = "VFX_Projectile_Cleric", style = VFXAnimationStyle.ProjectileLoop, duration = 2.5f, isLoop = true,
                    mesh1 = meshSpiralVortex, mat1 = mClericFlow, scale1 = new Vector3(0.42f, 0.42f, 0.42f), rotSpeed1 = new Vector3(0f, 320f, 0f),
                    mesh2 = null, mat2 = null, scale2 = Vector3.one, rotSpeed2 = Vector3.zero,
                    decalMat = mClericRune, decalSize = 0.85f, decalSpin = 90f,
                    partMat = mClericPart, partColor = new Color(1f, 0.92f, 0.55f, 1f), maxParticles = 30, gravityMod = 0f
                },
                new VfxSpec {
                    name = "VFX_Projectile_Knight", style = VFXAnimationStyle.ProjectileLoop, duration = 2.5f, isLoop = true,
                    mesh1 = meshSpiralVortex, mat1 = mKnightSlam, scale1 = new Vector3(0.42f, 0.42f, 0.42f), rotSpeed1 = new Vector3(0f, 320f, 0f),
                    mesh2 = null, mat2 = null, scale2 = Vector3.one, rotSpeed2 = Vector3.zero,
                    decalMat = mKnightRune, decalSize = 0.85f, decalSpin = 90f,
                    partMat = mKnightPart, partColor = new Color(1f, 0.75f, 0.32f, 1f), maxParticles = 30, gravityMod = 0f
                },
                new VfxSpec {
                    name = "VFX_Projectile_Mage", style = VFXAnimationStyle.ProjectileLoop, duration = 2.5f, isLoop = true,
                    mesh1 = meshSpiralVortex, mat1 = mMageFlow, scale1 = new Vector3(0.45f, 0.45f, 0.45f), rotSpeed1 = new Vector3(0f, 340f, 0f),
                    mesh2 = null, mat2 = null, scale2 = Vector3.one, rotSpeed2 = Vector3.zero,
                    decalMat = mMageRune, decalSize = 0.90f, decalSpin = -110f,
                    partMat = mMagePart, partColor = new Color(0.65f, 0.40f, 1.0f, 1f), maxParticles = 30, gravityMod = 0f
                },
                new VfxSpec {
                    name = "VFX_Projectile_Ranger", style = VFXAnimationStyle.ProjectileLoop, duration = 2.5f, isLoop = true,
                    mesh1 = meshSpiralVortex, mat1 = mRangerFlow, scale1 = new Vector3(0.42f, 0.42f, 0.42f), rotSpeed1 = new Vector3(0f, 360f, 0f),
                    mesh2 = null, mat2 = null, scale2 = Vector3.one, rotSpeed2 = Vector3.zero,
                    decalMat = mRangerRune, decalSize = 0.85f, decalSpin = 120f,
                    partMat = mRangerPart, partColor = new Color(0.28f, 0.96f, 0.58f, 1f), maxParticles = 30, gravityMod = 0f
                },
                new VfxSpec {
                    name = "VFX_GroundTelegraph_Rune", style = VFXAnimationStyle.GroundTelegraph, duration = 1.2f, isLoop = false,
                    mesh1 = meshHolyPillar, mat1 = mMageFrost, scale1 = new Vector3(0.95f, 0.18f, 0.95f), rotSpeed1 = new Vector3(0f, 65f, 0f),
                    mesh2 = null, mat2 = null, scale2 = Vector3.one, rotSpeed2 = Vector3.zero,
                    decalMat = mMageRune, decalSize = 3.0f, decalSpin = 45f,
                    partMat = mMagePart, partColor = new Color(0.45f, 0.80f, 1.0f, 1f), maxParticles = 30, gravityMod = -0.20f
                },
                new VfxSpec {
                    name = "VFX_Common_SpiralDash", style = VFXAnimationStyle.SpiralVortex, duration = 0.65f, isLoop = false,
                    mesh1 = meshSpiralVortex, mat1 = mRangerFlow, scale1 = new Vector3(0.85f, 0.75f, 0.85f), rotSpeed1 = new Vector3(0f, 320f, 0f),
                    mesh2 = null, mat2 = null, scale2 = Vector3.one, rotSpeed2 = Vector3.zero,
                    decalMat = mRangerRune, decalSize = 1.8f, decalSpin = 90f,
                    partMat = mRangerPart, partColor = new Color(0.55f, 0.98f, 0.78f, 1f), maxParticles = 30, gravityMod = -0.15f
                }
            };

            for (int i = 0; i < specs.Count; i++)
            {
                GameObject saved = BuildSinglePrefab(specs[i], quadMesh, prefabDir, resPrefabDir);
                prefabMap[specs[i].name] = saved;
                createdEntries.Add(new VFXPoolManager.PoolEntry
                {
                    key = specs[i].name,
                    prefab = saved,
                    prewarmCount = 2,
                    maxCapacity = 12
                });
            }

            // Create VFXPoolManager Prefab
            GameObject poolMgrGo = new GameObject("VFXPoolManager");
            VFXPoolManager mgr = poolMgrGo.AddComponent<VFXPoolManager>();
            mgr.poolEntries = createdEntries;
            PrefabUtility.SaveAsPrefabAsset(poolMgrGo, prefabDir + "/VFXPoolManager.prefab");
            PrefabUtility.SaveAsPrefabAsset(poolMgrGo, resPrefabDir + "/VFXPoolManager.prefab");
            UnityEngine.Object.DestroyImmediate(poolMgrGo);

            // Bind Character Prefabs, ClassDefinitionAssets, and SkillData/ClassData
            int boundPrefabs = BindAllCharacterPrefabs(prefabMap);
            int boundClassAssets = BindAllClassAssets(prefabMap);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return string.Format("Built {0} 3-layer VFX Prefabs, bound {1} character prefabs, and updated {2} class assets.",
                createdEntries.Count, boundPrefabs, boundClassAssets);
        }

        private static Material LoadMat(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/VFX/Materials/" + name + ".mat");
        }

        private static GameObject BuildSinglePrefab(VfxSpec s, Mesh quadMesh, string prefabDir, string resPrefabDir)
        {
            GameObject root = new GameObject(s.name);
            VFXLayerController ctrl = root.AddComponent<VFXLayerController>();
            ctrl.poolKey = s.name;
            ctrl.animationStyle = s.style;
            ctrl.duration = s.duration;
            ctrl.isLooping = s.isLoop;

            // Layer 1 Primary 3D Mesh
            if (s.mesh1 != null && s.mat1 != null)
            {
                GameObject l1 = new GameObject("Layer1_3DMesh_Primary");
                l1.transform.SetParent(root.transform, false);
                l1.transform.localScale = s.scale1;
                MeshFilter mf1 = l1.AddComponent<MeshFilter>();
                mf1.sharedMesh = s.mesh1;
                MeshRenderer mr1 = l1.AddComponent<MeshRenderer>();
                mr1.sharedMaterial = s.mat1;
                mr1.shadowCastingMode = ShadowCastingMode.Off;
                mr1.receiveShadows = false;

                ctrl.layer1PrimaryMesh = l1.transform;
                ctrl.primaryBaseScale = s.scale1;
                ctrl.primaryRotationSpeed = s.rotSpeed1;
            }

            // Layer 1 Secondary 3D Mesh
            if (s.mesh2 != null && s.mat2 != null)
            {
                GameObject l1b = new GameObject("Layer1_3DMesh_Secondary");
                l1b.transform.SetParent(root.transform, false);
                l1b.transform.localScale = s.scale2;
                MeshFilter mf2 = l1b.AddComponent<MeshFilter>();
                mf2.sharedMesh = s.mesh2;
                MeshRenderer mr2 = l1b.AddComponent<MeshRenderer>();
                mr2.sharedMaterial = s.mat2;
                mr2.shadowCastingMode = ShadowCastingMode.Off;
                mr2.receiveShadows = false;

                ctrl.layer1SecondaryMesh = l1b.transform;
                ctrl.secondaryBaseScale = s.scale2;
                ctrl.secondaryRotationSpeed = s.rotSpeed2;
            }

            // Layer 2: Ground Rune / Cracks Quad Decal
            if (s.decalMat != null)
            {
                GameObject l2 = new GameObject("Layer2_GroundDecal");
                l2.transform.SetParent(root.transform, false);
                l2.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                l2.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Vector3 dScale = new Vector3(s.decalSize, s.decalSize, 1f);
                l2.transform.localScale = dScale;

                MeshFilter mfd = l2.AddComponent<MeshFilter>();
                mfd.sharedMesh = quadMesh;
                MeshRenderer mrd = l2.AddComponent<MeshRenderer>();
                mrd.sharedMaterial = s.decalMat;
                mrd.shadowCastingMode = ShadowCastingMode.Off;
                mrd.receiveShadows = false;

                ctrl.layer2DecalTransform = l2.transform;
                ctrl.decalBaseScale = dScale;
                ctrl.decalSpinSpeed = s.decalSpin;
            }

            // Layer 3: Lightweight Auxiliary ParticleSystem (strictly capped 30-45 particles)
            GameObject l3 = new GameObject("Layer3_Particles");
            l3.transform.SetParent(root.transform, false);
            l3.transform.localPosition = new Vector3(0f, 0.15f, 0f);

            ParticleSystem ps = l3.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psr = l3.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = s.partMat;
            psr.shadowCastingMode = ShadowCastingMode.Off;
            psr.receiveShadows = false;
            psr.renderMode = ParticleSystemRenderMode.Billboard;

            int clampedMax = Mathf.Clamp(s.maxParticles, 30, 45);
            var main = ps.main;
            main.loop = s.isLoop;
            main.playOnAwake = false;
            main.duration = Mathf.Max(0.6f, s.duration * 0.8f);
            main.maxParticles = clampedMax;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.90f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 3.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startColor = s.partColor;
            main.gravityModifier = s.gravityMod;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = s.isLoop ? 22f : 14f;
            short burstCount = (short)Mathf.Clamp(clampedMax - 12, 16, 28);
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0.0f, burstCount)
            });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 28f;
            shape.radius = Mathf.Max(0.4f, s.decalSize * 0.32f);

            var colLife = ps.colorOverLifetime;
            colLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(s.partColor, 0.5f),
                    new GradientColorKey(s.partColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(0.85f, 0.65f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colLife.color = new ParticleSystem.MinMaxGradient(grad);

            var sizeLife = ps.sizeOverLifetime;
            sizeLife.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.45f),
                new Keyframe(0.25f, 1.0f),
                new Keyframe(1f, 0.0f)
            );
            sizeLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            ctrl.layer3Particles = ps;

            string assetPath1 = prefabDir + "/" + s.name + ".prefab";
            string assetPath2 = resPrefabDir + "/" + s.name + ".prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, assetPath1);
            PrefabUtility.SaveAsPrefabAsset(root, assetPath2);
            UnityEngine.Object.DestroyImmediate(root);
            return savedPrefab;
        }

        private static int BindAllCharacterPrefabs(Dictionary<string, GameObject> prefabMap)
        {
            string[] folders = new string[] {
                "Assets/Prefabs/Classes",
                "Assets/Prefabs/Player",
                "Assets/Resources/Models/Heroes"
            };

            int count = 0;
            for (int f = 0; f < folders.Length; f++)
            {
                if (!Directory.Exists(folders[f])) continue;
                string[] files = Directory.GetFiles(folders[f], "*.prefab", SearchOption.TopDirectoryOnly);
                for (int i = 0; i < files.Length; i++)
                {
                    string assetPath = files[i].Replace("\\", "/");
                    GameObject root = PrefabUtility.LoadPrefabContents(assetPath);
                    if (root == null) continue;

                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
                    int classId = InferClassIdFromName(root.name, out string heroDisplayName, out string className);
                    CharacterSkillVFX binding = root.GetComponent<CharacterSkillVFX>();
                    if (binding == null) binding = root.AddComponent<CharacterSkillVFX>();

                    binding.heroDisplayName = heroDisplayName;
                    binding.className = className;
                    binding.classId = classId;

                    ClassDef def = (GameDB.Classes != null && classId >= 0 && classId < GameDB.Classes.Length)
                        ? GameDB.Classes[classId]
                        : null;

                    string[] slotLabels = { "Skill Q", "Skill W", "Skill E", "Skill R (Ultimate)" };
                    binding.skillSlots = new SkillVfxSlot[4];
                    for (int s = 0; s < 4; s++)
                    {
                        string key = VFXPoolManager.GetSkillVfxKey(classId, s);
                        prefabMap.TryGetValue(key, out GameObject vfxPrefab);
                        string skName = (def != null && def.skills != null && s < def.skills.Length) ? def.skills[s].name : slotLabels[s];
                        binding.skillSlots[s] = new SkillVfxSlot
                        {
                            slotLabel = slotLabels[s],
                            skillName = skName,
                            vfxPoolKey = key,
                            vfxPrefab = vfxPrefab,
                            defaultScale = 1.0f,
                            defaultDuration = (s == 3) ? 1.65f : 1.25f
                        };
                    }

                    string projKey = VFXPoolManager.GetProjectileVfxKey(classId);
                    prefabMap.TryGetValue(projKey, out binding.projectileVfxPrefab);
                    binding.projectileVfxKey = projKey;

                    prefabMap.TryGetValue("VFX_Common_SpiralDash", out binding.dashVfxPrefab);
                    binding.dashVfxKey = "VFX_Common_SpiralDash";

                    PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                    PrefabUtility.UnloadPrefabContents(root);
                    count++;
                }
            }
            return count;
        }

        private static int BindAllClassAssets(Dictionary<string, GameObject> prefabMap)
        {
            int count = 0;

            // 1. Update ClassDefinitionAsset in Resources/Classes & ScriptableObjects/Classes
            string[] cdGuids = AssetDatabase.FindAssets("t:ClassDefinitionAsset");
            for (int i = 0; i < cdGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(cdGuids[i]);
                ClassDefinitionAsset cd = AssetDatabase.LoadAssetAtPath<ClassDefinitionAsset>(path);
                if (cd == null) continue;

                if (cd.skills != null)
                {
                    for (int s = 0; s < cd.skills.Length; s++)
                    {
                        string key = VFXPoolManager.GetSkillVfxKey(cd.classId, s);
                        cd.skills[s].vfxPoolKey = key;
                        if (prefabMap.TryGetValue(key, out GameObject p))
                        {
                            cd.skills[s].vfxPrefab = p;
                        }
                    }
                }
                string projKey = VFXPoolManager.GetProjectileVfxKey(cd.classId);
                prefabMap.TryGetValue(projKey, out cd.projectileVfxPrefab);
                prefabMap.TryGetValue("VFX_Common_SpiralDash", out cd.dashVfxPrefab);

                EditorUtility.SetDirty(cd);
                count++;
            }

            // 2. Populate SkillData & ClassData in Assets/ScriptableObjects/Classes & Assets/ScriptableObjects/Skills
            string skillDir = "Assets/ScriptableObjects/Skills";
            if (!Directory.Exists(skillDir)) Directory.CreateDirectory(skillDir);

            string[] classDataGuids = AssetDatabase.FindAssets("t:ClassData");
            for (int i = 0; i < classDataGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(classDataGuids[i]);
                ClassData cData = AssetDatabase.LoadAssetAtPath<ClassData>(path);
                if (cData == null) continue;

                int classId = InferClassIdFromName(cData.name, out string _, out string cName);
                ClassDef def = (GameDB.Classes != null && classId >= 0 && classId < GameDB.Classes.Length)
                    ? GameDB.Classes[classId]
                    : null;

                SkillData[] skillAssets = new SkillData[4];
                for (int s = 0; s < 4; s++)
                {
                    string key = VFXPoolManager.GetSkillVfxKey(classId, s);
                    prefabMap.TryGetValue(key, out GameObject vfxPrefab);
                    string skAssetPath = string.Format("{0}/SD_{1}_Skill{2}.asset", skillDir, cData.name, s + 1);
                    SkillData sd = AssetDatabase.LoadAssetAtPath<SkillData>(skAssetPath);
                    if (sd == null)
                    {
                        sd = ScriptableObject.CreateInstance<SkillData>();
                        AssetDatabase.CreateAsset(sd, skAssetPath);
                    }
                    if (def != null && def.skills != null && s < def.skills.Length)
                    {
                        sd.skillName = def.skills[s].name;
                        sd.description = def.skills[s].desc;
                        sd.cooldown = def.skills[s].cooldown;
                        sd.manaCost = def.skills[s].mana;
                        sd.damageMultiplier = def.skills[s].power;
                        sd.range = def.skills[s].range;
                    }
                    else
                    {
                        sd.skillName = key;
                    }
                    sd.vfxPrefab = vfxPrefab;
                    EditorUtility.SetDirty(sd);
                    skillAssets[s] = sd;
                }
                cData.skills = skillAssets;
                EditorUtility.SetDirty(cData);
                count++;
            }

            return count;
        }

        private static int InferClassIdFromName(string objName, out string heroDisplayName, out string className)
        {
            string n = objName != null ? objName.ToLowerInvariant() : "";
            if (n.Contains("cleric")) { heroDisplayName = "Arthur"; className = "Cleric"; return 9; }
            if (n.Contains("knight") && !n.Contains("shadow")) { heroDisplayName = "Lyra"; className = "Knight"; return 0; }
            if (n.Contains("mage")) { heroDisplayName = "Kael"; className = "Mage"; return 5; }
            if (n.Contains("ranger")) { heroDisplayName = "Seraphina"; className = "Ranger"; return 4; }
            if (n.Contains("paladin")) { heroDisplayName = "Arthur (Paladin)"; className = "Paladin"; return 2; }
            if (n.Contains("berserker")) { heroDisplayName = "Lyra (Berserker)"; className = "Berserker"; return 1; }
            if (n.Contains("assassin")) { heroDisplayName = "Seraphina (Assassin)"; className = "Assassin"; return 3; }
            if (n.Contains("necromancer")) { heroDisplayName = "Kael (Necromancer)"; className = "Necromancer"; return 6; }
            if (n.Contains("elementalist")) { heroDisplayName = "Kael (Elementalist)"; className = "Elementalist"; return 7; }
            if (n.Contains("monk")) { heroDisplayName = "Lyra (Monk)"; className = "Monk"; return 8; }
            if (n.Contains("bard")) { heroDisplayName = "Kael (Bard)"; className = "Bard"; return 10; }
            if (n.Contains("shadow")) { heroDisplayName = "Lyra (ShadowKnight)"; className = "ShadowKnight"; return 11; }
            heroDisplayName = "Arthur";
            className = "Cleric";
            return 9;
        }
    }
}
#endif

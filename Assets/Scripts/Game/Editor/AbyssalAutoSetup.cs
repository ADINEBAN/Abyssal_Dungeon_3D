#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace AbyssGame.EditorTools
{
    /// <summary>
    /// One-click & automatic Editor setup for Abyssal Dungeon 3D using authentic assets extracted from ExportedProject:
    /// 1. Validates normal maps, UI sprites, and PBR textures in Assets/Art/Exported & Assets/Resources.
    /// 2. Builds 10 Class-specific Hero AnimatorControllers + 13 Monster/Summon/Beast/Boss AnimatorControllers.
    /// 3. Assembles 10 Hero Class Prefabs (with authentic 3D armor rigs & signature 3D weapons on RH_WP/LH_WP),
    ///    Druid Beast Form prefab, Warlock SoulMinion prefab, 10 Monster prefabs, and Abyssal Knight Boss prefab.
    /// 4. Builds 28 normalized Modular 3D Environment, Interactive Prop & Loot Prefabs in Resources/EnvironmentKit.
    /// 5. Generates Biome & Class ScriptableObjects and all 7 playable scenes in Build Settings.
    /// </summary>
    [InitializeOnLoad]
    public static class AbyssalAutoSetup
    {
        private const string ReadyMarker = "Assets/Resources/Animators/HeroAnimator_Knight.controller";
        private const string HeroPrefabMarker = "Assets/Resources/Models/Heroes/Hero_Knight.prefab";
        private const string BossPrefabMarker = "Assets/Resources/Models/Enemies/Boss_AbyssalKnight.prefab";
        private const string Phase1PrefabMarker = "Assets/Resources/EnvironmentKit/GothicPillar.prefab";
        private const string Phase4ClassMarker = "Assets/Resources/Classes/CD_Knight.asset";
        private const string PbrV3Marker = "Assets/Resources/Models/ExportedPbrV3FaceMarker.txt";
        private const string GothicUIV4Marker = "Assets/Resources/UI/GothicUIV4Marker.txt";

        static AbyssalAutoSetup()
        {
            EditorApplication.delayCall += AutoRunIfNeeded;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += AutoRunIfNeeded;
            }
        }

        private static void AutoRunIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SessionState.GetBool("AbyssalAutoSetup_GothicUI_v4", false) && File.Exists(PbrV3Marker) && File.Exists(GothicUIV4Marker)) return;
            SessionState.SetBool("AbyssalAutoSetup_GothicUI_v4", true);

            bool hasPbrV3 = File.Exists(PbrV3Marker);
            bool hasGothicUIV4 = File.Exists(GothicUIV4Marker);
            bool hasAnimator = AssetDatabase.LoadAssetAtPath<AnimatorController>(ReadyMarker) != null;
            bool hasHero = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabMarker) != null;
            bool hasBoss = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabMarker) != null;
            bool hasEnvKit = AssetDatabase.LoadAssetAtPath<GameObject>(Phase1PrefabMarker) != null;
            bool hasMenuScene = File.Exists("Assets/Scenes/MainMenu.unity");

            if (!hasPbrV3 || !hasAnimator || !hasHero || !hasBoss || !hasEnvKit || !hasMenuScene)
            {
                Debug.Log("<color=cyan>[Abyssal Dungeon 3D] Running automatic ExportedProject PBR v3 + Ancient Gothic UI v4 setup...</color>");
                SetupCompleteGame();
                return;
            }

            if (!hasGothicUIV4)
            {
                EnsureFolders();
                ConfigureTextureImporters();
                BuildGothicUIPrefabs();
                File.WriteAllText(GothicUIV4Marker, "Ancient Gothic UI v4 Ready\n");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (AssetDatabase.LoadAssetAtPath<ClassDefinitionAsset>(Phase4ClassMarker) == null)
            {
                SetupPhase4ClassesAndPrefabs();
            }
            else
            {
                EnsureBuildSettings();
            }
        }

        [MenuItem("Tools/Abyssal Dungeon/4. Rebuild All Hero, Enemy & Boss Animators + Prefabs")]
        public static void UpgradePhase2AnimatorAndPrefabs()
        {
            EnsureFolders();
            ModelImportSetup.ConfigureAll();
            var heroCtrls = BuildAllHeroAnimatorControllers();
            var enemyCtrls = BuildAllEnemyAnimatorControllers();
            CreateAllCharacterAndEnemyPrefabs(heroCtrls, enemyCtrls);
            File.WriteAllText(PbrV3Marker, "ExportedProject PBR Materials + 3D Faces v3\n");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[Abyssal Dungeon 3D] All 10 Hero Classes + 13 Enemy/Summon/Beast/Boss AnimatorControllers & 3D Prefabs rebuilt!</color>");
        }

        [MenuItem("Tools/Abyssal Dungeon/Setup Complete Game (One-Click)")]
        public static void SetupCompleteGame()
        {
            try
            {
                EnsureFolders();
                ModelImportSetup.ConfigureAll();
                ConfigureTextureImporters();
                BuildPersistentPbrMaterials();

                var heroCtrls = BuildAllHeroAnimatorControllers();
                var enemyCtrls = BuildAllEnemyAnimatorControllers();
                CreateAllCharacterAndEnemyPrefabs(heroCtrls, enemyCtrls);

                SetupPhase1EnvironmentAssets(recreateAllScenes: true);
                SetupPhase4ClassesAndPrefabs();
                BuildGothicUIPrefabs();

                File.WriteAllText(PbrV3Marker, "ExportedProject PBR Materials + 3D Faces v3\n");
                File.WriteAllText(GothicUIV4Marker, "Ancient Gothic UI v4 Ready\n");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (!Application.isBatchMode)
                {
                    string cur = SceneManager.GetActiveScene().name;
                    if ((string.IsNullOrEmpty(cur) || cur == "SampleScene") && File.Exists("Assets/Scenes/MainMenu.unity"))
                    {
                        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
                    }
                }

                Debug.Log("<color=green><b>[Abyssal Dungeon 3D] Complete ExportedProject Asset Setup Finished! Press Play on MainMenu to start!</b></color>");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Tools/Abyssal Dungeon/3. Setup Phase 1 3D Environment & Biomes")]
        public static void SetupPhase1Menu()
        {
            SetupPhase1EnvironmentAssets(recreateAllScenes: false);
        }

        public static void SetupPhase1EnvironmentAssets(bool recreateAllScenes = false)
        {
            EnsureFolders();
            ConfigureTextureImporters();
            BuildPersistentPbrMaterials();
            BuildModularEnvironmentPrefabs();
            BuildBiomeScriptableObjects();

            if (recreateAllScenes)
            {
                CreateAllGameScenes();
            }
            else if (!File.Exists("Assets/Scenes/VisualShowcase.unity"))
            {
                string prevScene = SceneManager.GetActiveScene().path;
                CreateSingleBootstrapScene("VisualShowcase", AbyssSceneKind.VisualShowcase);
                if (!string.IsNullOrEmpty(prevScene) && File.Exists(prevScene))
                    EditorSceneManager.OpenScene(prevScene, OpenSceneMode.Single);
            }

            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[Abyssal Dungeon 3D] ExportedProject Modular 3D Environment Kit, PBR Materials & 7 Biomes Ready!</color>");
        }

        private static void EnsureFolders()
        {
            string[] dirs =
            {
                "Assets/Scenes",
                "Assets/Resources",
                "Assets/Resources/Models",
                "Assets/Resources/Models/Heroes",
                "Assets/Resources/Models/Enemies",
                "Assets/Resources/Animators",
                "Assets/Resources/UI",
                "Assets/Resources/UI/Classes",
                "Assets/Resources/UI/Panels",
                "Assets/Resources/UI/Buttons",
                "Assets/Resources/UI/Decor",
                "Assets/Resources/UI/Icons",
                "Assets/Resources/UI/Fonts",
                "Assets/Resources/UI/Prefabs",
                "Assets/Resources/UI/Prefabs/Panels",
                "Assets/Resources/UI/Prefabs/Buttons",
                "Assets/Resources/Environment",
                "Assets/Resources/Environment/PBR",
                "Assets/Resources/Environment/Materials",
                "Assets/Resources/EnvironmentKit",
                "Assets/Resources/Biomes",
                "Assets/Resources/Shaders",
                "Assets/Resources/Audio",
                "Assets/Resources/Classes",
                "Assets/Art/Materials",
                "Assets/Prefabs",
                "Assets/Prefabs/Player",
                "Assets/Prefabs/Classes",
                "Assets/Prefabs/Enemy",
                "Assets/Prefabs/Boss",
                "Assets/Prefabs/Environment",
                "Assets/Prefabs/Dungeon",
                "Assets/ScriptableObjects/Biomes",
                "Assets/ScriptableObjects/Classes"
            };
            foreach (var d in dirs)
            {
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
            }
            AssetDatabase.Refresh();
        }

        private static void ConfigureTextureImporters()
        {
            // UI sprites (Menu/Lobby backgrounds, Panel Frame & 10 Painted Class Portraits)
            string[] uiPngs = Directory.Exists("Assets/Resources/UI")
                ? Directory.GetFiles("Assets/Resources/UI", "*.png", SearchOption.AllDirectories)
                : Array.Empty<string>();

            foreach (var raw in uiPngs)
            {
                string path = raw.Replace("\\", "/");
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti != null)
                {
                    Vector4 border = Vector4.zero;
                    if (path.IndexOf("/Panels/", StringComparison.OrdinalIgnoreCase) >= 0 || path.EndsWith("/panel_frame.png", StringComparison.OrdinalIgnoreCase))
                        border = new Vector4(32, 32, 32, 32);
                    else if (path.IndexOf("/Buttons/", StringComparison.OrdinalIgnoreCase) >= 0)
                        border = new Vector4(24, 20, 24, 20);
                    else if (path.EndsWith("Bar_Frame_Gothic.png", StringComparison.OrdinalIgnoreCase))
                        border = new Vector4(12, 8, 12, 8);
                    else if (path.EndsWith("Slot_Frame_Gothic.png", StringComparison.OrdinalIgnoreCase))
                        border = new Vector4(20, 20, 20, 20);

                    if (ti.textureType != TextureImporterType.Sprite || ti.mipmapEnabled || !ti.isReadable || ti.spriteBorder != border)
                    {
                        ti.textureType = TextureImporterType.Sprite;
                        ti.spriteImportMode = SpriteImportMode.Single;
                        ti.mipmapEnabled = false;
                        ti.alphaIsTransparency = true;
                        ti.isReadable = true;
                        ti.spriteBorder = border;
                        ti.SaveAndReimport();
                    }
                }
            }

            // Tileable environment PBR textures in Assets/Resources/Environment
            if (Directory.Exists("Assets/Resources/Environment"))
            {
                foreach (var raw in Directory.GetFiles("Assets/Resources/Environment", "*.png", SearchOption.AllDirectories))
                {
                    string path = raw.Replace("\\", "/");
                    var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (ti == null) continue;

                    bool isNormal = path.IndexOf("_Normal.png", StringComparison.OrdinalIgnoreCase) >= 0;
                    bool isDecal = path.IndexOf("T_Decal", StringComparison.OrdinalIgnoreCase) >= 0;
                    bool isLinearData = path.IndexOf("_Mask.png", StringComparison.OrdinalIgnoreCase) >= 0
                                     || path.IndexOf("_AO.png", StringComparison.OrdinalIgnoreCase) >= 0;

                    TextureImporterType desiredType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    TextureWrapMode desiredWrap = isDecal ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                    bool desiredSrgb = !isNormal && !isLinearData;

                    if (ti.textureType != desiredType || ti.wrapMode != desiredWrap || ti.sRGBTexture != desiredSrgb)
                    {
                        ti.textureType = desiredType;
                        ti.wrapMode = desiredWrap;
                        ti.sRGBTexture = desiredSrgb;
                        ti.alphaIsTransparency = isDecal;
                        ti.anisoLevel = 4;
                        ti.SaveAndReimport();
                    }
                }
            }
        }

        [MenuItem("Tools/Abyssal Dungeon/5. Build Ancient Gothic UI Prefabs")]
        public static void BuildGothicUIPrefabs()
        {
            EnsureFolders();
            var tempRoot = new GameObject("_TempGothicUIPrefabBuilder");
            try
            {
                foreach (UIPanelStyle pStyle in Enum.GetValues(typeof(UIPanelStyle)))
                {
                    string pName = "UI_Panel_" + pStyle;
                    var img = GameUIKit.MakeStyledPanel(pName, tempRoot.transform, pStyle,
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-240f, -160f), new Vector2(240f, 160f));
                    string assetPath = $"Assets/Resources/UI/Prefabs/Panels/{pName}.prefab";
                    PrefabUtility.SaveAsPrefabAsset(img.gameObject, assetPath);
                }

                foreach (UIButtonStyle bStyle in Enum.GetValues(typeof(UIButtonStyle)))
                {
                    string bName = "UI_Btn_" + bStyle;
                    var btn = GameUIKit.MakeStyledButton(bName, tempRoot.transform, bStyle.ToString().ToUpper(), 16, bStyle,
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-120f, -28f), new Vector2(120f, 28f), null);
                    string assetPath = $"Assets/Resources/UI/Prefabs/Buttons/{bName}.prefab";
                    PrefabUtility.SaveAsPrefabAsset(btn.gameObject, assetPath);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tempRoot);
            }
        }

        // =====================================================================
        // ANIMATOR CONTROLLERS (10 HERO CLASSES + 13 ENEMIES / BEAST / BOSS)
        // =====================================================================

        private static Dictionary<string, AnimatorController> BuildAllHeroAnimatorControllers()
        {
            string[] classes =
            {
                "Knight", "Guardian", "Berserker", "Assassin", "Ranger",
                "Mage", "Warlock", "Monk", "Druid", "Cleric"
            };

            AnimationClip clipSprint = ModelImportSetup.LoadHeroClip("Common", "Sprint");
            AnimationClip clipDodge = ModelImportSetup.LoadHeroClip("Common", "Dodge");
            AnimationClip clipHit = ModelImportSetup.LoadHeroClip("Common", "Hit");
            AnimationClip clipDeath = ModelImportSetup.LoadHeroClip("Common", "Death");

            var dict = new Dictionary<string, AnimatorController>(StringComparer.Ordinal);
            foreach (string cls in classes)
            {
                string ctrlPath = $"Assets/Resources/Animators/HeroAnimator_{cls}.controller";
                var ctrl = CreateUnitAnimatorController(
                    ctrlPath,
                    clipIdle:   ModelImportSetup.LoadHeroClip(cls, "Idle"),
                    clipWalk:   ModelImportSetup.LoadHeroClip(cls, "Run"),
                    clipRun:    ModelImportSetup.LoadHeroClip(cls, "Run"),
                    clipSprint: clipSprint != null ? clipSprint : ModelImportSetup.LoadHeroClip(cls, "Run"),
                    clipAttack: ModelImportSetup.LoadHeroClip(cls, "Attack"),
                    clipCast:   ModelImportSetup.LoadHeroClip(cls, "Cast"),
                    clipDodge:  clipDodge,
                    clipJump:   ModelImportSetup.LoadHeroClip(cls, "Jump"),
                    clipHit:    clipHit,
                    clipDeath:  clipDeath,
                    walkTimeScale: 0.65f);
                dict[cls] = ctrl;
            }

            // Also save default HeroAnimator.controller (Knight stance) for backwards compatibility
            CreateUnitAnimatorController(
                "Assets/Resources/Animators/HeroAnimator.controller",
                clipIdle:   ModelImportSetup.LoadHeroClip("Knight", "Idle"),
                clipWalk:   ModelImportSetup.LoadHeroClip("Knight", "Run"),
                clipRun:    ModelImportSetup.LoadHeroClip("Knight", "Run"),
                clipSprint: clipSprint,
                clipAttack: ModelImportSetup.LoadHeroClip("Knight", "Attack"),
                clipCast:   ModelImportSetup.LoadHeroClip("Knight", "Cast"),
                clipDodge:  clipDodge,
                clipJump:   ModelImportSetup.LoadHeroClip("Knight", "Jump"),
                clipHit:    clipHit,
                clipDeath:  clipDeath,
                walkTimeScale: 0.65f);

            return dict;
        }

        private static Dictionary<string, AnimatorController> BuildAllEnemyAnimatorControllers()
        {
            string[] enemyIds =
            {
                "DruidBeast",
                "Goblin", "GoblinArcher", "Skeleton", "SkeletonWarrior", "SkeletonMage",
                "Zombie", "DarkKnight", "Demon", "CrystalGolem", "ForestMonster",
                "SoulMinion", "AbyssalKnight"
            };

            var dict = new Dictionary<string, AnimatorController>(StringComparer.Ordinal);
            foreach (string id in enemyIds)
            {
                AnimationClip idle = ModelImportSetup.LoadEnemyClip(id, "Idle");
                AnimationClip walk = ModelImportSetup.LoadEnemyClip(id, "Walk") ?? idle;
                AnimationClip run = ModelImportSetup.LoadEnemyClip(id, "Run") ?? walk;
                AnimationClip atk = ModelImportSetup.LoadEnemyClip(id, "Attack") ?? idle;
                AnimationClip cast = ModelImportSetup.LoadEnemyClip(id, "Cast") ?? atk;
                AnimationClip hit = ModelImportSetup.LoadEnemyClip(id, "Hit") ?? idle;
                AnimationClip death = ModelImportSetup.LoadEnemyClip(id, "Death") ?? idle;

                string path = $"Assets/Resources/Animators/EnemyAnimator_{id}.controller";
                var ctrl = CreateUnitAnimatorController(
                    path,
                    clipIdle: idle,
                    clipWalk: walk,
                    clipRun: run,
                    clipSprint: run,
                    clipAttack: atk,
                    clipCast: cast,
                    clipDodge: run,
                    clipJump: run,
                    clipHit: hit,
                    clipDeath: death,
                    walkTimeScale: 1.0f);
                dict[id] = ctrl;

                if (id == "AbyssalKnight")
                {
                    CreateUnitAnimatorController(
                        "Assets/Resources/Animators/BossAnimator_AbyssalKnight.controller",
                        idle, walk, run, run, atk, cast, run, run, hit, death, 1.0f);
                }
            }
            return dict;
        }

        private static AnimatorController CreateUnitAnimatorController(
            string assetPath,
            AnimationClip clipIdle,
            AnimationClip clipWalk,
            AnimationClip clipRun,
            AnimationClip clipSprint,
            AnimationClip clipAttack,
            AnimationClip clipCast,
            AnimationClip clipDodge,
            AnimationClip clipJump,
            AnimationClip clipHit,
            AnimationClip clipDeath,
            float walkTimeScale)
        {
            if (File.Exists(assetPath)) AssetDatabase.DeleteAsset(assetPath);

            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(assetPath);
            ctrl.AddParameter(new AnimatorControllerParameter { name = "Speed", type = AnimatorControllerParameterType.Float, defaultFloat = 0f });
            ctrl.AddParameter(new AnimatorControllerParameter { name = "LocoSpeedMult", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
            ctrl.AddParameter(new AnimatorControllerParameter { name = "IsGrounded", type = AnimatorControllerParameterType.Bool, defaultBool = true });
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Cast", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Dodge", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

            var sm = ctrl.layers[0].stateMachine;

            // 4-Threshold Locomotion BlendTree: Idle (0) -> Walk (0.45) -> Run (1.0) -> Sprint (1.45) + Stride Sync
            var locoState = ctrl.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            locoState.speedParameterActive = true;
            locoState.speedParameter = "LocoSpeedMult";
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;

            var childMotions = new List<ChildMotion>();
            if (clipIdle != null)
                childMotions.Add(new ChildMotion { motion = clipIdle, threshold = 0f, timeScale = 1.0f });
            if (clipWalk != null)
                childMotions.Add(new ChildMotion { motion = clipWalk, threshold = 0.45f, timeScale = walkTimeScale });
            if (clipRun != null)
                childMotions.Add(new ChildMotion { motion = clipRun, threshold = 1.00f, timeScale = 1.02f });
            if (clipSprint != null)
                childMotions.Add(new ChildMotion { motion = clipSprint, threshold = 1.45f, timeScale = 1.25f });

            tree.children = childMotions.ToArray();
            sm.defaultState = locoState;

            AddActionState(sm, locoState, "Attack", clipAttack, "Attack", 0.85f);
            AddActionState(sm, locoState, "Cast", clipCast != null ? clipCast : clipAttack, "Cast", 0.85f);
            AddActionState(sm, locoState, "Dodge", clipDodge != null ? clipDodge : clipRun, "Dodge", 0.82f);

            var jumpState = sm.AddState("Jump");
            jumpState.motion = clipJump != null ? clipJump : (clipDodge != null ? clipDodge : clipRun);
            jumpState.speed = 1.0f;
            var toJump = sm.AddAnyStateTransition(jumpState);
            toJump.hasExitTime = false;
            toJump.duration = 0.08f;
            toJump.canTransitionToSelf = false;
            toJump.AddCondition(AnimatorConditionMode.If, 0f, "Jump");
            toJump.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");

            var landTrans = jumpState.AddTransition(locoState);
            landTrans.hasExitTime = true;
            landTrans.exitTime = 0.35f;
            landTrans.duration = 0.12f;
            landTrans.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");

            var jumpFallback = jumpState.AddTransition(locoState);
            jumpFallback.hasExitTime = true;
            jumpFallback.exitTime = 0.85f;
            jumpFallback.duration = 0.14f;

            var hitState = sm.AddState("TakeDamage");
            hitState.motion = clipHit != null ? clipHit : clipIdle;
            hitState.speed = 1.25f;
            var toHit = sm.AddAnyStateTransition(hitState);
            toHit.hasExitTime = false;
            toHit.duration = 0.05f;
            toHit.canTransitionToSelf = true;
            toHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
            toHit.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");

            var hitBack = hitState.AddTransition(locoState);
            hitBack.hasExitTime = true;
            hitBack.exitTime = 0.55f;
            hitBack.duration = 0.12f;

            var deadState = sm.AddState("Death");
            deadState.motion = clipDeath;
            var toDead = sm.AddAnyStateTransition(deadState);
            toDead.hasExitTime = false;
            toDead.duration = 0.10f;
            toDead.canTransitionToSelf = false;
            toDead.AddCondition(AnimatorConditionMode.If, 0f, "IsDead");

            var reviveTrans = deadState.AddTransition(locoState);
            reviveTrans.hasExitTime = false;
            reviveTrans.duration = 0.15f;
            reviveTrans.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        private static void AddActionState(AnimatorStateMachine sm, AnimatorState loco, string stateName, AnimationClip clip, string triggerParam, float exitTime)
        {
            var st = sm.AddState(stateName);
            st.motion = clip;

            var anyTo = sm.AddAnyStateTransition(st);
            anyTo.hasExitTime = false;
            anyTo.duration = 0.08f;
            anyTo.canTransitionToSelf = true;
            anyTo.AddCondition(AnimatorConditionMode.If, 0f, triggerParam);
            anyTo.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");

            var back = st.AddTransition(loco);
            back.hasExitTime = true;
            back.exitTime = exitTime;
            back.duration = 0.14f;
        }

        // =====================================================================
        // ASSEMBLED HERO, BEAST, MONSTER, SUMMON & BOSS PREFABS
        // =====================================================================

        private static void CreateAllCharacterAndEnemyPrefabs(
            Dictionary<string, AnimatorController> heroCtrls,
            Dictionary<string, AnimatorController> enemyCtrls)
        {
            var heroSpecs = new (string cls, string rigFile, string rightWpn, string leftWpn, string floatingBook)[]
            {
                ("Knight",    "HeroRig_Knight",    "Wpn_KnightSword",         "Wpn_KnightShield",   null),
                ("Guardian",  "HeroRig_Guardian",  "Wpn_GuardianMace",        "Wpn_GuardianShield", null),
                ("Berserker", "HeroRig_Berserker", "Wpn_BerserkerGreatsword", null,                 null),
                ("Assassin",  "HeroRig_Assassin",  "Wpn_AssassinBladeR",      "Wpn_AssassinBladeL", null),
                ("Ranger",    "HeroRig_Ranger",    null,                      "Wpn_RangerBow",      null),
                ("Mage",      "HeroRig_Mage",      "Wpn_MageStaff",           null,                 null),
                ("Warlock",   "HeroRig_Warlock",   "Wpn_WarlockSword",        null,                 "Wpn_WarlockBook"),
                ("Monk",      "HeroRig_Monk",      null,                      null,                 null),
                ("Druid",     "HeroRig_Druid",     "Wpn_DruidStaff",          null,                 null),
                ("Cleric",    "HeroRig_Cleric",    "Wpn_ClericHammer",        "Wpn_ClericShield",   null)
            };

            foreach (var sp in heroSpecs)
            {
                string rigPath = $"{ModelImportSetup.ExportedCharDir}/{sp.rigFile}.prefab";
                GameObject srcRig = AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);
                if (srcRig == null)
                {
                    Debug.LogWarning($"[AbyssalAutoSetup] Missing exported hero rig: {rigPath}");
                    continue;
                }

                var root = new GameObject($"Hero_{sp.cls}");
                var rigInst = (GameObject)UnityEngine.Object.Instantiate(srcRig);
                rigInst.name = "Rig";
                StripAllCollidersAndShadowSettings(rigInst, castShadows: true);
                rigInst.transform.SetParent(root.transform, false);
                rigInst.transform.localPosition = Vector3.zero;
                rigInst.transform.localRotation = Quaternion.identity;
                rigInst.transform.localScale = Vector3.one;

                var anim = rigInst.GetComponent<Animator>();
                if (anim == null) anim = rigInst.AddComponent<Animator>();
                if (heroCtrls.TryGetValue(sp.cls, out var hCtrl))
                    anim.runtimeAnimatorController = hCtrl;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                if (!string.IsNullOrEmpty(sp.rightWpn))
                    AttachExportedWeaponToSocket(rigInst.transform, "RH_WP", sp.rightWpn, "SignatureMainWeapon");
                if (!string.IsNullOrEmpty(sp.leftWpn))
                    AttachExportedWeaponToSocket(rigInst.transform, "LH_WP", sp.leftWpn, "SignatureOffhand");
                if (!string.IsNullOrEmpty(sp.floatingBook))
                {
                    var bookSrc = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelImportSetup.ExportedWeapDir}/{sp.floatingBook}.prefab");
                    if (bookSrc != null)
                    {
                        var bookInst = (GameObject)UnityEngine.Object.Instantiate(bookSrc);
                        bookInst.name = "WarlockFloatingGrimoire";
                        StripAllCollidersAndShadowSettings(bookInst, castShadows: true);
                        bookInst.transform.SetParent(root.transform, false);
                        bookInst.transform.localPosition = new Vector3(-0.50f, 1.42f, 0.22f);
                        bookInst.transform.localRotation = Quaternion.Euler(18f, -24f, 0f);
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, $"Assets/Resources/Models/Heroes/Hero_{sp.cls}.prefab");
                PrefabUtility.SaveAsPrefabAsset(root, $"Assets/Prefabs/Player/Player_{sp.cls}.prefab");
                PrefabUtility.SaveAsPrefabAsset(root, $"Assets/Prefabs/Classes/Hero_{sp.cls}.prefab");

                // Save legacy base-model aliases so any Resources.Load("Models/Paladin") continues to resolve
                if (sp.cls == "Knight")
                    PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/Models/Paladin.prefab");
                else if (sp.cls == "Berserker")
                    PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/Models/Vanguard.prefab");
                else if (sp.cls == "Assassin")
                    PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/Models/Kachujin.prefab");

                UnityEngine.Object.DestroyImmediate(root);
            }

            // Druid Dire-Beast Form Prefab
            SaveAssembledCreaturePrefab(
                outPrefabPaths: new[] { "Assets/Resources/Models/Heroes/Hero_DruidBeast.prefab", "Assets/Prefabs/Classes/Hero_DruidBeast.prefab" },
                rootName: "Hero_DruidBeast",
                srcPath: $"{ModelImportSetup.ExportedCharDir}/HeroRig_DruidBeast.prefab",
                ctrl: enemyCtrls.TryGetValue("DruidBeast", out var beastCtrl) ? beastCtrl : null,
                targetHeight: 1.52f,
                rightWpn: null,
                leftWpn: null);

            // 10 Dungeon Enemy Types + Warlock SoulMinion + Final Boss AbyssalKnight
            var enemySpecs = new (string id, string srcFile, float unitHeight, string rightWpn, string leftWpn)[]
            {
                ("Goblin",          "Mst_Goblin",          1.84f, "Wpn_GoblinShovel",  null),
                ("GoblinArcher",    "Mst_GoblinArcher",    1.90f, null,                "Wpn_RangerBow"),
                ("Skeleton",        "Mst_Skeleton",        1.90f, "Wpn_SkeletonSword", null),
                ("SkeletonWarrior", "Mst_SkeletonWarrior", 1.96f, "Wpn_SkeletonSword", "Wpn_SkeletonShield"),
                ("SkeletonMage",    "Mst_SkeletonMage",    1.96f, null,                null),
                ("Zombie",          "Mst_Zombie",          1.96f, null,                null),
                ("DarkKnight",      "Mst_DarkKnight",      2.02f, null,                null),
                ("Demon",           "Mst_Demon",           2.10f, null,                null),
                ("CrystalGolem",    "Mst_CrystalGolem",    1.92f, null,                null),
                ("ForestMonster",   "Mst_ForestMonster",   1.86f, null,                null),
                ("SoulMinion",      "Mst_SoulMinion",      1.48f, null,                null),
                ("AbyssalKnight",   "Boss_AbyssalKnight",  1.88f, null,                null)
            };

            foreach (var es in enemySpecs)
            {
                enemyCtrls.TryGetValue(es.id, out var eCtrl);
                string srcPath = $"{ModelImportSetup.ExportedEnemyDir}/{es.srcFile}.prefab";
                string[] targets = es.id == "AbyssalKnight"
                    ? new[]
                    {
                        "Assets/Resources/Models/Enemies/Boss_AbyssalKnight.prefab",
                        "Assets/Resources/Models/Enemies/Enemy_AbyssalKnight.prefab",
                        "Assets/Prefabs/Boss/Boss_AbyssalKnight.prefab"
                    }
                    : new[]
                    {
                        $"Assets/Resources/Models/Enemies/Enemy_{es.id}.prefab",
                        $"Assets/Prefabs/Enemy/Enemy_{es.id}.prefab"
                    };

                SaveAssembledCreaturePrefab(
                    targets,
                    es.id == "AbyssalKnight" ? "Boss_AbyssalKnight" : $"Enemy_{es.id}",
                    srcPath,
                    eCtrl,
                    es.unitHeight,
                    es.rightWpn,
                    es.leftWpn);
            }
        }

        private static void SaveAssembledCreaturePrefab(
            string[] outPrefabPaths,
            string rootName,
            string srcPath,
            AnimatorController ctrl,
            float targetHeight,
            string rightWpn,
            string leftWpn)
        {
            GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(srcPath);
            if (src == null)
            {
                Debug.LogWarning($"[AbyssalAutoSetup] Missing creature rig: {srcPath}");
                return;
            }

            var rigInst = (GameObject)UnityEngine.Object.Instantiate(src);
            rigInst.name = "Rig";
            StripAllCollidersAndShadowSettings(rigInst, castShadows: true);
            rigInst.transform.position = Vector3.zero;
            rigInst.transform.rotation = Quaternion.identity;
            rigInst.transform.localScale = Vector3.one;

            if (TryComputeWorldBounds(rigInst, out Bounds b) && b.size.y > 0.1f)
            {
                float s = Mathf.Clamp(targetHeight / b.size.y, 0.42f, 2.25f);
                rigInst.transform.localScale = Vector3.one * s;
                if (TryComputeWorldBounds(rigInst, out Bounds b2) && Mathf.Abs(b2.min.y) > 0.06f)
                {
                    rigInst.transform.position += new Vector3(0f, -b2.min.y, 0f);
                }
            }

            var anim = rigInst.GetComponent<Animator>();
            if (anim == null) anim = rigInst.AddComponent<Animator>();
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (!string.IsNullOrEmpty(rightWpn))
                AttachExportedWeaponToSocket(rigInst.transform, "RH_WP", rightWpn, "SignatureMainWeapon");
            if (!string.IsNullOrEmpty(leftWpn))
                AttachExportedWeaponToSocket(rigInst.transform, "LH_WP", leftWpn, "SignatureOffhand");

            var root = new GameObject(rootName);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            rigInst.transform.SetParent(root.transform, true);

            foreach (string outPath in outPrefabPaths)
                PrefabUtility.SaveAsPrefabAsset(root, outPath);

            UnityEngine.Object.DestroyImmediate(root);
        }

        private static GameObject AttachExportedWeaponToSocket(Transform rigRoot, string socketName, string weaponPrefabName, string instanceName)
        {
            string wPath = $"{ModelImportSetup.ExportedWeapDir}/{weaponPrefabName}.prefab";
            GameObject wSrc = AssetDatabase.LoadAssetAtPath<GameObject>(wPath);
            if (wSrc == null) return null;

            Transform socket = FindExactOrPartial(rigRoot, socketName);
            if (socket == null)
                socket = FindExactOrPartial(rigRoot, socketName.StartsWith("R") ? "Bip001 R Hand" : "Bip001 L Hand") ?? rigRoot;

            var wInst = (GameObject)UnityEngine.Object.Instantiate(wSrc);
            wInst.name = instanceName;
            StripAllCollidersAndShadowSettings(wInst, castShadows: true);

            // Preserve the weapon prefab's artist-authored localRotation & localScale when parenting to RH_WP / LH_WP!
            Quaternion authoredRot = wInst.transform.localRotation;
            Vector3 authoredPos = wInst.transform.localPosition;
            Vector3 authoredScale = wInst.transform.localScale;

            wInst.transform.SetParent(socket, false);
            wInst.transform.localRotation = authoredRot;
            wInst.transform.localScale = authoredScale;
            wInst.transform.localPosition = authoredPos.sqrMagnitude > 0.09f ? Vector3.zero : authoredPos;
            return wInst;
        }

        private static Transform FindExactOrPartial(Transform root, string targetName)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (string.Equals(all[i].name, targetName, StringComparison.OrdinalIgnoreCase))
                    return all[i];
            for (int i = 0; i < all.Length; i++)
                if (all[i].name.IndexOf(targetName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return all[i];
            return null;
        }

        private static void StripAllCollidersAndShadowSettings(GameObject go, bool castShadows)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(c);

            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = castShadows
                    ? UnityEngine.Rendering.ShadowCastingMode.On
                    : UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = true;
            }
        }

        private static bool TryComputeWorldBounds(GameObject root, out Bounds bounds)
        {
            var rs = root.GetComponentsInChildren<Renderer>(true);
            bounds = default;
            bool hasAny = false;
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] == null) continue;
                Bounds rb = rs[i].bounds;
                if (rb.size.sqrMagnitude < 0.00001f) continue;
                if (!hasAny)
                {
                    bounds = rb;
                    hasAny = true;
                }
                else
                {
                    bounds.Encapsulate(rb);
                }
            }
            return hasAny;
        }

        // =====================================================================
        // PERSISTENT PBR ENVIRONMENT MATERIALS & MODULAR ENVIRONMENT KIT
        // =====================================================================

        private static void BuildPersistentPbrMaterials()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var specs = new (string matName, string texPrefix, Color col, float metal, float smooth)[]
            {
                ("M_GothicStone",    "T_GothicStone",    new Color(0.86f, 0.82f, 0.78f, 1f), 0.06f, 0.30f),
                ("M_CathedralFloor", "T_CathedralFloor", new Color(0.84f, 0.80f, 0.76f, 1f), 0.08f, 0.38f),
                ("M_MossyRuins",     "T_MossyRuins",     new Color(0.80f, 0.86f, 0.78f, 1f), 0.04f, 0.38f),
                ("M_WroughtIron",    "T_WroughtIron",    new Color(0.78f, 0.80f, 0.86f, 1f), 0.84f, 0.54f),
                ("M_AgedOakWood",    "T_AgedOakWood",    new Color(0.86f, 0.76f, 0.66f, 1f), 0.04f, 0.28f),
                ("M_RunicGold",      "T_WroughtIron",    new Color(0.94f, 0.74f, 0.32f, 1f), 0.88f, 0.68f)
            };

            foreach (var sp in specs)
            {
                Texture2D alb = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Resources/Environment/PBR/{sp.texPrefix}_Albedo.png");
                Texture2D nrm = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Resources/Environment/PBR/{sp.texPrefix}_Normal.png");
                Texture2D msk = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Resources/Environment/PBR/{sp.texPrefix}_Mask.png");
                Texture2D ao  = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Resources/Environment/PBR/{sp.texPrefix}_AO.png");

                string[] targets =
                {
                    $"Assets/Art/Materials/{sp.matName}.mat",
                    $"Assets/Resources/Environment/Materials/{sp.matName}.mat"
                };

                foreach (var path in targets)
                {
                    var mat = new Material(urpLit) { name = sp.matName, enableInstancing = true };
                    mat.color = sp.col;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", sp.col);
                    if (alb != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", alb);
                    if (nrm != null && mat.HasProperty("_BumpMap"))
                    {
                        mat.SetTexture("_BumpMap", nrm);
                        mat.EnableKeyword("_NORMALMAP");
                    }
                    if (msk != null && mat.HasProperty("_MetallicGlossMap"))
                    {
                        mat.SetTexture("_MetallicGlossMap", msk);
                        mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                    }
                    if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", sp.metal);
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", sp.smooth);
                    if (ao != null && mat.HasProperty("_OcclusionMap"))
                    {
                        mat.SetTexture("_OcclusionMap", ao);
                        mat.EnableKeyword("_OCCLUSIONMAP");
                    }
                    if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
                    AssetDatabase.CreateAsset(mat, path);
                }
            }
        }

        private enum EnvNormMode
        {
            FitXYZBottomZero,
            FitXYZTopZero,
            UniformHeightBottomZero,
            UniformWidthBottomZero,
            UniformHeightCentered,
            UniformWidthCentered,
            FixedScalePreservePivot
        }

        private static void BuildModularEnvironmentPrefabs()
        {
            string envDir = ModelImportSetup.ExportedEnvDir;
            string wpnDir = ModelImportSetup.ExportedWeapDir;

            var pieces = new (string outName, string srcPath, Quaternion preRot, Vector3 targetSize, EnvNormMode mode, float extraParam, string dungeonAlias)[]
            {
                // Modular Architecture
                ("DungeonFloor_ChannelTile",    $"{envDir}/Env_DungeonFloor.prefab",          Quaternion.identity,          new Vector3(4.00f, 0.22f, 4.00f), EnvNormMode.FitXYZTopZero,           0f,    null),
                ("DungeonWall_Modular",         $"{envDir}/Env_DungeonWall.prefab",           Quaternion.Euler(0f, 90f, 0f),new Vector3(4.00f, 5.20f, 0.72f), EnvNormMode.FitXYZBottomZero,        0f,    null),
                ("GothicPillar",                $"{envDir}/Env_GothicPillar.prefab",          Quaternion.identity,          new Vector3(1.15f, 5.10f, 1.15f), EnvNormMode.UniformHeightBottomZero, 1.20f, "Pillar"),
                ("GothicArchVault",             $"{envDir}/Env_GothicArchVault.prefab",       Quaternion.identity,          new Vector3(4.00f, 5.10f, 0.90f), EnvNormMode.FitXYZBottomZero,        0f,    null),
                ("GothicGateFrame",             $"{envDir}/Env_GothicArchVault.prefab",       Quaternion.identity,          new Vector3(6.20f, 5.20f, 1.10f), EnvNormMode.FitXYZBottomZero,        0f,    "Door"),
                ("IronPortcullis",              $"{envDir}/Env_IronPortcullis.prefab",        Quaternion.identity,          new Vector3(5.05f, 3.90f, 0.22f), EnvNormMode.FitXYZBottomZero,        0f,    null),
                ("StoneStairs_Grand",           $"{envDir}/Env_StoneStairs.prefab",           Quaternion.identity,          new Vector3(4.00f, 1.48f, 4.00f), EnvNormMode.FitXYZBottomZero,        0f,    null),

                // Lighting, Shrines, Portals, Chests, Traps & Biome Props
                ("WallTorchSconce",             $"{envDir}/Env_WallTorchSconce.prefab",       Quaternion.identity,          new Vector3(0.45f, 0.68f, 0.45f), EnvNormMode.UniformHeightCentered,   0f,    "Torch"),
                ("FireBrazierGrate",            $"{envDir}/Env_FireBrazierGrate.prefab",      Quaternion.identity,          new Vector3(0.75f, 1.12f, 0.75f), EnvNormMode.UniformHeightBottomZero, 0f,    "Trap_Fire"),
                ("AbyssalCrystalCluster",       $"{envDir}/Env_AbyssalCrystalCluster.prefab", Quaternion.identity,          new Vector3(1.35f, 1.65f, 1.35f), EnvNormMode.FitXYZBottomZero,        0f,    "Crystal"),
                ("HealingShrinePedestal",       $"{envDir}/Env_HealingShrine.prefab",         Quaternion.identity,          new Vector3(1.20f, 1.78f, 1.20f), EnvNormMode.UniformHeightBottomZero, 0f,    "HealingShrine"),
                ("RunicPillarAltar",            $"{envDir}/Env_RunePillarAltar.prefab",       Quaternion.identity,          new Vector3(1.40f, 1.82f, 1.40f), EnvNormMode.UniformHeightBottomZero, 1.65f, "Altar"),
                ("GuardianStatue_Gothic",       $"{envDir}/Env_GuardianStatue.prefab",        Quaternion.identity,          new Vector3(1.60f, 3.65f, 1.60f), EnvNormMode.UniformHeightBottomZero, 0f,    null),
                ("AncientRuins_Rubble",         $"{envDir}/Env_AncientRubble.prefab",         Quaternion.identity,          new Vector3(1.80f, 0.75f, 1.80f), EnvNormMode.UniformWidthBottomZero,  0f,    null),
                ("CursedTreeRoot",              $"{envDir}/Env_CursedTreeRoot.prefab",        Quaternion.identity,          new Vector3(2.40f, 2.65f, 2.40f), EnvNormMode.UniformHeightBottomZero, 2.90f, null),
                ("TreasureChest_IronboundBase", $"{envDir}/Env_TreasureChestBase.prefab",     Quaternion.identity,          Vector3.one,                      EnvNormMode.FixedScalePreservePivot, 0.76f, "Chest"),
                ("TreasureChest_IronboundLid",  $"{envDir}/Env_TreasureChestLid.prefab",      Quaternion.identity,          Vector3.one,                      EnvNormMode.FixedScalePreservePivot, 0.76f, null),
                ("SpikeTrapPlate",              $"{envDir}/Env_SpikeTrapPlate.prefab",        Quaternion.identity,          new Vector3(2.00f, 0.16f, 2.00f), EnvNormMode.FitXYZBottomZero,        0f,    "Trap_Spike"),
                ("SpikeCluster",                $"{envDir}/Env_SpikeCluster.prefab",          Quaternion.identity,          new Vector3(1.55f, 0.55f, 1.55f), EnvNormMode.FitXYZBottomZero,        0f,    null),
                ("BossThroneAltar",             $"{envDir}/Env_BossThroneAltar.prefab",       Quaternion.identity,          new Vector3(3.20f, 4.40f, 2.60f), EnvNormMode.UniformHeightBottomZero, 0f,    null),
                ("AbyssalPortal",               $"{envDir}/Env_AbyssalPortal.prefab",         Quaternion.identity,          new Vector3(3.80f, 4.20f, 3.80f), EnvNormMode.UniformHeightBottomZero, 4.60f, "Portal"),
                ("EscapeFloorPortal",           $"{envDir}/Env_EscapeFloorPortal.prefab",     Quaternion.identity,          new Vector3(3.20f, 0.35f, 3.20f), EnvNormMode.UniformWidthBottomZero,  0f,    null),

                // 3D Equipment Drop Pickups & Elite Crown Banner
                ("Weap_Knight_RunicLongsword",  $"{wpnDir}/Wpn_KnightSword.prefab",           Quaternion.identity,          new Vector3(0.35f, 1.05f, 0.35f), EnvNormMode.UniformHeightCentered,   0f,    null),
                ("Equip_Armor_Cuirass",         $"{envDir}/Loot_ArmorBox.prefab",             Quaternion.identity,          new Vector3(0.75f, 0.55f, 0.75f), EnvNormMode.UniformWidthCentered,    0f,    null),
                ("Equip_Helmet_Gothic",         $"{envDir}/Loot_Helm.prefab",                 Quaternion.identity,          new Vector3(0.55f, 0.58f, 0.55f), EnvNormMode.UniformHeightCentered,   0f,    null),
                ("Equip_Ring_Signet",           $"{envDir}/Loot_GemRing.prefab",              Quaternion.identity,          new Vector3(0.45f, 0.25f, 0.45f), EnvNormMode.UniformWidthCentered,    0f,    null),
                ("Equip_Amulet_Talisman",       $"{envDir}/Loot_SkullTalisman.prefab",        Quaternion.identity,          new Vector3(0.52f, 0.25f, 0.52f), EnvNormMode.UniformWidthCentered,    0f,    null),
                ("Enemy_Elite_AffixCrownBanner",$"{envDir}/Loot_EliteBanner.prefab",          Quaternion.identity,          new Vector3(0.45f, 0.65f, 0.45f), EnvNormMode.UniformHeightBottomZero, 0f,    null)
            };

            foreach (var p in pieces)
            {
                GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(p.srcPath);
                if (src == null)
                {
                    Debug.LogWarning($"[AbyssalAutoSetup] Missing environment source prefab: {p.srcPath}");
                    continue;
                }

                var child = (GameObject)UnityEngine.Object.Instantiate(src);
                child.name = "Model";
                StripAllCollidersAndShadowSettings(child, castShadows: p.outName != "DungeonFloor_ChannelTile");
                child.transform.position = Vector3.zero;
                child.transform.rotation = p.preRot != default ? p.preRot : Quaternion.identity;
                child.transform.localScale = Vector3.one;

                if (p.mode == EnvNormMode.FixedScalePreservePivot)
                {
                    child.transform.localScale = Vector3.one * p.extraParam;
                }
                else if (TryComputeWorldBounds(child, out Bounds b))
                {
                    float sx = 1f, sy = 1f, sz = 1f;
                    switch (p.mode)
                    {
                        case EnvNormMode.FitXYZBottomZero:
                        case EnvNormMode.FitXYZTopZero:
                            sx = p.targetSize.x / Mathf.Max(0.04f, b.size.x);
                            sy = p.targetSize.y / Mathf.Max(0.04f, b.size.y);
                            sz = p.targetSize.z / Mathf.Max(0.04f, b.size.z);
                            break;

                        case EnvNormMode.UniformHeightBottomZero:
                        case EnvNormMode.UniformHeightCentered:
                            sy = p.targetSize.y / Mathf.Max(0.04f, b.size.y);
                            sx = sy;
                            sz = sy;
                            if (p.extraParam > 0.05f)
                            {
                                float maxFoot = Mathf.Max(b.size.x, b.size.z) * sy;
                                if (maxFoot > p.extraParam)
                                {
                                    float shrinkXZ = p.extraParam / maxFoot;
                                    sx *= shrinkXZ;
                                    sz *= shrinkXZ;
                                }
                            }
                            break;

                        case EnvNormMode.UniformWidthBottomZero:
                        case EnvNormMode.UniformWidthCentered:
                            float wMax = Mathf.Max(0.04f, Mathf.Max(b.size.x, b.size.z));
                            sx = p.targetSize.x / wMax;
                            sy = sx;
                            sz = sx;
                            break;
                    }

                    // Apply world-axis scaling via an intermediate holder when preRot is non-identity
                    if (Quaternion.Angle(child.transform.rotation, Quaternion.identity) > 0.5f)
                    {
                        var rotHolder = new GameObject("RotatedModel");
                        child.transform.SetParent(rotHolder.transform, true);
                        rotHolder.transform.localScale = new Vector3(sx, sy, sz);
                        child = rotHolder;
                    }
                    else
                    {
                        child.transform.localScale = new Vector3(sx, sy, sz);
                    }

                    if (TryComputeWorldBounds(child, out Bounds b2))
                    {
                        float dx = -b2.center.x;
                        float dz = -b2.center.z;
                        float dy;
                        if (p.mode == EnvNormMode.FitXYZTopZero)
                            dy = -b2.max.y;
                        else if (p.mode == EnvNormMode.UniformHeightCentered || p.mode == EnvNormMode.UniformWidthCentered)
                            dy = -b2.center.y;
                        else
                            dy = -b2.min.y;

                        child.transform.position += new Vector3(dx, dy, dz);
                    }
                }

                var root = new GameObject(p.outName);
                root.transform.position = Vector3.zero;
                root.transform.rotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;
                child.transform.SetParent(root.transform, true);

                PrefabUtility.SaveAsPrefabAsset(root, $"Assets/Prefabs/Environment/{p.outName}.prefab");
                PrefabUtility.SaveAsPrefabAsset(root, $"Assets/Resources/EnvironmentKit/{p.outName}.prefab");
                if (!string.IsNullOrEmpty(p.dungeonAlias))
                {
                    PrefabUtility.SaveAsPrefabAsset(root, $"Assets/Prefabs/Dungeon/{p.dungeonAlias}.prefab");
                }
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildBiomeScriptableObjects()
        {
            foreach (DungeonBiome b in Enum.GetValues(typeof(DungeonBiome)))
            {
                BiomeAtmosphereData d = BiomeAtmosphereLibrary.GetBuiltIn(b);
                string[] paths =
                {
                    $"Assets/ScriptableObjects/Biomes/Biome_{b}.asset",
                    $"Assets/Resources/Biomes/Biome_{b}.asset"
                };
                foreach (var p in paths)
                {
                    var so = ScriptableObject.CreateInstance<BiomeAtmosphereSO>();
                    so.Data = d;
                    if (File.Exists(p)) AssetDatabase.DeleteAsset(p);
                    AssetDatabase.CreateAsset(so, p);
                }
            }
        }

        [MenuItem("Tools/Abyssal Dungeon/5. Setup Phase 4 Classes & Data Assets")]
        public static void SetupPhase4ClassesAndPrefabs()
        {
            EnsureFolders();

            for (int i = 0; i < GameDB.Classes.Length; i++)
            {
                ClassDef cls = GameDB.Classes[i];
                if (cls == null) continue;
                string[] soPaths =
                {
                    $"Assets/ScriptableObjects/Classes/CD_{cls.name}.asset",
                    $"Assets/Resources/Classes/CD_{cls.name}.asset"
                };
                foreach (var p in soPaths)
                {
                    var asset = ScriptableObject.CreateInstance<ClassDefinitionAsset>();
                    asset.SyncFromDatabase(cls);
                    if (File.Exists(p)) AssetDatabase.DeleteAsset(p);
                    AssetDatabase.CreateAsset(asset, p);
                }
            }

            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[Abyssal Dungeon 3D] 10 ClassDefinitionAsset ScriptableObjects synced with ExportedProject prefabs & portraits!</color>");
        }

        [MenuItem("Tools/Abyssal Dungeon/2. Generate All 7 Scenes")]
        public static void CreateAllGameScenes()
        {
            var scenes = new (string name, AbyssSceneKind kind)[]
            {
                ("MainMenu", AbyssSceneKind.MainMenu),
                ("Lobby", AbyssSceneKind.Lobby),
                ("ClassSelection", AbyssSceneKind.ClassSelection),
                ("Dungeon", AbyssSceneKind.Dungeon),
                ("EndGame", AbyssSceneKind.EndGame),
                ("Town", AbyssSceneKind.Town),
                ("VisualShowcase", AbyssSceneKind.VisualShowcase)
            };

            foreach (var (name, kind) in scenes)
            {
                CreateSingleBootstrapScene(name, kind);
            }
        }

        private static void CreateSingleBootstrapScene(string name, AbyssSceneKind kind)
        {
            string path = $"Assets/Scenes/{name}.unity";
            Scene sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.04f, 0.10f);
            cam.nearClipPlane = 0.15f;
            cam.farClipPlane = 160f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 2f, -6f);

            var lightGo = new GameObject("Directional Light");
            var dl = lightGo.AddComponent<Light>();
            dl.type = LightType.Directional;
            dl.color = new Color(0.92f, 0.88f, 1f);
            dl.intensity = 1.05f;
            dl.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            var bootGo = new GameObject("AbyssSceneBootstrap");
            var boot = bootGo.AddComponent<SceneBootstrap>();
            boot.SceneKind = kind;

            EditorSceneManager.SaveScene(sc, path);
        }

        public static void EnsureBuildSettings()
        {
            string[] order =
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/Lobby.unity",
                "Assets/Scenes/ClassSelection.unity",
                "Assets/Scenes/Dungeon.unity",
                "Assets/Scenes/EndGame.unity",
                "Assets/Scenes/Town.unity",
                "Assets/Scenes/VisualShowcase.unity"
            };

            var list = new List<EditorBuildSettingsScene>();
            foreach (var p in order)
            {
                if (File.Exists(p))
                    list.Add(new EditorBuildSettingsScene(p, true));
            }
            if (list.Count > 0)
                EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
#endif

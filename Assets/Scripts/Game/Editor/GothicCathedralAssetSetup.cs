#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace AbyssGame.EditorTools
{
    /// <summary>
    /// Builds persistent URP 12.1.7 Materials, Modular Gothic Cathedral Prefabs (from Blender 5.3 FBXs),
    /// UI Icon Sprite Atlas, 9-sliced Panel/Button Sprite borders, and Reusable Gothic HUD Prefabs.
    /// Compatible with Unity 2021.3.11f1 + URP 12.1.7. Never overwrites existing gameplay prefabs.
    /// </summary>
    public static class GothicCathedralAssetSetup
    {
        private const string RawFbxDir = "Assets/Resources/EnvironmentKit/Raw";
        private const string ResEnvKitDir = "Assets/Resources/EnvironmentKit";
        private const string PrefabEnvDir = "Assets/Prefabs/Environment";
        private const string ResMatDir = "Assets/Resources/Environment/Materials";
        private const string PrefabUiDir = "Assets/Prefabs/UI";

        [InitializeOnLoadMethod]
        private static void AutoBuildMissingPrefabsOnce()
        {
            if (SessionState.GetBool("Abyss_GothicPrefabsBuilt_v1", false)) return;
            if (File.Exists($"{PrefabUiDir}/DungeonHUD_Master.prefab") &&
                File.Exists($"{PrefabEnvDir}/FallenAngelStatue_Gothic.prefab") &&
                File.Exists("Assets/Resources/UI/AbyssalHUD_Icons.spriteatlas"))
            {
                SessionState.SetBool("Abyss_GothicPrefabsBuilt_v1", true);
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (SessionState.GetBool("Abyss_GothicPrefabsBuilt_v1", false)) return;
                SessionState.SetBool("Abyss_GothicPrefabsBuilt_v1", true);
                BuildAllGothicCathedralAndHudPrefabs();
            };
        }

        [MenuItem("Tools/Abyssal Dungeon/2. Build Gothic Cathedral & HUD Prefabs")]
        public static void BuildAllGothicCathedralAndHudPrefabs()
        {
            EnsureFolders();
            AssetDatabase.Refresh();

            BuildPersistentMaterials(
                out Material stoneMat,
                out Material ironMat,
                out Material woodMat,
                out Material runeMat,
                out Material warmShaftMat,
                out Material arcaneShaftMat);

            // 1. Modular Gothic Cathedral Architecture & Decor Prefabs from Blender 5.3 FBX
            BuildFbxPrefab("GothicCathedralVaultRib", new[] { stoneMat, ironMat }, addCollider: false, castShadows: false, receiveShadows: false);
            BuildFbxPrefab("FallenAngelStatue_Gothic", new[] { stoneMat, ironMat }, addCollider: true, castShadows: false, receiveShadows: true);
            BuildFbxPrefab("WoodenBarrelsCrates_Gothic", new[] { woodMat, ironMat }, addCollider: true, castShadows: false, receiveShadows: true);
            BuildFbxPrefab("WallRuneTablet_Gothic", new[] { stoneMat, runeMat }, addCollider: false, castShadows: false, receiveShadows: true);
            BuildFbxPrefab("GothicLightShaft_Cone", new[] { warmShaftMat }, addCollider: false, castShadows: false, receiveShadows: false);

            // 2. Configure 9-slicing on UI Panels & Buttons + Build UI Icon Sprite Atlas
            ConfigureNineSliceBorders();
            BuildHudSpriteAtlas();

            // 3. Build Reusable Gothic HUD Prefabs under Assets/Prefabs/UI/
            BuildReusableHudPrefabs();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GothicCathedralAssetSetup] Built 5 Blender Gothic prefabs, URP light-shaft materials, UI Sprite Atlas, and 7 reusable Gothic HUD prefabs.");
        }

        private static void EnsureFolders()
        {
            string[] dirs = { ResEnvKitDir, PrefabEnvDir, ResMatDir, PrefabUiDir };
            foreach (var d in dirs)
            {
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
            }
        }

        private static void BuildPersistentMaterials(
            out Material stoneMat,
            out Material ironMat,
            out Material woodMat,
            out Material runeMat,
            out Material warmShaftMat,
            out Material arcaneShaftMat)
        {
            stoneMat = AssetDatabase.LoadAssetAtPath<Material>(ResMatDir + "/M_GothicStone.mat") ?? PbrMaterialLibrary.GothicStone();
            ironMat  = AssetDatabase.LoadAssetAtPath<Material>(ResMatDir + "/M_WroughtIron.mat") ?? PbrMaterialLibrary.WroughtIron();
            woodMat  = AssetDatabase.LoadAssetAtPath<Material>(ResMatDir + "/M_AgedOakWood.mat") ?? PbrMaterialLibrary.AgedOakWood();

            Shader crystalSh = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Resources/Shaders/AbyssalCrystalLit.shader")
                               ?? Shader.Find("AbyssalDungeon/CrystalLit")
                               ?? Shader.Find("Universal Render Pipeline/Lit");
            runeMat = CreateOrUpdateMaterial(ResMatDir + "/M_WallRune_Arcane.mat", crystalSh, m =>
            {
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.20f, 0.56f, 0.98f, 1f));
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", new Color(0.32f, 0.78f, 1.0f, 1f) * 1.9f);
                if (m.HasProperty("_RimColor")) m.SetColor("_RimColor", new Color(0.45f, 0.85f, 1.0f, 1f) * 2.1f);
                if (m.HasProperty("_PulseSpeed")) m.SetFloat("_PulseSpeed", 1.8f);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.85f);
                m.EnableKeyword("_EMISSION");
                m.enableInstancing = true;
            });

            Shader shaftSh = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Resources/Shaders/AbyssalLightShaft.shader")
                             ?? Shader.Find("AbyssalDungeon/LightShaft")
                             ?? Shader.Find("Universal Render Pipeline/Unlit");

            warmShaftMat = CreateOrUpdateMaterial(ResMatDir + "/M_GothicLightShaft_Warm.mat", shaftSh, m =>
            {
                if (m.HasProperty("_Color")) m.SetColor("_Color", new Color(1.00f, 0.86f, 0.62f, 0.13f));
                if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", 0.85f);
                if (m.HasProperty("_FresnelPower")) m.SetFloat("_FresnelPower", 2.6f);
                if (m.HasProperty("_ScrollSpeed")) m.SetFloat("_ScrollSpeed", 0.20f);
                if (m.HasProperty("_FadeDist")) m.SetFloat("_FadeDist", 3.8f);
                m.enableInstancing = true;
            });

            arcaneShaftMat = CreateOrUpdateMaterial(ResMatDir + "/M_GothicLightShaft_Arcane.mat", shaftSh, m =>
            {
                if (m.HasProperty("_Color")) m.SetColor("_Color", new Color(0.46f, 0.78f, 1.00f, 0.15f));
                if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", 0.90f);
                if (m.HasProperty("_FresnelPower")) m.SetFloat("_FresnelPower", 2.6f);
                if (m.HasProperty("_ScrollSpeed")) m.SetFloat("_ScrollSpeed", 0.22f);
                if (m.HasProperty("_FadeDist")) m.SetFloat("_FadeDist", 3.8f);
                m.enableInstancing = true;
            });
        }

        private static Material CreateOrUpdateMaterial(string assetPath, Shader shader, Action<Material> configure)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(shader);
                configure?.Invoke(mat);
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            else
            {
                if (shader != null && mat.shader != shader) mat.shader = shader;
                configure?.Invoke(mat);
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static void BuildFbxPrefab(string baseName, Material[] slotMaterials, bool addCollider, bool castShadows, bool receiveShadows)
        {
            string fbxPath = $"{RawFbxDir}/{baseName}.fbx";
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.importAnimation) { importer.importAnimation = false; dirty = true; }
                if (importer.importCameras) { importer.importCameras = false; dirty = true; }
                if (importer.importLights) { importer.importLights = false; dirty = true; }
                if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
                {
                    importer.materialImportMode = ModelImporterMaterialImportMode.None;
                    dirty = true;
                }
                if (dirty) importer.SaveAndReimport();
            }

            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxAsset == null)
            {
                Debug.LogWarning($"[GothicCathedralAssetSetup] Missing FBX at {fbxPath}");
                return;
            }

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
            inst.name = baseName;

            Bounds combined = new Bounds(Vector3.zero, Vector3.zero);
            bool hasBounds = false;
            var mrs = inst.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < mrs.Length; i++)
            {
                MeshRenderer mr = mrs[i];
                var mf = mr.GetComponent<MeshFilter>();
                int subCount = (mf != null && mf.sharedMesh != null) ? Mathf.Max(1, mf.sharedMesh.subMeshCount) : 1;
                var mats = new Material[subCount];
                for (int s = 0; s < subCount; s++)
                    mats[s] = slotMaterials[Mathf.Min(s, slotMaterials.Length - 1)];
                mr.sharedMaterials = mats;
                mr.shadowCastingMode = castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = receiveShadows;
                if (!hasBounds) { combined = mr.bounds; hasBounds = true; }
                else combined.Encapsulate(mr.bounds);
            }

            if (addCollider && hasBounds)
            {
                var bc = inst.AddComponent<BoxCollider>();
                bc.center = inst.transform.InverseTransformPoint(combined.center);
                bc.size = new Vector3(
                    Mathf.Max(0.4f, combined.size.x * 0.88f),
                    Mathf.Max(0.4f, combined.size.y),
                    Mathf.Max(0.4f, combined.size.z * 0.88f));
            }

            // Save reusable prefab in Resources/EnvironmentKit (runtime loader) AND Assets/Prefabs/Environment (editor library)
            PrefabUtility.SaveAsPrefabAsset(inst, $"{ResEnvKitDir}/{baseName}.prefab");
            PrefabUtility.SaveAsPrefabAsset(inst, $"{PrefabEnvDir}/{baseName}.prefab");
            UnityEngine.Object.DestroyImmediate(inst);
        }

        private static void ConfigureNineSliceBorders()
        {
            ApplyNineSliceInFolder("Assets/Resources/UI/Panels", new Vector4(32, 32, 32, 32));
            ApplyNineSliceInFolder("Assets/Resources/UI/Buttons", new Vector4(24, 20, 24, 20));
        }

        private static void ApplyNineSliceInFolder(string folder, Vector4 border)
        {
            if (!Directory.Exists(folder)) return;
            foreach (string raw in Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly))
            {
                string path = raw.Replace("\\", "/");
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                bool dirty = false;
                if (ti.textureType != TextureImporterType.Sprite) { ti.textureType = TextureImporterType.Sprite; dirty = true; }
                if (ti.spriteImportMode != SpriteImportMode.Single) { ti.spriteImportMode = SpriteImportMode.Single; dirty = true; }
                if (ti.spriteBorder != border) { ti.spriteBorder = border; dirty = true; }
                if (dirty) ti.SaveAndReimport();
            }
        }

        private static void BuildHudSpriteAtlas()
        {
            const string atlasPath = "Assets/Resources/UI/AbyssalHUD_Icons.spriteatlas";
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            bool created = false;
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                created = true;
            }

            var packSettings = new SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                padding = 4,
                enableRotation = false,
                enableTightPacking = false
            };
            atlas.SetPackingSettings(packSettings);

            var texSettings = new SpriteAtlasTextureSettings
            {
                readable = false,
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear
            };
            atlas.SetTextureSettings(texSettings);

            var iconsFolder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Resources/UI/Icons");
            var classesFolder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Resources/UI/Classes");
            var packables = new System.Collections.Generic.List<UnityEngine.Object>();
            if (iconsFolder != null) packables.Add(iconsFolder);
            if (classesFolder != null) packables.Add(classesFolder);

            var existing = atlas.GetPackables();
            if (existing != null && existing.Length > 0) atlas.Remove(existing);
            if (packables.Count > 0) atlas.Add(packables.ToArray());

            if (created) AssetDatabase.CreateAsset(atlas, atlasPath);
            else EditorUtility.SetDirty(atlas);
        }

        private static void BuildReusableHudPrefabs()
        {
            // Build a single clean master Canvas holding all 6 Gothic HUD modules and save each module + master as reusable prefabs
            var canvasGo = new GameObject("DungeonHUD_Master");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            Transform root = canvasGo.transform;

            // 1. Party Panel (Top-Left)
            var partyPanel = GameUIKit.MakeStyledPanel("HUD_PartyPanel", root, UIPanelStyle.Lobby,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -338f), new Vector2(364f, -14f));
            GameUIKit.MakeIcon("PartyCrest", partyPanel.transform, GameUIKit.GetIcon("shield"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -34f), new Vector2(36f, -10f));
            GameUIKit.MakeText("PartyHeader", partyPanel.transform, "TỔ ĐỘI THỢ SĂN", 13, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0.66f, 1f), new Vector2(42f, -36f), new Vector2(0f, -8f));
            GameUIKit.MakeText("PartyStateTag", partyPanel.transform, "CO-OP 4/4", 11, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleRight,
                new Vector2(0.60f, 1f), new Vector2(1f, 1f), new Vector2(0f, -36f), new Vector2(-20f, -8f));
            GameUIKit.MakeRect("Members", partyPanel.transform, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -40f));
            PrefabUtility.SaveAsPrefabAsset(partyPanel.gameObject, $"{PrefabUiDir}/HUD_PartyPanel.prefab");

            // 2. Quest Notification Panel (Top-Center)
            var questPanel = GameUIKit.MakeStyledPanel("HUD_QuestNotification", root, UIPanelStyle.Notification,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-390f, -92f), new Vector2(390f, -14f));
            questPanel.gameObject.AddComponent<CanvasGroup>();
            GameUIKit.MakeIcon("QuestIcon", questPanel.transform, GameUIKit.GetIcon("crown"),
                new Vector2(0f, 0.20f), new Vector2(0f, 0.84f), new Vector2(16f, 0f), new Vector2(54f, 0f));
            GameUIKit.MakeText("QuestTitle", questPanel.transform, "NHIỆM VỤ HẦM NGỤC", 16, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.48f), new Vector2(1f, 0.94f), new Vector2(62f, 0f), new Vector2(-62f, 0f));
            GameUIKit.MakeText("QuestDesc", questPanel.transform, "Khám phá đại thánh đường và đánh bại Abyssal Knight.", 12, FontStyle.Italic, GameUIKit.ColParchment, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.08f), new Vector2(1f, 0.48f), new Vector2(62f, 0f), new Vector2(-62f, 0f));
            PrefabUtility.SaveAsPrefabAsset(questPanel.gameObject, $"{PrefabUiDir}/HUD_QuestNotification.prefab");

            // 3. Minimap Panel (Top-Right)
            var mapPanel = GameUIKit.MakeStyledPanel("HUD_MinimapPanel", root, UIPanelStyle.Ornate,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-248f, -254f), new Vector2(-18f, -14f));
            GameUIKit.MakeText("MapTitle", mapPanel.transform, "BẢN ĐỒ CỔ NGỮ • TẦNG 1", 12, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -28f), new Vector2(-8f, -4f));
            var radarRt = GameUIKit.MakeRect("Radar", mapPanel.transform, Vector2.zero, Vector2.one, new Vector2(12f, 26f), new Vector2(-12f, -30f));
            radarRt.gameObject.AddComponent<RawImage>().color = Color.white;
            GameUIKit.MakeText("MapLegend", mapPanel.transform, "Vàng: Bạn  •  Đỏ: Cửa khóa  •  Tím: Kho báu/Boss", 9, FontStyle.Normal, GameUIKit.ColMuted, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(8f, 6f), new Vector2(-8f, 24f));
            PrefabUtility.SaveAsPrefabAsset(mapPanel.gameObject, $"{PrefabUiDir}/HUD_MinimapPanel.prefab");

            // 4. Right-Side Menu Panel (4 Gothic buttons: [I], [M], [K], [Esc])
            var rightMenu = GameUIKit.MakeRect("HUD_RightMenuPanel", root,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-248f, -438f), new Vector2(-18f, -260f));
            GameUIKit.MakeStyledButton("BtnInv", rightMenu, "Hành Trang & Build [I]", 12, UIButtonStyle.Primary,
                new Vector2(0f, 0.76f), new Vector2(1f, 1.00f), Vector2.zero, Vector2.zero, null, GameUIKit.GetIcon("bag"));
            GameUIKit.MakeStyledButton("BtnFullMap", rightMenu, "Bản Đồ Chiến Thuật [M]", 12, UIButtonStyle.Secondary,
                new Vector2(0f, 0.51f), new Vector2(1f, 0.74f), Vector2.zero, Vector2.zero, null, GameUIKit.GetIcon("portal"));
            GameUIKit.MakeStyledButton("BtnSkills", rightMenu, "Kỹ Năng [K]", 12, UIButtonStyle.Secondary,
                new Vector2(0f, 0.26f), new Vector2(1f, 0.49f), Vector2.zero, Vector2.zero, null, GameUIKit.GetIcon("skill_tree"));
            GameUIKit.MakeStyledButton("BtnPause", rightMenu, "Tạm Dừng [Esc]", 12, UIButtonStyle.Secondary,
                new Vector2(0f, 0.00f), new Vector2(1f, 0.24f), Vector2.zero, Vector2.zero, null, GameUIKit.GetIcon("settings"));
            PrefabUtility.SaveAsPrefabAsset(rightMenu.gameObject, $"{PrefabUiDir}/HUD_RightMenuPanel.prefab");

            // 5. Player Status Panel (Bottom-Left)
            var playerPanel = GameUIKit.MakeStyledPanel("HUD_PlayerStatusPanel", root, UIPanelStyle.Ornate,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 16f), new Vector2(468f, 198f));
            GameUIKit.MakeStyledPanel("PortraitBox", playerPanel.transform, UIPanelStyle.Character,
                new Vector2(0f, 0.25f), new Vector2(0f, 0.92f), new Vector2(14f, 0f), new Vector2(102f, 0f));
            GameUIKit.MakeText("HeroMeta", playerPanel.transform, "KNIGHT • Lv.1", 15, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.77f), new Vector2(0.68f, 0.95f), new Vector2(110f, 0f), Vector2.zero);
            GameUIKit.MakeText("GoldText", playerPanel.transform, "0 G", 14, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleRight,
                new Vector2(0.68f, 0.77f), new Vector2(1f, 0.95f), Vector2.zero, new Vector2(-14f, 0f));
            GameUIKit.MakeBar("HpBar", playerPanel.transform, new Color(0.76f, 0.14f, 0.15f),
                new Vector2(0f, 0.56f), new Vector2(1f, 0.75f), new Vector2(110f, 0f), new Vector2(-14f, 0f));
            GameUIKit.MakeBar("MpBar", playerPanel.transform, new Color(0.16f, 0.44f, 0.84f),
                new Vector2(0f, 0.38f), new Vector2(1f, 0.54f), new Vector2(110f, 0f), new Vector2(-14f, 0f));
            GameUIKit.MakeBar("StaBar", playerPanel.transform, new Color(0.24f, 0.62f, 0.34f),
                new Vector2(0f, 0.21f), new Vector2(1f, 0.36f), new Vector2(110f, 0f), new Vector2(-14f, 0f));
            GameUIKit.MakeBar("ExpBar", playerPanel.transform, new Color(0.78f, 0.58f, 0.24f),
                new Vector2(0f, 0.05f), new Vector2(1f, 0.18f), new Vector2(14f, 0f), new Vector2(-14f, 0f));
            PrefabUtility.SaveAsPrefabAsset(playerPanel.gameObject, $"{PrefabUiDir}/HUD_PlayerStatusPanel.prefab");

            // 6. Skill Hotbar Panel (Bottom-Center)
            var skillPanel = GameUIKit.MakeStyledPanel("HUD_SkillHotbarPanel", root, UIPanelStyle.Ornate,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-438f, 16f), new Vector2(468f, 128f));
            PrefabUtility.SaveAsPrefabAsset(skillPanel.gameObject, $"{PrefabUiDir}/HUD_SkillHotbarPanel.prefab");

            // Master HUD Canvas Prefab
            PrefabUtility.SaveAsPrefabAsset(canvasGo, $"{PrefabUiDir}/DungeonHUD_Master.prefab");
            UnityEngine.Object.DestroyImmediate(canvasGo);
        }
    }
}
#endif

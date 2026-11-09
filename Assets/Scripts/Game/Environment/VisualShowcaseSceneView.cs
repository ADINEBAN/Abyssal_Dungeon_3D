using UnityEngine;
using UnityEngine.UI;

namespace AbyssGame
{
    /// <summary>
    /// Phase 1 AAA Dark Fantasy Sample Area ("VisualShowcase" scene) modeled directly after Reference Image 4:
    /// - Vaulted Gothic Cathedral Nave with double fluted colonnades, pointed arches & clerestory light shafts
    /// - Recessed stone floor channels, left-aisle broken pillar & scattered masonry rubble
    /// - Grand stone staircase rising to an illuminated Abyssal Sanctuary Altar, Ironbound Treasure Chest & Guardian Statues
    /// - Interactive live switcher [1..7] for all 7 Dark Fantasy Biomes + toggleable Cinematic Camera / 3rd-Person Hero [C].
    /// </summary>
    public class VisualShowcaseSceneView : MonoBehaviour
    {
        private ActorUnit _hero;
        private bool _cinematicMode = true;
        private float _cineAngle;
        private Canvas _hudCanvas;
        private Text _biomeLabel;
        private Text _modeLabel;

        public void Build()
        {
            AbyssalPostProcessing.EnsureGlobalVolumeAndCameraSetup(transform);
            var atmo = BiomeAtmosphereController.EnsureOnScene(transform);
            atmo.ApplyBiome(DungeonBiome.ForgottenTemple, instant: true);
            GameAudio.I.PlayMusic(GameMusic.Dungeon);

            BuildCathedralNaveArchitecture();
            SpawnWalkableHero();
            BuildShowcaseHUD();
            ApplyCameraMode(true);
        }

        private void BuildCathedralNaveArchitecture()
        {
            var hall = new GameObject("CathedralShowcaseNave").transform;
            hall.SetParent(transform, false);

            // Main Nave: 24m wide x 36m long, 6.2m tall vaulted hall
            Vector3 naveCenter = new Vector3(0f, 0f, 14f);
            ModularDungeonArchitect.BuildModularRoomShell(
                hall, naveCenter, width: 24f, length: 36f, wallHeight: 6.2f,
                biome: DungeonBiome.ForgottenTemple,
                openSouthDoor: false, openNorthDoor: false, doorwayWidth: 6f);

            Material stoneMat = PbrMaterialLibrary.GothicStone(new Color(0.86f, 0.78f, 0.68f));
            Material floorMat = PbrMaterialLibrary.CathedralFloor(new Color(0.80f, 0.73f, 0.65f));
            Material woodMat = PbrMaterialLibrary.AgedOakWood();
            Material ironMat = PbrMaterialLibrary.WroughtIron();
            Material crystalMat = PbrMaterialLibrary.AbyssalCrystal(new Color(0.16f, 0.55f, 0.92f), new Color(0.38f, 0.90f, 1.0f), 2.4f);

            // Reference Image 4 Left-Aisle Ruined Pillar & Masonry Rubble Foreground
            ObjMeshLoader.SpawnPiece("AncientRuins_Rubble", hall,
                new Vector3(-3.8f, 0f, 7.2f), Quaternion.Euler(0f, 22f, 0f), Vector3.one * 1.35f, stoneMat);
            ObjMeshLoader.SpawnPiece("AncientRuins_Rubble", hall,
                new Vector3(-5.2f, 0f, 9.6f), Quaternion.Euler(0f, -64f, 0f), Vector3.one * 1.15f, stoneMat);

            // Grand Stone Staircase leading up to Raised North Sanctuary Dais (y = 1.44m)
            ObjMeshLoader.SpawnPiece("StoneStairs_Grand", hall,
                new Vector3(0f, 0f, 22.8f), Quaternion.identity, new Vector3(1.45f, 1f, 1.15f), stoneMat, addBoxCollider: false);

            // Sloped ramp collider for smooth player ascent up the grand stairs
            var stairRamp = new GameObject("StairsWalkRamp");
            stairRamp.transform.SetParent(hall, false);
            stairRamp.transform.position = new Vector3(0f, 0.72f, 22.8f);
            stairRamp.transform.rotation = Quaternion.Euler(-19.2f, 0f, 0f);
            stairRamp.AddComponent<BoxCollider>().size = new Vector3(5.6f, 0.2f, 4.4f);

            // Raised North Sanctuary Platform (z = 24.8 .. 31.8, y = 1.44m)
            var dais = new GameObject("SanctuaryDais").transform;
            dais.SetParent(hall, false);
            var daisCol = new GameObject("DaisCollider");
            daisCol.transform.SetParent(dais, false);
            daisCol.transform.position = new Vector3(0f, 0.72f, 28.2f);
            daisCol.AddComponent<BoxCollider>().size = new Vector3(16f, 1.44f, 7.2f);

            for (int ix = -1; ix <= 1; ix++)
            {
                ObjMeshLoader.SpawnPiece("DungeonFloor_ChannelTile", dais,
                    new Vector3(ix * 4.8f, 1.44f, 28.0f), Quaternion.identity,
                    new Vector3(1.2f, 1f, 1.75f), floorMat, false, false);
            }

            // Illuminated Sanctuary Centerpiece: BossThroneAltar + Ironbound Chest + Guardian Sentues + Crystal Clusters
            ObjMeshLoader.SpawnPiece("BossThroneAltar", dais,
                new Vector3(0f, 1.44f, 29.8f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.05f, stoneMat, addBoxCollider: true);

            var chestRoot = new GameObject("ShowcaseIronboundChest").transform;
            chestRoot.SetParent(dais, false);
            chestRoot.position = new Vector3(0f, 1.44f, 27.1f);
            chestRoot.rotation = Quaternion.Euler(0f, 180f, 0f);
            ObjMeshLoader.SpawnPiece("TreasureChest_IronboundBase", chestRoot, Vector3.zero, Quaternion.identity, Vector3.one, woodMat, true);
            ObjMeshLoader.SpawnPiece("TreasureChest_IronboundLid", chestRoot, new Vector3(0f, 0.54f, -0.38f), Quaternion.Euler(-18f, 0f, 0f), Vector3.one, ironMat);

            ObjMeshLoader.SpawnPiece("GuardianStatue_Gothic", dais,
                new Vector3(-4.8f, 1.44f, 28.6f), Quaternion.Euler(0f, 165f, 0f), Vector3.one * 1.15f, stoneMat, addBoxCollider: true);
            ObjMeshLoader.SpawnPiece("GuardianStatue_Gothic", dais,
                new Vector3(4.8f, 1.44f, 28.6f), Quaternion.Euler(0f, -165f, 0f), Vector3.one * 1.15f, stoneMat, addBoxCollider: true);

            ModularDungeonArchitect.SpawnFireBrazier(dais, new Vector3(-2.6f, 1.44f, 26.8f),
                new Color(1f, 0.62f, 0.22f), new Color(1f, 0.90f, 0.55f), true);
            ModularDungeonArchitect.SpawnFireBrazier(dais, new Vector3(2.6f, 1.44f, 26.8f),
                new Color(1f, 0.62f, 0.22f), new Color(1f, 0.90f, 0.55f), true);

            // Side-aisle Crystal & Root Props showcasing all modular assets
            ObjMeshLoader.SpawnPiece("AbyssalCrystalCluster", hall,
                new Vector3(8.2f, 0f, 12.5f), Quaternion.Euler(0f, -45f, 0f), Vector3.one * 1.25f, crystalMat, addBoxCollider: true);
            ObjMeshLoader.SpawnPiece("RunicPillarAltar", hall,
                new Vector3(-8.2f, 0f, 15.5f), Quaternion.identity, Vector3.one, stoneMat, addBoxCollider: true);
            ObjMeshLoader.SpawnPiece("CursedTreeRoot", hall,
                new Vector3(-9.5f, 0f, 20.5f), Quaternion.Euler(0f, 55f, 0f), Vector3.one * 1.1f, woodMat, addBoxCollider: false);

            // Upper Clerestory Light Shafts (Reference Image 4 upper-left window sunbeams hitting right pillars!)
            BuildClerestoryLightShafts(hall);
        }

        private void BuildClerestoryLightShafts(Transform parent)
        {
            Shader addSh = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            var shaftMat = new Material(addSh) { name = "M_ClerestoryShaft" };
            Color shaftCol = new Color(0.72f, 0.80f, 1.0f, 0.055f);
            if (shaftMat.HasProperty("_Surface")) shaftMat.SetFloat("_Surface", 1f);
            if (shaftMat.HasProperty("_Blend")) shaftMat.SetFloat("_Blend", 2f);
            if (shaftMat.HasProperty("_SrcBlend")) shaftMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (shaftMat.HasProperty("_DstBlend")) shaftMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            if (shaftMat.HasProperty("_ZWrite")) shaftMat.SetFloat("_ZWrite", 0f);
            if (shaftMat.HasProperty("_BaseColor")) shaftMat.SetColor("_BaseColor", shaftCol);
            shaftMat.color = shaftCol;

            float[] zPositions = { 8.5f, 15.5f, 22.0f };
            for (int i = 0; i < zPositions.Length; i++)
            {
                var beam = GameObject.CreatePrimitive(PrimitiveType.Quad);
                beam.name = "ClerestorySunShaft_" + i;
                Destroy(beam.GetComponent<Collider>());
                beam.transform.SetParent(parent, false);
                beam.transform.position = new Vector3(-2.5f, 4.4f, zPositions[i]);
                beam.transform.rotation = Quaternion.Euler(12f, 25f, -38f);
                beam.transform.localScale = new Vector3(3.4f, 9.5f, 1f);
                var mr = beam.GetComponent<MeshRenderer>();
                mr.sharedMaterial = shaftMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
        }

        private void SpawnWalkableHero()
        {
            var heroGo = new GameObject("ShowcaseHero");
            heroGo.transform.SetParent(transform, false);
            heroGo.transform.position = new Vector3(0f, 0f, 4.5f);
            _hero = heroGo.AddComponent<ActorUnit>();
            _hero.InitHero(GameSession.I.Profile.selectedClass, GameSession.I.Profile.selectedBuild,
                GameSession.I.Profile.level, GameSession.I.Profile.heroName, true, 0);
            heroGo.AddComponent<HeroPlayerInput>();
        }

        private void ApplyCameraMode(bool cinematic)
        {
            _cinematicMode = cinematic;
            if (_hero != null && _hero.GetComponent<HeroPlayerInput>() is HeroPlayerInput hInp)
                hInp.enabled = !cinematic;
            if (Camera.main == null) return;

            var orbit = Camera.main.GetComponent<OrbitCamera3D>();
            if (cinematic)
            {
                if (orbit != null) orbit.enabled = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                // Frame exact Reference Image 4 composition!
                Camera.main.fieldOfView = 54f;
                Camera.main.transform.position = new Vector3(0f, 2.35f, 0.2f);
                Camera.main.transform.rotation = Quaternion.Euler(4.5f, 0f, 0f);
            }
            else
            {
                if (orbit == null) orbit = Camera.main.gameObject.AddComponent<OrbitCamera3D>();
                orbit.enabled = true;
                orbit.Target = _hero != null ? _hero.transform : null;
                orbit.AllowCursorLock = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (_modeLabel != null)
            {
                _modeLabel.text = _cinematicMode
                    ? "Camera: GÓC QUAY ĐIỆN ẢNH MẪU [Nhấn C để Điều Khiển Nhân Vật 3D]"
                    : "Camera: ĐIỀU KHIỂN NHÂN VẬT WASD [Nhấn C để về Góc Quay Điện Ảnh]";
            }
        }

        private void BuildShowcaseHUD()
        {
            _hudCanvas = GameUIKit.CreateCanvas("ShowcaseHUD");
            var root = _hudCanvas.transform;

            var top = GameUIKit.MakePanel("TopBar", root, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(20f, -74f), new Vector2(-20f, -12f), new Color(0.05f, 0.05f, 0.09f, 0.84f), GameUIKit.ColGold);

            _biomeLabel = GameUIKit.MakeText("BiomeTitle", top.transform,
                "KHU VỰC MẪU 3D DARK FANTASY — Đền Thờ Lãng Quên (Forgotten Temple)",
                18, FontStyle.Bold, GameUIKit.ColGold, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0.56f, 1f), new Vector2(18f, 0f), Vector2.zero);

            _modeLabel = GameUIKit.MakeText("CamMode", top.transform, "",
                14, FontStyle.Normal, GameUIKit.ColMuted, TextAnchor.MiddleRight,
                new Vector2(0.45f, 0f), new Vector2(0.74f, 1f), Vector2.zero, new Vector2(-12f, 0f));

            GameUIKit.MakeButton("BtnToggleCam", top.transform, "Đổi Camera [C]", 14, GameUIKit.ColAccent, Color.white,
                new Vector2(0.75f, 0.16f), new Vector2(0.87f, 0.84f), Vector2.zero, Vector2.zero,
                () => ApplyCameraMode(!_cinematicMode));

            GameUIKit.MakeButton("BtnBack", top.transform, "← Main Menu", 14, GameUIKit.ColCard, Color.white,
                new Vector2(0.88f, 0.16f), new Vector2(0.99f, 0.84f), Vector2.zero, Vector2.zero,
                () => GameSession.GoToScene("MainMenu"));

            // Bottom Biome Switcher Bar (1..7)
            var bot = GameUIKit.MakePanel("BiomeBar", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-560f, 14f), new Vector2(560f, 66f), new Color(0.05f, 0.05f, 0.09f, 0.84f), new Color(1f, 1f, 1f, 0.16f));

            string[] shortNames =
            {
                "[1] Temple", "[2] Dungeon", "[3] Fortress", "[4] Abyss Cave",
                "[5] Crystal", "[6] Forest", "[7] Boss Arena"
            };

            for (int i = 0; i < 7; i++)
            {
                int idx = i;
                float a0 = i / 7f + 0.005f;
                float a1 = (i + 1) / 7f - 0.005f;
                GameUIKit.MakeButton("BiomeBtn_" + i, bot.transform, shortNames[i], 13, GameUIKit.ColCard, Color.white,
                    new Vector2(a0, 0.14f), new Vector2(a1, 0.86f), Vector2.zero, Vector2.zero,
                    () => SwitchBiome((DungeonBiome)idx));
            }
        }

        private void SwitchBiome(DungeonBiome biome)
        {
            if (BiomeAtmosphereController.Instance != null)
                BiomeAtmosphereController.Instance.ApplyBiome(biome, instant: false);

            var d = BiomeAtmosphereLibrary.Get(biome);
            if (_biomeLabel != null)
                _biomeLabel.text = $"KHU VỰC MẪU 3D DARK FANTASY — {d.vietnameseTitle}";
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
                ApplyCameraMode(!_cinematicMode);

            if (Input.GetKeyDown(KeyCode.H) && _hudCanvas != null)
                _hudCanvas.gameObject.SetActive(!_hudCanvas.gameObject.activeSelf);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GameSession.GoToScene("MainMenu");
                return;
            }

            for (int i = 0; i < 7; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    SwitchBiome((DungeonBiome)i);
            }

            if (_cinematicMode && Camera.main != null)
            {
                _cineAngle += Time.deltaTime * 0.28f;
                float swayX = Mathf.Sin(_cineAngle) * 1.35f;
                float dollyZ = 0.2f + (Mathf.Cos(_cineAngle * 0.7f) * 0.5f + 0.5f) * 4.2f;
                Camera.main.transform.position = new Vector3(swayX, 2.38f, dollyZ);
                Vector3 lookTarget = new Vector3(0f, 2.15f, 25.5f);
                Camera.main.transform.rotation = Quaternion.LookRotation((lookTarget - Camera.main.transform.position).normalized, Vector3.up);
            }
        }
    }
}


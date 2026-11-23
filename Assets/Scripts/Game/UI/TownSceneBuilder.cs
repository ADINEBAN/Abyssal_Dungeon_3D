using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssGame
{
    /// <summary>
    /// 3D Town Hub ("Aether Haven") with Ancient Gothic HUD & Interactive Forging/Alchemy Stations.
    /// </summary>
    public class TownSceneBuilder : MonoBehaviour
    {
        [SerializeField] private bool _isPrebakedInScene;
        [SerializeField] private Canvas _townCanvas;
        [SerializeField] private ActorUnit _hero;
        [SerializeField] private Text _topInfoTxt;
        [SerializeField] private Text _interactPrompt;
        [SerializeField] private GameObject _interactBox;
        [SerializeField] private GameObject _charModal;
        [SerializeField] private GameObject _toastBox;
        [SerializeField] private Text _toastTxt;
        [SerializeField] private Button _btnBag;
        [SerializeField] private Button _btnLobby;
        [SerializeField] private Button _btnMenu;
        [SerializeField] private WorldExitPortal _blacksmithPortal;
        [SerializeField] private WorldExitPortal _alchemistPortal;
        [SerializeField] private WorldExitPortal _dungeonExitPortal;

        private float _toastTimer;
        private bool _runtimeInitialized;
        public bool IsPrebakedInScene => _isPrebakedInScene;

        private void Start()
        {
            if (_isPrebakedInScene && !_runtimeInitialized)
                InitializePrebaked();
        }

        public void InitializePrebaked()
        {
            if (_runtimeInitialized) return;
            _runtimeInitialized = true;

            HeroPlayerInput.Interactables.Clear();
            GameAudio.I.PlayMusic(GameMusic.Town);

            // Principle 2: Even though TownHero is pre-baked in Town.unity, always synchronize its Class, Build, Level & Equipment with GameSession
            if (_hero != null)
            {
                _hero.InitHero(GameSession.I.Profile.selectedClass, GameSession.I.Profile.selectedBuild,
                    GameSession.I.Profile.level, GameSession.I.Profile.heroName, true, 0);
                if (_hero.GetComponent<HeroPlayerInput>() == null)
                    _hero.gameObject.AddComponent<HeroPlayerInput>();
            }

            if (Camera.main != null && _hero != null)
            {
                var cam = Camera.main.GetComponent<OrbitCamera3D>();
                if (cam == null) cam = Camera.main.gameObject.AddComponent<OrbitCamera3D>();
                cam.Target = _hero.transform;
                cam.AllowCursorLock = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (_blacksmithPortal != null)
                _blacksmithPortal.BindCallback("[F] Rèn Trang Bị Cổ Ngữ Mới (80 Gold)", OnUseBlacksmith);
            if (_alchemistPortal != null)
                _alchemistPortal.BindCallback("[F] Điều Chế 2 Bình Hồi Máu (25 Gold)", OnUseAlchemist);
            if (_dungeonExitPortal != null)
                _dungeonExitPortal.BindCallback("[F] BƯỚC VÀO CỔNG HẦM NGỤC ABYSSAL DUNGEON", () => GameSession.GoToScene("Dungeon"));

            BindTownButton(_btnBag, () => { if (_charModal != null) _charModal.SetActive(!_charModal.activeSelf); });
            BindTownButton(_btnLobby, () => GameSession.GoToScene("Lobby"));
            BindTownButton(_btnMenu, () => GameSession.GoToScene("MainMenu"));

            if (_townCanvas != null)
                _charModal = GameUIKit.BuildCharacterSheetModal(_townCanvas.transform, () => _hero, null);

            HeroPlayerInput.OnToggleInventory = () => { if (_charModal != null) _charModal.SetActive(!_charModal.activeSelf); };
        }

        private static void BindTownButton(Button btn, System.Action onClick)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                GameAudio.I?.PlaySfx(GameSfx.Click, 0.6f);
                onClick?.Invoke();
            });
        }

        private void OnUseBlacksmith()
        {
            var p = GameSession.I.Profile;
            if (p.gold < 80) { ShowToast("Không đủ Vàng trong kho (cần 80 Gold)!"); return; }
            p.gold -= 80;
            EquipItem item = LootRoller.RollItem(p.highestFloor, 2, 0.45f);
            GameSession.I.AddItemToBag(item, autoEquipIfBetter: true);
            if (_hero != null) _hero.RecalculateStats(true);
            GameAudio.I.PlaySfx(GameSfx.Loot, 1f);
            ShowToast($"Đã rèn thành công: [{GameDB.RarityNames[(int)item.rarity]}] {item.name} ({item.Summary()})!");
        }

        private void OnUseAlchemist()
        {
            var p = GameSession.I.Profile;
            if (p.gold < 25) { ShowToast("Không đủ Vàng trong kho (cần 25 Gold)!"); return; }
            p.gold -= 25;
            p.potions += 2;
            GameSession.I.SaveProfile();
            GameAudio.I.PlaySfx(GameSfx.Heal, 0.9f);
            ShowToast($"Đã nhận +2 Bình Máu Cổ Ngữ (Hiện có: {p.potions})!");
        }

        public void Build()
        {
            HeroPlayerInput.Interactables.Clear();
            AbyssalPostProcessing.EnsureGlobalVolumeAndCameraSetup(transform);
            var atmo = BiomeAtmosphereController.EnsureOnScene(transform);
            atmo.ApplyBiome(DungeonBiome.ForgottenTemple, instant: true);
            if (Application.isPlaying)
                GameAudio.I.PlayMusic(GameMusic.Town);

            ModularDungeonArchitect.BuildModularRoomShell(
                transform, Vector3.zero, width: 32f, length: 32f, wallHeight: 5.8f,
                biome: DungeonBiome.ForgottenTemple, openSouthDoor: false, openNorthDoor: false);

            Material stoneMat = PbrMaterialLibrary.GothicStone();
            Material crystalMat = PbrMaterialLibrary.AbyssalCrystal(new Color(0.88f, 0.68f, 0.28f), new Color(1.0f, 0.84f, 0.42f), 2.2f);

            ObjMeshLoader.SpawnPiece("RunicPillarAltar", transform, Vector3.zero, Quaternion.identity, new Vector3(1.8f, 1.05f, 1.8f), stoneMat, addBoxCollider: true);
            ObjMeshLoader.SpawnPiece("AbyssalCrystalCluster", transform, new Vector3(0f, 1.85f, 0f), Quaternion.identity, Vector3.one * 0.95f, crystalMat, addBoxCollider: false);
            ObjMeshLoader.SpawnPiece("GuardianStatue_Gothic", transform, new Vector3(-3.8f, 0f, 12.2f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.15f, stoneMat, addBoxCollider: true);
            ObjMeshLoader.SpawnPiece("GuardianStatue_Gothic", transform, new Vector3(3.8f, 0f, 12.2f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.15f, stoneMat, addBoxCollider: true);

            // Walkable Aged-Oak Wooden Plank Decks & Iron Forge Grate in Town for distinct Stone vs Wood vs Metal footsteps
            ModularDungeonArchitect.BuildWoodenPlankDeck(transform, "TownCenterBridge", new Vector3(0f, 0f, -3.2f), 4.2f, 4.4f);
            ModularDungeonArchitect.BuildWoodenPlankDeck(transform, "BlacksmithWoodDeck", new Vector3(-8f, 0f, 3f), 5.6f, 5.6f);
            ModularDungeonArchitect.BuildWoodenPlankDeck(transform, "AlchemistWoodDeck", new Vector3(8f, 0f, 3f), 5.6f, 5.6f);
            ModularDungeonArchitect.BuildIronGrateDeck(transform, "ForgeIronGrate", new Vector3(-4.2f, 0f, 3f), 3.2f, 4.2f);

            // Blacksmith Station (West)
            _blacksmithPortal = CreateTownStation(new Vector3(-8f, 0f, 3f), "Thợ Rèn Cổ Ngữ", new Color(1f, 0.68f, 0.24f),
                "[F] Rèn Trang Bị Cổ Ngữ Mới (80 Gold)", OnUseBlacksmith);

            // Alchemist Station (East)
            _alchemistPortal = CreateTownStation(new Vector3(8f, 0f, 3f), "Giả Kim Thuật Sư", new Color(0.38f, 0.88f, 0.48f),
                "[F] Điều Chế 2 Bình Hồi Máu (25 Gold)", OnUseAlchemist);

            // North Abyssal Dungeon Portal
            _dungeonExitPortal = WorldExitPortal.Create(transform, new Vector3(0f, 0f, 11.5f),
                "[F] BƯỚC VÀO CỔNG HẦM NGỤC ABYSSAL DUNGEON", () => GameSession.GoToScene("Dungeon"));

            // Spawn Player Hero
            var heroGo = new GameObject("TownHero");
            heroGo.transform.position = new Vector3(0f, 0f, -6f);
            _hero = heroGo.AddComponent<ActorUnit>();
            _hero.InitHero(GameSession.I.Profile.selectedClass, GameSession.I.Profile.selectedBuild,
                GameSession.I.Profile.level, GameSession.I.Profile.heroName, true, 0);
            heroGo.AddComponent<HeroPlayerInput>();

            if (Camera.main != null)
            {
                var cam = Camera.main.GetComponent<OrbitCamera3D>();
                if (cam == null) cam = Camera.main.gameObject.AddComponent<OrbitCamera3D>();
                cam.Target = heroGo.transform;
                cam.AllowCursorLock = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            BuildTownHUD();
            _isPrebakedInScene = true;
            _runtimeInitialized = Application.isPlaying;
        }

        private WorldExitPortal CreateTownStation(Vector3 pos, string title, Color glowCol, string prompt, System.Action onUse)
        {
            var st = new GameObject(title);
            st.transform.SetParent(transform, false);
            st.transform.position = pos;

            var baseMat = PbrMaterialLibrary.GothicStone();
            var orbMat = CharacterVisualFactory.MakeMat(glowCol, 0.85f, 0.1f, glowCol * 2f);
            CharacterVisualFactory.Prim(PrimitiveType.Cube, "Anvil", st.transform, new Vector3(0f, 0.6f, 0f), new Vector3(1.6f, 1.2f, 1.6f), baseMat, default, true);
            CharacterVisualFactory.Prim(PrimitiveType.Sphere, "Beacon", st.transform, new Vector3(0f, 1.85f, 0f), Vector3.one * 0.55f, orbMat);

            var lbl = new GameObject("Sign").AddComponent<TextMesh>();
            lbl.transform.SetParent(st.transform, false);
            lbl.transform.localPosition = new Vector3(0f, 2.75f, 0f);
            lbl.text = title;
            lbl.fontSize = 40;
            lbl.characterSize = 0.08f;
            lbl.anchor = TextAnchor.MiddleCenter;
            lbl.fontStyle = FontStyle.Bold;
            lbl.color = glowCol;

            return WorldExitPortal.Create(st.transform, Vector3.zero, prompt, onUse);
        }

        private void BuildTownHUD()
        {
            var canvas = GameUIKit.CreateCanvas("TownHUD");
            _townCanvas = canvas;
            var root = canvas.transform;

            var top = GameUIKit.MakeStyledPanel("TopBar", root, UIPanelStyle.Ornate,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -82f), new Vector2(-24f, -12f));

            GameUIKit.MakeIcon("TownCrest", top.transform, GameUIKit.GetIcon("anvil"),
                new Vector2(0f, 0.16f), new Vector2(0.84f, 0.84f), new Vector2(18f, 0f), new Vector2(62f, 0f));

            _topInfoTxt = GameUIKit.MakeText("Info", top.transform, "", 18, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0.54f, 1f), new Vector2(72f, 0f), Vector2.zero);

            _btnBag = GameUIKit.MakeStyledButton("BtnBag", top.transform, "Hành Trang & Cổ Ngữ [I]", 14, UIButtonStyle.Primary,
                new Vector2(0.55f, 0.16f), new Vector2(0.71f, 0.84f), Vector2.zero, Vector2.zero,
                () => _charModal.SetActive(!_charModal.activeSelf), GameUIKit.GetIcon("bag"));

            _btnLobby = GameUIKit.MakeStyledButton("BtnLobby", top.transform, "Sảnh Tổ Đội (Lobby)", 14, UIButtonStyle.Secondary,
                new Vector2(0.72f, 0.16f), new Vector2(0.86f, 0.84f), Vector2.zero, Vector2.zero,
                () => GameSession.GoToScene("Lobby"), GameUIKit.GetIcon("crossed_swords"));

            _btnMenu = GameUIKit.MakeStyledButton("BtnMenu", top.transform, "Main Menu", 14, UIButtonStyle.Secondary,
                new Vector2(0.87f, 0.16f), new Vector2(0.985f, 0.84f), Vector2.zero, Vector2.zero,
                () => GameSession.GoToScene("MainMenu"), GameUIKit.GetIcon("crown"));

            var intImg = GameUIKit.MakeStyledPanel("InteractPrompt", root, UIPanelStyle.Notification,
                new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.16f), new Vector2(-280f, -26f), new Vector2(280f, 26f));
            _interactBox = intImg.gameObject;
            _interactPrompt = GameUIKit.MakeText("Txt", intImg.transform, "", 17, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _interactBox.SetActive(false);

            var toastImg = GameUIKit.MakeStyledPanel("ToastBanner", root, UIPanelStyle.Notification,
                new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), new Vector2(-440f, -26f), new Vector2(440f, 26f));
            _toastBox = toastImg.gameObject;
            _toastTxt = GameUIKit.MakeText("Toast", toastImg.transform, "", 17, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-16f, 0f));
            _toastBox.SetActive(false);

            _charModal = GameUIKit.BuildCharacterSheetModal(root, () => _hero, null);
            HeroPlayerInput.OnToggleInventory = () => _charModal.SetActive(!_charModal.activeSelf);
            GameUIKit.OptimizeCanvasHierarchy(canvas);
        }

        private void ShowToast(string msg)
        {
            if (_toastBox != null) _toastBox.SetActive(true);
            _toastTxt.text = msg;
            _toastTimer = 3.5f;
        }

        private string _lastTopTownInfo = "";
        private string _lastTownPrompt = "";

        private void Update()
        {
            var p = GameSession.I.Profile;
            ClassDef cls = GameDB.GetClass(p.selectedClass);
            if (_topInfoTxt != null)
            {
                string nextTop = $"THÁNH ĐỊA AETHER HAVEN  •  {cls.name} Lv.{p.level}  •  Vàng: <color=#F8DC85>{p.gold} G</color>  •  Bình máu: {p.potions}  •  Tầng: {p.selectedFloor}";
                if (nextTop != _lastTopTownInfo)
                {
                    _lastTopTownInfo = nextTop;
                    _topInfoTxt.text = nextTop;
                }
            }

            var inp = _hero != null ? _hero.GetComponent<HeroPlayerInput>() : null;
            string prompt = (inp != null && inp.NearestInteractable != null) ? inp.NearestInteractable.GetPrompt() : null;
            bool showInt = !string.IsNullOrEmpty(prompt);
            if (_interactBox != null && _interactBox.activeSelf != showInt)
                _interactBox.SetActive(showInt);
            if (showInt && prompt != _lastTownPrompt)
            {
                _lastTownPrompt = prompt;
                _interactPrompt.text = prompt;
            }

            if (_toastTimer > 0f) _toastTimer -= Time.deltaTime;
            else if (_toastBox != null && _toastBox.activeSelf) _toastBox.SetActive(false);
        }
    }
}

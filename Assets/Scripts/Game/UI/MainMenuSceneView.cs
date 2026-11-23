using UnityEngine;
using UnityEngine.UI;

namespace AbyssGame
{
    /// <summary>
    /// Section 4.1 Main Menu Scene UI (ANCIENT GOTHIC – ANTIQUE GOLD & DARK BROWN):
    /// - Left Ornate Gothic Command Monument with engraved crest, Current Hunter Relic Plaque, and 7 antique metal buttons
    /// - Right/Center 3D Gothic Sanctuary Stage with warm torchlight, fog, and live 3D Hero preview
    /// - Ornate Dialog Modals for Instructions & Settings
    /// </summary>
    public class MainMenuSceneView : MonoBehaviour
    {
        [SerializeField] private bool _isPrebakedInScene;
        [SerializeField] private Canvas _mainCanvas;
        [SerializeField] private GameObject _howToModal;
        [SerializeField] private GameObject _settingsModal;
        [SerializeField] private Transform _stageRoot;
        [SerializeField] private GameObject _heroPreview;
        [SerializeField] private Animator _heroAnim;
        [SerializeField] private int _bakedClassId = -1;
        [SerializeField] private int _bakedBuildId = -1;
        [SerializeField] private Image _heroPortraitImg;
        [SerializeField] private Text _heroInfoTitleTxt;
        [SerializeField] private Text _heroInfoSubTxt;
        [SerializeField] private Text _stageHeroTxt;
        [SerializeField] private Button _btnPlay;
        [SerializeField] private Button _btnClasses;
        [SerializeField] private Button _btnTown;
        [SerializeField] private Button _btnShowcase;
        [SerializeField] private Button _btnHowTo;
        [SerializeField] private Button _btnSettings;
        [SerializeField] private Button _btnQuit;
        [SerializeField] private Button _btnCloseHowTo;

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

            GameAudio.I.PlayMusic(GameMusic.MainMenu);
            BindMenuButton(_btnPlay, () => GameSession.GoToScene("Lobby"));
            BindMenuButton(_btnClasses, () =>
            {
                GameSession.I.ReturnSceneAfterClassSelect = "MainMenu";
                GameSession.GoToScene("ClassSelection");
            });
            BindMenuButton(_btnTown, () => GameSession.GoToScene("Town"));
            BindMenuButton(_btnShowcase, () => GameSession.GoToScene("VisualShowcase"));
            BindMenuButton(_btnHowTo, () => { if (_howToModal != null) _howToModal.SetActive(true); });
            BindMenuButton(_btnSettings, () => { if (_settingsModal != null) _settingsModal.SetActive(true); });
            BindMenuButton(_btnQuit, QuitGame);
            BindMenuButton(_btnCloseHowTo, () => { if (_howToModal != null) _howToModal.SetActive(false); });

            if (_mainCanvas != null)
                _settingsModal = GameUIKit.BuildSettingsModal(_mainCanvas.transform, includeResetSave: true);

            RefreshHeroProfileAndPreview();
        }

        private static void BindMenuButton(Button btn, System.Action onClick)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                GameAudio.I?.PlaySfx(GameSfx.Click, 0.6f);
                onClick?.Invoke();
            });
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void RefreshHeroProfileAndPreview()
        {
            var p = GameSession.I.Profile;
            ClassDef curCls = GameDB.GetClass(p.selectedClass);
            string curBuildName = curCls.builds[Mathf.Clamp(p.selectedBuild, 0, curCls.builds.Length - 1)];

            if (_heroPortraitImg != null)
                _heroPortraitImg.sprite = GameUIKit.GetClassIcon(p.selectedClass);

            if (_heroInfoTitleTxt != null)
                _heroInfoTitleTxt.text = $"THỢ SĂN HIỆN TẠI: <color=#F8DC85>{p.heroName.ToUpper()}</color> (Lv.{p.level})";

            if (_heroInfoSubTxt != null)
                _heroInfoSubTxt.text =
                    $"Hệ phái: <color=#EDE0BA>{curCls.name} — {curBuildName}</color>\n" +
                    $"Kho ngân: <color=#F8DC85>{p.gold} Gold</color>  •  Tầng sâu nhất: <color=#F8DC85>Tầng {p.highestFloor}</color>";

            if (_stageHeroTxt != null)
                _stageHeroTxt.text =
                    $"<color=#F8DC85>{curCls.name.ToUpper()} — {curBuildName}</color>\n" +
                    $"<size=13>{curCls.roleText}  •  Nội tại: {curCls.passive}</size>";

            if (_stageRoot != null && (_heroPreview == null || _bakedClassId != p.selectedClass || _bakedBuildId != p.selectedBuild))
            {
                if (_heroPreview != null)
                    BakedAssetRegistry.DestroySafe(_heroPreview);
                _heroPreview = CharacterVisualFactory.BuildHeroVisual(curCls, _stageRoot, out _heroAnim, p.selectedBuild);
                _heroPreview.transform.position = new Vector3(1.35f, 0f, -0.6f);
                _heroPreview.transform.rotation = Quaternion.Euler(0f, -162f, 0f);
                _bakedClassId = p.selectedClass;
                _bakedBuildId = p.selectedBuild;
            }
        }

        public void Build()
        {
            if (Application.isPlaying)
                GameAudio.I.PlayMusic(GameMusic.MainMenu);
            Build3DMenuStage();

            var canvas = GameUIKit.CreateCanvas("MainMenuCanvas");
            _mainCanvas = canvas;
            var root = canvas.transform;

            // Left Ancient Gothic Command Monument
            var leftCard = GameUIKit.MakeStyledPanel(
                "LeftColumn", root, UIPanelStyle.Ornate,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(44f, 36f), new Vector2(610f, -36f));

            // Top Crest & Game Logo
            GameUIKit.MakeIcon("LogoCrest", leftCard.transform, GameUIKit.GetIcon("crossed_swords"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -88f), new Vector2(88f, -28f));

            GameUIKit.MakeText("LogoTitle", leftCard.transform, "ABYSSAL DUNGEON", 36, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(98f, -72f), new Vector2(-24f, -20f));

            GameUIKit.MakeText("Subtitle", leftCard.transform,
                "BIÊN NIÊN SỬ HẦM NGỤC CỔ ĐẠI • 3D CO-OP ACTION RPG",
                13, FontStyle.Bold, GameUIKit.ColMuted, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(98f, -96f), new Vector2(-24f, -68f));

            GameUIKit.MakeGothicDivider("LogoDiv", leftCard.transform,
                new Vector2(0.05f, 1f), new Vector2(0.95f, 1f), new Vector2(0f, -114f), new Vector2(0f, -96f));

            // Current Character Relic Plaque (Requirement 5.A: Thông tin nhân vật hiện tại)
            var p = GameSession.I.Profile;
            ClassDef curCls = GameDB.GetClass(p.selectedClass);
            string curBuildName = curCls.builds[Mathf.Clamp(p.selectedBuild, 0, curCls.builds.Length - 1)];

            var charPlaque = GameUIKit.MakeStyledPanel(
                "CurrentHeroPlaque", leftCard.transform, UIPanelStyle.Character,
                new Vector2(0.06f, 1f), new Vector2(0.94f, 1f),
                new Vector2(0f, -214f), new Vector2(0f, -120f));

            Sprite curIcon = GameUIKit.GetClassIcon(p.selectedClass);
            if (curIcon != null)
            {
                _heroPortraitImg = GameUIKit.MakeIcon("HeroPortrait", charPlaque.transform, curIcon,
                    new Vector2(0.03f, 0.12f), new Vector2(0.21f, 0.88f), Vector2.zero, Vector2.zero);
            }

            _heroInfoTitleTxt = GameUIKit.MakeText("HeroInfoTitle", charPlaque.transform,
                $"THỢ SĂN HIỆN TẠI: <color=#F8DC85>{p.heroName.ToUpper()}</color> (Lv.{p.level})",
                15, FontStyle.Bold, GameUIKit.ColGold, TextAnchor.UpperLeft,
                new Vector2(0.24f, 0.52f), new Vector2(0.97f, 0.90f), Vector2.zero, Vector2.zero);

            _heroInfoSubTxt = GameUIKit.MakeText("HeroInfoSub", charPlaque.transform,
                $"Hệ phái: <color=#EDE0BA>{curCls.name} — {curBuildName}</color>\n" +
                $"Kho ngân: <color=#F8DC85>{p.gold} Gold</color>  •  Tầng sâu nhất: <color=#F8DC85>Tầng {p.highestFloor}</color>",
                13, FontStyle.Normal, GameUIKit.ColMuted, TextAnchor.LowerLeft,
                new Vector2(0.24f, 0.10f), new Vector2(0.97f, 0.54f), Vector2.zero, Vector2.zero);

            // 7 Antique Metal Command Buttons
            float btnTop = -230f;
            float btnStep = 72f;

            _btnPlay = GameUIKit.MakeStyledButton("BtnPlay", leftCard.transform,
                "KHỞI HÀNH — SẢNH TỔ ĐỘI (LOBBY)", 18, UIButtonStyle.Primary,
                new Vector2(0.06f, 1f), new Vector2(0.94f, 1f),
                new Vector2(0f, btnTop - 58f), new Vector2(0f, btnTop),
                () => GameSession.GoToScene("Lobby"),
                GameUIKit.GetIcon("crossed_swords"));

            _btnClasses = GameUIKit.MakeStyledButton("BtnClasses", leftCard.transform,
                "ĐỀN CHỌN LỚP NHÂN VẬT (10 CLASSES)", 16, UIButtonStyle.Secondary,
                new Vector2(0.06f, 1f), new Vector2(0.94f, 1f),
                new Vector2(0f, btnTop - btnStep - 58f), new Vector2(0f, btnTop - btnStep),
                () =>
                {
                    GameSession.I.ReturnSceneAfterClassSelect = "MainMenu";
                    GameSession.GoToScene("ClassSelection");
                },
                GameUIKit.GetIcon("crown"));

            _btnTown = GameUIKit.MakeStyledButton("BtnTown", leftCard.transform,
                "THỊ TRẤN AETHER HAVEN (RÈN & NÂNG CẤP)", 16, UIButtonStyle.Secondary,
                new Vector2(0.06f, 1f), new Vector2(0.94f, 1f),
                new Vector2(0f, btnTop - btnStep * 2 - 58f), new Vector2(0f, btnTop - btnStep * 2),
                () => GameSession.GoToScene("Town"),
                GameUIKit.GetIcon("anvil"));

            _btnShowcase = GameUIKit.MakeStyledButton("BtnShowcase", leftCard.transform,
                "PHÒNG TRƯNG BÀY DI VẬT 3D (SHOWCASE)", 16, UIButtonStyle.Secondary,
                new Vector2(0.06f, 1f), new Vector2(0.94f, 1f),
                new Vector2(0f, btnTop - btnStep * 3 - 58f), new Vector2(0f, btnTop - btnStep * 3),
                () => GameSession.GoToScene("VisualShowcase"),
                GameUIKit.GetIcon("portal"));

            _btnHowTo = GameUIKit.MakeStyledButton("BtnHowTo", leftCard.transform,
                "CỔ THƯ HƯỚNG DẪN & ĐIỀU KHIỂN", 16, UIButtonStyle.Secondary,
                new Vector2(0.06f, 1f), new Vector2(0.94f, 1f),
                new Vector2(0f, btnTop - btnStep * 4 - 58f), new Vector2(0f, btnTop - btnStep * 4),
                () => _howToModal.SetActive(true),
                GameUIKit.GetIcon("exp"));

            _btnSettings = GameUIKit.MakeStyledButton("BtnSettings", leftCard.transform,
                "CÀI ĐẶT ÂM THANH & ĐỒ HỌA", 16, UIButtonStyle.Secondary,
                new Vector2(0.06f, 1f), new Vector2(0.94f, 1f),
                new Vector2(0f, btnTop - btnStep * 5 - 58f), new Vector2(0f, btnTop - btnStep * 5),
                () => _settingsModal.SetActive(true),
                GameUIKit.GetIcon("settings"));

            _btnQuit = GameUIKit.MakeStyledButton("BtnQuit", leftCard.transform,
                "THOÁT KHỎI HẦM NGỤC (QUIT GAME)", 16, UIButtonStyle.Danger,
                new Vector2(0.06f, 1f), new Vector2(0.94f, 1f),
                new Vector2(0f, btnTop - btnStep * 6 - 58f), new Vector2(0f, btnTop - btnStep * 6),
                QuitGame,
                GameUIKit.GetIcon("skull_boss"));

            GameUIKit.MakeText("FooterVer", leftCard.transform,
                "ANCIENT GOTHIC EDITION • 10 CLASSES • 30 BUILDS • LAN CO-OP",
                12, FontStyle.Italic, GameUIKit.ColMuted, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0f), new Vector2(0.95f, 0f), new Vector2(0f, 14f), new Vector2(0f, 38f));

            // Bottom-Right 3D Hero Stage Plaque
            var stageBanner = GameUIKit.MakeStyledPanel(
                "HeroStagePlaque", root, UIPanelStyle.Notification,
                new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-560f, 36f), new Vector2(-44f, 128f));

            GameUIKit.MakeIcon("StageCrest", stageBanner.transform, GameUIKit.GetIcon("shield"),
                new Vector2(0.03f, 0.16f), new Vector2(0.15f, 0.84f), Vector2.zero, Vector2.zero);

            _stageHeroTxt = GameUIKit.MakeText("StageHeroTxt", stageBanner.transform,
                $"<color=#F8DC85>{curCls.name.ToUpper()} — {curBuildName}</color>\n" +
                $"<size=13>{curCls.roleText}  •  Nội tại: {curCls.passive}</size>",
                15, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.MiddleLeft,
                new Vector2(0.17f, 0.10f), new Vector2(0.97f, 0.90f), Vector2.zero, Vector2.zero);

            BuildHowToModal(root);
            BuildSettingsModal(root);
            GameUIKit.OptimizeCanvasHierarchy(canvas);
            _isPrebakedInScene = true;
            _runtimeInitialized = Application.isPlaying;
        }

        private void Build3DMenuStage()
        {
            AbyssalPostProcessing.EnsureGlobalVolumeAndCameraSetup(transform);
            var atmo = BiomeAtmosphereController.EnsureOnScene(transform);
            atmo.ApplyBiome(DungeonBiome.AncientDungeon, instant: true);

            if (Camera.main != null)
            {
                Camera.main.transform.position = new Vector3(-0.9f, 1.85f, -5.2f);
                Camera.main.transform.rotation = Quaternion.Euler(6f, 12f, 0f);
            }

            _stageRoot = new GameObject("Menu3DStage").transform;
            _stageRoot.SetParent(transform, false);

            // Place South wall at z = -9.5f (well behind Camera at z = -5.2f) so no wall or colonnade arch ever blocks the view
            ModularDungeonArchitect.BuildModularRoomShell(
                _stageRoot, new Vector3(1.5f, 0f, 0.5f), width: 18f, length: 20f, wallHeight: 5.5f,
                biome: DungeonBiome.AncientDungeon, openSouthDoor: false, openNorthDoor: false);

            Material stoneMat = PbrMaterialLibrary.GothicStone();

            ObjMeshLoader.SpawnPiece("GuardianStatue_Gothic", _stageRoot, new Vector3(4.2f, 0f, 3.8f), Quaternion.Euler(0f, -155f, 0f), Vector3.one * 1.15f, stoneMat, addBoxCollider: false);
            ObjMeshLoader.SpawnPiece("RunicPillarAltar", _stageRoot, new Vector3(-1.4f, 0f, 3.4f), Quaternion.Euler(0f, 25f, 0f), Vector3.one * 0.9f, stoneMat, addBoxCollider: false);

            // Warm flickering torchlight & antique gold rim light on the 3D Hero
            var keyGo = new GameObject("HeroTorchKeyLight");
            keyGo.transform.SetParent(_stageRoot, false);
            keyGo.transform.position = new Vector3(0.4f, 2.7f, -2.5f);
            var keyLight = keyGo.AddComponent<Light>();
            keyLight.type = LightType.Point;
            keyLight.color = new Color(1.0f, 0.84f, 0.58f);
            keyLight.range = 12f;
            keyLight.intensity = 2.6f;

            var rimGo = new GameObject("HeroBronzeRimLight");
            rimGo.transform.SetParent(_stageRoot, false);
            rimGo.transform.position = new Vector3(2.6f, 2.8f, 1.2f);
            var rimLight = rimGo.AddComponent<Light>();
            rimLight.type = LightType.Point;
            rimLight.color = new Color(0.95f, 0.68f, 0.30f);
            rimLight.range = 10f;
            rimLight.intensity = 1.8f;

            ClassDef cls = GameDB.GetClass(GameSession.I.Profile.selectedClass);
            _bakedClassId = GameSession.I.Profile.selectedClass;
            _bakedBuildId = GameSession.I.Profile.selectedBuild;
            _heroPreview = CharacterVisualFactory.BuildHeroVisual(cls, _stageRoot, out _heroAnim, _bakedBuildId);
            _heroPreview.transform.position = new Vector3(1.35f, 0f, -0.6f);
            _heroPreview.transform.rotation = Quaternion.Euler(0f, -162f, 0f);
        }

        private void Update()
        {
            if (_heroPreview != null)
                _heroPreview.transform.Rotate(0f, Mathf.Sin(Time.time * 0.6f) * 6f * Time.deltaTime, 0f);
        }

        private void BuildHowToModal(Transform root)
        {
            var img = GameUIKit.MakeStyledPanel("HowToModal", root, UIPanelStyle.Dialog,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520f, -340f), new Vector2(520f, 340f));
            _howToModal = img.gameObject;

            GameUIKit.MakeIcon("HowToIc", img.transform, GameUIKit.GetIcon("exp"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -62f), new Vector2(68f, -22f));

            GameUIKit.MakeText("Title", img.transform, "CỔ THƯ HƯỚNG DẪN CHIẾN ĐẤU & CƠ CHẾ HẦM NGỤC", 24, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(78f, -66f), new Vector2(-24f, -16f));

            GameUIKit.MakeGothicDivider("HowToDiv", img.transform,
                new Vector2(0.04f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, -76f), new Vector2(0f, -58f));

            string body =
                "• <color=#F8DC85>Di chuyển Nhân vật:</color> W, A, S, D  |  <color=#F8DC85>Xoay Camera:</color> Di chuột (hoặc giữ Chuột Phải)\n" +
                "• <color=#F8DC85>Đánh thường (Combo 3 nhịp):</color> Chuột Trái (LMB) hoặc phím J\n" +
                "• <color=#F8DC85>4 Kỹ năng Chủ động & Tuyệt kỹ:</color> Q, W, E và R (hoặc phím 1, 2, 3, 4)\n" +
                "• <color=#F8DC85>Né đòn Bất tử ngắn (Dodge Roll):</color> Phím Space (tốn 25 Thể lực)\n" +
                "• <color=#F8DC85>Dùng Bình Máu Cổ Ngữ:</color> Phím H  |  <color=#F8DC85>Tương tác Rương / Cổng / Giải đố:</color> Phím F\n" +
                "• <color=#F8DC85>Mở Hồ sơ Chỉ số, Cây Cổ Ngữ & Túi đồ:</color> Phím I hoặc Tab\n" +
                "• <color=#F8DC85>Bản đồ Chiến thuật & Nhật ký Chiến đấu:</color> Phím M (Bản đồ) / Phím L (Combat Log)\n" +
                "• <color=#F8DC85>Tổ đội Co-op 3–4 Người:</color> Kết nối LAN trực tiếp hoặc sát cánh cùng AI Hunter thông minh.\n" +
                "• <color=#F8DC85>Phím tắt Kiểm thử Nhanh trong Dungeon:</color> F5 (+1 Cấp), F6 (Rớt Đồ), F7 (Tới Phòng Boss), F8 (Dọn Phòng).";

            GameUIKit.MakeText("Body", img.transform, body, 16, FontStyle.Normal, GameUIKit.ColParchment, TextAnchor.UpperLeft,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(36f, 92f), new Vector2(-36f, -86f));

            _btnCloseHowTo = GameUIKit.MakeStyledButton("Close", img.transform, "ĐÃ LĨNH HỘI (ĐÓNG)", 17, UIButtonStyle.Primary,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-150f, 22f), new Vector2(150f, 74f),
                () => _howToModal.SetActive(false), GameUIKit.GetIcon("check_seal"));

            _howToModal.SetActive(false);
        }

        private void BuildSettingsModal(Transform root)
        {
            _settingsModal = GameUIKit.BuildSettingsModal(root, includeResetSave: true);
        }
    }
}

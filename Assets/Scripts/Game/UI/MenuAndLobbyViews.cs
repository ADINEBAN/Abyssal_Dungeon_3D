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
        private GameObject _howToModal;
        private GameObject _settingsModal;
        private Transform _stageRoot;
        private GameObject _heroPreview;
        private Animator _heroAnim;

        public void Build()
        {
            GameAudio.I.PlayMusic(GameMusic.MainMenu);
            Build3DMenuStage();

            var canvas = GameUIKit.CreateCanvas("MainMenuCanvas");
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
                GameUIKit.MakeIcon("HeroPortrait", charPlaque.transform, curIcon,
                    new Vector2(0.03f, 0.12f), new Vector2(0.21f, 0.88f), Vector2.zero, Vector2.zero);
            }

            GameUIKit.MakeText("HeroInfoTitle", charPlaque.transform,
                $"THỢ SĂN HIỆN TẠI: <color=#F8DC85>{p.heroName.ToUpper()}</color> (Lv.{p.level})",
                15, FontStyle.Bold, GameUIKit.ColGold, TextAnchor.UpperLeft,
                new Vector2(0.24f, 0.52f), new Vector2(0.97f, 0.90f), Vector2.zero, Vector2.zero);

            GameUIKit.MakeText("HeroInfoSub", charPlaque.transform,
                $"Hệ phái: <color=#EDE0BA>{curCls.name} — {curBuildName}</color>\n" +
                $"Kho ngân: <color=#F8DC85>{p.gold} Gold</color>  •  Tầng sâu nhất: <color=#F8DC85>Tầng {p.highestFloor}</color>",
                13, FontStyle.Normal, GameUIKit.ColMuted, TextAnchor.LowerLeft,
                new Vector2(0.24f, 0.10f), new Vector2(0.97f, 0.54f), Vector2.zero, Vector2.zero);

            // 7 Antique Metal Command Buttons
            float btnTop = -230f;
            float btnStep = 72f;

            GameUIKit.MakeStyledButton("BtnPlay", leftCard.transform,
                "KHỞI HÀNH — SẢNH TỔ ĐỘI (LOBBY)", 18, UIButtonStyle.Primary,
                new Vector2(0.06f, 1f), new Vector2(0.94f, 1f),
                new Vector2(0f, btnTop - 58f), new Vector2(0f, btnTop),
                () => GameSession.GoToScene("Lobby"),
                GameUIKit.GetIcon("crossed_swords"));

            GameUIKit.MakeStyledButton("BtnClasses", leftCard.transform,
                "ĐỀN CHỌN LỚP NHÂN VẬT (10 CLASSES)", 16, UIButtonStyle.Secondary,
                new Vector2(0.06f, 1f), new Vector2(0.94f, 1f),
                new Vector2(0f, btnTop - btnStep - 58f), new Vector2(0f, btnTop - btnStep),
                () =>
                {
                    GameSession.I.ReturnSceneAfterClassSelect = "MainMenu";
                    GameSession.GoToScene("ClassSelection");
                },
                GameUIKit.GetIcon("crown"));

            GameUIKit.MakeStyledButton("BtnTown", leftCard.transform,
                "THỊ TRẤN AETHER HAVEN (RÈN & NÂNG CẤP)", 16, UIButtonStyle.Secondary,
                new Vector2(0.06f, 1f), new Vector2(0.94f, 1f),
                new Vector2(0f, btnTop - btnStep * 2 - 58f), new Vector2(0f, btnTop - btnStep * 2),
                () => GameSession.GoToScene("Town"),
                GameUIKit.GetIcon("anvil"));

            GameUIKit.MakeStyledButton("BtnShowcase", leftCard.transform,
                "PHÒNG TRƯNG BÀY DI VẬT 3D (SHOWCASE)", 16, UIButtonStyle.Secondary,
                new Vector2(0.06f, 1f), new Vector2(0.94f, 1f),
                new Vector2(0f, btnTop - btnStep * 3 - 58f), new Vector2(0f, btnTop - btnStep * 3),
                () => GameSession.GoToScene("VisualShowcase"),
                GameUIKit.GetIcon("portal"));

            GameUIKit.MakeStyledButton("BtnHowTo", leftCard.transform,
                "CỔ THƯ HƯỚNG DẪN & ĐIỀU KHIỂN", 16, UIButtonStyle.Secondary,
                new Vector2(0.06f, 1f), new Vector2(0.94f, 1f),
                new Vector2(0f, btnTop - btnStep * 4 - 58f), new Vector2(0f, btnTop - btnStep * 4),
                () => _howToModal.SetActive(true),
                GameUIKit.GetIcon("exp"));

            GameUIKit.MakeStyledButton("BtnSettings", leftCard.transform,
                "CÀI ĐẶT ÂM THANH & ĐỒ HỌA", 16, UIButtonStyle.Secondary,
                new Vector2(0.06f, 1f), new Vector2(0.94f, 1f),
                new Vector2(0f, btnTop - btnStep * 5 - 58f), new Vector2(0f, btnTop - btnStep * 5),
                () => _settingsModal.SetActive(true),
                GameUIKit.GetIcon("settings"));

            GameUIKit.MakeStyledButton("BtnQuit", leftCard.transform,
                "THOÁT KHỎI HẦM NGỤC (QUIT GAME)", 16, UIButtonStyle.Danger,
                new Vector2(0.06f, 1f), new Vector2(0.94f, 1f),
                new Vector2(0f, btnTop - btnStep * 6 - 58f), new Vector2(0f, btnTop - btnStep * 6),
                () =>
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                },
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

            GameUIKit.MakeText("StageHeroTxt", stageBanner.transform,
                $"<color=#F8DC85>{curCls.name.ToUpper()} — {curBuildName}</color>\n" +
                $"<size=13>{curCls.roleText}  •  Nội tại: {curCls.passive}</size>",
                15, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.MiddleLeft,
                new Vector2(0.17f, 0.10f), new Vector2(0.97f, 0.90f), Vector2.zero, Vector2.zero);

            BuildHowToModal(root);
            BuildSettingsModal(root);
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
            _heroPreview = CharacterVisualFactory.BuildHeroVisual(cls, _stageRoot, out _heroAnim, GameSession.I.Profile.selectedBuild);
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

            GameUIKit.MakeStyledButton("Close", img.transform, "ĐÃ LĨNH HỘI (ĐÓNG)", 17, UIButtonStyle.Primary,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-150f, 22f), new Vector2(150f, 74f),
                () => _howToModal.SetActive(false), GameUIKit.GetIcon("check_seal"));

            _howToModal.SetActive(false);
        }

        private void BuildSettingsModal(Transform root)
        {
            var img = GameUIKit.MakeStyledPanel("SettingsModal", root, UIPanelStyle.Dialog,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420f, -260f), new Vector2(420f, 260f));
            _settingsModal = img.gameObject;

            GameUIKit.MakeIcon("SetIc", img.transform, GameUIKit.GetIcon("settings"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -62f), new Vector2(68f, -22f));

            GameUIKit.MakeText("Title", img.transform, "CÀI ĐẶT ÂM THANH & HIỆU NĂNG", 24, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(78f, -66f), new Vector2(-24f, -16f));

            GameUIKit.MakeGothicDivider("SetDiv", img.transform,
                new Vector2(0.05f, 1f), new Vector2(0.95f, 1f), new Vector2(0f, -76f), new Vector2(0f, -58f));

            var p = GameSession.I.Profile;
            Text volLbl = GameUIKit.MakeText("VolLbl", img.transform, $"Âm lượng tổng: {Mathf.RoundToInt(p.masterVol * 100)}%", 18, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -135f), new Vector2(-24f, -92f));

            GameUIKit.MakeStyledButton("VolDown", img.transform, "- 10% Âm Lượng", 16, UIButtonStyle.Secondary,
                new Vector2(0.10f, 1f), new Vector2(0.47f, 1f), new Vector2(0f, -196f), new Vector2(0f, -144f), () =>
                {
                    p.masterVol = Mathf.Clamp01(p.masterVol - 0.1f);
                    GameAudio.I.ApplyVolumes();
                    GameSession.I.SaveProfile();
                    volLbl.text = $"Âm lượng tổng: {Mathf.RoundToInt(p.masterVol * 100)}%";
                });

            GameUIKit.MakeStyledButton("VolUp", img.transform, "+ 10% Âm Lượng", 16, UIButtonStyle.Primary,
                new Vector2(0.53f, 1f), new Vector2(0.90f, 1f), new Vector2(0f, -196f), new Vector2(0f, -144f), () =>
                {
                    p.masterVol = Mathf.Clamp01(p.masterVol + 0.1f);
                    GameAudio.I.ApplyVolumes();
                    GameSession.I.SaveProfile();
                    volLbl.text = $"Âm lượng tổng: {Mathf.RoundToInt(p.masterVol * 100)}%";
                });

            GameUIKit.MakeStyledButton("ResetSave", img.transform, "KHỞI TẠO LẠI HỒ SƠ (LV.1)", 15, UIButtonStyle.Danger,
                new Vector2(0.14f, 1f), new Vector2(0.86f, 1f), new Vector2(0f, -268f), new Vector2(0f, -216f), () =>
                {
                    PlayerPrefs.DeleteKey("AbyssalDungeon3D_Profile_v2");
                    Destroy(GameSession.I.gameObject);
                    GameSession.GoToScene("MainMenu");
                });

            GameUIKit.MakeStyledButton("Close", img.transform, "LƯU & ĐÓNG", 17, UIButtonStyle.Primary,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-130f, 22f), new Vector2(130f, 72f),
                () => _settingsModal.SetActive(false), GameUIKit.GetIcon("check_seal"));

            _settingsModal.SetActive(false);
        }
    }

    /// <summary>
    /// Section 4.2 Multiplayer Lobby & Party Management Scene UI (PHASE 4):
    /// - 3–4 3D Heroes standing on carved Gothic stone & antique bronze pedestals
    /// - Left Ornate Lobby Command Panel (Create Room, Party Size, LAN Host/Join, 4 Tactical Presets, Floor Difficulty, Start Dungeon)
    /// - 4 Ornate Gothic Party Member Cards with framed Class Portrait, Level/Role badge, Class arrows, and Ready Seal
    /// </summary>
    public class LobbySceneView : MonoBehaviour
    {
        private Text _roomHeaderTxt;
        private Text _netStatusTxt;
        private Text _floorTxt;
        private InputField _ipInput;
        private readonly GameObject[] _slotCards = new GameObject[4];
        private readonly Image[] _slotPortraits = new Image[4];
        private readonly Text[] _slotTitleTxts = new Text[4];
        private readonly Text[] _slotSubTxts = new Text[4];
        private readonly Button[] _readyBtns = new Button[4];
        private readonly Image[] _readyIcons = new Image[4];
        private Transform _stageRoot;
        private readonly GameObject[] _stageModels = new GameObject[4];

        public void Build()
        {
            GameAudio.I.PlayMusic(GameMusic.MainMenu);
            Build3DLobbyStage();

            var canvas = GameUIKit.CreateCanvas("LobbyCanvas");
            var root = canvas.transform;

            // Top Ornate Gothic Header Bar
            var top = GameUIKit.MakeStyledPanel("TopBar", root, UIPanelStyle.Ornate,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -84f), new Vector2(-24f, -12f));

            GameUIKit.MakeIcon("TopCrest", top.transform, GameUIKit.GetIcon("crown"),
                new Vector2(0f, 0.16f), new Vector2(0f, 0.84f), new Vector2(18f, 0f), new Vector2(64f, 0f));

            _roomHeaderTxt = GameUIKit.MakeText("RoomTitle", top.transform, "", 23, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0.54f, 1f), new Vector2(74f, 0f), Vector2.zero);

            _netStatusTxt = GameUIKit.MakeText("NetStatus", top.transform, "", 14, FontStyle.Bold, GameUIKit.ColMuted, TextAnchor.MiddleRight,
                new Vector2(0.50f, 0f), new Vector2(0.83f, 1f), Vector2.zero, new Vector2(-14f, 0f));

            GameUIKit.MakeStyledButton("BackMenu", top.transform, "← MAIN MENU", 15, UIButtonStyle.Secondary,
                new Vector2(0.84f, 0.16f), new Vector2(0.985f, 0.84f), Vector2.zero, Vector2.zero,
                () => GameSession.GoToScene("MainMenu"));

            // Left Party & Room Command Panel (UI_Panel_Lobby)
            var left = GameUIKit.MakeStyledPanel("LeftControls", root, UIPanelStyle.Lobby,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(24f, 24f), new Vector2(456f, -96f));

            GameUIKit.MakeText("Sec1", left.transform, "QUẢN LÝ PHÒNG & KẾT NỐI CO-OP", 16, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -44f), new Vector2(-20f, -12f));

            GameUIKit.MakeStyledButton("NewRoom", left.transform, "Tạo Phòng Mới", 14, UIButtonStyle.Primary,
                new Vector2(0.04f, 1f), new Vector2(0.49f, 1f), new Vector2(0f, -96f), new Vector2(0f, -50f), () =>
                {
                    GameSession.I.CreateDefaultRoom();
                    RefreshUI();
                });

            GameUIKit.MakeStyledButton("ToggleSize", left.transform, "Đổi Tổ Đội 3 / 4 Người", 14, UIButtonStyle.Secondary,
                new Vector2(0.51f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, -96f), new Vector2(0f, -50f), () =>
                {
                    int next = GameSession.I.ActivePartyCount() == 4 ? 3 : 4;
                    GameSession.I.SetPartySize(next);
                    RefreshUI();
                });

            GameUIKit.MakeStyledButton("HostLan", left.transform, "Mở Host LAN (:7777)", 14, UIButtonStyle.Secondary,
                new Vector2(0.04f, 1f), new Vector2(0.49f, 1f), new Vector2(0f, -150f), new Vector2(0f, -104f), () =>
                {
                    LanRoomBridge.I.StartHost(7777);
                    RefreshUI();
                });

            // IP Input Field in Forged Iron Frame
            var ipBg = GameUIKit.MakeStyledPanel("IpBox", left.transform, UIPanelStyle.Inventory,
                new Vector2(0.51f, 1f), new Vector2(0.76f, 1f), new Vector2(0f, -150f), new Vector2(0f, -104f));
            var ipTxt = GameUIKit.MakeText("Text", ipBg.transform, "127.0.0.1", 14, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(6f, 2f), new Vector2(-6f, -2f));
            _ipInput = ipBg.gameObject.AddComponent<InputField>();
            _ipInput.textComponent = ipTxt;
            _ipInput.text = "127.0.0.1";

            GameUIKit.MakeStyledButton("JoinLan", left.transform, "Join LAN", 13, UIButtonStyle.Primary,
                new Vector2(0.775f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, -150f), new Vector2(0f, -104f), () =>
                {
                    LanRoomBridge.I.JoinHost(_ipInput.text, 7777);
                    RefreshUI();
                });

            GameUIKit.MakeGothicDivider("Div1", left.transform,
                new Vector2(0.05f, 1f), new Vector2(0.95f, 1f), new Vector2(0f, -172f), new Vector2(0f, -156f));

            // Section 2: Tactical Party Presets
            GameUIKit.MakeText("Sec2", left.transform, "ĐỘI HÌNH CHIẾN THUẬT MẪU:", 15, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -204f), new Vector2(-20f, -174f));

            string[] presetNames =
            {
                "Tổ đội Cân bằng (Knight + Mage + Ranger + Cleric)",
                "Tổ đội Công Kích (Berserker + Assassin + Mage + Ranger)",
                "Tổ đội Thành Trì (Knight + Guardian + Druid + Cleric)",
                "Tổ đội Khống Chế (Guardian + Mage + Warlock + Ranger)"
            };
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                float topY = -210f - i * 48f;
                GameUIKit.MakeStyledButton("Preset" + i, left.transform, presetNames[i], 13, UIButtonStyle.Secondary,
                    new Vector2(0.04f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, topY - 42f), new Vector2(0f, topY), () =>
                    {
                        GameSession.I.ApplyPartyPreset(idx);
                        RefreshUI();
                    });
            }

            GameUIKit.MakeGothicDivider("Div2", left.transform,
                new Vector2(0.05f, 1f), new Vector2(0.95f, 1f), new Vector2(0f, -420f), new Vector2(0f, -404f));

            // Section 3: Dungeon Floor Difficulty Selector
            GameUIKit.MakeText("Sec3", left.transform, "ĐỘ KHÓ TẦNG HẦM NGỤC:", 15, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -452f), new Vector2(-20f, -422f));

            GameUIKit.MakeStyledButton("FloorMinus", left.transform, "-", 22, UIButtonStyle.Secondary,
                new Vector2(0.04f, 1f), new Vector2(0.20f, 1f), new Vector2(0f, -504f), new Vector2(0f, -456f), () =>
                {
                    GameSession.I.Profile.selectedFloor = Mathf.Max(1, GameSession.I.Profile.selectedFloor - 1);
                    RefreshUI();
                });

            var floorFrame = GameUIKit.MakeStyledPanel("FloorBadge", left.transform, UIPanelStyle.Ornate,
                new Vector2(0.22f, 1f), new Vector2(0.78f, 1f), new Vector2(0f, -504f), new Vector2(0f, -456f));
            _floorTxt = GameUIKit.MakeText("FloorLbl", floorFrame.transform, "", 16, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameUIKit.MakeStyledButton("FloorPlus", left.transform, "+", 22, UIButtonStyle.Secondary,
                new Vector2(0.80f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, -504f), new Vector2(0f, -456f), () =>
                {
                    int maxUnlock = Mathf.Max(3, GameSession.I.Profile.highestFloor + 1);
                    GameSession.I.Profile.selectedFloor = Mathf.Min(maxUnlock, GameSession.I.Profile.selectedFloor + 1);
                    RefreshUI();
                });

            // Bottom Action Buttons
            GameUIKit.MakeStyledButton("CustomizeClass", left.transform,
                "CHỌN CLASS & BUILD CHI TIẾT →", 15, UIButtonStyle.Tab,
                new Vector2(0.04f, 0f), new Vector2(0.96f, 0f), new Vector2(0f, 88f), new Vector2(0f, 140f), () =>
                {
                    GameSession.I.ReturnSceneAfterClassSelect = "Lobby";
                    GameSession.GoToScene("ClassSelection");
                },
                GameUIKit.GetIcon("crown"));

            GameUIKit.MakeStyledButton("StartDungeon", left.transform,
                "VÀO PHÓ BẢN (START DUNGEON)", 18, UIButtonStyle.Primary,
                new Vector2(0.04f, 0f), new Vector2(0.96f, 0f), new Vector2(0f, 18f), new Vector2(0f, 78f), () =>
                {
                    if (!GameSession.I.AllPartyReady())
                    {
                        foreach (var m in GameSession.I.Party) m.ready = true;
                    }
                    if (LanRoomBridge.I.IsHosting)
                        LanRoomBridge.I.BroadcastStartDungeon();
                    GameSession.GoToScene("Dungeon");
                },
                GameUIKit.GetIcon("crossed_swords"));

            // Bottom-Right: 4 Ornate Gothic Party Member Cards
            for (int i = 0; i < 4; i++)
            {
                int slot = i;
                float xMin = 0.25f + i * 0.184f;
                float xMax = xMin + 0.174f;

                var card = GameUIKit.MakeStyledPanel("SlotCard" + i, root, UIPanelStyle.Character,
                    new Vector2(xMin, 0f), new Vector2(xMax, 0f), new Vector2(0f, 24f), new Vector2(0f, 304f));
                _slotCards[i] = card.gameObject;

                // Portrait Frame
                var pFrame = GameUIKit.MakeStyledPanel("PortraitFrame", card.transform, UIPanelStyle.Ornate,
                    new Vector2(0.06f, 0.54f), new Vector2(0.42f, 0.93f), Vector2.zero, Vector2.zero);

                var pImg = GameUIKit.MakePanel("Ic", pFrame.transform,
                    new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero, Color.white);
                pImg.preserveAspect = true;
                _slotPortraits[i] = pImg;

                _slotTitleTxts[i] = GameUIKit.MakeText("SlotTitle", card.transform, "", 16, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                    new Vector2(0.45f, 0.56f), new Vector2(0.95f, 0.93f), Vector2.zero, Vector2.zero);

                _slotSubTxts[i] = GameUIKit.MakeText("SlotSub", card.transform, "", 13, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.UpperLeft,
                    new Vector2(0.06f, 0.36f), new Vector2(0.94f, 0.53f), Vector2.zero, Vector2.zero);

                // Cycle Class Buttons
                GameUIKit.MakeStyledButton("PrevCls", card.transform, "◄", 14, UIButtonStyle.Secondary,
                    new Vector2(0.06f, 0.20f), new Vector2(0.28f, 0.34f), Vector2.zero, Vector2.zero, () =>
                    {
                        var m = GameSession.I.Party[slot];
                        m.classId = (m.classId + 9) % 10;
                        m.buildId = 0;
                        if (slot == 0) GameSession.I.SelectLocalClass(m.classId, 0);
                        if (LanRoomBridge.I.IsHosting) LanRoomBridge.I.BroadcastFromHost();
                        RefreshUI();
                    });

                GameUIKit.MakeText("CycleHint", card.transform, "Đổi Class", 12, FontStyle.Bold, GameUIKit.ColMuted, TextAnchor.MiddleCenter,
                    new Vector2(0.28f, 0.20f), new Vector2(0.72f, 0.34f), Vector2.zero, Vector2.zero);

                GameUIKit.MakeStyledButton("NextCls", card.transform, "►", 14, UIButtonStyle.Secondary,
                    new Vector2(0.72f, 0.20f), new Vector2(0.94f, 0.34f), Vector2.zero, Vector2.zero, () =>
                    {
                        var m = GameSession.I.Party[slot];
                        m.classId = (m.classId + 1) % 10;
                        m.buildId = 0;
                        if (slot == 0) GameSession.I.SelectLocalClass(m.classId, 0);
                        if (LanRoomBridge.I.IsHosting) LanRoomBridge.I.BroadcastFromHost();
                        RefreshUI();
                    });

                _readyBtns[i] = GameUIKit.MakeStyledButton("ReadyBtn", card.transform, "SẴN SÀNG", 14, UIButtonStyle.Selected,
                    new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.175f), Vector2.zero, Vector2.zero, () =>
                    {
                        var m = GameSession.I.Party[slot];
                        m.ready = !m.ready;
                        if (LanRoomBridge.I.IsConnectedClient && slot == LanRoomBridge.I.LocalSlotIndex)
                            LanRoomBridge.I.NotifyClientSlotUpdate(m.ready);
                        else if (LanRoomBridge.I.IsHosting)
                            LanRoomBridge.I.BroadcastFromHost();
                        RefreshUI();
                    },
                    GameUIKit.GetIcon("check_seal"));

                var btnIc = _readyBtns[i].transform.Find("BtnIcon")?.GetComponent<Image>();
                _readyIcons[i] = btnIc;
            }

            GameSession.I.OnPartyChanged += RefreshUI;
            RefreshUI();
        }

        private void OnDestroy()
        {
            if (GameSession.I != null)
                GameSession.I.OnPartyChanged -= RefreshUI;
        }

        private void Build3DLobbyStage()
        {
            AbyssalPostProcessing.EnsureGlobalVolumeAndCameraSetup(transform);
            var atmo = BiomeAtmosphereController.EnsureOnScene(transform);
            atmo.ApplyBiome(DungeonBiome.ForgottenTemple, instant: true);

            if (Camera.main != null)
            {
                Camera.main.transform.position = new Vector3(-0.7f, 2.25f, -6.5f);
                Camera.main.transform.rotation = Quaternion.Euler(9f, 5f, 0f);
            }

            _stageRoot = new GameObject("Lobby3DStage").transform;
            _stageRoot.SetParent(transform, false);

            // Place South wall at z = -10.5f (4m behind Camera at z = -6.5f) so the South wall & colonnade never block the 4 Party Heroes
            ModularDungeonArchitect.BuildModularRoomShell(
                _stageRoot, new Vector3(0.8f, 0f, -0.5f), width: 20f, length: 20f, wallHeight: 5.5f,
                biome: DungeonBiome.ForgottenTemple, openSouthDoor: false, openNorthDoor: false);

            Material stoneMat = PbrMaterialLibrary.GothicStone();
            ObjMeshLoader.SpawnPiece("RunicPillarAltar", _stageRoot, new Vector3(0.8f, 0f, 3.8f), Quaternion.identity, Vector3.one * 1.1f, stoneMat, addBoxCollider: false);

            // Warm overhead sanctuary key light + fill light so all 4 3D Hero models & Gothic backdrop show rich PBR armor & faces
            var keyGo = new GameObject("LobbyStageKeyLight");
            keyGo.transform.SetParent(_stageRoot, false);
            keyGo.transform.position = new Vector3(0.8f, 3.4f, -3.2f);
            var kl = keyGo.AddComponent<Light>();
            kl.type = LightType.Point;
            kl.color = new Color(1.0f, 0.88f, 0.68f);
            kl.range = 18f;
            kl.intensity = 2.5f;

            var rimGo = new GameObject("LobbyStageRimLight");
            rimGo.transform.SetParent(_stageRoot, false);
            rimGo.transform.position = new Vector3(1.2f, 3.2f, 2.4f);
            var rl = rimGo.AddComponent<Light>();
            rl.type = LightType.Point;
            rl.color = new Color(0.96f, 0.72f, 0.36f);
            rl.range = 15f;
            rl.intensity = 1.65f;
        }

        private void RefreshUI()
        {
            var s = GameSession.I;
            s.EnsureDefaultParty();
            int activeCount = s.ActivePartyCount();
            _roomHeaderTxt.text = $"PHÒNG TỔ ĐỘI: {s.RoomCode}  •  Thành viên: {activeCount}/4 (Co-op 3–4 Người)";
            _netStatusTxt.text = $"Trạng thái mạng: {LanRoomBridge.I.StatusText}";
            int maxUnlock = Mathf.Max(3, s.Profile.highestFloor + 1);
            _floorTxt.text = $"Tầng {s.Profile.selectedFloor} (Mở khóa: {maxUnlock})";

            Material stoneMat = PbrMaterialLibrary.GothicStone();
            Material goldMat = PbrMaterialLibrary.RunicGold();

            for (int i = 0; i < 4; i++)
            {
                var m = s.Party[i];
                bool active = m != null && m.active;
                _slotCards[i].SetActive(active);
                if (_stageModels[i] != null) Destroy(_stageModels[i]);

                if (!active) continue;

                ClassDef cls = GameDB.GetClass(m.classId);
                string buildName = cls.builds[Mathf.Clamp(m.buildId, 0, cls.builds.Length - 1)];

                if (_slotPortraits[i] != null)
                    _slotPortraits[i].sprite = GameUIKit.GetClassIcon(m.classId);

                _slotTitleTxts[i].text = $"{m.playerName}\n<size=13><color=#EDE0BA>Cấp {m.level}</color></size>";
                _slotSubTxts[i].text = $"<color=#F8DC85>{cls.name} — {buildName}</color>\n<size=12><color=#BDA685>{cls.roleText}</color></size>";

                var rLbl = _readyBtns[i].transform.Find("Label")?.GetComponent<Text>();
                if (rLbl != null)
                    rLbl.text = m.ready ? "ĐÃ SẴN SÀNG (READY)" : "CHƯA SẴN SÀNG";

                GameUIKit.SetButtonStyle(_readyBtns[i], m.ready ? UIButtonStyle.Selected : UIButtonStyle.Secondary);
                if (_readyIcons[i] != null)
                    _readyIcons[i].sprite = GameUIKit.GetIcon(m.ready ? "check_seal" : "lock_iron");

                // Spawn 3D Hero directly on the Gothic Sanctuary Stone Floor so their real 3D human shadow casts cleanly across the floor
                float xPos = -1.2f + i * 1.45f;
                float zPos = (i % 2 == 0) ? -0.35f : 0.25f;
                Animator anim;
                var model = CharacterVisualFactory.BuildHeroVisual(cls, _stageRoot, out anim, m.buildId);
                model.transform.position = new Vector3(xPos, 0f, zPos);
                model.transform.rotation = Quaternion.Euler(0f, 180f + (i - 1.5f) * -8f, 0f);

                _stageModels[i] = model;
            }
        }
    }

    /// <summary>
    /// Section 4.3 & 3.10 Class Selection & Build Scene UI (PHASE 3 - Screen B):
    /// - Left 2x5 Grid of 10 Classes with Ornate Portrait Frames & Rune Selection
    /// - Center Interactive 3D Hero on Gothic Stone Pedestal with Animation Preview Controls
    /// - Right Ornate Reliquary Panel showing Attributes, Active/Ultimate Skills with Icons, and 3 Build Specializations
    /// </summary>
    public class ClassSelectSceneView : MonoBehaviour
    {
        private int _previewClass;
        private int _previewBuild;
        private Transform _previewStage;
        private GameObject _previewModel;
        private Animator _previewAnim;
        private Transform _detailContainer;
        private readonly Button[] _classButtons = new Button[10];

        public void Build()
        {
            _previewClass = GameSession.I.Profile.selectedClass;
            _previewBuild = GameSession.I.Profile.selectedBuild;

            GameAudio.I.PlayMusic(GameMusic.MainMenu);
            Build3DPreviewStage();

            var canvas = GameUIKit.CreateCanvas("ClassSelectCanvas");
            var root = canvas.transform;

            // Top Ornate Header Bar
            var topBar = GameUIKit.MakeStyledPanel("TopHeader", root, UIPanelStyle.Ornate,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -78f), new Vector2(-28f, -12f));

            GameUIKit.MakeIcon("HdrIcon", topBar.transform, GameUIKit.GetIcon("crown"),
                new Vector2(0f, 0.16f), new Vector2(0f, 0.84f), new Vector2(18f, 0f), new Vector2(62f, 0f));

            GameUIKit.MakeText("Header", topBar.transform,
                "ĐỀN THỜ CỔ NGỮ — CHỌN LỚP NHÂN VẬT (10 AETHER HUNTER CLASSES)",
                24, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0.76f, 1f), new Vector2(72f, 0f), Vector2.zero);

            GameUIKit.MakeStyledButton("BackLobby", topBar.transform, "← QUAY LẠI", 16, UIButtonStyle.Secondary,
                new Vector2(0.84f, 0.16f), new Vector2(0.985f, 0.84f), Vector2.zero, Vector2.zero,
                () => GameSession.GoToScene(GameSession.I.ReturnSceneAfterClassSelect));

            // Left: 2x5 Grid of 10 Classes inside UI_Panel_Character
            var leftGrid = GameUIKit.MakeStyledPanel("ClassGrid", root, UIPanelStyle.Character,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(28f, 28f), new Vector2(530f, -90f));

            GameUIKit.MakeText("GridTitle", leftGrid.transform, "DANH SÁCH 10 HỆ PHÁI THỢ SĂN:", 15, FontStyle.Bold, GameUIKit.ColGold, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.935f), new Vector2(0.96f, 0.985f), Vector2.zero, Vector2.zero);

            for (int i = 0; i < 10; i++)
            {
                int cid = i;
                ClassDef c = GameDB.Classes[i];
                int col = i % 2;
                int row = i / 2;
                float xMin = col * 0.48f + 0.03f;
                float xMax = xMin + 0.46f;
                float yMax = 0.92f - row * 0.18f;
                float yMin = yMax - 0.165f;

                var btn = GameUIKit.MakeStyledButton("Class_" + c.name, leftGrid.transform, "", 15, UIButtonStyle.Secondary,
                    new Vector2(xMin, yMin), new Vector2(xMax, yMax), Vector2.zero, Vector2.zero, () =>
                    {
                        _previewClass = cid;
                        _previewBuild = 0;
                        RefreshSelection();
                    });
                _classButtons[i] = btn;

                Sprite ic = GameUIKit.GetClassIcon(i);
                if (ic != null)
                {
                    GameUIKit.MakeIcon("Ic", btn.transform, ic,
                        new Vector2(0.05f, 0.12f), new Vector2(0.36f, 0.88f), Vector2.zero, Vector2.zero);
                }

                GameUIKit.MakeText("Name", btn.transform,
                    $"<color=#F8DC85>{c.name}</color>\n<size=11><color=#EDE0BA>{c.roleText}</color></size>",
                    15, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                    new Vector2(0.39f, 0.08f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);
            }

            // Center-Bottom Animation Preview Dock under the 3D Hero
            var animBar = GameUIKit.MakeStyledPanel("AnimButtons", root, UIPanelStyle.Standard,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-240f, 28f), new Vector2(150f, 92f));

            GameUIKit.MakeStyledButton("TryAtk", animBar.transform, "Đòn Đánh", 14, UIButtonStyle.Secondary,
                new Vector2(0.02f, 0.14f), new Vector2(0.33f, 0.86f), Vector2.zero, Vector2.zero,
                () => TriggerPreviewAnim("Attack"), GameUIKit.GetIcon("sword"));

            GameUIKit.MakeStyledButton("TryCast", animBar.transform, "Kỹ Năng", 14, UIButtonStyle.Secondary,
                new Vector2(0.345f, 0.14f), new Vector2(0.655f, 0.86f), Vector2.zero, Vector2.zero,
                () => TriggerPreviewAnim("Cast"), GameUIKit.GetIcon("exp"));

            GameUIKit.MakeStyledButton("TryDodge", animBar.transform, "Né Đòn", 14, UIButtonStyle.Secondary,
                new Vector2(0.67f, 0.14f), new Vector2(0.98f, 0.86f), Vector2.zero, Vector2.zero,
                () => TriggerPreviewAnim("Dodge"), GameUIKit.GetIcon("dodge"));

            // Right: Detailed Attributes, Active/Ultimate Skills Q/W/E/R, Build Specialization & Confirm
            var right = GameUIKit.MakeStyledPanel("RightDetails", root, UIPanelStyle.Ornate,
                new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-690f, 28f), new Vector2(-28f, -90f));
            _detailContainer = GameUIKit.MakeRect("DetailContent", right.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            RefreshSelection();
        }

        private void Build3DPreviewStage()
        {
            AbyssalPostProcessing.EnsureGlobalVolumeAndCameraSetup(transform);
            var atmo = BiomeAtmosphereController.EnsureOnScene(transform);
            atmo.ApplyBiome(DungeonBiome.ForgottenTemple, instant: true);

            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = new Color(0.06f, 0.042f, 0.03f);
                Camera.main.transform.position = new Vector3(0.35f, 1.55f, -4.1f);
                Camera.main.transform.rotation = Quaternion.Euler(7f, 0f, 0f);
            }

            _previewStage = new GameObject("ClassPreviewStage").transform;
            _previewStage.SetParent(transform, false);

            // Place South wall at z = -9.5f (well behind Camera at z = -4.1f) so no wall or colonnade arch intersects the camera
            ModularDungeonArchitect.BuildModularRoomShell(
                _previewStage, new Vector3(0f, 0f, 0.5f), width: 16f, length: 20f, wallHeight: 5.5f,
                biome: DungeonBiome.ForgottenTemple, openSouthDoor: false, openNorthDoor: false);

            var l = new GameObject("KeyLight");
            l.transform.SetParent(_previewStage, false);
            l.transform.position = new Vector3(0.7f, 3.0f, -2.4f);
            var pl = l.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = new Color(1.0f, 0.90f, 0.72f);
            pl.range = 16f;
            pl.intensity = 2.6f;

            var rim = new GameObject("RimLight");
            rim.transform.SetParent(_previewStage, false);
            rim.transform.position = new Vector3(-1.4f, 2.6f, 1.6f);
            var rpl = rim.AddComponent<Light>();
            rpl.type = LightType.Point;
            rpl.color = new Color(0.95f, 0.72f, 0.35f);
            rpl.range = 12f;
            rpl.intensity = 1.6f;
        }

        private void TriggerPreviewAnim(string trigger)
        {
            if (_previewAnim != null) _previewAnim.SetTrigger(trigger);
            GameAudio.I.PlaySfx(GameSfx.Attack, 0.6f);
        }

        private void Update()
        {
            if (_previewModel != null)
            {
                if (Input.GetMouseButton(0))
                    _previewModel.transform.Rotate(0f, -Input.GetAxis("Mouse X") * 6f, 0f);
                else
                    _previewModel.transform.Rotate(0f, 20f * Time.deltaTime, 0f);
            }
        }

        private void RefreshSelection()
        {
            ClassDef cls = GameDB.GetClass(_previewClass);

            for (int i = 0; i < 10; i++)
            {
                if (_classButtons[i] != null)
                    GameUIKit.SetButtonStyle(_classButtons[i], (i == _previewClass) ? UIButtonStyle.Selected : UIButtonStyle.Secondary);
            }

            if (_previewModel != null) Destroy(_previewModel);
            _previewModel = CharacterVisualFactory.BuildHeroVisual(cls, _previewStage, out _previewAnim, _previewBuild);
            _previewModel.transform.position = Vector3.zero;
            _previewModel.transform.rotation = Quaternion.Euler(0f, 185f, 0f);

            Color buildCol = ClassSpecializationSystem.GetBuildAccentColor(_previewClass, _previewBuild, cls.color);
            string archLabel = ClassSpecializationSystem.GetArchetypeShortTag(ClassSpecializationSystem.GetClassArchetype(_previewClass));

            for (int i = _detailContainer.childCount - 1; i >= 0; i--)
                Destroy(_detailContainer.GetChild(i).gameObject);

            // Portrait + Class Title Header
            Sprite clsPortrait = GameUIKit.GetClassIcon(_previewClass);
            if (clsPortrait != null)
            {
                GameUIKit.MakeIcon("ClsPortrait", _detailContainer, clsPortrait,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -104f), new Vector2(104f, -20f));
            }

            GameUIKit.MakeText("Title", _detailContainer,
                $"{cls.name.ToUpper()} — {cls.roleText}",
                25, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(118f, -58f), new Vector2(-24f, -16f));

            GameUIKit.MakeText("Desc", _detailContainer,
                $"{cls.desc}\n<color=#D9B057>• Nội tại: {cls.passive}   |   Hệ Giáp: {archLabel} (+15% Chỉ Số)</color>",
                13, FontStyle.Normal, GameUIKit.ColParchment, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(118f, -112f), new Vector2(-24f, -56f));

            // Base Attributes Bar
            string stats = $"HP: <color=#F8DC85>{cls.hp:0}</color>   |   MP: <color=#7EC8FF>{cls.mana:0}</color>   |   ATK: <color=#F8DC85>{cls.atk:0}</color>   |   DEF: <color=#F8DC85>{cls.def:0}</color>   |   Crit: <color=#F8DC85>{cls.crit * 100:0}%</color>   |   Tốc chạy: <color=#F8DC85>{cls.moveSpeed:0.1f}</color>";
            var statBox = GameUIKit.MakeStyledPanel("StatBox", _detailContainer, UIPanelStyle.Standard,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -160f), new Vector2(-24f, -118f));
            GameUIKit.MakeText("StatTxt", statBox.transform, stats, 14, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // 4 Active & Ultimate Skills (Q, W, E, R) with Engraved Skill Medallions
            string[] keys = { "Q / 1", "W / 2", "E / 3", "R / 4 • ULTIMATE" };
            for (int s = 0; s < 4; s++)
            {
                SkillDef sk = cls.skills[s];
                float top = -170f - s * 76f;
                var skCard = GameUIKit.MakeStyledPanel("Sk" + s, _detailContainer,
                    s == 3 ? UIPanelStyle.Ornate : UIPanelStyle.SkillTree,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, top - 68f), new Vector2(-24f, top));

                GameUIKit.MakeIcon("SkIc", skCard.transform, GameUIKit.GetSkillIcon(sk),
                    new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(10f, 8f), new Vector2(62f, -8f));

                GameUIKit.MakeText("SkTitle", skCard.transform,
                    $"[{keys[s]}] {sk.name}   <size=12><color=#BDA685>(CD: {sk.cooldown:0.#}s | Mana: {sk.mana:0})</color></size>",
                    15, FontStyle.Bold, sk.color, TextAnchor.UpperLeft,
                    Vector2.zero, Vector2.one, new Vector2(72f, 6f), new Vector2(-14f, -8f));

                GameUIKit.MakeText("SkDesc", skCard.transform, sk.desc, 13, FontStyle.Normal, GameUIKit.ColParchment, TextAnchor.LowerLeft,
                    Vector2.zero, Vector2.one, new Vector2(72f, 8f), new Vector2(-14f, -32f));
            }

            // 3 Build Specializations
            GameUIKit.MakeText("BuildHeader", _detailContainer,
                "CHUYÊN MÔN HÓA HƯỚNG BUILD (3 HƯỚNG BUILD / CLASS):",
                15, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 162f), new Vector2(-24f, 190f));

            for (int b = 0; b < 3; b++)
            {
                int bIdx = b;
                bool active = (_previewBuild == b);
                GameUIKit.MakeStyledButton("BuildBtn" + b, _detailContainer, cls.builds[b], 14,
                    active ? UIButtonStyle.Selected : UIButtonStyle.Tab,
                    new Vector2(0.04f + b * 0.315f, 0f), new Vector2(0.335f + b * 0.315f, 0f),
                    new Vector2(0f, 114f), new Vector2(0f, 158f), () =>
                    {
                        _previewBuild = bIdx;
                        RefreshSelection();
                    });
            }

            GameUIKit.MakeText("BuildNote", _detailContainer,
                "• " + cls.buildDesc[_previewBuild],
                13, FontStyle.Italic, buildCol, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(26f, 76f), new Vector2(-26f, 112f));

            GameUIKit.MakeStyledButton("Confirm", _detailContainer,
                $"XÁC NHẬN CHỌN {cls.name.ToUpper()} ({cls.builds[_previewBuild]})", 18, UIButtonStyle.Primary,
                new Vector2(0.04f, 0f), new Vector2(0.96f, 0f), new Vector2(0f, 16f), new Vector2(0f, 72f), () =>
                {
                    GameSession.I.SelectLocalClass(_previewClass, _previewBuild);
                    if (LanRoomBridge.I.IsConnectedClient)
                        LanRoomBridge.I.NotifyClientSlotUpdate(true);
                    else
                        LanRoomBridge.I.BroadcastFromHost();
                    GameSession.GoToScene(GameSession.I.ReturnSceneAfterClassSelect);
                },
                GameUIKit.GetIcon("check_seal"));
        }
    }
}

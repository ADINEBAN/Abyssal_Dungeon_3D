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
            GameUIKit.MakeSanctuaryAtmosphericVignette(root);

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
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = new Color(0.024f, 0.025f, 0.042f);
                Camera.main.transform.position = new Vector3(-0.75f, 1.72f, -4.85f);
                Camera.main.transform.rotation = Quaternion.Euler(6f, 13f, 0f);
            }

            _stageRoot = new GameObject("Menu3DStage").transform;
            _stageRoot.SetParent(transform, false);

            ModularDungeonArchitect.BuildGothicSanctuaryStage(_stageRoot, new Vector3(1.35f, 0f, -0.45f), wideForParty: false);

            // Gloomy Gothic dual-tone lighting: warm flickering torch key + cool moonlight rim
            var keyGo = new GameObject("HeroTorchKeyLight");
            keyGo.transform.SetParent(_stageRoot, false);
            keyGo.transform.position = new Vector3(0.35f, 2.55f, -2.35f);
            var keyLight = keyGo.AddComponent<Light>();
            keyLight.type = LightType.Point;
            keyLight.color = new Color(1.0f, 0.78f, 0.48f);
            keyLight.range = 12f;
            keyLight.intensity = 2.35f;
            keyLight.shadows = LightShadows.None;

            var rimGo = new GameObject("HeroMoonRimLight");
            rimGo.transform.SetParent(_stageRoot, false);
            rimGo.transform.position = new Vector3(2.8f, 2.9f, 1.6f);
            var rimLight = rimGo.AddComponent<Light>();
            rimLight.type = LightType.Point;
            rimLight.color = new Color(0.46f, 0.58f, 0.92f);
            rimLight.range = 11f;
            rimLight.intensity = 1.65f;
            rimLight.shadows = LightShadows.None;

            ClassDef cls = GameDB.GetClass(GameSession.I.Profile.selectedClass);
            _heroPreview = CharacterVisualFactory.BuildHeroVisual(cls, _stageRoot, out _heroAnim, GameSession.I.Profile.selectedBuild);
            _heroPreview.transform.position = new Vector3(1.35f, 0.02f, -0.55f);
            _heroPreview.transform.rotation = Quaternion.Euler(0f, -162f, 0f);
            SetSafeLobbyBool(_heroAnim, true);
        }

        internal static void SetSafeLobbyBool(Animator anim, bool val)
        {
            if (anim == null || anim.runtimeAnimatorController == null) return;
            var pars = anim.parameters;
            for (int i = 0; i < pars.Length; i++)
            {
                if (pars[i].type == AnimatorControllerParameterType.Bool && pars[i].name == "InLobby")
                {
                    anim.SetBool("InLobby", val);
                    return;
                }
            }
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
            _settingsModal = GameUIKit.BuildSettingsModal(root, includeResetSave: true);
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
            GameUIKit.MakeSanctuaryAtmosphericVignette(root);

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
            atmo.ApplyBiome(DungeonBiome.AncientDungeon, instant: true);

            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = new Color(0.024f, 0.025f, 0.042f);
                Camera.main.transform.position = new Vector3(-0.65f, 2.15f, -6.2f);
                Camera.main.transform.rotation = Quaternion.Euler(8.5f, 5f, 0f);
            }

            _stageRoot = new GameObject("Lobby3DStage").transform;
            _stageRoot.SetParent(transform, false);

            ModularDungeonArchitect.BuildGothicSanctuaryStage(_stageRoot, new Vector3(0.95f, 0f, 0.0f), wideForParty: true);

            var keyGo = new GameObject("LobbyStageKeyLight");
            keyGo.transform.SetParent(_stageRoot, false);
            keyGo.transform.position = new Vector3(0.85f, 3.2f, -3.1f);
            var kl = keyGo.AddComponent<Light>();
            kl.type = LightType.Point;
            kl.color = new Color(1.0f, 0.82f, 0.56f);
            kl.range = 17f;
            kl.intensity = 2.35f;
            kl.shadows = LightShadows.None;

            var rimGo = new GameObject("LobbyStageMoonRimLight");
            rimGo.transform.SetParent(_stageRoot, false);
            rimGo.transform.position = new Vector3(1.2f, 3.3f, 2.8f);
            var rl = rimGo.AddComponent<Light>();
            rl.type = LightType.Point;
            rl.color = new Color(0.48f, 0.60f, 0.94f);
            rl.range = 15f;
            rl.intensity = 1.65f;
            rl.shadows = LightShadows.None;
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
                model.transform.position = new Vector3(xPos, 0.02f, zPos);
                model.transform.rotation = Quaternion.Euler(0f, 180f + (i - 1.5f) * -8f, 0f);
                MainMenuSceneView.SetSafeLobbyBool(anim, true);

                _stageModels[i] = model;
            }
        }
    }

    /// <summary>
    /// Section 4.3 & 3.10 Class Selection & Build Scene UI (PHASE 3 - Screen B):
    /// - Left 2x5 Grid of 10 Classes with Ornate Portrait Frames & Rune Selection
    /// - Center Interactive 3D Hero on Gothic Stone Pedestal with Animation Preview Controls
    /// <summary>
    /// Section 4.3 & Phase 6 Redesigned Class Selection Scene UI (1920x1080 Ancient Gothic Dark Fantasy):
    /// - Left (390px): Clean 2x5 Class Grid (Portrait + Name + 1-line role, bright antique gold on active, dark stone on inactive)
    /// - Center (Wide 54% unobstructed Sanctuary Stage): 3D Hero Preview facing 3/4 forward, smooth mouse drag-to-rotate,
    ///   focused Gothic spotlight + subtle Action Preview Dock (never blocks weapons!)
    /// - Right (446px): Concise Dark-Charcoal Panel with single antique-gold border, 1-sentence summary,
    ///   4 clean stat bars (HP / ATK / DEF / MOBILITY), 4 compact skill rows (Q/1, 2/Z, E/3, R/4) with interactive detail tooltip,
    ///   3 Build Specialization tabs, and Confirm button.
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
        private GameObject _skillTooltipBox;
        private Text _skillTooltipTitle;
        private Text _skillTooltipBody;
        private bool _draggingModel;

        private int _previewComboStep;

        private static readonly string[] ShortRoleTags =
        {
            "TANK · FIGHTER",
            "TANK · SUPPORT",
            "MELEE · BRUISER",
            "MELEE · BURST",
            "RANGED · MARKSMAN",
            "RANGED · AOE MAGE",
            "CURSE · SUMMONER",
            "MELEE · AGILE",
            "HYBRID · NATURE",
            "HEALER · SUPPORT"
        };

        private static readonly string[] ShortSummaries =
        {
            "Kiếm khiên cân bằng, tiên phong phá vỡ đội hình địch.",
            "Bảo vệ đồng đội bằng khiên nặng và phòng thủ vững chắc.",
            "Đại kiếm hai tay càn quét, càng ít máu sát thương càng mạnh.",
            "Song đoản kiếm tốc độ cao, ám sát mục tiêu trọng yếu.",
            "Cung dài cơ động, bắn tỉa chính xác từ tầm xa.",
            "Pháp trượng nguyên tố hủy diệt diện rộng và khống chế.",
            "Nguyền rủa linh hồn và triệu hồi bóng tối hút sinh lực.",
            "Quyền giáp liên hoàn tốc độ cao, áp sát linh hoạt.",
            "Phép thiên nhiên hồi phục và hóa gấu mở giao tranh.",
            "Thánh chùy và khiên ánh sáng bảo hộ, hồi sinh đồng đội."
        };

        public void Build()
        {
            _previewClass = GameSession.I.Profile.selectedClass;
            _previewBuild = GameSession.I.Profile.selectedBuild;

            GameAudio.I.PlayMusic(GameMusic.MainMenu);
            Build3DPreviewStage();

            var canvas = GameUIKit.CreateCanvas("ClassSelectCanvas");
            var root = canvas.transform;
            GameUIKit.MakeSanctuaryAtmosphericVignette(root);

            // Top Slim Charcoal-Bronze Header Bar (zero cluttered double borders)
            var topBar = CreateCharcoalGothicPanel("TopHeader", root,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -68f), new Vector2(-24f, -12f), 0.88f);

            GameUIKit.MakeIcon("HdrIcon", topBar.transform, GameUIKit.GetIcon("crown"),
                new Vector2(0f, 0.18f), new Vector2(0f, 0.82f), new Vector2(16f, 0f), new Vector2(52f, 0f));

            GameUIKit.MakeText("Header", topBar.transform,
                "CHỌN LỚP NHÂN VẬT",
                22, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0.36f, 1f), new Vector2(62f, 0f), Vector2.zero);

            GameUIKit.MakeText("HeaderSub", topBar.transform,
                "10 AETHER HUNTER CLASSES  •  GIỮ CHUỘT TRÁI GIỮA MÀN HÌNH ĐỂ XOAY NHÂN VẬT",
                12, FontStyle.Normal, new Color(0.76f, 0.70f, 0.56f, 0.92f), TextAnchor.MiddleCenter,
                new Vector2(0.30f, 0f), new Vector2(0.80f, 1f), Vector2.zero, Vector2.zero);

            GameUIKit.MakeStyledButton("BackLobby", topBar.transform, "← QUAY LẠI", 14, UIButtonStyle.Secondary,
                new Vector2(0.87f, 0.14f), new Vector2(0.988f, 0.86f), Vector2.zero, Vector2.zero,
                () => GameSession.GoToScene(GameSession.I.ReturnSceneAfterClassSelect));

            // LEFT (396px): Clean 2x5 Grid of 10 Classes
            var leftGrid = CreateCharcoalGothicPanel("ClassGrid", root,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(24f, 24f), new Vector2(420f, -80f), 0.86f);

            GameUIKit.MakeText("GridTitle", leftGrid.transform, "HỆ PHÁI THỢ SĂN", 14, FontStyle.Bold, GameUIKit.ColGold, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.942f), new Vector2(0.96f, 0.988f), Vector2.zero, Vector2.zero);

            for (int i = 0; i < 10; i++)
            {
                int cid = i;
                ClassDef c = GameDB.Classes[i];
                int col = i % 2;
                int row = i / 2;
                float xMin = 0.032f + col * 0.482f;
                float xMax = xMin + 0.454f;
                float yMax = 0.930f - row * 0.184f;
                float yMin = yMax - 0.168f;

                var btn = GameUIKit.MakeStyledButton("Class_" + c.name, leftGrid.transform, "", 14, UIButtonStyle.Secondary,
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
                        new Vector2(0.05f, 0.12f), new Vector2(0.38f, 0.88f), Vector2.zero, Vector2.zero);
                }

                string shortTag = i < ShortRoleTags.Length ? ShortRoleTags[i].Split('·')[0].Trim() : c.roleText;
                GameUIKit.MakeText("Name", btn.transform,
                    $"<color=#F8DC85>{c.name}</color>\n<size=11><color=#BFA984>{shortTag}</color></size>",
                    15, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                    new Vector2(0.41f, 0.08f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);
            }

            // CENTER-BOTTOM: Compact Action Preview Dock (unobtrusive, below hero's boots)
            var animBar = CreateCharcoalGothicPanel("AnimButtons", root,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-215f, 24f), new Vector2(185f, 78f), 0.84f);

            GameUIKit.MakeStyledButton("TryAtk", animBar.transform, "Đánh", 13, UIButtonStyle.Secondary,
                new Vector2(0.02f, 0.14f), new Vector2(0.26f, 0.86f), Vector2.zero, Vector2.zero,
                () => TriggerPreviewAnim("Attack"), GameUIKit.GetIcon("sword"));

            GameUIKit.MakeStyledButton("TryCast", animBar.transform, "Chiêu", 13, UIButtonStyle.Secondary,
                new Vector2(0.275f, 0.14f), new Vector2(0.515f, 0.86f), Vector2.zero, Vector2.zero,
                () => TriggerPreviewAnim("Cast"), GameUIKit.GetIcon("exp"));

            GameUIKit.MakeStyledButton("TryDodge", animBar.transform, "Né", 13, UIButtonStyle.Secondary,
                new Vector2(0.53f, 0.14f), new Vector2(0.75f, 0.86f), Vector2.zero, Vector2.zero,
                () => TriggerPreviewAnim("Dodge"), GameUIKit.GetIcon("dodge"));

            GameUIKit.MakeStyledButton("ResetRot", animBar.transform, "Xoay ↺", 12, UIButtonStyle.Secondary,
                new Vector2(0.765f, 0.14f), new Vector2(0.98f, 0.86f), Vector2.zero, Vector2.zero,
                () => { if (_previewModel != null) _previewModel.transform.rotation = Quaternion.Euler(0f, 165f, 0f); });

            // RIGHT (460px): Crisp Charcoal-Stone Class Details Panel with Breathing Room
            var right = CreateCharcoalGothicPanel("RightDetails", root,
                new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-484f, 24f), new Vector2(-24f, -80f), 0.88f);
            _detailContainer = GameUIKit.MakeRect("DetailContent", right.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Interactive Skill Tooltip Popup (anchored cleanly beside Right Details panel on hover/click)
            var tipPanel = CreateCharcoalGothicPanel("SkillTooltipPopup", root,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-810f, -68f), new Vector2(-496f, 68f), 0.95f);
            _skillTooltipBox = tipPanel.gameObject;
            _skillTooltipTitle = GameUIKit.MakeText("TipTitle", tipPanel.transform, "", 15, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(14f, 8f), new Vector2(-14f, -12f));
            _skillTooltipBody = GameUIKit.MakeText("TipBody", tipPanel.transform, "", 13, FontStyle.Normal, GameUIKit.ColParchment, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(14f, 10f), new Vector2(-14f, -38f));
            _skillTooltipBox.SetActive(false);

            RefreshSelection();
        }

        private static Image CreateCharcoalGothicPanel(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, float alpha)
        {
            var img = GameUIKit.MakePanel(name, parent, aMin, aMax, oMin, oMax, new Color(0.075f, 0.068f, 0.078f, alpha));
            var outl = img.gameObject.AddComponent<Outline>();
            outl.effectColor = new Color(0.76f, 0.58f, 0.25f, 0.90f);
            outl.effectDistance = new Vector2(1.5f, -1.5f);
            return img;
        }

        private void Build3DPreviewStage()
        {
            AbyssalPostProcessing.EnsureGlobalVolumeAndCameraSetup(transform);
            var atmo = BiomeAtmosphereController.EnsureOnScene(transform);
            atmo.ApplyBiome(DungeonBiome.AncientDungeon, instant: true);

            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = new Color(0.022f, 0.024f, 0.036f);
                // Frame Hero centered cleanly between Left (420px) and Right (460px) panels with weapons fully visible!
                Camera.main.transform.position = new Vector3(0.08f, 1.38f, -4.15f);
                Camera.main.transform.rotation = Quaternion.Euler(4.5f, 0f, 0f);
                Camera.main.fieldOfView = 42f;
            }

            _previewStage = new GameObject("ClassPreviewStage").transform;
            _previewStage.SetParent(transform, false);

            ModularDungeonArchitect.BuildGothicSanctuaryStage(_previewStage, Vector3.zero, wideForParty: false);

            // Focused Gothic 3-point Hero Showcase Lighting (warm front-3/4 key + cool moonlight rim + subtle weapon fill)
            var keyGo = new GameObject("HeroShowcaseKeyLight");
            keyGo.transform.SetParent(_previewStage, false);
            keyGo.transform.position = new Vector3(1.15f, 2.75f, -2.45f);
            var pl = keyGo.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = new Color(1.0f, 0.86f, 0.62f);
            pl.range = 14f;
            pl.intensity = 2.95f;
            pl.shadows = LightShadows.None;

            var rimGo = new GameObject("HeroShowcaseMoonRim");
            rimGo.transform.SetParent(_previewStage, false);
            rimGo.transform.position = new Vector3(-1.85f, 2.85f, 1.65f);
            var rpl = rimGo.AddComponent<Light>();
            rpl.type = LightType.Point;
            rpl.color = new Color(0.48f, 0.66f, 1.0f);
            rpl.range = 13f;
            rpl.intensity = 2.10f;
            rpl.shadows = LightShadows.None;

            var fillGo = new GameObject("HeroShowcaseWeaponFill");
            fillGo.transform.SetParent(_previewStage, false);
            fillGo.transform.position = new Vector3(-1.35f, 1.55f, -2.10f);
            var fpl = fillGo.AddComponent<Light>();
            fpl.type = LightType.Point;
            fpl.color = new Color(0.88f, 0.78f, 0.64f);
            fpl.range = 9f;
            fpl.intensity = 1.25f;
            fpl.shadows = LightShadows.None;
        }

        private void TriggerPreviewAnim(string trigger)
        {
            ClassDef cls = GameDB.GetClass(_previewClass);
            Color accent = ClassSpecializationSystem.GetBuildAccentColor(_previewClass, _previewBuild, cls.color);

            if (_previewAnim != null)
            {
                if (trigger == "Attack")
                {
                    int step = _previewComboStep % 3;
                    _previewComboStep = (step + 1) % 3;
                    var pars = _previewAnim.parameters;
                    for (int p = 0; p < pars.Length; p++)
                    {
                        if (pars[p].name == "ComboStep" && pars[p].type == AnimatorControllerParameterType.Int)
                            _previewAnim.SetInteger("ComboStep", step);
                    }
                    _previewAnim.SetTrigger("Attack");
                }
                else
                {
                    _previewAnim.SetTrigger(trigger);
                }
            }

            if (_previewModel != null)
            {
                var wRig = _previewModel.GetComponent<WeaponRigController>();
                if (wRig != null && trigger == "Attack")
                    wRig.TriggerBowDraw(0.24f);

                var trails = _previewModel.GetComponentsInChildren<WeaponBladeTrail3D>(true);
                for (int i = 0; i < trails.Length; i++)
                    trails[i].Activate(0.36f, accent);

                Vector3 rootPos = _previewModel.transform.position;
                Vector3 origin = rootPos + Vector3.up * 1.12f;
                Quaternion facing = _previewModel.transform.rotation;
                Vector3 fwd = _previewModel.transform.forward;

                var gng = AbyssalDungeon.VFX.GnGSkillVfxDispatcher.EnsureInstance();

                if (trigger == "Attack")
                {
                    int playedStep = (_previewComboStep + 2) % 3;
                    GameAudio.I.PlayWeaponAttack(_previewClass, playedStep, isHeavy: playedStep == 2, isFinisher: playedStep == 2, 0.78f);
                    if (gng != null)
                        gng.PlayComboOrHeavy(_previewClass, 2, playedStep == 2, rootPos, facing);

                    if (_previewClass == 4)
                    {
                        Vector3 bowMuzzle = wRig != null ? wRig.GetProjectileSpawnWorldPos(origin, fwd) : (origin + fwd * 0.55f);
                        AbyssalDungeon.VFX.VFXManager.EnsureInstance()?.SpawnVFX(
                            AbyssalDungeon.VFX.VFXType.Wpn_MuzzleCastBurst, bowMuzzle, facing, 0.30f, accent, null, 0.85f);
                        CombatFeelManager.I.SpawnThrustPierce(bowMuzzle, facing, accent, 0.95f);
                    }
                    else if (_previewClass == 5 || _previewClass == 6)
                    {
                        Vector3 staffEmit = wRig != null ? wRig.GetProjectileSpawnWorldPos(origin, fwd) : (origin + fwd * 1.0f);
                        AbyssalDungeon.VFX.VFXManager.EnsureInstance()?.SpawnVFX(
                            AbyssalDungeon.VFX.VFXType.Wpn_SpellImpact, staffEmit, facing, 0.40f, accent, null, 0.95f);
                    }
                    else
                    {
                        bool heavy = (playedStep == 2 || _previewClass == 2 || _previewClass == 1);
                        CombatFeelManager.I.SpawnSlashArc(
                            origin + fwd * 0.22f, facing,
                            heavy ? 2.15f : 1.75f,
                            heavy ? 168f : 144f,
                            playedStep == 1 ? 20f : -16f,
                            Color.Lerp(accent, Color.white, 0.52f),
                            accent,
                            heavy ? 0.28f : 0.22f,
                            heavy ? 0.92f : 0.65f,
                            playedStep == 1);
                    }
                }
                else if (trigger == "Cast")
                {
                    GameAudio.I.PlaySfx(GameSfx.Magic, 0.72f);
                    SkillKind previewKind = cls.skills != null && cls.skills.Length > 3 ? cls.skills[3].kind : SkillKind.SelfAoE;
                    if (gng != null)
                        gng.PlayClassSkill(_previewClass, 3, previewKind, rootPos, rootPos + fwd * 1.6f, facing);

                    if (_previewClass == 1 || _previewClass == 9)
                        CombatFeelManager.I.SpawnShieldAegisBlock(origin + fwd * 0.55f, facing, accent, 1.05f);
                    else if (_previewClass == 3 || _previewClass == 7)
                        CombatFeelManager.I.SpawnThrustPierce(origin + fwd * 0.55f, facing, accent, 1.10f);
                    else
                        CombatFeelManager.I.SpawnWhirlwindSlash(rootPos + Vector3.up * 0.45f, facing, 2.0f, accent);
                }
                else
                {
                    GameAudio.I.PlaySfx(GameSfx.Dodge, 0.65f);
                    AbyssalDungeon.VFX.VFXManager.EnsureInstance()?.SpawnVFX(
                        AbyssalDungeon.VFX.VFXType.DodgeTrail, origin, facing, 0.35f, accent, null, 1.0f);
                }
                return;
            }

            GameAudio.I.PlaySfx(GameSfx.Attack, 0.6f);
        }

        private void Update()
        {
            if (_previewModel == null) return;

            bool overUi = UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            if (Input.GetMouseButtonDown(0) && !overUi)
                _draggingModel = true;
            if (Input.GetMouseButtonUp(0))
                _draggingModel = false;

            if (_draggingModel && Input.GetMouseButton(0))
            {
                _previewModel.transform.Rotate(0f, -Input.GetAxis("Mouse X") * 7.5f, 0f, Space.World);
            }
        }

        private void ShowSkillTooltip(string keyTag, SkillDef sk)
        {
            if (_skillTooltipBox == null || sk == null) return;
            _skillTooltipTitle.text = $"[{keyTag}] {sk.name}";
            _skillTooltipBody.text =
                $"<color=#F8DC85>Hồi chiêu: {sk.cooldown:0.#}s   •   Mana: {sk.mana:0}</color>\n\n{sk.desc}";
            _skillTooltipBox.SetActive(true);
        }

        private void HideSkillTooltip()
        {
            if (_skillTooltipBox != null) _skillTooltipBox.SetActive(false);
        }

        private static void BuildStatBarRow(Transform parent, string label, string valueText, float norm01, Color fillCol, float yTop)
        {
            GameUIKit.MakeText("Lbl_" + label, parent, label, 12, FontStyle.Bold, new Color(0.84f, 0.78f, 0.64f), TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, yTop - 22f), new Vector2(106f, yTop));

            var bg = GameUIKit.MakePanel("BarBg_" + label, parent,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(110f, yTop - 17f), new Vector2(-78f, yTop - 5f),
                new Color(0.04f, 0.038f, 0.045f, 0.95f));
            var fill = GameUIKit.MakePanel("Fill", bg.transform,
                Vector2.zero, new Vector2(Mathf.Clamp(norm01, 0.08f, 1f), 1f), new Vector2(1f, 1f), new Vector2(-1f, -1f), fillCol);

            GameUIKit.MakeText("Val_" + label, parent, valueText, 12, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-74f, yTop - 22f), new Vector2(-20f, yTop));
        }

        private void RefreshSelection()
        {
            HideSkillTooltip();
            ClassDef cls = GameDB.GetClass(_previewClass);

            for (int i = 0; i < 10; i++)
            {
                if (_classButtons[i] != null)
                    GameUIKit.SetButtonStyle(_classButtons[i], (i == _previewClass) ? UIButtonStyle.Selected : UIButtonStyle.Secondary);
            }

            if (_previewModel != null) Destroy(_previewModel);
            _previewModel = CharacterVisualFactory.BuildHeroVisual(cls, _previewStage, out _previewAnim, _previewBuild);
            _previewModel.transform.position = new Vector3(0f, 0.02f, 0f);
            // Heroic 3/4 front stance showing both right-hand weapon and left-hand shield/bow clearly!
            _previewModel.transform.rotation = Quaternion.Euler(0f, 165f, 0f);
            MainMenuSceneView.SetSafeLobbyBool(_previewAnim, false);

            Color buildCol = ClassSpecializationSystem.GetBuildAccentColor(_previewClass, _previewBuild, cls.color);

            for (int i = _detailContainer.childCount - 1; i >= 0; i--)
                Destroy(_detailContainer.GetChild(i).gameObject);

            // 1. Clean Header: Portrait + CLASS NAME + Role Tag + 1 Short Sentence
            Sprite clsPortrait = GameUIKit.GetClassIcon(_previewClass);
            if (clsPortrait != null)
            {
                var pBox = CreateCharcoalGothicPanel("PortraitFrame", _detailContainer,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -92f), new Vector2(92f, -20f), 0.95f);
                GameUIKit.MakeIcon("ClsPortrait", pBox.transform, clsPortrait,
                    new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero);
            }

            string roleTag = (_previewClass >= 0 && _previewClass < ShortRoleTags.Length) ? ShortRoleTags[_previewClass] : cls.roleText;
            string summary = (_previewClass >= 0 && _previewClass < ShortSummaries.Length) ? ShortSummaries[_previewClass] : cls.desc;

            GameUIKit.MakeText("Title", _detailContainer, cls.name.ToUpper(), 24, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(106f, -52f), new Vector2(-20f, -18f));

            GameUIKit.MakeText("RoleTag", _detailContainer, roleTag, 12, FontStyle.Bold, new Color(0.84f, 0.68f, 0.36f), TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(106f, -72f), new Vector2(-20f, -50f));

            GameUIKit.MakeText("Summary", _detailContainer, summary, 13, FontStyle.Normal, GameUIKit.ColParchment, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -138f), new Vector2(-20f, -98f));

            GameUIKit.MakePanel("Div1", _detailContainer,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -145f), new Vector2(-20f, -143f),
                new Color(0.68f, 0.52f, 0.24f, 0.48f));

            // 2. 4 Main Stat Bars (HP, ATK, DEF, MOBILITY — fixed float format!)
            BuildStatBarRow(_detailContainer, "HP",       $"{cls.hp:0}",          cls.hp / 1350f,        new Color(0.78f, 0.22f, 0.22f), -156f);
            BuildStatBarRow(_detailContainer, "ATK",      $"{cls.atk:0}",         cls.atk / 115f,        new Color(0.88f, 0.54f, 0.20f), -184f);
            BuildStatBarRow(_detailContainer, "DEF",      $"{cls.def:0}",         cls.def / 85f,         new Color(0.32f, 0.58f, 0.86f), -212f);
            BuildStatBarRow(_detailContainer, "MOBILITY", $"{cls.moveSpeed:0.0}", cls.moveSpeed / 7.2f,  new Color(0.28f, 0.78f, 0.44f), -240f);

            GameUIKit.MakePanel("Div2", _detailContainer,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -274f), new Vector2(-20f, -272f),
                new Color(0.68f, 0.52f, 0.24f, 0.48f));

            // 3. 4 Compact Skills (Q/1, 2/Z, E/3, R/4 ★) with Hover/Click Detail Tooltip (Zero border overlap!)
            GameUIKit.MakeText("SkHdr", _detailContainer, "KỸ NĂNG CHÍNH  <size=11><color=#9E9074>(Di chuột / bấm để xem chi tiết)</color></size>",
                13, FontStyle.Bold, GameUIKit.ColGold, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -300f), new Vector2(-20f, -278f));

            string[] keys = { "Q / 1", "2 / Z", "E / 3", "R / 4 ★" };
            for (int s = 0; s < 4; s++)
            {
                SkillDef sk = cls.skills[s];
                string keyTag = keys[s];
                float top = -308f - s * 78f;

                var skCard = GameUIKit.MakePanel("Sk" + s, _detailContainer,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, top - 70f), new Vector2(-20f, top),
                    s == 3 ? new Color(0.14f, 0.11f, 0.08f, 0.94f) : new Color(0.10f, 0.09f, 0.105f, 0.90f));
                var skOut = skCard.gameObject.AddComponent<Outline>();
                skOut.effectColor = s == 3 ? new Color(0.95f, 0.76f, 0.30f, 0.92f) : new Color(0.56f, 0.44f, 0.22f, 0.65f);
                skOut.effectDistance = new Vector2(1f, -1f);

                // Hover & Click tooltip triggers
                var skBtn = skCard.gameObject.AddComponent<Button>();
                skBtn.transition = Selectable.Transition.ColorTint;
                skBtn.onClick.AddListener(() => ShowSkillTooltip(keyTag, sk));

                var trig = skCard.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                var enterE = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
                enterE.callback.AddListener(_ => ShowSkillTooltip(keyTag, sk));
                trig.triggers.Add(enterE);
                var exitE = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
                exitE.callback.AddListener(_ => HideSkillTooltip());
                trig.triggers.Add(exitE);

                GameUIKit.MakeIcon("SkIc", skCard.transform, GameUIKit.GetSkillIcon(sk),
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, -24f), new Vector2(58f, 24f));

                GameUIKit.MakeText("SkTitle", skCard.transform,
                    $"<color=#D9B057>[{keyTag}]</color> {sk.name}",
                    14, FontStyle.Bold, s == 3 ? GameUIKit.ColGoldBright : GameUIKit.ColParchment, TextAnchor.UpperLeft,
                    Vector2.zero, Vector2.one, new Vector2(68f, 34f), new Vector2(-12f, -8f));

                string shortDesc = sk.desc != null && sk.desc.Length > 52 ? (sk.desc.Substring(0, 49) + "...") : (sk.desc ?? "");
                GameUIKit.MakeText("SkDesc", skCard.transform, shortDesc, 12, FontStyle.Normal, new Color(0.78f, 0.73f, 0.62f), TextAnchor.LowerLeft,
                    Vector2.zero, Vector2.one, new Vector2(68f, 8f), new Vector2(-12f, -34f));
            }

            // 4. 3 Build Specializations + Confirm Button
            GameUIKit.MakeText("BuildHeader", _detailContainer, "HƯỚNG BUILD", 13, FontStyle.Bold, GameUIKit.ColGold, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 166f), new Vector2(-20f, 190f));

            for (int b = 0; b < 3; b++)
            {
                int bIdx = b;
                bool active = (_previewBuild == b);
                GameUIKit.MakeStyledButton("BuildBtn" + b, _detailContainer, cls.builds[b], 12,
                    active ? UIButtonStyle.Selected : UIButtonStyle.Tab,
                    new Vector2(0.044f + b * 0.310f, 0f), new Vector2(0.336f + b * 0.310f, 0f),
                    new Vector2(0f, 120f), new Vector2(0f, 162f), () =>
                    {
                        _previewBuild = bIdx;
                        RefreshSelection();
                    });
            }

            GameUIKit.MakeText("BuildNote", _detailContainer,
                cls.buildDesc[_previewBuild],
                12, FontStyle.Italic, buildCol, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(22f, 80f), new Vector2(-22f, 118f));

            GameUIKit.MakeStyledButton("Confirm", _detailContainer,
                $"XÁC NHẬN • {cls.name.ToUpper()}", 16, UIButtonStyle.Primary,
                new Vector2(0.044f, 0f), new Vector2(0.956f, 0f), new Vector2(0f, 18f), new Vector2(0f, 70f), () =>
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

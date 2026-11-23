using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssGame
{
    /// <summary>
    /// Section 4.2 Multiplayer Lobby & Party Management Scene UI (PHASE 4):
    /// - 3–4 3D Heroes standing on carved Gothic stone & antique bronze pedestals
    /// - Left Ornate Lobby Command Panel (Create Room, Party Size, LAN Host/Join, 4 Tactical Presets, Floor Difficulty, Start Dungeon)
    /// - 4 Ornate Gothic Party Member Cards with framed Class Portrait, Level/Role badge, Class arrows, and Ready Seal
    /// </summary>
    public class LobbySceneView : MonoBehaviour
    {
        [SerializeField] private bool _isPrebakedInScene;
        [SerializeField] private Canvas _lobbyCanvas;
        [SerializeField] private Text _roomHeaderTxt;
        [SerializeField] private Text _netStatusTxt;
        [SerializeField] private Text _hostInfoBadgeTxt;
        [SerializeField] private Text _floorTxt;
        [SerializeField] private InputField _heroNameInput;
        [SerializeField] private InputField _ipInput;
        [SerializeField] private Button _btnBackMenu;
        [SerializeField] private Button _btnModeSingle;
        [SerializeField] private Button _btnModeHost;
        [SerializeField] private Button _btnJoinLan;
        [SerializeField] private Button _btnFloorMinus;
        [SerializeField] private Button _btnFloorPlus;
        [SerializeField] private Button _btnCustomizeClass;
        [SerializeField] private Button _btnStartDungeon;
        [SerializeField] private Button[] _quickClassBtns = new Button[4];
        [SerializeField] private Button[] _presetBtns = new Button[4];
        private static readonly int[] QuickClassIds = { 0, 5, 4, 9 }; // Knight, Mage, Ranger, Cleric
        [SerializeField] private GameObject[] _slotCards = new GameObject[4];
        [SerializeField] private Image[] _slotPortraits = new Image[4];
        [SerializeField] private Text[] _slotRoleBadges = new Text[4];
        [SerializeField] private Text[] _slotTitleTxts = new Text[4];
        [SerializeField] private Text[] _slotSubTxts = new Text[4];
        [SerializeField] private Button[] _slotModeBtns = new Button[4];
        [SerializeField] private Button[] _slotPrevClsBtns = new Button[4];
        [SerializeField] private Button[] _slotNextClsBtns = new Button[4];
        [SerializeField] private Button[] _readyBtns = new Button[4];
        [SerializeField] private Image[] _readyIcons = new Image[4];
        [SerializeField] private Transform _stageRoot;
        [SerializeField] private Transform[] _lobbySpawnPoints = new Transform[4];
        [SerializeField] private GameObject[] _stageModels = new GameObject[4];
        [SerializeField] private GameObject[] _stageRings = new GameObject[4];

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

            if (Application.isPlaying)
            {
                GameAudio.I.PlayMusic(GameMusic.MainMenu);
                var _ = GameModeManager.I;
            }

            BindButton(_btnBackMenu, () => GameSession.GoToScene("MainMenu"));
            if (_heroNameInput != null)
            {
                _heroNameInput.text = GameSession.I.Profile.heroName;
                _heroNameInput.onEndEdit.RemoveAllListeners();
                _heroNameInput.onEndEdit.AddListener(OnHeroNameEdited);
            }

            for (int c = 0; c < 4 && _quickClassBtns != null && c < _quickClassBtns.Length; c++)
            {
                int cid = QuickClassIds[c];
                BindButton(_quickClassBtns[c], () => OnQuickClassClicked(cid));
            }

            BindButton(_btnModeSingle, () =>
            {
                GameModeManager.I.SetSinglePlayerMode();
                RefreshUI();
            });

            BindButton(_btnModeHost, () =>
            {
                GameModeManager.I.StartCoOpHost(7777);
                RefreshUI();
            });

            BindButton(_btnJoinLan, () =>
            {
                GameModeManager.I.JoinCoOpRoom(_ipInput != null ? _ipInput.text : "127.0.0.1", 7777);
                RefreshUI();
            });

            for (int i = 0; i < 4 && _presetBtns != null && i < _presetBtns.Length; i++)
            {
                int idx = i;
                BindButton(_presetBtns[i], () =>
                {
                    GameSession.I.ApplyPartyPreset(idx);
                    if (_heroNameInput != null) _heroNameInput.text = GameSession.I.Profile.heroName;
                    if (LanRoomBridge.I.IsHosting) LanRoomBridge.I.BroadcastFromHost();
                    RefreshUI();
                });
            }

            BindButton(_btnFloorMinus, () =>
            {
                GameSession.I.Profile.selectedFloor = Mathf.Max(1, GameSession.I.Profile.selectedFloor - 1);
                if (LanRoomBridge.I.IsHosting) LanRoomBridge.I.BroadcastFromHost();
                RefreshUI();
            });

            BindButton(_btnFloorPlus, () =>
            {
                int maxUnlock = Mathf.Max(3, GameSession.I.Profile.highestFloor + 1);
                GameSession.I.Profile.selectedFloor = Mathf.Min(maxUnlock, GameSession.I.Profile.selectedFloor + 1);
                if (LanRoomBridge.I.IsHosting) LanRoomBridge.I.BroadcastFromHost();
                RefreshUI();
            });

            BindButton(_btnCustomizeClass, () =>
            {
                GameSession.I.ReturnSceneAfterClassSelect = "Lobby";
                GameSession.GoToScene("ClassSelection");
            });

            BindButton(_btnStartDungeon, () =>
            {
                if (!GameSession.I.AllPartyReady())
                {
                    foreach (var m in GameSession.I.Party) if (m != null) m.ready = true;
                }
                if (LanRoomBridge.I.IsHosting)
                    LanRoomBridge.I.BroadcastStartDungeon();
                GameSession.GoToScene("Dungeon");
            });

            for (int i = 0; i < 4; i++)
            {
                int slot = i;
                if (i > 0 && _slotModeBtns != null && i < _slotModeBtns.Length)
                    BindButton(_slotModeBtns[i], () => OnToggleSlotHumanAI(slot));
                if (_slotPrevClsBtns != null && i < _slotPrevClsBtns.Length)
                    BindButton(_slotPrevClsBtns[i], () => OnCycleSlotClass(slot, -1));
                if (_slotNextClsBtns != null && i < _slotNextClsBtns.Length)
                    BindButton(_slotNextClsBtns[i], () => OnCycleSlotClass(slot, 1));
                if (_readyBtns != null && i < _readyBtns.Length)
                    BindButton(_readyBtns[i], () => OnToggleSlotReady(slot));
            }

            GameSession.I.OnPartyChanged -= RefreshUI;
            GameSession.I.OnPartyChanged += RefreshUI;
            RefreshUI();
        }

        private static void BindButton(Button btn, System.Action onClick)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                GameAudio.I?.PlaySfx(GameSfx.Click, 0.6f);
                onClick?.Invoke();
            });
        }

        private void OnHeroNameEdited(string val)
        {
            string clean = string.IsNullOrWhiteSpace(val) ? "Aether Hunter" : val.Trim();
            GameSession.I.Profile.heroName = clean;
            int mySlot = LanRoomBridge.I.IsConnectedClient ? Mathf.Clamp(LanRoomBridge.I.LocalSlotIndex, 0, 3) : 0;
            if (GameSession.I.Party[mySlot] != null)
                GameSession.I.Party[mySlot].playerName = clean;
            GameSession.I.SaveProfile();
            if (LanRoomBridge.I.IsConnectedClient)
                LanRoomBridge.I.NotifyClientSlotUpdate(GameSession.I.Party[mySlot].ready);
            else if (LanRoomBridge.I.IsHosting)
                LanRoomBridge.I.BroadcastFromHost();
            RefreshUI();
        }

        private void OnQuickClassClicked(int cid)
        {
            int mySlot = LanRoomBridge.I.IsConnectedClient ? Mathf.Clamp(LanRoomBridge.I.LocalSlotIndex, 0, 3) : 0;
            GameSession.I.SelectLocalClass(cid, 0);
            if (GameSession.I.Party[mySlot] != null)
            {
                GameSession.I.Party[mySlot].classId = cid;
                GameSession.I.Party[mySlot].buildId = 0;
            }
            if (mySlot == 0 && GameModeManager.IsSinglePlayer)
                GameModeManager.I.RefillRemainingSlotsWithAI();
            if (LanRoomBridge.I.IsConnectedClient)
                LanRoomBridge.I.NotifyClientSlotUpdate(GameSession.I.Party[mySlot].ready);
            else if (LanRoomBridge.I.IsHosting)
                LanRoomBridge.I.BroadcastFromHost();
            RefreshUI();
        }

        private void OnToggleSlotHumanAI(int slot)
        {
            var m = GameSession.I.Party[slot];
            if (m == null) return;
            m.isHuman = !m.isHuman;
            if (m.isHuman)
            {
                m.playerName = $"Co-op P{slot + 1}";
                GameModeManager.I.StartCoOpHost(7777);
            }
            else
            {
                m.playerName = GameModeManager.GetDefaultAICompanionName(m.classId, slot);
            }
            if (LanRoomBridge.I.IsHosting) LanRoomBridge.I.BroadcastFromHost();
            RefreshUI();
        }

        private void OnCycleSlotClass(int slot, int delta)
        {
            var m = GameSession.I.Party[slot];
            if (m == null) return;
            m.classId = (m.classId + 10 + delta) % 10;
            m.buildId = 0;
            if (slot == 0) GameSession.I.SelectLocalClass(m.classId, 0);
            else if (!m.isHuman) m.playerName = GameModeManager.GetDefaultAICompanionName(m.classId, slot);
            if (LanRoomBridge.I.IsHosting) LanRoomBridge.I.BroadcastFromHost();
            RefreshUI();
        }

        private void OnToggleSlotReady(int slot)
        {
            var m = GameSession.I.Party[slot];
            if (m == null) return;
            m.ready = !m.ready;
            if (LanRoomBridge.I.IsConnectedClient && slot == LanRoomBridge.I.LocalSlotIndex)
                LanRoomBridge.I.NotifyClientSlotUpdate(m.ready);
            else if (LanRoomBridge.I.IsHosting)
                LanRoomBridge.I.BroadcastFromHost();
            RefreshUI();
        }

        public void Build()
        {
            if (Application.isPlaying)
            {
                GameAudio.I.PlayMusic(GameMusic.MainMenu);
                var _ = GameModeManager.I; // Ensure GameModeManager is active
            }
            Build3DLobbyStage();

            var canvas = GameUIKit.CreateCanvas("LobbyCanvas");
            _lobbyCanvas = canvas;
            var root = canvas.transform;

            // Top Ornate Gothic Header Bar
            var top = GameUIKit.MakeStyledPanel("TopBar", root, UIPanelStyle.Ornate,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -84f), new Vector2(-24f, -12f));

            GameUIKit.MakeIcon("TopCrest", top.transform, GameUIKit.GetIcon("crown"),
                new Vector2(0f, 0.16f), new Vector2(0f, 0.84f), new Vector2(18f, 0f), new Vector2(64f, 0f));

            _roomHeaderTxt = GameUIKit.MakeText("RoomTitle", top.transform, "", 21, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0.52f, 1f), new Vector2(74f, 0f), Vector2.zero);

            _netStatusTxt = GameUIKit.MakeText("NetStatus", top.transform, "", 13, FontStyle.Bold, GameUIKit.ColMuted, TextAnchor.MiddleRight,
                new Vector2(0.48f, 0f), new Vector2(0.83f, 1f), Vector2.zero, new Vector2(-14f, 0f));

            _btnBackMenu = GameUIKit.MakeStyledButton("BackMenu", top.transform, "← MAIN MENU", 15, UIButtonStyle.Secondary,
                new Vector2(0.84f, 0.16f), new Vector2(0.985f, 0.84f), Vector2.zero, Vector2.zero,
                () => GameSession.GoToScene("MainMenu"));

            // Left Party & Room Command Panel (UI_Panel_Lobby)
            var left = GameUIKit.MakeStyledPanel("LeftControls", root, UIPanelStyle.Lobby,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(24f, 24f), new Vector2(468f, -96f));

            // 1. Character Name Input + Quick 4-Class Selector (Knight, Mage, Ranger, Cleric)
            GameUIKit.MakeText("SecHero", left.transform, "1. TÊN NHÂN VẬT & CHỌN HỆ PHÁI NHANH", 14, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -36f), new Vector2(-18f, -10f));

            GameUIKit.MakeText("NameLbl", left.transform, "Tên Nhân Vật:", 13, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 1f), new Vector2(0.34f, 1f), new Vector2(0f, -76f), new Vector2(0f, -38f));

            var nameBg = GameUIKit.MakeStyledPanel("HeroNameBox", left.transform, UIPanelStyle.Inventory,
                new Vector2(0.34f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, -76f), new Vector2(0f, -38f));
            var nameTxt = GameUIKit.MakeText("Text", nameBg.transform, GameSession.I.Profile.heroName, 14, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(10f, 2f), new Vector2(-10f, -2f));
            _heroNameInput = nameBg.gameObject.AddComponent<InputField>();
            _heroNameInput.textComponent = nameTxt;
            _heroNameInput.text = GameSession.I.Profile.heroName;
            _heroNameInput.onEndEdit.AddListener(OnHeroNameEdited);

            string[] quickClassLabels = { "Knight\n(Lyra)", "Mage\n(Kael)", "Ranger\n(Seraphina)", "Cleric\n(Arthur)" };
            for (int c = 0; c < 4; c++)
            {
                int cid = QuickClassIds[c];
                float xMin = 0.04f + c * 0.235f;
                float xMax = xMin + 0.22f;
                _quickClassBtns[c] = GameUIKit.MakeStyledButton("QuickCls_" + cid, left.transform, quickClassLabels[c], 12, UIButtonStyle.Secondary,
                    new Vector2(xMin, 1f), new Vector2(xMax, 1f), new Vector2(0f, -126f), new Vector2(0f, -82f),
                    () => OnQuickClassClicked(cid));
            }

            GameUIKit.MakeGothicDivider("DivHero", left.transform,
                new Vector2(0.05f, 1f), new Vector2(0.95f, 1f), new Vector2(0f, -142f), new Vector2(0f, -128f));

            // 2. GameMode & Multiplayer Co-op Controls (SinglePlayer vs Host / Join)
            GameUIKit.MakeText("Sec1", left.transform, "2. CHẾ ĐỘ CHƠI ĐƠN & MULTIPLAYER CO-OP (2–4 NGƯỜI)", 14, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -168f), new Vector2(-18f, -142f));

            _btnModeSingle = GameUIKit.MakeStyledButton("ModeSingle", left.transform, "Chơi Đơn (1P + 3 AI)", 13, UIButtonStyle.Selected,
                new Vector2(0.04f, 1f), new Vector2(0.49f, 1f), new Vector2(0f, -212f), new Vector2(0f, -170f), () =>
                {
                    GameModeManager.I.SetSinglePlayerMode();
                    RefreshUI();
                });

            _btnModeHost = GameUIKit.MakeStyledButton("HostLan", left.transform, "Tạo Phòng (Host)", 13, UIButtonStyle.Primary,
                new Vector2(0.51f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, -212f), new Vector2(0f, -170f), () =>
                {
                    GameModeManager.I.StartCoOpHost(7777);
                    RefreshUI();
                });

            // Host IP & Room Code Display Plaque
            var hostBadge = GameUIKit.MakeStyledPanel("HostInfoPlaque", left.transform, UIPanelStyle.Standard,
                new Vector2(0.04f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, -248f), new Vector2(0f, -216f));
            _hostInfoBadgeTxt = GameUIKit.MakeText("HostInfoTxt", hostBadge.transform, "", 12, FontStyle.Bold, GameUIKit.ColGold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(6f, 0f), new Vector2(-6f, 0f));

            // IP / Room Code Input Field + Join Button
            var ipBg = GameUIKit.MakeStyledPanel("IpBox", left.transform, UIPanelStyle.Inventory,
                new Vector2(0.04f, 1f), new Vector2(0.56f, 1f), new Vector2(0f, -294f), new Vector2(0f, -252f));
            var ipTxt = GameUIKit.MakeText("Text", ipBg.transform, "127.0.0.1", 13, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(6f, 2f), new Vector2(-6f, -2f));
            _ipInput = ipBg.gameObject.AddComponent<InputField>();
            _ipInput.textComponent = ipTxt;
            _ipInput.text = "127.0.0.1";

            _btnJoinLan = GameUIKit.MakeStyledButton("JoinLan", left.transform, "Tham Gia Phòng (Join)", 13, UIButtonStyle.Primary,
                new Vector2(0.58f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, -294f), new Vector2(0f, -252f), () =>
                {
                    GameModeManager.I.JoinCoOpRoom(_ipInput.text, 7777);
                    RefreshUI();
                });

            GameUIKit.MakeGothicDivider("Div1", left.transform,
                new Vector2(0.05f, 1f), new Vector2(0.95f, 1f), new Vector2(0f, -310f), new Vector2(0f, -296f));

            // 3. Tactical Party Presets
            GameUIKit.MakeText("Sec2", left.transform, "3. ĐỘI HÌNH CHIẾN THUẬT MẪU:", 14, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -334f), new Vector2(-18f, -310f));

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
                float topY = -338f - i * 38f;
                _presetBtns[i] = GameUIKit.MakeStyledButton("Preset" + i, left.transform, presetNames[i], 12, UIButtonStyle.Secondary,
                    new Vector2(0.04f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, topY - 34f), new Vector2(0f, topY), () =>
                    {
                        GameSession.I.ApplyPartyPreset(idx);
                        if (_heroNameInput != null) _heroNameInput.text = GameSession.I.Profile.heroName;
                        if (LanRoomBridge.I.IsHosting) LanRoomBridge.I.BroadcastFromHost();
                        RefreshUI();
                    });
            }

            // 4. Dungeon Floor Difficulty Selector
            GameUIKit.MakeText("Sec3", left.transform, "4. ĐỘ KHÓ TẦNG HẦM NGỤC:", 14, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0.46f, 1f), new Vector2(18f, -536f), new Vector2(0f, -494f));

            _btnFloorMinus = GameUIKit.MakeStyledButton("FloorMinus", left.transform, "-", 20, UIButtonStyle.Secondary,
                new Vector2(0.45f, 1f), new Vector2(0.57f, 1f), new Vector2(0f, -536f), new Vector2(0f, -494f), () =>
                {
                    GameSession.I.Profile.selectedFloor = Mathf.Max(1, GameSession.I.Profile.selectedFloor - 1);
                    if (LanRoomBridge.I.IsHosting) LanRoomBridge.I.BroadcastFromHost();
                    RefreshUI();
                });

            var floorFrame = GameUIKit.MakeStyledPanel("FloorBadge", left.transform, UIPanelStyle.Ornate,
                new Vector2(0.58f, 1f), new Vector2(0.83f, 1f), new Vector2(0f, -536f), new Vector2(0f, -494f));
            _floorTxt = GameUIKit.MakeText("FloorLbl", floorFrame.transform, "", 13, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _btnFloorPlus = GameUIKit.MakeStyledButton("FloorPlus", left.transform, "+", 20, UIButtonStyle.Secondary,
                new Vector2(0.84f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, -536f), new Vector2(0f, -494f), () =>
                {
                    int maxUnlock = Mathf.Max(3, GameSession.I.Profile.highestFloor + 1);
                    GameSession.I.Profile.selectedFloor = Mathf.Min(maxUnlock, GameSession.I.Profile.selectedFloor + 1);
                    if (LanRoomBridge.I.IsHosting) LanRoomBridge.I.BroadcastFromHost();
                    RefreshUI();
                });

            // Bottom Action Buttons
            _btnCustomizeClass = GameUIKit.MakeStyledButton("CustomizeClass", left.transform,
                "ĐỀN 10 CLASS & CHUYÊN MÔN HÓA BUILD →", 14, UIButtonStyle.Tab,
                new Vector2(0.04f, 0f), new Vector2(0.96f, 0f), new Vector2(0f, 84f), new Vector2(0f, 132f), () =>
                {
                    GameSession.I.ReturnSceneAfterClassSelect = "Lobby";
                    GameSession.GoToScene("ClassSelection");
                },
                GameUIKit.GetIcon("crown"));

            _btnStartDungeon = GameUIKit.MakeStyledButton("StartDungeon", left.transform,
                "SẴN SÀNG / BẮT ĐẦU (START DUNGEON)", 17, UIButtonStyle.Primary,
                new Vector2(0.04f, 0f), new Vector2(0.96f, 0f), new Vector2(0f, 16f), new Vector2(0f, 74f), () =>
                {
                    if (!GameSession.I.AllPartyReady())
                    {
                        foreach (var m in GameSession.I.Party) if (m != null) m.ready = true;
                    }
                    if (LanRoomBridge.I.IsHosting)
                        LanRoomBridge.I.BroadcastStartDungeon();
                    GameSession.GoToScene("Dungeon");
                },
                GameUIKit.GetIcon("crossed_swords"));

            // Bottom-Right: 4 Ornate Gothic Party Member Cards (Slots 0..3)
            for (int i = 0; i < 4; i++)
            {
                int slot = i;
                float xMin = 0.258f + i * 0.181f;
                float xMax = xMin + 0.172f;

                var card = GameUIKit.MakeStyledPanel("SlotCard" + i, root, UIPanelStyle.Character,
                    new Vector2(xMin, 0f), new Vector2(xMax, 0f), new Vector2(0f, 24f), new Vector2(0f, 320f));
                _slotCards[i] = card.gameObject;

                // Top Slot Type Badge ([NGƯỜI CHƠI CHÍNH] / [NGƯỜI CHƠI CO-OP] / [AI ĐỒNG ĐỘI])
                _slotRoleBadges[i] = GameUIKit.MakeText("SlotBadge", card.transform, "", 11, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                    new Vector2(0.06f, 0.87f), new Vector2(0.58f, 0.98f), Vector2.zero, Vector2.zero);

                if (i > 0)
                {
                    // Button to toggle AI slot <-> Co-op Human Partner slot (or kick back to AI)
                    _slotModeBtns[i] = GameUIKit.MakeStyledButton("ToggleHumanAI", card.transform, "AI ⇄ Co-op", 11, UIButtonStyle.Secondary,
                        new Vector2(0.58f, 0.87f), new Vector2(0.94f, 0.975f), Vector2.zero, Vector2.zero,
                        () => OnToggleSlotHumanAI(slot));
                }

                // Portrait Frame
                var pFrame = GameUIKit.MakeStyledPanel("PortraitFrame", card.transform, UIPanelStyle.Ornate,
                    new Vector2(0.06f, 0.50f), new Vector2(0.40f, 0.85f), Vector2.zero, Vector2.zero);

                var pImg = GameUIKit.MakePanel("Ic", pFrame.transform,
                    new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero, Color.white);
                pImg.preserveAspect = true;
                _slotPortraits[i] = pImg;

                _slotTitleTxts[i] = GameUIKit.MakeText("SlotTitle", card.transform, "", 15, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                    new Vector2(0.43f, 0.52f), new Vector2(0.95f, 0.85f), Vector2.zero, Vector2.zero);

                _slotSubTxts[i] = GameUIKit.MakeText("SlotSub", card.transform, "", 12, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.UpperLeft,
                    new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.49f), Vector2.zero, Vector2.zero);

                // Cycle Class Buttons
                _slotPrevClsBtns[i] = GameUIKit.MakeStyledButton("PrevCls", card.transform, "◄", 14, UIButtonStyle.Secondary,
                    new Vector2(0.06f, 0.19f), new Vector2(0.28f, 0.32f), Vector2.zero, Vector2.zero,
                    () => OnCycleSlotClass(slot, -1));

                GameUIKit.MakeText("CycleHint", card.transform, "Đổi Hệ Phái", 11, FontStyle.Bold, GameUIKit.ColMuted, TextAnchor.MiddleCenter,
                    new Vector2(0.28f, 0.19f), new Vector2(0.72f, 0.32f), Vector2.zero, Vector2.zero);

                _slotNextClsBtns[i] = GameUIKit.MakeStyledButton("NextCls", card.transform, "►", 14, UIButtonStyle.Secondary,
                    new Vector2(0.72f, 0.19f), new Vector2(0.94f, 0.32f), Vector2.zero, Vector2.zero,
                    () => OnCycleSlotClass(slot, 1));

                _readyBtns[i] = GameUIKit.MakeStyledButton("ReadyBtn", card.transform, "SẴN SÀNG", 13, UIButtonStyle.Selected,
                    new Vector2(0.06f, 0.035f), new Vector2(0.94f, 0.165f), Vector2.zero, Vector2.zero,
                    () => OnToggleSlotReady(slot),
                    GameUIKit.GetIcon("check_seal"));

                var btnIc = _readyBtns[i].transform.Find("BtnIcon")?.GetComponent<Image>();
                _readyIcons[i] = btnIc;
            }

            if (Application.isPlaying)
                GameSession.I.OnPartyChanged += RefreshUI;
            RefreshUI();
            GameUIKit.OptimizeCanvasHierarchy(canvas);
            _isPrebakedInScene = true;
            _runtimeInitialized = Application.isPlaying;
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
                Camera.main.transform.position = new Vector3(-0.85f, 2.25f, -6.5f);
                Camera.main.transform.rotation = Quaternion.Euler(8f, 5f, 0f);
            }

            _stageRoot = new GameObject("Lobby3DStage").transform;
            _stageRoot.SetParent(transform, false);

            // Place South wall well behind Camera so the South wall & colonnade never block the 4 Party Heroes
            ModularDungeonArchitect.BuildModularRoomShell(
                _stageRoot, new Vector3(0.8f, 0f, -0.5f), width: 20f, length: 20f, wallHeight: 5.5f,
                biome: DungeonBiome.ForgottenTemple, openSouthDoor: false, openNorthDoor: false);

            Material stoneMat = PbrMaterialLibrary.GothicStone();
            ObjMeshLoader.SpawnPiece("RunicPillarAltar", _stageRoot, new Vector3(0.8f, 0f, 4.4f), Quaternion.identity, Vector3.one * 1.1f, stoneMat, addBoxCollider: false);

            // Create 4 explicit spawn anchors aligned vertically above the 4 bottom party cards
            Plane floorPlane = new Plane(Vector3.up, Vector3.zero);
            for (int i = 0; i < 4; i++)
            {
                var sp = new GameObject($"LobbyHeroSpawn_{i}").transform;
                sp.SetParent(_stageRoot, false);
                Vector3 worldPos = new Vector3(-1.35f + i * 1.55f, 0f, (i % 2 == 0) ? -0.2f : 0.3f);
                if (Camera.main != null)
                {
                    float cardCenterViewportX = 0.258f + i * 0.181f + 0.086f;
                    Ray ray = Camera.main.ViewportPointToRay(new Vector3(cardCenterViewportX, 0.36f, 0f));
                    if (floorPlane.Raycast(ray, out float enter))
                    {
                        worldPos = ray.GetPoint(enter);
                        worldPos.y = 0f;
                    }
                }
                sp.position = worldPos;
                _lobbySpawnPoints[i] = sp;
            }

            // Warm overhead sanctuary key light + fill light so all 4 3D Hero models & Gothic backdrop show rich PBR armor & faces
            var keyGo = new GameObject("LobbyStageKeyLight");
            keyGo.transform.SetParent(_stageRoot, false);
            keyGo.transform.position = new Vector3(0.8f, 3.6f, -3.8f);
            var kl = keyGo.AddComponent<Light>();
            kl.type = LightType.Point;
            kl.color = new Color(1.0f, 0.90f, 0.72f);
            kl.range = 20f;
            kl.intensity = 2.8f;

            var rimGo = new GameObject("LobbyStageRimLight");
            rimGo.transform.SetParent(_stageRoot, false);
            rimGo.transform.position = new Vector3(1.2f, 3.2f, 2.4f);
            var rl = rimGo.AddComponent<Light>();
            rl.type = LightType.Point;
            rl.color = new Color(0.96f, 0.72f, 0.36f);
            rl.range = 15f;
            rl.intensity = 1.75f;
        }

        private void RefreshUI()
        {
            var s = GameSession.I;
            s.EnsureDefaultParty();
            int humans = GameModeManager.GetHumanPlayerCount();
            int ais = GameModeManager.GetAICompanionCount();
            string modeLabel = GameModeManager.IsMultiplayerCoOp
                ? $"MULTIPLAYER CO-OP ({humans} Người + {ais} AI)"
                : $"CHƠI ĐƠN OFFLINE (1 Người + {ais} AI)";
            _roomHeaderTxt.text = $"SẢNH TỔ ĐỘI: {s.RoomCode}  •  {modeLabel}";
            _netStatusTxt.text = LanRoomBridge.I.StatusText;

            string localIp = LanRoomBridge.GetLocalIPv4();
            if (_hostInfoBadgeTxt != null)
            {
                _hostInfoBadgeTxt.text = GameModeManager.IsMultiplayerCoOp
                    ? $"IP Phòng: <color=#85F8B2>{localIp}:7777</color>  |  Mã Phòng: <color=#F8DC85>{s.RoomCode}</color>  |  Loot Riêng"
                    : $"Chế độ: <color=#F8DC85>Chơi Đơn Offline 100%</color> (Nhấn 'Tạo Phòng' để mở Co-op 2–4 Người)";
            }

            if (_btnModeSingle != null)
                GameUIKit.SetButtonStyle(_btnModeSingle, GameModeManager.IsSinglePlayer ? UIButtonStyle.Selected : UIButtonStyle.Secondary);
            if (_btnModeHost != null)
                GameUIKit.SetButtonStyle(_btnModeHost, GameModeManager.IsMultiplayerCoOp ? UIButtonStyle.Selected : UIButtonStyle.Primary);

            int mySlot = LanRoomBridge.I.IsConnectedClient ? Mathf.Clamp(LanRoomBridge.I.LocalSlotIndex, 0, 3) : 0;
            int myClass = s.Party[mySlot] != null ? s.Party[mySlot].classId : s.Profile.selectedClass;
            for (int c = 0; c < 4; c++)
            {
                if (_quickClassBtns[c] != null)
                    GameUIKit.SetButtonStyle(_quickClassBtns[c], QuickClassIds[c] == myClass ? UIButtonStyle.Selected : UIButtonStyle.Secondary);
            }

            int maxUnlock = Mathf.Max(3, s.Profile.highestFloor + 1);
            _floorTxt.text = $"Tầng {s.Profile.selectedFloor} / {maxUnlock}";

            for (int i = 0; i < 4; i++)
            {
                var m = s.Party[i];
                bool active = m != null && m.active;
                _slotCards[i].SetActive(active);
                if (_stageModels[i] != null) BakedAssetRegistry.DestroySafe(_stageModels[i]);
                if (_stageRings[i] != null) BakedAssetRegistry.DestroySafe(_stageRings[i]);

                if (!active) continue;

                ClassDef cls = GameDB.GetClass(m.classId);
                string buildName = cls.builds[Mathf.Clamp(m.buildId, 0, cls.builds.Length - 1)];

                if (_slotRoleBadges[i] != null)
                {
                    if (i == 0)
                        _slotRoleBadges[i].text = "<color=#72FFA8>★ CHỦ PHÒNG (HOST)</color>";
                    else if (m.isHuman)
                        _slotRoleBadges[i].text = $"<color=#6CE0FF>● NGƯỜI CHƠI #{i + 1}</color>";
                    else
                        _slotRoleBadges[i].text = "<color=#F8DC85>◆ AI ĐỒNG ĐỘI</color>";
                }

                if (_slotPortraits[i] != null)
                    _slotPortraits[i].sprite = GameUIKit.GetClassIcon(m.classId);

                _slotTitleTxts[i].text = $"{m.playerName}\n<size=12><color=#EDE0BA>Cấp {m.level} • {(m.isHuman ? "Loot Riêng" : "Utility AI")}</color></size>";
                _slotSubTxts[i].text = $"<color=#F8DC85>{cls.name} — {buildName}</color>\n<size=11><color=#BDA685>{cls.roleText}</color></size>";

                var rLbl = _readyBtns[i].transform.Find("Label")?.GetComponent<Text>();
                if (rLbl != null)
                    rLbl.text = m.ready ? "ĐÃ SẴN SÀNG (READY)" : "CHƯA SẴN SÀNG";

                GameUIKit.SetButtonStyle(_readyBtns[i], m.ready ? UIButtonStyle.Selected : UIButtonStyle.Secondary);
                if (_readyIcons[i] != null)
                    _readyIcons[i].sprite = GameUIKit.GetIcon(m.ready ? "check_seal" : "lock_iron");

                // Spawn 3D Hero directly at LobbyHeroSpawn_i above card i with glowing floor ring
                Transform spawnAnchor = _lobbySpawnPoints[i] != null ? _lobbySpawnPoints[i] : _stageRoot;
                Animator anim;
                var model = CharacterVisualFactory.BuildHeroVisual(cls, spawnAnchor, out anim, m.buildId);
                model.transform.localPosition = Vector3.zero;
                model.transform.localScale = Vector3.one;
                if (Camera.main != null)
                {
                    Vector3 toCam = Camera.main.transform.position - spawnAnchor.position;
                    toCam.y = 0f;
                    if (toCam.sqrMagnitude > 0.01f)
                        model.transform.rotation = Quaternion.LookRotation(toCam.normalized);
                }
                else
                {
                    model.transform.rotation = Quaternion.Euler(0f, 180f + (i - 1.5f) * -8f, 0f);
                }

                Color ringCol = ClassSpecializationSystem.GetBuildAccentColor(m.classId, m.buildId, cls.color);
                _stageRings[i] = CharacterVisualFactory.SpawnGroundGlowRing(spawnAnchor, ringCol, 0.62f, $"LobbyRing_{i}");
                _stageModels[i] = model;
            }
        }
    }
}

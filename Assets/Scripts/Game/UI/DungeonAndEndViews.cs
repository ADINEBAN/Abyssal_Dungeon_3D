using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssGame
{
    /// <summary>
    /// Redesigned Ancient Gothic Dark Fantasy Dungeon HUD:
    /// - Top-Left Party Panel (up to 4 hunters: Avatar, Name/Class/Level, Crimson HP + Sapphire MP bars, Alive/Downed/Dead & Party Status)
    /// - Top-Center Animated Quest Notification (Antique Bronze-Gold frame, dynamic icon, title + description, smooth fade/slide) + 3-Phase Boss Bar
    /// - Top-Right Ornate Gothic Minimap (Fog-of-War explored/unexplored rooms, corridors, locked/open doors, boss/treasure/objective markers, clickable to expand [M])
    /// - Right-Side Gothic Menu ([I] Hành Trang & Build, [M] Bản Đồ Chiến Thuật, [K] Kỹ Năng, [Esc] Tạm Dừng)
    /// - Bottom-Left Player Status (Portrait + Level badge, Class, Gold plaque, Crimson HP + Gold Shield edge, Sapphire MP, Emerald Stamina, EXP)
    /// - Bottom-Center Skill Hotbar (LMB, RMB/X, Q/1, 2/Z, E/3, R/4 with Radial360 cooldowns, Insufficient-Mana state, Ready pulse, Space Dodge, H Potion)
    /// </summary>
    public class DungeonHUDView : MonoBehaviour
    {
        private static Sprite _whiteFillSprite;

        private RectTransform _hpFill, _shieldFill, _mpFill, _staFill, _expFill;
        private Text _hpTxt, _mpTxt, _staTxt, _expTxt, _heroMetaTxt, _goldTxt, _levelBadgeTxt, _statusTxt;

        // Skill Hotbar (4 Class Skills + Basic/Heavy + Dodge + Potion)
        private readonly Image[] _skillIcons = new Image[4];
        private readonly Image[] _skillCdOverlays = new Image[4];
        private readonly Image[] _skillOomOverlays = new Image[4];
        private readonly Image[] _skillReadyGlows = new Image[4];
        private readonly Text[] _skillCdTexts = new Text[4];
        private readonly Text[] _skillRankTexts = new Text[4];
        private readonly Text[] _skillCostTexts = new Text[4];
        private readonly bool[] _skillWasOnCd = new bool[4];
        private readonly float[] _skillReadyFlash = new float[4];

        private Image _basicWpnCdOverlay, _heavyWpnCdOverlay, _heavyOomOverlay, _dodgeCdOverlay, _potionCdOverlay;
        private Text _basicWpnCdTxt, _basicWpnComboTxt, _heavyWpnCdTxt;
        private Text _dodgeCdTxt, _potionCountTxt, _potionCdTxt;

        // Party Panel (up to 4 members with HP + MP + Alive/Dead + Role status)
        private Transform _partyListRoot;
        private Text _partyStatusTagTxt;
        private struct PartyMemberWidget
        {
            public Image portrait;
            public RectTransform hpFill;
            public RectTransform mpFill;
            public Text nameTxt;
            public Text statusTxt;
            public Text hpTxt;
        }
        private readonly List<PartyMemberWidget> _partyWidgets = new List<PartyMemberWidget>();

        // Animated Quest Notification + Banner + Interact + Loot
        private RectTransform _questRect;
        private CanvasGroup _questCg;
        private Image _questIconImg;
        private Text _objectiveTxt, _directorTxt;
        private string _lastObjectiveStr = "";
        private float _questAnimTimer;

        private RectTransform _bannerRect;
        private CanvasGroup _bannerCg;
        private GameObject _bannerBox, _interactBox, _lootBox;
        private Text _bannerTxt, _interactTxt, _lootFeedTxt;

        // Boss UI
        private GameObject _bossPanel;
        private RectTransform _bossHpFill;
        private Text _bossTitleTxt, _bossSkillTxt;

        // Minimap (192x192 crisp Gothic Fog-of-War + Door/Boss/Treasure/Objective markers)
        private Texture2D _mapTex;
        private Color32[] _mapPixels;
        private const int MapSize = 192;
        private float _mapRefreshTimer;

        // Modals
        private GameObject _charModal;
        private GameObject _skillsModal;
        private GameObject _pauseModal;
        private GameObject _settingsModal;
        private GameObject _fullMapModal;
        private readonly List<string> _lootLogs = new List<string>();
        private float _lootLogTimer;

        private static Sprite GetWhiteFillSprite()
        {
            if (_whiteFillSprite != null) return _whiteFillSprite;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var cols = new Color32[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255) };
            tex.SetPixels32(cols);
            tex.Apply(false, false);
            _whiteFillSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            return _whiteFillSprite;
        }

        public void Build()
        {
            var canvas = GameUIKit.CreateCanvas("DungeonHUDCanvas");
            var root = canvas.transform;
            ScreenCombatFeedbackUI.Attach(root);

            Color panelUmberTint = new Color(0.96f, 0.90f, 0.84f, 0.96f);
            Color hpCrimson = new Color(0.76f, 0.14f, 0.15f, 1f);
            Color mpSapphire = new Color(0.15f, 0.43f, 0.84f, 1f);
            Color staEmerald = new Color(0.23f, 0.62f, 0.33f, 1f);
            Color expGold = new Color(0.80f, 0.60f, 0.22f, 1f);

            // =========================================================================
            // 1. TOP-CENTER: QUEST NOTIFICATION (Animated Bronze-Gold Gothic Frame)
            // =========================================================================
            var objBox = GameUIKit.MakeStyledPanel("HUD_QuestNotification", root, UIPanelStyle.Notification,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-390f, -92f), new Vector2(390f, -12f),
                panelUmberTint, GameUIKit.ColGold);
            _questRect = objBox.rectTransform;
            _questCg = objBox.gameObject.AddComponent<CanvasGroup>();
            _questCg.alpha = 1f;

            _questIconImg = GameUIKit.MakeIcon("QuestIcon", objBox.transform, GameUIKit.GetIcon("crown"),
                new Vector2(0f, 0.18f), new Vector2(0f, 0.82f), new Vector2(16f, 0f), new Vector2(56f, 0f));

            _objectiveTxt = GameUIKit.MakeText("QuestTitle", objBox.transform, "TIẾN VÀO ĐẠI THÁNH ĐƯỜNG VỰC THẲM", 16, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.48f), new Vector2(1f, 0.94f), new Vector2(64f, 0f), new Vector2(-64f, 0f));

            _directorTxt = GameUIKit.MakeText("QuestDesc", objBox.transform, "Khám phá các khu vực hầm ngục, thu thập cổ vật và tiêu diệt Abyssal Knight.", 12, FontStyle.Italic, GameUIKit.ColParchment, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.08f), new Vector2(1f, 0.48f), new Vector2(64f, 0f), new Vector2(-64f, 0f));

            // 2. Boss 3-Phase Health Bar (Engraved Dark Bronze & Blood-Crimson Frame below Quest Notification)
            var bossImg = GameUIKit.MakeStyledPanel("BossHealthPanel", root, UIPanelStyle.Ornate,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-430f, -184f), new Vector2(430f, -98f),
                new Color(1f, 0.90f, 0.86f, 0.98f), GameUIKit.ColDanger);
            _bossPanel = bossImg.gameObject;

            GameUIKit.MakeIcon("BossSkull", bossImg.transform, GameUIKit.GetIcon("skull_boss"),
                new Vector2(0f, 0.48f), new Vector2(0f, 0.94f), new Vector2(14f, 0f), new Vector2(54f, 0f));

            _bossTitleTxt = GameUIKit.MakeText("BossTitle", bossImg.transform, "ABYSSAL KNIGHT — PHASE 1", 16, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.52f), new Vector2(0.62f, 0.96f), new Vector2(60f, 0f), Vector2.zero);

            _bossSkillTxt = GameUIKit.MakeText("BossSkill", bossImg.transform, "", 13, FontStyle.Bold, new Color(1f, 0.55f, 0.48f), TextAnchor.MiddleRight,
                new Vector2(0.52f, 0.52f), new Vector2(1f, 0.96f), Vector2.zero, new Vector2(-18f, 0f));

            var bossBar = GameUIKit.MakeBar("BossHpBar", bossImg.transform, hpCrimson,
                new Vector2(0.025f, 0.12f), new Vector2(0.975f, 0.48f), Vector2.zero, Vector2.zero);
            _bossHpFill = bossBar.fill;

            for (int pTick = 1; pTick <= 2; pTick++)
            {
                float nx = pTick / 3f;
                var tick = GameUIKit.MakeRect("PhaseTick_" + pTick, bossBar.bg.transform,
                    new Vector2(nx, 0f), new Vector2(nx, 1f), new Vector2(-1.5f, 2f), new Vector2(1.5f, -2f));
                tick.gameObject.AddComponent<Image>().color = GameUIKit.ColGoldBright;
            }
            _bossPanel.SetActive(false);

            // =========================================================================
            // 3. TOP-LEFT: PARTY PANEL (Up to 4 Players, Avatar, Class, Lv, HP + MP, Status)
            // =========================================================================
            var partyBox = GameUIKit.MakeStyledPanel("HUD_PartyPanel", root, UIPanelStyle.Lobby,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -342f), new Vector2(368f, -12f),
                panelUmberTint, GameUIKit.ColBronze);

            GameUIKit.MakeIcon("PartyIc", partyBox.transform, GameUIKit.GetIcon("shield"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -34f), new Vector2(36f, -10f));

            GameUIKit.MakeText("PartyHeader", partyBox.transform, "TỔ ĐỘI THỢ SĂN", 13, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0.58f, 1f), new Vector2(42f, -36f), new Vector2(0f, -8f));

            _partyStatusTagTxt = GameUIKit.MakeText("PartyStatusTag", partyBox.transform, "CO-OP 4/4", 11, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleRight,
                new Vector2(0.54f, 1f), new Vector2(1f, 1f), new Vector2(0f, -36f), new Vector2(-20f, -8f));

            _partyListRoot = GameUIKit.MakeRect("Members", partyBox.transform,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 10f), new Vector2(-10f, -40f));

            // =========================================================================
            // 4. TOP-RIGHT: ORNATE MINIMAP + 4-BUTTON RIGHT-SIDE MENU ([I], [M], [K], [Esc])
            // =========================================================================
            var mapFrame = GameUIKit.MakeStyledPanel("HUD_MinimapPanel", root, UIPanelStyle.Ornate,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-248f, -256f), new Vector2(-18f, -12f),
                panelUmberTint, GameUIKit.ColGold);

            GameUIKit.MakeText("MapTitle", mapFrame.transform, $"BẢN ĐỒ CỔ NGỮ • TẦNG {GameSession.I.Profile.selectedFloor}", 12, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(6f, -28f), new Vector2(-6f, -4f));

            var rawGo = GameUIKit.MakeRect("Radar", mapFrame.transform, Vector2.zero, Vector2.one, new Vector2(12f, 24f), new Vector2(-12f, -30f));
            var rawImg = rawGo.gameObject.AddComponent<RawImage>();
            _mapTex = new Texture2D(MapSize, MapSize, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            _mapPixels = new Color32[MapSize * MapSize];
            rawImg.texture = _mapTex;

            var radarOutline = rawGo.gameObject.AddComponent<Outline>();
            radarOutline.effectColor = GameUIKit.ColBronze;
            radarOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Click minimap directly to expand into Tactical Map [M]
            var mapExpandBtn = rawGo.gameObject.AddComponent<Button>();
            mapExpandBtn.targetGraphic = rawImg;
            mapExpandBtn.onClick.AddListener(() => { GameAudio.I?.PlaySfx(GameSfx.Click, 0.5f); ToggleFullMapModal(); });

            GameUIKit.MakeText("MapHint", mapFrame.transform, "Vàng: Bạn • Đỏ: Cửa khóa • [M] Mở rộng", 9, FontStyle.Normal, GameUIKit.ColParchment, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(14f, 9f), new Vector2(-14f, 25f));

            // Right-Side Menu (4 Gothic Buttons directly below Minimap)
            var rightMenu = GameUIKit.MakeRect("HUD_RightMenuPanel", root,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-248f, -432f), new Vector2(-18f, -262f));

            GameUIKit.MakeStyledButton("BtnInv", rightMenu, "Hành Trang & Build [I]", 12, UIButtonStyle.Primary,
                new Vector2(0f, 0.76f), new Vector2(1f, 1.00f), Vector2.zero, Vector2.zero,
                ToggleCharacterModal, GameUIKit.GetIcon("bag"));

            GameUIKit.MakeStyledButton("BtnFullMap", rightMenu, "Bản Đồ Chiến Thuật [M]", 12, UIButtonStyle.Secondary,
                new Vector2(0f, 0.51f), new Vector2(1f, 0.74f), Vector2.zero, Vector2.zero,
                ToggleFullMapModal, GameUIKit.GetIcon("portal"));

            GameUIKit.MakeStyledButton("BtnSkills", rightMenu, "Kỹ Năng [K]", 12, UIButtonStyle.Secondary,
                new Vector2(0f, 0.26f), new Vector2(1f, 0.49f), Vector2.zero, Vector2.zero,
                ToggleSkillsModal, GameUIKit.GetIcon("skill_tree"));

            GameUIKit.MakeStyledButton("BtnPause", rightMenu, "Tạm Dừng [Esc]", 12, UIButtonStyle.Secondary,
                new Vector2(0f, 0.00f), new Vector2(1f, 0.24f), Vector2.zero, Vector2.zero,
                TogglePauseModal, GameUIKit.GetIcon("settings"));

            // =========================================================================
            // 5. CENTER CROSSHAIR + ANIMATED BANNER TOAST + INTERACT PROMPT + LOOT FEED
            // =========================================================================
            var ch = GameUIKit.MakeRect("Crosshair", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-3.5f, -3.5f), new Vector2(3.5f, 3.5f));
            var chImg = ch.gameObject.AddComponent<Image>();
            chImg.color = new Color(0.95f, 0.84f, 0.52f, 0.60f);
            chImg.raycastTarget = false;

            var banImg = GameUIKit.MakeStyledPanel("Banner", root, UIPanelStyle.Notification,
                new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f), new Vector2(-430f, -26f), new Vector2(430f, 26f),
                panelUmberTint, GameUIKit.ColGoldBright);
            banImg.raycastTarget = false;
            _bannerBox = banImg.gameObject;
            _bannerRect = banImg.rectTransform;
            _bannerCg = _bannerBox.AddComponent<CanvasGroup>();
            _bannerCg.blocksRaycasts = false;
            _bannerTxt = GameUIKit.MakeText("Txt", banImg.transform, "", 17, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, 0f));
            _bannerTxt.raycastTarget = false;
            _bannerBox.SetActive(false);

            var intImg = GameUIKit.MakeStyledPanel("InteractPrompt", root, UIPanelStyle.Notification,
                new Vector2(0.5f, 0.23f), new Vector2(0.5f, 0.23f), new Vector2(-275f, -22f), new Vector2(275f, 22f),
                panelUmberTint, GameUIKit.ColGoldBright);
            intImg.raycastTarget = false;
            _interactBox = intImg.gameObject;
            _interactTxt = GameUIKit.MakeText("Txt", intImg.transform, "[F] Tương tác", 15, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(14f, 0f), new Vector2(-14f, 0f));
            _interactTxt.raycastTarget = false;
            _interactBox.SetActive(false);

            var lootPanel = GameUIKit.MakeStyledPanel("LootNotificationPanel", root, UIPanelStyle.Notification,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-470f, 150f), new Vector2(-18f, 286f),
                panelUmberTint, GameUIKit.ColBronze);
            lootPanel.raycastTarget = false;
            _lootBox = lootPanel.gameObject;
            _lootFeedTxt = GameUIKit.MakeText("LootFeed", lootPanel.transform, "", 12, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, new Vector2(14f, 8f), new Vector2(-14f, -8f));
            _lootFeedTxt.raycastTarget = false;
            _lootBox.SetActive(false);

            // =========================================================================
            // 6. BOTTOM-LEFT: PLAYER STATUS (Portrait, Class, Lv, Gold, HP, MP, Stamina, EXP)
            // =========================================================================
            var playerBox = GameUIKit.MakeStyledPanel("HUD_PlayerStatusPanel", root, UIPanelStyle.Ornate,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 16f), new Vector2(468f, 198f),
                panelUmberTint, GameUIKit.ColGold);

            ClassDef cls = GameDB.GetClass(GameSession.I.Profile.selectedClass);
            var portraitFrame = GameUIKit.MakeStyledPanel("PortraitBox", playerBox.transform, UIPanelStyle.Character,
                new Vector2(0f, 0.25f), new Vector2(0f, 0.92f), new Vector2(14f, 0f), new Vector2(102f, 0f),
                null, GameUIKit.ColGold);
            Sprite clsIcon = GameUIKit.GetClassIcon(cls.id);
            if (clsIcon != null)
            {
                GameUIKit.MakeIcon("ClsIcon", portraitFrame.transform, clsIcon,
                    new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero);
            }

            // Engraved Level Medallion tucked neatly at bottom-right of portrait
            var lvBadge = GameUIKit.MakePanel("LvBadge", portraitFrame.transform,
                new Vector2(0.48f, 0.0f), new Vector2(1.02f, 0.28f), Vector2.zero, Vector2.zero,
                new Color(0.08f, 0.06f, 0.05f, 0.96f), GameUIKit.ColGoldBright);
            _levelBadgeTxt = GameUIKit.MakeText("LvTxt", lvBadge.transform, "Lv.1", 11, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _heroMetaTxt = GameUIKit.MakeText("HeroMeta", playerBox.transform, "", 14, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.77f), new Vector2(0.68f, 0.95f), new Vector2(110f, 0f), Vector2.zero);

            // Dedicated Gold Plaque with Gold Coin Icon
            GameUIKit.MakeIcon("GoldIc", playerBox.transform, GameUIKit.GetIcon("gold"),
                new Vector2(0.69f, 0.78f), new Vector2(0.75f, 0.94f), Vector2.zero, Vector2.zero);
            _goldTxt = GameUIKit.MakeText("GoldTxt", playerBox.transform, "0 G", 13, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleRight,
                new Vector2(0.74f, 0.77f), new Vector2(1f, 0.95f), Vector2.zero, new Vector2(-14f, 0f));

            // HP Bar (Crimson Red) + Top-Edge Gold Shield Strip (never obscures HP text!)
            var hpBar = GameUIKit.MakeBar("HpBar", playerBox.transform, hpCrimson,
                new Vector2(0f, 0.56f), new Vector2(1f, 0.75f), new Vector2(110f, 0f), new Vector2(-14f, 0f), 12);
            _hpFill = hpBar.fill;
            _hpTxt = hpBar.label;

            _shieldFill = GameUIKit.MakeRect("ShieldStrip", hpBar.bg.transform, new Vector2(0f, 0.74f), new Vector2(0f, 1f), new Vector2(4f, 0f), new Vector2(-4f, -2f));
            var shImg = _shieldFill.gameObject.AddComponent<Image>();
            shImg.color = new Color(0.96f, 0.85f, 0.48f, 0.92f);
            shImg.raycastTarget = false;
            hpBar.label.transform.SetAsLastSibling();

            // MP Bar (Sapphire Blue)
            var mpBar = GameUIKit.MakeBar("MpBar", playerBox.transform, mpSapphire,
                new Vector2(0f, 0.38f), new Vector2(1f, 0.54f), new Vector2(110f, 0f), new Vector2(-14f, 0f), 12);
            _mpFill = mpBar.fill;
            _mpTxt = mpBar.label;

            // Stamina Bar (Emerald Green)
            var staBar = GameUIKit.MakeBar("StaBar", playerBox.transform, staEmerald,
                new Vector2(0f, 0.21f), new Vector2(1f, 0.36f), new Vector2(110f, 0f), new Vector2(-14f, 0f), 11);
            _staFill = staBar.fill;
            _staTxt = staBar.label;

            // EXP Bar (Antique Gold)
            var expBar = GameUIKit.MakeBar("ExpBar", playerBox.transform, expGold,
                new Vector2(0f, 0.05f), new Vector2(1f, 0.18f), new Vector2(14f, 0f), new Vector2(-14f, 0f), 11);
            _expFill = expBar.fill;
            _expTxt = expBar.label;

            _statusTxt = GameUIKit.MakeText("Statuses", root, "", 13, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(22f, 202f), new Vector2(540f, 228f));

            // =========================================================================
            // 7. BOTTOM-CENTER: SKILL HOTBAR (LMB, RMB/X, Q/1, 2/Z, E/3, R/4, Space, H)
            // =========================================================================
            var skillBox = GameUIKit.MakeStyledPanel("HUD_SkillHotbarPanel", root, UIPanelStyle.Ornate,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-438f, 16f), new Vector2(468f, 128f),
                panelUmberTint, GameUIKit.ColGold);

            GameUIKit.GetWeaponSkillNames(cls.id, out string basicWpnName, out string heavyWpnName);
            const float slotW = 0.116f;
            const float slotStep = 0.1225f;
            const float startX = 0.014f;
            Sprite radialSp = GetWhiteFillSprite();

            // Slot 0: [LMB] Weapon Basic Attack
            {
                float x0 = startX;
                var wpnBasicBtn = GameUIKit.MakeStyledButton("WpnBasicSlot", skillBox.transform, "", 12, UIButtonStyle.Secondary,
                    new Vector2(x0, 0.10f), new Vector2(x0 + slotW, 0.90f), Vector2.zero, Vector2.zero, () =>
                    {
                        var inp = DungeonWorldBuilder.Instance?.LocalHero?.GetComponent<HeroPlayerInput>();
                        inp?.TriggerBasicAttackFromUI();
                    });

                GameUIKit.MakeIcon("WpnBasicIcon", wpnBasicBtn.transform, GameUIKit.GetWeaponAttackIcon(cls.id, false),
                    new Vector2(0.16f, 0.22f), new Vector2(0.84f, 0.80f), Vector2.zero, Vector2.zero,
                    new Color(1f, 1f, 1f, 0.96f));

                GameUIKit.MakeText("Key", wpnBasicBtn.transform, "LMB", 10, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.UpperLeft,
                    Vector2.zero, Vector2.one, new Vector2(5f, 2f), new Vector2(-4f, -3f));
                _basicWpnComboTxt = GameUIKit.MakeText("Combo", wpnBasicBtn.transform, "Đòn 1", 9, FontStyle.Bold, new Color(0.98f, 0.84f, 0.48f), TextAnchor.UpperRight,
                    Vector2.zero, Vector2.one, new Vector2(4f, 2f), new Vector2(-5f, -3f));
                GameUIKit.MakeText("Name", wpnBasicBtn.transform, basicWpnName, 10, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.LowerCenter,
                    Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -2f));

                _basicWpnCdOverlay = MakeRadialCooldownOverlay(wpnBasicBtn.transform, radialSp, out _basicWpnCdTxt, 15);
            }

            // Slot 1: [RMB / X] Weapon Heavy Technique (20 Stamina)
            {
                float x1 = startX + slotStep;
                var wpnHeavyBtn = GameUIKit.MakeStyledButton("WpnHeavySlot", skillBox.transform, "", 12, UIButtonStyle.Primary,
                    new Vector2(x1, 0.10f), new Vector2(x1 + slotW, 0.90f), Vector2.zero, Vector2.zero, () =>
                    {
                        var inp = DungeonWorldBuilder.Instance?.LocalHero?.GetComponent<HeroPlayerInput>();
                        inp?.TriggerHeavyAttackFromUI();
                    });

                GameUIKit.MakeIcon("WpnHeavyIcon", wpnHeavyBtn.transform, GameUIKit.GetWeaponAttackIcon(cls.id, true),
                    new Vector2(0.16f, 0.22f), new Vector2(0.84f, 0.80f), Vector2.zero, Vector2.zero,
                    new Color(1f, 1f, 1f, 0.96f));

                GameUIKit.MakeText("Key", wpnHeavyBtn.transform, "RMB/X", 10, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.UpperLeft,
                    Vector2.zero, Vector2.one, new Vector2(5f, 2f), new Vector2(-4f, -3f));
                GameUIKit.MakeText("Cost", wpnHeavyBtn.transform, "20TL", 9, FontStyle.Bold, new Color(0.55f, 0.94f, 0.64f), TextAnchor.UpperRight,
                    Vector2.zero, Vector2.one, new Vector2(4f, 2f), new Vector2(-5f, -3f));
                GameUIKit.MakeText("Name", wpnHeavyBtn.transform, heavyWpnName, 10, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.LowerCenter,
                    Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -2f));

                _heavyOomOverlay = MakeTintOverlay("LowStaOverlay", wpnHeavyBtn.transform, new Color(0.08f, 0.14f, 0.12f, 0.56f));
                _heavyWpnCdOverlay = MakeRadialCooldownOverlay(wpnHeavyBtn.transform, radialSp, out _heavyWpnCdTxt, 16);
            }

            // Slots 2..5: Class Active & Ultimate Skills (Accurate Hotkeys: Q/1, 2/Z, E/3, R/4)
            string[] hotkeys = { "Q / 1", "2 / Z", "E / 3", "R / 4" };
            for (int s = 0; s < 4; s++)
            {
                int slot = s;
                SkillDef sk = cls.skills[s];
                bool isUlt = sk.ultimate || s == 3;
                float xMin = startX + (s + 2) * slotStep;

                var btn = GameUIKit.MakeStyledButton("SkSlot" + s, skillBox.transform, "", 12,
                    isUlt ? UIButtonStyle.Primary : UIButtonStyle.Secondary,
                    new Vector2(xMin, 0.10f), new Vector2(xMin + slotW, 0.90f), Vector2.zero, Vector2.zero, () =>
                    {
                        var inp = DungeonWorldBuilder.Instance?.LocalHero?.GetComponent<HeroPlayerInput>();
                        inp?.TriggerSkillFromUIOrKey(slot);
                    });

                // Subtle antique-gold ready-pulse highlight border
                var readyRt = GameUIKit.MakeRect("ReadyGlow", btn.transform, Vector2.zero, Vector2.one, new Vector2(-2f, -2f), new Vector2(2f, 2f));
                var readyImg = readyRt.gameObject.AddComponent<Image>();
                readyImg.color = new Color(0.98f, 0.85f, 0.45f, 0f);
                readyImg.raycastTarget = false;
                var readyOl = readyRt.gameObject.AddComponent<Outline>();
                readyOl.effectColor = new Color(1f, 0.88f, 0.48f, 0.85f);
                readyOl.effectDistance = new Vector2(2f, -2f);
                _skillReadyGlows[s] = readyImg;

                _skillIcons[s] = GameUIKit.MakeIcon("SkIcon", btn.transform, GameUIKit.GetSkillIcon(sk),
                    new Vector2(0.16f, 0.24f), new Vector2(0.84f, 0.80f), Vector2.zero, Vector2.zero,
                    new Color(1f, 1f, 1f, 0.96f));

                GameUIKit.MakeText("Key", btn.transform, hotkeys[s], 10, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.UpperLeft,
                    Vector2.zero, Vector2.one, new Vector2(5f, 2f), new Vector2(-4f, -3f));

                _skillRankTexts[s] = GameUIKit.MakeText("Rank", btn.transform, "Lv.1", 9, FontStyle.Bold, new Color(0.70f, 0.94f, 0.74f), TextAnchor.UpperRight,
                    Vector2.zero, Vector2.one, new Vector2(4f, 2f), new Vector2(-5f, -3f));

                GameUIKit.MakeText("Name", btn.transform, sk.name, 9, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.LowerLeft,
                    Vector2.zero, Vector2.one, new Vector2(5f, 3f), new Vector2(-32f, -2f));

                _skillCostTexts[s] = GameUIKit.MakeText("Cost", btn.transform, $"{sk.mana:0}MP", 9, FontStyle.Bold, new Color(0.54f, 0.84f, 1f), TextAnchor.LowerRight,
                    Vector2.zero, Vector2.one, new Vector2(4f, 3f), new Vector2(-5f, -2f));

                // Insufficient Mana Overlay (deep cool-indigo tint)
                _skillOomOverlays[s] = MakeTintOverlay("OomOverlay", btn.transform, new Color(0.08f, 0.14f, 0.30f, 0.62f));

                // Radial 360 Cooldown Sweep Overlay
                _skillCdOverlays[s] = MakeRadialCooldownOverlay(btn.transform, radialSp, out _skillCdTexts[s], 17);
            }

            // Slot 6: [Space] Dodge Slot (clean icon + non-overlapping footer)
            float xDodge = startX + 6 * slotStep;
            var dodgeCard = GameUIKit.MakeStyledButton("DodgeSlot", skillBox.transform, "", 11, UIButtonStyle.Secondary,
                new Vector2(xDodge, 0.10f), new Vector2(xDodge + slotW, 0.90f), Vector2.zero, Vector2.zero, () =>
                {
                    var inp = DungeonWorldBuilder.Instance?.LocalHero?.GetComponent<HeroPlayerInput>();
                    inp?.TriggerDodgeFromUI();
                });
            GameUIKit.MakeIcon("DodgeIc", dodgeCard.transform, GameUIKit.GetIcon("dodge"),
                new Vector2(0.18f, 0.24f), new Vector2(0.82f, 0.80f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.92f));
            GameUIKit.MakeText("DodgeKey", dodgeCard.transform, "Space", 10, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(5f, 2f), new Vector2(-4f, -3f));
            GameUIKit.MakeText("DodgeLbl", dodgeCard.transform, "Né Đòn", 10, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.LowerCenter,
                Vector2.zero, Vector2.one, new Vector2(4f, 3f), new Vector2(-4f, -2f));
            _dodgeCdOverlay = MakeRadialCooldownOverlay(dodgeCard.transform, radialSp, out _dodgeCdTxt, 16);

            // Slot 7: [H] Consumable Health Potion Slot (clear icon + golden count badge + cooldown sweep)
            float xPot = startX + 7 * slotStep;
            var potBtn = GameUIKit.MakeStyledButton("PotionSlot", skillBox.transform, "", 11, UIButtonStyle.Danger,
                new Vector2(xPot, 0.10f), new Vector2(xPot + slotW, 0.90f), Vector2.zero, Vector2.zero, () =>
                {
                    var inp = DungeonWorldBuilder.Instance?.LocalHero?.GetComponent<HeroPlayerInput>();
                    inp?.TryUseHealthPotion();
                });
            GameUIKit.MakeIcon("PotIc", potBtn.transform, GameUIKit.GetIcon("potion"),
                new Vector2(0.18f, 0.22f), new Vector2(0.82f, 0.80f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.95f));
            GameUIKit.MakeText("PotKey", potBtn.transform, "H", 10, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(5f, 2f), new Vector2(-4f, -3f));
            GameUIKit.MakeText("PotLbl", potBtn.transform, "Bình Máu", 9, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.LowerLeft,
                Vector2.zero, Vector2.one, new Vector2(4f, 3f), new Vector2(-28f, -2f));
            _potionCountTxt = GameUIKit.MakeText("PotCount", potBtn.transform, "x5", 11, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.UpperRight,
                Vector2.zero, Vector2.one, new Vector2(4f, 2f), new Vector2(-5f, -3f));
            _potionCdOverlay = MakeRadialCooldownOverlay(potBtn.transform, radialSp, out _potionCdTxt, 16);

            GameUIKit.MakeText("ControlHint", root,
                "WASD: Di chuyển | LMB: Liên kích | RMB/X: Phá Giáp | Space: Né | Tab: Khóa mục tiêu | Q, 2/Z, E, R: Kỹ năng | H: Bình máu | I / K / M",
                11, FontStyle.Normal, GameUIKit.ColMuted, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-460f, 130f), new Vector2(480f, 150f));

            // Modals (Preserve all existing modals + new dedicated Skills [K] modal)
            _settingsModal = GameUIKit.BuildSettingsModal(root);
            _charModal = GameUIKit.BuildCharacterSheetModal(root, () => DungeonWorldBuilder.Instance?.LocalHero, SyncCursorLockState);
            _skillsModal = DungeonOverlayPanelsUI.BuildSkillTreeQuickModal(root, () => DungeonWorldBuilder.Instance?.LocalHero, SyncCursorLockState);
            _fullMapModal = DungeonOverlayPanelsUI.BuildFullDungeonMapModal(root, SyncCursorLockState);
            _pauseModal = BuildPauseModal(root);

            HeroPlayerInput.OnToggleInventory = ToggleCharacterModal;
            HeroPlayerInput.OnToggleSkills = ToggleSkillsModal;
            HeroPlayerInput.OnTogglePause = TogglePauseModal;

            if (DungeonWorldBuilder.Instance != null)
            {
                DungeonWorldBuilder.Instance.OnLootNotification += msg =>
                {
                    _lootLogs.Add("• " + msg);
                    if (_lootLogs.Count > 4) _lootLogs.RemoveAt(0);
                    _lootLogTimer = 6f;
                };
            }

            RebuildPartyWidgets();
        }

        private static Image MakeTintOverlay(string name, Transform parent, Color col)
        {
            var rt = GameUIKit.MakeRect(name, parent, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
            var img = rt.gameObject.AddComponent<Image>();
            img.color = col;
            img.raycastTarget = false;
            img.gameObject.SetActive(false);
            return img;
        }

        private static Image MakeRadialCooldownOverlay(Transform parent, Sprite fillSprite, out Text cdTxt, int fontSize)
        {
            var cdRt = GameUIKit.MakeRect("CdOverlay", parent, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
            var cdOv = cdRt.gameObject.AddComponent<Image>();
            cdOv.sprite = fillSprite;
            cdOv.type = Image.Type.Filled;
            cdOv.fillMethod = Image.FillMethod.Radial360;
            cdOv.fillOrigin = (int)Image.Origin360.Top;
            cdOv.fillClockwise = false;
            cdOv.fillAmount = 1f;
            cdOv.color = new Color(0.05f, 0.03f, 0.02f, 0.80f);
            cdOv.raycastTarget = false;

            cdTxt = GameUIKit.MakeText("CdTxt", cdOv.transform, "", fontSize, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            cdTxt.raycastTarget = false;
            cdOv.gameObject.SetActive(false);
            return cdOv;
        }

        private void OnDestroy()
        {
            HeroPlayerInput.OnToggleInventory = null;
            HeroPlayerInput.OnToggleSkills = null;
            HeroPlayerInput.OnTogglePause = null;
            if (_mapTex != null) Destroy(_mapTex);
        }

        private void RebuildPartyWidgets()
        {
            _partyWidgets.Clear();
            var dw = DungeonWorldBuilder.Instance;
            if (dw == null || _partyListRoot == null) return;

            for (int i = _partyListRoot.childCount - 1; i >= 0; i--)
                Destroy(_partyListRoot.GetChild(i).gameObject);

            Color hpCrimson = new Color(0.76f, 0.14f, 0.15f, 1f);
            Color mpSapphire = new Color(0.15f, 0.43f, 0.84f, 1f);

            int maxSlots = Mathf.Min(4, dw.PartyUnits.Count);
            for (int i = 0; i < maxSlots; i++)
            {
                var u = dw.PartyUnits[i];
                float top = 1f - i * 0.25f;
                var row = GameUIKit.MakeStyledPanel("Member" + i, _partyListRoot, UIPanelStyle.Character,
                    new Vector2(0f, top - 0.238f), new Vector2(1f, top - 0.008f), Vector2.zero, Vector2.zero,
                    new Color(0.96f, 0.90f, 0.84f, 0.96f), i == 0 ? GameUIKit.ColGold : GameUIKit.ColBronze);

                int cid = u != null && u.HeroClass != null ? u.HeroClass.id : 0;
                Image pImg = null;
                Sprite pIc = GameUIKit.GetClassIcon(cid);
                if (pIc != null)
                {
                    pImg = GameUIKit.MakeIcon("Portrait", row.transform, pIc,
                        new Vector2(0.02f, 0.10f), new Vector2(0.19f, 0.90f), Vector2.zero, Vector2.zero);
                }

                var nameLbl = GameUIKit.MakeText("Name", row.transform, "", 11, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                    new Vector2(0.21f, 0.62f), new Vector2(0.74f, 0.95f), Vector2.zero, Vector2.zero);

                var statusLbl = GameUIKit.MakeText("Status", row.transform, "", 10, FontStyle.Bold, new Color(0.72f, 0.92f, 0.72f), TextAnchor.MiddleRight,
                    new Vector2(0.70f, 0.62f), new Vector2(0.97f, 0.95f), Vector2.zero, Vector2.zero);

                var hpBar = GameUIKit.MakeBar("Hp", row.transform, hpCrimson,
                    new Vector2(0.21f, 0.34f), new Vector2(0.96f, 0.62f), Vector2.zero, Vector2.zero, 10);

                var mpBar = GameUIKit.MakeBar("Mp", row.transform, mpSapphire,
                    new Vector2(0.21f, 0.08f), new Vector2(0.96f, 0.30f), Vector2.zero, Vector2.zero, 9);
                mpBar.label.text = "";

                _partyWidgets.Add(new PartyMemberWidget
                {
                    portrait = pImg,
                    hpFill = hpBar.fill,
                    mpFill = mpBar.fill,
                    nameTxt = nameLbl,
                    statusTxt = statusLbl,
                    hpTxt = hpBar.label
                });
            }
        }

        private GameObject BuildPauseModal(Transform root)
        {
            var ov = GameUIKit.MakeRect("PauseModal", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var dim = ov.gameObject.AddComponent<Image>();
            dim.color = new Color(0.04f, 0.025f, 0.015f, 0.80f);

            var box = GameUIKit.MakeStyledPanel("Box", ov.transform, UIPanelStyle.Dialog,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-280f, -260f), new Vector2(280f, 260f));

            GameUIKit.MakeIcon("PauseCrest", box.transform, GameUIKit.GetIcon("crown"),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-22f, -54f), new Vector2(22f, -14f));

            GameUIKit.MakeText("Title", box.transform, "TẠM DỪNG CUỘC VIỄN CHINH", 24, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -92f), new Vector2(-16f, -52f));

            GameUIKit.MakeGothicDivider("PauseDiv", box.transform,
                new Vector2(0.08f, 1f), new Vector2(0.92f, 1f), new Vector2(0f, -106f), new Vector2(0f, -90f));

            GameUIKit.MakeStyledButton("Resume", box.transform, "TIẾP TỤC CHIẾN ĐẤU", 17, UIButtonStyle.Primary,
                new Vector2(0.10f, 0.66f), new Vector2(0.90f, 0.78f), Vector2.zero, Vector2.zero,
                TogglePauseModal, GameUIKit.GetIcon("crossed_swords"));

            GameUIKit.MakeStyledButton("Settings", box.transform, "CÀI ĐẶT ĐỒ HỌA & ÂM THANH", 16, UIButtonStyle.Secondary,
                new Vector2(0.10f, 0.50f), new Vector2(0.90f, 0.62f), Vector2.zero, Vector2.zero,
                () =>
                {
                    if (_settingsModal != null)
                    {
                        _settingsModal.transform.SetAsLastSibling();
                        _settingsModal.SetActive(true);
                    }
                }, GameUIKit.GetIcon("settings"));

            GameUIKit.MakeStyledButton("Town", box.transform, "RÚT VỀ THỊ TRẤN (AETHER HAVEN)", 16, UIButtonStyle.Secondary,
                new Vector2(0.10f, 0.34f), new Vector2(0.90f, 0.46f), Vector2.zero, Vector2.zero,
                () => GameSession.GoToScene("Town"), GameUIKit.GetIcon("anvil"));

            GameUIKit.MakeStyledButton("MainMenu", box.transform, "VỀ MÀN HÌNH CHÍNH (MAIN MENU)", 16, UIButtonStyle.Danger,
                new Vector2(0.10f, 0.18f), new Vector2(0.90f, 0.30f), Vector2.zero, Vector2.zero,
                () => GameSession.GoToScene("MainMenu"), GameUIKit.GetIcon("skull_boss"));

            GameUIKit.MakeText("DebugHint", box.transform,
                "Phím Kiểm Thử: F5 (+Level) | F6 (Rớt Đồ) | F7 (Đến Phòng Boss) | F8 (Dọn Phòng)",
                11, FontStyle.Italic, GameUIKit.ColMuted, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.15f), Vector2.zero, Vector2.zero);

            ov.gameObject.SetActive(false);
            return ov.gameObject;
        }

        private void ToggleCharacterModal()
        {
            if (_charModal == null) return;
            bool next = !_charModal.activeSelf;
            if (next && _skillsModal != null) _skillsModal.SetActive(false);
            if (next && _fullMapModal != null) _fullMapModal.SetActive(false);
            _charModal.SetActive(next);
            SyncCursorLockState();
        }

        private void ToggleSkillsModal()
        {
            if (_skillsModal == null) return;
            bool next = !_skillsModal.activeSelf;
            if (next && _charModal != null) _charModal.SetActive(false);
            if (next && _fullMapModal != null) _fullMapModal.SetActive(false);
            _skillsModal.SetActive(next);
            SyncCursorLockState();
        }

        private void ToggleFullMapModal()
        {
            if (_fullMapModal == null) return;
            bool next = !_fullMapModal.activeSelf;
            if (next && _charModal != null) _charModal.SetActive(false);
            if (next && _skillsModal != null) _skillsModal.SetActive(false);
            _fullMapModal.SetActive(next);
            SyncCursorLockState();
        }

        private void TogglePauseModal()
        {
            if (_pauseModal == null) return;
            if (_charModal != null && _charModal.activeSelf) { _charModal.SetActive(false); SyncCursorLockState(); return; }
            if (_skillsModal != null && _skillsModal.activeSelf) { _skillsModal.SetActive(false); SyncCursorLockState(); return; }
            if (_fullMapModal != null && _fullMapModal.activeSelf) { _fullMapModal.SetActive(false); SyncCursorLockState(); return; }
            bool next = !_pauseModal.activeSelf;
            _pauseModal.SetActive(next);
            Time.timeScale = next ? 0f : 1f;
            SyncCursorLockState();
        }

        private void SyncCursorLockState()
        {
            bool anyModal = (_charModal != null && _charModal.activeSelf)
                         || (_skillsModal != null && _skillsModal.activeSelf)
                         || (_fullMapModal != null && _fullMapModal.activeSelf)
                         || (_pauseModal != null && _pauseModal.activeSelf)
                         || (_settingsModal != null && _settingsModal.activeSelf);
            if (OrbitCamera3D.Instance != null)
                OrbitCamera3D.Instance.UIBlockingCursor = anyModal;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M)) ToggleFullMapModal();
            HandleLabTestShortcuts();

            var dw = DungeonWorldBuilder.Instance;
            if (dw == null) return;

            var prof = GameSession.I.Profile;
            ActorUnit hero = dw.LocalHero;
            float dt = Time.deltaTime;

            // =========================================================================
            // 1. PLAYER STATUS & SKILL HOTBAR UPDATES
            // =========================================================================
            if (hero != null)
            {
                string formTag = hero.InBearForm ? " [DẠNG GẤU]" : "";
                _heroMetaTxt.text = $"{hero.HeroClass.name.ToUpper()}{formTag} • Lv.{prof.level}";
                if (_levelBadgeTxt != null) _levelBadgeTxt.text = $"Lv.{prof.level}";
                if (_goldTxt != null) _goldTxt.text = $"{prof.gold:N0} G";

                GameUIKit.SetBarRatio(_hpFill, hero.Hp / Mathf.Max(1f, hero.MaxHp));
                GameUIKit.SetBarRatio(_shieldFill, hero.Shield / Mathf.Max(1f, hero.MaxHp));
                _hpTxt.text = hero.Shield > 1f
                    ? $"HP: {hero.Hp:0} / {hero.MaxHp:0} <color=#F8DC85>(+{hero.Shield:0} Khiên)</color>"
                    : $"HP: {hero.Hp:0} / {hero.MaxHp:0}";

                GameUIKit.SetBarRatio(_mpFill, hero.Mana / Mathf.Max(1f, hero.MaxMana));
                _mpTxt.text = $"MP: {hero.Mana:0} / {hero.MaxMana:0}";

                if (_staFill != null)
                {
                    GameUIKit.SetBarRatio(_staFill, hero.Stamina / Mathf.Max(1f, hero.MaxStamina));
                    string gait = hero.IsSprinting ? " [CHẠY NHANH]" : (hero.IsWalking ? " [ĐI BỘ]" : "");
                    _staTxt.text = $"Thể lực: {hero.Stamina:0} / {hero.MaxStamina:0}{gait}";
                }

                float reqExp = GameDB.ExpToNext(prof.level);
                GameUIKit.SetBarRatio(_expFill, prof.exp / Mathf.Max(1f, reqExp));
                int totalPts = prof.statPoints + prof.skillPoints;
                _expTxt.text = $"EXP: {prof.exp:0} / {reqExp:0}" + (totalPts > 0 ? $"  <color=#F8DC85>(+{totalPts} Điểm [I/K])</color>" : "");

                // Active Statuses + Lock-On Target HUD Indicator
                var parts = new List<string>();
                var lockSys = CameraTargetLockSystem.Instance;
                if (lockSys != null && lockSys.IsLocked && lockSys.LockedTarget != null)
                {
                    ActorUnit lt = lockSys.LockedTarget;
                    parts.Add($"[MỤC TIÊU: {lt.DisplayName} ({lt.Hp:0}/{lt.MaxHp:0})]");
                }
                foreach (var st in hero.Statuses) parts.Add($"{GameDB.StatusName(st.type)} ({st.remaining:0.0}s)");
                _statusTxt.text = parts.Count > 0 ? string.Join("  •  ", parts) : "";

                // Weapon Basic & Heavy Cooldowns (Radial 360)
                if (_basicWpnComboTxt != null) _basicWpnComboTxt.text = $"Đòn {(hero.ComboStep % 3) + 1}";
                if (_basicWpnCdOverlay != null)
                {
                    bool bCd = hero.BasicAttackCd > 0.08f;
                    _basicWpnCdOverlay.gameObject.SetActive(bCd);
                    if (bCd)
                    {
                        _basicWpnCdOverlay.fillAmount = Mathf.Clamp01(hero.BasicAttackCd / 0.85f);
                        if (_basicWpnCdTxt != null) _basicWpnCdTxt.text = $"{hero.BasicAttackCd:0.0}";
                    }
                }
                if (_heavyWpnCdOverlay != null)
                {
                    bool hCd = hero.HeavyAttackCd > 0.05f;
                    _heavyWpnCdOverlay.gameObject.SetActive(hCd);
                    if (hCd)
                    {
                        _heavyWpnCdOverlay.fillAmount = Mathf.Clamp01(hero.HeavyAttackCd / 2.2f);
                        if (_heavyWpnCdTxt != null) _heavyWpnCdTxt.text = $"{hero.HeavyAttackCd:0.0}s";
                    }
                    if (_heavyOomOverlay != null)
                        _heavyOomOverlay.gameObject.SetActive(!hCd && hero.Stamina < 20f);
                }

                // Class Skills 0..3: Cooldown Radial Sweep + Insufficient Mana State + Skill Ready Pulse
                for (int s = 0; s < 4; s++)
                {
                    SkillDef sk = hero.HeroClass.skills[s];
                    int rnk = ClassSpecializationSystem.GetSkillRank(hero, s);
                    if (_skillRankTexts[s] != null) _skillRankTexts[s].text = $"Lv.{rnk}";

                    float cd = hero.SkillCds[s];
                    bool onCd = cd > 0.05f;
                    bool lowMana = hero.Mana < sk.mana;
                    bool isUlt = sk.ultimate || s == 3;

                    // Trigger Ready-Pulse flash on transition from Cooldown -> Ready!
                    if (_skillWasOnCd[s] && !onCd)
                        _skillReadyFlash[s] = 0.85f;
                    _skillWasOnCd[s] = onCd;
                    if (_skillReadyFlash[s] > 0f) _skillReadyFlash[s] = Mathf.Max(0f, _skillReadyFlash[s] - dt);

                    _skillCdOverlays[s].gameObject.SetActive(onCd);
                    if (onCd)
                    {
                        float maxCd = Mathf.Max(0.5f, sk.cooldown * (1f - hero.CooldownReduction));
                        _skillCdOverlays[s].fillAmount = Mathf.Clamp01(cd / maxCd);
                        _skillCdTexts[s].text = $"{cd:0.0}s";
                    }

                    // Insufficient Mana visual state
                    if (_skillOomOverlays[s] != null)
                        _skillOomOverlays[s].gameObject.SetActive(!onCd && lowMana);
                    if (_skillIcons[s] != null)
                        _skillIcons[s].color = (!onCd && lowMana) ? new Color(0.42f, 0.48f, 0.68f, 0.85f) : new Color(1f, 1f, 1f, 0.96f);
                    if (_skillCostTexts[s] != null)
                        _skillCostTexts[s].color = lowMana ? new Color(1f, 0.42f, 0.46f) : new Color(0.54f, 0.84f, 1f);

                    // Skill Ready shimmer (flash when ready + subtle breathing gold rim on ready Ultimate)
                    if (_skillReadyGlows[s] != null)
                    {
                        float flashA = _skillReadyFlash[s] > 0f ? Mathf.Sin((_skillReadyFlash[s] / 0.85f) * Mathf.PI) * 0.42f : 0f;
                        float ultBreath = (isUlt && !onCd && !lowMana) ? (0.06f + 0.06f * Mathf.Sin(Time.unscaledTime * 3.4f)) : 0f;
                        float glowA = Mathf.Max(flashA, ultBreath);
                        _skillReadyGlows[s].gameObject.SetActive(glowA > 0.005f);
                        _skillReadyGlows[s].color = new Color(0.98f, 0.86f, 0.48f, glowA);
                    }
                }

                // Dodge [Space] Radial Cooldown
                if (_dodgeCdOverlay != null)
                {
                    bool dCd = hero.DodgeCd > 0.05f;
                    _dodgeCdOverlay.gameObject.SetActive(dCd);
                    if (dCd)
                    {
                        _dodgeCdOverlay.fillAmount = Mathf.Clamp01(hero.DodgeCd / 1.4f);
                        _dodgeCdTxt.text = $"{hero.DodgeCd:0.0}s";
                    }
                }

                // Potion [H] Count & Radial Cooldown
                var inp = hero.GetComponent<HeroPlayerInput>();
                float potCd = inp != null ? inp.PotionCooldown : 0f;
                if (_potionCountTxt != null)
                {
                    _potionCountTxt.text = $"x{prof.potions}";
                    _potionCountTxt.color = prof.potions > 0 ? GameUIKit.ColGoldBright : GameUIKit.ColMuted;
                }
                if (_potionCdOverlay != null)
                {
                    bool pCd = potCd > 0.05f;
                    _potionCdOverlay.gameObject.SetActive(pCd);
                    if (pCd)
                    {
                        _potionCdOverlay.fillAmount = Mathf.Clamp01(potCd / 6.0f);
                        _potionCdTxt.text = $"{potCd:0.0}s";
                    }
                }

                // Interact prompt
                if (inp != null && inp.NearestInteractable != null)
                {
                    _interactBox.SetActive(true);
                    _interactTxt.text = inp.NearestInteractable.GetPrompt();
                }
                else _interactBox.SetActive(false);
            }

            // =========================================================================
            // 2. PARTY PANEL UPDATES (4 Slots: HP, MP, Level, Alive/Downed/Dead, Party Tag)
            // =========================================================================
            int partyTarget = Mathf.Min(4, dw.PartyUnits.Count);
            if (_partyWidgets.Count != partyTarget) RebuildPartyWidgets();

            int aliveCount = 0;
            for (int i = 0; i < partyTarget && i < _partyWidgets.Count; i++)
            {
                var u = dw.PartyUnits[i];
                if (u == null) continue;
                var w = _partyWidgets[i];
                if (!u.IsDead) aliveCount++;

                GameUIKit.SetBarRatio(w.hpFill, u.Hp / Mathf.Max(1f, u.MaxHp));
                GameUIKit.SetBarRatio(w.mpFill, u.Mana / Mathf.Max(1f, u.MaxMana));

                w.nameTxt.text = $"{u.DisplayName} <size=10><color=#C9B89A>{u.HeroClass.name} Lv.{u.Level}</color></size>";
                string roleTag = u.IsLocalPlayer ? "Đội trưởng" : (LanRoomBridge.IsLanActive ? "Co-op" : "AI");
                if (u.IsDead)
                {
                    w.statusTxt.text = "<color=#F05858>TỬ TRẬN</color>";
                    w.hpTxt.text = "0 / " + u.MaxHp.ToString("0");
                    if (w.portrait != null) w.portrait.color = new Color(0.45f, 0.38f, 0.38f, 0.72f);
                }
                else if (u.IsDowned)
                {
                    w.statusTxt.text = "<color=#F8DC85>GỤC • CỨU!</color>";
                    w.hpTxt.text = "GỤC NGÃ";
                    if (w.portrait != null) w.portrait.color = new Color(0.88f, 0.65f, 0.55f, 0.90f);
                }
                else
                {
                    w.statusTxt.text = $"<color=#96D696>{roleTag}</color>";
                    w.hpTxt.text = $"{u.Hp:0}/{u.MaxHp:0}  •  <color=#8CC6FF>{u.Mana:0}MP</color>";
                    if (w.portrait != null) w.portrait.color = Color.white;
                }
            }
            if (_partyStatusTagTxt != null)
            {
                string mode = LanRoomBridge.IsLanActive ? "LAN CO-OP" : "TỔ ĐỘI";
                _partyStatusTagTxt.text = $"{mode} {aliveCount}/{partyTarget}";
            }

            // =========================================================================
            // 3. ANIMATED QUEST NOTIFICATION & EVENT BANNER
            // =========================================================================
            if (_lastObjectiveStr != dw.CurrentObjective)
            {
                _lastObjectiveStr = dw.CurrentObjective;
                _objectiveTxt.text = dw.CurrentObjective;
                _questAnimTimer = 0.55f;
                UpdateQuestRoomIcon(dw);
            }
            if (_questAnimTimer > 0f && _questRect != null && _questCg != null)
            {
                _questAnimTimer = Mathf.Max(0f, _questAnimTimer - Time.unscaledDeltaTime);
                float p = 1f - (_questAnimTimer / 0.55f);
                float ease = 1f - Mathf.Pow(1f - p, 3f);
                _questCg.alpha = Mathf.Lerp(0.35f, 1f, ease);
                float ySlide = Mathf.Lerp(14f, 0f, ease);
                _questRect.offsetMin = new Vector2(-390f, -92f + ySlide);
                _questRect.offsetMax = new Vector2(390f, -12f + ySlide);
            }

            if (AbyssDirectorAI.Instance != null && !string.IsNullOrEmpty(AbyssDirectorAI.Instance.LastDecision))
                _directorTxt.text = AbyssDirectorAI.Instance.LastDecision;

            // Smooth fade-in / fade-out Banner Notification
            if (dw.BannerTimer > 0f && !string.IsNullOrEmpty(dw.BannerMessage))
            {
                if (!_bannerBox.activeSelf) _bannerBox.SetActive(true);
                _bannerTxt.text = dw.BannerMessage;
                if (_bannerCg != null)
                {
                    float fadeOut = Mathf.Clamp01(dw.BannerTimer / 0.45f);
                    _bannerCg.alpha = Mathf.MoveTowards(_bannerCg.alpha, fadeOut, Time.unscaledDeltaTime * 4.5f);
                }
            }
            else if (_bannerBox.activeSelf)
            {
                if (_bannerCg != null && _bannerCg.alpha > 0.02f)
                    _bannerCg.alpha = Mathf.MoveTowards(_bannerCg.alpha, 0f, Time.unscaledDeltaTime * 4.0f);
                else
                    _bannerBox.SetActive(false);
            }

            // Loot feed
            if (_lootLogTimer > 0f && _lootLogs.Count > 0)
            {
                _lootLogTimer -= dt;
                _lootBox.SetActive(true);
                _lootFeedTxt.text = "<color=#D9B057>CHIẾN LỢI PHẨM THU ĐƯỢC:</color>\n" + string.Join("\n", _lootLogs);
            }
            else if (_lootBox != null)
            {
                _lootBox.SetActive(false);
            }

            // Boss 3-Phase Health Bar
            var boss = AbyssalBossAI.ActiveBoss;
            if (boss != null && boss.GetComponent<ActorUnit>() is ActorUnit bu && !bu.IsDead)
            {
                _bossPanel.SetActive(true);
                _bossTitleTxt.text = $"ABYSSAL KNIGHT — PHASE {boss.Phase}/3 ({bu.Hp:0} / {bu.MaxHp:0})";
                _bossSkillTxt.text = boss.CurrentSkillAnnouncement;
                GameUIKit.SetBarRatio(_bossHpFill, bu.Hp / Mathf.Max(1f, bu.MaxHp));
            }
            else _bossPanel.SetActive(false);

            // Minimap refresh at 12 Hz
            _mapRefreshTimer -= dt;
            if (_mapRefreshTimer <= 0f)
            {
                _mapRefreshTimer = 0.08f;
                RenderMinimap(dw);
            }
        }

        private void UpdateQuestRoomIcon(DungeonWorldBuilder dw)
        {
            if (_questIconImg == null || dw == null || dw.CurrentRoomIndex < 0 || dw.CurrentRoomIndex >= dw.Rooms.Count) return;
            var rm = dw.Rooms[dw.CurrentRoomIndex];
            string key = rm.type == RoomType.Boss ? "skull_boss"
                       : (rm.type == RoomType.Treasure ? "gold"
                       : (rm.type == RoomType.Puzzle ? "portal"
                       : (rm.type == RoomType.Entrance ? "crown" : "crossed_swords")));
            Sprite sp = GameUIKit.GetIcon(key);
            if (sp != null) _questIconImg.sprite = sp;
        }

        private void RenderMinimap(DungeonWorldBuilder dw)
        {
            if (_mapTex == null || dw.LocalHero == null) return;

            // Ancient Gothic Obsidian-Umber Radar Background
            Color32 bg = new Color32(14, 11, 9, 255);
            for (int i = 0; i < _mapPixels.Length; i++) _mapPixels[i] = bg;

            Vector3 focus = dw.LocalHero.transform.position;
            const float worldSpan = 108f;

            int WorldToPxX(float wx) => Mathf.RoundToInt(((wx - focus.x) / worldSpan + 0.5f) * (MapSize - 1));
            int WorldToPxY(float wz) => Mathf.RoundToInt(((wz - focus.z) / worldSpan + 0.5f) * (MapSize - 1));

            // 1. Draw Corridors connecting adjacent rooms (both Main North-South and Side East-West!)
            Color32 corExplored = new Color32(52, 42, 32, 255);
            Color32 corUnexplored = new Color32(28, 23, 19, 255);
            for (int i = 0; i < dw.Rooms.Count; i++)
            {
                var r = dw.Rooms[i];
                if (r.exitGate != null)
                {
                    Vector3 gp = r.exitGate.transform.position;
                    DrawRectPx(WorldToPxX(gp.x - 3f), WorldToPxY(gp.z), WorldToPxX(gp.x + 3f), WorldToPxY(gp.z + 8f),
                        r.discovered ? corExplored : corUnexplored, new Color32(95, 74, 42, 255));
                }
                if (r.sideGate != null)
                {
                    Vector3 sp = r.sideGate.transform.position;
                    DrawRectPx(WorldToPxX(sp.x), WorldToPxY(sp.z - 3f), WorldToPxX(sp.x + 8f), WorldToPxY(sp.z + 3f),
                        r.discovered ? corExplored : corUnexplored, new Color32(115, 92, 52, 255));
                }
            }

            // 2. Draw Rooms with Explored vs Unexplored Fog-of-War & Active Highlight
            Color32 unexploredFill = new Color32(24, 20, 18, 255);
            Color32 unexploredEdge = new Color32(72, 58, 38, 255);
            Color32 exploredFill   = new Color32(58, 45, 33, 255);
            Color32 clearedFill    = new Color32(42, 60, 42, 255);
            Color32 activeFill     = new Color32(74, 58, 38, 255);
            Color32 treasureFill   = new Color32(76, 62, 28, 255);
            Color32 bossFill       = new Color32(68, 26, 32, 255);
            Color32 goldEdge       = new Color32(204, 162, 82, 255);
            Color32 bronzeEdge     = new Color32(138, 102, 52, 255);

            for (int i = 0; i < dw.Rooms.Count; i++)
            {
                var r = dw.Rooms[i];
                bool isCurrent = (i == dw.CurrentRoomIndex);
                bool explored = r.discovered || r.activated || r.cleared || isCurrent;

                int x0 = WorldToPxX(r.center.x - r.width * 0.5f);
                int x1 = WorldToPxX(r.center.x + r.width * 0.5f);
                int y0 = WorldToPxY(r.center.z - r.length * 0.5f);
                int y1 = WorldToPxY(r.center.z + r.length * 0.5f);

                Color32 fill = !explored ? unexploredFill
                    : (isCurrent ? activeFill
                    : (r.type == RoomType.Boss ? bossFill
                    : (r.type == RoomType.Treasure ? treasureFill
                    : (r.cleared ? clearedFill : exploredFill))));

                Color32 edge = !explored ? unexploredEdge : (isCurrent ? goldEdge : bronzeEdge);
                DrawRectPx(x0, y0, x1, y1, fill, edge, dashedBorder: !explored);

                // Room Landmark Markers (Boss, Treasure, Puzzle Objective)
                int cx = WorldToPxX(r.center.x);
                int cy = WorldToPxY(r.center.z);
                if (r.type == RoomType.Boss)
                    StampDiamond(cx, cy, 4, new Color32(235, 72, 92, 255));
                else if (r.type == RoomType.Treasure && !r.cleared)
                    StampDiamond(cx, cy, 3, new Color32(250, 210, 82, 255));
                else if (!r.cleared && isCurrent)
                    StampRing(cx, cy, 5, new Color32(246, 214, 118, 255));

                // Draw Doors / Gates (Red = Locked, Gold = Open)
                DrawGateMarker(r.entryGate, WorldToPxX, WorldToPxY, horizontal: true);
                DrawGateMarker(r.exitGate, WorldToPxX, WorldToPxY, horizontal: true);
                DrawGateMarker(r.sideGate, WorldToPxX, WorldToPxY, horizontal: false);
            }

            // 3. Draw Units (Allies, Monsters, Boss, and Directional Player Arrow)
            foreach (var u in ActorUnit.AllUnits)
            {
                if (u == null || u.IsDead) continue;
                int px = WorldToPxX(u.transform.position.x);
                int py = WorldToPxY(u.transform.position.z);
                if (u.IsLocalPlayer)
                {
                    StampPlayerArrow(px, py, u.transform.forward, new Color32(255, 226, 112, 255));
                }
                else
                {
                    Color32 dc = (u.Team == Team.Hero) ? new Color32(92, 218, 128, 255)
                               : (u.IsBoss ? new Color32(255, 96, 56, 255) : new Color32(228, 60, 54, 255));
                    StampDot(px, py, u.IsBoss ? 4 : 2, dc);
                }
            }

            _mapTex.SetPixels32(_mapPixels);
            _mapTex.Apply(false);
        }

        private void DrawGateMarker(DungeonGate gate, System.Func<float, int> toX, System.Func<float, int> toY, bool horizontal)
        {
            if (gate == null) return;
            int gx = toX(gate.transform.position.x);
            int gy = toY(gate.transform.position.z);
            Color32 col = gate.IsOpen ? new Color32(218, 178, 86, 255) : new Color32(235, 58, 52, 255);
            if (horizontal) DrawRectPx(gx - 4, gy - 1, gx + 4, gy + 1, col, col);
            else DrawRectPx(gx - 1, gy - 4, gx + 1, gy + 4, col, col);
        }

        private void DrawRectPx(int x0, int y0, int x1, int y1, Color32 fill, Color32 border, bool dashedBorder = false)
        {
            if (x1 < 0 || y1 < 0 || x0 >= MapSize || y0 >= MapSize) return;
            int cx0 = Mathf.Clamp(x0, 0, MapSize - 1);
            int cx1 = Mathf.Clamp(x1, 0, MapSize - 1);
            int cy0 = Mathf.Clamp(y0, 0, MapSize - 1);
            int cy1 = Mathf.Clamp(y1, 0, MapSize - 1);

            for (int y = cy0; y <= cy1; y++)
            {
                for (int x = cx0; x <= cx1; x++)
                {
                    bool isEdge = (x == x0 || x == x1 || y == y0 || y == y1);
                    if (isEdge)
                    {
                        if (!dashedBorder || ((x + y) & 2) == 0)
                            _mapPixels[y * MapSize + x] = border;
                    }
                    else
                    {
                        _mapPixels[y * MapSize + x] = fill;
                    }
                }
            }
        }

        private void StampDiamond(int cx, int cy, int r, Color32 col)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                int y = cy + dy;
                if (y < 0 || y >= MapSize) continue;
                int span = r - Mathf.Abs(dy);
                for (int dx = -span; dx <= span; dx++)
                {
                    int x = cx + dx;
                    if (x >= 0 && x < MapSize) _mapPixels[y * MapSize + x] = col;
                }
            }
        }

        private void StampRing(int cx, int cy, int r, Color32 col)
        {
            int r2Min = (r - 1) * (r - 1);
            int r2Max = r * r;
            for (int dy = -r; dy <= r; dy++)
            {
                int y = cy + dy;
                if (y < 0 || y >= MapSize) continue;
                for (int dx = -r; dx <= r; dx++)
                {
                    int d2 = dx * dx + dy * dy;
                    if (d2 >= r2Min && d2 <= r2Max)
                    {
                        int x = cx + dx;
                        if (x >= 0 && x < MapSize) _mapPixels[y * MapSize + x] = col;
                    }
                }
            }
        }

        private void StampPlayerArrow(int cx, int cy, Vector3 fwd, Color32 col)
        {
            StampDot(cx, cy, 3, col);
            Vector2 dir = new Vector2(fwd.x, fwd.z);
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
            dir.Normalize();
            for (int s = 3; s <= 6; s++)
            {
                int tx = cx + Mathf.RoundToInt(dir.x * s);
                int ty = cy + Mathf.RoundToInt(dir.y * s);
                StampDot(tx, ty, s >= 5 ? 1 : 2, new Color32(255, 248, 200, 255));
            }
        }

        private void StampDot(int cx, int cy, int r, Color32 col)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                int y = cy + dy;
                if (y < 0 || y >= MapSize) continue;
                for (int dx = -r; dx <= r; dx++)
                {
                    int x = cx + dx;
                    if (x < 0 || x >= MapSize) continue;
                    _mapPixels[y * MapSize + x] = col;
                }
            }
        }

        private void HandleLabTestShortcuts()
        {
            var dw = DungeonWorldBuilder.Instance;
            if (dw == null || dw.LocalHero == null) return;

            if (Input.GetKeyDown(KeyCode.F5))
                dw.AwardExpAndGold(Mathf.RoundToInt(GameDB.ExpToNext(GameSession.I.Profile.level)), 100);

            if (Input.GetKeyDown(KeyCode.F6))
                LootOrbPickup.Spawn(dw.LocalHero.transform.position + dw.LocalHero.transform.forward * 2f, LootRoller.RollItem(GameSession.I.Profile.selectedFloor, 4, 1f));

            if (Input.GetKeyDown(KeyCode.F7) && dw.Rooms.Count > 0)
            {
                var bossRoom = dw.Rooms.Find(r => r.type == RoomType.Boss) ?? dw.Rooms[dw.Rooms.Count - 1];
                Vector3 bossEntry = bossRoom.center + new Vector3(0f, 0f, -bossRoom.length * 0.35f);
                foreach (var h in dw.PartyUnits)
                {
                    if (h == null) continue;
                    if (h.Controller != null) h.Controller.enabled = false;
                    h.transform.position = bossEntry + new Vector3(Random.Range(-2f, 2f), 0f, 0f);
                    if (h.Controller != null) h.Controller.enabled = true;
                }
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                for (int i = ActorUnit.AllUnits.Count - 1; i >= 0; i--)
                    if (ActorUnit.AllUnits[i].Team == Team.Monster && !ActorUnit.AllUnits[i].IsDead)
                        ActorUnit.AllUnits[i].ForceKill();
            }
        }
    }

    /// <summary>
    /// Section 4.7 End Game Summary Scene (Screen H: Victory / Defeat Screen):
    /// - Victory uses Antique Gold & Warm Bronze (UI_Panel_Ornate)
    /// - Defeat uses Dark Bronze & Blood Red (UI_Panel_Dialog)
    /// - Displays Dungeon Rewards (EXP, Gold, Boss Status), Combat Statistics, Items Obtained with Icons, and 4 Navigation Buttons
    /// </summary>
    public class EndGameSceneView : MonoBehaviour
    {
        public void Build()
        {
            RunReport rep = GameSession.I.LastRun ?? new RunReport();
            GameAudio.I.PlayMusic(rep.victory ? GameMusic.Victory : GameMusic.Defeat);
            Build3DPodium(rep.victory);

            var canvas = GameUIKit.CreateCanvas("EndGameCanvas");
            var root = canvas.transform;

            Color themeBorder = rep.victory ? GameUIKit.ColGoldBright : GameUIKit.ColDanger;
            Color themeTint   = rep.victory ? new Color(1f, 0.98f, 0.92f, 0.98f) : new Color(0.95f, 0.82f, 0.80f, 0.98f);

            var panel = GameUIKit.MakeStyledPanel("SummaryCard", root,
                rep.victory ? UIPanelStyle.Ornate : UIPanelStyle.Dialog,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-640f, -410f), new Vector2(640f, 410f),
                themeTint, themeBorder);

            // Top Crest & Headline
            GameUIKit.MakeIcon("ResultCrest", panel.transform,
                GameUIKit.GetIcon(rep.victory ? "crown" : "skull_boss"),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-28f, -66f), new Vector2(28f, -12f));

            string headline = rep.victory
                ? $"VICTORY — KHẢI HOÀN CHINH PHỤC TẦNG {rep.floor}!"
                : $"DEFEAT — NGÃ XUỐNG TẠI HẦM NGỤC TẦNG {rep.floor}";
            GameUIKit.MakeText("Headline", panel.transform, headline, 30, FontStyle.Bold,
                rep.victory ? GameUIKit.ColGoldBright : new Color(0.95f, 0.32f, 0.28f), TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -108f), new Vector2(-24f, -62f));

            GameUIKit.MakeGothicDivider("TopDiv", panel.transform,
                new Vector2(0.04f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, -122f), new Vector2(0f, -104f));

            // 3 Top Reward Plaques (EXP Gained, Gold Gained, Boss Status)
            var expBadge = GameUIKit.MakeStyledPanel("ExpReward", panel.transform, UIPanelStyle.Notification,
                new Vector2(0.03f, 0.74f), new Vector2(0.33f, 0.84f), Vector2.zero, Vector2.zero);
            GameUIKit.MakeIcon("Ic", expBadge.transform, GameUIKit.GetIcon("exp"),
                new Vector2(0.04f, 0.14f), new Vector2(0.22f, 0.86f), Vector2.zero, Vector2.zero);
            GameUIKit.MakeText("Txt", expBadge.transform,
                $"KINH NGHIỆM ĐẠT ĐƯỢC\n<color=#F8DC85>+{rep.expGained} EXP</color> (Lv.{rep.startLevel} → Lv.{rep.endLevel})",
                13, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.MiddleLeft,
                new Vector2(0.25f, 0.08f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);

            var goldBadge = GameUIKit.MakeStyledPanel("GoldReward", panel.transform, UIPanelStyle.Notification,
                new Vector2(0.35f, 0.74f), new Vector2(0.65f, 0.84f), Vector2.zero, Vector2.zero);
            GameUIKit.MakeIcon("Ic", goldBadge.transform, GameUIKit.GetIcon("gold"),
                new Vector2(0.04f, 0.14f), new Vector2(0.22f, 0.86f), Vector2.zero, Vector2.zero);
            GameUIKit.MakeText("Txt", goldBadge.transform,
                $"VÀNG CHIẾN LỢI PHẨM\n<color=#F8DC85>+{rep.goldGained} Gold</color> (Tổng: {GameSession.I.Profile.gold} G)",
                13, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.MiddleLeft,
                new Vector2(0.25f, 0.08f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);

            var bossBadge = GameUIKit.MakeStyledPanel("BossReward", panel.transform, UIPanelStyle.Notification,
                new Vector2(0.67f, 0.74f), new Vector2(0.97f, 0.84f), Vector2.zero, Vector2.zero);
            GameUIKit.MakeIcon("Ic", bossBadge.transform, GameUIKit.GetIcon("skull_boss"),
                new Vector2(0.04f, 0.14f), new Vector2(0.22f, 0.86f), Vector2.zero, Vector2.zero);
            string bossStatus = rep.victory ? "<color=#68E878>ĐÃ TIÊU DIỆT CHúa TỂ</color>" : "<color=#F06868>CHƯA HẠ GỤC BOSS</color>";
            GameUIKit.MakeText("Txt", bossBadge.transform,
                $"CHÚA TỂ HẦM NGỤC (BOSS)\n{bossStatus}",
                13, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.MiddleLeft,
                new Vector2(0.25f, 0.08f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);

            int mins = Mathf.FloorToInt(rep.elapsedTime / 60f);
            int secs = Mathf.FloorToInt(rep.elapsedTime % 60f);

            // Left: Combat & Director Statistics (UI_Panel_Character)
            var left = GameUIKit.MakeStyledPanel("StatsBox", panel.transform, UIPanelStyle.Character,
                new Vector2(0.03f, 0.17f), new Vector2(0.49f, 0.72f), Vector2.zero, Vector2.zero);

            string statsBody =
                $"<color=#F8DC85>BIÊN NIÊN SỬ CHIẾN ĐẤU:</color>\n\n" +
                $"• Thời gian hoàn thành: <color=#F8DC85>{mins:00}:{secs:00}</color>\n" +
                $"• Số khu vực vượt qua: <color=#F8DC85>{rep.roomsCleared} / {rep.totalRooms}</color>\n" +
                $"• Quái vật tiêu diệt: <color=#F8DC85>{rep.monstersKilled}</color>\n" +
                $"• Tổng sát thương gây ra: <color=#F8DC85>{rep.damageDealt:0}</color>\n" +
                $"• Tổng sát thương gánh chịu: <color=#EDE0BA>{rep.damageTaken:0}</color>\n" +
                $"• Tổng lượng hồi phục: <color=#8EE89E>{rep.healingDone:0}</color>\n" +
                $"• Phán quyết AI Abyss Director: <color=#D9B057>{rep.directorVerdict}</color>";

            GameUIKit.MakeText("StatsTxt", left.transform, statsBody, 15, FontStyle.Normal, GameUIKit.ColParchment, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(22f, 18f), new Vector2(-22f, -18f));

            // Right: Looted Items Obtained (UI_Panel_Inventory)
            var right = GameUIKit.MakeStyledPanel("LootBox", panel.transform, UIPanelStyle.Inventory,
                new Vector2(0.51f, 0.17f), new Vector2(0.97f, 0.72f), Vector2.zero, Vector2.zero);

            GameUIKit.MakeIcon("LootIc", right.transform, GameUIKit.GetIcon("bag"),
                new Vector2(0.03f, 0.86f), new Vector2(0.11f, 0.97f), Vector2.zero, Vector2.zero);

            GameUIKit.MakeText("LootTitle", right.transform,
                $"DI VẬT & TRANG BỊ THU ĐƯỢC ({rep.lootedItems.Count}):",
                16, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0.12f, 0.85f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

            if (rep.lootedItems.Count == 0)
            {
                GameUIKit.MakeText("NoLoot", right.transform,
                    "Chưa thu thập được di vật nào trong lượt viễn chinh này.",
                    15, FontStyle.Italic, GameUIKit.ColMuted, TextAnchor.MiddleCenter,
                    new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.82f), Vector2.zero, Vector2.zero);
            }
            else
            {
                int show = Mathf.Min(6, rep.lootedItems.Count);
                for (int i = 0; i < show; i++)
                {
                    EquipItem it = rep.lootedItems[i];
                    float top = 0.83f - i * 0.132f;
                    Color rc = GameDB.RarityColors[(int)it.rarity];
                    var row = GameUIKit.MakeStyledPanel("LootRow" + i, right.transform, UIPanelStyle.Standard,
                        new Vector2(0.03f, top - 0.118f), new Vector2(0.97f, top), Vector2.zero, Vector2.zero, null, rc);

                    GameUIKit.MakeIcon("ItIc", row.transform, GameUIKit.GetSlotIcon(it.slot),
                        new Vector2(0.02f, 0.12f), new Vector2(0.11f, 0.88f), Vector2.zero, Vector2.zero);

                    GameUIKit.MakeText("Txt", row.transform,
                        $"[{GameDB.RarityNames[(int)it.rarity]}] {it.name} — <color=#EDE0BA>{it.Summary()}</color>",
                        13, FontStyle.Bold, rc, TextAnchor.MiddleLeft,
                        new Vector2(0.13f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero);
                }
            }

            // 4 Bottom Gothic Action Buttons
            GameUIKit.MakeStyledButton("BtnTown", panel.transform, "VỀ THỊ TRẤN (TOWN)", 16, UIButtonStyle.Secondary,
                new Vector2(0.03f, 0.035f), new Vector2(0.26f, 0.135f), Vector2.zero, Vector2.zero,
                () => GameSession.GoToScene("Town"), GameUIKit.GetIcon("anvil"));

            string replayLabel = rep.victory ? $"TIẾP TỤC TẦNG {rep.floor + 1}" : "CHƠI LẠI PHÓ BẢN";
            GameUIKit.MakeStyledButton("BtnReplay", panel.transform, replayLabel, 16, UIButtonStyle.Primary,
                new Vector2(0.275f, 0.035f), new Vector2(0.535f, 0.135f), Vector2.zero, Vector2.zero, () =>
                {
                    if (rep.victory)
                    {
                        GameSession.I.Profile.selectedFloor = rep.floor + 1;
                        GameSession.I.SaveProfile();
                    }
                    GameSession.GoToScene("Dungeon");
                }, GameUIKit.GetIcon("crossed_swords"));

            GameUIKit.MakeStyledButton("BtnMenu", panel.transform, "MAIN MENU", 16, UIButtonStyle.Secondary,
                new Vector2(0.55f, 0.035f), new Vector2(0.76f, 0.135f), Vector2.zero, Vector2.zero,
                () => GameSession.GoToScene("MainMenu"), GameUIKit.GetIcon("crown"));

            GameUIKit.MakeStyledButton("BtnQuit", panel.transform, "THOÁT GAME", 16, UIButtonStyle.Danger,
                new Vector2(0.775f, 0.035f), new Vector2(0.97f, 0.135f), Vector2.zero, Vector2.zero, () =>
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                }, GameUIKit.GetIcon("skull_boss"));
        }

        private void Build3DPodium(bool victory)
        {
            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = victory ? new Color(0.08f, 0.055f, 0.03f) : new Color(0.09f, 0.025f, 0.025f);
                Camera.main.transform.position = new Vector3(0f, 2.2f, -6.5f);
                Camera.main.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
            }
            var l = new GameObject("EndLight").AddComponent<Light>();
            l.transform.SetParent(transform, false);
            l.transform.position = new Vector3(0f, 4f, -3f);
            l.type = LightType.Point;
            l.range = 16f;
            l.intensity = 2.4f;
            l.color = victory ? new Color(1f, 0.86f, 0.52f) : new Color(0.92f, 0.28f, 0.24f);
        }
    }

    /// <summary>
    /// 3D Town Hub ("Aether Haven") with Ancient Gothic HUD & Interactive Forging/Alchemy Stations.
    /// </summary>
    public class TownSceneBuilder : MonoBehaviour
    {
        private ActorUnit _hero;
        private Text _topInfoTxt;
        private Text _interactPrompt;
        private GameObject _interactBox;
        private GameObject _charModal;
        private GameObject _toastBox;
        private Text _toastTxt;
        private float _toastTimer;

        public void Build()
        {
            HeroPlayerInput.Interactables.Clear();
            AbyssalPostProcessing.EnsureGlobalVolumeAndCameraSetup(transform);
            var atmo = BiomeAtmosphereController.EnsureOnScene(transform);
            atmo.ApplyBiome(DungeonBiome.ForgottenTemple, instant: true);
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
            CreateTownStation(new Vector3(-8f, 0f, 3f), "Thợ Rèn Cổ Ngữ", new Color(1f, 0.68f, 0.24f),
                "[F] Rèn Trang Bị Cổ Ngữ Mới (80 Gold)", () =>
                {
                    var p = GameSession.I.Profile;
                    if (p.gold < 80) { ShowToast("Không đủ Vàng trong kho (cần 80 Gold)!"); return; }
                    p.gold -= 80;
                    EquipItem item = LootRoller.RollItem(p.highestFloor, 2, 0.45f);
                    GameSession.I.AddItemToBag(item, autoEquipIfBetter: true);
                    _hero.RecalculateStats(true);
                    GameAudio.I.PlaySfx(GameSfx.Loot, 1f);
                    ShowToast($"Đã rèn thành công: [{GameDB.RarityNames[(int)item.rarity]}] {item.name} ({item.Summary()})!");
                });

            // Alchemist Station (East)
            CreateTownStation(new Vector3(8f, 0f, 3f), "Giả Kim Thuật Sư", new Color(0.38f, 0.88f, 0.48f),
                "[F] Điều Chế 2 Bình Hồi Máu (25 Gold)", () =>
                {
                    var p = GameSession.I.Profile;
                    if (p.gold < 25) { ShowToast("Không đủ Vàng trong kho (cần 25 Gold)!"); return; }
                    p.gold -= 25;
                    p.potions += 2;
                    GameSession.I.SaveProfile();
                    GameAudio.I.PlaySfx(GameSfx.Heal, 0.9f);
                    ShowToast($"Đã nhận +2 Bình Máu Cổ Ngữ (Hiện có: {p.potions})!");
                });

            // North Abyssal Dungeon Portal
            WorldExitPortal.Create(transform, new Vector3(0f, 0f, 11.5f),
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
        }

        private void CreateTownStation(Vector3 pos, string title, Color glowCol, string prompt, System.Action onUse)
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

            WorldExitPortal.Create(st.transform, Vector3.zero, prompt, onUse);
        }

        private void BuildTownHUD()
        {
            var canvas = GameUIKit.CreateCanvas("TownHUD");
            var root = canvas.transform;

            var top = GameUIKit.MakeStyledPanel("TopBar", root, UIPanelStyle.Ornate,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -82f), new Vector2(-24f, -12f));

            GameUIKit.MakeIcon("TownCrest", top.transform, GameUIKit.GetIcon("anvil"),
                new Vector2(0f, 0.16f), new Vector2(0f, 0.84f), new Vector2(18f, 0f), new Vector2(62f, 0f));

            _topInfoTxt = GameUIKit.MakeText("Info", top.transform, "", 18, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0.54f, 1f), new Vector2(72f, 0f), Vector2.zero);

            GameUIKit.MakeStyledButton("BtnBag", top.transform, "Hành Trang & Cổ Ngữ [I]", 14, UIButtonStyle.Primary,
                new Vector2(0.55f, 0.16f), new Vector2(0.71f, 0.84f), Vector2.zero, Vector2.zero,
                () => _charModal.SetActive(!_charModal.activeSelf), GameUIKit.GetIcon("bag"));

            GameUIKit.MakeStyledButton("BtnLobby", top.transform, "Sảnh Tổ Đội (Lobby)", 14, UIButtonStyle.Secondary,
                new Vector2(0.72f, 0.16f), new Vector2(0.86f, 0.84f), Vector2.zero, Vector2.zero,
                () => GameSession.GoToScene("Lobby"), GameUIKit.GetIcon("crossed_swords"));

            GameUIKit.MakeStyledButton("BtnMenu", top.transform, "Main Menu", 14, UIButtonStyle.Secondary,
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
        }

        private void ShowToast(string msg)
        {
            if (_toastBox != null) _toastBox.SetActive(true);
            _toastTxt.text = msg;
            _toastTimer = 3.5f;
        }

        private void Update()
        {
            var p = GameSession.I.Profile;
            ClassDef cls = GameDB.GetClass(p.selectedClass);
            if (_topInfoTxt != null)
                _topInfoTxt.text = $"THÁNH ĐỊA AETHER HAVEN  •  {cls.name} Lv.{p.level}  •  Vàng: <color=#F8DC85>{p.gold} G</color>  •  Bình máu: {p.potions}  •  Tầng: {p.selectedFloor}";

            var inp = _hero != null ? _hero.GetComponent<HeroPlayerInput>() : null;
            if (inp != null && inp.NearestInteractable != null)
            {
                _interactBox.SetActive(true);
                _interactPrompt.text = inp.NearestInteractable.GetPrompt();
            }
            else if (_interactBox != null) _interactBox.SetActive(false);

            if (_toastTimer > 0f) _toastTimer -= Time.deltaTime;
            else if (_toastBox != null && _toastBox.activeSelf) _toastBox.SetActive(false);
        }
    }
}

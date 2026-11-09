using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssGame
{
    /// <summary>
    /// Section 5 Complete Dungeon Gameplay HUD (ANCIENT GOTHIC – ANTIQUE GOLD & DARK BROWN):
    /// - Bottom-Left Ornate Player Vitals (Portrait, HP + Shield, Mana, Stamina, EXP, Level, Gold, Statuses)
    /// - Bottom-Center Gothic Skill Hotbar (Q, W, E, R with Custom Skill Icons, Rank Badges, Cooldowns, Dodge [Space], Potion [H])
    /// - Top-Left Party Health & Status Plaques (3-4 Hunters with Portraits)
    /// - Top-Center Quest/Objective Tracker + AI Abyss Director Banner + 3-Phase Boss Health Bar with Phase Markers
    /// - Top-Right Ornate Gothic Mini-Map + Quick Relic Buttons ([I], [M], [Esc])
    /// </summary>
    public class DungeonHUDView : MonoBehaviour
    {
        private RectTransform _hpFill, _shieldFill, _mpFill, _staFill, _expFill;
        private Text _hpTxt, _mpTxt, _staTxt, _expTxt, _heroMetaTxt, _statusTxt;

        private readonly Image[] _skillCdOverlays = new Image[4];
        private readonly Text[] _skillCdTexts = new Text[4];
        private readonly Text[] _skillRankTexts = new Text[4];
        private Image _basicWpnCdOverlay, _heavyWpnCdOverlay;
        private Text _basicWpnCdTxt, _basicWpnComboTxt, _heavyWpnCdTxt;
        private Text _dodgeCdTxt, _potionTxt;

        private Transform _partyListRoot;
        private readonly List<(RectTransform fill, Text txt)> _partyWidgets = new List<(RectTransform, Text)>();

        private Text _objectiveTxt, _directorTxt, _bannerTxt, _interactTxt, _lootFeedTxt;
        private GameObject _bannerBox, _interactBox, _lootBox;

        // Boss UI
        private GameObject _bossPanel;
        private RectTransform _bossHpFill;
        private Text _bossTitleTxt, _bossSkillTxt;

        // Minimap
        private Texture2D _mapTex;
        private Color32[] _mapPixels;
        private const int MapSize = 128;
        private float _mapRefreshTimer;

        // Modals
        private GameObject _charModal;
        private GameObject _pauseModal;
        private GameObject _settingsModal;
        private GameObject _fullMapModal;
        private readonly List<string> _lootLogs = new List<string>();
        private float _lootLogTimer;

        public void Build()
        {
            var canvas = GameUIKit.CreateCanvas("DungeonHUDCanvas");
            var root = canvas.transform;
            ScreenCombatFeedbackUI.Attach(root);

            // 1. Top-Center Quest Tracker + AI Abyss Director Banner (UI_Panel_Notification)
            var objBox = GameUIKit.MakeStyledPanel("ObjectivePanel", root, UIPanelStyle.Notification,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-410f, -94f), new Vector2(410f, -12f));

            GameUIKit.MakeIcon("ObjCrestL", objBox.transform, GameUIKit.GetIcon("crossed_swords"),
                new Vector2(0f, 0.22f), new Vector2(0f, 0.82f), new Vector2(14f, 0f), new Vector2(54f, 0f));

            _objectiveTxt = GameUIKit.MakeText("Objective", objBox.transform, "", 17, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.46f), new Vector2(1f, 0.96f), new Vector2(60f, 0f), new Vector2(-60f, 0f));

            _directorTxt = GameUIKit.MakeText("Director", objBox.transform, "", 13, FontStyle.Italic, GameUIKit.ColParchment, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.06f), new Vector2(1f, 0.48f), new Vector2(60f, 0f), new Vector2(-60f, 0f));

            // 2. Boss 3-Phase Health Bar (Engraved Dark Bronze & Blood-Crimson Frame below Objective)
            var bossImg = GameUIKit.MakeStyledPanel("BossHealthPanel", root, UIPanelStyle.Ornate,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-440f, -188f), new Vector2(440f, -100f),
                new Color(1f, 0.90f, 0.88f, 0.98f), GameUIKit.ColDanger);
            _bossPanel = bossImg.gameObject;

            GameUIKit.MakeIcon("BossSkull", bossImg.transform, GameUIKit.GetIcon("skull_boss"),
                new Vector2(0f, 0.48f), new Vector2(0f, 0.94f), new Vector2(14f, 0f), new Vector2(54f, 0f));

            _bossTitleTxt = GameUIKit.MakeText("BossTitle", bossImg.transform, "ABYSSAL KNIGHT — PHASE 1", 17, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.52f), new Vector2(0.60f, 0.96f), new Vector2(60f, 0f), Vector2.zero);

            _bossSkillTxt = GameUIKit.MakeText("BossSkill", bossImg.transform, "", 14, FontStyle.Bold, new Color(1f, 0.55f, 0.48f), TextAnchor.MiddleRight,
                new Vector2(0.52f, 0.52f), new Vector2(1f, 0.96f), Vector2.zero, new Vector2(-18f, 0f));

            var bossBar = GameUIKit.MakeBar("BossHpBar", bossImg.transform, new Color(0.78f, 0.14f, 0.16f),
                new Vector2(0.025f, 0.12f), new Vector2(0.975f, 0.50f), Vector2.zero, Vector2.zero);
            _bossHpFill = bossBar.fill;

            // Phase 2 (66%) and Phase 3 (33%) Antique Gold Tick Dividers
            for (int pTick = 1; pTick <= 2; pTick++)
            {
                float nx = pTick / 3f;
                var tick = GameUIKit.MakeRect("PhaseTick_" + pTick, bossBar.bg.transform,
                    new Vector2(nx, 0f), new Vector2(nx, 1f), new Vector2(-1.5f, 2f), new Vector2(1.5f, -2f));
                tick.gameObject.AddComponent<Image>().color = GameUIKit.ColGoldBright;
            }
            _bossPanel.SetActive(false);

            // 3. Top-Left Party Health & Status HUD (3-4 Members)
            var partyBox = GameUIKit.MakeStyledPanel("PartyHUD", root, UIPanelStyle.Lobby,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -304f), new Vector2(336f, -12f));

            GameUIKit.MakeIcon("PartyIc", partyBox.transform, GameUIKit.GetIcon("shield"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -34f), new Vector2(38f, -8f));

            GameUIKit.MakeText("PartyHeader", partyBox.transform, "TỔ ĐỘI THỢ SĂN (PARTY)", 14, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(44f, -36f), new Vector2(-12f, -6f));

            _partyListRoot = GameUIKit.MakeRect("Members", partyBox.transform,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 10f), new Vector2(-10f, -40f));

            // 4. Top-Right Ornate Gothic Mini-Map + Quick Relic Action Buttons
            var mapFrame = GameUIKit.MakeStyledPanel("MinimapFrame", root, UIPanelStyle.Ornate,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-244f, -246f), new Vector2(-18f, -12f));

            GameUIKit.MakeText("MapTitle", mapFrame.transform, $"BẢN ĐỒ CỔ NGỮ • TẦNG {GameSession.I.Profile.selectedFloor}", 12, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(6f, -28f), new Vector2(-6f, -4f));

            var rawGo = GameUIKit.MakeRect("Radar", mapFrame.transform, Vector2.zero, Vector2.one, new Vector2(12f, 12f), new Vector2(-12f, -30f));
            var rawImg = rawGo.gameObject.AddComponent<RawImage>();
            _mapTex = new Texture2D(MapSize, MapSize, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            _mapPixels = new Color32[MapSize * MapSize];
            rawImg.texture = _mapTex;

            var radarOutline = rawGo.gameObject.AddComponent<Outline>();
            radarOutline.effectColor = GameUIKit.ColBronze;
            radarOutline.effectDistance = new Vector2(1.5f, -1.5f);

            GameUIKit.MakeStyledButton("BtnInv", root, "Hành Trang & Build [I]", 13, UIButtonStyle.Primary,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-244f, -292f), new Vector2(-18f, -252f),
                ToggleCharacterModal, GameUIKit.GetIcon("bag"));

            GameUIKit.MakeStyledButton("BtnFullMap", root, "Bản Đồ Chiến Thuật [M]", 13, UIButtonStyle.Secondary,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-244f, -336f), new Vector2(-18f, -296f),
                ToggleFullMapModal, GameUIKit.GetIcon("portal"));

            GameUIKit.MakeStyledButton("BtnPause", root, "Tạm Dừng [Esc]", 13, UIButtonStyle.Secondary,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-244f, -380f), new Vector2(-18f, -340f),
                TogglePauseModal, GameUIKit.GetIcon("settings"));

            // 5. Center Crosshair + Banner + Interact Prompt + Ornate Loot Notification Toast
            var ch = GameUIKit.MakeRect("Crosshair", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-4f, -4f), new Vector2(4f, 4f));
            var chImg = ch.gameObject.AddComponent<Image>();
            chImg.color = new Color(0.95f, 0.84f, 0.52f, 0.65f);
            chImg.raycastTarget = false;

            var banImg = GameUIKit.MakeStyledPanel("Banner", root, UIPanelStyle.Notification,
                new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(-460f, -30f), new Vector2(460f, 30f));
            _bannerBox = banImg.gameObject;
            _bannerTxt = GameUIKit.MakeText("Txt", banImg.transform, "", 19, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, 0f));
            _bannerBox.SetActive(false);

            var intImg = GameUIKit.MakeStyledPanel("InteractPrompt", root, UIPanelStyle.Notification,
                new Vector2(0.5f, 0.24f), new Vector2(0.5f, 0.24f), new Vector2(-270f, -24f), new Vector2(270f, 24f));
            _interactBox = intImg.gameObject;
            _interactTxt = GameUIKit.MakeText("Txt", intImg.transform, "[F] Tương tác", 17, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(14f, 0f), new Vector2(-14f, 0f));
            _interactBox.SetActive(false);

            var lootPanel = GameUIKit.MakeStyledPanel("LootNotificationPanel", root, UIPanelStyle.Notification,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-490f, 154f), new Vector2(-18f, 294f));
            lootPanel.raycastTarget = false;
            _lootBox = lootPanel.gameObject;
            _lootFeedTxt = GameUIKit.MakeText("LootFeed", lootPanel.transform, "", 13, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, new Vector2(16f, 8f), new Vector2(-16f, -8f));
            _lootFeedTxt.raycastTarget = false;
            _lootBox.SetActive(false);

            // 6. Bottom-Left Player Vitals HUD (UI_Panel_Ornate: Portrait, HP + Shield, Mana, Stamina, EXP, Level, Gold)
            var playerBox = GameUIKit.MakeStyledPanel("PlayerHUD", root, UIPanelStyle.Ornate,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 16f), new Vector2(468f, 192f));

            ClassDef cls = GameDB.GetClass(GameSession.I.Profile.selectedClass);
            var portraitFrame = GameUIKit.MakeStyledPanel("PortraitBox", playerBox.transform, UIPanelStyle.Character,
                new Vector2(0f, 0.26f), new Vector2(0f, 0.92f), new Vector2(14f, 0f), new Vector2(98f, 0f));
            Sprite clsIcon = GameUIKit.GetClassIcon(cls.id);
            if (clsIcon != null)
            {
                GameUIKit.MakeIcon("ClsIcon", portraitFrame.transform, clsIcon,
                    new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero);
            }

            _heroMetaTxt = GameUIKit.MakeText("HeroMeta", playerBox.transform, "", 15, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.77f), new Vector2(1f, 0.96f), new Vector2(108f, 0f), new Vector2(-14f, 0f));

            var hpBar = GameUIKit.MakeBar("HpBar", playerBox.transform, new Color(0.78f, 0.15f, 0.16f),
                new Vector2(0f, 0.56f), new Vector2(1f, 0.76f), new Vector2(108f, 0f), new Vector2(-14f, 0f));
            _hpFill = hpBar.fill;
            _hpTxt = hpBar.label;

            // Shield overlay bar inside HP bar
            _shieldFill = GameUIKit.MakeRect("ShieldOverlay", hpBar.bg.transform, Vector2.zero, new Vector2(0f, 1f), new Vector2(4f, 3f), new Vector2(-4f, -3f));
            var shImg = _shieldFill.gameObject.AddComponent<Image>();
            shImg.color = new Color(0.92f, 0.82f, 0.45f, 0.52f);
            shImg.raycastTarget = false;
            hpBar.label.transform.SetAsLastSibling();

            var mpBar = GameUIKit.MakeBar("MpBar", playerBox.transform, new Color(0.18f, 0.48f, 0.85f),
                new Vector2(0f, 0.38f), new Vector2(1f, 0.54f), new Vector2(108f, 0f), new Vector2(-14f, 0f));
            _mpFill = mpBar.fill;
            _mpTxt = mpBar.label;

            var staBar = GameUIKit.MakeBar("StaBar", playerBox.transform, new Color(0.28f, 0.66f, 0.36f),
                new Vector2(0f, 0.21f), new Vector2(1f, 0.36f), new Vector2(108f, 0f), new Vector2(-14f, 0f));
            _staFill = staBar.fill;
            _staTxt = staBar.label;

            var expBar = GameUIKit.MakeBar("ExpBar", playerBox.transform, new Color(0.84f, 0.66f, 0.26f),
                new Vector2(0f, 0.05f), new Vector2(1f, 0.18f), new Vector2(14f, 0f), new Vector2(-14f, 0f));
            _expFill = expBar.fill;
            _expTxt = expBar.label;

            _statusTxt = GameUIKit.MakeText("Statuses", root, "", 14, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 196f), new Vector2(540f, 224f));

            // 7. Bottom-Center Gothic Combat & Weapon Skill Hotbar:
            // [LMB] Weapon Attack + [RMB/X] Weapon Heavy Skill + [Q, W, E, R] Class Skills + [Space] Dodge + [H] Health Potion
            var skillBox = GameUIKit.MakeStyledPanel("SkillBar", root, UIPanelStyle.Ornate,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-430f, 16f), new Vector2(478f, 126f));

            GameUIKit.GetWeaponSkillNames(cls.id, out string basicWpnName, out string heavyWpnName);
            const float slotW = 0.116f;
            const float slotStep = 0.1225f;
            const float startX = 0.014f;

            // Slot 0: [LMB] Weapon Basic Attack Button
            {
                float x0 = startX;
                var wpnBasicBtn = GameUIKit.MakeStyledButton("WpnBasicSlot", skillBox.transform, "", 12, UIButtonStyle.Secondary,
                    new Vector2(x0, 0.10f), new Vector2(x0 + slotW, 0.90f), Vector2.zero, Vector2.zero, () =>
                    {
                        var inp = DungeonWorldBuilder.Instance?.LocalHero?.GetComponent<HeroPlayerInput>();
                        inp?.TriggerBasicAttackFromUI();
                    });

                GameUIKit.MakeIcon("WpnBasicIcon", wpnBasicBtn.transform, GameUIKit.GetWeaponAttackIcon(cls.id, false),
                    new Vector2(0.14f, 0.16f), new Vector2(0.86f, 0.84f), Vector2.zero, Vector2.zero,
                    new Color(1f, 1f, 1f, 0.96f));

                GameUIKit.MakeText("Key", wpnBasicBtn.transform, "LMB", 10, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.UpperLeft,
                    Vector2.zero, Vector2.one, new Vector2(5f, 2f), new Vector2(-4f, -3f));

                _basicWpnComboTxt = GameUIKit.MakeText("Combo", wpnBasicBtn.transform, "Đòn 1", 10, FontStyle.Bold, new Color(0.98f, 0.84f, 0.48f), TextAnchor.UpperRight,
                    Vector2.zero, Vector2.one, new Vector2(4f, 2f), new Vector2(-5f, -3f));

                GameUIKit.MakeText("Name", wpnBasicBtn.transform, basicWpnName, 10, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.LowerLeft,
                    Vector2.zero, Vector2.one, new Vector2(5f, 3f), new Vector2(-28f, -2f));

                GameUIKit.MakeText("Cost", wpnBasicBtn.transform, "0MP", 9, FontStyle.Bold, new Color(0.68f, 0.95f, 0.72f), TextAnchor.LowerRight,
                    Vector2.zero, Vector2.one, new Vector2(4f, 3f), new Vector2(-5f, -2f));

                var cdRt = GameUIKit.MakeRect("CdOverlay", wpnBasicBtn.transform, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
                _basicWpnCdOverlay = cdRt.gameObject.AddComponent<Image>();
                _basicWpnCdOverlay.color = new Color(0.05f, 0.03f, 0.02f, 0.76f);
                _basicWpnCdOverlay.raycastTarget = false;
                _basicWpnCdTxt = GameUIKit.MakeText("CdTxt", _basicWpnCdOverlay.transform, "", 16, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                _basicWpnCdOverlay.gameObject.SetActive(false);
            }

            // Slot 1: [RMB / X] Weapon Heavy Skill Button (Guard-Breaker / Weapon Technique)
            {
                float x1 = startX + slotStep;
                var wpnHeavyBtn = GameUIKit.MakeStyledButton("WpnHeavySlot", skillBox.transform, "", 12, UIButtonStyle.Primary,
                    new Vector2(x1, 0.10f), new Vector2(x1 + slotW, 0.90f), Vector2.zero, Vector2.zero, () =>
                    {
                        var inp = DungeonWorldBuilder.Instance?.LocalHero?.GetComponent<HeroPlayerInput>();
                        inp?.TriggerHeavyAttackFromUI();
                    });

                GameUIKit.MakeIcon("WpnHeavyIcon", wpnHeavyBtn.transform, GameUIKit.GetWeaponAttackIcon(cls.id, true),
                    new Vector2(0.14f, 0.16f), new Vector2(0.86f, 0.84f), Vector2.zero, Vector2.zero,
                    new Color(1f, 1f, 1f, 0.96f));

                GameUIKit.MakeText("Key", wpnHeavyBtn.transform, "RMB/X", 10, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.UpperLeft,
                    Vector2.zero, Vector2.one, new Vector2(5f, 2f), new Vector2(-4f, -3f));

                GameUIKit.MakeText("Tag", wpnHeavyBtn.transform, "Vũ Khí", 9, FontStyle.Bold, new Color(1f, 0.78f, 0.42f), TextAnchor.UpperRight,
                    Vector2.zero, Vector2.one, new Vector2(4f, 2f), new Vector2(-5f, -3f));

                GameUIKit.MakeText("Name", wpnHeavyBtn.transform, heavyWpnName, 10, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.LowerLeft,
                    Vector2.zero, Vector2.one, new Vector2(5f, 3f), new Vector2(-30f, -2f));

                GameUIKit.MakeText("Cost", wpnHeavyBtn.transform, "20TL", 9, FontStyle.Bold, new Color(0.55f, 0.92f, 0.62f), TextAnchor.LowerRight,
                    Vector2.zero, Vector2.one, new Vector2(4f, 3f), new Vector2(-5f, -2f));

                var cdRt = GameUIKit.MakeRect("CdOverlay", wpnHeavyBtn.transform, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
                _heavyWpnCdOverlay = cdRt.gameObject.AddComponent<Image>();
                _heavyWpnCdOverlay.color = new Color(0.05f, 0.03f, 0.02f, 0.82f);
                _heavyWpnCdOverlay.raycastTarget = false;
                _heavyWpnCdTxt = GameUIKit.MakeText("CdTxt", _heavyWpnCdOverlay.transform, "", 17, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                _heavyWpnCdOverlay.gameObject.SetActive(false);
            }

            // Slots 2..5: Class Active & Ultimate Skills (Q/1, W/2, E/3, R/4)
            string[] hotkeys = { "Q / 1", "W / 2", "E / 3", "R / 4" };
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

                // Full-slot borderless Skill Emblem Icon (no inner empty frame)
                GameUIKit.MakeIcon("SkIcon", btn.transform, GameUIKit.GetSkillIcon(sk),
                    new Vector2(0.14f, 0.16f), new Vector2(0.86f, 0.84f), Vector2.zero, Vector2.zero,
                    new Color(1f, 1f, 1f, 0.96f));

                GameUIKit.MakeText("Key", btn.transform, hotkeys[s], 10, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.UpperLeft,
                    Vector2.zero, Vector2.one, new Vector2(5f, 2f), new Vector2(-4f, -3f));

                _skillRankTexts[s] = GameUIKit.MakeText("Rank", btn.transform, "Lv.1", 10, FontStyle.Bold, new Color(0.68f, 0.95f, 0.72f), TextAnchor.UpperRight,
                    Vector2.zero, Vector2.one, new Vector2(4f, 2f), new Vector2(-5f, -3f));

                GameUIKit.MakeText("Name", btn.transform, sk.name, 10, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.LowerLeft,
                    Vector2.zero, Vector2.one, new Vector2(5f, 3f), new Vector2(-34f, -2f));

                GameUIKit.MakeText("Cost", btn.transform, $"{sk.mana:0}MP", 9, FontStyle.Bold, new Color(0.55f, 0.82f, 1f), TextAnchor.LowerRight,
                    Vector2.zero, Vector2.one, new Vector2(4f, 3f), new Vector2(-5f, -2f));

                // Cooldown Dark Iron Overlay
                var cdRt = GameUIKit.MakeRect("CdOverlay", btn.transform, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
                var cdOv = cdRt.gameObject.AddComponent<Image>();
                cdOv.color = new Color(0.05f, 0.03f, 0.02f, 0.82f);
                cdOv.raycastTarget = false;
                _skillCdOverlays[s] = cdOv;

                _skillCdTexts[s] = GameUIKit.MakeText("CdTxt", cdOv.transform, "", 18, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            // Slot 6: [Space] Clickable Dodge Slot with Winged Greave Icon
            float xDodge = startX + 6 * slotStep;
            var dodgeCard = GameUIKit.MakeStyledButton("DodgeSlot", skillBox.transform, "", 11, UIButtonStyle.Secondary,
                new Vector2(xDodge, 0.10f), new Vector2(xDodge + slotW, 0.90f), Vector2.zero, Vector2.zero, () =>
                {
                    var inp = DungeonWorldBuilder.Instance?.LocalHero?.GetComponent<HeroPlayerInput>();
                    inp?.TriggerDodgeFromUI();
                });
            GameUIKit.MakeIcon("DodgeIc", dodgeCard.transform, GameUIKit.GetIcon("dodge"),
                new Vector2(0.16f, 0.18f), new Vector2(0.84f, 0.82f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.52f));
            _dodgeCdTxt = GameUIKit.MakeText("DodgeTxt", dodgeCard.transform, "[Space]\nNé Đòn\nSẵn sàng", 11, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Slot 7: [H] Crimson Health Potion Slot with Exported Potion Icon
            float xPot = startX + 7 * slotStep;
            var potBtn = GameUIKit.MakeStyledButton("PotionSlot", skillBox.transform, "", 11, UIButtonStyle.Danger,
                new Vector2(xPot, 0.10f), new Vector2(xPot + slotW, 0.90f), Vector2.zero, Vector2.zero, () =>
                {
                    var inp = DungeonWorldBuilder.Instance?.LocalHero?.GetComponent<HeroPlayerInput>();
                    inp?.TryUseHealthPotion();
                });
            GameUIKit.MakeIcon("PotIc", potBtn.transform, GameUIKit.GetIcon("potion"),
                new Vector2(0.16f, 0.18f), new Vector2(0.84f, 0.82f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.65f));
            _potionTxt = GameUIKit.MakeText("PotTxt", potBtn.transform, "", 11, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameUIKit.MakeText("ControlHint", root,
                "WASD: Di chuyển | LMB: Liên kích | RMB/X: Phá Giáp | Space: Né | Tab: Khóa mục tiêu | Q,W,E,R: Kỹ năng | H: Bình máu | I: Hồ sơ & Túi đồ",
                12, FontStyle.Normal, GameUIKit.ColMuted, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-460f, 128f), new Vector2(480f, 150f));

            // Modals
            _settingsModal = GameUIKit.BuildSettingsModal(root);
            _charModal = GameUIKit.BuildCharacterSheetModal(root, () => DungeonWorldBuilder.Instance?.LocalHero, SyncCursorLockState);
            _fullMapModal = DungeonOverlayPanelsUI.BuildFullDungeonMapModal(root, SyncCursorLockState);
            _pauseModal = BuildPauseModal(root);

            HeroPlayerInput.OnToggleInventory = ToggleCharacterModal;
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

        private void OnDestroy()
        {
            HeroPlayerInput.OnToggleInventory = null;
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

            for (int i = 0; i < dw.PartyUnits.Count; i++)
            {
                var u = dw.PartyUnits[i];
                float top = 1f - i * 0.25f;
                var row = GameUIKit.MakeStyledPanel("Member" + i, _partyListRoot, UIPanelStyle.Character,
                    new Vector2(0f, top - 0.235f), new Vector2(1f, top - 0.01f), Vector2.zero, Vector2.zero);

                int cid = u != null && u.HeroClass != null ? u.HeroClass.id : 0;
                Sprite pIc = GameUIKit.GetClassIcon(cid);
                if (pIc != null)
                {
                    GameUIKit.MakeIcon("Portrait", row.transform, pIc,
                        new Vector2(0.025f, 0.12f), new Vector2(0.18f, 0.88f), Vector2.zero, Vector2.zero);
                }

                var bar = GameUIKit.MakeBar("Hp", row.transform, new Color(0.28f, 0.66f, 0.36f),
                    new Vector2(0.20f, 0.14f), new Vector2(0.97f, 0.46f), Vector2.zero, Vector2.zero);
                var lbl = GameUIKit.MakeText("Name", row.transform, "", 12, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                    new Vector2(0.20f, 0.50f), new Vector2(0.97f, 0.94f), Vector2.zero, Vector2.zero);
                _partyWidgets.Add((bar.fill, lbl));
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

            GameUIKit.MakeStyledButton("Settings", box.transform, "CÀI ĐẶT ÂM THANH (SETTINGS)", 16, UIButtonStyle.Secondary,
                new Vector2(0.10f, 0.50f), new Vector2(0.90f, 0.62f), Vector2.zero, Vector2.zero,
                () => _settingsModal.SetActive(true), GameUIKit.GetIcon("settings"));

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
            _charModal.SetActive(next);
            SyncCursorLockState();
        }

        private void ToggleFullMapModal()
        {
            if (_fullMapModal == null) return;
            bool next = !_fullMapModal.activeSelf;
            _fullMapModal.SetActive(next);
            SyncCursorLockState();
        }

        private void TogglePauseModal()
        {
            if (_pauseModal == null) return;
            if (_charModal != null && _charModal.activeSelf)
            {
                _charModal.SetActive(false);
                SyncCursorLockState();
                return;
            }
            if (_fullMapModal != null && _fullMapModal.activeSelf)
            {
                _fullMapModal.SetActive(false);
                SyncCursorLockState();
                return;
            }
            bool next = !_pauseModal.activeSelf;
            _pauseModal.SetActive(next);
            Time.timeScale = next ? 0f : 1f;
            SyncCursorLockState();
        }

        private void SyncCursorLockState()
        {
            bool anyModal = (_charModal != null && _charModal.activeSelf)
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

            // Player HUD
            if (hero != null)
            {
                string formTag = hero.InBearForm ? " [DẠNG GẤU]" : "";
                _heroMetaTxt.text = $"{hero.HeroClass.name.ToUpper()}{formTag} • Lv.{prof.level} • <color=#F8DC85>{prof.gold} G</color>";
                GameUIKit.SetBarRatio(_hpFill, hero.Hp / Mathf.Max(1f, hero.MaxHp));
                GameUIKit.SetBarRatio(_shieldFill, hero.Shield / Mathf.Max(1f, hero.MaxHp));
                _hpTxt.text = hero.Shield > 1f ? $"HP: {hero.Hp:0}/{hero.MaxHp:0} (+Khiên {hero.Shield:0})" : $"HP: {hero.Hp:0} / {hero.MaxHp:0}";

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
                _expTxt.text = $"EXP: {prof.exp:0} / {reqExp:0}" + (totalPts > 0 ? $"  (+{totalPts} Điểm Nâng Cấp [I])" : "");

                // Active Statuses + Lock-On Target HUD Indicator
                var parts = new List<string>();
                var lockSys = CameraTargetLockSystem.Instance;
                if (lockSys != null && lockSys.IsLocked && lockSys.LockedTarget != null)
                {
                    ActorUnit lt = lockSys.LockedTarget;
                    parts.Add($"[MỤC TIÊU KHÓA: {lt.DisplayName} ({lt.Hp:0}/{lt.MaxHp:0})]");
                }
                foreach (var st in hero.Statuses) parts.Add($"{GameDB.StatusName(st.type)} ({st.remaining:0.0}s)");
                _statusTxt.text = parts.Count > 0 ? string.Join("  •  ", parts) : "";

                // Weapon Attack & Weapon Heavy Skill Cooldowns
                if (_basicWpnComboTxt != null)
                    _basicWpnComboTxt.text = $"Đòn {(hero.ComboStep % 3) + 1}";
                if (_basicWpnCdOverlay != null)
                {
                    bool bCd = hero.BasicAttackCd > 0.08f;
                    _basicWpnCdOverlay.gameObject.SetActive(bCd);
                    if (bCd && _basicWpnCdTxt != null) _basicWpnCdTxt.text = $"{hero.BasicAttackCd:0.0}s";
                }
                if (_heavyWpnCdOverlay != null)
                {
                    bool hCd = hero.HeavyAttackCd > 0.05f;
                    _heavyWpnCdOverlay.gameObject.SetActive(hCd);
                    if (hCd && _heavyWpnCdTxt != null) _heavyWpnCdTxt.text = $"{hero.HeavyAttackCd:0.0}s";
                }

                // Skills cooldowns & ranks
                for (int s = 0; s < 4; s++)
                {
                    int rnk = ClassSpecializationSystem.GetSkillRank(hero, s);
                    if (_skillRankTexts[s] != null) _skillRankTexts[s].text = $"Lv.{rnk}";
                    float cd = hero.SkillCds[s];
                    bool onCd = cd > 0.05f;
                    _skillCdOverlays[s].gameObject.SetActive(onCd);
                    if (onCd) _skillCdTexts[s].text = $"{cd:0.0}s";
                }

                _dodgeCdTxt.text = hero.DodgeCd > 0.05f ? $"[Space]\nNé Đòn\n<color=#F8DC85>{hero.DodgeCd:0.0}s</color>" : "[Space]\nNé Đòn\nSẵn sàng";

                var inp = hero.GetComponent<HeroPlayerInput>();
                float potCd = inp != null ? inp.PotionCooldown : 0f;
                _potionTxt.text = potCd > 0.05f ? $"[H] Bình Máu\n({prof.potions})\n{potCd:0.0}s" : $"[H] Bình Máu\nx{prof.potions}";

                // Interact prompt
                if (inp != null && inp.NearestInteractable != null)
                {
                    _interactBox.SetActive(true);
                    _interactTxt.text = inp.NearestInteractable.GetPrompt();
                }
                else _interactBox.SetActive(false);
            }

            // Party HUD
            if (_partyWidgets.Count != dw.PartyUnits.Count) RebuildPartyWidgets();
            for (int i = 0; i < dw.PartyUnits.Count && i < _partyWidgets.Count; i++)
            {
                var u = dw.PartyUnits[i];
                if (u == null) continue;
                GameUIKit.SetBarRatio(_partyWidgets[i].fill, u.Hp / Mathf.Max(1f, u.MaxHp));
                string st = u.IsDead ? "<color=#F06060>[TỬ TRẬN]</color>" : (u.IsDowned ? "<color=#F8DC85>[GỤC - CỨU!]</color>" : $"{u.Hp:0}/{u.MaxHp:0}");
                _partyWidgets[i].txt.text = $"{u.DisplayName} <size=11>({u.HeroClass.name})</size> — {st}";
            }

            // Objective & AI Abyss Director
            _objectiveTxt.text = dw.CurrentObjective;
            if (AbyssDirectorAI.Instance != null)
                _directorTxt.text = AbyssDirectorAI.Instance.LastDecision;

            // Banner
            if (dw.BannerTimer > 0f && !string.IsNullOrEmpty(dw.BannerMessage))
            {
                _bannerBox.SetActive(true);
                _bannerTxt.text = dw.BannerMessage;
            }
            else _bannerBox.SetActive(false);

            // Loot feed
            if (_lootLogTimer > 0f && _lootLogs.Count > 0)
            {
                _lootLogTimer -= Time.deltaTime;
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
            _mapRefreshTimer -= Time.deltaTime;
            if (_mapRefreshTimer <= 0f)
            {
                _mapRefreshTimer = 0.08f;
                RenderMinimap(dw);
            }
        }

        private void RenderMinimap(DungeonWorldBuilder dw)
        {
            if (_mapTex == null || dw.LocalHero == null) return;
            // Ancient Gothic Obsidian-Brown Radar Background
            Color32 bg = new Color32(18, 13, 9, 255);
            for (int i = 0; i < _mapPixels.Length; i++) _mapPixels[i] = bg;

            Vector3 focus = dw.LocalHero.transform.position;
            const float worldSpan = 96f;

            int WorldToPxX(float wx) => Mathf.RoundToInt(((wx - focus.x) / worldSpan + 0.5f) * (MapSize - 1));
            int WorldToPxY(float wz) => Mathf.RoundToInt(((wz - focus.z) / worldSpan + 0.5f) * (MapSize - 1));

            // Draw rooms in Warm Engraved Bronze & Cleared Emerald-Bronze
            Color32 roomCol = new Color32(66, 48, 32, 255);
            Color32 clearedCol = new Color32(44, 68, 42, 255);
            Color32 borderCol = new Color32(142, 104, 52, 255);
            foreach (var r in dw.Rooms)
            {
                int x0 = Mathf.Clamp(WorldToPxX(r.center.x - r.width * 0.5f), 0, MapSize - 1);
                int x1 = Mathf.Clamp(WorldToPxX(r.center.x + r.width * 0.5f), 0, MapSize - 1);
                int y0 = Mathf.Clamp(WorldToPxY(r.center.z - r.length * 0.5f), 0, MapSize - 1);
                int y1 = Mathf.Clamp(WorldToPxY(r.center.z + r.length * 0.5f), 0, MapSize - 1);
                Color32 rc = r.cleared ? clearedCol : roomCol;
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        bool isEdge = (x == x0 || x == x1 || y == y0 || y == y1);
                        _mapPixels[y * MapSize + x] = isEdge ? borderCol : rc;
                    }
                }
            }

            // Draw units
            foreach (var u in ActorUnit.AllUnits)
            {
                if (u == null || u.IsDead) continue;
                int px = WorldToPxX(u.transform.position.x);
                int py = WorldToPxY(u.transform.position.z);
                Color32 dc = u.IsLocalPlayer ? new Color32(250, 218, 100, 255)
                           : (u.Team == Team.Hero ? new Color32(95, 225, 125, 255)
                           : (u.IsBoss ? new Color32(255, 110, 55, 255) : new Color32(225, 58, 52, 255)));
                int rad = (u.IsLocalPlayer || u.IsBoss) ? 3 : 2;
                StampDot(px, py, rad, dc);
            }

            _mapTex.SetPixels32(_mapPixels);
            _mapTex.Apply(false);
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

            if (Input.GetKeyDown(KeyCode.F7) && dw.Rooms.Count >= 7)
            {
                Vector3 bossEntry = dw.Rooms[6].center + new Vector3(0f, 0f, -12f);
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

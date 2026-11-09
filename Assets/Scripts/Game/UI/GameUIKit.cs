using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AbyssGame
{
    public enum UIPanelStyle
    {
        Standard,
        Ornate,
        Inventory,
        Character,
        SkillTree,
        Lobby,
        Notification,
        Dialog
    }

    public enum UIButtonStyle
    {
        Primary,
        Secondary,
        Danger,
        Disabled,
        Selected,
        Tab
    }

    /// <summary>
    /// Official Design System & UI Factory for ABYSSAL DUNGEON:
    /// Style: ANCIENT GOTHIC – ANTIQUE GOLD & DARK BROWN
    /// Provides 8 9-sliced Gothic Panel types, 6 Gothic Button types, Cinzel/Gothic Typography,
    /// Medieval Engraving & Rune Icons, and the 3-Column Ancient Reliquary Character/Skill/Inventory Modal.
    /// </summary>
    public static class GameUIKit
    {
        private static Font _titleFont;
        private static Font _bodyFont;
        private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<int, Sprite> _classIcons = new Dictionary<int, Sprite>();

        // =====================================================================
        // OFFICIAL COLOR SYSTEM: ANCIENT GOTHIC – ANTIQUE GOLD & DARK BROWN
        // =====================================================================
        public static readonly Color ColBgDark      = new Color(0.08f, 0.055f, 0.04f, 0.96f);   // #140E0A Deep Abyssal Umber
        public static readonly Color ColPanel       = new Color(0.14f, 0.10f, 0.07f, 0.96f);    // #241A12 Dark Aged Leather & Bronze
        public static readonly Color ColCard        = new Color(0.19f, 0.135f, 0.095f, 0.96f);  // #302218 Warm Dark Chestnut Plate
        public static readonly Color ColGold        = new Color(0.85f, 0.69f, 0.34f, 1.00f);    // #D9B057 Antique Gold
        public static readonly Color ColGoldBright  = new Color(0.97f, 0.86f, 0.52f, 1.00f);    // #F8DC85 Illuminated Gold Highlight
        public static readonly Color ColBronze      = new Color(0.64f, 0.46f, 0.24f, 1.00f);    // #A3753D Engraved Antique Bronze
        public static readonly Color ColIron        = new Color(0.22f, 0.18f, 0.15f, 0.96f);    // #382E26 Dark Forged Iron
        public static readonly Color ColParchment   = new Color(0.93f, 0.86f, 0.73f, 1.00f);    // #EDE0BA Aged Parchment Text
        public static readonly Color ColAccent      = new Color(0.76f, 0.54f, 0.20f, 1.00f);    // #C28A33 Burnished Amber-Gold (Primary Action)
        public static readonly Color ColGreen       = new Color(0.28f, 0.62f, 0.32f, 1.00f);    // #479E52 Medieval Emerald Seal
        public static readonly Color ColDanger      = new Color(0.68f, 0.16f, 0.15f, 1.00f);    // #AD2926 Blood Crimson
        public static readonly Color ColMuted       = new Color(0.74f, 0.65f, 0.52f, 1.00f);    // #BDA685 Warm Aged Pewter/Tan

        // =====================================================================
        // TYPOGRAPHY SYSTEM (Title Serif + Body Sans with Full Vietnamese Support)
        // =====================================================================
        public static Font TitleFont
        {
            get
            {
                if (_titleFont != null) return _titleFont;
                _titleFont = Resources.Load<Font>("UI/Fonts/GothicTitle");
                if (_titleFont == null)
                {
                    try { _titleFont = Font.CreateDynamicFontFromOSFont(new[] { "Cinzel", "Palatino Linotype", "Georgia", "Times New Roman" }, 24); }
                    catch { /* ignore */ }
                }
                if (_titleFont == null) _titleFont = DefaultFont;
                return _titleFont;
            }
        }

        public static Font BodyFont
        {
            get
            {
                if (_bodyFont != null) return _bodyFont;
                _bodyFont = Resources.Load<Font>("UI/Fonts/GothicBody");
                if (_bodyFont == null)
                {
                    try { _bodyFont = Font.CreateDynamicFontFromOSFont(new[] { "Noto Sans", "Inter", "Segoe UI", "Arial" }, 16); }
                    catch { /* ignore */ }
                }
                if (_bodyFont == null) _bodyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_bodyFont == null) _bodyFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _bodyFont;
            }
        }

        public static Font DefaultFont => BodyFont;

        // =====================================================================
        // SPRITE & ICON LOADER (With Runtime 9-Slice Support)
        // =====================================================================
        public static Sprite LoadSlicedSprite(string resourcePath, Vector4 border)
        {
            string key = resourcePath + ":" + border;
            if (_spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
                return cached;

            Texture2D tex = Resources.Load<Texture2D>(resourcePath);
            if (tex != null)
            {
                Sprite sp = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect,
                    border);
                _spriteCache[key] = sp;
                return sp;
            }

            Sprite direct = Resources.Load<Sprite>(resourcePath);
            if (direct != null)
            {
                _spriteCache[key] = direct;
                return direct;
            }
            return null;
        }

        public static Sprite GetPanelSprite(UIPanelStyle style)
        {
            string name = "UI_Panel_" + style;
            return LoadSlicedSprite("UI/Panels/" + name, new Vector4(32, 32, 32, 32))
                   ?? LoadSlicedSprite("UI/panel_frame", new Vector4(32, 32, 32, 32));
        }

        public static Sprite GetButtonSprite(UIButtonStyle style)
        {
            string name = "Btn_" + style;
            return LoadSlicedSprite("UI/Buttons/" + name, new Vector4(24, 20, 24, 20));
        }

        public static Sprite GetDecorSprite(string name, Vector4 border = default)
        {
            return LoadSlicedSprite("UI/Decor/" + name, border);
        }

        public static Sprite GetIcon(string iconName)
        {
            if (string.IsNullOrEmpty(iconName)) return null;
            string clean = iconName.StartsWith("icon_") ? iconName : ("icon_" + iconName);
            return LoadSlicedSprite("UI/Icons/" + clean, Vector4.zero);
        }

        public static Sprite GetSkillIcon(SkillDef sk)
        {
            if (sk == null) return GetIcon("crossed_swords");
            return GetIcon("kind_" + sk.kind) ?? GetIcon("crossed_swords");
        }

        public static Sprite GetWeaponAttackIcon(int classId, bool heavy)
        {
            int cid = Mathf.Clamp(classId, 0, 9);
            string name = heavy ? $"wpn_heavy_{cid}" : $"wpn_{cid}";
            return GetIcon(name) ?? GetIcon("sword");
        }

        public static void GetWeaponSkillNames(int classId, out string basicName, out string heavyName)
        {
            switch (Mathf.Clamp(classId, 0, 9))
            {
                case 0: basicName = "Trảm Kiếm";  heavyName = "Phá Khiên"; break;
                case 1: basicName = "Chùy Kích";  heavyName = "Chấn Địa";  break;
                case 2: basicName = "Cuồng Trảm"; heavyName = "Bổ Rìu";    break;
                case 3: basicName = "Song Kích";  heavyName = "Đoạt Mệnh"; break;
                case 4: basicName = "Xạ Tiễn";    heavyName = "Xuyên Phá"; break;
                case 5: basicName = "Ma Đạn";     heavyName = "Nổ Nguyên Tố"; break;
                case 6: basicName = "Hắc Tiễn";   heavyName = "Hồn Bạo";   break;
                case 7: basicName = "Quyền Cước"; heavyName = "Long Chưởng"; break;
                case 8: basicName = "Phong Trảm"; heavyName = "Nộ Thảo";   break;
                case 9: basicName = "Thánh Chùy"; heavyName = "Thiên Phạt"; break;
                default: basicName = "Liên Kích"; heavyName = "Phá Giáp";  break;
            }
        }

        public static Sprite GetSlotIcon(ItemSlot slot)
        {
            switch (slot)
            {
                case ItemSlot.Weapon: return GetIcon("sword");
                case ItemSlot.Armor:  return GetIcon("armor");
                case ItemSlot.Helmet: return GetIcon("helm");
                case ItemSlot.Ring:   return GetIcon("ring");
                case ItemSlot.Amulet: return GetIcon("amulet");
                default:              return GetIcon("sword");
            }
        }

        public static Sprite GetClassIcon(int classId)
        {
            if (_classIcons.TryGetValue(classId, out Sprite sp) && sp != null) return sp;
            string[] names = { "knight", "guardian", "berserker", "assassin", "ranger", "mage", "warlock", "monk", "druid", "cleric" };
            int idx = Mathf.Clamp(classId, 0, names.Length - 1);
            sp = LoadSlicedSprite("UI/Classes/icon_" + names[idx], Vector4.zero);
            _classIcons[classId] = sp;
            return sp;
        }

        public static Sprite GetBackgroundSprite(string name)
        {
            return LoadSlicedSprite("UI/" + name, Vector4.zero);
        }

        // =====================================================================
        // CANVAS & EVENT SYSTEM
        // =====================================================================
        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        public static Canvas CreateCanvas(string name = "GameCanvas", int sortOrder = 10)
        {
            EnsureEventSystem();
            var go = new GameObject(name);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = sortOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        public static RectTransform MakeRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        // =====================================================================
        // PANEL SYSTEM (8 Ornate 9-Sliced Gothic Panel Styles)
        // =====================================================================
        private static UIPanelStyle InferPanelStyle(string name, Color borderColor)
        {
            if (string.IsNullOrEmpty(name)) return UIPanelStyle.Standard;
            string n = name.ToLowerInvariant();
            if (n.Contains("modal") || n.Contains("dialog") || n.Contains("popup") || n.Contains("tooltip") || n.Contains("summary"))
                return UIPanelStyle.Dialog;
            if (n.Contains("inv") || n.Contains("bag") || n.Contains("loot") || n.Contains("equip") || n.Contains("slot"))
                return UIPanelStyle.Inventory;
            if (n.Contains("skill") || n.Contains("tree") || n.Contains("rune"))
                return UIPanelStyle.SkillTree;
            if (n.Contains("char") || n.Contains("hero") || n.Contains("stat") || n.Contains("portrait") || n.Contains("detail"))
                return UIPanelStyle.Character;
            if (n.Contains("lobby") || n.Contains("room") || n.Contains("party"))
                return UIPanelStyle.Lobby;
            if (n.Contains("banner") || n.Contains("toast") || n.Contains("notice") || n.Contains("prompt") || n.Contains("director") || n.Contains("top") || n.Contains("boss"))
                return UIPanelStyle.Notification;
            if (borderColor.a > 0.4f && (borderColor.r > 0.7f || borderColor == ColGold))
                return UIPanelStyle.Ornate;
            return UIPanelStyle.Standard;
        }

        private static Color NormalizeGothicTint(Color legacyBg, bool hasSprite)
        {
            if (!hasSprite) return legacyBg;
            // If transparent/invisible container, preserve alpha
            if (legacyBg.a < 0.05f) return legacyBg;

            // Detect legacy flat dark-purple/navy tints and convert to warm white/amber multiplier for the textured 9-slice sprite
            bool wasDarkPanel = (legacyBg.r < 0.30f && legacyBg.g < 0.30f && legacyBg.b < 0.48f);
            if (wasDarkPanel)
                return new Color(1f, 0.98f, 0.94f, Mathf.Max(0.94f, legacyBg.a));

            // Otherwise blend the requested tint gently with the underlying Gothic texture
            return new Color(
                Mathf.Lerp(0.85f, 1f, legacyBg.r),
                Mathf.Lerp(0.82f, 1f, legacyBg.g),
                Mathf.Lerp(0.75f, 1f, legacyBg.b),
                Mathf.Max(0.92f, legacyBg.a));
        }

        public static Image MakeStyledPanel(
            string name,
            Transform parent,
            UIPanelStyle style,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color? tintOverride = null,
            Color? borderOutlineColor = null)
        {
            var rt = MakeRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            var img = rt.gameObject.AddComponent<Image>();
            Sprite sp = GetPanelSprite(style);
            if (sp != null)
            {
                img.sprite = sp;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f;
                img.color = tintOverride ?? new Color(1f, 0.98f, 0.95f, 0.98f);
            }
            else
            {
                img.color = tintOverride ?? ColPanel;
            }

            Color outCol = borderOutlineColor ?? (style == UIPanelStyle.Ornate || style == UIPanelStyle.Dialog
                ? new Color(ColGold.r, ColGold.g, ColGold.b, 0.65f)
                : new Color(ColBronze.r, ColBronze.g, ColBronze.b, 0.45f));

            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = outCol;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance = new Vector2(3.5f, -4.5f);

            // Ancient Gothic Luxury surface relief & filigree corner brackets
            AddAncientGothicPanelDecor(rt, style);

            return img;
        }

        private static void AddAncientGothicPanelDecor(RectTransform panelRt, UIPanelStyle style)
        {
            if (panelRt == null) return;

            // 1. Non-stretched tiled weathered stone & parchment relief inside the frame
            Sprite surfaceSp = GetDecorSprite("Panel_Ancient_Surface");
            if (surfaceSp != null)
            {
                var surfRt = MakeRect("_GothicDecor_Relief", panelRt, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));
                var surfImg = surfRt.gameObject.AddComponent<Image>();
                surfImg.sprite = surfaceSp;
                surfImg.type = Image.Type.Tiled;
                surfImg.pixelsPerUnitMultiplier = 1.25f;
                surfImg.color = new Color(1f, 0.94f, 0.82f, 0.24f);
                surfImg.raycastTarget = false;
            }

            // 2. Inner recessed antique bronze-gold filigree rim
            var rimRt = MakeRect("_GothicDecor_InnerRim", panelRt, Vector2.zero, Vector2.one, new Vector2(7f, 7f), new Vector2(-7f, -7f));
            var rimImg = rimRt.gameObject.AddComponent<Image>();
            rimImg.color = new Color(0f, 0f, 0f, 0f);
            rimImg.raycastTarget = false;
            var rimOutline = rimRt.gameObject.AddComponent<Outline>();
            rimOutline.effectColor = (style == UIPanelStyle.Ornate || style == UIPanelStyle.Dialog)
                ? new Color(ColGold.r, ColGold.g, ColGold.b, 0.42f)
                : new Color(ColBronze.r, ColBronze.g, ColBronze.b, 0.30f);
            rimOutline.effectDistance = new Vector2(1f, -1f);

            // 3. 4 Non-stretched sculpted antique-gold Gothic corner brackets on major panels
            bool addCornerBrackets = style == UIPanelStyle.Ornate
                                  || style == UIPanelStyle.Dialog
                                  || style == UIPanelStyle.Character
                                  || style == UIPanelStyle.Inventory
                                  || style == UIPanelStyle.SkillTree
                                  || style == UIPanelStyle.Lobby;
            Sprite cornerSp = addCornerBrackets ? GetDecorSprite("Corner_Gothic_Filigree") : null;
            if (cornerSp != null)
            {
                float cSize = (style == UIPanelStyle.Dialog || style == UIPanelStyle.Ornate) ? 30f : 24f;
                CreateCornerFiligree("_GothicDecor_CornerTL", panelRt, cornerSp, new Vector2(0f, 1f), new Vector2(3f, -3f - cSize), new Vector2(3f + cSize, -3f), new Vector3(1f, 1f, 1f));
                CreateCornerFiligree("_GothicDecor_CornerTR", panelRt, cornerSp, new Vector2(1f, 1f), new Vector2(-3f - cSize, -3f - cSize), new Vector2(-3f, -3f), new Vector3(-1f, 1f, 1f));
                CreateCornerFiligree("_GothicDecor_CornerBL", panelRt, cornerSp, new Vector2(0f, 0f), new Vector2(3f, 3f), new Vector2(3f + cSize, 3f + cSize), new Vector3(1f, -1f, 1f));
                CreateCornerFiligree("_GothicDecor_CornerBR", panelRt, cornerSp, new Vector2(1f, 0f), new Vector2(-3f - cSize, 3f), new Vector2(-3f, 3f + cSize), new Vector3(-1f, -1f, 1f));
            }
        }

        private static void CreateCornerFiligree(string name, Transform parent, Sprite sp, Vector2 anchor, Vector2 offMin, Vector2 offMax, Vector3 scale)
        {
            var rt = MakeRect(name, parent, anchor, anchor, offMin, offMax);
            rt.localScale = scale;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sp;
            img.preserveAspect = true;
            img.color = new Color(1f, 0.96f, 0.86f, 0.92f);
            img.raycastTarget = false;
        }

        public static Image MakePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color bg, Color? borderColor = null)
        {
            // Keep tiny decorative rects or pure white icon holders simple if needed
            if (name == "Ic" || name == "Fill" || name.StartsWith("Heart") || bg.a <= 0.02f)
            {
                var rt = MakeRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
                var img = rt.gameObject.AddComponent<Image>();
                img.color = bg;
                if (borderColor.HasValue)
                {
                    var outline = rt.gameObject.AddComponent<Outline>();
                    outline.effectColor = borderColor.Value;
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                }
                return img;
            }

            UIPanelStyle style = InferPanelStyle(name, borderColor ?? Color.clear);
            Color tint = NormalizeGothicTint(bg, true);
            return MakeStyledPanel(name, parent, style, anchorMin, anchorMax, offsetMin, offsetMax, tint, borderColor);
        }

        // =====================================================================
        // DECORATIVE DIVIDERS & ICONS
        // =====================================================================
        public static Image MakeGothicDivider(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = MakeRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            var img = rt.gameObject.AddComponent<Image>();
            Sprite sp = GetDecorSprite("Divider_Gothic_Gold");
            if (sp != null)
            {
                img.sprite = sp;
                img.preserveAspect = false;
                img.color = Color.white;
            }
            else
            {
                img.color = ColGold;
            }
            img.raycastTarget = false;
            return img;
        }

        public static Image MakeIcon(string name, Transform parent, Sprite iconSprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color? tint = null)
        {
            var rt = MakeRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = iconSprite;
            img.preserveAspect = true;
            img.color = tint ?? Color.white;
            img.raycastTarget = false;
            return img;
        }

        // =====================================================================
        // TYPOGRAPHY FACTORY
        // =====================================================================
        private static bool ShouldUseTitleFont(string name, int size, FontStyle style)
        {
            if (size >= 21) return true;
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.ToLowerInvariant();
            return n.Contains("title") || n.Contains("header") || n.Contains("headline") || n.Contains("logo") || n.Contains("banner");
        }

        public static Text MakeText(string name, Transform parent, string content, int size, FontStyle style, Color color, TextAnchor align,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = MakeRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            var t = rt.gameObject.AddComponent<Text>();
            bool isTitle = ShouldUseTitleFont(name, size, style);
            t.font = isTitle ? TitleFont : BodyFont;
            t.text = content;
            t.fontSize = size;
            t.fontStyle = style;
            // Warm pure white into Aged Parchment for authentic Ancient Gothic readability
            if (color == Color.white && !isTitle)
                t.color = ColParchment;
            else
                t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;

            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.88f);
            sh.effectDistance = isTitle ? new Vector2(2.0f, -2.0f) : new Vector2(1.4f, -1.4f);

            if (isTitle)
            {
                var ol = rt.gameObject.AddComponent<Outline>();
                ol.effectColor = new Color(0.08f, 0.05f, 0.02f, 0.75f);
                ol.effectDistance = new Vector2(1f, -1f);
            }
            return t;
        }

        // =====================================================================
        // BUTTON SYSTEM (6 Ornate Gothic Button Styles + Hover/Press/Rune FX)
        // =====================================================================
        private static UIButtonStyle InferButtonStyle(string name, Color bg)
        {
            if (!string.IsNullOrEmpty(name))
            {
                string n = name.ToLowerInvariant();
                if (n.Contains("quit") || n.Contains("danger") || n.Contains("delete") || n.Contains("sell") || n.Contains("exit"))
                    return UIButtonStyle.Danger;
                if (n.Contains("tab") || n.Contains("filter"))
                    return UIButtonStyle.Tab;
                if (n.Contains("play") || n.Contains("start") || n.Contains("confirm") || n.Contains("create") || n.Contains("upgrade") || n.Contains("equip"))
                    return UIButtonStyle.Primary;
            }
            if (bg.r > 0.55f && bg.g < 0.28f && bg.b < 0.28f)
                return UIButtonStyle.Danger;
            if ((bg.g > 0.50f && bg.r < 0.45f) || (bg.r > 0.65f && bg.g > 0.45f && bg.b < 0.35f))
                return UIButtonStyle.Primary;
            return UIButtonStyle.Secondary;
        }

        public static Button MakeStyledButton(
            string name,
            Transform parent,
            string label,
            int fontSize,
            UIButtonStyle style,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Action onClick,
            Sprite leftIcon = null)
        {
            var rt = MakeRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            var img = rt.gameObject.AddComponent<Image>();
            Sprite sp = GetButtonSprite(style);
            if (sp != null)
            {
                img.sprite = sp;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else
            {
                img.color = style == UIButtonStyle.Primary ? ColAccent : (style == UIButtonStyle.Danger ? ColDanger : ColCard);
            }

            var outline = rt.gameObject.AddComponent<Outline>();
            Color outCol = style == UIButtonStyle.Primary || style == UIButtonStyle.Selected
                ? new Color(ColGoldBright.r, ColGoldBright.g, ColGoldBright.b, 0.85f)
                : (style == UIButtonStyle.Danger
                    ? new Color(0.92f, 0.42f, 0.32f, 0.8f)
                    : new Color(ColBronze.r, ColBronze.g, ColBronze.b, 0.7f));
            outline.effectColor = outCol;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            float textLeft = 10f;
            if (leftIcon != null)
            {
                var ic = MakeIcon("BtnIcon", rt, leftIcon, new Vector2(0f, 0.14f), new Vector2(0f, 0.86f), new Vector2(10f, 0f), new Vector2(42f, 0f));
                ic.raycastTarget = false;
                textLeft = 46f;
            }

            Color txtCol = style == UIButtonStyle.Primary || style == UIButtonStyle.Selected
                ? ColGoldBright
                : (style == UIButtonStyle.Disabled ? ColMuted : ColParchment);

            Text lbl = null;
            if (!string.IsNullOrEmpty(label))
            {
                lbl = MakeText("Label", rt, label, fontSize, FontStyle.Bold, txtCol, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(textLeft, 2f), new Vector2(-10f, -2f));
                lbl.raycastTarget = false;
            }

            var fx = rt.gameObject.AddComponent<UIButtonGothicFX>();
            fx.targetImage = img;
            fx.borderOutline = outline;
            fx.labelRect = lbl != null ? lbl.rectTransform : null;
            fx.style = style;
            fx.baseColor = Color.white;
            fx.baseOutlineColor = outCol;
            fx.CacheLabelOffsets();
            fx.ApplyVisualState();

            btn.onClick.AddListener(() =>
            {
                GameAudio.I?.PlaySfx(GameSfx.Click, 0.6f);
                onClick?.Invoke();
            });
            return btn;
        }

        public static void SetButtonStyle(Button btn, UIButtonStyle style, Color? customTint = null)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                Sprite sp = GetButtonSprite(style);
                if (sp != null)
                {
                    img.sprite = sp;
                    img.type = Image.Type.Sliced;
                }
                img.color = customTint ?? Color.white;
            }
            var fx = btn.GetComponent<UIButtonGothicFX>();
            if (fx != null)
            {
                fx.style = style;
                fx.baseColor = customTint ?? Color.white;
                fx.baseOutlineColor = (style == UIButtonStyle.Primary || style == UIButtonStyle.Selected)
                    ? ColGoldBright
                    : (style == UIButtonStyle.Danger ? new Color(0.92f, 0.42f, 0.32f, 0.8f) : ColBronze);
                fx.ApplyVisualState();
            }
        }

        public static Button MakeButton(string name, Transform parent, string label, int fontSize, Color bg, Color fg,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Action onClick)
        {
            UIButtonStyle style = InferButtonStyle(name, bg);
            var btn = MakeStyledButton(name, parent, label, fontSize, style, anchorMin, anchorMax, offsetMin, offsetMax, onClick);
            var lbl = btn.transform.Find("Label")?.GetComponent<Text>();
            if (lbl != null && fg != Color.white)
                lbl.color = fg;
            return btn;
        }

        // =====================================================================
        // PROGRESS BARS (Beveled Gothic Iron Frame + Engraved Metallic Fill)
        // =====================================================================
        public static (Image bg, RectTransform fill, Text label) MakeBar(string name, Transform parent, Color fillColor,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int fontSize = 13)
        {
            var bgRt = MakeRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            var bg = bgRt.gameObject.AddComponent<Image>();
            Sprite frameSp = GetDecorSprite("Bar_Frame_Gothic", new Vector4(12, 8, 12, 8));
            if (frameSp != null)
            {
                bg.sprite = frameSp;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0.08f, 0.055f, 0.04f, 0.95f);
            }

            var fillRt = MakeRect("Fill", bg.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(4f, 3f), new Vector2(-4f, -3f));
            var fillImg = fillRt.gameObject.AddComponent<Image>();
            Sprite fillSp = GetDecorSprite("Bar_Fill_Gothic");
            if (fillSp != null)
            {
                fillImg.sprite = fillSp;
                fillImg.type = Image.Type.Simple;
            }
            fillImg.color = fillColor;

            var lbl = MakeText("Text", bg.transform, "", fontSize, FontStyle.Bold, ColParchment, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return (bg, fillRt, lbl);
        }

        public static void SetBarRatio(RectTransform fill, float ratio)
        {
            if (fill == null) return;
            fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        }

        // =====================================================================
        // REDESIGNED ANCIENT GOTHIC CHARACTER, SKILL TREE & INVENTORY MODAL
        // (Covers Screens C: Stats & Build, D: Inventory & Equipment, E: Skill Tree)
        // =====================================================================
        public static GameObject BuildCharacterSheetModal(Transform canvasRoot, Func<ActorUnit> getHero, Action onChanged)
        {
            var modal = MakeStyledPanel(
                "CharacterSheetModal", canvasRoot, UIPanelStyle.Dialog,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-880f, -470f), new Vector2(880f, 470f));

            // Ornate Top Header
            MakeIcon("CrestLeft", modal.transform, GetIcon("crown"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -60f), new Vector2(66f, -20f));

            MakeText("Title", modal.transform,
                "DI VẬT THỢ SĂN — HỒ SƠ CHỈ SỐ, CÂY CỔ NGỮ & HÀNH TRANG",
                24, FontStyle.Bold, ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0.76f, 1f), new Vector2(76f, -64f), new Vector2(0f, -14f));

            MakeGothicDivider("HeaderDiv", modal.transform,
                new Vector2(0.02f, 1f), new Vector2(0.98f, 1f), new Vector2(0f, -72f), new Vector2(0f, -54f));

            MakeStyledButton("CloseBtn", modal.transform, "ĐÓNG [ESC / I]", 15, UIButtonStyle.Danger,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-196f, -60f), new Vector2(-24f, -16f),
                () => modal.gameObject.SetActive(false));

            var bodyRoot = MakeRect("Body", modal.transform, Vector2.zero, Vector2.one, new Vector2(20f, 18f), new Vector2(-20f, -76f));

            int activeBagFilter = -1; // -1 = All, 0 = Weapon, 1 = Armor/Helm, 2 = Ring/Amulet
            EquipItem inspectedItem = null;
            bool inspectedFromEquipped = false;

            Action rebuild = null;
            rebuild = () =>
            {
                for (int i = bodyRoot.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.Destroy(bodyRoot.GetChild(i).gameObject);

                var p = GameSession.I.Profile;
                ClassDef cls = GameDB.GetClass(p.selectedClass);
                ActorUnit hero = getHero?.Invoke();
                Color buildAccent = ClassSpecializationSystem.GetBuildAccentColor(p.selectedClass, p.selectedBuild, cls.color);
                var arch = ClassSpecializationSystem.GetClassArchetype(p.selectedClass);
                string archTag = ClassSpecializationSystem.GetArchetypeShortTag(arch);

                // =================================================================
                // COLUMN 1 (LEFT): UI_Panel_Character — CHARACTER STATS & BUILD
                // =================================================================
                var colChar = MakeStyledPanel("UI_Panel_Character", bodyRoot, UIPanelStyle.Character,
                    new Vector2(0.00f, 0f), new Vector2(0.32f, 1f), Vector2.zero, Vector2.zero);

                // Hero Portrait & Identity Plaque
                var idCard = MakeStyledPanel("HeroIdentity", colChar.transform, UIPanelStyle.Ornate,
                    new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.97f), Vector2.zero, Vector2.zero);

                Sprite clsPortrait = GetClassIcon(p.selectedClass);
                if (clsPortrait != null)
                {
                    MakeIcon("Portrait", idCard.transform, clsPortrait,
                        new Vector2(0.03f, 0.10f), new Vector2(0.27f, 0.90f), Vector2.zero, Vector2.zero);
                }

                MakeText("HeroTitle", idCard.transform,
                    $"{p.heroName} — {cls.name.ToUpper()} (Lv.{p.level})",
                    17, FontStyle.Bold, ColGoldBright, TextAnchor.UpperLeft,
                    new Vector2(0.30f, 0.50f), new Vector2(0.97f, 0.92f), Vector2.zero, Vector2.zero);

                MakeText("HeroSub", idCard.transform,
                    $"Vai trò: {cls.roleText}  •  Giáp: {archTag} (+15%)\nEXP: {p.exp:0}/{GameDB.ExpToNext(p.level):0}  •  Vàng: <color=#F8DC85>{p.gold} G</color>  •  Bình máu: {p.potions}",
                    13, FontStyle.Normal, ColParchment, TextAnchor.LowerLeft,
                    new Vector2(0.30f, 0.08f), new Vector2(0.97f, 0.54f), Vector2.zero, Vector2.zero);

                // Detailed Combat Attributes Box
                var attrBox = MakeStyledPanel("AttrTable", colChar.transform, UIPanelStyle.Standard,
                    new Vector2(0.04f, 0.44f), new Vector2(0.96f, 0.78f), Vector2.zero, Vector2.zero);

                MakeText("AttrHeader", attrBox.transform, "CHỈ SỐ CHIẾN ĐẤU & CỘNG THÊM TRANG BỊ", 14, FontStyle.Bold, ColGold, TextAnchor.MiddleLeft,
                    new Vector2(0.04f, 0.83f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

                float eqHp = 0f, eqAtk = 0f, eqDef = 0f, eqCrit = 0f;
                foreach (var eq in p.equipped)
                {
                    if (eq == null) continue;
                    eqHp += eq.hp;
                    eqAtk += eq.atk;
                    eqDef += eq.def;
                    eqCrit += eq.crit;
                }

                float curMaxHp = hero != null ? hero.MaxHp : (cls.hp + (p.level - 1) * 45f + p.bonusHpPts * 35f + eqHp);
                float curMana  = hero != null ? hero.MaxMana : (cls.mana + (p.level - 1) * 10f);
                float curAtk   = hero != null ? hero.Atk : (cls.atk + (p.level - 1) * 6f + p.bonusAtkPts * 5f + eqAtk);
                float curDef   = hero != null ? hero.Def : (cls.def + (p.level - 1) * 3f + p.bonusDefPts * 3.5f + eqDef);
                float curCrit  = hero != null ? hero.CritChance * 100f : ((cls.crit + p.bonusHastePts * 0.015f + eqCrit) * 100f);
                float curAspd  = hero != null ? hero.AtkSpeed : cls.atkSpeed;
                float curMove  = hero != null ? hero.MoveSpeed : cls.moveSpeed;
                float curLife  = hero != null ? hero.Lifesteal * 100f : 0f;
                float curCdr   = hero != null ? hero.CooldownReduction * 100f : 0f;

                string leftStats =
                    $"• Sinh lực (HP): <color=#F8DC85>{curMaxHp:0}</color> <size=11>(+{eqHp:0} đồ)</size>\n" +
                    $"• Năng lượng (MP): <color=#7EC8FF>{curMana:0}</color>\n" +
                    $"• Tấn công (ATK): <color=#F8DC85>{curAtk:0}</color> <size=11>(+{eqAtk:0} đồ)</size>\n" +
                    $"• Phòng thủ (DEF): <color=#F8DC85>{curDef:0}</color> <size=11>(+{eqDef:0} đồ)</size>\n" +
                    $"• Tỷ lệ Chí mạng: <color=#F8DC85>{curCrit:0.0}%</color>";

                string rightStats =
                    $"• Tốc độ đánh: <color=#F8DC85>{curAspd:0.00}x</color>\n" +
                    $"• Tốc độ chạy: <color=#F8DC85>{curMove:0.1f}</color>\n" +
                    $"• Hút máu: <color=#8EE89E>{curLife:0.#}%</color>\n" +
                    $"• Giảm hồi chiêu: <color=#7EC8FF>{curCdr:0.#}%</color>\n" +
                    $"• Nội tại: <color=#D9B057>{cls.passive}</color>";

                MakeText("AttrLeft", attrBox.transform, leftStats, 13, FontStyle.Normal, ColParchment, TextAnchor.UpperLeft,
                    new Vector2(0.04f, 0.06f), new Vector2(0.51f, 0.81f), Vector2.zero, Vector2.zero);
                MakeText("AttrRight", attrBox.transform, rightStats, 13, FontStyle.Normal, ColParchment, TextAnchor.UpperLeft,
                    new Vector2(0.52f, 0.06f), new Vector2(0.97f, 0.81f), Vector2.zero, Vector2.zero);

                // Stat Upgrade Allocation
                MakeText("StatUpHeader", colChar.transform,
                    $"NÂNG CẤP CHỈ SỐ GỐC — ĐIỂM TIỀM NĂNG: <color=#F8DC85>{p.statPoints}</color>",
                    14, FontStyle.Bold, ColGold, TextAnchor.MiddleLeft,
                    new Vector2(0.04f, 0.37f), new Vector2(0.96f, 0.43f), Vector2.zero, Vector2.zero);

                string[] stNames = { $"+35 HP ({p.bonusHpPts})", $"+5 ATK ({p.bonusAtkPts})", $"+3.5 DEF ({p.bonusDefPts})", $"+1.5% Crit ({p.bonusHastePts})" };
                UIButtonStyle stBtnStyle = p.statPoints > 0 ? UIButtonStyle.Primary : UIButtonStyle.Disabled;
                for (int s = 0; s < 4; s++)
                {
                    int idx = s;
                    float x0 = 0.04f + s * 0.232f;
                    var b = MakeStyledButton("AddStat" + s, colChar.transform, stNames[s], 12, stBtnStyle,
                        new Vector2(x0, 0.29f), new Vector2(x0 + 0.22f, 0.365f), Vector2.zero, Vector2.zero, () =>
                        {
                            GameSession.I.SpendStatPoint(idx);
                            hero?.RecalculateStats(false);
                            onChanged?.Invoke();
                            rebuild();
                        });
                    b.interactable = p.statPoints > 0;
                }

                // Build Specialization Selector (3 Builds)
                MakeText("BuildTitle", colChar.transform,
                    "CHUYÊN MÔN HÓA HƯỚNG BUILD (THAY ĐỔI GIÁP & KỸ NĂNG):",
                    14, FontStyle.Bold, ColGold, TextAnchor.MiddleLeft,
                    new Vector2(0.04f, 0.21f), new Vector2(0.96f, 0.27f), Vector2.zero, Vector2.zero);

                for (int b = 0; b < 3; b++)
                {
                    int bi = b;
                    bool isSel = (p.selectedBuild == b);
                    float x0 = 0.04f + b * 0.31f;
                    MakeStyledButton("BuildTab" + b, colChar.transform, cls.builds[b], 12,
                        isSel ? UIButtonStyle.Selected : UIButtonStyle.Tab,
                        new Vector2(x0, 0.13f), new Vector2(x0 + 0.295f, 0.205f), Vector2.zero, Vector2.zero, () =>
                        {
                            p.selectedBuild = bi;
                            GameSession.I.SelectLocalClass(p.selectedClass, bi);
                            hero?.RecalculateStats(false);
                            onChanged?.Invoke();
                            rebuild();
                        });
                }

                var buildDescBox = MakeStyledPanel("BuildDescBox", colChar.transform, UIPanelStyle.Notification,
                    new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.115f), Vector2.zero, Vector2.zero);
                MakeText("BuildDescTxt", buildDescBox.transform,
                    $"• {cls.builds[p.selectedBuild]}: {cls.buildDesc[p.selectedBuild]}",
                    12, FontStyle.Italic, buildAccent, TextAnchor.MiddleLeft,
                    Vector2.zero, Vector2.one, new Vector2(12f, 4f), new Vector2(-12f, -4f));

                // =================================================================
                // COLUMN 2 (CENTER): UI_Panel_SkillTree — ANCIENT RUNE SKILL TREE
                // =================================================================
                var colSkill = MakeStyledPanel("UI_Panel_SkillTree", bodyRoot, UIPanelStyle.SkillTree,
                    new Vector2(0.335f, 0f), new Vector2(0.655f, 1f), Vector2.zero, Vector2.zero);

                // Background Ancient Rune Compass Rose
                Sprite crestSp = GetDecorSprite("Pedestal_Crest_Gold");
                if (crestSp != null)
                {
                    var watermark = MakeIcon("RuneWatermark", colSkill.transform, crestSp,
                        new Vector2(0.18f, 0.22f), new Vector2(0.82f, 0.78f), Vector2.zero, Vector2.zero,
                        new Color(1f, 0.88f, 0.55f, 0.11f));
                    watermark.raycastTarget = false;
                }

                MakeIcon("SkillIconHdr", colSkill.transform, GetIcon("skill_tree"),
                    new Vector2(0.04f, 0.92f), new Vector2(0.11f, 0.975f), Vector2.zero, Vector2.zero);

                MakeText("SkillTreeHdr", colSkill.transform,
                    $"CÂY KỸ NĂNG CỔ NGỮ — ĐIỂM KỸ NĂNG: <color=#F8DC85>{p.skillPoints} SP</color>",
                    15, FontStyle.Bold, ColGoldBright, TextAnchor.MiddleLeft,
                    new Vector2(0.12f, 0.915f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

                // Top Mastery Core Node (Passive + Build Relic)
                var coreNode = MakeStyledPanel("CorePassiveNode", colSkill.transform, UIPanelStyle.Ornate,
                    new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.90f), Vector2.zero, Vector2.zero);
                MakeIcon("CoreIc", coreNode.transform, GetIcon("crown"),
                    new Vector2(0.02f, 0.14f), new Vector2(0.14f, 0.86f), Vector2.zero, Vector2.zero);
                MakeText("CoreTxt", coreNode.transform,
                    $"<color=#F8DC85>[CỔ NGỮ CỘI NGUỒN] {cls.builds[p.selectedBuild]}</color>\n{cls.passive}",
                    13, FontStyle.Bold, ColParchment, TextAnchor.MiddleLeft,
                    new Vector2(0.16f, 0.08f), new Vector2(0.97f, 0.92f), Vector2.zero, Vector2.zero);

                // Vertical Golden Rune Conduit Line connecting nodes
                var conduit = MakeRect("RuneConduit", colSkill.transform,
                    new Vector2(0.105f, 0.08f), new Vector2(0.112f, 0.79f), Vector2.zero, Vector2.zero);
                conduit.gameObject.AddComponent<Image>().color = new Color(ColGold.r, ColGold.g, ColGold.b, 0.45f);

                // 4 Skill Tree Nodes (Active Q, W, E + Ultimate R)
                string[] hotkeys = { "Q", "W", "E", "R • ULTIMATE" };
                for (int k = 0; k < 4; k++)
                {
                    int ki = k;
                    SkillDef sk = cls.skills[k];
                    int rank = (p.skillRanks != null && k < p.skillRanks.Length) ? p.skillRanks[k] : 1;
                    bool isUlt = sk.ultimate || k == 3;
                    float top = 0.76f - k * 0.185f;
                    float bot = top - 0.168f;

                    var node = MakeStyledPanel("SkillNode_" + k, colSkill.transform,
                        isUlt ? UIPanelStyle.Ornate : UIPanelStyle.Standard,
                        new Vector2(0.04f, bot), new Vector2(0.96f, top), Vector2.zero, Vector2.zero);

                    // Skill Icon Medallion Frame
                    var slotFrame = MakeStyledPanel("IconSlot", node.transform, UIPanelStyle.Inventory,
                        new Vector2(0.02f, 0.12f), new Vector2(0.16f, 0.88f), Vector2.zero, Vector2.zero);
                    MakeIcon("SkIc", slotFrame.transform, GetSkillIcon(sk),
                        new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);

                    string typeTag = isUlt ? "<color=#F8DC85>[TUYỆT KỸ TỐI THƯỢNG]</color>" : "<color=#BDA685>[KỸ NĂNG CHỦ ĐỘNG]</color>";
                    float scaleMult = 1f + 0.15f * (rank - 1);
                    MakeText("SkTitle", node.transform,
                        $"[{hotkeys[k]}] {sk.name}  {typeTag}  •  <color=#F8DC85>Cấp {rank}/5</color>",
                        14, FontStyle.Bold, sk.color, TextAnchor.UpperLeft,
                        new Vector2(0.18f, 0.56f), new Vector2(0.75f, 0.92f), Vector2.zero, Vector2.zero);

                    MakeText("SkDesc", node.transform,
                        $"{sk.desc}\n<size=11><color=#BDA685>Hồi chiêu: {sk.cooldown:0.#}s  |  Mana: {sk.mana:0}  |  Uy lực: {(sk.power * scaleMult * 100f):0}% ATK</color></size>",
                        12, FontStyle.Normal, ColParchment, TextAnchor.LowerLeft,
                        new Vector2(0.18f, 0.22f), new Vector2(0.75f, 0.60f), Vector2.zero, Vector2.zero);

                    // Rank Progress Bar (1..5 runes)
                    var progBar = MakeBar("RankBar", node.transform, isUlt ? ColGold : ColAccent,
                        new Vector2(0.18f, 0.07f), new Vector2(0.73f, 0.19f), Vector2.zero, Vector2.zero, 10);
                    SetBarRatio(progBar.fill, rank / 5f);

                    bool canUp = p.skillPoints > 0 && rank < 5;
                    UIButtonStyle upStyle = rank >= 5 ? UIButtonStyle.Selected : (canUp ? UIButtonStyle.Primary : UIButtonStyle.Disabled);
                    string upLbl = rank >= 5 ? "ĐÃ TỐI ĐA" : "NÂNG CẤP\n(+15%)";
                    var upBtn = MakeStyledButton("UpgradeSk" + k, node.transform, upLbl, 12, upStyle,
                        new Vector2(0.76f, 0.14f), new Vector2(0.97f, 0.86f), Vector2.zero, Vector2.zero, () =>
                        {
                            if (GameSession.I.SpendSkillPoint(ki))
                            {
                                GameAudio.I?.PlaySfx(GameSfx.LevelUp, 0.8f);
                                onChanged?.Invoke();
                                rebuild();
                            }
                        });
                    upBtn.interactable = canUp;
                }

                // =================================================================
                // COLUMN 3 (RIGHT): UI_Panel_Inventory — EQUIPMENT, GRID & TOOLTIP
                // =================================================================
                var colInv = MakeStyledPanel("UI_Panel_Inventory", bodyRoot, UIPanelStyle.Inventory,
                    new Vector2(0.67f, 0f), new Vector2(1.00f, 1f), Vector2.zero, Vector2.zero);

                MakeIcon("InvHdrIc", colInv.transform, GetIcon("bag"),
                    new Vector2(0.03f, 0.925f), new Vector2(0.10f, 0.98f), Vector2.zero, Vector2.zero);

                MakeText("InvHdrTxt", colInv.transform,
                    "TRANG BỊ ĐANG MẶC & TÚI ĐỒ CỔ NGỮ",
                    15, FontStyle.Bold, ColGoldBright, TextAnchor.MiddleLeft,
                    new Vector2(0.11f, 0.92f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

                // 5 Equipment Slots (Horizontal Relic Row at Top of Inventory)
                for (int s = 0; s < 5; s++)
                {
                    EquipItem eq = GameSession.I.GetEquipped((ItemSlot)s);
                    float x0 = 0.03f + s * 0.19f;
                    float x1 = x0 + 0.18f;

                    Color rimCol = eq != null ? GameDB.RarityColors[(int)eq.rarity] : ColBronze;
                    var eqCard = MakeStyledPanel("EqSlot_" + s, colInv.transform, UIPanelStyle.Ornate,
                        new Vector2(x0, 0.74f), new Vector2(x1, 0.91f), Vector2.zero, Vector2.zero, null, rimCol);

                    MakeIcon("SlotIc", eqCard.transform, GetSlotIcon((ItemSlot)s),
                        new Vector2(0.22f, 0.42f), new Vector2(0.78f, 0.88f), Vector2.zero, Vector2.zero,
                        eq != null ? Color.white : new Color(1f, 1f, 1f, 0.38f));

                    string eqLabel = eq != null
                        ? $"<color=#{ColorUtility.ToHtmlStringRGB(rimCol)}>{GameDB.SlotNames[s]}</color>\n<size=10>{eq.Summary()}</size>"
                        : $"<color=#BDA685>{GameDB.SlotNames[s]}</color>\n<size=10>(Trống)</size>";

                    MakeText("SlotTxt", eqCard.transform, eqLabel, 11, FontStyle.Bold, ColParchment, TextAnchor.LowerCenter,
                        new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.46f), Vector2.zero, Vector2.zero);

                    var inspectBtn = eqCard.gameObject.AddComponent<Button>();
                    inspectBtn.targetGraphic = eqCard;
                    inspectBtn.onClick.AddListener(() =>
                    {
                        if (eq != null)
                        {
                            inspectedItem = eq;
                            inspectedFromEquipped = true;
                            rebuild();
                        }
                    });
                }

                // Category Filter Tabs
                string[] tabNames = { "Tất Cả", "Vũ Khí", "Giáp & Mũ", "Trang Sức" };
                int[] tabVals = { -1, 0, 1, 2 };
                for (int t = 0; t < 4; t++)
                {
                    int tv = tabVals[t];
                    bool activeTab = (activeBagFilter == tv);
                    float tx0 = 0.03f + t * 0.238f;
                    MakeStyledButton("BagTab_" + t, colInv.transform, tabNames[t], 12,
                        activeTab ? UIButtonStyle.Selected : UIButtonStyle.Tab,
                        new Vector2(tx0, 0.675f), new Vector2(tx0 + 0.228f, 0.73f), Vector2.zero, Vector2.zero, () =>
                        {
                            activeBagFilter = tv;
                            rebuild();
                        });
                }

                // Filtered Bag Items List
                List<EquipItem> filtered = new List<EquipItem>();
                foreach (var it in p.bag)
                {
                    if (it == null) continue;
                    if (activeBagFilter == 0 && it.slot != ItemSlot.Weapon) continue;
                    if (activeBagFilter == 1 && it.slot != ItemSlot.Armor && it.slot != ItemSlot.Helmet) continue;
                    if (activeBagFilter == 2 && it.slot != ItemSlot.Ring && it.slot != ItemSlot.Amulet) continue;
                    filtered.Add(it);
                }

                var gridBox = MakeStyledPanel("BagGridBox", colInv.transform, UIPanelStyle.Standard,
                    new Vector2(0.03f, 0.26f), new Vector2(0.97f, 0.665f), Vector2.zero, Vector2.zero);

                if (filtered.Count == 0)
                {
                    MakeText("EmptyBag", gridBox.transform,
                        "Chưa có vật phẩm thuộc nhóm này trong túi đồ.\nHãy tiêu diệt quái vật hoặc rèn đồ tại Thị Trấn!",
                        13, FontStyle.Italic, ColMuted, TextAnchor.MiddleCenter,
                        Vector2.zero, Vector2.one, new Vector2(16f, 16f), new Vector2(-16f, -16f));
                }
                else
                {
                    int maxRows = Mathf.Min(5, filtered.Count);
                    for (int i = 0; i < maxRows; i++)
                    {
                        EquipItem item = filtered[i];
                        if (inspectedItem == null && i == 0)
                        {
                            inspectedItem = item;
                            inspectedFromEquipped = false;
                        }

                        float rTop = 0.97f - i * 0.19f;
                        float rBot = rTop - 0.175f;
                        Color rCol = GameDB.RarityColors[(int)item.rarity];
                        bool isInspected = (inspectedItem == item);

                        var row = MakeStyledPanel("BagItem_" + i, gridBox.transform,
                            isInspected ? UIPanelStyle.Ornate : UIPanelStyle.Standard,
                            new Vector2(0.02f, rBot), new Vector2(0.98f, rTop), Vector2.zero, Vector2.zero, null, rCol);

                        MakeIcon("ItIc", row.transform, GetSlotIcon(item.slot),
                            new Vector2(0.015f, 0.12f), new Vector2(0.11f, 0.88f), Vector2.zero, Vector2.zero);

                        MakeText("ItTxt", row.transform,
                            $"[{GameDB.RarityNames[(int)item.rarity]}] {item.name} <size=11>({GameDB.SlotNames[(int)item.slot]})</size>\n<size=11><color=#EDE0BA>{item.Summary()}</color></size>",
                            12, FontStyle.Bold, rCol, TextAnchor.MiddleLeft,
                            new Vector2(0.125f, 0.06f), new Vector2(0.62f, 0.94f), Vector2.zero, Vector2.zero);

                        MakeStyledButton("Inspect_" + i, row.transform, "Chi Tiết", 11, UIButtonStyle.Secondary,
                            new Vector2(0.63f, 0.14f), new Vector2(0.76f, 0.86f), Vector2.zero, Vector2.zero, () =>
                            {
                                inspectedItem = item;
                                inspectedFromEquipped = false;
                                rebuild();
                            });

                        MakeStyledButton("Equip_" + i, row.transform, "Mặc", 11, UIButtonStyle.Primary,
                            new Vector2(0.77f, 0.14f), new Vector2(0.88f, 0.86f), Vector2.zero, Vector2.zero, () =>
                            {
                                GameSession.I.Equip(item);
                                inspectedItem = item;
                                inspectedFromEquipped = true;
                                hero?.RecalculateStats(false);
                                onChanged?.Invoke();
                                rebuild();
                            });

                        int sellGold = Mathf.Max(5, item.value > 0 ? item.value : 12 * ((int)item.rarity + 1));
                        MakeStyledButton("Sell_" + i, row.transform, $"Bán\n+{sellGold}G", 10, UIButtonStyle.Danger,
                            new Vector2(0.89f, 0.14f), new Vector2(0.985f, 0.86f), Vector2.zero, Vector2.zero, () =>
                            {
                                if (inspectedItem == item) inspectedItem = null;
                                GameSession.I.SellBagItem(item);
                                onChanged?.Invoke();
                                rebuild();
                            });
                    }
                }

                // Bottom Item Tooltip & Comparison Panel (UI_Panel_Dialog)
                var tooltipBox = MakeStyledPanel("ItemTooltipPanel", colInv.transform, UIPanelStyle.Dialog,
                    new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.245f), Vector2.zero, Vector2.zero);

                if (inspectedItem == null && p.equipped != null && p.equipped.Count > 0)
                {
                    inspectedItem = p.equipped[0];
                    inspectedFromEquipped = true;
                }

                if (inspectedItem == null)
                {
                    MakeText("NoTooltip", tooltipBox.transform,
                        "CHỌN MỘT TRANG BỊ ĐỂ XEM CHI TIẾT & SO SÁNH CHỈ SỐ",
                        13, FontStyle.Italic, ColMuted, TextAnchor.MiddleCenter,
                        Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                }
                else
                {
                    Color rc = GameDB.RarityColors[(int)inspectedItem.rarity];
                    EquipItem curEq = GameSession.I.GetEquipped(inspectedItem.slot);

                    MakeIcon("TtIc", tooltipBox.transform, GetSlotIcon(inspectedItem.slot),
                        new Vector2(0.02f, 0.18f), new Vector2(0.15f, 0.82f), Vector2.zero, Vector2.zero);

                    string cmpLine = "";
                    if (!inspectedFromEquipped && curEq != null && curEq != inspectedItem)
                    {
                        float dAtk = inspectedItem.atk - curEq.atk;
                        float dHp  = inspectedItem.hp - curEq.hp;
                        float dDef = inspectedItem.def - curEq.def;
                        string cAtk = dAtk >= 0 ? $"<color=#68E878>+{dAtk:0} ATK</color>" : $"<color=#F06868>{dAtk:0} ATK</color>";
                        string cHp  = dHp >= 0  ? $"<color=#68E878>+{dHp:0} HP</color>"  : $"<color=#F06868>{dHp:0} HP</color>";
                        string cDef = dDef >= 0 ? $"<color=#68E878>+{dDef:0} DEF</color>" : $"<color=#F06868>{dDef:0} DEF</color>";
                        cmpLine = $"\nSo với đồ đang mặc ({curEq.name}): {cAtk}  |  {cHp}  |  {cDef}";
                    }
                    else if (inspectedFromEquipped)
                    {
                        cmpLine = "\n<color=#F8DC85>• Trang bị này đang được mặc trên người.</color>";
                    }

                    MakeText("TtBody", tooltipBox.transform,
                        $"[{GameDB.RarityNames[(int)inspectedItem.rarity].ToUpper()}] {inspectedItem.name} — Vị trí: {GameDB.SlotNames[(int)inspectedItem.slot]}\n" +
                        $"<size=12><color=#EDE0BA>Chỉ số: {inspectedItem.Summary()}</color>{cmpLine}</size>",
                        13, FontStyle.Bold, rc, TextAnchor.MiddleLeft,
                        new Vector2(0.17f, 0.08f), new Vector2(0.98f, 0.92f), Vector2.zero, Vector2.zero);
                }
            };

            rebuild();
            modal.gameObject.SetActive(false);
            return modal.gameObject;
        }

        public static GameObject BuildSettingsModal(Transform canvasRoot, bool includeResetSave = false)
        {
            var img = MakeStyledPanel("SettingsModal", canvasRoot, UIPanelStyle.Dialog,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-640f, -420f), new Vector2(640f, 420f));
            var modal = img.gameObject;

            MakeIcon("SetIc", img.transform, GetIcon("settings"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -58f), new Vector2(66f, -20f));

            MakeText("Title", img.transform, "CỔ ĐÀI THIẾT LẬP ĐỒ HỌA THÍCH ỨNG & ÂM THANH (URP 12.1.7)", 22, FontStyle.Bold, ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(76f, -62f), new Vector2(-24f, -14f));

            MakeGothicDivider("SetDiv", img.transform,
                new Vector2(0.025f, 1f), new Vector2(0.975f, 1f), new Vector2(0f, -72f), new Vector2(0f, -54f));

            var content = MakeRect("SettingsContent", img.transform, Vector2.zero, Vector2.one, new Vector2(24f, 82f), new Vector2(-24f, -74f));

            Text telemetryTxt = null;
            Action rebuildUI = null;

            rebuildUI = () =>
            {
                for (int i = content.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.Destroy(content.GetChild(i).gameObject);

                var gm = GraphicsManager.Instance;
                var hw = gm != null ? gm.HardwareReport : GraphicsDetector.AnalyzeHardware();
                var s = gm != null && gm.SettingsData != null ? gm.SettingsData : new UserGraphicsSettingsData();
                var curTier = gm != null ? gm.CurrentTier : hw.staticRecommendedTier;

                // 1. Hardware & Real-Time Performance Monitor Plaque (Top: y = 0.81 .. 1.00)
                var hwBox = MakeStyledPanel("HardwarePlaque", content, UIPanelStyle.Character,
                    new Vector2(0f, 0.81f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

                string memType = hw.isIntegratedOrSharedMemory
                    ? $"RAM dùng chung (Báo cáo: {hw.reportedVramMB} MB • Ngân sách an toàn: ~{hw.estimatedDedicatedVramMB} MB)"
                    : $"VRAM rời: {hw.reportedVramMB} MB (RAM hệ thống: {hw.systemRamMB} MB)";

                telemetryTxt = MakeText("HwInfo", hwBox.transform,
                    $"<color=#F8DC85>PHẦN CỨNG:</color> {hw.gpuName} ({hw.graphicsApi}) • CPU: {hw.cpuCores} nhân @ {hw.cpuFrequencyMHz} MHz\n" +
                    $"• Bộ nhớ: <color=#EDE0BA>{memType}</color>\n" +
                    $"• Khuyến nghị Tự động: <color=#78E888>{hw.staticRecommendedTier.ToString().ToUpper()}</color> — {hw.recommendationSummary}",
                    12, FontStyle.Normal, ColParchment, TextAnchor.MiddleLeft,
                    new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f), Vector2.zero, Vector2.zero);

                // 2. Control #1: Graphics Preset (Low / Medium / High / Ultra / Cinematic) + Auto-Detect (y = 0.66 .. 0.79)
                MakeText("PresetHdr", content,
                    $"1. CẤU HÌNH TỔNG THỂ (PRESET) — {(s.hasUserManualSelection ? "<color=#F8DC85>Chế độ Thủ công</color>" : "<color=#78E888>Chế độ Khuyến nghị</color>")} (★ = Khuyến nghị cho máy bạn):",
                    13, FontStyle.Bold, ColGoldBright, TextAnchor.MiddleLeft,
                    new Vector2(0f, 0.735f), new Vector2(0.72f, 0.80f), Vector2.zero, Vector2.zero);

                MakeStyledButton("BtnAutoDetect", content, "TỰ ĐỘNG ĐO & KHUYẾN NGHỊ", 12, UIButtonStyle.Primary,
                    new Vector2(0.73f, 0.735f), new Vector2(1f, 0.80f), Vector2.zero, Vector2.zero, () =>
                    {
                        gm?.ApplyAutoRecommendedPreset((_, __) =>
                        {
                            if (modal != null && modal.activeInHierarchy) rebuildUI();
                        });
                        rebuildUI();
                    });

                GraphicsQualityTier[] tiers = (GraphicsQualityTier[])Enum.GetValues(typeof(GraphicsQualityTier));
                for (int i = 0; i < tiers.Length; i++)
                {
                    GraphicsQualityTier t = tiers[i];
                    float x0 = i * 0.202f;
                    float x1 = Mathf.Min(1f, x0 + 0.192f);
                    bool isSel = (t == curTier);
                    bool isRec = (t == hw.staticRecommendedTier);
                    string recBadge = isRec ? " ★" : "";
                    UIButtonStyle bStyle = isSel ? UIButtonStyle.Selected : UIButtonStyle.Secondary;

                    MakeStyledButton("Tier_" + t, content, $"{t.ToString().ToUpper()}{recBadge}", 13, bStyle,
                        new Vector2(x0, 0.64f), new Vector2(x1, 0.725f), Vector2.zero, Vector2.zero, () =>
                        {
                            gm?.SetQualityTier(t, isUserManualSelection: true, saveImmediately: true);
                            rebuildUI();
                        });
                }

                // 3. 2-Column Grid of Granular Graphics Controls (#2 .. #14) + Adaptive Controller (y = 0.13 .. 0.62)
                var gridBox = MakeStyledPanel("GranularGridBox", content, UIPanelStyle.Standard,
                    new Vector2(0f, 0.13f), new Vector2(1f, 0.625f), Vector2.zero, Vector2.zero);

                // Helper to create a Gothic Option Row inside the 2-column x 7-row grid
                void MakeOptionRow(int col, int row, string label, string valueText, Action onPrev, Action onNext)
                {
                    float colX0 = col == 0 ? 0.018f : 0.510f;
                    float colX1 = col == 0 ? 0.490f : 0.982f;
                    float rowH = 1f / 7.2f;
                    float y1 = 0.97f - row * rowH;
                    float y0 = y1 - rowH + 0.015f;

                    var rowRt = MakeRect($"Opt_{col}_{row}", gridBox.transform,
                        new Vector2(colX0, y0), new Vector2(colX1, y1), Vector2.zero, Vector2.zero);

                    MakeText("Lbl", rowRt, label, 12, FontStyle.Bold, ColParchment, TextAnchor.MiddleLeft,
                        new Vector2(0.0f, 0f), new Vector2(0.47f, 1f), Vector2.zero, Vector2.zero);

                    MakeStyledButton("Prev", rowRt, "◀", 12, UIButtonStyle.Secondary,
                        new Vector2(0.48f, 0.06f), new Vector2(0.57f, 0.94f), Vector2.zero, Vector2.zero, () =>
                        {
                            onPrev?.Invoke();
                            rebuildUI();
                        });

                    var valPanel = MakeStyledPanel("ValBox", rowRt, UIPanelStyle.Inventory,
                        new Vector2(0.58f, 0.06f), new Vector2(0.89f, 0.94f), Vector2.zero, Vector2.zero);
                    MakeText("ValTxt", valPanel.transform, valueText, 12, FontStyle.Bold, ColGoldBright, TextAnchor.MiddleCenter,
                        Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

                    MakeStyledButton("Next", rowRt, "▶", 12, UIButtonStyle.Secondary,
                        new Vector2(0.90f, 0.06f), new Vector2(0.99f, 0.94f), Vector2.zero, Vector2.zero, () =>
                        {
                            onNext?.Invoke();
                            rebuildUI();
                        });
                }

                // Control #2: Resolution
                Resolution[] resList = gm != null ? gm.GetAvailableResolutions() : new[] { new Resolution { width = Screen.width, height = Screen.height, refreshRate = 60 } };
                int curResIdx = gm != null ? gm.GetCurrentResolutionIndex() : 0;
                string resLabel = $"{s.resolutionWidth} × {s.resolutionHeight}";
                MakeOptionRow(0, 0, "2. Độ phân giải (Resolution):", resLabel,
                    () =>
                    {
                        int next = (curResIdx - 1 + resList.Length) % resList.Length;
                        gm?.SetResolution(resList[next].width, resList[next].height, 60);
                    },
                    () =>
                    {
                        int next = (curResIdx + 1) % resList.Length;
                        gm?.SetResolution(resList[next].width, resList[next].height, 60);
                    });

                // Control #3: Display Mode (Fullscreen / Borderless / Windowed)
                string[] dispNames = { "Toàn màn hình (Full)", "Không viền (Borderless)", "Cửa sổ (Windowed)" };
                int dispIdx = Mathf.Clamp(s.displayMode, 0, 2);
                MakeOptionRow(0, 1, "3. Chế độ Hiển thị:", dispNames[dispIdx],
                    () => gm?.SetDisplayMode((DisplayModeOption)((dispIdx + 2) % 3)),
                    () => gm?.SetDisplayMode((DisplayModeOption)((dispIdx + 1) % 3)));

                // Control #4: FPS Limit (30 / 60 / 90 / 120 / Unlimited)
                int[] fpsChoices = { 30, 60, 90, 120, -1 };
                int fpsIdx = Array.IndexOf(fpsChoices, s.targetFrameRate);
                if (fpsIdx < 0) fpsIdx = 1;
                string fpsText = s.targetFrameRate <= 0 ? "Không giới hạn" : $"{s.targetFrameRate} FPS";
                MakeOptionRow(0, 2, "4. Giới hạn FPS:", fpsText,
                    () => gm?.SetFpsLimit(fpsChoices[(fpsIdx + fpsChoices.Length - 1) % fpsChoices.Length]),
                    () => gm?.SetFpsLimit(fpsChoices[(fpsIdx + 1) % fpsChoices.Length]));

                // Control #5: VSync
                MakeOptionRow(0, 3, "5. Đồng bộ dọc (VSync):", s.vSyncEnabled ? "<color=#78E888>BẬT</color>" : "<color=#BDA685>TẮT</color>",
                    () => gm?.SetVSync(!s.vSyncEnabled),
                    () => gm?.SetVSync(!s.vSyncEnabled));

                // Control #6: Render Scale (50% .. 125%)
                int scalePct = Mathf.RoundToInt(s.renderScale * 100f);
                MakeOptionRow(0, 4, "6. Tỷ lệ Render (Render Scale):", $"{scalePct}%",
                    () => gm?.SetRenderScale(Mathf.Clamp(s.renderScale - 0.05f, 0.50f, 1.25f), false),
                    () => gm?.SetRenderScale(Mathf.Clamp(s.renderScale + 0.05f, 0.50f, 1.25f), false));

                // Control #7: Shadow Quality
                string[] shNames = { "Tắt (Off)", "Thấp (Low)", "Trung bình (Med)", "Cao (High)", "Cực cao (Ultra)" };
                int shIdx = Mathf.Clamp(s.shadowQuality, 0, 4);
                MakeOptionRow(0, 5, "7. Chất lượng Bóng đổ:", shNames[shIdx],
                    () => gm?.SetShadowQuality((ShadowQualityOption)((shIdx + 4) % 5), false),
                    () => gm?.SetShadowQuality((ShadowQualityOption)((shIdx + 1) % 5), false));

                // Control #8: Texture Quality
                string[] texNames = { "Thấp (1/4 VRAM)", "Trung bình (1/2)", "Nguyên bản (Full)" };
                int texIdx = Mathf.Clamp(s.textureQuality, 0, 2);
                MakeOptionRow(0, 6, "8. Độ nét Vân bề mặt (Texture):", texNames[texIdx],
                    () => gm?.SetTextureQuality((TextureQualityOption)((texIdx + 2) % 3), false),
                    () => gm?.SetTextureQuality((TextureQualityOption)((texIdx + 1) % 3), false));

                // Right Column (#9 .. #14 + Adaptive Quality toggle)
                // Control #9: Anti-Aliasing
                string[] aaNames = { "Tắt (Off)", "FXAA (Nhẹ)", "SMAA", "MSAA 2x + SMAA", "MSAA 4x + SMAA" };
                int aaIdx = Mathf.Clamp(s.antiAliasing, 0, 4);
                MakeOptionRow(1, 0, "9. Khử răng cưa (Anti-Aliasing):", aaNames[aaIdx],
                    () => gm?.SetAntiAliasing((AntiAliasingOption)((aaIdx + 4) % 5), false),
                    () => gm?.SetAntiAliasing((AntiAliasingOption)((aaIdx + 1) % 5), false));

                // Control #10: Ambient Occlusion (SSAO)
                string[] aoNames = { "Tắt (Off)", "Bán phần (Half-Res)", "Toàn phần (Full-Res)" };
                int aoIdx = Mathf.Clamp(s.ambientOcclusion, 0, 2);
                MakeOptionRow(1, 1, "10. Bóng tiếp xúc (SSAO):", aoNames[aoIdx],
                    () => gm?.SetAmbientOcclusion((AmbientOcclusionOption)((aoIdx + 2) % 3), false),
                    () => gm?.SetAmbientOcclusion((AmbientOcclusionOption)((aoIdx + 1) % 3), false));

                // Control #11: Bloom
                string[] blNames = { "Tắt (Off)", "Nhẹ (Low)", "Trung bình (Med)", "Rực rỡ (High)" };
                int blIdx = Mathf.Clamp(s.bloomQuality, 0, 3);
                MakeOptionRow(1, 2, "11. Hiệu ứng Phát sáng (Bloom):", blNames[blIdx],
                    () => gm?.SetBloom((BloomQualityOption)((blIdx + 3) % 4), false),
                    () => gm?.SetBloom((BloomQualityOption)((blIdx + 1) % 4), false));

                // Control #12: VFX Quality
                string[] vfxNames = { "Thấp (Low)", "Trung bình (Med)", "Cao (High)", "Cực cao (Ultra)", "Điện ảnh (Cinematic)" };
                int vfxIdx = Mathf.Clamp(s.vfxQuality, 0, 4);
                MakeOptionRow(1, 3, "12. Chất lượng Kỹ năng (VFX):", vfxNames[vfxIdx],
                    () => gm?.SetVfxQuality((VfxQualityOption)((vfxIdx + 4) % 5), false),
                    () => gm?.SetVfxQuality((VfxQualityOption)((vfxIdx + 1) % 5), false));

                // Control #13: View Distance
                string[] vdNames = { "Gần (Near 38m)", "Trung bình (52m)", "Xa (Far 72m)", "Rất xa (Ultra 100m)" };
                int vdIdx = Mathf.Clamp(s.viewDistance, 0, 3);
                MakeOptionRow(1, 4, "13. Tầm nhìn (View Distance):", vdNames[vdIdx],
                    () => gm?.SetViewDistance((ViewDistanceOption)((vdIdx + 3) % 4), false),
                    () => gm?.SetViewDistance((ViewDistanceOption)((vdIdx + 1) % 4), false));

                // Control #14: Effects Density
                int densPct = Mathf.RoundToInt(s.effectsDensity * 100f);
                MakeOptionRow(1, 5, "14. Mật độ Hạt (Effects Density):", $"{densPct}%",
                    () => gm?.SetEffectsDensity(Mathf.Clamp(s.effectsDensity - 0.10f, 0.35f, 1.35f), false),
                    () => gm?.SetEffectsDensity(Mathf.Clamp(s.effectsDensity + 0.10f, 0.35f, 1.35f), false));

                // Adaptive Quality Controller Toggle (Row 6 of Col 1)
                string adapLabel = s.adaptiveResolutionEnabled
                    ? (s.adaptiveQualityDowngradeEnabled ? "<color=#78E888>Scale + Hạ Tier</color>" : "<color=#78E888>BẬT (Scale/VFX)</color>")
                    : "<color=#BDA685>TẮT</color>";
                MakeOptionRow(1, 6, "Thích ứng FPS (Adaptive Quality):", adapLabel,
                    () =>
                    {
                        if (!s.adaptiveResolutionEnabled) { gm?.SetAdaptiveResolution(true); gm?.SetAdaptiveQualityDowngrade(false); }
                        else if (!s.adaptiveQualityDowngradeEnabled) { gm?.SetAdaptiveQualityDowngrade(true); }
                        else { gm?.SetAdaptiveResolution(false); gm?.SetAdaptiveQualityDowngrade(false); }
                    },
                    () =>
                    {
                        if (!s.adaptiveResolutionEnabled) { gm?.SetAdaptiveResolution(true); gm?.SetAdaptiveQualityDowngrade(false); }
                        else if (!s.adaptiveQualityDowngradeEnabled) { gm?.SetAdaptiveQualityDowngrade(true); }
                        else { gm?.SetAdaptiveResolution(false); gm?.SetAdaptiveQualityDowngrade(false); }
                    });

                // 4. Bottom Action Bar: Audio Volume + #15 Apply + #16 Restore Defaults (+ Optional Reset Save) (y = 0.01 .. 0.11)
                var p = GameSession.I != null ? GameSession.I.Profile : null;
                float curVol = p != null ? p.masterVol : 0.8f;
                Text volLbl = MakeText("VolLbl", content, $"Âm lượng: <color=#F8DC85>{Mathf.RoundToInt(curVol * 100)}%</color>",
                    13, FontStyle.Bold, ColParchment, TextAnchor.MiddleLeft,
                    new Vector2(0.00f, 0.01f), new Vector2(0.15f, 0.11f), Vector2.zero, Vector2.zero);

                MakeStyledButton("VolDown", content, "-10% Âm", 12, UIButtonStyle.Secondary,
                    new Vector2(0.155f, 0.01f), new Vector2(0.255f, 0.11f), Vector2.zero, Vector2.zero, () =>
                    {
                        if (p == null) return;
                        p.masterVol = Mathf.Clamp01(p.masterVol - 0.1f);
                        GameAudio.I?.ApplyVolumes();
                        GameSession.I?.SaveProfile();
                        volLbl.text = $"Âm lượng: <color=#F8DC85>{Mathf.RoundToInt(p.masterVol * 100)}%</color>";
                    });

                MakeStyledButton("VolUp", content, "+10% Âm", 12, UIButtonStyle.Secondary,
                    new Vector2(0.265f, 0.01f), new Vector2(0.365f, 0.11f), Vector2.zero, Vector2.zero, () =>
                    {
                        if (p == null) return;
                        p.masterVol = Mathf.Clamp01(p.masterVol + 0.1f);
                        GameAudio.I?.ApplyVolumes();
                        GameSession.I?.SaveProfile();
                        volLbl.text = $"Âm lượng: <color=#F8DC85>{Mathf.RoundToInt(p.masterVol * 100)}%</color>";
                    });

                // Control #15: Apply Settings
                MakeStyledButton("BtnApplySettings", content, "15. ÁP DỤNG (APPLY)", 13, UIButtonStyle.Primary,
                    new Vector2(0.385f, 0.01f), new Vector2(0.585f, 0.11f), Vector2.zero, Vector2.zero, () =>
                    {
                        gm?.ApplyAndSave();
                        rebuildUI();
                    }, GetIcon("check_seal"));

                // Control #16: Restore Defaults
                MakeStyledButton("BtnRestoreDefaults", content, "16. MẶC ĐỊNH (DEFAULT)", 12, UIButtonStyle.Secondary,
                    new Vector2(0.595f, 0.01f), new Vector2(0.805f, 0.11f), Vector2.zero, Vector2.zero, () =>
                    {
                        gm?.RestoreDefaultSettings();
                        rebuildUI();
                    });

                if (includeResetSave)
                {
                    MakeStyledButton("ResetSave", content, "XÓA HỒ SƠ (LV.1)", 12, UIButtonStyle.Danger,
                        new Vector2(0.815f, 0.01f), new Vector2(1.00f, 0.11f), Vector2.zero, Vector2.zero, () =>
                        {
                            PlayerPrefs.DeleteKey("AbyssalDungeon3D_Profile_v2");
                            AtomicSaveSystem.DeleteAtomicSaves();
                            if (GameSession.I != null) UnityEngine.Object.Destroy(GameSession.I.gameObject);
                            GameSession.GoToScene("MainMenu");
                        });
                }
            };

            rebuildUI();

            var updater = modal.AddComponent<SettingsModalTelemetryBehaviour>();
            updater.GetTelemetryText = () => telemetryTxt;

            MakeStyledButton("Close", img.transform, "LƯU & ĐÓNG CỔ ĐÀI", 15, UIButtonStyle.Primary,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-160f, 16f), new Vector2(160f, 64f),
                () =>
                {
                    GraphicsManager.Instance?.ApplyAndSave();
                    modal.SetActive(false);
                }, GetIcon("check_seal"));

            modal.SetActive(false);
            return modal;
        }

        private class SettingsModalTelemetryBehaviour : MonoBehaviour
        {
            public Func<Text> GetTelemetryText;
            private float _timer;

            private void Update()
            {
                _timer -= Time.unscaledDeltaTime;
                if (_timer > 0f) return;
                _timer = 0.35f;

                Text txt = GetTelemetryText?.Invoke();
                var gm = GraphicsManager.Instance;
                if (txt == null || gm == null) return;

                var hw = gm.HardwareReport;
                var pm = gm.PerfMonitor;
                var aq = gm.AdaptiveController;
                float fps = pm != null ? pm.SmoothedFps : (1f / Mathf.Max(0.001f, Time.unscaledDeltaTime));
                float low1 = pm != null ? pm.OnePercentLowFps : fps;
                float ms = pm != null ? pm.SmoothedCpuFrameTimeMs : (Time.unscaledDeltaTime * 1000f);
                float rScale = aq != null ? aq.CurrentRenderScale : (gm.SettingsData != null ? gm.SettingsData.renderScale : 1f);
                long draws = pm != null ? pm.DrawCallsCount : 0;
                long batches = pm != null ? pm.BatchesCount : 0;

                string memType = hw.isIntegratedOrSharedMemory
                    ? $"RAM dùng chung ({hw.reportedVramMB} MB • An toàn: ~{hw.estimatedDedicatedVramMB} MB)"
                    : $"VRAM rời: {hw.reportedVramMB} MB (RAM: {hw.systemRamMB} MB)";

                txt.text =
                    $"<color=#F8DC85>PHẦN CỨNG:</color> {hw.gpuName} ({hw.graphicsApi})  |  <color=#78E888>FPS: {fps:0} (1% Low: {low1:0} • {ms:0.1} ms) • Scale: {Mathf.RoundToInt(rScale * 100)}% • DrawCalls: {draws} (Batches: {batches})</color>\n" +
                    $"• Bộ nhớ Đồ họa: <color=#EDE0BA>{memType}</color> • Đang cấp phát: <color=#EDE0BA>{(pm != null ? pm.TotalAllocatedMemoryMB : 0f):0} MB</color>\n" +
                    $"• Khuyến nghị: <color=#78E888>{hw.staticRecommendedTier.ToString().ToUpper()}</color> — {(aq != null ? aq.StatusSummary : hw.recommendationSummary)}";
            }
        }
    }
}

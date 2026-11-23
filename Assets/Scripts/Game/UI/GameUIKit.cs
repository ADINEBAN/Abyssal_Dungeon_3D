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

        private static Sprite[] _itemSheetSprites;

        public static Sprite GetItemSheetSprite(int index)
        {
            index = Mathf.Clamp(index, 0, 15);
            if (_itemSheetSprites == null || _itemSheetSprites.Length < 16 || _itemSheetSprites[0] == null)
            {
                _itemSheetSprites = new Sprite[16];
                Sprite[] loaded = Resources.LoadAll<Sprite>("UI/Icons/ItemIcons_Sheet");
                if (loaded != null && loaded.Length >= 16)
                {
                    for (int i = 0; i < loaded.Length; i++)
                    {
                        if (loaded[i] == null) continue;
                        string n = loaded[i].name;
                        if (n.StartsWith("ItemIcon_") && int.TryParse(n.Substring("ItemIcon_".Length), out int parsed) && parsed >= 0 && parsed < 16)
                            _itemSheetSprites[parsed] = loaded[i];
                        else if (i < 16 && _itemSheetSprites[i] == null)
                            _itemSheetSprites[i] = loaded[i];
                    }
                }

                // Runtime fallback slice if TextureImporter sub-sprites were not populated yet
                if (_itemSheetSprites[0] == null)
                {
                    Texture2D sheetTex = Resources.Load<Texture2D>("UI/Icons/ItemIcons_Sheet");
                    if (sheetTex != null)
                    {
                        int w = sheetTex.width, h = sheetTex.height;
                        float marginX = w * 0.031f, marginY = h * 0.031f;
                        float cellW = (w - marginX * 2f) / 4f;
                        float cellH = (h - marginY * 2f) / 4f;
                        float inset = cellW * 0.045f;
                        int idx = 0;
                        for (int row = 0; row < 4; row++)
                        {
                            int ty = 3 - row;
                            for (int col = 0; col < 4; col++)
                            {
                                Rect r = new Rect(
                                    Mathf.Round(marginX + col * cellW + inset),
                                    Mathf.Round(marginY + ty * cellH + inset),
                                    Mathf.Round(cellW - inset * 2f),
                                    Mathf.Round(cellH - inset * 2f));
                                _itemSheetSprites[idx] = Sprite.Create(sheetTex, r, new Vector2(0.5f, 0.5f), 100f);
                                idx++;
                            }
                        }
                    }
                }
            }
            return _itemSheetSprites[index] ?? GetIcon("sword");
        }

        public static Sprite GetItemSprite(EquipItem item)
        {
            if (item == null) return GetItemSheetSprite(1);
            string nm = item.name ?? "";
            switch (item.slot)
            {
                case ItemSlot.Weapon:
                    if (nm.Contains("Cung")) return GetItemSheetSprite(2);
                    if (nm.Contains("Đoản") || nm.Contains("Quyền") || item.archetype == EquipArchetype.StalkerLeather)
                        return GetItemSheetSprite(0);
                    if (nm.Contains("Trượng") || nm.Contains("Chùy") || item.archetype == EquipArchetype.ArcanistCloth || item.archetype == EquipArchetype.SacredRelic)
                        return GetItemSheetSprite(3);
                    return GetItemSheetSprite(1);

                case ItemSlot.Helmet:
                    switch (item.archetype)
                    {
                        case EquipArchetype.VanguardPlate:  return GetItemSheetSprite(4);
                        case EquipArchetype.StalkerLeather: return GetItemSheetSprite(5);
                        case EquipArchetype.ArcanistCloth:  return GetItemSheetSprite(6);
                        case EquipArchetype.SacredRelic:    return GetItemSheetSprite(7);
                        default: return GetItemSheetSprite(4);
                    }

                case ItemSlot.Armor:
                    switch (item.archetype)
                    {
                        case EquipArchetype.VanguardPlate:  return GetItemSheetSprite(8);
                        case EquipArchetype.StalkerLeather: return GetItemSheetSprite(9);
                        case EquipArchetype.ArcanistCloth:  return GetItemSheetSprite(10);
                        case EquipArchetype.SacredRelic:    return GetItemSheetSprite(11);
                        default: return GetItemSheetSprite(8);
                    }

                case ItemSlot.Ring:
                    return GetItemSheetSprite(12);

                case ItemSlot.Amulet:
                    return GetItemSheetSprite(13);

                case ItemSlot.Relic:
                    return GetItemSheetSprite(14);

                default:
                    return GetItemSheetSprite(1);
            }
        }

        public static Sprite GetSlotIcon(ItemSlot slot)
        {
            switch (slot)
            {
                case ItemSlot.Weapon: return GetItemSheetSprite(1);
                case ItemSlot.Armor:  return GetItemSheetSprite(8);
                case ItemSlot.Helmet: return GetItemSheetSprite(4);
                case ItemSlot.Ring:   return GetItemSheetSprite(12);
                case ItemSlot.Amulet: return GetItemSheetSprite(13);
                case ItemSlot.Relic:  return GetItemSheetSprite(14);
                default:              return GetItemSheetSprite(1);
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

        public static Canvas CreateCanvas(string name = "GameCanvas", int sortOrder = 10, bool interactive = true)
        {
            EnsureEventSystem();
            var go = new GameObject(name);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = sortOrder;
            c.pixelPerfect = false;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (interactive)
                go.AddComponent<GraphicRaycaster>();
            return c;
        }

        public static Canvas GetModalCanvas()
        {
            var modalGo = GameObject.Find("DungeonModalsCanvas");
            if (modalGo != null && modalGo.TryGetComponent<Canvas>(out var modalCanvas))
                return modalCanvas;
            var raycaster = UnityEngine.Object.FindObjectOfType<GraphicRaycaster>();
            if (raycaster != null && raycaster.TryGetComponent<Canvas>(out var rayCanvas))
                return rayCanvas;
            return UnityEngine.Object.FindObjectOfType<Canvas>();
        }

        /// <summary>
        /// Disables Pixel Perfect and strips redundant raycastTarget flags on all non-interactive
        /// Images/Texts/RawImages/TMPro labels inside a Canvas hierarchy to eliminate per-frame raycast cost.
        /// </summary>
        public static void OptimizeCanvasHierarchy(Canvas rootCanvas)
        {
            if (rootCanvas == null) return;
            rootCanvas.pixelPerfect = false;
            foreach (var nested in rootCanvas.GetComponentsInChildren<Canvas>(true))
                nested.pixelPerfect = false;

            var interactiveGraphics = new HashSet<Graphic>();
            foreach (var sel in rootCanvas.GetComponentsInChildren<Selectable>(true))
            {
                if (sel == null) continue;
                var ownGraphic = sel.GetComponent<Graphic>();
                if (ownGraphic != null) interactiveGraphics.Add(ownGraphic);
                if (sel.targetGraphic != null) interactiveGraphics.Add(sel.targetGraphic);
            }
            foreach (var sr in rootCanvas.GetComponentsInChildren<ScrollRect>(true))
            {
                if (sr == null) continue;
                var ownG = sr.GetComponent<Graphic>();
                if (ownG != null) interactiveGraphics.Add(ownG);
                if (sr.viewport != null)
                {
                    var vpG = sr.viewport.GetComponent<Graphic>();
                    if (vpG != null) interactiveGraphics.Add(vpG);
                }
            }

            foreach (var g in rootCanvas.GetComponentsInChildren<Graphic>(true))
            {
                if (g == null) continue;
                bool keepRaycast = interactiveGraphics.Contains(g)
                    || g.name.EndsWith("Dimmer")
                    || g.name.EndsWith("Blocker")
                    || g.name == "PauseModal"
                    || g.name == "HowToModal"
                    || g.name == "LoreDialogueModal"
                    || g.name == "ChoiceAltarModal";
                if (g.raycastTarget != keepRaycast)
                    g.raycastTarget = keepRaycast;
            }
        }

        public static void OptimizeAllCanvasesInScene()
        {
            foreach (var c in UnityEngine.Object.FindObjectsOfType<Canvas>(true))
                OptimizeCanvasHierarchy(c);
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
            img.raycastTarget = false;
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
                img.raycastTarget = false;
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
            t.raycastTarget = false;
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
            bg.raycastTarget = false;
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
            fillImg.raycastTarget = false;
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

            int activeBagFilter = -1; // -1 = All, 0 = Weapon, 1 = Armor/Helm, 2 = Ring/Amulet/Relic
            EquipItem inspectedItem = null;
            bool inspectedFromEquipped = false;
            float savedBagScrollPos = 1f;
            ScrollRect currentBagScrollRect = null;

            Action rebuild = null;
            rebuild = () =>
            {
                if (currentBagScrollRect != null)
                    savedBagScrollPos = currentBagScrollRect.verticalNormalizedPosition;
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

                // Stat Upgrade Allocation (Highlighted with glowing frame when statPoints > 0)
                bool hasStatPts = p.statPoints > 0;
                var statSectionBox = MakeStyledPanel("StatUpgradeSection", colChar.transform,
                    hasStatPts ? UIPanelStyle.Ornate : UIPanelStyle.Standard,
                    new Vector2(0.03f, 0.28f), new Vector2(0.97f, 0.435f), Vector2.zero, Vector2.zero,
                    null, hasStatPts ? new Color(1f, 0.86f, 0.28f, 0.98f) : new Color(ColBronze.r, ColBronze.g, ColBronze.b, 0.45f));
                if (hasStatPts) AttachGlowPulse(statSectionBox.gameObject, new Color(1f, 0.85f, 0.25f, 1f), new Color(1f, 0.58f, 0.12f, 0.55f));

                MakeText("StatUpHeader", statSectionBox.transform,
                    hasStatPts
                        ? $"★ NÂNG CẤP CHỈ SỐ GỐC — ĐIỂM TIỀM NĂNG: <color=#78FA88>+{p.statPoints} ĐIỂM!</color>"
                        : $"NÂNG CẤP CHỈ SỐ GỐC — ĐIỂM TIỀM NĂNG: <color=#F8DC85>{p.statPoints}</color>",
                    13, FontStyle.Bold, hasStatPts ? ColGoldBright : ColGold, TextAnchor.MiddleLeft,
                    new Vector2(0.03f, 0.58f), new Vector2(0.97f, 0.95f), Vector2.zero, Vector2.zero);

                string[] stNames = { $"+35 HP ({p.bonusHpPts})", $"+5 ATK ({p.bonusAtkPts})", $"+3.5 DEF ({p.bonusDefPts})", $"+1.5% Crit ({p.bonusHastePts})" };
                UIButtonStyle stBtnStyle = hasStatPts ? UIButtonStyle.Primary : UIButtonStyle.Disabled;
                for (int s = 0; s < 4; s++)
                {
                    int idx = s;
                    float x0 = 0.025f + s * 0.24f;
                    var b = MakeStyledButton("AddStat" + s, statSectionBox.transform, stNames[s], 11, stBtnStyle,
                        new Vector2(x0, 0.10f), new Vector2(x0 + 0.225f, 0.54f), Vector2.zero, Vector2.zero, () =>
                        {
                            GameSession.I.SpendStatPoint(idx);
                            hero?.RecalculateStats(false);
                            onChanged?.Invoke();
                            rebuild();
                        });
                    b.interactable = hasStatPts;
                }

                // Build Specialization Selector (3 Builds — Highlighted frame with build accent)
                var buildSectionBox = MakeStyledPanel("BuildSpecSection", colChar.transform, UIPanelStyle.Ornate,
                    new Vector2(0.03f, 0.015f), new Vector2(0.97f, 0.268f), Vector2.zero, Vector2.zero,
                    null, new Color(buildAccent.r, buildAccent.g, buildAccent.b, 0.85f));
                AttachGlowPulse(buildSectionBox.gameObject, new Color(buildAccent.r, buildAccent.g, buildAccent.b, 0.92f), new Color(ColGold.r, ColGold.g, ColGold.b, 0.45f));

                MakeText("BuildTitle", buildSectionBox.transform,
                    "✦ CHUYÊN MÔN HÓA HƯỚNG BUILD (THAY ĐỔI GIÁP & KỸ NĂNG):",
                    13, FontStyle.Bold, ColGoldBright, TextAnchor.MiddleLeft,
                    new Vector2(0.03f, 0.76f), new Vector2(0.97f, 0.96f), Vector2.zero, Vector2.zero);

                for (int b = 0; b < 3; b++)
                {
                    int bi = b;
                    bool isSel = (p.selectedBuild == b);
                    float x0 = 0.025f + b * 0.32f;
                    MakeStyledButton("BuildTab" + b, buildSectionBox.transform, cls.builds[b], 12,
                        isSel ? UIButtonStyle.Selected : UIButtonStyle.Tab,
                        new Vector2(x0, 0.43f), new Vector2(x0 + 0.305f, 0.73f), Vector2.zero, Vector2.zero, () =>
                        {
                            p.selectedBuild = bi;
                            GameSession.I.SelectLocalClass(p.selectedClass, bi);
                            hero?.RecalculateStats(false);
                            onChanged?.Invoke();
                            rebuild();
                        });
                }

                var buildDescBox = MakeStyledPanel("BuildDescBox", buildSectionBox.transform, UIPanelStyle.Notification,
                    new Vector2(0.025f, 0.06f), new Vector2(0.975f, 0.39f), Vector2.zero, Vector2.zero);
                MakeText("BuildDescTxt", buildDescBox.transform,
                    $"• {cls.builds[p.selectedBuild]}: {cls.buildDesc[p.selectedBuild]}",
                    11, FontStyle.Italic, buildAccent, TextAnchor.MiddleLeft,
                    Vector2.zero, Vector2.one, new Vector2(10f, 2f), new Vector2(-10f, -2f));

                // =================================================================
                // COLUMN 2 (CENTER): UI_Panel_SkillTree — ANCIENT RUNE SKILL TREE
                // =================================================================
                bool hasSkillPts = p.skillPoints > 0;
                var colSkill = MakeStyledPanel("UI_Panel_SkillTree", bodyRoot, UIPanelStyle.SkillTree,
                    new Vector2(0.335f, 0f), new Vector2(0.655f, 1f), Vector2.zero, Vector2.zero,
                    null, hasSkillPts ? new Color(1f, 0.88f, 0.30f, 1f) : (Color?)null);
                if (hasSkillPts) AttachGlowPulse(colSkill.gameObject, new Color(1f, 0.90f, 0.32f, 1f), new Color(0.95f, 0.55f, 0.12f, 0.55f));

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
                    hasSkillPts
                        ? $"★ CÂY KỸ NĂNG CỔ NGỮ — <color=#78FA88>CÓ {p.skillPoints} ĐIỂM NÂNG CẤP!</color>"
                        : $"CÂY KỸ NĂNG CỔ NGỮ — ĐIỂM KỸ NĂNG: <color=#F8DC85>{p.skillPoints} SP</color>",
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
                    if (canUp) AttachGlowPulse(node.gameObject, new Color(1f, 0.88f, 0.30f, 0.98f), new Color(0.45f, 0.95f, 0.55f, 0.65f));
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
                // COLUMN 3 (RIGHT): UI_Panel_Inventory — 6 EQUIPPED SLOTS, SCROLLABLE BAG & TOOLTIP
                // =================================================================
                var colInv = MakeStyledPanel("UI_Panel_Inventory", bodyRoot, UIPanelStyle.Inventory,
                    new Vector2(0.67f, 0f), new Vector2(1.00f, 1f), Vector2.zero, Vector2.zero);

                MakeIcon("InvHdrIc", colInv.transform, GetItemSheetSprite(14),
                    new Vector2(0.03f, 0.925f), new Vector2(0.095f, 0.98f), Vector2.zero, Vector2.zero);

                MakeText("InvHdrTxt", colInv.transform,
                    $"TRANG BỊ ĐANG MẶC (6 Ô) & TÚI ĐỒ ({p.bag.Count} MÓN)",
                    15, FontStyle.Bold, ColGoldBright, TextAnchor.MiddleLeft,
                    new Vector2(0.105f, 0.92f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

                // 6 Equipment Slots in a clean 3x2 Relic Grid (No Text Overflow!)
                for (int s = 0; s < 6; s++)
                {
                    EquipItem eq = GameSession.I.GetEquipped((ItemSlot)s);
                    int gridCol = s % 3;
                    int gridRow = s / 3;
                    float x0 = 0.03f + gridCol * 0.316f;
                    float x1 = x0 + 0.304f;
                    float y1 = gridRow == 0 ? 0.912f : 0.818f;
                    float y0 = y1 - 0.086f;

                    Color rimCol = eq != null ? GameDB.RarityColors[(int)eq.rarity] : ColBronze;
                    var eqCard = MakeStyledPanel("EqSlot_" + s, colInv.transform, UIPanelStyle.Ornate,
                        new Vector2(x0, y0), new Vector2(x1, y1), Vector2.zero, Vector2.zero, null, rimCol);
                    eqCard.gameObject.AddComponent<RectMask2D>();

                    var icBox = MakePanel("EqIcFrame", eqCard.transform,
                        new Vector2(0.04f, 0.12f), new Vector2(0.31f, 0.88f), Vector2.zero, Vector2.zero,
                        new Color(0.06f, 0.07f, 0.10f, 0.92f), rimCol);
                    MakeIcon("SlotIc", icBox.transform, eq != null ? GetItemSprite(eq) : GetSlotIcon((ItemSlot)s),
                        new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero,
                        eq != null ? Color.white : new Color(1f, 1f, 1f, 0.35f));

                    string slotTitle = s < GameDB.SlotNames.Length ? GameDB.SlotNames[s] : "Cổ vật";
                    string eqLabel = eq != null
                        ? $"<color=#{ColorUtility.ToHtmlStringRGB(rimCol)}>{slotTitle}</color>\n<size=10><color=#EDE0BA>{FormatCompactPrimaryStat(eq)}</color></size>"
                        : $"<color=#BDA685>{slotTitle}</color>\n<size=10><color=#7A7062>(Trống)</color></size>";

                    var slotTxt = MakeText("SlotTxt", eqCard.transform, eqLabel, 11, FontStyle.Bold, ColParchment, TextAnchor.MiddleLeft,
                        new Vector2(0.35f, 0.08f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);
                    slotTxt.verticalOverflow = VerticalWrapMode.Truncate;
                    slotTxt.resizeTextForBestFit = true;
                    slotTxt.resizeTextMinSize = 9;
                    slotTxt.resizeTextMaxSize = 11;

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
                        new Vector2(tx0, 0.672f), new Vector2(tx0 + 0.228f, 0.722f), Vector2.zero, Vector2.zero, () =>
                        {
                            activeBagFilter = tv;
                            savedBagScrollPos = 1f;
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
                    if (activeBagFilter == 2 && it.slot != ItemSlot.Ring && it.slot != ItemSlot.Amulet && it.slot != ItemSlot.Relic) continue;
                    filtered.Add(it);
                }

                var gridBox = MakeStyledPanel("BagGridBox", colInv.transform, UIPanelStyle.Standard,
                    new Vector2(0.03f, 0.252f), new Vector2(0.97f, 0.662f), Vector2.zero, Vector2.zero);

                if (filtered.Count == 0)
                {
                    MakeText("EmptyBag", gridBox.transform,
                        "Chưa có vật phẩm thuộc nhóm này trong túi đồ.\nHãy tiêu diệt quái vật hoặc rèn đồ tại Thị Trấn!",
                        13, FontStyle.Italic, ColMuted, TextAnchor.MiddleCenter,
                        Vector2.zero, Vector2.one, new Vector2(16f, 16f), new Vector2(-16f, -16f));
                }
                else
                {
                    // Full Vertical ScrollRect + Viewport (RectMask2D) + Content (VerticalLayoutGroup + ContentSizeFitter) + Right Scrollbar
                    var scrollRt = MakeRect("BagScrollRect", gridBox.transform, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
                    var scrollRect = scrollRt.gameObject.AddComponent<ScrollRect>();
                    scrollRect.horizontal = false;
                    scrollRect.vertical = true;
                    scrollRect.movementType = ScrollRect.MovementType.Clamped;
                    scrollRect.inertia = true;
                    scrollRect.decelerationRate = 0.12f;
                    scrollRect.scrollSensitivity = 32f;
                    currentBagScrollRect = scrollRect;

                    var viewportRt = MakeRect("Viewport", scrollRt, new Vector2(0f, 0f), new Vector2(0.962f, 1f), Vector2.zero, Vector2.zero);
                    var vpImg = viewportRt.gameObject.AddComponent<Image>();
                    vpImg.color = new Color(0f, 0f, 0f, 0.01f);
                    vpImg.raycastTarget = true;
                    viewportRt.gameObject.AddComponent<RectMask2D>();
                    scrollRect.viewport = viewportRt;

                    var contentRt = MakeRect("Content", viewportRt, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
                    contentRt.pivot = new Vector2(0.5f, 1f);
                    var vlg = contentRt.gameObject.AddComponent<VerticalLayoutGroup>();
                    vlg.padding = new RectOffset(4, 4, 4, 6);
                    vlg.spacing = 7f;
                    vlg.childAlignment = TextAnchor.UpperCenter;
                    vlg.childControlWidth = true;
                    vlg.childControlHeight = true;
                    vlg.childForceExpandWidth = true;
                    vlg.childForceExpandHeight = false;

                    var csf = contentRt.gameObject.AddComponent<ContentSizeFitter>();
                    csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    scrollRect.content = contentRt;

                    // Vertical Scrollbar on Right
                    var sbRt = MakeRect("ScrollbarVertical", scrollRt, new Vector2(0.968f, 0.02f), new Vector2(0.996f, 0.98f), Vector2.zero, Vector2.zero);
                    var sbBg = sbRt.gameObject.AddComponent<Image>();
                    sbBg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
                    var sbOutline = sbRt.gameObject.AddComponent<Outline>();
                    sbOutline.effectColor = new Color(ColBronze.r, ColBronze.g, ColBronze.b, 0.55f);

                    var slideArea = MakeRect("SlidingArea", sbRt, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
                    var handleRt = MakeRect("Handle", slideArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    var handleImg = handleRt.gameObject.AddComponent<Image>();
                    handleImg.color = ColGold;

                    var scrollbar = sbRt.gameObject.AddComponent<Scrollbar>();
                    scrollbar.handleRect = handleRt;
                    scrollbar.targetGraphic = handleImg;
                    scrollbar.direction = Scrollbar.Direction.BottomToTop;
                    scrollRect.verticalScrollbar = scrollbar;
                    scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

                    for (int i = 0; i < filtered.Count; i++)
                    {
                        EquipItem item = filtered[i];
                        if (inspectedItem == null && i == 0)
                        {
                            inspectedItem = item;
                            inspectedFromEquipped = false;
                        }

                        Color rCol = GameDB.RarityColors[(int)item.rarity];
                        bool isInspected = (inspectedItem == item);
                        EquipItem curEq = GameSession.I.GetEquipped(item.slot);

                        var rowRt = MakeRect("BagItem_" + i, contentRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                        var rowLe = rowRt.gameObject.AddComponent<LayoutElement>();
                        rowLe.minHeight = 76f;
                        rowLe.preferredHeight = 76f;

                        var rowImg = rowRt.gameObject.AddComponent<Image>();
                        rowImg.raycastTarget = false;
                        rowImg.color = isInspected
                            ? new Color(0.13f, 0.16f, 0.23f, 0.98f)
                            : new Color(0.08f, 0.10f, 0.15f, 0.96f);

                        var rowOutline = rowRt.gameObject.AddComponent<Outline>();
                        rowOutline.effectColor = isInspected ? Color.Lerp(rCol, Color.white, 0.35f) : new Color(rCol.r, rCol.g, rCol.b, 0.88f);
                        rowOutline.effectDistance = isInspected ? new Vector2(2f, -2f) : new Vector2(1.5f, -1.5f);

                        // Left Rarity-Framed Crisp Sliced Item Icon
                        var icFrame = MakePanel("IcFrame", rowRt,
                            new Vector2(0.014f, 0.11f), new Vector2(0.122f, 0.89f), Vector2.zero, Vector2.zero,
                            new Color(0.05f, 0.06f, 0.09f, 0.98f), rCol);
                        MakeIcon("ItIc", icFrame.transform, GetItemSprite(item),
                            new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero);

                        // Top Title Row: [Rarity] Name (Slot) + Class Compatibility Pill Badge
                        var topLineRt = MakeRect("TopLine", rowRt,
                            new Vector2(0.136f, 0.52f), new Vector2(0.648f, 0.92f), Vector2.zero, Vector2.zero);
                        var topHlg = topLineRt.gameObject.AddComponent<HorizontalLayoutGroup>();
                        topHlg.spacing = 6f;
                        topHlg.childAlignment = TextAnchor.MiddleLeft;
                        topHlg.childControlWidth = true;
                        topHlg.childControlHeight = true;
                        topHlg.childForceExpandWidth = false;
                        topHlg.childForceExpandHeight = false;

                        string slotLbl = (int)item.slot < GameDB.SlotNames.Length ? GameDB.SlotNames[(int)item.slot] : "Cổ vật";
                        var titleTxt = MakeText("ItTitle", topLineRt,
                            $"[{GameDB.RarityNames[(int)item.rarity]}] {item.name} ({slotLbl})",
                            12, FontStyle.Bold, rCol, TextAnchor.MiddleLeft,
                            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                        titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                        titleTxt.verticalOverflow = VerticalWrapMode.Truncate;

                        // Class Compatibility Badge (e.g. "Hợp Assassin", "Hợp Knight", "Hợp Mage", "Hợp Cleric")
                        string compatBadgeText = GetClassAffinityBadgeText(p.selectedClass, item, out bool isCompat);
                        CreatePillBadge(topLineRt, compatBadgeText,
                            isCompat ? new Color(0.08f, 0.24f, 0.16f, 0.96f) : new Color(0.26f, 0.12f, 0.12f, 0.96f),
                            isCompat ? new Color(0.24f, 0.72f, 0.42f, 0.85f) : new Color(0.75f, 0.30f, 0.30f, 0.85f),
                            isCompat ? new Color(0.38f, 0.95f, 0.58f, 1f) : new Color(0.96f, 0.52f, 0.52f, 1f),
                            10);

                        // Bottom Pill Row: [Archetype] + Individual Stat Comparison Pills (▲+11 Green / ▼-3 Red)
                        var botLineRt = MakeRect("BotLine", rowRt,
                            new Vector2(0.136f, 0.08f), new Vector2(0.648f, 0.48f), Vector2.zero, Vector2.zero);
                        var botHlg = botLineRt.gameObject.AddComponent<HorizontalLayoutGroup>();
                        botHlg.spacing = 5f;
                        botHlg.childAlignment = TextAnchor.MiddleLeft;
                        botHlg.childControlWidth = true;
                        botHlg.childControlHeight = true;
                        botHlg.childForceExpandWidth = false;
                        botHlg.childForceExpandHeight = false;

                        string archShort = (item.slot == ItemSlot.Ring || item.slot == ItemSlot.Amulet || item.slot == ItemSlot.Relic)
                            ? "[Trang Sức]"
                            : $"[{ClassSpecializationSystem.GetArchetypeShortTag(item.archetype)}]";
                        CreatePillBadge(botLineRt, archShort,
                            new Color(0.13f, 0.16f, 0.22f, 0.95f),
                            new Color(0.32f, 0.38f, 0.48f, 0.75f),
                            new Color(0.78f, 0.82f, 0.90f, 1f),
                            10);

                        PopulateStatComparisonPills(botLineRt, item, curEq);

                        // Right 3 Action Buttons: Chi Tiết | Mặc | Bán +XXG
                        CreateRowActionButton("Inspect_" + i, rowRt, "Chi Tiết", 11,
                            new Color(0.14f, 0.18f, 0.25f, 0.98f), new Color(0.36f, 0.44f, 0.56f, 0.85f), ColParchment,
                            new Vector2(0.654f, 0.16f), new Vector2(0.764f, 0.84f), () =>
                            {
                                inspectedItem = item;
                                inspectedFromEquipped = false;
                                rebuild();
                            });

                        CreateRowActionButton("Equip_" + i, rowRt, "Mặc", 11,
                            new Color(0.34f, 0.25f, 0.11f, 0.98f), new Color(0.85f, 0.68f, 0.30f, 0.92f), ColGoldBright,
                            new Vector2(0.772f, 0.16f), new Vector2(0.872f, 0.84f), () =>
                            {
                                GameSession.I.Equip(item);
                                inspectedItem = item;
                                inspectedFromEquipped = true;
                                hero?.RecalculateStats(false);
                                onChanged?.Invoke();
                                rebuild();
                            });

                        int sellGold = Mathf.Max(5, item.value > 0 ? item.value : 12 * ((int)item.rarity + 1));
                        CreateRowActionButton("Sell_" + i, rowRt, $"Bán\n<color=#F8DC85>+{sellGold}G</color>", 10,
                            new Color(0.36f, 0.11f, 0.15f, 0.98f), new Color(0.82f, 0.26f, 0.32f, 0.92f), Color.white,
                            new Vector2(0.880f, 0.12f), new Vector2(0.986f, 0.88f), () =>
                            {
                                if (inspectedItem == item) inspectedItem = null;
                                GameSession.I.SellBagItem(item);
                                onChanged?.Invoke();
                                rebuild();
                            });
                    }

                    Canvas.ForceUpdateCanvases();
                    scrollRect.verticalNormalizedPosition = Mathf.Clamp01(savedBagScrollPos);
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

                    MakeIcon("TtIc", tooltipBox.transform, GetItemSprite(inspectedItem),
                        new Vector2(0.02f, 0.16f), new Vector2(0.15f, 0.84f), Vector2.zero, Vector2.zero);

                    string cmpLine = "";
                    if (!inspectedFromEquipped && curEq != null && curEq != inspectedItem)
                    {
                        float dAtk = inspectedItem.atk - curEq.atk;
                        float dHp  = inspectedItem.hp - curEq.hp;
                        float dDef = inspectedItem.def - curEq.def;
                        string cAtk = dAtk >= 0 ? $"<color=#4CE878>▲+{dAtk:0} ATK</color>" : $"<color=#F85858>▼{dAtk:0} ATK</color>";
                        string cHp  = dHp >= 0  ? $"<color=#4CE878>▲+{dHp:0} HP</color>"  : $"<color=#F85858>▼{dHp:0} HP</color>";
                        string cDef = dDef >= 0 ? $"<color=#4CE878>▲+{dDef:0} DEF</color>" : $"<color=#F85858>▼{dDef:0} DEF</color>";
                        cmpLine = $"\nSo với đồ đang mặc ({curEq.name}): {cAtk}  |  {cHp}  |  {cDef}";
                    }
                    else if (inspectedFromEquipped)
                    {
                        cmpLine = "\n<color=#F8DC85>• Trang bị này đang được mặc trên người.</color>";
                    }

                    int slIdx = Mathf.Clamp((int)inspectedItem.slot, 0, GameDB.SlotNames.Length - 1);
                    MakeText("TtBody", tooltipBox.transform,
                        $"[{GameDB.RarityNames[(int)inspectedItem.rarity].ToUpper()}] {inspectedItem.name} — Vị trí: {GameDB.SlotNames[slIdx]}\n" +
                        $"<size=12><color=#EDE0BA>Chỉ số: {inspectedItem.Summary()}</color>{cmpLine}</size>",
                        13, FontStyle.Bold, rc, TextAnchor.MiddleLeft,
                        new Vector2(0.17f, 0.08f), new Vector2(0.98f, 0.92f), Vector2.zero, Vector2.zero);
                }
            };

            rebuild();
            modal.gameObject.SetActive(false);
            return modal.gameObject;
        }

        private static string FormatCompactPrimaryStat(EquipItem eq)
        {
            if (eq == null) return "(Trống)";
            var p = new List<string>(2);
            if (eq.atk > 0.1f) p.Add($"+{eq.atk:0} ATK");
            if (eq.def > 0.1f) p.Add($"+{eq.def:0} DEF");
            if (eq.hp > 0.5f && p.Count < 2) p.Add($"+{eq.hp:0} HP");
            if (eq.crit > 0.001f && p.Count < 2) p.Add($"+{eq.crit * 100:0.#}% Crit");
            if (eq.cdr > 0.001f && p.Count < 2) p.Add($"+{eq.cdr * 100:0.#}% CDR");
            return p.Count > 0 ? string.Join(" • ", p) : eq.name;
        }

        private static string GetClassAffinityBadgeText(int selectedClassId, EquipItem item, out bool isCompat)
        {
            isCompat = true;
            if (item == null) return "Hợp Mọi Hệ";
            if (item.slot == ItemSlot.Ring || item.slot == ItemSlot.Amulet || item.slot == ItemSlot.Relic || item.archetype == EquipArchetype.Universal)
                return "Hợp Mọi Hệ";

            string nm = item.name ?? "";
            string targetClassLabel;
            switch (item.archetype)
            {
                case EquipArchetype.VanguardPlate:
                    targetClassLabel = nm.Contains("Đại Đao") || nm.Contains("Trọng Kiếm") ? "Hợp Berserker/Knight" : "Hợp Knight/Guardian";
                    break;
                case EquipArchetype.StalkerLeather:
                    if (nm.Contains("Cung")) targetClassLabel = "Hợp Ranger";
                    else if (nm.Contains("Quyền") || nm.Contains("Thiền")) targetClassLabel = "Hợp Monk/Assassin";
                    else targetClassLabel = "Hợp Assassin";
                    break;
                case EquipArchetype.ArcanistCloth:
                    targetClassLabel = nm.Contains("Hắc") || nm.Contains("Nguyền") ? "Hợp Warlock/Mage" : "Hợp Mage";
                    break;
                case EquipArchetype.SacredRelic:
                    targetClassLabel = nm.Contains("Rừng") || nm.Contains("Lộc") ? "Hợp Druid/Cleric" : "Hợp Cleric";
                    break;
                default:
                    targetClassLabel = "Hợp Mọi Hệ";
                    break;
            }

            EquipArchetype heroArch = ClassSpecializationSystem.GetClassArchetype(selectedClassId);
            isCompat = (item.archetype == heroArch) || (item.slot != ItemSlot.Weapon);
            return targetClassLabel;
        }

        private static void CreatePillBadge(Transform parent, string text, Color bgCol, Color borderCol, Color textCol, int fontSize = 10)
        {
            var pillGo = new GameObject("PillBadge");
            pillGo.transform.SetParent(parent, false);
            var rt = pillGo.AddComponent<RectTransform>();
            var img = pillGo.AddComponent<Image>();
            img.color = bgCol;
            var ol = pillGo.AddComponent<Outline>();
            ol.effectColor = borderCol;
            ol.effectDistance = new Vector2(1f, -1f);

            var hlg = pillGo.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(7, 7, 2, 2);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var csf = pillGo.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var txtGo = new GameObject("Txt");
            txtGo.transform.SetParent(pillGo.transform, false);
            txtGo.AddComponent<RectTransform>();
            var t = txtGo.AddComponent<Text>();
            t.font = BodyFont;
            t.text = text;
            t.fontSize = fontSize;
            t.fontStyle = FontStyle.Bold;
            t.color = textCol;
            t.alignment = TextAnchor.MiddleCenter;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
        }

        private static void PopulateStatComparisonPills(Transform parent, EquipItem item, EquipItem curEq)
        {
            if (item == null) return;
            bool hasCmp = curEq != null && curEq != item;

            void AddStatPill(float val, float eqVal, string label, bool isPercent)
            {
                if (val <= (isPercent ? 0.001f : 0.1f)) return;
                float dispVal = isPercent ? val * 100f : val;
                float dispEq  = isPercent ? eqVal * 100f : eqVal;
                float delta   = hasCmp ? (dispVal - dispEq) : dispVal;
                string suffix = isPercent ? "%" : "";
                string fmt    = isPercent ? "0.#" : "0";

                string deltaStr;
                if (Mathf.Abs(delta) < (isPercent ? 0.05f : 0.4f))
                    deltaStr = "";
                else if (delta > 0f)
                    deltaStr = $" <color=#3CE878>▲+{delta.ToString(fmt)}{suffix}</color>";
                else
                    deltaStr = $" <color=#F85858>▼{delta.ToString(fmt)}{suffix}</color>";

                string fullText = $"+{dispVal.ToString(fmt)}{suffix} {label}{deltaStr}";
                CreatePillBadge(parent, fullText,
                    new Color(0.10f, 0.13f, 0.19f, 0.96f),
                    new Color(0.24f, 0.30f, 0.40f, 0.80f),
                    new Color(0.92f, 0.94f, 0.98f, 1f),
                    10);
            }

            AddStatPill(item.atk,       curEq != null ? curEq.atk : 0f,       "ATK",            false);
            AddStatPill(item.def,       curEq != null ? curEq.def : 0f,       "DEF",            false);
            AddStatPill(item.hp,        curEq != null ? curEq.hp : 0f,        "HP",             false);
            AddStatPill(item.mana,      curEq != null ? curEq.mana : 0f,      "MP",             false);
            AddStatPill(item.crit,      curEq != null ? curEq.crit : 0f,      "Crit",           true);
            AddStatPill(item.atkSpeed,  curEq != null ? curEq.atkSpeed : 0f,  "ASPD",           true);
            AddStatPill(item.cdr,       curEq != null ? curEq.cdr : 0f,       "Giảm hồi chiêu", true);
            AddStatPill(item.lifesteal, curEq != null ? curEq.lifesteal : 0f, "Hút máu",        true);
        }

        private static Button CreateRowActionButton(string name, Transform parent, string label, int fontSize,
            Color bgCol, Color borderCol, Color textCol, Vector2 anchorMin, Vector2 anchorMax, Action onClick)
        {
            var rt = MakeRect(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bgCol;
            var ol = rt.gameObject.AddComponent<Outline>();
            ol.effectColor = borderCol;
            ol.effectDistance = new Vector2(1.2f, -1.2f);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var lbl = MakeText("Lbl", rt, label, fontSize, FontStyle.Bold, textCol, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            lbl.raycastTarget = false;

            btn.onClick.AddListener(() =>
            {
                GameAudio.I?.PlaySfx(GameSfx.Click, 0.6f);
                onClick?.Invoke();
            });
            return btn;
        }

        private static void AttachGlowPulse(GameObject go, Color c1, Color c2)
        {
            if (go == null) return;
            var pulse = go.GetComponent<UIGlowPulseBehaviour>() ?? go.AddComponent<UIGlowPulseBehaviour>();
            pulse.OutlineTarget = go.GetComponent<Outline>();
            pulse.ColorA = c1;
            pulse.ColorB = c2;
        }

        private class UIGlowPulseBehaviour : MonoBehaviour
        {
            public Outline OutlineTarget;
            public Color ColorA = new Color(1f, 0.88f, 0.30f, 1f);
            public Color ColorB = new Color(1f, 0.55f, 0.15f, 0.5f);

            private void Update()
            {
                if (OutlineTarget == null) return;
                float s = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4.2f);
                OutlineTarget.effectColor = Color.Lerp(ColorB, ColorA, s);
                float d = Mathf.Lerp(1.5f, 2.6f, s);
                OutlineTarget.effectDistance = new Vector2(d, -d);
            }
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

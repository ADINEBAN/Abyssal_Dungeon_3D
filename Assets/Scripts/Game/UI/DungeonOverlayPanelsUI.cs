using System;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssGame
{
    /// <summary>
    /// Full Dungeon Tactical Map [M] & Quick Rune Skill Tree Modal [K] (ANCIENT GOTHIC EDITION).
    /// Zero emojis: uses authentic engraved medieval & rune icons.
    /// </summary>
    public static class DungeonOverlayPanelsUI
    {
        public static GameObject BuildFullDungeonMapModal(Transform canvasRoot, Action onStateChanged)
        {
            var modal = GameUIKit.MakeStyledPanel("FullDungeonMapModal", canvasRoot, UIPanelStyle.Dialog,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-640f, -400f), new Vector2(640f, 400f));

            GameUIKit.MakeIcon("MapCrest", modal.transform, GameUIKit.GetIcon("portal"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -60f), new Vector2(66f, -20f));

            GameUIKit.MakeText("Title", modal.transform,
                "BẢN ĐỒ CHIẾN THUẬT HẦM NGỤC & BÁO CÁO AI ABYSS DIRECTOR",
                22, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0.80f, 1f), new Vector2(76f, -64f), new Vector2(0f, -14f));

            GameUIKit.MakeGothicDivider("MapDiv", modal.transform,
                new Vector2(0.03f, 1f), new Vector2(0.97f, 1f), new Vector2(0f, -74f), new Vector2(0f, -56f));

            GameUIKit.MakeStyledButton("Close", modal.transform, "ĐÓNG [M]", 15, UIButtonStyle.Danger,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-164f, -60f), new Vector2(-24f, -16f), () =>
                {
                    modal.gameObject.SetActive(false);
                    onStateChanged?.Invoke();
                });

            var content = GameUIKit.MakeRect("Content", modal.transform, Vector2.zero, Vector2.one, new Vector2(24f, 20f), new Vector2(-24f, -78f));
            var updater = modal.gameObject.AddComponent<FullMapModalUpdater>();
            updater.ContentRoot = content;
            updater.OnClosed = onStateChanged;

            modal.gameObject.SetActive(false);
            return modal.gameObject;
        }

        /// <summary>
        /// Dedicated Gothic Rune Skill Tree & Active Ability Inspector Modal opened by Right-Menu "Kỹ Năng [K]" or key [K].
        /// Uses the exact same GameSession.SpendSkillPoint & ClassSpecializationSystem bindings without altering gameplay logic.
        /// </summary>
        public static GameObject BuildSkillTreeQuickModal(Transform canvasRoot, Func<ActorUnit> getHero, Action onStateChanged)
        {
            var modal = GameUIKit.MakeStyledPanel("SkillTreeQuickModal", canvasRoot, UIPanelStyle.Dialog,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-500f, -380f), new Vector2(500f, 380f));

            GameUIKit.MakeIcon("SkModalCrest", modal.transform, GameUIKit.GetIcon("skill_tree"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -60f), new Vector2(66f, -20f));

            GameUIKit.MakeText("Title", modal.transform,
                "CÂY KỸ NĂNG CỔ NGỮ & NÂNG CẤP CHIÊU THỨC",
                22, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0.78f, 1f), new Vector2(76f, -64f), new Vector2(0f, -14f));

            GameUIKit.MakeGothicDivider("SkDiv", modal.transform,
                new Vector2(0.03f, 1f), new Vector2(0.97f, 1f), new Vector2(0f, -74f), new Vector2(0f, -56f));

            GameUIKit.MakeStyledButton("Close", modal.transform, "ĐÓNG [K]", 15, UIButtonStyle.Danger,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-164f, -60f), new Vector2(-24f, -16f), () =>
                {
                    modal.gameObject.SetActive(false);
                    onStateChanged?.Invoke();
                });

            var content = GameUIKit.MakeRect("Content", modal.transform, Vector2.zero, Vector2.one, new Vector2(24f, 20f), new Vector2(-24f, -78f));
            var updater = modal.gameObject.AddComponent<SkillTreeQuickUpdater>();
            updater.ContentRoot = content;
            updater.GetHero = getHero;
            updater.OnChanged = onStateChanged;

            modal.gameObject.SetActive(false);
            return modal.gameObject;
        }

        private class SkillTreeQuickUpdater : MonoBehaviour
        {
            public Transform ContentRoot;
            public Func<ActorUnit> GetHero;
            public Action OnChanged;

            private void OnEnable() => Rebuild();

            private void Rebuild()
            {
                if (ContentRoot == null) return;
                for (int i = ContentRoot.childCount - 1; i >= 0; i--)
                    Destroy(ContentRoot.GetChild(i).gameObject);

                var p = GameSession.I.Profile;
                ClassDef cls = GameDB.GetClass(p.selectedClass);
                ActorUnit hero = GetHero?.Invoke();

                // Header summary bar
                var topBar = GameUIKit.MakeStyledPanel("SkillTopBar", ContentRoot, UIPanelStyle.Ornate,
                    new Vector2(0f, 0.86f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
                GameUIKit.MakeIcon("ClsIc", topBar.transform, GameUIKit.GetClassIcon(cls.id),
                    new Vector2(0.015f, 0.12f), new Vector2(0.095f, 0.88f), Vector2.zero, Vector2.zero);
                GameUIKit.MakeText("HdrTxt", topBar.transform,
                    $"{cls.name.ToUpper()} — {cls.builds[p.selectedBuild]}   •   Nội tại: <color=#EDE0BA>{cls.passive}</color>\n" +
                    $"Điểm Kỹ Năng khả dụng: <color=#F8DC85>{p.skillPoints} SP</color>   •   Nhấn <color=#F8DC85>[I]</color> để đổi hướng Build & Trang bị.",
                    13, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                    new Vector2(0.11f, 0.08f), new Vector2(0.98f, 0.92f), Vector2.zero, Vector2.zero);

                string[] hotkeys = { "Q / 1", "2 / Z", "E / 3", "R / 4 • ULTIMATE" };
                for (int k = 0; k < 4; k++)
                {
                    int ki = k;
                    SkillDef sk = cls.skills[k];
                    int rank = (p.skillRanks != null && k < p.skillRanks.Length) ? p.skillRanks[k] : 1;
                    bool isUlt = sk.ultimate || k == 3;
                    float yMax = 0.835f - k * 0.208f;
                    float yMin = yMax - 0.192f;

                    var card = GameUIKit.MakeStyledPanel("SkillCard_" + k, ContentRoot,
                        isUlt ? UIPanelStyle.Ornate : UIPanelStyle.Standard,
                        new Vector2(0f, yMin), new Vector2(1f, yMax), Vector2.zero, Vector2.zero);

                    var icFrame = GameUIKit.MakeStyledPanel("IcFrame", card.transform, UIPanelStyle.Inventory,
                        new Vector2(0.015f, 0.12f), new Vector2(0.115f, 0.88f), Vector2.zero, Vector2.zero);
                    GameUIKit.MakeIcon("SkIc", icFrame.transform, GameUIKit.GetSkillIcon(sk),
                        new Vector2(0.10f, 0.10f), new Vector2(0.90f, 0.90f), Vector2.zero, Vector2.zero);

                    float mult = 1f + 0.15f * (rank - 1);
                    GameUIKit.MakeText("SkTitle", card.transform,
                        $"[{hotkeys[k]}] {sk.name}   •   <color=#F8DC85>Cấp {rank}/5</color>",
                        15, FontStyle.Bold, isUlt ? GameUIKit.ColGoldBright : sk.color, TextAnchor.UpperLeft,
                        new Vector2(0.135f, 0.56f), new Vector2(0.77f, 0.92f), Vector2.zero, Vector2.zero);

                    GameUIKit.MakeText("SkDesc", card.transform,
                        $"{sk.desc}\n<color=#BDA685>Hồi chiêu: {sk.cooldown:0.#}s   |   Mana: {sk.mana:0} MP   |   Sát thương / Hiệu quả: {(sk.power * mult * 100f):0}% ATK</color>",
                        13, FontStyle.Normal, GameUIKit.ColParchment, TextAnchor.LowerLeft,
                        new Vector2(0.135f, 0.22f), new Vector2(0.77f, 0.58f), Vector2.zero, Vector2.zero);

                    var prog = GameUIKit.MakeBar("RankProg", card.transform, isUlt ? GameUIKit.ColGold : new Color(0.54f, 0.28f, 0.84f),
                        new Vector2(0.135f, 0.06f), new Vector2(0.75f, 0.19f), Vector2.zero, Vector2.zero, 10);
                    GameUIKit.SetBarRatio(prog.fill, rank / 5f);

                    bool canUp = p.skillPoints > 0 && rank < 5;
                    UIButtonStyle style = rank >= 5 ? UIButtonStyle.Selected : (canUp ? UIButtonStyle.Primary : UIButtonStyle.Disabled);
                    string lbl = rank >= 5 ? "ĐÃ TỐI ĐA" : "NÂNG CẤP\n(+15% Uy lực)";
                    var btn = GameUIKit.MakeStyledButton("UpBtn_" + k, card.transform, lbl, 12, style,
                        new Vector2(0.79f, 0.16f), new Vector2(0.98f, 0.84f), Vector2.zero, Vector2.zero, () =>
                        {
                            if (GameSession.I.SpendSkillPoint(ki))
                            {
                                GameAudio.I?.PlaySfx(GameSfx.LevelUp, 0.8f);
                                hero?.RecalculateStats(false);
                                OnChanged?.Invoke();
                                Rebuild();
                            }
                        });
                    btn.interactable = canUp;
                }
            }
        }

        private class FullMapModalUpdater : MonoBehaviour
        {
            public Transform ContentRoot;
            public Action OnClosed;
            private float _timer;

            private void OnEnable()
            {
                _timer = 0f;
                Rebuild();
            }

            private void Update()
            {
                _timer -= Time.unscaledDeltaTime;
                if (_timer <= 0f)
                {
                    _timer = 0.6f;
                    Rebuild();
                }
            }

            private void Rebuild()
            {
                if (ContentRoot == null) return;
                for (int i = ContentRoot.childCount - 1; i >= 0; i--)
                    Destroy(ContentRoot.GetChild(i).gameObject);

                var dw = DungeonWorldBuilder.Instance;
                if (dw == null) return;

                int roomCount = Mathf.Max(1, dw.Rooms.Count);

                // Left: Dynamic Multi-Zone Dungeon Progression Chain (Main Path + Side Branch Rooms!)
                var left = GameUIKit.MakeStyledPanel("RoomChainBox", ContentRoot, UIPanelStyle.SkillTree,
                    new Vector2(0f, 0f), new Vector2(0.56f, 1f), Vector2.zero, Vector2.zero);

                GameUIKit.MakeText("Hdr", left.transform,
                    $"TIẾN ĐỘ {roomCount} KHU VỰC HẦM NGỤC — TẦNG {GameSession.I.Profile.selectedFloor}",
                    15, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -42f), new Vector2(-18f, -8f));

                int curIdx = dw.CurrentRoomIndex;
                float stepY = Mathf.Min(0.118f, 0.86f / roomCount);
                float cardH = stepY * 0.88f;

                for (int i = 0; i < dw.Rooms.Count; i++)
                {
                    var r = dw.Rooms[i];
                    bool isHere = (i == curIdx);
                    float yMax = 0.90f - i * stepY;
                    float yMin = yMax - cardH;

                    UIPanelStyle rowStyle = isHere ? UIPanelStyle.Ornate : (r.cleared ? UIPanelStyle.Character : UIPanelStyle.Standard);
                    Color? borderCol = isHere ? GameUIKit.ColGoldBright
                        : (r.type == RoomType.Boss ? GameUIKit.ColDanger
                        : (r.type == RoomType.Treasure ? new Color(0.95f, 0.78f, 0.28f) : (Color?)null));

                    var card = GameUIKit.MakeStyledPanel("Room_" + i, left.transform, rowStyle,
                        new Vector2(0.035f, yMin), new Vector2(0.965f, yMax), Vector2.zero, Vector2.zero, null, borderCol);

                    string iconKey = r.cleared ? "check_seal"
                        : (isHere ? "crossed_swords"
                        : (r.type == RoomType.Boss ? "skull_boss"
                        : (r.type == RoomType.Treasure ? "gold" : "lock_iron")));
                    GameUIKit.MakeIcon("StateIc", card.transform, GameUIKit.GetIcon(iconKey),
                        new Vector2(0.02f, 0.14f), new Vector2(0.10f, 0.86f), Vector2.zero, Vector2.zero);

                    string branchTag = r.isSideBranch ? " <color=#E8C872>[LỐI PHỤ]</color>" : "";
                    string statusTag = r.cleared
                        ? "<color=#78E888>[ĐÃ DỌN]</color>"
                        : (isHere ? "<color=#F8DC85>[ĐANG Ở ĐÂY]</color>"
                        : (r.discovered ? "<color=#8CC6FF>[ĐÃ PHÁT HIỆN]</color>" : "<color=#94887A>[CHƯA KHÁM PHÁ]</color>"));

                    string puzzleNote = (r.type == RoomType.Puzzle && r.puzzlePillars.Count > 0)
                        ? $" • Ấn: {r.puzzlePillars.Count}"
                        : "";

                    GameUIKit.MakeText("Lbl", card.transform,
                        $"{r.title}{branchTag}  {statusTag}{puzzleNote}",
                        12, FontStyle.Bold, isHere ? GameUIKit.ColGoldBright : GameUIKit.ColParchment, TextAnchor.MiddleLeft,
                        new Vector2(0.12f, 0f), new Vector2(0.76f, 1f), Vector2.zero, Vector2.zero);

                    if (r.cleared || r.discovered || i <= curIdx)
                    {
                        Vector3 dest = r.center;
                        GameUIKit.MakeStyledButton("Tp_" + i, card.transform, "Dịch Chuyển", 11, UIButtonStyle.Secondary,
                            new Vector2(0.77f, 0.14f), new Vector2(0.975f, 0.86f), Vector2.zero, Vector2.zero, () =>
                            {
                                foreach (var h in dw.PartyUnits)
                                {
                                    if (h == null) continue;
                                    if (h.Controller != null) h.Controller.enabled = false;
                                    h.transform.position = dest + new Vector3(UnityEngine.Random.Range(-1.8f, 1.8f), 0f, UnityEngine.Random.Range(-1.8f, 1.8f));
                                    if (h.Controller != null) h.Controller.enabled = true;
                                }
                                gameObject.SetActive(false);
                                OnClosed?.Invoke();
                            });
                    }
                }

                // Right: AI Abyss Director Telemetry & Party Status
                var right = GameUIKit.MakeStyledPanel("DirectorBox", ContentRoot, UIPanelStyle.Character,
                    new Vector2(0.58f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

                GameUIKit.MakeIcon("DirIc", right.transform, GameUIKit.GetIcon("crown"),
                    new Vector2(0.04f, 0.90f), new Vector2(0.13f, 0.975f), Vector2.zero, Vector2.zero);

                GameUIKit.MakeText("DirHdr", right.transform,
                    "PHÂN TÍCH AI ABYSS DIRECTOR",
                    16, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                    new Vector2(0.15f, 0.90f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

                var dir = AbyssDirectorAI.Instance;
                float score = dir != null ? dir.PerformanceScore : 50f;
                float hpScale = dir != null ? dir.HpMultiplier : 1f;
                float dmgScale = dir != null ? dir.AtkMultiplier : 1f;
                int extraDelta = dir != null ? dir.ExtraMonsterDelta : 0;
                string dec = dir != null ? dir.LastDecision : "Đang thu thập dữ liệu chiến đấu...";

                int aliveMonsters = 0;
                foreach (var u in ActorUnit.AllUnits)
                    if (u != null && u.Team == Team.Monster && !u.IsDead) aliveMonsters++;

                string info =
                    $"• <color=#F8DC85>Điểm Hiệu Suất Tổ Đội (0–100):</color> {score:0.0}\n" +
                    $"• <color=#F8DC85>Hệ số Sinh lực Quái vật:</color> x{hpScale:0.00}\n" +
                    $"• <color=#F8DC85>Hệ số Sát thương Quái vật:</color> x{dmgScale:0.00}\n" +
                    $"• <color=#F8DC85>Điều chỉnh Mật độ Quái:</color> {(extraDelta >= 0 ? "+" : "")}{extraDelta}\n" +
                    $"• <color=#F8DC85>Quái vật còn sống trong khu vực:</color> {aliveMonsters}\n\n" +
                    $"<color=#D9B057>PHÁN QUYẾT HIỆN TẠI:</color>\n{dec}\n\n" +
                    $"<color=#F8DC85>CƠ CHẾ CHÚA TỂ 3 GIAI ĐOẠN (ABYSSAL KNIGHT):</color>\n" +
                    $"• Phase 1 (100%–66% HP): Trảm kích xung kích & Quét kiếm.\n" +
                    $"• Phase 2 (66%–33% HP): Triệu hồi Hộ Vệ Xương + Mưa Thiên Thạch.\n" +
                    $"• Phase 3 (<33% HP): Cuồng Nộ Vực Thẳm + Sóng Hủy Diệt Toàn Sân.";

                GameUIKit.MakeText("DirBody", right.transform, info, 13, FontStyle.Normal, GameUIKit.ColParchment, TextAnchor.UpperLeft,
                    new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.87f), Vector2.zero, Vector2.zero);
            }
        }
    }
}

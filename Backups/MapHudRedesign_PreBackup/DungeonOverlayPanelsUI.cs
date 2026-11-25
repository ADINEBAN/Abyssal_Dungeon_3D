using System;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssGame
{
    /// <summary>
    /// Full Dungeon Map & AI Abyss Director Tactical Inspector Modal [M] (ANCIENT GOTHIC EDITION).
    /// Zero emojis: uses authentic engraved medieval & rune icons.
    /// </summary>
    public static class DungeonOverlayPanelsUI
    {
        public static GameObject BuildFullDungeonMapModal(Transform canvasRoot, Action onStateChanged)
        {
            var modal = GameUIKit.MakeStyledPanel("FullDungeonMapModal", canvasRoot, UIPanelStyle.Dialog,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-620f, -390f), new Vector2(620f, 390f));

            GameUIKit.MakeIcon("MapCrest", modal.transform, GameUIKit.GetIcon("portal"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -60f), new Vector2(66f, -20f));

            GameUIKit.MakeText("Title", modal.transform,
                "BẢN ĐỒ CHIẾN THUẬT HẦM NGỤC & BÁO CÁO AI ABYSS DIRECTOR",
                23, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
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

            modal.gameObject.SetActive(false);
            return modal.gameObject;
        }

        private class FullMapModalUpdater : MonoBehaviour
        {
            public Transform ContentRoot;
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

                // Left: 7-Room Dungeon Progression Chain (UI_Panel_SkillTree)
                var left = GameUIKit.MakeStyledPanel("RoomChainBox", ContentRoot, UIPanelStyle.SkillTree,
                    new Vector2(0f, 0f), new Vector2(0.55f, 1f), Vector2.zero, Vector2.zero);

                GameUIKit.MakeText("Hdr", left.transform,
                    $"TIẾN ĐỘ 7 KHU VỰC HẦM NGỤC — TẦNG {GameSession.I.Profile.selectedFloor}",
                    16, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -44f), new Vector2(-18f, -10f));

                int curIdx = dw.CurrentRoomIndex;

                for (int i = 0; i < dw.Rooms.Count; i++)
                {
                    var r = dw.Rooms[i];
                    bool isHere = (i == curIdx);
                    float yMax = 0.90f - i * 0.122f;
                    float yMin = yMax - 0.108f;

                    UIPanelStyle rowStyle = isHere ? UIPanelStyle.Ornate : (r.cleared ? UIPanelStyle.Character : UIPanelStyle.Standard);
                    Color? borderCol = isHere ? GameUIKit.ColGoldBright : (r.type == RoomType.Boss ? GameUIKit.ColDanger : (Color?)null);

                    var card = GameUIKit.MakeStyledPanel("Room_" + i, left.transform, rowStyle,
                        new Vector2(0.04f, yMin), new Vector2(0.96f, yMax), Vector2.zero, Vector2.zero, null, borderCol);

                    string iconKey = r.cleared ? "check_seal" : (isHere ? "crossed_swords" : (r.type == RoomType.Boss ? "skull_boss" : "lock_iron"));
                    GameUIKit.MakeIcon("StateIc", card.transform, GameUIKit.GetIcon(iconKey),
                        new Vector2(0.02f, 0.14f), new Vector2(0.11f, 0.86f), Vector2.zero, Vector2.zero);

                    string statusTag = r.cleared
                        ? "<color=#78E888>[ĐÃ DỌN SẠCH]</color>"
                        : (isHere ? "<color=#F8DC85>[ĐANG GIAO CHIẾN]</color>" : "<color=#BDA685>[CHƯA MỞ]</color>");

                    string puzzleNote = (r.type == RoomType.Puzzle && r.puzzlePillars.Count > 0)
                        ? $"  •  Trụ Cổ Ngữ: {r.puzzlePillars.Count}"
                        : "";

                    GameUIKit.MakeText("Lbl", card.transform,
                        $"Phòng {i + 1}: {r.title}  [{r.type}]  {statusTag}{puzzleNote}",
                        13, FontStyle.Bold, isHere ? GameUIKit.ColGoldBright : GameUIKit.ColParchment, TextAnchor.MiddleLeft,
                        new Vector2(0.13f, 0f), new Vector2(0.76f, 1f), Vector2.zero, Vector2.zero);

                    if (r.cleared || i <= curIdx)
                    {
                        Vector3 dest = r.center;
                        GameUIKit.MakeStyledButton("Tp_" + i, card.transform, "Dịch Chuyển", 12, UIButtonStyle.Secondary,
                            new Vector2(0.77f, 0.16f), new Vector2(0.97f, 0.84f), Vector2.zero, Vector2.zero, () =>
                            {
                                foreach (var h in dw.PartyUnits)
                                {
                                    if (h == null) continue;
                                    if (h.Controller != null) h.Controller.enabled = false;
                                    h.transform.position = dest + new Vector3(UnityEngine.Random.Range(-1.8f, 1.8f), 0f, UnityEngine.Random.Range(-1.8f, 1.8f));
                                    if (h.Controller != null) h.Controller.enabled = true;
                                }
                                gameObject.SetActive(false);
                            });
                    }
                }

                // Right: AI Abyss Director Telemetry & Party Status (UI_Panel_Character)
                var right = GameUIKit.MakeStyledPanel("DirectorBox", ContentRoot, UIPanelStyle.Character,
                    new Vector2(0.57f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

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

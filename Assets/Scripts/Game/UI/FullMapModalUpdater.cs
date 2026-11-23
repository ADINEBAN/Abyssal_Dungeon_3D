using System;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssGame
{
    public class FullMapModalUpdater : MonoBehaviour
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

                // Left: Interactive 2D Branching Dungeon Graph Map with Fog of War (UI_Panel_SkillTree)
                var left = GameUIKit.MakeStyledPanel("BranchingGraphBox", ContentRoot, UIPanelStyle.SkillTree,
                    new Vector2(0f, 0f), new Vector2(0.58f, 1f), Vector2.zero, Vector2.zero);

                int discoveredCount = 0;
                int clearedCount = 0;
                for (int i = 0; i < dw.Rooms.Count; i++)
                {
                    if (dw.Rooms[i].discovered || dw.Rooms[i].visited) discoveredCount++;
                    if (dw.Rooms[i].cleared) clearedCount++;
                }

                GameUIKit.MakeText("Hdr", left.transform,
                    $"ĐỒ THỊ HẦM NGỤC RẼ NHÁNH (TẦNG {GameSession.I.Profile.selectedFloor}) — Mở bản đồ: {discoveredCount}/{dw.Rooms.Count} | Dọn sạch: {clearedCount}/{dw.Rooms.Count}",
                    14, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -40f), new Vector2(-16f, -8f));

                var graphArea = GameUIKit.MakeRect("GraphCanvas", left.transform,
                    new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.90f), Vector2.zero, Vector2.zero);

                int curIdx = dw.CurrentRoomIndex;

                // Helper to map (gridX in [-1..1], gridY in [0..4]) to normalized anchor in graphArea
                Vector2 GridToCenter(int gx, int gy)
                {
                    float nx = 0.50f + gx * 0.31f;
                    float ny = 0.11f + gy * 0.195f;
                    return new Vector2(nx, ny);
                }

                // 1. Draw N/S and E/W Corridor Links between rooms
                for (int i = 0; i < dw.Rooms.Count; i++)
                {
                    var r = dw.Rooms[i];
                    if (!r.visited && !r.discovered) continue;
                    Vector2 cA = GridToCenter(r.gridX, r.gridY);

                    if (r.openNorth)
                    {
                        Vector2 cB = GridToCenter(r.gridX, r.gridY + 1);
                        var link = GameUIKit.MakeRect($"LinkNS_{i}", graphArea,
                            new Vector2(cA.x - 0.012f, cA.y + 0.065f),
                            new Vector2(cA.x + 0.012f, cB.y - 0.065f),
                            Vector2.zero, Vector2.zero);
                        var img = link.gameObject.AddComponent<Image>();
                        img.color = r.visited ? new Color(0.82f, 0.64f, 0.30f, 0.90f) : new Color(0.42f, 0.36f, 0.48f, 0.65f);
                    }

                    if (r.openEast)
                    {
                        Vector2 cB = GridToCenter(r.gridX + 1, r.gridY);
                        var link = GameUIKit.MakeRect($"LinkEW_{i}", graphArea,
                            new Vector2(cA.x + 0.125f, cA.y - 0.010f),
                            new Vector2(cB.x - 0.125f, cA.y + 0.010f),
                            Vector2.zero, Vector2.zero);
                        var img = link.gameObject.AddComponent<Image>();
                        img.color = r.visited ? new Color(0.82f, 0.64f, 0.30f, 0.90f) : new Color(0.42f, 0.36f, 0.48f, 0.65f);
                    }
                }

                // 2. Draw Room Nodes on the 2D Graph
                const float halfW = 0.132f;
                const float halfH = 0.072f;

                for (int i = 0; i < dw.Rooms.Count; i++)
                {
                    var r = dw.Rooms[i];
                    Vector2 c = GridToCenter(r.gridX, r.gridY);
                    bool isHere = (i == curIdx);

                    if (!r.visited && !r.discovered)
                    {
                        // Hidden under Fog of War
                        var fogNode = GameUIKit.MakeStyledPanel("FogRoom_" + i, graphArea, UIPanelStyle.Standard,
                            new Vector2(c.x - halfW, c.y - halfH), new Vector2(c.x + halfW, c.y + halfH),
                            Vector2.zero, Vector2.zero,
                            new Color(0.14f, 0.12f, 0.18f, 0.72f), new Color(0.32f, 0.28f, 0.40f, 0.65f));
                        GameUIKit.MakeText("FogTxt", fogNode.transform, "SƯƠNG MÙ\n[ ? ? ? ]", 11, FontStyle.Italic,
                            new Color(0.58f, 0.52f, 0.68f, 0.85f), TextAnchor.MiddleCenter,
                            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                        continue;
                    }

                    UIPanelStyle nodeStyle = isHere
                        ? UIPanelStyle.Ornate
                        : (r.cleared ? UIPanelStyle.Character : UIPanelStyle.Standard);

                    Color? borderCol = isHere
                        ? GameUIKit.ColGoldBright
                        : (r.type == RoomType.Boss ? GameUIKit.ColDanger
                        : (!r.visited ? new Color(0.55f, 0.48f, 0.68f, 0.9f)
                        : (r.cleared ? new Color(0.38f, 0.85f, 0.48f, 0.95f) : (Color?)null)));

                    Color? bgTint = !r.visited ? new Color(0.22f, 0.19f, 0.28f, 0.88f) : (Color?)null;

                    var node = GameUIKit.MakeStyledPanel("GraphNode_" + i, graphArea, nodeStyle,
                        new Vector2(c.x - halfW, c.y - halfH), new Vector2(c.x + halfW, c.y + halfH),
                        Vector2.zero, Vector2.zero, bgTint, borderCol);

                    string iconKey = r.type == RoomType.Boss ? "skull_boss"
                        : (r.type == RoomType.Puzzle ? "rune"
                        : (r.isSanctuary ? "potion"
                        : (r.index == 2 || r.index == 6 ? "gold"
                        : (r.cleared ? "check_seal" : (isHere ? "crossed_swords" : "lock_iron")))));

                    GameUIKit.MakeIcon("NodeIc", node.transform, GameUIKit.GetIcon(iconKey),
                        new Vector2(0.04f, 0.48f), new Vector2(0.22f, 0.92f), Vector2.zero, Vector2.zero);

                    string dirBadge = r.gridX < 0 ? "[TÂY]" : (r.gridX > 0 ? "[ĐÔNG]" : "[TRỤC BẮC]");
                    string stateLine = !r.visited
                        ? "<color=#B8A8D8>Chưa đặt chân</color>"
                        : (isHere ? "<color=#F8DC85>ĐANG Ở ĐÂY</color>"
                        : (r.cleared ? "<color=#78E888>Đã dọn sạch</color>" : "<color=#F08868>Đang giao chiến</color>"));

                    GameUIKit.MakeText("Title", node.transform,
                        $"{dirBadge} {r.subTypeLabel}\n{stateLine}",
                        10, FontStyle.Bold, isHere ? GameUIKit.ColGoldBright : GameUIKit.ColParchment, TextAnchor.UpperLeft,
                        new Vector2(0.24f, 0.36f), new Vector2(0.96f, 0.94f), Vector2.zero, Vector2.zero);

                    if (r.visited || r.cleared)
                    {
                        Vector3 dest = r.center;
                        GameUIKit.MakeStyledButton("Tp_" + i, node.transform, isHere ? "Vị Trí Hiện Tại" : "Dịch Chuyển", 10,
                            isHere ? UIButtonStyle.Primary : UIButtonStyle.Secondary,
                            new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.34f), Vector2.zero, Vector2.zero, () =>
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
                    else
                    {
                        GameUIKit.MakeText("Hint", node.transform, "Đi qua cửa nối để mở", 9, FontStyle.Italic,
                            GameUIKit.ColMuted, TextAnchor.MiddleCenter,
                            new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.34f), Vector2.zero, Vector2.zero);
                    }
                }

                // Right: AI Abyss Director Telemetry & Legend (UI_Panel_Character)
                var right = GameUIKit.MakeStyledPanel("DirectorBox", ContentRoot, UIPanelStyle.Character,
                    new Vector2(0.60f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

                GameUIKit.MakeIcon("DirIc", right.transform, GameUIKit.GetIcon("crown"),
                    new Vector2(0.04f, 0.90f), new Vector2(0.13f, 0.975f), Vector2.zero, Vector2.zero);

                GameUIKit.MakeText("DirHdr", right.transform,
                    "CHÚ GIẢI NHÁNH & AI DIRECTOR",
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

                var curRoom = (curIdx >= 0 && curIdx < dw.Rooms.Count) ? dw.Rooms[curIdx] : null;
                string curBranchInfo = curRoom != null
                    ? $"{curRoom.title} ({curRoom.subTypeLabel})"
                    : "Không xác định";

                string info =
                    $"<color=#F8DC85>VỊ TRÍ HIỆN TẠI:</color>\n• {curBranchInfo}\n\n" +
                    $"<color=#F8DC85>CẤU TRÚC NHÁNH HẦM NGỤC (FOG OF WAR):</color>\n" +
                    $"• <color=#78E888>Trục Chính (Bắc):</color> Khởi Đầu → Tiền Đồn → Pháo Đài Tinh Anh → Mini-Boss → Boss Cuối.\n" +
                    $"• <color=#68E0F8>Nhánh Đông/Tây Tầng Dưới:</color> Mật Thất Kho Báu / Tế Đàn Nguyền Rủa & Điện Giải Đố Trụ Cổ Ngữ.\n" +
                    $"• <color=#F8DC85>Nhánh Đông/Tây Tầng Trên:</color> Thánh Đường Đài Hồi Phục & Hầm Ngục Phong Ấn Huyết Tinh.\n\n" +
                    $"<color=#D9B057>THÔNG SỐ AI ABYSS DIRECTOR:</color>\n" +
                    $"• Điểm Hiệu Suất Tổ Đội: <color=#F8DC85>{score:0.0}/100</color>\n" +
                    $"• Hệ số Sinh lực / Sát thương Quái: <color=#F8DC85>x{hpScale:0.00} / x{dmgScale:0.00}</color>\n" +
                    $"• Điều chỉnh Mật độ Quái: <color=#F8DC85>{(extraDelta >= 0 ? "+" : "")}{extraDelta}</color> (Quái đang sống: {aliveMonsters})\n\n" +
                    $"<color=#D9B057>PHÁN QUYẾT:</color> {dec}";

                GameUIKit.MakeText("DirBody", right.transform, info, 12, FontStyle.Normal, GameUIKit.ColParchment, TextAnchor.UpperLeft,
                    new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.87f), Vector2.zero, Vector2.zero);

                var parentCanvas = GetComponentInParent<Canvas>();
                if (parentCanvas != null) GameUIKit.OptimizeCanvasHierarchy(parentCanvas);
            }
        }
    }


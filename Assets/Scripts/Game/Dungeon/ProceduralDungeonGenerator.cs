using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Phase 6: Seeded Deterministic Dungeon Layout & Pacing Generator + Environmental Events.
    /// Follows `level-design` (sawtooth tension/rest pacing curve) and `procedural-gen` (seeded RNG + graph reachability validation).
    /// </summary>
    public static class ProceduralDungeonGenerator
    {
        public const float GridCellPitch = 34f;

        public struct RoomNodeSpec
        {
            public int index;
            public int gridX;
            public int gridY;
            public RoomType type;
            public DungeonBiome biome;
            public float width;
            public float length;
            public float tensionTarget;
            public bool hasEnvironmentalEvent;
            public string eventTitle;
            public string customTitle;
            public string subTypeLabel;
            public bool isSanctuary;
            public bool openNorth;
            public bool openSouth;
            public bool openWest;
            public bool openEast;
            public int[] connectedIndices;
        }

        /// <summary>
        /// Generates a deterministic 9-room 2D Branching Dungeon Graph (North/South/East/West branches)
        /// using a seed derived from floor + run index:
        /// - (0,0) Entrance Sanctuary
        /// - (0,1) Combat Crossroads -> branches E/W to Treasure/Cursed Altar & Runic Pillar Puzzle
        /// - (0,2) Elite Crossroads -> branches E/W to Healing Shrine Sanctuary & Blood Seal Altar Vault
        /// - (0,3) Mini-Boss Chamber
        /// - (0,4) Final Boss Arena
        /// </summary>
        public static RoomNodeSpec[] GenerateSeededDungeon(int floor, int seedOffset, out bool graphValid)
        {
            int seed = (floor * 7919) ^ (seedOffset * 104729) ^ 0x5A17C3;
            var rng = new System.Random(seed);

            bool flipTier1 = rng.Next(0, 2) == 1;
            bool flipTier2 = rng.Next(0, 2) == 1;

            int treasureGridX = flipTier1 ? 1 : -1;
            int puzzleGridX   = flipTier1 ? -1 : 1;

            int shrineGridX   = flipTier2 ? 1 : -1;
            int bloodSealGridX = flipTier2 ? -1 : 1;

            var specs = new RoomNodeSpec[]
            {
                // 0: (0,0) Entrance
                new RoomNodeSpec
                {
                    index = 0, gridX = 0, gridY = 0,
                    type = RoomType.Entrance, biome = DungeonBiome.ForgottenTemple,
                    width = 24f, length = 24f, tensionTarget = 0.10f,
                    hasEnvironmentalEvent = false, eventTitle = "",
                    customTitle = "Sảnh Khởi Đầu — Cổng Thánh Địa",
                    subTypeLabel = "Khởi Đầu", isSanctuary = true
                },
                // 1: (0,1) Combat Crossroads
                new RoomNodeSpec
                {
                    index = 1, gridX = 0, gridY = 1,
                    type = RoomType.Combat, biome = DungeonBiome.AncientDungeon,
                    width = 24f, length = 24f, tensionTarget = 0.50f,
                    hasEnvironmentalEvent = true, eventTitle = "Tế Đàn Huyết Nguyệt",
                    customTitle = "Ngã Tư Tiền Đồn Cổ Đại",
                    subTypeLabel = "Phòng Quái", isSanctuary = false
                },
                // 2: (-1,1) or (1,1) Side-Branch: Treasure & Cursed Choice Altar
                new RoomNodeSpec
                {
                    index = 2, gridX = treasureGridX, gridY = 1,
                    type = RoomType.Combat, biome = DungeonBiome.AbyssalCave,
                    width = 24f, length = 24f, tensionTarget = 0.65f,
                    hasEnvironmentalEvent = true, eventTitle = "Tế Đàn Nguyền Rủa",
                    customTitle = "Mật Thất Kho Báu & Tế Đàn Nguyền Rủa",
                    subTypeLabel = "Kho Báu / Tế Đàn", isSanctuary = false
                },
                // 3: (1,1) or (-1,1) Side-Branch: Runic Pillar Puzzle Room
                new RoomNodeSpec
                {
                    index = 3, gridX = puzzleGridX, gridY = 1,
                    type = RoomType.Puzzle, biome = DungeonBiome.CursedForest,
                    width = 24f, length = 24f, tensionTarget = 0.45f,
                    hasEnvironmentalEvent = false, eventTitle = "Ấn Phong Cổ Tự",
                    customTitle = "Điện Giải Đố Trụ Cổ Ngữ",
                    subTypeLabel = "Giải Đố Cổ Ngữ", isSanctuary = false
                },
                // 4: (0,2) Elite Crossroads
                new RoomNodeSpec
                {
                    index = 4, gridX = 0, gridY = 2,
                    type = RoomType.Elite, biome = DungeonBiome.UndergroundFortress,
                    width = 24f, length = 24f, tensionTarget = 0.85f,
                    hasEnvironmentalEvent = true, eventTitle = "Lõi Năng Lượng Cổ Đại",
                    customTitle = "Pháo Đài Hộ Vệ Tinh Anh",
                    subTypeLabel = "Quái Tinh Anh", isSanctuary = false
                },
                // 5: (-1,2) or (1,2) Side-Branch: Healing Shrine Sanctuary
                new RoomNodeSpec
                {
                    index = 5, gridX = shrineGridX, gridY = 2,
                    type = RoomType.Entrance, biome = DungeonBiome.ForgottenTemple,
                    width = 24f, length = 24f, tensionTarget = 0.15f,
                    hasEnvironmentalEvent = false, eventTitle = "Đài Hồi Phục Thánh Quang",
                    customTitle = "Thánh Đường Đài Hồi Phục",
                    subTypeLabel = "Đài Hồi Phục", isSanctuary = true
                },
                // 6: (1,2) or (-1,2) Side-Branch: Blood Seal Altar & Vault
                new RoomNodeSpec
                {
                    index = 6, gridX = bloodSealGridX, gridY = 2,
                    type = RoomType.Combat, biome = DungeonBiome.AbyssalCave,
                    width = 24f, length = 24f, tensionTarget = 0.80f,
                    hasEnvironmentalEvent = true, eventTitle = "Phong Ấn Huyết Tinh",
                    customTitle = "Hầm Ngục Phong Ấn Huyết Tinh",
                    subTypeLabel = "Tế Đàn Phong Ấn", isSanctuary = false
                },
                // 7: (0,3) Mini-Boss Chamber
                new RoomNodeSpec
                {
                    index = 7, gridX = 0, gridY = 3,
                    type = RoomType.MiniBoss, biome = DungeonBiome.CrystalChamber,
                    width = 24f, length = 24f, tensionTarget = 0.92f,
                    hasEnvironmentalEvent = false, eventTitle = "",
                    customTitle = "Điện Pha Lê Hộ Pháp (Mini-Boss)",
                    subTypeLabel = "Mini-Boss", isSanctuary = false
                },
                // 8: (0,4) Final Boss Arena
                new RoomNodeSpec
                {
                    index = 8, gridX = 0, gridY = 4,
                    type = RoomType.Boss, biome = DungeonBiome.BossArena,
                    width = 32f, length = 32f, tensionTarget = 1.00f,
                    hasEnvironmentalEvent = false, eventTitle = "",
                    customTitle = "Thánh Điện Vực Thẳm — Abyssal Knight",
                    subTypeLabel = "Boss Cuối", isSanctuary = false
                }
            };

            // Compute N/S/E/W adjacency & doorway flags from 2D grid coordinates
            for (int i = 0; i < specs.Length; i++)
            {
                var conn = new List<int>();
                for (int j = 0; j < specs.Length; j++)
                {
                    if (i == j) continue;
                    int dx = specs[j].gridX - specs[i].gridX;
                    int dy = specs[j].gridY - specs[i].gridY;
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) == 1)
                    {
                        conn.Add(j);
                        if (dx == 0 && dy == 1) specs[i].openNorth = true;
                        else if (dx == 0 && dy == -1) specs[i].openSouth = true;
                        else if (dx == -1 && dy == 0) specs[i].openWest = true;
                        else if (dx == 1 && dy == 0) specs[i].openEast = true;
                    }
                }
                specs[i].connectedIndices = conn.ToArray();
            }

            graphValid = ValidateDungeonGraphReachability(specs);
            return specs;
        }

        /// <summary>
        /// Validates that every room in the branching 2D dungeon graph is reachable from Entrance (0) via BFS.
        /// </summary>
        public static bool ValidateDungeonGraphReachability(RoomNodeSpec[] specs)
        {
            if (specs == null || specs.Length < 2) return false;
            var visited = new bool[specs.Length];
            var queue = new Queue<int>();
            queue.Enqueue(0);
            visited[0] = true;
            int reached = 0;

            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                reached++;
                var neighbors = specs[cur].connectedIndices;
                if (neighbors == null) continue;
                for (int k = 0; k < neighbors.Length; k++)
                {
                    int nb = neighbors[k];
                    if (nb >= 0 && nb < specs.Length && !visited[nb])
                    {
                        visited[nb] = true;
                        queue.Enqueue(nb);
                    }
                }
            }
            return reached == specs.Length && visited[specs.Length - 1];
        }

        public static bool ValidateDungeonGraphReachability(int roomCount)
        {
            return roomCount >= 2;
        }
    }

    /// <summary>
    /// Interactive Environmental Event in Dungeon rooms (e.g., Blood Moon Altar, Ancient Energy Core, Abyssal Soul Well).
    /// Interacting triggers an arcane shockwave + optional ambush guardian and grants Party Buffs + Rare/Epic Loot.
    /// </summary>
    public class WorldEnvironmentalEvent : MonoBehaviour, IWorldInteractable
    {
        public Vector3 Position => transform.position;
        public float InteractRadius => 3.2f;
        public bool Completed { get; private set; }

        private string _eventName;
        private int _roomIndex;
        private Transform _floatingCore;
        private Light _eventLight;

        public static WorldEnvironmentalEvent Create(Transform parent, Vector3 pos, string eventName, int roomIndex)
        {
            var go = new GameObject($"EnvEvent_{roomIndex}_{eventName}");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            Color themeCol = roomIndex == 1
                ? new Color(0.95f, 0.22f, 0.32f)
                : (roomIndex == 2 ? new Color(0.25f, 0.78f, 1.0f) : new Color(0.72f, 0.28f, 1.0f));

            var stoneMat = PbrMaterialLibrary.GothicStone(new Color(0.52f, 0.48f, 0.62f));
            var crystalMat = PbrMaterialLibrary.AbyssalCrystal(themeCol, themeCol * 1.3f, 3.2f);

            ObjMeshLoader.SpawnPiece("RunicPillarAltar", go.transform, Vector3.zero, Quaternion.identity, new Vector3(1.1f, 0.85f, 1.1f), stoneMat, addBoxCollider: true);
            var core = ObjMeshLoader.SpawnPiece("Equip_Amulet_Talisman", go.transform, new Vector3(0f, 1.75f, 0f), Quaternion.identity, Vector3.one * 2.2f, crystalMat, addBoxCollider: false);

            var lightGo = new GameObject("EventLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.9f, 0f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = themeCol;
            l.range = 8.5f;
            l.intensity = 2.2f;

            var ev = go.AddComponent<WorldEnvironmentalEvent>();
            ev._eventName = string.IsNullOrEmpty(eventName) ? "Tế Đàn Cổ Đại" : eventName;
            ev._roomIndex = roomIndex;
            ev._floatingCore = core.transform;
            ev._eventLight = l;
            HeroPlayerInput.Interactables.Add(ev);
            return ev;
        }

        private void Update()
        {
            if (_floatingCore != null && !Completed)
            {
                _floatingCore.localPosition = new Vector3(0f, 1.75f + Mathf.Sin(Time.time * 3.2f) * 0.12f, 0f);
                _floatingCore.Rotate(0f, 75f * Time.deltaTime, 0f, Space.World);
            }
        }

        public bool CanInteract() => !Completed;
        public string GetPrompt() => $"[F] Kích hoạt Sự Kiện: {_eventName} (Thử thách & Buff Tổ Đội)";

        public void Interact(ActorUnit user)
        {
            if (Completed) return;
            Completed = true;

            if (_eventLight != null) _eventLight.intensity = 0.5f;
            if (_floatingCore != null) _floatingCore.localScale *= 0.65f;

            GameAudio.I.PlaySfx(GameSfx.Heal, 1f);
            CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up * 1.4f, new Color(1f, 0.65f, 0.22f), 4.2f, 0.65f);

            var dw = DungeonWorldBuilder.Instance;
            if (dw != null)
            {
                // Grant Party-wide Haste + Shield + Empower buff
                foreach (var h in dw.PartyUnits)
                {
                    if (h != null && !h.IsDead)
                    {
                        h.AddShield(h.MaxHp * 0.25f);
                        h.ApplyStatus(StatusType.Rage, 14f, 0.25f, user);
                        h.ReceiveHeal(h.MaxHp * 0.25f, user);
                        CharacterVisualFactory.SpawnFloatingText(h.transform.position + Vector3.up * 2.3f,
                            $"PHƯỚC LÀNH {_eventName.ToUpper()}!", new Color(1f, 0.86f, 0.32f), 1.05f);
                    }
                }

                int floor = GameSession.I.Profile.selectedFloor;
                dw.AwardExpAndGold(60 * floor, 55 * floor);
                EquipItem reward = LootRoller.RollItem(floor, 2, 0.35f);
                LootOrbPickup.Spawn(transform.position + new Vector3(0f, 0f, -1.1f), reward);
                dw.ShowBanner($"SỰ KIỆN HOÀN TẤT: {_eventName} — Toàn đội nhận Lá Chắn, Tốc Độ & Trang Bị Hiếm!", 3.6f);
            }
        }
    }

    /// <summary>
    /// Phase 5: Ancient Runic Lore Monolith revealing the fall of Aether Haven Sanctuary across dungeon floors,
    /// with a concise Gothic Dialogue Modal and an Insight Blessing (+EXP, Gold & Shield).
    /// </summary>
    public class WorldLoreMonolith : MonoBehaviour, IWorldInteractable
    {
        public Vector3 Position => transform.position;
        public float InteractRadius => 3.2f;
        public bool ReadOnce { get; private set; }

        private int _chapterIndex;
        private Transform _floatingTablet;
        private static GameObject _activeLoreModal;

        public static WorldLoreMonolith Create(Transform parent, Vector3 pos, int chapterIndex)
        {
            var go = new GameObject($"LoreMonolith_Ch{chapterIndex}");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            Color runeCyan = new Color(0.28f, 0.88f, 1.0f);
            var stoneMat = PbrMaterialLibrary.GothicStone(new Color(0.50f, 0.56f, 0.68f));
            var crystalMat = PbrMaterialLibrary.AbyssalCrystal(runeCyan * 0.85f, runeCyan * 1.4f, 3.0f);

            ObjMeshLoader.SpawnPiece("RunicPillarAltar", go.transform, Vector3.zero, Quaternion.identity, new Vector3(0.95f, 1.15f, 0.95f), stoneMat, addBoxCollider: true);
            var tablet = ObjMeshLoader.SpawnPiece("AbyssalCrystalCluster", go.transform, new Vector3(0f, 2.25f, 0f), Quaternion.identity, new Vector3(0.32f, 0.48f, 0.32f), crystalMat, addBoxCollider: false);

            var lGo = new GameObject("LoreGlow");
            lGo.transform.SetParent(go.transform, false);
            lGo.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            var lt = lGo.AddComponent<Light>();
            lt.type = LightType.Point;
            lt.color = runeCyan;
            lt.range = 6.8f;
            lt.intensity = 1.8f;
            lt.shadows = LightShadows.None;

            var m = go.AddComponent<WorldLoreMonolith>();
            m._chapterIndex = chapterIndex;
            m._floatingTablet = tablet != null ? tablet.transform : null;
            HeroPlayerInput.Interactables.Add(m);
            return m;
        }

        private void Update()
        {
            if (_floatingTablet != null)
            {
                _floatingTablet.localPosition = new Vector3(0f, 2.22f + Mathf.Sin(Time.time * 2.6f) * 0.09f, 0f);
                _floatingTablet.Rotate(0f, 48f * Time.deltaTime, 0f, Space.World);
            }
        }

        public bool CanInteract() => true;

        public string GetPrompt() => ReadOnce
            ? "[F] Đọc lại Bia Đá Cổ Ngữ — Biên Niên Sử Aether Haven"
            : "[F] Giải mã Bia Đá Cổ Ngữ — Bí Ẩn Thánh Địa Aether Haven (+EXP & Khiên)";

        public void Interact(ActorUnit user)
        {
            int floor = GameSession.I.Profile.selectedFloor;
            if (!ReadOnce)
            {
                ReadOnce = true;
                GameAudio.I.PlaySfx(GameSfx.Heal, 0.85f);
                var dw = DungeonWorldBuilder.Instance;
                if (dw != null)
                {
                    dw.AwardExpAndGold(45 * floor, 35 * floor);
                    foreach (var h in dw.PartyUnits)
                    {
                        if (h != null && !h.IsDead)
                            h.AddShield(h.MaxHp * 0.20f);
                    }
                }
            }
            ShowLoreDialogueModal(floor, _chapterIndex);
        }

        private static void ShowLoreDialogueModal(int floor, int chapterIdx)
        {
            if (_activeLoreModal != null) Destroy(_activeLoreModal);
            Canvas canvas = GameUIKit.GetModalCanvas();
            if (canvas == null) return;

            GetLoreContent(floor, chapterIdx, out string title, out string speakerLine, out string bodyText);

            var ov = GameUIKit.MakeRect("LoreDialogueModal", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var dim = ov.gameObject.AddComponent<UnityEngine.UI.Image>();
            dim.color = new Color(0.03f, 0.02f, 0.05f, 0.78f);
            _activeLoreModal = ov.gameObject;

            var box = GameUIKit.MakeStyledPanel("LoreCard", ov.transform, UIPanelStyle.Dialog,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420f, -235f), new Vector2(420f, 235f),
                null, GameUIKit.ColGoldBright);

            GameUIKit.MakeIcon("RuneIc", box.transform, GameUIKit.GetIcon("rune"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -56f), new Vector2(60f, -20f));

            GameUIKit.MakeText("Title", box.transform, title, 20, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(72f, -60f), new Vector2(-24f, -16f));

            GameUIKit.MakeGothicDivider("Div", box.transform,
                new Vector2(0.04f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, -70f), new Vector2(0f, -54f));

            GameUIKit.MakeText("Body", box.transform, bodyText, 14, FontStyle.Normal, GameUIKit.ColParchment, TextAnchor.UpperLeft,
                new Vector2(0.05f, 0.34f), new Vector2(0.95f, 0.84f), Vector2.zero, Vector2.zero);

            var quoteBox = GameUIKit.MakeStyledPanel("PartyDialogue", box.transform, UIPanelStyle.Notification,
                new Vector2(0.05f, 0.17f), new Vector2(0.95f, 0.32f), Vector2.zero, Vector2.zero);
            GameUIKit.MakeText("QuoteTxt", quoteBox.transform, speakerLine, 13, FontStyle.Italic, new Color(0.68f, 0.94f, 1f), TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(16f, 4f), new Vector2(-16f, -4f));

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            GameUIKit.MakeStyledButton("CloseBtn", box.transform, "TIẾP TỤC HÀNH TRÌNH [ĐÓNG]", 14, UIButtonStyle.Primary,
                new Vector2(0.30f, 0.03f), new Vector2(0.70f, 0.14f), Vector2.zero, Vector2.zero, () =>
                {
                    if (_activeLoreModal != null) Destroy(_activeLoreModal);
                });
        }

        private static void GetLoreContent(int floor, int chapter, out string title, out string speakerLine, out string body)
        {
            int key = ((floor - 1) * 2 + chapter) % 3;
            if (key == 0)
            {
                title = $"BIA ĐÁ CỔ NGỮ TẦNG {floor} — SỰ SỤP ĐỔ CỦA THÁNH ĐỊA AETHER HAVEN";
                body =
                    "\"Ba trăm năm trước, Thánh Địa Aether Haven từng là ngọn hải đăng rực rỡ nhất của loài người, được nuôi dưỡng bởi Lõi Tinh Thể Vĩnh Cửu.\n\n" +
                    "Khi vết nứt Hư Không đầu tiên xuất hiện, Đại Hiệp Sĩ Vespera đã tự nguyện dùng thân mình làm vật chứa phong ấn. Nhưng bóng tối từ Vực Thẳm đã ăn mòn trái tim ngài, biến vị anh hùng năm xưa thành Chúa Tể Hầm Ngục — Abyssal Knight.\"";
                speakerLine = "Lyra (Knight): \"Linh hồn của Ngài Vespera vẫn đang bị giam cầm dưới đáy Thánh Điện... Chúng ta phải giải thoát cho ngài ấy!\"";
            }
            else if (key == 1)
            {
                title = $"BIA ĐÁ CỔ NGỮ TẦNG {floor} — LỜI THỀ CỦA BỐN HỘ VỆ";
                body =
                    "\"Những kẻ bước vào Hầm Ngục Vực Thẳm hãy nhớ: Lõi Aether đã vỡ thành nhiều mảnh, tạo nên những ngã rẽ không gian biến đổi không ngừng.\n\n" +
                    "Phía sau các cánh cửa nhánh là Tế Đàn Huyết Thệ và các Trụ Phong Ấn Cổ Ngữ. Chỉ những ai biết phối hợp Phá Giáp (Stagger) và Thanh Tẩy mới vượt qua được đội quân Tinh Anh canh giữ nơi này.\"";
                speakerLine = "Kael (Mage): \"Các dòng chảy ma năng ở ngã rẽ Đông – Tây đang hội tụ về phía Bắc. Hãy tận dụng các Tế Đàn ở nhánh phụ trước khi tiến vào Điện Boss!\"";
            }
            else
            {
                title = $"BIA ĐÁ CỔ NGỮ TẦNG {floor} — PHÁN QUYẾT NƠI ĐÁY VỰC";
                body =
                    "\"Khi ba Trụ Ấn Phong được thắp sáng bằng dòng năng lượng vàng kim, bức màn sương mù của Vực Thẳm sẽ hé lộ con đường dẫn tới ngai vàng của Abyssal Knight.\n\n" +
                    "Đừng để cơn cuồng nộ của Hư Không khuất phục. Ánh sáng của Aether Haven chỉ hồi sinh khi thanh đại kiếm bị nguyền rủa kia được thanh tẩy hoàn toàn.\"";
                speakerLine = "Arthur (Cleric): \"Ánh sáng Thánh Quang sẽ dẫn lối cho tổ đội chúng ta xuyên qua bóng tối của Aether Haven!\"";
            }
        }
    }

    /// <summary>
    /// Phase 5: Interactive Choice Altar (Tế Đàn Lựa Chọn) placed in Dungeon Side-Branches:
    /// Lets the player choose between:
    /// 1) Sacrificing 20% current HP for a floor-long +28% ATK & Crit Blood Blessing + Rare Loot,
    /// 2) Breaking the Blood Seal to summon an Elite Guardian in exchange for guaranteed Epic/Legendary (Tím/Cam) gear,
    /// 3) Safe Runic Cleansing (+35% HP & Mana recovery).
    /// </summary>
    public class WorldChoiceAltar : MonoBehaviour, IWorldInteractable
    {
        public Vector3 Position => transform.position;
        public float InteractRadius => 3.4f;
        public bool Resolved { get; private set; }

        private string _altarTitle;
        private Transform _floatingRelic;
        private Light _altarLight;
        private static GameObject _activeChoiceModal;

        public static WorldChoiceAltar Create(Transform parent, Vector3 pos, string altarTitle)
        {
            var go = new GameObject($"ChoiceAltar_{altarTitle}");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            Color crimsonGold = new Color(1.0f, 0.32f, 0.24f);
            var stoneMat = PbrMaterialLibrary.GothicStone(new Color(0.55f, 0.42f, 0.46f));
            var crystalMat = PbrMaterialLibrary.AbyssalCrystal(crimsonGold, new Color(1f, 0.65f, 0.22f) * 1.4f, 3.4f);

            ObjMeshLoader.SpawnPiece("RunicPillarAltar", go.transform, Vector3.zero, Quaternion.identity, new Vector3(1.18f, 0.92f, 1.18f), stoneMat, addBoxCollider: true);
            var relic = ObjMeshLoader.SpawnPiece("Equip_Amulet_Talisman", go.transform, new Vector3(0f, 1.85f, 0f), Quaternion.identity, Vector3.one * 2.4f, crystalMat, addBoxCollider: false);

            var lGo = new GameObject("ChoiceAltarLight");
            lGo.transform.SetParent(go.transform, false);
            lGo.transform.localPosition = new Vector3(0f, 2.0f, 0f);
            var lt = lGo.AddComponent<Light>();
            lt.type = LightType.Point;
            lt.color = crimsonGold;
            lt.range = 8.5f;
            lt.intensity = 2.4f;
            lt.shadows = LightShadows.None;

            var ca = go.AddComponent<WorldChoiceAltar>();
            ca._altarTitle = string.IsNullOrEmpty(altarTitle) ? "Tế Đàn Lựa Chọn Cổ Ngữ" : altarTitle;
            ca._floatingRelic = relic != null ? relic.transform : null;
            ca._altarLight = lt;
            HeroPlayerInput.Interactables.Add(ca);
            return ca;
        }

        private void Update()
        {
            if (_floatingRelic != null && !Resolved)
            {
                _floatingRelic.localPosition = new Vector3(0f, 1.85f + Mathf.Sin(Time.time * 3.4f) * 0.12f, 0f);
                _floatingRelic.Rotate(0f, 82f * Time.deltaTime, 0f, Space.World);
            }
        }

        public bool CanInteract() => !Resolved;
        public string GetPrompt() => $"[F] Tương tác {_altarTitle} (Lựa Chọn Hiến Tế / Triệu Hồi Tinh Anh)";

        public void Interact(ActorUnit user)
        {
            if (Resolved) return;
            OpenChoiceModal();
        }

        private void OpenChoiceModal()
        {
            if (_activeChoiceModal != null) Destroy(_activeChoiceModal);
            Canvas canvas = GameUIKit.GetModalCanvas();
            if (canvas == null) return;

            var ov = GameUIKit.MakeRect("ChoiceAltarModal", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var dim = ov.gameObject.AddComponent<UnityEngine.UI.Image>();
            dim.color = new Color(0.04f, 0.02f, 0.03f, 0.82f);
            _activeChoiceModal = ov.gameObject;

            var box = GameUIKit.MakeStyledPanel("AltarBox", ov.transform, UIPanelStyle.Dialog,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-460f, -265f), new Vector2(460f, 265f),
                null, GameUIKit.ColGoldBright);

            GameUIKit.MakeIcon("AltarIc", box.transform, GameUIKit.GetIcon("skull_boss"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -58f), new Vector2(62f, -20f));

            GameUIKit.MakeText("Title", box.transform,
                $"{_altarTitle.ToUpper()} — PHÁN QUYẾT AETHER HAVEN",
                21, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(74f, -62f), new Vector2(-24f, -16f));

            GameUIKit.MakeGothicDivider("Div", box.transform,
                new Vector2(0.04f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, -72f), new Vector2(0f, -56f));

            GameUIKit.MakeText("Desc", box.transform,
                "Một luồng cổ ngữ từ thời đại Aether Haven vang vọng trong tâm trí bạn. Hãy chọn cách thức tương tác với Tế Đàn:",
                14, FontStyle.Normal, GameUIKit.ColParchment, TextAnchor.MiddleLeft,
                new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.86f), Vector2.zero, Vector2.zero);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Option 1: Sacrifice 20% Current HP -> +28% ATK & Crit Blessing + Rare Item
            var opt1 = GameUIKit.MakeStyledPanel("Opt1", box.transform, UIPanelStyle.Ornate,
                new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.72f), Vector2.zero, Vector2.zero);
            GameUIKit.MakeText("Txt1", opt1.transform,
                "<color=#FF7868>[1] HIẾN TẾ HUYẾT THỆ (-20% Máu Hiện Tại)</color>\n" +
                "Toàn đội nhận <color=#F8DC85>Phước Lành Cuồng Nộ (+28% Sát Thương, +12% Chí Mạng & Tốc Đánh)</color> + Rớt 1 Trang Bị Hiếm.",
                13, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.08f), new Vector2(0.74f, 0.92f), Vector2.zero, Vector2.zero);
            GameUIKit.MakeStyledButton("Btn1", opt1.transform, "HIẾN TẾ MÁU", 13, UIButtonStyle.Danger,
                new Vector2(0.76f, 0.16f), new Vector2(0.97f, 0.84f), Vector2.zero, Vector2.zero, () =>
                {
                    ExecuteChoice(1);
                    if (_activeChoiceModal != null) Destroy(_activeChoiceModal);
                });

            // Option 2: Break Blood Seal -> Summon Elite Guardian -> Guaranteed Epic/Legendary (Tím/Cam) Gear
            var opt2 = GameUIKit.MakeStyledPanel("Opt2", box.transform, UIPanelStyle.Ornate,
                new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.50f), Vector2.zero, Vector2.zero);
            GameUIKit.MakeText("Txt2", opt2.transform,
                "<color=#D888FF>[2] PHÁ GIẢI PHONG ẤN (Triệu Hồi Tinh Anh Hộ Vệ)</color>\n" +
                "Triệu hồi 1 <color=#FF9848>Quái Tinh Anh Hộ Pháp</color> ngay tại phòng và nhận ngay <color=#E078FF>Trang Bị Cực Phẩm Tím / Cam (Epic+)</color>!",
                13, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.08f), new Vector2(0.74f, 0.92f), Vector2.zero, Vector2.zero);
            GameUIKit.MakeStyledButton("Btn2", opt2.transform, "PHÁ PHONG ẤN", 13, UIButtonStyle.Primary,
                new Vector2(0.76f, 0.16f), new Vector2(0.97f, 0.84f), Vector2.zero, Vector2.zero, () =>
                {
                    ExecuteChoice(2);
                    if (_activeChoiceModal != null) Destroy(_activeChoiceModal);
                });

            // Option 3: Safe Cleansing Prayer -> +35% HP & Mana
            var opt3 = GameUIKit.MakeStyledPanel("Opt3", box.transform, UIPanelStyle.Character,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.28f), Vector2.zero, Vector2.zero);
            GameUIKit.MakeText("Txt3", opt3.transform,
                "<color=#78F098>[3] THANH TẨY CỔ NGỮ AN TOÀN</color>\n" +
                "Hấp thụ năng lượng hiền hòa từ tế đàn để <color=#78F098>Hồi 35% Máu, Mana & Giải Trừ Hiệu Ứng Xấu</color> cho toàn đội.",
                13, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.08f), new Vector2(0.74f, 0.92f), Vector2.zero, Vector2.zero);
            GameUIKit.MakeStyledButton("Btn3", opt3.transform, "THANH TẨY", 13, UIButtonStyle.Secondary,
                new Vector2(0.76f, 0.16f), new Vector2(0.97f, 0.84f), Vector2.zero, Vector2.zero, () =>
                {
                    ExecuteChoice(3);
                    if (_activeChoiceModal != null) Destroy(_activeChoiceModal);
                });
        }

        private void ExecuteChoice(int choice)
        {
            if (Resolved) return;
            Resolved = true;
            if (_altarLight != null) _altarLight.intensity = 0.6f;
            if (_floatingRelic != null) _floatingRelic.localScale *= 0.6f;

            var dw = DungeonWorldBuilder.Instance;
            if (dw == null) return;
            int floor = GameSession.I.Profile.selectedFloor;

            if (choice == 1)
            {
                // Sacrifice 20% current HP -> Permanent +28% ATK & +12% Crit for the floor + Rage/SpeedUp + Rare Item
                GameAudio.I.PlaySfx(GameSfx.Explosion, 0.85f);
                CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up * 1.4f, new Color(1f, 0.25f, 0.22f), 4.2f, 0.65f);
                foreach (var h in dw.PartyUnits)
                {
                    if (h == null || h.IsDead) continue;
                    h.Hp = Mathf.Max(15f, Mathf.Round(h.Hp * 0.80f));
                    h.Atk = Mathf.Round(h.Atk * 1.28f);
                    h.CritChance = Mathf.Clamp(h.CritChance + 0.12f, 0f, 0.90f);
                    h.ApplyStatus(StatusType.AtkUp, 45f, 0.28f, h);
                    h.ApplyStatus(StatusType.Rage, 30f, 0.30f, h);
                    CharacterVisualFactory.SpawnFloatingText(h.transform.position + Vector3.up * 2.3f,
                        "HUYẾT THỆ: +28% SÁT THƯƠNG!", new Color(1f, 0.42f, 0.25f), 1.15f);
                }
                EquipItem rareReward = LootRoller.RollItem(floor, 2, 0.45f);
                LootOrbPickup.Spawn(transform.position + new Vector3(0f, 0f, -1.3f), rareReward);
                dw.ShowBanner("HIẾN TẾ HUYẾT THỆ: Toàn đội hiến 20% Máu để nhận +28% Sát Thương & +12% Chí Mạng!", 4.2f);
            }
            else if (choice == 2)
            {
                // Break Blood Seal -> Spawn Elite Guardian + Drop Guaranteed Epic/Legendary (Tím/Cam) Gear!
                GameAudio.I.PlaySfx(GameSfx.GuardBreak, 1.0f);
                CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up * 1.5f, new Color(0.75f, 0.22f, 1.0f), 4.6f, 0.75f);

                string eliteId = (floor % 2 == 1) ? "DarkKnight" : "Demon";
                Vector3 spawnPos = transform.position + new Vector3(0f, 0f, 2.8f);
                ActorUnit guardian = dw.SpawnMonsterAt(GameDB.Enemies[eliteId], spawnPos, floor, true, false);
                if (dw.CurrentRoomIndex >= 0 && dw.CurrentRoomIndex < dw.Rooms.Count)
                {
                    dw.Rooms[dw.CurrentRoomIndex].activeMonsters.Add(guardian);
                }

                // Spawn guaranteed Epic (3) or Legendary (4) loot orb right at the altar!
                int minRarity = UnityEngine.Random.value < 0.40f ? 4 : 3;
                EquipItem epicOrLeg = LootRoller.RollItem(floor, minRarity, 0.85f);
                LootOrbPickup.Spawn(transform.position + new Vector3(0f, 0f, -1.4f), epicOrLeg);
                dw.AwardExpAndGold(85 * floor, 75 * floor);
                dw.ShowBanner("PHÁ GIẢI PHONG ẤN: Triệu hồi Tinh Anh Hộ Pháp & Xuất hiện Di Vật TÍM / CAM!", 4.5f);
            }
            else
            {
                // Safe Runic Cleansing -> Heal 35% HP & Mana + Cleanse
                GameAudio.I.PlaySfx(GameSfx.Heal, 0.95f);
                CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up * 1.4f, new Color(0.28f, 0.96f, 0.55f), 3.8f, 0.55f);
                foreach (var h in dw.PartyUnits)
                {
                    if (h == null || h.IsDead) continue;
                    h.ReceiveHeal(h.MaxHp * 0.35f, h);
                    h.Mana = Mathf.Min(h.MaxMana, h.Mana + h.MaxMana * 0.35f);
                    h.CleanseDebuffs();
                }
                dw.AwardExpAndGold(40 * floor, 30 * floor);
                dw.ShowBanner("THANH TẨY CỔ NGỮ: Toàn đội hồi 35% HP & Mana an toàn!", 3.4f);
            }
        }
    }

    /// <summary>
    /// Upgraded 24-segment Curved Perlin-Noise Energy Conduit connecting activated Rune Pillars to the Puzzle Gate:
    /// - 24-segment LineRenderer with smooth catenary arch + real-time dual-frequency PerlinNoise oscillation
    /// - URP Unlit HDR Bloom material (Gold/Cyan gradient) with UV energy flow from source pillar to target gate
    /// - Glowing HDR endpoint orbs at both pillars/gate + streaming energy motes traveling along the curve.
    /// </summary>
    public class RuneSealEnergyBeam : MonoBehaviour
    {
        private const int SegmentCount = 24;
        private static Material _sharedBeamFlowMat;
        private static Material _sharedOrbCoreMat;
        private static Material _sharedOrbHaloMat;
        private static Texture2D _beamFlowTex;

        private LineRenderer _line;
        private Vector3 _startPos;
        private Vector3 _endPos;
        private Vector3 _rightAxis;
        private Vector3 _upAxis;
        private float _distance;
        private float _noiseSeed;
        private Transform _startOrb;
        private Transform _endOrb;
        private Transform[] _travelMotes;
        private float[] _motePhases;
        private readonly Vector3[] _positions = new Vector3[SegmentCount];

        public static void SpawnConnectorBeam(Transform parent, Vector3 fromPos, Vector3 toPos, Color color)
        {
            Vector3 start = fromPos + Vector3.up * 1.92f;
            Vector3 end = toPos + Vector3.up * 2.35f;
            Vector3 diff = end - start;
            float dist = diff.magnitude;
            if (dist < 0.2f) return;

            EnsureSharedMaterials(color);

            var go = new GameObject("RuneEnergyConduit_24Seg");
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = start;

            var comp = go.AddComponent<RuneSealEnergyBeam>();
            comp.Init(start, end, dist, color);
        }

        private static void EnsureSharedMaterials(Color baseGold)
        {
            if (_beamFlowTex == null)
            {
                int w = 64, h = 16;
                _beamFlowTex = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    name = "T_RuneBeamFlow_HDR",
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Bilinear
                };
                var cols = new Color32[w * h];
                for (int y = 0; y < h; y++)
                {
                    float ny = (y / (float)(h - 1)) * 2f - 1f;
                    float edgeFade = Mathf.Clamp01(1f - ny * ny);
                    for (int x = 0; x < w; x++)
                    {
                        float u = x / (float)w;
                        float pulse = 0.55f + 0.45f * Mathf.Sin(u * Mathf.PI * 4f);
                        float core = Mathf.Pow(edgeFade, 1.5f) * pulse;
                        byte cByte = (byte)Mathf.Clamp(Mathf.RoundToInt(core * 255f), 0, 255);
                        cols[y * w + x] = new Color32(255, 255, 255, cByte);
                    }
                }
                _beamFlowTex.SetPixels32(cols);
                _beamFlowTex.Apply(false, false);
            }

            if (_sharedBeamFlowMat == null)
            {
                Shader vfxSh = Shader.Find("AbyssalDungeon/AbyssalVFX_URP")
                               ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                               ?? Shader.Find("Universal Render Pipeline/Unlit");
                _sharedBeamFlowMat = new Material(vfxSh) { name = "M_RuneConduit_HDR" };
                if (_sharedBeamFlowMat.HasProperty("_MainTex")) _sharedBeamFlowMat.SetTexture("_MainTex", _beamFlowTex);
                if (_sharedBeamFlowMat.HasProperty("_BaseMap")) _sharedBeamFlowMat.SetTexture("_BaseMap", _beamFlowTex);
                _sharedBeamFlowMat.mainTexture = _beamFlowTex;

                Color hdrGold = new Color(1.0f, 0.88f, 0.32f, 0.96f);
                Color hdrCyan = new Color(0.24f, 0.96f, 1.0f, 1.0f);
                if (_sharedBeamFlowMat.HasProperty("_MainColor")) _sharedBeamFlowMat.SetColor("_MainColor", hdrGold);
                if (_sharedBeamFlowMat.HasProperty("_BaseColor")) _sharedBeamFlowMat.SetColor("_BaseColor", hdrGold);
                if (_sharedBeamFlowMat.HasProperty("_FresnelColor")) _sharedBeamFlowMat.SetColor("_FresnelColor", hdrCyan);
                if (_sharedBeamFlowMat.HasProperty("_EmissionIntensity")) _sharedBeamFlowMat.SetFloat("_EmissionIntensity", 3.4f);
                if (_sharedBeamFlowMat.HasProperty("_ScrollSpeedX")) _sharedBeamFlowMat.SetFloat("_ScrollSpeedX", -2.6f);
                if (_sharedBeamFlowMat.HasProperty("_ScrollSpeedY")) _sharedBeamFlowMat.SetFloat("_ScrollSpeedY", 0.0f);
                if (_sharedBeamFlowMat.HasProperty("_DstBlend")) _sharedBeamFlowMat.SetFloat("_DstBlend", 1f); // Additive HDR Bloom
            }

            if (_sharedOrbCoreMat == null)
            {
                _sharedOrbCoreMat = CharacterVisualFactory.MakeTransparentMat(
                    new Color(1.0f, 0.92f, 0.45f, 0.92f),
                    new Color(1.0f, 0.85f, 0.30f) * 3.2f);
            }

            if (_sharedOrbHaloMat == null)
            {
                _sharedOrbHaloMat = CharacterVisualFactory.MakeTransparentMat(
                    new Color(0.25f, 0.95f, 0.98f, 0.48f),
                    new Color(0.22f, 0.90f, 1.0f) * 2.4f);
            }
        }

        private void Init(Vector3 start, Vector3 end, float dist, Color color)
        {
            _startPos = start;
            _endPos = end;
            _distance = dist;
            _noiseSeed = UnityEngine.Random.Range(0f, 100f);

            Vector3 fwd = (end - start).normalized;
            _rightAxis = Vector3.Cross(Vector3.up, fwd);
            if (_rightAxis.sqrMagnitude < 0.001f) _rightAxis = Vector3.right;
            _rightAxis.Normalize();
            _upAxis = Vector3.Cross(fwd, _rightAxis).normalized;

            _line = gameObject.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = SegmentCount;
            _line.sharedMaterial = _sharedBeamFlowMat;
            _line.textureMode = LineTextureMode.Tile;
            _line.alignment = LineAlignment.View;
            _line.numCapVertices = 4;
            _line.numCornerVertices = 4;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;

            // Tapered width curve: crisp endpoints, fuller pulsing mid-conduit
            var widthCurve = new AnimationCurve(
                new Keyframe(0.0f, 0.10f),
                new Keyframe(0.15f, 0.22f),
                new Keyframe(0.50f, 0.28f),
                new Keyframe(0.85f, 0.22f),
                new Keyframe(1.0f, 0.12f));
            _line.widthCurve = widthCurve;

            // Gold -> Radiant Cyan-Gold -> Solar Gold gradient along the beam
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1.0f, 0.86f, 0.28f), 0.0f),
                    new GradientColorKey(new Color(0.32f, 0.98f, 0.92f), 0.45f),
                    new GradientColorKey(new Color(1.0f, 0.92f, 0.38f), 1.0f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0.0f),
                    new GradientAlphaKey(1.00f, 0.5f),
                    new GradientAlphaKey(0.95f, 1.0f)
                });
            _line.colorGradient = grad;

            // Endpoint glowing orbs (source pillar & target gate)
            _startOrb = CreateEndpointOrb("SourcePillarOrb", _startPos, 0.34f);
            _endOrb = CreateEndpointOrb("TargetGateOrb", _endPos, 0.40f);

            // Lightweight streaming energy motes traveling along the curve from pillar to gate
            int moteCount = 6;
            _travelMotes = new Transform[moteCount];
            _motePhases = new float[moteCount];
            for (int i = 0; i < moteCount; i++)
            {
                _motePhases[i] = i / (float)moteCount;
                var mote = CharacterVisualFactory.Prim(
                    PrimitiveType.Sphere, "ConduitMote_" + i, transform,
                    Vector3.zero, Vector3.one * 0.14f, i % 2 == 0 ? _sharedOrbCoreMat : _sharedOrbHaloMat);
                var r = mote.GetComponent<Renderer>();
                if (r != null)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }
                _travelMotes[i] = mote.transform;
            }

            // Also attach a lightweight ParticleSystem streaming sparkles along the beam
            BuildBeamParticleStream(start, end, dist);
            UpdateBeamCurve(0f);
        }

        private Transform CreateEndpointOrb(string name, Vector3 worldPos, float baseSize)
        {
            var root = new GameObject(name).transform;
            root.SetParent(transform, false);
            root.position = worldPos;

            var core = CharacterVisualFactory.Prim(PrimitiveType.Sphere, "Core", root, Vector3.zero, Vector3.one * baseSize, _sharedOrbCoreMat);
            var halo = CharacterVisualFactory.Prim(PrimitiveType.Sphere, "Halo", root, Vector3.zero, Vector3.one * (baseSize * 1.65f), _sharedOrbHaloMat);
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            return root;
        }

        private void BuildBeamParticleStream(Vector3 start, Vector3 end, float dist)
        {
            var psGo = new GameObject("BeamEnergyParticles");
            psGo.transform.SetParent(transform, false);
            psGo.transform.position = start;
            psGo.transform.rotation = Quaternion.LookRotation((end - start).normalized);

            var ps = psGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 24;
            float speed = Mathf.Clamp(dist * 0.85f, 4.5f, 12f);
            main.startSpeed = speed;
            main.startLifetime = Mathf.Clamp(dist / speed, 0.45f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1.0f, 0.90f, 0.35f, 0.95f),
                new Color(0.30f, 0.96f, 1.0f, 0.95f));

            var em = ps.emission;
            em.rateOverTime = 12f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 2.5f;
            shape.radius = 0.08f;

            var psr = psGo.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = PbrMaterialLibrary.SoftAdditiveParticleMaterial();
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;
        }

        private void Update()
        {
            UpdateBeamCurve(Time.time);
        }

        private void UpdateBeamCurve(float t)
        {
            if (_line == null) return;

            float archHeight = Mathf.Clamp(_distance * 0.14f, 0.55f, 2.2f);
            float waveAmp = Mathf.Clamp(_distance * 0.045f, 0.14f, 0.48f);

            for (int i = 0; i < SegmentCount; i++)
            {
                float u = i / (float)(SegmentCount - 1);
                Vector3 basePt = Vector3.Lerp(_startPos, _endPos, u);

                // Smooth envelope = 0 at both endpoints, 1 at center
                float env = Mathf.Sin(u * Mathf.PI);

                // Graceful upward catenary arch
                float arch = env * archHeight;

                // Dual-frequency Perlin noise oscillation traveling from source pillar (u=0) to gate (u=1)
                float nX = (Mathf.PerlinNoise(_noiseSeed + u * 3.4f - t * 1.85f, t * 0.95f) - 0.5f) * 2f;
                float nY = (Mathf.PerlinNoise(_noiseSeed + 31.7f + u * 3.8f - t * 2.15f, t * 1.15f) - 0.5f) * 2f;
                float harmonic = Mathf.Sin((u * 2.5f - t * 1.6f) * Mathf.PI * 2f) * 0.35f;

                Vector3 offset = _upAxis * (arch + (nY + harmonic) * waveAmp * env)
                               + _rightAxis * (nX * waveAmp * env);

                _positions[i] = basePt + offset;
            }

            _line.SetPositions(_positions);

            // Scroll UV along the LineRenderer if using standard material property
            if (_sharedBeamFlowMat != null)
            {
                Vector2 texOff = new Vector2(-t * 2.2f, 0f);
                _sharedBeamFlowMat.mainTextureOffset = texOff;
                if (_sharedBeamFlowMat.HasProperty("_BaseMap"))
                    _sharedBeamFlowMat.SetTextureOffset("_BaseMap", texOff);
            }

            // Pulse endpoint orbs
            float pulseA = 1f + Mathf.Sin(t * 5.2f + _noiseSeed) * 0.16f;
            float pulseB = 1f + Mathf.Cos(t * 4.8f + _noiseSeed) * 0.16f;
            if (_startOrb != null) _startOrb.localScale = Vector3.one * pulseA;
            if (_endOrb != null) _endOrb.localScale = Vector3.one * pulseB;

            // Move streaming energy motes along the 24-segment curve
            if (_travelMotes != null)
            {
                for (int i = 0; i < _travelMotes.Length; i++)
                {
                    Transform m = _travelMotes[i];
                    if (m == null) continue;
                    float u = Mathf.Repeat(_motePhases[i] + t * 0.58f, 1f);
                    float fIdx = u * (SegmentCount - 1);
                    int idx0 = Mathf.Clamp(Mathf.FloorToInt(fIdx), 0, SegmentCount - 1);
                    int idx1 = Mathf.Min(SegmentCount - 1, idx0 + 1);
                    m.position = Vector3.Lerp(_positions[idx0], _positions[idx1], fIdx - idx0);
                    float s = 0.11f + 0.07f * Mathf.Sin(u * Mathf.PI);
                    m.localScale = Vector3.one * s;
                }
            }
        }
    }
}

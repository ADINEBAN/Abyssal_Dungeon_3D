using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Phase 5: Ancient Runic Lore Monolith revealing the fall of Aether Haven Sanctuary across dungeon floors,
    /// with a concise Gothic Dialogue Modal and an Insight Blessing (+EXP, Gold & Shield).
    /// </summary>
    public class WorldLoreMonolith : MonoBehaviour, IWorldInteractable
    {
        public Vector3 Position => transform.position;
        public float InteractRadius => 3.2f;
        public bool ReadOnce { get; private set; }

        [SerializeField] private int _chapterIndex;
        [SerializeField] private Transform _floatingTablet;
        private static GameObject _activeLoreModal;

        private void OnEnable()
        {
            if (!HeroPlayerInput.Interactables.Contains(this))
                HeroPlayerInput.Interactables.Add(this);
        }

        private void OnDisable()
        {
            HeroPlayerInput.Interactables.Remove(this);
        }

        public static WorldLoreMonolith Create(Transform parent, Vector3 pos, int chapterIndex)
        {
            return BakedAssetRegistry.InstantiateOrBuildPrefab<WorldLoreMonolith>(
                BakedAssetRegistry.PropsPrefabFolder,
                "PF_LoreMonolith",
                parent,
                pos,
                Quaternion.identity,
                () =>
                {
                    var go = new GameObject("PF_LoreMonolith");
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
                    return go;
                },
                m =>
                {
                    if (m == null) return;
                    m.gameObject.name = $"LoreMonolith_Ch{chapterIndex}";
                    m._chapterIndex = chapterIndex;
                    if (!HeroPlayerInput.Interactables.Contains(m))
                        HeroPlayerInput.Interactables.Add(m);
                });
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
}

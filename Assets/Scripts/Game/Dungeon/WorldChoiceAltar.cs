using UnityEngine;

namespace AbyssGame
{
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

        [SerializeField] private string _altarTitle;
        [SerializeField] private Transform _floatingRelic;
        [SerializeField] private Light _altarLight;
        private static GameObject _activeChoiceModal;

        private void OnEnable()
        {
            if (!HeroPlayerInput.Interactables.Contains(this))
                HeroPlayerInput.Interactables.Add(this);
        }

        private void OnDisable()
        {
            HeroPlayerInput.Interactables.Remove(this);
        }

        public static WorldChoiceAltar Create(Transform parent, Vector3 pos, string altarTitle)
        {
            return BakedAssetRegistry.InstantiateOrBuildPrefab<WorldChoiceAltar>(
                BakedAssetRegistry.PropsPrefabFolder,
                "PF_ChoiceAltar",
                parent,
                pos,
                Quaternion.identity,
                () =>
                {
                    var go = new GameObject("PF_ChoiceAltar");
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
                    return go;
                },
                ca =>
                {
                    if (ca == null) return;
                    ca.gameObject.name = $"ChoiceAltar_{altarTitle}";
                    ca._altarTitle = string.IsNullOrEmpty(altarTitle) ? "Tế Đàn Lựa Chọn Cổ Ngữ" : altarTitle;
                    if (!HeroPlayerInput.Interactables.Contains(ca))
                        HeroPlayerInput.Interactables.Add(ca);
                });
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
                GameAudio.I.PlaySfx(GameSfx.GuardBreak, 1.0f);
                CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up * 1.5f, new Color(0.75f, 0.22f, 1.0f), 4.6f, 0.75f);

                string eliteId = (floor % 2 == 1) ? "DarkKnight" : "Demon";
                Vector3 spawnPos = transform.position + new Vector3(0f, 0f, 2.8f);
                ActorUnit guardian = dw.SpawnMonsterAt(GameDB.Enemies[eliteId], spawnPos, floor, true, false);
                if (dw.CurrentRoomIndex >= 0 && dw.CurrentRoomIndex < dw.Rooms.Count)
                {
                    dw.Rooms[dw.CurrentRoomIndex].activeMonsters.Add(guardian);
                }

                int minRarity = UnityEngine.Random.value < 0.40f ? 4 : 3;
                EquipItem epicOrLeg = LootRoller.RollItem(floor, minRarity, 0.85f);
                LootOrbPickup.Spawn(transform.position + new Vector3(0f, 0f, -1.4f), epicOrLeg);
                dw.AwardExpAndGold(85 * floor, 75 * floor);
                dw.ShowBanner("PHÁ GIẢI PHONG ẤN: Triệu hồi Tinh Anh Hộ Pháp & Xuất hiện Di Vật TÍM / CAM!", 4.5f);
            }
            else
            {
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
}

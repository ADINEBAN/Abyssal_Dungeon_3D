using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Interactive Environmental Event in Dungeon rooms (e.g., Blood Moon Altar, Ancient Energy Core, Abyssal Soul Well).
    /// Interacting triggers an arcane shockwave + optional ambush guardian and grants Party Buffs + Rare/Epic Loot.
    /// </summary>
    public class WorldEnvironmentalEvent : MonoBehaviour, IWorldInteractable
    {
        public Vector3 Position => transform.position;
        public float InteractRadius => 3.2f;
        public bool Completed { get; private set; }

        [SerializeField] private string _eventName;
        [SerializeField] private int _roomIndex;
        [SerializeField] private Transform _floatingCore;
        [SerializeField] private Light _eventLight;

        private void OnEnable()
        {
            if (!HeroPlayerInput.Interactables.Contains(this))
                HeroPlayerInput.Interactables.Add(this);
        }

        private void OnDisable()
        {
            HeroPlayerInput.Interactables.Remove(this);
        }

        public static WorldEnvironmentalEvent Create(Transform parent, Vector3 pos, string eventName, int roomIndex)
        {
            string prefabName = $"PF_EnvironmentalEvent_R{roomIndex}";
            return BakedAssetRegistry.InstantiateOrBuildPrefab<WorldEnvironmentalEvent>(
                BakedAssetRegistry.PropsPrefabFolder,
                prefabName,
                parent,
                pos,
                Quaternion.identity,
                () =>
                {
                    var go = new GameObject(prefabName);
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
                    return go;
                },
                ev =>
                {
                    if (ev == null) return;
                    ev.gameObject.name = $"EnvEvent_{roomIndex}_{eventName}";
                    ev._eventName = string.IsNullOrEmpty(eventName) ? "Tế Đàn Cổ Đại" : eventName;
                    ev._roomIndex = roomIndex;
                    if (!HeroPlayerInput.Interactables.Contains(ev))
                        HeroPlayerInput.Interactables.Add(ev);
                });
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
}

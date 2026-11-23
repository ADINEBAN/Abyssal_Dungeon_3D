using UnityEngine;

namespace AbyssGame
{
    public class WorldTreasureChest : MonoBehaviour, IWorldInteractable
    {
        public Vector3 Position => transform.position;
        public float InteractRadius => 3.2f;

        [SerializeField] private bool _unlocked = false;
        [SerializeField] private bool _opened = false;
        [SerializeField] private int _minRarity = 1;
        [SerializeField] private Transform _lid;
        [SerializeField] private Renderer _gemRenderer;

        private void OnEnable()
        {
            if (!HeroPlayerInput.Interactables.Contains(this))
                HeroPlayerInput.Interactables.Add(this);
        }

        private void OnDisable()
        {
            HeroPlayerInput.Interactables.Remove(this);
        }

        public static WorldTreasureChest Create(Transform parent, Vector3 pos, int minRarity)
        {
            return BakedAssetRegistry.InstantiateOrBuildPrefab<WorldTreasureChest>(
                BakedAssetRegistry.PropsPrefabFolder,
                "PF_TreasureChest",
                parent,
                pos,
                Quaternion.Euler(0f, 180f, 0f),
                () =>
                {
                    var go = new GameObject("PF_TreasureChest");
                    var wood = PbrMaterialLibrary.AgedOakWood();
                    var iron = PbrMaterialLibrary.WroughtIron();
                    var lockMat = PbrMaterialLibrary.AbyssalCrystal(new Color(0.85f, 0.16f, 0.22f), new Color(1f, 0.25f, 0.2f), 3.0f);

                    ObjMeshLoader.SpawnPiece("TreasureChest_IronboundBase", go.transform, Vector3.zero, Quaternion.identity, Vector3.one, wood, addBoxCollider: true);
                    var lidPivot = new GameObject("LidPivot");
                    lidPivot.transform.SetParent(go.transform, false);
                    lidPivot.transform.localPosition = new Vector3(0f, 0.54f, -0.38f);
                    ObjMeshLoader.SpawnPiece("TreasureChest_IronboundLid", lidPivot.transform, Vector3.zero, Quaternion.identity, Vector3.one, iron, addBoxCollider: false);
                    var gem = CharacterVisualFactory.Prim(PrimitiveType.Sphere, "LockGem", go.transform, new Vector3(0f, 0.44f, 0.43f), Vector3.one * 0.18f, lockMat);

                    var chest = go.AddComponent<WorldTreasureChest>();
                    chest._minRarity = minRarity;
                    chest._lid = lidPivot.transform;
                    chest._gemRenderer = gem.GetComponent<Renderer>();
                    return go;
                },
                chest =>
                {
                    if (chest == null) return;
                    chest.gameObject.name = "TreasureChest";
                    chest._minRarity = minRarity;
                    if (!HeroPlayerInput.Interactables.Contains(chest))
                        HeroPlayerInput.Interactables.Add(chest);
                });
        }

        public void UnlockChest()
        {
            _unlocked = true;
            if (_gemRenderer != null)
                _gemRenderer.sharedMaterial = PbrMaterialLibrary.AbyssalCrystal(new Color(0.18f, 0.92f, 0.42f), new Color(0.35f, 1f, 0.55f), 3.2f);
        }

        public bool CanInteract() => _unlocked && !_opened;
        public string GetPrompt() => "[F] Mở Rương Kho Báu";

        public void Interact(ActorUnit user)
        {
            if (!_unlocked || _opened) return;
            _opened = true;
            if (_lid != null) _lid.localRotation = Quaternion.Euler(-65f, 0f, 0f);
            GameAudio.I.PlaySfx(GameSfx.Loot, 1f);

            int floor = GameSession.I.Profile.selectedFloor;
            if (DungeonWorldBuilder.Instance != null)
                DungeonWorldBuilder.Instance.AwardExpAndGold(45 * floor, 40 * floor);

            for (int i = 0; i < 2; i++)
            {
                EquipItem it = LootRoller.RollItem(floor, _minRarity, 0.25f);
                Vector3 dropPos = transform.position + new Vector3((i == 0 ? -0.7f : 0.7f), 0f, 0.9f);
                LootOrbPickup.Spawn(dropPos, it);
                if (LanRoomBridge.HasInstance && LanRoomBridge.I.IsHosting)
                    LanRoomBridge.I.BroadcastInstancedLootDrop(dropPos, floor, _minRarity, 0.25f);
            }
            CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up * 0.8f, new Color(1f, 0.85f, 0.25f), 2.2f, 0.5f);
        }
    }
}

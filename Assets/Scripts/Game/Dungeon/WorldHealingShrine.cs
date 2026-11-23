using UnityEngine;

namespace AbyssGame
{
    public class WorldHealingShrine : MonoBehaviour, IWorldInteractable
    {
        public Vector3 Position => transform.position;
        public float InteractRadius => 3.2f;
        [SerializeField] private bool _used = false;
        [SerializeField] private Transform _orb;

        private void OnEnable()
        {
            if (!HeroPlayerInput.Interactables.Contains(this))
                HeroPlayerInput.Interactables.Add(this);
        }

        private void OnDisable()
        {
            HeroPlayerInput.Interactables.Remove(this);
        }

        public static WorldHealingShrine Create(Transform parent, Vector3 pos)
        {
            return BakedAssetRegistry.InstantiateOrBuildPrefab<WorldHealingShrine>(
                BakedAssetRegistry.PropsPrefabFolder,
                "PF_HealingShrine",
                parent,
                pos,
                Quaternion.identity,
                () =>
                {
                    var go = new GameObject("PF_HealingShrine");
                    var stone = PbrMaterialLibrary.GothicStone(new Color(0.76f, 0.84f, 0.78f));
                    var heal = PbrMaterialLibrary.AbyssalCrystal(new Color(0.16f, 0.85f, 0.46f), new Color(0.32f, 1.0f, 0.62f), 2.8f);

                    ObjMeshLoader.SpawnPiece("HealingShrinePedestal", go.transform, Vector3.zero, Quaternion.identity, Vector3.one * 0.95f, stone, addBoxCollider: true);
                    var orb = ObjMeshLoader.SpawnPiece("AbyssalCrystalCluster", go.transform, new Vector3(0f, 2.15f, 0f), Quaternion.identity, Vector3.one * 0.32f, heal, addBoxCollider: false);

                    var sh = go.AddComponent<WorldHealingShrine>();
                    sh._orb = orb.transform;
                    return go;
                },
                sh =>
                {
                    if (sh == null) return;
                    sh.gameObject.name = "HealingShrine";
                    if (!HeroPlayerInput.Interactables.Contains(sh))
                        HeroPlayerInput.Interactables.Add(sh);
                });
        }

        private void Update()
        {
            if (_orb != null && !_used)
            {
                _orb.localPosition = new Vector3(0f, 2.15f + Mathf.Sin(Time.time * 2.8f) * 0.10f, 0f);
                _orb.Rotate(0f, 45f * Time.deltaTime, 0f, Space.World);
            }
        }

        public bool CanInteract() => !_used;
        public string GetPrompt() => "[F] Cầu nguyện Đài Hồi Phục (+70% HP & Mana cả đội)";

        public void Interact(ActorUnit user)
        {
            if (_used) return;
            _used = true;
            if (_orb != null) _orb.gameObject.SetActive(false);
            GameAudio.I.PlaySfx(GameSfx.Heal, 1f);

            foreach (var u in ActorUnit.AllUnits)
            {
                if (u.Team == Team.Hero && !u.IsDead)
                {
                    if (u.IsDowned) u.Revive(0.7f);
                    else
                    {
                        u.ReceiveHeal(u.MaxHp * 0.7f, user);
                        u.Mana = Mathf.Min(u.MaxMana, u.Mana + u.MaxMana * 0.7f);
                        u.CleanseDebuffs();
                    }
                }
            }
            GameSession.I.Profile.potions = Mathf.Min(8, GameSession.I.Profile.potions + 1);
            GameSession.I.NotifyProfileChanged();
            CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up, new Color(0.25f, 1f, 0.55f), 4f, 0.6f);
        }
    }
}

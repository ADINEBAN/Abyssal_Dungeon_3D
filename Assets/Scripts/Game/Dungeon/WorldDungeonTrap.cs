using UnityEngine;

namespace AbyssGame
{
    public class WorldDungeonTrap : MonoBehaviour
    {
        [SerializeField] private bool _fireTrap;
        private float _timer;
        [SerializeField] private Transform _hazardVisual;

        public static WorldDungeonTrap Create(Transform parent, Vector3 pos, bool fireTrap)
        {
            string prefabName = fireTrap ? "PF_FireTrap" : "PF_SpikeTrap";
            return BakedAssetRegistry.InstantiateOrBuildPrefab<WorldDungeonTrap>(
                BakedAssetRegistry.PropsPrefabFolder,
                prefabName,
                parent,
                pos,
                Quaternion.identity,
                () =>
                {
                    var go = new GameObject(prefabName);
                    ObjMeshLoader.SpawnPiece("SpikeTrapPlate", go.transform,
                        Vector3.zero, Quaternion.identity, Vector3.one,
                        PbrMaterialLibrary.WroughtIron(), addBoxCollider: false);

                    var hazardHolder = new GameObject("Hazard");
                    hazardHolder.transform.SetParent(go.transform, false);

                    if (fireTrap)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            float x = (i % 2 == 0) ? -0.58f : 0.58f;
                            float z = (i < 2) ? -0.58f : 0.58f;
                            ModularDungeonArchitect.SpawnFlameVisual(hazardHolder.transform,
                                new Vector3(x, 0.42f, z), new Vector3(0.65f, 1.05f, 0.65f),
                                new Color(1f, 0.38f, 0.08f), new Color(1f, 0.90f, 0.45f));
                        }
                    }
                    else
                    {
                        ObjMeshLoader.SpawnPiece("SpikeCluster", hazardHolder.transform,
                            Vector3.zero, Quaternion.identity, Vector3.one,
                            PbrMaterialLibrary.WroughtIron(), addBoxCollider: false);
                    }
                    hazardHolder.SetActive(false);

                    var tr = go.AddComponent<WorldDungeonTrap>();
                    tr._fireTrap = fireTrap;
                    tr._hazardVisual = hazardHolder.transform;
                    return go;
                },
                tr =>
                {
                    if (tr == null) return;
                    tr.gameObject.name = fireTrap ? "FireTrap" : "SpikeTrap";
                    tr._fireTrap = fireTrap;
                    tr._timer = UnityEngine.Random.Range(0f, 3f);
                });
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            float cycle = _timer % 3.6f;
            bool active = cycle > 2.6f;
            if (_hazardVisual != null && _hazardVisual.gameObject.activeSelf != active)
            {
                _hazardVisual.gameObject.SetActive(active);
                if (active) TriggerTrapDamage();
            }
        }

        private void TriggerTrapDamage()
        {
            for (int i = ActorUnit.AllUnits.Count - 1; i >= 0; i--)
            {
                var u = ActorUnit.AllUnits[i];
                if (u != null && u.Team == Team.Hero && !u.IsDead && !u.IsDowned)
                {
                    Vector2 flatTrap = new Vector2(transform.position.x, transform.position.z);
                    Vector2 flatHero = new Vector2(u.transform.position.x, u.transform.position.z);
                    if (Vector2.Distance(flatTrap, flatHero) <= 1.55f)
                    {
                        if (u.transform.position.y >= 0.52f)
                        {
                            CharacterVisualFactory.SpawnFloatingText(u.transform.position + Vector3.up * 1.9f, "NHẢY QUA!", new Color(0.55f, 1f, 0.78f), 0.85f);
                            continue;
                        }
                        u.ReceiveHit(u.MaxHp * 0.10f, false, null);
                        if (_fireTrap) u.ApplyStatus(StatusType.Burn, 3f, 12f, null);
                    }
                }
            }
        }
    }
}

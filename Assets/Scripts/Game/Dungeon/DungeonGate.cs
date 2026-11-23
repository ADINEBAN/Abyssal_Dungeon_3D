using UnityEngine;

namespace AbyssGame
{
    public class DungeonGate : MonoBehaviour
    {
        [SerializeField] private Transform _bars;
        [SerializeField] private BoxCollider _blocker;
        [SerializeField] private bool _open = true;

        public static DungeonGate Create(Transform parent, Vector3 pos, float width, bool eastWest = false)
        {
            string prefabName = eastWest ? "PF_DungeonGate_EW" : "PF_DungeonGate_NS";
            Quaternion rot = eastWest ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;

            return BakedAssetRegistry.InstantiateOrBuildPrefab<DungeonGate>(
                BakedAssetRegistry.PropsPrefabFolder,
                prefabName,
                parent,
                pos,
                rot,
                () =>
                {
                    var go = new GameObject(prefabName);
                    var g = go.AddComponent<DungeonGate>();
                    g._blocker = go.AddComponent<BoxCollider>();
                    g._blocker.center = new Vector3(0f, 2f, 0f);
                    g._blocker.size = new Vector3(width, 4f, 0.5f);

                    var barsRoot = new GameObject("Bars");
                    barsRoot.transform.SetParent(go.transform, false);
                    g._bars = barsRoot.transform;

                    ObjMeshLoader.SpawnPiece("IronPortcullis", barsRoot.transform,
                        Vector3.zero, Quaternion.identity, new Vector3(width / 5.05f, 1f, 1f),
                        PbrMaterialLibrary.WroughtIron(), addBoxCollider: false);

                    g.SetOpen(true, instant: true);
                    return go;
                },
                g =>
                {
                    if (g == null) return;
                    g.gameObject.name = eastWest ? "DungeonGate_EW" : "DungeonGate_NS";
                    g.SetOpen(true, instant: true);
                });
        }

        public void SetOpen(bool open, bool instant = false)
        {
            _open = open;
            if (_blocker != null) _blocker.enabled = !open;
            if (instant && _bars != null)
                _bars.localPosition = new Vector3(0f, open ? 3.85f : 0f, 0f);
        }

        private void Update()
        {
            if (_bars == null) return;
            Vector3 target = new Vector3(0f, _open ? 3.85f : 0f, 0f);
            _bars.localPosition = Vector3.MoveTowards(_bars.localPosition, target, 7.5f * Time.deltaTime);
        }
    }
}

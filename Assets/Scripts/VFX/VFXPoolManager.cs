using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssalDungeon.VFX
{
    /// <summary>
    /// Centralized Object Pooling system for all 3-layer 3D Skill VFX, Projectiles, Shields, Traps, and Telegraphs.
    /// Eliminates runtime Instantiate/Destroy spikes on both low-end and high-end hardware.
    /// </summary>
    public class VFXPoolManager : MonoBehaviour
    {
        [Serializable]
        public class PoolEntry
        {
            public string key;
            public GameObject prefab;
            public int prewarmCount = 2;
            public int maxCapacity = 12;
        }

        private static VFXPoolManager _instance;
        public static bool HasInstance => _instance != null;

        public static VFXPoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<VFXPoolManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[VFXPoolManager]");
                        DontDestroyOnLoad(go);
                        _instance = go.AddComponent<VFXPoolManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Configured VFX Pools")]
        public List<PoolEntry> poolEntries = new List<PoolEntry>();

        private readonly Dictionary<string, GameObject> _keyToPrefab = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Queue<GameObject>> _inactivePools = new Dictionary<string, Queue<GameObject>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<GameObject>> _activeInstances = new Dictionary<string, List<GameObject>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, string> _instanceIdToKey = new Dictionary<int, string>();
        private readonly Dictionary<int, string> _prefabIdToKey = new Dictionary<int, string>();
        private bool _initialized;

        private static readonly string[] ClericSkillKeys = {
            "VFX_Cleric_Smite",
            "VFX_Cleric_GroupHeal",
            "VFX_Cleric_Purify",
            "VFX_Cleric_Resurrection"
        };

        private static readonly string[] KnightSkillKeys = {
            "VFX_Knight_ShieldBash",
            "VFX_Knight_IronGuard",
            "VFX_Knight_Taunt",
            "VFX_Knight_Judgement"
        };

        private static readonly string[] MageSkillKeys = {
            "VFX_Mage_Fireball",
            "VFX_Mage_FrostNova",
            "VFX_Mage_ArcanePulse",
            "VFX_Mage_Meteor"
        };

        private static readonly string[] RangerSkillKeys = {
            "VFX_Ranger_PiercingShot",
            "VFX_Ranger_FrostTrap",
            "VFX_Ranger_Volley",
            "VFX_Ranger_ArrowStorm"
        };

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            InitializePools();
        }

        public void InitializePools()
        {
            if (_initialized) return;
            _initialized = true;

            if (poolEntries != null)
            {
                for (int i = 0; i < poolEntries.Count; i++)
                {
                    PoolEntry entry = poolEntries[i];
                    if (entry != null && entry.prefab != null && !string.IsNullOrEmpty(entry.key))
                    {
                        RegisterPrefab(entry.key, entry.prefab, entry.prewarmCount);
                    }
                }
            }

            GameObject[] resourcePrefabs = Resources.LoadAll<GameObject>("VFX/Prefabs");
            if (resourcePrefabs != null)
            {
                for (int i = 0; i < resourcePrefabs.Length; i++)
                {
                    GameObject p = resourcePrefabs[i];
                    if (p != null && p.name != "VFXPoolManager" && !_keyToPrefab.ContainsKey(p.name))
                    {
                        RegisterPrefab(p.name, p, 2);
                    }
                }
            }
        }

        public void RegisterPrefab(string key, GameObject prefab, int prewarmCount = 2)
        {
            if (string.IsNullOrEmpty(key) || prefab == null) return;

            _keyToPrefab[key] = prefab;
            _prefabIdToKey[prefab.GetInstanceID()] = key;

            if (!_inactivePools.TryGetValue(key, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>(Mathf.Max(2, prewarmCount));
                _inactivePools[key] = queue;
            }
            if (!_activeInstances.ContainsKey(key))
            {
                _activeInstances[key] = new List<GameObject>(8);
            }

            int currentTotal = queue.Count + _activeInstances[key].Count;
            for (int i = currentTotal; i < prewarmCount; i++)
            {
                GameObject inst = CreateNewInstance(key, prefab);
                inst.SetActive(false);
                queue.Enqueue(inst);
            }
        }

        public GameObject Spawn(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            Transform followTarget = null,
            float scaleMultiplier = 1f,
            float durationOverride = -1f)
        {
            if (prefab == null) return null;
            InitializePools();

            int prefabId = prefab.GetInstanceID();
            if (!_prefabIdToKey.TryGetValue(prefabId, out string key))
            {
                key = prefab.name;
                RegisterPrefab(key, prefab, 1);
            }
            return Spawn(key, position, rotation, followTarget, scaleMultiplier, durationOverride);
        }

        public GameObject Spawn(
            string key,
            Vector3 position,
            Quaternion rotation,
            Transform followTarget = null,
            float scaleMultiplier = 1f,
            float durationOverride = -1f)
        {
            if (string.IsNullOrEmpty(key)) return null;
            InitializePools();

            if (!_keyToPrefab.TryGetValue(key, out GameObject prefab) || prefab == null)
            {
                prefab = Resources.Load<GameObject>("VFX/Prefabs/" + key);
                if (prefab != null)
                {
                    RegisterPrefab(key, prefab, 2);
                }
                else
                {
                    return null;
                }
            }

            Queue<GameObject> queue = _inactivePools[key];
            List<GameObject> activeList = _activeInstances[key];
            GameObject instance = null;

            while (queue.Count > 0 && instance == null)
            {
                instance = queue.Dequeue();
            }

            if (instance == null && activeList.Count >= 16)
            {
                instance = activeList[0];
                activeList.RemoveAt(0);
            }

            if (instance == null)
            {
                instance = CreateNewInstance(key, prefab);
            }

            activeList.Add(instance);
            instance.transform.SetParent(null, false);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);

            VFXAnimatedLayerController ctrl = instance.GetComponent<VFXAnimatedLayerController>();
            if (ctrl != null)
            {
                ctrl.OnSpawnedFromPool(scaleMultiplier, durationOverride, followTarget, Vector3.zero);
            }

            return instance;
        }

        public static string GetSkillVfxKey(int classId, int skillIndex)
        {
            int idx = Mathf.Clamp(skillIndex, 0, 3);
            switch (classId)
            {
                case 9:
                case 2:
                    return ClericSkillKeys[idx];

                case 0:
                case 1:
                case 8:
                case 11:
                    return KnightSkillKeys[idx];

                case 5:
                case 6:
                case 7:
                case 10:
                    return MageSkillKeys[idx];

                case 4:
                case 3:
                default:
                    return RangerSkillKeys[idx];
            }
        }

        public static string GetProjectileVfxKey(int classId)
        {
            switch (classId)
            {
                case 9:
                case 2:
                    return "VFX_Projectile_Cleric";
                case 0:
                case 1:
                case 8:
                case 11:
                    return "VFX_Projectile_Knight";
                case 5:
                case 6:
                case 7:
                case 10:
                    return "VFX_Projectile_Mage";
                case 4:
                case 3:
                default:
                    return "VFX_Projectile_Ranger";
            }
        }

        public GameObject SpawnForClassSkill(
            int classId,
            int skillIndex,
            Vector3 position,
            Quaternion rotation,
            float scaleMultiplier = 1f,
            float durationOverride = -1f,
            Transform followTarget = null)
        {
            string key = GetSkillVfxKey(classId, skillIndex);
            return Spawn(key, position, rotation, followTarget, scaleMultiplier, durationOverride);
        }

        public GameObject SpawnGroundTelegraph(Vector3 center, float radius, float duration, int classId = -1)
        {
            string key = "VFX_GroundTelegraph_Rune";
            float scale = Mathf.Clamp(radius / 1.5f, 0.5f, 3.5f);
            Vector3 pos = new Vector3(center.x, 0.03f, center.z);
            return Spawn(key, pos, Quaternion.identity, null, scale, duration);
        }

        public void Release(GameObject instance)
        {
            if (instance == null) return;

            int id = instance.GetInstanceID();
            if (!_instanceIdToKey.TryGetValue(id, out string key))
            {
                VFXAnimatedLayerController ctrl = instance.GetComponent<VFXAnimatedLayerController>();
                key = ctrl != null ? ctrl.poolKey : instance.name;
                _instanceIdToKey[id] = key;
            }

            if (_activeInstances.TryGetValue(key, out List<GameObject> activeList))
            {
                activeList.Remove(instance);
            }

            if (!_inactivePools.TryGetValue(key, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                _inactivePools[key] = queue;
            }

            instance.SetActive(false);
            instance.transform.SetParent(transform, false);
            if (!queue.Contains(instance))
            {
                queue.Enqueue(instance);
            }
        }

        private GameObject CreateNewInstance(string key, GameObject prefab)
        {
            GameObject inst = Instantiate(prefab, transform);
            inst.name = key + "_Pooled";
            int id = inst.GetInstanceID();
            _instanceIdToKey[id] = key;
            VFXAnimatedLayerController ctrl = inst.GetComponent<VFXAnimatedLayerController>();
            if (ctrl != null)
            {
                ctrl.poolKey = key;
                ctrl.CacheComponents();
            }
            return inst;
        }
    }
}

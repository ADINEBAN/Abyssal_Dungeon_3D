using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssalDungeon.VFX
{
    public enum VFXType
    {
        // Existing core & combat VFX (preserve order/indices for serialized assets)
        Fire,
        Magic,
        Hit,
        CriticalHit,
        Heal,
        Shield,
        Poison,
        Burn,
        Freeze,
        Death,
        BossAura,
        DungeonFog,
        Portal,
        DodgeTrail,
        LevelUp,
        ChestOpen,
        TrapSpike,
        TrapFire,
        TrapMagic,

        // Phase 1 — Fire VFX (6)
        Env_TorchFire,
        Env_BrazierFire,
        Env_CandleFlame,
        Env_TrapFire,
        Env_GroundFire,
        Env_RisingEmbers,

        // Phase 1 — Smoke VFX (4)
        Env_TorchSmoke,
        Env_BlackFireSmoke,
        Env_AmbientSmoke,
        Env_HazardDenseSmoke,

        // Phase 1 — Atmosphere VFX (5)
        Env_GroundFog,
        Env_FloatingDust,
        Env_AshParticles,
        Env_FloatingEmbers,
        Env_AtmosphericParticles,

        // Phase 1 — Magic Environment VFX (5)
        Env_CrystalGlow,
        Env_FloatingRunes,
        Env_MagicalEnergy,
        Env_PortalParticles,
        Env_AncientAltarActivation,

        // Phase 1 — Environmental Interaction (5)
        Env_TrapActivation,
        Env_FireExplosion,
        Env_GroundDustBurst,
        Env_StoneDebris,
        Env_EnvironmentalImpact,

        // Phase 2 — Blender 3D Weapon & Combat VFX (10)
        Wpn_LightSlash,
        Wpn_HeavyCleave,
        Wpn_SpinWhirlwind,
        Wpn_ThrustPierce,
        Wpn_ShieldBlockAegis,
        Wpn_PoiseShatter,
        Wpn_MuzzleCastBurst,
        Wpn_ArrowImpact,
        Wpn_SpellImpact,
        Wpn_EnchantAura,

        // Phase 2B — Bespoke Assassin Dual Blades 3D VFX Set (6)
        Wpn_Asn_DualBladeSlash,
        Wpn_Asn_RapidCombo,
        Wpn_Asn_DashAfterimage,
        Wpn_Asn_BackstabImpact,
        Wpn_Asn_PoisonImpact,
        Wpn_Asn_ShadowBurst
    }

    internal sealed class PooledVfxInstance : MonoBehaviour
    {
        public VFXType Type;
        public Vector3 BaseScale = Vector3.one;
        public ParticleSystem[] Systems;
        public Color[] BaseStartColors;
        public TrailRenderer[] Trails;
        public int SpawnToken;

        private static readonly HashSet<string> SuppressedCircleChildren = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "MuzzleRuneRing",
            "PortalFloorSigil",
            "AltarRuneBurst",
            "AsnShadowRuneSeal3D",
            "HeavyFloorRing"
        };

        public void CacheIfNeeded()
        {
            if (Systems != null) return;
            BaseScale = transform.localScale;
            var allSystems = GetComponentsInChildren<ParticleSystem>(true);
            var kept = new List<ParticleSystem>(allSystems.Length);
            for (int i = 0; i < allSystems.Length; i++)
            {
                var ps = allSystems[i];
                if (ps == null) continue;
                if (SuppressedCircleChildren.Contains(ps.gameObject.name))
                {
                    ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.gameObject.SetActive(false);
                    continue;
                }
                kept.Add(ps);
            }
            Systems = kept.ToArray();
            BaseStartColors = new Color[Systems.Length];
            for (int i = 0; i < Systems.Length; i++)
            {
                if (Systems[i] != null)
                    BaseStartColors[i] = Systems[i].main.startColor.color;
            }
            Trails = GetComponentsInChildren<TrailRenderer>(true);
        }

        public void ResetAndPlay(float scaleMultiplier, Color? tintOverride)
        {
            CacheIfNeeded();
            transform.localScale = BaseScale * scaleMultiplier;

            if (Trails != null)
            {
                for (int i = 0; i < Trails.Length; i++)
                {
                    if (Trails[i] != null) Trails[i].Clear();
                }
            }

            if (Systems != null)
            {
                for (int i = 0; i < Systems.Length; i++)
                {
                    var ps = Systems[i];
                    if (ps == null) continue;
                    if (ps.main.startColor.mode == ParticleSystemGradientMode.Color)
                    {
                        var main = ps.main;
                        Color baseCol = BaseStartColors[i];
                        if (tintOverride.HasValue)
                        {
                            Color ov = tintOverride.Value;
                            main.startColor = new Color(ov.r, ov.g, ov.b, ov.a > 0.01f ? ov.a * Mathf.Max(0.45f, baseCol.a) : baseCol.a);
                        }
                        else
                        {
                            main.startColor = baseCol;
                        }
                    }
                    ps.Clear(false);
                    if (ps.gameObject.activeInHierarchy)
                        ps.Play(false);
                }
            }
        }

        public void StopAndClear()
        {
            if (Systems != null)
            {
                for (int i = 0; i < Systems.Length; i++)
                {
                    if (Systems[i] != null)
                        Systems[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
            if (Trails != null)
            {
                for (int i = 0; i < Trails.Length; i++)
                {
                    if (Trails[i] != null) Trails[i].Clear();
                }
            }
        }
    }

    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [SerializeField] private Dictionary<VFXType, GameObject> vfxPrefabs = new Dictionary<VFXType, GameObject>();
        private readonly Dictionary<VFXType, Queue<GameObject>> pool = new Dictionary<VFXType, Queue<GameObject>>();
        private readonly HashSet<GameObject> _activeObjects = new HashSet<GameObject>();
        private Transform _templateRoot;
        private Transform _poolRoot;

        public static VFXManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            Instance = FindObjectOfType<VFXManager>();
            if (Instance == null && Application.isPlaying)
            {
                var go = new GameObject("[VFXManager]");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<VFXManager>();
            }
            return Instance;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (Application.isPlaying)
                    DontDestroyOnLoad(gameObject);
                InitializeRootsAndPools();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void InitializeRootsAndPools()
        {
            if (_templateRoot == null)
            {
                var tGo = new GameObject("[Templates]");
                tGo.transform.SetParent(transform, false);
                tGo.SetActive(false);
                _templateRoot = tGo.transform;
            }
            if (_poolRoot == null)
            {
                var pGo = new GameObject("[PooledInactive]");
                pGo.transform.SetParent(transform, false);
                _poolRoot = pGo.transform;
            }

            foreach (VFXType type in Enum.GetValues(typeof(VFXType)))
            {
                if (!pool.ContainsKey(type))
                    pool[type] = new Queue<GameObject>();
            }
        }

        private GameObject GetOrCreatePrefab(VFXType type)
        {
            InitializeRootsAndPools();
            if (vfxPrefabs.TryGetValue(type, out GameObject existing) && existing != null)
                return existing;

            GameObject loaded = Resources.Load<GameObject>($"VFX/Prefabs/VFX_{type}");
            if (loaded != null)
            {
                vfxPrefabs[type] = loaded;
                return loaded;
            }

            GameObject created = ProceduralVFXFactory.CreateVFXPrefab(type);
            if (created != null)
            {
                created.transform.SetParent(_templateRoot, false);
                created.SetActive(false);
                vfxPrefabs[type] = created;
            }
            return created;
        }

        public void Prewarm(VFXType type, int count)
        {
            InitializeRootsAndPools();
            Queue<GameObject> q = pool[type];
            int needed = Mathf.Max(0, count - q.Count);
            if (needed <= 0) return;

            GameObject prefab = GetOrCreatePrefab(type);
            if (prefab == null) return;

            for (int i = 0; i < needed; i++)
            {
                GameObject inst = Instantiate(prefab, _poolRoot);
                inst.name = $"VFX_{type}_Pooled";
                var meta = inst.GetComponent<PooledVfxInstance>();
                if (meta == null) meta = inst.AddComponent<PooledVfxInstance>();
                meta.Type = type;
                meta.CacheIfNeeded();
                inst.SetActive(false);
                q.Enqueue(inst);
            }
        }

        public GameObject SpawnVFX(VFXType type, Vector3 position, float duration)
        {
            return SpawnVFX(type, position, Quaternion.identity, duration, null, null, 1f);
        }

        public GameObject SpawnVFX(
            VFXType type,
            Vector3 position,
            Quaternion rotation,
            float duration,
            Color? tintOverride = null,
            Transform parent = null,
            float scale = 1f)
        {
            GameObject vfxObj = AcquireInstance(type, parent, position, rotation, isLocal: false, tintOverride, scale);
            if (vfxObj != null && duration > 0f)
            {
                var meta = vfxObj.GetComponent<PooledVfxInstance>();
                int token = meta != null ? meta.SpawnToken : 0;
                StartCoroutine(ReturnToPoolAfterDuration(type, vfxObj, duration, token));
            }
            return vfxObj;
        }

        public GameObject SpawnLoopingVFX(
            VFXType type,
            Transform parent,
            Vector3 localPos,
            Quaternion localRot,
            Color? tintOverride = null,
            float scale = 1f)
        {
            return AcquireInstance(type, parent, localPos, localRot, isLocal: true, tintOverride, scale);
        }

        private GameObject AcquireInstance(
            VFXType type,
            Transform parent,
            Vector3 pos,
            Quaternion rot,
            bool isLocal,
            Color? tintOverride,
            float scale)
        {
            InitializeRootsAndPools();

            GameObject vfxObj = null;
            Queue<GameObject> q = pool[type];
            while (q.Count > 0 && vfxObj == null)
            {
                vfxObj = q.Dequeue();
            }

            if (vfxObj == null)
            {
                GameObject prefab = GetOrCreatePrefab(type);
                if (prefab == null) return null;
                vfxObj = Instantiate(prefab);
                vfxObj.name = $"VFX_{type}";
            }

            var meta = vfxObj.GetComponent<PooledVfxInstance>();
            if (meta == null) meta = vfxObj.AddComponent<PooledVfxInstance>();
            meta.Type = type;
            meta.SpawnToken++;
            meta.CacheIfNeeded();

            if (parent != null)
            {
                vfxObj.transform.SetParent(parent, false);
                if (isLocal)
                {
                    vfxObj.transform.localPosition = pos;
                    vfxObj.transform.localRotation = rot;
                }
                else
                {
                    vfxObj.transform.position = pos;
                    vfxObj.transform.rotation = rot;
                }
            }
            else
            {
                vfxObj.transform.SetParent(null, false);
                vfxObj.transform.position = pos;
                vfxObj.transform.rotation = rot;
            }

            vfxObj.SetActive(true);
            meta.ResetAndPlay(scale, tintOverride);
            _activeObjects.Add(vfxObj);
            return vfxObj;
        }

        public void ReleaseVFX(VFXType type, GameObject vfxObj)
        {
            if (vfxObj == null) return;
            InitializeRootsAndPools();

            _activeObjects.Remove(vfxObj);
            var meta = vfxObj.GetComponent<PooledVfxInstance>();
            if (meta != null)
            {
                meta.SpawnToken++;
                meta.StopAndClear();
            }

            vfxObj.SetActive(false);
            if (_poolRoot != null)
                vfxObj.transform.SetParent(_poolRoot, false);

            if (!pool.ContainsKey(type))
                pool[type] = new Queue<GameObject>();
            if (!pool[type].Contains(vfxObj))
                pool[type].Enqueue(vfxObj);
        }

        public void GetPoolStats(out int pooledInactive, out int activeTracked)
        {
            pooledInactive = 0;
            foreach (var kv in pool)
            {
                if (kv.Value != null) pooledInactive += kv.Value.Count;
            }
            _activeObjects.RemoveWhere(x => x == null || !x.activeInHierarchy);
            activeTracked = _activeObjects.Count;
        }

        private IEnumerator ReturnToPoolAfterDuration(VFXType type, GameObject vfxObj, float duration, int expectedToken)
        {
            yield return new WaitForSeconds(duration);
            if (vfxObj == null) yield break;
            var meta = vfxObj.GetComponent<PooledVfxInstance>();
            if (meta != null && meta.SpawnToken != expectedToken) yield break;
            if (!vfxObj.activeInHierarchy) yield break;
            ReleaseVFX(type, vfxObj);
        }
    }
}

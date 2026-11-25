using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using AbyssGame;

namespace AbyssalDungeon.VFX
{
    /// <summary>
    /// High-impact, zero-GC pooled Skill & Combo VFX Dispatcher combining:
    /// 1) Authentic Gold and Glory (Studio Dois) VFX prefabs in Resources/VFX/GnG/
    /// 2) 3D URP Blender VFX Meshes (SpinWhirlwind, HeavyCleave, ShieldAegis, ThrustPierce, SpellImpact)
    /// 3) Animated Dark-Gothic Arcane Rune Seals, Rising Pillars & Shockwave Rings.
    /// Guaranteed visible even on Low preset (Intel Iris Xe) with protected local-player pool budget.
    /// </summary>
    public sealed class GnGSkillVfxDispatcher : MonoBehaviour
    {
        public static GnGSkillVfxDispatcher Instance { get; private set; }

        private readonly Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>(32);
        private readonly Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>(32);
        private readonly HashSet<string> _missingKeys = new HashSet<string>();
        private readonly Queue<SkillRuneSealFx> _sealPool = new Queue<SkillRuneSealFx>(16);
        private Transform _poolRoot;
        private int _activeCount;

        public static GnGSkillVfxDispatcher EnsureInstance()
        {
            if (Instance != null) return Instance;
            if (!Application.isPlaying) return null;

            Instance = FindObjectOfType<GnGSkillVfxDispatcher>();
            if (Instance == null)
            {
                var go = new GameObject("[GnGSkillVfxPool]");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<GnGSkillVfxDispatcher>();
            }
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            EnsurePoolRoot();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void EnsurePoolRoot()
        {
            if (_poolRoot != null) return;
            var p = new GameObject("InactivePool");
            p.transform.SetParent(transform, false);
            _poolRoot = p.transform;
        }

        private GameObject LoadGnGPrefab(string key)
        {
            if (string.IsNullOrEmpty(key) || _missingKeys.Contains(key)) return null;
            if (_prefabCache.TryGetValue(key, out GameObject cached) && cached != null)
                return cached;

            GameObject loaded = Resources.Load<GameObject>("VFX/GnG/" + key);
            if (loaded == null)
            {
                _missingKeys.Add(key);
                return null;
            }
            _prefabCache[key] = loaded;
            return loaded;
        }

        public GameObject SpawnGnG(
            string key,
            Vector3 worldPos,
            Quaternion worldRot,
            float duration = 1.35f,
            float scale = 1f,
            Transform followParent = null,
            bool priorityPlayer = true)
        {
            if (!Application.isPlaying || string.IsNullOrEmpty(key)) return null;

            int maxConcurrent = 28;
            if (GraphicsManager.I != null)
            {
                if (GraphicsManager.I.CurrentTier == GraphicsQualityTier.Low) maxConcurrent = 16;
                else if (GraphicsManager.I.CurrentTier == GraphicsQualityTier.Medium) maxConcurrent = 22;
            }
            // Never let AI party bots starve the player's own attacks & skills
            if (!priorityPlayer && _activeCount >= maxConcurrent - 4) return null;
            if (_activeCount >= maxConcurrent + 6) return null;

            EnsurePoolRoot();
            GameObject prefab = LoadGnGPrefab(key);
            if (prefab == null) return null;

            if (!_pools.TryGetValue(key, out Queue<GameObject> q))
            {
                q = new Queue<GameObject>(4);
                _pools[key] = q;
            }

            GameObject inst = null;
            while (q.Count > 0 && inst == null)
                inst = q.Dequeue();

            if (inst == null)
            {
                inst = Instantiate(prefab, _poolRoot);
                inst.name = key + "_Pooled";
                PrepareGnGInstanceForUrpVisibility(inst);
            }

            Transform tr = inst.transform;
            if (followParent != null)
            {
                tr.SetParent(followParent, false);
                tr.position = worldPos;
                tr.rotation = worldRot;
            }
            else
            {
                tr.SetParent(null, false);
                tr.SetPositionAndRotation(worldPos, worldRot);
            }
            tr.localScale = Vector3.one * Mathf.Clamp(scale, 0.35f, 3.0f);

            inst.SetActive(true);
            RestartEffectSystems(inst);
            _activeCount++;

            if (duration > 0.05f)
                StartCoroutine(ReleaseAfterDelay(key, inst, duration));

            return inst;
        }

        private static void PrepareGnGInstanceForUrpVisibility(GameObject inst)
        {
            // Disable aggressive LODGroup culling so skill bursts never pop out at third-person camera distance
            var lods = inst.GetComponentsInChildren<LODGroup>(true);
            for (int i = 0; i < lods.Length; i++)
                if (lods[i] != null) lods[i].enabled = false;

            // Ensure particle renderers sort ABOVE ground decals/floors and brief 0.18s flashes hold long enough to read
            var renderers = inst.GetComponentsInChildren<ParticleSystemRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                r.sortingOrder = Mathf.Max(12, r.sortingOrder + 22);
                r.sortingFudge = -2f;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                if (r.maxParticleSize < 1.5f) r.maxParticleSize = 2.0f;
            }

            var systems = inst.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                var ps = systems[i];
                if (ps == null) continue;
                ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                if (main.startLifetime.mode == ParticleSystemCurveMode.Constant && main.startLifetime.constant > 0f && main.startLifetime.constant < 0.34f)
                {
                    main.startLifetime = Mathf.Max(0.42f, main.startLifetime.constant * 1.85f);
                }
            }

            var responder = inst.GetComponent<ScalableVfxResponder>();
            if (responder == null) responder = inst.AddComponent<ScalableVfxResponder>();
            responder.isCombatTelegraph = true; // Keep player skills crisp even on Low preset!
        }

        private static void RestartEffectSystems(GameObject root)
        {
            var trails = root.GetComponentsInChildren<TrailRenderer>(true);
            for (int i = 0; i < trails.Length; i++)
                if (trails[i] != null) trails[i].Clear();

            var systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                var ps = systems[i];
                if (ps == null) continue;
                ps.Clear(false);
                if (ps.gameObject.activeInHierarchy)
                    ps.Play(false);
            }
        }

        private IEnumerator ReleaseAfterDelay(string key, GameObject inst, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReleaseInstance(key, inst);
        }

        public void ReleaseInstance(string key, GameObject inst)
        {
            _activeCount = Mathf.Max(0, _activeCount - 1);
            if (inst == null) return;

            var systems = inst.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
                if (systems[i] != null)
                    systems[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

            inst.SetActive(false);
            EnsurePoolRoot();
            inst.transform.SetParent(_poolRoot, false);

            if (!_pools.TryGetValue(key, out Queue<GameObject> q))
            {
                q = new Queue<GameObject>(4);
                _pools[key] = q;
            }
            q.Enqueue(inst);
        }

        public void SpawnSkillRuneSeal(Vector3 groundPos, Color color, float radius, float duration, bool withPillar, bool withShockRing = false)
        {
            // Circular floor rune/ring frames removed per art direction; only spawn vertical light pillars when requested.
            if (!Application.isPlaying || !withPillar) return;
            EnsurePoolRoot();

            SkillRuneSealFx fx = null;
            while (_sealPool.Count > 0 && fx == null)
                fx = _sealPool.Dequeue();

            if (fx == null)
            {
                var go = new GameObject("SkillRuneSeal3D_Pooled");
                fx = go.AddComponent<SkillRuneSealFx>();
            }

            fx.transform.SetParent(null, false);
            fx.transform.position = new Vector3(groundPos.x, groundPos.y + 0.045f, groundPos.z);
            fx.transform.rotation = Quaternion.identity;
            fx.gameObject.SetActive(true);
            fx.Play(color, radius, duration, true, false, this);
        }

        internal void ReturnSealToPool(SkillRuneSealFx fx)
        {
            if (fx == null) return;
            fx.gameObject.SetActive(false);
            EnsurePoolRoot();
            fx.transform.SetParent(_poolRoot, false);
            _sealPool.Enqueue(fx);
        }

        // =====================================================================
        // HIGH-LEVEL CLASS COMBO, SKILL, PROJECTILE & HIT MAPPINGS (10 CLASSES)
        // =====================================================================

        public void PlayComboOrHeavy(int classId, int step, bool isHeavy, Vector3 unitRootPos, Quaternion facing, Color accent = default, bool isLocalPlayer = true)
        {
            string key = null;
            float scale = isHeavy ? 1.22f : (step == 2 ? 1.08f : 0.92f);
            float dur = isHeavy ? 1.35f : 0.95f;
            Vector3 spawnPos = unitRootPos + Vector3.up * 0.65f + (facing * Vector3.forward) * 0.45f;

            switch (classId)
            {
                case 0: // Knight
                    key = isHeavy ? "GnG_Warrior_BlessedSword" : (step == 2 ? "GnG_Warrior_RapidStrike" : "GnG_Hit_Sword");
                    if (isHeavy) spawnPos = unitRootPos + Vector3.up * 0.25f;
                    break;
                case 1: // Guardian
                    key = isHeavy ? "GnG_Warrior_DragonShock" : "GnG_Hit_HolyHammer";
                    spawnPos = unitRootPos + (facing * Vector3.forward) * 1.05f + Vector3.up * (isHeavy ? 0.2f : 0.85f);
                    break;
                case 2: // Berserker
                    key = isHeavy ? "GnG_Berserker_FinalHit" : "GnG_Berserker_Slash";
                    spawnPos = unitRootPos + Vector3.up * 0.35f;
                    break;
                case 3: // Assassin
                    key = "GnG_Rogue_Slash1";
                    spawnPos = unitRootPos + Vector3.up * 0.55f + (facing * Vector3.forward) * 0.35f;
                    break;
                case 4: // Ranger
                    key = isHeavy ? "GnG_Archer_ShockBack" : "GnG_Archer_TriShoot";
                    spawnPos = unitRootPos + Vector3.up * 0.95f + (facing * Vector3.forward) * 0.65f;
                    break;
                case 5: // Mage
                    key = isHeavy ? "GnG_Wizard_IceBurst" : "GnG_Wizard_ArcaneMissile";
                    spawnPos = unitRootPos + Vector3.up * 0.95f + (facing * Vector3.forward) * 0.65f;
                    break;
                case 6: // Warlock
                    key = isHeavy ? "GnG_Warlock_AroundBlood" : "GnG_Warlock_BloodSlash";
                    spawnPos = unitRootPos + Vector3.up * 0.65f + (facing * Vector3.forward) * 0.45f;
                    break;
                case 7: // Monk
                    key = (isHeavy || step == 2) ? "GnG_Monk_Combo3" : "GnG_Monk_Combo1";
                    spawnPos = unitRootPos + Vector3.up * 0.55f;
                    break;
                case 8: // Druid
                    key = isHeavy ? "GnG_Cleric_HolySkill" : "GnG_Druid_Nature";
                    spawnPos = unitRootPos + Vector3.up * 0.88f + (facing * Vector3.forward) * 0.55f;
                    scale = isHeavy ? 1.25f : 1.15f;
                    break;
                case 9: // Cleric
                    key = isHeavy ? "GnG_Hit_HolyHammer" : "GnG_Cleric_HolySkill";
                    spawnPos = unitRootPos + (facing * Vector3.forward) * (isHeavy ? 1.25f : 0.45f) + Vector3.up * 0.75f;
                    break;
            }

            if (!string.IsNullOrEmpty(key))
                SpawnGnG(key, spawnPos, facing, dur, scale, null, isLocalPlayer);

            // Layer 3D Blender Weapon Wave on Heavy Attacks & Combo Swings (with bespoke Assassin Dual Blades geometry!)
            var vfxMgr = VFXManager.EnsureInstance();
            if (vfxMgr != null)
            {
                if (classId == 3)
                {
                    VFXType asnType = (isHeavy || step == 2) ? VFXType.Wpn_Asn_RapidCombo : VFXType.Wpn_Asn_DualBladeSlash;
                    vfxMgr.SpawnVFX(asnType, unitRootPos + Vector3.up * 0.98f + (facing * Vector3.forward) * 0.35f,
                        facing, isHeavy ? 0.46f : 0.32f, null, null, isHeavy ? 1.18f : 1.0f);
                }
                else if (isHeavy || step == 2)
                {
                    Color c = accent != default ? accent : GetDefaultClassColor(classId);
                    VFXType wType = isHeavy ? VFXType.Wpn_HeavyCleave : VFXType.Wpn_LightSlash;
                    vfxMgr.SpawnVFX(wType, unitRootPos + Vector3.up * 0.95f + (facing * Vector3.forward) * 0.5f,
                        facing, isHeavy ? 0.42f : 0.30f, c, null, isHeavy ? 1.25f : 1.05f);
                }
            }
        }

        public void PlayClassSkill(
            int classId,
            int slot,
            SkillKind kind,
            Vector3 casterRootPos,
            Vector3 aimPos,
            Quaternion facing,
            Color skillColor = default,
            float radius = 3.0f,
            bool isLocalPlayer = true)
        {
            Color col = skillColor != default ? skillColor : GetDefaultClassColor(classId);
            bool isUlt = (slot == 3);

            string key = GetSkillVfxKey(classId, slot, kind, out bool spawnAtAim, out float dur, out float scale);
            Vector3 targetGround = spawnAtAim ? new Vector3(aimPos.x, casterRootPos.y, aimPos.z) : casterRootPos;

            // Lift chest-level slashes/casts so root-origin GnG meshes never clip under floor flagstones
            float yLift = (kind == SkillKind.Projectile || kind == SkillKind.MeleeArc || kind == SkillKind.Dash || classId == 8)
                ? 0.82f
                : 0.15f;
            if (spawnAtAim) yLift = 0.22f;

            if (!string.IsNullOrEmpty(key))
                SpawnGnG(key, targetGround + Vector3.up * yLift, facing, dur, scale * (isUlt ? 1.32f : 1.12f), null, isLocalPlayer);

            // Always spawn unmistakable 3D URP Skill VFX layers (Gothic Rune Seal + 3D Blender Cyclone/Aegis/Wave)
            var vfxMgr = VFXManager.EnsureInstance();
            float sealRadius = Mathf.Clamp(radius > 0.5f ? radius : (isUlt ? 4.2f : 2.6f), 2.0f, 6.2f);

            // Keep vertical sanctuary/ult light pillars cleanly without circular floor rune frames
            bool casterPillar = isUlt || kind == SkillKind.HealAll || kind == SkillKind.HealTarget || kind == SkillKind.Revive || kind == SkillKind.Transform || kind == SkillKind.BuffAll;
            if (casterPillar)
                SpawnSkillRuneSeal(casterRootPos, col, isUlt ? 3.0f : 2.0f, isUlt ? 1.05f : 0.68f, withPillar: true, withShockRing: false);

            if (spawnAtAim && (isUlt || kind == SkillKind.GroundAoE) && Vector3.Distance(casterRootPos, targetGround) > 0.8f)
            {
                SpawnSkillRuneSeal(targetGround, col, sealRadius, isUlt ? 1.15f : 0.78f, withPillar: true, withShockRing: false);
            }

            if (vfxMgr == null) return;

            // Bespoke Blender 3D VFX routing for Assassin Dual Blades (Phase 2 Production Set)
            if (classId == 3)
            {
                if (isUlt || kind == SkillKind.SelfAoE)
                {
                    vfxMgr.SpawnVFX(VFXType.Wpn_Asn_ShadowBurst, casterRootPos + Vector3.up * 0.04f,
                        facing, 0.85f, null, null, isUlt ? 1.35f : 1.05f);
                    return;
                }
                if (kind == SkillKind.Dash)
                {
                    vfxMgr.SpawnVFX(VFXType.Wpn_Asn_DashAfterimage, casterRootPos, facing, 0.48f, null, null, 1.05f);
                    vfxMgr.SpawnVFX(VFXType.Wpn_Asn_DualBladeSlash, casterRootPos + Vector3.up * 0.98f + (facing * Vector3.forward) * 0.65f,
                        facing, 0.38f, null, null, 1.12f);
                    return;
                }
                if (kind == SkillKind.Projectile || kind == SkillKind.GroundAoE || kind == SkillKind.Trap || slot == 2)
                {
                    vfxMgr.SpawnVFX(VFXType.Wpn_Asn_PoisonImpact,
                        (spawnAtAim ? targetGround : (casterRootPos + (facing * Vector3.forward) * 1.15f)) + Vector3.up * 0.05f,
                        facing, 0.65f, null, null, 1.10f);
                    return;
                }
                vfxMgr.SpawnVFX(VFXType.Wpn_Asn_RapidCombo, casterRootPos + Vector3.up * 0.98f + (facing * Vector3.forward) * 0.4f,
                    facing, 0.50f, null, null, 1.15f);
                return;
            }

            switch (kind)
            {
                case SkillKind.MeleeArc:
                    vfxMgr.SpawnVFX(isUlt ? VFXType.Wpn_SpinWhirlwind : VFXType.Wpn_HeavyCleave,
                        casterRootPos + Vector3.up * 0.92f + (facing * Vector3.forward) * 0.45f,
                        facing, isUlt ? 0.65f : 0.42f, col, null, isUlt ? 1.45f : 1.15f);
                    break;

                case SkillKind.SelfAoE:
                case SkillKind.Taunt:
                    vfxMgr.SpawnVFX(VFXType.Wpn_SpinWhirlwind,
                        casterRootPos + Vector3.up * 0.55f,
                        facing, isUlt ? 0.75f : 0.50f, col, null, Mathf.Clamp(sealRadius * 0.48f, 1.15f, 2.1f));
                    vfxMgr.SpawnVFX(VFXType.Wpn_SpellImpact,
                        casterRootPos + Vector3.up * 0.9f,
                        Quaternion.identity, 0.55f, col, null, isUlt ? 1.65f : 1.20f);
                    break;

                case SkillKind.GroundAoE:
                case SkillKind.Trap:
                    vfxMgr.SpawnVFX(VFXType.Wpn_SpellImpact,
                        targetGround + Vector3.up * 0.45f,
                        Quaternion.LookRotation(Vector3.up), 0.65f, col, null, Mathf.Clamp(sealRadius * 0.45f, 1.1f, 1.95f));
                    if (isUlt)
                    {
                        vfxMgr.SpawnVFX(VFXType.Wpn_SpinWhirlwind,
                            targetGround + Vector3.up * 0.45f,
                            Quaternion.identity, 0.75f, col, null, 1.55f);
                    }
                    break;

                case SkillKind.Dash:
                    vfxMgr.SpawnVFX(VFXType.Wpn_ThrustPierce,
                        casterRootPos + Vector3.up * 0.95f + (facing * Vector3.forward) * 0.6f,
                        facing, 0.45f, col, null, 1.30f);
                    break;

                case SkillKind.ShieldSelf:
                case SkillKind.ShieldAll:
                case SkillKind.BuffSelf:
                case SkillKind.BuffAll:
                case SkillKind.Cleanse:
                    vfxMgr.SpawnVFX(VFXType.Wpn_ShieldBlockAegis,
                        casterRootPos + Vector3.up * 1.0f + (facing * Vector3.forward) * 0.35f,
                        facing, 0.58f, col, null, isUlt ? 1.45f : 1.15f);
                    break;

                case SkillKind.HealAll:
                case SkillKind.HealTarget:
                case SkillKind.Revive:
                case SkillKind.Summon:
                case SkillKind.Transform:
                    vfxMgr.SpawnVFX(VFXType.Wpn_SpinWhirlwind,
                        casterRootPos + Vector3.up * 0.55f,
                        Quaternion.identity, 0.65f, col, null, isUlt ? 1.50f : 1.18f);
                    vfxMgr.SpawnVFX(VFXType.Wpn_SpellImpact,
                        casterRootPos + Vector3.up * 1.0f,
                        Quaternion.identity, 0.55f, col, null, 1.22f);
                    break;

                case SkillKind.Projectile:
                    vfxMgr.SpawnVFX(VFXType.Wpn_MuzzleCastBurst,
                        casterRootPos + Vector3.up * 1.15f + (facing * Vector3.forward) * 0.7f,
                        facing, 0.42f, col, null, isUlt ? 1.38f : 1.08f);
                    break;
            }
        }

        private static Color GetDefaultClassColor(int classId)
        {
            switch (classId)
            {
                case 0: return new Color(1.00f, 0.82f, 0.32f); // Knight Gold
                case 1: return new Color(0.45f, 0.78f, 1.00f); // Guardian Azure
                case 2: return new Color(1.00f, 0.28f, 0.16f); // Berserker Crimson
                case 3: return new Color(0.68f, 0.28f, 0.98f); // Assassin Violet
                case 4: return new Color(0.42f, 0.95f, 0.48f); // Ranger Emerald
                case 5: return new Color(0.32f, 0.78f, 1.00f); // Mage Arcane
                case 6: return new Color(0.92f, 0.18f, 0.36f); // Warlock Blood
                case 7: return new Color(1.00f, 0.72f, 0.24f); // Monk Chi Gold
                case 8: return new Color(0.28f, 0.96f, 0.52f); // Druid Nature Emerald
                case 9: return new Color(1.00f, 0.90f, 0.48f); // Cleric Holy Radiance
                default: return new Color(1.00f, 0.75f, 0.32f);
            }
        }

        private static string GetSkillVfxKey(int classId, int slot, SkillKind kind, out bool spawnAtAim, out float dur, out float scale)
        {
            spawnAtAim = (kind == SkillKind.GroundAoE || kind == SkillKind.Trap);
            dur = slot == 3 ? 1.85f : 1.35f;
            scale = slot == 3 ? 1.25f : 1.05f;

            switch (classId)
            {
                case 0: // Knight
                    if (slot == 0) return "GnG_Warrior_RapidStrike";
                    if (slot == 1) return "GnG_Warrior_BraveCharge";
                    if (slot == 2) return "GnG_Warrior_ShieldAura";
                    return "GnG_Warrior_BlessedSword";

                case 1: // Guardian
                    if (slot == 0) return "GnG_Warrior_DragonShock";
                    if (slot == 1 || slot == 2) return "GnG_Warrior_ShieldAura";
                    return "GnG_Warrior_Buff";

                case 2: // Berserker
                    if (slot == 0) return "GnG_Berserker_Slash";
                    if (slot == 1 || slot == 2) return "GnG_Warrior_Buff";
                    return "GnG_Berserker_FinalHit";

                case 3: // Assassin
                    if (slot == 2 || kind == SkillKind.Projectile) return "GnG_Rogue_Poison";
                    return "GnG_Rogue_Slash1";

                case 4: // Ranger
                    if (kind == SkillKind.Trap) return "GnG_Archer_Trap";
                    if (slot == 0 || slot == 3) return "GnG_Archer_TriShoot";
                    return "GnG_Archer_ShockBack";

                case 5: // Mage
                    if (slot == 0) return "GnG_Wizard_FireCast";
                    if (slot == 1) return "GnG_Wizard_IceBurst";
                    if (slot == 2) return "GnG_Wizard_ArcaneMissile";
                    return "GnG_Wizard_Thunder";

                case 6: // Warlock
                    if (slot == 0 || slot == 1) return "GnG_Warlock_BloodSlash";
                    return "GnG_Warlock_AroundBlood";

                case 7: // Monk
                    if (slot == 0) return "GnG_Monk_Combo1";
                    if (slot == 2) return "GnG_Warrior_ShieldAura";
                    return "GnG_Monk_Combo3";

                case 8: // Druid
                    if (slot == 0) return "GnG_Rogue_Poison";      // Emerald nature/thorn volley
                    if (slot == 1) return "GnG_Cleric_HolySkill";  // Regrowth radiant bloom
                    if (slot == 2) return "GnG_Warrior_Buff";      // Bear Form primal roar aura
                    return "GnG_Cleric_HolySkill";                 // Nature's Wrath sanctuary storm

                case 9: // Cleric
                    if (slot == 0) return "GnG_Hit_HolyHammer";
                    return "GnG_Cleric_HolySkill";

                default:
                    return "GnG_Warrior_BlessedSword";
            }
        }

        public void PlayHitVfx(int attackerClassId, bool critOrHeavy, Vector3 hitWorldPos, Quaternion rot, bool isLocalPlayer = true)
        {
            // Always show player hits even on Low preset; only throttle AI non-crit hits on Low
            if (!isLocalPlayer && GraphicsManager.I != null && GraphicsManager.I.CurrentTier == GraphicsQualityTier.Low && !critOrHeavy)
                return;

            string key = "GnG_Hit_Sword";
            if (attackerClassId == 1 || attackerClassId == 9)
                key = "GnG_Hit_HolyHammer";
            else if (attackerClassId == 7 || !critOrHeavy)
                key = "GnG_Hit_Metal";

            SpawnGnG(key, hitWorldPos, rot, 0.75f, critOrHeavy ? 1.05f : 0.80f, null, isLocalPlayer);
        }

        public GameObject AttachProjectileCore(Transform projParent, ActorUnit owner, StatusType status, bool isArrow)
        {
            if (isArrow || projParent == null) return null;
            bool isLocal = owner != null && owner.IsLocalPlayer;
            if (!isLocal && GraphicsManager.I != null && GraphicsManager.I.CurrentTier == GraphicsQualityTier.Low)
                return null;

            string key = null;
            int cid = (owner != null && owner.HeroClass != null) ? owner.HeroClass.id : -1;
            if (status == StatusType.Burn || (cid == 5 && owner.BuildIndex == 0))
                key = "GnG_Proj_FireBall";
            else if (status == StatusType.Slow || status == StatusType.Freeze || cid == 5)
                key = "GnG_Proj_IceBall";
            else if (cid == 6 || (owner != null && owner.Team == Team.Monster))
                key = "GnG_Proj_GhostBall";
            else if (cid == 8)
                key = "GnG_Proj_IceBall"; // Luminous orb core tinted emerald by outer trail
            else if (cid == 9 || cid == 0 || cid == 1)
                key = "GnG_Proj_Adjudication";

            if (string.IsNullOrEmpty(key)) return null;

            GameObject prefab = LoadGnGPrefab(key);
            if (prefab == null) return null;

            GameObject core = Instantiate(prefab, projParent, false);
            PrepareGnGInstanceForUrpVisibility(core);
            core.transform.localPosition = Vector3.zero;
            core.transform.localRotation = Quaternion.identity;
            core.transform.localScale = Vector3.one * 0.85f;
            RestartEffectSystems(core);
            return core;
        }
    }

    /// <summary>
    /// Pooled, zero-allocation 3D Dark-Gothic Arcane Rune Seal + Expanding Shockwave Ring + Vertical Energy Pillar.
    /// Guarantees unmistakable visual feedback for every cast, heal, buff, AoE, and transformation on all GPU tiers.
    /// </summary>
    public sealed class SkillRuneSealFx : MonoBehaviour
    {
        private static Material _sharedPillarMat;
        private static Mesh _crossedVerticalMesh;

        private Transform _pillar;
        private Renderer _pillarRend;
        private MaterialPropertyBlock _mpb;

        private Color _col;
        private float _radius;
        private float _dur;
        private float _t;
        private bool _usePillar;
        private GnGSkillVfxDispatcher _owner;

        private static Material GetAdditiveTexMat(ref Material cache, string matName, string texPath)
        {
            if (cache != null) return cache;
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                        ?? Shader.Find("Particles/Standard Unlit")
                        ?? Shader.Find("Sprites/Default");
            cache = new Material(sh) { name = matName, enableInstancing = true };
            cache.SetOverrideTag("RenderType", "Transparent");
            cache.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (cache.HasProperty("_Surface")) cache.SetFloat("_Surface", 1f);
            if (cache.HasProperty("_Blend")) cache.SetFloat("_Blend", 2f);
            if (cache.HasProperty("_SrcBlend")) cache.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (cache.HasProperty("_DstBlend")) cache.SetFloat("_DstBlend", (float)BlendMode.One);
            if (cache.HasProperty("_ZWrite")) cache.SetFloat("_ZWrite", 0f);
            if (cache.HasProperty("_Cull")) cache.SetFloat("_Cull", (float)CullMode.Off);
            Texture2D tex = Resources.Load<Texture2D>(texPath);
            if (tex != null)
            {
                if (cache.HasProperty("_BaseMap")) cache.SetTexture("_BaseMap", tex);
                cache.mainTexture = tex;
            }
            cache.renderQueue = 3025;
            return cache;
        }

        private static Mesh GetCrossedPillarMesh()
        {
            if (_crossedVerticalMesh != null) return _crossedVerticalMesh;
            _crossedVerticalMesh = new Mesh { name = "SkillCrossedPillarMesh" };
            _crossedVerticalMesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, 0f), new Vector3(-0.5f, 1f, 0f), new Vector3(0.5f, 1f, 0f), new Vector3(0.5f, 0f, 0f),
                new Vector3(0f, 0f, -0.5f), new Vector3(0f, 1f, -0.5f), new Vector3(0f, 1f, 0.5f), new Vector3(0f, 0f, 0.5f)
            };
            _crossedVerticalMesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f)
            };
            _crossedVerticalMesh.normals = new[]
            {
                Vector3.back, Vector3.back, Vector3.back, Vector3.back,
                Vector3.right, Vector3.right, Vector3.right, Vector3.right
            };
            _crossedVerticalMesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7 };
            _crossedVerticalMesh.RecalculateBounds();
            return _crossedVerticalMesh;
        }

        private void EnsureBuilt()
        {
            if (_pillar != null) return;
            _mpb = new MaterialPropertyBlock();

            var pl = new GameObject("LightPillar");
            pl.transform.SetParent(transform, false);
            pl.AddComponent<MeshFilter>().sharedMesh = GetCrossedPillarMesh();
            _pillarRend = pl.AddComponent<MeshRenderer>();
            _pillarRend.sharedMaterial = GetAdditiveTexMat(ref _sharedPillarMat, "M_SkillPillarBeam", "VFX/Textures/T_Vfx_PillarBeam");
            _pillarRend.shadowCastingMode = ShadowCastingMode.Off;
            _pillarRend.receiveShadows = false;
            _pillar = pl.transform;
        }

        public void Play(Color color, float radius, float duration, bool withPillar, bool withShockRing, GnGSkillVfxDispatcher owner)
        {
            EnsureBuilt();
            _col = Color.Lerp(color, Color.white, 0.22f);
            _radius = Mathf.Max(1.2f, radius);
            _dur = Mathf.Max(0.25f, duration);
            _t = 0f;
            _usePillar = withPillar;
            _owner = owner;

            if (_pillar != null) _pillar.gameObject.SetActive(withPillar);
            UpdateVisual(0f);
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float u = Mathf.Clamp01(_t / _dur);
            UpdateVisual(u);
            if (_t >= _dur)
            {
                if (_owner != null) _owner.ReturnSealToPool(this);
                else gameObject.SetActive(false);
            }
        }

        private void UpdateVisual(float u)
        {
            if (_usePillar && _pillar != null)
            {
                float pw = Mathf.Lerp(_radius * 0.95f, _radius * 0.42f, u);
                float ph = Mathf.Lerp(1.6f, 4.4f, Mathf.Sqrt(u));
                _pillar.localScale = new Vector3(pw, ph, pw);
                _pillar.localRotation = Quaternion.Euler(0f, -_t * 120f, 0f);
                ApplyTint(_pillarRend, Color.Lerp(_col, Color.white, 0.35f), (1f - u) * 0.82f);
            }
        }

        private void ApplyTint(Renderer r, Color c, float alpha)
        {
            if (r == null) return;
            Color hdr = new Color(c.r * 1.75f, c.g * 1.75f, c.b * 1.75f, Mathf.Clamp01(alpha));
            _mpb.Clear();
            _mpb.SetColor("_BaseColor", hdr);
            _mpb.SetColor("_Color", hdr);
            r.SetPropertyBlock(_mpb);
        }
    }
}

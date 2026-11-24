using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssGame
{
    public class ActiveStatus
    {
        public StatusType type;
        public float remaining;
        public float power;
        public float tickTimer;
        public ActorUnit source;
    }

    /// <summary>
    /// Core 3D combatant used by Local Player, AI Party Members, Summons, 10 Monster types, Mini-Boss, and 3-Phase Boss.
    /// </summary>
    [DefaultExecutionOrder(0)]
    public class ActorUnit : MonoBehaviour
    {
        public static readonly List<ActorUnit> AllUnits = new List<ActorUnit>();

        public Team Team;
        public string DisplayName = "Unit";
        public bool IsLocalPlayer;
        public bool IsSummon;
        public bool IsElite;
        public bool IsMiniBoss;
        public bool IsBoss;
        public int PartySlotIndex = -1;
        public EliteAffixKind ActiveEliteAffix = EliteAffixKind.None;

        public ClassDef HeroClass;
        public int BuildIndex;
        public EnemyDef MonsterDef;
        public string BakedEnemyId = "";
        public bool IsRoomDormant;
        public int Level = 1;

        // Computed stats
        public float MaxHp = 500f;
        public float Hp = 500f;
        public float Shield = 0f;
        public float MaxMana = 200f;
        public float Mana = 200f;
        public float Atk = 50f;
        public float Def = 20f;
        public float AtkSpeed = 1f;
        public float CritChance = 0.1f;
        public float CritDamage = 1.85f;
        public float MoveSpeed = 5f;
        public float Lifesteal = 0f;
        public float CooldownReduction = 0f;
        public float HealBonus = 0f;
        public float AttackRange = 2.5f;
        public bool IsRanged = false;

        // State & Phase 2 Locomotion / Stamina
        public float MaxStamina = 100f;
        public float Stamina = 100f;
        public bool IsSprinting;
        public bool IsWalking;
        public float VisualBaseScale = 1f;
        public bool IsDead;
        public bool IsDowned;
        public float ReviveProgress;
        public bool IsInvincible;
        public bool InBearForm;
        private float _bearFormTimer;
        private float _summonLifeTimer;
        private float _staminaRegenDelay;
        private float _coyoteTimer;
        private float _jumpCooldown;
        private CharacterMotionPolisher _motionPolisher;

        public bool IsGroundedUnit => IsDead || Controller == null || !Controller.enabled
            || ((Controller.isGrounded || transform.position.y <= 0.04f) && _velocity.y <= 0.1f);

        // Cooldowns & Phase 3 Core Combat State (Combo Chain, Heavy Attack, Poise/Stagger, Knockback)
        public float BasicAttackCd;
        public float HeavyAttackCd;
        public float DodgeCd;
        public readonly float[] SkillCds = new float[4];
        public readonly float[] SkillMaxCds = new float[4];

        public int ComboStep;
        private float _comboWindowTimer;
        public float MaxPoise = 60f;
        public float Poise = 60f;
        public bool IsStaggered;
        public bool IsEnraged;
        public bool IsDodging { get; private set; }
        public float PoiseImmunityTimer;
        public bool IsPoiseImmune => PoiseImmunityTimer > 0f;
        private float _staggerTimer;
        private float _poiseRegenDelay;
        private float _lifestealBudget;
        private int _attackCancelVersion;
        private Vector3 _knockbackVel;
        private Vector3 _lungeVel;
        private Vector3 _queuedPlanarVel;
        private string _lastOverheadLabel;

        public Vector3 InterpolatedPosition => transform.position;

        public void SyncPhysicsPosition(Vector3 worldPos)
        {
            bool wasEnabled = Controller != null && Controller.enabled;
            if (wasEnabled) Controller.enabled = false;
            transform.position = worldPos;
            if (wasEnabled) Controller.enabled = true;
            _queuedPlanarVel = Vector3.zero;
        }

        public void GrantPoiseImmunity(float seconds)
        {
            PoiseImmunityTimer = Mathf.Max(PoiseImmunityTimer, seconds);
            if (IsStaggered)
            {
                IsStaggered = false;
                _staggerTimer = 0f;
                Poise = MaxPoise;
            }
        }

        public float ConsumeLifestealBudget(float requestedHeal)
        {
            if (requestedHeal <= 0f || _lifestealBudget <= 0.1f) return 0f;
            float granted = Mathf.Min(requestedHeal, _lifestealBudget);
            _lifestealBudget -= granted;
            return granted;
        }

        private WeaponHitboxSweep3D _hitboxSweep;
        private WeaponBladeTrail3D[] _weaponTrails;

        // Statuses
        public readonly List<ActiveStatus> Statuses = new List<ActiveStatus>();
        public ActorUnit TauntTarget;

        // Visuals & Physics
        public CharacterController Controller;
        public Animator Anim;
        public GameObject MainVisual;
        public GameObject BearVisual;
        private float _swingAnimTimer;
        private Vector3 _velocity;
        [SerializeField] private Transform _hpBarFill;
        [SerializeField] private Transform _staggerBarFill;
        [SerializeField] private Transform _shieldBarFill;
        [SerializeField] private Transform _hpBarRoot;
        [SerializeField] private TextMesh _nameLabel;

        // Events
        public event Action<ActorUnit, ActorUnit, float, bool> OnDamaged; // victim, attacker, dmg, crit
        public event Action<ActorUnit> OnKilled;

        private static readonly int HashSpeed = Animator.StringToHash("Speed");
        private static readonly int HashAttack = Animator.StringToHash("Attack");
        private static readonly int HashCast = Animator.StringToHash("Cast");
        private static readonly int HashDodge = Animator.StringToHash("Dodge");
        private static readonly int HashDead = Animator.StringToHash("IsDead");

        private void OnEnable()
        {
            if (!IsRoomDormant && !AllUnits.Contains(this)) AllUnits.Add(this);
        }

        private void OnDisable()
        {
            AllUnits.Remove(this);
        }

        public void SetRoomDormant(bool dormant)
        {
            IsRoomDormant = dormant;
            if (dormant)
            {
                AllUnits.Remove(this);
            }
            else if (isActiveAndEnabled && !AllUnits.Contains(this))
            {
                AllUnits.Add(this);
            }
            var mAi = GetComponent<MonsterAI>();
            if (mAi != null) mAi.enabled = !dormant;
            var bAi = GetComponent<AbyssalBossAI>();
            if (bAi != null) bAi.enabled = !dormant;
            if (_hpBarRoot != null) _hpBarRoot.gameObject.SetActive(!dormant && !IsDead);
        }

        public void InitHero(int classId, int buildId, int level, string displayName, bool isLocal, int partySlot)
        {
            Team = Team.Hero;
            HeroClass = GameDB.GetClass(classId);
            BuildIndex = Mathf.Clamp(buildId, 0, 2);
            Level = Mathf.Max(1, level);
            DisplayName = displayName;
            IsLocalPlayer = isLocal;
            PartySlotIndex = partySlot;
            gameObject.tag = isLocal ? "Player" : "Untagged";

            // Clean any pre-baked visual or locator ring children when re-initializing at runtime for current selectedClass/Build
            if (MainVisual != null)
            {
                BakedAssetRegistry.DestroySafe(MainVisual);
                MainVisual = null;
            }
            if (BearVisual != null)
            {
                BakedAssetRegistry.DestroySafe(BearVisual);
                BearVisual = null;
            }
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform ch = transform.GetChild(i);
                if (ch == null) continue;
                string cn = ch.name;
                if (cn.StartsWith("Visual_", StringComparison.Ordinal) ||
                    cn.StartsWith("BearForm", StringComparison.Ordinal) ||
                    cn == "GroundLocatorRing" ||
                    cn == "HumanGroundShadowGroup")
                {
                    BakedAssetRegistry.DestroySafe(ch.gameObject);
                }
            }

            EnsureController(0.45f, 1.9f);
            transform.localScale = Vector3.one;
            gameObject.layer = 0;
            MainVisual = CharacterVisualFactory.BuildHeroVisual(HeroClass, transform, out Anim, BuildIndex);
            if (MainVisual != null)
            {
                MainVisual.transform.localScale = Vector3.one;
                VisualBaseScale = 1f;
            }
            if (HeroClass.id == 8) // Druid has Bear Form mesh ready
            {
                BearVisual = CharacterVisualFactory.BuildBearFormMesh();
                BearVisual.transform.SetParent(transform, false);
                BearVisual.SetActive(false);
            }

            MaxStamina = 100f;
            Stamina = MaxStamina;
            MaxPoise = HeroClass.id <= 2 ? 125f : 85f;
            Poise = MaxPoise;
            RecalculateStats(true);

            // Step 4: Hide giant overhead green bar & huge name text on AI teammates;
            // keep only a clean small ground locator ring at their feet + left-corner Party HUD.
            Color locatorCol = isLocal
                ? new Color(1.0f, 0.84f, 0.30f, 0.85f)
                : Color.Lerp(new Color(0.28f, 0.92f, 0.55f), HeroClass.color, 0.45f);
            CharacterVisualFactory.SpawnGroundGlowRing(transform, locatorCol, isLocal ? 0.52f : 0.46f, "GroundLocatorRing");

            SetupCombatAttachments(0.45f, 1.9f);
            _motionPolisher = GetComponent<CharacterMotionPolisher>();
            if (_motionPolisher == null) _motionPolisher = gameObject.AddComponent<CharacterMotionPolisher>();
            _motionPolisher.Bind(this);
        }

        public void RebuildHeroVisualForBuild()
        {
            if (Team != Team.Hero || HeroClass == null) return;
            if (MainVisual != null) Destroy(MainVisual);
            MainVisual = CharacterVisualFactory.BuildHeroVisual(HeroClass, transform, out Anim, BuildIndex);
            MainVisual.SetActive(!InBearForm);
            VisualBaseScale = MainVisual.transform.localScale.x;
            RefreshWeaponTrailsCache();
            if (_motionPolisher != null) _motionPolisher.Bind(this);
        }

        public void InitMonster(EnemyDef def, int floor, float hpMult, float atkMult, bool elite, bool miniBoss = false)
        {
            Team = Team.Monster;
            MonsterDef = def;
            BakedEnemyId = def != null ? def.id : "";
            Level = Mathf.Max(1, floor);
            IsElite = elite || def.archetype == Archetype.Elite;
            IsMiniBoss = miniBoss || def.archetype == Archetype.MiniBoss;
            IsBoss = def.archetype == Archetype.Boss;
            DisplayName = IsBoss ? def.name : (IsMiniBoss ? "[Mini Boss] " + def.name : (IsElite ? "[Tinh Anh] " + def.name : def.name));

            float rad = IsBoss ? 0.95f : (def.model == ModelKind.Golem ? 0.8f : 0.48f);
            float h = IsBoss ? 3.2f : 2.0f;
            EnsureController(rad, h);

            if (MainVisual != null)
            {
                BakedAssetRegistry.DestroySafe(MainVisual);
                MainVisual = null;
            }
            if (_hpBarRoot != null)
            {
                BakedAssetRegistry.DestroySafe(_hpBarRoot.gameObject);
                _hpBarRoot = null;
            }

            MainVisual = CharacterVisualFactory.BuildMonsterVisual(def, IsElite || IsMiniBoss, transform, out Anim);
            if (IsMiniBoss && !IsBoss) MainVisual.transform.localScale *= 1.32f;
            else if (IsElite && def.archetype != Archetype.Elite && !IsBoss) MainVisual.transform.localScale *= 1.18f;
            VisualBaseScale = MainVisual != null ? MainVisual.transform.localScale.x : 1f;

            ApplyMonsterFloorStats(def, floor, hpMult, atkMult, rollNewAffix: true);

            if (Anim != null && Mathf.Abs(def.animSpeed - 1f) > 0.02f) Anim.speed = def.animSpeed;
            if (!IsBoss)
            {
                Color barCol = IsMiniBoss ? new Color(1f, 0.5f, 0.1f) : (IsElite ? new Color(0.95f, 0.75f, 0.15f) : new Color(0.9f, 0.22f, 0.22f));
                BuildOverheadBar(barCol, (IsMiniBoss ? 3.1f : 2.25f) * def.scale);
            }

            SetupCombatAttachments(rad, h);
            _motionPolisher = GetComponent<CharacterMotionPolisher>();
            if (_motionPolisher == null) _motionPolisher = gameObject.AddComponent<CharacterMotionPolisher>();
            _motionPolisher.Bind(this);
        }

        public void ReinitializePrebakedMonster(int floor, float hpMult, float atkMult)
        {
            Team = Team.Monster;
            EnemyDef def = MonsterDef;
            if (def == null || string.IsNullOrEmpty(def.id))
            {
                if (IsBoss || BakedEnemyId == "AbyssalKnight")
                    def = GameDB.Boss;
                else if (!string.IsNullOrEmpty(BakedEnemyId) && GameDB.Enemies.TryGetValue(BakedEnemyId, out var found))
                    def = found;
                else
                    def = GameDB.Enemies["Skeleton"];
            }
            MonsterDef = def;
            BakedEnemyId = def.id;

            if (Controller == null) Controller = GetComponent<CharacterController>();
            if (Anim == null && MainVisual != null) Anim = MainVisual.GetComponentInChildren<Animator>();
            if (MainVisual != null) VisualBaseScale = MainVisual.transform.localScale.x;

            ApplyMonsterFloorStats(def, floor, hpMult, atkMult, rollNewAffix: true);

            if (_nameLabel != null)
                _nameLabel.text = DisplayName;

            float rad = IsBoss ? 0.95f : (def.model == ModelKind.Golem ? 0.8f : 0.48f);
            float h = IsBoss ? 3.2f : 2.0f;
            SetupCombatAttachments(rad, h);
            _motionPolisher = GetComponent<CharacterMotionPolisher>();
            if (_motionPolisher == null) _motionPolisher = gameObject.AddComponent<CharacterMotionPolisher>();
            _motionPolisher.Bind(this);
        }

        private static void GetPartyReferenceStats(int floor, out float refHeroHp, out float refHeroAtk)
        {
            float sumHp = 0f;
            float sumAtk = 0f;
            int count = 0;

            for (int i = 0; i < AllUnits.Count; i++)
            {
                var u = AllUnits[i];
                if (u != null && u.Team == Team.Hero && !u.IsSummon)
                {
                    // Weight local player slightly higher so solo/party combat feels tuned around the player
                    float w = u.IsLocalPlayer ? 1.5f : 1.0f;
                    sumHp += u.MaxHp * w;
                    sumAtk += u.Atk * w;
                    count++;
                }
            }

            if (count > 0)
            {
                float weightTotal = count + 0.5f;
                refHeroHp = Mathf.Max(850f, sumHp / weightTotal);
                refHeroAtk = Mathf.Max(85f, sumAtk / weightTotal);
                return;
            }

            // Fallback estimate before heroes spawn (e.g., Edit Mode bake)
            float flScale = 1f + Mathf.Max(0, floor - 1) * 0.38f;
            refHeroHp = 950f * flScale;
            refHeroAtk = 95f * flScale;
        }

        private void ApplyMonsterFloorStats(EnemyDef def, int floor, float hpMult, float atkMult, bool rollNewAffix)
        {
            Level = Mathf.Max(1, floor);
            DisplayName = IsBoss ? def.name : (IsMiniBoss ? "[Mini Boss] " + def.name : (IsElite ? "[Tinh Anh] " + def.name : def.name));

            GetPartyReferenceStats(floor, out float refHeroHp, out float refHeroAtk);

            // Progressive Floor Scaling (HP, ATK, Attack Speed, Poise)
            float floorHpScale = 1f + (floor - 1) * 0.42f + Mathf.Pow(Mathf.Max(0, floor - 1), 1.35f) * 0.08f;
            float tierHp = IsBoss ? 7.2f : (IsMiniBoss ? 6.2f : (IsElite ? 4.6f : 2.35f));

            // Calibrate HP to both floor progression AND party attack power so Floor 13 fights stay thrilling (no 3s melts)
            float floorBasedHp = def.hp * floorHpScale * tierHp;
            float ttkTargetHits = IsBoss ? 88f : (IsMiniBoss ? 36f : (IsElite ? 18f : 6.2f));
            float partyScaledHp = refHeroAtk * ttkTargetHits * (1f + (floor - 1) * 0.04f);
            MaxHp = Mathf.Round(Mathf.Max(floorBasedHp, partyScaledHp * 0.72f) * hpMult);
            Hp = MaxHp;
            MaxMana = 100f;
            Mana = 100f;

            // Poise scales gently with floor so Bosses & Elites require intentional Heavy Attacks / skills to break
            float poiseFloorScale = 1f + (floor - 1) * 0.08f;
            MaxPoise = Mathf.Round((IsBoss ? 420f : (IsMiniBoss ? 250f : (IsElite ? 150f : 70f))) * poiseFloorScale);
            Poise = MaxPoise;
            PoiseImmunityTimer = 0f;

            // Calibrate Monster & Boss ATK directly to Party MaxHP + asymptotic DR (~42-50% avg mitigation):
            // - Normal Monster basic: ~6-10% Hero MaxHP post-DR (raw ~13-17% refHeroHp)
            // - Elite Monster basic:  ~12-18% Hero MaxHP post-DR (raw ~24-30% refHeroHp)
            // - Mini-Boss basic:      ~14-20% Hero MaxHP post-DR (raw ~28-34% refHeroHp)
            // - Boss basic:           ~15-22% Hero MaxHP post-DR (raw ~31-38% refHeroHp)
            // - Boss Heavy / Red AoE: ~35-55% Hero MaxHP post-DR (via 2.35x-2.65x skill mult)
            float speciesAtkFactor = Mathf.Clamp(def.atk / 34f, 0.82f, 1.25f);
            float floorDangerBoost = 1f + Mathf.Min(0.28f, (floor - 1) * 0.022f);
            float baseTargetPct = IsBoss
                ? 0.34f
                : (IsMiniBoss ? 0.30f : (IsElite ? 0.26f : 0.145f * speciesAtkFactor));

            float calibratedAtk = refHeroHp * baseTargetPct * floorDangerBoost * atkMult;
            float legacyFloorAtk = def.atk * (1f + (floor - 1) * 0.34f) * (IsBoss ? 2.4f : (IsElite ? 2.1f : 1.75f)) * atkMult;
            Atk = Mathf.Round(Mathf.Max(calibratedAtk, legacyFloorAtk));

            Def = Mathf.Round(def.def * (1f + (floor - 1) * 0.22f));
            MoveSpeed = def.speed * (1f + Mathf.Min(0.20f, (floor - 1) * 0.015f));

            // Progressive Floor Attack Speed: higher floors attack noticeably faster (up to +55% at high floors)
            float floorAspdMult = 1f + Mathf.Min(0.55f, (floor - 1) * 0.04f);
            AtkSpeed = (1f / Mathf.Max(0.38f, def.attackCd)) * floorAspdMult;
            AttackRange = def.attackRange;
            IsRanged = (def.archetype == Archetype.Ranged || def.archetype == Archetype.Mage);
            CritChance = IsBoss ? 0.16f : (IsElite ? 0.10f : 0.05f);

            if ((IsElite || IsMiniBoss) && !IsBoss && rollNewAffix)
            {
                ActiveEliteAffix = EnemyTacticalBrain.RollEliteAffix(def, true);
                string afx = EnemyTacticalBrain.GetAffixLabel(ActiveEliteAffix);
                if (!string.IsNullOrEmpty(afx))
                    DisplayName = $"{DisplayName} [{afx}]";
                if (ActiveEliteAffix == EliteAffixKind.Vampiric)
                    Lifesteal = Mathf.Max(Lifesteal, 0.25f);
                else if (ActiveEliteAffix == EliteAffixKind.ArcaneShielded)
                    Shield = MaxHp * 0.28f;
            }
        }

        public void InitSummon(ActorUnit summoner, float duration)
        {
            Team = summoner.Team;
            IsSummon = true;
            DisplayName = "Tiểu Quỷ Hồn";
            Level = summoner.Level;
            _summonLifeTimer = duration;

            EnsureController(0.35f, 1.4f);
            MainVisual = CharacterVisualFactory.BuildSoulMinionMesh();
            MainVisual.transform.SetParent(transform, false);
            VisualBaseScale = MainVisual != null ? MainVisual.transform.localScale.x : 1f;
            Anim = MainVisual != null ? MainVisual.GetComponentInChildren<Animator>() : null;
            if (Anim != null) Anim.applyRootMotion = false;

            MaxHp = summoner.MaxHp * 0.45f;
            Hp = MaxHp;
            Atk = summoner.Atk * 0.55f;
            Def = summoner.Def * 0.5f;
            MoveSpeed = 6.2f;
            AtkSpeed = 1.2f;
            AttackRange = 10f;
            IsRanged = true;
            MaxPoise = 40f;
            Poise = MaxPoise;
            BuildOverheadBar(new Color(0.65f, 0.35f, 1f), 1.85f);
            SetupCombatAttachments(0.35f, 1.4f);
            _motionPolisher = GetComponent<CharacterMotionPolisher>();
            if (_motionPolisher == null) _motionPolisher = gameObject.AddComponent<CharacterMotionPolisher>();
            _motionPolisher.Bind(this);
        }

        public void RefreshWeaponTrailsCache()
        {
            _weaponTrails = GetComponentsInChildren<WeaponBladeTrail3D>(true);
        }

        public void PulseWeaponTrails(float duration, Color customCol = default)
        {
            if (_weaponTrails == null || _weaponTrails.Length == 0)
                _weaponTrails = GetComponentsInChildren<WeaponBladeTrail3D>(true);
            if (_weaponTrails == null) return;
            for (int i = 0; i < _weaponTrails.Length; i++)
                if (_weaponTrails[i] != null) _weaponTrails[i].Activate(duration, customCol);
        }

        private void SetupCombatAttachments(float radius, float height)
        {
            ActorHurtbox.AttachTo(this, radius, height);
            _hitboxSweep = WeaponHitboxSweep3D.EnsureOn(this);
            StatusVisualAttachment.AttachTo(this);
            RefreshWeaponTrailsCache();
        }

        private void EnsureController(float radius, float height)
        {
            Controller = GetComponent<CharacterController>();
            if (Controller == null) Controller = gameObject.AddComponent<CharacterController>();
            Controller.radius = radius;
            Controller.height = height;
            Controller.center = new Vector3(0f, height * 0.5f, 0f);
            Controller.stepOffset = Mathf.Min(0.4f, height * 0.25f);
        }

        public void RecalculateStats(bool refillVitals = false)
        {
            if (HeroClass == null) return;
            float hpRatio = MaxHp > 0 ? Hp / MaxHp : 1f;
            float mpRatio = MaxMana > 0 ? Mana / MaxMana : 1f;

            float lvlMult = 1f + (Level - 1) * 0.11f;
            float hp = HeroClass.hp * lvlMult;
            float mp = HeroClass.mana * (1f + (Level - 1) * 0.06f);
            float atk = HeroClass.atk * lvlMult;
            float def = HeroClass.def + (Level - 1) * 3.5f;
            float aspd = HeroClass.atkSpeed;
            float crit = HeroClass.crit;
            float crd = 1.85f;
            float spd = HeroClass.moveSpeed;
            float ls = (HeroClass.id == 6) ? 0.04f : 0f; // Warlock passive
            float cdr = 0f;
            float healB = (HeroClass.id == 9) ? 0.15f : 0f; // Cleric passive

            // Local profile bonuses & equipment (AI party scales proportionally with player's level/gear)
            var prof = GameSession.I.Profile;
            if (IsLocalPlayer)
            {
                hp += prof.bonusHpPts * 45f;
                atk += prof.bonusAtkPts * 8f;
                def += prof.bonusDefPts * 5f;
                cdr += prof.bonusHastePts * 0.02f;
                crit += prof.bonusHastePts * 0.01f;

                foreach (var eq in prof.equipped)
                {
                    if (eq == null) continue;
                    if (!ClassSpecializationSystem.EvaluateCompatibility(HeroClass.id, eq, out float eqMult, out _))
                        continue;
                    hp += eq.hp * eqMult;
                    mp += eq.mana * eqMult;
                    atk += eq.atk * eqMult;
                    def += eq.def * eqMult;
                    aspd += eq.atkSpeed * eqMult;
                    crit += eq.crit * eqMult;
                    ls += eq.lifesteal * eqMult;
                    cdr += eq.cdr * eqMult;
                }
            }
            else
            {
                // AI teammates get automatic gear scaling matching current floor
                float gearScale = (prof.selectedFloor - 1) * 0.18f;
                hp *= (1f + gearScale);
                atk *= (1f + gearScale);
                def += (prof.selectedFloor - 1) * 6f;
            }

            // Phase 4: 30 Authentic Build Specialization Modifiers across all 10 Classes
            ClassSpecializationSystem.ApplyBuildStatModifiers(
                HeroClass.id, BuildIndex,
                ref hp, ref mp, ref atk, ref def,
                ref aspd, ref crit, ref crd, ref spd,
                ref ls, ref cdr, ref healB);

            if (InBearForm)
            {
                def *= 1.60f;
                atk *= 1.20f;
                IsRanged = false;
                AttackRange = 2.8f;
            }
            else
            {
                IsRanged = HeroClass.ranged;
                AttackRange = HeroClass.attackRange;
            }

            MaxHp = Mathf.Max(100f, Mathf.Round(hp));
            MaxMana = Mathf.Max(50f, Mathf.Round(mp));
            Atk = Mathf.Max(10f, atk);
            Def = Mathf.Max(0f, def);
            AtkSpeed = Mathf.Clamp(aspd, 0.5f, 3.0f);
            CritChance = Mathf.Clamp(crit, 0f, 0.72f);
            CritDamage = Mathf.Clamp(crd, 1.50f, 2.15f);
            MoveSpeed = spd;
            Lifesteal = Mathf.Clamp(ls, 0f, 0.30f);
            CooldownReduction = Mathf.Clamp(cdr, 0f, 0.45f);
            HealBonus = healB;
            _lifestealBudget = MaxHp * 0.08f;

            for (int i = 0; i < 4; i++)
            {
                int rank = ClassSpecializationSystem.GetSkillRank(this, i);
                float rankCdMult = ClassSpecializationSystem.GetSkillCooldownMultiplier(rank);
                SkillMaxCds[i] = HeroClass.skills[i].cooldown * rankCdMult * (1f - CooldownReduction);
            }

            if (refillVitals)
            {
                Hp = MaxHp;
                Mana = MaxMana;
            }
            else
            {
                Hp = Mathf.Clamp(MaxHp * hpRatio, 1f, MaxHp);
                Mana = Mathf.Clamp(MaxMana * mpRatio, 0f, MaxMana);
            }

            if (IsLocalPlayer)
            {
                EnemyVisualArmorKit.RefreshHeroLegendaryEquipmentVisuals(this);
            }
        }

        private void BuildOverheadBar(Color fillColor, float yOffset)
        {
            var root = new GameObject("OverheadBar");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, yOffset, 0f);
            _hpBarRoot = root.transform;

            float barW = (IsElite || IsMiniBoss) ? 1.62f : 1.35f;
            float fillW = barW - 0.05f;
            float halfFill = fillW * 0.5f;

            var bgMat = CharacterVisualFactory.MakeTransparentMat(new Color(0.05f, 0.05f, 0.08f, 0.88f));
            CharacterVisualFactory.Prim(PrimitiveType.Cube, "Bg", root.transform, Vector3.zero, new Vector3(barW, 0.14f, 0.02f), bgMat);

            var fMat = CharacterVisualFactory.MakeTransparentMat(fillColor, fillColor * 0.6f);
            var fillHolder = new GameObject("FillPivot");
            fillHolder.transform.SetParent(root.transform, false);
            fillHolder.transform.localPosition = new Vector3(-halfFill, 0f, -0.01f);
            CharacterVisualFactory.Prim(PrimitiveType.Cube, "Fill", fillHolder.transform, new Vector3(halfFill, 0f, 0f), new Vector3(fillW, 0.105f, 0.025f), fMat);
            _hpBarFill = fillHolder.transform;

            // Monster Stagger Bar (Poise / Guard-Break Gauge) + Shield Overlay Bar
            if (Team == Team.Monster)
            {
                // Arcane/Barrier Shield Overlay on top of HP bar
                var shMat = CharacterVisualFactory.MakeTransparentMat(
                    new Color(0.32f, 0.88f, 1.0f, 0.88f), new Color(0.28f, 0.82f, 1.0f) * 1.4f);
                var shHolder = new GameObject("ShieldPivot");
                shHolder.transform.SetParent(root.transform, false);
                shHolder.transform.localPosition = new Vector3(-halfFill, 0.085f, -0.012f);
                CharacterVisualFactory.Prim(PrimitiveType.Cube, "ShieldFill", shHolder.transform,
                    new Vector3(halfFill, 0f, 0f), new Vector3(fillW, 0.055f, 0.025f), shMat);
                _shieldBarFill = shHolder.transform;
                _shieldBarFill.localScale = new Vector3(0f, 1f, 1f);

                // Clear Amber/Gold Stagger Bar directly below HP bar
                float stagY = -0.105f;
                CharacterVisualFactory.Prim(PrimitiveType.Cube, "StaggerBg", root.transform,
                    new Vector3(0f, stagY, 0f), new Vector3(barW, 0.075f, 0.02f), bgMat);

                Color stagCol = new Color(1.0f, 0.84f, 0.20f, 0.96f);
                var stagMat = CharacterVisualFactory.MakeTransparentMat(stagCol, stagCol * 1.25f);
                var stagHolder = new GameObject("StaggerPivot");
                stagHolder.transform.SetParent(root.transform, false);
                stagHolder.transform.localPosition = new Vector3(-halfFill, stagY, -0.01f);
                CharacterVisualFactory.Prim(PrimitiveType.Cube, "StaggerFill", stagHolder.transform,
                    new Vector3(halfFill, 0f, 0f), new Vector3(fillW, 0.055f, 0.025f), stagMat);
                _staggerBarFill = stagHolder.transform;
            }

            var lblObj = new GameObject("Name");
            lblObj.transform.SetParent(root.transform, false);
            lblObj.transform.localPosition = new Vector3(0f, 0.27f, 0f);
            _nameLabel = lblObj.AddComponent<TextMesh>();
            _nameLabel.text = DisplayName;
            _nameLabel.fontSize = 36;
            _nameLabel.characterSize = 0.055f;
            _nameLabel.anchor = TextAnchor.MiddleCenter;
            _nameLabel.fontStyle = FontStyle.Bold;
            _nameLabel.color = Team == Team.Hero
                ? new Color(0.75f, 1f, 0.85f)
                : (IsMiniBoss ? new Color(1f, 0.72f, 0.28f)
                : (IsElite ? new Color(1f, 0.90f, 0.38f) : new Color(1f, 0.85f, 0.8f)));
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (BasicAttackCd > 0f) BasicAttackCd -= dt;
            if (HeavyAttackCd > 0f) HeavyAttackCd -= dt;
            if (DodgeCd > 0f) DodgeCd -= dt;
            if (_jumpCooldown > 0f) _jumpCooldown -= dt;
            if (PoiseImmunityTimer > 0f) PoiseImmunityTimer = Mathf.Max(0f, PoiseImmunityTimer - dt);

            // Refill rolling 1-second Lifesteal cap budget (max 8% MaxHP per second)
            float maxLsPerSec = MaxHp * 0.08f;
            _lifestealBudget = Mathf.Min(maxLsPerSec, _lifestealBudget + maxLsPerSec * dt);

            if (_comboWindowTimer > 0f)
            {
                _comboWindowTimer -= dt;
                if (_comboWindowTimer <= 0f) ComboStep = 0;
            }
            if (IsStaggered)
            {
                _staggerTimer -= dt;
                if (_staggerTimer <= 0f)
                {
                    IsStaggered = false;
                    Poise = MaxPoise;
                    // Anti-stunlock Hyper-Armor / Unstoppable window (8-10s for Boss/MiniBoss)
                    PoiseImmunityTimer = IsBoss ? 9.5f : (IsMiniBoss ? 8.5f : (IsElite ? 6.0f : 3.5f));
                    if (IsBoss || IsMiniBoss)
                    {
                        CharacterVisualFactory.SpawnFloatingText(transform.position + Vector3.up * 2.5f,
                            $"BẤT KHUẤT ({PoiseImmunityTimer:0}s)!", new Color(0.75f, 0.9f, 1f), 1.05f);
                    }
                }
            }
            else if (Poise < MaxPoise)
            {
                if (_poiseRegenDelay > 0f) _poiseRegenDelay -= dt;
                else Poise = Mathf.Min(MaxPoise, Poise + MaxPoise * 0.22f * dt);
            }
            for (int i = 0; i < 4; i++) if (SkillCds[i] > 0f) SkillCds[i] = Mathf.Max(0f, SkillCds[i] - dt);

            if (IsSummon && !IsDead)
            {
                _summonLifeTimer -= dt;
                if (_summonLifeTimer <= 0f) { ForceKill(); return; }
            }

            if (InBearForm)
            {
                _bearFormTimer -= dt;
                if (_bearFormTimer <= 0f) SetBearForm(false);
            }

            // Passive mana, stamina & Druid out-of-combat regen
            if (!IsDead && !IsDowned && Team == Team.Hero)
            {
                Mana = Mathf.Min(MaxMana, Mana + (8f + MaxMana * 0.025f) * dt);
                if (HeroClass != null && HeroClass.id == 8)
                    Hp = Mathf.Min(MaxHp, Hp + MaxHp * 0.008f * dt);

                if (_staminaRegenDelay > 0f)
                {
                    _staminaRegenDelay = Mathf.Max(0f, _staminaRegenDelay - dt);
                }
                else if (!IsSprinting)
                {
                    Stamina = Mathf.Min(MaxStamina, Stamina + 30f * dt);
                }
            }

            // Co-op revive proximity check when downed
            if (IsDowned && !IsDead)
            {
                bool helperNear = false;
                foreach (var u in AllUnits)
                {
                    if (u != this && u.Team == Team.Hero && !u.IsDead && !u.IsDowned && !u.IsSummon)
                    {
                        if (Vector3.Distance(transform.position, u.transform.position) <= 2.8f)
                        {
                            helperNear = true;
                            break;
                        }
                    }
                }
                if (helperNear)
                {
                    ReviveProgress += dt / 2.5f;
                    if (ReviveProgress >= 1f) Revive(0.5f);
                }
                else ReviveProgress = Mathf.Max(0f, ReviveProgress - dt * 0.3f);
            }

            UpdateStatuses(dt);
            UpdateLocomotionAndGravity(dt);
            UpdateGravityAndVisuals(dt);
        }

        private void UpdateStatuses(float dt)
        {
            for (int i = Statuses.Count - 1; i >= 0; i--)
            {
                var st = Statuses[i];
                st.remaining -= dt;
                st.tickTimer -= dt;
                if (st.tickTimer <= 0f)
                {
                    st.tickTimer = 1f;
                    if (st.type == StatusType.Burn || st.type == StatusType.Poison)
                    {
                        float dot = Mathf.Max(5f, st.power);
                        ReceiveTrueDot(dot, st.source, st.type == StatusType.Burn ? new Color(1f, 0.5f, 0.1f) : new Color(0.4f, 0.95f, 0.3f));
                    }
                    else if (st.type == StatusType.Regen && !IsDead && !IsDowned)
                    {
                        ReceiveHeal(Mathf.Max(12f, MaxHp * 0.04f), st.source);
                    }
                }
                if (st.remaining <= 0f)
                {
                    if (st.type == StatusType.Taunted) TauntTarget = null;
                    Statuses.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Single unified Update()-driven CharacterController.Move loop using Time.deltaTime.
        /// Eliminates FixedUpdate/Update beat stutter and keeps OrbitCamera3D.LateUpdate silky smooth.
        /// </summary>
        private void UpdateLocomotionAndGravity(float dt)
        {
            if (dt <= 0.0001f) return;

            if (Controller != null && Controller.enabled && !IsDead)
            {
                bool groundedNow = (Controller.isGrounded || transform.position.y <= 0.04f) && _velocity.y <= 0.05f;
                if (groundedNow)
                {
                    _coyoteTimer = 0.12f;
                    _velocity.y = -2.2f; // Steady downward stick so CharacterController.isGrounded never flickers
                }
                else
                {
                    _coyoteTimer = Mathf.Max(0f, _coyoteTimer - dt);
                    _velocity.y -= 20f * dt;
                }

                Vector3 planarImpulse = _knockbackVel + _lungeVel;
                if (planarImpulse.sqrMagnitude > 0.002f)
                {
                    _knockbackVel = Vector3.Lerp(_knockbackVel, Vector3.zero, 1f - Mathf.Exp(-11f * dt));
                    _lungeVel = Vector3.Lerp(_lungeVel, Vector3.zero, 1f - Mathf.Exp(-15f * dt));
                }
                else
                {
                    planarImpulse = Vector3.zero;
                    _knockbackVel = Vector3.zero;
                    _lungeVel = Vector3.zero;
                }

                Vector3 totalPlanarVel = _queuedPlanarVel + new Vector3(planarImpulse.x, 0f, planarImpulse.z);
                _queuedPlanarVel = Vector3.zero;

                Vector3 stepDisp = new Vector3(totalPlanarVel.x * dt, _velocity.y * dt, totalPlanarVel.z * dt);
                if (transform.position.y + stepDisp.y < 0f && _velocity.y < 0f)
                {
                    stepDisp.y = -transform.position.y;
                    _velocity.y = -2.2f;
                }

                if (stepDisp.sqrMagnitude > 0.0000001f)
                {
                    Controller.Move(stepDisp);
                }
            }
            else
            {
                _queuedPlanarVel = Vector3.zero;
            }
        }

        private void UpdateGravityAndVisuals(float dt)
        {
            if (_swingAnimTimer > 0f)
            {
                _swingAnimTimer -= dt;
                if (Anim == null && MainVisual != null)
                {
                    float s = Mathf.Sin(_swingAnimTimer * 24f) * 14f;
                    MainVisual.transform.localRotation = Quaternion.Euler(s, 0f, 0f);
                }
            }
            else if (Anim == null && MainVisual != null)
            {
                MainVisual.transform.localRotation = Quaternion.identity;
            }

            if (_hpBarRoot != null)
            {
                Camera cam = Camera.main;
                if (cam != null) _hpBarRoot.rotation = cam.transform.rotation;
                if (_hpBarFill != null)
                    _hpBarFill.localScale = new Vector3(Mathf.Clamp01(Hp / Mathf.Max(1f, MaxHp)), 1f, 1f);
                if (_staggerBarFill != null)
                {
                    float stagRatio = IsStaggered ? 0f : Mathf.Clamp01(Poise / Mathf.Max(1f, MaxPoise));
                    _staggerBarFill.localScale = new Vector3(stagRatio, 1f, 1f);
                }
                if (_shieldBarFill != null)
                {
                    float shRatio = Mathf.Clamp01(Shield / Mathf.Max(1f, MaxHp * 0.5f));
                    _shieldBarFill.localScale = new Vector3(shRatio, 1f, 1f);
                }
                if (_nameLabel != null)
                {
                    string desiredLabel = IsDowned
                        ? $"{DisplayName} [GỤC - Đứng gần cứu {(int)(ReviveProgress * 100)}%]"
                        : (IsStaggered ? $"{DisplayName} [PHÁ GIÁP!]" : (IsPoiseImmune && (IsElite || IsMiniBoss) ? $"{DisplayName} [BẤT KHUẤT]" : DisplayName));
                    if (!string.Equals(_lastOverheadLabel, desiredLabel, StringComparison.Ordinal))
                    {
                        _lastOverheadLabel = desiredLabel;
                        _nameLabel.text = desiredLabel;
                    }
                }
            }
        }

        public bool IsCrowdControlled()
        {
            return IsStaggered || HasStatus(StatusType.Stun) || HasStatus(StatusType.Freeze);
        }

        public void ApplyKnockback(Vector3 worldDir, float impulseSpeed)
        {
            if (IsDead || impulseSpeed <= 0.05f) return;
            worldDir.y = 0f;
            if (worldDir.sqrMagnitude < 0.001f) worldDir = -transform.forward;
            worldDir.Normalize();

            float resist = IsBoss ? 0.08f : (IsMiniBoss ? 0.28f : (IsElite ? 0.52f : (Team == Team.Hero ? 0.45f : 1.0f)));
            if (InBearForm) resist *= 0.4f;
            _knockbackVel += worldDir * (impulseSpeed * resist);
            _knockbackVel = Vector3.ClampMagnitude(_knockbackVel, 14f);
        }

        public bool ApplyPoiseDamage(float poiseDmg, ActorUnit attacker)
        {
            if (IsDead || IsDowned || IsStaggered || IsPoiseImmune || poiseDmg <= 0f || MaxPoise <= 0f) return false;
            // Heroes only stagger from massive Boss/MiniBoss blows; Monsters have full Poise/Guard break!
            if (Team == Team.Hero) return false;

            // AI teammates deal reduced Poise pressure so the player controls stagger timing
            if (attacker != null && attacker.Team == Team.Hero && !attacker.IsLocalPlayer)
                poiseDmg *= 0.50f;

            _poiseRegenDelay = 3.2f;
            Poise = Mathf.Max(0f, Poise - poiseDmg);
            if (Poise > 0.01f) return false;

            IsStaggered = true;
            _staggerTimer = IsBoss ? 2.4f : 2.5f;
            if (_hitboxSweep != null) _hitboxSweep.CancelAllSweeps();

            // Shatter Arcane Shielded barrier when guard-broken!
            if (ActiveEliteAffix == EliteAffixKind.ArcaneShielded && Shield > 0f)
            {
                Shield = 0f;
                CharacterVisualFactory.SpawnFloatingText(transform.position + Vector3.up * 2.65f,
                    "VỠ KHIÊN KHÁNG PHÉP!", new Color(0.42f, 0.95f, 1f), 1.05f);
            }

            GameAudio.I.PlaySfx(GameSfx.GuardBreak, 0.9f);
            AbyssalDungeon.VFX.VFXPoolManager.Instance.Spawn("VFX_Knight_Judgement", transform.position, Quaternion.identity, null, IsBoss ? 1.05f : 0.78f);
            CharacterVisualFactory.SpawnFloatingText(transform.position + Vector3.up * 2.35f, "PHÁ GIÁP! (140% SÁT THƯƠNG • 2.5s)", new Color(1f, 0.85f, 0.22f), 1.25f);

            if (attacker != null && attacker.IsLocalPlayer)
            {
                CombatFeelManager.I.TriggerHitStop(0.03f, 0.08f);
                if (OrbitCamera3D.Instance != null) OrbitCamera3D.Instance.Shake(0.18f, 0.18f);
                if (ScreenCombatFeedbackUI.Instance != null)
                    ScreenCombatFeedbackUI.Instance.NotifyCombatSurge(new Color(1f, 0.84f, 0.28f), 0.34f);
            }
            return true;
        }

        public bool CanAct()
        {
            return !IsDead && !IsDowned && !IsCrowdControlled();
        }

        public bool HasStatus(StatusType t)
        {
            for (int i = 0; i < Statuses.Count; i++) if (Statuses[i].type == t) return true;
            return false;
        }

        public void ApplyStatus(StatusType type, float duration, float power = 0f, ActorUnit source = null)
        {
            if (type == StatusType.None || IsDead) return;
            if (HeroClass != null && source != null && source.HeroClass != null && source.HeroClass.id == 5 && source.BuildIndex == 1)
            {
                // Mage Frost build: +50% freeze/slow duration
                if (type == StatusType.Freeze || type == StatusType.Slow) duration *= 1.5f;
            }
            if (IsBoss && (type == StatusType.Stun || type == StatusType.Freeze)) duration *= 0.4f;

            var existing = Statuses.Find(s => s.type == type);
            if (existing != null)
            {
                existing.remaining = Mathf.Max(existing.remaining, duration);
                existing.power = Mathf.Max(existing.power, power);
                existing.source = source;
            }
            else
            {
                Statuses.Add(new ActiveStatus { type = type, remaining = duration, power = power, tickTimer = 0.9f, source = source });
                string label = GameDB.StatusName(type);
                if (!string.IsNullOrEmpty(label))
                    CharacterVisualFactory.SpawnFloatingText(transform.position + Vector3.up * 2.1f, label, GameDB.IsDebuff(type) ? new Color(1f, 0.65f, 0.25f) : new Color(0.45f, 1f, 0.7f), 0.75f);
            }
            if (type == StatusType.Taunted && source != null) TauntTarget = source;
        }

        public void CleanseDebuffs()
        {
            Statuses.RemoveAll(s => GameDB.IsDebuff(s.type));
            TauntTarget = null;
        }

        public float EffectiveMoveSpeed()
        {
            float m = MoveSpeed;
            if (HasStatus(StatusType.Slow)) m *= 0.58f;
            if (HasStatus(StatusType.SpeedUp) || HasStatus(StatusType.Rage)) m *= 1.25f;
            return m;
        }

        public float EffectiveAttack()
        {
            float a = Atk;
            if (HeroClass != null && HeroClass.id == 2)
            {
                // Berserker passive: up to +40% damage as HP drops (rebalanced from 60% to prevent runaway spikes)
                float missing = 1f - Mathf.Clamp01(Hp / Mathf.Max(1f, MaxHp));
                a *= (1f + missing * 0.40f);
            }
            if (HasStatus(StatusType.AtkUp)) a *= 1.20f;
            if (HasStatus(StatusType.Rage)) a *= 1.28f;
            return a;
        }

        public float EffectiveDefense()
        {
            float d = Def;
            if (HasStatus(StatusType.DefUp)) d *= 1.32f;
            return d;
        }

        public bool ConsumeStamina(float amount)
        {
            if (Stamina < amount) return false;
            Stamina = Mathf.Max(0f, Stamina - amount);
            _staminaRegenDelay = 0.55f;
            return true;
        }

        public void DrainStaminaContinuous(float amount)
        {
            Stamina = Mathf.Max(0f, Stamina - amount);
            _staminaRegenDelay = 0.45f;
        }

        public void SetPlanarMoveVelocity(Vector3 worldVelocity)
        {
            if (!CanAct()) return;
            worldVelocity.y = 0f;
            _queuedPlanarVel = worldVelocity;
            if (_motionPolisher == null && Anim != null)
            {
                float spdRatio = worldVelocity.magnitude / Mathf.Max(0.01f, EffectiveMoveSpeed());
                Anim.SetFloat(HashSpeed, Mathf.Clamp01(spdRatio), 0.1f, Time.deltaTime);
            }
        }

        public void SetFixedMoveVelocity(Vector3 worldVelocity)
        {
            SetPlanarMoveVelocity(worldVelocity);
        }

        public void MoveWorld(Vector3 worldDelta)
        {
            if (!CanAct()) return;
            worldDelta.y = 0f;
            float dt = Mathf.Clamp(Time.deltaTime, 0.0001f, 0.05f);
            Vector3 vel = worldDelta / dt;
            _queuedPlanarVel = vel;
            if (_motionPolisher == null && Anim != null)
            {
                Anim.SetFloat(HashSpeed, Mathf.Clamp01(vel.magnitude / Mathf.Max(0.01f, EffectiveMoveSpeed())), 0.1f, dt);
            }
        }

        public void StopMovingAnim()
        {
            IsSprinting = false;
            if (_motionPolisher == null && Anim != null) Anim.SetFloat(HashSpeed, 0f, 0.12f, Time.deltaTime);
        }

        public void FaceDirection(Vector3 dir, float turnSpeed = 14f)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-turnSpeed * dt));
        }

        public bool TryJump()
        {
            if (!CanAct() || IsDodging || _jumpCooldown > 0f) return false;
            if (!IsGroundedUnit && _coyoteTimer <= 0f) return false;
            if (IsLocalPlayer && Stamina < 10f) return false;

            if (IsLocalPlayer) ConsumeStamina(10f);
            _jumpCooldown = 0.50f;
            _coyoteTimer = 0f;
            _velocity.y = 7.4f;

            GameAudio.I.PlaySfx(GameSfx.Jump, 0.65f);
            if (_motionPolisher != null) _motionPolisher.NotifyJumpTakeoff();
            return true;
        }

        /// <summary>
        /// Responsive Dodge Cancel with explicit 0.35s i-frames:
        /// Immediately cancels active attack swings/windups/lunges so the player can react cleanly to Boss red AoEs.
        /// </summary>
        public bool TryDodge(Vector3 moveDir)
        {
            if (!CanAct() || IsDodging || DodgeCd > 0f) return false;
            if (IsLocalPlayer && !ConsumeStamina(16f)) return false;

            // Dodge Cancel: abort ongoing attack sweeps, attack coroutines, and forward lunges immediately!
            _attackCancelVersion++;
            if (_hitboxSweep != null) _hitboxSweep.CancelAllSweeps();
            _lungeVel = Vector3.zero;
            _swingAnimTimer = 0f;
            BasicAttackCd = Mathf.Min(BasicAttackCd, 0.10f);
            HeavyAttackCd = Mathf.Min(HeavyAttackCd, 0.25f);

            IsSprinting = false;
            DodgeCd = IsLocalPlayer ? 0.95f : 2.6f;
            Vector3 dir = moveDir.sqrMagnitude > 0.02f ? moveDir.normalized : transform.forward;
            dir.y = 0f;
            transform.rotation = Quaternion.LookRotation(dir);
            if (_motionPolisher != null) _motionPolisher.TriggerAction("Dodge");
            else if (Anim != null) Anim.SetTrigger(HashDodge);
            GameAudio.I.PlaySfx(GameSfx.Dodge, 0.7f);
            if (IsLocalPlayer && LanRoomBridge.HasInstance)
                LanRoomBridge.I.BroadcastHeroAction(PartySlotIndex, "dodge", dir, transform.position + dir * 4f);
            if (IsLocalPlayer && OrbitCamera3D.Instance != null)
                OrbitCamera3D.Instance.PunchFov(2.4f, 0.20f);

            const float dodgeIFrameDuration = 0.35f;
            StartCoroutine(DodgeCoroutine(dir, 6.4f, dodgeIFrameDuration));
            return true;
        }

        private IEnumerator DodgeCoroutine(Vector3 dir, float dist, float dur)
        {
            IsDodging = true;
            IsInvincible = true;
            AbyssalDungeon.VFX.VFXPoolManager.Instance.Spawn("VFX_Common_SpiralDash", transform.position, transform.rotation, null, 0.85f);
            float t = 0f;
            float spd = dist / dur;
            int ghostsSpawned = 0;
            while (t < dur && !IsDead && !IsDowned)
            {
                if (_motionPolisher != null && ghostsSpawned < 3 && t >= ghostsSpawned * 0.085f)
                {
                    _motionPolisher.SpawnDodgeGhost();
                    ghostsSpawned++;
                }
                // Slight ease-out curve on dodge velocity for crisp start and clean stop
                float ease = Mathf.Lerp(1.22f, 0.72f, Mathf.Clamp01(t / dur));
                _queuedPlanarVel = dir * (spd * ease);
                t += Mathf.Min(Time.deltaTime, 0.05f);
                yield return null;
            }
            IsInvincible = false;
            IsDodging = false;
        }

        public bool TryBasicAttack(Vector3 aimDir, Vector3 aimPoint)
        {
            if (!CanAct() || IsDodging || BasicAttackCd > 0f) return false;
            IsSprinting = false;

            int step = (_comboWindowTimer > 0f) ? (ComboStep % 3) : 0;
            bool isFinisher = (step == 2) && (Team == Team.Hero);
            ComboStep = (step + 1) % 3;
            _comboWindowTimer = 1.45f;

            float effAspd = AtkSpeed;
            if (HeroClass != null && HeroClass.id == 2 && BuildIndex == 0)
            {
                // Berserker Blood Frenzy: up to +35% bonus attack speed below 50% HP
                float lowRatio = Mathf.Clamp01((0.50f - Hp / Mathf.Max(1f, MaxHp)) * 2f);
                effAspd *= (1f + lowRatio * 0.35f);
            }
            float baseInterval = 1f / Mathf.Max(0.35f, effAspd);
            BasicAttackCd = isFinisher ? baseInterval * 1.16f : baseInterval * (step == 1 ? 0.92f : 0.96f);

            aimDir.y = 0f;
            if (aimDir.sqrMagnitude < 0.001f) aimDir = transform.forward;
            aimDir.Normalize();
            transform.rotation = Quaternion.LookRotation(aimDir);

            if (_motionPolisher != null) _motionPolisher.TriggerAction("Attack");
            else if (Anim != null) Anim.SetTrigger(HashAttack);
            _swingAnimTimer = isFinisher ? 0.34f : 0.24f;
            if (IsLocalPlayer && LanRoomBridge.HasInstance)
                LanRoomBridge.I.BroadcastHeroAction(PartySlotIndex, "basic", aimDir, aimPoint);

            // Monk passive: +4 mana per basic combo strike
            if (HeroClass != null && HeroClass.id == 7) Mana = Mathf.Min(MaxMana, Mana + 4f);

            float[] comboDmgMults = { 1.00f, 1.18f, 1.52f };
            float dmgMult = Team == Team.Hero ? comboDmgMults[step] : 1.0f;
            float rawAtk = EffectiveAttack() * dmgMult;
            float poiseDmg = Team == Team.Hero ? (step == 0 ? 18f : (step == 1 ? 24f : 38f)) : 12f;
            float kbForce = isFinisher ? 5.6f : (step == 1 ? 2.2f : 1.4f);
            float bonusCrit = isFinisher ? 0.12f : 0f;

            Color accent = HeroClass != null
                ? ClassSpecializationSystem.GetBuildAccentColor(HeroClass.id, BuildIndex, HeroClass.color)
                : (MonsterDef != null ? MonsterDef.projColor : new Color(1f, 0.42f, 0.3f));

            if (IsRanged)
            {
                if (HeroClass != null)
                {
                    GameAudio.I.PlayWeaponAttack(HeroClass.id, step, isHeavy: false, isFinisher: isFinisher, IsLocalPlayer ? (isFinisher ? 0.84f : 0.70f) : 0.20f);
                }
                else
                {
                    float pitch = 0.96f + step * 0.07f;
                    GameAudio.I.PlaySfxVaried(isFinisher ? GameSfx.ComboFinisher : GameSfx.Magic, 0.45f, 0.04f, pitch);
                }
                PulseWeaponTrails(0.20f, accent);

                if (isFinisher)
                {
                    // Combo Stage 3 Ranged Finisher: 3-way empowered fan / piercing detonation!
                    for (int i = -1; i <= 1; i++)
                    {
                        Vector3 shotDir = Quaternion.Euler(0f, i * 11f, 0f) * aimDir;
                        SkillProjectile.Spawn(this, transform.position + Vector3.up * 1.25f + shotDir * 0.65f, shotDir,
                            24f, AttackRange + 4.5f, rawAtk * (i == 0 ? 1.0f : 0.58f), i == 0, StatusType.None, 0f, 0f,
                            i == 0 ? 1.6f : 0f, accent, poiseDmg * 0.6f, kbForce * 0.7f, bonusCrit);
                    }
                }
                else
                {
                    SkillProjectile.Spawn(this, transform.position + Vector3.up * 1.25f + aimDir * 0.6f, aimDir,
                        21f + step * 2f, AttackRange + 4f, rawAtk, false, StatusType.None, 0f, 0f, 0f, accent, poiseDmg, kbForce, 0f);
                }
            }
            else
            {
                // Responsive forward micro-lunge on Hero melee combos
                if (Team == Team.Hero)
                {
                    float lungeSpeed = step == 0 ? 4.2f : (step == 1 ? 5.4f : 7.2f);
                    _lungeVel = aimDir * lungeSpeed;
                }

                if (HeroClass != null)
                {
                    GameAudio.I.PlayWeaponAttack(HeroClass.id, step, isHeavy: false, isFinisher: isFinisher, IsLocalPlayer ? (isFinisher ? 0.88f : 0.74f) : 0.22f);
                }
                else
                {
                    float pitch = 0.94f + step * 0.08f;
                    GameAudio.I.PlaySfxVaried(isFinisher ? GameSfx.ComboFinisher : GameSfx.Attack, 0.48f, 0.05f, pitch);
                }
                PulseWeaponTrails(isFinisher ? 0.32f : 0.22f, accent);

                float reach = AttackRange + (isFinisher ? 1.05f : 0.65f);
                float arc = isFinisher ? 170f : 132f;
                float tilt = step == 0 ? -20f : (step == 1 ? 24f : 0f);
                bool reverseSlash = (step == 1);

                var sweep = _hitboxSweep != null ? _hitboxSweep : WeaponHitboxSweep3D.EnsureOn(this);
                sweep.BeginSweep(
                    windupDelay: 0.06f,
                    activeDuration: isFinisher ? 0.16f : 0.12f,
                    reachRadius: reach,
                    arcDegrees: arc,
                    onWindupComplete: () =>
                    {
                        if (this == null || IsDead) return;
                        Color core = isFinisher ? Color.Lerp(accent, new Color(1f, 0.95f, 0.6f), 0.55f) : Color.Lerp(accent, Color.white, 0.45f);
                        CombatFeelManager.I.SpawnSlashArc(
                            transform.position + Vector3.up * 1.08f + transform.forward * 0.25f,
                            transform.rotation,
                            Mathf.Clamp(reach * 0.78f, 1.35f, 3.4f),
                            arc,
                            tilt,
                            core,
                            accent,
                            isFinisher ? 0.26f : 0.19f,
                            isFinisher ? 0.88f : 0.62f,
                            reverseSlash);
                    },
                    onHitVictim: (victim, contactPt) =>
                    {
                        DealDamageTo(victim, rawAtk, StatusType.None, 0f, 0f, bonusCrit, poiseDmg, kbForce, contactPt, isFinisher);
                    });
            }
            return true;
        }

        /// <summary>
        /// Charged Heavy Attack [RMB / X] (Phase 3 - Section 3.2):
        /// High-impact guard-breaking blow with Stamina cost, overhead/piercing VFX, heavy knockback, and Hit-Stop.
        /// </summary>
        public bool TryHeavyAttack(Vector3 aimDir, Vector3 aimPoint)
        {
            if (!CanAct() || IsDodging || HeavyAttackCd > 0f || BasicAttackCd > 0.25f) return false;
            if (IsLocalPlayer && !ConsumeStamina(20f)) return false;

            IsSprinting = false;
            ComboStep = 0;
            _comboWindowTimer = 0f;

            float cdBase = 1.62f / Mathf.Max(0.45f, AtkSpeed);
            HeavyAttackCd = cdBase;
            BasicAttackCd = Mathf.Max(BasicAttackCd, 0.52f);

            aimDir.y = 0f;
            if (aimDir.sqrMagnitude < 0.001f) aimDir = transform.forward;
            aimDir.Normalize();
            transform.rotation = Quaternion.LookRotation(aimDir);

            if (_motionPolisher != null) _motionPolisher.TriggerAction("Attack");
            else if (Anim != null) Anim.SetTrigger(HashAttack);
            _swingAnimTimer = 0.42f;
            if (IsLocalPlayer && LanRoomBridge.HasInstance)
                LanRoomBridge.I.BroadcastHeroAction(PartySlotIndex, "heavy", aimDir, aimPoint);

            Color accent = HeroClass != null ? HeroClass.color : new Color(1f, 0.55f, 0.2f);
            Color goldCore = Color.Lerp(accent, new Color(1f, 0.92f, 0.48f), 0.65f);
            float rawHeavy = EffectiveAttack() * 2.15f;
            const float heavyPoise = 58f;
            const float heavyKb = 8.4f;

            if (HeroClass != null)
                GameAudio.I.PlayWeaponAttack(HeroClass.id, 2, isHeavy: true, isFinisher: true, IsLocalPlayer ? 0.92f : 0.26f);
            else
                GameAudio.I.PlaySfxVaried(GameSfx.HeavyAttack, 0.88f, 0.04f, 0.96f);
            PulseWeaponTrails(0.42f, goldCore);
            if (IsLocalPlayer && OrbitCamera3D.Instance != null)
                OrbitCamera3D.Instance.PunchFov(-2.4f, 0.26f);

            if (IsRanged)
            {
                StartCoroutine(DelayedHeavyRangedShot(0.14f, aimDir, rawHeavy, accent, heavyPoise, heavyKb));
            }
            else
            {
                _lungeVel = aimDir * 8.6f;
                float reach = AttackRange + 1.35f;
                var sweep = _hitboxSweep != null ? _hitboxSweep : WeaponHitboxSweep3D.EnsureOn(this);
                sweep.BeginSweep(
                    windupDelay: 0.15f,
                    activeDuration: 0.17f,
                    reachRadius: reach,
                    arcDegrees: 150f,
                    onWindupComplete: () =>
                    {
                        if (this == null || IsDead) return;
                        // Vertical overhead smash arc + ground shockwave!
                        CombatFeelManager.I.SpawnSlashArc(
                            transform.position + Vector3.up * 1.25f + transform.forward * 0.4f,
                            transform.rotation,
                            Mathf.Clamp(reach * 0.85f, 1.8f, 3.8f),
                            155f,
                            78f,
                            goldCore,
                            accent,
                            0.30f,
                            0.98f,
                            false);
                        CharacterVisualFactory.SpawnBurst(transform.position + transform.forward * (reach * 0.55f) + Vector3.up * 0.15f, accent, 1.45f, 0.30f);
                        if (IsLocalPlayer && OrbitCamera3D.Instance != null)
                            OrbitCamera3D.Instance.Shake(0.18f, 0.18f);
                    },
                    onHitVictim: (victim, contactPt) =>
                    {
                        DealDamageTo(victim, rawHeavy, StatusType.None, 0f, 0f, 0.15f, heavyPoise, heavyKb, contactPt, true);
                    });
            }
            return true;
        }

        private IEnumerator DelayedHeavyRangedShot(float delay, Vector3 aimDir, float rawHeavy, Color accent, float poiseDmg, float kb)
        {
            int token = _attackCancelVersion;
            yield return new WaitForSeconds(delay);
            if (IsDead || IsDowned || token != _attackCancelVersion) yield break;
            CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up * 1.25f + aimDir * 0.7f, accent, 0.95f, 0.22f);
            SkillProjectile.Spawn(this, transform.position + Vector3.up * 1.25f + aimDir * 0.75f, aimDir,
                27f, AttackRange + 6f, rawHeavy * 0.95f, true, StatusType.Slow, 1.8f, 0f, 2.2f, accent, poiseDmg * 0.85f, kb * 0.75f, 0.15f);
        }

        public bool TryUseSkill(int slot, Vector3 aimPoint)
        {
            if (HeroClass == null || slot < 0 || slot >= 4 || !CanAct() || IsDodging) return false;
            SkillDef sk = HeroClass.skills[slot];
            if (SkillCds[slot] > 0f) return false;
            float cost = (HeroClass.id == 5) ? sk.mana * 0.9f : sk.mana; // Mage passive -10% mana
            if (Mana < cost) return false;

            IsSprinting = false;
            Mana -= cost;
            SkillCds[slot] = SkillMaxCds[slot];

            Vector3 dir = aimPoint - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            dir.Normalize();
            transform.rotation = Quaternion.LookRotation(dir);

            if (_motionPolisher != null) _motionPolisher.TriggerAction(sk.useCastAnim ? "Cast" : "Attack");
            else if (Anim != null) Anim.SetTrigger(sk.useCastAnim ? HashCast : HashAttack);
            _swingAnimTimer = 0.3f;
            if (IsLocalPlayer && LanRoomBridge.HasInstance)
                LanRoomBridge.I.BroadcastHeroSkill(PartySlotIndex, slot, aimPoint);

            if (sk.selfStatus != StatusType.None)
                ApplyStatus(sk.selfStatus, sk.selfStatusDur, 0f, this);

            int rank = ClassSpecializationSystem.GetSkillRank(this, slot);
            float rankPow = ClassSpecializationSystem.GetSkillPowerMultiplier(rank);
            float areaMult = ClassSpecializationSystem.GetSkillAreaMultiplier(rank);
            Color fxCol = ClassSpecializationSystem.GetBuildAccentColor(HeroClass.id, BuildIndex, sk.color);

            PulseWeaponTrails(0.36f, fxCol);
            GameAudio.I.PlayWeaponSkill(HeroClass.id, sk, slot, IsLocalPlayer ? 0.90f : 0.24f);
            if (IsLocalPlayer && sk.ultimate && ScreenCombatFeedbackUI.Instance != null)
                ScreenCombatFeedbackUI.Instance.NotifyCombatSurge(fxCol, 0.36f);

            float rawPow = EffectiveAttack() * sk.power * rankPow;
            float effRadius = sk.radius * areaMult;
            float effRange = sk.range * areaMult;

            switch (sk.kind)
            {
                case SkillKind.MeleeArc:
                    GameAudio.I.PlaySfx(GameSfx.Attack, 0.85f);
                    AbyssalDungeon.VFX.VFXPoolManager.Instance.SpawnForClassSkill(
                        HeroClass.id, slot, transform.position + dir * 0.8f, transform.rotation, Mathf.Clamp(effRadius / 3.0f, 0.75f, 1.4f));
                    int hits = Mathf.Max(1, sk.count);
                    // Monk Windwalker Build: Flurry hits 5 times
                    if (HeroClass.id == 7 && BuildIndex == 0 && slot == 0) hits = 5;
                    StartCoroutine(MultiArcRoutine(hits, rawPow, sk, effRadius, fxCol));
                    break;

                case SkillKind.Projectile:
                    GameAudio.I.PlaySfx(GameSfx.Magic, 0.8f);
                    AbyssalDungeon.VFX.VFXPoolManager.Instance.SpawnForClassSkill(
                        HeroClass.id, slot, transform.position + dir * 0.5f, transform.rotation, 0.75f);
                    int cnt = Mathf.Max(1, sk.count);
                    bool pierce = sk.pierce || (HeroClass.id == 6 && BuildIndex == 2 && slot == 0);
                    for (int i = 0; i < cnt; i++)
                    {
                        float ang = cnt == 1 ? 0f : Mathf.Lerp(-22f, 22f, (float)i / (cnt - 1));
                        Vector3 shotDir = Quaternion.Euler(0f, ang, 0f) * dir;
                        SkillProjectile.Spawn(this, transform.position + Vector3.up * 1.25f + shotDir * 0.6f, shotDir,
                            22f, effRange, rawPow, pierce, sk.status, sk.statusDur, EffectiveAttack() * sk.statusPower * rankPow, effRadius > 1.5f && cnt == 1 ? effRadius : 0f, fxCol, 20f, 2.0f, 0f, slot);
                    }
                    break;

                case SkillKind.GroundAoE:
                    GameAudio.I.PlaySfx(GameSfx.Magic, 0.85f);
                    Vector3 clampedAim = ClampRange(transform.position, aimPoint, effRange);
                    int groundClassId = HeroClass.id;
                    int groundSlot = slot;
                    CharacterVisualFactory.SpawnGroundTelegraph(clampedAim, effRadius, sk.ultimate ? 0.65f : 0.35f, fxCol, () =>
                    {
                        if (this == null) return;
                        GameAudio.I.PlaySfx(GameSfx.Explosion, 0.85f);
                        AbyssalDungeon.VFX.VFXPoolManager.Instance.SpawnForClassSkill(
                            groundClassId, groundSlot, clampedAim, Quaternion.identity, Mathf.Clamp(effRadius / 4.2f, 0.85f, 1.55f));
                        DealCircleDamage(clampedAim, effRadius, rawPow, sk.status, sk.statusDur, EffectiveAttack() * sk.statusPower * rankPow, fxCol);
                    });
                    break;

                case SkillKind.SelfAoE:
                    GameAudio.I.PlaySfx(GameSfx.Explosion, 0.8f);
                    AbyssalDungeon.VFX.VFXPoolManager.Instance.SpawnForClassSkill(
                        HeroClass.id, slot, transform.position, transform.rotation, Mathf.Clamp(effRadius / 4.0f, 0.85f, 1.5f));
                    DealCircleDamage(transform.position, effRadius, rawPow, sk.status, sk.statusDur, EffectiveAttack() * sk.statusPower * rankPow, fxCol);
                    // Knight Radiant Paladin Judgement heals allies; Druid Nature's Wrath heals allies
                    if ((HeroClass.id == 0 && BuildIndex == 1 && sk.ultimate) || (HeroClass.id == 8 && sk.ultimate))
                        HealAlliesAround(transform.position, 14f, EffectiveAttack() * 1.8f * rankPow);
                    break;

                case SkillKind.Dash:
                    GameAudio.I.PlaySfx(GameSfx.Dodge, 0.9f);
                    AbyssalDungeon.VFX.VFXPoolManager.Instance.Spawn("VFX_Common_SpiralDash", transform.position, transform.rotation, transform, 1.0f);
                    StartCoroutine(DashAttackRoutine(dir, effRange, rawPow, effRadius, fxCol));
                    break;

                case SkillKind.HealAll:
                    GameAudio.I.PlaySfx(GameSfx.Heal, 0.85f);
                    HealAlliesAround(transform.position, effRadius, rawPow * (1f + HealBonus), sk.status, sk.statusDur);
                    AbyssalDungeon.VFX.VFXPoolManager.Instance.SpawnForClassSkill(
                        HeroClass.id, slot, transform.position, Quaternion.identity, 1.15f);
                    break;

                case SkillKind.HealTarget:
                    GameAudio.I.PlaySfx(GameSfx.Heal, 0.85f);
                    ActorUnit weakest = FindWeakestAlly(effRange);
                    if (weakest != null)
                    {
                        weakest.ReceiveHeal(rawPow * (1f + HealBonus), this);
                        if (sk.status != StatusType.None) weakest.ApplyStatus(sk.status, sk.statusDur, 0f, this);
                        AbyssalDungeon.VFX.VFXPoolManager.Instance.SpawnForClassSkill(
                            HeroClass.id, slot, weakest.transform.position, Quaternion.identity, 0.95f, -1f, weakest.transform);
                    }
                    break;

                case SkillKind.ShieldSelf:
                    GameAudio.I.PlaySfx(GameSfx.Shield, 0.8f);
                    AddShield(MaxHp * sk.power * rankPow * (1f + HealBonus));
                    AbyssalDungeon.VFX.VFXPoolManager.Instance.SpawnForClassSkill(
                        HeroClass.id, slot, transform.position, Quaternion.identity, 1.0f, -1f, transform);
                    break;

                case SkillKind.ShieldAll:
                    GameAudio.I.PlaySfx(GameSfx.Shield, 0.85f);
                    foreach (var u in AllUnits)
                        if (u.Team == Team && !u.IsDead && !u.IsDowned && Vector3.Distance(transform.position, u.transform.position) <= effRadius)
                            u.AddShield(u.MaxHp * sk.power * rankPow * (1f + HealBonus));
                    AbyssalDungeon.VFX.VFXPoolManager.Instance.SpawnForClassSkill(
                        HeroClass.id, slot, transform.position, Quaternion.identity, 1.25f, -1f, transform);
                    break;

                case SkillKind.BuffSelf:
                    GameAudio.I.PlaySfx(GameSfx.Shield, 0.8f);
                    ApplyStatus(sk.status, sk.statusDur, 0f, this);
                    AbyssalDungeon.VFX.VFXPoolManager.Instance.SpawnForClassSkill(
                        HeroClass.id, slot, transform.position, Quaternion.identity, 1.0f, -1f, transform);
                    break;

                case SkillKind.BuffAll:
                    GameAudio.I.PlaySfx(GameSfx.Shield, 0.85f);
                    foreach (var u in AllUnits)
                        if (u.Team == Team && !u.IsDead && !u.IsDowned && Vector3.Distance(transform.position, u.transform.position) <= effRadius)
                            u.ApplyStatus(sk.status, sk.statusDur, 0f, this);
                    AbyssalDungeon.VFX.VFXPoolManager.Instance.SpawnForClassSkill(
                        HeroClass.id, slot, transform.position, Quaternion.identity, 1.2f);
                    break;

                case SkillKind.Taunt:
                    GameAudio.I.PlaySfx(GameSfx.BossRoar, 0.7f);
                    if (sk.power > 0.05f) AddShield(MaxHp * sk.power * rankPow);
                    foreach (var u in AllUnits)
                        if (u.Team != Team && !u.IsDead && Vector3.Distance(transform.position, u.transform.position) <= effRadius)
                            u.ApplyStatus(StatusType.Taunted, sk.statusDur, 0f, this);
                    AbyssalDungeon.VFX.VFXPoolManager.Instance.SpawnForClassSkill(
                        HeroClass.id, slot, transform.position, Quaternion.identity, 1.15f);
                    break;

                case SkillKind.Summon:
                    GameAudio.I.PlaySfx(GameSfx.Magic, 0.85f);
                    var minGo = new GameObject("SoulMinion");
                    minGo.transform.position = transform.position + transform.right * 1.4f;
                    var minUnit = minGo.AddComponent<ActorUnit>();
                    minUnit.InitSummon(this, sk.statusDur + (rank - 1) * 2.5f);
                    minGo.AddComponent<HeroPartyAI>().IsMinion = true;
                    AbyssalDungeon.VFX.VFXPoolManager.Instance.SpawnForClassSkill(
                        HeroClass.id, slot, minGo.transform.position, Quaternion.identity, 0.95f);
                    break;

                case SkillKind.Revive:
                    GameAudio.I.PlaySfx(GameSfx.LevelUp, 0.9f);
                    foreach (var u in AllUnits)
                    {
                        if (u.Team == Team && !u.IsSummon)
                        {
                            if (u.IsDowned || u.IsDead) u.Revive(Mathf.Min(1f, 0.65f + (rank - 1) * 0.08f));
                            else u.ReceiveHeal(rawPow * (1f + HealBonus), this);
                        }
                    }
                    AbyssalDungeon.VFX.VFXPoolManager.Instance.SpawnForClassSkill(
                        HeroClass.id, slot, transform.position, transform.rotation, 1.25f, -1f, transform);
                    break;

                case SkillKind.Trap:
                    GameAudio.I.PlaySfx(GameSfx.Trap, 0.75f);
                    Vector3 trapPos = ClampRange(transform.position, aimPoint, effRange);
                    PlacedTrap.Spawn(this, trapPos, effRadius, rawPow, sk.status, sk.statusDur, fxCol);
                    break;

                case SkillKind.Transform:
                    GameAudio.I.PlaySfx(GameSfx.BossRoar, 0.75f);
                    float bearDur = (HeroClass.id == 8 && BuildIndex == 1) ? 18f : sk.statusDur;
                    SetBearForm(!InBearForm, bearDur + (rank - 1) * 1.5f);
                    break;

                case SkillKind.Cleanse:
                    GameAudio.I.PlaySfx(GameSfx.Heal, 0.8f);
                    foreach (var u in AllUnits)
                    {
                        if (u.Team == Team && !u.IsDead && !u.IsDowned && Vector3.Distance(transform.position, u.transform.position) <= effRadius)
                        {
                            u.CleanseDebuffs();
                            u.AddShield(u.MaxHp * sk.power * rankPow * (1f + HealBonus));
                        }
                    }
                    AbyssalDungeon.VFX.VFXPoolManager.Instance.SpawnForClassSkill(
                        HeroClass.id, slot, transform.position, Quaternion.identity, 1.15f, -1f, transform);
                    break;
            }

            // Execute Build-Specific Active Skill Augmentations across all 10 Classes!
            ClassSpecializationSystem.ExecuteBuildSkillAugment(this, slot, sk, aimPoint, dir, rawPow);
            return true;
        }

        public void SetBearForm(bool bear, float duration = 12f)
        {
            InBearForm = bear;
            _bearFormTimer = duration;
            if (MainVisual != null) MainVisual.SetActive(!bear);
            if (BearVisual != null) BearVisual.SetActive(bear);
            Anim = (bear && BearVisual != null)
                ? BearVisual.GetComponentInChildren<Animator>()
                : (MainVisual != null ? MainVisual.GetComponentInChildren<Animator>() : null);
            if (Anim != null) Anim.applyRootMotion = false;
            if (_motionPolisher != null) _motionPolisher.Bind(this);

            if (bear)
            {
                float shieldPct = (HeroClass != null && HeroClass.id == 8 && BuildIndex == 1) ? 0.45f : 0.25f;
                AddShield(MaxHp * shieldPct);
            }
            RecalculateStats(false);
            CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up, new Color(0.3f, 0.9f, 0.45f), 2.2f, 0.4f);
        }

        private IEnumerator MultiArcRoutine(int hits, float rawPow, SkillDef sk, float effRadius = -1f, Color overrideCol = default)
        {
            int token = _attackCancelVersion;
            float rad = effRadius > 0f ? effRadius : sk.radius;
            Color fxCol = overrideCol != default ? overrideCol : sk.color;
            for (int i = 0; i < hits; i++)
            {
                if (IsDead || IsDowned || token != _attackCancelVersion) yield break;
                PulseWeaponTrails(0.18f, fxCol);
                CombatFeelManager.I.SpawnSlashArc(
                    transform.position + Vector3.up * 1.1f + transform.forward * 0.25f,
                    transform.rotation,
                    Mathf.Clamp(rad * 0.85f, 1.5f, 4.2f),
                    Mathf.Clamp(sk.arc, 90f, 340f),
                    (i % 2 == 0) ? -16f : 18f,
                    Color.Lerp(fxCol, Color.white, 0.45f),
                    fxCol,
                    0.20f,
                    0.72f,
                    i % 2 == 1);
                DealConeDamage(rawPow, rad, sk.arc, sk.status, sk.statusDur, EffectiveAttack() * sk.statusPower, fxCol);
                if (i < hits - 1) yield return new WaitForSeconds(0.11f);
            }
        }

        private IEnumerator DashAttackRoutine(Vector3 dir, float dist, float rawDmg, float hitRadius, Color col)
        {
            IsInvincible = true;
            var hitSet = new HashSet<ActorUnit>();
            float dur = 0.22f, t = 0f;
            float spd = dist / dur;
            while (t < dur && !IsDead && !IsDowned)
            {
                _queuedPlanarVel = dir * spd;
                for (int i = AllUnits.Count - 1; i >= 0; i--)
                {
                    var u = AllUnits[i];
                    if (u != null && u.Team != Team && !u.IsDead && !hitSet.Contains(u))
                    {
                        if (Vector3.Distance(transform.position, u.transform.position) <= hitRadius)
                        {
                            hitSet.Add(u);
                            DealDamageTo(u, rawDmg, StatusType.None, 0f, 0f, isMultiTargetOrAoe: true);
                        }
                    }
                }
                t += Mathf.Min(Time.deltaTime, 0.05f);
                yield return null;
            }
            IsInvincible = false;
            CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up, col, hitRadius, 0.35f);
        }

        public void DealConeDamage(float rawDamage, float radius, float arcDeg, StatusType status, float statusDur, float statusPow, Color fxCol, bool heavyTelegraph = false)
        {
            CharacterVisualFactory.SpawnBurst(transform.position + transform.forward * (radius * 0.5f) + Vector3.up * 0.9f, fxCol, radius * 0.6f, 0.28f);
            float cosHalf = Mathf.Cos(arcDeg * 0.5f * Mathf.Deg2Rad);
            for (int i = AllUnits.Count - 1; i >= 0; i--)
            {
                var u = AllUnits[i];
                if (u == null || u.Team == Team || u.IsDead || u.IsDowned) continue;
                Vector3 to = u.transform.position - transform.position;
                to.y = 0f;
                float d = to.magnitude;
                if (d <= radius + (u.Controller != null ? u.Controller.radius : 0.4f))
                {
                    if (d < 0.6f || Vector3.Dot(transform.forward, to.normalized) >= cosHalf)
                    {
                        DealDamageTo(u, rawDamage, status, statusDur, statusPow, 0f, 24f, 3.0f, u.transform.position + Vector3.up * 1.05f, heavyTelegraph, isMultiTargetOrAoe: true, heavyTelegraph: heavyTelegraph);
                    }
                }
            }
        }

        public void DealCircleDamage(Vector3 center, float radius, float rawDamage, StatusType status, float statusDur, float statusPow, Color fxCol, bool heavyTelegraph = false)
        {
            CharacterVisualFactory.SpawnBurst(center + Vector3.up * 0.5f, fxCol, radius, 0.42f);
            for (int i = AllUnits.Count - 1; i >= 0; i--)
            {
                var u = AllUnits[i];
                if (u == null || u.Team == Team || u.IsDead || u.IsDowned) continue;
                Vector3 a = center; a.y = 0f;
                Vector3 b = u.transform.position; b.y = 0f;
                float rExtra = u.Controller != null ? u.Controller.radius : 0.4f;
                if (Vector3.Distance(a, b) <= radius + rExtra)
                {
                    DealDamageTo(u, rawDamage, status, statusDur, statusPow, 0f, 28f, heavyTelegraph ? 5.2f : 3.2f, u.transform.position + Vector3.up * 1.05f, heavyTelegraph, isMultiTargetOrAoe: true, heavyTelegraph: heavyTelegraph);
                }
            }
        }

        public void DealDamageTo(
            ActorUnit target,
            float rawDamage,
            StatusType status,
            float statusDur,
            float statusPow,
            float bonusCrit = 0f,
            float poiseDmg = 20f,
            float knockbackForce = 2.0f,
            Vector3 contactPoint = default,
            bool isHeavyOrFinisher = false,
            bool isMultiTargetOrAoe = false,
            bool heavyTelegraph = false)
        {
            if (target == null || target.IsDead || target.IsDowned) return;
            if (target.IsInvincible)
            {
                CharacterVisualFactory.SpawnFloatingText(target.transform.position + Vector3.up * 1.8f, "NÉ ĐÒN!", new Color(0.55f, 0.92f, 1f), 0.92f);
                return;
            }

            // Positional Backstab Detection: striking from behind target (+15% damage + +8% crit)
            Vector3 attackVector = target.transform.position - transform.position;
            attackVector.y = 0f;
            bool backstab = false;
            if (Team == Team.Hero && attackVector.sqrMagnitude > 0.04f)
            {
                float backDot = Vector3.Dot(target.transform.forward, attackVector.normalized);
                if (backDot > 0.42f) backstab = true;
            }

            // Ranger passive "Mắt Ưng": +10% Crit (+18% on Deadeye Sniper) vs targets >= 8m away
            float rangerRangeCrit = 0f;
            if (HeroClass != null && HeroClass.id == 4 && attackVector.sqrMagnitude >= 64f)
                rangerRangeCrit = (BuildIndex == 0) ? 0.18f : 0.10f;

            float effectiveCrit = Mathf.Clamp01(CritChance + bonusCrit + rangerRangeCrit + (backstab ? 0.08f : 0f));
            bool crit = UnityEngine.Random.value < effectiveCrit;

            // Organic +/-5% RPG damage roll (removed duplicate 1.25x stagger double-dip!)
            float variance = UnityEngine.Random.Range(0.95f, 1.05f);
            float dmg = rawDamage * variance * (crit ? CritDamage : 1f);
            if (backstab) dmg *= 1.15f;

            // Assassin passive: +30% execute damage on targets below 30% HP
            if (HeroClass != null && HeroClass.id == 3 && target.Hp / Mathf.Max(1f, target.MaxHp) < 0.30f)
                dmg *= 1.30f;

            // AI Companion Damage Scaling: AI teammates deal ~40% of player damage (support roles, never carry solo!)
            if (Team == Team.Hero && !IsLocalPlayer)
            {
                dmg *= IsSummon ? 0.35f : 0.40f;
            }

            // Anti-snowball soft cap on extreme single-hit hero spikes (prevents 75k anomalies while rewarding big crits)
            if (Team == Team.Hero)
            {
                float softCap = EffectiveAttack() * 6.5f;
                if (dmg > softCap)
                    dmg = softCap + (dmg - softCap) * 0.35f;
            }

            Vector3 hitPt = contactPoint != default
                ? contactPoint
                : (target.transform.position + Vector3.up * 1.05f);
            Vector3 pushDir = attackVector.sqrMagnitude > 0.01f ? attackVector.normalized : transform.forward;

            // Apply Knockback & Poise/Stagger Break
            if (knockbackForce > 0.05f)
                target.ApplyKnockback(pushDir, knockbackForce * (crit ? 1.15f : 1.0f));

            bool brokeGuard = false;
            if (poiseDmg > 0f)
                brokeGuard = target.ApplyPoiseDamage(poiseDmg * (crit ? 1.15f : 1.0f), this);

            target.ReceiveHit(dmg, crit, this, isHeavyOrFinisher || backstab, isMultiTargetOrAoe, heavyTelegraph);

            // 3D Contact Sparks & Subtle Hit-Stop Juice
            Color sparkCol = HeroClass != null ? HeroClass.color : new Color(1f, 0.55f, 0.3f);
            CombatFeelManager.I.SpawnHitImpact(hitPt, pushDir, sparkCol, crit || isHeavyOrFinisher, target);

            if (IsLocalPlayer)
            {
                CombatFeelManager.I.RecordPlayerHit(crit, isHeavyOrFinisher, brokeGuard);
                if (ScreenCombatFeedbackUI.Instance != null)
                    ScreenCombatFeedbackUI.Instance.NotifyEnemyStruck(target);

                if (!brokeGuard && (crit || isHeavyOrFinisher))
                {
                    CombatFeelManager.I.TriggerHitStop(0.03f, 0.08f);
                    if (OrbitCamera3D.Instance != null)
                        OrbitCamera3D.Instance.Shake(isHeavyOrFinisher ? 0.12f : 0.07f, 0.14f);
                }
                if (backstab && HeroClass != null && HeroClass.id == 3)
                    CharacterVisualFactory.SpawnFloatingText(target.transform.position + Vector3.up * 2.15f, "ĐÁNH LÉN!", new Color(0.85f, 0.45f, 1f), 0.85f);
            }

            if (status != StatusType.None && !target.IsDead)
            {
                float sp = statusPow;
                // Mage Fire build: stronger Burn
                if (HeroClass != null && HeroClass.id == 5 && BuildIndex == 0 && status == StatusType.Burn) sp *= 1.35f;
                target.ApplyStatus(status, statusDur, sp, this);
            }
        }

        public void ReceiveHit(float incomingRaw, bool crit, ActorUnit attacker, bool emphaticPopup = false, bool isMultiTargetOrAoe = false, bool heavyTelegraph = false)
        {
            if (IsDead || IsDowned || IsInvincible) return;

            // Asymptotic ARPG Defense Formula: DamageReduction = DEF / (DEF + 150 + 25 * AttackerLevel), capped at 65%
            int attackerLevel = attacker != null ? Mathf.Max(1, attacker.Level) : Mathf.Max(1, Level);
            float def = Mathf.Max(0f, EffectiveDefense());
            float damageReduction = Mathf.Clamp(def / (def + 150f + 25f * attackerLevel), 0f, 0.65f);
            float finalDmg = incomingRaw * (1f - damageReduction);

            // Curse increases damage taken by 20%
            if (HasStatus(StatusType.Curse)) finalDmg *= 1.20f;

            // Staggered monsters take 140% damage (rebalanced from 200%/250% to curb snowball bursts)
            if (Team == Team.Monster)
            {
                if (IsStaggered)
                    finalDmg *= 1.40f;
                else if (ActiveEliteAffix == EliteAffixKind.ArcaneShielded)
                    finalDmg *= 0.78f;
            }

            // Knight passive -8% damage taken
            if (HeroClass != null && HeroClass.id == 0) finalDmg *= 0.92f;

            // Guardian aura: nearby allies take 6% less damage
            if (Team == Team.Hero)
            {
                foreach (var u in AllUnits)
                {
                    if (u.Team == Team.Hero && !u.IsDead && !u.IsDowned && u.HeroClass != null && u.HeroClass.id == 1)
                    {
                        if (Vector3.Distance(transform.position, u.transform.position) <= 10f) { finalDmg *= 0.94f; break; }
                    }
                }
            }

            // Strict non-zero damage floor so Boss/Monsters never hit Heroes for 0 HP!
            float minChipFloor = Mathf.Max(5f, incomingRaw * 0.28f);
            finalDmg = Mathf.Round(Mathf.Max(minChipFloor, finalDmg));

            float beforeHp = Hp;
            float hpDamage = finalDmg;

            // Heavy red-telegraph AoEs & Boss strikes pierce 35% of shields directly into HP so players must dodge!
            float shieldPierce = (Team == Team.Hero && (heavyTelegraph || (attacker != null && attacker.IsBoss)))
                ? Mathf.Round(hpDamage * 0.35f)
                : 0f;
            float shieldableDmg = hpDamage - shieldPierce;

            if (Shield > 0f && shieldableDmg > 0f)
            {
                float absorbed = Mathf.Min(Shield, shieldableDmg);
                Shield -= absorbed;
                shieldableDmg -= absorbed;
            }

            float actualHpDmg = shieldPierce + shieldableDmg;
            Hp = Mathf.Max(0f, Hp - actualHpDmg);
            float realHpLost = beforeHp - Hp;

            // Lifesteal for attacker: -60% effectiveness on AoE/multi-target hits + per-second MaxHP cap!
            if (attacker != null && !attacker.IsDead && !attacker.IsDowned && actualHpDmg > 0f)
            {
                float ls = attacker.Lifesteal + (attacker.HasStatus(StatusType.Rage) ? 0.10f : 0f);
                if (isMultiTargetOrAoe) ls *= 0.40f; // 60% reduction on AoE / multi-target skills
                if (ls > 0.001f)
                {
                    float cappedHeal = attacker.ConsumeLifestealBudget(actualHpDmg * ls);
                    if (cappedHeal > 0.5f)
                        attacker.ReceiveHeal(cappedHeal, attacker, false);
                }

                // Guardian "Thorns Templar" Build (Build 1): reflect 28% melee damage back to attacker!
                if (HeroClass != null && HeroClass.id == 1 && BuildIndex == 1 && attacker.Team != Team)
                {
                    if (Vector3.Distance(transform.position, attacker.transform.position) <= 4.4f)
                    {
                        float thorns = Mathf.Max(4f, Mathf.Round(finalDmg * 0.28f));
                        attacker.ReceiveTrueDot(thorns, this, new Color(1f, 0.56f, 0.18f));
                    }
                }

                // Elite Affix "Phản đòn / Thorns": reflects 14% of close-range damage back to attacker
                if (Team == Team.Monster && ActiveEliteAffix == EliteAffixKind.Thorns && attacker.Team != Team)
                {
                    if (Vector3.Distance(transform.position, attacker.transform.position) <= 5.2f)
                    {
                        float reflectDmg = Mathf.Max(4f, Mathf.Round(Mathf.Min(incomingRaw * 0.14f, attacker.MaxHp * 0.06f)));
                        attacker.ReceiveTrueDot(reflectDmg, this, new Color(1f, 0.45f, 0.18f));
                    }
                }
            }

            // Record Director stats
            if (AbyssDirectorAI.Instance != null)
            {
                if (Team == Team.Monster && attacker != null && attacker.Team == Team.Hero)
                    AbyssDirectorAI.Instance.RecordDamageDealt(finalDmg);
                else if (Team == Team.Hero && !IsSummon)
                    AbyssDirectorAI.Instance.RecordDamageTaken(realHpLost > 0f ? realHpLost : finalDmg);
            }

            float shownDmg = realHpLost > 0.5f ? realHpLost : finalDmg;
            Color popCol = Team == Team.Hero
                ? (heavyTelegraph ? new Color(1f, 0.16f, 0.16f) : new Color(1f, 0.38f, 0.38f))
                : (crit ? new Color(1f, 0.86f, 0.18f) : (emphaticPopup ? new Color(1f, 0.72f, 0.32f) : Color.white));
            float popScale = crit ? 1.24f : (heavyTelegraph ? 1.28f : (emphaticPopup ? 1.08f : 0.90f));
            CharacterVisualFactory.SpawnFloatingText(transform.position + Vector3.up * 1.8f, (crit || emphaticPopup || heavyTelegraph) ? $"{shownDmg:0}!" : $"{shownDmg:0}", popCol, popScale);
            GameAudio.I.PlaySfxVaried(crit ? GameSfx.CritHit : GameSfx.Hit, 0.46f, 0.07f);

            bool heavyHit = crit || emphaticPopup || heavyTelegraph || realHpLost >= MaxHp * 0.12f;
            if (_motionPolisher != null) _motionPolisher.NotifyTakeDamage(attacker, heavyHit);

            // Red screen vignette flash + subtle camera shake ONLY when player takes real HP loss
            if (IsLocalPlayer && realHpLost > 0.5f)
            {
                float hpLossFrac = realHpLost / Mathf.Max(1f, MaxHp);
                if (OrbitCamera3D.Instance != null)
                {
                    float shakeAmp = Mathf.Clamp(0.08f + hpLossFrac * 0.45f, 0.08f, 0.24f);
                    OrbitCamera3D.Instance.Shake(shakeAmp, heavyHit ? 0.20f : 0.13f);
                }
                if (ScreenCombatFeedbackUI.Instance != null)
                    ScreenCombatFeedbackUI.Instance.NotifyPlayerDamaged(hpLossFrac);
            }

            if (Team == Team.Monster)
            {
                var proc = GetComponentInChildren<EnemyProceduralAnimator>();
                if (proc != null) proc.TriggerHitReaction();
            }

            OnDamaged?.Invoke(this, attacker, realHpLost > 0f ? realHpLost : finalDmg, crit);

            if (Team == Team.Monster && (IsElite || IsMiniBoss) && !IsBoss && Hp > 0f && !IsEnraged && (Hp / Mathf.Max(1f, MaxHp)) < 0.40f)
            {
                TriggerMonsterEnrage();
            }

            if (Hp <= 0f)
            {
                HandleZeroHp();
            }
        }

        private void TriggerMonsterEnrage()
        {
            IsEnraged = true;
            AtkSpeed *= 1.25f;
            MoveSpeed *= 1.15f;
            ApplyStatus(StatusType.Rage, 999f, 0f, this);

            Color enrageCol = new Color(1.0f, 0.16f, 0.28f);
            GameAudio.I.PlaySfx(GameSfx.BossRoar, IsBoss ? 1f : 0.78f);
            CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up * 1.3f, enrageCol, IsBoss ? 6.5f : 3.6f, 0.65f);
            CharacterVisualFactory.SpawnFloatingText(transform.position + Vector3.up * 2.55f, "CUỒNG NỘ! (+25% TỐC ĐÁNH)", enrageCol, IsBoss ? 1.35f : 1.15f);

            var light = GetComponentInChildren<Light>();
            if (light != null)
            {
                light.color = enrageCol;
                light.intensity = Mathf.Max(light.intensity, 3.6f);
            }
            else if (MainVisual != null)
            {
                var auraGo = new GameObject("EnrageAuraLight");
                auraGo.transform.SetParent(MainVisual.transform, false);
                auraGo.transform.localPosition = new Vector3(0f, 1.35f, 0f);
                var pl = auraGo.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.color = enrageCol;
                pl.range = 6.5f;
                pl.intensity = 3.2f;
            }
        }

        private void ReceiveTrueDot(float amount, ActorUnit source, Color col)
        {
            if (IsDead || IsDowned) return;
            Hp = Mathf.Max(0f, Hp - amount);
            if (AbyssDirectorAI.Instance != null && Team == Team.Monster)
                AbyssDirectorAI.Instance.RecordDamageDealt(amount);
            CharacterVisualFactory.SpawnFloatingText(transform.position + Vector3.up * 1.7f, $"{amount:0}", col, 0.8f);
            if (Hp <= 0f) HandleZeroHp();
        }

        public void ReceiveHeal(float amount, ActorUnit healer, bool popup = true)
        {
            if (IsDead || IsDowned || amount <= 0.5f) return;
            float before = Hp;
            Hp = Mathf.Min(MaxHp, Hp + amount);
            float actual = Hp - before;
            if (actual > 0.5f)
            {
                if (AbyssDirectorAI.Instance != null && Team == Team.Hero && !IsSummon)
                    AbyssDirectorAI.Instance.RecordHealing(actual);
                if (popup)
                    CharacterVisualFactory.SpawnFloatingText(transform.position + Vector3.up * 1.9f, $"+{actual:0}", new Color(0.35f, 1f, 0.5f), 0.95f);
            }
        }

        public void AddShield(float amount)
        {
            if (IsDead || IsDowned || amount <= 0f) return;
            // Cap Hero shield at 35% MaxHP (down from 80%) so party cannot become permanently immune to Boss hits
            float maxCap = Team == Team.Hero ? (MaxHp * 0.35f) : (MaxHp * 0.40f);
            if (Team == Team.Hero && !IsLocalPlayer) amount *= 0.70f;
            Shield = Mathf.Min(maxCap, Shield + amount);
            CharacterVisualFactory.SpawnFloatingText(transform.position + Vector3.up * 2.0f, $"+Khiên {amount:0}", new Color(0.55f, 0.85f, 1f), 0.78f);
        }

        public void Revive(float hpPercent)
        {
            IsDowned = false;
            IsDead = false;
            ReviveProgress = 0f;
            Hp = Mathf.Max(50f, MaxHp * hpPercent);
            Mana = Mathf.Max(Mana, MaxMana * 0.4f);
            Stamina = MaxStamina;
            if (_motionPolisher != null) _motionPolisher.SetDeadBool(false);
            else if (Anim != null) Anim.SetBool(HashDead, false);
            CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up, new Color(1f, 0.92f, 0.5f), 2.2f, 0.5f);
            CharacterVisualFactory.SpawnFloatingText(transform.position + Vector3.up * 2.2f, "HỒI SINH!", new Color(1f, 0.92f, 0.4f), 1.2f);
        }

        private void HandleZeroHp()
        {
            if (Team == Team.Hero && !IsSummon)
            {
                if (AbyssDirectorAI.Instance != null) AbyssDirectorAI.Instance.RecordPlayerDeath();
                // Check if any non-summon hero ally is still standing
                bool anyStanding = false;
                foreach (var u in AllUnits)
                    if (u != this && u.Team == Team.Hero && !u.IsSummon && !u.IsDead && !u.IsDowned)
                        anyStanding = true;

                if (anyStanding)
                {
                    IsDowned = true;
                    IsSprinting = false;
                    ReviveProgress = 0f;
                    if (_motionPolisher != null) _motionPolisher.SetDeadBool(true);
                    else if (Anim != null) Anim.SetBool(HashDead, true);
                    CharacterVisualFactory.SpawnFloatingText(transform.position + Vector3.up * 2.2f, "GỤC NGÃ!", new Color(1f, 0.3f, 0.3f), 1.15f);
                    return;
                }
            }
            ForceKill();
        }

        public void ForceKill()
        {
            if (IsDead) return;
            IsDead = true;
            IsDowned = false;
            IsSprinting = false;
            Hp = 0f;
            if (Controller != null) Controller.enabled = false;
            if (_motionPolisher != null) _motionPolisher.SetDeadBool(true);
            else if (Anim != null) Anim.SetBool(HashDead, true);
            if (_hpBarRoot != null) _hpBarRoot.gameObject.SetActive(false);

            // Assassin "Venom Alchemist" Build (Build 2): poisoned enemies explode in a toxic miasma cloud on death!
            if (Team == Team.Monster)
            {
                // Elite Affix "Bùng nổ khi chết / Molten Death": spawns a 4.2m GroundTelegraphFx magma detonation!
                if (ActiveEliteAffix == EliteAffixKind.MoltenDeath)
                {
                    Vector3 deathPos = transform.position;
                    float blastDmg = EffectiveAttack() * 1.65f;
                    CharacterVisualFactory.SpawnFloatingText(deathPos + Vector3.up * 2.3f,
                        "BÙNG NỔ KHI CHẾT — NÉ [SPACE]!", new Color(1f, 0.45f, 0.12f), 1.15f);
                    CharacterVisualFactory.SpawnGroundTelegraph(deathPos, 4.2f, 0.92f, new Color(1f, 0.36f, 0.08f), () =>
                    {
                        GameAudio.I.PlaySfx(GameSfx.Explosion, 0.9f);
                        AbyssalDungeon.VFX.VFXPoolManager.Instance.Spawn("VFX_Mage_Meteor", deathPos, Quaternion.identity, null, 1.05f);
                        for (int k = AllUnits.Count - 1; k >= 0; k--)
                        {
                            var u = AllUnits[k];
                            if (u != null && u.Team == Team.Hero && !u.IsDead && !u.IsDowned)
                            {
                                if (Vector3.Distance(deathPos, u.transform.position) <= 4.2f)
                                {
                                    u.ReceiveHit(blastDmg, false, null, true);
                                    u.ApplyStatus(StatusType.Burn, 4f, blastDmg * 0.18f, null);
                                    u.ApplyKnockback(u.transform.position - deathPos, 7.5f);
                                }
                            }
                        }
                    });
                }

                for (int i = 0; i < Statuses.Count; i++)
                {
                    var st = Statuses[i];
                    if (st.type == StatusType.Poison && st.source != null && !st.source.IsDead
                        && st.source.HeroClass != null && st.source.HeroClass.id == 3 && st.source.BuildIndex == 2)
                    {
                        ActorUnit alch = st.source;
                        alch.DealCircleDamage(transform.position, 4.0f, alch.EffectiveAttack() * 1.15f,
                            StatusType.Poison, 5f, alch.EffectiveAttack() * 0.35f, new Color(0.28f, 0.96f, 0.38f));
                        break;
                    }
                }
            }

            OnKilled?.Invoke(this);

            if (Team == Team.Monster || IsSummon)
            {
                var proc = GetComponentInChildren<EnemyProceduralAnimator>();
                if (proc != null) proc.TriggerDeathCollapse();
                Destroy(gameObject, Anim != null ? 1.8f : 1.05f);
            }
        }

        private void HealAlliesAround(Vector3 center, float radius, float amount, StatusType bonusStatus = StatusType.None, float dur = 0f)
        {
            foreach (var u in AllUnits)
            {
                if (u.Team == Team && !u.IsDead && !u.IsDowned && Vector3.Distance(center, u.transform.position) <= radius)
                {
                    u.ReceiveHeal(amount, this);
                    if (bonusStatus != StatusType.None) u.ApplyStatus(bonusStatus, dur, 0f, this);
                }
            }
        }

        public ActorUnit FindWeakestAlly(float maxRange)
        {
            ActorUnit best = this;
            float bestPct = Hp / Mathf.Max(1f, MaxHp);
            foreach (var u in AllUnits)
            {
                if (u.Team == Team && !u.IsDead && !u.IsDowned && !u.IsSummon && Vector3.Distance(transform.position, u.transform.position) <= maxRange)
                {
                    float pct = u.Hp / Mathf.Max(1f, u.MaxHp);
                    if (pct < bestPct) { bestPct = pct; best = u; }
                }
            }
            return best;
        }

        private static Vector3 ClampRange(Vector3 origin, Vector3 target, float maxRange)
        {
            Vector3 d = target - origin;
            d.y = 0f;
            if (d.magnitude > maxRange) d = d.normalized * maxRange;
            return new Vector3(origin.x + d.x, 0f, origin.z + d.z);
        }
    }

    public class SkillProjectile : MonoBehaviour
    {
        private static readonly Queue<SkillProjectile> Pool = new Queue<SkillProjectile>();

        private ActorUnit _owner;
        private Team _team;
        private int _ownerClassId = -1;
        private int _skillSlot = -1;
        private Vector3 _dir;
        private float _speed, _maxDist, _traveled, _rawDmg, _aoeRadius, _statusDur, _statusPow;
        private float _poiseDmg, _kbForce, _bonusCrit;
        private bool _pierce;
        private StatusType _status;
        private Color _color;
        private TrailRenderer _tr;
        private Material _trailMat;
        private GameObject _pooledVisual;
        private readonly HashSet<ActorUnit> _hit = new HashSet<ActorUnit>();

        public static void Spawn(ActorUnit owner, Vector3 startPos, Vector3 dir, float speed, float range, float rawDmg,
            bool pierce, StatusType status, float statusDur, float statusPow, float aoeRadius, Color color,
            float poiseDmg = 20f, float kbForce = 2.0f, float bonusCrit = 0f, int skillSlot = -1)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
            dir.Normalize();
            Quaternion rot = Quaternion.LookRotation(dir);

            SkillProjectile p = null;
            while (Pool.Count > 0 && p == null)
            {
                p = Pool.Dequeue();
            }

            if (p == null)
            {
                var go = new GameObject("SkillProjectile_Pooled");
                p = go.AddComponent<SkillProjectile>();
                var tr = go.AddComponent<TrailRenderer>();
                tr.time = 0.14f;
                tr.minVertexDistance = 0.08f;
                tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tr.receiveShadows = false;
                tr.widthCurve = new AnimationCurve(new Keyframe(0f, 0.26f), new Keyframe(1f, 0f));
                p._tr = tr;
                p._trailMat = CharacterVisualFactory.MakeTransparentMat(new Color(color.r, color.g, color.b, 0.65f), color * 2f);
                tr.sharedMaterial = p._trailMat;
            }

            p.transform.SetPositionAndRotation(startPos, rot);
            p.gameObject.SetActive(true);
            p._hit.Clear();
            p._traveled = 0f;
            p._owner = owner;
            p._team = owner != null ? owner.Team : Team.Monster;
            p._ownerClassId = (owner != null && owner.HeroClass != null) ? owner.HeroClass.id : 5;
            p._skillSlot = skillSlot;
            p._dir = dir;
            p._speed = speed;
            p._maxDist = range;
            p._rawDmg = rawDmg;
            p._pierce = pierce;
            p._status = status;
            p._statusDur = statusDur;
            p._statusPow = statusPow;
            p._aoeRadius = aoeRadius;
            p._color = color;
            p._poiseDmg = poiseDmg;
            p._kbForce = kbForce;
            p._bonusCrit = bonusCrit;

            if (p._trailMat != null)
            {
                p._trailMat.color = new Color(color.r, color.g, color.b, 0.65f);
                if (p._trailMat.HasProperty("_BaseColor")) p._trailMat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, 0.65f));
                if (p._trailMat.HasProperty("_EmissionColor")) p._trailMat.SetColor("_EmissionColor", color * 2f);
            }
            if (p._tr != null) p._tr.Clear();

            string projKey = AbyssalDungeon.VFX.VFXPoolManager.GetProjectileVfxKey(p._ownerClassId);
            p._pooledVisual = AbyssalDungeon.VFX.VFXPoolManager.Instance.Spawn(projKey, startPos, rot, p.transform, 1.0f, 3.0f);
        }

        private void OnDestroy()
        {
            if (_trailMat != null) Destroy(_trailMat);
        }

        private void ReleaseProjectile()
        {
            if (_pooledVisual != null)
            {
                AbyssalDungeon.VFX.VFXPoolManager.Instance.Release(_pooledVisual);
                _pooledVisual = null;
            }
            gameObject.SetActive(false);
            Pool.Enqueue(this);
        }

        private void Update()
        {
            float step = _speed * Time.deltaTime;
            // Wall check
            if (Physics.Raycast(transform.position, _dir, step, 1 << 0, QueryTriggerInteraction.Ignore))
            {
                ExplodeIfNeeded();
                ReleaseProjectile();
                return;
            }

            transform.position += _dir * step;
            _traveled += step;

            for (int i = ActorUnit.AllUnits.Count - 1; i >= 0; i--)
            {
                var u = ActorUnit.AllUnits[i];
                if (u == null || u.Team == _team || u.IsDead || u.IsDowned || _hit.Contains(u)) continue;
                Vector3 upos = u.transform.position + Vector3.up * 1.0f;
                float hitR = (u.Controller != null ? u.Controller.radius : 0.45f) + 0.4f;
                if (Vector3.Distance(transform.position, upos) <= hitR)
                {
                    _hit.Add(u);
                    if (_skillSlot >= 0)
                    {
                        AbyssalDungeon.VFX.VFXPoolManager.Instance.SpawnForClassSkill(
                            _ownerClassId, _skillSlot, u.transform.position, Quaternion.identity, 0.9f);
                    }

                    if (_aoeRadius > 0.5f && _owner != null)
                    {
                        _owner.DealCircleDamage(u.transform.position, _aoeRadius, _rawDmg, _status, _statusDur, _statusPow, _color);
                    }
                    else if (_owner != null)
                    {
                        _owner.DealDamageTo(u, _rawDmg, _status, _statusDur, _statusPow, _bonusCrit, _poiseDmg, _kbForce, transform.position, _bonusCrit > 0.1f, isMultiTargetOrAoe: _pierce);
                    }
                    else
                    {
                        u.ReceiveHit(_rawDmg, false, null);
                    }

                    if (!_pierce)
                    {
                        ReleaseProjectile();
                        return;
                    }
                }
            }

            if (_traveled >= _maxDist)
            {
                ExplodeIfNeeded();
                ReleaseProjectile();
            }
        }

        private void ExplodeIfNeeded()
        {
            if (_aoeRadius > 0.5f && _owner != null)
                _owner.DealCircleDamage(transform.position, _aoeRadius, _rawDmg, _status, _statusDur, _statusPow, _color);
        }
    }

    public class PlacedTrap : MonoBehaviour
    {
        private static readonly Queue<PlacedTrap> Pool = new Queue<PlacedTrap>();

        private ActorUnit _owner;
        private float _radius, _dmg, _dur, _life = 14f;
        private StatusType _status;
        private Color _col;
        private GameObject _pooledTrapVfx;

        public static void Spawn(ActorUnit owner, Vector3 pos, float radius, float dmg, StatusType status, float dur, Color col)
        {
            PlacedTrap t = null;
            while (Pool.Count > 0 && t == null)
            {
                t = Pool.Dequeue();
            }
            if (t == null)
            {
                var go = new GameObject("FrostTrap_Pooled");
                t = go.AddComponent<PlacedTrap>();
            }

            Vector3 groundPos = new Vector3(pos.x, 0.04f, pos.z);
            t.transform.SetPositionAndRotation(groundPos, Quaternion.identity);
            t.gameObject.SetActive(true);
            t._owner = owner;
            t._radius = radius;
            t._dmg = dmg;
            t._status = status;
            t._dur = dur;
            t._col = col;
            t._life = 14f;

            // Spawn the 3D EntangleRoots + GroundCracks + Spores VFX from pool
            t._pooledTrapVfx = AbyssalDungeon.VFX.VFXPoolManager.Instance.Spawn(
                "VFX_Ranger_FrostTrap", groundPos, Quaternion.identity, t.transform, Mathf.Clamp(radius / 3.2f, 0.85f, 1.35f), 14f);
        }

        private void ReleaseTrap()
        {
            if (_pooledTrapVfx != null)
            {
                AbyssalDungeon.VFX.VFXPoolManager.Instance.Release(_pooledTrapVfx);
                _pooledTrapVfx = null;
            }
            gameObject.SetActive(false);
            Pool.Enqueue(this);
        }

        private void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f)
            {
                ReleaseTrap();
                return;
            }
            for (int i = ActorUnit.AllUnits.Count - 1; i >= 0; i--)
            {
                var u = ActorUnit.AllUnits[i];
                if (u != null && _owner != null && u.Team != _owner.Team && !u.IsDead)
                {
                    if (Vector3.Distance(transform.position, u.transform.position) <= _radius * 0.65f)
                    {
                        GameAudio.I.PlaySfx(GameSfx.Explosion, 0.7f);
                        AbyssalDungeon.VFX.VFXPoolManager.Instance.Spawn(
                            "VFX_Ranger_FrostTrap", u.transform.position, Quaternion.identity, null, 1.15f, 1.35f);
                        _owner.DealCircleDamage(transform.position, _radius, _dmg, _status, _dur, 0f, _col);
                        ReleaseTrap();
                        return;
                    }
                }
            }
        }
    }
}


using UnityEngine;

namespace AbyssGame
{
    public enum MonsterFlowState
    {
        Idle,
        Patrol,
        Detect,
        SelectTarget,
        Chase,
        Attack,
        Evaluate,
        Retreat
    }

    /// <summary>
    /// Phase 5 (Section 5 & GDD 3.3, 3.4):
    /// Hybrid State Machine + Behavior Tree / Utility AI Monster Controller:
    /// Idle -> Patrol -> Detect -> SelectTarget -> Chase -> Attack -> Evaluate -> Retreat / SwitchTarget
    /// with distinct coordinated role behaviors:
    /// - Archer / Mage (Goblin Archer, Skeleton Mage): Keeps distance, ranged attacks, kites & strafes away.
    /// - Assassin (Goblin, Demon): Utility-targets weakest/isolated hero, paths behind target for Backstab (+25% dmg), retreats when endangered.
    /// - Tank (Skeleton Warrior, Dark Knight, Crystal Golem): Protects Elite/Mage allies with Guard Link shield & taunting slams.
    /// - Elite Monsters: Dynamic Elite Affixes (Vampiric, Molten, Stormcaller, FrozenPulse).
    /// </summary>
    [RequireComponent(typeof(ActorUnit))]
    public class MonsterAI : MonoBehaviour
    {
        public MonsterFlowState State = MonsterFlowState.Idle;
        public ActorUnit CurrentTarget;
        public Vector3 SpawnHome;
        public float RoomHalfExtent = 12f;
        public readonly EnemyBlackboard Blackboard = new EnemyBlackboard();

        private ActorUnit _self;
        private EnemyProceduralAnimator _procAnim;
        private Vector3 _patrolPoint;
        private float _stateTimer;
        private float _specialCd;
        private float _strafeSign = 1f;

        private void Awake()
        {
            _self = GetComponent<ActorUnit>();
            _strafeSign = Random.value > 0.5f ? 1f : -1f;
            Blackboard.Self = _self;
            Blackboard.Controller = this;
        }

        private void Start()
        {
            if (SpawnHome == Vector3.zero) SpawnHome = transform.position;
            _procAnim = GetComponentInChildren<EnemyProceduralAnimator>();
            Blackboard.EliteAffix = EnemyTacticalBrain.RollEliteAffix(_self.MonsterDef, _self.IsElite);
            if (Blackboard.EliteAffix != EliteAffixKind.None)
            {
                string afx = EnemyTacticalBrain.GetAffixLabel(Blackboard.EliteAffix);
                if (!string.IsNullOrEmpty(afx))
                    _self.DisplayName = $"{_self.DisplayName} [{afx}]";
            }

            PickNewPatrolPoint();
            State = MonsterFlowState.Idle;
            _stateTimer = Random.Range(0.45f, 1.25f);
            _specialCd = Random.Range(2f, 4.5f);
        }

        private void Update()
        {
            if (_self == null || _self.IsDead || !_self.CanAct())
            {
                if (_self != null) _self.StopMovingAnim();
                if (_self != null && _self.IsDead && _procAnim != null)
                    _procAnim.TriggerDeathCollapse();
                return;
            }

            float dt = Time.deltaTime;
            _stateTimer -= dt;
            _specialCd -= dt;
            if (Blackboard.BackstabBonusReadyTimer > 0f)
                Blackboard.BackstabBonusReadyTimer -= dt;

            // Always honor Tank Taunt immediately
            if (_self.TauntTarget != null && !_self.TauntTarget.IsDead && !_self.TauntTarget.IsDowned)
            {
                CurrentTarget = _self.TauntTarget;
                Blackboard.Target = CurrentTarget;
                if (State == MonsterFlowState.Idle || State == MonsterFlowState.Patrol)
                    State = MonsterFlowState.Chase;
            }

            // Tick Elite Affix & Tank VIP Guard Link synergies
            EnemyTacticalBrain.TickEliteAndRoleSynergies(Blackboard, dt);

            switch (State)
            {
                case MonsterFlowState.Idle:
                    UpdateIdle();
                    break;
                case MonsterFlowState.Patrol:
                    UpdatePatrol();
                    break;
                case MonsterFlowState.Detect:
                    UpdateDetect();
                    break;
                case MonsterFlowState.SelectTarget:
                    SelectTacticalTarget();
                    State = CurrentTarget != null ? MonsterFlowState.Chase : MonsterFlowState.Patrol;
                    break;
                case MonsterFlowState.Chase:
                    UpdateChase();
                    break;
                case MonsterFlowState.Attack:
                    UpdateAttack();
                    break;
                case MonsterFlowState.Evaluate:
                    EvaluateSituation();
                    break;
                case MonsterFlowState.Retreat:
                    UpdateRetreat();
                    break;
            }
        }

        private void UpdateIdle()
        {
            _self.StopMovingAnim();
            float detectRange = _self.MonsterDef != null ? _self.MonsterDef.detect : 14f;
            if (AnyHeroInRange(detectRange))
            {
                State = MonsterFlowState.Detect;
                _stateTimer = 0.28f;
                return;
            }

            if (_stateTimer <= 0f)
            {
                PickNewPatrolPoint();
                State = MonsterFlowState.Patrol;
                _stateTimer = Random.Range(3.2f, 5.6f);
            }
        }

        private void UpdatePatrol()
        {
            float detectRange = _self.MonsterDef != null ? _self.MonsterDef.detect : 14f;
            if (AnyHeroInRange(detectRange))
            {
                State = MonsterFlowState.Detect;
                _stateTimer = 0.30f;
                _self.StopMovingAnim();
                return;
            }

            Vector3 to = _patrolPoint - transform.position;
            to.y = 0f;
            if (to.magnitude < 1.1f || _stateTimer <= 0f)
            {
                State = MonsterFlowState.Idle;
                _stateTimer = Random.Range(0.6f, 1.4f);
                return;
            }

            Vector3 dir = HeroPartyAI.ObstacleAvoidDir(transform.position, to.normalized);
            _self.FaceDirection(dir, 8f);
            _self.MoveWorld(dir * (_self.EffectiveMoveSpeed() * 0.55f * Time.deltaTime));
        }

        private void UpdateDetect()
        {
            SelectTacticalTarget();
            if (CurrentTarget != null)
                _self.FaceDirection(CurrentTarget.transform.position - transform.position, 14f);
            if (_stateTimer <= 0f)
                State = MonsterFlowState.SelectTarget;
        }

        private void SelectTacticalTarget()
        {
            CurrentTarget = EnemyTacticalBrain.EvaluateBestTarget(_self, CurrentTarget, out ActorUnit vip);
            Blackboard.Target = CurrentTarget;
            Blackboard.ProtectedVipAlly = vip;
        }

        private void UpdateChase()
        {
            if (CurrentTarget == null || CurrentTarget.IsDead || CurrentTarget.IsDowned)
            {
                State = MonsterFlowState.Patrol;
                return;
            }

            Vector3 toTarget = CurrentTarget.transform.position - transform.position;
            toTarget.y = 0f;
            float distToHero = toTarget.magnitude;
            Archetype arch = _self.MonsterDef != null ? _self.MonsterDef.archetype : Archetype.Melee;

            // Assassin shadow step behind target when at medium distance
            if (arch == Archetype.Assassin && _specialCd <= 0f && distToHero > 3.8f && distToHero < 11f)
            {
                _specialCd = 6.2f;
                Blackboard.BackstabBonusReadyTimer = 2.5f;
                _self.TryDodge(toTarget.normalized);
                return;
            }

            // Tank monster guards Elite/Mage ally while raising block shield & taunting nearby heroes
            if (arch == Archetype.Tank && _specialCd <= 0f && distToHero < 8.5f)
            {
                _specialCd = 7.8f;
                _self.AddShield(_self.MaxHp * 0.18f);
                if (Blackboard.ProtectedVipAlly != null && !Blackboard.ProtectedVipAlly.IsDead)
                {
                    Blackboard.ProtectedVipAlly.AddShield(Blackboard.ProtectedVipAlly.MaxHp * 0.15f);
                    CharacterVisualFactory.SpawnFloatingText(transform.position + Vector3.up * 2.1f, "HỘ VỆ TINH ANH!", new Color(0.55f, 0.85f, 1f), 0.9f);
                }

                if (_self.MonsterDef != null && _self.MonsterDef.id == "CrystalGolem" && distToHero <= 4.8f)
                {
                    Vector3 slamPos = transform.position;
                    CharacterVisualFactory.SpawnGroundTelegraph(slamPos, 4.2f, 0.85f, new Color(0.45f, 0.75f, 1f), () =>
                    {
                        if (_self != null && !_self.IsDead)
                        {
                            GameAudio.I.PlaySfx(GameSfx.Explosion, 0.85f);
                            _self.DealCircleDamage(slamPos, 4.2f, _self.EffectiveAttack() * 1.7f, StatusType.Stun, 1.1f, 0f, new Color(0.45f, 0.8f, 1f));
                        }
                    });
                }
            }

            if (distToHero <= _self.AttackRange)
            {
                State = MonsterFlowState.Attack;
                _stateTimer = 1.6f;
                return;
            }

            // Compute tactical surround / rear-flank / VIP-guard point
            Vector3 engagePoint = EnemyTacticalBrain.ComputeTacticalEngagePoint(_self, CurrentTarget, Blackboard.ProtectedVipAlly, out bool rearFlank);
            Blackboard.TacticalDestination = engagePoint;
            if (rearFlank) Blackboard.BackstabBonusReadyTimer = 1.5f;

            Vector3 moveVec = engagePoint - transform.position;
            moveVec.y = 0f;
            if (moveVec.sqrMagnitude < 0.25f) moveVec = toTarget;

            Vector3 dir = HeroPartyAI.ObstacleAvoidDir(transform.position, moveVec.normalized);
            _self.FaceDirection(dir, 12f);
            _self.MoveWorld(dir * (_self.EffectiveMoveSpeed() * Time.deltaTime));
        }

        private void UpdateAttack()
        {
            if (CurrentTarget == null || CurrentTarget.IsDead || CurrentTarget.IsDowned)
            {
                State = MonsterFlowState.SelectTarget;
                return;
            }

            Vector3 to = CurrentTarget.transform.position - transform.position;
            to.y = 0f;
            float dist = to.magnitude;
            Archetype arch = _self.MonsterDef != null ? _self.MonsterDef.archetype : Archetype.Melee;

            // Archer / Mage keeps distance if hero rushes them (Section 5: "Giữ khoảng cách, Di chuyển tránh người chơi")
            if ((arch == Archetype.Ranged || arch == Archetype.Mage) && dist < 5.4f)
            {
                State = MonsterFlowState.Retreat;
                _stateTimer = 1.45f;
                return;
            }

            if (dist > _self.AttackRange + 1.2f)
            {
                State = MonsterFlowState.Chase;
                return;
            }

            _self.FaceDirection(to, 16f);

            if (_self.IsRanged)
            {
                Vector3 strafe = Vector3.Cross(Vector3.up, to.normalized) * _strafeSign;
                _self.MoveWorld(strafe * (_self.EffectiveMoveSpeed() * 0.42f * Time.deltaTime));
            }
            else if (arch == Archetype.Assassin)
            {
                // Assassin circles behind the target's back while in melee range!
                float backDot = Vector3.Dot((transform.position - CurrentTarget.transform.position).normalized, -CurrentTarget.transform.forward);
                if (backDot < 0.35f)
                {
                    Vector3 orbit = Vector3.Cross(Vector3.up, to.normalized) * _strafeSign;
                    _self.MoveWorld(orbit * (_self.EffectiveMoveSpeed() * 0.65f * Time.deltaTime));
                }
                else
                {
                    Blackboard.BackstabBonusReadyTimer = 1.2f;
                    _self.StopMovingAnim();
                }
            }
            else
            {
                _self.StopMovingAnim();
            }

            // Special species attacks across the 10 Enemy types
            if (_specialCd <= 0f && _self.MonsterDef != null)
            {
                string id = _self.MonsterDef.id;
                if (id == "DarkKnight" && dist <= 4.2f)
                {
                    _specialCd = 6.5f;
                    _procAnim?.TriggerAttackPulse();
                    Vector3 p = transform.position + transform.forward * 2f;
                    CharacterVisualFactory.SpawnGroundTelegraph(p, 3.2f, 0.75f, new Color(0.75f, 0.15f, 0.9f), () =>
                    {
                        if (_self != null && !_self.IsDead)
                            _self.DealCircleDamage(p, 3.2f, _self.EffectiveAttack() * 1.85f, StatusType.Slow, 2.5f, 0f, new Color(0.7f, 0.15f, 0.95f));
                    });
                    return;
                }
                if (id == "Demon" && dist <= 3.8f)
                {
                    _specialCd = 5.8f;
                    _procAnim?.TriggerAttackPulse();
                    float backMult = Blackboard.BackstabBonusReadyTimer > 0f ? 1.25f : 1.0f;
                    if (backMult > 1.05f)
                        CharacterVisualFactory.SpawnFloatingText(transform.position + Vector3.up * 2.1f, "ĐÁNH LÉN SAU LƯNG!", new Color(1f, 0.32f, 0.25f), 0.95f);
                    _self.DealConeDamage(_self.EffectiveAttack() * 1.55f * backMult, 3.2f, 110f, StatusType.Burn, 3.5f, _self.EffectiveAttack() * 0.22f, new Color(1f, 0.28f, 0.15f));
                    return;
                }
                if (id == "Goblin" && dist <= 2.4f && Blackboard.BackstabBonusReadyTimer > 0f)
                {
                    _specialCd = 4.8f;
                    _procAnim?.TriggerAttackPulse();
                    CharacterVisualFactory.SpawnFloatingText(transform.position + Vector3.up * 1.9f, "ĐÂM LÉN (+25%)!", new Color(1f, 0.8f, 0.25f), 0.9f);
                    _self.DealDamageTo(CurrentTarget, _self.EffectiveAttack() * 1.25f, StatusType.Poison, 3f, _self.EffectiveAttack() * 0.15f);
                    Blackboard.BackstabBonusReadyTimer = 0f;
                    return;
                }
                if (id == "GoblinArcher")
                {
                    _specialCd = 5.2f;
                    _procAnim?.TriggerAttackPulse();
                    SkillProjectile.Spawn(_self, transform.position + Vector3.up * 1.25f, to.normalized, 13.5f, 18f,
                        _self.EffectiveAttack() * 1.2f, false, StatusType.Poison, 3.5f, _self.EffectiveAttack() * 0.15f, 0f, new Color(0.48f, 0.92f, 0.28f));
                    return;
                }
                if (id == "SkeletonWarrior" && dist <= 3.0f)
                {
                    _specialCd = 6.0f;
                    _procAnim?.TriggerAttackPulse();
                    _self.AddShield(_self.MaxHp * 0.16f);
                    _self.DealConeDamage(_self.EffectiveAttack() * 1.3f, 2.8f, 100f, StatusType.Slow, 2.0f, 0f, new Color(0.75f, 0.82f, 0.95f));
                    return;
                }
                if (id == "SkeletonMage")
                {
                    _specialCd = 5.5f;
                    _procAnim?.TriggerAttackPulse();
                    SkillProjectile.Spawn(_self, transform.position + Vector3.up * 1.3f, to.normalized, 14f, 16f,
                        _self.EffectiveAttack() * 1.25f, false, StatusType.Curse, 4f, 0f, 2.2f, new Color(0.65f, 0.25f, 1f));
                    return;
                }
                if (id == "Zombie" && dist <= 2.5f)
                {
                    _specialCd = 5f;
                    _procAnim?.TriggerAttackPulse();
                    _self.DealConeDamage(_self.EffectiveAttack(), 2.5f, 120f, StatusType.Poison, 4f, _self.EffectiveAttack() * 0.2f, new Color(0.4f, 0.8f, 0.3f));
                    return;
                }
                if (id == "ForestMonster" && dist <= 3.8f)
                {
                    _specialCd = 5.5f;
                    _procAnim?.TriggerAttackPulse();
                    _self.DealCircleDamage(transform.position, 3.6f, _self.EffectiveAttack() * 1.25f, StatusType.Slow, 3.2f, 0f, new Color(0.35f, 0.85f, 0.3f));
                    return;
                }
            }

            if (_self.TryBasicAttack(to.normalized, CurrentTarget.transform.position))
            {
                _procAnim?.TriggerAttackPulse();
            }

            if (_stateTimer <= 0f)
                State = MonsterFlowState.Evaluate;
        }

        private void EvaluateSituation()
        {
            Archetype arch = _self.MonsterDef != null ? _self.MonsterDef.archetype : Archetype.Melee;
            float hpPct = _self.Hp / Mathf.Max(1f, _self.MaxHp);

            // Assassin / Ranged / Mage tactical retreat when endangered (Section 5: "Rút lui khi gặp nguy hiểm")
            float retreatThreshold = (arch == Archetype.Assassin || arch == Archetype.Ranged || arch == Archetype.Mage) ? 0.32f : 0.20f;
            if (hpPct < retreatThreshold && arch != Archetype.Tank && !_self.IsMiniBoss && Random.value < 0.65f)
            {
                if (arch == Archetype.Assassin)
                    CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up * 0.8f, new Color(0.25f, 0.15f, 0.35f), 1.6f, 0.35f);
                State = MonsterFlowState.Retreat;
                _stateTimer = 1.65f;
                return;
            }

            SelectTacticalTarget();
            State = CurrentTarget != null ? MonsterFlowState.Attack : MonsterFlowState.Patrol;
            _stateTimer = 1.75f;
        }

        private void UpdateRetreat()
        {
            if (CurrentTarget == null || CurrentTarget.IsDead || _stateTimer <= 0f)
            {
                State = MonsterFlowState.SelectTarget;
                return;
            }
            Vector3 away = transform.position - CurrentTarget.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            Vector3 dir = HeroPartyAI.ObstacleAvoidDir(transform.position, away.normalized);
            _self.FaceDirection(dir, 14f);
            _self.MoveWorld(dir * (_self.EffectiveMoveSpeed() * 1.05f * Time.deltaTime));
        }

        private bool AnyHeroInRange(float r)
        {
            foreach (var u in ActorUnit.AllUnits)
                if (u != null && u.Team == Team.Hero && !u.IsDead && !u.IsDowned && Vector3.Distance(transform.position, u.transform.position) <= r)
                    return true;
            return false;
        }

        private void PickNewPatrolPoint()
        {
            Vector2 rnd = Random.insideUnitCircle * Mathf.Min(5.5f, RoomHalfExtent * 0.6f);
            _patrolPoint = SpawnHome + new Vector3(rnd.x, 0f, rnd.y);
        }
    }
}

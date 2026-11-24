using UnityEngine;

namespace AbyssGame
{
    public enum PartyAIState
    {
        RegroupOutOfCombat,
        EvadeTelegraph,
        ReviveAndSupport,
        FrontlineIntercept,
        RangedTacticalKite,
        MeleeAssassinate
    }

    /// <summary>
    /// Phase 3 Upgrade: Role-Based Utility AI + State Machine + Separation Steering
    /// for Lyra (Knight - Tank/Stagger Frontliner), Kael (Mage - AoE/Control Kiter),
    /// Seraphina (Ranger - Sniper/Trap Kiter), and Arthur (Cleric - Healer/Support).
    /// - Never clumps or blindly tails the player during combat.
    /// - Only regroups around the player when the room is completely cleared of monsters.
    /// </summary>
    [DefaultExecutionOrder(-15)]
    [RequireComponent(typeof(ActorUnit))]
    public class HeroPartyAI : MonoBehaviour
    {
        public bool IsMinion = false;
        public PartyAIState CurrentState { get; private set; } = PartyAIState.RegroupOutOfCombat;

        private ActorUnit _self;
        private float _thinkTimer;
        private float _hazardReactionTimer;
        private float _strafeSign = 1f;
        private Vector3 _formationOffset;
        private float _preferredOrbitAngle;

        private void Awake()
        {
            _self = GetComponent<ActorUnit>();
            _strafeSign = Random.value > 0.5f ? 1f : -1f;
        }

        private void Start()
        {
            int slot = Mathf.Max(1, _self.PartySlotIndex);
            float angleDeg = slot * 105f + 25f;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            _formationOffset = new Vector3(Mathf.Cos(angleRad) * 2.8f, 0f, Mathf.Sin(angleRad) * 2.8f);
            _preferredOrbitAngle = (slot - 2f) * 52f; // Distinct angular sector for ranged/melee spread
        }

        private void Update()
        {
            if (_self == null || _self.IsDead || _self.IsDowned || !_self.CanAct())
            {
                if (_self != null) _self.StopMovingAnim();
                return;
            }

            _thinkTimer -= Time.deltaTime;
            if (_thinkTimer <= 0f)
            {
                _thinkTimer = Random.Range(0.28f, 0.52f);
                if (Random.value < 0.22f) _strafeSign = -_strafeSign;
            }

            // =====================================================================
            // PRIORITY 1 (UTILITY = 1.00): EVADE RED GROUND TELEGRAPH HAZARDS
            // =====================================================================
            if (TryEvadeGroundTelegraphs())
            {
                CurrentState = PartyAIState.EvadeTelegraph;
                return;
            }

            ActorUnit leader = FindLeader();

            // Teleport catch-up ONLY if stuck very far behind leader between rooms
            if (!IsMinion && leader != null && Vector3.Distance(transform.position, leader.transform.position) > 28f)
            {
                _self.SyncPhysicsPosition(leader.transform.position + _formationOffset);
                return;
            }

            // =====================================================================
            // PRIORITY 2 (UTILITY = 0.92): REVIVE DOWNED ALLIES & PARTY SUPPORT
            // =====================================================================
            if (!IsMinion)
            {
                ActorUnit downed = FindDownedAlly();
                if (downed != null)
                {
                    CurrentState = PartyAIState.ReviveAndSupport;
                    if (_self.HeroClass != null && _self.HeroClass.id == 9 && _self.SkillCds[3] <= 0f)
                    {
                        _self.TryUseSkill(3, downed.transform.position);
                        return;
                    }
                    float dDown = Vector3.Distance(transform.position, downed.transform.position);
                    if (dDown > 1.9f)
                    {
                        SteerWithSeparation(downed.transform.position, 1.4f, 1.0f);
                        return;
                    }
                    _self.StopMovingAnim();
                    return;
                }

                if (_self.HeroClass != null && TrySupportParty())
                {
                    CurrentState = PartyAIState.ReviveAndSupport;
                    return;
                }
            }

            // =====================================================================
            // PRIORITY 3: EVALUATE COMBAT vs OUT-OF-COMBAT REGROUP
            // =====================================================================
            ActorUnit target = SelectBestMonsterTarget(leader, out ActorUnit nearestThreat, out float nearestThreatDist);
            if (target == null)
            {
                // Room is clear of monsters -> Regroup around Player with Separation Steering
                CurrentState = PartyAIState.RegroupOutOfCombat;
                if (leader != null)
                {
                    Vector3 sep = ComputeAllySeparationForce(2.7f);
                    Vector3 goal = leader.transform.position + _formationOffset + sep * 1.4f;
                    float distToGoal = Vector3.Distance(transform.position, goal);
                    if (distToGoal > 1.75f)
                        SteerWithSeparation(goal, 2.5f, 1.0f);
                    else if (sep.sqrMagnitude > 0.18f)
                        SteerWithSeparation(transform.position + sep * 2.2f, 2.5f, 0.75f);
                    else
                        _self.StopMovingAnim();
                }
                return;
            }

            // =====================================================================
            // PRIORITY 4: ROLE-SPECIFIC COMBAT UTILITY & STATE EXECUTION
            // =====================================================================
            bool isTankRole = _self.HeroClass != null && (_self.HeroClass.role == Role.Tank || _self.HeroClass.id == 0 || _self.HeroClass.id == 1);
            if (isTankRole)
            {
                CurrentState = PartyAIState.FrontlineIntercept;
                ExecuteFrontlineTankAI(target, nearestThreat, leader);
            }
            else if (_self.IsRanged)
            {
                CurrentState = PartyAIState.RangedTacticalKite;
                ExecuteRangedKiteAI(target, nearestThreat, nearestThreatDist);
            }
            else
            {
                CurrentState = PartyAIState.MeleeAssassinate;
                ExecuteMeleeDpsAI(target);
            }
        }

        private bool TryEvadeGroundTelegraphs()
        {
            Vector3 combinedEscape = Vector3.zero;
            bool inDanger = false;
            bool imminentDanger = false;

            for (int i = 0; i < GroundTelegraphFx.ActiveHazards.Count; i++)
            {
                var hz = GroundTelegraphFx.ActiveHazards[i];
                if (hz == null) continue;
                Vector3 away = transform.position - hz.transform.position;
                away.y = 0f;
                float d = away.magnitude;
                float safeR = hz.HazardRadius + (_self.IsRanged ? 1.35f : 0.95f);
                if (d < safeR)
                {
                    inDanger = true;
                    if (d < hz.HazardRadius * 0.85f) imminentDanger = true;
                    Vector3 dir = d > 0.05f ? away.normalized : Quaternion.Euler(0f, _self.PartySlotIndex * 90f, 0f) * Vector3.forward;
                    float urgency = Mathf.Clamp01(1.25f - d / Mathf.Max(0.5f, safeR));
                    combinedEscape += dir * (1f + urgency * 2f);
                }
            }

            if (!inDanger)
            {
                _hazardReactionTimer = 0f;
                return false;
            }

            // Human-like reaction delay (0.42s) so AI companions reposition intelligently yet still take real HP damage
            // if caught in fast/overlapping Boss red AoEs instead of robotically dodging 100% of attacks.
            _hazardReactionTimer += Time.deltaTime;
            if (_hazardReactionTimer < 0.42f) return false;

            Vector3 escapeDir = FindBestClearDirection(transform.position, combinedEscape.normalized);
            if (imminentDanger && _self.DodgeCd <= 0f && Random.value < 0.45f && _self.TryDodge(escapeDir))
                return true;

            SteerInDirection(escapeDir, 0.92f);
            return true;
        }

        /// <summary>
        /// Lyra (Knight) & Guardian Frontline AI:
        /// Charges to the frontline between ranged allies and Elite/Boss threats, prioritizes Taunt
        /// and Guard-Breaking Stagger attacks (Shield Bash, Judgement, Heavy Attack).
        /// </summary>
        private void ExecuteFrontlineTankAI(ActorUnit target, ActorUnit nearestThreat, ActorUnit leader)
        {
            // If an untaunted monster is harassing a ranged teammate, intercept it immediately!
            ActorUnit priorityTarget = FindMonsterThreateningBackline() ?? target;
            Vector3 toTarget = priorityTarget.transform.position - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            Vector3 dirToTarget = dist > 0.01f ? toTarget / dist : transform.forward;

            int cid = _self.HeroClass != null ? _self.HeroClass.id : 0;
            Vector3 tp = priorityTarget.transform.position;

            // 1. Taunt Utility: Force Elites/Bosses or packs around teammates to focus Lyra
            bool targetAlreadyTaunted = priorityTarget.Statuses.Exists(s => s.type == StatusType.Taunted);
            int nearbyEnemies = CountMonstersNear(transform.position, 9.5f);
            if (cid == 0 && _self.SkillCds[2] <= 0f && dist <= 9.2f && (!targetAlreadyTaunted || nearbyEnemies >= 2))
            {
                if (_self.TryUseSkill(2, tp)) return; // Knight W/E: Taunt + DefUp
            }
            if (cid == 1 && _self.SkillCds[3] <= 0f && dist <= 10.5f && (!targetAlreadyTaunted || nearbyEnemies >= 2))
            {
                if (_self.TryUseSkill(3, tp)) return; // Guardian Ultimate: Citadel Taunt + 50% Shield
            }

            // 2. Stagger / Guard-Break Priority on Elite/Boss or high-Poise monsters
            bool needsStaggerBreak = (priorityTarget.IsElite || priorityTarget.IsMiniBoss || priorityTarget.IsBoss || priorityTarget.Poise > 10f)
                                  && !priorityTarget.IsStaggered;

            if (dist <= 5.0f && _self.SkillCds[3] <= 0f && (needsStaggerBreak || nearbyEnemies >= 2))
            {
                if (_self.TryUseSkill(3, tp)) return; // Judgement AoE Stun & Stagger Slam
            }
            if (dist <= 3.6f && _self.SkillCds[0] <= 0f)
            {
                if (_self.TryUseSkill(0, tp)) return; // Shield Bash / Shield Wave Stagger
            }
            if (cid == 0 && _self.SkillCds[1] <= 0f && (_self.Hp < _self.MaxHp * 0.85f || priorityTarget.IsBoss || priorityTarget.IsElite))
            {
                if (_self.TryUseSkill(1, tp)) return; // Iron Guard 30% HP Shield
            }

            // 3. Frontline Positioning & Heavy Attack Guard-Break
            float engageDist = _self.AttackRange - 0.15f;
            if (dist > engageDist)
            {
                // Charge directly toward the frontline side of the monster
                SteerWithSeparation(tp - dirToTarget * 1.1f, 1.65f, 1.05f);
            }
            else
            {
                // Maintain separation from other melee heroes while holding frontline
                Vector3 sep = ComputeAllySeparationForce(2.0f);
                if (sep.sqrMagnitude > 0.25f)
                {
                    SteerInDirection((sep + dirToTarget * 0.3f).normalized, 0.55f);
                }
                else
                {
                    _self.StopMovingAnim();
                }

                _self.FaceDirection(dirToTarget, 16f);

                // Prioritize Charged Heavy Attack [RMB/X] to shatter monster Poise / Stagger Bar!
                if (needsStaggerBreak && _self.HeavyAttackCd <= 0f)
                {
                    if (_self.TryHeavyAttack(dirToTarget, tp)) return;
                }
                _self.TryBasicAttack(dirToTarget, tp);
            }
        }

        /// <summary>
        /// Kael (Mage) & Seraphina (Ranger) Ranged Tactical Kite AI:
        /// - Keeps optimal sniper distance [6.0m .. 10.0m].
        /// - Actively kites backward/laterally when any monster closes within 6.0m.
        /// - Uses peel skills (Frost Nova, Frost Trap) when pressured.
        /// - Spreads into distinct angular sectors with Separation Steering so ranged allies never stack.
        /// </summary>
        private void ExecuteRangedKiteAI(ActorUnit target, ActorUnit nearestThreat, float nearestThreatDist)
        {
            const float OptMinDist = 6.0f;
            float optMaxDist = Mathf.Clamp(_self.AttackRange - 1.5f, 9.5f, 11.0f);

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            float distToTarget = toTarget.magnitude;
            Vector3 aimDir = distToTarget > 0.01f ? toTarget / distToTarget : transform.forward;

            ActorUnit kiteSource = (nearestThreat != null && nearestThreatDist < OptMinDist) ? nearestThreat : target;
            Vector3 awayFromThreat = transform.position - kiteSource.transform.position;
            awayFromThreat.y = 0f;
            float threatDist = awayFromThreat.magnitude;
            Vector3 retreatDir = threatDist > 0.05f ? awayFromThreat / threatDist : -aimDir;

            // 1. Emergency Peel / Dodge if a monster is right in our face (< 3.4m)
            if (threatDist < 3.4f && _self.HeroClass != null)
            {
                int cid = _self.HeroClass.id;
                // Kael (Mage) casts W: Frost Nova to freeze pursuing melee enemies
                if (cid == 5 && _self.SkillCds[1] <= 0f && _self.TryUseSkill(1, transform.position))
                    return;
                // Seraphina (Ranger) drops W: Frost Trap at her feet while kiting away
                if (cid == 4 && _self.SkillCds[1] <= 0f && _self.TryUseSkill(1, kiteSource.transform.position))
                    return;

                Vector3 dodgeVec = FindBestClearDirection(transform.position, (retreatDir + ComputeAllySeparationForce(3.0f) * 0.6f).normalized);
                if (threatDist < 2.8f && _self.TryDodge(dodgeVec))
                    return;
            }

            // 2. Cast Ranged Offensive Skills (Fireball, Arcane Pulse, Meteor, Piercing Shot, Volley, Arrow Storm)
            if (!IsMinion && _self.HeroClass != null && threatDist >= 2.8f)
            {
                TryOffensiveSkills(target, distToTarget);
            }

            // 3. Kite / Reposition / Orbit inside [6m .. 10m] ring with Separation Steering
            Vector3 allySep = ComputeAllySeparationForce(3.1f);
            Vector3 tangent = Vector3.Cross(Vector3.up, aimDir) * _strafeSign;

            if (threatDist < OptMinDist)
            {
                // ACTIVE KITE: Retreat away from approaching enemy + slight lateral arc + ally separation
                Vector3 desiredKite = (retreatDir * 1.35f + tangent * 0.42f + allySep * 0.85f).normalized;
                Vector3 clearKite = FindBestClearDirection(transform.position, desiredKite);
                SteerInDirection(clearKite, 1.0f);

                // Stutter-step shot if cooldown is ready while kiting
                if (threatDist >= 3.6f && distToTarget <= _self.AttackRange && _self.BasicAttackCd <= 0f)
                {
                    _self.FaceDirection(aimDir, 20f);
                    _self.TryBasicAttack(aimDir, target.transform.position);
                }
            }
            else if (distToTarget > optMaxDist)
            {
                // Advance to optimal 8.5m sniper ring around target at our assigned sector angle
                Vector3 sectorDir = Quaternion.Euler(0f, _preferredOrbitAngle, 0f) * (-aimDir);
                Vector3 sniperSpot = target.transform.position + sectorDir.normalized * 8.2f + allySep * 1.5f;
                SteerWithSeparation(sniperSpot, 3.0f, 0.95f);
            }
            else
            {
                // Inside optimal [6m .. 10m] sniper zone: maintain ally separation & gentle tactical strafe
                Vector3 moveVec = tangent * 0.38f + allySep * 1.15f;
                if (moveVec.sqrMagnitude > 0.08f)
                {
                    Vector3 clearStrafe = FindBestClearDirection(transform.position, moveVec.normalized);
                    _self.FaceDirection(aimDir, 18f);
                    _self.MoveWorld(clearStrafe * (_self.EffectiveMoveSpeed() * Mathf.Clamp(moveVec.magnitude, 0.35f, 0.80f) * Time.deltaTime));
                }
                else
                {
                    _self.StopMovingAnim();
                    _self.FaceDirection(aimDir, 18f);
                }

                // Weave Heavy Piercing Shot on Elite/Boss or staggered targets, otherwise Basic Attack
                if ((target.IsStaggered || target.IsElite || target.IsBoss) && _self.HeavyAttackCd <= 0f && Random.value < 0.35f)
                    _self.TryHeavyAttack(aimDir, target.transform.position);
                else
                    _self.TryBasicAttack(aimDir, target.transform.position);
            }
        }

        private void ExecuteMeleeDpsAI(ActorUnit target)
        {
            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            Vector3 dir = dist > 0.01f ? toTarget / dist : transform.forward;

            if (!IsMinion && _self.HeroClass != null)
                TryOffensiveSkills(target, dist);

            if (dist > _self.AttackRange - 0.2f)
            {
                // Flank slightly around target using slot sector so melee units don't stack on the Tank
                Vector3 flankOffset = Quaternion.Euler(0f, _preferredOrbitAngle, 0f) * (-dir) * 1.2f;
                SteerWithSeparation(target.transform.position + flankOffset, 2.0f, 1.0f);
            }
            else
            {
                Vector3 sep = ComputeAllySeparationForce(1.9f);
                if (sep.sqrMagnitude > 0.22f)
                    SteerInDirection((sep + dir * 0.35f).normalized, 0.55f);
                else
                    _self.StopMovingAnim();

                _self.FaceDirection(dir, 16f);
                if (_self.HeavyAttackCd <= 0f && (target.IsElite || target.IsBoss || target.Poise > 15f))
                    _self.TryHeavyAttack(dir, target.transform.position);
                else
                    _self.TryBasicAttack(dir, target.transform.position);
            }
        }

        private bool TrySupportParty()
        {
            ActorUnit weakest = _self.FindWeakestAlly(18f);
            float weakPct = weakest != null ? weakest.Hp / Mathf.Max(1f, weakest.MaxHp) : 1f;
            bool anyDebuffed = false;
            for (int i = 0; i < ActorUnit.AllUnits.Count; i++)
            {
                var u = ActorUnit.AllUnits[i];
                if (u != null && u.Team == Team.Hero && !u.IsDead && !u.IsDowned && u.Statuses.Exists(s => GameDB.IsDebuff(s.type)))
                {
                    anyDebuffed = true;
                    break;
                }
            }

            int cid = _self.HeroClass.id;
            if (cid == 9) // Cleric (Arthur): tactical healing when allies drop below 62% HP so combat damage sticks
            {
                if (weakPct < 0.62f && _self.TryUseSkill(1, transform.position)) return true; // Group Heal
                if ((anyDebuffed || weakPct < 0.72f) && _self.TryUseSkill(2, transform.position)) return true; // Purify + Shield
            }
            else if (cid == 8) // Druid
            {
                if (weakPct < 0.64f && weakest != null && _self.TryUseSkill(1, weakest.transform.position)) return true; // Regrowth
                if (weakPct < 0.48f && _self.TryUseSkill(3, transform.position)) return true; // Nature's Wrath
            }
            else if (cid == 7) // Monk
            {
                if (weakPct < 0.58f && _self.TryUseSkill(1, transform.position)) return true; // Zen Mend
            }
            else if (cid == 1) // Guardian
            {
                if (weakPct < 0.72f && _self.TryUseSkill(1, transform.position)) return true; // Sanctuary party shield
                if (weakPct < 0.66f && _self.TryUseSkill(2, transform.position)) return true; // Aegis def up
            }
            return false;
        }

        private void TryOffensiveSkills(ActorUnit target, float dist)
        {
            int cid = _self.HeroClass.id;
            Vector3 tp = target.transform.position;

            for (int s = 3; s >= 0; s--)
            {
                SkillDef sk = _self.HeroClass.skills[s];
                if (_self.SkillCds[s] > 0f) continue;
                if (sk.kind == SkillKind.HealAll || sk.kind == SkillKind.HealTarget || sk.kind == SkillKind.Revive || sk.kind == SkillKind.Cleanse)
                    continue;
                if (sk.kind == SkillKind.Transform && _self.Hp > _self.MaxHp * 0.55f) continue;
                // Keep Mage Frost Nova (slot 1) reserved for close-range peel unless enemy is within 5.2m
                if (cid == 5 && s == 1 && dist > 5.2f) continue;

                float maxUseDist = (sk.kind == SkillKind.MeleeArc || sk.kind == SkillKind.SelfAoE)
                    ? sk.radius + 0.5f
                    : Mathf.Max(8.5f, sk.range);
                if (dist <= maxUseDist)
                {
                    if (_self.TryUseSkill(s, tp)) return;
                }
            }
        }

        private ActorUnit FindLeader()
        {
            for (int i = 0; i < ActorUnit.AllUnits.Count; i++)
            {
                var u = ActorUnit.AllUnits[i];
                if (u != null && u.IsLocalPlayer && !u.IsDead) return u;
            }
            for (int i = 0; i < ActorUnit.AllUnits.Count; i++)
            {
                var u = ActorUnit.AllUnits[i];
                if (u != null && u.Team == Team.Hero && !u.IsSummon && !u.IsDead && u != _self) return u;
            }
            return null;
        }

        private ActorUnit FindDownedAlly()
        {
            for (int i = 0; i < ActorUnit.AllUnits.Count; i++)
            {
                var u = ActorUnit.AllUnits[i];
                if (u != null && u != _self && u.Team == Team.Hero && u.IsDowned && !u.IsDead) return u;
            }
            return null;
        }

        private ActorUnit FindMonsterThreateningBackline()
        {
            ActorUnit best = null;
            float bestThreatScore = 0f;
            for (int i = 0; i < ActorUnit.AllUnits.Count; i++)
            {
                var m = ActorUnit.AllUnits[i];
                if (m == null || m.Team != Team.Monster || m.IsDead) continue;
                if (Vector3.Distance(transform.position, m.transform.position) > 22f) continue;

                float threat = 0f;
                if (m.IsBoss) threat += 35f;
                else if (m.IsMiniBoss) threat += 25f;
                else if (m.IsElite) threat += 18f;

                // Check if monster is close to any ranged ally or the player
                for (int j = 0; j < ActorUnit.AllUnits.Count; j++)
                {
                    var ally = ActorUnit.AllUnits[j];
                    if (ally == null || ally == _self || ally.Team != Team.Hero || ally.IsDead) continue;
                    float dAlly = Vector3.Distance(m.transform.position, ally.transform.position);
                    if (dAlly < 5.5f && (ally.IsRanged || ally.IsLocalPlayer))
                        threat += (6.0f - dAlly) * 6f;
                }

                if (!m.Statuses.Exists(s => s.type == StatusType.Taunted))
                    threat += 8f;

                if (threat > bestThreatScore)
                {
                    bestThreatScore = threat;
                    best = m;
                }
            }
            return best;
        }

        private int CountMonstersNear(Vector3 center, float radius)
        {
            int count = 0;
            for (int i = 0; i < ActorUnit.AllUnits.Count; i++)
            {
                var u = ActorUnit.AllUnits[i];
                if (u != null && u.Team == Team.Monster && !u.IsDead && Vector3.Distance(center, u.transform.position) <= radius)
                    count++;
            }
            return count;
        }

        private ActorUnit SelectBestMonsterTarget(ActorUnit leader, out ActorUnit nearestThreat, out float nearestThreatDist)
        {
            nearestThreat = null;
            nearestThreatDist = float.MaxValue;

            Vector3 anchor = leader != null ? leader.transform.position : transform.position;
            ActorUnit best = null;
            float bestScore = float.MaxValue;
            bool isTank = _self.HeroClass != null && _self.HeroClass.role == Role.Tank;

            for (int i = 0; i < ActorUnit.AllUnits.Count; i++)
            {
                var u = ActorUnit.AllUnits[i];
                if (u == null || u.Team != Team.Monster || u.IsDead) continue;
                float distToAnchor = Vector3.Distance(anchor, u.transform.position);
                if (distToAnchor > 26f) continue;

                float d = Vector3.Distance(transform.position, u.transform.position);
                if (d < nearestThreatDist)
                {
                    nearestThreatDist = d;
                    nearestThreat = u;
                }

                float score = d;
                if (isTank)
                {
                    // Lyra (Knight) prioritizes Boss / MiniBoss / Elite & unbroken-poise frontliners
                    if (u.IsBoss) score -= 12f;
                    else if (u.IsMiniBoss) score -= 9f;
                    else if (u.IsElite) score -= 7f;
                    if (!u.IsStaggered && u.Poise > 0f) score -= 2.5f;
                }
                else if (_self.IsRanged)
                {
                    // Kael & Seraphina prefer targets near their sweet spot [6m..10m] or staggered priority targets
                    if (u.IsStaggered) score -= 5f;
                    if (d >= 5.5f && d <= 11.5f) score -= 3f;
                }
                else if (_self.HeroClass != null && _self.HeroClass.id == 3 && u.IsRanged)
                {
                    score -= 5f;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    best = u;
                }
            }
            return best;
        }

        /// <summary>
        /// Computes inverse-square repulsive Separation Steering vector away from nearby friendly units
        /// so teammates never overlap, stack, or form a single-file train.
        /// </summary>
        private Vector3 ComputeAllySeparationForce(float desiredSpacing)
        {
            Vector3 force = Vector3.zero;
            Vector3 myPos = transform.position;
            for (int i = 0; i < ActorUnit.AllUnits.Count; i++)
            {
                var other = ActorUnit.AllUnits[i];
                if (other == null || other == _self || other.Team != Team.Hero || other.IsDead) continue;
                Vector3 diff = myPos - other.transform.position;
                diff.y = 0f;
                float d = diff.magnitude;
                if (d < desiredSpacing)
                {
                    Vector3 pushDir = d > 0.02f
                        ? diff / d
                        : Quaternion.Euler(0f, _self.PartySlotIndex * 90f, 0f) * Vector3.right;
                    float strength = 1f - (d / desiredSpacing);
                    force += pushDir * (strength * strength * 1.85f);
                }
            }
            return force;
        }

        private void SteerWithSeparation(Vector3 goal, float allySeparationRadius, float speedMult = 1.0f)
        {
            Vector3 toGoal = goal - transform.position;
            toGoal.y = 0f;
            Vector3 sep = ComputeAllySeparationForce(allySeparationRadius);
            Vector3 combined = toGoal.normalized + sep * 0.85f;
            if (toGoal.sqrMagnitude < 0.08f && sep.sqrMagnitude < 0.08f)
            {
                _self.StopMovingAnim();
                return;
            }
            Vector3 clearDir = FindBestClearDirection(transform.position, combined.normalized);
            SteerInDirection(clearDir, speedMult);
        }

        private void SteerInDirection(Vector3 dir, float speedMult = 1.0f)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f)
            {
                _self.StopMovingAnim();
                return;
            }
            dir.Normalize();
            _self.FaceDirection(dir, 15f);
            _self.MoveWorld(dir * (_self.EffectiveMoveSpeed() * speedMult * Time.deltaTime));
        }

        public static Vector3 FindBestClearDirection(Vector3 pos, Vector3 desiredDir)
        {
            desiredDir.y = 0f;
            if (desiredDir.sqrMagnitude < 0.001f) return Vector3.forward;
            desiredDir.Normalize();

            Vector3 origin = pos + Vector3.up * 0.7f;
            if (!IsWallBlocked(origin, desiredDir, 1.75f))
                return desiredDir;

            float[] testAngles = { -35f, 35f, -70f, 70f, -110f, 110f, -150f, 150f };
            for (int i = 0; i < testAngles.Length; i++)
            {
                Vector3 candidate = Quaternion.Euler(0f, testAngles[i], 0f) * desiredDir;
                if (!IsWallBlocked(origin, candidate, 1.85f))
                    return candidate.normalized;
            }
            return -desiredDir;
        }

        private static bool IsWallBlocked(Vector3 origin, Vector3 dir, float checkDist)
        {
            if (Physics.Raycast(origin, dir, out RaycastHit hit, checkDist, 1 << 0, QueryTriggerInteraction.Ignore))
            {
                return hit.collider.GetComponentInParent<ActorUnit>() == null;
            }
            return false;
        }

        public static Vector3 ObstacleAvoidDir(Vector3 pos, Vector3 desiredDir)
        {
            return FindBestClearDirection(pos, desiredDir);
        }
    }
}



using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Role-aware AI for the 3-4 Hunter party members (Tank, Healer, Ranged DPS, Melee DPS, Hybrid, and Summons).
    /// Coordinates taunts, heals, shields, revives, kiting, and dodging Boss AoE telegraphs.
    /// </summary>
    [RequireComponent(typeof(ActorUnit))]
    public class HeroPartyAI : MonoBehaviour
    {
        public bool IsMinion = false;
        private ActorUnit _self;
        private float _thinkTimer;
        private float _strafeSign = 1f;
        private Vector3 _formationOffset;

        private void Awake()
        {
            _self = GetComponent<ActorUnit>();
            _strafeSign = Random.value > 0.5f ? 1f : -1f;
        }

        private void Start()
        {
            float angle = (_self.PartySlotIndex * 95f + 40f) * Mathf.Deg2Rad;
            _formationOffset = new Vector3(Mathf.Cos(angle) * 2.6f, 0f, Mathf.Sin(angle) * 2.6f);
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
                _thinkTimer = Random.Range(0.35f, 0.65f);
                if (Random.value < 0.25f) _strafeSign = -_strafeSign;
            }

            // 1. Dodge active ground hazard telegraphs!
            foreach (var hz in GroundTelegraphFx.ActiveHazards)
            {
                if (hz == null) continue;
                Vector3 away = transform.position - hz.transform.position;
                away.y = 0f;
                if (away.magnitude < hz.HazardRadius + 0.8f)
                {
                    if (away.sqrMagnitude < 0.01f) away = transform.right;
                    if (!_self.TryDodge(away.normalized))
                        SteerToward(transform.position + away.normalized * 4f);
                    return;
                }
            }

            ActorUnit leader = FindLeader();

            // Teleport catch-up if stuck far behind leader between rooms
            if (!IsMinion && leader != null && Vector3.Distance(transform.position, leader.transform.position) > 25f)
            {
                if (_self.Controller != null) _self.Controller.enabled = false;
                transform.position = leader.transform.position + _formationOffset;
                if (_self.Controller != null) _self.Controller.enabled = true;
                return;
            }

            // 2. Revive downed ally priority!
            if (!IsMinion)
            {
                ActorUnit downed = FindDownedAlly();
                if (downed != null)
                {
                    // Cleric uses R: Resurrection immediately if ready
                    if (_self.HeroClass != null && _self.HeroClass.id == 9 && _self.SkillCds[3] <= 0f)
                    {
                        _self.TryUseSkill(3, downed.transform.position);
                        return;
                    }
                    float dDown = Vector3.Distance(transform.position, downed.transform.position);
                    if (dDown > 2.0f)
                    {
                        SteerToward(downed.transform.position);
                        return;
                    }
                    _self.StopMovingAnim();
                    return;
                }
            }

            // 3. Support / Healing logic before offensive skills
            if (!IsMinion && _self.HeroClass != null && TrySupportParty())
                return;

            // 4. Find combat target
            ActorUnit target = SelectBestMonsterTarget(leader);
            if (target == null)
            {
                // Follow leader in formation
                if (leader != null)
                {
                    Vector3 goal = leader.transform.position + _formationOffset;
                    if (Vector3.Distance(transform.position, goal) > 1.8f)
                        SteerToward(goal);
                    else
                        _self.StopMovingAnim();
                }
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            // 5. Offensive / tactical skills
            if (!IsMinion && _self.HeroClass != null)
            {
                TryOffensiveSkills(target, dist);
            }

            // 6. Positioning & basic attack
            if (_self.IsRanged)
            {
                float prefMin = 6.5f, prefMax = Mathf.Min(12.5f, _self.AttackRange - 1f);
                if (dist < prefMin)
                {
                    SteerToward(transform.position - toTarget.normalized * 4f);
                }
                else if (dist > prefMax)
                {
                    SteerToward(target.transform.position);
                }
                else
                {
                    Vector3 strafe = Vector3.Cross(Vector3.up, toTarget.normalized) * _strafeSign;
                    _self.FaceDirection(toTarget);
                    _self.MoveWorld(strafe * (_self.EffectiveMoveSpeed() * 0.45f * Time.deltaTime));
                }

                if (dist <= _self.AttackRange)
                    _self.TryBasicAttack(toTarget.normalized, target.transform.position);
            }
            else
            {
                if (dist > _self.AttackRange - 0.25f)
                {
                    SteerToward(target.transform.position);
                }
                else
                {
                    _self.StopMovingAnim();
                    _self.TryBasicAttack(toTarget.normalized, target.transform.position);
                }
            }
        }

        private bool TrySupportParty()
        {
            ActorUnit weakest = _self.FindWeakestAlly(16f);
            float weakPct = weakest != null ? weakest.Hp / Mathf.Max(1f, weakest.MaxHp) : 1f;
            bool anyDebuffed = false;
            foreach (var u in ActorUnit.AllUnits)
                if (u.Team == Team.Hero && !u.IsDead && !u.IsDowned && u.Statuses.Exists(s => GameDB.IsDebuff(s.type)))
                    anyDebuffed = true;

            int cid = _self.HeroClass.id;
            if (cid == 9) // Cleric
            {
                if (weakPct < 0.72f && _self.TryUseSkill(1, transform.position)) return true; // Group Heal
                if ((anyDebuffed || weakPct < 0.82f) && _self.TryUseSkill(2, transform.position)) return true; // Purify + Shield
            }
            else if (cid == 8) // Druid
            {
                if (weakPct < 0.75f && _self.TryUseSkill(1, weakest.transform.position)) return true; // Regrowth
                if (weakPct < 0.55f && _self.TryUseSkill(3, transform.position)) return true; // Nature's Wrath
            }
            else if (cid == 7) // Monk
            {
                if (weakPct < 0.68f && _self.TryUseSkill(1, transform.position)) return true; // Zen Mend
            }
            else if (cid == 1) // Guardian
            {
                if (weakPct < 0.85f && _self.TryUseSkill(1, transform.position)) return true; // Sanctuary party shield
                if (weakPct < 0.78f && _self.TryUseSkill(2, transform.position)) return true; // Aegis def up
            }
            return false;
        }

        private void TryOffensiveSkills(ActorUnit target, float dist)
        {
            int cid = _self.HeroClass.id;
            Vector3 tp = target.transform.position;

            if (cid == 0 || cid == 1) // Tanks: Taunt & Shield & Slam
            {
                if (dist <= 8f && _self.SkillCds[2] <= 0f && cid == 0) { _self.TryUseSkill(2, tp); return; }
                if (dist <= 9f && _self.SkillCds[3] <= 0f) { _self.TryUseSkill(3, tp); return; }
                if (_self.Hp < _self.MaxHp * 0.8f && cid == 0 && _self.SkillCds[1] <= 0f) { _self.TryUseSkill(1, tp); return; }
                if (dist <= 3.8f && _self.SkillCds[0] <= 0f) { _self.TryUseSkill(0, tp); return; }
            }
            else
            {
                // Cast Q/W/E/R opportunistically when in range
                for (int s = 3; s >= 0; s--)
                {
                    SkillDef sk = _self.HeroClass.skills[s];
                    if (_self.SkillCds[s] > 0f) continue;
                    if (sk.kind == SkillKind.HealAll || sk.kind == SkillKind.HealTarget || sk.kind == SkillKind.Revive || sk.kind == SkillKind.Cleanse)
                        continue;
                    if (sk.kind == SkillKind.Transform && _self.Hp > _self.MaxHp * 0.55f) continue;

                    float maxUseDist = (sk.kind == SkillKind.MeleeArc || sk.kind == SkillKind.SelfAoE) ? sk.radius + 0.5f : Mathf.Max(8f, sk.range);
                    if (dist <= maxUseDist)
                    {
                        if (_self.TryUseSkill(s, tp)) return;
                    }
                }
            }
        }

        private ActorUnit FindLeader()
        {
            foreach (var u in ActorUnit.AllUnits)
                if (u.IsLocalPlayer && !u.IsDead) return u;
            foreach (var u in ActorUnit.AllUnits)
                if (u.Team == Team.Hero && !u.IsSummon && !u.IsDead && u != _self) return u;
            return null;
        }

        private ActorUnit FindDownedAlly()
        {
            foreach (var u in ActorUnit.AllUnits)
                if (u != _self && u.Team == Team.Hero && u.IsDowned && !u.IsDead) return u;
            return null;
        }

        private ActorUnit SelectBestMonsterTarget(ActorUnit leader)
        {
            Vector3 anchor = leader != null ? leader.transform.position : transform.position;
            ActorUnit best = null;
            float bestScore = float.MaxValue;

            foreach (var u in ActorUnit.AllUnits)
            {
                if (u == null || u.Team != Team.Monster || u.IsDead) continue;
                float distToAnchor = Vector3.Distance(anchor, u.transform.position);
                if (distToAnchor > 22f) continue;

                float d = Vector3.Distance(transform.position, u.transform.position);
                float score = d;
                // Assassins prioritize low-HP ranged/mages; Tanks prioritize Boss/Elites
                if (_self.HeroClass != null && _self.HeroClass.id == 3 && u.IsRanged) score -= 5f;
                if (_self.HeroClass != null && (_self.HeroClass.role == Role.Tank) && (u.IsElite || u.IsBoss)) score -= 4f;

                if (score < bestScore) { bestScore = score; best = u; }
            }
            return best;
        }

        private void SteerToward(Vector3 goal)
        {
            Vector3 to = goal - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.05f) { _self.StopMovingAnim(); return; }
            Vector3 dir = ObstacleAvoidDir(transform.position, to.normalized);
            _self.FaceDirection(dir, 14f);
            _self.MoveWorld(dir * (_self.EffectiveMoveSpeed() * Time.deltaTime));
        }

        public static Vector3 ObstacleAvoidDir(Vector3 pos, Vector3 desiredDir)
        {
            Vector3 origin = pos + Vector3.up * 0.7f;
            if (!Physics.Raycast(origin, desiredDir, out RaycastHit hit, 1.5f, 1 << 0, QueryTriggerInteraction.Ignore)
                || hit.collider.GetComponentInParent<ActorUnit>() != null)
                return desiredDir;

            Vector3 left = Quaternion.Euler(0f, -48f, 0f) * desiredDir;
            Vector3 right = Quaternion.Euler(0f, 48f, 0f) * desiredDir;
            bool lClear = !Physics.Raycast(origin, left, 1.6f, 1 << 0, QueryTriggerInteraction.Ignore);
            bool rClear = !Physics.Raycast(origin, right, 1.6f, 1 << 0, QueryTriggerInteraction.Ignore);
            if (lClear && !rClear) return left;
            if (rClear && !lClear) return right;
            return (left + Vector3.Cross(Vector3.up, hit.normal)).normalized;
        }
    }
}


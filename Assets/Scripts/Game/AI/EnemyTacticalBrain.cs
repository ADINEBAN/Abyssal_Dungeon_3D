using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssGame
{
    // ==================== LIGHTWEIGHT BEHAVIOR TREE + UTILITY AI RUNTIME ====================
    // Applied from .agents/skills/ai-behavior-trees-utility-ai/SKILL.md & game-ai/SKILL.md

    public enum BTStatus
    {
        Success,
        Failure,
        Running
    }

    public abstract class BTNode
    {
        public abstract BTStatus Tick(EnemyBlackboard bb, float dt);
    }

    public sealed class BTCondition : BTNode
    {
        private readonly Func<EnemyBlackboard, bool> _pred;
        public BTCondition(Func<EnemyBlackboard, bool> pred) { _pred = pred; }
        public override BTStatus Tick(EnemyBlackboard bb, float dt) => _pred(bb) ? BTStatus.Success : BTStatus.Failure;
    }

    public sealed class BTAction : BTNode
    {
        private readonly Func<EnemyBlackboard, float, BTStatus> _act;
        public BTAction(Func<EnemyBlackboard, float, BTStatus> act) { _act = act; }
        public override BTStatus Tick(EnemyBlackboard bb, float dt) => _act(bb, dt);
    }

    public sealed class BTSequence : BTNode
    {
        private readonly BTNode[] _children;
        public BTSequence(params BTNode[] children) { _children = children; }

        public override BTStatus Tick(EnemyBlackboard bb, float dt)
        {
            for (int i = 0; i < _children.Length; i++)
            {
                BTStatus s = _children[i].Tick(bb, dt);
                if (s != BTStatus.Success) return s;
            }
            return BTStatus.Success;
        }
    }

    public sealed class BTSelector : BTNode
    {
        private readonly BTNode[] _children;
        public BTSelector(params BTNode[] children) { _children = children; }

        public override BTStatus Tick(EnemyBlackboard bb, float dt)
        {
            for (int i = 0; i < _children.Length; i++)
            {
                BTStatus s = _children[i].Tick(bb, dt);
                if (s != BTStatus.Failure) return s;
            }
            return BTStatus.Failure;
        }
    }

    /// <summary>
    /// Normalized 0..1 response curves for Utility AI target & action scoring.
    /// </summary>
    public static class UtilityCurves
    {
        public static float Linear01(float x, float min, float max)
        {
            if (Mathf.Abs(max - min) < 1e-5f) return 0f;
            return Mathf.Clamp01((x - min) / (max - min));
        }

        public static float InverseLinear01(float x, float min, float max) => 1f - Linear01(x, min, max);

        public static float Quadratic01(float x, float min, float max, float exp = 2f)
        {
            return Mathf.Pow(Linear01(x, min, max), exp);
        }

        public static float Sigmoid01(float x, float midpoint, float steepness = 5f)
        {
            return 1f / (1f + Mathf.Exp(-steepness * (x - midpoint)));
        }
    }

    public class EnemyBlackboard
    {
        public ActorUnit Self;
        public MonsterAI Controller;
        public ActorUnit Target;
        public ActorUnit ProtectedVipAlly;
        public Vector3 TacticalDestination;
        public int AssignedSurroundSlot = -1;
        public EliteAffixKind EliteAffix = EliteAffixKind.None;
        public float AffixPulseTimer = 4f;
        public float BackstabBonusReadyTimer = 0f;
    }

    /// <summary>
    /// Phase 5 (Section 5 & GDD 3.3, 3.4):
    /// Hybrid Behavior Tree + Utility AI Tactical Coordinator for the 10 Enemy types:
    /// - Coordinates angular surround slots so enemies never stack on the same point.
    /// - Archer / Mage: Maintains optimal range band, kites & strafes away when approached.
    /// - Assassin (Goblin / Demon): Utility-scores weak/isolated heroes, paths to rear flank for Backstab (+25% dmg), retreats when endangered.
    /// - Tank (Skeleton Warrior / Dark Knight / Crystal Golem): Intercepts heroes threatening Elite/Mage allies and grants a Guard Link shield.
    /// - Elite Affixes: Vampiric, Molten, Stormcaller, FrozenPulse.
    /// </summary>
    public static class EnemyTacticalBrain
    {
        private static readonly Dictionary<ActorUnit, int> _slotAssignments = new Dictionary<ActorUnit, int>();

        public static EliteAffixKind RollEliteAffix(EnemyDef def, bool isElite)
        {
            if (!isElite || def == null) return EliteAffixKind.None;
            if (def.id == "DarkKnight") return EliteAffixKind.Vampiric;
            if (def.id == "Demon") return EliteAffixKind.Molten;
            if (def.id == "CrystalGolem") return EliteAffixKind.FrozenPulse;
            if (def.id == "SkeletonMage") return EliteAffixKind.Stormcaller;
            int r = UnityEngine.Random.Range(1, 5);
            return (EliteAffixKind)r;
        }

        public static string GetAffixLabel(EliteAffixKind affix)
        {
            switch (affix)
            {
                case EliteAffixKind.Vampiric: return "Huyết Thực (Hút Máu)";
                case EliteAffixKind.Molten: return "Dung Nham (Nổ Lửa)";
                case EliteAffixKind.Stormcaller: return "Lôi Đình (Giật Sét)";
                case EliteAffixKind.FrozenPulse: return "Hàn Băng (Đóng Băng)";
                default: return "";
            }
        }

        public static Color GetAffixColor(EliteAffixKind affix, Color fallback)
        {
            switch (affix)
            {
                case EliteAffixKind.Vampiric: return new Color(1.0f, 0.16f, 0.28f);
                case EliteAffixKind.Molten: return new Color(1.0f, 0.48f, 0.12f);
                case EliteAffixKind.Stormcaller: return new Color(0.32f, 0.78f, 1.0f);
                case EliteAffixKind.FrozenPulse: return new Color(0.45f, 0.95f, 1.0f);
                default: return fallback;
            }
        }

        /// <summary>
        /// Utility AI Target Selector: evaluates every living Hero using normalized 0..1 considerations
        /// tailored to the monster's tactical archetype (Assassin, Tank, Archer/Mage, Melee) with hysteresis.
        /// </summary>
        public static ActorUnit EvaluateBestTarget(ActorUnit self, ActorUnit currentTarget, out ActorUnit vipToGuard)
        {
            vipToGuard = null;
            if (self == null || self.IsDead) return null;

            // 1. Hard Taunt override from Knight / Guardian / Druid Bear
            if (self.TauntTarget != null && !self.TauntTarget.IsDead && !self.TauntTarget.IsDowned)
                return self.TauntTarget;

            Archetype arch = self.MonsterDef != null ? self.MonsterDef.archetype : Archetype.Melee;
            float detectRadius = (self.MonsterDef != null ? self.MonsterDef.detect : 15f) + 7f;

            // Compute party centroid for Assassin isolation scoring
            Vector3 partyCenter = Vector3.zero;
            int heroCount = 0;
            for (int i = 0; i < ActorUnit.AllUnits.Count; i++)
            {
                var u = ActorUnit.AllUnits[i];
                if (u != null && u.Team == Team.Hero && !u.IsDead && !u.IsDowned && !u.IsSummon)
                {
                    partyCenter += u.transform.position;
                    heroCount++;
                }
            }
            if (heroCount > 0) partyCenter /= heroCount;

            if (arch == Archetype.Tank)
            {
                vipToGuard = FindHighValueMonsterAlly(self);
            }

            ActorUnit bestHero = null;
            float highestUtility = -1f;

            for (int i = 0; i < ActorUnit.AllUnits.Count; i++)
            {
                var h = ActorUnit.AllUnits[i];
                if (h == null || h.Team != Team.Hero || h.IsDead || h.IsDowned) continue;

                float dist = Vector3.Distance(self.transform.position, h.transform.position);
                if (dist > detectRadius) continue;

                float proximityScore = UtilityCurves.InverseLinear01(dist, 1.5f, detectRadius);
                float lowHpScore = UtilityCurves.InverseLinear01(h.Hp / Mathf.Max(1f, h.MaxHp), 0.1f, 1.0f);
                float isolationDist = Vector3.Distance(h.transform.position, partyCenter);
                float isolationScore = UtilityCurves.Linear01(isolationDist, 1.5f, 10.0f);

                float utility = proximityScore * 0.65f + lowHpScore * 0.35f;

                if (arch == Archetype.Assassin)
                {
                    // Assassin prioritizes weak HP OR isolated backline targets (Mage/Ranger/Cleric)
                    bool isSquishyBackline = h.HeroClass != null && (h.HeroClass.role == Role.Healer || h.HeroClass.role == Role.RangedDPS);
                    utility = lowHpScore * 0.45f + isolationScore * 0.35f + proximityScore * 0.20f + (isSquishyBackline ? 0.22f : 0f);
                }
                else if (arch == Archetype.Tank && vipToGuard != null)
                {
                    // Tank intercepts whichever hero is closest to the VIP Elite/Mage ally
                    float distToVip = Vector3.Distance(vipToGuard.transform.position, h.transform.position);
                    float vipThreatScore = UtilityCurves.InverseLinear01(distToVip, 1.0f, 14.0f);
                    utility = vipThreatScore * 0.75f + proximityScore * 0.25f;
                }
                else if (arch == Archetype.Ranged || arch == Archetype.Mage)
                {
                    // Ranged prefers targets in medium range without having to run past frontline tanks
                    float idealBand = 1f - Mathf.Abs(dist - 8.5f) / 10f;
                    utility = Mathf.Clamp01(idealBand) * 0.55f + lowHpScore * 0.45f;
                }

                // Hysteresis (+10% stickiness to current target to prevent jittery target flipping)
                if (h == currentTarget) utility += 0.10f;

                if (utility > highestUtility)
                {
                    highestUtility = utility;
                    bestHero = h;
                }
            }

            return bestHero;
        }

        public static ActorUnit FindHighValueMonsterAlly(ActorUnit self)
        {
            ActorUnit bestVip = null;
            float bestPriority = -1f;

            for (int i = 0; i < ActorUnit.AllUnits.Count; i++)
            {
                var m = ActorUnit.AllUnits[i];
                if (m == null || m == self || m.Team != Team.Monster || m.IsDead) continue;
                float d = Vector3.Distance(self.transform.position, m.transform.position);
                if (d > 16f) continue;

                float prio = 0f;
                if (m.IsElite || m.IsMiniBoss) prio = 3.0f;
                else if (m.MonsterDef != null && (m.MonsterDef.archetype == Archetype.Mage || m.MonsterDef.archetype == Archetype.Ranged)) prio = 2.0f;

                if (prio > 0f)
                {
                    prio += (1f - Mathf.Clamp01(d / 16f));
                    if (prio > bestPriority)
                    {
                        bestPriority = prio;
                        bestVip = m;
                    }
                }
            }
            return bestVip;
        }

        /// <summary>
        /// Computes a tactical approach position so:
        /// - Assassins circle behind the target's back (-target.forward) for a Backstab!
        /// - Tanks interpose directly between theProtectedVIP and the attacking Hero!
        /// - Melee monsters distribute into 8 angular surround slots instead of clumping!
        /// </summary>
        public static Vector3 ComputeTacticalEngagePoint(ActorUnit self, ActorUnit target, ActorUnit vipAlly, out bool isRearFlank)
        {
            isRearFlank = false;
            if (target == null) return self != null ? self.transform.position : Vector3.zero;

            Archetype arch = self.MonsterDef != null ? self.MonsterDef.archetype : Archetype.Melee;

            // 1. Assassin Rear Flanking (Section 5: "Tiếp cận mục tiêu yếu, Tìm cách tấn công từ phía sau")
            if (arch == Archetype.Assassin)
            {
                Vector3 behind = target.transform.position - target.transform.forward * 1.65f;
                behind.y = 0f;
                float dot = Vector3.Dot((self.transform.position - target.transform.position).normalized, -target.transform.forward);
                isRearFlank = dot > 0.25f;
                return behind;
            }

            // 2. Tank Bodyguard Interposition (Section 5: "Bảo vệ Elite Monster, Thu hút sự chú ý của người chơi")
            if (arch == Archetype.Tank && vipAlly != null && !vipAlly.IsDead)
            {
                Vector3 vipToHero = target.transform.position - vipAlly.transform.position;
                vipToHero.y = 0f;
                if (vipToHero.magnitude > 2.2f)
                {
                    Vector3 guardPos = vipAlly.transform.position + vipToHero.normalized * Mathf.Min(3.2f, vipToHero.magnitude * 0.55f);
                    guardPos.y = 0f;
                    // If hero is still far from the guard point, engage the hero along that line
                    if (Vector3.Distance(guardPos, target.transform.position) <= self.AttackRange + 1.5f)
                        return guardPos;
                }
            }

            // 3. Coordinated 8-slot angular surround ring so melee enemies don't stack on 1 point
            int slot = GetOrAssignSurroundSlot(self);
            float angleRad = (slot * 45f) * Mathf.Deg2Rad;
            float ringRadius = Mathf.Max(1.35f, self.AttackRange * 0.78f);
            Vector3 offset = new Vector3(Mathf.Cos(angleRad) * ringRadius, 0f, Mathf.Sin(angleRad) * ringRadius);
            return target.transform.position + offset;
        }

        private static int GetOrAssignSurroundSlot(ActorUnit unit)
        {
            if (unit == null) return 0;
            if (_slotAssignments.TryGetValue(unit, out int s)) return s;
            int hash = Mathf.Abs(unit.GetInstanceID()) % 8;
            _slotAssignments[unit] = hash;
            return hash;
        }

        /// <summary>
        /// Ticks Elite Monster special affix behavior & Tank Guard Link protection.
        /// </summary>
        public static void TickEliteAndRoleSynergies(EnemyBlackboard bb, float dt)
        {
            if (bb == null || bb.Self == null || bb.Self.IsDead) return;
            ActorUnit self = bb.Self;

            // Tank Guard Link: grants periodic shield to nearby Elite / Mage ally
            if (self.MonsterDef != null && self.MonsterDef.archetype == Archetype.Tank && bb.ProtectedVipAlly != null && !bb.ProtectedVipAlly.IsDead)
            {
                float d = Vector3.Distance(self.transform.position, bb.ProtectedVipAlly.transform.position);
                if (d <= 6.5f && bb.ProtectedVipAlly.Shield < bb.ProtectedVipAlly.MaxHp * 0.12f)
                {
                    bb.ProtectedVipAlly.AddShield(bb.ProtectedVipAlly.MaxHp * 0.12f);
                }
            }

            // Elite Affix periodic pulse
            if (bb.EliteAffix == EliteAffixKind.None || !self.IsElite) return;

            bb.AffixPulseTimer -= dt;
            if (bb.AffixPulseTimer > 0f) return;
            bb.AffixPulseTimer = 5.2f;

            switch (bb.EliteAffix)
            {
                case EliteAffixKind.Vampiric:
                    // Heals self if a hero is in melee range
                    if (bb.Target != null && Vector3.Distance(self.transform.position, bb.Target.transform.position) <= 4.0f)
                    {
                        self.ReceiveHeal(self.MaxHp * 0.06f, self);
                        CharacterVisualFactory.SpawnBurst(self.transform.position + Vector3.up, new Color(1f, 0.15f, 0.3f), 1.8f, 0.4f);
                    }
                    break;

                case EliteAffixKind.Molten:
                    {
                        Vector3 p = self.transform.position;
                        CharacterVisualFactory.SpawnGroundTelegraph(p, 3.0f, 0.85f, new Color(1f, 0.42f, 0.1f), () =>
                        {
                            if (self != null && !self.IsDead)
                                self.DealCircleDamage(p, 3.0f, self.EffectiveAttack() * 1.15f, StatusType.Burn, 3f, self.EffectiveAttack() * 0.18f, new Color(1f, 0.45f, 0.1f));
                        });
                        break;
                    }

                case EliteAffixKind.Stormcaller:
                    if (bb.Target != null && Vector3.Distance(self.transform.position, bb.Target.transform.position) <= 10f)
                    {
                        Vector3 strikePos = bb.Target.transform.position;
                        CharacterVisualFactory.SpawnGroundTelegraph(strikePos, 2.4f, 0.75f, new Color(0.3f, 0.8f, 1f), () =>
                        {
                            if (self != null && !self.IsDead)
                                self.DealCircleDamage(strikePos, 2.4f, self.EffectiveAttack() * 1.1f, StatusType.Slow, 2.5f, 0f, new Color(0.3f, 0.85f, 1f));
                        });
                    }
                    break;

                case EliteAffixKind.FrozenPulse:
                    {
                        Vector3 p = self.transform.position;
                        CharacterVisualFactory.SpawnGroundTelegraph(p, 3.4f, 0.85f, new Color(0.45f, 0.95f, 1f), () =>
                        {
                            if (self != null && !self.IsDead)
                                self.DealCircleDamage(p, 3.4f, self.EffectiveAttack() * 1.05f, StatusType.Slow, 3.2f, 0f, new Color(0.45f, 0.95f, 1f));
                        });
                        break;
                    }
            }
        }
    }
}


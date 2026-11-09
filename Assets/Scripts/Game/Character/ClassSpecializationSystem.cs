using UnityEngine;

namespace AbyssGame
{
    public enum EquipArchetype
    {
        Universal = 0,
        VanguardPlate = 1,   // Knight, Guardian, Berserker
        StalkerLeather = 2,  // Assassin, Ranger, Monk
        ArcanistCloth = 3,   // Mage, Warlock
        SacredRelic = 4      // Druid, Cleric
    }

    /// <summary>
    /// Modular 10-Class Build Variations, Skill Progression (Rank 1..5), Build-Specific Skill Hooks,
    /// and Equipment Compatibility / Class Affinity System (Phase 4 - Section 4 & Section 2.2).
    /// </summary>
    public static class ClassSpecializationSystem
    {
        public static EquipArchetype GetClassArchetype(int classId)
        {
            switch (classId)
            {
                case 0: // Knight
                case 1: // Guardian
                case 2: // Berserker
                    return EquipArchetype.VanguardPlate;
                case 3: // Assassin
                case 4: // Ranger
                case 7: // Monk
                    return EquipArchetype.StalkerLeather;
                case 5: // Mage
                case 6: // Warlock
                    return EquipArchetype.ArcanistCloth;
                case 8: // Druid
                case 9: // Cleric
                    return EquipArchetype.SacredRelic;
                default:
                    return EquipArchetype.Universal;
            }
        }

        public static string GetArchetypeLabel(EquipArchetype arch)
        {
            switch (arch)
            {
                case EquipArchetype.VanguardPlate:  return "Chiến Giáp Nặng (Knight/Guardian/Berserker)";
                case EquipArchetype.StalkerLeather: return "Giáp Nhẹ Cơ Động (Assassin/Ranger/Monk)";
                case EquipArchetype.ArcanistCloth:  return "Pháp Phục Hắc Bí (Mage/Warlock)";
                case EquipArchetype.SacredRelic:    return "Thánh Y Tự Nhiên (Druid/Cleric)";
                default:                            return "Đa Dụng (Mọi Class)";
            }
        }

        public static string GetArchetypeShortTag(EquipArchetype arch)
        {
            switch (arch)
            {
                case EquipArchetype.VanguardPlate:  return "Giáp Nặng";
                case EquipArchetype.StalkerLeather: return "Giáp Nhẹ";
                case EquipArchetype.ArcanistCloth:  return "Pháp Phục";
                case EquipArchetype.SacredRelic:    return "Thánh Y";
                default:                            return "Đa Dụng";
            }
        }

        /// <summary>
        /// Evaluates Equipment Compatibility & Class Affinity Bonus for a given class:
        /// - Universal items (Rings, Amulets, legacy items): 1.00x compatible for all classes.
        /// - Matching Class Archetype Weapon/Armor/Helmet: +15% Class Affinity Bonus (1.15x stats)!
        /// - Non-matching Weapon: cannot equip incompatible weapon class (returns false).
        /// - Non-matching Armor/Helmet: wearable at 0.85x penalty if forced, never auto-equipped over matching gear.
        /// </summary>
        public static bool EvaluateCompatibility(int classId, EquipItem item, out float statMultiplier, out string tagText)
        {
            statMultiplier = 1f;
            tagText = "";
            if (item == null) return false;

            if (item.archetype == EquipArchetype.Universal || item.slot == ItemSlot.Ring || item.slot == ItemSlot.Amulet)
            {
                statMultiplier = 1.0f;
                tagText = "[Đa Dụng]";
                return true;
            }

            EquipArchetype heroArch = GetClassArchetype(classId);
            if (item.archetype == heroArch)
            {
                statMultiplier = 1.15f;
                tagText = $"[{GetArchetypeShortTag(item.archetype)} • Tương Thích +15%]";
                return true;
            }

            // Incompatible Weapon slot is strictly restricted to compatible archetype
            if (item.slot == ItemSlot.Weapon)
            {
                statMultiplier = 0f;
                tagText = $"[Yêu cầu {GetArchetypeShortTag(item.archetype)}]";
                return false;
            }

            // Cross-archetype Armor/Helmet is usable with -15% off-spec penalty
            statMultiplier = 0.85f;
            tagText = $"[{GetArchetypeShortTag(item.archetype)} -15%]";
            return true;
        }

        /// <summary>
        /// Distinct secondary accent color per Build (0, 1, 2) so switching Builds visibly transforms
        /// the Hero's aura, weapon glow, and skill VFX!
        /// </summary>
        public static Color GetBuildAccentColor(int classId, int buildIndex, Color baseClassColor)
        {
            int b = Mathf.Clamp(buildIndex, 0, 2);
            switch (classId)
            {
                case 0: // Knight: Iron Vanguard (Steel Blue) / Radiant Paladin (Holy Gold) / Dark Knight (Abyss Crimson-Violet)
                    return b == 0 ? new Color(0.32f, 0.68f, 1.0f) : (b == 1 ? new Color(1.0f, 0.88f, 0.36f) : new Color(0.82f, 0.18f, 0.52f));
                case 1: // Guardian: Immortal Bastion (Gold) / Thorns Templar (Amber) / Valkyrie Aegis (Sky Cyan)
                    return b == 0 ? new Color(0.95f, 0.76f, 0.30f) : (b == 1 ? new Color(1.0f, 0.52f, 0.18f) : new Color(0.45f, 0.90f, 1.0f));
                case 2: // Berserker: Blood Frenzy (Crimson) / Titan Juggernaut (Molten Bronze) / Crimson Whirlwind (Scarlet Flame)
                    return b == 0 ? new Color(0.95f, 0.14f, 0.18f) : (b == 1 ? new Color(0.92f, 0.52f, 0.18f) : new Color(1.0f, 0.28f, 0.12f));
                case 3: // Assassin: Nightblade (Shadow Violet) / Phantom Dancer (Ghost Cyan) / Venom Alchemist (Toxic Emerald)
                    return b == 0 ? new Color(0.72f, 0.28f, 0.95f) : (b == 1 ? new Color(0.35f, 0.88f, 0.95f) : new Color(0.28f, 0.96f, 0.38f));
                case 4: // Ranger: Deadeye Sniper (Emerald) / Frost Warden (Glacial Ice) / Tempest Volley (Storm Teal)
                    return b == 0 ? new Color(0.30f, 0.85f, 0.42f) : (b == 1 ? new Color(0.42f, 0.86f, 1.0f) : new Color(0.65f, 0.96f, 0.35f));
                case 5: // Mage: Pyromancer (Fire) / Cryomancer (Frost) / Arcanist (Arcane Magenta)
                    return b == 0 ? new Color(1.0f, 0.44f, 0.12f) : (b == 1 ? new Color(0.36f, 0.86f, 1.0f) : new Color(0.78f, 0.36f, 1.0f));
                case 6: // Warlock: Affliction Reaper (Necro Green-Purple) / Demonologist (Hellfire Violet) / Soul Harvester (Abyssal Magenta)
                    return b == 0 ? new Color(0.52f, 0.92f, 0.42f) : (b == 1 ? new Color(0.92f, 0.28f, 0.48f) : new Color(0.68f, 0.22f, 1.0f));
                case 7: // Monk: Windwalker (Jade Wind) / Mistweaver (Soothing Lotus) / Dragon Fist (Solar Flame)
                    return b == 0 ? new Color(0.35f, 0.95f, 0.72f) : (b == 1 ? new Color(0.48f, 0.98f, 0.62f) : new Color(1.0f, 0.56f, 0.15f));
                case 8: // Druid: Grove Healer (Spring Leaf) / Ursine Guardian (Amber Earth) / Stormcaller (Thunder Cyan)
                    return b == 0 ? new Color(0.32f, 0.95f, 0.52f) : (b == 1 ? new Color(0.92f, 0.64f, 0.24f) : new Color(0.38f, 0.82f, 1.0f));
                case 9: // Cleric: Divine Oracle (Pure Gold) / Holy Inquisitor (Searing Dawn) / Lightbringer (Platinum Halo)
                    return b == 0 ? new Color(1.0f, 0.90f, 0.48f) : (b == 1 ? new Color(1.0f, 0.58f, 0.22f) : new Color(0.75f, 0.92f, 1.0f));
                default:
                    return baseClassColor;
            }
        }

        /// <summary>
        /// Applies authentic per-class, per-build stat modifiers (30 distinct builds across 10 classes).
        /// </summary>
        public static void ApplyBuildStatModifiers(
            int classId, int buildIndex,
            ref float hp, ref float mp, ref float atk, ref float def,
            ref float aspd, ref float crit, ref float crd, ref float spd,
            ref float ls, ref float cdr, ref float healB)
        {
            int b = Mathf.Clamp(buildIndex, 0, 2);
            switch (classId)
            {
                case 0: // Knight
                    if (b == 0) { hp *= 1.30f; def *= 1.22f; }                             // Iron Vanguard
                    else if (b == 1) { mp *= 1.30f; healB += 0.28f; hp *= 1.12f; }         // Radiant Paladin
                    else { atk *= 1.28f; ls += 0.10f; crit += 0.06f; }                     // Dark Knight
                    break;

                case 1: // Guardian
                    if (b == 0) { hp *= 1.35f; def *= 1.25f; healB += 0.15f; }             // Immortal Bastion
                    else if (b == 1) { def *= 1.18f; atk *= 1.18f; hp *= 1.15f; }          // Thorns Templar
                    else { spd *= 1.16f; cdr += 0.18f; atk *= 1.15f; }                     // Valkyrie Aegis
                    break;

                case 2: // Berserker
                    if (b == 0) { aspd *= 1.22f; ls += 0.12f; spd *= 1.08f; }              // Blood Frenzy
                    else if (b == 1) { hp *= 1.25f; def *= 1.20f; atk *= 1.12f; }          // Titan Juggernaut
                    else { atk *= 1.24f; crit += 0.12f; crd += 0.20f; }                    // Crimson Whirlwind
                    break;

                case 3: // Assassin
                    if (b == 0) { crd += 0.38f; crit += 0.10f; atk *= 1.12f; }             // Nightblade
                    else if (b == 1) { cdr += 0.20f; spd *= 1.14f; aspd *= 1.12f; }        // Phantom Dancer
                    else { atk *= 1.18f; cdr += 0.12f; ls += 0.06f; }                      // Venom Alchemist
                    break;

                case 4: // Ranger
                    if (b == 0) { crit += 0.15f; crd += 0.28f; atk *= 1.14f; }             // Deadeye Sniper
                    else if (b == 1) { cdr += 0.16f; mp *= 1.20f; hp *= 1.12f; }           // Frost Warden
                    else { aspd *= 1.25f; spd *= 1.12f; atk *= 1.10f; }                    // Tempest Volley
                    break;

                case 5: // Mage
                    if (b == 0) { crd += 0.30f; crit += 0.10f; atk *= 1.12f; }             // Pyromancer
                    else if (b == 1) { hp *= 1.15f; def += 12f; cdr += 0.10f; }            // Cryomancer
                    else { mp *= 1.32f; cdr += 0.22f; atk *= 1.12f; }                      // Arcanist
                    break;

                case 6: // Warlock
                    if (b == 0) { atk *= 1.18f; ls += 0.06f; cdr += 0.12f; }               // Affliction Reaper
                    else if (b == 1) { mp *= 1.25f; hp *= 1.16f; cdr += 0.15f; }           // Demonologist
                    else { atk *= 1.22f; ls += 0.10f; crit += 0.08f; }                     // Soul Harvester
                    break;

                case 7: // Monk
                    if (b == 0) { aspd *= 1.24f; spd *= 1.14f; crit += 0.08f; }            // Windwalker
                    else if (b == 1) { healB += 0.35f; mp *= 1.25f; cdr += 0.14f; }        // Mistweaver
                    else { atk *= 1.25f; crd += 0.25f; }                                   // Dragon Fist
                    break;

                case 8: // Druid
                    if (b == 0) { healB += 0.35f; mp *= 1.25f; cdr += 0.12f; }             // Grove Healer
                    else if (b == 1) { hp *= 1.28f; def *= 1.25f; atk *= 1.10f; }          // Ursine Guardian
                    else { atk *= 1.25f; cdr += 0.15f; crit += 0.08f; }                    // Stormcaller
                    break;

                case 9: // Cleric
                    if (b == 0) { healB += 0.35f; mp *= 1.25f; hp *= 1.10f; }              // Divine Oracle
                    else if (b == 1) { atk *= 1.38f; crit += 0.15f; aspd *= 1.12f; }       // Holy Inquisitor
                    else { cdr += 0.20f; hp *= 1.16f; healB += 0.20f; }                    // Lightbringer
                    break;
            }
        }

        // ==================== SKILL PROGRESSION (RANK 1..5) ====================

        public static int GetSkillRank(ActorUnit unit, int slot)
        {
            if (slot < 0 || slot >= 4) return 1;
            if (unit != null && unit.IsLocalPlayer && GameSession.I != null)
            {
                var ranks = GameSession.I.Profile.skillRanks;
                if (ranks != null && slot < ranks.Length)
                    return Mathf.Clamp(ranks[slot], 1, 5);
            }
            // AI party scales skill ranks automatically with party level
            int lvl = unit != null ? unit.Level : 1;
            return Mathf.Clamp(1 + lvl / 4, 1, 5);
        }

        public static float GetSkillPowerMultiplier(int rank)
        {
            return 1f + (Mathf.Clamp(rank, 1, 5) - 1) * 0.13f; // +13% per rank (up to +52% at Lv.5)
        }

        public static float GetSkillCooldownMultiplier(int rank)
        {
            return 1f - (Mathf.Clamp(rank, 1, 5) - 1) * 0.05f; // -5% CD per rank (up to -20% at Lv.5)
        }

        public static float GetSkillAreaMultiplier(int rank)
        {
            return rank >= 3 ? 1.18f : 1.0f; // Milestone Rank 3+: +18% AoE radius & range!
        }

        /// <summary>
        /// Executes authentic Build-Specific Active Skill Augmentations when a Hero casts Q/W/E/R!
        /// Every single Class has real mechanical skill variations across its 3 Builds (no clone skills!).
        /// </summary>
        public static void ExecuteBuildSkillAugment(ActorUnit caster, int slot, SkillDef sk, Vector3 aimPoint, Vector3 dir, float rawPow)
        {
            if (caster == null || caster.HeroClass == null) return;
            int cid = caster.HeroClass.id;
            int b = caster.BuildIndex;
            Color bCol = GetBuildAccentColor(cid, b, sk.color);

            switch (cid)
            {
                case 0: // Knight
                    if (b == 2 && (slot == 0 || slot == 3)) // Dark Knight: Crimson Abyss Shockwave sacrificing 4% HP for +35% AoE burst
                    {
                        caster.Hp = Mathf.Max(15f, caster.Hp - caster.MaxHp * 0.04f);
                        caster.DealCircleDamage(caster.transform.position + dir * 2.2f, 3.6f, rawPow * 0.42f, StatusType.Curse, 4f, 0f, bCol);
                    }
                    else if (b == 1 && slot == 3) // Radiant Paladin: Judgement grants party shield
                    {
                        foreach (var u in ActorUnit.AllUnits)
                            if (u.Team == caster.Team && !u.IsDead && Vector3.Distance(caster.transform.position, u.transform.position) <= 14f)
                                u.AddShield(u.MaxHp * 0.18f);
                    }
                    break;

                case 1: // Guardian
                    if (b == 2 && slot == 0) // Valkyrie Aegis: Shield Wave launches a piercing lance wave!
                    {
                        SkillProjectile.Spawn(caster, caster.transform.position + Vector3.up * 1.2f + dir * 0.7f, dir,
                            22f, 14f, rawPow * 0.85f, true, StatusType.Slow, 2.5f, 0f, 0f, bCol, 32f, 4f, 0.1f);
                    }
                    else if (b == 0 && slot == 1) // Immortal Bastion: Sanctuary bonus shield
                    {
                        caster.AddShield(caster.MaxHp * 0.14f);
                    }
                    break;

                case 2: // Berserker
                    if (b == 1 && slot == 0) // Titan Juggernaut: Cleave triggers seismic tremor slowing enemies
                    {
                        caster.DealCircleDamage(caster.transform.position + dir * 1.8f, 3.4f, rawPow * 0.35f, StatusType.Slow, 2.5f, 0f, bCol);
                    }
                    else if (b == 2 && slot == 3) // Crimson Whirlwind: Ragnarok grants SpeedUp + Rage extension
                    {
                        caster.ApplyStatus(StatusType.SpeedUp, 5f, 0f, caster);
                    }
                    break;

                case 3: // Assassin
                    if (b == 1 && slot == 1) // Phantom Dancer: Shadow Step refunds 35% CD & grants evasion shield
                    {
                        caster.SkillCds[1] *= 0.65f;
                        caster.AddShield(caster.MaxHp * 0.14f);
                    }
                    else if (b == 2 && slot == 2) // Venom Alchemist: Venom throws 3 toxic fan daggers!
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            Vector3 d = Quaternion.Euler(0f, i * 16f, 0f) * dir;
                            SkillProjectile.Spawn(caster, caster.transform.position + Vector3.up * 1.2f, d,
                                23f, 11f, rawPow * 0.55f, false, StatusType.Poison, 6f, caster.EffectiveAttack() * 0.38f, 0f, bCol);
                        }
                    }
                    break;

                case 4: // Ranger
                    if (b == 2 && slot == 2) // Tempest Volley: bonus 2 outer wind arrows + SpeedUp!
                    {
                        caster.ApplyStatus(StatusType.SpeedUp, 3.5f, 0f, caster);
                        for (int s = -1; s <= 1; s += 2)
                        {
                            Vector3 d = Quaternion.Euler(0f, s * 28f, 0f) * dir;
                            SkillProjectile.Spawn(caster, caster.transform.position + Vector3.up * 1.25f, d, 24f, 18f, rawPow * 0.85f, true, StatusType.None, 0f, 0f, 0f, bCol);
                        }
                    }
                    else if (b == 0 && slot == 0) // Deadeye Sniper: Piercing Shot fires a high-impact shockwave round
                    {
                        SkillProjectile.Spawn(caster, caster.transform.position + Vector3.up * 1.25f + dir * 0.8f, dir,
                            29f, 24f, rawPow * 0.45f, true, StatusType.Slow, 1.5f, 0f, 1.8f, bCol, 36f, 5f, 0.25f);
                    }
                    break;

                case 5: // Mage
                    if (b == 2 && slot == 2) // Arcanist: Arcane Pulse fires 2 extra converging arcane missiles!
                    {
                        for (int s = -1; s <= 1; s += 2)
                        {
                            Vector3 d = Quaternion.Euler(0f, s * 10f, 0f) * dir;
                            SkillProjectile.Spawn(caster, caster.transform.position + Vector3.up * 1.25f, d, 24f, 20f, rawPow * 0.48f, true, StatusType.None, 0f, 0f, 0f, bCol);
                        }
                    }
                    else if (b == 1 && slot == 1) // Cryomancer: Frost Nova encases Mage in Ice Barrier
                    {
                        caster.AddShield(caster.MaxHp * 0.22f);
                    }
                    break;

                case 6: // Warlock
                    if (b == 1 && slot == 2) // Demonologist: Summons a SECOND Abyssal Minion!
                    {
                        var minGo = new GameObject("AbyssFiendMinion");
                        minGo.transform.position = caster.transform.position - caster.transform.right * 1.4f;
                        var minUnit = minGo.AddComponent<ActorUnit>();
                        minUnit.InitSummon(caster, sk.statusDur + 4f);
                        minGo.AddComponent<HeroPartyAI>().IsMinion = true;
                        CharacterVisualFactory.SpawnBurst(minGo.transform.position + Vector3.up, bCol, 1.5f, 0.4f);
                    }
                    else if (b == 2 && slot == 0) // Soul Harvester: Soul Bolt restores 12 Mana on cast & pierces
                    {
                        caster.Mana = Mathf.Min(caster.MaxMana, caster.Mana + 12f);
                    }
                    break;

                case 7: // Monk
                    if (b == 1 && slot == 1) // Mistweaver: Zen Mend also shields allies!
                    {
                        foreach (var u in ActorUnit.AllUnits)
                            if (u.Team == caster.Team && !u.IsDead && Vector3.Distance(caster.transform.position, u.transform.position) <= 14f)
                                u.AddShield(u.MaxHp * 0.14f);
                    }
                    else if (b == 2 && slot == 2) // Dragon Fist build: Palm Strike launches a fiery Ki wave!
                    {
                        SkillProjectile.Spawn(caster, caster.transform.position + Vector3.up * 1.2f + dir * 0.6f, dir,
                            22f, 12f, rawPow * 0.55f, true, StatusType.Burn, 3f, caster.EffectiveAttack() * 0.25f, 0f, bCol);
                    }
                    break;

                case 8: // Druid
                    if (b == 1 && slot == 2 && caster.InBearForm) // Ursine Guardian: Bear Form transform taunts & slams ground!
                    {
                        caster.DealCircleDamage(caster.transform.position, 4.5f, caster.EffectiveAttack() * 1.6f, StatusType.Taunted, 4f, 0f, bCol);
                    }
                    else if (b == 2 && (slot == 0 || slot == 3)) // Stormcaller: Lightning strike stun!
                    {
                        Vector3 strikePos = slot == 0 ? aimPoint : caster.transform.position;
                        caster.DealCircleDamage(strikePos, 3.2f, rawPow * 0.38f, StatusType.Stun, 1.1f, 0f, bCol);
                    }
                    break;

                case 9: // Cleric
                    if (b == 1 && slot == 0) // Holy Inquisitor: Smite fires 2 flanking judgement bolts!
                    {
                        for (int s = -1; s <= 1; s += 2)
                        {
                            Vector3 d = Quaternion.Euler(0f, s * 12f, 0f) * dir;
                            SkillProjectile.Spawn(caster, caster.transform.position + Vector3.up * 1.25f, d,
                                22f, 18f, rawPow * 0.52f, false, StatusType.Burn, 3f, caster.EffectiveAttack() * 0.22f, 0f, bCol);
                        }
                    }
                    else if (b == 0 && slot == 1) // Divine Oracle: Group Heal grants DefUp blessing
                    {
                        foreach (var u in ActorUnit.AllUnits)
                            if (u.Team == caster.Team && !u.IsDead && Vector3.Distance(caster.transform.position, u.transform.position) <= 16f)
                                u.ApplyStatus(StatusType.DefUp, 5f, 0f, caster);
                    }
                    break;
            }
        }
    }
}


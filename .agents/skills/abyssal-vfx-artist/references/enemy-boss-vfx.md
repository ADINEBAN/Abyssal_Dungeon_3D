# Enemy & Boss VFX — All 10 Monsters, Summons & 3-Phase Abyssal Knight

Enemies and bosses in Abyssal Dungeon 3D must broadcast threats clearly to up to 4 co-op players simultaneously without burying characters under unreadable fog. Existing enemy/boss systems live in `GameDB` (`AbyssGame`), [DungeonWorldBuilder.cs](../../../Assets/Scripts/Game/Dungeon/DungeonWorldBuilder.cs), and [AbyssalBossAI.cs](../../../Assets/Scripts/Game/AI/AbyssalBossAI.cs).

---

## 1. Enemy Roster VFX Profiles (`GameDB.Enemies`)

| Enemy ID | Prefab / Rig | Archetype Role | Aggro & Windup Tell | Attack / Special VFX |
| --- | --- | --- | --- | --- |
| `Goblin` | `Enemy_Goblin` | Fast Swarm Melee | Eyes amber glint + short ground scratch spark | Rust-iron dagger crescent (`#8C6239`) |
| `GoblinArcher` | `Enemy_GoblinArcher` | Ranged Skirmisher | Crimson-amber arrowhead charge flash (`0.35s` draw) | Fast feathered arrow trail (`TrailRenderer` `0.10s`) |
| `Skeleton` | `Enemy_Skeleton` | Basic Undead | Pale cyan bone-joint rattle dust | Bone-white blunt cleave arc |
| `SkeletonWarrior` | `Enemy_SkeletonWarrior` | Shielded Bruiser | Heavy crimson-eye flare + shield brace pulse | Wide bronze-slate sword sweep + block deflection sparks |
| `SkeletonMage` | `Enemy_SkeletonMage` | Ranged Caster | Spinning purple rune ring at staff head (`0.5s` windup) | Dark-violet bolt + small cursed sigil splash |
| `Zombie` | `Enemy_Zombie` | Plague Brute | Dripping sickly bile wisps from shoulders (`Poison`) | Toxic green lunge swipe + brief stink spore cloud |
| `DarkKnight` | `Enemy_DarkKnight` | **Elite** Heavy Melee | Abyssal-crimson sword ignition + dark ember plume | Heavy dark-crimson `AbyssalWeaponTrail` + ground shock arc |
| `Demon` | `Enemy_Demon` | **Elite** Aggressive Diver | Hellfire chest vent flare + horned ember trail | Fiery double-claw crimson slashes (`Fire` / `Blood`) |
| `CrystalGolem` | `Enemy_CrystalGolem` | **MiniBoss** Tank (`CrystalChamber`) | Crystal core vein pulse acceleration (`AbyssalCrystalLit.shader`) | Shard eruption cone + azure crystal shatter shockwave (`T_Vfx_ShatterLines`) |
| `ForestMonster` | `Enemy_ForestMonster` | Brute / Corrupted Sentinel | Swirling emerald moss-spore inhalation | Briar root slam (`T_Vfx_GroundCrack`) + green spore burst |
| `SoulMinion` | `Enemy_SoulMinion` | Summon (Warlock & Boss) | Translucent violet ghost trail around floating torso | Wisp dart impact + clean soul vanish pop on death/timeout |

### Elite & MiniBoss Modifiers
- **Elite Monsters**: Ground-level sinister dark-gold / crimson rotating aura ring (`0.65m` radius, low opacity) + richer shoulder ember/spore wisps so players spot elites in crowds immediately.
- **CrystalGolem MiniBoss**: Drive `_EmissionColor` and `_PulseSpeed` on `AbyssalCrystalLit.shader` via `MaterialPropertyBlock` during windups instead of spawning heavy overlapping particles.

---

## 2. Standard Enemy Lifecycle States

1. **Spawn Emergence**: Rising dark-stone/void ring (`0.55s`) + brief upward ash burst. Summoned `SoulMinion` emerges from a crisp `T_Vfx_GothicRuneCircle` portal.
2. **Aggro Alert**: Brief `0.25s` eye/head flare above `Bip001 Head` (no giant billboard clutter).
3. **Windup & Telegraph (CRITICAL)**:
   - Use `CharacterVisualFactory.SpawnGroundTelegraph` (`ProceduralVFXFactory` / `ScalableVfxResponder.isCombatTelegraph = true`).
   - Sharp crimson/violet perimeter outline + inner progress fill synced to `windupDelay`.
   - **NEVER scale down or disable ground telegraphs on `Low` graphics quality.**
4. **Damage Reaction & Hit Flash**: `CombatFeelManager` applies fast `MaterialPropertyBlock` white/crimson flash (`0.08s`) + directional sparks at exact `WeaponHitboxSweep3D` hit point.
5. **Armor / Poise Stagger**: Shatter line burst (`T_Vfx_ShatterLines`) + rotating golden-grey stun halo above head during `Stun`/stagger duration.
6. **Status / DoT Loops**: Compact shoulder/torso attachment (`Burn` orange lick, `Poison` green drip, `Freeze` ice crust sheen, `Bleed` crimson droplets) capped at `1` loop system per status type.
7. **Death & Dissolution**:
   - Standard monsters: dark ash/smoke puff + falling ember motes + optional brief soul-wisp rise (`0.5s`).
   - Boss/MiniBoss: staggered internal explosions -> high-contrast abyssal-gold shatter pillar (`1.5s`) -> lingering cleared-arena purification glow.

---

## 3. 3-Phase Boss VFX — `AbyssalKnight` (`AbyssalBossAI.cs`)

`AbyssalBossAI` drives **3 escalated combat phases** in Room 6 (`BossArena`). Keep his silhouette regal and towering while amplifying arena danger cleanly:

| Boss Move / Phase | `AbyssalBossAI` Trigger | Visual Signature & Readability Spec |
| --- | --- | --- |
| **Phase 1 (100%–65% HP)** | Base knight armor glow | Measured dark-violet armor veins; `Sword Slash` wide crimson-violet trail + `Shield Bash` amber-iron shockwave + `Charge` red line path telegraph |
| **Phase 2 (65%–30% HP)** | Enrage roar + Phase 2 transition | Phase shockwave (`T_Vfx_NovaWave`, `radius = 8m`) + armor glow shift from steel-violet to molten magenta; unlocks `Dark Slash` crescent projectile wave (`T_Vfx_BladeWave`) and `Summons` (`SoulMinion` rift gates) |
| **Phase 3 (< 30% HP)** | `ArenaPhase3Transformation` | Full gothic apocalypse ramp: arena outer hazard ring (`Abyssal Void Burn` perimeter decal + rising purple fire columns), `Shockwave` double-ring slam (`T_Vfx_GroundCrack` + `T_Vfx_ShockRing`), and `Meteor Storm` (3s staggered crimson sky-drop telegraph circles -> falling abyssal meteors -> crater bursts) |

> **Co-op Safety Rule**: In Boss Phase 3 (`Meteor Storm` + outer hazard ring + 4 players casting), enforce strict particle budget (`Low`: crater core ring + sharp telegraph only; `Medium`: +12 embers; `High`: +28 embers + scorch fade) so frame time stays flat when multiple meteors land together.


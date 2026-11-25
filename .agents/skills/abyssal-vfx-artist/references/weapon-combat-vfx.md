# Weapon & Combat VFX — Sockets, Trails, Sparks & Impact Feedback

Abyssal Dungeon 3D's melee and ranged combat relies on immediate, readable weapon arcs synchronized to **exact hitbox windows** (`WeaponHitboxSweep3D` in [CombatHitboxSystem.cs](../../../Assets/Scripts/Game/Combat/CombatHitboxSystem.cs)) and visual slash feedback in [CombatFeelManager.cs](../../../Assets/Scripts/Game/Combat/CombatFeelManager.cs) and [ClassVisualArmorKit.cs](../../../Assets/Scripts/Game/Character/ClassVisualArmorKit.cs).

---

## 1. Verified Weapon Families & Bone Sockets

All exported hero prefabs share standardized bone hierarchies wired by `ClassVisualArmorKit`:

| Weapon Family | Repo Classes / Prefabs | Active Socket(s) | Trail Tip Attachment & Width |
| --- | --- | --- | --- |
| **Sword & Shield** | Knight (`ClassWeapon_KnightSword`, `ClassShield_KnightCrest`, `Weap_Knight_RunicLongsword`) | `RH_WP` (`Bip001 R Hand`), `LH_WP` (`Bip001 L Hand`) | Tip `0.95m`, width `0.26m`, `0.17s` decay |
| **Hammer & Tower Shield** | Guardian (`ClassWeapon_GuardianHammer`, `ClassShield_GuardianBulwark`) | `RH_WP`, `LH_WP` | Tip `0.85m`, width `0.32m`, heavy golden shock arc |
| **Greatsword / Cleaver** | Berserker (`ClassWeapon_BerserkerCleaver`) | `RH_WP` (2H pose) | Tip `1.25m`, width `0.42m`, molten blood-ember core |
| **Dual Daggers** | Assassin (`ClassWeapon_AssassinDagger` ×2) | `RH_WP` (`SignatureMainWeapon`) + `LH_WP` (`SignatureOffhand`) | Dual tips `0.55m`, width `0.18m`, twin violet ribbons |
| **Bow & Quiver** | Ranger (Bow in `LH_WP`, arrow draw from `RH_WP` + `RangerQuiver`) | `LH_WP` + bowstring draw vector | Arrow trail `TrailRenderer` (`0.12s`) + wind fletch burst |
| **Mage Staff & Astrolabe** | Mage (`ClassWeapon_MageStaff`, `MageAstrolabe`) | `RH_WP` + crystal tip offset | Orb tip flame/frost/arcane emitter + orb cast flare |
| **Warlock Scepter & Grimoire** | Warlock (`ClassWeapon_WarlockScepter`, `WarlockGrimoire`) | `RH_WP` + floating book | Void-purple wisps at scepter eye + curse sigil ribbon |
| **Monk Gauntlets & Beads** | Monk (`ClassWeapon_MonkGauntlet` ×2, `MonkBeads`) | `RH_WP` + `LH_WP` knuckles | Short-life `0.12s` golden chi fist streaks + sonic palm burst |
| **Druid Scythe-Staff** | Druid (`ClassWeapon_DruidScytheStaff`) + Beast Claws | `RH_WP` / Bear claws | Curved crescent emerald trail on cast; wide claw arcs in BearForm |
| **Cleric Mace & Reliquary** | Cleric (`ClassWeapon_ClericMace`, `ClassShield_ClericReliquary`) | `RH_WP`, `LH_WP` | Warm consecrated gold trail (`0.16s`) + shield flare on ward cast |

---

## 2. Weapon Trail & Slash Arc Standard

Two complementary layers create satisfying swordplay without clutter:
1. **`WeaponBladeTrail3D` (`AbyssalWeaponTrail.shader`)**:
   - Attached directly at weapon tip (`BladeTrailTip`) via `WeaponBladeTrail3D.Attach(weaponParent, localTipOffset, bladeWidth, accentColor)`.
   - Driven ONLY during the active swing window (`trail.Activate(duration, color)`), never left emitting permanently during idle/walk.
   - Uses `_MainColor`, `_EdgeColor`, `_Progress`, and `_Opacity` with UV fade and razor-edge core.
2. **Directional `SlashArcRibbonFx` (`GameObjectPool` + `T_Vfx_SlashCrescent` / `T_Vfx_BladeSlash`)**:
   - Spawned at `CombatFeelManager.SpawnSlashArc` slanted at the combo step angle (`Combo 1`: right-to-left `-18 deg`, `Combo 2`: left-to-right `+20 deg`, `Combo 3 Finisher`: steep heavy cleave `0 deg` + `1.35x` scale).
   - Rotates across the forward swing plane in `0.16–0.22s` so hits read clearly from the angled top-down camera even if a character animation clips rapidly.

---

## 3. Melee & Ranged Combat Events

| Combat Moment | Required Visual Components | Pooled / Shared Hook |
| --- | --- | --- |
| **Normal Hit Impact** | Directional spark cone (`DirectionalSparkBurstFx`) along surface normal + tight flash (`ExpandingBurstFx`) + brief `MaterialPropertyBlock` white/tint flash on victim | `CombatFeelManager.SpawnHitImpact` |
| **Critical Hit** | `1.45x` spark count, gold-amber starburst flare (`T_Vfx_StarFlare`), thin radial shock ring (`T_Vfx_ShockRing`), gold FloatingCombatText | `CombatFeelManager.SpawnHitImpact(isCrit: true)` |
| **Heavy / Combo Finisher** | Wider blade arc (`1.35x`), ground dust/crack decal (`T_Vfx_GroundCrack`), miniature camera shake impulse (`0.12s`) | `ActorUnit` combo step 3 / Heavy attack |
| **Block / Guard** | Shield-centered brass-gold rune disc ripple (`T_Vfx_GothicRuneCircle`) + radial metallic deflection sparks outward along shield normal | Shield-bearing classes + Boss block |
| **Parry / Perfect Counter** | High-contrast white-gold star flare (`T_Vfx_StarFlare`, `0.15s`) + concentric shockwave ring + short `0.06s` hit-stop (`TriggerHitStop`) | Combat feel extension |
| **Armor / Poise Break** | Expanding shatter shards (`T_Vfx_ShatterLines`) + gold-crimson burst around `Target` + stagger star loop (`StatusVisualAttachment`) | `ActorUnit` poise break (`Poise <= 0`) |
| **Charged Attack** | Gather inward wisps into weapon head during hold -> ring snap on release (`full charge`) -> empowered trail color | Hold/windup skills |
| **Projectile Launch** | Muzzle rune ring (`T_Vfx_GothicRuneCircle`, `0.2s`) + core orb/arrow mesh + `TrailRenderer` ribbon (`0.10–0.18s`) | Mage bolts, Ranger arrows, Warlock hex orbs |
| **Projectile Hit / Pierce** | Splash flash on enemy chest height (`y = 1.0m`) + fading residual sparks; zero orphaned trail snap (detach & let `TrailRenderer` decay cleanly!) | Pool return after trail `time` expires |
| **Ultimate Attack** | Brief caster ground sigil (`0.35s` anticipation) -> multi-layered primary burst -> persistent lingering ember/mist field (`<= 2.5s`) | Slot 4 (`R / 4` Ultimate skills) |

---

## 4. Weapon Trail & Clipping Hygiene

- Always set `shadowCastingMode = ShadowCastingMode.Off` and `receiveShadows = false` on all weapon trails, slash crescents, and impact billboards.
- On projectile return-to-pool, **disable emission and delay deactivation by `trail.time` (`0.18s`)** or call `trail.Clear()` before teleporting, preventing cross-room stretching lines across the dungeon.
- Never alter `WeaponHitboxSweep3D`'s `windupDelay`, `activeDuration`, `reachRadius`, or `arcDegrees` to fit a slower/longer VFX trail; tune VFX timing to match the authoritative hitbox values.


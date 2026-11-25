# ABYSSAL DUNGEON 3D — PHASE 1 AUDIT (10 CLASSES: WEAPONS, RIGGING, ANIMATION, VFX & UI)

**Project**: `C:\LTG\Abyssal_Dungeon_3D` (`Unity 2021.3.11f1` + `URP 12.1.7` + `com.unity.animation.rigging 1.2.1`)  
**Backup Location**: `Backups/WeaponRigUiRedesign_PreBackup/`  
**Before Capture**: `Captures/WeaponUiRedesign/01_Before_ClassSelect_UI.png`

---

## 1. Bảng Audit Toàn Bộ 10 Class

| Class | Current Loadout & Prefab | Skeleton / Sockets (`RH_WP`, `LH_WP`) | Animator & Clip Lengths | VFX & Hitbox Sync | Status & Issues Found |
|---|---|---|---|---|---|
| **Knight (0)** | `Wpn_KnightSword` (R) + `Wpn_KnightShield` (L) | Bip001 Generic (12 fingers), `RH_WP` `(-0.077,0.029,0)` `rot(0,0,0)`, `LH_WP` `rot(0,180,180)` | `HeroAnimator_Knight` (15 clips): Idle 2.00s, Atk 1.43s, Heavy 1.67s, **Cast 3.23s** | Trail hardcoded in `ClassVisualArmorKit`; VFX fires at `t=0` before swing windup | **NEED FIX**: Needs bespoke Gothic Longsword+Shield in `Assets/Art/Weapons/`, `WeaponAttachmentProfile`, Cast clip speed/early-exit fix |
| **Guardian (1)** | `Wpn_GuardianMace` (R) + `Wpn_GuardianShield` (L) | Bip001 Generic (12 fingers), `RH_WP`/`LH_WP` same | `HeroAnimator_Guardian` (15 clips): Idle 1.50s, Atk 1.60s, Heavy 1.67s, **Cast 2.17s** | Generic `0.06s` windup too fast for heavy mace; hardcoded tip offset | **NEED FIX**: Bespoke Gothic Heavy Mace + Tower Shield, heavier windup/impact timing, Animator recovery exit |
| **Berserker (2)** | `Wpn_BerserkerGreatsword` (R) | Bip001 Generic (12 fingers), `RH_WP` `+Z=(0.07,0.93,0.36)` | `HeroAnimator_Berserker` (15 clips): Idle 1.67s, Atk 1.17s, Heavy 1.33s, **Cast 3.90s** | `windupDelay=0.06s` way too fast for 2H Greatsword; Cast clip locks character for 3.3s | **NEED FIX**: Bespoke Gothic 2H Greatsword w/ `SecondaryGripPoint`, weighty windup (`0.16s`), Cast speed/cancel |
| **Assassin (3)** | `Wpn_AssassinBladeR` (R) + `Wpn_AssassinBladeL` (L) | Bip001 Generic (12 fingers), `LH_WP` 180° X-flipped | `HeroAnimator_Assassin` (15 clips): Idle 1.50s, Atk 1.00s, Heavy 1.13s, Cast 1.70s | Dual-blade trail & bespoke Assassin VFX exist; needs unified profile | **NEED FIX**: Bespoke Gothic Dual Daggers in `Assets/Art/Weapons/`, verified mirrored `LH_WP` grip |
| **Ranger (4)** | `Wpn_RangerBow` (L only — no arrow) | Bip001 Generic (12 fingers), `LH_WP` `+X=Up`, `+Y=Forward` | `HeroAnimator_Ranger` (15 clips): Idle 1.50s, Atk 0.70s, Heavy 1.33s, **Cast 5.33s** | Projectiles spawn at hardcoded `(0,1.25,0.6)` NOT from bow socket; bowstring rigid | **NEED FIX**: Bespoke Gothic Recurve Bow + Arrow w/ straight riggable bowstring + `ArrowNock` + `ProjectileSpawn`; fix 5.33s Cast lock |
| **Mage (5)** | `Wpn_MageStaff` (R, hardcoded `pos=(0,0,-0.52)`) | Bip001 Generic (**16 fingers**), `RH_WP (-0.069,0.024,0.007)`, `LH_WP (0,0,0)` | `HeroAnimator_Mage` (15 clips): Idle 1.67s, Atk 1.10s, Heavy 1.67s, Cast 0.40s | Projectiles spawn at chest instead of staff tip; hardcoded `-0.52` Z shift | **NEED FIX**: Bespoke Gothic Arcane Staff w/ `MagicEmitSocket` & proper grip origin; Mage unique skeleton verified |
| **Warlock (6)** | `Wpn_WarlockSword` (R) + `WarlockFloatingGrimoire` (Orbit) | Bip001 Generic (12 fingers), `RH_WP` + root floating relic | `HeroAnimator_Warlock` (15 clips): Idle 2.00s, Atk 1.57s, **Heavy 3.77s**, Cast 1.50s | Soul spells spawn at chest, not Grimoire/Sword tip; 3.77s HeavyAttack tail stalls | **NEED FIX**: Bespoke Gothic Grimoire + Ritual Sword w/ `MagicEmitSocket`, fix 3.77s Heavy clip pacing |
| **Monk (7)** | **NONE (`R:NONE | L:NONE`) — MISSING!** | Bip001 Generic (12 fingers), `RH_WP`/`LH_WP` `+Z=Punch Forward` | `HeroAnimator_Monk` (15 clips): Idle 2.33s, Atk 1.17s, Heavy 1.47s, Cast 1.47s | Completely unarmed visually (`AbyssalAutoSetup` passed `null, null`) | **CRITICAL FIX**: Bespoke Gothic Combat Gauntlets (L & R) with knuckle impact sockets + trails |
| **Druid (8)** | `Wpn_DruidStaff` (R, hardcoded `pos=(0,0,-0.52)`) | Bip001 Generic (12 fingers), `RH_WP` same as Knight | `HeroAnimator_Druid` (15 clips): Idle 1.67s, Atk 1.10s, **Heavy 2.23s**, Cast 1.73s | Nature spells spawn at root instead of staff focus; Bear Form preserves swap | **NEED FIX**: Bespoke Gothic Nature Staff w/ `MagicEmitSocket` + grip origin, fix Heavy/Cast sync |
| **Cleric (9)** | `Wpn_ClericHammer` (R) + `Wpn_ClericShield` (L) | Bip001 Generic (12 fingers), `RH_WP`/`LH_WP` | `HeroAnimator_Cleric` (15 clips): Idle 1.50s, Atk 1.60s, Heavy 1.50s, **Cast 4.33s** | Hammer head offset negative; 4.33s Cast clip locks locomotion; Holy VFX not socketed | **NEED FIX**: Bespoke Gothic Holy Hammer + Relic Shield w/ `ImpactPoint` + `MagicEmitSocket`, fix 4.33s Cast |

---

## 2. Root-Cause Findings Across All Systems

1. **Missing & Unprofiled Weapons**:
   - `Monk` has **no weapons attached at all** (`null, null` in `AbyssalAutoSetup.cs:642`).
   - Weapon transforms are hardcoded by prefab name in `AbyssalAutoSetup.cs:848–867` and tip offsets hardcoded by `cls.id` in `ClassVisualArmorKit.cs:121–157`.
   - No `Assets/Art/Weapons/` bespoke Ancient Gothic Dark Fantasy weapon pack or `WeaponAttachmentProfile` ScriptableObject system exists yet.
2. **Animation Jitter & Movement Stutter (4 Root Causes Identified)**:
   - **Double `Controller.Move` per frame**: `ActorUnit.DodgeCoroutine` (`line 864`) and `DashAttackRoutine` (`line 1366`) call `Controller.Move()` directly in coroutines while `UpdateGravityAndVisuals` (`line 617`) ALSO calls `Controller.Move()` in the same frame.
   - **Raw single-frame velocity noise in `CharacterMotionPolisher.LateUpdate`**: `actualPlanarSpeed = planarDelta.magnitude / dt` drives `Speed` and `LocoSpeedMult` with zero smoothing on `actualPlanarSpeed`, causing frame-time spikes to oscillate Walk/Run stride speed.
   - **Non-uniform class `VisualBaseScale` destroyed on frame 1**: `ActorUnit.InitHero` saves scalar `VisualBaseScale = MainVisual.transform.localScale.x`, then `CharacterMotionPolisher` overwrites `localScale` uniformly every `LateUpdate`, popping `Y` proportions (`Guardian`, `Berserker`, `Assassin`, `Druid`).
   - **Animator state machine locks & self-retriggering**: Long `Cast`/`HeavyAttack` clips (`3.23s–5.33s`) have `exitTime = 0.85f` with NO locomotion-cancel exit transition, so characters slide frozen across the floor after casting; `AnyState -> Dodge/TakeDamage` had `canTransitionToSelf = true` and `TakeDamage` could interrupt active attacks/dodges without protection.
3. **Combat Hitbox & VFX Desync**:
   - `TryBasicAttack` and `TryUseSkill` trigger `GnGSkillVfxDispatcher` at `t = 0.00s` before attack/cast anticipation completes.
   - All ranged attacks & projectile skills spawn at hardcoded `transform.position + Vector3.up * 1.25f + dir * 0.6f` instead of weapon sockets (`ProjectileSpawn` / `ArrowNock` / `MagicEmitSocket`).
   - All melee classes share identical `windupDelay = 0.06f` regardless of weapon weight (Berserker Greatsword feels identical to Assassin Daggers).
4. **Class Selection Panel (`MenuAndLobbyViews.cs:622–982`)**:
   - Right panel (`662px` wide) is an overcrowded wall of text (long desc, passive, armor tag, 6-stat pipe text, 4 verbose skill cards, wrong hotkey `W / 2` instead of `2 / Z`, build note, confirm button).
   - Center character preview auto-spins continuously and competes for space with thick busy ornate side panels.

# ABYSSAL DUNGEON 3D — WEAPON VFX ASSET & PIPELINE AUDIT

**Project**: `C:\LTG\Abyssal_Dungeon_3D`  
**Engine & Render Pipeline**: Unity `2021.3.11f1` + Universal Render Pipeline (URP) `12.1.7`  
**Connected DCC**: Blender `5.3.0` via `blender-mcp` (`addon_version: 2.0`, `protocol_version: 16`)  
**Audit Date**: 2026-11-25

---

## 1. Executive Summary & Critical Orientation Findings

1. **Weapon Forward / Blade Length Axis Is NOT Uniform Across Assets**:
   - Most exported hero melee weapons (`Wpn_AssassinBladeR`, `Wpn_AssassinBladeL`, `Wpn_KnightSword`, `Wpn_BerserkerGreatsword`, `Wpn_GuardianMace`, `Wpn_WarlockSword`) extend along **local `+Z`** from the grip origin `(0, 0, 0)`.
   - Two long-hafted weapons (`Wpn_ClericHammer`, `Wpn_DruidStaff`) have negative `-Z` center offsets (`-0.264m` and `-0.272m`), spanning both `-Z` and `+Z` across the hand grip, with `Hero_Mage` and `Hero_Druid` applying `SignatureMainWeapon.localPosition = (0, 0, -0.52)` on the socket child to balance grip placement.
   - `Wpn_RangerBow` spans `2.191m` along local `Z` (`-1.097m .. +1.094m`) centered at the grip `(0.041, 0, -0.002)` and is mounted in `LH_WP -> SignatureOffhand` with quaternion `(0.5, 0.5, 0.5, 0.5)`.
   - `Wpn_WarlockBook` (`1.038m × 0.503m × 0.621m`) is NOT hand-parented; it floats at `WarlockFloatingGrimoire` `(-0.50, 1.42, 0.22)` driven by `ClassRelicFloatAnimator` (`OrbitMode = true`).
   - `Wpn_GargoyleSword` (enemy weapon) extends along **local `-Y`** (`center.y = -0.903m`, `size.y = 3.444m`).
2. **Left-Hand Socket Mirroring (`LH_WP`)**:
   - Across all 10 hero rigs (`Assets/Resources/Models/Heroes/Hero_*.prefab` and `Assets/Prefabs/Classes/Hero_*.prefab`), `RH_WP` has identity local rotation `(0, 0, 0, 1)`, whereas **`LH_WP` has a `180°` X-axis flip `(1, 0, 0, 0)`**. Child VFX attached to `LH_WP` / `SignatureOffhand` must account for inverted local `Y`/`Z` world directions during swings.
3. **Trail Tip Offset Mismatch Discovered in `ClassVisualArmorKit.cs`**:
   - In `ClassVisualArmorKit.cs` (lines 120–141), `WeaponBladeTrail3D.Attach` placed `BladeTrailTip` at `(0, bladeLen * 0.55f, bladeLen * 0.45f)` for main-hand and `(0, 0.28f, 0.22f)` for offhand. Because `Wpn_AssassinBladeR` (`0.064 × 0.046 × 0.657m`) and `Wpn_AssassinBladeL` (`0.063 × 0.046 × 0.597m`) are only `0.046m` thick on `Y` and extend to `+0.530m` on `+Z`, the trail emitter floated `0.28–0.40m` off the flat of the blade instead of tracking the true dagger tip `(0, 0, +0.52m)`. Fixed in Phase 2 integration without altering gameplay hitboxes.

---

## 2. Measured Weapon Prefabs, Meshes & Dimensions

All measurements below were extracted directly from Unity YAML `localAABB` (`m_Center`, `m_Extent * 2`) in meters (`scale = (1, 1, 1)` on all prefabs):

| Weapon Group | Unity Prefab Path | Mesh Asset | Local AABB Center `(x, y, z)` | Dimensions `(W × H × L)` (m) | Grip-to-Tip Span & Axis |
| --- | --- | --- | --- | --- | --- |
| **Assassin — Dual Blades (R)** | `Assets/Art/Exported/Weapons/Wpn_AssassinBladeR.prefab` | `MES_G1P_Wpn_DualBlade01_0101_edc9abe4.asset` | `(0.002, 0.000, +0.202)` | `0.064 × 0.046 × 0.657` | `z: -0.127 → +0.531` (`+Z` blade) |
| **Assassin — Dual Blades (L)** | `Assets/Art/Exported/Weapons/Wpn_AssassinBladeL.prefab` | `MES_G1P_Wpn_DualBlade02_0101_16e4d607.asset` | `(0.001, 0.000, +0.234)` | `0.063 × 0.046 × 0.597` | `z: -0.065 → +0.533` (`+Z` blade, `LH_WP` X-flipped) |
| **Berserker — Greatsword** | `Assets/Art/Exported/Weapons/Wpn_BerserkerGreatsword.prefab` | `MES_G1P_Wpn_Zweihander0101_abebaa31.asset` + `vfx_MES_G1P_Wpn_Zweihander0101_1227be83.asset` | `(0.007, 0.000, +0.725)` | `0.193 × 0.132 × 1.630` total | `z: -0.180 → +1.451` (`+Z` heavy blade) |
| **Cleric — Hammer** | `Assets/Art/Exported/Weapons/Wpn_ClericHammer.prefab` | `MES_G1P_Wpn_Sledgehammer_0101_d56e3e66.asset` | `(0.000, -0.002, -0.264)` | `0.317 × 0.113 × 1.767` | `z: -1.148 → +0.620` (`Z` shaft, `+Z` head) |
| **Cleric — Shield** | `Assets/Art/Exported/Weapons/Wpn_ClericShield.prefab` | `MES_G1P_Wpn_RoundShield_0101_c2b5b4a9.asset` | `(0.000, +0.076, 0.000)` | `0.624 × 0.175 × 0.653` | Round buckler on `LH_WP`, `+Y` face normal |
| **Guardian — Mace** | `Assets/Art/Exported/Weapons/Wpn_GuardianMace.prefab` | `MES_G1P_Wpn_Morningstar_0002_85e328f2.asset` | `(-0.008, +0.002, +0.230)` | `0.153 × 0.143 × 0.747` | `z: -0.144 → +0.604` (`+Z` flanged head) |
| **Guardian — Shield** | `Assets/Art/Exported/Weapons/Wpn_GuardianShield.prefab` | `MES_G1P_Wpn_PaviseShield_0101_ebf2b6b2.asset` | `(-0.113, -0.056, -0.260)` | `0.837 × 0.145 × 1.425` | Tower pavise on `LH_WP` |
| **Knight — Sword** | `Assets/Art/Exported/Weapons/Wpn_KnightSword.prefab` | `MES_G1P_Wpn_Longsword_0101_7ae77166.asset` | `(0.000, 0.000, +0.527)` | `0.337 × 0.052 × 1.718` | `z: -0.332 → +1.386` (`+Z` longsword) |
| **Knight — Shield** | `Assets/Art/Exported/Weapons/Wpn_KnightShield.prefab` | `pasted__a_low_602151ce.asset` | `(+0.062, +0.062, -0.106)` | `0.593 × 0.157 × 0.859` | Heater crest shield on `LH_WP` |
| **Knight — World Drop** | `Assets/Resources/EnvironmentKit/Weap_Knight_RunicLongsword.prefab` | `Weap_Knight_RunicLongsword` | Normalized `0.92m` | `~0.22 × 0.06 × 0.92` | World loot / display prop |
| **Mage — Staff** | `Assets/Art/Exported/Weapons/Wpn_MageStaff.prefab` | `MES_G1P_Wpn_WizardStaff_0101_27046401.asset` | `(-0.005, -0.004, +0.728)` | `0.051 × 0.050 × 0.234` (head) | Socket offset `z = -0.52`; finial tip `z ≈ +0.84m` |
| **Monk — Gloves** | `Assets/Art/Exported/Weapons/Wpn_MonkGloves.prefab` | `MES_G1P_Wpn_BoxingGloves_0001_Floor_9dd5d37b.asset` | `(+0.028, -0.006, -0.022)` | `0.244 × 0.084 × 0.248` | Dual fist sockets `RH_WP` & `LH_WP` (`±0.14m` knuckle) |
| **Ranger — Bow** | `Assets/Art/Exported/Weapons/Wpn_RangerBow.prefab` | `MES_G1P_Wpn_Longbow_0101_649ce19a.asset` | `(+0.041, 0.000, -0.002)` | `0.351 × 0.097 × 2.191` | `LH_WP -> SignatureOffhand` `rot=(0.5,0.5,0.5,0.5)` |
| **Warlock — Book** | `Assets/Art/Exported/Weapons/Wpn_WarlockBook.prefab` | `MES_G1P_Wpn_Book_0101_c2a4d4ac.asset` | `(+0.001, -0.261, +0.001)` | `1.038 × 0.503 × 0.621` | Floating `WarlockFloatingGrimoire` `(-0.50, 1.42, 0.22)` |
| **Warlock — Sword** | `Assets/Art/Exported/Weapons/Wpn_WarlockSword.prefab` | `MES_G1P_CrystalSword_0001_a79ccb1b.asset` | `(0.000, 0.000, +0.132)` | `0.224 × 0.033 × 0.620` | `z: -0.178 → +0.442` (`+Z` crystal ritual blade) |
| **Druid — Scythe-Staff** | `Assets/Art/Exported/Weapons/Wpn_DruidStaff.prefab` | `MES_G1P_Wpn_WizardStaff_SBSprouts_3da86efb.asset` | `(+0.004, +0.001, -0.272)` | `0.110 × 0.071 × 2.041` | Socket offset `z = -0.52`; crown at `+0.75m` |
| **Enemy / Boss Weapons** | `Wpn_DarkKnightSword`, `Wpn_BossShield`, `Wpn_GargoyleSword`, `Wpn_GoblinShovel`, `Wpn_SkeletonSword`, `Wpn_SkeletonShield` | Various `MES_G1P_*` | Varies (`+Z` except Gargoyle `-Y`) | `0.96m – 3.44m` | Used by Dungeon Monsters & 3-Phase Boss |

---

## 3. Character Skeletons, Sockets & Attack Animation Timing

### 3.1 Standardized Hero Bone Hierarchy (`Assets/Resources/Models/Heroes/Hero_*.prefab`)
- **Right Hand**: `Bip001 -> ... -> Bip001 R Hand -> RH_WP -> SignatureMainWeapon`
  - `RH_WP` local position: `(-0.0774, +0.0291, 0.0000)`, local rotation: `(0, 0, 0, 1)`.
- **Left Hand**: `Bip001 -> ... -> Bip001 L Hand -> LH_WP -> SignatureOffhand`
  - `LH_WP` local position: `(-0.0774, +0.0291, 0.0000)`, local rotation: `(1, 0, 0, 0)` (`180°` roll around `X`).

### 3.2 Assassin Animation Clips (`Assets/Art/Exported/Animations/Heroes/Assassin/`) & Combat Windows
All clips sampled at **30 FPS**:
- `Hero_Assassin_Attack.anim`: **`1.000s` (30 frames)** — Light Combo Hit 1 (`WeaponHitboxSweep3D`: `windupDelay = 0.06s`, `activeDuration = 0.12s`, `arc = 132°`, `tilt = -20°`).
- `Hero_Assassin_Attack2.anim`: **`1.067s` (32 frames)** — Light Combo Hit 2 (`windupDelay = 0.06s`, `activeDuration = 0.12s`, reverse sweep, `tilt = +24°`).
- `Hero_Assassin_Attack3.anim`: **`1.433s` (43 frames)** — Light Combo 3 Finisher (`windupDelay = 0.06s`, `activeDuration = 0.16s`, `arc = 170°`, `1.52x` multiplier).
- `Hero_Assassin_HeavyAttack.anim`: **`1.133s` (34 frames)** — Guard-Break Heavy Strike (`windupDelay = 0.15s`, `activeDuration = 0.17s`, `arc = 150°`).
- `Hero_Assassin_Cast.anim`: **`1.700s` (51 frames)** / `Hero_Assassin_SkillAlt.anim`: **`2.000s` (60 frames)** — Poison / Shadow Skill & Ultimate execution.
- **Positional Backstab (`ActorUnit.DealDamageTo`)**: Triggers automatically when `Vector3.Dot(target.forward, attackDir) > 0.42f` (`+20%` damage, `+10%` crit, `"ĐÁNH LÉN!"` callout) — ideal hook for `VFX_Asn_BackstabImpact`.

---

## 4. Existing VFX Architecture, Shaders & Reusable Assets

### 4.1 Runtime Systems (Must Reuse — Zero Duplicate Managers)
- `AbyssalDungeon.VFX.VFXManager` (`Assets/Scripts/VFX/VFXManager.cs`): Enum-keyed (`VFXType`) pooled prefab spawner (`Resources/VFX/Prefabs/VFX_<Type>`) with `ProceduralVFXFactory` fallback and `PooledVfxInstance` zero-GC reset.
- `AbyssalDungeon.VFX.WeaponVfxFactory` (`Assets/Scripts/VFX/WeaponVfxFactory.cs`): Builds the 10 generic `Wpn_*` prefabs, weapon aura sheaths (`AttachSmartWeaponAura`), and 3D projectile visuals (`BuildProjectile3DVisual`).
- `AbyssalDungeon.VFX.GnGSkillVfxDispatcher` (`Assets/Scripts/VFX/GnGSkillVfxDispatcher.cs`): Pooled class-specific combo, heavy, skill, projectile, and hit VFX dispatcher with protected local-player concurrency cap.
- `AbyssGame.VfxQualitySystem` & `ScalableVfxResponder` (`Assets/Scripts/Game/Graphics/VfxQualitySystem.cs`, `ScalableVfxResponder.cs`): Scales particle counts (`Low: 0.35x`, `Medium: 0.65x`, `High: 1.0x`, `Ultra: 1.25x`), secondary layers (`VfxLayerTierFilter`), and lights while preserving 100% visibility when `isCombatTelegraph = true`.
- `AbyssGame.CombatFeelManager` & `WeaponBladeTrail3D` (`CombatFeelManager.cs`, `CombatHitboxSystem.cs`): Hit-stop, pooled `SlashArcRibbonFx`, hurtbox contact sparks, and `TrailRenderer` blade tip ribbons.
- Legacy reference components (`Assets/Scripts/Classes/SkillData.cs`, `Assets/Scripts/Player/PlayerCombat.cs`): Early prototype scripts retained for compatibility; live dungeon gameplay runs on `AbyssGame.ActorUnit` + `GameDB`.

### 4.2 Existing URP VFX Shaders (`Assets/Resources/Shaders/`)
- `AbyssalWeaponTrail.shader` (`AbyssalDungeon/WeaponTrail`): Additive double-sided (`Cull Off`, `ZWrite Off`, `Queue = Transparent+15`) UV-sweeping slash/trail shader (`_MainTex`, `_MainColor`, `_EdgeColor`, `_Progress`, `_Opacity`, `_ScrollSpeed`). Expects `U: 0 → 1` along sweep, `V: 0 → 1` hilt-to-edge.
- `AbyssalWeaponAura.shader` (`AbyssalDungeon/WeaponAura`): Fresnel + scrolling rune + vertex shimmer 3D mesh shader (`_CoreColor`, `_RimColor`, `_ScrollSpeed`, `_FresnelPower`, `_ShimmerAmp`).
- `AbyssalSkillVfx.shader` (`AbyssalDungeon/SkillVfxAdditive`): Additive radial/edge-masked energy shader (`_RadialMask`, `_PulseSpeed`).
- `AbyssalCrystalLit.shader` & `AbyssalGnGEffect.shader`: Lit emissive crystal & particle mesh shaders.

### 4.3 Reusable Existing Assets vs Missing Assets Required for This Production

| Category | Existing Assets in Unity (`Assets/Resources/VFX/`) | Missing Assets to Author in `VFX_Blender_Production` |
| --- | --- | --- |
| **Shared Slashes** | `SM_Vfx_WpnSlashCrescent.fbx`, `SM_Vfx_WpnHeavyCleaveWave.fbx` | Full 6-piece modular set: Thin Slash, Wide Heavy Slash, Sculpted Crescent, Double Slash, Cross X-Slash, Multi-Layer Energy Slash |
| **Shared Impacts & Shockwaves** | `SM_Vfx_ShockCone.fbx`, `SM_Vfx_StoneDebris.fbx` | Full 6-piece set: Weapon Star Impact, Ground Plume Burst, Circular Toroidal Shockwave, 12-Ray Radial Burst, 3D Sunken Ground Crack, Multi-Shard Debris |
| **Shared Magical Meshes** | `SM_Vfx_VortexSpiral.fbx`, `SM_Vfx_WpnSpellBoltCore.fbx` | Full 7-piece set: Rotating Gothic Rune Circle, Gyro Arcane Ring, Geodesic Energy Orb, Aerodynamic Projectile Core, Fluted Magic Beam, Rising Double-Helix Spiral, Ceremonial Summoning Circle |
| **Shared Weapon Trails** | Dynamic `TrailRenderer` (`WeaponBladeTrail3D`) | Full 6-piece 3D swept trail meshes: Curved Blade, Dual-Blade Twin Helix, Greatsword Molten Trail, Rifled Bow Arrow Wake, Spiraling Staff Conduit, Monk Fist Sonic Cuff |
| **Weapon-Specific Class Sets** | Only generic color-swapped `Wpn_*` prefabs | **9 bespoke class weapon VFX sets** with unique geometry, silhouettes, textures, animated `.blend` reference assemblies, manifests & URP prefabs — starting with **Assassin Dual Blades** (6 bespoke effects: Scissor Dual Slash, 5-Arc Rapid Flurry, Hooded Dash Afterimage, Stiletto Backstab Cross, Viper Poison Splash, Shadow-Sickle Burst + Dark Smoke alpha shader support) |
| **Dark Smoke / Shadow Blending** | Only pure Additive (`SrcAlpha One`) weapon shaders (cannot render true black smoke!) | Need alpha-blended / premultiplied shadow-smoke support (`M_Asn_ShadowSmoke`) so Assassin's **obsidian black smoke + crimson/violet/venom** reads with dark contrast instead of washing out white! |

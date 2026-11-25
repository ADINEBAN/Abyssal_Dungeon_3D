# ABYSSAL DUNGEON 3D — WEAPON PRODUCTION, RIGGING, ANIMATION, VFX & CLASS SELECTION UI REPORT

**Project**: `C:\LTG\Abyssal_Dungeon_3D` (`Unity 2021.3.11f1` · `URP 12.1.7` · `Blender 5.3.0`)
**Art Direction**: Ancient Gothic Dark Fantasy (Dark Forged Steel, Antique Gold/Bronze, Carved Leather/Wood, Class Rune Accents)
**Backups**: `Backups/WeaponRigUiRedesign_PreBackup/` (ClassSelection.unity, MenuAndLobbyViews.cs, ClassVisualArmorKit.cs, AbyssalAutoSetup.cs, ActorUnit.cs, CharacterMotionPolisher.cs, CharacterVisualFactory.cs, CombatHitboxSystem.cs)

---

## 1. 17 Bespoke Ancient Gothic 3D Weapons (`Blender 5.3.0` → `Assets/Art/Weapons/`)

Export pipeline: `axis_forward='Z'`, `axis_up='Y'`, `use_space_transform=True`, `bake_space_transform=True` so all blades/hammers/maces/staffs/gauntlets/arrows point cleanly along Unity `+Z`, shields orient top `+Z` / outer face `+Y`, and Ranger's bow curves forward `+Z` with limbs along `±Y`.

Manifest: `Assets/Art/Weapons/weapon_asset_manifest.json`
Generator scripts: `Tools/BuildGothicWeapons_Batch1_Blender.py`, `Tools/BuildGothicWeapons_Batch2_3_Blender.py`

| # | Weapon | Class | Tris | Key Sockets | FBX Path | Editable `.blend` |
|---|---|---|---:|---|---|---|
| 1 | `Knight_Longsword` | Knight | 552 | `GripPoint`, `PrimaryAttach`, `GuardPoint`, `BladeStart`, `BladeTip`, `VfxTrailSocket` | `Assets/Art/Weapons/Batch1/Knight_Longsword.fbx` | `VFX_Blender_Production/02_Weapons/Knight/Sword_Shield/Blender/Knight_Longsword.blend` |
| 2 | `Knight_HeaterShield` | Knight | 428 | `GripPoint`, `PrimaryAttach`, `ForearmAttach`, `ShieldFrontCenter` | `Assets/Art/Weapons/Batch1/Knight_HeaterShield.fbx` | `VFX_Blender_Production/02_Weapons/Knight/Sword_Shield/Blender/Knight_HeaterShield.blend` |
| 3 | `Berserker_Greatsword` | Berserker | 680 | `GripPoint`, `PrimaryAttach`, `SecondaryGripPoint`, `GuardPoint`, `BladeStart`, `BladeTip`, `VfxTrailSocket` | `Assets/Art/Weapons/Batch1/Berserker_Greatsword.fbx` | `VFX_Blender_Production/02_Weapons/Berserker/Greatsword/Blender/Berserker_Greatsword.blend` |
| 4 | `Ranger_RecurveBow` | Ranger | 448 | `GripPoint`, `PrimaryAttach`, `UpperLimbTip`, `LowerLimbTip`, `ArrowNock`, `ProjectileSpawn`, `VfxTrailSocket` + straight riggable 2-segment `BowString_Upper`/`BowString_Lower` | `Assets/Art/Weapons/Batch1/Ranger_RecurveBow.fbx` | `VFX_Blender_Production/02_Weapons/Ranger/Bow/Blender/Ranger_RecurveBow.blend` |
| 5 | `Ranger_Arrow` | Ranger | 140 | `NockPoint`, `ArrowTip` | `Assets/Art/Weapons/Batch1/Ranger_Arrow.fbx` | `VFX_Blender_Production/02_Weapons/Ranger/Bow/Blender/Ranger_Arrow.blend` |
| 6 | `Guardian_HeavyMace` | Guardian | 496 | `GripPoint`, `PrimaryAttach`, `ImpactPoint`, `VfxTrailSocket` | `Assets/Art/Weapons/Batch2/Guardian_HeavyMace.fbx` | `VFX_Blender_Production/02_Weapons/Guardian/Mace_Shield/Blender/Guardian_HeavyMace.blend` |
| 7 | `Guardian_TowerShield` | Guardian | 452 | `GripPoint`, `PrimaryAttach`, `ForearmAttach`, `ShieldFrontCenter` | `Assets/Art/Weapons/Batch2/Guardian_TowerShield.fbx` | `VFX_Blender_Production/02_Weapons/Guardian/Mace_Shield/Blender/Guardian_TowerShield.blend` |
| 8 | `Assassin_DaggerR` | Assassin | 280 | `GripPoint`, `PrimaryAttach`, `BladeTip`, `VfxTrailSocket` | `Assets/Art/Weapons/Batch2/Assassin_DaggerR.fbx` | `VFX_Blender_Production/02_Weapons/Assassin/Dual_Blades/Blender/Assassin_DaggerR.blend` |
| 9 | `Assassin_DaggerL` | Assassin | 292 | `GripPoint`, `PrimaryAttach`, `BladeTip`, `VfxTrailSocket` | `Assets/Art/Weapons/Batch2/Assassin_DaggerL.fbx` | `VFX_Blender_Production/02_Weapons/Assassin/Dual_Blades/Blender/Assassin_DaggerL.blend` |
| 10 | `Mage_ArcaneStaff` | Mage | 648 | `GripPoint`, `PrimaryAttach`, `MagicEmitSocket`, `ProjectileSpawn`, `VfxTrailSocket` | `Assets/Art/Weapons/Batch2/Mage_ArcaneStaff.fbx` | `VFX_Blender_Production/02_Weapons/Mage/Staff/Blender/Mage_ArcaneStaff.blend` |
| 11 | `Warlock_RitualSword` | Warlock | 236 | `GripPoint`, `PrimaryAttach`, `BladeTip`, `MagicEmitSocket`, `VfxTrailSocket` | `Assets/Art/Weapons/Batch3/Warlock_RitualSword.fbx` | `VFX_Blender_Production/02_Weapons/Warlock/Book_Sword/Blender/Warlock_RitualSword.blend` |
| 12 | `Warlock_Grimoire` | Warlock | 380 | `GripPoint`, `PrimaryAttach`, `MagicEmitSocket` | `Assets/Art/Weapons/Batch3/Warlock_Grimoire.fbx` | `VFX_Blender_Production/02_Weapons/Warlock/Book_Sword/Blender/Warlock_Grimoire.blend` |
| 13 | `Monk_GauntletR` | Monk | 284 | `GripPoint`, `PrimaryAttach`, `ImpactPoint`, `VfxTrailSocket` | `Assets/Art/Weapons/Batch3/Monk_GauntletR.fbx` | `VFX_Blender_Production/02_Weapons/Monk/Gloves/Blender/Monk_GauntletR.blend` |
| 14 | `Monk_GauntletL` | Monk | 284 | `GripPoint`, `PrimaryAttach`, `ImpactPoint`, `VfxTrailSocket` | `Assets/Art/Weapons/Batch3/Monk_GauntletL.fbx` | `VFX_Blender_Production/02_Weapons/Monk/Gloves/Blender/Monk_GauntletL.blend` |
| 15 | `Druid_NatureStaff` | Druid | 388 | `GripPoint`, `PrimaryAttach`, `MagicEmitSocket`, `ProjectileSpawn`, `VfxTrailSocket` | `Assets/Art/Weapons/Batch3/Druid_NatureStaff.fbx` | `VFX_Blender_Production/02_Weapons/Druid/Staff/Blender/Druid_NatureStaff.blend` |
| 16 | `Cleric_HolyHammer` | Cleric | 332 | `GripPoint`, `PrimaryAttach`, `ImpactPoint`, `MagicEmitSocket`, `VfxTrailSocket` | `Assets/Art/Weapons/Batch3/Cleric_HolyHammer.fbx` | `VFX_Blender_Production/02_Weapons/Cleric/Hammer_Shield/Blender/Cleric_HolyHammer.blend` |
| 17 | `Cleric_RelicShield` | Cleric | 668 | `GripPoint`, `PrimaryAttach`, `ForearmAttach`, `ShieldFrontCenter` | `Assets/Art/Weapons/Batch3/Cleric_RelicShield.fbx` | `VFX_Blender_Production/02_Weapons/Cleric/Hammer_Shield/Blender/Cleric_RelicShield.blend` |

### URP 12.1.7 PBR Textures & Materials
- **Textures (`512×512`, `Assets/Art/Weapons/Textures/`)**: `T_Wpn_{DarkSteel,AntiqueGold,LeatherWood}_{Albedo,Normal,Mask}.png` (9 PBR maps) + `T_Wpn_GothicRune_Emission.png` (10 total).
- **Materials (`Assets/Art/Weapons/Materials/`)**: `M_Wpn_DarkSteel.mat`, `M_Wpn_AntiqueGold.mat`, `M_Wpn_LeatherGrip.mat`, `M_Wpn_DarkWood.mat`, `M_Wpn_Parchment.mat`, `M_Wpn_BowString.mat`, `M_Wpn_Rune{Arcane,Crimson,Emerald,Gold,Holy,Violet}.mat` (12 URP Lit materials).
- **Prefabs**: 17 synced weapon prefabs in `Assets/Art/Weapons/Prefabs/` and `Assets/Resources/Weapons/Prefabs/` (all 45 weapon + hero prefabs audited with 0 missing scripts).
- **Studio Preview PNGs**: 17 `768×768` previews under `VFX_Blender_Production/02_Weapons/<Class>/<Group>/Previews/` and copied to `Captures/WeaponUiRedesign/Previews/`.
- **Legacy Preservation**: No files under `Assets/Art/Exported/` were deleted or overwritten.

---

## 2. Unified Weapon Rigging & Socket Architecture

Each serialized type resides in its exact matching `.cs` file:
- `Assets/Scripts/Game/Character/WeaponAttachmentProfile.cs` — `WeaponGripType` enum (all 9 grip styles) + `WeaponAttachmentProfile` ScriptableObject (`16` assets: `WAP_*.asset`).
- `Assets/Scripts/Game/Character/ClassWeaponLoadoutProfile.cs` — per-class weapon slots, projectile visual prefab (`Ranger_Arrow`), and synchronized combat timing (`10` assets: `Loadout_*.asset` under `Assets/Resources/Weapons/Profiles/`).
- `Assets/Scripts/Game/Character/BowStringRigAnimator.cs` — live straight 2-segment bowstring pullback (`BowString_Upper`/`BowString_Lower` tracking `UpperLimbTip`, `LowerLimbTip`, `ArrowNock`) plus nocked `Ranger_Arrow` visual during draw windup.
- `Assets/Scripts/Game/Character/WeaponRigController.cs` — dynamic bone lookup (`RH_WP`, `LH_WP` + fallbacks), rotated `GripPoint` alignment, world-space `ProjectileSpawn`/`MagicEmitSocket`/`ImpactPoint` queries, blade trails on tip sockets, blade aura sheaths restricted strictly to bladed melee weapons (`OneHandedSword`, `GreatswordTwoHanded`, `DualWield`), loud `Debug.LogError` if any class lacks a mounted weapon.
- `Assets/Scripts/Game/Character/ClassVisualArmorKit.cs` — delegates weapon mounting & trails cleanly to `WeaponRigController.BindToHero`.
- `Assets/Scripts/Game/Editor/GothicWeaponAssetSetup.cs` — Editor pipeline (`Tools/Abyssal Dungeon/3. Build Gothic Weapons, Profiles & Smooth Animators`) rebuilding all URP materials, 17 prefabs, 16 `WAP_*`, 10 `Loadout_*`, hero prefabs, and calibrated Animator Controllers.

### Key Rigging Fixes Verified Live
1. **Reversed Shields & Upside-Down Staffs/Swords**: Root cause was FBX export `axis_forward='-Z'`; fixed to `axis_forward='Z'` and verified all 10 hero live `CombatIdle` socket vectors.
2. **Monk Missing Weapon Bug**: Built & mounted `Monk_GauntletR` (`RH_WP`) and `Monk_GauntletL` (`LH_WP`) with knuckle spikes forward.
3. **Ranger Bow Alignment & Hand Grip**: `Hero_Ranger`'s `LH_WP` bone carries a 29° backward pitch (`+X=(0.01, 0.87, -0.49)`) and her clenched left fist ring sits at `LH_WP = (0.000, -0.005, +0.016)`. Calibrated `WAP_Ranger_RecurveBow` to `LocalPosition=(0.000, -0.005, 0.016)`, `LocalRotation=(304, 270, 354)`, `LocalScale=0.85`, seating `RangerBow_Grip` dead-center inside her clenched fingers (`0.000m` gap) while keeping the bow 99.5% upright (`charUp=(-0.05, 0.995, 0.09)`, `charFwd=(-0.26, -0.10, 0.96)`) with `9.1cm` forearm clearance and `28.4cm` shoulder clearance.
4. **Black URP Floor Decals**: Fixed `PbrMaterialLibrary.SurfaceDecal` (`renderQueue=2990`) and `SoftShadowDecalMaterial` (`renderQueue=2992`) so URP alpha-blends floor decals properly instead of opaque black squares.

---

## 3. Animation Smoothing & Combat/VFX Synchronization

- **Animator Controllers (`HeroAnimator_*.controller`, 10 classes)**:
  - Calibrated playback speed on 3.2s–5.3s cast/heavy clips so active attacks feel responsive.
  - Added early locomotion-cancel transitions (`Speed > 0.18`, `TransitionInterruptionSource.Destination`) out of Attack/Heavy/Cast recovery windows.
  - Disabled `canTransitionToSelf` on `Dodge`, `Jump`, `TakeDamage`, `Death` to eliminate re-trigger pops.
- **Locomotion Filtering (`Assets/Scripts/Game/Player/CharacterMotionPolisher.cs`)**:
  - Low-pass filtered planar velocity (`_filteredPlanarSpeed`) and stride multiplier (`_smoothedStrideMult`) to stop Walk/Run threshold micro-jitter.
  - Preserved non-uniform hero proportions via `Vector3 VisualBaseScaleVec` (no uniform-scale pop).
- **Unified Motion & Synced Combat VFX (`Assets/Scripts/Game/Combat/ActorUnit.cs`)**:
  - Merged `DodgeCoroutine` and `DashAttackRoutine` into `_pendingPlanarMove` to eliminate double `CharacterController.Move` jitter per frame.
  - Bound `WeaponRigController WeaponRig` and synchronized `GnGSkillVfxDispatcher`, melee impacts, and ranged projectiles (`DelayedRangedBasicShot`, `DelayedHeavyRangedShot`, `DelayedSkillProjectileVolley`) to per-class active windup (`GetCombatTiming`) + live bowstring draw (`BowRig.TriggerDraw`), spawning arrows from `WeaponRig.GetProjectileSpawnWorldPos` with the bespoke `Ranger_Arrow` 3D mesh.

---

## 4. Class Selection UI Redesign (`1920×1080` Gothic Dark Fantasy)

File: `Assets/Scripts/Game/UI/MenuAndLobbyViews.cs` (`ClassSelectSceneView`)
- **Left (396px)**: Clean `2×5` class grid with antique gold active state & dark charcoal-stone inactive cards.
- **Center (54% unobstructed Sanctuary Stage)**: 3/4 hero preview, smooth left-drag rotation, 3-point warm/moonlight showcase lighting, compact bottom Action Dock (`Đánh / Chiêu / Né / Xoay ↺`) below the hero's boots.
- **Right (460px)**: Single antique-gold border, 1-sentence class summary, 4 clean visual stat bars (`HP / ATK / DEF / MOBILITY`, fixing raw `51f` speed display → `5.8`), 4 compact skill rows (`[Q / 1]`, `[2 / Z]`, `[E / 3]`, `[R / 4 ★]`) with interactive hover/click tooltip (`SkillTooltipPopup`), 3 Build Specialization tabs, and intact Confirm / Back navigation.

---

## 5. Play Mode Verification Matrix (All 10 Classes)

Verified in Unity `2021.3.11f1` Play Mode across both `ClassSelection.unity` and `Dungeon.unity`:
- **Console Errors**: **0** (both Play Mode and Edit Mode).
- **Missing Scripts**: **0** across all 45 scanned weapon + hero prefabs.
- **Combat Smoke Test (`Dungeon.unity`)**: `MoveWorld` = OK, `TryBasicAttack` = `True`, `TryHeavyAttack` = `True`, `TryDodge` = `True`, `TryUseSkill(0..3)` = `4/4`, Ranger `BowRig.TriggerDraw` + `ProjectileSpawnWorldPos` = OK.

| ID | Class | Loadout Asset | Main Weapon | Offhand Weapon | Weapon Renderers | Animator Controller | Play Mode Visual |
|---:|---|---|---|---:|---:|---|---|
| 0 | Knight | `Loadout_Knight` | `Knight_Longsword` | `Knight_HeaterShield` | 11 | `HeroAnimator_Knight` | PASS (`03_After_ClassSelect_Knight.png`, `07_After_Dungeon_PartyWeapons.png`) |
| 1 | Guardian | `Loadout_Guardian` | `Guardian_HeavyMace` | `Guardian_TowerShield` | 9 | `HeroAnimator_Guardian` | PASS (`03b_After_ClassSelect_Guardian.png`) |
| 2 | Berserker | `Loadout_Berserker` | `Berserker_Greatsword` | — | 7 | `HeroAnimator_Berserker` | PASS (`04_After_ClassSelect_Berserker.png`) |
| 3 | Assassin | `Loadout_Assassin` | `Assassin_DaggerR` | `Assassin_DaggerL` | 14 | `HeroAnimator_Assassin` | PASS (`04b_After_ClassSelect_Assassin.png`) |
| 4 | Ranger | `Loadout_Ranger` | — | `Ranger_RecurveBow` (+ `BowStringRigAnimator`) | 10 | `HeroAnimator_Ranger` | PASS (`02_After_ClassSelect_Ranger_Front.png`, `02b_After_ClassSelect_Ranger_Back.png`, `07_After_Dungeon_PartyWeapons.png`) |
| 5 | Mage | `Loadout_Mage` | `Mage_ArcaneStaff` | — | 4 | `HeroAnimator_Mage` | PASS (`06_After_ClassSelect_Mage.png`, `07_After_Dungeon_PartyWeapons.png`) |
| 6 | Warlock | `Loadout_Warlock` | `Warlock_RitualSword` | `Warlock_Grimoire` (orbiting) | 11 | `HeroAnimator_Warlock` | PASS (`06b_After_ClassSelect_Warlock.png`) |
| 7 | Monk | `Loadout_Monk` | `Monk_GauntletR` | `Monk_GauntletL` | 10 | `HeroAnimator_Monk` | PASS (`05_After_ClassSelect_Monk.png`) |
| 8 | Druid | `Loadout_Druid` | `Druid_NatureStaff` | — | 4 | `HeroAnimator_Druid` | PASS (`06c_After_ClassSelect_Druid.png`) |
| 9 | Cleric | `Loadout_Cleric` | `Cleric_HolyHammer` | `Cleric_RelicShield` | 9 | `HeroAnimator_Cleric` | PASS (`06d_After_ClassSelect_Cleric.png`, `07_After_Dungeon_PartyWeapons.png`) |

### Before & After Captures (`Captures/WeaponUiRedesign/`)
- **Before**: `01_Before_ClassSelect_UI.png`
- **After (Class Selection — All 10 Classes + Ranger Back)**: `02_After_ClassSelect_Ranger_Front.png`, `02b_After_ClassSelect_Ranger_Back.png`, `03_After_ClassSelect_Knight.png`, `03b_After_ClassSelect_Guardian.png`, `04_After_ClassSelect_Berserker.png`, `04b_After_ClassSelect_Assassin.png`, `05_After_ClassSelect_Monk.png`, `06_After_ClassSelect_Mage.png`, `06b_After_ClassSelect_Warlock.png`, `06c_After_ClassSelect_Druid.png`, `06d_After_ClassSelect_Cleric.png`
- **After (Dungeon Co-op Party)**: `07_After_Dungeon_PartyWeapons.png`

---

## 6. Known Limitations (Honest Scope Notes)

1. **Generic Bip001 Skeletons (No Humanoid Avatar IK)**: All 10 imported hero models use Generic `Bip001` rigs rather than Unity Humanoid Avatars, so two-handed left-hand grip on `Berserker_Greatsword` (`SecondaryGripPoint` socket is present on the weapon) and Ranger's right-hand string-pull follow animation clips + `BowStringRigAnimator` rather than runtime Humanoid `Animator.SetIKPosition` / Two-Bone IK constraints.
2. **LAN Multiplayer Verification**: Verified locally in `Dungeon.unity` with a 4-hero co-op party (`1` Local Player + `3` AI Hunters) sharing the same `WeaponRigController` and `ActorUnit` prefab pipeline; multi-machine physical LAN testing was not run in this single-editor session.
3. **Blender Generation Transport**: Local Blender 5.3.0 CLI (`blender.exe --background --python ...`) executed the `.blend` / FBX / PBR / preview pipeline because the interactive Blender GUI MCP socket (`localhost:9876`) was offline during this session.

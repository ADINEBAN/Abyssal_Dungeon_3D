# ABYSSAL DUNGEON 3D — BLENDER WEAPON VFX PRODUCTION REPORT (PHASE 1 & PHASE 2)

**Project**: `C:\LTG\Abyssal_Dungeon_3D`  
**Engine & Render Pipeline**: Unity `2021.3.11f1` + URP `12.1.7`  
**Blender Pipeline**: Blender `5.3.0` (`blender-mcp` live connection verified, `addon_version: 2.0`, `protocol_version: 16`)  
**Scope Executed & Verified**: Phase 1 (Production Directory Hierarchy + Quantitative Unity Weapon Audit + 25 Shared VFX Foundation Assets) & Phase 2 (Complete **Assassin — Dual Blades** 6-Effect Set, Sculpted Reference Daggers, 6-Stage Keyframed Assembly Animation, Previews, Manifests & Unity URP Integration).

---

## 1. Created Directory Structure (`VFX_Blender_Production/`)

Verified on disk (`117` directories created under `C:\LTG\Abyssal_Dungeon_3D\VFX_Blender_Production\`):
- `00_References/`
- `01_Shared_VFX/` (`Slash/`, `Impact/`, `Shockwave/`, `Energy/`, `Projectile/`, `Rune/`, `Debris/`)
- `02_Weapons/` (`Assassin/Dual_Blades`, `Berserker/Greatsword`, `Cleric/Hammer_Shield`, `Guardian/Mace_Shield`, `Knight/Sword_Shield`, `Mage/Staff`, `Monk/Gloves`, `Ranger/Bow`, `Warlock/Book_Sword` — each containing `Blender/`, `Meshes/`, `Materials_Reference/`, `Textures/`, `Animations/`, `FBX/`, `Previews/`)
- `03_Characters/`, `04_Enemies_Bosses/`, `05_Environment_VFX/`
- `06_Textures/`
- `07_Exports_Unity/` (`Shared/` + all 9 `[Class]/[Weapon]/` export roots)
- `08_Previews/`
- `09_Documentation/` (`WEAPON_VFX_ASSET_AUDIT.md`, `WEAPON_VFX_PRODUCTION_REPORT.md`)

---

## 2. Actual Unity Assets Inspected

Documented in full in [WEAPON_VFX_ASSET_AUDIT.md](WEAPON_VFX_ASSET_AUDIT.md):
- **21 Weapon Prefabs & Meshes**: Measured exact `localAABB` centers, extents, and grip-to-tip axes across `Assets/Art/Exported/Weapons/*.prefab` and `Weap_Knight_RunicLongsword.prefab`.
  - Confirmed `Wpn_AssassinBladeR` (`0.064 × 0.046 × 0.657m`, tip `+0.531m Z`) and `Wpn_AssassinBladeL` (`0.063 × 0.046 × 0.597m`, tip `+0.533m Z`) extend along local `+Z`, with `LH_WP` carrying a `180°` X-roll `(1, 0, 0, 0)`.
  - Uncovered and fixed a trail-tip offset bug in `ClassVisualArmorKit.cs` where `BladeTrailTip` had been placed in `+Y/+Z` diagonal space instead of along the measured `+Z` blade axis.
- **11 Assassin Animation Clips** (`30 FPS`): `Attack` (`1.00s`), `Attack2` (`1.067s`), `Attack3` (`1.433s`), `HeavyAttack` (`1.133s`), `Cast` (`1.70s`), `SkillAlt` (`2.00s`), `Idle`, `LobbyIdle`, `Walk`, `Run`, `Jump`.
- **Existing VFX & Shaders Reused**: `VFXManager`, `ProceduralVFXFactory`, `WeaponVfxFactory`, `GnGSkillVfxDispatcher`, `VfxQualitySystem`, `ScalableVfxResponder`, `CombatFeelManager`, `WeaponBladeTrail3D`, `AbyssalWeaponTrail.shader`, `AbyssalWeaponAura.shader`, `AbyssalSkillVfx.shader`.

---

## 3. Blender Scenes Created (`.blend`)

1. **`01_Shared_VFX/Abyssal_Shared_VFX_Library.blend`** — Editable 5×5 showcase & source scene containing all 25 modular Shared VFX meshes, reference emissive shaders, obsidian studio floor, and `PreviewCamera_Shared`.
2. **`02_Weapons/Assassin/Dual_Blades/Blender/Assassin_DualBlades_VFX_Assembly.blend`** (also exported to `07_Exports_Unity/Assassin/Dual_Blades/`) — Full Reference Assembly scene containing sculpted right & left Gothic Kris/Stiletto daggers (`WpnRef_Assassin_Dagger_R` & `L`), dual blade trails, charge sigil, all 6 bespoke Assassin VFX meshes, 60-frame 6-stage animation, lighting, and `PreviewCamera_Assassin`.

---

## 4. Meshes Created (33 Custom Blender 5.3.0 Meshes)

### 4.1 Shared VFX Foundation (`25` Meshes — `01_Shared_VFX/` & `07_Exports_Unity/Shared/`)
- **Slashes (6)**: `SM_Shared_Slash_Thin`, `SM_Shared_Slash_WideHeavy`, `SM_Shared_Slash_Crescent`, `SM_Shared_Slash_Double`, `SM_Shared_Slash_Cross`, `SM_Shared_Slash_MultiEnergy`
- **Impacts, Shockwaves & Debris (6)**: `SM_Shared_Impact_WeaponStar`, `SM_Shared_Impact_GroundBurst`, `SM_Shared_Shockwave_Circular`, `SM_Shared_Impact_RadialBurst`, `SM_Shared_Impact_GroundCrack`, `SM_Shared_Debris_Cluster`
- **Magical Effects (7)**: `SM_Shared_Rune_GothicCircle`, `SM_Shared_Energy_ArcaneRing`, `SM_Shared_Energy_Orb`, `SM_Shared_Projectile_Core`, `SM_Shared_Energy_Beam`, `SM_Shared_Energy_Spiral`, `SM_Shared_Rune_SummoningCircle`
- **Weapon Trails (6)**: `SM_Shared_Trail_CurvedBlade`, `SM_Shared_Trail_DualBlades`, `SM_Shared_Trail_GreatswordHeavy`, `SM_Shared_Trail_BowArrow`, `SM_Shared_Trail_StaffEnergy`, `SM_Shared_Trail_GloveFist`

### 4.2 Assassin — Dual Blades (`8` Meshes — `02_Weapons/Assassin/Dual_Blades/`)
- **Representative Weapons (2)**: `WpnRef_Assassin_Dagger_R` (`0.657m`), `WpnRef_Assassin_Dagger_L` (`0.597m`) — sculpted curved Kris/Stiletto geometry matching Unity `+Z` blade orientation.
- **Bespoke Assassin VFX (6)**:
  1. `SM_Asn_DualBladeSlash` — Asymmetric crisscrossing scissor-crescent slash with predator claw barbs.
  2. `SM_Asn_RapidComboTrails` — 5-ribbon high-velocity alternating laceration cage.
  3. `SM_Asn_DashAfterimage` — Forward-leaning hooded shadow-stalker phantom cowl, 5 tattered cloak streamers & twin thrusting dagger wakes.
  4. `SM_Asn_BackstabImpact` — Twin `±38°` crossing stiletto puncture spikes driving through a shattered 8-point Gothic blood-sigil.
  5. `SM_Asn_PoisonBladeImpact` — Twin downward viper-fang arcs + 9 curved toxic venom splash plumes.
  6. `SM_Asn_ShadowBurst` — 8 outward-spiraling obsidian smoke-sickle blades + jagged void vortex crown.

---

## 5. Materials and Textures Created

### 5.1 Textures (`512×512` RGBA PNGs with Mipmaps & Alpha Transparency)
- **Shared (`06_Textures/` & `Assets/Resources/VFX/Textures/`)**:
  - `T_Shared_GothicSlashArc.png`
  - `T_Shared_GothicRuneSeal.png`
  - `T_Shared_ShockwaveBurst.png`
- **Assassin (`02_Weapons/Assassin/Dual_Blades/Textures/` & `Assets/Resources/VFX/Textures/`)**:
  - `T_Asn_CrimsonShadowSlash.png`
  - `T_Asn_VenomSplatter.png`
  - `T_Asn_ShadowSmokeWisps.png`
  - `T_Asn_BackstabSigil.png`

### 5.2 URP Material Specifications (`WeaponVfxFactory.cs` & `Materials_Reference/Assassin_URP_Materials_Spec.json`)
- `M_Asn_CrimsonSlash` (`AbyssalDungeon/WeaponTrail`, Additive `SrcAlpha One`, Crimson `#D9192E` core + Shadow Purple `#6B1FB8` edge)
- `M_Asn_ShadowSmoke` (`Universal Render Pipeline/Particles/Unlit`, **Alpha Blend `SrcAlpha OneMinusSrcAlpha`** — renders true dark obsidian black-violet smoke without washing white!)
- `M_Asn_BackstabCore` (`AbyssalDungeon/WeaponAura`, Additive HDR crimson stiletto sigil)
- `M_Asn_VenomSplash` (`AbyssalDungeon/WeaponAura`, Additive Emerald `#28E65C` core + Shadow Violet `#6B1FB8` rim)
- `M_Asn_ShadowPhantom` (`AbyssalDungeon/WeaponAura`, Fresnel shadow-purple cowl + crimson rim)

---

## 6. Animations Created

- **`Anim_Asn_DualBlades_6StageShowcase.fbx`** (`60 frames @ 30 FPS = 2.0s` baked transform/scale animation in `02_Weapons/Assassin/Dual_Blades/Animations/` & `07_Exports_Unity/Assassin/Dual_Blades/`):
  - **Anticipation (`f1–f8`)**: Reverse-grip offhand + forward-grip mainhand draw back; shadow rune gathers.
  - **Charge (`f9–f18`)**: Crossguard sigil coils & spins at peak intensity.
  - **Release (`f19–f28`)**: X-scissor dual slash & 5-ribbon rapid combo flurry erupt forward.
  - **Travel (`f29–f38`)**: Hooded shadow-stalker afterimage lunges `2.15m` along `-Y` (`+Z` Unity).
  - **Impact (`f39–f48`)**: Stiletto backstab cross, venom fang crown, and 8-sickle shadow burst detonate at target point.
  - **Dissipation (`f49–f60`)**: Smooth expansion & scale-down recovery.

---

## 7. FBX Files & Manifests Exported

All FBX exports used `-Z Forward, Y Up`, `bake_space_transform=True`, `object_types={'MESH'}`, `add_leaf_bones=False` (zero cameras/lights inside FBX):
- `25` Shared FBXs -> `07_Exports_Unity/Shared/SM_Shared_*.fbx` + `shared_vfx_manifest.json`
- `6` Assassin VFX FBXs + `1` Animated Showcase FBX -> `07_Exports_Unity/Assassin/Dual_Blades/` + `assassin_dual_blades_manifest.json`
- `2` Reference Weapon FBXs -> `02_Weapons/Assassin/Dual_Blades/Meshes/WpnRef_Assassin_Dagger_R.fbx` & `L.fbx`

---

## 8. Unity Assets Imported & VFX Prefabs Integrated

- **Imported into Unity (`32` FBXs + `7` PNGs with clean `.meta` files, `materialImportMode: 0`, `addCollider: 0`, `importCameras: 0`, `importLights: 0`)**:
  - `Assets/Resources/VFX/Meshes/Shared/SM_Shared_*.fbx` (`25` files)
  - `Assets/Resources/VFX/Meshes/Assassin/SM_Asn_*.fbx` + `Anim_Asn_DualBlades_6StageShowcase.fbx` (`7` files)
  - `Assets/Resources/VFX/Textures/T_Shared_*.png` & `T_Asn_*.png` (`7` files)
- **6 New Pooled Assassin URP VFX Types & Prefab Builders Integrated**:
  - `VFXType.Wpn_Asn_DualBladeSlash` (`VFX_Wpn_Asn_DualBladeSlash.prefab`) — Combo 1 & 2 + Dash slash
  - `VFXType.Wpn_Asn_RapidCombo` (`VFX_Wpn_Asn_RapidCombo.prefab`) — Combo 3 Finisher, Heavy Attack & MeleeArc Flurry
  - `VFXType.Wpn_Asn_DashAfterimage` (`VFX_Wpn_Asn_DashAfterimage.prefab`) — Assassin Dodge & Dash skills (`CharacterMotionPolisher` + `GnGSkillVfxDispatcher`)
  - `VFXType.Wpn_Asn_BackstabImpact` (`VFX_Wpn_Asn_BackstabImpact.prefab`, `isCombatTelegraph = true`) — Positional Backstab (`ActorUnit.DealDamageTo`)
  - `VFXType.Wpn_Asn_PoisonImpact` (`VFX_Wpn_Asn_PoisonImpact.prefab`, `isCombatTelegraph = true`) — Poison status hits & Poison skills
  - `VFXType.Wpn_Asn_ShadowBurst` (`VFX_Wpn_Asn_ShadowBurst.prefab`, `isCombatTelegraph = true`) — Assassin Ultimate & SelfAoE skills
- **Auto-Baking & QA**: Registered in `Assets/Scripts/Editor/WeaponVfxBuilder.cs` (`WpnVfxDurationFixedV3.txt`) so Unity Editor automatically serializes `.mat` and `.prefab` assets into `Assets/Resources/VFX/Prefabs/` on focus while `VFXManager` / `WeaponVfxFactory` also instantiate & pool them immediately at runtime with zero GC.

---

## 9. Preview Images Rendered (`1600×1000` Blender 5.3.0 EEVEE)

1. `08_Previews/Preview_Shared_VFX_Library.png` — All 25 Shared VFX building blocks on 5×5 dark obsidian grid.
2. `08_Previews/Preview_Assassin_6Effects_Sheet.png` (and `02_Weapons/Assassin/Dual_Blades/Previews/`) — 3×2 gallery of all 6 distinct Assassin Dual Blades 3D meshes.
3. `08_Previews/Preview_Assassin_ReleaseSlash_f24.png` — Reference Assembly frame 24 showing sculpted twin daggers executing X-scissor slash & flurry ribbons.
4. `08_Previews/Preview_Assassin_ImpactBurst_f44.png` — Reference Assembly frame 44 showing lethal stiletto backstab sigil, emerald venom splash, and spiraling shadow-sickle burst.

---

## 10. Errors, Fixes & Tooling Limitations

1. **Blender 5.3.0 `RenderSettings.bl_rna` Attribute**: During initial Shared library script execution, `scene.render.blrna` raised `AttributeError` (`bl_rna` in Blender 5.3 Python API) after exporting all 25 FBXs; immediately corrected to `scene.render.bl_rna` and completed EEVEE render + `.blend` save cleanly.
2. **MCP Server Availability**: `blender-mcp` is live and used for all 3D geometry, materials, animations, FBX exports, and viewport/frame renders. `unityMCP` is not registered in the active MCP server configuration for this session (`blender-mcp` and `drawio` only); therefore Unity inspection, `.meta` generation, `WeaponVfxBuilder` prefab serialization wiring, and direct Roslyn C# compilation (`csc.dll` against Unity's `Assembly-CSharp.rsp`) were executed directly on the project filesystem with zero compile errors.
3. **Gameplay Safety**: Zero colliders on VFX meshes/prefabs; combat damage, hitboxes, cooldowns, and LAN multiplayer authority remain 100% untouched.

---

## 11. Next Production Phase (Phase 3)

- **Phase 3 — Berserker Greatsword (`02_Weapons/Berserker/Greatsword/`)**:
  - Measured Unity target: `Wpn_BerserkerGreatsword.prefab` (`0.193 × 0.132 × 1.630m`, tip at `+1.451m Z`).
  - Author bespoke molten blood-red / dark-orange / ember-ash 3D meshes in Blender 5.3.0: Heavy Sword Arc, Ground Splitting Crack, Heavy Impact Shockwave, Berserk Jagged Aura, Greatsword Charged Attack, and Ultimate Volcanic Ground Explosion -> integrate & verify in Unity URP, then proceed one weapon group at a time through Phase 4 (`Cleric`, `Guardian`, `Knight`, `Mage`, `Monk`, `Ranger`, `Warlock`).

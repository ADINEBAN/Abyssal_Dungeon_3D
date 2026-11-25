# Shared Modular VFX Architecture — Reuse, Naming & Pooling

Instead of building 200 one-off prefabs that bloat draw calls and VRAM, assemble Abyssal Dungeon 3D's entire VFX library out of **15 modular, recolorable building blocks** paired with our **14 shared alpha masks** (`Assets/Resources/VFX/Textures/T_Vfx_*.png`) and **5 core URP shaders**.

---

## 1. The 15 Reusable VFX Modules

| # | Module ID | Core Mesh / Component | Shared Texture (`T_Vfx_*`) | Reused Across |
| --- | --- | --- | --- | --- |
| `01` | `Mod_SlashCrescent` | Swept arc mesh / billboard | `T_Vfx_SlashCrescent`, `T_Vfx_BladeSlash` | All melee combos, Cleaves, Boss Dark Slash |
| `02` | `Mod_BladeWave` | Forward-traveling arc ribbon | `T_Vfx_BladeWave` | Knight/Berserker wave projectiles, Boss Phase 2 wave |
| `03` | `Mod_SparkCone` | Stretched billboard `ParticleSystem` | Soft dot / `T_Vfx_StarFlare` | Hit sparks, Shield block, Metal footsteps, Chest burst |
| `04` | `Mod_RadialShockRing` | Horizontal UV ring quad | `T_Vfx_ShockRing`, `T_Vfx_RadialBurstRing` | Landings, Slams, Parry, Boss Shockwave, Loot/Heal pulse |
| `05` | `Mod_GroundRuneCircle` | Ground `y=+0.02m` gothic decal | `T_Vfx_GothicRuneCircle` | Telegraphs, Summon rifts, Level-up base, Rune pillars |
| `06` | `Mod_GroundCrack` | Ground fracture decal + chips | `T_Vfx_GroundCrack` | Earthbreaker, Guardian slam, CrystalGolem, Boss impact |
| `07` | `Mod_PillarBeam` | Open-ended vertical cylinder | `T_Vfx_PillarBeam` | Cleric Smite, Level-Up, Legendary/Mythic loot beams, Portals |
| `08` | `Mod_NovaDome` | Inverted hemisphere / dome | `T_Vfx_NovaWave`, `T_Vfx_FrostFlower` | Guardian Sanctuary/Aegis, Frost Nova, Soul Anchor, Bear morph |
| `09` | `Mod_AuraOrb` | Core glow sphere + orbiting motes | `T_Vfx_EnergyTrail`, `T_Vfx_StarFlare` | Mage/Warlock projectiles, Healing Shrine orb, Boss Meteor core |
| `10` | `Mod_ProjectileTrail` | `TrailRenderer` (`AbyssalWeaponTrail`) | `T_Vfx_EnergyTrail` | Arrows, Fireballs, Dark Bolts, Weapon tips (`WeaponBladeTrail3D`) |
| `11` | `Mod_LightningSpline` | `LineRenderer` (`3–8` segments) | `T_Vfx_Lightning` | Monk Chi chains, Arcane Storm, RuneSealEnergyBeam |
| `12` | `Mod_FlameLick` | Crossed quad (`AbyssalTorchFlame`) | Procedural HLSL noise | Dungeon sconces, Braziers, Fire Traps, Demon/Boss fire |
| `13` | `Mod_GroundMist` | Horizontal XZ plane (`AbyssalGroundMist`) | Procedural HLSL drift noise | All 7 dungeon biomes, Cursed Forest swamp haze, Boss Void fog |
| `14` | `Mod_ShatterLines` | Radial burst quad / shards | `T_Vfx_ShatterLines` | Poise/Armor break, CrystalGolem death, Ice shatter |
| `15` | `Mod_SmokeWisp` | Low-count `Billboard` `ParticleSystem` | Soft procedural circle/puff | Torch soot, Footstep dust, Enemy death ash, Poison cloud |

---

## 2. Strict Naming Conventions

| Asset Kind | Pattern | Examples |
| --- | --- | --- |
| **VFX Prefab** | `VFX_<Domain>_<Subject>_<Variant>.prefab` | `VFX_Skill_Mage_Meteor.prefab`, `VFX_Env_BossArena_Hazard.prefab` |
| **Shader** | `Abyssal<Domain><Purpose>.shader` | `AbyssalSkillVfx.shader`, `AbyssalWeaponTrail.shader` |
| **Material** | `M_Vfx_<Module>_<Element>.mat` | `M_Vfx_Slash_Blood.mat`, `M_Vfx_Rune_HolyGold.mat` |
| **Texture** | `T_Vfx_<Shape>[_<Map>].png` | `T_Vfx_GothicRuneCircle.png`, `T_Vfx_Crack_Mask.png` |
| **Custom Mesh (Blender)** | `SM_Vfx_<Shape>.fbx` | `SM_Vfx_SlashCrescent.fbx`, `SM_Vfx_SpiralCone.fbx` |
| **ScriptableObject Preset** | `VFXSpec_<Domain>_<Name>.asset` | `VFXSpec_Boss_AbyssalKnight.asset` |

---

## 3. Standard VFX Prefab Hierarchy

Keep every authored prefab predictable so `VfxQualitySystem` and `GameObjectPool` control it reliably:

```text
VFX_<Domain>_<Subject>            [Root: Pooling wrapper + ScalableVfxResponder]
  ├── Telegraph_Ground            [Optional: y=+0.02m, isCombatTelegraph=true -> ALWAYS ON]
  ├── Core_Primary                [ParticleSystem or Mesh: readable silhouette on ALL tiers]
  ├── Accent_RibbonOrArc          [TrailRenderer / LineRenderer / Mesh crescent]
  ├── Secondary_EmbersOrSmoke     [Medium/High only: disabled automatically on Low tier]
  └── Optional_PointLight         [High tier ONLY if budgeted; no shadows; <= 0.4s flash]
```

---

## 4. Material Reuse & Zero-Leak Pooling Rules

1. **Color via `ParticleSystem.main.startColor`, Vertex Color, or `MaterialPropertyBlock`**: Never create `new Material(...)` at runtime inside `Update` or every projectile spawn! Cache shared materials in a static lookup (`CharacterVisualFactory` / `PbrMaterialLibrary` / `VFXManager`) and call `Destroy()` in `OnDestroy()` ONLY if an isolated instance material was genuinely unavoidable (like `WeaponBladeTrail3D`).
2. **Prewarm Pools During Room Load**: Pre-allocate shared bursts (`ExpandingBurstFx`, `SlashArcRibbonFx`, `DirectionalSparkBurstFx`, `FloatingCombatText`) in `GameObjectPool` before combat begins to prevent frame-time hitches on the first swing in a dungeon room.
3. **Never Modify Shared Assets at Runtime**: Do not mutate `.sharedMaterial.color` directly on project-wide asset materials in builds without restoring or using `MaterialPropertyBlock`.


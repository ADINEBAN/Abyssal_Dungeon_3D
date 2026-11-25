---
name: abyssal-vfx-artist
description: Complete game-wide VFX production director and engineering skill for Abyssal Dungeon 3D (Unity 2021.3.11f1 + URP 12.1.7 + Blender 5.3.0 MCP). Covers Ancient Gothic Dark Fantasy art direction, all 10 playable classes, weapon trails/impacts, 10 elemental skill families across 40 abilities, movement & game feel, 10 enemies + 3-phase Abyssal Knight boss, all 7 dungeon biomes, loot/UI/world interactions, animation timing, modular pooling, LAN multiplayer replication, and Low/Medium/High GPU optimization.
---

# Abyssal VFX Artist — Complete Game-Wide VFX Production Skill

You are the **VFX Director, Senior Unity URP Technical Artist, Blender 3D Artist, and Performance Engineer** for **Abyssal Dungeon 3D**. Use this skill to design, author, integrate, optimize, and validate every visual effect in the game across all 8 production domains without breaking gameplay, multiplayer authority, or low-end GPU performance.

> **Engine Lock**: **Unity `2021.3.11f1` + URP `12.1.7`** (`Packages/manifest.json`). Never install HDRP, `com.unity.visualeffectgraph`, or upgrade Unity/URP. Author all runtime VFX using Unity built-in `ParticleSystem`, `TrailRenderer`, `LineRenderer`, custom URP HLSL `.shader` files, and custom FBX meshes built in **Blender `5.3.0`** via `blender-mcp`.

---

## 1. Existing Project Systems You MUST Reuse (Never Duplicate)

Before creating any script, manager, or asset, inspect and extend the existing systems:
- **Pooled VFX & Runtime Factory**: `AbyssalDungeon.VFX.VFXManager` ([VFXManager.cs](../../../Assets/Scripts/VFX/VFXManager.cs)), `ProceduralVFXFactory` ([ProceduralVFXFactory.cs](../../../Assets/Scripts/VFX/ProceduralVFXFactory.cs)), and `AbyssGame.GameObjectPool` ([GameObjectPool.cs](../../../Assets/Scripts/Game/Core/GameObjectPool.cs)).
- **Combat & Feel Dispatchers**: `CombatFeelManager` ([CombatFeelManager.cs](../../../Assets/Scripts/Game/Combat/CombatFeelManager.cs)), `WeaponBladeTrail3D` & `WeaponHitboxSweep3D` ([CombatHitboxSystem.cs](../../../Assets/Scripts/Game/Combat/CombatHitboxSystem.cs)), `CharacterVisualFactory` ([CharacterVisualFactory.cs](../../../Assets/Scripts/Game/Visuals/CharacterVisualFactory.cs)), `ClassVisualArmorKit` ([ClassVisualArmorKit.cs](../../../Assets/Scripts/Game/Character/ClassVisualArmorKit.cs)), `CharacterMotionPolisher` ([CharacterMotionPolisher.cs](../../../Assets/Scripts/Game/Player/CharacterMotionPolisher.cs)), and `ScreenCombatFeedbackUI` ([ScreenCombatFeedbackUI.cs](../../../Assets/Scripts/Game/Combat/ScreenCombatFeedbackUI.cs)).
- **Quality, Lighting & Profiling**: `VfxQualitySystem` & `ScalableVfxResponder` ([VfxQualitySystem.cs](../../../Assets/Scripts/Game/Graphics/VfxQualitySystem.cs)), `GraphicsManager` ([GraphicsManager.cs](../../../Assets/Scripts/Game/Graphics/GraphicsManager.cs)), `PerformanceMonitor` ([PerformanceMonitor.cs](../../../Assets/Scripts/Game/Graphics/PerformanceMonitor.cs)), and `EnvironmentDistanceCuller` ([EnvironmentDistanceCuller.cs](../../../Assets/Scripts/Game/Environment/EnvironmentDistanceCuller.cs)).
- **Dungeon, Boss & LAN**: `ModularDungeonArchitect` ([ModularDungeonArchitect.cs](../../../Assets/Scripts/Game/Environment/ModularDungeonArchitect.cs)), `DungeonWorldBuilder` ([DungeonWorldBuilder.cs](../../../Assets/Scripts/Game/Dungeon/DungeonWorldBuilder.cs)), `AbyssalBossAI` ([AbyssalBossAI.cs](../../../Assets/Scripts/Game/AI/AbyssalBossAI.cs)), `LanRoomBridge` ([LanRoomBridge.cs](../../../Assets/Scripts/Game/Net/LanRoomBridge.cs)).
- **Shared Shaders & Alpha Textures**: `AbyssalSkillVfx.shader`, `AbyssalTorchFlame.shader`, `AbyssalGroundMist.shader`, `AbyssalWeaponTrail.shader`, `AbyssalCrystalLit.shader` in `Assets/Resources/Shaders/`, plus the 14 `T_Vfx_*.png` masks in `Assets/Resources/VFX/Textures/`.

---

## 2. Non-Negotiable Production Rules

1. **Strict Gameplay Separation**: VFX prefabs and scripts MUST NEVER contain colliders, deal damage, modify hitboxes, alter animation clip playback speeds, extend i-frames, or mutate multiplayer state.
2. **Preserve Combat Telegraphs on Every Tier**: Ground danger indicators and boss/trap warnings (`ScalableVfxResponder.isCombatTelegraph = true`) MUST retain 100% visibility, exact radius, and exact timing even on **Low** quality.
3. **Zero-GC Runtime Playback**: Reuse `GameObjectPool` / `VFXManager` (`Stop Action = Disable`), color via `MaterialPropertyBlock` or `ParticleSystem.main.startColor`, clear `TrailRenderer` on recycle, and disable `Cast Shadows` / `Receive Shadows` on every VFX renderer.
4. **Bloom & Dark Fantasy Discipline**: Under `AbyssalPostProcessing` (ACES + Bloom `threshold = 0.95`), keep HDR colors `<= 2.6` and smoke/mist `< 0.75` luminance so blades, runes, and danger cues shine cleanly over dark gothic stone.
5. **Honest Benchmarking**: Never invent FPS guarantees; verify frame times, draw calls, active particles, and GC allocs with `PerformanceMonitor` on real hardware.

---

## 3. Master VFX Production Workflow (8 Steps)

When asked to create, upgrade, or audit VFX for any part of the game:

1. **Identify Scope & Read Domain Reference**: Match the request to one or more of the 8 coverage domains (A–H) in Section 4 and read the corresponding `references/*.md` + [vfx-style-manifest.json](assets/vfx-style-manifest.json).
2. **Draft a Spec First**: Fill out the matching template from `templates/` (`character`, `weapon`, `skill`, `environment`, `boss`, `prefab`) before building assets.
3. **Check Modular Reuse (`shared-vfx-architecture.md`)**: Reuse the 15 modular building blocks (`Mod_*`), existing `T_Vfx_*.png` textures, and the 5 `Abyssal*` URP shaders before creating anything new.
4. **Author Custom Meshes in Blender (When Needed)**: For curved slash shells, tornados, vortex cones, shatter shards, or gothic portal rings, follow [blender-workflow.md](references/blender-workflow.md) using `execute_blender_code` + `look` verification -> Unity FBX export (`Assets/Art/VFX/Meshes/SM_Vfx_*.fbx`).
5. **Assemble in Unity 2021.3.11f1 + URP 12.1.7**: Follow [unity-urp-workflow.md](references/unity-urp-workflow.md), wire `ScalableVfxResponder`, register with `VFXManager` / `GameObjectPool` / `CombatFeelManager`, and sync exact timing via [animation-integration.md](references/animation-integration.md).
6. **Verify Multiplayer Classification (`multiplayer-vfx.md`)**: Assign `Local-Only`, `Owner-Only`, `Shared Event`, or `Host-Authoritative` so LAN co-op stays deterministic without streaming particle state.
7. **Configure Low / Medium / High Scaling (`performance-optimization.md`)**: Tune particle caps, ember toggles, light limits, and fill-rate budgets for Integrated Intel iGPUs (`Low`) through GTX 1650 (`High`).
8. **Run Pre-Merge Gate**: Complete [vfx-review-checklist.md](templates/vfx-review-checklist.md) (and [map-vfx-placement-checklist.md](templates/map-vfx-placement-checklist.md) for dungeon rooms).

---

## 4. Reference & Template Directory Map (All 8 Domains)

### Core Style Manifest
- [vfx-style-manifest.json](assets/vfx-style-manifest.json) — Machine-readable Ancient Gothic palette, 10 elemental ramps, 10 class signatures, 7 biome colors, and Low/Medium/High budgets.

### Domain References (What to Build)
- **Art Direction**: [references/art-direction.md](references/art-direction.md)
- **Domain A — Playable Characters (All 10 Classes)**: [references/character-vfx.md](references/character-vfx.md)
- **Domain B — Weapons & Combat**: [references/weapon-combat-vfx.md](references/weapon-combat-vfx.md)
- **Domain C — Skills & 10 Elements (40 Class Skills)**: [references/skill-elemental-vfx.md](references/skill-elemental-vfx.md)
- **Domain D — Movement, Locomotion & Game Feel**: [references/movement-game-feel-vfx.md](references/movement-game-feel-vfx.md)
- **Domain E — Enemies, Summons & 3-Phase Boss**: [references/enemy-boss-vfx.md](references/enemy-boss-vfx.md)
- **Domain F — Environment & 7 Dungeon Biomes**: [references/environment-vfx.md](references/environment-vfx.md)
- **Domain G & H — Loot, Interactions & UI-World Boundaries**: [references/interaction-ui-vfx.md](references/interaction-ui-vfx.md)

### Engineering & Pipeline References (How to Build & Ship)
- [references/animation-integration.md](references/animation-integration.md) — `WeaponHitboxSweep3D`, `ActorUnit`, `PlayerAnimationController`, bone sockets, cancellation hygiene.
- [references/blender-workflow.md](references/blender-workflow.md) — Blender `5.3.0` (`blender-mcp`) procedural UV mesh workflow & Unity FBX export rules.
- [references/unity-urp-workflow.md](references/unity-urp-workflow.md) — Unity `2021.3.11f1` + URP `12.1.7` constraints, HLSL shaders, `ParticleSystem`/`Trail`/`Line`, `VFXManager`/`GameObjectPool`.
- [references/shared-vfx-architecture.md](references/shared-vfx-architecture.md) — 15 modular building blocks, asset naming, standard prefab hierarchy, pooling rules.
- [references/multiplayer-vfx.md](references/multiplayer-vfx.md) — `LanRoomBridge` 4-way sync classification, event-only replication, co-op clutter clamping.
- [references/environment-placement.md](references/environment-placement.md) — Biome-by-biome placement across Rooms 0–6, sightlines, point light budgets, `EnvironmentDistanceCuller`.
- [references/performance-optimization.md](references/performance-optimization.md) — Low/Medium/High quality scaling, fill-rate/GC elimination, `PerformanceMonitor` benchmarking.

### Spec & Review Templates
- [templates/character-vfx-spec.md](templates/character-vfx-spec.md)
- [templates/weapon-vfx-spec.md](templates/weapon-vfx-spec.md)
- [templates/skill-vfx-spec.md](templates/skill-vfx-spec.md)
- [templates/environment-vfx-spec.md](templates/environment-vfx-spec.md)
- [templates/boss-vfx-spec.md](templates/boss-vfx-spec.md)
- [templates/vfx-prefab-spec.md](templates/vfx-prefab-spec.md)
- [templates/vfx-review-checklist.md](templates/vfx-review-checklist.md)
- [templates/map-vfx-placement-checklist.md](templates/map-vfx-placement-checklist.md)


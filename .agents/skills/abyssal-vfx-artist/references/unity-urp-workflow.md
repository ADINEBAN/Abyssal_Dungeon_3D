# Unity 2021.3.11f1 + URP 12.1.7 VFX Production Workflow

Verified engine baseline from [ProjectVersion.txt](../../../ProjectSettings/ProjectVersion.txt) and [manifest.json](../../../Packages/manifest.json):
- **Unity**: `2021.3.11f1`
- **URP**: `com.unity.render-pipelines.universal: 12.1.7`
- **Shader Graph**: `12.1.7` (with HLSL hand-written `.shader` already used for all core VFX)
- **Built-in Particle System**: `com.unity.modules.particlesystem: 1.0.0`

> **Hard Constraints**: Do NOT add HDRP, do NOT add `com.unity.visualeffectgraph`, do NOT upgrade Unity or URP, and do NOT enable URP Depth/Opaque Texture captures unconditionally on `Low` graphics preset.

---

## 1. Existing VFX & Graphics Systems to Extend (Never Duplicate!)

| System | Script Path | How to Reuse / Extend |
| --- | --- | --- |
| **`AbyssalDungeon.VFX.VFXManager`** | [VFXManager.cs](../../../Assets/Scripts/VFX/VFXManager.cs) | Add new enum values to `VFXType` and register pooled prefabs rather than building a second global `VFXManager`. |
| **`ProceduralVFXFactory`** | [ProceduralVFXFactory.cs](../../../Assets/Scripts/VFX/ProceduralVFXFactory.cs) | Runtime URP `Particles/Unlit` generator for `VFXType`; every prefab auto-attaches `ScalableVfxResponder`. |
| **`AbyssGame.GameObjectPool`** | [GameObjectPool.cs](../../../Assets/Scripts/Game/Core/GameObjectPool.cs) | Expand zero-GC pools (`FloatingCombatText`, `ExpandingBurstFx`, `SlashArcRibbonFx`, `DirectionalSparkBurstFx`) for high-frequency hit/slash VFX. |
| **`CombatFeelManager`** | [CombatFeelManager.cs](../../../Assets/Scripts/Game/Combat/CombatFeelManager.cs) | Central dispatcher for `SpawnSlashArc`, `SpawnHitImpact`, `TriggerHitStop`, and `MaterialPropertyBlock` hit flash. |
| **`VfxQualitySystem` & `ScalableVfxResponder`** | [VfxQualitySystem.cs](../../../Assets/Scripts/Game/Graphics/VfxQualitySystem.cs) | Register every `ParticleSystem` (`VfxQualitySystem.RegisterParticleSystem`) or attach `ScalableVfxResponder` (mark telegraphs `isCombatTelegraph = true`!). |
| **`GraphicsManager`** | [GraphicsManager.cs](../../../Assets/Scripts/Game/Graphics/GraphicsManager.cs) | Read `GraphicsManager.ParticleDensityMultiplier` and `CurrentViewDistanceMeters`; respect Bloom, MSAA, Point Light limits. |
| **`CharacterVisualFactory`** | [CharacterVisualFactory.cs](../../../Assets/Scripts/Game/Visuals/CharacterVisualFactory.cs) | Use `SpawnBurst`, `SpawnGroundTelegraph`, and URP material builders when prototyping or binding dynamic units. |

---

## 2. URP 12.1.7 Shader & Material Standards

Inspect existing shaders in [Assets/Resources/Shaders/](../../../Assets/Resources/Shaders/) before authoring new shaders:
- `AbyssalSkillVfx.shader` (`AbyssalDungeon/SkillVfxAdditive`): HDR `_MainColor`/`_EdgeColor`, `_RadialMask`, `_PulseSpeed`. Ideal for floor runes, shock rings, nova waves, slash shells.
- `AbyssalTorchFlame.shader` (`AbyssalDungeon/TorchFlame`): Instanced vertex wind sway + procedural noise teardrop flame (`Cull Off`, `ZWrite Off`, `Blend SrcAlpha OneMinusSrcAlpha` or Additive).
- `AbyssalGroundMist.shader` (`AbyssalDungeon/GroundMist`): GPU-instanced world-space XZ fog drift + soft radial fade.
- `AbyssalWeaponTrail.shader` (`AbyssalDungeon/WeaponTrail`): Sweep `_Progress` + blade edge sharpness.
- `AbyssalCrystalLit.shader` (`AbyssalDungeon/CrystalLit`): Opaque PBR lit crystal + Fresnel rim + pulsing emission.

### URP 12.1.7HLSL Compatibility Rules
1. Always tag `"RenderPipeline" = "UniversalPipeline"` inside `SubShader Tags`.
2. Include `"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"`.
3. Keep all material uniforms inside `CBUFFER_START(UnityPerMaterial) ... CBUFFER_END` so **SRP Batcher** compatibility remains intact!
4. Add `#pragma multi_compile_instancing` (and `UNITY_VERTEX_INPUT_INSTANCE_ID` in `Attributes`/`Varyings`) for instanced ground decals, mist planes, and torches.
5. Prefer **ParticleSystem `Custom Vertex Streams` + `UV` / `VertexColor`** or **`MaterialPropertyBlock`** over `renderer.material` per-instance material cloning!

---

## 3. Built-in VFX Component Rules (ParticleSystem, TrailRenderer, LineRenderer)

1. **`ParticleSystem`**:
   - Set `Simulation Space = World` for smoke, sparks, weapon embers, and footstep dust; `Local` for inward charge halos and orbiting shields.
   - Set `Stop Action = Disable` (never `Destroy`) on pooled prefabs so `GameObjectPool` / `VFXManager` reuses them with zero GC alloc.
   - Disable `Collision`, `Lights`, and `Noise` modules on `Low` unless strictly necessary for hero/boss clarity.
   - Prefer `Billboard` + `Stretched Billboard` (for sparks/bolts) + `Mesh` (for `SM_Vfx_*` crescents/rings/shards).
2. **`TrailRenderer` (`WeaponBladeTrail3D`)**:
   - Clear on pool respawn (`_trail.Clear()`) and turn `emitting = false` cleanly via timer before deactivating.
   - Use `LineAlignment.View` for uniform silhouette readability or `TransformZ` for ground-locked energy waves.
3. **`LineRenderer`**:
   - Use `useWorldSpace = true` for lightning chains and `RuneSealEnergyBeam`; update point arrays via reusable cached `Vector3[]` buffers (zero per-frame allocations!).

---

## 4. Strict Separation of Gameplay and VFX

- **Never put colliders, `Damage`, `ApplyStatus`, `Knockback`, or cooldown timers inside a VFX prefab.**
- Gameplay (`ActorUnit`, `WeaponHitboxSweep3D`, `AbyssalBossAI`, `WorldDungeonTrap`) owns timing, radius, hit detection, and multiplayer messages; VFX only listens or receives fire-and-forget spawn calls.
- Never allow `GraphicsManager` quality tiers (`Low` / `Medium` / `High`) to slow animation speed, shrink combat hitboxes, delay boss telegraphs, or drop essential gameplay warnings.


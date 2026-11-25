# VFX Production Review Checklist (Pre-Merge Gate)

Run this checklist before signing off on any new or updated VFX asset in **Abyssal Dungeon 3D**.

## 1. Visual & Art Direction
- [ ] Fits **Ancient Gothic Dark Fantasy** (`#D4AF37` gold, `#8C6239` bronze, `#3B281E` brown, `#121016` obsidian, `#8A3FFC` arcane purple).
- [ ] Uses 3-value ramp (bright hot core -> unmistakable elemental midtone -> dark smoke/rim skirt).
- [ ] HDR intensity `<= 2.6` (no blown-out white ACES blobs under `AbyssalPostProcessing` Bloom `threshold = 0.95`).
- [ ] Distinct silhouette readable from top-down `OrbitCamera3D` gameplay angle without camera clipping.

## 2. Gameplay, Animation & Multiplayer Safety
- [ ] **Zero gameplay coupling**: VFX contains no damage values, hitboxes, colliders, cooldown logic, or status mutators.
- [ ] Syncs cleanly to `WeaponHitboxSweep3D` / `ActorUnit` timing (`Anticipation -> Execution -> Impact -> Dissipation`).
- [ ] Cancelled/interrupted actions stop anticipation emitters and turn off `WeaponBladeTrail3D.emitting` immediately.
- [ ] Classified cleanly for LAN (`Local-Only` / `Owner-Only` / `Shared Event` / `Host-Authoritative`).
- [ ] Combat telegraphs (`isCombatTelegraph = true`) preserve exact radius, timing, and border sharpness across **Low / Medium / High**.

## 3. Unity 2021.3.11f1 + URP 12.1.7 Compliance
- [ ] Uses **only** URP `12.1.7` (`ParticleSystem`, `TrailRenderer`, `LineRenderer`, custom `.shader`, `MeshRenderer`). No HDRP or VFX Graph.
- [ ] Extends existing `VFXManager`, `ProceduralVFXFactory`, `GameObjectPool`, `CombatFeelManager`, or `VfxQualitySystem` (no duplicate managers).
- [ ] All meshes imported cleanly (`SM_Vfx_*.fbx`, `Cast Shadows = Off`, `Receive Shadows = Off`, no imported lights/cameras/colliders).
- [ ] Reuses shared textures (`T_Vfx_*`) and shared materials (`MaterialPropertyBlock` where tinted).

## 4. Performance & Pooling (`PerformanceMonitor`)
- [ ] Zero-GC playback (`GcAllocBytesPerFrame = 0 B` from steady-state VFX bursts; `Stop Action = Disable` + pre-warmed pool).
- [ ] Tested on **Low**, **Medium**, and **High** tiers (`GraphicsManager.SetQualityTier`).
- [ ] `TrailRenderer.Clear()` called properly on pool recycle (no cross-room stretched trails).
- [ ] Any FPS claims cite **real GPU test measurements** from `PerformanceMonitor`, never guesses.


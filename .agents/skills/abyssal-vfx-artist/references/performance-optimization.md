# Performance & Optimization Guidelines — Integrated GPU to GTX 1650+

Abyssal Dungeon 3D must scale gracefully from **Integrated Intel UHD / Iris Xe laptops (`Low`)** up to **GTX 1650 4GB / RTX 2050+ (`High` target 60 FPS @ 1080p, subject to real hardware profiling)**. Existing performance hooks live in [VfxQualitySystem.cs](../../../Assets/Scripts/Game/Graphics/VfxQualitySystem.cs), [GraphicsManager.cs](../../../Assets/Scripts/Game/Graphics/GraphicsManager.cs), [GraphicsPreset.cs](../../../Assets/Scripts/Game/Graphics/GraphicsPreset.cs), and [PerformanceMonitor.cs](../../../Assets/Scripts/Game/Graphics/PerformanceMonitor.cs).

---

## 1. Mandatory 3-Tier VFX Scaling Matrix

*(Maps directly to `VfxQualityOption` & `GraphicsQualityTier` Low / Medium / High; `Ultra` & `Cinematic` presets unlock `High+` ambient flourishes within `GraphicsManager` budgets.)*

| Metric / Feature | **Low (Integrated GPU Safe)** | **Medium (Balanced)** | **High (Target 60 FPS @ 1080p GTX 1650)** |
| --- | --- | --- | --- |
| **`GraphicsManager.ParticleDensityMultiplier`** | `0.35 – 0.45` | `0.65 – 0.75` | `1.00` (`1.25–1.50` Ultra/Cinematic) |
| **Max Active Particles per Single Effect** | `<= 16` | `<= 36` | `<= 72` (Boss Ult cap `96`) |
| **Max Concurrent Combat Point Lights** | `0` extra VFX lights (scene limit `4`) | `0–1` brief impact/boss light (scene limit `8`) | `2–4` short-lived spell/portal lights (scene limit `12`) |
| **Secondary Debris & Ash / Embers** | **Disabled** (`Secondary_EmbersOrSmoke` off) | `50%` emission rate | `100%` emission rate |
| **Footstep Dust & Idle Armor Motes** | **Disabled** | Alternate steps / light idle | Full surface-aware footsteps & class relic motes |
| **Ground Mist (`AbyssalGroundMist`)** | **Disabled** (clean stone read, huge fill-rate win) | Single low-alpha layer | Multi-room drifting mist enabled |
| **Transparent Overdraw Budget** | Max `2` overlapping alpha quads in combat center | Max `3` overlapping layers | Max `4` overlapping layers (tight bounding boxes!) |
| **Combat Telegraphs (`isCombatTelegraph = true`)** | **100% FULL VISIBILITY & TIMING** | **100% FULL VISIBILITY & TIMING** | **100% FULL VISIBILITY & TIMING** |

---

## 2. Biggest GPU Bottlenecks on Integrated Laptops & How to Beat Them

1. **Pixel Fill-Rate & Alpha Overdraw (The #1 Killer on Intel iGPUs)**:
   - Full-screen additive particles or 12 layered smoke billboards destroy fill-rate (`GPU FrameTimeMs` spikes rapidly at 1080p).
   - **Fixes**: Crop alpha masks tightly in `Assets/Resources/VFX/Textures/` (minimal transparent padding!), keep max particle billboard `startSize` modest, favor **1 crisp custom UV mesh (`SM_Vfx_SlashCrescent`)** over 20 stacked smoke billboards, and put `ZTest LEqual` cleanly on opaque/alpha-test embers where possible.
2. **Runtime Material Allocation & GC Spikes**:
   - Reading `PerformanceMonitor.GcAllocBytesPerFrame` MUST show **`0 B` steady-state allocation per VFX burst**.
   - Use `GameObjectPool` / `VFXManager` pre-warmed pools + `MaterialPropertyBlock` + cached `LineRenderer` point buffers.
3. **Shader Math & Texture Sampling**:
   - Keep fragment shaders simple (`1–2` texture samples max in `AbyssalSkillVfx.shader`), avoid screen-color grab passes (`_CameraOpaqueTexture` distortion) on `Low` and `Medium`.
4. **Particle Shadows**:
   - **Never enable `Cast Shadows` or `Receive Shadows` on ParticleSystems, TrailRenderers, or LineRenderers.**

---

## 3. Measurement & Honest Profiling Protocol

Never claim an effect or room hits 60 FPS by guessing! Measure using [PerformanceMonitor.cs](../../../Assets/Scripts/Game/Graphics/PerformanceMonitor.cs) and the Unity Profiler during real multi-enemy + 4-hero combat (`Dungeon.unity` Boss Phase 3 / Elite Room):
- Track **`SmoothedCpuFrameTimeMs`**, **`SmoothedGpuFrameTimeMs`**, **`OnePercentLowFps`**, **`DrawCallsCount`**, **`SetPassCallsCount`**, **`ActiveParticlesCount`**, and **`GcAllocBytesPerFrame`**.
- Target budgets for a heavy 4-player + 6-enemy + torches combat frame on **High (`1080p`)**:
  - Total **`ActiveParticlesCount`**: keep under `350–450` on `High`, `<= 180` on `Medium`, `<= 80` on `Low`.
  - **VFX-added `SetPassCalls`**: `<= 12–18` across shared materials (`SRP Batcher` + GPU Instancing).
  - **Per-frame GC Alloc from VFX playback**: `0 bytes`.
- Report actual GPU hardware used during any benchmark test (e.g., laptop Intel iGPU vs GTX 1650 desktop/laptop), never extrapolating unmeasured FPS guarantees.


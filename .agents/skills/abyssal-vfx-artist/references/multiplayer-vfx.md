# Multiplayer & Co-op VFX Synchronization (`LanRoomBridge`)

Abyssal Dungeon 3D supports local 4-hero party play (Player + AI or LAN co-op) via custom TCP JSON-line messaging in [LanRoomBridge.cs](../../../Assets/Scripts/Game/Net/LanRoomBridge.cs) and legacy protocol structures in `AbyssalDungeon.LAN`. There is **no Mirror / Photon / Netcode for GameObjects package** — network bandwidth is precious (~12.5 Hz position sync + lightweight `LobbyNetMsg` `op` events).

---

## 1. The 4-Way Multiplayer VFX Classification

Before implementing ANY new VFX, assign it one of these four sync tiers:

| Classification | Examples | Replicated Over LAN? | Authority / Trigger Rule |
| --- | --- | --- | --- |
| **1. Local-Only Cosmetic** | Torch embers, ground mist drift, footstep dust, idle cape motes, environmental dust, adaptive particle count | **NO** | Driven independently by local camera position (`EnvironmentDistanceCuller`) & local `GraphicsManager` preset. |
| **2. Owner-Only Feedback** | Low-HP screen vignette (`ScreenCombatFeedbackUI`), local combo card glow, camera micro-shake, hit-stop feel | **NO** | Runs purely on `IsLocalPlayer` client; never shake or flash another teammate's screen! |
| **3. Shared Visual Event** | Hero combo slash, dodge afterimage, Q/W/E/R skill VFX, healing aura, level-up pillar, loot orb spawn | **YES (Event ID Only)** | Broadcast lightweight JSON event (`op: "hero_skill"`, caster ID, skill slot, aim/target position) — NEVER stream individual particle transforms or lifetimes! |
| **4. Host/Server-Authoritative Gameplay State** | Boss Phase 2/3 transitions (`boss_sync`), Meteor Storm target coordinates, Room Clear portal/chest unlock (`room_clear`), Soul Anchor respawn | **YES (Host Source of Truth)** | Host determines RNG seed / coordinates & start timestamps; clients spawn matching local VFX anchored to the exact authoritative positions. |

---

## 2. Deterministic Timing & Readability Rules for Co-op

1. **Same Telegraph Timing on Every Client**:
   - A Boss or Elite ground telegraph (`CharacterVisualFactory.SpawnGroundTelegraph`, `AbyssalBossAI`) must start immediately on `boss_sync` receipt and match the authoritative `windupDelay`.
   - Local quality settings (`Low` vs `High`) MAY reduce decorative embers around the telegraph, **NEVER alter telegraph radius, angle, position, or duration** (`ScalableVfxResponder.isCombatTelegraph = true`).
2. **Event-Only Network Payloads**:
   - Replicate `{ casterNetId, eventEnum, originVec3, dirVec3, durationOrSeed }` (1 message per cast/impact), letting each client's local `VFXManager` / `GameObjectPool` render at its own local `ParticleDensityMultiplier`.
3. **4-Player Visual Noise Clamping**:
   - When 3–4 heroes attack the same cluster simultaneously, **dim non-local ally slash/impact opacity to `0.60x – 0.70x`** and cap ally point lights to `0` (`Optional_PointLight` enabled for `IsLocalPlayer` and Boss threats first).
   - Keep **Enemy & Boss danger telegraphs** drawn cleanly above ally spell glow so no teammate's Fireball/Arcane field ever hides a Boss Slam circle!
4. **Zero Gameplay Desync Guarantee**:
   - Visual effects are strictly fire-and-forget presentation subscribers. If a client skips a decorative particle burst due to pooling limits or `Low` quality mode, combat HP, status effects, loot rolls, and room triggers remain 100% identical to the Host.


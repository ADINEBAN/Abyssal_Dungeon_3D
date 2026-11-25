# Movement, Locomotion & Game-Feel VFX

Great action combat in Abyssal Dungeon 3D pairs crisp player timing with unmistakable physical feedback while keeping gameplay logic 100% decoupled. Existing feel hooks live in [CharacterMotionPolisher.cs](../../../Assets/Scripts/Game/Player/CharacterMotionPolisher.cs), [CombatFeelManager.cs](../../../Assets/Scripts/Game/Combat/CombatFeelManager.cs), [ScreenCombatFeedbackUI.cs](../../../Assets/Scripts/Game/Combat/ScreenCombatFeedbackUI.cs), and `OrbitCamera3D`.

---

## 1. Locomotion Surface-Aware Feedback (`CharacterMotionPolisher`)

`CharacterMotionPolisher` samples collider names beneath the player's feet at heel-strike cadence (`FootstepSurface`):
- **`Stone` (Default Dungeon Floor)**: Muted slate-grey micro dust puff (`0.35m` spread), zero linger.
- **`Wood` (`WoodFloor_*` Plank Bridges)**: Warm dark-brown dust + 2–3 tiny wood-splinter chips (`High` only).
- **`Metal` (`MetalFloor_*` Iron Grates)**: Tiny brief bronze-gold friction spark (`1–3` particles on heavy step/dash).
- **`Dirt` / `CursedForest` (`Biome == CursedForest`)**: Damp moss-green spore / leaf scatter puff.

> **Quality Rule**: Footstep dust runs at `100%` rate on `High`, `50%` rate (alternate foot) on `Medium`, and **OFF** on `Low` to save fill-rate.

---

## 2. Dodge, Dash, Jump & Landing Polishes

| Action | Motion Hook | VFX Layer | Game-Feel Timing Rule |
| --- | --- | --- | --- |
| **Dodge / Roll** | `ActorUnit` dodge trigger -> `CharacterMotionPolisher.SpawnDodgeGhost` | 1–3 translucent ghost meshes (`0.26s` exponential alpha fade) + backward ground wind cone | Ghost fade MUST match i-frame clarity without obscuring floor hazards |
| **Jump Takeoff** | `TriggerJumpTakeoff` | Ground launch compression puff (`T_Vfx_ShockRing`, scale `0.9m`) | Instantaneous frame-0 trigger |
| **Land Impact** | `TriggerLandImpact(fallSpeed)` | Radial ground dust ring proportional to fall speed + camera vertical micro-dip (`1–2 cm`) | Suppress if `fallSpeed < 2.0f` |
| **Sprint Start** | Crossing sprint velocity threshold | Brief ankle wind streak (`0.25s`) | Cooldown-gated (`0.6s`) to prevent spam on tap |

---

## 3. Impact Feel, Hit-Stop & Camera Shake Coordination

`CombatFeelManager` owns hit-stop (`TriggerHitStop`), camera trauma, and victim material flash (`MaterialPropertyBlock`). Follow these synchronization rules:

1. **VFX Uses Unscaled or Simulation-Appropriate Time**:
   - During short melee **hit-stop (`0.03s – 0.07s`)**, freeze or slow the **attacker & victim animation / trail** (`Animator.speed` or `Time.timeScale`) slightly while letting **impact sparks (`DirectionalSparkBurstFx`) flash brightly on Frame 0** (`useUnscaledTime = false` for world-synced ribbons, `true` for crisp radial spark burst so impact feels electric rather than sluggish).
2. **Camera Shake Pairing**:
   - **Normal hit**: NO camera shake (only `SpawnHitImpact` + `MaterialPropertyBlock` flash).
   - **Heavy / Combo 3 / Crit**: Tiny camera kick (`0.06 – 0.12` trauma) paired with `1.4x` sparks + shock ring.
   - **Boss Slam / Meteor**: Directional rumble (`0.25 – 0.40` trauma) strictly synchronized to the **ground impact frame**, NEVER the windup frame.
3. **Zero Gameplay Contamination**:
   - VFX scripts MUST NOT call `ActorUnit.ReceiveHit`, mutate velocity/position, alter hitbox sweep radii, change animation clip playback speed, or extend/shorten i-frames.
   - VFX scripts ONLY **subscribe to events or receive fire-and-forget calls** (`SpawnHitImpact`, `SpawnSlashArc`, `TriggerHitStop` initiated by `ActorUnit` / `CombatFeelManager`).


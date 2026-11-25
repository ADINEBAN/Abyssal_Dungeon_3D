# Character VFX Spec — `<ClassName>`

- **Class ID & Rig**: `<ID> <ClassName>` (`Paladin` / `Vanguard` / `Kachujin` / `Hero_DruidBeast`)
- **Primary Signature Weapon(s) & Relics**: `<RH_WP / LH_WP / Back Relic>`
- **Primary Elemental Motif**: `<Element>` (`Core Hex` / `Mid Hex` / `Rim Hex`)
- **Multiplayer Sync Tier**: `<Local-Only / Owner-Only / Shared Visual Event>`

## Lifecycle Effects Matrix

| State | Trigger Source (`ActorUnit` / `CharacterMotionPolisher` / `AnimationEvent`) | Socket / Anchor | Visual Composition | Low Tier Behavior | Medium / High Tier Behavior |
| --- | --- | --- | --- | --- | --- |
| **Spawn** | | `Root (y=+0.02m)` | | | |
| **Idle Aura / Relic** | | `<BackRelic / Head>` | | Off | Motes + soft glow |
| **Footsteps / Sprint** | `CharacterMotionPolisher` | Feet | Surface-aware puff (`Stone/Wood/Metal/Dirt`) | Off | Full surface puffs |
| **Dodge / Dash** | `ActorUnit` dodge window | Root + `SpawnDodgeGhost` | | 1 ghost | 3 stepped ghosts + wind cone |
| **Jump & Land** | `TriggerJumpTakeoff` / `TriggerLandImpact` | Root (`y=+0.02m`) | | Small shock ring | Shock ring + dust burst |
| **Buff / Shield / Status** | `ApplyStatus` / `AddShield` | Chest / Waist | | Simple Fresnel rim | Fresnel rim + rotating plates |
| **Low-HP / Death / Revive** | `IsDowned` / `IsDead` / `Revive` | Root + Chest | | | |

## Constraints Check
- [ ] Does NOT modify hitboxes, animation speed, cooldowns, or multiplayer authority.
- [ ] All emitters registered with `VfxQualitySystem` / `ScalableVfxResponder`.
- [ ] Verified visibility against dark dungeon floors (`AncientDungeon`, `BossArena`) without clipping Bloom (`< 2.6` HDR).


# Boss & Encounter VFX Spec — `<BossOrMiniBossName>`

- **Actor Class / AI Script**: `<AbyssalBossAI / MonsterAI>` (`CrystalGolem` / `AbyssalKnight`)
- **Arena Biome & Geometry**: `<BossArena / CrystalChamber>`, `ArenaRadius = <m>`
- **Authority Mode**: Host/Server-authoritative (`boss_sync` in `LanRoomBridge`)

## Phase Visual Progression

| Phase | HP Threshold | Armor / Silhouette Visual (`MaterialPropertyBlock`) | Arena Environmental Shift |
| --- | --- | --- | --- |
| **Phase 1** | `100% – 65%` | | |
| **Phase 2** | `65% – 30%` | | Transition nova wave + summons rift |
| **Phase 3** | `< 30%` | | `ArenaPhase3Transformation` outer hazard ring |

## Move-by-Move Telegraph & Impact Table

| Ability Name | Authoritative Windup (`s`) | Ground Telegraph Shape & Color (`isCombatTelegraph = true`) | Active / Impact VFX | Quality Cap (Low vs High) |
| --- | --- | --- | --- | --- |
| **Primary Slash / Slam** | | | | |
| **Charge / Dash** | | Red directional lane decal | | |
| **Ranged Wave / Shard** | | | | |
| **Shockwave / AoE Slam** | | Expanding inner fill + crisp border circle | | |
| **Summon / Meteor Storm** | | Multi-circle sky-drop markers (`3s` stagger) | | Low: markers + core craters only (`<= 16` parts/meteor) |

## Co-op Clarity Check
- [ ] Enemy telegraphs draw at `y = +0.02m` with border contrast unaffected by Low/Medium/High quality.
- [ ] Death dissolution clears active arena hazard flames/decals cleanly and spawns `WorldExitPortal`.


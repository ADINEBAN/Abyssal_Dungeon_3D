# Environment & Biome VFX Spec — `<BiomeName>` (Room `<#>` — `<RoomType>`)

- **Biome Enum (`DungeonBiome`)**: `<ForgottenTemple / AncientDungeon / UndergroundFortress / AbyssalCave / CrystalChamber / CursedForest / BossArena>`
- **Profile Colors (`BiomeAtmosphereProfile`)**:
  - `Torch Outer / Core`: `#______ / #______`
  - `Fog Color`: `#______`
  - `Ground Mist Color`: `#______`

## 4-Category Environmental Breakdown

| Category | Asset / Shader Used | Placement Anchor (`ModularDungeonArchitect` / `DungeonWorldBuilder`) | Low Tier | Medium / High Tier |
| --- | --- | --- | --- | --- |
| **Fire & Light** | `AbyssalTorchFlame.shader` + `TorchFlickerBehaviour` | Pillar sconces, Entrance/Exit gate braziers (`withPointLight` <= budget) | Crossed mesh only (`<= 4` room lights) | + Rising embers (`High`) + `8–12` lights |
| **Smoke & Atmosphere** | `AbyssalGroundMist.shader` + `AmbientDustEmberParticles` | Corners & trench edges (keep central combat sightline clear) | Mist **Off**, Dust **Off** | Single mist / Full mist + dust motes |
| **Magical Environment** | `AbyssalCrystalLit.shader`, `RuneSealEnergyBeam`, Portal rings | `WorldRunePillar`, `WorldHealingShrine`, `CrystalCluster`, `WorldExitPortal` | Core shader pulse kept | + Orbiting wisps / beam motes |
| **Physical & Traps** | `WorldDungeonTrap`, `FloorStoryDecals`, Surface footsteps | `WoodFloor_*`, `MetalFloor_*`, Spike/Fire trap plates | Trap pre-telegraph sparks **ALWAYS ON** | Full sparks, wood chips, water/moisture drips |

## Camera Sightline & Culling Check
- [ ] Tall smokes/flames kept away from player camera line (`-Z` chase cone).
- [ ] `EnvironmentDistanceCuller` registered; `Light.shadows = LightShadows.None` on all torches.
- [ ] Verified central floor clarity so enemy/boss danger telegraphs retain 100% contrast.


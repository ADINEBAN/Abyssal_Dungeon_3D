# Environment & Biome VFX — All 7 Dungeon Biomes

Abyssal Dungeon 3D spans **7 sequential biomes** configured in [BiomeAtmosphereProfile.cs](../../../Assets/Scripts/Game/Environment/BiomeAtmosphereProfile.cs), built modularly by [ModularDungeonArchitect.cs](../../../Assets/Scripts/Game/Environment/ModularDungeonArchitect.cs) and [DungeonWorldBuilder.cs](../../../Assets/Scripts/Game/Dungeon/DungeonWorldBuilder.cs), and budgeted by [EnvironmentDistanceCuller.cs](../../../Assets/Scripts/Game/Environment/EnvironmentDistanceCuller.cs) and `GraphicsManager`.

Existing world-space shaders already built for performance:
- `AbyssalTorchFlame.shader` (crossed vertical quads, vertex wind sway, procedural flame — zero ParticleSystem needed per torch!)
- `AbyssalGroundMist.shader` (instanced drifting world-space XZ fog planes with soft edge falloff)
- `AbyssalCrystalLit.shader` (opaque lit crystal with pulsing vein emission + Fresnel rim)

---

## 1. Biome-by-Biome VFX Matrix (All 4 Environmental Categories)

| # | Biome (`RoomType`) | Fire / Light VFX | Smoke & Atmosphere | Magical Environment | Physical / Hazard VFX |
| --- | --- | --- | --- | --- | --- |
| `0` | **ForgottenTemple** (`Entrance`) | Warm Antique-Gold torches (`#FFA640` / `#FFED9E`), gentle brazier embers | Thin violet-slate dust motes (`#262133`) in sun/arch shafts | Floating temple sanctuary dust near entrance altar; subtle floor rune seams | Masonry dust trickle along broken pillars; clean dry stone air |
| `1` | **AncientDungeon** (`Combat`) | Amber-orange wall sconces (`#FF8029`), dry crackle embers | Heavy low ground mist (`AbyssalGroundMist`, `#242230`) drifting across damp flagstones | Faint necrotic soul wisps drifting from iron cell cages | **Spike Traps (`WorldDungeonTrap`)**: metallic glint + dust puff when spikes snap (`3.6s` cycle); **Fire Traps**: crossed flame bursts |
| `2` | **UndergroundFortress** (`Elite`) | Fierce forge-orange braziers (`#FF5E1F`), glowing anvil/gate braziers | Dense warm ash & drifting forge soot embers | Elite ward brazier sigil glowing near fortified choke gates | Heated ember sparks rising over `MetalFloor_*` iron grate decks; chain creak dust |
| `3` | **AbyssalCave** (`Puzzle`) | Spectral indigo/arcane wall crystal torches (`#6652FF` / `#BDAEFF`) | Cool sapphire-indigo cavern cavern mist (`#1C2440`) hugging trenches | **3 Puzzle Rune Pillars (`WorldRunePillar`)**: cold blue idle rotation -> antique-gold ignition + **`RuneSealEnergyBeam` tether** streaming to `DungeonGate`! | Dripping subterranean cavern moisture beads; falling pebbles on puzzle gate unlock |
| `4` | **CrystalChamber** (`MiniBoss`) | Electric cyan crystal sconces (`#38BDFF` / `#BFEEFF`) | Crisp sparkling prism dust motes suspended in air | Pulsing crystal veins (`AbyssalCrystalLit.shader`) synced to `CrystalGolem` proximity | Refractive prism floor reflections & crystalline fracture dust when `CrystalGolem` slams |
| `5` | **CursedForest** (`Combat`) | Eerie willow-green lantern flames (`#4CE885` / `#C2FFE0`) | Swamp miasma fog ribbons (`#1A3326`) + drifting toxic fireflies | Twisted root aura wisps & subtle poison spore bloom near `CursedForest` props | Falling dry leaves / spores; rotting timber dust when stepping on `WoodFloor_*` plank bridges |
| `6` | **BossArena** (`Boss`) | Blazing Abyssal-Purple braziers (`#B833FF` / `#F0B8FF`) around arena ring | Swirling dark void storm haze (`#331445`) + upward ash updraft | Arena perimeter seal runes pulsing faster per Boss phase (Phase 3 turns outer floor into purple fire ring) | Cracked obsidian floor fissures (`FloorStoryDecals`) radiating abyssal heat haze during slams |

---

## 2. Shader & Emitter Standardization for Environment VFX

1. **Always Prefer Mesh/Shader Effects for Static Repeating Props**:
   - Wall torches and braziers (`SpawnWallTorch`, `SpawnFireBrazier`) use `GetCrossedFlameMesh()` + `AbyssalTorchFlame.shader` (GPU instanced, zero CPU particle simulation!). Add small rising `ParticleSystem` embers **only on hero-proximal braziers on `Medium`/`High`**.
   - Room-floor fog (`SpawnGroundMistPlane`) uses `AbyssalGroundMist.shader` (disable `GroundMistPlane` automatically on `Low` preset via `GraphicsManager` or keep ultra-cheap single alpha quad).
2. **Trap Readability (`WorldDungeonTrap`)**:
   - **Pre-trigger telegraph (`2.1s – 2.6s` of `3.6s` cycle)**: faint floor grate glow/heat shimmer or iron rattle sparks **MUST play even on `Low`** so players can jump (`y >= 0.52m`) cleanly!
   - **Active hazard (`2.6s – 3.6s`)**: `FireTrap` spawns 4 crossed flames + scorch embers; `SpikeTrap` bursts upward metallic sparks + stone dust.
3. **Distance & Light Budgets (`EnvironmentDistanceCuller`)**:
   - Never attach real-time `LightShadows` to environmental torch `Point` lights (`pl.shadows = LightShadows.None`).
   - Only enable `TorchFlickerBehaviour` and environmental `AmbientDustEmberParticles` inside the player's current and adjacent room (`cullDistMeters`: `38m` Low, `52m` Medium, `72m` High).


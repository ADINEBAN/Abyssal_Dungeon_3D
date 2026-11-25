# Systematic Environment VFX Placement Across All 7 Dungeon Biomes

Dungeon levels in `Abyssal_Dungeon_3D` generate sequentially through 7 themed rooms (`DungeonWorldBuilder` + `ModularDungeonArchitect` + `BiomeAtmosphereProfile`) bounded by a global view/culling distance (`EnvironmentDistanceCuller` + `GraphicsManager.CurrentViewDistanceMeters`).

---

## 1. Zone-by-Zone Placement Blueprint

Walk every generated room systematically along the **Northward Critical Path (`-Z → +Z`)**:

| Room # | Biome / Type | Mandatory Focal Anchors | Wall & Perimeter Placement | Floor / Trench / Hazard Rules |
| --- | --- | --- | --- | --- |
| **0** | `ForgottenTemple` (`Entrance`) | Warm Antique-Gold brazier pair framing spawn altar & exit gate | Alternating warm wall sconces (`SpawnWallTorch`) at pillar bays (`>= 8m` spacing) | Very light violet-slate dust motes; NO ground haze obscuring party spawn inspect look |
| **1** | `AncientDungeon` (`Combat`) | Amber sconce lighting guiding eye toward north iron gate; unlocked chest glow | Torch sconces on load-bearing pillars; max `4–6` dynamic `Point` lights active in room | `FloorStoryDecals` (cracks/scorch) + `AbyssalGroundMist` in room corners (keep center combat lane crisp); trap telegraph sparks on `WorldDungeonTrap` |
| **2** | `UndergroundFortress` (`Elite`) | Twin fiery gate braziers signaling Elite arena entrance | High-intensity forge torches along fortified archways | Subtle rising heat embers over `MetalFloor_*` iron grate decks; zero thick smoke over elite silhouettes |
| **3** | `AbyssalCave` (`Puzzle`) | **3 `WorldRunePillar` pedestals** (cool blue -> antique gold) + **`RuneSealEnergyBeam` sightlines** converging on north gate | Indigo crystal wall sconces leading visually from puzzle pillars to locked portcullis | Low cool sapphire cavern mist in side recesses; leave beam trajectories unobstructed for instant puzzle readability |
| **4** | `CrystalChamber` (`MiniBoss`) | Central glowing Crystal cluster + `CrystalGolem` spawn plinth | Electric cyan crystal sconces (`AbyssalCrystalLit` pulse veins) | Reflective mineral dust motes drifting lazily; NO bright mist near floor so CrystalGolem slam telegraphs pop at 100% contrast |
| **5** | `CursedForest` (`Combat`) | Emerald willow lanterns + `WoodFloor_*` bridge edges | Moss-lit wall/arch lanterns every `10–12m` | Low curling swamp miasma around outer perimeter walls & tree props + occasional slow green fireflies (`maxParticles <= 24`) |
| **6** | `BossArena` (`Boss`) | Perimeter Abyssal-Purple braziers defining arena circle + Phase 3 outer void fire boundary + post-kill `WorldExitPortal` | Symmetrical ring of purple-flame pillars outside combat radius (`ArenaRadius`) | **Center arena floor MUST stay crystal-clear dark stone** (`no thick fog`) so `AbyssalBossAI` Charge lanes, Shockwaves, and Meteor Storm telegraphs are readable from every camera angle! |

---

## 2. Camera Sightline & Player-Path Checklist

1. **Never Block the Camera Ray**: Place rising pillar smokes, brazier flame stacks, and tall portals **along room walls, corners, and behind focal gates** — never along the central camera-to-player line (`y = 2m–6m` behind hero).
2. **Sightline Wayfinding**: Use warm gold/amber/cyan torch pairs (`SpawnWallTorch`) to naturally lead players toward the next `DungeonGate`, `WorldHealingShrine`, `WorldTreasureChest`, or `WorldExitPortal`.
3. **Occlusion & Pop-In Safety**: Set `EnvironmentDistanceCuller` radii and particle `cullingMode` (`Pause` vs `AlwaysSimulate` — use `Pause` for ambient dust/torches offscreen, `AlwaysSimulate` ONLY for continuous timed boss arena hazards!).
4. **Point Light Disciplined Pairing**: Maximum **2–4 shadowless `Point` lights** visible simultaneously per room on `Low`, **6–8** on `Medium`, **10–12** on `High`. Remaining distant sconces use **pure emissive `AbyssalTorchFlame` + baked/vertex shading** with `withPointLight = false`!


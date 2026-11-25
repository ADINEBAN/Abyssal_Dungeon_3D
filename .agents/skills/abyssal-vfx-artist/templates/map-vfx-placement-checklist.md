# Map & Dungeon Biome VFX Placement Checklist (`Dungeon.unity` / `DungeonWorldBuilder`)

Use this checklist to verify environmental VFX across all **7 Dungeon Rooms** from Entrance to Boss Arena.

## Room-by-Room Sweep (`0 → 6`)
- [ ] **Room 0 (`ForgottenTemple` / Entrance)**: Warm Antique-Gold torches framing spawn & exit gate; dust subtle; spawn party silhouette crystal clear.
- [ ] **Room 1 (`AncientDungeon` / Combat)**: Amber sconces (`>= 8m` spacing); corner ground mist only; `WorldDungeonTrap` pre-trigger telegraph clearly readable at `2.1s–2.6s`.
- [ ] **Room 2 (`UndergroundFortress` / Elite)**: Forge-orange gate braziers; iron grate sparks (`MetalFloor_*`); central Elite combat lane free of obscuring fog.
- [ ] **Room 3 (`AbyssalCave` / Puzzle)**: Cool indigo wall sconces; all **3 `WorldRunePillar`** crystals glow blue when unlit -> antique-gold when lit; **`RuneSealEnergyBeam` sightlines** cleanly reach `DungeonGate`.
- [ ] **Room 4 (`CrystalChamber` / MiniBoss)**: Cyan sconces; `AbyssalCrystalLit` pulse veins; floor contrast kept clean for `CrystalGolem` slam telegraphs.
- [ ] **Room 5 (`CursedForest` / Combat)**: Willow-green lanterns; outer-wall swamp miasma; `WoodFloor_*` bridge dust active on Medium/High.
- [ ] **Room 6 (`BossArena` / Boss)**: Perimeter Abyssal-Purple braziers outside combat ring; central obsidian stone floor clear of heavy haze for Boss telegraphs; Phase 3 outer fire boundary reads cleanly; `WorldExitPortal` + chest clear on victory.

## Camera, Light & Culling Verification
- [ ] Walked full `-Z → +Z` critical path in `OrbitCamera3D` view: **zero particles or torch beams block camera-to-player ray**.
- [ ] Every room complies with active `Point` light cap (`<= 4` Low, `<= 8` Medium, `<= 12` High) and `LightShadows.None` on all torches.
- [ ] `EnvironmentDistanceCuller` culls distant rooms/emitters beyond `GraphicsManager.CurrentViewDistanceMeters`.
- [ ] Tested switching `Low` / `Medium` / `High` live in `Dungeon.unity`: `Low` disables ground mist & secondary dust cleanly while keeping traps, shrines, portals, and boss telegraphs 100% readable.


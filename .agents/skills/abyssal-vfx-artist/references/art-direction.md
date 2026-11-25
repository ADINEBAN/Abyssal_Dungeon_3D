# Abyssal Dungeon 3D — VFX Art Direction & Ancient Gothic Style Guide

All visual effects in **Abyssal Dungeon 3D** must speak one cohesive visual language rooted in **Ancient Gothic Dark Fantasy**: weathered obsidian, engraved runic stone, tarnished gold and bronze relics, and sinister abyssal magic piercing deep cavern shadows.

Machine-readable palette hexes and HDR bounds live in [vfx-style-manifest.json](../assets/vfx-style-manifest.json).

---

## 1. Visual Pillars

1. **Dark Value Separation First**: Dungeon rooms (`BiomeAtmosphereProfile`) sit in low-value slate, obsidian, and mossy stone (`fogColor` luminance < 0.08). VFX read instantly because bright spectral cores (`> 1.0` HDR) contrast against dark smoky rims (`0.05–0.18`).
2. **Silhouette Over Particle Soup**: A clean crescent mesh (`T_Vfx_SlashCrescent`), crisp rune ring (`T_Vfx_GothicRuneCircle`), or sculpted ribbon (`AbyssalWeaponTrail.shader`) communicates better at 30 particles than a blurry 250-particle billboard cloud.
3. **Ancient Gothic Ornamentation**: Circles are never generic sci-fi HUD rings — frame AoEs, portals, buffs, and boss hazards with pointed gothic arches, diamond rune notches, crackle fractures (`T_Vfx_GroundCrack`), and tarnished metallic sparks.
4. **Elemental Readability Without Neon Slop**: Keep saturation grounded via warm charcoal/bronze smoke or dark indigo/obsidian backing shells so bright colors read like alchemical fire, frost crystal, consecrated gold, or eldritch void rather than cyberpunk neon.

---

## 2. Core Palette Hierarchy

| Tone | Hex | Linear/RGB Approx | Use Case |
| --- | --- | --- | --- |
| **Antique Gold** | `#D4AF37` | `(0.83, 0.69, 0.22)` | Cleric/Guardian consecrated wards, Level-Up pillars, Rune Pillar activation, Legendary loot beams |
| **Aged Bronze** | `#8C6239` | `(0.55, 0.38, 0.22)` | Sconce embers, armor-hit metallic sparks, warrior shockwave grit, dusty warmth |
| **Dark Brown / Ash** | `#3B281E` | `(0.23, 0.16, 0.12)` | Soft ambient smoke, stone stomp debris, decaying soot wisps |
| **Obsidian Black** | `#121016` | `(0.07, 0.06, 0.09)` | Void tear cores, shadowstep silhouettes, Dark Knight corrupted aura rims |
| **Arcane Purple** | `#8A3FFC` | `(0.54, 0.25, 0.99)` | Abyssal portals, Warlock curses, Abyssal Knight boss telegraphs & Phase 3 storm |

### Elemental Readability Rules (3-Value Ramp)
Every elemental VFX MUST compose from three distinct values:
1. **Hot Core (15–25% area, HDR `1.6–2.6`)**: Desaturated near-white tint of the hue (e.g., `#FFE08A` for Fire, `#E6FAFF` for Ice, `#FFF5C8` for Holy).
2. **Primary Hue Body (50–60% area, HDR `1.0–1.5`)**: Unmistakable elemental color.
3. **Dark Rim / Smoke Skirt (20–35% area, LDR `0.05–0.25`)**: Grounds additive streaks inside the 3D dungeon and prevents washed-out overdraw.

---

## 3. HDR & Bloom Discipline

`AbyssalPostProcessing.cs` configures URP `Bloom` at `threshold = 0.95f`, `intensity = 0.55f`, `scatter = 0.66f` under ACES Tonemapping:
- **Never exceed `2.6` HDR scalar** on additive materials (`AbyssalSkillVfx.shader` `_MainColor` / `_EdgeColor`, `AbyssalWeaponTrail.shader`). Values above `3.0` clip ACES into white blobs and obscure enemy animations during co-op chaos.
- **Keep atmospheric smoke, ground mist (`AbyssalGroundMist.shader`), and ambient dust strictly `< 0.75` luminance** so Bloom ignores background clutter and only kisses blade edges, spell cores, torches, and crystal veins (`AbyssalCrystalLit.shader`).

---

## 4. Camera Readability & Telegraph Hierarchy

The default third-person orbit camera (`OrbitCamera3D`) views heroes from above and behind at 8–12m range:
- **Priority 1 — Lethal Enemy & Boss Telegraphs**: Crimson/violet ground decals + rising edge fill (`CharacterVisualFactory.SpawnGroundTelegraph`) MUST have sharp outer boundary borders (`_RadialMask` edge falloff <= `0.08`) and **MUST remain 100% visible even on `Low` VFX quality** (`ScalableVfxResponder.isCombatTelegraph = true`).
- **Priority 2 — Player Action Confirmation**: Weapon trails (`WeaponBladeTrail3D`), impact bursts (`ExpandingBurstFx`), directional sparks (`DirectionalSparkBurstFx`), and dodge clarity.
- **Priority 3 — Allies & Ambient Dungeon**: Keep co-op ally and ambient biome dust/fog **35–50% lower alpha and scale** when clustered near the center of the camera frustum.

---

## 5. Forbidden Visual Anti-Patterns

- **NO flat white/magenta fallback cubes or spheres** left active in finished combat prefabs (audit all `PrimitiveType` fallbacks in `CharacterVisualFactory` / `ProceduralVFXFactory` when shipping final polished effects).
- **NO blinding full-screen flash overlays** on common hits (`ScreenCombatFeedbackUI` owns brief 3–5% border vignettes and surge flashes—VFX should not duplicate screen-covering quads).
- **NO high-opacity additive stacking** over enemy windup silhouettes (`alpha * particleCount` in center > `0.85` hides incoming Boss swings).
- **NO hard billboard clipping into floors** on `Low`/`Medium`/`High`—lift ground rings `y = +0.02m` (`0.014m+` above floor collider, matching `ModularDungeonArchitect` decals) and offset spark origins toward attacker normal.


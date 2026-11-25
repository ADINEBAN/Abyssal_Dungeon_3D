# Skill & Elemental VFX Architecture — 10 Elements & 40 Class Skills

Abyssal Dungeon 3D defines **40 hero skills** across 10 classes in `AbyssGame.GameDB` (`ActorUnit.UseSkill`, `SkillKind` switch) plus data-driven `AbyssalDungeon.Classes.SkillData` assets. Every skill effect MUST follow the **4-Stage Choreography Rule** and pick a clear element and spatial category so players recognize friend vs foe spells instantly.

---

## 1. The 4-Stage Skill Choreography Rule

| Stage | Timing Window | Purpose | Core Visual Elements |
| --- | --- | --- | --- |
| **1. Anticipation (Windup)** | `0.10s – 0.45s` (matches cast delay) | Signal intent & range before damage | Caster hand/weapon gather wisps + subtle floor rune outline or cone marker |
| **2. Execution (Active)** | Hitbox / projectile flight lifetime | Communicate force, speed & vector | Projectile ribbon, beam core, slash crescent, expanding nova wave |
| **3. Impact (Detonation)** | `0.15s – 0.35s` flash frame | Confirm hit & element cleanly | Directional spark burst, `T_Vfx_RadialBurstRing`, brief target rim flash |
| **4. Dissipation (Aftermath)** | `0.40s – 1.80s` tail | Fade naturally without pop-out | Rising embers, scorch/frost/crack decal fade, curling smoke wisps |

---

## 2. The 10 Elemental Palettes & Motion Signatures

| Element | Core / Mid / Rim Colors | Motion & Shape Language |
| --- | --- | --- |
| **Fire** | `#FFE08A` / `#FF6A1A` / `#7A1608` | Buoyant upward lick flames, crackling embers, ash swirl, cracked magma floor decals |
| **Ice** | `#E6FAFF` / `#52C7F2` / `#16507A` | Sharp angular crystal shards, low ground frost blooms (`T_Vfx_FrostFlower`), crisp icy mist |
| **Lightning** | `#F5FDFF` / `#70D6FF` / `#2C4BFF` | Jagged UV bolts (`T_Vfx_Lightning`), snappy 2-frame strobes, radial static ground arcs |
| **Poison** | `#C5FF7A` / `#53A824` / `#1D3B10` | Viscous bubbling pools, sagging spore clouds, dripping caustic drops |
| **Holy** | `#FFF5C8` / `#D4AF37` / `#8C6239` | Symmetrical gothic cathedral halos, vertical sunbeams (`T_Vfx_PillarBeam`), warm gold dust |
| **Shadow** | `#B084FF` / `#3A1C59` / `#0E0914` | Negative-space obsidian silhouettes, inward-sucking smoke tendrils, violet rim slashes |
| **Arcane** | `#E3C8FF` / `#8A3FFC` / `#341678` | Rotating astrolabe rings (`T_Vfx_GothicRuneCircle`), crisp geometric rune projectiles |
| **Nature** | `#D6FFC4` / `#389E52` / `#1B4229` | Spiral vines/thorns, drifting glowing pollen, emerald mist rings |
| **Blood** | `#FF6E7A` / `#A61124` / `#30050A` | Heavy crimson-ink sweeps, jagged crimson pulse rings, boiling red mist |
| **Abyssal** | `#C88CFF` / `#6610C9` / `#10081A` | Twisted void rifts, gravity-distorting black holes, jagged abyssal lightning |

---

## 3. All 15 Skill Effect Categories

1. **Slash Wave / Projectile Crescent**: `Knight`, `Berserker` (`T_Vfx_BladeWave`)
2. **Ground Slam & Shockwave**: `Knight`, `Guardian`, `Berserker`, `Monk` (`T_Vfx_GroundCrack` + `T_Vfx_ShockRing`)
3. **Cone Cleave / Breath**: `Berserker`, `Demon` (`T_Vfx_SlashCrescent` wide sweep)
4. **Homing / Linear Missile Volley**: `Mage`, `Ranger`, `Warlock` (Core orb + `TrailRenderer`)
5. **Meteor / Sky Drop / Orbital Strike**: `Mage` Fireball/Meteor, `AbyssalBossAI` Meteor Storm (Warning circle -> falling core -> crater blast)
6. **Chain / Forked Arc**: `Monk`, `Mage` Arcanist (`LineRenderer` multi-point jagged arc with `T_Vfx_Lightning`)
7. **Expanding Nova / Pulse Ring**: `Cleric`, `Guardian`, `Knight` (`T_Vfx_NovaWave`)
8. **Channeled Pillar / Beam**: `Cleric`, `Mage`, `Warlock` (`T_Vfx_PillarBeam` + rotating base rune)
9. **Persistent Hazard Zone / DoT Field**: `Warlock`, `Druid`, `Ranger` Poison/Frost trap (`Ground decal` + rising motes)
10. **Summon / Conjure Rift**: `Warlock` Soul Minion, `Boss` summon (`T_Vfx_GothicRuneCircle` + vertical portal cylinder)
11. **Defensive Dome / Barrier**: `Guardian`, `Cleric` (`Sanctuary`, `Aegis`, `Citadel`) — fresnel shell + orbiting plates
12. **Dash / Blink / Shadow Step**: `Assassin`, `Knight` (`Dash` trail + origin/target burst)
13. **Tether / Life Drain / Link**: `Warlock`, `Cleric` (`LineRenderer` spline flowing from target to caster)
14. **Transform / Ascension**: `Druid` (`BearForm`), `Berserker` (`Undying Rage`), `Boss` Phase transitions
15. **Aura / Party Warcry**: `Guardian`, `Cleric`, `Knight` (Expanding translucent wave + brief golden/blue chest ring on buffed allies)

---

## 4. Mapping to Existing 40 `GameDB` Class Skills (`SkillKind`)

| Class | `[Q/1]` | `[W/2]` | `[E/3]` | `[R/4 Ultimate]` |
| --- | --- | --- | --- | --- |
| **Knight** | Shield Slam (Holy/Steel) | Holy Cleave (BladeWave Gold-Blue) | Bastion Guard (Shield Dome) | Judgment Call (Pillar Beam + Nova) |
| **Guardian** | Shield Wave (ShockRing) | Sanctuary (Party Shield Dome) | Aegis (Golden Fortify Pulse) | Citadel (Taunt + 50% Bastion Ring) |
| **Berserker** | Bloody Cleave (Blood Crescent) | Whirlwind (Spinning Blade Ring) | Blood Rage (Crimson Aura + Sparks) | Earthbreaker (Ground Crack + Slam) |
| **Assassin** | Twin Fang (Dual Shadow Slash) | Poison Blade (Venom Coat Trail) | Shadow Step (Blink Obsidian Burst) | Death Blossom (Multi-Crescent Nova) |
| **Ranger** | Piercing Arrow (Wind Beam) | Poison Volley (Caustic Fan) | Evasive Roll (Wind Dash) | Rain of Arrows (Sky Volley Telegraphed) |
| **Mage** | Fireball / Frostbolt (Missile) | Frost Nova (FrostFlower + Ring) | Arcane Surge (Astrolabe + Mana Pulse) | Meteor / Arcane Cataclysm (Sky Drop) |
| **Warlock** | Dark Bolt (Void Orb) | Curse / Plague (Hex Sigil DoT) | Soul Minion (Summon Portal Rift) | Abyssal Cataclysm (Void Rift Nova) |
| **Monk** | Chi Strike (Gold-Lightning Palm) | Cyclone Kick (Spiral Wind Ring) | Iron Body / Mantra (Golden Barrier) | Hundred Palms / Tempest (Rapid Multi-Arc) |
| **Druid** | Thorns (Briar Ground Spikes) | Regrowth (Emerald Heal Pillar) | Bear Form (Transform Burst) | Nature's Wrath (Storm + Heal Ring) |
| **Cleric** | Smite (Sunbeam Lance) | Holy Light (Warm Gold Heal Burst) | Purify / Ward (Cleanse + Barrier) | Divine Sanctuary (Cathedral Halo Dome) |

When implementing or upgrading any skill above, drive the VFX from `ActorUnit` skill invocation timestamps and pass `isHostile` / `element` to pooled prefabs without touching skill cooldown math or damage radius.


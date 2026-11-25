# Interaction, Progression & UI-World VFX Separation

This reference covers **Domain G (Loot, Progression & Dungeon Interactions)** and **Domain H (World-Space vs Screen-Space UI VFX)**. Existing systems live in [DungeonWorldBuilder.cs](../../../Assets/Scripts/Game/Dungeon/DungeonWorldBuilder.cs), [CombatFeelManager.cs](../../../Assets/Scripts/Game/Combat/CombatFeelManager.cs), and [ScreenCombatFeedbackUI.cs](../../../Assets/Scripts/Game/Combat/ScreenCombatFeedbackUI.cs).

---

## 1. Domain G — Loot, Progression & Interactables

| World Event | Existing Hook | Required Multi-Layer VFX |
| --- | --- | --- |
| **Loot Orb Drop (`LootOrbPickup`)** | `LootOrbPickup.Spawn(pos, item)` + `GameDB.RarityColors` | **6 Rarity Tiers** must look distinctly tiered:<br>- `Common (#B8B8C0)`: dim spinning mesh, no pillar beam<br>- `Uncommon (#4CE07A)`: soft short beam (`0.8m`), tiny motes<br>- `Rare (#4CA6FF)`: crisp azure beam (`1.3m`), slow ground ring<br>- `Epic (#A852FF)`: violet rune halo + upward spiral wisps (`1.8m`)<br>- `Legendary (#FFAD29)`: antique-gold gothic flare + wide pillar beam (`2.6m`) + chime sparks<br>- `Mythic (#FF4242)`: deep crimson-gold void column + pulsing ground rune crest (`3.2m`) |
| **Treasure Chest (`WorldTreasureChest`)** | `UnlockChest()` / `Interact()` | **Locked**: crimson `LockGem`. **Unlocked**: emerald `LockGem` pulse (`AbyssalCrystalLit`). **Open**: lid blast of golden coins/sparks (`T_Vfx_StarFlare` + `T_Vfx_RadialBurstRing`) + loot fan-out arc |
| **EXP & Level-Up** | `DungeonWorldBuilder.AwardExpAndGold` | **Level-Up**: Golden cathedral pillar (`T_Vfx_PillarBeam`) + base expanding rune crest (`T_Vfx_GothicRuneCircle`, `T_Vfx_NovaWave`) + ascending gold sparks + gold `"LEVEL UP!"` floating text |
| **Skill Unlock / Altar Buff** | Altar / Level-Up skill unlock | Brief swirling arcane-gold filigree around hero torso (`0.7s`) + crest snap |
| **Healing Shrine (`WorldHealingShrine`)** | `Interact()` (+70% HP/Mana & cleanse) | Emerald-gold crystal shatter dissipation + party-wide rising emerald lotus wisps + base cleanse ring on each hero |
| **Soul Anchor Respawn** | `TriggerSoulAnchorPartyRespawn()` | Massive antique-gold & cyan anchor wave (`radius 7.5m`) pushing nearby enemies visually with radial shock rings + rising golden shield domes on all 4 heroes |
| **Puzzle Rune Pillar (`WorldRunePillar`)** | `Interact()` + `RuneSealEnergyBeam` | Unlit sapphire glow -> gold awakening burst + persistent streaming `RuneSealEnergyBeam` connecting Pillar to exit `DungeonGate` |
| **Exit / Victory Portal (`WorldExitPortal`)** | `WorldExitPortal.Create` on Boss kill | `AbyssalPortal` stone arch + rotating `EscapeFloorPortal` runic vortex ring + inward spiraling violet-gold gateway motes (`Light` range `12m`) |
| **Dungeon Clear Celebration** | `MarkRoomCleared` / `CompleteRun` | Room gate lock-chain shatter flashes + golden banner aura flare when room seals break |

---

## 2. Domain H — World-Space vs Screen-Space UI VFX Boundaries

Keep 3D combat spaces cleanly separated from HUD overlays:

| Effect Purpose | Owner Layer | Strict Rule |
| --- | --- | --- |
| **Combat Telegraphs** | **World-Space** (`SpawnGroundTelegraph`, `AbyssalBossAI`) | Must anchor to `y = +0.02m` on dungeon floor with depth-clean edge border (`isCombatTelegraph = true`). Never hide behind tall mist planes. |
| **Interactable Prompts (`[F]`)** | **World-Space Highlight + HUD Prompt** | Subtle pulsing crystal/ring aura on interactable + clean HUD prompt text; no giant floating 3D arrows blocking character head. |
| **Damage Numbers** | **World-Space Pooled Canvas/Mesh** (`FloatingCombatText` in `GameObjectPool`) | Critical hits scale `1.35x` gold with slight pop curve; regular hits `1.0x` white/red; Heal emerald green; Immune/Blocked muted silver. Zero per-hit allocation! |
| **Damage / Low-HP / Surge Flash** | **Screen-Space** (`ScreenCombatFeedbackUI`) | Handled solely via edge border vignettes (`#8C1018` hit, `#D4AF37` gold combat surge) and combo/poise HUD cards — 3D VFX must NEVER spawn camera-blocking quads. |
| **Skill Cooldown / Ready Pulse** | **Screen-Space HUD** | Brief antique-gold corner glint on the HUD hotbar slot when Q/W/E/R finishes cooldown. |
| **Shield / Status Icons** | **World Rim + HUD Bar** | 3D barrier shows subtle Fresnel armor rim; exact numerical shield/HP bar stays cleanly in HUD / compact overhead unit plate. |
| **Boss Phase Transition** | **Both (Synchronized)** | **World**: Boss shockwave + arena shift. **HUD**: Brief dark-purple gothic phase banner + target poise/HP bar flash. |


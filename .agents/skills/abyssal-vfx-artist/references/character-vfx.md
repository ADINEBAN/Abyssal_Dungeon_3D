# Character Visual Effects — All 10 Playable Classes & Hero States

Every playable class in `AbyssGame.GameDB` uses an exported Mixamo rig (`Paladin`, `Vanguard`, `Kachujin`, plus `Hero_DruidBeast` in BearForm) dressed with procedural dark armor and class silhouettes in [ClassVisualArmorKit.cs](../../../Assets/Scripts/Game/Character/ClassVisualArmorKit.cs). All character VFX attach cleanly to rig bones or `ActorUnit` root without modifying combat cooldowns or damage authority.

---

## 1. The 10 Playable Classes — Elemental & Aesthetic Identity

| ID | Class | Base Rig | Signature Equipment & Back Relic | Elemental / Aesthetic Motif | Passive Class VFX Signature |
| --- | --- | --- | --- | --- | --- |
| `0` | **Knight** | `Paladin` | `ClassWeapon_KnightSword`, `ClassShield_KnightCrest`, Royal Visor Plume | Steel-Blue & Consecrated Gold (`#63A3FF` / `#D4AF37`) | Faint blue-gold glint along shield crest on guard/combo 3 |
| `1` | **Guardian** | `Paladin` | `ClassWeapon_GuardianHammer`, `ClassShield_GuardianBulwark`, Bastion Crown | Bastion Amber-Gold & Iron (`#D4AF37` / `#8C6239`) | Heavy goldenward pulse around shield rim; golden bastion ring under aura |
| `2` | **Berserker** | `Vanguard` | `ClassWeapon_BerserkerCleaver`, Horned Skull Helm, Shoulder Spikes | Blood Crimson & Forge Ember (`#D9332E` / `#FF6A1A`) | Rising blood-ember heat wisps as HP drops or during `Bloodthirster` rage |
| `3` | **Assassin** | `Kachujin` | Dual `ClassWeapon_AssassinDagger` (`RH_WP` + `LH_WP`), `AssassinCloak` | Shadow Violet & Venom Green (`#964CE8` / `#53A824`) | Wispy obsidian-violet cloak motes at heels while sprinting/stealthing |
| `4` | **Ranger** | `Kachujin` | Bow in `LH_WP`, `RangerQuiver` on Spine, Feathered Hood | Emerald Gale & Briar Green (`#5AD470` / `#C5FF7A`) | Subtle wind-leaf spiral around bow limbs at draw; quiver arrow glint |
| `5` | **Mage** | `Kachujin` | `ClassWeapon_MageStaff`, orbiting `MageAstrolabe` rings behind back | Alchemical Fire, Frost & Arcane (`#FA8A33` / `#52C7F2` / `#8A3FFC`) | Astrolabe ring rotation speed rises with mana spend; staff orb ember motes |
| `6` | **Warlock** | `Vanguard` | `ClassWeapon_WarlockScepter`, orbiting `WarlockGrimoire` & dark halo | Abyssal Void & Soul Violet (`#A345F0` / `#121016`) | Grimoire drifting dark-violet rune embers; faint obsidian distortion ring |
| `7` | **Monk** | `Kachujin` | Dual `ClassWeapon_MonkGauntlet`, orbiting `MonkBeads` prayer halo | Chi Lightning Gold (`#F5D959` / `#70D6FF`) | Prayer beads spark tiny golden-cyan static arcs on rapid combos |
| `8` | **Druid** | `Vanguard` / `Hero_DruidBeast` | `ClassWeapon_DruidScytheStaff`, Twin Antler Crest | Ancient Grove Emerald & Storm (`#4ACC78` / `#70D6FF`) | Falling spore/leaf motes at feet; fur-burst & amber roar ring on BearForm morph |
| `9` | **Cleric** | `Paladin` | `ClassWeapon_ClericMace`, `ClassShield_ClericReliquary`, Halo Crown | Sunlit Holy Gold (`#FCEB94` / `#D4AF37`) | Soft warm halo crown shimmer above `Bip001 Head`; golden dust motes during heal |

> Note on Cleric weapon rig: `ClassVisualArmorKit` equips `ClassWeapon_ClericMace` on `RH_WP` + `ClassShield_ClericReliquary` on `LH_WP` (hammer/mace + reliquary shield), while `GameDB` marks `WeaponKind.Mace`. Author Mace/Hammer trails and consecrated wave arcs to fit both cleanly.

---

## 2. Standard Character Lifecycle VFX Matrix

Every hero shares a unified character VFX layer scaled by `GraphicsManager.ParticleDensityMultiplier` and `VfxQualitySystem`:

| Character State | Trigger Source | Visual Layers | Budget / Tier Behavior |
| --- | --- | --- | --- |
| **Spawn / Room Entry** | `DungeonWorldBuilder.SpawnParty` | Rising gothic rune circle (`0.8s`) + upward spectral motes | Low: flat circle + fade; High: circle + 16 motes + vertical light shaft |
| **Idle Aura** | Constant (signature classes) | Slow-orbiting relic motes (`MageAstrolabe`, `WarlockGrimoire`, `MonkBeads`, Cleric halo) | **Off on Low**, `0.5x` rate on Medium, full on High |
| **Footsteps** | `CharacterMotionPolisher` bone height check | Surface-typed puff (`Stone`, `Wood`, `Metal`, `Dirt` via collider prefix `WoodFloor_`, `MetalFloor_`) | **Off on Low**, simple billboard dust on Medium, dust + tiny grit on High |
| **Walk / Run / Sprint** | `ActorUnit` speed > sprint threshold | Heel dust cadence + trailing speed wisps behind boots/cloak | Scale emission strictly by `Speed` (`0` when stopped) |
| **Dash / Dodge Roll** | `ActorUnit` dodge window | `CharacterMotionPolisher.SpawnDodgeGhost` translucent afterimage + directional wind ring | Low: 1 afterimage; Medium/High: 3 stepped ghosts (`0.28s` fade) |
| **Jump & Land** | `CharacterMotionPolisher` (`TriggerJumpTakeoff` / `TriggerLandImpact`) | Squash/stretch + outward ground dust ring (`T_Vfx_ShockRing`) proportional to fall velocity | Low: small dust ring; High: ring + stone chips |
| **Teleport / Blink** | Assassin `Phantom Step` / Mage blink / Soul Anchor | Disappear obsidian/arcane implosion at origin -> arrival rune flare at destination | Never hide target destination for > `0.12s` |
| **Buffs & Debuffs** | `ActorUnit.ApplyStatus` / `StatusVisualAttachment` | Floating chest/waist ring + element-coded loop particles (`Burn`, `Poison`, `Freeze`, `Stun`, `Bleed`, `DefUp`, `AtkUp`, `Haste`) | Max **1 active icon ring + 1 compact emitter** per unit |
| **Shield / Barrier** | `ActorUnit.AddShield` (`Guardian`, `Cleric`, `Knight`) | Hex/gothic runic translucent dome or rotating orbit plates when `Shield > 0` | Fresnel rim shader, zero center opacity (< `0.15`) so character stays sharp |
| **Heal / Cleanse** | `ActorUnit.ReceiveHeal` / `CleanseDebuffs` | Upward emerald-gold motes (`VFXType.Heal`) + gentle base chime ring | Burst `8–16` particles, auto-pool back |
| **Low-HP Warning** | HP <= `25%` (`ScreenCombatFeedbackUI` + world) | Subtle slow-pulsing crimson-ember heartbeat rim at character waist + screen border pulse | Never obscure ground hazards beneath feet |
| **Death & Revive** | `ActorUnit` `IsDowned` / `IsDead` / `Revive` | Downed: kneeling dark ember tether; Final Death: ash dispersion + rising soul wisp; Revive: golden pillar (`Soul Anchor`) | Keep downed body silhouette visible for teammates to read |

---

## 3. BearForm Transformation (`Druid` Special Case)

When `ActorUnit` toggles `Druid` `BearForm`:
1. **Morph Burst (`0.0s`)**: Root-centered emerald-amber shockwave (`T_Vfx_NovaWave`) + swirling leaf/fur vortex (`0.45s`) masking the mesh swap to `Hero_DruidBeast`.
2. **Active Aura**: Persistent faint earthen-amber ground glow while `IsBearForm == true`.
3. **Revert Burst**: Reverse inward leaf-dissolve restoring the human `Vanguard` rig cleanly.


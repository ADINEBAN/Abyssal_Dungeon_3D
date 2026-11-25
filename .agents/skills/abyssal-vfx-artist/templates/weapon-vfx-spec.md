# Weapon & Combat VFX Spec — `<WeaponName / Family>`

- **Target Weapon Family & Class(es)**: `<SwordShield / GreatSword / Daggers / Bow / Staff / Wand / Fists / Mace / TowerShield>`
- **Hand Socket(s)**: `RH_WP` (`SignatureMainWeapon`) / `LH_WP` (`SignatureOffhand`)
- **Trail Tip Offset & Width**: `localTipOffset = (x, y, z)`, `bladeWidth = <m>`
- **Primary Color Palette**: `<Core / Mid / Edge Hex>`

## Combat Events Specification

| Event | Trigger Callback (`WeaponHitboxSweep3D` / `CombatFeelManager`) | Trail / Mesh / Particle Layers | Duration (`s`) | Shared Assets Used (`T_Vfx_*`, `AbyssalWeaponTrail.shader`) |
| --- | --- | --- | --- | --- |
| **Combo 1 Swing** | `onWindupComplete` (`-18 deg`) | | | |
| **Combo 2 Swing** | `onWindupComplete` (`+20 deg`) | | | |
| **Combo 3 / Heavy** | `onWindupComplete` (`0 deg`, `1.35x`) | | | |
| **Normal Hit** | `onHitVictim(victim, contactPoint)` | `DirectionalSparkBurstFx` + MPB flash | `0.18s` | |
| **Critical Hit** | `onHitVictim(..., isCrit: true)` | `1.45x` sparks + `T_Vfx_StarFlare` + shock ring | `0.25s` | |
| **Block / Parry** | Guard / Counter event | Shield rune ripple + metallic sparks | `0.22s` | |
| **Charged / Ultimate** | Hold release / Slot 4 | Inward charge gather -> empowered trail | | |

## Trail & Pool Hygiene
- [ ] Trail emitting toggles **only** during active swing window (`emitting = false` in idle/hit-stun/death).
- [ ] Zero cross-room stretched trails on projectile/object pool recycle (`Clear()` before teleport/disable).
- [ ] `ShadowCastingMode.Off` and `receiveShadows = false` verified on `TrailRenderer` & slash meshes.


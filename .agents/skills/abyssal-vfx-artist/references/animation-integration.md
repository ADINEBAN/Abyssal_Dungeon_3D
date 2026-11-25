# Animation & Combat Timing Integration

VFX feel broken the instant a sword ribbon trails behind a sheath or a spell detonates before a staff rises. This guide documents the exact `Animator`, `.anim` clip, bone socket, and sweep timing hooks in **Abyssal Dungeon 3D**.

---

## 1. Exported Animation Clips & Parameters

All character and enemy clips live under [Assets/Art/Exported/Animations/](../../../Assets/Art/Exported/Animations/):
- **Heroes (`Hero_<Class>_*` & `Hero_Common_*`)**: `Attack`, `Cast`, `Idle`, `Jump`, `Run`, `Death`, `Dodge`, `Hit`, `Sprint`.
- **Enemies (`<Enemy>_*`)**: `Attack`, `Cast`, `Death`, `Hit`, `Idle`, `Run`, `Walk` for all 10 enemies + `AbyssalKnight`, `SoulMinion`, and `DruidBeast`.

### Verified Animator Parameters
- **Active 3D Game (`ActorUnit`)**: `Speed`, `LocoSpeedMult`, `IsGrounded`, `Jump`, `Hit`, `Attack`, `Cast`, `Dodge`, `IsDead`.
- **Modular Player Script (`PlayerAnimationController.cs`)**: `Speed`, `IsGrounded`, `AttackIndex`, `IsAttacking`, `IsDodging`, `IsDead`, `SkillIndex`, `Attack` trigger, `UseSkill` trigger, plus `OnAttackImpact()` Animation Event hook.

---

## 2. Three Synchronization Patterns (Choose the Right Hook)

| Pattern | When to Use | How to Wire in Abyssal Dungeon 3D |
| --- | --- | --- |
| **1. `WeaponHitboxSweep3D` Callbacks** *(Preferred for Melee)* | Sword/Hammer/Cleaver/Dagger/Mace swings & `WeaponBladeTrail3D` | Hook trail `Activate(activeDuration, color)` and swing `SpawnSlashArc` into `onWindupComplete` (so trail starts ONLY when blade swings forward, never during draw-back), and spawn sparks inside `onHitVictim(victim, contactPoint)`. |
| **2. Procedural Action Timers** *(Used for Runtime-Built Classes / Co-op AI)* | `ActorUnit.UseSkill`, `AbyssalBossAI` windups, `CharacterMotionPolisher` | Derive windup anticipation duration directly from the authoritative cast/windup timer (`windupDelay`), and trigger impact burst strictly when the skill executes its damage query (`elapsed >= impactTime`). |
| **3. Unity `AnimationEvent` Hooks** *(Best for Custom Keyframed Clips)* | Specific footfalls, bow release, staff apex slam on `.anim` files | Place lightweight named events (e.g., `OnAttackImpact()`, `OnFootstep()`, `OnVfxBurst(int slot)`) on the `.anim` clip. Always guard event handlers with `if (!IsDead && gameObject.activeInHierarchy)` and supply a procedural fallback timer in case an AI clip blends early! |

---

## 3. Humanoid Avatar Rig Sockets & Attachment Hygiene

Check `ClassVisualArmorKit.cs` when binding world/weapon VFX:
- **Right Hand Weapon**: `RH_WP` (or `Bip001 R Hand`) -> `SignatureMainWeapon` -> `BladeTrailTip` (`WeaponBladeTrail3D`).
- **Left Hand Weapon / Shield**: `LH_WP` (or `Bip001 L Hand`) -> `SignatureOffhand` (`Ranger` bow, shields, dual-wield offhand daggers/gauntlets).
- **Head / Overhead Aura**: `Bip001 Head` / `HP_Head` (use for Cleric halo, enemy aggro glint, stagger ring — offset `+0.35m` above crown).
- **Torso / Core Buffs**: Chest bone (`Spine2` / `Hips + 1.0m`) for shield domes, heal vortices, and burn/poison status emitters.
- **Root / Feet (`transform.position`)**: Footstep dust, dodge ghosts, ground telegraphs (`y = +0.02m`), and aura rings.

### Rules for Smooth Transition & Cancellation
1. **Cancelled Swing / Hit Stun**: If `ActorUnit` enters `Hit`, `Stun`, or `IsDead` mid-windup, **immediately stop anticipation emitters and disable `WeaponBladeTrail3D` emitting** (`trail.emitting = false`, `CancelAllSweeps()`). Never let a cancelled attack spawn a phantom explosion or lingering ground danger telegraph!
2. **State Blend Pop Prevention**: Never hard-Destroy a `ParticleSystem` attached to a disappearing weapon (`BearForm` transform or death). Call `ps.Stop(true, ParticleSystemStopBehavior.StopEmitting)` and let existing particles finish or return the pooled child after `maxLifetime`.
3. **Never Alter Gameplay Speed for VFX**: Do not scale `Animator.speed` inside VFX scripts; `CombatFeelManager` owns hit-stop timing centrally.


# VFX Prefab Technical Spec — `VFX_<Domain>_<Subject>_<Variant>`

- **Prefab Path**: `Assets/Prefabs/VFX/VFX_<Domain>_<Subject>_<Variant>.prefab`
- **VFXType / Pool Key**: `<VFXType enum or GameObjectPool key>`
- **Duration & Auto-Return Mode**: `Duration = <__>s`, `Stop Action = Disable` (`Pool return timer = <__>s`)

## Hierarchy Blueprint

```text
VFX_<Domain>_<Subject>_<Variant>      [ScalableVfxResponder]
  ├── Telegraph_Ground                [MeshRenderer / Projector; isCombatTelegraph=true if danger tell]
  ├── Core_Primary                    [ParticleSystem / SM_Vfx_* Mesh]
  ├── Accent_RibbonOrArc              [TrailRenderer / LineRenderer / Crescent Mesh]
  ├── Secondary_EmbersOrSmoke         [Optional ParticleSystem — disabled on Low]
  └── Optional_PointLight             [Point Light: Shadows=None, High-only, fade <= 0.35s]
```

## Technical Budgets & Component Audit

| Layer | Material Used | Shader (`Abyssal*` / `URP/Particles/Unlit`) | Texture (`T_Vfx_*`) | `maxParticles` (L / M / H) | Cast/Recv Shadows |
| --- | --- | --- | --- | --- | --- |
| `Telegraph_Ground` | | | | Static Mesh (`1` draw) | `Off / Off` |
| `Core_Primary` | | | | `__ / __ / __` | `Off / Off` |
| `Accent_RibbonOrArc` | | | | | `Off / Off` |
| `Secondary_EmbersOrSmoke` | | | | `0 / __ / __` | `Off / Off` |

## Pooling & Shader Safety
- [ ] Zero `Collider` or gameplay script on prefab.
- [ ] Zero runtime `new Material(...)` leak; uses `MaterialPropertyBlock` or `ParticleSystem` startColor.
- [ ] Total `SetPass` additions per active instance `<= 3`.
- [ ] Registered with `VfxQualitySystem` / `VFXManager` / `GameObjectPool`.


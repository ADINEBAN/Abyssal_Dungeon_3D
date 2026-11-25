# Blender 5.3.0 Workflow via `blender-mcp` — Custom VFX Meshes for Unity URP

**Live Tooling Confirmed**: `blender-mcp` is connected to **Blender 5.3.0** (`get_addon_status`, `get_scene_info`, `execute_blender_code`, `look`, `search_assets`, `import_asset`, `generate_3d`). Note that remote `AssetLibraries` and `Generators` are currently disabled on the addon — author custom VFX meshes procedurally using Python (`bpy` / `bmesh`) via `execute_blender_code` and visually verify every mesh with `look`!

---

## 1. Decision Rule: When to Author in Blender vs Unity Built-in Systems

| Effect Asset | Author in **Blender (`.fbx`)** | Author in **Unity (`ParticleSystem`/`Trail`/`Line`/`Shader`)** |
| --- | --- | --- |
| **Curved Slash Crescent Shell** (custom UV pan arc) | **YES** (clean swept arc strip with radial 0→1 UVs) | Pair with `AbyssalWeaponTrail.shader` / `AbyssalSkillVfx.shader` |
| **Twisted Energy Cone / Tornado / Shock Dome** | **YES** (faceted spiral or hemisphere with aligned polar UVs) | Animate UV scroll & dissolve in URP `.shader` |
| **Ground Crack / Rune Altar Relief / Crystal Shard Cluster** | **YES** (low-poly jagged crystal/rock cluster for spawning/shatter) | Animate rise/scale + `AbyssalCrystalLit.shader` |
| **Muzzle / Portal Ring Arch / Astrolabe Halo** | **YES** (chamfered gothic ring geometry) | Pair with emissive trim + ParticleSystem motes |
| **Billboard Smoke, Fire, Sparks, Rain, Dust, Aura Motes** | **NO** | Build directly with `ParticleSystem` + `Universal Render Pipeline/Particles/Unlit` |
| **Dynamic Weapon Blade Trails & Lightning Splines** | **NO** | Build with `WeaponBladeTrail3D` (`TrailRenderer`) & `LineRenderer` |

---

## 2. Blender Python (`execute_blender_code`) Rules for Clean Unity FBX Meshes

When generating any VFX mesh in Blender for `Abyssal_Dungeon_3D`:

1. **UV Mapping Is Everything**:
   - **Slash Arcs / Tornados**: Unwrap linearly so `U` runs along sweep direction (`0 → 1`) and `V` runs across blade/ribbon width (`0 → 1`). This lets `AbyssalSkillVfx.shader` / `AbyssalWeaponTrail.shader` pan and alpha-taper seamlessly.
   - **Radial Discs / Domes**: Put center at `(0.5, 0.5)` or map `V = 0` at base and `V = 1` at rim/tip so shaders fade cleanly without seam artifacts.
2. **Vertex Color Support**:
   - Paint vertex alpha or RGB gradient (`1.0` core → `0.0` sharp outer fade) when helpful so a single material drives dozens of silhouettes with zero extra texture samples.
3. **Triangle / Poly Budgets (Keep Tight!)**:
   - **Slash Crescent / Ribbon Shell**: `48 – 160` tris.
   - **Shockwave / Portal Ring**: `64 – 240` tris.
   - **Crystal Shard / Rock Debris (for Mesh Particle emission)**: `24 – 80` tris.
   - **Aura Dome / Vortex Cone**: `160 – 480` tris.
4. **Pivot & Axis Standards for Unity**:
   - Ground impacts & aura domes: **pivot at bottom center `(0, 0, 0)`**, sitting cleanly on `Z = 0` in Blender (maps to `Y = 0` ground in Unity).
   - Slash crescents & projectiles: **pivot at emitter/caster center `(0, 0, 0)`**, pointing forward along Blender `-Y` (Unity `+Z` after FBX transform).
   - Apply transforms cleanly (`bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)`).
   - Check face normals outward (`bmesh.ops.recalc_face_normals`); disable Backface Culling only in Unity shaders where double-sided ribbons are intended (`Cull Off`).

---

## 3. Mandatory Blender Inspection & Unity FBX Export Settings

Before calling any Blender VFX mesh finished:
1. Run `look` (Blender viewport render) from **3/4 isometric gameplay angle** and **front angle** to confirm clean arc smoothness, zero UV stretching spikes, and non-overlapping faces.
2. Export FBX to `Assets/Art/VFX/Meshes/SM_Vfx_<Name>.fbx` using Unity-compatible settings:
   - `use_selection=True`
   - `apply_scale_options='FBX_SCALE_ALL'` (or `1.0` metric meters matching Unity `1 unit = 1m`)
   - `axis_forward='-Z'`, `axis_up='Y'` (standard Unity forward `+Z`, up `+Y`)
   - `bake_space_transform=True`
   - `mesh_smooth_type='FACE'`, `use_mesh_modifiers=True`
   - **Disable** `add_leaf_bones=True`, lights, and cameras on export!
3. **In Unity Model Importer** (`ModelImporter` rules):
   - Disable `Import Cameras`, `Import Lights`, `Import Materials` (assign shared URP VFX materials explicitly!).
   - Set `Mesh Compression` to `Medium` and enable `Optimize Mesh`.
   - Disable `Generate Colliders` and `Cast Shadows` on `MeshRenderer`.
4. **Clean Up Blender Scene After Export**: Save source `.blend` separately or delete temporary scratch objects if exporting only the target mesh so the next Blender session stays clean.


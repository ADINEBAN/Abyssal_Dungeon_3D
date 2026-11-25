"""
ABYSSAL DUNGEON 3D — BATCH 1 BLENDER WEAPON ASSET PRODUCTION
Generates bespoke Ancient Gothic Dark Fantasy 3D weapons in Blender 5.3.0:
  1. Knight_Longsword       (Main blade, Gothic crossguard, grip, pommel, glowing rune fuller + sockets)
  2. Knight_HeaterShield    (Curved heater body, Gothic gold rib trim, central crest boss, rune sigil, back grip + sockets)
  3. Berserker_Greatsword   (Heavy twin-fuller serrated blade, horned guard, 2H hilt, spiked pommel, ember runes + sockets)
  4. Ranger_RecurveBow      (Sculpted recurve limbs, riser grip, bronze caps, separate straight riggable 2-segment bowstring + sockets)
  5. Ranger_Arrow           (Bodkin hunting broadhead, bronze collar, ash shaft, raven-emerald fletching + nock origin)

Coordinate convention (exported with axis_forward='-Z', axis_up='Y', bake_space_transform=True):
  Blender (+X, +Y, +Z) -> Unity (-X, +Z, +Y)
  Therefore:
    - Model along Blender +Y  => Unity +Z (Forward / Blade length / Shield height / Bow shoot direction)
    - Model along Blender +Z  => Unity +Y (Up / Shield front face / Bow vertical limbs)
    - Model along Blender +/-X => Unity -/+X (Width)

Outputs:
  - Editable .blend files: VFX_Blender_Production/02_Weapons/<Class>/<Group>/Blender/
  - Preview PNG renders:   VFX_Blender_Production/02_Weapons/<Class>/<Group>/Previews/ + Captures/WeaponUiRedesign/Previews/
  - FBX exports:           VFX_Blender_Production/02_Weapons/<Class>/<Group>/FBX/ + Assets/Art/Weapons/Batch1/
  - PBR Textures (1024px): Assets/Art/Weapons/Textures/
  - Manifest JSON:         Assets/Art/Weapons/weapon_asset_manifest.json
"""

import os
import math
import json
import shutil
import bpy
import bmesh
from mathutils import Vector, Matrix, Euler

ROOT = r"C:\LTG\Abyssal_Dungeon_3D"
VFX_WEAPONS_ROOT = os.path.join(ROOT, "VFX_Blender_Production", "02_Weapons")
UNITY_WEAPONS_ROOT = os.path.join(ROOT, "Assets", "Art", "Weapons")
UNITY_BATCH1_DIR = os.path.join(UNITY_WEAPONS_ROOT, "Batch1")
UNITY_TEX_DIR = os.path.join(UNITY_WEAPONS_ROOT, "Textures")
CAPTURE_PREVIEWS_DIR = os.path.join(ROOT, "Captures", "WeaponUiRedesign", "Previews")

for d in [UNITY_WEAPONS_ROOT, UNITY_BATCH1_DIR, UNITY_TEX_DIR, CAPTURE_PREVIEWS_DIR]:
    os.makedirs(d, exist_ok=True)


# =============================================================================
# 1. PROCEDURAL 1024x1024 PBR TEXTURE GENERATION (VIA BLENDER IMAGE API + NUMPY)
# =============================================================================

def generate_pbr_textures():
    import numpy as np

    size = 512  # Crisp 512x512 weapon tileables (sharp in 3rd-person, light VRAM on Intel Iris Xe)
    y_grid, x_grid = np.mgrid[0:size, 0:size].astype(np.float32) / float(size)

    def hash2d(gx, gy, seed=1.0):
        n = np.sin(gx * 127.1 + gy * 311.7 + seed * 74.7) * 43758.5453
        return n - np.floor(n)

    def smooth_noise(freq_x, freq_y, seed=1.0):
        ux = x_grid * freq_x
        uy = y_grid * freq_y
        ix = np.floor(ux)
        iy = np.floor(uy)
        fx = ux - ix
        fy = uy - iy
        fx = fx * fx * (3.0 - 2.0 * fx)
        fy = fy * fy * (3.0 - 2.0 * fy)
        a = hash2d(ix % freq_x, iy % freq_y, seed)
        b = hash2d((ix + 1) % freq_x, iy % freq_y, seed)
        c = hash2d(ix % freq_x, (iy + 1) % freq_y, seed)
        d = hash2d((ix + 1) % freq_x, (iy + 1) % freq_y, seed)
        return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy

    def fbm(fx, fy, octaves=4, seed=1.0):
        val = np.zeros((size, size), dtype=np.float32)
        amp = 0.5
        for o in range(octaves):
            val += smooth_noise(fx * (2 ** o), fy * (2 ** o), seed + o * 13.7) * amp
            amp *= 0.5
        return np.clip(val, 0.0, 1.0)

    def save_rgba(path, r, g, b, a=None):
        if a is None:
            a = np.ones((size, size), dtype=np.float32)
        rgba = np.stack([np.clip(r, 0, 1), np.clip(g, 0, 1), np.clip(b, 0, 1), np.clip(a, 0, 1)], axis=-1)
        img = bpy.data.images.new(os.path.basename(path), width=size, height=size, alpha=True)
        img.pixels.foreach_set(rgba.ravel())
        img.filepath_raw = path
        img.file_format = 'PNG'
        img.save()
        bpy.data.images.remove(img)

    def height_to_normal(h, strength=2.6):
        dx = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) * strength
        dy = (np.roll(h, -1, axis=0) - np.roll(h, 1, axis=0)) * strength
        nx = -dx
        ny = -dy
        nz = np.ones_like(h)
        inv_len = 1.0 / np.sqrt(nx * nx + ny * ny + nz * nz + 1e-6)
        return nx * inv_len * 0.5 + 0.5, ny * inv_len * 0.5 + 0.5, nz * inv_len * 0.5 + 0.5

    # A. Dark Forged Gothic Steel (Damascus grain + fine battle scratches)
    n_steel = fbm(8, 24, 4, seed=1.3)
    scratch = smooth_noise(48, 6, seed=4.2) * 0.25 + smooth_noise(12, 64, seed=9.1) * 0.15
    h_steel = np.clip(n_steel * 0.72 + scratch, 0.0, 1.0)
    r = 0.26 + h_steel * 0.22
    g = 0.28 + h_steel * 0.23
    b = 0.33 + h_steel * 0.26
    save_rgba(os.path.join(UNITY_TEX_DIR, "T_Wpn_DarkSteel_Albedo.png"), r, g, b)
    nx, ny, nz = height_to_normal(h_steel, 3.0)
    save_rgba(os.path.join(UNITY_TEX_DIR, "T_Wpn_DarkSteel_Normal.png"), nx, ny, nz)
    # Unity URP MetallicGlossMap: R=Metallic, A=Smoothness
    met = np.clip(0.82 + h_steel * 0.14, 0, 1)
    sm = np.clip(0.50 + h_steel * 0.28, 0, 1)
    save_rgba(os.path.join(UNITY_TEX_DIR, "T_Wpn_DarkSteel_Mask.png"), met, met, met, sm)

    # B. Antique Cathedral Gold / Burnished Bronze (engraved filigree ridges + dark patina crevices)
    n_gold = fbm(10, 10, 4, seed=11.4)
    filigree = 0.5 + 0.5 * np.sin(x_grid * math.pi * 16.0 + n_gold * 2.4) * np.cos(y_grid * math.pi * 16.0)
    h_gold = np.clip(n_gold * 0.65 + filigree * 0.35, 0.0, 1.0)
    save_rgba(os.path.join(UNITY_TEX_DIR, "T_Wpn_AntiqueGold_Albedo.png"),
              0.46 + h_gold * 0.44, 0.33 + h_gold * 0.37, 0.14 + h_gold * 0.18)
    nx, ny, nz = height_to_normal(h_gold, 3.2)
    save_rgba(os.path.join(UNITY_TEX_DIR, "T_Wpn_AntiqueGold_Normal.png"), nx, ny, nz)
    met_g = np.clip(0.76 + h_gold * 0.20, 0, 1)
    sm_g = np.clip(0.46 + h_gold * 0.34, 0, 1)
    save_rgba(os.path.join(UNITY_TEX_DIR, "T_Wpn_AntiqueGold_Mask.png"), met_g, met_g, met_g, sm_g)

    # C. Wrapped Dark Leather & Ebony Wood Grip
    wrap_bands = np.abs(np.sin((y_grid * 14.0 + x_grid * 2.0) * math.pi))
    grain = fbm(16, 4, 4, seed=27.9)
    h_leath = np.clip(wrap_bands * 0.55 + grain * 0.45, 0.0, 1.0)
    save_rgba(os.path.join(UNITY_TEX_DIR, "T_Wpn_LeatherWood_Albedo.png"),
              0.15 + h_leath * 0.18, 0.10 + h_leath * 0.12, 0.08 + h_leath * 0.09)
    nx, ny, nz = height_to_normal(h_leath, 3.8)
    save_rgba(os.path.join(UNITY_TEX_DIR, "T_Wpn_LeatherWood_Normal.png"), nx, ny, nz)
    met_l = np.full((size, size), 0.05, dtype=np.float32)
    sm_l = np.clip(0.18 + h_leath * 0.22, 0, 1)
    save_rgba(os.path.join(UNITY_TEX_DIR, "T_Wpn_LeatherWood_Mask.png"), met_l, met_l, met_l, sm_l)

    # D. Gothic Rune Glow Pattern (for emissive fuller inlays & shield runes)
    center_band = np.exp(-((x_grid - 0.5) * 6.5) ** 2)
    rune_glyphs = np.clip(np.sin(y_grid * math.pi * 24.0) * np.cos((x_grid - 0.5) * math.pi * 12.0) * 1.4 + 0.55, 0.0, 1.0)
    diamond_sigil = np.clip(1.0 - (np.abs((x_grid - 0.5) * 4.0) + np.abs(((y_grid * 8.0) % 1.0) - 0.5) * 2.0), 0.0, 1.0)
    glow = np.clip(center_band * (0.45 + 0.55 * np.maximum(rune_glyphs, diamond_sigil)), 0.0, 1.0)
    save_rgba(os.path.join(UNITY_TEX_DIR, "T_Wpn_GothicRune_Emission.png"), glow, glow, glow)


# =============================================================================
# 2. SCENE & MATERIAL HELPERS
# =============================================================================

def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1.0


def make_collection(name, parent=None):
    col = bpy.data.collections.new(name)
    if parent is None:
        bpy.context.scene.collection.children.link(col)
    else:
        parent.children.link(col)
    return col


def link_obj(obj, col):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    col.objects.link(obj)


def make_pbr_mat(name, base_color, metallic=0.8, roughness=0.35, emission=None, emission_strength=0.0, tex_prefix=None):
    if name in bpy.data.materials:
        return bpy.data.materials[name]
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*base_color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness

    if tex_prefix:
        alb_path = os.path.join(UNITY_TEX_DIR, f"{tex_prefix}_Albedo.png")
        nrm_path = os.path.join(UNITY_TEX_DIR, f"{tex_prefix}_Normal.png")
        if os.path.exists(alb_path):
            tex_node = nt.nodes.new("ShaderNodeTexImage")
            tex_node.image = bpy.data.images.load(alb_path, check_existing=True)
            mix = nt.nodes.new("ShaderNodeMix")
            mix.data_type = 'RGBA'
            mix.blend_type = 'MULTIPLY'
            mix.inputs["Factor"].default_value = 0.72
            mix.inputs["A"].default_value = (*base_color, 1.0)
            nt.links.new(tex_node.outputs["Color"], mix.inputs["B"])
            nt.links.new(mix.outputs["Result"], bsdf.inputs["Base Color"])
        if os.path.exists(nrm_path):
            n_tex = nt.nodes.new("ShaderNodeTexImage")
            n_tex.image = bpy.data.images.load(nrm_path, check_existing=True)
            n_tex.image.colorspace_settings.name = 'Non-Color'
            n_map = nt.nodes.new("ShaderNodeNormalMap")
            n_map.inputs["Strength"].default_value = 0.85
            nt.links.new(n_tex.outputs["Color"], n_map.inputs["Color"])
            nt.links.new(n_map.outputs["Normal"], bsdf.inputs["Normal"])

    if emission is not None and emission_strength > 0.0:
        if "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = (*emission, 1.0)
        if "Emission Strength" in bsdf.inputs:
            bsdf.inputs["Emission Strength"].default_value = emission_strength

    return mat


def create_mesh_obj(name, bm, col, mat, smooth=False):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    # Clean planar/box UV unwrap directly in bmesh so every face has non-zero UVs
    uv_layer = bm.loops.layers.uv.verify()
    for f in bm.faces:
        n = f.normal
        ax, ay, az = abs(n.x), abs(n.y), abs(n.z)
        for loop in f.loops:
            v = loop.vert.co
            if az >= ax and az >= ay:
                loop[uv_layer].uv = (v.x * 1.6 + 0.5, v.y * 1.6 + 0.5)
            elif ax >= ay and ax >= az:
                loop[uv_layer].uv = (v.y * 1.6 + 0.5, v.z * 1.6 + 0.5)
            else:
                loop[uv_layer].uv = (v.x * 1.6 + 0.5, v.z * 1.6 + 0.5)

    mesh = bpy.data.meshes.new(name + "_Mesh")
    bm.to_mesh(mesh)
    bm.free()

    if smooth:
        for p in mesh.polygons:
            p.use_smooth = True

    obj = bpy.data.objects.new(name, mesh)
    link_obj(obj, col)
    if mat is not None:
        obj.data.materials.append(mat)
    return obj


def add_empty(name, location, col, parent=None, radius=0.05):
    emp = bpy.data.objects.new(name, None)
    emp.empty_display_type = 'ARROWS'
    emp.empty_display_size = radius
    emp.location = Vector(location)
    link_obj(emp, col)
    if parent is not None:
        emp.parent = parent
    return emp


def loft_profile(bm, sections, cap_start=True, cap_end=True):
    """Loft a list of (rings of Vector) into closed watertight tube with crisp normals."""
    rings = []
    for pts in sections:
        ring = [bm.verts.new(Vector(p)) for p in pts]
        rings.append(ring)
    n = len(rings[0])
    for r_idx in range(len(rings) - 1):
        r0 = rings[r_idx]
        r1 = rings[r_idx + 1]
        for i in range(n):
            j = (i + 1) % n
            try:
                bm.faces.new((r0[i], r0[j], r1[j], r1[i]))
            except ValueError:
                pass
    if cap_start and len(rings[0]) >= 3:
        try:
            bm.faces.new(list(reversed(rings[0])))
        except ValueError:
            pass
    if cap_end and len(rings[-1]) >= 3:
        try:
            bm.faces.new(rings[-1])
        except ValueError:
            pass
    return rings


def add_box(bm, center, size, rot_euler=(0, 0, 0)):
    mat = Matrix.Translation(Vector(center)) @ Euler(rot_euler, 'XYZ').to_matrix().to_4x4() @ Matrix.Diagonal((*size, 1.0))
    bmesh.ops.create_cube(bm, size=1.0, matrix=mat)


def add_cylinder(bm, p0, p1, r0, r1, segs=12):
    v0 = Vector(p0)
    v1 = Vector(p1)
    axis = v1 - v0
    length = axis.length
    if length < 1e-5:
        return
    z_dir = axis.normalized()
    up = Vector((0, 0, 1)) if abs(z_dir.z) < 0.95 else Vector((1, 0, 0))
    x_dir = up.cross(z_dir).normalized()
    y_dir = z_dir.cross(x_dir).normalized()
    ring0 = []
    ring1 = []
    for i in range(segs):
        a = (2.0 * math.pi * i) / segs
        ca, sa = math.cos(a), math.sin(a)
        ring0.append(v0 + (x_dir * ca + y_dir * sa) * r0)
        ring1.append(v1 + (x_dir * ca + y_dir * sa) * r1)
    loft_profile(bm, [ring0, ring1], cap_start=True, cap_end=True)


# =============================================================================
# 3. RENDER STUDIO PREVIEW (3-POINT GOTHIC LIGHTING)
# =============================================================================

def render_preview(root_objs, preview_path, cam_pos, cam_target):
    scene = bpy.context.scene
    # Use EEVEE or Workbench (fast & clean even headless!)
    for engine in ('BLENDER_EEVEE_NEXT', 'BLENDER_EEVEE', 'CYCLES'):
        try:
            scene.render.engine = engine
            break
        except Exception:
            continue

    scene.render.resolution_x = 768
    scene.render.resolution_y = 768
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = preview_path

    # Dark Gothic slate background
    world = bpy.data.worlds.new("PreviewWorld")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs["Color"].default_value = (0.025, 0.022, 0.032, 1.0)
        bg.inputs["Strength"].default_value = 0.85

    studio_col = make_collection("_PreviewStudio")

    # Camera
    cam_data = bpy.data.cameras.new("PreviewCam")
    cam_data.lens = 50.0
    cam_obj = bpy.data.objects.new("PreviewCam", cam_data)
    link_obj(cam_obj, studio_col)
    cam_obj.location = Vector(cam_pos)
    direction = Vector(cam_target) - cam_obj.location
    cam_obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
    scene.camera = cam_obj

    # Key (warm antique gold), Rim (cool arcane blue), Fill (soft amber)
    lights_spec = [
        ("KeyLight", (cam_pos[0] + 1.4, cam_pos[1] - 0.6, cam_pos[2] + 1.6), (1.0, 0.86, 0.68), 180.0),
        ("RimLight", (-cam_pos[0] - 1.2, cam_pos[1] + 1.2, cam_pos[2] + 1.4), (0.45, 0.65, 1.0), 140.0),
        ("FillLight", (cam_pos[0] - 1.5, cam_pos[1] - 1.0, cam_pos[2] - 0.5), (0.95, 0.72, 0.45), 60.0),
    ]
    for lname, lpos, lcol, lenergy in lights_spec:
        ld = bpy.data.lights.new(lname, type='POINT')
        ld.color = lcol
        ld.energy = lenergy
        ld.shadow_soft_size = 0.6
        lo = bpy.data.objects.new(lname, ld)
        lo.location = Vector(lpos)
        link_obj(lo, studio_col)

    try:
        bpy.ops.render.render(write_still=True)
    except Exception as ex:
        print(f"[WARN] Preview render skipped for {preview_path}: {ex}")

    # Remove preview studio before saving clean editable .blend / FBX!
    for o in list(studio_col.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    bpy.data.collections.remove(studio_col)


def export_weapon(weapon_name, class_name, group_name, root_obj, col, cam_pos, cam_target):
    # Ensure folders exist
    base_dir = os.path.join(VFX_WEAPONS_ROOT, class_name, group_name)
    blend_dir = os.path.join(base_dir, "Blender")
    fbx_dir = os.path.join(base_dir, "FBX")
    prev_dir = os.path.join(base_dir, "Previews")
    tex_ref_dir = os.path.join(base_dir, "Textures")
    for d in [blend_dir, fbx_dir, prev_dir, tex_ref_dir]:
        os.makedirs(d, exist_ok=True)

    # Render preview PNG
    prev_path = os.path.join(prev_dir, f"{weapon_name}_Preview.png")
    render_preview([root_obj], prev_path, cam_pos, cam_target)
    if os.path.exists(prev_path):
        shutil.copy2(prev_path, os.path.join(CAPTURE_PREVIEWS_DIR, f"{weapon_name}_Preview.png"))

    # Save editable .blend file
    blend_path = os.path.join(blend_dir, f"{weapon_name}.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)

    # Select all objects in weapon collection and export clean FBX
    bpy.ops.object.select_all(action='DESELECT')
    tris = 0
    for obj in col.objects:
        obj.select_set(True)
        if obj.type == 'MESH':
            obj.data.calc_loop_triangles()
            tris += len(obj.data.loop_triangles)

    fbx_prod_path = os.path.join(fbx_dir, f"{weapon_name}.fbx")
    fbx_unity_path = os.path.join(UNITY_BATCH1_DIR, f"{weapon_name}.fbx")

    bpy.ops.export_scene.fbx(
        filepath=fbx_unity_path,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        use_space_transform=True,
        bake_space_transform=True,
        axis_forward='Z',
        axis_up='Y',
        mesh_smooth_type='FACE',
        add_leaf_bones=False,
        bake_anim=False,
        path_mode='AUTO'
    )
    shutil.copy2(fbx_unity_path, fbx_prod_path)

    # Gather socket names
    sockets = [o.name for o in col.objects if o.type == 'EMPTY']
    meshes = [o.name for o in col.objects if o.type == 'MESH']
    print(f"[EXPORTED] {weapon_name} | tris={tris} | meshes={meshes} | sockets={sockets}")
    return {
        "name": weapon_name,
        "class": class_name,
        "group": group_name,
        "batch": 1,
        "triangles": tris,
        "meshes": meshes,
        "sockets": sockets,
        "blend": os.path.relpath(blend_path, ROOT).replace("\\", "/"),
        "fbx": os.path.relpath(fbx_unity_path, ROOT).replace("\\", "/"),
        "preview": os.path.relpath(prev_path, ROOT).replace("\\", "/")
    }


# =============================================================================
# 4. WEAPON BUILDERS — BATCH 1 (KNIGHT, BERSERKER, RANGER)
# =============================================================================

def build_knight_longsword():
    """
    Ancient Gothic Runic Longsword:
    Blade extends along Blender +Y => Unity +Z!
    GripPoint at (0, 0, 0). Pommel at y=-0.24, Crossguard at y=+0.11, Blade Tip at y=+1.22.
    """
    reset_scene()
    col = make_collection("Knight_Longsword")

    m_steel = make_pbr_mat("M_Wpn_DarkSteel", (0.62, 0.66, 0.74), metallic=0.90, roughness=0.28, tex_prefix="T_Wpn_DarkSteel")
    m_gold = make_pbr_mat("M_Wpn_AntiqueGold", (0.92, 0.74, 0.34), metallic=0.88, roughness=0.30, tex_prefix="T_Wpn_AntiqueGold")
    m_grip = make_pbr_mat("M_Wpn_LeatherGrip", (0.28, 0.18, 0.14), metallic=0.05, roughness=0.72, tex_prefix="T_Wpn_LeatherWood")
    m_rune = make_pbr_mat("M_Wpn_RuneGold", (1.0, 0.85, 0.42), metallic=0.25, roughness=0.20, emission=(1.0, 0.78, 0.28), emission_strength=3.2)

    root = add_empty("Knight_Longsword", (0, 0, 0), col, radius=0.08)

    # 1. Main Tapered Diamond-Section Blade (Blender +Y -> Unity +Z)
    bm_blade = bmesh.new()
    # Profile slices: (y, half_width_x, ridge_z, fuller_z)
    blade_slices = [
        (0.12, 0.044, 0.015, 0.008),  # Ricasso base
        (0.22, 0.041, 0.014, 0.007),
        (0.50, 0.036, 0.013, 0.006),
        (0.84, 0.030, 0.011, 0.005),
        (1.06, 0.022, 0.008, 0.004),
        (1.18, 0.009, 0.004, 0.002),
        (1.24, 0.0008, 0.0008, 0.0006),  # Sharp spear-point tip
    ]
    sections = []
    for y, wx, rz, fz in blade_slices:
        # 8-vertex gothic diamond w/ recessed fuller channel
        pts = [
            (wx, y, 0.0),
            (wx * 0.42, y, rz),
            (0.0, y, fz),
            (-wx * 0.42, y, rz),
            (-wx, y, 0.0),
            (-wx * 0.42, y, -rz),
            (0.0, y, -fz),
            (wx * 0.42, y, -rz),
        ]
        sections.append(pts)
    loft_profile(bm_blade, sections, cap_start=True, cap_end=True)
    blade_obj = create_mesh_obj("KnightSword_Blade", bm_blade, col, m_steel)
    blade_obj.parent = root

    # 2. Gothic Arched Crossguard + Pommel + Cathedral Collar (Antique Gold)
    bm_guard = bmesh.new()
    # Sculpted arched crossguard wings sweeping forward slightly (+Y)
    guard_steps = [-0.17, -0.12, -0.06, 0.0, 0.06, 0.12, 0.17]
    g_secs = []
    for gx in guard_steps:
        t = abs(gx) / 0.17
        sweep_y = 0.105 + (t ** 1.6) * 0.038
        wy = 0.024 * (1.0 - 0.35 * t)
        wz = 0.022 * (1.0 - 0.30 * t)
        if abs(t - 1.0) < 0.05:  # Flared Gothic finial tip
            wy *= 1.35
            wz *= 1.35
        g_secs.append([
            (gx, sweep_y + wy, 0.0),
            (gx, sweep_y, wz),
            (gx, sweep_y - wy, 0.0),
            (gx, sweep_y, -wz),
        ])
    loft_profile(bm_guard, g_secs, cap_start=True, cap_end=True)
    # Central Gothic shield escutcheon over ricasso
    add_box(bm_guard, (0.0, 0.125, 0.0), (0.068, 0.055, 0.036))
    # Grip decorative rings
    add_cylinder(bm_guard, (0, 0.075, 0), (0, 0.095, 0), 0.024, 0.028, segs=10)
    add_cylinder(bm_guard, (0, -0.155, 0), (0, -0.175, 0), 0.024, 0.022, segs=10)
    # Octagonal Gothic wheel-pommel with spire finial
    add_cylinder(bm_guard, (0, -0.175, 0), (0, -0.220, 0), 0.022, 0.038, segs=8)
    add_cylinder(bm_guard, (0, -0.220, 0), (0, -0.258, 0), 0.038, 0.012, segs=8)
    add_cylinder(bm_guard, (0, -0.258, 0), (0, -0.280, 0), 0.012, 0.002, segs=8)
    guard_obj = create_mesh_obj("KnightSword_GuardPommel", bm_guard, col, m_gold)
    guard_obj.parent = root

    # 3. Leather-Wrapped Ergonomic Grip
    bm_grip = bmesh.new()
    grip_rings = []
    for i in range(9):
        t = i / 8.0
        y = -0.155 + t * 0.235
        bulge = 1.0 + 0.14 * math.sin(t * math.pi) + 0.06 * math.sin(t * math.pi * 8.0)
        rx = 0.020 * bulge
        rz = 0.016 * bulge
        ring = [(rx * math.cos(a), y, rz * math.sin(a)) for a in [2 * math.pi * k / 10 for k in range(10)]]
        grip_rings.append(ring)
    loft_profile(bm_grip, grip_rings, cap_start=True, cap_end=True)
    grip_obj = create_mesh_obj("KnightSword_Grip", bm_grip, col, m_grip, smooth=True)
    grip_obj.parent = root

    # 4. Separate Glowing Runic Fuller Detail Mesh (for URP emissive bloom / aura)
    bm_rune = bmesh.new()
    add_box(bm_rune, (0.0, 0.46, 0.0095), (0.012, 0.56, 0.003))
    add_box(bm_rune, (0.0, 0.46, -0.0095), (0.012, 0.56, 0.003))
    add_box(bm_rune, (0.0, 0.125, 0.019), (0.022, 0.022, 0.006), rot_euler=(0, 0, math.pi * 0.25))
    add_box(bm_rune, (0.0, 0.125, -0.019), (0.022, 0.022, 0.006), rot_euler=(0, 0, math.pi * 0.25))
    rune_obj = create_mesh_obj("KnightSword_RuneDetail", bm_rune, col, m_rune)
    rune_obj.parent = root

    # 5. Standardized Sockets (Blender +Y -> Unity +Z)
    add_empty("GripPoint", (0, 0, 0), col, parent=root)
    add_empty("PrimaryAttach", (0, 0, 0), col, parent=root)
    add_empty("GuardPoint", (0, 0.11, 0), col, parent=root)
    add_empty("BladeStart", (0, 0.15, 0), col, parent=root)
    add_empty("BladeTip", (0, 1.24, 0), col, parent=root)
    add_empty("VfxTrailSocket", (0, 1.18, 0), col, parent=root)

    return export_weapon("Knight_Longsword", "Knight", "Sword_Shield", root, col,
                         cam_pos=(1.85, 0.50, 0.72), cam_target=(0.0, 0.48, 0.0))


def build_knight_shield():
    """
    Ancient Gothic Heater Crest Shield:
    Verified against live Knight LH_WP basis:
      Blender +Y => Unity +Z (Vertical height: bottom tip y=-0.46, top crest y=+0.38)
      Blender +Z => Unity +Y (Outer armored face normal +Z; back forearm/hand grip at z=-0.02)
      Blender +/-X => Unity -/+X (Shield width +/-0.30)
    """
    reset_scene()
    col = make_collection("Knight_HeaterShield")

    m_steel = make_pbr_mat("M_Wpn_DarkSteel", (0.50, 0.55, 0.64), metallic=0.88, roughness=0.30, tex_prefix="T_Wpn_DarkSteel")
    m_gold = make_pbr_mat("M_Wpn_AntiqueGold", (0.94, 0.74, 0.34), metallic=0.88, roughness=0.28, tex_prefix="T_Wpn_AntiqueGold")
    m_grip = make_pbr_mat("M_Wpn_LeatherGrip", (0.26, 0.17, 0.13), metallic=0.05, roughness=0.72, tex_prefix="T_Wpn_LeatherWood")
    m_rune = make_pbr_mat("M_Wpn_RuneGold", (1.0, 0.86, 0.44), metallic=0.25, roughness=0.20, emission=(1.0, 0.80, 0.32), emission_strength=3.0)

    root = add_empty("Knight_HeaterShield", (0, 0, 0), col, radius=0.08)

    # Heater shield outline points in (x, y) — gently curved along Z (convex forward +Z)
    outline_2d = [
        (0.00, 0.38),
        (0.16, 0.36),
        (0.29, 0.31),
        (0.30, 0.14),
        (0.26, -0.08),
        (0.17, -0.28),
        (0.00, -0.46),
        (-0.17, -0.28),
        (-0.26, -0.08),
        (-0.30, 0.14),
        (-0.29, 0.31),
        (-0.16, 0.36),
    ]

    def shield_z_curve(x, y, z_off):
        return z_off + 0.065 * (1.0 - (x / 0.32) ** 2) - 0.015 * (y / 0.46) ** 2

    # 1. Main Curved Steel Heater Body
    bm_body = bmesh.new()
    front_v = [bm_body.verts.new((x, y, shield_z_curve(x, y, 0.045))) for x, y in outline_2d]
    back_v = [bm_body.verts.new((x * 0.96, y * 0.96, shield_z_curve(x, y, 0.008))) for x, y in outline_2d]
    f_center = bm_body.verts.new((0.0, 0.04, 0.135))
    b_center = bm_body.verts.new((0.0, 0.04, 0.072))
    n = len(outline_2d)
    for i in range(n):
        j = (i + 1) % n
        bm_body.faces.new((front_v[i], front_v[j], f_center))
        bm_body.faces.new((back_v[j], back_v[i], b_center))
        bm_body.faces.new((front_v[j], front_v[i], back_v[i], back_v[j]))
    body_obj = create_mesh_obj("KnightShield_Body", bm_body, col, m_steel)
    body_obj.parent = root

    # 2. Ornate Gothic Antique-Gold Rim Border + Cathedral Cross Boss
    bm_trim = bmesh.new()
    for i in range(n):
        j = (i + 1) % n
        p0 = (outline_2d[i][0], outline_2d[i][1], shield_z_curve(outline_2d[i][0], outline_2d[i][1], 0.042))
        p1 = (outline_2d[j][0], outline_2d[j][1], shield_z_curve(outline_2d[j][0], outline_2d[j][1], 0.042))
        add_cylinder(bm_trim, p0, p1, 0.022, 0.022, segs=6)
    # Raised central cathedral cross ribs
    add_box(bm_trim, (0.0, -0.02, 0.118), (0.055, 0.66, 0.024))
    add_box(bm_trim, (0.0, 0.12, 0.120), (0.44, 0.055, 0.024))
    # Central pointed umbo boss
    add_cylinder(bm_trim, (0.0, 0.12, 0.125), (0.0, 0.12, 0.172), 0.075, 0.015, segs=8)
    trim_obj = create_mesh_obj("KnightShield_GoldCrest", bm_trim, col, m_gold)
    trim_obj.parent = root

    # 3. Glowing Holy Rune Inlay on Shield Crest
    bm_rune = bmesh.new()
    add_box(bm_rune, (0.0, -0.02, 0.132), (0.018, 0.56, 0.006))
    add_box(bm_rune, (0.0, 0.12, 0.134), (0.34, 0.018, 0.006))
    rune_obj = create_mesh_obj("KnightShield_RuneDetail", bm_rune, col, m_rune)
    rune_obj.parent = root

    # 4. Back Hand Grip & Forearm Leather Strap (centered cleanly at GripPoint (0,0,0))
    bm_grip = bmesh.new()
    add_cylinder(bm_grip, (-0.07, 0.0, 0.0), (0.07, 0.0, 0.0), 0.018, 0.018, segs=8)
    add_box(bm_grip, (-0.07, 0.0, 0.025), (0.024, 0.032, 0.055))
    add_box(bm_grip, (0.07, 0.0, 0.025), (0.024, 0.032, 0.055))
    # Forearm brace loop lower down
    add_box(bm_grip, (0.0, -0.18, -0.015), (0.14, 0.045, 0.022))
    grip_obj = create_mesh_obj("KnightShield_BackGrip", bm_grip, col, m_grip)
    grip_obj.parent = root

    # 5. Sockets
    add_empty("GripPoint", (0, 0, 0), col, parent=root)
    add_empty("PrimaryAttach", (0, 0, 0), col, parent=root)
    add_empty("ForearmAttach", (0, -0.18, -0.02), col, parent=root)
    add_empty("ShieldFrontCenter", (0, 0.10, 0.16), col, parent=root)

    return export_weapon("Knight_HeaterShield", "Knight", "Sword_Shield", root, col,
                         cam_pos=(0.82, 0.25, 1.05), cam_target=(0.0, -0.02, 0.06))


def build_berserker_greatsword():
    """
    Ancient Gothic Two-Handed Berserker Greatsword:
    Massive dark iron executioner/zweihander silhouette with parrying fangs, wide fullered blade,
    crimson ember rune inlays, and two-handed wrapped hilt (Primary + SecondaryGripPoint).
    Blender +Y => Unity +Z!
    """
    reset_scene()
    col = make_collection("Berserker_Greatsword")

    m_iron = make_pbr_mat("M_Wpn_DarkSteel", (0.42, 0.40, 0.44), metallic=0.88, roughness=0.34, tex_prefix="T_Wpn_DarkSteel")
    m_bronze = make_pbr_mat("M_Wpn_AntiqueGold", (0.78, 0.56, 0.26), metallic=0.86, roughness=0.36, tex_prefix="T_Wpn_AntiqueGold")
    m_grip = make_pbr_mat("M_Wpn_LeatherGrip", (0.22, 0.12, 0.10), metallic=0.05, roughness=0.76, tex_prefix="T_Wpn_LeatherWood")
    m_ember = make_pbr_mat("M_Wpn_RuneCrimson", (1.0, 0.24, 0.12), metallic=0.20, roughness=0.22, emission=(1.0, 0.22, 0.08), emission_strength=3.8)

    root = add_empty("Berserker_Greatsword", (0, 0, 0), col, radius=0.09)

    # 1. Massive Wide Twin-Fuller Greatsword Blade (y = 0.14 .. 1.48)
    bm_blade = bmesh.new()
    blade_slices = [
        (0.14, 0.056, 0.022, 0.012),  # Reinforced ricasso
        (0.32, 0.054, 0.021, 0.012),  # Parierhaken neck
        (0.38, 0.096, 0.022, 0.012),  # Flared parrying fangs!
        (0.46, 0.072, 0.020, 0.010),
        (0.78, 0.078, 0.020, 0.010),  # Broad cleaving belly
        (1.12, 0.070, 0.018, 0.009),
        (1.32, 0.048, 0.014, 0.007),
        (1.48, 0.001, 0.001, 0.0008),  # Brutal executioner tip
    ]
    sections = []
    for y, wx, rz, fz in blade_slices:
        sections.append([
            (wx, y, 0.0),
            (wx * 0.45, y, rz),
            (0.0, y, fz),
            (-wx * 0.45, y, rz),
            (-wx, y, 0.0),
            (-wx * 0.45, y, -rz),
            (0.0, y, -fz),
            (wx * 0.45, y, -rz),
        ])
    loft_profile(bm_blade, sections, cap_start=True, cap_end=True)
    # Brutal serrated spine notches along upper edges
    for ny in (0.58, 0.72, 0.86, 1.00):
        add_box(bm_blade, (0.082, ny, 0.0), (0.022, 0.036, 0.010), rot_euler=(0, 0, 0.45))
        add_box(bm_blade, (-0.082, ny, 0.0), (0.022, 0.036, 0.010), rot_euler=(0, 0, -0.45))
    blade_obj = create_mesh_obj("BerserkerGS_Blade", bm_blade, col, m_iron)
    blade_obj.parent = root

    # 2. Horned Gothic Crossguard + Heavy Spiked Pommel
    bm_guard = bmesh.new()
    gx_steps = [-0.23, -0.17, -0.09, 0.0, 0.09, 0.17, 0.23]
    g_secs = []
    for gx in gx_steps:
        t = abs(gx) / 0.23
        sy = 0.13 + (t ** 1.5) * 0.075  # Aggressive forward-swept horns
        wy = 0.034 * (1.0 - 0.35 * t)
        wz = 0.030 * (1.0 - 0.28 * t)
        if t > 0.92:
            wy *= 0.45
            wz *= 0.45
        g_secs.append([
            (gx, sy + wy, 0.0),
            (gx, sy, wz),
            (gx, sy - wy, 0.0),
            (gx, sy, -wz),
        ])
    loft_profile(bm_guard, g_secs, cap_start=True, cap_end=True)
    # Central iron skull-crest collar
    add_box(bm_guard, (0.0, 0.15, 0.0), (0.11, 0.085, 0.052))
    # Two-handed grip divider ring & spiked counterweight pommel
    add_cylinder(bm_guard, (0, -0.10, 0), (0, -0.13, 0), 0.027, 0.027, segs=10)
    add_cylinder(bm_guard, (0, -0.29, 0), (0, -0.35, 0), 0.026, 0.048, segs=8)
    add_cylinder(bm_guard, (0, -0.35, 0), (0, -0.41, 0), 0.048, 0.005, segs=8)
    guard_obj = create_mesh_obj("BerserkerGS_GuardPommel", bm_guard, col, m_bronze)
    guard_obj.parent = root

    # 3. Long Two-Handed Leather Grip (Primary y=0.0, Secondary y=-0.20)
    bm_grip = bmesh.new()
    grip_rings = []
    for i in range(13):
        t = i / 12.0
        y = -0.29 + t * 0.40
        bulge = 1.0 + 0.12 * math.sin(t * math.pi * 2.0) + 0.05 * math.sin(t * math.pi * 12.0)
        rx = 0.024 * bulge
        rz = 0.020 * bulge
        grip_rings.append([(rx * math.cos(a), y, rz * math.sin(a)) for a in [2 * math.pi * k / 10 for k in range(10)]])
    loft_profile(bm_grip, grip_rings, cap_start=True, cap_end=True)
    grip_obj = create_mesh_obj("BerserkerGS_Grip", bm_grip, col, m_grip, smooth=True)
    grip_obj.parent = root

    # 4. Molten Blood-Rune Fuller Inlay (Detail Mesh)
    bm_rune = bmesh.new()
    add_box(bm_rune, (0.0, 0.78, 0.013), (0.020, 0.82, 0.004))
    add_box(bm_rune, (0.0, 0.78, -0.013), (0.020, 0.82, 0.004))
    add_box(bm_rune, (0.0, 0.15, 0.027), (0.036, 0.036, 0.008), rot_euler=(0, 0, math.pi * 0.25))
    add_box(bm_rune, (0.0, 0.15, -0.027), (0.036, 0.036, 0.008), rot_euler=(0, 0, math.pi * 0.25))
    rune_obj = create_mesh_obj("BerserkerGS_RuneDetail", bm_rune, col, m_ember)
    rune_obj.parent = root

    # 5. Sockets
    add_empty("GripPoint", (0, 0, 0), col, parent=root)
    add_empty("PrimaryAttach", (0, 0, 0), col, parent=root)
    add_empty("SecondaryGripPoint", (0, -0.20, 0), col, parent=root)
    add_empty("GuardPoint", (0, 0.14, 0), col, parent=root)
    add_empty("BladeStart", (0, 0.20, 0), col, parent=root)
    add_empty("BladeTip", (0, 1.48, 0), col, parent=root)
    add_empty("VfxTrailSocket", (0, 1.42, 0), col, parent=root)

    return export_weapon("Berserker_Greatsword", "Berserker", "Greatsword", root, col,
                         cam_pos=(2.35, 0.56, 0.85), cam_target=(0.0, 0.54, 0.0))


def build_ranger_recurve_bow():
    """
    Ancient Gothic Recurve Bow:
    Standard Unity upright orientation:
      Blender +Z => Unity +Y (Vertical limbs: -0.84m .. +0.84m)
      Blender +Y => Unity +Z (Forward arrow shooting direction!)
      Blender +/-X => Unity -/+X (Bow thickness)
    IMPORTANT: Bowstring is built as TWO straight separate riggable meshes (BowString_Upper, BowString_Lower)
    pivoting cleanly at UpperLimbTip / LowerLimbTip so Unity can pull ArrowNock back during draw animations!
    """
    reset_scene()
    col = make_collection("Ranger_RecurveBow")

    m_wood = make_pbr_mat("M_Wpn_DarkWood", (0.28, 0.19, 0.13), metallic=0.08, roughness=0.58, tex_prefix="T_Wpn_LeatherWood")
    m_gold = make_pbr_mat("M_Wpn_AntiqueGold", (0.90, 0.72, 0.34), metallic=0.86, roughness=0.30, tex_prefix="T_Wpn_AntiqueGold")
    m_grip = make_pbr_mat("M_Wpn_LeatherGrip", (0.20, 0.14, 0.11), metallic=0.05, roughness=0.76, tex_prefix="T_Wpn_LeatherWood")
    m_rune = make_pbr_mat("M_Wpn_RuneEmerald", (0.34, 0.98, 0.56), metallic=0.15, roughness=0.20, emission=(0.24, 1.0, 0.48), emission_strength=3.4)
    m_string = make_pbr_mat("M_Wpn_BowString", (0.86, 0.82, 0.72), metallic=0.0, roughness=0.65)

    root = add_empty("Ranger_RecurveBow", (0, 0, 0), col, radius=0.08)

    # 1. Sculpted Smooth Double-Recurve Bow Limbs + Ergonomic Riser
    bm_bow = bmesh.new()
    # Profile from Lower Tip (z=-0.84) to Upper Tip (z=+0.84)
    # y > 0 = forward toward target; y < 0 = backward toward archer
    curve_pts = [
        (-0.84, -0.09, 0.009, 0.011),  # Lower recurve tip
        (-0.74, -0.14, 0.013, 0.017),  # Lower recurve knee
        (-0.56, -0.06, 0.016, 0.022),  # Mid-lower limb
        (-0.34, +0.05, 0.020, 0.026),  # Inner limb belly
        (-0.16, +0.075, 0.022, 0.028), # Lower riser fade
        (0.00,  +0.045, 0.021, 0.026), # Smooth ergonomic recessed grip
        (+0.16, +0.075, 0.022, 0.028), # Upper riser fade + arrow shelf
        (+0.34, +0.05, 0.020, 0.026),  # Inner upper limb
        (+0.56, -0.06, 0.016, 0.022),  # Mid-upper limb
        (+0.74, -0.14, 0.013, 0.017),  # Upper recurve knee
        (+0.84, -0.09, 0.009, 0.011),  # Upper recurve tip
    ]
    sections = []
    for z, y, wx, wy in curve_pts:
        sections.append([
            (wx, y, z),
            (wx * 0.6, y + wy, z),
            (-wx * 0.6, y + wy, z),
            (-wx, y, z),
            (-wx * 0.6, y - wy, z),
            (wx * 0.6, y - wy, z),
        ])
    loft_profile(bm_bow, sections, cap_start=True, cap_end=True)
    bow_obj = create_mesh_obj("RangerBow_Limbs", bm_bow, col, m_wood, smooth=True)
    bow_obj.parent = root

    # 2. Ornate Gothic Bronze Limb Caps, Arrow Rest & Sight Guard (flush tangent angles!)
    bm_trim = bmesh.new()
    # Upper & lower Gothic recurve horn tips
    add_cylinder(bm_trim, (0, -0.14, 0.72), (0, -0.082, 0.865), 0.017, 0.009, segs=8)
    add_cylinder(bm_trim, (0, -0.14, -0.72), (0, -0.082, -0.865), 0.017, 0.009, segs=8)
    # Flush Gothic bronze riser collars
    add_cylinder(bm_trim, (0, 0.065, 0.12), (0, 0.075, 0.20), 0.028, 0.030, segs=8)
    add_cylinder(bm_trim, (0, 0.065, -0.12), (0, 0.075, -0.20), 0.028, 0.030, segs=8)
    # Arrow shelf bracket just above hand grip
    add_box(bm_trim, (-0.022, 0.065, 0.055), (0.032, 0.058, 0.016))
    # Flush mid-limb bronze cuffs aligned along limb tangent (0.34 -> 0.56: dy=-0.11, dz=+0.22 -> rot_x = +0.46 rad!)
    add_cylinder(bm_trim, (0, 0.045, 0.34), (0, 0.015, 0.42), 0.027, 0.025, segs=8)
    add_cylinder(bm_trim, (0, 0.045, -0.34), (0, 0.015, -0.42), 0.027, 0.025, segs=8)
    trim_obj = create_mesh_obj("RangerBow_GothicTrim", bm_trim, col, m_gold)
    trim_obj.parent = root

    # 3. Leather Hand Grip Wrap around (0, 0.045, 0)
    bm_grip = bmesh.new()
    add_cylinder(bm_grip, (0, 0.045, -0.11), (0, 0.045, 0.11), 0.027, 0.027, segs=10)
    grip_obj = create_mesh_obj("RangerBow_Grip", bm_grip, col, m_grip, smooth=True)
    grip_obj.parent = root

    # 4. Flush Glowing Emerald Hunter Rune Strips hugging the curved limbs!
    bm_rune = bmesh.new()
    add_cylinder(bm_rune, (0, 0.030, 0.42), (0, -0.052, 0.58), 0.0085, 0.0070, segs=6)
    add_cylinder(bm_rune, (0, 0.030, -0.42), (0, -0.052, -0.58), 0.0085, 0.0070, segs=6)
    add_box(bm_rune, (0.0, 0.102, 0.16), (0.014, 0.008, 0.048))
    add_box(bm_rune, (0.0, 0.102, -0.16), (0.014, 0.008, 0.048))
    rune_obj = create_mesh_obj("RangerBow_RuneDetail", bm_rune, col, m_rune)
    rune_obj.parent = root

    # 5. RIGGABLE STRAIGHT 2-SEGMENT BOWSTRING (parented directly to root at limb tips so bake_space_transform is 100% exact!)
    upper_tip = add_empty("UpperLimbTip", (0, -0.10, 0.84), col, parent=root)
    lower_tip = add_empty("LowerLimbTip", (0, -0.10, -0.84), col, parent=root)
    nock = add_empty("ArrowNock", (0, -0.10, 0.04), col, parent=root)

    # Straight upper string mesh pivoted at UpperLimbTip (local -Z length = 0.80 -> Unity -Y length = 0.80)
    bm_su = bmesh.new()
    add_cylinder(bm_su, (0, 0, 0), (0, 0, -0.80), 0.0042, 0.0042, segs=6)
    su_obj = create_mesh_obj("BowString_Upper", bm_su, col, m_string)
    su_obj.location = Vector((0, -0.10, 0.84))
    su_obj.parent = root

    # Straight lower string mesh pivoted at LowerLimbTip (local +Z length = 0.88 -> Unity +Y length = 0.88)
    bm_sl = bmesh.new()
    add_cylinder(bm_sl, (0, 0, 0), (0, 0, 0.88), 0.0042, 0.0042, segs=6)
    sl_obj = create_mesh_obj("BowString_Lower", bm_sl, col, m_string)
    sl_obj.location = Vector((0, -0.10, -0.84))
    sl_obj.parent = root

    # 6. Standardized Sockets
    add_empty("GripPoint", (0, 0.045, 0), col, parent=root)
    add_empty("PrimaryAttach", (0, 0.045, 0), col, parent=root)
    add_empty("ProjectileSpawn", (0, 0.36, 0.04), col, parent=root)
    add_empty("VfxTrailSocket", (0, 0.14, 0.04), col, parent=root)

    return export_weapon("Ranger_RecurveBow", "Ranger", "Bow", root, col,
                         cam_pos=(2.35, 0.20, 0.15), cam_target=(0.0, 0.0, 0.0))


def build_ranger_arrow():
    """
    Ancient Gothic Hunter Bodkin Arrow:
    Blender +Y => Unity +Z (Forward direction! Nock at y=0.0, Broadhead tip at y=+0.84m).
    """
    reset_scene()
    col = make_collection("Ranger_Arrow")

    m_steel = make_pbr_mat("M_Wpn_DarkSteel", (0.72, 0.76, 0.84), metallic=0.92, roughness=0.22, tex_prefix="T_Wpn_DarkSteel")
    m_wood = make_pbr_mat("M_Wpn_DarkWood", (0.34, 0.24, 0.15), metallic=0.04, roughness=0.62, tex_prefix="T_Wpn_LeatherWood")
    m_gold = make_pbr_mat("M_Wpn_AntiqueGold", (0.90, 0.72, 0.34), metallic=0.86, roughness=0.30, tex_prefix="T_Wpn_AntiqueGold")
    m_fletch = make_pbr_mat("M_Wpn_RuneEmerald", (0.28, 0.88, 0.48), metallic=0.10, roughness=0.38, emission=(0.18, 0.75, 0.36), emission_strength=1.4)

    root = add_empty("Ranger_Arrow", (0, 0, 0), col, radius=0.04)

    # 1. Ash Shaft
    bm_shaft = bmesh.new()
    add_cylinder(bm_shaft, (0, 0.0, 0), (0, 0.72, 0), 0.0065, 0.0060, segs=8)
    shaft_obj = create_mesh_obj("RangerArrow_Shaft", bm_shaft, col, m_wood, smooth=True)
    shaft_obj.parent = root

    # 2. Barbed Gothic Broadhead Tip
    bm_head = bmesh.new()
    head_secs = [
        [(0.022, 0.70, 0.0), (0.0, 0.70, 0.007), (-0.022, 0.70, 0.0), (0.0, 0.70, -0.007)],
        [(0.015, 0.76, 0.0), (0.0, 0.76, 0.006), (-0.015, 0.76, 0.0), (0.0, 0.76, -0.006)],
        [(0.001, 0.84, 0.0), (0.0, 0.84, 0.001), (-0.001, 0.84, 0.0), (0.0, 0.84, -0.001)],
    ]
    loft_profile(bm_head, head_secs, cap_start=True, cap_end=True)
    head_obj = create_mesh_obj("RangerArrow_Broadhead", bm_head, col, m_steel)
    head_obj.parent = root

    # 3. Bronze Collar & Nock Cap
    bm_col = bmesh.new()
    add_cylinder(bm_col, (0, 0.68, 0), (0, 0.715, 0), 0.0085, 0.0095, segs=8)
    add_cylinder(bm_col, (0, -0.015, 0), (0, 0.02, 0), 0.008, 0.0075, segs=8)
    col_obj = create_mesh_obj("RangerArrow_Collar", bm_col, col, m_gold)
    col_obj.parent = root

    # 4. 3-Vane Raven-Emerald Fletching (thin 3D prisms for clean watertight double-sided normals)
    bm_fletch = bmesh.new()
    for k in range(3):
        ang = k * (2.0 * math.pi / 3.0)
        rx = math.cos(ang)
        rz = math.sin(ang)
        add_box(bm_fletch, (rx * 0.019, 0.095, rz * 0.019), (0.026, 0.135, 0.0025), rot_euler=(0.0, -ang, 0.0))
    fletch_obj = create_mesh_obj("RangerArrow_Fletching", bm_fletch, col, m_fletch)
    fletch_obj.parent = root

    add_empty("NockPoint", (0, 0, 0), col, parent=root)
    add_empty("ArrowTip", (0, 0.84, 0), col, parent=root)

    return export_weapon("Ranger_Arrow", "Ranger", "Bow", root, col,
                         cam_pos=(0.75, 0.42, 0.35), cam_target=(0.0, 0.42, 0.0))


def main():
    print("=== GENERATING WEAPON PBR TEXTURES (512x512) ===")
    reset_scene()
    generate_pbr_textures()

    manifest = {"project": "Abyssal_Dungeon_3D", "art_direction": "Ancient Gothic Dark Fantasy", "weapons": []}
    manifest["weapons"].append(build_knight_longsword())
    manifest["weapons"].append(build_knight_shield())
    manifest["weapons"].append(build_berserker_greatsword())
    manifest["weapons"].append(build_ranger_recurve_bow())
    manifest["weapons"].append(build_ranger_arrow())

    manifest_path = os.path.join(UNITY_WEAPONS_ROOT, "weapon_asset_manifest.json")
    with open(manifest_path, "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2)
    print(f"=== BATCH 1 COMPLETE ({len(manifest['weapons'])} weapons) -> {manifest_path} ===")


if __name__ == "__main__":
    main()

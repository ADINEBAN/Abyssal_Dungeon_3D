import bpy, bmesh, math, os
from mathutils import Vector, Matrix, Euler

out_fbx_dir = r"C:\LTG\Abyssal_Dungeon_3D\Assets\Resources\EnvironmentKit\Raw"
out_blend_dir = r"C:\LTG\Abyssal_Dungeon_3D\VFX_Blender_Production\05_Environment_VFX"
os.makedirs(out_fbx_dir, exist_ok=True)
os.makedirs(out_blend_dir, exist_ok=True)

# Clean existing objects/meshes without resetting addons
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj, do_unlink=True)
for me in list(bpy.data.meshes):
    bpy.data.meshes.remove(me, do_unlink=True)

scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0

def make_mat(name, color, metallic=0.0, rough=0.75, emission=None):
    mat = bpy.data.materials.get(name)
    if mat:
        return mat
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*color, 1.0)
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = rough
        if emission and "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = (*emission, 1.0)
            bsdf.inputs["Emission Strength"].default_value = 2.0
    return mat

mat_stone = make_mat("M_GothicStone", (0.38, 0.36, 0.35), 0.05, 0.82)
mat_trim  = make_mat("M_WroughtIron", (0.20, 0.21, 0.24), 0.82, 0.45)
mat_wood  = make_mat("M_AgedOakWood", (0.26, 0.17, 0.11), 0.02, 0.80)
mat_rune  = make_mat("M_ArcaneRuneGlow", (0.28, 0.68, 1.0), 0.1, 0.25, (0.35, 0.78, 1.0))
mat_shaft = make_mat("M_LightShaft", (1.0, 0.80, 0.48), 0.0, 0.5, (1.0, 0.80, 0.48))

def box_uv_project(bm, scale=0.45):
    uv_layer = bm.loops.layers.uv.verify()
    for f in bm.faces:
        n = f.normal
        ax, ay, az = abs(n.x), abs(n.y), abs(n.z)
        for loop in f.loops:
            v = loop.vert.co
            if az >= ax and az >= ay:
                loop[uv_layer].uv = (v.x * scale, v.y * scale)
            elif ax >= ay and ax >= az:
                loop[uv_layer].uv = (v.y * scale, v.z * scale)
            else:
                loop[uv_layer].uv = (v.x * scale, v.z * scale)

def add_box(bm, center, size, rot_euler=(0,0,0), mat_idx=0):
    mat = Matrix.Translation(center) @ Euler(rot_euler, 'XYZ').to_matrix().to_4x4() @ Matrix.Diagonal((size[0], size[1], size[2], 1.0))
    ret = bmesh.ops.create_cube(bm, size=1.0, matrix=mat)
    for v in ret["verts"]:
        for f in v.link_faces:
            f.material_index = mat_idx
    return ret["verts"]

def finalize_mesh(name, bm, mats, smooth=False):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name + "_Mesh")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for m in mats:
        obj.data.materials.append(m)
    if smooth:
        for p in obj.data.polygons:
            p.use_smooth = True
    return obj

def export_single_fbx(obj, filename):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    fbx_path = os.path.join(out_fbx_dir, filename)
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        use_space_transform=True,
        bake_space_transform=True,
        axis_forward='-Z',
        axis_up='Y',
        mesh_smooth_type='FACE',
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
    )
    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    print(f"EXPORTED {filename}: {len(obj.data.vertices)} verts, {tris} tris -> {fbx_path}")

# 1. GothicCathedralVaultRib (Pointed Ogive Ribbed Vault Arch, span 6.0m, rises 0..3.15m)
bm = bmesh.new()
span_half = 3.0
apex_z = 3.15
segs = 10

def ogive_pt(side, t):
    x = side * span_half * (1.0 - t)
    z = apex_z * (math.sin(t * math.pi * 0.5) ** 0.85)
    return x, z

for side in (-1, 1):
    add_box(bm, (side * (span_half - 0.16), 0.0, 0.22), (0.46, 0.72, 0.44), mat_idx=0)
    add_box(bm, (side * (span_half - 0.12), 0.0, 0.46), (0.56, 0.82, 0.14), mat_idx=1)

    for i in range(segs):
        t0, t1 = i / segs, (i + 1) / segs
        x0, z0 = ogive_pt(side, t0)
        x1, z1 = ogive_pt(side, t1)
        mx, mz = (x0 + x1) * 0.5, (z0 + z1) * 0.5
        dx, dz = x1 - x0, z1 - z0
        seg_len = math.hypot(dx, dz) + 0.05
        ang_y = -math.atan2(dz, dx)
        add_box(bm, (mx, 0.0, mz), (seg_len, 0.46, 0.36), (0, ang_y, 0), mat_idx=0)
        add_box(bm, (mx, 0.0, mz - 0.14), (seg_len, 0.22, 0.14), (0, ang_y, 0), mat_idx=1)

        for dy_dir in (-1, 1):
            y0 = dy_dir * (1.65 * (1.0 - t0))
            y1 = dy_dir * (1.65 * (1.0 - t1))
            my = (y0 + y1) * 0.5
            dy = y1 - y0
            v = Vector((dx, dy, dz))
            dlen = v.length + 0.04
            rot_z = math.atan2(dy, dx)
            pitch = -math.atan2(dz, math.hypot(dx, dy))
            add_box(bm, (mx, my, mz + 0.06), (dlen, 0.20, 0.20), (0, pitch, rot_z), mat_idx=0)

add_box(bm, (0.0, 0.0, apex_z - 0.04), (0.52, 0.64, 0.52), mat_idx=1)
add_box(bm, (0.0, 0.0, apex_z + 0.18), (6.15, 0.34, 0.26), mat_idx=0)
add_box(bm, (0.0, 0.0, apex_z + 0.16), (0.28, 3.45, 0.22), mat_idx=0)

box_uv_project(bm, 0.42)
obj_vault = finalize_mesh("GothicCathedralVaultRib", bm, [mat_stone, mat_trim], smooth=False)
export_single_fbx(obj_vault, "GothicCathedralVaultRib.fbx")

# 2. FallenAngelStatue_Gothic (3.5m weeping winged angel on octagonal stone plinth)
bm = bmesh.new()
def add_oct_prism(bm, z0, z1, r0, r1, mat_idx=0):
    ret = bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=8, radius1=r0, radius2=r1, depth=(z1 - z0),
                                matrix=Matrix.Translation((0, 0, (z0 + z1) * 0.5)))
    for v in ret["verts"]:
        for f in v.link_faces:
            f.material_index = mat_idx

add_oct_prism(bm, 0.00, 0.18, 1.05, 0.98, 0)
add_oct_prism(bm, 0.18, 0.64, 0.86, 0.78, 0)
add_oct_prism(bm, 0.64, 0.78, 0.92, 0.88, 1)
add_oct_prism(bm, 0.78, 1.42, 0.58, 0.42, 0)
add_oct_prism(bm, 1.42, 1.98, 0.42, 0.34, 0)
for ang_deg in range(0, 360, 45):
    rad = math.radians(ang_deg)
    add_box(bm, (math.cos(rad)*0.40, math.sin(rad)*0.40, 1.32), (0.14, 0.18, 1.04), (0, 0, rad), 0)

add_oct_prism(bm, 1.98, 2.46, 0.34, 0.44, 0)
add_box(bm, (0.0, 0.0, 2.40), (0.86, 0.38, 0.24), mat_idx=0)
ret_head = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=0.22, matrix=Matrix.Translation((0, -0.06, 2.66)) @ Matrix.Diagonal((0.92, 1.08, 1.15, 1.0)))
for v in ret_head["verts"]:
    for f in v.link_faces:
        f.material_index = 0
add_box(bm, (0.0, 0.04, 2.84), (0.28, 0.36, 0.24), (0.32, 0, 0), 0)

for side in (-1, 1):
    add_box(bm, (side * 0.36, -0.14, 2.16), (0.18, 0.22, 0.46), (0.28, side * 0.18, 0), 0)
    add_box(bm, (side * 0.16, -0.34, 1.92), (0.34, 0.16, 0.16), (0.1, -side * 0.2, -side * 0.35), 0)

add_box(bm, (0.0, -0.44, 1.28), (0.14, 0.06, 1.02), mat_idx=1)
add_box(bm, (0.0, -0.44, 1.80), (0.56, 0.10, 0.10), mat_idx=1)
add_box(bm, (0.0, -0.44, 1.98), (0.08, 0.08, 0.30), mat_idx=1)
add_box(bm, (0.0, -0.44, 2.16), (0.14, 0.14, 0.14), mat_idx=1)

for side in (-1, 1):
    add_box(bm, (side * 0.48, 0.28, 2.66), (0.52, 0.16, 0.22), (0.18, -side * 0.65, side * 0.22), 0)
    add_box(bm, (side * 0.86, 0.42, 3.06), (0.62, 0.14, 0.20), (0.22, -side * 0.48, side * 0.30), 0)
    feather_count = 6
    for f_i in range(feather_count):
        ft = f_i / (feather_count - 1)
        fx = side * (0.44 + ft * 0.78)
        fy = 0.24 + ft * 0.30
        fz = 2.65 + math.sin(ft * math.pi * 0.75) * 0.48 - ft * 0.35
        flen = 1.48 - ft * 0.28
        if side == 1 and f_i == 5:
            flen *= 0.68
        add_box(bm, (fx, fy, fz - flen * 0.36), (0.19, 0.09, flen), (0.14, side * (0.18 + ft * 0.22), side * 0.25), 0)
        add_box(bm, (fx * 0.92, fy - 0.08, fz - flen * 0.16), (0.16, 0.08, flen * 0.55), (0.10, side * (0.14 + ft * 0.18), side * 0.22), 0)

box_uv_project(bm, 0.55)
obj_angel = finalize_mesh("FallenAngelStatue_Gothic", bm, [mat_stone, mat_trim], smooth=False)
export_single_fbx(obj_angel, "FallenAngelStatue_Gothic.fbx")

# 3. WoodenBarrelsCrates_Gothic (Oak barrels + iron hoops + braced crate)
bm = bmesh.new()
def add_barrel(bm, cx, cy, cz, r_mid, h, rot=(0,0,0)):
    R = Euler(rot, 'XYZ').to_matrix().to_4x4()
    T = Matrix.Translation((cx, cy, cz))
    r_end = r_mid * 0.84
    slices = [(0.0, 0.25, r_end, r_mid * 0.96), (0.25, 0.75, r_mid * 0.96, r_mid * 0.96), (0.75, 1.0, r_mid * 0.96, r_end)]
    for t0, t1, ra, rb in slices:
        z_mid = (t0 + t1) * 0.5 * h
        dz = (t1 - t0) * h
        m = T @ R @ Matrix.Translation((0, 0, z_mid))
        ret = bmesh.ops.create_cone(bm, cap_ends=(t0 == 0.0 or t1 == 1.0), cap_tris=False, segments=12, radius1=ra, radius2=rb, depth=dz, matrix=m)
        for v in ret["verts"]:
            for f in v.link_faces:
                f.material_index = 0
    for hz, hr in ((0.14, r_mid * 0.92), (0.50, r_mid * 0.99), (0.86, r_mid * 0.92)):
        m = T @ R @ Matrix.Translation((0, 0, hz * h))
        ret = bmesh.ops.create_cone(bm, cap_ends=False, cap_tris=False, segments=12, radius1=hr, radius2=hr, depth=0.065, matrix=m)
        for v in ret["verts"]:
            for f in v.link_faces:
                f.material_index = 1

add_barrel(bm, -0.44, 0.14, 0.0, 0.42, 1.02)
add_barrel(bm, -0.10, -0.48, 0.0, 0.35, 0.86)
cx, cy, cz = 0.42, 0.16, 0.38
cw, cd, ch = 0.80, 0.78, 0.76
cr_rot = (0, 0, 0.18)
add_box(bm, (cx, cy, cz), (cw - 0.06, cd - 0.06, ch - 0.06), cr_rot, 0)
Rcr = Euler(cr_rot, 'XYZ').to_matrix()
for sx in (-1, 1):
    for sy in (-1, 1):
        p = Vector((cx, cy, cz)) + Rcr @ Vector((sx * (cw * 0.46), sy * (cd * 0.46), 0))
        add_box(bm, p, (0.10, 0.10, ch), cr_rot, 0)
        p_cap = Vector((cx, cy, cz + ch * 0.46)) + Rcr @ Vector((sx * (cw * 0.47), sy * (cd * 0.47), 0))
        add_box(bm, p_cap, (0.12, 0.12, 0.08), cr_rot, 1)
for sz in (-1, 1):
    add_box(bm, (cx, cy, cz + sz * (ch * 0.45)), (cw, cd, 0.09), cr_rot, 0)

box_uv_project(bm, 0.95)
obj_props = finalize_mesh("WoodenBarrelsCrates_Gothic", bm, [mat_wood, mat_trim], smooth=False)
export_single_fbx(obj_props, "WoodenBarrelsCrates_Gothic.fbx")

# 4. WallRuneTablet_Gothic (Pointed Gothic stone tablet with recessed glowing rune sigil)
bm = bmesh.new()
add_box(bm, (0.0, 0.0, 0.96), (1.22, 0.20, 1.92), mat_idx=0)
add_box(bm, (0.0, -0.04, 0.12), (1.36, 0.28, 0.24), mat_idx=0)
add_box(bm, (-0.34, -0.02, 2.04), (0.64, 0.24, 0.18), (0, 0.72, 0), mat_idx=0)
add_box(bm, ( 0.34, -0.02, 2.04), (0.64, 0.24, 0.18), (0, -0.72, 0), mat_idx=0)
for side in (-1, 1):
    add_box(bm, (side * 0.56, -0.06, 1.02), (0.16, 0.22, 1.82), mat_idx=0)

rune_y = -0.125
for i in range(8):
    a0 = i * math.pi * 0.25
    a1 = (i + 1) * math.pi * 0.25
    r = 0.34
    mx = (math.cos(a0) + math.cos(a1)) * 0.5 * r
    mz = 1.24 + (math.sin(a0) + math.sin(a1)) * 0.5 * r
    ang_y = math.atan2(math.sin(a1) - math.sin(a0), math.cos(a1) - math.cos(a0))
    add_box(bm, (mx, rune_y, mz), (0.28, 0.04, 0.045), (0, -ang_y, 0), mat_idx=1)
add_box(bm, (0.0, rune_y, 1.12), (0.05, 0.045, 1.18), mat_idx=1)
add_box(bm, (0.0, rune_y, 1.24), (0.44, 0.045, 0.045), mat_idx=1)
add_box(bm, (0.0, rune_y, 0.72), (0.28, 0.045, 0.045), (0, 0.45, 0), mat_idx=1)
add_box(bm, (0.0, rune_y, 0.72), (0.28, 0.045, 0.045), (0, -0.45, 0), mat_idx=1)

box_uv_project(bm, 0.65)
obj_rune = finalize_mesh("WallRuneTablet_Gothic", bm, [mat_stone, mat_rune], smooth=False)
export_single_fbx(obj_rune, "WallRuneTablet_Gothic.fbx")

# 5. GothicLightShaft_Cone (Slanted volumetric cathedral god-ray frustum)
bm = bmesh.new()
uv_layer = bm.loops.layers.uv.verify()
def add_shaft_cone(bm, r_bot, r_top, height, slant_x, rings=4, segs=16):
    ring_verts = []
    for r_i in range(rings + 1):
        v_coord = r_i / rings
        z = v_coord * height
        rad = r_bot * (1.0 - v_coord) + r_top * v_coord
        ox = slant_x * v_coord
        row = []
        for s_i in range(segs + 1):
            u_coord = s_i / segs
            ang = u_coord * math.tau
            vx = ox + math.cos(ang) * rad
            vy = math.sin(ang) * rad
            row.append((bm.verts.new((vx, vy, z)), u_coord, v_coord))
        ring_verts.append(row)
    bm.verts.ensure_lookup_table()
    for r_i in range(rings):
        for s_i in range(segs):
            v00, u00, w00 = ring_verts[r_i][s_i]
            v10, u10, w10 = ring_verts[r_i][s_i + 1]
            v11, u11, w11 = ring_verts[r_i + 1][s_i + 1]
            v01, u01, w01 = ring_verts[r_i + 1][s_i]
            f = bm.faces.new((v00, v10, v11, v01))
            f.material_index = 0
            for loop, (uu, vv) in zip(f.loops, [(u00, w00), (u10, w10), (u11, w11), (u01, w01)]):
                loop[uv_layer].uv = (uu, vv)

add_shaft_cone(bm, r_bot=2.15, r_top=0.68, height=7.4, slant_x=-2.2, rings=4, segs=16)
add_shaft_cone(bm, r_bot=1.18, r_top=0.38, height=7.4, slant_x=-2.2, rings=4, segs=12)

obj_shaft = finalize_mesh("GothicLightShaft_Cone", bm, [mat_shaft], smooth=True)
export_single_fbx(obj_shaft, "GothicLightShaft_Cone.fbx")

obj_vault.location = (0, 0, 0)
obj_angel.location = (6.5, 0, 0)
obj_props.location = (10.5, 0, 0)
obj_rune.location  = (14.0, 0, 0)
obj_shaft.location = (-6.5, 0, 0)

blend_path = os.path.join(out_blend_dir, "Abyssal_Gothic_Cathedral_Kit.blend")
bpy.ops.wm.save_as_mainfile(filepath=blend_path)
print("SAVED MASTER BLEND:", blend_path)

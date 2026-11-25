"""
ABYSSAL DUNGEON 3D — BATCH 2 & BATCH 3 BLENDER WEAPON ASSET PRODUCTION
Builds the remaining 12 exclusive Ancient Gothic Dark Fantasy 3D weapons in Blender 5.3.0:

Batch 2:
  - Guardian_HeavyMace       (6-flanged Gothic war morningstar, spiked finial, iron grip)
  - Guardian_TowerShield     (Tall armored Gothic pavise bulwark, notched port, gold ribs, back grip; front=+Z->Unity +Y!)
  - Assassin_DaggerR         (Curved crimson-runed Gothic kris dagger, finger guard)
  - Assassin_DaggerL         (Asymmetric twin shadow-violet serrated parrying dagger, blade breaker)
  - Mage_ArcaneStaff         (1.82m Gothic spire-cage archmage staff, levitating sapphire arcane crystal at +Y->Unity +Z!)

Batch 3:
  - Warlock_RitualSword      (Jagged occult sacrificial sword, hooked spine, violet-soul gem guard)
  - Warlock_Grimoire         (Open floating leather spellbook, parchment pages, bronze clasps, glowing violet sigil)
  - Monk_GauntletR / L       (Articulated Gothic brass-iron combat gauntlets w/ knuckle spikes & chi-rune plates)
  - Druid_NatureStaff        (1.86m gnarled Gothic root staff, crescent moon-antler cradle & glowing emerald heartstone)
  - Cleric_HolyHammer        (Cathedral gable-arch warhammer, holy sunburst medallions, glowing gold runes)
  - Cleric_RelicShield       (Round convex sunburst reliquary shield, radiant gold rays, back grip; front=+Z->Unity +Y!)

Coordinate convention (exported with axis_forward='-Z', axis_up='Y', bake_space_transform=True):
  Blender (+X, +Y, +Z) -> Unity (-X, +Z, +Y)
  - Melee blades/maces/hammers/staffs/gauntlets: length along Blender +Y => Unity +Z (verified 100% upright/forward on RH_WP/LH_WP!)
  - Shields: height along Blender +Y => Unity +Z (UP), outer armored crest on Blender +Z => Unity +Y (FORWARD toward enemies!).
"""

import os
import sys
import math
import json
import shutil
import bpy
import bmesh
from mathutils import Vector, Matrix, Euler

ROOT = r"C:\LTG\Abyssal_Dungeon_3D"
TOOLS_DIR = os.path.join(ROOT, "Tools")
if TOOLS_DIR not in sys.path:
    sys.path.insert(0, TOOLS_DIR)

# Reuse clean Blender helpers & studio preview renderer from Batch 1 script
import BuildGothicWeapons_Batch1_Blender as b1

UNITY_WEAPONS_ROOT = os.path.join(ROOT, "Assets", "Art", "Weapons")
UNITY_BATCH2_DIR = os.path.join(UNITY_WEAPONS_ROOT, "Batch2")
UNITY_BATCH3_DIR = os.path.join(UNITY_WEAPONS_ROOT, "Batch3")
VFX_WEAPONS_ROOT = os.path.join(ROOT, "VFX_Blender_Production", "02_Weapons")
CAPTURE_PREVIEWS_DIR = os.path.join(ROOT, "Captures", "WeaponUiRedesign", "Previews")

for d in [UNITY_BATCH2_DIR, UNITY_BATCH3_DIR, CAPTURE_PREVIEWS_DIR]:
    os.makedirs(d, exist_ok=True)


def export_batch_weapon(weapon_name, class_name, group_name, batch_num, root_obj, col, cam_pos, cam_target):
    base_dir = os.path.join(VFX_WEAPONS_ROOT, class_name, group_name)
    blend_dir = os.path.join(base_dir, "Blender")
    fbx_dir = os.path.join(base_dir, "FBX")
    prev_dir = os.path.join(base_dir, "Previews")
    for d in [blend_dir, fbx_dir, prev_dir]:
        os.makedirs(d, exist_ok=True)

    prev_path = os.path.join(prev_dir, f"{weapon_name}_Preview.png")
    b1.render_preview([root_obj], prev_path, cam_pos, cam_target)
    if os.path.exists(prev_path):
        shutil.copy2(prev_path, os.path.join(CAPTURE_PREVIEWS_DIR, f"{weapon_name}_Preview.png"))

    blend_path = os.path.join(blend_dir, f"{weapon_name}.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)

    bpy.ops.object.select_all(action='DESELECT')
    tris = 0
    for obj in col.objects:
        obj.select_set(True)
        if obj.type == 'MESH':
            obj.data.calc_loop_triangles()
            tris += len(obj.data.loop_triangles)

    unity_dir = UNITY_BATCH2_DIR if batch_num == 2 else UNITY_BATCH3_DIR
    fbx_unity_path = os.path.join(unity_dir, f"{weapon_name}.fbx")
    fbx_prod_path = os.path.join(fbx_dir, f"{weapon_name}.fbx")

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

    sockets = [o.name for o in col.objects if o.type == 'EMPTY']
    meshes = [o.name for o in col.objects if o.type == 'MESH']
    print(f"[EXPORTED B{batch_num}] {weapon_name} | tris={tris} | meshes={meshes} | sockets={sockets}")
    return {
        "name": weapon_name,
        "class": class_name,
        "group": group_name,
        "batch": batch_num,
        "triangles": tris,
        "meshes": meshes,
        "sockets": sockets,
        "blend": os.path.relpath(blend_path, ROOT).replace("\\", "/"),
        "fbx": os.path.relpath(fbx_unity_path, ROOT).replace("\\", "/"),
        "preview": os.path.relpath(prev_path, ROOT).replace("\\", "/")
    }


# =============================================================================
# BATCH 2: GUARDIAN, ASSASSIN, MAGE
# =============================================================================

def build_guardian_heavy_mace():
    b1.reset_scene()
    col = b1.make_collection("Guardian_HeavyMace")
    m_steel = b1.make_pbr_mat("M_Wpn_DarkSteel", (0.56, 0.60, 0.68), 0.90, 0.30, tex_prefix="T_Wpn_DarkSteel")
    m_gold = b1.make_pbr_mat("M_Wpn_AntiqueGold", (0.94, 0.76, 0.34), 0.88, 0.28, tex_prefix="T_Wpn_AntiqueGold")
    m_grip = b1.make_pbr_mat("M_Wpn_LeatherGrip", (0.26, 0.16, 0.12), 0.05, 0.72, tex_prefix="T_Wpn_LeatherWood")
    m_rune = b1.make_pbr_mat("M_Wpn_RuneGold", (1.0, 0.86, 0.42), 0.25, 0.20, emission=(1.0, 0.76, 0.26), emission_strength=3.2)

    root = b1.add_empty("Guardian_HeavyMace", (0, 0, 0), col, radius=0.08)

    # 1. Heavy Steel Shaft + 6 Gothic Cathedral Flanges + Top Crown Spike (y = -0.26 .. +0.88)
    bm_head = bmesh.new()
    b1.add_cylinder(bm_head, (0, 0.12, 0), (0, 0.62, 0), 0.022, 0.030, segs=10)
    b1.add_cylinder(bm_head, (0, 0.56, 0), (0, 0.78, 0), 0.048, 0.054, segs=12)  # Core mace drum
    b1.add_cylinder(bm_head, (0, 0.78, 0), (0, 0.90, 0), 0.036, 0.003, segs=8)   # Top Gothic spire spike
    # 6 sharp radial Gothic flanges with notched teeth
    for k in range(6):
        ang = k * (math.pi / 3.0)
        rx, rz = math.cos(ang), math.sin(ang)
        b1.add_box(bm_head, (rx * 0.072, 0.66, rz * 0.072), (0.088, 0.21, 0.016), rot_euler=(0.0, -ang, 0.0))
        b1.add_box(bm_head, (rx * 0.105, 0.68, rz * 0.105), (0.042, 0.075, 0.012), rot_euler=(0.0, -ang, 0.0))
    head_obj = b1.create_mesh_obj("GuardianMace_Head", bm_head, col, m_steel)
    head_obj.parent = root

    # 2. Gold Crown Collars, Hand Guard Ring & Heavy Pommel
    bm_gold = bmesh.new()
    b1.add_cylinder(bm_gold, (0, 0.52, 0), (0, 0.57, 0), 0.042, 0.054, segs=10)
    b1.add_cylinder(bm_gold, (0, 0.76, 0), (0, 0.80, 0), 0.056, 0.038, segs=10)
    b1.add_cylinder(bm_gold, (0, 0.10, 0), (0, 0.15, 0), 0.038, 0.028, segs=10)
    b1.add_cylinder(bm_gold, (0, -0.18, 0), (0, -0.24, 0), 0.026, 0.042, segs=8)
    b1.add_cylinder(bm_gold, (0, -0.24, 0), (0, -0.29, 0), 0.042, 0.008, segs=8)
    gold_obj = b1.create_mesh_obj("GuardianMace_GoldTrim", bm_gold, col, m_gold)
    gold_obj.parent = root

    # 3. Leather Grip
    bm_grip = bmesh.new()
    b1.add_cylinder(bm_grip, (0, -0.18, 0), (0, 0.10, 0), 0.024, 0.024, segs=10)
    grip_obj = b1.create_mesh_obj("GuardianMace_Grip", bm_grip, col, m_grip, smooth=True)
    grip_obj.parent = root

    # 4. Glowing Bulwark Rune Band on Mace Drum
    bm_rune = bmesh.new()
    b1.add_cylinder(bm_rune, (0, 0.64, 0), (0, 0.69, 0), 0.057, 0.057, segs=12)
    rune_obj = b1.create_mesh_obj("GuardianMace_RuneDetail", bm_rune, col, m_rune)
    rune_obj.parent = root

    b1.add_empty("GripPoint", (0, 0, 0), col, parent=root)
    b1.add_empty("PrimaryAttach", (0, 0, 0), col, parent=root)
    b1.add_empty("ImpactPoint", (0, 0.68, 0), col, parent=root)
    b1.add_empty("VfxTrailSocket", (0, 0.82, 0), col, parent=root)

    return export_batch_weapon("Guardian_HeavyMace", "Guardian", "Mace_Shield", 2, root, col,
                               cam_pos=(1.35, 0.32, 0.55), cam_target=(0.0, 0.30, 0.0))


def build_guardian_tower_shield():
    """
    Tall Ancient Gothic Pavise Tower Shield (1.32m tall x 0.72m wide).
    Blender +Y => Unity +Z (Height UP), Blender +Z => Unity +Y (Outer Armored Face FORWARD toward enemies!).
    """
    b1.reset_scene()
    col = b1.make_collection("Guardian_TowerShield")
    m_steel = b1.make_pbr_mat("M_Wpn_DarkSteel", (0.44, 0.48, 0.56), 0.90, 0.32, tex_prefix="T_Wpn_DarkSteel")
    m_gold = b1.make_pbr_mat("M_Wpn_AntiqueGold", (0.95, 0.76, 0.34), 0.88, 0.28, tex_prefix="T_Wpn_AntiqueGold")
    m_grip = b1.make_pbr_mat("M_Wpn_LeatherGrip", (0.24, 0.15, 0.11), 0.05, 0.74, tex_prefix="T_Wpn_LeatherWood")
    m_rune = b1.make_pbr_mat("M_Wpn_RuneGold", (1.0, 0.88, 0.45), 0.25, 0.20, emission=(1.0, 0.78, 0.28), emission_strength=3.2)

    root = b1.add_empty("Guardian_TowerShield", (0, 0, 0), col, radius=0.09)

    outline = [
        (0.00,  0.66),
        (0.24,  0.62),
        (0.36,  0.52),
        (0.37,  0.10),
        (0.35, -0.34),
        (0.26, -0.56),
        (0.00, -0.66),
        (-0.26, -0.56),
        (-0.35, -0.34),
        (-0.37,  0.10),
        (-0.36,  0.52),
        (-0.24,  0.62),
    ]

    def z_curve(x, y, z0):
        return z0 + 0.085 * (1.0 - (x / 0.40) ** 2) - 0.015 * (y / 0.66) ** 2

    # 1. Curved Fortress Steel Plate Body
    bm_body = bmesh.new()
    fv = [bm_body.verts.new((x, y, z_curve(x, y, 0.055))) for x, y in outline]
    bv = [bm_body.verts.new((x * 0.96, y * 0.96, z_curve(x, y, 0.012))) for x, y in outline]
    fc = bm_body.verts.new((0.0, 0.02, 0.165))
    bc = bm_body.verts.new((0.0, 0.02, 0.095))
    n = len(outline)
    for i in range(n):
        j = (i + 1) % n
        bm_body.faces.new((fv[i], fv[j], fc))
        bm_body.faces.new((bv[j], bv[i], bc))
        bm_body.faces.new((fv[j], fv[i], bv[i], bv[j]))
    body_obj = b1.create_mesh_obj("GuardianShield_Body", bm_body, col, m_steel)
    body_obj.parent = root

    # 2. Heavy Gold Cathedral Border, Central Pillar Spine & Winged Bastion Crest
    bm_trim = bmesh.new()
    for i in range(n):
        j = (i + 1) % n
        p0 = (outline[i][0], outline[i][1], z_curve(outline[i][0], outline[i][1], 0.052))
        p1 = (outline[j][0], outline[j][1], z_curve(outline[j][0], outline[j][1], 0.052))
        b1.add_cylinder(bm_trim, p0, p1, 0.030, 0.030, segs=6)
    # Heavy vertical fortress ridge & twin horizontal braces
    b1.add_box(bm_trim, (0.0, 0.0, 0.150), (0.082, 1.18, 0.034))
    b1.add_box(bm_trim, (0.0, 0.24, 0.140), (0.64, 0.068, 0.028))
    b1.add_box(bm_trim, (0.0, -0.24, 0.140), (0.62, 0.068, 0.028))
    # Central armored Bastion Crown Boss
    b1.add_cylinder(bm_trim, (0.0, 0.04, 0.155), (0.0, 0.04, 0.220), 0.115, 0.032, segs=8)
    trim_obj = b1.create_mesh_obj("GuardianShield_GoldBastion", bm_trim, col, m_gold)
    trim_obj.parent = root

    # 3. Glowing Bastion Rune Inlays
    bm_rune = bmesh.new()
    b1.add_box(bm_rune, (0.0, 0.0, 0.168), (0.024, 1.02, 0.008))
    b1.add_box(bm_rune, (0.0, 0.24, 0.156), (0.50, 0.020, 0.008))
    b1.add_box(bm_rune, (0.0, -0.24, 0.156), (0.48, 0.020, 0.008))
    rune_obj = b1.create_mesh_obj("GuardianShield_RuneDetail", bm_rune, col, m_rune)
    rune_obj.parent = root

    # 4. Back Handle & Double Forearm Harness at GripPoint (0,0,0)
    bm_grip = bmesh.new()
    b1.add_cylinder(bm_grip, (-0.08, 0.0, 0.0), (0.08, 0.0, 0.0), 0.020, 0.020, segs=8)
    b1.add_box(bm_grip, (-0.08, 0.0, 0.030), (0.026, 0.034, 0.060))
    b1.add_box(bm_grip, (0.08, 0.0, 0.030), (0.026, 0.034, 0.060))
    b1.add_box(bm_grip, (0.0, -0.22, -0.015), (0.16, 0.055, 0.024))
    grip_obj = b1.create_mesh_obj("GuardianShield_BackGrip", bm_grip, col, m_grip)
    grip_obj.parent = root

    b1.add_empty("GripPoint", (0, 0, 0), col, parent=root)
    b1.add_empty("PrimaryAttach", (0, 0, 0), col, parent=root)
    b1.add_empty("ForearmAttach", (0, -0.22, -0.02), col, parent=root)
    b1.add_empty("ShieldFrontCenter", (0, 0.04, 0.22), col, parent=root)

    return export_batch_weapon("Guardian_TowerShield", "Guardian", "Mace_Shield", 2, root, col,
                               cam_pos=(1.15, 0.30, 1.50), cam_target=(0.0, 0.0, 0.08))


def build_assassin_dagger(is_right=True):
    name = "Assassin_DaggerR" if is_right else "Assassin_DaggerL"
    b1.reset_scene()
    col = b1.make_collection(name)
    m_steel = b1.make_pbr_mat("M_Wpn_DarkSteel", (0.34, 0.36, 0.44), 0.92, 0.24, tex_prefix="T_Wpn_DarkSteel")
    m_gold = b1.make_pbr_mat("M_Wpn_AntiqueGold", (0.82, 0.64, 0.30), 0.86, 0.32, tex_prefix="T_Wpn_AntiqueGold")
    m_grip = b1.make_pbr_mat("M_Wpn_LeatherGrip", (0.16, 0.12, 0.14), 0.05, 0.72, tex_prefix="T_Wpn_LeatherWood")
    if is_right:
        m_rune = b1.make_pbr_mat("M_Wpn_RuneCrimson", (1.0, 0.20, 0.32), 0.20, 0.20, emission=(1.0, 0.16, 0.28), emission_strength=3.6)
    else:
        m_rune = b1.make_pbr_mat("M_Wpn_RuneViolet", (0.76, 0.30, 1.0), 0.20, 0.20, emission=(0.68, 0.18, 1.0), emission_strength=3.6)

    root = b1.add_empty(name, (0, 0, 0), col, radius=0.06)

    # 1. Wicked Curved Gothic Kris / Parrying Blade (y = 0.08 .. 0.58)
    bm_blade = bmesh.new()
    sign = 1.0 if is_right else -1.0
    slices = [
        (0.08, 0.000, 0.032, 0.009),
        (0.18, 0.014 * sign, 0.034, 0.009),
        (0.32, 0.004 * sign, 0.028, 0.008),
        (0.45, -0.012 * sign, 0.020, 0.006),
        (0.54, -0.004 * sign, 0.009, 0.003),
        (0.60, 0.002 * sign, 0.0008, 0.0008),
    ]
    secs = []
    for y, cx, wx, wz in slices:
        secs.append([
            (cx + wx, y, 0.0),
            (cx, y, wz),
            (cx - wx * 0.75, y, 0.0),
            (cx, y, -wz),
        ])
    b1.loft_profile(bm_blade, secs, cap_start=True, cap_end=True)
    # Asymmetric serrated back tooth / blade-breaker notch on left parrying dagger
    if not is_right:
        b1.add_box(bm_blade, (-0.036, 0.22, 0.0), (0.032, 0.045, 0.008), rot_euler=(0, 0, -0.55))
    blade_obj = b1.create_mesh_obj(name + "_Blade", bm_blade, col, m_steel)
    blade_obj.parent = root

    # 2. Swept S-Guard & Talon Ring Pommel
    bm_guard = bmesh.new()
    b1.add_box(bm_guard, (0.045 * sign, 0.095, 0.0), (0.095, 0.022, 0.024), rot_euler=(0, 0, 0.35 * sign))
    b1.add_box(bm_guard, (-0.040 * sign, 0.075, 0.0), (0.085, 0.020, 0.022), rot_euler=(0, 0, -0.30 * sign))
    # Ring pommel for quick spinning grip
    for k in range(8):
        a0 = k * math.pi / 4.0
        a1 = (k + 1) * math.pi / 4.0
        p0 = (math.cos(a0) * 0.026, -0.155 + math.sin(a0) * 0.026, 0.0)
        p1 = (math.cos(a1) * 0.026, -0.155 + math.sin(a1) * 0.026, 0.0)
        b1.add_cylinder(bm_guard, p0, p1, 0.007, 0.007, segs=6)
    guard_obj = b1.create_mesh_obj(name + "_GuardPommel", bm_guard, col, m_gold)
    guard_obj.parent = root

    # 3. Wrapped Grip
    bm_grip = bmesh.new()
    b1.add_cylinder(bm_grip, (0, -0.125, 0), (0, 0.075, 0), 0.016, 0.018, segs=8)
    grip_obj = b1.create_mesh_obj(name + "_Grip", bm_grip, col, m_grip, smooth=True)
    grip_obj.parent = root

    # 4. Glowing Crimson / Shadow-Violet Vein Strip
    bm_rune = bmesh.new()
    b1.add_box(bm_rune, (0.004 * sign, 0.30, 0.0085), (0.008, 0.34, 0.003))
    b1.add_box(bm_rune, (0.004 * sign, 0.30, -0.0085), (0.008, 0.34, 0.003))
    rune_obj = b1.create_mesh_obj(name + "_RuneDetail", bm_rune, col, m_rune)
    rune_obj.parent = root

    b1.add_empty("GripPoint", (0, 0, 0), col, parent=root)
    b1.add_empty("PrimaryAttach", (0, 0, 0), col, parent=root)
    b1.add_empty("BladeTip", (0, 0.60, 0), col, parent=root)
    b1.add_empty("VfxTrailSocket", (0, 0.56, 0), col, parent=root)

    return export_batch_weapon(name, "Assassin", "Dual_Blades", 2, root, col,
                               cam_pos=(0.95, 0.24, 0.42), cam_target=(0.0, 0.24, 0.0))


def build_mage_arcane_staff():
    """
    Ancient Gothic Archmage Arcane Staff (1.84m total):
    GripPoint at (0, 0, 0). Lower iron ferule ends at y=-0.82m (clears floor cleanly!).
    Sculpted Gothic spire-cage cradle + levitating faceted sapphire arcane crystal at y=+0.96m (HIGH ABOVE HEAD!).
    """
    b1.reset_scene()
    col = b1.make_collection("Mage_ArcaneStaff")
    m_wood = b1.make_pbr_mat("M_Wpn_DarkWood", (0.24, 0.18, 0.16), 0.10, 0.56, tex_prefix="T_Wpn_LeatherWood")
    m_gold = b1.make_pbr_mat("M_Wpn_AntiqueGold", (0.94, 0.76, 0.36), 0.88, 0.28, tex_prefix="T_Wpn_AntiqueGold")
    m_steel = b1.make_pbr_mat("M_Wpn_DarkSteel", (0.50, 0.56, 0.68), 0.90, 0.26, tex_prefix="T_Wpn_DarkSteel")
    m_arcane = b1.make_pbr_mat("M_Wpn_RuneArcane", (0.38, 0.80, 1.0), 0.22, 0.14, emission=(0.24, 0.72, 1.0), emission_strength=4.0)

    root = b1.add_empty("Mage_ArcaneStaff", (0, 0, 0), col, radius=0.08)

    # 1. Fluted Ebony Staff Shaft (y = -0.78 .. +0.76)
    bm_shaft = bmesh.new()
    b1.add_cylinder(bm_shaft, (0, -0.78, 0), (0, 0.0, 0), 0.017, 0.021, segs=10)
    b1.add_cylinder(bm_shaft, (0, 0.0, 0), (0, 0.76, 0), 0.021, 0.026, segs=10)
    shaft_obj = b1.create_mesh_obj("MageStaff_Shaft", bm_shaft, col, m_wood, smooth=True)
    shaft_obj.parent = root

    # 2. Ornate Cathedral Lantern Spire-Cage Finial + Ferule (Antique Gold)
    bm_gold = bmesh.new()
    # Bottom spiked steel/gold ferule
    b1.add_cylinder(bm_gold, (0, -0.76, 0), (0, -0.84, 0), 0.022, 0.004, segs=8)
    # Grip boundary rings
    b1.add_cylinder(bm_gold, (0, -0.14, 0), (0, -0.10, 0), 0.027, 0.027, segs=10)
    b1.add_cylinder(bm_gold, (0, 0.10, 0), (0, 0.14, 0), 0.028, 0.028, segs=10)
    # Upper ornate cathedral chalice collar
    b1.add_cylinder(bm_gold, (0, 0.70, 0), (0, 0.80, 0), 0.026, 0.068, segs=10)
    # 4 Gothic pointed arch prongs curving around the levitating arcane crystal (y = 0.78 .. 1.14)
    for k in range(4):
        ang = k * (math.pi * 0.5)
        rx, rz = math.cos(ang), math.sin(ang)
        b1.add_cylinder(bm_gold, (rx * 0.055, 0.78, rz * 0.055), (rx * 0.125, 0.94, rz * 0.125), 0.014, 0.012, segs=6)
        b1.add_cylinder(bm_gold, (rx * 0.125, 0.94, rz * 0.125), (rx * 0.032, 1.14, rz * 0.032), 0.012, 0.006, segs=6)
    # Astrolabe halo ring around crystal
    for k in range(12):
        a0 = k * (2 * math.pi / 12)
        a1 = (k + 1) * (2 * math.pi / 12)
        b1.add_cylinder(bm_gold, (math.cos(a0) * 0.135, 0.96, math.sin(a0) * 0.135),
                        (math.cos(a1) * 0.135, 0.96, math.sin(a1) * 0.135), 0.009, 0.009, segs=6)
    gold_obj = b1.create_mesh_obj("MageStaff_GothicCrown", bm_gold, col, m_gold)
    gold_obj.parent = root

    # 3. Levitating Faceted Arcane Sapphire Crystal (Separate Emissive Detail Mesh!)
    bm_crys = bmesh.new()
    b1.add_cylinder(bm_crys, (0, 0.85, 0), (0, 0.96, 0), 0.004, 0.068, segs=6)
    b1.add_cylinder(bm_crys, (0, 0.96, 0), (0, 1.08, 0), 0.068, 0.004, segs=6)
    crys_obj = b1.create_mesh_obj("MageStaff_ArcaneCrystal", bm_crys, col, m_arcane)
    crys_obj.parent = root

    b1.add_empty("GripPoint", (0, 0, 0), col, parent=root)
    b1.add_empty("PrimaryAttach", (0, 0, 0), col, parent=root)
    b1.add_empty("MagicEmitSocket", (0, 0.98, 0), col, parent=root)
    b1.add_empty("ProjectileSpawn", (0, 1.06, 0), col, parent=root)
    b1.add_empty("VfxTrailSocket", (0, 1.04, 0), col, parent=root)

    return export_batch_weapon("Mage_ArcaneStaff", "Mage", "Staff", 2, root, col,
                               cam_pos=(2.15, 0.18, 0.65), cam_target=(0.0, 0.16, 0.0))


# =============================================================================
# BATCH 3: WARLOCK, MONK, DRUID, CLERIC
# =============================================================================

def build_warlock_ritual_sword():
    b1.reset_scene()
    col = b1.make_collection("Warlock_RitualSword")
    m_steel = b1.make_pbr_mat("M_Wpn_DarkSteel", (0.30, 0.26, 0.38), 0.90, 0.28, tex_prefix="T_Wpn_DarkSteel")
    m_gold = b1.make_pbr_mat("M_Wpn_AntiqueGold", (0.76, 0.58, 0.32), 0.86, 0.34, tex_prefix="T_Wpn_AntiqueGold")
    m_grip = b1.make_pbr_mat("M_Wpn_LeatherGrip", (0.18, 0.12, 0.18), 0.05, 0.72, tex_prefix="T_Wpn_LeatherWood")
    m_soul = b1.make_pbr_mat("M_Wpn_RuneViolet", (0.76, 0.34, 1.0), 0.20, 0.18, emission=(0.68, 0.20, 1.0), emission_strength=3.8)

    root = b1.add_empty("Warlock_RitualSword", (0, 0, 0), col, radius=0.07)

    # 1. Jagged Occult Sacrificial Flame-Wave Blade (y = 0.10 .. 0.78)
    bm_blade = bmesh.new()
    slices = [
        (0.10, 0.000, 0.036, 0.011),
        (0.24, 0.014, 0.038, 0.011),
        (0.38, -0.012, 0.034, 0.010),
        (0.52, 0.012, 0.028, 0.009),
        (0.66, -0.006, 0.018, 0.006),
        (0.78, 0.000, 0.001, 0.001),
    ]
    secs = [[(cx + wx, y, 0), (cx, y, wz), (cx - wx, y, 0), (cx, y, -wz)] for y, cx, wx, wz in slices]
    b1.loft_profile(bm_blade, secs, cap_start=True, cap_end=True)
    blade_obj = b1.create_mesh_obj("WarlockSword_Blade", bm_blade, col, m_steel)
    blade_obj.parent = root

    # 2. Crescent Occult Horn Crossguard & Eye-Gem Collar
    bm_guard = bmesh.new()
    b1.add_cylinder(bm_guard, (-0.14, 0.16, 0), (0, 0.10, 0), 0.007, 0.024, segs=8)
    b1.add_cylinder(bm_guard, (0.14, 0.16, 0), (0, 0.10, 0), 0.007, 0.024, segs=8)
    b1.add_cylinder(bm_guard, (0, -0.15, 0), (0, -0.21, 0), 0.020, 0.032, segs=8)
    b1.add_cylinder(bm_guard, (0, -0.21, 0), (0, -0.25, 0), 0.032, 0.006, segs=8)
    guard_obj = b1.create_mesh_obj("WarlockSword_GuardPommel", bm_guard, col, m_gold)
    guard_obj.parent = root

    bm_grip = bmesh.new()
    b1.add_cylinder(bm_grip, (0, -0.15, 0), (0, 0.09, 0), 0.018, 0.019, segs=8)
    grip_obj = b1.create_mesh_obj("WarlockSword_Grip", bm_grip, col, m_grip, smooth=True)
    grip_obj.parent = root

    bm_rune = bmesh.new()
    b1.add_cylinder(bm_rune, (0, 0.075, -0.022), (0, 0.075, 0.022), 0.020, 0.020, segs=8)
    b1.add_box(bm_rune, (0.0, 0.42, 0.010), (0.010, 0.44, 0.003))
    b1.add_box(bm_rune, (0.0, 0.42, -0.010), (0.010, 0.44, 0.003))
    rune_obj = b1.create_mesh_obj("WarlockSword_RuneDetail", bm_rune, col, m_soul)
    rune_obj.parent = root

    b1.add_empty("GripPoint", (0, 0, 0), col, parent=root)
    b1.add_empty("PrimaryAttach", (0, 0, 0), col, parent=root)
    b1.add_empty("BladeTip", (0, 0.78, 0), col, parent=root)
    b1.add_empty("MagicEmitSocket", (0, 0.74, 0), col, parent=root)
    b1.add_empty("VfxTrailSocket", (0, 0.74, 0), col, parent=root)

    return export_batch_weapon("Warlock_RitualSword", "Warlock", "Book_Sword", 3, root, col,
                               cam_pos=(1.25, 0.30, 0.50), cam_target=(0.0, 0.28, 0.0))


def build_warlock_grimoire():
    """Open floating Gothic Spellbook Grimoire with bronze clasps & hovering violet arcane seal."""
    b1.reset_scene()
    col = b1.make_collection("Warlock_Grimoire")
    m_cover = b1.make_pbr_mat("M_Wpn_LeatherGrip", (0.20, 0.10, 0.22), 0.12, 0.64, tex_prefix="T_Wpn_LeatherWood")
    m_pages = b1.make_pbr_mat("M_Wpn_Parchment", (0.84, 0.76, 0.58), 0.02, 0.80, tex_prefix="T_Wpn_LeatherWood")
    m_gold = b1.make_pbr_mat("M_Wpn_AntiqueGold", (0.90, 0.70, 0.32), 0.88, 0.30, tex_prefix="T_Wpn_AntiqueGold")
    m_soul = b1.make_pbr_mat("M_Wpn_RuneViolet", (0.78, 0.36, 1.0), 0.20, 0.18, emission=(0.72, 0.22, 1.0), emission_strength=4.0)

    root = b1.add_empty("Warlock_Grimoire", (0, 0, 0), col, radius=0.07)

    # Open V-angled Gothic leather cover boards
    bm_cov = bmesh.new()
    b1.add_box(bm_cov, (-0.14, 0.0, -0.02), (0.28, 0.36, 0.022), rot_euler=(0, 0.34, 0))
    b1.add_box(bm_cov, (0.14, 0.0, -0.02), (0.28, 0.36, 0.022), rot_euler=(0, -0.34, 0))
    b1.add_cylinder(bm_cov, (0, -0.18, -0.068), (0, 0.18, -0.068), 0.028, 0.028, segs=8)  # Spine
    cov_obj = b1.create_mesh_obj("WarlockGrimoire_Cover", bm_cov, col, m_cover)
    cov_obj.parent = root

    # Thick aged parchment page blocks (open pages curving upward)
    bm_pg = bmesh.new()
    b1.add_box(bm_pg, (-0.13, 0.0, 0.015), (0.25, 0.33, 0.036), rot_euler=(0, 0.28, 0))
    b1.add_box(bm_pg, (0.13, 0.0, 0.015), (0.25, 0.33, 0.036), rot_euler=(0, -0.28, 0))
    pg_obj = b1.create_mesh_obj("WarlockGrimoire_Pages", bm_pg, col, m_pages)
    pg_obj.parent = root

    # Heavy Antique Bronze Corner Clasps & Spine Ribs
    bm_gold = bmesh.new()
    for sx in (-0.26, 0.26):
        for sy in (-0.16, 0.16):
            b1.add_box(bm_gold, (sx, sy, 0.022), (0.048, 0.048, 0.030))
    for ry in (-0.12, 0.0, 0.12):
        b1.add_cylinder(bm_gold, (0, ry - 0.015, -0.072), (0, ry + 0.015, -0.072), 0.034, 0.034, segs=8)
    gold_obj = b1.create_mesh_obj("WarlockGrimoire_Clasps", bm_gold, col, m_gold)
    gold_obj.parent = root

    # Glowing Violet Soul-Sigil Floating Just Above Open Pages
    bm_rune = bmesh.new()
    for k in range(8):
        a0 = k * math.pi / 4.0
        a1 = (k + 1) * math.pi / 4.0
        b1.add_cylinder(bm_rune, (math.cos(a0) * 0.12, math.sin(a0) * 0.12, 0.12),
                        (math.cos(a1) * 0.12, math.sin(a1) * 0.12, 0.12), 0.008, 0.008, segs=6)
    b1.add_box(bm_rune, (0.0, 0.0, 0.12), (0.075, 0.075, 0.012), rot_euler=(0, 0, math.pi * 0.25))
    rune_obj = b1.create_mesh_obj("WarlockGrimoire_RuneSigil", bm_rune, col, m_soul)
    rune_obj.parent = root

    b1.add_empty("GripPoint", (0, 0, 0), col, parent=root)
    b1.add_empty("PrimaryAttach", (0, 0, 0), col, parent=root)
    b1.add_empty("MagicEmitSocket", (0, 0, 0.16), col, parent=root)

    return export_batch_weapon("Warlock_Grimoire", "Warlock", "Book_Sword", 3, root, col,
                               cam_pos=(0.62, -0.68, 0.62), cam_target=(0.0, 0.0, 0.04))


def build_monk_gauntlet(is_right=True):
    """
    Ancient Gothic Combat Cestus Gauntlet (Left & Right):
    Covers fist cleanly: knuckles strike forward along Blender +Y => Unity +Z!
    Wrist/forearm cuff extends back along Blender -Y => Unity -Z.
    Thumb armor plate on Blender +X (matches measured Monk RH/LH +X thumb-up!).
    """
    name = "Monk_GauntletR" if is_right else "Monk_GauntletL"
    b1.reset_scene()
    col = b1.make_collection(name)
    m_steel = b1.make_pbr_mat("M_Wpn_DarkSteel", (0.48, 0.50, 0.56), 0.88, 0.32, tex_prefix="T_Wpn_DarkSteel")
    m_gold = b1.make_pbr_mat("M_Wpn_AntiqueGold", (0.94, 0.72, 0.30), 0.88, 0.28, tex_prefix="T_Wpn_AntiqueGold")
    m_grip = b1.make_pbr_mat("M_Wpn_LeatherGrip", (0.28, 0.18, 0.12), 0.05, 0.74, tex_prefix="T_Wpn_LeatherWood")
    m_chi = b1.make_pbr_mat("M_Wpn_RuneGold", (1.0, 0.84, 0.34), 0.22, 0.20, emission=(1.0, 0.74, 0.20), emission_strength=3.4)

    root = b1.add_empty(name, (0, 0, 0), col, radius=0.06)

    # 1. Armored Fist Shell & Forearm Vambrace Bracer (y = -0.22 .. +0.14)
    bm_body = bmesh.new()
    b1.add_cylinder(bm_body, (0, -0.22, 0), (0, -0.05, 0), 0.062, 0.068, segs=10)  # Forearm bracer cuff
    b1.add_box(bm_body, (0.0, 0.04, 0.0), (0.145, 0.155, 0.125))                  # Armored fist cage
    # Outer forearm ridge plate
    side_z = 0.066 if is_right else -0.066
    b1.add_box(bm_body, (0.0, -0.10, side_z), (0.105, 0.20, 0.024))
    body_obj = b1.create_mesh_obj(name + "_Armor", bm_body, col, m_steel)
    body_obj.parent = root

    # 2. Brass-Gold Knuckle Duster Bar, 4 Strike Spikes & Wrist Collars
    bm_gold = bmesh.new()
    b1.add_box(bm_gold, (0.0, 0.12, 0.0), (0.158, 0.042, 0.132))
    for kx in (-0.052, -0.017, 0.017, 0.052):
        b1.add_cylinder(bm_gold, (kx, 0.135, 0.0), (kx, 0.185, 0.0), 0.018, 0.004, segs=6)  # 4 knuckle impact spikes!
    b1.add_cylinder(bm_gold, (0, -0.06, 0), (0, -0.03, 0), 0.074, 0.074, segs=10)
    b1.add_cylinder(bm_gold, (0, -0.22, 0), (0, -0.19, 0), 0.068, 0.068, segs=10)
    # Thumb guard plate along +X
    b1.add_box(bm_gold, (0.082, 0.03, 0.02 * (1 if is_right else -1)), (0.036, 0.095, 0.055))
    gold_obj = b1.create_mesh_obj(name + "_BrassKnuckles", bm_gold, col, m_gold)
    gold_obj.parent = root

    # 3. Inner Leather Wrist Straps
    bm_grip = bmesh.new()
    b1.add_cylinder(bm_grip, (0, -0.16, 0), (0, -0.12, 0), 0.068, 0.068, segs=10)
    grip_obj = b1.create_mesh_obj(name + "_Straps", bm_grip, col, m_grip)
    grip_obj.parent = root

    # 4. Glowing Golden Chi-Rune Crest on Back of Hand
    bm_rune = bmesh.new()
    b1.add_box(bm_rune, (0.0, 0.04, side_z * 1.02), (0.048, 0.088, 0.010))
    rune_obj = b1.create_mesh_obj(name + "_RuneDetail", bm_rune, col, m_chi)
    rune_obj.parent = root

    b1.add_empty("GripPoint", (0, 0, 0), col, parent=root)
    b1.add_empty("PrimaryAttach", (0, 0, 0), col, parent=root)
    b1.add_empty("ImpactPoint", (0, 0.185, 0), col, parent=root)
    b1.add_empty("VfxTrailSocket", (0, 0.185, 0), col, parent=root)

    return export_batch_weapon(name, "Monk", "Gloves", 3, root, col,
                               cam_pos=(0.56, 0.32, 0.36), cam_target=(0.0, -0.02, 0.0))


def build_druid_nature_staff():
    """
    Ancient Gothic Druid Nature Staff (1.86m total):
    GripPoint at (0,0,0), gnarled root shaft (-0.82m .. +0.76m), sculpted crescent moon-antler cradle
    framing a glowing emerald nature heartstone at y=+0.98m (HIGH ABOVE HEAD!).
    """
    b1.reset_scene()
    col = b1.make_collection("Druid_NatureStaff")
    m_wood = b1.make_pbr_mat("M_Wpn_DarkWood", (0.28, 0.22, 0.14), 0.06, 0.66, tex_prefix="T_Wpn_LeatherWood")
    m_gold = b1.make_pbr_mat("M_Wpn_AntiqueGold", (0.82, 0.68, 0.34), 0.82, 0.36, tex_prefix="T_Wpn_AntiqueGold")
    m_emerald = b1.make_pbr_mat("M_Wpn_RuneEmerald", (0.30, 0.98, 0.52), 0.18, 0.18, emission=(0.20, 1.0, 0.44), emission_strength=3.8)

    root = b1.add_empty("Druid_NatureStaff", (0, 0, 0), col, radius=0.08)

    # 1. Gnarled Organic Twisted Ancient-Oak Shaft + Antler Crescent Cradle
    bm_wood = bmesh.new()
    segs = [
        (0.00, -0.82, 0.00, 0.016),
        (0.01, -0.42, -0.01, 0.021),
        (0.00,  0.00, 0.00, 0.023),
        (-0.01, 0.38, 0.01, 0.025),
        (0.00,  0.74, 0.00, 0.032),
    ]
    for i in range(len(segs) - 1):
        p0 = (segs[i][0], segs[i][1], segs[i][2])
        p1 = (segs[i + 1][0], segs[i + 1][1], segs[i + 1][2])
        b1.add_cylinder(bm_wood, p0, p1, segs[i][3], segs[i + 1][3], segs=8)
    # Sculpted sweeping crescent horn / antler fork framing the emerald heartstone!
    b1.add_cylinder(bm_wood, (0.0, 0.72, 0.0), (0.14, 0.88, 0.0), 0.026, 0.020, segs=8)
    b1.add_cylinder(bm_wood, (0.14, 0.88, 0.0), (0.08, 1.12, 0.0), 0.020, 0.009, segs=8)
    b1.add_cylinder(bm_wood, (0.08, 1.12, 0.0), (-0.06, 1.16, 0.0), 0.009, 0.004, segs=6)
    # Counter-branch antler tine on opposite side
    b1.add_cylinder(bm_wood, (0.0, 0.74, 0.0), (-0.11, 0.86, 0.0), 0.022, 0.015, segs=8)
    b1.add_cylinder(bm_wood, (-0.11, 0.86, 0.0), (-0.15, 1.02, 0.0), 0.015, 0.006, segs=6)
    wood_obj = b1.create_mesh_obj("DruidStaff_GnarledWood", bm_wood, col, m_wood, smooth=True)
    wood_obj.parent = root

    # 2. Ancient Bronze Root Bindings & Leaf Collar
    bm_gold = bmesh.new()
    b1.add_cylinder(bm_gold, (0, -0.14, 0), (0, -0.10, 0), 0.028, 0.028, segs=8)
    b1.add_cylinder(bm_gold, (0, 0.10, 0), (0, 0.14, 0), 0.028, 0.028, segs=8)
    b1.add_cylinder(bm_gold, (0, 0.68, 0), (0, 0.74, 0), 0.036, 0.042, segs=8)
    b1.add_cylinder(bm_gold, (0, -0.80, 0), (0, -0.86, 0), 0.020, 0.005, segs=8)
    gold_obj = b1.create_mesh_obj("DruidStaff_BronzeBindings", bm_gold, col, m_gold)
    gold_obj.parent = root

    # 3. Suspended Glowing Emerald Forest Heartstone
    bm_gem = bmesh.new()
    b1.add_cylinder(bm_gem, (0.01, 0.86, 0), (0.01, 0.96, 0), 0.005, 0.062, segs=6)
    b1.add_cylinder(bm_gem, (0.01, 0.96, 0), (0.01, 1.06, 0), 0.062, 0.005, segs=6)
    gem_obj = b1.create_mesh_obj("DruidStaff_NatureHeartstone", bm_gem, col, m_emerald)
    gem_obj.parent = root

    b1.add_empty("GripPoint", (0, 0, 0), col, parent=root)
    b1.add_empty("PrimaryAttach", (0, 0, 0), col, parent=root)
    b1.add_empty("MagicEmitSocket", (0.01, 0.96, 0), col, parent=root)
    b1.add_empty("ProjectileSpawn", (0.01, 1.04, 0), col, parent=root)
    b1.add_empty("VfxTrailSocket", (0.01, 1.04, 0), col, parent=root)

    return export_batch_weapon("Druid_NatureStaff", "Druid", "Staff", 3, root, col,
                               cam_pos=(2.18, 0.18, 0.65), cam_target=(0.0, 0.16, 0.0))


def build_cleric_holy_hammer():
    """Ancient Gothic Cleric Cathedral Warhammer (grip at (0,0,0), golden war-head at y=+0.74m)."""
    b1.reset_scene()
    col = b1.make_collection("Cleric_HolyHammer")
    m_steel = b1.make_pbr_mat("M_Wpn_DarkSteel", (0.62, 0.64, 0.70), 0.90, 0.28, tex_prefix="T_Wpn_DarkSteel")
    m_gold = b1.make_pbr_mat("M_Wpn_AntiqueGold", (0.98, 0.82, 0.40), 0.88, 0.26, tex_prefix="T_Wpn_AntiqueGold")
    m_grip = b1.make_pbr_mat("M_Wpn_LeatherGrip", (0.34, 0.22, 0.16), 0.05, 0.70, tex_prefix="T_Wpn_LeatherWood")
    m_holy = b1.make_pbr_mat("M_Wpn_RuneHoly", (1.0, 0.94, 0.68), 0.25, 0.18, emission=(1.0, 0.90, 0.48), emission_strength=3.6)

    root = b1.add_empty("Cleric_HolyHammer", (0, 0, 0), col, radius=0.08)

    # 1. Heavy Octagonal Cathedral Hammer Head + Steel Haft
    bm_head = bmesh.new()
    b1.add_cylinder(bm_head, (0, 0.12, 0), (0, 0.68, 0), 0.021, 0.026, segs=8)
    # Cathedral double-faced warhammer block (aligned along X)
    b1.add_box(bm_head, (0.0, 0.74, 0.0), (0.32, 0.145, 0.135))
    # Beveled impact faces on +/-X
    b1.add_cylinder(bm_head, (0.16, 0.74, 0), (0.20, 0.74, 0), 0.072, 0.058, segs=8)
    b1.add_cylinder(bm_head, (-0.16, 0.74, 0), (-0.20, 0.74, 0), 0.072, 0.058, segs=8)
    head_obj = b1.create_mesh_obj("ClericHammer_Head", bm_head, col, m_steel)
    head_obj.parent = root

    # 2. Radiant Gold Cathedral Finial Spire, Flanges & Pommel
    bm_gold = bmesh.new()
    b1.add_cylinder(bm_gold, (0, 0.81, 0), (0, 0.96, 0), 0.044, 0.004, segs=8)   # Top holy spire
    b1.add_box(bm_gold, (0.0, 0.74, 0.0), (0.11, 0.168, 0.155))                 # Gold central collar wrap
    b1.add_cylinder(bm_gold, (0, 0.58, 0), (0, 0.66, 0), 0.032, 0.046, segs=8)
    b1.add_cylinder(bm_gold, (0, 0.10, 0), (0, 0.15, 0), 0.034, 0.026, segs=8)
    b1.add_cylinder(bm_gold, (0, -0.18, 0), (0, -0.24, 0), 0.024, 0.038, segs=8)
    b1.add_cylinder(bm_gold, (0, -0.24, 0), (0, -0.29, 0), 0.038, 0.006, segs=8)
    gold_obj = b1.create_mesh_obj("ClericHammer_GoldReliquary", bm_gold, col, m_gold)
    gold_obj.parent = root

    bm_grip = bmesh.new()
    b1.add_cylinder(bm_grip, (0, -0.18, 0), (0, 0.10, 0), 0.022, 0.022, segs=10)
    grip_obj = b1.create_mesh_obj("ClericHammer_Grip", bm_grip, col, m_grip, smooth=True)
    grip_obj.parent = root

    # 4. Glowing Holy Sunburst Cross Inlays on Both Sides & Striking Faces
    bm_rune = bmesh.new()
    b1.add_box(bm_rune, (0.0, 0.74, 0.079), (0.24, 0.024, 0.006))
    b1.add_box(bm_rune, (0.0, 0.74, -0.079), (0.24, 0.024, 0.006))
    b1.add_box(bm_rune, (0.0, 0.74, 0.079), (0.024, 0.115, 0.006))
    b1.add_box(bm_rune, (0.0, 0.74, -0.079), (0.024, 0.115, 0.006))
    rune_obj = b1.create_mesh_obj("ClericHammer_HolyRune", bm_rune, col, m_holy)
    rune_obj.parent = root

    b1.add_empty("GripPoint", (0, 0, 0), col, parent=root)
    b1.add_empty("PrimaryAttach", (0, 0, 0), col, parent=root)
    b1.add_empty("ImpactPoint", (0, 0.74, 0), col, parent=root)
    b1.add_empty("MagicEmitSocket", (0, 0.90, 0), col, parent=root)
    b1.add_empty("VfxTrailSocket", (0, 0.84, 0), col, parent=root)

    return export_batch_weapon("Cleric_HolyHammer", "Cleric", "Hammer_Shield", 3, root, col,
                               cam_pos=(1.42, 0.34, 0.56), cam_target=(0.0, 0.34, 0.0))


def build_cleric_relic_shield():
    """
    Round Convex Gothic Sunburst Reliquary Shield (0.68m diameter).
    Blender +Y => Unity +Z (Height UP), Blender +Z => Unity +Y (Outer Sunburst Crest FORWARD toward enemies!).
    """
    b1.reset_scene()
    col = b1.make_collection("Cleric_RelicShield")
    m_steel = b1.make_pbr_mat("M_Wpn_DarkSteel", (0.58, 0.60, 0.66), 0.88, 0.28, tex_prefix="T_Wpn_DarkSteel")
    m_gold = b1.make_pbr_mat("M_Wpn_AntiqueGold", (0.98, 0.82, 0.40), 0.88, 0.26, tex_prefix="T_Wpn_AntiqueGold")
    m_grip = b1.make_pbr_mat("M_Wpn_LeatherGrip", (0.30, 0.20, 0.14), 0.05, 0.72, tex_prefix="T_Wpn_LeatherWood")
    m_holy = b1.make_pbr_mat("M_Wpn_RuneHoly", (1.0, 0.94, 0.68), 0.25, 0.18, emission=(1.0, 0.90, 0.48), emission_strength=3.6)

    root = b1.add_empty("Cleric_RelicShield", (0, 0, 0), col, radius=0.08)

    n = 16
    rad = 0.34
    bm_body = bmesh.new()
    fv = [bm_body.verts.new((math.cos(2 * math.pi * i / n) * rad, math.sin(2 * math.pi * i / n) * rad, 0.048)) for i in range(n)]
    bv = [bm_body.verts.new((math.cos(2 * math.pi * i / n) * (rad * 0.96), math.sin(2 * math.pi * i / n) * (rad * 0.96), 0.012)) for i in range(n)]
    fc = bm_body.verts.new((0, 0, 0.135))
    bc = bm_body.verts.new((0, 0, 0.075))
    for i in range(n):
        j = (i + 1) % n
        bm_body.faces.new((fv[i], fv[j], fc))
        bm_body.faces.new((bv[j], bv[i], bc))
        bm_body.faces.new((fv[j], fv[i], bv[i], bv[j]))
    body_obj = b1.create_mesh_obj("ClericShield_Body", bm_body, col, m_steel)
    body_obj.parent = root

    # Radiant Gold Rim + 8 Sunburst Rays + Sacred Umbo Dome
    bm_trim = bmesh.new()
    for i in range(n):
        j = (i + 1) % n
        p0 = (math.cos(2 * math.pi * i / n) * rad, math.sin(2 * math.pi * i / n) * rad, 0.048)
        p1 = (math.cos(2 * math.pi * j / n) * rad, math.sin(2 * math.pi * j / n) * rad, 0.048)
        b1.add_cylinder(bm_trim, p0, p1, 0.024, 0.024, segs=6)
    for k in range(8):
        ang = k * (math.pi * 0.25)
        b1.add_cylinder(bm_trim, (0, 0, 0.132), (math.cos(ang) * 0.32, math.sin(ang) * 0.32, 0.056), 0.018, 0.010, segs=6)
    b1.add_cylinder(bm_trim, (0, 0, 0.125), (0, 0, 0.175), 0.088, 0.028, segs=12)
    trim_obj = b1.create_mesh_obj("ClericShield_SunburstGold", bm_trim, col, m_gold)
    trim_obj.parent = root

    bm_rune = bmesh.new()
    b1.add_cylinder(bm_rune, (0, 0, 0.168), (0, 0, 0.184), 0.046, 0.012, segs=8)
    rune_obj = b1.create_mesh_obj("ClericShield_HolyCore", bm_rune, col, m_holy)
    rune_obj.parent = root

    bm_grip = bmesh.new()
    b1.add_cylinder(bm_grip, (-0.07, 0.0, 0.0), (0.07, 0.0, 0.0), 0.018, 0.018, segs=8)
    b1.add_box(bm_grip, (-0.07, 0.0, 0.025), (0.022, 0.030, 0.055))
    b1.add_box(bm_grip, (0.07, 0.0, 0.025), (0.022, 0.030, 0.055))
    grip_obj = b1.create_mesh_obj("ClericShield_BackGrip", bm_grip, col, m_grip)
    grip_obj.parent = root

    b1.add_empty("GripPoint", (0, 0, 0), col, parent=root)
    b1.add_empty("PrimaryAttach", (0, 0, 0), col, parent=root)
    b1.add_empty("ForearmAttach", (0, -0.14, -0.02), col, parent=root)
    b1.add_empty("ShieldFrontCenter", (0, 0, 0.18), col, parent=root)

    return export_batch_weapon("Cleric_RelicShield", "Cleric", "Hammer_Shield", 3, root, col,
                               cam_pos=(0.76, 0.22, 0.98), cam_target=(0.0, 0.0, 0.06))


def main():
    manifest_path = os.path.join(UNITY_WEAPONS_ROOT, "weapon_asset_manifest.json")
    if os.path.exists(manifest_path):
        with open(manifest_path, "r", encoding="utf-8") as f:
            manifest = json.load(f)
        # Keep existing Batch 1 entries cleanly without duplicating
        manifest["weapons"] = [w for w in manifest.get("weapons", []) if w.get("batch") == 1]
    else:
        manifest = {"project": "Abyssal_Dungeon_3D", "art_direction": "Ancient Gothic Dark Fantasy", "weapons": []}

    # Batch 2 (5 assets)
    manifest["weapons"].append(build_guardian_heavy_mace())
    manifest["weapons"].append(build_guardian_tower_shield())
    manifest["weapons"].append(build_assassin_dagger(is_right=True))
    manifest["weapons"].append(build_assassin_dagger(is_right=False))
    manifest["weapons"].append(build_mage_arcane_staff())

    # Batch 3 (7 assets)
    manifest["weapons"].append(build_warlock_ritual_sword())
    manifest["weapons"].append(build_warlock_grimoire())
    manifest["weapons"].append(build_monk_gauntlet(is_right=True))
    manifest["weapons"].append(build_monk_gauntlet(is_right=False))
    manifest["weapons"].append(build_druid_nature_staff())
    manifest["weapons"].append(build_cleric_holy_hammer())
    manifest["weapons"].append(build_cleric_relic_shield())

    with open(manifest_path, "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2)
    print(f"=== ALL 3 BATCHES COMPLETE ({len(manifest['weapons'])} weapons total) -> {manifest_path} ===")


if __name__ == "__main__":
    main()

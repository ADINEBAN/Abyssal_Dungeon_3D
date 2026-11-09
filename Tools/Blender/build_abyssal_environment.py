# Tools/Blender/build_abyssal_environment.py
# Optional Blender CLI High-Poly/Mid-Poly FBX Generator (Track B in Phase 0 Audit).
# Usage when Blender 3.6+ / 4.x is installed:
#   blender --background --python Tools/Blender/build_abyssal_environment.py

import os
import math

try:
    import bpy
    import bmesh
except ImportError:
    raise SystemExit("Run inside Blender CLI: blender --background --python Tools/Blender/build_abyssal_environment.py")

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT_ENV = os.path.join(ROOT, "Assets", "Art", "Environments")
OUT_PROP = os.path.join(ROOT, "Assets", "Art", "Props")
os.makedirs(OUT_ENV, exist_ok=True)
os.makedirs(OUT_PROP, exist_ok=True)


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def add_bevel_and_uv(obj, width=0.035, segments=2):
    bev = obj.modifiers.new(name="Bevel", type='BEVEL')
    bev.width = width
    bev.segments = segments
    bev.limit_method = 'ANGLE'
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bev.name)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.02)
    bpy.ops.object.mode_set(mode='OBJECT')


def export_fbx(filepath):
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=False,
        apply_unit_scale=True,
        bake_space_transform=True,
        axis_forward='-Z',
        axis_up='Y',
        mesh_smooth_type='FACE',
    )


def build_high_gothic_pillar():
    reset_scene()
    parts = []
    specs = [(0.16, 1.38, 0.32), (0.46, 1.18, 0.30), (2.60, 0.84, 3.60), (4.75, 1.28, 0.34)]
    for (z_h, w, h) in specs:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, z_h))
        o = bpy.context.active_object
        o.scale = (w, w, h)
        bpy.ops.object.transform_apply(scale=True)
        add_bevel_and_uv(o, width=0.04, segments=2)
        parts.append(o)
    bpy.ops.object.select_all(action='SELECT')
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    parts[0].name = "GothicPillar_Blender"
    export_fbx(os.path.join(OUT_ENV, "GothicPillar_Blender.fbx"))


if __name__ == "__main__":
    build_high_gothic_pillar()
    print("[Blender Pipeline] Exported FBX assets to Assets/Art/Environments/")


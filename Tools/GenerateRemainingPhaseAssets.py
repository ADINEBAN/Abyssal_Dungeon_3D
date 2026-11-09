#!/usr/bin/env python3
"""
Phase 5, 7 & 8 - 3D Enemy Roster, Boss Abyssal Knight & Dark Fantasy Equipment Asset Generator
Generates 22 multi-part polygonal Wavefront .obj meshes (with UVs, vertex normals, triangulated faces):
- Phase 5 (11 meshes): 10 Enemy 3D kits + 1 Elite Affix Crown in Assets/Art/Enemies/*.obj
- Phase 7 (5 meshes):  Abyssal Knight Boss 3D Heavy Armor, Crown, Spines, Greatsword & Bulwark Shield in Assets/Art/Bosses/*.obj
- Phase 8 (6 meshes):  Dark Fantasy 3D Equipment (Helmet, Armor, Gloves, Boots, Ring, Amulet) in Assets/Art/Armor/*.obj
- Runtime mirrors written to Assets/Resources/EnvironmentMeshes/*_LOD0.txt for instant ObjMeshLoader spawning.
- Manifest saved to Assets/Art/asset-manifest-phases5-8.json
"""

import json
import math
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "Tools"))
from GeneratePhase4ClassAssets import MeshBuilder

ENEMY_DIR = os.path.join(ROOT, "Assets", "Art", "Enemies")
BOSS_DIR = os.path.join(ROOT, "Assets", "Art", "Bosses")
ARMOR_DIR = os.path.join(ROOT, "Assets", "Art", "Armor")
MIRROR_DIR = os.path.join(ROOT, "Assets", "Resources", "EnvironmentMeshes")
MANIFEST_PATH = os.path.join(ROOT, "Assets", "Art", "asset-manifest-phases5-8.json")

for d in (ENEMY_DIR, BOSS_DIR, ARMOR_DIR, MIRROR_DIR):
    os.makedirs(d, exist_ok=True)


def save_asset(mb, out_dir, category, target_Tris):
    obj_text = mb.to_obj()
    v_cnt, t_cnt = len(mb.verts), len(mb.tris)
    obj_path = os.path.join(out_dir, f"{mb.name}.obj")
    with open(obj_path, "w", encoding="utf-8") as f:
        f.write(obj_text)
    mirror_path = os.path.join(MIRROR_DIR, f"{mb.name}_LOD0.txt")
    with open(mirror_path, "w", encoding="utf-8") as f:
        f.write(obj_text)
    return {
        "name": mb.name,
        "category": category,
        "objPath": os.path.relpath(obj_path, ROOT).replace("\\", "/"),
        "runtimeResource": f"EnvironmentMeshes/{mb.name}_LOD0",
        "vertices": v_cnt,
        "triangles": t_cnt,
    }


# ==================== PHASE 5: 10 ENEMY 3D KITS + ELITE CROWN ====================

def build_enemy_goblin():
    mb = MeshBuilder("Enemy_Goblin_HoodDagger")
    # Scavenged leather cowl + long jagged goblin ears + shoulder spikes
    mb.add_lathe([(0.13, -0.04), (0.16, 0.05), (0.14, 0.16), (0.04, 0.25), (0.0, 0.28)], segs=10, oy=0.02)
    for s in (-1, 1):
        mb.add_tapered_tube(
            [(s * 0.12, 0.08, 0.0), (s * 0.26, 0.14, -0.02), (s * 0.38, 0.20, -0.05)],
            [0.045, 0.032, 0.005], segs=6
        )
    return mb


def build_enemy_goblin_archer():
    mb = MeshBuilder("Enemy_GoblinArcher_QuiverBow")
    # Back quiver cylinder + 4 arrow shafts/fletchings + scout visor
    mb.add_lathe([(0.055, -0.22), (0.065, 0.0), (0.075, 0.22), (0.07, 0.25)], segs=10, ox=0.08, oy=0.05, oz=-0.16)
    for i in range(4):
        ox = 0.05 + (i - 1.5) * 0.022
        mb.add_tapered_tube(
            [(ox, 0.18, -0.16), (ox + 0.02, 0.42, -0.18)],
            [0.010, 0.016], segs=5
        )
    return mb


def build_enemy_skeleton():
    mb = MeshBuilder("Enemy_Skeleton_RibcageHelm")
    # Cracked bone skull-dome + 4 pairs of curved thoracic ribs
    mb.add_lathe([(0.11, 0.0), (0.14, 0.08), (0.12, 0.18), (0.0, 0.22)], segs=10, oy=0.02)
    for r in range(4):
        y = -0.18 - r * 0.065
        for s in (-1, 1):
            mb.add_tapered_tube(
                [(0.0, y, -0.10), (s * 0.16, y, 0.0), (s * 0.08, y - 0.01, 0.12)],
                [0.022, 0.020, 0.012], segs=6
            )
    return mb


def build_enemy_skeleton_warrior():
    mb = MeshBuilder("Enemy_SkeletonWarrior_CrestShield")
    # Horned bone war-helm + spiked shoulder guards
    mb.add_lathe([(0.13, -0.02), (0.15, 0.08), (0.13, 0.19), (0.0, 0.24)], segs=12)
    mb.add_box(0.0, 0.22, 0.02, 0.035, 0.16, 0.26)
    for s in (-1, 1):
        mb.add_tapered_tube(
            [(s * 0.12, 0.14, 0.0), (s * 0.24, 0.24, 0.04), (s * 0.28, 0.36, 0.08)],
            [0.038, 0.025, 0.005], segs=7
        )
    return mb


def build_enemy_skeleton_mage():
    mb = MeshBuilder("Enemy_SkeletonMage_NecroCrown")
    # Tall spire necromancer crown + floating soul-lantern collar
    mb.add_lathe([(0.13, 0.02), (0.15, 0.10), (0.14, 0.16)], segs=12)
    for i in range(6):
        ang = i * (2 * math.pi / 6.0)
        x, z = math.cos(ang) * 0.135, math.sin(ang) * 0.135
        mb.add_tapered_tube(
            [(x, 0.14, z), (x * 1.12, 0.36 + (i % 2) * 0.07, z * 1.12)],
            [0.025, 0.004], segs=6
        )
    return mb


def build_enemy_zombie():
    mb = MeshBuilder("Enemy_Zombie_PlagueShackles")
    # Heavy iron neck-pillory shackle + hanging chain links + plague spikes
    mb.add_lathe([(0.16, -0.04), (0.18, 0.0), (0.16, 0.04)], segs=12, oy=-0.08)
    for s in (-1, 1):
        mb.add_box(s * 0.20, -0.08, 0.0, 0.08, 0.09, 0.10)
        mb.add_tapered_tube(
            [(s * 0.18, -0.06, -0.04), (s * 0.26, 0.12, -0.10)],
            [0.045, 0.012], segs=6
        )
    return mb


def build_enemy_dark_knight():
    mb = MeshBuilder("Enemy_DarkKnight_DreadArmor")
    # Winged dread-helm + dual shoulder spire blades (inspired by reference dark knight)
    mb.add_lathe([(0.14, -0.03), (0.16, 0.09), (0.13, 0.22), (0.0, 0.28)], segs=12)
    for s in (-1, 1):
        mb.add_tapered_tube(
            [(s * 0.12, 0.12, -0.02), (s * 0.22, 0.28, -0.06), (s * 0.28, 0.48, -0.12)],
            [0.036, 0.022, 0.004], segs=6
        )
        mb.add_tapered_tube(
            [(s * 0.24, -0.14, 0.0), (s * 0.38, 0.08, -0.04), (s * 0.42, 0.34, -0.08)],
            [0.048, 0.026, 0.004], segs=6
        )
    return mb


def build_enemy_demon():
    mb = MeshBuilder("Enemy_Demon_AbyssWingsHorns")
    # Curled ram-demon horns + spiked dorsal bat-wings (inspired by reference undead demon)
    for s in (-1, 1):
        mb.add_tapered_tube(
            [
                (s * 0.10, 0.14, 0.02),
                (s * 0.24, 0.24, -0.04),
                (s * 0.30, 0.12, -0.14),
                (s * 0.22, 0.02, -0.06),
            ],
            [0.048, 0.038, 0.026, 0.008], segs=8
        )
        # Wing frame struts on back
        mb.add_tapered_tube(
            [(s * 0.08, -0.18, -0.14), (s * 0.42, 0.12, -0.26), (s * 0.68, -0.05, -0.32)],
            [0.035, 0.026, 0.010], segs=6
        )
        mb.add_tapered_tube(
            [(s * 0.42, 0.12, -0.26), (s * 0.48, -0.32, -0.28)],
            [0.022, 0.006], segs=5
        )
    return mb


def build_enemy_crystal_golem():
    mb = MeshBuilder("Enemy_CrystalGolem_Monolith")
    # Faceted crystalline dorsal cluster & monolith core shards
    angles = [(-0.28, 0.18, -0.12), (0.28, 0.18, -0.12), (0.0, 0.28, -0.18), (-0.18, 0.05, -0.22), (0.18, 0.05, -0.22)]
    for idx, (ox, oy, oz) in enumerate(angles):
        h = 0.42 + (idx % 3) * 0.12
        mb.add_tapered_tube(
            [(ox * 0.4, oy * 0.4, oz * 0.5), (ox, oy + h, oz)],
            [0.085, 0.006], segs=6
        )
    return mb


def build_enemy_forest_monster():
    mb = MeshBuilder("Enemy_ForestMonster_BlightTreant")
    # Gnarled stag-branch canopy crown & twisted root mantle
    for s in (-1, 1):
        mb.add_tapered_tube(
            [(s * 0.12, 0.08, 0.0), (s * 0.28, 0.28, -0.04), (s * 0.42, 0.48, -0.08)],
            [0.045, 0.030, 0.008], segs=6
        )
        mb.add_tapered_tube(
            [(s * 0.28, 0.28, -0.04), (s * 0.20, 0.46, 0.08)],
            [0.024, 0.006], segs=5
        )
    return mb


def build_enemy_elite_crown():
    mb = MeshBuilder("Enemy_Elite_AffixCrownBanner")
    # Floating 8-point runic war-halo above Elite monsters
    mb.add_lathe([(0.28, 0.0), (0.31, 0.03), (0.28, 0.06)], segs=16, oy=0.42)
    for i in range(8):
        ang = i * (2 * math.pi / 8.0)
        x, z = math.cos(ang) * 0.295, math.sin(ang) * 0.295
        mb.add_tapered_tube(
            [(x, 0.45, z), (x * 1.10, 0.64 + (i % 2) * 0.08, z * 1.10)],
            [0.028, 0.004], segs=5
        )
    return mb


# ==================== PHASE 7: BOSS ABYSSAL KNIGHT 3D KIT ====================

def build_boss_crown_helm():
    mb = MeshBuilder("Boss_AbyssalKnight_CrownHelm")
    # Towering gothic war-helm with 6 tall dorsal/lateral abyssal spires (matching reference image 2)
    mb.add_lathe([(0.15, -0.05), (0.18, 0.08), (0.16, 0.22), (0.08, 0.34), (0.0, 0.40)], segs=14)
    mb.add_box(0.0, 0.18, 0.04, 0.04, 0.28, 0.32)
    for s in (-1, 1):
        mb.add_tapered_tube(
            [(s * 0.14, 0.12, 0.0), (s * 0.24, 0.34, -0.04), (s * 0.30, 0.62, -0.10)],
            [0.042, 0.024, 0.004], segs=6
        )
        mb.add_tapered_tube(
            [(s * 0.10, 0.20, -0.06), (s * 0.16, 0.46, -0.12), (s * 0.19, 0.72, -0.18)],
            [0.032, 0.018, 0.003], segs=6
        )
    return mb


def build_boss_dread_pauldrons():
    mb = MeshBuilder("Boss_AbyssalKnight_DreadPauldrons")
    # Layered dark-fantasy dread-pauldrons with upward-sweeping razor fins
    for s in (-1, 1):
        mb.add_lathe([(0.05, 0.18), (0.20, 0.12), (0.28, 0.0), (0.24, -0.12)], segs=10, ox=s * 0.32, oy=0.04)
        for k in range(3):
            z_off = (k - 1) * 0.07
            mb.add_tapered_tube(
                [(s * 0.32, 0.08, z_off), (s * 0.44, 0.34, z_off - 0.03), (s * 0.48, 0.64 - abs(k - 1) * 0.10, z_off - 0.06)],
                [0.036, 0.020, 0.003], segs=6
            )
    return mb


def build_boss_back_spines_cape():
    mb = MeshBuilder("Boss_AbyssalKnight_BackSpinesCape")
    # Segmented spinal armor plates + 6 tattered battle-tassets & dorsal energy core
    for i in range(6):
        y = 0.28 - i * 0.11
        mb.add_box(0.0, y, -0.16, 0.16 - i * 0.012, 0.08, 0.09)
    # 6 long armored waist/back tasset ribbons (matching reference image 2)
    for i in range(6):
        x = (i - 2.5) * 0.095
        mb.add_tapered_tube(
            [(x * 0.7, -0.25, -0.14), (x * 1.1, -0.65, -0.20), (x * 1.35, -1.08, -0.24)],
            [0.042, 0.032, 0.008], segs=5, scale_x=1.4, scale_z=0.35
        )
    return mb


def build_boss_greatsword():
    mb = MeshBuilder("Boss_AbyssalKnight_Greatsword")
    # Colossal 1.85m Abyssal Greatsword with runic fuller & spiked wing crossguard
    mb.add_lathe([(0.032, -0.32), (0.026, -0.15), (0.030, 0.0)], segs=10)
    mb.add_lathe([(0.065, -0.38), (0.075, -0.34), (0.02, -0.31)], segs=10)
    # Winged crossguard
    for s in (-1, 1):
        mb.add_tapered_tube(
            [(0.0, 0.04, 0.0), (s * 0.24, 0.09, 0.0), (s * 0.38, 0.18, 0.0)],
            [0.055, 0.038, 0.008], segs=6
        )
    # Massive serrated blade
    mb.add_tapered_tube(
        [(0.0, 0.06, 0.0), (0.0, 0.65, 0.0), (0.0, 1.25, 0.0), (0.0, 1.68, 0.0)],
        [0.11, 0.13, 0.095, 0.005], segs=6, scale_x=1.45, scale_z=0.22
    )
    return mb


def build_boss_bulwark_shield():
    mb = MeshBuilder("Boss_AbyssalKnight_BulwarkShield")
    # Gothic cathedral kite/tower shield with sculpted central relief & spiked rim
    mb.add_box(-0.08, 0.0, 0.0, 0.06, 0.96, 0.56)
    mb.add_box(-0.12, 0.04, 0.0, 0.04, 0.62, 0.32)
    for s in (-1, 1):
        mb.add_tapered_tube(
            [(-0.10, 0.44, s * 0.24), (-0.12, 0.62, s * 0.28)],
            [0.032, 0.005], segs=6
        )
    return mb


# ==================== PHASE 8: DARK FANTASY 3D EQUIPMENT MODELS ====================

def build_equip_helmet():
    mb = MeshBuilder("Equip_Helmet_Gothic")
    mb.add_lathe([(0.14, -0.06), (0.16, 0.06), (0.14, 0.18), (0.0, 0.24)], segs=12)
    mb.add_box(0.0, 0.14, 0.04, 0.03, 0.22, 0.28)
    return mb


def build_equip_armor():
    mb = MeshBuilder("Equip_Armor_Cuirass")
    mb.add_lathe([(0.18, -0.26), (0.24, -0.05), (0.26, 0.18), (0.20, 0.28)], segs=12, scale_z=0.72)
    for s in (-1, 1):
        mb.add_box(s * 0.28, 0.20, 0.0, 0.16, 0.14, 0.22)
    return mb


def build_equip_gloves():
    mb = MeshBuilder("Equip_Gloves_Gauntlets")
    for s in (-1, 1):
        mb.add_lathe([(0.065, -0.14), (0.085, 0.0), (0.075, 0.14)], segs=10, ox=s * 0.16)
    return mb


def build_equip_boots():
    mb = MeshBuilder("Equip_Boots_Greaves")
    for s in (-1, 1):
        mb.add_lathe([(0.08, -0.18), (0.075, 0.05), (0.085, 0.18)], segs=10, ox=s * 0.14)
        mb.add_box(s * 0.14, -0.16, 0.06, 0.13, 0.08, 0.22)
    return mb


def build_equip_ring():
    mb = MeshBuilder("Equip_Ring_Signet")
    mb.add_lathe([(0.14, -0.04), (0.16, 0.0), (0.14, 0.04)], segs=16)
    mb.add_box(0.0, 0.0, 0.16, 0.10, 0.10, 0.08)
    return mb


def build_equip_amulet():
    mb = MeshBuilder("Equip_Amulet_Talisman")
    mb.add_lathe([(0.02, -0.22), (0.14, -0.06), (0.18, 0.08), (0.08, 0.20), (0.0, 0.24)], segs=8, scale_z=0.45)
    return mb


def main():
    manifest = []

    enemy_builders = [
        build_enemy_goblin,
        build_enemy_goblin_archer,
        build_enemy_skeleton,
        build_enemy_skeleton_warrior,
        build_enemy_skeleton_mage,
        build_enemy_zombie,
        build_enemy_dark_knight,
        build_enemy_demon,
        build_enemy_crystal_golem,
        build_enemy_forest_monster,
        build_enemy_elite_crown,
    ]
    for fn in enemy_builders:
        manifest.append(save_asset(fn(), ENEMY_DIR, "Enemy3D", 160))

    boss_builders = [
        build_boss_crown_helm,
        build_boss_dread_pauldrons,
        build_boss_back_spines_cape,
        build_boss_greatsword,
        build_boss_bulwark_shield,
    ]
    for fn in boss_builders:
        manifest.append(save_asset(fn(), BOSS_DIR, "Boss3D", 220))

    equip_builders = [
        build_equip_helmet,
        build_equip_armor,
        build_equip_gloves,
        build_equip_boots,
        build_equip_ring,
        build_equip_amulet,
    ]
    for fn in equip_builders:
        manifest.append(save_asset(fn(), ARMOR_DIR, "Equipment3D", 140))

    with open(MANIFEST_PATH, "w", encoding="utf-8") as f:
        json.dump({"generatedCount": len(manifest), "assets": manifest}, f, indent=2)

    print(f"Generated {len(manifest)} 3D .obj models for Phases 5, 7, 8 successfully.")


if __name__ == "__main__":
    main()

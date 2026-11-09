#!/usr/bin/env python3
"""
Phase 4 - 3D Character Silhouette, Armor Regalia & Signature Weapon Asset Generator
Generates 28 clean polygonal Wavefront .obj meshes (vertices, UVs, normals, triangulated faces)
for all 10 Aether Hunter Classes (Section 2.2, 2.4 & 4 of Master Prompt):
- Saved to Assets/Art/Characters/Classes/*.obj & Assets/Art/Weapons/*.obj
- Runtime mirrors written to Assets/Resources/EnvironmentMeshes/*.txt for instant ObjMeshLoader spawning
- Full asset manifest written to Assets/Art/asset-manifest-phase4.json
"""

import json
import math
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CHAR_DIR = os.path.join(ROOT, "Assets", "Art", "Characters", "Classes")
WEAP_DIR = os.path.join(ROOT, "Assets", "Art", "Weapons")
MIRROR_DIR = os.path.join(ROOT, "Assets", "Resources", "EnvironmentMeshes")
MANIFEST_PATH = os.path.join(ROOT, "Assets", "Art", "asset-manifest-phase4.json")

for d in (CHAR_DIR, WEAP_DIR, MIRROR_DIR):
    os.makedirs(d, exist_ok=True)


class MeshBuilder:
    def __init__(self, name):
        self.name = name
        self.verts = []
        self.uvs = []
        self.tris = []  # (v0, v1, v2, uv0, uv1, uv2) 0-indexed

    def add_vert(self, x, y, z, u=0.5, v=0.5):
        idx = len(self.verts)
        self.verts.append((float(x), float(y), float(z)))
        self.uvs.append((float(u), float(v)))
        return idx

    def add_tri(self, a, b, c):
        if a == b or b == c or a == c:
            return
        self.tris.append((a, b, c))

    def add_quad(self, a, b, c, d):
        self.add_tri(a, b, c)
        self.add_tri(a, c, d)

    def add_box(self, cx, cy, cz, sx, sy, sz):
        hx, hy, hz = sx * 0.5, sy * 0.5, sz * 0.5
        # 8 corners
        pts = [
            (cx - hx, cy - hy, cz - hz),
            (cx + hx, cy - hy, cz - hz),
            (cx + hx, cy + hy, cz - hz),
            (cx - hx, cy + hy, cz - hz),
            (cx - hx, cy - hy, cz + hz),
            (cx + hx, cy - hy, cz + hz),
            (cx + hx, cy + hy, cz + hz),
            (cx - hx, cy + hy, cz + hz),
        ]
        faces = [
            (0, 1, 2, 3),  # -Z
            (5, 4, 7, 6),  # +Z
            (4, 0, 3, 7),  # -X
            (1, 5, 6, 2),  # +X
            (3, 2, 6, 7),  # +Y
            (4, 5, 1, 0),  # -Y
        ]
        uvq = [(0, 0), (1, 0), (1, 1), (0, 1)]
        for f in faces:
            idxs = [self.add_vert(*pts[f[k]], uvq[k][0], uvq[k][1]) for k in range(4)]
            self.add_quad(*idxs)

    def add_lathe(self, profile, segs=12, ox=0.0, oy=0.0, oz=0.0, scale_x=1.0, scale_z=1.0, arc=2 * math.pi, start_ang=0.0):
        """profile: list of (radius, y)"""
        closed = abs(arc - 2 * math.pi) < 1e-4
        cols = segs if closed else (segs + 1)
        rings = []
        for r_idx, (rad, y) in enumerate(profile):
            v_coord = r_idx / max(1, len(profile) - 1)
            row = []
            for s in range(cols):
                u_coord = s / float(segs)
                ang = start_ang + u_coord * arc
                x = ox + math.cos(ang) * rad * scale_x
                z = oz + math.sin(ang) * rad * scale_z
                row.append(self.add_vert(x, oy + y, z, u_coord, v_coord))
            rings.append(row)

        for r_idx in range(len(rings) - 1):
            r0 = rings[r_idx]
            r1 = rings[r_idx + 1]
            for s in range(segs):
                sn = (s + 1) % cols if closed else (s + 1)
                self.add_quad(r0[s], r0[sn], r1[sn], r1[s])

    def add_tapered_tube(self, pts, radii, segs=8, scale_x=1.0, scale_z=1.0):
        """Extrudes a tube along a 3D spine pts=[(x,y,z),...] with matching radii."""
        rings = []
        n = len(pts)
        for i, ((px, py, pz), rad) in enumerate(zip(pts, radii)):
            if i == 0:
                tx, ty, tz = pts[1][0] - px, pts[1][1] - py, pts[1][2] - pz
            elif i == n - 1:
                tx, ty, tz = px - pts[i - 1][0], py - pts[i - 1][1], pz - pts[i - 1][2]
            else:
                tx, ty, tz = pts[i + 1][0] - pts[i - 1][0], pts[i + 1][1] - pts[i - 1][1], pts[i + 1][2] - pts[i - 1][2]
            tlen = max(1e-6, math.sqrt(tx * tx + ty * ty + tz * tz))
            tx, ty, tz = tx / tlen, ty / tlen, tz / tlen
            # Pick up-reference
            ux, uy, uz = (0.0, 0.0, 1.0) if abs(ty) > 0.92 else (0.0, 1.0, 0.0)
            # bx = t x u
            bx = ty * uz - tz * uy
            by = tz * ux - tx * uz
            bz = tx * uy - ty * ux
            blen = max(1e-6, math.sqrt(bx * bx + by * by + bz * bz))
            bx, by, bz = bx / blen, by / blen, bz / blen
            # nx = b x t
            nx = by * tz - bz * ty
            ny = bz * tx - bx * tz
            nz = bx * ty - by * tx

            v_coord = i / max(1, n - 1)
            row = []
            for s in range(segs):
                ang = (s / float(segs)) * 2.0 * math.pi
                ca, sa = math.cos(ang) * scale_x, math.sin(ang) * scale_z
                vx = px + (bx * ca + nx * sa) * rad
                vy = py + (by * ca + ny * sa) * rad
                vz = pz + (bz * ca + nz * sa) * rad
                row.append(self.add_vert(vx, vy, vz, s / float(segs), v_coord))
            rings.append(row)

        for i in range(len(rings) - 1):
            r0, r1 = rings[i], rings[i + 1]
            for s in range(segs):
                sn = (s + 1) % segs
                self.add_quad(r0[s], r0[sn], r1[sn], r1[s])

        # Cap start & end
        c0 = self.add_vert(*pts[0], 0.5, 0.0)
        for s in range(segs):
            sn = (s + 1) % segs
            self.add_tri(c0, rings[0][sn], rings[0][s])
        c1 = self.add_vert(*pts[-1], 0.5, 1.0)
        for s in range(segs):
            sn = (s + 1) % segs
            self.add_tri(c1, rings[-1][s], rings[-1][sn])

    def add_blade_ribbon(self, spine_yz, half_widths, thickness=0.022):
        """Builds a sharp double-edged 3D sword/dagger/scythe blade with diamond cross-section!"""
        rings = []
        n = len(spine_yz)
        for i, ((cy, cz), hw) in enumerate(zip(spine_yz, half_widths)):
            v_c = i / max(1, n - 1)
            th = thickness * (1.0 - 0.75 * (v_c ** 2))
            if hw < 1e-4:
                tip = self.add_vert(0.0, cy, cz, 0.5, v_c)
                rings.append([tip, tip, tip, tip])
            else:
                # 4 diamond points: +Z edge, +X ridge, -Z edge, -X ridge
                p_front = self.add_vert(0.0, cy, cz + hw, 1.0, v_c)
                p_right = self.add_vert(th * 0.5, cy, cz, 0.5, v_c)
                p_back = self.add_vert(0.0, cy, cz - hw, 0.0, v_c)
                p_left = self.add_vert(-th * 0.5, cy, cz, 0.5, v_c)
                rings.append([p_front, p_right, p_back, p_left])

        for i in range(n - 1):
            r0, r1 = rings[i], rings[i + 1]
            for s in range(4):
                sn = (s + 1) % 4
                self.add_quad(r0[s], r0[sn], r1[sn], r1[s])

    def to_obj(self):
        # Compute smooth/per-face normals
        v_norms = [[0.0, 0.0, 0.0] for _ in self.verts]
        for a, b, c in self.tris:
            ax, ay, az = self.verts[a]
            bx, by, bz = self.verts[b]
            cx, cy, cz = self.verts[c]
            ux, uy, uz = bx - ax, by - ay, bz - az
            vx, vy, vz = cx - ax, cy - ay, cz - az
            nx = uy * vz - uz * vy
            ny = uz * vx - ux * vz
            nz = ux * vy - uy * vx
            for idx in (a, b, c):
                v_norms[idx][0] += nx
                v_norms[idx][1] += ny
                v_norms[idx][2] += nz

        lines = [f"# Abyssal Dungeon 3D - Phase 4 Authored Asset: {self.name}", f"o {self.name}"]
        for x, y, z in self.verts:
            lines.append(f"v {x:.5f} {y:.5f} {z:.5f}")
        for u, v in self.uvs:
            lines.append(f"vt {u:.4f} {v:.4f}")
        for nx, ny, nz in v_norms:
            ln = math.sqrt(nx * nx + ny * ny + nz * nz)
            if ln < 1e-6:
                nx, ny, nz = 0.0, 1.0, 0.0
            else:
                nx, ny, nz = nx / ln, ny / ln, nz / ln
            lines.append(f"vn {nx:.4f} {ny:.4f} {nz:.4f}")
        for a, b, c in self.tris:
            i0, i1, i2 = a + 1, b + 1, c + 1
            lines.append(f"f {i0}/{i0}/{i0} {i1}/{i1}/{i1} {i2}/{i2}/{i2}")
        return "\n".join(lines) + "\n"


def save_asset(mb, out_dir, category, class_name):
    obj_text = mb.to_obj()
    obj_path = os.path.join(out_dir, f"{mb.name}_LOD0.obj")
    with open(obj_path, "w", encoding="utf-8") as f:
        f.write(obj_text)
    mirror_path = os.path.join(MIRROR_DIR, f"{mb.name}_LOD0.txt")
    with open(mirror_path, "w", encoding="utf-8") as f:
        f.write(obj_text)
    return {
        "id": mb.name,
        "class": class_name,
        "category": category,
        "vertices": len(mb.verts),
        "triangles": len(mb.tris),
        "objPath": os.path.relpath(obj_path, ROOT).replace("\\", "/"),
        "runtimeMirror": os.path.relpath(mirror_path, ROOT).replace("\\", "/"),
    }


def build_all():
    manifest = []

    # 1. KNIGHT (Winged Greathelm + Runed Longsword + Heater Crest Shield)
    m = MeshBuilder("ClassHelm_Knight")
    m.add_lathe([(0.125, -0.04), (0.135, 0.05), (0.13, 0.14), (0.09, 0.21), (0.0, 0.24)], segs=14)
    m.add_box(0.0, 0.07, 0.125, 0.03, 0.16, 0.03)  # Cross nose-guard
    for side in (-1, 1):
        # Swept valkyrie/templar side wings
        pts = [(side * 0.12, 0.08, -0.01), (side * 0.19, 0.16, -0.05), (side * 0.24, 0.27, -0.11), (side * 0.26, 0.34, -0.16)]
        m.add_tapered_tube(pts, [0.028, 0.036, 0.024, 0.005], segs=6, scale_x=0.45, scale_z=1.3)
    manifest.append(save_asset(m, CHAR_DIR, "Headgear", "Knight"))

    m = MeshBuilder("ClassWeapon_KnightSword")
    m.add_lathe([(0.026, -0.14), (0.018, -0.10), (0.017, 0.02)], segs=8)  # Pommel + grip
    m.add_box(0.0, 0.04, 0.0, 0.035, 0.035, 0.26)  # Crossguard
    m.add_blade_ribbon([(0.05, 0.0), (0.25, 0.0), (0.68, 0.0), (0.86, 0.0), (0.96, 0.0)], [0.048, 0.045, 0.036, 0.022, 0.0], 0.024)
    manifest.append(save_asset(m, WEAP_DIR, "Weapon", "Knight"))

    m = MeshBuilder("ClassShield_KnightCrest")
    # Curved heater kite shield
    m.add_blade_ribbon([(-0.36, 0.0), (-0.12, 0.0), (0.16, 0.0), (0.30, 0.0)], [0.02, 0.19, 0.23, 0.21], 0.055)
    m.add_box(0.032, 0.02, 0.0, 0.02, 0.38, 0.045)  # Raised cross vertical
    m.add_box(0.032, 0.08, 0.0, 0.02, 0.045, 0.24)  # Raised cross horizontal
    manifest.append(save_asset(m, WEAP_DIR, "Shield", "Knight"))

    # 2. GUARDIAN (Bastion Crown-Helm + Gilded Warhammer + Cathedral Tower Bulwark)
    m = MeshBuilder("ClassHelm_Guardian")
    m.add_lathe([(0.135, -0.05), (0.145, 0.06), (0.14, 0.15), (0.11, 0.20), (0.0, 0.22)], segs=12)
    for i in range(5):
        ang = -0.9 + i * 0.45
        m.add_box(math.sin(ang) * 0.135, 0.22, math.cos(ang) * 0.135, 0.032, 0.09, 0.025)
    manifest.append(save_asset(m, CHAR_DIR, "Headgear", "Guardian"))

    m = MeshBuilder("ClassWeapon_GuardianHammer")
    m.add_lathe([(0.025, -0.12), (0.02, 0.56), (0.03, 0.64)], segs=8)
    m.add_box(0.0, 0.60, 0.0, 0.15, 0.15, 0.30)  # Heavy warhammer head
    m.add_tapered_tube([(0.0, 0.60, 0.15), (0.0, 0.60, 0.25)], [0.06, 0.005], segs=6)  # Back spike
    manifest.append(save_asset(m, WEAP_DIR, "Weapon", "Guardian"))

    m = MeshBuilder("ClassShield_GuardianBulwark")
    m.add_box(0.0, 0.02, 0.0, 0.06, 0.94, 0.52)
    m.add_box(0.035, 0.46, 0.0, 0.08, 0.08, 0.56)  # Top rampart trim
    m.add_box(0.035, -0.42, 0.0, 0.08, 0.08, 0.48)  # Bottom trim
    m.add_lathe([(0.0, -0.18), (0.09, 0.02), (0.06, 0.20), (0.0, 0.28)], segs=8, ox=0.038)  # Angelic relief boss
    manifest.append(save_asset(m, WEAP_DIR, "Shield", "Guardian"))

    # 3. BERSERKER (Horned Skull Helm from Ref Image 3 + Jagged Skull Zweihander Cleaver)
    m = MeshBuilder("ClassHelm_Berserker")
    m.add_lathe([(0.12, -0.04), (0.14, 0.06), (0.135, 0.15), (0.08, 0.21), (0.0, 0.23)], segs=12)
    m.add_tapered_tube([(0.0, 0.21, 0.0), (0.0, 0.38, 0.02)], [0.026, 0.003], segs=6)  # Central skull spike
    for side in (-1, 1):
        # Sweeping bull horns with spikes (Ref Image 3 top-left!)
        pts = [
            (side * 0.11, 0.12, 0.02),
            (side * 0.22, 0.16, 0.04),
            (side * 0.27, 0.27, 0.03),
            (side * 0.21, 0.39, 0.01),
        ]
        m.add_tapered_tube(pts, [0.042, 0.034, 0.022, 0.004], segs=8)
    manifest.append(save_asset(m, CHAR_DIR, "Headgear", "Berserker"))

    m = MeshBuilder("ClassWeapon_BerserkerCleaver")
    m.add_lathe([(0.032, -0.22), (0.022, -0.16), (0.022, 0.05)], segs=8)
    m.add_box(0.0, 0.07, 0.0, 0.06, 0.07, 0.34)  # Brutal skull guard
    m.add_blade_ribbon(
        [(0.09, 0.0), (0.35, 0.01), (0.68, -0.01), (1.02, 0.02), (1.28, 0.0), (1.42, 0.0)],
        [0.085, 0.095, 0.11, 0.125, 0.075, 0.0],
        0.036,
    )
    manifest.append(save_asset(m, WEAP_DIR, "Weapon", "Berserker"))

    # 4. ASSASSIN (Pointed Shadow Cowl + Curved Nightblade Kris + Split Shadow Mantle)
    m = MeshBuilder("ClassHelm_Assassin")
    m.add_lathe([(0.145, -0.06), (0.148, 0.06), (0.135, 0.16), (0.07, 0.24), (0.0, 0.29)], segs=12, oz=-0.02)
    m.add_tapered_tube([(0.0, 0.24, -0.02), (0.0, 0.27, -0.16)], [0.055, 0.005], segs=6)  # Pointed hood tail
    manifest.append(save_asset(m, CHAR_DIR, "Headgear", "Assassin"))

    m = MeshBuilder("ClassWeapon_AssassinDagger")
    m.add_lathe([(0.02, -0.10), (0.015, 0.02)], segs=6)
    m.add_box(0.0, 0.03, 0.0, 0.025, 0.025, 0.14)
    m.add_blade_ribbon([(0.04, 0.0), (0.16, 0.025), (0.30, -0.015), (0.44, 0.02), (0.54, 0.0)], [0.034, 0.042, 0.035, 0.02, 0.0], 0.018)
    manifest.append(save_asset(m, WEAP_DIR, "Weapon", "Assassin"))

    m = MeshBuilder("ClassBack_AssassinCloak")
    for side in (-1, 1):
        pts = [(side * 0.11, 0.05, -0.04), (side * 0.16, -0.28, -0.09), (side * 0.22, -0.64, -0.14), (side * 0.25, -0.92, -0.18)]
        m.add_tapered_tube(pts, [0.08, 0.11, 0.075, 0.01], segs=6, scale_x=1.4, scale_z=0.18)
    manifest.append(save_asset(m, CHAR_DIR, "BackRegalia", "Assassin"))

    # 5. RANGER (Feathered Hood + Elven Recurve Greatbow + Arrow Quiver)
    m = MeshBuilder("ClassHelm_Ranger")
    m.add_lathe([(0.155, -0.02), (0.135, 0.05), (0.115, 0.16), (0.03, 0.24), (0.0, 0.26)], segs=12)
    m.add_tapered_tube([(0.11, 0.10, 0.0), (0.17, 0.24, -0.12), (0.20, 0.36, -0.24)], [0.02, 0.028, 0.003], segs=6, scale_x=0.3)
    manifest.append(save_asset(m, CHAR_DIR, "Headgear", "Ranger"))

    m = MeshBuilder("ClassWeapon_RangerBow")
    bow_pts = [
        (0.0, -0.62, -0.14),
        (0.0, -0.44, 0.06),
        (0.0, -0.18, 0.13),
        (0.0, 0.0, 0.10),
        (0.0, 0.18, 0.13),
        (0.0, 0.44, 0.06),
        (0.0, 0.62, -0.14),
    ]
    m.add_tapered_tube(bow_pts, [0.008, 0.022, 0.028, 0.034, 0.028, 0.022, 0.008], segs=8)
    m.add_tapered_tube([(0.0, -0.60, -0.13), (0.0, 0.60, -0.13)], [0.005, 0.005], segs=4)  # Bowstring
    manifest.append(save_asset(m, WEAP_DIR, "Weapon", "Ranger"))

    m = MeshBuilder("ClassBack_RangerQuiver")
    m.add_tapered_tube([(-0.08, -0.28, -0.06), (0.08, 0.24, -0.08)], [0.065, 0.085], segs=10)
    for k in range(4):
        ox = 0.05 + (k - 1.5) * 0.025
        m.add_tapered_tube([(ox, 0.20, -0.08), (ox + 0.04, 0.42, -0.09)], [0.008, 0.016], segs=5)
    manifest.append(save_asset(m, CHAR_DIR, "BackRegalia", "Ranger"))

    # 6. MAGE (Arcane Archmage Spire Hat + Crystal Archstaff + Floating Astrolabe Halo)
    m = MeshBuilder("ClassHelm_Mage")
    m.add_lathe([(0.28, -0.01), (0.29, 0.01), (0.14, 0.04), (0.10, 0.18), (0.05, 0.34), (0.0, 0.48)], segs=14)
    manifest.append(save_asset(m, CHAR_DIR, "Headgear", "Mage"))

    m = MeshBuilder("ClassWeapon_MageStaff")
    m.add_lathe([(0.022, -0.45), (0.020, 0.85), (0.038, 0.96)], segs=8)
    # Twin crescent crystal cradle + floating diamond core
    for side in (-1, 1):
        pts = [(0.0, 0.95, 0.0), (0.0, 1.06, side * 0.14), (0.0, 1.24, side * 0.16), (0.0, 1.38, side * 0.06)]
        m.add_tapered_tube(pts, [0.024, 0.022, 0.018, 0.005], segs=6)
    m.add_lathe([(0.0, 1.06), (0.085, 1.18), (0.0, 1.32)], segs=6)  # Levitating mana crystal
    manifest.append(save_asset(m, WEAP_DIR, "Weapon", "Mage"))

    m = MeshBuilder("ClassBack_MageAstrolabe")
    ring_pts = []
    for s in range(17):
        a = (s / 16.0) * 2.0 * math.pi
        ring_pts.append((math.cos(a) * 0.38, 0.18 + math.sin(a) * 0.38, -0.10))
    m.add_tapered_tube(ring_pts, [0.018] * len(ring_pts), segs=6)
    manifest.append(save_asset(m, CHAR_DIR, "BackRegalia", "Mage"))

    # 7. WARLOCK (Abyssal Demon Horns + Twin-Skull Ritual Scepter + Floating Grimoire)
    m = MeshBuilder("ClassHelm_Warlock")
    m.add_lathe([(0.135, 0.02), (0.14, 0.07), (0.135, 0.09)], segs=12)  # Dark diadem band
    for side in (-1, 1):
        pts = [
            (side * 0.12, 0.08, 0.03),
            (side * 0.19, 0.20, -0.04),
            (side * 0.15, 0.34, -0.12),
            (side * 0.09, 0.44, -0.05),
        ]
        m.add_tapered_tube(pts, [0.038, 0.028, 0.018, 0.004], segs=8)
    manifest.append(save_asset(m, CHAR_DIR, "Headgear", "Warlock"))

    m = MeshBuilder("ClassWeapon_WarlockScepter")
    m.add_tapered_tube([(0.0, -0.25, 0.0), (0.0, 0.18, 0.03), (0.0, 0.55, -0.02), (0.0, 0.78, 0.0)], [0.022, 0.02, 0.022, 0.03], segs=8)
    m.add_lathe([(0.0, 0.74), (0.09, 0.83), (0.07, 0.92), (0.0, 0.96)], segs=10)  # Skull effigy orb
    m.add_blade_ribbon([(0.78, 0.04), (0.86, 0.18), (0.80, 0.34), (0.68, 0.42)], [0.045, 0.055, 0.035, 0.0], 0.02)  # Ritual reaper hook
    manifest.append(save_asset(m, WEAP_DIR, "Weapon", "Warlock"))

    m = MeshBuilder("ClassBack_WarlockGrimoire")
    m.add_box(-0.08, 0.0, 0.0, 0.15, 0.24, 0.028)  # Left tome cover
    m.add_box(0.08, 0.0, 0.0, 0.15, 0.24, 0.028)   # Right tome cover
    m.add_box(0.0, 0.0, 0.018, 0.26, 0.21, 0.022)  # Glowing open pages
    manifest.append(save_asset(m, CHAR_DIR, "BackRegalia", "Warlock"))

    # 8. MONK (Martial Lotus Kasa Hat + Dragon Cestus Gauntlet + Spirit Prayer Beads)
    m = MeshBuilder("ClassHelm_Monk")
    m.add_lathe([(0.34, 0.02), (0.24, 0.09), (0.11, 0.16), (0.02, 0.21), (0.0, 0.22)], segs=16)
    manifest.append(save_asset(m, CHAR_DIR, "Headgear", "Monk"))

    m = MeshBuilder("ClassWeapon_MonkGauntlet")
    m.add_lathe([(0.065, -0.08), (0.085, 0.04), (0.075, 0.14), (0.0, 0.18)], segs=10)
    for k in (-1, 0, 1):
        m.add_tapered_tube([(k * 0.035, 0.13, 0.03), (k * 0.04, 0.26, 0.05)], [0.018, 0.003], segs=5)
    manifest.append(save_asset(m, WEAP_DIR, "Weapon", "Monk"))

    m = MeshBuilder("ClassBack_MonkBeads")
    for s in range(10):
        a = (s / 10.0) * 2.0 * math.pi
        bx, by = math.cos(a) * 0.34, 0.22 + math.sin(a) * 0.34
        m.add_lathe([(0.0, -0.042), (0.044, 0.0), (0.0, 0.042)], segs=8, ox=bx, oy=by, oz=-0.08)
    manifest.append(save_asset(m, CHAR_DIR, "BackRegalia", "Monk"))

    # 9. DRUID (Ancient Stag Antler Crown + Crescent Moon-Scythe Nature Staff)
    m = MeshBuilder("ClassHelm_Druid")
    m.add_lathe([(0.135, 0.01), (0.14, 0.06), (0.132, 0.08)], segs=12)
    for side in (-1, 1):
        trunk = [(side * 0.11, 0.07, 0.01), (side * 0.20, 0.20, -0.02), (side * 0.28, 0.34, -0.03), (side * 0.31, 0.46, 0.02)]
        tine1 = [(side * 0.20, 0.20, -0.02), (side * 0.17, 0.31, 0.08)]
        tine2 = [(side * 0.26, 0.30, -0.03), (side * 0.35, 0.39, -0.09)]
        m.add_tapered_tube(trunk, [0.026, 0.020, 0.014, 0.004], segs=6)
        m.add_tapered_tube(tine1, [0.015, 0.003], segs=5)
        m.add_tapered_tube(tine2, [0.013, 0.003], segs=5)
    manifest.append(save_asset(m, CHAR_DIR, "Headgear", "Druid"))

    m = MeshBuilder("ClassWeapon_DruidScytheStaff")
    m.add_tapered_tube([(0.0, -0.45, 0.0), (0.0, 0.15, -0.03), (0.0, 0.65, 0.03), (0.0, 0.98, 0.0)], [0.026, 0.023, 0.025, 0.032], segs=8)
    # Crescent moon-sickle head
    cres = []
    rads = []
    for s in range(9):
        a = -0.6 + (s / 8.0) * 2.4
        cres.append((0.0, 1.12 + math.sin(a) * 0.22, math.cos(a) * 0.22 - 0.06))
        rads.append(0.032 * math.sin(math.pi * (s / 8.0)) + 0.004)
    m.add_tapered_tube(cres, rads, segs=6, scale_x=0.45)
    manifest.append(save_asset(m, WEAP_DIR, "Weapon", "Druid"))

    # 10. CLERIC (Radiant Seraph Sunburst Halo + Flanged Sun Reliquary Mace + Sanctified Buckler)
    m = MeshBuilder("ClassHelm_Cleric")
    m.add_lathe([(0.132, 0.02), (0.138, 0.08), (0.11, 0.18), (0.0, 0.24)], segs=12)  # Bishop/Seraph mitra
    for i in range(7):
        ang = -1.2 + (i / 6.0) * 2.4
        rx, ry = math.sin(ang), math.cos(ang)
        m.add_tapered_tube(
            [(rx * 0.16, 0.12 + ry * 0.16, -0.06), (rx * 0.30, 0.12 + ry * 0.30, -0.08)],
            [0.018, 0.003],
            segs=5,
        )
    manifest.append(save_asset(m, CHAR_DIR, "Headgear", "Cleric"))

    m = MeshBuilder("ClassWeapon_ClericMace")
    m.add_lathe([(0.024, -0.14), (0.020, 0.48), (0.042, 0.56)], segs=8)
    m.add_lathe([(0.0, 0.54), (0.11, 0.66), (0.0, 0.78)], segs=8)  # Sunburst flanged mace head
    for i in range(4):
        ang = i * (math.pi * 0.5)
        m.add_box(math.cos(ang) * 0.09, 0.66, math.sin(ang) * 0.09, 0.04, 0.16, 0.04)
    manifest.append(save_asset(m, WEAP_DIR, "Weapon", "Cleric"))

    m = MeshBuilder("ClassShield_ClericReliquary")
    m.add_lathe([(0.0, 0.045), (0.24, 0.03), (0.27, 0.0), (0.0, -0.02)], segs=14)  # Round sun buckler rotated on attach
    manifest.append(save_asset(m, WEAP_DIR, "Shield", "Cleric"))

    with open(MANIFEST_PATH, "w", encoding="utf-8") as f:
        json.dump(
            {
                "phase": "Phase 4 - 10-Class Silhouette, Armor Regalia & Signature Weapons",
                "totalAssets": len(manifest),
                "assets": manifest,
            },
            f,
            indent=2,
            ensure_ascii=False,
        )
    print(f"Generated {len(manifest)} Phase 4 3D class/weapon .obj assets + runtime mirrors!")


if __name__ == "__main__":
    build_all()


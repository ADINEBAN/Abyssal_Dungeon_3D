#!/usr/bin/env python3
"""
Generate3DEnvironmentKit.py
Generates cohesive Dark Fantasy 3D Modular Environment & Prop polygonal meshes (.obj)
with both LOD0 (high detail) and LOD1 (optimized detail), clean normals, and world-space UVs.
Outputs to:
  - Assets/Art/Environments/
  - Assets/Art/Props/
  - Assets/Resources/EnvironmentMeshes/ (as .txt copies for instant runtime mesh loading fallback)
"""

import math
import os
import json

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

ART_DIRS = [
    "Assets/Art/Characters",
    "Assets/Art/Enemies",
    "Assets/Art/Bosses",
    "Assets/Art/Weapons",
    "Assets/Art/Armor",
    "Assets/Art/Environments",
    "Assets/Art/Props",
    "Assets/Art/Materials",
    "Assets/Art/Materials/Shaders",
    "Assets/Art/Textures",
    "Assets/Art/Textures/Environment",
    "Assets/Art/VFX",
    "Assets/Resources/EnvironmentMeshes",
    "Assets/Prefabs/Environment",
]


class MeshBuilder:
    def __init__(self, name: str, uv_scale: float = 0.5):
        self.name = name
        self.uv_scale = uv_scale
        self.verts = []
        self.uvs = []
        self.norms = []
        self.faces = []  # tuples of ((v, vt, vn), ...)

    def _add_v(self, p):
        self.verts.append(p)
        return len(self.verts)

    def _add_vt(self, uv):
        self.uvs.append(uv)
        return len(self.uvs)

    def _add_vn(self, n):
        l = math.sqrt(n[0] * n[0] + n[1] * n[1] + n[2] * n[2])
        if l < 1e-8:
            n = (0.0, 1.0, 0.0)
        else:
            n = (n[0] / l, n[1] / l, n[2] / l)
        self.norms.append(n)
        return len(self.norms)

    @staticmethod
    def _sub(a, b):
        return (a[0] - b[0], a[1] - b[1], a[2] - b[2])

    @staticmethod
    def _cross(a, b):
        return (
            a[1] * b[2] - a[2] * b[1],
            a[2] * b[0] - a[0] * b[2],
            a[0] * b[1] - a[1] * b[0],
        )

    def add_quad(self, p0, p1, p2, p3, uv0=None, uv1=None, uv2=None, uv3=None):
        """Adds a quad (two triangles) with outward face normal (CCW: p0, p1, p2, p3)."""
        e1 = self._sub(p1, p0)
        e2 = self._sub(p3, p0)
        n = self._cross(e1, e2)
        vn = self._add_vn(n)

        if uv0 is None:
            # Triplanar-like dominant axis projection for zero UV stretching
            ax, ay, az = abs(n[0]), abs(n[1]), abs(n[2])
            s = self.uv_scale
            def proj(p):
                if ay >= ax and ay >= az:
                    return (p[0] * s, p[2] * s)
                elif ax >= az:
                    return (p[2] * s, p[1] * s)
                else:
                    return (p[0] * s, p[1] * s)
            uv0, uv1, uv2, uv3 = proj(p0), proj(p1), proj(p2), proj(p3)

        i0, i1, i2, i3 = self._add_v(p0), self._add_v(p1), self._add_v(p2), self._add_v(p3)
        t0, t1, t2, t3 = self._add_vt(uv0), self._add_vt(uv1), self._add_vt(uv2), self._add_vt(uv3)
        self.faces.append(((i0, t0, vn), (i1, t1, vn), (i2, t2, vn)))
        self.faces.append(((i0, t0, vn), (i2, t2, vn), (i3, t3, vn)))

    def add_tri(self, p0, p1, p2):
        e1 = self._sub(p1, p0)
        e2 = self._sub(p2, p0)
        n = self._cross(e1, e2)
        vn = self._add_vn(n)
        s = self.uv_scale
        ax, ay, az = abs(n[0]), abs(n[1]), abs(n[2])
        def proj(p):
            if ay >= ax and ay >= az:
                return (p[0] * s, p[2] * s)
            elif ax >= az:
                return (p[2] * s, p[1] * s)
            else:
                return (p[0] * s, p[1] * s)
        i0, i1, i2 = self._add_v(p0), self._add_v(p1), self._add_v(p2)
        t0, t1, t2 = self._add_vt(proj(p0)), self._add_vt(proj(p1)), self._add_vt(proj(p2))
        self.faces.append(((i0, t0, vn), (i1, t1, vn), (i2, t2, vn)))

    def add_beveled_box(self, cx, cy, cz, sx, sy, sz, bevel=0.04, top_cap=True, bottom_cap=True):
        """Creates a chamfered architectural stone block / timber beam (no harsh 90-deg placeholder look)."""
        hx, hy, hz = sx * 0.5, sy * 0.5, sz * 0.5
        b = min(bevel, hx * 0.35, hy * 0.35, hz * 0.35)
        if b <= 0.002:
            x0, x1 = cx - hx, cx + hx
            y0, y1 = cy - hy, cy + hy
            z0, z1 = cz - hz, cz + hz
            self.add_quad((x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1))
            self.add_quad((x1, y0, z0), (x0, y0, z0), (x0, y1, z0), (x1, y1, z0))
            self.add_quad((x0, y0, z0), (x0, y0, z1), (x0, y1, z1), (x0, y1, z0))
            self.add_quad((x1, y0, z1), (x1, y0, z0), (x1, y1, z0), (x1, y1, z1))
            if top_cap:
                self.add_quad((x0, y1, z1), (x1, y1, z1), (x1, y1, z0), (x0, y1, z0))
            if bottom_cap:
                self.add_quad((x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1))
            return

        # 3-ring vertical loft for clean 3D stone block bevels
        rings = [
            (cy - hy, hx - b, hz - b),
            (cy - hy + b, hx, hz),
            (cy + hy - b, hx, hz),
            (cy + hy, hx - b, hz - b),
        ]
        def ring_pts(y, rx, rz):
            ic = b * 0.7
            return [
                (cx - rx + ic, y, cz + rz),
                (cx + rx - ic, y, cz + rz),
                (cx + rx, y, cz + rz - ic),
                (cx + rx, y, cz - rz + ic),
                (cx + rx - ic, y, cz - rz),
                (cx - rx + ic, y, cz - rz),
                (cx - rx, y, cz - rz + ic),
                (cx - rx, y, cz + rz - ic),
            ]

        r_lists = [ring_pts(y, rx, rz) for (y, rx, rz) in rings]
        for r in range(len(r_lists) - 1):
            a, c = r_lists[r], r_lists[r + 1]
            n = len(a)
            for i in range(n):
                j = (i + 1) % n
                self.add_quad(a[i], a[j], c[j], c[i])

        if top_cap:
            top = r_lists[-1]
            tc = (cx, rings[-1][0], cz)
            for i in range(len(top)):
                j = (i + 1) % len(top)
                self.add_tri(tc, top[i], top[j])
        if bottom_cap:
            bot = r_lists[0]
            bc = (cx, rings[0][0], cz)
            for i in range(len(bot)):
                j = (i + 1) % len(bot)
                self.add_tri(bc, bot[j], bot[i])

    def add_lathe(self, profile, sides=8, cx=0.0, cz=0.0, rot_offset=0.0, cap_bottom=True, cap_top=True, fluted=0.0):
        """
        profile: list of (y, radius) from bottom to top.
        fluted: depth of concave vertical flutes if > 0.
        """
        rings = []
        for (y, rad) in profile:
            pts = []
            for s in range(sides):
                ang = rot_offset + (2.0 * math.pi * s) / sides
                r_mod = rad
                if fluted > 0.0 and s % 2 == 1:
                    r_mod = max(0.02, rad - fluted)
                pts.append((cx + math.cos(ang) * r_mod, y, cz + math.sin(ang) * r_mod))
            rings.append(pts)

        for r in range(len(rings) - 1):
            a, b = rings[r], rings[r + 1]
            for i in range(sides):
                j = (i + 1) % sides
                # Outward normal for Y-up ring: a[i] -> a[j] -> b[j] -> b[i] (Wait: cos,sin goes +X to +Z, which is CW from top; let's check cross!)
                # Let's check: p0=(1,0,0), p1=(0,0,1), p3=(1,1,0): e1=(-1,0,1), e2=(0,1,0) -> e1 x e2 = (-1, 0, -1) -> INWARD!
                # So outward CCW is: a[j], a[i], b[i], b[j]!
                self.add_quad(a[j], a[i], b[i], b[j])

        if cap_top and profile[-1][1] > 0.005:
            top = rings[-1]
            tc = (cx, profile[-1][0], cz)
            for i in range(sides):
                j = (i + 1) % sides
                self.add_tri(tc, top[j], top[i])

        if cap_bottom and profile[0][1] > 0.005:
            bot = rings[0]
            bc = (cx, profile[0][0], cz)
            for i in range(sides):
                j = (i + 1) % sides
                self.add_tri(bc, bot[i], bot[j])

    def add_extruded_tube(self, spine_pts, radii, sides=6, cap_ends=True):
        """Sweeps a polygon along a 3D curve (for horns, tree roots, curved arches, iron brackets)."""
        rings = []
        for idx, pt in enumerate(spine_pts):
            if idx == 0:
                t = self._sub(spine_pts[1], spine_pts[0])
            elif idx == len(spine_pts) - 1:
                t = self._sub(spine_pts[-1], spine_pts[-2])
            else:
                t = self._sub(spine_pts[idx + 1], spine_pts[idx - 1])
            tl = math.sqrt(t[0]*t[0] + t[1]*t[1] + t[2]*t[2]) or 1.0
            t = (t[0]/tl, t[1]/tl, t[2]/tl)
            up = (0.0, 1.0, 0.0) if abs(t[1]) < 0.92 else (1.0, 0.0, 0.0)
            bx = self._cross(up, t)
            bl = math.sqrt(bx[0]*bx[0] + bx[1]*bx[1] + bx[2]*bx[2]) or 1.0
            bx = (bx[0]/bl, bx[1]/bl, bx[2]/bl)
            by = self._cross(t, bx)

            rad = radii[idx]
            ring = []
            for s in range(sides):
                ang = (2.0 * math.pi * s) / sides
                ca, sa = math.cos(ang), math.sin(ang)
                px = pt[0] + (bx[0]*ca + by[0]*sa) * rad
                py = pt[1] + (bx[1]*ca + by[1]*sa) * rad
                pz = pt[2] + (bx[2]*ca + by[2]*sa) * rad
                ring.append((px, py, pz))
            rings.append(ring)

        for r in range(len(rings) - 1):
            a, b = rings[r], rings[r + 1]
            for i in range(sides):
                j = (i + 1) % sides
                self.add_quad(a[i], a[j], b[j], b[i])

        if cap_ends:
            if radii[0] > 0.004:
                c0 = spine_pts[0]
                for i in range(sides):
                    j = (i + 1) % sides
                    self.add_tri(c0, rings[0][j], rings[0][i])
            if radii[-1] > 0.004:
                c1 = spine_pts[-1]
                for i in range(sides):
                    j = (i + 1) % sides
                    self.add_tri(c1, rings[-1][i], rings[-1][j])

    def to_obj(self) -> str:
        lines = [f"# Abyssal Dungeon 3D Authored Asset: {self.name}", f"o {self.name}"]
        for v in self.verts:
            lines.append(f"v {v[0]:.4f} {v[1]:.4f} {v[2]:.4f}")
        for vt in self.uvs:
            lines.append(f"vt {vt[0]:.4f} {vt[1]:.4f}")
        for vn in self.norms:
            lines.append(f"vn {vn[0]:.4f} {vn[1]:.4f} {vn[2]:.4f}")
        lines.append("s off")
        for f in self.faces:
            parts = [f"{vi}/{ti}/{ni}" for (vi, ti, ni) in f]
            lines.append("f " + " ".join(parts))
        return "\n".join(lines) + "\n"

    def tri_count(self) -> int:
        return len(self.faces)


# ==================== INDIVIDUAL DARK FANTASY 3D MODELS ====================

def build_gothic_pillar(lod: int) -> MeshBuilder:
    m = MeshBuilder(f"GothicPillar_LOD{lod}", uv_scale=0.55)
    # Stepped Cathedral Plinth Base (matching Reference Image 4!)
    m.add_beveled_box(0, 0.16, 0, 1.38, 0.32, 1.38, bevel=0.05 if lod == 0 else 0.0)
    m.add_beveled_box(0, 0.46, 0, 1.18, 0.30, 1.18, bevel=0.04 if lod == 0 else 0.0)
    m.add_beveled_box(0, 0.72, 0, 1.02, 0.22, 1.02, bevel=0.04 if lod == 0 else 0.0)

    # Fluted Gothic Shaft with stone course bands
    sides = 16 if lod == 0 else 8
    flute = 0.045 if lod == 0 else 0.0
    m.add_lathe([
        (0.82, 0.46),
        (1.95, 0.44),
        (2.02, 0.48),
        (2.10, 0.44),
        (3.25, 0.42),
        (3.32, 0.46),
        (3.40, 0.42),
        (4.35, 0.41),
    ], sides=sides, rot_offset=math.pi / sides, cap_bottom=False, cap_top=False, fluted=flute)

    if lod == 0:
        # 4 corner colonnette shafts hugging the pillar (classic Gothic cathedral detail!)
        for sx in (-0.34, 0.34):
            for sz in (-0.34, 0.34):
                m.add_lathe([(0.82, 0.085), (4.35, 0.08)], sides=6, cx=sx, cz=sz, cap_bottom=False, cap_top=False)

    # Flared Gothic Capital
    m.add_beveled_box(0, 4.50, 0, 1.04, 0.26, 1.04, bevel=0.04 if lod == 0 else 0.0)
    m.add_beveled_box(0, 4.78, 0, 1.26, 0.30, 1.26, bevel=0.05 if lod == 0 else 0.0)
    m.add_beveled_box(0, 5.02, 0, 1.42, 0.20, 1.42, bevel=0.03 if lod == 0 else 0.0)
    return m


def build_gothic_arch_vault(lod: int) -> MeshBuilder:
    """Pointed Gothic stone arch spanning 4.0m width, springing from y=3.6m to apex y=5.5m."""
    m = MeshBuilder(f"GothicArchVault_LOD{lod}", uv_scale=0.5)
    segs = 8 if lod == 0 else 4
    span_half = 2.0
    y_spring = 3.55
    arch_h = 1.85
    thickness = 0.46

    # Left & right arch ribs
    for side in (-1.0, 1.0):
        pts = []
        rads = []
        for i in range(segs + 1):
            t = i / segs
            # Pointed ogival curve from x = side*span_half -> 0
            x = side * span_half * (1.0 - t)
            y = y_spring + arch_h * math.sin(t * math.pi * 0.5) ** 0.78
            pts.append((x, y, 0.0))
            rads.append(0.24 if lod == 0 else 0.22)
        m.add_extruded_tube(pts, rads, sides=6 if lod == 0 else 4, cap_ends=True)

    # Central Keystone & upper spandrel beam
    m.add_beveled_box(0, y_spring + arch_h + 0.02, 0, 0.42, 0.56, thickness + 0.12, bevel=0.04 if lod == 0 else 0.0)
    m.add_beveled_box(0, y_spring + arch_h + 0.36, 0, 4.2, 0.28, thickness, bevel=0.03 if lod == 0 else 0.0)
    return m


def build_dungeon_wall_modular(lod: int) -> MeshBuilder:
    """4.0m wide x 5.2m tall layered Gothic Dungeon Wall with wainscoting, blind arch recess, and cornice."""
    m = MeshBuilder(f"DungeonWall_Modular_LOD{lod}", uv_scale=0.45)
    # Main backing wall
    m.add_beveled_box(0, 2.60, 0.0, 4.0, 5.20, 0.48, bevel=0.02 if lod == 0 else 0.0)
    # Heavy stone plinth base molding
    m.add_beveled_box(0, 0.35, 0.08, 4.02, 0.70, 0.62, bevel=0.04 if lod == 0 else 0.0)
    # Upper cornice & frieze
    m.add_beveled_box(0, 4.95, 0.09, 4.02, 0.50, 0.66, bevel=0.04 if lod == 0 else 0.0)

    if lod == 0:
        # Left & right pilaster jambs framing a recessed Gothic blind-arch niche
        m.add_beveled_box(-1.62, 2.65, 0.09, 0.68, 3.95, 0.58, bevel=0.04)
        m.add_beveled_box(1.62, 2.65, 0.09, 0.68, 3.95, 0.58, bevel=0.04)
        # Stepped arch header blocks inside niche
        m.add_beveled_box(-0.78, 4.32, 0.08, 1.05, 0.38, 0.56, bevel=0.03)
        m.add_beveled_box(0.78, 4.32, 0.08, 1.05, 0.38, 0.56, bevel=0.03)
        m.add_beveled_box(0.0, 4.52, 0.10, 0.72, 0.42, 0.60, bevel=0.03)
        # Subtle protruding ashlar stone blocks for rich shadow relief
        blocks = [(-0.62, 1.35, 1.15, 0.45), (0.62, 1.35, 1.10, 0.45), (0.0, 2.35, 1.35, 0.48), (-0.55, 3.30, 1.15, 0.44), (0.60, 3.30, 1.10, 0.44)]
        for (bx, by, bw, bh) in blocks:
            m.add_beveled_box(bx, by, 0.05, bw, bh, 0.53, bevel=0.03)
    return m


def build_dungeon_floor_tile() -> MeshBuilder:
    """4.0m x 4.0m carved stone floor slab with recessed border gutters & raised flagstones (Reference Image 4!)."""
    m = MeshBuilder("DungeonFloor_ChannelTile_LOD0", uv_scale=0.5)
    # Sub-base slab (contains recessed channel floor at y = -0.04)
    m.add_beveled_box(0, -0.14, 0, 4.0, 0.22, 4.0, bevel=0.0, bottom_cap=False)
    # Outer curb stones forming recessed gutters around a central flagstone court
    m.add_beveled_box(0, -0.02, -1.82, 3.96, 0.08, 0.34, bevel=0.02, bottom_cap=False)
    m.add_beveled_box(0, -0.02, 1.82, 3.96, 0.08, 0.34, bevel=0.02, bottom_cap=False)
    m.add_beveled_box(-1.82, -0.02, 0, 0.34, 0.08, 3.24, bevel=0.02, bottom_cap=False)
    m.add_beveled_box(1.82, -0.02, 0, 0.34, 0.08, 3.24, bevel=0.02, bottom_cap=False)
    # 3x3 central beveled flagstones inside the recessed border channel!
    coords = [-0.92, 0.0, 0.92]
    for ix, fx in enumerate(coords):
        for iz, fz in enumerate(coords):
            h_var = 0.008 * ((ix + iz) % 2)
            m.add_beveled_box(fx, -0.015 + h_var, fz, 0.88, 0.08, 0.88, bevel=0.025, bottom_cap=False)
    return m


def build_stone_stairs_grand() -> MeshBuilder:
    """4.4m wide x 3.6m deep x 1.5m high grand cathedral stone staircase with side balustrades."""
    m = MeshBuilder("StoneStairs_Grand_LOD0", uv_scale=0.5)
    steps = 8
    total_w = 3.6
    step_d = 3.6 / steps
    step_h = 1.44 / steps
    for i in range(steps):
        h = (i + 1) * step_h
        z = -1.8 + (i + 0.5) * step_d
        m.add_beveled_box(0, h * 0.5, z, total_w, h, step_d + 0.04, bevel=0.025, bottom_cap=(i == 0))
    # Left & Right sloped stone cheek-walls / balustrades
    for sx in (-2.02, 2.02):
        m.add_beveled_box(sx, 0.45, -1.55, 0.44, 0.90, 0.65, bevel=0.04)
        m.add_beveled_box(sx, 0.85, 0.0, 0.40, 1.35, 2.55, bevel=0.04)
        m.add_beveled_box(sx, 1.18, 1.55, 0.46, 1.85, 0.65, bevel=0.04)
    return m


def build_gothic_gate_frame() -> MeshBuilder:
    """Monumental 6m-span Gothic Doorway Frame with twin buttress jambs and pointed arch."""
    m = MeshBuilder("GothicGateFrame_LOD0", uv_scale=0.45)
    for sx in (-2.95, 2.95):
        m.add_beveled_box(sx, 0.35, 0, 1.15, 0.70, 1.15, bevel=0.05)
        m.add_beveled_box(sx, 2.45, 0, 0.92, 3.60, 0.92, bevel=0.05)
        m.add_beveled_box(sx, 4.40, 0, 1.12, 0.42, 1.12, bevel=0.04)

    # Pointed Gothic Header spanning -2.95..+2.95
    for side in (-1.0, 1.0):
        pts, rads = [], []
        for i in range(7):
            t = i / 6.0
            x = side * 2.95 * (1.0 - t)
            y = 4.35 + 1.45 * (math.sin(t * math.pi * 0.5) ** 0.8)
            pts.append((x, y, 0.0))
            rads.append(0.36)
        m.add_extruded_tube(pts, rads, sides=6, cap_ends=True)

    m.add_beveled_box(0, 5.85, 0, 0.65, 0.78, 0.88, bevel=0.05)
    return m


def build_iron_portcullis() -> MeshBuilder:
    """Spiked wrought-iron portcullis gate (5.2m wide x 4.2m tall)."""
    m = MeshBuilder("IronPortcullis_LOD0", uv_scale=0.8)
    bars = 9
    for i in range(bars):
        x = -2.35 + (4.70 * i) / (bars - 1)
        # Octagonal iron bar + forged bottom spike point
        m.add_lathe([
            (0.05, 0.008),
            (0.32, 0.065),
            (4.15, 0.058),
            (4.32, 0.012),
        ], sides=6, cx=x, cz=0.0, cap_bottom=False, cap_top=False)
    # 3 Horizontal riveted crossbeams
    for y in (0.75, 2.15, 3.65):
        m.add_beveled_box(0, y, 0, 5.05, 0.16, 0.18, bevel=0.025)
    return m


def build_wall_torch_sconce() -> MeshBuilder:
    """Wrought-iron wall sconce bracket + spiked fire basket + wooden torch."""
    m = MeshBuilder("WallTorchSconce_LOD0", uv_scale=1.2)
    # Wall backplate (at z = 0)
    m.add_beveled_box(0, 0.0, 0.03, 0.22, 0.56, 0.06, bevel=0.015)
    # Horizontal arm & diagonal support strut jutting to +Z
    m.add_beveled_box(0, 0.10, 0.24, 0.07, 0.07, 0.42, bevel=0.01)
    m.add_extruded_tube([(0, -0.20, 0.04), (0, 0.08, 0.36)], [0.032, 0.028], sides=6)
    # Torch handle & flared iron cresset cage at (0, y, 0.40)
    m.add_lathe([
        (-0.28, 0.028),
        (0.12, 0.042),
        (0.24, 0.115),
        (0.38, 0.155),
        (0.42, 0.135),
    ], sides=8, cx=0.0, cz=0.40)
    # 4 iron cage prongs around fire bowl
    for i in range(4):
        ang = (math.pi * 0.5 * i) + math.pi * 0.25
        px, pz = math.cos(ang) * 0.145, 0.40 + math.sin(ang) * 0.145
        m.add_extruded_tube([(px*0.8, 0.24, 0.40 + (pz-0.40)*0.8), (px*1.15, 0.52, 0.40 + (pz-0.40)*1.15)], [0.022, 0.006], sides=5)
    return m


def build_treasure_chest_base() -> MeshBuilder:
    """Ironbound wooden treasure chest base (Reference Image 3!). Pivot at y=0."""
    m = MeshBuilder("TreasureChest_IronboundBase_LOD0", uv_scale=0.9)
    # Main oak chest body
    m.add_beveled_box(0, 0.28, 0, 1.18, 0.52, 0.76, bevel=0.025)
    # 4 Corner & 2 Mid Iron Straps with raised silhouette
    for sx in (-0.54, -0.18, 0.18, 0.54):
        m.add_beveled_box(sx, 0.28, 0, 0.09, 0.55, 0.80, bevel=0.015)
    # Bottom corner iron feet & front keyhole escutcheon plate
    m.add_beveled_box(0, 0.05, 0, 1.24, 0.10, 0.82, bevel=0.02)
    m.add_beveled_box(0, 0.42, 0.40, 0.22, 0.24, 0.06, bevel=0.015)
    return m


def build_treasure_chest_lid() -> MeshBuilder:
    """Curved barrel-vaulted ironbound chest lid (hinge pivot at local (0, 0, 0) = back edge of chest!)."""
    m = MeshBuilder("TreasureChest_IronboundLid_LOD0", uv_scale=0.9)
    # Lid spans z from 0.0 (back hinge) to +0.76 (front latch), arch height 0.30
    segs = 6
    w_wood = 1.16
    for i in range(segs):
        t0, t1 = i / segs, (i + 1) / segs
        tm = (t0 + t1) * 0.5
        z_c = tm * 0.76
        # Barrel arch profile
        y_arch = math.sin(tm * math.pi) * 0.26
        m.add_beveled_box(0, y_arch * 0.5 + 0.04, z_c, w_wood, y_arch + 0.08, (0.76 / segs) + 0.02, bevel=0.015)
    # Iron arch straps over the curved lid
    for sx in (-0.54, -0.18, 0.18, 0.54):
        pts, rads = [], []
        for i in range(segs + 1):
            t = i / segs
            pts.append((sx, math.sin(t * math.pi) * 0.27 + 0.06, t * 0.77))
            rads.append(0.048)
        m.add_extruded_tube(pts, rads, sides=6)
    return m


def build_abyssal_crystal_cluster(lod: int) -> MeshBuilder:
    """Organic multi-spire hexagonal quartz/abyssal crystal formation."""
    m = MeshBuilder(f"AbyssalCrystalCluster_LOD{lod}", uv_scale=0.7)
    # Craggy base rock
    m.add_lathe([(0.0, 0.68), (0.18, 0.62), (0.34, 0.42)], sides=7, cap_bottom=True, cap_top=True)
    spires = [
        ((0.0, 0.12, 0.0), (0.04, 1.85, 0.02), 0.22),
        ((-0.26, 0.10, 0.14), (-0.52, 1.32, 0.30), 0.16),
        ((0.28, 0.10, -0.10), (0.58, 1.42, -0.22), 0.17),
        ((-0.14, 0.10, -0.26), (-0.32, 1.05, -0.52), 0.14),
        ((0.16, 0.08, 0.26), (0.38, 0.95, 0.54), 0.13),
    ]
    if lod > 0:
        spires = spires[:3]
    for (p0, p1, r) in spires:
        pm = (
            p0[0] + (p1[0] - p0[0]) * 0.78,
            p0[1] + (p1[1] - p0[1]) * 0.78,
            p0[2] + (p1[2] - p0[2]) * 0.78,
        )
        m.add_extruded_tube([p0, pm, p1], [r, r * 0.82, 0.008], sides=6, cap_ends=True)
    return m


def build_runic_pillar_altar() -> MeshBuilder:
    """Octagonal stepped runic shrine / puzzle obelisk pedestal (floating crystal sits at y=2.1)."""
    m = MeshBuilder("RunicPillarAltar_LOD0", uv_scale=0.6)
    m.add_lathe([
        (0.0, 0.82),
        (0.22, 0.78),
        (0.26, 0.62),
        (0.52, 0.58),
        (0.58, 0.44),
        (1.42, 0.38),
        (1.56, 0.54),
        (1.72, 0.48),
    ], sides=8, fluted=0.03)
    # 4 upward stone claw prongs cradling the floating orb/rune
    for i in range(4):
        ang = i * math.pi * 0.5 + math.pi * 0.25
        c, s = math.cos(ang), math.sin(ang)
        m.add_extruded_tube(
            [(c*0.38, 1.65, s*0.38), (c*0.52, 1.98, s*0.52), (c*0.34, 2.28, s*0.34)],
            [0.075, 0.055, 0.015], sides=5
        )
    return m


def build_guardian_statue() -> MeshBuilder:
    """Gothic Hooded Sentinel Statue holding a resting greatsword on an octagonal plinth."""
    m = MeshBuilder("GuardianStatue_Gothic_LOD0", uv_scale=0.55)
    # Plinth pedestal
    m.add_beveled_box(0, 0.35, 0, 1.35, 0.70, 1.35, bevel=0.05)
    m.add_beveled_box(0, 0.82, 0, 1.08, 0.26, 1.08, bevel=0.04)
    # Robed torso & draped cloak
    m.add_lathe([
        (0.94, 0.46),
        (1.65, 0.38),
        (2.35, 0.44),
        (2.68, 0.26),
    ], sides=8, fluted=0.035)
    # Broad Gothic Pauldrons
    m.add_beveled_box(-0.42, 2.48, 0.0, 0.36, 0.28, 0.42, bevel=0.04)
    m.add_beveled_box(0.42, 2.48, 0.0, 0.36, 0.28, 0.42, bevel=0.04)
    # Deep Cowl Hood
    m.add_lathe([(2.65, 0.22), (2.92, 0.24), (3.16, 0.14), (3.26, 0.03)], sides=7, cz=0.03)
    # Resting Greatsword in front (+Z)
    m.add_beveled_box(0, 1.62, 0.42, 0.14, 1.35, 0.05, bevel=0.015)
    m.add_beveled_box(0, 2.28, 0.42, 0.56, 0.10, 0.10, bevel=0.02)
    m.add_beveled_box(0, 2.48, 0.42, 0.09, 0.34, 0.09, bevel=0.015)
    # Folded gauntlets over pommel
    m.add_beveled_box(0, 2.38, 0.34, 0.34, 0.18, 0.22, bevel=0.03)
    # Swept Stone Angelic/Gargoyle Wings behind shoulders
    for sx in (-1.0, 1.0):
        m.add_extruded_tube(
            [(sx*0.24, 2.35, -0.18), (sx*0.72, 2.95, -0.36), (sx*0.95, 2.15, -0.45), (sx*0.58, 1.35, -0.32)],
            [0.14, 0.16, 0.12, 0.05], sides=5
        )
    return m


def build_ancient_ruins_rubble() -> MeshBuilder:
    """Scattered beveled stone blocks & fallen column drum (Reference Image 4 left foreground!)."""
    m = MeshBuilder("AncientRuins_Rubble_LOD0", uv_scale=0.6)
    blocks = [
        (-0.55, 0.18, 0.15, 0.68, 0.36, 0.52),
        (0.32, 0.14, -0.22, 0.58, 0.28, 0.48),
        (-0.12, 0.12, 0.52, 0.46, 0.24, 0.40),
        (-0.38, 0.44, 0.08, 0.44, 0.26, 0.38),
        (0.62, 0.10, 0.35, 0.36, 0.20, 0.32),
    ]
    for (cx, cy, cz, sx, sy, sz) in blocks:
        m.add_beveled_box(cx, cy, cz, sx, sy, sz, bevel=0.04, bottom_cap=False)
    # Fallen broken fluted column drum lying sideways
    m.add_extruded_tube([(0.15, 0.24, -0.55), (0.78, 0.22, -0.18)], [0.24, 0.23], sides=8)
    return m


def build_spike_trap_plate() -> MeshBuilder:
    m = MeshBuilder("SpikeTrapPlate_LOD0", uv_scale=0.8)
    m.add_beveled_box(0, 0.04, 0, 2.35, 0.08, 2.35, bevel=0.025, bottom_cap=False)
    for sx in (-0.65, 0.0, 0.65):
        for sz in (-0.65, 0.0, 0.65):
            m.add_lathe([(0.07, 0.14), (0.12, 0.11)], sides=6, cx=sx, cz=sz, cap_bottom=False)
    return m


def build_spike_cluster() -> MeshBuilder:
    m = MeshBuilder("SpikeCluster_LOD0", uv_scale=1.0)
    for sx in (-0.65, 0.0, 0.65):
        for sz in (-0.65, 0.0, 0.65):
            m.add_lathe([(0.0, 0.095), (0.68, 0.045), (0.95, 0.005)], sides=5, cx=sx, cz=sz, cap_bottom=False, cap_top=False)
    return m


def build_fire_brazier() -> MeshBuilder:
    m = MeshBuilder("FireBrazierGrate_LOD0", uv_scale=0.8)
    m.add_lathe([
        (0.0, 0.52),
        (0.16, 0.46),
        (0.45, 0.26),
        (0.72, 0.62),
        (0.95, 0.72),
        (1.02, 0.66),
    ], sides=8)
    for i in range(4):
        ang = i * math.pi * 0.5
        c, s = math.cos(ang), math.sin(ang)
        m.add_extruded_tube([(c*0.42, 0.02, s*0.42), (c*0.58, 0.52, s*0.58), (c*0.68, 1.08, s*0.68)], [0.06, 0.05, 0.02], sides=5)
    return m


def build_cursed_tree_root() -> MeshBuilder:
    """Twisted petrified root arch / tendril for the Cursed Forest Dungeon biome."""
    m = MeshBuilder("CursedTreeRoot_LOD0", uv_scale=0.6)
    m.add_extruded_tube(
        [(0.0, 0.0, 0.0), (-0.28, 0.85, 0.18), (0.16, 1.75, -0.15), (-0.22, 2.65, 0.22), (-0.55, 3.25, 0.42)],
        [0.42, 0.30, 0.22, 0.14, 0.03], sides=7
    )
    m.add_extruded_tube(
        [(0.18, 0.0, -0.15), (0.45, 0.65, 0.22), (0.72, 1.35, 0.35)],
        [0.24, 0.15, 0.03], sides=6
    )
    m.add_extruded_tube(
        [(-0.08, 1.45, 0.05), (-0.65, 2.05, -0.28), (-0.95, 2.45, -0.42)],
        [0.16, 0.10, 0.02], sides=6
    )
    return m


def build_boss_throne_altar() -> MeshBuilder:
    """Monumental spiked Abyssal Sovereign Throne & Dais for Room 7 Boss Arena."""
    m = MeshBuilder("BossThroneAltar_LOD0", uv_scale=0.45)
    # 2-tier octagonal dais
    m.add_lathe([(0.0, 3.2), (0.28, 3.05)], sides=8)
    m.add_lathe([(0.28, 2.35), (0.56, 2.20)], sides=8)
    # Monumental Gothic Throne Seat & High Backrest
    m.add_beveled_box(0, 1.05, -0.35, 1.85, 0.98, 1.25, bevel=0.05)
    m.add_beveled_box(-0.82, 1.45, -0.20, 0.32, 0.65, 1.35, bevel=0.04)
    m.add_beveled_box(0.82, 1.45, -0.20, 0.32, 0.65, 1.35, bevel=0.04)
    m.add_beveled_box(0, 2.65, -0.82, 1.95, 3.65, 0.38, bevel=0.05)
    # Jagged Abyssal Crown Spikes fanning behind the throne (echoing Reference Image 2!)
    fan = [
        ((-1.15, 2.2, -0.92), (-1.95, 4.35, -1.15), 0.16),
        ((-0.65, 2.8, -0.92), (-1.15, 5.15, -1.10), 0.18),
        ((0.0, 3.2, -0.92), (0.0, 5.75, -1.08), 0.22),
        ((0.65, 2.8, -0.92), (1.15, 5.15, -1.10), 0.18),
        ((1.15, 2.2, -0.92), (1.95, 4.35, -1.15), 0.16),
    ]
    for (p0, p1, r) in fan:
        m.add_extruded_tube([p0, p1], [r, 0.015], sides=5)
    return m


def save_mesh(mb: MeshBuilder, category_folder: str, manifest_list: list):
    obj_text = mb.to_obj()
    rel_obj = f"{category_folder}/{mb.name}.obj"
    abs_obj = os.path.join(ROOT, rel_obj)
    with open(abs_obj, "w", encoding="utf-8") as f:
        f.write(obj_text)

    # Also write .txt mirror into Resources/EnvironmentMeshes/ so runtime works 100% deterministically
    res_txt = os.path.join(ROOT, "Assets/Resources/EnvironmentMeshes", f"{mb.name}.txt")
    with open(res_txt, "w", encoding="utf-8") as f:
        f.write(obj_text)

    manifest_list.append({
        "name": mb.name,
        "path": rel_obj,
        "triangles": mb.tri_count(),
        "vertices": len(mb.verts),
        "pivot": "bottom_center_y0",
        "uv_projection": "world_triplanar_scaled",
        "provenance": "Tools/Generate3DEnvironmentKit.py (Abyssal Dungeon Phase 1)",
    })
    print(f"  [OK] {rel_obj:<54} | verts={len(mb.verts):4d} | tris={mb.tri_count():4d}")


def main():
    for d in ART_DIRS:
        os.makedirs(os.path.join(ROOT, d), exist_ok=True)

    manifest = []
    print("Generating Abyssal Dungeon 3D Modular Environment & Prop Meshes...")

    env_models = [
        build_gothic_pillar(0),
        build_gothic_pillar(1),
        build_gothic_arch_vault(0),
        build_gothic_arch_vault(1),
        build_dungeon_wall_modular(0),
        build_dungeon_wall_modular(1),
        build_dungeon_floor_tile(),
        build_stone_stairs_grand(),
        build_gothic_gate_frame(),
        build_iron_portcullis(),
    ]
    for mb in env_models:
        save_mesh(mb, "Assets/Art/Environments", manifest)

    prop_models = [
        build_wall_torch_sconce(),
        build_treasure_chest_base(),
        build_treasure_chest_lid(),
        build_abyssal_crystal_cluster(0),
        build_abyssal_crystal_cluster(1),
        build_runic_pillar_altar(),
        build_guardian_statue(),
        build_ancient_ruins_rubble(),
        build_spike_trap_plate(),
        build_spike_cluster(),
        build_fire_brazier(),
        build_cursed_tree_root(),
        build_boss_throne_altar(),
    ]
    for mb in prop_models:
        save_mesh(mb, "Assets/Art/Props", manifest)

    manifest_path = os.path.join(ROOT, "Assets/Art/asset-manifest-phase1.json")
    with open(manifest_path, "w", encoding="utf-8") as f:
        json.dump({"phase": 1, "category": "Environment & Props 3D Kit", "assets": manifest}, f, indent=2)
    print(f"Saved manifest ({len(manifest)} 3D models) -> Assets/Art/asset-manifest-phase1.json")


if __name__ == "__main__":
    main()

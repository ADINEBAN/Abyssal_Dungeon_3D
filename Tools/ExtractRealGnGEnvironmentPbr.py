#!/usr/bin/env python3
"""
Extracts authentic Gold and Glory (Studio Dois) 1024x1024 hand-painted medieval dungeon
stone brick, flagstone floor, mossy ruin, aged oak wood, and wrought-iron PBR textures
into Assets/Resources/Environment/PBR/ while preserving existing Unity .meta GUIDs.
Also reconstructs clean tangent-space Normal + AO + Mask maps from Studio Dois _NS maps.
"""
import os
import stat
import glob
import numpy as np
from PIL import Image

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
GNG_ENV_ROOT = os.path.join(PROJECT_ROOT, "ExportedProject", "Assets", "ART", "Environment")
OUT_PBR_DIR = os.path.join(PROJECT_ROOT, "Assets", "Resources", "Environment", "PBR")


def find_gng_tex(candidates):
    for rel in candidates:
        full = os.path.join(GNG_ENV_ROOT, rel)
        if os.path.isfile(full):
            return full
    # Fallback recursive search by basename
    if os.path.isdir(GNG_ENV_ROOT):
        for rel in candidates:
            base = os.path.basename(rel).lower()
            for root, _, files in os.walk(GNG_ENV_ROOT):
                for fn in files:
                    if fn.lower() == base:
                        return os.path.join(root, fn)
    return None


def safe_save_png(img: Image.Image, path: str):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    if os.path.exists(path):
        try:
            os.chmod(path, stat.S_IWRITE)
        except OSError:
            pass
    img.save(path, "PNG", optimize=False)


def load_rgba_1024(path: str) -> np.ndarray:
    im = Image.open(path).convert("RGBA")
    if im.size != (1024, 1024):
        im = im.resize((1024, 1024), Image.Resampling.LANCZOS)
    return np.asarray(im, dtype=np.float32) / 255.0


def build_normal_ao_mask_from_albedo_and_ns(
    albedo_rgb: np.ndarray,
    ns_rgba: np.ndarray = None,
    base_metallic: float = 0.05,
    base_smoothness: float = 0.28,
    normal_strength: float = 1.6,
):
    lum = 0.299 * albedo_rgb[:, :, 0] + 0.587 * albedo_rgb[:, :, 1] + 0.114 * albedo_rgb[:, :, 2]

    if ns_rgba is not None:
        # Studio Dois _NS maps store packed X/Y normal in R/G (or A/G) and smoothness/AO in B/A
        nx = ns_rgba[:, :, 0] * 2.0 - 1.0
        ny = ns_rgba[:, :, 1] * 2.0 - 1.0
        # If R is mostly flat (~1.0) and A varies (DXT5nm style), use A for X
        if float(np.std(ns_rgba[:, :, 0])) < 0.02 and float(np.std(ns_rgba[:, :, 3])) > 0.02:
            nx = ns_rgba[:, :, 3] * 2.0 - 1.0
        nx *= normal_strength
        ny *= normal_strength
        nz = np.sqrt(np.clip(1.0 - np.clip(nx * nx + ny * ny, 0.0, 0.92), 0.08, 1.0))
        inv = 1.0 / np.maximum(1e-5, np.sqrt(nx * nx + ny * ny + nz * nz))
        nx *= inv
        ny *= inv
        nz *= inv
        smooth_chan = np.clip(ns_rgba[:, :, 2] * 0.65 + base_smoothness * 0.55, 0.08, 0.72)
    else:
        # Derive crisp Sobel tangent normal from hand-painted luminance
        gx = np.roll(lum, -1, axis=1) - np.roll(lum, 1, axis=1)
        gy = np.roll(lum, -1, axis=0) - np.roll(lum, 1, axis=0)
        nx = -gx * (2.4 * normal_strength)
        ny = -gy * (2.4 * normal_strength)
        nz = np.ones_like(nx)
        inv = 1.0 / np.sqrt(nx * nx + ny * ny + nz * nz)
        nx *= inv
        ny *= inv
        nz *= inv
        smooth_chan = np.clip(base_smoothness + (lum - 0.35) * 0.22, 0.08, 0.68)

    nrm_u8 = np.dstack([
        np.clip((nx * 0.5 + 0.5) * 255.0, 0, 255).astype(np.uint8),
        np.clip((ny * 0.5 + 0.5) * 255.0, 0, 255).astype(np.uint8),
        np.clip((nz * 0.5 + 0.5) * 255.0, 0, 255).astype(np.uint8),
        np.full_like(nx, 255, dtype=np.uint8),
    ])

    ao_f = np.clip(0.46 + lum * 0.78, 0.32, 1.0)
    ao_u8_1 = (ao_f * 255.0).astype(np.uint8)
    ao_u8 = np.dstack([ao_u8_1, ao_u8_1, ao_u8_1, np.full_like(ao_u8_1, 255)])

    met_u8 = np.full_like(ao_u8_1, int(round(base_metallic * 255.0)))
    sm_u8 = np.clip(smooth_chan * 255.0, 0, 255).astype(np.uint8)
    mask_u8 = np.dstack([met_u8, ao_u8_1, np.zeros_like(ao_u8_1), sm_u8])

    return Image.fromarray(nrm_u8, "RGBA"), Image.fromarray(ao_u8, "RGBA"), Image.fromarray(mask_u8, "RGBA")


def grade_albedo(rgb: np.ndarray, brightness: float = 1.0, desat: float = 0.0, contrast: float = 1.05) -> Image.Image:
    c = np.clip(rgb, 0.0, 1.0)
    lum = (0.299 * c[:, :, 0] + 0.587 * c[:, :, 1] + 0.114 * c[:, :, 2])[:, :, None]
    if desat > 0.0:
        c = c * (1.0 - desat) + lum * desat
    c = (c - 0.5) * contrast + 0.5
    c = np.clip(c * brightness, 0.03, 0.96)
    u8 = np.dstack([
        (c[:, :, 0] * 255.0).astype(np.uint8),
        (c[:, :, 1] * 255.0).astype(np.uint8),
        (c[:, :, 2] * 255.0).astype(np.uint8),
        np.full((c.shape[0], c.shape[1]), 255, dtype=np.uint8),
    ])
    return Image.fromarray(u8, "RGBA"), c


def export_set(prefix, albedo_candidates, ns_candidates, brightness=1.0, desat=0.12, contrast=1.08, metallic=0.05, smoothness=0.26):
    alb_path = find_gng_tex(albedo_candidates)
    ns_path = find_gng_tex(ns_candidates) if ns_candidates else None
    if not alb_path:
        print(f"[WARN] Could not find GnG texture for {prefix}: {albedo_candidates}")
        return False

    raw_alb = load_rgba_1024(alb_path)[:, :, :3]
    raw_ns = load_rgba_1024(ns_path) if ns_path else None

    alb_img, graded_rgb = grade_albedo(raw_alb, brightness=brightness, desat=desat, contrast=contrast)
    nrm_img, ao_img, msk_img = build_normal_ao_mask_from_albedo_and_ns(
        graded_rgb, raw_ns, base_metallic=metallic, base_smoothness=smoothness
    )

    safe_save_png(alb_img, os.path.join(OUT_PBR_DIR, f"{prefix}_Albedo.png"))
    safe_save_png(nrm_img, os.path.join(OUT_PBR_DIR, f"{prefix}_Normal.png"))
    safe_save_png(ao_img, os.path.join(OUT_PBR_DIR, f"{prefix}_AO.png"))
    safe_save_png(msk_img, os.path.join(OUT_PBR_DIR, f"{prefix}_Mask.png"))
    print(f"[OK] {prefix} <- {os.path.basename(alb_path)}" + (f" + {os.path.basename(ns_path)}" if ns_path else ""))
    return True


def main():
    # 1. Walls & Pillars: Authentic Gold & Glory medieval dungeon ashlar stone brick
    export_set(
        "T_GothicStone",
        [
            r"CommonDyn\Texture\TEX_M01_DC_Brick_01_C.tga",
            r"CommonDyn\Texture\TEX_Common_Wall_01_C.tga",
            r"Art_Map401\Texture_1024\TEX_Common_Stone_02_C.tga",
        ],
        [
            r"CommonDyn\Texture\TEX_M01_DC_Brick_01_NS.tga",
            r"CommonDyn\Texture\TEX_Common_wall_01_NS.tga",
        ],
        brightness=0.88,
        desat=0.20,
        contrast=1.12,
        metallic=0.04,
        smoothness=0.24,
    )

    # 2. Dungeon Floors: Authentic Gold & Glory dark weathered stone flagstones (zero bullseye rings!)
    export_set(
        "T_CathedralFloor",
        [
            r"Art_Map01\Texture\TEX_CT02_Floor_01_C.tga",
            r"Art_Map01\Texture_1024\TEX_Beginner01_Stone_01_C.tga",
            r"CommonDyn\Texture_1024\TEX_Common_StoneTabletl_01_C.tga",
        ],
        [
            r"CommonDyn\Texture_1024\TEX_Common_StoneTabletl_01_NS.tga",
            r"CommonDyn\Texture\TEX_M01_DC_Brick_01_NS.tga",
        ],
        brightness=0.76,
        desat=0.25,
        contrast=1.10,
        metallic=0.05,
        smoothness=0.30,
    )

    # 3. Mossy / Ancient Ruin Floors & Walls
    export_set(
        "T_MossyRuins",
        [
            r"Art_Map01\Texture_1024\TEX_Beginner01_Stone_01_C.tga",
            r"CommonDyn\Texture\TEX_Common_Wall_01_C.tga",
            r"Art_Map401\Texture_1024\TEX_Common_Stone_02_C.tga",
        ],
        [
            r"CommonDyn\Texture\TEX_Common_wall_01_NS.tga",
        ],
        brightness=0.80,
        desat=0.14,
        contrast=1.10,
        metallic=0.03,
        smoothness=0.32,
    )

    # 4. Aged Dungeon Oak Wood (desaturated dark smoked timber — never bright orange!)
    export_set(
        "T_AgedOakWood",
        [
            r"Art_Map401\Texture_1024\TEX_Common_Wood_02_C.tga",
            r"CommonDyn\Texture\TEX_Common_Wood_01_C.tga",
        ],
        [
            r"Art_Map401\Texture_1024\TEX_Common_Wood_03_NS.tga",
            r"CommonDyn\Texture\TEX_Common_Wood_01_NS.tga",
        ],
        brightness=0.66,
        desat=0.38,
        contrast=1.08,
        metallic=0.02,
        smoothness=0.22,
    )

    # Also update legacy stone_floor.png / stone_wall.png if referenced anywhere
    fl = os.path.join(OUT_PBR_DIR, "T_CathedralFloor_Albedo.png")
    wl = os.path.join(OUT_PBR_DIR, "T_GothicStone_Albedo.png")
    env_root = os.path.join(PROJECT_ROOT, "Assets", "Resources", "Environment")
    if os.path.isfile(fl):
        safe_save_png(Image.open(fl), os.path.join(env_root, "stone_floor.png"))
    if os.path.isfile(wl):
        safe_save_png(Image.open(wl), os.path.join(env_root, "stone_wall.png"))


if __name__ == "__main__":
    main()

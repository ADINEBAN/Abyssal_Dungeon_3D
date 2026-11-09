import os
import math
import shutil
import hashlib
from PIL import Image, ImageDraw, ImageFilter

PROJECT_ROOT = r"c:\LTG\Abyssal_Dungeon_3D"
EXPORTED_VFX_TEX = os.path.join(PROJECT_ROOT, "ExportedProject", "Assets", "ART", "Effect", "Texture")
OUT_DIR = os.path.join(PROJECT_ROOT, "Assets", "Resources", "VFX", "Textures")


def make_guid(seed_str: str) -> str:
    return hashlib.md5(seed_str.encode("utf-8")).hexdigest()


def write_texture_meta(tex_path: str, wrap_mode: int = 1):
    """
    wrap_mode: 1 = Clamp, 0 = Repeat
    """
    meta_path = tex_path + ".meta"
    if os.path.exists(meta_path):
        return
    rel = os.path.relpath(tex_path, PROJECT_ROOT).replace("\\", "/")
    guid = make_guid("abyssal_vfx_tex::" + rel)
    content = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 12
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMasterTextureLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 1024
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 4
    mipBias: 0
    wrapU: {wrap_mode}
    wrapV: {wrap_mode}
    wrapW: {wrap_mode}
  nPOTScale: 1
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 1024
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(content)


def ensure_folder_meta(folder_path: str):
    meta_path = folder_path + ".meta"
    if os.path.exists(meta_path):
        return
    rel = os.path.relpath(folder_path, PROJECT_ROOT).replace("\\", "/")
    guid = make_guid("abyssal_vfx_folder::" + rel)
    content = f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(content)


def copy_exported_vfx_textures():
    mapping = [
        (os.path.join(EXPORTED_VFX_TEX, "blade", "m_vfx_blade_1303.png"), "T_Vfx_BladeSlash.png"),
        (os.path.join(EXPORTED_VFX_TEX, "blade", "m_vfx_blade_1304.png"), "T_Vfx_BladeWave.png"),
        (os.path.join(EXPORTED_VFX_TEX, "ring", "T_vfx_ring_001.png"), "T_Vfx_ShockRing.png"),
        (os.path.join(EXPORTED_VFX_TEX, "ring", "T_vfx_ring_011.png"), "T_Vfx_RadialBurstRing.png"),
        (os.path.join(EXPORTED_VFX_TEX, "floor", "T_vfx_floor_806.png"), "T_Vfx_GroundCrack.png"),
        (os.path.join(EXPORTED_VFX_TEX, "floor", "T_vfx_floor_001.png"), "T_Vfx_ShatterLines.png"),
        (os.path.join(EXPORTED_VFX_TEX, "floor", "T_vfx_floor_004.png"), "T_Vfx_FrostFlower.png"),
        (os.path.join(EXPORTED_VFX_TEX, "glow", "T_vfx_glow_001.png"), "T_Vfx_StarFlare.png"),
        (os.path.join(EXPORTED_VFX_TEX, "lighting", "T_vfx_lighting_003.png"), "T_Vfx_Lightning.png"),
        (os.path.join(EXPORTED_VFX_TEX, "trail", "T_vfx_trail_015.png"), "T_Vfx_EnergyTrail.png"),
    ]
    for src, dst_name in mapping:
        if os.path.exists(src):
            dst = os.path.join(OUT_DIR, dst_name)
            # Load via PIL and convert luminance/alpha so black backgrounds become clean alpha + white RGB for tinting
            im = Image.open(src).convert("RGBA")
            w, h = im.size
            px = im.load()
            out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
            opx = out.load()
            cx, cy = (w - 1) * 0.5, (h - 1) * 0.5
            max_r = min(cx, cy)
            for y in range(h):
                for x in range(w):
                    r, g, b, a = px[x, y]
                    lum = max(r, g, b) / 255.0
                    alpha_in = a / 255.0
                    # Edge vignette so no texture ever touches the quad border
                    dx = (x - cx) / max(1.0, cx)
                    dy = (y - cy) / max(1.0, cy)
                    rad = math.sqrt(dx * dx + dy * dy)
                    edge_fade = max(0.0, min(1.0, (0.96 - rad) / 0.18))
                    # Combine luminance and alpha so black-background additive textures work in both alpha & additive modes
                    val = lum * alpha_in * edge_fade
                    out_a = int(max(0, min(255, (val ** 0.85) * 255.0)))
                    out_c = int(max(0, min(255, min(1.0, val * 1.35) * 255.0)))
                    opx[x, y] = (out_c, out_c, out_c, out_a)
            out.save(dst)
            write_texture_meta(dst, wrap_mode=1)


def build_gothic_rune_circle_texture():
    """
    Generates a 512x512 intricate Ancient Gothic Magic Circle texture with:
    - Outer double runic ring + 24 carved rune glyphs
    - Inscribed sacred octagram (two 4-point Gothic stars)
    - 8 ornate satellite sigil medallions at the star tips
    - Inner arcane rose & quatrefoil core
    - 100% zero alpha outside radius 0.92 so a quad NEVER shows a box!
    """
    size = 512
    im = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(im)
    cx, cy = size // 2, size // 2

    def circle_outline(r, width=3, intensity=255):
        draw.ellipse((cx - r, cy - r, cx + r, cy + r), outline=intensity, width=width)

    # Outer concentric rings
    circle_outline(222, width=4, intensity=245)
    circle_outline(210, width=2, intensity=190)
    circle_outline(182, width=4, intensity=250)
    circle_outline(172, width=2, intensity=185)
    circle_outline(116, width=3, intensity=230)
    circle_outline(64, width=3, intensity=240)

    # 24 Ancient Gothic Rune Glyphs between r=185 and r=207
    for i in range(24):
        ang = i * (2.0 * math.pi / 24.0)
        r_mid = 196.0
        gx = cx + math.cos(ang) * r_mid
        gy = cy + math.sin(ang) * r_mid
        tang = ang + math.pi * 0.5
        # Radial stem
        x0 = cx + math.cos(ang) * 186.0
        y0 = cy + math.sin(ang) * 186.0
        x1 = cx + math.cos(ang) * 206.0
        y1 = cy + math.sin(ang) * 206.0
        draw.line([(x0, y0), (x1, y1)], fill=235, width=2)
        # Diagonal rune branches
        bx = gx + math.cos(tang) * 7.0
        by = gy + math.sin(tang) * 7.0
        draw.line([(x0, y0), (bx, by)], fill=215, width=2)
        if i % 2 == 0:
            bx2 = gx - math.cos(tang) * 7.0
            by2 = gy - math.sin(tang) * 7.0
            draw.line([(x1, y1), (bx2, by2)], fill=215, width=2)

    # Sacred Octagram (Two interlocking squares / 8-pointed Gothic star)
    for offset_deg in (0.0, 45.0):
        pts = []
        for k in range(4):
            a = math.radians(offset_deg + k * 90.0)
            pts.append((cx + math.cos(a) * 172.0, cy + math.sin(a) * 172.0))
        pts.append(pts[0])
        draw.line(pts, fill=240, width=3)

    # 8 Satellite Medallions & Gothic Spokes
    for i in range(8):
        a = math.radians(i * 45.0)
        x_in = cx + math.cos(a) * 64.0
        y_in = cy + math.sin(a) * 64.0
        x_out = cx + math.cos(a) * 172.0
        y_out = cy + math.sin(a) * 172.0
        draw.line([(x_in, y_in), (x_out, y_out)], fill=210, width=2)

        mx = cx + math.cos(a) * 172.0
        my = cy + math.sin(a) * 172.0
        draw.ellipse((mx - 14, my - 14, mx + 14, my + 14), fill=40, outline=255, width=3)
        draw.ellipse((mx - 5, my - 5, mx + 5, my + 5), fill=255)

    # Inner Quatrefoil / Gothic Rose
    for i in range(8):
        a = math.radians(i * 45.0 + 22.5)
        rx = cx + math.cos(a) * 38.0
        ry = cy + math.sin(a) * 38.0
        draw.ellipse((rx - 24, ry - 24, rx + 24, ry + 24), outline=210, width=2)

    draw.ellipse((cx - 16, cy - 16, cx + 16, cy + 16), fill=255)

    # Create soft glow layer + crisp line layer
    glow = im.filter(ImageFilter.GaussianBlur(radius=6.0))
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    px_line = im.load()
    px_glow = glow.load()
    opx = out.load()

    for y in range(size):
        dy = (y - cy) / 256.0
        for x in range(size):
            dx = (x - cx) / 256.0
            r = math.sqrt(dx * dx + dy * dy)
            if r >= 0.95:
                continue
            # Soft radial aura + crisp lines
            radial_glow = max(0.0, 1.0 - (r / 0.92)) ** 1.8 * 0.28
            ring_band = math.exp(-((r - 0.72) ** 2) / 0.008) * 0.35
            line_v = px_line[x, y] / 255.0
            glow_v = px_glow[x, y] / 255.0
            edge_mask = max(0.0, min(1.0, (0.94 - r) / 0.08))

            val = min(1.0, (line_v * 0.95 + glow_v * 0.65 + radial_glow + ring_band) * edge_mask)
            c = int(max(0, min(255, min(1.0, val * 1.25) * 255.0)))
            a = int(max(0, min(255, (val ** 0.85) * 255.0)))
            opx[x, y] = (c, c, c, a)

    dst = os.path.join(OUT_DIR, "T_Vfx_GothicRuneCircle.png")
    out.save(dst)
    write_texture_meta(dst, wrap_mode=1)


def build_slash_crescent_texture():
    """
    Generates a 512x256 UV-mapped slash texture (u: 0 tail -> 1 head, v: 0 inner -> 1 outer razor tip)
    with fiery energy streaks, calligraphic blade core, and razor edge.
    """
    w, h = 512, 256
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    opx = out.load()

    for y in range(h):
        v = y / float(h - 1)  # 0 = inner hilt, 1 = outer blade tip
        for x in range(w):
            u = x / float(w - 1)  # 0 = tail, 1 = leading blade head

            # Taper along u (smooth tail fade, sharp curved head)
            head_fade = max(0.0, min(1.0, (0.98 - u) / 0.08))
            tail_fade = max(0.0, min(1.0, (u - 0.02) / 0.55)) ** 1.35
            u_mask = head_fade * tail_fade

            # Radial profile across v:
            # Bright razor edge near v=0.82..0.93, secondary inner energy filament at v=0.45..0.62
            outer_cut = max(0.0, min(1.0, (0.97 - v) / 0.07))
            inner_cut = max(0.0, min(1.0, (v - 0.05) / 0.35))

            razor = math.exp(-((v - 0.85) ** 2) / 0.0045)
            sub_blade = math.exp(-((v - 0.56) ** 2) / 0.012) * 0.72
            flame_wave = (
                0.5 + 0.5 * math.sin(u * 28.0 - v * 9.0 + math.sin(u * 11.0) * 2.2)
            ) * (0.5 + 0.5 * math.cos(u * 42.0 + v * 14.0))

            body = inner_cut * outer_cut * (0.28 + 0.52 * flame_wave)
            val = min(1.0, u_mask * outer_cut * (razor * 1.15 + sub_blade + body * 0.65))

            c = int(max(0, min(255, min(1.0, val * 1.3) * 255.0)))
            a = int(max(0, min(255, (val ** 0.82) * 255.0)))
            opx[x, y] = (c, c, c, a)

    dst = os.path.join(OUT_DIR, "T_Vfx_SlashCrescent.png")
    out.save(dst)
    write_texture_meta(dst, wrap_mode=1)


def build_nova_wave_texture():
    """
    Generates a 512x512 radial starburst + concentric shockwave texture for explosive skill impacts.
    """
    size = 512
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    opx = out.load()
    cx, cy = (size - 1) * 0.5, (size - 1) * 0.5

    for y in range(size):
        dy = (y - cy) / cx
        for x in range(size):
            dx = (x - cx) / cx
            r = math.sqrt(dx * dx + dy * dy)
            if r >= 0.96:
                continue
            ang = math.atan2(dy, dx)

            # Outer shockwave rim + inner secondary rim
            rim1 = math.exp(-((r - 0.78) ** 2) / 0.0055)
            rim2 = math.exp(-((r - 0.52) ** 2) / 0.008) * 0.65
            core = math.exp(-(r * r) / 0.06) * 0.9

            # 16 sharp starburst rays + 32 fine filaments
            rays16 = (0.5 + 0.5 * math.cos(ang * 16.0 + r * 4.0)) ** 4.0
            rays8 = (0.5 + 0.5 * math.cos(ang * 8.0 - r * 2.5)) ** 6.0
            ray_mask = max(0.0, 1.0 - r / 0.92) * (rays16 * 0.55 + rays8 * 0.65)

            edge_fade = max(0.0, min(1.0, (0.95 - r) / 0.08))
            val = min(1.0, (rim1 * 1.1 + rim2 + core + ray_mask * 0.75) * edge_fade)

            c = int(max(0, min(255, min(1.0, val * 1.25) * 255.0)))
            a = int(max(0, min(255, (val ** 0.85) * 255.0)))
            opx[x, y] = (c, c, c, a)

    dst = os.path.join(OUT_DIR, "T_Vfx_NovaWave.png")
    out.save(dst)
    write_texture_meta(dst, wrap_mode=1)


def build_pillar_beam_texture():
    """
    Generates a 256x512 vertical energy pillar texture with soft horizontal & vertical fade.
    """
    w, h = 256, 512
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    opx = out.load()

    for y in range(h):
        v = y / float(h - 1)  # 0 bottom -> 1 top
        # Strong base, smooth fade toward top, zero at extreme ends
        bot_fade = max(0.0, min(1.0, v / 0.08))
        top_fade = max(0.0, min(1.0, (0.98 - v) / 0.55)) ** 1.3
        v_mask = bot_fade * top_fade

        for x in range(w):
            u = (x / float(w - 1)) * 2.0 - 1.0  # -1..1 across width
            ax = abs(u)
            if ax >= 0.96:
                continue
            core = math.exp(-(ax * ax) / 0.08)
            outer = math.exp(-(ax * ax) / 0.36) * 0.55
            streaks = 0.7 + 0.3 * math.sin(u * 18.0 + v * 22.0) * math.cos(u * 9.0 - v * 14.0)
            h_fade = max(0.0, min(1.0, (0.95 - ax) / 0.12))

            val = min(1.0, v_mask * h_fade * (core * 1.1 + outer * streaks))
            c = int(max(0, min(255, min(1.0, val * 1.3) * 255.0)))
            a = int(max(0, min(255, (val ** 0.85) * 255.0)))
            opx[x, y] = (c, c, c, a)

    dst = os.path.join(OUT_DIR, "T_Vfx_PillarBeam.png")
    out.save(dst)
    write_texture_meta(dst, wrap_mode=1)


def main():
    vfx_root = os.path.join(PROJECT_ROOT, "Assets", "Resources", "VFX")
    os.makedirs(OUT_DIR, exist_ok=True)
    ensure_folder_meta(vfx_root)
    ensure_folder_meta(OUT_DIR)

    copy_exported_vfx_textures()
    build_gothic_rune_circle_texture()
    build_slash_crescent_texture()
    build_nova_wave_texture()
    build_pillar_beam_texture()
    print("Successfully built all Skill VFX textures in", OUT_DIR)


if __name__ == "__main__":
    main()


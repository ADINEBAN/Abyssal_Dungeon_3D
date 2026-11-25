import os
import math
import hashlib
from PIL import Image, ImageDraw, ImageFilter

PROJECT_ROOT = r"c:\LTG\Abyssal_Dungeon_3D"
OUT_DIR = os.path.join(PROJECT_ROOT, "Assets", "Resources", "VFX", "Textures")


def make_guid(seed_str: str) -> str:
    return hashlib.md5(seed_str.encode("utf-8")).hexdigest()


def write_texture_meta(tex_path: str, wrap_mode: int = 1):
    meta_path = tex_path + ".meta"
    if os.path.exists(meta_path):
        return
    rel = os.path.relpath(tex_path, PROJECT_ROOT).replace("\\", "/")
    guid = make_guid("abyssal_env_vfx_tex::" + rel)
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
    with open(meta_path, "w", encoding="utf-8") as f:
        f.write(content)


def smoothstep(e0: float, e1: float, x: float) -> float:
    t = max(0.0, min(1.0, (x - e0) / max(1e-6, e1 - e0)))
    return t * t * (3.0 - 2.0 * t)


def hash2d(ix: int, iy: int, seed: int = 1337) -> float:
    n = ix * 374761393 + iy * 668265263 + seed * 1442695041
    n = (n ^ (n >> 13)) * 1274126177
    n = n ^ (n >> 16)
    return ((n & 0x7FFFFFFF) / 2147483647.0)


def value_noise(x: float, y: float, seed: int = 1337) -> float:
    ix = math.floor(x)
    iy = math.floor(y)
    fx = x - ix
    fy = y - iy
    ux = fx * fx * (3.0 - 2.0 * fx)
    uy = fy * fy * (3.0 - 2.0 * fy)
    a = hash2d(ix, iy, seed)
    b = hash2d(ix + 1, iy, seed)
    c = hash2d(ix, iy + 1, seed)
    d = hash2d(ix + 1, iy + 1, seed)
    return (a + (b - a) * ux) + ((c + (d - c) * ux) - (a + (b - a) * ux)) * uy


def fbm(x: float, y: float, octaves: int = 4, seed: int = 1337) -> float:
    val = 0.0
    amp = 0.5
    freq = 1.0
    norm = 0.0
    for o in range(octaves):
        val += amp * value_noise(x * freq, y * freq, seed + o * 97)
        norm += amp
        amp *= 0.5
        freq *= 2.03
    return val / max(1e-6, norm)


def build_gothic_smoke():
    sz = 256
    img = Image.new("RGBA", (sz, sz), (0, 0, 0, 0))
    px = img.load()
    for y in range(sz):
        ny = (y / (sz - 1)) * 2.0 - 1.0
        for x in range(sz):
            nx = (x / (sz - 1)) * 2.0 - 1.0
            r = math.hypot(nx, ny)
            if r >= 1.0:
                continue
            swirl_amp = 0.65 * smoothstep(0.0, 0.35, r)
            swirl_x = nx * 3.1 + (-ny * 0.85 + nx * 0.25) * swirl_amp
            swirl_y = ny * 3.1 + (nx * 0.85 + ny * 0.25) * swirl_amp
            warp = fbm(nx * 2.2 + 1.7, ny * 2.2 + 9.1, octaves=3, seed=313) - 0.5
            n = fbm(swirl_x + warp * 1.1 + 4.2, swirl_y + warp * 1.1 + 7.5, octaves=4, seed=401)
            mask = (1.0 - smoothstep(0.32, 0.94, r))
            billow = smoothstep(0.24, 0.86, n * 0.85 + (1.0 - r) * 0.42)
            a = int(max(0.0, min(1.0, mask * billow)) * 255)
            shade = int(195 + n * 60)
            px[x, y] = (shade, shade, shade, a)
    out = os.path.join(OUT_DIR, "T_Vfx_GothicSmoke.png")
    img.save(out)
    write_texture_meta(out, wrap_mode=1)


def build_ground_fog_wisp():
    sz = 256
    img = Image.new("RGBA", (sz, sz), (0, 0, 0, 0))
    px = img.load()
    for y in range(sz):
        ny = (y / (sz - 1)) * 2.0 - 1.0
        for x in range(sz):
            nx = (x / (sz - 1)) * 2.0 - 1.0
            ell = math.hypot(nx * 0.92, ny * 1.15)
            if ell >= 1.0:
                continue
            n = fbm(nx * 2.6 + 11.3, ny * 3.4 + 2.7, octaves=4, seed=709)
            radial = (1.0 - smoothstep(0.20, 0.96, ell)) ** 1.4
            wisp = smoothstep(0.28, 0.88, n * 0.72 + (1.0 - ell) * 0.45)
            a = int(max(0.0, min(1.0, radial * wisp)) * 235)
            px[x, y] = (240, 240, 245, a)
    out = os.path.join(OUT_DIR, "T_Vfx_GroundFogWisp.png")
    img.save(out)
    write_texture_meta(out, wrap_mode=1)


def build_ember_spark():
    sz = 128
    img = Image.new("RGBA", (sz, sz), (0, 0, 0, 0))
    px = img.load()
    for y in range(sz):
        ny = (y / (sz - 1)) * 2.0 - 1.0
        for x in range(sz):
            nx = (x / (sz - 1)) * 2.0 - 1.0
            d = math.hypot(nx * 1.35, ny * 0.88)
            if d >= 1.0:
                continue
            core = max(0.0, 1.0 - d)
            glow = (core ** 2.6) * 0.75 + (core ** 6.0) * 0.65
            a = int(max(0.0, min(1.0, glow)) * 255)
            px[x, y] = (255, 255, 255, a)
    out = os.path.join(OUT_DIR, "T_Vfx_EmberSpark.png")
    img.save(out)
    write_texture_meta(out, wrap_mode=1)


def build_ash_flake():
    sz = 128
    img = Image.new("RGBA", (sz, sz), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    pts = [
        (36, 22), (76, 18), (104, 46), (94, 88), (62, 108), (28, 92), (20, 52)
    ]
    draw.polygon(pts, fill=(245, 240, 235, 235))
    img = img.filter(ImageFilter.GaussianBlur(radius=2.2))
    out = os.path.join(OUT_DIR, "T_Vfx_AshFlake.png")
    img.save(out)
    write_texture_meta(out, wrap_mode=1)


def build_flame_wisp():
    sz = 256
    img = Image.new("RGBA", (sz, sz), (0, 0, 0, 0))
    px = img.load()
    for y in range(sz):
        # PIL y=0 is top (flame tip), y=sz-1 is bottom (flame base)
        v = 1.0 - (y / (sz - 1))
        for x in range(sz):
            u = x / (sz - 1)
            nx = (u - 0.5) * 2.0
            sway = math.sin(v * 7.5 + 0.8) * 0.14 * v
            n = fbm((u + sway) * 4.5 + 3.1, v * 5.0 - 1.4, octaves=4, seed=911)
            half_w = (1.0 - v * 0.78) * (0.58 + (n - 0.5) * 0.45)
            dx = abs(nx - sway)
            radial = 1.0 - smoothstep(half_w * 0.15, max(0.06, half_w), dx)
            vert = smoothstep(0.02, 0.18, v) * (1.0 - smoothstep(0.55, 0.99, v))
            val = max(0.0, min(1.0, radial * vert * (0.65 + n * 0.75)))
            px[x, y] = (255, 255, 255, int(val * 255))
    out = os.path.join(OUT_DIR, "T_Vfx_FlameWisp.png")
    img.save(out)
    write_texture_meta(out, wrap_mode=1)


def build_runes_sheet():
    # 512x512 2x2 flipbook sheet (4 crisp Ancient Gothic Runes)
    sz = 512
    half = sz // 2
    img = Image.new("RGBA", (sz, sz), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    def glyph_tile(gx: int, gy: int, kind: int):
        cx = gx * half + half // 2
        cy = gy * half + half // 2
        R = 84
        c = (255, 255, 255, 255)
        w = 7
        # Outer thin diamond frame for cohesive gothic iconography
        if kind == 0:
            # Obsidian Crown / Spear Rune
            draw.line([(cx, cy - R), (cx, cy + R)], fill=c, width=w)
            draw.line([(cx - 46, cy - 22), (cx, cy - R), (cx + 46, cy - 22)], fill=c, width=w)
            draw.line([(cx - 38, cy + 28), (cx, cy - 12), (cx + 38, cy + 28)], fill=c, width=w)
            draw.line([(cx - 26, cy + 68), (cx + 26, cy + 68)], fill=c, width=w)
        elif kind == 1:
            # Triple-Notch Ward Glyph
            draw.arc([cx - 64, cy - 64, cx + 64, cy + 64], 35, 325, fill=c, width=w)
            draw.polygon([(cx, cy - 54), (cx + 38, cy), (cx, cy + 54), (cx - 38, cy)], outline=c, width=w - 1)
            draw.line([(cx - 52, cy), (cx + 52, cy)], fill=c, width=5)
        elif kind == 2:
            # Abyssal Bifurcated Cross Rune
            draw.line([(cx, cy - R), (cx, cy + R)], fill=c, width=w)
            draw.line([(cx - 56, cy - 36), (cx + 56, cy - 36)], fill=c, width=w)
            draw.line([(cx - 56, cy - 36), (cx - 56, cy - 8)], fill=c, width=5)
            draw.line([(cx + 56, cy - 36), (cx + 56, cy - 8)], fill=c, width=5)
            draw.line([(cx - 36, cy + 44), (cx, cy + 8), (cx + 36, cy + 44)], fill=c, width=w)
        else:
            # Crescent Chalice / Eye Seal
            draw.polygon([(cx, cy - R + 8), (cx + 58, cy + 58), (cx - 58, cy + 58)], outline=c, width=w)
            draw.ellipse([cx - 24, cy - 6, cx + 24, cy + 42], outline=c, width=5)
            draw.line([(cx, cy - R + 8), (cx, cy + 72)], fill=c, width=5)

    glyph_tile(0, 0, 0)
    glyph_tile(1, 0, 1)
    glyph_tile(0, 1, 2)
    glyph_tile(1, 1, 3)

    # Soft outer glow halo + crisp core composite
    glow = img.filter(ImageFilter.GaussianBlur(radius=4.5))
    comp = Image.alpha_composite(glow, img)
    out = os.path.join(OUT_DIR, "T_Vfx_RunesSheet.png")
    comp.save(out)
    write_texture_meta(out, wrap_mode=1)


if __name__ == "__main__":
    os.makedirs(OUT_DIR, exist_ok=True)
    build_gothic_smoke()
    build_ground_fog_wisp()
    build_ember_spark()
    build_ash_flake()
    build_flame_wisp()
    build_runes_sheet()
    print("Generated 6 Environment VFX textures cleanly in", OUT_DIR)

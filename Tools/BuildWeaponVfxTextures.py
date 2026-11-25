import os
import math
import hashlib
from PIL import Image, ImageDraw, ImageFilter

PROJECT_ROOT = r"c:\LTG\Abyssal_Dungeon_3D"
TEX_DIR = os.path.join(PROJECT_ROOT, "Assets", "Resources", "VFX", "Textures")
MESH_DIR = os.path.join(PROJECT_ROOT, "Assets", "Resources", "VFX", "Meshes")
SHADER_PATH = os.path.join(PROJECT_ROOT, "Assets", "Resources", "Shaders", "AbyssalWeaponAura.shader")

WEAPON_MESHES = [
    "SM_Vfx_WpnSlashCrescent.fbx",
    "SM_Vfx_WpnHeavyCleaveWave.fbx",
    "SM_Vfx_WpnSpinWhirlwind.fbx",
    "SM_Vfx_WpnThrustSpearCone.fbx",
    "SM_Vfx_WpnArrowBolt.fbx",
    "SM_Vfx_WpnSpellBoltCore.fbx",
    "SM_Vfx_WpnShieldAegis.fbx",
    "SM_Vfx_WpnBladeAuraSheath.fbx",
]


def make_guid(seed_str: str) -> str:
    return hashlib.md5(seed_str.encode("utf-8")).hexdigest()


def write_texture_meta(tex_path: str, wrap_mode: int = 1):
    meta_path = tex_path + ".meta"
    if os.path.exists(meta_path):
        return
    rel = os.path.relpath(tex_path, PROJECT_ROOT).replace("\\", "/")
    guid = make_guid("abyssal_wpn_tex::" + rel)
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


def write_fbx_meta(fbx_path: str):
    meta_path = fbx_path + ".meta"
    if os.path.exists(meta_path):
        return
    rel = os.path.relpath(fbx_path, PROJECT_ROOT).replace("\\", "/")
    guid = make_guid("abyssal_wpn_mesh::" + rel)
    content = f"""fileFormatVersion: 2
guid: {guid}
ModelImporter:
  serializedVersion: 21300
  internalIDToNameTable: []
  externalObjects: {{}}
  materials:
    materialImportMode: 0
    materialName: 0
    materialSearch: 1
    materialLocation: 1
  animations:
    legacyGenerateAnimations: 4
    bakeSimulation: 0
    resampleCurves: 1
    optimizeGameObjects: 0
    removeConstantScaleCurves: 1
    motionNodeName: 
    rigImportErrors: 
    rigImportWarnings: 
    animationImportErrors: 
    animationImportWarnings: 
    animationRetargetingWarnings: 
    animationDoRetargetingWarnings: 0
    importAnimatedCustomProperties: 0
    importConstraints: 0
    animationCompression: 1
    animationRotationError: 0.5
    animationPositionError: 0.5
    animationScaleError: 0.5
    animationWrapMode: 0
    extraExposedTransformPaths: []
    extraUserProperties: []
    clipAnimations: []
    isReadable: 1
  meshes:
    lODScreenPercentages: []
    globalScale: 1
    meshCompression: 2
    addColliders: 0
    useSRGBMaterialColor: 1
    sortHierarchyByName: 1
    importVisibility: 1
    importBlendShapes: 1
    importCameras: 0
    importLights: 0
    nodeNameCollisionStrategy: 1
    fileIdsGeneration: 2
    swapUVChannels: 0
    generateSecondaryUV: 0
    useFileUnits: 1
    keepQuads: 0
    weldVertices: 1
    bakeAxisConversion: 0
    preserveHierarchy: 0
    skinWeightsMode: 0
    maxBonesPerVertex: 4
    minBoneWeight: 0.001
    optimizeBones: 1
    meshOptimizationFlags: -1
    indexFormat: 0
    secondaryUVAngleDistortion: 8
    secondaryUVAreaDistortion: 15.000001
    secondaryUVHardAngle: 88
    secondaryUVMarginMethod: 1
    secondaryUVMinLightmapResolution: 40
    secondaryUVMinObjectScale: 1
    secondaryUVPackMargin: 4
    useFileScale: 1
  tangentSpace:
    normalSmoothAngle: 60
    normalImportMode: 0
    tangentImportMode: 3
    normalCalculationMode: 4
    legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes: 0
    blendShapeNormalImportMode: 1
    normalSmoothingSource: 0
  referencedClips: []
  importAnimation: 0
  humanDescription:
    serializedVersion: 3
    human: []
    skeleton: []
    armTwist: 0.5
    foreArmTwist: 0.5
    upperLegTwist: 0.5
    legTwist: 0.5
    armStretch: 0.05
    legStretch: 0.05
    feetSpacing: 0
    globalScale: 1
    rootMotionBoneName: 
    hasTranslationDoF: 0
    hasExtraRoot: 0
    skeletonHasParents: 1
  lastHumanDescriptionAvatarSource: {{instanceID: 0}}
  autoGenerateAvatarMappingIfUnspecified: 1
  animationType: 0
  humanoidOversampling: 1
  avatarSetup: 0
  addHumanoidExtraRootOnlyWhenUsingAvatar: 1
  remapMaterialsIfMaterialImportModeIsNone: 0
  additionalBone: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(content)


def write_shader_meta(shader_path: str):
    meta_path = shader_path + ".meta"
    if os.path.exists(meta_path):
        return
    rel = os.path.relpath(shader_path, PROJECT_ROOT).replace("\\", "/")
    guid = make_guid("abyssal_wpn_shader::" + rel)
    content = f"""fileFormatVersion: 2
guid: {guid}
ShaderImporter:
  externalObjects: {{}}
  defaultTextures: []
  nonModifiableTextures: []
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(content)


def build_wpn_rune_streak():
    """
    512x256 seamless scrolling runic blade energy streak (U seamless Repeat, V feathered 0->1).
    Used on SM_Vfx_WpnBladeAuraSheath, SM_Vfx_WpnArrowBolt, SM_Vfx_WpnSpellBoltCore, and WeaponBladeTrail3D.
    """
    w, h = 512, 256
    line_im = Image.new("L", (w, h), 0)
    draw = ImageDraw.Draw(line_im)

    # Carved Gothic elder runes repeating seamlessly every 64px along U
    for i in range(8):
        cx = i * 64 + 32
        cy = 152
        draw.line([(cx, cy - 26), (cx, cy + 26)], fill=235, width=3)
        if i % 2 == 0:
            draw.line([(cx - 14, cy - 10), (cx, cy - 24), (cx + 14, cy - 10)], fill=215, width=2)
            draw.line([(cx - 10, cy + 8), (cx + 10, cy - 6)], fill=200, width=2)
        else:
            draw.polygon([(cx, cy - 20), (cx + 13, cy), (cx, cy + 20), (cx - 13, cy)], outline=220, width=2)

    glow = line_im.filter(ImageFilter.GaussianBlur(radius=4.5))
    px_l = line_im.load()
    px_g = glow.load()

    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    opx = out.load()

    for y in range(h):
        v = y / float(h - 1)
        v_in = max(0.0, min(1.0, (v - 0.04) / 0.28))
        v_out = max(0.0, min(1.0, (0.97 - v) / 0.12))
        razor = math.exp(-((v - 0.82) ** 2) / 0.008)
        spine = math.exp(-((v - 0.58) ** 2) / 0.025) * 0.65

        for x in range(w):
            u = x / float(w - 1)
            # Periodic waves (multiples of 2*pi for exact horizontal seamless tiling!)
            w1 = 0.5 + 0.5 * math.sin(u * 2.0 * math.pi * 3.0 + v * 5.0)
            w2 = 0.5 + 0.5 * math.cos(u * 2.0 * math.pi * 7.0 - v * 8.0)
            rune_v = (px_l[x, y] / 255.0) * 0.85 + (px_g[x, y] / 255.0) * 0.55

            val = min(1.0, v_in * v_out * (razor * (0.75 + 0.45 * w1) + spine * (0.55 + 0.45 * w2) + rune_v * 0.72))
            c = int(max(0, min(255, min(1.0, val * 1.28) * 255.0)))
            a = int(max(0, min(255, (val ** 0.84) * 255.0)))
            opx[x, y] = (c, c, c, a)

    dst = os.path.join(TEX_DIR, "T_Vfx_WpnRuneStreak.png")
    out.save(dst)
    write_texture_meta(dst, wrap_mode=0)


def build_wpn_serrated_arc():
    """
    512x256 multi-filament heavy cleave & whirlwind arc texture (U=0 tail -> U=1 razor head, V=0 inner -> V=1 edge).
    """
    w, h = 512, 256
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    opx = out.load()

    for y in range(h):
        v = y / float(h - 1)
        outer_fade = max(0.0, min(1.0, (0.98 - v) / 0.06))
        inner_fade = max(0.0, min(1.0, (v - 0.04) / 0.26))
        crest1 = math.exp(-((v - 0.88) ** 2) / 0.0038)
        crest2 = math.exp(-((v - 0.66) ** 2) / 0.0075) * 0.82
        crest3 = math.exp(-((v - 0.42) ** 2) / 0.0120) * 0.48

        for x in range(w):
            u = x / float(w - 1)
            head_fade = max(0.0, min(1.0, (0.985 - u) / 0.065))
            tail_fade = max(0.0, min(1.0, (u - 0.015) / 0.48)) ** 1.25
            u_env = head_fade * tail_fade

            jag = (0.5 + 0.5 * math.sin(u * 38.0 - v * 14.0)) ** 1.6
            streaks = 0.62 + 0.38 * math.cos(u * 54.0 + v * 9.0)

            val = min(1.0, u_env * outer_fade * inner_fade * ((crest1 * 1.2 + crest2 * streaks + crest3 * jag) + 0.24 * jag))
            c = int(max(0, min(255, min(1.0, val * 1.32) * 255.0)))
            a = int(max(0, min(255, (val ** 0.80) * 255.0)))
            opx[x, y] = (c, c, c, a)

    dst = os.path.join(TEX_DIR, "T_Vfx_WpnSerratedArc.png")
    out.save(dst)
    write_texture_meta(dst, wrap_mode=1)


def build_wpn_aegis_hex():
    """
    512x512 Ancient Gothic Sacred Octagonal/Hexagonal Aegis Shield Lattice with zero-alpha circular border.
    """
    size = 512
    im = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(im)
    cx, cy = size // 2, size // 2

    # Sacred gothic octagon border + inner ward rings
    for rad, wd, val in ((226, 5, 255), (212, 2, 195), (156, 3, 225), (84, 3, 235)):
        pts = []
        for k in range(8):
            ang = math.radians(k * 45.0 + 22.5)
            pts.append((cx + math.cos(ang) * rad, cy + math.sin(ang) * rad))
        pts.append(pts[0])
        draw.line(pts, fill=val, width=wd)

    # Radial Gothic ribs + crossbars
    for k in range(8):
        ang = math.radians(k * 45.0 + 22.5)
        x0, y0 = cx + math.cos(ang) * 42, cy + math.sin(ang) * 42
        x1, y1 = cx + math.cos(ang) * 226, cy + math.sin(ang) * 226
        draw.line([(x0, y0), (x1, y1)], fill=215, width=2)

    # Subtle hexagonal ward cells inside
    hex_r = 34.0
    dx_step = hex_r * 1.5
    dy_step = hex_r * math.sqrt(3.0)
    for gx in range(-6, 7):
        for gy in range(-6, 7):
            hx = cx + gx * dx_step
            hy = cy + (gy + (0.5 if gx % 2 else 0.0)) * dy_step
            dist = math.hypot(hx - cx, hy - cy)
            if dist > 205:
                continue
            hpts = []
            for m in range(6):
                ma = math.radians(m * 60.0)
                hpts.append((hx + math.cos(ma) * (hex_r * 0.92), hy + math.sin(ma) * (hex_r * 0.92)))
            hpts.append(hpts[0])
            draw.line(hpts, fill=115, width=1)

    draw.ellipse((cx - 22, cy - 22, cx + 22, cy + 22), outline=255, width=3)
    glow = im.filter(ImageFilter.GaussianBlur(radius=5.0))
    px_l = im.load()
    px_g = glow.load()

    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    opx = out.load()
    for y in range(size):
        dy = (y - cy) / 256.0
        for x in range(size):
            dx = (x - cx) / 256.0
            r = math.hypot(dx, dy)
            if r >= 0.95:
                continue
            edge = max(0.0, min(1.0, (0.94 - r) / 0.08))
            rim_glow = math.exp(-((r - 0.84) ** 2) / 0.008) * 0.45
            lv = px_l[x, y] / 255.0
            gv = px_g[x, y] / 255.0
            val = min(1.0, (lv * 0.90 + gv * 0.65 + rim_glow + 0.12 * (1.0 - r)) * edge)
            c = int(max(0, min(255, min(1.0, val * 1.25) * 255.0)))
            a = int(max(0, min(255, (val ** 0.85) * 255.0)))
            opx[x, y] = (c, c, c, a)

    dst = os.path.join(TEX_DIR, "T_Vfx_WpnAegisHex.png")
    out.save(dst)
    write_texture_meta(dst, wrap_mode=1)


def main():
    os.makedirs(TEX_DIR, exist_ok=True)
    os.makedirs(MESH_DIR, exist_ok=True)
    build_wpn_rune_streak()
    build_wpn_serrated_arc()
    build_wpn_aegis_hex()

    for fbx_name in WEAPON_MESHES:
        fbx_p = os.path.join(MESH_DIR, fbx_name)
        if os.path.exists(fbx_p):
            write_fbx_meta(fbx_p)

    if os.path.exists(SHADER_PATH):
        write_shader_meta(SHADER_PATH)

    print("Successfully generated 3 Weapon VFX textures + 8 FBX .meta + Shader .meta!")


if __name__ == "__main__":
    main()

import os
import shutil
import hashlib
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageEnhance

ROOT = r"c:\LTG\Abyssal_Dungeon_3D"
EXP_UI = os.path.join(ROOT, "ExportedProject", "Assets", "AssetBox", "ResFolder", "UI")
EXP_FONT = os.path.join(ROOT, "ExportedProject", "Assets", "Resources", "UI", "Font")
RES_UI = os.path.join(ROOT, "Assets", "Resources", "UI")


def deterministic_guid(rel_path: str) -> str:
    h = hashlib.md5(("AbyssalGothicUI_v1:" + rel_path.replace("\\", "/").lower()).encode("utf-8")).hexdigest()
    return h[:32]


def ensure_folder_meta(folder_path: str):
    os.makedirs(folder_path, exist_ok=True)
    meta_path = folder_path + ".meta"
    if not os.path.exists(meta_path):
        rel = os.path.relpath(folder_path, ROOT)
        guid = deterministic_guid("folder:" + rel)
        with open(meta_path, "w", encoding="utf-8", newline="\n") as f:
            f.write(
                f"fileFormatVersion: 2\n"
                f"guid: {guid}\n"
                f"folderAsset: yes\n"
                f"DefaultImporter:\n"
                f"  externalObjects: {{}}\n"
                f"  userData: \n"
                f"  assetBundleName: \n"
                f"  assetBundleVariant: \n"
            )


def write_sprite_meta(png_path: str, border=(0, 0, 0, 0)):
    """
    border = (left, bottom, right, top) in pixels for 9-slicing.
    Preserves existing GUID if .meta already exists.
    """
    meta_path = png_path + ".meta"
    rel = os.path.relpath(png_path, ROOT)
    guid = deterministic_guid(rel)
    if os.path.exists(meta_path):
        with open(meta_path, "r", encoding="utf-8", errors="ignore") as f:
            for line in f:
                if line.startswith("guid:"):
                    old = line.split(":", 1)[1].strip()
                    if len(old) == 32:
                        guid = old
                    break

    bx, by, bz, bw = border
    content = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 12
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
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
  isReadable: 1
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMasterTextureLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: {bx}, y: {by}, z: {bz}, w: {bw}}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  spritePackingTag: 
  pSDRemoveMatte: 0
  pSDShowRemoveMatteOption: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(content)


def write_font_meta(ttf_path: str):
    meta_path = ttf_path + ".meta"
    rel = os.path.relpath(ttf_path, ROOT)
    guid = deterministic_guid(rel)
    if os.path.exists(meta_path):
        with open(meta_path, "r", encoding="utf-8", errors="ignore") as f:
            for line in f:
                if line.startswith("guid:"):
                    old = line.split(":", 1)[1].strip()
                    if len(old) == 32:
                        guid = old
                    break
    content = f"""fileFormatVersion: 2
guid: {guid}
TrueTypeFontImporter:
  serializedVersion: 4
  externalObjects: {{}}
  fontSize: 18
  forceTextureCase: -2
  characterSpacing: 0
  characterPadding: 1
  includeFontData: 1
  fontNames: []
  fallbackFontReferences: []
  customCharacters: 
  fontRenderingMode: 0
  output:
    hasEmptyFontData: 0
  ascentCalculationMode: 1
  useLegacyBoundsCalculation: 0
  shouldRoundAdvanceValue: 1
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(content)


def save_ui_png(img: Image.Image, path: str, border=(0, 0, 0, 0)):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path, "PNG")
    write_sprite_meta(path, border)


def setup_fonts():
    font_dir = os.path.join(RES_UI, "Fonts")
    ensure_folder_meta(font_dir)
    win_fonts = r"C:\Windows\Fonts"

    # Title Font: Classical engraved serif with 100% Vietnamese Unicode support
    title_candidates = [
        os.path.join(win_fonts, "palab.ttf"),     # Palatino Linotype Bold (Roman serif like Cinzel + full Vietnamese)
        os.path.join(win_fonts, "georgiab.ttf"),  # Georgia Bold (Full Vietnamese)
        os.path.join(win_fonts, "timesbd.ttf"),   # Times New Roman Bold
        os.path.join(EXP_FONT, "ASUL-FF-BOLD.ttf"),
    ]
    for c in title_candidates:
        if os.path.exists(c):
            dst = os.path.join(font_dir, "GothicTitle.ttf")
            shutil.copy2(c, dst)
            write_font_meta(dst)
            break

    # Body Font: Clean humanist sans-serif with 100% Vietnamese Unicode support
    body_candidates = [
        os.path.join(win_fonts, "segoeuib.ttf"),
        os.path.join(win_fonts, "segoeui.ttf"),
        os.path.join(win_fonts, "arialbd.ttf"),
        os.path.join(win_fonts, "arial.ttf"),
    ]
    for c in body_candidates:
        if os.path.exists(c):
            dst = os.path.join(font_dir, "GothicBody.ttf")
            shutil.copy2(c, dst)
            write_font_meta(dst)
            break

    # Also copy ExportedProject ASUL-FF-BOLD.ttf as AncientDisplay.ttf
    exp_asul = os.path.join(EXP_FONT, "ASUL-FF-BOLD.ttf")
    if os.path.exists(exp_asul):
        dst = os.path.join(font_dir, "AncientDisplay.ttf")
        shutil.copy2(exp_asul, dst)
        write_font_meta(dst)


def make_noise_field(w: int, h: int, seed: int = 42, scale: float = 0.08) -> np.ndarray:
    rng = np.random.RandomState(seed)
    small_w = max(8, int(w * scale))
    small_h = max(8, int(h * scale))
    base = rng.uniform(0.0, 1.0, (small_h, small_w)).astype(np.float32)
    pil_n = Image.fromarray((base * 255).astype(np.uint8)).resize((w, h), Image.Resampling.BICUBIC)
    fine = rng.uniform(-0.15, 0.15, (h, w)).astype(np.float32)
    arr = np.clip(np.asarray(pil_n, dtype=np.float32) / 255.0 + fine, 0.0, 1.0)
    return arr


def draw_corner_filigree(draw: ImageDraw.ImageDraw, w: int, h: int, margin: int, col_gold, col_bright, ornate_level: int = 2):
    """Draws symmetrical Gothic corner ornaments and rivets in all 4 corners."""
    corners = [
        (margin, margin, 1, 1),
        (w - 1 - margin, margin, -1, 1),
        (margin, h - 1 - margin, 1, -1),
        (w - 1 - margin, h - 1 - margin, -1, -1),
    ]
    arm = 18 if ornate_level == 1 else (28 if ornate_level == 2 else 36)
    for cx, cy, sx, sy in corners:
        # Outer L bracket
        draw.line([(cx, cy), (cx + sx * arm, cy)], fill=col_gold, width=2)
        draw.line([(cx, cy), (cx, cy + sy * arm)], fill=col_gold, width=2)
        # Inner stepped gothic filigree line
        c2x, c2y = cx + sx * 4, cy + sy * 4
        draw.line([(c2x, c2y), (c2x + sx * (arm - 6), c2y)], fill=col_bright, width=1)
        draw.line([(c2x, c2y), (c2x, c2y + sy * (arm - 6))], fill=col_bright, width=1)
        # Diagonal brace / gothic diamond
        if ornate_level >= 2:
            draw.line([(cx + sx * 12, cy), (cx, cy + sy * 12)], fill=col_gold, width=2)
            draw.line([(cx + sx * 18, cy + sy * 4), (cx + sx * 4, cy + sy * 18)], fill=col_bright, width=1)
        if ornate_level >= 3:
            # Extra gothic trefoil / cross motif near corner
            px, py = cx + sx * 10, cy + sy * 10
            draw.polygon([(px, py - 4), (px + 4, py), (px, py + 4), (px - 4, py)], fill=col_bright, outline=col_gold)
        # Corner rivet stud
        rx, ry = cx + sx * 6, cy + sy * 6
        draw.ellipse([rx - 2, ry - 2, rx + 2, ry + 2], fill=col_bright, outline=col_gold)


def build_gothic_panel(
    size: int,
    bg_top,
    bg_bot,
    outer_rim,
    mid_gold,
    inner_bronze,
    highlight_gold,
    ornate_level: int = 2,
    texture_kind: str = "leather",
    alpha: int = 246
) -> Image.Image:
    w = h = size
    noise = make_noise_field(w, h, seed=101 + ornate_level * 17, scale=0.12)
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    ny = yy / float(h - 1)
    nx = xx / float(w - 1)

    # Radial vignette for ancient depth
    vignette = 1.0 - 0.32 * np.clip(np.sqrt((nx - 0.5) ** 2 + (ny - 0.5) ** 2) * 1.35, 0.0, 1.0)

    arr = np.zeros((h, w, 4), dtype=np.float32)
    for c in range(3):
        grad = bg_top[c] * (1.0 - ny) + bg_bot[c] * ny
        tex_mod = (noise - 0.5) * (18.0 if texture_kind == "leather" else 24.0)
        if texture_kind == "grid_iron":
            grid = ((np.sin(xx * (2 * math.pi / 32.0)) ** 2) + (np.sin(yy * (2 * math.pi / 32.0)) ** 2)) * 4.5
            tex_mod += grid - 4.5
        elif texture_kind == "runic":
            dist = np.sqrt((xx - w * 0.5) ** 2 + (yy - h * 0.5) ** 2)
            ring = np.exp(-((dist - w * 0.33) ** 2) / 18.0) * 18.0 + np.exp(-((dist - w * 0.24) ** 2) / 12.0) * 12.0
            tex_mod += ring
        arr[:, :, c] = np.clip((grad + tex_mod) * vignette, 0, 255)
    arr[:, :, 3] = alpha

    img = Image.fromarray(arr.astype(np.uint8))
    draw = ImageDraw.Draw(img)

    # 1. Outer Dark Iron Shadow & Bevel (0..3px)
    draw.rectangle([0, 0, w - 1, h - 1], outline=(10, 7, 5, 255), width=2)
    draw.rectangle([2, 2, w - 3, h - 3], outline=outer_rim + (255,), width=2)

    # 2. Primary Antique Gold / Bronze Frame (4..6px)
    draw.rectangle([4, 4, w - 5, h - 5], outline=mid_gold + (255,), width=2)
    # Top edge specular bevel
    draw.line([(5, 5), (w - 6, 5)], fill=highlight_gold + (255,), width=1)

    # 3. Recessed Dark Channel (7..10px)
    draw.rectangle([7, 7, w - 8, h - 8], outline=(16, 11, 8, 255), width=3)

    # 4. Inner Engraved Bronze Hairline (11px)
    draw.rectangle([11, 11, w - 12, h - 12], outline=inner_bronze + (235,), width=1)

    if ornate_level >= 2:
        draw.rectangle([14, 14, w - 15, h - 15], outline=(inner_bronze[0] // 2, inner_bronze[1] // 2, inner_bronze[2] // 2, 180), width=1)

    # 5. Corner Gothic Filigree & Rivets (inside the 32px 9-slice border zone)
    draw_corner_filigree(draw, w, h, 5, mid_gold + (255,), highlight_gold + (255,), ornate_level=ornate_level)

    return img


def build_all_panels():
    pdir = os.path.join(RES_UI, "Panels")
    ensure_folder_meta(pdir)
    b9 = (32, 32, 32, 32)

    # 1. UI_Panel_Standard: Dark Forged Iron & Warm Umber + Double Bronze Border
    p_std = build_gothic_panel(
        256,
        bg_top=(34, 25, 18), bg_bot=(20, 14, 10),
        outer_rim=(58, 43, 30), mid_gold=(162, 118, 58),
        inner_bronze=(112, 80, 40), highlight_gold=(218, 178, 96),
        ornate_level=1, texture_kind="leather", alpha=244
    )
    save_ui_png(p_std, os.path.join(pdir, "UI_Panel_Standard.png"), b9)

    # 2. UI_Panel_Ornate: Antique Gold Filigree & Deep Espresso Leather
    p_orn = build_gothic_panel(
        256,
        bg_top=(42, 29, 20), bg_bot=(22, 15, 10),
        outer_rim=(76, 54, 32), mid_gold=(214, 174, 86),
        inner_bronze=(154, 112, 52), highlight_gold=(248, 218, 130),
        ornate_level=3, texture_kind="leather", alpha=248
    )
    save_ui_png(p_orn, os.path.join(pdir, "UI_Panel_Ornate.png"), b9)

    # 3. UI_Panel_Inventory: Forged Dark Iron Grid Reliquary
    p_inv = build_gothic_panel(
        256,
        bg_top=(28, 21, 16), bg_bot=(16, 12, 9),
        outer_rim=(64, 48, 34), mid_gold=(188, 146, 72),
        inner_bronze=(130, 94, 46), highlight_gold=(235, 198, 112),
        ornate_level=2, texture_kind="grid_iron", alpha=248
    )
    save_ui_png(p_inv, os.path.join(pdir, "UI_Panel_Inventory.png"), b9)

    # 4. UI_Panel_Character: Warm Aged Parchment-Leather & Burnished Gold
    p_chr = build_gothic_panel(
        256,
        bg_top=(46, 33, 22), bg_bot=(26, 18, 12),
        outer_rim=(78, 56, 34), mid_gold=(206, 164, 78),
        inner_bronze=(146, 106, 50), highlight_gold=(244, 212, 124),
        ornate_level=3, texture_kind="leather", alpha=248
    )
    save_ui_png(p_chr, os.path.join(pdir, "UI_Panel_Character.png"), b9)

    # 5. UI_Panel_SkillTree: Ancient Obsidian & Engraved Rune Tablet
    p_skl = build_gothic_panel(
        256,
        bg_top=(26, 19, 14), bg_bot=(14, 10, 8),
        outer_rim=(68, 50, 32), mid_gold=(198, 156, 74),
        inner_bronze=(138, 98, 44), highlight_gold=(242, 208, 118),
        ornate_level=3, texture_kind="runic", alpha=250
    )
    save_ui_png(p_skl, os.path.join(pdir, "UI_Panel_SkillTree.png"), b9)

    # 6. UI_Panel_Lobby: Gothic Heraldic Command Panel
    p_lob = build_gothic_panel(
        256,
        bg_top=(38, 27, 19), bg_bot=(20, 14, 10),
        outer_rim=(70, 50, 32), mid_gold=(192, 150, 72),
        inner_bronze=(134, 96, 46), highlight_gold=(236, 200, 112),
        ornate_level=2, texture_kind="leather", alpha=246
    )
    save_ui_png(p_lob, os.path.join(pdir, "UI_Panel_Lobby.png"), b9)

    # 7. UI_Panel_Notification: Ornate Relic Scroll Banner
    p_not = build_gothic_panel(
        256,
        bg_top=(48, 34, 22), bg_bot=(28, 19, 12),
        outer_rim=(82, 58, 34), mid_gold=(218, 178, 88),
        inner_bronze=(158, 114, 52), highlight_gold=(250, 222, 136),
        ornate_level=2, texture_kind="leather", alpha=242
    )
    save_ui_png(p_not, os.path.join(pdir, "UI_Panel_Notification.png"), b9)

    # 8. UI_Panel_Dialog: Heavy Ancient Tome / Modal Frame
    p_dlg = build_gothic_panel(
        256,
        bg_top=(32, 22, 15), bg_bot=(16, 11, 8),
        outer_rim=(84, 60, 36), mid_gold=(222, 182, 92),
        inner_bronze=(164, 118, 56), highlight_gold=(252, 226, 142),
        ornate_level=3, texture_kind="leather", alpha=252
    )
    save_ui_png(p_dlg, os.path.join(pdir, "UI_Panel_Dialog.png"), b9)

    # Also update legacy Assets/Resources/UI/panel_frame.png so any legacy call gets the ornate gothic frame
    save_ui_png(p_orn, os.path.join(RES_UI, "panel_frame.png"), b9)


def build_gothic_button(
    w: int,
    h: int,
    top_col,
    bot_col,
    border_col,
    highlight_col,
    inner_col,
    ornate: bool = True,
    rune_glow: bool = False,
    tab_arch: bool = False
) -> Image.Image:
    noise = make_noise_field(w, h, seed=303, scale=0.18)
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    ny = yy / float(h - 1)
    nx = xx / float(w - 1)

    # Metallic horizontal brushed sheen + vertical bevel
    sheen = np.exp(-((ny - 0.22) ** 2) / 0.045) * 22.0
    edge_dark = 1.0 - 0.22 * (np.abs(nx - 0.5) * 2.0) ** 2

    arr = np.zeros((h, w, 4), dtype=np.float32)
    for c in range(3):
        grad = top_col[c] * (1.0 - ny) + bot_col[c] * ny
        tex = (noise - 0.5) * 14.0
        arr[:, :, c] = np.clip((grad + sheen + tex) * edge_dark, 0, 255)
    arr[:, :, 3] = 250

    img = Image.fromarray(arr.astype(np.uint8))
    draw = ImageDraw.Draw(img)

    # Outer dark iron bevel
    draw.rectangle([0, 0, w - 1, h - 1], outline=(10, 7, 5, 255), width=2)
    # Main metallic border
    draw.rectangle([2, 2, w - 3, h - 3], outline=border_col + (255,), width=2)
    # Top specular highlight
    draw.line([(4, 3), (w - 5, 3)], fill=highlight_col + (255,), width=1)
    # Inner dark groove & inner hairline
    draw.rectangle([4, 4, w - 5, h - 5], outline=(18, 12, 8, 240), width=2)
    draw.rectangle([6, 6, w - 7, h - 7], outline=inner_col + (220,), width=1)

    if tab_arch:
        # Highlighted top arch bar for tabs
        draw.line([(6, 2), (w - 7, 2)], fill=highlight_col + (255,), width=2)
        draw.line([(2, h - 2), (w - 3, h - 2)], fill=(12, 9, 6, 255), width=3)

    if ornate:
        draw_corner_filigree(draw, w, h, 3, border_col + (255,), highlight_col + (255,), ornate_level=2)

    if rune_glow:
        # Engraved glowing diamond runes on left & right ends
        for cx in (16, w - 17):
            cy = h // 2
            pts = [(cx, cy - 8), (cx + 8, cy), (cx, cy + 8), (cx - 8, cy)]
            draw.polygon(pts, fill=highlight_col + (240,), outline=border_col + (255,))
            draw.ellipse([cx - 2, cy - 2, cx + 2, cy + 2], fill=(255, 250, 210, 255))

    return img


def build_all_buttons():
    bdir = os.path.join(RES_UI, "Buttons")
    ensure_folder_meta(bdir)
    b9 = (24, 20, 24, 20)

    # 1. Primary Button: Burnished Antique Gold & Bronze
    b_pri = build_gothic_button(
        256, 80,
        top_col=(118, 82, 34), bot_col=(62, 41, 16),
        border_col=(224, 184, 92), highlight_col=(252, 228, 148),
        inner_col=(168, 126, 58), ornate=True, rune_glow=False
    )
    save_ui_png(b_pri, os.path.join(bdir, "Btn_Primary.png"), b9)

    # 2. Secondary Button: Forged Dark Iron & Warm Umber with Bronze Rim
    b_sec = build_gothic_button(
        256, 80,
        top_col=(52, 38, 27), bot_col=(28, 20, 14),
        border_col=(164, 122, 62), highlight_col=(216, 176, 98),
        inner_col=(112, 82, 42), ornate=True, rune_glow=False
    )
    save_ui_png(b_sec, os.path.join(bdir, "Btn_Secondary.png"), b9)

    # 3. Danger Button: Deep Blood-Crimson & Antique Bronze
    b_dng = build_gothic_button(
        256, 80,
        top_col=(104, 26, 24), bot_col=(52, 12, 12),
        border_col=(208, 112, 74), highlight_col=(246, 168, 128),
        inner_col=(148, 62, 46), ornate=True, rune_glow=False
    )
    save_ui_png(b_dng, os.path.join(bdir, "Btn_Danger.png"), b9)

    # 4. Disabled Button: Weathered Dark Iron
    b_dis = build_gothic_button(
        256, 80,
        top_col=(34, 30, 27), bot_col=(22, 19, 17),
        border_col=(82, 72, 62), highlight_col=(108, 96, 84),
        inner_col=(58, 50, 44), ornate=False, rune_glow=False
    )
    save_ui_png(b_dis, os.path.join(bdir, "Btn_Disabled.png"), b9)

    # 5. Selected Button: Illuminated Golden Rune Plate
    b_sel = build_gothic_button(
        256, 80,
        top_col=(142, 100, 38), bot_col=(78, 52, 18),
        border_col=(246, 212, 114), highlight_col=(255, 242, 178),
        inner_col=(198, 154, 72), ornate=True, rune_glow=True
    )
    save_ui_png(b_sel, os.path.join(bdir, "Btn_Selected.png"), b9)

    # 6. Tab Button: Gothic Arch Header Plate
    b_tab = build_gothic_button(
        256, 80,
        top_col=(64, 46, 30), bot_col=(32, 22, 15),
        border_col=(188, 144, 72), highlight_col=(238, 202, 118),
        inner_col=(132, 96, 48), ornate=True, rune_glow=False, tab_arch=True
    )
    save_ui_png(b_tab, os.path.join(bdir, "Btn_Tab.png"), b9)


def build_decor_and_bars():
    ddir = os.path.join(RES_UI, "Decor")
    ensure_folder_meta(ddir)

    # 1. Divider_Gothic_Gold.png (512x28)
    w, h = 512, 28
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    cy = h // 2
    cx = w // 2
    for x in range(16, w - 16):
        dist = abs(x - cx) / float(cx - 16)
        alpha = int(max(0.0, (1.0 - dist ** 1.6)) * 240)
        draw.point((x, cy), fill=(214, 174, 86, alpha))
        draw.point((x, cy - 1), fill=(248, 218, 130, int(alpha * 0.75)))
        draw.point((x, cy + 1), fill=(120, 84, 38, int(alpha * 0.85)))
    # Center gothic diamond & side flourishes
    draw.polygon([(cx, cy - 9), (cx + 14, cy), (cx, cy + 9), (cx - 14, cy)], fill=(48, 32, 18, 255), outline=(238, 200, 108, 255))
    draw.polygon([(cx, cy - 5), (cx + 8, cy), (cx, cy + 5), (cx - 8, cy)], fill=(246, 214, 122, 255))
    for sx in (-1, 1):
        px = cx + sx * 26
        draw.polygon([(px, cy - 5), (px + 6, cy), (px, cy + 5), (px - 6, cy)], fill=(196, 152, 72, 255), outline=(244, 212, 124, 255))
        px2 = cx + sx * 44
        draw.ellipse([px2 - 2, cy - 2, px2 + 2, cy + 2], fill=(238, 200, 108, 255))
    save_ui_png(img, os.path.join(ddir, "Divider_Gothic_Gold.png"))

    # 2. Bar_Frame_Gothic.png (256x36, 9-sliced)
    bw, bh = 256, 36
    bimg = Image.new("RGBA", (bw, bh), (14, 10, 8, 238))
    bdraw = ImageDraw.Draw(bimg)
    bdraw.rectangle([0, 0, bw - 1, bh - 1], outline=(10, 7, 5, 255), width=1)
    bdraw.rectangle([1, 1, bw - 2, bh - 2], outline=(172, 130, 64, 255), width=2)
    bdraw.line([(3, 2), (bw - 4, 2)], fill=(232, 194, 106, 220), width=1)
    bdraw.rectangle([3, 3, bw - 4, bh - 4], outline=(24, 17, 12, 255), width=2)
    save_ui_png(bimg, os.path.join(ddir, "Bar_Frame_Gothic.png"), (12, 8, 12, 8))

    # 3. Bar_Fill_Gothic.png (128x28)
    fw, fh = 128, 28
    farr = np.zeros((fh, fw, 4), dtype=np.uint8)
    for y in range(fh):
        ny = y / float(fh - 1)
        # Bright top bevel fading to rich body and dark bottom edge
        if ny < 0.30:
            v = int(225 + (1.0 - ny / 0.30) * 30)
        elif ny < 0.80:
            v = int(220 - (ny - 0.30) * 90)
        else:
            v = int(165 - (ny - 0.80) * 180)
        farr[y, :, 0] = v
        farr[y, :, 1] = v
        farr[y, :, 2] = v
        farr[y, :, 3] = 255
    # Subtle vertical engraved tick marks every 16px
    for x in range(15, fw, 16):
        farr[:, x, :3] = (farr[:, x, :3].astype(np.int32) * 88 // 100).astype(np.uint8)
    save_ui_png(Image.fromarray(farr), os.path.join(ddir, "Bar_Fill_Gothic.png"))

    # 4. Slot_Frame_Gothic.png (128x128, 9-sliced)
    slot = build_gothic_panel(
        128,
        bg_top=(24, 18, 13), bg_bot=(14, 10, 7),
        outer_rim=(62, 46, 30), mid_gold=(186, 144, 70),
        inner_bronze=(126, 90, 44), highlight_gold=(236, 198, 110),
        ornate_level=2, texture_kind="leather", alpha=248
    )
    save_ui_png(slot, os.path.join(ddir, "Slot_Frame_Gothic.png"), (20, 20, 20, 20))

    # 5. Pedestal_Crest_Gold.png (256x256 runic circle emblem)
    cs = 256
    cimg = Image.new("RGBA", (cs, cs), (0, 0, 0, 0))
    cdraw = ImageDraw.Draw(cimg)
    cc = cs // 2
    cdraw.ellipse([8, 8, cs - 9, cs - 9], outline=(196, 152, 72, 180), width=3)
    cdraw.ellipse([18, 18, cs - 19, cs - 19], outline=(238, 200, 108, 210), width=2)
    cdraw.ellipse([44, 44, cs - 45, cs - 45], outline=(164, 120, 56, 160), width=2)
    for deg in range(0, 360, 15):
        rad = math.radians(deg)
        r1 = 100 if deg % 45 == 0 else 108
        r2 = 118
        x1, y1 = cc + int(math.cos(rad) * r1), cc + int(math.sin(rad) * r1)
        x2, y2 = cc + int(math.cos(rad) * r2), cc + int(math.sin(rad) * r2)
        cdraw.line([(x1, y1), (x2, y2)], fill=(238, 200, 108, 200), width=2 if deg % 45 == 0 else 1)
    # 8-pointed gothic star
    for rot in (0, 45):
        pts = []
        for i in range(8):
            ang = math.radians(rot + i * 45)
            r = 84 if i % 2 == 0 else 34
            pts.append((cc + int(math.cos(ang) * r), cc + int(math.sin(ang) * r)))
        cdraw.polygon(pts, outline=(214, 174, 86, 150))
    save_ui_png(cimg, os.path.join(ddir, "Pedestal_Crest_Gold.png"))


def make_medallion_base(size: int = 96, tint_bg=(32, 22, 15), rim_col=(214, 174, 86)) -> Image.Image:
    """Creates a borderless radial-glow icon canvas (no inner square/rounded-rect box frame)."""
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    px = img.load()
    cx, cy = (size - 1) * 0.5, (size - 1) * 0.5
    max_r = size * 0.46
    for y in range(size):
        for x in range(size):
            d = math.hypot(x - cx, y - cy) / max_r
            if d < 1.0:
                falloff = (1.0 - d * d) ** 1.6
                r = max(0, min(255, int(tint_bg[0] * 0.65 + rim_col[0] * 0.35 * falloff)))
                g = max(0, min(255, int(tint_bg[1] * 0.65 + rim_col[1] * 0.35 * falloff)))
                b = max(0, min(255, int(tint_bg[2] * 0.65 + rim_col[2] * 0.35 * falloff)))
                a = max(0, min(255, int(165 * falloff)))
                px[x, y] = (r, g, b, a)
    return img


def tint_and_frame_exported_icon(src_png: str, size: int = 96, rim_col=(214, 174, 86), bg_col=(32, 22, 15)) -> Image.Image:
    base = make_medallion_base(size, bg_col, rim_col)
    if os.path.exists(src_png):
        try:
            ic = Image.open(src_png).convert("RGBA")
            inner = size - 10
            ic = ic.resize((inner, inner), Image.Resampling.LANCZOS)
            base.alpha_composite(ic, (5, 5))
            return base
        except Exception:
            pass
    return base


def draw_engraved_icon(name: str, accent=(238, 200, 108), bg=(32, 22, 15), rim=(214, 174, 86)) -> Image.Image:
    s = 96
    img = make_medallion_base(s, bg, rim)
    d = ImageDraw.Draw(img)
    cx, cy = s // 2, s // 2
    hi = (255, 238, 175, 255)
    ac = accent + (255,)
    dk = (92, 64, 30, 255)

    if name == "sword":
        # Diagonal medieval broadsword
        d.polygon([(24, 72), (30, 66), (72, 20), (76, 20), (76, 24), (34, 70)], fill=hi, outline=ac)
        d.line([(28, 68), (73, 23)], fill=ac, width=2)
        # Crossguard
        d.line([(22, 52), (44, 74)], fill=ac, width=5)
        d.line([(22, 52), (44, 74)], fill=hi, width=2)
        # Pommel
        d.ellipse([17, 71, 25, 79], fill=ac, outline=hi)
    elif name == "crossed_swords":
        for sx in (-1, 1):
            x0 = cx - sx * 24
            x1 = cx + sx * 24
            d.line([(x0, 70), (x1, 22)], fill=hi, width=5)
            d.line([(x0, 70), (x1, 22)], fill=ac, width=2)
            d.line([(cx - sx * 10, 62), (cx - sx * 24, 48)], fill=ac, width=4)
        d.polygon([(cx, 26), (cx + 6, 34), (cx, 42), (cx - 6, 34)], fill=hi, outline=ac)
    elif name == "shield":
        pts = [(cx, 18), (70, 26), (66, 56), (cx, 78), (30, 56), (26, 26)]
        d.polygon(pts, fill=(58, 42, 24, 255), outline=hi)
        pts2 = [(cx, 23), (64, 30), (61, 53), (cx, 72), (35, 53), (32, 30)]
        d.polygon(pts2, fill=dk, outline=ac)
        d.line([(cx, 24), (cx, 70)], fill=hi, width=3)
        d.line([(34, 45), (62, 45)], fill=hi, width=3)
    elif name == "armor":
        pts = [(30, 22), (40, 20), (cx, 28), (56, 20), (66, 22), (74, 36), (64, 44), (62, 74), (cx, 78), (34, 74), (32, 44), (22, 36)]
        d.polygon(pts, fill=dk, outline=hi)
        d.line([(cx, 28), (cx, 76)], fill=hi, width=2)
        d.arc([34, 30, 62, 54], 0, 180, fill=ac, width=2)
    elif name == "helm":
        d.pieslice([26, 20, 70, 68], 180, 360, fill=dk, outline=hi)
        d.rectangle([26, 42, 70, 72], fill=dk, outline=hi, width=2)
        # Visor T-slit
        d.rectangle([34, 46, 62, 51], fill=(14, 10, 8, 255), outline=ac)
        d.rectangle([45, 46, 51, 70], fill=(14, 10, 8, 255), outline=ac)
        d.line([(cx, 16), (cx, 44)], fill=hi, width=3)
    elif name == "ring":
        d.ellipse([26, 30, 70, 74], outline=hi, width=6)
        d.ellipse([28, 32, 68, 72], outline=ac, width=2)
        # Top gem
        d.polygon([(cx, 16), (cx + 10, 26), (cx, 36), (cx - 10, 26)], fill=(220, 55, 55, 255), outline=hi)
    elif name == "amulet":
        d.arc([26, 14, 70, 56], 0, 180, fill=ac, width=2)
        d.polygon([(cx, 32), (64, 52), (cx, 76), (32, 52)], fill=dk, outline=hi)
        d.polygon([(cx, 38), (58, 52), (cx, 68), (38, 52)], fill=(70, 165, 235, 255), outline=ac)
    elif name == "dodge":
        # Winged greave / boot
        pts = [(34, 24), (52, 24), (52, 54), (70, 62), (72, 72), (30, 72), (30, 48)]
        d.polygon(pts, fill=dk, outline=hi)
        # Golden wing
        d.polygon([(52, 36), (76, 24), (68, 38), (78, 38), (54, 48)], fill=hi, outline=ac)
    elif name == "gold":
        d.ellipse([22, 22, 74, 74], fill=(168, 122, 38, 255), outline=hi, width=3)
        d.ellipse([28, 28, 68, 68], outline=hi, width=2)
        d.polygon([(cx, 34), (cx + 12, cy), (cx, 62), (cx - 12, cy)], fill=hi, outline=dk)
    elif name == "exp":
        # Radiant ancient star / tome
        for rot in (0, 45):
            pts = []
            for i in range(8):
                ang = math.radians(rot + i * 45)
                r = 28 if i % 2 == 0 else 11
                pts.append((cx + int(math.cos(ang) * r), cy + int(math.sin(ang) * r)))
            d.polygon(pts, fill=ac if rot == 0 else hi, outline=hi)
    elif name == "skull_boss":
        # Horned Abyssal Skull
        d.polygon([(28, 38), (14, 18), (24, 24), (36, 30)], fill=ac, outline=hi)
        d.polygon([(68, 38), (82, 18), (72, 24), (60, 30)], fill=ac, outline=hi)
        d.ellipse([28, 26, 68, 64], fill=dk, outline=hi, width=2)
        d.rectangle([36, 56, 60, 74], fill=dk, outline=hi, width=2)
        # Glowing crimson eyes
        d.ellipse([34, 40, 44, 50], fill=(235, 45, 45, 255), outline=hi)
        d.ellipse([52, 40, 62, 50], fill=(235, 45, 45, 255), outline=hi)
        for tx in (41, 48, 55):
            d.line([(tx, 60), (tx, 72)], fill=hi, width=2)
    elif name == "check_seal":
        # Wax/Gold Seal with engraved checkmark
        d.ellipse([18, 18, 78, 78], fill=(42, 92, 46, 255), outline=hi, width=3)
        d.ellipse([23, 23, 73, 73], outline=ac, width=1)
        d.line([(32, 49), (44, 61), (65, 35)], fill=hi, width=5)
    elif name == "lock_iron":
        d.arc([32, 18, 64, 50], 180, 360, fill=hi, width=5)
        d.rounded_rectangle([26, 42, 70, 76], radius=6, fill=dk, outline=hi, width=2)
        d.ellipse([43, 52, 53, 62], fill=(16, 12, 9, 255), outline=ac)
        d.polygon([(46, 60), (50, 60), (52, 70), (44, 70)], fill=(16, 12, 9, 255))
    elif name == "crown":
        pts = [(20, 66), (24, 34), (38, 48), (cx, 24), (58, 48), (72, 34), (76, 66)]
        d.polygon(pts, fill=ac, outline=hi)
        d.rectangle([20, 66, 76, 73], fill=dk, outline=hi)
    elif name == "anvil":
        pts = [(18, 40), (34, 36), (72, 36), (78, 44), (62, 50), (58, 64), (70, 72), (26, 72), (38, 64), (34, 50)]
        d.polygon(pts, fill=dk, outline=hi)
        d.line([(24, 26), (56, 26)], fill=hi, width=4)
    elif name == "portal":
        d.arc([22, 16, 74, 78], 180, 360, fill=hi, width=4)
        d.rectangle([22, 46, 30, 76], fill=dk, outline=hi)
        d.rectangle([66, 46, 74, 76], fill=dk, outline=hi)
        d.ellipse([32, 28, 64, 72], fill=(64, 145, 225, 210), outline=ac, width=2)
    elif name == "bag":
        d.rounded_rectangle([26, 32, 70, 74], radius=10, fill=dk, outline=hi, width=2)
        d.arc([36, 20, 60, 38], 180, 360, fill=hi, width=3)
        d.line([(26, 46), (70, 46)], fill=ac, width=2)
        d.rectangle([43, 42, 53, 52], fill=hi, outline=dk)
    elif name == "skill_tree":
        # Rune constellation
        nodes = [(cx, 22), (26, 48), (70, 48), (cx, 74), (cx, 48)]
        for n in nodes[:4]:
            d.line([nodes[4], n], fill=ac, width=2)
        for nx, ny in nodes:
            d.polygon([(nx, ny - 7), (nx + 7, ny), (nx, ny + 7), (nx - 7, ny)], fill=hi, outline=dk)
    elif name == "settings":
        d.ellipse([26, 26, 70, 70], outline=hi, width=4)
        d.ellipse([38, 38, 58, 58], fill=dk, outline=ac, width=2)
        for deg in range(0, 360, 45):
            r = math.radians(deg)
            x1, y1 = cx + int(math.cos(r) * 20), cy + int(math.sin(r) * 20)
            x2, y2 = cx + int(math.cos(r) * 28), cy + int(math.sin(r) * 28)
            d.line([(x1, y1), (x2, y2)], fill=hi, width=4)
    elif name == "whirlwind":
        # Ragnarok / SelfAoE spinning crimson-gold blades + shockwave rings
        d.ellipse([12, 12, 84, 84], outline=ac, width=3)
        d.ellipse([20, 20, 76, 76], outline=hi, width=2)
        for deg in (0, 90, 180, 270):
            r0 = math.radians(deg)
            r1 = math.radians(deg + 55)
            p0 = (cx + int(math.cos(r0) * 12), cy + int(math.sin(r0) * 12))
            p1 = (cx + int(math.cos(r0 + 0.45) * 38), cy + int(math.sin(r0 + 0.45) * 38))
            p2 = (cx + int(math.cos(r1) * 26), cy + int(math.sin(r1) * 26))
            d.polygon([p0, p1, p2], fill=hi, outline=ac)
        d.ellipse([38, 38, 58, 58], fill=ac, outline=hi, width=2)
    elif name == "rage_flame":
        # Bloodlust / BuffSelf roaring battle aura + twin fangs/runes
        pts_outer = [(cx, 10), (68, 28), (78, 54), (62, 82), (34, 82), (18, 54), (28, 28)]
        d.polygon(pts_outer, fill=ac, outline=hi)
        pts_inner = [(cx, 22), (60, 38), (64, 58), (cx, 74), (32, 58), (36, 38)]
        d.polygon(pts_inner, fill=hi, outline=dk)
        d.line([(cx, 26), (cx, 70)], fill=dk, width=3)
        d.line([(34, 48), (62, 48)], fill=dk, width=3)
    elif name == "war_horn":
        # War Cry / BuffAll golden battle standard & horn crest
        d.polygon([(cx, 12), (76, 26), (68, 68), (cx, 84), (28, 68), (20, 26)], fill=dk, outline=hi)
        d.polygon([(cx, 20), (66, 32), (cx, 74), (30, 32)], fill=ac, outline=hi)
        d.ellipse([38, 34, 58, 54], fill=hi, outline=dk, width=2)
    else:
        # Generic arcane rune diamond
        d.polygon([(cx, 14), (82, cy), (cx, 82), (14, cy)], fill=dk, outline=hi)
        d.polygon([(cx, 24), (70, cy), (cx, 72), (26, cy)], fill=ac, outline=hi)

    return img


def draw_weapon_icon(class_id: int, is_heavy: bool = False) -> Image.Image:
    """Draws a large, full-bleed Gothic Weapon Icon for each of the 10 classes (Basic LMB & Heavy RMB/X)."""
    s = 96
    cx, cy = s // 2, s // 2
    palettes = {
        0: ((90, 175, 250), (24, 34, 52)),   # Knight: Sword & Shield
        1: ((240, 195, 88), (46, 34, 18)),   # Guardian: Tower Shield & Hammer
        2: ((238, 68, 52),  (52, 18, 16)),   # Berserker: Greataxe / Greatsword
        3: ((192, 95, 245), (36, 18, 48)),   # Assassin: Twin Daggers
        4: ((92, 220, 118), (18, 42, 24)),   # Ranger: Longbow
        5: ((78, 205, 255), (18, 36, 54)),   # Mage: Elemental Staff
        6: ((178, 72, 248), (34, 14, 48)),   # Warlock: Abyssal Scepter
        7: ((255, 172, 62), (48, 28, 14)),   # Monk: Martial Gauntlets
        8: ((72, 228, 158), (16, 44, 32)),   # Druid: Nature Scythe / Claws
        9: ((255, 225, 118), (48, 38, 18)),  # Cleric: Sacred Flanged Mace
    }
    ac_rgb, bg_rgb = palettes.get(class_id, ((238, 200, 108), (32, 22, 15)))
    if is_heavy:
        ac_rgb = tuple(min(255, int(c * 1.15 + 25)) for c in ac_rgb)

    img = make_medallion_base(s, bg_rgb, ac_rgb)
    d = ImageDraw.Draw(img)
    hi = (255, 244, 198, 255)
    ac = ac_rgb + (255,)
    dk = (42, 28, 18, 255)

    # If heavy/weapon skill, draw radiant golden shockwave rays behind weapon
    if is_heavy:
        for deg in range(0, 360, 30):
            rad = math.radians(deg)
            x1, y1 = cx + int(math.cos(rad) * 18), cy + int(math.sin(rad) * 18)
            x2, y2 = cx + int(math.cos(rad) * 42), cy + int(math.sin(rad) * 42)
            d.line([(x1, y1), (x2, y2)], fill=ac_rgb + (185,), width=2)
        d.ellipse([14, 14, 82, 82], outline=hi, width=2)

    if class_id == 0:  # Knight: Broadsword + Kite Shield
        d.polygon([(38, 20), (74, 28), (68, 58), (48, 78), (28, 56), (26, 28)], fill=dk, outline=ac)
        d.line([(18, 78), (78, 18)], fill=hi, width=6)
        d.line([(18, 78), (78, 18)], fill=ac, width=2)
        d.line([(20, 56), (42, 78)], fill=hi, width=4)
    elif class_id == 1:  # Guardian: Warhammer + Bulwark
        d.line([(22, 76), (68, 30)], fill=hi, width=5)
        d.polygon([(50, 14), (82, 32), (72, 48), (40, 28)], fill=ac, outline=hi)
        d.polygon([(46, 20), (76, 36), (68, 44), (42, 26)], fill=hi)
    elif class_id == 2:  # Berserker: Double-Edged Greataxe / Cleaver
        d.line([(18, 80), (76, 22)], fill=hi, width=5)
        d.polygon([(38, 14), (68, 12), (84, 28), (82, 58), (56, 46), (48, 38)], fill=ac, outline=hi)
        d.polygon([(44, 18), (66, 16), (78, 28), (62, 38)], fill=hi)
    elif class_id == 3:  # Assassin: Twin Crossed Daggers
        for sx in (-1, 1):
            d.polygon([(cx - sx * 28, 76), (cx - sx * 20, 68), (cx + sx * 26, 16), (cx + sx * 16, 28)], fill=hi, outline=ac)
            d.line([(cx - sx * 8, 62), (cx - sx * 24, 48)], fill=ac, width=4)
    elif class_id == 4:  # Ranger: Recurve Longbow + Arrow
        d.arc([16, 12, 76, 84], -75, 75, fill=hi, width=5)
        d.line([(46, 14), (46, 82)], fill=ac, width=2)
        d.line([(18, 48), (82, 48)], fill=hi, width=4)
        d.polygon([(84, 48), (70, 41), (73, 48), (70, 55)], fill=ac, outline=hi)
    elif class_id == 5:  # Mage: Arcane Crystal Staff
        d.line([(20, 78), (66, 32)], fill=hi, width=5)
        d.ellipse([52, 12, 84, 44], outline=hi, width=3)
        d.polygon([(68, 16), (78, 28), (68, 40), (58, 28)], fill=ac, outline=hi)
    elif class_id == 6:  # Warlock: Horned Abyssal Skull Scepter
        d.line([(20, 78), (64, 34)], fill=ac, width=5)
        d.polygon([(52, 28), (44, 12), (62, 22)], fill=hi, outline=ac)
        d.polygon([(72, 38), (84, 22), (68, 26)], fill=hi, outline=ac)
        d.ellipse([52, 20, 76, 44], fill=ac, outline=hi, width=2)
    elif class_id == 7:  # Monk: Dragon Claws / Gauntlet Fist
        d.rounded_rectangle([26, 30, 70, 72], radius=10, fill=ac, outline=hi, width=3)
        for ox in (-16, -5, 6, 17):
            d.polygon([(cx + ox - 4, 32), (cx + ox, 12), (cx + ox + 4, 32)], fill=hi, outline=ac)
    elif class_id == 8:  # Druid: Crescent Nature Scythe
        d.line([(22, 80), (68, 20)], fill=hi, width=5)
        d.polygon([(30, 16), (72, 14), (84, 34), (66, 54), (60, 32), (38, 26)], fill=ac, outline=hi)
    else:  # 9 Cleric: Flanged Radiant Sun Mace
        d.line([(20, 78), (62, 36)], fill=hi, width=5)
        for deg in range(0, 360, 45):
            r = math.radians(deg)
            p1 = (64 + int(math.cos(r) * 8), 32 + int(math.sin(r) * 8))
            p2 = (64 + int(math.cos(r) * 22), 32 + int(math.sin(r) * 22))
            d.line([p1, p2], fill=hi, width=4)
        d.ellipse([52, 20, 76, 44], fill=ac, outline=hi, width=3)

    return img


def has_meaningful_opaque_silhouette(png_path: str) -> bool:
    if not os.path.exists(png_path):
        return False
    try:
        im = Image.open(png_path).convert("RGBA")
        arr = np.asarray(im)
        alpha = arr[:, :, 3]
        rgb_sum = arr[:, :, 0].astype(np.int32) + arr[:, :, 1].astype(np.int32) + arr[:, :, 2].astype(np.int32)
        visible = np.logical_and(alpha > 90, rgb_sum > 90)
        ratio = float(np.count_nonzero(visible)) / float(alpha.size)
        return 0.08 <= ratio <= 0.82
    except Exception:
        return False


def build_all_icons():
    idir = os.path.join(RES_UI, "Icons")
    ensure_folder_meta(idir)

    # 1. Potion icon from ExportedProject hsricon_consumable_HealingPotion_01.png
    exp_potion = os.path.join(EXP_UI, "Sprite", "hsr_ui", "battleconsumable", "hsricon_consumable_HealingPotion_01.png")
    pot_img = tint_and_frame_exported_icon(exp_potion, 96, rim_col=(238, 92, 82), bg_col=(42, 18, 16))
    save_ui_png(pot_img, os.path.join(idir, "icon_potion.png"))

    # 2. Core Gothic Engraved UI Icons
    core_icons = [
        ("sword", (238, 200, 108)),
        ("crossed_swords", (238, 200, 108)),
        ("shield", (214, 178, 96)),
        ("armor", (214, 178, 96)),
        ("helm", (214, 178, 96)),
        ("ring", (238, 196, 98)),
        ("amulet", (140, 208, 245)),
        ("dodge", (165, 224, 145)),
        ("gold", (248, 212, 92)),
        ("exp", (198, 168, 245)),
        ("skull_boss", (235, 88, 72)),
        ("check_seal", (130, 228, 142)),
        ("lock_iron", (188, 164, 132)),
        ("crown", (250, 214, 96)),
        ("anvil", (235, 164, 88)),
        ("portal", (125, 198, 250)),
        ("bag", (224, 182, 96)),
        ("skill_tree", (242, 202, 106)),
        ("settings", (214, 182, 122)),
    ]
    for name, col in core_icons:
        im = draw_engraved_icon(name, accent=col)
        save_ui_png(im, os.path.join(idir, f"icon_{name}.png"))

    # 3. 17 SkillKind icons — guaranteed high-contrast full-bleed Gothic skill art (zero empty frames!)
    skill_kinds = [
        ("MeleeArc",   "sword",          (245, 188, 92)),
        ("Projectile", "crossed_swords", (130, 208, 255)),
        ("GroundAoE",  "skull_boss",     (248, 122, 68)),
        ("SelfAoE",    "whirlwind",      (248, 96, 72)),
        ("Dash",       "dodge",          (152, 236, 168)),
        ("HealAll",    "amulet",         (118, 238, 148)),
        ("HealTarget", "amulet",         (118, 238, 148)),
        ("ShieldAll",  "shield",         (248, 214, 112)),
        ("ShieldSelf", "shield",         (195, 220, 248)),
        ("BuffSelf",   "rage_flame",     (248, 108, 68)),
        ("BuffAll",    "war_horn",       (250, 206, 96)),
        ("Taunt",      "helm",           (245, 186, 86)),
        ("Summon",     "portal",         (192, 138, 248)),
        ("Revive",     "exp",            (255, 232, 138)),
        ("Trap",       "lock_iron",      (138, 214, 248)),
        ("Transform",  "skull_boss",     (136, 228, 126)),
        ("Cleanse",    "check_seal",     (168, 238, 224)),
    ]
    for kname, shape, col in skill_kinds:
        im = draw_engraved_icon(shape, accent=col)
        save_ui_png(im, os.path.join(idir, f"icon_kind_{kname}.png"))

    # 4. 20 Class-Specific Weapon Attack & Weapon Skill Icons (icon_wpn_0..9.png & icon_wpn_heavy_0..9.png)
    for cid in range(10):
        save_ui_png(draw_weapon_icon(cid, is_heavy=False), os.path.join(idir, f"icon_wpn_{cid}.png"))
        save_ui_png(draw_weapon_icon(cid, is_heavy=True), os.path.join(idir, f"icon_wpn_heavy_{cid}.png"))


def upgrade_class_portraits_and_backgrounds():
    # Frame all 10 class portraits in Assets/Resources/UI/Classes with an ornate antique gold border
    cdir = os.path.join(RES_UI, "Classes")
    if os.path.isdir(cdir):
        for fn in os.listdir(cdir):
            if fn.endswith(".png") and fn.startswith("icon_"):
                p = os.path.join(cdir, fn)
                try:
                    im = Image.open(p).convert("RGBA").resize((128, 128), Image.Resampling.LANCZOS)
                    draw = ImageDraw.Draw(im)
                    draw.rectangle([0, 0, 127, 127], outline=(12, 8, 5, 255), width=2)
                    draw.rectangle([2, 2, 125, 125], outline=(214, 174, 86, 255), width=3)
                    draw.rectangle([5, 5, 122, 122], outline=(98, 68, 32, 220), width=1)
                    draw_corner_filigree(draw, 128, 128, 2, (214, 174, 86, 255), (250, 222, 136, 255), ornate_level=1)
                    save_ui_png(im, p)
                except Exception as e:
                    print("Portrait frame warning:", fn, e)

    # Also copy an authentic dark gothic sanctuary background from ExportedProject for menu_background & lobby_background
    exp_bg1 = os.path.join(EXP_UI, "Texture", "UILobby_DawnSanctuary", "ui_dawnsanctuary_mainbg02_BG.png")
    exp_bg2 = os.path.join(EXP_UI, "Texture", "UICamp", "ui_camp_bg_mainbg01.png")
    for src, dst_name in [(exp_bg1, "menu_background.png"), (exp_bg2, "lobby_background.png")]:
        dst = os.path.join(RES_UI, dst_name)
        if os.path.exists(src):
            try:
                im = Image.open(src).convert("RGBA")
                # Warm dark-brown & antique-gold color grading
                arr = np.asarray(im, dtype=np.float32)
                arr[:, :, 0] = np.clip(arr[:, :, 0] * 0.82 + 14, 0, 255)
                arr[:, :, 1] = np.clip(arr[:, :, 1] * 0.68 + 8, 0, 255)
                arr[:, :, 2] = np.clip(arr[:, :, 2] * 0.50 + 4, 0, 255)
                arr[:, :, 3] = 255
                save_ui_png(Image.fromarray(arr.astype(np.uint8)), dst)
            except Exception as e:
                print("BG warning:", dst_name, e)


def build_ornate_gothic_brick_pbr_textures():
    """Generates a 512x512 Staggered Gothic Brick & Carved Ornamental Relief PBR texture set
    (T_GothicStone_Albedo, T_GothicStone_Normal, T_GothicStone_AO, T_GothicStone_Mask)."""
    pbr_dir = os.path.join(ROOT, "Assets", "Resources", "Environment", "PBR")
    os.makedirs(pbr_dir, exist_ok=True)
    sz = 512

    # Build heightmap (0..1) and color map (RGB) using vectorized numpy operations
    yy, xx = np.mgrid[0:sz, 0:sz].astype(np.float32)

    # 1. Coursed staggered Gothic masonry bricks (8 horizontal courses)
    rows = 8
    row_h = sz / rows  # 64px per course
    row_idx = np.floor(yy / row_h).astype(np.int32)
    local_y = (yy % row_h) / row_h  # 0..1 within course

    # Row 3 and Row 7 are ornate carved Gothic frieze / rosette relief bands!
    is_frieze = np.logical_or(row_idx == 3, row_idx == 7)

    # Staggered brick columns (4 bricks per row, shifted by 0.5 on odd rows)
    cols = 4
    brick_w = sz / cols  # 128px per brick
    stagger_x = (xx + (row_idx % 2) * (brick_w * 0.5)) % sz
    col_idx = np.floor(stagger_x / brick_w).astype(np.int32)
    local_x = (stagger_x % brick_w) / brick_w  # 0..1 within brick

    # Deep mortar joints & beveled stone edges
    mortar_y = np.minimum(local_y, 1.0 - local_y) * rows
    mortar_x = np.minimum(local_x, 1.0 - local_x) * cols
    brick_edge = np.minimum(mortar_x * 4.2, mortar_y * 2.4)
    brick_bevel = np.clip((brick_edge - 0.14) / 0.24, 0.0, 1.0)
    brick_bevel = brick_bevel * brick_bevel * (3.0 - 2.0 * brick_bevel)

    # Per-brick deterministic tone variation
    brick_hash = np.sin(row_idx * 12.9898 + col_idx * 78.233) * 43758.5453
    brick_var = (brick_hash - np.floor(brick_hash)) - 0.5  # -0.5 .. +0.5

    # Multi-scale stone surface grain & chisel marks
    n1 = np.sin(xx * 0.09 + np.cos(yy * 0.08) * 2.5) * np.cos(yy * 0.11 - np.sin(xx * 0.07) * 2.2)
    n2 = np.sin(xx * 0.27 + yy * 0.19) * np.cos(xx * 0.23 - yy * 0.31) * 0.45
    n3 = np.sin(xx * 0.65 - yy * 0.58) * 0.20
    stone_grain = (n1 + n2 + n3) * 0.08

    # Carved Gothic Pointed-Arch & Quatrefoil Rosette Relief inside the frieze courses
    # Repeat 8 gothic motifs across the 512px width (each 64px wide x 64px high)
    motif_x = ((xx % 64.0) / 64.0) * 2.0 - 1.0  # -1..1
    motif_y = local_y * 2.0 - 1.0                 # -1..1
    r_center = np.hypot(motif_x, motif_y)
    theta = np.arctan2(motif_y, motif_x)
    # 4-lobed Gothic Quatrefoil + Rosette ring + Pointed trefoil arch
    quatrefoil = np.abs(np.cos(2.0 * theta)) * 0.32 + 0.36
    ring_dist = np.abs(r_center - quatrefoil)
    rosette_relief = np.clip(1.0 - ring_dist / 0.14, 0.0, 1.0)
    inner_boss = np.clip(1.0 - r_center / 0.22, 0.0, 1.0)
    border_rail = np.clip(1.0 - np.minimum(np.abs(local_y - 0.12), np.abs(local_y - 0.88)) / 0.07, 0.0, 1.0)
    frieze_height = np.maximum(np.maximum(rosette_relief * 0.85, inner_boss * 0.95), border_rail * 0.90)

    # Subtle carved Gothic blind-arch tracery embossed into standard brick courses too
    arch_x = ((stagger_x % brick_w) / brick_w) * 2.0 - 1.0
    arch_y = local_y * 2.0 - 1.0
    # Pointed ogive arch curve inside each large ashlar block
    ogive_r = np.hypot(arch_x - np.sign(arch_x) * 0.35, arch_y + 0.15)
    ogive_rib = np.clip(1.0 - np.abs(ogive_r - 0.88) / 0.09, 0.0, 1.0) * np.where(arch_y > -0.55, 1.0, 0.0)
    diamond_center = np.clip(1.0 - (np.abs(arch_x) + np.abs(arch_y)) / 0.28, 0.0, 1.0)
    brick_carving = np.maximum(ogive_rib * 0.28, diamond_center * 0.22) * brick_bevel

    height = np.where(
        is_frieze,
        0.25 + frieze_height * 0.72 + stone_grain * 0.4,
        brick_bevel * (0.72 + brick_var * 0.12 + brick_carving) + stone_grain
    )
    height = np.clip(height, 0.0, 1.0)

    # Albedo RGB: Rich Gothic warm dark stone + dark mortar + antique bronze-gold relief highlights on carved motifs
    base_r = 96.0 + brick_var * 26.0 + stone_grain * 90.0
    base_g = 86.0 + brick_var * 22.0 + stone_grain * 82.0
    base_b = 78.0 + brick_var * 18.0 + stone_grain * 74.0

    # Mortar is deep shadow charcoal-brown
    mortar_r, mortar_g, mortar_b = 26.0, 22.0, 20.0
    alb_r = mortar_r + (base_r - mortar_r) * brick_bevel
    alb_g = mortar_g + (base_g - mortar_g) * brick_bevel
    alb_b = mortar_b + (base_b - mortar_b) * brick_bevel

    # Highlight beveled brick rims & gothic carvings with warm sandstone/bronze
    rim_boost = np.clip(1.0 - np.abs(brick_bevel - 0.78) / 0.18, 0.0, 1.0) * 18.0 + brick_carving * 55.0
    alb_r += rim_boost * 1.25
    alb_g += rim_boost * 1.05
    alb_b += rim_boost * 0.72

    # Ornate Frieze Course: dark carved basalt recess + antique gold/bronze relief ornament
    frieze_r = 44.0 + frieze_height * 148.0 + stone_grain * 45.0
    frieze_g = 36.0 + frieze_height * 118.0 + stone_grain * 38.0
    frieze_b = 28.0 + frieze_height * 66.0 + stone_grain * 30.0

    final_r = np.clip(np.where(is_frieze, frieze_r, alb_r), 0, 255).astype(np.uint8)
    final_g = np.clip(np.where(is_frieze, frieze_g, alb_g), 0, 255).astype(np.uint8)
    final_b = np.clip(np.where(is_frieze, frieze_b, alb_b), 0, 255).astype(np.uint8)
    final_a = np.full((sz, sz), 255, dtype=np.uint8)

    albedo_arr = np.dstack([final_r, final_g, final_b, final_a])
    Image.fromarray(albedo_arr, "RGBA").save(os.path.join(pbr_dir, "T_GothicStone_Albedo.png"))

    # Tangent-space Normal Map from height gradient
    strength = 6.5
    dh_dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * strength
    dh_dy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * strength
    nx = -dh_dx
    ny = dh_dy
    nz = np.ones_like(height)
    inv_len = 1.0 / np.sqrt(nx * nx + ny * ny + nz * nz)
    nx *= inv_len
    ny *= inv_len
    nz *= inv_len

    nrm_r = np.clip((nx * 0.5 + 0.5) * 255.0, 0, 255).astype(np.uint8)
    nrm_g = np.clip((ny * 0.5 + 0.5) * 255.0, 0, 255).astype(np.uint8)
    nrm_b = np.clip((nz * 0.5 + 0.5) * 255.0, 0, 255).astype(np.uint8)
    nrm_arr = np.dstack([nrm_r, nrm_g, nrm_b, final_a])
    Image.fromarray(nrm_arr, "RGBA").save(os.path.join(pbr_dir, "T_GothicStone_Normal.png"))

    # Ambient Occlusion Map (deep mortar & carving crevices)
    ao_val = np.clip(0.32 + height * 0.68, 0.0, 1.0)
    ao_u8 = (ao_val * 255.0).astype(np.uint8)
    ao_arr = np.dstack([ao_u8, ao_u8, ao_u8, final_a])
    Image.fromarray(ao_arr, "RGBA").save(os.path.join(pbr_dir, "T_GothicStone_AO.png"))

    # Metallic (R) & Smoothness (A) Mask Map
    metal_u8 = np.where(is_frieze, np.clip(frieze_height * 165.0, 10, 180), 18).astype(np.uint8)
    smooth_u8 = np.where(is_frieze, np.clip(70.0 + frieze_height * 125.0, 50, 200), np.clip(55.0 + brick_bevel * 55.0, 40, 130)).astype(np.uint8)
    mask_arr = np.dstack([metal_u8, ao_u8, np.zeros_like(ao_u8), smooth_u8])
    Image.fromarray(mask_arr, "RGBA").save(os.path.join(pbr_dir, "T_GothicStone_Mask.png"))


def main():
    ensure_folder_meta(RES_UI)
    setup_fonts()
    build_all_panels()
    build_all_buttons()
    build_decor_and_bars()
    build_all_icons()
    upgrade_class_portraits_and_backgrounds()
    build_ornate_gothic_brick_pbr_textures()
    print("Successfully built Ancient Gothic UI Asset Kit & Ornate Gothic Brick PBR Textures!")


if __name__ == "__main__":
    main()


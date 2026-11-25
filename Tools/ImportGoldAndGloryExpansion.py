#!/usr/bin/env python3
"""
Gold and Glory (ExportedProject) Expansion Importer for Abyssal Dungeon 3D:
1. Fixes Env_DungeonFloor & Env_GothicArchVault (open doorway arch without prison bars)
   and imports 6 new authentic Gold and Glory Gothic dungeon/sanctuary props.
2. Decodes & exports full 3-hit Combo (Attack, Attack2, Attack3), HeavyAttack, SkillAlt,
   Walk, and CareerChoices LobbyIdle animations for all 10 Hero Classes via CRC32 bone-path table.
3. Extracts 28 authentic Gold and Glory Skill, Projectile, and Hit VFX prefabs (preferring *_l / *_m
   iGPU-optimized variants), upgrades Effect .mat files to URP AbyssalDungeon/GnGEffect, strips
   scripts/lights/audio/colliders, and caps maxParticles for smooth laptop performance.
"""

import os
import re
import shutil
import stat
import time
import zlib
from concurrent.futures import ThreadPoolExecutor

import ImportFromExportedProject as base_imp

PROJECT_ROOT = r"c:\LTG\Abyssal_Dungeon_3D"
EP_ASSETS = r"\\?\c:\LTG\Abyssal_Dungeon_3D\ExportedProject\Assets"
DST_ASSETS = os.path.join(PROJECT_ROOT, "Assets")
DST_EXPORTED = os.path.join(DST_ASSETS, "Art", "Exported")
DST_GNG_VFX = os.path.join(DST_ASSETS, "Resources", "VFX", "GnG")

GNG_EFFECT_SHADER_GUID = "7b4e91c2d83a4f509e126c84a90d31ef"

# ---------------------------------------------------------------------------
# 1. FIXED & EXPANDED GOLD AND GLORY ENVIRONMENT ARCHITECTURE & PROPS
# ---------------------------------------------------------------------------
EXTRA_ENVIRONMENT = {
    # Fix floor (was wall!) and open arch (was closed iron door!)
    "Env_DungeonFloor":     r"ART\Environment\Art_Map01\Common\PFB_CT02_Floor_02.prefab",
    "Env_GothicArchVault":  r"ART\Environment\Art_Map01\Common\204_doorframe.prefab",
    # Rich authentic Gold and Glory Gothic dungeon & sanctuary props
    "Env_StoneCoffin":      r"ART\Environment\Art_Map01\Fbx\Fbx_Other_01\MES_CT03_Stone_Coffin_01a.prefab",
    "Env_CandleGroup":      r"ART\Environment\Art_Map01\Common\PFB_M01_CMN_Candle_01_Group.prefab",
    "Env_RitualTable":      r"ART\Environment\Art_Map01\Common\PFB_M01_PO_Table_01.prefab",
    "Env_HangingChain":     r"ART\Environment\Art_Map01\Common\PFB_Stone_Chain_01.prefab",
    "Env_KnightStatue":     r"ART\Environment\Art_Map01\Common\PFB_M01_PO_Statue_01.prefab",
    "Env_GothicChandelier": r"ART\Environment\Art_Map01\Common\PFB_M01_CMN_Lamp_04_Group.prefab",
}

# ---------------------------------------------------------------------------
# 2. FULL MULTI-COMBO, HEAVY, SKILL, WALK & LOBBY SHOWCASE ANIMATIONS (10 CLASSES)
# ---------------------------------------------------------------------------
HERO_EXPANSION_ANIMS = {
    "Knight": {
        "Walk":        r"Character\G1P\G1P@SwordShield\WPN_SwordShield_Crouch_Walk_Loop.anim",
        "Attack":      r"Character\G1P\G1P@SwordShield\WPN_SwordShield_Attack_01.anim",
        "Attack2":     r"Character\G1P\G1P@SwordShield\WPN_SwordShield_Attack_02.anim",
        "Attack3":     r"Character\G1P\G1P@SwordShield\WPN_SwordShield_Attack_03.anim",
        "HeavyAttack": r"Character\G1P\G1P@SwordShield\WPN_SwordShield_Shield_Attack_01.anim",
        "SkillAlt":    r"Character\G1P\G1P@SwordShield\WPN_SwordShield_BraveCharge_Attack.anim",
        "LobbyIdle":   r"Character\G1P\G1P@Common\G1P@CareerChoices\G1P_WPN_CareerChoices_Fighter_Loop.anim",
    },
    "Guardian": {
        "Walk":        r"Character\G1P\G1P@Mace\G1P_WPN_Mace_Move_01.anim",
        "Attack":      r"Character\G1P\G1P@Mace\G1P_WPN_Mace_Attack_01.anim",
        "Attack2":     r"Character\G1P\G1P@Mace\G1P_WPN_Mace_Attack_02.anim",
        "Attack3":     r"Character\G1P\G1P@Mace\G1P_WPN_Mace_Attack_03.anim",
        "HeavyAttack": r"Character\G1P\G1P@SwordShield\WPN_SwordShield_Shield_Attack_01.anim",
        "SkillAlt":    r"Character\G1P\G1P@Warrior\G1P_WPN_Warrior_ShoulderImpact_01.anim",
        "LobbyIdle":   r"Character\G1P\G1P@Common\G1P@CareerChoices\G1P_WPN_CareerChoices_Fighter_Loop.anim",
    },
    "Berserker": {
        "Walk":        r"Character\G1P\G1P@Zweihander\G1P_WPN_Zweihander_CrouchMoveF_01.anim",
        "Attack":      r"Character\G1P\G1P@Zweihander\G1P_WPN_Zweihander_Attack_01.anim",
        "Attack2":     r"Character\G1P\G1P@Zweihander\G1P_WPN_Zweihander_Attack_02.anim",
        "Attack3":     r"Character\G1P\G1P@Zweihander\G1P_WPN_Zweihander_Attack_03.anim",
        "HeavyAttack": r"Character\G1P\G1P@Zweihander\G1P_WPN_Zweihander_Attack_04.anim",
        "SkillAlt":    r"Character\G1P\G1P@Zweihander\G1P_WPN_Zweihander_SprintAttack.anim",
        "LobbyIdle":   r"Character\G1P\G1P@Common\G1P@CareerChoices\G1P_WPN_CareerChoices_LongSword_Loop.anim",
    },
    "Assassin": {
        "Walk":        r"Character\G1P\G1P@DoubleSword\G1P_WPN_DoubleSword_CrouchWalkF_01.anim",
        "Attack":      r"Character\G1P\G1P@DoubleSword\G1P_WPN_DoubleSword_Attack_01.anim",
        "Attack2":     r"Character\G1P\G1P@DoubleSword\G1P_WPN_DoubleSword_Attack_02.anim",
        "Attack3":     r"Character\G1P\G1P@DoubleSword\G1P_WPN_DoubleSword_Attack_03.anim",
        "HeavyAttack": r"Character\G1P\G1P@DoubleSword\G1P_WPN_DoubleSword_Smash_01.anim",
        "SkillAlt":    r"Character\G1P\G1P@Rogue\G1P_WPN_Rogue_PoisonBladeWave_01.anim",
        "LobbyIdle":   r"Character\G1P\G1P@Common\G1P@CareerChoices\G1P_WPN_CareerChoices_Rogue_Loop.anim",
    },
    "Ranger": {
        "Walk":        r"Character\G1P\G1P@Bow\G1P_WPN_Bow_CrouchWalkF_01.anim",
        "Attack":      r"Character\G1P\G1P@Bow\G1P_WPN_Bow_Shoot_01.anim",
        "Attack2":     r"Character\G1P\G1P@Bow\G1P_WPN_Bow_ShootContinuous_01.anim",
        "Attack3":     r"Character\G1P\G1P@Aracher\G1P_WPN_Aracher_TripleShoot_01.anim",
        "HeavyAttack": r"Character\G1P\G1P@Bow\G1P_WPN_Bow_PowerShootContinuous_01.anim",
        "SkillAlt":    r"Character\G1P\G1P@Bow\G1P_WPN_Longbow_Skill_01.anim",
        "LobbyIdle":   r"Character\G1P\G1P@Common\G1P@CareerChoices\G1P_WPN_CareerChoices_Aracher_Loop.anim",
    },
    "Mage": {
        "Walk":        r"Character\G1P\G1P@Wand\G1P_WPN_Wand_CrouchMoveF_01.anim",
        "Attack":      r"Character\G1P\G1P@Wand\WPN_Wand_Attack_01.anim",
        "Attack2":     r"Character\G1P\G1P@Wand\WPN_Wand_Attack_02.anim",
        "Attack3":     r"Character\G1P\G1P@Wand\WPN_Wand_Attack_03.anim",
        "HeavyAttack": r"Character\G1P\G1P@Wand\G1P_WPN_Wizard_IceSpear_Attack_Wand.anim",
        "SkillAlt":    r"Character\G1P\G1P@Wizard\G1P_WPN_Wizard_IceArrow.anim",
        "LobbyIdle":   r"Character\G1P\G1P@Common\G1P@CareerChoices\G1P_WPN_CareerChoices_Wizzard_Loop.anim",
    },
    "Warlock": {
        "Walk":        r"Character\G1P\G1P@CrystalSword\G1P_WPN_CrystalSword_CrouchWalkF_01.anim",
        "Attack":      r"Character\G1P\G1P@CrystalSword\G1P_WPN_CrystalSword_Attack_01.anim",
        "Attack2":     r"Character\G1P\G1P@CrystalSword\G1P_WPN_CrystalSword_Attack_02.anim",
        "Attack3":     r"Character\G1P\G1P@CrystalSword\G1P_WPN_CrystalSword_Attack_03.anim",
        "HeavyAttack": r"Character\G1P\G1P@CurseKnight\G1P_WPN_CurseKnight_CrippleAttack.anim",
        "SkillAlt":    r"Character\G1P\G1P@CurseKnight\G1P_WPN_CurseKnight_AroundBlood.anim",
        "LobbyIdle":   r"Character\G1P\G1P@Common\G1P@CareerChoices\G1P_WPN_CareerChoices_CurseKnight_Loop_01.anim",
    },
    "Monk": {
        "Walk":        r"Character\G1P\G1P@Fist\G1P_WPN_Fist_CrouchWalkF_01.anim",
        "Attack":      r"Character\G1P\G1P@Fist\G1P_WPN_Fist_attack01.anim",
        "Attack2":     r"Character\G1P\G1P@Fist\G1P_WPN_Fist_attack02.anim",
        "Attack3":     r"Character\G1P\G1P@Fist\G1P_WPN_Fist_attack03.anim",
        "HeavyAttack": r"Character\G1P\G1P@Fist\G1P_WPN_Fist_attack13.anim",
        "SkillAlt":    r"Character\G1P\G1P@Fist\G1P_WPN_Fist_shield_attack01.anim",
        "LobbyIdle":   r"Character\G1P\G1P@Common\G1P@CareerChoices\G1P_WPN_CareerChoices_Boxer_Loop.anim",
    },
    "Druid": {
        "Walk":        r"Character\G1P\G1P@Wand\G1P_WPN_Wand_CrouchMoveF_01.anim",
        "Attack":      r"Character\G1P\G1P@Wand\WPN_Wand_Attack_01.anim",
        "Attack2":     r"Character\G1P\G1P@Wand\WPN_Wand_Attack_02.anim",
        "Attack3":     r"Character\G1P\G1P@Spear\G1P_WPN_Spear_Attack_03.anim",
        "HeavyAttack": r"Character\G1P\G1P@Spear\G1P_WPN_Spear_WeaponSkill_01.anim",
        "SkillAlt":    r"Character\G1P\G1P@Spear\G1P_WPN_Spear_skill_02.anim",
        "LobbyIdle":   r"Character\G1P\G1P@Common\G1P@CareerChoices\G1P_WPN_CareerChoices_Doctor_Loop_01.anim",
    },
    "Cleric": {
        "Walk":        r"Character\G1P\G1P@Mace\G1P_WPN_Mace_Move_01.anim",
        "Attack":      r"Character\G1P\G1P@Mace\G1P_WPN_Mace_Attack_01.anim",
        "Attack2":     r"Character\G1P\G1P@Mace\G1P_WPN_Mace_Attack_02.anim",
        "Attack3":     r"Character\G1P\G1P@Mace\G1P_WPN_Mace_Attack_03.anim",
        "HeavyAttack": r"Character\G1P\G1P@Mace\G1P_WPN_Mace_Skill_Down_01.anim",
        "SkillAlt":    r"Character\G1P\G1P@Warpriest\G1P_WPN_Warpriest_HolyNovaStart.anim",
        "LobbyIdle":   r"Character\G1P\G1P@Common\G1P@CareerChoices\G1P_WPN_CareerChoices_ClericLoop_01.anim",
    },
}

# ---------------------------------------------------------------------------
# 3. AUTHENTIC GOLD AND GLORY SKILL, PROJECTILE & HIT VFX MANIFEST
#    (Automatically resolves *_l.prefab / *_m.prefab first for iGPU speed!)
# ---------------------------------------------------------------------------
GNG_VFX_MANIFEST = {
    # Knight / Guardian / Berserker (warrior & katana)
    "GnG_Warrior_BlessedSword":  ("character/warrior", ["vfx_char_Warrior_BlessedSword"]),
    "GnG_Warrior_BraveCharge":   ("character/warrior", ["vfx_char_Warrior_BraveCharge_fire", "vfx_char_Warrior_BraveCharge_loop"]),
    "GnG_Warrior_DragonShock":   ("character/warrior", ["vfx_char_Warrior_DragonShoc_01", "vfx_char_Warrior_DragonShoc_02"]),
    "GnG_Warrior_ShieldAura":    ("character/warrior", ["vfx_char_warrior_sheild_01_3P", "vfx_char_warrior_sheild_end"]),
    "GnG_Warrior_RapidStrike":   ("character/warrior", ["vfx_char_warrior_RapidStrike_01"]),
    "GnG_Warrior_Buff":          ("character/warrior", ["vfx_char_warrior_GeBuff_03_3P", "vfx_char_warrior_GeBuff_03"]),
    "GnG_Berserker_Slash":       ("character/katana",  ["vfx_char_katana_attack_01_shura", "vfx_char_katana_attack_01"]),
    "GnG_Berserker_FinalHit":    ("character/wizard",  ["vfx_char_Katana_skillfinal_fire_fangfan_hit"]),

    # Assassin (rogue)
    "GnG_Rogue_Slash1":          ("character/rogue",   ["CycloneSlash", "TearingStrike", "SilentStrike", "Poison", "attack"]),
    "GnG_Rogue_Poison":          ("character/rogue",   ["Poison", "Explode", "Dart", "Smoke"]),

    # Ranger (archer)
    "GnG_Archer_ShockBack":      ("character/archer",  ["vfx_char_archer_Bow_ShockBack_fire", "vfx_char_archer_Bow_ShockBack_dimian"]),
    "GnG_Archer_TriShoot":       ("character/archer",  ["vfx_char_archer_Bow_TriShoot_test_fire", "vfx_char_archer_Bow_TriShoot_test"]),
    "GnG_Archer_Trap":           ("character/archer",  ["vfx_char_archer_Building_HuntingTrap_born", "HuntingTrap"]),

    # Mage (wizard)
    "GnG_Wizard_ArcaneMissile":  ("character/wizard",  ["vfx_char_wizard_ArcaneMissile_01", "ArcaneMissile_hit_01_3P"]),
    "GnG_Wizard_IceBurst":       ("character/wizard",  ["vfx_char_wizard_attack_ice_fire", "attack_ice_trail_01"]),
    "GnG_Wizard_FireCast":       ("character/wizard",  ["vfx_char_wizard_cast_fire_loop_01", "cast_fireball_start_loop"]),
    "GnG_Wizard_Thunder":        ("character/wizard",  ["Thunder", "Lightning", "ArcaneMissile_02"]),

    # Warlock (curseKnight)
    "GnG_Warlock_AroundBlood":   ("character/curseKnight", ["vfx_char_CurseKnight_AroundBlood_02", "AroundBlood_01_start"]),
    "GnG_Warlock_BloodSlash":    ("character/curseKnight", ["vfx_char_CurseKnight_attack03_bloodoverflow_3P", "Soul", "Curse"]),

    # Monk (Boxer)
    "GnG_Monk_Combo1":           ("character/Boxer",   ["vfx_char_boxer_attack_001_small", "vfx_char_boxer_attack_001"]),
    "GnG_Monk_Combo3":           ("character/Boxer",   ["vfx_char_boxer_attack_003", "skill"]),

    # Druid & Cleric (Doctor / oracle / warpriest)
    "GnG_Druid_Nature":          ("character/Doctor",  ["vfx_char_doctor_attack_03", "vfx_char_doctor_attack_01_new", "heal", "poison"]),
    "GnG_Cleric_HolySkill":      ("character/warpriest", ["Holy", "Shield", "Heal", "Imprison", "Trial", "vfx_char"]),

    # Common Hits & Projectiles
    "GnG_Hit_Sword":             ("hit",               ["VFX_CMN_Hit_Sword_02_3P", "VFX_CMN_Hit_Sword_02"]),
    "GnG_Hit_HolyHammer":        ("hit",               ["VFX_CMN_Hit_hammer_holy_3P", "VFX_CMN_Hit_hammer_sunder_3P"]),
    "GnG_Hit_Metal":             ("hit",               ["VFX_CMN_Hit_Metal_01"]),
    "GnG_Proj_FireBall":         ("__PROJ__",          ["FireBallExpire", "FireBall"]),
    "GnG_Proj_IceBall":          ("__PROJ__",          ["IceBall_start_bullet", "IceBall"]),
    "GnG_Proj_GhostBall":        ("__PROJ__",          ["GhostBallExpire", "GhostBall_FromFireBall"]),
    "GnG_Proj_Adjudication":     ("__PROJ__",          ["Adjudication", "ThunderAttack"]),
}


def resolve_vfx_prefab_candidate(subdir: str, patterns):
    if subdir == "__PROJ__":
        base_dir = os.path.join(EP_ASSETS, r"AssetBox\ResFolder\Prefabs\Projectile")
    else:
        base_dir = os.path.join(EP_ASSETS, r"AssetBox\ResFolder\Prefabs\Effect\Prefab", subdir.replace("/", os.sep))

    if not os.path.isdir(base_dir):
        return None

    all_prefabs = []
    for dp, _, fns in os.walk(base_dir):
        for f in fns:
            if f.lower().endswith(".prefab"):
                all_prefabs.append(os.path.join(dp, f))

    if not all_prefabs:
        return None

    for pat in patterns:
        pat_low = pat.lower()
        # 1. Exact stem match preferring _l.prefab (Low GPU) -> _m.prefab (Med GPU) -> .prefab
        for suffix in ("_l.prefab", "_m.prefab", ".prefab"):
            target = pat_low + suffix
            for p in all_prefabs:
                if os.path.basename(p).lower() == target:
                    return p
        # 2. Substring match preferring non-1P, _l/_m first
        matches = [
            p for p in all_prefabs
            if pat_low in os.path.basename(p).lower() and "_1p" not in os.path.basename(p).lower()
        ]
        if matches:
            matches.sort(key=lambda p: (
                0 if p.lower().endswith("_l.prefab") else (1 if p.lower().endswith("_m.prefab") else 2),
                len(os.path.basename(p))
            ))
            return matches[0]

    # Fallback: smallest 3P prefab in folder
    non_1p = [p for p in all_prefabs if "_1p" not in os.path.basename(p).lower()]
    pool = non_1p if non_1p else all_prefabs
    pool.sort(key=lambda p: os.path.getsize(p))
    return pool[0]


def upgrade_effect_material_to_urp(mat_yaml: str, mat_name: str) -> str:
    """Upgrades Studio Dois custom Effect shader Material YAML -> URP AbyssalDungeon/GnGEffect."""
    base_ref = base_imp.extract_tex_ref(mat_yaml, ["_BaseMap", "_MainTex", "_Tex", "_ColorTex"])
    mask_ref = base_imp.extract_tex_ref(mat_yaml, ["_Mask", "_MaskTex", "_MaskMap", "_DissolveTex"])

    # Detect blend mode: _BlendDst: 1 = One (Additive), 10 = OneMinusSrcAlpha (AlphaBlend)
    m_dst = re.search(r"_BlendDst:\s*(\d+)", mat_yaml)
    m_src = re.search(r"_BlendSrc:\s*(\d+)", mat_yaml)
    dst_blend = int(m_dst.group(1)) if m_dst else 1
    src_blend = int(m_src.group(1)) if m_src else 5
    if dst_blend not in (1, 10):
        dst_blend = 1 if ("add" in mat_name.lower() or "glow" in mat_name.lower() or dst_blend <= 2) else 10
    if src_blend not in (1, 5):
        src_blend = 5

    # Detect whether texture uses RGB luminance for alpha (very common in Gold and Glory Effect textures!)
    invalid_sec = re.search(r"m_InvalidKeywords:\s*\r?\n((?:\s*-\s*[^\r\n]+\r?\n?)*)", mat_yaml)
    invalid_kws = invalid_sec.group(1) if invalid_sec else ""
    valid_sec = re.search(r"m_ValidKeywords:\s*\r?\n((?:\s*-\s*[^\r\n]+\r?\n?)*)", mat_yaml)
    valid_kws = valid_sec.group(1) if valid_sec else ""

    use_alpha_disabled = "_USEALPHA_ON" in invalid_kws
    lum_as_alpha = 0.85 if (dst_blend == 1 or use_alpha_disabled) else 0.15
    use_mask = 1.0 if (mask_ref and ("_MASK_ON" in valid_kws or mask_ref["guid"] != (base_ref["guid"] if base_ref else ""))) else 0.0

    # Extract tint color (_BaseColor or _TintColor or _Color), clamping extreme HDR values for laptop Bloom stability
    col_r, col_g, col_b, col_a = 1.0, 1.0, 1.0, 1.0
    for cprop in ("_BaseColor", "_TintColor", "_Color", "_MainColor"):
        mc = re.search(rf"{cprop}:\s*\{{r:\s*([0-9.eE+-]+),\s*g:\s*([0-9.eE+-]+),\s*b:\s*([0-9.eE+-]+),\s*a:\s*([0-9.eE+-]+)\}}", mat_yaml)
        if mc:
            col_r = min(2.2, max(0.05, float(mc.group(1))))
            col_g = min(2.2, max(0.05, float(mc.group(2))))
            col_b = min(2.2, max(0.05, float(mc.group(3))))
            col_a = min(1.0, max(0.15, float(mc.group(4))))
            break

    def fmt_tex(ref):
        if not ref:
            return "        m_Texture: {fileID: 0}\n        m_Scale: {x: 1, y: 1}\n        m_Offset: {x: 0, y: 0}"
        sx_m = re.search(r"x:\s*([0-9.eE+-]+),\s*y:\s*([0-9.eE+-]+)", ref["scale"])
        scale_str = ref["scale"]
        if sx_m:
            sx, sy = float(sx_m.group(1)), float(sx_m.group(2))
            if abs(sx) < 0.001:
                sx = 1.0
            if abs(sy) < 0.001:
                sy = 1.0
            scale_str = f"x: {sx:g}, y: {sy:g}"
        return (
            f"        m_Texture: {{fileID: {ref['fileID']}, guid: {ref['guid']}, type: {ref['type']}}}\n"
            f"        m_Scale: {{{scale_str}}}\n"
            f"        m_Offset: {{{ref['offset']}}}"
        )

    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 6
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {mat_name}
  m_Shader: {{fileID: 4800000, guid: {GNG_EFFECT_SHADER_GUID}, type: 3}}
  m_ShaderKeywords: 
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 1
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: 3010
  stringTagMap:
    RenderType: Transparent
  disabledShaderPasses: []
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
      _BaseMap:
{fmt_tex(base_ref)}
      _MaskMap:
{fmt_tex(mask_ref)}
    m_Floats:
      _Cull: 0
      _DstBlend: {dst_blend}
      _EmissionBoost: 1.25
      _LumAsAlpha: {lum_as_alpha}
      _SrcBlend: {src_blend}
      _UseMask: {use_mask}
    m_Colors:
      _BaseColor: {{r: {col_r:.4f}, g: {col_g:.4f}, b: {col_b:.4f}, a: {col_a:.4f}}}
"""


def sanitize_vfx_prefab_yaml(yaml_text: str, out_name: str) -> str:
    """
    Sanitizes a Gold and Glory VFX prefab for Unity 2021.3 URP + integrated GPU laptops:
    - Strips MonoBehaviours (!u!114), AudioSources (!u!82), Lights (!u!108), Colliders (!u!64/65/135/136)
    - Forces m_Layer: 0 (Default, visible to all cameras!) — NOTE: GnG VFX prefabs use m_Layer: 1 (TransparentFX), keep 0!
    - Forces m_CastShadows: 0, m_ReceiveShadows: 0
    - Caps maxParticles <= 28 per ParticleSystem so laptop GPUs never choke on overdraw!
    """
    blocks = yaml_text.split("--- !u!")
    if len(blocks) <= 1:
        return yaml_text

    strip_prefixes = ("114 ", "82 ", "108 ", "64 ", "65 ", "135 ", "136 ", "54 ")
    stripped_ids = set()
    kept_blocks = [blocks[0]]

    for b in blocks[1:]:
        header = b.split("\n", 1)[0]
        if any(b.startswith(p) for p in strip_prefixes):
            if "&" in header:
                stripped_ids.add(header.split("&", 1)[1].strip())
            continue
        kept_blocks.append(b)

    comp_line_re = re.compile(r"^\s*-\s*component:\s*\{fileID:\s*(\d+)\}\s*$", re.MULTILINE)
    max_pt_re = re.compile(r"(maxNumParticles:\s*)(\d+)")

    final_blocks = [kept_blocks[0]]
    renamed_root = False

    for b in kept_blocks[1:]:
        if b.startswith("1 "):
            # Remove stripped component references & set m_Layer: 0
            lines = b.splitlines(keepends=True)
            filtered = []
            for ln in lines:
                m = comp_line_re.match(ln)
                if m and m.group(1) in stripped_ids:
                    continue
                filtered.append(ln)
            b = "".join(filtered)
            b = re.sub(r"m_Layer:\s*\d+", "m_Layer: 0", b)
            if not renamed_root:
                b = re.sub(r"m_Name:\s*[^\r\n]+", f"m_Name: {out_name}", b, count=1)
                renamed_root = True
        elif b.startswith("198 "):  # ParticleSystem
            b = max_pt_re.sub(lambda m: f"{m.group(1)}{min(28, int(m.group(2)))}", b)
            b = re.sub(r"prewarm:\s*1", "prewarm: 0", b)
            m_vel = re.search(r"  VelocityModule:\r?\n((?:    .*\r?\n)*)", b)
            if m_vel and "enabled: 1" in m_vel.group(1):
                vsec = m_vel.group(1)
                states = [int(x) for x in re.findall(r"minMaxState:\s*(\d+)", vsec)]
                if len(set(states)) > 1:
                    target_st = max(states)
                    vsec = re.sub(r"(minMaxState:\s*)\d+", rf"\g<1>{target_st}", vsec)
                    b = b[: m_vel.start(1)] + vsec + b[m_vel.end(1) :]
        elif b.startswith("199 ") or b.startswith("96 ") or b.startswith("23 ") or b.startswith("137 "):
            b = re.sub(r"m_CastShadows:\s*\d+", "m_CastShadows: 0", b)
            b = re.sub(r"m_ReceiveShadows:\s*\d+", "m_ReceiveShadows: 0", b)

        final_blocks.append("--- !u!" + b)

    return "".join(final_blocks)


def safe_write_text(path: str, content: str):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    for attempt in range(4):
        try:
            if os.path.exists(path):
                os.chmod(path, stat.S_IWRITE | stat.S_IREAD)
            with open(path, "w", encoding="utf-8", newline="\n") as fh:
                fh.write(content)
            return
        except PermissionError:
            time.sleep(0.15 * (attempt + 1))
    with open(path, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(content)


def copy_with_meta_preserve_guid(src_path: str, dst_path: str, meta_transform=None):
    os.makedirs(os.path.dirname(dst_path), exist_ok=True)
    if os.path.exists(dst_path):
        try:
            os.chmod(dst_path, stat.S_IWRITE | stat.S_IREAD)
        except Exception:
            pass
    shutil.copy2(src_path, dst_path)
    try:
        os.chmod(dst_path, stat.S_IWRITE | stat.S_IREAD)
    except Exception:
        pass
    src_meta = src_path + ".meta"
    dst_meta = dst_path + ".meta"
    if os.path.isfile(src_meta):
        mtxt = open(src_meta, "r", encoding="utf-8", errors="ignore").read()
        if meta_transform is not None:
            mtxt = meta_transform(mtxt)
        safe_write_text(dst_meta, mtxt)


def collect_existing_project_guids():
    """Collect GUIDs already present inside Assets/Art/Exported so we reuse shared textures/meshes/materials."""
    existing = {}
    if not os.path.isdir(DST_EXPORTED):
        return existing
    guid_re = re.compile(r"guid:\s*([0-9a-f]{32})")
    for dp, _, fns in os.walk(DST_EXPORTED):
        for f in fns:
            if f.endswith(".meta"):
                mp = os.path.join(dp, f)
                try:
                    txt = open(mp, "r", encoding="utf-8", errors="ignore").read(256)
                    m = guid_re.search(txt)
                    if m:
                        existing[m.group(1)] = mp[:-5]
                except Exception:
                    pass
    return existing


def import_extra_environment_prefabs(guid_map, existing_guids):
    print("[2/4] Importing fixed Floor, open ArchVault & 6 new Gold and Glory Gothic Map Props...", flush=True)
    path_to_guid, is_mat_textured, mesh_to_mats, partname_to_mats = base_imp.build_material_resolution_tables(guid_map)
    guid_re = re.compile(r"guid:\s*([0-9a-f]{32})")
    normal_tex_guids = set()
    queue = []

    env_dst_dir = os.path.join(DST_EXPORTED, "Environment")
    os.makedirs(env_dst_dir, exist_ok=True)

    for out_name, rel in EXTRA_ENVIRONMENT.items():
        src = os.path.join(EP_ASSETS, rel)
        if not os.path.isfile(src):
            print(f"  [WARN] Missing environment prefab: {rel}")
            continue
        dst = os.path.join(env_dst_dir, out_name + ".prefab")
        raw_yaml = open(src, "r", encoding="utf-8", errors="ignore").read()
        patched_yaml, _ = base_imp.patch_blank_materials_in_prefab(
            raw_yaml, src, guid_map, path_to_guid, is_mat_textured, mesh_to_mats, partname_to_mats
        )
        # Preserve existing .meta GUID if destination prefab already exists!
        old_meta = None
        if os.path.isfile(dst + ".meta"):
            old_meta = open(dst + ".meta", "r", encoding="utf-8", errors="ignore").read()
        safe_write_text(dst, patched_yaml)
        if old_meta:
            safe_write_text(dst + ".meta", old_meta)
        elif os.path.isfile(src + ".meta"):
            safe_write_text(dst + ".meta", open(src + ".meta", "r", encoding="utf-8", errors="ignore").read())

        for g in guid_re.findall(patched_yaml):
            if g not in existing_guids and g in guid_map:
                queue.append(g)

    copied_deps = 0
    while queue:
        g = queue.pop()
        if g in existing_guids or g not in guid_map:
            continue
        src = guid_map[g]
        ext = os.path.splitext(src)[1].lower()
        stem = os.path.splitext(os.path.basename(src))[0]
        short = g[:8]

        if ext == ".mat":
            dst = os.path.join(DST_EXPORTED, "Materials", f"{stem}_{short}.mat")
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            mat_txt = open(src, "r", encoding="utf-8", errors="ignore").read()
            urp_txt, used_guids, norm_guid = base_imp.upgrade_material_to_urp_lit(mat_txt, f"{stem}_{short}")
            if norm_guid:
                normal_tex_guids.add(norm_guid)
            safe_write_text(dst, urp_txt)
            if os.path.isfile(src + ".meta"):
                safe_write_text(dst + ".meta", open(src + ".meta", "r", encoding="utf-8", errors="ignore").read())
            existing_guids[g] = dst
            copied_deps += 1
            for dg in used_guids:
                if dg not in existing_guids and dg in guid_map:
                    queue.append(dg)
        elif ext in (".fbx", ".obj", ".asset"):
            dst = os.path.join(DST_EXPORTED, "Meshes", f"{stem}_{short}{ext}")
            copy_with_meta_preserve_guid(src, dst)
            existing_guids[g] = dst
            copied_deps += 1
        elif ext in (".png", ".tga", ".jpg", ".jpeg", ".psd", ".exr"):
            dst = os.path.join(DST_EXPORTED, "Textures", f"{stem}_{short}{ext}")
            copy_with_meta_preserve_guid(src, dst)
            if g in normal_tex_guids or "_ns" in stem.lower() or "_nm" in stem.lower() or "normal" in stem.lower():
                base_imp.fix_normal_map_image_and_meta(src, dst, src + ".meta", dst + ".meta")
            existing_guids[g] = dst
            copied_deps += 1

    print(f"      Updated/imported {len(EXTRA_ENVIRONMENT)} environment prefabs + {copied_deps} new mesh/material/texture deps.", flush=True)


def import_gng_skill_vfx(guid_map, existing_guids):
    print("[3/4] Extracting & upgrading 30 authentic Gold and Glory Skill/Projectile/Hit VFX prefabs...", flush=True)
    os.makedirs(DST_GNG_VFX, exist_ok=True)
    vfx_mat_dir = os.path.join(DST_EXPORTED, "VFXMaterials")
    vfx_mesh_dir = os.path.join(DST_EXPORTED, "VFXMeshes")
    vfx_tex_dir = os.path.join(DST_EXPORTED, "VFXTextures")
    os.makedirs(vfx_mat_dir, exist_ok=True)
    os.makedirs(vfx_mesh_dir, exist_ok=True)
    os.makedirs(vfx_tex_dir, exist_ok=True)

    guid_re = re.compile(r"guid:\s*([0-9a-f]{32})")
    queue = []
    imported_vfx = []

    for out_name, (subdir, patterns) in GNG_VFX_MANIFEST.items():
        src = resolve_vfx_prefab_candidate(subdir, patterns)
        if not src or not os.path.isfile(src):
            print(f"  [WARN] Could not resolve VFX prefab for {out_name}")
            continue

        raw_yaml = open(src, "r", encoding="utf-8", errors="ignore").read()
        clean_yaml = sanitize_vfx_prefab_yaml(raw_yaml, out_name)
        dst = os.path.join(DST_GNG_VFX, f"{out_name}.prefab")

        old_meta = None
        if os.path.isfile(dst + ".meta"):
            old_meta = open(dst + ".meta", "r", encoding="utf-8", errors="ignore").read()
        safe_write_text(dst, clean_yaml)
        if old_meta:
            safe_write_text(dst + ".meta", old_meta)
        elif os.path.isfile(src + ".meta"):
            # Generate deterministic unique GUID for out_name so multiple aliases never collide!
            ug = f"{zlib.crc32(out_name.encode('utf-8')) & 0xFFFFFFFF:08x}a4bc4d2198ef001122334455"[:32]
            meta_txt = open(src + ".meta", "r", encoding="utf-8", errors="ignore").read()
            meta_txt = re.sub(r"guid:\s*[0-9a-f]{32}", f"guid: {ug}", meta_txt, count=1)
            safe_write_text(dst + ".meta", meta_txt)

        imported_vfx.append((out_name, os.path.basename(src)))
        for g in guid_re.findall(clean_yaml):
            if g not in existing_guids and g in guid_map:
                queue.append(g)

    copied_deps = 0
    while queue:
        g = queue.pop()
        if g in existing_guids or g not in guid_map:
            continue
        src = guid_map[g]
        ext = os.path.splitext(src)[1].lower()
        stem = re.sub(r"[^A-Za-z0-9_.-]", "_", os.path.splitext(os.path.basename(src))[0])
        short = g[:8]

        if ext == ".mat":
            dst = os.path.join(vfx_mat_dir, f"{stem}_{short}.mat")
            mat_txt = open(src, "r", encoding="utf-8", errors="ignore").read()
            urp_txt = upgrade_effect_material_to_urp(mat_txt, f"{stem}_{short}")
            safe_write_text(dst, urp_txt)
            if os.path.isfile(src + ".meta"):
                safe_write_text(dst + ".meta", open(src + ".meta", "r", encoding="utf-8", errors="ignore").read())
            existing_guids[g] = dst
            copied_deps += 1
            for dg in guid_re.findall(urp_txt):
                if dg != GNG_EFFECT_SHADER_GUID and dg not in existing_guids and dg in guid_map:
                    queue.append(dg)
        elif ext in (".fbx", ".obj", ".asset"):
            dst = os.path.join(vfx_mesh_dir, f"{stem}_{short}{ext}")
            copy_with_meta_preserve_guid(src, dst)
            existing_guids[g] = dst
            copied_deps += 1
            if ext == ".asset":
                try:
                    atxt = open(src, "r", encoding="utf-8", errors="ignore").read(65536)
                    for dg in guid_re.findall(atxt):
                        if dg not in existing_guids and dg in guid_map:
                            queue.append(dg)
                except Exception:
                    pass
        elif ext in (".png", ".tga", ".jpg", ".jpeg", ".psd", ".exr"):
            dst = os.path.join(vfx_tex_dir, f"{stem}_{short}{ext}")
            def _cap_vfx_tex_meta(mtxt: str) -> str:
                mtxt = re.sub(r"maxTextureSize:\s*\d+", "maxTextureSize: 512", mtxt)
                mtxt = re.sub(r"alphaIsTransparency:\s*\d+", "alphaIsTransparency: 1", mtxt)
                return mtxt
            copy_with_meta_preserve_guid(src, dst, meta_transform=_cap_vfx_tex_meta)
            existing_guids[g] = dst
            copied_deps += 1
        elif ext in (".anim", ".controller", ".overridecontroller"):
            dst = os.path.join(vfx_mesh_dir, f"{stem}_{short}{ext}")
            copy_with_meta_preserve_guid(src, dst)
            existing_guids[g] = dst
            copied_deps += 1
            if ext in (".controller", ".overridecontroller"):
                try:
                    ctxt = open(src, "r", encoding="utf-8", errors="ignore").read()
                    for dg in guid_re.findall(ctxt):
                        if dg not in existing_guids and dg in guid_map:
                            queue.append(dg)
                except Exception:
                    pass

    print(f"      Imported {len(imported_vfx)} GnG VFX prefabs + {copied_deps} VFX meshes/URP-materials/textures.", flush=True)
    for k, src_b in imported_vfx:
        print(f"        • {k} <= {src_b}")


def decode_expansion_hero_animations():
    print("[4/4] Decoding 70 multi-combo, heavy, skill, walk & CareerChoices lobby .anim clips...", flush=True)
    crc_table = {"0": ""}

    skeletons = [
        os.path.join(EP_ASSETS, r"AssetBox\ResFolder\Ingame\Character\RIG_G1P_SknM_0001_Skeleton.prefab"),
        os.path.join(EP_ASSETS, r"AssetBox\ResFolder\Ingame\Character\PFB_G1P_SknF_0001_Skeleton.prefab"),
        os.path.join(EP_ASSETS, r"AssetBox\ResFolder\Ingame\Character\G1P_InGameSknM_0001.prefab"),
        os.path.join(EP_ASSETS, r"AssetBox\ResFolder\Ingame\Character\G1P_InGameSknF_0001.prefab"),
    ]
    for p in skeletons:
        base_imp.collect_bone_paths_from_prefab(p, crc_table)
    for rel in base_imp.HERO_RIGS.values():
        base_imp.collect_bone_paths_from_prefab(os.path.join(EP_ASSETS, rel), crc_table)
    for rel in base_imp.WEAPONS.values():
        base_imp.collect_bone_paths_from_prefab(os.path.join(EP_ASSETS, rel), crc_table)

    anim_root_src = os.path.join(EP_ASSETS, r"AssetBox\ResFolder\Animation")
    path_re = re.compile(r"^(\s*path:\s*)(\d+)\s*$", re.MULTILINE)

    decoded = 0
    resolved_curves = 0

    for cls_name, cmap in HERO_EXPANSION_ANIMS.items():
        for state, rel in cmap.items():
            src = os.path.join(anim_root_src, rel)
            if not os.path.isfile(src):
                print(f"  [WARN] Missing expansion anim {cls_name}.{state}: {rel}")
                continue
            dst = os.path.join(DST_EXPORTED, "Animations", "Heroes", cls_name, f"Hero_{cls_name}_{state}.anim")
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            txt = open(src, "r", encoding="utf-8", errors="ignore").read()

            def repl_path(m):
                nonlocal resolved_curves
                prefix, num = m.group(1), m.group(2)
                if num in crc_table:
                    resolved_curves += 1
                    val = crc_table[num]
                    return f"{prefix}{val}" if val else f"{prefix}"
                return m.group(0)

            clip_name = f"Hero_{cls_name}_{state}"
            should_loop = state in ("Idle", "Walk", "Run", "Sprint", "LobbyIdle")
            txt = path_re.sub(repl_path, txt)
            txt = re.sub(r"m_Name:\s*[^\r\n]+", f"m_Name: {clip_name}", txt, count=1)
            txt = re.sub(r"m_LoopTime:\s*\d+", f"m_LoopTime: {'1' if should_loop else '0'}", txt)
            txt = re.sub(r"m_Events:\s*(?:\r?\n(?:  .*|\s*-.*))*", "m_Events: []\n", txt)

            safe_write_text(dst, txt)
            decoded += 1

    print(f"      Decoded & saved {decoded} Hero combo/skill/lobby .anim clips ({resolved_curves:,} bone curves resolved)!", flush=True)


def main():
    guid_map = base_imp.build_guid_index()
    existing_guids = collect_existing_project_guids()
    import_extra_environment_prefabs(guid_map, existing_guids)
    import_gng_skill_vfx(guid_map, existing_guids)
    decode_expansion_hero_animations()
    print("=== Gold and Glory Expansion Import Complete! ===", flush=True)


if __name__ == "__main__":
    main()

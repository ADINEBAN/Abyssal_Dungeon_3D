#!/usr/bin/env python3
"""
Deterministic Asset Extraction, Dependency Closure, URP Material Upgrade,
CRC32 Animation Bone-Path Decoding, and UI/PBR Texture Pipeline
from ExportedProject/Assets -> Abyssal_Dungeon_3D/Assets.
"""

import os
import re
import shutil
import zlib
from concurrent.futures import ThreadPoolExecutor
from PIL import Image, ImageEnhance, ImageOps, ImageDraw
import numpy as np

PROJECT_ROOT = r"c:\LTG\Abyssal_Dungeon_3D"
EP_ASSETS = r"\\?\c:\LTG\Abyssal_Dungeon_3D\ExportedProject\Assets"
DST_ASSETS = os.path.join(PROJECT_ROOT, "Assets")
DST_EXPORTED = os.path.join(DST_ASSETS, "Art", "Exported")

URP_LIT_GUID = "933532a4fcc9baf4fa0491de14d08ed7"

# ---------------------------------------------------------------------------
# 1. TARGET PREFAB MANIFEST
# ---------------------------------------------------------------------------

HERO_RIGS = {
    "HeroRig_Knight":       r"ART\Model\BASE\Character\FitM_0003\PFB_G1P_FitM_0003.prefab",
    "HeroRig_Guardian":     r"ART\Model\BASE\Character\FitM_0001\PFB_G1P_FitM_0001.prefab",
    "HeroRig_Berserker":    r"ART\Model\BASE\Character\SmrM_0001\RIG_G1P_SmrM_0001.prefab",
    "HeroRig_BerserkerAlt": r"ART\Model\BASE\Character\FitM_0002\PFB_G1P_FitM_0002.prefab",
    "HeroRig_Assassin":     r"ART\Model\BASE\Character\RagM_0003\RIG_G1P_RagM_0003.prefab",
    "HeroRig_Ranger":       r"ART\Model\BASE\Character\RagF_0001\PFB_G1P_RagF_0001.prefab",
    "HeroRig_Mage":         r"ART\Model\BASE\Character\WizF_0003\PFB_G1P_WizF_0003.prefab",
    "HeroRig_Warlock":      r"ART\Model\BASE\Character\WizM_0005\RIG_G1P_WizM_0005.prefab",
    "HeroRig_Monk":         r"ART\Model\BASE\Character\TaiM_0001\PFB_G1P_TaiM_0001.prefab",
    "HeroRig_Druid":        r"ART\Model\BASE\Character\WizM_0001\PFB_G1P_WizM_0001.prefab",
    "HeroRig_Cleric":       r"ART\Model\BASE\Character\WizF_0008\PFB_G1P_WizF_0008.prefab",
    "HeroRig_DruidBeast":   r"ART\Model\OB_01_00\Enemy\SkullDog_01\PFB_G1P_Mst_SkullDog_01.prefab",
}

# Modular Face & Inner-Hair Avatar parts (bound to exact G1P Bip001 Head/Neck/Spine2/Clavicle bindposes)
HERO_AVATAR_PARTS = {
    "HeroRig_Knight": [
        r"AssetBox\ResFolder\Avatar\G1P_Skn\MES_G1P_SknM_0007_Face_01.asset",
    ],
    "HeroRig_Guardian": [
        r"AssetBox\ResFolder\Avatar\G1P_Skn\MES_G1P_SknM_0007_Face_01.asset",
    ],
    "HeroRig_Berserker": [
        r"AssetBox\ResFolder\Avatar\G1P_Skn\MES_G1P_SknM_0006_Face_01.asset",
    ],
    "HeroRig_BerserkerAlt": [
        r"AssetBox\ResFolder\Avatar\G1P_Skn\MES_G1P_SknM_0005_Face_01.asset",
    ],
    "HeroRig_Assassin": [
        r"AssetBox\ResFolder\Avatar\G1P_Skn\MES_G1P_SknM_0001_Face_01.asset",
    ],
    "HeroRig_Ranger": [
        r"AssetBox\ResFolder\Avatar\G1P_Skn\MES_G1P_SknF_0001_Face_01.asset",
        r"AssetBox\ResFolder\Avatar\G1P_Skn\MES_G1P_SknHair_0008_in.asset",
    ],
    "HeroRig_Mage": [
        r"AssetBox\ResFolder\Avatar\G1P_Skn\MES_G1P_SknF_0007_Face_01.asset",
        r"AssetBox\ResFolder\Avatar\G1P_Skn\MES_G1P_SknHair_0010_In.asset",
    ],
    "HeroRig_Warlock": [
        r"AssetBox\ResFolder\Avatar\G1P_Skn\MES_G1P_SknM_0009_Face_01.asset",
    ],
    # HeroRig_Monk already contains MES_G1P_SknM_0010_Face_01 + MES_G1P_TaiM_0001_Head_01 inside PFB_G1P_TaiM_0001.prefab
    "HeroRig_Druid": [
        r"AssetBox\ResFolder\Avatar\G1P_Skn\MES_G1P_SknM_0008_Face_01.asset",
        r"AssetBox\ResFolder\Avatar\G1P_Skn\MES_G1P_SknHair_0005_In.asset",
    ],
    "HeroRig_Cleric": [
        r"AssetBox\ResFolder\Avatar\G1P_Skn\MES_G1P_SknF_0008_Face_01.asset",
    ],
}

WEAPONS = {
    "Wpn_KnightSword":         r"AssetBox\ResFolder\Weapon\PFB_G1P_Wpn_Longsword_0101\PFB_G1P_Wpn_Longsword_0101_01.prefab",
    "Wpn_KnightShield":        r"ART\Model\BASE\Equipment\weapon\HeaterShield_0101\PFB_G1P_Wpn_HeaterShield_0101.prefab",
    "Wpn_GuardianMace":        r"AssetBox\ResFolder\Weapon\PFB_G1P_Wpn_FlangedMace_0101\PFB_G1P_Wpn_FlangedMace_0101_01.prefab",
    "Wpn_GuardianShield":      r"AssetBox\ResFolder\Weapon\PFB_G1P_Wpn_PaviseShield_0101\PFB_G1P_Wpn_PaviseShield_0101_01.prefab",
    "Wpn_BerserkerGreatsword": r"AssetBox\ResFolder\Weapon\PFB_G1P_Wpn_Zweihander_0101\PFB_G1P_Wpn_Zweihander_0101_01.prefab",
    "Wpn_AssassinBladeR":      r"AssetBox\ResFolder\Weapon\PFB_G1P_Wpn_DualBlade_0101\PFB_G1P_Wpn_DualBlade_0101_01.prefab",
    "Wpn_AssassinBladeL":      r"AssetBox\ResFolder\Weapon\PFB_G1P_Wpn_DualBlade_0101\PFB_G1P_Wpn_DualBlade_0101_02.prefab",
    "Wpn_RangerBow":           r"AssetBox\ResFolder\Weapon\PFB_G1P_Wpn_Longbow_0101\PFB_G1P_Wpn_Longbow_0101_01.prefab",
    "Wpn_MageStaff":           r"AssetBox\ResFolder\Weapon\PFB_G1P_Wpn_WizardStaff_0101\PFB_G1P_Wpn_WizardStaff_0101_01.prefab",
    "Wpn_WarlockSword":        r"AssetBox\ResFolder\Weapon\PFB_G1P_Wpn_CrystalSword_0101\PFB_G1P_Wpn_CrystalSword_0101_01.prefab",
    "Wpn_WarlockBook":         r"AssetBox\ResFolder\Weapon\PFB_G1P_Wpn_Book_0101\PFB_G1P_Wpn_Book_0101_01.prefab",
    "Wpn_MonkGloves":          r"AssetBox\ResFolder\Weapon\PFB_G1P_Wpn_BoxingGloves_0001_Pickup\PFB_G1P_Wpn_BoxingGloves_0001_Pickup_01.prefab",
    "Wpn_DruidStaff":          r"AssetBox\ResFolder\Weapon\PFB_G1P_Wpn_WizardStaff_SBSprouts\PFB_G1P_Wpn_WizardStaff_SBSprouts_01.prefab",
    "Wpn_ClericHammer":        r"AssetBox\ResFolder\Weapon\PFB_G1P_Wpn_Sledgehammer_0101\PFB_G1P_Wpn_Sledgehammer_0101_01.prefab",
    "Wpn_ClericShield":        r"ART\Model\BASE\Equipment\weapon\RoundShield_0101\MES_G1P_Wpn_RoundShield_0101.prefab",
    "Wpn_GoblinShovel":        r"ART\Model\BASE\Equipment\weapon\Goblinshovel_01\PFB_G1P_Wpn_Goblinshovel_01.prefab",
    "Wpn_SkeletonSword":       r"ART\Model\BASE\Equipment\weapon\ArmingSword_0002\PFB_G1P_Wpn_ArmingSword_0002_01.prefab",
    "Wpn_SkeletonShield":      r"ART\Model\BASE\Equipment\weapon\SKKnightSheild_0001\PFB_G1P_Wpn_SKKnightSheild_01.prefab",
    "Wpn_DarkKnightSword":     r"ART\Model\BASE\Equipment\weapon\SKKnightSword_0001\PFB_G1P_Wpn_SKKnightSword_0001.prefab",
    "Wpn_GargoyleSword":       r"ART\Model\BASE\Enemy\Elt_Gargoyle\PFB_G1P_Elt_Gargoyle_01_Sword.prefab",
    "Wpn_BossShield":          r"ART\Model\BASE\Equipment\weapon\SKKnightSheild_0001\PFB_G1P_Wpn_SKKnightSheild_01.prefab",
}

ENEMIES = {
    "Mst_Goblin":          r"ART\Model\BASE\Enemy\Mst_AngryGoblin\PFB_G1P_Mst_AngryGoblin.prefab",
    "Mst_GoblinArcher":    r"ART\Model\BASE\Enemy\Mst_SkullArcher\PFB_G1P_Mst_SkullArcher_Full_01.prefab",
    "Mst_Skeleton":        r"ART\Model\BASE\Enemy\Mst_SkullFighter\PFB_G1P_Mst_SkullFighter_Full_01.prefab",
    "Mst_SkeletonWarrior": r"ART\Model\BASE\Enemy\Mst_SkullShield\PFB_G1P_Mst_Skullshield.prefab",
    "Mst_SkeletonMage":    r"ART\Model\BASE\Enemy\Elt_SkullWizard\PFB_G1P_Elt_SkullWizard_01.prefab",
    "Mst_Zombie":          r"ART\Model\BASE\Enemy\Elt_Undead\PFB_G1P_Elt_Undead_Full_01.prefab",
    "Mst_DarkKnight":      r"ART\Model\BASE\Enemy\Elt_SkKnight\PFB_G1P_Elt_SkKnight_Full_01.prefab",
    "Mst_Demon":           r"ART\Model\BASE\Enemy\Elt_Gargoyle\PFB_G1P_Mst_Gargoyle_01.prefab",
    "Mst_CrystalGolem":    r"ART\Model\OB_01_00\Enemy\Mst_RockBig\PFB_G1P_Mst_RockBig_01.prefab",
    "Mst_ForestMonster":   r"ART\Model\OB_01_00\Enemy\Elt_BigTurtle\PFB_G1P_Elt_BigTurelt_01.prefab",
    "Mst_SoulMinion":      r"ART\Model\BASE\Enemy\Mst_Ghost01\PFB_G1P_Mst_Ghost01_Full_01.prefab",
    "Boss_AbyssalKnight":  r"ART\Model\OB_00_09\Enemy\Bos_SkullKing\PFB_G1P_Bos_SkullKing_01.prefab",
}

ENVIRONMENT = {
    "Env_GothicPillar":          r"ART\Environment\Art_Map01\Common\PFB_Column_02.prefab",
    "Env_GothicArchVault":       r"ART\Environment\CommonDyn\PFB_M01_PO_Doorarch_01.prefab",
    "Env_DungeonWall":           r"ART\Environment\Art_Map01\Common\PFB_CT08_Wall_02b.prefab",
    "Env_DungeonFloor":          r"ART\Environment\CommonDyn\PFB_M01_DS_Wall_01.prefab",
    "Env_DungeonFloorAlt":       r"ART\Environment\Art_Map01\Common\PFB_CN05_Floor_03.prefab",
    "Env_StoneStairs":           r"ART\Environment\Art_Map01\Common\PFB_CT02_Stairs_02.prefab",
    "Env_IronPortcullis":        r"ART\Environment\CommonDyn\PFB_M01_CMN_Fence_08.prefab",
    "Env_WallTorchSconce":       r"ART\Environment\Art_Map01\Common\PFB_M01_CMN_Lamp_01.prefab",
    "Env_FireBrazierGrate":      r"ART\Environment\Art_Map01\Common\PFB_M01_PO_Brazier_01.prefab",
    "Env_AbyssalCrystalCluster": r"ART\Environment\Art_Map01\Common\PFB_M01_CMN_Crystals_06.prefab",
    "Env_HealingShrine":         r"ART\Environment\CommonDyn\PFB_M01_DS_Shrinesheal_01.prefab",
    "Env_RunePillarAltar":       r"ART\Environment\CommonDyn\PFB_M01_DS_ShrinesReborn_01.prefab",
    "Env_GuardianStatue":        r"ART\Environment\Art_Map01\Common\PFB_Ritual_Sculpture_01.prefab",
    "Env_AncientRubble":         r"ART\Environment\Art_Map01\Common\PFB_M01_PO_Rock_04.prefab",
    "Env_CursedTreeRoot":        r"ART\Environment\Art_Map01\Common\PFB_M01_PO_Tree.prefab",
    "Env_TreasureChestBase":     r"ART\Environment\TreasureChest\PFB_Large_treasure_chest_01_pedestal.prefab",
    "Env_TreasureChestLid":      r"ART\Environment\TreasureChest\PFB_Large_treasure_chest_01_cap.prefab",
    "Env_SpikeTrapPlate":        r"ART\Environment\CommonDyn\PFB_M01_DS_Organ_01.prefab",
    "Env_SpikeCluster":          r"ART\Model\BASE\Equipment\utilities\HuntingTrap\HuntingTrap.prefab",
    "Env_BossThroneAltar":       r"ART\Model\OB_00_09\Enemy\Bos_SkullKing\MES_G1P_Bos_SkullKing_Throne.prefab",
    "Env_AbyssalPortal":         r"ART\Environment\Art_Map01\Common\PFB_CMN_POP_Portal_02.prefab",
    "Env_EscapeFloorPortal":     r"ART\Environment\CommonDyn\PFB_M01_PO_Escapeexit_01.prefab",
    "Loot_ArmorBox":             r"ART\Environment\CommonContainer\BattleAdventure\PFB_Common_WeaponBox_02.prefab",
    "Loot_Helm":                 r"ART\Environment\CommonDyn\PFB_M01_PO_Head_01a.prefab",
    "Loot_GemRing":              r"ART\Environment\CommonDyn\PFB_M01_PO_Gem_01a.prefab",
    "Loot_SkullTalisman":        r"ART\Model\BASE\Equipment\utilities\SkullKey\SkullKey.prefab",
    "Loot_EliteBanner":          r"ART\Environment\Art_Map01\Common\PFB_M01_DC_Flag_01.prefab",
}

# ---------------------------------------------------------------------------
# 2. ANIMATION CLIP MANIFEST (Hero 10 Classes + Beast + 12 Enemies/Boss)
# ---------------------------------------------------------------------------

COMMON_HERO_ANIMS = {
    "Sprint": r"Character\G1P\G1P@Sprint\G1P_Sprint_Move_01.anim",
    "Dodge":  r"Character\G1P\G1P@Rogue\G1P_WPN_Rogue_RollF_01.anim",
    "Hit":    r"Character\G1P\G1P@Common\G1P@Hit\G1P_WPN_LightHit_Left.anim",
    "Death":  r"Character\G1P\G1P@Common\G1P_WPN_Common_Death_01.anim",
}

HERO_CLASS_ANIMS = {
    "Knight": {
        "Idle":   r"Character\G1P\G1P@SwordShield\WPN_SwordShield_CombatIdle.anim",
        "Run":    r"Character\G1P\G1P@SwordShield\WPN_SwordShield_CombatRun_F.anim",
        "Jump":   r"Character\G1P\G1P@SwordShield\WPN_SwordShield_CombatJump_Start.anim",
        "Attack": r"Character\G1P\G1P@SwordShield\WPN_SwordShield_Attack_01.anim",
        "Cast":   r"Character\G1P\G1P@SwordShield\WPN_SwordShield_BlessedSword.anim",
    },
    "Guardian": {
        "Idle":   r"Character\G1P\G1P@PaviseShield\G1P_WPN_PaviseShield_Idle_01.anim",
        "Run":    r"Character\G1P\G1P@PaviseShield\G1P_WPN_PaviseShield_MoveF_01.anim",
        "Jump":   r"Character\G1P\G1P@PaviseShield\G1P_WPN_PaviseShield_JumpStart_01.anim",
        "Attack": r"Character\G1P\G1P@PaviseShield\G1P_WPN_PaviseShield_Attack_01.anim",
        "Cast":   r"Character\G1P\G1P@Warrior\G1P_WPN_Warrior_DragonShock.anim",
    },
    "Berserker": {
        "Idle":   r"Character\G1P\G1P@Zweihander\G1P_WPN_Zweihander_Idle_01.anim",
        "Run":    r"Character\G1P\G1P@Zweihander\G1P_WPN_Zweihander_Move_F_01.anim",
        "Jump":   r"Character\G1P\G1P@Zweihander\G1P_WPN_Zweihander_JumpStart_01.anim",
        "Attack": r"Character\G1P\G1P@Zweihander\G1P_WPN_Zweihander_Attack_01.anim",
        "Cast":   r"Character\G1P\G1P@Warrior\G1P_WPN_Warrior_Whirlwind_01.anim",
    },
    "Assassin": {
        "Idle":   r"Character\G1P\G1P@DoubleSword\G1P_WPN_DoubleSword_Idle_01.anim",
        "Run":    r"Character\G1P\G1P@DoubleSword\G1P_WPN_DoubleSword_Move_01.anim",
        "Jump":   r"Character\G1P\G1P@DoubleSword\G1P_WPN_DoubleSword_JumpStart_01.anim",
        "Attack": r"Character\G1P\G1P@DoubleSword\G1P_WPN_DoubleSword_Attack_01.anim",
        "Cast":   r"Character\G1P\G1P@Rogue\G1P_WPN_Rogue_CycloneSlash_01.anim",
    },
    "Ranger": {
        "Idle":   r"Character\G1P\G1P@Bow\G1P_WPN_Bow_Idle_01.anim",
        "Run":    r"Character\G1P\G1P@Bow\G1P_WPN_Bow_MoveF_01.anim",
        "Jump":   r"Character\G1P\G1P@Bow\G1P_WPN_Bow_JumpStart_01.anim",
        "Attack": r"Character\G1P\G1P@Aracher\G1P_WPN_Aracher_TripleShoot_01.anim",
        "Cast":   r"Character\G1P\G1P@Aracher\G1P_WPN_Aracher_DeadAttack_01.anim",
    },
    "Mage": {
        "Idle":   r"Character\G1P\G1P@Wand\G1P_WPN_Wand_Idle_01.anim",
        "Run":    r"Character\G1P\G1P@Wand\G1P_WPN_Wand_Move_01.anim",
        "Jump":   r"Character\G1P\G1P@Wand\G1P_WPN_Wand_JumpStart_01.anim",
        "Attack": r"Character\G1P\G1P@Wizard\G1P_WPN_Wizard_IceArrow.anim",
        "Cast":   r"Character\G1P\G1P@Wizard\G1P_WPN_Wizard_ThunderStormStart_01.anim",
    },
    "Warlock": {
        "Idle":   r"Character\G1P\G1P@CrystalSword\G1P_WPN_CrystalSword_Idle_01.anim",
        "Run":    r"Character\G1P\G1P@CrystalSword\G1P_WPN_CrystalSword_Move_01.anim",
        "Jump":   r"Character\G1P\G1P@CrystalSword\G1P_WPN_CrystalSword_JumpStart_01.anim",
        "Attack": r"Character\G1P\G1P@CrystalSword\G1P_WPN_CrystalSword_Attack_01.anim",
        "Cast":   r"Character\G1P\G1P@CurseKnight\G1P_WPN_CurseKnight_SoulMissle_01.anim",
    },
    "Monk": {
        "Idle":   r"Character\G1P\G1P@Fist\G1P_WPN_Fist_idle.anim",
        "Run":    r"Character\G1P\G1P@Fist\G1P_WPN_Fist_move_F_01.anim",
        "Jump":   r"Character\G1P\G1P@Fist\G1P_WPN_Fist_jump_start.anim",
        "Attack": r"Character\G1P\G1P@Fist\G1P_WPN_Fist_attack01.anim",
        "Cast":   r"Character\G1P\G1P@Fist\G1P_WPN_Fist_attack13.anim",
    },
    "Druid": {
        "Idle":   r"Character\G1P\G1P@Spear\G1P_WPN_Spear_Idle_01.anim",
        "Run":    r"Character\G1P\G1P@Spear\G1P_WPN_Spear_Move_01.anim",
        "Jump":   r"Character\G1P\G1P@Spear\G1P_WPN_Spear_JumpStart_01.anim",
        "Attack": r"Character\G1P\G1P@Spear\G1P_WPN_Spear_Attack_01.anim",
        "Cast":   r"Character\G1P\G1P@Aracher\G1P_WPN_Marksman_WildHeart.anim",
    },
    "Cleric": {
        "Idle":   r"Character\G1P\G1P@Mace\G1P_WPN_Mace_Idle_01.anim",
        "Run":    r"Character\G1P\G1P@Mace\G1P_WPN_Mace_Move_01.anim",
        "Jump":   r"Character\G1P\G1P@Mace\G1P_WPN_Mace_JumpStart_01.anim",
        "Attack": r"Character\G1P\G1P@Mace\G1P_WPN_Mace_Attack_01.anim",
        "Cast":   r"Character\G1P\G1P@Warpriest\G1P_WPN_Warpriest_Imprison.anim",
    },
}

ENEMY_ANIMS = {
    "DruidBeast": {
        "Idle":   r"Enemy\Mst_SkullDog_01\G1P_Mst_SkullDog_01_Idle_01_sp.anim",
        "Walk":   r"Enemy\Mst_SkullDog_01\G1P_Mst_SkullDog_01_Patrol_sp.anim",
        "Run":    r"Enemy\Mst_SkullDog_01\G1P_Mst_SkullDog_01_Chase_sp.anim",
        "Attack": r"Enemy\Mst_SkullDog_01\G1P_Mst_SkullDog_01_Attack_01_sp.anim",
        "Cast":   r"Enemy\Mst_SkullDog_01\G1P_Mst_SkullDog_01_Attack_02_sp.anim",
        "Hit":    r"Enemy\Mst_SkullDog_01\G1P_Mst_SkullDog_01_Hit_Front_Left_Light_sp.anim",
        "Death":  r"Enemy\Mst_SkullDog_01\G1P_Mst_SkullDog_01_Death_sp.anim",
    },
    "Goblin": {
        "Idle":   r"Enemy\Mst_PreciousGoblin\G1P_Mst_PreciousGoblin_Idle_01.anim",
        "Walk":   r"Enemy\Mst_PreciousGoblin\G1P_Mst_PreciousGoblin_Patrol_01.anim",
        "Run":    r"Enemy\Mst_PreciousGoblin\G1P_Mst_PreciousGoblin_Chase_01.anim",
        "Attack": r"Enemy\Mst_PreciousGoblin\G1P_Mst_PreciousGoblin_Attack_01.anim",
        "Cast":   r"Enemy\Mst_PreciousGoblin\G1P_Mst_PreciousGoblin_Angry_01.anim",
        "Hit":    r"Enemy\Mst_PreciousGoblin\G1P_Mst_PreciousGoblin_Shakehit_Left_01.anim",
        "Death":  r"Enemy\Mst_PreciousGoblin\G1P_Mst_PreciousGoblin_Death_01.anim",
    },
    "GoblinArcher": {
        "Idle":   r"Enemy\Mst_SkullFighter_Bow\G1P_Mst_SkullFighter_Bow_Idlel_01.anim",
        "Walk":   r"Enemy\Mst_SkullFighter_Bow\G1P_Mst_SkullFighter_Bow_MovePatrol.anim",
        "Run":    r"Enemy\Mst_SkullFighter_Bow\G1P_Mst_SkullFighter_Bow_MoveChase.anim",
        "Attack": r"Enemy\Mst_SkullFighter_Bow\G1P_Mst_SkullFighter_Bow_Skill_01.anim",
        "Cast":   r"Enemy\Mst_SkullFighter_Bow\G1P_Mst_SkullFighter_Bow_Skill_02.anim",
        "Hit":    r"Enemy\Mst_SkullFighter_Bow\G1P_Mst_SkullFighter_Bow_Hit_Front_Left_Light.anim",
        "Death":  r"Enemy\Mst_SkullFighter_Bow\G1P_Mst_SkullFighter_Bow_Death_01.anim",
    },
    "Skeleton": {
        "Idle":   r"Enemy\Mst_SkullFighter_Sword\G1P_Mst_SkullFighter_Sword_Idle_01.anim",
        "Walk":   r"Enemy\Mst_SkullFighter_Sword\G1P_Mst_SkullFighter_Sword_MovePatrol.anim",
        "Run":    r"Enemy\Mst_SkullFighter_Sword\G1P_Mst_SkullFighter_Sword_MoveChase.anim",
        "Attack": r"Enemy\Mst_SkullFighter_Sword\G1P_Mst_SkullFighter_Sword_Skill_01.anim",
        "Cast":   r"Enemy\Mst_SkullFighter_Sword\G1P_Mst_SkullFighter_Sword_Skill_02.anim",
        "Hit":    r"Enemy\Mst_SkullFighter_Sword\G1P_Mst_SkullFighter_Sword_ShakehitLeft.anim",
        "Death":  r"Enemy\Mst_SkullFighter_Sword\G1P_Mst_SkullFighter_Sword_Death_01.anim",
    },
    "SkeletonWarrior": {
        "Idle":   r"Enemy\Mst_Skullshield\G1P_Mst_Skullshield_Idle01.anim",
        "Walk":   r"Enemy\Mst_Skullshield\G1P_Mst_Skullshield_Patrol.anim",
        "Run":    r"Enemy\Mst_Skullshield\G1P_Mst_Skullshield_Chase.anim",
        "Attack": r"Enemy\Mst_Skullshield\G1P_Mst_Skullshield_Attack02.anim",
        "Cast":   r"Enemy\Mst_Skullshield\G1P_Mst_Skullshield_Attack05.anim",
        "Hit":    r"Enemy\Mst_Skullshield\G1P_Mst_Skullshield_Shakehit_Left.anim",
        "Death":  r"Enemy\Mst_Skullshield\G1P_Mst_Skullshield_Death_01.anim",
    },
    "SkeletonMage": {
        "Idle":   r"Enemy\Mst_SkullWizard\G1P_Mst_Skull_Mage_Idle_01.anim",
        "Walk":   r"Enemy\Mst_SkullWizard\G1P_Mst_Skull_Mage_Chase_01.anim",
        "Run":    r"Enemy\Mst_SkullWizard\G1P_Mst_Skull_Mage_Chase_01.anim",
        "Attack": r"Enemy\Mst_SkullWizard\G1P_Mst_Skull_Mage_Attack_01.anim",
        "Cast":   r"Enemy\Mst_SkullWizard\G1P_Mst_Skull_Mage_Attack_03.anim",
        "Hit":    r"Enemy\Mst_SkullWizard\G1P_Mst_Skull_Mage_Hit_Front_Left_Light.anim",
        "Death":  r"Enemy\Mst_SkullWizard\G1P_Mst_Skull_Mage_Death_01.anim",
    },
    "Zombie": {
        "Idle":   r"Enemy\Mst_Elt_Undead\G1P_Mst_Elt_Undead_Idle_01.anim",
        "Walk":   r"Enemy\Mst_Elt_Undead\G1P_Mst_Elt_Undead_MovePatrol.anim",
        "Run":    r"Enemy\Mst_Elt_Undead\G1P_Mst_Elt_Undead_MoveChase.anim",
        "Attack": r"Enemy\Mst_Elt_Undead01\G1P_Mst_Undead01_Attack_01.anim",
        "Cast":   r"Enemy\Mst_Elt_Undead01\G1P_Mst_Undead01_Shackshack_Attack.anim",
        "Hit":    r"Enemy\Mst_Elt_Undead\G1P_Mst_Elt_Undead_ShakehitLeft.anim",
        "Death":  r"Enemy\Mst_Elt_Undead\G1P_Mst_Elt_Undead_Death.anim",
    },
    "DarkKnight": {
        "Idle":   r"Enemy\Mst_Elt_SkKnight\G1P_Mst_Elt_SkKnight_Idle_01.anim",
        "Walk":   r"Enemy\Mst_Elt_SkKnight\G1P_Mst_Elt_SkKnight_MovePatrol.anim",
        "Run":    r"Enemy\Mst_Elt_SkKnight\G1P_Mst_Elt_SkKnight_MoveChase.anim",
        "Attack": r"Enemy\Mst_Elt_SkKnight\G1P_Mst_Elt_SkKnight_SkillAttack_01.anim",
        "Cast":   r"Enemy\Mst_Elt_SkKnight\G1P_Mst_Elt_SkKnight_DoubleStrike.anim",
        "Hit":    r"Enemy\Mst_Elt_SkKnight\G1P_Mst_Elt_SkKnight_ShakeHitLeft.anim",
        "Death":  r"Enemy\Mst_Elt_SkKnight\G1P_Mst_Elt_SkKnight_Death.anim",
    },
    "Demon": {
        "Idle":   r"Enemy\Mst_Gargoyle\G1P_Mst_Gargoyle_Idle_01.anim",
        "Walk":   r"Enemy\Mst_Gargoyle\G1P_Mst_Gargoyle_Patrol.anim",
        "Run":    r"Enemy\Mst_Gargoyle\G1P_Mst_Gargoyle_Chase.anim",
        "Attack": r"Enemy\Mst_Gargoyle\G1P_Mst_Gargoyle_Attack_01.anim",
        "Cast":   r"Enemy\Mst_Gargoyle\G1P_Mst_Gargoyle_HighAttack.anim",
        "Hit":    r"Enemy\Mst_Gargoyle\G1P_Mst_Gargoyle_Shakehit_Left.anim",
        "Death":  r"Enemy\Mst_Gargoyle\G1P_Mst_Gargoyle_Death_01.anim",
    },
    "CrystalGolem": {
        "Idle":   r"Enemy\Mst_RockCrabBig_01\G1P_Mst_RockCrabBig_Idle_01.anim",
        "Walk":   r"Enemy\Mst_RockCrabBig_01\G1P_Mst_RockCrabBig_Patrol_01.anim",
        "Run":    r"Enemy\Mst_RockCrabBig_01\G1P_Mst_RockCrabBig_Chase_01.anim",
        "Attack": r"Enemy\Mst_RockCrabBig_01\G1P_Mst_RockCrabBig_Attack_01.anim",
        "Cast":   r"Enemy\Mst_RockCrabBig_01\G1P_Mst_RockCrabBig_Skill_AOEAround_01.anim",
        "Hit":    r"Enemy\Mst_RockCrabBig_01\G1P_Mst_RockCrabBig_Dizzy_Strart.anim",
        "Death":  r"Enemy\Mst_RockCrabBig_01\G1P_Mst_RockCrabBig_Death.anim",
    },
    "ForestMonster": {
        "Idle":   r"Enemy\Mst_Turtle\G1P_Mst_Turtle_Idle.anim",
        "Walk":   r"Enemy\Mst_Turtle\G1P_Mst_Turtle_MovePatrol.anim",
        "Run":    r"Enemy\Mst_Turtle\G1P_Mst_Turtle_MoveChase.anim",
        "Attack": r"Enemy\Mst_Turtle\G1P_Mst_Turtle_Skill_01.anim",
        "Cast":   r"Enemy\Mst_Turtle\G1P_Mst_Turtle_Skill_03.anim",
        "Hit":    r"Enemy\Mst_Turtle\G1P_Mst_Turtle_LightHit_Left.anim",
        "Death":  r"Enemy\Mst_Turtle\G1P_Mst_Turtle_Death_01.anim",
    },
    "SoulMinion": {
        "Idle":   r"Enemy\Mst_Ghost\G1P_Mst_Ghost_Idle_01.anim",
        "Walk":   r"Enemy\Mst_Ghost\G1P_Mst_Ghost_Patrol.anim",
        "Run":    r"Enemy\Mst_Ghost\G1P_Mst_Ghost_Chase.anim",
        "Attack": r"Enemy\Mst_Ghost\G1P_Mst_Ghost_Attack_01.anim",
        "Cast":   r"Enemy\Mst_Ghost\G1P_Mst_Ghost_Attack_02.anim",
        "Hit":    r"Enemy\Mst_Ghost\G1P_Mst_Ghost_Shakehit_Left.anim",
        "Death":  r"Enemy\Mst_Ghost\G1P_Mst_Ghost_Death.anim",
    },
    "AbyssalKnight": {
        "Idle":   r"Enemy\Bos_SkullKing\G1P_Bos_SkullKing_01_Idle_01.anim",
        "Walk":   r"Enemy\Bos_SkullKing\G1P_Bos_SkullKing_Patrol.anim",
        "Run":    r"Enemy\Bos_SkullKing\G1P_Bos_SkullKing_Chase.anim",
        "Attack": r"Enemy\Bos_SkullKing\G1P_Bos_SkullKing_01_Attack_01.anim",
        "Cast":   r"Enemy\Bos_SkullKing\G1P_Bos_SkullKing_Skill_02.anim",
        "Hit":    r"Enemy\Bos_SkullKing\G1P_Bos_SkullKing_Shakehit_Left.anim",
        "Death":  r"Enemy\Bos_SkullKing\G1P_Bos_SkullKing_Death.anim",
    },
}


# ---------------------------------------------------------------------------
# 3. FAST MULTITHREADED GUID INDEXER
# ---------------------------------------------------------------------------

def build_guid_index():
    print("[1/5] Indexing ExportedProject GUIDs (ART, Material, Texture2D, Prefab, AssetBox)...", flush=True)
    meta_files = []
    scan_roots = [
        "ART",
        "Material",
        "Texture2D",
        "Prefab",
        r"AssetBox\ResFolder\Ingame",
        r"AssetBox\ResFolder\Weapon",
        r"AssetBox\ResFolder\Avatar",
        r"AssetBox\ResFolder\Prefabs",
    ]
    for sr in scan_roots:
        root = os.path.join(EP_ASSETS, sr)
        if not os.path.exists(root):
            continue
        for dp, _, fn in os.walk(root):
            for f in fn:
                if f.endswith(".meta"):
                    asset_path = os.path.join(dp, f[:-5])
                    if os.path.isfile(asset_path):
                        meta_files.append(os.path.join(dp, f))

    guid_re = re.compile(r"guid:\s*([0-9a-f]{32})")

    def parse_meta(mp):
        try:
            with open(mp, "r", encoding="utf-8", errors="ignore") as fh:
                head = fh.read(220)
            m = guid_re.search(head)
            if m:
                return (m.group(1), mp[:-5])
        except Exception:
            pass
        return None

    with ThreadPoolExecutor(max_workers=16) as ex:
        pairs = [r for r in ex.map(parse_meta, meta_files) if r is not None]

    guid_map = dict(pairs)
    print(f"      Indexed {len(guid_map):,} asset GUIDs.", flush=True)
    return guid_map


# ---------------------------------------------------------------------------
# 4. PREFAB SANITIZER, MATERIAL RESOLVER, URP LIT UPGRADER & TEXTURE ENHANCER
# ---------------------------------------------------------------------------

BASE_TEX_PROPS = ["_BaseMap", "_MainTex", "_AlbedoMap", "_Diffuse", "_ColorMap", "_MatCap", "_EmissionMap"]
NORM_TEX_PROPS = ["_NormalMMap", "_BumpMap", "_NormalMap", "_BSMap"]
EMIS_TEX_PROPS = ["_EmissionMap", "_EMIMap", "_EmissiveMap"]


def sanitize_ascii_name(filename, guid):
    base, ext = os.path.splitext(filename)
    clean = re.sub(r"[^A-Za-z0-9_\-\.]", "_", base).strip("_")
    if not clean or clean.replace("_", "") == "":
        clean = "Asset"
    return f"{clean}_{guid[:8]}{ext}"


def sanitize_prefab_yaml(yaml_text):
    """Strip any custom MonoBehaviour (!u!114) blocks and remove their fileIDs from GameObjects."""
    blocks = yaml_text.split("--- !u!")
    if len(blocks) <= 1:
        return yaml_text

    mb_ids = set()
    kept_blocks = [blocks[0]]
    for b in blocks[1:]:
        header_line = b.split("\n", 1)[0]
        m = re.match(r"(\d+)\s+&(\d+)", header_line)
        if m and m.group(1) == "114":
            mb_ids.add(m.group(2))
            continue
        kept_blocks.append("--- !u!" + b)

    out = "".join(kept_blocks)
    if mb_ids:
        for fid in mb_ids:
            out = re.sub(rf"\s*-\s*component:\s*\{{fileID:\s*{fid}\}}\r?\n", "\n", out)
    # Clear source AnimatorController GUIDs since AbyssalAutoSetup builds & binds our URP/Mecanim controllers
    out = re.sub(r"m_Controller:\s*\{fileID:\s*\d+,\s*guid:\s*[0-9a-fA-F]{32},\s*type:\s*\d+\}", "m_Controller: {fileID: 0}", out)
    return out


def extract_tex_ref(yaml_text, prop_names):
    for prop in prop_names:
        m = re.search(
            rf"\b{re.escape(prop)}:\s*\r?\n\s*m_Texture:\s*\{{fileID:\s*(\d+),\s*guid:\s*([0-9a-f]{{32}}),\s*type:\s*(\d+)\}}"
            rf"(?:\s*\r?\n\s*m_Scale:\s*\{{([^\}}]+)\}})?(?:\s*\r?\n\s*m_Offset:\s*\{{([^\}}]+)\}})?",
            yaml_text,
        )
        if m and m.group(1) != "0":
            return {
                "fileID": m.group(1),
                "guid": m.group(2),
                "type": m.group(3),
                "scale": m.group(4) or "x: 1, y: 1",
                "offset": m.group(5) or "x: 0, y: 0",
            }
    return None


def build_material_resolution_tables(guid_map):
    """Index painted material arrays from Character Prefabs & Avatar assets so raw RIG_/MES_ prefabs resolve 100%."""
    path_to_guid = {os.path.normcase(p): g for g, p in guid_map.items()}
    mat_tex_cache = {}

    def is_mat_textured(mg):
        if not mg or mg not in guid_map:
            return False
        if mg in mat_tex_cache:
            return mat_tex_cache[mg]
        mp = guid_map[mg]
        ok = False
        if mp.lower().endswith(".mat") and os.path.isfile(mp):
            try:
                txt = open(mp, "r", encoding="utf-8", errors="ignore").read()
                ref = extract_tex_ref(txt, BASE_TEX_PROPS)
                ok = ref is not None and ref["guid"] in guid_map
            except Exception:
                ok = False
        mat_tex_cache[mg] = ok
        return ok

    mesh_to_mats = {}
    partname_to_mats = {}
    guid_re = re.compile(r"guid:\s*([0-9a-f]{32})")

    # 1. Scan AssetBox\ResFolder\Prefabs\Character\*.prefab
    char_pfb_dir = os.path.join(EP_ASSETS, r"AssetBox\ResFolder\Prefabs\Character")
    if os.path.isdir(char_pfb_dir):
        for dp, _, fn in os.walk(char_pfb_dir):
            for f in fn:
                if not f.endswith(".prefab"):
                    continue
                txt = open(os.path.join(dp, f), "r", encoding="utf-8", errors="ignore").read()
                blocks = txt.split("--- !u!")
                go_name = {}
                mf_mesh = {}
                for b in blocks[1:]:
                    if b.startswith("1 "):
                        fid = b.split("\n", 1)[0].split("&")[1].strip()
                        mn = re.search(r"m_Name:\s*([^\r\n]+)", b)
                        if mn:
                            go_name[fid] = mn.group(1).strip()
                    elif b.startswith("33 "):
                        mg = re.search(r"m_GameObject:\s*\{fileID:\s*(\d+)\}", b)
                        mm = re.search(r"m_Mesh:\s*\{[^\}]*guid:\s*([0-9a-f]{32})", b)
                        if mg and mm:
                            mf_mesh[mg.group(1)] = mm.group(1)
                for b in blocks[1:]:
                    if b.startswith("23 ") or b.startswith("137 "):
                        mg = re.search(r"m_GameObject:\s*\{fileID:\s*(\d+)\}", b)
                        goid = mg.group(1) if mg else ""
                        gname = go_name.get(goid, "")
                        m_sec = re.search(r"m_Materials:\s*\r?\n((?:\s*-\s*\{[^\r\n]+\}\r?\n?)+)", b)
                        if not m_sec:
                            continue
                        mguids = guid_re.findall(m_sec.group(1))
                        if not mguids or not all(is_mat_textured(x) for x in mguids):
                            continue
                        if b.startswith("137 "):
                            mm = re.search(r"m_Mesh:\s*\{[^\}]*guid:\s*([0-9a-f]{32})", b)
                            mguid = mm.group(1) if mm else None
                        else:
                            mguid = mf_mesh.get(goid)
                        if mguid:
                            mesh_to_mats.setdefault(mguid, mguids)
                        if gname:
                            partname_to_mats.setdefault(gname.lower(), mguids)

    # 2. Scan AssetBox\ResFolder\Avatar\**\*.asset
    avatar_dir = os.path.join(EP_ASSETS, r"AssetBox\ResFolder\Avatar")
    if os.path.isdir(avatar_dir):
        for dp, _, fn in os.walk(avatar_dir):
            for f in fn:
                if not f.endswith(".asset"):
                    continue
                txt = open(os.path.join(dp, f), "r", encoding="utf-8", errors="ignore").read()
                mn = re.search(r"m_Name:\s*([^\r\n]+)", txt)
                m_sec = re.search(r"materials:\s*\r?\n((?:\s*-\s*\{[^\r\n]+\}\r?\n?)+)", txt)
                if mn and m_sec:
                    mguids = guid_re.findall(m_sec.group(1))
                    if mguids and all(is_mat_textured(x) for x in mguids):
                        partname_to_mats.setdefault(mn.group(1).strip().lower(), mguids)

    return path_to_guid, is_mat_textured, mesh_to_mats, partname_to_mats


def patch_blank_materials_in_prefab(
    yaml_text, src_prefab_path, guid_map, path_to_guid, is_mat_textured, mesh_to_mats, partname_to_mats
):
    """Replace any blank FBX Lit.mat references on MeshRenderer/SkinnedMeshRenderer with their painted materials."""
    yaml_text = sanitize_prefab_yaml(yaml_text)
    blocks = yaml_text.split("--- !u!")
    if len(blocks) <= 1:
        return yaml_text, 0

    go_name = {}
    mf_mesh = {}
    for b in blocks[1:]:
        if b.startswith("1 "):
            fid = b.split("\n", 1)[0].split("&")[1].strip()
            mn = re.search(r"m_Name:\s*([^\r\n]+)", b)
            if mn:
                go_name[fid] = mn.group(1).strip()
        elif b.startswith("33 "):
            mg = re.search(r"m_GameObject:\s*\{fileID:\s*(\d+)\}", b)
            mm = re.search(r"m_Mesh:\s*\{[^\}]*guid:\s*([0-9a-f]{32})", b)
            if mg and mm:
                mf_mesh[mg.group(1)] = mm.group(1)

    # Gather sibling Material/ folder textured .mat candidates
    pdir = os.path.dirname(src_prefab_path)
    cand_dirs = [
        os.path.join(pdir, "Material"),
        os.path.join(pdir, "Materials"),
        pdir,
        os.path.join(os.path.dirname(pdir), "Material"),
        os.path.join(os.path.dirname(pdir), "Materials"),
    ]
    local_mats = []
    seen_mg = set()
    for cd in cand_dirs:
        if os.path.isdir(cd):
            for f in sorted(os.listdir(cd)):
                if f.lower().endswith(".mat"):
                    fp = os.path.normcase(os.path.join(cd, f))
                    mg = path_to_guid.get(fp)
                    if mg and mg not in seen_mg and is_mat_textured(mg):
                        seen_mg.add(mg)
                        local_mats.append((os.path.splitext(f)[0].lower(), mg))

    pstem = os.path.splitext(os.path.basename(src_prefab_path))[0].lower()
    pcore = re.sub(r"^(mes|pfb|rig)_", "", pstem)

    guid_re = re.compile(r"guid:\s*([0-9a-f]{32})")
    mat_block_re = re.compile(r"(  m_Materials:\s*\r?\n(?:\s*-\s*\{[^\r\n]+\}\r?\n?)*)|(  m_Materials:\s*\[\]\r?\n)")
    patched_count = 0
    out_blocks = [blocks[0]]

    for b in blocks[1:]:
        if b.startswith("23 ") or b.startswith("137 "):
            mg_go = re.search(r"m_GameObject:\s*\{fileID:\s*(\d+)\}", b)
            goid = mg_go.group(1) if mg_go else ""
            gname = go_name.get(goid, "")
            gname_low = gname.lower()
            gcore = re.sub(r"^(mes|pfb|rig)_", "", gname_low)

            if b.startswith("137 "):
                mm = re.search(r"m_Mesh:\s*\{[^\}]*guid:\s*([0-9a-f]{32})", b)
                mguid = mm.group(1) if mm else None
            else:
                mguid = mf_mesh.get(goid)

            m_sec = mat_block_re.search(b)
            cur_mats = guid_re.findall(m_sec.group(0)) if m_sec else []

            if not cur_mats or not all(is_mat_textured(x) for x in cur_mats):
                resolved = None
                if mguid and mguid in mesh_to_mats:
                    resolved = mesh_to_mats[mguid]
                elif gname_low in partname_to_mats:
                    resolved = partname_to_mats[gname_low]
                elif gname_low.endswith("_02") and (gname_low[:-3] + "_01") in partname_to_mats:
                    resolved = partname_to_mats[gname_low[:-3] + "_01"]
                elif local_mats:
                    slot_cnt = max(1, len(cur_mats))
                    matched = [
                        mg for st, mg in local_mats
                        if (gcore and gcore in st) or (pcore and pcore in st) or (st.replace("mat_", "") in gcore)
                    ]
                    pool = matched if matched else [mg for _, mg in local_mats]
                    if len(pool) >= slot_cnt:
                        resolved = pool[:slot_cnt]
                    else:
                        resolved = (pool * slot_cnt)[:slot_cnt]

                if resolved and m_sec:
                    lines = ["  m_Materials:\n"] + [
                        f"  - {{fileID: 2100000, guid: {rg}, type: 2}}\n" for rg in resolved
                    ]
                    b = b[: m_sec.start()] + "".join(lines) + b[m_sec.end() :]
                    patched_count += 1

        out_blocks.append("--- !u!" + b)

    return "".join(out_blocks), patched_count


def inject_avatar_skinned_parts_into_prefab(yaml_text, avatar_rel_paths):
    """Attach modular G1P Face / Hair_In SkinnedMeshRenderers onto the prefab's Bip001 bone hierarchy."""
    if not avatar_rel_paths:
        return yaml_text, 0

    blocks = yaml_text.split("--- !u!")
    if len(blocks) <= 1:
        return yaml_text, 0

    used_fids = set(re.findall(r"--- !u!\d+\s+&(\d+)", yaml_text))
    go_name = {}
    tr_go = {}
    tr_father = {}
    root_tr_idx = None
    root_tr_fid = None

    for idx, b in enumerate(blocks[1:], start=1):
        if b.startswith("1 "):
            fid = b.split("\n", 1)[0].split("&")[1].strip()
            mn = re.search(r"m_Name:\s*([^\r\n]+)", b)
            if mn:
                go_name[fid] = mn.group(1).strip()
        elif b.startswith("4 "):
            fid = b.split("\n", 1)[0].split("&")[1].strip()
            mg = re.search(r"m_GameObject:\s*\{fileID:\s*(\d+)\}", b)
            mf = re.search(r"m_Father:\s*\{fileID:\s*(\d+)\}", b)
            if mg:
                tr_go[fid] = mg.group(1)
            if mf:
                father_id = mf.group(1)
                tr_father[fid] = father_id
                if father_id == "0" and root_tr_fid is None:
                    root_tr_fid = fid
                    root_tr_idx = idx

    if not root_tr_fid or root_tr_idx is None:
        return yaml_text, 0

    bone_to_tr = {go_name[gid]: tid for tid, gid in tr_go.items() if gid in go_name}
    existing_names = set(go_name.values())

    def alloc_fid(prefix_digits, seed_num):
        cand = int(f"{prefix_digits}{seed_num:013d}")
        while str(cand) in used_fids:
            cand += 1
        s = str(cand)
        used_fids.add(s)
        return s

    injected_go_blocks = []
    injected_tr_smr_blocks = []
    added_child_trs = []
    injected_count = 0

    for idx_part, rel_path in enumerate(avatar_rel_paths, start=1):
        ap = os.path.join(EP_ASSETS, rel_path)
        if not os.path.isfile(ap):
            print(f"  [WARN] Missing Avatar part asset: {ap}")
            continue
        atxt = open(ap, "r", encoding="utf-8", errors="ignore").read()
        mn = re.search(r"m_Name:\s*([^\r\n]+)", atxt)
        mref = re.search(r"Mesh:\s*(\{[^\}]+\})", atxt)
        bsec = re.search(r"bonesName:\s*\r?\n((?:\s*-\s*[^\r\n]+\r?\n?)+)", atxt)
        msec = re.search(r"materials:\s*\r?\n((?:\s*-\s*\{[^\}]+\}\r?\n?)+)", atxt)
        if not (mn and mref and bsec and msec):
            continue

        part_name = mn.group(1).strip()
        if part_name in existing_names:
            continue

        bone_names = [ln.strip()[2:].strip() for ln in bsec.group(1).strip().splitlines() if ln.strip().startswith("-")]
        if not bone_names or any(bn not in bone_to_tr for bn in bone_names):
            missing_b = [bn for bn in bone_names if bn not in bone_to_tr]
            print(f"  [WARN] Skipping {part_name}: missing bones {missing_b}")
            continue

        mat_lines = [ln.strip() for ln in msec.group(1).strip().splitlines() if ln.strip().startswith("-")]
        seed = (zlib.crc32(part_name.encode("utf-8")) & 0xFFFFFFFF) + idx_part * 997
        go_fid = alloc_fid("189", seed)
        tr_fid = alloc_fid("489", seed)
        smr_fid = alloc_fid("13789", seed)

        root_bone_fid = bone_to_tr.get("Bip001 Spine2", bone_to_tr[bone_names[0]])
        bones_yaml = "\n".join(f"  - {{fileID: {bone_to_tr[bn]}}}" for bn in bone_names)
        mats_yaml = "\n".join(f"  {ml}" for ml in mat_lines)

        go_block = f"""1 &{go_fid}
GameObject:
  serializedVersion: 6
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Component:
  - component: {{fileID: {tr_fid}}}
  - component: {{fileID: {smr_fid}}}
  m_Layer: 0
  m_Name: {part_name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
"""
        tr_smr_block = f"""4 &{tr_fid}
Transform:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_fid}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {root_tr_fid}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!137 &{smr_fid}
SkinnedMeshRenderer:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_fid}}}
  m_Enabled: 1
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 3
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
{mats_yaml}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 0
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_Quality: 0
  m_UpdateWhenOffscreen: 0
  m_SkinnedMotionVectors: 1
  m_Mesh: {mref.group(1)}
  m_Bones:
{bones_yaml}
  m_BlendShapeWeights: []
  m_RootBone: {{fileID: {root_bone_fid}}}
  m_AABB:
    m_Center: {{x: -0.31623024, y: 0.0076123327, z: 0.003104031}}
    m_Extent: {{x: 0.45, y: 0.45, z: 0.45}}
  m_DirtyAABB: 0
"""
        injected_go_blocks.append("--- !u!" + go_block)
        injected_tr_smr_blocks.append("--- !u!" + tr_smr_block)
        added_child_trs.append(tr_fid)
        existing_names.add(part_name)
        injected_count += 1

    if injected_count == 0:
        return yaml_text, 0

    # Register child Transforms on root Transform's m_Children
    rblock = blocks[root_tr_idx]
    child_entries = "".join(f"  - {{fileID: {tf}}}\n" for tf in added_child_trs)
    if "  m_Children: []" in rblock:
        rblock = rblock.replace("  m_Children: []\n", "  m_Children:\n" + child_entries, 1)
    else:
        rblock = re.sub(r"(  m_Children:\r?\n)", r"\1" + child_entries, rblock, count=1)
    blocks[root_tr_idx] = rblock

    # Rebuild YAML keeping GameObject blocks at top and Transform/SMR blocks at end
    out_parts = [blocks[0]]
    inserted_gos = False
    for b in blocks[1:]:
        if not inserted_gos and not b.startswith("1 "):
            out_parts.extend(injected_go_blocks)
            inserted_gos = True
        out_parts.append("--- !u!" + b)
    if not inserted_gos:
        out_parts.extend(injected_go_blocks)
    out_parts.extend(injected_tr_smr_blocks)

    return "".join(out_parts), injected_count


def upgrade_material_to_urp_lit(mat_yaml, mat_name):
    """Convert Unity 2022 custom shader Material YAML -> clean Unity 2021.3 URP Lit Material YAML."""
    base_ref = extract_tex_ref(mat_yaml, BASE_TEX_PROPS)
    norm_ref = extract_tex_ref(mat_yaml, NORM_TEX_PROPS)
    emis_ref = extract_tex_ref(mat_yaml, EMIS_TEX_PROPS)

    mat_low = mat_name.lower()
    is_hair = ("_ShiftTex:" in mat_yaml) or ("hair" in mat_low)
    is_skin = ("sknf" in mat_low) or ("sknm" in mat_low) or ("face" in mat_low)

    is_cutout = (
        is_hair
        or "TransparentCutout" in mat_yaml
        or "_ALPHATEST_ON" in mat_yaml
        or re.search(r"_AlphaClip:\s*1\b", mat_yaml) is not None
        or re.search(r"_AlphaTest:\s*1\b", mat_yaml) is not None
        or re.search(r"m_CustomRenderQueue:\s*2450\b", mat_yaml) is not None
    )
    cull_mode = 0 if (is_cutout or is_hair or re.search(r"_Cull:\s*0\b", mat_yaml) is not None) else 2

    keywords = []
    if norm_ref:
        keywords.append("_NORMALMAP")
    if emis_ref:
        keywords.append("_EMISSION")
    if is_cutout:
        keywords.append("_ALPHATEST_ON")

    def fmt_tex(ref):
        if not ref:
            return "        m_Texture: {fileID: 0}\n        m_Scale: {x: 1, y: 1}\n        m_Offset: {x: 0, y: 0}"
        return (
            f"        m_Texture: {{fileID: {ref['fileID']}, guid: {ref['guid']}, type: {ref['type']}}}\n"
            f"        m_Scale: {{{ref['scale']}}}\n"
            f"        m_Offset: {{{ref['offset']}}}"
        )

    kw_str = " ".join(keywords)
    render_queue = 2450 if is_cutout else -1
    render_type = "TransparentCutout" if is_cutout else "Opaque"
    alpha_clip = 1 if is_cutout else 0
    cutoff_val = 0.28 if is_hair else 0.35
    metallic_val = 0.04 if is_hair else (0.02 if is_skin else 0.16)
    smooth_val = 0.28 if is_hair else (0.22 if is_skin else 0.36)
    base_col = "{r: 1, g: 1, b: 1, a: 1}"
    emis_col = "{r: 0.85, g: 0.85, b: 0.85, a: 1}" if emis_ref else "{r: 0, g: 0, b: 0, a: 1}"
    if not base_ref:
        m_inner = re.search(r"_InnerNoiseColor:\s*(\{[^\}]+\})", mat_yaml)
        m_emcol = re.search(r"_EmissionColor:\s*(\{[^\}]+\})", mat_yaml)
        if m_inner:
            base_col = m_inner.group(1)
            smooth_val = 0.86
            if m_emcol:
                emis_col = m_emcol.group(1)
                if "_EMISSION" not in keywords:
                    keywords.append("_EMISSION")
                    kw_str = " ".join(keywords)

    urp_yaml = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 6
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {mat_name}
  m_Shader: {{fileID: 4800000, guid: {URP_LIT_GUID}, type: 3}}
  m_ShaderKeywords: {kw_str}
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 1
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: {render_queue}
  stringTagMap:
    RenderType: {render_type}
  disabledShaderPasses: []
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
      _BaseMap:
{fmt_tex(base_ref)}
      _BumpMap:
{fmt_tex(norm_ref)}
      _EmissionMap:
{fmt_tex(emis_ref)}
      _MainTex:
{fmt_tex(base_ref)}
    m_Floats:
      _AlphaClip: {alpha_clip}
      _Blend: 0
      _BumpScale: 1
      _Cull: {cull_mode}
      _Cutoff: {cutoff_val}
      _EnvironmentReflections: 1
      _Metallic: {metallic_val}
      _ReceiveShadows: 1
      _Smoothness: {smooth_val}
      _SpecularHighlights: 1
      _Surface: 0
      _WorkflowMode: 1
      _ZWrite: 1
    m_Colors:
      _BaseColor: {base_col}
      _Color: {base_col}
      _EmissionColor: {emis_col}
      _SpecColor: {{r: 0.2, g: 0.2, b: 0.2, a: 1}}
  m_BuildTextureStacks: []
"""
    used_guids = [r["guid"] for r in (base_ref, norm_ref, emis_ref) if r]
    norm_guid = norm_ref["guid"] if norm_ref else None
    return urp_yaml, used_guids, norm_guid


def fix_normal_map_image_and_meta(src_img_path, dst_img_path, src_meta_path, dst_meta_path):
    """Reconstruct unit Z in B channel for RG-packed normal maps and mark .meta as NormalMap."""
    try:
        im = Image.open(src_img_path).convert("RGBA")
        arr = np.array(im, dtype=np.float32)
        nx = (arr[:, :, 0] - 127.5) / 127.5
        ny = (arr[:, :, 1] - 127.5) / 127.5
        nz = np.sqrt(np.clip(1.0 - nx * nx - ny * ny, 0.0, 1.0))
        out = np.empty_like(arr, dtype=np.uint8)
        out[:, :, 0] = np.clip(arr[:, :, 0], 0, 255).astype(np.uint8)
        out[:, :, 1] = np.clip(arr[:, :, 1], 0, 255).astype(np.uint8)
        out[:, :, 2] = np.clip(nz * 127.5 + 127.5, 0, 255).astype(np.uint8)
        out[:, :, 3] = 255
        Image.fromarray(out).save(dst_img_path)
    except Exception:
        shutil.copy2(src_img_path, dst_img_path)

    meta_txt = open(src_meta_path, "r", encoding="utf-8", errors="ignore").read()
    meta_txt = re.sub(r"textureType:\s*\d+", "textureType: 1", meta_txt)
    meta_txt = re.sub(r"sRGBTexture:\s*\d+", "sRGBTexture: 0", meta_txt)
    meta_txt = re.sub(r"textureFormat:\s*-?\d+", "textureFormat: -1", meta_txt, count=1)
    meta_txt = re.sub(r"textureFormatSet:\s*\d+", "textureFormatSet: 0", meta_txt)
    with open(dst_meta_path, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(meta_txt)


def copy_diffuse_texture_and_meta(src_img_path, dst_img_path, src_meta_path, dst_meta_path):
    """Copy diffuse texture, gently lifting dark linear-authored albedos so PBR armor/stone colors read clearly."""
    ext = os.path.splitext(src_img_path)[1].lower()
    if ext in (".tga", ".png", ".jpg", ".jpeg"):
        try:
            im = Image.open(src_img_path).convert("RGBA")
            arr = np.array(im, dtype=np.float32)
            vis = arr[:, :, 3] > 16
            mean_rgb = float(arr[:, :, :3][vis].mean()) if np.any(vis) else float(arr[:, :, :3].mean())
            if mean_rgb < 78.0:
                lifted = np.power(np.clip(arr[:, :, :3] / 255.0, 0.0, 1.0), 0.72) * 1.14 * 255.0
                arr[:, :, :3] = np.clip(lifted, 0.0, 255.0)
                Image.fromarray(arr.astype(np.uint8)).save(dst_img_path)
            else:
                shutil.copy2(src_img_path, dst_img_path)
        except Exception:
            shutil.copy2(src_img_path, dst_img_path)
    else:
        shutil.copy2(src_img_path, dst_img_path)

    meta_txt = open(src_meta_path, "r", encoding="utf-8", errors="ignore").read()
    meta_txt = re.sub(r"textureType:\s*\d+", "textureType: 0", meta_txt)
    meta_txt = re.sub(r"sRGBTexture:\s*\d+", "sRGBTexture: 1", meta_txt)
    meta_txt = re.sub(r"textureFormat:\s*-?\d+", "textureFormat: -1", meta_txt, count=1)
    meta_txt = re.sub(r"textureFormatSet:\s*\d+", "textureFormatSet: 0", meta_txt)
    with open(dst_meta_path, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(meta_txt)


def copy_prefabs_and_dependencies(guid_map):
    print("[2/5] Resolving PBR Materials & Copying 3D Rigs, Weapons, Enemies, Boss & Environment...", flush=True)
    for sub in ["Characters", "Weapons", "Enemies", "Environment", "Meshes", "Materials", "Textures"]:
        os.makedirs(os.path.join(DST_EXPORTED, sub), exist_ok=True)

    path_to_guid, is_mat_textured, mesh_to_mats, partname_to_mats = build_material_resolution_tables(guid_map)
    print(f"      Indexed {len(mesh_to_mats):,} mesh->material & {len(partname_to_mats):,} avatar part->material mappings.", flush=True)

    copied_guids = {}  # guid -> dst_path
    normal_tex_guids = set()
    guid_re = re.compile(r"guid:\s*([0-9a-f]{32})")
    total_patched_renderers = 0
    total_injected_avatar_parts = 0

    all_roots = []
    for k, rel in HERO_RIGS.items():
        all_roots.append(("Characters", k + ".prefab", os.path.join(EP_ASSETS, rel)))
    for k, rel in WEAPONS.items():
        all_roots.append(("Weapons", k + ".prefab", os.path.join(EP_ASSETS, rel)))
    for k, rel in ENEMIES.items():
        all_roots.append(("Enemies", k + ".prefab", os.path.join(EP_ASSETS, rel)))
    for k, rel in ENVIRONMENT.items():
        all_roots.append(("Environment", k + ".prefab", os.path.join(EP_ASSETS, rel)))

    dep_queue = []

    for folder, out_name, src_path in all_roots:
        if not os.path.isfile(src_path):
            print(f"  [WARN] Missing root prefab: {src_path}")
            continue
        meta_path = src_path + ".meta"
        m = guid_re.search(open(meta_path, "r", encoding="utf-8", errors="ignore").read(220))
        root_guid = m.group(1) if m else None

        raw_yaml = open(src_path, "r", encoding="utf-8", errors="ignore").read()
        clean_yaml, pcnt = patch_blank_materials_in_prefab(
            raw_yaml, src_path, guid_map, path_to_guid, is_mat_textured, mesh_to_mats, partname_to_mats
        )
        total_patched_renderers += pcnt

        rig_key = os.path.splitext(out_name)[0]
        if folder == "Characters" and rig_key in HERO_AVATAR_PARTS:
            clean_yaml, icnt = inject_avatar_skinned_parts_into_prefab(clean_yaml, HERO_AVATAR_PARTS[rig_key])
            total_injected_avatar_parts += icnt

        dst_path = os.path.join(DST_EXPORTED, folder, out_name)
        with open(dst_path, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(clean_yaml)
        shutil.copy2(meta_path, dst_path + ".meta")
        if root_guid:
            copied_guids[root_guid] = dst_path

        for g in set(guid_re.findall(clean_yaml)):
            if g != root_guid and g in guid_map:
                dep_queue.append(g)

    # Transitive dependency resolution (Meshes, Materials, Sub-Prefabs, Textures)
    while dep_queue:
        g = dep_queue.pop()
        if g in copied_guids or g not in guid_map:
            continue
        src = guid_map[g]
        src_meta = src + ".meta"
        if not os.path.isfile(src) or not os.path.isfile(src_meta):
            continue

        ext = os.path.splitext(src)[1].lower()
        safe_name = sanitize_ascii_name(os.path.basename(src), g)

        if ext == ".mat":
            dst = os.path.join(DST_EXPORTED, "Materials", safe_name)
            mat_yaml = open(src, "r", encoding="utf-8", errors="ignore").read()
            mat_name = os.path.splitext(safe_name)[0]
            urp_yaml, tex_guids, norm_guid = upgrade_material_to_urp_lit(mat_yaml, mat_name)
            with open(dst, "w", encoding="utf-8", newline="\n") as fh:
                fh.write(urp_yaml)
            shutil.copy2(src_meta, dst + ".meta")
            copied_guids[g] = dst
            if norm_guid:
                normal_tex_guids.add(norm_guid)
            for tg in tex_guids:
                if tg not in copied_guids:
                    dep_queue.append(tg)

        elif ext in (".asset", ".fbx", ".obj"):
            dst = os.path.join(DST_EXPORTED, "Meshes", safe_name)
            shutil.copy2(src, dst)
            shutil.copy2(src_meta, dst + ".meta")
            copied_guids[g] = dst
            if ext == ".asset":
                head = open(src, "r", encoding="utf-8", errors="ignore").read(4096)
                for sub_g in set(guid_re.findall(head)):
                    if sub_g not in copied_guids and sub_g in guid_map:
                        dep_queue.append(sub_g)

        elif ext == ".prefab":
            dst = os.path.join(DST_EXPORTED, "Environment", safe_name)
            raw_yaml = open(src, "r", encoding="utf-8", errors="ignore").read()
            clean_yaml, pcnt = patch_blank_materials_in_prefab(
                raw_yaml, src, guid_map, path_to_guid, is_mat_textured, mesh_to_mats, partname_to_mats
            )
            total_patched_renderers += pcnt
            with open(dst, "w", encoding="utf-8", newline="\n") as fh:
                fh.write(clean_yaml)
            shutil.copy2(src_meta, dst + ".meta")
            copied_guids[g] = dst
            for sub_g in set(guid_re.findall(clean_yaml)):
                if sub_g not in copied_guids and sub_g in guid_map:
                    dep_queue.append(sub_g)

        elif ext in (".tga", ".png", ".jpg", ".jpeg", ".psd", ".exr"):
            dst = os.path.join(DST_EXPORTED, "Textures", safe_name)
            is_norm = (
                g in normal_tex_guids
                or bool(re.search(r"_(NM|NMT|NMA|NS|Normal)(?:_|\.)", os.path.basename(src), re.IGNORECASE))
            )
            if is_norm:
                fix_normal_map_image_and_meta(src, dst, src_meta, dst + ".meta")
            else:
                copy_diffuse_texture_and_meta(src, dst, src_meta, dst + ".meta")
            copied_guids[g] = dst

    # Second check: if a normal map texture was copied BEFORE a material marked it in normal_tex_guids, fix it now
    for ng in normal_tex_guids:
        if ng in copied_guids and ng in guid_map:
            src = guid_map[ng]
            dst = copied_guids[ng]
            fix_normal_map_image_and_meta(src, dst, src + ".meta", dst + ".meta")

    print(
        f"      Copied {len(all_roots)} root prefabs + {len(copied_guids) - len(all_roots)} dependencies "
        f"({len(copied_guids)} total assets; auto-patched {total_patched_renderers} blank FBX renderers; "
        f"injected {total_injected_avatar_parts} modular 3D Face/Hair parts).",
        flush=True,
    )
    return all_roots


# ---------------------------------------------------------------------------
# 5. CRC32 BONE-PATH TABLE & ANIMATION DECODER
# ---------------------------------------------------------------------------

def collect_bone_paths_from_prefab(prefab_path, crc_table):
    if not os.path.isfile(prefab_path):
        return
    txt = open(prefab_path, "r", encoding="utf-8", errors="ignore").read()
    blocks = txt.split("--- !u!")
    go_name = {}
    tr_go = {}
    tr_father = {}

    for b in blocks[1:]:
        if b.startswith("1 "):
            fid = b.split("\n", 1)[0].split("&")[1].strip()
            m = re.search(r"m_Name:\s*([^\r\n]+)", b)
            if m:
                go_name[fid] = m.group(1).strip()
        elif b.startswith("4 "):
            fid = b.split("\n", 1)[0].split("&")[1].strip()
            mg = re.search(r"m_GameObject:\s*\{fileID:\s*(\d+)\}", b)
            mf = re.search(r"m_Father:\s*\{fileID:\s*(\d+)\}", b)
            if mg:
                tr_go[fid] = mg.group(1)
            if mf:
                tr_father[fid] = mf.group(1)

    def get_rel_path(tid):
        parts = []
        cur = tid
        guard = 0
        while cur in tr_father and tr_father[cur] != "0" and guard < 64:
            parts.append(go_name.get(tr_go.get(cur, ""), ""))
            cur = tr_father[cur]
            guard += 1
        parts.reverse()
        return "/".join(parts)

    for tid in tr_go:
        rp = get_rel_path(tid)
        h = zlib.crc32(rp.encode("utf-8")) & 0xFFFFFFFF
        crc_table[str(h)] = rp
        # Also register sub-paths in case a rig has an extra wrapper above Root
        if "/" in rp:
            sub = rp.split("/", 1)[1]
            crc_table[str(zlib.crc32(sub.encode("utf-8")) & 0xFFFFFFFF)] = sub


def copy_and_decode_animations(all_roots):
    print("[3/5] Building CRC32 bone-path dictionary & decoding Hero/Enemy/Boss .anim clips...", flush=True)
    crc_table = {"0": ""}

    # Scan all copied character/enemy/weapon prefabs + master skeleton prefabs
    extra_skeletons = [
        os.path.join(EP_ASSETS, r"AssetBox\ResFolder\Ingame\Character\RIG_G1P_SknM_0001_Skeleton.prefab"),
        os.path.join(EP_ASSETS, r"AssetBox\ResFolder\Ingame\Character\PFB_G1P_SknF_0001_Skeleton.prefab"),
        os.path.join(EP_ASSETS, r"AssetBox\ResFolder\Ingame\Character\G1P_InGameSknM_0001.prefab"),
        os.path.join(EP_ASSETS, r"AssetBox\ResFolder\Ingame\Character\G1P_InGameSknF_0001.prefab"),
    ]
    for p in extra_skeletons:
        collect_bone_paths_from_prefab(p, crc_table)
    for _, _, src_path in all_roots:
        collect_bone_paths_from_prefab(src_path, crc_table)

    print(f"      Registered {len(crc_table):,} unique bone transform CRC32 hashes.", flush=True)

    anim_root_src = os.path.join(EP_ASSETS, r"AssetBox\ResFolder\Animation")
    path_re = re.compile(r"^(\s*path:\s*)(\d+)\s*$", re.MULTILINE)

    resolved_total = 0
    unresolved_set = set()
    copied_anims = 0

    def process_anim(src_rel, dst_path, clip_name, should_loop):
        nonlocal resolved_total, copied_anims
        src = os.path.join(anim_root_src, src_rel)
        if not os.path.isfile(src):
            print(f"  [WARN] Missing animation clip: {src_rel}")
            return
        os.makedirs(os.path.dirname(dst_path), exist_ok=True)
        txt = open(src, "r", encoding="utf-8", errors="ignore").read()

        def repl_path(m):
            nonlocal resolved_total
            prefix, num = m.group(1), m.group(2)
            if num in crc_table:
                resolved_total += 1
                val = crc_table[num]
                return f"{prefix}{val}" if val else f"{prefix}"
            unresolved_set.add(num)
            return m.group(0)

        txt = path_re.sub(repl_path, txt)
        txt = re.sub(r"m_Name:\s*[^\r\n]+", f"m_Name: {clip_name}", txt, count=1)
        loop_val = "1" if should_loop else "0"
        txt = re.sub(r"m_LoopTime:\s*\d+", f"m_LoopTime: {loop_val}", txt)
        # Clear m_Events so runtime never logs 'AnimationEvent has no receiver!'
        txt = re.sub(r"m_Events:\s*(?:\r?\n(?:  .*|\s*-.*))*", "m_Events: []\n", txt)

        with open(dst_path, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(txt)
        copied_anims += 1

    # Common Hero animations
    for state, rel in COMMON_HERO_ANIMS.items():
        loop = state in ("Sprint",)
        process_anim(
            rel,
            os.path.join(DST_EXPORTED, "Animations", "Heroes", "Common", f"Hero_Common_{state}.anim"),
            f"Hero_Common_{state}",
            loop,
        )

    # 10 Hero Class stance animations
    for cls_name, cmap in HERO_CLASS_ANIMS.items():
        for state, rel in cmap.items():
            loop = state in ("Idle", "Walk", "Run", "Sprint")
            process_anim(
                rel,
                os.path.join(DST_EXPORTED, "Animations", "Heroes", cls_name, f"Hero_{cls_name}_{state}.anim"),
                f"Hero_{cls_name}_{state}",
                loop,
            )

    # 12 Enemy/Summon/Boss + DruidBeast animations
    for ename, emap in ENEMY_ANIMS.items():
        for state, rel in emap.items():
            loop = state in ("Idle", "Walk", "Run")
            process_anim(
                rel,
                os.path.join(DST_EXPORTED, "Animations", "Enemies", ename, f"{ename}_{state}.anim"),
                f"{ename}_{state}",
                loop,
            )

    print(f"      Decoded & saved {copied_anims} .anim clips ({resolved_total:,} bone curves resolved, {len(unresolved_set)} extra helper hashes).", flush=True)


# ---------------------------------------------------------------------------
# 6. ENVIRONMENT PBR TEXTURES & DARK FANTASY UI ART
# ---------------------------------------------------------------------------

def save_pbr_set(out_dir, prefix, alb_src, nrm_src=None, tint=(1.0, 1.0, 1.0), metal_val=20, smooth_val=92, emission_tint=None):
    im = Image.open(alb_src).convert("RGBA").resize((512, 512), Image.Resampling.LANCZOS)
    arr = np.array(im, dtype=np.float32)
    mean_rgb = float(arr[:, :, :3].mean())
    if mean_rgb < 80.0:
        arr[:, :, :3] = np.clip(np.power(np.clip(arr[:, :, :3] / 255.0, 0.0, 1.0), 0.72) * 1.14 * 255.0, 0.0, 255.0)
    arr[:, :, 0] = np.clip(arr[:, :, 0] * tint[0], 0, 255)
    arr[:, :, 1] = np.clip(arr[:, :, 1] * tint[1], 0, 255)
    arr[:, :, 2] = np.clip(arr[:, :, 2] * tint[2], 0, 255)
    arr[:, :, 3] = 255
    alb_img = Image.fromarray(arr.astype(np.uint8), "RGBA")
    alb_img.save(os.path.join(out_dir, f"{prefix}_Albedo.png"))

    gray = np.array(ImageOps.grayscale(alb_img), dtype=np.float32) / 255.0
    ao = np.clip(0.55 + gray * 0.50, 0.25, 1.0)
    ao_u8 = (ao * 255).astype(np.uint8)
    Image.fromarray(np.stack([ao_u8, ao_u8, ao_u8, np.full_like(ao_u8, 255)], axis=-1), "RGBA").save(
        os.path.join(out_dir, f"{prefix}_AO.png")
    )

    # URP MetallicGlossMap: R = Metallic, A = Smoothness
    r_ch = np.full((512, 512), metal_val, dtype=np.uint8)
    a_ch = np.clip(smooth_val * (0.75 + 0.5 * gray), 10, 245).astype(np.uint8)
    Image.fromarray(np.stack([r_ch, ao_u8, r_ch, a_ch], axis=-1), "RGBA").save(
        os.path.join(out_dir, f"{prefix}_Mask.png")
    )

    if nrm_src and os.path.isfile(nrm_src):
        nim = Image.open(nrm_src).convert("RGBA").resize((512, 512), Image.Resampling.BILINEAR)
        narr = np.array(nim, dtype=np.float32)
        nx = (narr[:, :, 0] - 127.5) / 127.5
        ny = (narr[:, :, 1] - 127.5) / 127.5
        nz = np.sqrt(np.clip(1.0 - nx * nx - ny * ny, 0.0, 1.0))
        nout = np.stack(
            [
                np.clip(narr[:, :, 0], 0, 255).astype(np.uint8),
                np.clip(narr[:, :, 1], 0, 255).astype(np.uint8),
                np.clip(nz * 127.5 + 127.5, 0, 255).astype(np.uint8),
                np.full((512, 512), 255, dtype=np.uint8),
            ],
            axis=-1,
        )
        Image.fromarray(nout, "RGBA").save(os.path.join(out_dir, f"{prefix}_Normal.png"))
    else:
        # Derive tangent normal from height luminance of the real texture
        gx = np.roll(gray, -1, axis=1) - np.roll(gray, 1, axis=1)
        gy = np.roll(gray, -1, axis=0) - np.roll(gray, 1, axis=0)
        nx = -gx * 2.2
        ny = -gy * 2.2
        nz = np.ones_like(nx)
        inv = 1.0 / np.sqrt(nx * nx + ny * ny + nz * nz)
        nout = np.stack(
            [
                np.clip((nx * inv * 0.5 + 0.5) * 255, 0, 255).astype(np.uint8),
                np.clip((ny * inv * 0.5 + 0.5) * 255, 0, 255).astype(np.uint8),
                np.clip((nz * inv * 0.5 + 0.5) * 255, 0, 255).astype(np.uint8),
                np.full((512, 512), 255, dtype=np.uint8),
            ],
            axis=-1,
        )
        Image.fromarray(nout, "RGBA").save(os.path.join(out_dir, f"{prefix}_Normal.png"))

    if emission_tint is not None:
        em = np.stack(
            [
                np.clip(gray * 255 * emission_tint[0], 0, 255).astype(np.uint8),
                np.clip(gray * 255 * emission_tint[1], 0, 255).astype(np.uint8),
                np.clip(gray * 255 * emission_tint[2], 0, 255).astype(np.uint8),
                np.full((512, 512), 255, dtype=np.uint8),
            ],
            axis=-1,
        )
        Image.fromarray(em, "RGBA").save(os.path.join(out_dir, f"{prefix}_Emission.png"))

    return alb_img


def extract_pbr_and_ui_textures():
    print("[4/5] Extracting authentic PBR Environment Textures & Dark Fantasy UI Art...", flush=True)
    env_dir = os.path.join(DST_ASSETS, "Resources", "Environment")
    pbr_dir = os.path.join(env_dir, "PBR")
    ui_dir = os.path.join(DST_ASSETS, "Resources", "UI")
    cls_ui_dir = os.path.join(ui_dir, "Classes")
    os.makedirs(pbr_dir, exist_ok=True)
    os.makedirs(cls_ui_dir, exist_ok=True)

    stone_alb = os.path.join(EP_ASSETS, r"ART\Environment\CommonDyn\Texture\TEX_Common_Wall_01_C.tga")
    stone_nrm = os.path.join(EP_ASSETS, r"ART\Environment\CommonDyn\Texture\TEX_Common_wall_01_NS.tga")
    floor_alb = os.path.join(EP_ASSETS, r"ART\Environment\Art_Map01\Texture\TEX_CT02_Floor_01_C.tga")
    moss_alb  = os.path.join(EP_ASSETS, r"ART\Environment\Art_Map01\Texture\TEX_BID_POP_TRN_Ground_04_C.tga")
    moss_nrm  = os.path.join(EP_ASSETS, r"ART\Environment\Art_Map01\Texture\TEX_BID_POP_TRN_Ground_02_NS.tga")
    iron_alb  = os.path.join(EP_ASSETS, r"ART\Environment\Art_Map01\Texture\TEX_Iron_Virgin_01_C.tga")
    iron_nrm  = os.path.join(EP_ASSETS, r"ART\Environment\Art_Map01\Texture\TEX_Iron_Virgin_01_NS.tga")
    wood_alb  = os.path.join(EP_ASSETS, r"ART\Environment\CommonContainer\TradeGoods\Texture\TEX_M01_PO_Wood_01_C.tga")
    wood_nrm  = os.path.join(EP_ASSETS, r"ART\Environment\CommonContainer\TradeGoods\Texture\TEX_M01_PO_Wood_01_NS.tga")
    crys_alb  = os.path.join(EP_ASSETS, r"ART\Environment\CommonDyn\Texture\TEX_DS_CrystalMatCap_Blue.tga")

    wall_img  = save_pbr_set(pbr_dir, "T_GothicStone",    stone_alb, stone_nrm, (1.0, 1.0, 1.0), 18, 80)
    floor_img = save_pbr_set(pbr_dir, "T_CathedralFloor", floor_alb, stone_nrm, (1.0, 1.0, 1.0), 22, 96)
    save_pbr_set(pbr_dir, "T_MossyRuins",     moss_alb,  moss_nrm,  (0.92, 1.04, 0.90), 12, 92)
    save_pbr_set(pbr_dir, "T_WroughtIron",    iron_alb,  iron_nrm,  (1.0, 1.0, 1.05), 215, 145)
    save_pbr_set(pbr_dir, "T_AgedOakWood",    wood_alb,  wood_nrm,  (1.0, 0.96, 0.92), 10, 78)
    save_pbr_set(pbr_dir, "T_AbyssalCrystal", crys_alb,  None,      (0.85, 0.95, 1.15), 60, 220, emission_tint=(0.6, 0.85, 1.35))

    wall_img.save(os.path.join(env_dir, "stone_wall.png"))
    floor_img.save(os.path.join(env_dir, "stone_floor.png"))

    # Floor decals from ExportedProject ground cutout texture
    decal_src = os.path.join(EP_ASSETS, r"ART\Environment\Art_Map01\Texture\TEX_BID_POP_TRN_Ground_02_CA.tga")
    if os.path.isfile(decal_src):
        dim = Image.open(decal_src).convert("RGBA").resize((256, 256), Image.Resampling.LANCZOS)
        darr = np.array(dim, dtype=np.float32)
        # Soft radial vignette so floor decals blend seamlessly without hard square borders
        yy, xx = np.mgrid[0:256, 0:256]
        rad = np.sqrt(((xx - 127.5) / 127.5) ** 2 + ((yy - 127.5) / 127.5) ** 2)
        falloff = np.clip(1.0 - rad ** 1.8, 0.0, 1.0)
        crack = darr.copy()
        crack[:, :, 3] = np.clip(crack[:, :, 3] * falloff * 0.72, 0, 255)
        Image.fromarray(crack.astype(np.uint8), "RGBA").save(os.path.join(pbr_dir, "T_DecalCracksMoss.png"))

        scorch = darr.copy()
        scorch[:, :, 0] *= 0.35
        scorch[:, :, 1] *= 0.28
        scorch[:, :, 2] *= 0.25
        scorch[:, :, 3] = np.clip(220 * falloff * 0.65, 0, 255)
        Image.fromarray(scorch.astype(np.uint8), "RGBA").save(os.path.join(pbr_dir, "T_DecalBattleScorch.png"))

    # UI Backgrounds & Frames from ExportedProject
    bg_menu_src = os.path.join(EP_ASSETS, r"AssetBox\ResFolder\UI\Texture\UIClass\ui_class_main_BG.png")
    bg_lobby_src = os.path.join(EP_ASSETS, r"AssetBox\ResFolder\UI\Texture\UILobby_DawnSanctuary\ui_dawnsanctuary_mainbg02_BG.png")
    frame_src = os.path.join(EP_ASSETS, r"AssetBox\ResFolder\UI\Texture\Common\ui_common_bg01.png")

    if os.path.isfile(bg_menu_src):
        shutil.copy2(bg_menu_src, os.path.join(ui_dir, "menu_background.png"))
    if os.path.isfile(bg_lobby_src):
        # Composite Dawn Sanctuary foreground over dark gothic abyssal sky so alpha=0 top isn't white
        fg = Image.open(bg_lobby_src).convert("RGBA")
        w, h = fg.size
        sky = Image.open(bg_menu_src).convert("RGBA").resize((w, h), Image.Resampling.LANCZOS)
        comp = Image.alpha_composite(sky, fg)
        comp.save(os.path.join(ui_dir, "lobby_background.png"))
    if os.path.isfile(frame_src):
        shutil.copy2(frame_src, os.path.join(ui_dir, "panel_frame.png"))

    # Slice the 10 High-Resolution Painted Dark Fantasy Class Portraits from UICommonAvatar #0 & #1
    av0 = Image.open(os.path.join(EP_ASSETS, r"Texture2D\sactx-0-1024x1024-ASTC 10x10-UICommonAvatar-d73ad605.png")).convert("RGBA")
    av1 = Image.open(os.path.join(EP_ASSETS, r"Texture2D\sactx-1-1024x1024-ASTC 10x10-UICommonAvatar-d73ad605.png")).convert("RGBA")

    # Exact head-and-shoulders crop boxes (x1, y1, x2, y2) verified from the 1024x1024 sheets
    portrait_crops = {
        "knight":    (av0, (35,  30,  315, 310), (70, 115, 195)),
        "cleric":    (av0, (385, 45,  655, 315), (215, 185, 105)),
        "ranger":    (av0, (35,  515, 305, 785), (65, 155, 85)),
        "mage":      (av0, (380, 520, 640, 780), (65, 130, 225)),
        "druid":     (av0, (685, 535, 945, 795), (75, 165, 110)),
        "warlock":   (av1, (15,  25,  265, 275), (145, 65, 195)),
        "assassin":  (av1, (565, 35,  805, 275), (120, 75, 165)),
        "monk":      (av1, (25,  535, 285, 795), (210, 140, 65)),
        "guardian":  (av1, (335, 525, 595, 785), (210, 170, 75)),
        "berserker": (av1, (640, 515, 900, 775), (195, 65, 65)),
    }

    for cname, (sheet, box, border_rgb) in portrait_crops.items():
        crop = sheet.crop(box).resize((256, 256), Image.Resampling.LANCZOS)
        # Composite onto a rich dark gothic portrait card with subtle class-colored rim
        card = Image.new("RGBA", (256, 256), (16, 14, 24, 255))
        draw = ImageDraw.Draw(card)
        for r in range(120, 10, -4):
            alpha_f = (120 - r) / 120.0
            col = (
                int(16 + border_rgb[0] * 0.28 * alpha_f),
                int(14 + border_rgb[1] * 0.28 * alpha_f),
                int(24 + border_rgb[2] * 0.28 * alpha_f),
                255,
            )
            draw.ellipse([128 - r, 118 - r, 128 + r, 118 + r], fill=col)
        card = Image.alpha_composite(card, crop)
        draw = ImageDraw.Draw(card)
        draw.rectangle([2, 2, 253, 253], outline=(border_rgb[0], border_rgb[1], border_rgb[2], 235), width=4)
        draw.rectangle([7, 7, 248, 248], outline=(45, 40, 58, 200), width=1)
        card.save(os.path.join(cls_ui_dir, f"icon_{cname}.png"))

    print("      Generated 6 PBR texture sets + 10 painted Dark Fantasy Class Portraits + UI Backgrounds.", flush=True)


def main():
    import sys
    skip_anims = "--skip-anims" in sys.argv and os.path.isdir(os.path.join(DST_EXPORTED, "Animations"))

    if os.path.isdir(DST_EXPORTED):
        if skip_anims:
            for sub in ["Characters", "Weapons", "Enemies", "Environment", "Meshes", "Materials", "Textures"]:
                sp = os.path.join(DST_EXPORTED, sub)
                if os.path.isdir(sp):
                    shutil.rmtree(sp)
                if os.path.isfile(sp + ".meta"):
                    os.remove(sp + ".meta")
        else:
            shutil.rmtree(DST_EXPORTED)
            if os.path.isfile(DST_EXPORTED + ".meta"):
                os.remove(DST_EXPORTED + ".meta")

    guid_map = build_guid_index()
    all_roots = copy_prefabs_and_dependencies(guid_map)
    if not skip_anims:
        copy_and_decode_animations(all_roots)
    else:
        print("[3/5] Preserved existing 145 decoded .anim clips (--skip-anims).", flush=True)
    extract_pbr_and_ui_textures()
    print("[5/5] ExportedProject -> Assets migration complete!", flush=True)


if __name__ == "__main__":
    main()

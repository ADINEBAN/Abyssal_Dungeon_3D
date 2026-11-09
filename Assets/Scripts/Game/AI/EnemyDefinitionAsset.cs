using System;
using UnityEngine;

namespace AbyssGame
{
    public enum EliteAffixKind
    {
        None = 0,
        Vampiric = 1,     // Hút máu + hào quang huyết ngọc
        Molten = 2,       // Nổ dung nham xung quanh định kỳ
        Stormcaller = 3,  // Giật sét làm chậm mục tiêu ở gần
        FrozenPulse = 4   // Phát xung băng giá giảm tốc độ đánh
    }

    /// <summary>
    /// Phase 5 (Section 5 & Section 12): ScriptableObject definition for the 10 Enemy types + Final Boss.
    /// Decouples enemy stats, tactical role, 3D regalia asset ID, and Elite affix behavior from code.
    /// </summary>
    [CreateAssetMenu(fileName = "ED_Enemy", menuName = "Abyssal Dungeon/Enemy Definition", order = 21)]
    public class EnemyDefinitionAsset : ScriptableObject
    {
        public string enemyId;
        public string displayName;
        public Archetype tacticalRole;
        public ModelKind baseModelKind;
        public WeaponKind weaponKind;
        public string custom3DMeshPiece;
        public Color tintColor = Color.white;
        public float modelScale = 1f;

        [Header("Base Combat Stats (Floor 1)")]
        public float baseHp = 220f;
        public float baseAtk = 22f;
        public float baseDef = 8f;
        public float moveSpeed = 4.2f;
        public float attackRange = 2.0f;
        public float detectRange = 15f;
        public int expReward = 35;
        public int goldReward = 15;

        [Header("Tactical AI Tuning")]
        [Range(0f, 1f)] public float retreatHpThreshold = 0.25f;
        public float preferredMinRange = 0f;
        public float preferredMaxRange = 2.2f;
        public float specialSkillCooldown = 5.5f;

        public void PopulateFrom(EnemyDef d, EliteAffixKind affix)
        {
            PopulateFromDef(d);
        }

        public void PopulateFromDef(EnemyDef d)
        {
            if (d == null) return;
            enemyId = d.id;
            displayName = d.name;
            tacticalRole = d.archetype;
            baseModelKind = d.model;
            weaponKind = d.weapon;
            custom3DMeshPiece = EnemyVisualArmorKit.GetEnemyMeshPieceName(d.id);
            tintColor = d.tint;
            modelScale = d.scale;
            baseHp = d.hp;
            baseAtk = d.atk;
            baseDef = d.def;
            moveSpeed = d.speed;
            attackRange = d.attackRange;
            detectRange = d.detect;
            expReward = d.exp;
            goldReward = d.gold;

            if (d.archetype == Archetype.Ranged || d.archetype == Archetype.Mage)
            {
                preferredMinRange = 5.4f;
                preferredMaxRange = d.attackRange;
                retreatHpThreshold = 0.35f;
            }
            else if (d.archetype == Archetype.Assassin)
            {
                preferredMinRange = 0f;
                preferredMaxRange = d.attackRange;
                retreatHpThreshold = 0.30f;
            }
            else if (d.archetype == Archetype.Tank)
            {
                preferredMinRange = 0f;
                preferredMaxRange = d.attackRange;
                retreatHpThreshold = 0.0f;
            }
        }
    }
}

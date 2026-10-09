using UnityEngine;
using AbyssalDungeon.Loot;
using AbyssalDungeon.Classes;

namespace AbyssalDungeon.Enemy
{
    public enum EnemyType
    {
        Melee,
        Ranged,
        Tank,
        Mage,
        Assassin,
        Elite,
        MiniBoss
    }

    [CreateAssetMenu(fileName = "NewEnemy", menuName = "Abyssal Dungeon/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Basic Info")]
        public string enemyName;
        [TextArea(3, 10)]
        public string description;
        public EnemyType enemyType;
        public Color enemyColor = Color.red;

        [Header("Stats")]
        public float maxHP;
        public float attack;
        public float defense;
        public float movementSpeed;

        [Header("Combat")]
        public float detectionRange;
        public float attackRange;
        public float attackCooldown;

        [Header("Rewards")]
        public int expReward;
        public int goldReward;
        public LootTable lootTable;

        [Header("Visuals & Skills")]
        public GameObject prefab;
        public SkillData[] skills;
    }
}

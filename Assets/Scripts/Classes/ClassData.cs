using UnityEngine;

namespace AbyssalDungeon.Classes
{
    public enum ClassRole
    {
        Tank,
        MeleeDPS,
        RangedDPS,
        Healer,
        Support,
        Hybrid
    }

    public enum WeaponType
    {
        Sword,
        GreatSword,
        DualBlades,
        Bow,
        Staff,
        Wand,
        Shield,
        Fist,
        Scythe,
        Mace
    }

    [CreateAssetMenu(fileName = "NewClass", menuName = "Abyssal Dungeon/Class Data")]
    public class ClassData : ScriptableObject
    {
        [Header("Basic Info")]
        public string className;
        [TextArea(3, 10)]
        public string description;
        public ClassRole role;
        public Sprite icon;
        public Color classColor = Color.white;

        [Header("Base Stats")]
        public float baseHP;
        public float baseMana;
        public float attack;
        public float defense;
        public float attackSpeed;
        [Range(0f, 1f)]
        public float criticalChance;
        public float movementSpeed;

        [Header("Combat & Skills")]
        public WeaponType weaponType;
        public SkillData[] skills;
        [TextArea(3, 10)]
        public string passiveDescription;
    }
}

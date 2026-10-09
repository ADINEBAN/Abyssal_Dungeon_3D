using UnityEngine;

namespace AbyssalDungeon.Classes
{
    public enum DamageType
    {
        Physical,
        Magical,
        True,
        Elemental
    }

    public enum SkillType
    {
        Melee,
        Ranged,
        AoE,
        Buff,
        Debuff,
        Heal,
        Summon,
        Dash
    }

    public enum StatusEffect
    {
        None,
        Stun,
        Slow,
        Burn,
        Poison,
        Freeze,
        Silence
    }

    [CreateAssetMenu(fileName = "NewSkill", menuName = "Abyssal Dungeon/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("Basic Info")]
        public string skillName;
        [TextArea(3, 10)]
        public string description;
        public Sprite icon;

        [Header("Skill Properties")]
        public SkillType skillType;
        public DamageType damageType;
        public bool isUltimate;

        [Header("Cost & Timers")]
        public float manaCost;
        public float cooldown;
        public float castTime;

        [Header("Combat Stats")]
        public float damageMultiplier;
        public float range;
        public float radius; // For AoE
        public float duration; // For buffs/debuffs

        [Header("Status Effect")]
        public StatusEffect statusEffect;
        public float statusDuration;

        [Header("Visuals")]
        public GameObject vfxPrefab;
    }
}

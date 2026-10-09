using UnityEngine;

namespace AbyssalDungeon.Combat
{
    public enum DamageType
    {
        Physical,
        Magic,
        Fire,
        Ice,
        Poison,
        Dark,
        Holy
    }

    public enum StatusEffect
    {
        None,
        Burn,
        Poison,
        Freeze,
        Stun,
        Slow,
        Curse,
        Knockback
    }

    [System.Serializable]
    public class DamageInfo
    {
        public GameObject attacker;
        public GameObject target;
        public float baseDamage;
        public float finalDamage;
        public DamageType damageType = DamageType.Physical;
        public bool isCritical = false;
        public float skillMultiplier = 1f;
        public float criticalMultiplier = 2f;
        
        public StatusEffect statusEffect = StatusEffect.None;
        public float statusDuration = 0f;
        
        public float knockbackForce = 0f;
        public Vector3 hitPoint;
    }
}

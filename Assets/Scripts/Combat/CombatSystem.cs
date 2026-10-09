using UnityEngine;

namespace AbyssalDungeon.Combat
{
    public static class CombatSystem
    {
        public static float CalculateDamage(DamageInfo info, float targetDefense)
        {
            float rawDamage = info.baseDamage * info.skillMultiplier;
            if (info.isCritical)
            {
                rawDamage *= info.criticalMultiplier;
            }

            float finalDamage = rawDamage - targetDefense;
            return Mathf.Max(1f, finalDamage); // Clamp minimum damage to 1
        }
    }
}

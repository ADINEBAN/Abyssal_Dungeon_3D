using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Data-driven per-class weapon loadout + combat timing profile (Phase 2 & Phase 4)
    /// in its own matching .cs file so Unity reliably loads Loadout_*.asset via Resources.Load.
    /// </summary>
    [CreateAssetMenu(menuName = "Abyssal Dungeon/Class Weapon Loadout Profile", fileName = "Loadout_Class")]
    public class ClassWeaponLoadoutProfile : ScriptableObject
    {
        public int ClassId;
        public string ClassName;
        public WeaponAttachmentProfile MainHandProfile;
        public WeaponAttachmentProfile OffHandProfile;
        public GameObject ProjectileVisualPrefab;

        [Header("Synchronized Animation / Hitbox / VFX Timing (Seconds)")]
        public float BasicWindup = 0.08f;
        public float BasicActive = 0.13f;
        public float HeavyWindup = 0.16f;
        public float HeavyActive = 0.18f;
        public float SkillCastWindup = 0.14f;
    }
}

using System;
using UnityEngine;

namespace AbyssalDungeon.VFX
{
    public enum VFXAnimationStyle
    {
        HolyPillar = 0,
        EnergyDome = 1,
        AngelWings = 2,
        ShockwaveSlam = 3,
        EntangleRoots = 4,
        SpiralVortex = 5,
        GroundTelegraph = 6,
        ProjectileLoop = 7
    }

    [Serializable]
    public struct SkillVfxSlot
    {
        public string slotLabel;
        public string skillName;
        public string vfxPoolKey;
        public GameObject vfxPrefab;
        public float defaultScale;
        public float defaultDuration;
    }

    /// <summary>
    /// Component attached to Character Prefabs (Arthur/Cleric, Lyra/Knight, Kael/Mage, Seraphina/Ranger)
    /// to bind their skill slots directly to pooled 3-layer 3D VFX prefabs.
    /// </summary>
    [DisallowMultipleComponent]
    public class HeroSkillVfxBinding : MonoBehaviour
    {
        public string heroDisplayName = "Arthur";
        public string className = "Cleric";
        public int classId = 9;
        public SkillVfxSlot[] skillSlots = new SkillVfxSlot[4];
        public GameObject projectileVfxPrefab;
        public string projectileVfxKey;
        public GameObject dashVfxPrefab;
        public string dashVfxKey;

        public GameObject SpawnSkillVfx(int skillIndex, Vector3 position, Quaternion rotation, float scaleMultiplier = 1f)
        {
            if (skillSlots != null && skillIndex >= 0 && skillIndex < skillSlots.Length)
            {
                SkillVfxSlot slot = skillSlots[skillIndex];
                float finalScale = (slot.defaultScale > 0.01f ? slot.defaultScale : 1f) * scaleMultiplier;
                float dur = slot.defaultDuration > 0.05f ? slot.defaultDuration : -1f;
                if (slot.vfxPrefab != null)
                {
                    return VFXPoolManager.Instance.Spawn(slot.vfxPrefab, position, rotation, null, finalScale, dur);
                }
                if (!string.IsNullOrEmpty(slot.vfxPoolKey))
                {
                    return VFXPoolManager.Instance.Spawn(slot.vfxPoolKey, position, rotation, null, finalScale, dur);
                }
            }
            return VFXPoolManager.Instance.SpawnForClassSkill(classId, skillIndex, position, rotation, scaleMultiplier);
        }
    }
}


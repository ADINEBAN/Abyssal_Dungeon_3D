using System;
using UnityEngine;

namespace AbyssGame
{
    [Serializable]
    public struct BuildSpecEntry
    {
        public string buildName;
        [TextArea(1, 3)] public string description;
        public Color accentColor;
    }

    [Serializable]
    public struct SkillSpecEntry
    {
        public string skillName;
        [TextArea(1, 3)] public string description;
        public SkillKind kind;
        public float baseCooldown;
        public float manaCost;
        public float powerMultiplier;
        public float radius;
        public float range;
        public bool isUltimate;
        public Color vfxColor;
    }

    /// <summary>
    /// Modular Data-Driven ScriptableObject for each of the 10 Aether Hunter Classes (unity-scriptableobjects skill).
    /// Auto-populated into Assets/ScriptableObjects/Classes/ & Assets/Resources/Classes/ by AbyssalAutoSetup
    /// so designers can inspect and balance all 10 classes, 30 builds, and 40 skills directly in the Unity Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Abyssal Dungeon/Class Definition Asset", fileName = "CD_NewClass")]
    public class ClassDefinitionAsset : ScriptableObject
    {
        public int classId;
        public string className;
        public Role combatRole;
        public string roleTitle;
        [TextArea(2, 4)] public string loreAndIdentity;

        [Header("Base Stats (Level 1)")]
        public float baseHp = 900f;
        public float baseMana = 200f;
        public float baseAtk = 85f;
        public float baseDef = 35f;
        public float baseAtkSpeed = 1.0f;
        public float baseCritChance = 0.15f;
        public float baseMoveSpeed = 5.4f;
        public bool isRanged;
        public float attackRange = 2.6f;

        [Header("Visual Silhouette & Equipment Compatibility")]
        public ModelKind baseRigModel;
        public WeaponKind signatureWeapon;
        public EquipArchetype compatibleArmorArchetype;
        public Color primaryClassColor = Color.white;
        public string headgearMeshId;
        public string backRegaliaMeshId;
        public string mainWeaponMeshId;
        public string offhandMeshId;

        [Header("Passive & 3 Build Specializations")]
        public string passiveAbilityDescription;
        public BuildSpecEntry[] builds = new BuildSpecEntry[3];

        [Header("4 Signature Class Skills (Q / W / E / R)")]
        public SkillSpecEntry[] skills = new SkillSpecEntry[4];

        public void SyncFromDatabase(ClassDef def)
        {
            if (def == null) return;
            classId = def.id;
            className = def.name;
            combatRole = def.role;
            roleTitle = def.roleText;
            loreAndIdentity = def.desc;

            baseHp = def.hp;
            baseMana = def.mana;
            baseAtk = def.atk;
            baseDef = def.def;
            baseAtkSpeed = def.atkSpeed;
            baseCritChance = def.crit;
            baseMoveSpeed = def.moveSpeed;
            isRanged = def.ranged;
            attackRange = def.attackRange;

            baseRigModel = def.model;
            signatureWeapon = def.weapon;
            compatibleArmorArchetype = ClassSpecializationSystem.GetClassArchetype(def.id);
            primaryClassColor = def.color;
            passiveAbilityDescription = def.passive;

            ClassVisualArmorKit.GetClassMeshIds(def.id, out headgearMeshId, out backRegaliaMeshId, out mainWeaponMeshId, out offhandMeshId);

            builds = new BuildSpecEntry[3];
            for (int b = 0; b < 3; b++)
            {
                builds[b] = new BuildSpecEntry
                {
                    buildName = def.builds != null && b < def.builds.Length ? def.builds[b] : ("Build " + (b + 1)),
                    description = def.buildDesc != null && b < def.buildDesc.Length ? def.buildDesc[b] : "",
                    accentColor = ClassSpecializationSystem.GetBuildAccentColor(def.id, b, def.color)
                };
            }

            skills = new SkillSpecEntry[4];
            for (int s = 0; s < 4 && def.skills != null && s < def.skills.Length; s++)
            {
                var sk = def.skills[s];
                skills[s] = new SkillSpecEntry
                {
                    skillName = sk.name,
                    description = sk.desc,
                    kind = sk.kind,
                    baseCooldown = sk.cooldown,
                    manaCost = sk.mana,
                    powerMultiplier = sk.power,
                    radius = sk.radius,
                    range = sk.range,
                    isUltimate = sk.ultimate,
                    vfxColor = sk.color
                };
            }
        }
    }
}


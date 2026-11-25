using UnityEngine;

namespace AbyssGame
{
    public enum WeaponGripType
    {
        OneHandedSword = 0,
        GreatswordTwoHanded = 1,
        DualWield = 2,
        Bow = 3,
        Staff = 4,
        Shield = 5,
        Hammer = 6,
        FistWeapon = 7,
        BookOffhand = 8
    }

    /// <summary>
    /// Data-driven per-weapon attachment profile (Phase 2.2) in its own matching .cs file so Unity
    /// reliably serializes and loads .asset instances via Resources.Load.
    /// </summary>
    [CreateAssetMenu(menuName = "Abyssal Dungeon/Weapon Attachment Profile", fileName = "WAP_Weapon")]
    public class WeaponAttachmentProfile : ScriptableObject
    {
        [Header("Weapon Asset")]
        public string WeaponId = "";
        public GameObject WeaponPrefab;
        public WeaponGripType GripType = WeaponGripType.OneHandedSword;

        [Header("Skeleton Attachment")]
        public string PrimaryHandBone = "RH_WP";
        public string[] FallbackHandBones = new[] { "Bip001 R Hand", "RightHand", "Hand_R" };
        public string SecondaryHandBone = "";
        public bool IsFloatingRelic = false;

        [Header("Local Transform (Relative to Socket)")]
        public Vector3 LocalPosition = Vector3.zero;
        public Vector3 LocalRotation = Vector3.zero;
        public Vector3 LocalScale = Vector3.one;
        public Vector3 GripOffset = Vector3.zero;

        [Header("Sockets & Trail")]
        public bool HasBladeTrail = true;
        public string TipSocketName = "VfxTrailSocket";
        public string ProjectileSocketName = "ProjectileSpawn";
        public string ImpactSocketName = "BladeTip";
        public Vector3 FallbackTipLocalOffset = new Vector3(0f, 0f, 1.1f);
        public float TrailWidth = 0.28f;
        public float AuraScale = 0.95f;
    }
}

using System;
using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Unified Weapon Rigging & Socket Controller attached to every Hero Visual (Phase 2 & Phase 5)
    /// in its own matching .cs file for clean Unity serialization.
    /// </summary>
    public class WeaponRigController : MonoBehaviour
    {
        public ClassWeaponLoadoutProfile Loadout;
        public Transform MainWeaponInstance;
        public Transform OffhandWeaponInstance;
        public Transform ProjectileSocket;
        public Transform MagicEmitSocket;
        public Transform MainImpactSocket;
        public Transform OffhandImpactSocket;
        public BowStringRigAnimator BowRig;

        public static ClassWeaponLoadoutProfile LoadProfileForClass(string className)
        {
            if (string.IsNullOrEmpty(className)) return null;
            return Resources.Load<ClassWeaponLoadoutProfile>("Weapons/Profiles/Loadout_" + className);
        }

        public static WeaponRigController BindToHero(GameObject visualRoot, ClassDef cls, Color accentColor)
        {
            if (visualRoot == null || cls == null) return null;

            var ctrl = visualRoot.GetComponent<WeaponRigController>();
            if (ctrl == null) ctrl = visualRoot.AddComponent<WeaponRigController>();

            ctrl.Loadout = LoadProfileForClass(cls.name);
            ctrl.ResolveOrMountWeapons(cls, accentColor);
            return ctrl;
        }

        private void ResolveOrMountWeapons(ClassDef cls, Color accentColor)
        {
            Transform rig = transform.Find("Rig") ?? transform;

            if (Loadout != null)
            {
                if (Loadout.MainHandProfile != null)
                    MainWeaponInstance = MountSingleProfile(rig, Loadout.MainHandProfile, "SignatureMainWeapon", accentColor);
                if (Loadout.OffHandProfile != null)
                    OffhandWeaponInstance = MountSingleProfile(rig, Loadout.OffHandProfile, "SignatureOffhand", accentColor);
            }
            else
            {
                MainWeaponInstance = FindDeep(rig, "SignatureMainWeapon");
                OffhandWeaponInstance = FindDeep(rig, "SignatureOffhand") ?? FindDeep(transform, "WarlockFloatingGrimoire");
            }

            if (MainWeaponInstance == null && OffhandWeaponInstance == null)
            {
                Debug.LogError($"[WeaponRigController] Class '{cls.name}' (id={cls.id}) has NO weapon mounted on MainHand or OffHand! Check Loadout_{cls.name} asset.");
            }

            CacheSockets();
        }

        private Transform MountSingleProfile(Transform rig, WeaponAttachmentProfile profile, string slotName, Color accentColor)
        {
            if (profile == null) return null;
            if (profile.WeaponPrefab == null)
            {
                Debug.LogError($"[WeaponRigController] WeaponAttachmentProfile '{profile.name}' has null WeaponPrefab!");
                return null;
            }

            Transform socket;
            if (profile.IsFloatingRelic)
            {
                socket = transform;
                slotName = "WarlockFloatingGrimoire";
            }
            else
            {
                socket = FindBone(rig, profile.PrimaryHandBone, profile.FallbackHandBones);
                if (socket == null)
                {
                    Debug.LogError($"[WeaponRigController] Could not locate hand socket '{profile.PrimaryHandBone}' on rig '{name}' for weapon '{profile.WeaponId}'.");
                    return null;
                }
            }

            Transform existing = socket.Find(slotName);
            if (existing != null) Destroy(existing.gameObject);
            if (profile.IsFloatingRelic)
            {
                Transform oldBook = transform.Find("WarlockFloatingGrimoire");
                if (oldBook != null) Destroy(oldBook.gameObject);
            }

            GameObject inst = Instantiate(profile.WeaponPrefab, socket, false);
            inst.name = slotName;

            Quaternion rotQ = Quaternion.Euler(profile.LocalRotation);
            Vector3 localGripShift = profile.GripOffset;
            Transform grip = FindDeep(inst.transform, "GripPoint");
            if (grip != null)
                localGripShift -= inst.transform.InverseTransformPoint(grip.position);

            inst.transform.localRotation = rotQ;
            inst.transform.localScale = profile.LocalScale;
            inst.transform.localPosition = profile.LocalPosition + (rotQ * Vector3.Scale(localGripShift, profile.LocalScale));

            foreach (var col in inst.GetComponentsInChildren<Collider>(true))
                Destroy(col);
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = true;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
            }

            if (profile.IsFloatingRelic)
            {
                var floater = inst.GetComponent<ClassRelicFloatAnimator>();
                if (floater == null) floater = inst.AddComponent<ClassRelicFloatAnimator>();
                floater.OrbitMode = true;
            }

            if (profile.GripType == WeaponGripType.Bow)
            {
                BowRig = inst.GetComponent<BowStringRigAnimator>();
                if (BowRig == null) BowRig = inst.AddComponent<BowStringRigAnimator>();
                BowRig.InitializeSockets();
            }

            if (profile.HasBladeTrail)
            {
                Transform tipSocket = !string.IsNullOrEmpty(profile.TipSocketName) ? FindDeep(inst.transform, profile.TipSocketName) : null;
                Vector3 tipLocal = tipSocket != null
                    ? inst.transform.InverseTransformPoint(tipSocket.position)
                    : profile.FallbackTipLocalOffset;
                WeaponBladeTrail3D.Attach(inst.transform, tipLocal, profile.TrailWidth, accentColor);
            }

            // Only attach 3D blade aura sheath on bladed melee weapons (never bows/shields/staffs/books!)
            bool useBladeAura = profile.GripType == WeaponGripType.OneHandedSword
                             || profile.GripType == WeaponGripType.GreatswordTwoHanded
                             || profile.GripType == WeaponGripType.DualWield;
            if (useBladeAura && profile.AuraScale > 0.05f)
            {
                AbyssalDungeon.VFX.WeaponVfxFactory.AttachSmartWeaponAura(inst.transform, accentColor, profile.AuraScale * 0.65f);
            }

            return inst.transform;
        }

        private void CacheSockets()
        {
            ProjectileSocket = FindSocketOnWeapons("ProjectileSpawn", "ArrowNock", "MagicEmitSocket", "BladeTip");
            MagicEmitSocket = FindSocketOnWeapons("MagicEmitSocket", "ProjectileSpawn", "BladeTip");
            MainImpactSocket = MainWeaponInstance != null
                ? (FindDeep(MainWeaponInstance, "ImpactPoint") ?? FindDeep(MainWeaponInstance, "BladeTip") ?? FindDeep(MainWeaponInstance, "VfxTrailSocket"))
                : null;
            OffhandImpactSocket = OffhandWeaponInstance != null
                ? (FindDeep(OffhandWeaponInstance, "ShieldFrontCenter") ?? FindDeep(OffhandWeaponInstance, "ImpactPoint") ?? FindDeep(OffhandWeaponInstance, "BladeTip"))
                : null;

            if (BowRig == null)
                BowRig = GetComponentInChildren<BowStringRigAnimator>(true);
        }

        public Vector3 GetProjectileSpawnWorldPos(Vector3 fallbackWorldPos, Vector3 aimDir)
        {
            if (ProjectileSocket != null && ProjectileSocket.gameObject.activeInHierarchy)
                return ProjectileSocket.position;
            if (MagicEmitSocket != null && MagicEmitSocket.gameObject.activeInHierarchy)
                return MagicEmitSocket.position;
            return fallbackWorldPos + aimDir * 0.55f;
        }

        public Vector3 GetImpactWorldPos(Vector3 fallbackWorldPos)
        {
            if (MainImpactSocket != null && MainImpactSocket.gameObject.activeInHierarchy)
                return MainImpactSocket.position;
            if (OffhandImpactSocket != null && OffhandImpactSocket.gameObject.activeInHierarchy)
                return OffhandImpactSocket.position;
            return fallbackWorldPos;
        }

        public void TriggerBowDraw(float windupDuration)
        {
            if (BowRig != null) BowRig.TriggerDraw(windupDuration);
        }

        public void EnsureWeaponsVisible()
        {
            if (MainWeaponInstance != null && !MainWeaponInstance.gameObject.activeSelf)
                MainWeaponInstance.gameObject.SetActive(true);
            if (OffhandWeaponInstance != null && !OffhandWeaponInstance.gameObject.activeSelf)
                OffhandWeaponInstance.gameObject.SetActive(true);
        }

        private Transform FindSocketOnWeapons(params string[] names)
        {
            for (int n = 0; n < names.Length; n++)
            {
                if (MainWeaponInstance != null)
                {
                    var t = FindDeep(MainWeaponInstance, names[n]);
                    if (t != null) return t;
                }
                if (OffhandWeaponInstance != null)
                {
                    var t = FindDeep(OffhandWeaponInstance, names[n]);
                    if (t != null) return t;
                }
            }
            return null;
        }

        private static Transform FindBone(Transform root, string primary, string[] fallbacks)
        {
            if (root == null) return null;
            if (!string.IsNullOrEmpty(primary))
            {
                var t = FindDeep(root, primary);
                if (t != null) return t;
            }
            if (fallbacks != null)
            {
                for (int i = 0; i < fallbacks.Length; i++)
                {
                    var t = FindDeep(root, fallbacks[i]);
                    if (t != null) return t;
                }
            }
            return null;
        }

        private static Transform FindDeep(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrEmpty(targetName)) return null;
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (string.Equals(all[i].name, targetName, StringComparison.OrdinalIgnoreCase))
                    return all[i];
            return null;
        }
    }
}

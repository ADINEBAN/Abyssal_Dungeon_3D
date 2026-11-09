using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Builds 10 Unmistakable Dark Fantasy 3D Character Silhouettes, Armor Regalia, Pauldrons, Back Relics,
    /// and Signature 3D Weapons from authored .obj meshes in Resources/EnvironmentMeshes/ (Phase 4 - Section 2.2 & 4).
    /// Ensures Knight, Guardian, Berserker, Assassin, Ranger, Mage, Warlock, Monk, Druid, and Cleric each have
    /// unique silhouettes, headgear, armor trim, weapons, and build-reactive glowing relics!
    /// </summary>
    public static class ClassVisualArmorKit
    {
        public static void GetClassMeshIds(int classId, out string helmId, out string backId, out string mainWeapId, out string offhandId)
        {
            helmId = "";
            backId = "";
            mainWeapId = "";
            offhandId = "";

            switch (classId)
            {
                case 0: // Knight
                    helmId = "ClassHelm_Knight";
                    mainWeapId = "ClassWeapon_KnightSword";
                    offhandId = "ClassShield_KnightCrest";
                    break;
                case 1: // Guardian
                    helmId = "ClassHelm_Guardian";
                    mainWeapId = "ClassWeapon_GuardianHammer";
                    offhandId = "ClassShield_GuardianBulwark";
                    break;
                case 2: // Berserker
                    helmId = "ClassHelm_Berserker";
                    mainWeapId = "ClassWeapon_BerserkerCleaver";
                    break;
                case 3: // Assassin
                    helmId = "ClassHelm_Assassin";
                    backId = "ClassBack_AssassinCloak";
                    mainWeapId = "ClassWeapon_AssassinDagger";
                    offhandId = "ClassWeapon_AssassinDagger";
                    break;
                case 4: // Ranger
                    helmId = "ClassHelm_Ranger";
                    backId = "ClassBack_RangerQuiver";
                    offhandId = "ClassWeapon_RangerBow";
                    break;
                case 5: // Mage
                    helmId = "ClassHelm_Mage";
                    backId = "ClassBack_MageAstrolabe";
                    mainWeapId = "ClassWeapon_MageStaff";
                    break;
                case 6: // Warlock
                    helmId = "ClassHelm_Warlock";
                    backId = "ClassBack_WarlockGrimoire";
                    mainWeapId = "ClassWeapon_WarlockScepter";
                    break;
                case 7: // Monk
                    helmId = "ClassHelm_Monk";
                    backId = "ClassBack_MonkBeads";
                    mainWeapId = "ClassWeapon_MonkGauntlet";
                    offhandId = "ClassWeapon_MonkGauntlet";
                    break;
                case 8: // Druid
                    helmId = "ClassHelm_Druid";
                    mainWeapId = "ClassWeapon_DruidScytheStaff";
                    break;
                case 9: // Cleric
                    helmId = "ClassHelm_Cleric";
                    mainWeapId = "ClassWeapon_ClericMace";
                    offhandId = "ClassShield_ClericReliquary";
                    break;
            }
        }

        public static void DressHeroClassRegalia(
            ClassDef cls,
            int buildIndex,
            GameObject visualRoot,
            Animator animator,
            Transform rHand,
            Transform lHand,
            Transform head)
        {
            if (cls == null || visualRoot == null) return;

            Color accent = ClassSpecializationSystem.GetBuildAccentColor(cls.id, buildIndex, cls.color);
            GetClassMeshIds(cls.id, out string helmId, out string backId, out string mainWeapId, out string offhandId);

            // Distinct silhouette proportions per class role so even in pure silhouette they read instantly!
            Vector3 bodyScale = Vector3.one;
            switch (cls.id)
            {
                case 0: bodyScale = new Vector3(1.04f, 1.02f, 1.04f); break; // Knight: heroic balanced plate
                case 1: bodyScale = new Vector3(1.14f, 1.05f, 1.14f); break; // Guardian: towering broad bulwark
                case 2: bodyScale = new Vector3(1.12f, 1.08f, 1.08f); break; // Berserker: muscular juggernaut
                case 3: bodyScale = new Vector3(0.94f, 0.98f, 0.94f); break; // Assassin: sleek agile stalker
                case 4: bodyScale = new Vector3(0.97f, 1.01f, 0.97f); break; // Ranger: tall nimble archer
                case 5: bodyScale = new Vector3(0.96f, 1.00f, 0.96f); break; // Mage: slender archmage
                case 6: bodyScale = new Vector3(1.00f, 1.03f, 1.00f); break; // Warlock: imposing dark occultist
                case 7: bodyScale = new Vector3(0.99f, 0.99f, 0.99f); break; // Monk: athletic martial artist
                case 8: bodyScale = new Vector3(1.03f, 1.04f, 1.02f); break; // Druid: wildkeeper stature
                case 9: bodyScale = new Vector3(1.00f, 1.01f, 1.00f); break; // Cleric: sacred prelate
            }
            visualRoot.transform.localScale = bodyScale;

            // Authentic ExportedProject 3D Hero Rigs already include full PBR armor & mounted 3D weapons on RH_WP / LH_WP.
            Transform exportedRig = visualRoot.transform.Find("Rig");
            if (exportedRig != null)
            {
                Transform rWeaponSocket = rHand != null ? rHand : visualRoot.transform;
                Transform lWeaponSocket = lHand != null ? lHand : visualRoot.transform;
                Transform mainWpn = rHand != null ? rHand.Find("SignatureMainWeapon") : null;
                Transform offWpn = lHand != null ? lHand.Find("SignatureOffhand") : null;

                if (!cls.ranged || cls.id == 5 || cls.id == 6 || cls.id == 8)
                {
                    float bladeLen = cls.id == 2 ? 1.18f : (cls.id == 5 || cls.id == 8 ? 0.95f : (cls.id == 7 ? 0.22f : 0.72f));
                    float trailWidth = cls.id == 2 ? 0.54f : 0.34f;
                    WeaponBladeTrail3D.Attach(
                        mainWpn != null ? mainWpn : rWeaponSocket,
                        new Vector3(0f, bladeLen * 0.55f, bladeLen * 0.45f),
                        trailWidth,
                        accent);
                }

                if (cls.id == 3 || cls.id == 7) // Dual-wield Assassin & Monk offhand trail
                {
                    WeaponBladeTrail3D.Attach(
                        offWpn != null ? offWpn : lWeaponSocket,
                        new Vector3(0f, 0.28f, 0.22f),
                        0.26f,
                        accent);
                }

                Transform warlockBook = visualRoot.transform.Find("WarlockFloatingGrimoire");
                if (warlockBook != null && warlockBook.GetComponent<ClassRelicFloatAnimator>() == null)
                {
                    var floatAnim = warlockBook.gameObject.AddComponent<ClassRelicFloatAnimator>();
                    floatAnim.OrbitMode = true;
                }

                Transform existingRing = visualRoot.transform.Find("ClassRing");
                if (existingRing != null) Object.Destroy(existingRing.gameObject);
                return;
            }

            // Locate Chest / Shoulder sockets for Pauldrons & Back Regalia
            Transform chest = null;
            Transform lShoulder = null;
            Transform rShoulder = null;
            if (animator != null && animator.isHuman)
            {
                chest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
                if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Chest);
                if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Spine);
                lShoulder = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                if (lShoulder == null) lShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
                rShoulder = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                if (rShoulder == null) rShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            }
            if (chest == null) chest = visualRoot.transform;

            // Craft rich PBR Dark Fantasy materials per armor archetype
            Material primaryArmorMat = BuildClassArmorMaterial(cls.id, accent);
            Material accentGlowMat = CharacterVisualFactory.MakeMat(accent, 0.88f, 0.25f, accent * 2.1f);
            Material forgedSteelMat = PbrMaterialLibrary.WroughtIron();
            Material runicGoldMat = PbrMaterialLibrary.RunicGold();

            // 1. Headgear / Crown / Hood / Horned Skull Helm
            if (!string.IsNullOrEmpty(helmId) && head != null)
            {
                Material helmMat = (cls.id == 1 || cls.id == 9) ? runicGoldMat : primaryArmorMat;
                Vector3 helmOffset = (cls.id == 5 || cls.id == 7) ? new Vector3(0f, 0.11f, 0f) : new Vector3(0f, 0.04f, 0f);
                var helmGo = ObjMeshLoader.SpawnPiece(helmId, head, helmOffset, Quaternion.identity, Vector3.one, helmMat, false, true);
                helmGo.name = "ClassHeadgear";
            }

            // 2. Class-Specific Shoulder Pauldrons (Heavy Spiked / Winged / Mantle)
            AttachClassPauldrons(cls.id, lShoulder, rShoulder, primaryArmorMat, accentGlowMat, runicGoldMat);

            // 3. Back Regalia / Floating Arcane Relics (Quiver, Shadow Cloak, Astrolabe, Grimoire, Prayer Beads)
            if (!string.IsNullOrEmpty(backId))
            {
                bool floatingRelic = (cls.id == 5 || cls.id == 6 || cls.id == 7);
                Material backMat = floatingRelic ? accentGlowMat : primaryArmorMat;
                Transform backParent = floatingRelic ? visualRoot.transform : chest;
                Vector3 backPos = floatingRelic
                    ? (cls.id == 6 ? new Vector3(-0.52f, 1.48f, 0.18f) : new Vector3(0f, 1.32f, -0.26f))
                    : new Vector3(0f, 0.12f, -0.16f);

                var backGo = ObjMeshLoader.SpawnPiece(backId, backParent, backPos, Quaternion.identity, Vector3.one, backMat, false, true);
                backGo.name = "ClassBackRegalia";

                if (floatingRelic)
                {
                    var anim = backGo.AddComponent<ClassRelicFloatAnimator>();
                    anim.OrbitMode = (cls.id == 6); // Warlock Grimoire orbits & bobs!
                    anim.SpinZ = (cls.id == 5);     // Mage Astrolabe rotates!
                }
            }

            // 4. Signature 3D Polygonal Weapons & Shields + URP Blade Trails
            Transform rSocket = rHand != null ? rHand : visualRoot.transform;
            Transform lSocket = lHand != null ? lHand : visualRoot.transform;

            // Note: Paladin FBX has a baked sword/shield on Knight (id=0), so for Knight we add a glowing runic aura edge,
            // while Guardian, Berserker, Assassin, Ranger, Mage, Warlock, Monk, Druid, and Cleric get full authored .obj weapons!
            if (cls.id == 0)
            {
                WeaponBladeTrail3D.Attach(rSocket, new Vector3(0f, 0.72f, 0.10f), 0.38f, accent);
            }
            else
            {
                if (!string.IsNullOrEmpty(mainWeapId))
                {
                    Transform mainSocket = (cls.id == 4) ? lSocket : rSocket; // Ranger holds Bow in LeftHand
                    Material weapMat = (cls.id == 5 || cls.id == 6 || cls.id == 8) ? accentGlowMat : ((cls.id == 1 || cls.id == 9) ? runicGoldMat : forgedSteelMat);
                    Quaternion wRot = cls.id == 4 ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
                    var wGo = ObjMeshLoader.SpawnPiece(mainWeapId, mainSocket, Vector3.zero, wRot, Vector3.one, weapMat, false, true);
                    wGo.name = "SignatureMainWeapon";

                    if (!cls.ranged || cls.id == 5 || cls.id == 8)
                    {
                        float tipY = cls.id == 2 ? 1.32f : (cls.id == 5 || cls.id == 8 ? 1.18f : (cls.id == 7 ? 0.22f : 0.68f));
                        float trailW = cls.id == 2 ? 0.56f : 0.32f;
                        WeaponBladeTrail3D.Attach(wGo.transform, new Vector3(0f, tipY, 0f), trailW, accent);
                    }
                }

                if (!string.IsNullOrEmpty(offhandId))
                {
                    Transform offSocket = (cls.id == 3 || cls.id == 7) ? lSocket : lSocket;
                    Quaternion offRot = Quaternion.identity;
                    Vector3 offPos = Vector3.zero;
                    if (cls.id == 1) // Guardian Tower Bulwark
                    {
                        offPos = new Vector3(-0.08f, 0.04f, 0.06f);
                        offRot = Quaternion.Euler(0f, -90f, 0f);
                    }
                    else if (cls.id == 9) // Cleric Sun Reliquary Buckler
                    {
                        offPos = new Vector3(-0.08f, 0.04f, 0.04f);
                        offRot = Quaternion.Euler(0f, 0f, 90f);
                    }

                    Material offMat = (cls.id == 1 || cls.id == 9) ? runicGoldMat : (cls.id == 3 || cls.id == 7 ? accentGlowMat : forgedSteelMat);
                    var offGo = ObjMeshLoader.SpawnPiece(offhandId, offSocket, offPos, offRot, Vector3.one, offMat, false, true);
                    offGo.name = "SignatureOffhand";

                    if (cls.id == 3 || cls.id == 7) // Dual-wield Assassin & Monk left-hand weapon trail!
                    {
                        WeaponBladeTrail3D.Attach(offGo.transform, new Vector3(0f, 0.36f, 0f), 0.26f, accent);
                    }
                }
            }

            // Remove any legacy flat colored ring so the character's real 3D human shadow is unobstructed
            Transform oldRing = visualRoot.transform.Find("ClassRing");
            if (oldRing != null) Object.Destroy(oldRing.gameObject);
        }

        private static Material BuildClassArmorMaterial(int classId, Color accent)
        {
            switch (classId)
            {
                case 0: return CharacterVisualFactory.MakeMat(new Color(0.56f, 0.66f, 0.82f), 0.72f, 0.85f, accent * 0.18f);
                case 1: return CharacterVisualFactory.MakeMat(new Color(0.86f, 0.72f, 0.36f), 0.74f, 0.88f, accent * 0.20f);
                case 2: return CharacterVisualFactory.MakeMat(new Color(0.24f, 0.14f, 0.15f), 0.52f, 0.72f, accent * 0.28f);
                case 3: return CharacterVisualFactory.MakeMat(new Color(0.14f, 0.11f, 0.20f), 0.38f, 0.25f, accent * 0.25f);
                case 4: return CharacterVisualFactory.MakeMat(new Color(0.18f, 0.32f, 0.20f), 0.32f, 0.15f, accent * 0.15f);
                case 5: return CharacterVisualFactory.MakeMat(new Color(0.14f, 0.26f, 0.48f), 0.48f, 0.20f, accent * 0.32f);
                case 6: return CharacterVisualFactory.MakeMat(new Color(0.18f, 0.08f, 0.26f), 0.50f, 0.30f, accent * 0.35f);
                case 7: return CharacterVisualFactory.MakeMat(new Color(0.76f, 0.54f, 0.22f), 0.36f, 0.12f, accent * 0.22f);
                case 8: return CharacterVisualFactory.MakeMat(new Color(0.26f, 0.38f, 0.22f), 0.30f, 0.08f, accent * 0.24f);
                case 9: return CharacterVisualFactory.MakeMat(new Color(0.92f, 0.88f, 0.78f), 0.65f, 0.45f, accent * 0.26f);
                default: return PbrMaterialLibrary.WroughtIron();
            }
        }

        private static void AttachClassPauldrons(int classId, Transform lArm, Transform rArm, Material armorMat, Material glowMat, Material goldMat)
        {
            if (lArm == null || rArm == null) return;

            if (classId == 0 || classId == 1 || classId == 2) // Heavy Plate / Horned Pauldrons
            {
                Vector3 sz = classId == 1 ? new Vector3(0.24f, 0.20f, 0.26f) : new Vector3(0.20f, 0.16f, 0.22f);
                var pl = CharacterVisualFactory.Prim(PrimitiveType.Sphere, "PauldronL", lArm, new Vector3(-0.04f, 0.04f, 0f), sz, armorMat);
                var pr = CharacterVisualFactory.Prim(PrimitiveType.Sphere, "PauldronR", rArm, new Vector3(0.04f, 0.04f, 0f), sz, armorMat);
                if (classId == 2) // Berserker shoulder spikes!
                {
                    CharacterVisualFactory.Prim(PrimitiveType.Cube, "SpikeL", pl.transform, new Vector3(-0.35f, 0.45f, 0f), new Vector3(0.18f, 0.75f, 0.18f), glowMat, Quaternion.Euler(0f, 0f, 28f));
                    CharacterVisualFactory.Prim(PrimitiveType.Cube, "SpikeR", pr.transform, new Vector3(0.35f, 0.45f, 0f), new Vector3(0.18f, 0.75f, 0.18f), glowMat, Quaternion.Euler(0f, 0f, -28f));
                }
            }
            else if (classId == 6 || classId == 9) // Warlock / Cleric high arcane/seraph mantle fins
            {
                Material finMat = classId == 9 ? goldMat : glowMat;
                CharacterVisualFactory.Prim(PrimitiveType.Cube, "MantleFinL", lArm, new Vector3(-0.06f, 0.10f, -0.02f), new Vector3(0.05f, 0.24f, 0.16f), finMat, Quaternion.Euler(-12f, 0f, 26f));
                CharacterVisualFactory.Prim(PrimitiveType.Cube, "MantleFinR", rArm, new Vector3(0.06f, 0.10f, -0.02f), new Vector3(0.05f, 0.24f, 0.16f), finMat, Quaternion.Euler(-12f, 0f, -26f));
            }
        }
    }

    /// <summary>
    /// Animates floating class signature relics (Mage Astrolabe Ring, Warlock Orbiting Grimoire, Monk Spirit Beads).
    /// </summary>
    public class ClassRelicFloatAnimator : MonoBehaviour
    {
        public bool OrbitMode;
        public bool SpinZ;
        private Vector3 _baseLocalPos;
        private float _phase;

        private void Start()
        {
            _baseLocalPos = transform.localPosition;
            _phase = Random.Range(0f, 6.28f);
        }

        private void Update()
        {
            float t = Time.time + _phase;
            if (OrbitMode)
            {
                transform.localPosition = _baseLocalPos + new Vector3(Mathf.Cos(t * 1.4f) * 0.08f, Mathf.Sin(t * 2.4f) * 0.09f, Mathf.Sin(t * 1.4f) * 0.08f);
                transform.localRotation = Quaternion.Euler(14f + Mathf.Sin(t * 2f) * 6f, -25f + Mathf.Cos(t * 1.3f) * 14f, 0f);
            }
            else if (SpinZ)
            {
                transform.localPosition = _baseLocalPos + new Vector3(0f, Mathf.Sin(t * 2.0f) * 0.05f, 0f);
                transform.Rotate(0f, 0f, 26f * Time.deltaTime, Space.Self);
            }
            else
            {
                transform.localPosition = _baseLocalPos + new Vector3(0f, Mathf.Sin(t * 2.2f) * 0.055f, 0f);
            }
        }
    }
}


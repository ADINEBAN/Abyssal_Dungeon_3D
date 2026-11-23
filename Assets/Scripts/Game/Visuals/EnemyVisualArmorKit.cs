using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Phase 5, 7 & 8 (Section 2.4.2, 2.4.3, 2.4.5):
    /// Dresses the 10 Enemy types, Elite variants, Final Boss Abyssal Knight, and 3D Equipment drops
    /// using the authored polygonal .obj meshes in Assets/Art/{Enemies,Bosses,Armor}/
    /// and drives 5-state procedural animations (Idle, Walk/Run, Attack, Hit Reaction, Death).
    /// </summary>
    public static class EnemyVisualArmorKit
    {
        public static string GetEnemyMeshPieceName(string enemyId)
        {
            switch (enemyId)
            {
                case "Goblin": return "Enemy_Goblin_HoodDagger";
                case "GoblinArcher": return "Enemy_GoblinArcher_QuiverBow";
                case "Skeleton": return "Enemy_Skeleton_RibcageHelm";
                case "SkeletonWarrior": return "Enemy_SkeletonWarrior_CrestShield";
                case "SkeletonMage": return "Enemy_SkeletonMage_NecroCrown";
                case "Zombie": return "Enemy_Zombie_PlagueShackles";
                case "DarkKnight": return "Enemy_DarkKnight_DreadArmor";
                case "Demon": return "Enemy_Demon_AbyssWingsHorns";
                case "CrystalGolem": return "Enemy_CrystalGolem_Monolith";
                case "ForestMonster": return "Enemy_ForestMonster_BlightTreant";
                case "AbyssalKnight": return "Boss_AbyssalKnight_CrownHelm";
                default: return "Enemy_Skeleton_RibcageHelm";
            }
        }

        public static string GetEquipmentMeshPieceName(ItemSlot slot)
        {
            switch (slot)
            {
                case ItemSlot.Weapon: return "Weap_Knight_RunicLongsword";
                case ItemSlot.Armor: return "Equip_Armor_Cuirass";
                case ItemSlot.Helmet: return "Equip_Helmet_Gothic";
                case ItemSlot.Ring: return "Equip_Ring_Signet";
                case ItemSlot.Amulet: return "Equip_Amulet_Talisman";
                case ItemSlot.Relic: return "AbyssalCrystalCluster";
                default: return "Equip_Armor_Cuirass";
            }
        }

        public static void DressEnemyRegalia(EnemyDef def, bool isElite, bool isBoss, GameObject visualRoot,
            Animator animator, Transform rHand, Transform lHand, Transform head)
        {
            if (def == null || visualRoot == null) return;

            Transform exportedRig = visualRoot.transform.Find("Rig");
            Transform spine = FindBoneOrFallback(animator, HumanBodyBones.Chest, visualRoot.transform, "Torso", "Trunk", "Body");
            Transform headAnchor = head != null ? head : FindBoneOrFallback(animator, HumanBodyBones.Head, visualRoot.transform, "Head");

            if (exportedRig != null)
            {
                // Authentic ExportedProject monster & boss prefabs already contain full 3D armor, horns & weapons.
                if (isBoss || def.archetype == Archetype.Boss || def.id == "AbyssalKnight")
                {
                    Transform rh = rHand != null ? rHand : visualRoot.transform;
                    WeaponBladeTrail3D.Attach(rh, new Vector3(0f, 1.15f, 0.25f), 0.65f, new Color(0.55f, 0.22f, 1f));
                }
            }
            else if (isBoss || def.archetype == Archetype.Boss || def.id == "AbyssalKnight")
            {
                DressAbyssalKnightBoss(def, visualRoot, spine, headAnchor, rHand, lHand);
            }
            else
            {
                string piece = GetEnemyMeshPieceName(def.id);
                Material armorMat = PbrMaterialLibrary.GothicStone(Color.Lerp(def.tint, new Color(0.24f, 0.24f, 0.30f), 0.38f));
                if (def.id == "Demon" || def.id == "DarkKnight" || def.id == "SkeletonMage")
                    armorMat = PbrMaterialLibrary.AbyssalCrystal(def.tint * 0.72f, def.tint, isElite ? 2.4f : 1.5f);

                bool attachToHead = def.id == "Goblin" || def.id == "SkeletonWarrior" || def.id == "SkeletonMage";
                Transform targetAnchor = attachToHead ? headAnchor : spine;
                Vector3 localOffset = attachToHead ? new Vector3(0f, 0.04f, 0f) : new Vector3(0f, 0.15f, 0f);
                ObjMeshLoader.SpawnPiece(piece, targetAnchor, localOffset, Quaternion.identity, Vector3.one, armorMat, addBoxCollider: false);
            }

            // Elite 3D Floating Crown Banner + Affix Aura
            if (isElite && !isBoss)
            {
                Material eliteCrownMat = PbrMaterialLibrary.AbyssalCrystal(
                    new Color(1f, 0.68f, 0.16f), new Color(1f, 0.85f, 0.32f), 2.8f);
                Vector3 crownOffset = exportedRig != null ? new Vector3(0f, 0.28f, 0f) : new Vector3(0f, 0.18f, 0f);
                float crownScale = exportedRig != null ? 0.65f : 0.88f;
                var crown = ObjMeshLoader.SpawnPiece("Enemy_Elite_AffixCrownBanner", headAnchor,
                    crownOffset, Quaternion.identity, Vector3.one * crownScale, eliteCrownMat, addBoxCollider: false);
                if (crown != null) crown.AddComponent<SlowSpin3D>();
            }

            // Attach procedural 5-state animation polish (uses skeletal Animator when available, procedural fallback otherwise)
            var procAnim = visualRoot.GetComponent<EnemyProceduralAnimator>();
            if (procAnim == null) procAnim = visualRoot.AddComponent<EnemyProceduralAnimator>();
            Transform animTarget = exportedRig != null ? exportedRig : visualRoot.transform;
            procAnim.Initialize(animTarget, animator, animator == null);
        }

        private static void DressAbyssalKnightBoss(EnemyDef def, GameObject visualRoot, Transform spine, Transform head, Transform rHand, Transform lHand)
        {
            Color armorCol = new Color(0.14f, 0.17f, 0.28f);
            Color runeGlow = new Color(0.28f, 0.78f, 1.0f); // Phase 1 Abyssal Cyan-Blue (matching reference image 2)

            Material plateMat = CharacterVisualFactory.MakeMat(armorCol, 0.78f, 0.88f, runeGlow * 0.55f);
            Material bladeMat = PbrMaterialLibrary.AbyssalCrystal(new Color(0.20f, 0.25f, 0.42f), runeGlow, 2.6f);

            // 1. Towering Spire Crown Helm
            ObjMeshLoader.SpawnPiece("Boss_AbyssalKnight_CrownHelm", head,
                new Vector3(0f, 0.02f, 0f), Quaternion.identity, Vector3.one * 1.05f, plateMat, addBoxCollider: false);

            // 2. Layered Dread Pauldrons
            ObjMeshLoader.SpawnPiece("Boss_AbyssalKnight_DreadPauldrons", spine,
                new Vector3(0f, 0.22f, 0f), Quaternion.identity, Vector3.one * 1.08f, plateMat, addBoxCollider: false);

            // 3. Dorsal Spines & Armored Tasset Cape
            ObjMeshLoader.SpawnPiece("Boss_AbyssalKnight_BackSpinesCape", spine,
                new Vector3(0f, 0.10f, 0f), Quaternion.identity, Vector3.one * 1.05f, plateMat, addBoxCollider: false);

            // 4. Colossal Runic Abyssal Greatsword on Right Hand
            Transform rh = rHand != null ? rHand : visualRoot.transform;
            var gs = ObjMeshLoader.SpawnPiece("Boss_AbyssalKnight_Greatsword", rh,
                new Vector3(0f, 0.08f, 0.04f), Quaternion.identity, Vector3.one * 1.12f, bladeMat, addBoxCollider: false);
            if (gs != null)
                WeaponBladeTrail3D.Attach(gs.transform, new Vector3(0f, 1.55f, 0f), 0.65f, new Color(0.55f, 0.22f, 1f));

            // 5. Gothic Bulwark Shield on Left Hand
            Transform lh = lHand != null ? lHand : visualRoot.transform;
            ObjMeshLoader.SpawnPiece("Boss_AbyssalKnight_BulwarkShield", lh,
                new Vector3(-0.08f, 0.04f, 0.06f), Quaternion.identity, Vector3.one * 1.05f, plateMat, addBoxCollider: false);
        }

        /// <summary>
        /// Phase 8 (Section 2.4.5): Cleans up any legacy ground aura/ring frames under the Hero
        /// so only the natural 3D human shadow is visible on the floor.
        /// </summary>
        public static void RefreshHeroLegendaryEquipmentVisuals(ActorUnit hero)
        {
            if (hero == null || hero.MainVisual == null) return;

            Transform oldAura = hero.MainVisual.transform.Find("LegendaryMythicGearAura");
            if (oldAura != null) Object.Destroy(oldAura.gameObject);
        }

        private static Transform FindBoneOrFallback(Animator anim, HumanBodyBones bone, Transform root, params string[] names)
        {
            if (anim != null && anim.isHuman)
            {
                Transform b = anim.GetBoneTransform(bone);
                if (b != null) return b;
            }
            foreach (string n in names)
            {
                Transform f = root.Find(n);
                if (f != null) return f;
            }
            return root;
        }
    }
}

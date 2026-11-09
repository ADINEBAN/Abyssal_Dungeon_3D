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

    public class SlowSpin3D : MonoBehaviour
    {
        public float SpeedY = 55f;
        private void Update() => transform.Rotate(0f, SpeedY * Time.deltaTime, 0f, Space.Self);
    }

    /// <summary>
    /// Ensures every Enemy 3D model (both Humanoid FBX and custom Golem/Treant rigs)
    /// exhibits distinct Idle, Walk/Run, Attack, Hit Reaction, and Death animations (Section 2.4.2).
    /// </summary>
    public class EnemyProceduralAnimator : MonoBehaviour
    {
        private Transform _root;
        private Animator _animator;
        private bool _customConstruct;
        private Vector3 _baseLocalPos;
        private Quaternion _baseLocalRot = Quaternion.identity;
        private Vector3 _lastPos;
        private float _phase;
        private float _attackPulse;
        private float _hitPulse;
        private bool _dead;
        private float _deathProgress;

        public void Initialize(Transform root, Animator anim, bool customConstruct)
        {
            _root = root;
            _animator = anim;
            _customConstruct = customConstruct;
            _baseLocalPos = root != null ? root.localPosition : Vector3.zero;
            _baseLocalRot = root != null ? root.localRotation : Quaternion.identity;
            _lastPos = root != null ? root.position : Vector3.zero;
            _phase = Random.Range(0f, 6.28f);
        }

        public void TriggerAttackPulse() => _attackPulse = 1f;
        public void TriggerHitReaction() => _hitPulse = 1f;
        public void TriggerDeathCollapse() => _dead = true;

        private void LateUpdate()
        {
            if (_root == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0.0001f) return;

            if (_dead)
            {
                _deathProgress = Mathf.MoveTowards(_deathProgress, 1f, dt * 2.4f);
                if (_customConstruct || _animator == null)
                {
                    _root.localRotation = _baseLocalRot * Quaternion.Euler(-82f * _deathProgress, 0f, 0f);
                    _root.localPosition = _baseLocalPos + new Vector3(0f, -0.35f * _deathProgress, 0f);
                }
                return;
            }

            Vector3 delta = _root.position - _lastPos;
            delta.y = 0f;
            float planarSpeed = delta.magnitude / dt;
            _lastPos = _root.position;

            _phase += dt * (planarSpeed > 0.25f ? 9.5f : 2.4f);
            _attackPulse = Mathf.MoveTowards(_attackPulse, 0f, dt * 3.8f);
            _hitPulse = Mathf.MoveTowards(_hitPulse, 0f, dt * 5.2f);

            if (_customConstruct || _animator == null)
            {
                float bob = planarSpeed > 0.25f ? Mathf.Abs(Mathf.Sin(_phase)) * 0.09f : Mathf.Sin(_phase) * 0.03f;
                float pitch = _attackPulse * 24f - _hitPulse * 18f;
                float roll = planarSpeed > 0.25f ? Mathf.Sin(_phase) * 5.5f : 0f;
                _root.localPosition = _baseLocalPos + new Vector3(0f, bob, 0f);
                _root.localRotation = _baseLocalRot * Quaternion.Euler(pitch, 0f, roll);
            }
            else if (_hitPulse > 0.01f)
            {
                // Subtle physical recoil tilt on hit reaction while preserving base rig transform
                _root.localRotation = _baseLocalRot * Quaternion.Euler(-_hitPulse * 10f, 0f, 0f);
            }
            else
            {
                _root.localRotation = _baseLocalRot;
            }
        }
    }
}

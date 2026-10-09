#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using AbyssalDungeon.Classes;
using AbyssalDungeon.Enemy;
using AbyssalDungeon.Loot;

namespace AbyssalDungeon.Editor
{
    /// <summary>
    /// Auto-generates all prototype assets for Abyssal Dungeon.
    /// Menu: Tools / Abyssal Dungeon / Generate All
    /// </summary>
    public class AbyssalDungeonGenerator
    {
        private static int _totalSteps = 50;
        private static int _currentStep = 0;

        [MenuItem("Tools/Abyssal Dungeon/Generate All")]
        public static void GenerateAll()
        {
            _currentStep = 0;
            try
            {
                CreateFolders();
                CreateMaterials();
                CreatePlayerPrefab();
                CreateEnemyPrefabs();
                CreateBossPrefab();
                CreateDungeonProps();
                CreateClassDataAssets();
                CreateEnemyDataAssets();
                CreateItemDataAssets();
                CreateLootTableAssets();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("<color=green><b>[AbyssalDungeonGenerator] Prototype Generation Complete! All assets created.</b></color>");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Tools/Abyssal Dungeon/Generate Materials Only")]
        public static void GenerateMaterialsOnly()
        {
            _currentStep = 0;
            _totalSteps = 5;
            try
            {
                EnsureFolder("Assets/Art");
                EnsureFolder("Assets/Art/Materials");
                CreateMaterials();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("<color=green>[AbyssalDungeonGenerator] Materials generated.</color>");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        [MenuItem("Tools/Abyssal Dungeon/Generate Prefabs Only")]
        public static void GeneratePrefabsOnly()
        {
            _currentStep = 0;
            _totalSteps = 10;
            try
            {
                EnsureFolder("Assets/Prefabs");
                EnsureFolder("Assets/Prefabs/Player");
                EnsureFolder("Assets/Prefabs/Enemy");
                EnsureFolder("Assets/Prefabs/Boss");
                EnsureFolder("Assets/Prefabs/Dungeon");
                CreatePlayerPrefab();
                CreateEnemyPrefabs();
                CreateBossPrefab();
                CreateDungeonProps();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("<color=green>[AbyssalDungeonGenerator] Prefabs generated.</color>");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        [MenuItem("Tools/Abyssal Dungeon/Generate ScriptableObjects Only")]
        public static void GenerateSOOnly()
        {
            _currentStep = 0;
            _totalSteps = 10;
            try
            {
                EnsureFolder("Assets/ScriptableObjects");
                EnsureFolder("Assets/ScriptableObjects/Classes");
                EnsureFolder("Assets/ScriptableObjects/Enemies");
                EnsureFolder("Assets/ScriptableObjects/Items");
                EnsureFolder("Assets/ScriptableObjects/LootTables");
                CreateClassDataAssets();
                CreateEnemyDataAssets();
                CreateItemDataAssets();
                CreateLootTableAssets();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("<color=green>[AbyssalDungeonGenerator] ScriptableObjects generated.</color>");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        // =====================================================
        // UTILITY
        // =====================================================

        private static void UpdateProgress(string info)
        {
            _currentStep++;
            float progress = (float)_currentStep / _totalSteps;
            EditorUtility.DisplayProgressBar("Abyssal Dungeon Generator", info, progress);
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string folder = Path.GetFileName(path);
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    EnsureFolder(parent); // Recursive
                }
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static void AssignMaterial(GameObject obj, string matName)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Art/Materials/{matName}.mat");
            if (mat != null)
            {
                Renderer r = obj.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = mat;
            }
        }

        // =====================================================
        // FOLDERS
        // =====================================================

        private static void CreateFolders()
        {
            UpdateProgress("Creating Folders...");
            string[] folders = new string[]
            {
                "Assets/Art", "Assets/Art/Materials", "Assets/Art/Models",
                "Assets/Art/VFX", "Assets/Art/UI", "Assets/Art/Environment",
                "Assets/Audio", "Assets/Audio/Music", "Assets/Audio/SFX",
                "Assets/Prefabs", "Assets/Prefabs/Player", "Assets/Prefabs/Enemy",
                "Assets/Prefabs/Boss", "Assets/Prefabs/Dungeon", "Assets/Prefabs/UI", "Assets/Prefabs/VFX",
                "Assets/ScriptableObjects", "Assets/ScriptableObjects/Classes",
                "Assets/ScriptableObjects/Skills", "Assets/ScriptableObjects/Enemies",
                "Assets/ScriptableObjects/Items", "Assets/ScriptableObjects/LootTables"
            };
            foreach (string f in folders) EnsureFolder(f);
        }

        // =====================================================
        // MATERIALS (URP Lit)
        // =====================================================

        private static void CreateMaterials()
        {
            UpdateProgress("Creating Materials...");
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                Debug.LogWarning("[AbyssalDungeonGenerator] URP Lit shader not found. Falling back to Standard.");
                urpLit = Shader.Find("Standard");
            }

            // Environment
            CreateMaterial("M_DarkStone", urpLit, new Color(0.2f, 0.2f, 0.2f), 0.8f, 0f);
            CreateMaterial("M_Metal", urpLit, new Color(0.5f, 0.5f, 0.55f), 0.3f, 1f);
            CreateMaterial("M_Blood", urpLit, new Color(0.6f, 0f, 0f), 0.2f, 0f);
            CreateMaterial("M_MagicCrystal", urpLit, new Color(0.6f, 0.2f, 0.8f, 0.7f), 0.1f, 0f, true, new Color(0.6f, 0.2f, 0.8f) * 2f);
            CreateMaterial("M_Fire", urpLit, new Color(1f, 0.4f, 0f), 0.5f, 0f, true, new Color(1f, 0.4f, 0f) * 3f);
            CreateMaterial("M_AbyssEnergy", urpLit, new Color(0.3f, 0f, 0.4f), 0.5f, 0f, true, new Color(0.3f, 0f, 0.4f) * 2f);
            CreateMaterial("M_Gold", urpLit, new Color(1f, 0.8f, 0f), 0.2f, 1f);
            CreateMaterial("M_DungeonFloor", urpLit, new Color(0.25f, 0.2f, 0.2f), 0.9f, 0f);
            CreateMaterial("M_DungeonWall", urpLit, new Color(0.3f, 0.28f, 0.3f), 0.9f, 0f);
            CreateMaterial("M_Door", urpLit, new Color(0.3f, 0.15f, 0.1f), 0.8f, 0f);
            CreateMaterial("M_Chest", urpLit, new Color(0.4f, 0.2f, 0.1f), 0.7f, 0f);
            CreateMaterial("M_Portal", urpLit, new Color(0.8f, 0.2f, 1f, 0.5f), 0.2f, 0f, true, new Color(0.8f, 0.2f, 1f) * 2f);
            CreateMaterial("M_HealingShrine", urpLit, new Color(0.2f, 0.8f, 0.3f), 0.4f, 0f, true, new Color(0.2f, 0.8f, 0.3f) * 2f);
            CreateMaterial("M_Trap", urpLit, new Color(0.8f, 0.2f, 0.2f), 0.3f, 0.9f);

            // Player
            CreateMaterial("M_PlayerBody", urpLit, new Color(0.3f, 0.4f, 0.5f), 0.5f, 0.2f);

            // Enemies
            CreateMaterial("M_EnemyGoblin", urpLit, new Color(0.1f, 0.6f, 0.2f), 0.6f, 0f);
            CreateMaterial("M_EnemySkeleton", urpLit, new Color(0.9f, 0.9f, 0.8f), 0.8f, 0f);
            CreateMaterial("M_EnemyZombie", urpLit, new Color(0.4f, 0.5f, 0.4f), 0.7f, 0f);
            CreateMaterial("M_EnemyDarkKnight", urpLit, new Color(0.1f, 0f, 0.15f), 0.3f, 0.8f);
            CreateMaterial("M_EnemyDemon", urpLit, new Color(0.5f, 0f, 0f), 0.4f, 0.1f);
            CreateMaterial("M_EnemyGolem", urpLit, new Color(0.6f, 0.6f, 0.65f), 0.2f, 0.5f);
            CreateMaterial("M_EnemyForest", urpLit, new Color(0.2f, 0.4f, 0.1f), 0.6f, 0f);

            // Boss
            CreateMaterial("M_Boss", urpLit, new Color(0.2f, 0f, 0.3f), 0.4f, 0.3f, true, new Color(0.8f, 0f, 0f) * 1.5f);
        }

        private static Material CreateMaterial(string name, Shader shader, Color color, float roughness, float metallic,
            bool emissive = false, Color emissiveColor = default)
        {
            string path = $"Assets/Art/Materials/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat; // Skip if exists

            mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1f - roughness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissiveColor);
            }

            // Transparency for alpha < 1
            if (color.a < 1f)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // =====================================================
        // PLAYER PREFAB
        // =====================================================

        private static void CreatePlayerPrefab()
        {
            UpdateProgress("Creating Player Prefab...");
            string path = "Assets/Prefabs/Player/Player_Knight.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path)) return;

            GameObject player = new GameObject("Player_Knight");

            // Body - Capsule
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(player.transform);
            body.transform.localPosition = new Vector3(0, 1f, 0);
            body.transform.localScale = new Vector3(1, 1, 1);
            AssignMaterial(body, "M_PlayerBody");

            // Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(player.transform);
            head.transform.localPosition = new Vector3(0, 2.1f, 0);
            head.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            AssignMaterial(head, "M_PlayerBody");

            // Sword
            GameObject sword = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sword.name = "Sword";
            sword.transform.SetParent(player.transform);
            sword.transform.localPosition = new Vector3(0.65f, 1f, 0.4f);
            sword.transform.localScale = new Vector3(0.08f, 0.85f, 0.08f);
            AssignMaterial(sword, "M_Metal");

            // Shield
            GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shield.name = "Shield";
            shield.transform.SetParent(player.transform);
            shield.transform.localPosition = new Vector3(-0.65f, 1f, 0.2f);
            shield.transform.localScale = new Vector3(0.05f, 0.6f, 0.5f);
            AssignMaterial(shield, "M_Metal");

            // CharacterController
            CharacterController cc = player.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 1, 0);
            cc.height = 2f;
            cc.radius = 0.5f;

            // Eye indicator (small emissive sphere)
            GameObject eyeL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeL.name = "Eye_L";
            eyeL.transform.SetParent(head.transform);
            eyeL.transform.localPosition = new Vector3(-0.15f, 0.05f, 0.4f);
            eyeL.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            AssignMaterial(eyeL, "M_MagicCrystal");

            GameObject eyeR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeR.name = "Eye_R";
            eyeR.transform.SetParent(head.transform);
            eyeR.transform.localPosition = new Vector3(0.15f, 0.05f, 0.4f);
            eyeR.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            AssignMaterial(eyeR, "M_MagicCrystal");

            PrefabUtility.SaveAsPrefabAsset(player, path);
            GameObject.DestroyImmediate(player);
        }

        // =====================================================
        // ENEMY PREFABS
        // =====================================================

        private static void CreateEnemyPrefabs()
        {
            UpdateProgress("Creating Enemy Prefabs...");

            CreateEnemyPrefab("Enemy_Goblin", "M_EnemyGoblin", new Vector3(0.7f, 0.6f, 0.7f), new Vector3(0, 0.6f, 0));
            CreateEnemyPrefab("Enemy_GoblinArcher", "M_EnemyGoblin", new Vector3(0.7f, 0.6f, 0.7f), new Vector3(0, 0.6f, 0), hasRangedWeapon: true);
            CreateEnemyPrefab("Enemy_Skeleton", "M_EnemySkeleton", new Vector3(0.5f, 0.9f, 0.5f), new Vector3(0, 0.9f, 0));
            CreateEnemyPrefab("Enemy_SkeletonWarrior", "M_EnemySkeleton", new Vector3(0.5f, 0.9f, 0.5f), new Vector3(0, 0.9f, 0), hasShield: true);
            CreateEnemyPrefab("Enemy_SkeletonMage", "M_EnemySkeleton", new Vector3(0.5f, 0.9f, 0.5f), new Vector3(0, 0.9f, 0), hasStaff: true);
            CreateEnemyPrefab("Enemy_Zombie", "M_EnemyZombie", new Vector3(1f, 1f, 1f), new Vector3(0, 1f, 0));
            CreateEnemyPrefab("Enemy_DarkKnight", "M_EnemyDarkKnight", new Vector3(1f, 1f, 1f), new Vector3(0, 1f, 0), hasLargeSword: true);
            CreateEnemyPrefab("Enemy_Demon", "M_EnemyDemon", new Vector3(1f, 1f, 1f), new Vector3(0, 1f, 0), hasHorns: true);
            CreateEnemyPrefab("Enemy_CrystalGolem", "M_EnemyGolem", new Vector3(2f, 1.25f, 2f), new Vector3(0, 1.25f, 0), isGolem: true);
            CreateEnemyPrefab("Enemy_ForestMonster", "M_EnemyForest", new Vector3(1.2f, 0.8f, 1.2f), new Vector3(0, 0.8f, 0));
        }

        private static void CreateEnemyPrefab(string name, string matName, Vector3 bodyScale, Vector3 bodyPos,
            bool hasRangedWeapon = false, bool hasShield = false, bool hasStaff = false,
            bool hasLargeSword = false, bool hasHorns = false, bool isGolem = false)
        {
            string path = $"Assets/Prefabs/Enemy/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path)) return;

            GameObject enemy = new GameObject(name);

            // Body
            GameObject body = GameObject.CreatePrimitive(isGolem ? PrimitiveType.Sphere : PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(enemy.transform);
            body.transform.localScale = bodyScale;
            body.transform.localPosition = bodyPos;
            AssignMaterial(body, matName);

            if (!isGolem)
            {
                // Head
                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "Head";
                head.transform.SetParent(enemy.transform);
                head.transform.localScale = new Vector3(bodyScale.x * 0.8f, bodyScale.x * 0.8f, bodyScale.x * 0.8f);
                head.transform.localPosition = bodyPos + new Vector3(0, bodyScale.y, 0);
                AssignMaterial(head, matName);

                // Horns for demons
                if (hasHorns)
                {
                    GameObject horn1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    horn1.name = "Horn_L";
                    horn1.transform.SetParent(head.transform);
                    horn1.transform.localScale = new Vector3(0.3f, 0.6f, 0.3f);
                    horn1.transform.localPosition = new Vector3(0.4f, 0.4f, 0);
                    AssignMaterial(horn1, "M_DarkStone");

                    GameObject horn2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    horn2.name = "Horn_R";
                    horn2.transform.SetParent(head.transform);
                    horn2.transform.localScale = new Vector3(0.3f, 0.6f, 0.3f);
                    horn2.transform.localPosition = new Vector3(-0.4f, 0.4f, 0);
                    AssignMaterial(horn2, "M_DarkStone");
                }
            }
            else
            {
                // Golem arm clusters
                GameObject armL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                armL.name = "Arm_L";
                armL.transform.SetParent(enemy.transform);
                armL.transform.localScale = Vector3.one;
                armL.transform.localPosition = bodyPos + new Vector3(1.5f, 0, 0);
                AssignMaterial(armL, matName);

                GameObject armR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                armR.name = "Arm_R";
                armR.transform.SetParent(enemy.transform);
                armR.transform.localScale = Vector3.one;
                armR.transform.localPosition = bodyPos + new Vector3(-1.5f, 0, 0);
                AssignMaterial(armR, matName);
            }

            // Weapons
            if (hasRangedWeapon)
            {
                GameObject bow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bow.name = "Bow";
                bow.transform.SetParent(enemy.transform);
                bow.transform.localScale = new Vector3(0.05f, 0.5f, 0.05f);
                bow.transform.localPosition = bodyPos + new Vector3(0.6f, 0, 0.5f);
                AssignMaterial(bow, "M_DarkStone");
            }
            else if (hasStaff)
            {
                GameObject staff = GameObject.CreatePrimitive(PrimitiveType.Cube);
                staff.name = "Staff";
                staff.transform.SetParent(enemy.transform);
                staff.transform.localScale = new Vector3(0.08f, 1f, 0.08f);
                staff.transform.localPosition = bodyPos + new Vector3(0.6f, 0, 0.4f);
                AssignMaterial(staff, "M_DarkStone");

                GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = "StaffOrb";
                orb.transform.SetParent(staff.transform);
                orb.transform.localScale = new Vector3(2.5f, 0.25f, 2.5f);
                orb.transform.localPosition = new Vector3(0, 0.6f, 0);
                AssignMaterial(orb, "M_MagicCrystal");
            }
            else if (hasLargeSword)
            {
                GameObject bigSword = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bigSword.name = "GreatSword";
                bigSword.transform.SetParent(enemy.transform);
                bigSword.transform.localScale = new Vector3(0.15f, 1.2f, 0.15f);
                bigSword.transform.localPosition = bodyPos + new Vector3(0.8f, 0, 0.4f);
                AssignMaterial(bigSword, "M_Metal");
            }
            else if (!isGolem)
            {
                GameObject weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                weapon.name = "Weapon";
                weapon.transform.SetParent(enemy.transform);
                weapon.transform.localScale = new Vector3(0.08f, 0.6f, 0.08f);
                weapon.transform.localPosition = bodyPos + new Vector3(0.6f, 0, 0.4f);
                AssignMaterial(weapon, "M_Metal");
            }

            if (hasShield)
            {
                GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shield.name = "Shield";
                shield.transform.SetParent(enemy.transform);
                shield.transform.localScale = new Vector3(0.05f, 0.6f, 0.5f);
                shield.transform.localPosition = bodyPos + new Vector3(-0.6f, 0, 0.2f);
                AssignMaterial(shield, "M_Metal");
            }

            // Collider
            CapsuleCollider col = enemy.AddComponent<CapsuleCollider>();
            col.center = bodyPos;
            col.height = bodyScale.y * 2f;
            col.radius = bodyScale.x;

            // NavMeshAgent
            UnityEngine.AI.NavMeshAgent agent = enemy.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.radius = bodyScale.x;
            agent.height = bodyScale.y * 2f;

            PrefabUtility.SaveAsPrefabAsset(enemy, path);
            GameObject.DestroyImmediate(enemy);
        }

        // =====================================================
        // BOSS PREFAB
        // =====================================================

        private static void CreateBossPrefab()
        {
            UpdateProgress("Creating Boss Prefab...");
            string path = "Assets/Prefabs/Boss/Boss_AbyssalKnight.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path)) return;

            GameObject boss = new GameObject("Boss_AbyssalKnight");

            // Large body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(boss.transform);
            body.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            body.transform.localPosition = new Vector3(0, 1.5f, 0);
            AssignMaterial(body, "M_Boss");

            // Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(boss.transform);
            head.transform.localScale = Vector3.one;
            head.transform.localPosition = new Vector3(0, 3.2f, 0);
            AssignMaterial(head, "M_Boss");

            // Horns
            GameObject hornL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hornL.name = "Horn_L";
            hornL.transform.SetParent(head.transform);
            hornL.transform.localScale = new Vector3(0.2f, 0.6f, 0.2f);
            hornL.transform.localPosition = new Vector3(-0.4f, 0.5f, 0);
            AssignMaterial(hornL, "M_DarkStone");

            GameObject hornR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hornR.name = "Horn_R";
            hornR.transform.SetParent(head.transform);
            hornR.transform.localScale = new Vector3(0.2f, 0.6f, 0.2f);
            hornR.transform.localPosition = new Vector3(0.4f, 0.5f, 0);
            AssignMaterial(hornR, "M_DarkStone");

            // Glowing eyes
            GameObject eyeL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeL.name = "Eye_L";
            eyeL.transform.SetParent(head.transform);
            eyeL.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            eyeL.transform.localPosition = new Vector3(-0.2f, 0, 0.4f);
            AssignMaterial(eyeL, "M_Fire");

            GameObject eyeR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeR.name = "Eye_R";
            eyeR.transform.SetParent(head.transform);
            eyeR.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            eyeR.transform.localPosition = new Vector3(0.2f, 0, 0.4f);
            AssignMaterial(eyeR, "M_Fire");

            // Large sword
            GameObject sword = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sword.name = "AbyssalSword";
            sword.transform.SetParent(boss.transform);
            sword.transform.localScale = new Vector3(0.15f, 1.8f, 0.15f);
            sword.transform.localPosition = new Vector3(1.2f, 1.5f, 0.8f);
            AssignMaterial(sword, "M_Metal");

            // Large shield
            GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shield.name = "AbyssalShield";
            shield.transform.SetParent(boss.transform);
            shield.transform.localScale = new Vector3(0.1f, 1f, 0.8f);
            shield.transform.localPosition = new Vector3(-1.2f, 1.5f, 0.5f);
            AssignMaterial(shield, "M_Metal");

            // Aura particle placeholder
            GameObject aura = new GameObject("Aura");
            aura.transform.SetParent(boss.transform);
            aura.transform.localPosition = new Vector3(0, 1.5f, 0);
            ParticleSystem ps = aura.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = new Color(0.4f, 0f, 0.6f, 0.5f);
            main.startSize = 0.3f;
            main.startLifetime = 1.5f;
            main.startSpeed = 0.5f;
            var emission = ps.emission;
            emission.rateOverTime = 15f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 2f;

            // Collider
            CapsuleCollider col = boss.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1.5f, 0);
            col.height = 3.5f;
            col.radius = 1.5f;

            // NavMesh
            UnityEngine.AI.NavMeshAgent agent = boss.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.height = 3.5f;
            agent.radius = 1.5f;
            agent.speed = 3f;

            PrefabUtility.SaveAsPrefabAsset(boss, path);
            GameObject.DestroyImmediate(boss);
        }

        // =====================================================
        // DUNGEON PROPS
        // =====================================================

        private static void CreateDungeonProps()
        {
            UpdateProgress("Creating Dungeon Props...");

            // --- Torch ---
            CreatePropIfNotExists("Torch", () =>
            {
                GameObject torch = new GameObject("Torch");
                GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                baseObj.name = "Base";
                baseObj.transform.SetParent(torch.transform);
                baseObj.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
                AssignMaterial(baseObj, "M_DarkStone");

                GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flame.name = "Flame";
                flame.transform.SetParent(torch.transform);
                flame.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
                flame.transform.localPosition = new Vector3(0, 0.4f, 0);
                AssignMaterial(flame, "M_Fire");

                Light l = flame.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.6f, 0.2f);
                l.range = 5f;
                l.intensity = 2f;
                return torch;
            });

            // --- Crystal ---
            CreatePropIfNotExists("Crystal", () =>
            {
                GameObject crystal = new GameObject("Crystal");
                GameObject gem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                gem.name = "Gem";
                gem.transform.SetParent(crystal.transform);
                gem.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
                AssignMaterial(gem, "M_MagicCrystal");

                Light l = gem.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(0.4f, 0.2f, 0.8f);
                l.range = 3f;
                l.intensity = 1.5f;
                return crystal;
            });

            // --- Chest ---
            CreatePropIfNotExists("Chest", () =>
            {
                GameObject chest = new GameObject("Chest");
                GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = "Box";
                box.transform.SetParent(chest.transform);
                box.transform.localScale = new Vector3(0.8f, 0.6f, 0.5f);
                box.transform.localPosition = new Vector3(0, 0.3f, 0);
                AssignMaterial(box, "M_Chest");

                GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lid.name = "Lid";
                lid.transform.SetParent(chest.transform);
                lid.transform.localScale = new Vector3(0.82f, 0.1f, 0.52f);
                lid.transform.localPosition = new Vector3(0, 0.65f, 0);
                AssignMaterial(lid, "M_Gold");

                chest.AddComponent<BoxCollider>().center = new Vector3(0, 0.35f, 0);
                return chest;
            });

            // --- Door ---
            CreatePropIfNotExists("Door", () =>
            {
                GameObject door = new GameObject("Door");
                GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                panel.name = "Panel";
                panel.transform.SetParent(door.transform);
                panel.transform.localScale = new Vector3(2f, 3f, 0.2f);
                panel.transform.localPosition = new Vector3(0, 1.5f, 0);
                AssignMaterial(panel, "M_Door");

                // Door frame
                GameObject frameL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frameL.name = "Frame_L";
                frameL.transform.SetParent(door.transform);
                frameL.transform.localScale = new Vector3(0.3f, 3.2f, 0.3f);
                frameL.transform.localPosition = new Vector3(-1.15f, 1.6f, 0);
                AssignMaterial(frameL, "M_DarkStone");

                GameObject frameR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frameR.name = "Frame_R";
                frameR.transform.SetParent(door.transform);
                frameR.transform.localScale = new Vector3(0.3f, 3.2f, 0.3f);
                frameR.transform.localPosition = new Vector3(1.15f, 1.6f, 0);
                AssignMaterial(frameR, "M_DarkStone");

                return door;
            });

            // --- Portal ---
            CreatePropIfNotExists("Portal", () =>
            {
                GameObject portal = new GameObject("Portal");
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "Ring";
                ring.transform.SetParent(portal.transform);
                ring.transform.localScale = new Vector3(2f, 0.05f, 2f);
                ring.transform.rotation = Quaternion.Euler(90, 0, 0);
                ring.transform.localPosition = new Vector3(0, 1f, 0);
                AssignMaterial(ring, "M_Portal");

                Light l = ring.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(0.8f, 0.2f, 1f);
                l.range = 5f;
                l.intensity = 2f;
                return portal;
            });

            // --- Trap_Spike ---
            CreatePropIfNotExists("Trap_Spike", () =>
            {
                GameObject trap = new GameObject("Trap_Spike");
                GameObject baseO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseO.name = "Base";
                baseO.transform.SetParent(trap.transform);
                baseO.transform.localScale = new Vector3(1f, 0.1f, 1f);
                AssignMaterial(baseO, "M_DarkStone");

                // Spikes
                for (int i = 0; i < 4; i++)
                {
                    GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    spike.name = $"Spike_{i}";
                    spike.transform.SetParent(trap.transform);
                    spike.transform.localScale = new Vector3(0.1f, 0.4f, 0.1f);
                    float x = (i % 2 == 0 ? -0.2f : 0.2f);
                    float z = (i < 2 ? -0.2f : 0.2f);
                    spike.transform.localPosition = new Vector3(x, 0.25f, z);
                    AssignMaterial(spike, "M_Trap");
                }

                trap.AddComponent<BoxCollider>().isTrigger = true;
                return trap;
            });

            // --- Trap_Fire ---
            CreatePropIfNotExists("Trap_Fire", () =>
            {
                GameObject trap = new GameObject("Trap_Fire");
                GameObject baseO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseO.name = "Base";
                baseO.transform.SetParent(trap.transform);
                baseO.transform.localScale = new Vector3(1f, 0.1f, 1f);
                AssignMaterial(baseO, "M_DarkStone");

                GameObject fireZone = new GameObject("FireZone");
                fireZone.transform.SetParent(trap.transform);
                fireZone.transform.localPosition = new Vector3(0, 0.3f, 0);
                Light fl = fireZone.AddComponent<Light>();
                fl.type = LightType.Point;
                fl.color = new Color(1f, 0.3f, 0f);
                fl.range = 3f;

                trap.AddComponent<BoxCollider>().isTrigger = true;
                return trap;
            });

            // --- HealingShrine ---
            CreatePropIfNotExists("HealingShrine", () =>
            {
                GameObject shrine = new GameObject("HealingShrine");
                GameObject ped = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ped.name = "Pedestal";
                ped.transform.SetParent(shrine.transform);
                ped.transform.localScale = new Vector3(1f, 0.5f, 1f);
                ped.transform.localPosition = new Vector3(0, 0.5f, 0);
                AssignMaterial(ped, "M_DarkStone");

                GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = "HealOrb";
                orb.transform.SetParent(shrine.transform);
                orb.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                orb.transform.localPosition = new Vector3(0, 1.5f, 0);
                AssignMaterial(orb, "M_HealingShrine");

                Light l = orb.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = Color.green;
                l.range = 4f;
                l.intensity = 1.5f;

                return shrine;
            });

            // --- Pillar ---
            CreatePropIfNotExists("Pillar", () =>
            {
                GameObject pillar = new GameObject("Pillar");
                GameObject col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                col.name = "Column";
                col.transform.SetParent(pillar.transform);
                col.transform.localScale = new Vector3(0.5f, 2f, 0.5f);
                col.transform.localPosition = new Vector3(0, 2f, 0);
                AssignMaterial(col, "M_DarkStone");

                // Base plate
                GameObject basePlate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                basePlate.name = "BasePlate";
                basePlate.transform.SetParent(pillar.transform);
                basePlate.transform.localScale = new Vector3(0.7f, 0.1f, 0.7f);
                basePlate.transform.localPosition = Vector3.zero;
                AssignMaterial(basePlate, "M_DarkStone");
                return pillar;
            });

            // --- Altar ---
            CreatePropIfNotExists("Altar", () =>
            {
                GameObject altar = new GameObject("Altar");
                GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = "Slab";
                slab.transform.SetParent(altar.transform);
                slab.transform.localScale = new Vector3(1.5f, 1f, 1f);
                slab.transform.localPosition = new Vector3(0, 0.5f, 0);
                AssignMaterial(slab, "M_DarkStone");

                // Candle holders
                for (int i = 0; i < 2; i++)
                {
                    GameObject candle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    candle.name = $"Candle_{i}";
                    candle.transform.SetParent(altar.transform);
                    candle.transform.localScale = new Vector3(0.05f, 0.2f, 0.05f);
                    candle.transform.localPosition = new Vector3(i == 0 ? -0.5f : 0.5f, 1.1f, 0);
                    AssignMaterial(candle, "M_Gold");
                }
                return altar;
            });

            // --- Statue ---
            CreatePropIfNotExists("Statue", () =>
            {
                GameObject statue = new GameObject("Statue");
                GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pedestal.name = "Pedestal";
                pedestal.transform.SetParent(statue.transform);
                pedestal.transform.localScale = new Vector3(1f, 0.5f, 1f);
                pedestal.transform.localPosition = new Vector3(0, 0.25f, 0);
                AssignMaterial(pedestal, "M_DarkStone");

                GameObject figure = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                figure.name = "Figure";
                figure.transform.SetParent(statue.transform);
                figure.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f);
                figure.transform.localPosition = new Vector3(0, 1.3f, 0);
                AssignMaterial(figure, "M_DarkStone");
                return statue;
            });
        }

        private static void CreatePropIfNotExists(string propName, System.Func<GameObject> creator)
        {
            string path = $"Assets/Prefabs/Dungeon/{propName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path)) return;

            GameObject go = creator();
            PrefabUtility.SaveAsPrefabAsset(go, path);
            GameObject.DestroyImmediate(go);
        }

        // =====================================================
        // SCRIPTABLEOBJECTS - CLASS DATA
        // =====================================================

        private static void CreateClassDataAssets()
        {
            UpdateProgress("Creating Class ScriptableObjects...");

            CreateClassSO("Knight", "A stalwart defender wielding sword and shield.", ClassRole.Tank, 1000, 200, 80, 60, 1.0f, 0.1f, 5f, WeaponType.Sword, new Color(0.7f, 0.7f, 0.7f));
            CreateClassSO("Guardian", "An unyielding protector who shields the party.", ClassRole.Tank, 1200, 150, 50, 80, 0.8f, 0.05f, 4.5f, WeaponType.Shield, new Color(1f, 0.85f, 0f));
            CreateClassSO("Berserker", "A rage-fueled warrior dealing devastating melee damage.", ClassRole.MeleeDPS, 800, 100, 120, 30, 1.3f, 0.2f, 5.5f, WeaponType.GreatSword, Color.red);
            CreateClassSO("Assassin", "A deadly shadow striker with high burst damage.", ClassRole.MeleeDPS, 600, 150, 100, 20, 1.8f, 0.35f, 7f, WeaponType.DualBlades, new Color(0.5f, 0f, 0.5f));
            CreateClassSO("Ranger", "A sharpshooter who excels at ranged combat.", ClassRole.RangedDPS, 650, 200, 90, 25, 1.5f, 0.25f, 6f, WeaponType.Bow, Color.green);
            CreateClassSO("Mage", "A master of arcane arts dealing AoE devastation.", ClassRole.RangedDPS, 500, 500, 110, 15, 1.0f, 0.15f, 5f, WeaponType.Staff, Color.blue);
            CreateClassSO("Warlock", "A dark caster who curses and summons demons.", ClassRole.RangedDPS, 550, 450, 95, 20, 1.0f, 0.1f, 5f, WeaponType.Wand, new Color(0f, 0.5f, 0.2f));
            CreateClassSO("Monk", "A swift martial artist combining offense and support.", ClassRole.Hybrid, 700, 250, 85, 35, 2.0f, 0.2f, 6.5f, WeaponType.Fist, new Color(1f, 0.5f, 0f));
            CreateClassSO("Druid", "A nature hybrid who can shapeshift and heal.", ClassRole.Healer, 750, 400, 70, 40, 1.0f, 0.1f, 5.5f, WeaponType.Staff, new Color(0.4f, 0.3f, 0.1f));
            CreateClassSO("Cleric", "A devoted healer who shields and revives allies.", ClassRole.Healer, 800, 500, 40, 50, 0.8f, 0.05f, 4.5f, WeaponType.Mace, new Color(1f, 0.95f, 0.8f));
        }

        private static void CreateClassSO(string name, string desc, ClassRole role,
            float hp, float mana, float atk, float def, float atkSpd, float crit, float moveSpd,
            WeaponType weapon, Color color)
        {
            string path = $"Assets/ScriptableObjects/Classes/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<ClassData>(path)) return;

            ClassData data = ScriptableObject.CreateInstance<ClassData>();
            data.className = name;
            data.description = desc;
            data.role = role;
            data.baseHP = hp;
            data.baseMana = mana;
            data.attack = atk;
            data.defense = def;
            data.attackSpeed = atkSpd;
            data.criticalChance = crit;
            data.movementSpeed = moveSpd;
            data.weaponType = weapon;
            data.classColor = color;

            AssetDatabase.CreateAsset(data, path);
        }

        // =====================================================
        // SCRIPTABLEOBJECTS - ENEMY DATA
        // =====================================================

        private static void CreateEnemyDataAssets()
        {
            UpdateProgress("Creating Enemy ScriptableObjects...");

            CreateEnemySO("Goblin", "A small but aggressive green creature.", EnemyType.Melee, 100, 15, 5, 4f, 8f, 2f, 1.5f, 20, 10, new Color(0.1f, 0.6f, 0.2f));
            CreateEnemySO("GoblinArcher", "A goblin that attacks from range.", EnemyType.Ranged, 80, 20, 3, 3.5f, 12f, 10f, 2f, 25, 12, new Color(0.15f, 0.5f, 0.2f));
            CreateEnemySO("Skeleton", "Undead bones risen to fight.", EnemyType.Melee, 120, 18, 8, 3f, 8f, 2f, 1.8f, 25, 15, new Color(0.9f, 0.9f, 0.8f));
            CreateEnemySO("SkeletonWarrior", "A heavily armored skeleton.", EnemyType.Tank, 200, 25, 15, 2.5f, 8f, 2f, 2f, 40, 25, new Color(0.85f, 0.85f, 0.75f));
            CreateEnemySO("SkeletonMage", "A skeleton wielding dark magic.", EnemyType.Mage, 90, 30, 5, 3f, 12f, 10f, 2.5f, 35, 20, new Color(0.8f, 0.8f, 0.9f));
            CreateEnemySO("Zombie", "A slow but durable undead.", EnemyType.Melee, 150, 20, 10, 2f, 6f, 2f, 2.5f, 20, 8, new Color(0.4f, 0.5f, 0.4f));
            CreateEnemySO("DarkKnight", "An elite warrior of the abyss.", EnemyType.Elite, 300, 35, 25, 3.5f, 10f, 2.5f, 2f, 60, 40, new Color(0.1f, 0f, 0.15f));
            CreateEnemySO("Demon", "A fiend from the depths.", EnemyType.Elite, 250, 40, 15, 4f, 12f, 3f, 1.8f, 50, 35, new Color(0.5f, 0f, 0f));
            CreateEnemySO("CrystalGolem", "A massive construct of crystal.", EnemyType.Tank, 500, 45, 40, 2f, 8f, 3f, 3f, 80, 50, new Color(0.6f, 0.6f, 0.65f));
            CreateEnemySO("ForestMonster", "A twisted creature of the dark forest.", EnemyType.Melee, 180, 22, 12, 3.5f, 10f, 2f, 1.5f, 30, 18, new Color(0.2f, 0.4f, 0.1f));
        }

        private static void CreateEnemySO(string name, string desc, EnemyType type,
            float hp, float atk, float def, float moveSpd, float detRange, float atkRange,
            float atkCD, int exp, int gold, Color color)
        {
            string path = $"Assets/ScriptableObjects/Enemies/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<EnemyData>(path)) return;

            EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
            data.enemyName = name;
            data.description = desc;
            data.enemyType = type;
            data.maxHP = hp;
            data.attack = atk;
            data.defense = def;
            data.movementSpeed = moveSpd;
            data.detectionRange = detRange;
            data.attackRange = atkRange;
            data.attackCooldown = atkCD;
            data.expReward = exp;
            data.goldReward = gold;
            data.enemyColor = color;

            AssetDatabase.CreateAsset(data, path);
        }

        // =====================================================
        // SCRIPTABLEOBJECTS - ITEM DATA
        // =====================================================

        private static void CreateItemDataAssets()
        {
            UpdateProgress("Creating Item ScriptableObjects...");

            CreateItemSO("HealthPotion", "Restores 200 HP.", ItemType.Consumable, ItemRarity.Common, 0, 0, 200, 0, 0, 0, 0, 0, 10);
            CreateItemSO("ManaPotion", "Restores 150 Mana.", ItemType.Consumable, ItemRarity.Common, 0, 0, 0, 150, 0, 0, 0, 0, 10);
            CreateItemSO("IronSword", "A sturdy iron sword.", ItemType.Weapon, ItemRarity.Common, 15, 0, 0, 0, 0, 0, 0, 0, 25);
            CreateItemSO("SteelArmor", "Reliable steel armor.", ItemType.Armor, ItemRarity.Uncommon, 0, 12, 50, 0, 0, 0, 0, 0, 40);
            CreateItemSO("AbyssalBlade", "A blade forged in the abyss.", ItemType.Weapon, ItemRarity.Rare, 35, 0, 0, 0, 0.1f, 0.05f, 0, 0, 120);
            CreateItemSO("ShadowHelm", "A helm that whispers dark secrets.", ItemType.Helmet, ItemRarity.Rare, 0, 18, 30, 0, 0, 0, 0, 0.05f, 100);
            CreateItemSO("RingOfPower", "Grants immense strength.", ItemType.Ring, ItemRarity.Epic, 25, 10, 0, 50, 0, 0.1f, 0.05f, 0.1f, 250);
            CreateItemSO("AmuletOfTheAbyss", "Pulsates with abyssal energy.", ItemType.Amulet, ItemRarity.Epic, 20, 15, 100, 100, 0, 0.05f, 0, 0.15f, 300);
            CreateItemSO("DemonSlayer", "Legendary blade of the demon hunters.", ItemType.Weapon, ItemRarity.Legendary, 65, 10, 0, 0, 0.2f, 0.15f, 0.1f, 0, 500);
            CreateItemSO("MythicCrown", "Crown of the Abyssal King.", ItemType.Helmet, ItemRarity.Mythic, 30, 40, 200, 200, 0.15f, 0.1f, 0.05f, 0.2f, 1000);
        }

        private static void CreateItemSO(string name, string desc, ItemType type, ItemRarity rarity,
            float atkBonus, float defBonus, float hpBonus, float manaBonus,
            float atkSpdBonus, float critBonus, float lifesteal, float cdr, int sellPrice)
        {
            string path = $"Assets/ScriptableObjects/Items/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<ItemData>(path)) return;

            ItemData data = ScriptableObject.CreateInstance<ItemData>();
            data.itemName = name;
            data.description = desc;
            data.itemType = type;
            data.rarity = rarity;
            data.attackBonus = atkBonus;
            data.defenseBonus = defBonus;
            data.hpBonus = hpBonus;
            data.manaBonus = manaBonus;
            data.attackSpeedBonus = atkSpdBonus;
            data.criticalChanceBonus = critBonus;
            data.lifestealBonus = lifesteal;
            data.cooldownReduction = cdr;
            data.sellPrice = sellPrice;

            AssetDatabase.CreateAsset(data, path);
        }

        // =====================================================
        // SCRIPTABLEOBJECTS - LOOT TABLES
        // =====================================================

        private static void CreateLootTableAssets()
        {
            UpdateProgress("Creating Loot Table ScriptableObjects...");

            // Normal enemy loot
            CreateLootTableSO("NormalEnemyLoot", 5, 15, 15, 30,
                new string[] { "HealthPotion", "IronSword" },
                new float[] { 0.3f, 0.05f });

            // Elite enemy loot
            CreateLootTableSO("EliteEnemyLoot", 20, 50, 40, 80,
                new string[] { "HealthPotion", "ManaPotion", "SteelArmor", "AbyssalBlade", "RingOfPower" },
                new float[] { 0.5f, 0.3f, 0.15f, 0.1f, 0.05f });

            // Boss loot
            CreateLootTableSO("BossLoot", 100, 300, 200, 500,
                new string[] { "AbyssalBlade", "ShadowHelm", "RingOfPower", "AmuletOfTheAbyss", "DemonSlayer", "MythicCrown" },
                new float[] { 0.3f, 0.25f, 0.2f, 0.15f, 0.1f, 0.02f });
        }

        private static void CreateLootTableSO(string name, int minGold, int maxGold, int minEXP, int maxEXP,
            string[] itemNames, float[] dropChances)
        {
            string path = $"Assets/ScriptableObjects/LootTables/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<LootTable>(path)) return;

            LootTable table = ScriptableObject.CreateInstance<LootTable>();
            table.minGuaranteedGold = minGold;
            table.maxGuaranteedGold = maxGold;
            table.minGuaranteedEXP = minEXP;
            table.maxGuaranteedEXP = maxEXP;

            List<LootEntry> entries = new List<LootEntry>();
            for (int i = 0; i < itemNames.Length && i < dropChances.Length; i++)
            {
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>($"Assets/ScriptableObjects/Items/{itemNames[i]}.asset");
                if (item != null)
                {
                    entries.Add(new LootEntry
                    {
                        item = item,
                        dropChance = dropChances[i],
                        minQuantity = 1,
                        maxQuantity = 1
                    });
                }
            }
            table.entries = entries.ToArray();

            AssetDatabase.CreateAsset(table, path);
        }
    }
}
#endif

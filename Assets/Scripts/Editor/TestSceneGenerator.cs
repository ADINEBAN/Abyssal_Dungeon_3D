#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor.AI;
using AbyssalDungeon.Core;
using AbyssalDungeon.Dungeon;
using AbyssalDungeon.UI;

namespace AbyssalDungeon.Editor
{
    public class TestSceneGenerator
    {
        [MenuItem("Tools/Abyssal Dungeon/Create Test Dungeon Scene")]
        public static void CreateTestScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = "TestDungeon";

            // 1. Create Core Managers
            GameObject core = new GameObject("--- CORE MANAGERS ---");
            GameObject gameManager = new GameObject("GameManager");
            // DO NOT parent GameManager because Singletons use DontDestroyOnLoad which requires root objects
            gameManager.AddComponent<GameManager>();
            gameManager.AddComponent<PoolManager>();

            // 2. Create UI
            GameObject canvasObj = new GameObject("--- UI CANVAS ---");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            GameObject hud = new GameObject("PlayerHUD");
            hud.transform.SetParent(canvasObj.transform);
            hud.AddComponent<PlayerHUD>();
            
            GameObject damagePopup = new GameObject("DamagePopupPool");
            damagePopup.transform.SetParent(canvasObj.transform);
            
            // 3. Environment
            GameObject env = new GameObject("--- ENVIRONMENT ---");
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(env.transform);
            floor.transform.localScale = new Vector3(10, 1, 10);
            GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.NavigationStatic);
            
            Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_DungeonFloor.mat");
            if(mat != null) floor.GetComponent<Renderer>().sharedMaterial = mat;

            // 4. Player
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player_Knight.prefab");
            if (playerPrefab != null)
            {
                GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                player.transform.position = new Vector3(0, 1, 0);
                
                // Camera
                GameObject camObj = new GameObject("ThirdPersonCamera");
                Camera cam = camObj.AddComponent<Camera>();
                cam.tag = "MainCamera";
                var tpc = camObj.AddComponent<AbyssalDungeon.Player.ThirdPersonCamera>();
                SerializedObject so = new SerializedObject(tpc);
                so.FindProperty("target").objectReferenceValue = player.transform;
                so.FindProperty("offset").vector3Value = new Vector3(0, 1.5f, 0);
                so.FindProperty("distance").floatValue = 6f;
                so.ApplyModifiedProperties();
            }

            // 5. Enemies
            GameObject goblinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/Enemy_Goblin.prefab");
            if (goblinPrefab != null)
            {
                for(int i=0; i<3; i++)
                {
                    GameObject goblin = (GameObject)PrefabUtility.InstantiatePrefab(goblinPrefab);
                    goblin.transform.position = new Vector3(Random.Range(-5f, 5f), 1, Random.Range(3f, 8f));
                }
            }

            NavMeshBuilder.BuildNavMesh();
            Debug.Log("<color=green>Test Scene Created! You can now press Play to test the character movement and combat.</color>");
        }
    }
}
#endif



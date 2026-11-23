#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using AbyssGame;

namespace AbyssalDungeon.EditorTools
{
    /// <summary>
    /// Converts runtime-generated scenes into persistent Edit Mode scenes (.unity)
    /// while persisting all procedural Meshes, Materials, Textures, and VolumeProfiles
    /// to Assets/GeneratedBakedAssets/ via AssetDatabase.CreateAsset.
    /// </summary>
    public static class SceneRuntimeToEditModeBaker
    {
        public const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        public const string TownScenePath = "Assets/Scenes/Town.unity";

        [MenuItem("Abyssal Dungeon/Bake Scenes to Edit Mode/1. Pilot: Bake MainMenu + Town Scenes")]
        public static void BakePilotMainMenuAndTown()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SceneBaker] Please exit Play Mode before baking scenes.");
                return;
            }

            BakedAssetRegistry.EnsureFolders();
            BakeTownScene();
            BakeMainMenuScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#78E888>[SceneBaker] Pilot Bake Complete: MainMenu.unity & Town.unity are now fully populated in Edit Mode!</color>");
        }

        [MenuItem("Abyssal Dungeon/Bake Scenes to Edit Mode/2. Bake MainMenu Scene Only")]
        public static void BakeMainMenuScene()
        {
            BakeSingleScene(MainMenuScenePath, "MainMenu", root =>
            {
                var view = root.AddComponent<MainMenuSceneView>();
                view.Build();
            }, bakeNavMesh: false);
        }

        [MenuItem("Abyssal Dungeon/Bake Scenes to Edit Mode/3. Bake Town Scene Only")]
        public static void BakeTownScene()
        {
            BakeSingleScene(TownScenePath, "Town", root =>
            {
                var builder = root.AddComponent<TownSceneBuilder>();
                builder.Build();
            }, bakeNavMesh: true);
        }

        private static void BakeSingleScene(string scenePath, string sceneKindName, Action<GameObject> buildAction, bool bakeNavMesh)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            CleanupExistingGeneratedRoots(scene);
            EnsureBaseSceneInfrastructure();

            var contentRoot = new GameObject("SceneContent_" + sceneKindName);
            SceneManager.MoveGameObjectToScene(contentRoot, scene);

            buildAction?.Invoke(contentRoot);

            // Clean up any temporary singleton manager objects spawned into the scene during Edit Mode Build()
            CleanupTransientSingletonObjects(scene);

            // Mark static architectural geometry as NavigationStatic / BatchingStatic / OccluderStatic
            MarkEnvironmentGeometryStatic(scene);

            // Exhaustively persist all procedural Meshes, Materials, Textures, VolumeProfiles, Fonts
            BakedAssetRegistry.PersistAllUnsavedAssetsInScene(scene);

            if (bakeNavMesh)
            {
                try
                {
                    NavMeshBuilder.BuildNavMesh();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[SceneBaker] NavMesh bake warning: " + ex.Message);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            int totalObjects = UnityEngine.Object.FindObjectsOfType<Transform>(true).Length;
            Debug.Log($"<color=#F8DC85>[SceneBaker]</color> Baked <b>{scenePath}</b> in Edit Mode ({scene.rootCount} root objects, {totalObjects} total transforms).");
        }

        private static void CleanupExistingGeneratedRoots(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            for (int i = roots.Length - 1; i >= 0; i--)
            {
                GameObject go = roots[i];
                if (go == null) continue;
                string n = go.name;

                bool keep = n == "Main Camera"
                         || n == "Directional Light"
                         || n == "EventSystem"
                         || n == "VFXPoolManager"
                         || n == "AbyssSceneBootstrap"
                         || n == "[AbyssSceneBootstrap]";

                if (!keep)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
        }

        private static void CleanupTransientSingletonObjects(Scene scene)
        {
            string[] transientNames =
            {
                "[GameSession]",
                "[GameAudio]",
                "[GraphicsManager]",
                "GraphicsManager",
                "[LanRoomBridge]",
                "[CombatFeelManager]",
                "[GameModeManager]"
            };

            var roots = scene.GetRootGameObjects();
            for (int i = roots.Length - 1; i >= 0; i--)
            {
                var go = roots[i];
                if (go == null) continue;
                for (int t = 0; t < transientNames.Length; t++)
                {
                    if (string.Equals(go.name, transientNames[t], StringComparison.OrdinalIgnoreCase))
                    {
                        UnityEngine.Object.DestroyImmediate(go);
                        break;
                    }
                }
            }
        }

        private static void EnsureBaseSceneInfrastructure()
        {
            if (Camera.main == null)
            {
                var camGo = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.GetComponent<Camera>() ?? camGo.AddComponent<Camera>();
                cam.nearClipPlane = 0.15f;
                cam.farClipPlane = 160f;
                if (camGo.GetComponent<AudioListener>() == null)
                    camGo.AddComponent<AudioListener>();
            }
            else
            {
                // Remove leftover OrbitCamera3D before baking a non-gameplay scene if needed
                var orbit = Camera.main.GetComponent<OrbitCamera3D>();
                if (orbit != null && SceneManager.GetActiveScene().name == "MainMenu")
                    UnityEngine.Object.DestroyImmediate(orbit);
            }

            if (UnityEngine.Object.FindObjectOfType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                var dl = lightGo.AddComponent<Light>();
                dl.type = LightType.Directional;
                dl.color = new Color(0.92f, 0.88f, 1f);
                dl.intensity = 1.05f;
                lightGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            }

            if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }

            if (UnityEngine.Object.FindObjectOfType<SceneBootstrap>() == null)
            {
                var bootGo = new GameObject("AbyssSceneBootstrap");
                bootGo.AddComponent<SceneBootstrap>();
            }
        }

        private static void MarkEnvironmentGeometryStatic(Scene scene)
        {
            const StaticEditorFlags envFlags =
                StaticEditorFlags.NavigationStatic |
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccluderStatic |
                StaticEditorFlags.OccludeeStatic;

            string[] staticPrefixes =
            {
                "FloorPhysicsCollider",
                "ModularFloorTiles",
                "DungeonFloor_ChannelTile",
                "GothicFloorMasonry",
                "GothicFloorGoldInlay",
                "ModularPerimeterWalls",
                "DungeonWall_Modular",
                "GothicBrickMasonry",
                "GothicReliefTrim",
                "WallCollider",
                "GothicColonnade",
                "GothicPillar",
                "GothicArchVault",
                "GothicGateFrame",
                "GuardianStatue_Gothic",
                "RunicPillarAltar",
                "WoodFloor_",
                "WoodPlank_",
                "WoodDeckIronEdge",
                "MetalFloor_",
                "IronGratePlate"
            };

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == null) continue;
                foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                {
                    if (tr == null) continue;
                    string n = tr.gameObject.name;
                    for (int i = 0; i < staticPrefixes.Length; i++)
                    {
                        if (n.StartsWith(staticPrefixes[i], StringComparison.Ordinal))
                        {
                            GameObjectUtility.SetStaticEditorFlags(tr.gameObject, envFlags);
                            break;
                        }
                    }
                }
            }
        }
    }
}
#endif

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
        public const string ClassSelectionScenePath = "Assets/Scenes/ClassSelection.unity";
        public const string LobbyScenePath = "Assets/Scenes/Lobby.unity";
        public const string TownScenePath = "Assets/Scenes/Town.unity";
        public const string DungeonScenePath = "Assets/Scenes/Dungeon.unity";
        public const string EndGameScenePath = "Assets/Scenes/EndGame.unity";
        public const string VisualShowcaseScenePath = "Assets/Scenes/VisualShowcase.unity";

        [MenuItem("Abyssal Dungeon/Bake Scenes to Edit Mode/0. Bake ALL 7 Game Scenes + Prefabs + NavMesh")]
        public static void BakeAllSevenScenes()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SceneBaker] Please exit Play Mode before baking scenes.");
                return;
            }

            BakedAssetRegistry.EnsureFolders();
            BakedAssetRegistry.ResetPrefabSessionCache();

            BakeClassSelectionScene();
            BakeLobbyScene();
            BakeTownScene();
            BakeDungeonScene();
            BakeEndGameScene();
            BakeVisualShowcaseScene();
            BakeMainMenuScene();
            EnsureEditorBuildSettingsOrder();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#78E888>[SceneBaker] All 7 Scenes + Prefabs + GeneratedBakedAssets + Build Settings successfully baked in Edit Mode!</color>");
        }

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

        [MenuItem("Abyssal Dungeon/Bake Scenes to Edit Mode/4. Bake ClassSelection Scene Only")]
        public static void BakeClassSelectionScene()
        {
            BakeSingleScene(ClassSelectionScenePath, "ClassSelection", root =>
            {
                var view = root.AddComponent<ClassSelectSceneView>();
                view.Build();
            }, bakeNavMesh: false);
        }

        [MenuItem("Abyssal Dungeon/Bake Scenes to Edit Mode/5. Bake Lobby Scene Only")]
        public static void BakeLobbyScene()
        {
            BakeSingleScene(LobbyScenePath, "Lobby", root =>
            {
                var view = root.AddComponent<LobbySceneView>();
                view.Build();
            }, bakeNavMesh: false);
        }

        [MenuItem("Abyssal Dungeon/Bake Scenes to Edit Mode/6. Bake Dungeon Scene Only (+ Prefabs & NavMesh)")]
        public static void BakeDungeonScene()
        {
            BakedAssetRegistry.ResetPrefabSessionCache();
            BakeSingleScene(DungeonScenePath, "Dungeon", root =>
            {
                var dw = root.AddComponent<DungeonWorldBuilder>();
                dw.BuildAndStartDungeon();
                var hud = root.AddComponent<DungeonHUDView>();
                hud.Build();

                // Ensure all pre-baked monsters' overhead HP bars remain visible in Edit Mode Scene View
                foreach (var u in UnityEngine.Object.FindObjectsOfType<ActorUnit>(true))
                {
                    if (u == null) continue;
                    var bar = u.transform.Find("OverheadBar");
                    if (bar != null) bar.gameObject.SetActive(true);
                }
            }, bakeNavMesh: true);
        }

        [MenuItem("Abyssal Dungeon/Bake Scenes to Edit Mode/7. Bake EndGame Scene Only")]
        public static void BakeEndGameScene()
        {
            BakeSingleScene(EndGameScenePath, "EndGame", root =>
            {
                var view = root.AddComponent<EndGameSceneView>();
                view.Build();
            }, bakeNavMesh: false);
        }

        [MenuItem("Abyssal Dungeon/Bake Scenes to Edit Mode/8. Bake VisualShowcase Scene Only")]
        public static void BakeVisualShowcaseScene()
        {
            BakeSingleScene(VisualShowcaseScenePath, "VisualShowcase", root =>
            {
                var view = root.AddComponent<VisualShowcaseSceneView>();
                view.Build();
            }, bakeNavMesh: true);
        }

        [MenuItem("Abyssal Dungeon/Bake Scenes to Edit Mode/9. Bake Remaining 5 Scenes (ClassSelection, Lobby, Dungeon, EndGame, VisualShowcase)")]
        public static void BakeRemainingFiveScenes()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SceneBaker] Please exit Play Mode before baking scenes.");
                return;
            }

            BakedAssetRegistry.EnsureFolders();
            BakedAssetRegistry.ResetPrefabSessionCache();

            BakeClassSelectionScene();
            BakeLobbyScene();
            BakeDungeonScene();
            BakeEndGameScene();
            BakeVisualShowcaseScene();
            EnsureEditorBuildSettingsOrder();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#78E888>[SceneBaker] Remaining 5 Scenes (ClassSelection, Lobby, Dungeon, EndGame, VisualShowcase) + Prefabs + NavMesh + Build Settings baked in Edit Mode!</color>");
        }

        public static void EnsureEditorBuildSettingsOrder()
        {
            string[] orderedScenePaths =
            {
                MainMenuScenePath,
                ClassSelectionScenePath,
                LobbyScenePath,
                TownScenePath,
                DungeonScenePath,
                EndGameScenePath,
                VisualShowcaseScenePath
            };

            var list = new List<EditorBuildSettingsScene>(orderedScenePaths.Length);
            for (int i = 0; i < orderedScenePaths.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(orderedScenePaths[i]) != null)
                {
                    list.Add(new EditorBuildSettingsScene(orderedScenePaths[i], true));
                }
            }
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log($"<color=#F8DC85>[SceneBaker]</color> Updated EditorBuildSettings.scenes ({list.Count} scenes, Index 0 = {orderedScenePaths[0]}).");
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
                string sn = SceneManager.GetActiveScene().name;
                var orbit = Camera.main.GetComponent<OrbitCamera3D>();
                if (orbit != null && sn != "Dungeon" && sn != "Town" && sn != "VisualShowcase")
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
                "IronGratePlate",
                "ModularCorridor",
                "CorridorFloor",
                "StoneStairs_Grand",
                "StairsWalkRamp",
                "SanctuaryDais",
                "DaisCollider",
                "BossThroneAltar",
                "AncientRuins_Rubble"
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

        [MenuItem("Abyssal Dungeon/Bake Scenes to Edit Mode/10. Audit All 7 Scenes (Missing Scripts, Prefabs, Build Settings)")]
        public static void AuditAllSevenScenes()
        {
            EnsureEditorBuildSettingsOrder();

            string[] paths =
            {
                MainMenuScenePath,
                ClassSelectionScenePath,
                LobbyScenePath,
                TownScenePath,
                DungeonScenePath,
                EndGameScenePath,
                VisualShowcaseScenePath
            };

            int totalMissingScripts = 0;
            for (int s = 0; s < paths.Length; s++)
            {
                var sc = EditorSceneManager.OpenScene(paths[s], OpenSceneMode.Single);
                int missingInScene = 0;
                int prefabRootsInScene = 0;
                int totalTransforms = 0;

                foreach (var r in sc.GetRootGameObjects())
                {
                    if (r == null) continue;
                    foreach (var tr in r.GetComponentsInChildren<Transform>(true))
                    {
                        if (tr == null) continue;
                        totalTransforms++;
                        missingInScene += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject);
                        if (PrefabUtility.IsAnyPrefabInstanceRoot(tr.gameObject))
                            prefabRootsInScene++;
                    }
                }

                totalMissingScripts += missingInScene;
                Debug.Log($"<color=#78E888>[SceneAudit]</color> <b>{sc.name}.unity</b>: Roots={sc.rootCount}, Transforms={totalTransforms}, PrefabInstances={prefabRootsInScene}, MissingScripts={missingInScene}");
            }

            // Leave Dungeon.unity open so the user can immediately inspect the 7-room + branches + Prefab Instances in Edit Mode
            EditorSceneManager.OpenScene(DungeonScenePath, OpenSceneMode.Single);
            Debug.Log($"<color=#F8DC85>[SceneAudit] Completed audit across all 7 scenes. Total Missing Scripts = {totalMissingScripts}.</color>");
        }
    }
}
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AbyssalDungeon.EditorTools
{
    /// <summary>
    /// Ensures Unity Editor Play Mode ALWAYS boots from Assets/Scenes/MainMenu.unity
    /// regardless of which scene is currently open, and keeps Build Settings scene order valid.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStartSceneSetup
    {
        public const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

        private static readonly string[] RequiredSceneOrder =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/ClassSelection.unity",
            "Assets/Scenes/Lobby.unity",
            "Assets/Scenes/Dungeon.unity",
            "Assets/Scenes/EndGame.unity",
            "Assets/Scenes/Town.unity",
            "Assets/Scenes/VisualShowcase.unity"
        };

        static PlayModeStartSceneSetup()
        {
            EditorApplication.delayCall += ApplyPlayModeStartScene;
        }

        [MenuItem("Abyssal Dungeon/Setup PlayMode Start Scene (MainMenu)")]
        public static void ApplyPlayModeStartScene()
        {
            var startScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
            if (startScene != null && EditorSceneManager.playModeStartScene != startScene)
            {
                EditorSceneManager.playModeStartScene = startScene;
            }

            EnsureBuildSettingsOrder();
        }

        public static void EnsureBuildSettingsOrder()
        {
            var existing = EditorBuildSettings.scenes;
            var byPath = new Dictionary<string, EditorBuildSettingsScene>();
            foreach (var s in existing)
            {
                if (s != null && !string.IsNullOrEmpty(s.path) && !byPath.ContainsKey(s.path))
                    byPath[s.path] = s;
            }

            var ordered = new List<EditorBuildSettingsScene>();
            var used = new HashSet<string>();

            foreach (string path in RequiredSceneOrder)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    continue;

                if (byPath.TryGetValue(path, out var sceneCfg))
                    ordered.Add(new EditorBuildSettingsScene(path, true));
                else
                    ordered.Add(new EditorBuildSettingsScene(path, true));

                used.Add(path);
            }

            foreach (var s in existing)
            {
                if (s != null && !string.IsNullOrEmpty(s.path) && !used.Contains(s.path))
                    ordered.Add(s);
            }

            bool changed = ordered.Count != existing.Length;
            if (!changed)
            {
                for (int i = 0; i < ordered.Count; i++)
                {
                    if (ordered[i].path != existing[i].path || ordered[i].enabled != existing[i].enabled)
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (changed && ordered.Count > 0)
            {
                EditorBuildSettings.scenes = ordered.ToArray();
            }
        }
    }
}
#endif

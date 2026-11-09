#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Phase 5, 7, 8 & 12 Editor Automation & Release Build Pipeline (`unity-build-pipeline` skill):
    /// Generates Enemy & Boss ScriptableObjects (`EnemyDefinitionAsset`) and provides
    /// one-click Windows x64 Player Build with `BuildReport` validation.
    /// </summary>
    [InitializeOnLoad]
    public static class AbyssalBuildPipeline
    {
        private const string EnemySoMarker = "Assets/Resources/Enemies/Enemy_AbyssalKnight.asset";

        static AbyssalBuildPipeline()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(EnemySoMarker))
                {
                    BuildEnemyAndBossScriptableObjects();
                }
            };
        }

        [MenuItem("Tools/Abyssal Dungeon/Generate Phase 5-8 Enemy & Boss Assets", priority = 25)]
        public static void BuildEnemyAndBossScriptableObjects()
        {
            string[] dirs = { "Assets/ScriptableObjects/Enemies", "Assets/Resources/Enemies" };
            foreach (var d in dirs)
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);

            foreach (var kv in GameDB.Enemies)
            {
                SaveEnemyAsset(kv.Value, false);
            }
            if (GameDB.Boss != null)
            {
                SaveEnemyAsset(GameDB.Boss, true);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=cyan>[AbyssalBuildPipeline] Generated 11 Enemy & Boss ScriptableObjects (Phase 5 & 7)!</color>");
        }

        private static void SaveEnemyAsset(EnemyDef def, bool isBoss)
        {
            if (def == null) return;
            string resPath = $"Assets/Resources/Enemies/Enemy_{def.id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<EnemyDefinitionAsset>(resPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<EnemyDefinitionAsset>();
                asset.PopulateFrom(def, isBoss ? EliteAffixKind.Vampiric : EliteAffixKind.None);
                AssetDatabase.CreateAsset(asset, resPath);
            }
            else
            {
                asset.PopulateFrom(def, isBoss ? EliteAffixKind.Vampiric : EliteAffixKind.None);
                EditorUtility.SetDirty(asset);
            }
        }

        [MenuItem("Tools/Abyssal Dungeon/Build Windows x64 Release Player", priority = 40)]
        public static void BuildWindows64Release()
        {
            AbyssGame.EditorTools.AbyssalAutoSetup.EnsureBuildSettings();
            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled && File.Exists(s.path))
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[AbyssalBuildPipeline] No enabled scenes found in EditorBuildSettings!");
                return;
            }

            string outDir = "Builds/Windows64";
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outDir, "AbyssalDungeon3D.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"<color=green>[AbyssalBuildPipeline] Build Succeeded: {opts.locationPathName} ({report.summary.totalSize / (1024 * 1024)} MB)</color>");
            }
            else
            {
                Debug.LogError($"[AbyssalBuildPipeline] Build result: {report.summary.result} ({report.summary.totalErrors} errors)");
            }
        }
    }
}
#endif

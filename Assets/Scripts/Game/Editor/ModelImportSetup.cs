#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AbyssGame.EditorTools
{
    /// <summary>
    /// Configures and validates all imported 3D Character Rigs, Weapons, Monster/Boss Prefabs,
    /// Modular Environment Assets, PBR Normal Maps, and Decoded AnimationClips extracted from ExportedProject
    /// under Assets/Art/Exported/.
    /// </summary>
    public static class ModelImportSetup
    {
        public const string ExportedRoot = "Assets/Art/Exported";
        public const string ExportedCharDir = ExportedRoot + "/Characters";
        public const string ExportedWeapDir = ExportedRoot + "/Weapons";
        public const string ExportedEnemyDir = ExportedRoot + "/Enemies";
        public const string ExportedEnvDir = ExportedRoot + "/Environment";
        public const string ExportedTexDir = ExportedRoot + "/Textures";
        public const string ExportedAnimDir = ExportedRoot + "/Animations";

        [MenuItem("Tools/Abyssal Dungeon/1. Configure Exported 3D Assets & Textures")]
        public static void ConfigureAll()
        {
            if (!Directory.Exists(ExportedRoot))
            {
                Debug.LogWarning("[AbyssGame] Assets/Art/Exported not found. Run Tools/ImportFromExportedProject.py first.");
                return;
            }

            int updatedNormals = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                if (Directory.Exists(ExportedTexDir))
                {
                    foreach (var raw in Directory.GetFiles(ExportedTexDir, "*.*", SearchOption.AllDirectories))
                    {
                        if (raw.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                        string path = raw.Replace("\\", "/");
                        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                        if (ti == null) continue;

                        string fileName = Path.GetFileNameWithoutExtension(path);
                        bool isNormal = fileName.IndexOf("_NM", StringComparison.OrdinalIgnoreCase) >= 0
                                     || fileName.IndexOf("_NS", StringComparison.OrdinalIgnoreCase) >= 0
                                     || fileName.IndexOf("Normal", StringComparison.OrdinalIgnoreCase) >= 0;

                        bool dirty = false;
                        if (isNormal && ti.textureType != TextureImporterType.NormalMap)
                        {
                            ti.textureType = TextureImporterType.NormalMap;
                            ti.sRGBTexture = false;
                            dirty = true;
                        }
                        else if (!isNormal && !ti.sRGBTexture)
                        {
                            ti.sRGBTexture = true;
                            dirty = true;
                        }
                        if (ti.anisoLevel < 4)
                        {
                            ti.anisoLevel = 4;
                            dirty = true;
                        }
                        if (dirty)
                        {
                            ti.SaveAndReimport();
                            updatedNormals++;
                        }
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh();
            Debug.Log($"[AbyssGame] Configured ExportedProject 3D assets & textures (re-verified {updatedNormals} textures).");
        }

        public static AnimationClip LoadHeroClip(string className, string stateName)
        {
            string classPath = $"{ExportedAnimDir}/Heroes/{className}/Hero_{className}_{stateName}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(classPath);
            if (clip != null) return clip;

            string commonPath = $"{ExportedAnimDir}/Heroes/Common/Hero_Common_{stateName}.anim";
            clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(commonPath);
            if (clip != null) return clip;

            // Fallback to Knight stance clip if a specialized clip is omitted
            string fallbackPath = $"{ExportedAnimDir}/Heroes/Knight/Hero_Knight_{stateName}.anim";
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(fallbackPath);
        }

        public static AnimationClip LoadEnemyClip(string enemyId, string stateName)
        {
            string path = $"{ExportedAnimDir}/Enemies/{enemyId}/{enemyId}_{stateName}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null) return clip;

            if (stateName == "Walk")
                return AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ExportedAnimDir}/Enemies/{enemyId}/{enemyId}_Run.anim");
            if (stateName == "Cast")
                return AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ExportedAnimDir}/Enemies/{enemyId}/{enemyId}_Attack.anim");
            return null;
        }

        /// <summary>
        /// Backwards-compatible clip lookup mapping legacy state names onto Knight / Common exported clips.
        /// </summary>
        public static AnimationClip LoadClip(string animName)
        {
            switch (animName)
            {
                case "Idle":                    return LoadHeroClip("Knight", "Idle");
                case "Jogging":                 return LoadHeroClip("Knight", "Run");
                case "Sword And Shield Attack": return LoadHeroClip("Knight", "Attack");
                case "Sword And Shield Casting":return LoadHeroClip("Knight", "Cast");
                case "Standing Dodge Right":    return LoadHeroClip("Common", "Dodge");
                case "Two Handed Sword Death":  return LoadHeroClip("Common", "Death");
                default:                        return LoadHeroClip("Knight", animName);
            }
        }

        [MenuItem("Tools/Abyssal Dungeon/Probe Exported Models")]
        public static void Probe()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== ABYSSAL DUNGEON 3D — EXPORTED PROJECT ASSET PROBE ===");

            string[] folders = { ExportedCharDir, ExportedWeapDir, ExportedEnemyDir, ExportedEnvDir };
            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder)) continue;
                sb.AppendLine($"--- Folder: {folder} ---");
                foreach (var raw in Directory.GetFiles(folder, "*.prefab").OrderBy(p => p))
                {
                    string path = raw.Replace("\\", "/");
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go == null)
                    {
                        sb.AppendLine($"  [NULL] {path}");
                        continue;
                    }
                    var inst = (GameObject)UnityEngine.Object.Instantiate(go);
                    var rs = inst.GetComponentsInChildren<Renderer>(true);
                    Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds();
                    for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                    string mats = string.Join(", ", rs.SelectMany(r => r.sharedMaterials).Distinct()
                        .Select(m => m ? $"{m.name}({(m.shader ? m.shader.name : "no-shader")})" : "null"));
                    sb.AppendLine($"  {Path.GetFileName(path),-34} renderers={rs.Length} size={b.size:F2} minY={b.min.y:F2} mats=[{mats}]");
                    UnityEngine.Object.DestroyImmediate(inst);
                }
            }

            if (!Directory.Exists("Temp")) Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/probe.txt", sb.ToString());
            Debug.Log(sb.ToString());
        }

        public static void BatchConfigureAndProbe()
        {
            ConfigureAll();
            Probe();
        }
    }
}
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AbyssGame
{
    /// <summary>
    /// Ensures every procedural Mesh, Material, Texture2D, Sprite, Font, and VolumeProfile
    /// created by builders is saved as a physical asset file under Assets/GeneratedBakedAssets/
    /// via AssetDatabase.CreateAsset so scenes baked in Edit Mode never lose meshes or turn pink.
    /// </summary>
    public static class BakedAssetRegistry
    {
        public const string RootFolder = "Assets/GeneratedBakedAssets";
        public const string MeshesFolder = RootFolder + "/Meshes";
        public const string MaterialsFolder = RootFolder + "/Materials";
        public const string TexturesFolder = RootFolder + "/Textures";
        public const string ProfilesFolder = RootFolder + "/Profiles";

        public const string PrefabsRootFolder = "Assets/Prefabs";
        public const string HeroesPrefabFolder = PrefabsRootFolder + "/Heroes";
        public const string MonstersPrefabFolder = PrefabsRootFolder + "/Monsters";
        public const string BossPrefabFolder = PrefabsRootFolder + "/Boss";
        public const string PropsPrefabFolder = PrefabsRootFolder + "/DungeonProps";

#if UNITY_EDITOR
        private static readonly HashSet<string> _savedPrefabsThisSession = new HashSet<string>();
        private static int _autoAssetIdx = 0;

        public static void ResetPrefabSessionCache()
        {
            _savedPrefabsThisSession.Clear();
        }
#endif

        public static void DestroySafe(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(obj);
            else
                UnityEngine.Object.DestroyImmediate(obj);
        }

        public static string SanitizeFileName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "UnnamedAsset";
            string clean = Regex.Replace(raw.Trim(), @"[^a-zA-Z0-9_\-\.]", "_");
            if (clean.Length > 96) clean = clean.Substring(0, 96);
            return clean;
        }

#if UNITY_EDITOR
        public static void EnsureFolders()
        {
            EnsureFolder("Assets", "GeneratedBakedAssets");
            EnsureFolder(RootFolder, "Meshes");
            EnsureFolder(RootFolder, "Materials");
            EnsureFolder(RootFolder, "Textures");
            EnsureFolder(RootFolder, "Profiles");

            EnsureFolder("Assets", "Prefabs");
            EnsureFolder(PrefabsRootFolder, "Heroes");
            EnsureFolder(PrefabsRootFolder, "Monsters");
            EnsureFolder(PrefabsRootFolder, "Boss");
            EnsureFolder(PrefabsRootFolder, "DungeonProps");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string full = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(parent, child);
        }
#endif

        public static Mesh GetOrCreateMesh(string assetName, Func<Mesh> builder)
        {
            string clean = SanitizeFileName(assetName);
#if UNITY_EDITOR
            EnsureFolders();
            string path = $"{MeshesFolder}/{clean}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null && existing.vertexCount > 0)
                return existing;

            Mesh created = builder();
            if (created == null) return null;
            created.name = clean;
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(created)))
            {
                if (existing != null)
                    AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(created, path);
            }
            return created;
#else
            return builder();
#endif
        }

        public static Texture2D GetOrCreateTexture(string assetName, Func<Texture2D> builder)
        {
            string clean = SanitizeFileName(assetName);
#if UNITY_EDITOR
            EnsureFolders();
            string path = $"{TexturesFolder}/{clean}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null)
                return existing;

            Texture2D created = builder();
            if (created == null) return null;
            created.name = clean;
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(created)))
            {
                AssetDatabase.CreateAsset(created, path);
            }
            return created;
#else
            return builder();
#endif
        }

        public static Material GetOrCreateMaterial(string assetName, Func<Material> builder)
        {
            string clean = SanitizeFileName(assetName);
#if UNITY_EDITOR
            EnsureFolders();
            string path = $"{MaterialsFolder}/{clean}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null && existing.shader != null && existing.shader.isSupported)
                return existing;

            Material created = builder();
            if (created == null) return null;
            created.name = clean;
            PersistMaterialTexturesIfNeeded(created, clean);

            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(created)))
            {
                if (existing != null)
                    AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(created, path);
            }
            return created;
#else
            return builder();
#endif
        }

        public static VolumeProfile GetOrCreateVolumeProfile(string assetName, Func<VolumeProfile> builder)
        {
            string clean = SanitizeFileName(assetName);
#if UNITY_EDITOR
            EnsureFolders();
            string path = $"{ProfilesFolder}/{clean}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (existing != null && existing.components != null && existing.components.Count > 0 && existing.components[0] != null)
                return existing;

            VolumeProfile created = builder();
            if (created == null) return null;
            created.name = clean;
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            AssetDatabase.CreateAsset(created, path);
            if (created.components != null)
            {
                for (int i = 0; i < created.components.Count; i++)
                {
                    var comp = created.components[i];
                    if (comp != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(comp)))
                    {
                        comp.name = comp.GetType().Name;
                        AssetDatabase.AddObjectToAsset(comp, created);
                    }
                }
            }
            EditorUtility.SetDirty(created);
            return created;
#else
            return builder();
#endif
        }

        /// <summary>
        /// Saves or loads a real Prefab asset under Assets/Prefabs/... (after persisting all child meshes, materials, and textures)
        /// and instantiates it into the scene as a linked Prefab Instance in Edit Mode.
        /// </summary>
        public static T InstantiateOrBuildPrefab<T>(
            string prefabFolder,
            string prefabName,
            Transform parent,
            Vector3 worldPos,
            Quaternion worldRot,
            Func<GameObject> templateBuilder,
            Action<T> instanceInitializer = null) where T : Component
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EnsureFolders();
                string cleanName = SanitizeFileName(prefabName);
                string prefabPath = $"{prefabFolder}/{cleanName}.prefab";

                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefabAsset == null || !_savedPrefabsThisSession.Contains(prefabPath))
                {
                    GameObject tempGo = templateBuilder();
                    tempGo.name = cleanName;
                    tempGo.transform.SetParent(null, false);
                    tempGo.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                    PersistAllUnsavedAssetsOnRoot(tempGo, ref _autoAssetIdx);

                    prefabAsset = PrefabUtility.SaveAsPrefabAsset(tempGo, prefabPath);
                    _savedPrefabsThisSession.Add(prefabPath);
                    UnityEngine.Object.DestroyImmediate(tempGo);
                }

                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, parent);
                inst.transform.SetPositionAndRotation(worldPos, worldRot);
                T comp = inst.GetComponent<T>();
                instanceInitializer?.Invoke(comp);
                if (comp != null) EditorUtility.SetDirty(comp);
                EditorUtility.SetDirty(inst);
                return comp;
            }
#endif
            GameObject runtimeGo = templateBuilder();
            if (parent != null)
                runtimeGo.transform.SetParent(parent, false);
            runtimeGo.transform.SetPositionAndRotation(worldPos, worldRot);
            T runtimeComp = runtimeGo.GetComponent<T>();
            instanceInitializer?.Invoke(runtimeComp);
            return runtimeComp;
        }

#if UNITY_EDITOR
        private static readonly string[] CommonTextureProps =
        {
            "_BaseMap", "_MainTex", "_BumpMap", "_EmissionMap", "_MetallicGlossMap", "_OcclusionMap"
        };

        public static void PersistMaterialTexturesIfNeeded(Material mat, string matCleanName)
        {
            if (mat == null) return;
            EnsureFolders();

            for (int i = 0; i < CommonTextureProps.Length; i++)
            {
                string prop = CommonTextureProps[i];
                if (!mat.HasProperty(prop)) continue;
                Texture tex = mat.GetTexture(prop);
                if (tex == null) continue;
                if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(tex))) continue;

                string texName = SanitizeFileName(!string.IsNullOrEmpty(tex.name) ? tex.name : $"{matCleanName}_{prop}");
                string texPath = $"{TexturesFolder}/{texName}.asset";
                var already = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
                if (already != null)
                {
                    mat.SetTexture(prop, already);
                }
                else
                {
                    tex.name = texName;
                    AssetDatabase.CreateAsset(tex, texPath);
                }
            }
        }

        /// <summary>
        /// Exhaustive post-bake sweep across an entire Scene to guarantee that every single
        /// MeshFilter, SkinnedMeshRenderer, Renderer, ParticleSystemRenderer, Volume, Image, and Text
        /// references a persisted physical asset on disk.
        /// </summary>
        public static void PersistAllUnsavedAssetsInScene(Scene scene)
        {
            if (!scene.IsValid()) return;
            EnsureFolders();

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                PersistAllUnsavedAssetsOnRoot(root, ref _autoAssetIdx);
            }

            AssetDatabase.SaveAssets();
        }

        public static void PersistAllUnsavedAssetsOnRoot(GameObject root, ref int autoIdx)
        {
            if (root == null) return;
            EnsureFolders();

            // 1. MeshFilters
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf == null || mf.sharedMesh == null) continue;
                Mesh m = mf.sharedMesh;
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(m)))
                {
                    string mName = SanitizeFileName(!string.IsNullOrEmpty(m.name) ? m.name : $"Mesh_{mf.gameObject.name}_{autoIdx++}");
                    string path = $"{MeshesFolder}/{mName}.asset";
                    var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (existing != null && existing.vertexCount == m.vertexCount)
                    {
                        mf.sharedMesh = existing;
                    }
                    else
                    {
                        if (existing != null) path = $"{MeshesFolder}/{mName}_{autoIdx++}.asset";
                        m.name = Path.GetFileNameWithoutExtension(path);
                        AssetDatabase.CreateAsset(m, path);
                        mf.sharedMesh = m;
                    }
                    EditorUtility.SetDirty(mf);
                }
            }

            // 2. Renderers (MeshRenderer, SkinnedMeshRenderer, ParticleSystemRenderer, TrailRenderer, LineRenderer)
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;

                if (r is SkinnedMeshRenderer smr && smr.sharedMesh != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(smr.sharedMesh)))
                {
                    Mesh m = smr.sharedMesh;
                    string mName = SanitizeFileName(!string.IsNullOrEmpty(m.name) ? m.name : $"SkinnedMesh_{r.gameObject.name}_{autoIdx++}");
                    string path = $"{MeshesFolder}/{mName}.asset";
                    if (AssetDatabase.LoadAssetAtPath<Mesh>(path) == null)
                        AssetDatabase.CreateAsset(m, path);
                    smr.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    EditorUtility.SetDirty(smr);
                }

                var mats = r.sharedMaterials;
                bool matChanged = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material mat = mats[i];
                    if (mat == null) continue;
                    if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mat)))
                    {
                        string baseName = !string.IsNullOrEmpty(mat.name) ? mat.name : $"Mat_{r.gameObject.name}_{i}";
                        baseName = baseName.Replace("(Instance)", "").Trim();
                        Color bc = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : (mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white);
                        string colorTag = ColorUtility.ToHtmlStringRGBA(bc);
                        string cleanMat = SanitizeFileName($"{baseName}_{colorTag}");
                        string path = $"{MaterialsFolder}/{cleanMat}.mat";

                        PersistMaterialTexturesIfNeeded(mat, cleanMat);

                        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (existing != null)
                        {
                            mats[i] = existing;
                        }
                        else
                        {
                            mat.name = cleanMat;
                            AssetDatabase.CreateAsset(mat, path);
                            mats[i] = mat;
                        }
                        matChanged = true;
                    }
                }
                if (matChanged)
                {
                    r.sharedMaterials = mats;
                    EditorUtility.SetDirty(r);
                }
            }

            // 3. Post-Processing Volumes
            foreach (var vol in root.GetComponentsInChildren<Volume>(true))
            {
                if (vol == null || vol.sharedProfile == null) continue;
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(vol.sharedProfile)))
                {
                    var prof = vol.sharedProfile;
                    vol.sharedProfile = GetOrCreateVolumeProfile(!string.IsNullOrEmpty(prof.name) ? prof.name : "AbyssalDarkFantasyProfile", () => prof);
                    EditorUtility.SetDirty(vol);
                }
            }

            // 4. UI Fonts & RawImages
            Font fallbackTitle = Resources.Load<Font>("UI/Fonts/GothicTitle") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            Font fallbackBody = Resources.Load<Font>("UI/Fonts/GothicBody") ?? fallbackTitle;

            foreach (var txt in root.GetComponentsInChildren<Text>(true))
            {
                if (txt == null) continue;
                if (txt.font == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(txt.font)))
                {
                    txt.font = txt.fontSize >= 20 ? fallbackTitle : fallbackBody;
                    EditorUtility.SetDirty(txt);
                }
            }

            foreach (var raw in root.GetComponentsInChildren<RawImage>(true))
            {
                if (raw == null || raw.texture == null) continue;
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(raw.texture)))
                {
                    string tName = SanitizeFileName(!string.IsNullOrEmpty(raw.texture.name) ? raw.texture.name : $"RawTex_{raw.gameObject.name}");
                    string path = $"{TexturesFolder}/{tName}.asset";
                    var existing = AssetDatabase.LoadAssetAtPath<Texture>(path);
                    if (existing != null)
                    {
                        raw.texture = existing;
                    }
                    else
                    {
                        raw.texture.name = tName;
                        AssetDatabase.CreateAsset(raw.texture, path);
                    }
                    EditorUtility.SetDirty(raw);
                }
            }
        }
#endif
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// High-performance cached loader & spawner for Phase 1 authored polygonal 3D Environment & Prop meshes.
    /// Supports Editor-baked LODGroup Prefabs in Resources/EnvironmentKit/ as well as direct runtime parsing
    /// of Wavefront .obj mirrors in Resources/EnvironmentMeshes/ with Unity coordinate & tangent conversion.
    /// </summary>
    public static class ObjMeshLoader
    {
        private static readonly Dictionary<string, Mesh> _meshCache = new Dictionary<string, Mesh>(StringComparer.Ordinal);
        private static readonly Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private static readonly MaterialPropertyBlock _mpb = new MaterialPropertyBlock();
        private static Mesh _quadMesh;
        private static Mesh _gothicBrickWallStoneMesh;
        private static Mesh _gothicBrickWallGoldMesh;

        /// <summary>
        /// Loads or parses an authored polygonal Mesh (e.g. "GothicPillar_LOD0").
        /// </summary>
        public static Mesh GetMesh(string meshName)
        {
            if (string.IsNullOrEmpty(meshName)) return null;
            if (_meshCache.TryGetValue(meshName, out var cached) && cached != null)
                return cached;

            // 1. Check if an imported Mesh or Prefab exists in Resources/EnvironmentKit
            var importedGo = Resources.Load<GameObject>("EnvironmentKit/Raw/" + meshName);
            if (importedGo != null)
            {
                var mf = importedGo.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    _meshCache[meshName] = mf.sharedMesh;
                    return mf.sharedMesh;
                }
            }

            // 2. Parse authored .obj text mirror from Resources/EnvironmentMeshes/<meshName>
            TextAsset txt = Resources.Load<TextAsset>("EnvironmentMeshes/" + meshName);
            if (txt == null) return null;

            Mesh parsed = ParseObjToUnityMesh(meshName, txt.text);
            if (parsed != null)
                _meshCache[meshName] = parsed;
            return parsed;
        }

        /// <summary>
        /// Spawns a modular 3D piece with automatic LOD0 + LOD1 setup, shared PBR material, and optional fitted BoxCollider.
        /// BaseName can be passed either without suffix ("GothicPillar") or with "_LOD0".
        /// </summary>
        public static GameObject SpawnPiece(
            string baseName,
            Transform parent,
            Vector3 localPos,
            Quaternion localRot,
            Vector3 localScale,
            Material material,
            bool addBoxCollider = false,
            bool castShadows = true)
        {
            string cleanBase = baseName.EndsWith("_LOD0", StringComparison.Ordinal)
                ? baseName.Substring(0, baseName.Length - 5)
                : baseName;

            if (cleanBase == "DungeonWall_Modular")
            {
                return SpawnOrnateGothicBrickWall(parent, localPos, localRot, localScale, material, addBoxCollider, castShadows);
            }

            // Check Editor-baked LODGroup prefab first
            if (!_prefabCache.TryGetValue(cleanBase, out var prefab))
            {
                prefab = Resources.Load<GameObject>("EnvironmentKit/" + cleanBase);
                _prefabCache[cleanBase] = prefab; // caches null too so we don't repeat lookup
            }

            GameObject root;
            Bounds boundsLocal = new Bounds(new Vector3(0f, 0.5f, 0f), Vector3.one);

            if (prefab != null)
            {
                root = UnityEngine.Object.Instantiate(prefab, parent, false);
                root.name = cleanBase;
                root.transform.localPosition = localPos;
                root.transform.localRotation = localRot;
                root.transform.localScale = localScale;

                bool forceCustomWallMat = cleanBase == "GothicGateFrame" || cleanBase == "GothicPillar" || cleanBase == "GothicArchVault";
                bool forceFxMaterial = forceCustomWallMat || (material != null && material.shader != null &&
                    (material.shader.name.IndexOf("AbyssalCrystal", StringComparison.OrdinalIgnoreCase) >= 0
                     || material.renderQueue >= 2900));

                var renderers = root.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer r = renderers[i];
                    Material existing = r.sharedMaterial;
                    bool hasPaintedTex = existing != null &&
                        ((existing.HasProperty("_BaseMap") && existing.GetTexture("_BaseMap") != null) ||
                         (existing.HasProperty("_MainTex") && existing.GetTexture("_MainTex") != null));

                    if (!hasPaintedTex || forceFxMaterial)
                    {
                        if (material != null) r.sharedMaterial = material;
                    }
                    else if (material != null)
                    {
                        // Preserve ExportedProject hand-painted PBR texture & normal map while applying subtle biome/rarity tint
                        Color callerCol = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
                        Color tinted = Color.Lerp(Color.white, callerCol, 0.35f);
                        tinted.a = 1f;
                        _mpb.Clear();
                        _mpb.SetColor("_BaseColor", tinted);
                        _mpb.SetColor("_Color", tinted);
                        if (material.IsKeywordEnabled("_EMISSION") && material.HasProperty("_EmissionColor"))
                        {
                            _mpb.SetColor("_EmissionColor", material.GetColor("_EmissionColor") * 0.45f);
                        }
                        r.SetPropertyBlock(_mpb);
                    }

                    r.shadowCastingMode = castShadows
                        ? UnityEngine.Rendering.ShadowCastingMode.On
                        : UnityEngine.Rendering.ShadowCastingMode.Off;
                }

                boundsLocal = ComputeRootLocalBounds(root);
            }
            else
            {
                Mesh lod0 = GetMesh(cleanBase + "_LOD0") ?? GetMesh(cleanBase);
                Mesh lod1 = GetMesh(cleanBase + "_LOD1");

                root = new GameObject(cleanBase);
                if (parent != null) root.transform.SetParent(parent, false);
                root.transform.localPosition = localPos;
                root.transform.localRotation = localRot;
                root.transform.localScale = localScale;

                if (lod0 != null)
                {
                    boundsLocal = lod0.bounds;
                    if (lod1 != null)
                    {
                        var r0 = CreateMeshChild("LOD0", root.transform, lod0, material, castShadows);
                        var r1 = CreateMeshChild("LOD1", root.transform, lod1, material, castShadows);
                        var lodGroup = root.AddComponent<LODGroup>();
                        lodGroup.SetLODs(new[]
                        {
                            new LOD(0.24f, new Renderer[] { r0 }),
                            new LOD(0.02f, new Renderer[] { r1 })
                        });
                        lodGroup.RecalculateBounds();
                    }
                    else
                    {
                        var mf = root.AddComponent<MeshFilter>();
                        mf.sharedMesh = lod0;
                        var mr = root.AddComponent<MeshRenderer>();
                        if (material != null) mr.sharedMaterial = material;
                        mr.shadowCastingMode = castShadows
                            ? UnityEngine.Rendering.ShadowCastingMode.On
                            : UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                }
            }

            if (addBoxCollider && root.GetComponent<Collider>() == null)
            {
                var bc = root.AddComponent<BoxCollider>();
                bc.center = boundsLocal.center;
                bc.size = new Vector3(
                    Mathf.Max(0.08f, boundsLocal.size.x),
                    Mathf.Max(0.08f, boundsLocal.size.y),
                    Mathf.Max(0.08f, boundsLocal.size.z));
            }

            return root;
        }

        /// <summary>
        /// Spawns a 4.0m x 5.2m 3D Ornate Gothic Brick & Carved Relief Wall Panel (double-sided)
        /// featuring staggered Gothic ashlar brickwork, molded plinth dado, fluted pilasters,
        /// twin pointed Gothic blind-arch ribs, corbelled crown cornice, and bronze-gold relief medallions.
        /// </summary>
        private static GameObject SpawnOrnateGothicBrickWall(
            Transform parent,
            Vector3 localPos,
            Quaternion localRot,
            Vector3 localScale,
            Material wallMat,
            bool addBoxCollider,
            bool castShadows)
        {
            EnsureOrnateGothicBrickWallMeshes();

            var root = new GameObject("DungeonWall_Modular");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            root.transform.localRotation = localRot;
            root.transform.localScale = localScale;

            Material stoneMat = wallMat != null ? wallMat : PbrMaterialLibrary.GothicStone();
            Material goldMat = PbrMaterialLibrary.RunicGold();

            CreateMeshChild("GothicBrickMasonry", root.transform, _gothicBrickWallStoneMesh, stoneMat, castShadows);
            CreateMeshChild("GothicReliefTrim", root.transform, _gothicBrickWallGoldMesh, goldMat, castShadows);

            if (addBoxCollider)
            {
                var bc = root.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 2.6f, 0f);
                bc.size = new Vector3(4.0f, 5.2f, 0.56f);
            }

            return root;
        }

        private static void EnsureOrnateGothicBrickWallMeshes()
        {
            if (_gothicBrickWallStoneMesh != null && _gothicBrickWallGoldMesh != null)
                return;

            var sV = new List<Vector3>(512);
            var sUV = new List<Vector2>(512);
            var sN = new List<Vector3>(512);
            var sT = new List<int>(1024);

            var gV = new List<Vector3>(256);
            var gUV = new List<Vector2>(256);
            var gN = new List<Vector3>(256);
            var gT = new List<int>(512);

            // 1. Main Solid Staggered Gothic Brick Backing Wall (4.0m wide x 5.2m tall x 0.34m thick)
            AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(0f, 2.60f, 0f), new Vector3(4.00f, 5.20f, 0.34f), 0.38f);

            // 2. Heavy Molded Stone Plinth Base (y = 0..0.68m) & Upper Plinth Ledge
            AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(0f, 0.34f, 0f), new Vector3(4.04f, 0.68f, 0.54f), 0.40f);
            AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(0f, 0.74f, 0f), new Vector3(4.02f, 0.12f, 0.48f), 0.40f);

            // 3. Left, CenterMullion & Right Fluted Gothic Buttress Pilasters
            float[] pilasterXs = { -1.84f, 0.0f, 1.84f };
            for (int i = 0; i < pilasterXs.Length; i++)
            {
                float px = pilasterXs[i];
                float pw = (i == 1) ? 0.30f : 0.34f;
                float pd = (i == 1) ? 0.50f : 0.58f;
                AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(px, 2.60f, 0f), new Vector3(pw, 5.20f, pd), 0.42f);
                // Pilaster Capital & Base Collar
                AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(px, 4.42f, 0f), new Vector3(pw + 0.10f, 0.18f, pd + 0.08f), 0.42f);
                AppendBoxUvScaled(gV, gUV, gN, gT, new Vector3(px, 4.30f, 0f), new Vector3(pw + 0.06f, 0.06f, pd + 0.05f), 0.5f);
            }

            // 4. Carved Gothic Dado Frieze Band (y = 1.72m .. 1.98m) with Bronze-Gold Relief Moldings
            AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(0f, 1.85f, 0f), new Vector3(4.02f, 0.26f, 0.48f), 0.42f);
            AppendBoxUvScaled(gV, gUV, gN, gT, new Vector3(0f, 1.99f, 0f), new Vector3(4.03f, 0.05f, 0.50f), 0.5f);
            AppendBoxUvScaled(gV, gUV, gN, gT, new Vector3(0f, 1.71f, 0f), new Vector3(4.03f, 0.05f, 0.50f), 0.5f);

            // 5. Twin Gothic Pointed Blind Arches (Left Bay x = -0.92m, Right Bay x = +0.92m)
            float[] bayXs = { -0.92f, 0.92f };
            for (int b = 0; b < bayXs.Length; b++)
            {
                float bx = bayXs[b];
                // Vertical jambs of the blind arch
                AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(bx - 0.58f, 3.05f, 0f), new Vector3(0.14f, 2.15f, 0.44f), 0.42f);
                AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(bx + 0.58f, 3.05f, 0f), new Vector3(0.14f, 2.15f, 0.44f), 0.42f);

                // Pointed Ogive Arch Ribs (5 segments per side meeting at a sharp Gothic apex at y = 4.46m)
                int segs = 4;
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int s = 0; s < segs; s++)
                    {
                        float t0 = (float)s / segs;
                        float t1 = (float)(s + 1) / segs;
                        float tm = (t0 + t1) * 0.5f;
                        float x = bx + side * Mathf.Lerp(0.56f, 0.05f, tm);
                        float y = Mathf.Lerp(3.95f, 4.44f, Mathf.Sin(tm * Mathf.PI * 0.5f));
                        AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(x, y, 0f), new Vector3(0.18f, 0.20f, 0.46f), 0.45f);
                    }
                }

                // Gothic Arch Keystone + Carved Central Quatrefoil Rosette Boss in each bay
                AppendBoxUvScaled(gV, gUV, gN, gT, new Vector3(bx, 4.48f, 0f), new Vector3(0.18f, 0.22f, 0.49f), 0.5f);
                AppendBoxUvScaled(gV, gUV, gN, gT, new Vector3(bx, 3.22f, 0f), new Vector3(0.34f, 0.34f, 0.42f), 0.5f);
                AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(bx, 3.22f, 0f), new Vector3(0.22f, 0.52f, 0.40f), 0.5f);
                AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(bx, 3.22f, 0f), new Vector3(0.52f, 0.22f, 0.40f), 0.5f);
            }

            // 6. Corbelled Gothic Crown Cornice & Crenellated Frieze (y = 4.68m .. 5.20m)
            AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(0f, 4.78f, 0f), new Vector3(4.04f, 0.22f, 0.54f), 0.42f);
            AppendBoxUvScaled(gV, gUV, gN, gT, new Vector3(0f, 4.91f, 0f), new Vector3(4.05f, 0.06f, 0.58f), 0.5f);
            AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(0f, 5.06f, 0f), new Vector3(4.06f, 0.26f, 0.62f), 0.42f);

            // Stone corbel brackets along the upper frieze
            for (int c = -3; c <= 3; c++)
            {
                AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(c * 0.52f, 4.62f, 0f), new Vector3(0.16f, 0.18f, 0.52f), 0.45f);
            }

            _gothicBrickWallStoneMesh = new Mesh { name = "OrnateGothicBrickWall_Stone" };
            _gothicBrickWallStoneMesh.SetVertices(sV);
            _gothicBrickWallStoneMesh.SetUVs(0, sUV);
            _gothicBrickWallStoneMesh.SetNormals(sN);
            _gothicBrickWallStoneMesh.SetTriangles(sT, 0);
            _gothicBrickWallStoneMesh.RecalculateBounds();
            _gothicBrickWallStoneMesh.RecalculateTangents();

            _gothicBrickWallGoldMesh = new Mesh { name = "OrnateGothicBrickWall_GoldTrim" };
            _gothicBrickWallGoldMesh.SetVertices(gV);
            _gothicBrickWallGoldMesh.SetUVs(0, gUV);
            _gothicBrickWallGoldMesh.SetNormals(gN);
            _gothicBrickWallGoldMesh.SetTriangles(gT, 0);
            _gothicBrickWallGoldMesh.RecalculateBounds();
            _gothicBrickWallGoldMesh.RecalculateTangents();
        }

        private static void AppendBoxUvScaled(
            List<Vector3> verts,
            List<Vector2> uvs,
            List<Vector3> norms,
            List<int> tris,
            Vector3 center,
            Vector3 size,
            float uvPerMeter)
        {
            Vector3 h = size * 0.5f;
            // Shift wall depth slightly backward (-0.14m Z) so the brick face sits near z=0 and reliefs project cleanly into +Z.
            // Omit the exterior -Z (Vector3.back) face so cameras positioned behind/at a perimeter wall looking into the room (+Z)
            // are never occluded by the back of the wall.
            Vector3 c = center + new Vector3(0f, 0f, -0.14f);

            // 5 interior-visible faces: +Z (room-facing front), +X, -X, +Y, -Y
            AddFaceQuad(verts, uvs, norms, tris,
                c + new Vector3(-h.x, -h.y,  h.z),
                c + new Vector3(-h.x,  h.y,  h.z),
                c + new Vector3( h.x,  h.y,  h.z),
                c + new Vector3( h.x, -h.y,  h.z),
                Vector3.forward, size.x * uvPerMeter, size.y * uvPerMeter);

            AddFaceQuad(verts, uvs, norms, tris,
                c + new Vector3( h.x, -h.y,  h.z),
                c + new Vector3( h.x,  h.y,  h.z),
                c + new Vector3( h.x,  h.y, -h.z),
                c + new Vector3( h.x, -h.y, -h.z),
                Vector3.right, size.z * uvPerMeter, size.y * uvPerMeter);

            AddFaceQuad(verts, uvs, norms, tris,
                c + new Vector3(-h.x, -h.y, -h.z),
                c + new Vector3(-h.x,  h.y, -h.z),
                c + new Vector3(-h.x,  h.y,  h.z),
                c + new Vector3(-h.x, -h.y,  h.z),
                Vector3.left, size.z * uvPerMeter, size.y * uvPerMeter);

            AddFaceQuad(verts, uvs, norms, tris,
                c + new Vector3(-h.x,  h.y,  h.z),
                c + new Vector3(-h.x,  h.y, -h.z),
                c + new Vector3( h.x,  h.y, -h.z),
                c + new Vector3( h.x,  h.y,  h.z),
                Vector3.up, size.x * uvPerMeter, size.z * uvPerMeter);

            AddFaceQuad(verts, uvs, norms, tris,
                c + new Vector3(-h.x, -h.y, -h.z),
                c + new Vector3(-h.x, -h.y,  h.z),
                c + new Vector3( h.x, -h.y,  h.z),
                c + new Vector3( h.x, -h.y, -h.z),
                Vector3.down, size.x * uvPerMeter, size.z * uvPerMeter);
        }

        private static void AddFaceQuad(
            List<Vector3> verts,
            List<Vector2> uvs,
            List<Vector3> norms,
            List<int> tris,
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            Vector3 normal,
            float uSpan,
            float vSpan)
        {
            int idx = verts.Count;
            verts.Add(v0); verts.Add(v1); verts.Add(v2); verts.Add(v3);
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(0f, vSpan));
            uvs.Add(new Vector2(uSpan, vSpan));
            uvs.Add(new Vector2(uSpan, 0f));
            norms.Add(normal); norms.Add(normal); norms.Add(normal); norms.Add(normal);
            tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
            tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 3);
        }

        /// <summary>
        /// Shared flat horizontal quad (1m x 1m on XZ plane, normal +Y, UVs 0..1) for ground mist & surface decals.
        /// </summary>
        public static Mesh GetHorizontalQuadMesh()
        {
            if (_quadMesh != null) return _quadMesh;
            _quadMesh = new Mesh { name = "AbyssalHorizontalQuad" };
            _quadMesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f,  0.5f),
                new Vector3( 0.5f, 0f,  0.5f),
                new Vector3( 0.5f, 0f, -0.5f)
            };
            _quadMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            };
            _quadMesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            _quadMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            _quadMesh.RecalculateBounds();
            _quadMesh.RecalculateTangents();
            return _quadMesh;
        }

        private static Bounds ComputeRootLocalBounds(GameObject root)
        {
            var mfs = root.GetComponentsInChildren<MeshFilter>(true);
            bool has = false;
            Bounds combined = new Bounds(new Vector3(0f, 0.5f, 0f), Vector3.one);
            Matrix4x4 w2l = root.transform.worldToLocalMatrix;

            for (int i = 0; i < mfs.Length; i++)
            {
                if (mfs[i] == null || mfs[i].sharedMesh == null) continue;
                Bounds mb = mfs[i].sharedMesh.bounds;
                Matrix4x4 m = w2l * mfs[i].transform.localToWorldMatrix;
                Vector3 c = mb.center;
                Vector3 e = mb.extents;

                for (int cx = -1; cx <= 1; cx += 2)
                for (int cy = -1; cy <= 1; cy += 2)
                for (int cz = -1; cz <= 1; cz += 2)
                {
                    Vector3 pt = m.MultiplyPoint3x4(new Vector3(c.x + cx * e.x, c.y + cy * e.y, c.z + cz * e.z));
                    if (!has) { combined = new Bounds(pt, Vector3.zero); has = true; }
                    else combined.Encapsulate(pt);
                }
            }
            return combined;
        }

        private static MeshRenderer CreateMeshChild(string childName, Transform parent, Mesh mesh, Material mat, bool castShadows)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(parent, false);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            if (mat != null) mr.sharedMaterial = mat;
            mr.shadowCastingMode = castShadows
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
            return mr;
        }

        private static Mesh ParseObjToUnityMesh(string name, string objText)
        {
            var rawV = new List<Vector3>(1024);
            var rawVt = new List<Vector2>(1024);
            var rawVn = new List<Vector3>(1024);

            var outV = new List<Vector3>(2048);
            var outVt = new List<Vector2>(2048);
            var outVn = new List<Vector3>(2048);
            var outTris = new List<int>(3072);
            var vertDedup = new Dictionary<long, int>(2048);

            var inv = CultureInfo.InvariantCulture;
            string[] lines = objText.Split('\n');
            int[] faceIdx = new int[8];

            for (int l = 0; l < lines.Length; l++)
            {
                string line = lines[l].Trim();
                if (line.Length < 3 || line[0] == '#') continue;

                if (line[0] == 'v' && line[1] == ' ')
                {
                    string[] p = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length >= 4)
                    {
                        float x = float.Parse(p[1], inv);
                        float y = float.Parse(p[2], inv);
                        float z = float.Parse(p[3], inv);
                        // Match Unity's native Right-Handed -> Left-Handed .obj import (-X)
                        rawV.Add(new Vector3(-x, y, z));
                    }
                }
                else if (line[0] == 'v' && line[1] == 't')
                {
                    string[] p = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length >= 3)
                    {
                        rawVt.Add(new Vector2(float.Parse(p[1], inv), float.Parse(p[2], inv)));
                    }
                }
                else if (line[0] == 'v' && line[1] == 'n')
                {
                    string[] p = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length >= 4)
                    {
                        float nx = float.Parse(p[1], inv);
                        float ny = float.Parse(p[2], inv);
                        float nz = float.Parse(p[3], inv);
                        rawVn.Add(new Vector3(-nx, ny, nz));
                    }
                }
                else if (line[0] == 'f' && line[1] == ' ')
                {
                    string[] p = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    int cornerCount = Mathf.Min(p.Length - 1, faceIdx.Length);
                    if (cornerCount < 3) continue;

                    for (int c = 0; c < cornerCount; c++)
                    {
                        string[] sub = p[c + 1].Split('/');
                        int vi = sub.Length > 0 && sub[0].Length > 0 ? int.Parse(sub[0], inv) - 1 : 0;
                        int ti = sub.Length > 1 && sub[1].Length > 0 ? int.Parse(sub[1], inv) - 1 : -1;
                        int ni = sub.Length > 2 && sub[2].Length > 0 ? int.Parse(sub[2], inv) - 1 : -1;

                        long key = ((long)(vi & 0xFFFFF) << 40)
                                 | ((long)((ti + 1) & 0xFFFFF) << 20)
                                 | (long)((ni + 1) & 0xFFFFF);

                        if (!vertDedup.TryGetValue(key, out int unified))
                        {
                            unified = outV.Count;
                            vertDedup[key] = unified;
                            outV.Add(vi >= 0 && vi < rawV.Count ? rawV[vi] : Vector3.zero);
                            outVt.Add(ti >= 0 && ti < rawVt.Count ? rawVt[ti] : Vector2.zero);
                            outVn.Add(ni >= 0 && ni < rawVn.Count ? rawVn[ni] : Vector3.up);
                        }
                        faceIdx[c] = unified;
                    }

                    // Fan triangulate with reversed winding (0, i+1, i) to match -X reflection
                    for (int t = 1; t < cornerCount - 1; t++)
                    {
                        outTris.Add(faceIdx[0]);
                        outTris.Add(faceIdx[t + 1]);
                        outTris.Add(faceIdx[t]);
                    }
                }
            }

            if (outV.Count == 0 || outTris.Count == 0) return null;

            var mesh = new Mesh { name = name };
            if (outV.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.SetVertices(outV);
            mesh.SetUVs(0, outVt);
            if (rawVn.Count > 0)
                mesh.SetNormals(outVn);
            mesh.SetTriangles(outTris, 0);
            if (rawVn.Count == 0)
                mesh.RecalculateNormals();

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}


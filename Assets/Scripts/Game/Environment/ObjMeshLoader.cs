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
        private static Mesh _gothicCathedralFloorStoneMesh;
        private static Mesh _gothicCathedralFloorGoldMesh;
        private static Mesh _gothicOpenArchStoneMesh;
        private static Mesh _gothicOpenArchTrimMesh;

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
            if (cleanBase == "DungeonFloor_ChannelTile")
            {
                return SpawnOrnateGothicCathedralFloor(parent, localPos, localRot, localScale, material, addBoxCollider);
            }
            if (cleanBase == "GothicArchVault" || cleanBase == "GothicGateFrame")
            {
                float baseW = cleanBase == "GothicGateFrame" ? 6.0f : 4.0f;
                return SpawnOrnateGothicOpenArch(cleanBase, baseW, parent, localPos, localRot, localScale, material, castShadows);
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
                    if (material != null)
                    {
                        Material existing = r.sharedMaterial;
                        bool isMultiMat = r.sharedMaterials != null && r.sharedMaterials.Length > 1;
                        bool isCustomFxShader = existing != null && existing.shader != null &&
                            (existing.shader.name.IndexOf("LightShaft", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             existing.shader.name.IndexOf("AbyssalCrystal", StringComparison.OrdinalIgnoreCase) >= 0);
                        bool hasPaintedTex = existing != null &&
                            ((existing.HasProperty("_BaseMap") && existing.GetTexture("_BaseMap") != null) ||
                             (existing.HasProperty("_MainTex") && existing.GetTexture("_MainTex") != null));

                        if (!isMultiMat && !isCustomFxShader && (!hasPaintedTex || forceFxMaterial))
                        {
                            r.sharedMaterial = material;
                        }
                        else if (!isCustomFxShader)
                        {
                            // Preserve multi-material / hand-painted PBR texture & normal map while applying subtle biome tint
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
                    }

                    r.shadowCastingMode = castShadows
                        ? UnityEngine.Rendering.ShadowCastingMode.On
                        : UnityEngine.Rendering.ShadowCastingMode.Off;
                }

                var embeddedLights = root.GetComponentsInChildren<Light>(true);
                for (int i = 0; i < embeddedLights.Length; i++)
                {
                    if (embeddedLights[i].type != LightType.Directional)
                        embeddedLights[i].shadows = LightShadows.None;
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
            Material ironTrimMat = PbrMaterialLibrary.WroughtIron();

            CreateMeshChild("GothicBrickMasonry", root.transform, _gothicBrickWallStoneMesh, stoneMat, false);
            CreateMeshChild("GothicReliefTrim", root.transform, _gothicBrickWallGoldMesh, ironTrimMat, false);

            if (addBoxCollider)
            {
                var bc = root.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 2.6f, 0f);
                bc.size = new Vector3(4.0f, 5.2f, 0.56f);
            }

            return root;
        }

        /// <summary>
        /// Spawns a 100% open, pass-through 3D Gothic Pointed Cathedral Archway (zero grates/bars blocking heroes or doors).
        /// </summary>
        private static GameObject SpawnOrnateGothicOpenArch(
            string name,
            float baseWidth,
            Transform parent,
            Vector3 localPos,
            Quaternion localRot,
            Vector3 localScale,
            Material archMat,
            bool castShadows)
        {
            if (_gothicOpenArchStoneMesh == null || _gothicOpenArchTrimMesh == null)
            {
                var sV = new List<Vector3>(256);
                var sUV = new List<Vector2>(256);
                var sN = new List<Vector3>(256);
                var sT = new List<int>(512);

                var tV = new List<Vector3>(128);
                var tUV = new List<Vector2>(128);
                var tN = new List<Vector3>(128);
                var tT = new List<int>(256);

                // Left & Right Heavy Gothic Jamb Pillars (4.0m nominal span, clear 3.0m opening)
                for (int side = -1; side <= 1; side += 2)
                {
                    float jx = side * 1.76f;
                    AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(jx, 0.32f, 0f), new Vector3(0.58f, 0.64f, 0.74f), 0.42f);
                    AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(jx, 2.15f, 0f), new Vector3(0.46f, 3.30f, 0.62f), 0.42f);
                    AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(jx, 3.88f, 0f), new Vector3(0.58f, 0.22f, 0.72f), 0.42f);
                    AppendBoxUvScaled(tV, tUV, tN, tT, new Vector3(jx, 3.74f, 0f), new Vector3(0.52f, 0.06f, 0.66f), 0.5f);

                    // Pointed Ogive Voussoir Arch Stones rising from y = 3.95m to apex y = 5.02m
                    int segs = 5;
                    for (int s = 0; s < segs; s++)
                    {
                        float tm = (s + 0.5f) / segs;
                        float ax = side * Mathf.Lerp(1.66f, 0.16f, tm);
                        float ay = Mathf.Lerp(4.02f, 4.98f, Mathf.Sin(tm * Mathf.PI * 0.5f));
                        AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(ax, ay, 0f), new Vector3(0.42f, 0.34f, 0.60f), 0.45f);
                        AppendBoxUvScaled(tV, tUV, tN, tT, new Vector3(ax, ay - 0.16f, 0f), new Vector3(0.38f, 0.06f, 0.64f), 0.5f);
                    }
                }

                // Apex Keystone & Upper Spandrel Crown Lintel
                AppendBoxUvScaled(tV, tUV, tN, tT, new Vector3(0f, 5.04f, 0f), new Vector3(0.38f, 0.44f, 0.68f), 0.5f);
                AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(0f, 5.12f, 0f), new Vector3(4.06f, 0.22f, 0.64f), 0.42f);

                _gothicOpenArchStoneMesh = new Mesh { name = "OrnateGothicOpenArch_Stone" };
                _gothicOpenArchStoneMesh.SetVertices(sV);
                _gothicOpenArchStoneMesh.SetUVs(0, sUV);
                _gothicOpenArchStoneMesh.SetNormals(sN);
                _gothicOpenArchStoneMesh.SetTriangles(sT, 0);
                _gothicOpenArchStoneMesh.RecalculateBounds();
                _gothicOpenArchStoneMesh.RecalculateTangents();

                _gothicOpenArchTrimMesh = new Mesh { name = "OrnateGothicOpenArch_Trim" };
                _gothicOpenArchTrimMesh.SetVertices(tV);
                _gothicOpenArchTrimMesh.SetUVs(0, tUV);
                _gothicOpenArchTrimMesh.SetNormals(tN);
                _gothicOpenArchTrimMesh.SetTriangles(tT, 0);
                _gothicOpenArchTrimMesh.RecalculateBounds();
                _gothicOpenArchTrimMesh.RecalculateTangents();
            }

            var root = new GameObject(name);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            root.transform.localRotation = localRot;
            float widthFactor = baseWidth / 4.0f;
            root.transform.localScale = new Vector3(localScale.x * widthFactor, localScale.y, localScale.z);

            Material stoneMat = archMat != null ? archMat : PbrMaterialLibrary.GothicStone();
            Material ironMat = PbrMaterialLibrary.WroughtIron();
            CreateMeshChild("ArchStone", root.transform, _gothicOpenArchStoneMesh, stoneMat, false);
            CreateMeshChild("ArchIronTrim", root.transform, _gothicOpenArchTrimMesh, ironMat, false);
            return root;
        }

        private static void EnsureOrnateGothicBrickWallMeshes()
        {
            if (_gothicBrickWallStoneMesh != null && _gothicBrickWallGoldMesh != null)
                return;

            var sV = new List<Vector3>(256);
            var sUV = new List<Vector2>(256);
            var sN = new List<Vector3>(256);
            var sT = new List<int>(512);

            var gV = new List<Vector3>(64);
            var gUV = new List<Vector2>(64);
            var gN = new List<Vector3>(64);
            var gT = new List<int>(128);

            // 1. Clean Monumental Ashlar Stone Masonry Backing Wall (4.0m wide x 5.2m tall, seamless UV tiling)
            AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(0f, 2.60f, 0f), new Vector3(4.00f, 5.20f, 0.36f), 0.36f);

            // 2. Grounded Heavy Dungeon Stone Plinth Dado Base (y = 0..0.72m)
            AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(0f, 0.36f, 0.08f), new Vector3(4.02f, 0.72f, 0.44f), 0.38f);
            AppendBoxUvScaled(gV, gUV, gN, gT, new Vector3(0f, 0.74f, 0.12f), new Vector3(4.02f, 0.06f, 0.42f), 0.50f);

            // 3. Left & Right Bay Buttress Pilasters + Central Gothic Blind-Arch Lancet Relief
            float[] pilasterXs = { -1.84f, 1.84f };
            for (int i = 0; i < pilasterXs.Length; i++)
            {
                float px = pilasterXs[i];
                AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(px, 2.60f, 0.12f), new Vector3(0.36f, 5.20f, 0.48f), 0.38f);
                AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(px, 4.56f, 0.16f), new Vector3(0.48f, 0.24f, 0.54f), 0.40f);
                AppendBoxUvScaled(gV, gUV, gN, gT, new Vector3(px, 4.42f, 0.16f), new Vector3(0.42f, 0.06f, 0.52f), 0.50f);
            }

            // Twin Pointed Blind-Arch Moulded Stone Ribs on Front Wall Face (authentic Gothic cathedral relief)
            float[] mullionXs = { -0.92f, 0.0f, 0.92f };
            for (int m = 0; m < mullionXs.Length; m++)
            {
                float mx = mullionXs[m];
                float mw = (m == 1) ? 0.18f : 0.13f;
                AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(mx, 2.35f, 0.14f), new Vector3(mw, 3.20f, 0.22f), 0.42f);
            }
            // Pointed blind-arch voussoir lintels spanning the twin bays (y = 3.95..4.48m)
            for (int bay = -1; bay <= 1; bay += 2)
            {
                float bx = bay * 0.92f;
                AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(bx - 0.36f, 4.18f, 0.14f), new Vector3(0.46f, 0.20f, 0.22f), 0.45f);
                AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(bx + 0.36f, 4.18f, 0.14f), new Vector3(0.46f, 0.20f, 0.22f), 0.45f);
                AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(bx, 4.38f, 0.15f), new Vector3(0.44f, 0.22f, 0.24f), 0.45f);
                AppendBoxUvScaled(gV, gUV, gN, gT, new Vector3(bx, 4.38f, 0.20f), new Vector3(0.16f, 0.18f, 0.16f), 0.50f);
            }

            // 4. Clean Architectural Crown Cornice Lintel (y = 4.82m .. 5.20m) & Subtle Wrought-Iron Tie Band
            AppendBoxUvScaled(sV, sUV, sN, sT, new Vector3(0f, 4.98f, 0.14f), new Vector3(4.04f, 0.42f, 0.52f), 0.38f);
            AppendBoxUvScaled(gV, gUV, gN, gT, new Vector3(0f, 4.74f, 0.16f), new Vector3(4.02f, 0.06f, 0.46f), 0.50f);

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

        /// <summary>
        /// Spawns a clean, seamless 4.0m x 4.0m Gold and Glory Medieval Flagstone Floor Tile
        /// with world-aligned hand-painted stone slabs and zero repeating bullseye/gold grids.
        /// </summary>
        private static GameObject SpawnOrnateGothicCathedralFloor(
            Transform parent,
            Vector3 localPos,
            Quaternion localRot,
            Vector3 localScale,
            Material floorMat,
            bool addBoxCollider)
        {
            EnsureOrnateGothicCathedralFloorMeshes();

            var root = new GameObject("DungeonFloor_ChannelTile");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            root.transform.localRotation = localRot;
            root.transform.localScale = localScale;

            Material stoneMat = floorMat != null ? floorMat : PbrMaterialLibrary.CathedralFloor();
            var rStone = CreateMeshChild("GothicFloorMasonry", root.transform, _gothicCathedralFloorStoneMesh, stoneMat, false);
            rStone.receiveShadows = true;

            if (addBoxCollider)
            {
                var bc = root.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, -0.08f, 0f);
                bc.size = new Vector3(4.0f, 0.16f, 4.0f);
            }

            return root;
        }

        private static void EnsureOrnateGothicCathedralFloorMeshes()
        {
            if (_gothicCathedralFloorStoneMesh != null)
                return;

            var sV = new List<Vector3>(128);
            var sUV = new List<Vector2>(128);
            var sN = new List<Vector3>(128);
            var sT = new List<int>(256);

            // Tight seamless Cathedral Flagstone slabs (flush near y = 0.0m, subtle stone joints instead of stark black grid lines)
            AppendFloorSlabBox(sV, sUV, sN, sT, new Vector3(0f, -0.056f, 0f), new Vector3(4.00f, 0.09f, 4.00f));
            for (int qx = -1; qx <= 1; qx += 2)
            {
                for (int qz = -1; qz <= 1; qz += 2)
                {
                    Vector3 qc = new Vector3(qx * 0.996f, -0.012f, qz * 0.996f);
                    AppendFloorSlabBox(sV, sUV, sN, sT, qc, new Vector3(1.984f, 0.024f, 1.984f));
                }
            }

            _gothicCathedralFloorStoneMesh = new Mesh { name = "OrnateGothicCathedralFloor_Stone" };
            _gothicCathedralFloorStoneMesh.SetVertices(sV);
            _gothicCathedralFloorStoneMesh.SetUVs(0, sUV);
            _gothicCathedralFloorStoneMesh.SetNormals(sN);
            _gothicCathedralFloorStoneMesh.SetTriangles(sT, 0);
            _gothicCathedralFloorStoneMesh.RecalculateBounds();
            _gothicCathedralFloorStoneMesh.RecalculateTangents();
            _gothicCathedralFloorGoldMesh = _gothicCathedralFloorStoneMesh;
        }

        /// <summary>
        /// Appends a 5-face raised floor slab box (+Y top and 4 beveled vertical sides) with world-aligned 0..1 planar UVs
        /// mapped across the 4m x 4m tile so T_CathedralFloor_Albedo & Normal align seamlessly with the 3D geometry.
        /// </summary>
        private static void AppendFloorSlabBox(
            List<Vector3> verts,
            List<Vector2> uvs,
            List<Vector3> norms,
            List<int> tris,
            Vector3 center,
            Vector3 size)
        {
            Vector3 h = size * 0.5f;
            Vector3 v001 = center + new Vector3(-h.x,  h.y, -h.z);
            Vector3 v011 = center + new Vector3(-h.x,  h.y,  h.z);
            Vector3 v111 = center + new Vector3( h.x,  h.y,  h.z);
            Vector3 v101 = center + new Vector3( h.x,  h.y, -h.z);

            Vector3 v000 = center + new Vector3(-h.x, -h.y, -h.z);
            Vector3 v010 = center + new Vector3(-h.x, -h.y,  h.z);
            Vector3 v110 = center + new Vector3( h.x, -h.y,  h.z);
            Vector3 v100 = center + new Vector3( h.x, -h.y, -h.z);

            // Top face (+Y) mapped directly to 0..1 across the 4m x 4m tile
            int idx = verts.Count;
            verts.Add(v001); verts.Add(v011); verts.Add(v111); verts.Add(v101);
            uvs.Add(new Vector2((v001.x + 2f) * 0.25f, (v001.z + 2f) * 0.25f));
            uvs.Add(new Vector2((v011.x + 2f) * 0.25f, (v011.z + 2f) * 0.25f));
            uvs.Add(new Vector2((v111.x + 2f) * 0.25f, (v111.z + 2f) * 0.25f));
            uvs.Add(new Vector2((v101.x + 2f) * 0.25f, (v101.z + 2f) * 0.25f));
            norms.Add(Vector3.up); norms.Add(Vector3.up); norms.Add(Vector3.up); norms.Add(Vector3.up);
            tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
            tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 3);

            // 4 Side faces (+Z, -Z, +X, -X) for crisp normal-mapped stone edges
            AddFaceQuad(verts, uvs, norms, tris, v010, v110, v111, v011, Vector3.forward, size.x * 0.25f, size.y * 0.5f);
            AddFaceQuad(verts, uvs, norms, tris, v100, v000, v001, v101, Vector3.back,    size.x * 0.25f, size.y * 0.5f);
            AddFaceQuad(verts, uvs, norms, tris, v100, v101, v111, v110, Vector3.right,   size.z * 0.25f, size.y * 0.5f);
            AddFaceQuad(verts, uvs, norms, tris, v010, v011, v001, v000, Vector3.left,    size.z * 0.25f, size.y * 0.5f);
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
            // Shift backing depth slightly so front face (+Z) aligns cleanly with room perimeter and reliefs protrude inward.
            Vector3 c = center + new Vector3(0f, 0f, -0.14f);

            // All 6 faces with mathematically verified Left-Handed Unity clockwise front winding & upright UVs (+V = +Y):
            // 1. +Z Front Face (room-facing interior) -> Cross((0,+1,0), (-1,+1,0)) = (0,0,+1)
            AddFaceQuad(verts, uvs, norms, tris,
                c + new Vector3( h.x, -h.y,  h.z),
                c + new Vector3( h.x,  h.y,  h.z),
                c + new Vector3(-h.x,  h.y,  h.z),
                c + new Vector3(-h.x, -h.y,  h.z),
                Vector3.forward, size.x * uvPerMeter, size.y * uvPerMeter);

            // 2. -Z Back Face (visible from opposite side of archways/gates) -> Cross((0,+1,0), (+1,+1,0)) = (0,0,-1)
            AddFaceQuad(verts, uvs, norms, tris,
                c + new Vector3(-h.x, -h.y, -h.z),
                c + new Vector3(-h.x,  h.y, -h.z),
                c + new Vector3( h.x,  h.y, -h.z),
                c + new Vector3( h.x, -h.y, -h.z),
                Vector3.back, size.x * uvPerMeter, size.y * uvPerMeter);

            // 3. +X Right Face -> Cross((0,+1,0), (0,+1,+1)) = (+1,0,0)
            AddFaceQuad(verts, uvs, norms, tris,
                c + new Vector3( h.x, -h.y, -h.z),
                c + new Vector3( h.x,  h.y, -h.z),
                c + new Vector3( h.x,  h.y,  h.z),
                c + new Vector3( h.x, -h.y,  h.z),
                Vector3.right, size.z * uvPerMeter, size.y * uvPerMeter);

            // 4. -X Left Face -> Cross((0,+1,0), (0,+1,-1)) = (-1,0,0)
            AddFaceQuad(verts, uvs, norms, tris,
                c + new Vector3(-h.x, -h.y,  h.z),
                c + new Vector3(-h.x,  h.y,  h.z),
                c + new Vector3(-h.x,  h.y, -h.z),
                c + new Vector3(-h.x, -h.y, -h.z),
                Vector3.left, size.z * uvPerMeter, size.y * uvPerMeter);

            // 5. +Y Top Face -> Cross((0,0,+1), (+1,0,+1)) = (0,+1,0)
            AddFaceQuad(verts, uvs, norms, tris,
                c + new Vector3(-h.x,  h.y, -h.z),
                c + new Vector3(-h.x,  h.y,  h.z),
                c + new Vector3( h.x,  h.y,  h.z),
                c + new Vector3( h.x,  h.y, -h.z),
                Vector3.up, size.x * uvPerMeter, size.z * uvPerMeter);

            // 6. -Y Bottom Face -> Cross((0,0,-1), (+1,0,-1)) = (0,-1,0)
            AddFaceQuad(verts, uvs, norms, tris,
                c + new Vector3(-h.x, -h.y,  h.z),
                c + new Vector3(-h.x, -h.y, -h.z),
                c + new Vector3( h.x, -h.y, -h.z),
                c + new Vector3( h.x, -h.y,  h.z),
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


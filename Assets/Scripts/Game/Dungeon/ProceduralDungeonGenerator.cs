using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Phase 6: Seeded Deterministic Dungeon Layout & Pacing Generator + Environmental Events.
    /// Follows `level-design` (sawtooth tension/rest pacing curve) and `procedural-gen` (seeded RNG + graph reachability validation).
    /// </summary>
    public static class ProceduralDungeonGenerator
    {
        public const float GridCellPitch = 34f;

        public struct RoomNodeSpec
        {
            public int index;
            public int gridX;
            public int gridY;
            public RoomType type;
            public DungeonBiome biome;
            public float width;
            public float length;
            public float tensionTarget;
            public bool hasEnvironmentalEvent;
            public string eventTitle;
            public string customTitle;
            public string subTypeLabel;
            public bool isSanctuary;
            public bool openNorth;
            public bool openSouth;
            public bool openWest;
            public bool openEast;
            public int[] connectedIndices;
        }

        /// <summary>
        /// Generates a deterministic 9-room 2D Branching Dungeon Graph (North/South/East/West branches)
        /// using a seed derived from floor + run index:
        /// - (0,0) Entrance Sanctuary
        /// - (0,1) Combat Crossroads -> branches E/W to Treasure/Cursed Altar & Runic Pillar Puzzle
        /// - (0,2) Elite Crossroads -> branches E/W to Healing Shrine Sanctuary & Blood Seal Altar Vault
        /// - (0,3) Mini-Boss Chamber
        /// - (0,4) Final Boss Arena
        /// </summary>
        public static RoomNodeSpec[] GenerateSeededDungeon(int floor, int seedOffset, out bool graphValid)
        {
            int seed = (floor * 7919) ^ (seedOffset * 104729) ^ 0x5A17C3;
            var rng = new System.Random(seed);

            bool flipTier1 = rng.Next(0, 2) == 1;
            bool flipTier2 = rng.Next(0, 2) == 1;

            int treasureGridX = flipTier1 ? 1 : -1;
            int puzzleGridX   = flipTier1 ? -1 : 1;

            int shrineGridX   = flipTier2 ? 1 : -1;
            int bloodSealGridX = flipTier2 ? -1 : 1;

            var specs = new RoomNodeSpec[]
            {
                // 0: (0,0) Entrance
                new RoomNodeSpec
                {
                    index = 0, gridX = 0, gridY = 0,
                    type = RoomType.Entrance, biome = DungeonBiome.ForgottenTemple,
                    width = 24f, length = 24f, tensionTarget = 0.10f,
                    hasEnvironmentalEvent = false, eventTitle = "",
                    customTitle = "Sảnh Khởi Đầu — Cổng Thánh Địa",
                    subTypeLabel = "Khởi Đầu", isSanctuary = true
                },
                // 1: (0,1) Combat Crossroads
                new RoomNodeSpec
                {
                    index = 1, gridX = 0, gridY = 1,
                    type = RoomType.Combat, biome = DungeonBiome.AncientDungeon,
                    width = 24f, length = 24f, tensionTarget = 0.50f,
                    hasEnvironmentalEvent = true, eventTitle = "Tế Đàn Huyết Nguyệt",
                    customTitle = "Ngã Tư Tiền Đồn Cổ Đại",
                    subTypeLabel = "Phòng Quái", isSanctuary = false
                },
                // 2: (-1,1) or (1,1) Side-Branch: Treasure & Cursed Choice Altar
                new RoomNodeSpec
                {
                    index = 2, gridX = treasureGridX, gridY = 1,
                    type = RoomType.Combat, biome = DungeonBiome.AbyssalCave,
                    width = 24f, length = 24f, tensionTarget = 0.65f,
                    hasEnvironmentalEvent = true, eventTitle = "Tế Đàn Nguyền Rủa",
                    customTitle = "Mật Thất Kho Báu & Tế Đàn Nguyền Rủa",
                    subTypeLabel = "Kho Báu / Tế Đàn", isSanctuary = false
                },
                // 3: (1,1) or (-1,1) Side-Branch: Runic Pillar Puzzle Room
                new RoomNodeSpec
                {
                    index = 3, gridX = puzzleGridX, gridY = 1,
                    type = RoomType.Puzzle, biome = DungeonBiome.CursedForest,
                    width = 24f, length = 24f, tensionTarget = 0.45f,
                    hasEnvironmentalEvent = false, eventTitle = "Ấn Phong Cổ Tự",
                    customTitle = "Điện Giải Đố Trụ Cổ Ngữ",
                    subTypeLabel = "Giải Đố Cổ Ngữ", isSanctuary = false
                },
                // 4: (0,2) Elite Crossroads
                new RoomNodeSpec
                {
                    index = 4, gridX = 0, gridY = 2,
                    type = RoomType.Elite, biome = DungeonBiome.UndergroundFortress,
                    width = 24f, length = 24f, tensionTarget = 0.85f,
                    hasEnvironmentalEvent = true, eventTitle = "Lõi Năng Lượng Cổ Đại",
                    customTitle = "Pháo Đài Hộ Vệ Tinh Anh",
                    subTypeLabel = "Quái Tinh Anh", isSanctuary = false
                },
                // 5: (-1,2) or (1,2) Side-Branch: Healing Shrine Sanctuary
                new RoomNodeSpec
                {
                    index = 5, gridX = shrineGridX, gridY = 2,
                    type = RoomType.Entrance, biome = DungeonBiome.ForgottenTemple,
                    width = 24f, length = 24f, tensionTarget = 0.15f,
                    hasEnvironmentalEvent = false, eventTitle = "Đài Hồi Phục Thánh Quang",
                    customTitle = "Thánh Đường Đài Hồi Phục",
                    subTypeLabel = "Đài Hồi Phục", isSanctuary = true
                },
                // 6: (1,2) or (-1,2) Side-Branch: Blood Seal Altar & Vault
                new RoomNodeSpec
                {
                    index = 6, gridX = bloodSealGridX, gridY = 2,
                    type = RoomType.Combat, biome = DungeonBiome.AbyssalCave,
                    width = 24f, length = 24f, tensionTarget = 0.80f,
                    hasEnvironmentalEvent = true, eventTitle = "Phong Ấn Huyết Tinh",
                    customTitle = "Hầm Ngục Phong Ấn Huyết Tinh",
                    subTypeLabel = "Tế Đàn Phong Ấn", isSanctuary = false
                },
                // 7: (0,3) Mini-Boss Chamber
                new RoomNodeSpec
                {
                    index = 7, gridX = 0, gridY = 3,
                    type = RoomType.MiniBoss, biome = DungeonBiome.CrystalChamber,
                    width = 24f, length = 24f, tensionTarget = 0.92f,
                    hasEnvironmentalEvent = false, eventTitle = "",
                    customTitle = "Điện Pha Lê Hộ Pháp (Mini-Boss)",
                    subTypeLabel = "Mini-Boss", isSanctuary = false
                },
                // 8: (0,4) Final Boss Arena
                new RoomNodeSpec
                {
                    index = 8, gridX = 0, gridY = 4,
                    type = RoomType.Boss, biome = DungeonBiome.BossArena,
                    width = 32f, length = 32f, tensionTarget = 1.00f,
                    hasEnvironmentalEvent = false, eventTitle = "",
                    customTitle = "Thánh Điện Vực Thẳm — Abyssal Knight",
                    subTypeLabel = "Boss Cuối", isSanctuary = false
                }
            };

            // Compute N/S/E/W adjacency & doorway flags from 2D grid coordinates
            for (int i = 0; i < specs.Length; i++)
            {
                var conn = new List<int>();
                for (int j = 0; j < specs.Length; j++)
                {
                    if (i == j) continue;
                    int dx = specs[j].gridX - specs[i].gridX;
                    int dy = specs[j].gridY - specs[i].gridY;
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) == 1)
                    {
                        conn.Add(j);
                        if (dx == 0 && dy == 1) specs[i].openNorth = true;
                        else if (dx == 0 && dy == -1) specs[i].openSouth = true;
                        else if (dx == -1 && dy == 0) specs[i].openWest = true;
                        else if (dx == 1 && dy == 0) specs[i].openEast = true;
                    }
                }
                specs[i].connectedIndices = conn.ToArray();
            }

            graphValid = ValidateDungeonGraphReachability(specs);
            return specs;
        }

        /// <summary>
        /// Validates that every room in the branching 2D dungeon graph is reachable from Entrance (0) via BFS.
        /// </summary>
        public static bool ValidateDungeonGraphReachability(RoomNodeSpec[] specs)
        {
            if (specs == null || specs.Length < 2) return false;
            var visited = new bool[specs.Length];
            var queue = new Queue<int>();
            queue.Enqueue(0);
            visited[0] = true;
            int reached = 0;

            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                reached++;
                var neighbors = specs[cur].connectedIndices;
                if (neighbors == null) continue;
                for (int k = 0; k < neighbors.Length; k++)
                {
                    int nb = neighbors[k];
                    if (nb >= 0 && nb < specs.Length && !visited[nb])
                    {
                        visited[nb] = true;
                        queue.Enqueue(nb);
                    }
                }
            }
            return reached == specs.Length && visited[specs.Length - 1];
        }

        public static bool ValidateDungeonGraphReachability(int roomCount)
        {
            return roomCount >= 2;
        }
    }

    /// <summary>
    /// Upgraded 24-segment Curved Perlin-Noise Energy Conduit connecting activated Rune Pillars to the Puzzle Gate:
    /// - 24-segment LineRenderer with smooth catenary arch + real-time dual-frequency PerlinNoise oscillation
    /// - URP Unlit HDR Bloom material (Gold/Cyan gradient) with UV energy flow from source pillar to target gate
    /// - Glowing HDR endpoint orbs at both pillars/gate + streaming energy motes traveling along the curve.
    /// </summary>
    public class RuneSealEnergyBeam : MonoBehaviour
    {
        private const int SegmentCount = 24;
        private static Material _sharedBeamFlowMat;
        private static Material _sharedOrbCoreMat;
        private static Material _sharedOrbHaloMat;
        private static Texture2D _beamFlowTex;

        private LineRenderer _line;
        private Vector3 _startPos;
        private Vector3 _endPos;
        private Vector3 _rightAxis;
        private Vector3 _upAxis;
        private float _distance;
        private float _noiseSeed;
        private Transform _startOrb;
        private Transform _endOrb;
        private Transform[] _travelMotes;
        private float[] _motePhases;
        private readonly Vector3[] _positions = new Vector3[SegmentCount];

        public static void SpawnConnectorBeam(Transform parent, Vector3 fromPos, Vector3 toPos, Color color)
        {
            Vector3 start = fromPos + Vector3.up * 1.92f;
            Vector3 end = toPos + Vector3.up * 2.35f;
            Vector3 diff = end - start;
            float dist = diff.magnitude;
            if (dist < 0.2f) return;

            EnsureSharedMaterials(color);

            var go = new GameObject("RuneEnergyConduit_24Seg");
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = start;

            var comp = go.AddComponent<RuneSealEnergyBeam>();
            comp.Init(start, end, dist, color);
        }

        private static void EnsureSharedMaterials(Color baseGold)
        {
            if (_beamFlowTex == null)
            {
                int w = 64, h = 16;
                _beamFlowTex = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    name = "T_RuneBeamFlow_HDR",
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Bilinear
                };
                var cols = new Color32[w * h];
                for (int y = 0; y < h; y++)
                {
                    float ny = (y / (float)(h - 1)) * 2f - 1f;
                    float edgeFade = Mathf.Clamp01(1f - ny * ny);
                    for (int x = 0; x < w; x++)
                    {
                        float u = x / (float)w;
                        float pulse = 0.55f + 0.45f * Mathf.Sin(u * Mathf.PI * 4f);
                        float core = Mathf.Pow(edgeFade, 1.5f) * pulse;
                        byte cByte = (byte)Mathf.Clamp(Mathf.RoundToInt(core * 255f), 0, 255);
                        cols[y * w + x] = new Color32(255, 255, 255, cByte);
                    }
                }
                _beamFlowTex.SetPixels32(cols);
                _beamFlowTex.Apply(false, false);
            }

            if (_sharedBeamFlowMat == null)
            {
                Shader vfxSh = Shader.Find("AbyssalDungeon/AbyssalVFX_URP")
                               ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                               ?? Shader.Find("Universal Render Pipeline/Unlit");
                _sharedBeamFlowMat = new Material(vfxSh) { name = "M_RuneConduit_HDR" };
                if (_sharedBeamFlowMat.HasProperty("_MainTex")) _sharedBeamFlowMat.SetTexture("_MainTex", _beamFlowTex);
                if (_sharedBeamFlowMat.HasProperty("_BaseMap")) _sharedBeamFlowMat.SetTexture("_BaseMap", _beamFlowTex);
                _sharedBeamFlowMat.mainTexture = _beamFlowTex;

                Color hdrGold = new Color(1.0f, 0.88f, 0.32f, 0.96f);
                Color hdrCyan = new Color(0.24f, 0.96f, 1.0f, 1.0f);
                if (_sharedBeamFlowMat.HasProperty("_MainColor")) _sharedBeamFlowMat.SetColor("_MainColor", hdrGold);
                if (_sharedBeamFlowMat.HasProperty("_BaseColor")) _sharedBeamFlowMat.SetColor("_BaseColor", hdrGold);
                if (_sharedBeamFlowMat.HasProperty("_FresnelColor")) _sharedBeamFlowMat.SetColor("_FresnelColor", hdrCyan);
                if (_sharedBeamFlowMat.HasProperty("_EmissionIntensity")) _sharedBeamFlowMat.SetFloat("_EmissionIntensity", 3.4f);
                if (_sharedBeamFlowMat.HasProperty("_ScrollSpeedX")) _sharedBeamFlowMat.SetFloat("_ScrollSpeedX", -2.6f);
                if (_sharedBeamFlowMat.HasProperty("_ScrollSpeedY")) _sharedBeamFlowMat.SetFloat("_ScrollSpeedY", 0.0f);
                if (_sharedBeamFlowMat.HasProperty("_DstBlend")) _sharedBeamFlowMat.SetFloat("_DstBlend", 1f); // Additive HDR Bloom
            }

            if (_sharedOrbCoreMat == null)
            {
                _sharedOrbCoreMat = CharacterVisualFactory.MakeTransparentMat(
                    new Color(1.0f, 0.92f, 0.45f, 0.92f),
                    new Color(1.0f, 0.85f, 0.30f) * 3.2f);
            }

            if (_sharedOrbHaloMat == null)
            {
                _sharedOrbHaloMat = CharacterVisualFactory.MakeTransparentMat(
                    new Color(0.25f, 0.95f, 0.98f, 0.48f),
                    new Color(0.22f, 0.90f, 1.0f) * 2.4f);
            }
        }

        private void Init(Vector3 start, Vector3 end, float dist, Color color)
        {
            _startPos = start;
            _endPos = end;
            _distance = dist;
            _noiseSeed = UnityEngine.Random.Range(0f, 100f);

            Vector3 fwd = (end - start).normalized;
            _rightAxis = Vector3.Cross(Vector3.up, fwd);
            if (_rightAxis.sqrMagnitude < 0.001f) _rightAxis = Vector3.right;
            _rightAxis.Normalize();
            _upAxis = Vector3.Cross(fwd, _rightAxis).normalized;

            _line = gameObject.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = SegmentCount;
            _line.sharedMaterial = _sharedBeamFlowMat;
            _line.textureMode = LineTextureMode.Tile;
            _line.alignment = LineAlignment.View;
            _line.numCapVertices = 4;
            _line.numCornerVertices = 4;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;

            // Tapered width curve: crisp endpoints, fuller pulsing mid-conduit
            var widthCurve = new AnimationCurve(
                new Keyframe(0.0f, 0.10f),
                new Keyframe(0.15f, 0.22f),
                new Keyframe(0.50f, 0.28f),
                new Keyframe(0.85f, 0.22f),
                new Keyframe(1.0f, 0.12f));
            _line.widthCurve = widthCurve;

            // Gold -> Radiant Cyan-Gold -> Solar Gold gradient along the beam
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1.0f, 0.86f, 0.28f), 0.0f),
                    new GradientColorKey(new Color(0.32f, 0.98f, 0.92f), 0.45f),
                    new GradientColorKey(new Color(1.0f, 0.92f, 0.38f), 1.0f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0.0f),
                    new GradientAlphaKey(1.00f, 0.5f),
                    new GradientAlphaKey(0.95f, 1.0f)
                });
            _line.colorGradient = grad;

            // Endpoint glowing orbs (source pillar & target gate)
            _startOrb = CreateEndpointOrb("SourcePillarOrb", _startPos, 0.34f);
            _endOrb = CreateEndpointOrb("TargetGateOrb", _endPos, 0.40f);

            // Lightweight streaming energy motes traveling along the curve from pillar to gate
            int moteCount = 6;
            _travelMotes = new Transform[moteCount];
            _motePhases = new float[moteCount];
            for (int i = 0; i < moteCount; i++)
            {
                _motePhases[i] = i / (float)moteCount;
                var mote = CharacterVisualFactory.Prim(
                    PrimitiveType.Sphere, "ConduitMote_" + i, transform,
                    Vector3.zero, Vector3.one * 0.14f, i % 2 == 0 ? _sharedOrbCoreMat : _sharedOrbHaloMat);
                var r = mote.GetComponent<Renderer>();
                if (r != null)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }
                _travelMotes[i] = mote.transform;
            }

            // Also attach a lightweight ParticleSystem streaming sparkles along the beam
            BuildBeamParticleStream(start, end, dist);
            UpdateBeamCurve(0f);
        }

        private Transform CreateEndpointOrb(string name, Vector3 worldPos, float baseSize)
        {
            var root = new GameObject(name).transform;
            root.SetParent(transform, false);
            root.position = worldPos;

            var core = CharacterVisualFactory.Prim(PrimitiveType.Sphere, "Core", root, Vector3.zero, Vector3.one * baseSize, _sharedOrbCoreMat);
            var halo = CharacterVisualFactory.Prim(PrimitiveType.Sphere, "Halo", root, Vector3.zero, Vector3.one * (baseSize * 1.65f), _sharedOrbHaloMat);
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            return root;
        }

        private void BuildBeamParticleStream(Vector3 start, Vector3 end, float dist)
        {
            var psGo = new GameObject("BeamEnergyParticles");
            psGo.transform.SetParent(transform, false);
            psGo.transform.position = start;
            psGo.transform.rotation = Quaternion.LookRotation((end - start).normalized);

            var ps = psGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 24;
            float speed = Mathf.Clamp(dist * 0.85f, 4.5f, 12f);
            main.startSpeed = speed;
            main.startLifetime = Mathf.Clamp(dist / speed, 0.45f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1.0f, 0.90f, 0.35f, 0.95f),
                new Color(0.30f, 0.96f, 1.0f, 0.95f));

            var em = ps.emission;
            em.rateOverTime = 12f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 2.5f;
            shape.radius = 0.08f;

            var psr = psGo.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = PbrMaterialLibrary.SoftAdditiveParticleMaterial();
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;
        }

        private void Update()
        {
            UpdateBeamCurve(Time.time);
        }

        private void UpdateBeamCurve(float t)
        {
            if (_line == null) return;

            float archHeight = Mathf.Clamp(_distance * 0.14f, 0.55f, 2.2f);
            float waveAmp = Mathf.Clamp(_distance * 0.045f, 0.14f, 0.48f);

            for (int i = 0; i < SegmentCount; i++)
            {
                float u = i / (float)(SegmentCount - 1);
                Vector3 basePt = Vector3.Lerp(_startPos, _endPos, u);

                // Smooth envelope = 0 at both endpoints, 1 at center
                float env = Mathf.Sin(u * Mathf.PI);

                // Graceful upward catenary arch
                float arch = env * archHeight;

                // Dual-frequency Perlin noise oscillation traveling from source pillar (u=0) to gate (u=1)
                float nX = (Mathf.PerlinNoise(_noiseSeed + u * 3.4f - t * 1.85f, t * 0.95f) - 0.5f) * 2f;
                float nY = (Mathf.PerlinNoise(_noiseSeed + 31.7f + u * 3.8f - t * 2.15f, t * 1.15f) - 0.5f) * 2f;
                float harmonic = Mathf.Sin((u * 2.5f - t * 1.6f) * Mathf.PI * 2f) * 0.35f;

                Vector3 offset = _upAxis * (arch + (nY + harmonic) * waveAmp * env)
                               + _rightAxis * (nX * waveAmp * env);

                _positions[i] = basePt + offset;
            }

            _line.SetPositions(_positions);

            // Scroll UV along the LineRenderer if using standard material property
            if (_sharedBeamFlowMat != null)
            {
                Vector2 texOff = new Vector2(-t * 2.2f, 0f);
                _sharedBeamFlowMat.mainTextureOffset = texOff;
                if (_sharedBeamFlowMat.HasProperty("_BaseMap"))
                    _sharedBeamFlowMat.SetTextureOffset("_BaseMap", texOff);
            }

            // Pulse endpoint orbs
            float pulseA = 1f + Mathf.Sin(t * 5.2f + _noiseSeed) * 0.16f;
            float pulseB = 1f + Mathf.Cos(t * 4.8f + _noiseSeed) * 0.16f;
            if (_startOrb != null) _startOrb.localScale = Vector3.one * pulseA;
            if (_endOrb != null) _endOrb.localScale = Vector3.one * pulseB;

            // Move streaming energy motes along the 24-segment curve
            if (_travelMotes != null)
            {
                for (int i = 0; i < _travelMotes.Length; i++)
                {
                    Transform m = _travelMotes[i];
                    if (m == null) continue;
                    float u = Mathf.Repeat(_motePhases[i] + t * 0.58f, 1f);
                    float fIdx = u * (SegmentCount - 1);
                    int idx0 = Mathf.Clamp(Mathf.FloorToInt(fIdx), 0, SegmentCount - 1);
                    int idx1 = Mathf.Min(SegmentCount - 1, idx0 + 1);
                    m.position = Vector3.Lerp(_positions[idx0], _positions[idx1], fIdx - idx0);
                    float s = 0.11f + 0.07f * Mathf.Sin(u * Mathf.PI);
                    m.localScale = Vector3.one * s;
                }
            }
        }
    }
}

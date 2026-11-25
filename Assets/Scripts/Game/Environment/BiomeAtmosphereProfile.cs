using System;
using UnityEngine;
using UnityEngine.Rendering;
using AbyssalDungeon.VFX;

namespace AbyssGame
{
    /// <summary>
    /// All 7 Dark Fantasy environments specified in Master Prompt Section 2.1.
    /// </summary>
    public enum DungeonBiome
    {
        ForgottenTemple = 0,
        AncientDungeon = 1,
        UndergroundFortress = 2,
        AbyssalCave = 3,
        CrystalChamber = 4,
        CursedForest = 5,
        BossArena = 6
    }

    [Serializable]
    public struct BiomeAtmosphereData
    {
        public DungeonBiome biome;
        public string vietnameseTitle;
        public string ambienceClipName;

        // Trilight Ambient & Fog
        public Color skyAmbient;
        public Color equatorAmbient;
        public Color groundAmbient;
        public Color fogColor;
        public float fogStart;
        public float fogEnd;
        public Color cameraBgColor;

        // Main Directional Rim / Shaft Light
        public Color sunColor;
        public float sunIntensity;
        public Vector3 sunEuler;

        // Torches, Mist & Floating Environmental Particles
        public Color torchLightColor;
        public Color torchCoreColor;
        public Color mistColor;
        public Color particleColor;
        public float particleRiseSpeed;
        public float particleSize;

        // Material Tints
        public Color wallStoneTint;
        public Color floorStoneTint;
        public bool preferMossyStone;
    }

    public static class BiomeAtmosphereLibrary
    {
        public static BiomeAtmosphereData Get(DungeonBiome biome)
        {
            return GetBuiltIn(biome);
        }

        public static DungeonBiome BiomeForDungeonRoom(int roomIndex, RoomType type)
        {
            if (type == RoomType.Boss) return DungeonBiome.BossArena;
            switch (roomIndex)
            {
                case 0: return DungeonBiome.ForgottenTemple;
                case 1: return DungeonBiome.AncientDungeon;
                case 2: return DungeonBiome.UndergroundFortress;
                case 3: return DungeonBiome.AbyssalCave;
                case 4: return DungeonBiome.CrystalChamber;
                case 5: return DungeonBiome.CursedForest;
                default: return DungeonBiome.BossArena;
            }
        }

        public static BiomeAtmosphereData GetBuiltIn(DungeonBiome biome)
        {
            // Clean neutral key/ambient light + warm medieval torchlight across all biomes so characters & stone walls
            // retain crisp, natural colors with strong figure-ground separation (zero purple/green/cyan skin wash!).
            Color warmTorchLight = new Color(1.00f, 0.64f, 0.26f);
            Color warmTorchCore = new Color(1.00f, 0.90f, 0.56f);

            switch (biome)
            {
                case DungeonBiome.ForgottenTemple:
                    return new BiomeAtmosphereData
                    {
                        biome = biome,
                        vietnameseTitle = "Đền Thờ Lãng Quên (Forgotten Temple)",
                        ambienceClipName = "amb_forgotten_temple",
                        skyAmbient = new Color(0.42f, 0.42f, 0.46f),
                        equatorAmbient = new Color(0.32f, 0.30f, 0.32f),
                        groundAmbient = new Color(0.21f, 0.20f, 0.20f),
                        fogColor = new Color(0.09f, 0.10f, 0.12f),
                        fogStart = 32f,
                        fogEnd = 96f,
                        cameraBgColor = new Color(0.05f, 0.06f, 0.08f),
                        sunColor = new Color(0.98f, 0.94f, 0.88f),
                        sunIntensity = 1.44f,
                        sunEuler = new Vector3(48f, -38f, 0f),
                        torchLightColor = warmTorchLight,
                        torchCoreColor = warmTorchCore,
                        mistColor = new Color(0.38f, 0.40f, 0.46f, 0.18f),
                        particleColor = new Color(0.95f, 0.86f, 0.68f, 0.42f),
                        particleRiseSpeed = 0.18f,
                        particleSize = 0.09f,
                        wallStoneTint = new Color(0.92f, 0.89f, 0.85f),
                        floorStoneTint = new Color(0.92f, 0.89f, 0.85f),
                        preferMossyStone = false
                    };

                case DungeonBiome.AncientDungeon:
                    return new BiomeAtmosphereData
                    {
                        biome = biome,
                        vietnameseTitle = "Ngục Cổ Đá Đen (Ancient Dungeon)",
                        ambienceClipName = "amb_ancient_dungeon",
                        skyAmbient = new Color(0.38f, 0.39f, 0.44f),
                        equatorAmbient = new Color(0.30f, 0.29f, 0.32f),
                        groundAmbient = new Color(0.20f, 0.19f, 0.20f),
                        fogColor = new Color(0.08f, 0.09f, 0.11f),
                        fogStart = 32f,
                        fogEnd = 94f,
                        cameraBgColor = new Color(0.04f, 0.05f, 0.07f),
                        sunColor = new Color(0.96f, 0.95f, 0.94f),
                        sunIntensity = 1.40f,
                        sunEuler = new Vector3(50f, -32f, 0f),
                        torchLightColor = warmTorchLight,
                        torchCoreColor = warmTorchCore,
                        mistColor = new Color(0.34f, 0.36f, 0.42f, 0.18f),
                        particleColor = new Color(0.88f, 0.84f, 0.76f, 0.38f),
                        particleRiseSpeed = 0.14f,
                        particleSize = 0.08f,
                        wallStoneTint = new Color(0.90f, 0.89f, 0.90f),
                        floorStoneTint = new Color(0.88f, 0.87f, 0.88f),
                        preferMossyStone = false
                    };

                case DungeonBiome.UndergroundFortress:
                    return new BiomeAtmosphereData
                    {
                        biome = biome,
                        vietnameseTitle = "Pháo Đài Ngầm Sắt Nung (Underground Fortress)",
                        ambienceClipName = "amb_underground_fortress",
                        skyAmbient = new Color(0.30f, 0.28f, 0.28f),
                        equatorAmbient = new Color(0.23f, 0.21f, 0.21f),
                        groundAmbient = new Color(0.12f, 0.10f, 0.10f),
                        fogColor = new Color(0.09f, 0.08f, 0.08f),
                        fogStart = 34f,
                        fogEnd = 100f,
                        cameraBgColor = new Color(0.06f, 0.05f, 0.05f),
                        sunColor = new Color(0.97f, 0.92f, 0.88f),
                        sunIntensity = 1.22f,
                        sunEuler = new Vector3(46f, -28f, 0f),
                        torchLightColor = new Color(1.00f, 0.58f, 0.22f),
                        torchCoreColor = warmTorchCore,
                        mistColor = new Color(0.40f, 0.34f, 0.30f, 0.18f),
                        particleColor = new Color(1.00f, 0.62f, 0.24f, 0.60f),
                        particleRiseSpeed = 0.55f,
                        particleSize = 0.075f,
                        wallStoneTint = new Color(0.80f, 0.78f, 0.77f),
                        floorStoneTint = new Color(0.70f, 0.68f, 0.68f),
                        preferMossyStone = false
                    };

                case DungeonBiome.AbyssalCave:
                    return new BiomeAtmosphereData
                    {
                        biome = biome,
                        vietnameseTitle = "Hang Động Vực Thẳm Cổ Ngữ (Abyssal Cave)",
                        ambienceClipName = "amb_abyssal_cave",
                        skyAmbient = new Color(0.29f, 0.29f, 0.34f),
                        equatorAmbient = new Color(0.22f, 0.22f, 0.25f),
                        groundAmbient = new Color(0.11f, 0.10f, 0.12f),
                        fogColor = new Color(0.07f, 0.07f, 0.10f),
                        fogStart = 35f,
                        fogEnd = 104f,
                        cameraBgColor = new Color(0.05f, 0.05f, 0.08f),
                        sunColor = new Color(0.95f, 0.94f, 0.96f),
                        sunIntensity = 1.22f,
                        sunEuler = new Vector3(48f, -40f, 0f),
                        torchLightColor = warmTorchLight,
                        torchCoreColor = warmTorchCore,
                        mistColor = new Color(0.36f, 0.34f, 0.44f, 0.18f),
                        particleColor = new Color(0.76f, 0.58f, 1.00f, 0.52f),
                        particleRiseSpeed = 0.26f,
                        particleSize = 0.085f,
                        wallStoneTint = new Color(0.82f, 0.80f, 0.83f),
                        floorStoneTint = new Color(0.72f, 0.71f, 0.74f),
                        preferMossyStone = false
                    };

                case DungeonBiome.CrystalChamber:
                    return new BiomeAtmosphereData
                    {
                        biome = biome,
                        vietnameseTitle = "Điện Tinh Thể Aether (Crystal Chamber)",
                        ambienceClipName = "amb_crystal_chamber",
                        skyAmbient = new Color(0.29f, 0.32f, 0.36f),
                        equatorAmbient = new Color(0.22f, 0.24f, 0.26f),
                        groundAmbient = new Color(0.11f, 0.12f, 0.13f),
                        fogColor = new Color(0.07f, 0.09f, 0.12f),
                        fogStart = 36f,
                        fogEnd = 108f,
                        cameraBgColor = new Color(0.04f, 0.06f, 0.09f),
                        sunColor = new Color(0.94f, 0.96f, 0.98f),
                        sunIntensity = 1.24f,
                        sunEuler = new Vector3(50f, -35f, 0f),
                        torchLightColor = warmTorchLight,
                        torchCoreColor = warmTorchCore,
                        mistColor = new Color(0.34f, 0.44f, 0.52f, 0.18f),
                        particleColor = new Color(0.45f, 0.90f, 1.00f, 0.55f),
                        particleRiseSpeed = 0.30f,
                        particleSize = 0.085f,
                        wallStoneTint = new Color(0.82f, 0.84f, 0.86f),
                        floorStoneTint = new Color(0.73f, 0.75f, 0.78f),
                        preferMossyStone = false
                    };

                case DungeonBiome.CursedForest:
                    return new BiomeAtmosphereData
                    {
                        biome = biome,
                        vietnameseTitle = "Thánh Điện Rừng Nguyền Rủa (Cursed Forest)",
                        ambienceClipName = "amb_cursed_forest",
                        skyAmbient = new Color(0.29f, 0.32f, 0.30f),
                        equatorAmbient = new Color(0.22f, 0.24f, 0.22f),
                        groundAmbient = new Color(0.11f, 0.12f, 0.11f),
                        fogColor = new Color(0.07f, 0.09f, 0.08f),
                        fogStart = 35f,
                        fogEnd = 104f,
                        cameraBgColor = new Color(0.04f, 0.06f, 0.05f),
                        sunColor = new Color(0.95f, 0.96f, 0.92f),
                        sunIntensity = 1.22f,
                        sunEuler = new Vector3(46f, -30f, 0f),
                        torchLightColor = warmTorchLight,
                        torchCoreColor = warmTorchCore,
                        mistColor = new Color(0.32f, 0.40f, 0.35f, 0.18f),
                        particleColor = new Color(0.55f, 0.95f, 0.62f, 0.50f),
                        particleRiseSpeed = 0.22f,
                        particleSize = 0.09f,
                        wallStoneTint = new Color(0.82f, 0.84f, 0.80f),
                        floorStoneTint = new Color(0.74f, 0.76f, 0.72f),
                        preferMossyStone = true
                    };

                default: // BossArena
                    return new BiomeAtmosphereData
                    {
                        biome = DungeonBiome.BossArena,
                        vietnameseTitle = "Đại Thánh Đường Thẳm Sâu (Boss Arena)",
                        ambienceClipName = "amb_boss_arena",
                        skyAmbient = new Color(0.30f, 0.29f, 0.33f),
                        equatorAmbient = new Color(0.23f, 0.22f, 0.25f),
                        groundAmbient = new Color(0.11f, 0.10f, 0.12f),
                        fogColor = new Color(0.08f, 0.07f, 0.10f),
                        fogStart = 38f,
                        fogEnd = 115f,
                        cameraBgColor = new Color(0.05f, 0.04f, 0.07f),
                        sunColor = new Color(0.96f, 0.94f, 0.94f),
                        sunIntensity = 1.25f,
                        sunEuler = new Vector3(46f, -36f, 0f),
                        torchLightColor = warmTorchLight,
                        torchCoreColor = warmTorchCore,
                        mistColor = new Color(0.38f, 0.32f, 0.42f, 0.18f),
                        particleColor = new Color(0.95f, 0.68f, 0.36f, 0.58f),
                        particleRiseSpeed = 0.42f,
                        particleSize = 0.09f,
                        wallStoneTint = new Color(0.82f, 0.80f, 0.82f),
                        floorStoneTint = new Color(0.72f, 0.70f, 0.73f),
                        preferMossyStone = false
                    };
            }
        }
    }

    /// <summary>
    /// Smoothly drives Trilight Ambient, Volumetric-style Linear Fog, Directional Shadow Light,
    /// Environmental Particle Emitter, and Spatial Biome Audio Loops across room transitions.
    /// </summary>
    public class BiomeAtmosphereController : MonoBehaviour
    {
        public static BiomeAtmosphereController Instance { get; private set; }
        public DungeonBiome CurrentBiome { get; private set; }

        private BiomeAtmosphereData _from;
        private BiomeAtmosphereData _target;
        private float _blend = 1f;
        private Light _dirLight;
        private GameObject _envAtmoRoot;
        private ParticleSystem _envParticles;
        private ParticleSystem[] _envAtmoSystems;

        public static BiomeAtmosphereController EnsureOnScene(Transform parent = null)
        {
            if (Instance != null) return Instance;
            var go = new GameObject("BiomeAtmosphereController");
            if (parent != null) go.transform.SetParent(parent, false);
            Instance = go.AddComponent<BiomeAtmosphereController>();
            Instance.InitSystem();
            return Instance;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void InitSystem()
        {
            var preset = GraphicsManager.Instance != null ? GraphicsManager.Instance.ActivePreset : null;
            if (preset != null)
            {
                QualitySettings.shadows = preset.enableMainLightShadows
                    ? (preset.softShadows ? ShadowQuality.All : ShadowQuality.HardOnly)
                    : ShadowQuality.Disable;
                QualitySettings.shadowDistance = preset.shadowDistance;
            }
            else
            {
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowResolution = UnityEngine.ShadowResolution.VeryHigh;
                QualitySettings.shadowDistance = Mathf.Max(50f, QualitySettings.shadowDistance);
            }

            foreach (var l in FindObjectsOfType<Light>())
            {
                if (l.type == LightType.Directional)
                {
                    _dirLight = l;
                    break;
                }
            }

            if (_dirLight == null)
            {
                var dlGo = new GameObject("MainDirectionalShadowLight");
                dlGo.transform.SetParent(transform, false);
                dlGo.transform.rotation = Quaternion.Euler(50f, -34f, 0f);
                _dirLight = dlGo.AddComponent<Light>();
                _dirLight.type = LightType.Directional;
            }

            if (_dirLight != null)
            {
                RenderSettings.sun = _dirLight;
                if (preset != null)
                {
                    _dirLight.shadows = preset.enableMainLightShadows
                        ? (preset.softShadows ? LightShadows.Soft : LightShadows.Hard)
                        : LightShadows.None;
                    _dirLight.shadowResolution = preset.mainLightShadowResolution >= 4096
                        ? LightShadowResolution.VeryHigh
                        : (preset.mainLightShadowResolution >= 2048 ? LightShadowResolution.High : LightShadowResolution.Medium);
                }
                else
                {
                    _dirLight.shadows = LightShadows.Soft;
                    _dirLight.shadowResolution = LightShadowResolution.VeryHigh;
                }
                _dirLight.shadowStrength = 0.95f;
                _dirLight.shadowBias = 0.015f;
                _dirLight.shadowNormalBias = 0.20f;
            }

            BuildEnvironmentalParticleEmitter();
            if (GraphicsManager.Instance != null)
                GraphicsManager.Instance.ApplyActivePresetToCurrentScene();
        }

        public void ApplyBiome(DungeonBiome biome, bool instant = false)
        {
            var next = BiomeAtmosphereLibrary.Get(biome);
            CurrentBiome = biome;

            if (instant)
            {
                _from = next;
                _target = next;
                _blend = 1f;
                ApplyInterpolated(next);
            }
            else
            {
                _from = LerpData(_from, _target, _blend);
                _target = next;
                _blend = 0f;
            }

            UpdateParticleStyle(next);
            if (GameAudio.I != null && !string.IsNullOrEmpty(next.ambienceClipName))
                GameAudio.I.PlayAmbience(next.ambienceClipName);
        }

        private void Update()
        {
            if (_blend < 1f)
            {
                _blend = Mathf.Clamp01(_blend + Time.deltaTime * 0.75f);
                ApplyInterpolated(LerpData(_from, _target, Mathf.SmoothStep(0f, 1f, _blend)));
            }

            if (_envAtmoRoot != null && Camera.main != null)
            {
                Vector3 cp = Camera.main.transform.position + Camera.main.transform.forward * 7f;
                _envAtmoRoot.transform.position = new Vector3(cp.x, Mathf.Max(1.8f, cp.y), cp.z);
            }
        }

        private void ApplyInterpolated(BiomeAtmosphereData d)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = d.skyAmbient;
            RenderSettings.ambientEquatorColor = d.equatorAmbient;
            RenderSettings.ambientGroundColor = d.groundAmbient;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = d.fogColor;
            RenderSettings.fogStartDistance = d.fogStart;
            RenderSettings.fogEndDistance = d.fogEnd;

            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = d.cameraBgColor;
            }

            if (_dirLight != null)
            {
                _dirLight.color = d.sunColor;
                _dirLight.intensity = d.sunIntensity;
                _dirLight.transform.rotation = Quaternion.Euler(d.sunEuler);
            }
        }

        private static BiomeAtmosphereData LerpData(BiomeAtmosphereData a, BiomeAtmosphereData b, float t)
        {
            b.skyAmbient = Color.Lerp(a.skyAmbient, b.skyAmbient, t);
            b.equatorAmbient = Color.Lerp(a.equatorAmbient, b.equatorAmbient, t);
            b.groundAmbient = Color.Lerp(a.groundAmbient, b.groundAmbient, t);
            b.fogColor = Color.Lerp(a.fogColor, b.fogColor, t);
            b.fogStart = Mathf.Lerp(a.fogStart, b.fogStart, t);
            b.fogEnd = Mathf.Lerp(a.fogEnd, b.fogEnd, t);
            b.cameraBgColor = Color.Lerp(a.cameraBgColor, b.cameraBgColor, t);
            b.sunColor = Color.Lerp(a.sunColor, b.sunColor, t);
            b.sunIntensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t);
            b.sunEuler = Vector3.Lerp(a.sunEuler, b.sunEuler, t);
            return b;
        }

        private void BuildEnvironmentalParticleEmitter()
        {
            var mgr = VFXManager.EnsureInstance();
            if (mgr != null)
            {
                _envAtmoRoot = mgr.SpawnLoopingVFX(VFXType.Env_AtmosphericParticles, transform, Vector3.zero, Quaternion.identity);
                if (_envAtmoRoot != null)
                {
                    _envAtmoRoot.name = "AmbientDustEmberParticles";
                    _envAtmoSystems = _envAtmoRoot.GetComponentsInChildren<ParticleSystem>(true);
                    if (_envAtmoSystems != null && _envAtmoSystems.Length > 0)
                        _envParticles = _envAtmoSystems[0];
                    return;
                }
            }

            // Fallback if VFXManager unavailable
            var pGo = new GameObject("AmbientDustEmberParticles");
            pGo.transform.SetParent(transform, false);
            _envAtmoRoot = pGo;
            _envParticles = pGo.AddComponent<ParticleSystem>();

            var main = _envParticles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 6.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.22f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);

            var emission = _envParticles.emission;
            emission.rateOverTime = 20f;

            var shape = _envParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(24f, 7f, 24f);

            var colOverLife = _envParticles.colorOverLifetime;
            colOverLife.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.25f), new GradientAlphaKey(0.85f, 0.75f), new GradientAlphaKey(0f, 1f) }
            );
            colOverLife.color = new ParticleSystem.MinMaxGradient(grad);

            var psr = pGo.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = PbrMaterialLibrary.SoftAdditiveParticleMaterial();
            psr.shadowCastingMode = ShadowCastingMode.Off;
            psr.receiveShadows = false;
            _envAtmoSystems = new[] { _envParticles };
        }

        private void UpdateParticleStyle(BiomeAtmosphereData d)
        {
            if (_envAtmoSystems == null) return;
            for (int i = 0; i < _envAtmoSystems.Length; i++)
            {
                var ps = _envAtmoSystems[i];
                if (ps == null) continue;
                var main = ps.main;
                main.startColor = i == 0
                    ? d.particleColor
                    : new Color(Mathf.Lerp(d.particleColor.r, 0.55f, 0.5f), Mathf.Lerp(d.particleColor.g, 0.50f, 0.5f), Mathf.Lerp(d.particleColor.b, 0.48f, 0.5f), 0.45f);
                main.startSize = new ParticleSystem.MinMaxCurve(d.particleSize * 0.7f, d.particleSize * 1.45f);

                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                var zeroTwoConst = new ParticleSystem.MinMaxCurve(0f, 0f);
                vel.x = new ParticleSystem.MinMaxCurve(-0.18f, 0.18f);
                vel.y = new ParticleSystem.MinMaxCurve(d.particleRiseSpeed * 0.45f, d.particleRiseSpeed * 1.35f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.18f, 0.18f);
                vel.orbitalX = zeroTwoConst;
                vel.orbitalY = zeroTwoConst;
                vel.orbitalZ = zeroTwoConst;
                vel.orbitalOffsetX = zeroTwoConst;
                vel.orbitalOffsetY = zeroTwoConst;
                vel.orbitalOffsetZ = zeroTwoConst;
                vel.radial = zeroTwoConst;
                vel.speedModifier = new ParticleSystem.MinMaxCurve(1f, 1f);
            }
        }
    }
}


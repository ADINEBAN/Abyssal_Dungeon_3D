using UnityEngine;
using UnityEngine.Rendering;

namespace AbyssGame
{
    /// <summary>
    /// Smoothly drives Trilight Ambient, Volumetric-style Linear Fog, Directional Shadow Light,
    /// Environmental Particle Emitter, and Spatial Biome Audio Loops across room transitions.
    /// </summary>
    public class BiomeAtmosphereController : MonoBehaviour
    {
        public static BiomeAtmosphereController Instance { get; private set; }
        public DungeonBiome CurrentBiome { get; private set; }

        [SerializeField] private DungeonBiome _serializedBiome = DungeonBiome.ForgottenTemple;
        [SerializeField] private Light _dirLight;
        [SerializeField] private ParticleSystem _envParticles;

        private BiomeAtmosphereData _from;
        private BiomeAtmosphereData _target;
        private float _blend = 1f;
        private bool _initialized;

        public static BiomeAtmosphereController EnsureOnScene(Transform parent = null)
        {
            if (Instance != null) return Instance;
            var existing = FindObjectOfType<BiomeAtmosphereController>();
            if (existing != null)
            {
                Instance = existing;
                existing.InitSystem();
                return existing;
            }
            var go = new GameObject("BiomeAtmosphereController");
            if (parent != null) go.transform.SetParent(parent, false);
            Instance = go.AddComponent<BiomeAtmosphereController>();
            Instance.InitSystem();
            return Instance;
        }

        private void Awake()
        {
            Instance = this;
            InitSystem();
            if (_blend >= 1f && _target.vietnameseTitle == null)
                ApplyBiome(_serializedBiome, instant: true);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void InitSystem()
        {
            if (_initialized && _dirLight != null && _envParticles != null) return;
            _initialized = true;
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

            if (_envParticles == null)
                _envParticles = GetComponentInChildren<ParticleSystem>();
            if (_envParticles == null)
                BuildEnvironmentalParticleEmitter();
            if (GraphicsManager.Instance != null)
                GraphicsManager.Instance.ApplyActivePresetToCurrentScene();
        }

        public void ApplyBiome(DungeonBiome biome, bool instant = false)
        {
            var next = BiomeAtmosphereLibrary.Get(biome);
            CurrentBiome = biome;
            _serializedBiome = biome;

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
            if (Application.isPlaying && GameAudio.I != null && !string.IsNullOrEmpty(next.ambienceClipName))
                GameAudio.I.PlayAmbience(next.ambienceClipName);
        }

        private void Update()
        {
            if (_blend < 1f)
            {
                _blend = Mathf.Clamp01(_blend + Time.deltaTime * 0.75f);
                ApplyInterpolated(LerpData(_from, _target, Mathf.SmoothStep(0f, 1f, _blend)));
            }

            if (_envParticles != null && Camera.main != null)
            {
                Vector3 cp = Camera.main.transform.position + Camera.main.transform.forward * 7f;
                _envParticles.transform.position = new Vector3(cp.x, Mathf.Max(1.8f, cp.y), cp.z);
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
            var pGo = new GameObject("AmbientDustEmberParticles");
            pGo.transform.SetParent(transform, false);
            _envParticles = pGo.AddComponent<ParticleSystem>();

            var main = _envParticles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 140;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 6.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.22f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);

            var emission = _envParticles.emission;
            emission.rateOverTime = 24f;

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
        }

        private void UpdateParticleStyle(BiomeAtmosphereData d)
        {
            if (_envParticles == null) return;
            var main = _envParticles.main;
            main.startColor = d.particleColor;
            main.startSize = new ParticleSystem.MinMaxCurve(d.particleSize * 0.65f, d.particleSize * 1.35f);

            var vel = _envParticles.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.18f, 0.18f);
            vel.y = new ParticleSystem.MinMaxCurve(d.particleRiseSpeed * 0.5f, d.particleRiseSpeed * 1.4f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.18f, 0.18f);
        }
    }
}


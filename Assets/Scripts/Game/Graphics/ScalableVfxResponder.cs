using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Component attached to any VFX prefab or pooled VFX instance so it dynamically scales
    /// particle counts, bursts, secondary child layers, collision modules, and realtime lights
    /// according to VfxQualitySystem.
    /// </summary>
    public class ScalableVfxResponder : MonoBehaviour
    {
        public bool isCombatTelegraph = false;

        private ParticleSystem[] _systems;
        private int[] _baseMaxParticles;
        private float[] _baseRateOverTime;
        private ParticleSystem.Burst[][] _baseBursts;
        private bool[] _baseCollisionEnabled;
        private bool[] _baseLightsEnabled;
        private VfxLayerTierFilter[] _tierFilters;
        private Light[] _vfxLights;
        private bool _cached;

        private void Awake()
        {
            CacheBaseValues();
        }

        private void OnEnable()
        {
            CacheBaseValues();
            VfxQualitySystem.RegisterResponder(this);
            if (_systems != null)
            {
                for (int i = 0; i < _systems.Length; i++)
                    VfxQualitySystem.RegisterParticleSystem(_systems[i]);
            }
            ApplyRules(VfxQualitySystem.CurrentRules);
        }

        private void OnDisable()
        {
            VfxQualitySystem.UnregisterResponder(this);
        }

        private void CacheBaseValues()
        {
            if (_cached) return;
            _cached = true;

            _systems = GetComponentsInChildren<ParticleSystem>(true);
            _baseMaxParticles = new int[_systems.Length];
            _baseRateOverTime = new float[_systems.Length];
            _baseBursts = new ParticleSystem.Burst[_systems.Length][];
            _baseCollisionEnabled = new bool[_systems.Length];
            _baseLightsEnabled = new bool[_systems.Length];

            for (int i = 0; i < _systems.Length; i++)
            {
                var ps = _systems[i];
                if (ps == null) continue;
                _baseMaxParticles[i] = Mathf.Max(2, ps.main.maxParticles);
                var em = ps.emission;
                _baseRateOverTime[i] = em.rateOverTimeMultiplier;

                int bCount = em.burstCount;
                if (bCount > 0)
                {
                    var bursts = new ParticleSystem.Burst[bCount];
                    em.GetBursts(bursts);
                    _baseBursts[i] = bursts;
                }

                _baseCollisionEnabled[i] = ps.collision.enabled;
                _baseLightsEnabled[i] = ps.lights.enabled;
            }

            _tierFilters = GetComponentsInChildren<VfxLayerTierFilter>(true);
            _vfxLights = GetComponentsInChildren<Light>(true);
        }

        public void ApplyRules(VfxTierRules rules)
        {
            CacheBaseValues();

            float mult = isCombatTelegraph ? Mathf.Max(0.75f, rules.particleCountMultiplier) : rules.particleCountMultiplier;

            if (_tierFilters != null)
            {
                for (int i = 0; i < _tierFilters.Length; i++)
                {
                    var f = _tierFilters[i];
                    if (f == null || f.gameObject == gameObject) continue;
                    bool shouldActive = f.ShouldBeActive(rules, isCombatTelegraph);
                    if (f.gameObject.activeSelf != shouldActive)
                        f.gameObject.SetActive(shouldActive);
                }
            }

            if (_systems != null)
            {
                for (int i = 0; i < _systems.Length; i++)
                {
                    var ps = _systems[i];
                    if (ps == null) continue;

                    var main = ps.main;
                    main.maxParticles = Mathf.Max(2, Mathf.RoundToInt(_baseMaxParticles[i] * mult));

                    var em = ps.emission;
                    em.rateOverTimeMultiplier = _baseRateOverTime[i] * mult;

                    var baseBurstArr = _baseBursts[i];
                    if (baseBurstArr != null && baseBurstArr.Length > 0)
                    {
                        for (int b = 0; b < baseBurstArr.Length; b++)
                        {
                            var burst = baseBurstArr[b];
                            short minC = (short)Mathf.Max(1, Mathf.RoundToInt(burst.minCount * mult));
                            short maxC = (short)Mathf.Max(minC, Mathf.RoundToInt(burst.maxCount * mult));
                            burst.minCount = minC;
                            burst.maxCount = maxC;
                            em.SetBurst(b, burst);
                        }
                    }

                    var col = ps.collision;
                    col.enabled = _baseCollisionEnabled[i] && rules.enableCollisionParticles;

                    var lightsMod = ps.lights;
                    lightsMod.enabled = _baseLightsEnabled[i] && rules.enableParticleRealtimeLights;
                }
            }

            if (_vfxLights != null)
            {
                for (int i = 0; i < _vfxLights.Length; i++)
                {
                    if (_vfxLights[i] != null)
                    {
                        _vfxLights[i].enabled = rules.enableParticleRealtimeLights;
                        _vfxLights[i].shadows = LightShadows.None;
                    }
                }
            }
        }
    }
}

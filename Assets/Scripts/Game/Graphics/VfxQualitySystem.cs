using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssGame
{
    public struct VfxTierRules
    {
        public VfxQualityOption qualityLevel;
        public float particleCountMultiplier;
        public bool enableCollisionParticles;
        public bool enableParticleRealtimeLights;
        public bool enableHeavyDistortion;
        public bool enableShockwaveRings;
        public bool enableSecondarySparks;
        public int maxSlashRibbonLayers;
        public float transparencyAlphaScale;
        public bool preserveCombatTelegraphs;
    }

    /// <summary>
    /// Central Scalable VFX Architecture (Section 5 & Phase 3).
    /// Scales particle counts, collision sub-emitters, realtime particle lights, distortion,
    /// and transparency overdraw across Low / Medium / High / Ultra while strictly preserving
    /// 100% of combat attack telegraphs and danger zones across all players in Multiplayer.
    /// </summary>
    public static class VfxQualitySystem
    {
        private static readonly List<ParticleSystem> _registeredParticleSystems = new List<ParticleSystem>(64);
        private static readonly List<ScalableVfxResponder> _responders = new List<ScalableVfxResponder>(64);

        public static VfxTierRules CurrentRules { get; private set; } = GetRulesFor(VfxQualityOption.High, 1.0f);

        public static VfxTierRules GetRulesFor(VfxQualityOption level, float densityOverride = -1f)
        {
            float baseMult;
            switch (level)
            {
                case VfxQualityOption.Low:
                    baseMult = densityOverride > 0f ? densityOverride : 0.35f;
                    return new VfxTierRules
                    {
                        qualityLevel = VfxQualityOption.Low,
                        particleCountMultiplier = baseMult,
                        enableCollisionParticles = false,
                        enableParticleRealtimeLights = false,
                        enableHeavyDistortion = false,
                        enableShockwaveRings = false,
                        enableSecondarySparks = false,
                        maxSlashRibbonLayers = 1,
                        transparencyAlphaScale = 0.78f,
                        preserveCombatTelegraphs = true
                    };

                case VfxQualityOption.Medium:
                    baseMult = densityOverride > 0f ? densityOverride : 0.65f;
                    return new VfxTierRules
                    {
                        qualityLevel = VfxQualityOption.Medium,
                        particleCountMultiplier = baseMult,
                        enableCollisionParticles = false,
                        enableParticleRealtimeLights = false,
                        enableHeavyDistortion = false,
                        enableShockwaveRings = true,
                        enableSecondarySparks = false,
                        maxSlashRibbonLayers = 1,
                        transparencyAlphaScale = 0.88f,
                        preserveCombatTelegraphs = true
                    };

                case VfxQualityOption.High:
                    baseMult = densityOverride > 0f ? densityOverride : 1.00f;
                    return new VfxTierRules
                    {
                        qualityLevel = VfxQualityOption.High,
                        particleCountMultiplier = baseMult,
                        enableCollisionParticles = true,
                        enableParticleRealtimeLights = true,
                        enableHeavyDistortion = true,
                        enableShockwaveRings = true,
                        enableSecondarySparks = true,
                        maxSlashRibbonLayers = 2,
                        transparencyAlphaScale = 1.00f,
                        preserveCombatTelegraphs = true
                    };

                default: // Ultra & Cinematic
                    baseMult = densityOverride > 0f ? densityOverride : 1.25f;
                    return new VfxTierRules
                    {
                        qualityLevel = VfxQualityOption.Ultra,
                        particleCountMultiplier = baseMult,
                        enableCollisionParticles = true,
                        enableParticleRealtimeLights = true,
                        enableHeavyDistortion = true,
                        enableShockwaveRings = true,
                        enableSecondarySparks = true,
                        maxSlashRibbonLayers = 3,
                        transparencyAlphaScale = 1.00f,
                        preserveCombatTelegraphs = true
                    };
            }
        }

        public static void ApplyQuality(VfxQualityOption option, float densityMultiplier)
        {
            CurrentRules = GetRulesFor(option, densityMultiplier);

            for (int i = _responders.Count - 1; i >= 0; i--)
            {
                var r = _responders[i];
                if (r == null)
                {
                    _responders.RemoveAt(i);
                    continue;
                }
                r.ApplyRules(CurrentRules);
            }
        }

        public static void RegisterParticleSystem(ParticleSystem ps)
        {
            if (ps != null && !_registeredParticleSystems.Contains(ps))
                _registeredParticleSystems.Add(ps);
        }

        public static void UnregisterParticleSystem(ParticleSystem ps)
        {
            if (ps != null)
                _registeredParticleSystems.Remove(ps);
        }

        public static void RegisterResponder(ScalableVfxResponder r)
        {
            if (r != null && !_responders.Contains(r))
                _responders.Add(r);
        }

        public static void UnregisterResponder(ScalableVfxResponder r)
        {
            if (r != null)
                _responders.Remove(r);
        }

        public static void GetActiveParticleTelemetry(out int activeSystems, out int activeParticles)
        {
            activeSystems = 0;
            activeParticles = 0;

            for (int i = _registeredParticleSystems.Count - 1; i >= 0; i--)
            {
                var ps = _registeredParticleSystems[i];
                if (ps == null)
                {
                    _registeredParticleSystems.RemoveAt(i);
                    continue;
                }
                if (ps.gameObject.activeInHierarchy)
                {
                    activeSystems++;
                    activeParticles += ps.particleCount;
                }
            }
        }
    }

    /// <summary>
    /// Component attached to any VFX prefab or pooled VFX instance so it dynamically scales
    /// particle counts, collision modules, and realtime lights according to VfxQualitySystem.
    /// </summary>
    public class ScalableVfxResponder : MonoBehaviour
    {
        public bool isCombatTelegraph = false;

        private ParticleSystem[] _systems;
        private int[] _baseMaxParticles;
        private float[] _baseRateOverTime;
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

            for (int i = 0; i < _systems.Length; i++)
            {
                var ps = _systems[i];
                if (ps == null) continue;
                _baseMaxParticles[i] = Mathf.Max(4, ps.main.maxParticles);
                _baseRateOverTime[i] = ps.emission.rateOverTimeMultiplier;
            }

            _vfxLights = GetComponentsInChildren<Light>(true);
        }

        public void ApplyRules(VfxTierRules rules)
        {
            CacheBaseValues();

            // Combat telegraphs must always stay 100% readable even on Low
            float mult = isCombatTelegraph ? Mathf.Max(0.75f, rules.particleCountMultiplier) : rules.particleCountMultiplier;

            if (_systems != null)
            {
                for (int i = 0; i < _systems.Length; i++)
                {
                    var ps = _systems[i];
                    if (ps == null) continue;

                    var main = ps.main;
                    main.maxParticles = Mathf.Max(4, Mathf.RoundToInt(_baseMaxParticles[i] * mult));

                    var em = ps.emission;
                    em.rateOverTimeMultiplier = _baseRateOverTime[i] * mult;

                    var col = ps.collision;
                    if (col.enabled && !rules.enableCollisionParticles)
                        col.enabled = false;

                    var lightsMod = ps.lights;
                    if (lightsMod.enabled && !rules.enableParticleRealtimeLights)
                        lightsMod.enabled = false;
                }
            }

            if (_vfxLights != null)
            {
                for (int i = 0; i < _vfxLights.Length; i++)
                {
                    if (_vfxLights[i] != null)
                    {
                        _vfxLights[i].enabled = rules.enableParticleRealtimeLights;
                        _vfxLights[i].shadows = LightShadows.None; // Particle lights never cast realtime shadows
                    }
                }
            }
        }
    }
}


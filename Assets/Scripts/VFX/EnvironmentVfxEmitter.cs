using System.Collections.Generic;
using UnityEngine;
using AbyssGame;

namespace AbyssalDungeon.VFX
{
    /// <summary>
    /// Distance-culled, quality-scalable controller attached to ambient & environmental looping VFX instances.
    /// Zero per-frame allocations: evaluates camera proximity on a staggered interval and switches off
    /// optional environmental layers on Low / Medium presets without ever hiding combat or trap telegraphs.
    /// </summary>
    public class EnvironmentVfxEmitter : MonoBehaviour
    {
        private static readonly List<EnvironmentVfxEmitter> _activeEmitters = new List<EnvironmentVfxEmitter>(128);

        public VfxQualityOption minQualityTier = VfxQualityOption.Low;
        public float distanceMultiplier = 0.88f;
        public bool isCriticalHazardOrTelegraph = false;

        private ParticleSystem[] _systems;
        private Renderer[] _renderers;
        private bool _isPlaying = true;
        private bool _allowedByQuality = true;
        private float _checkTimer;

        private void Awake()
        {
            CacheComponents();
            _checkTimer = Random.Range(0.05f, 0.40f);
        }

        private void OnEnable()
        {
            CacheComponents();
            if (!_activeEmitters.Contains(this))
                _activeEmitters.Add(this);

            VfxQualitySystem.OnVfxRulesApplied -= HandleQualityChanged;
            VfxQualitySystem.OnVfxRulesApplied += HandleQualityChanged;

            EvaluateQualityRule(VfxQualitySystem.CurrentRules);
            EvaluateDistance(forceApply: true);
        }

        private void OnDisable()
        {
            _activeEmitters.Remove(this);
            VfxQualitySystem.OnVfxRulesApplied -= HandleQualityChanged;
        }

        private void CacheComponents()
        {
            if (_systems == null)
                _systems = GetComponentsInChildren<ParticleSystem>(true);
            if (_renderers == null)
                _renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void HandleQualityChanged(VfxTierRules rules)
        {
            EvaluateQualityRule(rules);
            EvaluateDistance(forceApply: true);
        }

        private void EvaluateQualityRule(VfxTierRules rules)
        {
            _allowedByQuality = isCriticalHazardOrTelegraph || rules.qualityLevel >= minQualityTier;
        }

        private void Update()
        {
            _checkTimer -= Time.unscaledDeltaTime;
            if (_checkTimer <= 0f)
            {
                _checkTimer = 0.40f;
                EvaluateDistance(forceApply: false);
            }
        }

        public void EvaluateDistance(bool forceApply)
        {
            bool shouldPlay = _allowedByQuality;
            if (shouldPlay && Camera.main != null)
            {
                float maxDist = Mathf.Max(22f, GraphicsManager.CurrentViewDistanceMeters * distanceMultiplier);
                float sqDist = (Camera.main.transform.position - transform.position).sqrMagnitude;
                if (sqDist > maxDist * maxDist)
                    shouldPlay = false;
            }

            if (!forceApply && shouldPlay == _isPlaying)
                return;

            _isPlaying = shouldPlay;
            ApplyPlaybackState(shouldPlay);
        }

        private void ApplyPlaybackState(bool active)
        {
            if (_renderers != null)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    if (_renderers[i] != null)
                        _renderers[i].enabled = active;
                }
            }

            if (_systems != null)
            {
                for (int i = 0; i < _systems.Length; i++)
                {
                    var ps = _systems[i];
                    if (ps == null) continue;
                    if (active)
                    {
                        if (!ps.isPlaying)
                            ps.Play(true);
                    }
                    else
                    {
                        if (ps.isPlaying)
                            ps.Pause(true);
                    }
                }
            }
        }

        public static void RefreshAllNow()
        {
            for (int i = _activeEmitters.Count - 1; i >= 0; i--)
            {
                var e = _activeEmitters[i];
                if (e == null)
                {
                    _activeEmitters.RemoveAt(i);
                    continue;
                }
                e.EvaluateQualityRule(VfxQualitySystem.CurrentRules);
                e.EvaluateDistance(forceApply: true);
            }
        }
    }
}


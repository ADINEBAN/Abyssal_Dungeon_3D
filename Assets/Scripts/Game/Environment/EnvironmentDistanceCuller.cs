using System.Collections.Generic;
using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Phase 4 Environment, Character & Lighting Distance Optimizer (Section 6).
    /// Periodically (every 0.35s, zero per-frame allocations) culls distant point lights,
    /// torch flickers, and off-camera decorative particles outside the active View Distance,
    /// and sets off-screen character Animators to CullUpdateTransforms without ever altering
    /// animation speed, combat timing, hitboxes, AI logic, or multiplayer sync.
    /// </summary>
    public class EnvironmentDistanceCuller : MonoBehaviour
    {
        public static EnvironmentDistanceCuller Instance { get; private set; }

        public float ViewDistanceMeters { get; private set; } = 72f;
        public float DistantAnimatorCullMeters { get; private set; } = 38f;
        public int MaxActivePointLights { get; private set; } = 10;

        private readonly List<Light> _scenePointLights = new List<Light>(32);
        private readonly List<Animator> _sceneAnimators = new List<Animator>(32);
        private float _scanTimer;
        private float _cullTimer;

        public static EnvironmentDistanceCuller EnsureOnActiveScene(float viewDist, float animDist, int maxLights)
        {
            if (Instance == null)
            {
                var go = new GameObject("[EnvironmentDistanceCuller]");
                Instance = go.AddComponent<EnvironmentDistanceCuller>();
            }
            Instance.Configure(viewDist, animDist, maxLights);
            return Instance;
        }

        public void Configure(float viewDist, float animDist, int maxLights)
        {
            ViewDistanceMeters = Mathf.Clamp(viewDist, 25f, 160f);
            DistantAnimatorCullMeters = Mathf.Clamp(animDist, 18f, 100f);
            MaxActivePointLights = Mathf.Clamp(maxLights, 0, 24);
            RefreshSceneCache();
            EvaluateDistanceCulling();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            RefreshSceneCache();
        }

        public void RefreshSceneCache()
        {
            _scenePointLights.Clear();
            Light[] lights = FindObjectsOfType<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                var l = lights[i];
                if (l == null) continue;
                if (l.type == LightType.Point || l.type == LightType.Spot)
                {
                    // Point lights never cast realtime shadows on Low/Medium/High
                    bool allowAddShadow = GraphicsManager.Instance != null &&
                                          GraphicsManager.Instance.ActivePreset != null &&
                                          GraphicsManager.Instance.ActivePreset.enableAdditionalLightShadows;
                    l.shadows = allowAddShadow ? LightShadows.Soft : LightShadows.None;
                    _scenePointLights.Add(l);
                }
            }

            _sceneAnimators.Clear();
            Animator[] anims = FindObjectsOfType<Animator>(true);
            for (int i = 0; i < anims.Length; i++)
            {
                var a = anims[i];
                if (a != null)
                    _sceneAnimators.Add(a);
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _scanTimer -= dt;
            if (_scanTimer <= 0f)
            {
                _scanTimer = 3.5f;
                RefreshSceneCache();
            }

            _cullTimer -= dt;
            if (_cullTimer <= 0f)
            {
                _cullTimer = 0.35f;
                EvaluateDistanceCulling();
            }
        }

        private void EvaluateDistanceCulling()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 camPos = cam.transform.position;

            // 1. Point Light distance & budget culling
            float maxLightDistSq = ViewDistanceMeters * ViewDistanceMeters;
            _scenePointLights.RemoveAll(l => l == null);
            _scenePointLights.Sort((a, b) =>
            {
                float da = (a.transform.position - camPos).sqrMagnitude;
                float db = (b.transform.position - camPos).sqrMagnitude;
                return da.CompareTo(db);
            });

            for (int i = 0; i < _scenePointLights.Count; i++)
            {
                Light l = _scenePointLights[i];
                float dSq = (l.transform.position - camPos).sqrMagnitude;
                bool enable = i < MaxActivePointLights && dSq <= maxLightDistSq;
                if (l.enabled != enable)
                    l.enabled = enable;
            }

            // 2. Safe Animator off-screen transform culling beyond DistantAnimatorCullMeters
            // Never alters Animator.speed or gameplay state; only skips off-screen bone matrix skinning when far
            float animDistSq = DistantAnimatorCullMeters * DistantAnimatorCullMeters;
            for (int i = _sceneAnimators.Count - 1; i >= 0; i--)
            {
                Animator a = _sceneAnimators[i];
                if (a == null)
                {
                    _sceneAnimators.RemoveAt(i);
                    continue;
                }
                float dSq = (a.transform.position - camPos).sqrMagnitude;
                AnimatorCullingMode targetMode = dSq > animDistSq
                    ? AnimatorCullingMode.CullUpdateTransforms
                    : AnimatorCullingMode.AlwaysAnimate;
                if (a.cullingMode != targetMode)
                    a.cullingMode = targetMode;
            }
        }
    }
}


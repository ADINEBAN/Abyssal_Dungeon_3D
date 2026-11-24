using UnityEngine;

namespace AbyssalDungeon.VFX
{
    /// <summary>
    /// Drives the 3-layer VFX prefab animation (Layer 1: 3D Blender Mesh, Layer 2: Rune/Crack Decal, Layer 3: Capped Particles)
    /// using cached MaterialPropertyBlocks with zero runtime allocations or GC spikes.
    /// </summary>
    [DisallowMultipleComponent]
    public class VFXAnimatedLayerController : MonoBehaviour
    {
        [Header("Lifecycle & Pool Identity")]
        public string poolKey;
        public float duration = 1.25f;
        public bool isLooping = false;
        public VFXAnimationStyle animationStyle = VFXAnimationStyle.HolyPillar;

        [Header("Layer 1: 3D Mesh Layer")]
        public Transform layer1PrimaryMesh;
        public Transform layer1SecondaryMesh;
        public Vector3 primaryBaseScale = Vector3.one;
        public Vector3 secondaryBaseScale = Vector3.one;
        public Vector3 primaryRotationSpeed = new Vector3(0f, 90f, 0f);
        public Vector3 secondaryRotationSpeed = new Vector3(0f, -140f, 0f);

        [Header("Layer 2: Ground Rune / Cracks Decal")]
        public Transform layer2DecalTransform;
        public Vector3 decalBaseScale = Vector3.one;
        public float decalSpinSpeed = 35f;

        [Header("Layer 3: Auxiliary Particle System (Max 30-45)")]
        public ParticleSystem layer3Particles;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;
        private float _elapsed;
        private float _scaleMultiplier = 1f;
        private Transform _followTarget;
        private Vector3 _followOffset;
        private bool _fadingOut;
        private float _fadeOutElapsed;
        private const float QuickFadeDuration = 0.28f;

        private static readonly int AlphaMultiplierId = Shader.PropertyToID("_AlphaMultiplier");
        private static readonly int EmissionIntensityId = Shader.PropertyToID("_EmissionIntensity");

        private void Awake()
        {
            CacheComponents();
        }

        public void CacheComponents()
        {
            if (_renderers == null || _renderers.Length == 0)
            {
                _renderers = GetComponentsInChildren<Renderer>(true);
            }
            if (_mpb == null)
            {
                _mpb = new MaterialPropertyBlock();
            }
            if (layer3Particles == null)
            {
                layer3Particles = GetComponentInChildren<ParticleSystem>(true);
            }
        }

        public void OnSpawnedFromPool(float scaleMultiplier, float durationOverride, Transform followTarget, Vector3 followOffset)
        {
            CacheComponents();
            _elapsed = 0f;
            _fadingOut = false;
            _fadeOutElapsed = 0f;
            _scaleMultiplier = Mathf.Max(0.1f, scaleMultiplier);
            _followTarget = followTarget;
            _followOffset = followOffset;
            if (durationOverride > 0.05f)
            {
                duration = durationOverride;
            }

            transform.localScale = Vector3.one * _scaleMultiplier;

            if (layer1PrimaryMesh != null)
            {
                layer1PrimaryMesh.localRotation = Quaternion.identity;
            }
            if (layer1SecondaryMesh != null)
            {
                layer1SecondaryMesh.localRotation = Quaternion.identity;
            }
            if (layer2DecalTransform != null)
            {
                layer2DecalTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            bool isTelegraph = animationStyle == VFXAnimationStyle.GroundTelegraph;
            ApplyShaderProperties(isTelegraph ? 0.92f : 0.56f, isTelegraph ? 1.75f : 1.25f);
            EvaluateAnimation(0f);

            if (layer3Particles != null)
            {
                var main = layer3Particles.main;
                int maxCap = isTelegraph ? 18 : 20;
                if (main.maxParticles > maxCap)
                {
                    main.maxParticles = maxCap;
                }
                layer3Particles.Clear(true);
                layer3Particles.Play(true);
            }
        }

        public void StopAndFadeOut()
        {
            if (_fadingOut) return;
            _fadingOut = true;
            _fadeOutElapsed = 0f;
            if (layer3Particles != null)
            {
                layer3Particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _elapsed += dt;

            if (_followTarget != null)
            {
                transform.position = _followTarget.position + _followOffset;
            }

            if (_fadingOut)
            {
                _fadeOutElapsed += dt;
                float fadeT = Mathf.Clamp01(_fadeOutElapsed / QuickFadeDuration);
                ApplyShaderProperties(1f - fadeT, Mathf.Lerp(2.4f, 0.5f, fadeT));
                if (fadeT >= 1f)
                {
                    ReturnToPool();
                }
                return;
            }

            float normalizedTime = duration > 0.001f ? Mathf.Clamp01(_elapsed / duration) : 0f;
            EvaluateAnimation(normalizedTime);

            if (!isLooping && _elapsed >= duration)
            {
                ReturnToPool();
            }
        }

        private void EvaluateAnimation(float t)
        {
            float dt = Time.deltaTime;

            bool isTelegraph = animationStyle == VFXAnimationStyle.GroundTelegraph;
            float baseAlphaCap = isTelegraph ? 0.92f : 0.56f;
            float baseEmission = isTelegraph ? 1.75f : 1.25f;
            float introEmission = isTelegraph ? 2.15f : 1.55f;

            float alpha = baseAlphaCap;
            float emission = baseEmission;
            if (!isLooping)
            {
                if (t < 0.12f)
                {
                    float intro = t / 0.12f;
                    alpha = Mathf.Clamp01(intro) * baseAlphaCap;
                    emission = Mathf.Lerp(introEmission, baseEmission, intro);
                }
                else if (t > 0.65f)
                {
                    float outro = (t - 0.65f) / 0.35f;
                    alpha = Mathf.Clamp01(1f - outro * outro) * baseAlphaCap;
                    emission = Mathf.Lerp(baseEmission, 0.35f, outro);
                }
            }
            else
            {
                float pulse = 0.88f + 0.12f * Mathf.Sin(_elapsed * 4.5f);
                alpha = pulse * baseAlphaCap;
                emission = baseEmission * (0.90f + 0.12f * Mathf.Sin(_elapsed * 4.5f));
            }

            ApplyShaderProperties(alpha, emission);

            if (layer1PrimaryMesh != null)
            {
                switch (animationStyle)
                {
                    case VFXAnimationStyle.HolyPillar:
                    {
                        float pop = EaseOutBack(Mathf.Clamp01(t / 0.22f));
                        float narrow = t > 0.65f ? Mathf.Lerp(1f, 0.35f, (t - 0.65f) / 0.35f) : 1f;
                        float yStretch = Mathf.Lerp(0.25f, 1.08f, Mathf.Clamp01(t / 0.18f));
                        layer1PrimaryMesh.localPosition = Vector3.zero;
                        layer1PrimaryMesh.localScale = new Vector3(
                            primaryBaseScale.x * pop * narrow,
                            primaryBaseScale.y * yStretch,
                            primaryBaseScale.z * pop * narrow);
                        layer1PrimaryMesh.Rotate(primaryRotationSpeed * dt, Space.Self);
                        break;
                    }
                    case VFXAnimationStyle.EnergyDome:
                    {
                        float intro = isLooping ? EaseOutBack(Mathf.Clamp01(_elapsed / 0.25f)) : EaseOutBack(Mathf.Clamp01(t / 0.22f));
                        float breathe = 1f + 0.045f * Mathf.Sin(_elapsed * 5.2f);
                        layer1PrimaryMesh.localPosition = Vector3.zero;
                        layer1PrimaryMesh.localScale = primaryBaseScale * (intro * breathe);
                        layer1PrimaryMesh.Rotate(primaryRotationSpeed * dt, Space.Self);
                        break;
                    }
                    case VFXAnimationStyle.AngelWings:
                    {
                        float unfurl = EaseOutBack(Mathf.Clamp01(t / 0.28f));
                        float rise = Mathf.Lerp(0.0f, 0.65f, Mathf.SmoothStep(0f, 1f, t));
                        float flap = 1f + 0.08f * Mathf.Sin(_elapsed * 6.0f);
                        layer1PrimaryMesh.localPosition = new Vector3(0f, rise, -0.15f);
                        layer1PrimaryMesh.localScale = new Vector3(
                            primaryBaseScale.x * unfurl * flap,
                            primaryBaseScale.y * unfurl,
                            primaryBaseScale.z * unfurl);
                        break;
                    }
                    case VFXAnimationStyle.ShockwaveSlam:
                    {
                        float expand = EaseOutCubic(Mathf.Clamp01(t / 0.55f));
                        float scale = Mathf.Lerp(0.22f, 1.28f, expand);
                        float yScale = Mathf.Lerp(1.35f, 0.65f, t);
                        layer1PrimaryMesh.localPosition = new Vector3(0f, 0.05f, 0f);
                        layer1PrimaryMesh.localScale = new Vector3(
                            primaryBaseScale.x * scale,
                            primaryBaseScale.y * yScale,
                            primaryBaseScale.z * scale);
                        layer1PrimaryMesh.Rotate(primaryRotationSpeed * dt, Space.Self);
                        break;
                    }
                    case VFXAnimationStyle.EntangleRoots:
                    {
                        float emergeT = Mathf.Clamp01(t / 0.26f);
                        float sinkT = (!isLooping && t > 0.78f) ? (t - 0.78f) / 0.22f : 0f;
                        float yOffset = Mathf.Lerp(-1.15f, 0f, EaseOutBack(emergeT)) - sinkT * 0.65f;
                        float scalePop = Mathf.Lerp(0.35f, 1.0f, EaseOutBack(emergeT));
                        layer1PrimaryMesh.localPosition = new Vector3(0f, yOffset, 0f);
                        layer1PrimaryMesh.localScale = primaryBaseScale * scalePop;
                        layer1PrimaryMesh.localRotation = Quaternion.Euler(0f, Mathf.Lerp(-28f, 0f, emergeT), 0f);
                        break;
                    }
                    case VFXAnimationStyle.SpiralVortex:
                    {
                        float intro = EaseOutCubic(Mathf.Clamp01(t / 0.20f));
                        float expand = Mathf.Lerp(0.45f, 1.15f, intro);
                        layer1PrimaryMesh.localPosition = Vector3.zero;
                        layer1PrimaryMesh.localScale = primaryBaseScale * expand;
                        layer1PrimaryMesh.Rotate(primaryRotationSpeed * dt, Space.Self);
                        break;
                    }
                    case VFXAnimationStyle.GroundTelegraph:
                    case VFXAnimationStyle.ProjectileLoop:
                    {
                        layer1PrimaryMesh.localScale = primaryBaseScale;
                        layer1PrimaryMesh.Rotate(primaryRotationSpeed * dt, Space.Self);
                        break;
                    }
                }
            }

            if (layer1SecondaryMesh != null)
            {
                float intro = EaseOutBack(Mathf.Clamp01(t / 0.25f));
                layer1SecondaryMesh.localScale = secondaryBaseScale * intro;
                layer1SecondaryMesh.Rotate(secondaryRotationSpeed * dt, Space.Self);
            }

            if (layer2DecalTransform != null)
            {
                float decalPop = EaseOutCubic(Mathf.Clamp01(t / 0.20f));
                layer2DecalTransform.localScale = decalBaseScale * decalPop;
                if (Mathf.Abs(decalSpinSpeed) > 0.01f)
                {
                    layer2DecalTransform.Rotate(0f, 0f, decalSpinSpeed * dt, Space.Self);
                }
            }
        }

        private void ApplyShaderProperties(float alphaMultiplier, float emissionIntensity)
        {
            if (_renderers == null || _mpb == null) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer r = _renderers[i];
                if (r == null || r is ParticleSystemRenderer) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetFloat(AlphaMultiplierId, alphaMultiplier);
                _mpb.SetFloat(EmissionIntensityId, emissionIntensity);
                r.SetPropertyBlock(_mpb);
            }
        }

        private void ReturnToPool()
        {
            _followTarget = null;
            if (VFXPoolManager.HasInstance)
            {
                VFXPoolManager.Instance.Release(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float xm1 = x - 1f;
            return 1f + c3 * xm1 * xm1 * xm1 + c1 * xm1 * xm1;
        }

        private static float EaseOutCubic(float x)
        {
            float inv = 1f - x;
            return 1f - inv * inv * inv;
        }
    }
}


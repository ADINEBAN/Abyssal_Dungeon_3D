using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AbyssGame
{
    /// <summary>
    /// AAA Combat Juice, Hit-Stop, Pooled 3D Curved Slash Ribbons, Pooled Directional Hit Sparks,
    /// Renderer Flash, Stylish Combo Counter, and 3D Status Effect Auras (Phase 3 Scalable VFX & game-feel skill).
    /// </summary>
    public class CombatFeelManager : MonoBehaviour
    {
        private static CombatFeelManager _inst;
        public static CombatFeelManager I
        {
            get
            {
                if (_inst == null)
                {
                    _inst = FindObjectOfType<CombatFeelManager>();
                    if (_inst == null)
                    {
                        var go = new GameObject("[CombatFeelManager]");
                        DontDestroyOnLoad(go);
                        _inst = go.AddComponent<CombatFeelManager>();
                    }
                }
                return _inst;
            }
        }

        // Stylish Live Combo Counter for Local Player
        public int ComboCount { get; private set; }
        public int MaxRunCombo { get; private set; }
        public float ComboTimer { get; private set; }
        public const float ComboDecayDuration = 4.2f;

        private int _hitStopToken;
        private bool _inHitStop;
        private Shader _slashShader;

        private void Awake()
        {
            if (_inst != null && _inst != this) { Destroy(gameObject); return; }
            _inst = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ResetRunCombo()
        {
            ComboCount = 0;
            MaxRunCombo = 0;
            ComboTimer = 0f;
        }

        public void RecordPlayerHit(bool crit, bool heavyOrFinisher, bool staggerBreak)
        {
            ComboCount += staggerBreak ? 3 : (heavyOrFinisher ? 2 : 1);
            if (ComboCount > MaxRunCombo) MaxRunCombo = ComboCount;
            ComboTimer = ComboDecayDuration;
        }

        public string GetComboRankLabel(out Color rankColor)
        {
            if (ComboCount >= 35) { rankColor = new Color(1f, 0.32f, 0.58f); return "SS • ABYSSAL SLAYER!"; }
            if (ComboCount >= 22) { rankColor = new Color(1f, 0.78f, 0.22f); return "S • HUYỀN THOẠI"; }
            if (ComboCount >= 14) { rankColor = new Color(0.75f, 0.42f, 1.0f); return "A • TÀN KHỐC"; }
            if (ComboCount >= 8)  { rankColor = new Color(0.32f, 0.85f, 1.0f); return "B • ĐIÊU LUYỆN"; }
            if (ComboCount >= 4)  { rankColor = new Color(0.45f, 0.95f, 0.55f); return "C • LIÊN KÍCH"; }
            rankColor = new Color(0.88f, 0.88f, 0.92f);
            return "ĐÒN ĐÁNH";
        }

        private void Update()
        {
            if (ComboTimer > 0f && Time.timeScale > 0.01f)
            {
                ComboTimer -= Time.unscaledDeltaTime;
                if (ComboTimer <= 0f)
                {
                    ComboTimer = 0f;
                    ComboCount = 0;
                }
            }
        }

        /// <summary>
        /// Safe Realtime Hit-Stop (game-feel Pattern 2):
        /// Freezes simulation briefly on WaitForSecondsRealtime using a monotonic token so overlapping crits
        /// never get stuck at slow-mo and Pause (Time.timeScale == 0) is strictly respected.
        /// </summary>
        public void TriggerHitStop(float duration = 0.042f, float slowScale = 0.05f)
        {
            if (Time.timeScale < 0.005f && !_inHitStop) return;
            duration = Mathf.Clamp(duration, 0.018f, 0.095f);
            _hitStopToken++;
            StartCoroutine(HitStopRoutine(_hitStopToken, duration, Mathf.Clamp(slowScale, 0.02f, 0.25f)));
        }

        private IEnumerator HitStopRoutine(int token, float duration, float slowScale)
        {
            _inHitStop = true;
            Time.timeScale = slowScale;
            yield return new WaitForSecondsRealtime(duration);
            if (token == _hitStopToken)
            {
                _inHitStop = false;
                if (Time.timeScale > 0.001f)
                    Time.timeScale = 1f;
            }
        }

        /// <summary>
        /// Spawns a pooled procedural 3D curved crescent weapon slash arc mesh animated with AbyssalWeaponTrail.shader.
        /// Scales transparency & secondary layers according to VfxQualitySystem while keeping attack readability 100% intact.
        /// </summary>
        public void SpawnSlashArc(
            Vector3 worldOrigin,
            Quaternion facingRot,
            float radius,
            float arcDegrees,
            float tiltDegrees,
            Color coreColor,
            Color edgeColor,
            float duration = 0.22f,
            float ribbonWidth = 0.68f,
            bool reverseSweep = false)
        {
            if (_slashShader == null)
            {
                _slashShader = Resources.Load<Shader>("Shaders/AbyssalWeaponTrail");
                if (_slashShader == null) _slashShader = Shader.Find("AbyssalDungeon/WeaponTrail");
            }

            Quaternion rot = facingRot * Quaternion.Euler(0f, 0f, tiltDegrees);
            var fx = GameObjectPool.GetSlashArc(worldOrigin, rot);
            fx.Init(_slashShader, radius, arcDegrees, ribbonWidth, coreColor, edgeColor, duration, reverseSweep);
        }

        /// <summary>
        /// Spawns a pooled 3D Blender helical whirlwind cyclone (SM_Vfx_WpnSpinWhirlwind) for 360-degree spin attacks.
        /// </summary>
        public void SpawnWhirlwindSlash(Vector3 worldOrigin, Quaternion facingRot, float radius, Color accent)
        {
            float scale = Mathf.Clamp(radius / 2.2f, 0.75f, 1.85f);
            AbyssalDungeon.VFX.VFXManager.EnsureInstance()?.SpawnVFX(
                AbyssalDungeon.VFX.VFXType.Wpn_SpinWhirlwind, worldOrigin, facingRot, 0.48f, accent, null, scale);
        }

        /// <summary>
        /// Spawns a pooled 3D Blender rifled thrust lance cone (SM_Vfx_WpnThrustSpearCone) for lunges, dashes, and stabs.
        /// </summary>
        public void SpawnThrustPierce(Vector3 worldOrigin, Quaternion facingRot, Color accent, float scale = 1.0f)
        {
            AbyssalDungeon.VFX.VFXManager.EnsureInstance()?.SpawnVFX(
                AbyssalDungeon.VFX.VFXType.Wpn_ThrustPierce, worldOrigin, facingRot, 0.35f, accent, null, scale);
        }

        /// <summary>
        /// Spawns a pooled 3D Blender Gothic Aegis barrier dome (SM_Vfx_WpnShieldAegis) on shield block or ward absorption.
        /// </summary>
        public void SpawnShieldAegisBlock(Vector3 worldOrigin, Quaternion facingRot, Color accent, float scale = 1.0f)
        {
            AbyssalDungeon.VFX.VFXManager.EnsureInstance()?.SpawnVFX(
                AbyssalDungeon.VFX.VFXType.Wpn_ShieldBlockAegis, worldOrigin, facingRot, 0.42f, accent, null, scale);
        }

        /// <summary>
        /// Spawns a pooled 3D poise/guard shatter burst when an enemy or boss is staggered.
        /// </summary>
        public void SpawnPoiseShatter(Vector3 worldOrigin, Color accent)
        {
            AbyssalDungeon.VFX.VFXManager.EnsureInstance()?.SpawnVFX(
                AbyssalDungeon.VFX.VFXType.Wpn_PoiseShatter, worldOrigin, Quaternion.identity, 0.75f, accent, null, 1.0f);
        }

        /// <summary>
        /// Spawns pooled directional 3D impact sparks + shockwave ring at the exact hurtbox contact point,
        /// plus a crisp 90ms MaterialPropertyBlock emission flash on the struck unit.
        /// </summary>
        public void SpawnHitImpact(Vector3 contactPoint, Vector3 hitDir, Color accent, bool heavyOrCrit, ActorUnit victim = null)
        {
            if (hitDir.sqrMagnitude < 0.001f) hitDir = Vector3.up;
            var burst = GameObjectPool.GetSparkBurst(contactPoint, Quaternion.LookRotation(hitDir.normalized));
            burst.Init(accent, heavyOrCrit);

            if (victim != null && victim.MainVisual != null && !victim.IsDead)
            {
                StartCoroutine(FlashVictimRenderersRoutine(victim, heavyOrCrit ? new Color(1f, 0.92f, 0.55f) : new Color(0.85f, 0.85f, 0.9f), 0.085f));
            }
        }

        private IEnumerator FlashVictimRenderersRoutine(ActorUnit victim, Color flashEmission, float dur)
        {
            if (victim == null || victim.MainVisual == null) yield break;
            var renderers = victim.MainVisual.GetComponentsInChildren<Renderer>(false);
            var block = new MaterialPropertyBlock();

            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || r is TrailRenderer || r is ParticleSystemRenderer) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", flashEmission * 1.65f);
                r.SetPropertyBlock(block);
            }

            yield return new WaitForSeconds(dur);

            if (victim == null || victim.MainVisual == null) yield break;
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || r is TrailRenderer || r is ParticleSystemRenderer) continue;
                r.SetPropertyBlock(null);
            }
        }
    }

    /// <summary>
    /// Pooled 3D Crescent Ribbon / Heavy Serrated Cleave Mesh for Melee Swings, Combo Finishers, and Heavy Overhead Smashes.
    /// Uses the sculpted Blender 5.3.0 3D volumetric meshes (SM_Vfx_WpnSlashCrescent / SM_Vfx_WpnHeavyCleaveWave)
    /// with MaterialPropertyBlock instancing, falling back to procedural geometry if not yet imported.
    /// </summary>
    public class SlashArcRibbonFx : MonoBehaviour
    {
        private static Mesh _blenderCrescentMesh;
        private static Mesh _blenderHeavyCleaveMesh;
        private static Material _sharedCrescentMat;
        private static Material _sharedHeavyMat;

        private Mesh _mesh;
        private MeshFilter _mf;
        private MeshRenderer _mr;
        private Material _fallbackMat;
        private MaterialPropertyBlock _mpb;
        private Vector3[] _verts;
        private Vector2[] _uvs;
        private Color[] _cols;
        private int[] _tris;
        private float _dur;
        private float _t;
        private float _alphaScale = 1f;
        private Color _coreCol;
        private Color _edgeCol;
        private Vector3 _baseScale = Vector3.one;
        private bool _usingBlenderMesh;

        public void Init(Shader shader, float radius, float arcDeg, float width, Color coreCol, Color edgeCol, float duration, bool reverse)
        {
            _dur = Mathf.Max(0.10f, duration);
            _t = 0f;
            _coreCol = coreCol;
            _edgeCol = edgeCol;

            VfxTierRules rules = VfxQualitySystem.CurrentRules;
            _alphaScale = rules.transparencyAlphaScale;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();

            if (_mf == null)
            {
                _mf = gameObject.GetComponent<MeshFilter>();
                if (_mf == null) _mf = gameObject.AddComponent<MeshFilter>();
            }

            if (_mr == null)
            {
                _mr = gameObject.GetComponent<MeshRenderer>();
                if (_mr == null) _mr = gameObject.AddComponent<MeshRenderer>();
                _mr.shadowCastingMode = ShadowCastingMode.Off;
                _mr.receiveShadows = false;
            }

            bool isHeavy = width >= 0.80f || arcDeg >= 154f;
            if (_blenderCrescentMesh == null)
                _blenderCrescentMesh = AbyssalDungeon.VFX.WeaponVfxFactory.LoadWeaponMesh("SM_Vfx_WpnSlashCrescent");
            if (_blenderHeavyCleaveMesh == null)
                _blenderHeavyCleaveMesh = AbyssalDungeon.VFX.WeaponVfxFactory.LoadWeaponMesh("SM_Vfx_WpnHeavyCleaveWave");

            Mesh chosenBlender = isHeavy ? (_blenderHeavyCleaveMesh ?? _blenderCrescentMesh) : _blenderCrescentMesh;
            _usingBlenderMesh = chosenBlender != null;

            if (_usingBlenderMesh)
            {
                _mf.sharedMesh = chosenBlender;
                float refRadius = (chosenBlender == _blenderHeavyCleaveMesh) ? 2.20f : 1.85f;
                float rScale = Mathf.Clamp(radius / refRadius, 0.55f, 2.25f);
                float arcScaleX = Mathf.Clamp(arcDeg / (isHeavy ? 174f : 148f), 0.72f, 1.32f);
                float signX = reverse ? -1f : 1f;
                _baseScale = new Vector3(rScale * arcScaleX * signX, rScale * (isHeavy ? 1.15f : 1.0f), rScale);

                if (shader != null)
                {
                    if (isHeavy)
                    {
                        if (_sharedHeavyMat == null)
                        {
                            _sharedHeavyMat = AbyssalDungeon.VFX.WeaponVfxFactory.GetSlashTrailMaterial(
                                "M_Wpn_HeavyCleave", "T_Vfx_WpnSerratedArc", Color.white, Color.white);
                        }
                        _mr.sharedMaterial = _sharedHeavyMat;
                    }
                    else
                    {
                        if (_sharedCrescentMat == null)
                        {
                            _sharedCrescentMat = AbyssalDungeon.VFX.WeaponVfxFactory.GetSlashTrailMaterial(
                                "M_Wpn_SlashCrescent", "T_Vfx_SlashCrescent", Color.white, Color.white);
                        }
                        _mr.sharedMaterial = _sharedCrescentMat;
                    }
                }
            }
            else
            {
                UpdateCrescentMesh(Mathf.Max(0.9f, radius), Mathf.Clamp(arcDeg, 40f, 340f), Mathf.Clamp(width, 0.25f, 1.6f), reverse);
                _mf.sharedMesh = _mesh;
                _baseScale = Vector3.one;

                if (_fallbackMat == null)
                {
                    if (shader != null)
                    {
                        _fallbackMat = new Material(shader);
                        var tex = Resources.Load<Texture2D>("VFX/Textures/T_Vfx_SlashCrescent")
                                  ?? Resources.Load<Texture2D>("VFX/Textures/T_Vfx_BladeSlash");
                        if (tex != null) _fallbackMat.SetTexture("_MainTex", tex);
                    }
                    else
                    {
                        _fallbackMat = CharacterVisualFactory.MakeTransparentMat(new Color(edgeCol.r, edgeCol.g, edgeCol.b, 0.85f), coreCol * 2.4f);
                    }
                }
                _mr.sharedMaterial = _fallbackMat;
            }

            ApplySlashPropertyBlock(0f, 1.45f * _alphaScale);
            transform.localScale = _baseScale * 0.88f;
        }

        private void ApplySlashPropertyBlock(float progress, float opacity)
        {
            if (_mr == null) return;
            _mpb.Clear();
            _mpb.SetColor("_MainColor", _coreCol * 1.35f);
            _mpb.SetColor("_EdgeColor", _edgeCol);
            _mpb.SetFloat("_Progress", progress);
            _mpb.SetFloat("_Opacity", opacity);
            _mpb.SetColor("_BaseColor", new Color(_edgeCol.r, _edgeCol.g, _edgeCol.b, opacity * 0.65f));
            _mr.SetPropertyBlock(_mpb);
        }

        private void UpdateCrescentMesh(float outerR, float arcDeg, float width, bool reverse)
        {
            const int segs = 18;
            int vCount = (segs + 1) * 2;
            bool firstBuild = (_mesh == null);

            if (firstBuild)
            {
                _mesh = new Mesh { name = "PooledSlashCrescent" };
                _mesh.MarkDynamic();
                _verts = new Vector3[vCount];
                _uvs = new Vector2[vCount];
                _cols = new Color[vCount];
                _tris = new int[segs * 6];
            }

            float halfRad = arcDeg * 0.5f * Mathf.Deg2Rad;
            for (int i = 0; i <= segs; i++)
            {
                float u = (float)i / segs;
                float uSweep = reverse ? (1f - u) : u;
                float ang = Mathf.Lerp(-halfRad, halfRad, uSweep);

                float taper = Mathf.Sin(Mathf.PI * Mathf.Clamp01(u));
                float innerR = Mathf.Max(0.15f, outerR - width * (0.25f + 0.75f * taper));

                float s = Mathf.Sin(ang);
                float c = Mathf.Cos(ang);

                _verts[i * 2 + 0] = new Vector3(s * innerR, 0f, c * innerR);
                _verts[i * 2 + 1] = new Vector3(s * outerR, 0f, c * outerR);

                if (firstBuild)
                {
                    _uvs[i * 2 + 0] = new Vector2(u, 0f);
                    _uvs[i * 2 + 1] = new Vector2(u, 1f);

                    _cols[i * 2 + 0] = new Color(1f, 1f, 1f, 0.35f * taper);
                    _cols[i * 2 + 1] = new Color(1f, 1f, 1f, 1.0f * taper);

                    if (i < segs)
                    {
                        int b = i * 2;
                        int t = i * 6;
                        _tris[t + 0] = b + 0;
                        _tris[t + 1] = b + 1;
                        _tris[t + 2] = b + 3;
                        _tris[t + 3] = b + 0;
                        _tris[t + 4] = b + 3;
                        _tris[t + 5] = b + 2;
                    }
                }
            }

            _mesh.vertices = _verts;
            if (firstBuild)
            {
                _mesh.uv = _uvs;
                _mesh.colors = _cols;
                _mesh.triangles = _tris;
            }
            _mesh.RecalculateBounds();
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float u = Mathf.Clamp01(_t / _dur);
            transform.localScale = _baseScale * Mathf.Lerp(0.88f, 1.12f, u);
            ApplySlashPropertyBlock(u, Mathf.Pow(1f - u, 0.75f) * _alphaScale * 1.35f);

            if (_t >= _dur)
                GameObjectPool.ReleaseSlashArc(this);
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
            if (_fallbackMat != null) Destroy(_fallbackMat);
        }
    }

    /// <summary>
    /// Pooled Snappy 3D Impact Spark Streaks + Expanding Shockwave Ring at weapon contact point.
    /// Pre-allocates up to MaxPooledSparks child Transforms once and scales active count via VfxQualitySystem.
    /// </summary>
    public class DirectionalSparkBurstFx : MonoBehaviour
    {
        private const int MaxPooledSparks = 14;

        private Transform[] _sparks;
        private Renderer[] _sparkRenderers;
        private Vector3[] _vels;
        private Transform _ring;
        private Renderer _ringRenderer;
        private Transform _flash;
        private Renderer _flashRenderer;
        private Material _sparkMat;
        private Material _ringMat;
        private Material _flashMat;
        private MaterialPropertyBlock _mpb;

        private int _activeCount;
        private bool _ringActive;
        private float _dur;
        private float _t;
        private bool _heavy;

        private void EnsureAllocated()
        {
            if (_sparks != null) return;

            _mpb = new MaterialPropertyBlock();

            Shader addShader = Resources.Load<Shader>("Shaders/AbyssalEnvAdditive");
            if (addShader == null) addShader = Shader.Find("AbyssalDungeon/EnvAdditive");
            if (addShader == null) addShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

            // 1. Spark Material with Ember/Spark Texture
            _sparkMat = new Material(addShader);
            var sparkTex = Resources.Load<Texture2D>("VFX/Textures/T_Vfx_EmberSpark");
            if (sparkTex == null) sparkTex = Resources.Load<Texture2D>("VFX/Textures/T_Vfx_StarFlare");
            if (sparkTex != null) _sparkMat.SetTexture("_MainTex", sparkTex);

            // 2. Shockwave Ring Material with ShockRing Texture
            _ringMat = new Material(addShader);
            var ringTex = Resources.Load<Texture2D>("VFX/Textures/T_Vfx_ShockRing");
            if (ringTex == null) ringTex = Resources.Load<Texture2D>("VFX/Textures/T_Vfx_RadialBurstRing");
            if (ringTex != null) _ringMat.SetTexture("_MainTex", ringTex);

            // 3. Central Hit Flash Material with StarFlare Texture
            _flashMat = new Material(addShader);
            var flashTex = Resources.Load<Texture2D>("VFX/Textures/T_Vfx_StarFlare");
            if (flashTex != null) _flashMat.SetTexture("_MainTex", flashTex);

            _sparks = new Transform[MaxPooledSparks];
            _sparkRenderers = new Renderer[MaxPooledSparks];
            _vels = new Vector3[MaxPooledSparks];

            for (int i = 0; i < MaxPooledSparks; i++)
            {
                var sp = GameObject.CreatePrimitive(PrimitiveType.Quad);
                sp.name = "SparkQuad_" + i;
                sp.transform.SetParent(transform, false);
                var col = sp.GetComponent<Collider>();
                if (col != null) Destroy(col);

                _sparks[i] = sp.transform;
                _sparkRenderers[i] = sp.GetComponent<Renderer>();
                _sparkRenderers[i].sharedMaterial = _sparkMat;
                _sparkRenderers[i].shadowCastingMode = ShadowCastingMode.Off;
                _sparkRenderers[i].receiveShadows = false;
                sp.SetActive(false);
            }

            // Expanding Shockwave Ring (Quad on impact plane)
            var ringGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ringGo.name = "ImpactShockRing";
            ringGo.transform.SetParent(transform, false);
            ringGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var ringCol = ringGo.GetComponent<Collider>();
            if (ringCol != null) Destroy(ringCol);

            _ring = ringGo.transform;
            _ringRenderer = ringGo.GetComponent<Renderer>();
            _ringRenderer.sharedMaterial = _ringMat;
            _ringRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _ringRenderer.receiveShadows = false;

            // Instant Center Star Flash
            var flashGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            flashGo.name = "ImpactStarFlash";
            flashGo.transform.SetParent(transform, false);
            var flashCol = flashGo.GetComponent<Collider>();
            if (flashCol != null) Destroy(flashCol);

            _flash = flashGo.transform;
            _flashRenderer = flashGo.GetComponent<Renderer>();
            _flashRenderer.sharedMaterial = _flashMat;
            _flashRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _flashRenderer.receiveShadows = false;
        }

        public void Init(Color accent, bool heavyOrCrit)
        {
            EnsureAllocated();

            _heavy = heavyOrCrit;
            _dur = heavyOrCrit ? 0.32f : 0.22f;
            _t = 0f;

            VfxTierRules rules = VfxQualitySystem.CurrentRules;
            Color bright = Color.Lerp(accent, new Color(1f, 0.96f, 0.75f), heavyOrCrit ? 0.75f : 0.45f);

            _mpb.Clear();
            _mpb.SetColor("_CoreColor", bright * 2.2f);
            _mpb.SetColor("_OuterColor", accent);
            _mpb.SetColor("_BaseColor", bright);
            _mpb.SetColor("_EmissionColor", bright * 3.5f);
            _mpb.SetFloat("_Intensity", 2.5f);

            int baseCount = heavyOrCrit ? 10 : 6;
            _activeCount = Mathf.Clamp(Mathf.RoundToInt(baseCount * rules.particleCountMultiplier), 3, MaxPooledSparks);

            for (int i = 0; i < MaxPooledSparks; i++)
            {
                bool active = i < _activeCount;
                _sparks[i].gameObject.SetActive(active);
                if (!active) continue;

                Vector3 dir = Quaternion.Euler(
                    Random.Range(-55f, 55f),
                    Random.Range(-65f, 65f),
                    Random.Range(0f, 360f)) * Vector3.forward;

                float spd = Random.Range(5.5f, heavyOrCrit ? 13.5f : 9.5f);
                _vels[i] = transform.TransformDirection(dir) * spd + Vector3.up * Random.Range(1.5f, 3.8f);

                _sparks[i].position = transform.position;
                _sparks[i].rotation = Quaternion.LookRotation(_vels[i].normalized);
                _sparks[i].localScale = new Vector3(0.08f, 0.08f, heavyOrCrit ? 0.45f : 0.30f);
                if (_sparkRenderers[i] != null)
                    _sparkRenderers[i].SetPropertyBlock(_mpb);
            }

            _ringActive = rules.enableShockwaveRings || heavyOrCrit;
            if (_ring != null)
            {
                _ring.gameObject.SetActive(_ringActive);
                if (_ringActive)
                {
                    _ring.localPosition = Vector3.zero;
                    _ring.localScale = Vector3.one * 0.2f;
                    if (_ringRenderer != null)
                        _ringRenderer.SetPropertyBlock(_mpb);
                }
            }

            if (_flash != null)
            {
                _flash.gameObject.SetActive(true);
                _flash.localPosition = Vector3.zero;
                _flash.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                _flash.localScale = Vector3.one * (heavyOrCrit ? 1.35f : 0.85f);
                if (_flashRenderer != null)
                    _flashRenderer.SetPropertyBlock(_mpb);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _t += dt;
            float u = Mathf.Clamp01(_t / _dur);

            for (int i = 0; i < _activeCount; i++)
            {
                if (_sparks[i] == null) continue;
                _vels[i] += Vector3.down * (15f * dt);
                _sparks[i].position += _vels[i] * dt;
                if (_vels[i].sqrMagnitude > 0.05f)
                    _sparks[i].rotation = Quaternion.LookRotation(_vels[i].normalized);
                float len = Mathf.Lerp(_heavy ? 0.45f : 0.30f, 0.04f, u);
                float w = Mathf.Lerp(0.09f, 0.01f, u);
                _sparks[i].localScale = new Vector3(w, w, len);
            }

            if (_ringActive && _ring != null)
            {
                float r = Mathf.Lerp(0.2f, _heavy ? 2.3f : 1.45f, Mathf.Sqrt(u));
                _ring.localScale = new Vector3(r, r, 1f);
                _mpb.SetFloat("_Intensity", Mathf.Lerp(2.8f, 0.0f, u));
                if (_ringRenderer != null) _ringRenderer.SetPropertyBlock(_mpb);
            }

            if (_flash != null && _flash.gameObject.activeSelf)
            {
                float flashScale = Mathf.Lerp(_heavy ? 1.35f : 0.85f, 0.0f, u * 2.8f);
                if (flashScale <= 0.01f)
                {
                    _flash.gameObject.SetActive(false);
                }
                else
                {
                    _flash.localScale = Vector3.one * flashScale;
                    _mpb.SetFloat("_Intensity", Mathf.Lerp(3.5f, 0.0f, u * 2.5f));
                    if (_flashRenderer != null) _flashRenderer.SetPropertyBlock(_mpb);
                }
            }

            if (_t >= _dur)
                GameObjectPool.ReleaseSparkBurst(this);
        }

        private void OnDestroy()
        {
            if (_sparkMat != null) Destroy(_sparkMat);
            if (_ringMat != null) Destroy(_ringMat);
            if (_flashMat != null) Destroy(_flashMat);
        }
    }

    /// <summary>
    /// Real-time 3D Status & Shield Visual Auras attached to ActorUnit:
    /// Shows readable 3D cues for Stagger/Stun stars and Burn embers.
    /// </summary>
    public class StatusVisualAttachment : MonoBehaviour
    {
        private ActorUnit _unit;
        private GameObject _shieldDome;
        private GameObject _staggerStars;
        private GameObject _freezeCrystal;
        private GameObject _burnRing;
        private GameObject _poisonRing;
        private GameObject _curseHex;

        public static StatusVisualAttachment AttachTo(ActorUnit unit)
        {
            if (unit == null) return null;
            var comp = unit.GetComponent<StatusVisualAttachment>();
            if (comp == null) comp = unit.gameObject.AddComponent<StatusVisualAttachment>();
            comp._unit = unit;
            return comp;
        }

        private void LateUpdate()
        {
            if (_unit == null || _unit.IsDead)
            {
                SetAllInactive();
                return;
            }

            float t = Time.time;

            if (_shieldDome != null) _shieldDome.SetActive(false);

            bool dazed = _unit.IsStaggered || _unit.HasStatus(StatusType.Stun);
            if (dazed && _staggerStars == null)
            {
                _staggerStars = new GameObject("VfxStaggerStars");
                _staggerStars.transform.SetParent(transform, false);
                float h = _unit.Controller != null ? _unit.Controller.height + 0.35f : 2.25f;
                _staggerStars.transform.localPosition = new Vector3(0f, h, 0f);
                var starMat = CharacterVisualFactory.MakeMat(new Color(1f, 0.88f, 0.22f), 0.9f, 0.2f, new Color(1f, 0.82f, 0.2f) * 2.6f);
                for (int i = 0; i < 3; i++)
                {
                    float ang = i * 120f * Mathf.Deg2Rad;
                    CharacterVisualFactory.Prim(
                        PrimitiveType.Cube, "Star" + i, _staggerStars.transform,
                        new Vector3(Mathf.Cos(ang) * 0.42f, 0f, Mathf.Sin(ang) * 0.42f),
                        Vector3.one * 0.13f, starMat, Quaternion.Euler(45f, 45f, 0f));
                }
            }
            if (_staggerStars != null)
            {
                _staggerStars.SetActive(dazed);
                if (dazed) _staggerStars.transform.Rotate(0f, 220f * Time.deltaTime, 0f);
            }

            if (_freezeCrystal != null) _freezeCrystal.SetActive(false);

            bool burning = _unit.HasStatus(StatusType.Burn);
            if (burning && _burnRing == null)
            {
                _burnRing = new GameObject("VfxBurnEmbers");
                _burnRing.transform.SetParent(transform, false);
                _burnRing.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                var fireMat = CharacterVisualFactory.MakeMat(new Color(1f, 0.48f, 0.1f), 0.9f, 0f, new Color(1f, 0.42f, 0.08f) * 3f);
                for (int i = 0; i < 4; i++)
                {
                    float ang = i * 90f * Mathf.Deg2Rad;
                    CharacterVisualFactory.Prim(PrimitiveType.Sphere, "Ember" + i, _burnRing.transform,
                        new Vector3(Mathf.Cos(ang) * 0.52f, (i % 2) * 0.22f, Mathf.Sin(ang) * 0.52f),
                        Vector3.one * 0.12f, fireMat);
                }
            }
            if (_burnRing != null)
            {
                _burnRing.SetActive(burning);
                if (burning)
                {
                    _burnRing.transform.Rotate(0f, -180f * Time.deltaTime, 0f);
                    _burnRing.transform.localPosition = new Vector3(0f, 0.95f + Mathf.Sin(t * 6f) * 0.08f, 0f);
                }
            }

            if (_poisonRing != null) _poisonRing.SetActive(false);
            if (_curseHex != null) _curseHex.SetActive(false);
        }

        private void SetAllInactive()
        {
            if (_shieldDome != null) _shieldDome.SetActive(false);
            if (_staggerStars != null) _staggerStars.SetActive(false);
            if (_freezeCrystal != null) _freezeCrystal.SetActive(false);
            if (_burnRing != null) _burnRing.SetActive(false);
            if (_poisonRing != null) _poisonRing.SetActive(false);
            if (_curseHex != null) _curseHex.SetActive(false);
        }
    }
}

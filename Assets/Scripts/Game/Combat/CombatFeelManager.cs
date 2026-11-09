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
    /// Pooled Procedural 3D Crescent Ribbon Mesh for Melee Swings, Combo Finishers, and Heavy Overhead Smashes.
    /// Reuses its Mesh and Material across pool cycles without per-swing allocations.
    /// </summary>
    public class SlashArcRibbonFx : MonoBehaviour
    {
        private Mesh _mesh;
        private MeshFilter _mf;
        private MeshRenderer _mr;
        private Material _mat;
        private Vector3[] _verts;
        private Vector2[] _uvs;
        private Color[] _cols;
        private int[] _tris;
        private float _dur;
        private float _t;
        private float _alphaScale = 1f;
        private Color _edgeCol;

        public void Init(Shader shader, float radius, float arcDeg, float width, Color coreCol, Color edgeCol, float duration, bool reverse)
        {
            _dur = Mathf.Max(0.10f, duration);
            _t = 0f;
            _edgeCol = edgeCol;

            VfxTierRules rules = VfxQualitySystem.CurrentRules;
            _alphaScale = rules.transparencyAlphaScale;

            UpdateCrescentMesh(Mathf.Max(0.9f, radius), Mathf.Clamp(arcDeg, 40f, 340f), Mathf.Clamp(width, 0.25f, 1.6f), reverse);

            if (_mf == null)
            {
                _mf = gameObject.GetComponent<MeshFilter>();
                if (_mf == null) _mf = gameObject.AddComponent<MeshFilter>();
                _mf.sharedMesh = _mesh;
            }

            if (_mr == null)
            {
                _mr = gameObject.GetComponent<MeshRenderer>();
                if (_mr == null) _mr = gameObject.AddComponent<MeshRenderer>();
                _mr.shadowCastingMode = ShadowCastingMode.Off;
                _mr.receiveShadows = false;
            }

            if (_mat == null)
            {
                if (shader != null)
                    _mat = new Material(shader);
                else
                    _mat = CharacterVisualFactory.MakeTransparentMat(new Color(edgeCol.r, edgeCol.g, edgeCol.b, 0.72f), coreCol * 2f);
                _mr.sharedMaterial = _mat;
            }

            if (_mat != null)
            {
                if (_mat.HasProperty("_MainColor")) _mat.SetColor("_MainColor", coreCol);
                if (_mat.HasProperty("_EdgeColor")) _mat.SetColor("_EdgeColor", edgeCol);
                if (_mat.HasProperty("_Progress")) _mat.SetFloat("_Progress", 0f);
                if (_mat.HasProperty("_Opacity")) _mat.SetFloat("_Opacity", 0.95f * _alphaScale);
            }

            transform.localScale = Vector3.one * 0.88f;
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
            transform.localScale = Vector3.one * Mathf.Lerp(0.88f, 1.12f, u);

            if (_mat != null)
            {
                if (_mat.HasProperty("_Progress")) _mat.SetFloat("_Progress", u);
                if (_mat.HasProperty("_Opacity")) _mat.SetFloat("_Opacity", Mathf.Pow(1f - u, 0.75f) * _alphaScale);
                if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", new Color(_edgeCol.r, _edgeCol.g, _edgeCol.b, (1f - u) * 0.7f * _alphaScale));
            }

            if (_t >= _dur)
                GameObjectPool.ReleaseSlashArc(this);
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
            if (_mat != null) Destroy(_mat);
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
        private Material _sparkMat;
        private Material _ringMat;
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
            _sparkMat = CharacterVisualFactory.MakeMat(Color.white, 0.9f, 0f, Color.white * 2.5f);
            _ringMat = CharacterVisualFactory.MakeTransparentMat(new Color(1f, 1f, 1f, 0.62f), Color.white * 2f);

            _sparks = new Transform[MaxPooledSparks];
            _sparkRenderers = new Renderer[MaxPooledSparks];
            _vels = new Vector3[MaxPooledSparks];

            for (int i = 0; i < MaxPooledSparks; i++)
            {
                var sp = CharacterVisualFactory.Prim(
                    PrimitiveType.Cube, "Spark" + i, transform,
                    Vector3.zero, new Vector3(0.035f, 0.035f, 0.25f), _sparkMat);
                _sparks[i] = sp.transform;
                _sparkRenderers[i] = sp.GetComponent<Renderer>();
                sp.SetActive(false);
            }

            var ringGo = CharacterVisualFactory.Prim(
                PrimitiveType.Cylinder, "ImpactRing", transform,
                Vector3.zero, new Vector3(0.2f, 0.012f, 0.2f), _ringMat,
                Quaternion.Euler(90f, 0f, 0f));
            _ring = ringGo.transform;
            _ringRenderer = ringGo.GetComponent<Renderer>();
        }

        public void Init(Color accent, bool heavyOrCrit)
        {
            EnsureAllocated();

            _heavy = heavyOrCrit;
            _dur = heavyOrCrit ? 0.28f : 0.19f;
            _t = 0f;

            VfxTierRules rules = VfxQualitySystem.CurrentRules;
            Color bright = Color.Lerp(accent, new Color(1f, 0.94f, 0.65f), heavyOrCrit ? 0.65f : 0.35f);

            _mpb.Clear();
            _mpb.SetColor("_BaseColor", bright);
            _mpb.SetColor("_Color", bright);
            _mpb.SetColor("_EmissionColor", bright * 3.0f);

            int baseCount = heavyOrCrit ? 8 : 5;
            _activeCount = Mathf.Clamp(Mathf.RoundToInt(baseCount * rules.particleCountMultiplier), 2, MaxPooledSparks);

            for (int i = 0; i < MaxPooledSparks; i++)
            {
                bool active = i < _activeCount;
                _sparks[i].gameObject.SetActive(active);
                if (!active) continue;

                Vector3 dir = Quaternion.Euler(
                    Random.Range(-52f, 52f),
                    Random.Range(-65f, 65f),
                    0f) * Vector3.forward;

                float spd = Random.Range(4.5f, heavyOrCrit ? 11.5f : 8.2f);
                _vels[i] = transform.TransformDirection(dir) * spd + Vector3.up * Random.Range(1.2f, 3.2f);

                _sparks[i].position = transform.position;
                _sparks[i].rotation = Quaternion.LookRotation(_vels[i].normalized);
                _sparks[i].localScale = new Vector3(0.035f, 0.035f, heavyOrCrit ? 0.34f : 0.22f);
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
                    _ring.localScale = new Vector3(0.22f, 0.01f, 0.22f);
                    if (_ringRenderer != null)
                    {
                        _mpb.SetColor("_BaseColor", new Color(bright.r, bright.g, bright.b, 0.62f * rules.transparencyAlphaScale));
                        _ringRenderer.SetPropertyBlock(_mpb);
                    }
                }
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
                _vels[i] += Vector3.down * (14f * dt);
                _sparks[i].position += _vels[i] * dt;
                if (_vels[i].sqrMagnitude > 0.05f)
                    _sparks[i].rotation = Quaternion.LookRotation(_vels[i].normalized);
                float len = Mathf.Lerp(_heavy ? 0.36f : 0.22f, 0.02f, u);
                _sparks[i].localScale = new Vector3(0.032f * (1f - u * 0.6f), 0.032f * (1f - u * 0.6f), len);
            }

            if (_ringActive && _ring != null)
            {
                float r = Mathf.Lerp(0.22f, _heavy ? 1.85f : 1.05f, Mathf.Sqrt(u));
                _ring.localScale = new Vector3(r, 0.01f, r);
            }

            if (_t >= _dur)
                GameObjectPool.ReleaseSparkBurst(this);
        }

        private void OnDestroy()
        {
            if (_sparkMat != null) Destroy(_sparkMat);
            if (_ringMat != null) Destroy(_ringMat);
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

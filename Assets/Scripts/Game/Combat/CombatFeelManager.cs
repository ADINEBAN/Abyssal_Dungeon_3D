using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AbyssGame
{
    /// <summary>
    /// AAA Combat Juice, Hit-Stop, 3D Curved Slash Ribbons, Directional Hit Sparks, Renderer Flash,
    /// Stylish Combo Counter, and 3D Status Effect Auras (Phase 3 - Section 3.2 & game-feel skill).
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
            // Never interfere if game is Paused (timeScale == 0)
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
                // Restore to 1f only if user did not open Pause menu during the 45ms hit-stop
                if (Time.timeScale > 0.001f)
                    Time.timeScale = 1f;
            }
        }

        /// <summary>
        /// Spawns a procedural 3D curved crescent weapon slash arc mesh animated with AbyssalWeaponTrail.shader.
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

            var go = new GameObject("VfxSlashArc");
            go.transform.position = worldOrigin;
            go.transform.rotation = facingRot * Quaternion.Euler(0f, 0f, tiltDegrees);

            var fx = go.AddComponent<SlashArcRibbonFx>();
            fx.Init(_slashShader, radius, arcDegrees, ribbonWidth, coreColor, edgeColor, duration, reverseSweep);
        }

        /// <summary>
        /// Spawns directional 3D impact sparks + shockwave ring at the exact hurtbox contact point,
        /// plus a crisp 90ms MaterialPropertyBlock emission flash on the struck unit.
        /// </summary>
        public void SpawnHitImpact(Vector3 contactPoint, Vector3 hitDir, Color accent, bool heavyOrCrit, ActorUnit victim = null)
        {
            var go = new GameObject("VfxHitSparks");
            go.transform.position = contactPoint;
            if (hitDir.sqrMagnitude < 0.001f) hitDir = Vector3.up;
            go.transform.rotation = Quaternion.LookRotation(hitDir.normalized);

            var burst = go.AddComponent<DirectionalSparkBurstFx>();
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
                r.SetPropertyBlock(null); // Cleanly revert to shared material state
            }
        }
    }

    /// <summary>
    /// Procedural 3D Crescent Ribbon Mesh for Melee Swings, Combo Finishers, and Heavy Overhead Smashes.
    /// </summary>
    public class SlashArcRibbonFx : MonoBehaviour
    {
        private Mesh _mesh;
        private Material _mat;
        private float _dur;
        private float _t;
        private Color _edgeCol;

        public void Init(Shader shader, float radius, float arcDeg, float width, Color coreCol, Color edgeCol, float duration, bool reverse)
        {
            _dur = Mathf.Max(0.10f, duration);
            _edgeCol = edgeCol;
            _mesh = BuildCrescentMesh(Mathf.Max(0.9f, radius), Mathf.Clamp(arcDeg, 40f, 340f), Mathf.Clamp(width, 0.25f, 1.6f), reverse);

            var mf = gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = _mesh;

            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            if (shader != null)
            {
                _mat = new Material(shader);
                _mat.SetColor("_MainColor", coreCol);
                _mat.SetColor("_EdgeColor", edgeCol);
                _mat.SetFloat("_Progress", 0f);
                _mat.SetFloat("_Opacity", 0.95f);
            }
            else
            {
                _mat = CharacterVisualFactory.MakeTransparentMat(new Color(edgeCol.r, edgeCol.g, edgeCol.b, 0.72f), coreCol * 2f);
            }
            mr.sharedMaterial = _mat;
        }

        private static Mesh BuildCrescentMesh(float outerR, float arcDeg, float width, bool reverse)
        {
            const int segs = 18;
            int vCount = (segs + 1) * 2;
            var verts = new Vector3[vCount];
            var uvs = new Vector2[vCount];
            var cols = new Color[vCount];
            var tris = new int[segs * 6];

            float halfRad = arcDeg * 0.5f * Mathf.Deg2Rad;
            for (int i = 0; i <= segs; i++)
            {
                float u = (float)i / segs;
                float uSweep = reverse ? (1f - u) : u;
                float ang = Mathf.Lerp(-halfRad, halfRad, uSweep);

                // Taper crescent width at both ends so blade arc looks sharp and calligraphic
                float taper = Mathf.Sin(Mathf.PI * Mathf.Clamp01(u));
                float innerR = Mathf.Max(0.15f, outerR - width * (0.25f + 0.75f * taper));

                float s = Mathf.Sin(ang);
                float c = Mathf.Cos(ang);

                verts[i * 2 + 0] = new Vector3(s * innerR, 0f, c * innerR);
                verts[i * 2 + 1] = new Vector3(s * outerR, 0f, c * outerR);

                uvs[i * 2 + 0] = new Vector2(u, 0f);
                uvs[i * 2 + 1] = new Vector2(u, 1f);

                cols[i * 2 + 0] = new Color(1f, 1f, 1f, 0.35f * taper);
                cols[i * 2 + 1] = new Color(1f, 1f, 1f, 1.0f * taper);

                if (i < segs)
                {
                    int b = i * 2;
                    int t = i * 6;
                    tris[t + 0] = b + 0;
                    tris[t + 1] = b + 1;
                    tris[t + 2] = b + 3;
                    tris[t + 3] = b + 0;
                    tris[t + 4] = b + 3;
                    tris[t + 5] = b + 2;
                }
            }

            var mesh = new Mesh { name = "SlashCrescent" };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.colors = cols;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float u = Mathf.Clamp01(_t / _dur);
            transform.localScale = Vector3.one * Mathf.Lerp(0.88f, 1.12f, u);

            if (_mat != null)
            {
                if (_mat.HasProperty("_Progress")) _mat.SetFloat("_Progress", u);
                if (_mat.HasProperty("_Opacity")) _mat.SetFloat("_Opacity", Mathf.Pow(1f - u, 0.75f));
                if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", new Color(_edgeCol.r, _edgeCol.g, _edgeCol.b, (1f - u) * 0.7f));
            }

            if (_t >= _dur) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
            if (_mat != null) Destroy(_mat);
        }
    }

    /// <summary>
    /// Snappy 3D Impact Spark Streaks + Expanding Shockwave Ring at weapon contact point.
    /// </summary>
    public class DirectionalSparkBurstFx : MonoBehaviour
    {
        private Transform[] _sparks;
        private Vector3[] _vels;
        private Transform _ring;
        private Material _sparkMat;
        private Material _ringMat;
        private float _dur;
        private float _t;
        private bool _heavy;

        public void Init(Color accent, bool heavyOrCrit)
        {
            _heavy = heavyOrCrit;
            _dur = heavyOrCrit ? 0.28f : 0.19f;
            Color bright = Color.Lerp(accent, new Color(1f, 0.94f, 0.65f), heavyOrCrit ? 0.65f : 0.35f);

            _sparkMat = CharacterVisualFactory.MakeMat(bright, 0.9f, 0f, bright * 3.2f);
            _ringMat = CharacterVisualFactory.MakeTransparentMat(new Color(bright.r, bright.g, bright.b, 0.62f), bright * 2f);

            int count = heavyOrCrit ? 8 : 5;
            _sparks = new Transform[count];
            _vels = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                Vector3 dir = Quaternion.Euler(
                    Random.Range(-52f, 52f),
                    Random.Range(-65f, 65f),
                    0f) * Vector3.forward;

                float spd = Random.Range(4.5f, heavyOrCrit ? 11.5f : 8.2f);
                _vels[i] = transform.TransformDirection(dir) * spd + Vector3.up * Random.Range(1.2f, 3.2f);

                var sp = CharacterVisualFactory.Prim(
                    PrimitiveType.Cube, "Spark" + i, null,
                    transform.position,
                    new Vector3(0.035f, 0.035f, heavyOrCrit ? 0.34f : 0.22f),
                    _sparkMat,
                    Quaternion.LookRotation(_vels[i].normalized));
                sp.transform.SetParent(transform, true);
                _sparks[i] = sp.transform;
            }

            _ring = CharacterVisualFactory.Prim(
                PrimitiveType.Cylinder, "ImpactRing", transform,
                Vector3.zero, new Vector3(0.2f, 0.012f, 0.2f), _ringMat,
                Quaternion.Euler(90f, 0f, 0f)).transform;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _t += dt;
            float u = Mathf.Clamp01(_t / _dur);

            for (int i = 0; i < _sparks.Length; i++)
            {
                if (_sparks[i] == null) continue;
                _vels[i] += Vector3.down * (14f * dt);
                _sparks[i].position += _vels[i] * dt;
                if (_vels[i].sqrMagnitude > 0.05f)
                    _sparks[i].rotation = Quaternion.LookRotation(_vels[i].normalized);
                float len = Mathf.Lerp(_heavy ? 0.36f : 0.22f, 0.02f, u);
                _sparks[i].localScale = new Vector3(0.032f * (1f - u * 0.6f), 0.032f * (1f - u * 0.6f), len);
            }

            if (_ring != null)
            {
                float r = Mathf.Lerp(0.22f, _heavy ? 1.85f : 1.05f, Mathf.Sqrt(u));
                _ring.localScale = new Vector3(r, 0.01f, r);
            }

            if (_t >= _dur) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_sparkMat != null) Destroy(_sparkMat);
            if (_ringMat != null) Destroy(_ringMat);
        }
    }

    /// <summary>
    /// Real-time 3D Status & Shield Visual Auras attached to ActorUnit:
    /// Shows readable 3D cues for Shield dome, Stagger/Stun stars, Freeze ice crystal, Burn embers, Poison miasma, and Rage aura.
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

            // 1. Aegis Shield Dome — disabled bulky sphere so characters stay clean without overlapping bubbles/frames
            if (_shieldDome != null) _shieldDome.SetActive(false);

            // 2. Stun / Stagger Break Orbiting Stars above Head
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

            // 3. Freeze / Slow — no cylinder ring at feet
            if (_freezeCrystal != null) _freezeCrystal.SetActive(false);

            // 4. Burn Orbiting Embers
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

            // 5. Poison Miasma — disabled bulky sphere
            if (_poisonRing != null) _poisonRing.SetActive(false);

            // 6. Curse Hex Sigil — disabled cylinder at feet
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


using UnityEngine;
using UnityEngine.Rendering;

namespace AbyssGame
{
    /// <summary>
    /// Attaches a sleek URP additive blade ribbon TrailRenderer to any melee weapon tip.
    /// Emits only during active attack swings, Heavy Attacks, and Dash skills.
    /// </summary>
    public class WeaponBladeTrail3D : MonoBehaviour
    {
        [SerializeField] private TrailRenderer _trail;
        [SerializeField] private Material _mat;
        [SerializeField] private Color _defaultColor;
        private float _emitTimer;

        public static WeaponBladeTrail3D Attach(Transform weaponParent, Vector3 localTipOffset, float bladeWidth, Color accentColor)
        {
            if (weaponParent == null) return null;
            var tipGo = new GameObject("BladeTrailTip");
            tipGo.transform.SetParent(weaponParent, false);
            tipGo.transform.localPosition = localTipOffset;

            var comp = tipGo.AddComponent<WeaponBladeTrail3D>();
            comp.Setup(bladeWidth, accentColor);
            return comp;
        }

        private void Setup(float width, Color accent)
        {
            _defaultColor = accent;
            _trail = gameObject.AddComponent<TrailRenderer>();
            _trail.time = 0.11f;
            _trail.minVertexDistance = 0.05f;
            _trail.numCornerVertices = 2;
            _trail.numCapVertices = 2;
            _trail.shadowCastingMode = ShadowCastingMode.Off;
            _trail.receiveShadows = false;
            _trail.alignment = LineAlignment.View;
            _trail.emitting = false;

            float compactW = width * 0.52f;
            var curve = new AnimationCurve(
                new Keyframe(0f, compactW),
                new Keyframe(0.35f, compactW * 0.78f),
                new Keyframe(1f, 0.0f));
            _trail.widthCurve = curve;

            Shader sh = Resources.Load<Shader>("Shaders/AbyssalWeaponTrail");
            if (sh == null) sh = Shader.Find("AbyssalDungeon/WeaponTrail");
            if (sh != null)
            {
                _mat = new Material(sh);
                _mat.SetColor("_MainColor", Color.Lerp(accent, Color.white, 0.35f) * 0.85f);
                _mat.SetColor("_EdgeColor", accent * 0.85f);
                _mat.SetFloat("_Progress", 0.35f);
                _mat.SetFloat("_Opacity", 0.48f);
            }
            else
            {
                _mat = CharacterVisualFactory.MakeTransparentMat(new Color(accent.r, accent.g, accent.b, 0.44f), accent * 1.15f);
            }
            _trail.sharedMaterial = _mat;
        }

        public void Activate(float duration, Color customColor = default)
        {
            if (_trail == null) _trail = GetComponent<TrailRenderer>();
            if (_trail == null) return;
            if (_mat == null) _mat = _trail.sharedMaterial;
            Color c = customColor.a > 0.01f ? customColor : _defaultColor;
            if (_mat != null)
            {
                if (_mat.HasProperty("_MainColor")) _mat.SetColor("_MainColor", Color.Lerp(c, Color.white, 0.48f));
                if (_mat.HasProperty("_EdgeColor")) _mat.SetColor("_EdgeColor", c);
                if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", new Color(c.r, c.g, c.b, 0.78f));
            }
            if (!_trail.emitting) _trail.Clear();
            _trail.emitting = true;
            _emitTimer = Mathf.Max(_emitTimer, duration);
        }

        private void Update()
        {
            if (_emitTimer > 0f)
            {
                _emitTimer -= Time.deltaTime;
                if (_emitTimer <= 0f && _trail != null)
                    _trail.emitting = false;
            }
        }
    }
}

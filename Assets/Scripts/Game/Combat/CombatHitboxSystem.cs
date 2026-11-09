using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AbyssGame
{
    /// <summary>
    /// Dedicated 3D Hurtbox Volume attached to every ActorUnit (Ignore Raycast layer 2, Trigger CapsuleCollider).
    /// Enables true 3D spatial weapon-hurtbox sweeps and exact surface contact points for hit sparks without
    /// obstructing camera collision or ground aim raycasts.
    /// </summary>
    public class ActorHurtbox : MonoBehaviour
    {
        public ActorUnit Owner { get; private set; }
        public CapsuleCollider Capsule { get; private set; }

        public static ActorHurtbox AttachTo(ActorUnit unit, float radius, float height)
        {
            if (unit == null) return null;
            Transform existing = unit.transform.Find("HurtboxVolume");
            GameObject go = existing != null ? existing.gameObject : new GameObject("HurtboxVolume");
            go.transform.SetParent(unit.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.layer = 2; // Ignore Raycast so camera/aim rays pass cleanly

            var hb = go.GetComponent<ActorHurtbox>();
            if (hb == null) hb = go.AddComponent<ActorHurtbox>();
            hb.Owner = unit;

            var cap = go.GetComponent<CapsuleCollider>();
            if (cap == null) cap = go.AddComponent<CapsuleCollider>();
            cap.isTrigger = true;
            cap.radius = Mathf.Max(0.32f, radius * 1.12f);
            cap.height = Mathf.Max(1.2f, height * 1.04f);
            cap.center = new Vector3(0f, cap.height * 0.5f, 0f);
            hb.Capsule = cap;
            return hb;
        }

        public Vector3 ClosestImpactPoint(Vector3 fromWorld)
        {
            if (Capsule != null && Capsule.enabled && gameObject.activeInHierarchy)
            {
                return Capsule.ClosestPoint(fromWorld);
            }
            return transform.position + Vector3.up * 1.05f;
        }
    }

    /// <summary>
    /// Attaches a sleek URP additive blade ribbon TrailRenderer to any melee weapon tip.
    /// Emits only during active attack swings, Heavy Attacks, and Dash skills.
    /// </summary>
    public class WeaponBladeTrail3D : MonoBehaviour
    {
        private TrailRenderer _trail;
        private Material _mat;
        private Color _defaultColor;
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
            _trail.time = 0.17f;
            _trail.minVertexDistance = 0.04f;
            _trail.numCornerVertices = 2;
            _trail.numCapVertices = 2;
            _trail.shadowCastingMode = ShadowCastingMode.Off;
            _trail.receiveShadows = false;
            _trail.alignment = LineAlignment.View;
            _trail.emitting = false;

            var curve = new AnimationCurve(
                new Keyframe(0f, width),
                new Keyframe(0.35f, width * 0.82f),
                new Keyframe(1f, 0.0f));
            _trail.widthCurve = curve;

            Shader sh = Resources.Load<Shader>("Shaders/AbyssalWeaponTrail");
            if (sh == null) sh = Shader.Find("AbyssalDungeon/WeaponTrail");
            if (sh != null)
            {
                _mat = new Material(sh);
                _mat.SetColor("_MainColor", Color.Lerp(accent, Color.white, 0.45f));
                _mat.SetColor("_EdgeColor", accent);
                _mat.SetFloat("_Progress", 0.35f);
                _mat.SetFloat("_Opacity", 0.92f);
            }
            else
            {
                _mat = CharacterVisualFactory.MakeTransparentMat(new Color(accent.r, accent.g, accent.b, 0.72f), accent * 2f);
            }
            _trail.sharedMaterial = _mat;
        }

        public void Activate(float duration, Color customColor = default)
        {
            if (_trail == null) return;
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

        private void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }
    }

    /// <summary>
    /// Active-Frame 3D Hitbox Sweep (unity-physics skill):
    /// Evaluates continuous Physics.OverlapSphereNonAlloc intersection against ActorHurtbox trigger capsules
    /// across the weapon swing's active window [windup -> windup + activeDuration], guaranteeing each enemy
    /// is struck at most once per swing and reporting exact 3D surface contact points for VFX sparks.
    /// </summary>
    public class WeaponHitboxSweep3D : MonoBehaviour
    {
        private struct ActiveSweep
        {
            public float windupRemaining;
            public float activeRemaining;
            public float reachRadius;
            public float cosHalfArc;
            public Action<ActorUnit, Vector3> onHitVictim;
            public Action onWindupComplete;
            public bool startedActive;
            public HashSet<ActorUnit> hitSet;
        }

        private ActorUnit _owner;
        private readonly List<ActiveSweep> _sweeps = new List<ActiveSweep>(4);
        private static readonly Collider[] OverlapBuffer = new Collider[48];

        public static WeaponHitboxSweep3D EnsureOn(ActorUnit owner)
        {
            if (owner == null) return null;
            var s = owner.GetComponent<WeaponHitboxSweep3D>();
            if (s == null) s = owner.gameObject.AddComponent<WeaponHitboxSweep3D>();
            s._owner = owner;
            return s;
        }

        public void BeginSweep(
            float windupDelay,
            float activeDuration,
            float reachRadius,
            float arcDegrees,
            Action onWindupComplete,
            Action<ActorUnit, Vector3> onHitVictim)
        {
            if (_owner == null) _owner = GetComponent<ActorUnit>();
            _sweeps.Add(new ActiveSweep
            {
                windupRemaining = Mathf.Max(0f, windupDelay),
                activeRemaining = Mathf.Max(0.04f, activeDuration),
                reachRadius = Mathf.Max(0.8f, reachRadius),
                cosHalfArc = Mathf.Cos(Mathf.Clamp(arcDegrees, 15f, 360f) * 0.5f * Mathf.Deg2Rad),
                onWindupComplete = onWindupComplete,
                onHitVictim = onHitVictim,
                startedActive = false,
                hitSet = new HashSet<ActorUnit>()
            });
        }

        public void CancelAllSweeps()
        {
            _sweeps.Clear();
        }

        private void Update()
        {
            if (_owner == null || _owner.IsDead || _owner.IsDowned || _owner.IsCrowdControlled())
            {
                if (_sweeps.Count > 0) _sweeps.Clear();
                return;
            }

            float dt = Time.deltaTime;
            for (int i = _sweeps.Count - 1; i >= 0; i--)
            {
                ActiveSweep sw = _sweeps[i];
                if (sw.windupRemaining > 0f)
                {
                    sw.windupRemaining -= dt;
                    if (sw.windupRemaining > 0f)
                    {
                        _sweeps[i] = sw;
                        continue;
                    }
                }

                if (!sw.startedActive)
                {
                    sw.startedActive = true;
                    sw.onWindupComplete?.Invoke();
                }

                EvaluateSweepFrame(ref sw);

                sw.activeRemaining -= dt;
                if (sw.activeRemaining <= 0f)
                {
                    // Final safety pass against AllUnits in case a freshly spawned summon lacked synced physics bounds
                    EvaluateFallbackPass(ref sw);
                    _sweeps.RemoveAt(i);
                }
                else
                {
                    _sweeps[i] = sw;
                }
            }
        }

        private void EvaluateSweepFrame(ref ActiveSweep sw)
        {
            Vector3 origin = _owner.transform.position + Vector3.up * 1.0f;
            Vector3 fwd = _owner.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            fwd.Normalize();

            int count = Physics.OverlapSphereNonAlloc(origin, sw.reachRadius + 0.55f, OverlapBuffer, ~0, QueryTriggerInteraction.Collide);
            for (int c = 0; c < count; c++)
            {
                Collider col = OverlapBuffer[c];
                if (col == null) continue;

                ActorHurtbox hb = col.GetComponent<ActorHurtbox>();
                ActorUnit victim = hb != null ? hb.Owner : col.GetComponentInParent<ActorUnit>();
                if (victim == null || victim == _owner || victim.Team == _owner.Team || victim.IsDead || victim.IsDowned)
                    continue;
                if (sw.hitSet.Contains(victim))
                    continue;

                Vector3 toVictim = victim.transform.position - _owner.transform.position;
                toVictim.y = 0f;
                float planarDist = toVictim.magnitude;
                float victimRad = victim.Controller != null ? victim.Controller.radius : 0.45f;
                if (planarDist > sw.reachRadius + victimRad)
                    continue;

                if (planarDist >= 0.62f && Vector3.Dot(fwd, toVictim / planarDist) < sw.cosHalfArc)
                    continue;

                sw.hitSet.Add(victim);
                Vector3 bladeCenter = origin + fwd * Mathf.Min(planarDist, sw.reachRadius * 0.65f);
                Vector3 contact = hb != null
                    ? hb.ClosestImpactPoint(bladeCenter)
                    : Vector3.Lerp(bladeCenter, victim.transform.position + Vector3.up * 1.05f, 0.65f);

                sw.onHitVictim?.Invoke(victim, contact);
            }
        }

        private void EvaluateFallbackPass(ref ActiveSweep sw)
        {
            Vector3 fwd = _owner.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            fwd.Normalize();

            for (int i = ActorUnit.AllUnits.Count - 1; i >= 0; i--)
            {
                ActorUnit victim = ActorUnit.AllUnits[i];
                if (victim == null || victim == _owner || victim.Team == _owner.Team || victim.IsDead || victim.IsDowned)
                    continue;
                if (sw.hitSet.Contains(victim))
                    continue;

                Vector3 toVictim = victim.transform.position - _owner.transform.position;
                toVictim.y = 0f;
                float d = toVictim.magnitude;
                float rExtra = victim.Controller != null ? victim.Controller.radius : 0.45f;
                if (d <= sw.reachRadius + rExtra && (d < 0.62f || Vector3.Dot(fwd, toVictim / d) >= sw.cosHalfArc))
                {
                    sw.hitSet.Add(victim);
                    Vector3 contact = victim.transform.position + Vector3.up * 1.05f - fwd * (rExtra * 0.5f);
                    sw.onHitVictim?.Invoke(victim, contact);
                }
            }
        }
    }
}


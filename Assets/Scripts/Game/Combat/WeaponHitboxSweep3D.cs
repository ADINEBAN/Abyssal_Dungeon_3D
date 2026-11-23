using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssGame
{
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

        [SerializeField] private ActorUnit _owner;
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
            if (_owner == null) _owner = GetComponent<ActorUnit>();
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
                if (victim == null || victim == _owner || victim.Team == _owner.Team || victim.IsDead || victim.IsDowned || victim.IsRoomDormant)
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
                if (victim == null || victim == _owner || victim.Team == _owner.Team || victim.IsDead || victim.IsDowned || victim.IsRoomDormant)
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

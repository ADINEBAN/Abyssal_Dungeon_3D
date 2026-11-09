using System.Collections.Generic;
using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// AAA Action-RPG Target Lock-On System (Section 3.1):
    /// - Toggle Lock-On via [Tab] or [Middle Mouse Button (MMB)]
    /// - Switch targets Left/Right in screen space via [~], [Alt+Q / Alt+E], or sharp mouse flick
    /// - Automatically transfers Lock-On to nearest visible enemy on target death
    /// - Renders a 3D counter-rotating Arcane Reticle & overhead Lock Diamond on the locked target.
    /// </summary>
    public class CameraTargetLockSystem : MonoBehaviour
    {
        public static CameraTargetLockSystem Instance { get; private set; }

        public ActorUnit OwnerHero;
        public ActorUnit LockedTarget { get; private set; }
        public float MaxLockDistance = 24f;
        public float BreakLockDistance = 28f;

        private GameObject _reticleRoot;
        private Transform _groundRing;
        private Transform _chestDiamond;
        private MeshRenderer _ringRenderer;
        private MeshRenderer _diamondRenderer;
        private float _losLostTimer;
        private float _flickCooldown;
        private readonly List<ActorUnit> _candidates = new List<ActorUnit>(32);

        public static CameraTargetLockSystem Ensure(ActorUnit ownerHero)
        {
            if (Instance != null)
            {
                Instance.OwnerHero = ownerHero;
                return Instance;
            }
            var go = new GameObject("CameraTargetLockSystem");
            if (ownerHero != null && ownerHero.transform.parent != null)
                go.transform.SetParent(ownerHero.transform.parent, false);
            Instance = go.AddComponent<CameraTargetLockSystem>();
            Instance.OwnerHero = ownerHero;
            Instance.BuildReticleVisuals();
            return Instance;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_reticleRoot != null) Destroy(_reticleRoot);
        }

        public bool IsLocked => LockedTarget != null && !LockedTarget.IsDead && !LockedTarget.IsDowned;

        public void ToggleLockOn()
        {
            if (IsLocked)
            {
                ClearLock();
                GameAudio.I.PlaySfx(GameSfx.Click, 0.55f);
                return;
            }

            ActorUnit best = FindBestCandidate();
            if (best != null)
            {
                SetLockedTarget(best);
                GameAudio.I.PlaySfx(GameSfx.LockOn, 0.75f);
            }
        }

        public void CycleTarget(int screenDirection)
        {
            if (!IsLocked || OwnerHero == null) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            GatherValidCandidates();
            if (_candidates.Count <= 1) return;

            Vector3 curVp = cam.WorldToViewportPoint(LockedTarget.transform.position + Vector3.up * 1.1f);
            ActorUnit bestNext = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < _candidates.Count; i++)
            {
                ActorUnit c = _candidates[i];
                if (c == LockedTarget) continue;

                Vector3 vp = cam.WorldToViewportPoint(c.transform.position + Vector3.up * 1.1f);
                if (vp.z <= 0.1f) continue;

                float dx = vp.x - curVp.x;
                if (screenDirection != 0 && Mathf.Sign(dx) != Mathf.Sign(screenDirection))
                    continue;

                float distScore = Mathf.Abs(dx) * 2.2f + Mathf.Abs(vp.y - curVp.y) * 0.8f;
                if (distScore < bestScore)
                {
                    bestScore = distScore;
                    bestNext = c;
                }
            }

            // Fallback wrap-around if none strictly in that direction
            if (bestNext == null)
            {
                for (int i = 0; i < _candidates.Count; i++)
                {
                    if (_candidates[i] != LockedTarget)
                    {
                        bestNext = _candidates[i];
                        break;
                    }
                }
            }

            if (bestNext != null && bestNext != LockedTarget)
            {
                SetLockedTarget(bestNext);
                GameAudio.I.PlaySfx(GameSfx.LockOn, 0.65f);
            }
        }

        public void ClearLock()
        {
            LockedTarget = null;
            _losLostTimer = 0f;
            if (_reticleRoot != null) _reticleRoot.SetActive(false);
        }

        private void SetLockedTarget(ActorUnit target)
        {
            LockedTarget = target;
            _losLostTimer = 0f;
            if (_reticleRoot == null) BuildReticleVisuals();
            _reticleRoot.SetActive(target != null);

            if (target != null)
            {
                bool highThreat = target.IsBoss || target.IsMiniBoss || target.IsElite;
                Color col = highThreat ? new Color(1.0f, 0.32f, 0.24f, 0.90f) : new Color(0.32f, 0.88f, 1.0f, 0.88f);
                Material mat = CharacterVisualFactory.MakeTransparentMat(col, col * 2.4f);
                if (_ringRenderer != null) _ringRenderer.sharedMaterial = mat;
                if (_diamondRenderer != null) _diamondRenderer.sharedMaterial = mat;
            }
        }

        private void Update()
        {
            if (_flickCooldown > 0f) _flickCooldown -= Time.deltaTime;

            if (OwnerHero == null || OwnerHero.IsDead)
            {
                if (LockedTarget != null) ClearLock();
                return;
            }

            if (LockedTarget != null)
            {
                // Auto-switch when locked enemy dies
                if (LockedTarget.IsDead || LockedTarget.IsDowned)
                {
                    ActorUnit next = FindBestCandidate(maxRangeOverride: 16f);
                    if (next != null) SetLockedTarget(next);
                    else ClearLock();
                    return;
                }

                float dist = Vector3.Distance(OwnerHero.transform.position, LockedTarget.transform.position);
                if (dist > BreakLockDistance)
                {
                    ClearLock();
                    return;
                }

                if (!HasLineOfSight(OwnerHero.transform.position + Vector3.up * 1.4f, LockedTarget.transform.position + Vector3.up * 1.2f))
                {
                    _losLostTimer += Time.deltaTime;
                    if (_losLostTimer > 2.2f)
                    {
                        ClearLock();
                        return;
                    }
                }
                else
                {
                    _losLostTimer = 0f;
                }

                // Sharp mouse horizontal flick switches target cleanly while locked
                float mx = Input.GetAxis("Mouse X");
                if (_flickCooldown <= 0f && Mathf.Abs(mx) > 3.6f && Cursor.lockState == CursorLockMode.Locked)
                {
                    _flickCooldown = 0.35f;
                    CycleTarget(mx > 0f ? 1 : -1);
                }

                UpdateReticleTransform();
            }
        }

        private void UpdateReticleTransform()
        {
            if (_reticleRoot == null || LockedTarget == null) return;
            if (!_reticleRoot.activeSelf) _reticleRoot.SetActive(true);

            Vector3 p = LockedTarget.transform.position;
            _reticleRoot.transform.position = p;

            float rad = LockedTarget.Controller != null ? LockedTarget.Controller.radius * 2.5f : 1.35f;
            float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.06f;

            if (_groundRing != null)
            {
                _groundRing.localPosition = new Vector3(0f, 0.06f, 0f);
                _groundRing.localScale = new Vector3(rad * pulse, 0.03f, rad * pulse);
                _groundRing.Rotate(0f, 85f * Time.deltaTime, 0f, Space.Self);
            }

            if (_chestDiamond != null)
            {
                float h = LockedTarget.Controller != null ? LockedTarget.Controller.height * 0.62f : 1.25f;
                _chestDiamond.localPosition = new Vector3(0f, h + Mathf.Sin(Time.time * 4.5f) * 0.08f, 0f);
                _chestDiamond.Rotate(0f, -135f * Time.deltaTime, 0f, Space.World);
            }
        }

        private ActorUnit FindBestCandidate(float maxRangeOverride = -1f)
        {
            GatherValidCandidates(maxRangeOverride > 0f ? maxRangeOverride : MaxLockDistance);
            if (_candidates.Count == 0) return null;

            Camera cam = Camera.main;
            Vector3 heroPos = OwnerHero.transform.position;
            Vector3 camFwd = cam != null ? cam.transform.forward : OwnerHero.transform.forward;

            ActorUnit best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < _candidates.Count; i++)
            {
                ActorUnit c = _candidates[i];
                Vector3 targetCenter = c.transform.position + Vector3.up * 1.1f;
                float dist = Vector3.Distance(heroPos, c.transform.position);

                float angle = cam != null
                    ? Vector3.Angle(camFwd, (targetCenter - cam.transform.position).normalized)
                    : Vector3.Angle(OwnerHero.transform.forward, (c.transform.position - heroPos).normalized);

                if (angle > 68f) continue;

                float priorityBonus = c.IsBoss ? -8f : (c.IsMiniBoss ? -5f : (c.IsElite ? -2.5f : 0f));
                float score = dist * 0.65f + angle * 0.32f + priorityBonus;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }

            return best != null ? best : _candidates[0];
        }

        private void GatherValidCandidates(float maxDist = -1f)
        {
            _candidates.Clear();
            if (OwnerHero == null) return;
            float limit = maxDist > 0f ? maxDist : MaxLockDistance;
            Vector3 origin = OwnerHero.transform.position + Vector3.up * 1.4f;

            for (int i = 0; i < ActorUnit.AllUnits.Count; i++)
            {
                ActorUnit u = ActorUnit.AllUnits[i];
                if (u == null || u.Team == OwnerHero.Team || u.IsDead || u.IsDowned) continue;
                float d = Vector3.Distance(OwnerHero.transform.position, u.transform.position);
                if (d > limit) continue;
                if (!HasLineOfSight(origin, u.transform.position + Vector3.up * 1.2f)) continue;
                _candidates.Add(u);
            }
        }

        private static bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            float dist = dir.magnitude;
            if (dist < 0.25f) return true;
            if (Physics.Raycast(from, dir / dist, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<ActorUnit>() == null)
                    return false;
            }
            return true;
        }

        private void BuildReticleVisuals()
        {
            _reticleRoot = new GameObject("TargetLockReticle3D");
            DontDestroyOnLoad(_reticleRoot);

            Color col = new Color(0.32f, 0.88f, 1.0f, 0.88f);
            Material mat = CharacterVisualFactory.MakeTransparentMat(col, col * 2.4f);

            var ringGo = CharacterVisualFactory.Prim(PrimitiveType.Cylinder, "LockGroundRing", _reticleRoot.transform,
                new Vector3(0f, 0.06f, 0f), new Vector3(1.4f, 0.03f, 1.4f), mat);
            _groundRing = ringGo.transform;
            _ringRenderer = ringGo.GetComponent<MeshRenderer>();

            var diaGo = CharacterVisualFactory.Prim(PrimitiveType.Cube, "LockDiamond", _reticleRoot.transform,
                new Vector3(0f, 1.4f, 0f), new Vector3(0.28f, 0.28f, 0.28f), mat, Quaternion.Euler(45f, 0f, 45f));
            _chestDiamond = diaGo.transform;
            _diamondRenderer = diaGo.GetComponent<MeshRenderer>();

            _reticleRoot.SetActive(false);
        }
    }
}


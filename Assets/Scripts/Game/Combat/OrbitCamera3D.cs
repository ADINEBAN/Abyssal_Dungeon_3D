using UnityEngine;
using UnityEngine.EventSystems;

namespace AbyssGame
{
    public interface IWorldInteractable
    {
        Vector3 Position { get; }
        float InteractRadius { get; }
        string GetPrompt();
        bool CanInteract();
        void Interact(ActorUnit user);
    }

    /// <summary>
    /// AAA Third-Person Action-RPG Camera (Phase 2 - Section 3.1):
    /// - Frame-rate-independent pivot SmoothDamp + micro-deadzone + planar velocity Look-Ahead
    /// - Over-the-shoulder soft Target Lock-On framing with CameraTargetLockSystem
    /// - Zero-wall-clip SphereCastNonAlloc with asymmetric collision spring (fast snap-in, smooth ease-out)
    /// - Sampled harmonic/Perlin trauma shake + dynamic Sprint/Dodge FOV kick + persistent Zoom & Invert-Y.
    /// </summary>
    [DefaultExecutionOrder(20)]
    public class OrbitCamera3D : MonoBehaviour
    {
        public static OrbitCamera3D Instance { get; private set; }

        public Transform Target;
        public float Yaw = 0f;
        public float Pitch = 24f;
        public float Distance = 7.8f;
        public Vector3 PivotOffset = new Vector3(0f, 1.55f, 0f);
        public bool AllowCursorLock = true;
        public bool UIBlockingCursor = false;

        private Camera _cam;
        private Transform _cachedTargetTransform;
        private ActorUnit _cachedTargetUnit;
        private float _baseFov = 58f;
        private float _fovKick;
        private float _fovKickVel;

        private bool _pivotInitialized;
        private Vector3 _smoothedPivot;
        private Vector3 _pivotVelXz;
        private float _pivotVelY;
        private Vector3 _lastTargetPos;
        private Vector3 _lookAheadOffset;
        private Vector3 _lookAheadVel;
        private Vector3 _shoulderOffset;

        private float _orbitDistance = 7.8f;
        private float _currentCollisionDist = 7.8f;
        private float _lastProfileCamDist = -1f;
        private float _saveZoomTimer = -1f;

        private float _trauma;
        private float _shakeAmpScale = 0.28f;
        private float _noiseTime;

        private readonly RaycastHit[] _camHits = new RaycastHit[16];

        private void Awake()
        {
            Instance = this;
            _cam = GetComponent<Camera>();
            if (_cam != null) _baseFov = Mathf.Clamp(_cam.fieldOfView, 50f, 70f);
        }

        private void Start()
        {
            var prof = GameSession.I != null ? GameSession.I.Profile : null;
            if (prof != null && prof.cameraDistance >= 3.4f && prof.cameraDistance <= 15f)
            {
                Distance = prof.cameraDistance;
                _lastProfileCamDist = prof.cameraDistance;
            }
            _orbitDistance = Distance;
            _currentCollisionDist = Distance;

            if (AllowCursorLock && !UIBlockingCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void Shake(float amp = 0.16f, float dur = 0.18f)
        {
            float clampedAmp = Mathf.Clamp(amp, 0.04f, 0.32f);
            float addTrauma = Mathf.Clamp01(clampedAmp * 1.9f);
            _trauma = Mathf.Clamp01(Mathf.Max(_trauma, addTrauma) + addTrauma * 0.25f);
            _shakeAmpScale = Mathf.Clamp(clampedAmp * 0.92f, 0.08f, 0.28f);
        }

        public void PunchFov(float deltaFov = 3.2f, float dur = 0.24f)
        {
            _fovKick = Mathf.Clamp(_fovKick + deltaFov, -6f, 8f);
        }

        private void LateUpdate()
        {
            if (Target == null) return;

            float dt = Time.unscaledDeltaTime > 0.0001f ? Mathf.Min(0.05f, Time.unscaledDeltaTime) : 0.016f;
            float simDt = Time.deltaTime;
            var prof = GameSession.I != null ? GameSession.I.Profile : null;

            // Sync camera distance if changed in Settings Modal
            if (prof != null && Mathf.Abs(prof.cameraDistance - _lastProfileCamDist) > 0.02f)
            {
                Distance = Mathf.Clamp(prof.cameraDistance, 3.6f, 14.5f);
                _lastProfileCamDist = Distance;
            }

            if (_saveZoomTimer > 0f)
            {
                _saveZoomTimer -= dt;
                if (_saveZoomTimer <= 0f && prof != null)
                {
                    prof.cameraDistance = Distance;
                    _lastProfileCamDist = Distance;
                    GameSession.I.SaveProfile();
                }
            }

            bool holdAlt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            bool wantUnlocked = UIBlockingCursor || holdAlt || Time.timeScale < 0.01f;

            if (AllowCursorLock)
            {
                if (wantUnlocked)
                {
                    if (Cursor.lockState != CursorLockMode.None)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                }
                else if (Input.GetMouseButtonDown(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            var lockSys = CameraTargetLockSystem.Instance;
            bool isLockedOn = lockSys != null && lockSys.IsLocked;

            bool rotating = (Cursor.lockState == CursorLockMode.Locked) || Input.GetMouseButton(1);
            if (rotating && Time.timeScale > 0.01f)
            {
                float sens = prof != null ? prof.mouseSens : 2.2f;
                float ySign = (prof != null && prof.invertCamY) ? 1f : -1f;
                float mx = Input.GetAxis("Mouse X") * sens * 1.6f;
                float my = Input.GetAxis("Mouse Y") * sens * 1.3f * ySign;

                // While locked-on, dampen manual yaw so soft lock framing stays anchored without fighting sharp flicks
                Yaw += isLockedOn ? mx * 0.28f : mx;
                Pitch = Mathf.Clamp(Pitch + my, 6f, 68f);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f && !UIBlockingCursor)
            {
                Distance = Mathf.Clamp(Distance - scroll * 4.5f, 3.6f, 14.5f);
                if (prof != null)
                {
                    prof.cameraDistance = Distance;
                    _lastProfileCamDist = Distance;
                    _saveZoomTimer = 0.45f;
                }
            }

            _orbitDistance = Mathf.Lerp(_orbitDistance, Distance, 1f - Mathf.Exp(-14f * dt));

            // 1. Pivot SmoothDamp directly on Update()-driven Target.position (zero FixedUpdate mismatch)
            if (_cachedTargetTransform != Target)
            {
                _cachedTargetTransform = Target;
                _cachedTargetUnit = Target != null ? Target.GetComponent<ActorUnit>() : null;
            }
            Vector3 targetPos = Target.position;
            if (!_pivotInitialized || Vector3.Distance(targetPos, _lastTargetPos) > 6.5f)
            {
                _pivotInitialized = true;
                _lastTargetPos = targetPos;
                _smoothedPivot = targetPos + PivotOffset;
                _pivotVelXz = Vector3.zero;
                _pivotVelY = 0f;
                _lookAheadOffset = Vector3.zero;
                _currentCollisionDist = _orbitDistance;
            }

            Vector3 planarVel = simDt > 0.0001f
                ? Vector3.ProjectOnPlane(targetPos - _lastTargetPos, Vector3.up) / simDt
                : Vector3.zero;
            _lastTargetPos = targetPos;

            Vector3 desiredLookAhead = Vector3.zero;
            if (isLockedOn && lockSys.LockedTarget != null)
            {
                Vector3 toLock = lockSys.LockedTarget.transform.position - targetPos;
                toLock.y = 0f;
                desiredLookAhead = Vector3.ClampMagnitude(toLock * 0.14f, 0.90f);
            }
            else if (planarVel.sqrMagnitude > 0.4f)
            {
                desiredLookAhead = Vector3.ClampMagnitude(planarVel * 0.075f, 0.50f);
            }
            _lookAheadOffset = Vector3.SmoothDamp(_lookAheadOffset, desiredLookAhead, ref _lookAheadVel, 0.20f, 12f, dt);

            Vector3 rawAnchor = targetPos + PivotOffset + _lookAheadOffset;
            _smoothedPivot = Vector3.SmoothDamp(_smoothedPivot, rawAnchor, ref _pivotVelXz, 0.055f, 80f, dt);

            // 2. Soft Target Lock-On Framing (over-the-shoulder toward locked enemy)
            if (isLockedOn && lockSys.LockedTarget != null && Time.timeScale > 0.01f)
            {
                Vector3 lockFocus = lockSys.LockedTarget.transform.position + Vector3.up * 1.25f;
                Vector3 toEnemy = lockFocus - _smoothedPivot;
                float flatDist = new Vector2(toEnemy.x, toEnemy.z).magnitude;
                if (flatDist > 0.35f)
                {
                    float desiredYaw = Mathf.Atan2(toEnemy.x, toEnemy.z) * Mathf.Rad2Deg;
                    Yaw = Mathf.LerpAngle(Yaw, desiredYaw, 1f - Mathf.Exp(-7.5f * simDt));

                    float desiredPitch = Mathf.Clamp(22f + Mathf.Clamp01(1f - flatDist / 16f) * 11f, 16f, 38f);
                    Pitch = Mathf.Lerp(Pitch, desiredPitch, 1f - Mathf.Exp(-4.5f * simDt));
                }
            }

            Quaternion rot = Quaternion.Euler(Pitch, Yaw, 0f);

            // Subtle over-right-shoulder offset when Locked-On so Hero silhouette never blocks target
            Vector3 desiredShoulder = isLockedOn ? (rot * new Vector3(0.42f, 0.10f, 0f)) : Vector3.zero;
            _shoulderOffset = Vector3.Lerp(_shoulderOffset, desiredShoulder, 1f - Mathf.Exp(-10f * dt));

            Vector3 camPivot = _smoothedPivot + _shoulderOffset;
            // Ensure shoulder offset itself never crosses a side wall
            Vector3 headOrigin = targetPos + PivotOffset;
            Vector3 toPivot = camPivot - headOrigin;
            float toPivotLen = toPivot.magnitude;
            if (toPivotLen > 0.05f && TryFindWallHit(headOrigin, toPivot / toPivotLen, toPivotLen, 0.18f, out float pivotHitDist))
            {
                camPivot = headOrigin + (toPivot / toPivotLen) * Mathf.Max(0.0f, pivotHitDist - 0.12f);
            }

            // 3. Anti-Wall-Clip SphereCast + Asymmetric Collision Spring
            Vector3 backDir = -(rot * Vector3.forward);
            float unobstructedDist = _orbitDistance;
            if (TryFindWallHit(camPivot, backDir, _orbitDistance, 0.26f, out float wallHitDist))
            {
                unobstructedDist = Mathf.Clamp(wallHitDist - 0.22f, 1.05f, _orbitDistance);
            }

            if (unobstructedDist < _currentCollisionDist)
                _currentCollisionDist = Mathf.Lerp(_currentCollisionDist, unobstructedDist, 1f - Mathf.Exp(-42f * dt)); // Snap IN fast
            else
                _currentCollisionDist = Mathf.Lerp(_currentCollisionDist, unobstructedDist, 1f - Mathf.Exp(-7.5f * dt)); // Ease OUT smoothly

            Vector3 finalPos = camPivot + backDir * _currentCollisionDist;
            Quaternion finalRot = rot;

            // 4. Sampled Harmonic Trauma Shake (additive offset AFTER collision — never fights follow!)
            if (_trauma > 0.001f)
            {
                _trauma = Mathf.Max(0f, _trauma - 2.4f * dt);
                float shake = _trauma * _trauma;
                _noiseTime += dt * 32f;
                Vector3 localShake = new Vector3(
                    Mathf.Sin(_noiseTime * 1.7f) * _shakeAmpScale * shake,
                    Mathf.Cos(_noiseTime * 2.3f) * _shakeAmpScale * 0.8f * shake,
                    0f);
                finalPos += rot * localShake;
                finalRot = rot * Quaternion.Euler(0f, 0f, Mathf.Sin(_noiseTime * 1.3f) * 2.2f * shake);
            }

            transform.position = finalPos;
            transform.rotation = finalRot;

            // 5. Dynamic FOV (Sprint speed sensation + Dodge FOV punch + Boss Lock focus)
            if (_cam != null)
            {
                ActorUnit hero = _cachedTargetUnit;
                float sprintFov = (hero != null && hero.IsSprinting) ? 4.5f : 0f;
                float lockFov = (isLockedOn && lockSys.LockedTarget != null && lockSys.LockedTarget.IsBoss) ? -1.8f : 0f;
                _fovKick = Mathf.SmoothDamp(_fovKick, 0f, ref _fovKickVel, 0.18f, 40f, dt);
                float targetFov = Mathf.Clamp(_baseFov + sprintFov + lockFov + _fovKick, 48f, 74f);
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFov, 1f - Mathf.Exp(-10f * dt));
            }
        }

        private bool TryFindWallHit(Vector3 origin, Vector3 dir, float maxDist, float radius, out float closestDist)
        {
            closestDist = maxDist;
            if (maxDist <= 0.05f) return false;

            int count = Physics.SphereCastNonAlloc(origin, radius, dir, _camHits, maxDist, ~0, QueryTriggerInteraction.Ignore);
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                var h = _camHits[i];
                if (h.collider == null) continue;
                if (h.collider.GetComponentInParent<ActorUnit>() != null) continue;

                // When initial sphere overlaps geometry, SphereCast reports distance 0; verify with thin ray
                float d = h.distance;
                if (d <= 0.001f)
                {
                    if (Physics.Raycast(origin, dir, out RaycastHit rh, maxDist, ~0, QueryTriggerInteraction.Ignore)
                        && rh.collider != null && rh.collider.GetComponentInParent<ActorUnit>() == null)
                    {
                        d = rh.distance;
                    }
                    else continue;
                }

                if (d < closestDist)
                {
                    closestDist = d;
                    found = true;
                }
            }
            return found;
        }
    }
}

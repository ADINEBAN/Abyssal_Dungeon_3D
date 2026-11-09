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

        public void Shake(float amp = 0.25f, float dur = 0.22f)
        {
            float addTrauma = Mathf.Clamp01(amp * 2.1f);
            _trauma = Mathf.Clamp01(Mathf.Max(_trauma, addTrauma) + addTrauma * 0.35f);
            _shakeAmpScale = Mathf.Max(0.18f, amp * 1.15f);
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

            // 1. Pivot SmoothDamp + Teleport Guard + Planar Look-Ahead
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
                desiredLookAhead = Vector3.ClampMagnitude(toLock * 0.14f, 0.95f);
            }
            else if (planarVel.sqrMagnitude > 0.4f)
            {
                desiredLookAhead = Vector3.ClampMagnitude(planarVel * 0.085f, 0.58f);
            }
            _lookAheadOffset = Vector3.SmoothDamp(_lookAheadOffset, desiredLookAhead, ref _lookAheadVel, 0.20f, 12f, dt);

            Vector3 rawAnchor = targetPos + PivotOffset + _lookAheadOffset;
            Vector3 curXz = new Vector3(_smoothedPivot.x, 0f, _smoothedPivot.z);
            Vector3 dstXz = new Vector3(rawAnchor.x, 0f, rawAnchor.z);

            // Subtle 4cm micro-deadzone when stationary prevents camera micro-jitter
            if (planarVel.sqrMagnitude > 0.05f || Vector3.Distance(curXz, dstXz) > 0.04f)
            {
                curXz = Vector3.SmoothDamp(curXz, dstXz, ref _pivotVelXz, 0.052f, 60f, dt);
            }
            float newPivotY = Mathf.SmoothDamp(_smoothedPivot.y, rawAnchor.y, ref _pivotVelY, 0.085f, 40f, dt);
            _smoothedPivot = new Vector3(curXz.x, newPivotY, curXz.z);

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
                ActorUnit hero = Target.GetComponent<ActorUnit>();
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

    /// <summary>
    /// Responsive Third-Person Action-RPG Input Controller (Phase 2 - Section 3.1):
    /// - Named action abstraction + radial deadzone + 180ms HeroInputBuffer
    /// - Idle / Walk [CapsLock] / Run [WASD] / Sprint [Hold LeftShift] / Jump [V] / Dodge [Space or Tap Shift]
    /// - Target Lock-On [Tab / MMB] + Left/Right Target Cycling [~ / Alt+Q / Alt+E] + Lock-On Combat Strafe.
    /// </summary>
    [RequireComponent(typeof(ActorUnit))]
    public class HeroPlayerInput : MonoBehaviour
    {
        public static readonly System.Collections.Generic.List<IWorldInteractable> Interactables = new System.Collections.Generic.List<IWorldInteractable>();

        public ActorUnit Unit { get; private set; }
        public float PotionCooldown;
        public IWorldInteractable NearestInteractable { get; private set; }

        public static System.Action OnToggleInventory;
        public static System.Action OnTogglePause;

        private HeroInputBuffer _inputBuffer;
        private float _shiftHoldTimer;
        private bool _shiftTriggeredSprint;
        private bool _shiftExhausted;
        private float _rmbDownTime;
        private float _rmbMouseTravel;

        private void Awake()
        {
            Unit = GetComponent<ActorUnit>();
        }

        private void Start()
        {
            if (Unit != null && Unit.IsLocalPlayer)
                CameraTargetLockSystem.Ensure(Unit);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnTogglePause?.Invoke();
                return;
            }
            if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.C))
            {
                OnToggleInventory?.Invoke();
            }

            if (Time.timeScale < 0.01f || Unit == null || Unit.IsDead) return;
            float dt = Time.deltaTime;
            if (PotionCooldown > 0f) PotionCooldown -= dt;

            UpdateNearestInteractable();

            bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            bool uiBlocking = OrbitCamera3D.Instance != null && OrbitCamera3D.Instance.UIBlockingCursor;

            // Target Lock-On Toggle [Tab / Middle Mouse] & Target Cycling [~ / Alt+Q / Alt+E]
            var lockSys = CameraTargetLockSystem.Ensure(Unit);
            if (!uiBlocking && PressedLockToggle())
            {
                lockSys.ToggleLockOn();
            }
            if (!uiBlocking && lockSys.IsLocked)
            {
                bool holdAlt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                if (Input.GetKeyDown(KeyCode.BackQuote)) lockSys.CycleTarget(1);
                else if (holdAlt && Input.GetKeyDown(KeyCode.Q)) lockSys.CycleTarget(-1);
                else if (holdAlt && Input.GetKeyDown(KeyCode.E)) lockSys.CycleTarget(1);
            }

            // Walk / Run Gait Toggle [CapsLock]
            if (!uiBlocking && Input.GetKeyDown(KeyCode.CapsLock))
            {
                Unit.IsWalking = !Unit.IsWalking;
                CharacterVisualFactory.SpawnFloatingText(
                    transform.position + Vector3.up * 2.15f,
                    Unit.IsWalking ? "CHẾ ĐỘ: ĐI BỘ" : "CHẾ ĐỘ: CHẠY",
                    new Color(0.82f, 0.9f, 1f), 0.72f);
            }

            if (Input.GetKeyDown(KeyCode.F) && NearestInteractable != null)
            {
                NearestInteractable.Interact(Unit);
            }

            if (Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.Alpha5))
            {
                TryUseHealthPotion();
            }

            // Poll movement vector with radial deadzone (supports WASD, Arrow Keys, and Gamepad Left Stick)
            Vector2 moveStick = ApplyRadialDeadzone(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 0.15f);
            float camYaw = OrbitCamera3D.Instance != null
                ? OrbitCamera3D.Instance.Yaw
                : (Camera.main != null ? Camera.main.transform.eulerAngles.y : 0f);
            Vector3 moveDir = Quaternion.Euler(0f, camYaw, 0f) * new Vector3(moveStick.x, 0f, moveStick.y);
            bool hasMove = moveDir.sqrMagnitude > 0.005f;

            Vector3 aimPoint = GetAimPointOnGround(out Vector3 aimDir);

            // Capture discrete combat/evasion inputs into 180ms Action Buffer even if briefly recovering
            CaptureDiscreteActionsToBuffer(moveDir, aimDir, aimPoint, pointerOverUi, uiBlocking, dt, hasMove);

            if (!Unit.CanAct())
            {
                Unit.StopMovingAnim();
                return;
            }

            // 1. Consume highest-priority buffered action on the exact frame Hero becomes ready
            if (_inputBuffer.HasBufferedAction && TryExecuteBufferedAction(moveDir, aimDir, aimPoint))
            {
                return;
            }

            // 2. Locomotion: Walk (0.52x) / Run (1.0x) / Sprint (1.38x) + Lock-On Combat Strafe
            if (hasMove)
            {
                float gaitMult = Unit.IsSprinting ? 1.38f : (Unit.IsWalking ? 0.52f : 1.0f);
                float moveSpd = Unit.EffectiveMoveSpeed() * gaitMult * moveStick.magnitude;

                if (lockSys != null && lockSys.IsLocked && !Unit.IsSprinting && lockSys.LockedTarget != null)
                {
                    // Combat strafe: keep facing locked enemy while moving freely in any direction
                    Vector3 toLocked = lockSys.LockedTarget.transform.position - transform.position;
                    Unit.FaceDirection(toLocked, 16f);
                }
                else
                {
                    Unit.FaceDirection(moveDir, Unit.IsSprinting ? 18f : 15f);
                }

                Unit.MoveWorld(moveDir.normalized * (moveSpd * dt));
            }
            else
            {
                Unit.StopMovingAnim();
            }

            // 3. Held Basic Attack (continuous auto-combo while holding LMB)
            if (!pointerOverUi && !uiBlocking && Input.GetMouseButton(0))
            {
                Unit.TryBasicAttack(aimDir, aimPoint);
            }
        }

        private void CaptureDiscreteActionsToBuffer(Vector3 moveDir, Vector3 aimDir, Vector3 aimPoint, bool pointerOverUi, bool uiBlocking, float dt, bool hasMove)
        {
            if (uiBlocking) return;

            // [Space]: immediate Dodge
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (!Unit.TryDodge(moveDir))
                    _inputBuffer.Enqueue(BufferedActionType.Dodge, moveDir, aimPoint);
            }

            // [LeftShift] Dual-Mode: Hold >= 0.16s to Sprint; Quick Tap (< 0.20s) to Dodge!
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                _shiftHoldTimer = 0f;
                _shiftTriggeredSprint = false;
            }

            if (Input.GetKey(KeyCode.LeftShift))
            {
                _shiftHoldTimer += dt;
                if (_shiftHoldTimer >= 0.16f && hasMove && !_shiftExhausted && Unit.CanAct())
                {
                    if (Unit.Stamina > 2f)
                    {
                        _shiftTriggeredSprint = true;
                        Unit.IsSprinting = true;
                        Unit.DrainStaminaContinuous(16f * dt);
                        if (Unit.Stamina <= 0.5f)
                        {
                            _shiftExhausted = true;
                            Unit.IsSprinting = false;
                        }
                    }
                    else
                    {
                        Unit.IsSprinting = false;
                    }
                }
                else
                {
                    Unit.IsSprinting = false;
                }
            }
            else
            {
                Unit.IsSprinting = false;
            }

            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                Unit.IsSprinting = false;
                _shiftExhausted = false;
                if (!_shiftTriggeredSprint && _shiftHoldTimer < 0.22f)
                {
                    if (!Unit.TryDodge(moveDir))
                        _inputBuffer.Enqueue(BufferedActionType.Dodge, moveDir, aimPoint);
                }
            }

            // [V]: Jump (evades floor traps, buffered with Coyote Time)
            if (Input.GetKeyDown(KeyCode.V))
            {
                if (!Unit.TryJump())
                    _inputBuffer.Enqueue(BufferedActionType.Jump, moveDir, aimPoint);
            }

            // [LMB Down]: Buffered Basic Attack (3-Hit Combo Chain)
            if (!pointerOverUi && Input.GetMouseButtonDown(0))
            {
                if (!Unit.TryBasicAttack(aimDir, aimPoint))
                    _inputBuffer.Enqueue(BufferedActionType.BasicAttack, aimDir, aimPoint);
            }

            // [X] or [RMB Quick Click]: Buffered Heavy Attack (Guard-Break Overhead / Piercing Shot)
            if (Input.GetKeyDown(KeyCode.X))
            {
                if (!Unit.TryHeavyAttack(aimDir, aimPoint))
                    _inputBuffer.Enqueue(BufferedActionType.HeavyAttack, aimDir, aimPoint);
            }

            if (!pointerOverUi && Input.GetMouseButtonDown(1))
            {
                _rmbDownTime = Time.unscaledTime;
                _rmbMouseTravel = 0f;
            }
            if (Input.GetMouseButton(1))
            {
                _rmbMouseTravel += Mathf.Abs(Input.GetAxisRaw("Mouse X")) + Mathf.Abs(Input.GetAxisRaw("Mouse Y"));
            }
            if (!pointerOverUi && Input.GetMouseButtonUp(1))
            {
                bool quickClick = (Time.unscaledTime - _rmbDownTime) <= 0.26f && _rmbMouseTravel < 1.8f;
                if (quickClick || Cursor.lockState == CursorLockMode.Locked)
                {
                    if (quickClick)
                    {
                        if (!Unit.TryHeavyAttack(aimDir, aimPoint))
                            _inputBuffer.Enqueue(BufferedActionType.HeavyAttack, aimDir, aimPoint);
                    }
                }
            }

            // Skills 1..4 (ignore Alt+Q / Alt+E which cycle Lock-On targets)
            bool holdAlt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            if ((!holdAlt && Input.GetKeyDown(KeyCode.Q)) || Input.GetKeyDown(KeyCode.Alpha1))
                TryOrBufferSkill(0, aimPoint);
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Z) || (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.W)))
                TryOrBufferSkill(1, aimPoint);
            if ((!holdAlt && Input.GetKeyDown(KeyCode.E)) || Input.GetKeyDown(KeyCode.Alpha3))
                TryOrBufferSkill(2, aimPoint);
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Alpha4))
                TryOrBufferSkill(3, aimPoint);
        }

        private void TryOrBufferSkill(int slot, Vector3 aimPoint)
        {
            if (!Unit.TryUseSkill(slot, aimPoint))
            {
                BufferedActionType t = BufferedActionType.Skill0 + slot;
                _inputBuffer.Enqueue(t, Vector3.zero, aimPoint);
            }
        }

        private bool TryExecuteBufferedAction(Vector3 currentMoveDir, Vector3 currentAimDir, Vector3 currentAimPoint)
        {
            if (!_inputBuffer.TryConsume(out BufferedActionType action, out Vector3 qDir, out Vector3 qAim))
                return false;

            bool fired = false;
            switch (action)
            {
                case BufferedActionType.Dodge:
                    fired = Unit.TryDodge(currentMoveDir.sqrMagnitude > 0.01f ? currentMoveDir : qDir);
                    break;
                case BufferedActionType.Jump:
                    fired = Unit.TryJump();
                    break;
                case BufferedActionType.BasicAttack:
                    fired = Unit.TryBasicAttack(currentAimDir, currentAimPoint);
                    break;
                case BufferedActionType.HeavyAttack:
                    fired = Unit.TryHeavyAttack(currentAimDir, currentAimPoint);
                    break;
                case BufferedActionType.Skill0:
                case BufferedActionType.Skill1:
                case BufferedActionType.Skill2:
                case BufferedActionType.Skill3:
                    fired = Unit.TryUseSkill((int)(action - BufferedActionType.Skill0), currentAimPoint);
                    break;
            }

            // If still on final frames of cooldown (<180ms remaining), re-queue briefly without resetting expire time
            if (!fired)
            {
                bool almostReady =
                    (action == BufferedActionType.Dodge && Unit.DodgeCd <= HeroInputBuffer.DefaultBufferWindow) ||
                    (action == BufferedActionType.BasicAttack && Unit.BasicAttackCd <= HeroInputBuffer.DefaultBufferWindow) ||
                    (action == BufferedActionType.HeavyAttack && Unit.HeavyAttackCd <= HeroInputBuffer.DefaultBufferWindow) ||
                    (action >= BufferedActionType.Skill0 && action <= BufferedActionType.Skill3 && Unit.SkillCds[(int)(action - BufferedActionType.Skill0)] <= HeroInputBuffer.DefaultBufferWindow);

                if (almostReady)
                    _inputBuffer.Enqueue(action, qDir, qAim, 0.08f);
            }

            return fired && action == BufferedActionType.Dodge;
        }

        private static bool PressedLockToggle()
        {
            return Input.GetKeyDown(KeyCode.Tab) || Input.GetMouseButtonDown(2);
        }

        private static Vector2 ApplyRadialDeadzone(Vector2 stick, float deadzone)
        {
            float mag = stick.magnitude;
            if (mag < deadzone) return Vector2.zero;
            float scaled = Mathf.Min(1f, (mag - deadzone) / Mathf.Max(0.01f, 1f - deadzone));
            return (stick / mag) * scaled;
        }

        public void TriggerSkillFromUIOrKey(int slot)
        {
            if (Unit == null) return;
            Vector3 aimPoint = GetAimPointOnGround(out _);
            TryOrBufferSkill(slot, aimPoint);
        }

        public void TriggerBasicAttackFromUI()
        {
            if (Unit == null) return;
            Vector3 aimPoint = GetAimPointOnGround(out Vector3 aimDir);
            if (!Unit.TryBasicAttack(aimDir, aimPoint))
                _inputBuffer.Enqueue(BufferedActionType.BasicAttack, aimDir, aimPoint);
        }

        public void TriggerHeavyAttackFromUI()
        {
            if (Unit == null) return;
            Vector3 aimPoint = GetAimPointOnGround(out Vector3 aimDir);
            if (!Unit.TryHeavyAttack(aimDir, aimPoint))
                _inputBuffer.Enqueue(BufferedActionType.HeavyAttack, aimDir, aimPoint);
        }

        public void TriggerDodgeFromUI()
        {
            if (Unit == null) return;
            Vector3 aimPoint = GetAimPointOnGround(out Vector3 aimDir);
            if (!Unit.TryDodge(aimDir))
                _inputBuffer.Enqueue(BufferedActionType.Dodge, aimDir, aimPoint);
        }

        public void TryUseHealthPotion()
        {
            if (PotionCooldown > 0f || Unit == null || Unit.IsDead || Unit.IsDowned) return;
            var prof = GameSession.I.Profile;
            if (prof.potions <= 0) return;
            if (Unit.Hp >= Unit.MaxHp - 1f && Unit.Mana >= Unit.MaxMana - 1f) return;

            prof.potions--;
            PotionCooldown = 6f;
            Unit.ReceiveHeal(Unit.MaxHp * 0.42f, Unit);
            Unit.Mana = Mathf.Min(Unit.MaxMana, Unit.Mana + Unit.MaxMana * 0.30f);
            GameAudio.I.PlaySfx(GameSfx.Heal, 0.9f);
            CharacterVisualFactory.SpawnBurst(Unit.transform.position + Vector3.up, new Color(0.25f, 1f, 0.55f), 1.6f, 0.4f);
            GameSession.I.SaveProfile();
            GameSession.I.NotifyProfileChanged();
        }

        private void UpdateNearestInteractable()
        {
            NearestInteractable = null;
            float best = float.MaxValue;
            for (int i = Interactables.Count - 1; i >= 0; i--)
            {
                var it = Interactables[i];
                if (it == null || (it as Object) == null) { Interactables.RemoveAt(i); continue; }
                if (!it.CanInteract()) continue;
                float d = Vector3.Distance(transform.position, it.Position);
                if (d <= it.InteractRadius && d < best)
                {
                    best = d;
                    NearestInteractable = it;
                }
            }
        }

        public Vector3 GetAimPointOnGround(out Vector3 aimDir)
        {
            Vector3 pos = transform.position;

            // 1. Hard Lock-On Priority: skills & ranged shots track the locked enemy directly!
            var lockSys = CameraTargetLockSystem.Instance;
            if (lockSys != null && lockSys.IsLocked && lockSys.LockedTarget != null)
            {
                Vector3 lockedPos = lockSys.LockedTarget.transform.position;
                aimDir = lockedPos - pos;
                aimDir.y = 0f;
                if (aimDir.sqrMagnitude < 0.01f) aimDir = transform.forward;
                aimDir.Normalize();
                return lockedPos;
            }

            Camera cam = Camera.main;

            // 2. Soft-assist toward closest monster in front of camera
            ActorUnit bestTarget = null;
            float bestScore = float.MaxValue;
            Vector3 camFwd = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : transform.forward;
            if (camFwd.sqrMagnitude < 0.01f) camFwd = transform.forward;

            foreach (var u in ActorUnit.AllUnits)
            {
                if (u == null || u.Team == Unit.Team || u.IsDead) continue;
                Vector3 to = u.transform.position - pos;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist > 22f) continue;
                float angle = Vector3.Angle(camFwd, to);
                if (angle < 42f)
                {
                    float score = dist + angle * 0.18f;
                    if (score < bestScore) { bestScore = score; bestTarget = u; }
                }
            }

            if (bestTarget != null)
            {
                Vector3 tp = bestTarget.transform.position;
                aimDir = (tp - pos); aimDir.y = 0f;
                if (aimDir.sqrMagnitude < 0.01f) aimDir = camFwd;
                aimDir.Normalize();
                return tp;
            }

            if (cam != null)
            {
                Ray ray = new Ray(cam.transform.position, cam.transform.forward);
                Plane ground = new Plane(Vector3.up, Vector3.zero);
                if (ground.Raycast(ray, out float enter) && enter > 0f && enter < 45f)
                {
                    Vector3 hit = ray.GetPoint(enter);
                    aimDir = hit - pos; aimDir.y = 0f;
                    if (aimDir.sqrMagnitude > 0.01f) { aimDir.Normalize(); return hit; }
                }
            }

            aimDir = camFwd;
            return pos + aimDir * 8f;
        }
    }
}

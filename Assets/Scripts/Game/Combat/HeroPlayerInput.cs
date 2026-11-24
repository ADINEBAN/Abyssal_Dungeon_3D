using UnityEngine;
using UnityEngine.EventSystems;

namespace AbyssGame
{
    /// <summary>
    /// Responsive Third-Person Action-RPG Input Controller (Phase 2 - Section 3.1):
    /// - Named action abstraction + radial deadzone + 180ms HeroInputBuffer
    /// - Idle / Walk [CapsLock] / Run [WASD] / Sprint [Hold LeftShift] / Jump [V] / Dodge [Space or Tap Shift]
    /// - Target Lock-On [Tab / MMB] + Left/Right Target Cycling [~ / Alt+Q / Alt+E] + Lock-On Combat Strafe.
    /// </summary>
    [DefaultExecutionOrder(-20)]
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
        private Vector3 _smoothedMoveVel;
        private Vector3 _moveVelSmoothRef;

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

            // Capture discrete combat/evasion inputs into 150ms Action Buffer even if briefly recovering
            CaptureDiscreteActionsToBuffer(moveDir, aimDir, aimPoint, pointerOverUi, uiBlocking, dt, hasMove);

            if (!Unit.CanAct())
            {
                _smoothedMoveVel = Vector3.zero;
                _moveVelSmoothRef = Vector3.zero;
                Unit.SetPlanarMoveVelocity(Vector3.zero);
                Unit.StopMovingAnim();
                return;
            }

            // While mid-dodge, DodgeCoroutine drives planar velocity directly; keep input velocity synced smoothly
            if (Unit.IsDodging)
            {
                _smoothedMoveVel = Vector3.zero;
                _moveVelSmoothRef = Vector3.zero;
                return;
            }

            // 1. Consume highest-priority buffered action on the exact frame Hero becomes ready
            if (_inputBuffer.HasBufferedAction && TryExecuteBufferedAction(moveDir, aimDir, aimPoint))
            {
                _smoothedMoveVel = Vector3.zero;
                _moveVelSmoothRef = Vector3.zero;
                return;
            }

            // 2. Smooth Update()-driven Locomotion with SmoothDamp accel/decel & smooth turn rotation
            Vector3 desiredVel = Vector3.zero;
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
                    Unit.FaceDirection(moveDir, Unit.IsSprinting ? 18f : 14f);
                }

                desiredVel = moveDir.normalized * moveSpd;
            }

            float smoothTime = hasMove ? 0.065f : 0.085f;
            _smoothedMoveVel = Vector3.SmoothDamp(_smoothedMoveVel, desiredVel, ref _moveVelSmoothRef, smoothTime, 60f, dt);
            if (!hasMove && _smoothedMoveVel.sqrMagnitude < 0.015f)
            {
                _smoothedMoveVel = Vector3.zero;
                _moveVelSmoothRef = Vector3.zero;
                Unit.StopMovingAnim();
            }

            Unit.SetPlanarMoveVelocity(_smoothedMoveVel);

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

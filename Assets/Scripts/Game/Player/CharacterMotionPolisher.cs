using System.Collections.Generic;
using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Eliminates foot-sliding and adds responsive AAA character secondary motion:
    /// - Safe Animator parameter introspection (never logs missing-parameter warnings)
    /// - Real planar displacement -> BlendTree Speed (Idle 0 / Walk 0.45 / Run 1.0 / Sprint 1.45) + stride speed sync
    /// - Turn-banking roll, Sprint forward lean, Jump stretch & Landing squash + dust burst
    /// - Directional Take-Damage hit flinch & Dodge translucent ghost afterimages.
    /// </summary>
    public class CharacterMotionPolisher : MonoBehaviour
    {
        private ActorUnit _unit;
        private Animator _cachedAnim;
        private readonly HashSet<int> _validParams = new HashSet<int>();

        private static readonly int HashSpeed = Animator.StringToHash("Speed");
        private static readonly int HashLocoMult = Animator.StringToHash("LocoSpeedMult");
        private static readonly int HashGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int HashJump = Animator.StringToHash("Jump");
        private static readonly int HashHit = Animator.StringToHash("Hit");
        private static readonly int HashAttack = Animator.StringToHash("Attack");
        private static readonly int HashCast = Animator.StringToHash("Cast");
        private static readonly int HashDodge = Animator.StringToHash("Dodge");
        private static readonly int HashDead = Animator.StringToHash("IsDead");

        private Vector3 _prevPos;
        private float _prevYaw;
        private float _smoothedAnimSpeed;
        private float _currentBankZ;
        private float _currentPitchX;
        private float _squashY = 1f;
        private float _flinchTimer;
        private Vector3 _flinchLocalOffset;
        private float _footstepDistAccum;
        private float _stepCooldownTimer;
        private bool _wasGrounded = true;
        private bool _nextFootLeft = true;
        private bool _boneStrideInitialized;
        private float _prevLeftLocalZ;
        private float _prevRightLocalZ;
        private bool _leftWasAdvancing;
        private bool _rightWasAdvancing;
        private Transform _leftFootBone;
        private Transform _rightFootBone;
        private static readonly RaycastHit[] _stepRayHits = new RaycastHit[12];

        public FootstepSurface CurrentSurface { get; private set; } = FootstepSurface.Stone;

        private const float ReferenceJogSpeed = 5.1f;

        public void Bind(ActorUnit unit)
        {
            _unit = unit;
            _prevPos = transform.position;
            _prevYaw = transform.eulerAngles.y;
            RebuildParameterCache();
        }

        private void RebuildParameterCache()
        {
            _validParams.Clear();
            _cachedAnim = _unit != null ? _unit.Anim : null;
            _leftFootBone = null;
            _rightFootBone = null;

            if (_cachedAnim != null && _cachedAnim.isHuman)
            {
                _leftFootBone = _cachedAnim.GetBoneTransform(HumanBodyBones.LeftFoot);
                _rightFootBone = _cachedAnim.GetBoneTransform(HumanBodyBones.RightFoot);
            }

            if (_unit != null && _unit.MainVisual != null)
            {
                var all = _unit.MainVisual.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    string n = all[i].name;
                    if (_leftFootBone == null && (n.IndexOf("Bip001 L Foot", System.StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("LeftFoot", System.StringComparison.OrdinalIgnoreCase) >= 0))
                        _leftFootBone = all[i];
                    else if (_rightFootBone == null && (n.IndexOf("Bip001 R Foot", System.StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("RightFoot", System.StringComparison.OrdinalIgnoreCase) >= 0))
                        _rightFootBone = all[i];
                }
            }

            if (_cachedAnim == null || _cachedAnim.runtimeAnimatorController == null) return;

            var pars = _cachedAnim.parameters;
            for (int i = 0; i < pars.Length; i++)
                _validParams.Add(pars[i].nameHash);
        }

        public void TriggerAction(string action)
        {
            if (_unit == null) return;
            if (_unit.Anim != _cachedAnim) RebuildParameterCache();
            if (_cachedAnim == null) return;

            int hash = 0;
            switch (action)
            {
                case "Attack": hash = HashAttack; break;
                case "Cast":   hash = HashCast; break;
                case "Dodge":  hash = HashDodge; break;
                case "Jump":   hash = HashJump; break;
                case "Hit":    hash = HashHit; break;
            }
            if (hash != 0 && _validParams.Contains(hash))
                _cachedAnim.SetTrigger(hash);
        }

        public void SetDeadBool(bool dead)
        {
            if (_unit != null && _unit.Anim != _cachedAnim) RebuildParameterCache();
            if (_cachedAnim != null && _validParams.Contains(HashDead))
                _cachedAnim.SetBool(HashDead, dead);
        }

        public void NotifyTakeDamage(ActorUnit attacker, bool heavyOrCrit)
        {
            _flinchTimer = heavyOrCrit ? 0.18f : 0.12f;
            Vector3 push = attacker != null
                ? (transform.position - attacker.transform.position)
                : -transform.forward;
            push.y = 0f;
            if (push.sqrMagnitude < 0.01f) push = -transform.forward;
            Vector3 localPush = transform.InverseTransformDirection(push.normalized);
            _flinchLocalOffset = localPush * (heavyOrCrit ? 0.16f : 0.09f);

            // Trigger flinch clip only when not actively mid-dodge or dead
            if (_unit != null && !_unit.IsDead && !_unit.IsInvincible && _unit.BasicAttackCd <= 0.05f)
                TriggerAction("Hit");
        }

        public void NotifyJumpTakeoff()
        {
            _squashY = 1.09f;
            TriggerAction("Jump");
            CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up * 0.08f, new Color(0.82f, 0.78f, 0.72f), 0.75f, 0.22f);
        }

        public void SpawnDodgeGhost()
        {
            Color col = _unit != null && _unit.HeroClass != null
                ? _unit.HeroClass.color
                : new Color(0.45f, 0.85f, 1f);

            var ghost = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            ghost.name = "DodgeAfterimage";
            Destroy(ghost.GetComponent<Collider>());
            ghost.transform.position = transform.position + Vector3.up * 0.92f;
            ghost.transform.rotation = transform.rotation;
            ghost.transform.localScale = new Vector3(0.62f, 0.92f, 0.48f);

            var mr = ghost.GetComponent<MeshRenderer>();
            mr.sharedMaterial = CharacterVisualFactory.MakeTransparentMat(new Color(col.r, col.g, col.b, 0.34f), col * 1.6f);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ghost.AddComponent<FadeAndDestroyBehaviour>().Init(0.22f);
        }

        private void LateUpdate()
        {
            if (_unit == null) return;
            if (_unit.Anim != _cachedAnim) RebuildParameterCache();

            float dt = Time.deltaTime;
            if (dt <= 0.0001f) return;

            // 1. Measure real planar displacement to eliminate skating / foot sliding
            Vector3 delta = transform.position - _prevPos;
            _prevPos = transform.position;
            Vector3 planarDelta = new Vector3(delta.x, 0f, delta.z);
            float actualPlanarSpeed = planarDelta.magnitude / dt;

            bool grounded = _unit.IsGroundedUnit;
            if (!_wasGrounded && grounded && !_unit.IsDead)
            {
                // Crisp landing knee compression + surface-matched landing audio & VFX
                _squashY = 0.89f;
                EmitSurfaceFootstep(isLanding: true, isSprint: true, isWalk: false, planarDelta);
            }
            _wasGrounded = grounded;

            // Normalized locomotion tier: 0 = Idle, 0.45 = Walk, 1.0 = Run, 1.45 = Sprint
            float baseMove = Mathf.Max(2.5f, _unit.EffectiveMoveSpeed());
            float targetNormSpeed = 0f;
            if (!_unit.IsDead && !_unit.IsDowned && actualPlanarSpeed > 0.12f)
            {
                float ratio = actualPlanarSpeed / baseMove;
                if (ratio < 0.68f)
                    targetNormSpeed = Mathf.Lerp(0.15f, 0.50f, ratio / 0.68f); // Walk
                else if (ratio <= 1.12f)
                    targetNormSpeed = Mathf.Lerp(0.55f, 1.0f, (ratio - 0.68f) / 0.44f); // Run
                else
                    targetNormSpeed = Mathf.Clamp(1.0f + (ratio - 1.0f) * 1.15f, 1.0f, 1.48f); // Sprint
            }

            float accelRate = targetNormSpeed > _smoothedAnimSpeed ? 14f : 19f;
            _smoothedAnimSpeed = Mathf.MoveTowards(_smoothedAnimSpeed, targetNormSpeed, accelRate * dt);

            if (_cachedAnim != null)
            {
                if (_validParams.Contains(HashSpeed))
                    _cachedAnim.SetFloat(HashSpeed, _smoothedAnimSpeed);

                if (_validParams.Contains(HashGrounded))
                    _cachedAnim.SetBool(HashGrounded, grounded);

                // Match stride cadence directly to ground speed so feet never slide
                float strideMult = actualPlanarSpeed > 0.25f
                    ? Mathf.Clamp(actualPlanarSpeed / ReferenceJogSpeed, 0.62f, 1.52f)
                    : 1.0f;

                if (_validParams.Contains(HashLocoMult))
                    _cachedAnim.SetFloat(HashLocoMult, strideMult);
            }

            // 2. Exact 1-to-1 Foot Bone Touchdown Synchronization (Local Player Audio + Surface VFX)
            if (_stepCooldownTimer > 0f)
                _stepCooldownTimer = Mathf.Max(0f, _stepCooldownTimer - dt);

            if (grounded && !_unit.IsDead && actualPlanarSpeed > 0.45f && !_unit.IsInvincible && _unit.BasicAttackCd <= 0.05f)
            {
                bool isWalk = actualPlanarSpeed < 3.0f || _unit.IsWalking;
                float minStepInterval = _unit.IsSprinting ? 0.30f : (isWalk ? 0.50f : 0.38f);

                bool hasAnimatedFootBones = (_leftFootBone != null && _rightFootBone != null && _leftFootBone != _rightFootBone);
                bool triggeredByBone = false;

                if (hasAnimatedFootBones)
                {
                    Vector3 localL = transform.InverseTransformPoint(_leftFootBone.position);
                    Vector3 localR = transform.InverseTransformPoint(_rightFootBone.position);
                    float boneSpread = Mathf.Abs(localL.z - localR.z);

                    if (!_boneStrideInitialized)
                    {
                        _prevLeftLocalZ = localL.z;
                        _prevRightLocalZ = localR.z;
                        _leftWasAdvancing = false;
                        _rightWasAdvancing = false;
                        _boneStrideInitialized = true;
                    }
                    else
                    {
                        bool leftAdvancing = (localL.z - _prevLeftLocalZ) > 0.0015f;
                        bool rightAdvancing = (localR.z - _prevRightLocalZ) > 0.0015f;

                        // A foot plants on the floor at the exact frame it finishes swinging forward (localZ apex)
                        // while leading the opposite foot. Strictly alternate Left <-> Right foot.
                        if (_stepCooldownTimer <= 0f && boneSpread > 0.03f)
                        {
                            if (_nextFootLeft && _leftWasAdvancing && !leftAdvancing && localL.z > localR.z)
                            {
                                _stepCooldownTimer = minStepInterval;
                                _footstepDistAccum = 0f;
                                EmitSurfaceFootstep(isLanding: false, isSprint: _unit.IsSprinting, isWalk: isWalk, planarDelta, forceLeft: true);
                                triggeredByBone = true;
                            }
                            else if (!_nextFootLeft && _rightWasAdvancing && !rightAdvancing && localR.z > localL.z)
                            {
                                _stepCooldownTimer = minStepInterval;
                                _footstepDistAccum = 0f;
                                EmitSurfaceFootstep(isLanding: false, isSprint: _unit.IsSprinting, isWalk: isWalk, planarDelta, forceLeft: false);
                                triggeredByBone = true;
                            }
                        }

                        _leftWasAdvancing = leftAdvancing;
                        _rightWasAdvancing = rightAdvancing;
                        _prevLeftLocalZ = localL.z;
                        _prevRightLocalZ = localR.z;
                    }
                }

                // Fallback only if a rig has non-animated feet or during steady un-keyed motion
                if (!triggeredByBone)
                {
                    _footstepDistAccum += planarDelta.magnitude;
                    float fallbackStepLen = _unit.IsSprinting ? 2.35f : (isWalk ? 1.65f : 2.00f);
                    if (_footstepDistAccum >= fallbackStepLen && _stepCooldownTimer <= 0f)
                    {
                        _footstepDistAccum = 0f;
                        _stepCooldownTimer = minStepInterval;
                        EmitSurfaceFootstep(isLanding: false, isSprint: _unit.IsSprinting, isWalk: isWalk, planarDelta, forceLeft: _nextFootLeft);
                    }
                }
            }
            else
            {
                if (_footstepDistAccum > 0f && _unit.IsLocalPlayer && GameAudio.I != null)
                {
                    GameAudio.I.StopFootsteps();
                }
                _footstepDistAccum = 0f;
                _boneStrideInitialized = false;
            }

            // 3. Secondary Visual Polish on MainVisual (Turn Bank, Sprint Forward Lean, Landing Squash, Hit Flinch)
            if (_unit.MainVisual != null && !_unit.InBearForm)
            {
                float curYaw = transform.eulerAngles.y;
                float yawVel = Mathf.DeltaAngle(_prevYaw, curYaw) / dt;
                _prevYaw = curYaw;

                float desiredBank = (actualPlanarSpeed > 1.5f && grounded)
                    ? Mathf.Clamp(-yawVel * 0.022f, -7.5f, 7.5f)
                    : 0f;
                float desiredPitch = (_unit.IsSprinting && actualPlanarSpeed > 2.5f && grounded) ? 6.0f : 0f;

                _currentBankZ = Mathf.Lerp(_currentBankZ, desiredBank, 1f - Mathf.Exp(-12f * dt));
                _currentPitchX = Mathf.Lerp(_currentPitchX, desiredPitch, 1f - Mathf.Exp(-12f * dt));
                _squashY = Mathf.Lerp(_squashY, 1f, 1f - Mathf.Exp(-16f * dt));

                Vector3 flinchOff = Vector3.zero;
                if (_flinchTimer > 0f)
                {
                    _flinchTimer = Mathf.Max(0f, _flinchTimer - dt);
                    flinchOff = _flinchLocalOffset * (_flinchTimer / 0.16f);
                }

                // Preserve base scale magnitude while applying subtle squash/stretch
                if (_cachedAnim != null)
                {
                    _unit.MainVisual.transform.localRotation = Quaternion.Euler(_currentPitchX, 0f, _currentBankZ);
                    _unit.MainVisual.transform.localPosition = flinchOff;
                    float invXz = 1f / Mathf.Sqrt(Mathf.Max(0.7f, _squashY));
                    float baseScale = Mathf.Max(0.1f, _unit.VisualBaseScale);
                    _unit.MainVisual.transform.localScale = new Vector3(baseScale * invXz, baseScale * _squashY, baseScale * invXz);
                }
            }
        }

        private void EmitSurfaceFootstep(bool isLanding, bool isSprint, bool isWalk, Vector3 planarDelta, bool? forceLeft = null)
        {
            bool useLeft = forceLeft ?? _nextFootLeft;
            _nextFootLeft = !useLeft;

            Transform footBone = useLeft ? _leftFootBone : _rightFootBone;
            Vector3 footPos = footBone != null
                ? footBone.position
                : (transform.position + transform.right * (useLeft ? -0.18f : 0.18f));

            FootstepSurface surface = DetectSurfaceUnderFoot(footPos, out float groundY);
            CurrentSurface = surface;
            Vector3 contactPos = new Vector3(footPos.x, groundY + 0.02f, footPos.z);

            // Play Footstep Audio ONLY for the Local Player so AI companions never multiply footstep sounds
            if (_unit.IsLocalPlayer)
            {
                float vol = isLanding ? 0.65f : (isSprint ? 0.46f : (isWalk ? 0.25f : 0.36f));
                GameAudio.I.PlayFootstep(surface, isSprint, isLanding, useLeft, vol);
                FootstepSurfaceVFX.Spawn(contactPos, surface, isSprint, isLanding, planarDelta);
            }
            else if (_unit.Team == Team.Hero)
            {
                // AI companions spawn subtle surface dust/ripples without playing overlapping 2D footstep audio
                Camera cam = Camera.main;
                float dist = cam != null ? Vector3.Distance(cam.transform.position, transform.position) : 10f;
                if (dist < 14f)
                {
                    FootstepSurfaceVFX.Spawn(contactPos, surface, isSprint, isLanding, planarDelta, scaleMult: 0.75f);
                }
            }
        }

        public static FootstepSurface DetectSurfaceUnderFoot(Vector3 footWorldPos, out float groundY)
        {
            groundY = footWorldPos.y;
            Vector3 rayOrigin = new Vector3(footWorldPos.x, footWorldPos.y + 0.85f, footWorldPos.z);
            int hitCount = Physics.RaycastNonAlloc(rayOrigin, Vector3.down, _stepRayHits, 2.5f, ~0, QueryTriggerInteraction.Ignore);

            Collider bestCollider = null;
            float highestY = -9999f;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit h = _stepRayHits[i];
                if (h.collider == null) continue;
                if (h.collider.GetComponentInParent<ActorUnit>() != null) continue;

                // Prefer the topmost walkable collider under the foot (e.g. WoodFloor deck at y=0.02 over StoneFloor at y=0.0)
                if (h.point.y > highestY)
                {
                    highestY = h.point.y;
                    bestCollider = h.collider;
                }
            }

            if (bestCollider != null)
            {
                groundY = highestY;
                string colName = bestCollider.name ?? string.Empty;
                string parentName = bestCollider.transform.parent != null ? bestCollider.transform.parent.name : string.Empty;
                var rend = bestCollider.GetComponentInChildren<Renderer>();
                string matName = (rend != null && rend.sharedMaterial != null) ? rend.sharedMaterial.name : string.Empty;

                if (MatchesAny(colName, parentName, matName, "Wood", "Oak", "Plank", "Bridge", "Deck", "Crate", "Chest", "Table", "Barrel", "Tree", "Root"))
                    return FootstepSurface.Wood;

                if (MatchesAny(colName, parentName, matName, "Metal", "Iron", "Grate", "Anvil", "Chain", "Forge", "Trap", "Gate", "Steel"))
                    return FootstepSurface.Metal;

                if (MatchesAny(colName, parentName, matName, "Crystal", "Water", "Ice", "Puddle", "Shrine", "Fountain"))
                    return FootstepSurface.CrystalWater;

                if (MatchesAny(colName, parentName, matName, "Dirt", "Moss", "Forest", "Grass", "Earth", "Mud"))
                    return FootstepSurface.DirtMoss;
            }
            else
            {
                groundY = Mathf.Max(0f, footWorldPos.y);
            }

            if (BiomeAtmosphereController.Instance != null)
            {
                DungeonBiome b = BiomeAtmosphereController.Instance.CurrentBiome;
                if (b == DungeonBiome.CursedForest) return FootstepSurface.DirtMoss;
                if (b == DungeonBiome.CrystalChamber) return FootstepSurface.CrystalWater;
            }

            return FootstepSurface.Stone;
        }

        private static bool MatchesAny(string a, string b, string c, params string[] keywords)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                string kw = keywords[i];
                if ((!string.IsNullOrEmpty(a) && a.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(b) && b.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(c) && c.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Spawns surface-specific 3D footstep visual effects at the exact left/right foot contact point:
    /// - Stone: Warm cathedral stone dust ring + pebble chips
    /// - Wood: Aged-oak sawdust puff + amber wood-splinter specs + plank ring
    /// - Metal: Crisp iron shock ring + bright orange-gold boot sparks
    /// - Dirt/Moss: Earthy soil puff + glowing green moss spores
    /// - Crystal/Water: Expanding cyan-gold ripple ring + rising crystal motes
    /// </summary>
    public class FootstepSurfaceVFX : MonoBehaviour
    {
        private Transform _ring;
        private Material _ringMat;
        private readonly Transform[] _specs = new Transform[5];
        private readonly Vector3[] _specVels = new Vector3[5];
        private Material _specMat;
        private Color _ringCol;
        private Color _specCol;
        private float _dur;
        private float _t;
        private float _maxRingSize;
        private bool _gravitySpecs;

        public static void Spawn(Vector3 pos, FootstepSurface surface, bool isSprint, bool isLanding, Vector3 moveDelta, float scaleMult = 1f)
        {
            var go = new GameObject("FootstepVFX_" + surface);
            go.transform.position = pos;
            var fx = go.AddComponent<FootstepSurfaceVFX>();
            fx.Init(surface, isSprint, isLanding, moveDelta, scaleMult);
        }

        private void Init(FootstepSurface surface, bool isSprint, bool isLanding, Vector3 moveDelta, float scaleMult)
        {
            float intensity = (isLanding ? 1.45f : (isSprint ? 1.18f : 0.85f)) * scaleMult;
            _dur = isLanding ? 0.34f : (isSprint ? 0.26f : 0.22f);
            _maxRingSize = (isLanding ? 0.72f : (isSprint ? 0.48f : 0.34f)) * scaleMult;
            _gravitySpecs = true;

            PrimitiveType specPrim = PrimitiveType.Cube;
            Vector3 specBaseScale = Vector3.one * 0.036f * intensity;

            switch (surface)
            {
                case FootstepSurface.Wood:
                    _ringCol = new Color(0.84f, 0.62f, 0.36f, 0.52f);
                    _specCol = new Color(0.76f, 0.52f, 0.26f, 0.92f);
                    specBaseScale = new Vector3(0.055f, 0.018f, 0.022f) * intensity; // Wood splinters / sawdust chips
                    break;

                case FootstepSurface.Metal:
                    _ringCol = new Color(1.00f, 0.74f, 0.32f, 0.58f);
                    _specCol = new Color(1.00f, 0.82f, 0.35f, 1.00f);
                    specBaseScale = new Vector3(0.024f, 0.024f, 0.065f) * intensity; // Bright friction sparks
                    break;

                case FootstepSurface.DirtMoss:
                    _ringCol = new Color(0.42f, 0.56f, 0.34f, 0.50f);
                    _specCol = new Color(0.48f, 0.78f, 0.38f, 0.90f);
                    specPrim = PrimitiveType.Sphere;
                    specBaseScale = Vector3.one * 0.042f * intensity; // Moss spores & earth clods
                    break;

                case FootstepSurface.CrystalWater:
                    _ringCol = new Color(0.36f, 0.92f, 1.00f, 0.62f);
                    _specCol = new Color(0.55f, 0.96f, 1.00f, 0.95f);
                    specPrim = PrimitiveType.Sphere;
                    specBaseScale = Vector3.one * 0.034f * intensity;
                    _maxRingSize *= 1.25f;
                    _gravitySpecs = false; // Floating crystal/water motes
                    break;

                default: // Stone
                    _ringCol = new Color(0.74f, 0.70f, 0.65f, 0.46f);
                    _specCol = new Color(0.68f, 0.64f, 0.59f, 0.88f);
                    specPrim = PrimitiveType.Sphere;
                    specBaseScale = Vector3.one * 0.036f * intensity; // Stone dust puff & pebbles
                    break;
            }

            // Ground impact ripple/dust quad
            var ringGo = new GameObject("StepRing");
            ringGo.transform.SetParent(transform, false);
            ringGo.transform.localPosition = Vector3.zero;
            ringGo.AddComponent<MeshFilter>().sharedMesh = ObjMeshLoader.GetHorizontalQuadMesh();
            var ringMr = ringGo.AddComponent<MeshRenderer>();
            _ringMat = CharacterVisualFactory.MakeTransparentMat(_ringCol, surface == FootstepSurface.Metal || surface == FootstepSurface.CrystalWater ? _ringCol : default);
            ringMr.sharedMaterial = _ringMat;
            ringMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringMr.receiveShadows = false;
            _ring = ringGo.transform;
            _ring.localScale = new Vector3(0.12f, 1f, 0.12f);

            // 5 flying surface particles (wood splinters, stone dust pebbles, metal sparks, moss spores, crystal motes)
            _specMat = CharacterVisualFactory.MakeTransparentMat(_specCol, surface == FootstepSurface.Metal || surface == FootstepSurface.CrystalWater ? _specCol * 1.8f : default);
            Vector3 backDir = moveDelta.sqrMagnitude > 0.0001f ? -moveDelta.normalized : Vector3.zero;

            for (int i = 0; i < _specs.Length; i++)
            {
                var sp = CharacterVisualFactory.Prim(specPrim, "Spec_" + i, transform,
                    new Vector3(Random.Range(-0.08f, 0.08f), 0.03f, Random.Range(-0.08f, 0.08f)),
                    specBaseScale * Random.Range(0.75f, 1.25f),
                    _specMat,
                    Quaternion.Euler(Random.Range(0f, 180f), Random.Range(0f, 360f), 0f));

                var r = sp.GetComponent<Renderer>();
                if (r != null)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }
                _specs[i] = sp.transform;

                float ang = (i * 72f + Random.Range(-22f, 22f)) * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * Random.Range(0.45f, 1.05f);
                float upSpd = surface == FootstepSurface.Metal ? Random.Range(1.2f, 2.2f) : Random.Range(0.55f, 1.25f);
                _specVels[i] = (radial + backDir * 0.45f) * intensity + Vector3.up * (upSpd * intensity);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _t += dt;
            float u = Mathf.Clamp01(_t / _dur);

            if (_ring != null)
            {
                float sz = Mathf.Lerp(0.14f, _maxRingSize, Mathf.Sqrt(u));
                _ring.localScale = new Vector3(sz, 1f, sz);
            }

            if (_ringMat != null)
            {
                Color rc = new Color(_ringCol.r, _ringCol.g, _ringCol.b, _ringCol.a * (1f - u));
                _ringMat.color = rc;
                if (_ringMat.HasProperty("_BaseColor")) _ringMat.SetColor("_BaseColor", rc);
            }

            if (_specMat != null)
            {
                Color sc = new Color(_specCol.r, _specCol.g, _specCol.b, _specCol.a * (1f - u * u));
                _specMat.color = sc;
                if (_specMat.HasProperty("_BaseColor")) _specMat.SetColor("_BaseColor", sc);
            }

            for (int i = 0; i < _specs.Length; i++)
            {
                if (_specs[i] == null) continue;
                if (_gravitySpecs)
                    _specVels[i].y -= 4.8f * dt;
                else
                    _specVels[i].y += 0.4f * dt;

                _specs[i].localPosition += _specVels[i] * dt;
                if (_specs[i].localPosition.y < 0.01f)
                {
                    Vector3 lp = _specs[i].localPosition;
                    lp.y = 0.01f;
                    _specs[i].localPosition = lp;
                    _specVels[i].x *= 0.6f;
                    _specVels[i].z *= 0.6f;
                }
                _specs[i].localScale *= Mathf.Pow(0.25f, dt / _dur);
            }

            if (_t >= _dur)
            {
                if (_ringMat != null) Destroy(_ringMat);
                if (_specMat != null) Destroy(_specMat);
                Destroy(gameObject);
            }
        }
    }

    public class FadeAndDestroyBehaviour : MonoBehaviour
    {
        private float _dur = 0.22f;
        private float _t;
        private Vector3 _initScale;

        public void Init(float duration)
        {
            _dur = Mathf.Max(0.05f, duration);
            _initScale = transform.localScale;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float u = Mathf.Clamp01(_t / _dur);
            transform.localScale = _initScale * Mathf.Lerp(1f, 0.72f, u);
            if (_t >= _dur) Destroy(gameObject);
        }
    }
}

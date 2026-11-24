using System;
using System.Collections;
using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Section 3.7 Multi-Phase Final Boss: Abyssal Knight.
    /// Phase 1 (100-65%): Sword Slash, Shield Bash, Charge.
    /// Phase 2 (65-30%):  Enrage + Knockback + Summon 2-3 Adds + Dark Slash Combo Chains & Shockwave.
    /// Phase 3 (30-0%):   Fury Enrage + Knockback + Summon 3 Elite Adds + Dense Meteor Storm + Charge-Shockwave Chains.
    /// </summary>
    [DefaultExecutionOrder(-15)]
    [RequireComponent(typeof(ActorUnit))]
    public class AbyssalBossAI : MonoBehaviour
    {
        public static AbyssalBossAI ActiveBoss { get; private set; }

        public int Phase = 1; // 1, 2, 3
        public string CurrentSkillAnnouncement = "";
        public Vector3 ArenaCenter;
        public float ArenaRadius = 15f;

        public event Action<int> OnPhaseChanged;

        private ActorUnit _self;
        private ActorUnit _target;
        private float _skillTimer = 2.8f;
        private int _patternIndex = 0;
        private bool _busy;
        private GameObject _arenaHazardVisual;
        private float _hazardTickTimer;

        private void Awake()
        {
            _self = GetComponent<ActorUnit>();
            if (_self != null)
            {
                _self.OnKilled += HandleBossDefeated;
            }
        }

        private void OnEnable()
        {
            if (_self == null) _self = GetComponent<ActorUnit>();
            if (_self != null && _self.IsRoomDormant) return;
            ActiveBoss = this;
            if (Application.isPlaying)
            {
                if (ArenaCenter == Vector3.zero) ArenaCenter = transform.position;
                GameAudio.I.PlayMusic(GameMusic.Boss);
                GameAudio.I.PlaySfx(GameSfx.BossRoar, 1f);
            }
        }

        private void OnDisable()
        {
            if (ActiveBoss == this) ActiveBoss = null;
        }

        private void OnDestroy()
        {
            if (_self != null) _self.OnKilled -= HandleBossDefeated;
            if (ActiveBoss == this) ActiveBoss = null;
            if (_arenaHazardVisual != null) Destroy(_arenaHazardVisual);
        }

        private void HandleBossDefeated(ActorUnit boss)
        {
            CurrentSkillAnnouncement = "ABYSSAL KNIGHT ĐÃ BỊ TIÊU DIỆT — CHIẾN THẮNG VỰC THẲM!";
            if (OrbitCamera3D.Instance != null) OrbitCamera3D.Instance.Shake(0.45f, 0.75f);
            Vector3 pos = transform.position;
            CharacterVisualFactory.SpawnBurst(pos + Vector3.up * 1.2f, new Color(1f, 0.85f, 0.25f), 8.5f, 0.95f);
            CharacterVisualFactory.SpawnBurst(pos + Vector3.up * 2.4f, new Color(0.55f, 0.25f, 1f), 6.5f, 0.85f);
            CharacterVisualFactory.SpawnFloatingText(pos + Vector3.up * 3.4f, "BOSS VANQUISHED!", new Color(1f, 0.92f, 0.35f), 1.6f);
        }

        private void Start()
        {
            if (ArenaCenter == Vector3.zero) ArenaCenter = transform.position;
        }

        private void Update()
        {
            if (_self == null || _self.IsDead) return;

            CheckPhaseTransitions();
            TickArenaHazard();

            if (_busy || !_self.CanAct()) return;

            SelectTarget();
            if (_target == null) { _self.StopMovingAnim(); return; }

            _skillTimer -= Time.deltaTime * (_self.IsEnraged ? 1.38f : 1.0f);
            Vector3 to = _target.transform.position - transform.position;
            to.y = 0f;
            float dist = to.magnitude;

            if (_skillTimer <= 0f)
            {
                ExecutePhaseSkill();
                return;
            }

            if (dist > _self.AttackRange - 0.3f)
            {
                Vector3 dir = HeroPartyAI.ObstacleAvoidDir(transform.position, to.normalized);
                _self.FaceDirection(dir, 10f);
                _self.MoveWorld(dir * (_self.EffectiveMoveSpeed() * Time.deltaTime));
            }
            else
            {
                _self.StopMovingAnim();
                _self.TryBasicAttack(to.normalized, _target.transform.position);
            }
        }

        private void CheckPhaseTransitions()
        {
            float pct = _self.Hp / Mathf.Max(1f, _self.MaxHp);
            if (Phase == 1 && pct <= 0.65f)
            {
                StartCoroutine(EnterPhaseRoutine(2));
            }
            else if (Phase == 2 && pct <= 0.30f)
            {
                StartCoroutine(EnterPhaseRoutine(3));
            }
        }

        private IEnumerator EnterPhaseRoutine(int newPhase)
        {
            Phase = newPhase;
            _busy = true;
            _self.StopMovingAnim();

            // Cleanse stagger & grant 9.5s Hyper-Armor (Unstoppable) on phase transition so Boss cannot be stun-locked
            _self.GrantPoiseImmunity(9.5f);
            _self.IsEnraged = true;
            GameAudio.I.PlaySfx(GameSfx.BossRoar, 1f);
            if (OrbitCamera3D.Instance != null) OrbitCamera3D.Instance.Shake(0.24f, 0.38f);

            // Short-range radial knockback shockwave pushing nearby melee heroes back on enrage roar
            PushBackNearbyHeroes(6.8f, 10.5f, _self.EffectiveAttack() * 0.85f);

            if (newPhase == 2)
            {
                CurrentSkillAnnouncement = "PHASE 2 (65% HP): CUỒNG NỘ HẮC ÁM — TRIỆU HỒI QUÂN ĐOÀN & LIÊN HOÀN TRẢM!";
                _self.AtkSpeed *= 1.15f;
                _self.MoveSpeed *= 1.10f;
                _self.AddShield(_self.MaxHp * 0.06f);
                UpdateBossArmorPhaseGlow(new Color(0.68f, 0.22f, 1.0f), 2.8f);
                CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up * 1.4f, new Color(0.65f, 0.2f, 1f), 4.2f, 0.55f);
                SpawnPhaseSupportAdds(isPhase3: false);
            }
            else if (newPhase == 3)
            {
                CurrentSkillAnnouncement = "PHASE 3 (30% HP): TỬ THẦN VỰC THẲM — MƯA THIÊN THẠCH & QUÂN ĐOÀN TINH ANH!";
                _self.ApplyStatus(StatusType.Rage, 999f, 0f, _self);
                _self.AtkSpeed *= 1.18f;
                _self.MoveSpeed *= 1.18f;
                UpdateBossArmorPhaseGlow(new Color(1.0f, 0.18f, 0.26f), 3.4f);
                SpawnArenaTransformation();
                CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up * 1.5f, new Color(1f, 0.15f, 0.25f), 5.0f, 0.65f);
                SpawnPhaseSupportAdds(isPhase3: true);
            }

            OnPhaseChanged?.Invoke(newPhase);
            yield return new WaitForSeconds(0.95f);
            float floorCdMult = AbyssDirectorAI.GetFloorBossCooldownMultiplier(GameSession.I != null ? GameSession.I.Profile.selectedFloor : 1);
            _skillTimer = 1.0f * floorCdMult;
            _busy = false;
        }

        private void PushBackNearbyHeroes(float radius, float knockbackImpulse, float chipDamage)
        {
            Vector3 origin = transform.position;
            for (int i = ActorUnit.AllUnits.Count - 1; i >= 0; i--)
            {
                var u = ActorUnit.AllUnits[i];
                if (u == null || u.Team != Team.Hero || u.IsDead || u.IsDowned) continue;
                Vector3 away = u.transform.position - origin;
                away.y = 0f;
                if (away.magnitude <= radius)
                {
                    Vector3 pushDir = away.sqrMagnitude > 0.01f ? away.normalized : transform.forward;
                    u.ApplyKnockback(pushDir, knockbackImpulse);
                    if (!u.IsInvincible)
                    {
                        u.ReceiveHit(chipDamage, false, _self, emphaticPopup: true, isMultiTargetOrAoe: true, heavyTelegraph: true);
                    }
                }
            }
        }

        private void SpawnPhaseSupportAdds(bool isPhase3)
        {
            if (DungeonWorldBuilder.Instance == null) return;

            int aliveAdds = 0;
            for (int i = 0; i < ActorUnit.AllUnits.Count; i++)
            {
                var u = ActorUnit.AllUnits[i];
                if (u != null && u.Team == Team.Monster && !u.IsBoss && !u.IsDead) aliveAdds++;
            }

            int maxAddsCap = 5;
            int floor = GameSession.I != null ? GameSession.I.Profile.selectedFloor : 1;
            int desiredCount = isPhase3 ? 3 : (floor >= 5 ? 3 : 2);
            int toSpawn = Mathf.Min(desiredCount, Mathf.Max(0, maxAddsCap - aliveAdds));
            if (toSpawn <= 0) return;

            string[] phase2Pool = { "SkeletonWarrior", "SkeletonMage", "GoblinArcher" };
            string[] phase3Pool = { "DarkKnight", "SkeletonMage", "GoblinArcher" };

            for (int i = 0; i < toSpawn; i++)
            {
                string id = isPhase3 ? phase3Pool[i % phase3Pool.Length] : phase2Pool[i % phase2Pool.Length];
                if (isPhase3 && i == 0 && UnityEngine.Random.value < 0.5f) id = "Demon";
                bool makeElite = (isPhase3 && i == 0) || (floor >= 10 && i == 0);

                float ang = (i * (360f / Mathf.Max(1, toSpawn)) + UnityEngine.Random.Range(-25f, 25f)) * Mathf.Deg2Rad;
                Vector3 spawnPos = ArenaCenter + new Vector3(Mathf.Cos(ang) * 6.8f, 0f, Mathf.Sin(ang) * 6.8f);
                DungeonWorldBuilder.Instance.SpawnMonsterAt(GameDB.Enemies[id], spawnPos, floor, makeElite, false);
                CharacterVisualFactory.SpawnBurst(spawnPos + Vector3.up * 0.8f, new Color(0.72f, 0.2f, 1f), 1.6f, 0.4f);
            }
        }

        private void UpdateBossArmorPhaseGlow(Color glowCol, float intensity)
        {
            var kit = GetComponentInChildren<EnemyProceduralAnimator>();
            if (kit != null) kit.TriggerAttackPulse();
            var light = GetComponentInChildren<Light>();
            if (light != null)
            {
                light.color = glowCol;
                light.intensity = intensity;
            }
        }

        private void ExecutePhaseSkill()
        {
            var procAnim = GetComponentInChildren<EnemyProceduralAnimator>();
            if (procAnim != null) procAnim.TriggerAttackPulse();
            _patternIndex++;

            int floor = GameSession.I != null ? GameSession.I.Profile.selectedFloor : 1;
            float floorCdMult = AbyssDirectorAI.GetFloorBossCooldownMultiplier(floor);

            if (Phase == 1)
            {
                int m = _patternIndex % 3;
                if (m == 0) StartCoroutine(SkillShieldBash());
                else if (m == 1) StartCoroutine(SkillCharge());
                else StartCoroutine(SkillHeavySlash());
                _skillTimer = 3.8f * floorCdMult;
            }
            else if (Phase == 2)
            {
                int m = _patternIndex % 4;
                if (m == 0) StartCoroutine(SkillPhase2ComboChain());
                else if (m == 1) StartCoroutine(SkillAbyssShockwave());
                else if (m == 2) StartCoroutine(SkillDarkSlashWave());
                else StartCoroutine(SkillSummonMinions(false));
                _skillTimer = 3.1f * floorCdMult;
            }
            else
            {
                int m = _patternIndex % 4;
                if (m == 0) StartCoroutine(SkillMeteorStorm());
                else if (m == 1) StartCoroutine(SkillPhase3FuryChain());
                else if (m == 2) StartCoroutine(SkillPhase2ComboChain());
                else StartCoroutine(SkillSummonMinions(true));
                _skillTimer = 2.55f * floorCdMult;
            }
        }

        /// <summary>Phase 2/3 Combo Chain: Heavy Crimson Sweep immediately chained into a 5-way Dark Slash Wave.</summary>
        private IEnumerator SkillPhase2ComboChain()
        {
            _busy = true;
            CurrentSkillAnnouncement = "Abyssal Knight: LIÊN HOÀN TRẢM HẮC ÁM! [Bấm Space né!]";
            GameAudio.I.PlaySfx(GameSfx.BossRoar, 0.85f);
            if (_target != null) _self.FaceDirection(_target.transform.position - transform.position, 45f);

            // Hit 1: Heavy Crimson Sweep (42% MaxHP if un-dodged!)
            Vector3 sweepPos = transform.position + transform.forward * 2.5f;
            CharacterVisualFactory.SpawnGroundTelegraph(sweepPos, 4.4f, 0.92f, new Color(1f, 0.14f, 0.14f), () =>
            {
                if (_self == null || _self.IsDead || _self.IsStaggered) return;
                GameAudio.I.PlaySfx(GameSfx.Attack, 1f);
                if (OrbitCamera3D.Instance != null) OrbitCamera3D.Instance.Shake(0.18f, 0.20f);
                _self.DealCircleDamage(sweepPos, 4.4f, _self.EffectiveAttack() * 2.30f, StatusType.Slow, 2.2f, 0f, new Color(1f, 0.22f, 0.18f), heavyTelegraph: true);
            });
            yield return new WaitForSeconds(0.98f);

            // Hit 2: Immediate follow-up Dark Slash fan!
            if (_self != null && !_self.IsDead && !_self.IsStaggered)
            {
                Vector3 dir = _target != null ? (_target.transform.position - transform.position) : transform.forward;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
                dir.Normalize();
                transform.rotation = Quaternion.LookRotation(dir);

                CharacterVisualFactory.SpawnGroundTelegraph(transform.position + dir * 2.6f, 3.8f, 0.72f, new Color(1f, 0.16f, 0.26f), null);
                yield return new WaitForSeconds(0.72f);

                if (_self != null && !_self.IsDead && !_self.IsStaggered)
                {
                    GameAudio.I.PlaySfx(GameSfx.Magic, 1f);
                    for (int i = -2; i <= 2; i++)
                    {
                        Vector3 shotDir = Quaternion.Euler(0f, i * 14f, 0f) * dir;
                        SkillProjectile.Spawn(_self, transform.position + Vector3.up * 1.4f + shotDir * 0.8f, shotDir,
                            16.5f, 22f, _self.EffectiveAttack() * 1.85f, true, StatusType.Curse, 5f, 0f, 0f, new Color(0.85f, 0.15f, 1f));
                    }
                }
            }
            yield return new WaitForSeconds(0.28f);
            _busy = false;
        }

        /// <summary>Phase 3 Fury Chain: Abyssal Charge straight into an Abyssal Shockwave detonation.</summary>
        private IEnumerator SkillPhase3FuryChain()
        {
            yield return StartCoroutine(SkillCharge());
            if (_self != null && !_self.IsDead && !_self.IsStaggered)
            {
                yield return new WaitForSeconds(0.18f);
                yield return StartCoroutine(SkillAbyssShockwave());
            }
        }

        private IEnumerator SkillHeavySlash()
        {
            _busy = true;
            CurrentSkillAnnouncement = "Abyssal Knight: CHÉM KIẾM HẮC ÁM! [Bấm Space né!]";
            GameAudio.I.PlaySfx(GameSfx.BossRoar, 0.7f);
            if (_target != null) _self.FaceDirection(_target.transform.position - transform.position, 40f);
            Vector3 p = transform.position + transform.forward * 2.4f;
            CharacterVisualFactory.SpawnGroundTelegraph(p, 4.2f, 1.00f, new Color(1f, 0.16f, 0.12f), () =>
            {
                if (_self == null || _self.IsDead || _self.IsStaggered) return;
                GameAudio.I.PlaySfx(GameSfx.Attack, 1f);
                _self.DealCircleDamage(p, 4.2f, _self.EffectiveAttack() * 2.25f, StatusType.None, 0f, 0f, new Color(1f, 0.25f, 0.18f), heavyTelegraph: true);
            });
            yield return new WaitForSeconds(1.05f);
            _busy = false;
        }

        private IEnumerator SkillShieldBash()
        {
            _busy = true;
            CurrentSkillAnnouncement = "Abyssal Knight: ĐẬP KHIÊN CHOÁNG! [Né Space!]";
            GameAudio.I.PlaySfx(GameSfx.BossRoar, 0.7f);
            CharacterVisualFactory.SpawnGroundTelegraph(transform.position, 4.6f, 1.08f, new Color(1f, 0.20f, 0.12f), () =>
            {
                if (_self == null || _self.IsDead || _self.IsStaggered) return;
                GameAudio.I.PlaySfx(GameSfx.Explosion, 0.9f);
                _self.DealCircleDamage(transform.position, 4.6f, _self.EffectiveAttack() * 2.10f, StatusType.Stun, 1.4f, 0f, new Color(1f, 0.35f, 0.18f), heavyTelegraph: true);
            });
            yield return new WaitForSeconds(1.12f);
            _busy = false;
        }

        private IEnumerator SkillCharge()
        {
            _busy = true;
            CurrentSkillAnnouncement = "Abyssal Knight: XUNG KÍCH VỰC THẲM! [Tránh đường đỏ!]";
            GameAudio.I.PlaySfx(GameSfx.BossRoar, 0.85f);
            Vector3 dir = _target != null ? (_target.transform.position - transform.position) : transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            dir.Normalize();
            transform.rotation = Quaternion.LookRotation(dir);

            float chargeLen = 12f;
            CharacterVisualFactory.SpawnGroundTelegraph(transform.position, 2.3f, 1.05f, new Color(1f, 0.14f, 0.14f), null, new Vector3(2.9f, 0.1f, chargeLen), Quaternion.LookRotation(dir));
            yield return new WaitForSeconds(1.05f);

            if (_self != null && !_self.IsDead && !_self.IsStaggered)
            {
                GameAudio.I.PlaySfx(GameSfx.Dodge, 1f);
                float dur = 0.32f, t = 0f;
                var hitSet = new System.Collections.Generic.HashSet<ActorUnit>();
                while (t < dur && !_self.IsDead && !_self.IsStaggered)
                {
                    Vector3 stepDir = HeroPartyAI.ObstacleAvoidDir(transform.position, dir);
                    if (_self.Controller != null && _self.Controller.enabled)
                        _self.Controller.Move(stepDir * ((chargeLen / dur) * Time.deltaTime));
                    foreach (var u in ActorUnit.AllUnits)
                    {
                        if (u != null && u.Team == Team.Hero && !u.IsDead && !u.IsDowned && !hitSet.Contains(u))
                        {
                            if (Vector3.Distance(transform.position, u.transform.position) <= 2.45f)
                            {
                                hitSet.Add(u);
                                _self.DealDamageTo(u, _self.EffectiveAttack() * 2.35f, StatusType.Stun, 0.9f, 0f, isHeavyOrFinisher: true, isMultiTargetOrAoe: true, heavyTelegraph: true);
                            }
                        }
                    }
                    t += Time.deltaTime;
                    yield return null;
                }
                if (OrbitCamera3D.Instance != null) OrbitCamera3D.Instance.Shake(0.20f, 0.22f);
            }
            _busy = false;
        }

        private IEnumerator SkillDarkSlashWave()
        {
            _busy = true;
            CurrentSkillAnnouncement = "Abyssal Knight: SÓNG TRẢM HẮC ÁM! [Né Space!]";
            GameAudio.I.PlaySfx(GameSfx.BossRoar, 0.75f);
            Vector3 dir = _target != null ? (_target.transform.position - transform.position) : transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            dir.Normalize();
            transform.rotation = Quaternion.LookRotation(dir);

            CharacterVisualFactory.SpawnGroundTelegraph(transform.position + dir * 2.5f, 3.6f, 0.95f, new Color(1f, 0.16f, 0.22f), null);
            yield return new WaitForSeconds(0.95f);
            if (_self != null && !_self.IsDead && !_self.IsStaggered)
            {
                GameAudio.I.PlaySfx(GameSfx.Magic, 1f);
                for (int i = -2; i <= 2; i++)
                {
                    Vector3 shotDir = Quaternion.Euler(0f, i * 16f, 0f) * dir;
                    SkillProjectile.Spawn(_self, transform.position + Vector3.up * 1.4f + shotDir * 0.8f, shotDir,
                        16f, 22f, _self.EffectiveAttack() * 1.85f, true, StatusType.Curse, 5f, 0f, 0f, new Color(0.85f, 0.15f, 1f));
                }
            }
            yield return new WaitForSeconds(0.32f);
            _busy = false;
        }

        private IEnumerator SkillAbyssShockwave()
        {
            _busy = true;
            CurrentSkillAnnouncement = "Abyssal Knight: BÙNG NỔ VỰC THẲM! [Chạy khỏi vòng đỏ!]";
            GameAudio.I.PlaySfx(GameSfx.BossRoar, 0.85f);
            Vector3 c = transform.position;
            CharacterVisualFactory.SpawnGroundTelegraph(c, 7.2f, 1.25f, new Color(1f, 0.14f, 0.16f), () =>
            {
                if (_self == null || _self.IsDead || _self.IsStaggered) return;
                GameAudio.I.PlaySfx(GameSfx.Explosion, 1f);
                if (OrbitCamera3D.Instance != null) OrbitCamera3D.Instance.Shake(0.24f, 0.28f);
                _self.DealCircleDamage(c, 7.2f, _self.EffectiveAttack() * 2.55f, StatusType.Slow, 3f, 0f, new Color(0.95f, 0.18f, 0.35f), heavyTelegraph: true);
            });
            yield return new WaitForSeconds(1.30f);
            _busy = false;
        }

        private IEnumerator SkillSummonMinions(bool summonElite)
        {
            _busy = true;
            CurrentSkillAnnouncement = summonElite ? "Abyssal Knight: Triệu Hồi Quân Đoàn Tinh Anh!" : "Abyssal Knight: Triệu Hồi Quân Đoàn Xương!";
            GameAudio.I.PlaySfx(GameSfx.Magic, 0.95f);
            yield return new WaitForSeconds(0.42f);

            SpawnPhaseSupportAdds(isPhase3: summonElite);
            yield return new WaitForSeconds(0.35f);
            _busy = false;
        }

        private IEnumerator SkillMeteorStorm()
        {
            _busy = true;
            CurrentSkillAnnouncement = "Abyssal Knight: MƯA THIÊN THẠCH VỰC THẲM! [Né vòng đỏ!]";
            GameAudio.I.PlaySfx(GameSfx.BossRoar, 0.9f);

            int meteorCount = Phase >= 3 ? 8 : 6;
            for (int i = 0; i < meteorCount; i++)
            {
                if (_self == null || _self.IsDead || _self.IsStaggered) break;
                Vector3 impact = ArenaCenter + new Vector3(UnityEngine.Random.Range(-10.5f, 10.5f), 0f, UnityEngine.Random.Range(-10.5f, 10.5f));
                // Aim 3 of the meteors directly on hero positions!
                if (i < 3 && _target != null) impact = _target.transform.position;

                CharacterVisualFactory.SpawnGroundTelegraph(impact, 3.9f, 1.15f, new Color(1f, 0.14f, 0.10f), () =>
                {
                    if (_self == null || _self.IsDead || _self.IsStaggered) return;
                    GameAudio.I.PlaySfx(GameSfx.Explosion, 0.85f);
                    if (OrbitCamera3D.Instance != null) OrbitCamera3D.Instance.Shake(0.18f, 0.18f);
                    _self.DealCircleDamage(impact, 3.9f, _self.EffectiveAttack() * 2.45f, StatusType.Burn, 4f, _self.EffectiveAttack() * 0.25f, new Color(1f, 0.35f, 0.1f), heavyTelegraph: true);
                });
                yield return new WaitForSeconds(0.20f);
            }
            yield return new WaitForSeconds(0.55f);
            _busy = false;
        }

        private void SpawnArenaTransformation()
        {
            if (_arenaHazardVisual != null) return;
            _arenaHazardVisual = new GameObject("ArenaPhase3Transformation");
            _arenaHazardVisual.transform.position = ArenaCenter;

            var fireMat = CharacterVisualFactory.MakeTransparentMat(new Color(0.95f, 0.15f, 0.25f, 0.35f), new Color(1f, 0.15f, 0.2f) * 1.5f);
            var obeliskMat = CharacterVisualFactory.MakeMat(new Color(0.25f, 0.05f, 0.35f), 0.7f, 0.5f, new Color(0.8f, 0.1f, 0.4f) * 1.6f);

            for (int i = 0; i < 12; i++)
            {
                float a = i * (360f / 12f) * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Cos(a) * (ArenaRadius - 2.2f), 0f, Mathf.Sin(a) * (ArenaRadius - 2.2f));
                CharacterVisualFactory.Prim(PrimitiveType.Cylinder, "FireNode" + i, _arenaHazardVisual.transform, p + Vector3.up * 0.04f, new Vector3(3.4f, 0.03f, 3.4f), fireMat);
                if (i % 3 == 0)
                    CharacterVisualFactory.Prim(PrimitiveType.Cube, "VoidObelisk" + i, _arenaHazardVisual.transform, p + Vector3.up * 1.6f, new Vector3(0.75f, 3.2f, 0.75f), obeliskMat, Quaternion.Euler(0f, i * 30f, 8f));
            }
        }

        private void TickArenaHazard()
        {
            if (Phase < 3) return;
            _hazardTickTimer -= Time.deltaTime;
            if (_hazardTickTimer > 0f) return;
            _hazardTickTimer = 1.0f;

            // Heroes standing in the outer collapsed ring of the arena take Abyssal Burn damage
            float safeRadius = ArenaRadius - 2.6f;
            foreach (var u in ActorUnit.AllUnits)
            {
                if (u != null && u.Team == Team.Hero && !u.IsDead && !u.IsDowned)
                {
                    Vector2 d = new Vector2(u.transform.position.x - ArenaCenter.x, u.transform.position.z - ArenaCenter.z);
                    if (d.magnitude > safeRadius)
                    {
                        u.ReceiveHit(_self.Atk * 0.45f, false, _self);
                    }
                }
            }
        }

        private void SelectTarget()
        {
            if (_self.TauntTarget != null && !_self.TauntTarget.IsDead && !_self.TauntTarget.IsDowned)
            {
                _target = _self.TauntTarget;
                return;
            }
            ActorUnit best = null;
            float bestDist = float.MaxValue;
            foreach (var u in ActorUnit.AllUnits)
            {
                if (u.Team == Team.Hero && !u.IsDead && !u.IsDowned)
                {
                    float d = Vector3.Distance(transform.position, u.transform.position);
                    if (d < bestDist) { bestDist = d; best = u; }
                }
            }
            _target = best;
        }
    }
}


using System;
using System.Collections;
using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Section 3.7 Multi-Phase Final Boss: Abyssal Knight.
    /// Phase 1 (100-66%): Sword Slash, Shield Bash, Charge.
    /// Phase 2 (66-33%):  Dark Slash, Summon Skeletons, AoE Shockwave.
    /// Phase 3 (33-0%):   Enrage, Meteor Attack, Arena Hazard Transformation, Summon Elite.
    /// </summary>
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
            ActiveBoss = this;
            if (_self != null)
            {
                _self.OnKilled += HandleBossDefeated;
            }
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
            GameAudio.I.PlayMusic(GameMusic.Boss);
            GameAudio.I.PlaySfx(GameSfx.BossRoar, 1f);
        }

        private void Update()
        {
            if (_self == null || _self.IsDead) return;

            CheckPhaseTransitions();
            TickArenaHazard();

            if (_busy || !_self.CanAct()) return;

            SelectTarget();
            if (_target == null) { _self.StopMovingAnim(); return; }

            _skillTimer -= Time.deltaTime;
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
            if (Phase == 1 && pct <= 0.66f)
            {
                StartCoroutine(EnterPhaseRoutine(2));
            }
            else if (Phase == 2 && pct <= 0.33f)
            {
                StartCoroutine(EnterPhaseRoutine(3));
            }
        }

        private IEnumerator EnterPhaseRoutine(int newPhase)
        {
            Phase = newPhase;
            _busy = true;
            _self.StopMovingAnim();
            GameAudio.I.PlaySfx(GameSfx.BossRoar, 1f);
            if (OrbitCamera3D.Instance != null) OrbitCamera3D.Instance.Shake(0.38f, 0.55f);

            if (newPhase == 2)
            {
                CurrentSkillAnnouncement = "PHASE 2: BÓNG TỐI TRỖI DẬY!";
                _self.AddShield(_self.MaxHp * 0.08f);
                UpdateBossArmorPhaseGlow(new Color(0.68f, 0.22f, 1.0f), 3.2f);
                CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up * 1.4f, new Color(0.65f, 0.2f, 1f), 5.5f, 0.7f);
            }
            else if (newPhase == 3)
            {
                CurrentSkillAnnouncement = "PHASE 3: ENRAGE — ĐẤU TRƯỜNG VỰC THẲM!";
                _self.ApplyStatus(StatusType.Rage, 999f, 0f, _self);
                _self.MoveSpeed *= 1.25f;
                UpdateBossArmorPhaseGlow(new Color(1.0f, 0.18f, 0.26f), 4.2f);
                SpawnArenaTransformation();
                CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up * 1.5f, new Color(1f, 0.15f, 0.25f), 7f, 0.8f);
            }

            OnPhaseChanged?.Invoke(newPhase);
            yield return new WaitForSeconds(1.1f);
            _skillTimer = 1.2f;
            _busy = false;
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
            if (Phase == 1)
            {
                int m = _patternIndex % 3;
                if (m == 0) StartCoroutine(SkillShieldBash());
                else if (m == 1) StartCoroutine(SkillCharge());
                else StartCoroutine(SkillHeavySlash());
                _skillTimer = 4.2f;
            }
            else if (Phase == 2)
            {
                int m = _patternIndex % 3;
                if (m == 0) StartCoroutine(SkillDarkSlashWave());
                else if (m == 1) StartCoroutine(SkillAbyssShockwave());
                else StartCoroutine(SkillSummonMinions(false));
                _skillTimer = 3.6f;
            }
            else
            {
                int m = _patternIndex % 3;
                if (m == 0) StartCoroutine(SkillMeteorStorm());
                else if (m == 1) StartCoroutine(SkillCharge());
                else StartCoroutine(SkillSummonMinions(true));
                _skillTimer = 3.1f;
            }
        }

        private IEnumerator SkillHeavySlash()
        {
            _busy = true;
            CurrentSkillAnnouncement = "Abyssal Knight: Chém Kiếm Hắc Ám!";
            if (_target != null) _self.FaceDirection(_target.transform.position - transform.position, 40f);
            Vector3 p = transform.position + transform.forward * 2.4f;
            CharacterVisualFactory.SpawnGroundTelegraph(p, 3.8f, 0.7f, new Color(1f, 0.35f, 0.2f), () =>
            {
                if (_self == null || _self.IsDead) return;
                GameAudio.I.PlaySfx(GameSfx.Attack, 1f);
                _self.DealCircleDamage(p, 3.8f, _self.EffectiveAttack() * 1.65f, StatusType.None, 0f, 0f, new Color(1f, 0.3f, 0.2f));
            });
            yield return new WaitForSeconds(0.8f);
            _busy = false;
        }

        private IEnumerator SkillShieldBash()
        {
            _busy = true;
            CurrentSkillAnnouncement = "Abyssal Knight: Shield Bash (Choáng)!";
            CharacterVisualFactory.SpawnGroundTelegraph(transform.position, 4.2f, 0.75f, new Color(1f, 0.75f, 0.2f), () =>
            {
                if (_self == null || _self.IsDead) return;
                GameAudio.I.PlaySfx(GameSfx.Explosion, 0.9f);
                _self.DealCircleDamage(transform.position, 4.2f, _self.EffectiveAttack() * 1.4f, StatusType.Stun, 1.4f, 0f, new Color(1f, 0.8f, 0.2f));
            });
            yield return new WaitForSeconds(0.85f);
            _busy = false;
        }

        private IEnumerator SkillCharge()
        {
            _busy = true;
            CurrentSkillAnnouncement = "Abyssal Knight: Xung Kích Vực Thẳm!";
            Vector3 dir = _target != null ? (_target.transform.position - transform.position) : transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            dir.Normalize();
            transform.rotation = Quaternion.LookRotation(dir);

            float chargeLen = 12f;
            CharacterVisualFactory.SpawnGroundTelegraph(transform.position, 2.2f, 0.85f, new Color(1f, 0.2f, 0.2f), null, new Vector3(2.8f, 0.1f, chargeLen), Quaternion.LookRotation(dir));
            yield return new WaitForSeconds(0.85f);

            if (_self != null && !_self.IsDead)
            {
                GameAudio.I.PlaySfx(GameSfx.Dodge, 1f);
                float dur = 0.32f, t = 0f;
                var hitSet = new System.Collections.Generic.HashSet<ActorUnit>();
                while (t < dur && !_self.IsDead)
                {
                    Vector3 stepDir = HeroPartyAI.ObstacleAvoidDir(transform.position, dir);
                    if (_self.Controller != null && _self.Controller.enabled)
                        _self.Controller.Move(stepDir * ((chargeLen / dur) * Time.deltaTime));
                    foreach (var u in ActorUnit.AllUnits)
                    {
                        if (u != null && u.Team == Team.Hero && !u.IsDead && !u.IsDowned && !hitSet.Contains(u))
                        {
                            if (Vector3.Distance(transform.position, u.transform.position) <= 2.4f)
                            {
                                hitSet.Add(u);
                                _self.DealDamageTo(u, _self.EffectiveAttack() * 1.9f, StatusType.Stun, 0.9f, 0f);
                            }
                        }
                    }
                    t += Time.deltaTime;
                    yield return null;
                }
                if (OrbitCamera3D.Instance != null) OrbitCamera3D.Instance.Shake(0.28f, 0.25f);
            }
            _busy = false;
        }

        private IEnumerator SkillDarkSlashWave()
        {
            _busy = true;
            CurrentSkillAnnouncement = "Abyssal Knight: Dark Slash!";
            Vector3 dir = _target != null ? (_target.transform.position - transform.position) : transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            dir.Normalize();
            transform.rotation = Quaternion.LookRotation(dir);

            yield return new WaitForSeconds(0.35f);
            if (_self != null && !_self.IsDead)
            {
                GameAudio.I.PlaySfx(GameSfx.Magic, 1f);
                for (int i = -2; i <= 2; i++)
                {
                    Vector3 shotDir = Quaternion.Euler(0f, i * 16f, 0f) * dir;
                    SkillProjectile.Spawn(_self, transform.position + Vector3.up * 1.4f + shotDir * 0.8f, shotDir,
                        16f, 22f, _self.EffectiveAttack() * 1.35f, true, StatusType.Curse, 5f, 0f, 0f, new Color(0.7f, 0.15f, 1f));
                }
            }
            yield return new WaitForSeconds(0.4f);
            _busy = false;
        }

        private IEnumerator SkillAbyssShockwave()
        {
            _busy = true;
            CurrentSkillAnnouncement = "Abyssal Knight: Bùng Nổ Vực Thẳm!";
            Vector3 c = transform.position;
            CharacterVisualFactory.SpawnGroundTelegraph(c, 6.8f, 1.15f, new Color(0.75f, 0.15f, 1f), () =>
            {
                if (_self == null || _self.IsDead) return;
                GameAudio.I.PlaySfx(GameSfx.Explosion, 1f);
                if (OrbitCamera3D.Instance != null) OrbitCamera3D.Instance.Shake(0.35f, 0.35f);
                _self.DealCircleDamage(c, 6.8f, _self.EffectiveAttack() * 2.1f, StatusType.Slow, 3f, 0f, new Color(0.75f, 0.15f, 1f));
            });
            yield return new WaitForSeconds(1.25f);
            _busy = false;
        }

        private IEnumerator SkillSummonMinions(bool summonElite)
        {
            _busy = true;
            CurrentSkillAnnouncement = summonElite ? "Abyssal Knight: Triệu Hồi Tinh Anh!" : "Abyssal Knight: Triệu Hồi Quân Đoàn Xương!";
            GameAudio.I.PlaySfx(GameSfx.Magic, 0.95f);
            yield return new WaitForSeconds(0.45f);

            int aliveAdds = 0;
            foreach (var u in ActorUnit.AllUnits)
                if (u.Team == Team.Monster && !u.IsBoss && !u.IsDead) aliveAdds++;

            if (aliveAdds < 4 && DungeonWorldBuilder.Instance != null)
            {
                int floor = GameSession.I.Profile.selectedFloor;
                if (summonElite)
                {
                    string id = UnityEngine.Random.value < 0.5f ? "DarkKnight" : "Demon";
                    Vector3 p = ArenaCenter + new Vector3(UnityEngine.Random.Range(-6f, 6f), 0f, UnityEngine.Random.Range(-6f, 6f));
                    DungeonWorldBuilder.Instance.SpawnMonsterAt(GameDB.Enemies[id], p, floor, true, false);
                }
                else
                {
                    for (int i = 0; i < 2; i++)
                    {
                        string id = i == 0 ? "Skeleton" : "SkeletonMage";
                        Vector3 p = ArenaCenter + new Vector3(UnityEngine.Random.Range(-7f, 7f), 0f, UnityEngine.Random.Range(-7f, 7f));
                        DungeonWorldBuilder.Instance.SpawnMonsterAt(GameDB.Enemies[id], p, floor, false, false);
                    }
                }
            }
            yield return new WaitForSeconds(0.4f);
            _busy = false;
        }

        private IEnumerator SkillMeteorStorm()
        {
            _busy = true;
            CurrentSkillAnnouncement = "Abyssal Knight: MƯA THIÊN THẠCH VỰC THẲM!";
            GameAudio.I.PlaySfx(GameSfx.BossRoar, 0.9f);

            for (int i = 0; i < 5; i++)
            {
                if (_self == null || _self.IsDead) break;
                Vector3 impact = ArenaCenter + new Vector3(UnityEngine.Random.Range(-10f, 10f), 0f, UnityEngine.Random.Range(-10f, 10f));
                // Aim 2 of the meteors directly on hero positions!
                if (i < 2 && _target != null) impact = _target.transform.position;

                CharacterVisualFactory.SpawnGroundTelegraph(impact, 3.6f, 1.05f, new Color(1f, 0.25f, 0.1f), () =>
                {
                    if (_self == null || _self.IsDead) return;
                    GameAudio.I.PlaySfx(GameSfx.Explosion, 0.85f);
                    if (OrbitCamera3D.Instance != null) OrbitCamera3D.Instance.Shake(0.25f, 0.22f);
                    _self.DealCircleDamage(impact, 3.6f, _self.EffectiveAttack() * 1.95f, StatusType.Burn, 4f, _self.EffectiveAttack() * 0.25f, new Color(1f, 0.35f, 0.1f));
                });
                yield return new WaitForSeconds(0.26f);
            }
            yield return new WaitForSeconds(0.6f);
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


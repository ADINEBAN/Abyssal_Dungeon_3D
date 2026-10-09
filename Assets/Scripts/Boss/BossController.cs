using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using AbyssalDungeon.Dungeon;
using AbyssalDungeon.Combat;

namespace AbyssalDungeon.Boss
{
    public enum BossPhase { Phase1, Phase2, Phase3 }
    public enum BossState { Idle, Attacking, Transitioning, Dead }

    [RequireComponent(typeof(NavMeshAgent), typeof(CapsuleCollider))]
    public class BossController : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        [SerializeField] private float maxHealth = 1000f;
        private float currentHealth;

        [Header("Phase Thresholds")]
        [SerializeField, Range(0,1)] private float phase2Threshold = 0.7f;
        [SerializeField, Range(0,1)] private float phase3Threshold = 0.35f;
        
        [Header("Damage Config")]
        [SerializeField] private float attackDamage = 50f;
        [SerializeField] private float skillCooldown = 3f;

        [Header("State")]
        [SerializeField] private BossPhase currentPhase = BossPhase.Phase1;
        [SerializeField] private BossState currentState = BossState.Idle;
        
        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private ParticleSystem phaseTransitionVFX;

        [Header("Events")]
        public UnityEvent<float, float> OnHealthChanged;
        public UnityEvent<BossPhase> OnPhaseChange;
        public UnityEvent OnBossDeath;

        private NavMeshAgent agent;
        private float nextAttackTime = 0f;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            currentHealth = maxHealth;
        }

        private void Start()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }
        }

        private void Update()
        {
            if (currentState == BossState.Dead || currentState == BossState.Transitioning) return;

            if (target != null)
            {
                agent.SetDestination(target.position);
                
                if (Time.time >= nextAttackTime && Vector3.Distance(transform.position, target.position) <= agent.stoppingDistance)
                {
                    ExecuteAttackPattern();
                }
            }
        }

        public void TakeDamage(DamageInfo info)
        {
            if (currentState == BossState.Dead) return;

            currentHealth -= info.finalDamage;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            CheckPhaseTransition();

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public float GetCurrentHP() => currentHealth;
        public float GetMaxHP() => maxHealth;
        public bool IsAlive() => currentHealth > 0;

        private void CheckPhaseTransition()
        {
            float healthPercent = currentHealth / maxHealth;

            if (currentPhase == BossPhase.Phase1 && healthPercent <= phase2Threshold && healthPercent > phase3Threshold)
            {
                StartCoroutine(PhaseTransitionRoutine(BossPhase.Phase2));
            }
            else if (currentPhase == BossPhase.Phase2 && healthPercent <= phase3Threshold)
            {
                StartCoroutine(PhaseTransitionRoutine(BossPhase.Phase3));
            }
        }

        private IEnumerator PhaseTransitionRoutine(BossPhase newPhase)
        {
            currentState = BossState.Transitioning;
            currentPhase = newPhase;
            agent.isStopped = true;
            
            if (phaseTransitionVFX != null) phaseTransitionVFX.Play();
            
            Debug.Log($"Boss entering {newPhase}!");
            OnPhaseChange?.Invoke(newPhase);
            
            // Wait for transition animation
            yield return new WaitForSeconds(3f);
            
            agent.isStopped = false;
            currentState = BossState.Idle;
        }

        private void ExecuteAttackPattern()
        {
            currentState = BossState.Attacking;
            nextAttackTime = Time.time + skillCooldown;

            // Attack logic based on phase
            switch (currentPhase)
            {
                case BossPhase.Phase1:
                    Debug.Log("Phase 1 Attack: SwordSlash / ShieldBash / Charge");
                    break;
                case BossPhase.Phase2:
                    Debug.Log("Phase 2 Attack: DarkSlash / SummonSkeleton / AoEAttack");
                    break;
                case BossPhase.Phase3:
                    Debug.Log("Phase 3 Attack: Enrage / MeteorAttack / SummonElite");
                    break;
            }

            // Simulate attack duration
            Invoke(nameof(ResetState), 1.5f);
        }

        private void ResetState()
        {
            if (currentState != BossState.Dead) currentState = BossState.Idle;
        }

        private void Die()
        {
            currentState = BossState.Dead;
            agent.isStopped = true;
            Debug.Log("Abyssal Knight Defeated!");
            OnBossDeath?.Invoke();
            
            // Destroy after delay
            Destroy(gameObject, 5f);
        }
    }
}


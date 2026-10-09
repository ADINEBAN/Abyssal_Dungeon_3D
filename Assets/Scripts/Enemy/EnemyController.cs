using System;
using UnityEngine;
using UnityEngine.AI;
using AbyssalDungeon.Combat;
using AbyssalDungeon.AI;

namespace AbyssalDungeon.Enemy
{


    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyStateMachine))]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        [Header("References")]
        public Transform[] PatrolPoints;
        public Transform Target { get; set; }

        [Header("Settings")]
        [SerializeField] private float _maxHp = 100f;
        [SerializeField] private float _detectionRange = 15f;
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _damage = 10f;
        [SerializeField] private LayerMask _playerLayer;
        
        [Header("VFX & Loot")]
        [SerializeField] private GameObject _deathVfxPrefab;
        [SerializeField] private GameObject[] _lootPrefabs;
        [SerializeField] private int _expReward = 50;
        [SerializeField] private int _goldReward = 20;

        public NavMeshAgent Agent { get; private set; }
        public EnemyStateMachine StateMachine { get; private set; }
        
        private float _currentHp;

        public event Action<float> OnEnemyDamaged;
        public event Action<EnemyController> OnEnemyDeath;

        protected virtual void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            StateMachine = GetComponent<EnemyStateMachine>();
        }

        protected virtual void Start()
        {
            _currentHp = _maxHp;
            ChangeState(EnemyStateType.Idle);
        }

        protected virtual void Update()
        {
            DetectPlayer();
        }

        public void ChangeState(EnemyStateType stateType)
        {
            EnemyState newState = stateType switch
            {
                EnemyStateType.Idle => new IdleState(this),
                EnemyStateType.Patrol => new PatrolState(this),
                EnemyStateType.Detect => new DetectState(this),
                EnemyStateType.Chase => new ChaseState(this),
                EnemyStateType.Attack => new AttackState(this),
                EnemyStateType.Hit => new HitState(this),
                EnemyStateType.Stunned => new StunnedState(this, 2f),
                EnemyStateType.Retreat => new RetreatState(this),
                EnemyStateType.Dead => new DeadState(this),
                _ => new IdleState(this)
            };

            StateMachine.ChangeState(stateType, newState);
        }

        public bool IsPlayerInDetectionRange()
        {
            if (Target == null) return false;
            return Vector3.Distance(transform.position, Target.position) <= _detectionRange;
        }

        public bool IsPlayerInAttackRange()
        {
            if (Target == null) return false;
            return Vector3.Distance(transform.position, Target.position) <= _attackRange;
        }

        private void DetectPlayer()
        {
            if (Target != null) return;

            Collider[] hits = Physics.OverlapSphere(transform.position, _detectionRange, _playerLayer);
            if (hits.Length > 0)
            {
                Target = hits[0].transform;
            }
        }

        public virtual void PerformAttack()
        {
            if (Target != null)
            {
                IDamageable damageable = Target.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    DamageInfo info = new DamageInfo { attacker = gameObject, target = Target.gameObject, baseDamage = _damage, finalDamage = _damage, damageType = DamageType.Physical };
                    damageable.TakeDamage(info);
                }
            }
        }

        public virtual void TakeDamage(DamageInfo info)
        {
            if (StateMachine.CurrentStateType == EnemyStateType.Dead) return;

            _currentHp -= info.finalDamage;
            OnEnemyDamaged?.Invoke(info.finalDamage);

            if (_currentHp <= 0)
            {
                Die();
            }
            else
            {
                ChangeState(EnemyStateType.Hit);
            }
        }

        public float GetCurrentHP() => _currentHp;
        public float GetMaxHP() => _maxHp;
        public bool IsAlive() => _currentHp > 0;

        protected virtual void Die()
        {
            ChangeState(EnemyStateType.Dead);
            OnEnemyDeath?.Invoke(this);

            if (_deathVfxPrefab != null)
            {
                Instantiate(_deathVfxPrefab, transform.position, Quaternion.identity);
            }

            DropLoot();
            GiveRewards();

            gameObject.SetActive(false);
        }

        private void DropLoot()
        {
            if (_lootPrefabs != null && _lootPrefabs.Length > 0)
            {
                foreach (var loot in _lootPrefabs)
                {
                    if (UnityEngine.Random.value > 0.5f) // Simple drop chance
                    {
                        Instantiate(loot, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                    }
                }
            }
        }

        private void GiveRewards()
        {
            // Give Exp and Gold to player system
            Debug.Log($"Gave {_expReward} EXP and {_goldReward} Gold");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRange);
        }
    }
}


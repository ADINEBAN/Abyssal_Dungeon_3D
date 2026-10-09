using UnityEngine;
using AbyssalDungeon.Enemy;

namespace AbyssalDungeon.AI
{
    public class RangedEnemyBehavior : EnemyController
    {
        [Header("Ranged Settings")]
        [SerializeField] private float _preferredDistance = 10f;
        [SerializeField] private float _fleeDistance = 5f;
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private Transform _firePoint;
        [SerializeField] private float _strafeSpeed = 3f;

        private float _strafeTimer;
        private Vector3 _strafeDirection;

        protected override void Update()
        {
            base.Update();
            
            if (Target != null && StateMachine.CurrentStateType != EnemyStateType.Dead && StateMachine.CurrentStateType != EnemyStateType.Stunned)
            {
                HandlePositioning();
            }
        }

        private void HandlePositioning()
        {
            float distanceToTarget = Vector3.Distance(transform.position, Target.position);

            if (distanceToTarget < _fleeDistance)
            {
                ChangeState(EnemyStateType.Retreat);
            }
            else if (distanceToTarget > _preferredDistance)
            {
                if (StateMachine.CurrentStateType != EnemyStateType.Chase && StateMachine.CurrentStateType != EnemyStateType.Attack)
                {
                    ChangeState(EnemyStateType.Chase);
                }
            }
            else
            {
                // In preferred range, strafe around
                if (StateMachine.CurrentStateType != EnemyStateType.Attack)
                {
                    ChangeState(EnemyStateType.Attack);
                }

                _strafeTimer -= Time.deltaTime;
                if (_strafeTimer <= 0)
                {
                    _strafeDirection = Random.value > 0.5f ? transform.right : -transform.right;
                    _strafeTimer = Random.Range(1f, 3f);
                }

                if (Agent.isActiveAndEnabled)
                {
                    Agent.Move(_strafeDirection * _strafeSpeed * Time.deltaTime);
                }
            }
        }

        public override void PerformAttack()
        {
            if (_projectilePrefab != null && _firePoint != null && Target != null)
            {
                Vector3 direction = (Target.position + Vector3.up * 1f) - _firePoint.position;
                Projectile projectile = Instantiate(_projectilePrefab, _firePoint.position, Quaternion.LookRotation(direction));
                projectile.Initialize(direction.normalized);
            }
        }
    }
}

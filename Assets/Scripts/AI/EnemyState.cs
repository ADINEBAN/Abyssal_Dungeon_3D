using UnityEngine;
using UnityEngine.AI;

namespace AbyssalDungeon.AI
{
    public abstract class EnemyState
    {
        protected AbyssalDungeon.Enemy.EnemyController owner;

        public EnemyState(AbyssalDungeon.Enemy.EnemyController owner)
        {
            this.owner = owner;
        }

        public abstract void Enter();
        public abstract void Execute();
        public abstract void Exit();
    }

    public class IdleState : EnemyState
    {
        private float _idleTime;
        private float _timer;

        public IdleState(AbyssalDungeon.Enemy.EnemyController owner) : base(owner) { }

        public override void Enter()
        {
            _idleTime = Random.Range(2f, 5f);
            _timer = 0f;
            if (owner.Agent != null && owner.Agent.isActiveAndEnabled)
            {
                owner.Agent.isStopped = true;
            }
        }

        public override void Execute()
        {
            _timer += Time.deltaTime;
            
            if (owner.IsPlayerInDetectionRange())
            {
                owner.ChangeState(EnemyStateType.Detect);
                return;
            }

            if (_timer >= _idleTime)
            {
                owner.ChangeState(EnemyStateType.Patrol);
            }
        }

        public override void Exit()
        {
        }
    }

    public class PatrolState : EnemyState
    {
        private int _currentPatrolIndex;

        public PatrolState(AbyssalDungeon.Enemy.EnemyController owner) : base(owner) { }

        public override void Enter()
        {
            if (owner.PatrolPoints.Length == 0)
            {
                owner.ChangeState(EnemyStateType.Idle);
                return;
            }

            if (owner.Agent != null && owner.Agent.isActiveAndEnabled)
            {
                owner.Agent.isStopped = false;
                owner.Agent.SetDestination(owner.PatrolPoints[_currentPatrolIndex].position);
            }
        }

        public override void Execute()
        {
            if (owner.IsPlayerInDetectionRange())
            {
                owner.ChangeState(EnemyStateType.Detect);
                return;
            }

            if (owner.Agent != null && owner.Agent.remainingDistance <= owner.Agent.stoppingDistance)
            {
                _currentPatrolIndex = (_currentPatrolIndex + 1) % owner.PatrolPoints.Length;
                owner.ChangeState(EnemyStateType.Idle);
            }
        }

        public override void Exit()
        {
        }
    }

    public class DetectState : EnemyState
    {
        private float _detectDuration = 1.5f;
        private float _timer = 0f;

        public DetectState(AbyssalDungeon.Enemy.EnemyController owner) : base(owner) { }

        public override void Enter()
        {
            _timer = 0f;
            if (owner.Agent != null && owner.Agent.isActiveAndEnabled)
            {
                owner.Agent.isStopped = true;
            }
            // Trigger alert animation
        }

        public override void Execute()
        {
            _timer += Time.deltaTime;

            if (owner.Target != null)
            {
                Vector3 direction = (owner.Target.position - owner.transform.position).normalized;
                direction.y = 0;
                owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
            }

            if (_timer >= _detectDuration)
            {
                owner.ChangeState(EnemyStateType.Chase);
            }
        }

        public override void Exit()
        {
        }
    }

    public class ChaseState : EnemyState
    {
        public ChaseState(AbyssalDungeon.Enemy.EnemyController owner) : base(owner) { }

        public override void Enter()
        {
            if (owner.Agent != null && owner.Agent.isActiveAndEnabled)
            {
                owner.Agent.isStopped = false;
            }
        }

        public override void Execute()
        {
            if (owner.Target == null || !owner.IsPlayerInDetectionRange())
            {
                owner.Target = null;
                owner.ChangeState(EnemyStateType.Patrol);
                return;
            }

            if (owner.IsPlayerInAttackRange())
            {
                owner.ChangeState(EnemyStateType.Attack);
                return;
            }

            if (owner.Agent != null && owner.Agent.isActiveAndEnabled)
            {
                owner.Agent.SetDestination(owner.Target.position);
            }
        }

        public override void Exit()
        {
        }
    }

    public class AttackState : EnemyState
    {
        private float _attackCooldown;
        private float _timer;

        public AttackState(AbyssalDungeon.Enemy.EnemyController owner) : base(owner) { }

        public override void Enter()
        {
            _attackCooldown = 2f; // Get from data later
            _timer = _attackCooldown;
            if (owner.Agent != null && owner.Agent.isActiveAndEnabled)
            {
                owner.Agent.isStopped = true;
            }
        }

        public override void Execute()
        {
            if (owner.Target == null)
            {
                owner.ChangeState(EnemyStateType.Idle);
                return;
            }

            Vector3 direction = (owner.Target.position - owner.transform.position).normalized;
            direction.y = 0;
            owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 10f);

            _timer += Time.deltaTime;
            if (_timer >= _attackCooldown)
            {
                owner.PerformAttack();
                _timer = 0f;
            }

            if (!owner.IsPlayerInAttackRange())
            {
                owner.ChangeState(EnemyStateType.Chase);
            }
        }

        public override void Exit()
        {
        }
    }

    public class HitState : EnemyState
    {
        private float _staggerDuration = 0.5f;
        private float _timer;

        public HitState(AbyssalDungeon.Enemy.EnemyController owner) : base(owner) { }

        public override void Enter()
        {
            _timer = 0f;
            if (owner.Agent != null && owner.Agent.isActiveAndEnabled)
            {
                owner.Agent.isStopped = true;
            }
        }

        public override void Execute()
        {
            _timer += Time.deltaTime;
            if (_timer >= _staggerDuration)
            {
                if (owner.Target != null)
                {
                    owner.ChangeState(EnemyStateType.Chase);
                }
                else
                {
                    owner.ChangeState(EnemyStateType.Idle);
                }
            }
        }

        public override void Exit()
        {
        }
    }

    public class StunnedState : EnemyState
    {
        private float _stunDuration;
        private float _timer;

        public StunnedState(AbyssalDungeon.Enemy.EnemyController owner, float duration) : base(owner) 
        {
            _stunDuration = duration;
        }

        public override void Enter()
        {
            _timer = 0f;
            if (owner.Agent != null && owner.Agent.isActiveAndEnabled)
            {
                owner.Agent.isStopped = true;
            }
        }

        public override void Execute()
        {
            _timer += Time.deltaTime;
            if (_timer >= _stunDuration)
            {
                owner.ChangeState(EnemyStateType.Idle);
            }
        }

        public override void Exit()
        {
        }
    }

    public class RetreatState : EnemyState
    {
        public RetreatState(AbyssalDungeon.Enemy.EnemyController owner) : base(owner) { }

        public override void Enter()
        {
            if (owner.Agent != null && owner.Agent.isActiveAndEnabled)
            {
                owner.Agent.isStopped = false;
            }
        }

        public override void Execute()
        {
            if (owner.Target != null && owner.Agent != null && owner.Agent.isActiveAndEnabled)
            {
                Vector3 dirAwayFromPlayer = (owner.transform.position - owner.Target.position).normalized;
                Vector3 retreatPos = owner.transform.position + dirAwayFromPlayer * 5f;
                owner.Agent.SetDestination(retreatPos);
            }

            if (!owner.IsPlayerInDetectionRange())
            {
                owner.ChangeState(EnemyStateType.Idle);
            }
        }

        public override void Exit()
        {
        }
    }

    public class DeadState : EnemyState
    {
        public DeadState(AbyssalDungeon.Enemy.EnemyController owner) : base(owner) { }

        public override void Enter()
        {
            if (owner.Agent != null && owner.Agent.isActiveAndEnabled)
            {
                owner.Agent.isStopped = true;
                owner.Agent.enabled = false;
            }
            
            // Handle death logic in controller
        }

        public override void Execute()
        {
        }

        public override void Exit()
        {
        }
    }
}

using UnityEngine;
using AbyssalDungeon.Enemy;

namespace AbyssalDungeon.AI
{
    public class AssassinEnemyBehavior : EnemyController
    {
        [Header("Assassin Settings")]
        [SerializeField] private float _dashSpeed = 20f;
        [SerializeField] private float _dashDuration = 0.2f;
        
        private bool _isDashing;

        protected override void Update()
        {
            base.Update();
            
            // Logic to find lowest HP target could be implemented in DetectPlayer override
            // For now, relies on base target acquisition
        }

        public override void PerformAttack()
        {
            if (!_isDashing && Target != null)
            {
                StartCoroutine(DashAttack());
            }
        }

        private System.Collections.IEnumerator DashAttack()
        {
            _isDashing = true;
            
            if (Agent != null && Agent.isActiveAndEnabled)
            {
                Agent.isStopped = true;
                Agent.enabled = false;
            }

            Vector3 dashDirection = (Target.position - transform.position).normalized;
            float timer = 0f;

            while (timer < _dashDuration)
            {
                transform.position += dashDirection * _dashSpeed * Time.deltaTime;
                timer += Time.deltaTime;
                yield return null;
            }

            base.PerformAttack();

            if (Agent != null)
            {
                Agent.enabled = true;
                Agent.Warp(transform.position);
            }
            
            ChangeState(EnemyStateType.Retreat);
            _isDashing = false;
        }
    }
}

using UnityEngine;
using AbyssalDungeon.Enemy;

namespace AbyssalDungeon.AI
{
    public class TankEnemyBehavior : EnemyController
    {
        [Header("Tank Settings")]
        [SerializeField] private float _blockChance = 0.3f;
        [SerializeField] private GameObject _shieldVfx;
        
        private bool _isBlocking;

        public override void TakeDamage(AbyssalDungeon.Combat.DamageInfo info)
        {
            if (StateMachine.CurrentStateType == EnemyStateType.Dead) return;

            if (Random.value <= _blockChance)
            {
                // Blocked
                BlockAttack();
                return;
            }

            base.TakeDamage(info);
        }

        private void BlockAttack()
        {
            _isBlocking = true;
            if (_shieldVfx != null)
            {
                _shieldVfx.SetActive(true);
                Invoke(nameof(EndBlock), 0.5f);
            }
            Debug.Log($"{gameObject.name} blocked the attack!");
        }

        private void EndBlock()
        {
            _isBlocking = false;
            if (_shieldVfx != null)
            {
                _shieldVfx.SetActive(false);
            }
        }
        
        public void Taunt(float tauntRadius, LayerMask playerLayer)
        {
            // Draw aggro logic - if multiplayer, forces players to target this enemy
            // In single player, it might just mean moving between player and other enemies
            Collider[] hits = Physics.OverlapSphere(transform.position, tauntRadius, playerLayer);
            foreach(var hit in hits)
            {
                Debug.Log($"{gameObject.name} taunted {hit.name}");
            }
        }
    }
}


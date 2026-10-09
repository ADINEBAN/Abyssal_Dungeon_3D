using UnityEngine;
using AbyssalDungeon.Combat;

namespace AbyssalDungeon.Enemy
{
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _speed = 15f;
        [SerializeField] private float _damage = 15f;
        [SerializeField] private float _lifetime = 3f;
        [SerializeField] private bool _piercing = false;

        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
        }

        public void Initialize(Vector3 direction)
        {
            _rb.velocity = direction * _speed;
            Destroy(gameObject, _lifetime);
        }

        private void OnTriggerEnter(Collider other)
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null && !other.CompareTag("Enemy"))
            {
                damageable.TakeDamage(new DamageInfo { attacker = gameObject, target = other.gameObject, baseDamage = _damage, finalDamage = _damage, damageType = DamageType.Physical });
                
                if (!_piercing)
                {
                    Destroy(gameObject);
                }
            }
            else if (!other.isTrigger && !other.CompareTag("Enemy"))
            {
                Destroy(gameObject);
            }
        }
    }
}


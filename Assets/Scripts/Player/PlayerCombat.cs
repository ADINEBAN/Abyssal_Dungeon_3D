using System;
using UnityEngine;
using AbyssalDungeon.Combat;
using AbyssalDungeon.VFX;
using System.Collections;

namespace AbyssalDungeon.Player
{
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Combat Settings")]
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackRadius = 1f;
        [SerializeField] private Transform attackPoint;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private GameObject weaponTrail;

        [Header("Cooldowns")]
        [SerializeField] private float basicAttackCooldown = 0.5f;
        [SerializeField] private float[] skillCooldowns = new float[3] { 5f, 10f, 30f };

        private PlayerStats playerStats;
        private PlayerAnimationController animController;
        private float lastAttackTime;
        private float[] lastSkillTimes = new float[3];
        private int comboStep = 0;

        public event Action OnAttack;
        public event Action OnAttackEnd;
        public event Action<int> OnSkillUsed;

        private void Awake()
        {
            playerStats = GetComponent<PlayerStats>();
            animController = GetComponent<PlayerAnimationController>();
            
            if (weaponTrail != null) weaponTrail.SetActive(false);
        }

        public void BasicAttack()
        {
            if (Time.time >= lastAttackTime + basicAttackCooldown)
            {
                lastAttackTime = Time.time;
                comboStep = (comboStep % 3) + 1;

                OnAttack?.Invoke();
                if (animController != null)
                {
                    animController.SetAttackIndex(comboStep);
                }

                StartCoroutine(PerformAttack(0.2f, 1f));
            }
        }

        public void UseSkill(int skillIndex)
        {
            if (skillIndex < 0 || skillIndex >= skillCooldowns.Length) return;

            if (Time.time >= lastSkillTimes[skillIndex] + skillCooldowns[skillIndex])
            {
                lastSkillTimes[skillIndex] = Time.time;
                OnSkillUsed?.Invoke(skillIndex);

                if (animController != null)
                {
                    animController.SetSkillIndex(skillIndex);
                }

                OnAttack?.Invoke();
                float skillMultiplier = 1.5f + (skillIndex * 0.5f);
                StartCoroutine(PerformAttack(0.3f, skillMultiplier));
            }
        }

        private IEnumerator PerformAttack(float delay, float multiplier)
        {
            if (weaponTrail != null) weaponTrail.SetActive(true);

            yield return new WaitForSeconds(delay);

            Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRadius, enemyLayer);
            foreach (Collider enemy in hitEnemies)
            {
                IDamageable damageable = enemy.GetComponent<IDamageable>();
                if (damageable != null && damageable.IsAlive())
                {
                    DamageInfo info = CreateDamageInfo(enemy.gameObject, multiplier);
                    damageable.TakeDamage(info);

                    // Spawn VFX on hit
                    if (VFXManager.Instance != null)
                    {
                        VFXType vfxType = info.isCritical ? VFXType.CriticalHit : VFXType.Hit;
                        if (multiplier > 1.5f) vfxType = VFXType.Magic; // Skill effect
                        
                        VFXManager.Instance.SpawnVFX(vfxType, enemy.transform.position + Vector3.up * 1f, 1f);
                    }
                }
            }

            yield return new WaitForSeconds(basicAttackCooldown - delay);

            if (weaponTrail != null) weaponTrail.SetActive(false);
            OnAttackEnd?.Invoke();
        }

        private DamageInfo CreateDamageInfo(GameObject target, float multiplier)
        {
            bool isCrit = UnityEngine.Random.value < playerStats.CriticalChance;
            float baseDmg = playerStats.Attack;
            
            DamageInfo info = new DamageInfo
            {
                attacker = this.gameObject,
                target = target,
                baseDamage = baseDmg,
                skillMultiplier = multiplier,
                isCritical = isCrit,
                criticalMultiplier = isCrit ? 2.0f : 1.0f,
                damageType = DamageType.Physical,
                hitPoint = target.transform.position
            };
            
            return info;
        }

        private void OnDrawGizmosSelected()
        {
            if (attackPoint == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}

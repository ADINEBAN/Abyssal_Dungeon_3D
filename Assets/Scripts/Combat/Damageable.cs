using System;
using System.Collections;
using UnityEngine;

namespace AbyssalDungeon.Combat
{
    public interface IDamageable
    {
        void TakeDamage(DamageInfo info);
        float GetCurrentHP();
        float GetMaxHP();
        bool IsAlive();
    }

    public class Damageable : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHP = 100f;
        [SerializeField] private float defense = 5f;
        [SerializeField] private Renderer mainRenderer;
        [SerializeField] private Color hitFlashColor = Color.red;
        [SerializeField] private float invincibilityDuration = 0.2f;

        private float currentHP;
        private bool isInvincible;
        private Color originalColor;
        private Material mat;

        public event Action<DamageInfo> OnDamaged;
        public event Action<float> OnHealed;
        public event Action OnDeath;

        private void Awake()
        {
            currentHP = maxHP;
            if (mainRenderer != null)
            {
                mat = mainRenderer.material;
                originalColor = mat.color;
            }
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive() || isInvincible) return;

            info.finalDamage = CombatSystem.CalculateDamage(info, defense);
            currentHP -= info.finalDamage;
            currentHP = Mathf.Clamp(currentHP, 0, maxHP);

            OnDamaged?.Invoke(info);

            StartCoroutine(HitFlashRoutine());

            if (currentHP <= 0)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive()) return;
            currentHP += amount;
            currentHP = Mathf.Clamp(currentHP, 0, maxHP);
            OnHealed?.Invoke(amount);
        }

        public float GetCurrentHP() => currentHP;
        public float GetMaxHP() => maxHP;
        public bool IsAlive() => currentHP > 0;

        private void Die()
        {
            OnDeath?.Invoke();
            // Handle death logic (e.g., destroy, disable, ragdoll)
        }

        private IEnumerator HitFlashRoutine()
        {
            isInvincible = true;
            if (mat != null)
            {
                mat.color = hitFlashColor;
            }

            yield return new WaitForSeconds(invincibilityDuration);

            if (mat != null)
            {
                mat.color = originalColor;
            }
            isInvincible = false;
        }
    }
}

using UnityEngine;
using System;

namespace AbyssalDungeon.Leveling
{
    public class LevelingSystem : MonoBehaviour
    {
        [Header("Current State")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private float currentEXP = 0f;
        [SerializeField] private int skillPoints = 0;

        [Header("Settings")]
        [SerializeField] private int levelCap = 50;

        [Header("Effects")]
        [SerializeField] private GameObject levelUpVFXPrefab;
        [SerializeField] private AudioClip levelUpSound;
        [SerializeField] private AudioSource audioSource;

        public event Action<int> OnLevelUp;

        private void Start()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        public float CalculateRequiredEXP(int level)
        {
            return 100f * level * 1.5f;
        }

        public void AddEXP(float amount)
        {
            if (currentLevel >= levelCap) return;

            currentEXP += amount;
            float requiredExp = CalculateRequiredEXP(currentLevel);

            while (currentEXP >= requiredExp && currentLevel < levelCap)
            {
                currentEXP -= requiredExp;
                LevelUp();
                requiredExp = CalculateRequiredEXP(currentLevel);
            }
        }

        private void LevelUp()
        {
            currentLevel++;
            skillPoints++;
            
            CalculateStatIncrease();
            PlayLevelUpEffects();
            
            OnLevelUp?.Invoke(currentLevel);
            Debug.Log($"Leveled up to {currentLevel}!");
        }

        private void CalculateStatIncrease()
        {
            // +10% MaxHP, +5 Attack, +3 Defense
            // Assuming this will communicate with a PlayerStats component.
            // Placeholder debug log.
            Debug.Log("Stats Increased: MaxHP +10%, Attack +5, Defense +3");
        }

        private void PlayLevelUpEffects()
        {
            if (levelUpVFXPrefab != null)
            {
                Instantiate(levelUpVFXPrefab, transform.position, Quaternion.identity, transform);
            }

            if (audioSource != null && levelUpSound != null)
            {
                audioSource.PlayOneShot(levelUpSound);
            }
        }

        public int GetCurrentLevel() => currentLevel;
        public float GetCurrentEXP() => currentEXP;
        public int GetSkillPoints() => skillPoints;
    }
}

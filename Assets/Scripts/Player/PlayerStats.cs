using System;
using UnityEngine;

namespace AbyssalDungeon.Player
{
    [CreateAssetMenu(fileName = "NewClassData", menuName = "AbyssalDungeon/Player/ClassData")]
    public class ClassData : ScriptableObject
    {
        public string className = "Warrior";
        public float baseHP = 100f;
        public float baseMana = 50f;
        public float baseAttack = 10f;
        public float baseDefense = 5f;
        public float baseAttackSpeed = 1f;
        public float baseCriticalChance = 0.05f;
        public float baseMovementSpeed = 5f;

        public float hpPerLevel = 10f;
        public float attackPerLevel = 2f;
    }

    public class PlayerStats : MonoBehaviour
    {
        [Header("Class Data")]
        [SerializeField] private ClassData classData;

        [Header("Current Stats")]
        [SerializeField] private float hp;
        [SerializeField] private float maxHP;
        [SerializeField] private float mana;
        [SerializeField] private float maxMana;

        [Header("Progression")]
        [SerializeField] private int level = 1;
        [SerializeField] private int currentEXP = 0;
        [SerializeField] private int requiredEXP = 100;
        [SerializeField] private int gold = 0;

        [Header("Combat Stats")]
        [SerializeField] private float attack;
        [SerializeField] private float defense;
        [SerializeField] private float attackSpeed;
        [SerializeField] private float criticalChance;
        [SerializeField] private float movementSpeed;

        public float HP => hp;
        public float MaxHP => maxHP;
        public float Mana => mana;
        public float MaxMana => maxMana;
        public int Level => level;
        public float Attack => attack;
        public float Defense => defense;
        public float CriticalChance => criticalChance;
        public bool isDead => hp <= 0;

        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnManaChanged;
        public event Action<int, int> OnExpChanged;
        public event Action<int> OnGoldChanged;
        public event Action<int> OnLevelUp;
        public event Action OnDeath;

        private void Start()
        {
            if (classData != null)
            {
                InitializeFromClass(classData);
            }
        }

        public void InitializeFromClass(ClassData data)
        {
            classData = data;
            maxHP = data.baseHP;
            hp = maxHP;
            maxMana = data.baseMana;
            mana = maxMana;

            attack = data.baseAttack;
            defense = data.baseDefense;
            attackSpeed = data.baseAttackSpeed;
            criticalChance = data.baseCriticalChance;
            movementSpeed = data.baseMovementSpeed;

            CalculateRequiredEXP();

            OnHealthChanged?.Invoke(hp, maxHP);
            OnManaChanged?.Invoke(mana, maxMana);
        }

        public void TakeDamage(float damage)
        {
            if (isDead) return;

            hp -= damage;
            hp = Mathf.Clamp(hp, 0, maxHP);
            OnHealthChanged?.Invoke(hp, maxHP);

            if (hp <= 0)
            {
                OnDeath?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (isDead) return;

            hp += amount;
            hp = Mathf.Clamp(hp, 0, maxHP);
            OnHealthChanged?.Invoke(hp, maxHP);
        }

        public void UseMana(float amount)
        {
            mana -= amount;
            mana = Mathf.Clamp(mana, 0, maxMana);
            OnManaChanged?.Invoke(mana, maxMana);
        }

        public void AddEXP(int amount)
        {
            if (isDead) return;

            currentEXP += amount;
            OnExpChanged?.Invoke(currentEXP, requiredEXP);

            while (currentEXP >= requiredEXP)
            {
                LevelUp();
            }
        }

        public void AddGold(int amount)
        {
            gold += amount;
            OnGoldChanged?.Invoke(gold);
        }

        private void LevelUp()
        {
            currentEXP -= requiredEXP;
            level++;

            if (classData != null)
            {
                maxHP += classData.hpPerLevel;
                attack += classData.attackPerLevel;
            }

            hp = maxHP;
            CalculateRequiredEXP();

            OnLevelUp?.Invoke(level);
            OnHealthChanged?.Invoke(hp, maxHP);
            OnExpChanged?.Invoke(currentEXP, requiredEXP);
        }

        private void CalculateRequiredEXP()
        {
            requiredEXP = Mathf.FloorToInt(100 * level * 1.5f);
        }
    }
}

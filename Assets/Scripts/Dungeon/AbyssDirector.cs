using UnityEngine;
using UnityEngine.Events;

namespace AbyssalDungeon.Dungeon
{
    public class AbyssDirector : MonoBehaviour
    {
        public static AbyssDirector Instance { get; private set; }

        [Header("Tracking Statistics")]
        public int playerDeaths = 0;
        public float damageDealt = 0f;
        public float damageTaken = 0f;
        public float completionTime = 0f;
        public float healingAmount = 0f;
        public float monsterKillSpeed = 0f;

        [Header("Difficulty Output")]
        [SerializeField, Range(0, 100)] private float difficultyScore = 50f;
        
        [Header("Configuration")]
        [SerializeField] private float minDifficulty = 10f;
        [SerializeField] private float maxDifficulty = 100f;

        public float EnemyHpMultiplier { get; private set; } = 1.0f;
        public float EnemyDamageMultiplier { get; private set; } = 1.0f;

        [Header("Events")]
        public UnityEvent<float> OnDifficultyChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (DungeonManager.Instance != null)
            {
                DungeonManager.Instance.OnRoomChanged.AddListener(EvaluateDifficulty);
            }
        }

        public void RegisterDamageDealt(float amount) => damageDealt += amount;
        public void RegisterDamageTaken(float amount) => damageTaken += amount;
        public void RegisterHealing(float amount) => healingAmount += amount;
        public void RegisterPlayerDeath() => playerDeaths++;

        private void EvaluateDifficulty(RoomManager newRoom)
        {
            // Simple heuristic to calculate difficulty score based on tracked stats
            float performanceScore = 50f; 
            
            // Higher damage ratio means doing well
            float damageRatio = damageTaken > 0 ? (damageDealt / damageTaken) : 10f;
            performanceScore += damageRatio * 5f;
            
            // Penalize for deaths
            performanceScore -= playerDeaths * 15f;

            difficultyScore = Mathf.Clamp(performanceScore, minDifficulty, maxDifficulty);

            ApplyDifficultyAdjustments();
            
            // Reset room-specific metrics if needed
            damageDealt = 0f;
            damageTaken = 0f;
        }

        private void ApplyDifficultyAdjustments()
        {
            if (difficultyScore > 70f)
            {
                // Party too strong
                EnemyHpMultiplier = 1.5f;
                EnemyDamageMultiplier = 1.25f;
                Debug.Log("Abyss Director: Increasing Difficulty (Spawning Elites)");
            }
            else if (difficultyScore < 30f)
            {
                // Party too weak
                EnemyHpMultiplier = 0.8f;
                EnemyDamageMultiplier = 0.8f;
                Debug.Log("Abyss Director: Decreasing Difficulty (Spawning Healing)");
            }
            else
            {
                // Balanced
                EnemyHpMultiplier = 1.0f;
                EnemyDamageMultiplier = 1.0f;
            }

            OnDifficultyChanged?.Invoke(difficultyScore);
        }
    }
}

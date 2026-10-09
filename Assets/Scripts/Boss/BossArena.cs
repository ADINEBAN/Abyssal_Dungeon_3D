using UnityEngine;
using AbyssalDungeon.Dungeon;
using TMPro;
using UnityEngine.UI;

namespace AbyssalDungeon.Boss
{
    [RequireComponent(typeof(BoxCollider))]
    public class BossArena : MonoBehaviour
    {
        [Header("Arena Configuration")]
        [SerializeField] private BossController bossPrefab;
        [SerializeField] private Transform bossSpawnPoint;
        [SerializeField] private DoorController[] arenaDoors;
        
        [Header("UI & Effects")]
        [SerializeField] private GameObject bossHealthUI;
        [SerializeField] private Image bossHealthBar;
        [SerializeField] private TextMeshProUGUI victoryText;
        [SerializeField] private AudioSource arenaMusicSource;
        [SerializeField] private AudioClip bossMusic;
        
        private BossController spawnedBoss;
        private bool isArenaActive = false;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (bossHealthUI != null) bossHealthUI.SetActive(false);
            if (victoryText != null) victoryText.gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isArenaActive && other.CompareTag("Player"))
            {
                ActivateArena();
            }
        }

        private void ActivateArena()
        {
            isArenaActive = true;
            
            // Close and lock doors
            foreach (var door in arenaDoors)
            {
                if (door != null) door.Lock();
            }

            // Spawn boss
            if (bossPrefab != null && bossSpawnPoint != null)
            {
                spawnedBoss = Instantiate(bossPrefab, bossSpawnPoint.position, bossSpawnPoint.rotation);
                spawnedBoss.OnHealthChanged.AddListener(UpdateHealthUI);
                spawnedBoss.OnBossDeath.AddListener(OnBossDefeated);
            }

            // UI
            if (bossHealthUI != null) bossHealthUI.SetActive(true);

            // Music
            if (arenaMusicSource != null && bossMusic != null)
            {
                arenaMusicSource.clip = bossMusic;
                arenaMusicSource.Play();
            }

            Debug.Log("Boss Arena Activated!");
        }

        private void UpdateHealthUI(float currentHealth, float maxHealth)
        {
            if (bossHealthBar != null)
            {
                bossHealthBar.fillAmount = currentHealth / maxHealth;
            }
        }

        private void OnBossDefeated()
        {
            if (bossHealthUI != null) bossHealthUI.SetActive(false);
            
            if (victoryText != null)
            {
                victoryText.text = "VICTORY!";
                victoryText.gameObject.SetActive(true);
            }

            // Unlock doors
            foreach (var door in arenaDoors)
            {
                if (door != null) door.Unlock();
            }
            
            if (arenaMusicSource != null) arenaMusicSource.Stop();

            Debug.Log("Boss Defeated, Arena cleared!");
        }
    }
}

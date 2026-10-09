using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

namespace AbyssalDungeon.UI
{
    public class EndGameUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private CanvasGroup mainGroup;
        
        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI expText;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI damageDealtText;
        [SerializeField] private TextMeshProUGUI damageTakenText;
        [SerializeField] private TextMeshProUGUI healingText;
        [SerializeField] private TextMeshProUGUI monstersKilledText;
        [SerializeField] private TextMeshProUGUI timeText;

        [Header("Buttons")]
        [SerializeField] private Button townButton;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;

        [Header("Loot")]
        [SerializeField] private Transform lootGrid;

        private void Start()
        {
            mainGroup.alpha = 0;
            gameObject.SetActive(false);

            townButton.onClick.AddListener(() => SceneManager.LoadScene("Lobby"));
            playAgainButton.onClick.AddListener(() => SceneManager.LoadScene("Dungeon"));
            mainMenuButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
            quitButton.onClick.AddListener(() => Application.Quit());
        }

        public void Show(bool isVictory)
        {
            gameObject.SetActive(true);
            titleText.text = isVictory ? "DUNGEON COMPLETE" : "DUNGEON FAILED";
            titleText.color = isVictory ? Color.yellow : Color.red;
            
            StartCoroutine(FadeIn());
        }

        public void SetStats(int level, int exp, int gold)
        {
            levelText.text = "Level: " + level;
            expText.text = "EXP: " + exp;
            goldText.text = "Gold: " + gold;
        }

        public void SetStatistics(int dmgDealt, int dmgTaken, int healing, int kills, string time)
        {
            damageDealtText.text = "Damage Dealt: " + dmgDealt;
            damageTakenText.text = "Damage Taken: " + dmgTaken;
            healingText.text = "Healing: " + healing;
            monstersKilledText.text = "Monsters Killed: " + kills;
            timeText.text = "Time: " + time;
        }

        private IEnumerator FadeIn()
        {
            float t = 0;
            while(t < 0.5f)
            {
                t += Time.deltaTime;
                mainGroup.alpha = t / 0.5f;
                yield return null;
            }
            mainGroup.alpha = 1;
            mainGroup.interactable = true;
            mainGroup.blocksRaycasts = true;
        }
    }
}

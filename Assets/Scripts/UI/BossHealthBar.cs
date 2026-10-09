using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace AbyssalDungeon.UI
{
    public class BossHealthBar : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Slider hpBar;
        [SerializeField] private Slider hpBarSlow;
        [SerializeField] private TextMeshProUGUI bossNameText;
        [SerializeField] private TextMeshProUGUI phaseText;
        
        [Header("Colors")]
        [SerializeField] private Color[] phaseColors;
        [SerializeField] private Image fillImage;

        private float currentHP;
        private float maxHP;

        private void Start()
        {
            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }

        public void ShowBoss(string name, float hp, int totalPhases)
        {
            gameObject.SetActive(true);
            bossNameText.text = name;
            maxHP = hp;
            currentHP = hp;
            hpBar.maxValue = maxHP;
            hpBar.value = maxHP;
            hpBarSlow.maxValue = maxHP;
            hpBarSlow.value = maxHP;
            SetPhase(1);
            
            StartCoroutine(FadeIn());
        }

        public void UpdateHealth(float hp)
        {
            currentHP = hp;
            hpBar.value = currentHP;
        }

        private void Update()
        {
            if (hpBarSlow.value > hpBar.value)
            {
                hpBarSlow.value -= Time.deltaTime * maxHP * 0.2f;
            }
        }

        public void SetPhase(int phase)
        {
            phaseText.text = "Phase " + phase;
            if (phaseColors != null && phaseColors.Length > 0)
            {
                int index = Mathf.Clamp(phase - 1, 0, phaseColors.Length - 1);
                fillImage.color = phaseColors[index];
            }
        }

        public void HideBoss()
        {
            StartCoroutine(FadeOut());
        }

        private IEnumerator FadeIn()
        {
            float t = 0;
            while(t < 1f)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = t;
                yield return null;
            }
            canvasGroup.alpha = 1;
        }

        private IEnumerator FadeOut()
        {
            float t = 0;
            while(t < 1f)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = 1 - t;
                yield return null;
            }
            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }
    }
}

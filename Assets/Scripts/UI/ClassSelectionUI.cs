using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace AbyssalDungeon.UI
{
    [System.Serializable]
    public class ClassCard
    {
        public Button cardButton;
        public Image iconImage;
        public TextMeshProUGUI classNameText;
        public TextMeshProUGUI roleText;
        public GameObject highlight;
    }

    public class ClassSelectionUI : MonoBehaviour
    {
        [Header("Cards")]
        [SerializeField] private List<ClassCard> classCards;

        [Header("Details Panel")]
        [SerializeField] private TextMeshProUGUI detailNameText;
        [SerializeField] private TextMeshProUGUI detailRoleText;
        [SerializeField] private TextMeshProUGUI detailStatsText;
        [SerializeField] private TextMeshProUGUI detailSkillsText;
        [SerializeField] private TextMeshProUGUI detailPassiveText;

        [Header("Buttons")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button backButton;

        private int selectedIndex = -1;

        private void Start()
        {
            confirmButton.onClick.AddListener(OnConfirm);
            backButton.onClick.AddListener(OnBack);

            for (int i = 0; i < classCards.Count; i++)
            {
                int index = i; // capture
                classCards[i].cardButton.onClick.AddListener(() => OnClassSelected(index));
                classCards[i].highlight.SetActive(false);
            }

            confirmButton.interactable = false;
            UpdateDetailsPanel();
        }

        private void OnClassSelected(int index)
        {
            if (selectedIndex >= 0 && selectedIndex < classCards.Count)
            {
                classCards[selectedIndex].highlight.SetActive(false);
            }

            selectedIndex = index;
            classCards[selectedIndex].highlight.SetActive(true);
            
            confirmButton.interactable = true;
            UpdateDetailsPanel();
            
            StartCoroutine(SelectAnimation(classCards[selectedIndex].cardButton.transform));
        }

        private void UpdateDetailsPanel()
        {
            if (selectedIndex == -1)
            {
                detailNameText.text = "Select a Class";
                detailRoleText.text = "";
                detailStatsText.text = "";
                detailSkillsText.text = "";
                detailPassiveText.text = "";
                return;
            }

            ClassCard card = classCards[selectedIndex];
            detailNameText.text = card.classNameText.text;
            detailRoleText.text = card.roleText.text;
            detailStatsText.text = "HP: 100\nMP: 50\nATK: 10\nDEF: 5";
            detailSkillsText.text = "Skill 1: Slash\nSkill 2: Dash\nUltimate: Earthquake";
            detailPassiveText.text = "Passive: Health Regeneration";
        }

        private IEnumerator SelectAnimation(Transform t)
        {
            float elapsed = 0f;
            float duration = 0.15f;
            Vector3 startScale = Vector3.one;
            Vector3 targetScale = Vector3.one * 1.1f;

            while(elapsed < duration)
            {
                elapsed += Time.deltaTime;
                t.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
                yield return null;
            }

            elapsed = 0f;
            while(elapsed < duration)
            {
                elapsed += Time.deltaTime;
                t.localScale = Vector3.Lerp(targetScale, startScale, elapsed / duration);
                yield return null;
            }
            t.localScale = startScale;
        }

        private void OnConfirm()
        {
            if (selectedIndex != -1)
            {
                SceneManager.LoadScene("Dungeon");
            }
        }

        private void OnBack()
        {
            SceneManager.LoadScene("Lobby");
        }
    }
}

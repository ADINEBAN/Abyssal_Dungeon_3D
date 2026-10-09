using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace AbyssalDungeon.UI
{
    [System.Serializable]
    public class SkillSlotUI
    {
        public Image skillIcon;
        public Image cooldownOverlay;
        public TextMeshProUGUI cooldownText;
        public TextMeshProUGUI keyText;
    }

    public class PlayerHUD : MonoBehaviour
    {
        [Header("Character Info")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Slider hpBar;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private Slider manaBar;
        [SerializeField] private TextMeshProUGUI manaText;

        [Header("Skills")]
        [SerializeField] private SkillSlotUI qSkill;
        [SerializeField] private SkillSlotUI wSkill;
        [SerializeField] private SkillSlotUI eSkill;
        [SerializeField] private SkillSlotUI rSkill;

        [Header("Dungeon Info")]
        [SerializeField] private TextMeshProUGUI objectiveText;
        [SerializeField] private TextMeshProUGUI roomProgressText;
        [SerializeField] private GameObject minimapPlaceholder;

        private void Start()
        {
            UpdateHP(100, 100);
            UpdateMana(50, 50);
            UpdateLevel(1);
            SetObjective("Clear the dungeon", "Room 1/5");
        }

        public void UpdateHP(float current, float max)
        {
            if (hpBar != null) hpBar.maxValue = max;
            if (hpBar != null) hpBar.value = current;
            if (hpText != null) hpText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        public void UpdateMana(float current, float max)
        {
            if (manaBar != null) manaBar.maxValue = max;
            if (manaBar != null) manaBar.value = current;
            if (manaText != null) manaText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        public void UpdateLevel(int level)
        {
            if (levelText != null) levelText.text = level.ToString();
        }

        public void UpdateSkillCooldown(int skillIndex, float currentCD, float maxCD)
        {
            SkillSlotUI targetSlot = skillIndex switch
            {
                0 => qSkill,
                1 => wSkill,
                2 => eSkill,
                3 => rSkill,
                _ => null
            };

            if (targetSlot == null) return;

            if (currentCD <= 0)
            {
                targetSlot.cooldownOverlay.fillAmount = 0;
                targetSlot.cooldownText.text = "";
            }
            else
            {
                targetSlot.cooldownOverlay.fillAmount = currentCD / maxCD;
                targetSlot.cooldownText.text = Mathf.CeilToInt(currentCD).ToString();
            }
        }

        public void SetObjective(string objective, string progress)
        {
            if (objectiveText != null) objectiveText.text = objective;
            if (roomProgressText != null) roomProgressText.text = progress;
        }
    }
}




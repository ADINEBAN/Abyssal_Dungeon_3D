using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace AbyssalDungeon.UI
{
    [System.Serializable]
    public class PartyMemberUI
    {
        public GameObject rootPanel;
        public TextMeshProUGUI nameText;
        public Image classIcon;
        public Slider hpBar;
        public Slider manaBar;
        public Image backgroundPanel;
    }

    public class PartyHUD : MonoBehaviour
    {
        [SerializeField] private List<PartyMemberUI> partyMembers;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color damageColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);

        private void Start()
        {
            // Hide all initially
            foreach (var member in partyMembers)
            {
                member.rootPanel.SetActive(false);
            }
        }

        public void SetPartyMemberCount(int count)
        {
            for (int i = 0; i < partyMembers.Count; i++)
            {
                partyMembers[i].rootPanel.SetActive(i < count);
            }
        }

        public void UpdateMemberInfo(int index, string name, float hpPercent, float manaPercent)
        {
            if (index < 0 || index >= partyMembers.Count) return;

            var member = partyMembers[index];
            member.nameText.text = name;
            member.hpBar.value = hpPercent;
            member.manaBar.value = manaPercent;
        }

        public void TriggerDamageFlash(int index)
        {
            if (index < 0 || index >= partyMembers.Count) return;
            StartCoroutine(FlashRoutine(partyMembers[index].backgroundPanel));
        }

        private System.Collections.IEnumerator FlashRoutine(Image bg)
        {
            bg.color = damageColor;
            float t = 0;
            while(t < 0.2f)
            {
                t += Time.deltaTime;
                bg.color = Color.Lerp(damageColor, normalColor, t / 0.2f);
                yield return null;
            }
            bg.color = normalColor;
        }
    }
}

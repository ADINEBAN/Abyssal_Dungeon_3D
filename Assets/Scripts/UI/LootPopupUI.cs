using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace AbyssalDungeon.UI
{
    public class LootPopupUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemStatsText;
        [SerializeField] private Image itemIcon;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;

        [Header("Settings")]
        [SerializeField] private float displayDuration = 3f;
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private float slideDistance = 100f;

        public void Setup(string itemName, string stats, Color rarityColor, Sprite icon)
        {
            itemNameText.text = itemName;
            itemNameText.color = rarityColor;
            itemStatsText.text = stats;
            if (icon != null) itemIcon.sprite = icon;

            StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            // Initial state
            canvasGroup.alpha = 0;
            Vector2 targetPos = rectTransform.anchoredPosition;
            Vector2 startPos = targetPos + Vector2.right * slideDistance;
            rectTransform.anchoredPosition = startPos;

            // Slide and fade in
            float t = 0;
            while(t < animationDuration)
            {
                t += Time.deltaTime;
                float normalizedTime = t / animationDuration;
                canvasGroup.alpha = normalizedTime;
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, Mathf.SmoothStep(0, 1, normalizedTime));
                yield return null;
            }

            // Wait
            yield return new WaitForSeconds(displayDuration);

            // Fade out and slide up
            t = 0;
            startPos = rectTransform.anchoredPosition;
            targetPos = startPos + Vector2.up * slideDistance;
            while(t < animationDuration)
            {
                t += Time.deltaTime;
                float normalizedTime = t / animationDuration;
                canvasGroup.alpha = 1 - normalizedTime;
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, Mathf.SmoothStep(0, 1, normalizedTime));
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}

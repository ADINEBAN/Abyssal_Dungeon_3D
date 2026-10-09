using UnityEngine;
using System.Collections;

namespace AbyssalDungeon.UI
{
    public static class UIAnimator
    {
        public static IEnumerator FadeInCoroutine(CanvasGroup cg, float duration)
        {
            if (cg == null) yield break;
            cg.gameObject.SetActive(true);
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Clamp01(t / duration);
                yield return null;
            }
            cg.alpha = 1;
        }

        public static IEnumerator FadeOutCoroutine(CanvasGroup cg, float duration)
        {
            if (cg == null) yield break;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                cg.alpha = 1 - Mathf.Clamp01(t / duration);
                yield return null;
            }
            cg.alpha = 0;
            cg.gameObject.SetActive(false);
        }

        public static IEnumerator ScaleInCoroutine(Transform tr, float duration)
        {
            if (tr == null) yield break;
            tr.gameObject.SetActive(true);
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                tr.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t / duration);
                yield return null;
            }
            tr.localScale = Vector3.one;
        }

        public static IEnumerator ScaleOutCoroutine(Transform tr, float duration)
        {
            if (tr == null) yield break;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                tr.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t / duration);
                yield return null;
            }
            tr.localScale = Vector3.zero;
            tr.gameObject.SetActive(false);
        }

        public static IEnumerator SlideInCoroutine(RectTransform rect, Vector2 startOffset, float duration)
        {
            if (rect == null) yield break;
            Vector2 targetPos = rect.anchoredPosition;
            Vector2 startPos = targetPos + startOffset;
            rect.anchoredPosition = startPos;
            rect.gameObject.SetActive(true);

            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float eval = Mathf.SmoothStep(0, 1, t / duration);
                rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, eval);
                yield return null;
            }
            rect.anchoredPosition = targetPos;
        }

        public static IEnumerator SlideOutCoroutine(RectTransform rect, Vector2 endOffset, float duration)
        {
            if (rect == null) yield break;
            Vector2 startPos = rect.anchoredPosition;
            Vector2 endPos = startPos + endOffset;

            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float eval = Mathf.SmoothStep(0, 1, t / duration);
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, eval);
                yield return null;
            }
            rect.anchoredPosition = endPos;
            rect.gameObject.SetActive(false);
            rect.anchoredPosition = startPos; // reset for next time
        }

        public static IEnumerator PunchScaleCoroutine(Transform tr, float amount, float duration)
        {
            if (tr == null) yield break;
            Vector3 originalScale = tr.localScale;
            Vector3 targetScale = originalScale * amount;

            float halfDuration = duration / 2f;
            float t = 0;
            while (t < halfDuration)
            {
                t += Time.deltaTime;
                tr.localScale = Vector3.Lerp(originalScale, targetScale, t / halfDuration);
                yield return null;
            }

            t = 0;
            while (t < halfDuration)
            {
                t += Time.deltaTime;
                tr.localScale = Vector3.Lerp(targetScale, originalScale, t / halfDuration);
                yield return null;
            }
            tr.localScale = originalScale;
        }
    }
}

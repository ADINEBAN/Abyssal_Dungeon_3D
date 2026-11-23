using UnityEngine;
using UnityEngine.UI;

namespace AbyssGame
{
    public class UIGlowPulseBehaviour : MonoBehaviour
    {
        public Outline OutlineTarget;
        public Color ColorA = new Color(1f, 0.88f, 0.30f, 1f);
        public Color ColorB = new Color(1f, 0.55f, 0.15f, 0.5f);

        private void Update()
        {
            if (OutlineTarget == null) return;
            float s = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4.2f);
            OutlineTarget.effectColor = Color.Lerp(ColorB, ColorA, s);
            float d = Mathf.Lerp(1.5f, 2.6f, s);
            OutlineTarget.effectDistance = new Vector2(d, -d);
        }
    }
}

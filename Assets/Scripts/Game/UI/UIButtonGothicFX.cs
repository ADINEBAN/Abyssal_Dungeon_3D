using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AbyssGame
{
    /// <summary>
    /// Interactive Gothic Button feedback component:
    /// - Hover: Brightens antique gold border & metallic plate
    /// - Pressed: Insets label downward by 2px like forged metal
    /// - Selected: Displays illuminated golden rune plate
    /// - Disabled: Low-contrast weathered dark iron
    /// </summary>
    public class UIButtonGothicFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Image targetImage;
        public Outline borderOutline;
        public RectTransform labelRect;
        public UIButtonStyle style = UIButtonStyle.Secondary;
        public Color baseColor = Color.white;
        public Color baseOutlineColor = new Color(0.85f, 0.69f, 0.34f, 0.85f);

        private Vector2 _labelBaseMin;
        private Vector2 _labelBaseMax;
        private bool _hasLabelBase;
        private bool _hovered;
        private bool _pressed;

        private void Awake()
        {
            if (targetImage == null) targetImage = GetComponent<Image>();
            if (borderOutline == null) borderOutline = GetComponent<Outline>();
            CacheLabelOffsets();
            ApplyVisualState();
        }

        public void CacheLabelOffsets()
        {
            if (labelRect == null)
            {
                var txt = GetComponentInChildren<Text>();
                if (txt != null) labelRect = txt.rectTransform;
            }
            if (labelRect != null && !_hasLabelBase)
            {
                _labelBaseMin = labelRect.offsetMin;
                _labelBaseMax = labelRect.offsetMax;
                _hasLabelBase = true;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            ApplyVisualState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
            ApplyVisualState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            ApplyVisualState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
            ApplyVisualState();
        }

        private void OnDisable()
        {
            _hovered = false;
            _pressed = false;
            ApplyVisualState();
        }

        public void ApplyVisualState()
        {
            if (targetImage == null) return;
            var btn = GetComponent<Button>();
            bool interactable = btn == null || btn.interactable;

            if (!interactable || style == UIButtonStyle.Disabled)
            {
                targetImage.color = new Color(0.55f, 0.50f, 0.45f, 0.85f);
                if (borderOutline != null) borderOutline.effectColor = new Color(0.32f, 0.28f, 0.24f, 0.65f);
            }
            else if (_pressed)
            {
                targetImage.color = new Color(baseColor.r * 0.84f, baseColor.g * 0.82f, baseColor.b * 0.78f, baseColor.a);
                if (borderOutline != null) borderOutline.effectColor = GameUIKit.ColGoldBright;
            }
            else if (_hovered)
            {
                targetImage.color = new Color(
                    Mathf.Clamp01(baseColor.r * 1.16f + 0.05f),
                    Mathf.Clamp01(baseColor.g * 1.14f + 0.04f),
                    Mathf.Clamp01(baseColor.b * 1.10f + 0.02f),
                    baseColor.a);
                if (borderOutline != null) borderOutline.effectColor = GameUIKit.ColGoldBright;
            }
            else
            {
                targetImage.color = baseColor;
                if (borderOutline != null) borderOutline.effectColor = baseOutlineColor;
            }

            if (labelRect != null && _hasLabelBase)
            {
                float shift = (_pressed && interactable) ? -2f : 0f;
                labelRect.offsetMin = _labelBaseMin + new Vector2(0f, shift);
                labelRect.offsetMax = _labelBaseMax + new Vector2(0f, shift);
            }
        }
    }
}


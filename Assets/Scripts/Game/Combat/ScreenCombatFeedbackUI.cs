using UnityEngine;
using UnityEngine.UI;

namespace AbyssGame
{
    /// <summary>
    /// Fullscreen Screen Combat Feedback & Stylish Combo / Enemy Poise HUD (Phase 3 - Section 3.2):
    /// - Procedural radial border-only vignette texture (keeps center screen 100% clear for gameplay)
    /// - Crimson border flash on taking damage + low-HP (<30%) heartbeat pulse
    /// - Golden/Cyan combat surge flash on Stagger Break, Combo Finisher, and Ultimate skills
    /// - Live Stylish Combo Counter + Locked/Engaged Enemy Poise (Guard) Bar.
    /// </summary>
    public class ScreenCombatFeedbackUI : MonoBehaviour
    {
        public static ScreenCombatFeedbackUI Instance { get; private set; }

        private Image _damageEdgeOverlay;
        private Image _surgeEdgeOverlay;
        private Texture2D _vignetteTex;
        private Sprite _vignetteSprite;

        private GameObject _comboPanel;
        private Text _comboCountTxt;
        private Text _comboRankTxt;
        private RectTransform _comboTimerFill;

        private GameObject _poisePanel;
        private Text _poiseTitleTxt;
        private RectTransform _poiseFill;

        private float _damageFlashAlpha;
        private float _surgeFlashAlpha;
        private Color _surgeColor = new Color(1f, 0.85f, 0.35f);
        private ActorUnit _lastStruckEnemy;
        private float _lastStruckTimer;

        public static ScreenCombatFeedbackUI Attach(Transform canvasRoot)
        {
            if (canvasRoot == null) return null;
            var go = new GameObject("ScreenCombatFeedbackUI");
            go.transform.SetParent(canvasRoot, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.transform.SetAsFirstSibling(); // Behind interactive modals & buttons

            var ui = go.AddComponent<ScreenCombatFeedbackUI>();
            ui.BuildWidgets(go.transform);
            return ui;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_vignetteSprite != null) Destroy(_vignetteSprite);
            if (_vignetteTex != null) Destroy(_vignetteTex);
        }

        private void BuildWidgets(Transform root)
        {
            _vignetteTex = BuildBorderVignetteTexture(128);
            _vignetteSprite = Sprite.Create(_vignetteTex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));

            // 1. Crimson Damage & Low-HP Heartbeat Vignette
            var dmgGo = GameUIKit.MakeRect("DamageVignette", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _damageEdgeOverlay = dmgGo.gameObject.AddComponent<Image>();
            _damageEdgeOverlay.sprite = _vignetteSprite;
            _damageEdgeOverlay.color = new Color(0.88f, 0.06f, 0.10f, 0f);
            _damageEdgeOverlay.raycastTarget = false;

            // 2. Combat Surge Vignette (Combo Finisher / Guard Break / Ultimate)
            var surgeGo = GameUIKit.MakeRect("SurgeVignette", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _surgeEdgeOverlay = surgeGo.gameObject.AddComponent<Image>();
            _surgeEdgeOverlay.sprite = _vignetteSprite;
            _surgeEdgeOverlay.color = new Color(1f, 0.85f, 0.35f, 0f);
            _surgeEdgeOverlay.raycastTarget = false;

            // 3. Right-Side Stylish Combo Counter Card (Ancient Gothic Ornate Plaque)
            var comboImg = GameUIKit.MakeStyledPanel(
                "ComboCounterCard", root, UIPanelStyle.Ornate,
                new Vector2(1f, 0.56f), new Vector2(1f, 0.56f),
                new Vector2(-256f, -46f), new Vector2(-18f, 46f));
            comboImg.raycastTarget = false;
            _comboPanel = comboImg.gameObject;

            _comboCountTxt = GameUIKit.MakeText(
                "ComboNum", comboImg.transform, "12 HITS", 24, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.44f), new Vector2(1f, 0.96f), new Vector2(16f, 0f), new Vector2(-12f, 0f));
            _comboCountTxt.raycastTarget = false;

            _comboRankTxt = GameUIKit.MakeText(
                "ComboRank", comboImg.transform, "B • ĐIÊU LUYỆN", 12, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.18f), new Vector2(1f, 0.48f), new Vector2(16f, 0f), new Vector2(-12f, 0f));
            _comboRankTxt.raycastTarget = false;

            var timerBar = GameUIKit.MakeBar(
                "ComboDecayBar", comboImg.transform, GameUIKit.ColGold,
                new Vector2(0.06f, 0.07f), new Vector2(0.94f, 0.16f), Vector2.zero, Vector2.zero);
            _comboTimerFill = timerBar.fill;
            _comboPanel.SetActive(false);

            // 4. Engaged / Locked Target Poise (Stagger / Guard) Indicator
            var poiseImg = GameUIKit.MakeStyledPanel(
                "TargetPoiseCard", root, UIPanelStyle.Notification,
                new Vector2(0.5f, 0.79f), new Vector2(0.5f, 0.79f),
                new Vector2(-190f, -22f), new Vector2(190f, 22f));
            poiseImg.raycastTarget = false;
            _poisePanel = poiseImg.gameObject;

            _poiseTitleTxt = GameUIKit.MakeText(
                "PoiseLbl", poiseImg.transform, "", 12, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.48f), new Vector2(1f, 0.96f), new Vector2(10f, 0f), new Vector2(-10f, 0f));
            _poiseTitleTxt.raycastTarget = false;

            var poiseBar = GameUIKit.MakeBar(
                "PoiseBar", poiseImg.transform, new Color(0.88f, 0.68f, 0.24f),
                new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.46f), Vector2.zero, Vector2.zero);
            _poiseFill = poiseBar.fill;
            _poisePanel.SetActive(false);
        }

        private static Texture2D BuildBorderVignetteTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                float ny = (y / (float)(size - 1)) * 2f - 1f;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x / (float)(size - 1)) * 2f - 1f;
                    // Elliptical corner/border distance leaving inner 62% completely transparent
                    float dist = Mathf.Sqrt(nx * nx + ny * ny);
                    float edge = Mathf.Clamp01((dist - 0.68f) / 0.58f);
                    byte a = (byte)Mathf.RoundToInt(edge * edge * 255f);
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false);
            return tex;
        }

        public void NotifyPlayerDamaged(float maxHpFraction)
        {
            float add = Mathf.Clamp(0.28f + maxHpFraction * 1.4f, 0.28f, 0.72f);
            _damageFlashAlpha = Mathf.Max(_damageFlashAlpha, add);
        }

        public void NotifyCombatSurge(Color color, float intensity = 0.36f)
        {
            _surgeColor = color;
            _surgeFlashAlpha = Mathf.Max(_surgeFlashAlpha, Mathf.Clamp(intensity, 0.15f, 0.55f));
        }

        public void NotifyEnemyStruck(ActorUnit enemy)
        {
            if (enemy == null || enemy.Team == Team.Hero) return;
            _lastStruckEnemy = enemy;
            _lastStruckTimer = 3.2f;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime > 0.0001f ? Mathf.Min(0.05f, Time.unscaledDeltaTime) : 0.016f;

            // Low-HP Heartbeat Vignette + Hit Flash
            float lowHpPulse = 0f;
            var dw = DungeonWorldBuilder.Instance;
            ActorUnit hero = dw != null ? dw.LocalHero : null;
            if (hero != null && !hero.IsDead)
            {
                float hpRatio = hero.Hp / Mathf.Max(1f, hero.MaxHp);
                if (hpRatio < 0.32f)
                {
                    float danger = 1f - Mathf.Clamp01(hpRatio / 0.32f);
                    float beat = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6.5f);
                    lowHpPulse = danger * (0.16f + 0.22f * beat);
                }
            }

            if (_damageFlashAlpha > 0f)
                _damageFlashAlpha = Mathf.MoveTowards(_damageFlashAlpha, 0f, 1.85f * dt);

            if (_damageEdgeOverlay != null)
            {
                float a = Mathf.Clamp01(Mathf.Max(_damageFlashAlpha, lowHpPulse));
                _damageEdgeOverlay.color = new Color(0.86f, 0.06f, 0.10f, a);
            }

            if (_surgeFlashAlpha > 0f)
                _surgeFlashAlpha = Mathf.MoveTowards(_surgeFlashAlpha, 0f, 2.2f * dt);

            if (_surgeEdgeOverlay != null)
                _surgeEdgeOverlay.color = new Color(_surgeColor.r, _surgeColor.g, _surgeColor.b, _surgeFlashAlpha);

            // Stylish Combo Counter Widget
            var feel = CombatFeelManager.I;
            if (_comboPanel != null && feel != null)
            {
                bool showCombo = feel.ComboCount >= 2 && feel.ComboTimer > 0.05f;
                _comboPanel.SetActive(showCombo);
                if (showCombo)
                {
                    string rank = feel.GetComboRankLabel(out Color rc);
                    _comboCountTxt.text = $"{feel.ComboCount} COMBO!";
                    _comboCountTxt.color = Color.Lerp(Color.white, rc, 0.45f);
                    _comboRankTxt.text = rank;
                    _comboRankTxt.color = rc;
                    GameUIKit.SetBarRatio(_comboTimerFill, feel.ComboTimer / CombatFeelManager.ComboDecayDuration);
                }
            }

            // Locked / Last-Struck Enemy Poise (Stagger) Meter
            if (_lastStruckTimer > 0f) _lastStruckTimer -= Time.deltaTime;
            ActorUnit focusEnemy = null;
            if (CameraTargetLockSystem.Instance != null && CameraTargetLockSystem.Instance.IsLocked)
                focusEnemy = CameraTargetLockSystem.Instance.LockedTarget;
            if (focusEnemy == null && _lastStruckTimer > 0f && _lastStruckEnemy != null && !_lastStruckEnemy.IsDead)
                focusEnemy = _lastStruckEnemy;

            if (_poisePanel != null)
            {
                bool showPoise = focusEnemy != null && !focusEnemy.IsDead && focusEnemy.MaxPoise > 1f;
                _poisePanel.SetActive(showPoise);
                if (showPoise)
                {
                    if (focusEnemy.IsStaggered)
                    {
                        _poiseTitleTxt.text = $"{focusEnemy.DisplayName} — [PHÁ GIÁP! +25% SÁT THƯƠNG]";
                        _poiseTitleTxt.color = new Color(1f, 0.42f, 0.35f);
                        GameUIKit.SetBarRatio(_poiseFill, 1f);
                    }
                    else
                    {
                        _poiseTitleTxt.text = $"{focusEnemy.DisplayName} • Giáp Bền Bỉ (Poise): {focusEnemy.Poise:0}/{focusEnemy.MaxPoise:0}";
                        _poiseTitleTxt.color = new Color(1f, 0.88f, 0.55f);
                        GameUIKit.SetBarRatio(_poiseFill, focusEnemy.Poise / Mathf.Max(1f, focusEnemy.MaxPoise));
                    }
                }
            }
        }
    }
}


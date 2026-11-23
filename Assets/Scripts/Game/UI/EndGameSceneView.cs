using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssGame
{
    /// <summary>
    /// Section 4.7 End Game Summary Scene (Screen H: Victory / Defeat Screen):
    /// - Victory uses Antique Gold & Warm Bronze (UI_Panel_Ornate)
    /// - Defeat uses Dark Bronze & Blood Red (UI_Panel_Dialog)
    /// - Displays Dungeon Rewards (EXP, Gold, Boss Status), Combat Statistics, Items Obtained with Icons, and 4 Navigation Buttons
    /// </summary>
    public class EndGameSceneView : MonoBehaviour
    {
        [SerializeField] private bool _isPrebakedInScene;
        [SerializeField] private Canvas _endGameCanvas;
        [SerializeField] private Light _endLight;
        [SerializeField] private Image _summaryCard;
        [SerializeField] private Image _resultCrest;
        [SerializeField] private Text _headlineTxt;
        [SerializeField] private Text _expRewardTxt;
        [SerializeField] private Text _goldRewardTxt;
        [SerializeField] private Text _bossRewardTxt;
        [SerializeField] private Text _statsBodyTxt;
        [SerializeField] private Text _lootTitleTxt;
        [SerializeField] private Transform _lootBoxRoot;
        [SerializeField] private Button _btnTown;
        [SerializeField] private Button _btnReplay;
        [SerializeField] private Button _btnMenu;
        [SerializeField] private Button _btnQuit;

        private bool _runtimeInitialized;
        public bool IsPrebakedInScene => _isPrebakedInScene;

        private void Start()
        {
            if (_isPrebakedInScene && !_runtimeInitialized)
                InitializePrebaked();
        }

        public void InitializePrebaked()
        {
            if (_runtimeInitialized) return;
            _runtimeInitialized = true;

            RunReport rep = GameSession.I.LastRun ?? new RunReport();
            if (Application.isPlaying)
                GameAudio.I.PlayMusic(rep.victory ? GameMusic.Victory : GameMusic.Defeat);

            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = rep.victory ? new Color(0.08f, 0.055f, 0.03f) : new Color(0.09f, 0.025f, 0.025f);
            }
            if (_endLight != null)
                _endLight.color = rep.victory ? new Color(1f, 0.86f, 0.52f) : new Color(0.92f, 0.28f, 0.24f);

            RefreshSummaryDisplay(rep);

            BindEndButton(_btnTown, () => GameSession.GoToScene("Town"));
            BindEndButton(_btnReplay, () =>
            {
                RunReport cur = GameSession.I.LastRun ?? new RunReport();
                if (cur.victory)
                {
                    GameSession.I.Profile.selectedFloor = cur.floor + 1;
                    GameSession.I.SaveProfile();
                }
                GameSession.GoToScene("Dungeon");
            });
            BindEndButton(_btnMenu, () => GameSession.GoToScene("MainMenu"));
            BindEndButton(_btnQuit, () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
        }

        private static void BindEndButton(Button btn, System.Action onClick)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                GameAudio.I?.PlaySfx(GameSfx.Click, 0.6f);
                onClick?.Invoke();
            });
        }

        public void Build()
        {
            RunReport rep = GameSession.I.LastRun ?? new RunReport();
            if (Application.isPlaying)
                GameAudio.I.PlayMusic(rep.victory ? GameMusic.Victory : GameMusic.Defeat);
            Build3DPodium(rep.victory);

            var canvas = GameUIKit.CreateCanvas("EndGameCanvas");
            _endGameCanvas = canvas;
            var root = canvas.transform;

            Color themeBorder = rep.victory ? GameUIKit.ColGoldBright : GameUIKit.ColDanger;
            Color themeTint   = rep.victory ? new Color(1f, 0.98f, 0.92f, 0.98f) : new Color(0.95f, 0.82f, 0.80f, 0.98f);

            var panel = GameUIKit.MakeStyledPanel("SummaryCard", root,
                rep.victory ? UIPanelStyle.Ornate : UIPanelStyle.Dialog,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-640f, -410f), new Vector2(640f, 410f),
                themeTint, themeBorder);
            _summaryCard = panel;

            // Top Crest & Headline
            _resultCrest = GameUIKit.MakeIcon("ResultCrest", panel.transform,
                GameUIKit.GetIcon(rep.victory ? "crown" : "skull_boss"),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-28f, -66f), new Vector2(28f, -12f));

            _headlineTxt = GameUIKit.MakeText("Headline", panel.transform, "", 30, FontStyle.Bold,
                rep.victory ? GameUIKit.ColGoldBright : new Color(0.95f, 0.32f, 0.28f), TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -108f), new Vector2(-24f, -62f));

            GameUIKit.MakeGothicDivider("TopDiv", panel.transform,
                new Vector2(0.04f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, -122f), new Vector2(0f, -104f));

            // 3 Top Reward Plaques (EXP Gained, Gold Gained, Boss Status)
            var expBadge = GameUIKit.MakeStyledPanel("ExpReward", panel.transform, UIPanelStyle.Notification,
                new Vector2(0.03f, 0.74f), new Vector2(0.33f, 0.84f), Vector2.zero, Vector2.zero);
            GameUIKit.MakeIcon("Ic", expBadge.transform, GameUIKit.GetIcon("exp"),
                new Vector2(0.04f, 0.14f), new Vector2(0.22f, 0.86f), Vector2.zero, Vector2.zero);
            _expRewardTxt = GameUIKit.MakeText("Txt", expBadge.transform, "",
                13, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.MiddleLeft,
                new Vector2(0.25f, 0.08f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);

            var goldBadge = GameUIKit.MakeStyledPanel("GoldReward", panel.transform, UIPanelStyle.Notification,
                new Vector2(0.35f, 0.74f), new Vector2(0.65f, 0.84f), Vector2.zero, Vector2.zero);
            GameUIKit.MakeIcon("Ic", goldBadge.transform, GameUIKit.GetIcon("gold"),
                new Vector2(0.04f, 0.14f), new Vector2(0.22f, 0.86f), Vector2.zero, Vector2.zero);
            _goldRewardTxt = GameUIKit.MakeText("Txt", goldBadge.transform, "",
                13, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.MiddleLeft,
                new Vector2(0.25f, 0.08f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);

            var bossBadge = GameUIKit.MakeStyledPanel("BossReward", panel.transform, UIPanelStyle.Notification,
                new Vector2(0.67f, 0.74f), new Vector2(0.97f, 0.84f), Vector2.zero, Vector2.zero);
            GameUIKit.MakeIcon("Ic", bossBadge.transform, GameUIKit.GetIcon("skull_boss"),
                new Vector2(0.04f, 0.14f), new Vector2(0.22f, 0.86f), Vector2.zero, Vector2.zero);
            _bossRewardTxt = GameUIKit.MakeText("Txt", bossBadge.transform, "",
                13, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.MiddleLeft,
                new Vector2(0.25f, 0.08f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);

            // Left: Combat & Director Statistics (UI_Panel_Character)
            var left = GameUIKit.MakeStyledPanel("StatsBox", panel.transform, UIPanelStyle.Character,
                new Vector2(0.03f, 0.17f), new Vector2(0.49f, 0.72f), Vector2.zero, Vector2.zero);

            _statsBodyTxt = GameUIKit.MakeText("StatsTxt", left.transform, "", 15, FontStyle.Normal, GameUIKit.ColParchment, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(22f, 18f), new Vector2(-22f, -18f));

            // Right: Looted Items Obtained (UI_Panel_Inventory)
            var right = GameUIKit.MakeStyledPanel("LootBox", panel.transform, UIPanelStyle.Inventory,
                new Vector2(0.51f, 0.17f), new Vector2(0.97f, 0.72f), Vector2.zero, Vector2.zero);
            _lootBoxRoot = right.transform;

            GameUIKit.MakeIcon("LootIc", right.transform, GameUIKit.GetIcon("bag"),
                new Vector2(0.03f, 0.86f), new Vector2(0.11f, 0.97f), Vector2.zero, Vector2.zero);

            _lootTitleTxt = GameUIKit.MakeText("LootTitle", right.transform, "",
                16, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0.12f, 0.85f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

            // 4 Bottom Gothic Action Buttons
            _btnTown = GameUIKit.MakeStyledButton("BtnTown", panel.transform, "VỀ THỊ TRẤN (TOWN)", 16, UIButtonStyle.Secondary,
                new Vector2(0.03f, 0.035f), new Vector2(0.26f, 0.135f), Vector2.zero, Vector2.zero,
                () => GameSession.GoToScene("Town"), GameUIKit.GetIcon("anvil"));

            string replayLabel = rep.victory ? $"TIẾP TỤC TẦNG {rep.floor + 1}" : "CHƠI LẠI PHÓ BẢN";
            _btnReplay = GameUIKit.MakeStyledButton("BtnReplay", panel.transform, replayLabel, 16, UIButtonStyle.Primary,
                new Vector2(0.275f, 0.035f), new Vector2(0.535f, 0.135f), Vector2.zero, Vector2.zero, () =>
                {
                    if (rep.victory)
                    {
                        GameSession.I.Profile.selectedFloor = rep.floor + 1;
                        GameSession.I.SaveProfile();
                    }
                    GameSession.GoToScene("Dungeon");
                }, GameUIKit.GetIcon("crossed_swords"));

            _btnMenu = GameUIKit.MakeStyledButton("BtnMenu", panel.transform, "MAIN MENU", 16, UIButtonStyle.Secondary,
                new Vector2(0.55f, 0.035f), new Vector2(0.76f, 0.135f), Vector2.zero, Vector2.zero,
                () => GameSession.GoToScene("MainMenu"), GameUIKit.GetIcon("crown"));

            _btnQuit = GameUIKit.MakeStyledButton("BtnQuit", panel.transform, "THOÁT GAME", 16, UIButtonStyle.Danger,
                new Vector2(0.775f, 0.035f), new Vector2(0.97f, 0.135f), Vector2.zero, Vector2.zero, () =>
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                }, GameUIKit.GetIcon("skull_boss"));

            RefreshSummaryDisplay(rep);
            GameUIKit.OptimizeCanvasHierarchy(canvas);
            _isPrebakedInScene = true;
            _runtimeInitialized = Application.isPlaying;
        }

        private void RefreshSummaryDisplay(RunReport rep)
        {
            if (rep == null) rep = new RunReport();

            if (_resultCrest != null)
                _resultCrest.sprite = GameUIKit.GetIcon(rep.victory ? "crown" : "skull_boss");

            if (_headlineTxt != null)
            {
                _headlineTxt.text = rep.victory
                    ? $"VICTORY — KHẢI HOÀN CHINH PHỤC TẦNG {rep.floor}!"
                    : $"DEFEAT — NGÃ XUỐNG TẠI HẦM NGỤC TẦNG {rep.floor}";
                _headlineTxt.color = rep.victory ? GameUIKit.ColGoldBright : new Color(0.95f, 0.32f, 0.28f);
            }

            if (_expRewardTxt != null)
                _expRewardTxt.text = $"KINH NGHIỆM ĐẠT ĐƯỢC\n<color=#F8DC85>+{rep.expGained} EXP</color> (Lv.{rep.startLevel} → Lv.{rep.endLevel})";

            if (_goldRewardTxt != null)
                _goldRewardTxt.text = $"VÀNG CHIẾN LỢI PHẨM\n<color=#F8DC85>+{rep.goldGained} Gold</color> (Tổng: {GameSession.I.Profile.gold} G)";

            if (_bossRewardTxt != null)
            {
                string bossStatus = rep.victory ? "<color=#68E878>ĐÃ TIÊU DIỆT CHÚA TỂ</color>" : "<color=#F06868>CHƯA HẠ GỤC BOSS</color>";
                _bossRewardTxt.text = $"CHÚA TỂ HẦM NGỤC (BOSS)\n{bossStatus}";
            }

            int mins = Mathf.FloorToInt(rep.elapsedTime / 60f);
            int secs = Mathf.FloorToInt(rep.elapsedTime % 60f);

            if (_statsBodyTxt != null)
            {
                _statsBodyTxt.text =
                    $"<color=#F8DC85>BIÊN NIÊN SỬ CHIẾN ĐẤU:</color>\n\n" +
                    $"• Thời gian hoàn thành: <color=#F8DC85>{mins:00}:{secs:00}</color>\n" +
                    $"• Số khu vực vượt qua: <color=#F8DC85>{rep.roomsCleared} / {rep.totalRooms}</color>\n" +
                    $"• Quái vật tiêu diệt: <color=#F8DC85>{rep.monstersKilled}</color>\n" +
                    $"• Tổng sát thương gây ra: <color=#F8DC85>{rep.damageDealt:0}</color>\n" +
                    $"• Tổng sát thương gánh chịu: <color=#EDE0BA>{rep.damageTaken:0}</color>\n" +
                    $"• Tổng lượng hồi phục: <color=#8EE89E>{rep.healingDone:0}</color>\n" +
                    $"• Phán quyết AI Abyss Director: <color=#D9B057>{rep.directorVerdict}</color>";
            }

            if (_lootTitleTxt != null)
                _lootTitleTxt.text = $"DI VẬT & TRANG BỊ THU ĐƯỢC ({rep.lootedItems.Count}):";

            if (_lootBoxRoot != null)
            {
                for (int i = _lootBoxRoot.childCount - 1; i >= 0; i--)
                {
                    Transform ch = _lootBoxRoot.GetChild(i);
                    if (ch != null && (ch.name == "NoLoot" || ch.name.StartsWith("LootRow", System.StringComparison.Ordinal)))
                        BakedAssetRegistry.DestroySafe(ch.gameObject);
                }

                if (rep.lootedItems.Count == 0)
                {
                    GameUIKit.MakeText("NoLoot", _lootBoxRoot,
                        "Chưa thu thập được di vật nào trong lượt viễn chinh này.",
                        15, FontStyle.Italic, GameUIKit.ColMuted, TextAnchor.MiddleCenter,
                        new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.82f), Vector2.zero, Vector2.zero);
                }
                else
                {
                    int show = Mathf.Min(6, rep.lootedItems.Count);
                    for (int i = 0; i < show; i++)
                    {
                        EquipItem it = rep.lootedItems[i];
                        float top = 0.83f - i * 0.132f;
                        Color rc = GameDB.RarityColors[(int)it.rarity];
                        var row = GameUIKit.MakeStyledPanel("LootRow" + i, _lootBoxRoot, UIPanelStyle.Standard,
                            new Vector2(0.03f, top - 0.118f), new Vector2(0.97f, top), Vector2.zero, Vector2.zero, null, rc);

                        GameUIKit.MakeIcon("ItIc", row.transform, GameUIKit.GetItemSprite(it),
                            new Vector2(0.02f, 0.12f), new Vector2(0.11f, 0.88f), Vector2.zero, Vector2.zero);

                        GameUIKit.MakeText("Txt", row.transform,
                            $"[{GameDB.RarityNames[(int)it.rarity]}] {it.name} — <color=#EDE0BA>{it.Summary()}</color>",
                            13, FontStyle.Bold, rc, TextAnchor.MiddleLeft,
                            new Vector2(0.13f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero);
                    }
                }
            }

            if (_btnReplay != null)
            {
                var lbl = _btnReplay.transform.Find("Label")?.GetComponent<Text>();
                if (lbl != null)
                    lbl.text = rep.victory ? $"TIẾP TỤC TẦNG {rep.floor + 1}" : "CHƠI LẠI PHÓ BẢN";
            }
        }

        private void Build3DPodium(bool victory)
        {
            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = victory ? new Color(0.08f, 0.055f, 0.03f) : new Color(0.09f, 0.025f, 0.025f);
                Camera.main.transform.position = new Vector3(0f, 2.2f, -6.5f);
                Camera.main.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
            }
            var l = new GameObject("EndLight").AddComponent<Light>();
            l.transform.SetParent(transform, false);
            l.transform.position = new Vector3(0f, 4f, -3f);
            l.type = LightType.Point;
            l.range = 16f;
            l.intensity = 2.4f;
            l.color = victory ? new Color(1f, 0.86f, 0.52f) : new Color(0.92f, 0.28f, 0.24f);
            _endLight = l;
        }
    }
}

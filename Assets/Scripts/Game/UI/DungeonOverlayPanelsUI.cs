using System;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssGame
{
    /// <summary>
    /// Full Dungeon Map & AI Abyss Director Tactical Inspector Modal [M] (ANCIENT GOTHIC EDITION).
    /// Zero emojis: uses authentic engraved medieval & rune icons.
    /// </summary>
    public static class DungeonOverlayPanelsUI
    {
        public static GameObject BuildFullDungeonMapModal(Transform canvasRoot, Action onStateChanged)
        {
            var modal = GameUIKit.MakeStyledPanel("FullDungeonMapModal", canvasRoot, UIPanelStyle.Dialog,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-620f, -390f), new Vector2(620f, 390f));

            GameUIKit.MakeIcon("MapCrest", modal.transform, GameUIKit.GetIcon("portal"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -60f), new Vector2(66f, -20f));

            GameUIKit.MakeText("Title", modal.transform,
                "BẢN ĐỒ CHIẾN THUẬT HẦM NGỤC & BÁO CÁO AI ABYSS DIRECTOR",
                23, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0.80f, 1f), new Vector2(76f, -64f), new Vector2(0f, -14f));

            GameUIKit.MakeGothicDivider("MapDiv", modal.transform,
                new Vector2(0.03f, 1f), new Vector2(0.97f, 1f), new Vector2(0f, -74f), new Vector2(0f, -56f));

            GameUIKit.MakeStyledButton("Close", modal.transform, "ĐÓNG [M]", 15, UIButtonStyle.Danger,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-164f, -60f), new Vector2(-24f, -16f), () =>
                {
                    modal.gameObject.SetActive(false);
                    onStateChanged?.Invoke();
                });

            var content = GameUIKit.MakeRect("Content", modal.transform, Vector2.zero, Vector2.one, new Vector2(24f, 20f), new Vector2(-24f, -78f));
            var updater = modal.gameObject.AddComponent<FullMapModalUpdater>();
            updater.ContentRoot = content;

            modal.gameObject.SetActive(false);
            return modal.gameObject;
        }
    }
}

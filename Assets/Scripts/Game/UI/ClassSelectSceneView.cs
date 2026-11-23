using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssGame
{
    /// <summary>
    /// Section 4.3 & 3.10 Class Selection & Build Scene UI (PHASE 3 - Screen B):
    /// - Left 2x5 Grid of 10 Classes with Ornate Portrait Frames & Rune Selection
    /// - Center Interactive 3D Hero on Gothic Stone Pedestal with Animation Preview Controls
    /// - Right Ornate Reliquary Panel showing Attributes, Active/Ultimate Skills with Icons, and 3 Build Specializations
    /// </summary>
    public class ClassSelectSceneView : MonoBehaviour
    {
        [SerializeField] private bool _isPrebakedInScene;
        [SerializeField] private Canvas _classSelectCanvas;
        [SerializeField] private int _previewClass;
        [SerializeField] private int _previewBuild;
        [SerializeField] private Transform _previewStage;
        [SerializeField] private Transform _previewSpawnPoint;
        [SerializeField] private GameObject _previewGlowRing;
        [SerializeField] private GameObject _previewModel;
        [SerializeField] private Animator _previewAnim;
        [SerializeField] private Transform _detailContainer;
        [SerializeField] private Button[] _classButtons = new Button[10];
        [SerializeField] private Button _btnBackLobby;
        [SerializeField] private Button _btnTryAtk;
        [SerializeField] private Button _btnTryCast;
        [SerializeField] private Button _btnTryDodge;

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

            _previewClass = GameSession.I.Profile.selectedClass;
            _previewBuild = GameSession.I.Profile.selectedBuild;

            if (Application.isPlaying)
                GameAudio.I.PlayMusic(GameMusic.MainMenu);

            BindClassButton(_btnBackLobby, () => GameSession.GoToScene(GameSession.I.ReturnSceneAfterClassSelect));
            for (int i = 0; i < 10 && _classButtons != null && i < _classButtons.Length; i++)
            {
                int cid = i;
                BindClassButton(_classButtons[i], () =>
                {
                    _previewClass = cid;
                    _previewBuild = 0;
                    RefreshSelection();
                });
            }

            BindClassButton(_btnTryAtk, () => TriggerPreviewAnim("Attack"));
            BindClassButton(_btnTryCast, () => TriggerPreviewAnim("Cast"));
            BindClassButton(_btnTryDodge, () => TriggerPreviewAnim("Dodge"));

            RefreshSelection();
        }

        private static void BindClassButton(Button btn, System.Action onClick)
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
            _previewClass = GameSession.I.Profile.selectedClass;
            _previewBuild = GameSession.I.Profile.selectedBuild;

            if (Application.isPlaying)
                GameAudio.I.PlayMusic(GameMusic.MainMenu);
            Build3DPreviewStage();

            var canvas = GameUIKit.CreateCanvas("ClassSelectCanvas");
            _classSelectCanvas = canvas;
            var root = canvas.transform;

            // Top Ornate Header Bar
            var topBar = GameUIKit.MakeStyledPanel("TopHeader", root, UIPanelStyle.Ornate,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -78f), new Vector2(-28f, -12f));

            GameUIKit.MakeIcon("HdrIcon", topBar.transform, GameUIKit.GetIcon("crown"),
                new Vector2(0f, 0.16f), new Vector2(0f, 0.84f), new Vector2(18f, 0f), new Vector2(62f, 0f));

            GameUIKit.MakeText("Header", topBar.transform,
                "ĐỀN THỜ CỔ NGỮ — CHỌN LỚP NHÂN VẬT (10 AETHER HUNTER CLASSES)",
                24, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0.76f, 1f), new Vector2(72f, 0f), Vector2.zero);

            _btnBackLobby = GameUIKit.MakeStyledButton("BackLobby", topBar.transform, "← QUAY LẠI", 16, UIButtonStyle.Secondary,
                new Vector2(0.84f, 0.16f), new Vector2(0.985f, 0.84f), Vector2.zero, Vector2.zero,
                () => GameSession.GoToScene(GameSession.I.ReturnSceneAfterClassSelect));

            // Left: 2x5 Grid of 10 Classes inside UI_Panel_Character
            var leftGrid = GameUIKit.MakeStyledPanel("ClassGrid", root, UIPanelStyle.Character,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(28f, 28f), new Vector2(530f, -90f));

            GameUIKit.MakeText("GridTitle", leftGrid.transform, "DANH SÁCH 10 HỆ PHÁI THỢ SĂN:", 15, FontStyle.Bold, GameUIKit.ColGold, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.935f), new Vector2(0.96f, 0.985f), Vector2.zero, Vector2.zero);

            for (int i = 0; i < 10; i++)
            {
                int cid = i;
                ClassDef c = GameDB.Classes[i];
                int col = i % 2;
                int row = i / 2;
                float xMin = col * 0.48f + 0.03f;
                float xMax = xMin + 0.46f;
                float yMax = 0.92f - row * 0.18f;
                float yMin = yMax - 0.165f;

                var btn = GameUIKit.MakeStyledButton("Class_" + c.name, leftGrid.transform, "", 15, UIButtonStyle.Secondary,
                    new Vector2(xMin, yMin), new Vector2(xMax, yMax), Vector2.zero, Vector2.zero, () =>
                    {
                        _previewClass = cid;
                        _previewBuild = 0;
                        RefreshSelection();
                    });
                _classButtons[i] = btn;

                Sprite ic = GameUIKit.GetClassIcon(i);
                if (ic != null)
                {
                    GameUIKit.MakeIcon("Ic", btn.transform, ic,
                        new Vector2(0.05f, 0.12f), new Vector2(0.36f, 0.88f), Vector2.zero, Vector2.zero);
                }

                GameUIKit.MakeText("Name", btn.transform,
                    $"<color=#F8DC85>{c.name}</color>\n<size=11><color=#EDE0BA>{c.roleText}</color></size>",
                    15, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                    new Vector2(0.39f, 0.08f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);
            }

            // Center-Bottom Animation Preview Dock under PreviewSpawnPoint
            var animBar = GameUIKit.MakeStyledPanel("AnimButtons", root, UIPanelStyle.Standard,
                new Vector2(0.458f, 0f), new Vector2(0.458f, 0f), new Vector2(-195f, 28f), new Vector2(195f, 92f));

            _btnTryAtk = GameUIKit.MakeStyledButton("TryAtk", animBar.transform, "Đòn Đánh", 14, UIButtonStyle.Secondary,
                new Vector2(0.02f, 0.14f), new Vector2(0.33f, 0.86f), Vector2.zero, Vector2.zero,
                () => TriggerPreviewAnim("Attack"), GameUIKit.GetIcon("sword"));

            _btnTryCast = GameUIKit.MakeStyledButton("TryCast", animBar.transform, "Kỹ Năng", 14, UIButtonStyle.Secondary,
                new Vector2(0.345f, 0.14f), new Vector2(0.655f, 0.86f), Vector2.zero, Vector2.zero,
                () => TriggerPreviewAnim("Cast"), GameUIKit.GetIcon("exp"));

            _btnTryDodge = GameUIKit.MakeStyledButton("TryDodge", animBar.transform, "Né Đòn", 14, UIButtonStyle.Secondary,
                new Vector2(0.67f, 0.14f), new Vector2(0.98f, 0.86f), Vector2.zero, Vector2.zero,
                () => TriggerPreviewAnim("Dodge"), GameUIKit.GetIcon("dodge"));

            // Right: Detailed Attributes, Active/Ultimate Skills Q/W/E/R, Build Specialization & Confirm
            var right = GameUIKit.MakeStyledPanel("RightDetails", root, UIPanelStyle.Ornate,
                new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-690f, 28f), new Vector2(-28f, -90f));
            _detailContainer = GameUIKit.MakeRect("DetailContent", right.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            RefreshSelection();
            GameUIKit.OptimizeCanvasHierarchy(canvas);
            _isPrebakedInScene = true;
            _runtimeInitialized = Application.isPlaying;
        }

        private void Build3DPreviewStage()
        {
            AbyssalPostProcessing.EnsureGlobalVolumeAndCameraSetup(transform);
            var atmo = BiomeAtmosphereController.EnsureOnScene(transform);
            atmo.ApplyBiome(DungeonBiome.ForgottenTemple, instant: true);

            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = new Color(0.06f, 0.042f, 0.03f);
                Camera.main.transform.position = new Vector3(0.32f, 1.52f, -4.15f);
                Camera.main.transform.rotation = Quaternion.Euler(7f, 0f, 0f);
            }

            _previewStage = new GameObject("ClassPreviewStage").transform;
            _previewStage.SetParent(transform, false);

            // Place South wall well behind Camera so no wall or colonnade arch intersects the camera
            ModularDungeonArchitect.BuildModularRoomShell(
                _previewStage, new Vector3(0f, 0f, 0.5f), width: 16f, length: 20f, wallHeight: 5.5f,
                biome: DungeonBiome.ForgottenTemple, openSouthDoor: false, openNorthDoor: false);

            // Create explicit PreviewSpawnPoint centered in the open viewport gap between ClassGrid and RightDetails
            _previewSpawnPoint = new GameObject("PreviewSpawnPoint").transform;
            _previewSpawnPoint.SetParent(_previewStage, false);
            Vector3 spawnPos = Vector3.zero;
            if (Camera.main != null)
            {
                Plane floor = new Plane(Vector3.up, Vector3.zero);
                Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.458f, 0.25f, 0f));
                if (floor.Raycast(ray, out float enter))
                {
                    spawnPos = ray.GetPoint(enter);
                    spawnPos.y = 0f;
                }
            }
            _previewSpawnPoint.position = spawnPos;

            var l = new GameObject("KeyLight");
            l.transform.SetParent(_previewStage, false);
            l.transform.position = spawnPos + new Vector3(0.8f, 3.1f, -2.6f);
            var pl = l.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = new Color(1.0f, 0.90f, 0.72f);
            pl.range = 16f;
            pl.intensity = 2.8f;

            var rim = new GameObject("RimLight");
            rim.transform.SetParent(_previewStage, false);
            rim.transform.position = spawnPos + new Vector3(-1.4f, 2.6f, 1.6f);
            var rpl = rim.AddComponent<Light>();
            rpl.type = LightType.Point;
            rpl.color = new Color(0.95f, 0.72f, 0.35f);
            rpl.range = 12f;
            rpl.intensity = 1.75f;
        }

        private void TriggerPreviewAnim(string trigger)
        {
            if (_previewAnim != null) _previewAnim.SetTrigger(trigger);
            GameAudio.I.PlaySfx(GameSfx.Attack, 0.6f);
        }

        private void Update()
        {
            if (_previewModel != null)
            {
                if (Input.GetMouseButton(0))
                    _previewModel.transform.Rotate(0f, -Input.GetAxis("Mouse X") * 6f, 0f);
                else
                    _previewModel.transform.Rotate(0f, 20f * Time.deltaTime, 0f);
            }
        }

        private void RefreshSelection()
        {
            ClassDef cls = GameDB.GetClass(_previewClass);

            for (int i = 0; i < 10; i++)
            {
                if (_classButtons[i] != null)
                    GameUIKit.SetButtonStyle(_classButtons[i], (i == _previewClass) ? UIButtonStyle.Selected : UIButtonStyle.Secondary);
            }

            if (_previewModel != null) BakedAssetRegistry.DestroySafe(_previewModel);
            if (_previewGlowRing != null) BakedAssetRegistry.DestroySafe(_previewGlowRing);

            Transform parentAnchor = _previewSpawnPoint != null ? _previewSpawnPoint : _previewStage;
            _previewModel = CharacterVisualFactory.BuildHeroVisual(cls, parentAnchor, out _previewAnim, _previewBuild);
            _previewModel.transform.localPosition = Vector3.zero;
            _previewModel.transform.localScale = Vector3.one;
            _previewModel.transform.rotation = Quaternion.Euler(0f, 185f, 0f);

            Color buildCol = ClassSpecializationSystem.GetBuildAccentColor(_previewClass, _previewBuild, cls.color);
            _previewGlowRing = CharacterVisualFactory.SpawnGroundGlowRing(parentAnchor, buildCol, 0.85f, "PreviewGlowRing");
            string archLabel = ClassSpecializationSystem.GetArchetypeShortTag(ClassSpecializationSystem.GetClassArchetype(_previewClass));

            for (int i = _detailContainer.childCount - 1; i >= 0; i--)
                BakedAssetRegistry.DestroySafe(_detailContainer.GetChild(i).gameObject);

            // Portrait + Class Title Header
            Sprite clsPortrait = GameUIKit.GetClassIcon(_previewClass);
            if (clsPortrait != null)
            {
                GameUIKit.MakeIcon("ClsPortrait", _detailContainer, clsPortrait,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -104f), new Vector2(104f, -20f));
            }

            GameUIKit.MakeText("Title", _detailContainer,
                $"{cls.name.ToUpper()} — {cls.roleText}",
                25, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(118f, -58f), new Vector2(-24f, -16f));

            GameUIKit.MakeText("Desc", _detailContainer,
                $"{cls.desc}\n<color=#D9B057>• Nội tại: {cls.passive}   |   Hệ Giáp: {archLabel} (+15% Chỉ Số)</color>",
                13, FontStyle.Normal, GameUIKit.ColParchment, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(118f, -112f), new Vector2(-24f, -56f));

            // Base Attributes Bar
            string stats = $"HP: <color=#F8DC85>{cls.hp:0}</color>   |   MP: <color=#7EC8FF>{cls.mana:0}</color>   |   ATK: <color=#F8DC85>{cls.atk:0}</color>   |   DEF: <color=#F8DC85>{cls.def:0}</color>   |   Crit: <color=#F8DC85>{cls.crit * 100:0}%</color>   |   Tốc chạy: <color=#F8DC85>{cls.moveSpeed:0.1f}</color>";
            var statBox = GameUIKit.MakeStyledPanel("StatBox", _detailContainer, UIPanelStyle.Standard,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -160f), new Vector2(-24f, -118f));
            GameUIKit.MakeText("StatTxt", statBox.transform, stats, 14, FontStyle.Bold, GameUIKit.ColParchment, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // 4 Active & Ultimate Skills (Q, W, E, R) with Engraved Skill Medallions
            string[] keys = { "Q / 1", "W / 2", "E / 3", "R / 4 • ULTIMATE" };
            for (int s = 0; s < 4; s++)
            {
                SkillDef sk = cls.skills[s];
                float top = -170f - s * 76f;
                var skCard = GameUIKit.MakeStyledPanel("Sk" + s, _detailContainer,
                    s == 3 ? UIPanelStyle.Ornate : UIPanelStyle.SkillTree,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, top - 68f), new Vector2(-24f, top));

                GameUIKit.MakeIcon("SkIc", skCard.transform, GameUIKit.GetSkillIcon(sk),
                    new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(10f, 8f), new Vector2(62f, -8f));

                GameUIKit.MakeText("SkTitle", skCard.transform,
                    $"[{keys[s]}] {sk.name}   <size=12><color=#BDA685>(CD: {sk.cooldown:0.#}s | Mana: {sk.mana:0})</color></size>",
                    15, FontStyle.Bold, sk.color, TextAnchor.UpperLeft,
                    Vector2.zero, Vector2.one, new Vector2(72f, 6f), new Vector2(-14f, -8f));

                GameUIKit.MakeText("SkDesc", skCard.transform, sk.desc, 13, FontStyle.Normal, GameUIKit.ColParchment, TextAnchor.LowerLeft,
                    Vector2.zero, Vector2.one, new Vector2(72f, 8f), new Vector2(-14f, -32f));
            }

            // 3 Build Specializations
            GameUIKit.MakeText("BuildHeader", _detailContainer,
                "CHUYÊN MÔN HÓA HƯỚNG BUILD (3 HƯỚNG BUILD / CLASS):",
                15, FontStyle.Bold, GameUIKit.ColGoldBright, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 162f), new Vector2(-24f, 190f));

            for (int b = 0; b < 3; b++)
            {
                int bIdx = b;
                bool active = (_previewBuild == b);
                GameUIKit.MakeStyledButton("BuildBtn" + b, _detailContainer, cls.builds[b], 14,
                    active ? UIButtonStyle.Selected : UIButtonStyle.Tab,
                    new Vector2(0.04f + b * 0.315f, 0f), new Vector2(0.335f + b * 0.315f, 0f),
                    new Vector2(0f, 114f), new Vector2(0f, 158f), () =>
                    {
                        _previewBuild = bIdx;
                        RefreshSelection();
                    });
            }

            GameUIKit.MakeText("BuildNote", _detailContainer,
                "• " + cls.buildDesc[_previewBuild],
                13, FontStyle.Italic, buildCol, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(26f, 76f), new Vector2(-26f, 112f));

            GameUIKit.MakeStyledButton("Confirm", _detailContainer,
                $"XÁC NHẬN CHỌN {cls.name.ToUpper()} ({cls.builds[_previewBuild]})", 18, UIButtonStyle.Primary,
                new Vector2(0.04f, 0f), new Vector2(0.96f, 0f), new Vector2(0f, 16f), new Vector2(0f, 72f), () =>
                {
                    GameSession.I.SelectLocalClass(_previewClass, _previewBuild);
                    if (LanRoomBridge.I.IsConnectedClient)
                        LanRoomBridge.I.NotifyClientSlotUpdate(true);
                    else
                        LanRoomBridge.I.BroadcastFromHost();
                    GameSession.GoToScene(GameSession.I.ReturnSceneAfterClassSelect);
                },
                GameUIKit.GetIcon("check_seal"));
        }
    }
}

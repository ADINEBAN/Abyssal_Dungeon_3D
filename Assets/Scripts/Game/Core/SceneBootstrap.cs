using UnityEngine;
using UnityEngine.SceneManagement;

namespace AbyssGame
{
    public enum AbyssSceneKind
    {
        AutoDetect,
        MainMenu,
        Lobby,
        ClassSelection,
        Dungeon,
        EndGame,
        Town,
        VisualShowcase
    }

    /// <summary>
    /// Bootstraps any of the 6 game scenes (MainMenu, Lobby, ClassSelection, Dungeon, EndGame, Town)
    /// and guarantees Camera, Directional Light, EventSystem, GameSession, and GameAudio exist.
    /// </summary>
    public class SceneBootstrap : MonoBehaviour
    {
        public AbyssSceneKind SceneKind = AbyssSceneKind.AutoDetect;
        private static string _pendingVirtualScene;
        private GameObject _activeRoot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrapOnAnyScene()
        {
            if (FindObjectOfType<SceneBootstrap>() == null)
            {
                var go = new GameObject("[AbyssSceneBootstrap]");
                go.AddComponent<SceneBootstrap>();
            }
        }

        public static void LoadVirtualScene(string sceneName)
        {
            _pendingVirtualScene = sceneName;
            var existing = FindObjectOfType<SceneBootstrap>();
            if (existing != null)
            {
                existing.BootForName(sceneName);
            }
        }

        private void Start()
        {
            string target = !string.IsNullOrEmpty(_pendingVirtualScene)
                ? _pendingVirtualScene
                : ResolveSceneName();
            _pendingVirtualScene = null;
            BootForName(target);
        }

        private string ResolveSceneName()
        {
            if (SceneKind != AbyssSceneKind.AutoDetect)
                return SceneKind.ToString();

            string activeName = SceneManager.GetActiveScene().name;
            if (activeName == "Lobby" || activeName == "ClassSelection" || activeName == "Dungeon" ||
                activeName == "EndGame" || activeName == "Town" || activeName == "MainMenu" ||
                activeName == "VisualShowcase")
                return activeName;

            return "MainMenu";
        }

        public void BootForName(string sceneName)
        {
            Time.timeScale = 1f;

            // Clean previous virtual root if switching inside a fallback single scene
            if (_activeRoot != null) Destroy(_activeRoot);

            // Clean any lingering Canvas / stray units when virtual-switching
            foreach (var c in FindObjectsOfType<Canvas>())
                if (c.transform.parent == null) Destroy(c.gameObject);
            for (int i = ActorUnit.AllUnits.Count - 1; i >= 0; i--)
                if (ActorUnit.AllUnits[i] != null) Destroy(ActorUnit.AllUnits[i].gameObject);

            EnsureCameraAndLight();
            _ = GameSession.I;
            _ = GameAudio.I;
            _ = GraphicsManager.Instance;

            // Remove OrbitCamera3D on menu scenes
            var orbit = Camera.main != null ? Camera.main.GetComponent<OrbitCamera3D>() : null;
            if (orbit != null && sceneName != "Dungeon" && sceneName != "Town" && sceneName != "VisualShowcase")
            {
                orbit.enabled = false;
                Destroy(orbit);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            _activeRoot = new GameObject("SceneContent_" + sceneName);
            AbyssalPostProcessing.EnsureGlobalVolumeAndCameraSetup(_activeRoot.transform);

            if (sceneName != "Dungeon" && sceneName != "VisualShowcase" && sceneName != "Town")
                GameAudio.I.PlayAmbience("amb_forgotten_temple");

            switch (sceneName)
            {
                case "Lobby":
                    _activeRoot.AddComponent<LobbySceneView>().Build();
                    break;
                case "ClassSelection":
                    _activeRoot.AddComponent<ClassSelectSceneView>().Build();
                    break;
                case "Dungeon":
                    var dw = _activeRoot.AddComponent<DungeonWorldBuilder>();
                    dw.BuildAndStartDungeon();
                    _activeRoot.AddComponent<DungeonHUDView>().Build();
                    break;
                case "EndGame":
                    _activeRoot.AddComponent<EndGameSceneView>().Build();
                    break;
                case "Town":
                    _activeRoot.AddComponent<TownSceneBuilder>().Build();
                    break;
                case "VisualShowcase":
                    _activeRoot.AddComponent<VisualShowcaseSceneView>().Build();
                    break;
                default:
                    _activeRoot.AddComponent<MainMenuSceneView>().Build();
                    break;
            }

            if (GraphicsManager.Instance != null)
                GraphicsManager.Instance.ApplyActivePresetToCurrentScene();
        }

        private static void EnsureCameraAndLight()
        {
            if (Camera.main == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.nearClipPlane = 0.15f;
                cam.farClipPlane = 160f;
                if (FindObjectOfType<AudioListener>() == null)
                    camGo.AddComponent<AudioListener>();
            }

            if (FindObjectOfType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                var dl = lightGo.AddComponent<Light>();
                dl.type = LightType.Directional;
                dl.color = new Color(0.92f, 0.88f, 1f);
                dl.intensity = 1.05f;
                lightGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            }
        }
    }
}


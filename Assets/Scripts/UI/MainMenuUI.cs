using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

namespace AbyssalDungeon.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private CanvasGroup mainPanel;
        [SerializeField] private CanvasGroup instructionsPanel;
        [SerializeField] private CanvasGroup settingsPanel;

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button instructionsButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Back Buttons")]
        [SerializeField] private Button closeInstructionsButton;
        [SerializeField] private Button closeSettingsButton;

        [Header("References")]
        [SerializeField] private ParticleSystem backgroundParticles;

        private void Start()
        {
            playButton.onClick.AddListener(OnPlayClicked);
            instructionsButton.onClick.AddListener(OnInstructionsClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            quitButton.onClick.AddListener(OnQuitClicked);
            
            closeInstructionsButton.onClick.AddListener(CloseAllPanels);
            closeSettingsButton.onClick.AddListener(CloseAllPanels);

            ShowPanel(mainPanel);
        }

        private void OnPlayClicked()
        {
            SceneManager.LoadScene("Lobby");
        }

        private void OnInstructionsClicked()
        {
            ShowPanel(instructionsPanel);
        }

        private void OnSettingsClicked()
        {
            ShowPanel(settingsPanel);
        }

        private void OnQuitClicked()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void CloseAllPanels()
        {
            StartCoroutine(FadeOut(instructionsPanel));
            StartCoroutine(FadeOut(settingsPanel));
            StartCoroutine(FadeIn(mainPanel));
        }

        private void ShowPanel(CanvasGroup panel)
        {
            StartCoroutine(FadeOut(mainPanel));
            StartCoroutine(FadeOut(instructionsPanel));
            StartCoroutine(FadeOut(settingsPanel));
            StartCoroutine(FadeIn(panel));
        }

        private IEnumerator FadeIn(CanvasGroup cg)
        {
            if (cg == null) yield break;
            cg.gameObject.SetActive(true);
            cg.blocksRaycasts = true;
            float t = 0;
            while(t < 0.3f)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(0, 1, t / 0.3f);
                yield return null;
            }
            cg.alpha = 1;
        }

        private IEnumerator FadeOut(CanvasGroup cg)
        {
            if (cg == null || !cg.gameObject.activeSelf) yield break;
            cg.blocksRaycasts = false;
            float t = 0;
            while(t < 0.3f)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1, 0, t / 0.3f);
                yield return null;
            }
            cg.alpha = 0;
            cg.gameObject.SetActive(false);
        }
    }
}

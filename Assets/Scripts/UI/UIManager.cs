using UnityEngine;
using System.Collections;
using TMPro;

namespace AbyssalDungeon.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Loading Screen")]
        [SerializeField] private CanvasGroup loadingScreen;
        
        [Header("Notifications")]
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private CanvasGroup notificationGroup;

        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject confirmationDialog;
        [SerializeField] private TextMeshProUGUI dialogMessage;
        [SerializeField] private UnityEngine.UI.Button confirmButton;
        [SerializeField] private UnityEngine.UI.Button cancelButton;

        private System.Action onConfirmCallback;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (notificationPanel != null) notificationPanel.SetActive(false);
            if (confirmationDialog != null) confirmationDialog.SetActive(false);
            
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);
        }

        public void ShowPanel(GameObject panel)
        {
            if(panel != null) panel.SetActive(true);
        }

        public void HidePanel(GameObject panel)
        {
            if(panel != null) panel.SetActive(false);
        }

        public void ShowLoadingScreen(bool show)
        {
            if (loadingScreen != null)
            {
                loadingScreen.gameObject.SetActive(show);
                loadingScreen.alpha = show ? 1f : 0f;
            }
        }

        public void ShowNotification(string message, float duration = 2f)
        {
            StartCoroutine(NotificationRoutine(message, duration));
        }

        private IEnumerator NotificationRoutine(string message, float duration)
        {
            notificationText.text = message;
            notificationPanel.SetActive(true);
            notificationGroup.alpha = 0;
            
            // Fade in
            float t = 0;
            while(t < 0.2f)
            {
                t += Time.deltaTime;
                notificationGroup.alpha = t / 0.2f;
                yield return null;
            }

            yield return new WaitForSeconds(duration);

            // Fade out
            t = 0;
            while(t < 0.2f)
            {
                t += Time.deltaTime;
                notificationGroup.alpha = 1 - (t / 0.2f);
                yield return null;
            }
            
            notificationPanel.SetActive(false);
        }

        public void ShowConfirmationDialog(string message, System.Action onConfirm)
        {
            dialogMessage.text = message;
            onConfirmCallback = onConfirm;
            confirmationDialog.SetActive(true);
        }

        private void OnConfirmClicked()
        {
            confirmationDialog.SetActive(false);
            onConfirmCallback?.Invoke();
        }

        private void OnCancelClicked()
        {
            confirmationDialog.SetActive(false);
        }
        
        public Coroutine FadeIn(CanvasGroup cg, float duration)
        {
            return StartCoroutine(UIAnimator.FadeInCoroutine(cg, duration));
        }
        
        public Coroutine FadeOut(CanvasGroup cg, float duration)
        {
            return StartCoroutine(UIAnimator.FadeOutCoroutine(cg, duration));
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

namespace AbyssalDungeon.Core
{
    public class SceneController : Singleton<SceneController>
    {
        public event Action<string> OnSceneLoadStarted;
        public event Action<string> OnSceneLoadCompleted;

        [Header("Loading Settings")]
        [SerializeField] private GameObject _loadingScreenPrefab;
        [SerializeField] private float _minLoadingTime = 1f;

        private GameObject _loadingScreenInstance;

        public void LoadSceneAsync(string sceneName)
        {
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            OnSceneLoadStarted?.Invoke(sceneName);
            
            if (_loadingScreenPrefab != null && _loadingScreenInstance == null)
            {
                _loadingScreenInstance = Instantiate(_loadingScreenPrefab);
                DontDestroyOnLoad(_loadingScreenInstance);
            }

            if (_loadingScreenInstance != null)
                _loadingScreenInstance.SetActive(true);

            float startTime = Time.time;
            
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;

            while (!operation.isDone)
            {
                if (operation.progress >= 0.9f)
                {
                    if (Time.time - startTime >= _minLoadingTime)
                    {
                        operation.allowSceneActivation = true;
                    }
                }
                yield return null;
            }

            if (_loadingScreenInstance != null)
                _loadingScreenInstance.SetActive(false);

            OnSceneLoadCompleted?.Invoke(sceneName);
            Debug.Log($"[SceneController] Scene {sceneName} loaded.");
        }
    }
}

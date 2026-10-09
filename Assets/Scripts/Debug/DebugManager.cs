using UnityEngine;
using AbyssalDungeon.Core;
using TMPro;

namespace AbyssalDungeon.DebugUtils
{
    public class DebugManager : Singleton<DebugManager>
    {
        [Header("Debug Settings")]
        [SerializeField] private bool _debugMode = false;
        [SerializeField] private TextMeshProUGUI _debugText;
        [SerializeField] private GameObject _debugUIContainer;

        [Header("Prefabs")]
        [SerializeField] private GameObject _enemyPrefab;
        [SerializeField] private GameObject _bossPrefab;

        public bool ShowAIState { get; private set; }
        public bool ShowEnemyDetectionRange { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _debugMode = true;
#endif

            if (_debugUIContainer != null)
                _debugUIContainer.SetActive(_debugMode);
        }

        private void Update()
        {
            if (!_debugMode) return;

            HandleInput();
            UpdateDebugText();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ShowAIState = !ShowAIState;
                Debug.Log($"[Debug] Show AI State: {ShowAIState}");
            }
            if (Input.GetKeyDown(KeyCode.F2))
            {
                ShowEnemyDetectionRange = !ShowEnemyDetectionRange;
                Debug.Log($"[Debug] Show Enemy Detection Range: {ShowEnemyDetectionRange}");
            }
            if (Input.GetKeyDown(KeyCode.F3))
            {
                SpawnEnemyAtMouse();
            }
            if (Input.GetKeyDown(KeyCode.F4))
            {
                KillAllEnemies();
            }
            if (Input.GetKeyDown(KeyCode.F5))
            {
                GiveExp(1000);
            }
            if (Input.GetKeyDown(KeyCode.F6))
            {
                GiveGold(500);
            }
            if (Input.GetKeyDown(KeyCode.F7))
            {
                SpawnBoss();
            }
            if (Input.GetKeyDown(KeyCode.F8))
            {
                CompleteCurrentRoom();
            }
        }

        private void SpawnEnemyAtMouse()
        {
            if (_enemyPrefab == null) return;
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Instantiate(_enemyPrefab, hit.point, Quaternion.identity);
                Debug.Log("[Debug] Spawned Enemy at mouse position.");
            }
        }

        private void KillAllEnemies()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(Constants.Tags.Enemy);
            foreach (var enemy in enemies)
            {
                Destroy(enemy);
            }
            Debug.Log($"[Debug] Killed {enemies.Length} enemies.");
        }

        private void GiveExp(int amount)
        {
            EventManager.TriggerExpChanged(amount);
            Debug.Log($"[Debug] Gave {amount} EXP.");
        }

        private void GiveGold(int amount)
        {
            EventManager.TriggerGoldChanged(amount);
            Debug.Log($"[Debug] Gave {amount} Gold.");
        }

        private void SpawnBoss()
        {
            if (_bossPrefab == null) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Instantiate(_bossPrefab, hit.point, Quaternion.identity);
                Debug.Log("[Debug] Spawned Boss.");
            }
        }

        private void CompleteCurrentRoom()
        {
            EventManager.TriggerRoomCleared();
            Debug.Log("[Debug] Current room cleared.");
        }

        private void UpdateDebugText()
        {
            if (_debugText != null)
            {
                _debugText.text = $"DEBUG MODE\nFPS: {Mathf.RoundToInt(1f / Time.unscaledDeltaTime)}\nAI State: {ShowAIState}\nDet. Range: {ShowEnemyDetectionRange}";
            }
        }
    }
}

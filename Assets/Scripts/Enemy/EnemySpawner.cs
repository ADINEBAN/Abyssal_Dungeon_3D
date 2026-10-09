using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssalDungeon.Enemy
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField] private GameObject[] _enemyPrefabs; // Should be EnemyData containing prefabs ideally
        [SerializeField] private int _maxEnemiesPerWave = 5;
        [SerializeField] private int _waveCount = 3;
        [SerializeField] private float _spawnDelay = 1f;

        private int _currentWave = 0;
        private int _aliveEnemiesCount = 0;
        private List<EnemyController> _activeEnemies = new List<EnemyController>();

        public event Action OnAllEnemiesDefeated;
        public event Action<int, int> OnWaveCompleted;

        private void Start()
        {
            StartCoroutine(SpawnWaveRoutine());
        }

        private IEnumerator SpawnWaveRoutine()
        {
            while (_currentWave < _waveCount)
            {
                yield return new WaitForSeconds(2f); // Time between waves
                _currentWave++;
                
                int enemiesToSpawn = Mathf.Min(_maxEnemiesPerWave, _spawnPoints.Length);
                SpawnEnemies(enemiesToSpawn);

                // Wait until all enemies are defeated before next wave
                yield return new WaitUntil(() => _aliveEnemiesCount <= 0);
                
                OnWaveCompleted?.Invoke(_currentWave, _waveCount);
            }

            OnAllEnemiesDefeated?.Invoke();
        }

        private void SpawnEnemies(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (i >= _spawnPoints.Length) break;
                if (_enemyPrefabs.Length == 0) break;

                GameObject randomEnemyPrefab = _enemyPrefabs[UnityEngine.Random.Range(0, _enemyPrefabs.Length)];
                Transform spawnPoint = _spawnPoints[i];

                GameObject enemyObj = Instantiate(randomEnemyPrefab, spawnPoint.position, spawnPoint.rotation);
                EnemyController enemyController = enemyObj.GetComponent<EnemyController>();
                
                if (enemyController != null)
                {
                    enemyController.OnEnemyDeath += HandleEnemyDeath;
                    _activeEnemies.Add(enemyController);
                    _aliveEnemiesCount++;
                }
            }
        }

        private void HandleEnemyDeath(EnemyController enemy)
        {
            enemy.OnEnemyDeath -= HandleEnemyDeath;
            _activeEnemies.Remove(enemy);
            _aliveEnemiesCount--;
        }
    }
}

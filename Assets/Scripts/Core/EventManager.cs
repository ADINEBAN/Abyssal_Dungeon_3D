using System;
using UnityEngine;

namespace AbyssalDungeon.Core
{
    public static class EventManager
    {
        public static event Action OnPlayerDeath;
        public static void TriggerPlayerDeath() => OnPlayerDeath?.Invoke();

        public static event Action<GameObject> OnEnemyDeath;
        public static void TriggerEnemyDeath(GameObject enemy) => OnEnemyDeath?.Invoke(enemy);

        public static event Action OnBossDefeated;
        public static void TriggerBossDefeated() => OnBossDefeated?.Invoke();

        public static event Action<int> OnLevelUp;
        public static void TriggerLevelUp(int newLevel) => OnLevelUp?.Invoke(newLevel);

        public static event Action<string, int> OnItemPickup;
        public static void TriggerItemPickup(string itemId, int amount) => OnItemPickup?.Invoke(itemId, amount);

        public static event Action OnDungeonComplete;
        public static void TriggerDungeonComplete() => OnDungeonComplete?.Invoke();

        public static event Action OnRoomCleared;
        public static void TriggerRoomCleared() => OnRoomCleared?.Invoke();

        public static event Action<int> OnGoldChanged;
        public static void TriggerGoldChanged(int totalGold) => OnGoldChanged?.Invoke(totalGold);

        public static event Action<int> OnExpChanged;
        public static void TriggerExpChanged(int totalExp) => OnExpChanged?.Invoke(totalExp);
    }
}

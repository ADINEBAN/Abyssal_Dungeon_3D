using UnityEngine;
using UnityEngine.Events;

namespace AbyssalDungeon.Dungeon
{
    public enum RoomType { Entry, Combat, Elite, Event, Puzzle, MiniBoss, Boss, Treasure }
    public enum RoomState { Locked, Active, Cleared }

    // Mock EnemySpawner class to ensure compilation.
    public class EnemySpawner : MonoBehaviour
    {
        public UnityEvent OnAllEnemiesDefeated;
        public void SpawnEnemies() { Debug.Log("Spawning enemies..."); }
    }

    public class RoomManager : MonoBehaviour
    {
        [Header("Room Details")]
        [SerializeField] private RoomType roomType;
        [SerializeField] private RoomState roomState = RoomState.Locked;
        
        [Header("References")]
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private DoorController[] doors;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private Collider triggerVolume;

        [Header("Events")]
        public UnityEvent OnRoomActivated;
        public UnityEvent OnRoomCleared;

        public bool isCleared => roomState == RoomState.Cleared;
        public RoomType Type => roomType;

        private void Start()
        {
            if (enemySpawner != null)
            {
                enemySpawner.OnAllEnemiesDefeated.AddListener(OnRoomClearedHandler);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && roomState == RoomState.Locked)
            {
                ActivateRoom();
            }
        }

        public void ActivateRoom()
        {
            if (roomState != RoomState.Locked) return;

            roomState = RoomState.Active;
            
            // Lock all doors
            foreach (var door in doors)
            {
                if (door != null) door.Lock();
            }

            if (triggerVolume != null) triggerVolume.enabled = false;

            if (enemySpawner != null)
            {
                enemySpawner.SpawnEnemies();
            }
            else if (roomType == RoomType.Entry || roomType == RoomType.Treasure)
            {
                // Auto-clear non-combat rooms immediately for this example
                OnRoomClearedHandler();
            }

            OnRoomActivated?.Invoke();
            Debug.Log($"Room {gameObject.name} Activated!");
        }

        private void OnRoomClearedHandler()
        {
            if (roomState == RoomState.Cleared) return;

            roomState = RoomState.Cleared;
            
            // Unlock doors
            foreach (var door in doors)
            {
                if (door != null) door.Unlock();
            }

            SpawnLoot();

            OnRoomCleared?.Invoke();
            Debug.Log($"Room {gameObject.name} Cleared!");

            if (DungeonManager.Instance != null && DungeonManager.Instance.GetCurrentRoom() == this)
            {
                DungeonManager.Instance.AdvanceToNextRoom();
            }
        }

        private void SpawnLoot()
        {
            Debug.Log("Spawning room completion loot...");
            // Implementation for spawning loot based on RoomType
        }
    }
}

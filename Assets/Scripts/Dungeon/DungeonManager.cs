using System;
using UnityEngine;
using UnityEngine.Events;

namespace AbyssalDungeon.Dungeon
{
    [Serializable]
    public class RoomData
    {
        public string roomName;
        public RoomManager roomManager;
        public bool isCompleted;
    }

    public class DungeonManager : MonoBehaviour
    {
        public static DungeonManager Instance { get; private set; }

        [Header("Dungeon Configuration")]
        [SerializeField] private RoomData[] rooms;
        [SerializeField] private int currentRoomIndex = 0;

        [Header("Dungeon Statistics")]
        public float totalTimeElapsed;
        public int totalEnemiesDefeated;
        public int totalDamageTaken;
        
        [Header("Events")]
        public UnityEvent<RoomManager> OnRoomChanged;
        public UnityEvent OnDungeonStarted;
        public UnityEvent OnDungeonComplete;

        private bool isDungeonActive = false;

        public bool IsDungeonComplete => currentRoomIndex >= rooms.Length;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            StartDungeon();
        }

        private void Update()
        {
            if (isDungeonActive && !IsDungeonComplete)
            {
                totalTimeElapsed += Time.deltaTime;
            }
        }

        public void StartDungeon()
        {
            isDungeonActive = true;
            currentRoomIndex = 0;
            totalTimeElapsed = 0f;
            
            OnDungeonStarted?.Invoke();
            
            if (rooms.Length > 0 && rooms[0].roomManager != null)
            {
                rooms[0].roomManager.ActivateRoom();
                OnRoomChanged?.Invoke(rooms[0].roomManager);
            }
        }

        public void AdvanceToNextRoom()
        {
            if (currentRoomIndex < rooms.Length)
            {
                rooms[currentRoomIndex].isCompleted = true;
                currentRoomIndex++;

                if (currentRoomIndex < rooms.Length)
                {
                    var nextRoom = rooms[currentRoomIndex].roomManager;
                    if (nextRoom != null)
                    {
                        nextRoom.ActivateRoom();
                        OnRoomChanged?.Invoke(nextRoom);
                    }
                }
                else
                {
                    CompleteDungeon();
                }
            }
        }

        public RoomManager GetCurrentRoom()
        {
            if (currentRoomIndex >= 0 && currentRoomIndex < rooms.Length)
                return rooms[currentRoomIndex].roomManager;
            return null;
        }

        private void CompleteDungeon()
        {
            isDungeonActive = false;
            OnDungeonComplete?.Invoke();
            Debug.Log($"Dungeon Completed in {totalTimeElapsed:F2} seconds!");
        }
    }
}

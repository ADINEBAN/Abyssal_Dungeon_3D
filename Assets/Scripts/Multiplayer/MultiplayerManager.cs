using System;
using UnityEngine;
using System.Collections.Generic;

namespace AbyssalDungeon.Multiplayer
{
    [Serializable]
    public class PartySlot
    {
        public string playerName;
        public int classIndex;
        public int level;
        public bool isReady;
        public bool isLocal;
        public bool isOccupied;
    }

    public class MultiplayerManager : MonoBehaviour
    {
        public static MultiplayerManager Instance { get; private set; }

        public PartySlot[] slots = new PartySlot[4];

        public event Action<int> OnPlayerJoined;
        public event Action<int> OnPlayerLeft;
        public event Action<int, bool> OnPlayerReady;
        public event Action OnAllReady;

        private string[] mockNames = { "Aria", "Thorin", "Elara", "Kael" };

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSlots();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeSlots()
        {
            for (int i = 0; i < 4; i++)
            {
                slots[i] = new PartySlot();
            }
        }

        public void CreateRoom()
        {
            slots[0].isOccupied = true;
            slots[0].isLocal = true;
            slots[0].playerName = "Player";
            slots[0].classIndex = 0;
            slots[0].level = 1;
            slots[0].isReady = false;
            OnPlayerJoined?.Invoke(0);
        }

        public void JoinRoom()
        {
            int emptySlot = GetNextEmptySlot();
            if (emptySlot != -1)
            {
                AddMockPlayer(mockNames[UnityEngine.Random.Range(0, mockNames.Length)], UnityEngine.Random.Range(0, 3));
            }
        }

        public void AddMockPlayer(string name, int classIdx)
        {
            int slot = GetNextEmptySlot();
            if (slot != -1)
            {
                slots[slot].isOccupied = true;
                slots[slot].isLocal = false;
                slots[slot].playerName = name;
                slots[slot].classIndex = classIdx;
                slots[slot].level = UnityEngine.Random.Range(1, 10);
                slots[slot].isReady = true;
                OnPlayerJoined?.Invoke(slot);
                CheckAllReady();
            }
        }

        public void RemoveMockPlayer(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < 4 && slots[slotIndex].isOccupied)
            {
                slots[slotIndex] = new PartySlot();
                OnPlayerLeft?.Invoke(slotIndex);
            }
        }

        public void SetReady(int slotIndex, bool ready)
        {
            if (slotIndex >= 0 && slotIndex < 4 && slots[slotIndex].isOccupied)
            {
                slots[slotIndex].isReady = ready;
                OnPlayerReady?.Invoke(slotIndex, ready);
                CheckAllReady();
            }
        }

        public bool IsAllReady()
        {
            int count = 0;
            foreach (var slot in slots)
            {
                if (slot.isOccupied)
                {
                    if (!slot.isReady) return false;
                    count++;
                }
            }
            return count > 0;
        }

        private void CheckAllReady()
        {
            if (IsAllReady())
            {
                OnAllReady?.Invoke();
            }
        }

        public int GetOccupiedSlotCount()
        {
            int count = 0;
            foreach (var slot in slots)
            {
                if (slot.isOccupied) count++;
            }
            return count;
        }

        private int GetNextEmptySlot()
        {
            for (int i = 0; i < 4; i++)
            {
                if (!slots[i].isOccupied) return i;
            }
            return -1;
        }
    }
}

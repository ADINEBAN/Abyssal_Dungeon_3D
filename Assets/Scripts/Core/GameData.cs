using System;
using System.Collections.Generic;

namespace AbyssalDungeon.Core
{
    [Serializable]
    public class PlayerData
    {
        public int level = 1;
        public int exp = 0;
        public int gold = 0;
        public string className = "Warrior";
        public List<string> equipment = new List<string>();
    }

    [Serializable]
    public class PartyData
    {
        public List<PlayerData> members = new List<PlayerData>();
    }

    [Serializable]
    public class DungeonProgress
    {
        public int currentFloor = 1;
        public int highestFloorReached = 1;
        public int roomsCleared = 0;
    }

    [Serializable]
    public class Statistics
    {
        public int damageDealt = 0;
        public int damageTaken = 0;
        public int healing = 0;
        public int monstersKilled = 0;
        public float completionTime = 0f;
    }

    [Serializable]
    public class SaveData
    {
        public PlayerData playerData = new PlayerData();
        public PartyData partyData = new PartyData();
        public DungeonProgress dungeonProgress = new DungeonProgress();
        public Statistics stats = new Statistics();
    }
}

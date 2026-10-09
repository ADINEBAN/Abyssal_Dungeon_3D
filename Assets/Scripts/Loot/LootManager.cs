using UnityEngine;
using System;
using System.Collections.Generic;

namespace AbyssalDungeon.Loot
{
    // Dummy class to satisfy PlayerStats dependency
    // In a real project, this would be imported from the Player namespace
    public class PlayerStats : MonoBehaviour 
    {
        public void AddExp(int amount) { }
        public void AddGold(int amount) { }
        public void AddItem(ItemData item, int quantity) { }
    }

    public class LootManager : MonoBehaviour
    {
        public static LootManager Instance { get; private set; }

        public event Action<List<LootDrop>, Vector3> OnLootDropped;
        public event Action<LootDrop, PlayerStats> OnLootCollected;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void GenerateLoot(LootTable table, Vector3 position)
        {
            if (table == null)
            {
                Debug.LogWarning("LootTable is null.");
                return;
            }

            List<LootDrop> drops = table.Roll();
            
            // Apply difficulty modifier (dummy logic for example)
            drops = ApplyRarityModifier(drops, 1.0f);

            SpawnLootPopup(drops, position);
            OnLootDropped?.Invoke(drops, position);
        }

        public void SpawnLootPopup(List<LootDrop> drops, Vector3 position)
        {
            // Implementation for spawning 3D objects or UI popups
            Debug.Log($"Spawning {drops.Count} loot drops at {position}");
            foreach (var drop in drops)
            {
                if (drop.item != null)
                {
                    Debug.Log($"- Item: {drop.item.itemName} x{drop.quantity}");
                }
                if (drop.gold > 0)
                {
                    Debug.Log($"- Gold: {drop.gold}");
                }
                if (drop.exp > 0)
                {
                    Debug.Log($"- EXP: {drop.exp}");
                }
            }
        }

        public void CollectLoot(LootDrop drop, PlayerStats player)
        {
            if (player == null) return;

            if (drop.item != null)
            {
                player.AddItem(drop.item, drop.quantity);
                Debug.Log($"Collected item: {drop.item.itemName} x{drop.quantity}");
            }
            if (drop.gold > 0)
            {
                player.AddGold(drop.gold);
                Debug.Log($"Collected gold: {drop.gold}");
            }
            if (drop.exp > 0)
            {
                player.AddExp(drop.exp);
                Debug.Log($"Collected EXP: {drop.exp}");
            }

            OnLootCollected?.Invoke(drop, player);
        }

        private List<LootDrop> ApplyRarityModifier(List<LootDrop> drops, float difficultyMultiplier)
        {
            // In a real implementation, you might adjust item quantities or re-roll rarity based on difficulty
            return drops;
        }
    }
}

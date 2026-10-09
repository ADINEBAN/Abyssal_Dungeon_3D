using UnityEngine;
using System.Collections.Generic;

namespace AbyssalDungeon.Loot
{
    [System.Serializable]
    public class LootEntry
    {
        public ItemData item;
        [Range(0f, 1f)]
        public float dropChance;
        public int minQuantity = 1;
        public int maxQuantity = 1;
    }

    public struct LootDrop
    {
        public ItemData item;
        public int quantity;
        public int gold;
        public int exp;
    }

    [CreateAssetMenu(fileName = "NewLootTable", menuName = "Abyssal Dungeon/Loot Table")]
    public class LootTable : ScriptableObject
    {
        [Header("Items")]
        public LootEntry[] entries;

        [Header("Guaranteed Rewards")]
        public int minGuaranteedGold;
        public int maxGuaranteedGold;
        
        public int minGuaranteedEXP;
        public int maxGuaranteedEXP;

        public List<LootDrop> Roll()
        {
            List<LootDrop> drops = new List<LootDrop>();

            // Always add guaranteed gold and exp
            int gold = Random.Range(minGuaranteedGold, maxGuaranteedGold + 1);
            int exp = Random.Range(minGuaranteedEXP, maxGuaranteedEXP + 1);

            LootDrop baseDrop = new LootDrop
            {
                item = null,
                quantity = 0,
                gold = gold,
                exp = exp
            };
            drops.Add(baseDrop);

            // Roll for items
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    if (entry.item != null && Random.value <= entry.dropChance)
                    {
                        int quantity = Random.Range(entry.minQuantity, entry.maxQuantity + 1);
                        drops.Add(new LootDrop
                        {
                            item = entry.item,
                            quantity = quantity,
                            gold = 0,
                            exp = 0
                        });
                    }
                }
            }

            return drops;
        }
    }
}

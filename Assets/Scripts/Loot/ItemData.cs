using UnityEngine;

namespace AbyssalDungeon.Loot
{
    public enum ItemType
    {
        Weapon,
        Armor,
        Helmet,
        Ring,
        Amulet,
        Consumable
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    [CreateAssetMenu(fileName = "NewItem", menuName = "Abyssal Dungeon/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Basic Info")]
        public string itemName;
        [TextArea(3, 10)]
        public string description;
        public Sprite icon;
        public ItemType itemType;
        public ItemRarity rarity;

        [Header("Stat Bonuses")]
        public float hpBonus;
        public float manaBonus;
        public float attackBonus;
        public float defenseBonus;
        public float attackSpeedBonus;
        public float criticalChanceBonus;
        public float lifestealBonus;
        public float cooldownReduction;

        [Header("Economy")]
        public int sellPrice;

        public Color GetRarityColor()
        {
            switch (rarity)
            {
                case ItemRarity.Common: return Color.white;
                case ItemRarity.Uncommon: return Color.green;
                case ItemRarity.Rare: return Color.blue;
                case ItemRarity.Epic: return new Color(0.5f, 0f, 0.5f); // Purple
                case ItemRarity.Legendary: return new Color(1f, 0.5f, 0f); // Orange
                case ItemRarity.Mythic: return Color.red;
                default: return Color.white;
            }
        }
    }
}

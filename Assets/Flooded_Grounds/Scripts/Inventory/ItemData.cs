using UnityEngine;

namespace HorrorGame.Inventory
{
    public enum ItemType
    {
        Consumable,
        KeyItem,
        Weapon,
        Ammunition,
        Document,
        Tool,       // Rìu, cuốc, dao...
        Resource,   // Gỗ, đá, lá cây...
        Building    // Vật liệu xây dựng
    }

    [CreateAssetMenu(fileName = "New Item", menuName = "Horror Game/Inventory/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Item Info")]
        public string itemID;
        public string itemName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;

        [Header("Settings")]
        public ItemType itemType;
        public bool isStackable = false;
        public int maxStack = 1;

        [Header("3D Model (Optional)")]
        public GameObject itemPrefab;
    }
}

using UnityEngine;
using HorrorGame.Inventory;

namespace HorrorGame.Environment
{
    [RequireComponent(typeof(Collider))]
    public class LootChest : MonoBehaviour
    {
        [Header("Loot Settings")]
        public ItemData gunItem;
        public int gunAmount = 1;
        public ItemData ammoItem;
        public int ammoAmount = 30;

        [Header("Interaction Settings")]
        public string promptText = "Press E to Open Chest";
        public bool isOpened = false;
        
        [Header("Optional Animation")]
        public Animator chestAnimator;
        public string openTriggerName = "Open";

        private bool playerInRange = false;

        private void Start()
        {
            // Tự động chỉnh lại kích thước rương cho chuẩn khi bắt đầu game
            transform.localScale = new Vector3(1.5f, 0.75f, 1f);
        }

        private void Update()
        {
            if (playerInRange && !isOpened)
            {
                // Here you could link to a UI text to show "Press E to Open"
                
                if (Input.GetKeyDown(KeyCode.E))
                {
                    OpenChest();
                }
            }
        }

        private void OpenChest()
        {
            isOpened = true;
            
            // Play animation if available
            if (chestAnimator != null)
            {
                chestAnimator.SetTrigger(openTriggerName);
            }

            // Add items to inventory
            if (InventoryManager.Instance != null)
            {
                bool gotGun = false;
                bool gotAmmo = false;
                
                if (gunItem != null)
                {
                    gotGun = InventoryManager.Instance.AddItem(gunItem, gunAmount);
                    if(gotGun) Debug.Log("Looted: " + gunItem.itemName);
                }
                
                if (ammoItem != null)
                {
                    gotAmmo = InventoryManager.Instance.AddItem(ammoItem, ammoAmount);
                    if(gotAmmo) Debug.Log("Looted: " + ammoItem.itemName);
                }
                
                if (!gotGun && !gotAmmo)
                {
                    Debug.Log("Inventory Full! Could not loot chest.");
                    isOpened = false; // Allow trying again
                }
            }
            else
            {
                Debug.LogWarning("InventoryManager not found in scene!");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !isOpened)
            {
                playerInRange = true;
                Debug.Log(promptText); // For testing
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = false;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-assign Gun Item if missing
            if (gunItem == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData");
                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    ItemData item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
                    if (item != null && item.itemType == ItemType.Weapon)
                    {
                        gunItem = item;
                        break;
                    }
                }
            }

            // Auto-assign Ammo Item if missing
            if (ammoItem == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData");
                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    ItemData item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
                    if (item != null && item.itemType == ItemType.Ammunition)
                    {
                        ammoItem = item;
                        break;
                    }
                }
            }
        }
#endif
    }
}

using UnityEngine;
using System.Collections.Generic;
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

        [Header("Chest Inventory")]
        public int maxSlots = 12;
        public List<InventorySlot> chestSlots = new List<InventorySlot>();

        private void Start()
        {
            // Initialize empty slots
            for (int i = 0; i < maxSlots; i++)
            {
                chestSlots.Add(new InventorySlot(null, 0));
            }

            // Optional: Auto-fill initial loot if not yet looted/initialized
            // This happens once when game starts. 
            // If you want saving/loading, you will override these slots later.
            if (gunItem != null)
            {
                chestSlots[0].item = gunItem;
                chestSlots[0].amount = gunAmount;
            }
            if (ammoItem != null)
            {
                chestSlots[1].item = ammoItem;
                chestSlots[1].amount = ammoAmount;
            }
        }

        private Transform playerTransform;
        public float interactRange = 3f;

        private void Update()
        {
            // Tim nguoi choi neu chua co
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                }
            }

            // Kiem tra khoang cach
            if (playerTransform != null)
            {
                float distance = Vector3.Distance(transform.position, playerTransform.position);
                
                if (distance <= interactRange)
                {
                    playerInRange = true;
                    
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        Debug.Log("=> Da bam E vao ruong. isOpened hien tai: " + isOpened);
                        if (!isOpened)
                        {
                            OpenChest();
                        }
                        else
                        {
                            CloseChest();
                        }
                    }
                }
                else
                {
                    playerInRange = false;
                    
                    // Tu dong dong ruong neu di ra xa
                    if (isOpened)
                    {
                        CloseChest();
                    }
                }
            }
        }

        private void OpenChest()
        {
            isOpened = true;
            Debug.Log("=> Dang mo ruong...");
            
            // Play animation if available
            if (chestAnimator != null)
            {
                chestAnimator.SetTrigger(openTriggerName);
            }

            // Open Chest UI
            if (HorrorGame.Inventory.UI.ChestUI.Instance != null)
            {
                Debug.Log("=> Tim thay ChestUI, dang bat UI len...");
                HorrorGame.Inventory.UI.ChestUI.Instance.OpenChest(this);
                
                // Open Player Inventory as well
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.OpenInventory();
                }
            }
            else
            {
                Debug.LogError("=> LOI: ChestUI.Instance dang bi NULL. Vui long kiem tra lai xem ChestUIPanel da duoc bat truoc khi Play chua, va co script ChestUI chua!");
            }
        }

        private void CloseChest()
        {
            isOpened = false;
            Debug.Log("=> Dang dong ruong...");

            if (HorrorGame.Inventory.UI.ChestUI.Instance != null)
            {
                HorrorGame.Inventory.UI.ChestUI.Instance.CloseChest();
                
                // Close Player Inventory as well
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.CloseInventory();
                }
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

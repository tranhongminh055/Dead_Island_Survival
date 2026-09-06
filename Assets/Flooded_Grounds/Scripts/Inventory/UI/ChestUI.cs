using UnityEngine;
using System.Collections.Generic;
using HorrorGame.Environment;

namespace HorrorGame.Inventory.UI
{
    public class ChestUI : MonoBehaviour
    {
        public static ChestUI Instance;

        public GameObject chestUIPanel;
        public Transform slotsParent;
        
        private InventorySlotUI[] slotUIs;
        private LootChest currentChest;

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

        private void Start()
        {
            if (slotsParent != null)
            {
                slotUIs = slotsParent.GetComponentsInChildren<InventorySlotUI>();
            }
            
            if (chestUIPanel != null)
            {
                chestUIPanel.SetActive(false);
            }
        }

        public void OpenChest(LootChest chest)
        {
            currentChest = chest;
            
            if (chestUIPanel != null)
            {
                chestUIPanel.SetActive(true);
            }

            UpdateUI();
        }

        public void CloseChest()
        {
            currentChest = null;
            
            if (chestUIPanel != null)
            {
                chestUIPanel.SetActive(false);
            }
        }

        public void UpdateUI()
        {
            if (currentChest == null || slotUIs == null) return;

            List<InventorySlot> chestSlots = currentChest.chestSlots;

            for (int i = 0; i < slotUIs.Length; i++)
            {
                if (i < chestSlots.Count)
                {
                    slotUIs[i].slotIndex = i;
                    slotUIs[i].UpdateSlot(chestSlots[i]);
                }
                else
                {
                    slotUIs[i].UpdateSlot(null);
                }
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using System;

namespace HorrorGame.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance;

        [Header("Inventory Settings")]
        public int maxSlots = 12; // Mặc định 12 ô
        public List<ItemData> startingItems = new List<ItemData>(); // Các món đồ có sẵn khi vào game
        public List<InventorySlot> slots = new List<InventorySlot>();

        [Header("UI Reference")]
        public GameObject inventoryUIPanel;
        public bool isInventoryOpen = false;

        public Action onInventoryChangedEvent;

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

            // Khởi tạo các ô trống
            for (int i = 0; i < maxSlots; i++)
            {
                slots.Add(new InventorySlot(null, 0));
            }

            // Nếu startingItems trống, tự động load GunItem từ Resources
            if (startingItems == null || startingItems.Count == 0)
            {
                Debug.Log("[INVENTORY] startingItems trong, tu dong load GunItem tu Resources...");
                ItemData gunItem = Resources.Load<ItemData>("GunItem");
                if (gunItem != null)
                {
                    startingItems.Add(gunItem);
                    Debug.Log("[INVENTORY] Da load thanh cong: " + gunItem.itemName);
                }
                else
                {
                    Debug.LogWarning("[INVENTORY] KHONG TIM THAY GunItem trong Resources folder!");
                }
            }

            // Thêm các món đồ khởi đầu vào túi
            foreach (ItemData item in startingItems)
            {
                if (item != null)
                {
                    AddItem(item, 1);
                }
            }

            Debug.Log("===== [INVENTORY DEBUG] Sau khi load starting items (Awake) =====");
            DebugLogAllItems();

            if (inventoryUIPanel != null)
            {
                inventoryUIPanel.SetActive(false);
            }
        }

        private void Start()
        {
            // Starting items đã được load trong Awake()
        }

        private void Update()
        {
            // Bấm phím I để mở/tắt túi đồ (giống The Forest)
            if (Input.GetKeyDown(KeyCode.I))
            {
                ToggleInventory();
            }
        }

        public void ToggleInventory()
        {
            isInventoryOpen = !isInventoryOpen;
            if (inventoryUIPanel != null)
            {
                inventoryUIPanel.SetActive(isInventoryOpen);
            }

            // Tùy chọn: Khóa chuột và dừng thời gian khi mở túi đồ
            if (isInventoryOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Time.timeScale = 0f; // Dừng game

                Debug.Log("===== [INVENTORY DEBUG] Mở túi đồ - Danh sách items =====");
                DebugLogAllItems();
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Time.timeScale = 1f; // Tiếp tục game
            }
        }

        public bool AddItem(ItemData item, int amount)
        {
            Debug.Log(string.Format("[INVENTORY DEBUG] Đang thêm item: {0} (ID: {1}), Type: {2}, Số lượng: {3}, Stackable: {4}", item.itemName, item.itemID, item.itemType, amount, item.isStackable));

            // Kiểm tra xem item có cộng dồn được không
            if (item.isStackable)
            {
                foreach (InventorySlot slot in slots)
                {
                    if (slot.item == item && slot.amount < item.maxStack)
                    {
                        // Kiểm tra xem có vượt quá maxStack không
                        if (slot.amount + amount <= item.maxStack)
                        {
                            slot.AddAmount(amount);
                            Debug.Log(string.Format("[INVENTORY DEBUG] Da stack them {0}x {1}, tong: {2}", amount, item.itemName, slot.amount));
                            if (onInventoryChangedEvent != null) onInventoryChangedEvent();
                            return true;
                        }
                    }
                }
            }

            // Tìm ô trống đầu tiên
            foreach (InventorySlot slot in slots)
            {
                if (slot.IsEmpty())
                {
                    slot.item = item;
                    slot.amount = amount;
                    Debug.Log(string.Format("[INVENTORY DEBUG] Da them {0}x {1} vao o trong", amount, item.itemName));
                    if (onInventoryChangedEvent != null) onInventoryChangedEvent();
                    return true;
                }
            }

            Debug.LogWarning("[INVENTORY DEBUG] Tui do da day! Khong the them " + item.itemName);
            return false;
        }

        public void RemoveItem(ItemData item, int amount)
        {
            foreach (InventorySlot slot in slots)
            {
                if (slot.item == item)
                {
                    if (slot.amount >= amount)
                    {
                        slot.RemoveAmount(amount);
                        if (onInventoryChangedEvent != null) onInventoryChangedEvent();
                        return;
                    }
                }
            }
        }

        public bool HasItem(ItemData item)
        {
            foreach (InventorySlot slot in slots)
            {
                if (slot.item == item && slot.amount > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public void OpenInventory()
        {
            if (isInventoryOpen) return;
            ToggleInventory();
        }

        public void CloseInventory()
        {
            if (!isInventoryOpen) return;
            ToggleInventory();
        }

        /// <summary>
        /// [DEBUG] In ra toàn bộ nội dung inventory vào Console
        /// </summary>
        public void DebugLogAllItems()
        {
            int itemCount = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                if (!slot.IsEmpty())
                {
                    itemCount++;
                    Debug.Log(string.Format("  [Slot {0}] {1} (ID: {2}) | Type: {3} | SL: {4}/{5} | Icon: {6} | Prefab: {7}", i, slot.item.itemName, slot.item.itemID, slot.item.itemType, slot.amount, slot.item.maxStack, slot.item.icon != null ? "Co" : "THIEU", slot.item.itemPrefab != null ? "Co" : "Khong"));
                }
            }

            if (itemCount == 0)
            {
                Debug.LogWarning("  [INVENTORY DEBUG] >>> Inventory TRỐNG - Không có item nào! <<<");
            }
            else
            {
                Debug.Log(string.Format("  [INVENTORY DEBUG] Tong cong: {0}/{1} o da su dung", itemCount, slots.Count));
            }
        }
        /// <summary>
        /// Đếm tổng số lượng của một item trong inventory (dùng cho Building System)
        /// </summary>
        public int GetItemCount(ItemData item)
        {
            int total = 0;
            foreach (InventorySlot slot in slots)
            {
                if (slot.item == item && slot.amount > 0)
                {
                    total += slot.amount;
                }
            }
            return total;
        }

        /// <summary>
        /// Đếm tổng số lượng item theo itemID (dùng khi không có reference trực tiếp)
        /// </summary>
        public int GetItemCountByID(string itemID)
        {
            int total = 0;
            foreach (InventorySlot slot in slots)
            {
                if (slot.item != null && slot.item.itemID == itemID && slot.amount > 0)
                {
                    total += slot.amount;
                }
            }
            return total;
        }

        /// <summary>
        /// Trừ số lượng item theo itemID. Trả về true nếu trừ thành công.
        /// </summary>
        public bool RemoveItemByID(string itemID, int amountToRemove)
        {
            // Kiểm tra đủ số lượng trước
            if (GetItemCountByID(itemID) < amountToRemove) return false;

            int remaining = amountToRemove;
            foreach (InventorySlot slot in slots)
            {
                if (remaining <= 0) break;
                if (slot.item != null && slot.item.itemID == itemID && slot.amount > 0)
                {
                    if (slot.amount >= remaining)
                    {
                        slot.RemoveAmount(remaining);
                        remaining = 0;
                    }
                    else
                    {
                        remaining -= slot.amount;
                        slot.ClearSlot();
                    }
                }
            }

            if (onInventoryChangedEvent != null) onInventoryChangedEvent();
            return remaining <= 0;
        }
    }
}

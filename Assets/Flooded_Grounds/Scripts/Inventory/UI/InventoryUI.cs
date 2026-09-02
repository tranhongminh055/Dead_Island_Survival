using UnityEngine;
using System.Collections.Generic;

namespace HorrorGame.Inventory.UI
{
    public class InventoryUI : MonoBehaviour
    {
        public Transform slotsParent; // Kéo thả Grid Layout Group chứa các Slot vào đây
        private InventorySlotUI[] slotUIs;

        private void Start()
        {
            // Lấy tất cả các InventorySlotUI là con của slotsParent
            slotUIs = slotsParent.GetComponentsInChildren<InventorySlotUI>();

            // Đăng ký sự kiện khi Inventory thay đổi
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.onInventoryChangedEvent += UpdateUI;
            }

            UpdateUI(); // Cập nhật lần đầu
        }

        private void OnDestroy()
        {
            // Hủy đăng ký sự kiện để tránh lỗi memory leak
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.onInventoryChangedEvent -= UpdateUI;
            }
        }

        public void UpdateUI()
        {
            Debug.Log("Inventory da them vat pham");
            if (InventoryManager.Instance == null) return;

            List<InventorySlot> inventorySlots = InventoryManager.Instance.slots;

            for (int i = 0; i < slotUIs.Length; i++)
            {
                if (i < inventorySlots.Count)
                {
                    // Truyền dữ liệu sang UI để hiển thị
                    slotUIs[i].slotIndex = i; // Gán ID cho ô
                    slotUIs[i].UpdateSlot(inventorySlots[i]);
                }
                else
                {
                    // Xóa nếu không có ô đồ tương ứng
                    slotUIs[i].UpdateSlot(null);
                }
            }
        }
    }
}

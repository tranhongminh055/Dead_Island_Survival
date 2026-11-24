using UnityEngine;
using System.Collections.Generic;

namespace HorrorGame.Inventory.UI
{
    public class InventoryUI : MonoBehaviour
    {
        public Transform slotsParent; // Kéo thả Grid Layout Group chứa các Slot vào đây
        private InventorySlotUI[] slotUIs;

        private void EnsureSlots()
        {
            if (slotUIs != null && slotUIs.Length > 0) return;

            if (slotsParent != null)
            {
                slotUIs = slotsParent.GetComponentsInChildren<InventorySlotUI>(true);
            }
            if (slotUIs == null || slotUIs.Length == 0)
            {
                slotUIs = GetComponentsInChildren<InventorySlotUI>(true);
            }
        }

        private void Start()
        {
            EnsureSlots();

            // Đăng ký sự kiện khi Inventory thay đổi
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.onInventoryChangedEvent += UpdateUI;
            }

            UpdateUI(); // Cập nhật lần đầu
        }

        private void OnEnable()
        {
            EnsureSlots();

            // Mỗi lần mở panel, đảm bảo đã đăng ký event và cập nhật lại UI
            if (InventoryManager.Instance != null)
            {
                // Hủy đăng ký trước để tránh đăng ký trùng
                InventoryManager.Instance.onInventoryChangedEvent -= UpdateUI;
                InventoryManager.Instance.onInventoryChangedEvent += UpdateUI;
            }

            UpdateUI();
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
            if (InventoryManager.Instance == null) return;

            EnsureSlots();
            if (slotUIs == null || slotUIs.Length == 0) return;

            List<InventorySlot> inventorySlots = InventoryManager.Instance.slots;

            for (int i = 0; i < slotUIs.Length; i++)
            {
                if (slotUIs[i] == null) continue;

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

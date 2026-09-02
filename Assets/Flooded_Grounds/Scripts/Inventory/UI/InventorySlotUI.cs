using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HorrorGame.Inventory.UI
{
    public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler
    {
        public Image iconImage;
        public Text amountText;
        public int slotIndex = -1; // Vị trí của ô đồ
        
        private InventorySlot currentSlot;
        
        // Static variable để theo dõi item đang được kéo
        public static InventorySlotUI draggedSlot;
        private Vector3 originalIconPosition;
        private Transform originalIconParent;

        public void UpdateSlot(InventorySlot slot)
        {
            currentSlot = slot;

            if (slot == null || slot.IsEmpty())
            {
                iconImage.sprite = null;
                iconImage.color = new Color(1, 1, 1, 0);
                if(amountText != null) amountText.text = "";
                iconImage.raycastTarget = false; // Ô trống không cần nhận Raycast từ chuột (ngoại trừ ô nền)
            }
            else
            {   
                iconImage.sprite = slot.item.icon;
                iconImage.color = new Color(1, 1, 1, 1);
                iconImage.raycastTarget = true; // Để có thể click/kéo

                if (slot.amount > 1 && amountText != null)
                    amountText.text = slot.amount.ToString();
                else if (amountText != null)
                    amountText.text = "";
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Debug.Log("<color=cyan>CHUỘT ĐÃ CHẠM VÀO Ô SỐ:</color> " + slotIndex);
        }

        // --- CLICK (Dùng đồ) ---
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                // Tách đồ (Split) - nếu cần làm giống Minecraft
                // Để mở rộng sau này
            }
            else if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (currentSlot != null && !currentSlot.IsEmpty())
                {
                    Debug.Log("Bạn vừa click vào: " + currentSlot.item.itemName + " | Phân loại: " + currentSlot.item.itemType);
                    
                    // Nếu bấm vào Súng
                    if (currentSlot.item.itemType == ItemType.Weapon)
                    {
                        var fpsWeapon = FindObjectOfType<FPSWeapon>();
                        
                        if (fpsWeapon == null)
                        {
                            Debug.Log("<color=red>LỖI:</color> Không tìm thấy script FPSWeapon trên Camera!");
                        }
                        else if (fpsWeapon.weaponItemData == null || fpsWeapon.weaponItemData.itemID != currentSlot.item.itemID)
                        {
                            Debug.Log("<color=red>LỖI:</color> Khẩu súng ở Camera CHƯA được gắn file GunItem vào ô Weapon Item Data, hoặc bị sai ID!");
                        }
                        else
                        {
                            Debug.Log("<color=green>THÀNH CÔNG!</color> Đã rút súng!");
                            fpsWeapon.ToggleWeapon();
                            InventoryManager.Instance.ToggleInventory();
                        }
                    }
                    else
                    {
                        Debug.Log("<color=yellow>CẢNH BÁO:</color> Bạn chưa đổi Item Type của file GunItem thành Weapon!");
                    }
                }
            }
        }

        // --- DRAG (Kéo) ---
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (currentSlot == null || currentSlot.IsEmpty()) return; // Ô trống không kéo được

            draggedSlot = this;
            originalIconPosition = iconImage.transform.position;
            originalIconParent = iconImage.transform.parent;

            // Đưa Icon lên root của Canvas để không bị che khuất
            iconImage.transform.SetParent(iconImage.transform.root);
            iconImage.transform.SetAsLastSibling(); // Hiển thị trên cùng
            iconImage.raycastTarget = false; // Bỏ qua Raycast để chuột có thể xuyên qua thả vào ô dưới
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (draggedSlot == null) return;
            iconImage.transform.position = Input.mousePosition; // Đi theo chuột
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (draggedSlot == null) return;

            // Reset vị trí icon về ô cũ nếu không drop thành công
            iconImage.transform.SetParent(originalIconParent);
            iconImage.transform.position = originalIconPosition;
            iconImage.raycastTarget = true; // Bật lại Raycast

            draggedSlot = null;

            // Xử lý vứt đồ ra ngoài (Nếu thả chuột ra ngoài cửa sổ UI)
            if (eventData.pointerEnter == null)
            {
                Debug.Log("Vứt đồ ra ngoài: " + currentSlot.item.itemName);
                // InventoryManager.Instance.DropItem(currentSlot); 
            }
        }

        // --- DROP (Thả) ---
        public void OnDrop(PointerEventData eventData)
        {
            if (InventorySlotUI.draggedSlot != null && InventorySlotUI.draggedSlot != this)
            {
                Debug.Log("Đổi chỗ: " + InventorySlotUI.draggedSlot.slotIndex + " và " + this.slotIndex);
                // Gọi InventoryManager để hoán đổi dữ liệu
                InventoryManager.Instance.SwapItems(InventorySlotUI.draggedSlot.slotIndex, this.slotIndex);
            }
        }
        
    }
}

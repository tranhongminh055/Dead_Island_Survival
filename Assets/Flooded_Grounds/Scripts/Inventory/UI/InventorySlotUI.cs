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
                if (amountText != null) 
                {
                    amountText.text = "";
                    amountText.raycastTarget = false;
                }
                iconImage.raycastTarget = false; // Ô trống không cần nhận Raycast từ chuột (ngoại trừ ô nền)
            }
            else
            {   
                iconImage.sprite = slot.item.icon;
                iconImage.color = new Color(1, 1, 1, 1);
                iconImage.raycastTarget = true; // Để có thể click/kéo

                if (amountText != null)
                {
                    amountText.raycastTarget = false; // Tránh chữ số lượng chặn click chuột vào icon
                    if (slot.amount > 1)
                        amountText.text = slot.amount.ToString();
                    else
                        amountText.text = "";
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Debug.Log("<color=cyan>CHUỘT ĐÃ CHẠM VÀO Ô SỐ:</color> " + slotIndex);
        }

        // --- CLICK (Dùng đồ - bấm chuột trái hoặc chuột phải đều dùng được) ---
        public void OnPointerClick(PointerEventData eventData)
        {
            UseCurrentItem();
        }

        public void UseCurrentItem()
        {
            if (currentSlot == null || currentSlot.IsEmpty() || currentSlot.item == null) return;

            ItemData item = currentSlot.item;
            string id = !string.IsNullOrEmpty(item.itemID) ? item.itemID.ToLower() : "";
            string name = !string.IsNullOrEmpty(item.itemName) ? item.itemName.ToLower() : "";
            ItemType type = item.itemType;

            Debug.Log(string.Format("<color=yellow>[USE ITEM]</color> Click sử dụng: {0} (ID: {1}, Type: {2})", item.itemName, item.itemID, type));

            // Tìm PlayerStats trên Player
            var stats = FindObjectOfType<Player.PlayerStats>();
            if (stats == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) stats = playerObj.GetComponent<Player.PlayerStats>();
            }

            // Tìm FPSWeapon trên Player
            var fpsWeapon = FindObjectOfType<FPSWeapon>();
            if (fpsWeapon == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) fpsWeapon = playerObj.GetComponentInChildren<FPSWeapon>();
            }

            bool itemUsed = false;
            string feedbackMsg = "";

            // 1. HỒI MÁU (Băng cứu thương / Medkit)
            if (id.Contains("medkit") || id.Contains("bandaid") || id.Contains("bandage") || name.Contains("băng") || name.Contains("cứu thương") || name.Contains("thuốc"))
            {
                if (stats != null)
                {
                    stats.Heal(40f);
                    itemUsed = true;
                    feedbackMsg = string.Format("💚 Đã dùng Băng cứu thương: Hồi +40 Máu! ({0:F0}/{1} HP)", stats.currentHealth, stats.maxHealth);
                }
            }
            // 2. LƯƠNG THỰC / ĐỒ HỘP (Hộp lương thực dự trữ / Food)
            else if (id.Contains("food") || id.Contains("ration") || id.Contains("canned") || name.Contains("lương thực") || name.Contains("thịt") || name.Contains("ăn"))
            {
                if (stats != null)
                {
                    stats.Eat(45f);
                    stats.Heal(15f); // Thức ăn dinh dưỡng hồi thêm 15 HP!
                    stats.currentStamina = Mathf.Min(stats.maxStamina, stats.currentStamina + 35f);
                    stats.RestoreSanity(20f);
                    itemUsed = true;
                    feedbackMsg = string.Format("🍖 Đã ăn Hộp lương thực: +45 Đói, +15 Máu, +35 Thể lực!");
                }
            }
            // 3. ĐẠN DƯỢC (Hộp đạn dã chiến / Ammo)
            else if (type == ItemType.Ammunition || id.Contains("ammo") || name.Contains("đạn"))
            {
                if (fpsWeapon != null)
                {
                    fpsWeapon.AddAmmo(30);
                    itemUsed = true;
                    feedbackMsg = "🔫 Đã nạp thêm +30 viên đạn vào súng!";
                }
                else
                {
                    feedbackMsg = "⚠️ Chưa trang bị súng để nạp đạn!";
                }
            }
            // 4. ĐÈN PIN CHIẾN THUẬT
            else if (id.Contains("flashlight") || name.Contains("đèn pin"))
            {
                var fl = FindObjectOfType<Player.PlayerFlashlight>();
                if (fl == null)
                {
                    var playerObj = GameObject.FindGameObjectWithTag("Player");
                    if (playerObj != null) fl = playerObj.AddComponent<Player.PlayerFlashlight>();
                }

                if (fl != null)
                {
                    fl.hasFlashlight = true;
                    fl.ToggleFlashlight();
                    feedbackMsg = fl.isOn ? "💡 Đèn pin: BẬT" : "💡 Đèn pin: TẮT";
                }
            }
            // 5. VŨ KHÍ (Súng / Lựu đạn)
            else if (type == ItemType.Weapon)
            {
                if (fpsWeapon != null)
                {
                    fpsWeapon.ToggleWeapon();
                    feedbackMsg = "🗡️ Đã đổi / trang bị vũ khí!";
                    if (InventoryManager.Instance != null) InventoryManager.Instance.ToggleInventory();
                }
            }
            // 6. BẤT KỲ VẬT PHẨM TIÊU HAO KHÁC
            else if (type == ItemType.Consumable)
            {
                if (stats != null)
                {
                    stats.Heal(25f);
                    itemUsed = true;
                    feedbackMsg = string.Format("✨ Đã sử dụng {0} (+25 Máu)", item.itemName);
                }
            }

            // Nếu vật phẩm tiêu thụ thành công: trừ trực tiếp 1 số lượng khỏi ô hiện tại
            if (itemUsed)
            {
                currentSlot.RemoveAmount(1);
                if (InventoryManager.Instance != null && InventoryManager.Instance.onInventoryChangedEvent != null)
                {
                    InventoryManager.Instance.onInventoryChangedEvent();
                }
            }

            // Hiển thị thông báo và in Log rõ ràng
            if (!string.IsNullOrEmpty(feedbackMsg))
            {
                Debug.Log("<color=green>[INVENTORY ACTION]:</color> " + feedbackMsg);
                var fl = FindObjectOfType<Player.PlayerFlashlight>();
                if (fl != null) fl.ShowNotice(feedbackMsg);
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
                
                InventorySlot slotA = InventorySlotUI.draggedSlot.currentSlot;
                InventorySlot slotB = this.currentSlot;

                if (slotA == null || slotB == null) return;

                // Nếu thả vào ô chứa item giống nhau và có thể cộng dồn (stack)
                if (slotA.item != null && slotB.item != null && slotA.item == slotB.item && slotA.item.isStackable)
                {
                    int totalAmount = slotA.amount + slotB.amount;
                    if (totalAmount <= slotA.item.maxStack)
                    {
                        slotB.amount = totalAmount;
                        slotA.ClearSlot();
                    }
                    else
                    {
                        int leftover = totalAmount - slotA.item.maxStack;
                        slotB.amount = slotA.item.maxStack;
                        slotA.amount = leftover;
                    }
                }
                else
                {
                    // Hoán đổi vị trí bình thường
                    ItemData tempItem = slotA.item;
                    int tempAmount = slotA.amount;

                    slotA.item = slotB.item;
                    slotA.amount = slotB.amount;

                    slotB.item = tempItem;
                    slotB.amount = tempAmount;
                }

                // Cập nhật lại UI cho cả 2 ô
                InventorySlotUI.draggedSlot.UpdateSlot(slotA);
                this.UpdateSlot(slotB);
            }
        }
        
    }
}

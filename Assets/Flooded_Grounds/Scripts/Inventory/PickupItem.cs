using UnityEngine;

namespace HorrorGame.Inventory
{
    [RequireComponent(typeof(Collider))]
    public class PickupItem : MonoBehaviour
    {
        public ItemData itemData;
        public int amount = 1;

        // Giả sử bạn sử dụng hệ thống Raycast từ Camera để tương tác (nhìn vào vật phẩm và bấm E)
        // Nếu bạn dùng Trigger thì sử dụng OnTriggerEnter
        
        // Hàm này sẽ được gọi bởi script PlayerInteract (hoặc tương đương)
        public void Interact()
        {
            if (InventoryManager.Instance != null && itemData != null)
            {
                bool success = InventoryManager.Instance.AddItem(itemData, amount);
                if (success)
                {
                    Debug.Log("Đã nhặt: " + itemData.itemName);
                    // TODO: Thêm âm thanh nhặt đồ tại đây
                    
                    Destroy(gameObject); // Biến mất khỏi màn hình
                }
                else
                {
                    Debug.Log("Không thể nhặt, túi đồ đã đầy.");
                }
            }
        }
    }
}

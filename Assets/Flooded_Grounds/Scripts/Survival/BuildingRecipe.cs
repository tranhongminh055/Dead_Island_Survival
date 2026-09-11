using UnityEngine;

namespace HorrorGame.Survival
{
    public enum BuildingCategory
    {
        Shelter,    // Lều, nhà
        Structure,  // Hàng rào, tường
        Utility     // Lửa trại, bàn chế tạo
    }

    /// <summary>
    /// Một nguyên liệu cần thiết trong công thức xây dựng
    /// </summary>
    [System.Serializable]
    public class RecipeIngredient
    {
        [Tooltip("ItemData của nguyên liệu (Gỗ, Đá, Lá...)")]
        public HorrorGame.Inventory.ItemData item;

        [Tooltip("Số lượng cần")]
        public int amount = 1;
    }

    /// <summary>
    /// ScriptableObject chứa công thức xây dựng 1 công trình.
    /// Tạo bằng menu: Horror Game/Survival/Building Recipe
    /// </summary>
    [CreateAssetMenu(fileName = "New Recipe", menuName = "Horror Game/Survival/Building Recipe")]
    public class BuildingRecipe : ScriptableObject
    {
        [Header("Thông tin công trình")]
        public string recipeName = "Công trình mới";
        [TextArea(2, 3)]
        public string description;
        public Sprite icon;
        public BuildingCategory category = BuildingCategory.Shelter;

        [Header("Nguyên liệu cần thiết")]
        public RecipeIngredient[] ingredients;

        [Header("Prefab")]
        [Tooltip("Prefab hiển thị khi đặt vị trí (ghost xanh bán trong suốt). Nếu để trống, sẽ dùng resultPrefab.")]
        public GameObject previewPrefab;

        [Tooltip("Prefab công trình hoàn chỉnh sau khi xây xong.")]
        public GameObject resultPrefab;

        [Header("Cài đặt đặt vị trí")]
        [Tooltip("Offset từ điểm raycast xuống mặt đất")]
        public Vector3 buildOffset = Vector3.zero;

        [Tooltip("Cho phép xoay khi đặt")]
        public bool allowRotation = true;

        /// <summary>
        /// Kiểm tra xem inventory có đủ nguyên liệu để xây không
        /// </summary>
        public bool CanBuild()
        {
            if (ingredients == null || ingredients.Length == 0) return true;

            var inv = HorrorGame.Inventory.InventoryManager.Instance;
            if (inv == null) return false;

            foreach (var ing in ingredients)
            {
                if (ing.item == null) continue;
                if (inv.GetItemCount(ing.item) < ing.amount) return false;
            }
            return true;
        }

        /// <summary>
        /// Trừ nguyên liệu khỏi inventory. Trả về true nếu thành công.
        /// </summary>
        public bool ConsumeIngredients()
        {
            if (!CanBuild()) return false;

            var inv = HorrorGame.Inventory.InventoryManager.Instance;
            if (inv == null) return false;

            foreach (var ing in ingredients)
            {
                if (ing.item == null) continue;
                inv.RemoveItem(ing.item, ing.amount);
            }
            return true;
        }
    }
}

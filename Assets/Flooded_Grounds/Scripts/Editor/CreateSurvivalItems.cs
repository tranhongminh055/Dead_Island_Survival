using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor Script: Tự động tạo các ItemData ScriptableObject cho hệ thống sinh tồn.
/// Chạy từ menu: Horror Game → Survival → Create Survival Items
/// Tạo 4 item: Rìu, Gỗ, Đá, Lá Cây vào thư mục Assets/Resources/
/// </summary>
public class CreateSurvivalItems : MonoBehaviour
{
    [MenuItem("Horror Game/Survival/Tạo Item Sinh Tồn (Rìu, Gỗ, Đá, Lá)", false, 100)]
    public static void CreateAllSurvivalItems()
    {
        // Đảm bảo thư mục Resources tồn tại
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        int created = 0;

        // 1. RÌU (Tool)
        created += CreateItemIfNotExists(
            path: "Assets/Resources/AxeItem.asset",
            itemID: "axe",
            itemName: "Rìu",
            description: "Rìu chặt cây — Trang bị bằng phím [2], click trái để chặt cây lấy gỗ và lá.",
            itemType: HorrorGame.Inventory.ItemType.Tool,
            isStackable: false,
            maxStack: 1
        );

        // 2. GỖ (Resource - stackable)
        created += CreateItemIfNotExists(
            path: "Assets/Resources/WoodItem.asset",
            itemID: "wood",
            itemName: "Gỗ",
            description: "Gỗ thu hoạch từ cây. Dùng để xây nhà, hàng rào và lửa trại.",
            itemType: HorrorGame.Inventory.ItemType.Resource,
            isStackable: true,
            maxStack: 99
        );

        // 3. ĐÁ (Resource - stackable)
        created += CreateItemIfNotExists(
            path: "Assets/Resources/StoneItem.asset",
            itemID: "stone",
            itemName: "Đá",
            description: "Đá nhặt từ mặt đất. Dùng để xây nhà đá và lửa trại.",
            itemType: HorrorGame.Inventory.ItemType.Resource,
            isStackable: true,
            maxStack: 99
        );

        // 4. LÁ CÂY (Resource - stackable)
        created += CreateItemIfNotExists(
            path: "Assets/Resources/LeafItem.asset",
            itemID: "leaf",
            itemName: "Lá Cây",
            description: "Lá cây rơi khi chặt cây. Dùng để dựng lều lá.",
            itemType: HorrorGame.Inventory.ItemType.Resource,
            isStackable: true,
            maxStack: 99
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (created > 0)
        {
            EditorUtility.DisplayDialog(
                "Tạo Item Sinh Tồn Thành Công!",
                string.Format("Đã tạo {0} ItemData mới trong thư mục Assets/Resources/.\n\n" +
                "• AxeItem (Rìu)\n" +
                "• WoodItem (Gỗ)\n" +
                "• StoneItem (Đá)\n" +
                "• LeafItem (Lá Cây)\n\n" +
                "Lưu ý: Bạn có thể kéo icon Sprite vào từng item trong Inspector.", created),
                "OK"
            );
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Thông báo",
                "Tất cả ItemData sinh tồn đã tồn tại trong Assets/Resources/.\nKhông cần tạo thêm.",
                "OK"
            );
        }

        Debug.Log(string.Format("[CreateSurvivalItems] Hoàn tất! Đã tạo {0} item mới.", created));
    }

    /// <summary>
    /// Tạo ItemData ScriptableObject nếu chưa tồn tại
    /// </summary>
    static int CreateItemIfNotExists(string path, string itemID, string itemName,
        string description, HorrorGame.Inventory.ItemType itemType, bool isStackable, int maxStack)
    {
        // Kiểm tra đã tồn tại chưa
        HorrorGame.Inventory.ItemData existing = AssetDatabase.LoadAssetAtPath<HorrorGame.Inventory.ItemData>(path);
        if (existing != null)
        {
            Debug.Log(string.Format("[CreateSurvivalItems] {0} đã tồn tại, bỏ qua.", itemName));
            return 0;
        }

        // Tạo mới
        HorrorGame.Inventory.ItemData item = ScriptableObject.CreateInstance<HorrorGame.Inventory.ItemData>();
        item.itemID = itemID;
        item.itemName = itemName;
        item.description = description;
        item.itemType = itemType;
        item.isStackable = isStackable;
        item.maxStack = maxStack;
        item.icon = null;       // Người dùng tự gán icon sau
        item.itemPrefab = null; // Sẽ tự tạo placeholder runtime

        AssetDatabase.CreateAsset(item, path);
        Debug.Log(string.Format("[CreateSurvivalItems] ĐÃ TẠO: {0} tại {1}", itemName, path));

        return 1;
    }

    // ===================================================
    // MENU PHỤ: Tạo nhanh cây và đá trong scene
    // ===================================================
    [MenuItem("Horror Game/Survival/Thêm Cây Chặt Được vào Scene", false, 200)]
    public static void AddChoppableTreeToScene()
    {
        // Tìm xem có cây nào đang được chọn không
        GameObject selected = Selection.activeGameObject;

        if (selected != null)
        {
            // Gắn ChoppableTree lên object đang chọn
            if (selected.GetComponent<HorrorGame.Survival.ChoppableTree>() == null)
            {
                selected.AddComponent<HorrorGame.Survival.ChoppableTree>();

                // Đảm bảo có Collider
                if (selected.GetComponent<Collider>() == null)
                {
                    CapsuleCollider cap = selected.AddComponent<CapsuleCollider>();
                    cap.height = 6f;
                    cap.radius = 0.5f;
                    cap.center = new Vector3(0, 3f, 0);
                }

                Debug.Log(string.Format("[CreateSurvivalItems] Đã gắn ChoppableTree lên: {0}", selected.name));
                EditorUtility.DisplayDialog("Thành công", "Đã gắn ChoppableTree lên " + selected.name, "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Thông báo", selected.name + " đã có ChoppableTree rồi.", "OK");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Hướng dẫn",
                "Hãy chọn 1 hoặc nhiều cây trong Scene (Hierarchy), rồi chạy lại lệnh này.\n\n" +
                "Script ChoppableTree sẽ được gắn lên cây đã chọn.",
                "OK");
        }
    }

    [MenuItem("Horror Game/Survival/Thêm Đá Nhặt Được vào Scene", false, 201)]
    public static void AddCollectableStoneToScene()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected != null)
        {
            if (selected.GetComponent<HorrorGame.Survival.CollectableStone>() == null)
            {
                selected.AddComponent<HorrorGame.Survival.CollectableStone>();

                // Đảm bảo có Collider
                if (selected.GetComponent<Collider>() == null)
                {
                    SphereCollider sphere = selected.AddComponent<SphereCollider>();
                    sphere.radius = 0.3f;
                    sphere.isTrigger = true;
                }

                Debug.Log(string.Format("[CreateSurvivalItems] Đã gắn CollectableStone lên: {0}", selected.name));
                EditorUtility.DisplayDialog("Thành công", "Đã gắn CollectableStone lên " + selected.name, "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Thông báo", selected.name + " đã có CollectableStone rồi.", "OK");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Hướng dẫn",
                "Hãy chọn 1 hoặc nhiều đá trong Scene (Hierarchy), rồi chạy lại lệnh này.\n\n" +
                "Script CollectableStone sẽ được gắn lên đá đã chọn.",
                "OK");
        }
    }
}

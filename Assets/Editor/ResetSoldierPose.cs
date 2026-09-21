using UnityEngine;
using UnityEditor;

public class ResetSoldierPose
{
    // Thêm nút chức năng vào menu Tools của Unity
    [MenuItem("Tools/Sửa Dáng Đứng Của Lính")]
    static void ResetPose()
    {
        // Tìm mô hình anh lính
        GameObject soldier = GameObject.Find("Ch15_nonPBR");
        if (soldier == null)
        {
            Debug.LogWarning("Không tìm thấy object tên là 'Ch15_nonPBR'. Bạn hãy chắc chắn là chưa đổi tên nó nhé.");
            return;
        }

        #pragma warning disable 0618
        // Cách siêu an toàn: Tạo một bản sao tạm thời từ Prefab gốc, chép lại góc xoay của từng cục xương, rồi xóa bản sao đi.
        Object prefabObj = PrefabUtility.GetPrefabParent(soldier);
        if (prefabObj != null)
        {
            GameObject tempPrefab = PrefabUtility.InstantiatePrefab(prefabObj) as GameObject;
            if (tempPrefab != null)
            {
                CopyTransforms(tempPrefab.transform, soldier.transform);
                GameObject.DestroyImmediate(tempPrefab);
                Debug.Log("Đã nắn xương thành công bằng cách copy chính xác 100% từ Prefab gốc!");
            }
        }
        else
        {
            Debug.LogWarning("Không tìm thấy Prefab gốc! Đảm bảo nhân vật của bạn được kéo ra từ Prefab.");
        }
        #pragma warning restore 0618

        // Đặt lại tọa độ gốc của anh lính cho vừa khít với Capsule (thân ở giữa, chân chạm đáy)
        soldier.transform.localPosition = new Vector3(0, -1, 0);
        
        // Chỉnh lại góc xoay cơ bản
        soldier.transform.localRotation = Quaternion.identity;

        // Bắt trúng mặt đất (Terrain) và tự động kéo cả Cụm Player (Capsule) xuống chạm đất
        if (soldier.transform.parent != null)
        {
            Transform root = soldier.transform.parent;
            RaycastHit hit;
            if (Physics.Raycast(root.position + Vector3.up * 10f, Vector3.down, out hit, 100f))
            {
                // Vì Capsule có chiều cao 2m (tâm nằm giữa), nên tâm đặt cách mặt đất 1m là khít
                root.position = hit.point + new Vector3(0, 1f, 0);
            }
        }

        // Tự động focus vào anh lính để bạn kiểm tra
        Selection.activeGameObject = soldier;
    }

    // Hàm đệ quy để copy góc xoay của từng cục xương từ bản gốc sang bản bị lỗi
    static void CopyTransforms(Transform source, Transform dest)
    {
        dest.localPosition = source.localPosition;
        dest.localRotation = source.localRotation;
        dest.localScale = source.localScale;
        
        for (int i = 0; i < source.childCount; i++)
        {
            Transform childSource = source.GetChild(i);
            Transform childDest = dest.Find(childSource.name);
            if (childDest != null)
            {
                CopyTransforms(childSource, childDest);
            }
        }
    }
}

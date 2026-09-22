using UnityEngine;
using UnityEditor;

public class CurvedCabinGenerator : EditorWindow
{
    [MenuItem("Horror Game/✈️ Vá Lỗ Hổng Bằng Vỏ Máy Bay Thật (Fix Gap)")]
    public static void GenerateCurvedCabin()
    {
        // 1. Xóa cái khối bát giác xấu xí lúc nãy đi (nếu có)
        GameObject uglyOctagon = GameObject.Find("Curved_Fuselage");
        if (uglyOctagon != null)
        {
            DestroyImmediate(uglyOctagon);
        }

        // 2. Lấy cái vỏ máy bay xịn mà bạn đang chọn (Fuselage_Mid_Aft)
        GameObject selectedShell = Selection.activeGameObject;
        if (selectedShell == null || selectedShell.GetComponent<MeshFilter>() == null)
        {
            Debug.LogError("❌ Vui lòng click chọn phần vỏ máy bay (Fuselage_Mid_Aft) trong Hierarchy trước khi bấm nút này!");
            return;
        }

        // 3. Nhân bản (Duplicate) cái vỏ xịn đó ra để làm phần thân giữa
        GameObject centerShell = Instantiate(selectedShell, selectedShell.transform.parent);
        centerShell.name = "Fuselage_Mid_Center_Fix";

        // Vá lỗi đi xuyên tường: Tự động gắn MeshCollider cho vỏ máy bay
        MeshFilter[] meshes = centerShell.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter mf in meshes)
        {
            if (mf.gameObject.GetComponent<Collider>() == null)
            {
                mf.gameObject.AddComponent<MeshCollider>();
            }
        }

        // 4. Kéo dài phần vỏ này ra (Scale Z) để nó lấp đầy khoảng trống
        // Tạm thời scale Z lên 4 lần (bạn có thể tự dùng phím R để kéo cho vừa khít)
        Vector3 newScale = centerShell.transform.localScale;
        newScale.z *= 4f; 
        centerShell.transform.localScale = newScale;

        // 5. Dịch chuyển nó về phía trước một chút để lọt vào giữa khe hở
        centerShell.transform.position += centerShell.transform.forward * 4f;

        // Chọn luôn vật thể mới để bạn dễ kéo thả
        Selection.activeGameObject = centerShell;

        Debug.Log("✅ Đã vá lỗ hổng bằng vỏ máy bay thật! Bạn hãy dùng phím R (Scale) và W (Move) để kéo cho nó vừa khít 100% nhé!");
    }
}

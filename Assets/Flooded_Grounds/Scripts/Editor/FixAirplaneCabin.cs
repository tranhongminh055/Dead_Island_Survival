using UnityEngine;
using UnityEditor;

public class FixAirplaneCabin : EditorWindow
{
    [MenuItem("Horror Game/Fix Airplane Cabin Gap (Thay vỏ hộp bằng Model thật)")]
    public static void FixCabin()
    {
        // 1. Tìm GameObject cụm máy bay tạm thời (khoang hộp vuông)
        GameObject tempCabin = GameObject.Find("Realistic_Airplane_Cabin");
        if (tempCabin == null) tempCabin = GameObject.Find("Temp_AirplaneCabin");
        if (tempCabin == null) tempCabin = GameObject.Find("[AIRPLANE_CRASH_SITE]");

        if (tempCabin == null)
        {
            Debug.LogError("❌ Không tìm thấy khoang khối hộp nào (Realistic_Airplane_Cabin hoặc Temp_AirplaneCabin) trong Scene!");
            return;
        }

        // 2. Lấy Object Vỏ máy bay (phần hình trụ) mà người dùng đang chọn
        GameObject realAirplaneModel = Selection.activeGameObject;
        if (realAirplaneModel == null || realAirplaneModel == tempCabin || realAirplaneModel.transform.parent == tempCabin.transform)
        {
            Debug.LogWarning("⚠️ Vui lòng click chọn Model Vỏ Máy Bay (phần thân trụ bên phải) trong cửa sổ Hierarchy, sau đó bấm lại nút Fix này!");
            return;
        }

        // 3. Xóa các khối hộp giả (sàn, tường, trần) do script cũ tạo ra
        int deletedCount = 0;
        // Lặp ngược để an toàn khi xóa children
        for (int i = tempCabin.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = tempCabin.transform.GetChild(i);
            string n = child.name;
            // Xóa các object hình hộp (CabinFloor, Wall_Left, Wall_Right, CabinCeiling, hoặc chứa Cube)
            if (n.Contains("Cube") || n.Contains("Wall") || n.Contains("Floor") || n.Contains("Ceiling") || n.Contains("Roof"))
            {
                DestroyImmediate(child.gameObject);
                deletedCount++;
            }
        }

        // 4. Di chuyển & Căn chỉnh model vỏ máy bay thật vào đúng vị trí của cụm khoang
        realAirplaneModel.transform.position = tempCabin.transform.position;
        // Đặt vỏ máy bay làm con của Temp_AirplaneCabin để đi liền với nhau
        realAirplaneModel.transform.SetParent(tempCabin.transform);

        Debug.Log("✅ ĐÃ SỬA LỖI ĐỒNG NHẤT: Xóa " + deletedCount + " khối hộp giả và thay thế bằng vỏ máy bay thật '" + realAirplaneModel.name + "' thành công!");
    }
}

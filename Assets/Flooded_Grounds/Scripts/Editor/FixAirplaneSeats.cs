using UnityEngine;
using UnityEditor;

public class FixAirplaneSeats : EditorWindow
{
    [MenuItem("Horror Game/🪑 Sửa Ghế Máy Bay Bị Gập (Fix Folded Seats)")]
    public static void FixFoldedSeats()
    {
        // Tìm C400 trong scene
        GameObject c400 = GameObject.Find("[C400_AIRPLANE_CABIN]");
        
        // Nếu chưa có trong Scene → tự động load từ FBX và đặt vào
        if (c400 == null)
        {
            Debug.Log("⏳ C400 chưa có trong Scene, đang tự động load...");
            
            GameObject c400Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/c-400/source/c-400.fbx");
            if (c400Prefab == null)
            {
                Debug.LogError("❌ Không tìm thấy model C400 tại Assets/Flooded_Grounds/c-400/source/c-400.fbx!");
                return;
            }
            
            c400 = (GameObject)PrefabUtility.InstantiatePrefab(c400Prefab);
            c400.name = "[C400_AIRPLANE_CABIN]";
            c400.transform.position = new Vector3(0f, 1000f, 0f);
            c400.transform.localScale = Vector3.one * 10f;
            
            Debug.Log("✅ Đã tự động đặt C400 vào Scene tại Y=1000.");
        }

        int fixedCount = 0;

        Transform[] allChildren = c400.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allChildren)
        {
            if (t == null) continue;
            string name = t.name;

            // ══════════════════════════════════════════════════
            // FIX: Đệm ghế bên TRÁI (CargoChairsCushionsL)
            // Bị gập vào với rotation Y=285 → Mở ra Y=0
            // ══════════════════════════════════════════════════
            if (name == "CargoChairsCushionsL")
            {
                Undo.RecordObject(t, "Fix Left Seat Cushions");
                Vector3 rot = t.localEulerAngles;
                Debug.Log("🪑 [TRƯỚC] " + name + " LocalRot=" + rot);
                
                t.localRotation = Quaternion.Euler(rot.x, 0f, rot.z);
                
                Debug.Log("✅ [SAU]  " + name + " LocalRot=" + t.localEulerAngles);
                fixedCount++;
            }

            // ══════════════════════════════════════════════════
            // FIX: Đệm ghế bên PHẢI (CargoChairsCushionsR)
            // Bị gập vào với rotation Y=75 → Mở ra Y=0
            // ══════════════════════════════════════════════════
            if (name == "CargoChairsCushionsR")
            {
                Undo.RecordObject(t, "Fix Right Seat Cushions");
                Vector3 rot = t.localEulerAngles;
                Debug.Log("🪑 [TRƯỚC] " + name + " LocalRot=" + rot);
                
                t.localRotation = Quaternion.Euler(rot.x, 0f, rot.z);
                
                Debug.Log("✅ [SAU]  " + name + " LocalRot=" + t.localEulerAngles);
                fixedCount++;
            }
        }

        if (fixedCount > 0)
        {
            EditorUtility.SetDirty(c400);
            Selection.activeGameObject = c400;
            Debug.Log("🎉 ĐÃ SỬA " + fixedCount + " BỘ PHẬN GHẾ! Ghế bây giờ đã mở ra bình thường. Hãy ấn Play để kiểm tra.");
        }
        else
        {
            Debug.LogWarning("⚠️ Không tìm thấy ghế bị gập (CargoChairsCushionsL/R). Có thể ghế đã được sửa rồi.");
        }
    }

    [MenuItem("Horror Game/🪑 Thử Các Góc Ghế Khác (Debug Seat Rotation)")]
    public static void TrySeatRotations()
    {
        GameObject c400 = GameObject.Find("[C400_AIRPLANE_CABIN]");
        if (c400 == null)
        {
            Debug.LogError("❌ Không tìm thấy [C400_AIRPLANE_CABIN]! Hãy chạy '🪑 Sửa Ghế Máy Bay Bị Gập' trước.");
            return;
        }

        Transform[] allChildren = c400.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allChildren)
        {
            if (t == null) continue;
            
            if (t.name == "CargoChairsCushionsL" || t.name == "CargoChairsCushionsR")
            {
                Undo.RecordObject(t, "Try Seat Rotation");
                Vector3 rot = t.localEulerAngles;
                // Xoay thêm 180 độ trục Y
                float newY = (rot.y + 180f) % 360f;
                t.localRotation = Quaternion.Euler(rot.x, newY, rot.z);
                Debug.Log("🔄 " + t.name + " → Thử Y=" + newY);
            }
        }
        
        EditorUtility.SetDirty(c400);
        Debug.Log("🔄 Đã xoay thêm 180° trục Y. Nếu ghế vẫn sai, bấm lại nút này để thử thêm.");
    }
}

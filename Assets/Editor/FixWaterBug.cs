using UnityEngine;
using UnityEditor;

public class FixWaterBug
{
    [MenuItem("Tools/Sửa Lỗi Mất Nước (Fix Water)")]
    public static void FixWater()
    {
        int count = 0;
        // Tìm tất cả các vật thể trên bản đồ
        MeshRenderer[] allMeshes = Object.FindObjectsOfType<MeshRenderer>();
        
        foreach (MeshRenderer mesh in allMeshes)
        {
            GameObject obj = mesh.gameObject;
            string name = obj.name.ToLower();
            
            // Tìm những vật thể có chữ water, ocean, plane, lake, sea, river
            if (name.Contains("water") || name.Contains("ocean") || name.Contains("lake") || name.Contains("sea") || name.Contains("river") || name.Contains("surface"))
            {
                // Gỡ bỏ trạng thái Static
                obj.isStatic = false;
                count++;
                Debug.Log("Đã sửa lỗi mặt nước: " + obj.name);
            }
        }
        
        if (count > 0)
        {
            Debug.Log("<b><color=green>[Thành Công]</color></b> Đã gỡ lỗi Static cho " + count + " mặt nước! Hãy bấm Play để xem kết quả.");
        }
        else
        {
            Debug.Log("<b><color=yellow>[Cảnh Báo]</color></b> Không tìm thấy mặt nước nào có tên chứa chữ Water. Bạn hãy click thủ công nhé.");
        }
    }
}

using UnityEngine;
using UnityEditor;

public class AutoStaticBatcher
{
    [MenuItem("Tools/Tự Động Đánh Dấu Static (Tối Ưu)")]
    public static void AutoMarkStatic()
    {
        int count = 0;
        // Tìm tất cả các vật thể có chứa lưới hình ảnh (MeshRenderer) trên bản đồ
        MeshRenderer[] allMeshes = Object.FindObjectsOfType<MeshRenderer>();
        
        foreach (MeshRenderer mesh in allMeshes)
        {
            GameObject obj = mesh.gameObject;
            
            // QUAN TRỌNG: Loại bỏ những thứ CÓ THỂ DI CHUYỂN
            // Nếu vật thể có Rigidbody (vật lý) thì bỏ qua
            if (obj.GetComponent<Rigidbody>() != null || obj.name.ToLower().Contains("water")) 
            {
                continue;
            }

            // Dùng try-catch để an toàn tuyệt đối với các Tag chưa được khai báo
            try
            {
                if (obj.CompareTag("Player") || obj.CompareTag("Enemy") || obj.CompareTag("Loot"))
                {
                    continue;
                }
            }
            catch { }

            // Đánh dấu vật thể là Static (Giúp Unity tự gộp Batching và Occlusion Culling)
            obj.isStatic = true;
            count++;
        }
        
        Debug.Log("<b><color=green>[Tối Ưu Xong]</color></b> Đã tự động đánh dấu " + count + " vật thể tĩnh (Cây cối, nhà cửa, đất đá) thành Static thành công! Bây giờ bạn có thể đi Bake Occlusion Culling được rồi.");
    }
}

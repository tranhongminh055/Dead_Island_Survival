using UnityEngine;
using UnityEditor;

public class MovePlayerToLand
{
    // Thêm một nút chức năng vào menu Tools của Unity
    [MenuItem("Tools/Dịch Chuyển Player Vào Bờ")]
    static void TeleportPlayerToLand()
    {
        // Tìm object tên là Player
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("Không tìm thấy object 'Player'.");
            return;
        }

        // Thử tìm điểm Spawn có sẵn trên bờ (Point_1)
        GameObject spawnPoint = GameObject.Find("Point_1");
        if (spawnPoint != null)
        {
            player.transform.position = spawnPoint.transform.position;
            Selection.activeGameObject = player; // Tự động chọn lại Player để bạn dễ nhìn
            Debug.Log("Đã thành công dịch chuyển Player tới tọa độ của Point_1 trên đất liền!");
            return;
        }

        // Nếu không có Point_1, thử tìm căn nhà sát bờ biển
        GameObject villa = GameObject.Find("Villa2_Deco_Handrail1_Mid_A (5)");
        if (villa != null)
        {
            player.transform.position = villa.transform.position;
            Selection.activeGameObject = player;
            Debug.Log("Đã thành công dịch chuyển Player tới khu vực căn nhà trên đất liền!");
            return;
        }

        Debug.LogWarning("Không tìm thấy điểm đến an toàn nào. Vui lòng kéo thủ công hoặc đổi tên điểm đến thành 'Point_1'.");
    }
}

using UnityEngine;
using UnityEditor;
using HorrorGame.Player;
using HorrorGame.Cutscenes;

public class SetFlyingC400 : EditorWindow
{
    [MenuItem("Horror Game/☁️ Đưa C400 Lên Trời & Cho Player Đi Lại")]
    public static void SetFlyingState()
    {
        // 1. Tìm máy bay C400
        GameObject c400 = GameObject.Find("[C400_AIRPLANE_CABIN]");
        if (c400 == null)
        {
            // Nếu không tìm thấy tên chuẩn, thử tìm dựa theo object đang chọn
            if (Selection.activeGameObject != null)
            {
                c400 = Selection.activeGameObject;
            }
            else
            {
                Debug.LogError("❌ Không tìm thấy máy bay C400. Vui lòng click chọn máy bay C400 trong Hierarchy rồi thử lại!");
                return;
            }
        }

        // 2. Đưa máy bay lên độ cao 1000m (Trạng thái đang bay)
        Undo.RecordObject(c400.transform, "Move C400 to Sky");
        c400.transform.position = new Vector3(0, 1000f, 0);
        // Xoay máy bay thẳng thớm để dễ đi lại
        c400.transform.rotation = Quaternion.identity; 

        // 3. Tìm Player và đưa vào trong khoang
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            Undo.RecordObject(player.transform, "Teleport Player to C400");
            // Đặt Player vào giữa máy bay, cao hơn sàn một chút (2 mét) để không bị rơi xuyên sàn
            player.transform.position = c400.transform.position + new Vector3(0, 2f, 0);
            
            // QUAN TRỌNG: Script AirplaneCrashCutscene cũ sẽ "trói" người chơi vào ghế ngay khi mới vào game.
            // Vì bạn muốn "đi lại tự do" trong khoang, ta phải tắt cái Cutscene đó đi!
            AirplaneCrashCutscene cutscene = FindObjectOfType<AirplaneCrashCutscene>();
            if (cutscene != null)
            {
                Undo.RecordObject(cutscene, "Disable Cutscene for Free Walk");
                cutscene.enabled = false;
            }
            
            // Đảm bảo bật lại script di chuyển cho Player
            player.enabled = true;
            
            Selection.activeGameObject = player.gameObject;
            Debug.Log("✅ Đã đưa máy bay lên trời (độ cao 1000m) và đặt Player vào trong! Bạn có thể ấn nút Play để tự do đi lại trong khoang máy bay.");
        }
        else
        {
            Debug.LogError("❌ Không tìm thấy Player trong Scene!");
        }
    }
}

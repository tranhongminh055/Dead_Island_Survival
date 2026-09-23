using UnityEngine;
using System.Collections;

public class AutoAddAirplaneColliders : MonoBehaviour
{
    // Cờ này giúp Unity tự động chạy hàm này ngay khi load xong Scene (bắt đầu chơi game)
    // Bạn KHÔNG CẦN phải kéo thả script này vào bất kỳ object nào trong Hierarchy cả!
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttachColliders()
    {
        // Tạo một GameObject ẩn để chạy Coroutine quét sau 1 khoảng thời gian
        GameObject runner = new GameObject("AirplaneColliderAutoFixer");
        runner.AddComponent<AutoAddAirplaneColliders>();
        DontDestroyOnLoad(runner);
    }

    IEnumerator Start()
    {
        // Chờ 1-2 giây để đảm bảo các script Cutscene (như AirplaneCrashCutscene) đã Instantiate xong máy bay
        yield return new WaitForSeconds(1.5f);

        int addedCount = 0;
        
        // Tìm toàn bộ các vật thể (kể cả đang bị ẩn / Inactive)
        MeshFilter[] allMeshes = Resources.FindObjectsOfTypeAll<MeshFilter>();
        
        foreach (MeshFilter mf in allMeshes)
        {
            // Chỉ gắn cho các object thực sự nằm trong Scene (bỏ qua Prefab trong thư mục Project)
            if (mf.gameObject.scene.IsValid())
            {
                if (mf.gameObject.GetComponent<Collider>() == null)
                {
                    // Các vật thể bị lật ngược (Scale âm) sẽ gây ra lỗi đỏ "mesh to be marked as readable" 
                    // khi bị ép gắn MeshCollider. Ta sẽ dùng BoxCollider thay thế cho chúng để vừa an toàn vừa nhẹ.
                    Vector3 s = mf.transform.lossyScale;
                    if (s.x < 0 || s.y < 0 || s.z < 0)
                    {
                        mf.gameObject.AddComponent<BoxCollider>();
                        continue;
                    }

                    try
                    {
                        mf.gameObject.AddComponent<MeshCollider>();
                        addedCount++;
                    }
                    catch (System.Exception)
                    {
                        // Bỏ qua lỗi Read/Write Enabled trên các mesh bị scale âm
                    }
                    // Tránh giật màn hình: cứ thêm 20 cái collider thì cho Unity nghỉ 1 frame để vẽ hình
                    // Lệnh yield không được đặt bên trong khối try-catch
                    if (addedCount > 0 && addedCount % 20 == 0) yield return null;
                }
            }
        }

        // Hỗ trợ thêm cho các vật thể dạng SkinnedMeshRenderer (đôi khi vỏ máy bay dùng dạng này)
        SkinnedMeshRenderer[] allSkinned = Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer smr in allSkinned)
        {
            if (smr.gameObject.scene.IsValid())
            {
                if (smr.gameObject.GetComponent<Collider>() == null)
                {
                    Vector3 s = smr.transform.lossyScale;
                    if (s.x < 0 || s.y < 0 || s.z < 0)
                    {
                        smr.gameObject.AddComponent<BoxCollider>();
                        continue;
                    }

                    try
                    {
                        MeshCollider mc = smr.gameObject.AddComponent<MeshCollider>();
                        if (smr.sharedMesh != null) mc.sharedMesh = smr.sharedMesh;
                        addedCount++;
                    }
                    catch (System.Exception)
                    {
                        // Bỏ qua lỗi
                    }
                    if (addedCount > 0 && addedCount % 20 == 0) yield return null;
                }
            }
        }

        if (addedCount > 0)
        {
            Debug.Log("🛡️ [Auto Fix] Đã tự động gắn " + addedCount + " MeshCollider cho các vật thể (kể cả ẩn/mới sinh ra)!");
        }
        
        // Hủy object chạy ngầm này sau khi hoàn thành
        Destroy(gameObject);
    }
}

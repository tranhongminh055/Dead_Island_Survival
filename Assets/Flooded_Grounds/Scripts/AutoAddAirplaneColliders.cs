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
            if (mf.gameObject.scene.IsValid())
            {
                if (mf.gameObject.GetComponent<Collider>() == null && mf.sharedMesh != null)
                {
                    // Kiểm tra Scale âm hoặc Mesh không Readable: Dùng BoxCollider để tránh lỗi console
                    Vector3 s = mf.transform.lossyScale;
                    bool hasNegativeScale = (s.x < 0 || s.y < 0 || s.z < 0);

                    if (hasNegativeScale || !mf.sharedMesh.isReadable)
                    {
                        mf.gameObject.AddComponent<BoxCollider>();
                        addedCount++;
                    }
                    else
                    {
                        try
                        {
                            mf.gameObject.AddComponent<MeshCollider>();
                            addedCount++;
                        }
                        catch (System.Exception)
                        {
                            mf.gameObject.AddComponent<BoxCollider>();
                            addedCount++;
                        }
                    }

                    // Tránh giật màn hình: cứ thêm 20 cái collider thì cho Unity nghỉ 1 frame
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
                if (smr.gameObject.GetComponent<Collider>() == null && smr.sharedMesh != null)
                {
                    Vector3 s = smr.transform.lossyScale;
                    bool hasNegativeScale = (s.x < 0 || s.y < 0 || s.z < 0);

                    if (hasNegativeScale || !smr.sharedMesh.isReadable)
                    {
                        smr.gameObject.AddComponent<BoxCollider>();
                        addedCount++;
                    }
                    else
                    {
                        try
                        {
                            MeshCollider mc = smr.gameObject.AddComponent<MeshCollider>();
                            if (smr.sharedMesh != null) mc.sharedMesh = smr.sharedMesh;
                            addedCount++;
                        }
                        catch (System.Exception)
                        {
                            smr.gameObject.AddComponent<BoxCollider>();
                            addedCount++;
                        }
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

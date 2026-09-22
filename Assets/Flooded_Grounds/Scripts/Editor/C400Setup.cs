using UnityEngine;
using UnityEditor;
using HorrorGame.Cutscenes;

public class C400Setup : EditorWindow
{
    [MenuItem("Horror Game/✈️ Thay Thế Bằng Máy Bay C400 Xịn")]
    public static void ReplaceWithC400()
    {
        // 1. Luôn đưa máy bay lên bầu trời (Y=1000) để không đâm xuyên xuống nhà cửa dưới đất
        Vector3 spawnPos = new Vector3(0f, 1000f, 0f); // Vị trí trên bầu trời
        Quaternion spawnRot = Quaternion.identity;
        
        GameObject oldSite = GameObject.Find("[AIRPLANE_CRASH_SITE]");
        if (oldSite != null)
        {
            DestroyImmediate(oldSite);
        }

        GameObject oldCabin = GameObject.Find("Temp_AirplaneCabin");
        if (oldCabin != null)
        {
            DestroyImmediate(oldCabin);
        }
        
        GameObject realisticCabin = GameObject.Find("Realistic_Airplane_Cabin");
        if (realisticCabin != null) DestroyImmediate(realisticCabin);

        // 2. Tải model C400 từ thư mục Assets
        string c400Path = "Assets/Flooded_Grounds/c-400/source/c-400.fbx";
        GameObject c400Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(c400Path);
        
        if (c400Prefab == null)
        {
            Debug.LogError("❌ Không tìm thấy model C400 tại đường dẫn: " + c400Path + "\nUnity có thể đang tải (Import), vui lòng đợi 1 chút rồi bấm lại!");
            return;
        }

        // 3. Đưa C400 vào Scene
        GameObject c400Instance = (GameObject)PrefabUtility.InstantiatePrefab(c400Prefab);
        c400Instance.name = "[C400_AIRPLANE_CABIN]";
        c400Instance.transform.position = spawnPos;
        c400Instance.transform.rotation = spawnRot;
        
        // Điều chỉnh lại Scale nếu model quá nhỏ/lớn (C400 thường cần scale x100 hoặc tùy định dạng FBX)
        // Đã sửa lại scale x10 để máy bay hiển thị to đúng kích thước thật (x100 bị quá to)
        c400Instance.transform.localScale = Vector3.one * 10f;

        // 4. Cập nhật lại Cutscene Manager để nó nhận diện máy bay mới
        AirplaneCrashCutscene manager = FindObjectOfType<AirplaneCrashCutscene>();
        if (manager != null)
        {
            Undo.RecordObject(manager, "Update C400 Airplane Cabin");
            manager.airplaneCabin = c400Instance;
            EditorUtility.SetDirty(manager);
            Debug.Log("✅ Đã cập nhật C400 vào script AirplaneCrashCutscene thành công!");
        }

        // 5. Đưa ngay Player vào trong chính giữa C400 để người dùng thấy ngay
        HorrorGame.Player.PlayerController player = FindObjectOfType<HorrorGame.Player.PlayerController>();
        if (player != null)
        {
            Undo.RecordObject(player.transform, "Move Player");
            Renderer[] renderers = c400Instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                
                // Gắn tạm MeshCollider để dò tìm mặt sàn chính xác (tránh bị rớt xuống bụng máy bay)
                System.Collections.Generic.List<MeshCollider> tempColliders = new System.Collections.Generic.List<MeshCollider>();
                foreach(var r in renderers) {
                    if (r.gameObject.GetComponent<Collider>() == null) {
                        tempColliders.Add(r.gameObject.AddComponent<MeshCollider>());
                    }
                }
                
                Vector3 centerFloor = bounds.center;
                // Bắn tia từ giữa máy bay hướng thẳng xuống để tìm mặt sàn
                RaycastHit hit;
                if (Physics.Raycast(bounds.center, Vector3.down, out hit, 1000f)) {
                    centerFloor.y = hit.point.y + 0.1f;
                } else {
                    centerFloor.y = c400Instance.transform.position.y; // Fallback
                }
                
                player.transform.position = centerFloor;
                
                // Xoá MeshCollider tạm
                foreach(var mc in tempColliders) {
                    Undo.DestroyObjectImmediate(mc);
                }
            }
            else
            {
                player.transform.position = spawnPos;
            }

            // Chỉnh dáng đứng thẳng cho Player ngay trong Scene
            HorrorGame.Cutscenes.AirplanePassengerSeatPose seatPose = player.GetComponent<HorrorGame.Cutscenes.AirplanePassengerSeatPose>();
            if (seatPose != null)
            {
                Undo.RecordObject(seatPose, "Stand Up");
                seatPose.StandUp();
            }
        }

        // Lựa chọn C400 để người dùng dễ nhìn thấy
        Selection.activeGameObject = c400Instance;
        
        Debug.Log("🎉 ĐÃ THAY THẾ TOÀN BỘ MÁY BAY CŨ BẰNG C400 THÀNH CÔNG!");
    }

    [MenuItem("Horror Game/🎬 Bật Lại Trailer Điện Ảnh (Fix Màn Hình Đen)")]
    public static void EnableCutsceneAgain()
    {
        AirplaneCrashCutscene cutscene = null;
        AirplaneCrashCutscene[] allCutscenes = Resources.FindObjectsOfTypeAll<AirplaneCrashCutscene>();
        foreach (var c in allCutscenes)
        {
            if (c.hideFlags == HideFlags.None)
            {
                cutscene = c;
                break;
            }
        }

        if (cutscene != null)
        {
            Undo.RecordObject(cutscene, "Enable Cutscene");
            cutscene.enabled = true;

            // Đưa C400 lên độ cao 1000m để đúng với bối cảnh máy bay đang bay
            GameObject c400 = GameObject.Find("[C400_AIRPLANE_CABIN]");
            if (c400 != null)
            {
                c400.transform.position = new Vector3(0, 1000f, 0);
            }

            Debug.Log("✅ Đã BẬT LẠI hệ thống Cutscene Trailer! Bạn hãy ấn Play để trải nghiệm!");
        }
        else
        {
            Debug.LogError("❌ Không tìm thấy script AirplaneCrashCutscene trong Scene. Bạn hãy mở Scene có sẵn Cutscene ra nhé!");
        }
    }
}

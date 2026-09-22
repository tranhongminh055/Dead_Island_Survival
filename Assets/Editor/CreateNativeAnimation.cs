using UnityEngine;
using UnityEditor;

public class CreateNativeAnimation : EditorWindow
{
    [MenuItem("Tools/Làm Thổ Dân Tự Đi (Scene 3)")]
    public static void CreateAnim()
    {
        GameObject native = GameObject.Find("Tho_Dan_Gia");
        if (native == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy diễn viên Tho_Dan_Gia trong Scene. Hãy tạo hắn trước nhé!", "OK");
            return;
        }

        // Tạo một đoạn phim Animation Clip mới
        AnimationClip clip = new AnimationClip();
        clip.name = "ThoDanDiChuyen";

        // Thổ dân đang quay mặt sang phải, nên hướng đi thẳng của hắn là trục Z (local)
        float startZ = native.transform.localPosition.z;
        float endZ = startZ + 15f; // Đi tới 15 mét

        // Tạo quỹ đạo chuyển động mượt mà trong 4 giây (từ giây 0 đến giây 4)
        AnimationCurve curveZ = AnimationCurve.Linear(0f, startZ, 4f, endZ);
        
        // Gắn quỹ đạo này vào thuộc tính m_LocalPosition.z
        clip.SetCurve("", typeof(Transform), "localPosition.z", curveZ);

        // Lưu file Animation này ra thư mục Assets
        string path = "Assets/ThoDan_Walk.anim";
        AssetDatabase.CreateAsset(clip, path);
        AssetDatabase.SaveAssets();

        // Tự động tìm và chọn file vừa tạo để người dùng thấy ngay
        Object animAsset = AssetDatabase.LoadAssetAtPath<Object>(path);
        Selection.activeObject = animAsset;

        Debug.Log("Đã tự động tạo ra cuộn phim ThoDan_Walk.anim! Hãy kéo nó vào Timeline.");
    }
}

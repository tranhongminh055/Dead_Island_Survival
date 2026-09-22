using UnityEngine;
using UnityEditor;

public class CreateNativePlaceholder : EditorWindow
{
    [MenuItem("Tools/Tạo Thổ Dân Giả (Scene 3)")]
    public static void CreatePlaceholder()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            // Thử tìm camera đầu tiên trong scene nếu không có MainCamera
            cam = FindObjectOfType<Camera>();
        }

        if (cam == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Camera nào trong cảnh để định vị!", "OK");
            return;
        }

        // 1. Tạo Capsule đóng vai Thổ dân
        GameObject native = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        native.name = "Tho_Dan_Gia";
        
        // Căn chỉnh vị trí: Cách camera 5 mét về phía trước, lệch sang trái 4 mét
        Vector3 spawnPos = cam.transform.position + (cam.transform.forward * 6f) + (cam.transform.right * -4f);
        
        // Tính toán để đặt nó nằm ngang tầm mắt (hơi thấp xuống một chút)
        spawnPos.y = cam.transform.position.y - 0.5f; 
        native.transform.position = spawnPos;

        // Xoay nó hướng mặt sang phải màn hình (để chuẩn bị đi ngang qua)
        native.transform.rotation = Quaternion.LookRotation(cam.transform.right);

        // Đổi màu cho dễ nhìn (tạo vật liệu tạm màu xám đậm)
        Renderer nativeRend = native.GetComponent<Renderer>();
        if (nativeRend != null)
        {
            Shader standardShader = Shader.Find("Standard");
            if (standardShader == null) standardShader = Shader.Find("Diffuse"); // Fallback
            
            if (standardShader != null)
            {
                Material mat = new Material(standardShader);
                mat.color = new Color(0.2f, 0.2f, 0.2f); // Xám đen rùng rợn
                nativeRend.material = mat;
            }
        }

        // 2. Tạo Cube đóng vai Timmy (đứa bé bị bế)
        GameObject timmy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        timmy.name = "Timmy_Gia";
        
        // Thu nhỏ đứa bé lại (bằng 1/2 người lớn)
        timmy.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        
        // Đặt làm con của Thổ dân
        timmy.transform.SetParent(native.transform);
        
        // Chỉnh vị trí nằm trên tay thổ dân (phía trước ngực)
        timmy.transform.localPosition = new Vector3(0f, 0.2f, 0.6f);

        // Tô màu cam cho đứa bé nổi bật
        Renderer timmyRend = timmy.GetComponent<Renderer>();
        if (timmyRend != null)
        {
            Shader standardShader = Shader.Find("Standard");
            if (standardShader == null) standardShader = Shader.Find("Diffuse"); // Fallback
            
            if (standardShader != null)
            {
                Material mat2 = new Material(standardShader);
                mat2.color = new Color(0.8f, 0.4f, 0.1f);
                timmyRend.material = mat2;
            }
        }

        // 3. Tự động chọn (Select) đối tượng vừa tạo để người dùng dễ thấy
        Selection.activeGameObject = native;

        Debug.Log("Đã tự động tạo và đặt Thổ dân giả ngay trước mắt Camera!");
    }
}

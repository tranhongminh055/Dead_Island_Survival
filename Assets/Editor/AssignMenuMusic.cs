using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Script tiện ích: Tự động gán file nhạc nền kinh dị từ thư mục "sound main menu" 
/// vào GameMenuManager trong tất cả các scene (MainMenu + Scene_A).
/// </summary>
public class AssignMenuMusic : Editor
{
    [MenuItem("🎮 Game Menu/🎵 Gán Nhạc Nền Menu (sound main menu)")]
    public static void AssignMusic()
    {
        // Tìm file nhạc trong thư mục "sound main menu"
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Flooded_Grounds/sound main menu" });
        
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("❌ Không tìm thấy nhạc!", 
                "Không có file .mp3/.wav/.ogg nào trong thư mục:\nAssets/Flooded_Grounds/sound main menu/\n\nHãy bỏ file nhạc vào thư mục đó trước.", "OK");
            return;
        }

        string musicPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(musicPath);

        if (clip == null)
        {
            EditorUtility.DisplayDialog("❌ Lỗi!", "Không load được AudioClip từ:\n" + musicPath, "OK");
            return;
        }

        // Tìm tất cả GameMenuManager trong scene hiện tại
        var managers = Object.FindObjectsOfType<HorrorGame.UI.GameMenuManager>();
        int count = 0;

        foreach (var mgr in managers)
        {
            mgr.menuMusic = clip;
            EditorUtility.SetDirty(mgr);
            count++;
            Debug.Log("🎵 Đã gán nhạc '" + clip.name + "' vào GameMenuManager trên GameObject: " + mgr.gameObject.name);
        }

        if (count > 0)
        {
            // Lưu scene
            EditorSceneManager.SaveOpenScenes();
            EditorUtility.DisplayDialog("✅ Thành công!", 
                "Đã gán nhạc nền:\n\"" + clip.name + "\"\n\nvào " + count + " GameMenuManager trong scene hiện tại.\n\nBấm Play để nghe thử!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("⚠️ Không tìm thấy GameMenuManager!", 
                "Không có GameMenuManager nào trong scene đang mở.\n\nHãy mở scene MainMenu hoặc Scene_A trước.", "OK");
        }
    }

    // Tự động gán vào cả 2 scene (MainMenu + Scene_A)
    [MenuItem("🎮 Game Menu/🎵 Gán Nhạc Vào TẤT CẢ Scene")]
    public static void AssignMusicToAllScenes()
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Flooded_Grounds/sound main menu" });
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("❌ Không tìm thấy nhạc!", 
                "Không có file nhạc nào trong thư mục:\nAssets/Flooded_Grounds/sound main menu/", "OK");
            return;
        }

        string musicPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(musicPath);
        if (clip == null) return;

        int totalAssigned = 0;
        string[] scenePaths = new string[] {
            "Assets/Flooded_Grounds/Scenes/MainMenu.unity",
            "Assets/Flooded_Grounds/Scenes/Scene_A.unity"
        };

        foreach (string scenePath in scenePaths)
        {
            if (!System.IO.File.Exists(scenePath.Replace("/", "\\"))) continue;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            
            // Tìm GameMenuManager trong scene vừa mở
            foreach (GameObject rootObj in scene.GetRootGameObjects())
            {
                var mgr = rootObj.GetComponent<HorrorGame.UI.GameMenuManager>();
                if (mgr != null)
                {
                    mgr.menuMusic = clip;
                    EditorUtility.SetDirty(mgr);
                    totalAssigned++;
                    Debug.Log("🎵 [" + scenePath + "] Đã gán nhạc '" + clip.name + "' vào " + rootObj.name);
                }
            }

            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.CloseScene(scene, true);
        }

        EditorUtility.DisplayDialog("✅ Hoàn tất!", 
            "Đã gán nhạc \"" + clip.name + "\" vào " + totalAssigned + " GameMenuManager trong tất cả scene.", "OK");
    }
}

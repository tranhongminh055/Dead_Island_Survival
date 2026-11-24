#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using HorrorGame.Cutscenes;
using HorrorGame.Player;

public class IntroSetupWizard : EditorWindow
{
    [MenuItem("Horror Game/Auto Setup Airplane Intro")]
    public static void ShowWindow()
    {
        SetupIntroScene();
    }

    private static void SetupIntroScene()
    {
        // 1. TÌM PLAYER & CAMERA
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("Không tìm thấy PlayerController trong Scene! Vui lòng kéo Player vào Scene trước.");
            return;
        }
        
        Camera playerCam = player.GetComponentInChildren<Camera>();
        if (playerCam == null) playerCam = Camera.main;

        // Thêm Camera Shake
        CameraShake shake = playerCam.GetComponent<CameraShake>();
        if (shake == null) shake = playerCam.gameObject.AddComponent<CameraShake>();

        // 2. TẠO MÀN HÌNH ĐEN (UI)
        GameObject canvasObj = new GameObject("IntroCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject blackScreen = new GameObject("BlackScreenUI");
        blackScreen.transform.SetParent(canvasObj.transform, false);
        Image blackImg = blackScreen.AddComponent<Image>();
        blackImg.color = Color.black;
        
        RectTransform rect = blackImg.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // 3. TẠO MÁY BAY GIẢ LẬP TẠM THỜI (CAO TRÊN TRỜI)
        GameObject airplaneObj = new GameObject("Temp_AirplaneCabin");
        airplaneObj.transform.position = new Vector3(0, 500f, 0); // Đưa lên cao 500m

        // Sàn máy bay
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.SetParent(airplaneObj.transform);
        floor.transform.localPosition = new Vector3(0, -1f, 0);
        floor.transform.localScale = new Vector3(10f, 0.5f, 20f);
        floor.GetComponent<Renderer>().sharedMaterial.color = Color.gray;

        // Tường trái phải
        GameObject wallL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallL.transform.SetParent(airplaneObj.transform);
        wallL.transform.localPosition = new Vector3(-5f, 2f, 0);
        wallL.transform.localScale = new Vector3(0.5f, 6f, 20f);
        GameObject wallR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallR.transform.SetParent(airplaneObj.transform);
        wallR.transform.localPosition = new Vector3(5f, 2f, 0);
        wallR.transform.localScale = new Vector3(0.5f, 6f, 20f);

        // Trần
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(airplaneObj.transform);
        roof.transform.localPosition = new Vector3(0, 5f, 0);
        roof.transform.localScale = new Vector3(10f, 0.5f, 20f);

        // Đèn đỏ (3 cái)
        Light[] lights = new Light[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject lObj = new GameObject("WarningLight_" + i);
            lObj.transform.SetParent(airplaneObj.transform);
            lObj.transform.localPosition = new Vector3(0, 4f, -5f + (i * 5f));
            Light l = lObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = Color.red;
            l.intensity = 3f;
            l.range = 10f;
            lights[i] = l;
        }

        // 4. DI CHUYỂN PLAYER LÊN MÁY BAY
        Vector3 forestPos = player.transform.position; // Lưu lại vị trí cũ trong rừng
        Quaternion forestRot = player.transform.rotation;
        
        player.transform.position = airplaneObj.transform.position + new Vector3(0, 0, -5f);
        
        // Tạo Spawn Point
        GameObject spawnObj = new GameObject("ForestSpawnPoint");
        spawnObj.transform.position = forestPos;
        spawnObj.transform.rotation = forestRot;

        // 5. TẠO CUTSCENE MANAGER
        GameObject managerObj = new GameObject("IntroCutsceneManager");
        AirplaneCrashCutscene manager = managerObj.AddComponent<AirplaneCrashCutscene>();
        
        // Tự động Gán dữ liệu
        manager.blackScreenUI = blackImg;
        manager.playerController = player;
        manager.playerWeapon = player.GetComponentInChildren<FPSWeapon>();
        if (manager.playerWeapon == null) manager.playerWeapon = player.GetComponentInChildren<Gun>();
        manager.cameraShake = shake;
        manager.airplaneCabin = airplaneObj;
        manager.warningLights = lights;
        manager.forestSpawnPoint = spawnObj.transform;

        Selection.activeGameObject = managerObj;
        Debug.Log("🎉 HOÀN TẤT TỰ ĐỘNG THIẾT LẬP INTRO MÁY BAY! Bạn chỉ cần thêm File Âm Thanh vào Inspector của IntroCutsceneManager nữa là xong!");
    }
}
#endif

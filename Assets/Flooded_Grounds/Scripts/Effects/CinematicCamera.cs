using UnityEngine;

public class CinematicCamera : MonoBehaviour
{
    [Header("Độ cao camera (tính từ mặt đất)")]
    public float highAltitude = 40f;     // Bay cao (toàn cảnh)
    public float midAltitude = 15f;      // Bay trung bình (ngang ngọn cây)
    public float lowAltitude = 4f;       // Bay thấp (sát mặt đất)

    [Header("Thời gian mỗi cảnh (giây)")]
    public float sceneDuration = 10f;

    [Header("Hiệu ứng")]
    public float fadeSpeed = 2f;

    // Các thông số sẽ được TỰ ĐỘNG tính toán
    private Vector3 mapCenter;
    private float mapSize;

    [Header("Tuyệt chiêu máy yếu: Quay chậm (Slow-mo)")]
    [Tooltip("Bật cái này nếu máy quá lag. Game sẽ chạy chậm lại 5 lần. Bạn quay video xong, mang vào CapCut x5 tốc độ lên là mượt như máy tính xịn!")]
    public bool dungChieuQuayCham = true;
    
    // --- Private ---
    private int currentScene = 0;
    private int totalScenes = 7;
    private float sceneTimer = 0f;
    private float angle = 0f;
    private bool isFinished = false;

    private Texture2D fadeTexture;
    private float fadeAlpha = 1f;
    private bool fadingIn = true;
    private bool fadingOut = false;

    private Vector3 randomOffset;
    private float randomAngleStart;

    private string[] sceneNames = new string[]
    {
        "Toàn cảnh hòn đảo từ trên cao",
        "Lướt sát mặt đất qua khu rừng",
        "Bay chậm giữa các tòa nhà",
        "Lia camera từ mặt đất lên bầu trời",
        "Bay lướt qua ngọn cây",
        "Tiến thẳng vào khu vực tối",
        "Xoáy ốc hạ cánh xuống đảo"
    };

    void Start()
    {
        QualitySettings.vSyncCount = 0; 
        Application.targetFrameRate = 30;

        // Nếu bật tuyệt chiêu quay chậm, thời gian trong game sẽ trôi chậm đi 5 lần
        if (dungChieuQuayCham)
        {
            Time.timeScale = 0.2f; 
        }

        fadeTexture = new Texture2D(1, 1);
        fadeTexture.SetPixel(0, 0, Color.black);
        fadeTexture.Apply();

        fadeAlpha = 1f;
        fadingIn = true;

        AutoFindTerrain();
        GenerateRandomOffset();

        Debug.Log("=== BẮT ĐẦU QUAY TRAILER ===");
        Debug.Log("Đã tự động tìm thấy trung tâm đảo tại: " + mapCenter + " | Kích thước: " + mapSize);
        Debug.Log("Cảnh 1/" + totalScenes + ": " + sceneNames[0]);
    }

    // TỰ ĐỘNG TÌM HÒN ĐẢO VÀ TÍNH TỌA ĐỘ
    private void AutoFindTerrain()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            Vector3 tSize = terrain.terrainData.size;
            Vector3 tPos = terrain.transform.position;

            // Tâm của bản đồ theo X và Z
            float centerX = tPos.x + (tSize.x / 2f);
            float centerZ = tPos.z + (tSize.z / 2f);

            // Tìm chiều cao của mặt đất tại tâm đảo
            float centerY = terrain.SampleHeight(new Vector3(centerX, 0, centerZ)) + tPos.y;

            mapCenter = new Vector3(centerX, centerY, centerZ);
            mapSize = Mathf.Min(tSize.x, tSize.z) * 0.35f; // Lấy 35% kích thước đảo làm vùng quay
        }
        else
        {
            Debug.LogWarning("Không tìm thấy Terrain! Dùng tọa độ mặc định.");
            mapCenter = new Vector3(500f, 16f, 450f);
            mapSize = 150f;
        }
    }

    void Update()
    {
        if (isFinished) return;

        sceneTimer += Time.deltaTime;

        if (fadingIn)
        {
            fadeAlpha -= Time.deltaTime * fadeSpeed;
            if (fadeAlpha <= 0f) { fadeAlpha = 0f; fadingIn = false; }
        }

        if (fadingOut)
        {
            fadeAlpha += Time.deltaTime * fadeSpeed;
            if (fadeAlpha >= 1f)
            {
                fadeAlpha = 1f;
                fadingOut = false;

                if (currentScene >= totalScenes - 1)
                {
                    isFinished = true;
                    Debug.Log("=== TRAILER HOÀN TẤT! Bấm Windows+Alt+R để dừng quay ===");
                    return;
                }
                NextScene();
            }
            return; // Khong update camera khi dang fade out
        }

        float fadeOutTime = 1f / fadeSpeed;
        if (sceneTimer >= sceneDuration - fadeOutTime && !fadingOut)
        {
            fadingOut = true;
        }

        switch (currentScene)
        {
            case 0: Scene_AerialPanorama(); break;
            case 1: Scene_GroundSweep(); break;
            case 2: Scene_SlowDolly(); break;
            case 3: Scene_CraneUp(); break;
            case 4: Scene_TreetopFlyover(); break;
            case 5: Scene_PushForward(); break;
            case 6: Scene_SpiralLanding(); break;
        }
    }

    private void NextScene()
    {
        currentScene++;
        sceneTimer = 0f;
        angle = 0f;
        fadingIn = true;
        fadeAlpha = 1f;

        GenerateRandomOffset();
        Debug.Log("Cảnh " + (currentScene + 1) + "/" + totalScenes + ": " + sceneNames[currentScene]);
    }

    private void GenerateRandomOffset()
    {
        randomOffset = new Vector3(
            Random.Range(-mapSize * 0.4f, mapSize * 0.4f),
            0,
            Random.Range(-mapSize * 0.4f, mapSize * 0.4f)
        );
        
        // Đảm bảo offset có chiều cao đúng với mặt đất tại điểm đó
        Terrain t = Terrain.activeTerrain;
        if (t != null)
        {
            Vector3 checkPos = mapCenter + randomOffset;
            float gHeight = t.SampleHeight(checkPos) + t.transform.position.y;
            randomOffset.y = gHeight - mapCenter.y;
        }
        
        randomAngleStart = Random.Range(0f, 360f);
    }

    private float GetGroundHeight(Vector3 pos)
    {
        Terrain t = Terrain.activeTerrain;
        if (t != null)
        {
            return t.SampleHeight(pos) + t.transform.position.y;
        }
        return mapCenter.y;
    }

    // ==========================================
    // CÁC CẢNH QUAY
    // ==========================================

    private void Scene_AerialPanorama()
    {
        angle = randomAngleStart + sceneTimer * 6f;
        float radius = mapSize * 0.8f;

        float x = mapCenter.x + Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
        float z = mapCenter.z + Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
        float y = GetGroundHeight(new Vector3(x, 0, z)) + highAltitude;

        Vector3 targetPos = new Vector3(x, y, z);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 2f);
        transform.LookAt(mapCenter + Vector3.up * 10f);
    }

    private void Scene_GroundSweep()
    {
        float t = Mathf.SmoothStep(0f, 1f, sceneTimer / sceneDuration);

        Vector3 start = mapCenter + randomOffset + new Vector3(-mapSize * 0.5f, 0, 0);
        Vector3 end = mapCenter + randomOffset + new Vector3(mapSize * 0.5f, 0, 0);

        start.y = GetGroundHeight(start) + lowAltitude;
        end.y = GetGroundHeight(end) + lowAltitude;

        float bobble = Mathf.Sin(sceneTimer * 3f) * 0.5f;

        Vector3 pos = Vector3.Lerp(start, end, t);
        pos.y = GetGroundHeight(pos) + lowAltitude + bobble;

        transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * 4f);

        Vector3 lookDir = (end - start).normalized;
        lookDir.y = -0.02f;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 3f);
    }

    private void Scene_SlowDolly()
    {
        float t = Mathf.SmoothStep(0f, 1f, sceneTimer / sceneDuration);

        Vector3 start = mapCenter + randomOffset + new Vector3(0, 0, -mapSize * 0.4f);
        Vector3 end = mapCenter + randomOffset + new Vector3(0, 0, mapSize * 0.4f);

        start.y = GetGroundHeight(start) + midAltitude;
        end.y = GetGroundHeight(end) + midAltitude;

        transform.position = Vector3.Lerp(start, end, t);

        float lookAngle = Mathf.Lerp(-20f, 20f, t);
        Vector3 lookDir = Quaternion.Euler(0, lookAngle, 0) * (end - start).normalized;
        lookDir.y = -0.05f;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 2f);
    }

    private void Scene_CraneUp()
    {
        float t = Mathf.SmoothStep(0f, 1f, sceneTimer / sceneDuration);

        Vector3 basePos = mapCenter + randomOffset;
        float currentHeight = Mathf.Lerp(lowAltitude, highAltitude * 1.5f, t);
        float forwardMove = Mathf.Lerp(0, mapSize * 0.3f, t);

        Vector3 pos = basePos + new Vector3(0, 0, forwardMove);
        pos.y = GetGroundHeight(pos) + currentHeight;

        transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * 3f);

        float pitchAngle = Mathf.Lerp(15f, -10f, t);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.Euler(pitchAngle, randomAngleStart, 0),
            Time.deltaTime * 3f
        );
    }

    private void Scene_TreetopFlyover()
    {
        float t = Mathf.SmoothStep(0f, 1f, sceneTimer / sceneDuration);

        Vector3 start = mapCenter + new Vector3(-mapSize * 0.6f, 0, randomOffset.z);
        Vector3 end = mapCenter + new Vector3(mapSize * 0.6f, 0, randomOffset.z);

        start.y = GetGroundHeight(start) + midAltitude * 1.2f;
        end.y = GetGroundHeight(end) + midAltitude * 1.2f;

        float sway = Mathf.Sin(sceneTimer * 2f) * 1f;

        Vector3 pos = Vector3.Lerp(start, end, t);
        pos.y += sway;

        transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * 3f);

        Vector3 lookDir = (end - start).normalized;
        lookDir.y = -0.05f;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 3f);
    }

    private void Scene_PushForward()
    {
        float t = Mathf.SmoothStep(0f, 1f, sceneTimer / sceneDuration);

        Vector3 start = mapCenter + randomOffset + new Vector3(0, 0, -mapSize * 0.5f);
        Vector3 end = mapCenter + randomOffset + new Vector3(0, 0, mapSize * 0.1f);

        start.y = GetGroundHeight(start) + lowAltitude * 1.5f;
        end.y = GetGroundHeight(end) + lowAltitude * 1.5f;

        transform.position = Vector3.Lerp(start, end, t);

        Vector3 lookDir = (end - start).normalized;
        lookDir.y = -0.02f;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 2f);
    }

    private void Scene_SpiralLanding()
    {
        angle += 15f * Time.deltaTime;

        float progress = Mathf.Clamp01(sceneTimer / sceneDuration);
        float currentRadius = Mathf.Lerp(mapSize * 0.6f, 10f, progress);
        float currentAltitude = Mathf.Lerp(highAltitude * 1.5f, lowAltitude, progress);

        float x = mapCenter.x + Mathf.Cos(angle * Mathf.Deg2Rad) * currentRadius;
        float z = mapCenter.z + Mathf.Sin(angle * Mathf.Deg2Rad) * currentRadius;
        float groundY = GetGroundHeight(new Vector3(x, 0, z));

        Vector3 targetPos = new Vector3(x, groundY + currentAltitude, z);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 4f);
        
        Vector3 lookTarget = mapCenter;
        lookTarget.y = GetGroundHeight(lookTarget) + 2f;
        transform.LookAt(lookTarget);
    }

    void OnGUI()
    {
        if (fadeAlpha > 0.01f)
        {
            GUI.color = new Color(0, 0, 0, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeTexture);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HorrorGame.Player;
using HorrorGame.Enemy;
using HorrorGame.Survival;
using HorrorGame.Environment;

namespace HorrorGame.Cutscenes
{
    /// <summary>
    /// 🎬 HỆ THỐNG ĐẠO DIỄN QUAY TRAILER ĐIỆN ẢNH TỰ ĐỘNG (CINEMATIC TRAILER DIRECTOR)
    /// Chuẩn thời lượng hành động kịch tính (~52 giây) — Nhịp độ dồn dập phong cách The Forest x Outlast
    /// 
    /// KỊCH BẢN NÂNG CẤP HOÀN TOÀN:
    ///  HỒI 1 (0:00 – 0:07.5 | 7.5s): Lướt nhanh là là mặt đất qua xác máy bay cháy rực, lia ống kính lên tàn tích sương mù trong đêm.
    ///  HỒI 2 (0:07.5 – 0:14.0 | 6.5s): Bão sấm chớp giật lóe sáng toàn cảnh đồi núi, bầy Thổ Dân cầm đuốc & Zombie gầm rú vang đảo.
    ///  HỒI 3 (0:14.0 – 0:20.0 | 6.0s): POV mở mắt sau tai nạn (chớp mắt, thở dốc, hoảng loạn nhìn bàn tay dính máu gượng đứng dậy).
    ///  HỒI 4 (0:20.0 – 0:29.0 | 9.0s): [CÓ PLAYER] Player cầm rìu bổ gỗ xây nhà sinh tồn, dăm gỗ bay tung tóe, lều gỗ lắp ráp, lửa trại bùng cháy!
    ///  HỒI 5 (0:29.0 – 0:39.0 | 10.0s): Cuộc rượt đuổi nghẹt thở (Outlast) trong rừng đêm, đèn pin lắc dữ dội, ngoái đầu thấy Zombie sát gót!
    ///  HỒI 6 (0:39.0 – 0:47.0 | 8.0s): [GÓC TƯỜNG CÓ CẢ PLAYER & ZOMBIE] Bị dồn vào chân tường đổ nát, bầy Zombie bao vây, JUMPSCARE vồ thẳng ống kính, bẻ xương gãy rắc!
    ///  KẾT   (0:47.0 – 0:52.0 | 5.0s): Màn đen, Cinema Braam rền vang, tựa đề DEAD ISLAND SURVIVAL phát sáng đỏ rực + Coming Soon.
    /// </summary>
    public class CinematicTrailerDirector : MonoBehaviour
    {
        public static CinematicTrailerDirector Instance;
        public static bool IsTrailerRunning { get; private set; }
        public static bool playOnSceneLoaded = false;

        [Header("── Điều Khiển Trailer ──")]
        public KeyCode triggerKey = KeyCode.F9;
        public KeyCode cancelKey = KeyCode.Escape;
        public bool autoPlayOnStart = false;

        [Header("── Tự Động Trích Xuất Video MP4 ──")]
        [Tooltip("Tự động biên dịch và xuất file video MP4 chất lượng cao sau khi quay xong")]
        public bool autoExportMp4 = true;
        [Tooltip("Số khung hình mỗi giây khi xuất video (30fps tiêu chuẩn)")]
        public int recordingFps = 30;
        private string tempFramesDir;
        private string finalMp4Path;
        private int capturedFrameIndex = 0;
        private bool isRecordingFrames = false;
        private bool isCompilingMp4 = false;

        [Header("── Âm Thanh Trailer ──")]
        public AudioClip boneSnapClip;
        public AudioClip screamAgonyClip;
        public AudioClip warCryClip;
        public AudioClip chopWoodClip;
        public AudioClip thunderClip;
        public AudioClip heartbeatClip;
        public AudioClip crawlStruggleClip;

        // References hệ thống game
        private PlayerController player;
        private Camera mainCamera;
        private Camera trailerCamera;
        private GameObject trailerCamObj;
        private AudioSource trailerAudioSource;
        private AudioSource musicAudioSource;

        // Visual & UI State
        private int currentSceneIndex = 0;
        private string currentSubtitle = "";
        private float subtitleAlpha = 0f;
        private float letterboxHeight = 0f;
        private float targetLetterboxHeight = 0f;
        private float blinkEyeAlpha = 0f;
        private float bloodSplatterAlpha = 0f;
        private float titleCardAlpha = 0f;
        private float blackFadeAlpha = 0f;
        private float cameraShakeTrauma = 0f;

        // Dữ liệu Actor & Scene props tạm thời
        private List<GameObject> spawnedTrailerProps = new List<GameObject>();
        private List<GameObject> chaseEnemies = new List<GameObject>();

        // Textures giao diện dựng bằng code (0 dependencies)
        private Texture2D blackTex;
        private Texture2D whiteTex;
        private Texture2D bloodTex;
        private GUIStyle subtitleStyle;
        private GUIStyle subtitleShadowStyle;
        private GUIStyle titleMainStyle;
        private GUIStyle titleSubStyle;

        // Tọa độ GỢI Ý ban đầu cho hiện trường tai nạn máy bay (sẽ được quét terrain tự động tìm giữa sông thật)
        private Vector3 crashSite = new Vector3(500f, 15.0f, 540f);
        // Tọa độ BỜ SÔNG GẦN NHẤT (đất liền) — dùng cho cảnh player tỉnh dậy ven sông
        private Vector3 shoreSite;
        // Tọa độ KHU CẮM TRẠI ĐẤT KHÔ CAO RÁO TUYỆT ĐỐI — dùng cho cảnh chặt cây, xây nhà, rượt đuổi
        private Vector3 campSite;

        // Lưu trữ trạng thái UI/HUD để khôi phục sau trailer
        private List<Canvas> hiddenCanvases = new List<Canvas>();
        private List<MonoBehaviour> disabledHudScripts = new List<MonoBehaviour>();
        private List<GameObject> hiddenObstacles = new List<GameObject>();

        // Thiết lập ánh sáng ban đêm
        private float origDayHour = 12f;
        private Color origAmbientLight;
        private UnityEngine.Rendering.AmbientMode origAmbientMode;
        private bool origFog;
        private FogMode origFogMode;
        private float origFogDensity;
        private Color origFogColor;
        private float origSunIntensity = 1f;
        private GameObject trailerNightLightObj;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            if (FindObjectOfType<CinematicTrailerDirector>() == null)
            {
                GameObject obj = new GameObject("[CinematicTrailerDirector]");
                obj.AddComponent<CinematicTrailerDirector>();
                DontDestroyOnLoad(obj);
            }
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(this);
                return;
            }

            tempFramesDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Trailer_Frames"));
            finalMp4Path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Trailer_DeadIslandSurvival.mp4"));

            CreateProceduralTextures();
        }

        void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (scene.name == "Scene_A" && playOnSceneLoaded)
            {
                playOnSceneLoaded = false;
                StartCoroutine(DelayedStartTrailerAfterLoad());
            }
        }

        private IEnumerator DelayedStartTrailerAfterLoad()
        {
            yield return new WaitForSeconds(0.35f);
            StartTrailer();
        }

        void Start()
        {
            FindSceneReferences();
            LoadAudioAssets();

            if (autoPlayOnStart)
            {
                StartCoroutine(StartTrailerRoutine());
            }
        }

        void Update()
        {
            // Phím tắt F9 để kích hoạt Trailer
            if (Input.GetKeyDown(triggerKey) && !IsTrailerRunning)
            {
                StartTrailer();
            }

            // Phím ESC để dừng trailer ngay lập tức
            if (Input.GetKeyDown(cancelKey) && IsTrailerRunning)
            {
                StopTrailer();
            }

            // Cập nhật rung chấn máy quay
            if (cameraShakeTrauma > 0f)
            {
                cameraShakeTrauma = Mathf.Max(0f, cameraShakeTrauma - Time.deltaTime * 1.2f);
            }
        }

        private void FindSceneReferences()
        {
            if (player == null) player = FindObjectOfType<PlayerController>();
            if (mainCamera == null) mainCamera = Camera.main;
        }

        private void LoadAudioAssets()
        {
#if UNITY_EDITOR
            if (boneSnapClip == null)
                boneSnapClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Resources/Fracture/bone_snap_crack.wav");
            if (boneSnapClip == null)
                boneSnapClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/âm thanh khi gãy xương/âm thanh khi gãy xương.wav");

            if (screamAgonyClip == null)
                screamAgonyClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Resources/Fracture/fracture_scream_agony.wav");
            if (screamAgonyClip == null)
                screamAgonyClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/âm thanh khi gãy chân/âm thanh khi gãy chân.wav");

            if (warCryClip == null)
                warCryClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/Cannibal/Cannibal_WarCry.mp3");
            if (warCryClip == null)
                warCryClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/tiếng sói hú/ribhavagrawal-wolf-howling-type-04-293317.mp3");

            if (chopWoodClip == null)
                chopWoodClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/sound axe chop tree/wings_of_freedom-chopping-wood-435769.mp3");

            if (thunderClip == null)
                thunderClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/Weather/Thunder_Close_1.wav");

            if (heartbeatClip == null)
                heartbeatClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Flight_Cabin-Sound/heartbeat_slow.wav");

            if (crawlStruggleClip == null)
                crawlStruggleClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Flight_Cabin-Sound/player_crawl_struggle.mp3");
#endif
            if (boneSnapClip == null) boneSnapClip = Resources.Load<AudioClip>("Fracture/bone_snap_crack");
            if (screamAgonyClip == null) screamAgonyClip = Resources.Load<AudioClip>("Fracture/fracture_scream_agony");
        }

        private void CreateProceduralTextures()
        {
            blackTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            blackTex.SetPixel(0, 0, Color.black);
            blackTex.Apply();

            whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();

            bloodTex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            Color clearRed = new Color(0.6f, 0f, 0f, 0f);
            for (int y = 0; y < 256; y++)
            {
                for (int x = 0; x < 256; x++)
                {
                    bloodTex.SetPixel(x, y, clearRed);
                }
            }

            Random.InitState(42);
            for (int s = 0; s < 40; s++)
            {
                int cx = Random.Range(20, 236);
                int cy = Random.Range(20, 236);
                int r = Random.Range(8, 35);
                float dark = Random.Range(0.45f, 0.85f);
                Color bloodCol = new Color(dark, 0.02f, 0.02f, Random.Range(0.75f, 0.98f));

                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (dx * dx + dy * dy <= r * r)
                        {
                            int px = cx + dx;
                            int py = cy + dy;
                            if (px >= 0 && px < 256 && py >= 0 && py < 256)
                            {
                                bloodTex.SetPixel(px, py, bloodCol);
                            }
                        }
                    }
                }
            }
            bloodTex.Apply();
        }

        public void StartTrailer()
        {
            if (IsTrailerRunning) return;

            // Nếu đang ở MainMenu, tự động chuyển vào Scene_A (nơi có hòn đảo, máy bay, rừng rậm) rồi mới quay!
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Scene_A")
            {
                playOnSceneLoaded = true;
                UnityEngine.SceneManagement.SceneManager.LoadScene("Scene_A");
                Debug.Log("🎬 [CinematicTrailerDirector] Đang tự động chuyển từ MainMenu sang Scene_A để quay hòn đảo...");
                return;
            }

            StartCoroutine(StartTrailerRoutine());
        }

        public void StopTrailer()
        {
            StopAllCoroutines();
            isRecordingFrames = false;
            isCompilingMp4 = false;
            Time.captureFramerate = 0;
            IsTrailerRunning = false;

            RestoreOriginalAtmosphere();

            if (player != null)
            {
                player.enabled = true;
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = true;
            }

            if (mainCamera != null)
            {
                mainCamera.enabled = true;
                AudioListener mainLis = mainCamera.GetComponent<AudioListener>();
                if (mainLis != null) mainLis.enabled = true;
            }
            else
            {
                Camera c = Camera.main;
                if (c != null)
                {
                    c.enabled = true;
                    AudioListener cLis = c.GetComponent<AudioListener>();
                    if (cLis != null) cLis.enabled = true;
                }
            }

            if (trailerCamObj != null) Destroy(trailerCamObj);

            CleanupTrailerProps();

            letterboxHeight = 0f;
            targetLetterboxHeight = 0f;
            blackFadeAlpha = 0f;
            bloodSplatterAlpha = 0f;
            blinkEyeAlpha = 0f;
            titleCardAlpha = 0f;
            currentSubtitle = "";

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Debug.Log("🎬 [CinematicTrailerDirector] Đã dừng Trailer và khôi phục game!");
        }

        private void SetupCinematicNightAtmosphere()
        {
            AirplaneCrashCutscene[] allCrashCutscenes = FindObjectsOfType<AirplaneCrashCutscene>();
            foreach (var acc in allCrashCutscenes)
            {
                if (acc != null)
                {
                    acc.StopAllCoroutines();
                    acc.enabled = false;
                    AudioSource[] audios = acc.GetComponentsInChildren<AudioSource>();
                    foreach (var a in audios) if (a != null) a.Stop();
                }
            }

            GameObject subUI = GameObject.Find("Cutscene_Subtitle_UI");
            if (subUI != null) subUI.SetActive(false);

            if (HorrorGame.UI.GameMenuManager.Instance != null)
            {
                HorrorGame.UI.GameMenuManager.Instance.enabled = false;
            }

            // ẨN TOÀN BỘ THANH CHỈ SỐ UI/HUD TRONG TRAILER CINEMATIC
            HideAllGameplayHUD();

            GameObject cabin = GameObject.Find("AirplaneCabin");
            if (cabin != null)
            {
                AudioSource[] cabinAudios = cabin.GetComponentsInChildren<AudioSource>();
                foreach (var a in cabinAudios) { if (a != null) { a.Stop(); a.enabled = false; } }
                cabin.SetActive(false);
            }

            origAmbientMode = RenderSettings.ambientMode;
            origAmbientLight = RenderSettings.ambientLight;
            origFog = RenderSettings.fog;
            origFogMode = RenderSettings.fogMode;
            origFogDensity = RenderSettings.fogDensity;
            origFogColor = RenderSettings.fogColor;

            if (HorrorGame.Environment.DayNightCycle.Instance != null)
            {
                origDayHour = HorrorGame.Environment.DayNightCycle.Instance.currentHour;
                HorrorGame.Environment.DayNightCycle.Instance.currentHour = 22.8f;

                if (HorrorGame.Environment.DayNightCycle.Instance.sunLight != null)
                {
                    origSunIntensity = HorrorGame.Environment.DayNightCycle.Instance.sunLight.intensity;
                    HorrorGame.Environment.DayNightCycle.Instance.sunLight.intensity = 0.01f;
                }

                if (HorrorGame.Environment.DayNightCycle.Instance.MoonLight != null)
                {
                    HorrorGame.Environment.DayNightCycle.Instance.MoonLight.intensity = 0.28f;
                    HorrorGame.Environment.DayNightCycle.Instance.MoonLight.color = new Color(0.38f, 0.52f, 0.85f);
                }
            }

            if (trailerNightLightObj == null)
            {
                trailerNightLightObj = new GameObject("[Trailer_Night_Moonlight]");
                Light moonL = trailerNightLightObj.AddComponent<Light>();
                moonL.type = LightType.Directional;
                moonL.color = new Color(0.32f, 0.45f, 0.72f);
                moonL.intensity = 0.26f;
                trailerNightLightObj.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.04f, 0.05f, 0.08f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.011f;
            RenderSettings.fogColor = new Color(0.015f, 0.022f, 0.04f);

            if (HorrorGame.Environment.Weather.WeatherSystem.Instance != null)
            {
                HorrorGame.Environment.Weather.WeatherSystem.Instance.SetWeather(HorrorGame.Environment.Weather.WeatherType.Thunderstorm, 0.5f);
            }

            HideAnyGameplayCorpses();
        }

        private void RestoreOriginalAtmosphere()
        {
            RestoreGameplayCorpses();

            if (trailerNightLightObj != null) Destroy(trailerNightLightObj);

            if (HorrorGame.UI.GameMenuManager.Instance != null)
            {
                HorrorGame.UI.GameMenuManager.Instance.enabled = true;
            }

            // KHÔI PHỤC TOÀN BỘ UI/HUD SAU KHI TRAILER KẾT THÚC
            RestoreAllGameplayHUD();

            if (HorrorGame.Environment.DayNightCycle.Instance != null)
            {
                HorrorGame.Environment.DayNightCycle.Instance.currentHour = origDayHour;
                if (HorrorGame.Environment.DayNightCycle.Instance.sunLight != null)
                {
                    HorrorGame.Environment.DayNightCycle.Instance.sunLight.intensity = origSunIntensity;
                }
            }

            RenderSettings.ambientMode = origAmbientMode;
            RenderSettings.ambientLight = origAmbientLight;
            RenderSettings.fog = origFog;
            RenderSettings.fogMode = origFogMode;
            RenderSettings.fogDensity = origFogDensity;
            RenderSettings.fogColor = origFogColor;
        }

        private IEnumerator StartTrailerRoutine()
        {
            IsTrailerRunning = true;
            FindSceneReferences();
            CleanupTrailerProps();

            SetupCinematicNightAtmosphere();

            if (player != null)
            {
                player.enabled = false;
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
            }

            if (autoExportMp4)
            {
                try
                {
                    if (Directory.Exists(tempFramesDir))
                    {
                        Directory.Delete(tempFramesDir, true);
                    }
                    Directory.CreateDirectory(tempFramesDir);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("⚠️ Không thể dọn/tạo thư mục Trailer_Frames: " + ex.Message);
                }

                Time.captureFramerate = recordingFps;
                capturedFrameIndex = 0;
                isRecordingFrames = true;
                StartCoroutine(CaptureFramesRoutine());
            }

            if (trailerCamObj == null)
            {
                trailerCamObj = new GameObject("[Trailer_Cinematic_Camera]");
                trailerCamera = trailerCamObj.AddComponent<Camera>();
                trailerCamera.nearClipPlane = 0.25f;
                trailerCamera.farClipPlane = 1000f;
                trailerCamera.fieldOfView = 65f;
                trailerCamera.depth = 999f;

                // Gắn AudioListener cho trailer camera để nghe chuẩn 3D spatial và lọc âm dưới nước
                AudioListener listener = trailerCamObj.GetComponent<AudioListener>();
                if (listener == null) listener = trailerCamObj.AddComponent<AudioListener>();

                // Gắn hiệu ứng lặn nước chân thực (UnderwaterVisualManager) cho trailer camera
                HorrorGame.Effects.UnderwaterVisualManager uvm = trailerCamObj.GetComponent<HorrorGame.Effects.UnderwaterVisualManager>();
                if (uvm == null) uvm = trailerCamObj.AddComponent<HorrorGame.Effects.UnderwaterVisualManager>();
                uvm.waterHeight = 16.0f;

                trailerAudioSource = trailerCamObj.AddComponent<AudioSource>();
                trailerAudioSource.spatialBlend = 0f;
                trailerAudioSource.priority = 0;

                musicAudioSource = trailerCamObj.AddComponent<AudioSource>();
                musicAudioSource.spatialBlend = 0f;
                musicAudioSource.loop = true;
                musicAudioSource.volume = 0.85f;
            }

            AudioListener[] allListeners = FindObjectsOfType<AudioListener>();
            foreach (var l in allListeners)
            {
                if (l != null && l.gameObject != trailerCamObj) l.enabled = false;
            }

            Camera[] allCams = FindObjectsOfType<Camera>();
            foreach (var cam in allCams)
            {
                if (cam != trailerCamera) cam.enabled = false;
            }

            trailerCamera.enabled = true;
            targetLetterboxHeight = Screen.height * 0.12f;

            Debug.Log("🎬 [CinematicTrailerDirector] ══ BẮT ĐẦU TRAILER NHANH & KỊCH TÍNH (~52 GIÂY) ══");

            // ═════════════════════════════════════════════════════════════════════
            // HỒI 1 (7.5s): HIỆN TRƯỜNG TAI NẠN & TÀN TÍCH BỎ HOANG
            // ═════════════════════════════════════════════════════════════════════
            yield return StartCoroutine(PlayScene1_CrashSiteAndRuins());

            // ═════════════════════════════════════════════════════════════════════
            // HỒI 2 (6.5s): ĐẢO QUỶ TRỖI DẬY — BẦY THỔ DÂN & ZOMBIE
            // ═════════════════════════════════════════════════════════════════════
            yield return StartCoroutine(PlayScene2_MonstersInfestation());

            // ═════════════════════════════════════════════════════════════════════
            // HỒI 3 (6.0s): TỈNH DẬY TRONG MÊ MAN (POV AWAKENING)
            // ═════════════════════════════════════════════════════════════════════
            yield return StartCoroutine(PlayScene3_FirstPersonAwakening());

            // ═════════════════════════════════════════════════════════════════════
            // HỒI 4 (9.0s): CÓ PLAYER CẦM RÌU BỔ GỖ XÂY DỰNG SINH TỒN
            // ═════════════════════════════════════════════════════════════════════
            yield return StartCoroutine(PlayScene4_SurvivalBuildingWithPlayer());

            // ═════════════════════════════════════════════════════════════════════
            // HỒI 5 (10.0s): CUỘC RƯỢT ĐUỔI KINH HOÀNG (OUTLAST STYLE)
            // ═════════════════════════════════════════════════════════════════════
            yield return StartCoroutine(PlayScene5_NightPursuitChase());

            // ═════════════════════════════════════════════════════════════════════
            // HỒI 6 (8.0s): DỒN VÀO GÓC TƯỜNG (CẢ PLAYER & ZOMBIE ĐỀU RÕ RÀNG) & JUMPSCARE
            // ═════════════════════════════════════════════════════════════════════
            yield return StartCoroutine(PlayScene6_CorneredAndJumpscare());

            // ═════════════════════════════════════════════════════════════════════
            // OUTRO (5.0s): TỰA ĐỀ PHÁT SÁNG & COMING SOON
            // ═════════════════════════════════════════════════════════════════════
            yield return StartCoroutine(PlayOutro_TitleCardReveal());

            if (autoExportMp4)
            {
                isRecordingFrames = false;
                Time.captureFramerate = 0;
                yield return StartCoroutine(CompileMp4Routine());
            }

            StopTrailer();
        }

        #region SCENE 1: CRASH SITE & RUINS (7.5s - Nhanh, Điện Ảnh)
        private IEnumerator PlayScene1_CrashSiteAndRuins()
        {
            currentSceneIndex = 1;
            blackFadeAlpha = 1.0f;

            // QUÉT TERRAIN TỰ ĐỘNG TÌM GIỮA SÔNG THẬT (không dùng tọa độ cố định nữa)
            crashSite = FindOpenWaterCrashSite(crashSite);
            // TÌM BỜ SÔNG GẦN NHẤT (đất liền) cho cảnh player tỉnh dậy ven sông
            shoreSite = FindNearestShorePosition(crashSite);
            // TÌM VÙNG ĐẤT KHÔ CAO RÁO TUYỆT ĐỐI cho cảnh sinh tồn, chặt cây, rượt đuổi (tuyệt đối không ở dưới nước)
            campSite = FindDryLandCampSite(shoreSite, crashSite);
            Debug.Log("🎬 [CinematicTrailerDirector] Máy bay GIỮA SÔNG: " + crashSite + " | Bờ sông: " + shoreSite + " | Khu cắm trại ĐẤT KHÔ: " + campSite);
            HideAnyGameplayCorpses();

            // 1. Xuất hiện chiếc máy bay C-400 khổng lồ bị đâm rơi bốc cháy ngùn ngụt trên mặt đầm lầy
            SpawnCrashedAirplaneAtSite(crashSite);
            SpawnRealisticCrashCasualties();

            // 2. Góc máy bay lượn TRÊN MẶT NƯỚC SÔNG lia dọc theo thân chiếc máy bay khổng lồ đang bốc cháy ngùn ngụt
            Vector3 startPos = SnapToGround(crashSite + new Vector3(16f, 0f, -16f), 3.2f);
            Vector3 midPos = SnapToGround(crashSite + new Vector3(8f, 0f, 8f), 4.8f);
            Vector3 endPos = SnapToGround(crashSite + new Vector3(-18f, 0f, 24f), 9.5f);

            Vector3 scene1Focus = crashSite + new Vector3(0f, 2.0f, 0f);
            trailerCamObj.transform.position = ResolveCameraCollision(startPos, scene1Focus, 0.4f);
            trailerCamObj.transform.rotation = Quaternion.Euler(14f, -40f, 0f);

            SetSubtitle("FLIGHT 404... CRASH-LANDED ON THE DEAD ISLAND.", 4.0f);

            float t = 0f;
            float dur = 7.5f;

            while (t < dur)
            {
                t += Time.deltaTime;
                float p = t / dur;

                // Mở nhanh màn đen trong 1.2s đầu
                if (t < 1.2f) blackFadeAlpha = Mathf.Lerp(1f, 0f, t / 1.2f);

                if (p < 0.55f)
                {
                    // Lướt camera TRÊN MẶT NƯỚC sát thân và cánh máy bay đang cháy khói nghi ngút
                    float subP = p / 0.55f;
                    Vector3 desired = Vector3.Lerp(startPos, midPos, Mathf.SmoothStep(0f, 1f, subP));
                    trailerCamObj.transform.position = ResolveCameraCollision(desired, scene1Focus, 0.4f);
                    trailerCamObj.transform.LookAt(scene1Focus);
                }
                else
                {
                    // Nâng vút lên cao quay toàn cảnh đuôi máy bay bốc cháy trên dòng sông và sương mù rừng cây
                    float subP = (p - 0.55f) / 0.45f;
                    Vector3 desired = Vector3.Lerp(midPos, endPos, Mathf.SmoothStep(0f, 1f, subP));
                    Vector3 lookTarget = endPos + new Vector3(35f, 6f, 45f);
                    trailerCamObj.transform.position = ResolveCameraCollision(desired, lookTarget, 0.4f);
                    trailerCamObj.transform.rotation = Quaternion.Slerp(trailerCamObj.transform.rotation, Quaternion.LookRotation(lookTarget - trailerCamObj.transform.position), Time.deltaTime * 3f);
                }

                ApplyProceduralCameraShake(0.14f);
                yield return null;
            }

            yield return StartCoroutine(QuickFadeTransition(0.35f));
        }
        #endregion

        #region SCENE 2: MONSTERS & CANNIBAL HORDE (6.5s - Kịch Tính & Chớp Sét)
        private IEnumerator PlayScene2_MonstersInfestation()
        {
            currentSceneIndex = 2;

            SpawnHordeActors();

            Vector3 flyStart = crashSite + new Vector3(-35f, 12f, -25f);
            Vector3 flyEnd = crashSite + new Vector3(-15f, 8f, 35f);

            trailerCamObj.transform.position = flyStart;
            trailerCamObj.transform.rotation = Quaternion.Euler(15f, 45f, 0f);

            SetSubtitle("THEY ARE NOT ALONE ON THIS ISLAND.", 3.6f);

            if (warCryClip != null && trailerAudioSource != null)
            {
                trailerAudioSource.PlayOneShot(warCryClip, 0.95f);
            }

            float t = 0f;
            float dur = 6.5f;

            while (t < dur)
            {
                t += Time.deltaTime;
                float p = t / dur;

                trailerCamObj.transform.position = Vector3.Lerp(flyStart, flyEnd, p);
                trailerCamObj.transform.LookAt(crashSite + new Vector3(20f, 2f, 0f));

                // Chớp sét giật chói chang ở giây 2.8s
                if (t >= 2.8f && t <= 3.05f)
                {
                    blackFadeAlpha = -0.7f;
                    if (thunderClip != null && trailerAudioSource != null && t <= 2.85f)
                    {
                        trailerAudioSource.PlayOneShot(thunderClip, 1.0f);
                        cameraShakeTrauma = 0.8f;
                    }
                }
                else if (blackFadeAlpha < 0f)
                {
                    blackFadeAlpha = Mathf.MoveTowards(blackFadeAlpha, 0f, Time.deltaTime * 4f);
                }

                yield return null;
            }

            yield return StartCoroutine(QuickFadeTransition(0.35f));
        }
        #endregion

        #region SCENE 3: POV AWAKENING (6.0s - Nhịp Nhanh, Thở Dốc, BÀN TAY DÍNH MÁU THẬT 100%)
        private IEnumerator PlayScene3_FirstPersonAwakening()
        {
            currentSceneIndex = 3;

            // Người sống sót tỉnh dậy TRÊN BỜ CẠN VEN SÔNG (đất liền), nhìn ra xác máy bay đang cháy rực giữa dòng sông
            Vector3 surfaceRestPos = SnapToGround(shoreSite, 0.55f);
            // Camera nhìn hướng về phía xác máy bay giữa sông
            Vector3 lookDir = (crashSite - shoreSite);
            lookDir.y = 0f;
            float lookYaw = (lookDir.sqrMagnitude > 0.1f) ? Quaternion.LookRotation(lookDir).eulerAngles.y : 65f;

            trailerCamObj.transform.position = surfaceRestPos;
            trailerCamObj.transform.rotation = Quaternion.Euler(-18f, lookYaw, 12f);

            SetSubtitle("ONLY ONE SURVIVOR...", 2.8f);

            if (heartbeatClip != null && trailerAudioSource != null)
                trailerAudioSource.PlayOneShot(heartbeatClip, 1.0f);

            if (crawlStruggleClip != null && trailerAudioSource != null)
                trailerAudioSource.PlayOneShot(crawlStruggleClip, 0.9f);

            // BÀN TAY CỦA CHÍNH PLAYER (MODEL CH15 MẶC ÁO CHIẾN THUẬT RẰN RI, GĂNG TAY ĐÃ ĐƯỢC GẮN VÀO CAMERA POV)
            GameObject playerHandsObj = CreateFirstPersonPlayerHands(trailerCamObj.transform);
            spawnedTrailerProps.Add(playerHandsObj);

            blinkEyeAlpha = 1.0f;
            float t = 0f;
            float dur = 6.0f;

            while (t < dur)
            {
                t += Time.deltaTime;

                // 2 nhịp chớp mắt dứt khoát
                if (t < 1.0f) blinkEyeAlpha = Mathf.Lerp(1.0f, 0.3f, t / 1.0f);
                else if (t < 1.6f) blinkEyeAlpha = Mathf.Lerp(0.3f, 0.9f, (t - 1.0f) / 0.6f);
                else if (t < 2.5f) blinkEyeAlpha = Mathf.Lerp(0.9f, 0.0f, (t - 1.6f) / 0.9f);
                else blinkEyeAlpha = 0f;

                // Giây 2.4s: Phụ đề bàng hoàng khi nhìn thấy bàn tay dính đầy máu của chính mình
                if (t >= 2.4f && t <= 2.55f)
                {
                    SetSubtitle("BLOOD... MY OWN HANDS...", 2.8f);
                }

                // Gượng đứng phắt dậy (từ 4.0s đến 5.8s)
                if (t >= 4.0f && t <= 5.8f)
                {
                    float p = (t - 4.0f) / 1.8f;
                    float smoothP = Mathf.SmoothStep(0f, 1f, p);
                    trailerCamObj.transform.position = Vector3.Lerp(surfaceRestPos, surfaceRestPos + Vector3.up * 1.35f, smoothP);
                    trailerCamObj.transform.rotation = Quaternion.Slerp(
                        Quaternion.Euler(-18f, lookYaw, 12f),
                        Quaternion.Euler(2f, lookYaw - 30f, 0f),
                        smoothP
                    );
                }

                ApplyProceduralCameraShake(0.18f * (1f - (t / dur)));
                yield return null;
            }

            if (playerHandsObj != null) Destroy(playerHandsObj);
            yield return StartCoroutine(QuickFadeTransition(0.35f));
        }
        #endregion

        #region SCENE 4: SURVIVAL BUILDING WITH PLAYER (9.0s - CÓ PLAYER BỔ RÌU XÂY NHÀ!)
        private IEnumerator PlayScene4_SurvivalBuildingWithPlayer()
        {
            currentSceneIndex = 4;

            // Khu vực xây dựng — TRÊN ĐẤT KHÔ CAO RÁO TUYỆT ĐỐI (KHÔNG BAO GIỜ DƯỚI NƯỚC)
            Vector3 campCenter = (campSite != Vector3.zero) ? campSite : FindDryLandCampSite(shoreSite, crashSite);
            Debug.Log(string.Format("🏕️ [Scene 4] Bắt đầu cảnh cắm trại tại ĐẤT KHÔ: {0} (Cao hơn nước {1:F2}m)", campCenter, campCenter.y - 16.0f));

            // Dọn sạch vật thể cản trở xung quanh khu xây dựng (nhà, tường, đá...)
            ClearObstaclesAroundSite(campCenter, 18f);

            // 1. TỌA ĐỘ CÂY THẬT TRONG MAP & XUẤT HIỆN CÂY CỔ THỤ THẬT SỰ TRÊN ĐẤT KHÔ
            Vector3 treePos = campCenter + new Vector3(-0.6f, 0f, 0.6f);
            treePos.y = GetExactTerrainHeight(treePos);
            GameObject realTree = SpawnRealTreeToChop(treePos, Quaternion.Euler(0f, 35f, 0f));

            // 2. Player đứng đối diện thân cây thật cách 1.25m, hướng mặt thẳng vào thân cây!
            Vector3 playerPos = treePos + new Vector3(0.15f, 0f, -1.25f);
            playerPos.y = GetExactTerrainHeight(playerPos);
            Vector3 lookToTree = (treePos - playerPos);
            lookToTree.y = 0f;
            Quaternion playerRot = Quaternion.LookRotation(lookToTree.normalized);

            GameObject playerActor = SpawnPlayerActor(playerPos, playerRot);

            // 3. Gắn Rìu vào tay phải của Actor (cầm 1 tay thật sự)
            GameObject axeObj = AttachAxeToActor(playerActor);

            // 4. Tạo công trình: Cột chống trước -> Lều gỗ hoàn chỉnh -> Lửa trại TRÊN ĐẤT KHÔ
            Vector3 shelterPos = campCenter + new Vector3(2.4f, 0f, 1.2f);
            shelterPos.y = GetExactTerrainHeight(shelterPos);
            GameObject woodenShelter = BuildingPrefabGenerator.CreateLeafShelter();
            woodenShelter.name = "Trailer_LeafShelter";
            woodenShelter.transform.position = shelterPos;
            woodenShelter.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
            spawnedTrailerProps.Add(woodenShelter);

            // Tạo các thanh cột dựng tạm (Scaffolding Frame)
            GameObject scaffoldFrame = new GameObject("ScaffoldFrame");
            scaffoldFrame.transform.position = shelterPos;
            Material woodMat = GetTrailerMaterial("BLD_Cabins", new Color(0.48f, 0.32f, 0.14f));
            for (int p = 0; p < 4; p++)
            {
                GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.transform.SetParent(scaffoldFrame.transform, false);
                pole.transform.localPosition = new Vector3(-1.2f + (p % 2) * 2.4f, 1.2f, -1.2f + (p / 2) * 2.4f);
                pole.transform.localScale = new Vector3(0.12f, 1.2f, 0.12f);
                pole.GetComponent<Renderer>().sharedMaterial = woodMat;
                Destroy(pole.GetComponent<Collider>());
            }
            spawnedTrailerProps.Add(scaffoldFrame);

            // 5. Đống lửa trại TRÊN ĐẤT KHÔ
            GameObject campfire = BuildingPrefabGenerator.CreateCampfire();
            campfire.name = "Trailer_Campfire";
            Vector3 firePos = campCenter + new Vector3(-2.2f, 0f, 1.6f);
            firePos.y = GetExactTerrainHeight(firePos);
            campfire.transform.position = firePos;
            Light campFireLight = campfire.GetComponentInChildren<Light>();
            if (campFireLight != null) campFireLight.intensity = 0.5f; // Chưa bén lửa to
            spawnedTrailerProps.Add(campfire);

            // Ẩn lều chính ban đầu, chỉ hiện khung dựng tạm
            woodenShelter.SetActive(false);
            scaffoldFrame.SetActive(true);

            // 6. GẮN COMPONENT CHẶT CÂY 1 TAY THẬT SỰ CHO PLAYER
            int chopCount = 0;
            PlayerWoodChopAnimator chopAnim = playerActor.AddComponent<PlayerWoodChopAnimator>();
            chopAnim.InitBones(axeObj.transform);
            chopAnim.onChopHit = () =>
            {
                chopCount++;
                if (chopWoodClip != null && trailerAudioSource != null)
                {
                    trailerAudioSource.PlayOneShot(chopWoodClip, 1.0f);
                }

                // Dăm gỗ văng tung tóe ngay trên vỏ thân cây thật
                Vector3 impactPoint = treePos + Vector3.up * 1.20f - lookToTree.normalized * 0.35f;
                SpawnWoodChipEffect(impactPoint);
                ApplyProceduralCameraShake(0.35f);

                // Rung cây thật mỗi nhát chém
                if (realTree != null)
                {
                    StartCoroutine(ShakeTreeRoutine(realTree.transform));
                }

                // Nhát thứ 2: Hoàn thiện lều sinh tồn
                if (chopCount == 2)
                {
                    scaffoldFrame.SetActive(false);
                    woodenShelter.SetActive(true);
                }

                // Nhát thứ 3: Lửa trại bùng sáng rực rỡ
                if (chopCount >= 3 && campFireLight != null)
                {
                    campFireLight.intensity = 4.5f;
                    campFireLight.range = 16f;
                }
            };

            // 7. THIẾT LẬP GÓC MÁY QUAY ĐIỆN ẢNH TRÊN ĐẤT KHÔ:
            // Shot A (0 - 5.5s): GÓC 3/4 PROFILE BÊN PHẢI QUAY RÕ PLAYER CẦM RÌU 1 TAY VUNG BỔ VÀO THÂN CÂY THẬT
            Vector3 camShotA_Start = playerPos + new Vector3(2.4f, 0f, -0.6f);
            camShotA_Start.y = GetExactTerrainHeight(camShotA_Start) + 1.45f;
            Vector3 camShotA_End = playerPos + new Vector3(1.8f, 0f, 0.5f);
            camShotA_End.y = GetExactTerrainHeight(camShotA_End) + 1.35f;
            Vector3 lookTargetA = playerPos + new Vector3(0.08f, 1.22f, 0.72f);

            // Shot B (5.5 - 9.0s): Hero Shot góc thấp chính diện chiêm ngưỡng thành quả lều gỗ & lửa trại
            Vector3 camShotB_Start = campCenter + new Vector3(3.2f, 0f, -2.2f);
            camShotB_Start.y = GetExactTerrainHeight(camShotB_Start) + 1.1f;
            Vector3 camShotB_End = campCenter + new Vector3(2.5f, 0f, -3.0f);
            camShotB_End.y = GetExactTerrainHeight(camShotB_End) + 1.4f;

            trailerCamObj.transform.position = camShotA_Start;
            trailerCamObj.transform.LookAt(lookTargetA);

            SetSubtitle("BUILD TO SURVIVE THE NIGHT.", 4.2f);

            float t = 0f;
            float dur = 9.0f;

            while (t < dur)
            {
                t += Time.deltaTime;

                if (t < 5.5f)
                {
                    // Giai đoạn Shot A: Nhìn rõ từ góc 3/4 thấy người chơi vung rìu 1 tay chém vào thân cây thật
                    float pA = t / 5.5f;
                    Vector3 desiredA = Vector3.Lerp(camShotA_Start, camShotA_End, Mathf.SmoothStep(0f, 1f, pA));
                    trailerCamObj.transform.position = ResolveCameraCollision(desiredA, lookTargetA, 0.45f);
                    trailerCamObj.transform.LookAt(lookTargetA);
                }
                else
                {
                    // Giai đoạn Shot B: Hero shot góc thấp chiêm ngưỡng lều gỗ & đống lửa
                    float pB = (t - 5.5f) / 3.5f;
                    Vector3 desiredB = Vector3.Lerp(camShotB_Start, camShotB_End, Mathf.SmoothStep(0f, 1f, pB));
                    Vector3 heroLook = campCenter + new Vector3(0.5f, 0f, 0f);
                    heroLook.y = GetExactTerrainHeight(heroLook) + 1.2f;
                    trailerCamObj.transform.position = ResolveCameraCollision(desiredB, heroLook, 0.45f);
                    trailerCamObj.transform.LookAt(heroLook);

                    // Giây 7.2s: Tiếng cành cây gãy rắc trong rừng! Player giật mình khựng lại!
                    if (t >= 7.2f && t <= 7.35f && boneSnapClip != null && trailerAudioSource != null)
                    {
                        trailerAudioSource.PlayOneShot(boneSnapClip, 0.8f);
                        SetSubtitle("WAIT... WHAT WAS THAT?", 2.2f);
                    }
                }

                ApplyProceduralCameraShake(0.06f);
                yield return null;
            }

            yield return StartCoroutine(QuickFadeTransition(0.35f));
        }
        #endregion

        #region SCENE 5: NIGHT PURSUIT CHASE (10.0s - Rượt Đuổi Nghẹt Thở, Không Xuyên Thân Cây/Đá)
        private IEnumerator PlayScene5_NightPursuitChase()
        {
            currentSceneIndex = 5;

            // Điểm bắt đầu rượt đuổi xuyên rừng đêm — TRÊN ĐẤT KHÔ TỪ KHU TRẠI
            Vector3 campBase = (campSite != Vector3.zero) ? campSite : shoreSite;
            Vector3 chaseDir = FindSafeDryChaseDirection(campBase);
            Vector3 chaseStart = campBase + chaseDir * 6f;
            chaseStart.y = GetExactTerrainHeight(chaseStart) + 1.65f;
            Vector3 chaseEnd = chaseStart + chaseDir * 55f;
            chaseEnd.y = GetExactTerrainHeight(chaseEnd) + 1.65f;
            Vector3 chaseDirection = (chaseEnd - chaseStart);
            chaseDirection.y = 0f;
            chaseDirection = chaseDirection.normalized;

            SpawnChaseSceneActors(chaseStart, chaseDirection);

            trailerCamObj.transform.position = chaseStart;
            trailerCamObj.transform.rotation = Quaternion.LookRotation(chaseDirection);

            SetSubtitle("RUN. NEVER LOOK BACK.", 3.8f);

            float t = 0f;
            float dur = 10.0f;
            float runSpeed = 5.8f; // ~58m xuyên rừng đêm về chân tường

            while (t < dur)
            {
                t += Time.deltaTime;

                // Quét SphereCast kiểm tra chướng ngại vật phía trước (cây, đá, tàn tích) để lướt vòng qua chứ không đi xuyên
                Vector3 moveDelta = chaseDirection * runSpeed * Time.deltaTime;
                Vector3 currentCamPos = trailerCamObj.transform.position;

                RaycastHit treeHit;
                int obstacleMask = ~LayerMask.GetMask("Ignore Raycast", "TransparentFX", "Water");
                if (Physics.SphereCast(currentCamPos, 0.35f, chaseDirection, out treeHit, moveDelta.magnitude + 0.6f, obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    if (treeHit.collider != null && !treeHit.collider.name.StartsWith("Chaser_") && !treeHit.collider.name.StartsWith("Trailer_"))
                    {
                        Vector3 slideNormal = treeHit.normal;
                        slideNormal.y = 0f;
                        if (slideNormal.sqrMagnitude > 0.01f)
                        {
                            Vector3 slideDir = Vector3.ProjectOnPlane(chaseDirection, slideNormal.normalized).normalized;
                            moveDelta = slideDir * runSpeed * Time.deltaTime;
                        }
                    }
                }

                Vector3 nextHoriz = currentCamPos + moveDelta;
                Vector3 groundedCam = SnapToGround(nextHoriz, 1.65f);

                // Kiểm tra Overlap tại vị trí camera để không bao giờ bị kẹt trong thân cây/tảng đá
                Collider[] overlaps = Physics.OverlapSphere(groundedCam, 0.28f, obstacleMask, QueryTriggerInteraction.Ignore);
                foreach (var col in overlaps)
                {
                    if (col == null || col.isTrigger) continue;
                    if (col.name.StartsWith("Chaser_") || col.name.StartsWith("Trailer_")) continue;
                    Vector3 closest = col.ClosestPoint(groundedCam);
                    Vector3 push = groundedCam - closest;
                    if (push.sqrMagnitude < 0.0001f) push = -chaseDirection;
                    groundedCam = closest + push.normalized * 0.35f;
                }

                float bob = Mathf.Sin(t * 16f) * 0.12f;
                float roll = Mathf.Cos(t * 8f) * 2.8f;

                trailerCamObj.transform.position = groundedCam + Vector3.up * bob;

                // 2 LẦN NGOÁI ĐẦU NHÌN RA SAU (ở giây 3.2s và 6.8s) THẤY RÕ BẦY QUÁI
                bool isLookingBack = (t >= 3.2f && t <= 4.5f) || (t >= 6.8f && t <= 8.0f);

                if (isLookingBack)
                {
                    Quaternion lookBackRot = Quaternion.LookRotation(-chaseDirection + Vector3.up * 0.08f);
                    trailerCamObj.transform.rotation = Quaternion.Slerp(trailerCamObj.transform.rotation, lookBackRot, Time.deltaTime * 11f);
                    cameraShakeTrauma = 0.55f;
                }
                else
                {
                    Quaternion forwardRot = Quaternion.Euler(bob * 14f, Quaternion.LookRotation(chaseDirection).eulerAngles.y, roll);
                    trailerCamObj.transform.rotation = Quaternion.Slerp(trailerCamObj.transform.rotation, forwardRot, Time.deltaTime * 9f);
                }

                UpdateChasingEnemies(trailerCamObj.transform.position, chaseDirection, Time.deltaTime);
                yield return null;
            }

            yield return StartCoroutine(QuickFadeTransition(0.35f));
        }
        #endregion

        #region SCENE 6: CORNERED & JUMPSCARE (8.0s - CẢ PLAYER & ZOMBIE HIỆN RÕ, TUYỆT ĐỐI KHÔNG XUYÊN TƯỜNG!)
        private IEnumerator PlayScene6_CorneredAndJumpscare()
        {
            currentSceneIndex = 6;

            // 1. DỰNG GÓC TƯỜNG ĐỔ NÁT THẬT (RUINED CORNER WALL) TRÊN ĐẤT KHÔ
            Vector3 campBase = (campSite != Vector3.zero) ? campSite : shoreSite;
            Vector3 chaseDir = FindSafeDryChaseDirection(campBase);
            Vector3 cornerBase = campBase + chaseDir * 61f;
            cornerBase.y = GetExactTerrainHeight(cornerBase);
            Quaternion cornerRot = Quaternion.LookRotation(chaseDir);
            BuildRuinedCorner(cornerBase, cornerRot);

            // 2. SPAWN PLAYER BỊ DỒN SÁT VÁCH GÓC TƯỜNG (LƯNG HƯỚNG VÀO TRỤ GÓC, MẶT HƯỚNG RA NGOÀI SÂN)
            // Tọa độ (1.15m, 1.15m) an toàn tuyệt đối, cách xa trụ đá (0.32m) và hai bức tường (dày 0.35m), không xuyên tường
            Vector3 playerInCorner = cornerBase + cornerRot * new Vector3(1.15f, 0f, 1.15f);
            playerInCorner.y = GetExactTerrainHeight(playerInCorner);
            Quaternion playerFaceOut = cornerRot * Quaternion.Euler(0f, 45f, 0f); // Mặt nhìn thẳng ra sân mở góc 45 độ
            GameObject cornerPlayer = SpawnPlayerActor(playerInCorner, playerFaceOut);

            // Gắn rìu/vũ khí cầm thủ thế
            AttachAxeToActor(cornerPlayer);

            // 3. SPAWN 3 ZOMBIE THẬT BAO VÂY TRONG SÂN MỞ (SPEED = 0 ĐỨNG THỦ THẾ ĐE DỌA, KHÔNG CHẠY TẠI CHỖ)
            List<GameObject> cornerZombies = new List<GameObject>();
            for (int z = 0; z < 3; z++)
            {
                float spread = (z - 1) * 1.35f;
                // Zombie đứng ở sân mở phía trước Player 3.2m
                Vector3 zPos = SnapToGround(playerInCorner + (playerFaceOut * Vector3.forward * 3.2f) + (playerFaceOut * Vector3.right * spread), 0f);
                Quaternion zRot = Quaternion.LookRotation(playerInCorner - zPos);
                GameObject zombie = SpawnZombieActor(zPos, zRot, 0f); // SPEED = 0 (IDLE)!
                cornerZombies.Add(zombie);

                // Giơ hai tay đe dọa (Attack stance)
                Animator zAnim = zombie.GetComponent<Animator>();
                if (zAnim != null)
                {
                    zAnim.SetFloat("Speed", 0f);
                    zAnim.SetTrigger("Attack");
                }
            }

            SetSubtitle("NOWHERE TO RUN.", 3.2f);

            // 4. GÓC MÁY QUAY CHIA LÀM 3 SHOT ĐIỆN ẢNH RÕ NÉT CẢ HAI BÊN (KHÔNG ĐI XUYÊN BẤT KỲ BỨC TƯỜNG NÀO):
            // Shot 6A (0 - 4.2s): Góc quay trung đặt ở ngoài sân mở (4.4m phía trước Player), quay thẳng vào góc tường:
            // Thấy rõ hậu cảnh hai bức tường đá, Player lưng dựa chân tường hoảng sợ, và bầy Zombie đang khép vòng vây!
            Vector3 cam6A_Pos = playerInCorner + (playerFaceOut * Vector3.forward * 4.4f) + Vector3.up * 1.65f;

            // Shot 6B (4.2 - 6.2s): Góc quay cận mặt Player đặt cách mặt Player 1.35m phía trước (trong sân mở, hoàn toàn không xuyên tường)
            Vector3 cam6B_Pos = playerInCorner + (playerFaceOut * Vector3.forward * 1.35f) + Vector3.up * 1.45f;

            // Shot 6C (6.2 - 8.0s): POV từ góc tường -> Zombie vồ thẳng vào camera (Jumpscare)
            Vector3 cam6C_Pos = playerInCorner + Vector3.up * 1.55f;

            trailerCamObj.transform.position = ResolveCameraCollision(cam6A_Pos, playerInCorner + Vector3.up * 1.15f, 0.35f);
            trailerCamObj.transform.LookAt(playerInCorner + Vector3.up * 1.15f);

            float t = 0f;
            float dur = 8.0f;
            bool jumpscareDone = false;

            while (t < dur)
            {
                t += Time.deltaTime;

                // Zombie lừ đừ tiến lại gần góc tường, khi sát 1.8m thì dừng lại đe dọa, không đi xuyên tường hay xuyên người
                for (int i = 0; i < cornerZombies.Count; i++)
                {
                    if (cornerZombies[i] != null)
                    {
                        float distToP = Vector3.Distance(cornerZombies[i].transform.position, playerInCorner);
                        if (distToP > 1.8f)
                        {
                            cornerZombies[i].transform.position = Vector3.MoveTowards(
                                cornerZombies[i].transform.position,
                                playerInCorner,
                                Time.deltaTime * 0.35f
                            );
                        }
                        else
                        {
                            Animator za = cornerZombies[i].GetComponent<Animator>();
                            if (za != null) za.SetFloat("Speed", 0f);
                        }
                        cornerZombies[i].transform.LookAt(playerInCorner);
                    }
                }

                if (t < 4.2f)
                {
                    // Shot 6A: Trung cảnh thấy CẢ PLAYER Ở CHÂN TƯỜNG LẪN BẦY ZOMBIE
                    Vector3 desiredPos = Vector3.Lerp(cam6A_Pos, cam6A_Pos + Vector3.up * 0.3f, t / 4.2f);
                    Vector3 lookTarget = playerInCorner + Vector3.up * 1.15f;
                    trailerCamObj.transform.position = ResolveCameraCollision(desiredPos, lookTarget, 0.35f);
                    trailerCamObj.transform.LookAt(lookTarget);
                    ApplyProceduralCameraShake(0.08f);
                }
                else if (t < 6.2f)
                {
                    // Shot 6B: Cận cảnh mặt Player sợ hãi tột độ
                    float pB = (t - 4.2f) / 2.0f;
                    Vector3 desiredPos = Vector3.Lerp(cam6B_Pos, cam6B_Pos - playerFaceOut * Vector3.forward * 0.2f, pB);
                    Vector3 lookTarget = playerInCorner + Vector3.up * 1.45f;
                    trailerCamObj.transform.position = ResolveCameraCollision(desiredPos, lookTarget, 0.35f);
                    trailerCamObj.transform.LookAt(lookTarget);
                    ApplyProceduralCameraShake(0.18f);
                }
                else
                {
                    // Shot 6C: POV TỪ CHÂN TƯỜNG — JUMPSCARE VỒ THẲNG VÀO ỐNG KÍNH!
                    // Ẩn model 3D Player để tránh camera POV bị lồng vào trong sọ nhân vật
                    if (cornerPlayer != null && cornerPlayer.activeSelf)
                    {
                        cornerPlayer.SetActive(false);
                    }

                    trailerCamObj.transform.position = cam6C_Pos;
                    if (cornerZombies.Count > 0 && cornerZombies[1] != null)
                    {
                        trailerCamObj.transform.LookAt(cornerZombies[1].transform.position + Vector3.up * 1.4f);

                        // Zombie giữa vồ lao thẳng vào mặt camera
                        cornerZombies[1].transform.position = Vector3.MoveTowards(
                            cornerZombies[1].transform.position,
                            cam6C_Pos,
                            Time.deltaTime * 6.5f
                        );
                    }

                    if (!jumpscareDone)
                    {
                        jumpscareDone = true;
                        cameraShakeTrauma = 1.0f;

                        if (boneSnapClip != null && trailerAudioSource != null)
                            trailerAudioSource.PlayOneShot(boneSnapClip, 1.0f);
                        if (screamAgonyClip != null && trailerAudioSource != null)
                            trailerAudioSource.PlayOneShot(screamAgonyClip, 1.0f);

                        bloodSplatterAlpha = 1.0f;

                        // Tắt phụt màn hình sau 0.45s giãy giụa
                        yield return new WaitForSeconds(0.45f);
                        blackFadeAlpha = 1.0f;
                        break;
                    }
                }

                yield return null;
            }

            yield return new WaitForSeconds(0.8f);
        }
        #endregion

        #region OUTRO: TITLE CARD & COMING SOON (5.0s - Nhanh, Đập Mắt)
        private IEnumerator PlayOutro_TitleCardReveal()
        {
            currentSceneIndex = 7;
            blackFadeAlpha = 1.0f;
            currentSubtitle = "";

            float t = 0f;
            float dur = 5.0f;

            while (t < dur)
            {
                t += Time.deltaTime;

                if (t < 2.0f)
                {
                    titleCardAlpha = Mathf.Lerp(0f, 1f, t / 2.0f);
                }

                yield return null;
            }

            titleCardAlpha = 0f;
        }
        #endregion

        #region ACTORS & ENVIRONMENT SPAWNERS
        private GameObject SpawnPlayerActor(Vector3 position, Quaternion rotation)
        {
            GameObject playerActor = null;

#if UNITY_EDITOR
            GameObject fbx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/Character No Animation/Ch15_nonPBR.fbx");
            if (fbx != null)
            {
                playerActor = Instantiate(fbx, position, rotation);
            }
#endif
            if (playerActor == null)
            {
                GameObject inSceneSoldier = GameObject.Find("Ch15_nonPBR");
                if (inSceneSoldier != null)
                {
                    playerActor = Instantiate(inSceneSoldier, position, rotation);
                }
            }

            if (playerActor == null)
            {
                playerActor = CreateProceduralHumanoid("Trailer_Player_Actor", position, rotation, new Color(0.25f, 0.35f, 0.45f));
            }
            else
            {
                playerActor.name = "Trailer_Player_Actor";
                playerActor.transform.position = position;
                playerActor.transform.rotation = rotation;
                playerActor.transform.localScale = Vector3.one;

#if UNITY_EDITOR
                Material mat1 = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Flooded_Grounds/Character No Animation/Materials/Ch15_1001_Diffuse.mat");
                Material mat2 = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Flooded_Grounds/Character No Animation/Materials/Ch15_1002_Diffuse.mat");
                SkinnedMeshRenderer[] smrs = playerActor.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var smr in smrs)
                {
                    if (mat1 != null && mat2 != null)
                    {
                        smr.sharedMaterials = new Material[] { mat1, mat2 };
                    }
                }

                RuntimeAnimatorController animCtrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Flooded_Grounds/Character No Animation/Animation Character/PlayerAnim.controller");
                Animator anim = playerActor.GetComponent<Animator>();
                if (anim == null) anim = playerActor.AddComponent<Animator>();
                if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;
                anim.applyRootMotion = false;
#endif
            }

            Collider[] cols = playerActor.GetComponentsInChildren<Collider>();
            foreach (var c in cols) c.enabled = false;

            spawnedTrailerProps.Add(playerActor);
            return playerActor;
        }

        private GameObject AttachAxeToActor(GameObject actor)
        {
            Transform rightHand = FindDeepChild(actor.transform, "RightHand");
            Transform parentTransform = (rightHand != null) ? rightHand : actor.transform;

            GameObject axeRig = new GameObject("Actor_Axe");
            axeRig.transform.SetParent(parentTransform, false);

            if (rightHand != null)
            {
                // Căn chỉnh trục chính xác vào trong lòng bàn tay phải
                axeRig.transform.localPosition = new Vector3(0.015f, 0.08f, 0.035f);
                axeRig.transform.localRotation = Quaternion.Euler(0f, 90f, -90f);
                axeRig.transform.localScale = Vector3.one;

                // Nếu actor chưa có WoodChopAnimator thì gắn bộ giữ ngón tay nắm chặt cán rìu
                if (actor.GetComponent<PlayerWoodChopAnimator>() == null && actor.GetComponent<ActorAxeHoldAnimator>() == null)
                {
                    actor.AddComponent<ActorAxeHoldAnimator>();
                }
            }
            else
            {
                axeRig.transform.localPosition = new Vector3(0.05f, 1.25f, 0.35f);
                axeRig.transform.localRotation = Quaternion.identity;
            }

            GameObject axeModel = null;
#if UNITY_EDITOR
            GameObject axePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/Prefabs/Axe/simple-axe/source/Axe/axe.obj");
            if (axePrefab != null)
            {
                axeModel = Instantiate(axePrefab, axeRig.transform);
                // Căn chỉnh trục axe.obj: cán dọc theo Y (tay cầm ở gốc Y=0), lưỡi quay về phía trước +Z
                axeModel.transform.localPosition = new Vector3(0f, -0.06f, 0f);
                axeModel.transform.localRotation = Quaternion.identity;
                axeModel.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);

                Material axeMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Flooded_Grounds/Prefabs/Axe/simple-axe/source/Axe/Materials/Axe.mat");
                Texture2D albedo = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Flooded_Grounds/Prefabs/Axe/simple-axe/source/Axe/Textures/axe_Axe_BaseColor.png");
                Texture2D normal = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Flooded_Grounds/Prefabs/Axe/simple-axe/source/Axe/Textures/axe_Axe_Normal.png");
                if (axeMat != null)
                {
                    if (albedo != null) axeMat.mainTexture = albedo;
                    if (normal != null) axeMat.SetTexture("_BumpMap", normal);
                    Renderer[] rens = axeModel.GetComponentsInChildren<Renderer>();
                    foreach (var r in rens) r.sharedMaterial = axeMat;
                }
            }
#endif
            if (axeModel == null)
            {
                axeModel = CreateProceduralAxe(axeRig.transform);
                axeModel.transform.localPosition = Vector3.zero;
                axeModel.transform.localRotation = Quaternion.identity;
            }

            return axeRig;
        }

        private GameObject SpawnZombieActor(Vector3 position, Quaternion rotation, float animSpeed = 0f)
        {
            GameObject zombieActor = null;

#if UNITY_EDITOR
            GameObject fbx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/Character No Animation/Enemy No Animation/Zombiegirl W Kurniawan.fbx");
            if (fbx != null)
            {
                zombieActor = Instantiate(fbx, position, rotation);
            }
#endif
            if (zombieActor == null)
            {
                ZombieSpawner zs = FindObjectOfType<ZombieSpawner>();
                if (zs != null && zs.zombiePrefab != null)
                {
                    zombieActor = Instantiate(zs.zombiePrefab, position, rotation);
                }
                else
                {
                    EnemyAI enemy = FindObjectOfType<EnemyAI>();
                    if (enemy != null) zombieActor = Instantiate(enemy.gameObject, position, rotation);
                }
            }

            if (zombieActor == null)
            {
                zombieActor = CreateProceduralHumanoid("Trailer_Zombie_Actor", position, rotation, new Color(0.18f, 0.12f, 0.12f));
            }
            else
            {
                zombieActor.name = "Trailer_Zombie_Actor";
                zombieActor.transform.position = position;
                zombieActor.transform.rotation = rotation;
                zombieActor.transform.localScale = Vector3.one;

#if UNITY_EDITOR
                Material matBody = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Flooded_Grounds/Character No Animation/Enemy No Animation/Materials/zombie_body_diffuse.mat");
                Material matHead = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Flooded_Grounds/Character No Animation/Enemy No Animation/Materials/zombie_diffuse.mat");
                SkinnedMeshRenderer[] smrs = zombieActor.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var smr in smrs)
                {
                    if (matBody != null && matHead != null)
                    {
                        smr.sharedMaterials = new Material[] { matBody, matHead };
                    }
                }

                RuntimeAnimatorController zAnimCtrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Flooded_Grounds/Character No Animation/Enemy No Animation/Animation zoombie/ZombieAnim.controller");
                Animator anim = zombieActor.GetComponent<Animator>();
                if (anim == null) anim = zombieActor.AddComponent<Animator>();
                if (zAnimCtrl != null)
                {
                    anim.runtimeAnimatorController = zAnimCtrl;
                    anim.SetFloat("Speed", animSpeed); // 0 = Idle đứng yên, 1 = Chạy thật sự
                }
#endif
            }

            NavMeshAgent agent = zombieActor.GetComponent<NavMeshAgent>();
            if (agent != null) Destroy(agent);
            EnemyAI enemyAI = zombieActor.GetComponent<EnemyAI>();
            if (enemyAI != null) Destroy(enemyAI);
            Collider[] cols = zombieActor.GetComponentsInChildren<Collider>();
            foreach (var c in cols) c.enabled = false;

            // Ánh mắt đỏ rực ma quái rùng rợn trong đêm
            Transform head = FindDeepChild(zombieActor.transform, "Head");
            if (head != null)
            {
                GameObject eyeGlow = new GameObject("Zombie_Eye_Glow");
                eyeGlow.transform.SetParent(head);
                eyeGlow.transform.localPosition = new Vector3(0f, 0.12f, 0.15f);
                Light eyeLight = eyeGlow.AddComponent<Light>();
                eyeLight.type = LightType.Point;
                eyeLight.color = new Color(1.0f, 0.15f, 0.1f);
                eyeLight.range = 2.4f;
                eyeLight.intensity = 2.2f;
            }

            spawnedTrailerProps.Add(zombieActor);
            return zombieActor;
        }

        private GameObject BuildRuinedCorner(Vector3 cornerPosition, Quaternion cornerRotation)
        {
            GameObject cornerRoot = new GameObject("Trailer_Ruined_Corner");
            cornerRoot.transform.position = cornerPosition;
            cornerRoot.transform.rotation = cornerRotation;

            Material wallMat = GetTrailerMaterial("BLD_Cabins_Mossy", new Color(0.28f, 0.28f, 0.26f));
            Material stoneMat = GetTrailerMaterial("NAT_Rocks1", new Color(0.22f, 0.22f, 0.22f));

            float wallH = 4.2f;
            float wallW = 5.2f;
            float wallThick = 0.35f;

            // Tường Trái
            GameObject wallL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallL.name = "Corner_Wall_Left";
            wallL.transform.SetParent(cornerRoot.transform, false);
            wallL.transform.localPosition = new Vector3(-wallThick * 0.5f, wallH * 0.5f, wallW * 0.5f);
            wallL.transform.localScale = new Vector3(wallThick, wallH, wallW);
            wallL.GetComponent<Renderer>().sharedMaterial = wallMat;

            // Tường Phải (Góc 90 độ)
            GameObject wallR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallR.name = "Corner_Wall_Right";
            wallR.transform.SetParent(cornerRoot.transform, false);
            wallR.transform.localPosition = new Vector3(wallW * 0.5f, wallH * 0.5f, -wallThick * 0.5f);
            wallR.transform.localScale = new Vector3(wallW, wallH, wallThick);
            wallR.GetComponent<Renderer>().sharedMaterial = wallMat;

            // Trụ góc đá kiên cố
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "Corner_Pillar";
            pillar.transform.SetParent(cornerRoot.transform, false);
            pillar.transform.localPosition = new Vector3(0f, wallH * 0.5f, 0f);
            pillar.transform.localScale = new Vector3(wallThick * 1.8f, wallH * 1.05f, wallThick * 1.8f);
            pillar.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Sàn đá đổ nát ẩm ướt
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Corner_Floor";
            floor.transform.SetParent(cornerRoot.transform, false);
            floor.transform.localPosition = new Vector3(wallW * 0.45f, 0.05f, wallW * 0.45f);
            floor.transform.localScale = new Vector3(wallW * 1.2f, 0.1f, wallW * 1.2f);
            floor.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Đèn pin rơi rọi sáng góc tường tạo bóng đổ kịch tính
            GameObject droppedFlashlight = new GameObject("Dropped_Flashlight");
            droppedFlashlight.transform.SetParent(cornerRoot.transform, false);
            droppedFlashlight.transform.localPosition = new Vector3(3.2f, 0.2f, 3.2f);
            droppedFlashlight.transform.localRotation = Quaternion.Euler(15f, 225f, 0f);
            Light spotL = droppedFlashlight.AddComponent<Light>();
            spotL.type = LightType.Spot;
            spotL.spotAngle = 65f;
            spotL.range = 14f;
            spotL.color = new Color(0.9f, 0.95f, 1.0f);
            spotL.intensity = 4.8f;

            spawnedTrailerProps.Add(cornerRoot);
            return cornerRoot;
        }

        /// <summary>
        /// ✈️ XUẤT HIỆN XÁC MÁY BAY C-400 RƠI ĐẮM GIỮA LÒNG SÔNG VỚI LỬA CHÁY NGÙN NGỤT
        /// Vị trí đã được FindOpenWaterCrashSite() đảm bảo 100% nằm giữa sông nước mở.
        /// Bụng máy bay ngập nửa dưới mặt nước (Y=15.2), phần thân nhô lên trên mặt nước tự nhiên.
        /// Tự động dọn sạch mọi vật thể cản trở (cây, nhà, đá) trong bán kính 25m.
        /// </summary>
        private GameObject SpawnCrashedAirplaneAtSite(Vector3 sitePos)
        {
            // DỌN SẠCH MỌI VẬT THỂ CẢN TRỞ TRONG BÁN KÍNH 25m (cây, nhà, đá, tường...)
            ClearObstaclesAroundSite(sitePos, 25f);

            GameObject c400Fbx = null;
#if UNITY_EDITOR
            c400Fbx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/c-400/source/c-400.fbx");
#endif
            GameObject plane = null;
            Quaternion crashRot = Quaternion.Euler(-1.5f, 20f, 2.5f);
            // Máy bay nằm giữa sông: bụng ngập dưới mặt nước (waterHeight=16.0), thân nhô lên trên mặt nước
            // Y = 15.2 => bụng máy bay chìm 0.8m dưới mực nước, trông tự nhiên như rơi xuống sông
            Vector3 spawnAt = new Vector3(sitePos.x, 15.2f, sitePos.z);

            if (c400Fbx != null)
            {
                plane = Instantiate(c400Fbx, spawnAt, crashRot);
                plane.name = "Trailer_Crashed_C400";
                plane.transform.localScale = Vector3.one * 0.75f;
            }
            else
            {
                GameObject inScenePlane = GameObject.Find("[C400_AIRPLANE_CABIN]");
                if (inScenePlane != null)
                {
                    plane = Instantiate(inScenePlane, spawnAt, crashRot);
                    plane.name = "Trailer_Crashed_C400";
                    plane.transform.localScale = Vector3.one * 0.75f;
                }
            }

            if (plane != null)
            {
                Collider[] cols = plane.GetComponentsInChildren<Collider>();
                foreach (var c in cols) c.enabled = false;

                // 1. Ánh lửa bùng cháy dữ dội tại động cơ cánh trái bị phá hủy
                GameObject engineFire = new GameObject("Plane_Engine_Fire_Light");
                engineFire.transform.SetParent(plane.transform, false);
                engineFire.transform.localPosition = new Vector3(-4.5f, 1.2f, 3.5f);
                Light fireLight = engineFire.AddComponent<Light>();
                fireLight.type = LightType.Point;
                fireLight.range = 38f;
                fireLight.color = new Color(1.0f, 0.45f, 0.08f);
                fireLight.intensity = 5.0f;

                // 2. Lửa bập bùng gần buồng lái và sườn máy bay
                GameObject cockpitFire = new GameObject("Plane_Cockpit_Fire_Light");
                cockpitFire.transform.SetParent(plane.transform, false);
                cockpitFire.transform.localPosition = new Vector3(-0.6f, 0.8f, 7.5f);
                Light fireLight2 = cockpitFire.AddComponent<Light>();
                fireLight2.type = LightType.Point;
                fireLight2.range = 30f;
                fireLight2.color = new Color(1.0f, 0.38f, 0.06f);
                fireLight2.intensity = 4.2f;

                // 3. Đèn khẩn cấp cứu nạn ELT trên đỉnh đuôi máy bay nhấp nháy
                GameObject eltLight = new GameObject("Plane_ELT_Emergency_Light");
                eltLight.transform.SetParent(plane.transform, false);
                eltLight.transform.localPosition = new Vector3(0f, 11.0f, -12.0f);
                Light elt = eltLight.AddComponent<Light>();
                elt.type = LightType.Point;
                elt.range = 45f;
                elt.color = new Color(0.9f, 0.95f, 1.0f);
                elt.intensity = 6.0f;

                spawnedTrailerProps.Add(plane);
            }
            return plane;
        }

        /// <summary>
        /// 🌲 CÂY RỪNG THẬT TRONG BẢN ĐỒ ĐỂ PLAYER CHẶT (PREFAB CÂY CỔ THỤ TreeCreator_Tall_A)
        /// </summary>
        private GameObject SpawnRealTreeToChop(Vector3 position, Quaternion rotation)
        {
            GameObject treePrefab = null;
#if UNITY_EDITOR
            treePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/Prefabs/Nature/Trees/TreeCreator_Tall_A.prefab");
            if (treePrefab == null)
            {
                treePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/Prefabs/Nature/Trees/TreeCreator_Crinkly_A.prefab");
            }
#endif
            GameObject realTree = null;
            if (treePrefab != null)
            {
                realTree = Instantiate(treePrefab, position, rotation);
            }
            else
            {
                GameObject existingTree = GameObject.Find("TreeCreator_Tall_A");
                if (existingTree != null)
                {
                    realTree = Instantiate(existingTree, position, rotation);
                }
            }

            if (realTree != null)
            {
                realTree.name = "Trailer_RealTree_Tall";
                Collider[] treeCols = realTree.GetComponentsInChildren<Collider>();
                foreach (var c in treeCols) c.enabled = false;

                // Vết rìu chém sâu trên vỏ thân cây thật ở độ cao 1.2m
                GameObject notch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                notch.name = "TreeBarkAxeNotch";
                notch.transform.SetParent(realTree.transform, false);
                notch.transform.localPosition = new Vector3(0f, 1.20f, -0.32f);
                notch.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
                notch.transform.localScale = new Vector3(0.08f, 0.04f, 0.12f);
                Material darkCutMat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
                darkCutMat.color = new Color(0.28f, 0.16f, 0.08f);
                notch.GetComponent<Renderer>().sharedMaterial = darkCutMat;
                Destroy(notch.GetComponent<Collider>());

                spawnedTrailerProps.Add(realTree);
            }
            return realTree;
        }

        /// <summary>
        /// 📳 RUNG LẮC THÂN CÂY THẬT MỖI KHI LƯỠI RÌU CHÉM VÀO
        /// </summary>
        private IEnumerator ShakeTreeRoutine(Transform tree)
        {
            if (tree == null) yield break;
            Vector3 basePos = tree.position;
            Quaternion baseRot = tree.rotation;
            float dur = 0.28f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = t / dur;
                float decay = 1f - p;
                float angle = Mathf.Sin(t * 40f) * 1.5f * decay;
                tree.rotation = baseRot * Quaternion.Euler(angle, 0f, 0f);
                yield return null;
            }
            if (tree != null)
            {
                tree.position = basePos;
                tree.rotation = baseRot;
            }
        }

        /// <summary>
        /// 🩸 BÀN TAY CỦA CHÍNH PLAYER (MODEL CH15 MẶC ÁO CHIẾN THUẬT RẰN RI, GĂNG TAY ĐẪM MÁU THẬT)
        /// Sử dụng chính xác 3D Mesh của nhân vật Player Ch15, ẩn xương đầu để không chắn camera POV
        /// </summary>
        private GameObject CreateFirstPersonPlayerHands(Transform cameraTransform)
        {
            GameObject playerFbx = null;
#if UNITY_EDITOR
            playerFbx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/Character No Animation/Ch15_nonPBR.fbx");
#endif
            GameObject handsActor = null;
            if (playerFbx != null)
            {
                handsActor = Instantiate(playerFbx, cameraTransform);
            }
            else
            {
                GameObject inSceneSoldier = GameObject.Find("Ch15_nonPBR");
                if (inSceneSoldier != null)
                {
                    handsActor = Instantiate(inSceneSoldier, cameraTransform);
                }
            }

            if (handsActor == null)
            {
                handsActor = CreateProceduralHumanoid("POV_Player_Hands_Actor", Vector3.zero, Quaternion.identity, new Color(0.25f, 0.35f, 0.45f));
                handsActor.transform.SetParent(cameraTransform, false);
            }
            else
            {
                handsActor.name = "POV_Player_Hands_Actor";
                handsActor.transform.localPosition = new Vector3(0f, -1.60f, -0.06f);
                handsActor.transform.localRotation = Quaternion.identity;
                handsActor.transform.localScale = Vector3.one;

#if UNITY_EDITOR
                Material mat1 = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Flooded_Grounds/Character No Animation/Materials/Ch15_1001_Diffuse.mat");
                Material mat2 = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Flooded_Grounds/Character No Animation/Materials/Ch15_1002_Diffuse.mat");
                SkinnedMeshRenderer[] smrs = handsActor.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var smr in smrs)
                {
                    if (mat1 != null && mat2 != null)
                    {
                        smr.sharedMaterials = new Material[] { mat1, mat2 };
                    }
                }
#endif
            }

            // Tắt Animator và CharacterController để script điều khiển tay trực tiếp
            Animator anim = handsActor.GetComponent<Animator>();
            if (anim != null) Destroy(anim);
            CharacterController cc = handsActor.GetComponent<CharacterController>();
            if (cc != null) Destroy(cc);
            Collider[] cols = handsActor.GetComponentsInChildren<Collider>();
            foreach (var c in cols) c.enabled = false;

            // Thu nhỏ xương đầu về 0 để mũ cối/đầu không che lấp tầm nhìn ống kính camera POV
            Transform head = FindDeepChild(handsActor.transform, "Head");
            if (head != null) head.localScale = Vector3.zero;

            // Chất liệu máu đỏ thẫm ướt bóng trên găng tay chiến thuật của Player
            Material bloodMat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            bloodMat.color = new Color(0.62f, 0.02f, 0.02f);
            bloodMat.SetFloat("_Glossiness", 0.92f);

            // Gắn máu 3D chi tiết vào lòng bàn tay và mu bàn tay của Player
            Transform leftHand = FindDeepChild(handsActor.transform, "LeftHand");
            Transform rightHand = FindDeepChild(handsActor.transform, "RightHand");
            AddBloodGoreToHand(leftHand, bloodMat);
            AddBloodGoreToHand(rightHand, bloodMat);

            // Gắn controller cử động giơ tay bàng hoàng
            POVPlayerHandsController controller = handsActor.AddComponent<POVPlayerHandsController>();
            controller.InitBones();

            return handsActor;
        }

        private void AddBloodGoreToHand(Transform handBone, Material bloodMat)
        {
            if (handBone == null) return;

            // Vết máu loang ướt đẫm lòng bàn tay găng chiến thuật
            GameObject palmBlood = GameObject.CreatePrimitive(PrimitiveType.Cube);
            palmBlood.name = "Glove_Blood_Palm";
            palmBlood.transform.SetParent(handBone, false);
            palmBlood.transform.localPosition = new Vector3(0.01f, 0.04f, 0.01f);
            palmBlood.transform.localScale = new Vector3(0.065f, 0.015f, 0.075f);
            palmBlood.GetComponent<Renderer>().sharedMaterial = bloodMat;
            Destroy(palmBlood.GetComponent<Collider>());

            // Vết máu nhỏ giọt dọc các ngón tay
            for (int i = 0; i < 3; i++)
            {
                GameObject drip = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                drip.name = "Finger_Blood_" + i;
                drip.transform.SetParent(handBone, false);
                drip.transform.localPosition = new Vector3(-0.02f + i * 0.02f, 0.08f, 0.01f);
                drip.transform.localScale = new Vector3(0.012f, 0.035f, 0.012f);
                drip.GetComponent<Renderer>().sharedMaterial = bloodMat;
                Destroy(drip.GetComponent<Collider>());
            }
        }

        private void SpawnWoodChipEffect(Vector3 pos)
        {
            GameObject chipRoot = new GameObject("WoodChips_Burst");
            chipRoot.transform.position = pos;
            Material woodMat = GetTrailerMaterial("BLD_Cabins", new Color(0.6f, 0.4f, 0.2f));

            for (int i = 0; i < 12; i++)
            {
                GameObject chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chip.transform.position = pos + Random.insideUnitSphere * 0.12f;
                float sx = Random.Range(0.015f, 0.035f);
                float sy = Random.Range(0.05f, 0.12f);
                float sz = Random.Range(0.02f, 0.045f);
                chip.transform.localScale = new Vector3(sx, sy, sz);
                chip.transform.rotation = Random.rotation;
                chip.GetComponent<Renderer>().sharedMaterial = woodMat;
                Destroy(chip.GetComponent<Collider>());

                Rigidbody rb = chip.AddComponent<Rigidbody>();
                Vector3 blastDir = new Vector3(Random.Range(-2.2f, 2.2f), Random.Range(2.2f, 4.8f), Random.Range(-2.8f, -0.6f));
                rb.velocity = blastDir;
                rb.angularVelocity = Random.insideUnitSphere * 35f;
                Destroy(chip, 1.8f);
            }
            Destroy(chipRoot, 2.0f);
        }

        private GameObject CreateProceduralAxe(Transform parent)
        {
            GameObject axe = new GameObject("Procedural_Axe");
            axe.transform.SetParent(parent, false);
            axe.transform.localPosition = new Vector3(0.08f, 0.05f, 0.02f);
            axe.transform.localRotation = Quaternion.Euler(75f, 180f, 0f);

            Material woodMat = GetTrailerMaterial("BLD_Cabins", new Color(0.45f, 0.28f, 0.12f));
            Material metalMat = GetTrailerMaterial("NAT_Rocks1", new Color(0.2f, 0.2f, 0.22f));

            // Cán rìu gỗ
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.transform.SetParent(axe.transform, false);
            handle.transform.localScale = new Vector3(0.035f, 0.35f, 0.035f);
            handle.GetComponent<Renderer>().sharedMaterial = woodMat;
            Destroy(handle.GetComponent<Collider>());

            // Lưỡi rìu sắt
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.transform.SetParent(axe.transform, false);
            blade.transform.localPosition = new Vector3(0.08f, 0.28f, 0f);
            blade.transform.localScale = new Vector3(0.18f, 0.14f, 0.025f);
            blade.GetComponent<Renderer>().sharedMaterial = metalMat;
            Destroy(blade.GetComponent<Collider>());

            return axe;
        }

        private GameObject CreateProceduralHumanoid(string name, Vector3 pos, Quaternion rot, Color bodyColor)
        {
            GameObject root = new GameObject(name);
            root.transform.position = pos;
            root.transform.rotation = rot;

            Material mat = new Material(Shader.Find("Diffuse"));
            mat.color = bodyColor;

            // Thân
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            body.transform.localScale = new Vector3(0.55f, 1.0f, 0.4f);
            body.GetComponent<Renderer>().material = mat;
            Destroy(body.GetComponent<Collider>());

            // Đầu
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            head.transform.localScale = new Vector3(0.35f, 0.38f, 0.35f);
            head.GetComponent<Renderer>().material = mat;
            Destroy(head.GetComponent<Collider>());

            // Tay phải
            GameObject rArm = new GameObject("RightArm");
            rArm.transform.SetParent(root.transform, false);
            rArm.transform.localPosition = new Vector3(0.35f, 1.45f, 0f);

            GameObject rHand = new GameObject("RightHand");
            rHand.transform.SetParent(rArm.transform, false);
            rHand.transform.localPosition = new Vector3(0f, -0.6f, 0.15f);

            return root;
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.ToLower().Contains(name.ToLower()))
                    return child;
                Transform result = FindDeepChild(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private Transform FindChildNamed(Transform parent, string name)
        {
            return FindDeepChild(parent, name);
        }

        private Material GetTrailerMaterial(string matName, Color fallbackColor)
        {
            Material mat = null;
#if UNITY_EDITOR
            mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Flooded_Grounds/Content/Materials/" + matName + ".mat");
#endif
            if (mat == null)
            {
                Shader s = Shader.Find("Standard");
                if (s == null) s = Shader.Find("Diffuse");
                mat = new Material(s);
                mat.color = fallbackColor;
            }
            return mat;
        }

        private Vector3 SnapToGround(Vector3 worldPos, float yOffset = 0f)
        {
            float terrainY = worldPos.y;
            bool foundTerrain = false;

            if (Terrain.activeTerrain != null)
            {
                terrainY = Terrain.activeTerrain.SampleHeight(worldPos) + Terrain.activeTerrain.transform.position.y;
                foundTerrain = true;
            }

            // Mặt nước Flooded Grounds nằm ở Y = 16.0m. Cao độ bề mặt hiển thị luôn là Max(mặt đất, mặt nước 16.0m)
            // Nhờ đó camera và các vật thể không bao giờ bị chìm xuống dưới mặt nước gây hiện tượng nhìn xuyên thấu mặt nước!
            float surfaceY = foundTerrain ? Mathf.Max(terrainY, 16.0f) : Mathf.Max(worldPos.y, 16.0f);

            RaycastHit hit;
            float checkStartY = Mathf.Max(surfaceY + 6f, worldPos.y + 6f);
            Vector3 rayStart = new Vector3(worldPos.x, checkStartY, worldPos.z);
            int layerMask = ~LayerMask.GetMask("Ignore Raycast", "TransparentFX", "Water");

            if (Physics.Raycast(rayStart, Vector3.down, out hit, 35f, layerMask, QueryTriggerInteraction.Ignore))
            {
                if (foundTerrain)
                {
                    if (hit.collider == null || hit.point.y > surfaceY + 0.15f || hit.point.y < surfaceY - 0.4f)
                    {
                        return new Vector3(worldPos.x, surfaceY + yOffset, worldPos.z);
                    }
                    return new Vector3(worldPos.x, Mathf.Max(hit.point.y, 16.0f) + yOffset, worldPos.z);
                }
                return new Vector3(worldPos.x, Mathf.Max(hit.point.y, 16.0f) + yOffset, worldPos.z);
            }

            return new Vector3(worldPos.x, surfaceY + yOffset, worldPos.z);
        }

        /// <summary>
        /// 🛡️ CHỐNG CAMERA ĐI XUYÊN VẬT THỂ (XUYÊN TƯỜNG, XUYÊN CÂY, XUYÊN ĐÁ, XUYÊN MẶT ĐẤT, XUYÊN MẶT NƯỚC)
        /// Sử dụng SphereCast từ điểm nhìn (focusTarget) tới camera (desiredCamPos) để phát hiện vật cản.
        /// Tự động kéo camera về phía trước vật cản và đẩy camera ra ngoài nếu nằm trong collider.
        /// </summary>
        private Vector3 ResolveCameraCollision(Vector3 desiredCamPos, Vector3 focusTarget, float minObstacleCushion = 0.35f, bool allowUnderwater = false)
        {
            int mask = ~LayerMask.GetMask("Ignore Raycast", "TransparentFX", "Water");

            Vector3 dir = desiredCamPos - focusTarget;
            float dist = dir.magnitude;
            if (dist < 0.15f) return desiredCamPos;

            Vector3 normDir = dir / dist;

            // 1. Quét SphereCast từ điểm nhìn ra vị trí máy quay mong muốn
            RaycastHit hit;
            if (Physics.SphereCast(focusTarget, 0.22f, normDir, out hit, dist, mask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != null &&
                    !hit.collider.name.StartsWith("Trailer_Player") &&
                    !hit.collider.name.StartsWith("Trailer_Zombie") &&
                    !hit.collider.name.StartsWith("Chaser_") &&
                    !hit.collider.name.StartsWith("Mob_"))
                {
                    float safeDist = Mathf.Max(0.45f, hit.distance - minObstacleCushion);
                    desiredCamPos = focusTarget + normDir * safeDist;
                }
            }

            // 2. Kiểm tra OverlapSphere: Nếu camera vẫn đang nằm lọt bên trong collider nào, đẩy ra ngoài
            Collider[] overlaps = Physics.OverlapSphere(desiredCamPos, 0.28f, mask, QueryTriggerInteraction.Ignore);
            foreach (var col in overlaps)
            {
                if (col == null || col.isTrigger) continue;
                if (col.name.StartsWith("Trailer_Player") ||
                    col.name.StartsWith("Trailer_Zombie") ||
                    col.name.StartsWith("Chaser_") ||
                    col.name.StartsWith("Mob_")) continue;

                Vector3 closest = col.ClosestPoint(desiredCamPos);
                Vector3 push = desiredCamPos - closest;
                if (push.sqrMagnitude < 0.0001f) push = Vector3.up;
                desiredCamPos = closest + push.normalized * 0.35f;
            }

            // 3. Khóa độ cao tối thiểu: Luôn cao hơn mặt nước (16.0f) để camera trailer chỉ quay TRÊN MẶT NƯỚC
            float minSafeY = 16.85f;
            if (Terrain.activeTerrain != null)
            {
                float tY = Terrain.activeTerrain.SampleHeight(desiredCamPos) + Terrain.activeTerrain.transform.position.y;
                minSafeY = Mathf.Max(minSafeY, tY + 0.35f);
            }

            if (desiredCamPos.y < minSafeY)
            {
                desiredCamPos.y = minSafeY;
            }

            return desiredCamPos;
        }

        /// <summary>
        /// 🌊 QUÉT TERRAIN TỰ ĐỘNG TÌM VỊ TRÍ GIỮA SÔNG THẬT SỰ
        /// Duyệt lưới terrain để tìm vùng nước lớn nhất (terrain < 16.0f),
        /// kiểm tra không có vật cản phía trên (trần nhà, mái che),
        /// không có collider gần (cây, nhà, tường, đá),
        /// trả về tọa độ trung tâm vùng nước mở rộng nhất.
        /// </summary>
        private Vector3 FindOpenWaterCrashSite(Vector3 startHint)
        {
            if (Terrain.activeTerrain == null)
            {
                Debug.LogWarning("[CinematicTrailerDirector] Không tìm thấy Terrain! Dùng tọa độ gợi ý.");
                return startHint;
            }

            Terrain terrain = Terrain.activeTerrain;
            Vector3 terrainPos = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;

            // Quét lưới 10m x 10m trên toàn bộ terrain
            float gridStep = 10f;
            float waterLevel = 16.0f;
            int obstacleMask = ~LayerMask.GetMask("Ignore Raycast", "TransparentFX", "Water");

            Vector3 bestPos = startHint;
            float bestScore = -1f;

            for (float x = terrainPos.x + 30f; x < terrainPos.x + terrainSize.x - 30f; x += gridStep)
            {
                for (float z = terrainPos.z + 30f; z < terrainPos.z + terrainSize.z - 30f; z += gridStep)
                {
                    Vector3 testPos = new Vector3(x, waterLevel, z);
                    float terrainY = terrain.SampleHeight(testPos) + terrainPos.y;

                    // Chỉ xét điểm DƯỚI MẶT NƯỚC (terrain < waterLevel) = vùng sông/đầm lầy
                    if (terrainY >= waterLevel - 0.3f) continue;

                    // Kiểm tra bầu trời mở (không có trần nhà/mái che phía trên)
                    RaycastHit roofHit;
                    if (Physics.Raycast(testPos + Vector3.up * 1f, Vector3.up, out roofHit, 50f, obstacleMask, QueryTriggerInteraction.Ignore))
                        continue;

                    // Kiểm tra KHÔNG CÓ vật thể nào trong bán kính 15m (cây, nhà, tường, đá)
                    Collider[] nearby = Physics.OverlapSphere(testPos, 15f, obstacleMask, QueryTriggerInteraction.Ignore);
                    bool hasObstacle = false;
                    foreach (var col in nearby)
                    {
                        if (col == null || col.isTrigger) continue;
                        // Bỏ qua terrain collider
                        if (col is TerrainCollider) continue;
                        hasObstacle = true;
                        break;
                    }
                    if (hasObstacle) continue;

                    // Tính điểm: ưu tiên vùng nước SÂU nhất (terrain thấp nhất) và GẦN tâm map nhất
                    float depth = waterLevel - terrainY; // Càng sâu càng tốt
                    float centerX = terrainPos.x + terrainSize.x * 0.5f;
                    float centerZ = terrainPos.z + terrainSize.z * 0.5f;
                    float distToCenter = Vector2.Distance(new Vector2(x, z), new Vector2(centerX, centerZ));
                    float maxDist = Mathf.Max(terrainSize.x, terrainSize.z) * 0.5f;
                    float centerBonus = 1f - Mathf.Clamp01(distToCenter / maxDist); // Gần tâm = điểm cao

                    // Kiểm tra thêm vùng xung quanh 20m cũng là nước (đảm bảo giữa sông rộng, không phải vũng nước nhỏ)
                    int waterNeighborCount = 0;
                    for (float dx = -20f; dx <= 20f; dx += 10f)
                    {
                        for (float dz = -20f; dz <= 20f; dz += 10f)
                        {
                            float ny = terrain.SampleHeight(new Vector3(x + dx, 0f, z + dz)) + terrainPos.y;
                            if (ny < waterLevel - 0.3f) waterNeighborCount++;
                        }
                    }
                    float areaBonus = waterNeighborCount / 25f; // Tối đa 25 ô 5x5

                    float score = depth * 2f + centerBonus * 3f + areaBonus * 5f;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestPos = testPos;
                    }
                }
            }

            if (bestScore > 0f)
            {
                Debug.Log("🌊 [CinematicTrailerDirector] Tìm thấy GIỮA SÔNG THẬT tại (" + bestPos.x + ", " + bestPos.z + ") — Score: " + bestScore);
                return new Vector3(bestPos.x, 15.2f, bestPos.z);
            }
            else
            {
                Debug.LogWarning("[CinematicTrailerDirector] Không tìm được vùng nước mở! Dùng tọa độ gợi ý.");
                return startHint;
            }
        }

        /// <summary>
        /// 📍 LẤY CAO ĐỘ TERRAIN THẬT CHÍNH XÁC (KHÔNG BỊ CLAMP XUỐNG NƯỚC)
        /// </summary>
        private float GetExactTerrainHeight(Vector3 pos)
        {
            if (Terrain.activeTerrain != null)
            {
                return Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y;
            }
            return Mathf.Max(pos.y, 16.0f);
        }

        /// <summary>
        /// 🏖️ TÌM BỜ SÔNG GẦN NHẤT (ĐẤT LIỀN VEN SÔNG) TỪ VỊ TRÍ GIỮA SÔNG
        /// Dành cho cảnh Scene 3 người sống sót tỉnh dậy trên bờ cạn, nhìn ra xác máy bay giữa sông.
        /// Đảm bảo cao hơn mặt nước ít nhất 1.8m (>= 17.8m), hướng nhìn thoáng về xác máy bay.
        /// </summary>
        private Vector3 FindNearestShorePosition(Vector3 waterPos)
        {
            if (Terrain.activeTerrain == null)
            {
                return waterPos + new Vector3(18f, 3f, 18f);
            }

            Terrain terrain = Terrain.activeTerrain;
            Vector3 terrainPos = terrain.transform.position;
            float waterLevel = 16.0f;
            float minShoreHeight = waterLevel + 1.8f; // Cao hơn nước ít nhất 1.8m

            Vector3 bestShore = waterPos;
            float bestHeight = -999f;

            // Quét vòng tròn mở rộng dần từ 15m đến 220m
            for (float radius = 15f; radius <= 220f; radius += 4f)
            {
                for (int angle = 0; angle < 360; angle += 15)
                {
                    float rad = angle * Mathf.Deg2Rad;
                    float x = waterPos.x + Mathf.Cos(rad) * radius;
                    float z = waterPos.z + Mathf.Sin(rad) * radius;

                    Vector3 testPos = new Vector3(x, 0f, z);
                    float terrainY = terrain.SampleHeight(testPos) + terrainPos.y;

                    if (terrainY < minShoreHeight) continue;

                    // Kiểm tra thêm 6m sâu hơn vào đất liền cũng phải trên cạn >= 17.5m
                    Vector3 inlandDir = new Vector3(x - waterPos.x, 0f, z - waterPos.z).normalized;
                    Vector3 inlandCheck = new Vector3(x + inlandDir.x * 6f, 0f, z + inlandDir.z * 6f);
                    float inlandY = terrain.SampleHeight(inlandCheck) + terrainPos.y;
                    if (inlandY < waterLevel + 1.5f) continue;

                    // Kiểm tra bầu trời mở (không có trần nhà)
                    int mask = ~LayerMask.GetMask("Ignore Raycast", "TransparentFX", "Water");
                    RaycastHit roofHit;
                    Vector3 groundPos = new Vector3(x, terrainY, z);
                    if (Physics.Raycast(groundPos + Vector3.up * 1f, Vector3.up, out roofHit, 30f, mask, QueryTriggerInteraction.Ignore))
                        continue;

                    Debug.Log(string.Format("🏖️ [CinematicTrailerDirector] Tìm thấy BỜ SÔNG ĐẤT LIỀN tại ({0:F1}, {1:F1}, {2:F1}) cao hơn nước {3:F1}m, cách sông {4:F1}m",
                        x, terrainY, z, terrainY - waterLevel, radius));
                    return groundPos;
                }
            }

            // Fallback: Tìm điểm cao nhất trong bán kính 160m
            for (float radius = 20f; radius <= 160f; radius += 5f)
            {
                for (int angle = 0; angle < 360; angle += 20)
                {
                    float rad = angle * Mathf.Deg2Rad;
                    float x = waterPos.x + Mathf.Cos(rad) * radius;
                    float z = waterPos.z + Mathf.Sin(rad) * radius;
                    float terrainY = terrain.SampleHeight(new Vector3(x, 0f, z)) + terrainPos.y;
                    if (terrainY > bestHeight)
                    {
                        bestHeight = terrainY;
                        bestShore = new Vector3(x, terrainY, z);
                    }
                }
            }

            if (bestHeight > waterLevel + 1.0f)
            {
                return bestShore;
            }

            return new Vector3(waterPos.x + 35f, 20f, waterPos.z + 35f);
        }

        /// <summary>
        /// 🏕️ TÌM KHU ĐẤT CAO RÁO, KHÔ RÁO TUYỆT ĐỐI (DRY LAND CAMP SITE)
        /// Dành cho Scene 4 (Player bổ củi dựng lều lửa trại) và các cảnh sau.
        /// Tiêu chuẩn bắt buộc:
        /// 1. Cao độ tại tâm campCenter >= 19.5m (cao hơn mặt nước 16.0m ít nhất 3.5m, trên đồi/thảo nguyên).
        /// 2. Bán kính an toàn 14m: 24 điểm lấy mẫu xung quanh đều >= 18.0m (tuyệt đối KHÔNG có nước, vũng lầy nào).
        /// 3. Độ dốc thoai thoải: Chênh lệch độ cao tối đa <= 3.5m.
        /// 4. Không có mái che / trần nhà phía trên.
        /// </summary>
        private Vector3 FindDryLandCampSite(Vector3 shorePos, Vector3 waterPos)
        {
            if (Terrain.activeTerrain == null)
            {
                return shorePos + new Vector3(20f, 6f, 20f);
            }

            Terrain terrain = Terrain.activeTerrain;
            Vector3 terrainPos = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            float waterLevel = 16.0f;
            int obstacleMask = ~LayerMask.GetMask("Ignore Raycast", "TransparentFX", "Water");

            // Hướng từ mặt nước tiến sâu vào đất liền
            Vector3 inlandDir = (shorePos - waterPos);
            inlandDir.y = 0f;
            if (inlandDir.sqrMagnitude < 0.1f) inlandDir = Vector3.forward;
            else inlandDir.Normalize();

            Vector3 rightDir = new Vector3(-inlandDir.z, 0f, inlandDir.x);

            Vector3 bestCamp = Vector3.zero;
            float bestScore = -999999f;

            // CHIẾN LƯỢC 1: Quét dọc hành lang hướng sâu vào đất liền từ bờ sông (25m đến 140m)
            for (float dist = 25f; dist <= 140f; dist += 6f)
            {
                for (float lat = -35f; lat <= 35f; lat += 7f)
                {
                    Vector3 candidate = shorePos + inlandDir * dist + rightDir * lat;

                    if (candidate.x < terrainPos.x + 40f || candidate.x > terrainPos.x + terrainSize.x - 40f ||
                        candidate.z < terrainPos.z + 40f || candidate.z > terrainPos.z + terrainSize.z - 40f)
                        continue;

                    float centerH = terrain.SampleHeight(candidate) + terrainPos.y;
                    // Bắt buộc cao hơn mặt nước ít nhất 3.5m (>= 19.5m)
                    if (centerH < waterLevel + 3.5f) continue;

                    // KIỂM TRA 24 ĐIỂM XUNG QUANH BÁN KÍNH 14M (MỌI ĐIỂM ĐỀU PHẢI TRÊN ĐẤT KHÔ >= 18.0m)
                    bool allDry = true;
                    float minH = centerH;
                    float maxH = centerH;

                    float[] testRadii = new float[] { 4f, 8f, 13f };
                    for (int rIdx = 0; rIdx < testRadii.Length && allDry; rIdx++)
                    {
                        float r = testRadii[rIdx];
                        for (int ang = 0; ang < 360; ang += 45)
                        {
                            float rad = ang * Mathf.Deg2Rad;
                            Vector3 samplePos = candidate + new Vector3(Mathf.Cos(rad) * r, 0f, Mathf.Sin(rad) * r);
                            float sH = terrain.SampleHeight(samplePos) + terrainPos.y;

                            if (sH < waterLevel + 2.0f) // Thấp hơn 18.0m = mép nước / vùng trũng -> LOẠI NGAY
                            {
                                allDry = false;
                                break;
                            }

                            if (sH < minH) minH = sH;
                            if (sH > maxH) maxH = sH;
                        }
                    }

                    if (!allDry) continue;

                    // Kiểm tra độ bằng phẳng: chênh lệch cao độ tối đa <= 3.5m
                    float slopeDiff = maxH - minH;
                    if (slopeDiff > 3.5f) continue;

                    // Kiểm tra bầu trời mở phía trên
                    RaycastHit roofHit;
                    Vector3 testSky = new Vector3(candidate.x, centerH + 1f, candidate.z);
                    if (Physics.Raycast(testSky, Vector3.up, out roofHit, 35f, obstacleMask, QueryTriggerInteraction.Ignore))
                        continue;

                    float idealHeightBonus = 10f - Mathf.Abs(centerH - 24f) * 0.5f;
                    float flatnessBonus = (3.5f - slopeDiff) * 4f;
                    float distBonus = 5f - Mathf.Abs(dist - 50f) * 0.05f;

                    float score = idealHeightBonus + flatnessBonus + distBonus;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestCamp = new Vector3(candidate.x, centerH, candidate.z);
                    }
                }
            }

            // CHIẾN LƯỢC 2: Quét mở rộng 360 độ từ shorePos (25m đến 220m)
            if (bestScore <= -999000f)
            {
                Debug.LogWarning("[CinematicTrailerDirector] Quét mở rộng 360 độ tìm khu đất khô...");
                for (float radius = 25f; radius <= 220f; radius += 8f)
                {
                    for (int ang = 0; ang < 360; ang += 20)
                    {
                        float rad = ang * Mathf.Deg2Rad;
                        Vector3 candidate = shorePos + new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);

                        if (candidate.x < terrainPos.x + 40f || candidate.x > terrainPos.x + terrainSize.x - 40f ||
                            candidate.z < terrainPos.z + 40f || candidate.z > terrainPos.z + terrainSize.z - 40f)
                            continue;

                        float centerH = terrain.SampleHeight(candidate) + terrainPos.y;
                        if (centerH < waterLevel + 3.0f) continue;

                        bool allDry = true;
                        float minH = centerH;
                        float maxH = centerH;
                        for (int ang2 = 0; ang2 < 360; ang2 += 45)
                        {
                            float rad2 = ang2 * Mathf.Deg2Rad;
                            Vector3 samplePos = candidate + new Vector3(Mathf.Cos(rad2) * 10f, 0f, Mathf.Sin(rad2) * 10f);
                            float sH = terrain.SampleHeight(samplePos) + terrainPos.y;
                            if (sH < waterLevel + 1.8f) { allDry = false; break; }
                            if (sH < minH) minH = sH;
                            if (sH > maxH) maxH = sH;
                        }
                        if (!allDry) continue;
                        if (maxH - minH > 3.8f) continue;

                        float score = centerH - (maxH - minH) * 2f;
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestCamp = new Vector3(candidate.x, centerH, candidate.z);
                        }
                    }
                }
            }

            // CHIẾN LƯỢC 3: Fallback trung tâm bản đồ đồi cao (vùng chắc chắn Y >= 26m)
            if (bestScore <= -999000f)
            {
                Debug.LogWarning("[CinematicTrailerDirector] Dùng vùng đồi cao của bản đồ làm campSite!");
                float hCenter = terrain.SampleHeight(new Vector3(500f, 0f, 520f)) + terrainPos.y;
                bestCamp = new Vector3(500f, Mathf.Max(hCenter, 24f), 520f);
            }

            Debug.Log(string.Format("🏖️🏕️ [CinematicTrailerDirector] Tìm thấy KHU ĐẤT KHÔ TUYỆT ĐỐI tại ({0:F1}, {1:F1}, {2:F1}) — Cao hơn nước {3:F1}m — Score: {4:F1}",
                bestCamp.x, bestCamp.y, bestCamp.z, bestCamp.y - waterLevel, bestScore));

            return bestCamp;
        }

        /// <summary>
        /// 🏃 TÌM HƯỚNG CHẠY RƯỢT ĐUỔI TRÊN ĐẤT KHÔ (CHASE DIRECTION ON DRY LAND)
        /// Quét 16 hướng từ khu cắm trại, chọn hướng mà toàn bộ đường chạy 55m
        /// đều nằm trên đồi/rừng khô ráo (terrainY >= 18.0m, tuyệt đối không đâm xuống sông).
        /// </summary>
        private Vector3 FindSafeDryChaseDirection(Vector3 startCamp)
        {
            if (Terrain.activeTerrain == null) return Vector3.forward;

            Terrain terrain = Terrain.activeTerrain;
            Vector3 terrainPos = terrain.transform.position;
            float waterLevel = 16.0f;

            Vector3 bestDir = Vector3.forward;
            float bestScore = -999999f;

            for (int ang = 0; ang < 360; ang += 22)
            {
                float rad = ang * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)).normalized;

                bool dryPath = true;
                float avgHeight = 0f;
                float minH = 999f;

                for (float d = 10f; d <= 55f; d += 10f)
                {
                    Vector3 checkPt = startCamp + dir * d;
                    float h = terrain.SampleHeight(checkPt) + terrainPos.y;
                    if (h < waterLevel + 1.8f) // Dưới 17.8m = đâm xuống mép sông/vũng nước -> LOẠI
                    {
                        dryPath = false;
                        break;
                    }
                    avgHeight += h;
                    if (h < minH) minH = h;
                }

                if (!dryPath) continue;

                avgHeight /= 5f;
                float score = avgHeight * 2f + minH;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDir = dir;
                }
            }

            return bestDir;
        }

        /// <summary>
        /// 🧹 DỌN SẠCH MỌI VẬT THỂ CẢN TRỞ XUNG QUANH VỊ TRÍ CHỈ ĐỊNH
        /// Tạm ẩn cây, nhà, đá, tường... trong bán kính chỉ định
        /// Chỉ ẩn trong suốt trailer, không xóa vĩnh viễn!
        /// </summary>
        private void ClearObstaclesAroundSite(Vector3 center, float radius)
        {
            int obstacleMask = ~LayerMask.GetMask("Ignore Raycast", "TransparentFX", "Water");
            // SỬ DỤNG CHÍNH XÁC CAO ĐỘ center.y thay vì hardcode 16f
            Collider[] obstacles = Physics.OverlapSphere(new Vector3(center.x, center.y, center.z), radius, obstacleMask, QueryTriggerInteraction.Ignore);

            int hiddenCount = 0;
            foreach (var col in obstacles)
            {
                if (col == null || col.isTrigger) continue;
                if (col is TerrainCollider) continue; // Không ẩn terrain
                if (col.name.StartsWith("Trailer_")) continue; // Không ẩn props trailer

                GameObject obj = col.gameObject;
                // Tìm root object (cha cao nhất không phải Scene root)
                Transform root = obj.transform;
                while (root.parent != null && root.parent.parent != null)
                {
                    root = root.parent;
                }

                if (root.gameObject.activeSelf && !root.name.StartsWith("Trailer_"))
                {
                    root.gameObject.SetActive(false);
                    spawnedTrailerProps.Add(null); // Placeholder
                    if (!hiddenObstacles.Contains(root.gameObject))
                    {
                        hiddenObstacles.Add(root.gameObject);
                        hiddenCount++;
                    }
                }
            }

            Debug.Log(string.Format("🧹 [CinematicTrailerDirector] Đã ẩn {0} vật thể cản trở xung quanh ({1:F1}, {2:F1}, {3:F1}) (bán kính {4}m)",
                hiddenCount, center.x, center.y, center.z, radius));
        }

        /// <summary>
        /// 🚫 TẮT TOÀN BỘ CÁC THI THỂ GAMEPLAY PROCEDURAL ĐỂ KHÔNG BAO GIỜ XUẤT HIỆN HÌNH KHỐI/GHẾ BAY LƠ LỬNG
        /// </summary>
        private void HideAnyGameplayCorpses()
        {
            HorrorGame.Environment.PassengerCorpse[] corpses = FindObjectsOfType<HorrorGame.Environment.PassengerCorpse>();
            for (int i = 0; i < corpses.Length; i++)
            {
                if (corpses[i] != null)
                {
                    corpses[i].gameObject.SetActive(false);
                }
            }
        }

        private void RestoreGameplayCorpses()
        {
            HorrorGame.Environment.PassengerCorpse[] corpses = Resources.FindObjectsOfTypeAll<HorrorGame.Environment.PassengerCorpse>();
            for (int i = 0; i < corpses.Length; i++)
            {
                if (corpses[i] != null && !corpses[i].gameObject.name.StartsWith("Trailer_"))
                {
                    corpses[i].gameObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// 👤 NẠN NHÂN TAI NẠN RƠI MÁY BAY: DÙNG MODEL 3D THẬT (CH15) NẰM SÁT MẶT ĐẤT
        /// Tuyệt đối không dùng các khối hình hộp/ghế sơ sài bay lơ lửng.
        /// </summary>
        private void SpawnRealisticCrashCasualties()
        {
            GameObject playerFbx = null;
#if UNITY_EDITOR
            playerFbx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/Character No Animation/Ch15_nonPBR.fbx");
#endif
            if (playerFbx != null)
            {
                Vector2[] offsets = new Vector2[]
                {
                    new Vector2(-9.5f, 5.0f),
                    new Vector2(11.0f, -6.5f)
                };

                for (int i = 0; i < offsets.Length; i++)
                {
                    Vector3 rawPos = crashSite + new Vector3(offsets[i].x, 0f, offsets[i].y);
                    Vector3 groundPos = SnapToGround(rawPos, 0.05f);

                    // Nằm sát trên mặt cỏ/mặt nước đầm lầy
                    Quaternion lieDownRot = Quaternion.Euler(-85f, (i * 110f + 45f) % 360f, 0f);
                    GameObject body = Instantiate(playerFbx, groundPos, lieDownRot);
                    body.name = "Trailer_Casualty_" + i;
                    body.transform.localScale = Vector3.one * 0.95f;

                    Collider[] cols = body.GetComponentsInChildren<Collider>();
                    foreach (var c in cols) c.enabled = false;

                    MonoBehaviour[] scripts = body.GetComponentsInChildren<MonoBehaviour>();
                    foreach (var s in scripts) Destroy(s);

                    spawnedTrailerProps.Add(body);
                }
            }

            Vector3 firePos = SnapToGround(crashSite + new Vector3(-3.5f, 0f, 2.0f), 0.3f);
            GameObject fireObj = new GameObject("CrashSite_Ground_Fire_Light");
            fireObj.transform.position = firePos;
            Light fireLight = fireObj.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.range = 35f;
            fireLight.color = new Color(1f, 0.45f, 0.08f);
            fireLight.intensity = 3.5f;
            spawnedTrailerProps.Add(fireObj);
        }

        private void SpawnHordeActors()
        {
            for (int i = 0; i < 7; i++)
            {
                Vector3 rawHPos = crashSite + new Vector3(25f + (i * 3.5f), 0f, -15f + (i % 3) * 6f);
                Vector3 groundHPos = SnapToGround(rawHPos, 0f);

                // Speed = 0f (ĐỨNG TRÊN BỜ SÔNG ĐE DỌA, NHÌN RA XÁC MÁY BAY DƯỚI LÒNG SÔNG)
                GameObject mob = SpawnZombieActor(groundHPos, Quaternion.Euler(0f, -90f + (i * 10f), 0f), 0f);
                mob.name = "Horde_Enemy_" + i;

                GameObject torchObj = new GameObject("Mob_Torch_" + i);
                torchObj.transform.position = groundHPos + new Vector3(0.35f, 1.2f, 0.2f);
                Light torchL = torchObj.AddComponent<Light>();
                torchL.type = LightType.Point;
                torchL.range = 16f;
                torchL.color = new Color(1f, 0.5f, 0.1f);
                torchL.intensity = 2.8f;
                spawnedTrailerProps.Add(torchObj);
            }
        }

        private void SpawnChaseSceneActors(Vector3 startCamPos, Vector3 chaseDir)
        {
            chaseEnemies.Clear();
            Vector3 behindDir = -chaseDir;
            Vector3 rightDir = Vector3.Cross(Vector3.up, chaseDir).normalized;

            for (int i = 0; i < 3; i++)
            {
                float lateral = (i - 1) * 1.3f;
                float distBehind = 2.4f + i * 1.4f;
                Vector3 rawPos = startCamPos + behindDir * distBehind + rightDir * lateral;
                Vector3 ePos = SnapToGround(rawPos, 0f);

                // Speed = 1f (CHẠY THỰC THỤ ĐUỔI THEO PLAYER!)
                GameObject chaser = SpawnZombieActor(ePos, Quaternion.LookRotation(chaseDir), 1.0f);
                chaser.name = "Chaser_" + i;
                chaseEnemies.Add(chaser);
            }
        }

        private void UpdateChasingEnemies(Vector3 playerPos, Vector3 chaseDir, float dt)
        {
            Vector3 behindDir = -chaseDir;
            Vector3 rightDir = Vector3.Cross(Vector3.up, chaseDir).normalized;

            for (int i = 0; i < chaseEnemies.Count; i++)
            {
                if (chaseEnemies[i] == null) continue;
                float lateral = (i - 1) * 1.1f + Mathf.Sin(Time.time * 6f + i) * 0.35f;
                float followDist = 2.2f + i * 1.3f;
                Vector3 targetPos = SnapToGround(playerPos + behindDir * followDist + rightDir * lateral, 0f);

                chaseEnemies[i].transform.position = Vector3.MoveTowards(
                    chaseEnemies[i].transform.position,
                    targetPos,
                    6.4f * dt
                );
                chaseEnemies[i].transform.rotation = Quaternion.LookRotation(chaseDir);
            }
        }

        private void CleanupTrailerProps()
        {
            for (int i = 0; i < spawnedTrailerProps.Count; i++)
            {
                if (spawnedTrailerProps[i] != null)
                {
                    Destroy(spawnedTrailerProps[i]);
                }
            }
            spawnedTrailerProps.Clear();
            chaseEnemies.Clear();

            // Khôi phục tất cả vật thể bị ẩn xung quanh máy bay (cây, nhà, đá...)
            foreach (var obj in hiddenObstacles)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
            hiddenObstacles.Clear();
        }
        /// <summary>
        /// 🚫 ẨN TOÀN BỘ THANH CHỈ SỐ UI/HUD GAMEPLAY TRONG SUỐT TRAILER CINEMATIC
        /// Tắt tất cả Canvas (thanh máu, thanh đói, thanh khát, thanh thể lực, thanh tinh thần...)
        /// Tắt PlayerUI, PlayerStats OnGUI, PlayerFractureSystem OnGUI, WeaponManager OnGUI, v.v.
        /// CHỈ ẢNH HƯỞNG TRONG TRAILER, KHÔNG ĐỤNG ĐẾN GAME CHÍNH!
        /// </summary>
        private void HideAllGameplayHUD()
        {
            hiddenCanvases.Clear();
            disabledHudScripts.Clear();

            // 1. Tắt tất cả Canvas trong scene (thanh máu, thanh đói, thanh khát, v.v.)
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in allCanvases)
            {
                if (canvas != null && canvas.enabled)
                {
                    canvas.enabled = false;
                    hiddenCanvases.Add(canvas);
                }
            }

            // 2. Tắt PlayerUI (thanh slider máu, thể lực, thức ăn, nước uống, tinh thần)
            HorrorGame.Player.PlayerUI[] playerUIs = FindObjectsOfType<HorrorGame.Player.PlayerUI>();
            foreach (var pui in playerUIs)
            {
                if (pui != null && pui.enabled)
                {
                    pui.enabled = false;
                    disabledHudScripts.Add(pui);
                }
            }

            // 3. Tắt PlayerStats (OnGUI thông báo trạng thái đói, khát, lạnh, cháy...)
            HorrorGame.Player.PlayerStats[] playerStats = FindObjectsOfType<HorrorGame.Player.PlayerStats>();
            foreach (var ps in playerStats)
            {
                if (ps != null && ps.enabled)
                {
                    ps.enabled = false;
                    disabledHudScripts.Add(ps);
                }
            }

            // 4. Tắt PlayerFractureSystem (OnGUI icon gãy xương)
            MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();
            foreach (var script in allScripts)
            {
                if (script == null || !script.enabled) continue;
                string typeName = script.GetType().Name;
                // Tắt mọi script HUD/UI gameplay OnGUI (không đụng đến CinematicTrailerDirector)
                if (typeName == "PlayerFractureSystem" ||
                    typeName == "WeaponManager" ||
                    typeName == "FPSWeapon" ||
                    typeName == "PlayerFlashlight" ||
                    typeName == "ZombieParasiteSystem")
                {
                    script.enabled = false;
                    disabledHudScripts.Add(script);
                }
            }

            // 5. Tắt UnityEngine.UI elements (EventSystem, các UI GameObject)
            EventSystem eventSys = FindObjectOfType<EventSystem>();
            if (eventSys != null && eventSys.enabled)
            {
                eventSys.enabled = false;
                disabledHudScripts.Add(eventSys);
            }

            Debug.Log("🎬 [CinematicTrailerDirector] Đã ẩn toàn bộ " + hiddenCanvases.Count + " Canvas và " + disabledHudScripts.Count + " HUD scripts trong Trailer!");
        }

        /// <summary>
        /// ✅ KHÔI PHỤC TOÀN BỘ UI/HUD GAMEPLAY SAU KHI TRAILER KẾT THÚC
        /// Bật lại tất cả Canvas và script HUD đã bị tắt, game chính hoạt động bình thường trở lại.
        /// </summary>
        private void RestoreAllGameplayHUD()
        {
            // Bật lại tất cả Canvas đã bị tắt
            foreach (var canvas in hiddenCanvases)
            {
                if (canvas != null)
                {
                    canvas.enabled = true;
                }
            }
            hiddenCanvases.Clear();

            // Bật lại tất cả HUD scripts đã bị tắt
            foreach (var script in disabledHudScripts)
            {
                if (script != null)
                {
                    script.enabled = true;
                }
            }
            disabledHudScripts.Clear();

            Debug.Log("🎬 [CinematicTrailerDirector] Đã khôi phục toàn bộ UI/HUD gameplay sau Trailer!");
        }
        #endregion

        #region HELPER METHODS
        private void SetSubtitle(string text, float duration)
        {
            currentSubtitle = text;
            StartCoroutine(SubtitleRoutine(duration));
        }

        private IEnumerator SubtitleRoutine(float duration)
        {
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                subtitleAlpha = t / 0.4f;
                yield return null;
            }

            yield return new WaitForSeconds(Mathf.Max(0.2f, duration - 0.8f));

            t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                subtitleAlpha = 1f - (t / 0.4f);
                yield return null;
            }
            subtitleAlpha = 0f;
            currentSubtitle = "";
        }

        private IEnumerator QuickFadeTransition(float duration)
        {
            float half = duration * 0.5f;
            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                blackFadeAlpha = t / half;
                yield return null;
            }
            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                blackFadeAlpha = 1f - (t / half);
                yield return null;
            }
            blackFadeAlpha = 0f;
        }

        private void ApplyProceduralCameraShake(float intensity)
        {
            if (trailerCamera == null) return;
            float trauma = Mathf.Clamp01(cameraShakeTrauma + intensity);
            float shakeX = (Mathf.PerlinNoise(Time.time * 28f, 0f) * 2f - 1f) * trauma * 0.16f;
            float shakeY = (Mathf.PerlinNoise(0f, Time.time * 28f) * 2f - 1f) * trauma * 0.16f;
            float shakeRot = (Mathf.PerlinNoise(Time.time * 22f, Time.time * 22f) * 2f - 1f) * trauma * 2.2f;

            trailerCamera.transform.position += new Vector3(shakeX, shakeY, 0f) * Time.deltaTime * 6f;
            trailerCamera.transform.Rotate(0f, 0f, shakeRot * Time.deltaTime * 6f);
        }
        #endregion

        #region MP4 AUTOMATIC EXPORT SYSTEM
        private string compilationStatus = "";

        private IEnumerator CaptureFramesRoutine()
        {
            while (isRecordingFrames)
            {
                yield return new WaitForEndOfFrame();
                if (!isRecordingFrames) break;

                string frameFileName = string.Format("frame_{0:D6}.png", capturedFrameIndex);
                string fullPath = Path.Combine(tempFramesDir, frameFileName);

#if UNITY_2017_1_OR_NEWER
                ScreenCapture.CaptureScreenshot(fullPath);
#else
                Application.CaptureScreenshot(fullPath);
#endif
                capturedFrameIndex++;
            }
        }

        private IEnumerator CompileMp4Routine()
        {
            isCompilingMp4 = true;
            compilationStatus = "Đang hoàn tất lưu dữ liệu khung hình vào ổ cứng...";
            Debug.Log("🎬 [CinematicTrailerDirector] ══ BẮT ĐẦU TRÍCH XUẤT VIDEO MP4 ══");
            Debug.Log("📸 Tổng số khung hình đã quay: " + capturedFrameIndex);

            yield return new WaitForSecondsRealtime(2.0f);

            compilationStatus = "Đang tìm kiếm Python và FFmpeg H.264 Encoder...";

            string scriptPath = Path.GetFullPath(Path.Combine(Application.dataPath, "Flooded_Grounds/Scripts/Cutscenes/compile_trailer_mp4.py"));
            string audioPath = Path.GetFullPath(Path.Combine(Application.dataPath, "Flooded_Grounds/sound main menu/lemonmusiclab-dark-ambient-soundscape-505384.mp3"));

            string pythonExe = "python";
            string[] candidatePaths = new string[]
            {
                @"C:\Users\Admin\AppData\Local\Programs\Python\Python313\python.exe",
                @"C:\Users\Admin\AppData\Local\Programs\Python\Python312\python.exe",
                @"C:\Users\Admin\AppData\Local\Programs\Python\Python311\python.exe",
                @"C:\Python313\python.exe",
                "python.exe"
            };
            foreach (var p in candidatePaths)
            {
                if (File.Exists(p))
                {
                    pythonExe = p;
                    break;
                }
            }

            compilationStatus = "Đang nén video MP4 H.264 tốc độ cao... Vui lòng đợi giây lát...";

            bool processStarted = false;
            System.Diagnostics.Process proc = null;
            try
            {
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
                psi.FileName = pythonExe;
                psi.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\" {3} \"{4}\"", 
                    scriptPath, tempFramesDir, finalMp4Path, recordingFps, audioPath);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                proc = System.Diagnostics.Process.Start(psi);
                processStarted = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("❌ Lỗi khi gọi Python compiler: " + ex.Message);
                compilationStatus = "Lỗi khi gọi Python: " + ex.Message;
            }

            if (processStarted && proc != null)
            {
                while (!proc.HasExited)
                {
                    yield return new WaitForSecondsRealtime(0.5f);
                }

                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                Debug.Log("🎬 [Python Output]:\n" + output);
                if (!string.IsNullOrEmpty(error)) Debug.LogWarning("🎬 [Python Stderr]:\n" + error);
            }

            if (File.Exists(finalMp4Path))
            {
                long fileSize = new FileInfo(finalMp4Path).Length;
                float sizeMb = fileSize / (1024f * 1024f);
                compilationStatus = string.Format("✅ XUẤT VIDEO THÀNH CÔNG: Trailer_DeadIslandSurvival.mp4 ({0:F1} MB)", sizeMb);
                Debug.Log("🎉 [CinematicTrailerDirector] " + compilationStatus);
            }
            else
            {
                compilationStatus = "⚠️ Hoàn tất nén video. Kiểm tra thư mục dự án.";
            }

            yield return new WaitForSecondsRealtime(3.5f);
            isCompilingMp4 = false;
        }
        #endregion

        #region ONGUI CINEMATIC RENDERING (21:9 LETTERBOX, BLINK, BLOOD, SUBTITLES, TITLE CARD)
        void OnGUI()
        {
            if (!IsTrailerRunning && !isCompilingMp4) return;

            InitGUIStyles();

            // 1. Viền đen điện ảnh (Cinematic Letterbox 21:9)
            letterboxHeight = Mathf.Lerp(letterboxHeight, targetLetterboxHeight, Time.deltaTime * 4.0f);
            if (letterboxHeight > 1f)
            {
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, letterboxHeight), blackTex);
                GUI.DrawTexture(new Rect(0, Screen.height - letterboxHeight, Screen.width, letterboxHeight), blackTex);
            }

            // 2. Hiệu ứng chớp mắt (Blinking Eyelids)
            if (blinkEyeAlpha > 0.01f)
            {
                GUI.color = new Color(0f, 0f, 0f, blinkEyeAlpha);
                float halfH = Screen.height * 0.5f * blinkEyeAlpha;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, halfH), blackTex);
                GUI.DrawTexture(new Rect(0, Screen.height - halfH, Screen.width, halfH), blackTex);
            }

            // 3. Máu bắn tung tóe lên ống kính (Blood Splatter Jumpscare)
            if (bloodSplatterAlpha > 0.01f)
            {
                GUI.color = new Color(1f, 1f, 1f, bloodSplatterAlpha);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), bloodTex);
            }

            // 4. Màn đen chuyển cảnh (Black Screen Fade)
            if (blackFadeAlpha > 0.01f)
            {
                GUI.color = new Color(0f, 0f, 0f, blackFadeAlpha);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTex);
            }

            // 5. Phụ đề điện ảnh (Cinematic Subtitles)
            if (!string.IsNullOrEmpty(currentSubtitle) && subtitleAlpha > 0.01f)
            {
                float subY = Screen.height - letterboxHeight - 48f;
                Rect subRect = new Rect(0, subY, Screen.width, 40);

                subtitleShadowStyle.normal.textColor = new Color(0f, 0f, 0f, subtitleAlpha * 0.9f);
                GUI.Label(new Rect(subRect.x + 2, subRect.y + 2, subRect.width, subRect.height), currentSubtitle, subtitleShadowStyle);

                subtitleStyle.normal.textColor = new Color(0.95f, 0.95f, 0.95f, subtitleAlpha);
                GUI.Label(subRect, currentSubtitle, subtitleStyle);
            }

            // 6. Tựa đề phát sáng cuối Trailer (TITLE CARD REVEAL)
            if (titleCardAlpha > 0.01f)
            {
                float centerY = Screen.height * 0.40f;

                titleMainStyle.normal.textColor = new Color(0.92f, 0.12f, 0.12f, titleCardAlpha);
                GUI.Label(new Rect(0, centerY, Screen.width, 70), "DEAD ISLAND SURVIVAL", titleMainStyle);

                titleSubStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f, titleCardAlpha * 0.88f);
                GUI.Label(new Rect(0, centerY + 75, Screen.width, 35), "CAN YOU SURVIVE THE NIGHT?", titleSubStyle);

                titleSubStyle.normal.textColor = new Color(0.95f, 0.75f, 0.2f, titleCardAlpha * 0.95f);
                GUI.Label(new Rect(0, centerY + 115, Screen.width, 35), "WISHLIST NOW ON STEAM  •  COMING SOON", titleSubStyle);
            }

            // 7. Hộp thông báo xuất video MP4 tự động
            if (isCompilingMp4)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.85f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTex);

                float boxW = Mathf.Min(720f, Screen.width * 0.88f);
                float boxH = 190f;
                float boxX = (Screen.width - boxW) * 0.5f;
                float boxY = (Screen.height - boxH) * 0.5f;

                GUI.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
                GUI.DrawTexture(new Rect(boxX, boxY, boxW, boxH), whiteTex);

                GUI.color = Color.white;
                subtitleStyle.normal.textColor = new Color(1f, 0.82f, 0.2f);
                GUI.Label(new Rect(boxX, boxY + 20, boxW, 35), "🎬 ĐANG TỰ ĐỘNG XUẤT VIDEO TRAILER MP4", subtitleStyle);

                subtitleStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(boxX, boxY + 68, boxW, 30), compilationStatus, subtitleStyle);

                titleSubStyle.normal.textColor = new Color(0.6f, 0.88f, 1f);
                GUI.Label(new Rect(boxX, boxY + 115, boxW, 25), finalMp4Path, titleSubStyle);

                titleSubStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f);
                GUI.Label(new Rect(boxX, boxY + 145, boxW, 25), "Windows Explorer sẽ tự động mở file MP4 khi hoàn tất...", titleSubStyle);
            }

            GUI.color = Color.white;
        }

        private void InitGUIStyles()
        {
            if (subtitleStyle == null)
            {
                subtitleStyle = new GUIStyle();
                subtitleStyle.alignment = TextAnchor.MiddleCenter;
                subtitleStyle.fontSize = Mathf.Clamp(Screen.width / 45, 18, 30);
                subtitleStyle.fontStyle = FontStyle.Bold;

                subtitleShadowStyle = new GUIStyle(subtitleStyle);

                titleMainStyle = new GUIStyle();
                titleMainStyle.alignment = TextAnchor.MiddleCenter;
                titleMainStyle.fontSize = Mathf.Clamp(Screen.width / 22, 36, 68);
                titleMainStyle.fontStyle = FontStyle.Bold;

                titleSubStyle = new GUIStyle();
                titleSubStyle.alignment = TextAnchor.MiddleCenter;
                titleSubStyle.fontSize = Mathf.Clamp(Screen.width / 55, 14, 24);
                titleSubStyle.fontStyle = FontStyle.Bold;
            }
        }
        #endregion
    }

    /// <summary>
    /// 🪓 ĐIỀU KHIỂN HÀNH ĐỘNG BỔ GỖ CHẶT CÂY THẬT BẰNG 1 TAY (ONE-HANDED REAL TREE CHOPPING)
    /// Player cầm chặt cán rìu bằng MỘT TAY PHẢI duy nhất (5 ngón cuộn tròn nắm đấm).
    /// Tay trái thả lỏng tự nhiên thăng bằng bên hông (KHÔNG CẦM RÌU).
    /// Vung rìu từ trên đầu xuống chém vào thân cây — toàn thân xoay hông mạnh theo nhát chém.
    /// </summary>
    public class PlayerWoodChopAnimator : MonoBehaviour
    {
        public Transform spine;
        public Transform rightShoulder;
        public Transform rightArm;
        public Transform rightForeArm;
        public Transform rightHand;
        public Transform leftShoulder;
        public Transform leftArm;
        public Transform leftForeArm;
        public Transform leftHand;
        public Transform head;

        public Transform axeTransform;
        public System.Action onChopHit;

        private float rightUpperArmLen = 0.28f;
        private float rightForeArmLen = 0.25f;
        private float leftUpperArmLen = 0.28f;
        private float leftForeArmLen = 0.25f;

        private Vector3 defaultRightUpperArmDir;
        private Quaternion defaultRightUpperArmRot;
        private Vector3 defaultRightForeArmDir;
        private Quaternion defaultRightForeArmRot;

        private Vector3 defaultLeftUpperArmDir;
        private Quaternion defaultLeftUpperArmRot;
        private Vector3 defaultLeftForeArmDir;
        private Quaternion defaultLeftForeArmRot;

        private Quaternion defaultSpineRot;
        private Quaternion defaultHeadRot;

        // Xương chân để chuyển trọng tâm theo nhát chém
        private Transform leftUpLeg;
        private Transform leftLeg;
        private Transform rightUpLeg;
        private Transform rightLeg;
        private Quaternion defaultLeftUpLegRot;
        private Quaternion defaultLeftLegRot;
        private Quaternion defaultRightUpLegRot;
        private Quaternion defaultRightLegRot;

        // Xương các ngón tay của 2 bàn tay (để nắm chặt cán rìu)
        private Transform[] rFingers = new Transform[15];
        private Quaternion[] defRFingerRots = new Quaternion[15];
        private Transform[] lFingers = new Transform[15];
        private Quaternion[] defLFingerRots = new Quaternion[15];

        private bool initializedBones = false;
        private float cycleTimer = 0f;
        private float cycleDuration = 1.05f; // Chu kỳ nhát chém 1.05s uy lực
        private bool hitTriggeredInCycle = false;

        public void InitBones(Transform axe)
        {
            axeTransform = axe;

            spine = FindChildNamed(transform, "Spine");
            rightShoulder = FindChildNamed(transform, "RightShoulder");
            rightArm = FindChildNamed(transform, "RightArm");
            rightForeArm = FindChildNamed(transform, "RightForeArm");
            rightHand = FindChildNamed(transform, "RightHand");
            leftShoulder = FindChildNamed(transform, "LeftShoulder");
            leftArm = FindChildNamed(transform, "LeftArm");
            leftForeArm = FindChildNamed(transform, "LeftForeArm");
            leftHand = FindChildNamed(transform, "LeftHand");
            head = FindChildNamed(transform, "Head");

            if (spine != null) defaultSpineRot = spine.localRotation;
            if (head != null) defaultHeadRot = head.localRotation;

            // Xương chân: cho chân bước theo nhát chém
            leftUpLeg = FindChildNamed(transform, "LeftUpLeg");
            leftLeg = FindChildNamed(transform, "LeftLeg");
            rightUpLeg = FindChildNamed(transform, "RightUpLeg");
            rightLeg = FindChildNamed(transform, "RightLeg");
            if (leftUpLeg != null) defaultLeftUpLegRot = leftUpLeg.localRotation;
            if (leftLeg != null) defaultLeftLegRot = leftLeg.localRotation;
            if (rightUpLeg != null) defaultRightUpLegRot = rightUpLeg.localRotation;
            if (rightLeg != null) defaultRightLegRot = rightLeg.localRotation;

            if (rightArm != null && rightForeArm != null && rightHand != null)
            {
                rightUpperArmLen = Mathf.Max(0.20f, Vector3.Distance(rightArm.position, rightForeArm.position));
                rightForeArmLen = Mathf.Max(0.18f, Vector3.Distance(rightForeArm.position, rightHand.position));

                defaultRightUpperArmDir = (rightForeArm.position - rightArm.position).normalized;
                defaultRightUpperArmRot = rightArm.rotation;
                defaultRightForeArmDir = (rightHand.position - rightForeArm.position).normalized;
                defaultRightForeArmRot = rightForeArm.rotation;
            }

            if (leftArm != null && leftForeArm != null && leftHand != null)
            {
                leftUpperArmLen = Mathf.Max(0.20f, Vector3.Distance(leftArm.position, leftForeArm.position));
                leftForeArmLen = Mathf.Max(0.18f, Vector3.Distance(leftForeArm.position, leftHand.position));

                defaultLeftUpperArmDir = (leftForeArm.position - leftArm.position).normalized;
                defaultLeftUpperArmRot = leftArm.rotation;
                defaultLeftForeArmDir = (leftHand.position - leftForeArm.position).normalized;
                defaultLeftForeArmRot = leftForeArm.rotation;
            }

            // Khởi tạo xương ngón tay bàn tay phải & trái
            string[] rFingerNames = new string[]
            {
                "RightHandIndex1", "RightHandIndex2", "RightHandIndex3",
                "RightHandMiddle1", "RightHandMiddle2", "RightHandMiddle3",
                "RightHandRing1", "RightHandRing2", "RightHandRing3",
                "RightHandPinky1", "RightHandPinky2", "RightHandPinky3",
                "RightHandThumb1", "RightHandThumb2", "RightHandThumb3"
            };

            string[] lFingerNames = new string[]
            {
                "LeftHandIndex1", "LeftHandIndex2", "LeftHandIndex3",
                "LeftHandMiddle1", "LeftHandMiddle2", "LeftHandMiddle3",
                "LeftHandRing1", "LeftHandRing2", "LeftHandRing3",
                "LeftHandPinky1", "LeftHandPinky2", "LeftHandPinky3",
                "LeftHandThumb1", "LeftHandThumb2", "LeftHandThumb3"
            };

            for (int i = 0; i < 15; i++)
            {
                rFingers[i] = FindChildNamed(transform, rFingerNames[i]);
                if (rFingers[i] != null) defRFingerRots[i] = rFingers[i].localRotation;

                lFingers[i] = FindChildNamed(transform, lFingerNames[i]);
                if (lFingers[i] != null) defLFingerRots[i] = lFingers[i].localRotation;
            }

            initializedBones = true;
        }

        void LateUpdate()
        {
            if (!initializedBones) return;

            cycleTimer += Time.deltaTime;
            float progress = (cycleTimer % cycleDuration) / cycleDuration;

            if (progress < 0.2f) hitTriggeredInCycle = false;

            float handForward = 0.34f;
            float handUp = 1.15f;
            float handRight = 0.18f;
            float axePitch = 12f;
            float axeYaw = 12f;
            float axeRoll = 0f;
            float spineYaw = 0f;
            float spinePitch = 4f;
            float leftSmooth = 0f;

            // 1. GIAI ĐOẠN LẤY ĐÀ VUNG RÌU TỪ TRÊN ĐẦU XUỐNG (WIND-UP): 0.0 -> 0.45
            //    Rìu đưa từ thế thủ lên cao qua vai ra sau đầu, cơ thể xoay hông mạnh
            if (progress < 0.45f)
            {
                float p = progress / 0.45f;
                float smoothP = Mathf.SmoothStep(0f, 1f, p);

                // Tay đưa rìu lên cao qua đầu và ra sau bên phải
                handForward = Mathf.Lerp(0.34f, -0.08f, smoothP);  // Kéo ra sau nhiều hơn
                handUp = Mathf.Lerp(1.15f, 1.65f, smoothP);        // Đưa lên cao qua đầu
                handRight = Mathf.Lerp(0.18f, 0.32f, smoothP);

                // Rìu nghiêng ngược ra sau (lưỡi rìu hướng lên trời)
                axePitch = Mathf.Lerp(12f, -65f, smoothP);
                axeYaw = Mathf.Lerp(12f, 45f, smoothP);
                axeRoll = Mathf.Lerp(0f, 22f, smoothP);

                // Xoay hông mạnh về bên phải lấy đà
                spineYaw = Mathf.Lerp(0f, 28f, smoothP);
                spinePitch = Mathf.Lerp(4f, -8f, smoothP);         // Ngả nhẹ ra sau
                leftSmooth = smoothP;
            }
            // 2. GIAI ĐOẠN VUNG CHÉM BỔ TỪ TRÊN XUỐNG (STRIKE): 0.45 -> 0.60 (CỰC NHANH, DỨT KHOÁT)
            //    Toàn thân dập xuống theo nhát chém, hông xoay ngược lại
            else if (progress < 0.60f)
            {
                float p = (progress - 0.45f) / 0.15f;
                float strikeP = p * p * p; // Gia tốc lũy thừa 3 — sấm sét

                // Tay đập xuống phía trước mạnh
                handForward = Mathf.Lerp(-0.08f, 0.52f, strikeP);
                handUp = Mathf.Lerp(1.65f, 1.08f, strikeP);
                handRight = Mathf.Lerp(0.32f, 0.08f, strikeP);

                // Rìu xoay từ trên xuống chém vào thân cây
                axePitch = Mathf.Lerp(-65f, 25f, strikeP);
                axeYaw = Mathf.Lerp(45f, -18f, strikeP);
                axeRoll = Mathf.Lerp(22f, -8f, strikeP);

                // Toàn thân dập xuống theo — xoay hông ngược lại
                spineYaw = Mathf.Lerp(28f, -20f, strikeP);
                spinePitch = Mathf.Lerp(-8f, 22f, strikeP);        // Cúi người xuống mạnh
                leftSmooth = 1f - strikeP * 0.4f;

                // Khoảnh khắc lưỡi rìu cắm ngập vào vỏ cây thật
                if (p >= 0.88f && !hitTriggeredInCycle)
                {
                    hitTriggeredInCycle = true;
                    if (onChopHit != null) onChopHit();
                }
            }
            // 3. GIAI ĐOẠN GIỮ LỰC & RUNG CHẤN (IMPACT HOLD): 0.60 -> 0.74
            //    Rìu cắm trong thân cây, toàn thân rung nhẹ
            else if (progress < 0.74f)
            {
                float p = (progress - 0.60f) / 0.14f;
                float recoilVibe = Mathf.Sin(p * Mathf.PI * 5f) * 0.018f * (1f - p);

                handForward = 0.52f + recoilVibe;
                handUp = 1.08f + recoilVibe * 2f;
                handRight = 0.08f;

                axePitch = 25f;
                axeYaw = -18f;
                axeRoll = -8f;

                spineYaw = -20f + recoilVibe * 40f;
                spinePitch = 22f - p * 6f;
                leftSmooth = 0.6f;
            }
            // 4. GIAI ĐOẠN RÚT RÌU VỀ (RECOVERY): 0.74 -> 1.0
            //    Rút lưỡi rìu khỏi vỏ cây, duỗi thẳng người, trả về thế thủ
            else
            {
                float p = (progress - 0.74f) / 0.26f;
                float smoothP = Mathf.SmoothStep(0f, 1f, p);

                handForward = Mathf.Lerp(0.52f, 0.34f, smoothP);
                handUp = Mathf.Lerp(1.08f, 1.15f, smoothP);
                handRight = Mathf.Lerp(0.08f, 0.18f, smoothP);

                axePitch = Mathf.Lerp(25f, 12f, smoothP);
                axeYaw = Mathf.Lerp(-18f, 12f, smoothP);
                axeRoll = Mathf.Lerp(-8f, 0f, smoothP);

                spineYaw = Mathf.Lerp(-20f, 0f, smoothP);
                spinePitch = Mathf.Lerp(16f, 4f, smoothP);
                leftSmooth = Mathf.Lerp(0.6f, 0f, smoothP);
            }

            Vector3 fwd = transform.forward;
            Vector3 up = transform.up;
            Vector3 right = transform.right;

            // Vị trí lòng bàn tay phải (tay duy nhất cầm rìu)
            Vector3 axeGripPosRight = transform.position + fwd * handForward + up * handUp + right * handRight;
            // Góc xoay của chiếc rìu theo hướng vung một tay
            Quaternion axeRot = transform.rotation * Quaternion.Euler(axePitch, axeYaw, axeRoll);

            // Cột Sống: Nghiêng và xoay người theo nhịp vung tay
            if (spine != null)
            {
                spine.localRotation = defaultSpineRot * Quaternion.Euler(spinePitch, spineYaw, 0f);
            }

            // Đầu: Mắt luôn nhìn thẳng vào vết chém trên thân cây thật
            if (head != null)
            {
                head.localRotation = defaultHeadRot * Quaternion.Euler(spinePitch * 0.5f + 10f, -spineYaw * 0.6f, 0f);
            }

            // CHÂN: Chuyển trọng tâm theo nhát chém — Tư thế sinh tồn thể thao, chùng gối vững chãi (không đứng thẳng đuỗn)
            float legSwing = spineYaw * 0.35f; // Chân xoay theo hông
            float baseKneeBend = 16f; // Gối luôn chùng tự nhiên 16 độ để tạo thế đứng trụ vững
            float dynamicKnee = Mathf.Abs(spinePitch) * 0.45f; // Chùng sâu khi dập lực chém

            if (rightUpLeg != null)
            {
                // Chân phải bước tới trước trụ lực khi chém
                rightUpLeg.localRotation = defaultRightUpLegRot * Quaternion.Euler(-legSwing * 0.8f - 8f, 4f, 0f);
            }
            if (rightLeg != null)
            {
                // Khớp gối phải co sâu theo lực chém (16 - 30 độ)
                rightLeg.localRotation = defaultRightLegRot * Quaternion.Euler(baseKneeBend + dynamicKnee, 0f, 0f);
            }
            if (leftUpLeg != null)
            {
                // Chân trái lùi sau làm đối trọng cân bằng
                leftUpLeg.localRotation = defaultLeftUpLegRot * Quaternion.Euler(legSwing * 0.6f + 10f, -6f, 0f);
            }
            if (leftLeg != null)
            {
                // Gối trái co đỡ trọng tâm
                leftLeg.localRotation = defaultLeftLegRot * Quaternion.Euler(baseKneeBend + dynamicKnee * 0.5f, 0f, 0f);
            }

            // TAY PHẢI (CẦM 1 TAY): Khớp quay bàn tay phải ôm chặt cán rìu
            Quaternion rightHandRot = axeRot * Quaternion.Euler(0f, -90f, 90f);
            Vector3 rightWristPos = axeGripPosRight - rightHandRot * new Vector3(0.015f, 0.08f, 0.035f);

            if (rightArm != null && rightForeArm != null && rightHand != null)
            {
                Vector3 hintRight = right * 0.7f - up * 0.3f;
                SolveTwoBoneIK(rightArm, rightForeArm, rightHand, rightWristPos, hintRight,
                    rightUpperArmLen, rightForeArmLen,
                    defaultRightUpperArmDir, defaultRightUpperArmRot,
                    defaultRightForeArmDir, defaultRightForeArmRot);

                rightHand.rotation = rightHandRot;
            }

            // TAY TRÁI (KHÔNG CẦM RÌU): Thả tự do làm đối trọng thăng bằng bên hông
            Vector3 leftWristTarget = transform.position
                - right * Mathf.Lerp(0.24f, 0.28f, leftSmooth)
                + fwd * Mathf.Lerp(0.08f, -0.06f, leftSmooth)
                + up * Mathf.Lerp(1.02f, 0.94f, leftSmooth);

            if (leftArm != null && leftForeArm != null && leftHand != null)
            {
                Vector3 hintLeft = -right * 0.6f - up * 0.4f - fwd * 0.2f;
                SolveTwoBoneIK(leftArm, leftForeArm, leftHand, leftWristTarget, hintLeft,
                    leftUpperArmLen, leftForeArmLen,
                    defaultLeftUpperArmDir, defaultLeftUpperArmRot,
                    defaultLeftForeArmDir, defaultLeftForeArmRot);

                leftHand.rotation = transform.rotation * Quaternion.Euler(20f, -15f, -25f);
            }

            // Đặt vị trí và góc xoay của Chiếc Rìu gắn chặt trong lòng bàn tay phải
            if (axeTransform != null)
            {
                axeTransform.position = axeGripPosRight;
                axeTransform.rotation = axeRot;
            }

            // UỐN CONG CÁC NGÓN TAY: Tay phải nắm chặt cán rìu (fist), Tay trái thả lỏng tự nhiên
            ApplyFingersGrip();
        }

        private void ApplyFingersGrip()
        {
            // 1. TAY PHẢI: 5 ngón cuộn tròn thành nắm đấm khóa chặt quanh cán rìu
            Quaternion qPhalanx1 = Quaternion.Euler(52f, 0f, 0f);
            Quaternion qPhalanx2 = Quaternion.Euler(72f, 0f, 0f);
            Quaternion qPhalanx3 = Quaternion.Euler(60f, 0f, 0f);

            for (int f = 0; f < 4; f++)
            {
                int baseIdx = f * 3;
                if (rFingers[baseIdx] != null) rFingers[baseIdx].localRotation = defRFingerRots[baseIdx] * qPhalanx1;
                if (rFingers[baseIdx + 1] != null) rFingers[baseIdx + 1].localRotation = defRFingerRots[baseIdx + 1] * qPhalanx2;
                if (rFingers[baseIdx + 2] != null) rFingers[baseIdx + 2].localRotation = defRFingerRots[baseIdx + 2] * qPhalanx3;
            }

            if (rFingers[12] != null) rFingers[12].localRotation = defRFingerRots[12] * Quaternion.Euler(32f, 25f, -30f);
            if (rFingers[13] != null) rFingers[13].localRotation = defRFingerRots[13] * Quaternion.Euler(45f, 0f, 0f);
            if (rFingers[14] != null) rFingers[14].localRotation = defRFingerRots[14] * Quaternion.Euler(50f, 0f, 0f);

            // 2. TAY TRÁI: Thả lỏng tự nhiên (Relaxed Resting Fingers - KHÔNG CẦM RÌU)
            Quaternion qRelax1 = Quaternion.Euler(18f, 0f, 0f);
            Quaternion qRelax2 = Quaternion.Euler(24f, 0f, 0f);
            Quaternion qRelax3 = Quaternion.Euler(20f, 0f, 0f);

            for (int f = 0; f < 4; f++)
            {
                int baseIdx = f * 3;
                if (lFingers[baseIdx] != null) lFingers[baseIdx].localRotation = defLFingerRots[baseIdx] * qRelax1;
                if (lFingers[baseIdx + 1] != null) lFingers[baseIdx + 1].localRotation = defLFingerRots[baseIdx + 1] * qRelax2;
                if (lFingers[baseIdx + 2] != null) lFingers[baseIdx + 2].localRotation = defLFingerRots[baseIdx + 2] * qRelax3;
            }

            if (lFingers[12] != null) lFingers[12].localRotation = defLFingerRots[12] * Quaternion.Euler(15f, -10f, 12f);
            if (lFingers[13] != null) lFingers[13].localRotation = defLFingerRots[13] * Quaternion.Euler(18f, 0f, 0f);
            if (lFingers[14] != null) lFingers[14].localRotation = defLFingerRots[14] * Quaternion.Euler(15f, 0f, 0f);
        }

        private void SolveTwoBoneIK(Transform upperArm, Transform foreArm, Transform hand,
            Vector3 targetPos, Vector3 hintDir,
            float l1, float l2,
            Vector3 defaultUpperDir, Quaternion defaultUpperRot,
            Vector3 defaultForeDir, Quaternion defaultForeRot)
        {
            Vector3 a = upperArm.position;
            Vector3 c = targetPos;
            Vector3 toTarget = c - a;
            float dist = toTarget.magnitude;
            float maxReach = l1 + l2 - 0.002f;
            float clampedDist = Mathf.Clamp(dist, 0.05f, maxReach);
            Vector3 dirAC = toTarget.normalized;

            // Định luật Cosin tìm góc mở tại khớp vai
            float cosAlpha = Mathf.Clamp((l1 * l1 + clampedDist * clampedDist - l2 * l2) / (2f * l1 * clampedDist), -1f, 1f);
            float angleAlpha = Mathf.Acos(cosAlpha) * Mathf.Rad2Deg;

            // Mặt phẳng uốn cong của cánh tay
            Vector3 bendNormal = Vector3.Cross(dirAC, hintDir).normalized;
            if (bendNormal.sqrMagnitude < 0.001f) bendNormal = transform.up;

            // Vector từ vai tới khuỷu tay
            Vector3 dirToElbow = (Quaternion.AngleAxis(angleAlpha, bendNormal) * dirAC).normalized;
            Vector3 elbowPos = a + dirToElbow * l1;

            // Vector từ khuỷu tay tới bàn tay
            Vector3 dirForearm = (c - elbowPos).normalized;

            // Cập nhật hướng xương tự nhiên, không bao giờ bị vặn xoắn
            upperArm.rotation = Quaternion.FromToRotation(defaultUpperDir, dirToElbow) * defaultUpperRot;
            foreArm.rotation = Quaternion.FromToRotation(defaultForeDir, dirForearm) * defaultForeRot;
        }

        private Transform FindChildNamed(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.ToLower().Contains(name.ToLower()))
                    return child;
                Transform result = FindChildNamed(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }

    /// <summary>
    /// ✊ GIỮ BÀN TAY PHẢI NẮM CHẶT CÁN RÌU CHO CÁC ACTOR KHÁC (NHƯ CORNER PLAYER HỒI 6)
    /// </summary>
    public class ActorAxeHoldAnimator : MonoBehaviour
    {
        private Transform rightHand;
        private Transform[] fingers = new Transform[15];
        private Quaternion[] defFingerRots = new Quaternion[15];
        private bool initialized = false;

        public void Init()
        {
            rightHand = FindChildNamed(transform, "RightHand");
            string[] fingerNames = new string[]
            {
                "RightHandIndex1", "RightHandIndex2", "RightHandIndex3",
                "RightHandMiddle1", "RightHandMiddle2", "RightHandMiddle3",
                "RightHandRing1", "RightHandRing2", "RightHandRing3",
                "RightHandPinky1", "RightHandPinky2", "RightHandPinky3",
                "RightHandThumb1", "RightHandThumb2", "RightHandThumb3"
            };
            for (int i = 0; i < 15; i++)
            {
                fingers[i] = FindChildNamed(transform, fingerNames[i]);
                if (fingers[i] != null) defFingerRots[i] = fingers[i].localRotation;
            }
            initialized = true;
        }

        void LateUpdate()
        {
            if (!initialized) Init();

            Quaternion qPhalanx1 = Quaternion.Euler(52f, 0f, 0f);
            Quaternion qPhalanx2 = Quaternion.Euler(72f, 0f, 0f);
            Quaternion qPhalanx3 = Quaternion.Euler(60f, 0f, 0f);

            for (int f = 0; f < 4; f++)
            {
                int baseIdx = f * 3;
                if (fingers[baseIdx] != null) fingers[baseIdx].localRotation = defFingerRots[baseIdx] * qPhalanx1;
                if (fingers[baseIdx + 1] != null) fingers[baseIdx + 1].localRotation = defFingerRots[baseIdx + 1] * qPhalanx2;
                if (fingers[baseIdx + 2] != null) fingers[baseIdx + 2].localRotation = defFingerRots[baseIdx + 2] * qPhalanx3;
            }

            if (fingers[12] != null) fingers[12].localRotation = defFingerRots[12] * Quaternion.Euler(32f, 25f, -30f);
            if (fingers[13] != null) fingers[13].localRotation = defFingerRots[13] * Quaternion.Euler(45f, 0f, 0f);
            if (fingers[14] != null) fingers[14].localRotation = defFingerRots[14] * Quaternion.Euler(50f, 0f, 0f);
        }

        private Transform FindChildNamed(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.ToLower().Contains(name.ToLower()))
                    return child;
                Transform result = FindChildNamed(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }

    /// <summary>
    /// 🩸 ĐIỀU KHIỂN CỬ ĐỘNG BÀN TAY CỦA CHÍNH PLAYER (POV AWAKENING HANDS)
    /// Cử động 2 cánh tay của model Player Ch15 (áo rằn ri, găng tay chiến thuật dính máu)
    /// Đưa 2 bàn tay lên trước mắt, run rẩy bàng hoàng trong mê man chấn thương.
    /// </summary>
    public class POVPlayerHandsController : MonoBehaviour
    {
        public Transform leftArm;
        public Transform leftForeArm;
        public Transform leftHand;
        public Transform rightArm;
        public Transform rightForeArm;
        public Transform rightHand;
        public Transform head;

        private Quaternion baseLeftArmRot;
        private Quaternion baseLeftForeArmRot;
        private Quaternion baseLeftHandRot;
        private Quaternion baseRightArmRot;
        private Quaternion baseRightForeArmRot;
        private Quaternion baseRightHandRot;

        public float sceneTimer = 0f;
        private bool initializedBones = false;

        public void InitBones()
        {
            leftArm = FindChildNamed(transform, "LeftArm");
            leftForeArm = FindChildNamed(transform, "LeftForeArm");
            leftHand = FindChildNamed(transform, "LeftHand");
            rightArm = FindChildNamed(transform, "RightArm");
            rightForeArm = FindChildNamed(transform, "RightForeArm");
            rightHand = FindChildNamed(transform, "RightHand");
            head = FindChildNamed(transform, "Head");

            if (leftArm != null) baseLeftArmRot = leftArm.localRotation;
            if (leftForeArm != null) baseLeftForeArmRot = leftForeArm.localRotation;
            if (leftHand != null) baseLeftHandRot = leftHand.localRotation;
            if (rightArm != null) baseRightArmRot = rightArm.localRotation;
            if (rightForeArm != null) baseRightForeArmRot = rightForeArm.localRotation;
            if (rightHand != null) baseRightHandRot = rightHand.localRotation;

            if (head != null) head.localScale = Vector3.zero;

            initializedBones = true;
        }

        void Start()
        {
            if (!initializedBones) InitBones();
        }

        void LateUpdate()
        {
            if (!initializedBones) InitBones();

            sceneTimer += Time.deltaTime;
            float t = sceneTimer;

            // Tiến trình đưa 2 bàn tay lên trước mắt:
            // 0.0s - 1.2s: Hai tay buông xuôi dưới mặt đất
            // 1.2s - 2.8s: Từ từ nâng 2 cánh tay đẫm máu lên trước mắt
            // 2.8s - 4.2s: Giữ 2 bàn tay trước ống kính, xoay lòng bàn tay vào mắt, run rẩy bàng hoàng
            // 4.2s - 6.0s: Hạ tay xuống chống đẩy mặt đất gượng đứng dậy
            float raiseProgress = 0f;
            float palmTurnProgress = 0f;

            if (t < 1.2f)
            {
                raiseProgress = 0f;
                palmTurnProgress = 0f;
            }
            else if (t < 2.8f)
            {
                float p = (t - 1.2f) / 1.6f;
                raiseProgress = Mathf.SmoothStep(0f, 1f, p);
                palmTurnProgress = Mathf.SmoothStep(0f, 0.9f, p);
            }
            else if (t < 4.2f)
            {
                raiseProgress = 1f;
                palmTurnProgress = 1f;
            }
            else
            {
                float p = (t - 4.2f) / 1.6f;
                raiseProgress = Mathf.Lerp(1f, 0.15f, Mathf.SmoothStep(0f, 1f, p));
                palmTurnProgress = Mathf.Lerp(1f, 0f, Mathf.SmoothStep(0f, 1f, p));
            }

            // Rung rẩy bàng hoàng do chấn động và sốc (Perlin noise tremor)
            float tremorIntensity = (t >= 2.0f && t <= 4.4f) ? 1f : 0.25f;
            float shakeX = (Mathf.PerlinNoise(t * 24f, 0f) * 2f - 1f) * 4.8f * tremorIntensity;
            float shakeY = (Mathf.PerlinNoise(0f, t * 24f) * 2f - 1f) * 4.8f * tremorIntensity;
            float shakeZ = (Mathf.PerlinNoise(t * 20f, t * 20f) * 2f - 1f) * 3.6f * tremorIntensity;

            float armLift = Mathf.Lerp(-42f, 48f, raiseProgress);
            float armInward = Mathf.Lerp(0f, 22f, raiseProgress);
            float foreArmBend = Mathf.Lerp(15f, 76f, raiseProgress);
            float palmTurn = Mathf.Lerp(0f, 95f, palmTurnProgress);

            Transform camT = transform.parent != null ? transform.parent : transform;
            Vector3 camRight = camT.right;
            Vector3 camUp = camT.up;
            Vector3 camForward = camT.forward;

            // 1. Tay Phải (Right Arm / ForeArm / Hand)
            if (rightArm != null)
            {
                Quaternion pitchR = Quaternion.AngleAxis(armLift + shakeY, camRight);
                Quaternion yawR = Quaternion.AngleAxis(-armInward, camUp);
                rightArm.rotation = pitchR * yawR * (rightArm.parent != null ? rightArm.parent.rotation * baseRightArmRot : baseRightArmRot);
            }
            if (rightForeArm != null)
            {
                Quaternion pitchForeR = Quaternion.AngleAxis(foreArmBend + shakeX, camRight);
                Quaternion rollForeR = Quaternion.AngleAxis(-palmTurn * 0.4f, camForward);
                rightForeArm.rotation = pitchForeR * rollForeR * (rightForeArm.parent != null ? rightForeArm.parent.rotation * baseRightForeArmRot : baseRightForeArmRot);
            }
            if (rightHand != null)
            {
                Quaternion wristR = Quaternion.AngleAxis(-palmTurn * 0.6f + shakeZ, camForward);
                rightHand.rotation = wristR * (rightHand.parent != null ? rightHand.parent.rotation * baseRightHandRot : baseRightHandRot);
            }

            // 2. Tay Trái (Left Arm / ForeArm / Hand)
            if (leftArm != null)
            {
                Quaternion pitchL = Quaternion.AngleAxis(armLift - shakeY, camRight);
                Quaternion yawL = Quaternion.AngleAxis(armInward, camUp);
                leftArm.rotation = pitchL * yawL * (leftArm.parent != null ? leftArm.parent.rotation * baseLeftArmRot : baseLeftArmRot);
            }
            if (leftForeArm != null)
            {
                Quaternion pitchForeL = Quaternion.AngleAxis(foreArmBend - shakeX, camRight);
                Quaternion rollForeL = Quaternion.AngleAxis(palmTurn * 0.4f, camForward);
                leftForeArm.rotation = pitchForeL * rollForeL * (leftForeArm.parent != null ? leftForeArm.parent.rotation * baseLeftForeArmRot : baseLeftForeArmRot);
            }
            if (leftHand != null)
            {
                Quaternion wristL = Quaternion.AngleAxis(palmTurn * 0.6f - shakeZ, camForward);
                leftHand.rotation = wristL * (leftHand.parent != null ? leftHand.parent.rotation * baseLeftHandRot : baseLeftHandRot);
            }

            // Luôn đảm bảo xương đầu co về 0 để không chắn camera
            if (head != null)
            {
                head.localScale = Vector3.zero;
            }
        }

        private Transform FindChildNamed(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.ToLower().Contains(name.ToLower()))
                    return child;
                Transform result = FindChildNamed(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }
}

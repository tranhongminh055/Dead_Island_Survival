using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace HorrorGame.UI
{
    public enum MenuState
    {
        Main,
        Settings,
        Controls
    }

    public enum MenuBackgroundScaleMode
    {
        StretchToFill,   // Lấp đầy toàn màn hình, hiển thị 100% hình ảnh không bị phóng to cắt góc (Mặc định chuẩn)
        FitCenter,       // Giữ chuẩn tỉ lệ gốc 4:3 của ảnh, căn giữa
        ScaleAndCrop     // Phóng to cắt xén góc
    }

    /// <summary>
    /// Hệ thống Menu chuẩn AAA cho Dead Island Survival:
    /// - Main Menu phong cách điện ảnh (Left-Aligned Layout phong cách Resident Evil / The Last of Us / Dead Island).
    /// - Không còn hộp đen che mất tác phẩm Avatar game; hình nền hiển thị trọn vẹn, hiệu ứng chuyển màu Smoky Gradient bên góc trái.
    /// - Cô lập hoàn toàn âm thanh: Menu chỉ phát nhạc nền u tối, KHÔNG phát âm thanh trailer / máy bay rơi khi ở menu.
    /// - Pause Menu In-Game (ESC) mượt mà, tạm dừng âm thanh môi trường & thời gian.
    /// - Cài đặt hệ thống (Âm lượng, Độ nhạy chuột, Đồ họa, Toàn màn hình) và Hướng dẫn phím điều khiển.
    /// </summary>
    public class GameMenuManager : MonoBehaviour
    {
        public static GameMenuManager Instance { get; private set; }
        public static bool IsPaused { get; private set; }

        [Header("Chế độ Menu")]
        [Tooltip("Tick nếu đặt trong Scene MainMenu. Bỏ tick nếu dùng làm Pause Menu trong màn chơi (Scene_A).")]
        public bool isMainMenuScene = false;

        [Header("Tên Scene Game")]
        public string gameplaySceneName = "Scene_A";
        public string mainMenuSceneName = "MainMenu";

        [Header("Ảnh & Video Nền Động (Phong Cách Outlast 1)")]
        [Tooltip("Kéo file Video (.mp4 / .webm) vào đây nếu muốn phát video động làm nền Main Menu")]
        public VideoClip menuBackgroundVideo;
        [Tooltip("Bật hiệu ứng chuyển cảnh động, máy quay cầm tay REC, nhiễu VHS, sấm chớp giống Outlast 1")]
        public bool enableOutlastEffects = true;
        [Tooltip("Ảnh nền cho Menu Game (avata chinh thuc.png)")]
        public Texture2D menuBackgroundTexture;
        [Tooltip("Ảnh nền phụ thứ 2 để cắt chuyển cảnh xen kẽ")]
        public Texture2D menuSecondaryTexture;

        [Header("Chế độ hiển thị ảnh nền")]
        [Tooltip("StretchToFill: Hiển thị trọn vẹn 100% toàn bộ chi tiết ảnh (chữ SURVIVAL, căn nhà, bầu trời), không bị phóng to cắt xén góc.")]
        public MenuBackgroundScaleMode backgroundScaleMode = MenuBackgroundScaleMode.StretchToFill;

        [Header("Âm thanh Menu")]
        public AudioClip menuMusic;
        public AudioClip buttonClickSound;
        public AudioClip buttonHoverSound;

        [Header("Tự động hiện Menu khi bấm Play trong Editor")]
        [Tooltip("Nếu bật: Khi bấm Play ngay trong Scene_A, Menu chính sẽ hiện và trailer/âm thanh máy bay được hoãn lại cho tới khi bấm 'Bắt đầu sinh tồn'.")]
        public bool showMenuOnPlayInGameScene = true;

        public static bool hasStartedFromMainMenu = false;

        // Trạng thái giao diện
        private MenuState currentState = MenuState.Main;
        private AudioSource audioSource;
        private AudioSource ambientWindSource;
        private AudioClip ambientLayerClip;
        private AudioListener tempMenuListener;
        private System.Collections.Generic.List<AudioListener> pausedSceneListeners = new System.Collections.Generic.List<AudioListener>();
        private bool loggedMenuAudioOnce = false;
        private int lastHoveredButton = -1;

        // Cài đặt game (Lưu vào PlayerPrefs)
        private float masterVolume = 1f;
        private float mouseSensitivity = 2f;
        private int graphicsQualityIndex = 2;
        private bool isFullscreen = true;

        // Dynamic Textures & GUIStyles (Khởi tạo mượt mà ở runtime)
        private Texture2D leftGradientTex;
        private Texture2D topVignetteTex;
        private Texture2D bottomVignetteTex;
        private Texture2D btnNormalGradientTex;
        private Texture2D btnHoverGradientTex;
        private Texture2D accentRedTex;
        private Texture2D lineDividerTex;
        private Texture2D modalBgTex;
        private Texture2D fullDarkVeilTex;
        private Texture2D badgeBgTex;

        private GUIStyle brandTagStyle;
        private GUIStyle titleStyle;
        private GUIStyle titleShadowStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle buttonTextStyle;
        private GUIStyle buttonHoverTextStyle;
        private GUIStyle modalHeaderStyle;
        private GUIStyle modalTagStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueLabelStyle;
        private GUIStyle keyBadgeStyle;
        private GUIStyle keyDescStyle;
        private bool stylesInitialized = false;

        // Hiệu ứng động phong cách Outlast 1 (VideoPlayer + Multi-Shot Pan/Zoom + VHS Glitch + Camcorder HUD + Lightning)
        private VideoPlayer bgVideoPlayer;
        private RenderTexture bgVideoRT;
        private Texture2D scanlineTex;
        private Texture2D staticNoiseTex;
        private Texture2D camcorderWhiteTex;
        private GUIStyle camcorderStyle;
        private GUIStyle camcorderRedStyle;

        private float outlastTime = 0f;
        private int currentShotIndex = 0;
        private float shotTimer = 0f;
        private const float SHOT_DURATION = 5.5f;
        private float glitchBurstTimer = 0f;
        private float nextRandomGlitchTime = 3.2f;
        private float lightningFlashAlpha = 0f;
        private float nextLightningTime = 6.5f;
        private Vector2[] dustMotes;
        private float[] dustSpeeds;

        // Nền 3D động thời gian thực (KHÔNG dùng ảnh tĩnh!) cho MainMenu.unity
        private GameObject liveMenuStageRoot;
        private Camera liveMenuCam;
        private Light liveFlickerLight;
        private Light liveLightningLight;
        private Light liveRedFlareLight;
        private Transform liveZombieTransform;
        private float liveZombieFloorLocalY = 0f;
        private Renderer[] liveZombieRenderers;
        private bool liveStageBuilt = false;

#if UNITY_EDITOR
        void OnValidate()
        {
            if (menuBackgroundTexture == null)
            {
                menuBackgroundTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Flooded_Grounds/avata game/avata chinh thuc.png");
            }
        }
#endif

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            // Tự động nạp ảnh nền phụ thứ 2 để cắt cảnh luân phiên nếu có
#if UNITY_EDITOR
            if (menuSecondaryTexture == null)
            {
                menuSecondaryTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Flooded_Grounds/avata game/10ff3322-8a41-4636-bade-bf7fff81406d.jpg");
            }
#endif
            // Tự động nạp VideoClip từ Resources/MenuVideo nếu người dùng có bỏ vào
            if (menuBackgroundVideo == null)
            {
                menuBackgroundVideo = Resources.Load<VideoClip>("MenuVideo");
            }
            SetupMenuVideoPlayer();
            InitOutlastDustMotes();

            // Tự động nạp ảnh nền Avatar chính thức
            if (menuBackgroundTexture == null)
            {
                menuBackgroundTexture = Resources.Load<Texture2D>("MenuBackground");
            }
#if UNITY_EDITOR
            if (menuBackgroundTexture == null)
            {
                menuBackgroundTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Flooded_Grounds/avata game/avata chinh thuc.png");
            }
#endif

            // Nạp cài đặt người dùng
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
            graphicsQualityIndex = PlayerPrefs.GetInt("GraphicsQuality", QualitySettings.GetQualityLevel());
            isFullscreen = PlayerPrefs.GetInt("FullscreenV2", 1) == 1;

            // Áp dụng cài đặt
            Application.runInBackground = true;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.audioMasterMute = false;
#endif
            AudioListener.volume = Mathf.Max(0.1f, masterVolume);
            QualitySettings.SetQualityLevel(graphicsQualityIndex);
#if !UNITY_EDITOR
            if (isFullscreen)
            {
                Resolution nativeRes = Screen.currentResolution;
                Screen.SetResolution(nativeRes.width, nativeRes.height, true);
            }
            else
            {
                Screen.fullScreen = false;
            }
#else
            Screen.fullScreen = isFullscreen;
#endif

            // Thiết lập AudioSource cho Menu (ép 2D stereo, không bị mute/pause)
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.ignoreListenerPause = true;
            audioSource.spatialBlend = 0f;
            audioSource.mute = false;
            audioSource.playOnAwake = false;

            // Tìm nạp đúng file nhạc nền menu: Assets/Flooded_Grounds/sound main menu/lemonmusiclab-dark-ambient-soundscape-505384.mp3
#if UNITY_EDITOR
            AudioClip directClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/sound main menu/lemonmusiclab-dark-ambient-soundscape-505384.mp3");
            if (directClip != null) menuMusic = directClip;
#endif
            if (menuMusic == null)
            {
                menuMusic = Resources.Load<AudioClip>("MenuMusic");
                if (menuMusic == null)
                    menuMusic = Resources.Load<AudioClip>("Background");
#if UNITY_EDITOR
                if (menuMusic == null)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Flooded_Grounds/sound main menu" });
                    if (guids.Length > 0)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                        menuMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    }
                }
#endif
            }

            if (buttonClickSound == null)
            {
                buttonClickSound = Resources.Load<AudioClip>("Taps");
#if UNITY_EDITOR
                if (buttonClickSound == null)
                    buttonClickSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/Taps.mp3");
#endif
            }
            if (buttonHoverSound == null)
            {
                buttonHoverSound = buttonClickSound;
            }

            // Nạp thêm lớp nhạc nền kinh dị tần số trung/cao (Background / WindHowl) để loa laptop vẫn nghe cực rõ ngay giây đầu tiên!
            ambientLayerClip = Resources.Load<AudioClip>("Background");
            if (ambientLayerClip == null)
                ambientLayerClip = Resources.Load<AudioClip>("Sounds/WindHowl");
#if UNITY_EDITOR
            if (ambientLayerClip == null)
                ambientLayerClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/Background.mp3");
#endif
            if (ambientWindSource == null)
            {
                GameObject ambObj = new GameObject("[Menu_AmbientHorrorLayer]");
                ambObj.transform.SetParent(transform, false);
                ambientWindSource = ambObj.AddComponent<AudioSource>();
                ambientWindSource.playOnAwake = false;
                ambientWindSource.ignoreListenerPause = true;
                ambientWindSource.spatialBlend = 0f;
                ambientWindSource.loop = true;
                ambientWindSource.mute = false;
            }

            // Xử lý luồng cảnh
            if (isMainMenuScene)
            {
                // Màn hình Menu chính biệt lập
                IsPaused = false;
                Time.timeScale = 1f;
                AudioListener.pause = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Đảm bảo luôn có ít nhất 1 AudioListener đang bật
                EnsureActiveAudioListener();

                // Tắt / ngắt các âm thanh rác trong scene nếu có
                MuteOtherSceneAudios();

                StartMenuAudioPlayback();
            }
            else
            {
                // Khi đang ở trong màn chơi Scene_A:
                if (!hasStartedFromMainMenu && showMenuOnPlayInGameScene)
                {
                    // Chuyển sang chế độ Main Menu hiển thị trên nền 3D Outlast (giữ Time.timeScale = 1f y hệt MainMenu.unity để FMOD phát nhạc 100% ngay lập tức!)
                    isMainMenuScene = true;
                    IsPaused = false;
                    Time.timeScale = 1f;
                    AudioListener.pause = false;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;

                    // Đảm bảo luôn có ít nhất 1 AudioListener đang bật để phát nhạc nền Menu
                    EnsureActiveAudioListener();

                    // Cách ly tuyệt đối: Tắt ngay âm thanh trailer máy bay trong Scene_A
                    MuteOtherSceneAudios();

                    StartMenuAudioPlayback();
                }
                else
                {
                    // Chơi game bình thường
                    IsPaused = false;
                    Time.timeScale = 1f;
                    AudioListener.pause = false;
                }
            }
        }

        void Start()
        {
            if (isMainMenuScene)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.audioMasterMute = false;
#endif
                EnsureActiveAudioListener();
                MuteOtherSceneAudios();
                StartMenuAudioPlayback();
            }
        }

        private void StartMenuAudioPlayback()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.audioMasterMute = false;
#endif
            AudioListener.pause = false;
            AudioListener.volume = Mathf.Max(0.1f, masterVolume);

            if (menuMusic != null && audioSource != null)
            {
                audioSource.clip = menuMusic;
                audioSource.loop = true;
                audioSource. priority = 0; // Ưu tiên FMOD cao nhất tuyệt đối (0), không bao giờ bị cướp kênh bởi 32 Real Voices trong Scene_A!
                audioSource.spatialBlend = 0f;
                audioSource.mute = false;
                audioSource.pitch = 1f;
                audioSource.ignoreListenerPause = true;
                audioSource.ignoreListenerVolume = false;
                audioSource.volume = Mathf.Max(0.25f, masterVolume);
                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                }
            }

            if (!loggedMenuAudioOnce)
            {
                loggedMenuAudioOnce = true;
                Debug.Log("🔊 [MenuAudio] Đã bật nhạc Menu! Clip=" + (menuMusic != null ? menuMusic.name : "null") + " | isPlaying=" + (audioSource != null && audioSource.isPlaying) + " | Vol=" + masterVolume);
            }
        }

        private void EnsureActiveAudioListener()
        {
            // Tạo 1 object AudioListener hoàn toàn độc lập & sạch 100% riêng cho Menu (không chung object với AudioSource/Filter/MissingScript!)
            if (tempMenuListener == null)
            {
                GameObject alObj = new GameObject("[Menu_Dedicated_AudioListener]");
                alObj.transform.SetParent(transform, false);
                tempMenuListener = alObj.AddComponent<AudioListener>();
            }
            tempMenuListener.enabled = true;

            // Tạm tắt các AudioListener khác trong scene lúc đang ở Menu để tránh trùng lặp hoặc lỗi DSP trên Camera
            AudioListener[] listeners = FindObjectsOfType<AudioListener>();
            for (int i = 0; i < listeners.Length; i++)
            {
                AudioListener l = listeners[i];
                if (l != null && l != tempMenuListener && l.enabled)
                {
                    l.enabled = false;
                    if (!pausedSceneListeners.Contains(l))
                        pausedSceneListeners.Add(l);
                }
            }
        }

        private void MuteOtherSceneAudios()
        {
            AudioSource[] all = FindObjectsOfType<AudioSource>();
            foreach (var a in all)
            {
                if (a != null && a != audioSource && a != ambientWindSource && a.isPlaying)
                {
                    a.Pause();
                }
            }
        }

        private void UnmuteOtherSceneAudios()
        {
            AudioSource[] all = FindObjectsOfType<AudioSource>();
            foreach (var a in all)
            {
                if (a == null || a == audioSource || a == ambientWindSource) continue;

                // Tuyệt đối không bật lại âm thanh mưa gió nếu đang xem trailer hoặc chưa vào game thật
                if (HorrorGame.Environment.Weather.WeatherSystem.Instance != null)
                {
                    if (a == HorrorGame.Environment.Weather.WeatherSystem.Instance.rainLightAudio ||
                        a == HorrorGame.Environment.Weather.WeatherSystem.Instance.rainHeavyAudio ||
                        a == HorrorGame.Environment.Weather.WeatherSystem.Instance.windAudio)
                    {
                        continue;
                    }
                }

                // Phục hồi audio đã bị Pause bởi MuteOtherSceneAudios + bật lại các source có clip (vì PlayOnAwake đã tắt)
                a.UnPause();
                if (!a.isPlaying && a.clip != null && a.loop)
                {
                    a.Play();
                }
            }
        }

        void Update()
        {
            // Nếu ở Main Menu Scene -> Luôn hiện chuột & duy trì âm thanh Menu liên tục (tránh bị script khác tắt AudioListener trong Start!)
            if (isMainMenuScene)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                AudioListener.pause = false;
                Time.timeScale = 1f; // Giữ timeScale = 1f như MainMenu.unity để FMOD luôn phát nhạc ngay từ giây đầu
                EnsureActiveAudioListener();
                MuteOtherSceneAudios();
                if (audioSource != null && menuMusic != null && !audioSource.isPlaying)
                {
                    StartMenuAudioPlayback();
                }
                return;
            }

            // Trong màn chơi (In-Game): Nhấn ESC để Tạm Dừng / Tiếp Tục
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Không bật Pause nếu đang trong Cutscene, Bạn Đã Chết, hoặc Màn Hình Ending
                if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;
                if (PlayerDeathUI.IsShowingDeathScreen) return;
                if (GameEndingManager.IsEndingUIActive) return;

                // Nếu túi đồ đang mở -> để túi đồ tự đóng trước
                if (Inventory.InventoryManager.Instance != null && Inventory.InventoryManager.Instance.isInventoryOpen) return;

                // Nếu đang mở bảng con (Settings hoặc Controls), ESC sẽ quay lại menu chính của Pause
                if (IsPaused && currentState != MenuState.Main)
                {
                    currentState = MenuState.Main;
                    PlayClickSound();
                    return;
                }

                TogglePause();
            }
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
            currentState = MenuState.Main;

            if (IsPaused)
            {
                Time.timeScale = 0f;
                AudioListener.pause = true; // Tạm dừng toàn bộ âm thanh gameplay
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                PlayClickSound();
            }
            else
            {
                AudioListener.pause = false; // Phục hồi âm thanh gameplay
                Time.timeScale = 1f;
                if (Inventory.InventoryManager.Instance == null || !Inventory.InventoryManager.Instance.isInventoryOpen)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                PlayClickSound();
            }
        }

        public void ResumeGame()
        {
            if (IsPaused) TogglePause();
        }

        public void RestartGame()
        {
            AudioListener.pause = false;
            Time.timeScale = 1f;
            IsPaused = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void LoadGameplay()
        {
            if (isMainMenuScene && SceneManager.GetActiveScene().name == gameplaySceneName)
            {
                // Người chơi đang bấm Play trực tiếp trong Scene_A -> Dọn sân khấu 3D menu & bắt đầu chơi ngay lập tức!
                CleanupLiveOutlast3DStage();
                isMainMenuScene = false;
                IsPaused = false;
                AudioListener.pause = false;
                Time.timeScale = 1f;

                if (audioSource != null && audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
                if (ambientWindSource != null && ambientWindSource.isPlaying)
                {
                    ambientWindSource.Stop();
                }
                if (tempMenuListener != null)
                {
                    tempMenuListener.enabled = false;
                }
                for (int i = 0; i < pausedSceneListeners.Count; i++)
                {
                    if (pausedSceneListeners[i] != null)
                        pausedSceneListeners[i].enabled = true;
                }
                pausedSceneListeners.Clear();

                UnmuteOtherSceneAudios();

                if (Inventory.InventoryManager.Instance == null || !Inventory.InventoryManager.Instance.isInventoryOpen)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                PlayClickSound();
            }
            else
            {
                // Nạp từ scene MainMenu vào Scene_A
                hasStartedFromMainMenu = true;
                AudioListener.pause = false;
                Time.timeScale = 1f;
                IsPaused = false;
                SceneManager.LoadScene(gameplaySceneName);
            }
        }

        public void LoadMainMenu()
        {
            AudioListener.pause = false;
            Time.timeScale = 1f;
            IsPaused = false;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void QuitGame()
        {
            PlayClickSound();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void PlayClickSound()
        {
            if (buttonClickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(buttonClickSound, masterVolume);
            }
        }

        // ========================== GIAO DIỆN ONGUI ĐỈNH CAO ==========================

        void OnGUI()
        {
            // Tuyệt đối không vẽ đè menu lên màn hình khi đang quay Trailer điện ảnh!
            if (HorrorGame.Cutscenes.CinematicTrailerDirector.IsTrailerRunning) return;

            // Chỉ vẽ khi: ở Main Menu Scene HOẶC đang trong trạng thái Pause In-Game
            if (!isMainMenuScene && !IsPaused) return;

            InitStyles();

            float sw = Screen.width;
            float sh = Screen.height;

            // 1. VẼ NỀN VIDEO ĐỘNG / CAMERA 3D TRỰC TIẾP PHONG CÁCH OUTLAST 1 (KHÔNG DÙNG ẢNH TĨNH!)
            if (isMainMenuScene)
            {
                EnsureLiveOutlast3DStage();
                UpdateOutlastAnimations();
                DrawOutlastAnimatedBackground(sw, sh);

                // Dải Smoky Gradient góc trái: Tạo bóng râm mờ đen phía sau cột nút bấm (36% màn hình)
                float gradWidth = Mathf.Clamp(sw * 0.36f, 320f, 440f);
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(0, 0, gradWidth, sh), leftGradientTex);

                if (enableOutlastEffects)
                {
                    DrawOutlastAtmosphereAndCamcorderHUD(sw, sh);
                }
            }
            else
            {
                // Khi Tạm Dừng In-Game (ESC): Phủ một lớp kính mờ u tối điện ảnh
                GUI.color = new Color(0.02f, 0.02f, 0.03f, 0.82f);
                GUI.DrawTexture(new Rect(0, 0, sw, sh), fullDarkVeilTex);

                // Thêm dải gradient bên trái
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(0, 0, 480f, sh), leftGradientTex);
            }

            // 2. VẼ NỘI DUNG THEO TRẠNG THÁI MENU
            switch (currentState)
            {
                case MenuState.Main:
                    DrawLeftAlignedMainMenu(sw, sh);
                    break;
                case MenuState.Settings:
                    DrawSettingsModal(sw, sh);
                    break;
                case MenuState.Controls:
                    DrawControlsModal(sw, sh);
                    break;
            }
        }

        /// <summary>
        /// Giao diện Main Menu & Pause Menu dạng Left-Aligned AAA (phong cách The Last of Us, Resident Evil)
        /// </summary>
        private void DrawLeftAlignedMainMenu(float sw, float sh)
        {
            float startX = Mathf.Clamp(sw * 0.065f, 50f, 110f);
            float currentY = sh * 0.20f;

            // --- KHỐI TIÊU ĐỀ THƯƠNG HIỆU ---
            if (isMainMenuScene)
            {
                // Tag thể loại
                GUI.Label(new Rect(startX, currentY, 400f, 22f), "SURVIVAL HORROR EXPERIENCE", brandTagStyle);
                currentY += 24f;

                // Tên Game chính với bóng đổ + giật glitch nhẹ khi nhiễu VHS (Outlast 1 style)
                string mainTitle = "DEAD ISLAND";
                float glitchJitterX = (enableOutlastEffects && glitchBurstTimer > 0f) ? Random.Range(-5f, 5f) : 0f;
                float glitchJitterY = (enableOutlastEffects && glitchBurstTimer > 0f) ? Random.Range(-2f, 2f) : 0f;
                GUI.Label(new Rect(startX + 2f + glitchJitterX, currentY + 2f + glitchJitterY, 400f, 52f), mainTitle, titleShadowStyle);
                GUI.Label(new Rect(startX + glitchJitterX, currentY + glitchJitterY, 400f, 52f), mainTitle, titleStyle);
                currentY += 56f;

                // Đường kẻ line máu sắc sảo (Hairline Divider)
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(startX, currentY, 340f, 2f), lineDividerTex);
                currentY += 8f;

                // Phụ đề mô tả
                GUI.Label(new Rect(startX, currentY, 400f, 22f), "BẢN ĐỒ ĐẢO CHẾT • SINH TỒN HẰNG ĐÊM", subtitleStyle);
                currentY += 45f;
            }
            else
            {
                // PAUSE MENU HEADER
                GUI.Label(new Rect(startX, currentY, 400f, 22f), "TRÒ CHƠI ĐANG TẠM DỪNG", brandTagStyle);
                currentY += 24f;

                string pauseTitle = "TẠM DỪNG";
                GUI.Label(new Rect(startX + 2f, currentY + 2f, 400f, 52f), pauseTitle, titleShadowStyle);
                GUI.Label(new Rect(startX, currentY, 400f, 52f), pauseTitle, titleStyle);
                currentY += 56f;

                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(startX, currentY, 340f, 2f), lineDividerTex);
                currentY += 8f;

                GUI.Label(new Rect(startX, currentY, 400f, 22f), "NHẤN ESC HOẶC TIẾP TỤC ĐỂ QUAY LẠI TRẬN ĐẤU", subtitleStyle);
                currentY += 45f;
            }

            // --- CỘT NÚT BẤM INTERACTIVE ---
            float btnWidth = 340f;
            float btnHeight = 46f;
            float btnGap = 11f;
            int btnIndex = 0;

            if (isMainMenuScene)
            {
                // 1. Bắt đầu sinh tồn
                if (DrawSleekButton(new Rect(startX, currentY + (btnIndex++) * (btnHeight + btnGap), btnWidth, btnHeight), "BẮT ĐẦU SINH TỒN", "▶", 1))
                {
                    PlayClickSound();
                    LoadGameplay();
                }

                // 2. Cài đặt hệ thống
                if (DrawSleekButton(new Rect(startX, currentY + (btnIndex++) * (btnHeight + btnGap), btnWidth, btnHeight), "CÀI ĐẶT HỆ THỐNG", "⚙", 2))
                {
                    PlayClickSound();
                    currentState = MenuState.Settings;
                }

                // 3. Hướng dẫn phím điều khiển
                if (DrawSleekButton(new Rect(startX, currentY + (btnIndex++) * (btnHeight + btnGap), btnWidth, btnHeight), "HƯỚNG DẪN ĐIỀU KHIỂN", "⌨", 3))
                {
                    PlayClickSound();
                    currentState = MenuState.Controls;
                }

                // 4. Thoát game
                if (DrawSleekButton(new Rect(startX, currentY + (btnIndex++) * (btnHeight + btnGap), btnWidth, btnHeight), "THOÁT GAME", "✕", 4))
                {
                    QuitGame();
                }
            }
            else
            {
                // IN-GAME PAUSE BUTTONS
                if (DrawSleekButton(new Rect(startX, currentY + (btnIndex++) * (btnHeight + btnGap), btnWidth, btnHeight), "TIẾP TỤC CHƠI", "▶", 10))
                {
                    ResumeGame();
                }

                if (DrawSleekButton(new Rect(startX, currentY + (btnIndex++) * (btnHeight + btnGap), btnWidth, btnHeight), "CÀI ĐẶT HỆ THỐNG", "⚙", 11))
                {
                    PlayClickSound();
                    currentState = MenuState.Settings;
                }

                if (DrawSleekButton(new Rect(startX, currentY + (btnIndex++) * (btnHeight + btnGap), btnWidth, btnHeight), "HƯỚNG DẪN ĐIỀU KHIỂN", "⌨", 12))
                {
                    PlayClickSound();
                    currentState = MenuState.Controls;
                }

                if (DrawSleekButton(new Rect(startX, currentY + (btnIndex++) * (btnHeight + btnGap), btnWidth, btnHeight), "CHƠI LẠI MÀN NÀY", "↺", 13))
                {
                    PlayClickSound();
                    RestartGame();
                }

                if (DrawSleekButton(new Rect(startX, currentY + (btnIndex++) * (btnHeight + btnGap), btnWidth, btnHeight), "VỀ MENU CHÍNH", "⌂", 14))
                {
                    PlayClickSound();
                    LoadMainMenu();
                }

                if (DrawSleekButton(new Rect(startX, currentY + (btnIndex++) * (btnHeight + btnGap), btnWidth, btnHeight), "THOÁT RA DESKTOP", "✕", 15))
                {
                    QuitGame();
                }
            }

            // --- FOOTER DƯỚI GÓC TRÁI ---
            GUI.Label(new Rect(startX, sh - 42f, 400f, 22f), "v1.2 • Unity 2017 Horror Engine • Dead Island Survival", subtitleStyle);
        }

        /// <summary>
        /// Nút bấm phong cách AAA: Kính tối trong suốt, thanh đỏ phát sáng bên trái, text trượt mượt mà khi rê chuột
        /// </summary>
        private bool DrawSleekButton(Rect rect, string text, string icon, int buttonId)
        {
            Vector2 mousePos = Event.current.mousePosition;
            bool isHovered = rect.Contains(mousePos);

            // Xử lý âm thanh Hover nhẹ
            if (isHovered && lastHoveredButton != buttonId)
            {
                lastHoveredButton = buttonId;
                if (buttonHoverSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(buttonHoverSound, masterVolume * 0.5f);
                }
            }
            else if (!isHovered && lastHoveredButton == buttonId)
            {
                lastHoveredButton = -1;
            }

            // 1. Vẽ nền gradient của nút
            GUI.color = Color.white;
            GUI.DrawTexture(rect, isHovered ? btnHoverGradientTex : btnNormalGradientTex);

            // 2. Vẽ thanh Accent đỏ bên mép trái
            float barWidth = isHovered ? 6f : 3f;
            Color barColor = isHovered ? new Color(1f, 0.22f, 0.22f, 1f) : new Color(0.6f, 0.12f, 0.12f, 0.75f);
            GUI.color = barColor;
            GUI.DrawTexture(new Rect(rect.x, rect.y, barWidth, rect.height), accentRedTex);

            // 3. Đường viền siêu mảnh trên và dưới khi hover
            if (isHovered)
            {
                GUI.color = new Color(0.95f, 0.25f, 0.25f, 0.6f);
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), accentRedTex);
                GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), accentRedTex);
            }

            // 4. Vẽ Text & Icon (Trượt nhẹ +8px sang phải khi hover)
            float textOffset = isHovered ? 28f : 20f;
            string displayText = icon + "   " + text;

            GUI.color = Color.white;
            GUIStyle curStyle = isHovered ? buttonHoverTextStyle : buttonTextStyle;
            GUI.Label(new Rect(rect.x + textOffset, rect.y, rect.width - textOffset, rect.height), displayText, curStyle);

            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        /// <summary>
        /// Bảng Cài đặt hệ thống dạng Modal Frosted Glass hiện đại
        /// </summary>
        private void DrawSettingsModal(float sw, float sh)
        {
            float modalWidth = 580f;
            float modalHeight = 520f;
            float modalX = (sw - modalWidth) * 0.5f;
            float modalY = (sh - modalHeight) * 0.5f;

            DrawModalBackground(new Rect(modalX, modalY, modalWidth, modalHeight));

            // Header
            GUI.Label(new Rect(modalX, modalY + 22f, modalWidth, 32f), "CÀI ĐẶT HỆ THỐNG", modalHeaderStyle);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(modalX + 60f, modalY + 58f, modalWidth - 120f, 2f), lineDividerTex);

            float curY = modalY + 80f;
            float itemX = modalX + 50f;
            float itemW = modalWidth - 100f;

            // 1. Âm Lượng Tổng (Master Volume)
            GUI.Label(new Rect(itemX, curY, itemW * 0.6f, 25f), "ÂM LƯỢNG TỔNG (VOLUME)", labelStyle);
            GUI.Label(new Rect(itemX + itemW * 0.6f, curY, itemW * 0.4f, 25f), Mathf.RoundToInt(masterVolume * 100f) + "%", valueLabelStyle);
            curY += 28f;

            float newVol = GUI.HorizontalSlider(new Rect(itemX, curY + 6f, itemW, 20f), masterVolume, 0f, 1f);
            if (Mathf.Abs(newVol - masterVolume) > 0.01f)
            {
                masterVolume = newVol;
                AudioListener.volume = masterVolume;
                if (audioSource != null) audioSource.volume = masterVolume;
                if (ambientWindSource != null) ambientWindSource.volume = masterVolume * 0.55f;
                PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            }
            curY += 38f;

            // 2. Độ Nhạy Chuột (Mouse Sensitivity)
            GUI.Label(new Rect(itemX, curY, itemW * 0.6f, 25f), "ĐỘ NHẠY CHUỘT (SENSITIVITY)", labelStyle);
            GUI.Label(new Rect(itemX + itemW * 0.6f, curY, itemW * 0.4f, 25f), mouseSensitivity.ToString("F1") + "x", valueLabelStyle);
            curY += 28f;

            float newSens = GUI.HorizontalSlider(new Rect(itemX, curY + 6f, itemW, 20f), mouseSensitivity, 0.5f, 5.0f);
            if (Mathf.Abs(newSens - mouseSensitivity) > 0.05f)
            {
                mouseSensitivity = newSens;
                PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivity);
            }
            curY += 42f;

            // 3. Chất Lượng Đồ Họa (Graphics Quality)
            GUI.Label(new Rect(itemX, curY, itemW, 25f), "CHẤT LƯỢNG ĐỒ HỌA", labelStyle);
            curY += 30f;

            string[] qualityNames = new string[] { "THẤP", "TRUNG BÌNH", "CAO", "ULTRA" };
            float qBtnW = (itemW - 15f) / 4f;
            for (int i = 0; i < 4; i++)
            {
                Rect qRect = new Rect(itemX + i * (qBtnW + 5f), curY, qBtnW, 36f);
                bool isSelected = (graphicsQualityIndex == i);

                GUI.color = isSelected ? new Color(0.85f, 0.15f, 0.15f, 0.95f) : new Color(0.12f, 0.12f, 0.15f, 0.85f);
                GUI.DrawTexture(qRect, accentRedTex);

                GUI.color = isSelected ? Color.white : new Color(0.75f, 0.75f, 0.78f, 0.9f);
                GUI.Label(qRect, qualityNames[i], buttonTextStyle);

                if (GUI.Button(qRect, GUIContent.none, GUIStyle.none))
                {
                    graphicsQualityIndex = i;
                    QualitySettings.SetQualityLevel(graphicsQualityIndex);
                    PlayerPrefs.SetInt("GraphicsQuality", graphicsQualityIndex);
                    PlayClickSound();
                }
            }
            curY += 52f;

            // 4. Chế Độ Toàn Màn Hình (Fullscreen)
            GUI.Label(new Rect(itemX, curY, itemW * 0.6f, 25f), "CHẾ ĐỘ HIỂN THỊ", labelStyle);
            Rect fsBtnRect = new Rect(itemX + itemW * 0.6f, curY - 2f, itemW * 0.4f, 32f);
            string fsText = isFullscreen ? "[ ✓ ] TOÀN MÀN HÌNH" : "[   ] CỬA SỔ";

            GUI.color = isFullscreen ? new Color(0.25f, 0.08f, 0.08f, 0.9f) : new Color(0.12f, 0.12f, 0.15f, 0.85f);
            GUI.DrawTexture(fsBtnRect, accentRedTex);
            GUI.color = isFullscreen ? new Color(1f, 0.4f, 0.4f, 1f) : Color.gray;
            GUI.Label(fsBtnRect, fsText, buttonTextStyle);

            if (GUI.Button(fsBtnRect, GUIContent.none, GUIStyle.none))
            {
                isFullscreen = !isFullscreen;
                if (isFullscreen)
                {
                    Resolution nativeRes = Screen.currentResolution;
                    Screen.SetResolution(nativeRes.width, nativeRes.height, true);
                }
                else
                {
                    Screen.fullScreen = false;
                }
                PlayerPrefs.SetInt("FullscreenV2", isFullscreen ? 1 : 0);
                PlayerPrefs.Save();
                PlayClickSound();
            }
            curY += 55f;

            // Nút Lưu và Quay Lại
            Rect backBtnRect = new Rect(itemX, modalY + modalHeight - 65f, itemW, 44f);
            if (DrawModalButton(backBtnRect, "◀   LƯU VÀ QUAY LẠI MENU"))
            {
                PlayerPrefs.Save();
                PlayClickSound();
                currentState = MenuState.Main;
            }
        }

        /// <summary>
        /// Bảng Hướng dẫn phím điều khiển hiện đại
        /// </summary>
        private void DrawControlsModal(float sw, float sh)
        {
            float modalWidth = 660f;
            float modalHeight = 540f;
            float modalX = (sw - modalWidth) * 0.5f;
            float modalY = (sh - modalHeight) * 0.5f;

            DrawModalBackground(new Rect(modalX, modalY, modalWidth, modalHeight));

            // Header
            GUI.Label(new Rect(modalX, modalY + 22f, modalWidth, 32f), "HƯỚNG DẪN ĐIỀU KHIỂN", modalHeaderStyle);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(modalX + 60f, modalY + 58f, modalWidth - 120f, 2f), lineDividerTex);

            float colW = (modalWidth - 110f) * 0.5f;
            float col1X = modalX + 45f;
            float col2X = col1X + colW + 20f;
            float rowH = 34f;

            // Cột 1: Di chuyển & Hành động
            float y1 = modalY + 80f;
            GUI.Label(new Rect(col1X, y1, colW, 25f), "DI CHUYỂN & HÀNH ĐỘNG", modalTagStyle);
            y1 += 30f;

            DrawKeyRow(col1X, y1, "W, A, S, D", "Di chuyển nhân vật"); y1 += rowH;
            DrawKeyRow(col1X, y1, "Left Shift", "Chạy nhanh (Sprint)"); y1 += rowH;
            DrawKeyRow(col1X, y1, "Space", "Nhảy vượt chướng ngại"); y1 += rowH;
            DrawKeyRow(col1X, y1, "Left Ctrl", "Ngồi / Khom người"); y1 += rowH;
            DrawKeyRow(col1X, y1, "Chuột Trái", "Bắn súng / Chặt cây"); y1 += rowH;
            DrawKeyRow(col1X, y1, "Chuột Phải", "Xoay hướng đồ khi xây"); y1 += rowH;

            // Cột 2: Trang bị & Sinh tồn
            float y2 = modalY + 80f;
            GUI.Label(new Rect(col2X, y2, colW, 25f), "TRANG BỊ & SINH TỒN", modalTagStyle);
            y2 += 30f;

            DrawKeyRow(col2X, y2, "Phím 1", "Rút / Cất Súng"); y2 += rowH;
            DrawKeyRow(col2X, y2, "Phím 2", "Rút / Cất Rìu"); y2 += rowH;
            DrawKeyRow(col2X, y2, "Phím R", "Nạp đạn cho súng"); y2 += rowH;
            DrawKeyRow(col2X, y2, "TAB / I", "Mở Túi Đồ (Inventory)"); y2 += rowH;
            DrawKeyRow(col2X, y2, "Phím C", "Bàn Chế Tạo (Crafting)"); y2 += rowH;
            DrawKeyRow(col2X, y2, "Phím B", "Menu Xây Dựng Căn Cứ"); y2 += rowH;
            DrawKeyRow(col2X, y2, "Phím ESC", "Tạm Dừng Game (Pause)"); y2 += rowH;

            // Nút Quay Lại
            Rect backBtnRect = new Rect(col1X, modalY + modalHeight - 65f, modalWidth - 90f, 44f);
            if (DrawModalButton(backBtnRect, "◀   QUAY LẠI MENU"))
            {
                PlayClickSound();
                currentState = MenuState.Main;
            }
        }

        private void DrawKeyRow(float x, float y, string key, string desc)
        {
            // Vẽ hộp phím mạ kim loại
            Rect badgeRect = new Rect(x, y, 98f, 26f);
            GUI.color = new Color(0.18f, 0.18f, 0.22f, 0.95f);
            GUI.DrawTexture(badgeRect, badgeBgTex);

            GUI.color = new Color(0.85f, 0.25f, 0.25f, 0.8f);
            GUI.DrawTexture(new Rect(badgeRect.x, badgeRect.y, 2f, badgeRect.height), accentRedTex);

            GUI.color = new Color(1f, 0.88f, 0.45f, 1f); // Màu vàng đồng nổi bật
            GUI.Label(badgeRect, key, keyBadgeStyle);

            // Mô tả phím
            GUI.color = new Color(0.88f, 0.88f, 0.92f, 0.95f);
            GUI.Label(new Rect(x + 108f, y, 185f, 26f), desc, keyDescStyle);
        }

        private void DrawModalBackground(Rect rect)
        {
            // Lớp nền Frosted Glass mờ 95%
            GUI.color = new Color(0.04f, 0.04f, 0.06f, 0.96f);
            GUI.DrawTexture(rect, modalBgTex);

            // Viền đỏ máu tinh xảo
            GUI.color = new Color(0.85f, 0.18f, 0.18f, 0.8f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), accentRedTex); // Top
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), accentRedTex); // Bottom
            GUI.DrawTexture(new Rect(rect.x, rect.y, 2f, rect.height), accentRedTex); // Left
            GUI.DrawTexture(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), accentRedTex); // Right

            GUI.color = Color.white;
        }

        private bool DrawModalButton(Rect rect, string text)
        {
            bool isHovered = rect.Contains(Event.current.mousePosition);

            GUI.color = isHovered ? new Color(0.40f, 0.08f, 0.08f, 0.95f) : new Color(0.14f, 0.14f, 0.18f, 0.9f);
            GUI.DrawTexture(rect, accentRedTex);

            GUI.color = isHovered ? new Color(1f, 0.3f, 0.3f, 1f) : new Color(0.6f, 0.15f, 0.15f, 0.8f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1.5f), accentRedTex);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1.5f, rect.width, 1.5f), accentRedTex);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 1.5f, rect.height), accentRedTex);
            GUI.DrawTexture(new Rect(rect.xMax - 1.5f, rect.y, 1.5f, rect.height), accentRedTex);

            GUI.color = isHovered ? Color.white : new Color(0.85f, 0.85f, 0.88f, 0.95f);
            GUI.Label(rect, text, buttonTextStyle);

            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        // ========================== KHỞI TẠO STYLES & TEXTURES ==========================

        private void InitStyles()
        {
            if (stylesInitialized && titleStyle != null) return;

            // 1. Tạo các Texture Gradient chất lượng cao
            leftGradientTex = MakeHorizontalGradient(512, new Color(0.02f, 0.02f, 0.04f, 0.95f), new Color(0.02f, 0.02f, 0.04f, 0f));
            topVignetteTex = MakeVerticalGradient(128, new Color(0.02f, 0.02f, 0.03f, 0.65f), new Color(0.02f, 0.02f, 0.03f, 0f), true);
            bottomVignetteTex = MakeVerticalGradient(128, new Color(0.02f, 0.02f, 0.03f, 0.75f), new Color(0.02f, 0.02f, 0.03f, 0f), false);

            btnNormalGradientTex = MakeHorizontalGradient(256, new Color(0.06f, 0.06f, 0.08f, 0.75f), new Color(0.03f, 0.03f, 0.04f, 0.25f));
            btnHoverGradientTex = MakeHorizontalGradient(256, new Color(0.45f, 0.08f, 0.08f, 0.90f), new Color(0.18f, 0.03f, 0.03f, 0.35f));

            accentRedTex = MakeSolidTex(2, 2, Color.white);
            modalBgTex = MakeSolidTex(2, 2, Color.white);
            fullDarkVeilTex = MakeSolidTex(2, 2, Color.white);
            badgeBgTex = MakeSolidTex(2, 2, Color.white);

            lineDividerTex = MakeHorizontalGradient(256, new Color(1f, 0.22f, 0.22f, 0.95f), new Color(1f, 0.22f, 0.22f, 0f));

            // 2. Khởi tạo GUIStyles sắc nét
            brandTagStyle = new GUIStyle();
            brandTagStyle.fontSize = 11;
            brandTagStyle.fontStyle = FontStyle.Bold;
            brandTagStyle.alignment = TextAnchor.MiddleLeft;
            brandTagStyle.normal.textColor = new Color(0.95f, 0.25f, 0.25f, 0.95f);

            titleStyle = new GUIStyle();
            titleStyle.fontSize = 38;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleLeft;
            titleStyle.normal.textColor = new Color(0.98f, 0.20f, 0.20f, 1f);

            titleShadowStyle = new GUIStyle(titleStyle);
            titleShadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.85f);

            subtitleStyle = new GUIStyle();
            subtitleStyle.fontSize = 12;
            subtitleStyle.fontStyle = FontStyle.Normal;
            subtitleStyle.alignment = TextAnchor.MiddleLeft;
            subtitleStyle.normal.textColor = new Color(0.70f, 0.70f, 0.75f, 0.85f);

            buttonTextStyle = new GUIStyle();
            buttonTextStyle.fontSize = 14;
            buttonTextStyle.fontStyle = FontStyle.Bold;
            buttonTextStyle.alignment = TextAnchor.MiddleLeft;
            buttonTextStyle.normal.textColor = new Color(0.88f, 0.88f, 0.90f, 0.95f);

            buttonHoverTextStyle = new GUIStyle(buttonTextStyle);
            buttonHoverTextStyle.fontSize = 15;
            buttonHoverTextStyle.normal.textColor = Color.white;

            modalHeaderStyle = new GUIStyle();
            modalHeaderStyle.fontSize = 22;
            modalHeaderStyle.fontStyle = FontStyle.Bold;
            modalHeaderStyle.alignment = TextAnchor.MiddleCenter;
            modalHeaderStyle.normal.textColor = new Color(0.98f, 0.22f, 0.22f, 1f);

            modalTagStyle = new GUIStyle();
            modalTagStyle.fontSize = 13;
            modalTagStyle.fontStyle = FontStyle.Bold;
            modalTagStyle.alignment = TextAnchor.MiddleLeft;
            modalTagStyle.normal.textColor = new Color(0.95f, 0.35f, 0.35f, 1f);

            labelStyle = new GUIStyle();
            labelStyle.fontSize = 13;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.alignment = TextAnchor.MiddleLeft;
            labelStyle.normal.textColor = new Color(0.85f, 0.85f, 0.88f, 0.95f);

            valueLabelStyle = new GUIStyle();
            valueLabelStyle.fontSize = 13;
            valueLabelStyle.fontStyle = FontStyle.Bold;
            valueLabelStyle.alignment = TextAnchor.MiddleRight;
            valueLabelStyle.normal.textColor = new Color(1f, 0.85f, 0.35f, 1f);

            keyBadgeStyle = new GUIStyle();
            keyBadgeStyle.fontSize = 12;
            keyBadgeStyle.fontStyle = FontStyle.Bold;
            keyBadgeStyle.alignment = TextAnchor.MiddleCenter;
            keyBadgeStyle.normal.textColor = new Color(1f, 0.9f, 0.45f, 1f);

            keyDescStyle = new GUIStyle();
            keyDescStyle.fontSize = 13;
            keyDescStyle.fontStyle = FontStyle.Normal;
            keyDescStyle.alignment = TextAnchor.MiddleLeft;
            keyDescStyle.normal.textColor = new Color(0.85f, 0.85f, 0.88f, 0.95f);

            stylesInitialized = true;
        }

        private Texture2D MakeSolidTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private Texture2D MakeHorizontalGradient(int width, Color leftColor, Color rightColor)
        {
            Texture2D tex = new Texture2D(width, 1, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width];
            for (int x = 0; x < width; x++)
            {
                float t = (float)x / (width - 1);
                // Đường cong mượt Hermite smoothstep
                float smoothT = t * t * (3f - 2f * t);
                pixels[x] = Color.Lerp(leftColor, rightColor, smoothT);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private Texture2D MakeVerticalGradient(int height, Color edgeColor, Color fadeColor, bool fromTop)
        {
            Texture2D tex = new Texture2D(1, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[height];
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);
                float smoothT = t * t * (3f - 2f * t);
                if (fromTop)
                {
                    pixels[y] = Color.Lerp(edgeColor, fadeColor, smoothT);
                }
                else
                {
                    pixels[y] = Color.Lerp(fadeColor, edgeColor, smoothT);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // =========================================================================
        // HỆ THỐNG NỀN VIDEO ĐỘNG & CHUYỂN CẢNH KINH DỊ PHONG CÁCH OUTLAST 1
        // =========================================================================

        private void SetupMenuVideoPlayer()
        {
            if (menuBackgroundVideo == null) return;

            if (bgVideoRT == null)
            {
                bgVideoRT = new RenderTexture(1280, 720, 0);
                bgVideoRT.Create();
            }

            bgVideoPlayer = GetComponent<VideoPlayer>();
            if (bgVideoPlayer == null) bgVideoPlayer = gameObject.AddComponent<VideoPlayer>();

            bgVideoPlayer.clip = menuBackgroundVideo;
            bgVideoPlayer.isLooping = true;
            bgVideoPlayer.playOnAwake = true;
            bgVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            bgVideoPlayer.targetTexture = bgVideoRT;
            bgVideoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            bgVideoPlayer.Play();
        }

        private void InitOutlastDustMotes()
        {
            int count = 36;
            dustMotes = new Vector2[count];
            dustSpeeds = new float[count];
            for (int i = 0; i < count; i++)
            {
                dustMotes[i] = new Vector2(Random.value, Random.value);
                dustSpeeds[i] = Random.Range(0.015f, 0.055f);
            }
        }

        private void EnsureOutlastTextures()
        {
            if (camcorderWhiteTex == null)
            {
                camcorderWhiteTex = new Texture2D(1, 1);
                camcorderWhiteTex.SetPixel(0, 0, Color.white);
                camcorderWhiteTex.Apply();
            }

            // Sọc quét màn hình CRT Scanlines
            if (scanlineTex == null)
            {
                scanlineTex = new Texture2D(2, 4, TextureFormat.RGBA32, false);
                scanlineTex.wrapMode = TextureWrapMode.Repeat;
                for (int y = 0; y < 4; y++)
                {
                    Color c = (y == 0) ? new Color(0f, 0f, 0f, 0.24f) : new Color(0f, 0f, 0f, 0f);
                    scanlineTex.SetPixel(0, y, c);
                    scanlineTex.SetPixel(1, y, c);
                }
                scanlineTex.Apply();
            }

            // Texture nhiễu hạt trắng đen VHS Static Noise
            if (staticNoiseTex == null)
            {
                int sz = 96;
                staticNoiseTex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
                staticNoiseTex.wrapMode = TextureWrapMode.Repeat;
                Color[] pix = new Color[sz * sz];
                for (int i = 0; i < pix.Length; i++)
                {
                    float v = Random.value;
                    pix[i] = new Color(v, v, v, v > 0.55f ? 0.55f : 0.15f);
                }
                staticNoiseTex.SetPixels(pix);
                staticNoiseTex.Apply();
            }

            if (camcorderStyle == null)
            {
                camcorderStyle = new GUIStyle(GUI.skin.label);
                camcorderStyle.fontSize = 14;
                camcorderStyle.fontStyle = FontStyle.Bold;
                camcorderStyle.normal.textColor = new Color(0.86f, 0.94f, 0.88f, 0.85f);
            }

            if (camcorderRedStyle == null)
            {
                camcorderRedStyle = new GUIStyle(camcorderStyle);
                camcorderRedStyle.normal.textColor = new Color(1.0f, 0.22f, 0.18f, 0.95f);
            }
        }

        private void UpdateOutlastAnimations()
        {
            if (Event.current.type != EventType.Repaint) return;

            float dt = Time.unscaledDeltaTime;
            outlastTime += dt;

            // 1. Tự động cắt chuyển cảnh mỗi 5.5 giây kèm nhiễu VHS đột ngột như Outlast 1
            shotTimer += dt;
            if (shotTimer >= SHOT_DURATION)
            {
                shotTimer = 0f;
                currentShotIndex = (currentShotIndex + 1) % 4;
                glitchBurstTimer = 0.26f; // Cắt cảnh bằng nhiễu tĩnh mạnh!
            }

            // 2. Nhiễu VHS ngẫu nhiên bất chợt
            if (glitchBurstTimer > 0f)
            {
                glitchBurstTimer -= dt;
            }
            else if (outlastTime >= nextRandomGlitchTime)
            {
                glitchBurstTimer = Random.Range(0.08f, 0.18f);
                nextRandomGlitchTime = outlastTime + Random.Range(2.8f, 6.2f);
            }

            // 3. Chớp sấm sét kinh dị bất chợt
            if (lightningFlashAlpha > 0f)
            {
                lightningFlashAlpha = Mathf.MoveTowards(lightningFlashAlpha, 0f, dt * 2.8f);
            }
            else if (outlastTime >= nextLightningTime)
            {
                lightningFlashAlpha = Random.Range(0.32f, 0.58f);
                nextLightningTime = outlastTime + Random.Range(5.5f, 11.5f);
            }

            // 4. Hạt tro/mưa lơ lửng trôi ngang
            if (dustMotes != null)
            {
                for (int i = 0; i < dustMotes.Length; i++)
                {
                    dustMotes[i].x -= dustSpeeds[i] * dt * 0.7f;
                    dustMotes[i].y += dustSpeeds[i] * dt * 1.4f;
                    if (dustMotes[i].x < 0f) dustMotes[i].x += 1f;
                    if (dustMotes[i].y > 1f) dustMotes[i].y -= 1f;
                }
            }
        }

        private Vector3 liveStageOrigin = new Vector3(0f, 820f, 0f);
        private bool ownsLiveMenuCam = false;

        private void CleanupLiveOutlast3DStage()
        {
            if (liveMenuStageRoot != null)
            {
                Destroy(liveMenuStageRoot);
                liveMenuStageRoot = null;
            }
            if (ownsLiveMenuCam && liveMenuCam != null)
            {
                Destroy(liveMenuCam.gameObject);
                liveMenuCam = null;
                ownsLiveMenuCam = false;
            }
            liveStageBuilt = false;
        }

        private void EnsureLiveOutlast3DStage()
        {
            if (liveStageBuilt) return;
            // Nếu có VideoClip .mp4 đang phát thì không cần dựng sân khấu 3D
            if (bgVideoPlayer != null && bgVideoRT != null && bgVideoPlayer.isPlaying) return;

            liveStageBuilt = true;

            bool isDedicatedMainMenu = SceneManager.GetActiveScene().name.ToLower().Contains("mainmenu");
            liveStageOrigin = isDedicatedMainMenu ? Vector3.zero : new Vector3(0f, 820f, 0f);

            // Tạo Camera 3D riêng (depth = 99) để hoạt động mượt ở CẢ MainMenu.unity LẪN Scene_A.unity!
            if (isDedicatedMainMenu && Camera.main != null)
            {
                liveMenuCam = Camera.main;
                ownsLiveMenuCam = false;
            }
            else
            {
                GameObject camObj = new GameObject("[Outlast_Live3D_MenuCamera]");
                liveMenuCam = camObj.AddComponent<Camera>();
                liveMenuCam.depth = 99f;
                ownsLiveMenuCam = true;
            }

            liveMenuCam.clearFlags = CameraClearFlags.SolidColor;
            liveMenuCam.backgroundColor = new Color(0.015f, 0.025f, 0.028f);
            liveMenuCam.fieldOfView = 55f;

            if (isDedicatedMainMenu)
            {
                Light[] existingLights = FindObjectsOfType<Light>();
                for (int i = 0; i < existingLights.Length; i++)
                {
                    if (existingLights[i] != null && existingLights[i].type == LightType.Directional)
                    {
                        existingLights[i].intensity = 0.08f;
                        existingLights[i].color = new Color(0.28f, 0.38f, 0.45f);
                        liveLightningLight = existingLights[i];
                    }
                }
                RenderSettings.ambientLight = new Color(0.04f, 0.06f, 0.055f);
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = new Color(0.018f, 0.030f, 0.026f);
                RenderSettings.fogDensity = 0.065f;
            }

            liveMenuStageRoot = new GameObject("[Outlast_Live3D_MenuStage]");
            liveMenuStageRoot.transform.position = liveStageOrigin;

            // 1. Sàn nền dự phòng đặt thấp bên dưới sàn đá của hầm (tránh cắt xuyên sàn hầm gây đốm đen/lún chân)
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.SetParent(liveMenuStageRoot.transform, false);
            ground.transform.localPosition = new Vector3(0f, -0.6f, 0f);
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            Renderer gr = ground.GetComponent<Renderer>();
            if (gr != null)
            {
                Material gm = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
                gm.color = new Color(0.05f, 0.06f, 0.05f);
                gm.SetFloat("_Glossiness", 0.45f);
                gr.sharedMaterial = gm;
            }

            GameObject pTunnel1 = Resources.Load<GameObject>("BuildAssets/Tunnel1");
            GameObject pTunnel2 = Resources.Load<GameObject>("BuildAssets/Tunnel2");
            GameObject pSupport1 = Resources.Load<GameObject>("BuildAssets/Support1");
            GameObject pLantern = Resources.Load<GameObject>("BuildAssets/Lantern");
            GameObject pCrate = Resources.Load<GameObject>("BuildAssets/Crate");
            GameObject pRock1 = Resources.Load<GameObject>("BuildAssets/Rock1");
            GameObject pZombie = Resources.Load<GameObject>("BuildAssets/Zombiegirl");
            RuntimeAnimatorController zCtrl = Resources.Load<RuntimeAnimatorController>("BuildAssets/ZombieAnim");
#if UNITY_EDITOR
            if (pTunnel1 == null) pTunnel1 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mine/Prefabs/Base/Tunnel1.prefab");
            if (pTunnel2 == null) pTunnel2 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mine/Prefabs/Base/Tunnel2.prefab");
            if (pSupport1 == null) pSupport1 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mine/Prefabs/Props/Support1.prefab");
            if (pLantern == null) pLantern = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mine/Prefabs/Props/Lantern.prefab");
            if (pCrate == null) pCrate = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mine/Prefabs/Props/Crate.prefab");
            if (pRock1 == null) pRock1 = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mine/Prefabs/Rocks/Rock1.prefab");
            if (pZombie == null) pZombie = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/Character No Animation/Enemy No Animation/Zombiegirl W Kurniawan.fbx");
            if (zCtrl == null) zCtrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Flooded_Grounds/Character No Animation/Enemy No Animation/Animation zoombie/ZombieAnim.controller");
#endif

            MeshCollider tunnelCol1 = null;
            MeshCollider tunnelCol2 = null;
            if (pTunnel1 != null)
            {
                GameObject t1 = Instantiate(pTunnel1);
                t1.transform.SetParent(liveMenuStageRoot.transform, false);
                t1.transform.localPosition = new Vector3(0f, 0f, 6f);
                tunnelCol1 = t1.GetComponentInChildren<MeshCollider>();
            }
            if (pTunnel2 != null)
            {
                GameObject t2 = Instantiate(pTunnel2);
                t2.transform.SetParent(liveMenuStageRoot.transform, false);
                t2.transform.localPosition = new Vector3(0f, 0f, 18f);
                tunnelCol2 = t2.GetComponentInChildren<MeshCollider>();
            }
            if (pSupport1 != null)
            {
                GameObject s1 = Instantiate(pSupport1); s1.transform.SetParent(liveMenuStageRoot.transform, false); s1.transform.localPosition = new Vector3(-1.45f, 0f, 4f);
                GameObject s2 = Instantiate(pSupport1); s2.transform.SetParent(liveMenuStageRoot.transform, false); s2.transform.localPosition = new Vector3(1.45f, 0f, 4f); s2.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                GameObject s3 = Instantiate(pSupport1); s3.transform.SetParent(liveMenuStageRoot.transform, false); s3.transform.localPosition = new Vector3(-1.45f, 0f, 11f);
                GameObject s4 = Instantiate(pSupport1); s4.transform.SetParent(liveMenuStageRoot.transform, false); s4.transform.localPosition = new Vector3(1.45f, 0f, 11f); s4.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
            if (pCrate != null)
            {
                GameObject c1 = Instantiate(pCrate); c1.transform.SetParent(liveMenuStageRoot.transform, false); c1.transform.localPosition = new Vector3(-1.2f, 0f, 5.2f); c1.transform.localRotation = Quaternion.Euler(0f, 24f, 0f);
                GameObject c2 = Instantiate(pCrate); c2.transform.SetParent(liveMenuStageRoot.transform, false); c2.transform.localPosition = new Vector3(1.3f, 0f, 9.8f); c2.transform.localRotation = Quaternion.Euler(0f, -18f, 0f);
            }
            if (pRock1 != null)
            {
                GameObject r1 = Instantiate(pRock1);
                r1.transform.SetParent(liveMenuStageRoot.transform, false);
                r1.transform.localPosition = new Vector3(0f, 0.2f, 23.5f);
                r1.transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
                r1.transform.localScale = Vector3.one * 1.8f;
            }
            if (pLantern != null)
            {
                GameObject lan = Instantiate(pLantern);
                lan.transform.SetParent(liveMenuStageRoot.transform, false);
                lan.transform.localPosition = new Vector3(1.15f, 2.2f, 7.5f);
                Light ll = lan.GetComponentInChildren<Light>();
                if (ll != null)
                {
                    ll.enabled = true;
                    ll.range = 14f;
                    ll.intensity = 2.4f;
#if UNITY_EDITOR
                    ll.lightmapBakeType = LightmapBakeType.Realtime;
#endif
                }
            }

            // 2. Bóng dáng Zombie 3D thật đứng chuẩn giữa lòng sàn đá hành lang (x = 0.1f để không trúng vách đá cong hai bên làm lơ lửng!)
            if (pZombie != null)
            {
                GameObject zObj = Instantiate(pZombie);
                zObj.name = "OutlastMenu_StalkingZombie";
                zObj.transform.SetParent(liveMenuStageRoot.transform, false);
                float floorLocalY = -0.04f;
                Ray downRay = new Ray(liveStageOrigin + new Vector3(0.05f, 1.2f, 10.5f), Vector3.down);
                RaycastHit floorHit;
                if (tunnelCol1 != null && tunnelCol1.Raycast(downRay, out floorHit, 3f))
                {
                    floorLocalY = Mathf.Clamp(floorHit.point.y - liveStageOrigin.y - 0.04f, -0.25f, 0.05f);
                }
                liveZombieFloorLocalY = floorLocalY;
                zObj.transform.localPosition = new Vector3(0.15f, floorLocalY, 10.5f);
                zObj.transform.localRotation = Quaternion.Euler(0f, 165f, 0f);
                Animator za = zObj.GetComponent<Animator>();
                if (za == null) za = zObj.AddComponent<Animator>();
                if (zCtrl != null) za.runtimeAnimatorController = zCtrl;
                za.updateMode = AnimatorUpdateMode.UnscaledTime;
                za.enabled = true;
                za.applyRootMotion = false;
                za.SetFloat("Speed", 0f);
                liveZombieTransform = zObj.transform;
                liveZombieRenderers = zObj.GetComponentsInChildren<Renderer>();
            }

            // 3. Đèn huỳnh quang xanh lục nhấp nháy trên trần (đặc trưng Outlast 1!)
            GameObject flObj = new GameObject("Outlast_FlickerFluorescent");
            flObj.transform.SetParent(liveMenuStageRoot.transform, false);
            flObj.transform.localPosition = new Vector3(0f, 3.5f, 9.5f);
            liveFlickerLight = flObj.AddComponent<Light>();
            liveFlickerLight.type = LightType.Point;
            liveFlickerLight.color = new Color(0.45f, 1.0f, 0.65f);
            liveFlickerLight.range = 18f;
            liveFlickerLight.intensity = 2.6f;

            // 4. Đèn pháo sáng đỏ rực phía cuối hành lang tạo tương phản kinh dị
            GameObject redObj = new GameObject("Outlast_RedEmergencyFlare");
            redObj.transform.SetParent(liveMenuStageRoot.transform, false);
            redObj.transform.localPosition = new Vector3(-0.8f, 0.8f, 15.5f);
            liveRedFlareLight = redObj.AddComponent<Light>();
            liveRedFlareLight.type = LightType.Point;
            liveRedFlareLight.color = new Color(1.0f, 0.16f, 0.12f);
            liveRedFlareLight.range = 16f;
            liveRedFlareLight.intensity = 3.0f;

            // 5. Hạt sương mù 3D bay cuộn (UnscaledTime để vẫn bay mượt khi Scene_A Pause!)
            GameObject mistObj = new GameObject("Outlast_3DMistParticles");
            mistObj.transform.SetParent(liveMenuStageRoot.transform, false);
            mistObj.transform.localPosition = new Vector3(0f, 1.8f, 9f);
            ParticleSystem ps = mistObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.useUnscaledTime = true;
            main.loop = true;
            main.startLifetime = 4.5f;
            main.startSpeed = 0.6f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startColor = new Color(0.65f, 0.90f, 0.72f, 0.35f);
            main.maxParticles = 140;
            var em = ps.emission;
            em.rateOverTime = 28f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(6f, 4f, 18f);

            // Gán Material + Texture tròn mềm bằng shader Sprites/Default (luôn có sẵn trong bản Build .exe, tránh lỗi hạt màu hồng!)
            ParticleSystemRenderer psr = mistObj.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                Texture2D softDot = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                for (int py = 0; py < 32; py++)
                {
                    for (int px = 0; px < 32; px++)
                    {
                        float dx = (px - 15.5f) / 15.5f;
                        float dy = (py - 15.5f) / 15.5f;
                        float d = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                        softDot.SetPixel(px, py, new Color(1f, 1f, 1f, d * d));
                    }
                }
                softDot.Apply();
                Shader pShader = Shader.Find("Sprites/Default") ?? Shader.Find("Particles/Alpha Blended") ?? Shader.Find("Standard");
                Material pMat = new Material(pShader);
                pMat.mainTexture = softDot;
                psr.sharedMaterial = pMat;
            }
        }

        private void UpdateLiveOutlast3DCamera()
        {
            if (liveMenuCam == null || liveMenuStageRoot == null) return;

            float p = Mathf.Clamp01(shotTimer / SHOT_DURATION);
            float t = outlastTime;

            if (liveFlickerLight != null)
            {
                float noise = Mathf.PerlinNoise(t * 14f, 0.5f);
                liveFlickerLight.intensity = (noise < 0.22f) ? 0.15f : (1.8f + noise * 1.4f);
            }
            if (liveRedFlareLight != null)
            {
                liveRedFlareLight.intensity = 2.4f + Mathf.Sin(t * 7f) * 0.6f;
            }
            if (liveLightningLight != null)
            {
                liveLightningLight.intensity = 0.08f + lightningFlashAlpha * 2.2f;
            }
            if (liveZombieTransform != null)
            {
                liveZombieTransform.localRotation = Quaternion.Euler(0f, 165f + Mathf.Sin(t * 0.6f) * 14f, 0f);

                // Tự động khóa bàn chân Zombie đứng đúng trên mặt sàn (chống animation Mixamo kéo tụt hông xuống dưới sàn!)
                if (liveZombieRenderers != null && liveZombieRenderers.Length > 0)
                {
                    float minY = float.MaxValue;
                    for (int i = 0; i < liveZombieRenderers.Length; i++)
                    {
                        if (liveZombieRenderers[i] != null)
                        {
                            float bMin = liveZombieRenderers[i].bounds.min.y;
                            if (bMin < minY) minY = bMin;
                        }
                    }
                    if (minY < float.MaxValue * 0.5f)
                    {
                        float targetFloorWorldY = liveStageOrigin.y + liveZombieFloorLocalY;
                        float deltaY = targetFloorWorldY - minY;
                        if (Mathf.Abs(deltaY) > 0.02f && Mathf.Abs(deltaY) < 3.0f)
                        {
                            Vector3 pZ = liveZombieTransform.position;
                            pZ.y += deltaY;
                            liveZombieTransform.position = pZ;
                        }
                    }
                }
            }

            Vector3 localCamPos;
            Vector3 localLookAt;
            switch (currentShotIndex)
            {
                case 0:
                    localCamPos = Vector3.Lerp(new Vector3(-0.3f, 1.55f, 2.2f), new Vector3(0.15f, 1.50f, 6.2f), p);
                    localLookAt = new Vector3(0.45f, 1.35f, 11.5f);
                    break;
                case 1:
                    localCamPos = Vector3.Lerp(new Vector3(-1.1f, 0.75f, 6.6f), new Vector3(-0.7f, 0.90f, 7.6f), p);
                    localLookAt = new Vector3(0.45f, 1.25f, 10.5f);
                    break;
                case 2:
                    localCamPos = Vector3.Lerp(new Vector3(0.95f, 1.55f, 7.4f), new Vector3(0.70f, 1.48f, 7.9f), p);
                    localLookAt = new Vector3(0.45f, 1.25f, 10.6f);
                    break;
                default:
                    localCamPos = Vector3.Lerp(new Vector3(0.4f, 1.85f, 13.8f), new Vector3(-0.3f, 1.65f, 15.8f), p);
                    localLookAt = new Vector3(0.3f, 1.35f, 9.0f);
                    break;
            }

            localCamPos.x += Mathf.Sin(t * 1.4f) * 0.06f;
            localCamPos.y += Mathf.Cos(t * 1.9f) * 0.04f;

            liveMenuCam.transform.position = liveStageOrigin + localCamPos;
            liveMenuCam.transform.LookAt(liveStageOrigin + localLookAt);
        }

        private void DrawOutlastAnimatedBackground(float sw, float sh)
        {
            EnsureOutlastTextures();

            // 1. Nếu người chơi có gán file Video (.mp4/.webm) -> phát trực tiếp Video toàn màn hình!
            if (bgVideoPlayer != null && bgVideoRT != null && bgVideoPlayer.isPlaying)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(0f, 0f, sw, sh), bgVideoRT, ScaleMode.ScaleAndCrop);
                return;
            }

            // 2. Mặc định: Chạy Camera 3D TRỰC TIẾP (KHÔNG VẼ ẢNH TĨNH ĐÈ LÊN!) -> nhìn xuyên thẳng ra khung cảnh 3D động phía sau!
            UpdateLiveOutlast3DCamera();

            // Phủ lớp tint xanh Night-Vision cực nhẹ đặc trưng Outlast 1
            GUI.color = new Color(0.05f, 0.14f, 0.08f, 0.16f);
            GUI.DrawTexture(new Rect(0f, 0f, sw, sh), camcorderWhiteTex);
            GUI.color = Color.white;
        }

        private void DrawOutlastAtmosphereAndCamcorderHUD(float sw, float sh)
        {
            Color prev = GUI.color;

            // 1. Hạt mưa / bụi tro bay lơ lửng
            if (dustMotes != null)
            {
                for (int i = 0; i < dustMotes.Length; i++)
                {
                    float mx = dustMotes[i].x * sw;
                    float my = dustMotes[i].y * sh;
                    float sz = (i % 3 == 0) ? 3.5f : 2.0f;
                    GUI.color = new Color(0.85f, 0.92f, 0.88f, 0.22f);
                    GUI.DrawTexture(new Rect(mx, my, sz, sz * 2.4f), camcorderWhiteTex);
                }
            }

            // 2. Sọc quét màn hình CRT Scanlines
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, sw, sh), scanlineTex, new Rect(0f, outlastTime * 0.4f, 1f, sh / 4f));

            // 3. Hiệu ứng Nhiễu Tĩnh VHS & Dải Glitch ngang khi chuyển cảnh
            if (glitchBurstTimer > 0f)
            {
                float gAlpha = Mathf.Clamp01(glitchBurstTimer * 3.2f) * 0.45f;
                GUI.color = new Color(0.85f, 1f, 0.9f, gAlpha);
                GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, sw, sh), staticNoiseTex,
                    new Rect(Random.value, Random.value, sw / 180f, sh / 180f));

                // 2 dải sọc ngang VHS tracking glitch
                for (int g = 0; g < 2; g++)
                {
                    float gy = Random.Range(0f, sh);
                    float gh = Random.Range(6f, 28f);
                    GUI.color = new Color(0.9f, 1f, 0.92f, gAlpha * 0.8f);
                    GUI.DrawTexture(new Rect(0f, gy, sw, gh), camcorderWhiteTex);
                }
            }

            // 4. Lóe sáng Sấm Chớp kinh dị
            if (lightningFlashAlpha > 0.01f)
            {
                GUI.color = new Color(0.85f, 0.92f, 1.0f, lightningFlashAlpha * 0.45f);
                GUI.DrawTexture(new Rect(0f, 0f, sw, sh), camcorderWhiteTex);
            }

            // 5. KHUNG NGẮM MÁY QUAY CẦM TAY OUTLAST 1 (Góc trên phải + 4 góc viền)
            GUI.color = new Color(0.82f, 0.92f, 0.85f, 0.45f);
            float pad = 22f;
            float arm = 26f;
            float thick = 2f;
            // Góc trên trái
            GUI.DrawTexture(new Rect(pad, pad, arm, thick), camcorderWhiteTex);
            GUI.DrawTexture(new Rect(pad, pad, thick, arm), camcorderWhiteTex);
            // Góc trên phải
            GUI.DrawTexture(new Rect(sw - pad - arm, pad, arm, thick), camcorderWhiteTex);
            GUI.DrawTexture(new Rect(sw - pad - thick, pad, thick, arm), camcorderWhiteTex);
            // Góc dưới trái
            GUI.DrawTexture(new Rect(pad, sh - pad - thick, arm, thick), camcorderWhiteTex);
            GUI.DrawTexture(new Rect(pad, sh - pad - arm, thick, arm), camcorderWhiteTex);
            // Góc dưới phải
            GUI.DrawTexture(new Rect(sw - pad - arm, sh - pad - thick, arm, thick), camcorderWhiteTex);
            GUI.DrawTexture(new Rect(sw - pad - thick, sh - pad - arm, thick, arm), camcorderWhiteTex);

            // Chữ ● REC nhấp nháy + Timecode chạy trực tiếp ở góc trên bên phải
            GUI.color = Color.white;
            bool blinkOn = (Mathf.FloorToInt(outlastTime * 2f) % 2 == 0);
            string recLabel = blinkOn ? "●  REC" : "    REC";
            GUI.Label(new Rect(sw - 185f, pad + 6f, 150f, 22f), recLabel, camcorderRedStyle);

            int totalSec = Mathf.FloorToInt(outlastTime);
            int mins = (totalSec / 60) % 60;
            int secs = totalSec % 60;
            int frames = Mathf.FloorToInt((outlastTime - totalSec) * 30f);
            string timecode = string.Format("00:{0:00}:{1:00}:{2:00}", mins, secs, frames);
            GUI.Label(new Rect(sw - 185f, pad + 26f, 150f, 22f), timecode, camcorderStyle);

            // Thanh Pin & Chế độ Night Vision góc dưới phải
            GUI.Label(new Rect(sw - 210f, sh - pad - 44f, 180f, 20f), "NIGHT VISION  •  SP", camcorderStyle);
            GUI.Label(new Rect(sw - 210f, sh - pad - 24f, 180f, 20f), "BAT [████░] 84%", camcorderStyle);

            GUI.color = prev;
        }
    }
}

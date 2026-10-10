using UnityEngine;
using UnityEngine.SceneManagement;

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

        [Header("Ảnh Nền Menu")]
        [Tooltip("Ảnh nền cho Menu Game (avata chinh thuc.png)")]
        public Texture2D menuBackgroundTexture;

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
            isFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;

            // Áp dụng cài đặt
            AudioListener.volume = masterVolume;
            QualitySettings.SetQualityLevel(graphicsQualityIndex);
            Screen.fullScreen = isFullscreen;

            // Thiết lập AudioSource cho Menu
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.ignoreListenerPause = true; // Cho phép phát tiếng click ngay cả khi đang Pause

            // Tìm nạp âm thanh nền menu (ưu tiên file nhạc kinh dị trong thư mục "sound main menu")
            if (menuMusic == null)
            {
                menuMusic = Resources.Load<AudioClip>("MenuMusic");
                if (menuMusic == null)
                    menuMusic = Resources.Load<AudioClip>("Background");
#if UNITY_EDITOR
                // Ưu tiên 1: File nhạc chính trong thư mục "sound main menu"
                if (menuMusic == null)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Flooded_Grounds/sound main menu" });
                    if (guids.Length > 0)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                        menuMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                        Debug.Log("🎵 [Menu] Đã nạp nhạc nền menu: " + path);
                    }
                }
                // Fallback: các file cũ
                if (menuMusic == null)
                    menuMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/Background.mp3");
                if (menuMusic == null)
                    menuMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/WindHowl.mp3");
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

            // Xử lý luồng cảnh
            if (isMainMenuScene)
            {
                // Màn hình Menu chính biệt lập
                IsPaused = false;
                Time.timeScale = 1f;
                AudioListener.pause = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Tắt / ngắt các âm thanh rác trong scene nếu có
                MuteOtherSceneAudios();

                if (menuMusic != null)
                {
                    audioSource.clip = menuMusic;
                    audioSource.loop = true;
                    audioSource.volume = masterVolume * 0.75f;
                    audioSource.Play();
                }
            }
            else
            {
                // Khi đang ở trong màn chơi Scene_A:
                if (!hasStartedFromMainMenu && showMenuOnPlayInGameScene)
                {
                    // Chuyển sang chế độ Main Menu hiển thị trên nền Avatar
                    isMainMenuScene = true;
                    IsPaused = true;
                    Time.timeScale = 0f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;

                    // Cách ly tuyệt đối: Tắt ngay âm thanh trailer máy bay trong Scene_A
                    MuteOtherSceneAudios();

                    if (menuMusic != null)
                    {
                        audioSource.clip = menuMusic;
                        audioSource.loop = true;
                        audioSource.volume = masterVolume * 0.75f;
                        audioSource.Play();
                    }
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

        private void MuteOtherSceneAudios()
        {
            AudioSource[] all = FindObjectsOfType<AudioSource>();
            foreach (var a in all)
            {
                if (a != audioSource && a.isPlaying)
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
                if (a == null || a == audioSource) continue;

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

                a.UnPause();
            }
        }

        void Update()
        {
            // Nếu ở Main Menu Scene -> Luôn hiện chuột
            if (isMainMenuScene)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            // Trong màn chơi (In-Game): Nhấn ESC để Tạm Dừng / Tiếp Tục
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Không bật Pause nếu đang trong Cutscene máy bay rơi
                if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;

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
                // Người chơi đang bấm Play trực tiếp trong Scene_A -> Bắt đầu chơi ngay lập tức!
                isMainMenuScene = false;
                IsPaused = false;
                AudioListener.pause = false;
                Time.timeScale = 1f;

                if (audioSource != null && audioSource.isPlaying)
                {
                    audioSource.Stop();
                }

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
            // Chỉ vẽ khi: ở Main Menu Scene HOẶC đang trong trạng thái Pause In-Game
            if (!isMainMenuScene && !IsPaused) return;

            InitStyles();

            float sw = Screen.width;
            float sh = Screen.height;

            // 1. VẼ NỀN VÀ HIỆU ỨNG ÁNH SÁNG ĐIỆN ẢNH
            if (isMainMenuScene)
            {
                // Vẽ tác phẩm Avatar game toàn màn hình (Không bị zoom hay cắt xén)
                if (menuBackgroundTexture != null)
                {
                    GUI.color = Color.white;
                    if (backgroundScaleMode == MenuBackgroundScaleMode.StretchToFill)
                    {
                        // Hiển thị trọn vẹn 100% hình ảnh (chữ SURVIVAL trên đầu, căn nhà ma quái, đầm lầy dưới chân)
                        GUI.DrawTexture(new Rect(0, 0, sw, sh), menuBackgroundTexture, ScaleMode.StretchToFill);
                    }
                    else if (backgroundScaleMode == MenuBackgroundScaleMode.FitCenter)
                    {
                        float imgAspect = (float)menuBackgroundTexture.width / menuBackgroundTexture.height;
                        float screenAspect = sw / sh;
                        float drawW, drawH;
                        if (screenAspect > imgAspect)
                        {
                            drawH = sh;
                            drawW = sh * imgAspect;
                        }
                        else
                        {
                            drawW = sw;
                            drawH = sw / imgAspect;
                        }
                        Rect bgRect = new Rect((sw - drawW) * 0.5f, (sh - drawH) * 0.5f, drawW, drawH);
                        GUI.DrawTexture(bgRect, menuBackgroundTexture, ScaleMode.StretchToFill);
                    }
                    else
                    {
                        GUI.DrawTexture(new Rect(0, 0, sw, sh), menuBackgroundTexture, ScaleMode.ScaleAndCrop);
                    }
                }
                else
                {
                    GUI.color = new Color(0.04f, 0.04f, 0.06f, 1f);
                    GUI.DrawTexture(new Rect(0, 0, sw, sh), fullDarkVeilTex);
                }

                // Dải Smoky Gradient góc trái: Tạo bóng râm mờ đen phía sau cột nút bấm (35% màn hình)
                // Giữ trọn vẹn chữ SURVIVAL và toàn bộ căn nhà hoang ở giữa và bên phải không bị che mờ!
                float gradWidth = Mathf.Clamp(sw * 0.36f, 320f, 440f);
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(0, 0, gradWidth, sh), leftGradientTex);
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

                // Tên Game chính với bóng đổ
                string mainTitle = "DEAD ISLAND";
                GUI.Label(new Rect(startX + 2f, currentY + 2f, 400f, 52f), mainTitle, titleShadowStyle);
                GUI.Label(new Rect(startX, currentY, 400f, 52f), mainTitle, titleStyle);
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
                if (audioSource != null) audioSource.volume = masterVolume * 0.75f;
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
                Screen.fullScreen = isFullscreen;
                PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
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
    }
}

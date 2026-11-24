using UnityEngine;
using UnityEngine.SceneManagement;
using HorrorGame.Player;
using HorrorGame.Environment;

namespace HorrorGame.UI
{
    /// <summary>
    /// PlayerDeathUI — Màn hình "BẠN ĐÃ CHẾT" phong cách The Forest:
    /// - Khi HP về 0: Camera ngã nghiêng xuống đất, viền máu đỏ thẫm phủ quanh màn hình rồi tối dần.
    /// - Hiện tiêu đề "BẠN ĐÃ CHẾT" đỏ sẫm kèm số ngày sống sót ("BẠN ĐÃ SỐNG SÓT ĐƯỢC X NGÀY") và nguyên nhân tử vong.
    /// - Các nút tương tác chuẩn: Chơi lại, Về Main Menu, Thoát game.
    /// - Tự động kích hoạt trong mọi màn chơi (trừ MainMenu), không cần kéo thả thủ công.
    /// </summary>
    public class PlayerDeathUI : MonoBehaviour
    {
        public static PlayerDeathUI Instance { get; private set; }
        public static bool IsShowingDeathScreen { get; private set; }

        [Header("── Cấu Hình Hiệu Ứng ──")]
        public float fadeDuration = 2.2f;
        public float cameraFallSpeed = 2.6f;

        private PlayerStats playerStats;
        private Transform playerCamera;
        private Vector3 origCamLocalPos;
        private Quaternion origCamLocalRot;
        private bool capturedOrigCam = false;

        private bool isDeadTriggered = false;
        private float deathElapsed = 0f;
        private int survivedDays = 1;
        private string deathReason = "Bị sinh vật trên đảo sát hại";

        // Dynamic textures & styles (khởi tạo 1 lần)
        private Texture2D blackTex;
        private Texture2D bloodVignetteTex;
        private Texture2D redLineTex;
        private Texture2D btnNormalTex;
        private Texture2D btnHoverTex;

        private GUIStyle titleStyle;
        private GUIStyle titleShadowStyle;
        private GUIStyle subTitleStyle;
        private GUIStyle daysStyle;
        private GUIStyle reasonStyle;
        private GUIStyle btnStyle;
        private bool stylesReady = false;

        private static bool sceneHookRegistered = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            if (!sceneHookRegistered)
            {
                sceneHookRegistered = true;
                SceneManager.sceneLoaded += OnAnySceneLoaded;
            }
            EnsureInstanceForActiveScene();
        }

        private static void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureInstanceForActiveScene();
        }

        private static void EnsureInstanceForActiveScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName.ToLower().Contains("mainmenu")) return;

            if (FindObjectOfType<PlayerDeathUI>() == null)
            {
                GameObject obj = new GameObject("[PlayerDeathUI]");
                obj.AddComponent<PlayerDeathUI>();
            }
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            IsShowingDeathScreen = false;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                IsShowingDeathScreen = false;
            }
            if (playerStats != null)
            {
                playerStats.OnPlayerDeath -= HandlePlayerDeath;
            }
        }

        private void Start()
        {
            BindPlayerStats();
        }

        private void BindPlayerStats()
        {
            if (playerStats != null) return;

            playerStats = PlayerStats.Instance;
            if (playerStats == null) playerStats = FindObjectOfType<PlayerStats>();

            if (playerStats != null)
            {
                playerStats.OnPlayerDeath -= HandlePlayerDeath;
                playerStats.OnPlayerDeath += HandlePlayerDeath;

                PlayerController pc = playerStats.GetComponent<PlayerController>();
                if (pc != null && pc.playerCamera != null)
                {
                    playerCamera = pc.playerCamera;
                }
                else if (Camera.main != null)
                {
                    playerCamera = Camera.main.transform;
                }
            }
        }

        private void Update()
        {
            if (playerStats == null)
            {
                BindPlayerStats();
            }

            // Tự động bắt trường hợp isDead bật trước khi kịp đăng ký event
            if (!isDeadTriggered && playerStats != null && playerStats.isDead)
            {
                HandlePlayerDeath();
            }

#if UNITY_EDITOR
            // Phím test nhanh trong Editor: nhấn LeftCtrl + K để xem ngay màn hình Bạn Đã Chết
            if (!isDeadTriggered && playerStats != null && Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.K))
            {
                playerStats.TakeDamage(9999f);
            }
#endif

            if (!isDeadTriggered) return;

            deathElapsed += Time.unscaledDeltaTime;

            // Giữ con trỏ chuột mở để người chơi bấm nút
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible) Cursor.visible = true;

            // Hiệu ứng Camera ngã gục nghiêng xuống mặt đất giống The Forest
            if (playerCamera != null)
            {
                if (!capturedOrigCam)
                {
                    origCamLocalPos = playerCamera.localPosition;
                    origCamLocalRot = playerCamera.localRotation;
                    capturedOrigCam = true;
                }

                float fallT = Mathf.Clamp01(deathElapsed * cameraFallSpeed * 0.65f);
                float smoothT = fallT * fallT * (3f - 2f * fallT);

                Vector3 targetPos = new Vector3(origCamLocalPos.x, origCamLocalPos.y - 1.15f, origCamLocalPos.z);
                Quaternion targetRot = Quaternion.Euler(14f, origCamLocalRot.eulerAngles.y, 68f);

                playerCamera.localPosition = Vector3.Lerp(origCamLocalPos, targetPos, smoothT);
                playerCamera.localRotation = Quaternion.Slerp(origCamLocalRot, targetRot, smoothT);
            }
        }

        private void HandlePlayerDeath()
        {
            if (isDeadTriggered) return;
            isDeadTriggered = true;
            IsShowingDeathScreen = true;
            deathElapsed = 0f;

            // Lấy số ngày đã sống sót
            survivedDays = (DayNightCycle.Instance != null) ? Mathf.Max(1, DayNightCycle.Instance.currentDay) : 1;

            // Xác định nguyên nhân tử vong
            if (playerStats != null)
            {
                if (playerStats.currentHunger <= 0.1f)
                    deathReason = "Nguyên nhân: Kiệt sức vì cơn đói hành hạ";
                else if (playerStats.currentThirst <= 0.1f)
                    deathReason = "Nguyên nhân: Mất nước nghiêm trọng";
                else
                    deathReason = "Nguyên nhân: Không thể sống sót trước hiểm họa trên đảo";
            }

            // Mở khóa chuột
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log(string.Format("💀 [PlayerDeathUI] Hiển thị màn hình BẠN ĐÃ CHẾT (Sống sót {0} ngày).", survivedDays));
        }

        private void InitStylesAndTextures()
        {
            if (stylesReady) return;
            stylesReady = true;

            blackTex = MakeSolidTex(Color.black);
            redLineTex = MakeSolidTex(new Color(0.75f, 0.10f, 0.10f, 0.85f));
            btnNormalTex = MakeSolidTex(new Color(0.10f, 0.08f, 0.08f, 0.82f));
            btnHoverTex = MakeSolidTex(new Color(0.45f, 0.08f, 0.08f, 0.92f));
            bloodVignetteTex = MakeBloodVignetteTex(128, 128);

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 62;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = new Color(0.84f, 0.11f, 0.11f, 1f);

            titleShadowStyle = new GUIStyle(titleStyle);
            titleShadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.9f);

            subTitleStyle = new GUIStyle(GUI.skin.label);
            subTitleStyle.fontSize = 18;
            subTitleStyle.fontStyle = FontStyle.Italic;
            subTitleStyle.alignment = TextAnchor.MiddleCenter;
            subTitleStyle.normal.textColor = new Color(0.75f, 0.72f, 0.70f, 0.9f);

            daysStyle = new GUIStyle(GUI.skin.label);
            daysStyle.fontSize = 24;
            daysStyle.fontStyle = FontStyle.Bold;
            daysStyle.alignment = TextAnchor.MiddleCenter;
            daysStyle.normal.textColor = new Color(0.95f, 0.92f, 0.86f, 1f);

            reasonStyle = new GUIStyle(GUI.skin.label);
            reasonStyle.fontSize = 15;
            reasonStyle.alignment = TextAnchor.MiddleCenter;
            reasonStyle.normal.textColor = new Color(0.72f, 0.68f, 0.65f, 0.9f);

            btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.fontSize = 17;
            btnStyle.fontStyle = FontStyle.Bold;
            btnStyle.alignment = TextAnchor.MiddleCenter;
            btnStyle.normal.background = btnNormalTex;
            btnStyle.normal.textColor = new Color(0.92f, 0.90f, 0.88f, 1f);
            btnStyle.hover.background = btnHoverTex;
            btnStyle.hover.textColor = Color.white;
            btnStyle.active.background = btnHoverTex;
            btnStyle.active.textColor = Color.white;
        }

        private Texture2D MakeSolidTex(Color c)
        {
            Texture2D t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        /// <summary>
        /// Tạo texture viền máu đỏ thẫm (Vignette) mượt mà ở 4 góc màn hình
        /// </summary>
        private Texture2D MakeBloodVignetteTex(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x / (float)(w - 1)) * 2f - 1f;
                    float ny = (y / (float)(h - 1)) * 2f - 1f;
                    float dist = Mathf.Sqrt(nx * nx + ny * ny);
                    float edge = Mathf.Clamp01((dist - 0.45f) / 0.95f);
                    float alpha = edge * edge * 0.85f;
                    tex.SetPixel(x, y, new Color(0.48f, 0.02f, 0.02f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            if (!isDeadTriggered) return;

            InitStylesAndTextures();

            float sw = Screen.width;
            float sh = Screen.height;
            Color prevColor = GUI.color;

            // 1. Viền máu đỏ thẫm phủ quanh màn hình ngay khi ngã gục
            float bloodAlpha = Mathf.Clamp01(deathElapsed / 0.75f);
            float pulse = 0.88f + Mathf.Sin(deathElapsed * 2.2f) * 0.12f;
            GUI.color = new Color(1f, 1f, 1f, bloodAlpha * pulse);
            GUI.DrawTexture(new Rect(0f, 0f, sw, sh), bloodVignetteTex);

            // 2. Màn phủ tối dần u ám (giữ hơi mờ để thấy góc camera ngã xuống đất)
            float darkAlpha = Mathf.Clamp01(deathElapsed / fadeDuration) * 0.84f;
            GUI.color = new Color(1f, 1f, 1f, darkAlpha);
            GUI.DrawTexture(new Rect(0f, 0f, sw, sh), blackTex);

            // 3. Cụm chữ trung tâm "BẠN ĐÃ CHẾT" phong cách The Forest (hiện dần từ 0.7s)
            float textAlpha = Mathf.Clamp01((deathElapsed - 0.65f) / 1.1f);
            if (textAlpha > 0.01f)
            {
                GUI.color = new Color(1f, 1f, 1f, textAlpha);

                float centerY = sh * 0.34f;
                float lineW = Mathf.Min(540f, sw * 0.65f);
                float lineX = (sw - lineW) * 0.5f;

                // Đường kẻ đỏ mảnh phía trên
                GUI.DrawTexture(new Rect(lineX, centerY - 12f, lineW, 2f), redLineTex);

                // Tiêu đề chính "BẠN ĐÃ CHẾT" + bóng đổ
                Rect titleRect = new Rect(0f, centerY, sw, 78f);
                GUI.Label(new Rect(titleRect.x + 3f, titleRect.y + 3f, titleRect.width, titleRect.height), "BẠN ĐÃ CHẾT", titleShadowStyle);
                GUI.Label(titleRect, "BẠN ĐÃ CHẾT", titleStyle);

                // Phụ đề phong cách The Forest
                GUI.Label(new Rect(0f, centerY + 68f, sw, 26f), "Y O U   A R E   D E A D", subTitleStyle);

                // Đường kẻ đỏ mảnh phía dưới
                GUI.DrawTexture(new Rect(lineX, centerY + 102f, lineW, 2f), redLineTex);

                // Thống kê số ngày sống sót
                string survivedText = string.Format("BẠN ĐÃ SỐNG SÓT ĐƯỢC {0} NGÀY", survivedDays);
                GUI.Label(new Rect(0f, centerY + 122f, sw, 34f), survivedText, daysStyle);

                // Nguyên nhân tử vong
                GUI.Label(new Rect(0f, centerY + 158f, sw, 26f), deathReason, reasonStyle);
            }

            // 4. Các nút hành động (hiện mượt sau 1.6s để người chơi không bấm nhầm lúc đang combat)
            float btnAlpha = Mathf.Clamp01((deathElapsed - 1.6f) / 0.8f);
            if (btnAlpha > 0.01f)
            {
                GUI.color = new Color(1f, 1f, 1f, btnAlpha);

                float btnW = 280f;
                float btnH = 44f;
                float spacing = 12f;
                float startX = (sw - btnW) * 0.5f;
                float startY = sh * 0.66f;

                if (GUI.Button(new Rect(startX, startY, btnW, btnH), "↻   CHƠI LẠI (SỐNG SÓT LẠI)", btnStyle))
                {
                    RestartSurvival();
                }

                if (GUI.Button(new Rect(startX, startY + btnH + spacing, btnW, btnH), "⌂   VỀ MÀN HÌNH CHÍNH", btnStyle))
                {
                    ReturnToMainMenu();
                }

                if (GUI.Button(new Rect(startX, startY + (btnH + spacing) * 2f, btnW, btnH), "✕   THOÁT GAME", btnStyle))
                {
                    QuitGame();
                }
            }

            GUI.color = prevColor;
        }

        private void RestartSurvival()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            IsShowingDeathScreen = false;
            GameMenuManager.hasStartedFromMainMenu = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            IsShowingDeathScreen = false;
            GameMenuManager.hasStartedFromMainMenu = false;

            if (GameMenuManager.Instance != null)
            {
                GameMenuManager.Instance.LoadMainMenu();
            }
            else
            {
                SceneManager.LoadScene("MainMenu");
            }
        }

        private void QuitGame()
        {
            Time.timeScale = 1f;
            IsShowingDeathScreen = false;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}


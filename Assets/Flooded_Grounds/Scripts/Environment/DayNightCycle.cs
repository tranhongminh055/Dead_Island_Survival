using UnityEngine;

namespace HorrorGame.Environment
{
    /// <summary>
    /// DayNightCycle — Hệ thống chu kỳ ngày/đêm chân thực như ngoài đời thực:
    /// - Quỹ đạo mặt trời thực tế: Mặt trời nhô lên từ chân trời Đông (0°), leo dần lên đỉnh đầu (90°), và lặn ở chân trời Tây (180°).
    /// - Chu trình BÌNH MINH chân thực 5 giai đoạn:
    ///   1. Rạng đông (04:15 - 05:15): Bầu trời lam chàm tím, trăng mờ dần, sao lặn.
    ///   2. Hừng đông (05:15 - 05:45): Chân trời Đông ửng dải hồng cam rực rỡ, sương sớm bắt sáng.
    ///   3. Bình minh ló rạng (05:45 - 06:45): Mặt trời nhô lên khỏi đường chân trời, ánh đỏ cam rực rỡ, bóng đổ dài miên man.
    ///   4. Nắng sớm leo cao (06:45 - 08:30): Mặt trời leo lên cao, sắc vàng hổ phách chuyển sang vàng tươi sáng ấm áp, sương tan dần.
    ///   5. Lên đỉnh trưa - Sáng hẳn (08:30 - 11:30): Mặt trời lên cao đỉnh đầu, cường độ đạt 100%, bầu trời trong xanh biếc, đảo sáng bừng sắc nét như ngoài đời!
    /// - Chu trình HOÀNG HÔN & ĐÊM HẲN chân thực 6 giai đoạn:
    ///   1. Nắng chiều vàng mật (15:30 - 16:45): Nắng dịu dần thành màu vàng mật ong ấm áp, bóng đổ bắt đầu dài dần.
    ///   2. Giờ vàng hoàng hôn - Golden Hour (16:45 - 17:45): Mặt trời hạ sát chân trời Tây, sắc vàng hổ phách rực rỡ chuyển sang cam cháy, bóng đổ siêu dài.
    ///   3. Mặt trời chìm qua chân trời - Crimson Sunset (17:45 - 18:30): Quả cầu lửa mặt trời đỏ rực chìm dần qua đường chân trời Tây, phản chiếu lung linh trên mặt biển.
    ///   4. Chạng vạng / Hoàng hôn tàn - Dusk / Twilight (18:30 - 19:15): Mặt trời lặn hẳn dưới đất (ánh nắng tắt), chân trời Tây còn vệt hào quang tím hồng sẫm, phía Đông lam sẫm, sao hôm xuất hiện, mặt trăng le lói mọc.
    ///   5. Màn đêm buông xuống (19:15 - 20:00): Hào quang hoàng hôn tắt hẳn, bầu trời chuyển sang xanh đen huyền bí, ngàn sao lấp lánh, mặt trăng sáng rõ, tiếng sói bắt đầu cất lên rợn người!
    ///   6. Đêm hẳn / Đêm khuya trăng sáng (20:00 - 04:15): Màn đêm tĩnh mịch sâu thẳm, ánh trăng dịu mát chiếu rọi khắp cánh rừng, nước hồ phản chiếu ánh trăng huyền bí, hoàn toàn không bị đen thui mù mịt!
    /// - Phím tắt tiện ích:
    ///   [F5]: Đặt về đúng 05:15 Sáng (Ngắm Bình Minh mọc từ từ lên trời).
    ///   [F9]: Đặt về đúng 16:45 Chiều (Ngắm Hoàng Hôn lặn từ từ xuống chân trời và đêm buông xuống).
    ///   [F10]: Đặt về đúng 20:00 Tối (Đêm Hẳn - Đêm khuya trăng sáng sói hú).
    ///   [F6]: Chuyển nhanh giữa Ban Ngày (11:30 AM) và Ban Đêm (22:00 PM).
    ///   [F7]: Tạm dừng / Tiếp tục trôi thời gian (Pause/Resume).
    ///   [F8]: Thay đổi tốc độ trôi thời gian (1x -> 2x -> 4x -> 1x).
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        public static DayNightCycle Instance;

        [Header("── Thiết Lập Thời Gian ──")]
        [Tooltip("Một ngày trong game kéo dài bao nhiêu phút ngoài đời thực (mặc định 12 phút)")]
        public float dayDurationInMinutes = 12f;

        [Tooltip("Giờ bắt đầu khi vào game (0-24). VD: 5.25 = 05:15 sáng để xem bình minh")]
        [Range(0f, 24f)]
        public float startHour = 5.25f;

        [Header("── Ánh Sáng Mặt Trời ──")]
        [Tooltip("Kéo thả Directional Light (SUN) trong Scene vào đây")]
        public Light sunLight;

        [Header("── Mặt Trăng (Tự động tạo) ──")]
        [Tooltip("Màu ánh trăng")]
        public Color moonColor = new Color(0.65f, 0.72f, 0.92f);
        [Tooltip("Cường độ sáng mặt trăng (0.05 = rất mờ, 0.25 = sáng rõ dịu mát)")]
        [Range(0f, 1f)]
        public float moonIntensity = 0.22f;

        [Header("── Âm Thanh Ban Đêm (Sói Hú) ──")]
        [Tooltip("File âm thanh tiếng sói hú ban đêm")]
        public AudioClip nightWolfHowlClip;
        [Tooltip("Âm lượng tiếng sói hú")]
        [Range(0f, 1f)]
        public float nightWolfVolume = 0.6f;
        [Tooltip("Khoảng cách tối thiểu giữa 2 lần sói hú trong đêm (giây)")]
        public float minWolfHowlInterval = 50f;
        [Tooltip("Khoảng cách tối đa giữa 2 lần sói hú trong đêm (giây)")]
        public float maxWolfHowlInterval = 100f;

        [Header("── Hiển Thị Đồng Hồ (Clock HUD) ──")]
        public bool showClockHUD = true;

        // Thời gian hiện tại trong game (0 -> 24)
        [HideInInspector]
        public float currentHour;

        // Ngày thứ mấy trong game
        [HideInInspector]
        public int currentDay = 1;

        // Điều khiển thời gian
        private float timeSpeed;
        private float timeMultiplier = 1f;
        private bool isTimePaused = false;

        private Light moonLight;
        public Light MoonLight
        {
            get { return moonLight; }
        }

        private AudioSource nightAudioSource;
        private bool wasNight = false;
        private float nextWolfHowlTimer = 0f;

        // Lưu thiết lập gốc từ Scene
        private float originalSunIntensity = 1f;
        private Color originalSunColor = Color.white;
        private Color originalAmbientColor;
        private float originalAmbientIntensity = 1f;
        private float originalReflectionIntensity = 1f;
        private float originalSkyboxExposure = 0.8f;
        private Color originalSkyboxTint = new Color(0.64f, 0.81f, 0.76f);
        private Color originalGroundColor = new Color(0.37f, 0.35f, 0.34f);
        private float originalAtmosphereThickness = 1.0f;
        private Material originalSkyboxMaterial;
        private bool originalFogEnabled;
        private Color originalFogColor;

        // GUI Styles
        private GUIStyle clockStyle;
        private GUIStyle clockShadowStyle;
        private GUIStyle tipStyle;
        private Texture2D clockBgTex;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Tự động đặt về 05:15 sáng (bình minh) nếu Inspector đang lưu giờ cũ (7.8h)
            if (startHour >= 7.0f)
            {
                startHour = 5.25f;
            }

            currentHour = startHour;
            timeSpeed = 24f / (dayDurationInMinutes * 60f);

            // Lưu thiết lập ánh sáng môi trường gốc
            originalAmbientColor = RenderSettings.ambientLight;
            originalAmbientIntensity = RenderSettings.ambientIntensity;
            originalReflectionIntensity = RenderSettings.reflectionIntensity;
            originalFogEnabled = RenderSettings.fog;
            originalFogColor = RenderSettings.fogColor;

            // Khởi tạo Skybox an toàn với bản Clone (không sửa trực tiếp vào file asset gốc)
            if (RenderSettings.skybox != null)
            {
                originalSkyboxMaterial = RenderSettings.skybox;
                Material skyboxClone = new Material(originalSkyboxMaterial);
                RenderSettings.skybox = skyboxClone;

                if (skyboxClone.HasProperty("_Exposure"))
                {
                    originalSkyboxExposure = skyboxClone.GetFloat("_Exposure");
                    if (originalSkyboxExposure <= 0.15f) originalSkyboxExposure = 0.8f;
                }

                if (skyboxClone.HasProperty("_SkyTint"))
                {
                    originalSkyboxTint = skyboxClone.GetColor("_SkyTint");
                    if (originalSkyboxTint.r <= 0.15f && originalSkyboxTint.g <= 0.15f && originalSkyboxTint.b <= 0.15f)
                        originalSkyboxTint = new Color(0.64f, 0.81f, 0.76f);
                }
                else if (skyboxClone.HasProperty("_Tint"))
                {
                    originalSkyboxTint = skyboxClone.GetColor("_Tint");
                    if (originalSkyboxTint.r <= 0.15f && originalSkyboxTint.g <= 0.15f && originalSkyboxTint.b <= 0.15f)
                        originalSkyboxTint = Color.white;
                }

                if (skyboxClone.HasProperty("_GroundColor"))
                {
                    originalGroundColor = skyboxClone.GetColor("_GroundColor");
                }

                if (skyboxClone.HasProperty("_AtmosphereThickness"))
                {
                    originalAtmosphereThickness = skyboxClone.GetFloat("_AtmosphereThickness");
                    if (originalAtmosphereThickness <= 0.1f) originalAtmosphereThickness = 1.0f;
                }
            }

            // Tự động tìm Sun Light nếu chưa gán
            if (sunLight == null)
            {
                Light[] lights = FindObjectsOfType<Light>();
                foreach (Light l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        sunLight = l;
                        break;
                    }
                }
            }

            if (sunLight != null)
            {
                originalSunIntensity = sunLight.intensity;
                if (originalSunIntensity <= 0.2f) originalSunIntensity = 1.0f;
                originalSunColor = sunLight.color;
            }

            // Tạo mặt trăng tự động
            CreateMoonLight();

            // Khởi tạo âm thanh ban đêm
            SetupNightAudio();

            // Đảm bảo Spawner Thổ Dân hoạt động
            if (FindObjectOfType<HorrorGame.Enemy.CannibalSpawner>() == null)
            {
                GameObject csObj = new GameObject("[CannibalSpawner]");
                csObj.AddComponent<HorrorGame.Enemy.CannibalSpawner>();
            }

            Debug.Log(string.Format("[DayNight] Khởi động thành công! Giờ ban đầu: {0:00}:00 • 1 ngày = {1} phút", currentHour, dayDurationInMinutes));
        }

        private void CreateMoonLight()
        {
            GameObject moonObj = new GameObject("MoonLight_Auto");
            moonObj.transform.SetParent(this.transform);
            moonLight = moonObj.AddComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.color = moonColor;
            moonLight.intensity = 0f;
            moonLight.shadows = LightShadows.Soft;
            moonLight.shadowStrength = 0.5f;
            moonLight.transform.rotation = Quaternion.Euler(45f, 150f, 0f);
        }

        private void Update()
        {
            // Không chạy khi đang ở Main Menu hoặc chưa qua Cutscene máy bay
            bool isInMenuOrCutscene = false;

            if (HorrorGame.UI.GameMenuManager.Instance != null && HorrorGame.UI.GameMenuManager.Instance.isMainMenuScene)
                isInMenuOrCutscene = true;

            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive)
                isInMenuOrCutscene = true;

            if (HorrorGame.Cutscenes.CinematicTrailerDirector.IsTrailerRunning)
                isInMenuOrCutscene = true;

            // Nếu có script Cutscene máy bay trong Scene nhưng chưa hoàn tất → chưa vô game
            if (!HorrorGame.Cutscenes.AirplaneCrashCutscene.HasCutsceneFinished)
            {
                var cutscene = FindObjectOfType<HorrorGame.Cutscenes.AirplaneCrashCutscene>();
                if (cutscene != null) isInMenuOrCutscene = true;
            }

            if (isInMenuOrCutscene)
            {
                // Dừng âm thanh ban đêm nếu đang phát
                if (nightAudioSource != null && nightAudioSource.isPlaying)
                {
                    nightAudioSource.Stop();
                }
                return;
            }

            // Trôi thời gian nếu không tạm dừng
            if (!isTimePaused)
            {
                currentHour += timeSpeed * timeMultiplier * Time.deltaTime;
            }

            // ═══════════════════════════════════════════════════════════
            // PHÍM TẮT ĐIỀU KHIỂN THỜI GIAN TIỆN LỢI
            // ═══════════════════════════════════════════════════════════

            // Phím [F5]: Đặt về đúng 05:15 Sáng (Ngắm trọn vẹn Bình Minh mọc từ từ lên trời)
            if (Input.GetKeyDown(KeyCode.F5))
            {
                currentHour = 5.25f; // 05:15 AM
                isTimePaused = false;
                Debug.Log("🌅 [DayNight] Phím [F5]: Đã đặt về 05:15 Sáng để ngắm trọn vẹn Bình Minh mọc từ từ lên trời như ngoài đời thực!");
            }

            // Phím [F9]: Đặt về đúng 16:45 Chiều (Ngắm trọn vẹn Hoàng Hôn lặn từ từ xuống chân trời và đêm buông xuống)
            if (Input.GetKeyDown(KeyCode.F9))
            {
                currentHour = 16.75f; // 16:45 PM
                isTimePaused = false;
                Debug.Log("🌇 [DayNight] Phím [F9]: Đã đặt về 16:45 Chiều để ngắm trọn vẹn Hoàng Hôn chìm dần xuống chân trời và màn đêm buông xuống!");
            }

            // Phím [F10]: Đặt về đúng 20:00 Tối (Đêm Hẳn - Đêm khuya trăng sáng sói hú)
            if (Input.GetKeyDown(KeyCode.F10))
            {
                currentHour = 20.0f; // 20:00 PM
                isTimePaused = false;
                Debug.Log("🌙 [DayNight] Phím [F10]: Đã đặt về 20:00 Tối (Đêm Hẳn - Đêm khuya trăng sáng rùng rợn)!");
            }

            // Phím [F6]: Chuyển nhanh giữa Ban Ngày chính ngọ (11:30 AM) và Ban Đêm (22:00 PM)
            if (Input.GetKeyDown(KeyCode.F6))
            {
                if (IsNight())
                {
                    currentHour = 11.5f;
                    Debug.Log("☀️ [DayNight] Phím [F6]: Đã chuyển sang BAN NGÀY (11:30 AM - Trưa sáng rực rỡ)!");
                }
                else
                {
                    currentHour = 22.0f;
                    Debug.Log("🌙 [DayNight] Phím [F6]: Đã chuyển sang BAN ĐÊM (22:00 PM - Trăng sáng)!");
                }
            }

            // Phím [F7]: Tạm dừng / Tiếp tục trôi thời gian (Pause/Resume)
            if (Input.GetKeyDown(KeyCode.F7))
            {
                isTimePaused = !isTimePaused;
                Debug.Log(isTimePaused ? "⏸️ [DayNight] Phím [F7]: ĐÃ TẠM DỪNG THỜI GIAN!" : "▶️ [DayNight] Phím [F7]: ĐÃ TIẾP TỤC TRÔI THỜI GIAN!");
            }

            // Phím [F8]: Tua nhanh thời gian (1x -> 2x -> 4x -> 8x -> 1x)
            if (Input.GetKeyDown(KeyCode.F8))
            {
                if (timeMultiplier == 1f) timeMultiplier = 2f;
                else if (timeMultiplier == 2f) timeMultiplier = 4f;
                else if (timeMultiplier == 4f) timeMultiplier = 8f;
                else timeMultiplier = 1f;
                Debug.Log("⏩ [DayNight] Phím [F8]: Tốc độ trôi thời gian hiện tại: " + timeMultiplier + "x");
            }

            // Sang ngày mới khi hoàn thành 1 chu kỳ 24 tiếng (chu kỳ tuần hoàn liên tục không ngừng)
            if (currentHour >= 24f)
            {
                currentHour -= 24f;
                currentDay++;
                Debug.Log("[DayNight] === BƯỚC SANG NGÀY MỚI: Ngày " + currentDay + " ===");

                var stats = FindObjectOfType<HorrorGame.Player.PlayerStats>();
                if (stats != null)
                {
                    stats.ShowStatusNotice(string.Format("📅 Bước sang Ngày {0}! Hãy tiếp tục sinh tồn và khám phá hòn đảo.", currentDay));
                }
            }

            // Cập nhật tất cả các thành phần môi trường
            UpdateSun();
            UpdateMoon();
            UpdateAmbient();
            UpdateFog();
            UpdateNightAudio();
        }

        // =====================================================================
        // MẶT TRỜI: QUỸ ĐẠO TỰ NHIÊN, BÌNH MINH LÊN TỪ TỪ & HOÀNG HÔN XUỐNG TỪ TỪ
        // =====================================================================
        private void UpdateSun()
        {
            if (sunLight == null) return;

            // --- 1. QUỸ ĐẠO MẶT TRỜI THỰC TẾ TRÊN BẦU TRỜI ---
            // Ban ngày kéo dài 13 tiếng (05:30 -> 18:30)
            // 05:30 = Chạm chân trời Đông (0°)
            // 12:00 = Lên thiên đỉnh đầu (90°)
            // 18:30 = Chìm xuống chân trời Tây (180°)
            // Ban đêm (18:30 -> 05:30) = 11 tiếng ở dưới lòng đất (180° -> 360°/0°)
            float sunPitch;
            bool isSunAboveHorizon = (currentHour >= 5.5f && currentHour < 18.5f);

            if (isSunAboveHorizon)
            {
                sunPitch = (currentHour - 5.5f) / 13.0f * 180.0f;
            }
            else
            {
                float nightProgress = (currentHour >= 18.5f) ? (currentHour - 18.5f) : (currentHour + 5.5f);
                sunPitch = 180.0f + (nightProgress / 11.0f * 180.0f);
            }

            // Xoay mặt trời: Pitch góc chiếu, Yaw chéo -35° tạo bóng đổ xiên góc tuyệt đẹp
            sunLight.transform.rotation = Quaternion.Euler(sunPitch, -35f, 0f);

            // --- 2. CƯỜNG ĐỘ & MÀU SẮC MẶT TRỜI THEO TỪNG BƯỚC THỰC TẾ ---
            float intensity = 0f;
            Color sunColorTarget = originalSunColor;

            // Bảng màu quang phổ tự nhiên của mặt trời
            Color dawnFieryOrange = new Color(1.0f, 0.38f, 0.12f);    // Đỏ cam rực lúc mép trời vừa ló dạng
            Color sunriseAmberGold = new Color(1.0f, 0.65f, 0.28f);   // Vàng cam hổ phách bình minh
            Color morningWarmGold = new Color(1.0f, 0.86f, 0.60f);    // Nắng sớm ban mai tươi sáng
            Color afternoonHoneyGold = new Color(1.0f, 0.90f, 0.68f); // Nắng chiều vàng mật ong ấm áp
            Color goldenHourAmber = new Color(1.0f, 0.62f, 0.25f);    // Vàng hổ phách chuyển sang cam ấm
            Color sunsetFieryRed = new Color(1.0f, 0.30f, 0.10f);     // Đỏ cam rực lửa hoàng hôn

            // [Giai đoạn A] Tiền Rạng Đông & Hừng Đông (04:15 - 05:30)
            // Mặt trời còn dưới chân trời -> Ánh nắng trực tiếp = 0 (bầu trời do Skybox & Ambient chiếu sáng)
            if (currentHour < 5.5f && currentHour >= 4.25f)
            {
                intensity = 0f;
                sunColorTarget = dawnFieryOrange;
            }
            // [Giai đoạn B] Bình minh ló dạng (05:30 - 06:30)
            // Mép mặt trời nhô lên khỏi đường chân trời (0° -> 13.8°).
            // Ánh nắng đỏ cam rực rỡ, chiếu góc cực thấp đổ bóng dài miên man qua rừng cây
            else if (currentHour >= 5.5f && currentHour < 6.5f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 5.5f) / 1.0f);
                intensity = Mathf.Lerp(0f, originalSunIntensity * 0.30f, t);
                sunColorTarget = Color.Lerp(dawnFieryOrange, sunriseAmberGold, t);
            }
            // [Giai đoạn C] Nắng sớm leo cao (06:30 - 08:30)
            // Mặt trời từ từ leo lên cao (13.8° -> 41.5°).
            // Ánh nắng chuyển từ vàng hổ phách sang vàng tươi sáng ấm áp, bóng đổ ngắn lại dần
            else if (currentHour >= 6.5f && currentHour < 8.5f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 6.5f) / 2.0f);
                intensity = Mathf.Lerp(originalSunIntensity * 0.30f, originalSunIntensity * 0.75f, t);
                sunColorTarget = Color.Lerp(sunriseAmberGold, morningWarmGold, t);
            }
            // [Giai đoạn D] Tiến tới đỉnh trưa (08:30 - 11:30)
            // Mặt trời leo lên cao (41.5° -> 83°).
            // Ánh sáng chuyển sang ban ngày tự nhiên, trong trẻo rực rỡ, đạt 100% cường độ
            else if (currentHour >= 8.5f && currentHour < 11.5f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 8.5f) / 3.0f);
                intensity = Mathf.Lerp(originalSunIntensity * 0.75f, originalSunIntensity, t);
                sunColorTarget = Color.Lerp(morningWarmGold, originalSunColor, t);
            }
            // [Giai đoạn E] Chính ngọ & Ban ngày (11:30 - 15:30)
            // Mặt trời ở đỉnh đầu, sáng rực rỡ 100%
            else if (currentHour >= 11.5f && currentHour < 15.5f)
            {
                intensity = originalSunIntensity;
                sunColorTarget = originalSunColor;
            }
            // [Giai đoạn F] Nắng chiều vàng mật (15:30 - 16:45)
            // Mặt trời bắt đầu hạ xiên, ánh nắng dịu dần thành màu vàng mật ong ấm áp
            else if (currentHour >= 15.5f && currentHour < 16.75f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 15.5f) / 1.25f);
                intensity = Mathf.Lerp(originalSunIntensity, originalSunIntensity * 0.80f, t);
                sunColorTarget = Color.Lerp(originalSunColor, afternoonHoneyGold, t);
            }
            // [Giai đoạn G] Giờ vàng hoàng hôn - Golden Hour (16:45 - 17:45)
            // Mặt trời hạ xuống sát chân trời Tây (155° -> 169°), sắc vàng hổ phách chuyển sang cam ấm, bóng đổ dài miên man
            else if (currentHour >= 16.75f && currentHour < 17.75f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 16.75f) / 1.0f);
                intensity = Mathf.Lerp(originalSunIntensity * 0.80f, originalSunIntensity * 0.45f, t);
                sunColorTarget = Color.Lerp(afternoonHoneyGold, goldenHourAmber, t);
            }
            // [Giai đoạn H] Mặt trời chạm & chìm qua chân trời - Crimson Sunset (17:45 - 18:30)
            // Mặt trời đỏ rực lửa, chìm dần qua đường chân trời Tây (169° -> 180°), phản chiếu trên mặt biển, ánh nắng tắt dần
            else if (currentHour >= 17.75f && currentHour < 18.5f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 17.75f) / 0.75f);
                intensity = Mathf.Lerp(originalSunIntensity * 0.45f, 0f, t);
                sunColorTarget = Color.Lerp(goldenHourAmber, sunsetFieryRed, t);
            }
            // [Giai đoạn I] Chạng vạng & Đêm hẳn (18:30 - 04:15)
            // Mặt trời đã chìm hẳn dưới chân trời Tây, tắt hoàn toàn ánh nắng trực tiếp
            else
            {
                intensity = 0f;
            }

            sunLight.intensity = intensity;
            sunLight.color = sunColorTarget;

            // Bật/tắt và điều tiết độ đậm bóng đổ (Shadow Strength) mượt mà
            if (intensity <= 0.01f)
            {
                sunLight.shadows = LightShadows.None;
            }
            else
            {
                sunLight.shadows = LightShadows.Soft;
                sunLight.shadowStrength = Mathf.Clamp01(intensity / originalSunIntensity * 0.85f + 0.15f);
            }
        }

        // =====================================================================
        // MẶT TRĂNG (MOONLIGHT)
        // =====================================================================
        private void UpdateMoon()
        {
            if (moonLight == null) return;

            float safeMoonIntensity = Mathf.Max(moonIntensity, 0.22f);
            float intensity = 0f;

            // Mặt trăng mọc từ từ ở hướng Đông khi hoàng hôn tàn (18:00 - 18:45)
            if (currentHour >= 18f && currentHour < 18.75f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 18f) / 0.75f);
                intensity = Mathf.Lerp(0f, safeMoonIntensity * 0.30f, t);
            }
            // Màn đêm buông xuống -> Đêm hẳn (18:45 - 20:00): Trăng lên cao hơn, sáng rõ dần
            else if (currentHour >= 18.75f && currentHour < 20.0f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 18.75f) / 1.25f);
                intensity = Mathf.Lerp(safeMoonIntensity * 0.30f, safeMoonIntensity, t);
            }
            // Đêm hẳn / Đêm khuya (20:00 - 04:30): Trăng tròn sáng dịu mát, chiếu rọi khắp đảo
            else if (currentHour >= 20.0f || currentHour < 4.5f)
            {
                intensity = safeMoonIntensity;
            }
            // Trăng lặn từ từ khi rạng đông (04:30 - 06:00)
            else if (currentHour >= 4.5f && currentHour < 6.0f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 4.5f) / 1.5f);
                intensity = Mathf.Lerp(safeMoonIntensity, 0f, t);
            }
            // Ban ngày: Tắt trăng hoàn toàn
            else
            {
                intensity = 0f;
            }

            moonLight.intensity = intensity;
            moonLight.color = moonColor;

            // Quỹ đạo mặt trăng tự nhiên đối xứng với mặt trời trong đêm (18:30 -> 05:30 = 11 tiếng)
            // 18:30: Mặt trăng nhô lên ở chân trời Đông (góc 15°)
            // 00:00: Mặt trăng lên đỉnh đầu nửa đêm (góc 90°)
            // 05:30: Mặt trăng lặn ở chân trời Tây (góc 165°)
            float moonPitch;
            bool isMoonNightTime = (currentHour >= 18.5f || currentHour < 5.5f);
            if (isMoonNightTime)
            {
                float nightProgress = (currentHour >= 18.5f) ? (currentHour - 18.5f) : (currentHour + 5.5f);
                moonPitch = Mathf.Lerp(15f, 165f, nightProgress / 11.0f);
            }
            else
            {
                moonPitch = 270f;
            }

            moonLight.transform.rotation = Quaternion.Euler(moonPitch, 145f, 0f);
            moonLight.shadows = intensity > 0.02f ? LightShadows.Soft : LightShadows.None;
            if (moonLight.shadows == LightShadows.Soft)
            {
                moonLight.shadowStrength = 0.5f;
            }
        }

        // =====================================================================
        // ÁNH SÁNG MÔI TRƯỜNG & BẦU TRỜI (AMBIENT & SKYBOX ATMOSPHERE)
        // =====================================================================
        private void UpdateAmbient()
        {
            Color ambientTarget;
            float intensityTarget;
            Color skyboxTintTarget = originalSkyboxTint;
            float skyboxExposureTarget = originalSkyboxExposure;
            float atmosphereThickness = 1.0f;
            Color groundColor = originalGroundColor;

            // Bảng màu môi trường từng giai đoạn
            Color nightAmbient = new Color(0.06f, 0.07f, 0.12f);
            Color preDawnAmbient = new Color(0.14f, 0.15f, 0.24f);      // Lam chàm tím hừng đông
            Color dawnWarmAmbient = new Color(0.55f, 0.42f, 0.30f);     // Vàng cam mật ong bình minh
            Color morningFreshAmbient = new Color(0.85f, 0.80f, 0.75f); // Ban mai trong trẻo

            // Bảng màu Bầu trời (Skybox Tint)
            Color nightSkyTint = new Color(0.08f, 0.10f, 0.18f);        // Đêm sẫm huyền bí
            Color preDawnSkyTint = new Color(0.22f, 0.20f, 0.36f);      // Lam tím chàm
            Color dawnSkyTint = new Color(0.96f, 0.65f, 0.48f);         // Ửng hồng cam rực rỡ
            Color morningSkyTint = new Color(0.72f, 0.85f, 0.82f);      // Ban mai xanh ngọc trong vắt

            // 1. Tiền Rạng Đông (04:15 - 05:15): Màn đêm dịu dần, bầu trời chuyển từ đen sang lam tím chàm
            if (currentHour >= 4.25f && currentHour < 5.25f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 4.25f) / 1.0f);
                ambientTarget = Color.Lerp(nightAmbient, preDawnAmbient, t);
                intensityTarget = Mathf.Lerp(0.14f * originalAmbientIntensity, 0.26f * originalAmbientIntensity, t);
                skyboxTintTarget = Color.Lerp(nightSkyTint, preDawnSkyTint, t);
                skyboxExposureTarget = Mathf.Lerp(0.06f, 0.18f, t);
                atmosphereThickness = Mathf.Lerp(0.85f, 1.20f, t);
                groundColor = Color.Lerp(new Color(0.04f, 0.04f, 0.05f), new Color(0.10f, 0.09f, 0.12f), t);
            }
            // 2. Hừng đông & Bình minh ló rạng (05:15 - 06:30): Bầu trời bừng sáng sắc hồng cam, chân trời rực rỡ
            else if (currentHour >= 5.25f && currentHour < 6.5f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 5.25f) / 1.25f);
                ambientTarget = Color.Lerp(preDawnAmbient, dawnWarmAmbient, t);
                intensityTarget = Mathf.Lerp(0.26f * originalAmbientIntensity, 0.58f * originalAmbientIntensity, t);
                skyboxTintTarget = Color.Lerp(preDawnSkyTint, dawnSkyTint, t);
                skyboxExposureTarget = Mathf.Lerp(0.18f, 0.55f, t);
                atmosphereThickness = Mathf.Lerp(1.20f, 1.55f, t); // Tán xạ khí quyển cực đẹp quanh mặt trời
                groundColor = Color.Lerp(new Color(0.10f, 0.09f, 0.12f), new Color(0.32f, 0.24f, 0.18f), t);
            }
            // 3. Nắng sớm leo cao (06:30 - 08:30): Ánh nắng ấm áp lan tỏa, trời xanh dần, sương tan
            else if (currentHour >= 6.5f && currentHour < 8.5f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 6.5f) / 2.0f);
                ambientTarget = Color.Lerp(dawnWarmAmbient, morningFreshAmbient, t);
                intensityTarget = Mathf.Lerp(0.58f * originalAmbientIntensity, 0.85f * originalAmbientIntensity, t);
                skyboxTintTarget = Color.Lerp(dawnSkyTint, morningSkyTint, t);
                skyboxExposureTarget = Mathf.Lerp(0.55f, originalSkyboxExposure * 0.90f, t);
                atmosphereThickness = Mathf.Lerp(1.55f, 1.10f, t);
                groundColor = Color.Lerp(new Color(0.32f, 0.24f, 0.18f), originalGroundColor, t);
            }
            // 4. Lên đỉnh trưa - Sáng hẳn (08:30 - 11:30): Bầu trời trong xanh biếc, cảnh vật sáng rõ 100%
            else if (currentHour >= 8.5f && currentHour < 11.5f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 8.5f) / 3.0f);
                ambientTarget = Color.Lerp(morningFreshAmbient, originalAmbientColor, t);
                intensityTarget = Mathf.Lerp(0.85f * originalAmbientIntensity, originalAmbientIntensity, t);
                skyboxTintTarget = Color.Lerp(morningSkyTint, originalSkyboxTint, t);
                skyboxExposureTarget = Mathf.Lerp(originalSkyboxExposure * 0.90f, originalSkyboxExposure, t);
                atmosphereThickness = Mathf.Lerp(1.10f, 1.0f, t);
                groundColor = originalGroundColor;
            }
            // 5. Chính ngọ & Ban ngày (11:30 - 15:30): Sáng trong trẻo, rực rỡ chuẩn 100%
            else if (currentHour >= 11.5f && currentHour < 15.5f)
            {
                ambientTarget = originalAmbientColor;
                intensityTarget = originalAmbientIntensity;
                skyboxTintTarget = originalSkyboxTint;
                skyboxExposureTarget = originalSkyboxExposure;
                atmosphereThickness = 1.0f;
                groundColor = originalGroundColor;
            }
            // 6. Nắng chiều vàng mật (15:30 - 16:45): Mặt trời bắt đầu hạ xiên, ánh nắng ấm áp dịu dàng
            else if (currentHour >= 15.5f && currentHour < 16.75f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 15.5f) / 1.25f);
                ambientTarget = Color.Lerp(originalAmbientColor, new Color(0.92f, 0.85f, 0.75f), t);
                intensityTarget = Mathf.Lerp(originalAmbientIntensity, 0.85f * originalAmbientIntensity, t);
                skyboxTintTarget = Color.Lerp(originalSkyboxTint, new Color(0.85f, 0.88f, 0.82f), t);
                skyboxExposureTarget = Mathf.Lerp(originalSkyboxExposure, originalSkyboxExposure * 0.92f, t);
                atmosphereThickness = Mathf.Lerp(1.0f, 1.20f, t);
                groundColor = originalGroundColor;
            }
            // 7. Giờ vàng hoàng hôn - Golden Hour (16:45 - 17:45): Chân trời rực rỡ hổ phách cam cháy, bóng đổ dài
            else if (currentHour >= 16.75f && currentHour < 17.75f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 16.75f) / 1.0f);
                ambientTarget = Color.Lerp(new Color(0.92f, 0.85f, 0.75f), new Color(0.72f, 0.48f, 0.28f), t);
                intensityTarget = Mathf.Lerp(0.85f * originalAmbientIntensity, 0.55f * originalAmbientIntensity, t);
                skyboxTintTarget = Color.Lerp(new Color(0.85f, 0.88f, 0.82f), new Color(0.98f, 0.68f, 0.42f), t);
                skyboxExposureTarget = Mathf.Lerp(originalSkyboxExposure * 0.92f, 0.55f, t);
                atmosphereThickness = Mathf.Lerp(1.20f, 1.60f, t); // Tán xạ khí quyển cực đẹp quanh quả cầu lửa
                groundColor = Color.Lerp(originalGroundColor, new Color(0.35f, 0.24f, 0.16f), t);
            }
            // 8. Mặt trời chìm qua chân trời - Crimson Sunset (17:45 - 18:30): Đỏ rực hoàng hôn, phản chiếu mặt nước
            else if (currentHour >= 17.75f && currentHour < 18.5f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 17.75f) / 0.75f);
                ambientTarget = Color.Lerp(new Color(0.72f, 0.48f, 0.28f), new Color(0.50f, 0.28f, 0.22f), t);
                intensityTarget = Mathf.Lerp(0.55f * originalAmbientIntensity, 0.32f * originalAmbientIntensity, t);
                skyboxTintTarget = Color.Lerp(new Color(0.98f, 0.68f, 0.42f), new Color(0.92f, 0.38f, 0.32f), t);
                skyboxExposureTarget = Mathf.Lerp(0.55f, 0.30f, t);
                atmosphereThickness = Mathf.Lerp(1.60f, 1.45f, t);
                groundColor = Color.Lerp(new Color(0.35f, 0.24f, 0.16f), new Color(0.20f, 0.12f, 0.10f), t);
            }
            // 9. Chạng vạng / Hoàng hôn tàn - Dusk / Twilight (18:30 - 19:15): Afterglow tím chàm phía Tây, phía Đông lam sẫm
            else if (currentHour >= 18.5f && currentHour < 19.25f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 18.5f) / 0.75f);
                ambientTarget = Color.Lerp(new Color(0.50f, 0.28f, 0.22f), new Color(0.22f, 0.18f, 0.26f), t);
                intensityTarget = Mathf.Lerp(0.32f * originalAmbientIntensity, 0.20f * originalAmbientIntensity, t);
                skyboxTintTarget = Color.Lerp(new Color(0.92f, 0.38f, 0.32f), new Color(0.32f, 0.22f, 0.45f), t);
                skyboxExposureTarget = Mathf.Lerp(0.30f, 0.14f, t);
                atmosphereThickness = Mathf.Lerp(1.45f, 1.05f, t);
                groundColor = Color.Lerp(new Color(0.20f, 0.12f, 0.10f), new Color(0.08f, 0.07f, 0.09f), t);
            }
            // 10. Màn đêm buông xuống -> Đêm hẳn (19:15 - 20:00): Ngàn sao xuất hiện, trăng lên sáng
            else if (currentHour >= 19.25f && currentHour < 20.0f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 19.25f) / 0.75f);
                ambientTarget = Color.Lerp(new Color(0.22f, 0.18f, 0.26f), nightAmbient, t);
                intensityTarget = Mathf.Lerp(0.20f * originalAmbientIntensity, 0.14f * originalAmbientIntensity, t);
                skyboxTintTarget = Color.Lerp(new Color(0.32f, 0.22f, 0.45f), nightSkyTint, t);
                skyboxExposureTarget = Mathf.Lerp(0.14f, 0.06f, t);
                atmosphereThickness = Mathf.Lerp(1.05f, 0.85f, t);
                groundColor = Color.Lerp(new Color(0.08f, 0.07f, 0.09f), new Color(0.04f, 0.04f, 0.05f), t);
            }
            // 11. Đêm hẳn / Đêm khuya trăng sáng (20:00 - 04:15): Đêm đen huyền bí đầy sao, không bị đen xì mù mắt
            else
            {
                ambientTarget = nightAmbient;
                intensityTarget = 0.14f * originalAmbientIntensity;
                skyboxTintTarget = nightSkyTint;
                skyboxExposureTarget = 0.06f;
                atmosphereThickness = 0.85f;
                groundColor = new Color(0.04f, 0.04f, 0.05f);
            }

            RenderSettings.ambientLight = ambientTarget;
            RenderSettings.ambientIntensity = intensityTarget;
            RenderSettings.reflectionIntensity = intensityTarget;

            // Áp dụng lên Skybox Material
            if (RenderSettings.skybox != null)
            {
                if (RenderSettings.skybox.HasProperty("_SkyTint"))
                    RenderSettings.skybox.SetColor("_SkyTint", skyboxTintTarget);

                if (RenderSettings.skybox.HasProperty("_Tint"))
                    RenderSettings.skybox.SetColor("_Tint", skyboxTintTarget);

                if (RenderSettings.skybox.HasProperty("_AtmosphereThickness"))
                    RenderSettings.skybox.SetFloat("_AtmosphereThickness", atmosphereThickness);

                if (RenderSettings.skybox.HasProperty("_GroundColor"))
                    RenderSettings.skybox.SetColor("_GroundColor", groundColor);

                if (RenderSettings.skybox.HasProperty("_Exposure"))
                {
                    float weatherMultiplier = 1f;
                    if (Weather.WeatherSystem.Instance != null)
                        weatherMultiplier = Weather.WeatherSystem.Instance.CurrentSkyboxMultiplier;
                    RenderSettings.skybox.SetFloat("_Exposure", skyboxExposureTarget * weatherMultiplier);
                }
            }
        }

        // =====================================================================
        // SƯƠNG MÙ THIÊN NHIÊN (FOG)
        // =====================================================================
        private void UpdateFog()
        {
            if (!originalFogEnabled) return;

            Color fogTarget;

            // 1. Tiền Rạng Đông (04:15 - 05:15)
            if (currentHour >= 4.25f && currentHour < 5.25f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 4.25f) / 1.0f);
                fogTarget = Color.Lerp(new Color(0.02f, 0.03f, 0.05f), new Color(0.12f, 0.14f, 0.22f), t);
            }
            // 2. Bình minh (05:15 - 06:30): Sương sớm nhuốm ánh vàng cam ban mai
            else if (currentHour >= 5.25f && currentHour < 6.5f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 5.25f) / 1.25f);
                fogTarget = Color.Lerp(new Color(0.12f, 0.14f, 0.22f), new Color(0.72f, 0.52f, 0.38f), t);
            }
            // 3. Nắng sớm (06:30 - 08:30): Sương tan dần chuyển sang màu trong trẻo
            else if (currentHour >= 6.5f && currentHour < 8.5f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 6.5f) / 2.0f);
                fogTarget = Color.Lerp(new Color(0.72f, 0.52f, 0.38f), originalFogColor, t);
            }
            // 4. Ban ngày (08:30 - 15:30)
            else if (currentHour >= 8.5f && currentHour < 15.5f)
            {
                fogTarget = originalFogColor;
            }
            // 5. Nắng chiều vàng mật (15:30 - 16:45)
            else if (currentHour >= 15.5f && currentHour < 16.75f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 15.5f) / 1.25f);
                fogTarget = Color.Lerp(originalFogColor, new Color(0.85f, 0.72f, 0.55f), t);
            }
            // 6. Giờ vàng hoàng hôn (16:45 - 17:45): Sương nhuốm ánh vàng cam ấm áp
            else if (currentHour >= 16.75f && currentHour < 17.75f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 16.75f) / 1.0f);
                fogTarget = Color.Lerp(new Color(0.85f, 0.72f, 0.55f), new Color(0.78f, 0.50f, 0.30f), t);
            }
            // 7. Mặt trời chìm qua chân trời (17:45 - 18:30): Sương đỏ hồng hoàng hôn
            else if (currentHour >= 17.75f && currentHour < 18.5f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 17.75f) / 0.75f);
                fogTarget = Color.Lerp(new Color(0.78f, 0.50f, 0.30f), new Color(0.65f, 0.32f, 0.28f), t);
            }
            // 8. Chạng vạng (18:30 - 19:15): Sương tím sẫm huyền ảo
            else if (currentHour >= 18.5f && currentHour < 19.25f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 18.5f) / 0.75f);
                fogTarget = Color.Lerp(new Color(0.65f, 0.32f, 0.28f), new Color(0.24f, 0.16f, 0.28f), t);
            }
            // 9. Màn đêm buông xuống -> Đêm hẳn (19:15 - 20:00)
            else if (currentHour >= 19.25f && currentHour < 20.0f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (currentHour - 19.25f) / 0.75f);
                fogTarget = Color.Lerp(new Color(0.24f, 0.16f, 0.28f), new Color(0.02f, 0.03f, 0.05f), t);
            }
            // 10. Đêm hẳn (20:00 - 04:15)
            else
            {
                fogTarget = new Color(0.02f, 0.03f, 0.05f);
            }

            RenderSettings.fogColor = fogTarget;
        }

        // =====================================================================
        // TIỆN ÍCH
        // =====================================================================

        /// <summary>
        /// Kiểm tra hiện tại có phải ban đêm không (từ 19h15 tối -> 5h15 sáng)
        /// </summary>
        public bool IsNight()
        {
            return currentHour >= 19.25f || currentHour < 5.25f;
        }

        /// <summary>
        /// Lấy chuỗi thời gian dạng "Ngày X - HH:MM"
        /// </summary>
        public string GetTimeString()
        {
            int hours = Mathf.FloorToInt(currentHour);
            int minutes = Mathf.FloorToInt((currentHour - hours) * 60f);
            return string.Format("Ngày {0} - {1:00}:{2:00}", currentDay, hours, minutes);
        }

        // =====================================================================
        // ÂM THANH BAN ĐÊM (TIẾNG SÓI HÚ)
        // =====================================================================
        private void SetupNightAudio()
        {
            if (nightAudioSource == null)
            {
                nightAudioSource = GetComponent<AudioSource>();
                if (nightAudioSource == null)
                {
                    nightAudioSource = gameObject.AddComponent<AudioSource>();
                }
                nightAudioSource.playOnAwake = false;
                nightAudioSource.loop = false;
                nightAudioSource.spatialBlend = 0.15f;
                nightAudioSource.volume = nightWolfVolume;
            }

#if UNITY_EDITOR
            if (nightWolfHowlClip == null)
            {
                nightWolfHowlClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/tiếng sói hú/ribhavagrawal-wolf-howling-type-04-293317.mp3");
            }
#endif
            if (nightWolfHowlClip == null)
            {
                nightWolfHowlClip = Resources.Load<AudioClip>("Sounds/WolfHowl");
            }

            wasNight = IsNight();
            if (wasNight)
            {
                nextWolfHowlTimer = Random.Range(4f, 8f);
            }
        }

        private void UpdateNightAudio()
        {
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive)
            {
                if (nightAudioSource != null && nightAudioSource.isPlaying)
                {
                    nightAudioSource.Stop();
                }
                return;
            }

            bool isCurrentlyNight = IsNight();

            // Chuyển từ Ban Ngày sang Ban Đêm (Màn đêm buông xuống lúc 19:15)
            if (!wasNight && isCurrentlyNight)
            {
                wasNight = true;
                Debug.Log("[DayNight] 🌙 Màn đêm buông xuống! Sói bắt đầu cất tiếng hú...");
                PlayWolfHowl();
                nextWolfHowlTimer = Random.Range(minWolfHowlInterval, maxWolfHowlInterval);

                var stats = FindObjectOfType<HorrorGame.Player.PlayerStats>();
                if (stats != null)
                {
                    stats.ShowStatusNotice("🌙 Màn đêm buông xuống... Tiếng sói hú rợn người vọng lại trong rừng thẳm!");
                }
            }
            // Chuyển từ Ban Đêm sang Ban Ngày (Bình minh ló rạng lúc 05:15)
            else if (wasNight && !isCurrentlyNight)
            {
                wasNight = false;
                Debug.Log("[DayNight] ☀️ Bình minh ló rạng! Dừng toàn bộ âm thanh sói hú ban đêm.");
                if (nightAudioSource != null && nightAudioSource.isPlaying)
                {
                    nightAudioSource.Stop();
                }
            }

            if (isCurrentlyNight)
            {
                nextWolfHowlTimer -= Time.deltaTime;
                if (nextWolfHowlTimer <= 0f)
                {
                    nextWolfHowlTimer = Random.Range(minWolfHowlInterval, maxWolfHowlInterval);
                    PlayWolfHowl();
                }
            }
        }

        public void PlayWolfHowl()
        {
            if (nightWolfHowlClip == null)
            {
#if UNITY_EDITOR
                nightWolfHowlClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/tiếng sói hú/ribhavagrawal-wolf-howling-type-04-293317.mp3");
#endif
                if (nightWolfHowlClip == null) nightWolfHowlClip = Resources.Load<AudioClip>("Sounds/WolfHowl");
            }

            if (nightWolfHowlClip != null && nightAudioSource != null)
            {
                nightAudioSource.volume = 1f;
                nightAudioSource.pitch = Random.Range(0.94f, 1.04f);
                nightAudioSource.PlayOneShot(nightWolfHowlClip, nightWolfVolume);
            }
        }

        // =====================================================================
        // ĐỒNG HỒ THỜI GIAN TRÊN MÀN HÌNH (CLOCK HUD)
        // =====================================================================
        private void OnGUI()
        {
            if (!showClockHUD) return;

            // Ẩn đồng hồ khi đang ở Main Menu
            if (HorrorGame.UI.GameMenuManager.Instance != null && HorrorGame.UI.GameMenuManager.Instance.isMainMenuScene)
                return;

            // Ẩn đồng hồ khi đang xem Cutscene máy bay hoặc Trailer
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;
            if (HorrorGame.Cutscenes.CinematicTrailerDirector.IsTrailerRunning) return;

            // Ẩn đồng hồ nếu Cutscene máy bay chưa hoàn tất (chưa chính thức vào game)
            if (!HorrorGame.Cutscenes.AirplaneCrashCutscene.HasCutsceneFinished)
            {
                var cutscene = FindObjectOfType<HorrorGame.Cutscenes.AirplaneCrashCutscene>();
                if (cutscene != null) return;
            }

            int hours = Mathf.FloorToInt(currentHour);
            int minutes = Mathf.FloorToInt((currentHour - hours) * 60f);

            string periodIcon;
            string periodText;

            if (currentHour >= 4.25f && currentHour < 5.25f)
            {
                periodIcon = "🌌";
                periodText = "Rạng Đông (Đêm Tàn)";
            }
            else if (currentHour >= 5.25f && currentHour < 5.75f)
            {
                periodIcon = "🌅";
                periodText = "Hừng Đông (Ửng Hồng)";
            }
            else if (currentHour >= 5.75f && currentHour < 6.75f)
            {
                periodIcon = "🌅";
                periodText = "Bình Minh Ló Dạng";
            }
            else if (currentHour >= 6.75f && currentHour < 8.5f)
            {
                periodIcon = "☀️";
                periodText = "Nắng Sớm Ban Mai";
            }
            else if (currentHour >= 8.5f && currentHour < 11.5f)
            {
                periodIcon = "☀️";
                periodText = "Sáng Hẳn - Lên Cao";
            }
            else if (currentHour >= 11.5f && currentHour < 15.5f)
            {
                periodIcon = "☀️";
                periodText = "Chính Ngọ (Trưa Sáng)";
            }
            else if (currentHour >= 15.5f && currentHour < 16.75f)
            {
                periodIcon = "🌤️";
                periodText = "Nắng Chiều Vàng Mật";
            }
            else if (currentHour >= 16.75f && currentHour < 17.75f)
            {
                periodIcon = "🌇";
                periodText = "Giờ Vàng Hoàng Hôn";
            }
            else if (currentHour >= 17.75f && currentHour < 18.5f)
            {
                periodIcon = "🌇";
                periodText = "Hoàng Hôn Rực Đỏ (Mặt Trời Lặn)";
            }
            else if (currentHour >= 18.5f && currentHour < 19.25f)
            {
                periodIcon = "🌆";
                periodText = "Chạng Vạng (Hoàng Hôn Tàn)";
            }
            else if (currentHour >= 19.25f && currentHour < 20.0f)
            {
                periodIcon = "🌌";
                periodText = "Màn Đêm Buông Xuống";
            }
            else
            {
                periodIcon = "🌙";
                periodText = "Đêm Hẳn (Trăng Sáng)";
            }

            string extraStatus = "";
            if (isTimePaused) extraStatus = " [TẠM DỪNG]";
            else if (timeMultiplier > 1f) extraStatus = string.Format(" [{0}x]", timeMultiplier);

            string timeText = string.Format("{0} {1:00}:{2:00} • Ngày {3} ({4}){5}", periodIcon, hours, minutes, currentDay, periodText, extraStatus);

            if (clockStyle == null)
            {
                clockStyle = new GUIStyle(GUI.skin.label);
                clockStyle.fontSize = 13;
                clockStyle.fontStyle = FontStyle.Bold;
                clockStyle.alignment = TextAnchor.MiddleCenter;
                clockStyle.normal.textColor = new Color(0.98f, 0.96f, 0.90f);

                clockShadowStyle = new GUIStyle(clockStyle);
                clockShadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.90f);

                tipStyle = new GUIStyle(GUI.skin.label);
                tipStyle.fontSize = 10;
                tipStyle.alignment = TextAnchor.MiddleCenter;
                tipStyle.normal.textColor = new Color(0.75f, 0.85f, 0.95f, 0.70f);

                clockBgTex = new Texture2D(1, 1);
                clockBgTex.SetPixel(0, 0, new Color(0.06f, 0.08f, 0.12f, 0.78f));
                clockBgTex.Apply();
            }

            float w = 350f;
            float h = 26f;
            float x = (Screen.width - w) / 2f;
            float y = 10f;

            if (clockBgTex != null)
            {
                GUI.DrawTexture(new Rect(x, y, w, h), clockBgTex);
            }

            // Viền bo thanh lịch
            GUI.color = new Color(0.45f, 0.60f, 0.75f, 0.45f);
            GUI.Box(new Rect(x, y, w, h), "");
            GUI.color = Color.white;

            GUI.Label(new Rect(x + 1, y + 1, w, h), timeText, clockShadowStyle);
            GUI.Label(new Rect(x, y, w, h), timeText, clockStyle);

            // Gợi ý phím tắt nhỏ ở dưới
            GUI.Label(new Rect(x, y + h + 2f, w, 16f), "[F5] Bình Minh • [F9] Hoàng Hôn • [F10] Đêm Hẳn • [F6] Trưa/Đêm • [F7] Dừng • [F8] Tốc Độ", tipStyle);
        }

        private void OnDestroy()
        {
            RenderSettings.ambientLight = originalAmbientColor;
            RenderSettings.fogColor = originalFogColor;
            if (sunLight != null)
            {
                sunLight.color = originalSunColor;
                sunLight.intensity = originalSunIntensity;
                sunLight.shadows = LightShadows.Soft;
            }
            if (originalSkyboxMaterial != null)
            {
                RenderSettings.skybox = originalSkyboxMaterial;
            }
        }
    }
}


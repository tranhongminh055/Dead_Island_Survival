using UnityEngine;

namespace HorrorGame.Environment
{
    public class DayNightCycle : MonoBehaviour
    {
        public static DayNightCycle Instance;

        [Header("Thời gian")]
        [Tooltip("Một ngày trong game kéo dài bao nhiêu phút ngoài đời thực")]
        public float dayDurationInMinutes = 10f;

        [Tooltip("Giờ bắt đầu khi vào game (0-24). VD: 8 = 8h sáng")]
        [Range(0f, 24f)]
        public float startHour = 8f;

        [Header("Ánh sáng Mặt Trời")]
        [Tooltip("Kéo thả Directional Light (SUN) trong Scene vào đây")]
        public Light sunLight;

        [Header("Mặt Trăng (Tự động tạo)")]
        [Tooltip("Màu ánh trăng")]
        public Color moonColor = new Color(0.5f, 0.6f, 0.8f); // Màu xanh ánh trăng sáng
        [Tooltip("Cường độ sáng mặt trăng (0.05 = rất mờ, 0.2 = sáng rõ)")]
        [Range(0f, 1f)]
        public float moonIntensity = 0.15f;

        // Thời gian hiện tại trong game (0 -> 24)
        [HideInInspector]
        public float currentHour;

        // Ngày thứ mấy trong game
        [HideInInspector]
        public int currentDay = 1;

        // Internal
        private float timeSpeed;
        private Light moonLight;
        
        // Lưu thiết lập gốc
        private float originalSunIntensity;
        private Color originalSunColor;
        private Color originalAmbientColor;
        private float originalAmbientIntensity;
        private float originalReflectionIntensity;
        private float originalSkyboxExposure = 1f;
        private Color originalSkyboxTint = Color.white;
        private bool originalFogEnabled;
        private Color originalFogColor;

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
            currentHour = startHour;
            timeSpeed = 24f / (dayDurationInMinutes * 60f);

            // Lưu thiết lập gốc
            originalAmbientColor = RenderSettings.ambientLight;
            originalAmbientIntensity = RenderSettings.ambientIntensity;
            originalReflectionIntensity = RenderSettings.reflectionIntensity;
            originalFogEnabled = RenderSettings.fog;
            originalFogColor = RenderSettings.fogColor;

            if (RenderSettings.skybox != null)
            {
                if (RenderSettings.skybox.HasProperty("_Exposure"))
                    originalSkyboxExposure = RenderSettings.skybox.GetFloat("_Exposure");
                
                if (RenderSettings.skybox.HasProperty("_Tint"))
                    originalSkyboxTint = RenderSettings.skybox.GetColor("_Tint");
            }

            // Tìm Directional Light (SUN) tự động nếu chưa gắn
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
                originalSunColor = sunLight.color;
            }

            // === TẠO MẶT TRĂNG TỰ ĐỘNG ===
            CreateMoonLight();
        }

        private void CreateMoonLight()
        {
            GameObject moonObj = new GameObject("MoonLight_Auto");
            moonObj.transform.SetParent(this.transform);
            moonLight = moonObj.AddComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.color = moonColor;
            moonLight.intensity = 0f; // Bắt đầu tắt, sẽ bật khi trời tối
            moonLight.shadows = LightShadows.Soft;
            moonLight.shadowStrength = 0.6f;
            // Xoay mặt trăng hướng xuống chéo (giả lập ánh trăng từ trên cao)
            moonLight.transform.rotation = Quaternion.Euler(45f, 150f, 0f);
            Debug.Log("[DayNight] Đã tạo MoonLight tự động.");
        }

        private void Update()
        {
            currentHour += timeSpeed * Time.deltaTime;

            if (currentHour >= 24f)
            {
                currentHour -= 24f;
                currentDay++;
                Debug.Log("[DayNight] === NGÀY MỚI: Ngày " + currentDay + " ===");
            }

            UpdateSun();
            UpdateMoon();
            UpdateAmbient();
            UpdateFog();
        }

        // =====================================================
        // MẶT TRỜI
        // =====================================================
        private void UpdateSun()
        {
            if (sunLight == null) return;

            // --- XOAY MẶT TRỜI ---
            // 6h = mọc (0 độ), 12h = đỉnh (90 độ), 18h = lặn (180 độ)
            float sunAngle = (currentHour - 6f) / 24f * 360f;
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);

            // --- CƯỜNG ĐỘ SÁNG ---
            float intensity = 0f;

            if (currentHour >= 5f && currentHour < 6f)
            {
                // Bình minh: từ 0 -> sáng dần (5h-6h)
                float t = (currentHour - 5f) / 1f;
                intensity = Mathf.Lerp(0f, originalSunIntensity, t);
                // Màu bình minh: cam ấm
                sunLight.color = Color.Lerp(new Color(1f, 0.5f, 0.2f), originalSunColor, t);
            }
            else if (currentHour >= 6f && currentHour < 17f)
            {
                // Ban ngày (6h - 17h): sáng bình thường
                intensity = originalSunIntensity;
                sunLight.color = originalSunColor;
            }
            else if (currentHour >= 17f && currentHour < 18f)
            {
                // Hoàng hôn: tối dần (17h-18h)
                float t = (currentHour - 17f) / 1f;
                intensity = Mathf.Lerp(originalSunIntensity, 0f, t);
                // Màu hoàng hôn: đỏ cam
                sunLight.color = Color.Lerp(originalSunColor, new Color(1f, 0.3f, 0.1f), t);
            }
            else
            {
                // Ban đêm (18h - 5h): TẮT HẲN mặt trời
                intensity = 0f;
            }

            sunLight.intensity = intensity;

            // Tắt shadow khi mặt trời không sáng (tránh lỗi shadow)
            if (intensity <= 0.01f)
            {
                sunLight.shadows = LightShadows.None;
            }
            else
            {
                sunLight.shadows = LightShadows.Soft;
            }
        }

        // =====================================================
        // MẶT TRĂNG
        // =====================================================
        private void UpdateMoon()
        {
            if (moonLight == null) return;

            // Đảm bảo cường độ mặt trăng ít nhất là 0.5 (rất sáng) để không bị tối do lưu cache cũ
            float safeMoonIntensity = Mathf.Max(moonIntensity, 0.5f);

            float intensity = 0f;

            if (currentHour >= 18f && currentHour < 19f)
            {
                // Mặt trăng mọc dần từ khi trời tối (18h-19h)
                float t = (currentHour - 18f) / 1f;
                intensity = Mathf.Lerp(0f, safeMoonIntensity, t);
            }
            else if (currentHour >= 19f || currentHour < 5f)
            {
                // Đêm khuya (19h - 5h): mặt trăng đã lên và sáng nhất
                intensity = safeMoonIntensity;
            }
            else if (currentHour >= 5f && currentHour < 6f)
            {
                // Mặt trăng lặn dần lúc bình minh (5h-6h)
                float t = (currentHour - 5f) / 1f;
                intensity = Mathf.Lerp(safeMoonIntensity, 0f, t);
            }
            else
            {
                // Ban ngày: tắt trăng
                intensity = 0f;
            }

            moonLight.intensity = intensity;
            moonLight.color = moonColor;

            // Tính toán góc xoay của mặt trăng
            float mHour = currentHour;
            if (mHour >= 18f) mHour -= 18f; // Từ 18h -> 24h quy về 0 -> 6
            else mHour += 6f;               // Từ 0h -> 6h quy về 6 -> 12

            // mHour chạy từ 0 đến 12. 
            // Cho góc chạy từ 30 -> 150 để trăng mọc cao hơn, tránh bị cây cối che khuất ở mốc 19h
            float moonAngle = Mathf.Lerp(30f, 150f, mHour / 12f);
            moonLight.transform.rotation = Quaternion.Euler(moonAngle, 150f, 0f);

            // Bật/tắt shadow cho trăng
            if (intensity > 0.01f)
            {
                moonLight.shadows = LightShadows.Soft;
            }
            else
            {
                moonLight.shadows = LightShadows.None;
            }
        }

        // =====================================================
        // ÁNH SÁNG MÔI TRƯỜNG (AMBIENT & SKYBOX)
        // =====================================================
        private void UpdateAmbient()
        {
            Color ambientTarget;
            float intensityTarget;
            Color skyboxTintTarget = originalSkyboxTint;
            float skyboxExposureTarget = originalSkyboxExposure;

            if (currentHour >= 5f && currentHour < 6f)
            {
                // Bình minh (5h đến 6h)
                float t = (currentHour - 5f) / 1f;
                ambientTarget = Color.Lerp(new Color(0.05f, 0.05f, 0.1f), originalAmbientColor, t);
                intensityTarget = Mathf.Lerp(0.15f * originalAmbientIntensity, originalAmbientIntensity, t);
                skyboxTintTarget = Color.Lerp(new Color(0.1f, 0.1f, 0.1f), originalSkyboxTint, t);
                skyboxExposureTarget = Mathf.Lerp(0.05f, originalSkyboxExposure, t);
            }
            else if (currentHour >= 6f && currentHour < 17f)
            {
                // Ban ngày (6h - 17h)
                ambientTarget = originalAmbientColor;
                intensityTarget = originalAmbientIntensity;
            }
            else if (currentHour >= 17f && currentHour < 18f)
            {
                // Hoàng hôn (17h - 18h)
                float t = (currentHour - 17f) / 1f;
                ambientTarget = Color.Lerp(originalAmbientColor, new Color(0.05f, 0.05f, 0.1f), t);
                intensityTarget = Mathf.Lerp(originalAmbientIntensity, 0.15f * originalAmbientIntensity, t);
                skyboxTintTarget = Color.Lerp(originalSkyboxTint, new Color(0.1f, 0.1f, 0.1f), t);
                skyboxExposureTarget = Mathf.Lerp(originalSkyboxExposure, 0.05f, t);
            }
            else
            {
                // BAN ĐÊM (Từ 18h tối đến 5h sáng): Tối đen nhưng không về 0 tuyệt đối để tránh hỏng shadow
                ambientTarget = new Color(0.05f, 0.05f, 0.1f);
                intensityTarget = 0.15f * originalAmbientIntensity; // Giữ lại 15% ánh sáng môi trường gốc
                skyboxTintTarget = new Color(0.1f, 0.1f, 0.1f);
                skyboxExposureTarget = 0.05f; // Mờ nhưng không tắt hẳn
            }

            // Ghi đè Ambient
            RenderSettings.ambientLight = ambientTarget;
            RenderSettings.ambientIntensity = intensityTarget;
            RenderSettings.reflectionIntensity = intensityTarget;

            // Làm đen bầu trời (Skybox)
            if (RenderSettings.skybox != null)
            {
                if (RenderSettings.skybox.HasProperty("_Tint"))
                    RenderSettings.skybox.SetColor("_Tint", skyboxTintTarget);
                
                if (RenderSettings.skybox.HasProperty("_Exposure"))
                    RenderSettings.skybox.SetFloat("_Exposure", skyboxExposureTarget);
            }
        }

        // =====================================================
        // SƯƠNG MÙ (FOG)
        // =====================================================
        private void UpdateFog()
        {
            if (!originalFogEnabled) return;

            Color fogTarget;

            if (currentHour >= 5f && currentHour < 6f)
            {
                // Bình minh
                float t = (currentHour - 5f) / 1f;
                fogTarget = Color.Lerp(Color.black, originalFogColor, t);
            }
            else if (currentHour >= 6f && currentHour < 17f)
            {
                // Ban ngày
                fogTarget = originalFogColor;
            }
            else if (currentHour >= 17f && currentHour < 18f)
            {
                // Hoàng hôn
                float t = (currentHour - 17f) / 1f;
                fogTarget = Color.Lerp(originalFogColor, Color.black, t);
            }
            else
            {
                // Ban đêm
                fogTarget = Color.black;
            }

            RenderSettings.fogColor = fogTarget;
        }

        // =====================================================
        // TIỆN ÍCH
        // =====================================================

        /// <summary>
        /// Kiểm tra hiện tại có phải ban đêm không (từ 18h -> 6h)
        /// </summary>
        public bool IsNight()
        {
            return currentHour >= 18f || currentHour < 6f;
        }

        /// <summary>
        /// Lấy chuỗi thời gian dạng "Ngày X - HH:MM"
        /// </summary>
        public string GetTimeString()
        {
            int hours = Mathf.FloorToInt(currentHour);
            int minutes = Mathf.FloorToInt((currentHour - hours) * 60f);
            return string.Format("Ngay {0} - {1:00}:{2:00}", currentDay, hours, minutes);
        }

        private void OnDestroy()
        {
            // Khôi phục lại thiết lập gốc khi thoát Play mode
            RenderSettings.ambientLight = originalAmbientColor;
            RenderSettings.fogColor = originalFogColor;
            if (sunLight != null)
            {
                sunLight.color = originalSunColor;
                sunLight.intensity = originalSunIntensity;
                sunLight.shadows = LightShadows.Soft;
            }
        }
    }
}

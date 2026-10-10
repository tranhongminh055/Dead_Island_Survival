using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace HorrorGame.Environment.Weather
{
    /// <summary>
    /// Hệ Thống Thời Tiết Trung Tâm (Weather System) cho game sinh tồn:
    /// - Quản lý chuyển đổi mượt mà giữa các trạng thái thời tiết (Nắng, Mây, Mưa nhỏ, Mưa to, Bão sấm sét)
    /// - Tự động điều tiết ánh sáng, sương mù, bầu trời kết hợp nhịp nhàng với DayNightCycle
    /// - Phát âm thanh mưa rơi, gió bão lập thể sống động
    /// - Phát hiện nơi trú ẩn (Shelter/Mái che) để giảm tiếng ồn và tránh ướt
    /// - Tự động kích hoạt sấm chớp theo chu kỳ vật lý
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        public static WeatherSystem Instance;

        [Header("── Trạng Thái Thời Tiết ──")]
        public WeatherType currentWeather = WeatherType.Clear;
        public WeatherType targetWeather = WeatherType.Clear;
        [Tooltip("Thời gian chuyển đổi mượt giữa 2 thời tiết (giây)")]
        public float transitionDuration = 12f;

        [Header("── Bão Mở Đầu Sau Trailer (Intro Storm) ──")]
        [Tooltip("Tự động kích hoạt trận bão sấm sét ngay sau khi trailer kết thúc")]
        public bool startStormAfterTrailer = true;
        [Tooltip("Thời lượng trận bão sấm sét mở đầu (giây). Mặc định 180s = 3 phút")]
        public float introStormDuration = 180f;
        [Tooltip("Sét đánh ngay giây đầu tiên sau khi trailer kết thúc")]
        public bool firstLightningStrikeImmediate = true;

        [Header("── Chu Kỳ Tự Động (Weather Cycle) ──")]
        [Tooltip("Tự động chuyển đổi thời tiết theo thời gian")]
        public bool autoChangeWeather = true;
        [Tooltip("Thời gian còn lại đến lần đổi thời tiết tiếp theo (giây)")]
        public float weatherTimer = 300f;

        [Header("── Các Module Thành Phần ──")]
        public RainEffect rainEffect;
        public LightningSystem lightningSystem;

        [Header("── Nguồn Âm Thanh (Audio Loops) ──")]
        public AudioSource rainLightAudio;
        public AudioSource rainHeavyAudio;
        public AudioSource windAudio;

        [Header("── Nhận Diện Nơi Trú Ẩn (Shelter Detection) ──")]
        public float shelterCheckDistance = 30f;
        public LayerMask shelterLayerMask = ~0; // Mặc định kiểm tra mọi vật cản phía trên

        // Preset data
        private Dictionary<WeatherType, WeatherData> weatherPresets = new Dictionary<WeatherType, WeatherData>();

        // Interpolated atmospheric values
        public float CurrentRainIntensity { get; private set; }
        public float CurrentWindIntensity { get; private set; }
        public float CurrentSunMultiplier { get; private set; }
        public float CurrentAmbientMultiplier { get; private set; }
        public float CurrentSkyboxMultiplier { get; private set; }
        public float CurrentFogDensityMultiplier { get; private set; }
        public Color CurrentFogColor { get; private set; }

        public bool IsRaining
        {
            get { return CurrentRainIntensity > 0.05f; }
        }

        public bool IsThunderstorm
        {
            get
            {
                return currentWeather == WeatherType.Thunderstorm || (targetWeather == WeatherType.Thunderstorm && CurrentRainIntensity > 0.7f);
            }
        }

        public bool IsPlayerSheltered { get; private set; }

        // Intro storm & Cutscene tracking
        public bool HasTriggeredIntroStorm
        {
            get { return hasTriggeredIntroStorm; }
        }
        private bool hasTriggeredIntroStorm = false;
        private bool wasCutsceneActiveLastFrame = false;

        // Internal transitions
        private float transitionProgress = 1f;
        private WeatherData fromData;
        private WeatherData toData;
        private float lightningTimer = 0f;
        private Transform playerTransform;

        // Lưu thông số gốc
        private float originalFogDensity;
        private Color originalFogColor;
        private float originalAmbientIntensity = 1f;
        private float originalSkyboxExposure = 1f;
        private HorrorGame.Cutscenes.AirplaneCrashCutscene cachedAirplaneCutscene;

        // Sự kiện thời tiết
        public System.Action<WeatherType> OnWeatherChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            CurrentSunMultiplier = 1f;
            CurrentAmbientMultiplier = 1f;
            CurrentSkyboxMultiplier = 1f;
            CurrentFogDensityMultiplier = 1f;
            CurrentFogColor = Color.gray;
            IsPlayerSheltered = false;

            InitializePresets();
            EnsureComponents();
        }

        private void Start()
        {
            currentWeather = WeatherType.Clear;
            targetWeather = WeatherType.Clear;
            fromData = weatherPresets[WeatherType.Clear];
            toData = fromData;
            ApplyWeatherImmediate(WeatherType.Clear);

            originalFogDensity = RenderSettings.fogDensity > 0 ? RenderSettings.fogDensity : 0.015f;
            originalFogColor = RenderSettings.fogColor;
            originalAmbientIntensity = RenderSettings.ambientIntensity;

            if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Exposure"))
            {
                originalSkyboxExposure = RenderSettings.skybox.GetFloat("_Exposure");
            }
            else
            {
                originalSkyboxExposure = 1f;
            }

            cachedAirplaneCutscene = FindObjectOfType<HorrorGame.Cutscenes.AirplaneCrashCutscene>();

            FindPlayer();

            // Luôn đảm bảo tắt hoàn toàn âm thanh mưa gió và hạt mưa khi vừa vào game/menu
            MuteAndStopAllWeather();
        }

        private void InitializePresets()
        {
            weatherPresets[WeatherType.Clear] = WeatherData.GetDefaultPreset(WeatherType.Clear);
            weatherPresets[WeatherType.Cloudy] = WeatherData.GetDefaultPreset(WeatherType.Cloudy);
            weatherPresets[WeatherType.LightRain] = WeatherData.GetDefaultPreset(WeatherType.LightRain);
            weatherPresets[WeatherType.HeavyRain] = WeatherData.GetDefaultPreset(WeatherType.HeavyRain);
            weatherPresets[WeatherType.Thunderstorm] = WeatherData.GetDefaultPreset(WeatherType.Thunderstorm);
        }

        private void FindPlayer()
        {
            if (playerTransform != null) return;
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else if (Camera.main != null) playerTransform = Camera.main.transform;
        }

        private void Update()
        {
            FindPlayer();

            // 0. Kiểm tra trạng thái Cutscene (Trailer)
            if (CheckCutsceneHandling())
            {
                // Đang trong trailer: tạm ngưng các tác vụ thời tiết ngoài trời
                return;
            }

            // 1. Kiểm tra mái che (Shelter)
            CheckShelter();

            // 2. Tiến trình chuyển giao thời tiết
            if (transitionProgress < 1f)
            {
                transitionProgress += Time.deltaTime / Mathf.Max(0.1f, transitionDuration);
                transitionProgress = Mathf.Clamp01(transitionProgress);

                BlendAtmosphere(fromData, toData, transitionProgress);

                if (transitionProgress >= 1f)
                {
                    currentWeather = targetWeather;
                    if (OnWeatherChanged != null)
                    {
                        OnWeatherChanged(currentWeather);
                    }
                }
            }

            // 3. Chu kỳ tự động đổi thời tiết
            if (autoChangeWeather)
            {
                weatherTimer -= Time.deltaTime;
                if (weatherTimer <= 0f)
                {
                    PickNextRandomWeather();
                }
            }

            // 4. Kích hoạt sấm sét định kỳ khi có giông bão
            HandleLightningCycle();

            // 5. Cập nhật âm thanh & hạt mưa
            UpdateAudio();

            if (rainEffect != null)
            {
                rainEffect.UpdateRain(CurrentRainIntensity, CurrentWindIntensity, IsPlayerSheltered);
            }
        }

        // =========================================================================
        // KIỂM TRA TRẠNG THÁI CUTSCENE (TRAILER) & MAIN MENU
        // =========================================================================
        public bool IsMenuOrCutsceneRunning()
        {
            // 1. Kiểm tra nếu đang ở Menu chính
            if (HorrorGame.UI.GameMenuManager.Instance != null && HorrorGame.UI.GameMenuManager.Instance.isMainMenuScene)
            {
                return true;
            }

            // 2. Kiểm tra nếu Trailer máy bay đang chạy
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive)
            {
                return true;
            }

            // 3. Nếu trong Scene có script Cutscene máy bay nhưng chưa hoàn tất
            if (!HorrorGame.Cutscenes.AirplaneCrashCutscene.HasCutsceneFinished)
            {
                if (cachedAirplaneCutscene == null)
                {
                    cachedAirplaneCutscene = FindObjectOfType<HorrorGame.Cutscenes.AirplaneCrashCutscene>();
                }
                if (cachedAirplaneCutscene != null)
                {
                    return true;
                }
            }

            return false;
        }

        private bool CheckCutsceneHandling()
        {
            if (IsMenuOrCutsceneRunning())
            {
                wasCutsceneActiveLastFrame = true;

                // Tắt hoàn toàn mọi hiệu ứng mưa và âm thanh trong menu & trailer
                MuteAndStopAllWeather();

                return true;
            }

            // Nếu frame trước đang trong trailer hoặc menu, frame này vừa chính thức vào game sau trailer!
            if (wasCutsceneActiveLastFrame)
            {
                wasCutsceneActiveLastFrame = false;
                if (startStormAfterTrailer && !hasTriggeredIntroStorm)
                {
                    TriggerPostTrailerStorm();
                }
            }

            return false;
        }

        public void MuteAndStopAllWeather()
        {
            CurrentRainIntensity = 0f;
            CurrentWindIntensity = 0f;

            if (rainEffect != null)
            {
                rainEffect.StopAllRainImmediate();
            }

            if (rainLightAudio != null)
            {
                rainLightAudio.volume = 0f;
                if (rainLightAudio.isPlaying) rainLightAudio.Stop();
            }
            if (rainHeavyAudio != null)
            {
                rainHeavyAudio.volume = 0f;
                if (rainHeavyAudio.isPlaying) rainHeavyAudio.Stop();
            }
            if (windAudio != null)
            {
                windAudio.volume = 0f;
                if (windAudio.isPlaying) windAudio.Stop();
            }
        }

        private void LateUpdate()
        {
            // Không can thiệp ánh sáng môi trường khi đang ở menu hoặc xem trailer
            if (IsMenuOrCutsceneRunning()) return;

            // Tác động khí quyển sau khi DayNightCycle chạy
            ApplyAtmosphereToScene();
        }

        // =========================================================================
        // ĐIỀU TIẾT KHÍ QUYỂN & ÁNH SÁNG
        // =========================================================================
        private void BlendAtmosphere(WeatherData a, WeatherData b, float t)
        {
            // Smoothstep cong chuyển tiếp mềm mại
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            CurrentRainIntensity = Mathf.Lerp(a.rainIntensity, b.rainIntensity, smoothT);
            CurrentWindIntensity = Mathf.Lerp(a.windIntensity, b.windIntensity, smoothT);
            CurrentSunMultiplier = Mathf.Lerp(a.sunLightMultiplier, b.sunLightMultiplier, smoothT);
            CurrentAmbientMultiplier = Mathf.Lerp(a.ambientMultiplier, b.ambientMultiplier, smoothT);
            CurrentSkyboxMultiplier = Mathf.Lerp(a.skyboxExposureMultiplier, b.skyboxExposureMultiplier, smoothT);
            CurrentFogDensityMultiplier = Mathf.Lerp(a.fogDensityMultiplier, b.fogDensityMultiplier, smoothT);
            CurrentFogColor = Color.Lerp(a.fogColor, b.fogColor, smoothT);
        }

        private void ApplyAtmosphereToScene()
        {
            // Giảm độ sáng của Mặt trời / Mặt trăng theo độ che phủ mây mưa
            if (DayNightCycle.Instance != null)
            {
                if (DayNightCycle.Instance.sunLight != null)
                {
                    DayNightCycle.Instance.sunLight.intensity *= CurrentSunMultiplier;
                }
                if (DayNightCycle.Instance.MoonLight != null)
                {
                    DayNightCycle.Instance.MoonLight.intensity *= CurrentSunMultiplier;
                }
            }

            // Tăng sương mù dày đặc khi mưa gió
            if (RenderSettings.fog)
            {
                RenderSettings.fogDensity = originalFogDensity * CurrentFogDensityMultiplier;
                RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, CurrentFogColor, CurrentRainIntensity * 0.75f);
            }

            // Làm tối màu môi trường xung quanh (dựa trên cường độ gốc)
            RenderSettings.ambientIntensity = originalAmbientIntensity * CurrentAmbientMultiplier;

            // Giảm độ phơi sáng của Skybox khi trời bão (dựa trên độ phơi sáng gốc)
            if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Exposure"))
            {
                RenderSettings.skybox.SetFloat("_Exposure", originalSkyboxExposure * CurrentSkyboxMultiplier);
            }
        }

        // =========================================================================
        // PHÁT HIỆN NƠI TRÚ ẨN (SHELTER)
        // =========================================================================
        private void CheckShelter()
        {
            if (playerTransform == null)
            {
                IsPlayerSheltered = false;
                return;
            }

            Vector3 origin = playerTransform.position + Vector3.up * 1.6f;
            RaycastHit hit;
            // Bắn tia thẳng đứng lên trời để xem có mái nhà / trần máy bay không
            if (Physics.Raycast(origin, Vector3.up, out hit, shelterCheckDistance, shelterLayerMask, QueryTriggerInteraction.Ignore))
            {
                IsPlayerSheltered = true;
            }
            else
            {
                IsPlayerSheltered = false;
            }
        }

        // =========================================================================
        // CHU KỲ SẤM SÉT (LIGHTNING CYCLE)
        // =========================================================================
        private void HandleLightningCycle()
        {
            if (lightningSystem == null) return;

            float currentFreq = Mathf.Lerp(fromData.lightningFrequency, toData.lightningFrequency, transitionProgress);
            if (currentFreq <= 0f) return;

            lightningTimer -= Time.deltaTime;
            if (lightningTimer <= 0f)
            {
                lightningSystem.StrikeRandom();

                // Tính thời gian chờ tới tia sét kế tiếp (ngẫu nhiên quanh tần suất)
                float avgInterval = 60f / currentFreq;
                lightningTimer = Random.Range(avgInterval * 0.5f, avgInterval * 1.5f);
            }
        }

        // =========================================================================
        // ÂM THANH MƯA & GIÓ
        // =========================================================================
        private void UpdateAudio()
        {
            // Âm lượng giảm khi đứng dưới mái che
            float shelterDamp = IsPlayerSheltered ? 0.35f : 1.0f;

            // Mưa nhỏ: đạt max ở rainIntensity ~ 0.4
            if (rainLightAudio != null)
            {
                float targetVol = Mathf.Clamp01(CurrentRainIntensity * 1.6f) * shelterDamp;
                rainLightAudio.volume = Mathf.MoveTowards(rainLightAudio.volume, targetVol, Time.deltaTime * 0.5f);
                if (rainLightAudio.clip != null)
                {
                    if (rainLightAudio.volume > 0.01f && !rainLightAudio.isPlaying) rainLightAudio.Play();
                    else if (rainLightAudio.volume <= 0.005f && rainLightAudio.isPlaying) rainLightAudio.Stop();
                }
            }

            // Mưa to: bùng nổ khi rainIntensity > 0.4
            if (rainHeavyAudio != null)
            {
                float targetVol = Mathf.Clamp01((CurrentRainIntensity - 0.35f) * 1.5f) * shelterDamp;
                rainHeavyAudio.volume = Mathf.MoveTowards(rainHeavyAudio.volume, targetVol, Time.deltaTime * 0.5f);
                if (rainHeavyAudio.clip != null)
                {
                    if (rainHeavyAudio.volume > 0.01f && !rainHeavyAudio.isPlaying) rainHeavyAudio.Play();
                    else if (rainHeavyAudio.volume <= 0.005f && rainHeavyAudio.isPlaying) rainHeavyAudio.Stop();
                }
            }

            // Tiếng gió rít
            if (windAudio != null)
            {
                float targetVol = Mathf.Clamp01(CurrentWindIntensity * 0.8f) * (IsPlayerSheltered ? 0.6f : 1.0f);
                windAudio.volume = Mathf.MoveTowards(windAudio.volume, targetVol, Time.deltaTime * 0.5f);
                if (windAudio.clip != null)
                {
                    if (windAudio.volume > 0.01f && !windAudio.isPlaying) windAudio.Play();
                    else if (windAudio.volume <= 0.005f && windAudio.isPlaying) windAudio.Stop();
                }
            }
        }

        // =========================================================================
        // BÃO MỞ ĐẦU SAU TRAILER & ĐIỀU KHIỂN THỜI TIẾT
        // =========================================================================
        /// <summary>
        /// Kích hoạt trận bão sấm sét ngay sau khi trailer kết thúc
        /// </summary>
        public void TriggerPostTrailerStorm(float customDuration = -1f, bool force = false)
        {
            if (hasTriggeredIntroStorm && !force) return;

            // Tuyệt đối không kích hoạt bão sớm khi người chơi vẫn đang ở Menu hoặc đang xem Trailer!
            if (!force && IsMenuOrCutsceneRunning())
            {
                return;
            }

            hasTriggeredIntroStorm = true;

            float stormDur = (customDuration > 0f) ? customDuration : introStormDuration;

            Debug.Log(string.Format("⛈️⚡ [WeatherSystem] BẮT ĐẦU TRẬN MƯA BÃO SẤM CHỚP SAU TRAILER! (Thời lượng: {0:F0}s)", stormDur));

            // Chuyển sang thời tiết Bão sấm sét lập tức để người chơi mở mắt ra là thấy bão ngay
            targetWeather = WeatherType.Thunderstorm;
            ApplyWeatherImmediate(WeatherType.Thunderstorm);

            weatherTimer = stormDur;
            autoChangeWeather = true;

            // Đánh sét ngay tức khắc tạo ấn tượng mạnh mẽ cho phân cảnh mở đầu
            if (firstLightningStrikeImmediate && lightningSystem != null)
            {
                StartCoroutine(DelayedFirstStrike(0.8f));
            }
        }

        private IEnumerator DelayedFirstStrike(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (lightningSystem != null)
            {
                lightningSystem.StrikeRandom();
            }
        }

        public void SetWeather(WeatherType newWeather, float customTransitionTime = -1f)
        {
            if (newWeather == targetWeather && transitionProgress >= 1f) return;

            targetWeather = newWeather;
            fromData = new WeatherData(
                currentWeather, "Current",
                CurrentRainIntensity, CurrentWindIntensity,
                CurrentSunMultiplier, CurrentAmbientMultiplier,
                CurrentSkyboxMultiplier, CurrentFogColor,
                CurrentFogDensityMultiplier,
                (targetWeather == WeatherType.Thunderstorm ? 6f : (targetWeather == WeatherType.HeavyRain ? 1.5f : 0f)), 3f, 6f
            );
            toData = weatherPresets[newWeather];

            transitionDuration = (customTransitionTime > 0f) ? customTransitionTime : 15f;
            transitionProgress = 0f;

            ScheduleNextWeatherDuration();
            Debug.Log(string.Format("🌦️ [WeatherSystem] Chuyển thời tiết sang: {0} ({1:F0}s)", toData.displayName, transitionDuration));
        }

        public void ApplyWeatherImmediate(WeatherType newWeather)
        {
            currentWeather = newWeather;
            targetWeather = newWeather;
            fromData = weatherPresets[newWeather];
            toData = fromData;
            transitionProgress = 1f;
            BlendAtmosphere(fromData, toData, 1f);
            ScheduleNextWeatherDuration();
        }

        private void PickNextRandomWeather()
        {
            WeatherType next = GetNextWeatherInCycle(currentWeather);
            float transitionTime = Random.Range(14f, 22f); // Chuyển đổi mềm mại tự nhiên
            SetWeather(next, transitionTime);
        }

        /// <summary>
        /// Ma trận chuyển đổi thời tiết thực tế: thời tiết biến đổi tuần tự nhịp nhàng
        /// </summary>
        private WeatherType GetNextWeatherInCycle(WeatherType current)
        {
            float roll = Random.value;
            switch (current)
            {
                case WeatherType.Thunderstorm:
                    // Sau cơn bão dữ dội:
                    // 60% giảm xuống Mưa nhỏ (LightRain)
                    // 25% tạnh mưa sang Nhiều mây (Cloudy)
                    // 15% tiếp tục Mưa rào nặng hạt (HeavyRain)
                    if (roll < 0.60f) return WeatherType.LightRain;
                    if (roll < 0.85f) return WeatherType.Cloudy;
                    return WeatherType.HeavyRain;

                case WeatherType.HeavyRain:
                    // Đang mưa to:
                    // 40% giảm xuống Mưa nhỏ (LightRain)
                    // 35% bùng phát thành Giông bão sấm sét (Thunderstorm)
                    // 25% tạnh mưa, trời nhiều mây (Cloudy)
                    if (roll < 0.40f) return WeatherType.LightRain;
                    if (roll < 0.75f) return WeatherType.Thunderstorm;
                    return WeatherType.Cloudy;

                case WeatherType.LightRain:
                    // Đang mưa nhỏ:
                    // 45% tạnh mưa thành Nhiều mây (Cloudy)
                    // 35% mạnh lên thành Mưa to (HeavyRain)
                    // 20% mây tan thành Nắng ráo (Clear)
                    if (roll < 0.45f) return WeatherType.Cloudy;
                    if (roll < 0.80f) return WeatherType.HeavyRain;
                    return WeatherType.Clear;

                case WeatherType.Cloudy:
                    // Đang nhiều mây:
                    // 45% chuyển thành Mưa nhỏ (LightRain)
                    // 40% trời quang mây tạnh thành Nắng ráo (Clear)
                    // 15% mây đen kéo tới Mưa to (HeavyRain)
                    if (roll < 0.45f) return WeatherType.LightRain;
                    if (roll < 0.85f) return WeatherType.Clear;
                    return WeatherType.HeavyRain;

                case WeatherType.Clear:
                default:
                    // Đang nắng ráo:
                    // 65% mây kéo tới thành Nhiều mây (Cloudy)
                    // 25% tiếp tục duy trì Nắng ráo (Clear)
                    // 10% mưa phùn rải rác (LightRain)
                    if (roll < 0.65f) return WeatherType.Cloudy;
                    if (roll < 0.90f) return WeatherType.Clear;
                    return WeatherType.LightRain;
            }
        }

        private void ScheduleNextWeatherDuration()
        {
            WeatherData currentPreset = weatherPresets.ContainsKey(targetWeather) ? weatherPresets[targetWeather] : weatherPresets[WeatherType.Clear];
            weatherTimer = Random.Range(currentPreset.minDurationMinutes * 60f, currentPreset.maxDurationMinutes * 60f);
        }

        // =========================================================================
        // KHỞI TẠO THÀNH PHẦN TỰ ĐỘNG
        // =========================================================================
        private void EnsureComponents()
        {
            if (rainEffect == null)
            {
                rainEffect = GetComponentInChildren<RainEffect>();
                if (rainEffect == null)
                {
                    GameObject rainObj = new GameObject("RainEffect_Module");
                    rainObj.transform.SetParent(this.transform);
                    rainEffect = rainObj.AddComponent<RainEffect>();
                }
            }

            if (lightningSystem == null)
            {
                lightningSystem = GetComponentInChildren<LightningSystem>();
                if (lightningSystem == null)
                {
                    GameObject lightnObj = new GameObject("LightningSystem_Module");
                    lightnObj.transform.SetParent(this.transform);
                    lightningSystem = lightnObj.AddComponent<LightningSystem>();
                }
            }

            // Audio Sources
            EnsureAudioSources();
        }

        private void EnsureAudioSources()
        {
            string soundPath = "Assets/Flooded_Grounds/Content/Sounds/Weather/";

            if (rainLightAudio == null)
            {
                rainLightAudio = CreateLoopAudio("Rain_Light_Loop");
#if UNITY_EDITOR
                rainLightAudio.clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(soundPath + "Rain_Light.wav");
#endif
            }

            if (rainHeavyAudio == null)
            {
                rainHeavyAudio = CreateLoopAudio("Rain_Heavy_Loop");
#if UNITY_EDITOR
                rainHeavyAudio.clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(soundPath + "Rain_Heavy.wav");
#endif
            }

            if (windAudio == null)
            {
                windAudio = CreateLoopAudio("Wind_Storm_Loop");
#if UNITY_EDITOR
                windAudio.clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(soundPath + "Wind_Storm.wav");
#endif
            }
        }

        private AudioSource CreateLoopAudio(string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(this.transform);
            AudioSource src = obj.AddComponent<AudioSource>();
            src.loop = true;
            src.playOnAwake = false; // Tuyệt đối không tự phát khi chưa có mưa
            src.volume = 0f;
            src.spatialBlend = 0f; // 2D âm thanh bao quanh
            return src;
        }
    }
}

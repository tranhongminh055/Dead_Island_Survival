using UnityEngine;
using System.Collections;

namespace HorrorGame.Effects
{
    /// <summary>
    /// 🌊 HIỆU ỨNG LẶN NƯỚC CHÂN THỰC (REALISTIC UNDERWATER VISUAL & AUDIO EFFECT)
    /// Khi con người nhảy xuống biển / đầm lầy:
    /// 1. MỜ MẮT MẤT TIÊU CỰ: Khúc xạ quang học làm mờ mịt khung cảnh như mắt người không đeo kính lặn.
    /// 2. SƯƠNG MÙ BIỂN SÂU: Ánh sáng đỏ bị hấp thụ, không gian ngập tràn sắc lam ngọc bích mờ ảo (8-10m).
    /// 3. ÂM THANH NGHẸT ĐỤC: AudioLowPassFilter cắt tần số xuống 650Hz, tạo tiếng ầm ù trầm đục chuẩn đời thực.
    /// 4. BỌT KHÍ NỔI LÊN: Bong bóng khí sủi bọt procedural nổi ngược lên mặt nước.
    /// 5. BẮN NƯỚC KHI TRỒI LÊN: Tiếng vỗ nước thoát khỏi mặt nước, âm thanh và tầm nhìn trong vắt trở lại.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class UnderwaterVisualManager : MonoBehaviour
    {
        public static UnderwaterVisualManager Instance { get; private set; }

        [Header("── Cao Độ Mặt Nước ──")]
        public float waterHeight = 16.0f;
        public float surfaceExitMargin = 0.15f; // Tránh nhấp nháy khi bơi sát mặt nước

        [Header("── Hiệu Ứng Mờ Mắt & Quang Học ──")]
        [Range(0f, 10f)]
        public float blurIntensity = 4.8f;
        [Range(0f, 1f)]
        public float waterDistortion = 0.16f;
        public Color underwaterColor = new Color(0.06f, 0.38f, 0.48f, 1.0f);
        public Color deepWaterColor = new Color(0.02f, 0.15f, 0.22f, 1.0f);

        [Header("── Sương Mù Dưới Biển ──")]
        public Color underwaterFogColor = new Color(0.04f, 0.22f, 0.32f, 1.0f);
        public float underwaterFogDensity = 0.085f;

        [Header("── Âm Thanh ──")]
        public float underwaterAudioCutoff = 650f;
        public float normalAudioCutoff = 22000f;

        // Trạng thái nội bộ
        private Camera targetCamera;
        private AudioLowPassFilter lowPassFilter;
        private AudioSource audioSource;
        private AudioClip waterSplashClip;

        private Material underwaterMat;
        private Shader underwaterShader;

        private bool isUnderwater = false;
        private float transitionProgress = 0f; // 0 = trên cạn, 1 = chìm hoàn toàn

        // Lưu thông số khí quyển gốc trên cạn
        private bool origFog;
        private Color origFogColor;
        private float origFogDensity;
        private FogMode origFogMode;

        // Hệ thống hạt bong bóng khí
        private ParticleSystem bubbleParticles;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName.ToLower().Contains("mainmenu")) return;

            GameObject bootstrapper = new GameObject("[Underwater_AutoBootstrapper]");
            bootstrapper.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(bootstrapper);
            bootstrapper.AddComponent<UnderwaterBootstrapper>();
        }

        private class UnderwaterBootstrapper : MonoBehaviour
        {
            private float timer = 0f;

            private void Start()
            {
                CheckAndAttach();
            }

            private void Update()
            {
                timer += Time.deltaTime;
                if (timer < 0.5f) return;
                timer = 0f;
                CheckAndAttach();
            }

            private void CheckAndAttach()
            {
                Camera mainCam = Camera.main;
                if (mainCam != null && mainCam.GetComponent<UnderwaterVisualManager>() == null)
                {
                    mainCam.gameObject.AddComponent<UnderwaterVisualManager>();
                }
            }
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            targetCamera = GetComponent<Camera>();

            // Tải shader mờ mắt dưới nước
            underwaterShader = Shader.Find("Hidden/UnderwaterBlurEffect");
            if (underwaterShader != null)
            {
                underwaterMat = new Material(underwaterShader);
                underwaterMat.hideFlags = HideFlags.DontSave;
            }

            SetupAudio();
            SetupBubbleParticles();
            SaveAtmosphereSettings();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (underwaterMat != null) Destroy(underwaterMat);
            RestoreAtmosphereSettings();
        }

        private void SetupAudio()
        {
            lowPassFilter = GetComponent<AudioLowPassFilter>();
            if (lowPassFilter == null)
            {
                lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
            }
            lowPassFilter.cutoffFrequency = normalAudioCutoff;
            lowPassFilter.lowpassResonanceQ = 1.4f;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f;
                audioSource.playOnAwake = false;
            }

#if UNITY_EDITOR
            waterSplashClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/Water.mp3");
#endif
        }

        private void SetupBubbleParticles()
        {
            GameObject bubbleObj = new GameObject("Underwater_Rising_Bubbles");
            bubbleObj.transform.SetParent(transform, false);
            bubbleObj.transform.localPosition = new Vector3(0f, -0.2f, 0.8f);

            bubbleParticles = bubbleObj.AddComponent<ParticleSystem>();
            var main = bubbleParticles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
            main.startColor = new Color(0.85f, 0.95f, 1.0f, 0.65f);
            main.gravityModifier = -0.4f; // Nổi ngược lên mặt nước
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;

            var emission = bubbleParticles.emission;
            emission.rateOverTime = 0f; // Bật khi ở dưới nước

            var shape = bubbleParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 20f;
            shape.radius = 0.35f;

            var rend = bubbleObj.GetComponent<ParticleSystemRenderer>();
            rend.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply") ?? Shader.Find("Diffuse"));
        }

        private void SaveAtmosphereSettings()
        {
            origFog = RenderSettings.fog;
            origFogColor = RenderSettings.fogColor;
            origFogDensity = RenderSettings.fogDensity;
            origFogMode = RenderSettings.fogMode;
        }

        private void RestoreAtmosphereSettings()
        {
            RenderSettings.fog = origFog;
            RenderSettings.fogColor = origFogColor;
            RenderSettings.fogDensity = origFogDensity;
            RenderSettings.fogMode = origFogMode;
        }

        private void Update()
        {
            if (targetCamera == null) targetCamera = GetComponent<Camera>();
            if (targetCamera == null) return;

            float camY = targetCamera.transform.position.y;
            bool shouldBeUnderwater = isUnderwater 
                ? (camY < waterHeight + surfaceExitMargin) 
                : (camY < waterHeight);

            // Xử lý khoảnh khắc chuyển giao
            if (shouldBeUnderwater != isUnderwater)
            {
                isUnderwater = shouldBeUnderwater;
                if (isUnderwater)
                {
                    OnEnterWater();
                }
                else
                {
                    OnExitWater();
                }
            }

            // Chuyển đổi mượt mà hiệu ứng (tăng tốc độ chuyển 0.18s)
            float targetP = isUnderwater ? 1f : 0f;
            transitionProgress = Mathf.MoveTowards(transitionProgress, targetP, Time.deltaTime * 5.5f);

            // 1. Điều chỉnh lọc âm thanh (Audio Low-Pass Filter)
            if (lowPassFilter != null)
            {
                lowPassFilter.cutoffFrequency = Mathf.Lerp(normalAudioCutoff, underwaterAudioCutoff, transitionProgress);
            }

            // 2. Điều chỉnh sương mù biển sâu (Underwater Oceanic Fog)
            if (transitionProgress > 0.01f)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = Color.Lerp(origFogColor, underwaterFogColor, transitionProgress);
                RenderSettings.fogDensity = Mathf.Lerp(origFogDensity, underwaterFogDensity, transitionProgress);
            }
            else if (!isUnderwater)
            {
                RestoreAtmosphereSettings();
            }

            // 3. Tỷ lệ phát bong bóng khí
            if (bubbleParticles != null)
            {
                var emission = bubbleParticles.emission;
                emission.rateOverTime = isUnderwater ? 18f : 0f;
            }
        }

        private void OnEnterWater()
        {
            // Bùng nổ 1 cụm bọt khí lúc vừa đâm sầm xuống nước
            if (bubbleParticles != null)
            {
                bubbleParticles.Emit(20);
            }

            // Âm thanh vỗ nước chìm sâu
            if (audioSource != null && waterSplashClip != null)
            {
                audioSource.pitch = Random.Range(0.85f, 0.95f);
                audioSource.PlayOneShot(waterSplashClip, 0.85f);
            }

            Debug.Log("🌊 [UnderwaterVisualManager] Đã lặn xuống biển! Kích hoạt hiệu ứng mờ mắt quang học & sương mù biển sâu.");
        }

        private void OnExitWater()
        {
            // Âm thanh trồi khỏi mặt nước
            if (audioSource != null && waterSplashClip != null)
            {
                audioSource.pitch = Random.Range(1.1f, 1.25f);
                audioSource.PlayOneShot(waterSplashClip, 0.6f);
            }

            Debug.Log("☀️ [UnderwaterVisualManager] Đã ngoi lên khỏi mặt nước! Khôi phục tầm nhìn trong vắt.");
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (underwaterMat != null && transitionProgress > 0.001f)
            {
                underwaterMat.SetFloat("_BlurIntensity", blurIntensity);
                underwaterMat.SetFloat("_Distortion", waterDistortion);
                underwaterMat.SetColor("_UnderwaterColor", underwaterColor);
                underwaterMat.SetColor("_DeepWaterColor", deepWaterColor);
                underwaterMat.SetFloat("_Transition", transitionProgress);

                Graphics.Blit(src, dest, underwaterMat);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }
    }
}

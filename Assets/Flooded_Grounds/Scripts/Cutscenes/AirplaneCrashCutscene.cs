using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using HorrorGame.Player;
using HorrorGame.Environment;

namespace HorrorGame.Cutscenes
{
    /// <summary>
    /// Cutscene mở đầu game ~30 giây — Giống The Forest:
    /// Camera ngồi bên trong cabin máy bay, nhìn hành khách,
    /// rồi turbulence, đèn đỏ chớp, mặt nạ oxy rơi, va chạm,
    /// flash trắng, tối sầm, tiêu đề game hiện lên.
    ///
    /// TIMELINE (~30s):
    ///  0.0 – 2.0s  : Fade in từ màn đen → thấy trong cabin
    ///  2.0 – 9.0s  : Bay bình thường, camera nhẹ nhàng pan (nhìn xung quanh)
    ///  9.0 – 15.0s : Turbulence nhẹ, đèn cabin nhấp nháy, hành khách lo lắng
    /// 15.0 – 21.0s : Báo động đỏ, mặt nạ oxy kích hoạt, rung cực mạnh
    /// 21.0 – 21.4s : Va chạm — flash trắng chói → tối sầm ngay
    /// 21.4 – 23.0s : Màn đen hoàn toàn (bất tỉnh)
    /// 23.0 – 27.0s : Tiêu đề game fade in / out
    /// 27.0 – 30.0s : Tỉnh dậy — fade về game world
    /// </summary>
    public class AirplaneCrashCutscene : MonoBehaviour
    {
        /// <summary>Cờ toàn cục thông báo phân cảnh mở đầu đang chạy</summary>
        public static bool IsCutsceneActive { get; private set; }

        // ──────────────────────────────────────────────
        // INSPECTOR FIELDS
        // ──────────────────────────────────────────────

        [Header("── Setup Chung ──")]
        [Tooltip("Image đen toàn màn hình (Canvas → Image, màu #000, Alpha=255)")]
        public Image blackScreenUI;
        [Tooltip("Image TRẮNG toàn màn hình để flash lúc va chạm (Alpha=0 ban đầu)")]
        public Image whiteFlashUI;
        public PlayerController playerController;
        public MonoBehaviour playerWeapon;
        public CameraShake cameraShake;

        [Header("── Tiêu Đề Game ──")]
        [Tooltip("CanvasGroup bao quanh Text/TextMeshPro tên game — Alpha=0 ban đầu")]
        public CanvasGroup gameTitleGroup;

        [Header("── Môi Trường Máy Bay ──")]
        [Tooltip("Kéo cụm Model Cabin Máy Bay vào đây")]
        public GameObject airplaneCabin;
        [Tooltip("Đèn đỏ báo động — kéo tất cả Point Light màu đỏ vào đây")]
        public Light[] warningLights;
        [Tooltip("Đèn trắng chiếu sáng cabin thường — sẽ nhấp nháy lúc turbulence")]
        public Light[] cabinLights;
        [Tooltip("Các GameObject mặt nạ oxy (sẽ SetActive(true) lúc báo động)")]
        public GameObject[] oxygenMasks;
        [Tooltip("Vị trí spawn xuống khu rừng sau cutscene")]
        public Transform forestSpawnPoint;

        [Header("── Điều Khiển Chuột Cabin (Mouse Look) ──")]
        [Tooltip("Độ nhạy lia chuột khi ngồi trong khoang máy bay")]
        public float cabinMouseSensitivity = 2.0f;
        [Tooltip("Cho phép người chơi tự do lia chuột quan sát toàn bộ khoang cabin thay vì tự động xoay đầu")]
        public bool enableCabinFreeLook = false;

        [Header("── Camera Pan (Dự phòng khi tắt Mouse Look) ──")]
        [Tooltip("Transform của Camera dùng trong cutscene (thường là Main Camera / FPS Camera)")]
        public Transform cutsceneCamera;
        [Tooltip("Các góc Euler camera sẽ pan qua trong lúc bay bình thường")]
        public Vector3[] cameraLookAngles = new Vector3[]
        {
            new Vector3(0f,    5f, 0f),   // Nhìn thẳng ra lối đi giữa và vách ngăn buồng lái
            new Vector3(-3f, -48f, 0f),   // Nhìn sang trái — cửa sổ ngắm cánh máy bay, đèn nhấp nháy và mây trôi
            new Vector3( 2f,  32f, 0f),   // Nhìn sang phải — người ngồi bên cạnh, hành lý và hàng ghế đối diện
            new Vector3(18f,   4f, 0f),   // Nhìn xuống — màn hình IFE bản đồ bay, túi an toàn và đai an toàn
            new Vector3( 0f,   0f, 0f),   // Hướng mắt trở lại lối đi cabin khi bắt đầu rung lắc
        };

        [Header("── Âm Thanh ──")]
        public AudioSource audioSource;
        [Tooltip("Giọng cơ trưởng phát thanh qua loa cabin (khoảng 15s trước khi rung lắc)")]
        public AudioClip captainAnnouncementClip;
        public AudioClip airplaneFlightClip;  // Tiếng động cơ bay bình thường
        public AudioClip turbulenceClip;      // Tiếng xóc, gió mạnh
        public AudioClip alarmBeepClip;       // Tiếng còi báo động (gpws_whoop_whoop)
        public AudioClip oxygenMaskClip;      // Tiếng mặt nạ oxy bung ra
        public AudioClip planeCrashClip;      // Tiếng va chạm nổ lớn

        [Header("── Phụ Đề / Subtitles ──")]
        [Tooltip("Text UI hiển thị lời thoại cơ trưởng (Nếu để trống, script sẽ tự tạo Text phụ đề trên Canvas)")]
        public Text subtitleText;

        [Header("── Kịch Bản Thời Gian ──")]
        [Tooltip("Fade in từ màn đen → nhìn thấy cabin [0.0 – 2.0s]")]
        public float fadeInDuration     = 2f;
        [Tooltip("Thời gian bay bình thường + 3 đoạn Cơ trưởng thông báo (3 x 15s = 45 giây)")]
        public float flyingDuration     = 45f;
        [Tooltip("Turbulence: Rung lắc tăng dần, đèn chập chờn [15 giây]")]
        public float turbulenceDuration = 15f;
        [Tooltip("Báo động đỏ: Mặt nạ oxy bung, còi hú, rung cực mạnh [15 giây]")]
        public float alarmDuration      = 15f;
        [Tooltip("Flash trắng khi va chạm [~0.4s]")]
        public float crashFlashDuration = 0.4f;

        [Header("── Hậu Va Chạm (The Forest Style) ──")]
        [Tooltip("Giọng nhân vật: Thở dốc, rên rỉ đau đớn khi bò lết")]
        public AudioClip playerCrawlVoiceClip;
        [Tooltip("Giọng nhân vật: Hít thở sâu và kiên quyết khi đứng dậy")]
        public AudioClip playerStandUpVoiceClip;
        [Tooltip("Tiếng nhịp tim đập chậm lúc ngất xỉu (35 giây)")]
        public AudioClip heartbeatClip;
        [Tooltip("Tiếng lá cây xào xạc lúc bò lết")]
        public AudioClip crawlLeavesClip;
        [Tooltip("Tiếng gió rít lúc ngất xỉu")]
        public AudioClip windAtmosphereClip;

        [Tooltip("Thời gian bò lết cố gắng tỉnh dậy [10 giây]")]
        public float crawlDuration      = 10f;
        [Tooltip("Thời gian chững lại ngất xỉu trong màn đen [35 giây]")]
        public float blackoutDuration   = 35f;
        [Tooltip("Thời gian từ từ đứng dậy sau khi tỉnh hẳn [5 giây]")]
        public float standUpDuration    = 5f;

        private AudioSource voiceAudioSource;
        private AudioSource crashAudioSource;
        private Vector3 originalCamLocalPos;
        private Quaternion originalCamLocalRot;
        private bool hasSavedCamTransform = false;

        private Canvas cutsceneCanvas;
        private List<GameObject> disabledGameplayCanvases = new List<GameObject>();
        private List<Renderer> cachedPlayerRenderers = new List<Renderer>();

        private float cabinLookYaw = 5.5f;
        private float cabinLookPitch = 2.5f;
        private bool isCabinFreeLookActive = false;

        // ──────────────────────────────────────────────
        // UNITY LIFECYCLE
        // ──────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Tự động nạp sẵn toàn bộ file âm thanh khi xem trong Inspector
            if (airplaneFlightClip == null)
                airplaneFlightClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Flight_Cabin-Sound/smooth_cabin.wav");
            if (alarmBeepClip == null)
                alarmBeepClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Flight_Cabin-Sound/gpws_whoop_whoop.wav");
            if (planeCrashClip == null)
                planeCrashClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Flight_Cabin-Sound/realistic_bomb_crash.wav");

            if (captainAnnouncementClip == null)
                captainAnnouncementClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Flight_Cabin-Sound/captain_announcement.wav");
            if (playerCrawlVoiceClip == null)
                playerCrawlVoiceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Flight_Cabin-Sound/player_crawl_struggle.mp3");
            if (playerStandUpVoiceClip == null)
                playerStandUpVoiceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Flight_Cabin-Sound/player_stand_up.mp3");
            if (heartbeatClip == null)
                heartbeatClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Flight_Cabin-Sound/heartbeat_slow.wav");
            if (crawlLeavesClip == null)
                crawlLeavesClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/LeafRustle.mp3");
            if (windAtmosphereClip == null)
                windAtmosphereClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/WindHowl.mp3");
        }
#endif

        private Material originalSkybox;
        private UnityEngine.Rendering.AmbientMode originalAmbientMode;
        private float originalAmbientIntensity;
        private void Start()
        {
            enableCabinFreeLook = false;
            enableCabinFreeLook = false;

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            voiceAudioSource = gameObject.AddComponent<AudioSource>();
            voiceAudioSource.playOnAwake = false;
            voiceAudioSource.loop = false;
            voiceAudioSource.spatialBlend = 0f; // 2D Stereo nghe rõ ràng
            voiceAudioSource.volume = 1.0f;

            crashAudioSource = gameObject.AddComponent<AudioSource>();
            crashAudioSource.playOnAwake = false;
            crashAudioSource.loop = false;
            crashAudioSource.spatialBlend = 0f; // 2D Stereo nổ thẳng vào tai
            crashAudioSource.volume = 1.0f;

            if (audioSource != null)
            {
                audioSource.spatialBlend = 0f;
                audioSource.volume = 1.0f;
            }

            if (playerController == null)
                playerController = FindObjectOfType<PlayerController>();

            // Lưu vị trí camera chuẩn của người chơi để đứng dậy
            if (playerController != null)
            {
                Camera cam = playerController.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    // Đưa camera vào xương Head để không bị lùi lại sau lưng khi có animation di chuyển người
                    Animator anim = playerController.GetComponentInChildren<Animator>();
                    if (anim != null) {
                        Transform headBone = anim.GetBoneTransform(HumanBodyBones.Head);
                        if (headBone != null && cam.transform.parent != headBone) {
                            cam.transform.SetParent(headBone, true);
                        }
                    }

                    originalCamLocalPos = cam.transform.localPosition;
                    originalCamLocalRot = cam.transform.localRotation; // LuÃƒÂ´n reset gÃƒÂ³c nhÃƒÂ¬n thÃ¡ÂºÂ³ng vÃ¡Â»Â phÃƒÂ­a trÃ†Â°Ã¡Â»â€ºc
                    hasSavedCamTransform = true;
                }
            }

            // Khoá người chơi ngay
            if (playerController != null)
            {
                playerController.enabled = false;
                Animator anim = playerController.GetComponentInChildren<Animator>();
                if (anim != null) anim.enabled = false;
            }
            if (playerWeapon     != null) playerWeapon.enabled     = false;

            // Đảm bảo người chơi luôn ngồi chính xác vào ghế 12A trong cabin máy bay khi bắt đầu cutscene
            SeatPlayerInAirplane();

            // Khởi tạo Canvas Cutscene độc lập & cô lập giao diện
            EnsureCutsceneUI();

            // Ẩn toàn bộ Gameplay UI (thanh máu, thể lực, túi đồ, v.v.) trong suốt cutscene
            SetGameplayUIVisible(false);

            // Ẩn mô hình nhân vật (mũ cối, giáp) để camera góc nhìn thứ nhất không bị che khuất
            SetPlayerRenderersVisible(false);

            // TÃ¡ÂºÂ¯t luÃƒÂ´n mÃƒÂ n hÃƒÂ¬nh Ã„â€˜en tÃ¡Â»Â« frame Ã„â€˜Ã¡ÂºÂ§u tiÃƒÂªn Ã„â€˜Ã¡Â»Æ’ trailer hiÃ¡Â»â€¡n ra lÃ¡ÂºÂ­p tÃ¡Â»Â©c
            SetBlackAlpha(0f);
            SetWhiteAlpha(0f);

            // Tắt đèn báo động & cabin
            SetWarningLights(false);
            SetCabinLights(true); // Cabin sáng bình thường ban đầu

            // Tắt mặt nạ oxy (sẽ bật ra sau)
            foreach (var mask in oxygenMasks)
                if (mask != null) mask.SetActive(false);

            // Ẩn tiêu đề
            if (gameTitleGroup != null) gameTitleGroup.alpha = 0f;

            IsCutsceneActive = true;
            Time.timeScale = 1.0f; // Luôn đảm bảo timeScale hoạt động bình thường
            SetPlayerInvincible(true);

            StartCoroutine(PlayCutscene());
        }

        private void Update()
        {
            if (isCabinFreeLookActive && cutsceneCamera != null)
            {
                HandleCabinMouseLook();
            }
        }

        /// <summary>
        /// Cho phép người chơi tự do di chuyển chuột để quan sát toàn bộ khoang cabin máy bay (300 độ ngang, 150 độ dọc)
        /// thay vì đứng ngắm đầu nhân vật tự động xoay.
        /// </summary>
        private void HandleCabinMouseLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * cabinMouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * cabinMouseSensitivity;

            cabinLookYaw += mouseX;
            cabinLookPitch -= mouseY;

            // Góc quay đầu tự nhiên khi ngồi thắt dây an toàn trên ghế máy bay:
            // Xoay ngang: từ -150 độ (quay sang trái nhìn cửa sổ, cánh máy bay và ngoái nhìn dãy ghế sau)
            //             đến +150 độ (quay sang phải nhìn lối đi, dãy ghế bên kia và ngoái nhìn phía sau)
            cabinLookYaw = Mathf.Clamp(cabinLookYaw, -150f, 150f);

            // Xoay dọc: từ -75 độ (cúi nhìn đùi, đai an toàn, khay ăn, sàn cabin)
            //           đến +75 độ (ngửa nhìn mặt nạ oxy, khoang hành lý, đèn đọc sách, trần máy bay)
            cabinLookPitch = Mathf.Clamp(cabinLookPitch, -75f, 75f);

            // Chuyển động thở nhẹ tự nhiên (Subtle Breathing Motion)
            float breathPitch = Mathf.Sin(Time.time * 1.5f) * 0.3f;
            float breathYaw   = Mathf.Cos(Time.time * 0.9f) * 0.2f;

            // Rung lắc từ cameraShake (khi turbulence hoặc báo động rơi)
            Vector3 shakeJitter = Vector3.zero;
            if (cameraShake != null)
            {
                shakeJitter = cameraShake.currentRotJitter;
            }

            Quaternion baseRot = Quaternion.Euler(cabinLookPitch + breathPitch, cabinLookYaw + breathYaw, 0f);
            if (shakeJitter != Vector3.zero)
            {
                baseRot *= Quaternion.Euler(shakeJitter);
            }

            cutsceneCamera.localRotation = baseRot;
        }

        private void OnDisable()
        {
            isCabinFreeLookActive = false;
            if (cameraShake != null) cameraShake.preventRotationOverride = false;
            IsCutsceneActive = false;
            Time.timeScale = 1.0f;
            SetPlayerInvincible(false);
            SetGameplayUIVisible(true);
            SetPlayerRenderersVisible(true);
            if (playerController != null)
            {
                Animator anim = playerController.GetComponentInChildren<Animator>();
                if (anim != null) anim.enabled = true;
            }
        }

        private void OnDestroy()
        {
            isCabinFreeLookActive = false;
            if (cameraShake != null) cameraShake.preventRotationOverride = false;
            IsCutsceneActive = false;
            Time.timeScale = 1.0f;
            SetPlayerInvincible(false);
            SetGameplayUIVisible(true);
            SetPlayerRenderersVisible(true);
            if (playerController != null)
            {
                Animator anim = playerController.GetComponentInChildren<Animator>();
                if (anim != null) anim.enabled = true;
            }
        }

        private void SetPlayerInvincible(bool invincible)
        {
            if (playerController != null)
            {
                var stats = playerController.GetComponent<PlayerStats>();
                if (stats == null) stats = playerController.GetComponentInChildren<PlayerStats>();
                if (stats != null) stats.isInvincible = invincible;
            }
        }

        // ──────────────────────────────────────────────
        // MAIN CUTSCENE COROUTINE
        // ──────────────────────────────────────────────

        private IEnumerator PlayCutscene()
        {
            // Ã¢â€â‚¬Ã¢â€â‚¬ PHASE 1 | BÃ¡ÂºÂ®T Ã„ÂÃ¡ÂºÂ¦U NGAY KHÃƒâ€NG Ã„ÂÃ¡Â»â€š MÃƒâ‚¬N HÃƒÅ’NH Ã„ÂEN LÃƒâ€šU Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
            PlayLoop(airplaneFlightClip);
            
            // HiÃ¡Â»â€¡n ngay lÃ¡ÂºÂ­p tÃ¡Â»Â©c, khÃƒÂ´ng fade Ã„â€˜en dÃƒÂ i 2s nÃ¡Â»Â¯a
            if (blackScreenUI != null)
            {
                blackScreenUI.color = new Color(0, 0, 0, 0);
                blackScreenUI.gameObject.SetActive(false);
            }
            yield return null;

            // ── PHASE 2 | CƠ TRƯỞNG PHÁT THANH & TỰ DO LIA CHUỘT QUAN SÁT CABIN ──
            if (captainAnnouncementClip != null)
            {
                voiceAudioSource.PlayOneShot(captainAnnouncementClip, 1.0f);
            }

            // Chạy phụ đề cơ trưởng song song
            StartCoroutine(DisplayCaptainSubtitles(flyingDuration));

            if (enableCabinFreeLook)
            {
                // Người chơi hoàn toàn tự do di chuyển chuột để nhìn toàn cảnh cabin máy bay
                yield return new WaitForSeconds(flyingDuration);
            }
            else
            {
                yield return StartCoroutine(PanCamera(flyingDuration));
            }

            // Tắt phụ đề khi bước sang phase rung lắc
            ClearSubtitle();

            // ── PHASE 3 | 6 GIÂY | TURBULENCE NHẸ ──────────
            if (turbulenceClip != null)
                audioSource.PlayOneShot(turbulenceClip, 0.6f);

            if (cameraShake != null)
                cameraShake.StartShake(turbulenceDuration, 0.15f, 1.5f);

            // Cabin lights nhấp nháy (đèn cabin chập chờn)
            yield return StartCoroutine(FlickerCabinLights(turbulenceDuration));

            // ── PHASE 4 | 15 GIÂY | BÁO ĐỘNG ĐỎ & RƠI TỰ DO ───────────
            SetCabinLights(false); // Đèn thường tắt hẳn
            SetWarningLights(true);

            // Báo động ngoại cảnh (động cơ bốc khói, cánh rung lắc dữ dội, sét giật)
            AirplaneCabinExterior exterior = FindObjectOfType<AirplaneCabinExterior>();
            if (exterior != null) exterior.TriggerEmergency();

            // Còi báo động phòng lái kêu liên tục (Loop) trong 15s
            if (alarmBeepClip != null)
            {
                voiceAudioSource.clip = alarmBeepClip;
                voiceAudioSource.loop = true;
                voiceAudioSource.Play();
            }

            // Mặt nạ oxy bung ra sau 0.5s
            yield return new WaitForSeconds(0.5f);
            DropOxygenMasks();
            if (oxygenMaskClip != null)
                audioSource.PlayOneShot(oxygenMaskClip, 0.8f);

            // Rung cÃ¡Â»Â±c mÃ¡ÂºÂ¡nh, dÃ¡ÂºÂ­p dÃ¡Â»Ânh hoÃ¡ÂºÂ£ng loÃ¡ÂºÂ¡n
            if (cameraShake != null)
                cameraShake.StartShake(alarmDuration - 0.5f, 0.55f, 3.0f);

            // TÃ¡Â»Â± Ã„â€˜Ã¡Â»â„¢ng cÃƒÂºi rÃ¡ÂºÂ¡p ngÃ†Â°Ã¡Â»Âi xuÃ¡Â»â€˜ng ÃƒÂ´m Ã„â€˜Ã¡ÂºÂ§u trong lÃƒÂºc hoÃ¡ÂºÂ£ng loÃ¡ÂºÂ¡n
            StartCoroutine(CrouchAndBrace(2.0f));

            // Đèn đỏ chớp tắt dồn dập (nhịp càng lúc càng nhanh)
            yield return StartCoroutine(AlarmRedFlicker(alarmDuration - 0.5f));

            // ── PHASE 5 | VA CHẠM NỔ BÙMMMM LỚN (CRASH EXPLOSION) ─────────
            isCabinFreeLookActive = false;
            if (cameraShake != null)
                cameraShake.preventRotationOverride = false;

            audioSource.Stop();
            voiceAudioSource.Stop(); // Dừng còi báo động

            // Phát tiếng nổ cực lớn trên crashAudioSource riêng (đảm bảo không bao giờ bị cắt tiếng)
            if (planeCrashClip != null)
            {
                crashAudioSource.clip = planeCrashClip;
                crashAudioSource.volume = 1.0f;
                crashAudioSource.Play();
            }

            // Rung cực đại làm chao đảo màn hình
            if (cameraShake != null)
                cameraShake.StartShake(3.0f, 3.5f, 6.0f);

            // Flash trắng chói lòa như sét đánh
            yield return StartCoroutine(WhiteFlash(1.0f));

            // Màn hình đen tối sầm ngay sau cú nổ
            SetBlackAlpha(1f);
            SetWhiteAlpha(0f);
            SetWarningLights(false);

            // DÃ¡Â»â€¹ch chuyÃ¡Â»Æ’n nhÃƒÂ¢n vÃ¡ÂºÂ­t vÃƒÂ  xÃƒÂ¡c C400 xuÃ¡Â»â€˜ng rÃ¡Â»Â«ng
            // (KhÃƒÂ´ng tÃ¡ÂºÂ¯t airplaneCabin vÃƒÂ¬ ta cÃ¡ÂºÂ§n dÃƒÂ¹ng nÃƒÂ³ lÃƒÂ m xÃƒÂ¡c rÃ¡Â»â€”ng)
            AirplaneCabinExterior extToHide = FindObjectOfType<AirplaneCabinExterior>();
            if (extToHide != null)
                extToHide.gameObject.SetActive(false);

            TeleportToForestLyingDown();

            // Cho người chơi nghe trọn vẹn 3 giây tiếng nổ rền vang, kim loại vỡ vụn
            yield return new WaitForSeconds(3.0f);

            if (cameraShake != null)
                cameraShake.StopShake();

            // ── PHASE 6 | CHỮNG LẠI 15 GIÂY TRONG BÓNG TỐI SAU KHI RƠI ──────
            yield return StartCoroutine(InitialCrashBlackoutSequence(15f));

            // ── PHASE 7 | TỈNH LẠI TẠM THỜI & BÒ LẾT (~10 GIÂY) ──────
            yield return StartCoroutine(CrawlStruggleSequence(crawlDuration));

            // ── PHASE 8 | NGẤT XỈU & CHỮNG LẠI TRONG BÓNG TỐI (35 GIÂY) ──────
            yield return StartCoroutine(BlackoutUnconsciousSequence(blackoutDuration));

            // ── PHASE 9 | TỈNH HẲN & TỪ TỪ ĐỨNG DẬY (5 GIÂY) ───────────────
            yield return StartCoroutine(StandUpSequence(standUpDuration));

            if (blackScreenUI != null)
                blackScreenUI.gameObject.SetActive(false);

            // Bật lại toàn bộ UI Gameplay (thanh chỉ số sinh tồn, HUD) khi nhân vật đã đứng dậy và chính thức vào game
            SetGameplayUIVisible(true);

            // Trả điều khiển cho người chơi
            if (playerController != null) playerController.enabled = true;
            if (playerWeapon     != null) playerWeapon.enabled     = true;

            IsCutsceneActive = false;
            SetPlayerInvincible(false);

            // Tắt script cutscene sau khi hoàn thành (thay vì Destroy để không làm lỗi Inspector)
            enabled = false;
        }

        // ──────────────────────────────────────────────
        // COROUTINE HELPERS
        // ──────────────────────────────────────────────

        /// <summary>Fade màn đen từ fromAlpha → toAlpha trong duration giây</summary>
        private IEnumerator FadeBlack(float fromAlpha, float toAlpha, float duration)
        {
            if (blackScreenUI == null) { yield return new WaitForSeconds(duration); yield break; }
            blackScreenUI.gameObject.SetActive(true);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                blackScreenUI.color = new Color(0, 0, 0, Mathf.Lerp(fromAlpha, toAlpha, t / duration));
                yield return null;
            }
            blackScreenUI.color = new Color(0, 0, 0, toAlpha);
        }

        /// <summary>Flash màn trắng chói rồi fade nhanh — cảm giác va chạm mạnh</summary>
        private IEnumerator WhiteFlash(float duration)
        {
            if (whiteFlashUI == null) { yield return new WaitForSeconds(duration); yield break; }
            whiteFlashUI.gameObject.SetActive(true);
            whiteFlashUI.color = new Color(1, 1, 1, 1f);
            yield return new WaitForSeconds(duration * 0.3f);

            float t = 0f;
            float fadeDur = duration * 0.7f;
            while (t < fadeDur)
            {
                t += Time.deltaTime;
                whiteFlashUI.color = new Color(1, 1, 1, Mathf.Lerp(1f, 0f, t / fadeDur));
                yield return null;
            }
            whiteFlashUI.color = new Color(1, 1, 1, 0f);
        }

        /// <summary>Camera từ từ pan qua các góc nhìn trong cabin (breathing sway nhẹ)</summary>
        private IEnumerator PanCamera(float totalDuration)
        {
            if (cutsceneCamera == null || cameraLookAngles.Length == 0)
            {
                yield return new WaitForSeconds(totalDuration);
                yield break;
            }

            float timePerAngle = totalDuration / cameraLookAngles.Length;
            foreach (var angle in cameraLookAngles)
            {
                Quaternion startRot = cutsceneCamera.localRotation;
                Quaternion target   = Quaternion.Euler(angle);
                float elapsed = 0f;

                while (elapsed < timePerAngle)
                {
                    elapsed += Time.deltaTime;
                    float sway = Mathf.Sin(elapsed * 0.9f) * 0.25f;
                    Quaternion swaySlight = target * Quaternion.Euler(sway, sway * 0.5f, 0f);
                    cutsceneCamera.localRotation = Quaternion.Slerp(startRot, swaySlight, elapsed / timePerAngle);
                    yield return null;
                }
            }
        }

        /// <summary>Đèn cabin nhấp nháy kiểu Perlin — giống chập điện</summary>
        private IEnumerator FlickerCabinLights(float duration)
        {
            if (cabinLights == null || cabinLights.Length == 0)
            {
                yield return new WaitForSeconds(duration);
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float noise = Mathf.PerlinNoise(t * 4f, 0f);
                bool on = noise > 0.25f;
                foreach (var l in cabinLights)
                    if (l != null) l.intensity = on ? 1f : 0f;
                yield return null;
            }
            SetCabinLights(false);
        }

        /// <summary>Đèn đỏ chớp tắt loạn — nhịp ngắn dần khi gần crash</summary>
        private IEnumerator AlarmRedFlicker(float duration)
        {
            float t = 0f;
            float nextFlicker = 0f;
            bool lightsOn = true;

            while (t < duration)
            {
                t += Time.deltaTime;
                if (t >= nextFlicker)
                {
                    lightsOn = !lightsOn;
                    SetWarningLights(lightsOn);
                    // Nhịp chớp ngắn dần theo thời gian → tạo cảm giác căng thẳng leo thang
                    float urgency = Mathf.Lerp(0.18f, 0.06f, t / duration);
                    nextFlicker = t + urgency;
                }
                yield return null;
            }
            SetWarningLights(false);
        }

        /// <summary>Fade CanvasGroup alpha</summary>
        private IEnumerator FadeGroup(CanvasGroup cg, float from, float to, float duration)
        {
            float t = 0f;
            cg.alpha = from;
            while (t < duration)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            cg.alpha = to;
        }

        // ──────────────────────────────────────────────
        // UTILITY METHODS
        // ──────────────────────────────────────────────

        private void SetBlackAlpha(float a)
        {
            if (blackScreenUI == null) return;
            blackScreenUI.gameObject.SetActive(true);
            blackScreenUI.color = new Color(0, 0, 0, a);
        }

        private void SetWhiteAlpha(float a)
        {
            if (whiteFlashUI == null) return;
            whiteFlashUI.gameObject.SetActive(a > 0f);
            whiteFlashUI.color = new Color(1, 1, 1, a);
        }

        private void SetWarningLights(bool on)
        {
            foreach (var l in warningLights)
                if (l != null) l.intensity = on ? 3.5f : 0f;
        }

        private void SetCabinLights(bool on)
        {
            foreach (var l in cabinLights)
                if (l != null) l.intensity = on ? 1f : 0f;
        }

        private void DropOxygenMasks()
        {
            foreach (var mask in oxygenMasks)
                if (mask != null) mask.SetActive(true);
        }

        private void PlayLoop(AudioClip clip)
        {
            if (clip == null || audioSource == null) return;
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.Play();
        }

        private IEnumerator CrouchAndBrace(float duration)
        {
            if (cutsceneCamera == null) yield break;

            Vector3 startPos = cutsceneCamera.localPosition;
            Quaternion startRot = cutsceneCamera.localRotation;

            // CÃƒÂºi gÃ¡ÂºÂ­p hÃ¡ÂºÂ³n ngÃ†Â°Ã¡Â»Âi xuÃ¡Â»â€˜ng ÃƒÂ´m Ã„â€˜Ã¡ÂºÂ§u (HÃ¡ÂºÂ¡ thÃ¡ÂºÂ¥p 1.1 mÃƒÂ©t vÃƒÂ  nhÃƒÂ¬n gÃ¡ÂºÂ­p xuÃ¡Â»â€˜ng 65 Ã„â€˜Ã¡Â»â„¢)
            Vector3 targetPos = new Vector3(startPos.x, Mathf.Max(0.2f, startPos.y - 1.1f), startPos.z);
            Quaternion targetRot = Quaternion.Euler(65f, 0f, 0f);

            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(t / duration);
                // DÃƒÂ¹ng Ã„â€˜Ã†Â°Ã¡Â»Âng cong mÃ†Â°Ã¡Â»Â£t (SmoothStep)
                float curve = normalizedTime * normalizedTime * (3f - 2f * normalizedTime);

                cutsceneCamera.localPosition = Vector3.Lerp(startPos, targetPos, curve);
                cutsceneCamera.localRotation = Quaternion.Lerp(startRot, targetRot, curve);
                yield return null;
            }
        }

        private void SeatPlayerInAirplane()
        {
            if (playerController == null)
                playerController = FindObjectOfType<HorrorGame.Player.PlayerController>();

            if (airplaneCabin == null)
            {
                airplaneCabin = GameObject.Find("[C400_AIRPLANE_CABIN]");
                
#if UNITY_EDITOR
                // Tự động load mô hình C400 vào scene nếu chưa có
                if (airplaneCabin == null)
                {
                    GameObject c400Prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/c-400/source/c-400.fbx");
                    if (c400Prefab != null)
                    {
                        // Luôn đưa máy bay lên bầu trời (Y=1000) để không bị đâm xuyên xuống nhà cửa dưới mặt đất
                        Vector3 spawnPos = new Vector3(0f, 1000f, 0f);
                        
                        GameObject oldCabin = GameObject.Find("Temp_AirplaneCabin");
                        if (oldCabin != null) 
                        {
                            oldCabin.SetActive(false); // Ẩn máy bay cũ để không bị ghế xanh
                        }
                        GameObject oldCabin2 = GameObject.Find("Realistic_Airplane_Cabin");
                        if (oldCabin2 != null) oldCabin2.SetActive(false);
                        
                        airplaneCabin = Instantiate(c400Prefab, spawnPos, Quaternion.identity);
                        airplaneCabin.name = "[C400_AIRPLANE_CABIN]";
                        // Scale x10 vì x100 quá to bao phủ cả map
                        airplaneCabin.transform.localScale = new Vector3(10f, 10f, 10f);
                    }
                }
#endif

                if (airplaneCabin == null) airplaneCabin = GameObject.Find("Realistic_Airplane_Cabin");
                if (airplaneCabin == null) airplaneCabin = GameObject.Find("Temp_AirplaneCabin");
            }

            if (airplaneCabin != null)
            {
                airplaneCabin.SetActive(true);
                // C400 cÃƒÂ³ thÃ¡Â»Æ’ rÃ¡Â»â€”ng hoÃ¡ÂºÂ·c khÃƒÂ´ng cÃƒÂ³ cÃ¡Â»Â­a sÃ¡Â»â€¢/bÃ¡ÂºÂ§u trÃ¡Â»Âi, nÃƒÂªn ta BÃ¡ÂºÂ¬T LÃ¡ÂºÂ I hÃ¡Â»â€¡ thÃ¡Â»â€˜ng giÃ¡ÂºÂ£ lÃ¡ÂºÂ­p mÃƒÂ´i trÃ†Â°Ã¡Â»Âng bay!
                EnsureCabinExteriorAndWindowTransparency(airplaneCabin);
                
            }

            Vector3 standPos = Vector3.zero;
            Quaternion standRot = Quaternion.identity;

            if (airplaneCabin != null)
            {
                Transform alignPivot = airplaneCabin.transform.Find("CabinAlignmentPivot");
                if (alignPivot != null)
                {
                    standPos = alignPivot.position;
                    standRot = alignPivot.rotation;
                }
                else
                {
                    standPos = airplaneCabin.transform.position;
                    if (airplaneCabin.name == "[C400_AIRPLANE_CABIN]")
                    {
                        Renderer[] renderers = airplaneCabin.GetComponentsInChildren<Renderer>();
                        if (renderers.Length > 0)
                        {
                            Bounds bounds = renderers[0].bounds;
                            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                            standPos = bounds.center; // Đưa vào chính giữa khoang
                            
                            // Gắn tạm MeshCollider để bắn tia dò tìm mặt sàn
                            System.Collections.Generic.List<MeshCollider> tempColliders = new System.Collections.Generic.List<MeshCollider>();
                            foreach(var r in renderers) {
                                if (r.gameObject.GetComponent<Collider>() == null) {
                                    tempColliders.Add(r.gameObject.AddComponent<MeshCollider>());
                                }
                            }
                            
                            RaycastHit hit;
                            if (Physics.Raycast(bounds.center, Vector3.down, out hit, 1000f)) {
                                standPos.y = hit.point.y + 0.1f;
                            } else {
                                standPos.y = airplaneCabin.transform.position.y;
                            }
                            
                            foreach(var mc in tempColliders) {
                                Destroy(mc);
                            }
                        }
                    }
                    standRot = airplaneCabin.transform.rotation;
                }
            }
            if (airplaneCabin != null)
            {
                // TÃ¡Â»Â± Ã„â€˜Ã¡Â»â„¢ng tÃ¡ÂºÂ¡o mÃ¡Â»â„¢t cÃƒÂ¡i Ã„â€˜ÃƒÂ¨n thÃ¡ÂºÂ¯p sÃƒÂ¡ng khoang C400 Ã„â€˜Ã¡Â»Æ’ khÃƒÂ´ng bÃ¡Â»â€¹ Ã„â€˜en thui
                GameObject interiorLight = GameObject.Find("C400_InteriorLight");
                if (interiorLight == null)
                {
                    interiorLight = new GameObject("C400_InteriorLight");
                    interiorLight.transform.SetParent(airplaneCabin.transform);
                    interiorLight.transform.position = standPos + new Vector3(0, 2f, 0); // Ã„ÂÃ¡ÂºÂ·t Ã„â€˜ÃƒÂ¨n trÃƒÂªn Ã„â€˜Ã¡ÂºÂ§u 2m
                    Light lit = interiorLight.AddComponent<Light>();
                    lit.type = LightType.Point;
                    lit.range = 25f;
                    lit.intensity = 1.2f;
                    lit.color = new Color(1f, 0.95f, 0.9f); // ÃƒÂnh sÃƒÂ¡ng vÃƒÂ ng Ã¡ÂºÂ¥m cÃ¡Â»Â§a Ã„â€˜ÃƒÂ¨n cabin
                    lit.shadows = LightShadows.Soft;

                    System.Collections.Generic.List<Light> lightsList = new System.Collections.Generic.List<Light>(cabinLights);
                    lightsList.Add(lit);
                    cabinLights = lightsList.ToArray();
                }
            }

            if (playerController != null)
            {
                // TÃ¡ÂºÂ¯t Component ngÃ¡Â»â€œi Ã„â€˜Ã¡Â»Æ’ nhÃƒÂ¢n vÃ¡ÂºÂ­t chuyÃ¡Â»Æ’n sang trÃ¡ÂºÂ¡ng thÃƒÂ¡i Ã„ÂÃ¡Â»Â©ng
                AirplanePassengerSeatPose seatPose = playerController.GetComponent<AirplanePassengerSeatPose>();
                if (seatPose != null)
                {
                    seatPose.StandUp();
                    Destroy(seatPose);
                }

                CharacterController cc = playerController.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                playerController.transform.position = standPos;
                playerController.transform.rotation = standRot;

                Camera cam = playerController.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    // Đưa camera vào xương Head để không bị lùi lại sau lưng khi có animation di chuyển người
                    Animator anim = playerController.GetComponentInChildren<Animator>();
                    if (anim != null) {
                        Transform headBone = anim.GetBoneTransform(HumanBodyBones.Head);
                        if (headBone != null && cam.transform.parent != headBone) {
                            cam.transform.SetParent(headBone, true);
                        }
                    }

                    originalCamLocalPos = cam.transform.localPosition;
                    originalCamLocalRot = cam.transform.localRotation;
                    hasSavedCamTransform = true;
                }

                // Hạ thấp camera nằm sát mặt đất (tầm mắt của người nằm úp/nghiêng bên cỏ)
                // Đẩy nhẹ về phía trước để có góc nhìn thoáng đãng
                cam.transform.localPosition = new Vector3(originalCamLocalPos.x, 0.22f, originalCamLocalPos.z + 0.35f);

                // Góc nhìn chuẩn người nằm bên đất: Má áp cỏ, mắt nhìn thẳng ra phía trước qua cánh rừng
                // Pitch: 2° (nhìn ngang tầm cỏ), Yaw: 5°, Roll: 22° (đầu nghiêng một bên trên nền đất)
                cam.transform.localRotation = Quaternion.Euler(2f, 5f, 22f);
            }
            if (airplaneCabin != null && forestSpawnPoint != null)
            {
                // DÃ¡Â»â€¹ch chuyÃ¡Â»Æ’n C400 ra xa khÃ¡Â»Âi ngÃ†Â°Ã¡Â»Âi chÃ†Â¡i Ã„â€˜Ã¡Â»Æ’ ngÃ†Â°Ã¡Â»Âi chÃ†Â¡i bÃƒÂ² ngoÃƒÂ i Ã„â€˜Ã¡Â»â€˜ng Ã„â€˜Ã¡Â»â€¢ nÃƒÂ¡t
                // XÃƒÂ¡c C400 khÃ¡Â»â€¢ng lÃ¡Â»â€œ nÃƒÂªn cÃ¡ÂºÂ§n cÃƒÂ¡ch xa 20m vÃ¡Â»Â phÃƒÂ­a trÃ†Â°Ã¡Â»â€ºc vÃƒÂ  lÃ¡Â»â€¡ch sang trÃƒÂ¡i 10m
                airplaneCabin.transform.position = forestSpawnPoint.position + (forestSpawnPoint.forward * 20f) - (forestSpawnPoint.right * 10f) + new Vector3(0, 1.5f, 0);
                
                // TÃ¡ÂºÂ¡o gÃƒÂ³c nghiÃƒÂªng Ã„â€˜ÃƒÂ¢m chÃƒÂºc Ã„â€˜Ã¡ÂºÂ§u vÃƒÂ  lÃ¡ÂºÂ­t nghiÃƒÂªng thÃƒÂ¢n Ã„â€˜Ã¡Â»Æ’ trÃƒÂ´ng giÃ¡Â»â€˜ng rÃ¡Â»â€ºt thÃ¡ÂºÂ­t
                airplaneCabin.transform.rotation = forestSpawnPoint.rotation * Quaternion.Euler(-12f, 30f, 25f);

                // XoÃƒÂ¡ bÃ¡Â»Â toÃƒÂ n bÃ¡Â»â„¢ lÃ¡Â»â€ºp tÃ†Â°Ã¡Â»Âng giÃ¡ÂºÂ£, cÃ¡Â»Â­a sÃ¡Â»â€¢ giÃ¡ÂºÂ£ vÃƒÂ  mÃƒÂ¢y bay (CabinAlignmentPivot)
                Transform alignPivot = airplaneCabin.transform.Find("CabinAlignmentPivot");
                if (alignPivot != null)
                {
                    Destroy(alignPivot.gameObject);
                }

                // TÃ¡ÂºÂ¡o hiÃ¡Â»â€¡u Ã¡Â»Â©ng chÃƒÂ¡y khÃƒÂ³i cho Ã„â€˜Ã¡Â»â€˜ng Ã„â€˜Ã¡Â»â€¢ nÃƒÂ¡t
                CreateCrashEffects(airplaneCabin.transform);
                RenderSettings.skybox = originalSkybox;
                RenderSettings.ambientMode = originalAmbientMode;
                RenderSettings.ambientIntensity = originalAmbientIntensity;
                DynamicGI.UpdateEnvironment();
            }
        }

        private void CreateCrashEffects(Transform wreckTarget)
        {
            // TÃ¡ÂºÂ¡o ÃƒÂ¡nh sÃƒÂ¡ng lÃ¡Â»Â­a bÃ¡ÂºÂ­p bÃƒÂ¹ng
            GameObject fireLight = new GameObject("Wreck_FireLight");
            fireLight.transform.position = wreckTarget.position + new Vector3(0, 5f, 0);
            Light lit = fireLight.AddComponent<Light>();
            lit.type = LightType.Point;
            lit.range = 40f;
            lit.intensity = 2.5f;
            lit.color = new Color(1f, 0.4f, 0.1f); // Cam Ã„â€˜Ã¡Â»Â
            lit.shadows = LightShadows.Soft;

            // ChÃ¡Â»â€ºp tÃ¡ÂºÂ¯t lÃ¡Â»Â­a (Animation Ã„â€˜Ã†Â¡n giÃ¡ÂºÂ£n)
            var flicker = fireLight.AddComponent<LightFlicker_Runtime>();
            flicker.lightSource = lit;

            // TÃ¡ÂºÂ¡o khÃƒÂ³i Ã„â€˜en
            GameObject smokeObj = new GameObject("Wreck_Smoke");
            smokeObj.transform.position = wreckTarget.position + new Vector3(0, 2f, 0);
            smokeObj.transform.rotation = Quaternion.Euler(-90f, 0, 0); // HÃ†Â°Ã¡Â»â€ºng lÃƒÂªn
            ParticleSystem ps = smokeObj.AddComponent<ParticleSystem>();
            ps.Stop();
            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = 10f;
            main.startSpeed = 8f;
            main.startSize = 2f;
            main.startColor = new Color(0.1f, 0.1f, 0.1f, 0.6f);
            main.maxParticles = 200;
            var emission = ps.emission;
            emission.rateOverTime = 20f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 2f;
            Shader smokeShader = Shader.Find("Particles/Standard Unlit");
            if (smokeShader == null) smokeShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (smokeShader == null) smokeShader = Shader.Find("Mobile/Particles/Alpha Blended");
            if (smokeShader != null) {
                Material smokeMat = new Material(smokeShader);
                smokeMat.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);
                if (smokeMat.HasProperty("_TintColor")) smokeMat.SetColor("_TintColor", new Color(0.1f, 0.1f, 0.1f, 0.6f));
                ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = smokeMat;
            }

            // TÃ¡ÂºÂ¡o lÃ¡Â»Â­a
            GameObject fireObj = new GameObject("Wreck_Fire");
            fireObj.transform.position = wreckTarget.position;
            fireObj.transform.rotation = Quaternion.Euler(-90f, 0, 0);
            ParticleSystem pFire = fireObj.AddComponent<ParticleSystem>();
            pFire.Stop();
            var fMain = pFire.main;
            fMain.duration = 5f;
            fMain.loop = true;
            fMain.startLifetime = 2.5f;
            fMain.startSpeed = 10f;
            fMain.startSize = 1.5f;
            fMain.startColor = new Color(1f, 0.3f, 0f, 0.8f); // Ã„ÂÃ¡Â»Â cam
            fMain.maxParticles = 150;
            var fEmission = pFire.emission;
            fEmission.rateOverTime = 40f;
            var fShape = pFire.shape;
            fShape.shapeType = ParticleSystemShapeType.Hemisphere;
            fShape.radius = 8.0f; // Tăng bán kính bốc lửa bao trùm xác máy bay
            fMain.startSize = 4.0f; // Lửa to hơn
            // Bỏ gán material để Unity tự động dùng Default-Particle (khắc phục lỗi ô vuông màu cam)
            var pRenderer = pFire.GetComponent<ParticleSystemRenderer>();
            if (pRenderer != null) {
                Material defaultMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
                if (defaultMat != null && defaultMat.shader != null) {
                    pRenderer.sharedMaterial = defaultMat;
                }
            }
            ps.Play();
            pFire.Play();

            // TÃ¡ÂºÂ¡o mÃ¡ÂºÂ£nh vÃ¡Â»Â¡ vÃ„Æ’ng tung tÃƒÂ³e xung quanh
            for (int i = 0; i < 20; i++)
            {
                PrimitiveType pt = Random.value > 0.5f ? PrimitiveType.Cube : PrimitiveType.Cylinder;
                GameObject debris = GameObject.CreatePrimitive(pt);
                debris.name = "C400_Debris_" + i;
                
                // RÃ¡ÂºÂ£i ngÃ¡ÂºÂ«u nhiÃƒÂªn trong bÃƒÂ¡n kÃƒÂ­nh 20m xung quanh xÃƒÂ¡c mÃƒÂ¡y bay
                Vector2 randCircle = Random.insideUnitCircle * 20f;
                Vector3 spawnPos = wreckTarget.position + new Vector3(randCircle.x, 20f, randCircle.y); // ThÃ¡ÂºÂ£ tÃ¡Â»Â« trÃƒÂªn cao rÃ¡Â»â€ºt xuÃ¡Â»â€˜ng
                debris.transform.position = spawnPos;
                
                // KÃƒÂ­ch thÃ†Â°Ã¡Â»â€ºc ngÃ¡ÂºÂ«u nhiÃƒÂªn mÃ¡ÂºÂ£nh vÃ¡Â»Â¡ sÃ¡ÂºÂ¯t thÃƒÂ©p
                debris.transform.localScale = new Vector3(Random.Range(0.2f, 1.5f), Random.Range(0.1f, 3f), Random.Range(0.2f, 2f));
                debris.transform.rotation = Random.rotation;

                // ThÃƒÂªm Rigidbody Ã„â€˜Ã¡Â»Æ’ nÃƒÂ³ tÃ¡Â»Â± rÃ¡Â»â€ºt vÃƒÂ  lÃ„Æ’n lÃƒÂ³c dÃ†Â°Ã¡Â»â€ºi Ã„â€˜Ã¡ÂºÂ¥t tÃ¡Â»Â± nhiÃƒÂªn
                Rigidbody rb = debris.AddComponent<Rigidbody>();
                rb.mass = Random.Range(10f, 100f);
                rb.drag = 0.5f;
                rb.angularDrag = 0.5f;

                // Ã„ÂÃ¡Â»â€¢i mÃƒÂ u thÃƒÂ nh kim loÃ¡ÂºÂ¡i chÃƒÂ¡y Ã„â€˜en xÃ¡Â»â€°n
                Renderer r = debris.GetComponent<Renderer>();
                if (r != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = new Color(0.15f, 0.15f, 0.15f); // Ã„Âen chÃƒÂ¡y
                    mat.SetFloat("_Metallic", 0.8f);
                    mat.SetFloat("_Glossiness", 0.2f);
                    r.material = mat;
                }
            }
        }

        // Script Ã„â€˜Ã¡Â»Æ’ nhÃ¡ÂºÂ¥p nhÃƒÂ¡y Ã„â€˜ÃƒÂ¨n lÃ¡Â»Â­a
        private class LightFlicker_Runtime : MonoBehaviour
        {
            public Light lightSource;
            private float baseIntensity;
            private void Start() { if (lightSource) baseIntensity = lightSource.intensity; }
            private void Update() { if (lightSource) lightSource.intensity = baseIntensity + Mathf.PerlinNoise(Time.time * 5f, 0f) * 1.5f; }
        }

        /// <summary>Chững lại 15 giây trong bóng tối ngay sau vụ rơi máy bay</summary>
        private IEnumerator InitialCrashBlackoutSequence(float duration)
        {
            SetBlackAlpha(1f);

            // Tiếng gió rít lạnh lẽo xa xăm giữa đống đổ nát
            if (windAtmosphereClip != null)
            {
                audioSource.clip = windAtmosphereClip;
                audioSource.loop = true;
                audioSource.volume = 0.6f;
                audioSource.Play();
            }

            SetSubtitle("... (Máy bay nổ tung... Bạn nằm bất tỉnh trong bóng tối) ...");
            yield return new WaitForSeconds(8f);
            ClearSubtitle();

            if (duration > 8f)
            {
                yield return new WaitForSeconds(duration - 8f);
            }
        }

        /// <summary>Đoạn 1: Nhân vật mở mắt he hé, thở dốc và cố trườn bò lết về phía trước (~10s)</summary>
        private IEnumerator CrawlStruggleSequence(float duration)
        {
            Camera cam = playerController != null ? playerController.GetComponentInChildren<Camera>() : null;
            Vector3 lyingCamPos = new Vector3(originalCamLocalPos.x, 0.22f, originalCamLocalPos.z + 0.35f);

            // Chớp mắt he hé (Blink): mở ra, nhắm lại vì choáng váng, rồi mở he hé nhìn đất
            yield return StartCoroutine(FadeBlack(1f, 0.5f, 1.2f));
            yield return StartCoroutine(FadeBlack(0.5f, 0.85f, 0.4f));
            yield return StartCoroutine(FadeBlack(0.85f, 0.15f, 1.0f));

            // Phát giọng nói thở dốc, đau đớn của nhân vật chính
            voiceAudioSource.loop = false;
            voiceAudioSource.volume = 1.0f;
            if (playerCrawlVoiceClip != null)
            {
                voiceAudioSource.clip = playerCrawlVoiceClip;
                voiceAudioSource.Play();
            }

            SetSubtitle("[Bạn]: \"*Thở dốc* Ugh... What... what happened? I have to crawl out of here...\"\n<size=17><color=#D1D5DB>(Chuyện gì vừa xảy ra vậy...? Mình... phải bò ra khỏi đây...)</color></size>");

            float elapsed = 0f;
            float stepSoundTimer = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                stepSoundTimer += Time.deltaTime;

                // Chu kỳ nhịp điệu trườn bò (bước tay kéo - nghỉ rướn)
                float cycle = elapsed * 2.8f;
                float strokePower = Mathf.Clamp01(Mathf.Sin(cycle) + 0.35f); // Xung lực cánh tay rướn kéo
                float forwardSpeed = 0.55f * strokePower;

                // Thân người từ từ trườn bò tới trước sát mặt đất
                if (playerController != null && elapsed < 5.5f)
                {
                    Vector3 crawlDir = playerController.transform.forward;
                    // Bắt đầu quét vật cản từ phía trước mép nhân vật (cách 0.5m) để tránh tự va vào collider của chính mình
                    Vector3 checkOrigin = playerController.transform.position + Vector3.up * 0.40f + crawlDir * 0.50f;
                    RaycastHit hit;
                    bool blocked = false;

                    if (Physics.SphereCast(checkOrigin, 0.22f, crawlDir, out hit, 0.45f))
                    {
                        if (hit.transform != playerController.transform && !hit.transform.IsChildOf(playerController.transform))
                        {
                            // Chỉ bị chặn nếu là vách đứng (mảnh vỡ, thân máy bay, cây), không chặn bởi mặt đất phẳng
                            if (Vector3.Dot(hit.normal, Vector3.up) < 0.6f)
                            {
                                blocked = true;
                            }
                        }
                    }

                    if (!blocked)
                    {
                        Vector3 newPos = playerController.transform.position + crawlDir * (forwardSpeed * Time.deltaTime);
                        Terrain terr = Terrain.activeTerrain;
                        if (terr != null)
                        {
                            newPos.y = terr.SampleHeight(newPos) + terr.transform.position.y + 0.08f;
                        }
                        playerController.transform.position = newPos;
                    }
                }

                // Tiếng lá cỏ xào xạc lúc cào tay chân rướn người bò
                if (stepSoundTimer >= 1.15f)
                {
                    stepSoundTimer = 0f;
                    if (crawlLeavesClip != null)
                        audioSource.PlayOneShot(crawlLeavesClip, 0.7f);
                }

                // Camera chuyển động trườn bò thứ nhất (First-person dynamic crawl sway & surge)
                // - Đầu nhô tới trước khi rướn tay kéo (surgeZ), lắc lư 2 vai (swayX), gật đầu nhìn đất (bobY)
                float bobY     = Mathf.Abs(Mathf.Sin(cycle)) * 0.08f;
                float surgeZ   = Mathf.Sin(cycle) * 0.12f;
                float swayX    = Mathf.Sin(elapsed * 1.4f) * 0.09f;
                float pitchBob = Mathf.Sin(cycle) * 5f;
                float rollBob  = Mathf.Cos(elapsed * 1.4f) * 6.5f;
                float yawBob   = Mathf.Sin(elapsed * 1.4f) * 4.5f;

                if (cam != null && elapsed < 5.5f)
                {
                    cam.transform.localPosition = lyingCamPos + new Vector3(swayX, bobY, surgeZ);
                    cam.transform.localRotation = Quaternion.Euler(2f + pitchBob, 5f + yawBob, 22f + rollBob);
                }

                // Sau 5.5s: Kiệt sức, mắt mờ dần, đầu từ từ gục xuống đất và tối sầm lại
                if (elapsed >= 5.5f)
                {
                    SetSubtitle("[Bạn]: \"*Kiệt sức* No... my eyes... I can't stay awake... Somebody, please help...\"\n<size=17><color=#F87171>(Không thể... mắt mình mờ quá... làm ơn cứu tôi với...)</color></size>");
                    float fadeProgress = Mathf.Clamp01((elapsed - 5.5f) / (duration - 5.5f));

                    // Đầu gục hẳn xuống mặt đất (pitch gục xuống -5°, roll nghiêng 32°, Y hạ xuống 0.14m)
                    if (cam != null)
                    {
                        cam.transform.localPosition = Vector3.Lerp(lyingCamPos, new Vector3(originalCamLocalPos.x, 0.14f, originalCamLocalPos.z + 0.35f), fadeProgress);
                        cam.transform.localRotation = Quaternion.Slerp(Quaternion.Euler(2f, 5f, 22f), Quaternion.Euler(-5f, 8f, 32f), fadeProgress);
                    }

                    if (blackScreenUI != null)
                    {
                        blackScreenUI.gameObject.SetActive(true);
                        blackScreenUI.color = new Color(0, 0, 0, Mathf.Lerp(0.15f, 1f, fadeProgress));
                    }
                }

                yield return null;
            }

            SetBlackAlpha(1f);
            ClearSubtitle();
        }

        /// <summary>Đoạn 2: Ngất xỉu và chững lại trong màn đen (35 giây)</summary>
        private IEnumerator BlackoutUnconsciousSequence(float duration)
        {
            SetBlackAlpha(1f);

            // Bắt đầu tiếng nhịp tim đập chậm rãi, ngột ngạt
            if (heartbeatClip != null)
            {
                voiceAudioSource.clip = heartbeatClip;
                voiceAudioSource.loop = true;
                voiceAudioSource.Play();
            }

            // Tiếng gió rít lạnh lẽo xa xăm
            if (windAtmosphereClip != null)
            {
                audioSource.clip = windAtmosphereClip;
                audioSource.loop = true;
                audioSource.Play();
            }

            // 0s - 8s: Cơn hôn mê sâu
            SetSubtitle("... (Bạn kiệt sức và chìm sâu vào cơn hôn mê) ...");
            yield return new WaitForSeconds(8f);
            ClearSubtitle();

            // 8s - 22s: Hiện Tiêu đề game (Title Card)
            if (gameTitleGroup != null)
            {
                yield return StartCoroutine(FadeGroup(gameTitleGroup, 0f, 1f, 2.0f));
                yield return new WaitForSeconds(6.0f);
                yield return StartCoroutine(FadeGroup(gameTitleGroup, 1f, 0f, 2.0f));
            }
            else
            {
                yield return new WaitForSeconds(10f);
            }

            // Tua thời gian trong game trôi qua vài tiếng (từ chiều sang đêm hoặc từ đêm sang sáng)
            if (DayNightCycle.Instance != null)
                DayNightCycle.Instance.currentHour += 4f;

            // 22s - 32s: Dòng chữ vài giờ sau
            SetSubtitle("... (Nhiều giờ sau, trong cánh rừng hoang vu) ...");
            yield return new WaitForSeconds(10f);
            ClearSubtitle();

            // 32s - 35s: Chờ đủ thời gian 35s
            if (duration > 28f)
            {
                yield return new WaitForSeconds(duration - 28f);
            }
        }

        /// <summary>Đoạn 3: Tỉnh hẳn và từ từ chống tay, gượng người đứng dậy (~6s chân thực như ngoài đời)</summary>
        private IEnumerator StandUpSequence(float duration)
        {
            Camera cam = playerController != null ? playerController.GetComponentInChildren<Camera>() : null;
            if (duration < 6.0f) duration = 6.0f;

            // Dừng tiếng nhịp tim và tiếng gió hôn mê
            voiceAudioSource.Stop();
            voiceAudioSource.loop = false;
            audioSource.Stop();

            // Phát giọng nói kiên định, gắng gượng thở dốc khi đứng dậy
            voiceAudioSource.volume = 1.0f;
            if (playerStandUpVoiceClip != null)
            {
                voiceAudioSource.clip = playerStandUpVoiceClip;
                voiceAudioSource.Play();
            }

            SetSubtitle("[Bạn]: \"*Hít một hơi thật sâu* I'm... I'm still alive. I have to survive on this island, whatever it takes!\"\n<size=17><color=#D1D5DB>(Mình... mình vẫn còn sống! Phải tìm cách sinh tồn trên hòn đảo này thôi!)</color></size>");

            // Mở mắt từ từ (Fade Black 1.0 -> 0.0)
            StartCoroutine(FadeBlack(1f, 0f, 2.5f));

            // Vị trí nằm úp/nghiêng ban đầu trên nền đất
            Vector3 lyingCamPos = new Vector3(originalCamLocalPos.x, 0.16f, originalCamLocalPos.z + 0.35f);
            Quaternion lyingCamRot = Quaternion.Euler(-5f, 8f, 32f); // Áp má xuống cỏ, đầu nghiêng 32 độ

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (cam != null && hasSavedCamTransform)
                {
                    Vector3 currentPos;
                    Quaternion currentRot;

                    if (t < 0.22f)
                    {
                        // ── GIAI ĐOẠN 1 (0% – 22%): CHỚP MẮT & NGỬNG ĐẦU DẬY KHỎI MẶT ĐẤT ──
                        // Đầu hơi nhấc lên 0.32m, góc nghiêng giảm từ 32° xuống 14°
                        float p = Mathf.SmoothStep(0f, 1f, t / 0.22f);
                        currentPos = Vector3.Lerp(lyingCamPos, new Vector3(originalCamLocalPos.x, 0.32f, originalCamLocalPos.z + 0.20f), p);
                        currentRot = Quaternion.Slerp(lyingCamRot, Quaternion.Euler(4f, 12f, 14f), p);
                    }
                    else if (t < 0.55f)
                    {
                        // ── GIAI ĐOẠN 2 (22% – 55%): CHỐNG HAI TAY XUỐNG ĐẤT, QUỲ NÂNG NGƯỜI LÊN ──
                        // Khi dồn trọng lượng lên hai tay: camera hơi chùng nhẹ xuống 3.5cm rồi rướn người lên tầm quỳ (Y ~ 0.78m)
                        float p = Mathf.SmoothStep(0f, 1f, (t - 0.22f) / 0.33f);
                        float dipY = Mathf.Sin(p * Mathf.PI) * -0.035f;
                        // Độ run rẩy cơ bắp do kiệt sức (muscle tremor)
                        float tremble = Mathf.Sin(elapsed * 28f) * 0.006f * (1f - p * 0.5f);

                        Vector3 kneelPos = new Vector3(originalCamLocalPos.x + tremble, 0.78f + dipY, originalCamLocalPos.z + 0.08f);
                        currentPos = Vector3.Lerp(new Vector3(originalCamLocalPos.x, 0.32f, originalCamLocalPos.z + 0.20f), kneelPos, p);

                        // Đầu ngước lên nhìn cảnh xác máy bay bốc cháy hoang tàn
                        Quaternion kneelRot = Quaternion.Euler(8f, -4f, 5f);
                        currentRot = Quaternion.Slerp(Quaternion.Euler(4f, 12f, 14f), kneelRot, p);
                    }
                    else if (t < 0.85f)
                    {
                        // ── GIAI ĐOẠN 3 (55% – 85%): DỒN LỰC ĐỨNG DẬY BẰNG HAI CHÂN & BƯỚC LOẠNG CHOẠNG ──
                        // Người bật dậy lên độ cao đứng đầy đủ (Y từ 0.78m lên ~1.65m)
                        float p = Mathf.SmoothStep(0f, 1f, (t - 0.55f) / 0.30f);

                        // Độ lắc lư thăng bằng khi đứng lên: hơi cúi mặt kiểm tra bước chân (Pitch -4.5°), lắc sang hai bên (Roll 2.8°)
                        float balancePitch = Mathf.Sin(p * Mathf.PI) * -4.5f;
                        float balanceRoll  = Mathf.Cos(p * Mathf.PI * 2f) * 2.8f * (1f - p);
                        float heaveBob     = Mathf.Sin(p * Mathf.PI * 1.5f) * 0.04f;

                        Vector3 standPos = Vector3.Lerp(new Vector3(originalCamLocalPos.x, 0.78f, originalCamLocalPos.z + 0.08f), originalCamLocalPos, p);
                        currentPos = standPos + new Vector3(0, heaveBob, 0);

                        Quaternion targetRot = Quaternion.Euler(originalCamLocalRot.eulerAngles.x + balancePitch, originalCamLocalRot.eulerAngles.y, balanceRoll);
                        currentRot = Quaternion.Slerp(Quaternion.Euler(8f, -4f, 5f), targetRot, p);
                    }
                    else
                    {
                        // ── GIAI ĐOẠN 4 (85% – 100%): THỞ PHÀO NHẸ NHÕM, LẤY LẠI THĂNG BẰNG HOÀN TOÀN ──
                        // Lồng ngực phập phồng hít thở (bob nhẹ 1.5cm) và tầm nhìn cân bằng tuyệt đối
                        float p = Mathf.SmoothStep(0f, 1f, (t - 0.85f) / 0.15f);
                        float breathBob = Mathf.Sin((elapsed - 4.5f) * 4f) * 0.015f * (1f - p);

                        currentPos = Vector3.Lerp(cam.transform.localPosition, originalCamLocalPos + new Vector3(0, breathBob, 0), p);
                        currentRot = Quaternion.Slerp(cam.transform.localRotation, originalCamLocalRot, p);
                    }

                    cam.transform.localPosition = currentPos;
                    cam.transform.localRotation = currentRot;
                }

                yield return null;
            }

            if (cam != null && hasSavedCamTransform)
            {
                cam.transform.localPosition = originalCamLocalPos;
                cam.transform.localRotation = originalCamLocalRot;
            }

            // Bật lại mô hình nhân vật cho gameplay
            SetPlayerRenderersVisible(true);

            // Bật lại toàn bộ giao diện Gameplay HUD
            SetGameplayUIVisible(true);

            // Kích hoạt lại Player Controller và Character Controller để điều khiển di chuyển
            if (playerController != null)
            {
                CharacterController cc = playerController.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = true;
                playerController.enabled = true;
            }
            if (playerWeapon != null) playerWeapon.enabled = true;

            yield return new WaitForSeconds(2.0f);
            ClearSubtitle();
        }

        // ──────────────────────────────────────────────
        // UI & CANVASES HELPER
        // ──────────────────────────────────────────────

        private void EnsureCutsceneUI()
        {
            // Tạo hoặc tìm Canvas riêng biệt chuyên dụng cho Cutscene với SortingOrder cao nhất (9999)
            GameObject canvasObj = GameObject.Find("Cutscene_Overlay_Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Cutscene_Overlay_Canvas");
                cutsceneCanvas = canvasObj.AddComponent<Canvas>();
                cutsceneCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                cutsceneCanvas.sortingOrder = 9999;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                cutsceneCanvas = canvasObj.GetComponent<Canvas>();
            }

            // Đảm bảo blackScreenUI tồn tại trên Cutscene Canvas
            if (blackScreenUI == null)
            {
                GameObject blackObj = new GameObject("Cutscene_BlackScreen");
                blackObj.transform.SetParent(cutsceneCanvas.transform, false);
                RectTransform rt = blackObj.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                blackScreenUI = blackObj.AddComponent<Image>();
                blackScreenUI.color = Color.black;
            }
            else if (blackScreenUI.transform.parent != cutsceneCanvas.transform)
            {
                blackScreenUI.transform.SetParent(cutsceneCanvas.transform, false);
            }

            // Đảm bảo whiteFlashUI tồn tại trên Cutscene Canvas
            if (whiteFlashUI == null)
            {
                GameObject whiteObj = new GameObject("Cutscene_WhiteFlash");
                whiteObj.transform.SetParent(cutsceneCanvas.transform, false);
                RectTransform rt = whiteObj.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                whiteFlashUI = whiteObj.AddComponent<Image>();
                whiteFlashUI.color = new Color(1, 1, 1, 0);
                whiteFlashUI.gameObject.SetActive(false);
            }
            else if (whiteFlashUI.transform.parent != cutsceneCanvas.transform)
            {
                whiteFlashUI.transform.SetParent(cutsceneCanvas.transform, false);
            }

            // Đảm bảo subtitleText tồn tại trên Cutscene Canvas
            if (subtitleText == null)
            {
                GameObject subObj = new GameObject("Cutscene_Subtitle_UI");
                subObj.transform.SetParent(cutsceneCanvas.transform, false);
                RectTransform rt = subObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.08f, 0.05f);
                rt.anchorMax = new Vector2(0.92f, 0.22f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                subtitleText = subObj.AddComponent<Text>();
                subtitleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                subtitleText.fontSize = 22;
                subtitleText.alignment = TextAnchor.MiddleCenter;
                subtitleText.color = new Color(1f, 0.95f, 0.8f, 1f);
                subtitleText.text = "";

                Outline outline = subObj.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }
            else if (subtitleText.transform.parent != cutsceneCanvas.transform)
            {
                subtitleText.transform.SetParent(cutsceneCanvas.transform, false);
            }

            // Đảm bảo gameTitleGroup (nếu có)
            if (gameTitleGroup != null && gameTitleGroup.transform.parent != cutsceneCanvas.transform)
            {
                gameTitleGroup.transform.SetParent(cutsceneCanvas.transform, false);
            }
        }

        private void SetGameplayUIVisible(bool visible)
        {
            if (!visible)
            {
                disabledGameplayCanvases.Clear();
                Canvas[] allCanvases = FindObjectsOfType<Canvas>();
                foreach (Canvas c in allCanvases)
                {
                    if (c == null) continue;
                    if (cutsceneCanvas != null && c == cutsceneCanvas) continue;
                    if (c.gameObject.name == "Cutscene_Overlay_Canvas") continue;

                    if (c.gameObject.activeSelf)
                    {
                        c.gameObject.SetActive(false);
                        disabledGameplayCanvases.Add(c.gameObject);
                    }
                }
            }
            else
            {
                // Bật lại các Canvas đã bị ẩn trong suốt trailer
                foreach (GameObject go in disabledGameplayCanvases)
                {
                    if (go != null) go.SetActive(true);
                }
                disabledGameplayCanvases.Clear();

                // Đảm bảo Canvas chính chứa thanh chỉ số sinh tồn (Stats Panel / PlayerUI) luôn được bật khi vào game
                Canvas[] allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
                foreach (Canvas c in allCanvases)
                {
                    if (c == null) continue;
                    if (cutsceneCanvas != null && c == cutsceneCanvas) continue;
                    if (c.gameObject.name == "Cutscene_Overlay_Canvas") continue;
                    if (c.hideFlags != HideFlags.None) continue;
                    if (c.gameObject.scene.name == null) continue; // Chỉ thao tác trên object trong Scene đang mở

                    if (!c.gameObject.activeSelf && (c.name == "Canvas" || c.GetComponentInChildren<PlayerUI>(true) != null))
                    {
                        c.gameObject.SetActive(true);
                    }
                }
            }
        }

        private void SetPlayerRenderersVisible(bool visible)
        {
            if (playerController == null) return;
            if (cachedPlayerRenderers.Count == 0)
            {
                Renderer[] all = playerController.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in all)
                {
                    if ((r is MeshRenderer || r is SkinnedMeshRenderer) && r.enabled)
                    {
                        cachedPlayerRenderers.Add(r);
                    }
                }
            }

            foreach (Renderer r in cachedPlayerRenderers)
            {
                if (r != null) r.enabled = visible;
            }
        }

        private IEnumerator DisplayCaptainSubtitles(float totalDuration)
        {
            if (subtitleText == null) yield break;

            SetSubtitle("[Captain]: \"Welcome aboard our C-400. Cruising altitude 35,000 feet, sit back and enjoy your flight.\"\n<size=17><color=#D1D5DB>(Chào mừng mọi người, đây là cơ trưởng chuyến bay C-400. Độ cao 35.000 feet, thời tiết đẹp, chúc mọi người chuyến bay an toàn.)</color></size>");
            yield return new WaitForSeconds(12f);
            ClearSubtitle();
            yield return new WaitForSeconds(3f);

            SetSubtitle("[Captain]: \"Uh, folks, we are encountering a slight patch of rough air ahead. Please ensure your seatbelts are securely fastened.\"\n<size=17><color=#D1D5DB>(Xin mọi người chú ý, phía trước có vùng nhiễu động. Xin vui lòng thắt chặt dây an toàn tại vị trí của mình.)</color></size>");
            yield return new WaitForSeconds(12f);
            ClearSubtitle();
            yield return new WaitForSeconds(3f);

            SetSubtitle("[Captain]: \"Everyone brace yourselves! Wait... what is that on radar?! We're losing altitude, hold on!\"\n<size=17><color=#F87171>(Tất cả bám chắc! Khoan đã... radar báo cái gì thế này?! Mất độ cao rồi, bám chắc vào!)</color></size>");
            yield return new WaitForSeconds(14.5f);
            ClearSubtitle();
        }

        private void SetSubtitle(string content)
        {
            if (subtitleText != null)
            {
                subtitleText.gameObject.SetActive(true);
                subtitleText.text = content;
            }
        }

        private void ClearSubtitle()
        {
            if (subtitleText != null)
            {
                subtitleText.text = "";
                subtitleText.gameObject.SetActive(false);
            }
        }
    }
}

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
        public bool enableCabinFreeLook = true;

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

        private void Start()
        {
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
                    originalCamLocalPos = cam.transform.localPosition;
                    originalCamLocalRot = cam.transform.localRotation;
                    hasSavedCamTransform = true;
                }
            }

            // Khoá người chơi ngay
            if (playerController != null) playerController.enabled = false;
            if (playerWeapon     != null) playerWeapon.enabled     = false;

            // Đảm bảo người chơi luôn ngồi chính xác vào ghế 12A trong cabin máy bay khi bắt đầu cutscene
            SeatPlayerInAirplane();

            // Khởi tạo Canvas Cutscene độc lập & cô lập giao diện
            EnsureCutsceneUI();

            // Ẩn toàn bộ Gameplay UI (thanh máu, thể lực, túi đồ, v.v.) trong suốt cutscene
            SetGameplayUIVisible(false);

            // Ẩn mô hình nhân vật (mũ cối, giáp) để camera góc nhìn thứ nhất không bị che khuất
            SetPlayerRenderersVisible(false);

            // Màn hình đen hoàn toàn
            SetBlackAlpha(1f);
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
            // ── PHASE 1 | 0.0 – 2.0s | FADE IN CABIN ──────────────
            // Tiếng động cơ bật lên ngay lúc còn màn đen → immersive
            PlayLoop(airplaneFlightClip);
            yield return StartCoroutine(FadeBlack(1f, 0f, fadeInDuration));

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

            // Rung cực mạnh, dập dềnh hoảng loạn (người chơi vẫn có thể lia chuột hoảng loạn nhìn quanh)
            if (cameraShake != null)
                cameraShake.StartShake(alarmDuration - 0.5f, 0.55f, 3.0f);

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

            // Tắt cabin máy bay & dịch chuyển nhân vật xuống rừng
            if (airplaneCabin != null)
                airplaneCabin.SetActive(false);

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

        private void SeatPlayerInAirplane()
        {
            if (playerController == null)
                playerController = FindObjectOfType<HorrorGame.Player.PlayerController>();

            if (airplaneCabin == null)
            {
                airplaneCabin = GameObject.Find("Realistic_Airplane_Cabin");
                if (airplaneCabin == null) airplaneCabin = GameObject.Find("Temp_AirplaneCabin");
            }

            if (airplaneCabin != null)
            {
                airplaneCabin.SetActive(true);
                // Đảm bảo cửa sổ không bị bít kín và có ngoại cảnh cánh + động cơ + đèn chớp + mây trôi
                EnsureCabinExteriorAndWindowTransparency(airplaneCabin);
            }

            // Tìm vị trí ngồi chuẩn trên Ghế 12A (Ghế sát cửa sổ mạn trái)
            Vector3 targetSeatPos = Vector3.zero;
            bool foundSeat = false;

            if (airplaneCabin != null)
            {
                // 1. Tìm dãy ghế hàng 12 bên trái: TripleSeat_L_Row_12 -> Seat_A
                Transform row12 = airplaneCabin.transform.Find("TripleSeat_L_Row_12");
                if (row12 != null)
                {
                    Transform seatA = row12.Find("Seat_A");
                    if (seatA != null)
                    {
                        targetSeatPos = seatA.position + Vector3.up * 0.12f;
                        foundSeat = true;
                    }
                }

                // 2. Tìm điểm neo PlayerCutsceneSeat nếu đã được đặt sát cửa sổ (-X)
                if (!foundSeat)
                {
                    Transform anchor = airplaneCabin.transform.Find("PlayerCutsceneSeat");
                    if (anchor != null && anchor.localPosition.x < -1.2f)
                    {
                        targetSeatPos = anchor.position;
                        foundSeat = true;
                    }
                }

                // 3. Tìm dãy ghế hàng 11 bên trái
                if (!foundSeat)
                {
                    Transform row11 = airplaneCabin.transform.Find("TripleSeat_L_Row_11");
                    if (row11 != null)
                    {
                        Transform seatA = row11.Find("Seat_A");
                        if (seatA != null)
                        {
                            targetSeatPos = seatA.position + Vector3.up * 0.12f;
                            foundSeat = true;
                        }
                    }
                }

                // 4. Fallback tọa độ chuẩn tuyệt đối của đệm ghế 12A trong cabin: (-1.62m, 0.15m, -2.75m)
                if (!foundSeat)
                {
                    targetSeatPos = airplaneCabin.transform.TransformPoint(new Vector3(-1.62f, 0.15f, -2.75f));
                    foundSeat = true;
                }
            }

            if (playerController != null && foundSeat)
            {
                // Tắt CharacterController tạm thời để teleport không bị va chạm ghi đè vị trí
                CharacterController cc = playerController.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                Camera cam = playerController.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    // ═══════════════════════════════════════════════════════════
                    // TÍNH TOÁN VỊ TRÍ CHÍNH XÁC ĐỂ CAMERA ĐÚNG TẦM MẮT CỬA SỔ
                    // ═══════════════════════════════════════════════════════════
                    // Camera nằm ở đâu đó trong hierarchy (thường ~1.6m trên player root)
                    // Ta cần đặt player root sao cho camera = tầm mắt hành khách ngồi ghế
                    //
                    // Tầm mắt chuẩn = bậu cửa sổ + nửa chiều cao cửa sổ = 0.82 + 0.78/2 = 1.21m
                    // (đo từ sàn cabin)

                    float eyeHeightFromFloor = 1.21f; // Ngang tâm cửa sổ bầu dục

                    // Offset Y giữa camera và player root (VD: camera ở Y=1.6 thì offset = 1.6)
                    float camOffsetY = cam.transform.position.y - playerController.transform.position.y;
                    if (camOffsetY < 0.1f) camOffsetY = 1.6f; // Fallback an toàn

                    // Vị trí player root cần đặt để camera nằm đúng eyeHeight
                    // cabinFloorWorldY = airplaneCabin root Y + 0.05 (sàn hơi nhô lên)
                    float cabinFloorY = airplaneCabin != null ? airplaneCabin.transform.position.y + 0.05f : targetSeatPos.y;
                    float targetPlayerY = cabinFloorY + eyeHeightFromFloor - camOffsetY;

                    // Vị trí X,Z lấy từ ghế (targetSeatPos đã đúng X,Z)
                    Vector3 finalPos = new Vector3(targetSeatPos.x, targetPlayerY, targetSeatPos.z);
                    playerController.transform.position = finalPos;
                    playerController.transform.rotation = Quaternion.identity;

                    cutsceneCamera = cam.transform;

                    // Góc nhìn ban đầu: Quay sang trái ra cửa sổ
                    // -55° đủ thấy cửa sổ nằm giữa tầm nhìn + cánh máy bay
                    cabinLookYaw   = -55f;
                    cabinLookPitch = 0f; // Nhìn ngang tầm mắt — thẳng ra cửa sổ
                    cam.transform.localRotation = Quaternion.Euler(cabinLookPitch, cabinLookYaw, 0f);
                    cam.fieldOfView = 60f;

                    if (enableCabinFreeLook)
                    {
                        isCabinFreeLookActive = true;
                        if (cameraShake != null)
                            cameraShake.preventRotationOverride = true;

                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
            }
        }

        /// <summary>
        /// Tự động làm sạch các vách che cửa sổ cũ và khởi tạo vách tường cửa sổ bầu dục liền khối 100% chuẩn hàng không,
        /// kèm theo ngoại cảnh (cánh máy bay, động cơ CFM56 xoay tít, đèn chớp hàng không, mây trôi) ngay trong Runtime.
        /// </summary>
        private void EnsureCabinExteriorAndWindowTransparency(GameObject cabin)
        {
            if (cabin == null) return;

            // 1. Tắt các khối che khuất cũ (nếu có)
            Transform[] allTrans = cabin.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allTrans)
            {
                if (t == null) continue;
                if (t.name.Contains("WindowBelt") || t.name == "StratosphereSky")
                {
                    t.gameObject.SetActive(false);
                }
            }

            // Nạp hoặc khởi tạo các vật liệu PBR tiêu chuẩn
            Shader stdShader = Shader.Find("Standard");
            Material matWall = Resources.Load<Material>("Cabin/M_Cabin_Wall");
#if UNITY_EDITOR
            if (matWall == null) matWall = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Flooded_Grounds/Materials/Cabin/M_Cabin_Wall.mat");
#endif
            if (matWall == null)
            {
                matWall = new Material(stdShader);
                matWall.color = new Color(0.92f, 0.92f, 0.94f);
                matWall.SetFloat("_Glossiness", 0.45f);
            }

            Material matBezel = Resources.Load<Material>("Cabin/M_Cabin_WindowFrame");
#if UNITY_EDITOR
            if (matBezel == null) matBezel = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Flooded_Grounds/Materials/Cabin/M_Cabin_WindowFrame.mat");
#endif
            if (matBezel == null)
            {
                matBezel = new Material(stdShader);
                matBezel.color = new Color(0.96f, 0.96f, 0.98f);
                matBezel.SetFloat("_Glossiness", 0.70f);
            }

            Material matGlass = Resources.Load<Material>("Cabin/M_Cabin_WindowGlass");
#if UNITY_EDITOR
            if (matGlass == null) matGlass = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Flooded_Grounds/Materials/Cabin/M_Cabin_WindowGlass.mat");
#endif
            if (matGlass == null)
            {
                matGlass = new Material(stdShader);
                matGlass.SetFloat("_Mode", 3); // Transparent
                matGlass.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                matGlass.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                matGlass.SetInt("_ZWrite", 0);
                matGlass.EnableKeyword("_ALPHABLEND_ON");
                matGlass.renderQueue = 3000;
                matGlass.color = new Color(0.85f, 0.92f, 0.98f, 0.25f);
                matGlass.SetFloat("_Glossiness", 0.95f);
            }

            Material matShade = new Material(stdShader);
            matShade.color = new Color(0.88f, 0.88f, 0.90f);
            matShade.SetFloat("_Glossiness", 0.35f);

            // 2. KIỂM TRA VÀ TỰ ĐỘNG NÂNG CẤP VÁCH TƯỜNG TRÁI & PHẢI SANG CỬA SỔ BẦU DỤC THỰC TẾ
            // QUAN TRỌNG: Dùng CÙNG kích thước với Editor Builder (4.10m x 17.5m x 2.45m)
            // để tường runtime không bị lệch / tạo lớp thứ 2 che khuất cửa sổ!
            float cabinW = 4.10f;
            float cabinL = 17.5f;
            float cabinH = 2.45f;

            Transform wallLeft = cabin.transform.Find("Wall_Left");
            bool needRebuildLeft = (wallLeft == null) || (wallLeft.Find("Window_0") != null) || (wallLeft.Find("OvalBay_0") == null);
            if (needRebuildLeft)
            {
                if (wallLeft != null) Destroy(wallLeft.gameObject);
                BuildRuntimeSeamlessCurvedWall(cabin, true, cabinW, cabinL, cabinH, matWall, matBezel, matGlass, matShade);
            }

            Transform wallRight = cabin.transform.Find("Wall_Right");
            bool needRebuildRight = (wallRight == null) || (wallRight.Find("Window_0") != null) || (wallRight.Find("OvalBay_0") == null);
            if (needRebuildRight)
            {
                if (wallRight != null) Destroy(wallRight.gameObject);
                BuildRuntimeSeamlessCurvedWall(cabin, false, cabinW, cabinL, cabinH, matWall, matBezel, matGlass, matShade);
            }

            // 3. LUÔN LÀM MỚI VÀ ĐẢM BẢO NGOẠI CẢNH (CÁNH + ĐỘNG CƠ CFM56 + ĐÈN CHỚP + MÂY) Ở TẦM MẮT CHUẨN
            AirplaneCabinExterior[] oldExteriors = cabin.GetComponentsInChildren<AirplaneCabinExterior>(true);
            foreach (var oldExt in oldExteriors)
            {
                if (oldExt != null) Destroy(oldExt.gameObject);
            }
            BuildRuntimeExterior(cabin);
        }

        /// <summary>
        /// Dựng vách tường máy bay liền khối với 9 ô cửa sổ bầu dục chuẩn hàng không quốc tế (Airbus A320 / Boeing 737):
        /// - Mặt vách liền mạch ôm sát từng ô cửa, hoàn toàn KHÔNG CÓ lỗ thủng vuông thô kệch
        /// - Vành đúc nhựa vát sâu 3D (7.5cm) vào thân máy bay
        /// - Tấm kính mica bầu dục 2 lớp trong suốt sáng bóng
        /// - Tấm che nắng trượt (Sliding Sunshade) có gờ kéo tay ở nửa trên
        /// - Lỗ thông áp vi mô (Breather Pinhole) đặc trưng trên kính
        /// </summary>
        private static void BuildRuntimeSeamlessCurvedWall(GameObject cabin, bool isLeft, float width, float length, float height,
            Material matWall, Material matBezel, Material matGlass, Material matShade)
        {
            float halfW = width * 0.5f;
            float posX = isLeft ? -halfW : halfW;
            string wallName = isLeft ? "Wall_Left" : "Wall_Right";

            GameObject wall = new GameObject(wallName);
            wall.transform.SetParent(cabin.transform, false);

            // 1. Tấm ốp chân vách tường (từ sàn Y=0 đến bậu cửa sổ Y=0.82m)
            float lowerH = 0.82f;
            CreateRuntimeBox("LowerWall", wall.transform, new Vector3(posX, lowerH * 0.5f, 0), new Vector3(0.08f, lowerH, length), matWall);

            // 2. Vách tường phía trên cửa sổ (từ đỉnh bậu cửa sổ Y=1.60m lên trần Y=height)
            float bayH = 0.78f; // Chiều cao khoang cửa sổ từ 0.82m đến 1.60m
            float winCenterY = lowerH + bayH * 0.5f; // = 1.21m (ngang tầm mắt người ngồi)
            float upperH = height - (lowerH + bayH); // = 2.45 - 1.60 = 0.85m
            float upperCenterY = lowerH + bayH + upperH * 0.5f;
            CreateRuntimeBox("UpperWall", wall.transform, new Vector3(posX, upperCenterY, 0), new Vector3(0.08f, upperH, length), matWall);

            // 3. Vách đầu và vách cuối ngoài phạm vi 9 cửa sổ
            float bayW = 1.35f;
            float firstBayZ = -5.4f - bayW * 0.5f;
            float aftZ = -length * 0.5f;
            CreateRuntimeBox("WallPillar_Aft", wall.transform, new Vector3(posX, winCenterY, (aftZ + firstBayZ) * 0.5f), new Vector3(0.08f, bayH, Mathf.Abs(firstBayZ - aftZ)), matWall);

            float lastBayZ = 5.4f + bayW * 0.5f;
            float fwdZ = length * 0.5f;
            CreateRuntimeBox("WallPillar_Fwd", wall.transform, new Vector3(posX, winCenterY, (fwdZ + lastBayZ) * 0.5f), new Vector3(0.08f, bayH, Mathf.Abs(fwdZ - lastBayZ)), matWall);

            // 4. Dựng 9 khoang vách cửa sổ bầu dục liền khối (Seamless Oval Window Bays)
            for (int w = 0; w < 9; w++)
            {
                float z = -5.4f + (w * bayW);
                Vector3 bayPos = new Vector3(posX, winCenterY, z);
                CreateRuntimeProceduralWindowBay("OvalBay_" + w, wall.transform, bayPos, isLeft, bayW, bayH, matWall, matBezel, matGlass, matShade);
            }
        }

        /// <summary>
        /// Tạo một khoang cửa sổ máy bay hoàn chỉnh với bề mặt tường liền mạch nối vào vành bầu dục,
        /// vành vát sâu 3D, kính mica và tấm che nắng chuẩn hàng không.
        /// </summary>
        private static GameObject CreateRuntimeProceduralWindowBay(string name, Transform parent, Vector3 localPos, bool isLeftSide,
            float bayWidth, float bayHeight, Material matWall, Material matBezel, Material matGlass, Material matShade)
        {
            GameObject bayObj = new GameObject(name);
            bayObj.transform.SetParent(parent, false);
            bayObj.transform.localPosition = localPos;

            int segments = 32;
            float semiW = 0.16f; // Bán kính ngang cửa sổ (Rộng 32cm)
            float semiH = 0.24f; // Bán kính dọc cửa sổ (Cao 48cm)
            float p = 3.2f;      // Hệ số siêu elip chuẩn hàng không thương mại
            float bevelDepth = 0.075f; // Độ vát sâu 7.5cm

            // Tính toán 32 đỉnh siêu elip
            Vector2[] ovalPts = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float c = Mathf.Cos(angle);
                float s = Mathf.Sin(angle);
                float oz = Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 2f / p) * semiW;
                float oy = Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), 2f / p) * semiH;
                ovalPts[i] = new Vector2(oz, oy);
            }

            // Chiếu tia từ tâm cửa sổ ra biên chữ nhật của khoang vách (bayWidth x bayHeight)
            Vector2[] rectPts = new Vector2[segments];
            float halfW = bayWidth * 0.5f;
            float halfH = bayHeight * 0.5f;
            for (int i = 0; i < segments; i++)
            {
                float oz = ovalPts[i].x;
                float oy = ovalPts[i].y;
                float tz = Mathf.Abs(oz) > 0.0001f ? (halfW / Mathf.Abs(oz)) : 1000f;
                float ty = Mathf.Abs(oy) > 0.0001f ? (halfH / Mathf.Abs(oy)) : 1000f;
                float t = Mathf.Min(tz, ty);
                rectPts[i] = new Vector2(oz * t, oy * t);
            }

            // ─────────────────────────────────────────────────────────────
            // A. MẶT VÁCH NỘI THẤT LIỀN KHỐI (SEAMLESS WALL PANEL WITH OVAL CUTOUT)
            // ─────────────────────────────────────────────────────────────
            Mesh wallMesh = new Mesh();
            wallMesh.name = "SeamlessWallMesh";

            Vector3[] wVerts = new Vector3[segments * 2];
            Vector3[] wNorms = new Vector3[segments * 2];
            Vector2[] wUVs   = new Vector2[segments * 2];
            int[] wTris      = new int[segments * 6 * 2]; // 2 mặt (Double-sided) để không bao giờ bị culling

            float wallSurfaceX = isLeftSide ? 0.04f : -0.04f; // Bề mặt trong cabin
            Vector3 inwardNorm = isLeftSide ? Vector3.right : Vector3.left;

            for (int i = 0; i < segments; i++)
            {
                // Vòng ngoài: mép chữ nhật tiếp giáp vách trên/dưới/cột
                wVerts[i] = new Vector3(wallSurfaceX, rectPts[i].y, rectPts[i].x);
                wNorms[i] = inwardNorm;
                wUVs[i]   = new Vector2((rectPts[i].x / bayWidth) + 0.5f, (rectPts[i].y / bayHeight) + 0.5f);

                // Vòng trong: mép ô cửa sổ bầu dục
                wVerts[i + segments] = new Vector3(wallSurfaceX, ovalPts[i].y, ovalPts[i].x);
                wNorms[i + segments] = inwardNorm;
                wUVs[i + segments]   = new Vector2((ovalPts[i].x / bayWidth) + 0.5f, (ovalPts[i].y / bayHeight) + 0.5f);
            }

            int tIdx = 0;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int r0 = i;
                int r1 = next;
                int o1 = next + segments;
                int o0 = i + segments;

                // Mặt hướng vào cabin
                wTris[tIdx++] = r0; wTris[tIdx++] = o1; wTris[tIdx++] = r1;
                wTris[tIdx++] = r0; wTris[tIdx++] = o0; wTris[tIdx++] = o1;

                // Mặt đối diện (Double-sided)
                wTris[tIdx++] = r0; wTris[tIdx++] = r1; wTris[tIdx++] = o1;
                wTris[tIdx++] = r0; wTris[tIdx++] = o1; wTris[tIdx++] = o0;
            }

            wallMesh.vertices = wVerts;
            wallMesh.normals = wNorms;
            wallMesh.uv = wUVs;
            wallMesh.triangles = wTris;

            GameObject wallPanel = new GameObject("SeamlessPanel");
            wallPanel.transform.SetParent(bayObj.transform, false);
            wallPanel.AddComponent<MeshFilter>().sharedMesh = wallMesh;
            wallPanel.AddComponent<MeshRenderer>().sharedMaterial = matWall;

            // ─────────────────────────────────────────────────────────────
            // B. VÀNH ĐÚC KHUÔN VÁT MÉP SÂU 3D (3D INWARD MOLDED BEZEL)
            // ─────────────────────────────────────────────────────────────
            Mesh bezelMesh = new Mesh();
            bezelMesh.name = "MoldedBezelMesh";

            Vector3[] bVerts = new Vector3[segments * 2];
            Vector3[] bNorms = new Vector3[segments * 2];
            Vector2[] bUVs   = new Vector2[segments * 2];
            int[] bTris      = new int[segments * 6 * 2]; // Double-sided

            float innerX = isLeftSide ? (wallSurfaceX - bevelDepth) : (wallSurfaceX + bevelDepth);

            for (int i = 0; i < segments; i++)
            {
                // Vành ngoài (sát mặt tường)
                bVerts[i] = new Vector3(wallSurfaceX, ovalPts[i].y, ovalPts[i].x);
                bNorms[i] = (inwardNorm + new Vector3(0, -ovalPts[i].y, -ovalPts[i].x) * 0.5f).normalized;
                bUVs[i]   = new Vector2((float)i / segments, 1f);

                // Vành trong (lõm sâu vào trong thân máy bay, thu hẹp nhẹ 10%)
                bVerts[i + segments] = new Vector3(innerX, ovalPts[i].y * 0.90f, ovalPts[i].x * 0.90f);
                bNorms[i + segments] = inwardNorm;
                bUVs[i + segments]   = new Vector2((float)i / segments, 0f);
            }

            tIdx = 0;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int b0 = i;
                int b1 = next;
                int in1 = next + segments;
                int in0 = i + segments;

                bTris[tIdx++] = b0; bTris[tIdx++] = in1; bTris[tIdx++] = b1;
                bTris[tIdx++] = b0; bTris[tIdx++] = in0; bTris[tIdx++] = in1;

                bTris[tIdx++] = b0; bTris[tIdx++] = b1;  bTris[tIdx++] = in1;
                bTris[tIdx++] = b0; bTris[tIdx++] = in1; bTris[tIdx++] = in0;
            }

            bezelMesh.vertices = bVerts;
            bezelMesh.normals = bNorms;
            bezelMesh.uv = bUVs;
            bezelMesh.triangles = bTris;

            GameObject bezelObj = new GameObject("MoldedBezel");
            bezelObj.transform.SetParent(bayObj.transform, false);
            bezelObj.AddComponent<MeshFilter>().sharedMesh = bezelMesh;
            bezelObj.AddComponent<MeshRenderer>().sharedMaterial = matBezel;

            // ─────────────────────────────────────────────────────────────
            // C. KÍNH MICA BẦU DỤC 2 LỚP TRONG SUỐT (ACRYLIC DUAL-PANE GLASS)
            // ─────────────────────────────────────────────────────────────
            Mesh glassMesh = new Mesh();
            glassMesh.name = "AcrylicGlassMesh";

            Vector3[] gVerts = new Vector3[segments + 1];
            Vector3[] gNorms = new Vector3[segments + 1];
            Vector2[] gUVs   = new Vector2[segments + 1];
            int[] gTris      = new int[segments * 3 * 2]; // Double-sided

            float glassX = isLeftSide ? (innerX - 0.005f) : (innerX + 0.005f);
            gVerts[0] = new Vector3(glassX, 0, 0); // Tâm đĩa kính
            gNorms[0] = inwardNorm;
            gUVs[0]   = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < segments; i++)
            {
                gVerts[i + 1] = new Vector3(glassX, ovalPts[i].y * 0.90f, ovalPts[i].x * 0.90f);
                gNorms[i + 1] = inwardNorm;
                gUVs[i + 1]   = new Vector2((ovalPts[i].x / semiW) * 0.5f + 0.5f, (ovalPts[i].y / semiH) * 0.5f + 0.5f);
            }

            tIdx = 0;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                // Mặt trước
                gTris[tIdx++] = 0; gTris[tIdx++] = next + 1; gTris[tIdx++] = i + 1;
                // Mặt sau
                gTris[tIdx++] = 0; gTris[tIdx++] = i + 1; gTris[tIdx++] = next + 1;
            }

            glassMesh.vertices = gVerts;
            glassMesh.normals = gNorms;
            glassMesh.uv = gUVs;
            glassMesh.triangles = gTris;

            GameObject glassObj = new GameObject("AcrylicGlass");
            glassObj.transform.SetParent(bayObj.transform, false);
            glassObj.AddComponent<MeshFilter>().sharedMesh = glassMesh;
            glassObj.AddComponent<MeshRenderer>().sharedMaterial = matGlass;

            // Lỗ thông áp vi mô (Breather Pinhole) ở đáy kính trong
            CreateRuntimeBox("BreatherPinhole", glassObj.transform,
                new Vector3(isLeftSide ? 0.002f : -0.002f, -semiH * 0.60f, 0),
                new Vector3(0.003f, 0.008f, 0.008f), matBezel);

            return bayObj;
        }

        private void BuildRuntimeExterior(GameObject cabin)
        {
            GameObject extObj = new GameObject("AirplaneExterior");
            extObj.transform.SetParent(cabin.transform, false);
            AirplaneCabinExterior extComp = extObj.AddComponent<AirplaneCabinExterior>();

            Shader stdShader = Shader.Find("Standard");

            // Nạp hoặc tạo Materials chuẩn PBR
            Material matWing = Resources.Load<Material>("Cabin/M_Plane_Wing");
#if UNITY_EDITOR
            if (matWing == null) matWing = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Flooded_Grounds/Materials/Cabin/M_Plane_Wing.mat");
#endif
            if (matWing == null)
            {
                matWing = new Material(stdShader);
                matWing.color = new Color(0.90f, 0.91f, 0.93f);
                matWing.SetFloat("_Glossiness", 0.75f);
            }

            Material matChrome = Resources.Load<Material>("Cabin/M_Plane_Chrome");
#if UNITY_EDITOR
            if (matChrome == null) matChrome = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Flooded_Grounds/Materials/Cabin/M_Plane_Chrome.mat");
#endif
            if (matChrome == null)
            {
                matChrome = new Material(stdShader);
                matChrome.color = new Color(0.96f, 0.96f, 0.98f);
                matChrome.SetFloat("_Metallic", 0.95f);
                matChrome.SetFloat("_Glossiness", 0.92f);
            }

            Material matEngine = Resources.Load<Material>("Cabin/M_Plane_EngineNacelle");
#if UNITY_EDITOR
            if (matEngine == null) matEngine = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Flooded_Grounds/Materials/Cabin/M_Plane_EngineNacelle.mat");
#endif
            if (matEngine == null) matEngine = matWing;

            Material matSpinner = Resources.Load<Material>("Cabin/M_Plane_SpinnerSpiral");
#if UNITY_EDITOR
            if (matSpinner == null) matSpinner = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Flooded_Grounds/Materials/Cabin/M_Plane_SpinnerSpiral.mat");
#endif
            if (matSpinner == null)
            {
                matSpinner = new Material(stdShader);
                matSpinner.color = new Color(0.12f, 0.12f, 0.14f);
                matSpinner.SetFloat("_Glossiness", 0.85f);
            }

            Material matCloud = Resources.Load<Material>("Cabin/M_Plane_CloudSoft");
#if UNITY_EDITOR
            if (matCloud == null) matCloud = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Flooded_Grounds/Materials/Cabin/M_Plane_CloudSoft.mat");
#endif
            if (matCloud == null)
            {
                matCloud = new Material(stdShader);
                matCloud.SetFloat("_Mode", 3);
                matCloud.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                matCloud.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                matCloud.SetInt("_ZWrite", 0);
                matCloud.EnableKeyword("_ALPHABLEND_ON");
                matCloud.renderQueue = 3000;
                matCloud.color = new Color(1f, 1f, 1f, 0.85f);
            }

            Material matNavRed = new Material(stdShader);
            matNavRed.color = Color.red;
            matNavRed.EnableKeyword("_EMISSION");
            matNavRed.SetColor("_EmissionColor", Color.red * 4f);

            Material matStrobe = new Material(stdShader);
            matStrobe.color = Color.white;
            matStrobe.EnableKeyword("_EMISSION");
            matStrobe.SetColor("_EmissionColor", Color.white * 5f);

            // ═════════════════════════════════════════════════════════════════
            // 1. CÁNH TRÁI MÁY BAY (LEFT WING - GẮN NGAY TẦM NHÌN CỬA SỔ)
            // ═════════════════════════════════════════════════════════════════
            GameObject wing = new GameObject("Wing_Left");
            wing.transform.SetParent(extObj.transform, false);

            // Gốc cánh (Wing Root) vươn từ sát thân máy bay ra ngoài, cao Y = 0.88m (ngay dưới bậu cửa)
            GameObject wRoot = CreateRuntimeBox("WingRoot", wing.transform, new Vector3(-3.0f, 0.88f, -2.5f), new Vector3(2.4f, 0.22f, 4.4f), matWing);
            wRoot.transform.localRotation = Quaternion.Euler(0, -12f, 3.5f);

            // Thân cánh giữa (Mid Wing - nơi treo pylon động cơ)
            GameObject wMid = CreateRuntimeBox("WingMid", wing.transform, new Vector3(-5.5f, 1.05f, -3.5f), new Vector3(3.2f, 0.18f, 3.4f), matWing);
            wMid.transform.localRotation = Quaternion.Euler(0, -15f, 4.0f);

            // Đầu cánh ngoài (Outer Wing)
            GameObject wOuter = CreateRuntimeBox("WingOuter", wing.transform, new Vector3(-8.8f, 1.22f, -4.6f), new Vector3(3.5f, 0.15f, 2.4f), matWing);
            wOuter.transform.localRotation = Quaternion.Euler(0, -19f, 4.5f);

            // Mép trước cánh mạ Crom sáng loáng (Leading Edge Chrome Slat)
            GameObject slat = CreateRuntimeBox("LeadingEdge_Chrome", wing.transform, new Vector3(-5.8f, 1.02f, -1.8f), new Vector3(8.5f, 0.10f, 0.25f), matChrome);
            slat.transform.localRotation = Quaternion.Euler(0, -15.5f, 4.0f);

            // Cánh nhỏ Sharklet cong vút lên trời ở đầu cánh
            GameObject winglet = CreateRuntimeBox("Sharklet_Winglet", wing.transform, new Vector3(-10.6f, 1.85f, -5.2f), new Vector3(0.12f, 1.40f, 0.90f), matWing);
            winglet.transform.localRotation = Quaternion.Euler(0, -21f, 75f);

            // ═════════════════════════════════════════════════════════════════
            // 2. ĐỘNG CƠ PHẢN LỰC CFM56 (JET ENGINE - TO LỚN, RÕ NÉT NGOÀI CỬA SỔ)
            // ═════════════════════════════════════════════════════════════════
            // Đặt tại X = -3.6m, Y = 0.62m, Z = -1.6m -> Ngồi ở ghế 12A nhìn xiên ra là THẤY TRỌN VẸN!
            GameObject engineObj = new GameObject("JetEngine_Left");
            engineObj.transform.SetParent(extObj.transform, false);
            engineObj.transform.localPosition = new Vector3(-3.6f, 0.62f, -1.6f);
            engineObj.transform.localRotation = Quaternion.Euler(0, -2.0f, 0);

            // Trụ treo pylon gắn vào cánh
            CreateRuntimeBox("Pylon", engineObj.transform, new Vector3(0, 0.48f, 0), new Vector3(0.20f, 0.42f, 2.0f), matWing);

            // Thân vỏ động cơ (Nacelle Cowling) - Đỉnh đạt Y = 0.62 + 0.72 = 1.34m (ngang tầm mắt!)
            CreateRuntimeBox("NacelleBody", engineObj.transform, new Vector3(0, 0, 0), new Vector3(1.45f, 1.45f, 2.4f), matEngine);

            // Vành miệng hút gió mạ Crom sáng chói (Chrome Intake Lip)
            CreateRuntimeBox("IntakeLip_Chrome", engineObj.transform, new Vector3(0, 0, 1.22f), new Vector3(1.48f, 1.48f, 0.14f), matChrome);

            // Lòng ống hút gió
            CreateRuntimeBox("IntakeDuct", engineObj.transform, new Vector3(0, 0, 0.75f), new Vector3(1.20f, 1.20f, 0.80f), matSpinner);

            // Cánh quạt turbine titan (Turbine Fan Blades)
            CreateRuntimeBox("TurbineFanBlades", engineObj.transform, new Vector3(0, 0, 0.40f), new Vector3(1.16f, 1.16f, 0.05f), matChrome);

            // Nón xoay Spinner mang hoa văn xoắn ốc (xoay tít 1200 RPM)
            GameObject spinner = CreateRuntimeBox("SpinnerBullet", engineObj.transform, new Vector3(0, 0, 0.46f), new Vector3(0.38f, 0.38f, 0.42f), matSpinner);
            extComp.engineSpinner = spinner.transform;

            // Ống xả phản lực phía sau
            CreateRuntimeBox("ExhaustNozzle", engineObj.transform, new Vector3(0, 0, -1.25f), new Vector3(1.10f, 1.10f, 0.25f), matChrome);

            // Đèn ánh lửa khi động cơ gặp sự cố
            GameObject engGlow = new GameObject("EngineGlowLight");
            engGlow.transform.SetParent(engineObj.transform, false);
            engGlow.transform.localPosition = new Vector3(0, 0, -1.0f);
            Light engGL = engGlow.AddComponent<Light>();
            engGL.type = LightType.Point;
            engGL.color = new Color(1f, 0.35f, 0f);
            engGL.range = 8.0f;
            engGL.intensity = 0f;
            extComp.engineGlowLight = engGL;

            // ═════════════════════════════════════════════════════════════════
            // 3. ĐÈN HÀNG KHÔNG ĐẦU CÁNH (AVIATION LIGHTS)
            // ═════════════════════════════════════════════════════════════════
            // Đèn định vị đỏ mạn trái (Port Nav Light - Red)
            GameObject navRedObj = CreateRuntimeBox("NavLight_Red", wing.transform, new Vector3(-10.6f, 1.30f, -4.6f), new Vector3(0.12f, 0.12f, 0.16f), matNavRed);
            Light navRedL = navRedObj.AddComponent<Light>();
            navRedL.type = LightType.Point;
            navRedL.color = Color.red;
            navRedL.range = 2.5f;
            navRedL.intensity = 2.5f;
            extComp.wingNavLight = navRedL;

            // Đèn chớp chống va chạm trắng (Anti-Collision Strobe)
            GameObject strobeObj = CreateRuntimeBox("StrobeLight_White", wing.transform, new Vector3(-10.6f, 1.78f, -5.3f), new Vector3(0.12f, 0.12f, 0.16f), matStrobe);
            Light strobeL = strobeObj.AddComponent<Light>();
            strobeL.type = LightType.Point;
            strobeL.color = Color.white;
            strobeL.range = 2.5f;
            strobeL.intensity = 0f;
            extComp.wingStrobeLight = strobeL;
            extComp.wingStrobeRenderer = strobeObj.GetComponent<Renderer>();

            // ═════════════════════════════════════════════════════════════════
            // 4. MÂY TRÔI DƯỚI CÁNH & ÁNH NẮNG RỰC RỠ CHIẾU VÀO CỬA SỔ
            // ═════════════════════════════════════════════════════════════════
            GameObject sunLightObj = new GameObject("WindowSunDirectional");
            sunLightObj.transform.SetParent(extObj.transform, false);
            sunLightObj.transform.localPosition = new Vector3(-15f, 18f, 0);
            sunLightObj.transform.localRotation = Quaternion.Euler(24f, -118f, 0);
            Light sunDirL = sunLightObj.AddComponent<Light>();
            sunDirL.type = LightType.Directional;
            sunDirL.color = new Color(1.0f, 0.97f, 0.90f);
            sunDirL.intensity = 1.25f;
            extComp.sunDirectionalLight = sunDirL;

            List<Transform> clouds = new List<Transform>();
            Vector3[] cloudCoords = new Vector3[]
            {
                new Vector3(-6.0f,  -0.5f, -24f),
                new Vector3(-10.0f, -0.2f, -8f),
                new Vector3(-5.0f,   0.2f,  10f),
                new Vector3(-12.0f, -0.4f,  28f),
                new Vector3(-4.5f,   0.0f,  -4f),
                new Vector3(-9.5f,   0.1f,  16f)
            };
            Vector3[] cloudSizes = new Vector3[]
            {
                new Vector3(18f, 0.05f, 24f),
                new Vector3(22f, 0.05f, 28f),
                new Vector3(18f, 0.05f, 22f),
                new Vector3(24f, 0.05f, 30f),
                new Vector3(12f, 0.05f, 16f),
                new Vector3(14f, 0.05f, 18f)
            };

            for (int c = 0; c < cloudCoords.Length; c++)
            {
                GameObject cloudLayer = CreateRuntimeBox("CloudLayer_" + c, extObj.transform, cloudCoords[c], cloudSizes[c], matCloud);
                clouds.Add(cloudLayer.transform);
            }

            extComp.cloudLayers = clouds.ToArray();
            extComp.cloudMoveSpeed = 48f;
            extComp.cloudStartZ = -40f;
            extComp.cloudResetZ = 40f;
        }

        private static GameObject CreateRuntimeBox(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            Collider col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            if (mat != null)
            {
                Renderer r = go.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = mat;
            }
            return go;
        }

        private void TeleportToForestLyingDown()
        {
            if (playerController == null || forestSpawnPoint == null) return;
            playerController.transform.position = forestSpawnPoint.position;
            playerController.transform.rotation = forestSpawnPoint.rotation;

            // Dọn sạch Zombie xung quanh khu vực xác máy bay để bảo vệ người chơi
            HorrorGame.Enemy.ZombieSpawner spawner = FindObjectOfType<HorrorGame.Enemy.ZombieSpawner>();
            if (spawner != null) spawner.ClearZombiesNearCrashSite();

            // Khôi phục đầy đủ 100% tất cả chỉ số sinh tồn khi tỉnh dậy
            var stats = playerController.GetComponent<PlayerStats>();
            if (stats == null) stats = playerController.GetComponentInChildren<PlayerStats>();
            if (stats != null)
            {
                stats.currentHealth  = stats.maxHealth;
                stats.currentStamina = stats.maxStamina;
                stats.currentHunger  = stats.maxHunger;
                stats.currentThirst  = stats.maxThirst;
                stats.currentSanity  = stats.maxSanity;
                stats.isInvincible   = true;
            }

            Camera cam = playerController.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                if (!hasSavedCamTransform)
                {
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
                    if (r is MeshRenderer || r is SkinnedMeshRenderer)
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

            // ── ĐOẠN 1 (0s – 15s) ──
            SetSubtitle("[Captain]: \"Good afternoon ladies and gentlemen, this is your captain speaking. Cruising altitude 35,000 feet, sit back and enjoy your flight.\"\n<size=17><color=#D1D5DB>(Kính chào quý hành khách, đây là cơ trưởng. Độ cao 35.000 feet, thời tiết đẹp, chúc quý khách chuyến bay an toàn.)</color></size>");
            yield return new WaitForSeconds(12f);
            ClearSubtitle();
            yield return new WaitForSeconds(3f); // Nghỉ 3s tự nhiên như ngắt bộ đàm

            // ── ĐOẠN 2 (15s – 30s) ──
            SetSubtitle("[Captain]: \"Uh, folks, we are encountering a slight patch of rough air ahead. Please ensure your seatbelts are securely fastened.\"\n<size=17><color=#D1D5DB>(Chúng ta đang gặp vùng nhiễu động nhẹ phía trước. Xin quý khách vui lòng thắt chặt dây an toàn.)</color></size>");
            yield return new WaitForSeconds(12f);
            ClearSubtitle();
            yield return new WaitForSeconds(3f); // Nghỉ 3s

            // ── ĐOẠN 3 (30s – 45s) - Căng thẳng tột độ ──
            SetSubtitle("[Captain]: \"Flight attendants, take your seats immediately! Wait... what is that on radar?! We're losing altitude, hold on!\"\n<size=17><color=#F87171>(Tiếp viên ngồi xuống ngay! Khoan đã... radar báo cái gì thế này?! Mất độ cao rồi, bám chắc vào!)</color></size>");
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

using System.Collections;
using System.IO;
using UnityEngine;
using HorrorGame.Player;

namespace HorrorGame.Player
{
    /// <summary>
    /// Hệ thống Què chân & Gãy xương (Bone Fracture System) chuẩn Green Hell:
    /// - Khi nhảy hoặc ngã từ trên cao xuống (>6.5m): Bị mất máu nặng và gãy xương/què chân.
    /// - Hiệu ứng âm thanh gãy xương sắc bén (Bone Snap Crack) và va đập chấn thương.
    /// - Nhân vật bị đi cà nhấc (Limping walk): Đầu gối chùng xuống, camera nghiêng lảo đảo khi bước chân đau.
    /// - Không thể chạy nước rút (Sprint disabled) và tốc độ đi bộ giảm mạnh.
    /// - Biểu tượng icon Gãy Xương đặc trưng Green Hell hiển thị góc dưới bên trái màn hình cạnh các thanh chỉ số.
    /// - Phương pháp chữa trị:
    ///   1. Dùng Băng gạc (Bandage / Medkit) trong Inventory để nẹp cố định.
    ///   2. Nghỉ ngơi trong Lều / Nhà trú ẩn (ShelterRest) giúp vết thương mau lành.
    ///   3. Tự hồi phục dần theo thời gian (3 phút).
    /// </summary>
    public class PlayerFractureSystem : MonoBehaviour
    {
        public static PlayerFractureSystem Instance;

        [Header("── Trạng Thái Gãy Xương ──")]
        [Tooltip("Có đang bị gãy xương / què chân hay không")]
        public bool isFractured = false;

        [Tooltip("Thời gian tự lành vết thương (giây)")]
        public float maxFractureDuration = 180f; // 3 phút
        public float currentFractureTimer = 0f;

        [Tooltip("Hệ số giảm tốc độ đi bộ khi què chân (0.48 = còn 48% tốc độ)")]
        public float speedPenaltyMultiplier = 0.48f;

        [Tooltip("Tốc độ tụt tinh thần Sanity khi bị đau nhức nhối")]
        public float sanityDrainRate = 0.05f;

        [Header("── Tài Nguyên & Âm Thanh ──")]
        public Texture2D fractureIcon;
        public AudioClip boneCrackClip;
        public AudioClip fallImpactClip;
        public AudioClip limpPainGruntClip;
        public AudioClip agonyScreamClip; // Tiếng rên la thất thanh đau đớn khi gãy xương
        public AudioClip waterSplashClip; // Tiếng nước bắn tung tóe khi nhảy xuống nước
        private AudioClip playerCrawlVoiceClip; // Tiếng rên thở quằn quại

        // References nội bộ
        private PlayerStats playerStats;
        private PlayerController playerController;
        private AudioSource audioSource;

        // Visual Effects
        private float painFlashAlpha = 0f;
        private float nextPainNoticeTime = 0f;
        private float sprintWarningCooldown = 0f;

        // GUI Styles
        private GUIStyle titleStyle;
        private GUIStyle subStyle;
        private GUIStyle tipStyle;
        private GUIStyle shadowStyle;
        private Texture2D whitePixelTex;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(this);
                return;
            }
        }

        void Start()
        {
            playerStats = GetComponent<PlayerStats>();
            if (playerStats == null) playerStats = PlayerStats.Instance != null ? PlayerStats.Instance : FindObjectOfType<PlayerStats>();

            playerController = GetComponent<PlayerController>();

            SetupAudioSource();
            LoadAssets();
            CreatePixelTexture();
        }

        private void SetupAudioSource()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D âm thanh trực diện tai người chơi
            audioSource.priority = 0;       // Mức ưu tiên cao nhất trong Audio Engine, không bao giờ bị drop/trễ
            audioSource.bypassEffects = true;
            audioSource.bypassListenerEffects = true;
            audioSource.bypassReverbZones = true;
        }

        private void LoadAssets()
        {
            // 1. Nạp Icon gãy xương
            if (fractureIcon == null)
            {
                fractureIcon = Resources.Load<Texture2D>("Fracture/Icon_Fracture_Bone");
            }

            if (fractureIcon == null)
            {
                string diskPath = Path.Combine(Application.dataPath, "Flooded_Grounds/Resources/Fracture/Icon_Fracture_Bone.png");
                if (File.Exists(diskPath))
                {
                    byte[] data = File.ReadAllBytes(diskPath);
                    fractureIcon = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                    fractureIcon.LoadImage(data);
                }
            }

            // 2. Nạp âm thanh gãy xương, tiếng rên la và va đập từ Resources (Ưu tiên WAV 0ms latency)
            if (boneCrackClip == null)
                boneCrackClip = Resources.Load<AudioClip>("Fracture/bone_snap_crack");
            if (fallImpactClip == null)
                fallImpactClip = Resources.Load<AudioClip>("Fracture/fall_impact_heavy");
            if (limpPainGruntClip == null)
                limpPainGruntClip = Resources.Load<AudioClip>("Fracture/limp_pain_grunt");
            if (agonyScreamClip == null)
                agonyScreamClip = Resources.Load<AudioClip>("Fracture/fracture_scream_agony");
            if (waterSplashClip == null)
                waterSplashClip = Resources.Load<AudioClip>("Fracture/water_splash_fall");

#if UNITY_EDITOR
            // 1. ÂM THANH BẺ RẮC GÃY XƯƠNG (BONE CRACK / SNAP):
            AudioClip boneCrackUser = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/âm thanh khi gãy xương/âm thanh khi gãy xương.wav");
            if (boneCrackUser == null)
            {
                boneCrackUser = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/âm thanh khi gãy xương/âm thanh khi gãy xương.mp3");
            }
            if (boneCrackUser != null)
            {
                boneCrackClip = boneCrackUser;
            }

            // 2. TIẾNG RÊN LA THẤT THANH ĐAU ĐỚN (AGONY SCREAM):
            AudioClip screamUser = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/âm thanh khi gãy chân/âm thanh khi gãy chân.wav");
            if (screamUser == null)
            {
                screamUser = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/âm thanh khi gãy chân/âm thanh khi gãy chân.mp3");
            }
            if (screamUser != null)
            {
                agonyScreamClip = screamUser;
            }

            // Tự động gán chéo dự phòng nếu người dùng chỉ cung cấp 1 trong 2 file:
            if (boneCrackClip == null && agonyScreamClip != null) boneCrackClip = agonyScreamClip;
            if (agonyScreamClip == null && boneCrackClip != null) agonyScreamClip = boneCrackClip;

            if (boneCrackClip == null)
                boneCrackClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Resources/Fracture/bone_snap_crack.wav");
            if (fallImpactClip == null)
                fallImpactClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Resources/Fracture/fall_impact_heavy.wav");
            if (limpPainGruntClip == null)
                limpPainGruntClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Resources/Fracture/limp_pain_grunt.wav");
            if (agonyScreamClip == null)
                agonyScreamClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Resources/Fracture/fracture_scream_agony.wav");
            if (waterSplashClip == null)
                waterSplashClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Resources/Fracture/water_splash_fall.wav");
            if (fractureIcon == null)
                fractureIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Flooded_Grounds/Resources/Fracture/Icon_Fracture_Bone.png");
#endif

            // Nạp sẵn toàn bộ dữ liệu mẫu âm thanh vào RAM ngay khi khởi động (tránh hitch trễ giải nén lần đầu)
            if (boneCrackClip != null) boneCrackClip.LoadAudioData();
            if (fallImpactClip != null) fallImpactClip.LoadAudioData();
            if (agonyScreamClip != null) agonyScreamClip.LoadAudioData();
            if (limpPainGruntClip != null) limpPainGruntClip.LoadAudioData();
        }

        private void CreatePixelTexture()
        {
            if (whitePixelTex == null)
            {
                whitePixelTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                whitePixelTex.SetPixel(0, 0, Color.white);
                whitePixelTex.Apply();
            }
        }

        void Update()
        {
            if (painFlashAlpha > 0f)
            {
                painFlashAlpha -= Time.deltaTime * 0.9f;
                if (painFlashAlpha < 0f) painFlashAlpha = 0f;
            }

            if (sprintWarningCooldown > 0f) sprintWarningCooldown -= Time.deltaTime;

#if UNITY_EDITOR
            // Phím tắt F8 để test nhanh trạng thái Gãy Xương trong Editor
            if (Input.GetKeyDown(KeyCode.F8))
            {
                if (isFractured) CureFracture();
                else TriggerFracture(8.5f, 45f);
            }
#endif

            if (isFractured)
            {
                // Đếm ngược thời gian lành xương
                currentFractureTimer -= Time.deltaTime;
                if (currentFractureTimer <= 0f)
                {
                    CureFracture(true);
                    return;
                }

                // Cơn đau gãy xương âm ỉ làm hao mòn tinh thần (Sanity)
                if (playerStats != null)
                {
                    playerStats.DrainSanity(sanityDrainRate * Time.deltaTime);

                    if (Time.time >= nextPainNoticeTime)
                    {
                        nextPainNoticeTime = Time.time + Random.Range(18f, 26f);
                        playerStats.ShowStatusNotice("🦴 Chân gãy đau nhức buốt từng cơn... Cần Băng gạc cố định! (-Tinh thần)");
                    }
                }
            }
        }

        /// <summary>
        /// Kích hoạt trạng thái Gãy Xương & Què Chân do ngã từ trên cao
        /// </summary>
        public void TriggerFracture(float fallDistance, float damage)
        {
            isFractured = true;
            currentFractureTimer = maxFractureDuration;
            painFlashAlpha = 0.95f;

            // 1. ÂM THANH GÃY XƯƠNG / GÃY CHÂN CỦA BẠN (PHÁT NGAY LẬP TỨC 0ms DELAY)
            if (boneCrackClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(boneCrackClip, 1.0f);
            }
            else if (fallImpactClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(fallImpactClip, 1.0f);
            }

            // 2. TIẾNG RÊN LA THẤT THANH (chỉ phát thêm nếu là file riêng biệt khác với file gãy chân)
            if (agonyScreamClip != null && audioSource != null && agonyScreamClip != boneCrackClip)
            {
                audioSource.PlayOneShot(agonyScreamClip, 1.0f);
            }

            // Tụt sốc tinh thần vì chấn thương kinh hoàng
            if (playerStats != null)
            {
                playerStats.DrainSanity(18f);
                playerStats.ShowStatusNotice(string.Format("🦴 BỊ GÃY XƯƠNG CHÂN! (-{0:F0} HP) - Đi cà nhấc, không thể chạy!", damage));
            }

            Debug.LogWarning(string.Format("[PlayerFractureSystem] ⚠️ Người chơi bị gãy xương do ngã {0:F1}m! Nhận {1:F0} sát thương.", fallDistance, damage));
        }

        /// <summary>
        /// Phát âm thanh nhảy ùm xuống nước an toàn
        /// </summary>
        public void PlayWaterSplash()
        {
            if (waterSplashClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(waterSplashClip, 1.0f);
            }
        }

        /// <summary>
        /// Chữa lành gãy xương (Dùng Băng gạc, hộp sơ cứu hoặc hết thời gian)
        /// </summary>
        public void CureFracture(bool naturalHeal = false)
        {
            if (!isFractured) return;

            isFractured = false;
            currentFractureTimer = 0f;

            if (playerStats != null)
            {
                if (naturalHeal)
                {
                    playerStats.ShowStatusNotice("✨ Vết thương gãy xương đã tự liền da và lành lặn trở lại!");
                }
                else
                {
                    playerStats.ShowStatusNotice("🩹 Đã nẹp và băng bó cố định xương gãy! Chân đã lành lặn, có thể chạy bình thường!");
                    playerStats.RestoreSanity(12f);
                }
            }

            Debug.Log("[PlayerFractureSystem] ✨ Gãy xương đã được chữa khỏi!");
        }

        /// <summary>
        /// Rút ngắn thời gian gãy xương khi nghỉ ngơi trong Nơi Trú Ẩn (Shelter)
        /// </summary>
        public void TreatInShelter(float seconds)
        {
            if (!isFractured) return;

            currentFractureTimer -= seconds;
            if (currentFractureTimer <= 0f)
            {
                CureFracture(true);
            }
            else
            {
                if (playerStats != null)
                {
                    playerStats.ShowStatusNotice(string.Format("⛺ Nghỉ ngơi đã giúp vết gãy xương hồi phục nhanh hơn {0:F0} giây!", seconds));
                }
            }
        }

        /// <summary>
        /// Khi người chơi bước chân chịu lực lên chân gãy (hiệu ứng đi cà nhấc)
        /// </summary>
        public void OnLimpStep()
        {
            if (!isFractured) return;

            // Thỉnh thoảng phát tiếng thở rên đau đớn
            if (limpPainGruntClip != null && audioSource != null && Random.value < 0.35f)
            {
                audioSource.PlayOneShot(limpPainGruntClip, 0.45f);
            }

            // Nháy đỏ viền nhẹ
            painFlashAlpha = Mathf.Max(painFlashAlpha, 0.18f);
        }

        /// <summary>
        /// Cảnh báo khi người chơi cố bấm phím chạy nước rút khi đang gãy xương
        /// </summary>
        public void OnAttemptSprint()
        {
            if (!isFractured) return;

            if (sprintWarningCooldown <= 0f)
            {
                sprintWarningCooldown = 2.5f;
                if (limpPainGruntClip != null && audioSource != null)
                {
                    audioSource.PlayOneShot(limpPainGruntClip, 0.75f);
                }
                if (playerStats != null)
                {
                    playerStats.ShowStatusNotice("⚠️ Chân đang bị gãy xương! Đau buốt, không thể chạy nước rút!");
                }
                painFlashAlpha = Mathf.Max(painFlashAlpha, 0.35f);
            }
        }

        public void PlayPainGrunt()
        {
            if (limpPainGruntClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(limpPainGruntClip, 0.7f);
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // HIỂN THỊ ICON GÃY XƯƠNG GREEN HELL GÓC DƯỚI BÊN TRÁI MÀN HÌNH
        // ═════════════════════════════════════════════════════════════════════
        void OnGUI()
        {
            // 1. Hiệu ứng đỏ màn hình (Pain Vignette Flash) khi va đập chấn thương
            if (painFlashAlpha > 0.01f && whitePixelTex != null)
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(0.85f, 0.08f, 0.08f, painFlashAlpha * 0.45f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whitePixelTex);
                GUI.color = oldColor;
            }

            // 2. Nếu không bị gãy xương thì không vẽ Icon
            if (!isFractured) return;

            // Khởi tạo Styles
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle();
                titleStyle.fontSize = 14;
                titleStyle.fontStyle = FontStyle.Bold;
                titleStyle.normal.textColor = new Color(1f, 0.32f, 0.28f); // Đỏ cam cảnh báo chấn thương

                subStyle = new GUIStyle();
                subStyle.fontSize = 12;
                subStyle.fontStyle = FontStyle.Normal;
                subStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 0.95f);

                tipStyle = new GUIStyle();
                tipStyle.fontSize = 11;
                tipStyle.fontStyle = FontStyle.Italic;
                tipStyle.normal.textColor = new Color(0.45f, 0.88f, 1f, 0.95f); // Xanh cyan chỉ dẫn

                shadowStyle = new GUIStyle();
                shadowStyle.fontSize = 14;
                shadowStyle.fontStyle = FontStyle.Bold;
                shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.9f);
            }

            // ══════════════════════════════════════════════════════════════════
            // TỌA ĐỘ VÀ VỊ TRÍ ICON: GÓC DƯỚI BÊN TRÁI MÀN HÌNH (CẠNH THANH CHỈ SỐ)
            // ══════════════════════════════════════════════════════════════════
            float iconSize = 54f;
            float cardWidth = 260f;
            float cardHeight = 62f;

            // Căn góc dưới bên trái, đặt ngay phía trên các thanh chỉ số máu/thể lực
            float posX = 20f;
            float posY = Screen.height - 230f;

            // Hiệu ứng nhịp đập thình thịch của cơn đau (Heartbeat Pulse Animation)
            float pulse = 1f + 0.05f * Mathf.Sin(Time.time * 4.5f);
            float displayIconSize = iconSize * pulse;
            float iconOffset = (displayIconSize - iconSize) * 0.5f;

            // A. Khung nền đen mờ phong cách Green Hell (Dark Glassmorphism)
            if (whitePixelTex != null)
            {
                Color oldGUI = GUI.color;

                // Nền đen mờ 85%
                GUI.color = new Color(0.07f, 0.08f, 0.10f, 0.88f);
                GUI.DrawTexture(new Rect(posX, posY, cardWidth, cardHeight), whitePixelTex);

                // Viền cảnh báo màu đỏ/cam Green Hell rực sáng theo nhịp đập
                float borderAlpha = 0.55f + 0.35f * Mathf.Sin(Time.time * 4.5f);
                GUI.color = new Color(0.95f, 0.22f, 0.18f, borderAlpha);
                // Vẽ 4 cạnh viền
                float bw = 2f;
                GUI.DrawTexture(new Rect(posX, posY, cardWidth, bw), whitePixelTex); // Viền trên
                GUI.DrawTexture(new Rect(posX, posY + cardHeight - bw, cardWidth, bw), whitePixelTex); // Viền dưới
                GUI.DrawTexture(new Rect(posX, posY, bw, cardHeight), whitePixelTex); // Viền trái
                GUI.DrawTexture(new Rect(posX + cardWidth - bw, posY, bw, cardHeight), whitePixelTex); // Viền phải

                GUI.color = oldGUI;
            }

            // B. Vẽ Icon Gãy Xương đặc trưng Green Hell
            Rect iconRect = new Rect(posX + 6f - iconOffset, posY + 4f - iconOffset, displayIconSize, displayIconSize);
            if (fractureIcon != null)
            {
                GUI.DrawTexture(iconRect, fractureIcon, ScaleMode.ScaleToFit);
            }
            else
            {
                // Fallback nếu chưa load texture
                GUI.Label(iconRect, "🦴", titleStyle);
            }

            // C. Nhãn thông tin & Chỉ số hồi phục
            float textX = posX + iconSize + 12f;
            float textY = posY + 5f;

            // Tiêu đề: GÃY XƯƠNG
            string titleStr = "GÃY XƯƠNG (FRACTURE)";
            GUI.Label(new Rect(textX + 1, textY + 1, cardWidth - iconSize, 18), titleStr, shadowStyle);
            GUI.Label(new Rect(textX, textY, cardWidth - iconSize, 18), titleStr, titleStyle);

            // Trạng thái: Đi cà nhấc • Không thể chạy
            string statusStr = "Đi cà nhấc • Tốc độ -52%";
            GUI.Label(new Rect(textX, textY + 18, cardWidth - iconSize, 16), statusStr, subStyle);

            // Thời gian tự lành & Thanh tiến độ mini
            int minutes = Mathf.FloorToInt(currentFractureTimer / 60f);
            int seconds = Mathf.FloorToInt(currentFractureTimer % 60f);
            string timeStr = string.Format("Tự lành: {0:0}:{1:00} | [I] Dùng Băng gạc", minutes, seconds);
            GUI.Label(new Rect(textX, textY + 36, cardWidth - iconSize, 16), timeStr, tipStyle);

            // D. Thanh tiến độ nhỏ dưới đáy card
            if (whitePixelTex != null)
            {
                Color oldCol = GUI.color;
                float barW = cardWidth - 8f;
                float barH = 3f;
                float barX = posX + 4f;
                float barY = posY + cardHeight - 5f;

                // Nền thanh
                GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                GUI.DrawTexture(new Rect(barX, barY, barW, barH), whitePixelTex);

                // Phần tiến độ đã lành (từ đỏ chuyển sang xanh lá)
                float progress = Mathf.Clamp01(1f - (currentFractureTimer / maxFractureDuration));
                Color progressCol = Color.Lerp(new Color(0.95f, 0.25f, 0.2f), new Color(0.3f, 0.9f, 0.4f), progress);
                GUI.color = progressCol;
                GUI.DrawTexture(new Rect(barX, barY, barW * progress, barH), whitePixelTex);

                GUI.color = oldCol;
            }
        }
    }
}

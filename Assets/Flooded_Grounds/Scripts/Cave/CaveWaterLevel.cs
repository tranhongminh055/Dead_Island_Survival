using UnityEngine;
using System.Collections;
using HorrorGame.Player;
using HorrorGame.Environment;

namespace HorrorGame.Cave
{
    /// <summary>
    /// CaveWaterLevel — Hệ thống mực nước và thủy triều cho Hang 3 ("Hang Thủy Triều" - Tidal Grotto).
    /// - Đồng bộ với DayNightCycle:
    ///   + Ban ngày (06:00 - 18:00): Thủy triều rút (Low Tide), mực nước hạ thấp cho phép khám phá hang.
    ///   + Ban đêm (18:00 - 06:00): Thủy triều dâng (High Tide), nước dâng cao ngập phần lớn hang.
    /// - Cơ chế Bẫy Nước Dâng (Flood Trap):
    ///   + Khi người chơi lấy kho báu / dao lặn ở đáy hang, kích hoạt báo động.
    ///   + Mực nước dâng nhanh gấp 10 lần trong 60 giây.
    ///   + Người chơi phải bơi tháo chạy ra cửa hang trước khi bị ngộp thở chết đuối.
    /// </summary>
    public class CaveWaterLevel : MonoBehaviour
    {
        public static CaveWaterLevel Instance;

        [Header("── Tham Chiếu Water Plane ──")]
        [Tooltip("Transform của mặt nước trong hang (sẽ di chuyển theo trục Y)")]
        public Transform waterPlaneTransform;

        [Header("── Mực Nước (Y Axis) ──")]
        [Tooltip("Mực nước khi thủy triều rút (an toàn đi lại)")]
        public float lowTideY = 0f;

        [Tooltip("Mực nước khi thủy triều dâng cao ban đêm")]
        public float highTideY = 3.5f;

        [Tooltip("Mực nước khẩn cấp khi kích hoạt Flood Trap (ngập toàn hang)")]
        public float floodTrapY = 6.0f;

        [Tooltip("Tốc độ chuyển đổi mực nước tự nhiên (m/s)")]
        public float naturalTideSpeed = 0.5f;

        [Header("── Cơ Chế Flood Trap (Bẫy Nước) ──")]
        public bool isFloodTrapActive = false;
        public float floodTrapDuration = 60f; // 60 giây để thoát
        public float floodTrapRiseSpeed = 0.15f; // Dâng nhanh

        [Header("── Drowning & Oxygen ──")]
        [Tooltip("Lượng oxy tối đa của player khi lặn (giây)")]
        public float maxBreathTime = 15f;
        public float currentBreathTime;
        public bool hasOxygenMask = false; // Nhặt được Crude Oxygen Mask sẽ tăng x2 thời gian

        [Header("── Âm Thanh ──")]
        public AudioClip waterRisingSound;
        public AudioClip emergencyAlarmSound;
        public AudioClip drowningGaspSound;

        private AudioSource audioSource;
        private Transform playerTransform;
        private PlayerStats playerStats;
        private float targetWaterY;
        private float escapeTimer = 0f;
        private bool isPlayerSubmerged = false;

        private void Awake()
        {
            Instance = this;
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (waterPlaneTransform == null)
            {
                waterPlaneTransform = transform;
            }

            currentBreathTime = maxBreathTime;
            targetWaterY = lowTideY;
        }

        private void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                playerStats = playerObj.GetComponent<PlayerStats>();
            }
        }

        private void Update()
        {
            // 1. Cập nhật mục tiêu mực nước theo chu kỳ ngày đêm hoặc bẫy nước
            if (isFloodTrapActive)
            {
                HandleFloodTrap();
            }
            else
            {
                HandleNaturalTide();
            }

            // 2. Di chuyển mượt mà mặt nước về vị trí mục tiêu
            if (waterPlaneTransform != null)
            {
                float currentY = waterPlaneTransform.position.y;
                float speed = isFloodTrapActive ? floodTrapRiseSpeed : naturalTideSpeed;
                float newY = Mathf.MoveTowards(currentY, targetWaterY, speed * Time.deltaTime);
                waterPlaneTransform.position = new Vector3(waterPlaneTransform.position.x, newY, waterPlaneTransform.position.z);
            }

            // 3. Kiểm tra người chơi có đang chìm dưới mặt nước không
            CheckPlayerSubmerged();
        }

        /// <summary>
        /// Xử lý thủy triều tự nhiên dựa theo thời gian ngày/đêm của đảo
        /// </summary>
        private void HandleNaturalTide()
        {
            float hour = 12f;
            if (DayNightCycle.Instance != null)
            {
                hour = DayNightCycle.Instance.currentHour;
            }

            // 6:00 đến 18:00 là ban ngày -> Thủy triều rút
            // 18:00 đến 6:00 sáng là ban đêm -> Thủy triều dâng
            if (hour >= 6f && hour < 18f)
            {
                targetWaterY = lowTideY;
            }
            else
            {
                targetWaterY = highTideY;
            }
        }

        /// <summary>
        /// Kích hoạt chuỗi sự kiện Bẫy Nước Dâng (gọi khi người chơi lấy Loot hoặc kích hoạt Trigger)
        /// </summary>
        public void TriggerFloodTrap()
        {
            if (isFloodTrapActive) return;

            isFloodTrapActive = true;
            targetWaterY = floodTrapY;
            escapeTimer = floodTrapDuration;

            if (audioSource != null)
            {
                if (emergencyAlarmSound != null) audioSource.PlayOneShot(emergencyAlarmSound);
                if (waterRisingSound != null)
                {
                    audioSource.clip = waterRisingSound;
                    audioSource.loop = true;
                    audioSource.Play();
                }
            }

            Debug.Log("<color=red>[CaveWaterLevel]</color> ⚠️ FLOOD TRAP KÍCH HOẠT! Nước đang dâng nhanh! Chạy ngay ra cửa hang!");
        }

        private void HandleFloodTrap()
        {
            if (escapeTimer > 0f)
            {
                escapeTimer -= Time.deltaTime;
                if (escapeTimer <= 0f)
                {
                    escapeTimer = 0f;
                    Debug.Log("<color=red>[CaveWaterLevel]</color> Hết thời gian thoát thân! Hang đã ngập hoàn toàn!");
                }
            }
        }

        /// <summary>
        /// Hủy bẫy nước khi người chơi thoát an toàn ra ngoài cửa hang
        /// </summary>
        public void ResetFloodTrap()
        {
            isFloodTrapActive = false;
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            targetWaterY = lowTideY;
        }

        /// <summary>
        /// Kiểm tra độ cao đầu người chơi so với mặt nước để tính oxy và ngạt nước
        /// </summary>
        private void CheckPlayerSubmerged()
        {
            if (playerTransform == null || waterPlaneTransform == null) return;

            // Đầu người chơi cao hơn vị trí chân ~1.6m
            float playerHeadY = playerTransform.position.y + 1.6f;
            float currentWaterY = waterPlaneTransform.position.y;

            if (playerHeadY < currentWaterY)
            {
                // Người chơi đang chìm dưới nước
                isPlayerSubmerged = true;

                float breathLimit = hasOxygenMask ? maxBreathTime * 2.0f : maxBreathTime;
                currentBreathTime -= Time.deltaTime;

                if (currentBreathTime <= 0f)
                {
                    currentBreathTime = 0f;
                    // Bị ngạt thở: Mất máu liên tục
                    if (playerStats != null)
                    {
                        playerStats.TakeDamage(15f * Time.deltaTime);
                    }
                }
            }
            else
            {
                // Đã ngoi lên mặt nước: Hồi lại dưỡng khí
                if (isPlayerSubmerged)
                {
                    isPlayerSubmerged = false;
                    if (audioSource != null && drowningGaspSound != null)
                    {
                        audioSource.PlayOneShot(drowningGaspSound);
                    }
                }

                currentBreathTime = Mathf.MoveTowards(currentBreathTime, maxBreathTime, 5f * Time.deltaTime);
            }
        }

        private void OnGUI()
        {
            // Hiển thị cảnh báo Flood Trap và đếm ngược
            if (isFloodTrapActive && escapeTimer > 0f)
            {
                GUIStyle alertStyle = new GUIStyle(GUI.skin.label);
                alertStyle.fontSize = 22;
                alertStyle.fontStyle = FontStyle.Bold;
                alertStyle.alignment = TextAnchor.MiddleCenter;
                alertStyle.normal.textColor = Color.red;

                GUI.Label(new Rect(Screen.width / 2 - 250, 80, 500, 35),
                    string.Format("⚠️ NƯỚC ĐANG DÂNG! THOÁT RA CỬA HANG TRONG: {0}s", Mathf.CeilToInt(escapeTimer)), alertStyle);
            }

            // Hiển thị thanh đo Oxy khi đang lặn dưới nước
            if (isPlayerSubmerged)
            {
                float breathLimit = hasOxygenMask ? maxBreathTime * 2.0f : maxBreathTime;
                float pct = Mathf.Clamp01(currentBreathTime / breathLimit);

                float barWidth = 220f;
                float barHeight = 16f;
                float x = Screen.width / 2 - barWidth / 2;
                float y = Screen.height - 120;

                // Nền đen
                GUI.color = new Color(0, 0, 0, 0.7f);
                GUI.DrawTexture(new Rect(x - 2, y - 2, barWidth + 4, barHeight + 4), Texture2D.whiteTexture);

                // Thanh oxy màu xanh dương
                GUI.color = Color.Lerp(Color.red, new Color(0.2f, 0.7f, 1f), pct);
                GUI.DrawTexture(new Rect(x, y, barWidth * pct, barHeight), Texture2D.whiteTexture);
                GUI.color = Color.white;

                GUIStyle oxyStyle = new GUIStyle(GUI.skin.label);
                oxyStyle.alignment = TextAnchor.MiddleCenter;
                oxyStyle.fontSize = 12;
                GUI.Label(new Rect(x, y, barWidth, barHeight), string.Format("OXY: {0}s", Mathf.CeilToInt(currentBreathTime)), oxyStyle);
            }
        }
    }
}

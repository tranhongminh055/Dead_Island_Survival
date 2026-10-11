using UnityEngine;
using HorrorGame.Player;

namespace HorrorGame.Player
{
    /// <summary>
    /// Hệ thống Ký Sinh Trùng Zombie & Bọ Ký Sinh phong cách Green Hell:
    /// - Lây nhiễm khi bị Zombie / Thổ dân cào cắn, lội đầm lầy ô nhiễm, hoặc ăn thịt sống.
    /// - Bọ ký sinh rút kiệt chất dinh dưỡng (thức ăn & nước uống tụt nhanh hơn).
    /// - Thỉnh thoảng cắn nhói gây đau đớn, mất máu nhẹ và tụt Sanity.
    /// - Cách chữa trị:
    ///   1. Sưởi ấm bên Lửa Trại (đứng cạnh lửa 7s để thiêu chết bọ)
    ///   2. Dùng Băng gạc (Bandage) để loại bỏ từng con
    ///   3. Uống Thuốc thảo dược (Herbal Medicine) để diệt sạch 100% bọ và giải độc
    /// </summary>
    public class ZombieParasiteSystem : MonoBehaviour
    {
        [Header("── Chỉ Số Ký Sinh Trùng ──")]
        [Tooltip("Số lượng ký sinh trùng đang bám dưới da (Tối đa 4)")]
        public int parasiteCount = 0;
        public int maxParasites = 4;

        [Header("── Tác Động Cơ Thể ──")]
        [Tooltip("Mỗi con bọ làm tăng tốc độ đói & khát thêm (%)")]
        public float hungerDrainMultiplierPerParasite = 0.30f;
        [Tooltip("Thời gian giữa các lần bọ cắn nhói (giây)")]
        public float biteIntervalMin = 14f;
        public float biteIntervalMax = 22f;

        // Trạng thái nội bộ
        private PlayerStats playerStats;
        private float nextBiteTime = 0f;
        private float campfireHeatTimer = 0f;
        private string alertMessage = "";
        private float alertTimer = 0f;

        private GUIStyle alertStyle;
        private GUIStyle alertShadowStyle;

        void Start()
        {
            playerStats = GetComponent<PlayerStats>();
            if (playerStats == null) playerStats = FindObjectOfType<PlayerStats>();
            ResetNextBiteTime();
        }

        void Update()
        {
            if (playerStats == null || playerStats.isDead) return;

            if (alertTimer > 0f) alertTimer -= Time.deltaTime;

            // 1. TỰ ĐỘNG THIÊU CHẾT BỌ KÝ SINH KHI ĐỨNG SƯỞI ẤM BÊN LỬA TRẠI (GREEN HELL STYLE)
            if (playerStats.isNearFire && parasiteCount > 0)
            {
                campfireHeatTimer += Time.deltaTime;
                if (campfireHeatTimer >= 6.5f)
                {
                    campfireHeatTimer = 0f;
                    RemoveParasites(1);
                    ShowAlert("🔥 Hơi nóng lửa trại đã hun chết và làm rụng 1 Ký sinh trùng!");
                    playerStats.RestoreSanity(5f);
                }
            }
            else
            {
                campfireHeatTimer = 0f;
            }

            // 2. BỌ CẮN NHÓI DƯỚI DA THEO ĐỢT
            if (parasiteCount > 0)
            {
                if (Time.time >= nextBiteTime)
                {
                    PerformParasiteBite();
                    ResetNextBiteTime();
                }
            }
        }

        /// <summary>
        /// Bọ cắn nhói gây đau đớn và suy sụp tinh thần
        /// </summary>
        private void PerformParasiteBite()
        {
            float painDamage = 1.5f * parasiteCount;
            float sanityDrop = 2.0f * parasiteCount;

            playerStats.TakeDamage(painDamage);
            playerStats.DrainSanity(sanityDrop);

            ShowAlert("🐛 Ký sinh trùng ngọ nguậy cắn nhói dưới da! (-" + Mathf.RoundToInt(sanityDrop) + " Tinh thần)");
            Debug.Log("[ZombieParasiteSystem] Ký sinh trùng cắn nhói gây sát thương và tụt tinh thần!");
        }

        private void ResetNextBiteTime()
        {
            nextBiteTime = Time.time + Random.Range(biteIntervalMin, biteIntervalMax);
        }

        /// <summary>
        /// Gọi khi bị Zombie cào, Thổ dân đánh trúng hoặc lội bùn ăn thịt sống
        /// </summary>
        public bool TryInfect(float chance, string sourceName = "Vết thương")
        {
            if (parasiteCount >= maxParasites) return false;

            if (Random.value <= chance)
            {
                parasiteCount++;
                if (parasiteCount > maxParasites) parasiteCount = maxParasites;

                if (playerStats != null)
                {
                    playerStats.DrainSanity(4f);
                }

                ShowAlert("⚠️ BỊ NHIỄM KÝ SINH TRÙNG! (" + sourceName + ") - Hãy tìm Lửa trại hoặc Băng gạc!");
                Debug.LogWarning("[ZombieParasiteSystem] Người chơi đã bị nhiễm ký sinh trùng! Hiện có: " + parasiteCount);
                ResetNextBiteTime();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Loại bỏ bọ bằng Băng gạc hoặc hun lửa
        /// </summary>
        public void RemoveParasites(int amount)
        {
            if (parasiteCount <= 0) return;

            parasiteCount = Mathf.Max(0, parasiteCount - amount);
            if (parasiteCount == 0)
            {
                ShowAlert("✨ Bạn đã loại bỏ hoàn toàn ký sinh trùng khỏi cơ thể!");
            }
        }

        /// <summary>
        /// Diệt sạch 100% bằng Thuốc thảo dược
        /// </summary>
        public void CureAll()
        {
            if (parasiteCount > 0)
            {
                parasiteCount = 0;
                ShowAlert("✨ Thuốc thảo dược đã tiêu diệt sạch toàn bộ ký sinh trùng và thanh lọc cơ thể!");
            }
        }

        public void ShowAlert(string msg)
        {
            alertMessage = msg;
            alertTimer = 4.0f;
        }

        void OnGUI()
        {
            if (playerStats == null || playerStats.isDead) return;

            // Khởi tạo Style GUI
            if (alertStyle == null)
            {
                alertStyle = new GUIStyle();
                alertStyle.fontSize = 15;
                alertStyle.fontStyle = FontStyle.Bold;
                alertStyle.alignment = TextAnchor.MiddleCenter;
                alertStyle.normal.textColor = new Color(1f, 0.25f, 0.25f, 1f);

                alertShadowStyle = new GUIStyle(alertStyle);
                alertShadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
            }

            // 1. Hiển thị thông báo khi có biến cố ký sinh trùng
            if (alertTimer > 0f && !string.IsNullOrEmpty(alertMessage))
            {
                float w = 550f;
                float h = 32f;
                float x = (Screen.width - w) * 0.5f;
                float y = Screen.height * 0.28f;

                GUI.Label(new Rect(x + 2, y + 2, w, h), alertMessage, alertShadowStyle);
                GUI.Label(new Rect(x, y, w, h), alertMessage, alertStyle);
            }

            // 2. Hiển thị Badge nhỏ cảnh báo số lượng ký sinh trùng dưới góc màn hình
            if (parasiteCount > 0)
            {
                float badgeW = 260f;
                float badgeH = 24f;
                float bx = 20f;
                float by = Screen.height - 180f;

                GUI.color = new Color(0f, 0f, 0f, 0.65f);
                GUI.Box(new Rect(bx - 5, by - 2, badgeW + 10, badgeH + 4), "");
                GUI.color = Color.white;

                string badgeText = "🐛 Ký sinh trùng dưới da: " + parasiteCount + "/" + maxParasites + " con";
                GUI.Label(new Rect(bx + 1, by + 1, badgeW, badgeH), badgeText, alertShadowStyle);
                GUI.Label(new Rect(bx, by, badgeW, badgeH), badgeText, alertStyle);
            }
        }
    }
}

using UnityEngine;
using HorrorGame.Player;

namespace HorrorGame.Environment.Weather
{
    /// <summary>
    /// Cơ chế Sinh Tồn Thân Nhiệt & Độ Ướt (Player Wetness Mechanic):
    /// - Khi dầm mưa ngoài trời, người chơi bị ướt dần (0 -> 100%)
    /// - Đứng dưới mái che (Shelter) hoặc gần Lửa trại (Campfire) sẽ hong khô người
    /// - Khi bị ướt sũng (> 60%), cơ thể bị nhiễm lạnh:
    ///   + Tụt thể lực / hồi phục thể lực chậm
    ///   + Tinh thần (Sanity) suy giảm nhanh hơn trong đêm mưa lạnh
    /// - Bấm phím [H] khi trời mưa để hứng nước mưa uống giải khát!
    /// </summary>
    public class PlayerWetness : MonoBehaviour
    {
        [Header("── Chỉ Số Ướt ──")]
        [Range(0f, 100f)]
        public float wetness = 0f;

        [Header("── Tốc Độ ──")]
        [Tooltip("Tốc độ bị ướt khi đứng dầm mưa (% mỗi giây)")]
        public float wetRate = 4.5f;
        [Tooltip("Tốc độ tự khô ráo trong thời tiết nắng ráo (% mỗi giây)")]
        public float dryRate = 2.0f;
        [Tooltip("Tốc độ khô nhanh khi đứng cạnh đống lửa (% mỗi giây)")]
        public float campfireDryRate = 8.0f;

        [Header("── Tác Động Sinh Tồn (Green Hell) ──")]
        [Tooltip("Mức độ ướt bắt đầu bị nhiễm lạnh (mặc định 60%)")]
        public float coldThreshold = 60f;
        [Tooltip("Tốc độ hao tổn Tinh thần khi dầm mưa bão lạnh")]
        public float wetSanityDrainRate = 0.5f;

        private PlayerStats playerStats;
        private bool isNearFire = false;
        private float drinkCooldown = 0f;

        private GUIStyle wetTextStyle;
        private Texture2D wetBarTexture;

        private void Start()
        {
            playerStats = GetComponent<PlayerStats>();
            if (playerStats == null)
            {
                playerStats = FindObjectOfType<PlayerStats>();
            }
        }

        private void Update()
        {
            if (drinkCooldown > 0f) drinkCooldown -= Time.deltaTime;

            CheckNearbyFire();
            UpdateWetnessLevel();
            ApplySurvivalPenalties();
            HandleRainDrinking();
        }

        private void UpdateWetnessLevel()
        {
            WeatherSystem ws = WeatherSystem.Instance;
            bool isRaining = (ws != null && ws.IsRaining);
            bool isSheltered = (ws != null && ws.IsPlayerSheltered);

            if (isRaining && !isSheltered)
            {
                // Dầm mưa -> ướt dần theo cường độ mưa
                wetness += wetRate * ws.CurrentRainIntensity * Time.deltaTime;
            }
            else
            {
                // Hong khô (khô nhanh hơn nếu gần lửa trại)
                float currentDrySpeed = isNearFire ? campfireDryRate : dryRate;
                wetness -= currentDrySpeed * Time.deltaTime;
            }

            wetness = Mathf.Clamp(wetness, 0f, 100f);
        }

        private void ApplySurvivalPenalties()
        {
            if (playerStats == null || playerStats.isDead) return;

            // Khi người bị ướt sũng (> 60%)
            if (wetness >= coldThreshold)
            {
                float coldSeverity = (wetness - coldThreshold) / (100f - coldThreshold);

                // 1. Tụt Tinh Thần (Sanity) do rét run
                if (WeatherSystem.Instance != null && WeatherSystem.Instance.IsThunderstorm)
                {
                    playerStats.DrainSanity(wetSanityDrainRate * coldSeverity * 1.5f * Time.deltaTime);
                }
                else
                {
                    playerStats.DrainSanity(wetSanityDrainRate * coldSeverity * Time.deltaTime);
                }

                // 2. Thể lực giảm sút do lạnh cóng
                if (playerStats.currentStamina > 0)
                {
                    playerStats.currentStamina -= (2f * coldSeverity) * Time.deltaTime;
                }
            }
        }

        /// <summary>
        /// Cho phép người chơi ngửa mặt hứng nước mưa uống khi khát
        /// </summary>
        private void HandleRainDrinking()
        {
            WeatherSystem ws = WeatherSystem.Instance;
            if (ws == null || !ws.IsRaining || ws.IsPlayerSheltered) return;

            // Nhấn phím H để hứng nước mưa
            if (Input.GetKeyDown(KeyCode.H) && drinkCooldown <= 0f)
            {
                drinkCooldown = 2.5f;
                if (playerStats != null)
                {
                    playerStats.currentThirst = Mathf.Min(playerStats.maxThirst, playerStats.currentThirst + 15f);
                    Debug.Log("💧 [PlayerWetness] Đã hứng nước mưa uống! Cơn khát giảm bớt.");
                }
            }
        }

        private void CheckNearbyFire()
        {
            // Quét các đống lửa xung quanh trong bán kính 4m
            Collider[] colliders = Physics.OverlapSphere(transform.position, 4.0f);
            isNearFire = false;
            foreach (var col in colliders)
            {
                if (col.name.ToLower().Contains("fire") || col.name.ToLower().Contains("camp"))
                {
                    isNearFire = true;
                    break;
                }
            }
        }

        private void OnGUI()
        {
            // Chỉ hiển thị UI khi cơ thể bắt đầu bị ướt (> 10%)
            if (wetness <= 10f) return;

            if (wetTextStyle == null)
            {
                wetTextStyle = new GUIStyle(GUI.skin.label);
                wetTextStyle.fontSize = 13;
                wetTextStyle.fontStyle = FontStyle.Bold;
                wetTextStyle.normal.textColor = new Color(0.6f, 0.85f, 1.0f);
            }

            // Vẽ thông báo tình trạng ướt ở góc trái dưới màn hình
            float screenH = Screen.height;
            string status = wetness >= coldThreshold ? "🥶 CƠ THỂ BỊ LẠNH CÓNG DO ƯỚT SŨNG!" : "💧 Đang bị ướt mưa";
            GUI.Label(new Rect(20, screenH - 145, 300, 25), string.Format("{0} ({1:F0}%)", status, wetness), wetTextStyle);

            if (WeatherSystem.Instance != null && WeatherSystem.Instance.IsRaining && !WeatherSystem.Instance.IsPlayerSheltered)
            {
                GUI.Label(new Rect(20, screenH - 125, 300, 25), "👉 Bấm [H] để hứng nước mưa uống", wetTextStyle);
            }
        }
    }
}

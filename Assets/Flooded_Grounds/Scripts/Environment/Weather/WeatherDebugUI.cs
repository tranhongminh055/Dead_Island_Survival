using UnityEngine;

namespace HorrorGame.Environment.Weather
{
    /// <summary>
    /// Giao diện Debug & Phím tắt kiểm tra hệ thống thời tiết nhanh chóng trong lúc chơi:
    /// - Nhấn [F8] để bật/tắt bảng điều khiển thời tiết
    /// - Chọn nhanh các kiểu thời tiết: Nắng, Mây, Mưa nhỏ, Mưa to, Bão sấm sét
    /// - Bấm nút [⚡ ĐÁNH SÉT NGAY] để thử cảm giác sét đánh tức thì
    /// - Hiển thị trạng thái thời tiết, độ ướt, nơi trú ẩn ở góc màn hình
    /// </summary>
    public class WeatherDebugUI : MonoBehaviour
    {
        [Tooltip("Bật/tắt giao diện Debug. Mặc định TẮT hoàn toàn để game vận hành tự nhiên 100%")]
        public bool enableDebugUI = false;
        public bool showDebugWindow = false;
        public KeyCode toggleKey = KeyCode.F8;

        private Rect windowRect = new Rect(20, 80, 280, 360);
        private GUIStyle headerStyle;
        private GUIStyle statusStyle;

        private void Update()
        {
            if (!enableDebugUI) return;

            if (Input.GetKeyDown(toggleKey))
            {
                showDebugWindow = !showDebugWindow;
            }
        }

        private void OnGUI()
        {
            if (!enableDebugUI) return;

            WeatherSystem ws = WeatherSystem.Instance;
            if (ws == null) return;

            InitStyles();

            // Hiển thị thanh trạng thái thời tiết nhỏ ở góc trên bên phải màn hình
            DrawWeatherStatusBadge(ws);

            if (showDebugWindow)
            {
                windowRect = GUI.Window(98765, windowRect, DrawWindowContent, "⛈️ BẢNG ĐIỀU KHIỂN THỜI TIẾT (F8)");
            }
        }

        private void InitStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.label);
                headerStyle.fontStyle = FontStyle.Bold;
                headerStyle.fontSize = 12;
                headerStyle.normal.textColor = Color.yellow;

                statusStyle = new GUIStyle(GUI.skin.box);
                statusStyle.fontSize = 12;
                statusStyle.fontStyle = FontStyle.Bold;
                statusStyle.normal.textColor = Color.white;
            }
        }

        private void DrawWeatherStatusBadge(WeatherSystem ws)
        {
            string weatherIcon = "☀️";
            string name = "Nắng ráo";

            switch (ws.currentWeather)
            {
                case WeatherType.Clear: weatherIcon = "☀️"; name = "Nắng Ráo"; break;
                case WeatherType.Cloudy: weatherIcon = "⛅"; name = "Nhiều Mây"; break;
                case WeatherType.LightRain: weatherIcon = "🌧️"; name = "Mưa Phùn"; break;
                case WeatherType.HeavyRain: weatherIcon = "🌧️🌧️"; name = "Mưa To"; break;
                case WeatherType.Thunderstorm: weatherIcon = "⛈️⚡"; name = "Bão Sấm Chớp"; break;
            }

            string shelterStr = ws.IsPlayerSheltered ? " [🏠 Dưới Mái Che]" : " [🌲 Ngoài Trời]";
            string badgeText = string.Format("{0} {1}{2}", weatherIcon, name, shelterStr);

            float w = 230f;
            GUI.Box(new Rect(Screen.width - w - 20, 20, w, 28), badgeText, statusStyle);
        }

        private void DrawWindowContent(int windowID)
        {
            WeatherSystem ws = WeatherSystem.Instance;
            if (ws == null) return;

            GUILayout.Space(5);
            GUILayout.Label(string.Format("Thời tiết hiện tại: <b>{0}</b>", ws.currentWeather), headerStyle);
            GUILayout.Label(string.Format("Cường độ mưa: {0:P0} | Gió: {1:P0}", ws.CurrentRainIntensity, ws.CurrentWindIntensity));
            GUILayout.Label(string.Format("Đang dưới mái che: {0}", ws.IsPlayerSheltered ? "CÓ ✅" : "KHÔNG ❌"));

            GUILayout.Space(8);
            GUILayout.Label("── CHỌN THỜI TIẾT NHANH ──", headerStyle);

            if (GUILayout.Button("☀️ Trời Nắng Ráo (Clear)", GUILayout.Height(26)))
            {
                ws.SetWeather(WeatherType.Clear, 5f);
            }

            if (GUILayout.Button("⛅ Âm U Nhiều Mây (Cloudy)", GUILayout.Height(26)))
            {
                ws.SetWeather(WeatherType.Cloudy, 5f);
            }

            if (GUILayout.Button("🌧️ Mưa Phùn Nhẹ (Light Rain)", GUILayout.Height(26)))
            {
                ws.SetWeather(WeatherType.LightRain, 5f);
            }

            if (GUILayout.Button("🌧️ Mưa Rào Nặng Hạt (Heavy Rain)", GUILayout.Height(26)))
            {
                ws.SetWeather(WeatherType.HeavyRain, 5f);
            }

            if (GUILayout.Button("⛈️ BÃO TÁP SẤM SÉT (Thunderstorm)", GUILayout.Height(28)))
            {
                ws.SetWeather(WeatherType.Thunderstorm, 5f);
            }

            GUILayout.Space(8);
            GUILayout.Label("── HIỆU ỨNG SẤM SÉT ──", headerStyle);

            GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);
            if (GUILayout.Button("⚡ ĐÁNH SÉT NGAY LẬP TỨC!", GUILayout.Height(32)))
            {
                if (ws.lightningSystem != null)
                {
                    ws.lightningSystem.StrikeRandom();
                }
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(8);
            GUILayout.Label("── CHU KỲ THỜI TIẾT ──", headerStyle);
            GUILayout.Label(string.Format("Đổi thời tiết tiếp theo sau: <b>{0:F0}s</b>", ws.weatherTimer));
            ws.autoChangeWeather = GUILayout.Toggle(ws.autoChangeWeather, " Tự động chu kỳ thời tiết");

            GUILayout.Space(6);
            GUI.backgroundColor = new Color(1f, 0.5f, 0.3f);
            if (GUILayout.Button("⛈️ KÍCH HOẠT BÃO MỞ ĐẦU (Test)", GUILayout.Height(28)))
            {
                ws.TriggerPostTrailerStorm(-1f, true);
            }
            GUI.backgroundColor = Color.white;

            GUI.DragWindow();
        }
    }
}

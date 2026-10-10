using UnityEngine;
using UnityEditor;
using HorrorGame.Environment.Weather;

namespace HorrorGame.Editor
{
    [CustomEditor(typeof(WeatherSystem))]
    public class WeatherSystemEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            WeatherSystem ws = (WeatherSystem)target;

            GUILayout.Space(12f);
            EditorGUILayout.LabelField("⚡ ĐIỀU KHIỂN NHANH TRONG PLAY MODE", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Hãy ấn PLAY game để sử dụng các nút chuyển đổi thời tiết và đánh sét trực tiếp!", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField(string.Format("Thời Tiết Hiện Tại: {0}", ws.currentWeather), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(string.Format("Cường độ mưa: {0:P0} | Gió: {1:P0}", ws.CurrentRainIntensity, ws.CurrentWindIntensity));
            EditorGUILayout.LabelField(string.Format("Nơi trú ẩn (Shelter): {0}", ws.IsPlayerSheltered ? "Có mái che ✅" : "Ngoài trời 🌲"));
            EditorGUILayout.LabelField(string.Format("Chu kỳ tự động: {0} (Còn {1:F0}s)", ws.autoChangeWeather ? "BẬT" : "TẮT", ws.weatherTimer));

            GUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("☀️ Nắng Ráo", GUILayout.Height(28))) ws.SetWeather(WeatherType.Clear, 4f);
            if (GUILayout.Button("⛅ Nhiều Mây", GUILayout.Height(28))) ws.SetWeather(WeatherType.Cloudy, 4f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🌧️ Mưa Phùn", GUILayout.Height(28))) ws.SetWeather(WeatherType.LightRain, 4f);
            if (GUILayout.Button("🌧️ Mưa Rào", GUILayout.Height(28))) ws.SetWeather(WeatherType.HeavyRain, 4f);
            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
            if (GUILayout.Button("⛈️ BÃO TÁP SẤM SÉT (Thunderstorm)", GUILayout.Height(32)))
            {
                ws.SetWeather(WeatherType.Thunderstorm, 4f);
            }

            GUILayout.Space(4f);
            GUI.backgroundColor = new Color(1f, 0.6f, 0.2f);
            if (GUILayout.Button("⛈️ [TEST] BÃO MỞ ĐẦU SAU TRAILER", GUILayout.Height(30)))
            {
                ws.TriggerPostTrailerStorm(-1f, true);
            }

            GUILayout.Space(4f);
            GUI.backgroundColor = new Color(1f, 0.9f, 0.3f);
            if (GUILayout.Button("⚡ [TEST] ĐÁNH SÉT NGAY BÂY GIỜ!", GUILayout.Height(34)))
            {
                if (ws.lightningSystem != null)
                {
                    ws.lightningSystem.StrikeRandom();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }
    }
}

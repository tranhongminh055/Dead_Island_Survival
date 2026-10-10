using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HorrorGame.Environment.Weather;
using HorrorGame.Player;

namespace HorrorGame.Editor
{
    public class WeatherSystemSetup
    {
        [MenuItem("Horror Game/⛈️ Kích Hoạt Hệ Thống Mưa & Sấm Chớp (Weather System)", false, 12)]
        public static void SetupWeatherInScene()
        {
            var existing = Object.FindObjectOfType<WeatherSystem>();
            if (existing != null)
            {
                Debug.Log("✅ [WeatherSetup] Scene hiện tại đã có WeatherSystem: " + existing.gameObject.name);
                Selection.activeGameObject = existing.gameObject;
                EnsurePlayerWetness();
                return;
            }

            // Tạo GameObject [WeatherSystem]
            GameObject weatherRoot = new GameObject("[WeatherSystem]");
            Undo.RegisterCreatedObjectUndo(weatherRoot, "Create Weather System");

            // Gắn các thành phần chính
            WeatherSystem ws = weatherRoot.AddComponent<WeatherSystem>();
            // Vận hành 100% tự nhiên, không hiển thị bảng debug UI làm phiền người chơi

            // Tạo module RainEffect
            GameObject rainObj = new GameObject("RainEffect_Module");
            rainObj.transform.SetParent(weatherRoot.transform);
            RainEffect rainEffect = rainObj.AddComponent<RainEffect>();
            ws.rainEffect = rainEffect;

            // Tạo module LightningSystem
            GameObject lightningObj = new GameObject("LightningSystem_Module");
            lightningObj.transform.SetParent(weatherRoot.transform);
            LightningSystem lightningSystem = lightningObj.AddComponent<LightningSystem>();
            ws.lightningSystem = lightningSystem;

            // Gắn PlayerWetness vào người chơi
            EnsurePlayerWetness();

            // Đánh dấu Scene đã thay đổi để lưu lại
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = weatherRoot;

            Debug.Log("🎉 [WeatherSetup] ĐÃ CÀI ĐẶT THÀNH CÔNG HỆ THỐNG TRỜI MƯA & SẤM CHỚP!");
            Debug.Log("👉 Bấm PLAY và nhấn [F8] để mở bảng điều khiển thời tiết & test sấm sét tức thì!");
        }

        private static void EnsurePlayerWetness()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                var pc = Object.FindObjectOfType<PlayerController>();
                if (pc != null) player = pc.gameObject;
            }

            if (player != null)
            {
                if (player.GetComponent<PlayerWetness>() == null)
                {
                    player.AddComponent<PlayerWetness>();
                    Debug.Log("✅ [WeatherSetup] Đã gắn cơ chế Thân nhiệt & Ướt mưa (PlayerWetness) vào " + player.name);
                }
            }
        }
    }
}

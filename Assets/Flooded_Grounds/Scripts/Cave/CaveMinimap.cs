using UnityEngine;
using System.Collections.Generic;

namespace HorrorGame.Cave
{
    /// <summary>
    /// CaveMinimap — Bản Đồ Hang Động Vẽ Tay (Parchment Cave Map).
    /// - Khi người chơi nhặt được Bản đồ hang hoặc bấm phím [M]:
    ///   + Mở giao diện bản đồ da dê cổ xưa vẽ tay phong cách The Forest.
    ///   + Đánh dấu các cửa hang đã được phát hiện (Cave 1 -> Cave 6).
    ///   + Hiển thị biểu tượng vị trí người chơi hiện tại trên đảo hoặc trong hang.
    /// </summary>
    public class CaveMinimap : MonoBehaviour
    {
        public static CaveMinimap Instance;

        [Header("── Phím Tắt ──")]
        public KeyCode toggleMapKey = KeyCode.M;

        [Header("── Tài Nguyên Hình Ảnh ──")]
        public Texture2D mapParchmentTexture;
        public Texture2D caveMarkerIcon;
        public Texture2D playerMarkerIcon;

        [Header("── Trạng Thái ──")]
        public bool hasMapItem = true; // Cho phép xem nếu đã có bản đồ
        public bool isMapOpen = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleMapKey))
            {
                if (hasMapItem)
                {
                    isMapOpen = !isMapOpen;
                }
            }
        }

        private void OnGUI()
        {
            if (!isMapOpen) return;

            // Kích thước bản đồ da dê
            float mapW = 600f;
            float mapH = 500f;
            float x = (Screen.width - mapW) / 2;
            float y = (Screen.height - mapH) / 2;

            // Nền da dê
            GUI.color = new Color(0.18f, 0.14f, 0.1f, 0.95f);
            GUI.DrawTexture(new Rect(x, y, mapW, mapH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Khung viền
            GUI.Box(new Rect(x - 4, y - 4, mapW + 8, mapH + 8), "");

            // Tiêu đề bản đồ
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 20;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.UpperCenter;
            titleStyle.normal.textColor = new Color(0.95f, 0.82f, 0.55f);
            GUI.Label(new Rect(x, y + 15, mapW, 30), "🗺️ BẢN ĐỒ KHÁM PHÁ HÒNG ĐẢO & HANG ĐỘNG", titleStyle);

            // Danh sách các hang động
            float listY = y + 65;
            string[] caveNames = new string[]
            {
                "1. Hang Sụp Đổ (Gần Xác Máy Bay) ⭐",
                "2. Hang Rễ Cây (Rừng Sâu Cổ Thụ) ⭐⭐",
                "3. Hang Thủy Triều (Vách Đá Bờ Biển Bắc) ⭐⭐⭐",
                "4. Hang Đầm Lầy (Tổ Thổ Dân Phía Đông) ⭐⭐⭐",
                "5. Hang Nham Thạch (Sườn Núi Lửa Nam) ⭐⭐⭐⭐",
                "6. Lõi Ký Sinh Ngầm (Tổ Trùm Cuối) 💀"
            };

            for (int i = 0; i < caveNames.Length; i++)
            {
                int caveID = i + 1;
                bool discovered = CaveManager.Instance != null && CaveManager.Instance.IsCaveDiscovered(caveID);
                bool completed = CaveManager.Instance != null && CaveManager.Instance.IsCaveCompleted(caveID);

                GUIStyle caveStyle = new GUIStyle(GUI.skin.label);
                caveStyle.fontSize = 14;

                if (completed)
                {
                    caveStyle.normal.textColor = new Color(0.3f, 0.9f, 0.3f);
                    GUI.Label(new Rect(x + 35, listY, mapW - 70, 24), "✔ " + caveNames[i] + " [ĐÃ HOÀN THÀNH]", caveStyle);
                }
                else if (discovered)
                {
                    caveStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
                    GUI.Label(new Rect(x + 35, listY, mapW - 70, 24), "❓ " + caveNames[i] + " [ĐÃ PHÁT HIỆN]", caveStyle);
                }
                else
                {
                    caveStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                    GUI.Label(new Rect(x + 35, listY, mapW - 70, 24), "🔒 " + caveNames[i] + " [CHƯA KHÁM PHÁ]", caveStyle);
                }

                listY += 32;
            }

            // Trạng thái hiện tại
            string currentLoc = CaveManager.IsPlayerInCave && CaveManager.CurrentCave != null
                ? "Đang ở trong: " + CaveManager.CurrentCave.caveName
                : "Đang ở trên mặt đất hòn đảo";

            GUIStyle locStyle = new GUIStyle(GUI.skin.label);
            locStyle.alignment = TextAnchor.MiddleCenter;
            locStyle.fontSize = 15;
            locStyle.fontStyle = FontStyle.Italic;
            locStyle.normal.textColor = Color.cyan;
            GUI.Label(new Rect(x, y + mapH - 70, mapW, 25), "📍 Vị trí hiện tại: " + currentLoc, locStyle);

            GUIStyle closeStyle = new GUIStyle(GUI.skin.label);
            closeStyle.alignment = TextAnchor.MiddleCenter;
            closeStyle.fontSize = 12;
            closeStyle.normal.textColor = Color.gray;
            GUI.Label(new Rect(x, y + mapH - 35, mapW, 20), "Nhấn [M] để đóng bản đồ", closeStyle);
        }
    }
}

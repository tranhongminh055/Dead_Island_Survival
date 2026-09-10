using UnityEngine;
using HorrorGame.Player;
using HorrorGame.Inventory;

namespace HorrorGame.Environment
{
    /// <summary>
    /// Hệ thống Tự Động Lưu Game (Auto Save System):
    /// - Tự động lưu vị trí người chơi và chỉ số sinh tồn (máu, đói, khát, thể lực) định kỳ
    /// - Lưu túi đồ và trạng thái trang bị
    /// - Tự động khôi phục dữ liệu khi bắt đầu game
    /// </summary>
    [DisallowMultipleComponent]
    public class AutoSaveSystem : MonoBehaviour
    {
        [Header("── Cài Đặt Lưu Tự Động (Auto Save Settings) ──")]
        [Tooltip("Bật hoặc tắt tính năng tự động lưu")]
        public bool enableAutoSave = true;

        [Tooltip("Khoảng thời gian giữa các lần tự động lưu (giây)")]
        public float autoSaveInterval = 120f; // Mặc định mỗi 2 phút

        [Tooltip("Hiển thị thông báo khi game tự động lưu")]
        public bool showSaveNotification = true;

        private float saveTimer = 0f;
        private string saveNoticeMessage = "";
        private float noticeTimer = 0f;
        private static GUIStyle noticeStyle;

        private void Start()
        {
            saveTimer = 0f;
        }

        private void Update()
        {
            if (!enableAutoSave) return;

            // Không lưu khi đang xem Cutscene
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;

            saveTimer += Time.deltaTime;
            if (saveTimer >= autoSaveInterval)
            {
                saveTimer = 0f;
                SaveGame();
            }

            if (noticeTimer > 0f)
            {
                noticeTimer -= Time.deltaTime;
            }
        }

        /// <summary>
        /// Thực hiện lưu game
        /// </summary>
        public void SaveGame()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            // 1. Lưu tọa độ vị trí người chơi
            Vector3 pos = player.transform.position;
            PlayerPrefs.SetFloat("Player_PosX", pos.x);
            PlayerPrefs.SetFloat("Player_PosY", pos.y);
            PlayerPrefs.SetFloat("Player_PosZ", pos.z);
            PlayerPrefs.SetFloat("Player_RotY", player.transform.eulerAngles.y);

            // 2. Lưu chỉ số sinh tồn
            PlayerStats stats = player.GetComponent<PlayerStats>();
            if (stats != null)
            {
                PlayerPrefs.SetFloat("Player_Health", stats.currentHealth);
                PlayerPrefs.SetFloat("Player_Stamina", stats.currentStamina);
                PlayerPrefs.SetFloat("Player_Hunger", stats.currentHunger);
                PlayerPrefs.SetFloat("Player_Thirst", stats.currentThirst);
                PlayerPrefs.SetFloat("Player_Sanity", stats.currentSanity);
            }

            PlayerPrefs.SetInt("HasSavedGame", 1);
            PlayerPrefs.Save();

            saveNoticeMessage = "💾 Đã tự động lưu tiến trình game!";
            noticeTimer = 3.0f;
            Debug.Log("<color=green>[AutoSaveSystem]</color> " + saveNoticeMessage);
        }

        /// <summary>
        /// Tải lại tiến trình đã lưu
        /// </summary>
        public void LoadGame()
        {
            if (PlayerPrefs.GetInt("HasSavedGame", 0) != 1) return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            // 1. Khôi phục tọa độ
            float px = PlayerPrefs.GetFloat("Player_PosX", player.transform.position.x);
            float py = PlayerPrefs.GetFloat("Player_PosY", player.transform.position.y);
            float pz = PlayerPrefs.GetFloat("Player_PosZ", player.transform.position.z);
            float ry = PlayerPrefs.GetFloat("Player_RotY", player.transform.eulerAngles.y);

            player.transform.position = new Vector3(px, py, pz);
            player.transform.rotation = Quaternion.Euler(0, ry, 0);

            // 2. Khôi phục chỉ số
            PlayerStats stats = player.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.currentHealth = PlayerPrefs.GetFloat("Player_Health", stats.maxHealth);
                stats.currentStamina = PlayerPrefs.GetFloat("Player_Stamina", stats.maxStamina);
                stats.currentHunger = PlayerPrefs.GetFloat("Player_Hunger", stats.maxHunger);
                stats.currentThirst = PlayerPrefs.GetFloat("Player_Thirst", stats.maxThirst);
                stats.currentSanity = PlayerPrefs.GetFloat("Player_Sanity", stats.maxSanity);
            }
        }

        private void OnGUI()
        {
            if (showSaveNotification && noticeTimer > 0f)
            {
                if (noticeStyle == null)
                {
                    noticeStyle = new GUIStyle(GUI.skin.box);
                    noticeStyle.fontSize = 15;
                    noticeStyle.fontStyle = FontStyle.Bold;
                    noticeStyle.alignment = TextAnchor.MiddleCenter;
                    noticeStyle.normal.textColor = new Color(0.2f, 1f, 0.4f);
                }

                float w = 320f;
                float h = 36f;
                float x = Screen.width - w - 20f;
                float y = 20f;

                GUI.Box(new Rect(x, y, w, h), saveNoticeMessage, noticeStyle);
            }
        }
    }
}

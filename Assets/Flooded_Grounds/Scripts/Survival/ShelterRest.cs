using UnityEngine;
using HorrorGame.Player;

namespace HorrorGame.Survival
{
    /// <summary>
    /// Gắn lên giường/nệm bên trong các công trình Nơi Trú Ẩn (Nhà gỗ, Nhà đá, Lều lá).
    /// Cho phép người chơi nhìn vào và bấm [E] để chợp mắt nghỉ ngơi:
    /// - Hồi phục +25 Máu
    /// - Hồi đầy 100% Thể lực (Stamina)
    /// - Hồi phục +25 Tinh thần (Sanity), giảm mệt mỏi
    /// </summary>
    public class ShelterRest : MonoBehaviour
    {
        [Header("── Cấu Hình Trú Ẩn ──")]
        public string shelterName = "Nơi Trú Ẩn";
        public float healAmount = 25f;
        public float sanityAmount = 25f;
        public float restCooldown = 15f; // Thời gian chờ giữa 2 lần ngủ
        public float interactDistance = 3.5f;

        private float lastRestTime = -999f;
        private bool playerLooking = false;
        private Transform playerTransform;
        private string noticeMessage = "";
        private float noticeTimer = 0f;

        private GUIStyle promptStyle;
        private GUIStyle shadowStyle;

        void Start()
        {
            FindPlayer();
        }

        private void FindPlayer()
        {
            if (playerTransform != null) return;
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else
            {
                var pc = FindObjectOfType<PlayerController>();
                if (pc != null) playerTransform = pc.transform;
                else if (Camera.main != null) playerTransform = Camera.main.transform;
            }
        }

        void Update()
        {
            FindPlayer();
            if (playerTransform == null) return;

            if (noticeTimer > 0f) noticeTimer -= Time.deltaTime;

            CheckPlayerLooking();

            // Nhấn E để nghỉ ngơi
            if (playerLooking && Input.GetKeyDown(KeyCode.E))
            {
                TryRest();
            }
        }

        private void CheckPlayerLooking()
        {
            playerLooking = false;
            Camera cam = Camera.main;
            if (cam == null) return;

            float dist = Vector3.Distance(transform.position, cam.transform.position);
            if (dist > interactDistance) return;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, interactDistance + 1f))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform) || transform.IsChildOf(hit.transform))
                {
                    playerLooking = true;
                }
            }
        }

        private void TryRest()
        {
            float elapsed = Time.time - lastRestTime;
            if (elapsed < restCooldown)
            {
                float remain = Mathf.Ceil(restCooldown - elapsed);
                ShowNotice("⏳ Bạn vừa mới nghỉ ngơi, hãy chờ thêm " + remain + " giây nữa.");
                return;
            }

            lastRestTime = Time.time;

            PlayerStats stats = playerTransform.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.Heal(healAmount);
                stats.currentStamina = stats.maxStamina;
                stats.currentSanity = Mathf.Min(stats.maxSanity, stats.currentSanity + sanityAmount);
                ShowNotice(string.Format("✨ Bạn đã nghỉ ngơi an toàn trong {0}! (+{1} Máu, Đầy Thể Lực)", shelterName, healAmount));
                Debug.Log(string.Format("⛺ [ShelterRest] Người chơi nghỉ ngơi trong {0}. Máu: {1}/{2}", shelterName, stats.currentHealth, stats.maxHealth));
            }
        }

        private void ShowNotice(string msg)
        {
            noticeMessage = msg;
            noticeTimer = 3.5f;
        }

        void OnGUI()
        {
            if (promptStyle == null)
            {
                promptStyle = new GUIStyle(GUI.skin.label);
                promptStyle.fontSize = 17;
                promptStyle.fontStyle = FontStyle.Bold;
                promptStyle.alignment = TextAnchor.MiddleCenter;
                promptStyle.normal.textColor = new Color(0.4f, 1f, 0.6f);

                shadowStyle = new GUIStyle(promptStyle);
                shadowStyle.normal.textColor = Color.black;
            }

            if (playerLooking)
            {
                string text = "[E] Chợp mắt nghỉ ngơi (" + shelterName + ")";
                Rect r = new Rect(Screen.width / 2 - 200, Screen.height / 2 + 50, 400, 30);
                GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, shadowStyle);
                GUI.Label(r, text, promptStyle);
            }

            if (noticeTimer > 0f && !string.IsNullOrEmpty(noticeMessage))
            {
                Rect r = new Rect(Screen.width / 2 - 300, Screen.height * 0.22f, 600, 35);
                GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), noticeMessage, shadowStyle);
                GUI.Label(r, noticeMessage, promptStyle);
            }
        }
    }
}

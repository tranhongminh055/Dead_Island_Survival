using UnityEngine;
using System.Collections;

namespace HorrorGame.Cave
{
    /// <summary>
    /// LoreNote — Ghi chú lore có thể đọc khi nhặt.
    /// Mỗi ghi chú kể một phần câu chuyện bí ẩn của hòn đảo.
    /// Khi player nhấn [E] gần LoreNote → hiển thị UI đọc toàn màn hình.
    /// Nhấn [ESC] hoặc [E] lần nữa để đóng.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LoreNote : MonoBehaviour
    {
        [Header("── Thông Tin Ghi Chú ──")]
        [Tooltip("ID duy nhất của ghi chú (1-6)")]
        public int noteID = 1;

        [Tooltip("Tiêu đề ghi chú")]
        public string noteTitle = "Nhật ký hành khách mất tích";

        [Tooltip("Nội dung ghi chú")]
        [TextArea(5, 15)]
        public string noteContent = "Ngày thứ 3 trên đảo...\n\nTôi nghe thấy tiếng gì đó vọng lên từ lòng đất. " +
            "Những tiếng rên rỉ không phải của con người. " +
            "Tôi đã tìm thấy một cái hang ở sườn đồi phía Tây. " +
            "Bên trong... có thứ gì đó đang chờ đợi.";

        [Tooltip("Tác giả / Nguồn gốc ghi chú")]
        public string noteAuthor = "— Nguyễn Văn A, Hành khách ghế 14C";

        [Header("── Tương Tác ──")]
        public float interactDistance = 2.5f;
        public AudioClip pickupSound;

        [Header("── Hiệu Ứng ──")]
        [Tooltip("Ghi chú sẽ phát sáng nhẹ để dễ tìm")]
        public bool glowEffect = true;

        // Trạng thái
        private bool isCollected = false;
        private bool isReading = false;
        private bool playerInRange = false;
        private Transform playerTransform;
        private Light glowLight;

        // UI
        private GUIStyle titleStyle;
        private GUIStyle contentStyle;
        private GUIStyle authorStyle;
        private GUIStyle promptStyle;
        private GUIStyle promptShadowStyle;
        private GUIStyle closeStyle;
        private Texture2D bgTexture;
        private Texture2D paperTexture;
        private float readingAlpha = 0f;

        // ═══════════════════════════════════════════════════════════════
        // KHỞI TẠO
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            // Đảm bảo collider là trigger
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            // Tạo hiệu ứng phát sáng
            if (glowEffect)
            {
                CreateGlowLight();
            }

            // Tạo texture nền cho UI đọc ghi chú
            bgTexture = new Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.85f));
            bgTexture.Apply();

            paperTexture = new Texture2D(1, 1);
            paperTexture.SetPixel(0, 0, new Color(0.92f, 0.87f, 0.75f, 0.95f));
            paperTexture.Apply();

            // Kiểm tra đã thu thập chưa
            if (PlayerPrefs.GetInt("LoreNote_" + noteID, 0) == 1)
            {
                isCollected = true;
                gameObject.SetActive(false);
            }
        }

        private void CreateGlowLight()
        {
            GameObject lightObj = new GameObject("LoreNote_Glow");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = Vector3.up * 0.2f;

            glowLight = lightObj.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.range = 3f;
            glowLight.intensity = 0.4f;
            glowLight.color = new Color(1f, 0.9f, 0.6f); // Ánh sáng vàng ấm giấy cũ
            glowLight.shadows = LightShadows.None;
        }

        // ═══════════════════════════════════════════════════════════════
        // TRIGGER & UPDATE
        // ═══════════════════════════════════════════════════════════════

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            playerInRange = true;
            playerTransform = other.transform;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            playerInRange = false;
        }

        private void Update()
        {
            if (isCollected) return;

            // Hiệu ứng phát sáng nhấp nháy
            if (glowLight != null)
            {
                float pulse = 0.3f + Mathf.Sin(Time.time * 2f) * 0.15f;
                glowLight.intensity = pulse;
            }

            // Xoay nhẹ ghi chú
            transform.Rotate(Vector3.up * 25f * Time.deltaTime, Space.World);

            // Kiểm tra input
            if (playerInRange && !isReading)
            {
                if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;
                if (HorrorGame.Cutscenes.CinematicTrailerDirector.IsTrailerRunning) return;

                if (Input.GetKeyDown(KeyCode.E))
                {
                    StartReading();
                }
            }
            else if (isReading)
            {
                // Fade in
                readingAlpha = Mathf.MoveTowards(readingAlpha, 1f, Time.unscaledDeltaTime * 3f);

                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
                {
                    StopReading();
                }
            }
        }

        private void StartReading()
        {
            isReading = true;
            readingAlpha = 0f;

            // Tạm dừng game
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Phát âm thanh
            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, 0.7f);
        }

        private void StopReading()
        {
            isReading = false;
            readingAlpha = 0f;

            // Thu thập ghi chú
            isCollected = true;
            PlayerPrefs.SetInt("LoreNote_" + noteID, 1);
            PlayerPrefs.Save();

            // Khôi phục game
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Debug.Log(string.Format("[LoreNote] 📜 Thu thập Ghi Chú #{0}: {1}", noteID, noteTitle));

            // Ẩn ghi chú
            gameObject.SetActive(false);
        }

        // ═══════════════════════════════════════════════════════════════
        // UI
        // ═══════════════════════════════════════════════════════════════

        private void OnGUI()
        {
            if (isCollected) return;

            // Prompt nhấn E
            if (playerInRange && !isReading)
            {
                DrawPrompt();
            }

            // Giao diện đọc ghi chú toàn màn hình
            if (isReading)
            {
                DrawReadingUI();
            }
        }

        private void DrawPrompt()
        {
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;
            if (HorrorGame.Cutscenes.CinematicTrailerDirector.IsTrailerRunning) return;

            if (promptStyle == null)
            {
                promptStyle = new GUIStyle(GUI.skin.box);
                promptStyle.fontSize = 18;
                promptStyle.fontStyle = FontStyle.Bold;
                promptStyle.alignment = TextAnchor.MiddleCenter;
                promptStyle.normal.textColor = new Color(1f, 0.9f, 0.5f);
            }
            if (promptShadowStyle == null)
            {
                promptShadowStyle = new GUIStyle(promptStyle);
                promptShadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
            }

            string text = string.Format("Nhấn [E] để đọc: {0}", noteTitle);
            float w = 400f;
            float h = 35f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height * 0.7f;

            GUI.Label(new Rect(x + 2, y + 2, w, h), text, promptShadowStyle);
            GUI.Box(new Rect(x, y, w, h), text, promptStyle);
        }

        private void DrawReadingUI()
        {
            if (readingAlpha <= 0f) return;

            Color prevColor = GUI.color;

            // Nền đen mờ
            GUI.color = new Color(0f, 0f, 0f, 0.85f * readingAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), bgTexture);

            // Khung giấy
            float paperW = Mathf.Min(600f, Screen.width * 0.8f);
            float paperH = Mathf.Min(500f, Screen.height * 0.75f);
            float paperX = (Screen.width - paperW) * 0.5f;
            float paperY = (Screen.height - paperH) * 0.5f;

            GUI.color = new Color(0.92f, 0.87f, 0.75f, 0.95f * readingAlpha);
            GUI.DrawTexture(new Rect(paperX, paperY, paperW, paperH), paperTexture);

            GUI.color = new Color(1f, 1f, 1f, readingAlpha);

            // Tiêu đề
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.fontSize = 24;
                titleStyle.fontStyle = FontStyle.Bold;
                titleStyle.alignment = TextAnchor.MiddleCenter;
                titleStyle.normal.textColor = new Color(0.3f, 0.15f, 0.05f);
                titleStyle.wordWrap = true;
            }
            titleStyle.normal.textColor = new Color(0.3f, 0.15f, 0.05f, readingAlpha);
            GUI.Label(new Rect(paperX + 20, paperY + 20, paperW - 40, 40), noteTitle, titleStyle);

            // Dòng kẻ ngang
            GUI.color = new Color(0.5f, 0.35f, 0.2f, 0.5f * readingAlpha);
            GUI.DrawTexture(new Rect(paperX + 40, paperY + 65, paperW - 80, 2), bgTexture);

            // Nội dung
            if (contentStyle == null)
            {
                contentStyle = new GUIStyle(GUI.skin.label);
                contentStyle.fontSize = 16;
                contentStyle.alignment = TextAnchor.UpperLeft;
                contentStyle.normal.textColor = new Color(0.2f, 0.15f, 0.1f);
                contentStyle.wordWrap = true;
                contentStyle.padding = new RectOffset(10, 10, 5, 5);
            }
            contentStyle.normal.textColor = new Color(0.2f, 0.15f, 0.1f, readingAlpha);
            GUI.Label(new Rect(paperX + 30, paperY + 80, paperW - 60, paperH - 170), noteContent, contentStyle);

            // Tác giả
            if (authorStyle == null)
            {
                authorStyle = new GUIStyle(GUI.skin.label);
                authorStyle.fontSize = 14;
                authorStyle.fontStyle = FontStyle.Italic;
                authorStyle.alignment = TextAnchor.LowerRight;
                authorStyle.normal.textColor = new Color(0.4f, 0.3f, 0.2f);
            }
            authorStyle.normal.textColor = new Color(0.4f, 0.3f, 0.2f, readingAlpha);
            GUI.Label(new Rect(paperX + 30, paperY + paperH - 80, paperW - 60, 30), noteAuthor, authorStyle);

            // Nút đóng
            if (closeStyle == null)
            {
                closeStyle = new GUIStyle(GUI.skin.label);
                closeStyle.fontSize = 14;
                closeStyle.alignment = TextAnchor.MiddleCenter;
                closeStyle.normal.textColor = new Color(0.5f, 0.4f, 0.3f);
            }
            closeStyle.normal.textColor = new Color(0.5f, 0.4f, 0.3f, readingAlpha * (0.5f + Mathf.Sin(Time.unscaledTime * 2f) * 0.3f));
            GUI.Label(new Rect(paperX, paperY + paperH - 35, paperW, 25), "Nhấn [ESC] hoặc [E] để đóng", closeStyle);

            GUI.color = prevColor;
        }

        // ═══════════════════════════════════════════════════════════════
        // GIZMOS
        // ═══════════════════════════════════════════════════════════════

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.4f, 0.6f);
            Gizmos.DrawSphere(transform.position, 0.2f);
            Gizmos.DrawWireSphere(transform.position, interactDistance);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f,
                string.Format("📜 Note #{0}: {1}", noteID, noteTitle));
#endif
        }
    }
}

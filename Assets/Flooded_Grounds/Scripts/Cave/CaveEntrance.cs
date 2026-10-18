using UnityEngine;

namespace HorrorGame.Cave
{
    /// <summary>
    /// CaveEntrance — Cửa hang: Trigger vào/ra hang động.
    /// Đặt một Collider (isTrigger=true) tại cửa hang.
    /// Khi player đứng trong trigger và nhấn [E] → vào hang.
    /// Khi player đang trong hang và chạm trigger cửa ra → ra ngoài.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CaveEntrance : MonoBehaviour
    {
        [Header("── Cấu Hình Cửa Hang ──")]
        [Tooltip("CaveZone mà cửa hang này dẫn vào")]
        public CaveZone targetCave;

        [Tooltip("Đây là cửa VÀO hay cửa RA?")]
        public EntranceType entranceType = EntranceType.Entrance;

        [Tooltip("Vị trí player sẽ được teleport tới khi vào hang")]
        public Transform spawnPointInside;

        [Tooltip("Vị trí player sẽ được teleport tới khi ra ngoài")]
        public Transform spawnPointOutside;

        [Header("── Hiển Thị ──")]
        [Tooltip("Text prompt hiển thị khi đứng gần cửa hang")]
        public string promptEnter = "Nhấn [E] để vào hang";
        public string promptExit = "Nhấn [E] để rời hang";
        public string promptLocked = "Cửa hang bị khóa!";

        [Header("── Âm Thanh ──")]
        public AudioClip enterSound;
        public AudioClip exitSound;
        public AudioClip lockedSound;

        [Header("── Hiệu Ứng Bên Ngoài ──")]
        [Tooltip("Particle effect tại cửa hang (hơi nước bốc lên, gió thổi ra...)")]
        public ParticleSystem entranceParticles;

        [Tooltip("Point Light yếu tại cửa hang (gợi ý có hang)")]
        public Light entranceHintLight;

        // Trạng thái runtime
        private bool playerInRange = false;
        private Transform playerTransform;
        private CharacterController playerController;
        private string currentPrompt = "";
        private bool canEnter = true;

        // UI
        private GUIStyle promptStyle;
        private GUIStyle promptShadowStyle;
        private GUIStyle lockedStyle;

        // ═══════════════════════════════════════════════════════════════
        // KHỞI TẠO
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            // Đảm bảo collider là trigger
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            // Tự động tạo collider nếu chưa có
            if (col == null)
            {
                BoxCollider box = gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(4f, 4f, 2f);
            }

            // Hiệu ứng gợi ý tại cửa hang
            if (entranceHintLight == null && entranceType == EntranceType.Entrance)
            {
                CreateHintLight();
            }
        }

        private void CreateHintLight()
        {
            GameObject lightObj = new GameObject("CaveEntrance_HintLight");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = Vector3.up * 1.5f;

            entranceHintLight = lightObj.AddComponent<Light>();
            entranceHintLight.type = LightType.Point;
            entranceHintLight.range = 8f;
            entranceHintLight.intensity = 0.6f;
            entranceHintLight.color = new Color(0.4f, 0.55f, 0.3f); // Xanh lục mờ huyền bí
            entranceHintLight.shadows = LightShadows.None;
        }

        // ═══════════════════════════════════════════════════════════════
        // TRIGGER DETECTION
        // ═══════════════════════════════════════════════════════════════

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            playerInRange = true;
            playerTransform = other.transform;

            // Lấy CharacterController (có thể ở parent)
            playerController = other.GetComponent<CharacterController>();
            if (playerController == null)
                playerController = other.GetComponentInParent<CharacterController>();

            UpdatePrompt();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            playerInRange = false;
            playerTransform = null;
            currentPrompt = "";
        }

        // ═══════════════════════════════════════════════════════════════
        // UPDATE — INPUT
        // ═══════════════════════════════════════════════════════════════

        private void Update()
        {
            if (!playerInRange) return;

            // Không tương tác trong cutscene hoặc trailer
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;
            if (HorrorGame.Cutscenes.CinematicTrailerDirector.IsTrailerRunning) return;

            // Nhấn E để vào/ra hang
            if (Input.GetKeyDown(KeyCode.E))
            {
                HandleInteraction();
            }

            // Nhấp nháy hint light
            if (entranceHintLight != null)
            {
                float flicker = 0.5f + Mathf.Sin(Time.time * 2f) * 0.15f;
                entranceHintLight.intensity = flicker;
            }
        }

        private void HandleInteraction()
        {
            if (targetCave == null) return;

            if (entranceType == EntranceType.Entrance)
            {
                // Đang ngoài trời → vào hang
                if (CaveManager.IsPlayerInCave) return; // Đã trong hang rồi

                string reason;
                if (!targetCave.CanEnter(out reason))
                {
                    // Không đủ điều kiện
                    currentPrompt = reason;
                    canEnter = false;
                    PlaySound(lockedSound);
                    return;
                }

                // Vào hang!
                PlaySound(enterSound);
                TeleportPlayer(spawnPointInside);
                CaveManager.Instance.EnterCave(targetCave);
            }
            else // Exit
            {
                // Đang trong hang → ra ngoài
                if (!CaveManager.IsPlayerInCave) return;

                PlaySound(exitSound);
                TeleportPlayer(spawnPointOutside);
                CaveManager.Instance.ExitCave();
            }
        }

        private void TeleportPlayer(Transform destination)
        {
            if (playerTransform == null || destination == null) return;

            // Tắt CharacterController tạm thời để teleport (CC chặn position change)
            if (playerController != null)
                playerController.enabled = false;

            playerTransform.position = destination.position;
            playerTransform.rotation = destination.rotation;

            if (playerController != null)
                playerController.enabled = true;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, transform.position, 0.8f);
        }

        private void UpdatePrompt()
        {
            if (entranceType == EntranceType.Entrance)
            {
                if (targetCave != null)
                {
                    string reason;
                    if (targetCave.CanEnter(out reason))
                    {
                        currentPrompt = string.Format("{0}\n({1})", promptEnter, targetCave.caveName);
                        canEnter = true;
                    }
                    else
                    {
                        currentPrompt = reason;
                        canEnter = false;
                    }
                }
            }
            else
            {
                currentPrompt = promptExit;
                canEnter = true;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // UI — PROMPT
        // ═══════════════════════════════════════════════════════════════

        private void OnGUI()
        {
            if (!playerInRange || string.IsNullOrEmpty(currentPrompt)) return;
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;
            if (HorrorGame.Cutscenes.CinematicTrailerDirector.IsTrailerRunning) return;

            // Style chính
            if (promptStyle == null)
            {
                promptStyle = new GUIStyle(GUI.skin.box);
                promptStyle.fontSize = 20;
                promptStyle.fontStyle = FontStyle.Bold;
                promptStyle.alignment = TextAnchor.MiddleCenter;
                promptStyle.wordWrap = true;
                promptStyle.padding = new RectOffset(15, 15, 8, 8);
            }

            if (promptShadowStyle == null)
            {
                promptShadowStyle = new GUIStyle(promptStyle);
                promptShadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.7f);
            }

            // Màu sắc dựa trên trạng thái
            if (canEnter)
            {
                promptStyle.normal.textColor = new Color(0.9f, 0.95f, 1f); // Trắng xanh
            }
            else
            {
                promptStyle.normal.textColor = new Color(1f, 0.4f, 0.3f); // Đỏ
            }

            float width = 420f;
            float height = 55f;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height * 0.68f;

            // Shadow
            GUI.Label(new Rect(x + 2, y + 2, width, height), currentPrompt, promptShadowStyle);
            // Main
            GUI.Box(new Rect(x, y, width, height), currentPrompt, promptStyle);
        }

        // ═══════════════════════════════════════════════════════════════
        // GIZMOS
        // ═══════════════════════════════════════════════════════════════

        private void OnDrawGizmos()
        {
            Gizmos.color = entranceType == EntranceType.Entrance ?
                new Color(0f, 0.8f, 1f, 0.4f) : new Color(0f, 1f, 0.4f, 0.4f);

            Gizmos.DrawWireCube(transform.position, new Vector3(4f, 4f, 2f));

            if (spawnPointInside != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(spawnPointInside.position, 0.3f);
                Gizmos.DrawLine(transform.position, spawnPointInside.position);
            }

            if (spawnPointOutside != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(spawnPointOutside.position, 0.3f);
                Gizmos.DrawLine(transform.position, spawnPointOutside.position);
            }

#if UNITY_EDITOR
            string label = entranceType == EntranceType.Entrance ? "🚪 CỬA VÀO" : "🚪 CỬA RA";
            if (targetCave != null) label += "\n→ " + targetCave.caveName;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, label);
#endif
        }
    }

    public enum EntranceType
    {
        Entrance,   // Cửa vào hang (từ ngoài trời)
        Exit        // Cửa ra khỏi hang (về ngoài trời)
    }
}

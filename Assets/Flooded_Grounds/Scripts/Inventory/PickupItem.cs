using UnityEngine;

namespace HorrorGame.Inventory
{
    [RequireComponent(typeof(Collider))]
    public class PickupItem : MonoBehaviour
    {
        [Header("Thông tin vật phẩm")]
        public ItemData itemData;
        public int amount = 1;

        [Header("Tương tác & Hiệu ứng")]
        public float interactDistance = 2.5f;
        public bool autoRotate = true;
        public bool hoverEffect = true;
        public AudioClip pickupSound;

        private Transform playerTransform;
        private Vector3 basePosition;
        private float randomOffset;
        private bool isPlayerInRange = false;

        private static GUIStyle promptStyle;
        private static GUIStyle shadowStyle;

        private void Start()
        {
            basePosition = transform.position;
            randomOffset = Random.Range(0f, 6.28f);

            // Tự động tìm âm thanh nhặt đồ nếu chưa gán
            if (pickupSound == null)
            {
                pickupSound = Resources.Load<AudioClip>("Sounds/ItemPickup");
            }
        }

        private void Update()
        {
            // Trong suốt phân cảnh Cutscene mở đầu: Không kiểm tra nhặt đồ và không hiển thị
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;

            // Hiệu ứng xoay tròn và nhấp nhô nhẹ để người chơi dễ phát hiện trong bụi cỏ
            if (autoRotate)
            {
                transform.Rotate(Vector3.up * (35f * Time.deltaTime), Space.World);
            }

            if (hoverEffect)
            {
                float newY = basePosition.y + Mathf.Sin(Time.time * 2.2f + randomOffset) * 0.04f;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }

            // Tìm Player
            if (playerTransform == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }

            if (playerTransform == null) return;

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            isPlayerInRange = dist <= interactDistance;

            // Bấm E để nhặt
            if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
            {
                Interact();
            }
        }

        public void Interact()
        {
            if (itemData == null)
            {
                Debug.LogWarning("[PickupItem] itemData chưa được gán trên vật phẩm " + gameObject.name);
                return;
            }

            if (InventoryManager.Instance != null)
            {
                bool success = InventoryManager.Instance.AddItem(itemData, amount);
                if (success)
                {
                    Debug.Log("<color=green>[Nhặt Đồ Thành Công]:</color> " + itemData.itemName + " x" + amount);

                    // Nếu nhặt Đèn pin, tự động mở khóa đèn pin cho người chơi
                    if (itemData.itemID.ToLower().Contains("flashlight") || itemData.itemName.ToLower().Contains("đèn pin"))
                    {
                        var fl = FindObjectOfType<Player.PlayerFlashlight>();
                        if (fl == null)
                        {
                            var p = GameObject.FindGameObjectWithTag("Player");
                            if (p != null) fl = p.AddComponent<Player.PlayerFlashlight>();
                        }
                        if (fl != null)
                        {
                            fl.hasFlashlight = true;
                            fl.ShowNotice("💡 Đã nhặt Đèn pin! Bấm [F] để bật/tắt đèn.");
                        }
                    }

                    // Phát âm thanh nhặt đồ tại vị trí người chơi
                    if (pickupSound != null)
                    {
                        AudioSource.PlayClipAtPoint(pickupSound, transform.position, 1.0f);
                    }

                    Destroy(gameObject);
                }
                else
                {
                    Debug.LogWarning("[PickupItem] Túi đồ đã đầy! Không thể nhặt thêm " + itemData.itemName);
                }
            }
        }

        private void OnGUI()
        {
            // Không hiển thị phím nhắc [E] khi đang trong Cutscene
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;

            // Chỉ hiển thị phím nhắc [E] khi người chơi lại gần
            if (!isPlayerInRange || itemData == null) return;

            if (promptStyle == null)
            {
                promptStyle = new GUIStyle(GUI.skin.box);
                promptStyle.fontSize = 18;
                promptStyle.fontStyle = FontStyle.Bold;
                promptStyle.alignment = TextAnchor.MiddleCenter;
                promptStyle.normal.textColor = Color.white;

                shadowStyle = new GUIStyle(promptStyle);
                shadowStyle.normal.textColor = Color.black;
            }

            string text = string.Format("[E] Nhặt {0}{1}", itemData.itemName, amount > 1 ? " (x" + amount + ")" : "");
            float width = 340f;
            float height = 38f;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height * 0.65f;

            // Vẽ bóng đổ chữ
            GUI.Label(new Rect(x + 1.5f, y + 1.5f, width, height), text, shadowStyle);
            GUI.Box(new Rect(x, y, width, height), text, promptStyle);
        }
    }
}

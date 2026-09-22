using UnityEngine;

namespace HorrorGame.Survival
{
    /// <summary>
    /// Gắn lên đá nằm trên mặt đất. Khi player lại gần + bấm E → nhặt đá vào inventory.
    /// Tương tự PickupItem nhưng chuyên dụng cho đá tài nguyên, có thể respawn.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CollectableStone : MonoBehaviour
    {
        [Header("Thông tin")]
        [Tooltip("ItemData của Đá. Nếu để trống sẽ tự load từ Resources.")]
        public HorrorGame.Inventory.ItemData stoneItemData;
        public int amount = 1;

        [Header("Tương tác")]
        public float interactDistance = 2.5f;

        [Header("Respawn")]
        public bool canRespawn = true;
        public float respawnTime = 180f; // 3 phút

        [Header("Âm thanh")]
        public AudioClip pickupSound;

        // Private
        private Transform playerTransform;
        private bool isPlayerInRange = false;
        private bool isCollected = false;
        private Vector3 originalPosition;
        private Quaternion originalRotation;

        // GUI
        private static GUIStyle promptStyle;
        private static GUIStyle shadowStyle;

        void Start()
        {
            originalPosition = transform.position;
            originalRotation = transform.rotation;

            // Tự load ItemData nếu chưa gán
            if (stoneItemData == null)
            {
                stoneItemData = Resources.Load<HorrorGame.Inventory.ItemData>("StoneItem");
                if (stoneItemData != null)
                    Debug.Log("[CollectableStone] Tự load StoneItem từ Resources.");
            }

            // Tạo visual placeholder nếu object chưa có Renderer
            if (GetComponent<Renderer>() == null && GetComponentInChildren<Renderer>() == null)
            {
                CreatePlaceholderStone();
            }
        }

        void CreatePlaceholderStone()
        {
            // Tạo viên đá bằng sphere dẹp
            GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            stone.transform.SetParent(transform);
            stone.transform.localPosition = Vector3.zero;
            stone.transform.localScale = new Vector3(0.4f, 0.25f, 0.35f);
            stone.GetComponent<Renderer>().material.color = new Color(0.5f, 0.5f, 0.48f); // Xám đá

            // Thêm viên đá nhỏ bên cạnh
            GameObject smallStone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            smallStone.transform.SetParent(transform);
            smallStone.transform.localPosition = new Vector3(0.2f, -0.05f, 0.1f);
            smallStone.transform.localScale = new Vector3(0.2f, 0.15f, 0.18f);
            smallStone.GetComponent<Renderer>().material.color = new Color(0.55f, 0.52f, 0.5f);
        }

        void Update()
        {
            if (isCollected) return;

            // Trong Cutscene thì bỏ qua
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;

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
                CollectStone();
            }
        }

        void CollectStone()
        {
            if (stoneItemData == null)
            {
                Debug.LogWarning("[CollectableStone] stoneItemData chưa được gán!");
                return;
            }

            if (HorrorGame.Inventory.InventoryManager.Instance == null) return;

            bool success = HorrorGame.Inventory.InventoryManager.Instance.AddItem(stoneItemData, amount);
            if (success)
            {
                Debug.Log(string.Format("<color=green>[Nhặt Đá]:</color> {0} x{1}", stoneItemData.itemName, amount));

                // Phát âm thanh
                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position, 1f);
                }

                isCollected = true;

                if (canRespawn)
                {
                    // Ẩn đá, đợi respawn
                    gameObject.SetActive(false);
                    Invoke("RespawnStone", respawnTime);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                Debug.LogWarning("[CollectableStone] Túi đồ đã đầy!");
            }
        }

        void RespawnStone()
        {
            isCollected = false;
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            gameObject.SetActive(true);
            Debug.Log("[CollectableStone] Đá đã xuất hiện lại!");
        }

        void OnGUI()
        {
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;
            if (!isPlayerInRange || isCollected || stoneItemData == null) return;

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

            string text = string.Format("[E] Nhặt {0}{1}", stoneItemData.itemName, amount > 1 ? " (x" + amount + ")" : "");
            float width = 340f;
            float height = 38f;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height * 0.65f;

            GUI.Label(new Rect(x + 1.5f, y + 1.5f, width, height), text, shadowStyle);
            GUI.Box(new Rect(x, y, width, height), text, promptStyle);
        }
    }
}

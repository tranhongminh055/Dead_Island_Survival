using UnityEngine;

namespace HorrorGame.Survival
{
    /// <summary>
    /// Hệ thống Rìu FPS — Gắn vào Camera (cùng vị trí với FPSWeapon).
    /// Phím 2 để rút/cất rìu. Click trái để chặt cây (ChoppableTree).
    /// Tự tạo model rìu placeholder nếu chưa có prefab.
    /// </summary>
    public class AxeController : MonoBehaviour
    {
        [Header("=== KÉO MODEL RÌU VÀO ĐÂY ===")]
        [Tooltip("Kéo thả model rìu 3D vào ô này. Nếu để trống sẽ tạo rìu tạm.")]
        public GameObject axeModelPrefab;

        [Header("Vị trí rìu trên màn hình")]
        public Vector3 axePosition = new Vector3(0.4f, -0.3f, 0.5f);
        public Vector3 axeRotation = new Vector3(0, 180, 0);
        public Vector3 axeScale = new Vector3(0.35f, 0.35f, 0.35f);

        [Header("Thông số chặt cây")]
        public float chopDamage = 25f;      // Sát thương mỗi nhát chặt
        public float chopRange = 3f;        // Tầm chặt
        public float chopCooldown = 0.8f;   // Thời gian giữa mỗi nhát (giây)

        [Header("Tích hợp Túi Đồ")]
        [Tooltip("Kéo ItemData của Rìu vào đây. Nếu để trống sẽ tự load từ Resources.")]
        public HorrorGame.Inventory.ItemData axeItemData;

        [Header("Phím tắt")]
        public KeyCode equipKey = KeyCode.Alpha2; // Phím 2 để rút rìu

        [Header("Âm thanh")]
        public AudioClip chopSound;   // Tiếng chặt gỗ
        public AudioClip equipSound;  // Tiếng rút rìu

        // Private
        private GameObject axeInstance;
        private bool isEquipped = false;
        private float nextChopTime = 0f;
        private AudioSource audioSource;

        // Swing animation state
        private Vector3 currentAxePosition;
        private Vector3 currentAxeRotation;
        private bool isSwinging = false;
        private float swingTimer = 0f;
        private float swingDuration = 0.4f;

        // UI
        private string displayText = "";
        private float displayTimer = 0f;
        private GUIStyle textStyle;
        private GUIStyle shadowStyle;

        void Start()
        {
            // Tự động load ItemData rìu từ Resources nếu chưa gán
            if (axeItemData == null)
            {
                axeItemData = Resources.Load<HorrorGame.Inventory.ItemData>("AxeItem");
                if (axeItemData != null)
                    Debug.Log("[AxeController] Tự động load AxeItem từ Resources thành công.");
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            CreateAxeVisual();

            // Ẩn rìu ban đầu
            if (axeInstance != null) axeInstance.SetActive(false);

            currentAxePosition = axePosition;
            currentAxeRotation = axeRotation;

            Debug.Log("=== HỆ THỐNG RÌU ĐÃ SẴN SÀNG! Bấm phím 2 để rút rìu ===");
        }

        void CreateAxeVisual()
        {
            if (axeModelPrefab != null)
            {
                axeInstance = Instantiate(axeModelPrefab, transform);
            }
            else
            {
                // Tạo rìu placeholder bằng Primitive
                axeInstance = CreatePlaceholderAxe();
            }

            axeInstance.transform.localPosition = axePosition;
            axeInstance.transform.localRotation = Quaternion.Euler(axeRotation);
            axeInstance.transform.localScale = axeScale;

            // Tắt Collider trên rìu
            Collider[] cols = axeInstance.GetComponentsInChildren<Collider>();
            foreach (Collider c in cols) c.enabled = false;
        }

        GameObject CreatePlaceholderAxe()
        {
            GameObject axe = new GameObject("PlaceholderAxe");
            axe.transform.SetParent(transform);

            // Cán rìu (Cylinder dài)
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.transform.SetParent(axe.transform);
            handle.transform.localPosition = new Vector3(0, 0, 0);
            handle.transform.localRotation = Quaternion.Euler(0, 0, 90);
            handle.transform.localScale = new Vector3(0.04f, 0.35f, 0.04f);
            handle.GetComponent<Renderer>().material.color = new Color(0.45f, 0.3f, 0.15f); // Nâu gỗ

            // Lưỡi rìu (Cube dẹp)
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.transform.SetParent(axe.transform);
            blade.transform.localPosition = new Vector3(0.32f, 0, 0);
            blade.transform.localRotation = Quaternion.Euler(0, 0, 15);
            blade.transform.localScale = new Vector3(0.15f, 0.02f, 0.2f);
            blade.GetComponent<Renderer>().material.color = new Color(0.6f, 0.6f, 0.65f); // Xám bạc

            // Phần nêm kẹp lưỡi rìu vào cán
            GameObject wedge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wedge.transform.SetParent(axe.transform);
            wedge.transform.localPosition = new Vector3(0.24f, 0, 0);
            wedge.transform.localScale = new Vector3(0.06f, 0.05f, 0.08f);
            wedge.GetComponent<Renderer>().material.color = new Color(0.35f, 0.25f, 0.12f); // Nâu đậm

            return axe;
        }

        void Update()
        {
            // Không nhận input khi đang Cutscene
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;

            // Không nhận input khi túi đồ đang mở
            if (Inventory.InventoryManager.Instance != null && Inventory.InventoryManager.Instance.isInventoryOpen) return;

            // Phím 2 để rút/cất rìu
            if (Input.GetKeyDown(equipKey))
            {
                TryToggleAxe();
            }

            if (!isEquipped) return;

            // Click trái để chặt
            if (Input.GetMouseButtonDown(0) && Time.time >= nextChopTime && !isSwinging)
            {
                nextChopTime = Time.time + chopCooldown;
                PerformChop();
            }

            // Animation vung rìu
            UpdateSwingAnimation();

            // Giảm timer hiển thị text
            if (displayTimer > 0) displayTimer -= Time.deltaTime;
        }

        void TryToggleAxe()
        {
            // Kiểm tra có rìu trong inventory không
            if (axeItemData != null && Inventory.InventoryManager.Instance != null)
            {
                if (Inventory.InventoryManager.Instance.HasItem(axeItemData))
                {
                    ToggleAxe();
                }
                else
                {
                    displayText = "Không có rìu trong túi!";
                    displayTimer = 2f;
                    Debug.Log("[AxeController] Không có rìu trong túi!");
                }
            }
            else
            {
                // Nếu chưa liên kết inventory, cho rút tự do
                ToggleAxe();
            }
        }

        void ToggleAxe()
        {
            isEquipped = !isEquipped;

            if (axeInstance != null) axeInstance.SetActive(isEquipped);

            // Khi rút rìu, cất súng (nếu có FPSWeapon)
            if (isEquipped)
            {
                FPSWeapon fpsWeapon = GetComponent<FPSWeapon>();
                if (fpsWeapon != null && fpsWeapon.enabled)
                {
                    // Cất súng bằng cách gọi ToggleWeapon nếu đang cầm
                    // Kiểm tra qua reflection hoặc field public
                }

                displayText = "Rìu";
                displayTimer = 2f;

                if (equipSound != null) audioSource.PlayOneShot(equipSound);

                Debug.Log("[AxeController] Đã rút rìu");
            }
            else
            {
                displayText = "Cất rìu";
                displayTimer = 2f;
                Debug.Log("[AxeController] Đã cất rìu");
            }

            // Reset animation
            currentAxePosition = axePosition;
            currentAxeRotation = axeRotation;
            isSwinging = false;
        }

        void PerformChop()
        {
            // Bắt đầu animation vung rìu
            isSwinging = true;
            swingTimer = 0f;

            // Phát âm thanh chặt
            if (chopSound != null) audioSource.PlayOneShot(chopSound);

            // Raycast kiểm tra có trúng cây không
            RaycastHit hit;
            Ray ray = new Ray(transform.position, transform.forward);

            if (Physics.Raycast(ray, out hit, chopRange))
            {
                // Kiểm tra có phải ChoppableTree không
                ChoppableTree tree = hit.transform.GetComponent<ChoppableTree>();
                if (tree == null) tree = hit.transform.GetComponentInParent<ChoppableTree>();

                if (tree != null)
                {
                    tree.TakeChopDamage(chopDamage, hit.point);
                    Debug.Log(string.Format("[AxeController] Chặt trúng cây! Damage: {0}, HP còn: {1}", chopDamage, tree.CurrentHealth));
                }
                else
                {
                    Debug.Log("[AxeController] Chặt trúng: " + hit.transform.name + " (không phải cây)");
                }
            }
        }

        void UpdateSwingAnimation()
        {
            if (axeInstance == null) return;

            if (isSwinging)
            {
                swingTimer += Time.deltaTime;
                float t = swingTimer / swingDuration;

                if (t < 0.4f)
                {
                    // Pha 1: Vung rìu lên (giơ cao)
                    float phase1 = t / 0.4f;
                    currentAxeRotation = Vector3.Lerp(axeRotation, axeRotation + new Vector3(-45f, 0, 20f), phase1);
                    currentAxePosition = Vector3.Lerp(axePosition, axePosition + new Vector3(0, 0.15f, -0.1f), phase1);
                }
                else if (t < 0.7f)
                {
                    // Pha 2: Chém xuống mạnh
                    float phase2 = (t - 0.4f) / 0.3f;
                    currentAxeRotation = Vector3.Lerp(axeRotation + new Vector3(-45f, 0, 20f), axeRotation + new Vector3(60f, 0, -10f), phase2);
                    currentAxePosition = Vector3.Lerp(axePosition + new Vector3(0, 0.15f, -0.1f), axePosition + new Vector3(0, -0.2f, 0.1f), phase2);
                }
                else
                {
                    // Pha 3: Hồi lại vị trí ban đầu
                    float phase3 = (t - 0.7f) / 0.3f;
                    currentAxeRotation = Vector3.Lerp(axeRotation + new Vector3(60f, 0, -10f), axeRotation, phase3);
                    currentAxePosition = Vector3.Lerp(axePosition + new Vector3(0, -0.2f, 0.1f), axePosition, phase3);
                }

                if (t >= 1f)
                {
                    isSwinging = false;
                    currentAxeRotation = axeRotation;
                    currentAxePosition = axePosition;
                }
            }
            else
            {
                // Smooth lerp về vị trí gốc
                currentAxePosition = Vector3.Lerp(currentAxePosition, axePosition, Time.deltaTime * 8f);
                currentAxeRotation = Vector3.Lerp(currentAxeRotation, axeRotation, Time.deltaTime * 8f);
            }

            axeInstance.transform.localPosition = currentAxePosition;
            axeInstance.transform.localRotation = Quaternion.Euler(currentAxeRotation);
        }

        // === UI: Hiển thị tên công cụ và tâm ngắm khi trang bị ===
        void OnGUI()
        {
            if (!isEquipped) return;

            // Tâm ngắm nhỏ (dot)
            float cx = Screen.width / 2f;
            float cy = Screen.height / 2f;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(cx - 2, cy - 2, 4, 4), Texture2D.whiteTexture);

            // Hiển thị tên rìu
            if (displayTimer > 0)
            {
                if (textStyle == null)
                {
                    textStyle = new GUIStyle();
                    textStyle.fontSize = 24;
                    textStyle.fontStyle = FontStyle.Bold;
                    textStyle.normal.textColor = Color.white;
                    textStyle.alignment = TextAnchor.MiddleCenter;

                    shadowStyle = new GUIStyle(textStyle);
                    shadowStyle.normal.textColor = Color.black;
                }

                float alpha = Mathf.Clamp01(displayTimer);
                textStyle.normal.textColor = new Color(1, 1, 1, alpha);
                shadowStyle.normal.textColor = new Color(0, 0, 0, alpha);

                float tx = cx - 150f;
                float ty = Screen.height - 130f;

                GUI.Label(new Rect(tx + 2, ty + 2, 300, 40), displayText, shadowStyle);
                GUI.Label(new Rect(tx, ty, 300, 40), displayText, textStyle);
            }
        }

        /// <summary>
        /// Kiểm tra rìu có đang được trang bị không (dùng bởi BuildingSystem để tránh xung đột)
        /// </summary>
        public bool IsEquipped { get { return isEquipped; } }

        /// <summary>
        /// Cất rìu (gọi từ bên ngoài, ví dụ khi vào build mode)
        /// </summary>
        public void ForceUnequip()
        {
            if (isEquipped) ToggleAxe();
        }
    }
}

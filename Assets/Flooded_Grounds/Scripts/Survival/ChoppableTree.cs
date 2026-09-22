using UnityEngine;

namespace HorrorGame.Survival
{
    /// <summary>
    /// Gắn lên bất kỳ cây nào trong scene để cho phép người chơi chặt cây lấy gỗ và lá.
    /// Cây có HP, bị chặt mỗi nhát trừ HP. Khi hết HP → hiệu ứng đổ cây → spawn gỗ + lá.
    /// Cây sẽ mọc lại sau thời gian respawn.
    /// </summary>
    public class ChoppableTree : MonoBehaviour
    {
        [Header("Thông số cây")]
        public float maxHealth = 100f;
        private float currentHealth;

        [Header("Phần thưởng khi chặt")]
        [Tooltip("ItemData của Gỗ. Nếu để trống sẽ tự load từ Resources.")]
        public HorrorGame.Inventory.ItemData woodItemData;
        public int woodDropAmount = 3;

        [Tooltip("ItemData của Lá cây. Nếu để trống sẽ tự load từ Resources.")]
        public HorrorGame.Inventory.ItemData leafItemData;
        public int leafDropAmount = 2;

        [Header("Hiệu ứng")]
        public float fallDuration = 2f;        // Thời gian cây đổ (giây)
        public float despawnDelay = 3f;         // Sau bao lâu cây biến mất sau khi đổ
        public float respawnTime = 300f;        // Thời gian cây mọc lại (giây, mặc định 5 phút)
        public bool canRespawn = true;

        [Header("Âm thanh")]
        public AudioClip chopHitSound;          // Tiếng chặt trúng
        public AudioClip treeFallSound;         // Tiếng cây đổ

        // Trạng thái
        private bool isDead = false;
        private bool isFalling = false;
        private float fallTimer = 0f;
        private Vector3 fallDirection;
        private Quaternion originalRotation;
        private Vector3 originalPosition;
        private Vector3 originalScale;

        // Shake effect
        private bool isShaking = false;
        private float shakeTimer = 0f;
        private float shakeDuration = 0.3f;
        private float shakeIntensity = 0.05f;
        private Vector3 shakeOriginPos;

        // HP Bar hiển thị
        private bool showHealthBar = false;
        private float healthBarTimer = 0f;
        private float healthBarDisplayTime = 3f;

        // Camera ref (để kiểm tra player nhìn vào cây)
        private Transform playerCamera;

        public float CurrentHealth { get { return currentHealth; } }

        void Start()
        {
            currentHealth = maxHealth;
            originalRotation = transform.rotation;
            originalPosition = transform.position;
            originalScale = transform.localScale;

            // Load item data từ Resources nếu chưa gán
            if (woodItemData == null)
            {
                woodItemData = Resources.Load<HorrorGame.Inventory.ItemData>("WoodItem");
            }
            if (leafItemData == null)
            {
                leafItemData = Resources.Load<HorrorGame.Inventory.ItemData>("LeafItem");
            }
        }

        void Update()
        {
            if (isDead) return;

            // Xử lý rung cây khi bị chặt
            if (isShaking)
            {
                shakeTimer += Time.deltaTime;
                if (shakeTimer < shakeDuration)
                {
                    float x = Random.Range(-shakeIntensity, shakeIntensity);
                    float z = Random.Range(-shakeIntensity, shakeIntensity);
                    transform.position = shakeOriginPos + new Vector3(x, 0, z);
                }
                else
                {
                    transform.position = shakeOriginPos;
                    isShaking = false;
                }
            }

            // Xử lý cây đổ
            if (isFalling)
            {
                fallTimer += Time.deltaTime;
                float t = Mathf.Clamp01(fallTimer / fallDuration);

                // Xoay cây ngã dần (quay quanh gốc cây)
                // Dùng easing function để cây ngã nhanh dần (như trọng lực)
                float easedT = t * t; // Quadratic ease-in
                float angle = Mathf.Lerp(0f, 85f, easedT);
                transform.rotation = originalRotation * Quaternion.AngleAxis(angle, fallDirection);

                if (t >= 1f)
                {
                    isFalling = false;
                    // Spawn phần thưởng
                    SpawnDrops();

                    // Biến mất sau delay
                    Invoke("DespawnTree", despawnDelay);
                }
            }

            // Giảm timer HP bar
            if (healthBarTimer > 0) healthBarTimer -= Time.deltaTime;
            else showHealthBar = false;
        }

        /// <summary>
        /// Gọi bởi AxeController khi chặt trúng cây
        /// </summary>
        public void TakeChopDamage(float damage, Vector3 hitPoint)
        {
            if (isDead) return;

            currentHealth -= damage;
            showHealthBar = true;
            healthBarTimer = healthBarDisplayTime;

            // Phát âm thanh chặt
            if (chopHitSound != null)
            {
                AudioSource.PlayClipAtPoint(chopHitSound, hitPoint, 1f);
            }

            // Hiệu ứng rung cây
            StartShake();

            Debug.Log(string.Format("[ChoppableTree] {0} bị chặt! HP: {1}/{2}", gameObject.name, currentHealth, maxHealth));

            // Kiểm tra chết
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                StartFalling(hitPoint);
            }
        }

        void StartShake()
        {
            isShaking = true;
            shakeTimer = 0f;
            shakeOriginPos = transform.position;
        }

        void StartFalling(Vector3 hitPoint)
        {
            isDead = true;
            isFalling = true;
            fallTimer = 0f;

            // Hướng ngã: đối diện với hướng chặt (cây đổ ngược lại hướng chặt)
            Vector3 hitDir = (hitPoint - transform.position);
            hitDir.y = 0;
            if (hitDir.sqrMagnitude < 0.01f)
            {
                hitDir = Vector3.forward; // Fallback
            }
            hitDir.Normalize();

            // Tính trục xoay (vuông góc với hướng ngã và trục Y)
            fallDirection = Vector3.Cross(Vector3.up, hitDir).normalized;

            // Phát tiếng cây đổ
            if (treeFallSound != null)
            {
                AudioSource.PlayClipAtPoint(treeFallSound, transform.position, 1f);
            }

            // Tắt collider để không chặn player
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Tắt collider con
            Collider[] childCols = GetComponentsInChildren<Collider>();
            foreach (Collider c in childCols) c.enabled = false;

            Debug.Log(string.Format("[ChoppableTree] {0} đổ!", gameObject.name));
        }

        void SpawnDrops()
        {
            Vector3 dropPos = transform.position + Vector3.up * 0.5f;

            // Spawn gỗ
            if (woodItemData != null && woodDropAmount > 0)
            {
                for (int i = 0; i < woodDropAmount; i++)
                {
                    SpawnDropItem(woodItemData, dropPos + Random.insideUnitSphere * 1.5f);
                }
            }

            // Spawn lá cây
            if (leafItemData != null && leafDropAmount > 0)
            {
                for (int i = 0; i < leafDropAmount; i++)
                {
                    SpawnDropItem(leafItemData, dropPos + Random.insideUnitSphere * 2f);
                }
            }

            Debug.Log(string.Format("[ChoppableTree] Spawn: {0} gỗ + {1} lá", woodDropAmount, leafDropAmount));
        }

        void SpawnDropItem(HorrorGame.Inventory.ItemData itemData, Vector3 position)
        {
            // Đảm bảo drop trên mặt đất
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 5f, Vector3.down, out hit, 20f))
            {
                position = hit.point + Vector3.up * 0.3f;
            }

            // Tạo GameObject cho drop item
            GameObject dropObj;

            // Nếu item có prefab 3D, dùng prefab đó
            if (itemData.itemPrefab != null)
            {
                dropObj = Instantiate(itemData.itemPrefab, position, Random.rotation);
            }
            else
            {
                // Tạo placeholder dựa trên loại item
                dropObj = CreateDropPlaceholder(itemData, position);
            }

            dropObj.name = "Drop_" + itemData.itemName;

            // Gắn PickupItem component
            if (dropObj.GetComponent<HorrorGame.Inventory.PickupItem>() == null)
            {
                var pickup = dropObj.AddComponent<HorrorGame.Inventory.PickupItem>();
                pickup.itemData = itemData;
                pickup.amount = 1;
                pickup.interactDistance = 2.5f;
                pickup.autoRotate = true;
                pickup.hoverEffect = true;
            }

            // Đảm bảo có Collider (cần cho PickupItem)
            if (dropObj.GetComponent<Collider>() == null)
            {
                BoxCollider box = dropObj.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = Vector3.one * 0.5f;
            }
        }

        GameObject CreateDropPlaceholder(HorrorGame.Inventory.ItemData itemData, Vector3 position)
        {
            GameObject obj = new GameObject(itemData.itemName);
            obj.transform.position = position;

            string id = itemData.itemID != null ? itemData.itemID.ToLower() : "";

            if (id.Contains("wood") || id.Contains("go"))
            {
                // Khúc gỗ nhỏ
                GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                log.transform.SetParent(obj.transform);
                log.transform.localPosition = Vector3.zero;
                log.transform.localRotation = Quaternion.Euler(0, 0, 90);
                log.transform.localScale = new Vector3(0.1f, 0.25f, 0.1f);
                log.GetComponent<Renderer>().material.color = new Color(0.55f, 0.35f, 0.15f); // Nâu gỗ
            }
            else if (id.Contains("leaf") || id.Contains("la"))
            {
                // Bó lá cây (cube dẹp xanh)
                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leaf.transform.SetParent(obj.transform);
                leaf.transform.localPosition = Vector3.zero;
                leaf.transform.localScale = new Vector3(0.3f, 0.05f, 0.3f);
                leaf.GetComponent<Renderer>().material.color = new Color(0.2f, 0.55f, 0.15f); // Xanh lá
            }
            else
            {
                // Mặc định: cube nhỏ
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(obj.transform);
                cube.transform.localPosition = Vector3.zero;
                cube.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                cube.GetComponent<Renderer>().material.color = Color.gray;
            }

            return obj;
        }

        void DespawnTree()
        {
            if (canRespawn)
            {
                // Ẩn cây, đợi respawn
                gameObject.SetActive(false);
                Invoke("RespawnTree", respawnTime);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void RespawnTree()
        {
            // Hồi sinh cây
            currentHealth = maxHealth;
            isDead = false;
            isFalling = false;
            fallTimer = 0f;
            isShaking = false;

            transform.position = originalPosition;
            transform.rotation = originalRotation;
            transform.localScale = originalScale;

            // Bật lại collider
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
            Collider[] childCols = GetComponentsInChildren<Collider>();
            foreach (Collider c in childCols) c.enabled = true;

            gameObject.SetActive(true);
            Debug.Log(string.Format("[ChoppableTree] {0} đã mọc lại!", gameObject.name));
        }

        // === UI: Thanh HP cây ===
        void OnGUI()
        {
            if (!showHealthBar || isDead) return;

            // Kiểm tra player camera
            if (playerCamera == null)
            {
                Camera cam = Camera.main;
                if (cam != null) playerCamera = cam.transform;
            }
            if (playerCamera == null) return;

            // Kiểm tra khoảng cách
            float dist = Vector3.Distance(transform.position, playerCamera.position);
            if (dist > 8f) return;

            // Chuyển vị trí 3D sang 2D màn hình
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 3f);
            if (screenPos.z < 0) return; // Cây ở phía sau camera

            float barWidth = 80f;
            float barHeight = 10f;
            float x = screenPos.x - barWidth / 2f;
            float y = Screen.height - screenPos.y - barHeight / 2f;

            // Nền đen
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(x - 1, y - 1, barWidth + 2, barHeight + 2), Texture2D.whiteTexture);

            // Thanh HP (xanh → vàng → đỏ)
            float hpPercent = currentHealth / maxHealth;
            Color hpColor;
            if (hpPercent > 0.5f)
                hpColor = Color.Lerp(Color.yellow, Color.green, (hpPercent - 0.5f) * 2f);
            else
                hpColor = Color.Lerp(Color.red, Color.yellow, hpPercent * 2f);

            GUI.color = hpColor;
            GUI.DrawTexture(new Rect(x, y, barWidth * hpPercent, barHeight), Texture2D.whiteTexture);

            // Tên cây
            GUI.color = Color.white;
            GUIStyle nameStyle = new GUIStyle();
            nameStyle.fontSize = 12;
            nameStyle.normal.textColor = Color.white;
            nameStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(x - 20, y - 18, barWidth + 40, 16), gameObject.name, nameStyle);

            GUI.color = Color.white;
        }
    }
}

using UnityEngine;

namespace HorrorGame.Survival
{
    /// <summary>
    /// Gắn lên bất kỳ cây nào trong scene để cho phép người chơi chặt cây lấy gỗ và lá.
    /// Cây có HP, bị chặt mỗi nhát trừ HP. Khi hết HP → hiệu ứng đổ cây → spawn gỗ + lá.
    /// Cây sẽ mọc lại sau thời gian respawn.
    ///
    /// HIỆU ỨNG TRỰC QUAN:
    /// - Rung cây mạnh mỗi nhát chặt + vụn gỗ/vỏ cây bắn ra
    /// - Lá cây rơi lả tả mỗi lần chặt
    /// - Tiếng kêu răng rắc trước khi đổ
    /// - Cây đổ chậm rồi nhanh dần (trọng lực thực tế)
    /// - Bụi bay + mảnh gỗ văng khi cây chạm đất
    /// - Camera rung khi cây đổ gần player
    /// - Thanh HP hiển thị trên đầu cây
    /// </summary>
    public class ChoppableTree : MonoBehaviour
    {
        [Header("Thông số cây")]
        public float maxHealth = 100f;
        private float currentHealth;

        [Tooltip("Bán kính thân cây cơ bản (mét)")]
        public float trunkRadius = 0.45f;
        [Tooltip("Chiều cao ước tính của cây (mét)")]
        public float treeHeight = 12f;

        [Header("Phần thưởng khi chặt")]
        [Tooltip("ItemData của Gỗ. Nếu để trống sẽ tự load từ Resources.")]
        public HorrorGame.Inventory.ItemData woodItemData;
        public int woodDropAmount = 3;

        [Tooltip("ItemData của Lá cây. Nếu để trống sẽ tự load từ Resources.")]
        public HorrorGame.Inventory.ItemData leafItemData;
        public int leafDropAmount = 2;

        [Header("Hiệu ứng đổ cây")]
        public float fallDuration = 2.6f;        // Thời gian cây đổ (giây)
        public float despawnDelay = 3f;           // Sau bao lâu tán cây biến mất sau khi đổ
        public float respawnTime = 300f;          // Thời gian cây mọc lại (giây, mặc định 5 phút)
        public bool canRespawn = true;

        [Header("Âm thanh")]
        public AudioClip chopHitSound;            // Tiếng chặt trúng
        public AudioClip treeFallSound;           // Tiếng cây đổ
        public AudioClip treeCreakSound;          // Tiếng cây kêu răng rắc
        public AudioClip groundImpactSound;       // Tiếng va đất

        // ═══════════════════════════════════════════════════════
        // TRẠNG THÁI NỘI BỘ
        // ═══════════════════════════════════════════════════════
        private bool isDead = false;
        private bool isFalling = false;
        private float fallTimer = 0f;
        private Vector3 fallDirection;       // Trục xoay (world space)
        private Vector3 fallAwayDir;         // Hướng cây đổ (world space, XZ) ra xa người chơi
        private Quaternion originalRotation;
        private Vector3 originalPosition;
        private Vector3 originalScale;
        private bool groundImpactDone = false;

        // Stump & Visuals
        private GameObject spawnedStump;
        private GameObject cutNotchObj;
        private float stumpHeight = 0.85f;

        // Shake effect
        private bool isShaking = false;
        private float shakeTimer = 0f;
        private float shakeDuration = 0.35f;
        private float shakeIntensity = 0.09f;
        private Vector3 shakeOriginPos;

        // Creak delay trước khi đổ
        private bool isCreaking = false;
        private float creakTimer = 0f;
        private float creakDuration = 0.7f;   // Dừng 0.7s rung dữ dội và kêu răng rắc trước khi đổ

        // HP Bar hiển thị
        private bool showHealthBar = false;
        private float healthBarTimer = 0f;
        private float healthBarDisplayTime = 4f;

        // Camera ref
        private Transform playerCamera;

        // Chop count để hiệu ứng tăng dần
        private int chopCount = 0;

        public float CurrentHealth { get { return currentHealth; } }

        // ═══════════════════════════════════════════════════════
        // KHỞI TẠO
        // ═══════════════════════════════════════════════════════

        void Awake()
        {
            currentHealth = maxHealth;
            originalRotation = transform.rotation;
            originalPosition = transform.position;
            originalScale = transform.localScale;

            // Load item data từ Resources nếu chưa gán
            if (woodItemData == null)
            {
                woodItemData = Resources.Load<HorrorGame.Inventory.ItemData>("Items/WoodItem");
                if (woodItemData == null) woodItemData = Resources.Load<HorrorGame.Inventory.ItemData>("WoodItem");
            }
            if (leafItemData == null)
            {
                leafItemData = Resources.Load<HorrorGame.Inventory.ItemData>("Items/LeafItem");
                if (leafItemData == null) leafItemData = Resources.Load<HorrorGame.Inventory.ItemData>("LeafItem");
            }

            // Tạo runtime nếu chưa có
            if (woodItemData == null) woodItemData = CreateRuntimeItem("wood", "Gỗ", "Gỗ thu hoạch từ cây. Dùng để xây nhà, hàng rào và lửa trại.");
            if (leafItemData == null) leafItemData = CreateRuntimeItem("leaf", "Lá Cây", "Lá cây rơi khi chặt cây. Dùng để dựng lều lá.");

            // Tự động nạp âm thanh chặt và đổ cây đầy đủ
#if UNITY_EDITOR
            if (chopHitSound == null)
            {
                chopHitSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/sound axe chop tree/âm tahh rìu khi chặt cây.mp3");
                if (chopHitSound == null)
                    chopHitSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/sound axe chop tree/wings_of_freedom-chopping-wood-435769.mp3");
                if (chopHitSound == null)
                    chopHitSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/sound axe chop tree/Chopping wood - Sound effects - Jeff Hottman.mp3");
            }
            if (treeCreakSound == null)
            {
                treeCreakSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/sound axe chop tree/Chopping wood - Sound effects - Jeff Hottman.mp3");
                if (treeCreakSound == null)
                    treeCreakSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/Taps.mp3");
            }
            if (treeFallSound == null)
            {
                treeFallSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/LeafRustle.mp3");
                if (treeFallSound == null)
                    treeFallSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/DeepRattle.mp3");
            }
            if (groundImpactSound == null)
            {
                groundImpactSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Flight_Cabin-Sound/realistic_bomb_crash.wav");
                if (groundImpactSound == null)
                    groundImpactSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/DeepRattle.mp3");
            }
#endif
        }

        private static HorrorGame.Inventory.ItemData runtimeWood;
        private static HorrorGame.Inventory.ItemData runtimeLeaf;

        static HorrorGame.Inventory.ItemData CreateRuntimeItem(string id, string displayName, string desc)
        {
            if (id == "wood" && runtimeWood != null) return runtimeWood;
            if (id == "leaf" && runtimeLeaf != null) return runtimeLeaf;

            var item = ScriptableObject.CreateInstance<HorrorGame.Inventory.ItemData>();
            item.itemID = id;
            item.itemName = displayName;
            item.description = desc;
            item.itemType = HorrorGame.Inventory.ItemType.Resource;
            item.isStackable = true;
            item.maxStack = 99;

            if (id == "wood") runtimeWood = item; else runtimeLeaf = item;
            return item;
        }

        // ═══════════════════════════════════════════════════════
        // VÒNG LẶP CHÍNH
        // ═══════════════════════════════════════════════════════

        void Update()
        {
            if (isDead && !isFalling && !isCreaking) return;

            // Rung cây khi bị chém
            UpdateShake();

            // Giai đoạn kêu răng rắc trước khi đổ
            if (isCreaking)
            {
                creakTimer += Time.deltaTime;

                // Rung lắc rung giật tăng dần
                float freq = 35f + creakTimer * 20f;
                float creakShake = Mathf.Sin(creakTimer * freq) * 0.05f * (creakTimer / creakDuration);
                transform.position = shakeOriginPos + new Vector3(creakShake, 0, creakShake);

                // Bắn mảnh vụn ở khớp gãy
                if (Random.value < 0.25f)
                {
                    SpawnBarkChips(originalPosition + Vector3.up * stumpHeight, 3);
                }

                if (creakTimer >= creakDuration)
                {
                    isCreaking = false;
                    transform.position = shakeOriginPos;
                    isFalling = true;  // Bắt đầu gục đổ!
                    fallTimer = 0f;

                    // Tiếng gió rít và tán lá quét qua không trung
                    if (treeFallSound != null)
                    {
                        AudioSource.PlayClipAtPoint(treeFallSound, transform.position + Vector3.up * 6f, 1.2f);
                    }
                }
            }

            // Giai đoạn ngã đổ
            UpdateFalling();

            // Giảm timer thanh HP
            if (healthBarTimer > 0) healthBarTimer -= Time.deltaTime;
            else showHealthBar = false;
        }

        void UpdateShake()
        {
            if (!isShaking) return;

            shakeTimer += Time.deltaTime;
            if (shakeTimer < shakeDuration)
            {
                float intensity = shakeIntensity * (1f + chopCount * 0.2f);
                float freq = 30f + chopCount * 5f;
                float decay = 1f - (shakeTimer / shakeDuration);
                float x = Mathf.Sin(shakeTimer * freq) * intensity * decay;
                float z = Mathf.Cos(shakeTimer * freq * 1.2f) * intensity * decay;
                transform.position = shakeOriginPos + new Vector3(x, 0, z);
            }
            else
            {
                transform.position = shakeOriginPos;
                isShaking = false;
            }
        }

        void UpdateFalling()
        {
            if (!isFalling) return;

            fallTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fallTimer / fallDuration);

            // Gia tốc trọng trường chân thực: bắt đầu chậm rãi rồi tăng tốc lao xuống
            float easedT = Mathf.Pow(t, 2.2f);
            float angle = Mathf.Lerp(0f, 90f, easedT);

            // Quay cây quanh đỉnh gốc cây (pivot = originalPosition + Vector3.up * stumpHeight)
            Vector3 pivot = originalPosition + Vector3.up * stumpHeight;
            transform.rotation = Quaternion.AngleAxis(angle, fallDirection) * originalRotation;
            transform.position = pivot + Quaternion.AngleAxis(angle, fallDirection) * (originalPosition - pivot);

            // Lá rụng lả tả khi cây quét qua không khí
            if (Random.value < 0.4f)
            {
                Vector3 leafPos = transform.position + Vector3.up * Random.Range(3f, 8f) + Random.insideUnitSphere * 2.5f;
                SpawnFallingLeaf(leafPos);
            }

            // Gần chạm đất -> phát hiệu ứng va đập
            if (t > 0.88f && !groundImpactDone)
            {
                groundImpactDone = true;
                OnGroundImpact();
            }

            if (t >= 1f)
            {
                isFalling = false;

                // Sinh các khúc gỗ vật lý và lá rụng
                SpawnFallenLogsAndLeaves();

                // Ẩn dần tán cây
                StartCoroutine(DespawnFallenCanopy());
            }
        }

        // ═══════════════════════════════════════════════════════
        // NHẬN SÁT THƯƠNG
        // ═══════════════════════════════════════════════════════

        public void TakeChopDamage(float damage, Vector3 hitPoint, Vector3 attackerPos = default(Vector3))
        {
            if (isDead) return;

            currentHealth -= damage;
            chopCount++;
            showHealthBar = true;
            healthBarTimer = healthBarDisplayTime;

            // Tiếng chém gỗ
            if (chopHitSound != null)
            {
                AudioSource.PlayClipAtPoint(chopHitSound, hitPoint, 1f);
            }

            // Vụn gỗ bắn ra
            SpawnBarkChips(hitPoint, 7 + chopCount * 2);

            // Vết băm/khía trên thân cây
            UpdateCutNotch(hitPoint);

            // Lá rụng từ trên tán
            int leafCount = 2 + chopCount;
            for (int i = 0; i < leafCount; i++)
            {
                Vector3 leafPos = transform.position + Vector3.up * Random.Range(3f, 7f) + Random.insideUnitSphere * 1.8f;
                SpawnFallingLeaf(leafPos);
            }

            // Rung thân cây
            StartShake();

            Debug.Log(string.Format("[ChoppableTree] {0} bị chặt! HP: {1}/{2} (nhát {3})", gameObject.name, currentHealth, maxHealth, chopCount));

            // Kiểm tra chết
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                StartFalling(hitPoint, attackerPos);
            }
        }

        void StartShake()
        {
            if (!isShaking) shakeOriginPos = transform.position;
            isShaking = true;
            shakeTimer = 0f;
        }

        // Tạo hoặc cập nhật vết chém (khía chữ V) trên thân cây
        void UpdateCutNotch(Vector3 hitPoint)
        {
            if (cutNotchObj == null)
            {
                cutNotchObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cutNotchObj.name = "ChopNotch";
                cutNotchObj.transform.SetParent(transform, true);
                cutNotchObj.transform.position = hitPoint;
                cutNotchObj.transform.rotation = Quaternion.LookRotation(transform.position - hitPoint);
                Destroy(cutNotchObj.GetComponent<Collider>());

                Renderer r = cutNotchObj.GetComponent<Renderer>();
                Material m = new Material(Shader.Find("Standard"));
                m.color = new Color(0.85f, 0.72f, 0.50f); // Màu mặt gỗ bên trong mới chặt
                r.material = m;
            }

            // Vết chém sâu và rộng dần theo số nhát
            float s = 0.12f + chopCount * 0.05f;
            cutNotchObj.transform.localScale = new Vector3(s * 1.5f, s * 0.8f, s * 0.8f);
        }

        // ═══════════════════════════════════════════════════════
        // BẮT ĐẦU ĐỔ CÂY
        // ═══════════════════════════════════════════════════════

        void StartFalling(Vector3 hitPoint, Vector3 attackerPos = default(Vector3))
        {
            isDead = true;
            groundImpactDone = false;

            // Xóa vết chém tạm
            if (cutNotchObj != null) Destroy(cutNotchObj);

            // TÍNH TOÁN HƯỚNG ĐỔ: CÂY ĐỔ RA XA NGƯỜI CHẶT (hướng về phía trước)
            Vector3 playerPos = (attackerPos != default(Vector3)) ? attackerPos : (playerCamera != null ? playerCamera.position : hitPoint);
            Vector3 toTree = transform.position - playerPos;
            toTree.y = 0f;
            if (toTree.sqrMagnitude < 0.01f) toTree = transform.position - hitPoint;
            toTree.y = 0f;
            if (toTree.sqrMagnitude < 0.01f) toTree = Vector3.forward;

            fallAwayDir = toTree.normalized;

            // Trục quay quanh mặt phẳng ngang (vuông góc với hướng đổ)
            fallDirection = Vector3.Cross(Vector3.up, fallAwayDir).normalized;

            // Giữ vị trí gốc
            shakeOriginPos = transform.position;
            isShaking = false;

            // TẠO GỐC CÂY (STUMP) TẠI VỊ TRÍ GỐC
            CreateStump();

            // BẮT ĐẦU GIAI ĐOẠN RĂNG RẮC (CREAK)
            isCreaking = true;
            creakTimer = 0f;

            // Tiếng gỗ gãy răng rắc
            if (treeCreakSound != null)
            {
                AudioSource.PlayClipAtPoint(treeCreakSound, transform.position + Vector3.up * 1f, 1.4f);
            }

            // Tắt collider thân cây đổ để không kẹt người chơi
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            Collider[] childCols = GetComponentsInChildren<Collider>();
            foreach (Collider c in childCols) c.enabled = false;

            Debug.Log(string.Format("[ChoppableTree] 🌲 {0} BẮT ĐẦU ĐỔ RA XA! Hướng: {1}", gameObject.name, fallAwayDir));
        }

        // Tạo gốc cây (Stump) cố định trên mặt đất có thể chặt tiếp
        void CreateStump()
        {
            if (spawnedStump != null) return;

            spawnedStump = new GameObject("TreeStump_" + gameObject.name);
            spawnedStump.transform.position = originalPosition;
            spawnedStump.transform.rotation = originalRotation;

            float r = Mathf.Max(0.35f, trunkRadius * Mathf.Max(0.4f, transform.localScale.x));
            float h = stumpHeight;

            // Vỏ ngoài gốc cây (Cylinder)
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "StumpMesh";
            trunk.transform.SetParent(spawnedStump.transform, false);
            trunk.transform.localPosition = new Vector3(0, h * 0.5f, 0);
            trunk.transform.localScale = new Vector3(r * 2f, h * 0.5f, r * 2f);

            Renderer trunkRend = trunk.GetComponent<Renderer>();
            Material barkMat = new Material(Shader.Find("Standard"));
            barkMat.color = new Color(0.32f, 0.22f, 0.12f);
            barkMat.SetFloat("_Glossiness", 0.05f);
            trunkRend.material = barkMat;

            // Mặt cắt phẳng trên đầu gốc cây (vân gỗ tươi sáng màu be)
            GameObject cutFace = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cutFace.name = "CutFace";
            cutFace.transform.SetParent(spawnedStump.transform, false);
            cutFace.transform.localPosition = new Vector3(0, h + 0.005f, 0);
            cutFace.transform.localScale = new Vector3(r * 1.95f, 0.01f, r * 1.95f);

            Renderer faceRend = cutFace.GetComponent<Renderer>();
            Material faceMat = new Material(Shader.Find("Standard"));
            faceMat.color = new Color(0.82f, 0.70f, 0.48f);
            faceMat.SetFloat("_Glossiness", 0.15f);
            faceRend.material = faceMat;

            Destroy(cutFace.GetComponent<Collider>());
            Destroy(trunk.GetComponent<Collider>());

            // Capsule Collider vững chắc cho gốc cây
            CapsuleCollider cap = spawnedStump.AddComponent<CapsuleCollider>();
            cap.direction = 1;
            cap.height = h;
            cap.center = new Vector3(0, h * 0.5f, 0);
            cap.radius = r;

            // Component chặt tiếp gốc cây lấy gỗ
            ChoppableStump stumpScript = spawnedStump.AddComponent<ChoppableStump>();
            stumpScript.woodItemData = woodItemData;
            stumpScript.chopHitSound = chopHitSound;
        }

        // ═══════════════════════════════════════════════════════
        // VA ĐẤT — ÂM THANH RẦM RĨ, BỤI, MẢNH VỤN, RUNG MÀN HÌNH
        // ═══════════════════════════════════════════════════════

        void OnGroundImpact()
        {
            float approxLength = treeHeight > 4f ? treeHeight * 0.7f : 8f;
            Vector3 tipPos = originalPosition + fallAwayDir * approxLength;

            RaycastHit groundHit;
            if (Physics.Raycast(tipPos + Vector3.up * 10f, Vector3.down, out groundHit, 25f))
            {
                tipPos = groundHit.point;
            }

            // Âm thanh va đập rung chuyển
            if (groundImpactSound != null)
            {
                AudioSource.PlayClipAtPoint(groundImpactSound, tipPos, 1.8f);
            }
            else if (treeFallSound != null)
            {
                AudioSource.PlayClipAtPoint(treeFallSound, tipPos, 2f);
            }

            // Đám bụi đất bốc lên cuồn cuộn
            SpawnDustCloud(tipPos, 25);

            // Mảnh gỗ và vỏ văng tung tóe
            SpawnBarkChips(tipPos, 20);

            // Mưa lá rụng quanh ngọn cây
            for (int i = 0; i < 20; i++)
            {
                Vector3 pos = tipPos + Random.insideUnitSphere * 4.5f;
                pos.y = tipPos.y + Random.Range(0.5f, 4f);
                SpawnFallingLeaf(pos);
            }

            // Rung giật màn hình người chơi
            ShakePlayerCamera(tipPos);

            Debug.Log("[ChoppableTree] 💥 CÂY CHẠM ĐẤT RẦM RĨ! Bụi bay và mảnh gỗ văng tung tóe");
        }

        // ═══════════════════════════════════════════════════════
        // SINH CÁC KHÚC GỖ VẬT LÝ VÀ BÓ LÁ
        // ═══════════════════════════════════════════════════════

        void SpawnFallenLogsAndLeaves()
        {
            int logCount = woodDropAmount > 0 ? woodDropAmount : 3;
            float r = Mathf.Max(0.25f, trunkRadius * 0.8f);

            // Sinh các khúc gỗ nằm rải rác dọc theo thân cây đổ
            for (int i = 0; i < logCount; i++)
            {
                float distAlongTrunk = 1.8f + i * 2.2f;
                Vector3 targetPos = originalPosition + fallAwayDir * distAlongTrunk;

                RaycastHit hit;
                if (Physics.Raycast(targetPos + Vector3.up * 5f, Vector3.down, out hit, 15f))
                {
                    targetPos = hit.point + Vector3.up * 0.25f;
                }

                SpawnSingleLog(woodItemData, targetPos, r, 1.35f, fallAwayDir);
            }

            // Sinh các bó lá rơi gần tán cây
            int leafCount = leafDropAmount > 0 ? leafDropAmount : 2;
            for (int i = 0; i < leafCount; i++)
            {
                float leafDist = 5.5f + i * 2.0f;
                Vector3 leafPos = originalPosition + fallAwayDir * leafDist + Random.insideUnitSphere * 1.5f;

                RaycastHit hit;
                if (Physics.Raycast(leafPos + Vector3.up * 5f, Vector3.down, out hit, 15f))
                {
                    leafPos = hit.point + Vector3.up * 0.15f;
                }

                SpawnLeafDrop(leafItemData, leafPos);
            }

            Debug.Log(string.Format("[ChoppableTree] 🪵 Đã tạo {0} khúc gỗ vật lý + {1} bó lá!", logCount, leafCount));
        }

        /// <summary>
        /// Tạo 1 khúc gỗ 3D vật lý thật (nằm lăn trên đất, có PickupItem [E] Nhặt Gỗ)
        /// </summary>
        public static GameObject SpawnSingleLog(HorrorGame.Inventory.ItemData woodItem, Vector3 pos, float radius = 0.25f, float length = 1.35f, Vector3 orientDir = default(Vector3))
        {
            GameObject logObj = new GameObject("WoodLog");
            logObj.transform.position = pos;

            if (orientDir == default(Vector3)) orientDir = Vector3.forward;
            Quaternion rot = Quaternion.LookRotation(orientDir) * Quaternion.Euler(0, Random.Range(-25f, 25f), 90f);
            logObj.transform.rotation = rot;

            // Thân khúc gỗ (Cylinder)
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "LogMesh";
            cylinder.transform.SetParent(logObj.transform, false);
            cylinder.transform.localPosition = Vector3.zero;
            cylinder.transform.localRotation = Quaternion.identity;
            cylinder.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);

            Renderer cr = cylinder.GetComponent<Renderer>();
            Material barkMat = new Material(Shader.Find("Standard"));
            barkMat.color = new Color(0.38f, 0.26f, 0.14f); // Vỏ cây nâu sần
            barkMat.SetFloat("_Glossiness", 0.08f);
            cr.material = barkMat;

            // 2 nắp mặt cắt gỗ sáng màu ở 2 đầu
            for (int end = -1; end <= 1; end += 2)
            {
                GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cap.name = "EndCap";
                cap.transform.SetParent(logObj.transform, false);
                cap.transform.localPosition = new Vector3(0, end * (length * 0.5f + 0.002f), 0);
                cap.transform.localScale = new Vector3(radius * 1.95f, 0.008f, radius * 1.95f);

                Renderer capRend = cap.GetComponent<Renderer>();
                Material capMat = new Material(Shader.Find("Standard"));
                capMat.color = new Color(0.82f, 0.70f, 0.48f); // Mặt cắt sáng
                capMat.SetFloat("_Glossiness", 0.15f);
                capRend.material = capMat;

                Destroy(cap.GetComponent<Collider>());
            }

            Destroy(cylinder.GetComponent<Collider>());

            // Capsule Collider ôm sát khúc gỗ
            CapsuleCollider col = logObj.AddComponent<CapsuleCollider>();
            col.direction = 1;
            col.height = length;
            col.radius = radius;

            // Rigidbody vật lý giúp khúc gỗ nảy nhẹ và lăn tự nhiên
            Rigidbody rb = logObj.AddComponent<Rigidbody>();
            rb.mass = 18f;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.velocity = (Random.insideUnitSphere + Vector3.up * 0.4f) * 1.2f;
            rb.angularVelocity = Random.insideUnitSphere * 2.5f;

            // Script nhặt đồ phím E
            var pickup = logObj.AddComponent<HorrorGame.Inventory.PickupItem>();
            pickup.itemData = woodItem;
            pickup.amount = 1;
            pickup.interactDistance = 2.8f;
            pickup.autoRotate = false;
            pickup.hoverEffect = false;

            return logObj;
        }

        /// <summary>
        /// Tạo bó lá cây 3D có thể nhặt bằng phím E
        /// </summary>
        public static GameObject SpawnLeafDrop(HorrorGame.Inventory.ItemData leafItem, Vector3 pos)
        {
            GameObject leafObj = new GameObject("LeafBundle");
            leafObj.transform.position = pos;

            for (int i = 0; i < 3; i++)
            {
                GameObject leafQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                leafQuad.name = "LeafQuad";
                leafQuad.transform.SetParent(leafObj.transform, false);
                leafQuad.transform.localPosition = Random.insideUnitSphere * 0.1f;
                leafQuad.transform.localRotation = Random.rotation;
                leafQuad.transform.localScale = Vector3.one * Random.Range(0.25f, 0.38f);

                Renderer r = leafQuad.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.22f, 0.62f, 0.16f);
                mat.SetColor("_EmissionColor", new Color(0.04f, 0.12f, 0.03f));
                mat.EnableKeyword("_EMISSION");
                r.material = mat;

                Destroy(leafQuad.GetComponent<Collider>());
            }

            SphereCollider col = leafObj.AddComponent<SphereCollider>();
            col.radius = 0.35f;
            col.isTrigger = true;

            var pickup = leafObj.AddComponent<HorrorGame.Inventory.PickupItem>();
            pickup.itemData = leafItem;
            pickup.amount = 1;
            pickup.interactDistance = 2.5f;
            pickup.autoRotate = true;
            pickup.hoverEffect = true;

            return leafObj;
        }

        // Thu nhỏ dần và biến mất tán cây đã ngã
        System.Collections.IEnumerator DespawnFallenCanopy()
        {
            yield return new WaitForSeconds(despawnDelay);

            float fadeDuration = 1.8f;
            float elapsed = 0f;
            Vector3 initScale = transform.localScale;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float f = 1f - (elapsed / fadeDuration);
                transform.localScale = initScale * f;
                yield return null;
            }

            if (canRespawn)
            {
                gameObject.SetActive(false);
                transform.localScale = originalScale;
                Invoke("RespawnTree", respawnTime);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void RespawnTree()
        {
            if (spawnedStump != null) Destroy(spawnedStump);

            currentHealth = maxHealth;
            isDead = false;
            isFalling = false;
            isCreaking = false;
            fallTimer = 0f;
            isShaking = false;
            chopCount = 0;
            groundImpactDone = false;

            transform.position = originalPosition;
            transform.rotation = originalRotation;
            transform.localScale = originalScale;

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
            Collider[] childCols = GetComponentsInChildren<Collider>();
            foreach (Collider c in childCols) c.enabled = true;

            gameObject.SetActive(true);
            Debug.Log(string.Format("[ChoppableTree] 🌱 {0} đã mọc lại!", gameObject.name));
        }

        // ═══════════════════════════════════════════════════════
        // VỤN GỖ / VỎ CÂY BẮN RA
        // ═══════════════════════════════════════════════════════

        void SpawnBarkChips(Vector3 point, int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(chip.GetComponent<Collider>());
                chip.name = "BarkChip";
                chip.transform.position = point + Random.insideUnitSphere * 0.15f;

                float s = Random.Range(0.04f, 0.11f);
                chip.transform.localScale = new Vector3(s, s * 0.35f, s * Random.Range(1f, 2.5f));
                chip.transform.rotation = Random.rotation;

                Renderer r = chip.GetComponent<Renderer>();
                Material m = new Material(Shader.Find("Standard"));
                float shade = Random.Range(0.35f, 0.7f);
                m.color = new Color(shade, shade * 0.65f, shade * 0.32f);
                r.material = m;

                Rigidbody rb = chip.AddComponent<Rigidbody>();
                rb.mass = 0.015f;
                Vector3 dir = (Random.insideUnitSphere + Vector3.up * 0.9f).normalized;
                rb.velocity = dir * Random.Range(2.5f, 5.5f);
                rb.angularVelocity = Random.insideUnitSphere * 12f;

                Destroy(chip, 2.5f);
            }
        }

        // ═══════════════════════════════════════════════════════
        // LÁ CÂY RỤNG
        // ═══════════════════════════════════════════════════════

        void SpawnFallingLeaf(Vector3 pos)
        {
            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(leaf.GetComponent<Collider>());
            leaf.name = "FallingLeaf";
            leaf.transform.position = pos;

            float ls = Random.Range(0.09f, 0.22f);
            leaf.transform.localScale = new Vector3(ls, ls, ls);
            leaf.transform.rotation = Random.rotation;

            Renderer r = leaf.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
            float g = Random.Range(0.35f, 0.75f);
            mat.color = new Color(Random.Range(0.12f, 0.35f), g, Random.Range(0.06f, 0.2f), 0.85f);
            r.material = mat;

            FallingLeafBehavior flb = leaf.AddComponent<FallingLeafBehavior>();
            flb.lifetime = Random.Range(2.5f, 4.5f);
        }

        // ═══════════════════════════════════════════════════════
        // ĐÁM BỤI ĐẤT khi cây chạm đất
        // ═══════════════════════════════════════════════════════

        void SpawnDustCloud(Vector3 pos, int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject dust = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(dust.GetComponent<Collider>());
                dust.name = "Dust";

                Vector3 offset = Random.insideUnitSphere * 3f;
                offset.y = Mathf.Abs(offset.y) * 0.6f;
                dust.transform.position = pos + offset;

                float ds = Random.Range(0.2f, 0.6f);
                dust.transform.localScale = new Vector3(ds, ds, ds);

                Renderer r = dust.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Standard"));
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = 3000;
                mat.color = new Color(0.6f, 0.5f, 0.35f, 0.5f);
                r.material = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                DustBehavior db = dust.AddComponent<DustBehavior>();
                db.riseSpeed = Random.Range(0.6f, 2.2f);
                db.expandRate = Random.Range(1.2f, 3.2f);
                db.lifetime = Random.Range(1.8f, 3.2f);
            }
        }

        void ShakePlayerCamera(Vector3 impactPos)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            float dist = Vector3.Distance(cam.transform.position, impactPos);
            if (dist > 25f) return;

            float intensity = Mathf.Lerp(0.45f, 0.05f, dist / 25f);
            CameraShakeEffect shake = cam.GetComponent<CameraShakeEffect>();
            if (shake == null) shake = cam.gameObject.AddComponent<CameraShakeEffect>();
            shake.StartShake(0.7f, intensity);
        }

        // ═══════════════════════════════════════════════════════
        // UI: THANH HP CÂY (hiển thị trực quan ở tầm mắt)
        // ═══════════════════════════════════════════════════════

        void OnGUI()
        {
            if (!showHealthBar || (isDead && !isFalling && !isCreaking)) return;

            if (playerCamera == null)
            {
                Camera cam = Camera.main;
                if (cam != null) playerCamera = cam.transform;
            }
            if (playerCamera == null) return;

            float dist = Vector3.Distance(transform.position, playerCamera.position);
            if (dist > 12f) return;

            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            // Đặt vị trí tầm mắt (~1.5m trên gốc) để luôn thấy rõ khi đứng chặt
            Vector3 worldPos = transform.position + Vector3.up * 1.5f;
            Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0) return;

            float barWidth = 140f;
            float barHeight = 14f;
            float x = screenPos.x - barWidth / 2f;
            float y = Screen.height - screenPos.y - barHeight / 2f;
            y = Mathf.Clamp(y, 40f, Screen.height - 80f);

            // Nền đen bóng viền
            GUI.color = new Color(0, 0, 0, 0.85f);
            GUI.DrawTexture(new Rect(x - 3, y - 3, barWidth + 6, barHeight + 6), Texture2D.whiteTexture);

            // Nền thanh đã mất
            GUI.color = new Color(0.25f, 0.18f, 0.12f, 0.9f);
            GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture);

            // Thanh HP chuyển màu theo máu (Xanh lá -> Vàng -> Đỏ)
            float hpPercent = Mathf.Clamp01(currentHealth / maxHealth);
            Color hpColor = (hpPercent > 0.5f)
                ? Color.Lerp(Color.yellow, new Color(0.2f, 0.88f, 0.2f), (hpPercent - 0.5f) * 2f)
                : Color.Lerp(Color.red, Color.yellow, hpPercent * 2f);

            GUI.color = hpColor;
            GUI.DrawTexture(new Rect(x, y, barWidth * hpPercent, barHeight), Texture2D.whiteTexture);

            // Tiêu đề & số HP
            GUI.color = Color.white;
            GUIStyle textStyle = new GUIStyle();
            textStyle.fontSize = 13;
            textStyle.fontStyle = FontStyle.Bold;
            textStyle.normal.textColor = Color.white;
            textStyle.alignment = TextAnchor.MiddleCenter;

            GUIStyle shadowStyle = new GUIStyle(textStyle);
            shadowStyle.normal.textColor = Color.black;

            string label = (currentHealth > 0)
                ? string.Format("🌲 Cây Rừng: {0}/{1}", Mathf.CeilToInt(currentHealth), Mathf.CeilToInt(maxHealth))
                : "🌲 CÂY ĐANG ĐỔ!";

            GUI.Label(new Rect(x - 29, y - 22, barWidth + 60, 20), label, shadowStyle);
            GUI.Label(new Rect(x - 30, y - 23, barWidth + 60, 20), label, textStyle);
        }
    }

    // ═════════════════════════════════════════════════════════════
    // GỐC CÂY CÓ THỂ CHẶT TIẾP (ChoppableStump)
    // ═════════════════════════════════════════════════════════════

    public class ChoppableStump : MonoBehaviour
    {
        public float health = 50f;
        public HorrorGame.Inventory.ItemData woodItemData;
        public AudioClip chopHitSound;

        private Vector3 originPos;
        private bool isShaking = false;
        private float shakeTimer = 0f;

        void Awake()
        {
            originPos = transform.position;
        }

        public void TakeChopDamage(float damage, Vector3 hitPoint)
        {
            health -= damage;

            if (chopHitSound != null)
                AudioSource.PlayClipAtPoint(chopHitSound, hitPoint, 1f);

            isShaking = true;
            shakeTimer = 0f;

            if (health <= 0)
            {
                // Rơi thêm 1 khúc gỗ thưởng và xóa gốc cây
                if (woodItemData != null)
                {
                    ChoppableTree.SpawnSingleLog(woodItemData, transform.position + Vector3.up * 0.4f, 0.25f, 1.2f);
                }

                Destroy(gameObject);
            }
        }

        void Update()
        {
            if (isShaking)
            {
                shakeTimer += Time.deltaTime;
                if (shakeTimer < 0.25f)
                {
                    float offset = Mathf.Sin(shakeTimer * 50f) * 0.03f;
                    transform.position = originPos + new Vector3(offset, 0, offset);
                }
                else
                {
                    transform.position = originPos;
                    isShaking = false;
                }
            }
        }
    }

    // ═════════════════════════════════════════════════════════════
    // HELPER: Lá rơi xoay tròn chậm
    // ═════════════════════════════════════════════════════════════

    public class FallingLeafBehavior : MonoBehaviour
    {
        public float lifetime = 3f;
        private float timer = 0f;
        private Vector3 swayDir;
        private float swaySpeed;
        private float fallSpeed;
        private float rotSpeed;

        void Start()
        {
            swayDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            swaySpeed = Random.Range(1f, 3f);
            fallSpeed = Random.Range(0.3f, 0.8f);
            rotSpeed = Random.Range(50f, 200f);
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            // Rơi chậm + lắc lư ngang
            float sway = Mathf.Sin(timer * swaySpeed) * 0.5f;
            Vector3 move = Vector3.down * fallSpeed + swayDir * sway * Time.deltaTime;
            transform.position += move * Time.deltaTime;

            // Xoay tròn
            transform.Rotate(Vector3.up, rotSpeed * Time.deltaTime, Space.World);
            transform.Rotate(Vector3.right, rotSpeed * 0.5f * Time.deltaTime, Space.Self);

            // Fade out gần cuối
            float fade = 1f - Mathf.Clamp01((timer - lifetime * 0.7f) / (lifetime * 0.3f));
            Renderer r = GetComponent<Renderer>();
            if (r != null && r.material.HasProperty("_Color"))
            {
                Color c = r.material.color;
                c.a = fade * 0.85f;
                r.material.color = c;
            }
        }
    }

    // ═════════════════════════════════════════════════════════════
    // HELPER: Bụi đất bay lên rồi tan
    // ═════════════════════════════════════════════════════════════

    public class DustBehavior : MonoBehaviour
    {
        public float riseSpeed = 1f;
        public float expandRate = 2f;
        public float lifetime = 2f;
        private float timer = 0f;
        private Vector3 driftDir;

        void Start()
        {
            driftDir = new Vector3(Random.Range(-1f, 1f), 0.3f, Random.Range(-1f, 1f)).normalized;
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float t = timer / lifetime;

            // Bay lên + trôi ngang
            transform.position += (Vector3.up * riseSpeed + driftDir * 0.5f) * Time.deltaTime;

            // Phình to
            float scale = 1f + t * expandRate;
            transform.localScale = Vector3.one * scale * 0.3f;

            // Mờ dần
            Renderer r = GetComponent<Renderer>();
            if (r != null && r.material.HasProperty("_Color"))
            {
                Color c = r.material.color;
                c.a = Mathf.Lerp(0.5f, 0f, t);
                r.material.color = c;
            }
        }
    }

    // ═════════════════════════════════════════════════════════════
    // HELPER: Camera rung khi cây đổ gần
    // ═════════════════════════════════════════════════════════════

    public class CameraShakeEffect : MonoBehaviour
    {
        private float shakeDuration = 0f;
        private float shakeIntensity = 0f;
        private float shakeTimer = 0f;
        private Vector3 originalLocalPos;
        private bool isShaking = false;

        public void StartShake(float duration, float intensity)
        {
            shakeDuration = duration;
            shakeIntensity = intensity;
            shakeTimer = 0f;

            if (!isShaking)
            {
                originalLocalPos = transform.localPosition;
                isShaking = true;
            }
        }

        void LateUpdate()
        {
            if (!isShaking) return;

            shakeTimer += Time.deltaTime;
            if (shakeTimer >= shakeDuration)
            {
                transform.localPosition = originalLocalPos;
                isShaking = false;
                return;
            }

            float decay = 1f - (shakeTimer / shakeDuration);
            float x = Random.Range(-1f, 1f) * shakeIntensity * decay;
            float y = Random.Range(-1f, 1f) * shakeIntensity * decay;
            transform.localPosition = originalLocalPos + new Vector3(x, y, 0f);
        }
    }
}

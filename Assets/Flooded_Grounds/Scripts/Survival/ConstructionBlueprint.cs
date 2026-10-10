using UnityEngine;
using System.Collections.Generic;
using HorrorGame.Inventory;

namespace HorrorGame.Survival
{
    /// <summary>
    /// Đại diện cho Khung Mờ Công Trình (Construction Blueprint Ghost) phong cách The Forest.
    /// Sau khi đặt khung mờ bằng Build Menu [B], khung mờ sẽ hiện hữu trong thế giới.
    /// Người chơi chặt cây nhặt từng khúc gỗ vào túi hoặc lại gần khung mờ:
    /// - Nhìn hoặc đứng gần khung mờ (< 5m quanh công trình hoặc bên trong)
    /// - Bấm [E] (hoặc Giữ [E]): Đóng từng thanh gỗ / nguyên liệu vào khung.
    /// - Nhận diện linh hoạt nguyên liệu (Wood, wood, Gỗ, Khúc gỗ, v.v.).
    /// - Khi đóng gỗ, công trình sẽ dần biến đổi từ khung mờ sang các mảng gỗ/vật liệu thật.
    /// - Khi đóng đủ số lượng nguyên liệu yêu cầu, công trình hoàn thành và biến thành nhà thật!
    /// - Bấm giữ [C]: Hủy khung mờ và hoàn trả lại nguyên liệu đã đóng.
    /// </summary>
    public class ConstructionBlueprint : MonoBehaviour
    {
        [Header("Thông tin công trình")]
        public string structureName = "Công trình";
        public string[] ingredientIDs = new string[] { "wood" };
        public string[] ingredientDisplayNames = new string[] { "Gỗ" };
        public int[] ingredientRequired = new int[] { 20 };
        public int[] ingredientCurrent = new int[] { 0 };

        [Header("Hàm tạo công trình hoàn thiện")]
        public System.Func<GameObject> createBuildingFunc;
        public GameObject resultPrefab;

        [Header("Âm thanh")]
        public AudioClip placeMaterialSound;
        public AudioClip completeSound;
        public AudioClip cancelSound;

        [Header("Cài đặt tầm tương tác")]
        public float interactionRayDistance = 8.0f; // Tầm ngắm tia raycast vào các vách tường/mái
        public float proximityDistance = 5.0f;      // Tầm đứng gần quanh công trình

        // Visual tracking for progressive build
        private List<Renderer> childRenderers = new List<Renderer>();
        private List<Material[]> originalMaterials = new List<Material[]>();
        private Material ghostBlueprintMat;

        // Bounding box của toàn bộ công trình
        private Bounds buildingBounds;
        private bool boundsCalculated = false;

        // Player detection
        private Transform playerCamera;
        private Transform playerTransform;
        private bool isPlayerLooking = false;

        // Input timers
        private float holdCancelTimer = 0f;
        private const float CANCEL_HOLD_DURATION = 1.0f;
        private float lastDepositTime = -999f;
        private float holdDepositTimer = 0f;
        private const float RAPID_DEPOSIT_INTERVAL = 0.18f; // Tốc độ đóng gỗ khi giữ E

        // Feedback notice
        private string feedbackMessage = "";
        private float feedbackTimer = 0f;

        // Audio source
        private AudioSource audioSource;

        // GUI Styles
        private GUIStyle hudBoxStyle;
        private GUIStyle titleStyle;
        private GUIStyle ingRowStyle;
        private GUIStyle ingDetailStyle;
        private GUIStyle promptStyle;
        private GUIStyle noticeStyle;
        private bool stylesReady = false;

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.85f;
            audioSource.minDistance = 2f;
            audioSource.maxDistance = 30f;
        }

        void Start()
        {
            FindPlayer();

            // Nếu chưa có âm thanh đặt gỗ, thử load âm thanh chặt gỗ
            if (placeMaterialSound == null)
            {
#if UNITY_EDITOR
                placeMaterialSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/sound axe chop tree/wings_of_freedom-chopping-wood-435769.mp3");
                if (placeMaterialSound == null)
                    placeMaterialSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/sound axe chop tree/litupsubway-ui-equip-sfx-513361.mp3");
#endif
            }

            // Tính toán Bounds ban đầu nếu chưa có
            CalculateBounds();
        }

        /// <summary>
        /// Khởi tạo khung mờ với đầy đủ công thức và material
        /// </summary>
        public void Initialize(string name, string[] ids, string[] displayNames, int[] required,
            System.Func<GameObject> createFunc, GameObject prefab, Material ghostMat,
            AudioClip placeSnd, AudioClip compSnd, AudioClip cancSnd)
        {
            structureName = name;
            ingredientIDs = ids;
            ingredientDisplayNames = displayNames;
            ingredientRequired = required;
            ingredientCurrent = new int[required.Length];

            createBuildingFunc = createFunc;
            resultPrefab = prefab;
            ghostBlueprintMat = ghostMat;

            if (placeSnd != null) placeMaterialSound = placeSnd;
            if (compSnd != null) completeSound = compSnd;
            if (cancSnd != null) cancelSound = cancSnd;

            // Thu thập toàn bộ Renderers và lưu lại material gốc
            childRenderers.Clear();
            originalMaterials.Clear();

            Renderer[] rends = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < rends.Length; i++)
            {
                Renderer r = rends[i];
                childRenderers.Add(r);

                // Lưu lại mảng material gốc
                Material[] origMats = new Material[r.sharedMaterials.Length];
                for (int m = 0; m < r.sharedMaterials.Length; m++)
                {
                    origMats[m] = r.sharedMaterials[m];
                }
                originalMaterials.Add(origMats);

                // Áp dụng material khung mờ blueprint
                if (ghostBlueprintMat != null)
                {
                    Material[] ghostMats = new Material[r.sharedMaterials.Length];
                    for (int m = 0; m < ghostMats.Length; m++)
                    {
                        ghostMats[m] = ghostBlueprintMat;
                    }
                    r.materials = ghostMats;
                }
            }

            // Đặt tất cả Collider con thành isTrigger = true:
            // - Người chơi đi lại xuyên qua không bị cản trở/mắc kẹt vật lý
            // - Nhưng tia raycast từ camera vẫn va chạm chuẩn xác vào từng thanh gỗ/vách tường!
            Collider[] cols = GetComponentsInChildren<Collider>();
            for (int i = 0; i < cols.Length; i++)
            {
                cols[i].enabled = true;
                cols[i].isTrigger = true;
            }

            CalculateBounds();

            // Cập nhật hiển thị tiến độ ban đầu (0%)
            UpdateProgressVisuals();

            Debug.Log(string.Format("[ConstructionBlueprint] Đã khởi tạo khung mờ: {0}, cần {1} nguyên liệu.", structureName, ingredientRequired.Length > 0 ? ingredientRequired[0] : 0));
        }

        private void CalculateBounds()
        {
            Renderer[] rends = GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                buildingBounds = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++)
                {
                    buildingBounds.Encapsulate(rends[i].bounds);
                }
                boundsCalculated = true;
            }
            else
            {
                buildingBounds = new Bounds(transform.position + Vector3.up * 1.6f, new Vector3(5.5f, 4.0f, 5.5f));
                boundsCalculated = true;
            }
        }

        private void FindPlayer()
        {
            if (playerCamera == null)
            {
                Camera cam = Camera.main;
                if (cam != null) playerCamera = cam.transform;
            }

            if (playerTransform == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
                else if (playerCamera != null) playerTransform = playerCamera.root;
            }
        }

        void Update()
        {
            FindPlayer();
            if (playerCamera == null) return;

            if (feedbackTimer > 0f) feedbackTimer -= Time.deltaTime;

            CheckPlayerLooking();

            if (isPlayerLooking)
            {
                // 1. Bấm nhấp [E] (nhát đầu tiên đóng ngay lập tức)
                if (Input.GetKeyDown(KeyCode.E))
                {
                    TryDepositMaterial();
                    holdDepositTimer = 0f;
                }
                // 2. Hoặc Giữ [E] (tự động đóng liên tục sau 0.25s giữ phím)
                else if (Input.GetKey(KeyCode.E))
                {
                    holdDepositTimer += Time.deltaTime;
                    if (holdDepositTimer >= 0.25f && Time.time - lastDepositTime >= RAPID_DEPOSIT_INTERVAL)
                    {
                        TryDepositMaterial();
                    }
                }
                else
                {
                    holdDepositTimer = 0f;
                }

                // 3. Giữ C để hủy khung mờ
                if (Input.GetKey(KeyCode.C))
                {
                    holdCancelTimer += Time.deltaTime;
                    if (holdCancelTimer >= CANCEL_HOLD_DURATION)
                    {
                        CancelBlueprint();
                    }
                }
                else
                {
                    holdCancelTimer = 0f;
                }
            }
            else
            {
                holdCancelTimer = 0f;
                holdDepositTimer = 0f;
            }
        }

        /// <summary>
        /// Kiểm tra người chơi có đang nhìn hoặc đứng gần khung mờ công trình không
        /// </summary>
        private void CheckPlayerLooking()
        {
            isPlayerLooking = false;
            if (playerCamera == null) return;

            if (!boundsCalculated) CalculateBounds();

            // Khoảng cách từ camera người chơi đến mép gần nhất của công trình (Bounds)
            float distToBounds = Mathf.Sqrt(buildingBounds.SqrDistance(playerCamera.position));

            // Quá xa so với công trình (> 10m) thì bỏ qua
            if (distToBounds > 10.0f) return;

            // 1. Raycast từ camera hướng về phía trước (QueryTriggerInteraction.Collide để bắt trúng collider trigger của khung)
            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, interactionRayDistance, ~0, QueryTriggerInteraction.Collide))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform) || transform.IsChildOf(hit.transform))
                {
                    isPlayerLooking = true;
                    return;
                }
            }

            // 2. Nếu người chơi đang đứng bên trong hoặc ngay mép công trình (< 4.5m)
            if (distToBounds <= proximityDistance)
            {
                // Nếu người chơi đứng bên trong công trình: luôn luôn nhận diện!
                if (buildingBounds.Contains(playerCamera.position))
                {
                    isPlayerLooking = true;
                    return;
                }

                // Nếu đứng bên ngoài mép công trình: nhìn tương đối về phía công trình (góc > 0.25 ~ 75 độ)
                Vector3 dirToCenter = (buildingBounds.center - playerCamera.position).normalized;
                float dot = Vector3.Dot(playerCamera.forward, dirToCenter);
                if (dot > 0.25f)
                {
                    isPlayerLooking = true;
                    return;
                }
            }
        }

        // =============================================
        // HỆ THỐNG NHẬN DIỆN VẬT PHẨM LINH HOẠT
        // =============================================
        /// <summary>
        /// Kiểm tra xem 1 ItemData có khớp với loại nguyên liệu yêu cầu không (không phân biệt hoa thường)
        /// </summary>
        public static bool IsItemMatch(ItemData item, string ingredientID)
        {
            if (item == null) return false;

            string id = !string.IsNullOrEmpty(item.itemID) ? item.itemID.Trim().ToLower() : "";
            string name = !string.IsNullOrEmpty(item.itemName) ? item.itemName.Trim().ToLower() : "";
            string assetName = !string.IsNullOrEmpty(item.name) ? item.name.Trim().ToLower() : "";
            string target = !string.IsNullOrEmpty(ingredientID) ? ingredientID.Trim().ToLower() : "";

            // 1. So khớp ID chính xác
            if (id == target || name == target || assetName == target) return true;

            // 2. So khớp linh hoạt cho GỖ (wood / log / củi)
            if (target == "wood")
            {
                if (id.Contains("wood") || id.Contains("log") || id.Contains("cui") ||
                    name.Contains("gỗ") || name.Contains("go") || name.Contains("wood") ||
                    assetName.Contains("wood") || assetName.Contains("log"))
                {
                    return true;
                }
            }
            // 3. So khớp linh hoạt cho ĐÁ (stone / rock)
            else if (target == "stone")
            {
                if (id.Contains("stone") || id.Contains("rock") || id.Contains("da") ||
                    name.Contains("đá") || name.Contains("da") || name.Contains("stone") ||
                    assetName.Contains("stone") || assetName.Contains("rock"))
                {
                    return true;
                }
            }
            // 4. So khớp linh hoạt cho LÁ CÂY (leaf / lá)
            else if (target == "leaf")
            {
                if (id.Contains("leaf") || id.Contains("la") ||
                    name.Contains("lá") || name.Contains("la") || name.Contains("leaf") ||
                    assetName.Contains("leaf"))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Đếm tổng số lượng nguyên liệu phù hợp trong túi đồ
        /// </summary>
        public static int GetMatchingItemCount(string ingredientID)
        {
            var inv = InventoryManager.Instance;
            if (inv == null || inv.slots == null) return 0;

            int total = 0;
            string target = !string.IsNullOrEmpty(ingredientID) ? ingredientID.Trim().ToLower() : "";

            for (int i = 0; i < inv.slots.Count; i++)
            {
                InventorySlot slot = inv.slots[i];
                if (slot != null && slot.item != null && slot.amount > 0)
                {
                    if (IsItemMatch(slot.item, target))
                    {
                        total += slot.amount;
                    }
                }
            }
            return total;
        }

        /// <summary>
        /// Trừ 1 (hoặc số lượng) nguyên liệu phù hợp khỏi túi đồ
        /// </summary>
        public static bool RemoveMatchingItem(string ingredientID, int amountToRemove)
        {
            var inv = InventoryManager.Instance;
            if (inv == null || inv.slots == null) return false;

            string target = !string.IsNullOrEmpty(ingredientID) ? ingredientID.Trim().ToLower() : "";
            int remaining = amountToRemove;

            for (int i = 0; i < inv.slots.Count; i++)
            {
                if (remaining <= 0) break;

                InventorySlot slot = inv.slots[i];
                if (slot != null && slot.item != null && slot.amount > 0)
                {
                    if (IsItemMatch(slot.item, target))
                    {
                        if (slot.amount >= remaining)
                        {
                            slot.RemoveAmount(remaining);
                            remaining = 0;
                        }
                        else
                        {
                            remaining -= slot.amount;
                            slot.ClearSlot();
                        }
                    }
                }
            }

            if (inv.onInventoryChangedEvent != null) inv.onInventoryChangedEvent();
            return remaining <= 0;
        }

        /// <summary>
        /// Thử đóng 1 thanh gỗ / nguyên liệu vào khung
        /// </summary>
        private void TryDepositMaterial()
        {
            // Tìm nguyên liệu còn thiếu mà người chơi đang có trong túi
            int foundIndex = -1;
            for (int i = 0; i < ingredientIDs.Length; i++)
            {
                if (ingredientCurrent[i] < ingredientRequired[i])
                {
                    if (GetMatchingItemCount(ingredientIDs[i]) > 0)
                    {
                        foundIndex = i;
                        break;
                    }
                }
            }

            // 1. Nếu có trong túi đồ: Trừ 1 và tăng tiến độ
            if (foundIndex >= 0)
            {
                string id = ingredientIDs[foundIndex];
                bool success = RemoveMatchingItem(id, 1);
                if (success)
                {
                    ingredientCurrent[foundIndex]++;

                    PlaySound(placeMaterialSound);
                    ShowFeedback(string.Format("+1 {0} vào khung! ({1}/{2})", ingredientDisplayNames[foundIndex], ingredientCurrent[foundIndex], ingredientRequired[foundIndex]));

                    lastDepositTime = Time.time;
                    UpdateProgressVisuals();
                    CheckIfCompleted();
                    return;
                }
            }

            // 2. Nếu trong túi không có gỗ: Tìm khúc gỗ WoodLog trên mặt đất gần đó (< 6.0m)
            for (int i = 0; i < ingredientIDs.Length; i++)
            {
                string target = ingredientIDs[i].ToLower();
                if (target == "wood" && ingredientCurrent[i] < ingredientRequired[i])
                {
                    Collider[] groundObjects = Physics.OverlapSphere(transform.position + Vector3.up * 1f, 6.0f);
                    for (int k = 0; k < groundObjects.Length; k++)
                    {
                        Collider gCol = groundObjects[k];
                        if (gCol == null) continue;

                        PickupItem pickup = gCol.GetComponentInParent<PickupItem>();
                        if (pickup != null && pickup.itemData != null && IsItemMatch(pickup.itemData, "wood"))
                        {
                            // Hút khúc gỗ từ mặt đất vào công trình!
                            GameObject logObj = pickup.gameObject;
                            Destroy(logObj);

                            ingredientCurrent[i]++;
                            PlaySound(placeMaterialSound);
                            ShowFeedback(string.Format("+1 Gỗ từ mặt đất! ({0}/{1})", ingredientCurrent[i], ingredientRequired[i]));

                            lastDepositTime = Time.time;
                            UpdateProgressVisuals();
                            CheckIfCompleted();
                            return;
                        }
                    }
                }
            }

            // 3. Nếu hoàn toàn không có nguyên liệu
            if (Time.time - lastDepositTime >= 1.0f)
            {
                ShowFeedback("❌ Bạn không có khúc gỗ nào! Hãy dùng rìu chặt cây để lấy gỗ.");
                lastDepositTime = Time.time;
            }
        }

        /// <summary>
        /// Cập nhật hiển thị tiến độ:
        /// Công trình dần chuyển từ khung mờ sang các mảng vật liệu thật khi đóng gỗ (giống The Forest)!
        /// </summary>
        private void UpdateProgressVisuals()
        {
            if (childRenderers.Count == 0 || originalMaterials.Count == 0) return;

            // Tính tỉ lệ phần trăm hoàn thành tổng thể
            int totalCurrent = 0;
            int totalReq = 0;
            for (int i = 0; i < ingredientRequired.Length; i++)
            {
                totalCurrent += ingredientCurrent[i];
                totalReq += ingredientRequired[i];
            }

            float progress = totalReq > 0 ? (float)totalCurrent / totalReq : 1f;

            // Số mảng vật thể đã thành vật liệu thật
            int solidPartsCount = Mathf.FloorToInt(progress * childRenderers.Count);

            for (int i = 0; i < childRenderers.Count; i++)
            {
                Renderer rend = childRenderers[i];
                if (rend == null) continue;

                if (i < solidPartsCount)
                {
                    // Chuyển sang vật liệu thật hoàn chỉnh
                    rend.materials = originalMaterials[i];
                }
                else
                {
                    // Vẫn là khung mờ blueprint
                    if (ghostBlueprintMat != null)
                    {
                        Material[] ghostMats = new Material[rend.materials.Length];
                        for (int m = 0; m < ghostMats.Length; m++) ghostMats[m] = ghostBlueprintMat;
                        rend.materials = ghostMats;
                    }
                }
            }
        }

        /// <summary>
        /// Kiểm tra công trình đã đóng đủ tất cả nguyên liệu chưa
        /// </summary>
        private void CheckIfCompleted()
        {
            bool allDone = true;
            for (int i = 0; i < ingredientRequired.Length; i++)
            {
                if (ingredientCurrent[i] < ingredientRequired[i])
                {
                    allDone = false;
                    break;
                }
            }

            if (allDone)
            {
                CompleteBuilding();
            }
        }

        /// <summary>
        /// Hoàn thành công trình: Spawn nhà thật với đầy đủ collider, giường ngủ, rương...
        /// </summary>
        private void CompleteBuilding()
        {
            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;

            // Phát âm thanh hoàn thành
            if (completeSound != null)
            {
                AudioSource.PlayClipAtPoint(completeSound, pos, 1f);
            }

            // Spawn công trình thật
            GameObject building = null;
            if (createBuildingFunc != null)
            {
                building = createBuildingFunc();
            }
            else if (resultPrefab != null)
            {
                building = Instantiate(resultPrefab);
            }

            if (building != null)
            {
                building.transform.position = pos;
                building.transform.rotation = rot;

                // Kích hoạt tất cả collider thực sự
                Collider[] allCols = building.GetComponentsInChildren<Collider>();
                for (int i = 0; i < allCols.Length; i++)
                {
                    allCols[i].enabled = true;
                    allCols[i].isTrigger = false;
                }

                Debug.Log(string.Format("[ConstructionBlueprint] 🎉 Đã xây xong {0} thành công tại {1}!", structureName, pos));
            }

            // Hiệu ứng hoàn thành
            SpawnCompletionBurst(pos + Vector3.up * 1.2f);

            // Thông báo trên màn hình
            if (BuildingSystem.Instance != null)
            {
                BuildingSystem.Instance.ShowExternalNotice(string.Format("🎉 ĐÃ HOÀN THÀNH XÂY DỰNG: {0}!", structureName));
            }

            // Xóa khung mờ
            Destroy(gameObject);
        }

        /// <summary>
        /// Hủy khung mờ và hoàn trả lại nguyên liệu đã đóng
        /// </summary>
        private void CancelBlueprint()
        {
            var inv = InventoryManager.Instance;
            int totalRefundedWood = 0;

            for (int i = 0; i < ingredientIDs.Length; i++)
            {
                int refundAmount = ingredientCurrent[i];
                if (refundAmount <= 0) continue;

                string id = ingredientIDs[i];
                ItemData itemData = GetItemDataByID(id);

                if (inv != null && itemData != null)
                {
                    inv.AddItem(itemData, refundAmount);
                    if (id == "wood") totalRefundedWood += refundAmount;
                }
            }

            if (cancelSound != null)
            {
                AudioSource.PlayClipAtPoint(cancelSound, transform.position, 1f);
            }

            string notice = totalRefundedWood > 0
                ? string.Format("Đã hủy khung mờ và hoàn trả {0} Gỗ vào túi đồ!", totalRefundedWood)
                : "Đã hủy khung mờ công trình!";

            if (BuildingSystem.Instance != null)
            {
                BuildingSystem.Instance.ShowExternalNotice(notice);
            }

            Destroy(gameObject);
        }

        private ItemData GetItemDataByID(string id)
        {
            ItemData data = Resources.Load<ItemData>("Items/" + id);
            if (data == null) data = Resources.Load<ItemData>(id);
            if (data == null)
            {
                if (id == "wood")
                {
                    data = Resources.Load<ItemData>("Items/WoodItem");
                    if (data == null) data = Resources.Load<ItemData>("WoodItem");
                }
                else if (id == "stone")
                {
                    data = Resources.Load<ItemData>("Items/StoneItem");
                    if (data == null) data = Resources.Load<ItemData>("StoneItem");
                }
                else if (id == "leaf")
                {
                    data = Resources.Load<ItemData>("Items/LeafItem");
                    if (data == null) data = Resources.Load<ItemData>("LeafItem");
                }
            }

            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ItemData>();
                data.itemID = id;
                data.itemName = id == "wood" ? "Gỗ" : (id == "stone" ? "Đá" : "Lá Cây");
                data.maxStack = 99;
            }
            return data;
        }

        private void SpawnCompletionBurst(Vector3 pos)
        {
            GameObject fx = new GameObject("BuildCompleteFX");
            fx.transform.position = pos;
            ParticleSystem ps = fx.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startLifetime = 1.0f;
            main.startSpeed = 4.5f;
            main.startSize = 0.22f;
            main.startColor = new Color(0.85f, 0.65f, 0.4f);
            main.maxParticles = 40;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 35) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1.2f;

            Destroy(fx, 1.8f);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(0.92f, 1.08f);
                audioSource.PlayOneShot(clip, 1f);
            }
        }

        private void ShowFeedback(string text)
        {
            feedbackMessage = text;
            feedbackTimer = 2.5f;
        }

        // =============================================
        // ONGUI HUD (PHONG CÁCH THE FOREST)
        // =============================================
        void InitStyles()
        {
            if (stylesReady) return;
            stylesReady = true;

            hudBoxStyle = new GUIStyle(GUI.skin.box);
            hudBoxStyle.normal.background = MakeTex(2, 2, new Color(0.08f, 0.10f, 0.12f, 0.94f));
            hudBoxStyle.padding = new RectOffset(15, 15, 12, 12);

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 17;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = new Color(1f, 0.85f, 0.35f);
            titleStyle.alignment = TextAnchor.MiddleLeft;

            ingRowStyle = new GUIStyle(GUI.skin.label);
            ingRowStyle.fontSize = 15;
            ingRowStyle.fontStyle = FontStyle.Bold;
            ingRowStyle.normal.textColor = Color.white;
            ingRowStyle.alignment = TextAnchor.MiddleLeft;

            ingDetailStyle = new GUIStyle(GUI.skin.label);
            ingDetailStyle.fontSize = 13;
            ingDetailStyle.fontStyle = FontStyle.Bold;
            ingDetailStyle.normal.textColor = new Color(0.75f, 0.95f, 0.75f);
            ingDetailStyle.alignment = TextAnchor.MiddleRight;

            promptStyle = new GUIStyle(GUI.skin.label);
            promptStyle.fontSize = 14;
            promptStyle.fontStyle = FontStyle.Bold;
            promptStyle.normal.textColor = new Color(0.4f, 1f, 0.5f);
            promptStyle.alignment = TextAnchor.MiddleLeft;

            noticeStyle = new GUIStyle(GUI.skin.label);
            noticeStyle.fontSize = 14;
            noticeStyle.fontStyle = FontStyle.Bold;
            noticeStyle.normal.textColor = new Color(1f, 0.9f, 0.3f);
            noticeStyle.alignment = TextAnchor.MiddleCenter;
        }

        void OnGUI()
        {
            if (!isPlayerLooking) return;

            InitStyles();

            // Tính toán tổng số lượng
            int totalCur = 0;
            int totalReq = 0;
            for (int i = 0; i < ingredientRequired.Length; i++)
            {
                totalCur += ingredientCurrent[i];
                totalReq += ingredientRequired[i];
            }
            float progress = totalReq > 0 ? (float)totalCur / totalReq : 1f;

            // Bảng HUD ở phía dưới màn hình
            float boxWidth = 470f;
            float rowHeight = 26f;
            float boxHeight = 110f + ingredientIDs.Length * rowHeight;
            float boxX = (Screen.width - boxWidth) / 2f;
            float boxY = Screen.height - boxHeight - 70f;

            GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), "", hudBoxStyle);

            // 1. Tiêu đề
            GUI.Label(new Rect(boxX + 15, boxY + 10, boxWidth - 30, 24), "🔨 KHUNG XÂY DỰNG: " + structureName, titleStyle);

            // 2. Thanh tiến độ tổng
            float barX = boxX + 15;
            float barY = boxY + 36;
            float barW = boxWidth - 30;
            float barH = 12f;

            // Nền thanh tiến độ
            GUI.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);
            GUI.DrawTexture(new Rect(barX, barY, barW, barH), Texture2D.whiteTexture);

            // Phần trăm hoàn thành
            GUI.color = new Color(0.3f, 0.85f, 0.45f, 0.95f);
            GUI.DrawTexture(new Rect(barX, barY, barW * Mathf.Clamp01(progress), barH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // 3. Danh sách nguyên liệu yêu cầu
            float curY = barY + 20;

            for (int i = 0; i < ingredientIDs.Length; i++)
            {
                string id = ingredientIDs[i];
                string name = ingredientDisplayNames[i];
                int cur = ingredientCurrent[i];
                int req = ingredientRequired[i];
                int inBag = GetMatchingItemCount(id);

                string icon = id.ToLower() == "wood" ? "🪵" : (id.ToLower() == "stone" ? "🪨" : "🌿");
                string leftText = string.Format("{0} {1}: {2} / {3}", icon, name, cur, req);
                string rightText = string.Format("[Trong túi: {0}]", inBag);

                GUI.Label(new Rect(barX, curY, barW - 130, rowHeight), leftText, ingRowStyle);

                // Màu số lượng trong túi (xanh nếu có, cam nếu thiếu)
                ingDetailStyle.normal.textColor = inBag > 0 ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);
                GUI.Label(new Rect(barX + barW - 130, curY, 130, rowHeight), rightText, ingDetailStyle);

                curY += rowHeight;
            }

            // 4. Hướng dẫn thao tác
            bool hasAnyInBag = false;
            for (int i = 0; i < ingredientIDs.Length; i++)
            {
                if (GetMatchingItemCount(ingredientIDs[i]) > 0 && ingredientCurrent[i] < ingredientRequired[i])
                {
                    hasAnyInBag = true;
                    break;
                }
            }

            curY += 4;
            if (hasAnyInBag)
            {
                promptStyle.normal.textColor = new Color(0.35f, 1f, 0.45f);
                GUI.Label(new Rect(barX, curY, barW, 22), "👉 Bấm [E] hoặc Giữ [E]: Đóng gỗ/nguyên liệu vào khung", promptStyle);
            }
            else
            {
                promptStyle.normal.textColor = new Color(1f, 0.45f, 0.45f);
                GUI.Label(new Rect(barX, curY, barW, 22), "❌ Cần thêm nguyên liệu! Chặt cây gom gỗ để tiếp tục.", promptStyle);
            }

            // 5. Nút hủy
            curY += 22;
            if (holdCancelTimer > 0f)
            {
                float cancelPct = Mathf.Clamp01(holdCancelTimer / CANCEL_HOLD_DURATION);
                GUIStyle cancelActiveStyle = new GUIStyle(promptStyle);
                cancelActiveStyle.normal.textColor = new Color(1f, 0.7f, 0.2f);
                GUI.Label(new Rect(barX, curY, barW, 20), string.Format("⚠️ ĐANG HỦY KHUNG MỜ... [{0}%]", Mathf.RoundToInt(cancelPct * 100)), cancelActiveStyle);
            }
            else
            {
                GUIStyle cancelDimStyle = new GUIStyle(promptStyle);
                cancelDimStyle.fontSize = 12;
                cancelDimStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                GUI.Label(new Rect(barX, curY, barW, 20), "Giữ [C]: Hủy khung mờ (Hoàn trả nguyên liệu đã đóng)", cancelDimStyle);
            }

            // 6. Thông báo phản hồi ngắn
            if (feedbackTimer > 0f && !string.IsNullOrEmpty(feedbackMessage))
            {
                float noticeY = boxY - 32f;
                GUI.Label(new Rect(boxX, noticeY, boxWidth, 26), feedbackMessage, noticeStyle);
            }
        }

        private Texture2D MakeTex(int w, int h, Color color)
        {
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = color;
            Texture2D tex = new Texture2D(w, h);
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }
    }
}

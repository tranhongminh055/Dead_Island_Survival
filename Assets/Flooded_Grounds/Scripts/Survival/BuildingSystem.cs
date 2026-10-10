using UnityEngine;
using System.Collections.Generic;

namespace HorrorGame.Survival
{
    /// <summary>
    /// Hệ thống xây dựng kiểu The Forest.
    /// Bấm B mở Build Menu → Chọn công trình → Ghost Preview → Click trái để đặt.
    /// Gắn lên Player (cùng GameObject với PlayerController).
    /// </summary>
    public class BuildingSystem : MonoBehaviour
    {
        public static BuildingSystem Instance;

        [Header("Cài đặt")]
        public KeyCode buildMenuKey = KeyCode.B;
        public float buildRange = 10f;              // Tầm xa tối đa đặt công trình
        public LayerMask groundLayer = ~0;           // Layer mặt đất (mặc định tất cả)

        [Header("Danh sách công thức xây dựng")]
        [Tooltip("Kéo thả BuildingRecipe ScriptableObject vào đây. Nếu để trống, hệ thống sẽ tự tạo công trình mặc định.")]
        public List<BuildingRecipe> recipes = new List<BuildingRecipe>();

        [Header("Âm thanh")]
        public AudioClip buildSound;
        public AudioClip cancelSound;
        public AudioClip placeMaterialSound;

        // === State ===
        private bool isBuildMenuOpen = false;
        private bool isInBuildMode = false;         // Đang đặt ghost preview
        private int selectedRecipeIndex = -1;

        // Ghost preview
        private GameObject ghostPreview;
        private float ghostRotation = 0f;
        private bool canPlace = true;
        private Material ghostValidMat;
        private Material ghostInvalidMat;
        private Material ghostBlueprintMat;

        // Built-in recipes (khi chưa có ScriptableObject)
        private List<BuiltInRecipe> builtInRecipes = new List<BuiltInRecipe>();

        // Camera reference
        private Transform playerCamera;
        private AudioSource audioSource;

        // Scroll position cho menu
        private Vector2 menuScrollPos;

        // UI
        private GUIStyle menuBoxStyle;
        private GUIStyle titleStyle;
        private GUIStyle itemStyle;
        private GUIStyle itemHoverStyle;
        private GUIStyle descStyle;
        private GUIStyle ingredientStyle;
        private GUIStyle insufficientStyle;
        private GUIStyle noticeStyle;
        private GUIStyle noticeShadowStyle;
        private string noticeText = "";
        private float noticeTimer = 0f;
        private bool stylesInitialized = false;

        // =============================================
        // Cấu trúc built-in recipe (không cần ScriptableObject)
        // =============================================
        [System.Serializable]
        private class BuiltInRecipe
        {
            public string name;
            public string description;
            public string[] ingredientNames;    // Tên ItemID nguyên liệu
            public int[] ingredientAmounts;      // Số lượng tương ứng
            public string[] ingredientDisplayNames; // Tên hiển thị
            public System.Func<GameObject> createFunc; // Hàm tạo prefab
            public BuildingCategory category;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        void Start()
        {
            // Tìm Camera
            Camera cam = Camera.main;
            if (cam == null) cam = GetComponentInChildren<Camera>();
            if (cam != null) playerCamera = cam.transform;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            if (placeMaterialSound == null)
            {
#if UNITY_EDITOR
                placeMaterialSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/sound axe chop tree/wings_of_freedom-chopping-wood-435769.mp3");
                if (placeMaterialSound == null)
                    placeMaterialSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/sound axe chop tree/litupsubway-ui-equip-sfx-513361.mp3");
#endif
            }

            // Tạo material cho ghost preview
            CreateGhostMaterials();

            // Nếu chưa có recipe ScriptableObject, tạo built-in recipes
            if (recipes == null || recipes.Count == 0)
            {
                InitBuiltInRecipes();
            }

            Debug.Log("=== HỆ THỐNG XÂY DỰNG SẴN SÀNG! Bấm [B] để mở Build Menu ===");
        }

        void CreateGhostMaterials()
        {
            // Material xanh lá bán trong suốt (đặt được)
            ghostValidMat = new Material(Shader.Find("Standard"));
            ghostValidMat.color = new Color(0.2f, 0.9f, 0.3f, 0.35f);
            ghostValidMat.SetFloat("_Mode", 3); // Transparent
            ghostValidMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            ghostValidMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            ghostValidMat.SetInt("_ZWrite", 0);
            ghostValidMat.DisableKeyword("_ALPHATEST_ON");
            ghostValidMat.EnableKeyword("_ALPHABLEND_ON");
            ghostValidMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            ghostValidMat.renderQueue = 3000;

            // Material đỏ bán trong suốt (không đặt được)
            ghostInvalidMat = new Material(Shader.Find("Standard"));
            ghostInvalidMat.color = new Color(0.9f, 0.2f, 0.2f, 0.35f);
            ghostInvalidMat.SetFloat("_Mode", 3);
            ghostInvalidMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            ghostInvalidMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            ghostInvalidMat.SetInt("_ZWrite", 0);
            ghostInvalidMat.DisableKeyword("_ALPHATEST_ON");
            ghostInvalidMat.EnableKeyword("_ALPHABLEND_ON");
            ghostInvalidMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            ghostInvalidMat.renderQueue = 3000;

            // Material khung mờ The Forest (xanh cyan sáng bán trong suốt hiển thị khung định vị trong thế giới)
            ghostBlueprintMat = new Material(Shader.Find("Standard"));
            ghostBlueprintMat.color = new Color(0.35f, 0.75f, 1.0f, 0.38f);
            ghostBlueprintMat.SetFloat("_Mode", 3);
            ghostBlueprintMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            ghostBlueprintMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            ghostBlueprintMat.SetInt("_ZWrite", 0);
            ghostBlueprintMat.DisableKeyword("_ALPHATEST_ON");
            ghostBlueprintMat.EnableKeyword("_ALPHABLEND_ON");
            ghostBlueprintMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            ghostBlueprintMat.renderQueue = 3000;
        }

        void InitBuiltInRecipes()
        {
            builtInRecipes.Clear();

            // 1. Nhà Gỗ
            builtInRecipes.Add(new BuiltInRecipe
            {
                name = "🏠 Nhà Gỗ",
                description = "Nhà gỗ 4 tường có cửa ra vào và cửa sổ",
                ingredientNames = new string[] { "wood" },
                ingredientAmounts = new int[] { 20 },
                ingredientDisplayNames = new string[] { "Gỗ" },
                createFunc = BuildingPrefabGenerator.CreateWoodHouse,
                category = BuildingCategory.Shelter
            });

            // 2. Nhà Đá
            builtInRecipes.Add(new BuiltInRecipe
            {
                name = "🏰 Nhà Đá",
                description = "Nhà đá kiên cố với mái gỗ",
                ingredientNames = new string[] { "stone", "wood" },
                ingredientAmounts = new int[] { 15, 5 },
                ingredientDisplayNames = new string[] { "Đá", "Gỗ" },
                createFunc = BuildingPrefabGenerator.CreateStoneHouse,
                category = BuildingCategory.Shelter
            });

            // 3. Lều Lá (The Forest)
            builtInRecipes.Add(new BuiltInRecipe
            {
                name = "⛺ Lều Lá",
                description = "Lều tạm kiểu The Forest — Khung chữ A phủ lá cây",
                ingredientNames = new string[] { "leaf", "wood" },
                ingredientAmounts = new int[] { 10, 5 },
                ingredientDisplayNames = new string[] { "Lá cây", "Gỗ" },
                createFunc = BuildingPrefabGenerator.CreateLeafShelter,
                category = BuildingCategory.Shelter
            });

            // 4. Hàng Rào Gỗ
            builtInRecipes.Add(new BuiltInRecipe
            {
                name = "🪵 Hàng Rào",
                description = "Hàng rào gỗ bảo vệ khu vực",
                ingredientNames = new string[] { "wood" },
                ingredientAmounts = new int[] { 5 },
                ingredientDisplayNames = new string[] { "Gỗ" },
                createFunc = BuildingPrefabGenerator.CreateWoodFence,
                category = BuildingCategory.Structure
            });

            // 5. Lửa Trại
            builtInRecipes.Add(new BuiltInRecipe
            {
                name = "🔥 Lửa Trại",
                description = "Sưởi ấm và nướng chín thịt sống từ thú săn được",
                ingredientNames = new string[] { "wood", "stone" },
                ingredientAmounts = new int[] { 3, 2 },
                ingredientDisplayNames = new string[] { "Gỗ", "Đá" },
                createFunc = BuildingPrefabGenerator.CreateCampfire,
                category = BuildingCategory.Utility
            });

            // 6. Rương Đồ
            builtInRecipes.Add(new BuiltInRecipe
            {
                name = "📦 Rương Đồ",
                description = "Rương gỗ chứa đồ 12 ngăn để cất giữ vật phẩm và nguyên liệu",
                ingredientNames = new string[] { "wood" },
                ingredientAmounts = new int[] { 8 },
                ingredientDisplayNames = new string[] { "Gỗ" },
                createFunc = BuildingPrefabGenerator.CreateWoodChest,
                category = BuildingCategory.Utility
            });

            Debug.Log("[BuildingSystem] Đã tạo " + builtInRecipes.Count + " built-in recipes.");
        }

        void Update()
        {
            if (HorrorGame.UI.GameMenuManager.IsPaused) return;

            // Không nhận input khi Cutscene
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;

            // Không nhận input khi mở inventory
            if (Inventory.InventoryManager.Instance != null && Inventory.InventoryManager.Instance.isInventoryOpen) return;

            // Giảm notice timer
            if (noticeTimer > 0) noticeTimer -= Time.deltaTime;

            // Phím B: Mở/đóng Build Menu
            if (Input.GetKeyDown(buildMenuKey))
            {
                if (isInBuildMode)
                {
                    CancelBuildMode();
                }
                else
                {
                    ToggleBuildMenu();
                }
            }

            // ESC hoặc Click phải: Hủy build mode
            if (isInBuildMode)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
                {
                    CancelBuildMode();
                    return;
                }

                UpdateGhostPreview();

                // Xoay công trình: Scroll chuột hoặc phím Q / E / R
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    ghostRotation += scroll * 60f;
                }
                if (Input.GetKey(KeyCode.Q))
                {
                    ghostRotation -= 90f * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.R))
                {
                    ghostRotation += 90f * Time.deltaTime;
                }

                // Click trái để đặt
                if (Input.GetMouseButtonDown(0) && canPlace)
                {
                    PlaceBuilding();
                }
            }
        }

        // =============================================
        // MENU XÂY DỰNG
        // =============================================
        void ToggleBuildMenu()
        {
            isBuildMenuOpen = !isBuildMenuOpen;

            if (isBuildMenuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void SelectRecipe(int index)
        {
            selectedRecipeIndex = index;
            isBuildMenuOpen = false;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Vào build mode đặt khung mờ (chuẩn phong cách The Forest - không cần đủ nguyên liệu trước)
            isInBuildMode = true;
            ghostRotation = 0f;
            CreateGhostPreview(index);

            // Cất rìu nếu đang cầm
            AxeController axe = FindObjectOfType<AxeController>();
            if (axe != null && axe.IsEquipped) axe.ForceUnequip();

            ShowNotice("🖱️ Click trái: Đặt khung mờ  |  Click phải / ESC: Hủy  |  Q / E / Scroll: Xoay");
        }

        // =============================================
        // GHOST PREVIEW
        // =============================================
        void CreateGhostPreview(int index)
        {
            if (ghostPreview != null) Destroy(ghostPreview);

            // Tạo preview từ built-in recipe
            if (index < builtInRecipes.Count)
            {
                ghostPreview = builtInRecipes[index].createFunc();
            }
            else if (recipes.Count > 0 && index - builtInRecipes.Count < recipes.Count)
            {
                // Từ ScriptableObject recipes
                BuildingRecipe recipe = recipes[index - builtInRecipes.Count];
                if (recipe.previewPrefab != null)
                    ghostPreview = Instantiate(recipe.previewPrefab);
                else if (recipe.resultPrefab != null)
                    ghostPreview = Instantiate(recipe.resultPrefab);
            }

            if (ghostPreview == null) return;

            ghostPreview.name = "BuildPreview_Ghost";

            // Tắt mọi collider trên ghost (để không va chạm)
            Collider[] cols = ghostPreview.GetComponentsInChildren<Collider>();
            foreach (Collider c in cols) c.enabled = false;

            // Áp dụng material xanh bán trong suốt
            ApplyGhostMaterial(ghostPreview, ghostValidMat);
        }

        void UpdateGhostPreview()
        {
            if (ghostPreview == null) return;

            if (playerCamera == null)
            {
                Camera cam = Camera.main;
                if (cam == null) cam = GetComponentInChildren<Camera>();
                if (cam != null) playerCamera = cam.transform;
                if (playerCamera == null) return;
            }

            // Raycast từ camera ra phía trước để tìm mặt đất (bắt đầu cách camera 0.5m để không trúng player)
            RaycastHit hit;
            Ray ray = new Ray(playerCamera.position + playerCamera.forward * 0.5f, playerCamera.forward);

            if (Physics.Raycast(ray, out hit, buildRange, groundLayer))
            {
                ghostPreview.transform.position = hit.point;
                ghostPreview.transform.rotation = Quaternion.Euler(0, ghostRotation, 0);

                // Kiểm tra có chướng ngại vật không
                canPlace = CheckCanPlace(hit);

                // Đổi màu ghost
                ApplyGhostMaterial(ghostPreview, canPlace ? ghostValidMat : ghostInvalidMat);
            }
            else
            {
                // Không raycast trúng gì → đặt xa ra
                ghostPreview.transform.position = playerCamera.position + playerCamera.forward * buildRange;
                canPlace = false;
                ApplyGhostMaterial(ghostPreview, ghostInvalidMat);
            }
        }

        private const float WATER_LEVEL = 15.6f;
        private string invalidPlacementReason = "";

        bool CheckCanPlace(RaycastHit hit)
        {
            // 1. Kiểm tra dưới nước
            if (hit.point.y < WATER_LEVEL)
            {
                invalidPlacementReason = "Không thể xây dựng dưới nước!";
                return false;
            }

            // 2. Kiểm tra độ dốc địa hình
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle > 38f)
            {
                invalidPlacementReason = "Địa hình quá dốc để đặt móng!";
                return false;
            }

            // 3. Kiểm tra va chạm với các vật cản lớn
            Collider[] overlaps = Physics.OverlapSphere(hit.point + Vector3.up * 1f, 1.1f);
            foreach (Collider col in overlaps)
            {
                if (col.GetComponent<Terrain>() != null) continue;
                if (col.CompareTag("Player")) continue;
                if (ghostPreview != null && col.transform.IsChildOf(ghostPreview.transform)) continue;

                // Có vật thể cứng cản trở
                if (col.gameObject.isStatic || col.GetComponent<Rigidbody>() != null)
                {
                    invalidPlacementReason = "Vị trí bị vật cản che khuất!";
                    return false;
                }
            }

            invalidPlacementReason = "";
            return true;
        }

        void ApplyGhostMaterial(GameObject obj, Material mat)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                Material[] mats = new Material[r.materials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.materials = mats;
            }
        }

        // =============================================
        // ĐẶT CÔNG TRÌNH (THE FOREST BLUEPRINT SYSTEM)
        // =============================================
        void PlaceBuilding()
        {
            if (ghostPreview == null || selectedRecipeIndex < 0) return;

            // Ghi nhớ vị trí và rotation
            Vector3 pos = ghostPreview.transform.position;
            Quaternion rot = ghostPreview.transform.rotation;

            // Lấy thông tin recipe
            string recipeName = "Công trình";
            string[] ingNames = new string[] { "wood" };
            string[] ingDisplays = new string[] { "Gỗ" };
            int[] ingAmounts = new int[] { 20 };
            System.Func<GameObject> createFunc = null;
            GameObject resultPrefab = null;

            if (selectedRecipeIndex < builtInRecipes.Count)
            {
                BuiltInRecipe bir = builtInRecipes[selectedRecipeIndex];
                recipeName = bir.name;
                ingNames = bir.ingredientNames;
                ingDisplays = bir.ingredientDisplayNames;
                ingAmounts = bir.ingredientAmounts;
                createFunc = bir.createFunc;
            }
            else if (recipes.Count > 0)
            {
                int recipeIdx = selectedRecipeIndex - builtInRecipes.Count;
                if (recipeIdx < recipes.Count)
                {
                    BuildingRecipe r = recipes[recipeIdx];
                    recipeName = r.recipeName;
                    resultPrefab = r.resultPrefab;
                    if (r.ingredients != null && r.ingredients.Length > 0)
                    {
                        ingNames = new string[r.ingredients.Length];
                        ingDisplays = new string[r.ingredients.Length];
                        ingAmounts = new int[r.ingredients.Length];
                        for (int i = 0; i < r.ingredients.Length; i++)
                        {
                            ingNames[i] = r.ingredients[i].item != null ? r.ingredients[i].item.itemID : "wood";
                            ingDisplays[i] = r.ingredients[i].item != null ? r.ingredients[i].item.itemName : "Gỗ";
                            ingAmounts[i] = r.ingredients[i].amount;
                        }
                    }
                }
            }

            // Xóa ghost preview tạm thời
            Destroy(ghostPreview);
            ghostPreview = null;

            // Tạo GameObject Khung Mờ (Blueprint Ghost) đặt cố định trong thế giới
            GameObject blueprintObj = null;
            if (createFunc != null)
            {
                blueprintObj = createFunc();
            }
            else if (resultPrefab != null)
            {
                blueprintObj = Instantiate(resultPrefab);
            }

            if (blueprintObj != null)
            {
                blueprintObj.name = "Blueprint_" + recipeName;
                blueprintObj.transform.position = pos;
                blueprintObj.transform.rotation = rot;

                // Gắn ConstructionBlueprint phong cách The Forest
                ConstructionBlueprint bp = blueprintObj.AddComponent<ConstructionBlueprint>();
                bp.Initialize(
                    recipeName,
                    ingNames,
                    ingDisplays,
                    ingAmounts,
                    createFunc,
                    resultPrefab,
                    ghostBlueprintMat,
                    placeMaterialSound,
                    buildSound,
                    cancelSound
                );

                ShowNotice(string.Format("🔨 Đã đặt khung mờ {0}! Chặt gỗ rồi đến gần bấm [E] để đóng từng thanh gỗ.", recipeName));
                Debug.Log(string.Format("[BuildingSystem] Đã đặt khung mờ {0} tại {1}", recipeName, pos));
            }

            // Phát âm thanh đặt khung
            AudioClip clickSnd = cancelSound != null ? cancelSound : buildSound;
            if (clickSnd != null) audioSource.PlayOneShot(clickSnd);

            // Thoát build mode
            isInBuildMode = false;
            selectedRecipeIndex = -1;
        }

        void CancelBuildMode()
        {
            if (ghostPreview != null) Destroy(ghostPreview);
            ghostPreview = null;
            isInBuildMode = false;
            selectedRecipeIndex = -1;

            if (isBuildMenuOpen)
            {
                isBuildMenuOpen = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (cancelSound != null) audioSource.PlayOneShot(cancelSound);
        }

        // =============================================
        // KIỂM TRA & TIÊU THỤ NGUYÊN LIỆU
        // =============================================
        bool HasEnoughIngredients(int index)
        {
            if (index < builtInRecipes.Count)
            {
                var recipe = builtInRecipes[index];
                for (int i = 0; i < recipe.ingredientNames.Length; i++)
                {
                    if (ConstructionBlueprint.GetMatchingItemCount(recipe.ingredientNames[i]) < recipe.ingredientAmounts[i])
                        return false;
                }
                return true;
            }
            else
            {
                int recipeIdx = index - builtInRecipes.Count;
                if (recipeIdx < recipes.Count)
                    return recipes[recipeIdx].CanBuild();
            }
            return false;
        }

        bool ConsumeIngredients(int index)
        {
            if (index < builtInRecipes.Count)
            {
                var recipe = builtInRecipes[index];

                // Kiểm tra đủ trước khi trừ
                for (int i = 0; i < recipe.ingredientNames.Length; i++)
                {
                    if (ConstructionBlueprint.GetMatchingItemCount(recipe.ingredientNames[i]) < recipe.ingredientAmounts[i])
                        return false;
                }

                // Trừ nguyên liệu
                for (int i = 0; i < recipe.ingredientNames.Length; i++)
                {
                    ConstructionBlueprint.RemoveMatchingItem(recipe.ingredientNames[i], recipe.ingredientAmounts[i]);
                }
                return true;
            }
            else
            {
                int recipeIdx = index - builtInRecipes.Count;
                if (recipeIdx < recipes.Count)
                    return recipes[recipeIdx].ConsumeIngredients();
            }
            return false;
        }

        int GetIngredientCount(string itemID)
        {
            return ConstructionBlueprint.GetMatchingItemCount(itemID);
        }

        // =============================================
        // CẤP NGUYÊN LIỆU THỬ NGHIỆM
        // =============================================
        public void GiveTestMaterials()
        {
            var inv = Inventory.InventoryManager.Instance;
            if (inv == null) return;

            HorrorGame.Inventory.ItemData wood = Resources.Load<HorrorGame.Inventory.ItemData>("Items/WoodItem");
            if (wood == null) wood = Resources.Load<HorrorGame.Inventory.ItemData>("WoodItem");
            HorrorGame.Inventory.ItemData stone = Resources.Load<HorrorGame.Inventory.ItemData>("Items/StoneItem");
            if (stone == null) stone = Resources.Load<HorrorGame.Inventory.ItemData>("StoneItem");
            HorrorGame.Inventory.ItemData leaf = Resources.Load<HorrorGame.Inventory.ItemData>("Items/LeafItem");
            if (leaf == null) leaf = Resources.Load<HorrorGame.Inventory.ItemData>("LeafItem");

            if (wood == null)
            {
                wood = ScriptableObject.CreateInstance<HorrorGame.Inventory.ItemData>();
                wood.itemID = "wood";
                wood.itemName = "Gỗ";
                wood.maxStack = 99;
            }
            if (stone == null)
            {
                stone = ScriptableObject.CreateInstance<HorrorGame.Inventory.ItemData>();
                stone.itemID = "stone";
                stone.itemName = "Đá";
                stone.maxStack = 99;
            }
            if (leaf == null)
            {
                leaf = ScriptableObject.CreateInstance<HorrorGame.Inventory.ItemData>();
                leaf.itemID = "leaf";
                leaf.itemName = "Lá Cây";
                leaf.maxStack = 99;
            }

            inv.AddItem(wood, 50);
            inv.AddItem(stone, 30);
            inv.AddItem(leaf, 20);
            ShowNotice("✅ Đã nhận 50 Gỗ, 30 Đá, 20 Lá để trải nghiệm xây dựng!");
        }

        // =============================================
        // HIỂN THỊ THÔNG BÁO
        // =============================================
        void ShowNotice(string text)
        {
            noticeText = text;
            noticeTimer = 3f;
        }

        public void ShowExternalNotice(string text)
        {
            ShowNotice(text);
        }

        // =============================================
        // GUI: BUILD MENU + THÔNG BÁO
        // =============================================
        void InitStyles()
        {
            if (stylesInitialized) return;
            stylesInitialized = true;

            menuBoxStyle = new GUIStyle(GUI.skin.box);
            menuBoxStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.12f, 0.92f));
            menuBoxStyle.padding = new RectOffset(15, 15, 10, 10);

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 26;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
            titleStyle.alignment = TextAnchor.MiddleCenter;

            itemStyle = new GUIStyle(GUI.skin.button);
            itemStyle.fontSize = 18;
            itemStyle.fontStyle = FontStyle.Bold;
            itemStyle.normal.textColor = Color.white;
            itemStyle.alignment = TextAnchor.MiddleLeft;
            itemStyle.padding = new RectOffset(12, 12, 8, 8);

            itemHoverStyle = new GUIStyle(itemStyle);
            itemHoverStyle.normal.background = MakeTex(2, 2, new Color(0.3f, 0.6f, 0.3f, 0.6f));

            descStyle = new GUIStyle(GUI.skin.label);
            descStyle.fontSize = 13;
            descStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f);
            descStyle.wordWrap = true;

            ingredientStyle = new GUIStyle(GUI.skin.label);
            ingredientStyle.fontSize = 14;
            ingredientStyle.normal.textColor = new Color(0.4f, 1f, 0.4f);

            insufficientStyle = new GUIStyle(ingredientStyle);
            insufficientStyle.normal.textColor = new Color(1f, 0.3f, 0.3f);

            noticeStyle = new GUIStyle();
            noticeStyle.fontSize = 22;
            noticeStyle.fontStyle = FontStyle.Bold;
            noticeStyle.normal.textColor = Color.white;
            noticeStyle.alignment = TextAnchor.MiddleCenter;

            noticeShadowStyle = new GUIStyle(noticeStyle);
            noticeShadowStyle.normal.textColor = Color.black;
        }

        void OnGUI()
        {
            InitStyles();

            // Hiển thị thông báo
            if (noticeTimer > 0 && !string.IsNullOrEmpty(noticeText))
            {
                float alpha = Mathf.Clamp01(noticeTimer);
                noticeStyle.normal.textColor = new Color(1, 1, 1, alpha);
                noticeShadowStyle.normal.textColor = new Color(0, 0, 0, alpha);

                float nx = Screen.width / 2f - 300f;
                float ny = Screen.height * 0.18f;

                GUI.Label(new Rect(nx + 2, ny + 2, 600, 40), noticeText, noticeShadowStyle);
                GUI.Label(new Rect(nx, ny, 600, 40), noticeText, noticeStyle);
            }

            // Build Menu
            if (isBuildMenuOpen)
            {
                DrawBuildMenu();
            }

            // Hướng dẫn khi đang trong build mode
            if (isInBuildMode)
            {
                DrawBuildModeHUD();
            }
        }

        void DrawBuildMenu()
        {
            float menuWidth = 420f;
            float menuHeight = 500f;
            float menuX = (Screen.width - menuWidth) / 2f;
            float menuY = (Screen.height - menuHeight) / 2f;

            GUI.Box(new Rect(menuX, menuY, menuWidth, menuHeight), "", menuBoxStyle);

            // Tiêu đề
            GUI.Label(new Rect(menuX, menuY + 10, menuWidth, 40), "🔨 XÂY DỰNG", titleStyle);

            // Nút test nguyên liệu (để trải nghiệm xây dựng ngay)
            GUIStyle testBtnStyle = new GUIStyle(GUI.skin.button);
            testBtnStyle.fontSize = 11;
            testBtnStyle.fontStyle = FontStyle.Bold;
            if (GUI.Button(new Rect(menuX + 15, menuY + 12, 110, 26), "+50 Gỗ/Đá (Test)", testBtnStyle))
            {
                GiveTestMaterials();
            }

            // Nút đóng
            GUIStyle closeStyle = new GUIStyle(GUI.skin.button);
            closeStyle.fontSize = 18;
            closeStyle.fontStyle = FontStyle.Bold;
            if (GUI.Button(new Rect(menuX + menuWidth - 40, menuY + 8, 30, 30), "✕", closeStyle))
            {
                ToggleBuildMenu();
                return;
            }

            // Đường kẻ ngang
            GUI.color = new Color(1, 0.85f, 0.3f, 0.5f);
            GUI.DrawTexture(new Rect(menuX + 15, menuY + 52, menuWidth - 30, 2), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Hướng dẫn
            GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
            hintStyle.fontSize = 12;
            hintStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            hintStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(menuX, menuY + 56, menuWidth, 20), "Đặt khung mờ định sẵn — Chặt gỗ đóng từng thanh vào khung", hintStyle);

            // Danh sách công trình
            float listY = menuY + 82;
            float listHeight = menuHeight - 92;

            menuScrollPos = GUI.BeginScrollView(
                new Rect(menuX + 10, listY, menuWidth - 20, listHeight),
                menuScrollPos,
                new Rect(0, 0, menuWidth - 40, builtInRecipes.Count * 100)
            );

            // Built-in recipes
            for (int i = 0; i < builtInRecipes.Count; i++)
            {
                DrawRecipeItem(i, builtInRecipes[i], menuWidth - 40);
            }

            GUI.EndScrollView();
        }

        void DrawRecipeItem(int index, BuiltInRecipe recipe, float width)
        {
            float itemHeight = 85f;
            float yPos = index * (itemHeight + 5);
            bool hasEnough = HasEnoughIngredients(index);

            // Nền item
            Color bgColor = hasEnough ? new Color(0.18f, 0.24f, 0.20f, 0.92f) : new Color(0.18f, 0.18f, 0.22f, 0.92f);
            GUI.color = bgColor;
            GUI.DrawTexture(new Rect(0, yPos, width, itemHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Tên công trình
            GUIStyle nameStyle = new GUIStyle();
            nameStyle.fontSize = 18;
            nameStyle.fontStyle = FontStyle.Bold;
            nameStyle.normal.textColor = new Color(1f, 0.9f, 0.45f);
            GUI.Label(new Rect(10, yPos + 5, width - 115, 28), recipe.name, nameStyle);

            // Mô tả
            GUI.Label(new Rect(10, yPos + 28, width - 20, 20), recipe.description, descStyle);

            // Nguyên liệu
            string ingredientText = "";
            for (int j = 0; j < recipe.ingredientNames.Length; j++)
            {
                int have = GetIngredientCount(recipe.ingredientNames[j]);
                int need = recipe.ingredientAmounts[j];
                string displayName = recipe.ingredientDisplayNames[j];

                if (j > 0) ingredientText += "   ";

                if (have >= need)
                    ingredientText += string.Format("[✓ {0}: {1}/{2}]", displayName, have, need);
                else
                    ingredientText += string.Format("[{0}: {1}/{2}]", displayName, have, need);
            }

            GUIStyle ingStyle = hasEnough ? ingredientStyle : new GUIStyle(ingredientStyle);
            if (!hasEnough) ingStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            GUI.Label(new Rect(10, yPos + 48, width - 20, 20), ingredientText, ingStyle);

            // Nút chọn đặt khung (luôn cho phép đặt khung mờ phong cách The Forest!)
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.fontSize = 13;
            btnStyle.fontStyle = FontStyle.Bold;
            btnStyle.normal.textColor = Color.white;

            if (GUI.Button(new Rect(width - 105, yPos + 6, 100, 32), "Đặt Khung ▶", btnStyle))
            {
                SelectRecipe(index);
            }
        }

        void DrawBuildModeHUD()
        {
            // Tâm ngắm xây dựng
            float cx = Screen.width / 2f;
            float cy = Screen.height / 2f;
            float crossSize = 15f;
            float crossThick = 2f;

            GUI.color = canPlace ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f);
            GUI.DrawTexture(new Rect(cx - crossSize, cy - crossThick / 2, crossSize * 2, crossThick), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - crossThick / 2, cy - crossSize, crossThick, crossSize * 2), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Hướng dẫn phím ở cuối màn hình
            GUIStyle guideStyle = new GUIStyle();
            guideStyle.fontSize = 15;
            guideStyle.fontStyle = FontStyle.Bold;
            guideStyle.normal.textColor = canPlace ? new Color(1f, 1f, 1f, 0.9f) : new Color(1f, 0.4f, 0.4f, 0.95f);
            guideStyle.alignment = TextAnchor.MiddleCenter;

            GUIStyle guideShadow = new GUIStyle(guideStyle);
            guideShadow.normal.textColor = new Color(0, 0, 0, 0.9f);

            string guide = canPlace
                ? "🖱️ Click trái: Đặt khung mờ định sẵn  |  Click phải / ESC: Hủy  |  Q / E / Scroll: Xoay"
                : "❌ " + (string.IsNullOrEmpty(invalidPlacementReason) ? "Không thể đặt tại đây!" : invalidPlacementReason) + "  |  Click phải / ESC: Hủy";

            float gy = Screen.height - 55f;
            float gw = 750f;
            float gx = (Screen.width - gw) / 2f;

            GUI.Label(new Rect(gx + 2, gy + 2, gw, 30), guide, guideShadow);
            GUI.Label(new Rect(gx, gy, gw, 30), guide, guideStyle);
        }

        // Tạo texture solid color cho GUI
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

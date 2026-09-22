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
            if (cam != null) playerCamera = cam.transform;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

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
                description = "Sưởi ấm và nấu ăn",
                ingredientNames = new string[] { "wood", "stone" },
                ingredientAmounts = new int[] { 3, 2 },
                ingredientDisplayNames = new string[] { "Gỗ", "Đá" },
                createFunc = BuildingPrefabGenerator.CreateCampfire,
                category = BuildingCategory.Utility
            });

            Debug.Log("[BuildingSystem] Đã tạo " + builtInRecipes.Count + " built-in recipes.");
        }

        void Update()
        {
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

                // Scroll để xoay
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    ghostRotation += scroll * 45f;
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

            // Kiểm tra đủ nguyên liệu
            if (!HasEnoughIngredients(index))
            {
                ShowNotice("❌ Không đủ nguyên liệu!");
                selectedRecipeIndex = -1;
                return;
            }

            // Vào build mode
            isInBuildMode = true;
            ghostRotation = 0f;
            CreateGhostPreview(index);

            // Cất rìu nếu đang cầm
            AxeController axe = FindObjectOfType<AxeController>();
            if (axe != null && axe.IsEquipped) axe.ForceUnequip();

            ShowNotice("Nhấn Click trái để đặt | Click phải / ESC để hủy | Scroll để xoay");
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
                if (cam != null) playerCamera = cam.transform;
                if (playerCamera == null) return;
            }

            // Raycast từ camera ra phía trước để tìm mặt đất
            RaycastHit hit;
            Ray ray = new Ray(playerCamera.position, playerCamera.forward);

            if (Physics.Raycast(ray, out hit, buildRange, groundLayer))
            {
                ghostPreview.transform.position = hit.point;
                ghostPreview.transform.rotation = Quaternion.Euler(0, ghostRotation, 0);

                // Kiểm tra có chướng ngại vật không (overlap test)
                canPlace = CheckCanPlace(hit.point);

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

        bool CheckCanPlace(Vector3 position)
        {
            // Kiểm tra overlap sphere xung quanh vị trí đặt
            // Bỏ qua terrain và mặt đất, chỉ quan tâm đến các vật thể khác
            Collider[] overlaps = Physics.OverlapSphere(position + Vector3.up * 1f, 1f);

            foreach (Collider col in overlaps)
            {
                // Bỏ qua terrain
                if (col.GetComponent<Terrain>() != null) continue;
                // Bỏ qua player
                if (col.CompareTag("Player")) continue;
                // Bỏ qua ghost preview
                if (col.transform.IsChildOf(ghostPreview.transform)) continue;

                // Có vật cản → không đặt được
                // (Chỉ cản khi vật đó là công trình đã đặt hoặc vật thể lớn)
                if (col.GetComponent<Rigidbody>() != null || col.gameObject.isStatic)
                {
                    return false;
                }
            }

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
        // ĐẶT CÔNG TRÌNH
        // =============================================
        void PlaceBuilding()
        {
            if (ghostPreview == null || selectedRecipeIndex < 0) return;

            // Trừ nguyên liệu
            if (!ConsumeIngredients(selectedRecipeIndex))
            {
                ShowNotice("❌ Không đủ nguyên liệu!");
                CancelBuildMode();
                return;
            }

            // Ghi nhớ vị trí và rotation
            Vector3 pos = ghostPreview.transform.position;
            Quaternion rot = ghostPreview.transform.rotation;

            // Xóa ghost
            Destroy(ghostPreview);
            ghostPreview = null;

            // Spawn công trình thật
            GameObject building = null;

            if (selectedRecipeIndex < builtInRecipes.Count)
            {
                building = builtInRecipes[selectedRecipeIndex].createFunc();
            }
            else if (recipes.Count > 0)
            {
                int recipeIdx = selectedRecipeIndex - builtInRecipes.Count;
                if (recipeIdx < recipes.Count && recipes[recipeIdx].resultPrefab != null)
                {
                    building = Instantiate(recipes[recipeIdx].resultPrefab);
                }
            }

            if (building != null)
            {
                building.transform.position = pos;
                building.transform.rotation = rot;

                // Bật lại collider (để chặn va chạm)
                Collider mainCol = building.GetComponent<Collider>();
                if (mainCol != null) mainCol.enabled = true;

                string buildName = selectedRecipeIndex < builtInRecipes.Count
                    ? builtInRecipes[selectedRecipeIndex].name
                    : "Công trình";
                ShowNotice("✅ Đã xây " + buildName + "!");
                Debug.Log(string.Format("[BuildingSystem] Đã đặt {0} tại {1}", buildName, pos));
            }

            // Phát âm thanh
            if (buildSound != null) audioSource.PlayOneShot(buildSound);

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
            var inv = Inventory.InventoryManager.Instance;
            if (inv == null) return false;

            if (index < builtInRecipes.Count)
            {
                var recipe = builtInRecipes[index];
                for (int i = 0; i < recipe.ingredientNames.Length; i++)
                {
                    if (inv.GetItemCountByID(recipe.ingredientNames[i]) < recipe.ingredientAmounts[i])
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
            var inv = Inventory.InventoryManager.Instance;
            if (inv == null) return false;

            if (index < builtInRecipes.Count)
            {
                var recipe = builtInRecipes[index];

                // Kiểm tra đủ trước khi trừ
                for (int i = 0; i < recipe.ingredientNames.Length; i++)
                {
                    if (inv.GetItemCountByID(recipe.ingredientNames[i]) < recipe.ingredientAmounts[i])
                        return false;
                }

                // Trừ nguyên liệu
                for (int i = 0; i < recipe.ingredientNames.Length; i++)
                {
                    inv.RemoveItemByID(recipe.ingredientNames[i], recipe.ingredientAmounts[i]);
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
            var inv = Inventory.InventoryManager.Instance;
            if (inv == null) return 0;
            return inv.GetItemCountByID(itemID);
        }

        // =============================================
        // HIỂN THỊ THÔNG BÁO
        // =============================================
        void ShowNotice(string text)
        {
            noticeText = text;
            noticeTimer = 3f;
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
            hintStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
            hintStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(menuX, menuY + 56, menuWidth, 20), "Chọn công trình để xây — Cần đủ nguyên liệu", hintStyle);

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
            Color bgColor = hasEnough ? new Color(0.18f, 0.22f, 0.18f, 0.9f) : new Color(0.22f, 0.15f, 0.15f, 0.9f);
            GUI.color = bgColor;
            GUI.DrawTexture(new Rect(0, yPos, width, itemHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Tên công trình
            GUIStyle nameStyle = new GUIStyle();
            nameStyle.fontSize = 18;
            nameStyle.fontStyle = FontStyle.Bold;
            nameStyle.normal.textColor = hasEnough ? Color.white : new Color(0.7f, 0.5f, 0.5f);
            GUI.Label(new Rect(10, yPos + 5, width - 20, 28), recipe.name, nameStyle);

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
                    ingredientText += string.Format("[✗ {0}: {1}/{2}]", displayName, have, need);
            }

            GUIStyle ingStyle = hasEnough ? ingredientStyle : insufficientStyle;
            GUI.Label(new Rect(10, yPos + 48, width - 20, 20), ingredientText, ingStyle);

            // Nút chọn
            if (hasEnough)
            {
                GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
                btnStyle.fontSize = 14;
                btnStyle.fontStyle = FontStyle.Bold;
                btnStyle.normal.textColor = Color.white;

                if (GUI.Button(new Rect(width - 75, yPos + 5, 70, 30), "Xây ▶", btnStyle))
                {
                    SelectRecipe(index);
                }
            }
            else
            {
                GUIStyle lblStyle = new GUIStyle(GUI.skin.label);
                lblStyle.fontSize = 12;
                lblStyle.normal.textColor = new Color(0.6f, 0.3f, 0.3f);
                lblStyle.alignment = TextAnchor.MiddleRight;
                GUI.Label(new Rect(width - 100, yPos + 5, 95, 30), "Thiếu NL", lblStyle);
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
            guideStyle.fontSize = 16;
            guideStyle.fontStyle = FontStyle.Bold;
            guideStyle.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            guideStyle.alignment = TextAnchor.MiddleCenter;

            GUIStyle guideShadow = new GUIStyle(guideStyle);
            guideShadow.normal.textColor = new Color(0, 0, 0, 0.8f);

            string guide = "🖱️ Click trái: Đặt  |  Click phải: Hủy  |  Scroll: Xoay  |  ESC: Thoát";
            float gy = Screen.height - 50f;
            float gw = 700f;
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

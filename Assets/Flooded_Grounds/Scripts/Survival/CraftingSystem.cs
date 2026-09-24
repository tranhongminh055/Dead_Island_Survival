using UnityEngine;
using System.Collections.Generic;
using HorrorGame.Inventory;

namespace HorrorGame.Survival
{
    /// <summary>
    /// Hệ thống Chế Tạo (Crafting) kiểu The Forest.
    /// Bấm C để mở/đóng menu chế tạo.
    /// Hiển thị danh sách công thức, kiểm tra nguyên liệu, craft ra item mới.
    /// 
    /// Gắn lên Player (cùng GameObject với PlayerController).
    /// </summary>
    public class CraftingSystem : MonoBehaviour
    {
        public static CraftingSystem Instance;

        [Header("── Cấu Hình ──")]
        public KeyCode craftMenuKey = KeyCode.C;

        // State
        private bool isMenuOpen = false;
        private int selectedRecipe = -1;
        private Vector2 scrollPos;
        private List<CraftRecipe> recipes = new List<CraftRecipe>();

        // Thông báo
        private string noticeText = "";
        private float noticeTimer = 0f;

        // UI Styles
        private GUIStyle boxStyle;
        private GUIStyle titleStyle;
        private GUIStyle recipeStyle;
        private GUIStyle recipeHoverStyle;
        private GUIStyle selectedStyle;
        private GUIStyle descStyle;
        private GUIStyle ingredientStyle;
        private GUIStyle ingredientMissStyle;
        private GUIStyle craftBtnStyle;
        private GUIStyle craftBtnDisabledStyle;
        private GUIStyle noticeStyle;
        private GUIStyle categoryStyle;
        private bool stylesInit = false;

        // Cấu trúc công thức chế tạo
        [System.Serializable]
        public class CraftRecipe
        {
            public string name;
            public string description;
            public string category;         // "Vũ Khí", "Y Tế", "Giáp", "Thức Ăn"
            public string[] ingredientIDs;
            public string[] ingredientNames;
            public int[] ingredientAmounts;
            public string resultItemID;
            public string resultItemName;
            public string resultDescription;
            public ItemType resultType;
            public int resultAmount;
            public bool resultStackable;
            public int resultMaxStack;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }

            InitRecipes();
        }

        void Update()
        {
            if (Input.GetKeyDown(craftMenuKey))
            {
                ToggleMenu();
            }

            if (noticeTimer > 0f)
            {
                noticeTimer -= Time.unscaledDeltaTime;
            }
        }

        private void ToggleMenu()
        {
            // Không mở khi đang trong cutscene
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;

            isMenuOpen = !isMenuOpen;
            selectedRecipe = -1;

            if (isMenuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Time.timeScale = 0f;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Time.timeScale = 1f;
            }
        }

        // ═══════════════════════════════════════════════════════
        // DANH SÁCH CÔNG THỨC CHẾ TẠO
        // ═══════════════════════════════════════════════════════

        private void InitRecipes()
        {
            recipes.Clear();

            // ── VŨ KHÍ ──
            recipes.Add(new CraftRecipe
            {
                name = "🗡️ Giáo Gỗ",
                description = "Giáo gỗ nhọn. Có thể đâm hoặc ném để săn thú.",
                category = "Vũ Khí",
                ingredientIDs = new[] { "wood", "stone" },
                ingredientNames = new[] { "Gỗ", "Đá" },
                ingredientAmounts = new[] { 2, 1 },
                resultItemID = "spear",
                resultItemName = "Giáo Gỗ",
                resultDescription = "Giáo gỗ nhọn dùng để săn thú và tự vệ.",
                resultType = ItemType.Weapon,
                resultAmount = 1,
                resultStackable = false,
                resultMaxStack = 1
            });

            recipes.Add(new CraftRecipe
            {
                name = "🏹 Cung",
                description = "Cung thô sơ. Dùng để săn thú từ xa.",
                category = "Vũ Khí",
                ingredientIDs = new[] { "wood", "animal_hide" },
                ingredientNames = new[] { "Gỗ", "Da Thú" },
                ingredientAmounts = new[] { 1, 1 },
                resultItemID = "bow",
                resultItemName = "Cung",
                resultDescription = "Cung thô sơ làm từ gỗ và dây da thú.",
                resultType = ItemType.Weapon,
                resultAmount = 1,
                resultStackable = false,
                resultMaxStack = 1
            });

            recipes.Add(new CraftRecipe
            {
                name = "🏹 Mũi Tên (x5)",
                description = "5 mũi tên gỗ dùng cho cung.",
                category = "Vũ Khí",
                ingredientIDs = new[] { "wood", "stone" },
                ingredientNames = new[] { "Gỗ", "Đá" },
                ingredientAmounts = new[] { 1, 1 },
                resultItemID = "arrow",
                resultItemName = "Mũi Tên",
                resultDescription = "Mũi tên gỗ cho cung.",
                resultType = ItemType.Ammunition,
                resultAmount = 5,
                resultStackable = true,
                resultMaxStack = 30
            });

            // ── Y TẾ ──
            recipes.Add(new CraftRecipe
            {
                name = "💊 Băng Gạc",
                description = "Băng gạc từ lá cây. Hồi 25 HP.",
                category = "Y Tế",
                ingredientIDs = new[] { "leaf" },
                ingredientNames = new[] { "Lá Cây" },
                ingredientAmounts = new[] { 2 },
                resultItemID = "bandage",
                resultItemName = "Băng Gạc",
                resultDescription = "Băng gạc thảo dược. Sử dụng để hồi 25 HP.",
                resultType = ItemType.Consumable,
                resultAmount = 1,
                resultStackable = true,
                resultMaxStack = 10
            });

            recipes.Add(new CraftRecipe
            {
                name = "🧴 Thuốc Thảo Dược",
                description = "Thuốc mạnh từ thảo dược. Hồi 50 HP.",
                category = "Y Tế",
                ingredientIDs = new[] { "leaf", "leaf" },
                ingredientNames = new[] { "Lá Cây", "Lá Cây" },
                ingredientAmounts = new[] { 5, 0 },
                resultItemID = "herbal_medicine",
                resultItemName = "Thuốc Thảo Dược",
                resultDescription = "Thuốc thảo dược mạnh. Hồi 50 HP.",
                resultType = ItemType.Consumable,
                resultAmount = 1,
                resultStackable = true,
                resultMaxStack = 5
            });

            // ── GIÁP ──
            recipes.Add(new CraftRecipe
            {
                name = "🛡️ Giáp Lá",
                description = "Giáp thô sơ từ lá và da thú. Giảm 20% sát thương.",
                category = "Giáp",
                ingredientIDs = new[] { "leaf", "animal_hide" },
                ingredientNames = new[] { "Lá Cây", "Da Thú" },
                ingredientAmounts = new[] { 10, 2 },
                resultItemID = "leaf_armor",
                resultItemName = "Giáp Lá",
                resultDescription = "Giáp bảo vệ thô sơ. Giảm 20% sát thương nhận vào.",
                resultType = ItemType.Tool,
                resultAmount = 1,
                resultStackable = false,
                resultMaxStack = 1
            });

            recipes.Add(new CraftRecipe
            {
                name = "🛡️ Giáp Da",
                description = "Giáp da thú chắc chắn. Giảm 40% sát thương.",
                category = "Giáp",
                ingredientIDs = new[] { "animal_hide", "stone" },
                ingredientNames = new[] { "Da Thú", "Đá" },
                ingredientAmounts = new[] { 5, 3 },
                resultItemID = "hide_armor",
                resultItemName = "Giáp Da",
                resultDescription = "Giáp da thú chắc chắn. Giảm 40% sát thương.",
                resultType = ItemType.Tool,
                resultAmount = 1,
                resultStackable = false,
                resultMaxStack = 1
            });

            // ── THỨC ĂN ──
            recipes.Add(new CraftRecipe
            {
                name = "🍖 Thịt Khô",
                description = "Thịt chín sấy khô. Bảo quản lâu, hồi 40 độ no.",
                category = "Thức Ăn",
                ingredientIDs = new[] { "cooked_meat" },
                ingredientNames = new[] { "Thịt Chín" },
                ingredientAmounts = new[] { 2 },
                resultItemID = "dried_meat",
                resultItemName = "Thịt Khô",
                resultDescription = "Thịt khô bảo quản. Ăn hồi 40 độ no.",
                resultType = ItemType.Consumable,
                resultAmount = 1,
                resultStackable = true,
                resultMaxStack = 10
            });

            // ── CÔNG CỤ ──
            recipes.Add(new CraftRecipe
            {
                name = "🪓 Rìu Đá",
                description = "Rìu đá nâng cấp. Chặt nhanh hơn 50%.",
                category = "Công Cụ",
                ingredientIDs = new[] { "wood", "stone" },
                ingredientNames = new[] { "Gỗ", "Đá" },
                ingredientAmounts = new[] { 3, 5 },
                resultItemID = "stone_axe",
                resultItemName = "Rìu Đá",
                resultDescription = "Rìu đá nâng cấp. Chặt cây nhanh hơn 50%.",
                resultType = ItemType.Tool,
                resultAmount = 1,
                resultStackable = false,
                resultMaxStack = 1
            });

            recipes.Add(new CraftRecipe
            {
                name = "🔦 Đuốc",
                description = "Đuốc gỗ. Chiếu sáng trong đêm tối.",
                category = "Công Cụ",
                ingredientIDs = new[] { "wood", "leaf" },
                ingredientNames = new[] { "Gỗ", "Lá Cây" },
                ingredientAmounts = new[] { 1, 2 },
                resultItemID = "torch",
                resultItemName = "Đuốc",
                resultDescription = "Đuốc gỗ. Chiếu sáng xung quanh trong đêm.",
                resultType = ItemType.Tool,
                resultAmount = 1,
                resultStackable = false,
                resultMaxStack = 1
            });
        }

        // ═══════════════════════════════════════════════════════
        // CHẾ TẠO
        // ═══════════════════════════════════════════════════════

        private bool CanCraft(CraftRecipe recipe)
        {
            InventoryManager inv = InventoryManager.Instance;
            if (inv == null) return false;

            for (int i = 0; i < recipe.ingredientIDs.Length; i++)
            {
                if (recipe.ingredientAmounts[i] <= 0) continue;
                if (inv.GetItemCountByID(recipe.ingredientIDs[i]) < recipe.ingredientAmounts[i])
                    return false;
            }
            return true;
        }

        private void DoCraft(CraftRecipe recipe)
        {
            InventoryManager inv = InventoryManager.Instance;
            if (inv == null || !CanCraft(recipe)) return;

            // Trừ nguyên liệu
            for (int i = 0; i < recipe.ingredientIDs.Length; i++)
            {
                if (recipe.ingredientAmounts[i] <= 0) continue;
                inv.RemoveItemByID(recipe.ingredientIDs[i], recipe.ingredientAmounts[i]);
            }

            // Tạo item mới
            ItemData resultItem = GetOrCreateItemData(
                recipe.resultItemID, recipe.resultItemName, recipe.resultDescription,
                recipe.resultType, recipe.resultStackable, recipe.resultMaxStack);

            inv.AddItem(resultItem, recipe.resultAmount);

            noticeText = "✅ Đã chế tạo: " + recipe.resultItemName + " x" + recipe.resultAmount;
            noticeTimer = 3f;

            Debug.Log("🛠️ Chế tạo thành công: " + recipe.resultItemName);
        }

        // ═══════════════════════════════════════════════════════
        // UI (OnGUI)
        // ═══════════════════════════════════════════════════════

        void OnGUI()
        {
            // Thông báo craft thành công (luôn hiển thị)
            if (noticeTimer > 0f && !string.IsNullOrEmpty(noticeText))
            {
                if (noticeStyle == null)
                {
                    noticeStyle = new GUIStyle(GUI.skin.label);
                    noticeStyle.fontSize = 22;
                    noticeStyle.fontStyle = FontStyle.Bold;
                    noticeStyle.alignment = TextAnchor.MiddleCenter;
                    noticeStyle.normal.textColor = new Color(0.3f, 1f, 0.3f);
                }

                float alpha = Mathf.Clamp01(noticeTimer);
                Color c = noticeStyle.normal.textColor;
                c.a = alpha;
                noticeStyle.normal.textColor = c;

                GUI.Label(new Rect(0, Screen.height - 80, Screen.width, 40), noticeText, noticeStyle);
            }

            if (!isMenuOpen) return;

            InitStyles();

            // ── BỐ CỤC MENU ──
            float menuWidth = 650;
            float menuHeight = 500;
            float menuX = (Screen.width - menuWidth) / 2;
            float menuY = (Screen.height - menuHeight) / 2;

            // Box nền
            GUI.Box(new Rect(menuX, menuY, menuWidth, menuHeight), "", boxStyle);

            // Tiêu đề
            GUI.Label(new Rect(menuX, menuY + 10, menuWidth, 35), "🛠️ CHẾ TẠO", titleStyle);

            // Nút đóng
            if (GUI.Button(new Rect(menuX + menuWidth - 40, menuY + 10, 30, 30), "X"))
            {
                ToggleMenu();
                return;
            }

            // ── DANH SÁCH CÔNG THỨC (bên trái) ──
            float listWidth = 260;
            float listX = menuX + 15;
            float listY = menuY + 55;
            float listHeight = menuHeight - 70;

            GUI.Box(new Rect(listX, listY, listWidth, listHeight), "");

            scrollPos = GUI.BeginScrollView(
                new Rect(listX, listY, listWidth, listHeight),
                scrollPos,
                new Rect(0, 0, listWidth - 20, recipes.Count * 32 + 100));

            float y = 5;
            string currentCategory = "";

            for (int i = 0; i < recipes.Count; i++)
            {
                // Category header
                if (recipes[i].category != currentCategory)
                {
                    currentCategory = recipes[i].category;
                    GUI.Label(new Rect(5, y, listWidth - 30, 25), "── " + currentCategory + " ──", categoryStyle);
                    y += 25;
                }

                bool canCraft = CanCraft(recipes[i]);
                GUIStyle style = (i == selectedRecipe) ? selectedStyle :
                                 canCraft ? recipeStyle : recipeHoverStyle;

                if (GUI.Button(new Rect(5, y, listWidth - 30, 28), recipes[i].name, style))
                {
                    selectedRecipe = i;
                }
                y += 30;
            }

            GUI.EndScrollView();

            // ── CHI TIẾT CÔNG THỨC (bên phải) ──
            float detailX = menuX + listWidth + 30;
            float detailWidth = menuWidth - listWidth - 45;
            float detailY = listY;

            if (selectedRecipe >= 0 && selectedRecipe < recipes.Count)
            {
                CraftRecipe recipe = recipes[selectedRecipe];

                // Tên
                GUI.Label(new Rect(detailX, detailY, detailWidth, 30), recipe.name, titleStyle);
                detailY += 35;

                // Mô tả
                GUI.Label(new Rect(detailX, detailY, detailWidth, 50), recipe.description, descStyle);
                detailY += 55;

                // Nguyên liệu
                GUI.Label(new Rect(detailX, detailY, detailWidth, 25), "Nguyên liệu cần:", descStyle);
                detailY += 25;

                InventoryManager inv = InventoryManager.Instance;
                for (int i = 0; i < recipe.ingredientIDs.Length; i++)
                {
                    if (recipe.ingredientAmounts[i] <= 0) continue;

                    int have = inv != null ? inv.GetItemCountByID(recipe.ingredientIDs[i]) : 0;
                    int need = recipe.ingredientAmounts[i];
                    bool enough = have >= need;

                    string text = "  • " + recipe.ingredientNames[i] + ": " + have + "/" + need;
                    GUI.Label(new Rect(detailX, detailY, detailWidth, 22),
                        text, enough ? ingredientStyle : ingredientMissStyle);
                    detailY += 22;
                }

                detailY += 15;

                // Kết quả
                GUI.Label(new Rect(detailX, detailY, detailWidth, 25),
                    "➜ " + recipe.resultItemName + " x" + recipe.resultAmount, descStyle);
                detailY += 35;

                // Nút chế tạo
                bool canCraftThis = CanCraft(recipe);
                if (canCraftThis)
                {
                    if (GUI.Button(new Rect(detailX, detailY, detailWidth - 20, 40),
                        "⚒️ CHẾ TẠO", craftBtnStyle))
                    {
                        DoCraft(recipe);
                    }
                }
                else
                {
                    GUI.Button(new Rect(detailX, detailY, detailWidth - 20, 40),
                        "❌ Thiếu nguyên liệu", craftBtnDisabledStyle);
                }
            }
            else
            {
                GUI.Label(new Rect(detailX, detailY + 50, detailWidth, 30),
                    "← Chọn công thức bên trái", descStyle);
            }
        }

        private void InitStyles()
        {
            if (stylesInit) return;
            stylesInit = true;

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.12f, 0.95f));

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 22;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = new Color(0.9f, 0.75f, 0.3f);

            categoryStyle = new GUIStyle(GUI.skin.label);
            categoryStyle.fontSize = 13;
            categoryStyle.fontStyle = FontStyle.Bold;
            categoryStyle.normal.textColor = new Color(0.6f, 0.6f, 0.7f);

            recipeStyle = new GUIStyle(GUI.skin.button);
            recipeStyle.fontSize = 14;
            recipeStyle.alignment = TextAnchor.MiddleLeft;
            recipeStyle.normal.textColor = Color.white;
            recipeStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.25f, 0.8f));

            recipeHoverStyle = new GUIStyle(recipeStyle);
            recipeHoverStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
            recipeHoverStyle.normal.background = MakeTex(2, 2, new Color(0.15f, 0.15f, 0.18f, 0.8f));

            selectedStyle = new GUIStyle(recipeStyle);
            selectedStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
            selectedStyle.normal.background = MakeTex(2, 2, new Color(0.3f, 0.25f, 0.1f, 0.9f));

            descStyle = new GUIStyle(GUI.skin.label);
            descStyle.fontSize = 14;
            descStyle.wordWrap = true;
            descStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            ingredientStyle = new GUIStyle(GUI.skin.label);
            ingredientStyle.fontSize = 14;
            ingredientStyle.normal.textColor = new Color(0.3f, 1f, 0.3f);

            ingredientMissStyle = new GUIStyle(ingredientStyle);
            ingredientMissStyle.normal.textColor = new Color(1f, 0.3f, 0.3f);

            craftBtnStyle = new GUIStyle(GUI.skin.button);
            craftBtnStyle.fontSize = 18;
            craftBtnStyle.fontStyle = FontStyle.Bold;
            craftBtnStyle.normal.textColor = Color.white;
            craftBtnStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.5f, 0.2f, 0.9f));
            craftBtnStyle.hover.background = MakeTex(2, 2, new Color(0.25f, 0.6f, 0.25f, 1f));

            craftBtnDisabledStyle = new GUIStyle(craftBtnStyle);
            craftBtnDisabledStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
            craftBtnDisabledStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.2f, 0.9f));
        }

        private Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = color;
            Texture2D tex = new Texture2D(width, height);
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        private ItemData GetOrCreateItemData(string id, string name, string desc, ItemType type, bool stackable, int maxStack)
        {
            ItemData item = Resources.Load<ItemData>("Items/" + id);
            if (item != null) return item;
            item = Resources.Load<ItemData>(id);
            if (item != null) return item;

            item = ScriptableObject.CreateInstance<ItemData>();
            item.itemID = id;
            item.itemName = name;
            item.description = desc;
            item.itemType = type;
            item.isStackable = stackable;
            item.maxStack = maxStack;
            return item;
        }
    }
}

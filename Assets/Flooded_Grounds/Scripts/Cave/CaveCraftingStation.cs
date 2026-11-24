using UnityEngine;
using System.Collections.Generic;
using HorrorGame.Inventory;
using HorrorGame.Player;

namespace HorrorGame.Cave
{
    /// <summary>
    /// CaveCraftingStation — Lò Rèn Cổ Đại (Ancient Obsidian Forge) trong Hang 5 ("Hang Nham Thạch").
    /// - Tận dụng địa nhiệt nham thạch để chế tạo các vật phẩm tối thượng:
    ///   + Katana Obsidian (Obsidian Katana): Vũ khí cận chiến mạnh nhất non-gun
    ///   + Mũi Tên Lửa (Fire Arrows): Gây cháy và sát thương kéo dài
    ///   + Áo Choàng Kháng Nhiệt (Heat Resistant Mantle): Bảo vệ khỏi dung nham
    /// - Hiệu ứng âm thanh búa đập đe, khói lửa xì nham thạch, và giao diện rèn đồ.
    /// </summary>
    public class CaveCraftingStation : MonoBehaviour
    {
        [System.Serializable]
        public class CaveForgeRecipe
        {
            public string recipeName;
            public string resultItemName;
            public int resultItemCount = 1;
            [TextArea(2, 3)]
            public string description;
            public List<string> requiredIngredients = new List<string>();
        }

        [Header("── Danh Sách Công Thức Rèn Cổ Đại ──")]
        public List<CaveForgeRecipe> forgeRecipes = new List<CaveForgeRecipe>();

        [Header("── Phạm Vi Tương Tác ──")]
        public float interactionRange = 3.5f;
        public KeyCode interactKey = KeyCode.E;

        [Header("── Âm Thanh & Hiệu Ứng ──")]
        public AudioClip hammerAnvilClip;
        public AudioClip lavaHissClip;
        public ParticleSystem forgeSparksParticle;

        private AudioSource audioSource;
        private Transform playerTransform;
        private bool isPlayerInRange = false;
        private bool isCraftingUIOpen = false;
        private string craftStatusMessage = "";
        private float craftStatusTimer = 0f;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.maxDistance = 25f;
            }

            // Khởi tạo các công thức mặc định nếu danh sách trống
            if (forgeRecipes.Count == 0)
            {
                forgeRecipes.Add(new CaveForgeRecipe
                {
                    recipeName = "Katana Obsidian Cổ Đại",
                    resultItemName = "ObsidianKatana",
                    resultItemCount = 1,
                    description = "Kiếm nhật rèn từ đá hắc diện thạch sắc bén tột cùng, chém xuyên giáp quái vật.",
                    requiredIngredients = new List<string> { "Quặng Obsidian x5", "Xương x10", "Dây thừng x2" }
                });

                forgeRecipes.Add(new CaveForgeRecipe
                {
                    recipeName = "Mũi Tên Lửa (x10)",
                    resultItemName = "FireArrow",
                    resultItemCount = 10,
                    description = "Mũi tên tẩm lưu huỳnh nham thạch, phát nổ bốc cháy khi bắn trúng mục tiêu.",
                    requiredIngredients = new List<string> { "Mũi tên thường x10", "Than nham thạch x2", "Vải vụn x2" }
                });

                forgeRecipes.Add(new CaveForgeRecipe
                {
                    recipeName = "Áo Choàng Kháng Nhiệt",
                    resultItemName = "HeatMantle",
                    resultItemCount = 1,
                    description = "Trang bị giúp kháng hoàn toàn sát thương từ dung nham và hơi nóng núi lửa.",
                    requiredIngredients = new List<string> { "Da thú x6", "Xương x8", "Khoáng thạch x4" }
                });
            }
        }

        private void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        private void Update()
        {
            if (playerTransform == null) return;

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            isPlayerInRange = dist <= interactionRange;

            if (isPlayerInRange && Input.GetKeyDown(interactKey))
            {
                isCraftingUIOpen = !isCraftingUIOpen;
            }
            else if (!isPlayerInRange)
            {
                isCraftingUIOpen = false;
            }

            if (craftStatusTimer > 0f)
            {
                craftStatusTimer -= Time.deltaTime;
            }
        }

        /// <summary>
        /// Thực hiện rèn vật phẩm
        /// </summary>
        public void CraftRecipe(int index)
        {
            if (index < 0 || index >= forgeRecipes.Count) return;

            CaveForgeRecipe recipe = forgeRecipes[index];

            // Phát âm thanh và hiệu ứng rèn
            if (audioSource != null)
            {
                if (hammerAnvilClip != null) audioSource.PlayOneShot(hammerAnvilClip);
                if (lavaHissClip != null) audioSource.PlayOneShot(lavaHissClip);
            }

            if (forgeSparksParticle != null)
            {
                forgeSparksParticle.Play();
            }

            craftStatusMessage = "🔥 Đã rèn thành công: " + recipe.recipeName + "!";
            craftStatusTimer = 3.5f;

            Debug.Log(string.Format("<color=orange>[CaveCraftingStation]</color> Rèn thành công {0} x{1}", recipe.recipeName, recipe.resultItemCount));
        }

        private void OnGUI()
        {
            if (isPlayerInRange && !isCraftingUIOpen)
            {
                GUIStyle promptStyle = new GUIStyle(GUI.skin.label);
                promptStyle.alignment = TextAnchor.MiddleCenter;
                promptStyle.fontSize = 18;
                promptStyle.normal.textColor = Color.yellow;

                GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 + 60, 400, 30),
                    "[ " + interactKey + " ] Dùng Lò Rèn Cổ Đại (Obsidian Forge)", promptStyle);
            }

            if (isCraftingUIOpen)
            {
                float panelW = 480f;
                float panelH = 360f;
                float px = (Screen.width - panelW) / 2;
                float py = (Screen.height - panelH) / 2;

                // Nền đen bán trong suốt
                GUI.color = new Color(0.1f, 0.05f, 0.05f, 0.95f);
                GUI.DrawTexture(new Rect(px, py, panelW, panelH), Texture2D.whiteTexture);
                GUI.color = Color.white;

                // Tiêu đề
                GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.fontSize = 20;
                titleStyle.fontStyle = FontStyle.Bold;
                titleStyle.alignment = TextAnchor.UpperCenter;
                titleStyle.normal.textColor = new Color(1f, 0.6f, 0.2f);
                GUI.Label(new Rect(px, py + 15, panelW, 30), "⚒️ LÒ RÈN CỔ ĐẠI (ANCIENT FORGE)", titleStyle);

                // Danh sách công thức
                float currentY = py + 60;
                for (int i = 0; i < forgeRecipes.Count; i++)
                {
                    CaveForgeRecipe recipe = forgeRecipes[i];

                    GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
                    nameStyle.fontSize = 15;
                    nameStyle.fontStyle = FontStyle.Bold;
                    nameStyle.normal.textColor = Color.white;

                    GUI.Label(new Rect(px + 20, currentY, 300, 22), recipe.recipeName, nameStyle);

                    GUIStyle descStyle = new GUIStyle(GUI.skin.label);
                    descStyle.fontSize = 12;
                    descStyle.normal.textColor = Color.gray;
                    GUI.Label(new Rect(px + 20, currentY + 22, 320, 35), recipe.description, descStyle);

                    if (GUI.Button(new Rect(px + 360, currentY + 10, 95, 35), "🔥 RÈN"))
                    {
                        CraftRecipe(i);
                    }

                    currentY += 75;
                }

                // Thông báo trạng thái rèn
                if (craftStatusTimer > 0f)
                {
                    GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
                    statusStyle.alignment = TextAnchor.MiddleCenter;
                    statusStyle.fontSize = 14;
                    statusStyle.normal.textColor = Color.green;
                    GUI.Label(new Rect(px, py + panelH - 45, panelW, 25), craftStatusMessage, statusStyle);
                }

                if (GUI.Button(new Rect(px + panelW - 35, py + 10, 25, 25), "X"))
                {
                    isCraftingUIOpen = false;
                }
            }
        }
    }
}

using UnityEngine;
using HorrorGame.Inventory;
using HorrorGame.Player;

namespace HorrorGame.Environment
{
    /// <summary>
    /// Thi thể hành khách xấu số sau vụ rơi máy bay phong cách The Forest:
    /// - Nằm rải rác quanh xác máy bay trong các tư thế văng khỏi ghế, kẹt trong ghế vỡ, nằm bên vali.
    /// - Người chơi có thể đến gần ấn [E] để lục soát lấy nhu yếu phẩm cứu sinh (băng gạc, socola, nước ngọt, thuốc men).
    /// - Nhìn thấy và lục soát thi thể sẽ gây sốc nhẹ làm tụt Tinh Thần (Sanity).
    /// </summary>
    public class PassengerCorpse : MonoBehaviour
    {
        [Header("── Thông Tin Thi Thể ──")]
        public string passengerName = "Hành khách xấu số";
        public bool isSearched = false;
        public float interactDistance = 2.8f;

        [Header("── Loại Trang Phục & Tư Thế ──")]
        public int corpseType = 0; // 0: Kẹt trên ghế máy bay, 1: Nằm văng trên cỏ, 2: Tiếp viên hàng không

        private Transform playerTransform;
        private PlayerStats playerStats;
        private bool isPlayerNearby = false;

        private GUIStyle promptStyle;
        private GUIStyle promptShadowStyle;

        void Start()
        {
            FindPlayer();
            BuildProceduralCorpseVisuals();
        }

        private void FindPlayer()
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                playerTransform = p.transform;
                playerStats = p.GetComponent<PlayerStats>();
            }
            else if (Camera.main != null)
            {
                playerTransform = Camera.main.transform;
                playerStats = playerTransform.GetComponentInParent<PlayerStats>();
            }
        }

        void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            isPlayerNearby = (dist <= interactDistance);

            // Bấm E để lục soát thi thể
            if (isPlayerNearby && !isSearched && Input.GetKeyDown(KeyCode.E))
            {
                SearchCorpse();
            }
        }

        private void SearchCorpse()
        {
            if (isSearched) return;
            isSearched = true;

            // 1. Gây sốc tinh thần chuẩn The Forest
            if (playerStats == null) FindPlayer();
            if (playerStats != null)
            {
                playerStats.DrainSanity(4.0f);
            }

            // 2. Thưởng vật phẩm cứu sinh ngẫu nhiên từ hành lý hành khách
            InventoryManager inv = InventoryManager.Instance;
            if (inv == null) inv = FindObjectOfType<InventoryManager>();
            if (inv != null)
            {
                GiveLootItem(inv);
            }
            else
            {
                Debug.LogError("[PassengerCorpse] Không tìm thấy InventoryManager trong Scene!");
            }

            Debug.Log("[PassengerCorpse] Đã lục soát thi thể: " + passengerName);
        }

        private void GiveLootItem(InventoryManager inv)
        {
            int roll = Random.Range(0, 4);
            ItemData lootItem = null;
            int amount = 1;

            if (roll == 0)
            {
                // Băng cứu thương / Medkit chính thức (có icon)
                lootItem = Resources.Load<ItemData>("Items/medkit_01");
                if (lootItem == null) lootItem = Resources.Load<ItemData>("Bandaid");
                amount = 1;
            }
            else if (roll == 1)
            {
                // Hộp lương thực dự trữ chính thức (có icon)
                lootItem = Resources.Load<ItemData>("Items/food_ration_01");
                amount = 1;
            }
            else if (roll == 2)
            {
                // Hộp đạn súng chính thức (có icon)
                lootItem = Resources.Load<ItemData>("Items/ammo_box_01");
                amount = 15;
            }
            else
            {
                // Băng gạc dán vết thương
                lootItem = Resources.Load<ItemData>("Bandaid");
                if (lootItem == null) lootItem = Resources.Load<ItemData>("Items/medkit_01");
                amount = 2;
            }

            // Dự phòng nếu máy không load được asset từ Resources
            if (lootItem == null)
            {
                lootItem = ScriptableObject.CreateInstance<ItemData>();
                lootItem.itemID = "medkit_01";
                lootItem.itemName = "Băng cứu thương";
                lootItem.description = "Hộp sơ cứu nhặt từ hành lý hành khách máy bay. (+35 HP, loại bỏ ký sinh trùng)";
                lootItem.itemType = ItemType.Consumable;
                lootItem.isStackable = true;
                lootItem.maxStack = 5;

                // Mượn icon từ item có sẵn nếu có
                ItemData gun = Resources.Load<ItemData>("GunItem");
                if (gun != null && gun.icon != null) lootItem.icon = gun.icon;
            }

            bool added = inv.AddItem(lootItem, amount);
            Debug.Log(string.Format("[PassengerCorpse] Đã thêm vào Inventory: {0} x{1} (Thành công: {2})", lootItem.itemName, amount, added));

            // Cập nhật ngay UI nếu đang mở
            HorrorGame.Inventory.UI.InventoryUI ui = FindObjectOfType<HorrorGame.Inventory.UI.InventoryUI>();
            if (ui != null) ui.UpdateUI();

            if (playerStats != null)
            {
                playerStats.ShowStatusNotice("📦 Nhặt được: " + lootItem.itemName + " x" + amount + "!");
            }
        }

        /// <summary>
        /// Tạo mô hình 3D thi thể và ghế máy bay procedural chi tiết
        /// </summary>
        private void BuildProceduralCorpseVisuals()
        {
            // Kiểm tra nếu đã có mesh con thì không tạo đè
            if (transform.childCount > 0) return;

            Shader standardShader = Shader.Find("Standard");
            if (standardShader == null) standardShader = Shader.Find("Diffuse");

            // 1. Ghế Máy Bay Vỡ Nát (Airplane Passenger Seat)
            GameObject seatObj = new GameObject("AirplaneSeat");
            seatObj.transform.SetParent(this.transform, false);

            // Nệm ngồi
            GameObject seatBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seatBase.name = "SeatCushion";
            seatBase.transform.SetParent(seatObj.transform, false);
            seatBase.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            seatBase.transform.localScale = new Vector3(0.65f, 0.15f, 0.6f);
            Destroy(seatBase.GetComponent<Collider>());

            // Lưng ghế nghiêng
            GameObject seatBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seatBack.name = "SeatBack";
            seatBack.transform.SetParent(seatObj.transform, false);
            seatBack.transform.localPosition = new Vector3(0f, 0.65f, -0.25f);
            seatBack.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            seatBack.transform.localScale = new Vector3(0.62f, 0.75f, 0.12f);
            Destroy(seatBack.GetComponent<Collider>());

            // Màu ghế vải xanh thẫm hàng không
            if (standardShader != null)
            {
                Material seatMat = new Material(standardShader);
                seatMat.color = new Color(0.18f, 0.28f, 0.42f); // Xanh navy đậm
                seatBase.GetComponent<Renderer>().material = seatMat;
                seatBack.GetComponent<Renderer>().material = seatMat;
            }

            // 2. Thi Thể Hành Khách (Passenger Body)
            GameObject bodyObj = new GameObject("PassengerBody");
            bodyObj.transform.SetParent(this.transform, false);

            // Thân người (Torso)
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Torso";
            torso.transform.SetParent(bodyObj.transform, false);
            torso.transform.localPosition = new Vector3(0f, 0.55f, -0.1f);
            torso.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            torso.transform.localScale = new Vector3(0.38f, 0.45f, 0.28f);
            Destroy(torso.GetComponent<Collider>());

            // Đầu gục xuống
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(bodyObj.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.95f, 0.05f);
            head.transform.localScale = new Vector3(0.24f, 0.27f, 0.25f);
            Destroy(head.GetComponent<Collider>());

            // Chân (Legs)
            GameObject legs = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legs.name = "Legs";
            legs.transform.SetParent(bodyObj.transform, false);
            legs.transform.localPosition = new Vector3(0f, 0.2f, 0.35f);
            legs.transform.localScale = new Vector3(0.35f, 0.2f, 0.55f);
            Destroy(legs.GetComponent<Collider>());

            // Quần áo hành khách (Áo sơ mi trắng / Vest xám)
            if (standardShader != null)
            {
                Material clothesMat = new Material(standardShader);
                clothesMat.color = (corpseType == 0) ? new Color(0.85f, 0.85f, 0.85f) : (corpseType == 1 ? new Color(0.2f, 0.2f, 0.25f) : new Color(0.7f, 0.15f, 0.15f));
                torso.GetComponent<Renderer>().material = clothesMat;

                Material skinMat = new Material(standardShader);
                skinMat.color = new Color(0.78f, 0.65f, 0.55f); // Da tái nhợt
                head.GetComponent<Renderer>().material = skinMat;

                Material pantsMat = new Material(standardShader);
                pantsMat.color = new Color(0.12f, 0.12f, 0.15f);
                legs.GetComponent<Renderer>().material = pantsMat;
            }

            // 3. Vali Hành Lý Rơi Bên Cạnh (Scattered Suitcase)
            GameObject suitcase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            suitcase.name = "LuggageSuitcase";
            suitcase.transform.SetParent(this.transform, false);
            suitcase.transform.localPosition = new Vector3(0.65f, 0.15f, 0.2f);
            suitcase.transform.localRotation = Quaternion.Euler(0f, Random.Range(20f, 65f), 0f);
            suitcase.transform.localScale = new Vector3(0.55f, 0.25f, 0.4f);
            Destroy(suitcase.GetComponent<Collider>());

            if (standardShader != null)
            {
                Material caseMat = new Material(standardShader);
                caseMat.color = new Color(0.32f, 0.20f, 0.12f); // Vali da nâu
                suitcase.GetComponent<Renderer>().material = caseMat;
            }

            // Thêm CapsuleCollider để click / raycast
            CapsuleCollider col = gameObject.GetComponent<CapsuleCollider>();
            if (col == null) col = gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.radius = 0.55f;
            col.height = 1.2f;
        }

        void OnGUI()
        {
            if (!isPlayerNearby || playerStats == null || playerStats.isDead) return;

            if (promptStyle == null)
            {
                promptStyle = new GUIStyle();
                promptStyle.fontSize = 16;
                promptStyle.fontStyle = FontStyle.Bold;
                promptStyle.alignment = TextAnchor.MiddleCenter;
                promptStyle.normal.textColor = Color.white;

                promptShadowStyle = new GUIStyle(promptStyle);
                promptShadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.9f);
            }

            string text = isSearched 
                ? "💀 " + passengerName + " (Đã lục soát)" 
                : "💀 Nhấn [E] Lục soát thi thể " + passengerName;

            promptStyle.normal.textColor = isSearched ? new Color(0.7f, 0.7f, 0.7f, 0.8f) : new Color(1f, 0.85f, 0.3f, 1f);

            float w = 450f;
            float h = 30f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height * 0.62f;

            GUI.Label(new Rect(x + 2, y + 2, w, h), text, promptShadowStyle);
            GUI.Label(new Rect(x, y, w, h), text, promptStyle);
        }
    }
}

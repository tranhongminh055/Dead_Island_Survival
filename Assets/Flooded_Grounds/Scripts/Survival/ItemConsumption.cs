using UnityEngine;
using HorrorGame.Inventory;
using HorrorGame.Player;

namespace HorrorGame.Survival
{
    /// <summary>
    /// Xử lý sử dụng item tiêu hao trong Inventory (ăn, uống, dùng thuốc).
    /// Gắn lên Player (cùng PlayerStats).
    /// 
    /// Khi mở Inventory (phím I), click chuột phải vào item để sử dụng.
    /// Hoặc dùng phím tắt nhanh: nhấn F để dùng item đang chọn.
    /// 
    /// Tự động nhận diện item theo itemID và áp dụng hiệu ứng:
    /// - raw_meat: Ăn thịt sống (+10 no, -5 HP, -10 tinh thần)
    /// - cooked_meat: Ăn thịt chín (+30 no, +10 HP)
    /// - burnt_meat: Ăn thịt cháy (+10 no, -5 HP)
    /// - dried_meat: Ăn thịt khô (+40 no)
    /// - bandage: Hồi 25 HP
    /// - herbal_medicine: Hồi 50 HP
    /// </summary>
    public class ItemConsumption : MonoBehaviour
    {
        private PlayerStats playerStats;
        private GUIStyle noticeStyle;
        private string noticeText = "";
        private float noticeTimer = 0f;

        void Start()
        {
            playerStats = GetComponent<PlayerStats>();
        }

        void Update()
        {
            if (noticeTimer > 0f) noticeTimer -= Time.deltaTime;
        }

        /// <summary>
        /// Sử dụng item theo itemID. Trả về true nếu thành công.
        /// Gọi từ Inventory UI khi click chuột phải.
        /// </summary>
        public bool UseItem(string itemID)
        {
            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
                if (playerStats == null) return false;
            }

            InventoryManager inv = InventoryManager.Instance;
            if (inv == null) return false;

            // Kiểm tra có item không
            if (inv.GetItemCountByID(itemID) <= 0) return false;

            bool consumed = false;
            string message = "";

            ZombieParasiteSystem parasiteSys = playerStats.GetComponent<ZombieParasiteSystem>();
            PlayerFractureSystem fractureSys = playerStats.GetComponent<PlayerFractureSystem>();
            if (fractureSys == null) fractureSys = PlayerFractureSystem.Instance;

            switch (itemID)
            {
                case "raw_meat":
                    playerStats.Eat(12f);
                    playerStats.TakeDamage(5f);
                    playerStats.DrainSanity(15f);
                    if (parasiteSys != null) parasiteSys.TryInfect(0.40f, "Ăn thịt sống nhiễm khuẩn");
                    message = "🥩 Ăn thịt sống tanh tưởi... (+12 No, -5 HP, -15 Tinh thần, nguy cơ nhiễm sán!)";
                    consumed = true;
                    break;

                case "cooked_meat":
                    playerStats.Eat(35f);
                    playerStats.Heal(15f);
                    playerStats.RestoreSanity(10f);
                    message = "🍖 Ăn thịt nướng thơm ngon! (+35 No, +15 HP, +10 Tinh thần)";
                    consumed = true;
                    break;

                case "burnt_meat":
                    playerStats.Eat(10f);
                    playerStats.TakeDamage(5f);
                    playerStats.DrainSanity(5f);
                    message = "💀 Ăn thịt cháy khét... (+10 No, -5 HP, -5 Tinh thần)";
                    consumed = true;
                    break;

                case "dried_meat":
                    playerStats.Eat(45f);
                    playerStats.Heal(10f);
                    playerStats.RestoreSanity(15f);
                    message = "🍖 Ăn thịt khô dinh dưỡng! (+45 No, +10 HP, +15 Tinh thần)";
                    consumed = true;
                    break;

                case "bandage":
                case "bandaid_01":
                    playerStats.Heal(30f);
                    if (parasiteSys != null) parasiteSys.RemoveParasites(1);
                    if (fractureSys != null && fractureSys.isFractured)
                    {
                        fractureSys.CureFracture();
                        message = "🩹 Dùng băng gạc nẹp cố định xương gãy! Chân đã lành lặn, chạy bình thường! (+30 HP)";
                    }
                    else
                    {
                        message = "💊 Dùng băng gạc băng bó vết thương & loại bỏ ký sinh trùng! (+30 HP)";
                    }
                    consumed = true;
                    break;

                case "medkit":
                case "medkit_01":
                case "firstaid":
                    playerStats.Heal(50f);
                    playerStats.RestoreSanity(15f);
                    if (parasiteSys != null) parasiteSys.RemoveParasites(2);
                    if (fractureSys != null && fractureSys.isFractured)
                    {
                        fractureSys.CureFracture();
                        message = "🧰 Dùng hộp sơ cứu điều trị gãy xương toàn diện! Chân đã bình phục! (+50 HP, +15 Tinh thần)";
                    }
                    else
                    {
                        message = "🧰 Dùng hộp sơ cứu khẩn cấp! (+50 HP, +15 Tinh thần, diệt ký sinh trùng)";
                    }
                    consumed = true;
                    break;

                case "food_ration_01":
                    playerStats.Eat(40f);
                    playerStats.RestoreSanity(12f);
                    if (playerStats.currentStamina < playerStats.maxStamina) playerStats.currentStamina += 25f;
                    message = "🥫 Ăn hộp lương thực dự trữ! (+40 No, +25 Thể lực, +12 Tinh thần)";
                    consumed = true;
                    break;

                case "herbal_medicine":
                    playerStats.Heal(50f);
                    playerStats.RestoreSanity(25f);
                    if (parasiteSys != null) parasiteSys.CureAll();
                    if (fractureSys != null && fractureSys.isFractured)
                    {
                        fractureSys.TreatInShelter(120f); // Giảm mạnh 2 phút gãy xương
                        message = "🧴 Uống thuốc thảo dược quý! Giảm đau nhức gãy xương & diệt sạch Ký sinh trùng (+50 HP, +25 Tinh thần)";
                    }
                    else
                    {
                        message = "🧴 Uống thuốc thảo dược quý! Diệt sạch 100% Ký sinh trùng (+50 HP, +25 Tinh thần)";
                    }
                    consumed = true;
                    break;

                case "bottle_water":
                case "soda":
                    playerStats.Drink(35f);
                    playerStats.RestoreSanity(10f);
                    message = "🥤 Uống nước ngọt cứu nạn! (+35 Nước, +10 Tinh thần)";
                    consumed = true;
                    break;

                case "chocolate":
                case "snack":
                    playerStats.Eat(25f);
                    playerStats.RestoreSanity(15f);
                    message = "🍫 Ăn thanh socola ngọt ngào! (+25 No, +15 Tinh thần)";
                    consumed = true;
                    break;

                case "canned_food":
                    playerStats.Eat(45f);
                    playerStats.RestoreSanity(10f);
                    message = "🥫 Ăn đồ hộp khẩn cấp! (+45 No, +10 Tinh thần)";
                    consumed = true;
                    break;

                default:
                    Debug.Log("[ItemConsumption] Item " + itemID + " không thể sử dụng.");
                    break;
            }

            if (consumed)
            {
                inv.RemoveItemByID(itemID, 1);
                noticeText = message;
                noticeTimer = 3f;
                Debug.Log(message);
            }

            return consumed;
        }

        void OnGUI()
        {
            if (noticeTimer <= 0f || string.IsNullOrEmpty(noticeText)) return;

            if (noticeStyle == null)
            {
                noticeStyle = new GUIStyle(GUI.skin.label);
                noticeStyle.fontSize = 20;
                noticeStyle.fontStyle = FontStyle.Bold;
                noticeStyle.alignment = TextAnchor.MiddleCenter;
                noticeStyle.normal.textColor = Color.white;
            }

            float alpha = Mathf.Clamp01(noticeTimer);
            Color c = noticeStyle.normal.textColor;
            c.a = alpha;
            noticeStyle.normal.textColor = c;

            GUI.Label(new Rect(0, Screen.height - 120, Screen.width, 40), noticeText, noticeStyle);
        }
    }
}

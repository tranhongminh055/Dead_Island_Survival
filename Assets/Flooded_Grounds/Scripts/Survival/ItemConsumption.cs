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

            switch (itemID)
            {
                case "raw_meat":
                    playerStats.Eat(10f);
                    playerStats.TakeDamage(5f);
                    playerStats.DrainSanity(10f);
                    message = "🥩 Ăn thịt sống... (+10 No, -5 HP, -10 Tinh thần)";
                    consumed = true;
                    break;

                case "cooked_meat":
                    playerStats.Eat(30f);
                    playerStats.Heal(10f);
                    message = "🍖 Ăn thịt chín! (+30 No, +10 HP)";
                    consumed = true;
                    break;

                case "burnt_meat":
                    playerStats.Eat(10f);
                    playerStats.TakeDamage(5f);
                    message = "💀 Ăn thịt cháy... (+10 No, -5 HP)";
                    consumed = true;
                    break;

                case "dried_meat":
                    playerStats.Eat(40f);
                    message = "🍖 Ăn thịt khô! (+40 No)";
                    consumed = true;
                    break;

                case "bandage":
                    playerStats.Heal(25f);
                    message = "💊 Dùng băng gạc! (+25 HP)";
                    consumed = true;
                    break;

                case "herbal_medicine":
                    playerStats.Heal(50f);
                    playerStats.RestoreSanity(20f);
                    message = "🧴 Dùng thuốc thảo dược! (+50 HP, +20 Tinh thần)";
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

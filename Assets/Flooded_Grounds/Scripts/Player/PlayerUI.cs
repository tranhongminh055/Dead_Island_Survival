using UnityEngine;
using UnityEngine.UI;

namespace HorrorGame.Player
{
    public class PlayerUI : MonoBehaviour
    {
        public PlayerStats playerStats;

        [Header("UI Sliders (Kéo thả từ Canvas vào đây)")]
        public Slider healthSlider;   // Thanh Máu
        public Slider staminaSlider;  // Thanh Thể lực
        public Slider hungerSlider;   // Thanh Thức ăn
        public Slider thirstSlider;   // Thanh Nước uống
        public Slider sanitySlider;   // Thanh Tinh thần (Ảo giác)

        [Header("UI Images (Dùng hình ảnh đổ vơi như Dạ Dày)")]
        public Image hungerImage;     // Hình dạ dày
        public Image thirstImage;     // Hình giọt nước
        public Image sanityImage;     // Hình bộ não (nếu có)

        private void Awake()
        {
            if (PlayerStats.Instance != null)
            {
                playerStats = PlayerStats.Instance;
            }
        }

        private void Start()
        {
            // Tự động gắn kết với PlayerStats.Instance duy nhất của nhân vật
            if (PlayerStats.Instance != null)
            {
                playerStats = PlayerStats.Instance;
            }
            else if (playerStats == null)
            {
                playerStats = FindObjectOfType<PlayerStats>();
            }

            // Thiết lập giá trị tối đa cho các thanh UI (bằng 100)
            if (playerStats != null)
            {
                if (healthSlider != null) healthSlider.maxValue = playerStats.maxHealth;
                if (staminaSlider != null) staminaSlider.maxValue = playerStats.maxStamina;
                if (hungerSlider != null) hungerSlider.maxValue = playerStats.maxHunger;
                if (thirstSlider != null) thirstSlider.maxValue = playerStats.maxThirst;
                if (sanitySlider != null) sanitySlider.maxValue = playerStats.maxSanity;
            }
        }

        private void Update()
        {
            // Đảm bảo luôn trỏ đúng vào Instance duy nhất của PlayerStats
            if (PlayerStats.Instance != null && playerStats != PlayerStats.Instance)
            {
                playerStats = PlayerStats.Instance;
            }
            else if (playerStats == null)
            {
                playerStats = PlayerStats.Instance != null ? PlayerStats.Instance : FindObjectOfType<PlayerStats>();
                if (playerStats != null)
                {
                    if (healthSlider != null) healthSlider.maxValue = playerStats.maxHealth;
                    if (staminaSlider != null) staminaSlider.maxValue = playerStats.maxStamina;
                    if (hungerSlider != null) hungerSlider.maxValue = playerStats.maxHunger;
                    if (thirstSlider != null) thirstSlider.maxValue = playerStats.maxThirst;
                    if (sanitySlider != null) sanitySlider.maxValue = playerStats.maxSanity;
                }
                else return;
            }

            // Cập nhật độ dài và trần tối đa của các thanh UI liên tục theo thời gian thực (hiển thị Fatigue)
            if (healthSlider != null) { healthSlider.maxValue = playerStats.maxHealth; healthSlider.value = playerStats.currentHealth; }
            if (staminaSlider != null) { staminaSlider.maxValue = playerStats.maxStamina; staminaSlider.value = playerStats.currentStamina; }
            if (hungerSlider != null) { hungerSlider.maxValue = playerStats.maxHunger; hungerSlider.value = playerStats.currentHunger; }
            if (thirstSlider != null) { thirstSlider.maxValue = playerStats.maxThirst; thirstSlider.value = playerStats.currentThirst; }
            if (sanitySlider != null) { sanitySlider.maxValue = playerStats.maxSanity; sanitySlider.value = playerStats.currentSanity; }

            // Cập nhật cho dạng Hình ảnh (Dạ dày đầy/vơi) - Tính theo tỷ lệ 0 đến 1
            if (hungerImage != null) hungerImage.fillAmount = playerStats.currentHunger / playerStats.maxHunger;
            if (thirstImage != null) thirstImage.fillAmount = playerStats.currentThirst / playerStats.maxThirst;
            if (sanityImage != null) sanityImage.fillAmount = playerStats.currentSanity / playerStats.maxSanity;
        }
    }
}

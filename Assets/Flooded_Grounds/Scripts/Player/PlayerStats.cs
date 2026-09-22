using UnityEngine;
using System; // Required for Action

namespace HorrorGame.Player
{
    public class PlayerStats : MonoBehaviour
    {
        [Header("Health")]
        public float maxHealth = 100f;
        public float currentHealth;

        [Header("Stamina")]
        public float maxStamina = 100f;
        public float currentStamina;
        public float staminaDrainRate = 15f; // Tốc độ tụt khi chạy
        public float staminaRegenRate = 10f; // Tốc độ hồi khi đi bộ
        private bool isExhaustedState = false; // Trạng thái kiệt sức không thể chạy

        [Header("Hunger & Thirst")]
        public float maxHunger = 100f;
        public float currentHunger;
        public float hungerDrainRate = 0.5f; // Tăng tốc độ đói để test nhanh

        public float maxThirst = 100f;
        public float currentThirst;
        public float thirstDrainRate = 1f; // Tăng tốc độ khát để test nhanh


        [Header("Sanity (Tinh thần)")]
        public float maxSanity = 100f;
        public float currentSanity;
        public float baseSanityDrainRate = 0.3f; // Tốc độ tự tụt tinh thần theo thời gian
        public float penaltySanityDrainRate = 2f; // Tốc độ tụt hoảng loạn khi đói/khát

        [Header("Fatigue (Mệt mỏi - Giống Green Hell)")]
        public float fatigueDrainRate = 0.2f; // Làm giảm Max Stamina theo thời gian (Cần ngủ để hồi)

        public bool isDead = false; // Đánh dấu đã chết
        public bool isInvincible = false; // Bất tử (dùng trong Cutscene mở đầu)
        public Action OnPlayerDeath; // Sự kiện khi người chơi chết

        private void Start()
        {
            // Luôn đảm bảo thời gian chạy mượt mà ở tốc độ bình thường
            Time.timeScale = 1.0f;
            isDead = false;

            // Khởi tạo chỉ số ban đầu
            currentHealth = maxHealth;
            currentStamina = maxStamina;
            currentHunger = maxHunger;
            currentThirst = maxThirst;
            currentSanity = maxSanity;
        }

        private void Update()
        {
            if (isDead) return; // Nếu chết rồi thì ngừng hoạt động cơ thể
            if (!isInvincible)
            {
                HandleSurvivalStats();
            }
        }

        private void HandleSurvivalStats()
        {
            // 1. Tụt đói khát theo thời gian thực
            if (currentHunger > 0) currentHunger -= hungerDrainRate * Time.deltaTime;
            if (currentThirst > 0) currentThirst -= thirstDrainRate * Time.deltaTime;

            // Ép giới hạn không cho âm
            if (currentHunger < 0) currentHunger = 0;
            if (currentThirst < 0) currentThirst = 0;

            // 2. GREEN HELL CƠ CHẾ: Tụt Tinh Thần (Sanity) tự nhiên theo thời gian
            if (currentSanity > 0) currentSanity -= baseSanityDrainRate * Time.deltaTime;

            // QUY TRÌNH ÉP TỤT SANITY NẶNG HƠN (Khi gặp tình trạng tồi tệ)
            bool isStarving = currentHunger <= 0; // Quá đói
            bool isDehydrated = currentThirst <= 0; // Quá khát
            bool isInPain = currentHealth <= 5f; // Hấp hối

            if (isStarving || isDehydrated || isInPain)
            {
                DrainSanity(penaltySanityDrainRate * Time.deltaTime);
            }

            // Nếu Tinh thần suy sụp hoàn toàn, cơ thể kiệt quệ và tụt máu
            if (currentSanity <= 0)
            {
                currentSanity = 0;
                TakeDamage(1f * Time.deltaTime); // Mất 1 máu mỗi giây
            }

            // 3. GREEN HELL CƠ CHẾ: Mệt mỏi (Fatigue)
            // Giảm giới hạn tối đa của Thể lực theo thời gian (Chỉ hồi lại khi ngủ)
            if (maxStamina > 10f) // Giữ lại ít nhất 10 thể lực để đi lết
            {
                maxStamina -= fatigueDrainRate * Time.deltaTime;
                if (currentStamina > maxStamina) currentStamina = maxStamina;
            }
        }

        // HÀM XỬ LÝ SANITY (TINH THẦN)
        public void DrainSanity(float amount)
        {
            if (currentSanity > 0)
            {
                currentSanity -= amount;
                if (currentSanity < 0) currentSanity = 0;
            }
        }

        public void RestoreSanity(float amount)
        {
            currentSanity += amount;
            if (currentSanity > maxSanity) currentSanity = maxSanity;
        }

        // HÀM XỬ LÝ THỂ LỰC (Gọi từ PlayerController)
        public void DrainStamina()
        {
            if (currentStamina > 0)
            {
                currentStamina -= staminaDrainRate * Time.deltaTime;
                if (currentStamina <= 0)
                {
                    currentStamina = 0;
                    isExhaustedState = true; // Cạn kiệt hoàn toàn, cấm chạy
                }
            }
        }

        public void RegenStamina()
        {
            if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                // Nếu hồi được hơn 20% thì cho phép chạy lại
                if (currentStamina >= 20f) 
                {
                    isExhaustedState = false;
                }
            }
        }

        public bool HasStamina()
        {
            return !isExhaustedState && currentStamina > 0; 
        }

        // HÀM XỬ LÝ SÁT THƯƠNG / HỒI MÁU
        public void TakeDamage(float amount)
        {
            if (isDead || isInvincible) return; // Chết rồi hoặc đang bất tử (cutscene) không nhận sát thương

            Debug.Log("Bị đánh! Máu bị trừ: " + amount + ". Máu hiện tại: " + currentHealth);
            currentHealth -= amount;
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                Die();
            }
        }

        public void Heal(float amount)
        {
            currentHealth += amount;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
        }

        public void Eat(float amount)
        {
            currentHunger += amount;
            if (currentHunger > maxHunger) currentHunger = maxHunger;
        }

        public void Drink(float amount)
        {
            currentThirst += amount;
            if (currentThirst > maxThirst) currentThirst = maxThirst;
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;
            Debug.Log("Player Died!");
            
            // Khóa điều khiển di chuyển của nhân vật thay vì đóng băng timeScale (để tránh lỗi FPS nhảy hàng triệu và treo game)
            PlayerController pc = GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false;

            if (OnPlayerDeath != null)
            {
                OnPlayerDeath.Invoke();
            }
        }
    }
}

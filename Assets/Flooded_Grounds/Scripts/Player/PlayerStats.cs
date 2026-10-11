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

        [Header("Hunger & Thirst (Chuẩn The Forest & Green Hell)")]
        public float maxHunger = 100f;
        public float currentHunger;
        public float baseHungerDrainRate = 0.035f; // Tiêu hao cơ bản khi đứng yên (~48 phút cho 100 điểm)

        public float maxThirst = 100f;
        public float currentThirst;
        public float baseThirstDrainRate = 0.055f; // Tiêu hao cơ bản khi đứng yên (~30 phút cho 100 điểm)

        [Header("Sanity (Tinh thần - Green Hell & The Forest)")]
        public float maxSanity = 100f;
        public float currentSanity;
        public float campfireSanityRegenRate = 0.42f; // Hơi ấm lửa trại hồi phục tinh thần (+0.42/s)

        [Header("Health Regeneration (Hồi Máu Tự Nhiên)")]
        public float naturalHealRate = 0.25f; // Tự hồi máu khi no đủ (>70%) và tinh thần ổn định

        [Header("Fatigue (Mệt mỏi theo thời gian)")]
        public float fatigueDrainRate = 0.02f; // Trần thể lực giảm từ từ (~80 phút), ngủ để hồi đầy

        // Trạng thái sinh tồn (Dùng public field để tương thích C# 4.0 của Unity)
        public bool isNearFire = false;
        public bool isHealingNaturally = false;
        public bool isInsane = false;

        public bool isDead = false; // Đánh dấu đã chết
        public bool isInvincible = false; // Bất tử (dùng trong Cutscene mở đầu)
        public Action OnPlayerDeath; // Sự kiện khi người chơi chết

        // References
        private Environment.Weather.PlayerWetness playerWetness;

        // HUD thông báo trạng thái
        private string statusNoticeText = "";
        private float statusNoticeTimer = 0f;
        private float lastFireNoticeTime = -999f;
        private float lastHungerNoticeTime = -999f;
        private float lastThirstNoticeTime = -999f;
        private float lastColdNoticeTime = -999f;
        private float lastHealthPanicNoticeTime = -999f;
        private float lastHealNoticeTime = -999f;
        private GUIStyle statusStyle;
        private GUIStyle statusShadowStyle;

        private void Start()
        {
            // Luôn đảm bảo thời gian chạy mượt mà ở tốc độ bình thường
            Time.timeScale = 1.0f;
            isDead = false;

            // Khởi tạo chỉ số ban đầu đầy đủ 100%
            currentHealth = maxHealth;
            currentStamina = maxStamina;
            currentHunger = maxHunger;
            currentThirst = maxThirst;
            currentSanity = maxSanity;

            // Tự động tìm các component hỗ trợ
            playerWetness = GetComponent<Environment.Weather.PlayerWetness>();

            // Đảm bảo có SanityEffects để xử lý ảo giác chuẩn Green Hell
            if (GetComponent<Effects.SanityEffects>() == null)
            {
                gameObject.AddComponent<Effects.SanityEffects>();
            }
        }

        private void Update()
        {
            if (isDead) return; // Nếu chết rồi thì ngừng hoạt động cơ thể
            if (!isInvincible)
            {
                HandleSurvivalStats();
            }

            if (statusNoticeTimer > 0f) statusNoticeTimer -= Time.deltaTime;
        }

        private void HandleSurvivalStats()
        {
            // ═══════════════════════════════════════════════════════════════
            // 1. TIÊU HAO ĐÓI & KHÁT THEO CƠN VẬN ĐỘNG (PHYSICAL EXERTION)
            // Chuẩn The Forest & Green Hell: Đứng yên tiêu hao rất chậm,
            // đi bộ vừa phải, chạy nước rút đốt calo và mất mồ hôi cực nhanh!
            // ═══════════════════════════════════════════════════════════════
            PlayerController pc = GetComponent<PlayerController>();
            float exertionMultiplier = 1.0f; // Đứng yên nghỉ ngơi (tốc độ tối thiểu)
            
            if (pc != null)
            {
                if (pc.isSprinting)
                {
                    exertionMultiplier = 2.4f; // Chạy nước rút đốt calo x2.4
                }
                else
                {
                    CharacterController cc = pc.GetComponent<CharacterController>();
                    if (cc != null)
                    {
                        Vector3 horizVel = cc.velocity;
                        horizVel.y = 0f;
                        if (horizVel.magnitude > 0.2f)
                        {
                            exertionMultiplier = 1.3f; // Đi bộ bình thường
                        }
                    }
                }
            }

            float currentHungerDrain = baseHungerDrainRate * exertionMultiplier;
            // Chạy nước rút đổ nhiều mồ hôi nên mất nước nhanh gấp 2.8 lần
            float thirstExertion = (pc != null && pc.isSprinting) ? 2.8f : exertionMultiplier;
            float currentThirstDrain = baseThirstDrainRate * thirstExertion;

            if (currentHunger > 0) currentHunger -= currentHungerDrain * Time.deltaTime;
            if (currentThirst > 0) currentThirst -= currentThirstDrain * Time.deltaTime;

            if (currentHunger < 0) currentHunger = 0;
            if (currentThirst < 0) currentThirst = 0;

            // ═══════════════════════════════════════════════════════════════
            // 2. TỤT SANITY KHI ĐÓI & KHÁT (HUNGER & THIRST PANGS)
            // ═══════════════════════════════════════════════════════════════
            bool isStarving = currentHunger <= 0;   // Đói cạn đáy
            bool isDehydrated = currentThirst <= 0; // Khát cạn đáy

            // A. Cơn đói:
            if (isStarving)
            {
                TakeDamage(0.14f * Time.deltaTime); // Mất máu chậm do chết đói
                DrainSanity(0.18f * Time.deltaTime); // Tinh thần suy sụp do chết đói
            }
            else if (currentHunger < 25f)
            {
                // Dạ dày cồn cào bắt đầu làm tụt Sanity
                DrainSanity(0.08f * Time.deltaTime);
                if (Time.time - lastHungerNoticeTime >= 22f)
                {
                    ShowStatusNotice("🍖 Dạ dày cồn cào... Bạn đang bị đói bụng (-Tinh thần)");
                    lastHungerNoticeTime = Time.time;
                }
            }

            // B. Cơn khát:
            if (isDehydrated)
            {
                TakeDamage(0.22f * Time.deltaTime); // Mất máu chậm do mất nước
                DrainSanity(0.22f * Time.deltaTime); // Tinh thần suy sụp do khát khô họng
            }
            else if (currentThirst < 25f)
            {
                // Cổ họng khô khát bắt đầu làm tụt Sanity
                DrainSanity(0.10f * Time.deltaTime);
                if (Time.time - lastThirstNoticeTime >= 22f)
                {
                    ShowStatusNotice("💧 Cổ họng khô khát... Bạn cần tìm nước uống (-Tinh thần)");
                    lastThirstNoticeTime = Time.time;
                }
            }

            // ═══════════════════════════════════════════════════════════════
            // 3. TỤT SANITY KHI LẠNH RÉT RUN (COLD / FREEZING)
            // ═══════════════════════════════════════════════════════════════
            CheckNearbyCampfire();

            if (playerWetness == null) playerWetness = GetComponent<Environment.Weather.PlayerWetness>();
            if (playerWetness != null && playerWetness.wetness > 60f && !isNearFire)
            {
                // Bị ướt sũng dầm mưa rét buốt mà không có lửa sưởi
                DrainSanity(0.14f * Time.deltaTime);
                if (Time.time - lastColdNoticeTime >= 15f)
                {
                    ShowStatusNotice("🥶 Cơ thể ướt sũng rét run... Cần tìm lửa sưởi ấm (-Tinh thần)");
                    lastColdNoticeTime = Time.time;
                }
            }

            // ═══════════════════════════════════════════════════════════════
            // 4. TỤT SANITY KHI MÁU BỊ TỤT QUÁ NHIỀU / BỊ ĐÁNH CHO GẦN CHẾT
            // ═══════════════════════════════════════════════════════════════
            if (currentHealth < 18f)
            {
                // Hấp hối cận kề cái chết -> Hoảng loạn tột độ
                DrainSanity(0.35f * Time.deltaTime);
                if (Time.time - lastHealthPanicNoticeTime >= 10f)
                {
                    ShowStatusNotice("🩸 Máu tụt quá thấp! Cận kề cái chết khiến bạn hoảng loạn (-Tinh thần)");
                    lastHealthPanicNoticeTime = Time.time;
                }
            }
            else if (currentHealth < 35f)
            {
                // Máu bị tụt quá nhiều, đau đớn
                DrainSanity(0.18f * Time.deltaTime);
                if (Time.time - lastHealthPanicNoticeTime >= 14f)
                {
                    ShowStatusNotice("🩸 Mất máu nghiêm trọng khiến bạn suy sụp tinh thần (-Tinh thần)");
                    lastHealthPanicNoticeTime = Time.time;
                }
            }

            // ═══════════════════════════════════════════════════════════════
            // 5. HỒI PHỤC SANITY (LỬA TRẠI & CƠ THỂ KHỎE MẠNH AN TOÀN)
            // ═══════════════════════════════════════════════════════════════
            if (isNearFire)
            {
                // 🔥 ĐỨNG GẦN LỬA TRẠI: Hơi ấm xoa dịu tinh thần, hồi phục Sanity liên tục!
                RestoreSanity(campfireSanityRegenRate * Time.deltaTime);
                if (Time.time - lastFireNoticeTime >= 12f)
                {
                    ShowStatusNotice("🔥 Hơi ấm lửa trại đang xoa dịu tâm trí (+Tinh thần)");
                    lastFireNoticeTime = Time.time;
                }
            }
            else if (currentHunger >= 50f && currentThirst >= 50f && currentHealth >= 70f && currentSanity < 100f)
            {
                // Khi không đói, không khát, không lạnh, máu đầy đủ: Tinh thần tự bình yên ổn định
                RestoreSanity(0.04f * Time.deltaTime);
            }

            // Trạng thái phát điên (Insanity) khi Sanity = 0
            isInsane = (currentSanity <= 0);
            if (currentSanity <= 0) currentSanity = 0;

            // ═══════════════════════════════════════════════════════════════
            // 6. HỒI MÁU TỰ NHIÊN KHI CƠ THỂ NO ĐỦ (NATURAL HEALTH REGEN)
            // Chuẩn The Forest & Green Hell: No bụng (>70%), Đủ nước (>70%), Tinh thần ổn (>50%) -> Tự hồi máu!
            // ═══════════════════════════════════════════════════════════════
            bool canNaturallyHeal = (currentHunger >= 70f && currentThirst >= 70f && currentSanity >= 50f && currentHealth < maxHealth);
            isHealingNaturally = canNaturallyHeal;

            if (canNaturallyHeal)
            {
                Heal(naturalHealRate * Time.deltaTime);
                if (Time.time - lastHealNoticeTime >= 18f && currentHealth < maxHealth - 5f)
                {
                    ShowStatusNotice("💚 Cơ thể no đủ đang tự hồi phục sinh lực (+Máu)");
                    lastHealNoticeTime = Time.time;
                }
            }

            // ═══════════════════════════════════════════════════════════════
            // 7. MỆT MỎI (FATIGUE): Giảm trần thể lực từ từ, ngủ để hồi 100%
            // ═══════════════════════════════════════════════════════════════
            if (maxStamina > 25f)
            {
                maxStamina -= fatigueDrainRate * Time.deltaTime;
                if (currentStamina > maxStamina) currentStamina = maxStamina;
            }
        }

        /// <summary>
        /// Gọi khi người chơi thực hiện hoạt động nặng (chặt cây, vung rìu)
        /// </summary>
        public void OnExertion(float effort = 1f)
        {
            if (currentHunger > 0) currentHunger -= 0.12f * effort;
            if (currentThirst > 0) currentThirst -= 0.20f * effort;
        }

        /// <summary>
        /// Quét các đống lửa trại xung quanh trong bán kính 5.5m
        /// </summary>
        private void CheckNearbyCampfire()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, 5.5f);
            isNearFire = false;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null) continue;
                string colName = colliders[i].name.ToLower();
                if (colName.Contains("fire") || colName.Contains("camp") || colliders[i].GetComponentInParent<Survival.CookingStation>() != null)
                {
                    isNearFire = true;
                    break;
                }
            }
        }

        public void ShowStatusNotice(string msg)
        {
            statusNoticeText = msg;
            statusNoticeTimer = 3.5f;
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

            // BỊ ZOMBIE / THÚ DỮ TẤN CÔNG HOẶC CHẤN THƯƠNG NẶNG (Sát thương tức thời >= 2 HP)
            if (amount >= 2f)
            {
                // Tụt một lượng Sanity tương ứng chấn thương tâm lý kinh hoàng
                float sanityShock = Mathf.Clamp(amount * 0.45f, 2f, 15f);
                DrainSanity(sanityShock);

                if (currentHealth < 30f)
                {
                    ShowStatusNotice("⚠️ Bị cắn xé trọng thương! Tinh thần hoảng loạn (-" + Mathf.RoundToInt(sanityShock) + " Tinh thần)");
                }
            }

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

        private void OnGUI()
        {
            if (statusNoticeTimer > 0f && !string.IsNullOrEmpty(statusNoticeText))
            {
                if (statusStyle == null)
                {
                    statusStyle = new GUIStyle(GUI.skin.label);
                    statusStyle.fontSize = 15;
                    statusStyle.fontStyle = FontStyle.Bold;
                    statusStyle.normal.textColor = new Color(1f, 0.95f, 0.7f);
                    statusStyle.alignment = TextAnchor.MiddleCenter;

                    statusShadowStyle = new GUIStyle(statusStyle);
                    statusShadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
                }

                float alpha = Mathf.Clamp01(statusNoticeTimer);
                GUI.color = new Color(1, 1, 1, alpha);

                float w = 600f;
                float h = 32f;
                float x = (Screen.width - w) / 2f;
                float y = 60f;

                GUI.Label(new Rect(x + 1, y + 1, w, h), statusNoticeText, statusShadowStyle);
                GUI.Label(new Rect(x, y, w, h), statusNoticeText, statusStyle);
                GUI.color = Color.white;
            }
        }
    }
}

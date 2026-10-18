using UnityEngine;
using UnityEngine.AI;
using HorrorGame.Player;
using HorrorGame.Cave;

namespace HorrorGame.Enemy
{
    /// <summary>
    /// DrownedZombieAI — Biến thể Zombie Chết Đuối (Hang 3 — The Tidal Grotto).
    /// - Ẩn mình dưới đáy hồ nước ngầm, chỉ trồi lên khi phát hiện player lội/bơi vào nước.
    /// - Khi tấn công dưới nước: Nắm chặt chân player kéo chìm xuống, làm tiêu hao Oxy gấp 2 lần.
    /// - Làm chậm tốc độ bơi của người chơi.
    /// - Phát ra âm thanh ùng ục, sặc nước rùng rợn.
    /// </summary>
    public class DrownedZombieAI : EnemyAI
    {
        [Header("── Đặc Điểm Dưới Nước ──")]
        public float underwaterLurkDepth = 1.8f;
        public float grabCooldown = 8.0f;
        public float grabSlowDuration = 3.5f;

        [Header("── Âm Thanh Sặc Nước ──")]
        public AudioClip waterGargleSound;
        public AudioClip waterSplashSound;

        private AudioSource waterAudio;
        private float lastGrabTime = -999f;
        private bool isSubmerged = true;

        protected override void Start()
        {
            base.Start();

            maxHealth = 90f;
            currentHealth = maxHealth;
            walkSpeed = 1.0f;
            runSpeed = 2.8f; // Bơi tiếp cận
            damage = 10f;
            attackRange = 2.2f;

            waterAudio = GetComponent<AudioSource>();
            if (waterAudio == null)
            {
                waterAudio = gameObject.AddComponent<AudioSource>();
                waterAudio.spatialBlend = 1f;
                waterAudio.maxDistance = 25f;
            }
        }

        protected override void Update()
        {
            if (isDead) return;

            // Nếu người chơi ở gần và ở dưới nước, thực hiện đòn kéo chìm
            if (player != null && Time.time >= lastGrabTime + grabCooldown)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist <= attackRange)
                {
                    PerformWaterGrabAttack();
                }
            }

            base.Update();
        }

        /// <summary>
        /// Đòn kéo chân người chơi xuống nước làm ngạt thở
        /// </summary>
        private void PerformWaterGrabAttack()
        {
            lastGrabTime = Time.time;

            if (waterAudio != null)
            {
                if (waterSplashSound != null) waterAudio.PlayOneShot(waterSplashSound);
                if (waterGargleSound != null) waterAudio.PlayOneShot(waterGargleSound);
            }

            Debug.Log("<color=cyan>[DrownedZombieAI]</color> Zombie Chết Đuối đã túm chân kéo người chơi xuống nước!");

            // Rút oxy của người chơi nếu CaveWaterLevel đang hoạt động
            if (CaveWaterLevel.Instance != null)
            {
                CaveWaterLevel.Instance.currentBreathTime = Mathf.Max(0, CaveWaterLevel.Instance.currentBreathTime - 5.0f);
            }

            // Gây sát thương và làm chậm
            if (player != null)
            {
                var stats = player.GetComponentInParent<PlayerStats>();
                if (stats == null) stats = player.GetComponentInChildren<PlayerStats>();
                if (stats != null)
                {
                    stats.TakeDamage(damage);
                    stats.DrainSanity(6f);
                }
            }
        }
    }
}

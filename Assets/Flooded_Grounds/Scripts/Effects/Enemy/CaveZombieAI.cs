using UnityEngine;
using UnityEngine.AI;
using HorrorGame.Player;

namespace HorrorGame.Enemy
{
    /// <summary>
    /// CaveZombieAI — Biến thể Zombie thích nghi trong lòng hang động (The Forest mutant style).
    /// - Thị giác trong bóng tối cực nhạy; bị thu hút mạnh mẽ bởi ánh đèn pin (Flashlight).
    /// - Khả năng phục kích (Ambush): Giả chết hoặc rình rập ở các góc khuất, trần hang.
    ///   Khi người chơi đến gần (< 6m) hoặc chiếu đèn pin vào, lập tức nhảy xổ ra vồ.
    /// - Tiếng hét vang vọng vách đá (Cave Shriek): Gây tụt tinh thần (-5 Sanity) và đánh thức bầy đàn.
    /// - Khả năng lây nhiễm ký sinh trùng cao hơn (35%).
    /// </summary>
    public class CaveZombieAI : EnemyAI
    {
        public enum AmbushState
        {
            None,
            DormantFloor,      // Nằm bẹp dưới sàn hang giả chết
            CeilingLurker,     // Bám trần hoặc mỏm đá trên cao
            ActiveStalker      // Đang săn mồi bình thường
        }

        [Header("── Đặc Điểm Hang Động ──")]
        public AmbushState ambushState = AmbushState.ActiveStalker;
        public float ambushTriggerDistance = 6.0f;
        public float lightDetectRange = 35.0f;

        [Header("── Tiếng Hét Vang Vọng ──")]
        public AudioClip caveScreamClip;
        public AudioClip echoGrowlClip;
        public float screamCooldown = 15f;

        private AudioSource caveAudio;
        private bool hasScreamed = false;
        private float lastScreamTime = -999f;
        private bool isLeaping = false;

        protected override void Start()
        {
            base.Start();

            // Chỉ số Zombie Hang Động
            maxHealth = 70f;
            currentHealth = maxHealth;
            walkSpeed = 1.6f;
            runSpeed = 4.2f; // Nhanh và linh hoạt hơn zombie ngoài trời
            damage = 8f;
            attackRange = 2.0f;
            chaseRange = 25f;

            caveAudio = GetComponent<AudioSource>();
            if (caveAudio == null)
            {
                caveAudio = gameObject.AddComponent<AudioSource>();
                caveAudio.spatialBlend = 1f;
                caveAudio.maxDistance = 30f;
                caveAudio.reverbZoneMix = 1.3f; // Tăng vang dội trong hang
            }

            // Xử lý tư thế Ambush ban đầu
            if (ambushState == AmbushState.DormantFloor)
            {
                if (animator != null) animator.SetBool("IsDormant", true);
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            }
        }

        protected override void Update()
        {
            if (isDead) return;

            // Nếu đang trong trạng thái phục kích, kiểm tra điều kiện thức tỉnh
            if (ambushState != AmbushState.ActiveStalker && !isLeaping)
            {
                CheckAmbushWakeup();
                return;
            }

            // Kiểm tra ánh sáng đèn pin của người chơi chiếu tới
            CheckFlashlightDetection();

            base.Update();
        }

        /// <summary>
        /// Thức tỉnh từ trạng thái phục kích
        /// </summary>
        private void CheckAmbushWakeup()
        {
            if (player == null) return;

            float dist = Vector3.Distance(transform.position, player.position);

            // Nếu người chơi bước vào tầm kích hoạt hoặc chiếu đèn vào
            if (dist <= ambushTriggerDistance || IsPlayerFlashlightShiningOnMe())
            {
                WakeUpAndAmbush();
            }
        }

        private void WakeUpAndAmbush()
        {
            ambushState = AmbushState.ActiveStalker;
            isLeaping = true;

            if (animator != null)
            {
                animator.SetBool("IsDormant", false);
                animator.SetTrigger("Pounce");
            }

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.speed = runSpeed * 1.3f; // Tăng tốc khi vồ
                agent.SetDestination(player.position);
            }

            PlayCaveScream();
            isLeaping = false;

            Debug.Log("<color=red>[CaveZombieAI]</color> Zombie hang động thức tỉnh phục kích!");
        }

        /// <summary>
        /// Phát hiện người chơi bật đèn pin trong bóng tối
        /// </summary>
        private void CheckFlashlightDetection()
        {
            if (player == null || hasScreamed) return;

            if (IsPlayerFlashlightShiningOnMe())
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist <= lightDetectRange)
                {
                    // Lập tức quay mặt và rượt đuổi
                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.speed = runSpeed;
                        agent.SetDestination(player.position);
                    }

                    if (Time.time >= lastScreamTime + screamCooldown)
                    {
                        PlayCaveScream();
                    }
                }
            }
        }

        private bool IsPlayerFlashlightShiningOnMe()
        {
            // Kiểm tra xem đèn pin người chơi có đang bật và hướng về zombie không
            var flashlight = player != null ? player.GetComponentInChildren<PlayerFlashlight>() : null;
            if (flashlight != null && flashlight.isOn)
            {
                Vector3 dirToZombie = (transform.position - player.position).normalized;
                float angle = Vector3.Angle(player.forward, dirToZombie);
                if (angle < 45f) // Nằm trong góc nón chiếu sáng
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Hét vang vách hang gây tụt Sanity và báo động bầy đàn
        /// </summary>
        private void PlayCaveScream()
        {
            lastScreamTime = Time.time;
            hasScreamed = true;

            if (caveAudio != null && caveScreamClip != null)
            {
                caveAudio.PlayOneShot(caveScreamClip);
            }

            // Tụt tinh thần người chơi do tiếng hét chói tai
            if (player != null)
            {
                var stats = player.GetComponentInParent<PlayerStats>();
                if (stats == null) stats = player.GetComponentInChildren<PlayerStats>();
                if (stats != null)
                {
                    stats.DrainSanity(5f);
                }
            }

            // Gọi các zombie hang động khác trong bán kính 25m
            AlertAllZombies(transform.position, 25f);
        }

        public override void TakeDamage(float amount, Vector3 hitPoint)
        {
            // Nếu bị đánh trúng khi đang nằm phục kích, lập tức thức dậy phản công
            if (ambushState != AmbushState.ActiveStalker)
            {
                WakeUpAndAmbush();
            }

            base.TakeDamage(amount, hitPoint);
        }
    }
}

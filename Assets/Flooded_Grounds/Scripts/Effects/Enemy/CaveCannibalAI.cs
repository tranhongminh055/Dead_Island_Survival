using UnityEngine;
using UnityEngine.AI;
using HorrorGame.Player;

namespace HorrorGame.Enemy
{
    /// <summary>
    /// CaveCannibalAI — Biến thể Thổ Dân Hang Động (Cannibal Shaman & Warrior).
    /// - Shaman: Ném giáo từ xa (Spear Throw) khi cách 10-25m, tụng niệm nghi lễ gọi bầy Zombie.
    /// - Warrior: Chiến binh đeo mặt nạ sọ người, cầm vũ khí xương kép chém cực đau.
    /// - Sơn mình phát sáng lân tinh trong bóng tối rùng rợn.
    /// </summary>
    public class CaveCannibalAI : CannibalAI
    {
        public enum CannibalRole
        {
            Warrior, // Cận chiến hung hãn
            Shaman   // Tầm xa ném giáo + triệu hồi quái
        }

        [Header("── Vai Trò Trong Hang ──")]
        public CannibalRole role = CannibalRole.Shaman;

        [Header("── Kỹ Năng Shaman (Ném Giáo) ──")]
        public GameObject spearProjectilePrefab;
        public Transform spearLaunchPoint;
        public float spearThrowRangeMin = 8.0f;
        public float spearThrowRangeMax = 22.0f;
        public float spearThrowCooldown = 5.0f;
        public float spearDamage = 16.0f;
        public AudioClip spearThrowSound;

        [Header("── Kỹ Năng Triệu Hồi (Summon Ritual) ──")]
        public GameObject summonZombiePrefab;
        public float summonCooldown = 30.0f;
        public AudioClip chantSound;

        private float lastSpearThrowTime = -999f;
        private float lastSummonTime = -999f;
        private AudioSource caveAudio;

        protected override void Start()
        {
            base.Start();

            caveAudio = GetComponent<AudioSource>();
            if (caveAudio == null)
            {
                caveAudio = gameObject.AddComponent<AudioSource>();
                caveAudio.spatialBlend = 1f;
                caveAudio.maxDistance = 35f;
            }

            if (role == CannibalRole.Warrior)
            {
                maxHealth = 120f;
                currentHealth = maxHealth;
                damage = 15f;
                runSpeed = 5.0f;
            }
            else
            {
                maxHealth = 95f;
                currentHealth = maxHealth;
                damage = 10f;
                runSpeed = 4.4f;
            }
        }

        protected override void Update()
        {
            if (isDead) return;

            // Nếu là Shaman, kiểm tra cơ hội ném giáo hoặc triệu hồi quái
            if (role == CannibalRole.Shaman && player != null)
            {
                float dist = Vector3.Distance(transform.position, player.position);

                // 1. Kiểm tra ném giáo từ xa
                if (dist >= spearThrowRangeMin && dist <= spearThrowRangeMax)
                {
                    if (Time.time >= lastSpearThrowTime + spearThrowCooldown)
                    {
                        ThrowSpearAtPlayer();
                    }
                }

                // 2. Kiểm tra tụng niệm triệu hồi quái khi máu dưới 60%
                if (currentHealth <= maxHealth * 0.6f && Time.time >= lastSummonTime + summonCooldown)
                {
                    PerformSummonRitual();
                }
            }

            base.Update();
        }

        /// <summary>
        /// Ném ngọn giáo về phía người chơi
        /// </summary>
        private void ThrowSpearAtPlayer()
        {
            lastSpearThrowTime = Time.time;

            // Dừng lại và quay mặt về phía người chơi
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

            if (animator != null)
            {
                animator.SetTrigger("Attack"); // Hoặc Throw nếu có
            }

            if (caveAudio != null && spearThrowSound != null)
            {
                caveAudio.PlayOneShot(spearThrowSound);
            }

            Vector3 spawnPos = spearLaunchPoint != null ? spearLaunchPoint.position : transform.position + Vector3.up * 1.5f + transform.forward * 0.8f;
            Vector3 targetPos = player.position + Vector3.up * 1.0f;
            Vector3 throwDir = (targetPos - spawnPos).normalized;

            if (spearProjectilePrefab != null)
            {
                GameObject spear = Instantiate(spearProjectilePrefab, spawnPos, Quaternion.LookRotation(throwDir));
                Rigidbody rb = spear.GetComponent<Rigidbody>();
                if (rb == null) rb = spear.AddComponent<Rigidbody>();
                rb.velocity = throwDir * 22f; // Phóng nhanh
                rb.useGravity = true;

                // Tự hủy sau 6s
                Destroy(spear, 6f);
            }
            else
            {
                // Fallback nếu chưa có Prefab 3D: Dùng Raycast gây sát thương tức thì
                RaycastHit hit;
                if (Physics.Raycast(spawnPos, throwDir, out hit, spearThrowRangeMax))
                {
                    if (hit.collider.CompareTag("Player"))
                    {
                        var stats = hit.collider.GetComponentInParent<PlayerStats>();
                        if (stats != null) stats.TakeDamage(spearDamage);
                    }
                }
            }

            // Tiếp tục di chuyển sau 0.8s
            Invoke("ResumeAgentMovement", 0.8f);
        }

        private void ResumeAgentMovement()
        {
            if (agent != null && agent.isOnNavMesh && !isDead)
            {
                agent.isStopped = false;
            }
        }

        /// <summary>
        /// Nghi thức tụng niệm gọi Cave Zombie trợ chiến
        /// </summary>
        private void PerformSummonRitual()
        {
            lastSummonTime = Time.time;

            if (caveAudio != null && chantSound != null)
            {
                caveAudio.PlayOneShot(chantSound);
            }

            Debug.Log("<color=purple>[CaveCannibalAI]</color> Shaman Thổ Dân đang tụng niệm gọi bầy đàn!");

            // Sinh 1 Cave Zombie bên cạnh
            if (summonZombiePrefab != null)
            {
                Vector3 spawnOffset = transform.right * (Random.value > 0.5f ? 2.5f : -2.5f);
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position + spawnOffset, out hit, 4f, NavMesh.AllAreas))
                {
                    Instantiate(summonZombiePrefab, hit.position, Quaternion.identity);
                }
            }
        }
    }
}

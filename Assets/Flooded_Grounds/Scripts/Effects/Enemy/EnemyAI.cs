using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace HorrorGame.Enemy
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Zombie Settings")]
        public Transform player; // Kéo thả Leon vào đây
        public float chaseRange = 30f; // Khoảng cách bắt đầu rượt đuổi (30m)
        public float attackRange = 2f; // Khoảng cách vung tay cào
        public float attackCooldown = 1.8f; // Nghỉ 1.8 giây giữa 2 cú đánh
        public float damage = 5f; // Sát thương mỗi cú cào (Chỉ mất 5 máu mỗi nhát)

        [Header("Tốc độ & Đi tuần (Wander)")]
        public float walkSpeed = 1.5f;
        public float runSpeed = 3.6f;
        private float wanderTimer = 0f;
        private float nextWanderDelay = 4f;
        private bool isInvestigatingSound = false;
        private float soundInvestigateTimer = 0f;

        [Header("Zombie Health Settings")]
        public float maxHealth = 100f; // Máu tối đa của Zombie
        private float currentHealth;

        private NavMeshAgent agent;
        private Animator animator;
        private bool isDead = false;
        private float lastAttackTime = 0f;
        private Coroutine flashRoutine;

        public bool IsDead { get { return isDead; } }
        public float CurrentHealth { get { return currentHealth; } }

        void Awake()
        {
            EnsureCollider();
        }

        public void EnsureCollider()
        {
            CapsuleCollider col = GetComponent<CapsuleCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<CapsuleCollider>();
            }
            col.center = new Vector3(0f, 0.95f, 0f);
            col.height = 1.9f;
            col.radius = 0.35f;
            col.isTrigger = false;
        }

        void Start()
        {
            EnsureCollider();
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            currentHealth = maxHealth; // Hồi đầy máu khi mới sinh ra

            // Tự động tìm người chơi nếu quên kéo thả
            if (player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
                else if (Camera.main != null) player = Camera.main.transform;
            }

            // Nếu Zombie này được đặt sẵn gần khu vực xác máy bay (< 20m), tự động di dời ra ngoài rìa
            float distToCrash = Vector3.Distance(transform.position, ZombieSpawner.CRASH_SITE_CENTER);
            if (distToCrash < 20f)
            {
                Vector3 farPos = ZombieSpawner.CRASH_SITE_CENTER + new Vector3(Random.Range(25f, 45f) * (Random.value > 0.5f ? 1 : -1), 0, Random.Range(25f, 45f) * (Random.value > 0.5f ? 1 : -1));
                NavMeshHit hit;
                if (NavMesh.SamplePosition(farPos, out hit, 30f, NavMesh.AllAreas))
                {
                    if (agent != null) agent.Warp(hit.position);
                    else transform.position = hit.position;
                }
            }
        }

        void Update()
        {
            if (isDead) return;

            if (player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
                else if (Camera.main != null) player = Camera.main.transform;
                if (player == null) return;
            }

            // Đang trong Cutscene máy bay rơi: Zombie hoàn toàn bất động, không tiếp cận hay tấn công người chơi
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive)
            {
                if (agent != null && agent.enabled) agent.SetDestination(transform.position);
                if (animator != null) animator.SetFloat("Speed", 0f);
                return;
            }

            // Chỉ tính khoảng cách trên mặt phẳng (bỏ qua độ cao Y) để tránh lỗi lệch tâm (pivot)
            Vector3 targetPos = new Vector3(player.position.x, transform.position.y, player.position.z);
            float distanceToPlayer = Vector3.Distance(transform.position, targetPos);

            if (distanceToPlayer <= attackRange)
            {
                // Áp sát -> Dừng lại và Tấn công
                agent.speed = 0f;
                agent.SetDestination(transform.position); 
                animator.SetFloat("Speed", 0f); // Trở về dáng Idle
                isInvestigatingSound = false;
                
                // Nhìn thẳng vào mặt người chơi
                transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
                
                // Kiểm tra xem đã hết thời gian nghỉ giữa 2 đòn chưa
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    animator.SetTrigger("Attack"); // Kích hoạt animation cào
                    lastAttackTime = Time.time;
                    
                    // Tìm component PlayerStats (có thể ở object cha hoặc con)
                    HorrorGame.Player.PlayerStats stats = player.GetComponentInParent<HorrorGame.Player.PlayerStats>();
                    if (stats == null) stats = player.GetComponentInChildren<HorrorGame.Player.PlayerStats>();

                    if (stats != null)
                    {
                        stats.TakeDamage(damage);
                    }
                    else
                    {
                        Debug.LogError("Không tìm thấy component PlayerStats trên người chơi (Player object)!");
                    }
                }
            }
            else if (distanceToPlayer <= chaseRange)
            {
                // Nằm trong tầm nhìn -> Chạy rượt đuổi
                agent.speed = runSpeed;
                agent.SetDestination(player.position);
                animator.SetFloat("Speed", 1f); // Kích hoạt animation Walk/Run
                isInvestigatingSound = false;
            }
            else if (isInvestigatingSound)
            {
                // Đang chạy lại kiểm tra tiếng súng nổ
                soundInvestigateTimer -= Time.deltaTime;
                if (soundInvestigateTimer <= 0f || agent.remainingDistance <= 2f)
                {
                    isInvestigatingSound = false;
                }
                else
                {
                    agent.speed = runSpeed;
                    animator.SetFloat("Speed", 1f);
                }
            }
            else
            {
                // Ở xa -> Lảng vảng đi dạo (Wander tuần tra)
                wanderTimer += Time.deltaTime;
                if (wanderTimer >= nextWanderDelay)
                {
                    wanderTimer = 0f;
                    nextWanderDelay = Random.Range(5f, 10f);

                    Vector3 randDir = Random.insideUnitSphere * 15f;
                    randDir += transform.position;
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(randDir, out hit, 15f, NavMesh.AllAreas))
                    {
                        agent.speed = walkSpeed;
                        agent.SetDestination(hit.position);
                        animator.SetFloat("Speed", 0.5f);
                    }
                }

                if (agent.remainingDistance <= 1.2f)
                {
                    animator.SetFloat("Speed", 0f); // Tới nơi đứng thở
                }
            }
        }

        /// <summary>
        /// Phát tín hiệu tiếng súng thu hút mọi Zombie gần đó chạy lại
        /// </summary>
        public static void AlertAllZombies(Vector3 soundOrigin, float radius)
        {
            EnemyAI[] all = FindObjectsOfType<EnemyAI>();
            foreach (var z in all)
            {
                if (z != null && !z.isDead)
                {
                    float dist = Vector3.Distance(z.transform.position, soundOrigin);
                    if (dist <= radius)
                    {
                        z.InvestigateSound(soundOrigin);
                    }
                }
            }
        }

        public void InvestigateSound(Vector3 soundOrigin)
        {
            if (isDead || agent == null || !agent.enabled) return;
            agent.speed = runSpeed;
            agent.SetDestination(soundOrigin);
            isInvestigatingSound = true;
            soundInvestigateTimer = 8f;
            if (animator != null) animator.SetFloat("Speed", 1f);
        }

        // Gọi hàm này khi Zombie bị người chơi bắn/chém trúng
        public void TakeDamage(float amount)
        {
            TakeDamage(amount, Vector3.zero);
        }

        public void TakeDamage(float amount, Vector3 hitPoint)
        {
            if (isDead) return;

            currentHealth -= amount;
            Debug.Log(string.Format("[EnemyAI] Zombie bị bắn trúng! -{0} HP, còn lại: {1}/{2}", amount, Mathf.Max(0, currentHealth), maxHealth));

            // Hiệu ứng chớp đỏ toàn thân báo hiệu trúng đòn
            if (gameObject.activeInHierarchy)
            {
                if (flashRoutine != null) StopCoroutine(flashRoutine);
                flashRoutine = StartCoroutine(HitFlashRoutine());
            }

            // Bị bắn trúng -> Ngay lập tức quay lại rượt đuổi người chơi (nếu có)
            if (player != null && !isDead)
            {
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.speed = runSpeed;
                    agent.SetDestination(player.position);
                }
                if (animator != null && animator.isActiveAndEnabled)
                {
                    animator.SetFloat("Speed", 1f);
                }
            }

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private IEnumerator HitFlashRoutine()
        {
            SkinnedMeshRenderer[] smrs = GetComponentsInChildren<SkinnedMeshRenderer>();
            if (smrs == null || smrs.Length == 0) yield break;

            Color[] origColors = new Color[smrs.Length];
            for (int i = 0; i < smrs.Length; i++)
            {
                if (smrs[i] != null && smrs[i].material != null && smrs[i].material.HasProperty("_Color"))
                {
                    origColors[i] = smrs[i].material.color;
                    smrs[i].material.color = new Color(1f, 0.2f, 0.2f, 1f);
                }
            }

            yield return new WaitForSeconds(0.08f);

            for (int i = 0; i < smrs.Length; i++)
            {
                if (smrs[i] != null && smrs[i].material != null && smrs[i].material.HasProperty("_Color"))
                {
                    smrs[i].material.color = origColors[i];
                }
            }
            flashRoutine = null;
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            Debug.Log("[EnemyAI] Zombie đã bị tiêu diệt!");

            // Ngừng tìm đường và tắt NavMeshAgent an toàn
            if (agent != null)
            {
                if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }
                agent.enabled = false;
            }

            // Tắt toàn bộ Collider trên người Zombie để người chơi có thể bước qua xác và đạn không vướng
            Collider[] colls = GetComponentsInChildren<Collider>();
            for (int i = 0; i < colls.Length; i++)
            {
                if (colls[i] != null) colls[i].enabled = false;
            }

            // Tắt Animator để dừng dáng đứng vĩnh viễn và thực hiện ngã gục
            if (animator != null)
            {
                animator.enabled = false;
            }

            // Kích hoạt hoạt ảnh gục ngã xuống đất
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(DeathToppleRoutine());
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private IEnumerator DeathToppleRoutine()
        {
            float duration = 0.55f;
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;

            // Ngã ngửa ra sau tiếp đất tự nhiên
            Quaternion targetRot = Quaternion.Euler(-80f, transform.eulerAngles.y, 0f);
            Vector3 targetPos = startPos - transform.forward * 0.35f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = Mathf.Sin(t * Mathf.PI * 0.5f);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, ease);
                transform.position = Vector3.Lerp(startPos, targetPos, ease);
                yield return null;
            }

            // Xác nằm nguyên trên mặt đất trong 12 giây
            yield return new WaitForSeconds(12f);

            // Chìm dần xuống đất trước khi biến mất
            float sinkDuration = 2f;
            float sinkElapsed = 0f;
            Vector3 settlePos = transform.position;
            while (sinkElapsed < sinkDuration)
            {
                sinkElapsed += Time.deltaTime;
                float st = sinkElapsed / sinkDuration;
                transform.position = settlePos - new Vector3(0f, st * 1.0f, 0f);
                yield return null;
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// Hiệu ứng văng máu khi đạn hoặc vũ khí chém trúng cơ thể
        /// </summary>
        public static void SpawnBloodImpact(Vector3 position, Vector3 normal)
        {
            GameObject fx = new GameObject("BloodImpactFX");
            fx.transform.position = position;
            if (normal != Vector3.zero)
            {
                fx.transform.rotation = Quaternion.LookRotation(normal);
            }

            ParticleSystem ps = fx.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = 0.35f;
            main.startSpeed = 3f;
            main.startSize = 0.15f;
            main.startColor = new Color(0.7f, 0.05f, 0.05f, 0.95f);
            main.playOnAwake = true;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 18) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.05f;

            ParticleSystemRenderer rend = fx.GetComponent<ParticleSystemRenderer>();
            Shader sh = Shader.Find("Particles/Additive");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh != null)
            {
                rend.material = new Material(sh);
                rend.material.color = new Color(0.7f, 0.05f, 0.05f, 0.95f);
            }

            Destroy(fx, 0.6f);
        }

        // Dùng cái này để dễ dàng nhìn thấy vòng tròn giới hạn trong cửa sổ Scene
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
        }
    }
}

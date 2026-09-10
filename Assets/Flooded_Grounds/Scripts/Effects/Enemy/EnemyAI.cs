using UnityEngine;
using UnityEngine.AI;

namespace HorrorGame.Enemy
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Zombie Settings")]
        public Transform player; // Kéo thả Leon vào đây
        public float chaseRange = 15f; // Khoảng cách bắt đầu rượt đuổi
        public float attackRange = 2f; // Khoảng cách vung tay cào
        public float attackCooldown = 2f; // Nghỉ 2 giây giữa 2 cú đánh
        public float damage = 5f; // Sát thương mỗi cú cào (Chỉ mất 5 máu mỗi nhát)
        [Header("Zombie Health Settings")]
        public float maxHealth = 100f; // Máu tối đa của Zombie
        private float currentHealth;

        private NavMeshAgent agent;
        private Animator animator;
        private bool isDead = false;
        private float lastAttackTime = 0f;

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            currentHealth = maxHealth; // Hồi đầy máu khi mới sinh ra

            // Tự động tìm người chơi nếu quên kéo thả
            if (player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }

            // Nếu Zombie này được đặt sẵn gần khu vực xác máy bay (< 120m), tự động di dời ra xa
            float distToCrash = Vector3.Distance(transform.position, ZombieSpawner.CRASH_SITE_CENTER);
            if (distToCrash < 120f)
            {
                Vector3 farPos = ZombieSpawner.CRASH_SITE_CENTER + new Vector3(Random.Range(140f, 200f) * (Random.value > 0.5f ? 1 : -1), 0, Random.Range(140f, 200f) * (Random.value > 0.5f ? 1 : -1));
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
            if (isDead || player == null) return;

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
                agent.SetDestination(transform.position); 
                animator.SetFloat("Speed", 0f); // Trở về dáng Idle
                
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
                agent.SetDestination(player.position);
                animator.SetFloat("Speed", 1f); // Kích hoạt animation Walk/Run
            }
            else
            {
                // Ở xa -> Đứng yên lảng vảng
                agent.SetDestination(transform.position);
                animator.SetFloat("Speed", 0f); // Idle
            }
        }

        // Gọi hàm này khi Zombie bị người chơi bắn/chém trúng
        public void TakeDamage(float amount)
        {
            if (isDead) return;
            
            currentHealth -= amount;
            Debug.Log("Zombie bị bắn trúng! Máu còn: " + currentHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            isDead = true;
            agent.isStopped = true; // Ngừng tìm đường
            animator.SetTrigger("Die");
            
            // Tắt va chạm để người chơi có thể bước qua xác chết
            Collider coll = GetComponent<Collider>();
            if (coll != null) coll.enabled = false;
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

using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace HorrorGame.Enemy
{
    public class ZombieSpawner : MonoBehaviour
    {
        [Header("Spawner Settings")]
        public GameObject zombiePrefab; // Kéo thả khối Zombie Prefab vào đây
        public int maxZombies = 24; // Giới hạn tối ưu FPS (tránh tụt FPS do 80 NavMeshAgent chạy cùng lúc)
        public float spawnInterval = 4.0f; // Nhịp spawn mượt mà, tăng dần theo ngày/đêm
        public float spawnRadius = 55f; // Bán kính khu vực đẻ ngẫu nhiên nếu không dùng Spawn Points

        [Header("Spawn Points (Khắp Map)")]
        public Transform[] spawnPoints; // Danh sách các điểm đẻ Zombie (Tạo Empty Object rồi kéo vào đây)
        public bool autoScatterOverMap = true; // Rải Zombie khắp các khu vực trên bản đồ

        [Header("Dynamic Player Spawner (Sinh Zombie quanh người chơi)")]
        [Tooltip("Bật chế độ sinh Zombie xung quanh người chơi để luôn chạm trán Zombie")]
        public bool spawnNearPlayer = true;
        public float minPlayerDistance = 22f; // Xuất hiện từ cự ly an toàn 22m (tránh đẻ sát lưng)
        public float maxPlayerDistance = 52f; // Vây quanh tới 52m
        public float despawnDistance = 85f;   // Quá 85m tự thu hồi để giữ FPS luôn mượt
        public int initialHordeCount = 8;     // Sinh gọn 8 con ban đầu, không gây khựng khung hình

        [Header("Crash Site Safe Zone (Vùng an toàn quanh xác máy bay)")]
        public static readonly Vector3 CRASH_SITE_CENTER = new Vector3(537f, 17.6f, 545f);
        public float crashSafeRadius = 26f; // Vùng an toàn 26m quanh xác máy bay để người chơi kịp nhặt đồ

        private float timer = 0f;
        private List<GameObject> activeZombies = new List<GameObject>();
        private NavMeshTriangulation navMeshData;
        private Transform playerTransform;
        private bool initialSpawnDone = false;
        private int spawnToggle = 0; // Luân phiên: 1 con quanh player, 1 con ở nơi khác trên map

        void Awake()
        {
            // Giữ giới hạn hợp lý để mượt FPS (không ép lên 80 con nữa)
            if (crashSafeRadius < 22f) crashSafeRadius = 26f;
            if (maxZombies > 28) maxZombies = 24;
            if (initialHordeCount > 12) initialHordeCount = 8;
            if (spawnInterval < 3f) spawnInterval = 4.0f;
            if (despawnDistance > 95f) despawnDistance = 85f;
        }

        void Start()
        {
            // Lấy toàn bộ mạng lưới mặt đất đã nướng (Bake) của bản đồ
            navMeshData = NavMesh.CalculateTriangulation();
            
            if (navMeshData.vertices.Length == 0)
            {
                Debug.LogError("ZombieSpawner: Không tìm thấy dữ liệu NavMesh! Bạn phải Bake NavMesh cho mặt đất trước.");
            }

            FindPlayer();

            // Nếu zombiePrefab đang trỏ tới 1 Zombie sống trong Scene, tạo bản mẫu ẩn riêng để tránh bị hỏng Animator hoặc bị Destroy khi con gốc chết!
            if (zombiePrefab != null && zombiePrefab.scene.IsValid())
            {
                GameObject safeTemplate = Instantiate(zombiePrefab);
                safeTemplate.name = "[ZombieTemplate_Safe]";
                safeTemplate.SetActive(false);
                Animator tplAnim = safeTemplate.GetComponent<Animator>();
                if (tplAnim == null) tplAnim = safeTemplate.GetComponentInChildren<Animator>();
                if (tplAnim != null)
                {
                    tplAnim.enabled = true;
                    tplAnim.applyRootMotion = false;
                }
                zombiePrefab = safeTemplate;
            }

            // Dọn sạch Zombie quá sát xác máy bay (< 20m) đưa ra mép rừng
            ClearZombiesNearCrashSite();

            // Đảm bảo bầy Thổ Dân Ăn Thịt Người (CannibalSpawner) luôn hoạt động đồng hành
            if (FindObjectOfType<CannibalSpawner>() == null)
            {
                GameObject csObj = new GameObject("[CannibalSpawner]");
                csObj.AddComponent<CannibalSpawner>();
                Debug.Log("🏹 [ZombieSpawner] Đã khởi tạo [CannibalSpawner] trong Scene!");
            }
        }

        private Transform FindPlayer()
        {
            if (playerTransform != null) return playerTransform;
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else if (Camera.main != null) playerTransform = Camera.main.transform;
            return playerTransform;
        }

        public void ClearZombiesNearCrashSite()
        {
            EnemyAI[] allZombies = FindObjectsOfType<EnemyAI>();
            foreach (var z in allZombies)
            {
                if (z == null) continue;
                float dist = Vector3.Distance(z.transform.position, CRASH_SITE_CENTER);
                if (dist < crashSafeRadius)
                {
                    Debug.Log(string.Format("<color=yellow>[SAFE ZONE]</color> Di dời Zombie ({0:F1}m) ra mép rừng quanh xác máy bay!", dist));
                    // Di dời Zombie ra mép rừng (25m - 40m)
                    Vector3 farPos = CRASH_SITE_CENTER + new Vector3(Random.Range(25f, 40f) * (Random.value > 0.5f ? 1 : -1), 0, Random.Range(25f, 40f) * (Random.value > 0.5f ? 1 : -1));
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(farPos, out hit, 30f, NavMesh.AllAreas))
                    {
                        var agent = z.GetComponent<NavMeshAgent>();
                        if (agent != null) agent.Warp(hit.position);
                        else z.transform.position = hit.position;
                    }
                    else
                    {
                        Destroy(z.gameObject);
                    }
                }
            }
        }

        void Update()
        {
            // Trong suốt cutscene mở đầu: Không đẻ thêm zombie
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;

            // Khi người chơi đang ở trong hang động: Không đẻ zombie mặt đất quanh người chơi
            if (HorrorGame.Cave.CaveManager.IsPlayerInCave) return;

            FindPlayer();

            // Tính giới hạn Zombie thông minh theo Ngày & Đêm (Đêm 1 dễ thở hơn, từ Ngày 2 căng thẳng dần)
            int currentDay = 1;
            bool isNight = false;
            if (HorrorGame.Environment.DayNightCycle.Instance != null)
            {
                currentDay = HorrorGame.Environment.DayNightCycle.Instance.currentDay;
                isNight = HorrorGame.Environment.DayNightCycle.Instance.IsNight();
            }

            int dynamicMax = (currentDay <= 1)
                ? (isNight ? 15 : 10)
                : (isNight ? maxZombies : Mathf.Min(14, maxZombies));
            float dynamicInterval = isNight ? Mathf.Max(3.5f, spawnInterval) : Mathf.Max(6.0f, spawnInterval * 1.5f);

            // Sinh đợt Zombie ban đầu vừa phải (không gây giật lag)
            if (!initialSpawnDone)
            {
                initialSpawnDone = true;
                int count = Mathf.Min(initialHordeCount, dynamicMax);
                for (int i = 0; i < count; i++)
                {
                    SpawnZombie(i % 2 == 0);
                }
                Debug.Log(string.Format("ZombieSpawner: Đã kích hoạt {0} Zombie (Ngày {1} - {2}).", count, currentDay, isNight ? "Đêm" : "Ngày"));
            }

            // Dọn dẹp danh sách: Xóa những con Zombie đã bị bắn chết hoặc đã bị hủy
            activeZombies.RemoveAll(item => item == null || (item.GetComponent<EnemyAI>() != null && item.GetComponent<EnemyAI>().IsDead));

            // Luôn thu hồi zombie đi quá xa (> despawnDistance) để giữ CPU & FPS luôn mượt
            if (playerTransform != null)
            {
                for (int i = activeZombies.Count - 1; i >= 0; i--)
                {
                    if (activeZombies[i] == null) continue;
                    float dist = Vector3.Distance(activeZombies[i].transform.position, playerTransform.position);
                    if (dist > despawnDistance)
                    {
                        Destroy(activeZombies[i]);
                        activeZombies.RemoveAt(i);
                        break; // Thu hồi từ từ mỗi frame 1 con tránh spike
                    }
                }
            }

            if (activeZombies.Count >= dynamicMax) return;

            timer += Time.deltaTime;
            if (timer >= dynamicInterval)
            {
                spawnToggle++;
                bool nearPlayer = (spawnToggle % 3 != 0);
                SpawnZombie(nearPlayer);
                timer = 0f;
            }
        }

        private void SpawnZombie(bool preferNearPlayer = true)
        {
            if (zombiePrefab == null)
            {
                Debug.LogError("ZombieSpawner: Chưa gắn Zombie Prefab!");
                return;
            }

            FindPlayer();

            Vector3 finalPosition = Vector3.zero;
            bool foundValidPosition = false;

            // 1. Sinh quanh người chơi nếu được ưu tiên
            if (preferNearPlayer && spawnNearPlayer && playerTransform != null)
            {
                for (int attempt = 0; attempt < 16; attempt++)
                {
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float dist = Random.Range(minPlayerDistance, maxPlayerDistance);
                    Vector3 testPos = playerTransform.position + new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);

                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(testPos, out hit, 15f, NavMesh.AllAreas))
                    {
                        if (Vector3.Distance(hit.position, CRASH_SITE_CENTER) >= crashSafeRadius)
                        {
                            finalPosition = hit.position;
                            foundValidPosition = true;
                            break;
                        }
                    }
                }
            }

            // 2. Sinh rải đều khắp các khu vực khác trên đảo (làng, đầm lầy, cầu, ngã ba...)
            if (!foundValidPosition)
            {
                for (int attempt = 0; attempt < 12; attempt++)
                {
                    if (autoScatterOverMap && navMeshData.vertices.Length > 0)
                    {
                        int randomIndex = Random.Range(0, navMeshData.vertices.Length);
                        Vector3 randomPoint = navMeshData.vertices[randomIndex];

                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(randomPoint, out hit, 5f, NavMesh.AllAreas))
                        {
                            if (Vector3.Distance(hit.position, CRASH_SITE_CENTER) >= crashSafeRadius)
                            {
                                finalPosition = hit.position;
                                foundValidPosition = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        Vector3 spawnCenter = playerTransform != null ? playerTransform.position : transform.position;

                        if (spawnPoints != null && spawnPoints.Length > 0)
                        {
                            int randomIndex = Random.Range(0, spawnPoints.Length);
                            if (spawnPoints[randomIndex] != null)
                            {
                                spawnCenter = spawnPoints[randomIndex].position;
                            }
                        }

                        Vector3 randomDirection = Random.insideUnitSphere * spawnRadius;
                        randomDirection += spawnCenter;
                        
                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(randomDirection, out hit, spawnRadius, NavMesh.AllAreas))
                        {
                            if (Vector3.Distance(hit.position, CRASH_SITE_CENTER) >= crashSafeRadius)
                            {
                                finalPosition = hit.position;
                                foundValidPosition = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (foundValidPosition)
            {
                GameObject newZombie = Instantiate(zombiePrefab, finalPosition, Quaternion.identity);
                newZombie.SetActive(true);
                Animator zAnim = newZombie.GetComponent<Animator>();
                if (zAnim == null) zAnim = newZombie.GetComponentInChildren<Animator>();
                if (zAnim != null)
                {
                    zAnim.enabled = true;
                    zAnim.applyRootMotion = false;
                }
                EnemyAI ai = newZombie.GetComponent<EnemyAI>();
                if (ai != null) ai.EnsureCollider();
                activeZombies.Add(newZombie);
                float distToPl = playerTransform != null ? Vector3.Distance(finalPosition, playerTransform.position) : 0f;
                Debug.Log(string.Format("ZombieSpawner: Đã spawn 1 con Zombie tại {0} (cách người chơi {1:F1}m)", finalPosition, distToPl));
            }
            else
            {
                Debug.LogWarning("ZombieSpawner: Không tìm thấy vị trí hợp lệ ngoài vùng an toàn để đẻ Zombie!");
            }
        }

        // Vẽ vòng tròn để dễ mường tượng phạm vi đẻ Zombie trong màn hình Scene (nếu dùng code cũ)
        private void OnDrawGizmosSelected()
        {
            if (autoScatterOverMap) return; // Nếu đang tự rải toàn map thì không cần vẽ vòng tròn

            Gizmos.color = Color.green;

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                foreach (Transform pt in spawnPoints)
                {
                    if (pt != null)
                    {
                        Gizmos.DrawWireSphere(pt.position, spawnRadius);
                    }
                }
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, spawnRadius);
            }
        }
    }
}

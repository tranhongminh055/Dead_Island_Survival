using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace HorrorGame.Enemy
{
    public class ZombieSpawner : MonoBehaviour
    {
        [Header("Spawner Settings")]
        public GameObject zombiePrefab; // Kéo thả khối Zombie Prefab vào đây
        public int maxZombies = 80; // Số lượng tối đa trên đảo (tăng lên 80 con đông nghẹt!)
        public float spawnInterval = 1.0f; // Cứ mỗi 1 giây sinh 1 con
        public float spawnRadius = 60f; // Bán kính khu vực đẻ ngẫu nhiên nếu không dùng Spawn Points

        [Header("Spawn Points (Khắp Map)")]
        public Transform[] spawnPoints; // Danh sách các điểm đẻ Zombie (Tạo Empty Object rồi kéo vào đây)
        public bool autoScatterOverMap = true; // Rải Zombie khắp các khu vực trên bản đồ

        [Header("Dynamic Player Spawner (Sinh Zombie quanh người chơi)")]
        [Tooltip("Bật chế độ sinh Zombie xung quanh người chơi để luôn chạm trán Zombie")]
        public bool spawnNearPlayer = true;
        public float minPlayerDistance = 15f; // Xuất hiện từ cự ly 15m (ngay bìa cây)
        public float maxPlayerDistance = 55f; // Vây quanh tới 55m
        public float despawnDistance = 140f;  // Đi quá 140m sẽ luân chuyển lại gần người chơi
        public int initialHordeCount = 30;    // Sinh ngay 30 con khi bắt đầu game!

        [Header("Crash Site Safe Zone (Vùng an toàn quanh xác máy bay)")]
        public static readonly Vector3 CRASH_SITE_CENTER = new Vector3(537f, 17.6f, 545f);
        public float crashSafeRadius = 10f; // Chỉ 10m ngay trên đám lửa máy bay!

        private float timer = 0f;
        private List<GameObject> activeZombies = new List<GameObject>();
        private NavMeshTriangulation navMeshData;
        private Transform playerTransform;
        private bool initialSpawnDone = false;
        private int spawnToggle = 0; // Luân phiên: 1 con quanh player, 1 con ở nơi khác trên map

        void Awake()
        {
            // Tối ưu giới hạn để đảo tràn ngập Zombie
            if (crashSafeRadius > 15f) crashSafeRadius = 10f;
            if (maxZombies < 60) maxZombies = 80;
            if (spawnInterval > 2f) spawnInterval = 1.0f;
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

            // Dọn sạch Zombie quá sát xác máy bay (< 20m) đưa ra mép rừng
            ClearZombiesNearCrashSite();
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

            FindPlayer();

            // Sinh đợt Zombie hùng hậu ban đầu (30 con) ngay khi cutscene kết thúc để chỗ nào cũng có Zombie
            if (!initialSpawnDone)
            {
                initialSpawnDone = true;
                int count = Mathf.Min(initialHordeCount, maxZombies);
                for (int i = 0; i < count; i++)
                {
                    // Luân phiên: một nửa quanh người chơi, một nửa rải khắp bản đồ
                    SpawnZombie(i % 2 == 0);
                }
                Debug.Log(string.Format("ZombieSpawner: Đã kích hoạt binh đoàn Zombie ({0} con) trên khắp hòn đảo!", count));
            }

            // Dọn dẹp danh sách: Xóa những con Zombie đã bị bắn chết
            activeZombies.RemoveAll(item => item == null);

            // Chỉ thu hồi zombie ở quá xa (> 140m) KHI ĐÃ ĐẠT GẦN MAX để nhường slot sinh gần người chơi
            if (playerTransform != null && activeZombies.Count >= maxZombies - 5)
            {
                for (int i = activeZombies.Count - 1; i >= 0; i--)
                {
                    if (activeZombies[i] == null) continue;
                    float dist = Vector3.Distance(activeZombies[i].transform.position, playerTransform.position);
                    if (dist > despawnDistance)
                    {
                        Destroy(activeZombies[i]);
                        activeZombies.RemoveAt(i);
                        break; // Giải phóng từ từ
                    }
                }
            }

            // Nếu đảo đã quá đông (đạt giới hạn 80 con) thì chờ
            if (activeZombies.Count >= maxZombies) return;

            // Đếm ngược thời gian đẻ (1 giây 1 con)
            timer += Time.deltaTime;
            if (timer >= spawnInterval)
            {
                spawnToggle++;
                // 2 con quanh người chơi, 1 con rải xa khắp map
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

using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace HorrorGame.Enemy
{
    public class ZombieSpawner : MonoBehaviour
    {
        [Header("Spawner Settings")]
        public GameObject zombiePrefab; // Kéo thả khối Zombie Prefab vào đây
        public int maxZombies = 20; // Số lượng tối đa trên đảo
        public float spawnInterval = 5f; // Cứ mỗi 5 giây đẻ 1 con
        public float spawnRadius = 50f; // Bán kính khu vực đẻ ngẫu nhiên nếu không dùng Spawn Points

        [Header("Spawn Points (Khắp Map)")]
        public Transform[] spawnPoints; // Danh sách các điểm đẻ Zombie (Tạo Empty Object rồi kéo vào đây)

        [Header("Tự động rải đều toàn map")]
        public bool autoScatterOverMap = true; // Bật cái này lên để tự động rải

        [Header("Crash Site Safe Zone (Vùng an toàn quanh xác máy bay)")]
        public static readonly Vector3 CRASH_SITE_CENTER = new Vector3(537f, 17.6f, 545f);
        public float crashSafeRadius = 120f; // Bán kính 120m quanh xác máy bay tuyệt đối không có Zombie!

        private float timer = 0f;
        private List<GameObject> activeZombies = new List<GameObject>();
        private NavMeshTriangulation navMeshData;

        void Start()
        {
            // Lấy toàn bộ mạng lưới mặt đất đã nướng (Bake) của bản đồ
            navMeshData = NavMesh.CalculateTriangulation();
            
            if (navMeshData.vertices.Length == 0)
            {
                Debug.LogError("ZombieSpawner: Không tìm thấy dữ liệu NavMesh! Bạn phải Bake NavMesh cho mặt đất trước.");
            }

            // Dọn sạch mọi Zombie có sẵn trong Scene đang ở trong bán kính 120m quanh xác máy bay
            ClearZombiesNearCrashSite();
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
                    Debug.Log(string.Format("<color=yellow>[SAFE ZONE]</color> Di dời Zombie ({0:F1}m) ra khỏi khu vực xác máy bay!", dist));
                    // Di dời Zombie ra xa ít nhất 150m trên NavMesh
                    Vector3 farPos = CRASH_SITE_CENTER + new Vector3(Random.Range(140f, 200f) * (Random.value > 0.5f ? 1 : -1), 0, Random.Range(140f, 200f) * (Random.value > 0.5f ? 1 : -1));
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

            // Dọn dẹp danh sách: Xóa những con Zombie đã bị bắn chết (bị Destroy)
            activeZombies.RemoveAll(item => item == null);

            // Nếu đảo đã quá đông (đạt giới hạn) thì ngừng đẻ
            if (activeZombies.Count >= maxZombies) return;

            // Đếm ngược thời gian đẻ
            timer += Time.deltaTime;
            if (timer >= spawnInterval)
            {
                SpawnZombie();
                timer = 0f;
            }
        }

        private void SpawnZombie()
        {
            if (zombiePrefab == null)
            {
                Debug.LogError("ZombieSpawner: Chưa gắn Zombie Prefab!");
                return;
            }

            Vector3 finalPosition = Vector3.zero;
            bool foundValidPosition = false;

            // Thử tối đa 12 lần để tìm vị trí nằm NGOÀI vùng an toàn máy bay rơi (cách xa > 120m)
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
                    Vector3 spawnCenter = transform.position;

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

            if (foundValidPosition)
            {
                GameObject newZombie = Instantiate(zombiePrefab, finalPosition, Quaternion.identity);
                activeZombies.Add(newZombie);
                Debug.Log("ZombieSpawner: Đã spawn 1 con Zombie tại " + finalPosition);
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

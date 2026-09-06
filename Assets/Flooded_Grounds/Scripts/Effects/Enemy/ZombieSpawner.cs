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
        }

        void Update()
        {
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

            if (autoScatterOverMap && navMeshData.vertices.Length > 0)
            {
                // Cách 1: Tự động rải đều ngẫu nhiên toàn map dựa trên dữ liệu NavMesh
                int randomIndex = Random.Range(0, navMeshData.vertices.Length);
                Vector3 randomPoint = navMeshData.vertices[randomIndex];

                NavMeshHit hit;
                // Kiểm tra lại lần cuối xem điểm đó có chắc chắn nằm trên mặt đất không
                if (NavMesh.SamplePosition(randomPoint, out hit, 5f, NavMesh.AllAreas))
                {
                    finalPosition = hit.position;
                    foundValidPosition = true;
                }
            }
            else
            {
                // Cách 2: Code cũ (dùng Spawn Points hoặc bắn quanh tâm) nếu tắt autoScatterOverMap
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
                    finalPosition = hit.position;
                    foundValidPosition = true;
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
                Debug.LogWarning("ZombieSpawner: Không tìm thấy mặt đất để đẻ Zombie!");
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

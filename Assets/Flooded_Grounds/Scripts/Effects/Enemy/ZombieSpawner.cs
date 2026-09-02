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

        private float timer = 0f;
        private List<GameObject> activeZombies = new List<GameObject>();

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
                Vector3 finalPosition = hit.position;
                GameObject newZombie = Instantiate(zombiePrefab, finalPosition, Quaternion.identity);
                activeZombies.Add(newZombie);
                Debug.Log("ZombieSpawner: Đã spawn 1 con Zombie tại " + finalPosition);
            }
            else
            {
                Debug.LogWarning("ZombieSpawner: Không tìm thấy mặt đất (NavMesh) để đẻ Zombie! Bạn đã Bake NavMesh chưa?");
            }
        }

        // Vẽ vòng tròn để dễ mường tượng phạm vi đẻ Zombie trong màn hình Scene
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                // Vẽ vòng tròn ở các điểm Spawn Points
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
                // Vẽ vòng tròn ở vị trí gốc nếu không dùng Spawn Points
                Gizmos.DrawWireSphere(transform.position, spawnRadius);
            }
        }
    }
}

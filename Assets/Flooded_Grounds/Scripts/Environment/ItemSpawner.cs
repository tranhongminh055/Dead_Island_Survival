using UnityEngine;
using System.Collections.Generic;

namespace HorrorGame.Environment
{
    public class ItemSpawner : MonoBehaviour
    {
        [Header("Spawning Settings")]
        public GameObject chestPrefab;
        public int numberOfChestsToSpawn = 5;
        [Tooltip("Kích thước của rương (Thay đổi nếu nó quá to hoặc quá nhỏ)")]
        public float chestScale = 0.05f;
        
        [Header("Spawn Points")]
        public List<Transform> spawnPoints;

        private void Start()
        {
            SpawnChests();
        }

        [Header("Ground Detection")]
        public LayerMask groundLayer = ~0; // Default to all layers
        public float raycastHeight = 50f; // Height to shoot raycast from
        
        private void SpawnChests()
        {
            if (chestPrefab == null || spawnPoints == null || spawnPoints.Count == 0)
            {
                Debug.LogWarning("ItemSpawner: Missing prefab or spawn points.");
                return;
            }

            // Shuffle spawn points (simple Fisher-Yates shuffle)
            List<Transform> availablePoints = new List<Transform>(spawnPoints);
            for (int i = 0; i < availablePoints.Count; i++)
            {
                int randomIndex = Random.Range(i, availablePoints.Count);
                Transform temp = availablePoints[i];
                availablePoints[i] = availablePoints[randomIndex];
                availablePoints[randomIndex] = temp;
            }

            int spawnCount = Mathf.Min(numberOfChestsToSpawn, availablePoints.Count);

            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 spawnPosition = availablePoints[i].position;
                
                // Thêm một chút random vị trí để các rương không bị dính chùm vào nhau tạo thành cái tháp
                Vector3 randomOffset = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
                Vector3 rayStart = spawnPosition + randomOffset + Vector3.up * raycastHeight;
                RaycastHit hit;
                
                if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastHeight * 2, groundLayer, QueryTriggerInteraction.Ignore))
                {
                    spawnPosition = hit.point;
                }
                else
                {
                    Debug.LogWarning("ItemSpawner: Could not find ground for spawn point " + availablePoints[i].name);
                }

                // Lấy góc quay của điểm Spawn, VÀ xoay toàn bộ rương -90 độ trục X
                // Cách này xoay CẢ rương (bao gồm cả xương và lưới) nên sẽ không bao giờ bị biến dạng!
                Quaternion spawnRot = availablePoints[i].rotation * Quaternion.Euler(-90f, 0f, 0f);
                GameObject chest = Instantiate(chestPrefab, spawnPosition, spawnRot) as GameObject;
                
                // Ép kích thước nhỏ lại ngay bằng code để chắc chắn 100% không bị to
                chest.transform.localScale = new Vector3(chestScale, chestScale, chestScale);
            }
            Debug.Log("Spawned " + spawnCount + " chests across the map.");
        }
    }
}

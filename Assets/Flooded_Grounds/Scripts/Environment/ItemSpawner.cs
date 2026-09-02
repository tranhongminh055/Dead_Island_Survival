using UnityEngine;
using System.Collections.Generic;

namespace HorrorGame.Environment
{
    public class ItemSpawner : MonoBehaviour
    {
        [Header("Spawning Settings")]
        public GameObject chestPrefab;
        public int numberOfChestsToSpawn = 5;
        
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
                
                // Shoot a raycast downwards from high up to find the exact ground position
                Vector3 rayStart = spawnPosition + Vector3.up * raycastHeight;
                RaycastHit hit;
                
                if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastHeight * 2, groundLayer, QueryTriggerInteraction.Ignore))
                {
                    spawnPosition = hit.point;
                }
                else
                {
                    Debug.LogWarning("ItemSpawner: Could not find ground for spawn point " + availablePoints[i].name);
                }

                // Spawn rương
                GameObject chest = Instantiate(chestPrefab, spawnPosition, availablePoints[i].rotation) as GameObject;
            }
            Debug.Log("Spawned " + spawnCount + " chests across the map.");
        }
    }
}

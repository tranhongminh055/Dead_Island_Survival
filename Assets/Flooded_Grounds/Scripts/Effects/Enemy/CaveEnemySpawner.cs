using UnityEngine;
using System.Collections.Generic;
using HorrorGame.Cave;

namespace HorrorGame.Enemy
{
    /// <summary>
    /// CaveEnemySpawner — Trình quản lý sinh quái vật riêng cho từng hang động.
    /// - Khi người chơi bước vào hang: Sinh ra các loại quái vật đặc thù (Cave Zombie, Shaman, Drowned, Magma).
    /// - Khi người chơi rời khỏi hang: Tắt / dọn dẹp các quái vật còn sống để tiết kiệm CPU và NavMesh.
    /// - Lưu trữ danh sách kẻ thù đã bị tiêu diệt (không hồi sinh lại ngay khi ra vào hang liên tục).
    /// </summary>
    public class CaveEnemySpawner : MonoBehaviour
    {
        [System.Serializable]
        public class EnemySpawnConfig
        {
            public string enemyName = "Cave Zombie";
            public GameObject enemyPrefab;
            public Transform spawnPoint;
            [Tooltip("Thời gian hồi sinh lại sau khi chết (giây, 0 = không hồi sinh)")]
            public float respawnDelay = 600f; // 10 phút
            [HideInInspector] public GameObject spawnedInstance;
            [HideInInspector] public bool isDead = false;
            [HideInInspector] public float deathTime = 0f;
        }

        [Header("── Danh Sách Điểm Sinh Quái ──")]
        public List<EnemySpawnConfig> spawnConfigs = new List<EnemySpawnConfig>();

        [Header("── Thuộc CaveZone ──")]
        public CaveZone parentCaveZone;

        private void Start()
        {
            if (parentCaveZone == null)
            {
                parentCaveZone = GetComponentInParent<CaveZone>();
            }

            // Nếu hang này đang mở sẵn khi bắt đầu, spawn ngay
            if (parentCaveZone != null && CaveManager.CurrentCave == parentCaveZone)
            {
                SpawnAllCaveEnemies();
            }
        }

        /// <summary>
        /// Gọi khi người chơi bước vào hang động này
        /// </summary>
        public void SpawnAllCaveEnemies()
        {
            for (int i = 0; i < spawnConfigs.Count; i++)
            {
                EnemySpawnConfig config = spawnConfigs[i];

                // Nếu đã chết, kiểm tra xem đã hết thời gian hồi sinh chưa
                if (config.isDead)
                {
                    if (config.respawnDelay > 0 && Time.time >= config.deathTime + config.respawnDelay)
                    {
                        config.isDead = false;
                    }
                    else
                    {
                        continue; // Vẫn đang chết
                    }
                }

                // Nếu đã có instance còn sống, bật lại
                if (config.spawnedInstance != null)
                {
                    config.spawnedInstance.SetActive(true);
                    continue;
                }

                // Nếu chưa có, instantiate mới
                if (config.enemyPrefab != null && config.spawnPoint != null)
                {
                    GameObject obj = Instantiate(config.enemyPrefab, config.spawnPoint.position, config.spawnPoint.rotation);
                    obj.transform.SetParent(transform);
                    config.spawnedInstance = obj;

                    // Lắng nghe sự kiện chết
                    EnemyAI ai = obj.GetComponent<EnemyAI>();
                    if (ai != null)
                    {
                        int index = i;
                        StartCoroutine(MonitorEnemyDeath(ai, index));
                    }
                }
            }

            Debug.Log(string.Format("<color=green>[CaveEnemySpawner]</color> Đã kích hoạt quái vật trong {0}", gameObject.name));
        }

        private System.Collections.IEnumerator MonitorEnemyDeath(EnemyAI enemy, int configIndex)
        {
            while (enemy != null && !enemy.IsDead)
            {
                yield return new WaitForSeconds(1.0f);
            }

            if (configIndex >= 0 && configIndex < spawnConfigs.Count)
            {
                spawnConfigs[configIndex].isDead = true;
                spawnConfigs[configIndex].deathTime = Time.time;
            }
        }

        /// <summary>
        /// Gọi khi người chơi rời khỏi hang động: Tắt quái vật để tối ưu hiệu năng
        /// </summary>
        public void DespawnAllCaveEnemies()
        {
            foreach (var config in spawnConfigs)
            {
                if (config.spawnedInstance != null && !config.isDead)
                {
                    config.spawnedInstance.SetActive(false);
                }
            }

            Debug.Log(string.Format("<color=yellow>[CaveEnemySpawner]</color> Đã tạm ẩn quái vật trong {0} khi player rời hang.", gameObject.name));
        }
    }
}

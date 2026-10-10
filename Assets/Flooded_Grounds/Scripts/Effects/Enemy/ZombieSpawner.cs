using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace HorrorGame.Enemy
{
    [System.Serializable]
    public class ZombieZone
    {
        public string zoneName;
        public Vector3 center;
        public List<GameObject> activeZombies = new List<GameObject>();
        public float timer = 0f;
        public bool isPlayerInside = false;
        
        public ZombieZone(string name, Vector3 pos)
        {
            zoneName = name;
            center = pos;
        }
    }

    public class ZombieSpawner : MonoBehaviour
    {
        [Header("Spawner Settings")]
        public GameObject zombiePrefab; // Kéo thả khối Zombie Prefab vào đây
        public Transform player; // Tự động tìm nếu để trống
        
        [Header("Vùng kích hoạt & Tối ưu (The Forest Style)")]
        public float activationRange = 150f; // Khoảng cách đánh thức Zombie
        
        [Header("Cấu hình ngày/đêm")]
        public int maxZombiesDay = 50; 
        public int maxZombiesNight = 120;
        public float spawnIntervalDay = 4f; 
        public float spawnIntervalNight = 1f; 
        public float spawnRadius = 80f; // Bán kính đẻ của mỗi cứ điểm
        
        public static readonly Vector3 CRASH_SITE_CENTER = new Vector3(537f, 17.6f, 545f);
        public float crashSafeRadius = 15f;

        [Header("Danh sách Cứ Điểm (Zones)")]
        public List<ZombieZone> zones = new List<ZombieZone>();

        // Zone di động theo người chơi — luôn đẻ Zombie xung quanh Player
        private ZombieZone playerFollowZone;
        private float playerFollowZoneUpdateTimer = 0f;

        /// <summary>
        /// Tự động gắn ZombieSpawner vào Scene khi game chạy (không cần kéo thả thủ công)
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            // Tránh tạo trùng lặp
            if (FindObjectOfType<ZombieSpawner>() != null) return;

            GameObject spawnerObj = new GameObject("[ZombieSpawner_Auto]");
            spawnerObj.AddComponent<ZombieSpawner>();
            DontDestroyOnLoad(spawnerObj);
            Debug.Log("<color=lime>[ZombieSpawner] Tự động tạo ZombieSpawner thành công!</color>");
        }

        void Start()
        {
            FindPlayer();

            // Tự động tạo Zombie Prefab nếu chưa có ai kéo thả vào Inspector
            if (zombiePrefab == null)
            {
                AutoCreateZombiePrefab();
            }

            // Tự động tạo các Cứ Điểm bao quanh bản đồ nếu chưa có
            if (zones.Count == 0)
            {
                CreateDefaultCamps();
            }

            // Tạo zone di động bám theo người chơi
            if (player != null)
            {
                playerFollowZone = new ZombieZone("Khu Vuc Xung Quanh Player", player.position);
            }

            // Dọn sạch Zombie quá sát xác máy bay đưa ra mép rừng
            ClearZombiesNearCrashSite();

            // Sinh ngay vài con ban đầu xung quanh người chơi
            if (playerFollowZone != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    SpawnZombieInZone(playerFollowZone);
                }
            }
            
            Debug.Log("<color=cyan>[ZombieSpawner] Khởi tạo xong! Zombie Prefab: " + (zombiePrefab != null ? "OK" : "THIẾU") + " | Player: " + (player != null ? "OK" : "THIẾU") + " | Zones: " + zones.Count + "</color>");
        }

        /// <summary>
        /// Tự động nạp model Zombie từ file FBX gốc và gắn đầy đủ Component
        /// </summary>
        private void AutoCreateZombiePrefab()
        {
#if UNITY_EDITOR
            // Nạp model FBX zombie
            GameObject zombieModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Flooded_Grounds/Character No Animation/Enemy No Animation/Zombiegirl W Kurniawan.fbx");
            
            if (zombieModel == null)
            {
                Debug.LogError("[ZombieSpawner] Không tìm thấy file Zombiegirl W Kurniawan.fbx!");
                return;
            }

            // Nạp Animator Controller
            RuntimeAnimatorController animCtrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Flooded_Grounds/Character No Animation/Enemy No Animation/Animation zoombie/ZombieAnim.controller");

            // Tạo một bản sao tạm thời làm Prefab mẫu (ẩn, không hiện trong Scene)
            zombiePrefab = Instantiate(zombieModel);
            zombiePrefab.name = "ZombiePrefab_AutoCreated";
            zombiePrefab.SetActive(false);

            // Gắn Animator + Controller
            Animator anim = zombiePrefab.GetComponent<Animator>();
            if (anim == null) anim = zombiePrefab.AddComponent<Animator>();
            if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;

            // Gắn NavMeshAgent
            NavMeshAgent agent = zombiePrefab.GetComponent<NavMeshAgent>();
            if (agent == null) agent = zombiePrefab.AddComponent<NavMeshAgent>();
            agent.speed = 2.5f;
            agent.angularSpeed = 360f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 1.5f;
            agent.radius = 0.4f;
            agent.height = 1.8f;

            // Gắn Capsule Collider
            CapsuleCollider capsule = zombiePrefab.GetComponent<CapsuleCollider>();
            if (capsule == null) capsule = zombiePrefab.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0, 0.9f, 0);
            capsule.radius = 0.35f;
            capsule.height = 1.8f;

            // Gắn EnemyAI
            EnemyAI ai = zombiePrefab.GetComponent<EnemyAI>();
            if (ai == null) ai = zombiePrefab.AddComponent<EnemyAI>();

            DontDestroyOnLoad(zombiePrefab);

            Debug.Log("<color=cyan>[ZombieSpawner] Đã tự động tạo Zombie Prefab từ FBX thành công!</color>");
#else
            Debug.LogError("[ZombieSpawner] Cần gán Zombie Prefab trong Inspector trước khi Build game!");
#endif
        }

        private void CreateDefaultCamps()
        {
            // Cứ điểm GẦN crash site (người chơi sẽ gặp zombie ngay khi bắt đầu chơi)
            zones.Add(new ZombieZone("Khu Vuc Xac May Bay", CRASH_SITE_CENTER + new Vector3(60, 0, 60)));
            zones.Add(new ZombieZone("Rung Gan Crash", CRASH_SITE_CENTER + new Vector3(-80, 0, 40)));
            zones.Add(new ZombieZone("Lang Mac Gan", CRASH_SITE_CENTER + new Vector3(40, 0, -70)));

            // Cứ điểm TRUNG BÌNH (100-200m)
            zones.Add(new ZombieZone("Khu Rung Phia Bac", CRASH_SITE_CENTER + new Vector3(0, 0, 180)));
            zones.Add(new ZombieZone("Khu Vuc Phia Nam", CRASH_SITE_CENTER + new Vector3(0, 0, -180)));
            zones.Add(new ZombieZone("Doanh Trai Phia Dong", CRASH_SITE_CENTER + new Vector3(180, 0, 0)));
            zones.Add(new ZombieZone("Bai Doc Phia Tay", CRASH_SITE_CENTER + new Vector3(-180, 0, 0)));

            // Cứ điểm XA (300m) cho người chơi thám hiểm sâu
            zones.Add(new ZombieZone("Khu Dat Can Coi", CRASH_SITE_CENTER + new Vector3(300, 0, 300)));
            zones.Add(new ZombieZone("Vung Dat Hoang", CRASH_SITE_CENTER + new Vector3(-300, 0, -300)));
            
            Debug.Log("<color=lime>[ZombieSpawner] Da tu dong tao " + zones.Count + " cu diem (Zones) xung quanh map.</color>");
        }

        private Transform FindPlayer()
        {
            if (player != null) return player;
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else if (Camera.main != null) player = Camera.main.transform;
            return player;
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
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;
            if (player == null)
            {
                FindPlayer();
                if (player == null) return;
            }
            if (zombiePrefab == null) return;

            bool isNight = false;
            if (HorrorGame.Environment.DayNightCycle.Instance != null)
            {
                isNight = HorrorGame.Environment.DayNightCycle.Instance.IsNight();
            }

            int currentMaxZombies = isNight ? maxZombiesNight : maxZombiesDay;
            float currentSpawnInterval = isNight ? spawnIntervalNight : spawnIntervalDay;

            // === ZONE DI ĐỘNG BÁM THEO PLAYER ===
            // Luôn đẻ zombie quanh người chơi bất kể đang ở đâu trên bản đồ
            if (playerFollowZone != null)
            {
                // Cập nhật vị trí zone theo player mỗi 5 giây
                playerFollowZoneUpdateTimer += Time.deltaTime;
                if (playerFollowZoneUpdateTimer >= 5f)
                {
                    playerFollowZone.center = player.position;
                    playerFollowZoneUpdateTimer = 0f;
                }

                playerFollowZone.activeZombies.RemoveAll(item => item == null);

                if (playerFollowZone.activeZombies.Count < currentMaxZombies)
                {
                    playerFollowZone.timer += Time.deltaTime;
                    if (playerFollowZone.timer >= currentSpawnInterval)
                    {
                        SpawnZombieInZone(playerFollowZone);
                        playerFollowZone.timer = 0f;
                    }
                }

                // Đóng băng zombie ở xa người chơi (> 150m)
                for (int i = playerFollowZone.activeZombies.Count - 1; i >= 0; i--)
                {
                    GameObject z = playerFollowZone.activeZombies[i];
                    if (z == null) continue;
                    float dist = Vector3.Distance(z.transform.position, player.position);
                    if (dist > activationRange)
                    {
                        PauseZombie(z);
                    }
                    else
                    {
                        ResumeZombie(z);
                    }
                }
            }

            // === CÁC ZONE CỐ ĐỊNH ===
            foreach (var zone in zones)
            {
                // Dọn dẹp danh sách những Zombie đã chết
                zone.activeZombies.RemoveAll(item => item == null);

                // Tính khoảng cách từ người chơi tới tâm cứ điểm
                float distToPlayer = Vector3.Distance(player.position, zone.center);
                bool wasInside = zone.isPlayerInside;
                bool isInsideNow = distToPlayer <= activationRange;
                
                zone.isPlayerInside = isInsideNow;

                // Nếu người chơi VỪA bước vào vùng kích hoạt -> Đánh thức toàn bộ zombie
                if (!wasInside && isInsideNow)
                {
                    Debug.Log("<color=green>[Kích hoạt]</color> Bước vào " + zone.zoneName + ". Đánh thức Zombie!");
                    foreach(var z in zone.activeZombies)
                    {
                        ResumeZombie(z);
                    }
                }
                // Nếu người chơi VỪA rời khỏi vùng kích hoạt -> Bắt zombie đứng yên
                else if (wasInside && !isInsideNow)
                {
                    Debug.Log("<color=gray>[Ngủ đông]</color> Đã rời xa " + zone.zoneName + ". Zombie sẽ đứng yên chờ bạn.");
                    foreach(var z in zone.activeZombies)
                    {
                        PauseZombie(z);
                    }
                }

                // Nếu người chơi ĐANG ở trong vùng -> Đẻ thêm Zombie nếu chưa đủ
                if (isInsideNow)
                {
                    if (zone.activeZombies.Count < currentMaxZombies)
                    {
                        zone.timer += Time.deltaTime;
                        if (zone.timer >= currentSpawnInterval)
                        {
                            SpawnZombieInZone(zone);
                            zone.timer = 0f;
                        }
                    }
                }
            }
        }

        private void SpawnZombieInZone(ZombieZone zone)
        {
            if (zombiePrefab == null) return;
            FindPlayer();

            Vector3 finalPosition = Vector3.zero;
            bool foundValidPosition = false;

            // Thử 15 lần tìm vị trí ngẫu nhiên trên NavMesh quanh tâm của khu vực
            for (int attempt = 0; attempt < 15; attempt++)
            {
                Vector3 randomDirection = Random.insideUnitSphere * spawnRadius;
                randomDirection += zone.center;
                
                NavMeshHit hit;
                // SamplePosition trên khoảng cách 20m tính từ điểm ngẫu nhiên để rơi xuống mặt đất
                if (NavMesh.SamplePosition(randomDirection, out hit, 20f, NavMesh.AllAreas))
                {
                    // Tránh vùng an toàn máy bay
                    if (Vector3.Distance(hit.position, CRASH_SITE_CENTER) >= crashSafeRadius)
                    {
                        // Không đẻ quá sát mặt người chơi để tránh bị lộ (cách > 15m nếu có player)
                        if (player == null || Vector3.Distance(hit.position, player.position) > 15f)
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
                newZombie.SetActive(true); // Bật lên vì prefab mẫu đang bị ẩn (SetActive false)
                
                EnemyAI ai = newZombie.GetComponent<EnemyAI>();
                if (ai != null) ai.EnsureCollider();

                zone.activeZombies.Add(newZombie);
            }
        }

        private void PauseZombie(GameObject z)
        {
            if (z == null) return;
            var agent = z.GetComponent<NavMeshAgent>();
            var anim = z.GetComponent<Animator>();
            var ai = z.GetComponent<EnemyAI>();
            
            // Dừng AI và đóng băng Animation
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            if (anim != null) anim.enabled = false;
            if (ai != null) ai.enabled = false;
        }

        private void ResumeZombie(GameObject z)
        {
            if (z == null) return;
            var agent = z.GetComponent<NavMeshAgent>();
            var anim = z.GetComponent<Animator>();
            var ai = z.GetComponent<EnemyAI>();
            
            // Tiếp tục AI và Animation
            if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
            if (anim != null) anim.enabled = true;
            if (ai != null) ai.enabled = true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            foreach (var zone in zones)
            {
                // Vòng vàng là vùng kích hoạt (Player bước vào thì Zombie thức giấc)
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(zone.center, activationRange);
                
                // Vòng xanh là vùng Zombies đẻ ra bên trong cứ điểm
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(zone.center, spawnRadius);
            }
        }
    }
}

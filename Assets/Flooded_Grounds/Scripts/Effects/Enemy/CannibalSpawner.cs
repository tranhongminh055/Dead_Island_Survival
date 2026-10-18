using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace HorrorGame.Enemy
{
    /// <summary>
    /// Hệ thống Quản Lý & Sinh Thổ Dân Ăn Thịt Người (Cannibal Spawner):
    /// - Duy trì các nhóm thổ dân rình rập và cầm đuốc đi săn trong rừng.
    /// - Tự động kích hoạt khi vào game, đảm bảo luôn có thổ dân xuất hiện quanh người chơi.
    /// </summary>
    public class CannibalSpawner : MonoBehaviour
    {
        [Header("── Thiết Lập Thổ Dân ──")]
        public GameObject cannibalPrefab; // Prefab thổ dân riêng nếu có
        public int maxCannibals = 10;      // Duy trì khoảng 10 thổ dân trên đảo
        public float spawnInterval = 6f;   // Cứ 6s kiểm tra sinh thêm nếu thiếu
        public float minSpawnDistFromPlayer = 18f; // Xuất hiện từ cự ly 18m
        public float maxSpawnDistFromPlayer = 38f; // Vây quanh trong cự ly 38m

        [Header("── Âm Thanh Tiếng Hú & Gầm ──")]
        public AudioClip warCryClip;
        public AudioClip howlClip;
        public AudioClip growlClip;

        private float timer = 0f;
        private List<GameObject> activeCannibals = new List<GameObject>();
        private Transform playerTransform;
        private bool initialSpawnDone = false;

        // Tự động khởi chạy ngay khi bất kỳ Scene chơi nào được nạp (trừ MainMenu)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName.ToLower().Contains("mainmenu")) return;

            if (FindObjectOfType<CannibalSpawner>() == null)
            {
                GameObject spawnerObj = new GameObject("[CannibalSpawner]");
                spawnerObj.AddComponent<CannibalSpawner>();
                Debug.Log("🏹 [CannibalSpawner] Đã tự động kích hoạt bầy Thổ Dân Ăn Thịt Người cho Scene: " + sceneName);
            }
        }

        void Start()
        {
            FindPlayer();
            ResolvePrefab();
            LoadAudioClips();
        }

        private void ResolvePrefab()
        {
            if (cannibalPrefab == null)
            {
                ZombieSpawner zs = FindObjectOfType<ZombieSpawner>();
                if (zs != null && zs.zombiePrefab != null)
                {
                    cannibalPrefab = zs.zombiePrefab;
                }
            }

            if (cannibalPrefab == null)
            {
                EnemyAI found = FindObjectOfType<EnemyAI>();
                if (found != null) cannibalPrefab = found.gameObject;
            }
        }

        private void LoadAudioClips()
        {
#if UNITY_EDITOR
            if (warCryClip == null)
            {
                warCryClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/tiếng sói hú/ribhavagrawal-wolf-howling-type-04-293317.mp3");
            }
#endif
            if (warCryClip == null) warCryClip = Resources.Load<AudioClip>("Sounds/WolfHowl");
            if (howlClip == null) howlClip = warCryClip;
        }

        void Update()
        {
            // Trong Cutscene máy bay: Không sinh thổ dân
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;

            // Khi người chơi đang ở trong hang động: Không sinh thổ dân mặt đất
            if (HorrorGame.Cave.CaveManager.IsPlayerInCave) return;

            FindPlayer();
            ResolvePrefab();

            // Kiểm tra ban ngày / ban đêm để điều chỉnh số lượng & tốc độ spawn
            bool isNightTime = true;
            if (HorrorGame.Environment.DayNightCycle.Instance != null)
            {
                isNightTime = HorrorGame.Environment.DayNightCycle.Instance.IsNight();
            }

            // Ban ngày: Giảm mạnh số lượng thổ dân (tối đa 3, spawn chậm hơn)
            // Ban đêm: Bầy đàn đông đúc hung hãn (tối đa 10, spawn nhanh)
            int effectiveMax = isNightTime ? maxCannibals : Mathf.Min(3, maxCannibals);
            float effectiveInterval = isNightTime ? spawnInterval : 25f;

            // Sinh đợt thổ dân tuần tra ban đầu ngay sau khi vào game hoặc cutscene kết thúc
            if (!initialSpawnDone)
            {
                initialSpawnDone = true;
                int initialCount = isNightTime ? 4 : 2;
                for (int i = 0; i < initialCount; i++)
                {
                    SpawnCannibal();
                }
                Debug.Log(string.Format("🏹 [CannibalSpawner] Đã sinh bầy Thổ Dân ban đầu ({0} tên) rình rập quanh người chơi! ({1})",
                    initialCount, isNightTime ? "Ban Đêm" : "Ban Ngày"));
            }

            // Dọn dẹp danh sách những thổ dân đã chết hoặc bị hủy
            activeCannibals.RemoveAll(c => c == null || (c.GetComponent<CannibalAI>() != null && c.GetComponent<CannibalAI>().IsDead));

            if (activeCannibals.Count >= effectiveMax) return;

            timer += Time.deltaTime;
            if (timer >= effectiveInterval)
            {
                timer = 0f;
                SpawnCannibal();
                // Ban đêm có 40% cơ hội sinh thêm 1 tên cùng lúc; ban ngày không
                if (isNightTime && Random.value < 0.4f) SpawnCannibal();
            }
        }

        private void FindPlayer()
        {
            if (playerTransform != null) return;
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else if (Camera.main != null) playerTransform = Camera.main.transform;
        }

        public void SpawnCannibal()
        {
            ResolvePrefab();
            if (cannibalPrefab == null) return;
            FindPlayer();

            Vector3 center = (playerTransform != null) ? playerTransform.position : ZombieSpawner.CRASH_SITE_CENTER;

            for (int attempt = 0; attempt < 20; attempt++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(minSpawnDistFromPlayer, maxSpawnDistFromPlayer);
                Vector3 testPos = center + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

                NavMeshHit hit;
                if (NavMesh.SamplePosition(testPos, out hit, 15f, NavMesh.AllAreas))
                {
                    // Tránh sinh đè trực tiếp lên ngọn lửa máy bay (< 12m)
                    if (Vector3.Distance(hit.position, ZombieSpawner.CRASH_SITE_CENTER) < 12f)
                        continue;

                    GameObject obj = Instantiate(cannibalPrefab, hit.position, Quaternion.identity);
                    obj.name = "CannibalTribal_" + (activeCannibals.Count + 1);
                    obj.SetActive(true);

                    // Gỡ EnemyAI thường để thay bằng CannibalAI chuyên biệt
                    EnemyAI oldAI = obj.GetComponent<EnemyAI>();
                    if (oldAI != null && !(oldAI is CannibalAI))
                    {
                        oldAI.enabled = false;
                        Destroy(oldAI);
                    }

                    CannibalAI tribalAI = obj.GetComponent<CannibalAI>();
                    if (tribalAI == null)
                    {
                        tribalAI = obj.AddComponent<CannibalAI>();
                    }

                    if (tribalAI != null)
                    {
                        if (warCryClip != null) tribalAI.warCryClip = warCryClip;
                        if (howlClip != null) tribalAI.howlClip = howlClip;
                        if (growlClip != null) tribalAI.growlClip = growlClip;
                        tribalAI.LoadAudioClips();
                    }

                    // Warp NavMeshAgent vào đúng mặt đất
                    NavMeshAgent agent = obj.GetComponent<NavMeshAgent>();
                    if (agent != null)
                    {
                        agent.Warp(hit.position);
                        agent.enabled = true;
                    }

                    activeCannibals.Add(obj);
                    float dToPlayer = playerTransform != null ? Vector3.Distance(hit.position, playerTransform.position) : 0f;
                    Debug.Log(string.Format("🏹 [CannibalSpawner] Đã spawn Thổ Dân tại {0} (cách bạn {1:F1}m)!", hit.position, dToPlayer));
                    break;
                }
            }
        }
    }
}

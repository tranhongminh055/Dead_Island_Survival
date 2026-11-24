using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using HorrorGame.Inventory;

namespace HorrorGame.Survival
{
    /// <summary>
    /// Hệ thống Quản Lý & Tự Động Sinh Thú Rừng (Animal Spawner) trên Đảo.
    /// Tự động khởi tạo khi vào game, rải đàn thú ban đầu (10-14 con),
    /// tự động né mặt nước biển và vùng máy bay rơi, đảm bảo thú luôn xuất hiện trên mặt đất khô ráo.
    /// </summary>
    public class AnimalSpawner : MonoBehaviour
    {
        public static AnimalSpawner Instance;

        [Header("── Cấu Hình Spawner ──")]
        public Transform playerTransform;
        public int maxAnimals = 16;
        public int initialSpawnCount = 12; // Đàn thú ban đầu khi vừa vào game
        public float spawnRadius = 75f;
        public float minSpawnDistance = 18f;
        public float despawnRadius = 135f;
        public float spawnInterval = 7f; // Kiểm tra và sinh thêm mỗi 7 giây

        [Header("── Tỉ Lệ Các Loài Thú ──")]
        [Range(0f, 1f)] public float deerChance = 0.45f;    // 45% Hươu
        [Range(0f, 1f)] public float rabbitChance = 0.35f;  // 35% Thỏ
        [Range(0f, 1f)] public float boarChance = 0.20f;    // 20% Lợn rừng

        // Hằng số địa hình bản đồ Flooded Grounds
        private const float WATER_LEVEL = 15.6f;
        private static readonly Vector3 CRASH_SITE_CENTER = new Vector3(537f, 17.6f, 545f);
        private const float CRASH_SAFE_RADIUS = 15f;

        private List<GameObject> activeAnimals = new List<GameObject>();
        private float spawnTimer = 1.5f;
        private bool initialSpawnDone = false;

        // Tự động khởi chạy ngay khi bất kỳ Scene chơi nào được nạp (trừ MainMenu)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName.ToLower().Contains("mainmenu")) return;

            if (Instance == null && FindObjectOfType<AnimalSpawner>() == null)
            {
                GameObject spawnerObj = new GameObject("[AnimalSpawner]");
                spawnerObj.AddComponent<AnimalSpawner>();
                Debug.Log("🦌 [AnimalSpawner] Đã tự động kích hoạt hệ sinh thái Động Vật Hoang Dã cho scene: " + sceneName);
            }
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }
        }

        void Start()
        {
            FindPlayer();
            spawnTimer = 2.0f;
        }

        private void FindPlayer()
        {
            if (playerTransform != null) return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
            else
            {
                var pc = FindObjectOfType<HorrorGame.Player.PlayerController>();
                if (pc != null) playerTransform = pc.transform;
                else if (Camera.main != null) playerTransform = Camera.main.transform;
            }
        }

        void Update()
        {
            FindPlayer();
            if (playerTransform == null) return;

            // Trong lúc đang diễn ra cutscene máy bay rơi: chờ kết thúc rồi mới đẻ thú
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;

            // 1. Sinh ngay đàn thú ban đầu ngay khi người chơi bắt đầu tự do di chuyển
            if (!initialSpawnDone)
            {
                initialSpawnDone = true;
                SpawnInitialHerd();
            }

            // 2. Định kỳ kiểm tra dọn thú xa và bổ sung thú mới
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = spawnInterval;
                CleanupDeadAnimals();
                DespawnFarAnimals();

                if (activeAnimals.Count < maxAnimals)
                {
                    int count = Random.Range(1, 3);
                    for (int i = 0; i < count && activeAnimals.Count < maxAnimals; i++)
                    {
                        SpawnOneAnimal();
                    }
                }
            }
        }

        private void SpawnInitialHerd()
        {
            Debug.Log(string.Format("🦌 [AnimalSpawner] Đang rải đàn thú ban đầu ({0} con) khắp khu rừng...", initialSpawnCount));
            int successCount = 0;

            for (int i = 0; i < initialSpawnCount && activeAnimals.Count < maxAnimals; i++)
            {
                if (SpawnOneAnimal())
                {
                    successCount++;
                }
            }

            Debug.Log(string.Format("✅ [AnimalSpawner] Đã sinh thành công {0} thú rừng (Hươu, Thỏ, Lợn Rừng) quanh đảo!", successCount));
        }

        private bool SpawnOneAnimal()
        {
            Vector3 spawnPos;
            if (!FindValidSpawnPosition(out spawnPos)) return false;

            AnimalType type = PickAnimalType();
            GameObject animal = CreateAnimal(type, spawnPos);
            if (animal != null)
            {
                activeAnimals.Add(animal);
                return true;
            }
            return false;
        }

        private bool FindValidSpawnPosition(out Vector3 pos)
        {
            pos = Vector3.zero;
            if (playerTransform == null) return false;

            for (int attempt = 0; attempt < 16; attempt++)
            {
                // Chọn điểm ngẫu nhiên theo hình vành khăn quanh player
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(minSpawnDistance, spawnRadius);
                Vector3 candidatePos = playerTransform.position + new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);

                // Tránh vùng đám cháy máy bay rơi
                if (Vector3.Distance(candidatePos, CRASH_SITE_CENTER) < CRASH_SAFE_RADIUS) continue;

                // 1. Thử tìm vị trí trên NavMesh đã nướng sẵn (chính xác và không bao giờ ở dưới nước)
                NavMeshHit navHit;
                if (NavMesh.SamplePosition(candidatePos, out navHit, 16f, NavMesh.AllAreas))
                {
                    if (navHit.position.y >= WATER_LEVEL)
                    {
                        pos = navHit.position;
                        return true;
                    }
                }

                // 2. Fallback: Raycast kiểm tra Terrain mặt đất
                Terrain terrain = Terrain.activeTerrain;
                if (terrain != null)
                {
                    float terrainY = terrain.SampleHeight(candidatePos) + terrain.transform.position.y;
                    if (terrainY >= WATER_LEVEL)
                    {
                        RaycastHit hit;
                        if (Physics.Raycast(new Vector3(candidatePos.x, terrainY + 10f, candidatePos.z), Vector3.down, out hit, 25f))
                        {
                            if (hit.point.y >= WATER_LEVEL)
                            {
                                pos = hit.point;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private AnimalType PickAnimalType()
        {
            float roll = Random.value;
            if (roll < deerChance) return AnimalType.Deer;
            if (roll < deerChance + rabbitChance) return AnimalType.Rabbit;
            return AnimalType.Boar;
        }

        // ═══════════════════════════════════════════════════════
        // TẠO OBJECT VÀ CẤU TRÚC 3D CỦA THÚ
        // ═══════════════════════════════════════════════════════

        private GameObject CreateAnimal(AnimalType type, Vector3 position)
        {
            GameObject animal = new GameObject();
            animal.transform.position = position;
            animal.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            // Thêm NavMeshAgent để di chuyển thông minh và bám mặt đất
            NavMeshAgent agent = animal.AddComponent<NavMeshAgent>();
            agent.autoBraking = true;
            agent.acceleration = 12f;
            agent.angularSpeed = 260f;

            // Thêm AnimalAI
            AnimalAI ai = animal.AddComponent<AnimalAI>();
            ai.animalType = type;
            ai.playerTransform = playerTransform;

            BoxCollider col = animal.AddComponent<BoxCollider>();

            switch (type)
            {
                case AnimalType.Deer:
                    animal.name = "Deer_" + Random.Range(100, 999);
                    BuildDeer(animal, ai, agent, col);
                    break;

                case AnimalType.Rabbit:
                    animal.name = "Rabbit_" + Random.Range(100, 999);
                    BuildRabbit(animal, ai, agent, col);
                    break;

                case AnimalType.Boar:
                    animal.name = "Boar_" + Random.Range(100, 999);
                    BuildBoar(animal, ai, agent, col);
                    break;
            }

            ai.Initialize();
            return animal;
        }

        // ── 1. HƯƠU RỪNG (DEER) ──
        private void BuildDeer(GameObject root, AnimalAI ai, NavMeshAgent agent, BoxCollider col)
        {
            ai.maxHealth = 60f;
            ai.moveSpeed = 3.6f;
            ai.runSpeed = 8.8f;
            ai.detectionRange = 22f;
            ai.fleeRange = 38f;
            ai.meatDropAmount = 3;
            ai.hideDropAmount = 2;
            ai.isAggressive = false;

            agent.speed = ai.moveSpeed;
            agent.radius = 0.5f;
            agent.height = 1.6f;

            col.center = new Vector3(0, 0.9f, 0);
            col.size = new Vector3(0.9f, 1.6f, 1.8f);

            Color deerBrown = new Color(0.55f, 0.35f, 0.16f);
            Color deerBelly = new Color(0.88f, 0.82f, 0.72f);
            Color antlerColor = new Color(0.38f, 0.30f, 0.20f);
            Color darkLeg = new Color(0.40f, 0.26f, 0.12f);

            // Thân
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0, 0.95f, 0);
            body.transform.localRotation = Quaternion.Euler(90, 0, 0);
            body.transform.localScale = new Vector3(0.55f, 0.72f, 0.55f);
            SetColor(body, deerBrown);
            DestroyCollider(body);
            ai.bodyTransform = body.transform;
            ai.baseBodyY = 0.95f;

            // Bụng trắng kem
            GameObject belly = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            belly.name = "Belly";
            belly.transform.SetParent(body.transform, false);
            belly.transform.localPosition = new Vector3(0, 0, 0.22f);
            belly.transform.localScale = new Vector3(0.85f, 0.85f, 0.5f);
            SetColor(belly, deerBelly);
            DestroyCollider(belly);

            // Cổ + Đầu
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0, 1.45f, 0.78f);
            head.transform.localScale = new Vector3(0.35f, 0.42f, 0.48f);
            SetColor(head, deerBrown);
            DestroyCollider(head);
            ai.headTransform = head.transform;

            // Mõm
            GameObject snout = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            snout.name = "Snout";
            snout.transform.SetParent(head.transform, false);
            snout.transform.localPosition = new Vector3(0, -0.15f, 0.4f);
            snout.transform.localRotation = Quaternion.Euler(90, 0, 0);
            snout.transform.localScale = new Vector3(0.45f, 0.25f, 0.45f);
            SetColor(snout, deerBelly);
            DestroyCollider(snout);

            // Sừng trái
            GameObject antlerL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            antlerL.name = "AntlerL";
            antlerL.transform.SetParent(head.transform, false);
            antlerL.transform.localPosition = new Vector3(-0.35f, 0.65f, -0.05f);
            antlerL.transform.localRotation = Quaternion.Euler(-10, 0, 25);
            antlerL.transform.localScale = new Vector3(0.08f, 0.45f, 0.08f);
            SetColor(antlerL, antlerColor);
            DestroyCollider(antlerL);

            // Sừng phải
            GameObject antlerR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            antlerR.name = "AntlerR";
            antlerR.transform.SetParent(head.transform, false);
            antlerR.transform.localPosition = new Vector3(0.35f, 0.65f, -0.05f);
            antlerR.transform.localRotation = Quaternion.Euler(-10, 0, -25);
            antlerR.transform.localScale = new Vector3(0.08f, 0.45f, 0.08f);
            SetColor(antlerR, antlerColor);
            DestroyCollider(antlerR);

            // 4 Chân cử động
            ai.legTransforms = new Transform[4];
            float[] xOffsets = { -0.22f, 0.22f, -0.22f, 0.22f };
            float[] zOffsets = { 0.48f, 0.48f, -0.48f, -0.48f };

            for (int i = 0; i < 4; i++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = "Leg_" + i;
                leg.transform.SetParent(root.transform, false);
                leg.transform.localPosition = new Vector3(xOffsets[i], 0.45f, zOffsets[i]);
                leg.transform.localScale = new Vector3(0.12f, 0.45f, 0.12f);
                SetColor(leg, darkLeg);
                DestroyCollider(leg);
                ai.legTransforms[i] = leg.transform;
            }

            // Đuôi nhỏ
            GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tail.name = "Tail";
            tail.transform.SetParent(root.transform, false);
            tail.transform.localPosition = new Vector3(0, 1.05f, -0.72f);
            tail.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
            SetColor(tail, deerBelly);
            DestroyCollider(tail);
        }

        // ── 2. THỎ RỪNG (RABBIT) ──
        private void BuildRabbit(GameObject root, AnimalAI ai, NavMeshAgent agent, BoxCollider col)
        {
            ai.maxHealth = 18f;
            ai.moveSpeed = 4.2f;
            ai.runSpeed = 10.5f;
            ai.detectionRange = 14f;
            ai.fleeRange = 28f;
            ai.meatDropAmount = 1;
            ai.hideDropAmount = 1;
            ai.isAggressive = false;

            agent.speed = ai.moveSpeed;
            agent.radius = 0.28f;
            agent.height = 0.6f;

            col.center = new Vector3(0, 0.32f, 0);
            col.size = new Vector3(0.5f, 0.65f, 0.65f);

            Color rabbitFur = new Color(0.78f, 0.70f, 0.58f);
            Color rabbitInnerEar = new Color(0.92f, 0.68f, 0.70f);
            Color rabbitPaws = new Color(0.88f, 0.84f, 0.78f);

            // Thân tròn nhỏ
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0, 0.28f, 0);
            body.transform.localScale = new Vector3(0.36f, 0.30f, 0.46f);
            SetColor(body, rabbitFur);
            DestroyCollider(body);
            ai.bodyTransform = body.transform;
            ai.baseBodyY = 0.28f;

            // Đầu thỏ
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0, 0.42f, 0.26f);
            head.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
            SetColor(head, rabbitFur);
            DestroyCollider(head);
            ai.headTransform = head.transform;

            // Tai trái
            GameObject earL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            earL.name = "EarL";
            earL.transform.SetParent(head.transform, false);
            earL.transform.localPosition = new Vector3(-0.35f, 0.85f, -0.1f);
            earL.transform.localRotation = Quaternion.Euler(0, 0, 12);
            earL.transform.localScale = new Vector3(0.2f, 0.95f, 0.1f);
            SetColor(earL, rabbitInnerEar);
            DestroyCollider(earL);

            // Tai phải
            GameObject earR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            earR.name = "EarR";
            earR.transform.SetParent(head.transform, false);
            earR.transform.localPosition = new Vector3(0.35f, 0.85f, -0.1f);
            earR.transform.localRotation = Quaternion.Euler(0, 0, -12);
            earR.transform.localScale = new Vector3(0.2f, 0.95f, 0.1f);
            SetColor(earR, rabbitInnerEar);
            DestroyCollider(earR);

            // 4 Chân nhỏ
            ai.legTransforms = new Transform[4];
            float[] xOffsets = { -0.12f, 0.12f, -0.14f, 0.14f };
            float[] zOffsets = { 0.16f, 0.16f, -0.16f, -0.16f };

            for (int i = 0; i < 4; i++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leg.name = "Paw_" + i;
                leg.transform.SetParent(root.transform, false);
                leg.transform.localPosition = new Vector3(xOffsets[i], 0.12f, zOffsets[i]);
                leg.transform.localScale = new Vector3(0.11f, 0.11f, 0.14f);
                SetColor(leg, rabbitPaws);
                DestroyCollider(leg);
                ai.legTransforms[i] = leg.transform;
            }

            // Đuôi bông tròn
            GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tail.name = "Tail";
            tail.transform.SetParent(root.transform, false);
            tail.transform.localPosition = new Vector3(0, 0.32f, -0.25f);
            tail.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            SetColor(tail, Color.white);
            DestroyCollider(tail);
        }

        // ── 3. LỢN RỪNG (BOAR) ──
        private void BuildBoar(GameObject root, AnimalAI ai, NavMeshAgent agent, BoxCollider col)
        {
            ai.maxHealth = 110f;
            ai.moveSpeed = 2.8f;
            ai.runSpeed = 7.5f;
            ai.detectionRange = 16f;
            ai.fleeRange = 0f;
            ai.meatDropAmount = 4;
            ai.hideDropAmount = 3;
            ai.isAggressive = true;
            ai.attackDamage = 18f;
            ai.attackRange = 2.4f;
            ai.attackCooldown = 1.4f;

            agent.speed = ai.moveSpeed;
            agent.radius = 0.6f;
            agent.height = 1.2f;

            col.center = new Vector3(0, 0.65f, 0);
            col.size = new Vector3(1.0f, 1.2f, 1.6f);

            Color boarBrown = new Color(0.24f, 0.19f, 0.16f);
            Color snoutPink = new Color(0.48f, 0.34f, 0.30f);
            Color tuskIvory = new Color(0.96f, 0.93f, 0.84f);

            // Thân vạm vỡ
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0, 0.68f, 0);
            body.transform.localRotation = Quaternion.Euler(90, 0, 0);
            body.transform.localScale = new Vector3(0.72f, 0.72f, 0.72f);
            SetColor(body, boarBrown);
            DestroyCollider(body);
            ai.bodyTransform = body.transform;
            ai.baseBodyY = 0.68f;

            // Đầu to chúi về trước
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0, 0.72f, 0.75f);
            head.transform.localScale = new Vector3(0.55f, 0.50f, 0.58f);
            SetColor(head, boarBrown);
            DestroyCollider(head);
            ai.headTransform = head.transform;

            // Mõm lợn
            GameObject snout = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            snout.name = "Snout";
            snout.transform.SetParent(head.transform, false);
            snout.transform.localPosition = new Vector3(0, -0.1f, 0.45f);
            snout.transform.localRotation = Quaternion.Euler(90, 0, 0);
            snout.transform.localScale = new Vector3(0.35f, 0.18f, 0.35f);
            SetColor(snout, snoutPink);
            DestroyCollider(snout);

            // Ngà trái
            GameObject tuskL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tuskL.name = "TuskL";
            tuskL.transform.SetParent(head.transform, false);
            tuskL.transform.localPosition = new Vector3(-0.25f, -0.06f, 0.42f);
            tuskL.transform.localRotation = Quaternion.Euler(20, 0, 35);
            tuskL.transform.localScale = new Vector3(0.06f, 0.20f, 0.06f);
            SetColor(tuskL, tuskIvory);
            DestroyCollider(tuskL);

            // Ngà phải
            GameObject tuskR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tuskR.name = "TuskR";
            tuskR.transform.SetParent(head.transform, false);
            tuskR.transform.localPosition = new Vector3(0.25f, -0.06f, 0.42f);
            tuskR.transform.localRotation = Quaternion.Euler(20, 0, -35);
            tuskR.transform.localScale = new Vector3(0.06f, 0.20f, 0.06f);
            SetColor(tuskR, tuskIvory);
            DestroyCollider(tuskR);

            // 4 Chân ngắn khỏe
            ai.legTransforms = new Transform[4];
            float[] xOffsets = { -0.28f, 0.28f, -0.28f, 0.28f };
            float[] zOffsets = { 0.45f, 0.45f, -0.45f, -0.45f };

            for (int i = 0; i < 4; i++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = "Leg_" + i;
                leg.transform.SetParent(root.transform, false);
                leg.transform.localPosition = new Vector3(xOffsets[i], 0.28f, zOffsets[i]);
                leg.transform.localScale = new Vector3(0.18f, 0.28f, 0.18f);
                SetColor(leg, boarBrown * 0.85f);
                DestroyCollider(leg);
                ai.legTransforms[i] = leg.transform;
            }
        }

        private void SetColor(GameObject obj, Color color)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r != null)
            {
                Shader shader = Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("Mobile/Diffuse");
                if (shader == null) shader = Shader.Find("Diffuse");

                Material mat = new Material(shader);
                mat.color = color;
                r.sharedMaterial = mat;
            }
        }

        private void DestroyCollider(GameObject obj)
        {
            Collider col = obj.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        // ═══════════════════════════════════════════════════════
        // DỌN DẸP & THU HỒI
        // ═══════════════════════════════════════════════════════

        private void CleanupDeadAnimals()
        {
            activeAnimals.RemoveAll(a => a == null);
        }

        private void DespawnFarAnimals()
        {
            if (playerTransform == null) return;

            for (int i = activeAnimals.Count - 1; i >= 0; i--)
            {
                if (activeAnimals[i] == null) continue;

                float dist = Vector3.Distance(activeAnimals[i].transform.position, playerTransform.position);
                if (dist > despawnRadius)
                {
                    Destroy(activeAnimals[i]);
                    activeAnimals.RemoveAt(i);
                }
            }
        }
    }
}

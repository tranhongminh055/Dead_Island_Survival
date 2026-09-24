using UnityEngine;
using System.Collections.Generic;
using HorrorGame.Inventory;

namespace HorrorGame.Survival
{
    /// <summary>
    /// Quản lý spawn thú rừng quanh player.
    /// Tự tạo các loại thú: Hươu (Deer), Thỏ (Rabbit), Lợn rừng (Boar).
    /// Thú sẽ spawn ngoài tầm nhìn player và despawn khi quá xa.
    /// </summary>
    public class AnimalSpawner : MonoBehaviour
    {
        [Header("── Cấu Hình ──")]
        public Transform playerTransform;
        public int maxAnimals = 15;
        public float spawnRadius = 80f;
        public float despawnRadius = 120f;
        public float spawnInterval = 10f; // Spawn mỗi 10 giây
        public float minSpawnDistance = 30f; // Không spawn quá gần player

        [Header("── Tỉ Lệ Spawn ──")]
        [Range(0f, 1f)] public float deerChance = 0.4f;
        [Range(0f, 1f)] public float rabbitChance = 0.35f;
        [Range(0f, 1f)] public float boarChance = 0.25f;

        // Tracking
        private List<GameObject> activeAnimals = new List<GameObject>();
        private float spawnTimer;

        void Start()
        {
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTransform = player.transform;
                else
                {
                    // Tìm PlayerController
                    var pc = FindObjectOfType<HorrorGame.Player.PlayerController>();
                    if (pc != null) playerTransform = pc.transform;
                }
            }

            spawnTimer = 3f; // Spawn batch đầu tiên sau 3 giây
        }

        void Update()
        {
            if (playerTransform == null) return;

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = spawnInterval;
                CleanupDeadAnimals();
                DespawnFarAnimals();
                TrySpawnAnimal();
            }
        }

        private void TrySpawnAnimal()
        {
            if (activeAnimals.Count >= maxAnimals) return;

            // Spawn 1-3 con mỗi lần
            int count = Random.Range(1, 4);
            for (int i = 0; i < count && activeAnimals.Count < maxAnimals; i++)
            {
                SpawnOneAnimal();
            }
        }

        private void SpawnOneAnimal()
        {
            // Tìm vị trí spawn hợp lệ (trên mặt đất, ngoài tầm nhìn player)
            Vector3 spawnPos;
            if (!FindValidSpawnPosition(out spawnPos)) return;

            // Chọn loại thú
            AnimalType type = PickAnimalType();

            // Tạo thú
            GameObject animal = CreateAnimal(type, spawnPos);
            if (animal != null)
            {
                activeAnimals.Add(animal);
            }
        }

        private bool FindValidSpawnPosition(out Vector3 pos)
        {
            pos = Vector3.zero;

            for (int attempt = 0; attempt < 10; attempt++)
            {
                // Random vị trí trong vòng tròn quanh player
                Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minSpawnDistance, spawnRadius);
                Vector3 candidatePos = playerTransform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

                // Raycast xuống mặt đất
                RaycastHit hit;
                if (Physics.Raycast(candidatePos + Vector3.up * 50f, Vector3.down, out hit, 100f))
                {
                    // Kiểm tra không phải nước (Y quá thấp)
                    if (hit.point.y > 0.5f) // Trên mực nước
                    {
                        pos = hit.point;
                        return true;
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

        private GameObject CreateAnimal(AnimalType type, Vector3 position)
        {
            GameObject animal = new GameObject();
            animal.transform.position = position;

            // Tạo visual model (placeholder)
            GameObject body = null;

            switch (type)
            {
                case AnimalType.Deer:
                    animal.name = "Deer_" + Random.Range(1000, 9999);
                    body = CreateDeerModel(animal.transform);
                    break;

                case AnimalType.Rabbit:
                    animal.name = "Rabbit_" + Random.Range(1000, 9999);
                    body = CreateRabbitModel(animal.transform);
                    break;

                case AnimalType.Boar:
                    animal.name = "Boar_" + Random.Range(1000, 9999);
                    body = CreateBoarModel(animal.transform);
                    break;
            }

            // Thêm AnimalAI component
            AnimalAI ai = animal.AddComponent<AnimalAI>();
            ai.animalType = type;
            ai.playerTransform = playerTransform;

            // Cấu hình theo loại
            switch (type)
            {
                case AnimalType.Deer:
                    ai.maxHealth = 60f;
                    ai.moveSpeed = 3.5f;
                    ai.runSpeed = 8f;
                    ai.detectionRange = 20f;
                    ai.fleeRange = 35f;
                    ai.meatDropAmount = 3;
                    ai.hideDropAmount = 2;
                    ai.isAggressive = false;
                    break;

                case AnimalType.Rabbit:
                    ai.maxHealth = 15f;
                    ai.moveSpeed = 4f;
                    ai.runSpeed = 10f;
                    ai.detectionRange = 12f;
                    ai.fleeRange = 25f;
                    ai.meatDropAmount = 1;
                    ai.hideDropAmount = 1;
                    ai.isAggressive = false;
                    break;

                case AnimalType.Boar:
                    ai.maxHealth = 100f;
                    ai.moveSpeed = 2.5f;
                    ai.runSpeed = 7f;
                    ai.detectionRange = 15f;
                    ai.fleeRange = 0f; // Không chạy trốn
                    ai.meatDropAmount = 4;
                    ai.hideDropAmount = 3;
                    ai.isAggressive = true;
                    ai.attackDamage = 15f;
                    ai.attackRange = 2.5f;
                    break;
            }

            ai.Initialize();

            // Thêm collider cho việc bắn/chặt
            BoxCollider col = animal.AddComponent<BoxCollider>();
            if (body != null)
            {
                Renderer rr = body.GetComponent<Renderer>();
                if (rr != null)
                {
                    col.center = animal.transform.InverseTransformPoint(rr.bounds.center);
                    col.size = rr.bounds.size;
                }
            }

            // Tag để hệ thống vũ khí nhận diện
            animal.tag = "Untagged"; // Sẽ dùng layer hoặc component để check

            return animal;
        }

        // ═══════════════════════════════════════════════════════
        // TẠO MODEL PLACEHOLDER CHO THÚ
        // (Sẽ được thay bằng model thật khi có asset)
        // ═══════════════════════════════════════════════════════

        private GameObject CreateDeerModel(Transform parent)
        {
            // Thân
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(parent, false);
            body.transform.localPosition = new Vector3(0, 1f, 0);
            body.transform.localRotation = Quaternion.Euler(0, 0, 90);
            body.transform.localScale = new Vector3(0.6f, 1.2f, 0.6f);
            SetColor(body, new Color(0.55f, 0.35f, 0.15f)); // Nâu

            // Đầu
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(parent, false);
            head.transform.localPosition = new Vector3(0, 1.3f, 1.2f);
            head.transform.localScale = new Vector3(0.35f, 0.35f, 0.45f);
            SetColor(head, new Color(0.55f, 0.35f, 0.15f));

            // Sừng trái
            GameObject antlerL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            antlerL.name = "AntlerL";
            antlerL.transform.SetParent(head.transform, false);
            antlerL.transform.localPosition = new Vector3(-0.3f, 0.7f, 0f);
            antlerL.transform.localRotation = Quaternion.Euler(0, 0, 20);
            antlerL.transform.localScale = new Vector3(0.1f, 0.5f, 0.1f);
            SetColor(antlerL, new Color(0.4f, 0.3f, 0.15f));

            // Sừng phải
            GameObject antlerR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            antlerR.name = "AntlerR";
            antlerR.transform.SetParent(head.transform, false);
            antlerR.transform.localPosition = new Vector3(0.3f, 0.7f, 0f);
            antlerR.transform.localRotation = Quaternion.Euler(0, 0, -20);
            antlerR.transform.localScale = new Vector3(0.1f, 0.5f, 0.1f);
            SetColor(antlerR, new Color(0.4f, 0.3f, 0.15f));

            // 4 chân
            for (int i = 0; i < 4; i++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = "Leg" + i;
                leg.transform.SetParent(parent, false);
                float xOff = (i % 2 == 0) ? -0.25f : 0.25f;
                float zOff = (i < 2) ? 0.6f : -0.6f;
                leg.transform.localPosition = new Vector3(xOff, 0.4f, zOff);
                leg.transform.localScale = new Vector3(0.12f, 0.4f, 0.12f);
                SetColor(leg, new Color(0.45f, 0.3f, 0.12f));
                Destroy(leg.GetComponent<Collider>());
            }

            // Xóa collider thừa
            Destroy(head.GetComponent<Collider>());
            Destroy(antlerL.GetComponent<Collider>());
            Destroy(antlerR.GetComponent<Collider>());

            return body;
        }

        private GameObject CreateRabbitModel(Transform parent)
        {
            // Thân nhỏ
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            body.transform.SetParent(parent, false);
            body.transform.localPosition = new Vector3(0, 0.25f, 0);
            body.transform.localScale = new Vector3(0.3f, 0.25f, 0.4f);
            SetColor(body, new Color(0.75f, 0.65f, 0.5f)); // Nâu nhạt

            // Đầu
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(parent, false);
            head.transform.localPosition = new Vector3(0, 0.35f, 0.25f);
            head.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            SetColor(head, new Color(0.75f, 0.65f, 0.5f));

            // Tai trái
            GameObject earL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            earL.name = "EarL";
            earL.transform.SetParent(head.transform, false);
            earL.transform.localPosition = new Vector3(-0.3f, 0.7f, 0);
            earL.transform.localRotation = Quaternion.Euler(0, 0, 10);
            earL.transform.localScale = new Vector3(0.2f, 0.8f, 0.1f);
            SetColor(earL, new Color(0.85f, 0.7f, 0.55f));

            // Tai phải
            GameObject earR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            earR.name = "EarR";
            earR.transform.SetParent(head.transform, false);
            earR.transform.localPosition = new Vector3(0.3f, 0.7f, 0);
            earR.transform.localRotation = Quaternion.Euler(0, 0, -10);
            earR.transform.localScale = new Vector3(0.2f, 0.8f, 0.1f);
            SetColor(earR, new Color(0.85f, 0.7f, 0.55f));

            Destroy(head.GetComponent<Collider>());
            Destroy(earL.GetComponent<Collider>());
            Destroy(earR.GetComponent<Collider>());

            return body;
        }

        private GameObject CreateBoarModel(Transform parent)
        {
            // Thân to
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(parent, false);
            body.transform.localPosition = new Vector3(0, 0.7f, 0);
            body.transform.localRotation = Quaternion.Euler(0, 0, 90);
            body.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
            SetColor(body, new Color(0.3f, 0.25f, 0.2f)); // Nâu đen

            // Đầu
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(parent, false);
            head.transform.localPosition = new Vector3(0, 0.7f, 1f);
            head.transform.localScale = new Vector3(0.5f, 0.45f, 0.55f);
            SetColor(head, new Color(0.3f, 0.25f, 0.2f));

            // Mũi (snout)
            GameObject snout = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            snout.name = "Snout";
            snout.transform.SetParent(head.transform, false);
            snout.transform.localPosition = new Vector3(0, -0.1f, 0.5f);
            snout.transform.localRotation = Quaternion.Euler(90, 0, 0);
            snout.transform.localScale = new Vector3(0.35f, 0.2f, 0.35f);
            SetColor(snout, new Color(0.5f, 0.35f, 0.25f));

            // Ngà trái
            GameObject tuskL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tuskL.name = "TuskL";
            tuskL.transform.SetParent(head.transform, false);
            tuskL.transform.localPosition = new Vector3(-0.2f, -0.1f, 0.5f);
            tuskL.transform.localRotation = Quaternion.Euler(0, 0, 30);
            tuskL.transform.localScale = new Vector3(0.05f, 0.15f, 0.05f);
            SetColor(tuskL, new Color(0.9f, 0.85f, 0.7f)); // Ngà vàng

            // Ngà phải
            GameObject tuskR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tuskR.name = "TuskR";
            tuskR.transform.SetParent(head.transform, false);
            tuskR.transform.localPosition = new Vector3(0.2f, -0.1f, 0.5f);
            tuskR.transform.localRotation = Quaternion.Euler(0, 0, -30);
            tuskR.transform.localScale = new Vector3(0.05f, 0.15f, 0.05f);
            SetColor(tuskR, new Color(0.9f, 0.85f, 0.7f));

            // 4 chân ngắn
            for (int i = 0; i < 4; i++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = "Leg" + i;
                leg.transform.SetParent(parent, false);
                float xOff = (i % 2 == 0) ? -0.3f : 0.3f;
                float zOff = (i < 2) ? 0.5f : -0.5f;
                leg.transform.localPosition = new Vector3(xOff, 0.25f, zOff);
                leg.transform.localScale = new Vector3(0.15f, 0.25f, 0.15f);
                SetColor(leg, new Color(0.25f, 0.2f, 0.15f));
                Destroy(leg.GetComponent<Collider>());
            }

            Destroy(head.GetComponent<Collider>());
            Destroy(snout.GetComponent<Collider>());
            Destroy(tuskL.GetComponent<Collider>());
            Destroy(tuskR.GetComponent<Collider>());

            return body;
        }

        private void SetColor(GameObject obj, Color color)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                r.sharedMaterial = mat;
            }
        }

        // ═══════════════════════════════════════════════════════
        // CLEANUP
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

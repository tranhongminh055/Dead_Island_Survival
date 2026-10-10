using UnityEngine;
using UnityEngine.AI;
using HorrorGame.Inventory;

namespace HorrorGame.Survival
{
    public enum AnimalType
    {
        Deer,       // Hươu - nhát gan, chạy nhanh, cho nhiều thịt và da
        Rabbit,     // Thỏ - nhỏ, nhảy nhanh, cho ít thịt
        Boar        // Lợn rừng - hung dữ, tấn công lại player
    }

    public enum AnimalState
    {
        Idle,       // Đứng yên quan sát
        Wandering,  // Đi lang thang
        Eating,     // Ăn cỏ (cúi đầu)
        Fleeing,    // Hoảng sợ bỏ chạy
        Chasing,    // Rượt đuổi player (Boar)
        Attacking,  // Tấn công cắn/húc (Boar)
        Dead        // Đã chết, có thể thu hoạch thịt
    }

    /// <summary>
    /// AI Động Vật Sinh Tồn kiểu The Forest.
    /// Hỗ trợ NavMeshAgent, hoạt ảnh động học (chân bước, thỏ nhảy, cúi gặm cỏ),
    /// tự né nước biển, nhận sát thương từ Rìu/Súng, và cho phép người chơi ấn E thu hoạch thịt/da.
    /// </summary>
    public class AnimalAI : MonoBehaviour
    {
        [Header("── Cấu Hình Thú ──")]
        public AnimalType animalType = AnimalType.Deer;
        public float maxHealth = 60f;
        public float moveSpeed = 3.5f;
        public float runSpeed = 8.5f;
        public float detectionRange = 20f;  // Cự ly phát hiện người chơi
        public float fleeRange = 35f;       // Cự ly chạy trốn an toàn
        public bool isAggressive = false;   // Tự vệ hoặc chủ động tấn công?
        public float attackDamage = 15f;
        public float attackRange = 2.5f;
        public float attackCooldown = 1.6f;

        [Header("── Thu Hoạch (Loot) ──")]
        public int meatDropAmount = 3;
        public int hideDropAmount = 2;

        [Header("── Khung Xương Động Học ──")]
        public Transform headTransform;
        public Transform bodyTransform;
        public Transform[] legTransforms;
        public float baseBodyY = 0f;

        [HideInInspector] public Transform playerTransform;

        // Trạng thái nội bộ
        private AnimalState currentState = AnimalState.Idle;
        private float currentHealth;
        private float stateTimer = 0f;
        private Vector3 wanderTarget;
        private float nextAttackTime = 0f;
        private NavMeshAgent agent;

        // Animation động học
        private float animTimer = 0f;
        private bool isMoving = false;

        // Mặt đất & nước
        private const float WATER_LEVEL = 15.6f;
        private float groundCheckTimer = 0f;

        // UI & Tương tác
        private bool showHealthBar = false;
        private float healthBarTimer = 0f;
        private bool isDead = false;
        private float deathTimer = 0f;
        private bool lootDropped = false;
        private bool playerLookingAtThis = false;
        private string harvestNotice = "";
        private float harvestNoticeTimer = 0f;

        private GUIStyle interactStyle;
        private GUIStyle shadowStyle;

        public void Initialize()
        {
            currentHealth = maxHealth;
            currentState = AnimalState.Idle;
            stateTimer = Random.Range(2f, 5f);

            // Cài đặt NavMeshAgent
            agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = moveSpeed;
                agent.acceleration = 12f;
                agent.angularSpeed = 240f;
                agent.stoppingDistance = 0.5f;
            }

            PickNewWanderTarget();
        }

        void Start()
        {
            if (currentHealth <= 0) Initialize();
            FindPlayer();
        }

        private void FindPlayer()
        {
            if (playerTransform != null) return;
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else
            {
                var pc = FindObjectOfType<HorrorGame.Player.PlayerController>();
                if (pc != null) playerTransform = pc.transform;
                else if (Camera.main != null) playerTransform = Camera.main.transform;
            }
        }

        void Update()
        {
            if (isDead)
            {
                HandleDeath();
                return;
            }

            FindPlayer();
            if (playerTransform == null) return;

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            // Kiểm tra trạng thái AI
            switch (currentState)
            {
                case AnimalState.Idle:
                    HandleIdle(distToPlayer);
                    break;
                case AnimalState.Wandering:
                    HandleWandering(distToPlayer);
                    break;
                case AnimalState.Eating:
                    HandleEating(distToPlayer);
                    break;
                case AnimalState.Fleeing:
                    HandleFleeing(distToPlayer);
                    break;
                case AnimalState.Chasing:
                    HandleChasing(distToPlayer);
                    break;
                case AnimalState.Attacking:
                    HandleAttacking(distToPlayer);
                    break;
            }

            // Hoạt ảnh cử động (chân bước, nhún nhảy, gặm cỏ)
            UpdateProceduralAnimation();

            // Đảm bảo không rơi xuống đáy biển
            ClampToGroundAndLand();

            // Kiểm tra xem người chơi có đang nhìn vào thú không
            CheckPlayerLooking();

            // Đếm ngược thời gian thanh máu
            if (showHealthBar)
            {
                healthBarTimer -= Time.deltaTime;
                if (healthBarTimer <= 0f) showHealthBar = false;
            }

            if (harvestNoticeTimer > 0f)
            {
                harvestNoticeTimer -= Time.deltaTime;
            }
        }

        // ═══════════════════════════════════════════════════════
        // CÁC TRẠNG THÁI AI
        // ═══════════════════════════════════════════════════════

        private void HandleIdle(float distToPlayer)
        {
            isMoving = false;
            StopAgent();

            stateTimer -= Time.deltaTime;

            // Nhận diện người chơi
            if (distToPlayer < detectionRange)
            {
                if (isAggressive)
                {
                    currentState = AnimalState.Chasing;
                    return;
                }
                else
                {
                    currentState = AnimalState.Fleeing;
                    return;
                }
            }

            if (stateTimer <= 0f)
            {
                // Ngẫu nhiên đi lại hoặc gặm cỏ
                if (Random.value < 0.65f)
                {
                    currentState = AnimalState.Wandering;
                    PickNewWanderTarget();
                    stateTimer = Random.Range(6f, 14f);
                }
                else
                {
                    currentState = AnimalState.Eating;
                    stateTimer = Random.Range(3f, 7f);
                }
            }
        }

        private void HandleWandering(float distToPlayer)
        {
            // Phát hiện người chơi
            if (distToPlayer < detectionRange)
            {
                currentState = isAggressive ? AnimalState.Chasing : AnimalState.Fleeing;
                return;
            }

            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f || Vector3.Distance(transform.position, wanderTarget) < 1.8f)
            {
                currentState = AnimalState.Idle;
                stateTimer = Random.Range(2f, 5f);
                isMoving = false;
                StopAgent();
                return;
            }

            isMoving = true;
            MoveTowards(wanderTarget, moveSpeed);
        }

        private void HandleEating(float distToPlayer)
        {
            isMoving = false;
            StopAgent();

            if (distToPlayer < detectionRange)
            {
                currentState = isAggressive ? AnimalState.Chasing : AnimalState.Fleeing;
                return;
            }

            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                currentState = AnimalState.Idle;
                stateTimer = Random.Range(2f, 4f);
            }
        }

        private void HandleFleeing(float distToPlayer)
        {
            isMoving = true;

            // Chạy xa khỏi người chơi
            Vector3 fleeDir = (transform.position - playerTransform.position).normalized;
            fleeDir.y = 0;
            Vector3 fleePos = transform.position + fleeDir * 20f;

            // Kiểm tra điểm trốn hợp lệ trên đất liền
            NavMeshHit hit;
            if (NavMesh.SamplePosition(fleePos, out hit, 12f, NavMesh.AllAreas) && hit.position.y >= WATER_LEVEL)
            {
                fleePos = hit.position;
            }

            MoveTowards(fleePos, runSpeed);

            // Đã chạy đủ xa an toàn
            if (distToPlayer > fleeRange)
            {
                currentState = AnimalState.Idle;
                stateTimer = Random.Range(3f, 6f);
                isMoving = false;
                StopAgent();
            }
        }

        private void HandleChasing(float distToPlayer)
        {
            if (!isAggressive)
            {
                currentState = AnimalState.Fleeing;
                return;
            }

            isMoving = true;
            MoveTowards(playerTransform.position, runSpeed);

            // Đủ cự ly tấn công
            if (distToPlayer <= attackRange)
            {
                currentState = AnimalState.Attacking;
                isMoving = false;
                StopAgent();
            }

            // Người chơi chạy quá xa -> bỏ cuộc
            if (distToPlayer > detectionRange * 2.2f)
            {
                currentState = AnimalState.Idle;
                stateTimer = Random.Range(3f, 6f);
                isMoving = false;
                StopAgent();
            }
        }

        private void HandleAttacking(float distToPlayer)
        {
            isMoving = false;
            StopAgent();

            // Nhìn thẳng vào player
            Vector3 lookDir = (playerTransform.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 10f * Time.deltaTime);
            }

            // Đòn đánh theo nhịp
            if (Time.time >= nextAttackTime && distToPlayer <= attackRange)
            {
                nextAttackTime = Time.time + attackCooldown;
                AttackPlayer();
            }

            // Player lùi xa -> rượt tiếp
            if (distToPlayer > attackRange * 1.3f)
            {
                currentState = AnimalState.Chasing;
            }
        }

        private void AttackPlayer()
        {
            HorrorGame.Player.PlayerStats stats = playerTransform.GetComponent<HorrorGame.Player.PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(attackDamage);
                Debug.Log("🐗 [AnimalAI] " + GetAnimalName() + " tấn công người chơi! Sát thương: " + attackDamage);
            }
        }

        // ═══════════════════════════════════════════════════════
        // ĐIỀU HƯỚNG & DI CHUYỂN
        // ═══════════════════════════════════════════════════════

        private void MoveTowards(Vector3 destination, float speed)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.speed = speed;
                agent.SetDestination(destination);
            }
            else
            {
                // Fallback nếu không có NavMesh: di chuyển trực tiếp
                Vector3 dir = (destination - transform.position).normalized;
                dir.y = 0;
                if (dir.sqrMagnitude > 0.01f)
                {
                    transform.position += dir * speed * Time.deltaTime;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
                }
            }
        }

        private void StopAgent()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
        }

        private void PickNewWanderTarget()
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * 18f;
                Vector3 candidate = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

                NavMeshHit hit;
                if (NavMesh.SamplePosition(candidate, out hit, 10f, NavMesh.AllAreas))
                {
                    if (hit.position.y >= WATER_LEVEL)
                    {
                        wanderTarget = hit.position;
                        return;
                    }
                }
                else
                {
                    // Fallback Terrain
                    Terrain terrain = Terrain.activeTerrain;
                    if (terrain != null)
                    {
                        float ty = terrain.SampleHeight(candidate) + terrain.transform.position.y;
                        if (ty >= WATER_LEVEL)
                        {
                            wanderTarget = new Vector3(candidate.x, ty, candidate.z);
                            return;
                        }
                    }
                }
            }

            wanderTarget = transform.position;
        }

        private void ClampToGroundAndLand()
        {
            groundCheckTimer -= Time.deltaTime;
            if (groundCheckTimer > 0f) return;
            groundCheckTimer = 0.25f;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                // NavMeshAgent đã tự bám đất
                return;
            }

            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 8f, Vector3.down, out hit, 20f))
            {
                Vector3 pos = transform.position;
                pos.y = hit.point.y;
                transform.position = pos;
            }
        }

        // ═══════════════════════════════════════════════════════
        // HOẠT ẢNH ĐỘNG HỌC (PROCEDURAL ANIMATION)
        // ═══════════════════════════════════════════════════════

        private void UpdateProceduralAnimation()
        {
            if (isMoving)
            {
                float animSpeed = (currentState == AnimalState.Fleeing || currentState == AnimalState.Chasing) ? 14f : 8f;
                animTimer += Time.deltaTime * animSpeed;

                // 1. Chân bước tới lui xen kẽ tự nhiên
                float legAngle = Mathf.Sin(animTimer) * 28f;
                if (legTransforms != null && legTransforms.Length >= 4)
                {
                    if (legTransforms[0] != null) legTransforms[0].localRotation = Quaternion.Euler(legAngle, 0, 0);
                    if (legTransforms[1] != null) legTransforms[1].localRotation = Quaternion.Euler(-legAngle, 0, 0);
                    if (legTransforms[2] != null) legTransforms[2].localRotation = Quaternion.Euler(-legAngle, 0, 0);
                    if (legTransforms[3] != null) legTransforms[3].localRotation = Quaternion.Euler(legAngle, 0, 0);
                }

                // 2. Thỏ nhảy tưng tưng (Hop)
                if (animalType == AnimalType.Rabbit && bodyTransform != null)
                {
                    float hopY = Mathf.Abs(Mathf.Sin(animTimer)) * 0.22f;
                    bodyTransform.localPosition = new Vector3(0, baseBodyY + hopY, 0);
                }
            }
            else
            {
                // Khi đứng yên: duỗi thẳng chân lại
                if (legTransforms != null)
                {
                    foreach (var leg in legTransforms)
                    {
                        if (leg != null) leg.localRotation = Quaternion.Slerp(leg.localRotation, Quaternion.identity, 8f * Time.deltaTime);
                    }
                }

                if (animalType == AnimalType.Rabbit && bodyTransform != null)
                {
                    bodyTransform.localPosition = Vector3.Lerp(bodyTransform.localPosition, new Vector3(0, baseBodyY, 0), 8f * Time.deltaTime);
                }
            }

            // 3. Hoạt ảnh gặm cỏ (cúi đầu)
            if (headTransform != null)
            {
                if (currentState == AnimalState.Eating)
                {
                    headTransform.localRotation = Quaternion.Slerp(headTransform.localRotation, Quaternion.Euler(42f, 0, 0), 4f * Time.deltaTime);
                }
                else
                {
                    headTransform.localRotation = Quaternion.Slerp(headTransform.localRotation, Quaternion.identity, 6f * Time.deltaTime);
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        // CHIẾN ĐẤU & THU HOẠCH
        // ═══════════════════════════════════════════════════════

        public void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHealth -= damage;
            showHealthBar = true;
            healthBarTimer = 3.5f;

            Debug.Log("🎯 [AnimalAI] " + gameObject.name + " trúng đòn! HP: " + Mathf.Max(0, currentHealth) + "/" + maxHealth);

            // Bị đánh -> hoảng sợ hoặc phản công điên cuồng
            if (!isAggressive)
            {
                currentState = AnimalState.Fleeing;
            }
            else
            {
                currentState = AnimalState.Chasing;
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;
            currentState = AnimalState.Dead;
            currentHealth = 0;
            showHealthBar = false;

            if (agent != null && agent.enabled)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }

            // Xoay nghiêng xác ngã trên mặt đất
            transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 90f);

            deathTimer = 45f; // Xác tồn tại 45 giây để thu hoạch
            Debug.Log("💀 [AnimalAI] " + gameObject.name + " đã ngã gục! Hãy tới gần và nhấn [E] để thu hoạch.");
        }

        private void HandleDeath()
        {
            deathTimer -= Time.deltaTime;

            // Người chơi nhìn vào xác và ấn E để mổ thịt
            if (playerLookingAtThis && Input.GetKeyDown(KeyCode.E) && !lootDropped)
            {
                CollectLoot();
            }

            // Xác tan biến sau khi đã thu hoạch hoặc hết hạn
            if (deathTimer <= 0f && lootDropped)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 3f * Time.deltaTime);
                if (transform.localScale.magnitude < 0.1f)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void CollectLoot()
        {
            lootDropped = true;

            InventoryManager inv = InventoryManager.Instance;
            if (inv == null)
            {
                Debug.LogWarning("[AnimalAI] Không tìm thấy InventoryManager!");
                return;
            }

            // Tải hoặc tạo item thịt sống
            ItemData rawMeat = Resources.Load<ItemData>("Items/raw_meat");
            if (rawMeat == null) rawMeat = Resources.Load<ItemData>("raw_meat");
            if (rawMeat == null)
            {
                rawMeat = ScriptableObject.CreateInstance<ItemData>();
                rawMeat.itemID = "raw_meat";
                rawMeat.itemName = "Thịt Sống";
                rawMeat.description = "Thịt tươi sống từ thú rừng. Cần nấu chín trước khi ăn.";
                rawMeat.itemType = ItemType.Consumable;
                rawMeat.isStackable = true;
                rawMeat.maxStack = 20;
            }
            inv.AddItem(rawMeat, meatDropAmount);

            // Tải hoặc tạo item da thú
            ItemData hide = Resources.Load<ItemData>("Items/animal_hide");
            if (hide == null) hide = Resources.Load<ItemData>("animal_hide");
            if (hide == null)
            {
                hide = ScriptableObject.CreateInstance<ItemData>();
                hide.itemID = "animal_hide";
                hide.itemName = "Da Thú";
                hide.description = "Da thú dùng để chế tạo cung tên, giáp da và túi đựng.";
                hide.itemType = ItemType.Resource;
                hide.isStackable = true;
                hide.maxStack = 20;
            }
            inv.AddItem(hide, hideDropAmount);

            harvestNotice = string.Format("🥩 Thu hoạch: +{0} Thịt Sống, +{1} Da Thú!", meatDropAmount, hideDropAmount);
            harvestNoticeTimer = 3.5f;

            Debug.Log("🎒 [AnimalAI] " + harvestNotice);
        }

        private void CheckPlayerLooking()
        {
            playerLookingAtThis = false;
            Camera cam = Camera.main;
            if (cam == null) return;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 5f))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    playerLookingAtThis = true;
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        // GIAO DIỆN HIỂN THỊ (GUI)
        // ═══════════════════════════════════════════════════════

        void OnGUI()
        {
            InitStyles();

            // 1. Thanh máu trên đầu khi bị tấn công
            if (showHealthBar && !isDead)
            {
                DrawHealthBar();
            }

            // 2. Chữ tương tác khi nhìn vào xác hoặc thú sống
            if (playerLookingAtThis)
            {
                string label = "";
                if (isDead && !lootDropped)
                {
                    label = "[E] Thu hoạch " + GetAnimalName() + " (" + meatDropAmount + "x Thịt, " + hideDropAmount + "x Da)";
                }
                else if (!isDead)
                {
                    label = GetAnimalName() + (isAggressive ? " ⚠️ Hung Dữ" : " 🌿 Nhút Nhát");
                }

                if (!string.IsNullOrEmpty(label))
                {
                    Rect pos = new Rect(Screen.width / 2 - 200, Screen.height / 2 + 50, 400, 35);
                    GUI.Label(new Rect(pos.x + 2, pos.y + 2, pos.width, pos.height), label, shadowStyle);
                    GUI.Label(pos, label, interactStyle);
                }
            }

            // 3. Thông báo vừa thu hoạch thành công
            if (harvestNoticeTimer > 0f && !string.IsNullOrEmpty(harvestNotice))
            {
                Rect noticeRect = new Rect(Screen.width / 2 - 250, Screen.height / 2 - 60, 500, 40);
                GUI.Label(new Rect(noticeRect.x + 2, noticeRect.y + 2, noticeRect.width, noticeRect.height), harvestNotice, shadowStyle);
                GUI.Label(noticeRect, harvestNotice, interactStyle);
            }
        }

        private void DrawHealthBar()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 worldPos = transform.position + Vector3.up * 1.8f;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0) return;

            float barWidth = 90f;
            float barHeight = 9f;
            float x = screenPos.x - barWidth / 2;
            float y = Screen.height - screenPos.y - barHeight / 2;

            // Nền đen
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(x - 1, y - 1, barWidth + 2, barHeight + 2), Texture2D.whiteTexture);

            // Cột máu
            float hpPercent = Mathf.Clamp01(currentHealth / maxHealth);
            GUI.color = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.2f, 0.85f, 0.3f), hpPercent);
            GUI.DrawTexture(new Rect(x, y, barWidth * hpPercent, barHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void InitStyles()
        {
            if (interactStyle == null)
            {
                interactStyle = new GUIStyle(GUI.skin.label);
                interactStyle.fontSize = 17;
                interactStyle.fontStyle = FontStyle.Bold;
                interactStyle.alignment = TextAnchor.MiddleCenter;
                interactStyle.normal.textColor = new Color(1f, 0.95f, 0.6f);

                shadowStyle = new GUIStyle(interactStyle);
                shadowStyle.normal.textColor = Color.black;
            }
        }

        public string GetAnimalName()
        {
            switch (animalType)
            {
                case AnimalType.Deer: return "Hươu Rừng";
                case AnimalType.Rabbit: return "Thỏ Rừng";
                case AnimalType.Boar: return "Lợn Rừng";
                default: return "Thú Rừng";
            }
        }
    }
}

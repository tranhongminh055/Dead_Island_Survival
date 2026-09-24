using UnityEngine;
using HorrorGame.Inventory;

namespace HorrorGame.Survival
{
    public enum AnimalType
    {
        Deer,       // Hươu - nhát gan, chạy nhanh
        Rabbit,     // Thỏ - rất nhỏ, siêu nhanh
        Boar        // Lợn rừng - hung dữ, tấn công lại
    }

    public enum AnimalState
    {
        Idle,       // Đứng yên
        Wandering,  // Đi lang thang
        Eating,     // Ăn cỏ
        Fleeing,    // Chạy trốn player
        Chasing,    // Đuổi theo player (chỉ Boar)
        Attacking,  // Tấn công player (chỉ Boar)
        Dead        // Đã chết
    }

    /// <summary>
    /// AI cho thú rừng - Hệ thống săn bắt kiểu The Forest.
    /// Thú có thể đi lang thang, ăn cỏ, phát hiện player, chạy trốn hoặc tấn công.
    /// Player giết thú bằng rìu/súng → rơi thịt sống + da thú.
    /// </summary>
    public class AnimalAI : MonoBehaviour
    {
        [Header("── Thông Số ──")]
        public AnimalType animalType = AnimalType.Deer;
        public float maxHealth = 60f;
        public float moveSpeed = 3.5f;
        public float runSpeed = 8f;
        public float detectionRange = 20f;  // Phát hiện player
        public float fleeRange = 35f;       // Chạy bao xa mới dừng
        public bool isAggressive = false;   // Tấn công player?
        public float attackDamage = 15f;
        public float attackRange = 2.5f;
        public float attackCooldown = 1.5f;

        [Header("── Loot ──")]
        public int meatDropAmount = 3;
        public int hideDropAmount = 2;

        [HideInInspector] public Transform playerTransform;

        // State
        private AnimalState currentState = AnimalState.Idle;
        private float currentHealth;
        private float stateTimer = 0f;
        private Vector3 wanderTarget;
        private float nextAttackTime = 0f;

        // Raycast
        private float groundCheckTimer = 0f;

        // HP Bar
        private bool showHealthBar = false;
        private float healthBarTimer = 0f;

        // Death
        private bool isDead = false;
        private float deathTimer = 0f;
        private bool lootDropped = false;

        // UI
        private bool playerLookingAtThis = false;
        private GUIStyle interactStyle;
        private GUIStyle shadowStyle;

        public void Initialize()
        {
            currentHealth = maxHealth;
            currentState = AnimalState.Idle;
            stateTimer = Random.Range(2f, 5f);
            PickNewWanderTarget();
        }

        void Update()
        {
            if (isDead)
            {
                HandleDeath();
                return;
            }

            if (playerTransform == null) return;

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            // Cập nhật state
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

            // Giữ trên mặt đất
            SnapToGround();

            // Kiểm tra player nhìn vào
            CheckPlayerLooking();

            // Cập nhật HP bar timer
            if (showHealthBar)
            {
                healthBarTimer -= Time.deltaTime;
                if (healthBarTimer <= 0f) showHealthBar = false;
            }
        }

        // ═══════════════════════════════════════════════════════
        // STATE HANDLERS
        // ═══════════════════════════════════════════════════════

        private void HandleIdle(float distToPlayer)
        {
            stateTimer -= Time.deltaTime;

            // Phát hiện player
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
                // Chuyển sang đi lang thang hoặc ăn
                float roll = Random.value;
                if (roll < 0.6f)
                {
                    currentState = AnimalState.Wandering;
                    PickNewWanderTarget();
                    stateTimer = Random.Range(5f, 12f);
                }
                else
                {
                    currentState = AnimalState.Eating;
                    stateTimer = Random.Range(3f, 8f);
                }
            }
        }

        private void HandleWandering(float distToPlayer)
        {
            // Phát hiện player
            if (distToPlayer < detectionRange)
            {
                currentState = isAggressive ? AnimalState.Chasing : AnimalState.Fleeing;
                return;
            }

            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                currentState = AnimalState.Idle;
                stateTimer = Random.Range(2f, 5f);
                return;
            }

            // Di chuyển về phía target
            Vector3 direction = (wanderTarget - transform.position).normalized;
            direction.y = 0;

            if (direction.sqrMagnitude > 0.01f)
            {
                transform.position += direction * moveSpeed * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(direction), 5f * Time.deltaTime);
            }

            // Đã tới target
            if (Vector3.Distance(transform.position, wanderTarget) < 2f)
            {
                currentState = AnimalState.Idle;
                stateTimer = Random.Range(2f, 5f);
            }
        }

        private void HandleEating(float distToPlayer)
        {
            // Phát hiện player
            if (distToPlayer < detectionRange)
            {
                currentState = isAggressive ? AnimalState.Chasing : AnimalState.Fleeing;
                return;
            }

            stateTimer -= Time.deltaTime;

            // Animation ăn cỏ: cúi đầu xuống
            // (Sẽ thêm animation sau, placeholder: xoay nhẹ)

            if (stateTimer <= 0f)
            {
                currentState = AnimalState.Idle;
                stateTimer = Random.Range(2f, 4f);
            }
        }

        private void HandleFleeing(float distToPlayer)
        {
            // Chạy xa khỏi player
            Vector3 awayFromPlayer = (transform.position - playerTransform.position).normalized;
            awayFromPlayer.y = 0;

            transform.position += awayFromPlayer * runSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(awayFromPlayer), 10f * Time.deltaTime);

            // Đã chạy đủ xa
            if (distToPlayer > fleeRange || distToPlayer > detectionRange * 2f)
            {
                currentState = AnimalState.Idle;
                stateTimer = Random.Range(3f, 6f);
            }
        }

        private void HandleChasing(float distToPlayer)
        {
            if (!isAggressive)
            {
                currentState = AnimalState.Fleeing;
                return;
            }

            // Đuổi theo player
            Vector3 toPlayer = (playerTransform.position - transform.position).normalized;
            toPlayer.y = 0;

            transform.position += toPlayer * runSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(toPlayer), 10f * Time.deltaTime);

            // Đủ gần để tấn công
            if (distToPlayer < attackRange)
            {
                currentState = AnimalState.Attacking;
            }

            // Player chạy quá xa → bỏ cuộc
            if (distToPlayer > detectionRange * 2.5f)
            {
                currentState = AnimalState.Idle;
                stateTimer = Random.Range(3f, 6f);
            }
        }

        private void HandleAttacking(float distToPlayer)
        {
            if (!isAggressive) return;

            // Quay mặt về player
            Vector3 toPlayer = (playerTransform.position - transform.position).normalized;
            toPlayer.y = 0;
            transform.rotation = Quaternion.LookRotation(toPlayer);

            // Tấn công theo cooldown
            if (Time.time >= nextAttackTime && distToPlayer < attackRange)
            {
                nextAttackTime = Time.time + attackCooldown;
                AttackPlayer();
            }

            // Player chạy xa → đuổi theo
            if (distToPlayer > attackRange * 1.5f)
            {
                currentState = AnimalState.Chasing;
            }
        }

        private void AttackPlayer()
        {
            // Gây sát thương cho player
            HorrorGame.Player.PlayerStats stats = playerTransform.GetComponent<HorrorGame.Player.PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(attackDamage);
                Debug.Log("🐗 " + animalType + " tấn công player! Sát thương: " + attackDamage);
            }
        }

        // ═══════════════════════════════════════════════════════
        // NHẬN SÁT THƯƠNG
        // ═══════════════════════════════════════════════════════

        public void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHealth -= damage;
            showHealthBar = true;
            healthBarTimer = 3f;

            Debug.Log("🎯 " + gameObject.name + " bị đánh! HP: " + currentHealth + "/" + maxHealth);

            // Bị đánh → chạy trốn hoặc tức giận
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

            Debug.Log("💀 " + gameObject.name + " đã chết!");

            // Hiệu ứng ngã: xoay ngang
            transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 90);

            deathTimer = 30f; // Xác tồn tại 30 giây
        }

        private void HandleDeath()
        {
            deathTimer -= Time.deltaTime;

            // Player nhìn vào xác và nhấn E để thu thập
            if (playerLookingAtThis && Input.GetKeyDown(KeyCode.E) && !lootDropped)
            {
                CollectLoot();
            }

            // Sau 30 giây xác biến mất
            if (deathTimer <= 0f && lootDropped)
            {
                // Fade ra (đơn giản: scale nhỏ dần)
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 2f * Time.deltaTime);
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
            if (inv == null) return;

            // Thêm thịt sống
            ItemData rawMeat = GetOrCreateItemData("raw_meat", "Thịt Sống", 
                "Thịt sống từ thú rừng. Cần nấu chín trước khi ăn.", 
                ItemType.Consumable, true, 10);
            inv.AddItem(rawMeat, meatDropAmount);

            // Thêm da thú
            ItemData hide = GetOrCreateItemData("animal_hide", "Da Thú", 
                "Da thú dùng để chế tạo giáp và túi đựng.", 
                ItemType.Resource, true, 20);
            inv.AddItem(hide, hideDropAmount);

            Debug.Log("🎒 Thu hoạch: " + meatDropAmount + "x Thịt Sống, " + hideDropAmount + "x Da Thú");
        }

        // ═══════════════════════════════════════════════════════
        // HELPER
        // ═══════════════════════════════════════════════════════

        private void PickNewWanderTarget()
        {
            Vector2 random = Random.insideUnitCircle * 15f;
            wanderTarget = transform.position + new Vector3(random.x, 0, random.y);
        }

        private void SnapToGround()
        {
            groundCheckTimer -= Time.deltaTime;
            if (groundCheckTimer > 0f) return;
            groundCheckTimer = 0.2f; // Mỗi 0.2 giây

            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out hit, 20f))
            {
                Vector3 pos = transform.position;
                pos.y = hit.point.y;
                transform.position = pos;
            }
        }

        private void CheckPlayerLooking()
        {
            playerLookingAtThis = false;
            if (playerTransform == null) return;

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

        private ItemData GetOrCreateItemData(string id, string name, string desc, ItemType type, bool stackable, int maxStack)
        {
            // Thử load từ Resources trước
            ItemData item = Resources.Load<ItemData>("Items/" + id);
            if (item != null) return item;

            item = Resources.Load<ItemData>(id);
            if (item != null) return item;

            // Tạo runtime
            item = ScriptableObject.CreateInstance<ItemData>();
            item.itemID = id;
            item.itemName = name;
            item.description = desc;
            item.itemType = type;
            item.isStackable = stackable;
            item.maxStack = maxStack;
            return item;
        }

        // ═══════════════════════════════════════════════════════
        // UI
        // ═══════════════════════════════════════════════════════

        void OnGUI()
        {
            if (playerTransform == null) return;

            // Khởi tạo style
            if (interactStyle == null)
            {
                interactStyle = new GUIStyle(GUI.skin.label);
                interactStyle.fontSize = 18;
                interactStyle.fontStyle = FontStyle.Bold;
                interactStyle.alignment = TextAnchor.MiddleCenter;
                interactStyle.normal.textColor = Color.white;

                shadowStyle = new GUIStyle(interactStyle);
                shadowStyle.normal.textColor = Color.black;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);

            // Hiển thị HP bar khi bị đánh
            if (showHealthBar && !isDead && dist < 30f)
            {
                DrawHealthBar();
            }

            // Hiển thị tên thú khi player nhìn vào
            if (playerLookingAtThis && dist < 10f)
            {
                string label = "";
                if (isDead && !lootDropped)
                {
                    label = "[E] Thu hoạch " + GetAnimalName();
                }
                else if (!isDead)
                {
                    label = GetAnimalName();
                    if (isAggressive) label += " ⚠️";
                }

                if (!string.IsNullOrEmpty(label))
                {
                    Rect pos = new Rect(Screen.width / 2 - 150, Screen.height / 2 + 40, 300, 30);
                    GUI.Label(new Rect(pos.x + 2, pos.y + 2, pos.width, pos.height), label, shadowStyle);
                    GUI.Label(pos, label, interactStyle);
                }
            }
        }

        private void DrawHealthBar()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 worldPos = transform.position + Vector3.up * 2.5f;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0) return;

            float barWidth = 80f;
            float barHeight = 8f;
            float x = screenPos.x - barWidth / 2;
            float y = Screen.height - screenPos.y - barHeight / 2;

            // Background
            GUI.DrawTexture(new Rect(x - 1, y - 1, barWidth + 2, barHeight + 2), Texture2D.whiteTexture);

            // HP fill
            float hpPercent = currentHealth / maxHealth;
            Color hpColor = Color.Lerp(Color.red, Color.green, hpPercent);
            Color prevColor = GUI.color;
            GUI.color = hpColor;
            GUI.DrawTexture(new Rect(x, y, barWidth * hpPercent, barHeight), Texture2D.whiteTexture);
            GUI.color = prevColor;
        }

        private string GetAnimalName()
        {
            switch (animalType)
            {
                case AnimalType.Deer: return "Hươu";
                case AnimalType.Rabbit: return "Thỏ";
                case AnimalType.Boar: return "Lợn Rừng";
                default: return "Thú";
            }
        }
    }
}

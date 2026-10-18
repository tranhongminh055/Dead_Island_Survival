using UnityEngine;
using UnityEngine.AI;
using HorrorGame.Player;
using HorrorGame.Inventory;
using System.Collections;

namespace HorrorGame.Enemy
{
    /// <summary>
    /// AI Thổ Dân Ăn Thịt Người (Cannibal Tribal) phong cách The Forest:
    /// - Nhanh nhẹn, hung tợn hơn Zombie rất nhiều (Tốc độ chạy 5.2m/s, sát thương 14 HP).
    /// - Khi phát hiện người chơi: Hú vang tiếng hú rợn người làm rung chuyển tâm lý (-5 Sanity) và gọi bầy đàn.
    /// - Tấn công bằng Gậy chiến / Giáo gỗ, có nguy cơ lây nhiễm ký sinh trùng.
    /// - Biết bỏ chạy (Flee) lẩn vào rừng cây khi máu xuống thấp (< 25 HP).
    /// - Cho phép người chơi nhấn [E] thu hoạch Xương người (Bone) và Thịt sống (Raw Meat) sau khi bị hạ gục.
    /// </summary>
    public class CannibalAI : EnemyAI
    {
        [Header("── Đặc Điểm Thổ Dân ──")]
        public string tribalTitle = "Thổ dân ăn thịt người";
        public float warCryRange = 36f;
        public float fleeHealthThreshold = 25f;

        [Header("── Âm Thanh Tiếng Hú & Gầm ──")]
        public AudioClip warCryClip;  // Tiếng tù và chiến trận (Horn)
        public AudioClip howlClip;    // Tiếng hú rợn người (WindHowl)
        public AudioClip growlClip;   // Tiếng gầm gừ man rợ khi đánh (DeepRattle)

        // Trạng thái hành vi
        private float warCryCooldown = 0f;
        private bool isPerformingWarCry = false;
        private bool isFleeing = false;
        private float fleeTimer = 0f;
        private bool isHarvested = false;

        private AudioSource tribalAudio;
        private GameObject weaponObj;
        private GameObject torchObj;
        private Light torchLight;
        private float stalkTimer = 0f;
        private float nextStalkDelay = 3f;

        // UI Prompt
        private GUIStyle promptStyle;
        private GUIStyle promptShadowStyle;

        void Awake()
        {
            EnsureCollider();
            LoadAudioClips();
        }

        protected override void Start()
        {
            // Thiết lập chỉ số chiến binh thổ dân
            maxHealth = 85f;
            walkSpeed = 2.4f;
            runSpeed = 5.2f; // Nhanh hơn zombie rất nhiều
            damage = 14f;    // Bổ gậy rất đau
            attackRange = 2.4f;
            attackCooldown = 1.3f;
            chaseRange = 36f;
            warCryRange = 36f; // Phát hiện người chơi trong tầm là lập tức hú vang

            base.Start();

            SetupAudioSource();
            LoadAudioClips();
            BuildTribalAppearance();
        }

        private void SetupAudioSource()
        {
            tribalAudio = GetComponent<AudioSource>();
            if (tribalAudio == null)
            {
                tribalAudio = gameObject.AddComponent<AudioSource>();
            }
            tribalAudio.playOnAwake = false;
            tribalAudio.loop = false;
            // 0.35f Spatial Blend: Người chơi nghe rất to và rõ tiếng hú vọng trong rừng dù ở cách 35-50m
            // đồng thời vẫn có hướng âm thanh 3D lập thể để định vị góc thổ dân tiếp cận
            tribalAudio.spatialBlend = 0.35f;
            tribalAudio.minDistance = 8f;
            tribalAudio.maxDistance = 70f;
            tribalAudio.rolloffMode = AudioRolloffMode.Linear;
            tribalAudio.volume = 1f;
        }

        public void LoadAudioClips()
        {
#if UNITY_EDITOR
            // 1. Nạp file âm thanh tiếng sói hú chính thức mà bạn yêu cầu
            if (warCryClip == null)
            {
                warCryClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/tiếng sói hú/ribhavagrawal-wolf-howling-type-04-293317.mp3");
                if (warCryClip == null)
                {
                    warCryClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/Cannibal/Cannibal_WarCry.mp3");
                }
            }
            if (howlClip == null)
            {
                howlClip = warCryClip; // Dùng chung tiếng sói hú
            }
            if (growlClip == null)
            {
                growlClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/DeepRattle.mp3");
            }
#endif
            if (warCryClip == null) warCryClip = Resources.Load<AudioClip>("Sounds/WolfHowl");
            if (warCryClip == null) warCryClip = Resources.Load<AudioClip>("Sounds/Cannibal/Cannibal_WarCry");
            if (howlClip == null) howlClip = warCryClip;
            if (growlClip == null) growlClip = Resources.Load<AudioClip>("Sounds/DeepRattle");

            // Fallback âm thanh rống tổng hợp nếu chưa có file asset
            if (warCryClip == null && howlClip == null)
            {
                warCryClip = GenerateTribalWarCryClip();
            }
        }

        /// <summary>
        /// Tạo âm thanh hú rùng rợn tổng hợp procedural đề phòng trường hợp asset âm thanh chưa kịp import
        /// </summary>
        private AudioClip GenerateTribalWarCryClip()
        {
            int sampleRate = 44100;
            float duration = 1.6f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = 260f + 260f * Mathf.Sin(t * Mathf.PI / duration);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                float rattle = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 30f * t);
                float noise = (Random.value * 2f - 1f) * 0.25f;
                float envelope = Mathf.Clamp01(t * 5f) * Mathf.Clamp01((duration - t) * 3f);
                samples[i] = (wave * 0.7f * rattle + noise) * envelope * 0.85f;
            }

            AudioClip clip = AudioClip.Create("ProceduralTribalRoar", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        protected override void Update()
        {
            if (isDead)
            {
                HandleDeadHarvestPrompt();
                return;
            }

            // Đang trong Cutscene máy bay: Không hú hét, không di chuyển
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive)
            {
                return;
            }

            // Hiệu ứng ngọn đuốc lập lòe trong đêm
            if (torchLight != null)
            {
                torchLight.intensity = 2.0f + Mathf.Sin(Time.time * 9f) * 0.35f;
            }

            if (player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
                else if (Camera.main != null) player = Camera.main.transform;
            }

            if (warCryCooldown > 0f) warCryCooldown -= Time.deltaTime;

            // 1. TRẠNG THÁI BỎ CHẠY KHI BỊ THƯƠNG NẶNG (FLEEING)
            if (currentHealth > 0 && currentHealth <= fleeHealthThreshold)
            {
                HandleFleeing();
                return;
            }

            // 2. ĐANG TRONG QUÁ TRÌNH THỰC HIỆN TIẾNG HÚ (ĐỨNG LẠI HÚ)
            if (isPerformingWarCry)
            {
                return;
            }

            // 3. TIẾNG HÚ CHIẾN TRẬN KHI PHÁT HIỆN NGƯỜI CHƠI (WARCHANT ROAR)
            // Chỉ hú vào ban đêm (19:15 - 05:15). Ban ngày thổ dân vẫn tấn công nhưng không hú.
            bool isNightTime = true;
            if (HorrorGame.Environment.DayNightCycle.Instance != null)
            {
                isNightTime = HorrorGame.Environment.DayNightCycle.Instance.IsNight();
            }

            if (isNightTime && player != null && warCryCooldown <= 0f)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist <= warCryRange)
                {
                    StartCoroutine(PerformWarcry());
                    return;
                }
            }

            // 4. GẦM GỪ KHI CHUẨN BỊ VUNG VŨ KHÍ TẤN CÔNG
            if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange)
            {
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    PlayAttackGrowl();
                }
            }

            // 5. NẾU Ở NGOÀI TẦM RƯỢT ĐUỔI (> 36m): RÌNH RẬP TIẾP CẬN NGƯỜI CHƠI (STALKING)
            if (player != null && Vector3.Distance(transform.position, player.position) > chaseRange)
            {
                HandleStalking();
                return;
            }

            // 6. HÀNH VI DI CHUYỂN & TẤN CÔNG GỐC CỦA KẺ ĐỊCH KHI TRONG TẦM
            base.Update();
        }

        /// <summary>
        /// Thổ dân âm thầm rình rập, thu hẹp khoảng cách với người chơi từ trong bóng tối
        /// </summary>
        private void HandleStalking()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh || player == null) return;

            stalkTimer += Time.deltaTime;
            if (stalkTimer >= nextStalkDelay)
            {
                stalkTimer = 0f;
                nextStalkDelay = Random.Range(3.5f, 6.5f);

                // Điểm rình rập hướng dần về phía người chơi
                Vector3 dirToPlayer = (player.position - transform.position).normalized;
                Vector3 stalkPoint = transform.position + dirToPlayer * Random.Range(14f, 20f) + Random.insideUnitSphere * 6f;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(stalkPoint, out hit, 15f, NavMesh.AllAreas))
                {
                    agent.speed = walkSpeed * 1.35f;
                    agent.SetDestination(hit.position);
                    if (animator != null && animator.enabled)
                    {
                        animator.SetFloat("Speed", 0.6f);
                    }
                }
            }

            if (agent.remainingDistance <= 1.5f && animator != null && animator.enabled)
            {
                animator.SetFloat("Speed", 0f);
            }
        }

        /// <summary>
        /// Tiếng hú gọi bầy và khủng bố tinh thần người chơi chuẩn The Forest
        /// </summary>
        private IEnumerator PerformWarcry()
        {
            isPerformingWarCry = true;
            warCryCooldown = Random.Range(25f, 35f); // Hồi chiêu 25-35s trước khi hú tiếp

            // Dừng lại hú hét 1.4 giây
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            // Hướng mặt về người chơi
            if (player != null)
            {
                transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
            }

            // Phát âm thanh tiếng hú / tù và chiến trận rợn người
            PlayWarCrySound();

            // Cảnh báo người chơi khi thổ dân hú gọi bầy (KHÔNG trừ Sanity)
            if (player != null)
            {
                PlayerStats stats = player.GetComponentInParent<PlayerStats>();
                if (stats == null) stats = player.GetComponentInChildren<PlayerStats>();
                if (stats != null)
                {
                    stats.ShowStatusNotice("⚠️ Tiếng hú của Thổ dân ăn thịt người! Chúng đang gọi đàn kéo tới!");
                }
            }

            // Kêu gọi các thổ dân khác trong bán kính 45m cùng lao vào
            AlertNearbyTribals(transform.position, 45f);

            yield return new WaitForSeconds(1.4f);

            isPerformingWarCry = false;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
        }

        private void PlayWarCrySound()
        {
            if (tribalAudio == null) SetupAudioSource();
            if (tribalAudio == null) return;

            if (warCryClip == null && howlClip == null) LoadAudioClips();

            AudioClip clip = warCryClip != null ? warCryClip : howlClip;

            if (clip != null)
            {
                tribalAudio.pitch = Random.Range(0.95f, 1.04f);
                tribalAudio.PlayOneShot(clip, 1.0f);
                Debug.Log("[CannibalAI] 🐺 Thổ dân đang HÚ TIẾNG SÓI: " + clip.name + " (Volume: 1.0)");
            }
            else
            {
                AudioClip synthClip = GenerateTribalWarCryClip();
                tribalAudio.PlayOneShot(synthClip, 1.0f);
            }
        }

        private void PlayAttackGrowl()
        {
            if (tribalAudio != null && growlClip != null && !tribalAudio.isPlaying)
            {
                tribalAudio.pitch = Random.Range(0.85f, 1.15f);
                tribalAudio.PlayOneShot(growlClip, 0.9f);
            }
        }

        private void AlertNearbyTribals(Vector3 origin, float radius)
        {
            CannibalAI[] allTribals = FindObjectsOfType<CannibalAI>();
            foreach (var t in allTribals)
            {
                if (t != null && !t.IsDead && t != this)
                {
                    if (Vector3.Distance(t.transform.position, origin) <= radius)
                    {
                        t.InvestigateSound(player != null ? player.position : origin);
                    }
                }
            }
        }

        /// <summary>
        /// Thổ dân sợ chết, quay đầu bỏ chạy vào rừng cây khi máu tụt quá thấp
        /// </summary>
        private void HandleFleeing()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

            fleeTimer -= Time.deltaTime;
            if (fleeTimer <= 0f)
            {
                fleeTimer = 2.5f;
                // Chạy ngược hướng với người chơi
                Vector3 fleeDir = (transform.position - player.position).normalized;
                Vector3 fleeTarget = transform.position + fleeDir * 25f + Random.insideUnitSphere * 8f;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(fleeTarget, out hit, 20f, NavMesh.AllAreas))
                {
                    agent.speed = runSpeed * 1.1f;
                    agent.SetDestination(hit.position);
                }
            }

            if (animator != null && animator.enabled)
            {
                animator.SetFloat("Speed", 1f);
            }
        }

        public override void TakeDamage(float amount, Vector3 hitPoint)
        {
            base.TakeDamage(amount, hitPoint);

            // Bị đánh đau: gầm gừ giận dữ
            if (currentHealth > 0 && !isDead)
            {
                if (growlClip != null && tribalAudio != null && !tribalAudio.isPlaying)
                {
                    tribalAudio.pitch = Random.Range(1.1f, 1.35f);
                    tribalAudio.PlayOneShot(growlClip, 0.85f);
                }
            }
        }

        protected override void Die()
        {
            base.Die();
            Debug.Log("[CannibalAI] Thổ dân ăn thịt người đã bị tiêu diệt!");

            // Rơi vũ khí gậy gỗ xuống đất
            if (weaponObj != null)
            {
                weaponObj.transform.SetParent(null);
                Rigidbody rb = weaponObj.AddComponent<Rigidbody>();
                rb.mass = 1.5f;
                Destroy(weaponObj, 60f); // Tự dọn sau 60s
            }

            // Đuốc lửa rơi xuống và tàn dần
            if (torchLight != null)
            {
                torchLight.range = 6f;
                torchLight.intensity = 0.8f;
            }
            if (torchObj != null)
            {
                torchObj.transform.SetParent(null);
                Rigidbody trb = torchObj.AddComponent<Rigidbody>();
                trb.mass = 1.0f;
                Destroy(torchObj, 60f);
            }
        }

        /// <summary>
        /// Cho phép người chơi nhấn [E] thu hoạch Xương người và Thịt sống
        /// </summary>
        private void HandleDeadHarvestPrompt()
        {
            if (isHarvested || player == null) return;

            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= 2.6f)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    HarvestCorpse();
                }
            }
        }

        private void HarvestCorpse()
        {
            isHarvested = true;

            InventoryManager inv = InventoryManager.Instance;
            if (inv == null) inv = FindObjectOfType<InventoryManager>();

            if (inv != null)
            {
                // 1. Thưởng Thịt sống (Raw Meat - load asset chính thức có icon)
                ItemData rawMeat = Resources.Load<ItemData>("Items/raw_meat");
                if (rawMeat == null)
                {
                    rawMeat = ScriptableObject.CreateInstance<ItemData>();
                    rawMeat.itemID = "raw_meat";
                    rawMeat.itemName = "Thịt tươi";
                    rawMeat.description = "Miếng thịt tươi sống. Cần nướng chín trên lửa trại trước khi ăn!";
                    rawMeat.itemType = ItemType.Consumable;
                    rawMeat.isStackable = true;
                    rawMeat.maxStack = 10;
                }
                inv.AddItem(rawMeat, 1);

                // 2. Thưởng Xương người (Bone)
                ItemData boneItem = ScriptableObject.CreateInstance<ItemData>();
                boneItem.itemID = "bone";
                boneItem.itemName = "Xương người";
                boneItem.description = "Xương từ thổ dân ăn thịt người. Dùng để chế tạo vũ khí xương và giáp.";
                boneItem.itemType = ItemType.Resource;
                boneItem.isStackable = true;
                boneItem.maxStack = 20;

                // Mượn icon từ item có sẵn
                ItemData wood = Resources.Load<ItemData>("Items/WoodItem");
                if (wood != null && wood.icon != null) boneItem.icon = wood.icon;

                inv.AddItem(boneItem, 2);

                // Cập nhật ngay UI nếu đang mở
                HorrorGame.Inventory.UI.InventoryUI ui = FindObjectOfType<HorrorGame.Inventory.UI.InventoryUI>();
                if (ui != null) ui.UpdateUI();
            }

            PlayerStats stats = player.GetComponentInParent<PlayerStats>();
            if (stats != null)
            {
                stats.ShowStatusNotice("🦴 Đã thu hoạch: 2 Xương người & 1 Thịt tươi!");
            }
        }

        /// <summary>
        /// Tạo hình thể, ngọn đuốc rực sáng và gậy chiến của Thổ Dân
        /// </summary>
        private void BuildTribalAppearance()
        {
            // Đổi màu da ngăm nâu và vẽ sơn chiến binh
            SkinnedMeshRenderer[] smrs = GetComponentsInChildren<SkinnedMeshRenderer>();
            Shader stdShader = Shader.Find("Standard");

            if (smrs != null && smrs.Length > 0 && stdShader != null)
            {
                foreach (var smr in smrs)
                {
                    if (smr != null && smr.material != null)
                    {
                        Material tribalMat = new Material(smr.material);
                        tribalMat.color = new Color(0.48f, 0.35f, 0.25f); // Da ngăm nâu chiến binh
                        smr.material = tribalMat;
                    }
                }
            }

            // Gắn gậy chiến (War Club) vào tay phải
            weaponObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            weaponObj.name = "TribalWarClub";
            Destroy(weaponObj.GetComponent<Collider>());
            weaponObj.transform.SetParent(this.transform);
            weaponObj.transform.localPosition = new Vector3(0.35f, 0.95f, 0.45f);
            weaponObj.transform.localRotation = Quaternion.Euler(45f, 15f, 0f);
            weaponObj.transform.localScale = new Vector3(0.08f, 0.55f, 0.08f);

            if (stdShader != null)
            {
                Material woodMat = new Material(stdShader);
                woodMat.color = new Color(0.25f, 0.15f, 0.08f); // Màu gỗ sồi sẫm dính máu
                weaponObj.GetComponent<Renderer>().material = woodMat;
            }

            // Gắn ngọn đuốc rực lửa (Tribal Torch) vào tay trái để chiếu sáng trong đêm
            torchObj = new GameObject("TribalTorch");
            torchObj.transform.SetParent(this.transform);
            torchObj.transform.localPosition = new Vector3(-0.35f, 1.15f, 0.4f);
            torchObj.transform.localRotation = Quaternion.Euler(15f, -20f, 0f);

            GameObject torchWood = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            torchWood.name = "TorchWood";
            Destroy(torchWood.GetComponent<Collider>());
            torchWood.transform.SetParent(torchObj.transform);
            torchWood.transform.localPosition = Vector3.zero;
            torchWood.transform.localScale = new Vector3(0.06f, 0.45f, 0.06f);
            if (stdShader != null)
            {
                Material hMat = new Material(stdShader);
                hMat.color = new Color(0.2f, 0.12f, 0.05f);
                torchWood.GetComponent<Renderer>().material = hMat;
            }

            GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "TorchFlame";
            Destroy(flame.GetComponent<Collider>());
            flame.transform.SetParent(torchObj.transform);
            flame.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            flame.transform.localScale = new Vector3(0.2f, 0.25f, 0.2f);

            Shader emitShader = Shader.Find("Mobile/Particles/Additive");
            if (emitShader == null) emitShader = Shader.Find("Particles/Additive");
            if (emitShader == null) emitShader = stdShader;
            if (emitShader != null)
            {
                Material fMat = new Material(emitShader);
                fMat.color = new Color(1.0f, 0.65f, 0.15f, 1f);
                flame.GetComponent<Renderer>().material = fMat;
            }

            // Nguồn sáng lập lòe của ngọn đuốc soi sáng rừng đêm
            torchLight = torchObj.AddComponent<Light>();
            torchLight.type = LightType.Point;
            torchLight.range = 14f;
            torchLight.intensity = 2.2f;
            torchLight.color = new Color(1.0f, 0.55f, 0.18f);
            torchLight.shadows = LightShadows.None;
        }

        void OnGUI()
        {
            if (!isDead || isHarvested || player == null) return;

            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= 2.6f)
            {
                if (promptStyle == null)
                {
                    promptStyle = new GUIStyle();
                    promptStyle.fontSize = 15;
                    promptStyle.fontStyle = FontStyle.Bold;
                    promptStyle.alignment = TextAnchor.MiddleCenter;
                    promptStyle.normal.textColor = new Color(1f, 0.85f, 0.3f, 1f);

                    promptShadowStyle = new GUIStyle(promptStyle);
                    promptShadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
                }

                string msg = "🦴 Nhấn [E] Thu hoạch xác Thổ Dân (Xương & Thịt)";
                float w = 450f;
                float h = 30f;
                float x = (Screen.width - w) * 0.5f;
                float y = Screen.height * 0.62f;

                GUI.Label(new Rect(x + 2, y + 2, w, h), msg, promptShadowStyle);
                GUI.Label(new Rect(x, y, w, h), msg, promptStyle);
            }
        }
    }
}

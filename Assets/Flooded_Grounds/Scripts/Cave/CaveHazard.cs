using UnityEngine;
using System.Collections;
using HorrorGame.Player;

namespace HorrorGame.Cave
{
    /// <summary>
    /// CaveHazard — Quản lý các loại bẫy và mối nguy hiểm môi trường trong hang động:
    /// 1. Spike Trap (Bẫy chông nhọn): Thổ dân bố trí dưới đất, dẫm phải nhận sát thương lớn và què chân.
    /// 2. Rope Trap (Bẫy thừng thòng lọng): Treo ngược hoặc giữ chân player 3-5 giây nếu không cắt dây [E].
    /// 3. Falling Stalactite (Thạch nhũ rơi): Rung lắc và rơi từ trần hang xuống khi player đi qua.
    /// 4. Heat Zone (Vùng nhiệt dung nham): Gây sát thương nhiệt liên tục trừ khi có mặt nạ bảo hộ.
    /// 5. Mud Pit (Hố bùn lầy): Làm chậm tốc độ di chuyển 50%.
    /// 6. Toxic Spores (Bào tử độc / ký sinh): Giảm Sanity và tăng độ nhiễm ký sinh trùng.
    /// </summary>
    public class CaveHazard : MonoBehaviour
    {
        public enum HazardType
        {
            SpikeTrap,
            RopeTrap,
            FallingStalactite,
            HeatZone,
            MudPit,
            ToxicSpores
        }

        [Header("── Loại Bẫy / Nguy Hiểm ──")]
        public HazardType hazardType = HazardType.SpikeTrap;

        [Header("── Thông Số Sát Thương & Tác Động ──")]
        [Tooltip("Sát thương một lần khi kích hoạt (Spike, Stalactite) hoặc sát thương/giây (Heat)")]
        public float damage = 25f;

        [Tooltip("Tốc độ giảm di chuyển (ví dụ 0.5 = giảm 50% tốc độ trong bùn)")]
        [Range(0.1f, 1f)]
        public float speedMultiplier = 0.5f;

        [Tooltip("Thời gian bị trói giữ chân của bẫy thừng (giây)")]
        public float trapTrappedDuration = 4f;

        [Tooltip("Khoảng cách kích hoạt thạch nhũ rơi")]
        public float triggerRadius = 3.5f;

        [Header("── Âm Thanh & Hiệu Ứng ──")]
        public AudioClip triggerSound;
        public ParticleSystem triggerParticle;
        public GameObject visualTrapObject; // Mô hình cọc nhọn, tảng đá hoặc dây

        [Header("── Trạng Thái ──")]
        public bool isTriggered = false;
        public bool canReset = false;
        public float resetDelay = 10f;

        private AudioSource audioSource;
        private Transform playerTransform;
        private PlayerController playerController;
        private PlayerStats playerStats;
        private Vector3 originalTrapPosition;
        private bool isPlayerInside = false;
        private float continuousDamageTimer = 0f;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.maxDistance = 20f;
                audioSource.playOnAwake = false;
            }

            if (visualTrapObject != null)
            {
                originalTrapPosition = visualTrapObject.transform.localPosition;
            }
        }

        private void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                playerController = playerObj.GetComponent<PlayerController>();
                playerStats = playerObj.GetComponent<PlayerStats>();
            }

            // Đảm bảo có Collider isTrigger
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                BoxCollider box = gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(2f, 1f, 2f);
            }
            else
            {
                col.isTrigger = true;
            }
        }

        private void Update()
        {
            if (playerTransform == null) return;

            // Xử lý Falling Stalactite phát hiện player ở bên dưới
            if (hazardType == HazardType.FallingStalactite && !isTriggered)
            {
                Vector3 flatTrapPos = new Vector3(transform.position.x, 0, transform.position.z);
                Vector3 flatPlayerPos = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
                float horizontalDist = Vector3.Distance(flatTrapPos, flatPlayerPos);

                // Nếu player ở ngay dưới thạch nhũ (và thấp hơn thạch nhũ)
                if (horizontalDist <= triggerRadius && playerTransform.position.y < transform.position.y)
                {
                    StartCoroutine(TriggerFallingStalactiteRoutine());
                }
            }

            // Xử lý sát thương liên tục theo thời gian (Heat Zone hoặc Toxic Spores)
            if (isPlayerInside)
            {
                HandleContinuousHazard();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            isPlayerInside = true;

            switch (hazardType)
            {
                case HazardType.SpikeTrap:
                    if (!isTriggered)
                    {
                        TriggerSpikeTrap();
                    }
                    break;

                case HazardType.RopeTrap:
                    if (!isTriggered)
                    {
                        StartCoroutine(TriggerRopeTrapRoutine());
                    }
                    break;

                case HazardType.MudPit:
                    ApplyMudSlowdown(true);
                    break;

                case HazardType.HeatZone:
                case HazardType.ToxicSpores:
                    // Được xử lý trong Update khi isPlayerInside = true
                    break;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            isPlayerInside = false;

            if (hazardType == HazardType.MudPit)
            {
                ApplyMudSlowdown(false);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // CÁC HÀNH VI BẪY CỤ THỂ
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Bẫy cọc nhọn phóng lên từ sàn đất
        /// </summary>
        private void TriggerSpikeTrap()
        {
            isTriggered = true;
            PlayTriggerAudio();

            if (triggerParticle != null)
                triggerParticle.Play();

            // Hiệu ứng cọc nhọn nhô lên nhanh
            if (visualTrapObject != null)
            {
                StartCoroutine(SpikeThrustRoutine());
            }

            // Sát thương player và gây gãy xương/què chân
            if (playerStats != null)
            {
                playerStats.TakeDamage(damage);
                Debug.Log(string.Format("<color=red>[CaveHazard]</color> Player đạp phải Bẫy Chông Nhọn! Nhận {0} sát thương!", damage));
            }

            // Nếu có Fracture System, kích hoạt đau đớn
            var fracture = playerTransform != null ? playerTransform.GetComponent<PlayerFractureSystem>() : null;
            if (fracture != null)
            {
                fracture.TriggerFracture(2.5f, 10f);
            }

            if (canReset)
            {
                StartCoroutine(ResetTrapRoutine());
            }
        }

        private IEnumerator SpikeThrustRoutine()
        {
            Vector3 startPos = originalTrapPosition;
            Vector3 poppedPos = originalTrapPosition + Vector3.up * 1.2f;

            // Đâm lên trong 0.08 giây (rất nhanh bất ngờ)
            float elapsed = 0f;
            while (elapsed < 0.08f)
            {
                elapsed += Time.deltaTime;
                visualTrapObject.transform.localPosition = Vector3.Lerp(startPos, poppedPos, elapsed / 0.08f);
                yield return null;
            }
            visualTrapObject.transform.localPosition = poppedPos;
        }

        /// <summary>
        /// Bẫy thòng lọng giữ chân player
        /// </summary>
        private IEnumerator TriggerRopeTrapRoutine()
        {
            isTriggered = true;
            PlayTriggerAudio();

            Debug.Log("<color=yellow>[CaveHazard]</color> Player bị mắc bẫy thừng thòng lọng!");

            // Tạm thời dừng di chuyển player
            if (playerController != null)
            {
                playerController.walkSpeed *= 0.1f;
                playerController.sprintSpeed *= 0.1f;
            }

            float trappedTimer = 0f;
            while (trappedTimer < trapTrappedDuration)
            {
                trappedTimer += Time.deltaTime;

                // Nếu người chơi nhấn E hoặc chuột trái liên tục để giãy giụa
                if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
                {
                    trappedTimer += 1.0f; // Thoát nhanh hơn
                }

                yield return null;
            }

            // Khôi phục tốc độ
            if (playerController != null)
            {
                playerController.walkSpeed = 3f;
                playerController.sprintSpeed = 6f;
            }

            Debug.Log("<color=green>[CaveHazard]</color> Player đã cắt đứt bẫy thừng và thoát ra!");

            if (canReset)
            {
                StartCoroutine(ResetTrapRoutine());
            }
        }

        /// <summary>
        /// Thạch nhũ rung lắc rồi rụng xuống đầu
        /// </summary>
        private IEnumerator TriggerFallingStalactiteRoutine()
        {
            isTriggered = true;

            // 1. Rung lắc báo trước trong 0.5s
            Vector3 originalPos = transform.position;
            float shakeTime = 0.5f;
            float elapsed = 0f;

            while (elapsed < shakeTime)
            {
                elapsed += Time.deltaTime;
                transform.position = originalPos + Random.insideUnitSphere * 0.08f;
                yield return null;
            }
            transform.position = originalPos;

            // 2. Thêm Rigidbody để rơi tự do
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 80f;
            rb.useGravity = true;

            PlayTriggerAudio();

            // 3. Chờ va chạm đất
            yield return new WaitForSeconds(3f);

            // Tạo mảnh vỡ vụn
            if (triggerParticle != null)
            {
                triggerParticle.transform.position = transform.position;
                triggerParticle.Play();
            }

            Destroy(gameObject, 5f);
        }

        /// <summary>
        /// Xử lý sát thương liên tục của Heat Zone hoặc Bào tử độc
        /// </summary>
        private void HandleContinuousHazard()
        {
            continuousDamageTimer += Time.deltaTime;
            if (continuousDamageTimer >= 1.0f)
            {
                continuousDamageTimer = 0f;

                if (hazardType == HazardType.HeatZone)
                {
                    // Kiểm tra item chống nhiệt nếu có
                    if (playerStats != null)
                    {
                        playerStats.TakeDamage(damage);
                    }
                }
                else if (hazardType == HazardType.ToxicSpores)
                {
                    // Giảm Sanity và tăng ký sinh trùng
                    if (playerStats != null)
                    {
                        playerStats.DrainSanity(3f);
                    }

                    var parasite = playerTransform != null ? playerTransform.GetComponent<ZombieParasiteSystem>() : null;
                    if (parasite != null)
                    {
                        parasite.TryInfect(0.25f, "Bào tử ký sinh hang động");
                    }
                }
            }
        }

        /// <summary>
        /// Làm chậm tốc độ di chuyển khi đi qua bùn lầy
        /// </summary>
        private void ApplyMudSlowdown(bool slowed)
        {
            if (playerController == null) return;

            if (slowed)
            {
                playerController.walkSpeed = 1.5f;
                playerController.sprintSpeed = 2.5f;
            }
            else
            {
                playerController.walkSpeed = 3.0f;
                playerController.sprintSpeed = 6.0f;
            }
        }

        private IEnumerator ResetTrapRoutine()
        {
            yield return new WaitForSeconds(resetDelay);

            isTriggered = false;
            if (visualTrapObject != null)
            {
                visualTrapObject.transform.localPosition = originalTrapPosition;
            }
        }

        private void PlayTriggerAudio()
        {
            if (triggerSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(triggerSound);
            }
        }

        private void OnGUI()
        {
            // Hiển thị prompt khi bị dính Bẫy thừng
            if (hazardType == HazardType.RopeTrap && isTriggered && isPlayerInside)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = 20;
                style.normal.textColor = Color.red;

                GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 + 50, 400, 40), "⚠️ BỊ MẮC BẪY THỪNG! NHẤN [E] LIÊN TỤC ĐỂ CẮT DÂY!", style);
            }
        }
    }
}

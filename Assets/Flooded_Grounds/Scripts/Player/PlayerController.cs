using UnityEngine;

namespace HorrorGame.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Speeds")]
        public float walkSpeed = 3f;
        public float sprintSpeed = 6f;
        public float crouchSpeed = 1.5f;

        [Header("Jumping & Gravity")]
        public float jumpHeight = 1.5f;
        public float gravity = -9.81f;
        
        [Header("Mouse Look Settings")]
        public Transform playerCamera;
        public float mouseSensitivity = 2f;
        private float xRotation = 0f;

        [Header("Crouching Settings")]
        public float normalHeight = 2f;
        public float crouchHeight = 1f;

        // Trạng thái (States)
        public bool isSprinting = false;
        public bool isCrouching = false;
        public bool isGrounded = false;
        
        // Component References
        private CharacterController controller;
        private PlayerStats stats;
        private Animator animator;
        
        // Vector lưu vận tốc rớt xuống (Trọng lực)
        private Vector3 velocity;

        // Què Chân & Gãy Xương (Green Hell Limping & Fall Damage)
        private PlayerFractureSystem fractureSystem;
        private bool wasGroundedLastFrame = true;
        private float highestAirborneY = 0f;
        private float lastFallVelocityY = 0f;
        private bool landingImpactHandledThisFall = false;

        // Hiệu ứng Camera đi cà nhấc (Limping Head-Bob & Tilt)
        private Vector3 baseCameraLocalPos = new Vector3(0f, 0.65f, 0.15f);
        private float limpCycleTimer = 0f;
        private float limpYOffset = 0f;
        private float limpPitchOffset = 0f;
        private float limpRollOffset = 0f;
        private float lastLimpStepTime = -999f;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            if (controller != null && !HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive)
            {
                controller.enabled = true;
            }
            stats = GetComponent<PlayerStats>();
            animator = GetComponentInChildren<Animator>();

            // Tự động kích hoạt hệ thống Gãy Xương Green Hell
            fractureSystem = GetComponent<PlayerFractureSystem>();
            if (fractureSystem == null)
            {
                fractureSystem = gameObject.AddComponent<PlayerFractureSystem>();
            }

            // Khóa con trỏ chuột vào giữa màn hình
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Load độ nhạy chuột từ cài đặt
            mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", mouseSensitivity);

            if (playerCamera != null)
            {
                if (!HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive)
                {
                    playerCamera.localPosition = new Vector3(0f, 0.65f, 0.15f);
                }
                baseCameraLocalPos = playerCamera.localPosition;
            }

            if (playerCamera != null)
            {
                try
                {
                    if (!playerCamera.CompareTag("MainCamera"))
                    {
                        playerCamera.tag = "MainCamera";
                    }
                }
                catch {}
            }

            // Tự động kích hoạt hệ thống xây dựng nếu chưa có
            if (GetComponent<HorrorGame.Survival.BuildingSystem>() == null)
            {
                gameObject.AddComponent<HorrorGame.Survival.BuildingSystem>();
            }

            // Tự động kích hoạt hiệu ứng mờ mắt quang học và âm thanh biển sâu khi lặn dưới nước
            if (playerCamera != null)
            {
                Camera camComp = playerCamera.GetComponent<Camera>();
                if (camComp != null && camComp.GetComponent<HorrorGame.Effects.UnderwaterVisualManager>() == null)
                {
                    camComp.gameObject.AddComponent<HorrorGame.Effects.UnderwaterVisualManager>();
                }
            }
        }

        void OnEnable()
        {
            // Không kích hoạt lại controller khi Cinematic Trailer đang chạy
            if (HorrorGame.Cutscenes.CinematicTrailerDirector.IsTrailerRunning) return;

            if (controller == null) controller = GetComponent<CharacterController>();
            if (controller != null && !HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive)
            {
                controller.enabled = true;
            }

            if (playerCamera != null && !HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive)
            {
                playerCamera.localPosition = new Vector3(0f, 0.65f, 0.15f);
                baseCameraLocalPos = playerCamera.localPosition;
            }
        }

        void Update()
        {
            // Tạm dừng khi Pause Menu đang mở
            if (HorrorGame.UI.GameMenuManager.IsPaused)
                return;

            // Không nhận input điều khiển khi đang chạy Cutscene mở đầu
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive)
                return;

            // Không nhận input điều khiển khi đang chạy Cinematic Trailer
            if (HorrorGame.Cutscenes.CinematicTrailerDirector.IsTrailerRunning)
                return;

            // Kiểm tra xem túi đồ có đang mở không (nếu có thì không cho xoay chuột/di chuyển)
            // Giả sử có InventoryManager
            if (Inventory.InventoryManager.Instance != null && Inventory.InventoryManager.Instance.isInventoryOpen)
                return;

            HandleMouseLook();
            HandleMovement();
            HandleCrouch();
        }

        private void HandleMouseLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // Xoay Camera lên xuống
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -85f, 85f); // Khóa góc nhìn để không bị lật ngược cổ

            // Xoay toàn bộ thân nhân vật sang trái phải
            transform.Rotate(Vector3.up * mouseX);
        }

        private void HandleMovement()
        {
            if (fractureSystem == null) fractureSystem = GetComponent<PlayerFractureSystem>();
            bool hasFracture = (fractureSystem != null && fractureSystem.isFractured);

            // ═══════════════════════════════════════════════════════════════
            // 1. THEO DÕI ĐỘ CAO RƠI TỰ DO KHI Ở TRÊN KHÔNG
            // ═══════════════════════════════════════════════════════════════
            if (!controller.isGrounded)
            {
                if (wasGroundedLastFrame)
                {
                    // Frame đầu tiên vừa rời khỏi mặt đất
                    highestAirborneY = transform.position.y;
                    landingImpactHandledThisFall = false;
                }
                else if (transform.position.y > highestAirborneY)
                {
                    highestAirborneY = transform.position.y;
                }
            }

            // ═══════════════════════════════════════════════════════════════
            // 2. DI CHUYỂN WASD & TRẠNG THÁI CHẠY / QUÈ CHÂN
            // ═══════════════════════════════════════════════════════════════
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");
            Vector3 move = transform.right * x + transform.forward * z;

            bool isMoving = move.magnitude > 0.1f;
            bool wantsToSprint = Input.GetKey(KeyCode.LeftShift);

            // Nếu đang gãy xương mà cố bấm Shift chạy -> Cảnh báo đau buốt & chặn chạy
            if (hasFracture && wantsToSprint && isMoving)
            {
                fractureSystem.OnAttemptSprint();
            }

            // Chỉ cho phép chạy khi không gãy chân
            isSprinting = wantsToSprint && isMoving && !isCrouching && stats.HasStamina() && !hasFracture;

            float currentSpeed = walkSpeed;

            if (hasFracture)
            {
                // Què chân gãy xương: Tốc độ đi bộ giảm còn 48% (đi cà nhấc)
                currentSpeed = walkSpeed * fractureSystem.speedPenaltyMultiplier;
                if (isCrouching)
                {
                    currentSpeed = crouchSpeed * fractureSystem.speedPenaltyMultiplier;
                }
                stats.RegenStamina(); // Hồi thể lực bình thường
            }
            else if (isSprinting)
            {
                currentSpeed = sprintSpeed;
                stats.DrainStamina(); // Tụt thể lực khi chạy
            }
            else
            {
                if (isCrouching)
                {
                    currentSpeed = crouchSpeed;
                }
                stats.RegenStamina(); // Hồi thể lực khi đi bộ hoặc đứng yên
            }

            // Di chuyển nhân vật theo phương ngang
            controller.Move(move * currentSpeed * Time.deltaTime);

            // ═══════════════════════════════════════════════════════════════
            // 3. NHẢY (JUMP)
            // ═══════════════════════════════════════════════════════════════
            if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
            {
                if (hasFracture)
                {
                    // Gãy xương không thể nhảy cao, gây đau nhói buốt
                    fractureSystem.PlayPainGrunt();
                    if (stats != null) stats.ShowStatusNotice("⚠️ Chân gãy nhói buốt! Không thể nhảy!");
                }
                else
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    highestAirborneY = transform.position.y;
                    landingImpactHandledThisFall = false;
                    wasGroundedLastFrame = false;
                    isGrounded = false;
                    if (animator != null) animator.SetTrigger("Jump");
                }
            }

            // ═══════════════════════════════════════════════════════════════
            // 4. ÁP DỤNG TRỌNG LỰC & TIẾP ĐẤT TỨC THÌ (ZERO-LATENCY LANDING)
            // Khớp âm thanh và sát thương NGAY TRONG FRAME CHẠM ĐẤT, không có độ trễ!
            // ═══════════════════════════════════════════════════════════════
            lastFallVelocityY = velocity.y;
            velocity.y += gravity * Time.deltaTime;

            CollisionFlags verticalFlags = controller.Move(velocity * Time.deltaTime);

            bool hitGroundNow = (verticalFlags & CollisionFlags.Below) != 0 || controller.isGrounded;

            if (hitGroundNow)
            {
                // Vừa từ trên cao chạm mặt đất trong frame này -> Kích hoạt âm thanh & va đập ngay lập tức!
                if (!wasGroundedLastFrame && !landingImpactHandledThisFall)
                {
                    TriggerInstantLanding();
                }

                isGrounded = true;
                wasGroundedLastFrame = true;
                landingImpactHandledThisFall = false;

                if (velocity.y < 0)
                {
                    velocity.y = -2f; // Giữ nhân vật bám sát mặt đất
                }
            }
            else
            {
                isGrounded = false;
                wasGroundedLastFrame = false;
            }

            // ═══════════════════════════════════════════════════════════════
            // 5. HIỆU ỨNG CAMERA ĐI CÀ NHẮC (GREEN HELL LIMPING ANIMATION)
            // ═══════════════════════════════════════════════════════════════
            HandleLimpCamera(isMoving);

            // Truyền dữ liệu cho Animator
            if (animator != null)
            {
                float animationSpeed = isMoving ? (isSprinting ? 2f : (hasFracture ? 0.65f : 1f)) : 0f;
                animator.SetFloat("Speed", animationSpeed, 0.1f, Time.deltaTime);
                animator.SetBool("IsGrounded", isGrounded);
            }
        }

        /// <summary>
        /// Kích hoạt va đập tiếp đất tức thời (0ms delay) ngay thời khắc chạm đất
        /// </summary>
        private void TriggerInstantLanding()
        {
            if (landingImpactHandledThisFall) return;
            landingImpactHandledThisFall = true;

            float fallDistance = highestAirborneY - transform.position.y;
            float impactSpeed = Mathf.Max(Mathf.Abs(velocity.y), Mathf.Abs(lastFallVelocityY));

            HandleLandingImpact(fallDistance, impactSpeed);

            // Reset mốc độ cao
            highestAirborneY = transform.position.y;
        }

        /// <summary>
        /// Bắt va chạm cấp độ vật lý trực tiếp từ CharacterController (xảy ra tức thì bên trong controller.Move)
        /// </summary>
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!wasGroundedLastFrame && !landingImpactHandledThisFall)
            {
                // Pháp tuyến hướng lên > 0.45f hoặc điểm va chạm ở ngay dưới chân
                if (hit.normal.y > 0.45f || hit.point.y < transform.position.y + 0.25f)
                {
                    if (velocity.y < -3.0f || lastFallVelocityY < -3.0f)
                    {
                        TriggerInstantLanding();
                    }
                }
            }
        }

        /// <summary>
        /// Kiểm tra xem nhân vật có đang ở trong nước / rơi xuống nước hay không
        /// </summary>
        public bool IsInWater()
        {
            // 1. Kiểm tra cao độ so với mặt nước toàn bản đồ (Mặt nước Flooded Grounds ở Y = 16.0f)
            // Khi người chơi chạm nước hoặc ngập nước: transform.position.y <= 16.5f
            if (transform.position.y <= 16.5f)
                return true;

            // 2. Bắn Raycast xuống chân kiểm tra Collider / Mesh mặt nước
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 2.5f, ~0, QueryTriggerInteraction.Collide))
            {
                if (hit.collider != null)
                {
                    string colName = hit.collider.name.ToLower();
                    if (hit.collider.CompareTag("Water") || colName.Contains("water") || colName.Contains("river") || colName.Contains("sea") || colName.Contains("ocean") || colName.Contains("nước"))
                    {
                        return true;
                    }
                }
            }

            // 3. Quét các Trigger nước xung quanh chân (bán kính 1.2m)
            Collider[] hits = Physics.OverlapSphere(transform.position, 1.2f, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null) continue;
                string hName = hits[i].name.ToLower();
                if (hits[i].CompareTag("Water") || hName.Contains("water") || hName.Contains("river") || hName.Contains("sea") || hName.Contains("nước"))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Xử lý va đập khi rơi từ trên cao tiếp đất
        /// </summary>
        private void HandleLandingImpact(float fallDistance, float impactSpeed)
        {
            // ═══════════════════════════════════════════════════════════════
            // NẾU NHẢY / RƠI XUỐNG NƯỚC: AN TOÀN TUYỆT ĐỐI 100%!
            // Nước đỡ 100% va đập, KHÔNG BỊ MẤT MÁU, KHÔNG BỊ GÃY XƯƠNG!
            // ═══════════════════════════════════════════════════════════════
            if (IsInWater())
            {
                if (stats != null)
                {
                    stats.ShowStatusNotice("💧 Nhảy xuống nước an toàn! Không bị mất máu hay gãy chân.");
                }
                Debug.Log("[PlayerController] 💧 Rơi xuống nước an toàn! Đỡ hoàn toàn va đập, không gãy chân.");
                return;
            }

            // 1. Nhảy bình thường hoặc bậc thấp (< 3.2m và tốc độ < 8.5m/s): Hoàn toàn an toàn, không mất máu!
            if (fallDistance < 3.2f && impactSpeed < 8.5f)
            {
                return;
            }

            // 2. Rơi từ độ cao vừa (3.2m - 5.0m): Chấn thương nhẹ (8 - 25 HP), không gãy xương
            if (fallDistance < 5.0f && impactSpeed < 11.0f)
            {
                float damage = Mathf.Clamp((fallDistance - 3.0f) * 9.5f, 8f, 26f);
                if (stats != null)
                {
                    stats.TakeDamage(damage);
                    stats.ShowStatusNotice(string.Format("⚠️ Ngã đau! (-{0:F0} Máu)", damage));
                }
                if (fractureSystem != null)
                {
                    fractureSystem.PlayPainGrunt();
                }
                return;
            }

            // 3. Rơi từ trên cao xuống (>= 5.0m hoặc tốc độ >= 11.0m/s):
            // KHỚP NGAY LẬP TỨC: SÁT THƯƠNG NẶNG + BẺ RẮC GÃY XƯƠNG + TIẾNG RÊN LA THẤT THANH (0ms DELAY)!
            float severeDamage = 30f + (fallDistance - 5.0f) * 12.0f;
            if (stats != null)
            {
                stats.TakeDamage(severeDamage);
            }

            if (fractureSystem != null)
            {
                fractureSystem.TriggerFracture(fallDistance, severeDamage);
            }
        }

        /// <summary>
        /// Mô phỏng nhịp điệu camera đi cà nhấc lệch bước chuẩn phong cách Green Hell
        /// </summary>
        private void HandleLimpCamera(bool isMoving)
        {
            if (playerCamera == null) return;

            bool hasFracture = (fractureSystem != null && fractureSystem.isFractured);

            if (hasFracture && isMoving && isGrounded)
            {
                // Tần số chu kỳ bước chân cà nhấc (~1.6 bước/s)
                float limpSpeed = 1.6f;
                limpCycleTimer += Time.deltaTime * limpSpeed;
                float cycle = limpCycleTimer % 1.0f;

                // Chu kỳ bước chân lệch:
                // 0.0 - 0.45: Chân lành bước tiếp đất (chuyển động bình thường)
                // 0.45 - 0.85: Chân què tiếp đất chịu tải -> Chùng xuống sâu (Dip) và nghiêng lảo đảo (Roll & Pitch)
                // 0.85 - 1.0: Nhấc chân què lên chuyển trọng tâm
                if (cycle >= 0.45f && cycle <= 0.85f)
                {
                    float t = (cycle - 0.45f) / 0.40f;
                    float curve = Mathf.Sin(t * Mathf.PI);

                    limpYOffset = -curve * 0.16f;   // Chùng người xuống 16cm
                    limpRollOffset = -curve * 3.4f;  // Nghiêng cổ/đầu 3.4 độ
                    limpPitchOffset = curve * 1.5f;  // Chúi đầu về trước 1.5 độ

                    // Điểm chạm lực cực đại của chân què
                    if (cycle >= 0.60f && cycle <= 0.65f && Time.time - lastLimpStepTime > 0.5f)
                    {
                        lastLimpStepTime = Time.time;
                        if (fractureSystem != null)
                        {
                            fractureSystem.OnLimpStep();
                        }
                    }
                }
                else
                {
                    limpYOffset = Mathf.Lerp(limpYOffset, 0f, Time.deltaTime * 6f);
                    limpRollOffset = Mathf.Lerp(limpRollOffset, 0f, Time.deltaTime * 6f);
                    limpPitchOffset = Mathf.Lerp(limpPitchOffset, 0f, Time.deltaTime * 6f);
                }
            }
            else
            {
                // Khi đứng yên hoặc không gãy xương: Đưa camera về vị trí chuẩn
                limpYOffset = Mathf.Lerp(limpYOffset, 0f, Time.deltaTime * 8f);
                limpRollOffset = Mathf.Lerp(limpRollOffset, 0f, Time.deltaTime * 8f);
                limpPitchOffset = Mathf.Lerp(limpPitchOffset, 0f, Time.deltaTime * 8f);
                limpCycleTimer = 0f;
            }

            // Áp dụng lên Camera
            float targetCamY = isCrouching ? 0.35f : baseCameraLocalPos.y;
            playerCamera.localPosition = new Vector3(baseCameraLocalPos.x, targetCamY + limpYOffset, baseCameraLocalPos.z);
            playerCamera.localRotation = Quaternion.Euler(xRotation + limpPitchOffset, 0f, limpRollOffset);
        }

        private void HandleCrouch()
        {
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
            {
                isCrouching = !isCrouching;
                if (isCrouching)
                {
                    controller.height = crouchHeight;
                }
                else
                {
                    controller.height = normalHeight;
                }
            }
        }
    }
}

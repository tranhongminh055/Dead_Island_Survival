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
        private bool isSprinting = false;
        private bool isCrouching = false;
        private bool isGrounded = false;
        
        // Component References
        private CharacterController controller;
        private PlayerStats stats;
        private Animator animator;
        
        // Vector lưu vận tốc rớt xuống (Trọng lực)
        private Vector3 velocity;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            stats = GetComponent<PlayerStats>();
            animator = GetComponentInChildren<Animator>();

            // Khóa con trỏ chuột vào giữa màn hình
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            // Không nhận input điều khiển khi đang chạy Cutscene mở đầu
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive)
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
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            // Xoay toàn bộ thân nhân vật sang trái phải
            transform.Rotate(Vector3.up * mouseX);
        }

        private void HandleMovement()
        {
            // Kiểm tra chạm đất
            isGrounded = controller.isGrounded;
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // Giữ nhân vật bám sát mặt đất
            }

            // Lấy phím WASD
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");
            Vector3 move = transform.right * x + transform.forward * z;

            // Kiểm tra trạng thái Chạy (Sprint)
            bool isMoving = move.magnitude > 0.1f;
            isSprinting = Input.GetKey(KeyCode.LeftShift) && isMoving && !isCrouching && stats.HasStamina();

            float currentSpeed = walkSpeed;

            if (isSprinting)
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

            // Di chuyển
            controller.Move(move * currentSpeed * Time.deltaTime);

            // Nhảy
            if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (animator != null) animator.SetTrigger("Jump");
            }

            // Áp dụng trọng lực
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            // Truyền dữ liệu cho Animator
            if (animator != null)
            {
                // Speed: 0 = Đứng yên, 1 = Đi bộ, 2 = Chạy
                float animationSpeed = isMoving ? (isSprinting ? 2f : 1f) : 0f;
                
                // Dùng hàm lerp mượt mà chuyển đổi số thay vì giật cục (0.1f là độ trễ)
                animator.SetFloat("Speed", animationSpeed, 0.1f, Time.deltaTime);
                animator.SetBool("IsGrounded", isGrounded);
            }
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

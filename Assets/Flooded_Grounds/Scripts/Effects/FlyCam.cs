using UnityEngine;

public class FlyCam : MonoBehaviour
{
    [Header("Tốc độ bay")]
    public float moveSpeed = 20f;       // Tốc độ di chuyển WASD
    public float boostMultiplier = 3f;  // Nhân tốc độ khi giữ Shift
    public float scrollSpeed = 10f;     // Tốc độ tăng/giảm bằng con lăn chuột

    [Header("Tốc độ xoay camera")]
    public float lookSpeed = 3f;

    private float yaw = 0f;   // Góc xoay trái phải
    private float pitch = 0f; // Góc xoay lên xuống

    void Start()
    {
        // Lấy góc xoay hiện tại của camera làm điểm xuất phát
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;

        // Ẩn con trỏ chuột để quay mượt hơn
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // === XOAY CAMERA BẰNG CHUỘT ===
        yaw += Input.GetAxis("Mouse X") * lookSpeed;
        pitch -= Input.GetAxis("Mouse Y") * lookSpeed;
        pitch = Mathf.Clamp(pitch, -90f, 90f); // Không cho ngước quá 90 độ
        transform.eulerAngles = new Vector3(pitch, yaw, 0f);

        // === DI CHUYỂN BẰNG WASD ===
        float currentSpeed = moveSpeed;

        // Giữ Shift để bay nhanh gấp 3
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed *= boostMultiplier;
        }

        // Lăn con lăn chuột để thay đổi tốc độ bay
        moveSpeed += Input.GetAxis("Mouse ScrollWheel") * scrollSpeed;
        moveSpeed = Mathf.Clamp(moveSpeed, 5f, 200f); // Giới hạn tốc độ từ 5 đến 200

        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) move += transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= transform.forward;
        if (Input.GetKey(KeyCode.A)) move -= transform.right;
        if (Input.GetKey(KeyCode.D)) move += transform.right;
        if (Input.GetKey(KeyCode.E)) move += Vector3.up;   // Bay lên
        if (Input.GetKey(KeyCode.Q)) move += Vector3.down;  // Bay xuống

        transform.position += move.normalized * currentSpeed * Time.deltaTime;

        // Nhấn Escape để hiện lại con trỏ chuột (thoát chế độ quay)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}

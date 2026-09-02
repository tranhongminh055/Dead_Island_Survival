using UnityEngine;

namespace HorrorGame.Player
{
    public class WeaponManager : MonoBehaviour
    {
        [Header("Danh sách vũ khí (Weapon Slots)")]
        [Tooltip("Kéo thả các khẩu súng vào đây theo thứ tự: Phím 1 = ô 0, Phím 2 = ô 1...")]
        public GameObject[] weaponSlots; // Mỗi ô chứa 1 khẩu súng (GameObject)

        [Header("Trạng thái ban đầu")]
        [Tooltip("Khi vào game, người chơi chưa cầm gì (tay không)")]
        public bool startUnarmed = true;

        private int currentWeaponIndex = -1; // -1 = không cầm gì (tay không)

        [Header("UI Hiển thị")]
        public bool showWeaponName = true; // Hiện tên súng trên màn hình

        // Biến hiển thị tên súng
        private string currentWeaponName = "";
        private float weaponNameTimer = 0f;
        private float weaponNameDisplayTime = 2f; // Hiện tên súng trong 2 giây

        void Start()
        {
            // Khi vào game, tắt hết tất cả súng (giấu đi)
            HideAllWeapons();

            if (!startUnarmed && weaponSlots.Length > 0)
            {
                EquipWeapon(0); // Nếu không muốn tay không, tự động rút súng đầu tiên
            }

            Debug.Log("=== HỆ THỐNG VŨ KHÍ ĐÃ SẴN SÀNG ===");
            Debug.Log("Bấm phím 1-" + weaponSlots.Length + " để rút súng. Bấm lại lần nữa để cất súng.");
        }

        void Update()
        {
            // Giảm bộ đếm thời gian hiển thị tên súng
            if (weaponNameTimer > 0)
            {
                weaponNameTimer -= Time.deltaTime;
            }

            // Bấm phím số 1-9 để rút/cất súng
            if (Input.GetKeyDown(KeyCode.Alpha1)) ToggleWeapon(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) ToggleWeapon(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) ToggleWeapon(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) ToggleWeapon(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) ToggleWeapon(4);
            if (Input.GetKeyDown(KeyCode.Alpha6)) ToggleWeapon(5);
            if (Input.GetKeyDown(KeyCode.Alpha7)) ToggleWeapon(6);

            // Cuộn chuột để đổi súng
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f) SwitchToNextWeapon();
            if (scroll < 0f) SwitchToPreviousWeapon();
        }

        // Bấm phím lần 1 = Rút súng, bấm lần 2 = Cất súng
        private void ToggleWeapon(int index)
        {
            if (index >= weaponSlots.Length || weaponSlots[index] == null) return;

            if (currentWeaponIndex == index)
            {
                // Đang cầm súng này rồi -> Cất đi (tay không)
                HolsterWeapon();
            }
            else
            {
                // Rút súng mới
                EquipWeapon(index);
            }
        }

        // Rút súng ra
        private void EquipWeapon(int index)
        {
            if (index >= weaponSlots.Length || weaponSlots[index] == null) return;

            HideAllWeapons();

            weaponSlots[index].SetActive(true);
            currentWeaponIndex = index;

            // Bật script Gun trên khẩu súng này
            Gun gun = weaponSlots[index].GetComponent<Gun>();
            if (gun != null) gun.enabled = true;

            currentWeaponName = weaponSlots[index].name;
            weaponNameTimer = weaponNameDisplayTime;

            Debug.Log("Đã rút: " + currentWeaponName);
        }

        // Cất súng (tay không)
        private void HolsterWeapon()
        {
            if (currentWeaponIndex >= 0 && currentWeaponIndex < weaponSlots.Length)
            {
                weaponSlots[currentWeaponIndex].SetActive(false);
                currentWeaponName = "Tay không";
                weaponNameTimer = weaponNameDisplayTime;
                Debug.Log("Đã cất súng");
            }
            currentWeaponIndex = -1;
        }

        // Giấu hết tất cả súng
        private void HideAllWeapons()
        {
            for (int i = 0; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[i] != null)
                {
                    weaponSlots[i].SetActive(false);
                }
            }
        }

        // Cuộn lên = Chuyển sang súng tiếp theo
        private void SwitchToNextWeapon()
        {
            if (weaponSlots.Length == 0) return;

            int nextIndex = currentWeaponIndex + 1;
            if (nextIndex >= weaponSlots.Length) nextIndex = 0;

            EquipWeapon(nextIndex);
        }

        // Cuộn xuống = Chuyển sang súng trước đó
        private void SwitchToPreviousWeapon()
        {
            if (weaponSlots.Length == 0) return;

            int prevIndex = currentWeaponIndex - 1;
            if (prevIndex < 0) prevIndex = weaponSlots.Length - 1;

            EquipWeapon(prevIndex);
        }

        // Hiển thị tên súng trên màn hình
        void OnGUI()
        {
            if (!showWeaponName || weaponNameTimer <= 0) return;

            float alpha = Mathf.Clamp01(weaponNameTimer);

            GUIStyle style = new GUIStyle();
            style.fontSize = 28;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = new Color(1f, 1f, 1f, alpha);
            style.alignment = TextAnchor.MiddleCenter;

            // Viền đen (shadow)
            GUIStyle shadowStyle = new GUIStyle(style);
            shadowStyle.normal.textColor = new Color(0f, 0f, 0f, alpha);

            float x = Screen.width / 2f - 150f;
            float y = Screen.height - 120f;

            // Vẽ bóng trước, rồi vẽ chữ trắng đè lên
            GUI.Label(new Rect(x + 2, y + 2, 300, 40), currentWeaponName, shadowStyle);
            GUI.Label(new Rect(x, y, 300, 40), currentWeaponName, style);
        }

        // Kiểm tra xem người chơi có đang cầm súng hay không
        public bool IsArmed()
        {
            return currentWeaponIndex >= 0;
        }

        // Lấy khẩu súng hiện tại
        public GameObject GetCurrentWeapon()
        {
            if (currentWeaponIndex >= 0 && currentWeaponIndex < weaponSlots.Length)
                return weaponSlots[currentWeaponIndex];
            return null;
        }
    }
}

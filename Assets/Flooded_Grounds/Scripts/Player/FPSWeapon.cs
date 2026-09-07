using UnityEngine;
using System.Collections;

/// <summary>
/// Script tất cả trong một: Gắn vào Camera là chạy.
/// Tự động lấy model súng và hiển thị + bắn.
/// </summary>
public class FPSWeapon : MonoBehaviour
{
    [Header("=== KÉO SÚNG VÀO ĐÂY ===")]
    [Tooltip("Kéo thả model súng 3D (từ thư mục Project) vào ô này")]
    public GameObject gunModelPrefab; // Kéo file FBX hoặc Prefab súng vào đây

    [Header("Vị trí súng trên màn hình")]
    public Vector3 gunPosition = new Vector3(0.35f, -0.25f, 0.6f);
    public Vector3 gunRotation = new Vector3(0, 180, 0);
    public Vector3 gunScale = new Vector3(0.4f, 0.4f, 0.4f);

    [Header("Thông số súng")]
    public float damage = 25f;
    public float range = 100f;
    public float fireRate = 8f;
    public int maxAmmo = 30;
    public float reloadTime = 2f;

    [Header("Tích hợp Túi Đồ (Inventory)")]
    public HorrorGame.Inventory.ItemData weaponItemData; // Kéo file GunItem vào đây

    [Header("Phím tắt")]
    public KeyCode toggleKey = KeyCode.Alpha1; // Phím 1 để rút/cất súng

    [Header("Âm thanh & Hiệu ứng")]
    public AudioClip shootSound; // Kéo file âm thanh tiếng súng vào đây
    public AudioClip reloadSound; // (Tùy chọn) Kéo file âm thanh nạp đạn vào đây
    public AudioClip equipSound; // (Tùy chọn) Kéo file âm thanh rút súng vào đây
    public ParticleSystem muzzleFlash; // (Tùy chọn) Kéo file Particle tia lửa vào đây

    [Header("Recoil (Hoạt ảnh giật súng)")]
    public float recoilKickback = -0.1f; // Độ lùi của súng
    public float recoilRotation = -15f;  // Độ ngóc nòng súng
    public float recoilRecoverSpeed = 8f; // Tốc độ hồi nòng

    // Private
    private GameObject gunInstance;
    private int currentAmmo;
    private bool isReloading = false;
    private bool isEquipped = false;
    private float nextFireTime = 0f;

    // Recoil state
    private Vector3 currentGunPosition;
    private Vector3 currentGunRotation;

    // Animator
    private Animator playerAnimator;

    // Audio & FX
    private AudioSource audioSource;
    private Light flashLight;

    // UI
    private GUIStyle ammoStyle;
    private GUIStyle ammoShadowStyle;
    private GUIStyle weaponNameStyle;
    private float weaponNameTimer = 0f;
    private string weaponNameText = "";

    void Start()
    {
        // QUAN TRỌNG: Reset lại tốc độ game về bình thường
        Time.timeScale = 1f;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        // Tự động load WeaponItemData nếu chưa gán trong Inspector
        if (weaponItemData == null)
        {
            weaponItemData = Resources.Load<HorrorGame.Inventory.ItemData>("GunItem");
            if (weaponItemData != null)
            {
                Debug.Log("[FPSWeapon] Tự động load GunItem từ Resources thành công.");
            }
            else
            {
                Debug.LogWarning("[FPSWeapon] Không tìm thấy GunItem trong thư mục Resources!");
            }
        }

        // Tạo nguồn phát âm thanh trên Camera
        audioSource = gameObject.AddComponent<AudioSource>();

        // TẠO ĐÈN CHỚP LỬA (Muzzle Flash Light) tự động
        GameObject lightObj = new GameObject("MuzzleFlashLight");
        lightObj.transform.parent = transform; 
        // Đặt đèn ra phía trước nòng súng một chút và nhô lên trên để sáng rõ thân súng
        lightObj.transform.localPosition = new Vector3(0, 0.15f, 0.6f); 
        flashLight = lightObj.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.color = new Color(1f, 0.7f, 0.1f); // Màu cam vàng của lửa
        flashLight.range = 25f; // Tăng tầm chiếu xa để nhìn thẳng vẫn thấy sáng
        flashLight.intensity = 0f; // Ban đầu tắt đèn

        // TỰ ĐỘNG CĂN CHỈNH CAMERA VÀO ĐÚNG MẮT NHÂN VẬT
        // Đẩy trục Z ra xa thêm (0.25) để cam lọt hẳn ra ngoài, không bao giờ kẹt vào trong đầu nhân vật
        transform.localPosition = new Vector3(0f, 0.88f, 0.25f);
        transform.localRotation = Quaternion.Euler(0, 0, 0);

        currentAmmo = maxAmmo;
        CreateGunVisual();

        // Tìm Animator của nhân vật (từ GameObject cha)
        if (transform.parent != null)
        {
            playerAnimator = transform.parent.GetComponentInChildren<Animator>();
        }

        // Ban đầu cất súng (ẩn đi)
        if (gunInstance != null)
        {
            gunInstance.SetActive(false);
        }

        SetupGUIStyles();
        Debug.Log("=== FPS WEAPON SẴN SÀNG! Bấm phím 1 để rút súng ===");
    }

    void CreateGunVisual()
    {
        if (gunModelPrefab != null)
        {
            // Nếu đã kéo model súng vào ô gunModelPrefab
            gunInstance = Instantiate(gunModelPrefab, transform);
        }
        else
        {
            // Nếu chưa có model -> Tạo khẩu súng tạm bằng khối hộp
            Debug.LogWarning("Chưa gắn model súng! Đang dùng hình hộp tạm thời.");
            gunInstance = CreatePlaceholderGun();
        }

        // Đặt vị trí, xoay, kích thước
        gunInstance.transform.localPosition = gunPosition;
        gunInstance.transform.localRotation = Quaternion.Euler(gunRotation);
        gunInstance.transform.localScale = gunScale;

        // Tắt mọi Collider trên súng (để không va chạm với vật thể khác)
        Collider[] colliders = gunInstance.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        Debug.Log("Đã tạo súng thành công! Vị trí: " + gunPosition);
    }

    GameObject CreatePlaceholderGun()
    {
        // Tạo khẩu súng tạm bằng các khối hộp
        GameObject gun = new GameObject("PlaceholderGun");
        gun.transform.SetParent(transform);

        // Thân súng
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(gun.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.08f, 0.12f, 0.6f);
        body.GetComponent<Renderer>().material.color = new Color(0.2f, 0.2f, 0.2f); // Xám đen

        // Nòng súng
        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.transform.SetParent(gun.transform);
        barrel.transform.localPosition = new Vector3(0, 0.02f, 0.35f);
        barrel.transform.localRotation = Quaternion.Euler(90, 0, 0);
        barrel.transform.localScale = new Vector3(0.04f, 0.2f, 0.04f);
        barrel.GetComponent<Renderer>().material.color = new Color(0.15f, 0.15f, 0.15f);

        // Tay cầm
        GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grip.transform.SetParent(gun.transform);
        grip.transform.localPosition = new Vector3(0, -0.1f, -0.05f);
        grip.transform.localRotation = Quaternion.Euler(15, 0, 0);
        grip.transform.localScale = new Vector3(0.06f, 0.15f, 0.08f);
        grip.GetComponent<Renderer>().material.color = new Color(0.25f, 0.2f, 0.15f); // Nâu

        return gun;
    }

    void SetupGUIStyles()
    {
        ammoStyle = new GUIStyle();
        ammoStyle.fontSize = 32;
        ammoStyle.fontStyle = FontStyle.Bold;
        ammoStyle.normal.textColor = Color.white;
        ammoStyle.alignment = TextAnchor.MiddleRight;

        ammoShadowStyle = new GUIStyle(ammoStyle);
        ammoShadowStyle.normal.textColor = Color.black;

        weaponNameStyle = new GUIStyle();
        weaponNameStyle.fontSize = 24;
        weaponNameStyle.fontStyle = FontStyle.Bold;
        weaponNameStyle.normal.textColor = Color.white;
        weaponNameStyle.alignment = TextAnchor.MiddleCenter;
    }

    void Update()
    {
        // Bấm phím 1 để rút/cất súng
        if (Input.GetKeyDown(toggleKey))
        {
            // Nếu có liên kết với Túi đồ, kiểm tra xem súng có trong túi không
            if (weaponItemData != null && HorrorGame.Inventory.InventoryManager.Instance != null)
            {
                if (HorrorGame.Inventory.InventoryManager.Instance.HasItem(weaponItemData))
                {
                    ToggleWeapon();
                }
                else
                {
                    weaponNameText = "Không có súng trong túi!";
                    weaponNameTimer = 2f;
                    Debug.Log("Không có súng trong túi!");
                }
            }
            else
            {
                // Nếu chưa liên kết, cho rút tự do
                ToggleWeapon();
            }
        }

        if (!isEquipped || isReloading) return;

        // Bấm R hoặc hết đạn -> Thay đạn
        if (currentAmmo <= 0 || (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo))
        {
            StartCoroutine(Reload());
            return;
        }

        // Bấm chuột trái để bắn
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 1f / fireRate;
            Shoot();
        }

        // Nhả chuột trái -> Tắt ngay lập tức âm thanh (sửa lỗi tiếng súng nổ dư)
        if (Input.GetButtonUp("Fire1"))
        {
            if (audioSource.isPlaying) audioSource.Stop();
        }

        // Làm mờ dần ánh sáng chớp lửa súng
        if (flashLight != null && flashLight.intensity > 0)
        {
            flashLight.intensity = Mathf.Lerp(flashLight.intensity, 0f, Time.deltaTime * 30f);
        }

        // Giảm bộ đếm tên súng
        if (weaponNameTimer > 0) weaponNameTimer -= Time.deltaTime;

        // XỬ LÝ HOẠT ẢNH SÚNG (Giật / Nạp đạn) bằng Code
        if (gunInstance != null)
        {
            if (isReloading)
            {
                // Hoạt ảnh nạp đạn: Kéo súng thụt xuống dưới và xoay nòng lên trên một chút
                Vector3 reloadPos = gunPosition + new Vector3(0, -0.4f, 0);
                Vector3 reloadRot = gunRotation + new Vector3(45f, 0, 30f); // Xoay chéo khẩu súng
                
                currentGunPosition = Vector3.Lerp(currentGunPosition, reloadPos, Time.deltaTime * 5f);
                currentGunRotation = Vector3.Lerp(currentGunRotation, reloadRot, Time.deltaTime * 5f);
            }
            else
            {
                // Phục hồi nòng súng sau khi giật (Procedural Recoil Animation)
                currentGunPosition = Vector3.Lerp(currentGunPosition, gunPosition, Time.deltaTime * recoilRecoverSpeed);
                currentGunRotation = Vector3.Lerp(currentGunRotation, gunRotation, Time.deltaTime * recoilRecoverSpeed);
            }

            gunInstance.transform.localPosition = currentGunPosition;
            gunInstance.transform.localRotation = Quaternion.Euler(currentGunRotation);
        }
    }

    public void ToggleWeapon()
    {
        isEquipped = !isEquipped;

        if (gunInstance != null)
        {
            gunInstance.SetActive(isEquipped);
        }

        if (isEquipped)
        {
            weaponNameText = (gunModelPrefab != null) ? gunModelPrefab.name : "Súng";
            Debug.Log("Đã rút súng: " + weaponNameText);

            // Phát âm thanh rút súng
            if (equipSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(equipSound);
            }
        }
        else
        {
            weaponNameText = "Cất súng";
            Debug.Log("Đã cất súng");
        }

        // Báo cho Animator biết nhân vật đang cầm súng hay không
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsArmed", isEquipped);
        }

        weaponNameTimer = 2f;
    }

    void Shoot()
    {
        currentAmmo--;

        // Bật tia lửa (nếu có Particle)
        if (muzzleFlash != null) muzzleFlash.Play();
        
        // Bật ánh sáng chớp lửa (tăng cường độ sáng)
        if (flashLight != null) flashLight.intensity = 8f;

        // Phát tiếng súng (Reset lại mỗi lần bắn để không bị dội âm)
        if (shootSound != null)
        {
            audioSource.clip = shootSound;
            audioSource.Play();
        }

        // Tạo hiệu ứng giật súng (Recoil)
        currentGunPosition += new Vector3(0, 0, recoilKickback);
        currentGunRotation += new Vector3(recoilRotation, Random.Range(-2f, 2f), 0); // Thêm rung lắc nhẹ 2 bên

        // Bắn tia Raycast từ giữa màn hình ra phía trước
        RaycastHit hit;
        Ray ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out hit, range))
        {
            Debug.Log("Bắn trúng: " + hit.transform.name);

            // Kiểm tra Zombie
            HorrorGame.Enemy.EnemyAI enemy = hit.transform.GetComponent<HorrorGame.Enemy.EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            // Đẩy vật lý
            if (hit.rigidbody != null)
            {
                hit.rigidbody.AddForce(-hit.normal * 60f);
            }
        }
    }

    IEnumerator Reload()
    {
        isReloading = true;
        weaponNameText = "Đang nạp đạn...";
        Debug.Log("Đang nạp đạn...");

        // Phát âm thanh nạp đạn (nếu có)
        if (reloadSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(reloadSound);
        }

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;
        weaponNameText = (gunModelPrefab != null) ? gunModelPrefab.name : "Súng";
        Debug.Log("Nạp đạn xong!");
    }

    // Vẽ giao diện đạn + tâm ngắm lên màn hình
    void OnGUI()
    {
        if (!isEquipped) return;

        // === TÂM NGẮM (Crosshair) ===
        float crossSize = 20f;
        float crossThick = 2f;
        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        GUI.color = Color.white;
        // Ngang
        GUI.DrawTexture(new Rect(cx - crossSize, cy - crossThick / 2, crossSize * 2, crossThick), Texture2D.whiteTexture);
        // Dọc
        GUI.DrawTexture(new Rect(cx - crossThick / 2, cy - crossSize, crossThick, crossSize * 2), Texture2D.whiteTexture);

        // === SỐ ĐẠN ===
        string ammoText = currentAmmo + " / " + maxAmmo;
        if (isReloading) ammoText = "Nạp đạn...";

        float ax = Screen.width - 220;
        float ay = Screen.height - 60;

        GUI.color = Color.black;
        GUI.Label(new Rect(ax + 2, ay + 2, 200, 40), ammoText, ammoShadowStyle);
        GUI.color = Color.white;
        GUI.Label(new Rect(ax, ay, 200, 40), ammoText, ammoStyle);

        // === TÊN SÚNG (hiện 2 giây khi đổi súng) ===
        if (weaponNameTimer > 0)
        {
            float alpha = Mathf.Clamp01(weaponNameTimer);
            weaponNameStyle.normal.textColor = new Color(1, 1, 1, alpha);
            GUI.Label(new Rect(cx - 150, Screen.height - 130, 300, 40), weaponNameText, weaponNameStyle);
        }
    }
}

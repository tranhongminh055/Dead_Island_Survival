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

    [Header("Tự động căn súng thẳng như đang cầm (khuyên dùng)")]
    [Tooltip("Tự đo model súng, xoay nòng chĩa thẳng về phía trước, tự chỉnh kích thước. Khi bật, Gun Scale bị bỏ qua và Gun Rotation chỉ dùng để nghiêng thêm.")]
    public bool autoFitGun = true;
    [Tooltip("Chiều dài khẩu súng (mét). Tăng = súng to hơn")]
    public float gunLength = 0.55f;
    [Tooltip("Tick nếu nòng súng chĩa ngược về phía người chơi")]
    public bool flipGunDirection = false;
    [Tooltip("Nếu súng bị lật ngửa/nghiêng: thử 180, 90 hoặc -90")]
    public float gunRoll = 0f;

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

    /// <summary>
    /// Kiểm tra súng có đang được trang bị không (dùng bởi AxeController)
    /// </summary>
    public bool IsEquipped { get { return isEquipped; } }

    // Recoil state
    private Vector3 currentGunPosition;
    private Vector3 currentGunRotation;

    // Animator
    private Animator playerAnimator;

    // Audio & FX
    private AudioSource audioSource;
    private Light flashLight;

    // Cache WeaponManager để tránh FindObjectOfType mỗi frame
    private bool hasWeaponManager = false;

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

        // Cache kiểm tra WeaponManager
        hasWeaponManager = Object.FindObjectOfType<HorrorGame.Player.WeaponManager>() != null;

        // Tắt MeshRenderer gốc trên Camera (model súng gắn trực tiếp trong Scene)
        // để ẩn nó đi. gunInstance (clone) sẽ được tạo riêng và hiển thị ở vị trí FPS đúng.
        MeshRenderer meshOnCamera = GetComponent<MeshRenderer>();
        if (meshOnCamera != null) meshOnCamera.enabled = false;
        MeshCollider meshCol = GetComponent<MeshCollider>();
        if (meshCol != null) meshCol.enabled = false;

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

        // TỰ ĐỘNG CĂN CHỈNH CAMERA VÀO ĐÚNG MẮT NHÂN VẬT (chỉ khi KHÔNG có Cutscene máy bay đang chạy)
        if (!HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive)
        {
            transform.localPosition = new Vector3(0f, 0.65f, 0.15f);
            transform.localRotation = Quaternion.Euler(0, 0, 0);
        }

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
        // Giảm near clip để báng súng sát camera không bị cắt mất
        Camera cam = GetComponent<Camera>();
        if (cam != null && cam.nearClipPlane > 0.05f) cam.nearClipPlane = 0.02f;

        if (gunModelPrefab != null && autoFitGun)
        {
            gunInstance = CreateAutoFitGun();
            gunInstance.transform.localPosition = gunPosition;
            gunInstance.transform.localRotation = Quaternion.Euler(gunRotation);
            currentGunPosition = gunPosition;
            currentGunRotation = gunRotation;
            Debug.Log("Đã tạo súng (auto-fit) thành công! Vị trí: " + gunPosition);
            return;
        }

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

    /// <summary>
    /// Tạo súng nằm trong 1 "GunHolder": model được tự xoay sao cho trục dài nhất (nòng)
    /// chĩa về phía trước (Z), trục cao thứ hai hướng lên (Y), rồi chỉnh kích thước và căn giữa.
    /// </summary>
    GameObject CreateAutoFitGun()
    {
        GameObject holder = new GameObject("GunHolder");
        holder.transform.SetParent(transform, false);

        GameObject model = Instantiate(gunModelPrefab, holder.transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        Bounds b = GetLocalBounds(model, holder.transform);
        if (b.size == Vector3.zero) return holder;

        // Sắp xếp các trục theo độ dài: dài nhất = nòng, thứ hai = chiều cao súng
        Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
        float[] len = { b.size.x, b.size.y, b.size.z };
        int longest = 0;
        for (int i = 1; i < 3; i++) if (len[i] > len[longest]) longest = i;
        int second = -1;
        for (int i = 0; i < 3; i++)
        {
            if (i == longest) continue;
            if (second < 0 || len[i] > len[second]) second = i;
        }

        // R sao cho: R * trục_dài = forward, R * trục_cao = up
        Quaternion align = Quaternion.Inverse(Quaternion.LookRotation(axes[longest], axes[second]));
        Quaternion fix = Quaternion.Euler(0f, flipGunDirection ? 180f : 0f, gunRoll);
        model.transform.localRotation = fix * align;

        // Chỉnh kích thước theo chiều dài mong muốn
        b = GetLocalBounds(model, holder.transform);
        float k = gunLength / Mathf.Max(b.size.z, 0.0001f);
        model.transform.localScale = Vector3.one * k;

        // Căn tâm súng về gốc của holder
        b = GetLocalBounds(model, holder.transform);
        model.transform.localPosition = -b.center;

        // Súng FPS không cần đổ bóng
        foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        return holder;
    }

    public static Bounds GetLocalBounds(GameObject root, Transform space)
    {
        bool has = false;
        Bounds result = new Bounds();
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
        {
            Mesh mesh = null;
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf != null) mesh = mf.sharedMesh;
            SkinnedMeshRenderer smr = r as SkinnedMeshRenderer;
            if (smr != null) mesh = smr.sharedMesh;
            if (mesh == null) continue;

            Bounds mb = mesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = mb.center + Vector3.Scale(mb.extents, new Vector3(
                    (i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = space.InverseTransformPoint(r.transform.TransformPoint(c));
                if (!has) { result = new Bounds(p, Vector3.zero); has = true; }
                else result.Encapsulate(p);
            }
        }
        return result;
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
        body.GetComponent<Renderer>().enabled = false;

        // Nòng súng
        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.transform.SetParent(gun.transform);
        barrel.transform.localPosition = new Vector3(0, 0.02f, 0.35f);
        barrel.transform.localRotation = Quaternion.Euler(90, 0, 0);
        barrel.transform.localScale = new Vector3(0.04f, 0.2f, 0.04f);
        barrel.GetComponent<Renderer>().enabled = false;

        // Tay cầm
        GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grip.transform.SetParent(gun.transform);
        grip.transform.localPosition = new Vector3(0, -0.1f, -0.05f);
        grip.transform.localRotation = Quaternion.Euler(15, 0, 0);
        grip.transform.localScale = new Vector3(0.06f, 0.15f, 0.08f);
        grip.GetComponent<Renderer>().enabled = false;

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
        // Khi cutscene mở đầu đang diễn ra, tuyệt đối cất súng và không cho phép rút súng
        if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive)
        {
            if (gunInstance != null && gunInstance.activeSelf)
            {
                gunInstance.SetActive(false);
            }
            isEquipped = false;
            return;
        }

        // Nếu có WeaponManager đang hoạt động, nhường quyền xử lý phím cho WeaponManager
        // để tránh xung đột 2 script cùng bắt phím 1

        // Bấm phím 1 để rút/cất súng (chỉ khi KHÔNG có WeaponManager)
        if (!hasWeaponManager && Input.GetKeyDown(toggleKey))
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

    public void AddAmmo(int amount)
    {
        currentAmmo += amount;
        weaponNameText = string.Format("+{0} Viên đạn ({1}/{2})", amount, currentAmmo, maxAmmo);
        weaponNameTimer = 2.5f;
        Debug.Log("[FPSWeapon] Đã nạp thêm " + amount + " viên đạn! Hiện có: " + currentAmmo);
    }

    public void ToggleWeapon()
    {
        isEquipped = !isEquipped;

        if (gunInstance != null)
        {
            gunInstance.SetActive(isEquipped);

            // Bật/tắt tất cả Renderer bên trong model súng để hiện/ẩn đúng cách
            Renderer[] renderers = gunInstance.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                if (r != null) r.enabled = isEquipped;
            }
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

    /// <summary>
    /// Được gọi bởi WeaponManager khi rút súng qua hệ thống weaponSlots.
    /// Đồng bộ trạng thái isEquipped để các script khác (AxeController...) kiểm tra đúng.
    /// </summary>
    public void EquipFromManager()
    {
        if (!isEquipped)
        {
            isEquipped = true;

            if (gunInstance != null)
            {
                gunInstance.SetActive(true);
                Renderer[] renderers = gunInstance.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in renderers)
                {
                    if (r != null) r.enabled = true;
                }
            }

            weaponNameText = (gunModelPrefab != null) ? gunModelPrefab.name : "Súng";
            weaponNameTimer = 2f;

            if (equipSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(equipSound);
            }

            if (playerAnimator != null)
            {
                playerAnimator.SetBool("IsArmed", true);
            }

            Debug.Log("[FPSWeapon] EquipFromManager: Đã rút súng");
        }
    }

    /// <summary>
    /// Được gọi bởi WeaponManager khi cất súng.
    /// </summary>
    public void HolsterFromManager()
    {
        if (isEquipped)
        {
            isEquipped = false;

            if (gunInstance != null)
            {
                gunInstance.SetActive(false);
            }

            weaponNameText = "Cất súng";
            weaponNameTimer = 2f;

            if (playerAnimator != null)
            {
                playerAnimator.SetBool("IsArmed", false);
            }

            Debug.Log("[FPSWeapon] HolsterFromManager: Đã cất súng");
        }
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

        // Báo động tiếng súng: Mọi Zombie trong bán kính 80m sẽ nghe thấy và chạy lại
        HorrorGame.Enemy.EnemyAI.AlertAllZombies(transform.position, 80f);

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
        if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;
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

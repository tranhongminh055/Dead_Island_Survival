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

    [Header("Thông số súng & Đạn")]
    public float damage = 25f;
    public float range = 100f;
    public float fireRate = 8f;
    public int maxAmmo = 30;
    public int reserveAmmo = 60; // Số đạn dự trữ (nạp được 2 băng tiếp theo)
    public float reloadTime = 2f;

    [Header("Tích hợp Túi Đồ (Inventory)")]
    public HorrorGame.Inventory.ItemData weaponItemData; // Kéo file GunItem vào đây

    [Header("Phím tắt")]
    public KeyCode toggleKey = KeyCode.Alpha1; // Phím 1 để rút/cất súng

    [Header("Âm thanh & Hiệu ứng")]
    public AudioClip shootSound; // Kéo file âm thanh tiếng súng vào đây
    public AudioClip reloadSound; // (Tùy chọn) Kéo file âm thanh nạp đạn vào đây
    public AudioClip equipSound; // (Tùy chọn) Kéo file âm thanh rút súng vào đây
    public AudioClip emptyClickSound; // (Tùy chọn) Kéo file âm thanh cạch khi hết đạn vào đây
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

    // Hiệu ứng chớp lửa đầu nòng
    private GameObject muzzleFlameObj;
    private Coroutine muzzleFlashRoutine;
    private Transform muzzleTransform;

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
        SetupMuzzleFlashEffect();

        // Tự động gán âm thanh cạch khi hết đạn nếu chưa có
        if (emptyClickSound == null)
        {
            emptyClickSound = reloadSound;
        }

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

    void SetupMuzzleFlashEffect()
    {
        if (gunInstance == null) return;

        // 1. Tìm hoặc tạo điểm đầu nòng súng (Muzzle Point)
        Transform mPoint = gunInstance.transform.Find("MuzzlePoint");
        if (mPoint == null)
        {
            GameObject mpObj = new GameObject("MuzzlePoint");
            mpObj.transform.SetParent(gunInstance.transform, false);
            // gunLength thường là 0.55m -> đầu nòng ở khoảng z = gunLength * 0.52f, cao y = 0.035f
            mpObj.transform.localPosition = new Vector3(0f, 0.035f, gunLength * 0.52f);
            mpObj.transform.localRotation = Quaternion.identity;
            mPoint = mpObj.transform;
        }
        muzzleTransform = mPoint;

        // 2. Định vị đèn flashLight ngay tại đầu nòng để ánh sáng chớp lửa phát ra đúng chỗ
        if (flashLight != null)
        {
            flashLight.transform.SetParent(mPoint, false);
            flashLight.transform.localPosition = Vector3.zero;
            flashLight.color = new Color(1f, 0.85f, 0.3f);
            flashLight.range = 15f;
            flashLight.intensity = 0f;
        }

        // 3. Tự động khởi tạo ParticleSystem tia lửa nếu chưa có
        if (muzzleFlash == null)
        {
            GameObject psObj = new GameObject("MuzzleSparksFX");
            psObj.transform.SetParent(mPoint, false);
            psObj.transform.localPosition = Vector3.zero;
            psObj.transform.localRotation = Quaternion.identity;

            ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.4f, 1f), new Color(1f, 0.45f, 0.05f, 1f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 16) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 20f;
            shape.radius = 0.02f;

            ParticleSystemRenderer rend = psObj.GetComponent<ParticleSystemRenderer>();
            Shader s = Shader.Find("Particles/Additive");
            if (s == null) s = Shader.Find("Mobile/Particles/Additive");
            if (s == null) s = Shader.Find("Sprites/Default");
            if (s != null)
            {
                Material mat = new Material(s);
                mat.color = new Color(1f, 0.85f, 0.25f, 1f);
                rend.material = mat;
            }

            muzzleFlash = ps;
        }

        // 4. Tạo ngọn lửa chớp 3D rực sáng (Muzzle Flame Visual)
        if (muzzleFlameObj == null)
        {
            muzzleFlameObj = new GameObject("MuzzleFlameVisual");
            muzzleFlameObj.transform.SetParent(mPoint, false);
            muzzleFlameObj.transform.localPosition = Vector3.zero;

            MeshFilter mf = muzzleFlameObj.AddComponent<MeshFilter>();
            MeshRenderer mr = muzzleFlameObj.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            Vector3[] verts = new Vector3[]
            {
                // Quad 1 (Mặt phẳng XY)
                new Vector3(-0.15f, -0.15f, 0.02f), new Vector3(0.15f, -0.15f, 0.02f),
                new Vector3(0.15f, 0.15f, 0.02f), new Vector3(-0.15f, 0.15f, 0.02f),
                // Quad 2 (Chéo)
                new Vector3(-0.15f, 0.15f, 0.02f), new Vector3(0.15f, -0.15f, 0.02f),
                new Vector3(0.15f, 0.15f, 0.02f), new Vector3(-0.15f, -0.15f, 0.02f),
                // Ngọn lửa phụt ra phía trước (+Z)
                new Vector3(-0.09f, 0f, 0.01f), new Vector3(0.09f, 0f, 0.01f), new Vector3(0f, 0f, 0.35f),
                new Vector3(0f, -0.09f, 0.01f), new Vector3(0f, 0.09f, 0.01f), new Vector3(0f, 0f, 0.35f)
            };
            int[] tris = new int[]
            {
                0, 1, 2, 0, 2, 3, 2, 1, 0, 3, 2, 0,
                4, 5, 6, 4, 6, 7, 6, 5, 4, 7, 6, 4,
                8, 9, 10, 10, 9, 8,
                11, 12, 13, 13, 12, 11
            };
            Vector2[] uvs = new Vector2[]
            {
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 1),
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 1)
            };
            Color[] colors = new Color[]
            {
                new Color(1f, 0.6f, 0.1f, 0.95f), new Color(1f, 0.6f, 0.1f, 0.95f),
                new Color(1f, 0.95f, 0.3f, 1f), new Color(1f, 0.95f, 0.3f, 1f),
                new Color(1f, 0.6f, 0.1f, 0.95f), new Color(1f, 0.6f, 0.1f, 0.95f),
                new Color(1f, 0.95f, 0.3f, 1f), new Color(1f, 0.95f, 0.3f, 1f),
                new Color(1f, 0.8f, 0.2f, 1f), new Color(1f, 0.8f, 0.2f, 1f), new Color(1f, 1f, 0.9f, 1f),
                new Color(1f, 0.8f, 0.2f, 1f), new Color(1f, 0.8f, 0.2f, 1f), new Color(1f, 1f, 0.9f, 1f)
            };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.RecalculateNormals();
            mf.mesh = mesh;

            Shader s = Shader.Find("Particles/Additive");
            if (s == null) s = Shader.Find("Mobile/Particles/Additive");
            if (s == null) s = Shader.Find("Sprites/Default");
            Material mat = new Material(s);
            mat.color = new Color(1f, 0.9f, 0.3f, 1f);
            mr.material = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            muzzleFlameObj.SetActive(false);
        }
    }

    IEnumerator MuzzleFlashRoutine()
    {
        if (muzzleFlameObj != null)
        {
            muzzleFlameObj.SetActive(true);
            muzzleFlameObj.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            float s = Random.Range(0.85f, 1.35f);
            muzzleFlameObj.transform.localScale = new Vector3(s, s, s);
        }

        yield return new WaitForSeconds(0.045f);

        if (muzzleFlameObj != null)
        {
            muzzleFlameObj.SetActive(false);
        }
        muzzleFlashRoutine = null;
    }

    /// <summary>
    /// Đếm tổng số đạn đang có trong túi đồ
    /// </summary>
    public int GetInventoryAmmoCount()
    {
        int count = 0;
        if (HorrorGame.Inventory.InventoryManager.Instance != null && HorrorGame.Inventory.InventoryManager.Instance.slots != null)
        {
            foreach (var slot in HorrorGame.Inventory.InventoryManager.Instance.slots)
            {
                if (slot != null && slot.item != null && slot.amount > 0)
                {
                    if (slot.item.itemType == HorrorGame.Inventory.ItemType.Ammunition ||
                        (!string.IsNullOrEmpty(slot.item.itemID) && slot.item.itemID.ToLower().Contains("ammo")) ||
                        (!string.IsNullOrEmpty(slot.item.itemName) && slot.item.itemName.ToLower().Contains("đạn")))
                    {
                        count += slot.amount;
                    }
                }
            }
        }
        return count;
    }

    /// <summary>
    /// Tiêu thụ đạn từ túi đồ khi thay đạn
    /// </summary>
    public int ConsumeInventoryAmmo(int neededAmount)
    {
        int remainingNeeded = neededAmount;
        if (HorrorGame.Inventory.InventoryManager.Instance != null && HorrorGame.Inventory.InventoryManager.Instance.slots != null)
        {
            foreach (var slot in HorrorGame.Inventory.InventoryManager.Instance.slots)
            {
                if (slot != null && slot.item != null && slot.amount > 0)
                {
                    if (slot.item.itemType == HorrorGame.Inventory.ItemType.Ammunition ||
                        (!string.IsNullOrEmpty(slot.item.itemID) && slot.item.itemID.ToLower().Contains("ammo")) ||
                        (!string.IsNullOrEmpty(slot.item.itemName) && slot.item.itemName.ToLower().Contains("đạn")))
                    {
                        int take = Mathf.Min(remainingNeeded, slot.amount);
                        slot.RemoveAmount(take);
                        remainingNeeded -= take;
                        if (remainingNeeded <= 0) break;
                    }
                }
            }
            if (HorrorGame.Inventory.InventoryManager.Instance.onInventoryChangedEvent != null)
            {
                HorrorGame.Inventory.InventoryManager.Instance.onInventoryChangedEvent();
            }
        }
        return neededAmount - remainingNeeded;
    }

    /// <summary>
    /// Dừng ngay lập tức âm thanh bắn súng khi hết đạn, nạp đạn hoặc cất súng
    /// </summary>
    public void StopShootAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            if (audioSource.clip == shootSound)
            {
                audioSource.Stop();
                audioSource.clip = null;
            }
        }
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
        if (HorrorGame.UI.GameMenuManager.IsPaused) return;

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

        if (!isEquipped)
        {
            StopShootAudio();
            return;
        }

        if (isReloading)
        {
            StopShootAudio();
            return;
        }

        // Khi hết đạn trong băng -> Dừng ngay lập tức âm thanh bắn súng đang phát dở
        if (currentAmmo <= 0)
        {
            StopShootAudio();
        }

        // Bấm phím R để thay đạn
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (currentAmmo < maxAmmo)
            {
                int totalAvailable = reserveAmmo + GetInventoryAmmoCount();
                if (totalAvailable > 0)
                {
                    StopShootAudio();
                    StartCoroutine(Reload());
                    return;
                }
                else
                {
                    weaponNameText = "Không còn đạn dự trữ trong túi đồ!";
                    weaponNameTimer = 2f;
                    if (emptyClickSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(emptyClickSound);
                    }
                }
            }
            else
            {
                weaponNameText = "Băng đạn đã đầy!";
                weaponNameTimer = 1.5f;
            }
        }

        // Bấm chuột trái để bắn
        if (Input.GetButton("Fire1"))
        {
            if (currentAmmo <= 0)
            {
                // Khi hết đạn -> Tuyệt đối ngắt tiếng súng nổ
                StopShootAudio();

                // Phát tiếng cạch cạch (chỉ kêu khi vừa nhấn chuột xuống)
                if (Input.GetButtonDown("Fire1"))
                {
                    int totalAvailable = reserveAmmo + GetInventoryAmmoCount();
                    if (totalAvailable > 0)
                    {
                        weaponNameText = "Hết đạn trong băng! Bấm [R] để nạp đạn";
                    }
                    else
                    {
                        weaponNameText = "HẾT SẠCH ĐẠN! Cần tìm thêm hộp đạn";
                    }
                    weaponNameTimer = 2f;

                    if (emptyClickSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(emptyClickSound);
                    }
                }
            }
            else if (Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + 1f / fireRate;
                Shoot();
            }
        }

        // Nhả chuột trái -> Tắt ngay lập tức âm thanh bắn súng
        if (Input.GetButtonUp("Fire1"))
        {
            StopShootAudio();
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
        reserveAmmo += amount;
        weaponNameText = string.Format("+{0} Viên đạn dự trữ! ({1} trong băng | {2} dự trữ)", amount, currentAmmo, (reserveAmmo + GetInventoryAmmoCount()));
        weaponNameTimer = 2.5f;
        Debug.Log("[FPSWeapon] Đã nạp thêm " + amount + " viên đạn vào dự trữ! Hiện có: " + reserveAmmo);
    }

    public void ToggleWeapon()
    {
        StopShootAudio();
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
        StopShootAudio();
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
        if (currentAmmo <= 0) return;
        currentAmmo--;

        // Bật tia lửa (nếu có Particle)
        if (muzzleFlash != null) muzzleFlash.Play();
        
        // Kích hoạt ngọn lửa chớp 3D tại đầu nòng
        if (muzzleFlameObj != null)
        {
            if (muzzleFlashRoutine != null) StopCoroutine(muzzleFlashRoutine);
            muzzleFlashRoutine = StartCoroutine(MuzzleFlashRoutine());
        }

        // Bật ánh sáng chớp lửa (tăng cường độ sáng mạnh tại đầu nòng)
        if (flashLight != null) flashLight.intensity = 14f;

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

        // Bắn tia Raycast chính xác từ tâm màn hình (Camera Viewport Center)
        Camera cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        Ray ray = (cam != null) ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)) : new Ray(transform.position, transform.forward);

        RaycastHit[] hits = Physics.RaycastAll(ray, range);
        RaycastHit closestHit = new RaycastHit();
        bool hasHit = false;
        float minDistance = float.MaxValue;
        Transform playerRoot = transform.root;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit h = hits[i];
            if (h.collider == null) continue;
            // Bỏ qua chính người chơi và vũ khí
            if (h.collider.transform.IsChildOf(playerRoot) || h.collider.CompareTag("Player")) continue;
            // Bỏ qua Trigger
            if (h.collider.isTrigger) continue;

            if (h.distance < minDistance)
            {
                minDistance = h.distance;
                closestHit = h;
                hasHit = true;
            }
        }

        if (hasHit)
        {
            RaycastHit hit = closestHit;
            Debug.Log("[FPSWeapon] Bắn trúng: " + hit.transform.name);

            // 1. Kiểm tra Zombie (ở chính nó, hoặc cha/con)
            HorrorGame.Enemy.EnemyAI enemy = hit.transform.GetComponent<HorrorGame.Enemy.EnemyAI>();
            if (enemy == null) enemy = hit.transform.GetComponentInParent<HorrorGame.Enemy.EnemyAI>();
            if (enemy == null) enemy = hit.transform.GetComponentInChildren<HorrorGame.Enemy.EnemyAI>();

            if (enemy != null)
            {
                // Kiểm tra bắn trúng đầu (Headshot: cao hơn chân 1.35m)
                float currentDamage = damage;
                bool isHeadshot = (hit.point.y - enemy.transform.position.y) > 1.35f;
                if (isHeadshot)
                {
                    currentDamage *= 2f;
                    Debug.Log("🎯 [FPSWeapon] HEADSHOT Zombie! Sát thương nhân đôi: " + currentDamage);
                }

                enemy.TakeDamage(currentDamage, hit.point);
                HorrorGame.Enemy.EnemyAI.SpawnBloodImpact(hit.point, hit.normal);
            }
            else
            {
                // 2. Kiểm tra Thú Rừng (AnimalAI)
                HorrorGame.Survival.AnimalAI animal = hit.transform.GetComponent<HorrorGame.Survival.AnimalAI>();
                if (animal == null) animal = hit.transform.GetComponentInParent<HorrorGame.Survival.AnimalAI>();
                if (animal != null)
                {
                    animal.TakeDamage(damage);
                    HorrorGame.Enemy.EnemyAI.SpawnBloodImpact(hit.point, hit.normal);
                }
            }

            // Đẩy vật lý (nếu có Rigidbody)
            if (hit.rigidbody != null)
            {
                hit.rigidbody.AddForce(-hit.normal * 60f);
            }
        }
    }

    IEnumerator Reload()
    {
        isReloading = true;

        // Dừng ngay lập tức âm thanh bắn súng khi bắt đầu thay đạn
        StopShootAudio();

        weaponNameText = "Đang nạp đạn...";
        Debug.Log("Đang nạp đạn...");

        // Phát âm thanh nạp đạn (nếu có)
        if (reloadSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(reloadSound);
        }

        yield return new WaitForSeconds(reloadTime);

        int needed = maxAmmo - currentAmmo;
        // 1. Tiêu thụ đạn trong túi đồ trước (nếu có)
        int fromInv = ConsumeInventoryAmmo(needed);
        needed -= fromInv;

        // 2. Tiêu thụ từ đạn dự trữ reserveAmmo
        int fromReserve = 0;
        if (needed > 0 && reserveAmmo > 0)
        {
            fromReserve = Mathf.Min(needed, reserveAmmo);
            reserveAmmo -= fromReserve;
        }

        currentAmmo += (fromInv + fromReserve);
        isReloading = false;
        weaponNameText = (gunModelPrefab != null) ? gunModelPrefab.name : "Súng";
        int totalReserve = reserveAmmo + GetInventoryAmmoCount();
        Debug.Log(string.Format("Nạp đạn xong! Trong băng: {0}/{1} | Dự trữ: {2}", currentAmmo, maxAmmo, totalReserve));
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
        int totalReserve = reserveAmmo + GetInventoryAmmoCount();
        string ammoText = currentAmmo + " / " + totalReserve;
        if (isReloading) ammoText = "Nạp đạn...";
        else if (currentAmmo == 0 && totalReserve == 0) ammoText = "0 / 0 (HẾT ĐẠN)";

        float ax = Screen.width - 240;
        float ay = Screen.height - 60;

        GUI.color = Color.black;
        GUI.Label(new Rect(ax + 2, ay + 2, 230, 40), ammoText, ammoShadowStyle);
        
        if (currentAmmo == 0) GUI.color = Color.red;
        else GUI.color = Color.white;
        
        GUI.Label(new Rect(ax, ay, 230, 40), ammoText, ammoStyle);

        // === TÊN SÚNG (hiện 2 giây khi đổi súng) ===
        if (weaponNameTimer > 0)
        {
            float alpha = Mathf.Clamp01(weaponNameTimer);
            weaponNameStyle.normal.textColor = new Color(1, 1, 1, alpha);
            GUI.Label(new Rect(cx - 150, Screen.height - 130, 300, 40), weaponNameText, weaponNameStyle);
        }
    }
}

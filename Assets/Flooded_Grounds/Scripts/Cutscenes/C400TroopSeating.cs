using UnityEngine;
using System.Collections.Generic;
using HorrorGame.Cutscenes;

/// <summary>
/// Spawn lính ngồi lên các ghế trong khoang vận tải C400.
/// Ghế C400 xếp dọc 2 bên thành máy bay (kiểu ghế nhảy dù / troop transport),
/// lính ngồi quay mặt vào giữa khoang.
/// 
/// QUAN TRỌNG: Script này KHÔNG parent lính vào cabin (vì cabin scale 10x gây lỗi).
/// Thay vào đó, lính được spawn ở World Space và một component riêng
/// (SoldierSeatAnchor) giữ lính ngồi đúng vị trí ghế mỗi frame.
/// </summary>
public class C400TroopSeating : MonoBehaviour
{
    [Header("── Cấu Hình ──")]
    [Range(2, 20)]
    public int soldiersPerSide = 10;

    public float seatSpacing = 1.8f;

    [Header("── Model Lính ──")]
    public GameObject soldierPrefab;

    [HideInInspector]
    public List<GameObject> spawnedSoldiers = new List<GameObject>();
    private bool hasSpawned = false;

    public void SpawnTroops()
    {
        if (hasSpawned) return;
        hasSpawned = true;

        GameObject cabin = this.gameObject;

        // ═══════════════════════════════════════════════════════
        // BƯỚC 1: Tìm các mesh đệm ghế (Cushions) để lấy tọa độ world-space
        // ═══════════════════════════════════════════════════════
        Renderer cushionLRen = null, cushionRRen = null;
        foreach (Transform t in cabin.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "CargoChairsCushionsL")
                cushionLRen = t.GetComponent<Renderer>();
            else if (t.name == "CargoChairsCushionsR")
                cushionRRen = t.GetComponent<Renderer>();
        }

        if (cushionLRen == null || cushionRRen == null)
        {
            Debug.LogError("[C400TroopSeating] KHÔNG tìm thấy CargoChairsCushionsL/R! Hủy spawn.");
            return;
        }

        // Bounds ở world-space (đã bao gồm scale 10x của cabin)
        Bounds bL = cushionLRen.bounds;
        Bounds bR = cushionRRen.bounds;

        // Tọa độ X: tâm của từng hàng ghế
        float xLeft = bL.center.x;
        float xRight = bR.center.x;

        // Tọa độ Z: dọc theo khoang
        float zMin = Mathf.Min(bL.min.z, bR.min.z);
        float zMax = Mathf.Max(bL.max.z, bR.max.z);

        // Tọa độ Y: MẶT TRÊN của đệm ghế (chỗ mông ngồi)
        // Đây là điểm mà MÔNG LÍNH phải chạm vào
        float seatSurfaceY = Mathf.Max(bL.max.y, bR.max.y);

        // Tâm X cabin (để xác định hướng quay mặt)
        float cabinCenterX = (xLeft + xRight) * 0.5f;

        Debug.Log("[C400Troops] Cushion world bounds:" +
                  " L.center=" + bL.center + " R.center=" + bR.center +
                  " seatSurfaceY=" + seatSurfaceY +
                  " zRange=[" + zMin + "," + zMax + "]");

        // ═══════════════════════════════════════════════════════
        // BƯỚC 2: Tìm hoặc load model lính
        // ═══════════════════════════════════════════════════════
        GameObject soldierModel = soldierPrefab;
        if (soldierModel == null)
            soldierModel = FindSoldierModel();

        // ═══════════════════════════════════════════════════════
        // BƯỚC 3: Tạo container ở WORLD SPACE (KHÔNG parent vào cabin!)
        // ═══════════════════════════════════════════════════════
        GameObject troopContainer = new GameObject("[C400_TROOPS]");
        // Đặt ở gốc world, scale (1,1,1) - KHÔNG parent vào cabin
        troopContainer.transform.position = Vector3.zero;
        troopContainer.transform.rotation = Quaternion.identity;
        troopContainer.transform.localScale = Vector3.one;

        // ═══════════════════════════════════════════════════════
        // BƯỚC 4: Tính khoảng cách giữa các lính
        // ═══════════════════════════════════════════════════════
        float zLength = zMax - zMin;
        float spacing = Mathf.Min(seatSpacing, zLength / Mathf.Max(1, soldiersPerSide - 1));
        float zStart = (zMin + zMax) * 0.5f - (soldiersPerSide - 1) * spacing * 0.5f;

        Debug.Log("[C400Troops] Spawning " + (soldiersPerSide * 2) + " soldiers" +
                  " seatSurfaceY=" + seatSurfaceY +
                  " spacing=" + spacing);

        // ═══════════════════════════════════════════════════════
        // BƯỚC 5: Spawn 2 hàng lính
        // ═══════════════════════════════════════════════════════
        for (int i = 0; i < soldiersPerSide; i++)
        {
            float z = zStart + i * spacing;

            // Bên trái - quay mặt về phía tâm cabin
            float yRotL = cabinCenterX > xLeft ? 90f : -90f;
            SpawnOneSoldier(soldierModel,
                new Vector3(xLeft, seatSurfaceY, z), yRotL,
                troopContainer.transform, "Soldier_L" + i,
                seatSurfaceY);

            // Bên phải - quay mặt về phía tâm cabin
            float yRotR = cabinCenterX > xRight ? 90f : -90f;
            SpawnOneSoldier(soldierModel,
                new Vector3(xRight, seatSurfaceY, z), yRotR,
                troopContainer.transform, "Soldier_R" + i,
                seatSurfaceY);
        }

        Debug.Log("[C400Troops] ✅ Đã spawn " + spawnedSoldiers.Count + " lính!");
    }

    private void SpawnOneSoldier(GameObject model, Vector3 seatWorldPos,
        float yRotation, Transform parent, string name, float seatSurfaceY)
    {
        // Đặt gốc model (chân) tại vị trí mặt ghế trước
        // Sau đó sẽ điều chỉnh bằng cách di chuyển hips bone
        Quaternion rot = Quaternion.Euler(0, yRotation, 0);
        GameObject soldier;

        if (model != null)
        {
            // Spawn ở world space, parent là container (scale 1,1,1)
            soldier = Instantiate(model, seatWorldPos, rot, parent);
            // Cabin có scale 10x, lính phải scale 10x theo để khớp kích thước
            soldier.transform.localScale = Vector3.one * 10f;
        }
        else
        {
            // Placeholder capsule
            soldier = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            soldier.transform.position = seatWorldPos;
            soldier.transform.rotation = rot;
            soldier.transform.SetParent(parent, true);
            soldier.transform.localScale = new Vector3(0.3f, 0.6f, 0.3f);

            Renderer r = soldier.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.25f, 0.35f, 0.2f);
                r.sharedMaterial = mat;
            }
        }

        soldier.name = name;

        // Tắt tất cả Collider để không chặn player
        foreach (var c in soldier.GetComponentsInChildren<Collider>())
            c.enabled = false;

        // Tắt Animator để không bị override tư thế
        Animator anim = soldier.GetComponentInChildren<Animator>();
        if (anim != null) anim.enabled = false;

        spawnedSoldiers.Add(soldier);

        // ═══════════════════════════════════════════════════════
        // ÁP DỤNG TƯ THẾ NGỒI + KÉO MÔNG XUỐNG MẶT GHẾ
        // ═══════════════════════════════════════════════════════
        ApplySeatedPoseAndSnap(soldier, seatSurfaceY);
    }

    /// <summary>
    /// Áp dụng tư thế ngồi BẰNG CÁCH xoay xương (FK),
    /// sau đó DI CHUYỂN toàn bộ model để mông (hips) chạm mặt ghế.
    /// 
    /// Vấn đề FK: Khi xoay đùi 75 độ, mông KHÔNG tụt xuống.
    /// Chỉ có bàn chân bị giật lên cao. Mông vẫn ở vị trí cũ (cách gốc model ~0.95m).
    /// → Giải pháp: Sau khi xoay xương, đo khoảng cách từ hips tới mặt ghế,
    ///   rồi dịch chuyển TOÀN BỘ model xuống cho hips chạm ghế.
    /// </summary>
    private void ApplySeatedPoseAndSnap(GameObject soldier, float seatSurfaceY)
    {
        // Tìm tất cả bones
        Transform hips = null;
        Transform leftUpLeg = null, rightUpLeg = null;
        Transform leftLeg = null, rightLeg = null;
        Transform leftFoot = null, rightFoot = null;
        Transform spine = null, spine1 = null, spine2 = null;
        Transform head = null;
        Transform leftArm = null, rightArm = null;
        Transform leftForeArm = null, rightForeArm = null;

        foreach (Transform t in soldier.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name;
            if (n.EndsWith("Hips")) hips = t;
            else if (n.EndsWith("LeftUpLeg")) leftUpLeg = t;
            else if (n.EndsWith("RightUpLeg")) rightUpLeg = t;
            else if (n.EndsWith("LeftLeg")) leftLeg = t;
            else if (n.EndsWith("RightLeg")) rightLeg = t;
            else if (n.EndsWith("LeftFoot")) leftFoot = t;
            else if (n.EndsWith("RightFoot")) rightFoot = t;
            else if (n.EndsWith("Spine") && !n.EndsWith("Spine1") && !n.EndsWith("Spine2")) spine = t;
            else if (n.EndsWith("Spine1")) spine1 = t;
            else if (n.EndsWith("Spine2")) spine2 = t;
            else if (n.EndsWith("Head")) head = t;
            else if (n.EndsWith("LeftArm") && !n.Contains("Fore")) leftArm = t;
            else if (n.EndsWith("RightArm") && !n.Contains("Fore")) rightArm = t;
            else if (n.EndsWith("LeftForeArm")) leftForeArm = t;
            else if (n.EndsWith("RightForeArm")) rightForeArm = t;
        }

        if (hips == null)
        {
            Debug.LogWarning("[C400Troops] Không tìm thấy Hips bone cho " + soldier.name);
            return;
        }

        // ── BƯỚC A: Xoay xương để tạo tư thế ngồi ──
        float thighAngle = 75f + Random.Range(-3f, 3f);
        float kneeAngle = 85f + Random.Range(-5f, 5f);

        if (leftUpLeg != null) leftUpLeg.localRotation = Quaternion.Euler(thighAngle, -3f, 0f);
        if (rightUpLeg != null) rightUpLeg.localRotation = Quaternion.Euler(thighAngle, 3f, 0f);
        if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(kneeAngle, 0f, 0f);
        if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(kneeAngle, 0f, 0f);
        if (leftFoot != null) leftFoot.localRotation = Quaternion.Euler(-5f, 0f, 0f);
        if (rightFoot != null) rightFoot.localRotation = Quaternion.Euler(-5f, 0f, 0f);

        float spineAngle = -2f + Random.Range(-2f, 2f);
        if (spine != null) spine.localRotation = Quaternion.Euler(spineAngle, 0f, 0f);
        if (spine1 != null) spine1.localRotation = Quaternion.Euler(spineAngle * 0.4f, 0f, 0f);
        if (spine2 != null) spine2.localRotation = Quaternion.identity;

        // Tay đặt trên đùi
        if (leftArm != null) leftArm.localRotation = Quaternion.Euler(70f, -10f, 0f);
        if (rightArm != null) rightArm.localRotation = Quaternion.Euler(70f, 10f, 0f);
        if (leftForeArm != null) leftForeArm.localRotation = Quaternion.Euler(25f, 0f, 0f);
        if (rightForeArm != null) rightForeArm.localRotation = Quaternion.Euler(25f, 0f, 0f);

        // Đầu nhìn thẳng
        if (head != null) head.localRotation = Quaternion.identity;

        // ── BƯỚC B: KÉO MÔNG XUỐNG MẶT GHẾ ──
        // Sau khi xoay xương, hips vẫn ở vị trí cũ (cao hơn gốc model ~0.95m).
        // Ta cần dịch chuyển TOÀN BỘ soldier xuống để hips.position.y == seatSurfaceY
        float hipsWorldY = hips.position.y;
        float deltaY = seatSurfaceY - hipsWorldY;

        Debug.Log("[C400Troops] " + soldier.name +
                  ": hips.y=" + hipsWorldY +
                  " seatY=" + seatSurfaceY +
                  " deltaY=" + deltaY +
                  " soldierPos=" + soldier.transform.position);

        soldier.transform.position += new Vector3(0f, deltaY, 0f);

        Debug.Log("[C400Troops] " + soldier.name +
                  " → final pos=" + soldier.transform.position +
                  " hips.y=" + hips.position.y);

        // ── BƯỚC C: Gắn component giữ tư thế mỗi frame ──
        // (Không dùng AirplanePassengerSeatPose nữa vì nó không xử lý hips position)
        SoldierSeatLock seatLock = soldier.AddComponent<SoldierSeatLock>();
        seatLock.targetHipsY = seatSurfaceY;
        seatLock.hips = hips;
        seatLock.leftUpLeg = leftUpLeg;
        seatLock.rightUpLeg = rightUpLeg;
        seatLock.leftLeg = leftLeg;
        seatLock.rightLeg = rightLeg;
        seatLock.leftFoot = leftFoot;
        seatLock.rightFoot = rightFoot;
        seatLock.spine = spine;
        seatLock.spine1 = spine1;
        seatLock.spine2 = spine2;
        seatLock.leftArm = leftArm;
        seatLock.rightArm = rightArm;
        seatLock.leftForeArm = leftForeArm;
        seatLock.rightForeArm = rightForeArm;
        seatLock.head = head;
        seatLock.thighAngle = thighAngle;
        seatLock.kneeAngle = kneeAngle;
        seatLock.spineAngle = spineAngle;

        // Bật các SkinnedMeshRenderer
        foreach (var smr in soldier.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            smr.enabled = true;
            smr.updateWhenOffscreen = true;
        }

        // Ẩn vũ khí
        foreach (Transform t in soldier.GetComponentsInChildren<Transform>(true))
        {
            if (t == null) continue;
            string n = t.name.ToLower();
            if (n.Contains("gun") || n.Contains("weapon") || n.Contains("axe") ||
                n.Contains("placeholder") || n.Contains("aim") || n.Contains("target"))
            {
                t.gameObject.SetActive(false);
            }
        }
    }

    private GameObject FindSoldierModel()
    {
#if UNITY_EDITOR
        string[] paths = {
            "Assets/Flooded_Grounds/Character No Animation/Ch15_nonPBR.fbx",
        };
        foreach (string p in paths)
        {
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (prefab != null) return prefab;
        }
#endif
        return Resources.Load<GameObject>("Ch15_nonPBR");
    }

    public void ClearTroops()
    {
        foreach (var s in spawnedSoldiers)
            if (s != null) Destroy(s);
        spawnedSoldiers.Clear();
        hasSpawned = false;

        GameObject container = GameObject.Find("[C400_TROOPS]");
        if (container != null) Destroy(container);
    }
}

/// <summary>
/// Component gắn trên mỗi lính, giữ tư thế ngồi và khóa mông (hips) vào mặt ghế
/// MỖI FRAME trong LateUpdate.
/// Đây là giải pháp triệt để cho vấn đề FK không tự hạ mông xuống.
/// </summary>
public class SoldierSeatLock : MonoBehaviour
{
    [HideInInspector] public float targetHipsY;
    [HideInInspector] public Transform hips;
    [HideInInspector] public Transform leftUpLeg, rightUpLeg;
    [HideInInspector] public Transform leftLeg, rightLeg;
    [HideInInspector] public Transform leftFoot, rightFoot;
    [HideInInspector] public Transform spine, spine1, spine2;
    [HideInInspector] public Transform leftArm, rightArm;
    [HideInInspector] public Transform leftForeArm, rightForeArm;
    [HideInInspector] public Transform head;
    [HideInInspector] public float thighAngle;
    [HideInInspector] public float kneeAngle;
    [HideInInspector] public float spineAngle;

    private void LateUpdate()
    {
        if (hips == null) return;

        // 1. Xoay xương lại (phòng trường hợp Animator hoặc script khác override)
        if (leftUpLeg != null) leftUpLeg.localRotation = Quaternion.Euler(thighAngle, -3f, 0f);
        if (rightUpLeg != null) rightUpLeg.localRotation = Quaternion.Euler(thighAngle, 3f, 0f);
        if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(kneeAngle, 0f, 0f);
        if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(kneeAngle, 0f, 0f);
        if (leftFoot != null) leftFoot.localRotation = Quaternion.Euler(-5f, 0f, 0f);
        if (rightFoot != null) rightFoot.localRotation = Quaternion.Euler(-5f, 0f, 0f);
        if (spine != null) spine.localRotation = Quaternion.Euler(spineAngle, 0f, 0f);
        if (spine1 != null) spine1.localRotation = Quaternion.Euler(spineAngle * 0.4f, 0f, 0f);
        if (spine2 != null) spine2.localRotation = Quaternion.identity;
        if (leftArm != null) leftArm.localRotation = Quaternion.Euler(70f, -10f, 0f);
        if (rightArm != null) rightArm.localRotation = Quaternion.Euler(70f, 10f, 0f);
        if (leftForeArm != null) leftForeArm.localRotation = Quaternion.Euler(25f, 0f, 0f);
        if (rightForeArm != null) rightForeArm.localRotation = Quaternion.Euler(25f, 0f, 0f);
        if (head != null) head.localRotation = Quaternion.identity;

        // 2. KÉO MÔNG XUỐNG MẶT GHẾ - MỖI FRAME
        // Đây là bước quan trọng nhất: đo khoảng cách từ hips tới mặt ghế,
        // rồi dịch chuyển toàn bộ soldier xuống cho khớp.
        float currentHipsY = hips.position.y;
        float delta = targetHipsY - currentHipsY;
        if (Mathf.Abs(delta) > 0.01f)
        {
            transform.position += new Vector3(0f, delta, 0f);
        }
    }
}

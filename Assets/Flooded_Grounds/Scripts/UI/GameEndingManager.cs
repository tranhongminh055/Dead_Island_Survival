using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using HorrorGame.Player;
using HorrorGame.Environment;
using HorrorGame.Enemy;

namespace HorrorGame.UI
{
    public enum EndingStoryStage
    {
        SurviveAndPrepare = 0,       // Chưa đủ điều kiện ngày sinh tồn
        SearchSecretLab = 1,         // Tự khám phá tìm Phòng Thí Nghiệm Bí Mật ẩn trong rừng (không có tia sáng/waypoint chỉ thẳng!)
        BossFightActive = 2,         // Đang chiến đấu với Nữ Hoàng Ký Sinh bên ngoài Phòng Thí Nghiệm
        PickupDroppedCure = 3,       // Boss đã chết -> Phải lại gần nhặt Ống Huyết Thanh rơi trên mặt đất!
        ReturnToLabAndCarry = 4,     // Mang Huyết Thanh quay lại Phòng Thí Nghiệm tiêm & CÕNG Người Sống Sót
        CarryingSurvivorToDock = 5,  // Đang CÕNG Người Sống Sót chạy ra Bến Tàu ven sông!
        Ending1_Escape = 6,          // Hành động: Bấm [E] lên Tàu -> Ending 1
        Ending2_StayForever = 7,     // Hành động: Bấm [E] đập Trạm Tín Hiệu -> Ending 2 (True Ending)
        FreeRoamAfterEnding2 = 8     // Sinh tồn tự do mãi mãi sau Ending 2
    }

    /// <summary>
    /// GameEndingManager — Hệ thống Cốt Truyện Kết Game 2 Ending Chuẩn Hành Động (Action-Driven như The Forest):
    /// 1. Người Sống Sót bị giam trong Phòng Thí Nghiệm Bí Mật ẩn sâu trong rừng (KHÔNG có cột khói hay waypoint lộ từ xa -> phải tự khám phá!).
    /// 2. Khi đủ điều kiện sống sót tới Ngày 2 & tìm ra Phòng Thí Nghiệm -> bấm [E] đánh thức Trùm Cuối Nữ Hoàng Ký Sinh.
    /// 3. Hạ gục Boss -> rơi vật thể 3D Ống Huyết Thanh phát sáng nằm trên mặt đất để Player tự lại gần bấm [E] nhặt.
    /// 4. Quay vào Phòng Thí Nghiệm bấm [E] tiêm Huyết Thanh & CÕNG Người Sống Sót trên vai cùng chạy ra Bến Tàu ven sông!
    /// 5. Tại Bến Tàu ven sông (trên mặt nước thật), KHÔNG hiện menu chọn Ending — hành động vật lý của Player quyết định kết cục:
    ///    - Lại gần Con Tàu Cứu Hộ lớn đang nổ máy dưới sông bấm [E] -> ENDING 1: CÙNG NHAU THOÁT KHỎI ĐỊA NGỤC.
    ///    - Lại gần Trạm Tín Hiệu trên cầu tàu bấm [E] -> Đưa anh ta lên tàu rời đi & đập nát trạm -> ENDING 2: Ở LẠI MÃI MÃI (TRUE ENDING).
    /// 6. Bất tử hoàn toàn (isInvincible = true) khi Ending bắt đầu -> tuyệt đối không bị quái cắn chết trong lúc xem Ending!
    ///
    /// Phím test nhanh trong Unity Editor:
    /// - [Left Ctrl + F9]: Dịch chuyển tới ngay trước cửa Phòng Thí Nghiệm Bí Mật (đủ Ngày 2).
    /// - [Left Ctrl + F10]: Mô phỏng đang CÕNG Người Sống Sót & dịch chuyển ra ngay Bến Tàu ven sông để thử 2 hành động Ending!
    /// </summary>
    public class GameEndingManager : MonoBehaviour
    {
        public static GameEndingManager Instance { get; private set; }
        public static int TotalEnemiesKilled = 0;

        public static bool IsCarryingSurvivor
        {
            get { return Instance != null && Instance.Stage == EndingStoryStage.CarryingSurvivorToDock; }
        }

        public static bool IsEndingUIActive
        {
            get
            {
                return Instance != null && (
                    Instance.Stage == EndingStoryStage.Ending1_Escape ||
                    Instance.Stage == EndingStoryStage.Ending2_StayForever);
            }
        }

        [Header("── Điều Kiện Mở Khóa Kết Game ──")]
        [Tooltip("Số ngày tối thiểu phải sống sót để kích hoạt giải cứu & Boss cuối")]
        public int requiredSurvivalDay = 2;

        [Header("── Vị Trí Các Mốc Cốt Truyện ──")]
        [Tooltip("Phòng Thí Nghiệm Bí Mật ẩn sâu trong rừng (cách xa xác máy bay)")]
        public Vector3 secretLabPos = new Vector3(415f, 19.0f, 640f);

        [Tooltip("Bến Tàu ven sông (nằm sát mép nước)")]
        public Vector3 dockShorePos = new Vector3(605.8f, 17.5f, 710.8f);

        public EndingStoryStage Stage { get; private set; }
        public bool HasParasiteCure { get; private set; }
        public bool HasDefeatedBoss { get; private set; }
        public bool HasFoundSurvivor { get; private set; }

        private Transform playerTransform;
        private PlayerController playerController;
        private PlayerStats playerStats;

        // Các vật thể 3D thực trong màn chơi
        private GameObject secretLabRoot;
        private GameObject survivorVisualObj;
        private GameObject droppedCureObj;
        private Vector3 droppedCurePos;
        private GameObject dockConsoleObj;
        private GameObject rescueBoatObj;
        private GameObject carriedVisualOnPlayer;
        private Light dockConsoleLight;
        private ParasiteQueenBoss activeBoss;

        // Tọa độ tàu trên mặt nước thật & hướng chạy ra sông/biển
        private Vector3 boatWaterPos;
        private Vector3 boatSailDir;
        private Vector3 boatStartPos;

        // Cutscene 3D
        private Camera endingCam;
        private Camera prevMainCam;
        private const float CUTSCENE_DURATION = 8.2f;

        // UI
        private string activePrompt = "";
        private string storyBannerTitle = "";
        private string storyBannerSub = "";
        private float storyBannerTimer = 0f;
        private float endingTimer = 0f;
        private int finalSurvivedDays = 1;

        // Textures & Styles
        private Texture2D blackTex;
        private Texture2D panelTex;
        private Texture2D goldLineTex;
        private Texture2D redLineTex;
        private Texture2D btnNormalTex;
        private Texture2D btnHoverTex;
        private Texture2D btnAltNormalTex;
        private Texture2D btnAltHoverTex;

        private GUIStyle promptStyle;
        private GUIStyle bannerTitleStyle;
        private GUIStyle bannerSubStyle;
        private GUIStyle carryHudStyle;
        private GUIStyle choiceBtnStyle;
        private GUIStyle choiceAltBtnStyle;
        private GUIStyle endingBigTitleStyle;
        private GUIStyle endingSubStyle;
        private GUIStyle endingBodyStyle;
        private GUIStyle endingStatsStyle;
        private bool stylesReady = false;

        private static bool sceneHookRegistered = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            if (!sceneHookRegistered)
            {
                sceneHookRegistered = true;
                SceneManager.sceneLoaded += OnAnySceneLoaded;
            }
            EnsureInstanceForActiveScene();
        }

        private static void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureInstanceForActiveScene();
        }

        private static void EnsureInstanceForActiveScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName.ToLower().Contains("mainmenu")) return;

            if (FindObjectOfType<GameEndingManager>() == null)
            {
                GameObject obj = new GameObject("[GameEndingManager]");
                obj.AddComponent<GameEndingManager>();
            }
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            Stage = EndingStoryStage.SurviveAndPrepare;
        }

        private void OnEnable()
        {
            ParasiteQueenBoss.OnBossDefeatedEvent += OnParasiteQueenDefeated;
        }

        private void OnDisable()
        {
            ParasiteQueenBoss.OnBossDefeatedEvent -= OnParasiteQueenDefeated;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            FindPlayerRefs();
            ResolveSecretLabAndRiverDock();
            BuildSecretLabBunker3D();
            BuildRiverDockAndRescueBoat3D();
        }

        private void FindPlayerRefs()
        {
            if (playerTransform != null) return;
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null)
            {
                PlayerController pc = FindObjectOfType<PlayerController>();
                if (pc != null) p = pc.gameObject;
            }
            if (p != null)
            {
                playerTransform = p.transform;
                playerController = p.GetComponent<PlayerController>();
                playerStats = p.GetComponent<PlayerStats>();
            }
        }

        // =========================================================================
        // 1. ĐỊNH VỊ PHÒNG THÍ NGHIỆM BÍ MẬT (XA RỪNG SÂU) & BẾN TÀU SÁT SÔNG NƯỚC
        // =========================================================================
        private void ResolveSecretLabAndRiverDock()
        {
            // A. Phòng Thí Nghiệm Bí Mật: ẩn sâu trong rừng phía Tây Bắc / Bắc đảo (cách xa xác máy bay > 110m)
            GameObject manualLab = GameObject.Find("SecretLabSpawnPoint");
            if (manualLab != null)
            {
                secretLabPos = manualLab.transform.position;
            }
            else
            {
                secretLabPos = FindDryRemoteForestSpot(new Vector3(415f, 19f, 640f));
            }

            // B. Bến Tàu Ven Sông: tìm trực tiếp cầu tàu thật (Struct_Docking) hoặc mép nước thật (Terrain < 16.0m)
            GameObject manualDock = GameObject.Find("DockRescuePoint");
            if (manualDock != null)
            {
                dockShorePos = manualDock.transform.position;
            }
            else
            {
                FindRealRiverShoreline(out dockShorePos, out boatWaterPos, out boatSailDir);
            }

            if (boatSailDir.sqrMagnitude < 0.01f)
            {
                FindWaterDirectionFromShore(dockShorePos, out boatWaterPos, out boatSailDir);
            }

            Debug.Log(string.Format("🧪 [GameEndingManager] Phòng Thí Nghiệm Bí Mật tại {0} | Bến Tàu Ven Sông tại {1} | Tàu trên mặt nước tại {2} (hướng {3})",
                secretLabPos, dockShorePos, boatWaterPos, boatSailDir));
        }

        private Vector3 FindDryRemoteForestSpot(Vector3 preferred)
        {
            Terrain t = Terrain.activeTerrain;
            if (t == null) return preferred;

            // Quét quanh khu rừng sâu xa xác máy bay tìm bãi đất khô phẳng (Y >= 17.8m)
            for (float r = 0f; r <= 90f; r += 8f)
            {
                int steps = (r < 1f) ? 1 : 12;
                for (int i = 0; i < steps; i++)
                {
                    float rad = (i * (360f / steps)) * Mathf.Deg2Rad;
                    Vector3 cand = new Vector3(preferred.x + Mathf.Cos(rad) * r, 0f, preferred.z + Mathf.Sin(rad) * r);
                    float y = t.SampleHeight(cand) + t.transform.position.y;
                    if (y < 17.8f) continue;

                    Vector3 worldP = new Vector3(cand.x, y, cand.z);
                    // Tránh đè lên mái/sàn nhà sẵn có
                    if (Physics.Raycast(worldP + Vector3.up * 0.6f, Vector3.up, 35f, ~0, QueryTriggerInteraction.Ignore))
                        continue;

                    NavMeshHit nh;
                    if (NavMesh.SamplePosition(worldP, out nh, 4f, NavMesh.AllAreas) && Mathf.Abs(nh.position.y - y) < 1.2f)
                    {
                        return worldP;
                    }
                }
            }

            preferred.y = Mathf.Max(t.SampleHeight(preferred) + t.transform.position.y, 18.0f);
            return preferred;
        }

        /// <summary>
        /// Tìm chính xác mép sông / mặt nước thật (nơi Terrain chìm dưới 16.0m) để đặt Bến Tàu sát nước & Tàu nổi 100% trên sông!
        /// </summary>
        private void FindRealRiverShoreline(out Vector3 shorePos, out Vector3 waterBoatPos, out Vector3 openWaterDir)
        {
            Terrain t = Terrain.activeTerrain;
            // Mặc định hướng về khu cầu tàu thật Struct_Docking_A (1) tại (605.8, 17.1, 710.8) hoặc sông gần xác máy bay
            shorePos = new Vector3(605.8f, 17.2f, 710.8f);
            waterBoatPos = new Vector3(598f, 16.85f, 695f);
            openWaterDir = new Vector3(0f, 0f, -1f);

            // 1. Ưu tiên tìm vật thể Struct_Docking thật trong Scene_A
            MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                string n = renderers[i].gameObject.name.ToLower();
                if (n.Contains("struct_docking"))
                {
                    Vector3 dp = renderers[i].bounds.center;
                    shorePos = new Vector3(dp.x, Mathf.Max(dp.y, 17.3f), dp.z);
                    if (FindWaterDirectionFromShore(shorePos, out waterBoatPos, out openWaterDir))
                    {
                        return;
                    }
                }
            }

            // 2. Nếu không thấy Struct_Docking, quét từ khu xác máy bay (537, 545) ra tìm mép sông/biển gần nhất có đường nước dài > 60m
            if (t != null)
            {
                Vector3 center = ZombieSpawner.CRASH_SITE_CENTER;
                for (float r = 25f; r <= 240f; r += 10f)
                {
                    for (int deg = 0; deg < 360; deg += 15)
                    {
                        float rad = deg * Mathf.Deg2Rad;
                        Vector3 p = new Vector3(center.x + Mathf.Cos(rad) * r, 0f, center.z + Mathf.Sin(rad) * r);
                        float y = t.SampleHeight(p) + t.transform.position.y;
                        // Mép bờ sông khô ráo (17.1m - 18.6m)
                        if (y >= 17.1f && y <= 18.8f)
                        {
                            Vector3 candShore = new Vector3(p.x, y, p.z);
                            if (FindWaterDirectionFromShore(candShore, out waterBoatPos, out openWaterDir))
                            {
                                shorePos = candShore;
                                return;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Quét 16 hướng từ bờ để tìm hướng ra MẶT SÔNG / BIỂN SÂU THẬT (Terrain < 16.2m suốt 12m -> 75m) để tàu chạy 100% dưới nước!
        /// </summary>
        private bool FindWaterDirectionFromShore(Vector3 shore, out Vector3 boatPos, out Vector3 bestDir)
        {
            boatPos = shore + Vector3.forward * 12f;
            boatPos.y = 16.85f;
            bestDir = Vector3.forward;

            Terrain t = Terrain.activeTerrain;
            if (t == null) return false;

            float bestScore = -1f;
            for (int deg = 0; deg < 360; deg += 22)
            {
                float rad = deg * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));

                int waterSamples = 0;
                bool hitLandEarly = false;
                for (float d = 12f; d <= 72f; d += 8f)
                {
                    Vector3 sample = shore + dir * d;
                    float h = t.SampleHeight(sample) + t.transform.position.y;
                    if (h < 16.2f)
                    {
                        waterSamples++;
                    }
                    else if (d <= 44f)
                    {
                        hitLandEarly = true;
                        break;
                    }
                }

                if (!hitLandEarly && waterSamples >= 5 && waterSamples > bestScore)
                {
                    bestScore = waterSamples;
                    bestDir = dir;
                    boatPos = shore + dir * 13.5f;
                    boatPos.y = 16.85f; // Đúng mặt nước 3D_Water (16.8m)
                }
            }

            return bestScore >= 5;
        }

        // =========================================================================
        // 2. DỰNG PHÒNG THÍ NGHIỆM BÍ MẬT 3D (NƠI GIAM GIỮ NGƯỜI SỐNG SÓT — KHÓ TÌM!)
        // =========================================================================
        private void BuildSecretLabBunker3D()
        {
            secretLabRoot = new GameObject("[SecretLab_C400_Hidden]");
            secretLabRoot.transform.SetParent(transform);
            secretLabRoot.transform.position = secretLabPos;

            Color concreteCol = new Color(0.26f, 0.28f, 0.27f);
            Color darkMetalCol = new Color(0.16f, 0.18f, 0.20f);

            // Sàn bê tông 8m x 8m
            CreateLabBlock("Lab_Floor", new Vector3(0f, 0.1f, 0f), new Vector3(8.4f, 0.25f, 8.4f), concreteCol, true);
            // Trần bê tông
            CreateLabBlock("Lab_Roof", new Vector3(0f, 3.85f, 0f), new Vector3(8.6f, 0.3f, 8.6f), darkMetalCol, true);
            // 3 vách tường kín (trái, phải, sau) + 2 mảng tường trước chừa cửa ra vào rộng 2.8m ở phía -Z
            CreateLabBlock("Lab_Wall_Back", new Vector3(0f, 2.0f, 4.0f), new Vector3(8.4f, 3.8f, 0.35f), concreteCol, true);
            CreateLabBlock("Lab_Wall_Left", new Vector3(-4.0f, 2.0f, 0f), new Vector3(0.35f, 3.8f, 8.0f), concreteCol, true);
            CreateLabBlock("Lab_Wall_Right", new Vector3(4.0f, 2.0f, 0f), new Vector3(0.35f, 3.8f, 8.0f), concreteCol, true);
            CreateLabBlock("Lab_Wall_FrontL", new Vector3(-2.7f, 2.0f, -4.0f), new Vector3(2.8f, 3.8f, 0.35f), concreteCol, true);
            CreateLabBlock("Lab_Wall_FrontR", new Vector3(2.7f, 2.0f, -4.0f), new Vector3(2.8f, 3.8f, 0.35f), concreteCol, true);
            CreateLabBlock("Lab_DoorHeader", new Vector3(0f, 3.35f, -4.0f), new Vector3(2.8f, 1.0f, 0.35f), darkMetalCol, true);

            // Bàn thí nghiệm & 3 bình nuôi cấy Ký Sinh Trùng phát sáng xanh lạnh bên trong
            CreateLabBlock("Lab_Table_L", new Vector3(-2.6f, 0.9f, 2.2f), new Vector3(1.8f, 0.12f, 0.9f), darkMetalCol, true);
            CreateLabBlock("Lab_Table_R", new Vector3(2.6f, 0.9f, 2.2f), new Vector3(1.8f, 0.12f, 0.9f), darkMetalCol, true);

            for (int i = 0; i < 3; i++)
            {
                GameObject tube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tube.name = "Lab_SpecimenTube_" + i;
                tube.transform.SetParent(secretLabRoot.transform, false);
                tube.transform.localPosition = new Vector3(-2.8f + i * 0.55f, 1.55f, 2.2f);
                tube.transform.localScale = new Vector3(0.32f, 0.65f, 0.32f);
                SetObjColor(tube, new Color(0.22f, 0.95f, 0.45f), true);
            }

            // Đèn huỳnh quang xanh mờ bên trong phòng (chỉ thấy khi lại gần ~25m trong rừng tối -> tạo cảm giác khám phá!)
            GameObject labLightObj = new GameObject("Lab_FluorescentLight");
            labLightObj.transform.SetParent(secretLabRoot.transform, false);
            labLightObj.transform.localPosition = new Vector3(0f, 3.3f, 0.5f);
            Light ll = labLightObj.AddComponent<Light>();
            ll.type = LightType.Point;
            ll.color = new Color(0.45f, 0.95f, 0.72f);
            ll.range = 14f;
            ll.intensity = 2.0f;
            ll.shadows = LightShadows.None;

            // Người Sống Sót (C400) ngồi bị thương tựa tường sau trong Phòng Thí Nghiệm
            survivorVisualObj = BuildSurvivorFigure3D(secretLabRoot.transform, new Vector3(0f, 0.25f, 2.6f), 180f);
        }

        private void CreateLabBlock(string name, Vector3 localPos, Vector3 scale, Color col, bool keepCollider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(secretLabRoot.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (!keepCollider)
            {
                Collider c = go.GetComponent<Collider>();
                if (c != null) c.enabled = false;
            }
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
            {
                Material m = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
                m.color = col;
                r.sharedMaterial = m;
            }
        }

        /// <summary>
        /// Tạo nhân vật 3D thật (Ch15_nonPBR + PlayerAnim.controller) thay vì cục Capsule thô sơ!
        /// </summary>
        private GameObject BuildSurvivorFigure3D(Transform parent, Vector3 localPos, float yaw, bool isPlayerHero = false)
        {
            GameObject charPrefab = null;
#if UNITY_EDITOR
            charPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Flooded_Grounds/Character No Animation/Ch15_nonPBR.fbx");
#endif
            // Fallback ở bản Build: clone trực tiếp mesh nhân vật từ Player trong Scene
            if (charPrefab == null && playerTransform != null)
            {
                SkinnedMeshRenderer smr = playerTransform.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr != null && smr.transform.parent != null && smr.transform.parent != playerTransform)
                {
                    charPrefab = smr.transform.parent.gameObject;
                }
            }

            if (charPrefab != null)
            {
                GameObject model = Instantiate(charPrefab);
                model.name = isPlayerHero ? "Ending_PlayerHero_3D" : "Ending_Survivor_3D";
                model.transform.SetParent(parent, false);
                model.transform.localPosition = localPos;
                model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                model.transform.localScale = Vector3.one;
                model.SetActive(true);

                // Tắt toàn bộ collider & script gameplay thừa nếu clone từ Player
                foreach (var c in model.GetComponentsInChildren<Collider>()) c.enabled = false;
                foreach (var r in model.GetComponentsInChildren<Renderer>())
                {
                    r.enabled = true;
                    if (!isPlayerHero && r.material != null)
                    {
                        // Nhuộm sắc áo cam cứu hộ nhẹ để phân biệt Người Sống Sót với Player
                        r.material.color = Color.Lerp(r.material.color, new Color(1.0f, 0.55f, 0.25f), 0.45f);
                    }
                }

                // Gắn Animator Idle tự nhiên (tránh đứng T-Pose)
                Animator anim = model.GetComponent<Animator>();
                if (anim == null) anim = model.AddComponent<Animator>();
                anim.enabled = true;
                anim.applyRootMotion = false;
#if UNITY_EDITOR
                if (anim.runtimeAnimatorController == null)
                {
                    anim.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                        "Assets/Flooded_Grounds/Character No Animation/Animation Character/PlayerAnim.controller");
                }
#endif
                return model;
            }

            // Fallback cuối cùng nếu thiếu FBX
            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = isPlayerHero ? "Ending_PlayerHero_Fallback" : "Ending_Survivor_Fallback";
            fallback.transform.SetParent(parent, false);
            fallback.transform.localPosition = localPos + Vector3.up * 0.9f;
            SetObjColor(fallback, isPlayerHero ? new Color(0.22f, 0.30f, 0.38f) : new Color(0.95f, 0.44f, 0.10f), false);
            return fallback;
        }

        // =========================================================================
        // 3. DỰNG CẦU TÀU VEN SÔNG + TRẠM TÍN HIỆU (ENDING 2) + CON TÀU CỨU HỘ LỚN (ENDING 1)
        // =========================================================================
        private void BuildRiverDockAndRescueBoat3D()
        {
            // A. Cầu tàu gỗ nhô ra mép nước + Trạm Phát Tín Hiệu (tương tác [E] -> Đập nát = Ending 2!)
            dockConsoleObj = new GameObject("Dock_SignalConsole_Ending2");
            dockConsoleObj.transform.SetParent(transform);
            dockConsoleObj.transform.position = dockShorePos;
            dockConsoleObj.transform.rotation = Quaternion.LookRotation(boatSailDir, Vector3.up);

            // Sàn cầu tàu gỗ vươn từ bờ ra gần tàu
            GameObject pierDeck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pierDeck.name = "RiverDock_WoodenPier";
            pierDeck.transform.SetParent(dockConsoleObj.transform, false);
            pierDeck.transform.localPosition = new Vector3(0f, 0.15f, 3.8f);
            pierDeck.transform.localScale = new Vector3(3.8f, 0.3f, 9.0f);
            Renderer pr = pierDeck.GetComponent<Renderer>();
            if (pr != null)
            {
                Material m = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
                m.color = new Color(0.36f, 0.25f, 0.16f);
                pr.sharedMaterial = m;
            }

            // Máy phát tín hiệu cứu hộ đặt bên trái đầu cầu tàu (tương tác [E] để đập nát -> Ending 2)
            GameObject consoleBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            consoleBody.name = "SignalConsole_Machine";
            consoleBody.transform.SetParent(dockConsoleObj.transform, false);
            consoleBody.transform.localPosition = new Vector3(-1.35f, 1.0f, 1.0f);
            consoleBody.transform.localScale = new Vector3(0.85f, 1.4f, 0.65f);
            SetObjColor(consoleBody, new Color(0.20f, 0.24f, 0.28f), false);

            GameObject antenna = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            antenna.transform.SetParent(dockConsoleObj.transform, false);
            antenna.transform.localPosition = new Vector3(-1.35f, 2.35f, 1.0f);
            antenna.transform.localScale = new Vector3(0.08f, 0.9f, 0.08f);
            SetObjColor(antenna, new Color(0.65f, 0.68f, 0.72f), false);

            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.transform.SetParent(dockConsoleObj.transform, false);
            bulb.transform.localPosition = new Vector3(-1.35f, 3.3f, 1.0f);
            bulb.transform.localScale = Vector3.one * 0.34f;
            SetObjColor(bulb, new Color(1.0f, 0.25f, 0.20f), true);

            dockConsoleLight = bulb.AddComponent<Light>();
            dockConsoleLight.type = LightType.Point;
            dockConsoleLight.color = new Color(1.0f, 0.25f, 0.20f);
            dockConsoleLight.range = 18f;
            dockConsoleLight.intensity = 2.0f;
            dockConsoleLight.shadows = LightShadows.None;

            // B. CON TÀU CỨU HỘ LỚN (~15 MÉT DÀI) NỔI TRÊN MẶT NƯỚC THẬT (ẩn cho tới khi Player cõng người ra tới gần hoặc luôn neo sẵn chờ sẵn)
            SpawnLargeRescueBoat3D(boatWaterPos, boatSailDir);
            rescueBoatObj.SetActive(false); // Chỉ cập bến khi bạn đã cõng Người Sống Sót chạy ra Bến Tàu!
        }

        private void SpawnLargeRescueBoat3D(Vector3 waterPos, Vector3 forwardDir)
        {
            if (rescueBoatObj != null) Destroy(rescueBoatObj);

            rescueBoatObj = new GameObject("Ending_LargeRescueBoat_3D");
            rescueBoatObj.transform.SetParent(transform);
            rescueBoatObj.transform.position = waterPos;
            rescueBoatObj.transform.rotation = Quaternion.LookRotation(forwardDir, Vector3.up);

            // Thân tàu lớn (dài 15m, rộng 4.8m, cao 1.8m)
            GameObject hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.transform.SetParent(rescueBoatObj.transform, false);
            hull.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            hull.transform.localScale = new Vector3(4.8f, 1.8f, 14.5f);
            SetObjColor(hull, new Color(0.86f, 0.89f, 0.92f), false);

            // Mũi tàu vát nhọn phía trước
            GameObject bow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bow.transform.SetParent(rescueBoatObj.transform, false);
            bow.transform.localPosition = new Vector3(0f, 0.9f, 7.2f);
            bow.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            bow.transform.localScale = new Vector3(3.4f, 1.8f, 3.4f);
            SetObjColor(bow, new Color(0.86f, 0.89f, 0.92f), false);

            // Sọc đỏ cứu hộ lớn dọc thân tàu
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.transform.SetParent(rescueBoatObj.transform, false);
            stripe.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            stripe.transform.localScale = new Vector3(5.0f, 0.42f, 14.7f);
            SetObjColor(stripe, new Color(0.90f, 0.18f, 0.12f), false);

            // Cabin chỉ huy 2 tầng cao ráo
            GameObject cabin1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin1.transform.SetParent(rescueBoatObj.transform, false);
            cabin1.transform.localPosition = new Vector3(0f, 2.75f, 1.8f);
            cabin1.transform.localScale = new Vector3(3.6f, 2.3f, 5.2f);
            SetObjColor(cabin1, new Color(0.94f, 0.95f, 0.97f), false);

            // Kính buồng lái tối màu
            GameObject windshield = GameObject.CreatePrimitive(PrimitiveType.Cube);
            windshield.transform.SetParent(rescueBoatObj.transform, false);
            windshield.transform.localPosition = new Vector3(0f, 3.1f, 4.45f);
            windshield.transform.localScale = new Vector3(3.1f, 0.9f, 0.18f);
            SetObjColor(windshield, new Color(0.10f, 0.18f, 0.25f), false);

            // Cột radar & đèn pha cứu hộ rực sáng trên nóc cabin
            GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.transform.SetParent(rescueBoatObj.transform, false);
            mast.transform.localPosition = new Vector3(0f, 4.8f, 1.6f);
            mast.transform.localScale = new Vector3(0.18f, 1.2f, 0.18f);
            SetObjColor(mast, new Color(0.7f, 0.72f, 0.75f), false);

            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.transform.SetParent(rescueBoatObj.transform, false);
            beacon.transform.localPosition = new Vector3(0f, 6.1f, 1.6f);
            beacon.transform.localScale = Vector3.one * 0.55f;
            SetObjColor(beacon, new Color(1.0f, 0.88f, 0.35f), true);

            Light boatLight = beacon.AddComponent<Light>();
            boatLight.type = LightType.Point;
            boatLight.color = new Color(1.0f, 0.84f, 0.42f);
            boatLight.range = 38f;
            boatLight.intensity = 3.5f;
            boatLight.shadows = LightShadows.None;
        }

        // =========================================================================
        // 4. RƠI ỐNG HUYẾT THANH 3D TRÊN MẶT ĐẤT KHI BOSS CHẾT + CÕNG NGƯỜI SỐNG SÓT
        // =========================================================================
        private void SpawnDroppedParasiteCure3D(Vector3 pos)
        {
            if (droppedCureObj != null) Destroy(droppedCureObj);

            if (Terrain.activeTerrain != null)
            {
                pos.y = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y + 0.55f;
            }
            droppedCurePos = pos;

            droppedCureObj = new GameObject("Dropped_ParasiteCure_Vial");
            droppedCureObj.transform.SetParent(transform);
            droppedCureObj.transform.position = droppedCurePos;

            // Thân ống thủy tinh phát sáng xanh ngọc rực rỡ
            GameObject vial = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            vial.transform.SetParent(droppedCureObj.transform, false);
            vial.transform.localRotation = Quaternion.Euler(0f, 0f, 22f);
            vial.transform.localScale = new Vector3(0.24f, 0.42f, 0.24f);
            SetObjColor(vial, new Color(0.15f, 1.0f, 0.65f), true);

            // 2 nắp kim loại trên dưới
            GameObject capTop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            capTop.transform.SetParent(vial.transform, false);
            capTop.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            capTop.transform.localScale = new Vector3(1.15f, 0.22f, 1.15f);
            SetObjColor(capTop, new Color(0.85f, 0.88f, 0.90f), false);

            Light glow = droppedCureObj.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(0.20f, 1.0f, 0.70f);
            glow.range = 12f;
            glow.intensity = 3.2f;
            glow.shadows = LightShadows.None;

            // Tia sáng xanh thẳng đứng nhẹ cao 8m để dễ nhìn thấy ống thuốc rơi trên cỏ!
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.transform.SetParent(droppedCureObj.transform, false);
            beam.transform.localPosition = new Vector3(0f, 4.0f, 0f);
            beam.transform.localScale = new Vector3(0.12f, 4.0f, 0.12f);
            SetObjColor(beam, new Color(0.20f, 1.0f, 0.70f, 0.35f), true);
        }

        private void StartCarryingSurvivor()
        {
            Stage = EndingStoryStage.CarryingSurvivorToDock;
            if (survivorVisualObj != null) survivorVisualObj.SetActive(false);

            // Hiện con tàu cứu hộ lớn cập bến dưới sông!
            if (rescueBoatObj != null) rescueBoatObj.SetActive(true);

            // Gắn hình cánh tay áo phao cam của Người Sống Sót vắt qua vai trái Player để cảm giác đang CÕNG thật sự!
            if (playerController != null && playerController.playerCamera != null && carriedVisualOnPlayer == null)
            {
                carriedVisualOnPlayer = new GameObject("Carried_Survivor_ShoulderArm");
                carriedVisualOnPlayer.transform.SetParent(playerController.playerCamera, false);
                carriedVisualOnPlayer.transform.localPosition = new Vector3(-0.38f, -0.26f, 0.38f);
                carriedVisualOnPlayer.transform.localRotation = Quaternion.Euler(22f, 28f, -35f);

                GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                arm.transform.SetParent(carriedVisualOnPlayer.transform, false);
                arm.transform.localScale = new Vector3(0.11f, 0.32f, 0.11f);
                SetObjColor(arm, new Color(0.95f, 0.44f, 0.10f), false);

                GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hand.transform.SetParent(carriedVisualOnPlayer.transform, false);
                hand.transform.localPosition = new Vector3(0f, 0.34f, 0f);
                hand.transform.localScale = Vector3.one * 0.11f;
                SetObjColor(hand, new Color(0.86f, 0.70f, 0.56f), false);
            }

            // Tăng nhẹ sức nặng khi cõng người
            if (playerController != null)
            {
                playerController.caveSpeedModifier = 0.85f;
            }

            ShowStoryBanner("🧑 ĐÃ TIÊM HUYẾT THANH & CÕNG NGƯỜI SỐNG SÓT!", "Mau cõng anh ta chạy ra Bến Tàu ven sông — Con tàu cứu hộ lớn đã cập bến!");
        }

        private void StopCarryingVisual()
        {
            if (carriedVisualOnPlayer != null)
            {
                Destroy(carriedVisualOnPlayer);
                carriedVisualOnPlayer = null;
            }
            if (playerController != null)
            {
                playerController.caveSpeedModifier = 1.0f;
            }
        }

        // =========================================================================
        // 5. UPDATE LOOP & HÀNH ĐỘNG VẬT LÝ QUYẾT ĐỊNH ENDING
        // =========================================================================
        public bool IsSurvivalDayMet()
        {
            int day = (DayNightCycle.Instance != null) ? DayNightCycle.Instance.currentDay : 1;
            return day >= requiredSurvivalDay;
        }

        private void Update()
        {
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;
            if (HorrorGame.Cutscenes.CinematicTrailerDirector.IsTrailerRunning) return;
            if (PlayerDeathUI.IsShowingDeathScreen) return;

            FindPlayerRefs();
            if (storyBannerTimer > 0f) storyBannerTimer -= Time.unscaledDeltaTime;

            // Xoay nhẹ ống Huyết Thanh rơi trên mặt đất cho bắt mắt
            if (droppedCureObj != null && Stage == EndingStoryStage.PickupDroppedCure)
            {
                droppedCureObj.transform.Rotate(Vector3.up, 75f * Time.deltaTime, Space.World);
                Vector3 p = droppedCurePos;
                p.y += Mathf.Sin(Time.time * 3.0f) * 0.16f;
                droppedCureObj.transform.position = p;
            }

            // Con tàu lớn nhấp nhô nhẹ trên mặt sông khi đang neo chờ Player cõng người tới
            if (rescueBoatObj != null && rescueBoatObj.activeSelf && Stage == EndingStoryStage.CarryingSurvivorToDock)
            {
                Vector3 bp = boatWaterPos;
                bp.y = 16.85f + Mathf.Sin(Time.time * 1.8f) * 0.14f;
                rescueBoatObj.transform.position = bp;
            }

            // Khi đang chạy Cutscene Ending 1 hoặc Ending 2
            if (Stage == EndingStoryStage.Ending1_Escape || Stage == EndingStoryStage.Ending2_StayForever)
            {
                // LUÔN đảm bảo Player bất tử trong lúc Ending (fix dứt điểm lỗi hiện "BẠN ĐÃ CHẾT" đè lên Ending!)
                if (playerStats != null) playerStats.isInvincible = true;

                endingTimer += Time.unscaledDeltaTime;
                UpdateEndingCutscene3D(Stage == EndingStoryStage.Ending2_StayForever);

                if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
                if (!Cursor.visible) Cursor.visible = true;
                return;
            }

            if (Stage == EndingStoryStage.FreeRoamAfterEnding2)
            {
                activePrompt = "";
                return;
            }

            if (Stage == EndingStoryStage.SurviveAndPrepare && IsSurvivalDayMet())
            {
                Stage = EndingStoryStage.SearchSecretLab;
                ShowStoryBanner("📻 TÍN HIỆU MỜ TỪ PHÒNG THÍ NGHIỆM BÍ MẬT!", "Hãy tự lần theo rừng sâu phía Bắc/Tây Bắc để tìm Phòng Thí Nghiệm giam giữ Người Sống Sót!");
            }

            if (Stage == EndingStoryStage.BossFightActive && activeBoss == null)
            {
                OnParasiteQueenDefeated();
            }

            if (dockConsoleLight != null)
            {
                dockConsoleLight.color = (Stage == EndingStoryStage.CarryingSurvivorToDock)
                    ? new Color(0.25f, 1.0f, 0.60f)
                    : new Color(0.95f, 0.25f, 0.20f);
            }

            UpdatePlayerInteraction();
        }

        private void UpdatePlayerInteraction()
        {
            activePrompt = "";
            if (playerTransform == null || (playerStats != null && playerStats.isDead)) return;

            // 1. Tương tác với Người Sống Sót trong Phòng Thí Nghiệm Bí Mật
            float distLab = Vector3.Distance(playerTransform.position, secretLabPos + new Vector3(0f, 0.5f, 2.0f));
            if (distLab <= 4.8f)
            {
                if (Stage == EndingStoryStage.SurviveAndPrepare || Stage == EndingStoryStage.SearchSecretLab)
                {
                    if (!IsSurvivalDayMet())
                    {
                        int curDay = DayNightCycle.Instance != null ? DayNightCycle.Instance.currentDay : 1;
                        activePrompt = string.Format("🧪 NGƯỜI SỐNG SÓT TRONG PHÒNG THÍ NGHIỆM: \"Con quái vật đang rình ngoài kia!\"\n[KHÓA] Cần sống sót tới Ngày {0} (Hiện tại: Ngày {1}) & chuẩn bị súng đạn.", requiredSurvivalDay, curDay);
                    }
                    else
                    {
                        activePrompt = "🧑 NGƯỜI SỐNG SÓT (C400) đang kẹt trong Phòng Thí Nghiệm!\n👉 Nhấn [E] tiếp cận giải cứu (CẢNH BÁO: Nữ Hoàng Ký Sinh sẽ xông tới!)";
                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            HasFoundSurvivor = true;
                            TriggerParasiteQueenBossFight();
                        }
                    }
                    return;
                }
                else if (Stage == EndingStoryStage.ReturnToLabAndCarry && HasParasiteCure)
                {
                    activePrompt = "🧪 ĐÃ CÓ HUYẾT THANH GIẢI ĐỘC!\n👉 Nhấn [E] tiêm Huyết Thanh & CÕNG Người Sống Sót chạy ra Bến Tàu ven sông!";
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        StartCarryingSurvivor();
                    }
                    return;
                }
            }

            // 2. Nhặt Ống Huyết Thanh 3D rơi trên mặt đất sau khi giết Boss
            if (Stage == EndingStoryStage.PickupDroppedCure && droppedCureObj != null)
            {
                float distCure = Vector3.Distance(playerTransform.position, droppedCurePos);
                if (distCure <= 4.0f)
                {
                    activePrompt = "🧪 ỐNG HUYẾT THANH GIẢI ĐỘC DUY NHẤT (Rơi từ Nữ Hoàng Ký Sinh)\n👉 Nhấn [E] để nhặt lên!";
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        HasParasiteCure = true;
                        Destroy(droppedCureObj);
                        droppedCureObj = null;
                        Stage = EndingStoryStage.ReturnToLabAndCarry;
                        ShowStoryBanner("🧪 ĐÃ NHẶT HUYẾT THANH GIẢI ĐỘC!", "Quay lại Phòng Thí Nghiệm bấm [E] để tiêm & cõng Người Sống Sót chạy ra Bến Tàu!");
                    }
                    return;
                }
            }

            // 3. TẠI BẾN TÀU VEN SÔNG — HÀNH ĐỘNG VẬT LÝ QUYẾT ĐỊNH ENDING (KHÔNG HIỆN MENU CHỌN!)
            if (Stage == EndingStoryStage.CarryingSurvivorToDock)
            {
                Vector3 boatBoardPoint = dockShorePos + boatSailDir * 7.5f;
                Vector3 consolePoint = dockConsoleObj != null ? dockConsoleObj.transform.TransformPoint(new Vector3(-1.35f, 1.0f, 1.0f)) : dockShorePos;

                float distToBoatEdge = Vector3.Distance(playerTransform.position, boatBoardPoint);
                float distToConsole = Vector3.Distance(playerTransform.position, consolePoint);

                // Hành động A: Lại gần đầu cầu tàu hướng ra Con Tàu Cứu Hộ -> Lên tàu rời đảo (ENDING 1)
                if (distToBoatEdge <= 5.5f)
                {
                    activePrompt = "🚤 CON TÀU CỨU HỘ ĐANG CHỜ DƯỚI SÔNG!\n👉 Nhấn [E] LÊN TÀU cùng Người Sống Sót thoát khỏi hòn đảo ngay (Ending 1)";
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        TriggerEnding1_Escape();
                    }
                    return;
                }

                // Hành động B: Lại gần Trạm Phát Tín Hiệu trên cầu tàu -> Đưa anh ta lên tàu & đập nát trạm, ở lại mãi mãi (ENDING 2)
                if (distToConsole <= 3.8f)
                {
                    activePrompt = "📻 TRẠM PHÁT TÍN HIỆU BẾN TÀU (Máu bạn đã nhiễm mầm bệnh...)\n👉 Nhấn [E] ĐƯA ANH TA LÊN TÀU & ĐẬP NÁT TRẠM — Ở lại đảo mãi mãi (Ending 2)";
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        TriggerEnding2_StayForever();
                    }
                    return;
                }
            }
            else
            {
                // Nếu chưa cõng người mà chạy ra Bến Tàu -> báo khóa điều kiện
                float distDock = Vector3.Distance(playerTransform.position, dockShorePos);
                if (distDock <= 5.5f)
                {
                    string c1 = IsSurvivalDayMet() ? "[✓]" : "[✗]";
                    string c2 = HasFoundSurvivor ? "[✓]" : "[✗]";
                    string c3 = HasDefeatedBoss && HasParasiteCure ? "[✓]" : "[✗]";
                    activePrompt = string.Format(
                        "🔒 BẾN TÀU HOANG VẮNG — CHƯA CÓ TÀU CỨU HỘ:\n{0} Sống sót tới Ngày {1}  |  {2} Tìm Phòng Thí Nghiệm Bí Mật  |  {3} Hạ Boss, nhặt Huyết Thanh & cõng Người Sống Sót ra đây",
                        c1, requiredSurvivalDay, c2, c3);
                }
            }
        }

        private void TriggerParasiteQueenBossFight()
        {
            Stage = EndingStoryStage.BossFightActive;
            ShowStoryBanner("⚠️ NỮ HOÀNG KÝ SINH ĐÃ PHÁ VÂY!", "Hạ gục Trùm Cuối ngay bên ngoài Phòng Thí Nghiệm để nó rơi ra Ống Huyết Thanh Giải Độc!");

            GameObject baseModel = null;
            ZombieSpawner zs = FindObjectOfType<ZombieSpawner>();
            if (zs != null && zs.zombiePrefab != null) baseModel = zs.zombiePrefab;
            if (baseModel == null)
            {
                EnemyAI anyEnemy = FindObjectOfType<EnemyAI>();
                if (anyEnemy != null) baseModel = anyEnemy.gameObject;
            }

            Vector3 spawnPos = secretLabPos + new Vector3(0f, 0f, -15f);
            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPos, out hit, 25f, NavMesh.AllAreas))
                spawnPos = hit.position;
            else if (Terrain.activeTerrain != null)
                spawnPos.y = Terrain.activeTerrain.SampleHeight(spawnPos) + Terrain.activeTerrain.transform.position.y;

            GameObject bossObj;
            if (baseModel != null)
            {
                bossObj = Instantiate(baseModel, spawnPos, Quaternion.identity);
                bossObj.SetActive(true);
                EnemyAI oldAI = bossObj.GetComponent<EnemyAI>();
                if (oldAI != null) Destroy(oldAI);
            }
            else
            {
                bossObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                bossObj.transform.position = spawnPos;
                bossObj.AddComponent<NavMeshAgent>();
                bossObj.AddComponent<Animator>();
            }

            bossObj.name = "[BOSS] ParasiteQueen";
            bossObj.transform.localScale = Vector3.one * 3.2f;

            Animator anim = bossObj.GetComponent<Animator>();
            if (anim != null) { anim.enabled = true; anim.applyRootMotion = false; }

            activeBoss = bossObj.AddComponent<ParasiteQueenBoss>();
            activeBoss.player = playerTransform;
            activeBoss.bossTitle = "☠ NỮ HOÀNG KÝ SINH (PARASITE QUEEN) — TRÙM CUỐI ☠";
        }

        private void OnParasiteQueenDefeated()
        {
            if (HasDefeatedBoss) return;
            HasDefeatedBoss = true;
            Stage = EndingStoryStage.PickupDroppedCure;

            Vector3 dropPos = (activeBoss != null) ? activeBoss.transform.position : (secretLabPos + new Vector3(0f, 0f, -8f));
            SpawnDroppedParasiteCure3D(dropPos);

            ShowStoryBanner("🏆 NỮ HOÀNG KÝ SINH ĐÃ GỤC NGÃ!", "Ống Huyết Thanh phát sáng xanh vừa rơi xuống mặt đất — hãy lại gần nhấn [E] để nhặt!");
        }

        // =========================================================================
        // 6. KÍCH HOẠT CUTSCENE 3D CHO 2 ENDING (BẤT TỬ HOÀN TOÀN + TÀU CHẠY TRÊN SÔNG)
        // =========================================================================
        private void PrepareForEndingCutscene()
        {
            endingTimer = 0f;
            finalSurvivedDays = (DayNightCycle.Instance != null) ? Mathf.Max(1, DayNightCycle.Instance.currentDay) : 1;

            // BẤT TỬ TUYỆT ĐỐI & HỒI ĐẦY MÁU để không bao giờ hiện "BẠN ĐÃ CHẾT" khi đang Ending!
            if (playerStats != null)
            {
                playerStats.isInvincible = true;
                playerStats.currentHealth = playerStats.maxHealth;
            }
            if (playerController != null) playerController.enabled = false;

            StopCarryingVisual();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Tạm tắt AI quái xung quanh để không nhảy vào khung hình hoặc cắn Player trong Cutscene
            EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null && !enemies[i].IsDead)
                {
                    enemies[i].enabled = false;
                }
            }
        }

        private void TriggerEnding1_Escape()
        {
            PrepareForEndingCutscene();
            Stage = EndingStoryStage.Ending1_Escape;
            PlayerPrefs.SetInt("DeadIsland_Ending1_Unlocked", 1);
            PlayerPrefs.Save();
            StartEndingCutscene3D(false);
        }

        private void TriggerEnding2_StayForever()
        {
            PrepareForEndingCutscene();
            Stage = EndingStoryStage.Ending2_StayForever;
            PlayerPrefs.SetInt("DeadIsland_Ending2_Unlocked", 1);
            PlayerPrefs.Save();

            if (dockConsoleObj != null)
            {
                SpawnBeaconDestructionBurst(dockConsoleObj.transform.TransformPoint(new Vector3(-1.35f, 1.5f, 1.0f)));
                Transform machine = dockConsoleObj.transform.Find("SignalConsole_Machine");
                if (machine != null) machine.gameObject.SetActive(false);
            }
            StartEndingCutscene3D(true);
        }

        private void StartEndingCutscene3D(bool isEnding2)
        {
            if (DayNightCycle.Instance != null) DayNightCycle.Instance.currentHour = 6.15f;

            boatStartPos = boatWaterPos;
            if (rescueBoatObj == null) SpawnLargeRescueBoat3D(boatStartPos, boatSailDir);
            rescueBoatObj.SetActive(true);
            rescueBoatObj.transform.position = boatStartPos;
            rescueBoatObj.transform.rotation = Quaternion.LookRotation(boatSailDir, Vector3.up);

            // Đặt Người Sống Sót 3D thật (Ch15_nonPBR) đứng trên boong sau con tàu lớn
            BuildSurvivorFigure3D(rescueBoatObj.transform, new Vector3(0.95f, 1.66f, -3.5f), 180f, false);
            if (!isEnding2)
            {
                // Ending 1: Nhân vật Player 3D thật (Ch15_nonPBR) đứng cạnh Người Sống Sót trên boong tàu nhìn lại đảo!
                BuildSurvivorFigure3D(rescueBoatObj.transform, new Vector3(-0.95f, 1.66f, -3.5f), 180f, true);
            }
            else if (dockConsoleObj != null)
            {
                // Ending 2: Nhân vật Player 3D thật đứng lại trên mũi cầu tàu tiễn con tàu rời bến!
                BuildSurvivorFigure3D(dockConsoleObj.transform, new Vector3(0f, 0.32f, 6.2f), 0f, true);
            }

            prevMainCam = Camera.main;
            if (prevMainCam == null && playerController != null && playerController.playerCamera != null)
                prevMainCam = playerController.playerCamera.GetComponent<Camera>();
            if (prevMainCam != null) prevMainCam.enabled = false;

            GameObject camObj = new GameObject("EndingCutscene_Camera");
            camObj.transform.SetParent(transform);
            endingCam = camObj.AddComponent<Camera>();
            endingCam.fieldOfView = 52f;
            endingCam.nearClipPlane = 0.2f;
            endingCam.farClipPlane = 1200f;
        }

        private void UpdateEndingCutscene3D(bool isEnding2)
        {
            float t = endingTimer;

            // Tàu lớn rẽ sóng chạy thẳng dọc theo MẶT SÔNG/BIỂN THẬT (boatSailDir đã quét toàn nước)
            if (rescueBoatObj != null)
            {
                Vector3 pos = boatStartPos + boatSailDir * (t * 7.5f);
                pos.y = 16.85f + Mathf.Sin(t * 2.1f) * 0.18f;
                rescueBoatObj.transform.position = pos;
                rescueBoatObj.transform.rotation = Quaternion.LookRotation(boatSailDir, Vector3.up) * Quaternion.Euler(Mathf.Sin(t * 1.6f) * 1.8f, 0f, Mathf.Cos(t * 1.9f) * 2.0f);
            }

            if (endingCam != null && rescueBoatObj != null)
            {
                if (!isEnding2)
                {
                    Vector3 camPos = rescueBoatObj.transform.position - boatSailDir * 14.5f + Vector3.up * (5.2f + t * 0.45f) + rescueBoatObj.transform.right * 4.0f;
                    endingCam.transform.position = camPos;
                    endingCam.transform.LookAt(rescueBoatObj.transform.position + Vector3.up * 2.2f);
                }
                else
                {
                    Vector3 shoreCam = dockShorePos - boatSailDir * (3.5f + t * 1.0f) + Vector3.up * (3.0f + t * 0.65f);
                    endingCam.transform.position = shoreCam;
                    endingCam.transform.LookAt(rescueBoatObj.transform.position + Vector3.up * 2.0f);
                }
            }
        }

        private void ContinueFreeRoamAfterEnding2()
        {
            Stage = EndingStoryStage.FreeRoamAfterEnding2;

            if (endingCam != null) { Destroy(endingCam.gameObject); endingCam = null; }
            if (rescueBoatObj != null) { Destroy(rescueBoatObj); rescueBoatObj = null; }
            if (dockConsoleObj != null)
            {
                Transform heroClone = dockConsoleObj.transform.Find("Ending_PlayerHero_3D");
                if (heroClone != null) Destroy(heroClone.gameObject);
            }
            if (prevMainCam != null) prevMainCam.enabled = true;

            if (playerStats != null) playerStats.isInvincible = false;
            if (playerController != null) playerController.enabled = true;

            EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();
            for (int i = 0; i < enemies.Length; i++)
                if (enemies[i] != null && !enemies[i].IsDead) enemies[i].enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            ShowStoryBanner("🌲 CHẾ ĐỘ SINH TỒN TỰ DO ĐÃ MỞ KHÓA", "Bạn là Người Gác Đảo Vĩnh Cửu. Chúc bạn sinh tồn mãi mãi trên hòn đảo này!");
        }

        private void SpawnBeaconDestructionBurst(Vector3 pos)
        {
            GameObject burstObj = new GameObject("Beacon_ExplosionSparks");
            burstObj.transform.position = pos;
            ParticleSystem ps = burstObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.startLifetime = 1.8f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
            main.startColor = new Color(1.0f, 0.55f, 0.15f, 0.95f);
            main.gravityModifier = 0.8f;
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 45) });
            Destroy(burstObj, 4f);
        }

        private void SetObjColor(GameObject go, Color c, bool unlit)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            Renderer r = go.GetComponent<Renderer>();
            if (r == null) return;
            Shader sh = unlit ? (Shader.Find("Unlit/Color") ?? Shader.Find("Standard")) : (Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            Material m = new Material(sh);
            m.color = c;
            r.sharedMaterial = m;
        }

        public void ShowStoryBanner(string title, string sub)
        {
            storyBannerTitle = title;
            storyBannerSub = sub;
            storyBannerTimer = 6.0f;
        }

        // =========================================================================
        // 7. GIAO DIỆN HUD & MÀN HÌNH ENDING
        // =========================================================================
        private void InitStyles()
        {
            if (stylesReady) return;
            stylesReady = true;

            blackTex = MakeTex(Color.black);
            panelTex = MakeTex(new Color(0.05f, 0.06f, 0.09f, 0.90f));
            goldLineTex = MakeTex(new Color(0.95f, 0.74f, 0.25f, 0.92f));
            redLineTex = MakeTex(new Color(0.85f, 0.22f, 0.22f, 0.92f));
            btnNormalTex = MakeTex(new Color(0.12f, 0.22f, 0.30f, 0.92f));
            btnHoverTex = MakeTex(new Color(0.18f, 0.38f, 0.52f, 0.98f));
            btnAltNormalTex = MakeTex(new Color(0.28f, 0.11f, 0.11f, 0.92f));
            btnAltHoverTex = MakeTex(new Color(0.48f, 0.16f, 0.14f, 0.98f));

            promptStyle = new GUIStyle(GUI.skin.box);
            promptStyle.fontSize = 15;
            promptStyle.fontStyle = FontStyle.Bold;
            promptStyle.wordWrap = true;
            promptStyle.alignment = TextAnchor.MiddleCenter;
            promptStyle.normal.background = panelTex;
            promptStyle.normal.textColor = new Color(0.96f, 0.95f, 0.88f);
            promptStyle.padding = new RectOffset(14, 14, 10, 10);

            bannerTitleStyle = new GUIStyle(GUI.skin.label);
            bannerTitleStyle.fontSize = 20;
            bannerTitleStyle.fontStyle = FontStyle.Bold;
            bannerTitleStyle.alignment = TextAnchor.MiddleCenter;
            bannerTitleStyle.normal.textColor = new Color(1.0f, 0.82f, 0.32f);

            bannerSubStyle = new GUIStyle(GUI.skin.label);
            bannerSubStyle.fontSize = 13;
            bannerSubStyle.alignment = TextAnchor.MiddleCenter;
            bannerSubStyle.wordWrap = true;
            bannerSubStyle.normal.textColor = Color.white;

            carryHudStyle = new GUIStyle(promptStyle);
            carryHudStyle.fontSize = 14;
            carryHudStyle.normal.textColor = new Color(1.0f, 0.75f, 0.30f);

            choiceBtnStyle = new GUIStyle(GUI.skin.button);
            choiceBtnStyle.fontSize = 16;
            choiceBtnStyle.fontStyle = FontStyle.Bold;
            choiceBtnStyle.wordWrap = true;
            choiceBtnStyle.normal.background = btnNormalTex;
            choiceBtnStyle.hover.background = btnHoverTex;
            choiceBtnStyle.normal.textColor = Color.white;
            choiceBtnStyle.hover.textColor = Color.white;

            choiceAltBtnStyle = new GUIStyle(choiceBtnStyle);
            choiceAltBtnStyle.normal.background = btnAltNormalTex;
            choiceAltBtnStyle.hover.background = btnAltHoverTex;

            endingBigTitleStyle = new GUIStyle(GUI.skin.label);
            endingBigTitleStyle.fontSize = 48;
            endingBigTitleStyle.fontStyle = FontStyle.Bold;
            endingBigTitleStyle.alignment = TextAnchor.MiddleCenter;
            endingBigTitleStyle.normal.textColor = new Color(1.0f, 0.84f, 0.36f);

            endingSubStyle = new GUIStyle(GUI.skin.label);
            endingSubStyle.fontSize = 18;
            endingSubStyle.fontStyle = FontStyle.Italic;
            endingSubStyle.alignment = TextAnchor.MiddleCenter;
            endingSubStyle.normal.textColor = new Color(0.75f, 0.80f, 0.85f);

            endingBodyStyle = new GUIStyle(GUI.skin.label);
            endingBodyStyle.fontSize = 17;
            endingBodyStyle.wordWrap = true;
            endingBodyStyle.alignment = TextAnchor.MiddleCenter;
            endingBodyStyle.normal.textColor = new Color(0.92f, 0.93f, 0.95f);

            endingStatsStyle = new GUIStyle(GUI.skin.label);
            endingStatsStyle.fontSize = 16;
            endingStatsStyle.fontStyle = FontStyle.Bold;
            endingStatsStyle.alignment = TextAnchor.MiddleCenter;
            endingStatsStyle.normal.textColor = new Color(0.48f, 0.95f, 0.65f);
        }

        private Texture2D MakeTex(Color c)
        {
            Texture2D t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        private void OnGUI()
        {
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;
            if (HorrorGame.Cutscenes.CinematicTrailerDirector.IsTrailerRunning) return;
            if (PlayerDeathUI.IsShowingDeathScreen) return;
            if (GameMenuManager.IsPaused && !IsEndingUIActive) return;

            InitStyles();
            float sw = Screen.width;
            float sh = Screen.height;

            // 1. Màn hình Cutscene 3D & Kết Thúc Game (Ending 1 hoặc Ending 2)
            if (Stage == EndingStoryStage.Ending1_Escape || Stage == EndingStoryStage.Ending2_StayForever)
            {
                DrawEndingScreen(sw, sh, Stage == EndingStoryStage.Ending2_StayForever);
                return;
            }

            // 2. Banner cốt truyện
            if (storyBannerTimer > 0f)
            {
                float alpha = Mathf.Clamp01(storyBannerTimer < 1f ? storyBannerTimer : 1f);
                Color prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                float bw = 600f, bh = 74f;
                float bx = (sw - bw) * 0.5f, by = 78f;
                GUI.Box(new Rect(bx, by, bw, bh), "", promptStyle);
                GUI.DrawTexture(new Rect(bx, by, bw, 3f), goldLineTex);
                GUI.Label(new Rect(bx + 10, by + 8, bw - 20, 28), storyBannerTitle, bannerTitleStyle);
                GUI.Label(new Rect(bx + 10, by + 38, bw - 20, 30), storyBannerSub, bannerSubStyle);
                GUI.color = prev;
            }

            // 3. Trạng thái đang CÕNG Người Sống Sót
            if (Stage == EndingStoryStage.CarryingSurvivorToDock)
            {
                GUI.Box(new Rect((sw - 420f) * 0.5f, 158f, 420f, 36f), "🧑 ĐANG CÕNG NGƯỜI SỐNG SÓT — CHẠY RA BẾN TÀU VEN SÔNG!", carryHudStyle);
                DrawDockWaypoint(sw, sh);
            }
            else if (Stage == EndingStoryStage.PickupDroppedCure && droppedCureObj != null)
            {
                DrawPointBadge(sw, sh, droppedCurePos + Vector3.up * 1.2f, "🧪 ỐNG HUYẾT THANH GIẢI ĐỘC", new Color(0.20f, 1.0f, 0.70f));
            }
            else if (Stage == EndingStoryStage.ReturnToLabAndCarry)
            {
                DrawPointBadge(sw, sh, secretLabPos + Vector3.up * 2.5f, "🧑 QUAY LẠI PHÒNG THÍ NGHIỆM CÕNG NGƯỜI", new Color(1.0f, 0.78f, 0.30f));
            }

            // 4. Prompt tương tác [E]
            if (!string.IsNullOrEmpty(activePrompt))
            {
                float pw = Mathf.Min(680f, sw - 40f);
                float ph = 66f;
                GUI.Box(new Rect((sw - pw) * 0.5f, sh * 0.72f, pw, ph), activePrompt, promptStyle);
            }
        }

        private void DrawDockWaypoint(float sw, float sh)
        {
            DrawPointBadge(sw, sh, dockShorePos + Vector3.up * 3.0f, "🚤 BẾN TÀU CỨU HỘ VEN SÔNG", new Color(0.30f, 1.0f, 0.65f));
        }

        private void DrawPointBadge(float sw, float sh, Vector3 worldPos, string label, Color col)
        {
            if (playerTransform == null) return;
            Camera cam = Camera.main;
            if (cam == null && playerController != null && playerController.playerCamera != null)
                cam = playerController.playerCamera.GetComponent<Camera>();
            if (cam == null) cam = FindObjectOfType<Camera>();
            if (cam == null) return;

            float dist = Vector3.Distance(playerTransform.position, worldPos);
            Vector3 sp = cam.WorldToScreenPoint(worldPos);
            bool behind = sp.z < 0f;
            if (behind) { sp.x = sw - sp.x; sp.y = sh - sp.y; }

            float x = Mathf.Clamp(sp.x, 145f, sw - 145f);
            float y = Mathf.Clamp(sh - sp.y, 115f, sh - 135f);
            if (behind) y = sh - 135f;

            GUIStyle st = new GUIStyle(promptStyle);
            st.fontSize = 13;
            st.normal.textColor = col;
            st.padding = new RectOffset(8, 8, 4, 4);
            GUI.Box(new Rect(x - 145f, y - 16f, 290f, 32f), string.Format("{0}  [{1:0}m]", label, dist), st);
        }

        private void DrawEndingScreen(float sw, float sh, bool isEnding2)
        {
            Color prev = GUI.color;

            if (endingTimer < CUTSCENE_DURATION)
            {
                float barH = sh * 0.12f;
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(0f, 0f, sw, barH), blackTex);
                GUI.DrawTexture(new Rect(0f, sh - barH, sw, barH), blackTex);

                string subLine = !isEnding2
                    ? ((endingTimer < 4.0f)
                        ? "Bạn cùng Người Sống Sót bước lên con tàu cứu hộ, rẽ sóng rời khỏi hòn đảo chết chóc..."
                        : "Nhưng sâu trong tĩnh mạch, một nhịp đập lạnh buốt khẽ rung lên...")
                    : ((endingTimer < 4.0f)
                        ? "Bạn đặt anh ta an toàn lên tàu cứu hộ rồi tự tay đập nát trạm phát tín hiệu trên bến..."
                        : "Con tàu khuất dần ngoài khơi — thế giới bên ngoài đã an toàn. Còn bạn sẽ ở lại đảo mãi mãi.");

                GUI.color = Color.white;
                GUI.Label(new Rect(sw * 0.08f, sh - barH + 10f, sw * 0.84f, barH - 20f), subLine, endingBodyStyle);

                if (endingTimer > CUTSCENE_DURATION - 1.2f)
                {
                    float f = Mathf.Clamp01((endingTimer - (CUTSCENE_DURATION - 1.2f)) / 1.2f);
                    GUI.color = new Color(1f, 1f, 1f, f);
                    GUI.DrawTexture(new Rect(0f, 0f, sw, sh), blackTex);
                }

                GUI.color = prev;
                return;
            }

            float screenTime = endingTimer - CUTSCENE_DURATION;
            GUI.color = new Color(1f, 1f, 1f, 0.96f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), blackTex);

            float textAlpha = Mathf.Clamp01(screenTime / 1.0f);
            if (textAlpha > 0.01f)
            {
                GUI.color = new Color(1f, 1f, 1f, textAlpha);
                float cy = sh * 0.16f;
                float lw = Mathf.Min(660f, sw * 0.74f);
                float lx = (sw - lw) * 0.5f;

                GUI.DrawTexture(new Rect(lx, cy - 12f, lw, 2f), isEnding2 ? redLineTex : goldLineTex);

                string bigTitle = isEnding2 ? "NGƯỜI GÁC ĐẢO VĨNH CỬU" : "THOÁT KHỎI ĐỊA NGỤC";
                string subTitle = isEnding2 ? "ENDING 2 / 2  —  TRUE ENDING (STAY FOREVER)" : "ENDING 1 / 2  —  ESCAPE ENDING";
                string epilogue = isEnding2
                    ? "Bạn đã tiêm liều Huyết Thanh cuối cùng, cõng người sống sót ra tận bến tàu và tiễn anh ta rời đi trong ánh bình minh.\n" +
                      "Tự tay đập nát trạm phát tín hiệu để không một chuyến bay nào còn rơi xuống hòn đảo bị nguyền rủa này nữa.\n" +
                      "Thế giới bên ngoài đã được cứu — còn bạn sẽ ở lại hòn đảo này mãi mãi."
                    : "Con tàu cứu hộ rẽ sóng đưa hai người rời khỏi hòn đảo chết chóc khi mặt trời vừa ló rạng.\n" +
                      "Bạn đã sống sót sau thảm họa chuyến bay C400, tìm ra Phòng Thí Nghiệm Bí Mật và hạ gục Nữ Hoàng Ký Sinh...\n" +
                      "Nhưng sâu trong tĩnh mạch, một nhịp đập kỳ lạ khẽ rung lên — cơn ác mộng có thực sự đã kết thúc?";

                GUI.Label(new Rect(0, cy, sw, 64), bigTitle, endingBigTitleStyle);
                GUI.Label(new Rect(0, cy + 62, sw, 28), subTitle, endingSubStyle);
                GUI.DrawTexture(new Rect(lx, cy + 96f, lw, 2f), isEnding2 ? redLineTex : goldLineTex);
                GUI.Label(new Rect((sw - lw) * 0.5f, cy + 116f, lw, 128f), epilogue, endingBodyStyle);

                int unlockedCount = PlayerPrefs.GetInt("DeadIsland_Ending1_Unlocked", 0) + PlayerPrefs.GetInt("DeadIsland_Ending2_Unlocked", 0);
                string statsLine = string.Format("★ NGÀY SỐNG SÓT: {0}   |   ☠ QUÁI ĐÃ HẠ: {1}   |   🏆 ENDING ĐÃ MỞ: {2}/2",
                    finalSurvivedDays, TotalEnemiesKilled, unlockedCount);
                GUI.Label(new Rect(0, cy + 252f, sw, 30f), statsLine, endingStatsStyle);
                GUI.Label(new Rect(0, cy + 286f, sw, 26f), "— DEAD ISLAND SURVIVAL  •  CẢM ƠN BẠN ĐÃ TRẢI NGHIỆM —", endingSubStyle);
            }

            float btnAlpha = Mathf.Clamp01((screenTime - 0.9f) / 0.7f);
            if (btnAlpha > 0.01f)
            {
                GUI.color = new Color(1f, 1f, 1f, btnAlpha);
                float bw = 320f, bh = 46f;
                float bx = (sw - bw) * 0.5f;
                float by = sh * 0.73f;

                if (isEnding2)
                {
                    if (GUI.Button(new Rect(bx, by, bw, bh), "🌲  TIẾP TỤC SINH TỒN TỰ DO MÃI MÃI", choiceBtnStyle))
                        ContinueFreeRoamAfterEnding2();
                    by += 58f;
                }
                else
                {
                    if (GUI.Button(new Rect(bx, by, bw, bh), "↻  CHƠI LẠI TỪ ĐẦU", choiceBtnStyle))
                    {
                        Time.timeScale = 1f;
                        TotalEnemiesKilled = 0;
                        GameMenuManager.hasStartedFromMainMenu = true;
                        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    }
                    by += 58f;
                }

                if (GUI.Button(new Rect(bx, by, bw, bh), "⌂  VỀ MÀN HÌNH CHÍNH (MAIN MENU)", choiceAltBtnStyle))
                {
                    Time.timeScale = 1f;
                    TotalEnemiesKilled = 0;
                    GameMenuManager.hasStartedFromMainMenu = false;
                    if (GameMenuManager.Instance != null) GameMenuManager.Instance.LoadMainMenu();
                    else SceneManager.LoadScene("MainMenu");
                }
            }

            GUI.color = prev;
        }
    }
}

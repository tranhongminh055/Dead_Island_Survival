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
        SurviveAndPrepare = 0,   // Chưa đủ điều kiện ngày sinh tồn
        FindSurvivorInVillage = 1, // Tới Làng Bỏ Hoang tìm Người Sống Sót
        BossFightActive = 2,     // Đang chiến đấu với Nữ Hoàng Ký Sinh tại Làng Bỏ Hoang
        ReturnToDockWithCure = 3, // Đã có Huyết Thanh -> Ra Bến Tàu chọn số phận
        ChoosingEnding = 4,      // Đang mở bảng Lựa Chọn 2 Ending
        Ending1_Escape = 5,      // Ending 1: Cùng nhau thoát khỏi đảo
        Ending2_StayForever = 6, // Ending 2: Hy sinh ở lại đảo mãi mãi (True Ending)
        FreeRoamAfterEnding2 = 7 // Tiếp tục sinh tồn tự do sau Ending 2
    }

    /// <summary>
    /// GameEndingManager — Hệ thống Cốt Truyện Kết Game 2 Ending chuẩn The Forest (Cách 1):
    /// 1. Điều kiện mở khóa: Sống sót tới Ngày quy định (mặc định Ngày 2) -> Tới Làng Bỏ Hoang tìm Người Sống Sót chuyến bay C400.
    /// 2. Đánh thức & Hạ gục Trùm Cuối Nữ Hoàng Ký Sinh (ParasiteQueenBoss) ngay tại Sân Làng Bỏ Hoang để lấy liều Huyết Thanh Giải Độc duy nhất.
    /// 3. Quay về Bến Tàu (ngay gần Xác Máy Bay) -> Lựa chọn 1 trong 2 Ending đầy cảm xúc:
    ///    - Ending 1: CÙNG NHAU THOÁT KHỎI ĐỊA NGỤC (Escape Ending)
    ///    - Ending 2: HY SINH & Ở LẠI MÃI MÃI (True Ending — mở khóa sinh tồn tự do mãi mãi!)
    ///
    /// Phím test nhanh trong Unity Editor:
    /// - [Left Ctrl + F9]: Mở khóa ngày & dịch chuyển tới Làng Bỏ Hoang trước mặt Người Sống Sót (bấm [E] gọi Boss).
    /// - [Left Ctrl + F10]: Mô phỏng đã hạ Boss + có Huyết Thanh & dịch chuyển ra Bến Tàu (bấm [E] mở bảng chọn 2 Ending).
    /// </summary>
    public class GameEndingManager : MonoBehaviour
    {
        public static GameEndingManager Instance { get; private set; }
        public static bool IsEndingUIActive
        {
            get
            {
                return Instance != null && (
                    Instance.Stage == EndingStoryStage.ChoosingEnding ||
                    Instance.Stage == EndingStoryStage.Ending1_Escape ||
                    Instance.Stage == EndingStoryStage.Ending2_StayForever);
            }
        }

        [Header("── Điều Kiện Mở Khóa Kết Game ──")]
        [Tooltip("Số ngày tối thiểu phải sống sót để đánh thức sự kiện cứu người & Boss cuối")]
        public int requiredSurvivalDay = 2;

        [Header("── Vị Trí Các Mốc Cốt Truyện ──")]
        [Tooltip("Sân Làng Bỏ Hoang — Nơi Người Sống Sót bị kẹt & Trùm Cuối xuất hiện")]
        public Vector3 villageSurvivorPos = new Vector3(495f, 19.0f, 520f);

        [Tooltip("Bến Tàu Thoát Hiểm — Nơi gọi tàu cứu hộ & chọn 1 trong 2 Ending")]
        public Vector3 dockRescuePos = new Vector3(522f, 17.6f, 548f);

        public EndingStoryStage Stage { get; private set; }
        public bool HasParasiteCure { get; private set; }
        public bool HasDefeatedBoss { get; private set; }
        public bool HasFoundSurvivor { get; private set; }

        private Transform playerTransform;
        private PlayerController playerController;
        private PlayerStats playerStats;

        private GameObject survivorBeaconObj;
        private GameObject dockBeaconObj;
        private GameObject dockSmokeObj;
        private Light dockBeaconLight;
        private ParasiteQueenBoss activeBoss;

        // UI & hiệu ứng chuyển cảnh Ending
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
        private GUIStyle modalTitleStyle;
        private GUIStyle modalStoryStyle;
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
                Debug.Log("🎬 [GameEndingManager] Đã khởi tạo hệ thống Kết Game 2 Ending (Cách 1)!");
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
            ResolveLandmarkPositions();
            CreateSurvivorCampVisual();
            CreateDockBeaconVisual();
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

        private void ResolveLandmarkPositions()
        {
            CacheBuildingBlockers();

            // 0. Ưu tiên điểm đặt thủ công nếu người dùng tạo sẵn "SurvivorSpawnPoint"
            GameObject manualSurvivor = GameObject.Find("SurvivorSpawnPoint");
            if (manualSurvivor != null)
            {
                villageSurvivorPos = manualSurvivor.transform.position;
            }
            else
            {
                // 1. Bãi cỏ ngoài trời phía Nam Làng Bỏ Hoang (500, 470) — đã kiểm chứng từ dữ liệu Scene_A: cách mảnh nhà gần nhất ~26m
                // (Điểm cũ (472, 504) thực chất nằm TRONG biệt thự Villa2 — cạnh kính cửa sổ & thảm sàn tầng 1)
                villageSurvivorPos = FindOpenOutdoorSpot(new Vector3(500f, 17.9f, 470f), 20f);
            }

            // 2. Tìm đúng vị trí Bến Tàu (Struct_Docking / Boat) gần khu vực Xác Máy Bay (537, 17.6, 545)
            // LƯU Ý: Không dùng airplane.transform.position vì root [C400_AIRPLANE_CABIN] nằm ở (0,0,0)!
            GameObject manualDock = GameObject.Find("DockRescuePoint");
            if (manualDock != null)
            {
                dockRescuePos = manualDock.transform.position;
            }
            else
            {
                Vector3 crashCenter = ZombieSpawner.CRASH_SITE_CENTER;
                Vector3 bestDock = FindNearestDockOrShore(crashCenter);
                dockRescuePos = FindOpenOutdoorSpot(bestDock, 35f);
            }

            buildingBlockers = null;
            Debug.Log(string.Format("📍 [GameEndingManager] Người Sống Sót NGOÀI SÂN tại {0} | Bến Tàu Cứu Hộ tại {1}", villageSurvivorPos, dockRescuePos));
        }

        private Vector3 FindNearestDockOrShore(Vector3 nearCenter)
        {
            MeshRenderer[] all = FindObjectsOfType<MeshRenderer>();
            float bestDist = 99999f;
            Vector3 bestPos = new Vector3(522f, 17.8f, 548f);
            bool foundDock = false;

            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                string n = all[i].gameObject.name.ToLower();
                if (n.Contains("dock") || n.Contains("pier") || n.Contains("boat"))
                {
                    Vector3 p = all[i].bounds.center;
                    float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(nearCenter.x, nearCenter.z));
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestPos = p;
                        foundDock = true;
                    }
                }
            }

            if (foundDock && bestDist < 220f)
            {
                // Lùi nhẹ vào bờ đất khô cạnh bến tàu để người chơi đứng tương tác dễ dàng
                Vector3 dirToLand = (nearCenter - bestPos);
                dirToLand.y = 0f;
                if (dirToLand.sqrMagnitude > 0.1f) dirToLand.Normalize();
                return bestPos + dirToLand * 5.0f;
            }

            return new Vector3(522f, 17.8f, 548f);
        }

        // Danh sách khối bao (Bounds) CHỈ của các mảng nhà cửa (bỏ qua Combined Mesh gộp cả đảo!)
        private List<Bounds> buildingBlockers;

        private void CacheBuildingBlockers()
        {
            buildingBlockers = new List<Bounds>();
            MeshRenderer[] all = FindObjectsOfType<MeshRenderer>();
            for (int i = 0; i < all.Length; i++)
            {
                MeshRenderer r = all[i];
                if (r == null || !r.enabled) continue;
                if (r.transform.IsChildOf(transform)) continue;

                // BỎ QUA Static Batching Combined Mesh (vì Bounds của nó bao trùm cả trăm mét khắp bản đồ!)
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null && mf.sharedMesh.name.Contains("Combined")) continue;

                Bounds b = r.bounds;
                if (b.size.x > 26f || b.size.z > 26f) continue;

                string n = r.gameObject.name.ToLower();
                string pn = r.transform.parent != null ? r.transform.parent.name.ToLower() : "";
                bool isHousePart = n.Contains("villa") || n.Contains("house") || n.Contains("wall") ||
                                   n.Contains("floor") || n.Contains("roof") || n.Contains("ceiling") ||
                                   n.Contains("woodboard") || n.Contains("building") || n.Contains("cabin") ||
                                   pn.Contains("villa") || pn.Contains("house") || pn.Contains("building");
                if (isHousePart)
                {
                    buildingBlockers.Add(b);
                }
            }
        }

        private Vector3 FindOpenOutdoorSpot(Vector3 preferred, float maxSearchRadius)
        {
            Vector3 result;
            if (IsOpenOutdoorGround(preferred, out result)) return result;

            for (float r = 4f; r <= maxSearchRadius; r += 4f)
            {
                for (int deg = 0; deg < 360; deg += 20)
                {
                    float rad = deg * Mathf.Deg2Rad;
                    Vector3 cand = new Vector3(preferred.x + Mathf.Cos(rad) * r, preferred.y, preferred.z + Mathf.Sin(rad) * r);
                    if (IsOpenOutdoorGround(cand, out result)) return result;
                }
            }

            if (Terrain.activeTerrain != null)
            {
                preferred.y = Mathf.Max(Terrain.activeTerrain.SampleHeight(preferred) + Terrain.activeTerrain.transform.position.y, 17.8f);
            }
            return preferred;
        }

        private bool IsOpenOutdoorGround(Vector3 xzPos, out Vector3 groundPos)
        {
            groundPos = xzPos;
            if (Terrain.activeTerrain == null) return false;

            float ty = Terrain.activeTerrain.SampleHeight(xzPos) + Terrain.activeTerrain.transform.position.y;
            if (ty < 17.3f) return false; // Dưới mực nước (16.8m)

            Vector3 ground = new Vector3(xzPos.x, ty, xzPos.z);

            // 1. Tuyệt đối không nằm trong hoặc sát vách bất kỳ căn nhà nào (đệm 3.5m)
            if (buildingBlockers != null)
            {
                for (int i = 0; i < buildingBlockers.Count; i++)
                {
                    Bounds b = buildingBlockers[i];
                    if (ground.x > b.min.x - 3.5f && ground.x < b.max.x + 3.5f &&
                        ground.z > b.min.z - 3.5f && ground.z < b.max.z + 3.5f)
                    {
                        return false;
                    }
                }
            }

            // 2. Không bị mái nhà hay cầu che trên đầu
            if (Physics.Raycast(ground + Vector3.up * 0.5f, Vector3.up, 35f, ~0, QueryTriggerInteraction.Ignore))
                return false;

            // 3. Khoảng trống 1.5m quanh vị trí ngồi không vướng vật cản cứng (cây to / đá tảng)
            Collider[] near = Physics.OverlapSphere(ground + Vector3.up * 1.1f, 1.5f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < near.Length; i++)
            {
                if (near[i] == null || near[i].GetComponent<Terrain>() != null) continue;
                if (near[i].transform.IsChildOf(transform)) continue;
                if (near[i].GetComponentInParent<CharacterController>() != null) continue;
                return false;
            }

            // 4. Đảm bảo nằm trên mặt đất đi lại được (NavMesh)
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(ground, out navHit, 3.0f, NavMesh.AllAreas))
            {
                if (Mathf.Abs(navHit.position.y - ty) > 1.2f) return false;
            }

            groundPos = ground;
            return true;
        }

        private void CreateSurvivorCampVisual()
        {
            survivorBeaconObj = new GameObject("Ending_SurvivorCamp_Village");
            survivorBeaconObj.transform.SetParent(transform);
            survivorBeaconObj.transform.position = villageSurvivorPos;

            // 1. Ống pháo khói đỏ đặt trên mặt đất
            GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lamp.name = "Survivor_FlareStick";
            lamp.transform.SetParent(survivorBeaconObj.transform, false);
            lamp.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            lamp.transform.localScale = new Vector3(0.14f, 0.22f, 0.14f);
            SetObjColor(lamp, new Color(0.95f, 0.15f, 0.12f), true);

            // 2. Hình nhân Người Sống Sót chuyến bay C400 (ngồi tựa bị thương, mặc áo phao cam nổi bật dễ nhìn từ xa!)
            GameObject person = new GameObject("Injured_Survivor_C400");
            person.transform.SetParent(survivorBeaconObj.transform, false);
            person.transform.localPosition = new Vector3(0.95f, 0f, 0.35f);
            person.transform.localRotation = Quaternion.Euler(0f, -140f, 0f);

            // Thân áo phao cứu hộ màu cam nổi bật
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso";
            torso.transform.SetParent(person.transform, false);
            torso.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            torso.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            torso.transform.localScale = new Vector3(0.46f, 0.62f, 0.28f);
            SetObjColor(torso, new Color(0.92f, 0.42f, 0.10f), false);

            // Đầu
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(person.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.08f, -0.05f);
            head.transform.localScale = new Vector3(0.28f, 0.30f, 0.28f);
            SetObjColor(head, new Color(0.86f, 0.70f, 0.56f), false);

            // Hai chân duỗi ngồi trên cỏ
            GameObject legL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            legL.transform.SetParent(person.transform, false);
            legL.transform.localPosition = new Vector3(-0.14f, 0.18f, 0.38f);
            legL.transform.localRotation = Quaternion.Euler(82f, -12f, 0f);
            legL.transform.localScale = new Vector3(0.15f, 0.42f, 0.15f);
            SetObjColor(legL, new Color(0.18f, 0.22f, 0.30f), false);

            GameObject legR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            legR.transform.SetParent(person.transform, false);
            legR.transform.localPosition = new Vector3(0.14f, 0.18f, 0.38f);
            legR.transform.localRotation = Quaternion.Euler(82f, 14f, 0f);
            legR.transform.localScale = new Vector3(0.15f, 0.42f, 0.15f);
            SetObjColor(legR, new Color(0.18f, 0.22f, 0.30f), false);

            // Hai tay chống xuống đất
            GameObject armL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            armL.transform.SetParent(person.transform, false);
            armL.transform.localPosition = new Vector3(-0.30f, 0.42f, -0.05f);
            armL.transform.localRotation = Quaternion.Euler(15f, 0f, -18f);
            armL.transform.localScale = new Vector3(0.11f, 0.34f, 0.11f);
            SetObjColor(armL, new Color(0.92f, 0.42f, 0.10f), false);

            GameObject armR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            armR.transform.SetParent(person.transform, false);
            armR.transform.localPosition = new Vector3(0.30f, 0.42f, -0.05f);
            armR.transform.localRotation = Quaternion.Euler(15f, 0f, 18f);
            armR.transform.localScale = new Vector3(0.11f, 0.34f, 0.11f);
            SetObjColor(armR, new Color(0.92f, 0.42f, 0.10f), false);

            // 3. Hộp cứu thương trắng chữ thập đỏ bên cạnh
            GameObject medkit = GameObject.CreatePrimitive(PrimitiveType.Cube);
            medkit.name = "Survivor_Medkit";
            medkit.transform.SetParent(survivorBeaconObj.transform, false);
            medkit.transform.localPosition = new Vector3(-0.65f, 0.16f, 0.3f);
            medkit.transform.localRotation = Quaternion.Euler(0f, 22f, 0f);
            medkit.transform.localScale = new Vector3(0.45f, 0.30f, 0.32f);
            SetObjColor(medkit, new Color(0.92f, 0.92f, 0.92f), false);

            Light lgt = survivorBeaconObj.AddComponent<Light>();
            lgt.type = LightType.Point;
            lgt.color = new Color(1.0f, 0.35f, 0.22f);
            lgt.range = 18f;
            lgt.intensity = 2.6f;
            lgt.shadows = LightShadows.None;

            // Cột khói đỏ mềm mịn bốc cao ~90m lên trời
            CreateSignalSmokePlume(survivorBeaconObj.transform, new Color(0.95f, 0.16f, 0.12f, 0.65f), "Survivor_RedSignalSmoke");

            // Cột tia sáng pháo hiệu đỏ mảnh vươn thẳng 55m lên trời giúp định vị cực rõ cả ngày lẫn đêm!
            GameObject skyBeam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            skyBeam.name = "Survivor_SkyBeaconBeam";
            skyBeam.transform.SetParent(survivorBeaconObj.transform, false);
            skyBeam.transform.localPosition = new Vector3(0f, 28f, 0f);
            skyBeam.transform.localScale = new Vector3(0.22f, 28f, 0.22f);
            Collider beamCol = skyBeam.GetComponent<Collider>();
            if (beamCol != null) beamCol.enabled = false;
            Renderer beamRen = skyBeam.GetComponent<Renderer>();
            if (beamRen != null)
            {
                Shader sh = Shader.Find("Particles/Additive") ?? Shader.Find("Unlit/Color");
                Material bm = new Material(sh);
                bm.color = new Color(1.0f, 0.18f, 0.12f, 0.35f);
                beamRen.sharedMaterial = bm;
            }
        }

        private void SetObjColor(GameObject go, Color c, bool unlit)
        {
            // Tắt collider phụ trên các khối trang trí để không vướng bước chân hay tia nhìn của Player
            Collider col = go.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Renderer r = go.GetComponent<Renderer>();
            if (r == null) return;
            Shader sh = unlit ? (Shader.Find("Unlit/Color") ?? Shader.Find("Standard")) : (Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            Material m = new Material(sh);
            m.color = c;
            r.sharedMaterial = m;
        }

        private void CreateDockBeaconVisual()
        {
            dockBeaconObj = new GameObject("Ending_RescueBeacon_Dock");
            dockBeaconObj.transform.SetParent(transform);
            dockBeaconObj.transform.position = dockRescuePos;

            // Trụ pháo hiệu cứu hộ tại Bến Tàu
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Dock_RadioPost";
            post.transform.SetParent(dockBeaconObj.transform, false);
            post.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            post.transform.localScale = new Vector3(0.22f, 1.1f, 0.22f);
            Renderer pr = post.GetComponent<Renderer>();
            if (pr != null)
            {
                Material pm = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
                pm.color = new Color(0.22f, 0.24f, 0.28f);
                pr.sharedMaterial = pm;
            }

            // Đèn đỉnh trụ (đỏ khi khóa, vàng-xanh rực rỡ khi đã đủ điều kiện thoát đảo)
            GameObject topBulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            topBulb.name = "Dock_SignalBulb";
            topBulb.transform.SetParent(dockBeaconObj.transform, false);
            topBulb.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            topBulb.transform.localScale = Vector3.one * 0.35f;

            dockBeaconLight = topBulb.AddComponent<Light>();
            dockBeaconLight.type = LightType.Point;
            dockBeaconLight.color = new Color(0.95f, 0.25f, 0.20f);
            dockBeaconLight.range = 14f;
            dockBeaconLight.intensity = 1.6f;
            dockBeaconLight.shadows = LightShadows.None;

            // Cột khói xanh dẫn đường về Bến Tàu (chỉ bốc lên khi đã hạ Boss & có Huyết Thanh!)
            dockSmokeObj = CreateSignalSmokePlume(dockBeaconObj.transform, new Color(0.20f, 0.95f, 0.55f, 0.55f), "Dock_GreenRescueSmoke");
            dockSmokeObj.SetActive(false);
        }

        /// <summary>
        /// Tạo cột khói pháo hiệu bốc cao ~80m lên trời dễ nhìn từ xa khắp bản đồ
        /// </summary>
        private GameObject CreateSignalSmokePlume(Transform parent, Color smokeColor, string objName)
        {
            GameObject smokeObj = new GameObject(objName);
            smokeObj.transform.SetParent(parent, false);
            smokeObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            smokeObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            ParticleSystem ps = smokeObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 12f;
            main.startSpeed = 7.5f; // Cột khói cao ~90 mét!
            main.startSize = new ParticleSystem.MinMaxCurve(2.2f, 3.8f);
            main.startColor = smokeColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 180;

            var emission = ps.emission;
            emission.rateOverTime = 16f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 4.5f;
            shape.radius = 0.35f;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.5f);
            curve.AddKey(0.4f, 1.4f);
            curve.AddKey(1f, 2.6f);
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(smokeColor, 0f), new GradientColorKey(smokeColor, 0.7f), new GradientColorKey(Color.gray, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(smokeColor.a, 0.12f), new GradientAlphaKey(smokeColor.a * 0.7f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = new ParticleSystem.MinMaxGradient(grad);

            ParticleSystemRenderer rend = smokeObj.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                Shader sh = Shader.Find("Particles/Alpha Blended") ?? Shader.Find("Sprites/Default");
                Material mat = new Material(sh);
                mat.mainTexture = CreateSoftSmokeTexture(64);
                mat.color = Color.white;
                rend.sharedMaterial = mat;
            }

            return smokeObj;
        }

        /// <summary>
        /// Tạo texture hạt khói tròn mềm mịn (tránh lỗi hiện thành ô vuông đỏ!)
        /// </summary>
        private Texture2D CreateSoftSmokeTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - dist);
                    a = a * a;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return tex;
        }

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

#if UNITY_EDITOR
            HandleEditorQuickTestKeys();
#endif

            // Nếu đang hiện UI Chọn Ending hoặc Màn Hình Ending
            if (Stage == EndingStoryStage.ChoosingEnding ||
                Stage == EndingStoryStage.Ending1_Escape ||
                Stage == EndingStoryStage.Ending2_StayForever)
            {
                endingTimer += Time.unscaledDeltaTime;
                if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
                if (!Cursor.visible) Cursor.visible = true;
                return;
            }

            if (Stage == EndingStoryStage.FreeRoamAfterEnding2)
            {
                activePrompt = "";
                return;
            }

            // Tự động chuyển từ SurviveAndPrepare -> FindSurvivorInVillage khi đủ ngày sinh tồn
            if (Stage == EndingStoryStage.SurviveAndPrepare && IsSurvivalDayMet())
            {
                Stage = EndingStoryStage.FindSurvivorInVillage;
                ShowStoryBanner("📡 THU ĐƯỢC TÍN HIỆU CẦU CỨU!", "Có người sống sót từ chuyến bay C400 ở Khu Làng Bỏ Hoang — Hãy chuẩn bị vũ khí tới cứu họ!");
            }

            // Nếu Boss đang sống nhưng bị Destroy ngoài ý muốn
            if (Stage == EndingStoryStage.BossFightActive && activeBoss == null)
            {
                OnParasiteQueenDefeated();
            }

            // Cập nhật màu đèn & cột khói xanh tại Trạm Cứu Hộ ở Bến Tàu
            if (dockBeaconLight != null)
            {
                if (Stage == EndingStoryStage.ReturnToDockWithCure)
                {
                    dockBeaconLight.color = new Color(0.25f, 0.95f, 0.65f);
                    dockBeaconLight.intensity = 2.6f + Mathf.Sin(Time.time * 4f) * 0.6f;
                    if (dockSmokeObj != null && !dockSmokeObj.activeSelf) dockSmokeObj.SetActive(true);
                }
                else
                {
                    dockBeaconLight.color = new Color(0.95f, 0.25f, 0.20f);
                    dockBeaconLight.intensity = 1.4f;
                    if (dockSmokeObj != null && dockSmokeObj.activeSelf) dockSmokeObj.SetActive(false);
                }
            }

            UpdatePlayerInteraction();
        }

        private void UpdatePlayerInteraction()
        {
            activePrompt = "";
            if (playerTransform == null || (playerStats != null && playerStats.isDead)) return;

            float distToSurvivor = Vector3.Distance(playerTransform.position, villageSurvivorPos);
            float distToDock = Vector3.Distance(playerTransform.position, dockRescuePos);

            // 1. Tương tác với Người Sống Sót tại Làng Bỏ Hoang
            if (distToSurvivor <= 4.5f && (Stage == EndingStoryStage.SurviveAndPrepare || Stage == EndingStoryStage.FindSurvivorInVillage))
            {
                if (!IsSurvivalDayMet())
                {
                    int curDay = DayNightCycle.Instance != null ? DayNightCycle.Instance.currentDay : 1;
                    activePrompt = string.Format("🧑 NGƯỜI SỐNG SÓT (C400): \"Bão chưa tan và quái vật đang rình!\"\n[KHÓA] Cần sống sót tới Ngày {0} (Hiện tại: Ngày {1}) để mở khóa giải cứu.", requiredSurvivalDay, curDay);
                }
                else
                {
                    activePrompt = "🧑 NGƯỜI SỐNG SÓT (C400) bị nhiễm Ký Sinh Trùng nặng!\n👉 Nhấn [E] để giải cứu (CẢNH BÁO: Sẽ đánh thức Nữ Hoàng Ký Sinh!)";
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        HasFoundSurvivor = true;
                        TriggerParasiteQueenBossFight();
                    }
                }
                return;
            }

            // 2. Tương tác với Trạm Pháo Hiệu Cứu Hộ tại Bến Tàu
            if (distToDock <= 5.0f)
            {
                if (Stage == EndingStoryStage.ReturnToDockWithCure && HasParasiteCure && HasDefeatedBoss)
                {
                    activePrompt = "🚤 TRẠM CỨU HỘ BẾN TÀU ĐÃ SẴN SÀNG!\n👉 Nhấn [E] để kích hoạt Pháo Hiệu & Quyết Định Số Phận (2 Ending)";
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        OpenEndingChoiceScreen();
                    }
                }
                else
                {
                    string c1 = IsSurvivalDayMet() ? "[✓]" : "[✗]";
                    string c2 = HasFoundSurvivor ? "[✓]" : "[✗]";
                    string c3 = HasDefeatedBoss && HasParasiteCure ? "[✓]" : "[✗]";
                    activePrompt = string.Format(
                        "🔒 TRẠM CỨU HỘ BẾN TÀU ĐANG BỊ KHÓA — CẦN ĐỦ ĐIỀU KIỆN:\n{0} Sống sót tới Ngày {1}   |   {2} Cứu Người Sống Sót ở Làng   |   {3} Hạ Nữ Hoàng Ký Sinh lấy Huyết Thanh",
                        c1, requiredSurvivalDay, c2, c3);
                }
            }
        }

        private void TriggerParasiteQueenBossFight()
        {
            Stage = EndingStoryStage.BossFightActive;
            ShowStoryBanner("⚠️ NỮ HOÀNG KÝ SINH ĐÃ THỨC GIẤC!", "Hạ gục Trùm Cuối ngay tại Sân Làng để đoạt liều Huyết Thanh Giải Độc duy nhất!");

            // Tìm mẫu Zombie sẵn có để nâng cấp thành Trùm Cuối Nữ Hoàng Ký Sinh khổng lồ
            GameObject baseModel = null;
            ZombieSpawner zs = FindObjectOfType<ZombieSpawner>();
            if (zs != null && zs.zombiePrefab != null) baseModel = zs.zombiePrefab;
            if (baseModel == null)
            {
                EnemyAI anyEnemy = FindObjectOfType<EnemyAI>();
                if (anyEnemy != null) baseModel = anyEnemy.gameObject;
            }

            Vector3 spawnPos = villageSurvivorPos + new Vector3(14f, 0f, 10f);
            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPos, out hit, 25f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }
            else if (Terrain.activeTerrain != null)
            {
                spawnPos.y = Terrain.activeTerrain.SampleHeight(spawnPos) + Terrain.activeTerrain.transform.position.y;
            }

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
            bossObj.transform.localScale = Vector3.one * 3.2f; // Khổng lồ cao ~5.5m chuẩn Boss Sons of the Forest!

            // Đảm bảo Animator hoạt động mượt
            Animator anim = bossObj.GetComponent<Animator>();
            if (anim != null)
            {
                anim.enabled = true;
                anim.applyRootMotion = false;
            }

            activeBoss = bossObj.AddComponent<ParasiteQueenBoss>();
            activeBoss.player = playerTransform;
            activeBoss.bossTitle = "☠ NỮ HOÀNG KÝ SINH (PARASITE QUEEN) — TRÙM CUỐI ☠";
        }

        private void OnParasiteQueenDefeated()
        {
            if (HasDefeatedBoss) return;
            HasDefeatedBoss = true;
            HasParasiteCure = true;
            Stage = EndingStoryStage.ReturnToDockWithCure;

            if (survivorBeaconObj != null) survivorBeaconObj.SetActive(false);

            ShowStoryBanner("🏆 ĐÃ HẠ NỮ HOÀNG KÝ SINH & ĐOẠT HUYẾT THANH!", "Mau đưa Người Sống Sót quay về Bến Tàu để kích hoạt Pháo Hiệu thoát khỏi đảo!");
            Debug.Log("🏆 [GameEndingManager] Đã hạ Trùm Cuối! Mở khóa Bến Tàu chọn 2 Ending!");
        }

        private void OpenEndingChoiceScreen()
        {
            Stage = EndingStoryStage.ChoosingEnding;
            endingTimer = 0f;
            finalSurvivedDays = (DayNightCycle.Instance != null) ? Mathf.Max(1, DayNightCycle.Instance.currentDay) : 1;

            if (playerController != null) playerController.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void SelectEnding1_Escape()
        {
            Stage = EndingStoryStage.Ending1_Escape;
            endingTimer = 0f;
            Debug.Log("🚤 [GameEndingManager] Người chơi chọn ENDING 1: CÙNG NHAU THOÁT KHỎI ĐỊA NGỤC!");
        }

        private void SelectEnding2_StayForever()
        {
            Stage = EndingStoryStage.Ending2_StayForever;
            endingTimer = 0f;
            // Phá hủy đèn tín hiệu ở Bến Tàu
            if (dockBeaconObj != null) dockBeaconObj.SetActive(false);
            Debug.Log("🔥 [GameEndingManager] Người chơi chọn ENDING 2: HY SINH & Ở LẠI MÃI MÃI (TRUE ENDING)!");
        }

        private void ContinueFreeRoamAfterEnding2()
        {
            Stage = EndingStoryStage.FreeRoamAfterEnding2;
            if (playerController != null) playerController.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            ShowStoryBanner("🌲 CHẾ ĐỘ SINH TỒN TỰ DO ĐÃ MỞ KHÓA", "Bạn là Người Gác Đảo Vĩnh Cửu. Chúc bạn sinh tồn mãi mãi trên hòn đảo này!");
        }

        public void ShowStoryBanner(string title, string sub)
        {
            storyBannerTitle = title;
            storyBannerSub = sub;
            storyBannerTimer = 6.0f;
        }

#if UNITY_EDITOR
        private void HandleEditorQuickTestKeys()
        {
            if (playerTransform == null) return;

            // LeftCtrl + F9: Nhảy nhanh tới Làng Bỏ Hoang (đủ Ngày 2) & quay mặt thẳng vào Người Sống Sót để test gọi Boss
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F9))
            {
                if (DayNightCycle.Instance != null && DayNightCycle.Instance.currentDay < requiredSurvivalDay)
                {
                    DayNightCycle.Instance.currentDay = requiredSurvivalDay;
                }
                Stage = EndingStoryStage.FindSurvivorInVillage;
                TeleportPlayerSafe(villageSurvivorPos + new Vector3(0f, 0.5f, -3.5f), villageSurvivorPos);
                ShowStoryBanner("🧪 [TEST MODE] Đã dịch chuyển tới Người Sống Sót!", "Nhấn [E] ngay trước mặt Người Sống Sót áo cam để đánh thức Nữ Hoàng Ký Sinh!");
            }

            // LeftCtrl + F10: Mô phỏng đã hạ Boss & có Huyết Thanh -> Dịch chuyển ra Bến Tàu test bảng 2 Ending
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F10))
            {
                if (DayNightCycle.Instance != null && DayNightCycle.Instance.currentDay < requiredSurvivalDay)
                {
                    DayNightCycle.Instance.currentDay = requiredSurvivalDay;
                }
                HasFoundSurvivor = true;
                HasDefeatedBoss = true;
                HasParasiteCure = true;
                Stage = EndingStoryStage.ReturnToDockWithCure;
                TeleportPlayerSafe(dockRescuePos + new Vector3(0f, 0.5f, -3.5f), dockRescuePos);
                ShowStoryBanner("🧪 [TEST MODE] Đã có Huyết Thanh & tới Bến Tàu!", "Nhấn [E] tại Trụ Tín Hiệu Bến Tàu để chọn 1 trong 2 Ending!");
            }
        }

        private void TeleportPlayerSafe(Vector3 preferredPos, Vector3 lookTarget)
        {
            Vector3 finalPos = preferredPos;

            // Quét 8 góc cách mục tiêu 3.6m tìm điểm đứng thoáng không vướng tường/cây và nhìn thẳng thấy mục tiêu
            for (int i = 0; i < 8; i++)
            {
                float rad = (i * 45f) * Mathf.Deg2Rad;
                Vector3 cand = lookTarget + new Vector3(Mathf.Sin(rad) * 3.6f, 0f, -Mathf.Cos(rad) * 3.6f);
                if (Terrain.activeTerrain != null)
                {
                    cand.y = Terrain.activeTerrain.SampleHeight(cand) + Terrain.activeTerrain.transform.position.y + 0.25f;
                }

                NavMeshHit navHit;
                if (NavMesh.SamplePosition(cand, out navHit, 2.0f, NavMesh.AllAreas))
                {
                    Vector3 eyeFrom = navHit.position + Vector3.up * 1.4f;
                    Vector3 eyeTo = lookTarget + Vector3.up * 0.9f;
                    if (!Physics.Linecast(eyeFrom, eyeTo, ~0, QueryTriggerInteraction.Ignore))
                    {
                        finalPos = navHit.position + Vector3.up * 0.2f;
                        break;
                    }
                }
            }

            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            playerTransform.position = finalPos;
            Vector3 flatLook = new Vector3(lookTarget.x, finalPos.y, lookTarget.z);
            playerTransform.LookAt(flatLook);
            if (playerController != null) playerController.ResetFallState();
            if (cc != null) cc.enabled = true;
        }
#endif

        private void InitStyles()
        {
            if (stylesReady) return;
            stylesReady = true;

            blackTex = MakeTex(Color.black);
            panelTex = MakeTex(new Color(0.05f, 0.06f, 0.09f, 0.92f));
            goldLineTex = MakeTex(new Color(0.95f, 0.74f, 0.25f, 0.9f));
            redLineTex = MakeTex(new Color(0.85f, 0.22f, 0.22f, 0.9f));
            btnNormalTex = MakeTex(new Color(0.12f, 0.22f, 0.30f, 0.90f));
            btnHoverTex = MakeTex(new Color(0.18f, 0.38f, 0.52f, 0.98f));
            btnAltNormalTex = MakeTex(new Color(0.28f, 0.11f, 0.11f, 0.90f));
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

            modalTitleStyle = new GUIStyle(GUI.skin.label);
            modalTitleStyle.fontSize = 30;
            modalTitleStyle.fontStyle = FontStyle.Bold;
            modalTitleStyle.alignment = TextAnchor.MiddleCenter;
            modalTitleStyle.normal.textColor = new Color(1.0f, 0.82f, 0.35f);

            modalStoryStyle = new GUIStyle(GUI.skin.label);
            modalStoryStyle.fontSize = 16;
            modalStoryStyle.wordWrap = true;
            modalStoryStyle.alignment = TextAnchor.MiddleCenter;
            modalStoryStyle.normal.textColor = new Color(0.90f, 0.92f, 0.94f);

            choiceBtnStyle = new GUIStyle(GUI.skin.button);
            choiceBtnStyle.fontSize = 16;
            choiceBtnStyle.fontStyle = FontStyle.Bold;
            choiceBtnStyle.wordWrap = true;
            choiceBtnStyle.normal.background = btnNormalTex;
            choiceBtnStyle.hover.background = btnHoverTex;
            choiceBtnStyle.normal.textColor = Color.white;
            choiceBtnStyle.hover.textColor = Color.white;
            choiceBtnStyle.padding = new RectOffset(14, 14, 10, 10);

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

            // 1. Màn hình Lựa Chọn 2 Ending
            if (Stage == EndingStoryStage.ChoosingEnding)
            {
                DrawEndingChoiceModal(sw, sh);
                return;
            }

            // 2. Màn hình Kết Thúc Game (Ending 1 hoặc Ending 2)
            if (Stage == EndingStoryStage.Ending1_Escape || Stage == EndingStoryStage.Ending2_StayForever)
            {
                DrawEndingScreen(sw, sh, Stage == EndingStoryStage.Ending2_StayForever);
                return;
            }

            // 3. Banner cốt truyện
            if (storyBannerTimer > 0f)
            {
                float alpha = Mathf.Clamp01(storyBannerTimer < 1f ? storyBannerTimer : 1f);
                Color prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                float bw = 580f, bh = 74f;
                float bx = (sw - bw) * 0.5f, by = 78f;
                GUI.Box(new Rect(bx, by, bw, bh), "", promptStyle);
                GUI.DrawTexture(new Rect(bx, by, bw, 3f), goldLineTex);
                GUI.Label(new Rect(bx + 10, by + 8, bw - 20, 28), storyBannerTitle, bannerTitleStyle);
                GUI.Label(new Rect(bx + 10, by + 38, bw - 20, 30), storyBannerSub, bannerSubStyle);
                GUI.color = prev;
            }

            // 4. Nhãn định vị trực tiếp trên màn hình (Waypoint) giúp nhìn thấy vị trí Người Sống Sót / Bến Tàu kể cả ban đêm hay bị nhà che!
            DrawWaypointMarker(sw, sh);

            // 5. Prompt tương tác [E] gần Người Sống Sót / Trạm Bến Tàu
            if (!string.IsNullOrEmpty(activePrompt))
            {
                float pw = Mathf.Min(640f, sw - 40f);
                float ph = 64f;
                GUI.Box(new Rect((sw - pw) * 0.5f, sh * 0.72f, pw, ph), activePrompt, promptStyle);
            }
        }

        private void DrawWaypointMarker(float sw, float sh)
        {
            if (playerTransform == null) return;

            Camera cam = Camera.main;
            if (cam == null && playerController != null && playerController.playerCamera != null)
                cam = playerController.playerCamera.GetComponent<Camera>();
            if (cam == null) cam = FindObjectOfType<Camera>();
            if (cam == null) return;

            Vector3 targetPos;
            string label;
            Color markerColor;

            if (Stage == EndingStoryStage.SurviveAndPrepare || Stage == EndingStoryStage.FindSurvivorInVillage)
            {
                targetPos = villageSurvivorPos + Vector3.up * 2.2f;
                label = "🔴 NGƯỜI SỐNG SÓT (C400)";
                markerColor = new Color(1.0f, 0.38f, 0.28f);
            }
            else if (Stage == EndingStoryStage.ReturnToDockWithCure)
            {
                targetPos = dockRescuePos + Vector3.up * 2.8f;
                label = "🟢 TRẠM BẾN TÀU CỨU HỘ";
                markerColor = new Color(0.30f, 1.0f, 0.60f);
            }
            else
            {
                return;
            }

            float dist = Vector3.Distance(playerTransform.position, targetPos);
            Vector3 screenPos = cam.WorldToScreenPoint(targetPos);
            bool isBehind = screenPos.z < 0f;
            if (isBehind)
            {
                screenPos.x = sw - screenPos.x;
                screenPos.y = sh - screenPos.y;
            }

            float x = Mathf.Clamp(screenPos.x, 125f, sw - 125f);
            float y = Mathf.Clamp(sh - screenPos.y, 115f, sh - 135f);
            if (isBehind) y = sh - 135f;

            string text = string.Format("{0}  [{1:0}m]", label, dist);
            GUIStyle badgeStyle = new GUIStyle(promptStyle);
            badgeStyle.fontSize = 13;
            badgeStyle.normal.textColor = markerColor;
            badgeStyle.padding = new RectOffset(8, 8, 4, 4);

            Rect r = new Rect(x - 130f, y - 16f, 260f, 32f);
            GUI.Box(r, text, badgeStyle);
        }

        private void DrawEndingChoiceModal(float sw, float sh)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.82f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), blackTex);
            GUI.color = Color.white;

            float mw = Mathf.Min(740f, sw - 60f);
            float mh = 430f;
            float mx = (sw - mw) * 0.5f;
            float my = (sh - mh) * 0.5f;

            GUI.Box(new Rect(mx, my, mw, mh), "", promptStyle);
            GUI.DrawTexture(new Rect(mx, my, mw, 4f), goldLineTex);

            GUI.Label(new Rect(mx + 20, my + 20, mw - 40, 40), "QUYẾT ĐỊNH ĐỊNH MỆNH — BẾN TÀU CỨU HỘ", modalTitleStyle);

            string story =
                "Tàu cứu hộ đã áp sát ngoài khơi — bạn đã hạ gục Nữ Hoàng Ký Sinh và cứu được người sống sót cuối cùng của chuyến bay C400.\n\n" +
                "Nhưng sự thật kinh hoàng vừa phơi bày: CHỈ CÒN DUY NHẤT 1 LIỀU HUYẾT THANH GIẢI ĐỘC, " +
                "và chính trong máu của bạn cũng đã mang mầm ký sinh trùng ngủ yên. Nếu rời đi, mầm bệnh có thể theo bạn về đất liền...";
            GUI.Label(new Rect(mx + 34, my + 74, mw - 68, 125), story, modalStoryStyle);

            float btnW = mw - 80f;
            float btnH = 74f;
            float btnX = mx + 40f;

            if (GUI.Button(new Rect(btnX, my + 218, btnW, btnH),
                "🚤  ENDING 1: CÙNG NHAU THOÁT KHỎI ĐỊA NGỤC\n(Lên tàu cứu hộ rời khỏi hòn đảo ngay bây giờ — bất chấp hiểm họa mầm bệnh)", choiceBtnStyle))
            {
                SelectEnding1_Escape();
            }

            if (GUI.Button(new Rect(btnX, my + 306, btnW, btnH),
                "🔥  ENDING 2: HY SINH & Ở LẠI MÃI MÃI  (TRUE ENDING)\n(Nhường liều Huyết Thanh đưa người kia về đất liền an toàn — phá hủy trạm tín hiệu và ở lại đảo mãi mãi)", choiceAltBtnStyle))
            {
                SelectEnding2_StayForever();
            }
        }

        private void DrawEndingScreen(float sw, float sh, bool isEnding2)
        {
            float fade = Mathf.Clamp01(endingTimer / 1.5f);
            Color prev = GUI.color;

            GUI.color = new Color(1f, 1f, 1f, fade * 0.94f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), blackTex);

            float textAlpha = Mathf.Clamp01((endingTimer - 0.6f) / 1.0f);
            if (textAlpha > 0.01f)
            {
                GUI.color = new Color(1f, 1f, 1f, textAlpha);

                float cy = sh * 0.20f;
                float lw = Mathf.Min(620f, sw * 0.72f);
                float lx = (sw - lw) * 0.5f;

                GUI.DrawTexture(new Rect(lx, cy - 12f, lw, 2f), isEnding2 ? redLineTex : goldLineTex);

                string bigTitle = isEnding2 ? "NGƯỜI GÁC ĐẢO VĨNH CỬU" : "THOÁT KHỎI ĐỊA NGỤC";
                string subTitle = isEnding2 ? "ENDING 2 / 2  —  TRUE ENDING (STAY FOREVER)" : "ENDING 1 / 2  —  ESCAPE ENDING";
                string epilogue = isEnding2
                    ? "Bạn đã tiêm liều Huyết Thanh cuối cùng và tiễn người sống sót lên con tàu cứu hộ khuất dần trong ánh bình minh.\n" +
                      "Tự tay đập nát trạm phát tín hiệu để không một chuyến bay nào còn rơi xuống hòn đảo bị nguyền rủa này nữa.\n" +
                      "Thế giới bên ngoài đã được cứu — còn bạn, kẻ sống sót mạnh mẽ nhất, sẽ ở lại hòn đảo này mãi mãi."
                    : "Con tàu cứu hộ rẽ sóng đưa hai người rời khỏi hòn đảo chết chóc khi mặt trời vừa ló rạng.\n" +
                      "Bạn đã sống sót sau thảm họa chuyến bay C400 và hạ gục Nữ Hoàng Ký Sinh...\n" +
                      "Nhưng sâu trong tĩnh mạch, một nhịp đập kỳ lạ khẽ rung lên — cơn ác mộng có thực sự đã kết thúc?";

                GUI.Label(new Rect(0, cy, sw, 64), bigTitle, endingBigTitleStyle);
                GUI.Label(new Rect(0, cy + 62, sw, 28), subTitle, endingSubStyle);
                GUI.DrawTexture(new Rect(lx, cy + 96f, lw, 2f), isEnding2 ? redLineTex : goldLineTex);

                GUI.Label(new Rect((sw - lw) * 0.5f, cy + 120f, lw, 130f), epilogue, endingBodyStyle);

                string statsLine = string.Format("★ SỐ NGÀY SỐNG SÓT: {0} NGÀY   |   ☠ TRÙM CUỐI: NỮ HOÀNG KÝ SINH (ĐÃ HẠ)   |   DEAD ISLAND SURVIVAL", finalSurvivedDays);
                GUI.Label(new Rect(0, cy + 258f, sw, 32f), statsLine, endingStatsStyle);
            }

            float btnAlpha = Mathf.Clamp01((endingTimer - 1.8f) / 0.8f);
            if (btnAlpha > 0.01f)
            {
                GUI.color = new Color(1f, 1f, 1f, btnAlpha);
                float bw = 310f, bh = 46f;
                float bx = (sw - bw) * 0.5f;
                float by = sh * 0.72f;

                if (isEnding2)
                {
                    if (GUI.Button(new Rect(bx, by, bw, bh), "🌲  TIẾP TỤC SINH TỒN TỰ DO MÃI MÃI", choiceBtnStyle))
                    {
                        ContinueFreeRoamAfterEnding2();
                    }
                    by += 58f;
                }
                else
                {
                    if (GUI.Button(new Rect(bx, by, bw, bh), "↻  CHƠI LẠI TỪ ĐẦU", choiceBtnStyle))
                    {
                        Time.timeScale = 1f;
                        GameMenuManager.hasStartedFromMainMenu = true;
                        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    }
                    by += 58f;
                }

                if (GUI.Button(new Rect(bx, by, bw, bh), "⌂  VỀ MÀN HÌNH CHÍNH (MAIN MENU)", choiceAltBtnStyle))
                {
                    Time.timeScale = 1f;
                    GameMenuManager.hasStartedFromMainMenu = false;
                    if (GameMenuManager.Instance != null) GameMenuManager.Instance.LoadMainMenu();
                    else SceneManager.LoadScene("MainMenu");
                }
            }

            GUI.color = prev;
        }
    }
}


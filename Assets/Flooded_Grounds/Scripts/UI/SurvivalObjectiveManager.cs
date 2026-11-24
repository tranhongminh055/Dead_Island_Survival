using UnityEngine;
using HorrorGame.Environment;
using HorrorGame.Player;
using HorrorGame.Inventory;
using HorrorGame.Survival;

namespace HorrorGame.UI
{
    /// <summary>
    /// SurvivalObjectiveManager — Hệ thống Bảng Mục Tiêu Sinh Tồn (Objective Tracker HUD)
    /// Dẫn dắt người chơi từng bước sau cảnh rơi máy bay:
    ///   1. Kiểm tra Xác Máy Bay & Thu thập vật phẩm sinh tồn
    ///   2. Dựng trại / Đốt lửa trú ẩn & Sống sót qua Đêm 1
    ///   3. Khám phá Khu Làng Bỏ Hoang (tìm manh mối & thiết bị liên lạc)
    ///   4. Tiến tới Khu Bến Tàu / Cầu Gỗ (sẵn sàng gọi cứu hộ thoát đảo!)
    /// Nhấn [J] để ẩn/hiện bảng mục tiêu.
    /// </summary>
    public class SurvivalObjectiveManager : MonoBehaviour
    {
        public static SurvivalObjectiveManager Instance { get; private set; }

        [Header("── Tọa Độ Các Mốc Khám Phá Trên Đảo ──")]
        public Vector3 crashSitePos = new Vector3(537f, 17.6f, 545f);
        public Vector3 abandonedVillagePos = new Vector3(500f, 17.9f, 470f);
        public Vector3 escapeDockPos = new Vector3(522f, 17.6f, 548f);

        [Header("── Cài Đặt Hiển Thị ──")]
        public bool showHUD = true;
        public KeyCode toggleKey = KeyCode.J;

        // Chỉ số mục tiêu hiện tại: 0 -> 3 (4 = đã hoàn thành toàn bộ, sẵn sàng Ending)
        public int CurrentStage { get; private set; }
        public bool AllObjectivesCompleted { get { return CurrentStage >= 4; } }

        private Transform playerTransform;
        private PlayerStats playerStats;
        private int initialItemCount = -1;
        private float stage0Timer = 0f;
        private float checkTimer = 0f;

        // Thông báo toast khi hoàn thành mục tiêu
        private string toastTitle = "";
        private string toastSub = "";
        private float toastTimer = 0f;

        // Styles (khởi tạo 1 lần để không tốn GC)
        private GUIStyle boxStyle;
        private GUIStyle headerStyle;
        private GUIStyle titleStyle;
        private GUIStyle descStyle;
        private GUIStyle distStyle;
        private GUIStyle doneStyle;
        private GUIStyle hintStyle;
        private GUIStyle toastBgStyle;
        private GUIStyle toastTitleStyle;
        private GUIStyle toastSubStyle;
        private Texture2D darkTex;
        private Texture2D accentTex;

        private static bool sceneHookRegistered = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            if (!sceneHookRegistered)
            {
                sceneHookRegistered = true;
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnAnySceneLoaded;
            }
            EnsureInstanceForActiveScene();
        }

        private static void OnAnySceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            EnsureInstanceForActiveScene();
        }

        private static void EnsureInstanceForActiveScene()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName.ToLower().Contains("mainmenu")) return;

            if (FindObjectOfType<SurvivalObjectiveManager>() == null)
            {
                GameObject obj = new GameObject("[SurvivalObjectiveManager]");
                obj.AddComponent<SurvivalObjectiveManager>();
                Debug.Log("🧭 [ObjectiveTracker] Đã kích hoạt Bảng Mục Tiêu Sinh Tồn!");
            }
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            CurrentStage = 0;
        }

        private void Start()
        {
            FindPlayerRefs();

            // Tự động lấy tọa độ xác máy bay thật nếu có trong Scene
            GameObject airplane = GameObject.Find("[C400_AIRPLANE_CABIN]");
            if (airplane != null)
            {
                crashSitePos = airplane.transform.position;
            }
        }

        private void FindPlayerRefs()
        {
            if (playerTransform == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p == null)
                {
                    PlayerController pc = FindObjectOfType<PlayerController>();
                    if (pc != null) p = pc.gameObject;
                }
                if (p != null)
                {
                    playerTransform = p.transform;
                    playerStats = p.GetComponent<PlayerStats>();
                }
            }
        }

        private void Update()
        {
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;
            if (HorrorGame.Cutscenes.CinematicTrailerDirector.IsTrailerRunning) return;

            if (Input.GetKeyDown(toggleKey))
            {
                showHUD = !showHUD;
            }

            if (toastTimer > 0f) toastTimer -= Time.unscaledDeltaTime;

            FindPlayerRefs();
            if (playerTransform == null) return;

            checkTimer += Time.deltaTime;
            if (checkTimer < 0.35f) return;
            checkTimer = 0f;

            EvaluateObjectives();
        }

        private void EvaluateObjectives()
        {
            if (CurrentStage >= 4) return;

            switch (CurrentStage)
            {
                case 0:
                    // Mục tiêu 1: Khám phá xác máy bay & thu thập ít nhất 1 vật phẩm (hoặc đi khám phá xung quanh 25s)
                    stage0Timer += 0.35f;
                    int currentItems = CountInventoryItems();
                    if (initialItemCount < 0) initialItemCount = currentItems;

                    bool pickedSomething = currentItems > initialItemCount;
                    float distFromCrash = Vector2.Distance(
                        new Vector2(playerTransform.position.x, playerTransform.position.z),
                        new Vector2(crashSitePos.x, crashSitePos.z));

                    if (pickedSomething || (stage0Timer >= 25f && distFromCrash > 12f))
                    {
                        AdvanceStage(1, "Đã kiểm tra Xác Máy Bay!", "Tiếp theo: Dựng trại hoặc đốt lửa để sống sót qua Đêm 1.");
                    }
                    break;

                case 1:
                    // Mục tiêu 2: Dựng Lửa Trại / Chòi Trú Ẩn (hoặc đứng gần lửa / sống qua Ngày 2)
                    bool nearFire = (playerStats != null && playerStats.isNearFire);
                    bool builtShelter = (FindObjectOfType<ShelterRest>() != null && stage0Timer > 5f)
                                     || (FindObjectOfType<CookingStation>() != null && nearFire);
                    bool survivedDay1 = (DayNightCycle.Instance != null && DayNightCycle.Instance.currentDay >= 2);

                    if (nearFire || builtShelter || survivedDay1)
                    {
                        AdvanceStage(2, "Đã ổn định nơi trú ẩn!", "Tiếp theo: Khám phá Khu Làng Bỏ Hoang để tìm thiết bị liên lạc.");
                    }
                    break;

                case 2:
                    // Mục tiêu 3: Tiến tới Khu Làng Bỏ Hoang
                    float distVillage = Vector2.Distance(
                        new Vector2(playerTransform.position.x, playerTransform.position.z),
                        new Vector2(abandonedVillagePos.x, abandonedVillagePos.z));

                    if (distVillage <= 20f)
                    {
                        AdvanceStage(3, "Đã tới Khu Làng Bỏ Hoang!", "Tiếp theo: Tìm đường ra Bến Tàu / Cầu Gỗ phía Tây để chuẩn bị gọi cứu hộ.");
                    }
                    break;

                case 3:
                    // Mục tiêu 4: Tiến tới Khu Bến Tàu / Cầu Gỗ Thoát Hiểm
                    float distDock = Vector2.Distance(
                        new Vector2(playerTransform.position.x, playerTransform.position.z),
                        new Vector2(escapeDockPos.x, escapeDockPos.z));

                    if (distDock <= 22f)
                    {
                        AdvanceStage(4, "ĐÃ TỚI KHU BẾN TÀU THOÁT HIỂM!", "Bạn đã hoàn thành chặng sinh tồn chính — Sẵn sàng kích hoạt Cứu Hộ!");
                    }
                    break;
            }
        }

        private int CountInventoryItems()
        {
            if (InventoryManager.Instance == null || InventoryManager.Instance.slots == null) return 0;
            int total = 0;
            var slots = InventoryManager.Instance.slots;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null && !slots[i].IsEmpty())
                {
                    total += slots[i].amount;
                }
            }
            return total;
        }

        public void AdvanceStage(int nextStage, string completeTitle, string nextHint)
        {
            CurrentStage = nextStage;
            toastTitle = completeTitle;
            toastSub = nextHint;
            toastTimer = 5.0f;
            Debug.Log(string.Format("🧭 [ObjectiveTracker] {0} -> {1}", completeTitle, nextHint));
        }

        private void InitStyles()
        {
            if (darkTex == null)
            {
                darkTex = new Texture2D(1, 1);
                darkTex.SetPixel(0, 0, new Color(0.06f, 0.08f, 0.11f, 0.78f));
                darkTex.Apply();
            }
            if (accentTex == null)
            {
                accentTex = new Texture2D(1, 1);
                accentTex.SetPixel(0, 0, new Color(0.95f, 0.70f, 0.20f, 0.95f));
                accentTex.Apply();
            }

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle();
                boxStyle.normal.background = darkTex;
                boxStyle.padding = new RectOffset(12, 12, 10, 10);
            }
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.label);
                headerStyle.fontSize = 13;
                headerStyle.fontStyle = FontStyle.Bold;
                headerStyle.normal.textColor = new Color(1.0f, 0.78f, 0.28f);
            }
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.fontSize = 14;
                titleStyle.fontStyle = FontStyle.Bold;
                titleStyle.wordWrap = true;
                titleStyle.normal.textColor = Color.white;
            }
            if (descStyle == null)
            {
                descStyle = new GUIStyle(GUI.skin.label);
                descStyle.fontSize = 12;
                descStyle.wordWrap = true;
                descStyle.normal.textColor = new Color(0.82f, 0.86f, 0.90f);
            }
            if (distStyle == null)
            {
                distStyle = new GUIStyle(GUI.skin.label);
                distStyle.fontSize = 12;
                distStyle.fontStyle = FontStyle.Bold;
                distStyle.normal.textColor = new Color(0.42f, 0.92f, 0.62f);
            }
            if (doneStyle == null)
            {
                doneStyle = new GUIStyle(GUI.skin.label);
                doneStyle.fontSize = 11;
                doneStyle.normal.textColor = new Color(0.55f, 0.75f, 0.58f);
            }
            if (hintStyle == null)
            {
                hintStyle = new GUIStyle(GUI.skin.label);
                hintStyle.fontSize = 10;
                hintStyle.normal.textColor = new Color(0.60f, 0.65f, 0.70f);
            }
            if (toastBgStyle == null)
            {
                toastBgStyle = new GUIStyle();
                toastBgStyle.normal.background = darkTex;
            }
            if (toastTitleStyle == null)
            {
                toastTitleStyle = new GUIStyle(GUI.skin.label);
                toastTitleStyle.fontSize = 19;
                toastTitleStyle.fontStyle = FontStyle.Bold;
                toastTitleStyle.alignment = TextAnchor.MiddleCenter;
                toastTitleStyle.normal.textColor = new Color(0.45f, 1.0f, 0.55f);
            }
            if (toastSubStyle == null)
            {
                toastSubStyle = new GUIStyle(GUI.skin.label);
                toastSubStyle.fontSize = 13;
                toastSubStyle.alignment = TextAnchor.MiddleCenter;
                toastSubStyle.normal.textColor = new Color(0.95f, 0.95f, 0.95f);
            }
        }

        private void OnGUI()
        {
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;
            if (HorrorGame.Cutscenes.CinematicTrailerDirector.IsTrailerRunning) return;
            if (GameMenuManager.IsPaused) return;
            if (PlayerDeathUI.IsShowingDeathScreen) return;
            if (GameEndingManager.IsEndingUIActive) return;
            if (playerStats != null && playerStats.isDead) return;

            InitStyles();

            // 1. Vẽ Toast thông báo hoàn thành mục tiêu ở giữa trên màn hình
            if (toastTimer > 0f)
            {
                float alpha = Mathf.Clamp01(toastTimer < 1f ? toastTimer : 1f);
                Color prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);

                float tw = 520f;
                float th = 68f;
                float tx = (Screen.width - tw) * 0.5f;
                float ty = 78f;

                GUI.Box(new Rect(tx, ty, tw, th), "", toastBgStyle);
                GUI.DrawTexture(new Rect(tx, ty, tw, 3f), accentTex);
                GUI.Label(new Rect(tx + 10, ty + 8, tw - 20, 28), "✅ " + toastTitle, toastTitleStyle);
                GUI.Label(new Rect(tx + 10, ty + 36, tw - 20, 24), toastSub, toastSubStyle);
                GUI.color = prev;
            }

            if (!showHUD) return;

            // 2. Vẽ Bảng Mục Tiêu đồng bộ trực tiếp với Cốt Truyện 2 Ending (GameEndingManager)
            float w = 325f;
            float x = 14f;
            float y = 14f;
            float h = 152f;

            GUI.Box(new Rect(x, y, w, h), "", boxStyle);
            GUI.DrawTexture(new Rect(x, y, 4f, h), accentTex);

            string title = "";
            string desc = "";
            string navInfo = "";
            int stepDisplay = 1;

            GameEndingManager em = GameEndingManager.Instance;
            if (em != null && em.Stage == EndingStoryStage.FreeRoamAfterEnding2)
            {
                stepDisplay = 4;
                title = "★ NGƯỜI GÁC ĐẢO VĨNH CỬU";
                desc = "Bạn đã chọn ở lại bảo vệ thế giới. Tự do xây dựng và sinh tồn mãi mãi!";
                navInfo = "🌲 Chế độ Sinh Tồn Tự Do";
            }
            else if (em != null && em.Stage == EndingStoryStage.CarryingSurvivorToDock)
            {
                stepDisplay = 4;
                title = "4. Cõng Người Chạy Ra Bến Tàu!";
                desc = "Tại Bến Tàu ven sông: Bấm [E] lên Tàu = Thoát đảo (Ending 1) | Đập Máy Tín Hiệu = Ở lại (Ending 2).";
                navInfo = FormatNavHint("Bến Tàu Ven Sông", em.dockShorePos);
            }
            else if (em != null && em.Stage == EndingStoryStage.ReturnToLabAndCarry)
            {
                stepDisplay = 4;
                title = "4. Quay Lại Phòng Thí Nghiệm!";
                desc = "Mang ống Huyết Thanh quay lại Phòng Thí Nghiệm tiêm và cõng Người Sống Sót chạy ra Bến Tàu.";
                navInfo = FormatNavHint("Phòng Thí Nghiệm Bí Mật", em.secretLabPos);
            }
            else if (em != null && em.Stage == EndingStoryStage.PickupDroppedCure)
            {
                stepDisplay = 3;
                title = "🧪 Nhặt Ống Huyết Thanh Giải Độc!";
                desc = "Nữ Hoàng Ký Sinh đã rơi ra Ống Huyết Thanh phát sáng xanh trên mặt đất — hãy lại gần nhấn [E] nhặt!";
                navInfo = "★ Tìm ống thuốc phát sáng xanh ngay chỗ Boss gục";
            }
            else if (em != null && em.Stage == EndingStoryStage.BossFightActive)
            {
                stepDisplay = 3;
                title = "☠ HẠ GỤC NỮ HOÀNG KÝ SINH!";
                desc = "Tiêu diệt Trùm Cuối ngay ngoài Phòng Thí Nghiệm để nó rơi ra Ống Huyết Thanh Giải Độc!";
                navInfo = "⚔ Chiến đấu sinh tử!";
            }
            else if (em != null && em.Stage == EndingStoryStage.SearchSecretLab)
            {
                stepDisplay = 3;
                title = "3. Tìm Phòng Thí Nghiệm Bí Mật";
                desc = "Khám phá sâu trong rừng phía Tây Bắc để tìm căn hầm bê tông có ánh đèn huỳnh quang xanh mờ.";
                navInfo = "🔍 Không có định vị — Tự lần dấu trong rừng sâu!";
            }
            else if (CurrentStage == 0)
            {
                stepDisplay = 1;
                title = "1. Kiểm tra Xác Máy Bay & Nhặt đồ";
                desc = "Tìm quanh xác máy bay C400, nhấn [E] nhặt vật phẩm, [I] mở ba lô, [F] bật đèn pin.";
                navInfo = FormatNavHint("Xác máy bay", crashSitePos);
            }
            else
            {
                stepDisplay = 2;
                int day = DayNightCycle.Instance != null ? DayNightCycle.Instance.currentDay : 1;
                int reqDay = em != null ? em.requiredSurvivalDay : 2;
                title = string.Format("2. Sinh tồn tới Ngày {0} & Chuẩn bị", reqDay);
                desc = "Chặt cây, dựng Lửa Trại/Chòi ([B]), kiếm vũ khí sống qua đêm để mở khóa tín hiệu cầu cứu.";
                navInfo = string.Format("Tiến độ: Ngày {0}/{1} | Khi đủ ngày sẽ nhận tín hiệu ở Làng", day, reqDay);
            }

            GUI.Label(new Rect(x + 12, y + 6, w - 20, 20), string.Format("MỤC TIÊU CỐT TRUYỆN ({0}/4)", stepDisplay), headerStyle);
            GUI.Label(new Rect(x + 12, y + 28, w - 22, 22), title, titleStyle);
            GUI.Label(new Rect(x + 12, y + 50, w - 22, 44), desc, descStyle);
            GUI.Label(new Rect(x + 12, y + 98, w - 22, 22), navInfo, distStyle);
            GUI.Label(new Rect(x + 12, y + h - 20, w - 22, 18), "[J] Ẩn/Hiện bảng mục tiêu", hintStyle);
        }

        private string FormatNavHint(string targetName, Vector3 targetPos)
        {
            if (playerTransform == null) return "📍 " + targetName;
            Vector3 flatDiff = new Vector3(targetPos.x - playerTransform.position.x, 0f, targetPos.z - playerTransform.position.z);
            float dist = flatDiff.magnitude;
            if (dist < 1f) return string.Format("📍 {0}: Đã tới nơi!", targetName);

            Vector3 fwd = new Vector3(playerTransform.forward.x, 0f, playerTransform.forward.z).normalized;
            float angle = Vector3.SignedAngle(fwd, flatDiff.normalized, Vector3.up);
            string dir;
            if (Mathf.Abs(angle) <= 35f) dir = "↑ Phía trước";
            else if (Mathf.Abs(angle) >= 145f) dir = "↓ Phía sau";
            else if (angle > 0f) dir = "→ Bên phải";
            else dir = "← Bên trái";

            return string.Format("📍 {0}: {1:0}m ({2})", targetName, dist, dir);
        }
    }
}

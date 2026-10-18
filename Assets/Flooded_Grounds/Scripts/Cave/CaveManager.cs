using UnityEngine;
using System.Collections.Generic;

namespace HorrorGame.Cave
{
    /// <summary>
    /// CaveManager (Singleton) — Quản lý toàn bộ hệ thống hang động trên đảo.
    /// - Theo dõi trạng thái tất cả hang: đã khám phá, đã lấy loot, đã hạ boss
    /// - Quản lý transition vào/ra hang (ánh sáng, âm thanh, enemy spawning)
    /// - Lưu/Load tiến trình hang động qua PlayerPrefs
    /// - Cung cấp API cho các script khác kiểm tra trạng thái hang
    /// </summary>
    public class CaveManager : MonoBehaviour
    {
        public static CaveManager Instance;

        // ═══════════════════════════════════════════════════════════════
        // TRẠNG THÁI TOÀN CỤC
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Player có đang ở trong hang không?</summary>
        public static bool IsPlayerInCave { get; private set; }

        /// <summary>Hang hiện tại player đang ở trong (null nếu ngoài trời)</summary>
        public static CaveZone CurrentCave { get; private set; }

        [Header("── Danh Sách Hang Động ──")]
        [Tooltip("Kéo thả tất cả CaveZone trong Scene vào đây")]
        public List<CaveZone> allCaves = new List<CaveZone>();

        [Header("── Cài Đặt Chuyển Cảnh ──")]
        [Tooltip("Thời gian fade đen khi vào/ra hang (giây)")]
        public float transitionFadeTime = 1.5f;

        [Tooltip("Thời gian chờ trước khi bật ánh sáng hang (giây)")]
        public float lightingTransitionDelay = 0.5f;

        [Header("── Tham Chiếu ──")]
        [Tooltip("Directional Light (Mặt trời) — sẽ tắt khi vào hang")]
        public Light sunLight;

        [Tooltip("Directional Light (Mặt trăng) — sẽ tắt khi vào hang")]
        public Light moonLight;

        // Lưu trạng thái ánh sáng bên ngoài
        private float savedSunIntensity;
        private float savedMoonIntensity;
        private bool savedFogEnabled;
        private Color savedFogColor;
        private float savedFogDensity;
        private Color savedAmbientColor;
        private float savedAmbientIntensity;

        // Dữ liệu tiến trình mỗi hang
        private Dictionary<int, CaveProgressData> caveProgress = new Dictionary<int, CaveProgressData>();

        // Fade overlay
        private float fadeAlpha = 0f;
        private float fadeTarget = 0f;
        private float fadeSpeed = 1f;
        private bool isFading = false;
        private Texture2D fadeTexture;
        private System.Action onFadeComplete;

        // ═══════════════════════════════════════════════════════════════
        // KHỞI TẠO
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(this);
                return;
            }

            // Tạo texture đen cho fade
            fadeTexture = new Texture2D(1, 1);
            fadeTexture.SetPixel(0, 0, Color.black);
            fadeTexture.Apply();

            IsPlayerInCave = false;
            CurrentCave = null;
        }

        private void Start()
        {
            // Tự động tìm Sun và Moon nếu chưa gán
            if (sunLight == null)
            {
                var dayNight = FindObjectOfType<Environment.DayNightCycle>();
                if (dayNight != null)
                {
                    sunLight = dayNight.sunLight;
                    moonLight = dayNight.MoonLight;
                }
            }

            // Tự động tìm tất cả CaveZone nếu chưa gán
            if (allCaves.Count == 0)
            {
                CaveZone[] found = FindObjectsOfType<CaveZone>();
                allCaves.AddRange(found);
            }

            // Load tiến trình đã lưu
            LoadProgress();
        }

        // ═══════════════════════════════════════════════════════════════
        // VÀO / RA HANG
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Gọi khi player bước vào cửa hang
        /// </summary>
        public void EnterCave(CaveZone cave)
        {
            if (IsPlayerInCave) return;
            if (cave == null) return;

            Debug.Log(string.Format("[CaveManager] 🕳️ Đang vào {0}...", cave.caveName));

            // Fade đen → chuyển cảnh → fade sáng
            StartFade(1f, transitionFadeTime, () =>
            {
                // Lưu trạng thái bên ngoài
                SaveOutdoorLighting();

                // Đánh dấu trạng thái
                IsPlayerInCave = true;
                CurrentCave = cave;

                // Tắt ánh sáng ngoài trời
                DisableOutdoorLighting();

                // Kích hoạt hang
                cave.ActivateCave();

                // Đánh dấu đã khám phá
                MarkCaveDiscovered(cave.caveID);

                // Fade sáng trở lại (trong bóng tối hang)
                StartFade(0f, transitionFadeTime, null);
            });
        }

        /// <summary>
        /// Gọi khi player rời khỏi hang
        /// </summary>
        public void ExitCave()
        {
            if (!IsPlayerInCave || CurrentCave == null) return;

            CaveZone exitingCave = CurrentCave;
            Debug.Log(string.Format("[CaveManager] 🌤️ Đang rời {0}...", exitingCave.caveName));

            StartFade(1f, transitionFadeTime, () =>
            {
                // Tắt hang
                exitingCave.DeactivateCave();

                // Khôi phục ánh sáng ngoài trời
                RestoreOutdoorLighting();

                // Reset trạng thái
                IsPlayerInCave = false;
                CurrentCave = null;

                // Fade sáng trở lại
                StartFade(0f, transitionFadeTime, null);
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // QUẢN LÝ ÁNH SÁNG
        // ═══════════════════════════════════════════════════════════════

        private void SaveOutdoorLighting()
        {
            if (sunLight != null) savedSunIntensity = sunLight.intensity;
            if (moonLight != null) savedMoonIntensity = moonLight.intensity;
            savedFogEnabled = RenderSettings.fog;
            savedFogColor = RenderSettings.fogColor;
            savedFogDensity = RenderSettings.fogDensity;
            savedAmbientColor = RenderSettings.ambientLight;
            savedAmbientIntensity = RenderSettings.ambientIntensity;
        }

        private void DisableOutdoorLighting()
        {
            // Tắt mặt trời và mặt trăng
            if (sunLight != null) sunLight.intensity = 0f;
            if (moonLight != null) moonLight.intensity = 0f;

            // Đặt ambient tối nhất
            RenderSettings.ambientLight = new Color(0.02f, 0.02f, 0.03f);
            RenderSettings.ambientIntensity = 0.1f;

            // Fog đặc bên trong hang
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.01f, 0.01f, 0.02f);
            RenderSettings.fogDensity = 0.06f;
        }

        private void RestoreOutdoorLighting()
        {
            if (sunLight != null) sunLight.intensity = savedSunIntensity;
            if (moonLight != null) moonLight.intensity = savedMoonIntensity;
            RenderSettings.fog = savedFogEnabled;
            RenderSettings.fogColor = savedFogColor;
            RenderSettings.fogDensity = savedFogDensity;
            RenderSettings.ambientLight = savedAmbientColor;
            RenderSettings.ambientIntensity = savedAmbientIntensity;
        }

        // ═══════════════════════════════════════════════════════════════
        // TIẾN TRÌNH (PROGRESS)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Đánh dấu hang đã được khám phá (player đã vào lần đầu)</summary>
        public void MarkCaveDiscovered(int caveID)
        {
            if (!caveProgress.ContainsKey(caveID))
                caveProgress[caveID] = new CaveProgressData();
            caveProgress[caveID].isDiscovered = true;
            SaveProgress();
        }

        /// <summary>Đánh dấu hang đã hoàn thành (lấy hết loot chính)</summary>
        public void MarkCaveCompleted(int caveID)
        {
            if (!caveProgress.ContainsKey(caveID))
                caveProgress[caveID] = new CaveProgressData();
            caveProgress[caveID].isCompleted = true;
            SaveProgress();
        }

        /// <summary>Đánh dấu đã thu thập kho báu độc nhất của hang</summary>
        public void MarkCaveLootCollected(int caveID)
        {
            MarkCaveCompleted(caveID);
        }

        /// <summary>Đánh dấu boss trong hang đã bị hạ gục</summary>
        public void MarkBossDefeated(int caveID)
        {
            if (!caveProgress.ContainsKey(caveID))
                caveProgress[caveID] = new CaveProgressData();
            caveProgress[caveID].bossDefeated = true;
            SaveProgress();
        }

        /// <summary>Kiểm tra hang đã được khám phá chưa</summary>
        public bool IsCaveDiscovered(int caveID)
        {
            return caveProgress.ContainsKey(caveID) && caveProgress[caveID].isDiscovered;
        }

        /// <summary>Kiểm tra hang đã hoàn thành chưa</summary>
        public bool IsCaveCompleted(int caveID)
        {
            return caveProgress.ContainsKey(caveID) && caveProgress[caveID].isCompleted;
        }

        /// <summary>Kiểm tra boss trong hang đã bị tiêu diệt chưa</summary>
        public bool IsBossDefeated(int caveID)
        {
            return caveProgress.ContainsKey(caveID) && caveProgress[caveID].bossDefeated;
        }

        // ═══════════════════════════════════════════════════════════════
        // LƯU / LOAD
        // ═══════════════════════════════════════════════════════════════

        private void SaveProgress()
        {
            foreach (var kvp in caveProgress)
            {
                string prefix = "Cave_" + kvp.Key + "_";
                PlayerPrefs.SetInt(prefix + "discovered", kvp.Value.isDiscovered ? 1 : 0);
                PlayerPrefs.SetInt(prefix + "completed", kvp.Value.isCompleted ? 1 : 0);
                PlayerPrefs.SetInt(prefix + "bossDefeated", kvp.Value.bossDefeated ? 1 : 0);
            }
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            // Load cho 6 hang
            for (int i = 1; i <= 6; i++)
            {
                string prefix = "Cave_" + i + "_";
                if (PlayerPrefs.HasKey(prefix + "discovered"))
                {
                    CaveProgressData data = new CaveProgressData();
                    data.isDiscovered = PlayerPrefs.GetInt(prefix + "discovered") == 1;
                    data.isCompleted = PlayerPrefs.GetInt(prefix + "completed") == 1;
                    data.bossDefeated = PlayerPrefs.GetInt(prefix + "bossDefeated") == 1;
                    caveProgress[i] = data;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // FADE EFFECT
        // ═══════════════════════════════════════════════════════════════

        private void StartFade(float target, float duration, System.Action callback)
        {
            fadeTarget = target;
            fadeSpeed = 1f / Mathf.Max(duration, 0.01f);
            isFading = true;
            onFadeComplete = callback;
        }

        private void Update()
        {
            if (!isFading) return;

            if (fadeAlpha < fadeTarget)
            {
                fadeAlpha += Time.unscaledDeltaTime * fadeSpeed;
                if (fadeAlpha >= fadeTarget)
                {
                    fadeAlpha = fadeTarget;
                    isFading = false;
                    if (onFadeComplete != null)
                    {
                        System.Action cb = onFadeComplete;
                        onFadeComplete = null;
                        cb();
                    }
                }
            }
            else if (fadeAlpha > fadeTarget)
            {
                fadeAlpha -= Time.unscaledDeltaTime * fadeSpeed;
                if (fadeAlpha <= fadeTarget)
                {
                    fadeAlpha = fadeTarget;
                    isFading = false;
                    if (onFadeComplete != null)
                    {
                        System.Action cb = onFadeComplete;
                        onFadeComplete = null;
                        cb();
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (fadeAlpha <= 0f) return;

            Color prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeTexture);
            GUI.color = prev;
        }

        // ═══════════════════════════════════════════════════════════════
        // TIỆN ÍCH
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Lấy CaveZone theo ID</summary>
        public CaveZone GetCaveByID(int id)
        {
            for (int i = 0; i < allCaves.Count; i++)
            {
                if (allCaves[i] != null && allCaves[i].caveID == id)
                    return allCaves[i];
            }
            return null;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                IsPlayerInCave = false;
                CurrentCave = null;
            }
        }

        /// <summary>Debug: Hiển thị trạng thái hang trong Scene view</summary>
        private void OnDrawGizmos()
        {
            if (allCaves == null) return;
            for (int i = 0; i < allCaves.Count; i++)
            {
                if (allCaves[i] == null) continue;
                Gizmos.color = IsCaveDiscovered(allCaves[i].caveID) ? Color.green : Color.gray;
                Gizmos.DrawWireSphere(allCaves[i].transform.position, 5f);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureCaveSystemRuntime()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu") return;

            CaveManager mgr = FindObjectOfType<CaveManager>();
            if (mgr == null)
            {
                GameObject mgrObj = new GameObject("[CaveSystem_Manager]");
                mgr = mgrObj.AddComponent<CaveManager>();
                mgrObj.AddComponent<CaveMinimap>();
            }

            // Đảm bảo toàn bộ 6 Hang Động và Cửa hang tồn tại trên hòn đảo
            if (GameObject.Find("[Cave_Zones]") == null && FindObjectOfType<CaveZone>() == null)
            {
                GameObject cavesRoot = new GameObject("[Cave_Zones]");

                var presets = TheForestCaveEntrance.DefaultCavePresets;
                string[] treasures = new string[]
                {
                    "Bản Vẽ Rìu Đá Nâng Cấp (Upgraded Axe Blueprint)",
                    "Đèn Pin Thợ Lặn Chống Nước (Diver Flashlight)",
                    "Bình Dưỡng Khí Lặn Biển Rebreather (Cổ Vật)",
                    "Cung Tên Thợ Săn Thổ Dân & Mũi Tên Độc (Cannibal Hunter Bow)",
                    "Bản Vẽ Rèn Kiếm Nhật Katana Rỉ Sét (Ancient Forge Blueprint)",
                    "Huyết Thanh Kháng Ký Sinh Tối Thượng (Cure Vaccine)"
                };

                Color[] lightColors = new Color[]
                {
                    new Color(0.2f, 0.9f, 0.4f),  // Hang 1: Xanh nấm phát quang
                    new Color(0.9f, 0.7f, 0.3f),  // Hang 2: Vàng rễ cây
                    new Color(0.2f, 0.6f, 0.95f), // Hang 3: Xanh biển
                    new Color(0.95f, 0.35f, 0.1f),// Hang 4: Đỏ lửa đuốc
                    new Color(1.0f, 0.3f, 0.1f),  // Hang 5: Đỏ dung nham
                    new Color(0.3f, 0.95f, 0.5f)  // Hang 6: Xanh ký sinh
                };

                for (int i = 0; i < presets.Length; i++)
                {
                    var p = presets[i];
                    Vector3 cavePos = p.pos;
                    if (Terrain.activeTerrain != null)
                    {
                        cavePos.y = Mathf.Max(Terrain.activeTerrain.SampleHeight(cavePos) + Terrain.activeTerrain.transform.position.y, 16.2f);
                    }

                    GameObject caveObj = new GameObject("Cave" + p.id + "_" + p.name.Split(' ')[1]);
                    caveObj.transform.SetParent(cavesRoot.transform);
                    caveObj.transform.position = cavePos;

                    CaveZone zone = caveObj.AddComponent<CaveZone>();
                    zone.caveID = p.id;
                    zone.caveName = p.name;
                    zone.caveDescription = p.desc;
                    zone.difficulty = Mathf.Min(i + 1, 5);

                    // Cửa hang 3D The Forest
                    GameObject forestObj = new GameObject("TheForest_CaveEntrance_3D");
                    forestObj.transform.SetParent(caveObj.transform);
                    forestObj.transform.localPosition = Vector3.zero;
                    TheForestCaveEntrance forestCave = forestObj.AddComponent<TheForestCaveEntrance>();
                    forestCave.caveID = p.id;
                    forestCave.caveName = p.name;
                    forestCave.entranceWorldPosition = cavePos;
                    forestCave.ropeColor = p.ropeColor;
                    forestCave.BuildForestCaveEntranceModel();

                    // Ánh sáng nội thất riêng cho từng hang
                    GameObject lightsObj = new GameObject("Cave_LightingSystem");
                    lightsObj.transform.SetParent(caveObj.transform);
                    lightsObj.transform.localPosition = Vector3.zero;
                    CaveLighting lighting = lightsObj.AddComponent<CaveLighting>();
                    lighting.lightType = (i == 4) ? CaveLightType.LavaGlow : CaveLightType.Bioluminescent;
                    zone.caveLights = lightsObj;

                    GameObject pointLightObj = new GameObject("Interior_AmbienceLight");
                    pointLightObj.transform.SetParent(lightsObj.transform);
                    pointLightObj.transform.localPosition = new Vector3(0f, 2f, 20f);
                    Light pl = pointLightObj.AddComponent<Light>();
                    pl.type = LightType.Point;
                    pl.color = lightColors[i];
                    pl.range = 16f;
                    pl.intensity = 1.3f;

                    // Rương báu độc quyền
                    GameObject chestObj = new GameObject("CaveLootChest_" + p.id);
                    chestObj.transform.SetParent(caveObj.transform);
                    chestObj.transform.localPosition = new Vector3(2f, 0f, 30f);
                    chestObj.AddComponent<BoxCollider>().size = new Vector3(1.5f, 1f, 1.5f);
                    CaveLootChest chest = chestObj.AddComponent<CaveLootChest>();
                    chest.caveID = p.id;
                    chest.uniqueTreasureName = treasures[i];
                    zone.lootChests = new GameObject[] { chestObj };

                    // Tính năng riêng: Nước ngập cho Hang 3
                    if (p.id == 3 && caveObj.GetComponent<CaveWaterLevel>() == null)
                    {
                        caveObj.AddComponent<CaveWaterLevel>();
                    }
                    // Lò rèn cho Hang 5
                    if (p.id == 5)
                    {
                        GameObject forgeObj = new GameObject("AncientForge");
                        forgeObj.transform.SetParent(caveObj.transform);
                        forgeObj.transform.localPosition = new Vector3(0, 0, 15f);
                        forgeObj.AddComponent<CaveCraftingStation>();
                    }

                    mgr.allCaves.Add(zone);
                }

                Debug.Log("🎉 [CaveManager] Đã tự động tạo hoàn chỉnh toàn bộ 6 HANG ĐỘNG THE FOREST trên hòn đảo!");
            }
        }
    }

    /// <summary>Dữ liệu tiến trình của mỗi hang</summary>
    [System.Serializable]
    public class CaveProgressData
    {
        public bool isDiscovered = false;
        public bool isCompleted = false;
        public bool bossDefeated = false;
    }
}

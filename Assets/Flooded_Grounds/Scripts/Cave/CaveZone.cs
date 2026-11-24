using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace HorrorGame.Cave
{
    /// <summary>
    /// CaveZone — Controller riêng cho MỖI hang động.
    /// Quản lý toàn bộ nội dung bên trong 1 hang:
    /// - Bật/tắt đối tượng trong hang khi player vào/ra
    /// - Quản lý nguồn sáng riêng (nấm phát quang, đuốc, nham thạch)
    /// - Spawn/despawn kẻ thù
    /// - Theo dõi loot đã nhặt
    /// - Kích hoạt events/traps
    /// 
    /// Mỗi hang trong Scene là 1 GameObject gốc chứa CaveZone component.
    /// Tất cả mesh, light, enemy, loot đều là con của GameObject này.
    /// </summary>
    public class CaveZone : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // THÔNG TIN HANG
        // ═══════════════════════════════════════════════════════════════

        [Header("── Thông Tin Hang Động ──")]
        [Tooltip("ID duy nhất của hang (1-6)")]
        public int caveID = 1;

        [Tooltip("Tên hiển thị của hang")]
        public string caveName = "Hang Sụp Đổ";

        [Tooltip("Mô tả ngắn về hang")]
        [TextArea(2, 4)]
        public string caveDescription = "Hang động nhỏ gần xác máy bay. Nơi bắt đầu hành trình khám phá lòng đất.";

        [Tooltip("Độ khó (1-5 sao)")]
        [Range(1, 5)]
        public int difficulty = 1;

        // ═══════════════════════════════════════════════════════════════
        // ĐỐI TƯỢNG CON TRONG HANG
        // ═══════════════════════════════════════════════════════════════

        [Header("── Cấu Trúc Hang ──")]
        [Tooltip("GameObject gốc chứa toàn bộ mesh hang (tường, trần, sàn)")]
        public GameObject caveGeometry;

        [Tooltip("GameObject chứa tất cả nguồn sáng trong hang (nấm, đuốc, nham thạch)")]
        public GameObject caveLights;

        [Tooltip("GameObject chứa tất cả particle effects (bụi, sương, bào tử)")]
        public GameObject caveParticles;

        [Tooltip("GameObject chứa tất cả props trang trí (xương, tranh tường, cột đá)")]
        public GameObject caveProps;

        [Header("── Kẻ Thù ──")]
        [Tooltip("Spawn points cho kẻ thù trong hang")]
        public Transform[] enemySpawnPoints;

        [Tooltip("Prefab kẻ thù sẽ spawn trong hang này")]
        public GameObject[] enemyPrefabs;

        [Tooltip("Số lượng tối đa kẻ thù cùng lúc trong hang")]
        public int maxEnemies = 5;

        [Header("── Loot & Tương Tác ──")]
        [Tooltip("Các LootChest trong hang")]
        public GameObject[] lootChests;

        [Tooltip("Các LoreNote trong hang")]
        public GameObject[] loreNotes;

        [Tooltip("Các PickupItem trong hang")]
        public GameObject[] pickupItems;

        [Header("── Cơ Chế Đặc Biệt ──")]
        [Tooltip("Hang này có yêu cầu item mới vào được không?")]
        public bool requiresItem = false;

        [Tooltip("Tên item cần thiết (nếu requiresItem = true)")]
        public string requiredItemName = "";

        [Tooltip("Hang này chỉ vào được vào ban ngày? (Hang 3)")]
        public bool dayTimeOnly = false;

        [Tooltip("Khoảng giờ cho phép vào (nếu dayTimeOnly)")]
        public float dayTimeStart = 6f;
        public float dayTimeEnd = 18f;

        [Header("── Sanity & Environment ──")]
        [Tooltip("Hệ số nhân Sanity drain khi ở trong hang (1 = bình thường, 3 = gấp 3)")]
        public float sanityDrainMultiplier = 1f;

        [Tooltip("Có gây sát thương nhiệt không? (Hang 5)")]
        public bool hasHeatDamage = false;

        [Tooltip("Sát thương nhiệt mỗi giây")]
        public float heatDamagePerSecond = 2f;

        [Tooltip("Item có thể chống nhiệt")]
        public string heatProtectionItem = "CrudeOxygenMask";

        [Header("── Lối Đi Bí Mật ──")]
        [Tooltip("Có lối đi bí mật nối tới hang khác không?")]
        public bool hasSecretExit = false;

        [Tooltip("CaveZone của hang được nối tới")]
        public CaveZone connectedCave;

        [Tooltip("Vị trí cửa bí mật")]
        public Transform secretExitPoint;

        // ═══════════════════════════════════════════════════════════════
        // TRẠNG THÁI RUNTIME
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Hang có đang active không?</summary>
        public bool IsActive { get; private set; }

        private List<GameObject> spawnedEnemies = new List<GameObject>();
        private float heatDamageTimer = 0f;
        private bool isFirstVisit = true;
        private GUIStyle discoveryStyle;
        private float discoveryNoticeTimer = 0f;
        private string discoveryText = "";

        // ═══════════════════════════════════════════════════════════════
        // KHỞI TẠO
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            // Ban đầu tắt hết nội dung hang (để tiết kiệm performance)
            SetCaveContentActive(false);
            IsActive = false;
        }

        // ═══════════════════════════════════════════════════════════════
        // KÍCH HOẠT / VÔ HIỆU HÓA HANG
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Bật toàn bộ nội dung hang khi player vào
        /// </summary>
        public void ActivateCave()
        {
            IsActive = true;
            SetCaveContentActive(true);

            // Kiểm tra lần đầu vào → hiện thông báo khám phá
            if (CaveManager.Instance != null && !CaveManager.Instance.IsCaveDiscovered(caveID))
            {
                isFirstVisit = true;
                discoveryText = string.Format("🕳️ Đã Khám Phá: {0}", caveName);
                discoveryNoticeTimer = 5f;
            }
            else
            {
                isFirstVisit = false;
            }

            // Spawn kẻ thù
            SpawnEnemies();

            Debug.Log(string.Format("[CaveZone] ✅ Hang '{0}' đã được kích hoạt. Enemy: {1}", caveName, spawnedEnemies.Count));
        }

        /// <summary>
        /// Tắt toàn bộ nội dung hang khi player rời đi
        /// </summary>
        public void DeactivateCave()
        {
            IsActive = false;

            // Hủy tất cả kẻ thù đã spawn
            DespawnAllEnemies();

            // Tắt nội dung hang
            SetCaveContentActive(false);

            Debug.Log(string.Format("[CaveZone] ❌ Hang '{0}' đã được tắt.", caveName));
        }

        private void SetCaveContentActive(bool active)
        {
            if (caveGeometry != null) caveGeometry.SetActive(active);
            if (caveLights != null) caveLights.SetActive(active);
            if (caveParticles != null) caveParticles.SetActive(active);
            if (caveProps != null) caveProps.SetActive(active);

            // Loot, notes, items luôn active nếu chưa nhặt
            if (active)
            {
                SetArrayActive(lootChests, true);
                SetArrayActive(loreNotes, true);
                SetArrayActive(pickupItems, true);
            }
        }

        private void SetArrayActive(GameObject[] arr, bool active)
        {
            if (arr == null) return;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] != null) arr[i].SetActive(active);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // QUẢN LÝ KẺ THÙ
        // ═══════════════════════════════════════════════════════════════

        private void SpawnEnemies()
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;
            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0) return;

            int spawnCount = Mathf.Min(maxEnemies, enemySpawnPoints.Length);
            for (int i = 0; i < spawnCount; i++)
            {
                if (enemySpawnPoints[i] == null) continue;

                // Chọn random prefab từ danh sách
                GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                if (prefab == null) continue;

                GameObject enemy = Instantiate(prefab, enemySpawnPoints[i].position,
                    enemySpawnPoints[i].rotation);
                enemy.name = string.Format("{0}_CaveEnemy_{1}", caveName, i);
                spawnedEnemies.Add(enemy);
            }
        }

        private void DespawnAllEnemies()
        {
            for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
            {
                if (spawnedEnemies[i] != null)
                    Destroy(spawnedEnemies[i]);
            }
            spawnedEnemies.Clear();
        }

        /// <summary>Số kẻ thù còn sống trong hang</summary>
        public int GetAliveEnemyCount()
        {
            int count = 0;
            for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
            {
                if (spawnedEnemies[i] == null)
                {
                    spawnedEnemies.RemoveAt(i);
                    continue;
                }
                var ai = spawnedEnemies[i].GetComponent<Enemy.EnemyAI>();
                if (ai != null && !ai.IsDead) count++;
            }
            return count;
        }

        // ═══════════════════════════════════════════════════════════════
        // UPDATE — HAZARDS & SANITY
        // ═══════════════════════════════════════════════════════════════

        private void Update()
        {
            if (!IsActive) return;
            if (discoveryNoticeTimer > 0f) discoveryNoticeTimer -= Time.deltaTime;

            ApplyCaveEffects();
        }

        private void ApplyCaveEffects()
        {
            var stats = Player.PlayerStats.Instance;
            if (stats == null || stats.isDead) return;

            // Sanity drain nhân hệ số hang
            if (sanityDrainMultiplier > 1f)
            {
                float extraDrain = 0.08f * (sanityDrainMultiplier - 1f) * Time.deltaTime;
                stats.DrainSanity(extraDrain);
            }

            // Heat damage (Hang 5)
            if (hasHeatDamage)
            {
                // Kiểm tra player có item chống nhiệt không
                bool hasProtection = HasHeatProtection();
                if (!hasProtection)
                {
                    heatDamageTimer += Time.deltaTime;
                    if (heatDamageTimer >= 1f)
                    {
                        stats.TakeDamage(heatDamagePerSecond);
                        heatDamageTimer = 0f;
                    }
                }
            }
        }

        private bool HasHeatProtection()
        {
            if (string.IsNullOrEmpty(heatProtectionItem)) return false;

            // Kiểm tra trong inventory của player
            var inv = Inventory.InventoryManager.Instance;
            if (inv != null)
            {
                return inv.HasItem(heatProtectionItem);
            }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════
        // KIỂM TRA ĐIỀU KIỆN VÀO HANG
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Kiểm tra player có đủ điều kiện vào hang không
        /// </summary>
        public bool CanEnter(out string reason)
        {
            reason = "";

            // Kiểm tra thời gian (Hang 3 — chỉ ban ngày)
            if (dayTimeOnly)
            {
                var dayNight = Environment.DayNightCycle.Instance;
                if (dayNight != null)
                {
                    float hour = dayNight.currentHour;
                    if (hour < dayTimeStart || hour > dayTimeEnd)
                    {
                        reason = string.Format("Cửa hang bị ngập nước! Chỉ vào được từ {0}h đến {1}h.",
                            (int)dayTimeStart, (int)dayTimeEnd);
                        return false;
                    }
                }
            }

            // Kiểm tra item yêu cầu
            if (requiresItem && !string.IsNullOrEmpty(requiredItemName))
            {
                var inv = Inventory.InventoryManager.Instance;
                if (inv == null || !inv.HasItem(requiredItemName))
                {
                    reason = string.Format("Cần {0} để vào hang này!", requiredItemName);
                    return false;
                }
            }

            return true;
        }

        // ═══════════════════════════════════════════════════════════════
        // UI — THÔNG BÁO KHÁM PHÁ
        // ═══════════════════════════════════════════════════════════════

        private void OnGUI()
        {
            if (discoveryNoticeTimer <= 0f) return;

            if (discoveryStyle == null)
            {
                discoveryStyle = new GUIStyle(GUI.skin.label);
                discoveryStyle.fontSize = 28;
                discoveryStyle.fontStyle = FontStyle.Bold;
                discoveryStyle.alignment = TextAnchor.MiddleCenter;
                discoveryStyle.normal.textColor = new Color(1f, 0.85f, 0.3f); // Vàng kim
            }

            // Fade out effect
            float alpha = Mathf.Clamp01(discoveryNoticeTimer / 2f);
            Color c = discoveryStyle.normal.textColor;
            c.a = alpha;
            discoveryStyle.normal.textColor = c;

            float w = 600f;
            float h = 50f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height * 0.25f;

            // Shadow
            GUIStyle shadow = new GUIStyle(discoveryStyle);
            shadow.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.8f);
            GUI.Label(new Rect(x + 2, y + 2, w, h), discoveryText, shadow);

            // Main text
            GUI.Label(new Rect(x, y, w, h), discoveryText, discoveryStyle);

            // Sub text (mô tả)
            GUIStyle subStyle = new GUIStyle(GUI.skin.label);
            subStyle.fontSize = 16;
            subStyle.alignment = TextAnchor.MiddleCenter;
            subStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, alpha * 0.7f);
            subStyle.fontStyle = FontStyle.Italic;

            string subText = string.Format("Độ khó: {0}  |  {1}", new string('★', difficulty), caveDescription);
            GUI.Label(new Rect(x, y + h, w, 30f), subText, subStyle);
        }

        // ═══════════════════════════════════════════════════════════════
        // GIZMOS
        // ═══════════════════════════════════════════════════════════════

        private void OnDrawGizmos()
        {
            // Hiển thị vị trí spawn points trong Scene
            if (enemySpawnPoints != null)
            {
                Gizmos.color = Color.red;
                for (int i = 0; i < enemySpawnPoints.Length; i++)
                {
                    if (enemySpawnPoints[i] != null)
                    {
                        Gizmos.DrawSphere(enemySpawnPoints[i].position, 0.5f);
                        Gizmos.DrawLine(enemySpawnPoints[i].position,
                            enemySpawnPoints[i].position + Vector3.up * 2f);
                    }
                }
            }

            // Secret exit
            if (hasSecretExit && secretExitPoint != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(secretExitPoint.position, 1.5f);
                if (connectedCave != null)
                {
                    Gizmos.DrawLine(secretExitPoint.position, connectedCave.transform.position);
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f,
                string.Format("Hang {0}: {1}\nĐộ khó: {2}", caveID, caveName, new string('★', difficulty)));
        }
#endif
    }
}

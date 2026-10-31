using UnityEngine;
using System.Collections.Generic;
using HorrorGame.Enemy;
using HorrorGame.Environment;

namespace HorrorGame.Cave
{
    /// <summary>
    /// TheForestMineCavern — Hệ thống Hang Động & Hầm Mỏ Ngầm Đa Tầng Chuẩn The Forest
    /// Được xây dựng hoàn toàn từ Modular Mine Prefabs (Gregory Seguru):
    /// 1. Tầng hầm ngầm sâu ở Y = -60m (cách biệt hoàn toàn với mặt nước biển ngoài đảo Y=16.5m).
    /// 2. Giếng thang lối vào (Entrance Shaft) với Thang gỗ (Ladder), Nắp hầm (Hatch),
    ///    và Trigger tương tác [E] Trèo thang lên mặt đất (giải quyết triệt để vấn đề "lên kiểu gì").
    /// 3. Mạng lưới hầm mỏ sâu và rộng ("sâu và rộng như The Forest"):
    ///    - Dốc lao sâu xuống lòng đất (RampDown).
    ///    - Hành lang vách đá chống gỗ thông (Support1, Support2).
    ///    - Ngã ba phân nhánh (Turn3):
    ///      + Nhánh A: Hốc báu vật bí mật chứa Rương Cổ Vật (CaveLootChest) & Nhật ký Lore (LoreNote).
    ///      + Nhánh B: Sào huyệt quái vật (CaveEnemySpawner) & Tàn tích hành khách sống sót.
    /// 4. Ánh sáng đèn bão (Lantern) lửa lập lòe, quặng đá phát quang (OreBlue, OreGreen, OreRed).
    /// </summary>
    public class TheForestMineCavern : MonoBehaviour
    {
        [Header("── Cấu Hình Hang Mỏ ──")]
        public int caveID = 1;
        public string caveName = "Hang Mỏ Ngầm";
        public Vector3 entranceSurfacePos;
        public Color themeColor = new Color(0.95f, 0.85f, 0.12f);

        [Header("── Độ Sâu Của Mỏ ──")]
        [Tooltip("Độ sâu trục Y của hầm mỏ dưới lòng đất (mặc định -60m để tránh xa nước biển 16.5m)")]
        public float subterraneanDepthY = -60f;

        [Header("── Điểm Spawn & Lối Thoát ──")]
        public Transform interiorSpawnPoint;
        public Transform exitLadderTrigger;
        public CaveEntrance exitEntranceComponent;

        [Header("── Thành Phần Nội Thất ──")]
        public CaveZone parentZone;
        public CaveLootChest lootChest;
        public LoreNote loreNote;
        public CaveEnemySpawner enemySpawner;

        private GameObject cavernRoot;

        private void Awake()
        {
            if (parentZone == null) parentZone = GetComponentInParent<CaveZone>() ?? GetComponent<CaveZone>();
        }

        /// <summary>
        /// Dựng toàn bộ cấu trúc 3D Hầm Mỏ Ngầm bằng Modular Mine Prefabs
        /// </summary>
        public void BuildMineCavern(string treasureName = "", string loreTitle = "", string loreAuthor = "", string loreContent = "")
        {
            // Xóa hầm mỏ cũ nếu có
            Transform existing = transform.Find("TheForest_MineCavern_3D");
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }

            cavernRoot = new GameObject("TheForest_MineCavern_3D");
            cavernRoot.transform.SetParent(transform);

            // Đặt hầm mỏ ở độ sâu -60m ngay bên dưới vị trí cửa hang trên đảo
            Vector3 worldMinePos = new Vector3(entranceSurfacePos.x, subterraneanDepthY, entranceSurfacePos.z);
            cavernRoot.transform.position = worldMinePos;
            cavernRoot.transform.rotation = Quaternion.identity;

            if (parentZone != null)
            {
                parentZone.caveGeometry = cavernRoot;
            }

            // 1. TẢI CÁC PREFAB TỪ MINE ASSET
            GameObject pTunnel1 = LoadMinePrefab("Base", "Tunnel1");
            GameObject pTunnel2 = LoadMinePrefab("Base", "Tunnel2");
            GameObject pTunnel3 = LoadMinePrefab("Base", "Tunnel3");
            GameObject pTurnL = LoadMinePrefab("Base", "TurnL");
            GameObject pTurnR = LoadMinePrefab("Base", "TurnR");
            GameObject pTurn3 = LoadMinePrefab("Base", "Turn3");
            GameObject pRampDown = LoadMinePrefab("Base", "RampDown");

            GameObject pLadder = LoadMinePrefab("Props", "Ladder");
            GameObject pHatch = LoadMinePrefab("Props", "Hatch");
            GameObject pLantern = LoadMinePrefab("Props", "Lantern");
            GameObject pSupport1 = LoadMinePrefab("Props", "Support1");
            GameObject pSupport2 = LoadMinePrefab("Props", "Support2");
            GameObject pCrate = LoadMinePrefab("Props", "Crate");
            GameObject pPickaxe = LoadMinePrefab("Props", "Pickaxe");
            GameObject pPlanks = LoadMinePrefab("Props", "Planks");

            GameObject pRock1 = LoadMinePrefab("Rocks", "Rock1");
            GameObject pRock2 = LoadMinePrefab("Rocks", "Rock2");
            GameObject pOre = (caveID == 3) ? LoadMinePrefab("Rocks", "OreBlue")
                           : (caveID == 6) ? LoadMinePrefab("Rocks", "OreGreen")
                           : (caveID == 5) ? LoadMinePrefab("Rocks", "OreRed")
                           : LoadMinePrefab("Rocks", "OreBlue");

            // ═══════════════════════════════════════════════════════════════
            // 2. KHU VỰC 1: GIẾNG THANG LỐI VÀO (ENTRANCE SHAFT ROOM)
            // ═══════════════════════════════════════════════════════════════
            GameObject shaftRoom = SpawnPiece(cavernRoot, pTunnel1, Vector3.zero, Vector3.zero, "Shaft_EntranceRoom");

            // Nắp hầm ở trần hang
            SpawnPiece(shaftRoom, pHatch, new Vector3(0f, 3.8f, 0f), Vector3.zero, "Ceiling_Hatch");

            // Thang gỗ cao dựng từ sàn lên nắp hầm trần
            SpawnPiece(shaftRoom, pLadder, new Vector3(0.9f, 0f, 0f), new Vector3(0f, -90f, 0f), "Climb_Ladder");
            // Thêm đoạn thang thứ 2 nối dài lên trần
            SpawnPiece(shaftRoom, pLadder, new Vector3(0.9f, 2.0f, 0f), new Vector3(0f, -90f, 0f), "Climb_Ladder_Upper");

            // Khung gỗ gia cố & Đèn bão chiếu sáng
            SpawnPiece(shaftRoom, pSupport1, Vector3.zero, Vector3.zero, "Shaft_TimberSupport");
            SpawnPiece(shaftRoom, pLantern, new Vector3(-1.25f, 2.2f, 0.2f), Vector3.zero, "Shaft_Lantern");

            // Điểm Spawn khi người chơi từ trên mặt đất trèo xuống
            GameObject spawnObj = new GameObject("Mine_InteriorSpawnPoint");
            spawnObj.transform.SetParent(shaftRoom.transform);
            spawnObj.transform.localPosition = new Vector3(0f, 0.1f, 0.5f);
            spawnObj.transform.localRotation = Quaternion.identity;
            interiorSpawnPoint = spawnObj.transform;

            // TRIGGER TRÈO THANG LÊN MẶT ĐẤT (CLIMB LADDER TO SURFACE)
            GameObject exitTriggerObj = new GameObject("ExitLadder_InteractiveTrigger");
            exitTriggerObj.transform.SetParent(shaftRoom.transform);
            exitTriggerObj.transform.localPosition = new Vector3(0.85f, 1.0f, 0f);

            BoxCollider exitCol = exitTriggerObj.AddComponent<BoxCollider>();
            exitCol.isTrigger = true;
            exitCol.size = new Vector3(2.5f, 3.0f, 2.5f);

            exitEntranceComponent = exitTriggerObj.AddComponent<CaveEntrance>();
            exitEntranceComponent.entranceType = EntranceType.Exit;
            exitEntranceComponent.promptExit = string.Format("Nhấn [E] để trèo thang lên mặt đất ({0})", caveName);
            exitEntranceComponent.targetCave = parentZone;
            exitLadderTrigger = exitTriggerObj.transform;

            // ═══════════════════════════════════════════════════════════════
            // 3. KHU VỰC 2: HÀNH LANG VÁCH ĐÁ & DỐC LAO SÂU (DESCENT SLOPE)
            // ═══════════════════════════════════════════════════════════════
            // Đoạn hầm 1: Tiến về phía trước (+Z)
            SpawnPiece(cavernRoot, pTunnel2, new Vector3(0f, 0f, 6f), Vector3.zero, "Tunnel_Straight_1");
            SpawnPiece(cavernRoot, pSupport2, new Vector3(0f, 0f, 6f), Vector3.zero, "Support_1");
            SpawnPiece(cavernRoot, pCrate, new Vector3(-1.2f, 0f, 5.0f), new Vector3(0f, 25f, 0f), "Crate_1");
            SpawnPiece(cavernRoot, pPickaxe, new Vector3(1.1f, 0.1f, 5.5f), new Vector3(0f, 80f, 90f), "Pickaxe_1");

            // Đoạn dốc lao sâu xuống lòng đất (RampDown đưa xuống Y = -2m)
            SpawnPiece(cavernRoot, pRampDown, new Vector3(0f, 0f, 12f), Vector3.zero, "Slope_RampDown_1");
            SpawnPiece(cavernRoot, pLantern, new Vector3(-1.25f, 0.5f, 14f), Vector3.zero, "Slope_Lantern");

            // Đoạn hầm ngang sau dốc (Y = -2m)
            SpawnPiece(cavernRoot, pTunnel3, new Vector3(0f, -2f, 18f), Vector3.zero, "Tunnel_Deep_1");
            SpawnPiece(cavernRoot, pSupport1, new Vector3(0f, -2f, 18f), Vector3.zero, "Support_2");
            if (pOre != null) SpawnPiece(cavernRoot, pOre, new Vector3(1.4f, -1.0f, 19f), new Vector3(0f, -90f, 0f), "Wall_GlowingOre_1");

            // Khúc cua 90 độ rẽ trái (TurnL) sang hướng -X
            SpawnPiece(cavernRoot, pTurnL, new Vector3(0f, -2f, 24f), Vector3.zero, "TurnL_Junction");

            // ═══════════════════════════════════════════════════════════════
            // 4. KHU VỰC 3: NGÃ BA RẼ NHÁNH ĐA HƯỚNG (TURN3 T-JUNCTION)
            // ═══════════════════════════════════════════════════════════════
            // Đoạn nối từ TurnL sang Ngã ba
            SpawnPiece(cavernRoot, pTunnel1, new Vector3(-6f, -2f, 24f), new Vector3(0f, -90f, 0f), "Tunnel_Crossway");
            SpawnPiece(cavernRoot, pSupport2, new Vector3(-6f, -2f, 24f), new Vector3(0f, -90f, 0f), "Support_Crossway");
            SpawnPiece(cavernRoot, pPlanks, new Vector3(-6f, -1.95f, 24f), new Vector3(0f, 45f, 0f), "Floor_Planks");

            // Ngã ba T-Junction tại (-12, -2, 24)
            SpawnPiece(cavernRoot, pTurn3, new Vector3(-12f, -2f, 24f), new Vector3(0f, 90f, 0f), "Grand_T_Junction");
            SpawnPiece(cavernRoot, pLantern, new Vector3(-12f, 0.2f, 24f), Vector3.zero, "Junction_CenterLantern");

            // ═══════════════════════════════════════════════════════════════
            // 5. NHÁNH A: HẦM BÁU VẬT BÍ MẬT (SECRET TREASURE VAULT)
            // Tiến theo hướng +Z: rẽ phải vào buồng thiêng chứa Rương Cổ Vật
            // ═══════════════════════════════════════════════════════════════
            GameObject vaultGroup = new GameObject("Branch_TreasureVault");
            vaultGroup.transform.SetParent(cavernRoot.transform, false);

            SpawnPiece(vaultGroup, pTunnel1, new Vector3(-12f, -2f, 30f), Vector3.zero, "Vault_Corridor_1");
            SpawnPiece(vaultGroup, pSupport1, new Vector3(-12f, -2f, 30f), Vector3.zero, "Vault_Support_1");
            SpawnPiece(vaultGroup, pTurnR, new Vector3(-12f, -2f, 36f), Vector3.zero, "Vault_TurnR");

            // Buồng báu vật tận cùng
            GameObject vaultChamber = SpawnPiece(vaultGroup, pTunnel3, new Vector3(-6f, -2f, 36f), new Vector3(0f, 90f, 0f), "Vault_Sanctum");
            SpawnPiece(vaultChamber, pSupport2, new Vector3(-6f, -2f, 36f), new Vector3(0f, 90f, 0f), "Vault_Sanctum_Arch");
            SpawnPiece(vaultChamber, pLantern, new Vector3(-5.0f, 0.2f, 36f), Vector3.zero, "Vault_TreasureLantern");
            SpawnPiece(vaultChamber, pRock2, new Vector3(-4.5f, -2f, 37.5f), new Vector3(0f, 45f, 0f), "Vault_AltarRock");
            if (pOre != null) SpawnPiece(vaultChamber, pOre, new Vector3(-4.5f, -1.0f, 35f), new Vector3(0f, 90f, 0f), "Vault_OreDeposit");

            // RƯƠNG KHO BÁU ĐỘC QUYỀN
            CreateTreasureChest(vaultChamber, new Vector3(-5.5f, -2.0f, 36.2f), treasureName);

            // GHI CHÚ LORE CỐT TRUYỆN
            CreateLoreNote(vaultChamber, new Vector3(-6.5f, -1.6f, 36.5f), loreTitle, loreAuthor, loreContent);

            // ═══════════════════════════════════════════════════════════════
            // 6. NHÁNH B: SÀO HUYỆT QUÁI VẬT & ĐỘ SÂU THĂM THẲM (MONSTER LAIR)
            // Tiến tiếp theo hướng -X: dốc sâu thêm tầng 2 (Y = -4m)
            // ═══════════════════════════════════════════════════════════════
            GameObject lairGroup = new GameObject("Branch_MonsterLair");
            lairGroup.transform.SetParent(cavernRoot.transform, false);

            // Dốc lao xuống tầng 2 (RampDown đưa xuống Y = -4m)
            SpawnPiece(lairGroup, pRampDown, new Vector3(-18f, -2f, 24f), new Vector3(0f, -90f, 0f), "Lair_DescentSlope");
            SpawnPiece(lairGroup, pTunnel2, new Vector3(-24f, -4f, 24f), new Vector3(0f, -90f, 0f), "Lair_DeepTunnel");
            SpawnPiece(lairGroup, pTurnL, new Vector3(-30f, -4f, 24f), new Vector3(0f, -90f, 0f), "Lair_TurnL");

            // Hành lang quay về hướng -Z dẫn vào đại sảnh quái vật
            SpawnPiece(lairGroup, pTunnel1, new Vector3(-30f, -4f, 18f), new Vector3(0f, 180f, 0f), "Lair_Approach");
            SpawnPiece(lairGroup, pSupport1, new Vector3(-30f, -4f, 18f), new Vector3(0f, 180f, 0f), "Lair_Support_Deep");

            // Buồng sào huyệt rộng lớn
            GameObject lairChamber = SpawnPiece(lairGroup, pTunnel3, new Vector3(-30f, -4f, 12f), new Vector3(0f, 180f, 0f), "Lair_BossChamber");
            SpawnPiece(lairChamber, pRock1, new Vector3(-29f, -4f, 10.5f), Vector3.zero, "Lair_Boulder_1");
            SpawnPiece(lairChamber, pRock2, new Vector3(-31.5f, -4f, 13.5f), Vector3.zero, "Lair_Boulder_2");
            SpawnPiece(lairChamber, pCrate, new Vector3(-29.2f, -4f, 13.0f), new Vector3(0f, 30f, 0f), "Lair_SupplyCrate");
            SpawnPiece(lairChamber, pLantern, new Vector3(-30f, -1.8f, 12f), Vector3.zero, "Lair_OminousLantern");

            // ĐỐM LỬA / NGUỒN SÁNG ĐẶC TRƯNG CỦA SÀO HUYỆT
            GameObject lairLight = new GameObject("Lair_AtmosphereLight");
            lairLight.transform.SetParent(lairChamber.transform, false);
            lairLight.transform.localPosition = new Vector3(-30f, -2.5f, 12f);
            Light ll = lairLight.AddComponent<Light>();
            ll.type = LightType.Point;
            ll.color = themeColor;
            ll.range = 14f;
            ll.intensity = 1.4f;

            // KẺ THÙ TRONG HANG (CaveEnemySpawner)
            CreateEnemySpawner(lairChamber, new Vector3(-30f, -4f, 12f));

            Debug.Log(string.Format("💎 [TheForestMineCavern] Đã dựng thành công Hầm Mỏ Ngầm Đa Tầng cho {0} tại Y={1}!", caveName, subterraneanDepthY));
        }

        private GameObject SpawnPiece(GameObject parent, GameObject prefab, Vector3 localPos, Vector3 localRot, string name)
        {
            GameObject obj;
            if (prefab != null)
            {
                obj = Instantiate(prefab);
            }
            else
            {
                // Fallback nếu thiếu prefab: dùng Cube làm vách hầm
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.transform.localScale = new Vector3(3f, 3f, 6f);
            }

            obj.name = name;
            obj.transform.SetParent(parent.transform, false);
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.Euler(localRot);
            return obj;
        }

        private void CreateTreasureChest(GameObject parent, Vector3 localPos, string treasureName)
        {
            GameObject chestObj = new GameObject("Mine_LootChest_" + caveID);
            chestObj.transform.SetParent(parent.transform, false);
            chestObj.transform.localPosition = localPos;

            BoxCollider col = chestObj.AddComponent<BoxCollider>();
            col.size = new Vector3(1.4f, 1.0f, 1.2f);
            col.center = new Vector3(0f, 0.5f, 0f);

            // Dùng prefab Crate hoặc LootChest nếu có
            GameObject crateVisual = LoadMinePrefab("Props", "Crate");
            if (crateVisual != null)
            {
                GameObject visual = Instantiate(crateVisual);
                visual.name = "Chest_VisualModel";
                visual.transform.SetParent(chestObj.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localScale = Vector3.one * 1.2f;
            }

            lootChest = chestObj.AddComponent<CaveLootChest>();
            lootChest.caveID = caveID;
            lootChest.uniqueTreasureName = !string.IsNullOrEmpty(treasureName) ? treasureName : "Bảo Vật Hầm Mỏ Cổ";

            if (parentZone != null)
            {
                parentZone.lootChests = new GameObject[] { chestObj };
            }
        }

        private void CreateLoreNote(GameObject parent, Vector3 localPos, string title, string author, string content)
        {
            GameObject noteObj = new GameObject("Mine_LoreNote_" + caveID);
            noteObj.transform.SetParent(parent.transform, false);
            noteObj.transform.localPosition = localPos;

            BoxCollider col = noteObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(1.5f, 1.5f, 1.5f);

            loreNote = noteObj.AddComponent<LoreNote>();
            loreNote.noteID = caveID;
            loreNote.noteTitle = !string.IsNullOrEmpty(title) ? title : "Nhật ký người thợ mỏ";
            loreNote.noteAuthor = !string.IsNullOrEmpty(author) ? author : "Khai thác mỏ";
            loreNote.noteContent = !string.IsNullOrEmpty(content) ? content : "Hầm mỏ ngầm sâu này là nơi an toàn duy nhất để trốn lũ quái vật trên đảo... nhưng có thứ gì đó đang bò trong bóng tối.";

            if (parentZone != null)
            {
                parentZone.loreNotes = new GameObject[] { noteObj };
            }
        }

        private void CreateEnemySpawner(GameObject parent, Vector3 basePos)
        {
            GameObject spawnerObj = new GameObject("Mine_EnemySpawner");
            spawnerObj.transform.SetParent(parent.transform, false);
            spawnerObj.transform.localPosition = basePos;

            enemySpawner = spawnerObj.AddComponent<CaveEnemySpawner>();
            enemySpawner.parentCaveZone = parentZone;

            for (int i = 1; i <= 3; i++)
            {
                GameObject pt = new GameObject("MonsterSpawnPoint_" + i);
                pt.transform.SetParent(spawnerObj.transform, false);
                pt.transform.localPosition = new Vector3((i - 2) * 2.5f, 0f, (i - 1) * 3f);

                var cfg = new CaveEnemySpawner.EnemySpawnConfig
                {
                    enemyName = string.Format("Cave Monster #{0}", i),
                    spawnPoint = pt.transform,
                    respawnDelay = 300f
                };
                enemySpawner.spawnConfigs.Add(cfg);
            }
        }

        private GameObject LoadMinePrefab(string folder, string name)
        {
            GameObject prefab = null;
#if UNITY_EDITOR
            string path = "Assets/Mine/Prefabs/" + folder + "/" + name + ".prefab";
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
#endif
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>("Mine/Prefabs/" + folder + "/" + name);
            }
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>(name);
            }
            return prefab;
        }
    }
}

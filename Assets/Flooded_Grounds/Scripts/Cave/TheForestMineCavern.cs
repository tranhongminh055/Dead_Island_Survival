using UnityEngine;
using System.Collections.Generic;
using HorrorGame.Enemy;
using HorrorGame.Environment;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HorrorGame.Cave
{
    /// <summary>
    /// TheForestMineCavern — Hệ thống Hang Động & Hầm Mỏ Ngầm Đa Tầng chuẩn xác từng milimet
    /// Được xây dựng hoàn toàn từ gói Modular Mine Prefabs (Assets/Mine/Prefabs):
    ///
    /// Kích thước chuẩn đã kiểm chứng từ FBX & Example.unity:
    /// - Tunnel1 / Tunnel2 / Tunnel3: dài 12m (Z: -6 -> +6), cao 4m (Tunnel3 rộng 6m, cao 5.2m)
    /// - RampDown: dài 12m (Z: -6 -> +6), hạ độ cao chính xác 4.0m (Y: 0 -> -4)
    /// - Turn3 / TurnL / TurnR: tâm tại cổng vào (0,0,0), cổng rẽ tại (±6, 0, +6)
    ///
    /// Kiến trúc 3 tầng ngầm (0.00m hở, 0.00m chồng chéo, mọi ngõ cụt đều được bịt kín bằng đá sập):
    /// - Tầng 0 (Y = 0): Giếng thang lối vào (Hatch trần + Thang gỗ + Đèn bão + Đồ thợ mỏ) -> Hành lang -> Dốc xuống RampDown (-4m)
    /// - Tầng -1 (Y = -4): Hành lang sâu -> Ngã ba lớn Turn3:
    ///     + Nhánh Trái (Kho Báu): TurnR -> Buồng rộng Tunnel3 chứa Rương Cổ Vật (CaveLootChest) & Nhật ký (LoreNote)
    ///     + Nhánh Phải (Sào Huyệt): Dốc xuống RampDown (-8m) -> TurnR -> Tunnel2 -> Đại sảnh Tunnel3 chứa Quái vật & Quặng phát quang
    /// </summary>
    public class TheForestMineCavern : MonoBehaviour
    {
        [Header("── Cấu Hình Hang Mỏ ──")]
        public int caveID = 1;
        public string caveName = "Hang Mỏ Ngầm";
        public Vector3 entranceSurfacePos;
        public Color themeColor = new Color(0.95f, 0.85f, 0.12f);

        [Header("── Độ Sâu Của Mỏ ──")]
        [Tooltip("Độ sâu trục Y của hầm mỏ dưới lòng đất (mặc định -60m để cách biệt hoàn toàn với mặt nước biển)")]
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
        /// Dựng toàn bộ cấu trúc 3D Hầm Mỏ Ngầm bằng Modular Mine Prefabs (khớp 100% từng đốt)
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

            Vector3 worldMinePos = new Vector3(entranceSurfacePos.x, subterraneanDepthY, entranceSurfacePos.z);
            transform.position = worldMinePos;
            transform.rotation = Quaternion.identity;

            cavernRoot = new GameObject("TheForest_MineCavern_3D");
            cavernRoot.transform.SetParent(transform, false);
            cavernRoot.transform.localPosition = Vector3.zero;
            cavernRoot.transform.localRotation = Quaternion.identity;

            if (parentZone != null)
            {
                parentZone.caveGeometry = cavernRoot;
            }

            // 1. TẢI ĐẦY ĐỦ PREFAB TỪ PACKAGE MINE
            GameObject pTunnel1 = LoadMinePrefab("Base", "Tunnel1");
            GameObject pTunnel2 = LoadMinePrefab("Base", "Tunnel2");
            GameObject pTunnel3 = LoadMinePrefab("Base", "Tunnel3");
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
            GameObject pOreBlue = LoadMinePrefab("Rocks", "OreBlue");
            GameObject pOreGreen = LoadMinePrefab("Rocks", "OreGreen");
            GameObject pOreRed = LoadMinePrefab("Rocks", "OreRed");

            GameObject pPrimaryOre = (caveID == 5) ? pOreRed
                                   : (caveID == 6) ? pOreGreen
                                   : pOreBlue;

            GameObject geometryGroup = new GameObject("01_Mine_ModularTunnels");
            geometryGroup.transform.SetParent(cavernRoot.transform, false);

            GameObject propsGroup = new GameObject("02_Mine_PropsAndSupports");
            propsGroup.transform.SetParent(cavernRoot.transform, false);

            GameObject lightsGroup = new GameObject("03_Mine_LanternsAndLights");
            lightsGroup.transform.SetParent(cavernRoot.transform, false);

            // ═══════════════════════════════════════════════════════════════════════
            // TẦNG 0 (Y = 0): GIẾNG THANG LỐI VÀO -> HÀNH LANG -> DỐC XUỐNG (-4M)
            // ═══════════════════════════════════════════════════════════════════════

            // [1] Phòng Giếng Thang Lối Vào: Tunnel1 tại (0, 0, 0) [Z: -6 -> +6]
            SpawnPiece(geometryGroup, pTunnel1, new Vector3(0f, 0f, 0f), Vector3.zero, "01_EntranceShaft_Tunnel1");
            // Bịt kín đầu sau (Z = -6) bằng đá sập + tường chắn
            BuildRockfallSeal(propsGroup, pRock1, pRock2, new Vector3(0f, 0f, -6.0f), 0f, "Seal_EntranceBack");

            // Nắp hầm trần (Hatch) + Thang gỗ 2 tầng dẫn thẳng lên nắp hầm
            SpawnPiece(propsGroup, pHatch, new Vector3(0.95f, 3.86f, -1.0f), new Vector3(0f, 90f, 0f), "Ceiling_Hatch");
            SpawnPiece(propsGroup, pLadder, new Vector3(1.18f, 0.0f, -1.0f), new Vector3(0f, -90f, 0f), "Climb_Ladder_Lower");
            SpawnPiece(propsGroup, pLadder, new Vector3(1.18f, 1.0f, -1.0f), new Vector3(0f, -90f, 0f), "Climb_Ladder_Upper");

            // Khung gỗ gia cố + Đồ thợ mỏ bỏ lại + Đèn bão
            BuildTimberArch(propsGroup, pSupport1, pSupport2, new Vector3(0f, 0f, 3.5f), 0f, "Arch_Entrance");
            SpawnPiece(propsGroup, pCrate, new Vector3(-1.35f, 0f, -3.2f), new Vector3(0f, 18f, 0f), "Entrance_Crate_1");
            SpawnPiece(propsGroup, pCrate, new Vector3(-1.25f, 0.8f, -3.1f), new Vector3(0f, -12f, 0f), "Entrance_Crate_2");
            SpawnPiece(propsGroup, pPickaxe, new Vector3(-0.75f, 0.45f, -2.5f), new Vector3(12f, 45f, 78f), "Entrance_Pickaxe");
            SpawnPiece(propsGroup, pPlanks, new Vector3(0f, 0.02f, -1.0f), new Vector3(0f, 8f, 0f), "Entrance_FloorPlanks");

            SpawnLantern(lightsGroup, pLantern, new Vector3(-1.25f, 2.25f, -0.8f), 11f, 2.0f, new Color(1.0f, 0.72f, 0.42f), "Lantern_EntranceShaft");
            SpawnLantern(lightsGroup, pLantern, new Vector3(1.25f, 2.15f, 4.0f), 10f, 1.6f, new Color(1.0f, 0.72f, 0.42f), "Lantern_EntranceArch");

            // Điểm Spawn khi Player từ hố sâu trên mặt đất đi xuống (đứng vững trên sàn, nhìn thẳng vào hầm)
            GameObject spawnObj = new GameObject("Mine_InteriorSpawnPoint");
            spawnObj.transform.SetParent(cavernRoot.transform, false);
            spawnObj.transform.localPosition = new Vector3(-0.15f, 0.2f, 1.2f);
            spawnObj.transform.localRotation = Quaternion.identity;
            interiorSpawnPoint = spawnObj.transform;

            // TRIGGER TRÈO THANG LÊN MẶT ĐẤT
            GameObject exitTriggerObj = new GameObject("ExitLadder_InteractiveTrigger");
            exitTriggerObj.transform.SetParent(cavernRoot.transform, false);
            exitTriggerObj.transform.localPosition = new Vector3(0.85f, 1.5f, -1.0f);

            BoxCollider exitCol = exitTriggerObj.AddComponent<BoxCollider>();
            exitCol.isTrigger = true;
            exitCol.size = new Vector3(2.8f, 3.2f, 3.0f);

            exitEntranceComponent = exitTriggerObj.AddComponent<CaveEntrance>();
            exitEntranceComponent.entranceType = EntranceType.Exit;
            exitEntranceComponent.promptExit = string.Format("Nhấn [E] để trèo thang lên mặt đất ({0})", caveName);
            exitEntranceComponent.targetCave = parentZone;
            exitLadderTrigger = exitTriggerObj.transform;

            // [2] Hành Lang Chính: Tunnel2 tại (0, 0, 12) [Z: 6 -> 18]
            SpawnPiece(geometryGroup, pTunnel2, new Vector3(0f, 0f, 12f), Vector3.zero, "02_MainCorridor_Tunnel2");
            BuildTimberArch(propsGroup, pSupport1, pSupport2, new Vector3(0f, 0f, 9.5f), 0f, "Arch_Corridor_1");
            BuildTimberArch(propsGroup, pSupport1, pSupport2, new Vector3(0f, 0f, 15.5f), 0f, "Arch_Corridor_2");
            SpawnPiece(propsGroup, pPlanks, new Vector3(0.1f, 0.02f, 11.5f), new Vector3(0f, -5f, 0f), "Corridor_Planks");
            SpawnPiece(propsGroup, pCrate, new Vector3(-1.35f, 0f, 13.0f), new Vector3(0f, 32f, 0f), "Corridor_Crate");
            if (pPrimaryOre != null)
                SpawnPiece(propsGroup, pPrimaryOre, new Vector3(1.55f, 1.1f, 12.2f), new Vector3(0f, -90f, 15f), "Corridor_GlowingOre");
            SpawnLantern(lightsGroup, pLantern, new Vector3(-1.25f, 2.2f, 12.5f), 10f, 1.6f, new Color(1.0f, 0.70f, 0.40f), "Lantern_Corridor");

            // [3] Dốc Xuống Tầng -1: RampDown tại (0, 0, 24) [Z: 18 (Y=0) -> Z: 30 (Y=-4)]
            SpawnPiece(geometryGroup, pRampDown, new Vector3(0f, 0f, 24f), Vector3.zero, "03_FirstDescent_RampDown");
            SpawnLantern(lightsGroup, pLantern, new Vector3(1.25f, -0.6f, 23.5f), 11f, 1.7f, new Color(1.0f, 0.68f, 0.38f), "Lantern_RampDown1");

            // ═══════════════════════════════════════════════════════════════════════
            // TẦNG -1 (Y = -4): HÀNH LANG SÂU -> NGÃ BA LỚN (TURN3)
            // ═══════════════════════════════════════════════════════════════════════

            // [4] Hành Lang Sâu: Tunnel1 tại (0, -4, 36) [Z: 30 -> 42]
            SpawnPiece(geometryGroup, pTunnel1, new Vector3(0f, -4f, 36f), Vector3.zero, "04_DeepCorridor_Tunnel1");
            BuildTimberArch(propsGroup, pSupport1, pSupport2, new Vector3(0f, -4f, 33.5f), 0f, "Arch_Deep_1");
            BuildTimberArch(propsGroup, pSupport1, pSupport2, new Vector3(0f, -4f, 39.5f), 0f, "Arch_Deep_2");
            if (pPrimaryOre != null)
                SpawnPiece(propsGroup, pPrimaryOre, new Vector3(-1.6f, -2.8f, 36.5f), new Vector3(0f, 90f, -10f), "Deep_GlowingOre");
            SpawnLantern(lightsGroup, pLantern, new Vector3(1.25f, -1.8f, 36.0f), 10f, 1.6f, new Color(1.0f, 0.68f, 0.38f), "Lantern_DeepCorridor");

            // [5] Ngã Ba Lớn: Turn3 tại (0, -4, 42)
            // Cổng vào: (0, -4, 42) | Cổng Trái (-X): (-6, -4, 48) | Cổng Phải (+X): (+6, -4, 48)
            SpawnPiece(geometryGroup, pTurn3, new Vector3(0f, -4f, 42f), Vector3.zero, "05_GrandJunction_Turn3");
            SpawnPiece(propsGroup, pCrate, new Vector3(-0.8f, -4f, 49.8f), new Vector3(0f, 15f, 0f), "Junction_Crate");
            SpawnPiece(propsGroup, pPickaxe, new Vector3(0.4f, -3.6f, 49.9f), new Vector3(15f, 170f, 80f), "Junction_Pickaxe");
            SpawnLantern(lightsGroup, pLantern, new Vector3(0f, -1.2f, 47.5f), 13f, 2.0f, new Color(1.0f, 0.75f, 0.45f), "Lantern_GrandJunction");

            // ═══════════════════════════════════════════════════════════════════════
            // NHÁNH A (TRÁI, -X, Y = -4): HẦM BÁU VẬT BÍ MẬT (SECRET TREASURE VAULT)
            // ═══════════════════════════════════════════════════════════════════════

            // [6] Hành Lang Kho Báu: Tunnel2 tại (-12, -4, 48), yaw -90 [X: -6 -> -18]
            SpawnPiece(geometryGroup, pTunnel2, new Vector3(-12f, -4f, 48f), new Vector3(0f, -90f, 0f), "06_VaultCorridor_Tunnel2");
            BuildTimberArch(propsGroup, pSupport1, pSupport2, new Vector3(-12f, -4f, 48f), -90f, "Arch_VaultCorridor");
            SpawnPiece(propsGroup, pPlanks, new Vector3(-11.5f, -3.98f, 48f), new Vector3(0f, 85f, 0f), "Vault_FloorPlanks");
            SpawnLantern(lightsGroup, pLantern, new Vector3(-12f, -1.8f, 46.75f), 10f, 1.5f, new Color(1.0f, 0.72f, 0.42f), "Lantern_VaultCorridor");

            // [7] Khúc Cua Rẽ Phải (+Z): TurnR tại (-18, -4, 48), yaw -90 -> Cổng ra: (-24, -4, 54) hướng +Z
            SpawnPiece(geometryGroup, pTurnR, new Vector3(-18f, -4f, 48f), new Vector3(0f, -90f, 0f), "07_VaultTurn_TurnR");

            // [8] Đại Sảnh Kho Báu: Tunnel3 tại (-24, -4, 60), yaw 0 [Z: 54 -> 66]
            SpawnPiece(geometryGroup, pTunnel3, new Vector3(-24f, -4f, 60f), Vector3.zero, "08_VaultSanctum_Tunnel3");
            // Bịt kín đầu tận cùng Z = 66 bằng đá sập lớn
            BuildRockfallSeal(propsGroup, pRock1, pRock2, new Vector3(-24f, -4f, 66.0f), 180f, "Seal_VaultEnd");

            // Trang trí & Quặng phát quang trong buồng Kho Báu
            if (pOreBlue != null)
                SpawnPiece(propsGroup, pOreBlue, new Vector3(-26.3f, -2.8f, 61.5f), new Vector3(0f, 90f, 0f), "Vault_OreBlue_1");
            if (pOreGreen != null)
                SpawnPiece(propsGroup, pOreGreen, new Vector3(-21.7f, -2.6f, 62.5f), new Vector3(0f, -90f, 0f), "Vault_OreGreen_1");
            SpawnPiece(propsGroup, pPlanks, new Vector3(-24f, -3.98f, 60.5f), new Vector3(0f, 12f, 0f), "Vault_Planks");
            SpawnPiece(propsGroup, pPickaxe, new Vector3(-25.2f, -3.6f, 62.0f), new Vector3(10f, 35f, 80f), "Vault_Pickaxe");

            SpawnLantern(lightsGroup, pLantern, new Vector3(-25.8f, -1.6f, 60.5f), 12f, 1.9f, new Color(1.0f, 0.78f, 0.45f), "Lantern_VaultLeft");
            SpawnLantern(lightsGroup, pLantern, new Vector3(-22.2f, -1.6f, 62.5f), 12f, 1.9f, new Color(0.55f, 0.88f, 1.0f), "Lantern_VaultRight");

            // Rương Kho Báu & Nhật Ký Lore
            CreateTreasureChest(cavernRoot, new Vector3(-24.0f, -4.0f, 62.8f), treasureName);
            CreateLoreNote(cavernRoot, new Vector3(-22.4f, -3.5f, 60.8f), loreTitle, loreAuthor, loreContent);

            // ═══════════════════════════════════════════════════════════════════════
            // NHÁNH B (PHẢI, +X, TẦNG -2 Y = -8): DỐC SÂU & SÀO HUYỆT QUÁI VẬT
            // ═══════════════════════════════════════════════════════════════════════

            // [9] Dốc Lao Xuống Tầng -2: RampDown tại (+12, -4, 48), yaw +90 [X: +6 (Y=-4) -> X: +18 (Y=-8)]
            SpawnPiece(geometryGroup, pRampDown, new Vector3(12f, -4f, 48f), new Vector3(0f, 90f, 0f), "09_AbyssDescent_RampDown");
            SpawnLantern(lightsGroup, pLantern, new Vector3(12f, -4.7f, 49.25f), 11f, 1.7f, new Color(1.0f, 0.55f, 0.30f), "Lantern_AbyssRamp");

            // [10] Khúc Cua Rẽ Phải (-Z): TurnR tại (+18, -8, 48), yaw +90 -> Cổng ra: (+24, -8, 42) hướng -Z
            SpawnPiece(geometryGroup, pTurnR, new Vector3(18f, -8f, 48f), new Vector3(0f, 90f, 0f), "10_AbyssTurn_TurnR");

            // [11] Hành Lang Sào Huyệt: Tunnel2 tại (+24, -8, 36), yaw 180 [Z: 42 -> 30]
            SpawnPiece(geometryGroup, pTunnel2, new Vector3(24f, -8f, 36f), new Vector3(0f, 180f, 0f), "11_LairApproach_Tunnel2");
            BuildTimberArch(propsGroup, pSupport1, pSupport2, new Vector3(24f, -8f, 36f), 180f, "Arch_LairApproach");
            if (pOreRed != null)
                SpawnPiece(propsGroup, pOreRed, new Vector3(25.55f, -6.8f, 35.5f), new Vector3(0f, -90f, 0f), "Lair_OreRed_1");
            SpawnLantern(lightsGroup, pLantern, new Vector3(22.75f, -5.8f, 36.0f), 10f, 1.5f, new Color(1.0f, 0.50f, 0.28f), "Lantern_LairApproach");

            // [12] Đại Sảnh Sào Huyệt Quái Vật: Tunnel3 tại (+24, -8, 24), yaw 180 [Z: 30 -> 18]
            SpawnPiece(geometryGroup, pTunnel3, new Vector3(24f, -8f, 24f), new Vector3(0f, 180f, 0f), "12_MonsterLair_Tunnel3");
            // Bịt kín đầu tận cùng Z = 18 bằng đá sập lớn
            BuildRockfallSeal(propsGroup, pRock1, pRock2, new Vector3(24f, -8f, 18.0f), 0f, "Seal_LairEnd");

            // Đá tảng che chắn + Thùng gỗ + Quặng đỏ + Ánh sáng rùng rợn
            SpawnPiece(propsGroup, pRock1, new Vector3(26.0f, -8f, 25.5f), new Vector3(0f, 25f, 0f), "Lair_CoverRock_1");
            SpawnPiece(propsGroup, pRock2, new Vector3(22.0f, -8f, 22.0f), new Vector3(0f, 110f, 0f), "Lair_CoverRock_2");
            SpawnPiece(propsGroup, pCrate, new Vector3(25.6f, -8f, 22.5f), new Vector3(0f, -20f, 0f), "Lair_SupplyCrate");
            if (pOreRed != null)
                SpawnPiece(propsGroup, pOreRed, new Vector3(21.6f, -6.5f, 24.5f), new Vector3(0f, 90f, 0f), "Lair_OreRed_2");

            SpawnLantern(lightsGroup, pLantern, new Vector3(24.0f, -5.0f, 24.0f), 15f, 2.2f, themeColor, "Lantern_LairCenter");

            // Kẻ thù bên trong Đại Sảnh
            CreateEnemySpawner(cavernRoot, new Vector3(24f, -8f, 24f));

            // Hiệu ứng hạt bụi ẩm lơ lửng trong hang
            CreateCaveDustParticles(cavernRoot, new Vector3(0f, -2f, 24f));

            Debug.Log(string.Format("💎 [TheForestMineCavern] Đã dựng hoàn chỉnh Hầm Mỏ Ngầm 3 tầng (khớp 100% Modular Mine) cho {0} tại Y={1}!", caveName, subterraneanDepthY));
        }

        // =========================================================================
        // HELPER: DỰNG KHUNG GỖ CHỐNG HẦM (2 CỘT SUPPORT1 + 1 XÀ NGANG SUPPORT2)
        // =========================================================================
        private void BuildTimberArch(GameObject parent, GameObject pSupport1, GameObject pSupport2, Vector3 centerPos, float yaw, string archName)
        {
            GameObject arch = new GameObject(archName);
            arch.transform.SetParent(parent.transform, false);
            arch.transform.localPosition = centerPos;
            arch.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            if (pSupport1 != null)
            {
                SpawnPiece(arch, pSupport1, new Vector3(-1.45f, 0f, 0f), new Vector3(0f, 0f, 0f), "Post_Left");
                SpawnPiece(arch, pSupport1, new Vector3(1.45f, 0f, 0f), new Vector3(0f, 180f, 0f), "Post_Right");
            }
            if (pSupport2 != null)
            {
                // Xoay ngang trên trần hang (khớp đúng góc trong Example.unity)
                SpawnPiece(arch, pSupport2, new Vector3(0f, 3.85f, 0f), new Vector3(90f, 0f, -90f), "Beam_Top");
            }
        }

        // =========================================================================
        // HELPER: BỊT KÍN ĐẦU NGÕ CỤT BẰNG ĐÁ SẬP + COLLIDER CHẮN VOID
        // =========================================================================
        private void BuildRockfallSeal(GameObject parent, GameObject pRock1, GameObject pRock2, Vector3 centerPos, float yaw, string sealName)
        {
            GameObject seal = new GameObject(sealName);
            seal.transform.SetParent(parent.transform, false);
            seal.transform.localPosition = centerPos;
            seal.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            // Tường chắn vô hình đảm bảo 100% player không bao giờ lọt ra ngoài map
            BoxCollider blocker = seal.AddComponent<BoxCollider>();
            blocker.center = new Vector3(0f, 2.5f, -0.3f);
            blocker.size = new Vector3(7.0f, 6.0f, 1.2f);

            // Đá sập tự nhiên che kín tầm nhìn
            if (pRock1 != null)
            {
                GameObject r1 = SpawnPiece(seal, pRock1, new Vector3(-1.3f, 0f, 0.2f), new Vector3(5f, 25f, -8f), "SealRock_L");
                r1.transform.localScale = new Vector3(2.2f, 2.6f, 1.8f);

                GameObject r2 = SpawnPiece(seal, pRock1, new Vector3(1.4f, 0f, 0.3f), new Vector3(-6f, 160f, 6f), "SealRock_R");
                r2.transform.localScale = new Vector3(2.2f, 2.6f, 1.8f);
            }
            if (pRock2 != null)
            {
                GameObject r3 = SpawnPiece(seal, pRock2, new Vector3(0f, 1.8f, 0.1f), new Vector3(12f, 70f, 10f), "SealRock_Top");
                r3.transform.localScale = new Vector3(2.8f, 2.2f, 1.8f);

                GameObject r4 = SpawnPiece(seal, pRock2, new Vector3(0f, 0.1f, 0.4f), new Vector3(-4f, -40f, 0f), "SealRock_Center");
                r4.transform.localScale = new Vector3(2.0f, 1.8f, 1.6f);
            }
        }

        // =========================================================================
        // HELPER: ĐÈN BÃO REALTIME (FIX LỖI BAKED-ONLY TRONG PREFAB LANTERN)
        // =========================================================================
        private GameObject SpawnLantern(GameObject parent, GameObject pLantern, Vector3 localPos, float range, float intensity, Color lightColor, string name)
        {
            GameObject obj = SpawnPiece(parent, pLantern, localPos, Vector3.zero, name);
            Light lgt = obj.GetComponentInChildren<Light>();
            if (lgt == null)
            {
                GameObject lObj = new GameObject("Lantern_PointLight");
                lObj.transform.SetParent(obj.transform, false);
                lObj.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                lgt = lObj.AddComponent<Light>();
                lgt.type = LightType.Point;
            }

            lgt.enabled = true;
            lgt.color = lightColor;
            lgt.range = range;
            lgt.intensity = intensity;
            lgt.shadows = LightShadows.None; // Tối ưu FPS trong hang nhiều đèn
#if UNITY_EDITOR
            lgt.lightmapBakeType = LightmapBakeType.Realtime;
#endif
            return obj;
        }

        private GameObject SpawnPiece(GameObject parent, GameObject prefab, Vector3 localPos, Vector3 localRot, string name)
        {
            GameObject obj;
            if (prefab != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                }
                else
                {
                    obj = Instantiate(prefab);
                }
#else
                obj = Instantiate(prefab);
#endif
            }
            else
            {
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

            GameObject crateVisual = LoadMinePrefab("Props", "Crate");
            if (crateVisual != null)
            {
                GameObject visual = SpawnPiece(chestObj, crateVisual, Vector3.zero, Vector3.zero, "Chest_VisualModel");
                visual.transform.localScale = Vector3.one * 1.15f;
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
            col.size = new Vector3(1.6f, 1.6f, 1.6f);

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

            Vector3[] offsets = new Vector3[]
            {
                new Vector3(-1.4f, 0.1f, -2.0f),
                new Vector3(1.4f, 0.1f, 0.5f),
                new Vector3(0.0f, 0.1f, 2.5f)
            };

            Transform[] spawnTransforms = new Transform[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject pt = new GameObject("MonsterSpawnPoint_" + (i + 1));
                pt.transform.SetParent(spawnerObj.transform, false);
                pt.transform.localPosition = offsets[i];
                spawnTransforms[i] = pt.transform;

                var cfg = new CaveEnemySpawner.EnemySpawnConfig
                {
                    enemyName = string.Format("Cave Monster #{0}", i + 1),
                    spawnPoint = pt.transform,
                    respawnDelay = 300f
                };
                enemySpawner.spawnConfigs.Add(cfg);
            }

            if (parentZone != null)
            {
                parentZone.enemySpawnPoints = spawnTransforms;
            }
        }

        private void CreateCaveDustParticles(GameObject parent, Vector3 centerPos)
        {
            GameObject dustObj = new GameObject("Mine_AmbientDust");
            dustObj.transform.SetParent(parent.transform, false);
            dustObj.transform.localPosition = centerPos;

            ParticleSystem ps = dustObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 8f;
            main.startSpeed = 0.08f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
            main.startColor = new Color(0.75f, 0.80f, 0.85f, 0.22f);
            main.maxParticles = 120;

            var emission = ps.emission;
            emission.rateOverTime = 15f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(45f, 10f, 50f);
        }

        public static GameObject LoadMinePrefab(string folder, string name)
        {
            GameObject prefab = null;
#if UNITY_EDITOR
            string path = "Assets/Mine/Prefabs/" + folder + "/" + name + ".prefab";
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
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

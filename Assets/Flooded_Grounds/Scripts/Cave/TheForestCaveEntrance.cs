using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HorrorGame.Cave
{
    /// <summary>
    /// TheForestCaveEntrance — Hố Sâu Địa Hình Thật + Cửa Hầm Mỏ (Package Mine):
    /// - Khoét trực tiếp trên Terrain một hố sụt rộng (bán kính 10.5m), sâu 7.5m có lối dốc đi bộ xuống đáy hố.
    /// - Xung quanh viền ngoài hố (bán kính ~10m) trang trí đá tảng Rock1/Rock2 (tuyệt đối KHÔNG che lấp miệng hố).
    /// - Dưới đáy hố (-7.5m) mở ra Cửa Hầm Mỏ thật (Tunnel1 + khung gỗ Support1/Support2 + đèn bão Lantern + Crate + Pickaxe).
    /// - Khi Player đi xuống hố và bước vào cửa hầm tối đen (hoặc nhấn [E]) -> chuyển cảnh mượt vào Hầm Mỏ Ngầm 3 tầng (-60m).
    /// </summary>
    public class TheForestCaveEntrance : MonoBehaviour
    {
        public static List<TheForestCaveEntrance> AllEntrances = new List<TheForestCaveEntrance>();
        public static TheForestCaveEntrance Instance;

        [Header("── Thông Tin Cửa Hang ──")]
        public int caveID = 1;
        public string caveName = "Hang Sụp Đổ (The Collapsed Den)";

        [Tooltip("Tọa độ tâm hố sâu trên mặt đất")]
        public Vector3 entranceWorldPosition = new Vector3(310f, 17.5f, 500f);

        [Header("── Kích Thước Hố Sâu ──")]
        public float pitOuterRadius = 10.5f;
        public float pitFloorRadius = 4.5f;
        public float pitDepth = 7.5f;

        [Header("── Màu Chủ Đạo ──")]
        public Color ropeColor = new Color(0.95f, 0.85f, 0.12f);

        [Header("── Tham Chiếu Tự Động ──")]
        public CaveEntrance caveEntranceComponent;
        public CaveEntrance fallEntranceComponent;
        public Transform exteriorSpawnPoint;
        public Transform interiorSpawnPoint;

        private GameObject moundRoot;

        public static readonly ForestCavePreset[] DefaultCavePresets = new ForestCavePreset[]
        {
            new ForestCavePreset(1, "Hang Sụp Đổ (The Collapsed Den)", new Vector3(310f, 17.5f, 500f), new Color(0.95f, 0.85f, 0.12f), "Hố sụt sâu trong rừng rậm Tây Bắc dẫn xuống hầm mỏ cổ bỏ hoang."),
            new ForestCavePreset(2, "Hang Rễ Cây (The Root Cavern)", new Vector3(320f, 22.5f, 660f), new Color(0.95f, 0.65f, 0.15f), "Hố sâu dưới gốc cây cổ thụ khổng lồ phía Bắc đảo."),
            new ForestCavePreset(3, "Hang Thủy Triều (The Tidal Grotto)", new Vector3(450f, 18.0f, 725f), new Color(0.20f, 0.85f, 0.95f), "Hang vách đá biển phía Bắc. Mực nước dâng theo thủy triều ban đêm."),
            new ForestCavePreset(4, "Hang Đầm Lầy (The Swamp Abyss)", new Vector3(630f, 18.5f, 490f), new Color(0.95f, 0.25f, 0.15f), "Hố sâu phía Đông gần tổ thổ dân ăn thịt người."),
            new ForestCavePreset(5, "Hang Nham Thạch (The Volcanic Vent)", new Vector3(550f, 32.0f, 360f), new Color(1.00f, 0.55f, 0.10f), "Hang dung nham nóng rực sườn núi lửa phía Nam."),
            new ForestCavePreset(6, "Lõi Ký Sinh Ngầm (The Parasite Core - BOSS)", new Vector3(580f, 19.5f, 420f), new Color(0.25f, 0.95f, 0.45f), "Hố vực sâu nhất dưới thung lũng bí ẩn — sào huyệt Nữ Hoàng Ký Sinh.")
        };

        private void OnEnable()
        {
            if (!AllEntrances.Contains(this)) AllEntrances.Add(this);
            if (Instance == null || caveID == 1) Instance = this;
        }

        private void OnDisable()
        {
            if (AllEntrances.Contains(this)) AllEntrances.Remove(this);
        }

        private void Awake()
        {
            if (!AllEntrances.Contains(this)) AllEntrances.Add(this);
            if (Instance == null || caveID == 1) Instance = this;
        }

        private void Start()
        {
            if (transform.Find("CaveEntrance_DeepPit") == null)
            {
                BuildForestCaveEntranceModel(false);
            }
        }

        /// <summary>
        /// Dọn dẹp toàn bộ đá tảng / cây / vật cản nằm trong lòng hố
        /// </summary>
        public static void ClearObstaclesAroundPit(Vector3 center, float radius = 10.0f)
        {
            List<GameObject> toRemove = new List<GameObject>();

            Collider[] cols = Physics.OverlapSphere(center, radius);
            foreach (var c in cols)
            {
                if (c == null || c.gameObject == null) continue;
                GameObject go = c.gameObject;
                if (go.CompareTag("Player") || go.GetComponent<Terrain>() != null) continue;
                if (IsPartOfCaveSystem(go)) continue;

                string lower = go.name.ToLower();
                if (lower.Contains("rock") || lower.Contains("stone") || lower.Contains("cliff") ||
                    lower.Contains("boulder") || lower.Contains("cobble") || lower.Contains("rimrock") ||
                    lower.Contains("plank") || lower.Contains("ramp") || lower.Contains("tunnel") ||
                    lower.Contains("bush") || lower.Contains("stump"))
                {
                    if (!toRemove.Contains(go)) toRemove.Add(go);
                }
            }

            foreach (var obj in toRemove)
            {
                if (obj == null) continue;
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(obj);
#else
                Destroy(obj);
#endif
            }
        }

        private static bool IsPartOfCaveSystem(GameObject go)
        {
            Transform t = go.transform;
            while (t != null)
            {
                if (t.GetComponent<TheForestCaveEntrance>() != null ||
                    t.GetComponent<TheForestMineCavern>() != null)
                    return true;
                t = t.parent;
            }
            return false;
        }

        /// <summary>
        /// Tạo Hố Sụt Khô Ráo trên Terrain (CHỐNG NGẬP NƯỚC):
        /// - Luôn giữ đáy hố CAO HƠN mực nước biển (minFloorY = 17.8f > nước 16.8f) để hố KHÔNG BAO GIỜ bị ngập nước.
        /// - Nâng vách đồi tự nhiên xung quanh lên +7.5m tạo thành miệng hố sâu 7.5m cực kỳ rõ nét,
        ///   kèm đường dốc đi bộ thoải xuống đáy hố ở hướng -Z.
        /// </summary>
        public static void DigTerrainHole(Vector3 worldPos, float outerRadius = 10.5f, float depthMeters = 7.5f, float floorRadius = 4.5f)
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null || terrain.terrainData == null) return;

            TerrainData td = terrain.terrainData;
            Vector3 tPos = terrain.transform.position;
            Vector3 relPos = worldPos - tPos;

            int hmWidth = td.heightmapWidth;
            int hmHeight = td.heightmapHeight;
            Vector3 tSize = td.size;

            // Vùng đắp đồi tạo thành miệng hố sâu: rộng 22m bao quanh hố 10.5m
            float hillOuterRadius = outerRadius + 11.5f;
            int centerX = Mathf.RoundToInt((relPos.x / tSize.x) * (hmWidth - 1));
            int centerZ = Mathf.RoundToInt((relPos.z / tSize.z) * (hmHeight - 1));

            int radX = Mathf.Max(4, Mathf.CeilToInt((hillOuterRadius / tSize.x) * (hmWidth - 1)));
            int radZ = Mathf.Max(4, Mathf.CeilToInt((hillOuterRadius / tSize.z) * (hmHeight - 1)));

            int startX = Mathf.Clamp(centerX - radX, 0, hmWidth - 1);
            int startZ = Mathf.Clamp(centerZ - radZ, 0, hmHeight - 1);
            int sizeX = Mathf.Clamp(centerX + radX, 0, hmWidth - 1) - startX + 1;
            int sizeZ = Mathf.Clamp(centerZ + radZ, 0, hmHeight - 1) - startZ + 1;

            if (sizeX <= 0 || sizeZ <= 0) return;

            float[,] heights = td.GetHeights(startX, startZ, sizeX, sizeZ);

            // Tính cao độ mặt đất gốc ở rìa ngoài cùng (không bị ảnh hưởng bởi lần đào trước)
            float baseGroundWorldY = (heights[0, 0] + heights[0, sizeX - 1] + heights[sizeZ - 1, 0] + heights[sizeZ - 1, sizeX - 1]) * 0.25f * tSize.y + tPos.y;

            // Đáy hố luôn cao hơn mực nước (16.8m) ít nhất 1.2m -> 18.0m để khô ráo 100%!
            float floorWorldY = Mathf.Max(baseGroundWorldY - 1.5f, 18.0f);
            // Miệng hố (vành đồi xung quanh) cao hơn đáy hố đúng depthMeters (7.5m)
            float rimWorldY = floorWorldY + depthMeters;

            float floorNorm = Mathf.Clamp01((floorWorldY - tPos.y) / tSize.y);
            float rimNorm = Mathf.Clamp01((rimWorldY - tPos.y) / tSize.y);

            for (int z = 0; z < sizeZ; z++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    int curX = startX + x;
                    int curZ = startZ + z;

                    float wx = ((float)curX / (hmWidth - 1)) * tSize.x + tPos.x;
                    float wz = ((float)curZ / (hmHeight - 1)) * tSize.z + tPos.z;
                    float dx = wx - worldPos.x;
                    float dz = wz - worldPos.z;
                    float dist = Mathf.Sqrt(dx * dx + dz * dz);

                    if (dist >= hillOuterRadius) continue;

                    float origH = heights[z, x];
                    float targetH = origH;

                    if (dist <= floorRadius)
                    {
                        // Đáy hố phẳng khô ráo
                        targetH = floorNorm;
                    }
                    else if (dist <= outerRadius)
                    {
                        // Vách hố dốc đứng từ đáy lên miệng đồi (+7.5m)
                        float t = (dist - floorRadius) / (outerRadius - floorRadius);
                        float smooth = t * t * (3f - 2f * t);
                        targetH = Mathf.Lerp(floorNorm, rimNorm, smooth);
                    }
                    else
                    {
                        // Sườn đồi ngoài thoải mượt hòa xuống mặt đất tự nhiên
                        float t = (dist - outerRadius) / (hillOuterRadius - outerRadius);
                        float smooth = t * t * (3f - 2f * t);
                        targetH = Mathf.Lerp(rimNorm, origH, smooth);
                    }

                    // Cắt rãnh lối đi bộ thoải xuống đáy hố ở phía -Z (Z từ -18m -> -3.5m, rộng ±2.6m)
                    if (dz < -2.0f && dz > -19.0f && Mathf.Abs(dx) < 3.6f)
                    {
                        float rampT = Mathf.Clamp01((-dz - 3.5f) / 14.0f); // 0 ở đáy hố, 1 ở mặt đất ngoài
                        float entryH = Mathf.Lerp(floorNorm, origH, rampT);

                        float sideT = Mathf.Clamp01((Mathf.Abs(dx) - 1.8f) / 1.8f);
                        float sideSmooth = sideT * sideT * (3f - 2f * sideT);
                        float blendedRampH = Mathf.Lerp(entryH, targetH, sideSmooth);

                        targetH = Mathf.Min(targetH, blendedRampH);
                    }

                    heights[z, x] = targetH;
                }
            }

            td.SetHeights(startX, startZ, heights);
#if UNITY_EDITOR
            EditorUtility.SetDirty(td);
#endif
        }

        /// <summary>
        /// Dựng toàn bộ Hố Sâu + Cửa Hầm Mỏ Mine ở đáy hố
        /// </summary>
        public void BuildForestCaveEntranceModel(bool digTerrain = true)
        {
            if (transform.position != Vector3.zero)
            {
                entranceWorldPosition = transform.position;
            }

            // Đào & đắp đồi hố sâu khô ráo trước
            if (digTerrain)
            {
                ClearObstaclesAroundPit(entranceWorldPosition, pitOuterRadius + 2f);
                DigTerrainHole(entranceWorldPosition, pitOuterRadius, pitDepth, pitFloorRadius);
            }

            // Đo chính xác cao độ đáy hố & vành hố thực tế sau khi tạo địa hình
            float floorWorldY = 18.0f;
            float rimWorldY = floorWorldY + pitDepth;
            if (Terrain.activeTerrain != null)
            {
                floorWorldY = Terrain.activeTerrain.SampleHeight(entranceWorldPosition) + Terrain.activeTerrain.transform.position.y;
                float rimSample = Terrain.activeTerrain.SampleHeight(entranceWorldPosition + new Vector3(pitOuterRadius, 0f, 0f)) + Terrain.activeTerrain.transform.position.y;
                rimWorldY = Mathf.Max(rimSample, floorWorldY + 4.0f);
            }

            // Đặt gốc GameObject ngay tại cao độ vành hố, còn đáy hố ở localY = actualFloorY (âm)
            entranceWorldPosition.y = rimWorldY;
            transform.position = entranceWorldPosition;
            transform.rotation = Quaternion.identity;

            float actualFloorY = floorWorldY - rimWorldY;
            if (actualFloorY > -2.0f) actualFloorY = -pitDepth;

            // Xóa cấu trúc cũ
            while (transform.childCount > 0)
            {
                Transform child = transform.GetChild(0);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }

            moundRoot = new GameObject("CaveEntrance_DeepPit");
            moundRoot.transform.SetParent(transform, false);
            moundRoot.transform.localPosition = Vector3.zero;
            moundRoot.transform.localRotation = Quaternion.identity;

            // 1. CỬA HẦM MỎ THẬT (TUNNEL1 + SUPPORT + LANTERN + CRATE) Ở ĐÁY HỐ SÂU
            SetupBottomPitMineMouth(moundRoot, actualFloorY);

            // 2. VIỀN ĐÁ TẢNG TỰ NHIÊN QUANH MÉP NGOÀI HỐ (KHÔNG BAO GIỜ CHE LỖ HỐ!)
            SetupOuterRimRocksAndSignpost(moundRoot);

            // 3. SƯƠNG LẠNH BỐC RA TỪ CỬA HẦM ĐÁY HỐ
            SetupCaveDraftParticles(moundRoot, actualFloorY);

            // 4. TRIGGER BƯỚC VÀO CỬA HẦM & TRIGGER NHẤN [E]
            SetupInteractionAndWalkInTriggers(moundRoot, actualFloorY);

            // 5. CỘT KHÓI ĐỎ BÁO HIỆU (ĐẶT CÁCH XA MÉP HỐ 13M ĐỂ KHÔNG CHE HỐ)
            SetupAdjacentSignalSmoke();

            Debug.Log(string.Format("🕳️ [TheForestCaveEntrance] Đã dựng hoàn chỉnh HỐ SÂU {0:F1}m + Cửa Hầm Mỏ Mine cho {1} tại {2}!", -actualFloorY, caveName, entranceWorldPosition));
        }

        // =========================================================================
        // 1. CỬA HẦM MỎ (PACKAGE MINE) MỞ RA NGAY TẠI ĐÁY HỐ SÂU
        // =========================================================================
        private void SetupBottomPitMineMouth(GameObject root, float floorY)
        {
            GameObject mouthGroup = new GameObject("BottomPit_MineTunnelMouth");
            mouthGroup.transform.SetParent(root.transform, false);
            mouthGroup.transform.localPosition = new Vector3(0f, floorY, 0f);

            GameObject pTunnel1 = TheForestMineCavern.LoadMinePrefab("Base", "Tunnel1");
            GameObject pSupport1 = TheForestMineCavern.LoadMinePrefab("Props", "Support1");
            GameObject pSupport2 = TheForestMineCavern.LoadMinePrefab("Props", "Support2");
            GameObject pLantern = TheForestMineCavern.LoadMinePrefab("Props", "Lantern");
            GameObject pPlanks = TheForestMineCavern.LoadMinePrefab("Props", "Planks");
            GameObject pCrate = TheForestMineCavern.LoadMinePrefab("Props", "Crate");
            GameObject pPickaxe = TheForestMineCavern.LoadMinePrefab("Props", "Pickaxe");
            GameObject pRock1 = TheForestMineCavern.LoadMinePrefab("Rocks", "Rock1");

            // Đoạn hầm thật Tunnel1 (dài 12m: Z từ +2 -> +14) đâm thẳng vào vách đất phía +Z của hố sâu!
            if (pTunnel1 != null)
            {
                GameObject tunnel = InstantiatePrefabSafe(pTunnel1, mouthGroup.transform);
                tunnel.name = "MineMouth_Tunnel1";
                tunnel.transform.localPosition = new Vector3(0f, 0f, 8.0f);
                tunnel.transform.localRotation = Quaternion.identity;
            }

            // Bức tường tối đen ở sâu bên trong cửa hầm (Z = +11.5) tạo cảm giác sâu hun hút
            GameObject darkWall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            darkWall.name = "MineMouth_AbyssDarkness";
            darkWall.transform.SetParent(mouthGroup.transform, false);
            darkWall.transform.localPosition = new Vector3(0f, 2.0f, 11.2f);
            darkWall.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            darkWall.transform.localScale = new Vector3(5.0f, 4.5f, 1f);
            Collider dwCol = darkWall.GetComponent<Collider>();
            if (dwCol != null) dwCol.enabled = false;
            Renderer dwRen = darkWall.GetComponent<Renderer>();
            if (dwRen != null)
            {
                Material blackMat = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
                blackMat.color = Color.black;
                dwRen.sharedMaterial = blackMat;
            }

            // Khung gỗ chống cửa hầm (2 cột Support1 + 1 xà ngang Support2) ngay miệng hầm Z = +2.2
            if (pSupport1 != null)
            {
                GameObject postL = InstantiatePrefabSafe(pSupport1, mouthGroup.transform);
                postL.name = "Mouth_Support_L";
                postL.transform.localPosition = new Vector3(-1.55f, 0f, 2.2f);
                postL.transform.localRotation = Quaternion.identity;

                GameObject postR = InstantiatePrefabSafe(pSupport1, mouthGroup.transform);
                postR.name = "Mouth_Support_R";
                postR.transform.localPosition = new Vector3(1.55f, 0f, 2.2f);
                postR.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
            if (pSupport2 != null)
            {
                GameObject beam = InstantiatePrefabSafe(pSupport2, mouthGroup.transform);
                beam.name = "Mouth_Support_TopBeam";
                beam.transform.localPosition = new Vector3(0f, 3.85f, 2.2f);
                beam.transform.localRotation = Quaternion.Euler(90f, 0f, -90f);
            }

            // Đá tảng che viền phía trên & 2 bên miệng hầm cho hòa vào vách đất tự nhiên
            if (pRock1 != null)
            {
                GameObject rockTop = InstantiatePrefabSafe(pRock1, mouthGroup.transform);
                rockTop.name = "Mouth_OverhangRock";
                rockTop.transform.localPosition = new Vector3(0f, 4.7f, 3.5f);
                rockTop.transform.localRotation = Quaternion.Euler(8f, 20f, 5f);
                rockTop.transform.localScale = new Vector3(1.4f, 0.7f, 1.1f);

                GameObject rockL = InstantiatePrefabSafe(pRock1, mouthGroup.transform);
                rockL.name = "Mouth_SideRock_L";
                rockL.transform.localPosition = new Vector3(-3.2f, 1.4f, 2.8f);
                rockL.transform.localRotation = Quaternion.Euler(0f, 40f, 0f);
                rockL.transform.localScale = new Vector3(0.85f, 1.1f, 0.9f);

                GameObject rockR = InstantiatePrefabSafe(pRock1, mouthGroup.transform);
                rockR.name = "Mouth_SideRock_R";
                rockR.transform.localPosition = new Vector3(3.2f, 1.4f, 2.8f);
                rockR.transform.localRotation = Quaternion.Euler(0f, -35f, 0f);
                rockR.transform.localScale = new Vector3(0.85f, 1.1f, 0.9f);
            }

            // Ván gỗ dẫn lối, thùng gỗ và cuốc chim dưới đáy hố
            if (pPlanks != null)
            {
                GameObject pl1 = InstantiatePrefabSafe(pPlanks, mouthGroup.transform);
                pl1.name = "PitFloor_Planks_1";
                pl1.transform.localPosition = new Vector3(0f, 0.05f, 0.3f);
                pl1.transform.localRotation = Quaternion.Euler(0f, 4f, 0f);

                GameObject pl2 = InstantiatePrefabSafe(pPlanks, mouthGroup.transform);
                pl2.name = "PitFloor_Planks_2";
                pl2.transform.localPosition = new Vector3(0.1f, 0.05f, 2.2f);
                pl2.transform.localRotation = Quaternion.Euler(0f, -3f, 0f);
            }
            if (pCrate != null)
            {
                GameObject cr = InstantiatePrefabSafe(pCrate, mouthGroup.transform);
                cr.name = "PitFloor_Crate";
                cr.transform.localPosition = new Vector3(-2.1f, 0f, 0.8f);
                cr.transform.localRotation = Quaternion.Euler(0f, 22f, 0f);
            }
            if (pPickaxe != null)
            {
                GameObject pk = InstantiatePrefabSafe(pPickaxe, mouthGroup.transform);
                pk.name = "PitFloor_Pickaxe";
                pk.transform.localPosition = new Vector3(-1.55f, 0.45f, 1.2f);
                pk.transform.localRotation = Quaternion.Euler(12f, 40f, 78f);
            }

            // 2 Đèn bão Realtime chiếu sáng rõ rệt cửa hầm dưới đáy hố (nhìn từ trên mặt đất thấy ngay!)
            if (pLantern != null)
            {
                SpawnRealtimeLantern(pLantern, mouthGroup.transform, new Vector3(-1.45f, 2.3f, 2.1f), 14f, 2.5f, new Color(1.0f, 0.72f, 0.38f), "Mouth_Lantern_Left");
                SpawnRealtimeLantern(pLantern, mouthGroup.transform, new Vector3(1.35f, 2.2f, 5.5f), 11f, 1.8f, new Color(1.0f, 0.65f, 0.32f), "Mouth_Lantern_Inside");
            }
        }

        // =========================================================================
        // 2. VIỀN ĐÁ TẢNG QUANH MÉP NGOÀI HỐ (CÁCH TÂM 10M - HOÀN TOÀN THOÁNG RỖNG)
        // =========================================================================
        private void SetupOuterRimRocksAndSignpost(GameObject root)
        {
            GameObject rimGroup = new GameObject("OuterRim_NaturalRocks");
            rimGroup.transform.SetParent(root.transform, false);

            GameObject pRock1 = TheForestMineCavern.LoadMinePrefab("Rocks", "Rock1");
            GameObject pRock2 = TheForestMineCavern.LoadMinePrefab("Rocks", "Rock2");
            GameObject pLantern = TheForestMineCavern.LoadMinePrefab("Props", "Lantern");
            GameObject pCrate = TheForestMineCavern.LoadMinePrefab("Props", "Crate");

            int count = 12;
            for (int i = 0; i < count; i++)
            {
                float ang = i * (360f / count);
                // Chừa lối đi rộng rãi phía -Z (góc 235 -> 305 độ) để Player đi bộ xuống hố
                if (ang > 235f && ang < 305f) continue;

                float rad = ang * Mathf.Deg2Rad;
                float r = pitOuterRadius + 0.8f + ((i % 2 == 0) ? 0.5f : 0f);
                Vector3 localPos = new Vector3(Mathf.Cos(rad) * r, -0.3f, Mathf.Sin(rad) * r);

                // Bám sát độ cao Terrain tại điểm mép ngoài
                if (Terrain.activeTerrain != null)
                {
                    Vector3 worldP = transform.TransformPoint(localPos);
                    float ty = Terrain.activeTerrain.SampleHeight(worldP) + Terrain.activeTerrain.transform.position.y;
                    localPos.y = (ty - transform.position.y) - 0.35f;
                }

                GameObject prefab = (i % 3 == 0) ? pRock2 : pRock1;
                if (prefab != null)
                {
                    GameObject rock = InstantiatePrefabSafe(prefab, rimGroup.transform);
                    rock.name = "OuterRim_Rock_" + i;
                    rock.transform.localPosition = localPos;
                    rock.transform.localRotation = Quaternion.Euler(0f, ang + 25f, 0f);
                    float scale = (prefab == pRock2) ? 0.55f : 0.72f;
                    rock.transform.localScale = new Vector3(scale, scale * 0.65f, scale);
                }
            }

            // Đánh dấu đầu lối đi xuống hố (phía -Z, cách tâm 12m) bằng thùng gỗ + đèn bão dẫn đường
            Vector3 trailheadPos = new Vector3(-2.6f, 0f, -12.0f);
            if (Terrain.activeTerrain != null)
            {
                Vector3 wp = transform.TransformPoint(trailheadPos);
                trailheadPos.y = Terrain.activeTerrain.SampleHeight(wp) + Terrain.activeTerrain.transform.position.y - transform.position.y;
            }
            if (pCrate != null)
            {
                GameObject markerCrate = InstantiatePrefabSafe(pCrate, rimGroup.transform);
                markerCrate.name = "Trailhead_Crate";
                markerCrate.transform.localPosition = trailheadPos;
                markerCrate.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
            }
            if (pLantern != null)
            {
                SpawnRealtimeLantern(pLantern, rimGroup.transform, trailheadPos + new Vector3(0f, 0.82f, 0f), 10f, 2.0f, new Color(1.0f, 0.75f, 0.40f), "Trailhead_Lantern");
            }
        }

        // =========================================================================
        // 3. LÀN SƯƠNG LẠNH BỐC RA TỪ CỬA HẦM ĐÁY HỐ
        // =========================================================================
        private void SetupCaveDraftParticles(GameObject root, float floorY)
        {
            GameObject draftObj = new GameObject("Cave_ColdMistDraft");
            draftObj.transform.SetParent(root.transform, false);
            draftObj.transform.localPosition = new Vector3(0f, floorY + 1.2f, 3.0f);
            draftObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            ParticleSystem ps = draftObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 4.0f;
            main.startSpeed = 0.6f;
            main.startSize = new ParticleSystem.MinMaxCurve(1.0f, 1.8f);
            main.startColor = new Color(0.65f, 0.80f, 0.90f, 0.12f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 24;

            var emission = ps.emission;
            emission.rateOverTime = 5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1.2f;
        }

        // =========================================================================
        // 4. TRIGGER BƯỚC VÀO CỬA HẦM & TRIGGER NHẤN [E]
        // =========================================================================
        private void SetupInteractionAndWalkInTriggers(GameObject root, float floorY)
        {
            // Điểm đứng an toàn ngoài trời khi trèo thang từ hang lên (ngay đáy hố, trước cửa hầm, nhìn ra lối đi lên)
            GameObject exteriorSpawn = new GameObject("ExteriorSpawnPoint");
            exteriorSpawn.transform.SetParent(root.transform, false);
            exteriorSpawn.transform.localPosition = new Vector3(0f, floorY + 0.3f, -2.5f);
            exteriorSpawn.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            exteriorSpawnPoint = exteriorSpawn.transform;

            GameObject interiorSpawn = new GameObject("InteriorSpawnPoint_Default");
            interiorSpawn.transform.SetParent(root.transform, false);
            interiorSpawn.transform.localPosition = new Vector3(0f, -60f, 0f);
            interiorSpawnPoint = interiorSpawn.transform;

            // TRIGGER 1: Đứng ngay miệng hầm nhấn [E] để vào hang
            GameObject interactTrigger = new GameObject("Entrance_InteractiveTrigger");
            interactTrigger.transform.SetParent(root.transform, false);
            interactTrigger.transform.localPosition = new Vector3(0f, floorY + 1.8f, 2.2f);

            BoxCollider box = interactTrigger.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(3.6f, 3.6f, 3.2f);

            caveEntranceComponent = interactTrigger.AddComponent<CaveEntrance>();
            caveEntranceComponent.entranceType = EntranceType.Entrance;
            caveEntranceComponent.autoTriggerOnFall = false;
            caveEntranceComponent.promptEnter = string.Format("Nhấn [E] để đi sâu vào {0}", caveName);
            caveEntranceComponent.spawnPointOutside = exteriorSpawnPoint;
            caveEntranceComponent.spawnPointInside = interiorSpawnPoint;

            // TRIGGER 2: Đi thẳng sâu vào trong cửa hầm tối -> Tự động chuyển cảnh vào Hầm Mỏ Ngầm!
            GameObject walkInTrigger = new GameObject("Shaft_FallThroughDescentTrigger");
            walkInTrigger.transform.SetParent(root.transform, false);
            walkInTrigger.transform.localPosition = new Vector3(0f, floorY + 2.0f, 6.2f);

            BoxCollider walkBox = walkInTrigger.AddComponent<BoxCollider>();
            walkBox.isTrigger = true;
            walkBox.size = new Vector3(3.8f, 4.0f, 5.0f);

            fallEntranceComponent = walkInTrigger.AddComponent<CaveEntrance>();
            fallEntranceComponent.entranceType = EntranceType.Entrance;
            fallEntranceComponent.autoTriggerOnFall = true;
            fallEntranceComponent.spawnPointInside = interiorSpawnPoint;
            fallEntranceComponent.spawnPointOutside = exteriorSpawnPoint;

            CaveZone zone = GetComponentInParent<CaveZone>() ?? FindObjectOfType<CaveZone>();
            if (zone != null)
            {
                caveEntranceComponent.targetCave = zone;
                fallEntranceComponent.targetCave = zone;
            }
        }

        public void LinkToMineCavern(TheForestMineCavern mineCavern, CaveZone zone)
        {
            if (mineCavern == null) return;

            if (mineCavern.interiorSpawnPoint != null)
            {
                interiorSpawnPoint = mineCavern.interiorSpawnPoint;
                if (caveEntranceComponent != null)
                {
                    caveEntranceComponent.spawnPointInside = interiorSpawnPoint;
                    if (zone != null) caveEntranceComponent.targetCave = zone;
                }
                if (fallEntranceComponent != null)
                {
                    fallEntranceComponent.spawnPointInside = interiorSpawnPoint;
                    if (zone != null) fallEntranceComponent.targetCave = zone;
                }
            }

            if (mineCavern.exitEntranceComponent != null && exteriorSpawnPoint != null)
            {
                mineCavern.exitEntranceComponent.spawnPointOutside = exteriorSpawnPoint;
                if (zone != null) mineCavern.exitEntranceComponent.targetCave = zone;
            }
        }

        private void SetupAdjacentSignalSmoke()
        {
            string smokeObjName = string.Format("[Cave{0}_RedSmokeSignal]", caveID);
            Transform existingSmoke = transform.Find(smokeObjName);
            CaveSignalSmoke signal = existingSmoke != null ? existingSmoke.GetComponent<CaveSignalSmoke>() : null;

            // Đặt cột khói đỏ ngay đầu đường dốc đi xuống hố sâu (-3.2, 0, -12) để Player đi theo khói là nhìn thẳng xuống hố!
            Vector3 smokePos = entranceWorldPosition + new Vector3(-3.2f, 0f, -12.0f);
            if (Terrain.activeTerrain != null)
            {
                smokePos.y = Terrain.activeTerrain.SampleHeight(smokePos) + Terrain.activeTerrain.transform.position.y;
            }

            if (signal == null)
            {
                GameObject smokeObj = new GameObject(smokeObjName);
                smokeObj.transform.SetParent(transform);
                smokeObj.transform.position = smokePos;
                signal = smokeObj.AddComponent<CaveSignalSmoke>();
                signal.caveName = string.Format("Hang {0} - {1}", caveID, caveName);
                signal.caveEntrancePos = smokePos;
                signal.SetupSmokeSystems();
            }
            else
            {
                signal.transform.position = smokePos;
                signal.caveName = string.Format("Hang {0} - {1}", caveID, caveName);
                signal.caveEntrancePos = smokePos;
                signal.SetupSmokeSystems();
            }
        }

        private static GameObject SpawnRealtimeLantern(GameObject pLantern, Transform parent, Vector3 localPos, float range, float intensity, Color color, string name)
        {
            GameObject lantern = InstantiatePrefabSafe(pLantern, parent);
            lantern.name = name;
            lantern.transform.localPosition = localPos;

            Light lgt = lantern.GetComponentInChildren<Light>();
            if (lgt == null) lgt = lantern.AddComponent<Light>();
            lgt.type = LightType.Point;
            lgt.enabled = true;
            lgt.color = color;
            lgt.range = range;
            lgt.intensity = intensity;
            lgt.shadows = LightShadows.None;
#if UNITY_EDITOR
            lgt.lightmapBakeType = LightmapBakeType.Realtime;
#endif
            return lantern;
        }

        private static GameObject InstantiatePrefabSafe(GameObject prefab, Transform parent)
        {
            GameObject obj;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            else
                obj = Instantiate(prefab);
#else
            obj = Instantiate(prefab);
#endif
            obj.transform.SetParent(parent, false);
            return obj;
        }

        [System.Serializable]
        public struct ForestCavePreset
        {
            public int id;
            public string name;
            public Vector3 pos;
            public Color ropeColor;
            public string desc;

            public ForestCavePreset(int id, string name, Vector3 pos, Color ropeColor, string desc)
            {
                this.id = id;
                this.name = name;
                this.pos = pos;
                this.ropeColor = ropeColor;
                this.desc = desc;
            }
        }
    }
}

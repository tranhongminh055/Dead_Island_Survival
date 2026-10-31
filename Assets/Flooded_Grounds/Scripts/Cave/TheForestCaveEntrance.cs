using UnityEngine;
using System.Collections.Generic;

namespace HorrorGame.Cave
{
    /// <summary>
    /// TheForestCaveEntrance — Dựng cấu trúc Cửa Hang Động chuẩn 100% phong cách THE FOREST (như ảnh tham chiếu):
    /// Hỗ trợ cả 6 hang động rải rác trên toàn bộ hòn đảo với thiết kế tự nhiên, chuẩn tỷ lệ người thật (~2.5m).
    /// 1. Tỷ lệ chuẩn người thật (~2.5m - 2.8m): Không bị to khổng lồ như vách núi, vừa vặn tầm mắt.
    /// 2. Vòm đá tảng tự nhiên (CobbleRocks) bọc rêu phong, xếp chồng tạo miệng vòm tự nhiên.
    /// 3. Hố đen sâu thẳm (Cavern Void) dẫn thẳng xuống lòng đất tối đen như mực.
    /// 4. Bục ván gỗ cổ xưa gác ngang ngưỡng cửa hố.
    /// 5. Dây thừng leo núi (Climbing Rope) buộc từ mỏm đá vắt qua ván gỗ thõng dài xuống hố sâu.
    /// 6. Thảm thực vật rừng bao quanh thay đổi linh hoạt theo từng khu vực (rừng rậm, đầm lầy, vách biển, núi lửa...).
    /// 7. Mỗi hang có Cột Khói Đỏ và Điểm Đánh Dấu riêng trên màn hình để người chơi định vị từ xa.
    /// 8. Trigger tương tác phím [E] để trèo dây thừng vào lòng hang.
    /// </summary>
    [ExecuteInEditMode]
    public class TheForestCaveEntrance : MonoBehaviour
    {
        public static List<TheForestCaveEntrance> AllEntrances = new List<TheForestCaveEntrance>();
        public static TheForestCaveEntrance Instance;

        [Header("── Thông Tin Cửa Hang ──")]
        public int caveID = 1;
        public string caveName = "Hang Sụp Đổ (The Collapsed Den)";

        [Tooltip("Tọa độ cửa hang sâu trong rừng rậm thoáng đãng Tây Bắc (Cách xa khu nhà gỗ / ban công)")]
        public Vector3 entranceWorldPosition = new Vector3(310f, 17.5f, 500f);

        [Header("── Tùy Chỉnh Kích Thước (Chuẩn Người Thật ~2.6m) ──")]
        public float entranceScale = 1.0f;

        [Header("── Màu Dây Thừng ──")]
        public Color ropeColor = new Color(0.95f, 0.85f, 0.12f); // Mặc định vàng chanh The Forest

        private GameObject moundRoot;
        public CaveEntrance caveEntranceComponent;
        public Transform exteriorSpawnPoint;
        public Transform interiorSpawnPoint;

        public static readonly ForestCavePreset[] DefaultCavePresets = new ForestCavePreset[]
        {
            new ForestCavePreset(1, "Hang Sụp Đổ (The Collapsed Den)", new Vector3(310f, 17.5f, 500f), new Color(0.95f, 0.85f, 0.12f), "Hang động nhỏ trong rừng rậm Tây Bắc. Nơi bắt đầu học cách sinh tồn dưới lòng đất."),
            new ForestCavePreset(2, "Hang Rễ Cây (The Root Cavern)", new Vector3(320f, 22.5f, 660f), new Color(0.95f, 0.65f, 0.15f), "Hang sâu dưới gốc cây cổ thụ khổng lồ phía Bắc đảo. Mê cung rễ cây và suối ngầm 2 tầng."),
            new ForestCavePreset(3, "Hang Thủy Triều (The Tidal Grotto)", new Vector3(450f, 18.0f, 725f), new Color(0.20f, 0.85f, 0.95f), "Hang vách đá biển phía Bắc. Mực nước dâng theo thủy triều ban đêm và có bẫy nước dâng."),
            new ForestCavePreset(4, "Hang Đầm Lầy (The Swamp Abyss)", new Vector3(630f, 18.5f, 490f), new Color(0.95f, 0.25f, 0.15f), "Tổ lớn của bộ tộc Thổ Dân Ăn Thịt Người phía Đông. Đầy cọc nhọn cạm bẫy bí mật."),
            new ForestCavePreset(5, "Hang Nham Thạch (The Volcanic Vent)", new Vector3(550f, 32.0f, 360f), new Color(1.00f, 0.55f, 0.10f), "Hang dung nham sườn núi lửa phía Nam. Chứa Lò Rèn Cổ Đại để chế tạo vũ khí tối thượng."),
            new ForestCavePreset(6, "Lõi Ký Sinh Ngầm (The Parasite Core - BOSS)", new Vector3(580f, 19.5f, 420f), new Color(0.25f, 0.95f, 0.45f), "Sào huyệt tối tăm dưới thung lũng bí ẩn. Nơi trú ngụ của Nữ Hoàng Ký Sinh (Trùm Cuối).")
        };

        private void OnEnable()
        {
            if (!AllEntrances.Contains(this)) AllEntrances.Add(this);
            if (Instance == null || caveID == 1) Instance = this;

            CheckAndAutoRelocateCave1();
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
            CheckAndAutoRelocateCave1();
            BuildForestCaveEntranceModel();
        }

        private bool hasAutoRebuilt = false;

        private void Update()
        {
            // Tự động di dời Cửa Hang 1 ra khỏi khu nhà gỗ nếu đang bị kẹt ở ban công/nhà sàn tọa độ cũ (385, 575)
            CheckAndAutoRelocateCave1();

            // Tự động dựng lại ngay lập tức trong frame đầu tiên sau khi code reload
            if (!hasAutoRebuilt)
            {
                hasAutoRebuilt = true;
                BuildForestCaveEntranceModel();
            }

            // Phím [F4]: Làm mới lại cấu trúc cửa hang và dọn quang cây cối ngay tức thì
            if (Input.GetKeyDown(KeyCode.F4))
            {
                BuildForestCaveEntranceModel();
                Debug.Log(string.Format("⛰️ [TheForestCaveEntrance] Phím [F4]: Đã làm mới cửa hang {0} và dọn quang toàn bộ mặt tiền!", caveID));
            }
        }

        /// <summary>
        /// Tự động di dời Hang 1 nếu phát hiện đang nằm trên nhà sàn / ban công cũ (385, 575)
        /// </summary>
        private void CheckAndAutoRelocateCave1()
        {
            if (caveID == 1)
            {
                bool atOldVilla = Vector3.Distance(transform.position, new Vector3(385f, 16.76f, 575f)) < 25f ||
                                  Vector3.Distance(entranceWorldPosition, new Vector3(385f, 21.5f, 575f)) < 25f;

                if (transform.parent != null && transform.parent.name.Contains("Cave1"))
                {
                    if (Vector3.Distance(transform.parent.position, new Vector3(385f, 16.76f, 575f)) < 25f ||
                        Vector3.Distance(transform.parent.position, new Vector3(385f, 21.5f, 575f)) < 25f)
                    {
                        atOldVilla = true;
                        transform.parent.position = new Vector3(310f, 17.5f, 500f);
                        transform.parent.rotation = Quaternion.Euler(0f, 75f, 0f);
                    }
                }

                if (atOldVilla)
                {
                    entranceWorldPosition = new Vector3(310f, 17.5f, 500f);
                    transform.position = entranceWorldPosition;
                    transform.rotation = Quaternion.Euler(0f, 75f, 0f);
                    BuildForestCaveEntranceModel();
                    Debug.Log("🌲 [TheForestCaveEntrance] Đã tự động dời Hang 1 khỏi khu nhà gỗ sang rừng thoáng mát (310, 17.5, 500)!");
                }
            }
        }

        /// <summary>
        /// Tự động dựng toàn bộ mô hình 3D cửa hang hầm mỏ chuẩn kích thước người thật (~2.6m)
        /// </summary>
        public void BuildForestCaveEntranceModel()
        {
            // Tự động căn chỉnh độ cao Y khít mặt đất rừng bằng Terrain
            if (Terrain.activeTerrain != null)
            {
                float terrainY = Terrain.activeTerrain.SampleHeight(entranceWorldPosition) + Terrain.activeTerrain.transform.position.y;
                entranceWorldPosition.y = Mathf.Max(terrainY, 16.2f);
            }

            transform.position = entranceWorldPosition;
            if (caveID == 1 && transform.rotation == Quaternion.identity)
            {
                transform.rotation = Quaternion.Euler(0f, 75f, 0f);
            }

            // Xóa cấu trúc cũ nếu có để dựng mới hoàn toàn
            Transform existingMound = transform.Find("CaveEntrance_ForestMound");
            if (existingMound != null)
            {
                if (Application.isPlaying) Destroy(existingMound.gameObject);
                else DestroyImmediate(existingMound.gameObject);
            }

            moundRoot = new GameObject("CaveEntrance_ForestMound");
            moundRoot.transform.SetParent(transform);
            moundRoot.transform.localPosition = Vector3.zero;
            moundRoot.transform.localRotation = Quaternion.identity;
            moundRoot.transform.localScale = Vector3.one * entranceScale;

            // 1. TẠO CỔNG HẦM MỎ MINE (MINE TIMBER PORTAL & TUNNEL - CHUẨN TỶ LỆ NGƯỜI THẬT ~2.6m, KHÔNG CÓ ĐÁ CAO CHÓT VÓT)
            SetupMineEntrancePortal(moundRoot);

            // 2. TẠO KHÔNG GIAN TỐI HUN HÚT TRONG HẦM (DARK CAVERN TUNNEL VOID)
            SetupCavernAbyssVoid(moundRoot);

            // 3. TẠO CÁC TẤM VÁN GỖ THỀM CỬA HẦM (WOODEN PLANKS TỪ MINE ASSET)
            SetupWoodenThresholdPlanks(moundRoot);

            // 4. DÂY THỪNG LEO HANG (CLIMBING ROPE THE FOREST)
            SetupClimbingRope(moundRoot);

            // 5. CÂY CỐI & HOA CỎ RỪNG RẬM BAO QUANH CỬA HẦM
            SetupForestSurroundings(moundRoot);

            // 6. LÀN HƠI SƯƠNG LẠNH BỐC TỪ TRONG HẦM RA (COLD MIST)
            SetupCaveDraftParticles(moundRoot);

            // 7. GẮN TRIGGER TƯƠNG TÁC PHÍM [E]
            SetupInteractionTrigger(moundRoot);

            // 8. CẬP NHẬT CỘT KHÓI ĐỎ BÁO HIỆU RIÊNG CHO HANG NÀY
            SetupAdjacentSignalSmoke();

            // 9. TỰ ĐỘNG XÂY DỰNG HẦM MỎ NGẦM MINE (NẾU CHƯA CÓ)
            EnsureSubterraneanMineCavern();

            Debug.Log(string.Format("⛰️ [TheForestCaveEntrance] Đã dựng thành công Cửa Hầm Mỏ {0} ({1}) tại tọa độ: {2}", caveID, caveName, entranceWorldPosition));
        }

        // =========================================================================
        // 1. DỰNG CỔNG HẦM MỎ BẰNG ASSET MINE (CHUẨN TỶ LỆ NGƯỜI THẬT ~2.6m - 2.8m)
        // DÙNG TRỰC TIẾP Support1, Tunnel1, Lantern, Pickaxe, Crate TỪ MINE ASSET!
        // TUYỆT ĐỐI KHÔNG DÙNG ĐÁ TẢNG KHỔNG LỒ CAO CHÓT VÓT LÊN TRỜI!
        // =========================================================================
        private void SetupMineEntrancePortal(GameObject root)
        {
            GameObject group = new GameObject("Mine_Entrance_Portal");
            group.transform.SetParent(root.transform);
            group.transform.localPosition = Vector3.zero;
            group.transform.localRotation = Quaternion.identity;

            // 1. ĐOẠN ĐƯỜNG HẦM ĐÁ MINE (Tunnel1 - Chiều cao ~2.8m, rộng ~3.0m chuẩn người thật, có MeshCollider rỗng đi xuyên qua được)
            GameObject tunnelPrefab = LoadMinePrefab("Base", "Tunnel1");
            if (tunnelPrefab != null)
            {
                GameObject tunnel = Instantiate(tunnelPrefab);
                tunnel.name = "Entrance_StoneTunnel";
                tunnel.transform.SetParent(group.transform);
                tunnel.transform.localPosition = new Vector3(0f, 0f, 2.0f);
                tunnel.transform.localRotation = Quaternion.identity;
                tunnel.transform.localScale = Vector3.one;
            }

            // 2. KHUNG GỖ CỔNG MỎ MẶT TIỀN (Support1 - 2 cột gỗ đứng + 1 xà ngang + thanh giằng chéo)
            GameObject supportPrefab = LoadMinePrefab("Props", "Support1");
            if (supportPrefab != null)
            {
                GameObject frontSupport = Instantiate(supportPrefab);
                frontSupport.name = "Entrance_FrontTimberPortal";
                frontSupport.transform.SetParent(group.transform);
                frontSupport.transform.localPosition = new Vector3(0f, 0f, 0.05f);
                frontSupport.transform.localRotation = Quaternion.identity;
                frontSupport.transform.localScale = Vector3.one;
            }

            // 3. KHUNG GỖ PHÍA TRONG HẦM (Support2 - Tạo chiều sâu hầm mỏ bỏ hoang)
            GameObject support2Prefab = LoadMinePrefab("Props", "Support2") ?? supportPrefab;
            if (support2Prefab != null)
            {
                GameObject innerSupport = Instantiate(support2Prefab);
                innerSupport.name = "Entrance_InnerTimberSupport";
                innerSupport.transform.SetParent(group.transform);
                innerSupport.transform.localPosition = new Vector3(0f, 0f, 3.8f);
                innerSupport.transform.localRotation = Quaternion.identity;
                innerSupport.transform.localScale = Vector3.one;
            }

            // 4. ĐÈN BÃO MINE TREO TRÊN KHUNG GỖ (Có sẵn Point Light ấm áp và Lửa cháy bập bùng)
            GameObject lanternPrefab = LoadMinePrefab("Props", "Lantern");
            if (lanternPrefab != null)
            {
                GameObject lantern = Instantiate(lanternPrefab);
                lantern.name = "Entrance_Lantern";
                lantern.transform.SetParent(group.transform);
                lantern.transform.localPosition = new Vector3(-1.05f, 2.05f, -0.1f);
                lantern.transform.localRotation = Quaternion.identity;

                Light lanternLight = lantern.GetComponentInChildren<Light>();
                if (lanternLight != null)
                {
                    lanternLight.color = Color.Lerp(new Color(1.0f, 0.75f, 0.4f), ropeColor, 0.3f);
                    lanternLight.range = 10f;
                    lanternLight.intensity = 2.5f;
                }
            }

            // 5. CÔNG CỤ KHAI KHOÁNG (Cuốc chim tựa vào vách gỗ)
            GameObject pickaxePrefab = LoadMinePrefab("Props", "Pickaxe");
            if (pickaxePrefab != null)
            {
                GameObject pickaxe = Instantiate(pickaxePrefab);
                pickaxe.name = "Entrance_Pickaxe";
                pickaxe.transform.SetParent(group.transform);
                pickaxe.transform.localPosition = new Vector3(1.15f, 0.45f, -0.05f);
                pickaxe.transform.localRotation = Quaternion.Euler(15f, 0f, -22f);
            }

            // 6. THÙNG GỖ TIẾP TẾ ĐẶT NÉP BÊN CỔNG
            GameObject cratePrefab = LoadMinePrefab("Props", "Crate");
            if (cratePrefab != null)
            {
                GameObject crate = Instantiate(cratePrefab);
                crate.name = "Entrance_SupplyCrate";
                crate.transform.SetParent(group.transform);
                crate.transform.localPosition = new Vector3(1.4f, 0.25f, 0.6f);
                crate.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
            }

            // 7. ĐÁ TẢNG SÁT ĐẤT (CHỈ CAO ~0.5m ÔM CHÂN CỘT GỖ - TUYỆT ĐỐI KHÔNG CÓ ĐÁ CAO CHÓT VÓT LÊN TRỜI)
            GameObject rock1Prefab = LoadMinePrefab("Rocks", "Rock1");
            GameObject rock2Prefab = LoadMinePrefab("Rocks", "Rock2") ?? rock1Prefab;

            if (rock1Prefab != null)
            {
                // Chân cột trái: khối đá nhỏ ôm sát đất
                GameObject rLeft = Instantiate(rock1Prefab);
                rLeft.name = "GroundRock_LeftBase";
                rLeft.transform.SetParent(group.transform);
                rLeft.transform.localPosition = new Vector3(-1.55f, 0.1f, -0.1f);
                rLeft.transform.localRotation = Quaternion.Euler(5f, 35f, 0f);
                rLeft.transform.localScale = new Vector3(0.65f, 0.55f, 0.65f); // Chiều cao chỉ ~0.5m!

                // Chân cột phải: khối đá nhỏ ôm sát đất
                GameObject rRight = Instantiate(rock2Prefab);
                rRight.name = "GroundRock_RightBase";
                rRight.transform.SetParent(group.transform);
                rRight.transform.localPosition = new Vector3(1.55f, 0.1f, -0.1f);
                rRight.transform.localRotation = Quaternion.Euler(5f, -30f, 0f);
                rRight.transform.localScale = new Vector3(0.65f, 0.55f, 0.65f); // Chiều cao chỉ ~0.5m!
            }

            // 8. VÁCH ĐÁ TỐI ĐẰNG SAU ĐOẠN HẦM (Tạo cảm giác đường hầm sâu hun hút)
            GameObject darkBackdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
            darkBackdrop.name = "Tunnel_DarkAbyssBackdrop";
            darkBackdrop.transform.SetParent(group.transform);
            darkBackdrop.transform.localPosition = new Vector3(0f, 1.35f, 4.0f);
            darkBackdrop.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            darkBackdrop.transform.localScale = new Vector3(3.2f, 2.7f, 1f);
            Collider quadCol = darkBackdrop.GetComponent<Collider>();
            if (quadCol != null) quadCol.enabled = false;
            Renderer quadRen = darkBackdrop.GetComponent<Renderer>();
            if (quadRen != null)
            {
                Shader unlit = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                Material mat = new Material(unlit);
                mat.color = Color.black;
                quadRen.sharedMaterial = mat;
            }
        }

        private GameObject LoadRockPrefab(string name)
        {
            GameObject prefab = Resources.Load<GameObject>("Rocks/" + name);
            if (prefab == null) prefab = Resources.Load<GameObject>(name);
#if UNITY_EDITOR
            if (prefab == null)
            {
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/Resources/Rocks/" + name + ".prefab");
            }
            if (prefab == null)
            {
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/Prefabs/Nature/Rocks/" + name + ".prefab");
            }
#endif
            return prefab;
        }

        private GameObject LoadNaturePrefab(string subFolder, string name)
        {
            GameObject prefab = Resources.Load<GameObject>(subFolder + "/" + name);
            if (prefab == null) prefab = Resources.Load<GameObject>(name);
#if UNITY_EDITOR
            if (prefab == null)
            {
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/Prefabs/Nature/" + subFolder + "/" + name + ".prefab");
            }
#endif
            return prefab;
        }

        private GameObject LoadMinePrefab(string subFolder, string name)
        {
            GameObject prefab = null;
#if UNITY_EDITOR
            string path = "Assets/Mine/Prefabs/" + subFolder + "/" + name + ".prefab";
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
#endif
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>("Mine/Prefabs/" + subFolder + "/" + name);
            }
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>(name);
            }
            return prefab;
        }

        // =========================================================================
        // 2. DỰNG KHÔNG GIAN TỐI HUN HÚT TRONG HẦM (DARK CAVERN TUNNEL VOID)
        // =========================================================================
        private void SetupCavernAbyssVoid(GameObject root)
        {
            // Đèn lồng / ánh sáng ấm áp chiếu rọi miệng hang và dây thừng
            GameObject lanternObj = new GameObject("Cave_EntranceBeaconLight");
            lanternObj.transform.SetParent(root.transform);
            lanternObj.transform.localPosition = new Vector3(0f, 1.2f, 0.5f);
            Light lgt = lanternObj.AddComponent<Light>();
            lgt.type = LightType.Point;
            lgt.color = new Color(1.0f, 0.72f, 0.35f);
            lgt.range = 9.5f;
            lgt.intensity = 1.8f;
        }

        // =========================================================================
        // 3. DỰNG VÁN GỖ THỀM CỬA HẦM (WOODEN PLANKS TỪ ASSET MINE)
        // =========================================================================
        private void SetupWoodenThresholdPlanks(GameObject root)
        {
            GameObject group = new GameObject("Wooden_Planks");
            group.transform.SetParent(root.transform);
            group.transform.localPosition = Vector3.zero;

            // Sử dụng trực tiếp Planks prefab từ Mine asset
            GameObject planksPrefab = LoadMinePrefab("Props", "Planks");
            if (planksPrefab != null)
            {
                GameObject p1 = Instantiate(planksPrefab);
                p1.name = "Entrance_FloorPlanks_1";
                p1.transform.SetParent(group.transform);
                p1.transform.localPosition = new Vector3(0f, 0.03f, 0.6f);
                p1.transform.localRotation = Quaternion.identity;

                GameObject p2 = Instantiate(planksPrefab);
                p2.name = "Entrance_FloorPlanks_2";
                p2.transform.SetParent(group.transform);
                p2.transform.localPosition = new Vector3(0f, 0.03f, 2.2f);
                p2.transform.localRotation = Quaternion.identity;
            }
        }

        // =========================================================================
        // 4. DỰNG DÂY THỪNG LEO HANG (CLIMBING ROPE - THE FOREST)
        // =========================================================================
        private void SetupClimbingRope(GameObject root)
        {
            GameObject rope = new GameObject("TheForest_ClimbingRope");
            rope.transform.SetParent(root.transform);
            rope.transform.localPosition = Vector3.zero;

            LineRenderer line = rope.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.startWidth = 0.09f;
            line.endWidth = 0.07f;
            line.positionCount = 6;

            line.SetPosition(0, new Vector3(-0.95f, 2.45f, 0.05f)); // Buộc vào xà ngang cổng gỗ
            line.SetPosition(1, new Vector3(-0.85f, 1.80f, 0.15f));
            line.SetPosition(2, new Vector3(-0.75f, 1.10f, 0.35f));
            line.SetPosition(3, new Vector3(-0.65f, 0.50f, 0.65f));
            line.SetPosition(4, new Vector3(-0.55f, 0.15f, 1.10f)); // Cuộn trên thềm ván gỗ
            line.SetPosition(5, new Vector3(-0.50f, 0.05f, 1.50f));

            Shader unlit = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            Material ropeMat = new Material(unlit);
            ropeMat.color = ropeColor;
            line.sharedMaterial = ropeMat;
        }

        // =========================================================================
        // 5. CÂY CỐI & HOA CỎ RỪNG RẬM BAO QUANH MIỆNG HANG (THEO TỪNG BIOME)
        // =========================================================================
        private void SetupForestSurroundings(GameObject root)
        {
            GameObject foliageGroup = new GameObject("Forest_Surroundings");
            foliageGroup.transform.SetParent(root.transform);
            foliageGroup.transform.localPosition = Vector3.zero;

            // 1. CÂY RỪNG BAO QUANH (gần hơn — vây quanh hang nhỏ)
            string treeNameA = (caveID == 4) ? "TreeCreator_Tall_C_Dead" : "TreeCreator_Tall_A";
            string treeNameB = (caveID == 2) ? "TreeCreator_Crinkly_A" : "TreeCreator_Tall_B";
            string treeNameC = "TreeCreator_Tall_C";

            GameObject treeTallA = LoadNaturePrefab("Trees", treeNameA) ?? LoadNaturePrefab("Trees", "TreeCreator_Tall_A");
            GameObject treeTallB = LoadNaturePrefab("Trees", treeNameB) ?? LoadNaturePrefab("Trees", "TreeCreator_Tall_B");
            GameObject treeTallC = LoadNaturePrefab("Trees", treeNameC) ?? LoadNaturePrefab("Trees", "TreeCreator_Tall_C");

            // 1. CÂY RỪNG BAO QUANH: Chỉ đặt ở phía SAU LƯNG và HAI BÊN XA (Mặt tiền trước cửa hang hoàn toàn thông thoáng)
            Vector3[] treeOffsets = new Vector3[]
            {
                new Vector3(-5.5f, 0f, 3.5f),   // Sau lưng bên trái
                new Vector3( 5.5f, 0f, 3.0f),   // Sau lưng bên phải
                new Vector3(-5.8f, 0f, 0.5f),   // Sườn xa bên trái
                new Vector3( 5.8f, 0f, 0.5f)    // Sườn xa bên phải
            };

            GameObject[] treePrefabs = new GameObject[] { treeTallA, treeTallB, treeTallC, treeTallA };

            for (int t = 0; t < treeOffsets.Length; t++)
            {
                if (treePrefabs[t] != null)
                {
                    GameObject tree = Instantiate(treePrefabs[t]);
                    tree.name = "SurroundingTree_" + t;
                    tree.transform.SetParent(foliageGroup.transform);
                    Vector3 worldPos = transform.position + treeOffsets[t];
                    if (Terrain.activeTerrain != null)
                    {
                        worldPos.y = Terrain.activeTerrain.SampleHeight(worldPos) + Terrain.activeTerrain.transform.position.y;
                    }
                    tree.transform.position = worldPos;
                    tree.transform.rotation = Quaternion.Euler(0f, t * 85f + caveID * 20f, 0f);
                }
            }

            // 2. BỤI CÂY RỪNG: Đặt nép sau lưng hai bên vách đá, dọn sạch hoàn toàn lối vào phía trước
            GameObject bushA = LoadNaturePrefab("Bushes", "DecoBush_A") ?? LoadNaturePrefab("Bushes", "DecoBush_B");
            if (bushA != null)
            {
                GameObject bL = Instantiate(bushA);
                bL.name = "Bush_LeftBack";
                bL.transform.SetParent(foliageGroup.transform);
                bL.transform.localPosition = new Vector3(-2.8f, 0f, 1.8f);
                bL.transform.localScale = Vector3.one * 0.5f;

                GameObject bR = Instantiate(bushA);
                bR.name = "Bush_RightBack";
                bR.transform.SetParent(foliageGroup.transform);
                bR.transform.localPosition = new Vector3(2.8f, 0f, 1.8f);
                bR.transform.localScale = Vector3.one * 0.5f;
            }

            // 3. KHÓM HOA DẠI RỪNG TRẮNG ĐẶC TRƯNG (ẢNH 2)
            SetupWhiteForestWildflowers(foliageGroup, new Vector3(0.15f, 0.03f, -0.85f));

            // 4. CỎ RỪNG ĐIỂM XUYẾT (gần hơn)
            GameObject grassPrefab = LoadNaturePrefab("Grass", "Grass_Med_A") ?? LoadNaturePrefab("Grass", "Grass_Small_A");
            if (grassPrefab != null)
            {
                Vector3[] grassOffsets = new Vector3[]
                {
                    new Vector3(-0.8f, 0f, -0.7f),
                    new Vector3( 0.75f, 0f, -0.7f),
                    new Vector3(-1.2f, 0f,  0.15f),
                    new Vector3( 1.2f, 0f,  0.15f)
                };

                for (int g = 0; g < grassOffsets.Length; g++)
                {
                    GameObject grass = Instantiate(grassPrefab);
                    grass.name = "Grass_" + g;
                    grass.transform.SetParent(foliageGroup.transform);
                    grass.transform.localPosition = grassOffsets[g];
                    grass.transform.localRotation = Quaternion.Euler(0f, g * 70f, 0f);
                    grass.transform.localScale = Vector3.one * 0.75f;
                }
            }
        }

        /// <summary>
        /// Tạo cụm hoa dại rừng (cánh hoa trắng, nhị vàng, lá xanh hình sao) y hệt như Ảnh 2
        /// </summary>
        private void SetupWhiteForestWildflowers(GameObject parent, Vector3 localPos)
        {
            GameObject plantObj = new GameObject("Wildflower_Herb_Photo2");
            plantObj.transform.SetParent(parent.transform);
            plantObj.transform.localPosition = localPos;
            plantObj.transform.localRotation = Quaternion.Euler(0f, 25f + caveID * 15f, 0f);

            Material leafMat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            leafMat.color = (caveID == 6) ? new Color(0.18f, 0.40f, 0.32f) : new Color(0.22f, 0.48f, 0.16f);

            for (int i = 0; i < 5; i++)
            {
                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leaf.name = "Leaf_" + i;
                leaf.transform.SetParent(plantObj.transform);
                float angle = i * 72f;
                leaf.transform.localRotation = Quaternion.Euler(15f, angle, 0f);
                leaf.transform.localPosition = Quaternion.Euler(0, angle, 0) * new Vector3(0, 0.06f, 0.14f);
                leaf.transform.localScale = new Vector3(0.08f, 0.015f, 0.22f);

                Collider c = leaf.GetComponent<Collider>();
                if (c != null) c.enabled = false;
                Renderer r = leaf.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = leafMat;
            }

            Material petalMat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            petalMat.color = (caveID == 5) ? new Color(1.0f, 0.85f, 0.6f) : new Color(0.96f, 0.98f, 0.96f);
            Material centerMat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            centerMat.color = (caveID == 6) ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.95f, 0.85f, 0.15f);

            for (int f = 0; f < 3; f++)
            {
                Vector3 flowerOffset = (f == 0) ? new Vector3(0f, 0.16f, 0.05f) :
                                       (f == 1) ? new Vector3(-0.09f, 0.14f, 0.12f) :
                                                  new Vector3( 0.09f, 0.14f, 0.12f);

                GameObject flower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flower.name = "FlowerPetal_" + f;
                flower.transform.SetParent(plantObj.transform);
                flower.transform.localPosition = flowerOffset;
                flower.transform.localScale = new Vector3(0.08f, 0.02f, 0.08f);
                Collider fc = flower.GetComponent<Collider>();
                if (fc != null) fc.enabled = false;
                Renderer fr = flower.GetComponent<Renderer>();
                if (fr != null) fr.sharedMaterial = petalMat;

                GameObject center = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                center.name = "FlowerCenter_" + f;
                center.transform.SetParent(plantObj.transform);
                center.transform.localPosition = flowerOffset + new Vector3(0, 0.012f, 0);
                center.transform.localScale = new Vector3(0.028f, 0.02f, 0.028f);
                Collider cc = center.GetComponent<Collider>();
                if (cc != null) cc.enabled = false;
                Renderer cr = center.GetComponent<Renderer>();
                if (cr != null) cr.sharedMaterial = centerMat;
            }
        }

        // =========================================================================
        // 6. LÀN SƯƠNG LẠNH THỔI TỪ HANG (COLD DRAFT PARTICLES)
        // =========================================================================
        private void SetupCaveDraftParticles(GameObject root)
        {
            GameObject draftObj = new GameObject("Cave_ColdMistDraft");
            draftObj.transform.SetParent(root.transform);
            draftObj.transform.localPosition = new Vector3(0f, 0.25f, 0.3f);
            draftObj.transform.localRotation = Quaternion.Euler(-30f, 0f, 0f);

            ParticleSystem ps = draftObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 3.0f;
            main.startSpeed = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            
            // Màu sương thay đổi nhẹ theo tính chất hang
            if (caveID == 5) main.startColor = new Color(0.8f, 0.4f, 0.2f, 0.15f); // Khói dung nham cam
            else if (caveID == 6) main.startColor = new Color(0.3f, 0.8f, 0.4f, 0.15f); // Bào tử ký sinh xanh
            else main.startColor = new Color(0.6f, 0.8f, 0.9f, 0.12f); // Sương lạnh tự nhiên

            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 20;

            var emission = ps.emission;
            emission.rateOverTime = 4f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f;
        }

        // =========================================================================
        // 7. TRIGGER TƯƠNG TÁC PHÍM [E]
        // =========================================================================
        private void SetupInteractionTrigger(GameObject root)
        {
            GameObject triggerObj = new GameObject("Entrance_InteractiveTrigger");
            triggerObj.transform.SetParent(root.transform);
            triggerObj.transform.localPosition = new Vector3(0f, 1.2f, 1.2f);

            BoxCollider box = triggerObj.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2.8f, 2.6f, 2.8f);
            box.center = Vector3.zero;

            caveEntranceComponent = triggerObj.AddComponent<CaveEntrance>();
            caveEntranceComponent.promptEnter = string.Format("Nhấn [E] để khám phá {0}", caveName);

            // Spawn Points: Mặt đất bên ngoài cách cổng hầm ~3.0m
            GameObject exteriorSpawn = new GameObject("ExteriorSpawnPoint");
            exteriorSpawn.transform.SetParent(triggerObj.transform);
            exteriorSpawn.transform.localPosition = new Vector3(0f, -1.1f, -3.0f);
            exteriorSpawn.transform.localRotation = Quaternion.identity;
            exteriorSpawnPoint = exteriorSpawn.transform;
            caveEntranceComponent.spawnPointOutside = exteriorSpawnPoint;

            // Spawn Point tạm thời dưới lòng đất (-60m)
            GameObject interiorSpawn = new GameObject("InteriorSpawnPoint_Default");
            interiorSpawn.transform.SetParent(triggerObj.transform);
            interiorSpawn.transform.localPosition = new Vector3(0f, -60f, 0f);
            interiorSpawnPoint = interiorSpawn.transform;
            caveEntranceComponent.spawnPointInside = interiorSpawnPoint;

            CaveZone zone = GetComponentInParent<CaveZone>() ?? FindObjectOfType<CaveZone>();
            if (zone != null && caveEntranceComponent != null)
            {
                caveEntranceComponent.targetCave = zone;
            }
        }

        public void EnsureSubterraneanMineCavern()
        {
            Transform caveRoot = transform.parent != null ? transform.parent : transform;
            TheForestMineCavern mineCavern = caveRoot.GetComponentInChildren<TheForestMineCavern>();
            if (mineCavern == null)
            {
                GameObject mineObj = new GameObject("TheForest_MineCavern");
                mineObj.transform.SetParent(caveRoot);
                mineCavern = mineObj.AddComponent<TheForestMineCavern>();
            }

            mineCavern.caveID = caveID;
            mineCavern.caveName = caveName;
            mineCavern.entranceSurfacePos = entranceWorldPosition;
            mineCavern.themeColor = ropeColor;
            mineCavern.parentZone = GetComponentInParent<CaveZone>();
            mineCavern.BuildMineCavern();

            // Liên kết điểm spawn và exit trigger 2 chiều
            if (caveEntranceComponent != null && mineCavern.interiorSpawnPoint != null)
            {
                interiorSpawnPoint = mineCavern.interiorSpawnPoint;
                caveEntranceComponent.spawnPointInside = interiorSpawnPoint;
            }
            if (mineCavern.exitEntranceComponent != null && exteriorSpawnPoint != null)
            {
                mineCavern.exitEntranceComponent.spawnPointOutside = exteriorSpawnPoint;
            }
        }

        // =========================================================================
        // 8. CỘT KHÓI ĐỎ VÀ PHÁO SÁNG BÊN CẠNH VÒM ĐÁ
        // =========================================================================
        private void SetupAdjacentSignalSmoke()
        {
            string smokeObjName = string.Format("[Cave{0}_RedSmokeSignal]", caveID);
            Transform existingSmoke = transform.Find(smokeObjName);
            CaveSignalSmoke signal = existingSmoke != null ? existingSmoke.GetComponent<CaveSignalSmoke>() : null;

            // Đặt cột khói đỏ NGAY BÊN CẠNH CỔNG HẦM MỎ (lệch 2.2m bên phải, rất thoáng đãng)
            Vector3 smokePos = entranceWorldPosition + transform.TransformDirection(new Vector3(2.2f, 0f, -0.4f));
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureForestCaveAtRuntime()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu") return;

            // Vùng cấm: Khu nhà gỗ / lều trại player / nhà sàn ven đầm — hang KHÔNG ĐƯỢC spawn tại đây
            Vector3[] forbiddenZones = new Vector3[]
            {
                new Vector3(480f, 15f, 492f),   // Nhà gỗ chính
                new Vector3(480f, 15f, 500f),   // Khu vực spawn player
                new Vector3(475f, 15f, 488f),   // Khu vực lều trại
                new Vector3(485f, 15f, 495f),   // Bán kính mở rộng
                new Vector3(385f, 16.8f, 575f)  // Khu Villa2 nhà sàn ven đầm cũ
            };
            float forbiddenRadius = 60f; // Bán kính cấm rộng hơn

            // Kiểm tra xem đã có hệ thống hang chưa, nếu chưa có thì tạo đủ cả 6 hang The Forest
            for (int i = 0; i < DefaultCavePresets.Length; i++)
            {
                ForestCavePreset p = DefaultCavePresets[i];
                TheForestCaveEntrance found = null;
                foreach (var e in FindObjectsOfType<TheForestCaveEntrance>())
                {
                    if (e.caveID == p.id) { found = e; break; }
                }

                if (found == null)
                {
                    // Kiểm tra vị trí preset có nằm trong vùng cấm không
                    bool isForbidden = false;
                    for (int f = 0; f < forbiddenZones.Length; f++)
                    {
                        if (Vector3.Distance(p.pos, forbiddenZones[f]) < forbiddenRadius)
                        {
                            isForbidden = true;
                            break;
                        }
                    }

                    if (isForbidden)
                    {
                        Debug.LogWarning(string.Format("⚠️ [TheForestCaveEntrance] Hang {0} bị chặn vì nằm gần khu nhà! Bỏ qua.", p.id));
                        continue;
                    }

                    GameObject caveObj = new GameObject(string.Format("[TheForest_CaveEntrance_Hang{0}]", p.id));
                    caveObj.transform.position = p.pos;
                    TheForestCaveEntrance comp = caveObj.AddComponent<TheForestCaveEntrance>();
                    comp.caveID = p.id;
                    comp.caveName = p.name;
                    comp.entranceWorldPosition = p.pos;
                    comp.ropeColor = p.ropeColor;
                    comp.BuildForestCaveEntranceModel();
                    Debug.Log(string.Format("🎉 [TheForestCaveEntrance] Đã tự động tạo Hang {0} ({1}) tại {2}", p.id, p.name, p.pos));
                }
                else
                {
                    // Kiểm tra tất cả vùng cấm — nếu hang đang ở gần nhà → di dời ngay vào rừng
                    bool needRelocate = false;
                    for (int f = 0; f < forbiddenZones.Length; f++)
                    {
                        if (Vector3.Distance(found.transform.position, forbiddenZones[f]) < forbiddenRadius ||
                            Vector3.Distance(found.entranceWorldPosition, forbiddenZones[f]) < forbiddenRadius)
                        {
                            needRelocate = true;
                            break;
                        }
                    }

                    if (needRelocate)
                    {
                        found.entranceWorldPosition = p.pos;
                        found.transform.position = p.pos;
                        found.BuildForestCaveEntranceModel();
                        Debug.Log(string.Format("🌲 [TheForestCaveEntrance] Đã di dời Hang {0} từ gần nhà vào sâu trong rừng rậm!", p.id));
                    }
                }
            }
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

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

        [Tooltip("Tọa độ cửa hang sâu trong rừng rậm (Cách xa khu nhà gỗ ~120m)")]
        public Vector3 entranceWorldPosition = new Vector3(385f, 21.5f, 575f);

        [Header("── Tùy Chỉnh Kích Thước (Chuẩn Người Thật ~2.5m) ──")]
        public float entranceScale = 1.0f;

        [Header("── Màu Dây Thừng ──")]
        public Color ropeColor = new Color(0.95f, 0.85f, 0.12f); // Mặc định vàng chanh The Forest

        private GameObject moundRoot;
        private CaveEntrance caveEntranceComponent;

        public static readonly ForestCavePreset[] DefaultCavePresets = new ForestCavePreset[]
        {
            new ForestCavePreset(1, "Hang Sụp Đổ (The Collapsed Den)", new Vector3(385f, 21.5f, 575f), new Color(0.95f, 0.85f, 0.12f), "Hang động nhỏ trong rừng rậm Tây Bắc. Nơi bắt đầu học cách sinh tồn dưới lòng đất."),
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
            BuildForestCaveEntranceModel();
        }

        /// <summary>
        /// Tự động dựng toàn bộ mô hình 3D cửa hang The Forest chuẩn kích thước người thật
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

            // 1. TẠO VÒM ĐÁ TẢNG TỰ NHIÊN (CHUẨN KÍCH THƯỚC THE FOREST)
            SetupCaveArchRocks(moundRoot);

            // 2. TẠO HỐ ĐEN SÂU THẲM (DARK CAVERN VOID)
            SetupCavernAbyssVoid(moundRoot);

            // 3. TẠO CÁC TẤM VÁN GỖ GÁC MIỆNG HANG (WOODEN PLANKS)
            SetupWoodenThresholdPlanks(moundRoot);

            // 4. TẠO DÂY THỪNG LEO HANG MÀU ĐẶC TRƯNG (CLIMBING ROPE)
            SetupClimbingRope(moundRoot);

            // 5. TẠO CÂY CỐI & HOA CỎ RỪNG RẬM BAO QUANH MIỆNG HANG (THEO TỪNG BIOME)
            SetupForestSurroundings(moundRoot);

            // 6. TẠO LÀN HƠI SƯƠNG LẠNH BỐC TỪ TRONG HANG RA (COLD MIST)
            SetupCaveDraftParticles(moundRoot);

            // 7. GẮN TRIGGER TƯƠNG TÁC PHÍM [E]
            SetupInteractionTrigger(moundRoot);

            // 8. CẬP NHẬT CỘT KHÓI ĐỎ BÁO HIỆU RIÊNG CHO HANG NÀY
            SetupAdjacentSignalSmoke();

            Debug.Log(string.Format("⛰️ [TheForestCaveEntrance] Đã dựng thành công Cửa Hang {0} ({1}) tại tọa độ: {2}", caveID, caveName, entranceWorldPosition));
        }

        // =========================================================================
        // 1. DỰNG VÒM ĐÁ TẢNG TỰ NHIÊN CHUẨN TỶ LỆ (THE FOREST ROCK ARCH)
        // =========================================================================
        private void SetupCaveArchRocks(GameObject root)
        {
            GameObject group = new GameObject("Arch_Rocks");
            group.transform.SetParent(root.transform);
            group.transform.localPosition = Vector3.zero;
            group.transform.localRotation = Quaternion.identity;

            // Ưu tiên dùng CobbleRocks (tảng đá vừa có rêu phong) để đạt tỉ lệ chuẩn The Forest (~2m)
            GameObject rockPillarLeft = LoadRockPrefab("CobbleRock_A") ?? LoadRockPrefab("CobbleRock_B");
            GameObject rockPillarRight = LoadRockPrefab("CobbleRock_C") ?? LoadRockPrefab("CobbleRock_D");
            GameObject rockArchTop = LoadRockPrefab("CobbleRock_E") ?? LoadRockPrefab("CobbleRock_B");
            GameObject rockBackDome = LoadRockPrefab("CobbleRock_B") ?? LoadRockPrefab("CobbleRock_A");
            GameObject rockWingL = LoadRockPrefab("CobbleRock_D") ?? LoadRockPrefab("CobbleRock_F");
            GameObject rockWingR = LoadRockPrefab("CobbleRock_F") ?? LoadRockPrefab("CobbleRock_C");
            GameObject cobbleSmall = LoadRockPrefab("CobbleRock_A") ?? LoadRockPrefab("CobbleRock_E");

            // Độ xoay ngẫu nhiên nhẹ theo ID hang để mỗi hang có nét độc đáo tự nhiên
            float rotSeed = (caveID * 17f) % 30f - 15f;

            // 1. Đá trụ vách TRÁI (Cao ~2.0m, nghiêng vào giữa vòm)
            SpawnRockPiece(group, rockPillarLeft, new Vector3(-1.15f, 0.85f, 0.0f), new Vector3(-8f, 25f + rotSeed, -10f), new Vector3(1.6f, 1.95f, 1.6f), "Rock_LeftWall");

            // 2. Đá trụ vách PHẢI (Cao ~2.0m, nghiêng đón đầu vách trái)
            SpawnRockPiece(group, rockPillarRight, new Vector3(1.15f, 0.85f, 0.0f), new Vector3(-10f, -30f + rotSeed, 10f), new Vector3(1.6f, 1.95f, 1.6f), "Rock_RightWall");

            // 3. Đá vòm NÓC (Bắc ngang đỉnh 2 trụ đá, tạo vòm cung cao ~2.4m - 2.6m)
            SpawnRockPiece(group, rockArchTop, new Vector3(0.0f, 2.15f, -0.05f), new Vector3(15f, 85f + rotSeed, 0f), new Vector3(1.85f, 1.25f, 1.7f), "Rock_TopArch");

            // 4. Đá gò bọc lưng phía sau (Tạo thành gò đồi đất đá tự nhiên dốc thoai thoải)
            SpawnRockPiece(group, rockBackDome, new Vector3(0f, 0.95f, 1.35f), new Vector3(-12f, 0f, 0f), new Vector3(2.2f, 1.75f, 1.8f), "Rock_BackDome");

            // 5. Đá kè sườn bên trái
            SpawnRockPiece(group, rockWingL, new Vector3(-1.75f, 0.5f, 0.7f), new Vector3(8f, 40f + rotSeed, 0f), new Vector3(1.3f, 1.4f, 1.3f), "Rock_LeftWing");

            // 6. Đá kè sườn bên phải
            SpawnRockPiece(group, rockWingR, new Vector3(1.75f, 0.5f, 0.7f), new Vector3(8f, -40f + rotSeed, 0f), new Vector3(1.3f, 1.4f, 1.3f), "Rock_RightWing");

            // 7. Vành đá cuội nhỏ tự nhiên viền xung quanh mép cửa miệng hố
            Vector3[] rimPositions = new Vector3[]
            {
                new Vector3(-1.45f, 0.06f, -0.65f),
                new Vector3(-1.0f,  0.06f, -1.05f),
                new Vector3(-0.4f,  0.04f, -1.25f),
                new Vector3( 0.4f,  0.04f, -1.25f),
                new Vector3( 1.0f,  0.06f, -1.05f),
                new Vector3( 1.45f, 0.06f, -0.65f),
                new Vector3(-1.85f, 0.08f, -0.2f),
                new Vector3( 1.85f, 0.08f, -0.2f)
            };

            for (int i = 0; i < rimPositions.Length; i++)
            {
                SpawnRockPiece(group, cobbleSmall, rimPositions[i], new Vector3(Random.Range(-12, 12), i * 45f, 0f), new Vector3(0.65f, 0.55f, 0.65f), "CobbleRim_" + i);
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

        private void SpawnRockPiece(GameObject parent, GameObject prefab, Vector3 localPos, Vector3 localRot, Vector3 localScale, string name)
        {
            GameObject rock;
            if (prefab != null)
            {
                rock = Instantiate(prefab);
            }
            else
            {
                rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Renderer r = rock.GetComponent<Renderer>();
                if (r != null)
                {
                    Material mat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
                    // Tông màu đá tùy biến nhẹ theo hang
                    if (caveID == 5) mat.color = new Color(0.22f, 0.20f, 0.20f); // Nham thạch bazan đen sẫm
                    else if (caveID == 6) mat.color = new Color(0.25f, 0.35f, 0.28f); // Ký sinh xanh rêu
                    else mat.color = new Color(0.38f, 0.40f, 0.35f); // Đá rêu phong The Forest
                    r.sharedMaterial = mat;
                }
            }

            rock.name = name;
            rock.transform.SetParent(parent.transform);
            rock.transform.localPosition = localPos;
            rock.transform.localRotation = Quaternion.Euler(localRot);
            rock.transform.localScale = localScale;
        }

        // =========================================================================
        // 2. DỰNG HỐ ĐEN SÂU THẲM (DARK CAVERN VOID)
        // =========================================================================
        private void SetupCavernAbyssVoid(GameObject root)
        {
            GameObject voidObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            voidObj.name = "Cavern_DarkAbyssVoid";
            voidObj.transform.SetParent(root.transform);
            voidObj.transform.localPosition = new Vector3(0f, -0.3f, 0.6f);
            voidObj.transform.localRotation = Quaternion.Euler(50f, 0f, 0f);
            voidObj.transform.localScale = new Vector3(1.6f, 2.2f, 1.6f);

            Collider col = voidObj.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Renderer r = voidObj.GetComponent<Renderer>();
            if (r != null)
            {
                Shader unlitShader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                Material blackVoidMat = new Material(unlitShader);
                blackVoidMat.color = new Color(0.01f, 0.01f, 0.01f, 1f);
                r.sharedMaterial = blackVoidMat;
            }
        }

        // =========================================================================
        // 3. DỰNG VÁN GỖ GÁC NGANG MIỆNG HANG (WOODEN PLANKS)
        // =========================================================================
        private void SetupWoodenThresholdPlanks(GameObject root)
        {
            GameObject group = new GameObject("Wooden_Planks");
            group.transform.SetParent(root.transform);
            group.transform.localPosition = Vector3.zero;

            Vector3[] plankPos = new Vector3[]
            {
                new Vector3(-0.2f,  0.05f, -0.3f),
                new Vector3( 0.15f, 0.06f, -0.55f),
                new Vector3(-0.05f, 0.07f, -0.8f)
            };

            Vector3[] plankRot = new Vector3[]
            {
                new Vector3(0f, 7f, 0f),
                new Vector3(0f, -4f, 0f),
                new Vector3(0f, 3f, 0f)
            };

            Vector3[] plankScale = new Vector3[]
            {
                new Vector3(1.25f, 0.06f, 0.28f),
                new Vector3(1.35f, 0.06f, 0.30f),
                new Vector3(1.15f, 0.06f, 0.25f)
            };

            Material woodMat = CreateWoodMaterial();

            for (int i = 0; i < plankPos.Length; i++)
            {
                GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.name = "Plank_" + i;
                plank.transform.SetParent(group.transform);
                plank.transform.localPosition = plankPos[i];
                plank.transform.localRotation = Quaternion.Euler(plankRot[i]);
                plank.transform.localScale = plankScale[i];

                Renderer pr = plank.GetComponent<Renderer>();
                if (pr != null && woodMat != null) pr.sharedMaterial = woodMat;
            }
        }

        private Material CreateWoodMaterial()
        {
            Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            if (s == null) return null;
            Material m = new Material(s);
            m.color = new Color(0.40f, 0.27f, 0.16f); // Nâu gỗ mộc cổ xưa
            return m;
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
            line.startWidth = 0.07f;
            line.endWidth = 0.06f;
            line.positionCount = 7;

            line.SetPosition(0, new Vector3(-1.0f,  0.95f, -0.25f)); // Neo vách đá trái
            line.SetPosition(1, new Vector3(-0.65f, 0.42f, -0.30f));
            line.SetPosition(2, new Vector3(-0.35f, 0.11f, -0.35f)); // Tựa lên tấm ván gỗ
            line.SetPosition(3, new Vector3( 0.0f,  0.08f, -0.20f)); // Mép ngưỡng hố
            line.SetPosition(4, new Vector3( 0.12f, -0.30f, 0.25f)); // Rơi vào hố sâu
            line.SetPosition(5, new Vector3( 0.15f, -1.20f, 0.80f));
            line.SetPosition(6, new Vector3( 0.18f, -2.40f, 1.50f));

            Shader unlit = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            Material ropeMat = new Material(unlit);
            ropeMat.color = ropeColor;
            line.sharedMaterial = ropeMat;

            GameObject knot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            knot.name = "Rope_AnchorKnot";
            knot.transform.SetParent(rope.transform);
            knot.transform.localPosition = new Vector3(-1.0f, 0.95f, -0.25f);
            knot.transform.localScale = Vector3.one * 0.18f;
            Collider kc = knot.GetComponent<Collider>();
            if (kc != null) kc.enabled = false;
            Renderer kr = knot.GetComponent<Renderer>();
            if (kr != null) kr.sharedMaterial = ropeMat;
        }

        // =========================================================================
        // 5. CÂY CỐI & HOA CỎ RỪNG RẬM BAO QUANH MIỆNG HANG (THEO TỪNG BIOME)
        // =========================================================================
        private void SetupForestSurroundings(GameObject root)
        {
            GameObject foliageGroup = new GameObject("Forest_Surroundings");
            foliageGroup.transform.SetParent(root.transform);
            foliageGroup.transform.localPosition = Vector3.zero;

            // 1. CÂY RỪNG BAO QUANH
            string treeNameA = (caveID == 4) ? "TreeCreator_Tall_C_Dead" : "TreeCreator_Tall_A";
            string treeNameB = (caveID == 2) ? "TreeCreator_Crinkly_A" : "TreeCreator_Tall_B";
            string treeNameC = "TreeCreator_Tall_C";

            GameObject treeTallA = LoadNaturePrefab("Trees", treeNameA) ?? LoadNaturePrefab("Trees", "TreeCreator_Tall_A");
            GameObject treeTallB = LoadNaturePrefab("Trees", treeNameB) ?? LoadNaturePrefab("Trees", "TreeCreator_Tall_B");
            GameObject treeTallC = LoadNaturePrefab("Trees", treeNameC) ?? LoadNaturePrefab("Trees", "TreeCreator_Tall_C");

            Vector3[] treeOffsets = new Vector3[]
            {
                new Vector3(-6.5f, 0f, 3.5f),
                new Vector3( 6.8f, 0f, 3.0f),
                new Vector3(-4.8f, 0f, -6.5f),
                new Vector3( 5.5f, 0f, -7.0f)
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

            // 2. BỤI CÂY RỪNG
            GameObject bushA = LoadNaturePrefab("Bushes", "DecoBush_A") ?? LoadNaturePrefab("Bushes", "DecoBush_B");
            if (bushA != null)
            {
                GameObject bL = Instantiate(bushA);
                bL.name = "Bush_Left";
                bL.transform.SetParent(foliageGroup.transform);
                bL.transform.localPosition = new Vector3(-2.2f, 0f, -0.6f);
                bL.transform.localScale = Vector3.one * 0.85f;

                GameObject bR = Instantiate(bushA);
                bR.name = "Bush_Right";
                bR.transform.SetParent(foliageGroup.transform);
                bR.transform.localPosition = new Vector3(2.1f, 0f, -0.7f);
                bR.transform.localScale = Vector3.one * 0.85f;
            }

            // 3. KHÓM HOA DẠI RỪNG TRẮNG ĐẶC TRƯNG (ẢNH 2)
            SetupWhiteForestWildflowers(foliageGroup, new Vector3(0.25f, 0.05f, -1.35f));

            // 4. CỎ RỪNG ĐIỂM XUYẾT
            GameObject grassPrefab = LoadNaturePrefab("Grass", "Grass_Med_A") ?? LoadNaturePrefab("Grass", "Grass_Small_A");
            if (grassPrefab != null)
            {
                Vector3[] grassOffsets = new Vector3[]
                {
                    new Vector3(-1.3f, 0f, -1.1f),
                    new Vector3( 1.2f, 0f, -1.1f),
                    new Vector3(-2.0f, 0f,  0.2f),
                    new Vector3( 2.0f, 0f,  0.2f)
                };

                for (int g = 0; g < grassOffsets.Length; g++)
                {
                    GameObject grass = Instantiate(grassPrefab);
                    grass.name = "Grass_" + g;
                    grass.transform.SetParent(foliageGroup.transform);
                    grass.transform.localPosition = grassOffsets[g];
                    grass.transform.localRotation = Quaternion.Euler(0f, g * 70f, 0f);
                    grass.transform.localScale = Vector3.one * 0.9f;
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
            draftObj.transform.localPosition = new Vector3(0f, 0.4f, 0.5f);
            draftObj.transform.localRotation = Quaternion.Euler(-30f, 0f, 0f);

            ParticleSystem ps = draftObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 3.5f;
            main.startSpeed = 0.8f;
            main.startSize = new ParticleSystem.MinMaxCurve(1.0f, 1.8f);
            
            // Màu sương thay đổi nhẹ theo tính chất hang
            if (caveID == 5) main.startColor = new Color(0.8f, 0.4f, 0.2f, 0.15f); // Khói dung nham cam
            else if (caveID == 6) main.startColor = new Color(0.3f, 0.8f, 0.4f, 0.15f); // Bào tử ký sinh xanh
            else main.startColor = new Color(0.6f, 0.8f, 0.9f, 0.12f); // Sương lạnh tự nhiên

            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 30;

            var emission = ps.emission;
            emission.rateOverTime = 6f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.6f;
        }

        // =========================================================================
        // 7. TRIGGER TƯƠNG TÁC PHÍM [E]
        // =========================================================================
        private void SetupInteractionTrigger(GameObject root)
        {
            GameObject triggerObj = new GameObject("Entrance_InteractiveTrigger");
            triggerObj.transform.SetParent(root.transform);
            triggerObj.transform.localPosition = new Vector3(0f, 0.5f, -0.4f);

            BoxCollider box = triggerObj.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(3.2f, 2.8f, 3.2f);
            box.center = new Vector3(0f, 0.8f, 0f);

            caveEntranceComponent = triggerObj.AddComponent<CaveEntrance>();
            caveEntranceComponent.promptEnter = string.Format("Nhấn [E] để trèo dây thừng vào {0}", caveName);

            // Spawn Points
            GameObject interiorSpawn = new GameObject("InteriorSpawnPoint");
            interiorSpawn.transform.SetParent(triggerObj.transform);
            interiorSpawn.transform.localPosition = new Vector3(0f, 0f, 15f);
            caveEntranceComponent.spawnPointInside = interiorSpawn.transform;

            GameObject exteriorSpawn = new GameObject("ExteriorSpawnPoint");
            exteriorSpawn.transform.SetParent(triggerObj.transform);
            exteriorSpawn.transform.localPosition = new Vector3(0f, 0f, -3.5f);
            caveEntranceComponent.spawnPointOutside = exteriorSpawn.transform;

            CaveZone zone = GetComponentInParent<CaveZone>() ?? FindObjectOfType<CaveZone>();
            if (zone != null && caveEntranceComponent != null)
            {
                caveEntranceComponent.targetCave = zone;
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

            Vector3 smokePos = entranceWorldPosition + new Vector3(2.5f, 0f, -1.2f);
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
            }
            else
            {
                signal.transform.position = smokePos;
                signal.caveName = string.Format("Hang {0} - {1}", caveID, caveName);
                signal.caveEntrancePos = smokePos;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureForestCaveAtRuntime()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu") return;

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
                    // Nếu hang 1 còn sót ở tọa độ cũ trước nhà (480, 492) -> Di dời ngay vào rừng
                    if (p.id == 1 && (Vector3.Distance(found.transform.position, new Vector3(480f, 15f, 492f)) < 25f ||
                                      Vector3.Distance(found.entranceWorldPosition, new Vector3(480f, 15f, 492f)) < 25f))
                    {
                        found.entranceWorldPosition = p.pos;
                        found.transform.position = p.pos;
                        found.BuildForestCaveEntranceModel();
                        Debug.Log("🌲 [TheForestCaveEntrance] Đã di dời Hang 1 từ trước nhà vào sâu trong rừng rậm!");
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

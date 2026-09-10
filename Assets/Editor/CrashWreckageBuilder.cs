#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HorrorGame.Environment;
using HorrorGame.Cutscenes;
using HorrorGame.Inventory;

namespace HorrorGame.EditorTools
{
    public class CrashWreckageBuilder : EditorWindow
    {
        private const string CABIN_MATS_DIR = "Assets/Flooded_Grounds/Materials/Cabin";
        private const string TEXTURES_PATH   = "Assets/Flooded_Grounds/Materials/Cabin/Textures";
        private const string WRECK_MATS_DIR  = "Assets/Flooded_Grounds/Materials/Wreckage";

        [MenuItem("Horror Game/🔥 Cập Nhật Xác Máy Bay & Cột Khói Nghi Ngút (Crash Site & Smoke Plume)")]
        public static void BuildCrashSiteAndBeacon()
        {
            // 1. Kiểm tra hoặc xóa xác máy bay cũ nếu đã có
            GameObject oldWreckage = GameObject.Find("[AIRPLANE_CRASH_SITE]");
            if (oldWreckage != null)
            {
                Undo.DestroyObjectImmediate(oldWreckage);
            }

            EnsureDirectoriesExist();

            // 2. Tìm hoặc tạo vị trí máy bay rơi tại khu rừng
            Vector3 crashCenter = new Vector3(537f, 17.6f, 545f);
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                float terrainY = terrain.SampleHeight(crashCenter) + terrain.transform.position.y;
                crashCenter.y = terrainY;
            }

            // 3. Tải Textures chuẩn từ Cabin ban đầu (Economy Comfort)
            Texture2D texEconomyComfort = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES_PATH + "/Tex_EconomyComfort.png");
            Texture2D texSeatBlue       = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES_PATH + "/Tex_SeatFabric_Blue.png");
            Texture2D texAisleCarpet    = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES_PATH + "/Tex_AisleCarpet.png");

            // 4. Khởi tạo Materials ĐỒNG BỘ 100% VỚI CABIN MÁY BAY BAN ĐẦU
            Material matSeatBlue       = GetOrCreateMaterial(CABIN_MATS_DIR, "M_Plane_SeatBlue", new Color(0.18f, 0.35f, 0.72f), 0.05f, 0.18f, texSeatBlue);
            Material matSeatShell      = GetOrCreateMaterial(CABIN_MATS_DIR, "M_Plane_SeatShellWhite", new Color(0.94f, 0.95f, 0.96f), 0.08f, 0.45f);
            Material matHeadrestOrange = GetOrCreateMaterial(CABIN_MATS_DIR, "M_Plane_HeadrestOrange", Color.white, 0.04f, 0.22f, texEconomyComfort);
            Material matArmrestPad     = GetOrCreateMaterial(CABIN_MATS_DIR, "M_Plane_ArmrestPad", new Color(0.12f, 0.18f, 0.32f), 0.10f, 0.35f);
            Material matCarpetPeeling  = GetOrCreateMaterial(CABIN_MATS_DIR, "M_Plane_AisleCarpet", new Color(0.85f, 0.88f, 0.95f), 0.0f, 0.10f, texAisleCarpet);
            Material matInteriorWall   = GetOrCreateMaterial(CABIN_MATS_DIR, "M_Plane_InteriorWall", new Color(0.91f, 0.92f, 0.94f), 0.04f, 0.30f);
            Material matOverheadBin    = GetOrCreateMaterial(CABIN_MATS_DIR, "M_Plane_OverheadBin", new Color(0.93f, 0.94f, 0.95f), 0.06f, 0.48f);
            Material matMetal          = GetOrCreateMaterial(CABIN_MATS_DIR, "M_Plane_SeatMetal", new Color(0.82f, 0.84f, 0.87f), 0.88f, 0.75f);
            Material matGlass          = GetOrCreateGlassMaterial(CABIN_MATS_DIR, "M_Plane_WindowGlass", new Color(0.85f, 0.94f, 1f, 0.25f), 0.1f, 0.95f);
            Material matOxygenMask     = GetOrCreateMaterial(CABIN_MATS_DIR, "M_Plane_OxygenMask", new Color(1.0f, 0.82f, 0.15f), 0.05f, 0.35f);
            Material matOxygenTube     = GetOrCreateGlassMaterial(CABIN_MATS_DIR, "M_Plane_OxygenTube", new Color(0.9f, 0.98f, 0.92f, 0.45f), 0.05f, 0.85f);

            // Materials cháy sém cho phần vỡ toác
            Material matScorchedWhite  = GetOrCreateMaterial(WRECK_MATS_DIR, "M_Plane_ScorchedWhite", new Color(0.82f, 0.82f, 0.80f), 0.15f, 0.35f);
            Material matCharredMetal   = GetOrCreateMaterial(WRECK_MATS_DIR, "M_Plane_CharredBlack", new Color(0.12f, 0.12f, 0.13f), 0.65f, 0.20f);
            Material matWing           = GetOrCreateMaterial(WRECK_MATS_DIR, "M_Plane_WingMetal", new Color(0.80f, 0.82f, 0.85f), 0.80f, 0.60f);
            Material matBeaconOrange   = GetOrCreateMaterial(WRECK_MATS_DIR, "M_Plane_ELT_Orange", new Color(1.0f, 0.45f, 0.05f), 0.30f, 0.50f);
            Material matLuggageRed     = GetOrCreateMaterial(WRECK_MATS_DIR, "M_Plane_Luggage_Red", new Color(0.75f, 0.15f, 0.15f), 0.10f, 0.25f);
            Material matLuggageBrown   = GetOrCreateMaterial(WRECK_MATS_DIR, "M_Plane_Luggage_Brown", new Color(0.35f, 0.22f, 0.15f), 0.10f, 0.20f);
            Material matBeaconBeam     = GetOrCreateBeaconBeamMat();

            // 5. Tạo Root Container
            GameObject root = new GameObject("[AIRPLANE_CRASH_SITE]");
            root.transform.position = crashCenter;
            Undo.RegisterCreatedObjectUndo(root, "Create Airplane Crash Site & Beacon");

            AirplaneCrashSite crashSiteScript = root.AddComponent<AirplaneCrashSite>();

            // ═════════════════════════════════════════════════════════════════
            // PHẦN 1: THÂN KHOANG HÀNH KHÁCH BỊ XÉ TOẠC (TORN ECONOMY FUSELAGE)
            // ═════════════════════════════════════════════════════════════════
            GameObject cabinGroup = new GameObject("Broken_Economy_Fuselage");
            cabinGroup.transform.SetParent(root.transform, false);
            cabinGroup.transform.localPosition = new Vector3(-1.2f, 1.6f, 6.0f);
            cabinGroup.transform.localRotation = Quaternion.Euler(-5f, -20f, 16f); // Nghiêng toác một bên

            // Sàn khoang hành khách vỡ nát với thảm xanh hàng không và ray hợp kim
            GameObject floor = CreatePrimitive("Torn_Cabin_Floor", cabinGroup.transform, new Vector3(0, -1.1f, 0), new Vector3(4.2f, 0.22f, 9.0f), matCarpetPeeling, PrimitiveType.Cube);
            CreatePrimitive("SeatTrack_1", cabinGroup.transform, new Vector3(-1.2f, -0.98f, 0), new Vector3(0.05f, 0.02f, 8.8f), matMetal, PrimitiveType.Cube);
            CreatePrimitive("SeatTrack_2", cabinGroup.transform, new Vector3( 1.2f, -0.98f, 0), new Vector3(0.05f, 0.02f, 8.8f), matMetal, PrimitiveType.Cube);

            // Vách trái còn nguyên vẹn một phần: CÓ CÁC Ô CỬA SỔ BẦU DỤC ĐẶC TRƯNG CỦA MÁY BAY
            GameObject wallL = CreatePrimitive("Fuselage_Wall_L", cabinGroup.transform, new Vector3(-2.1f, 0.35f, 0), new Vector3(0.18f, 2.7f, 9.0f), matScorchedWhite, PrimitiveType.Cube);
            for (int w = 0; w < 5; w++)
            {
                float wz = -3.2f + (w * 1.6f);
                GameObject winObj = CreatePrimitive("Window_Bezel_" + w, cabinGroup.transform, new Vector3(-2.0f, 0.45f, wz), new Vector3(0.06f, 0.52f, 0.36f), matSeatShell, PrimitiveType.Cube);
                CreatePrimitive("Window_Glass_" + w, winObj.transform, Vector3.zero, new Vector3(1.1f, 0.85f, 0.80f), matGlass, PrimitiveType.Cube);
            }

            // Vách phải bị xé toạc, cháy đen với kim loại uốn cong
            GameObject wallR = CreatePrimitive("Torn_Wall_R", cabinGroup.transform, new Vector3(2.1f, -0.2f, 1.5f), new Vector3(0.20f, 1.8f, 5.5f), matCharredMetal, PrimitiveType.Cube);
            CreatePrimitive("Torn_Serrated_Edge", cabinGroup.transform, new Vector3(2.05f, 0.8f, 1.5f), new Vector3(0.12f, 0.35f, 5.4f), matCharredMetal, PrimitiveType.Cube);

            // Các đường khung sườn (Ribs) kim loại cong queo vắt qua trần
            for (int r = 0; r < 5; r++)
            {
                float rZ = -3.5f + (r * 1.8f);
                GameObject rib = CreatePrimitive("Structural_Rib_" + r, cabinGroup.transform, new Vector3(0, 1.45f, rZ), new Vector3(4.25f, 0.16f, 0.22f), matCharredMetal, PrimitiveType.Cube);
                rib.transform.localRotation = Quaternion.Euler(Random.Range(-8f, 8f), 0, Random.Range(-12f, 12f));
            }

            // Hộc hành lý (Overhead Bins) màu trắng méo mó treo bên vách, có hộc bung cửa
            GameObject binL = CreatePrimitive("Broken_Overhead_Bin", cabinGroup.transform, new Vector3(-1.3f, 1.15f, 0), new Vector3(0.80f, 0.40f, 8.5f), matOverheadBin, PrimitiveType.Cube);
            binL.transform.localRotation = Quaternion.Euler(0, 0, 8f);
            // Một số nắp hộc hành lý bật mở
            for (int b = 0; b < 4; b++)
            {
                float bz = -2.8f + (b * 1.8f);
                GameObject door = CreatePrimitive("Open_BinDoor_" + b, cabinGroup.transform, new Vector3(-0.92f, 1.0f, bz), new Vector3(0.04f, 0.35f, 0.70f), matSeatShell, PrimitiveType.Cube);
                door.transform.localRotation = Quaternion.Euler(0, 0, -35f);
            }

            // Mặt nạ oxy màu vàng rơi lủng lẳng từ trần
            for (int m = 0; m < 4; m++)
            {
                float mz = -3.0f + (m * 2.0f);
                CreatePrimitive("OxygenMask_Hanging_" + m, cabinGroup.transform, new Vector3(-0.4f, 0.4f, mz), new Vector3(0.12f, 0.15f, 0.10f), matOxygenMask, PrimitiveType.Cube);
                CreatePrimitive("OxygenTube_Hanging_" + m, cabinGroup.transform, new Vector3(-0.4f, 0.8f, mz), new Vector3(0.015f, 0.75f, 0.015f), matOxygenTube, PrimitiveType.Cube);
            }

            // ═════════════════════════════════════════════════════════════════
            // PHẦN 2: CÁC DÃY GHẾ ECONOMY COMFORT RƠI VÃI (Y HỆT CABIN BAN ĐẦU)
            // ═════════════════════════════════════════════════════════════════
            // Cụm 3 ghế Economy Comfort còn nằm trên sàn cabin bị nghiêng
            GameObject seatRowCabin = BuildWreckageTripleSeat(cabinGroup.transform, new Vector3(-0.6f, -0.65f, 0.8f),
                matSeatBlue, matSeatShell, matHeadrestOrange, matArmrestPad, matMetal);
            seatRowCabin.transform.localRotation = Quaternion.Euler(6f, 15f, -12f);

            // Cụm 2 ghế khác nằm chỏng chơ gần mép vỡ
            GameObject seatPair = BuildWreckageDoubleSeat(cabinGroup.transform, new Vector3(1.1f, -0.75f, -2.0f),
                matSeatBlue, matSeatShell, matHeadrestOrange, matArmrestPad, matMetal);
            seatPair.transform.localRotation = Quaternion.Euler(-18f, -40f, 25f);

            // Các ghế đơn lẻ bị văng ra bãi cỏ và bùn lầy bên ngoài
            Vector3[] scatteredSeatPos = new Vector3[]
            {
                new Vector3(4.2f, 0.35f, 2.2f),
                new Vector3(-4.5f, 0.30f, 4.0f),
                new Vector3(2.5f, 0.25f, -4.0f)
            };
            for (int s = 0; s < scatteredSeatPos.Length; s++)
            {
                GameObject singleSeat = BuildWreckageSingleSeat(root.transform, scatteredSeatPos[s], "Ejected_EconomySeat_" + s,
                    matSeatBlue, matSeatShell, matHeadrestOrange, matArmrestPad);
                singleSeat.transform.localRotation = Quaternion.Euler(Random.Range(-25f, 25f), Random.Range(0, 360f), Random.Range(-35f, 35f));
            }

            // ═════════════════════════════════════════════════════════════════
            // PHẦN 3: MŨI MÁY BAY & BUỒNG LÁI (COCKPIT SECTION)
            // ═════════════════════════════════════════════════════════════════
            GameObject cockpitGroup = new GameObject("Cockpit_Section");
            cockpitGroup.transform.SetParent(root.transform, false);
            cockpitGroup.transform.localPosition = new Vector3(0, 1.2f, -4f);
            cockpitGroup.transform.localRotation = Quaternion.Euler(14f, 15f, -8f);

            CreatePrimitive("Nose_Cone", cockpitGroup.transform, new Vector3(0, 0, 4.5f), new Vector3(3.2f, 2.6f, 3.5f), matCharredMetal, PrimitiveType.Cube);
            CreatePrimitive("Cockpit_Hull", cockpitGroup.transform, new Vector3(0, 0.3f, 1.5f), new Vector3(3.8f, 2.9f, 4.0f), matScorchedWhite, PrimitiveType.Cube);
            GameObject windshield = CreatePrimitive("Windshield", cockpitGroup.transform, new Vector3(0, 1.3f, 3.8f), new Vector3(2.4f, 0.9f, 0.2f), matCharredMetal, PrimitiveType.Cube);
            windshield.transform.localRotation = Quaternion.Euler(35f, 0, 0);

            // ═════════════════════════════════════════════════════════════════
            // PHẦN 4: CÁNH GÃY & ĐỘNG CƠ PHẢN LỰC (WING & JET TURBINE)
            // ═════════════════════════════════════════════════════════════════
            GameObject wingGroup = new GameObject("Snapped_Wing");
            wingGroup.transform.SetParent(root.transform, false);
            wingGroup.transform.localPosition = new Vector3(7.5f, 1.5f, 2.0f);
            wingGroup.transform.localRotation = Quaternion.Euler(32f, -40f, -25f);

            CreatePrimitive("Main_Wing_Slab", wingGroup.transform, Vector3.zero, new Vector3(3.8f, 0.35f, 13f), matWing, PrimitiveType.Cube);
            CreatePrimitive("Winglet_Tip", wingGroup.transform, new Vector3(1.8f, 0.6f, 6.2f), new Vector3(0.2f, 1.4f, 1.8f), matScorchedWhite, PrimitiveType.Cube);

            GameObject engine = CreatePrimitive("Jet_Turbine", root.transform, new Vector3(-6.5f, 0.9f, 1.5f), new Vector3(2.0f, 2.8f, 2.0f), matCharredMetal, PrimitiveType.Cylinder);
            engine.transform.localRotation = Quaternion.Euler(75f, 25f, 0);

            // ═════════════════════════════════════════════════════════════════
            // PHẦN 5: VALI HÀNH LÝ RƠI VÃI (LUGGAGE & CARGO)
            // ═════════════════════════════════════════════════════════════════
            for (int l = 0; l < 8; l++)
            {
                float angle = l * 45f * Mathf.Deg2Rad;
                float dist = Random.Range(4.5f, 10.5f);
                Vector3 lugPos = new Vector3(Mathf.Cos(angle) * dist, 0.2f, Mathf.Sin(angle) * dist);
                Material lugMat = (l % 2 == 0) ? matLuggageRed : matLuggageBrown;

                // Không đặt vali trúng khu vực bãi cỏ người chơi hồi tỉnh
                if (Vector3.Distance(lugPos, new Vector3(7.0f, 0.2f, -5.5f)) < 3.0f)
                {
                    lugPos += new Vector3(0, 0, -4.0f);
                }

                GameObject luggage = CreatePrimitive("Suitcase_" + l, root.transform, lugPos, new Vector3(0.55f, 0.25f, 0.4f), lugMat, PrimitiveType.Cube);
                luggage.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360f), Random.Range(-15f, 15f));
            }

            // ═════════════════════════════════════════════════════════════════
            // PHẦN 6: ÁNH LỬA BẬP BÙNG TRONG XÁC MÁY BAY (DYNAMIC FIRE)
            // ═════════════════════════════════════════════════════════════════
            Light fire1 = CreateFireLight(cabinGroup.transform, new Vector3(0, 0, 1.0f), new Color(1f, 0.45f, 0.1f), 12f, 2.8f);
            Light fire2 = CreateFireLight(cockpitGroup.transform, new Vector3(0, 0, 0), new Color(1f, 0.35f, 0.05f), 10f, 2.2f);
            crashSiteScript.fireFlickerLights = new Light[] { fire1, fire2 };

            // ═════════════════════════════════════════════════════════════════
            // PHẦN 7: CỘT KHÓI ĐEN CHÁY NGHI NGÚT BỐC LÊN BẦU TRỜI (BILLOWING SMOKE PLUME)
            // ═════════════════════════════════════════════════════════════════
            Material matSmoke = GetOrCreateSmokeMaterial();
            GameObject smokeRoot = new GameObject("Crash_Smoke_Plume_System");
            smokeRoot.transform.SetParent(cabinGroup.transform, false);
            smokeRoot.transform.localPosition = new Vector3(0, 0.4f, 1.2f);
            smokeRoot.transform.localRotation = Quaternion.Euler(-90f, 0, 0); // Phun thẳng đứng lên bầu trời

            // 1. Hệ thống hạt khói cuồn cuộn (Dynamic Particle System)
            ParticleSystem ps = smokeRoot.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psr = smokeRoot.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = matSmoke;
            psr.sortingFudge = 5;

            var psMain = ps.main;
            psMain.duration = 10f;
            psMain.loop = true;
            psMain.startLifetime = 14f;
            psMain.startSpeed = 8.5f;
            psMain.startSize = 4.0f;
            psMain.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2f);
            psMain.startColor = new Color(0.12f, 0.12f, 0.14f, 0.85f);
            psMain.simulationSpace = ParticleSystemSimulationSpace.World;
            psMain.maxParticles = 350;

            var psEmission = ps.emission;
            psEmission.rateOverTime = 25f;

            var psShape = ps.shape;
            psShape.shapeType = ParticleSystemShapeType.Cone;
            psShape.angle = 9.0f;
            psShape.radius = 1.2f;

            var psSize = ps.sizeOverLifetime;
            psSize.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1.0f);
            sizeCurve.AddKey(0.4f, 2.8f);
            sizeCurve.AddKey(1.0f, 4.8f);
            psSize.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

            var psColor = ps.colorOverLifetime;
            psColor.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.08f, 0.08f, 0.09f), 0.0f),
                    new GradientColorKey(new Color(0.18f, 0.18f, 0.20f), 0.45f),
                    new GradientColorKey(new Color(0.40f, 0.40f, 0.42f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.85f, 0.0f),
                    new GradientAlphaKey(0.70f, 0.55f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            psColor.color = grad;

            var psVel = ps.velocityOverLifetime;
            psVel.enabled = true;
            psVel.space = ParticleSystemSimulationSpace.World;
            psVel.x = new ParticleSystem.MinMaxCurve(0.8f);
            psVel.z = new ParticleSystem.MinMaxCurve(0.5f);

            // 2. Cột khói thể tích cao 140m vươn cao ngất trên ngọn cây (nhìn thấy từ mọi góc trên đảo)
            BuildVolumetricSmokePillar(smokeRoot.transform, matSmoke);

            // 3. Tàn lửa đỏ bay lơ lửng trong đám khói (Embers)
            BuildFireEmbers(smokeRoot.transform);

            // 4. Giữ nguyên thanh chỉ số (Stats Panel) tại vị trí nguyên bản ở giữa đáy màn hình
            RestoreStatsPanelOriginalPosition();

            // ═════════════════════════════════════════════════════════════════
            // PHẦN 8: ĐỒNG BỘ ĐIỂM TỈNH DẬY TRÊN BÃI CỎ THOÁNG (KHÔNG BỊ VƯỚNG)
            // ═════════════════════════════════════════════════════════════════
            Vector3 playerWakePos = SyncForestSpawnPointToWreckage(crashCenter, terrain);

            // Đặt các vật phẩm sinh tồn trên bãi cỏ gần điểm tỉnh dậy nhưng lệch sang một bên
            SpawnCrashSiteStarterItem("Assets/Flooded_Grounds/Prefabs/Pickups/Pickup_Medkit.prefab", root.transform, playerWakePos + new Vector3(-1.2f, 0, 1.8f), terrain);
            SpawnCrashSiteStarterItem("Assets/Flooded_Grounds/Prefabs/Pickups/Pickup_Flashlight.prefab", root.transform, playerWakePos + new Vector3(1.2f, 0, 1.5f), terrain);
            SpawnCrashSiteStarterItem("Assets/Flooded_Grounds/Prefabs/Pickups/Pickup_FoodRation.prefab", root.transform, playerWakePos + new Vector3(-0.8f, 0, 2.6f), terrain);

            // Dọn sạch Zombie gần khu vực xác máy bay
            ClearZombiesNearCrashSiteInEditor(crashCenter, 120f);

            // Lưu trực tiếp Scene
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("<color=green><b>[CRASH SITE COMPLETE]</b></color> Đã tạo thành công Xác Máy Bay Rơi chuẩn Economy Comfort & Cột Sáng Cứu Hộ!");
        }

        public static Vector3 SyncForestSpawnPointToWreckage(Vector3 crashCenter, Terrain terrain)
        {
            GameObject spawnPointObj = GameObject.Find("ForestSpawnPoint");
            if (spawnPointObj == null)
            {
                spawnPointObj = new GameObject("ForestSpawnPoint");
            }

            // Đặt trên bãi cỏ thoáng bên ngoài xác máy bay (cách 8m, hướng nhìn thoáng dọc theo lối mòn)
            Vector3 playerWakePos = crashCenter + new Vector3(7.0f, 0, -6.0f);
            if (terrain != null)
            {
                playerWakePos.y = terrain.SampleHeight(playerWakePos) + terrain.transform.position.y + 0.12f;
            }

            Undo.RecordObject(spawnPointObj.transform, "Update ForestSpawnPoint near Crash Site");
            spawnPointObj.transform.position = playerWakePos;
            // Hướng nhìn 25 độ: Hướng nhìn dọc theo bãi cỏ thoáng, xác máy bay cháy và cột sáng nằm trọn vẹn ở phía bên trái phía trước
            spawnPointObj.transform.rotation = Quaternion.Euler(0, 25f, 0);

            AirplaneCrashCutscene cutscene = FindObjectOfType<AirplaneCrashCutscene>();
            if (cutscene != null)
            {
                Undo.RecordObject(cutscene, "Assign ForestSpawnPoint to Cutscene");
                cutscene.forestSpawnPoint = spawnPointObj.transform;
                EditorUtility.SetDirty(cutscene);
            }

            return playerWakePos;
        }

        // ══════════════════════════════════════════════
        // CÁC HÀM XÂY DỰNG GHẾ ECONOMY COMFORT RƠI VÃI
        // ══════════════════════════════════════════════

        private static GameObject BuildWreckageTripleSeat(Transform parent, Vector3 localPos,
            Material matBlue, Material matShell, Material matHeadrest, Material matArmPad, Material matMetal)
        {
            GameObject rowObj = new GameObject("Wreckage_TripleSeat");
            rowObj.transform.SetParent(parent, false);
            rowObj.transform.localPosition = localPos;

            float spacing = 0.50f;
            for (int i = -1; i <= 1; i++)
            {
                BuildWreckageSingleSeat(rowObj.transform, new Vector3(i * spacing, 0, 0), "Seat_" + (i + 1),
                    matBlue, matShell, matHeadrest, matArmPad);
            }

            CreatePrimitive("Leg_L", rowObj.transform, new Vector3(-0.45f, 0.18f, 0), new Vector3(0.04f, 0.36f, 0.36f), matMetal, PrimitiveType.Cube);
            CreatePrimitive("Leg_R", rowObj.transform, new Vector3( 0.45f, 0.18f, 0), new Vector3(0.04f, 0.36f, 0.36f), matMetal, PrimitiveType.Cube);
            return rowObj;
        }

        private static GameObject BuildWreckageDoubleSeat(Transform parent, Vector3 localPos,
            Material matBlue, Material matShell, Material matHeadrest, Material matArmPad, Material matMetal)
        {
            GameObject rowObj = new GameObject("Wreckage_DoubleSeat");
            rowObj.transform.SetParent(parent, false);
            rowObj.transform.localPosition = localPos;

            float spacing = 0.50f;
            BuildWreckageSingleSeat(rowObj.transform, new Vector3(-spacing * 0.5f, 0, 0), "Seat_L", matBlue, matShell, matHeadrest, matArmPad);
            BuildWreckageSingleSeat(rowObj.transform, new Vector3( spacing * 0.5f, 0, 0), "Seat_R", matBlue, matShell, matHeadrest, matArmPad);

            CreatePrimitive("Bent_Leg", rowObj.transform, new Vector3(0, 0.18f, 0), new Vector3(0.05f, 0.35f, 0.35f), matMetal, PrimitiveType.Cube);
            return rowObj;
        }

        private static GameObject BuildWreckageSingleSeat(Transform parent, Vector3 localPos, string name,
            Material matBlue, Material matShell, Material matHeadrest, Material matArmPad)
        {
            GameObject seat = new GameObject(name);
            seat.transform.SetParent(parent, false);
            seat.transform.localPosition = localPos;

            // 1. ỐP LƯNG / KHUNG VỎ BẢO VỆ MÀU TRẮNG ĐÚC KHUÔN (WHITE SHELL)
            CreatePrimitive("Shell_SeatPan", seat.transform, new Vector3(0, 0.32f, 0), new Vector3(0.48f, 0.08f, 0.48f), matShell, PrimitiveType.Cube);
            GameObject shellBack = CreatePrimitive("Shell_Backrest", seat.transform, new Vector3(0, 0.72f, -0.22f), new Vector3(0.47f, 0.72f, 0.06f), matShell, PrimitiveType.Cube);
            shellBack.transform.localRotation = Quaternion.Euler(8f, 0, 0);

            // 2. ĐỆM NGỒI VẢI XANH HOÀNG GIA (ROYAL BLUE CUSHION)
            CreatePrimitive("SeatCushion_Blue", seat.transform, new Vector3(0, 0.38f, 0.01f), new Vector3(0.45f, 0.09f, 0.46f), matBlue, PrimitiveType.Cube);

            // 3. TỰA LƯNG VẢI XANH HOÀNG GIA
            GameObject backrestBlue = CreatePrimitive("Backrest_Blue", seat.transform, new Vector3(0, 0.73f, -0.20f), new Vector3(0.44f, 0.70f, 0.055f), matBlue, PrimitiveType.Cube);
            backrestBlue.transform.localRotation = Quaternion.Euler(8f, 0, 0);

            // 4. KHĂN PHỦ TỰA ĐẦU "ECONOMY COMFORT" MÀU CAM ĐẤT
            CreatePrimitive("EconomyComfort_Headrest", backrestBlue.transform, new Vector3(0, 0.22f, 0.032f), new Vector3(0.32f, 0.24f, 0.008f), matHeadrest, PrimitiveType.Cube);

            // 5. Tay vịn
            CreatePrimitive("Armrest_L", seat.transform, new Vector3(-0.25f, 0.50f, 0.02f), new Vector3(0.045f, 0.10f, 0.42f), matShell, PrimitiveType.Cube);
            CreatePrimitive("Armrest_R", seat.transform, new Vector3( 0.25f, 0.50f, 0.02f), new Vector3(0.045f, 0.10f, 0.42f), matShell, PrimitiveType.Cube);
            return seat;
        }

        // ══════════════════════════════════════════════
        // TIỆN ÍCH HELPER
        // ══════════════════════════════════════════════

        private static GameObject CreatePrimitive(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, PrimitiveType type)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;
            if (mat != null)
            {
                obj.GetComponent<Renderer>().sharedMaterial = mat;
            }
            return obj;
        }

        private static Light CreateFireLight(Transform parent, Vector3 localPos, Color color, float range, float intensity)
        {
            GameObject lightObj = new GameObject("Fire_Glow_Light");
            lightObj.transform.SetParent(parent, false);
            lightObj.transform.localPosition = localPos;
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            return l;
        }

        private static void SpawnCrashSiteStarterItem(string prefabPath, Transform parent, Vector3 targetPos, Terrain terrain)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                if (terrain != null)
                {
                    targetPos.y = terrain.SampleHeight(targetPos) + terrain.transform.position.y + 0.15f;
                }
                GameObject itemObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                itemObj.transform.position = targetPos;
                itemObj.transform.SetParent(parent, true);
                Undo.RegisterCreatedObjectUndo(itemObj, "Spawn Crash Starter Item");
            }
        }

        private static int ClearZombiesNearCrashSiteInEditor(Vector3 crashCenter, float radius)
        {
            int count = 0;
            HorrorGame.Enemy.EnemyAI[] zombies = Object.FindObjectsOfType<HorrorGame.Enemy.EnemyAI>();
            foreach (var z in zombies)
            {
                if (z == null) continue;
                float dist = Vector3.Distance(z.transform.position, crashCenter);
                if (dist < radius)
                {
                    Undo.RecordObject(z.transform, "Move Zombie Far From Crash Site");
                    Vector3 farPos = crashCenter + new Vector3(150f, 0, 150f);
                    Terrain terrain = Terrain.activeTerrain;
                    if (terrain != null)
                    {
                        farPos.y = terrain.SampleHeight(farPos) + terrain.transform.position.y;
                    }
                    z.transform.position = farPos;
                    EditorUtility.SetDirty(z.gameObject);
                    count++;
                }
            }
            return count;
        }

        private static Material GetOrCreateMaterial(string folder, string matName, Color albedoColor, float metallic, float smoothness, Texture2D mainTex = null)
        {
            string path = folder + "/" + matName + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = albedoColor;
            if (mainTex != null) mat.mainTexture = mainTex;
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Glossiness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateGlassMaterial(string folder, string matName, Color color, float metallic, float smoothness)
        {
            string path = folder + "/" + matName + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.color = color;
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Glossiness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateBeaconBeamMat()
        {
            string path = WRECK_MATS_DIR + "/M_Beacon_SkyBeam.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader unlit = Shader.Find("Mobile/Particles/Additive");
                if (unlit == null) unlit = Shader.Find("Particles/Additive");
                if (unlit == null) unlit = Shader.Find("Standard");

                mat = new Material(unlit);
                mat.SetColor("_TintColor", new Color(0.9f, 0.98f, 1f, 0.45f));
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static Material GetOrCreateSmokeMaterial()
        {
            string path = WRECK_MATS_DIR + "/M_Crash_SmokePuff.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader s = Shader.Find("Particles/Alpha Blended Premultiply");
                if (s == null) s = Shader.Find("Mobile/Particles/Alpha Blended");
                if (s == null) s = Shader.Find("Particles/Alpha Blended");
                if (s == null) s = Shader.Find("Standard");
                mat = new Material(s);
                AssetDatabase.CreateAsset(mat, path);
            }

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Flooded_Grounds/Materials/Wreckage/Textures/Tex_SmokePuff.png");
            if (tex == null)
            {
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Flooded_Grounds/Materials/Cabin/Textures/Tex_CloudSoft.png");
            }
            if (tex != null)
            {
                mat.mainTexture = tex;
            }

            if (mat.HasProperty("_TintColor"))
            {
                mat.SetColor("_TintColor", new Color(0.20f, 0.20f, 0.22f, 0.85f));
            }
            mat.color = new Color(0.18f, 0.18f, 0.20f, 0.85f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void BuildVolumetricSmokePillar(Transform parent, Material matSmoke)
        {
            GameObject pillarRoot = new GameObject("Volumetric_Smoke_Pillar");
            pillarRoot.transform.SetParent(parent, false);
            pillarRoot.transform.localPosition = Vector3.zero;
            pillarRoot.transform.localRotation = Quaternion.identity;

            float height = 140f;
            float baseW  = 4.5f;
            float topW   = 28.0f;

            // Dựng 4 cặp mặt phẳng chữ thập chéo uốn lượn để thấy cột khói từ mọi góc nhìn trên đảo
            for (int b = 0; b < 4; b++)
            {
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "SmokeSheet_" + b;
                quad.transform.SetParent(pillarRoot.transform, false);
                quad.transform.localPosition = new Vector3(0, 0, height * 0.5f);
                quad.transform.localScale = new Vector3(Mathf.Lerp(baseW, topW, 0.5f), height, 1f);
                quad.transform.localRotation = Quaternion.Euler(0, 0, b * 45f);

                Renderer r = quad.GetComponent<Renderer>();
                if (r != null)
                {
                    r.sharedMaterial = matSmoke;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }
                DestroyImmediate(quad.GetComponent<Collider>());
            }
        }

        private static void BuildFireEmbers(Transform parent)
        {
            GameObject emberObj = new GameObject("Crash_Fire_Embers");
            emberObj.transform.SetParent(parent, false);
            emberObj.transform.localPosition = Vector3.zero;
            emberObj.transform.localRotation = Quaternion.identity;

            ParticleSystem ps = emberObj.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psr = emberObj.GetComponent<ParticleSystemRenderer>();

            Material matEmber = AssetDatabase.LoadAssetAtPath<Material>(WRECK_MATS_DIR + "/M_Beacon_SkyBeam.mat");
            if (matEmber != null) psr.sharedMaterial = matEmber;

            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = 6f;
            main.startSpeed = 6f;
            main.startSize = 0.25f;
            main.startColor = new Color(1f, 0.55f, 0.10f, 1.0f); // Tàn lửa cam vàng rực
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;

            var emission = ps.emission;
            emission.rateOverTime = 18f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 2.0f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sc = new AnimationCurve();
            sc.AddKey(0f, 1.0f);
            sc.AddKey(1f, 0.1f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, sc);
        }

        private static void RestoreStatsPanelOriginalPosition()
        {
            GameObject statsPanel = GameObject.Find("Stats Panel");
            if (statsPanel != null)
            {
                RectTransform rt = statsPanel.GetComponent<RectTransform>();
                if (rt != null)
                {
                    Undo.RecordObject(rt, "Restore Stats Panel to Original Center-Bottom");
                    // Trả lại nguyên bản ở giữa đáy màn hình theo đúng mong muốn của người chơi:
                    rt.anchorMin = new Vector2(0.5f, 0f);
                    rt.anchorMax = new Vector2(0.5f, 0f);
                    rt.pivot = new Vector2(0.5f, 0f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(250f, 100f);
                    EditorUtility.SetDirty(rt);
                    Debug.Log("✅ Đã giữ nguyên thanh chỉ số sinh tồn (Stats Panel) tại vị trí nguyên bản ở giữa đáy màn hình!");
                }
            }
        }

        private static void EnsureDirectoriesExist()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Flooded_Grounds/Materials"))
                AssetDatabase.CreateFolder("Assets/Flooded_Grounds", "Materials");
            if (!AssetDatabase.IsValidFolder(WRECK_MATS_DIR))
                AssetDatabase.CreateFolder("Assets/Flooded_Grounds/Materials", "Wreckage");
            if (!AssetDatabase.IsValidFolder(WRECK_MATS_DIR + "/Textures"))
                AssetDatabase.CreateFolder(WRECK_MATS_DIR, "Textures");
        }
    }
}
#endif

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;
using HorrorGame.Cutscenes;
using HorrorGame.Player;

namespace HorrorGame.EditorTools
{
    /// <summary>
    /// Công cụ dựng Khoang Máy Bay Chuẩn 100% Theo Ảnh Tham Chiếu (Economy Comfort Cabin):
    /// - Cấu hình 3 - 3 chuẩn tàu bay thương mại (Airbus A320 / Boeing 737)
    /// - Ghế vải màu Xanh Hoàng Gia (Royal Blue) với ốp lưng / khung vỏ màu Trắng Sữa đúc khuôn
    /// - Khăn phủ tựa đầu màu Cam Đất đặc trưng thêu chữ "Economy Comfort" trong khung vuông
    /// - Dãy hộc hành lý trên đầu màu trắng uốn lượn liên tục dọc 2 bên trần
    /// - Đèn trần Amber ấm áp và hệ thống chiếu sáng cabin rực rỡ, chân thực
    /// - Lối đi trải thảm xanh đậm hoa văn hàng không
    /// - Cửa sổ bầu dục đón ánh sáng ban ngày tự nhiên tràn vào cabin
    /// - Camera đặt đúng góc nhìn bao quát toàn bộ chiều sâu khoang máy bay như ảnh mẫu
    /// </summary>
    public class AirplaneCabinBuilder : EditorWindow
    {
        private const string MATERIALS_PATH = "Assets/Flooded_Grounds/Materials/Cabin";
        private const string TEXTURES_PATH  = "Assets/Flooded_Grounds/Materials/Cabin/Textures";

        [MenuItem("Horror Game/✈️ Dựng Lại Khoang Máy Bay Chuẩn Ảnh 2")]
        public static void BuildFullAirplaneCabin()
        {
            BuildRealisticCabin();
        }

        [MenuItem("Horror Game/✈️ Tạo Khoang Máy Bay Xịn (The Forest Style)")]
        public static void BuildRealisticCabin()
        {
            // 1. Dọn dẹp sạch sẽ các cabin cũ nếu có
            GameObject oldCabin = GameObject.Find("Realistic_Airplane_Cabin");
            if (oldCabin != null) Undo.DestroyObjectImmediate(oldCabin);

            GameObject tempCabin = GameObject.Find("Temp_AirplaneCabin");
            if (tempCabin != null) tempCabin.SetActive(false);

            EnsureFoldersExist();

            // 2. Nạp Textures chất lượng cao chuẩn Ảnh 2
            Texture2D texEconomyComfort = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES_PATH + "/Tex_EconomyComfort.png");
            Texture2D texSeatBlue       = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES_PATH + "/Tex_SeatFabric_Blue.png");
            Texture2D texAisleCarpet    = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES_PATH + "/Tex_AisleCarpet.png");
            Texture2D texAmberPsu       = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES_PATH + "/Tex_Amber_PSU.png");
            Texture2D texSigns          = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES_PATH + "/Tex_LightedSigns.png");
            Texture2D texCockpitDoor    = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES_PATH + "/Tex_CockpitDoor.png");
            Texture2D texCloudSoft      = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES_PATH + "/Tex_CloudSoft.png");
            Texture2D texSpinnerSpiral  = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES_PATH + "/Tex_SpinnerSpiral.png");

            // 3. Khởi tạo Materials chuẩn PBR phản chiếu ánh sáng chân thực
            Material matSeatBlue    = GetOrCreateMaterial("M_Plane_SeatBlue", new Color(0.18f, 0.35f, 0.72f), 0.05f, 0.18f, texSeatBlue, false, Color.black, new Vector2(2f, 2f));
            Material matSeatShell   = GetOrCreateMaterial("M_Plane_SeatShellWhite", new Color(0.94f, 0.95f, 0.96f), 0.08f, 0.45f);
            Material matHeadrest    = GetOrCreateMaterial("M_Plane_HeadrestOrange", Color.white, 0.04f, 0.22f, texEconomyComfort);
            Material matArmrestPad  = GetOrCreateMaterial("M_Plane_ArmrestPad", new Color(0.12f, 0.18f, 0.32f), 0.10f, 0.35f);
            Material matCarpet      = GetOrCreateMaterial("M_Plane_AisleCarpet", new Color(0.92f, 0.95f, 1.0f), 0.0f, 0.10f, texAisleCarpet, false, Color.black, new Vector2(4f, 16f));
            Material matWall        = GetOrCreateMaterial("M_Plane_InteriorWall", new Color(0.92f, 0.93f, 0.95f), 0.04f, 0.30f);
            Material matCeiling     = GetOrCreateMaterial("M_Plane_CeilingWhite", new Color(0.96f, 0.97f, 0.98f), 0.02f, 0.22f);
            Material matBin         = GetOrCreateMaterial("M_Plane_OverheadBin", new Color(0.94f, 0.95f, 0.96f), 0.06f, 0.50f);
            Material matMetal       = GetOrCreateMaterial("M_Plane_SeatMetal", new Color(0.82f, 0.84f, 0.87f), 0.88f, 0.75f);
            Material matAmberLight  = GetOrCreateMaterial("M_Plane_AmberGlow", Color.white, 0.0f, 0.10f, texAmberPsu, true, new Color(1.0f, 0.72f, 0.15f) * 3.2f);
            Material matSigns       = GetOrCreateMaterial("M_Plane_Signs", Color.white, 0.1f, 0.8f, texSigns, true, Color.white * 2.0f);
            Material matDoor        = GetOrCreateMaterial("M_Plane_CockpitDoor", new Color(0.42f, 0.44f, 0.47f), 0.5f, 0.4f, texCockpitDoor);
            Material matGlass       = GetOrCreateGlassMaterial("M_Plane_WindowGlass", new Color(0.85f, 0.94f, 1f, 0.08f), 0.1f, 0.95f);
            Material matOxygenMask  = GetOrCreateMaterial("M_Plane_OxygenMask", new Color(1.0f, 0.82f, 0.15f), 0.05f, 0.35f);
            Material matOxygenTube  = GetOrCreateGlassMaterial("M_Plane_OxygenTube", new Color(0.9f, 0.98f, 0.92f, 0.45f), 0.05f, 0.85f);
            Material matWing        = GetOrCreateMaterial("M_Plane_Wing", new Color(0.90f, 0.91f, 0.93f), 0.35f, 0.70f);
            Material matChrome      = GetOrCreateMaterial("M_Plane_Chrome", new Color(0.95f, 0.96f, 0.98f), 0.95f, 0.90f);
            Material matEngine      = GetOrCreateMaterial("M_Plane_EngineNacelle", new Color(0.94f, 0.95f, 0.96f), 0.15f, 0.75f);
            Material matSpinner     = GetOrCreateMaterial("M_Plane_SpinnerSpiral", Color.white, 0.10f, 0.60f, texSpinnerSpiral);
            Material matCloud       = GetOrCreateTransparentMaterial("M_Plane_CloudSoft", texCloudSoft, new Color(1.0f, 1.0f, 1.0f, 0.85f));
            Material matSky         = GetOrCreateMaterial("M_Plane_SkyBackdrop", new Color(0.18f, 0.44f, 0.82f), 0.0f, 0.10f);
            Material matNavRed      = GetOrCreateMaterial("M_Plane_NavRed", Color.red, 0f, 0.6f, null, true, Color.red * 4f);
            Material matStrobe      = GetOrCreateMaterial("M_Plane_StrobeWhite", Color.white, 0f, 0.9f, null, true, Color.white * 5f);

            // 4. Khởi tạo Root Object
            GameObject root = new GameObject("Realistic_Airplane_Cabin");
            root.transform.position = new Vector3(0, 300f, 0);
            Undo.RegisterCreatedObjectUndo(root, "Create Economy Comfort Cabin");

            List<Light> cabinLightList      = new List<Light>();
            List<Light> warningLightList    = new List<Light>();
            List<GameObject> oxygenMaskList = new List<GameObject>();

            float cabinWidth  = 4.10f; // Chiều rộng cabin 3-3 chuẩn Airbus A320
            float cabinLength = 17.5f; // Chiều dài 9 hàng ghế tạo chiều sâu hút mắt
            float cabinHeight = 2.45f; // Chiều cao vòm trần

            // ══════════════════════════════════════════════
            // A. SÀN CABIN & THẢM LỐI ĐI XANH ĐẬM HÀNG KHÔNG
            // ══════════════════════════════════════════════
            CreateBox("CabinFloor", root.transform, new Vector3(0, 0, 0), new Vector3(cabinWidth, 0.10f, cabinLength), matCarpet);

            // Dải ray nhôm gắn chân ghế dọc theo sàn (Seat Tracks)
            CreateBox("SeatTrack_L1", root.transform, new Vector3(-1.68f, 0.052f, 0), new Vector3(0.04f, 0.012f, cabinLength * 0.96f), matMetal);
            CreateBox("SeatTrack_L2", root.transform, new Vector3(-0.72f, 0.052f, 0), new Vector3(0.04f, 0.012f, cabinLength * 0.96f), matMetal);
            CreateBox("SeatTrack_R1", root.transform, new Vector3( 0.72f, 0.052f, 0), new Vector3(0.04f, 0.012f, cabinLength * 0.96f), matMetal);
            CreateBox("SeatTrack_R2", root.transform, new Vector3( 1.68f, 0.052f, 0), new Vector3(0.04f, 0.012f, cabinLength * 0.96f), matMetal);

            // ══════════════════════════════════════════════
            // B. VÁCH THÂN MÁY BAY UỐN CONG & CỬA SỔ BẦU DỤC ÁNH SÁNG
            // ══════════════════════════════════════════════
            BuildCurvedCabinWalls(root.transform, cabinWidth, cabinLength, cabinHeight, matWall, matSeatShell, matGlass);

            // ══════════════════════════════════════════════
            // C. VÒM TRẦN TRẮNG & CÁC CỤM ĐÈN AMBER (CHUẨN ẢNH 2)
            // ══════════════════════════════════════════════
            BuildCeilingAndAmberLights(root.transform, cabinWidth, cabinLength, cabinHeight, matCeiling, matAmberLight, cabinLightList);

            // ══════════════════════════════════════════════
            // D. DÃY HỘC HÀNH LÝ TRẮNG UỐN LƯỢN LIÊN TỤC (CHUẨN ẢNH 2)
            // ══════════════════════════════════════════════
            BuildContinuousOverheadBins(root.transform, cabinLength, cabinHeight, matBin, matSeatShell);

            // ══════════════════════════════════════════════
            // E. VÁCH NGĂN PHÒNG LÁI & VÁCH SAU (BULKHEADS)
            // ══════════════════════════════════════════════
            BuildBulkheads(root.transform, cabinWidth, cabinLength, cabinHeight, matWall, matDoor, matMetal, matSigns);

            // ══════════════════════════════════════════════
            // F. 9 HÀNG GHẾ ECONOMY COMFORT CHUẨN 100% THEO ẢNH 2
            // ══════════════════════════════════════════════
            int totalRows = 9;
            float startZ = -5.4f;
            float rowSpacing = 1.35f;

            for (int r = 0; r < totalRows; r++)
            {
                float z = startZ + (r * rowSpacing);
                int rowNumber = 10 + r;

                // Cụm 3 ghế bên Trái (Ghế A sát cửa sổ, Ghế B ở giữa, Ghế C sát lối đi)
                BuildTripleSeatRow(root.transform, new Vector3(-1.20f, 0.05f, z), rowNumber, true,
                    matSeatBlue, matSeatShell, matHeadrest, matArmrestPad, matMetal);

                // Cụm 3 ghế bên Phải (Ghế D sát lối đi, Ghế E ở giữa, Ghế F sát cửa sổ)
                BuildTripleSeatRow(root.transform, new Vector3( 1.20f, 0.05f, z), rowNumber, false,
                    matSeatBlue, matSeatShell, matHeadrest, matArmrestPad, matMetal);

                // Cụm mặt nạ oxy ngầm trên trần mỗi hàng
                GameObject maskL = BuildOxygenUnit(root.transform, new Vector3(-1.20f, cabinHeight - 0.20f, z), matOxygenMask, matOxygenTube);
                oxygenMaskList.Add(maskL);
                maskL.SetActive(false);

                GameObject maskR = BuildOxygenUnit(root.transform, new Vector3( 1.20f, cabinHeight - 0.20f, z), matOxygenMask, matOxygenTube);
                oxygenMaskList.Add(maskR);
                maskR.SetActive(false);
            }

            // ══════════════════════════════════════════════
            // G. HỆ THỐNG ÁNH SÁNG RỰC RỠ, CHÂN THỰC (ẢNH 2 DAYLIGHT STYLE)
            // ══════════════════════════════════════════════
            // 1. Đèn trần Soft Downlight ấm áp dọc lối đi
            for (int i = 0; i < 7; i++)
            {
                float z = -5.0f + (i * 1.8f);
                GameObject lObj = new GameObject("CabinDownlight_" + i);
                lObj.transform.SetParent(root.transform, false);
                lObj.transform.localPosition = new Vector3(0, cabinHeight - 0.15f, z);
                Light l = lObj.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1.0f, 0.97f, 0.92f);
                l.range = 5.5f;
                l.intensity = 1.6f;
                l.shadows = LightShadows.Soft;
                cabinLightList.Add(l);
            }

            // 2. Đèn Ambient tổng thể cho khoang sáng rõ, ngập tràn ánh sáng
            GameObject ambObj = new GameObject("CabinAmbientFill");
            ambObj.transform.SetParent(root.transform, false);
            ambObj.transform.localPosition = new Vector3(0, 1.8f, 0);
            Light ambL = ambObj.AddComponent<Light>();
            ambL.type = LightType.Point;
            ambL.color = new Color(0.92f, 0.96f, 1.0f);
            ambL.range = 16.0f;
            ambL.intensity = 1.3f;
            cabinLightList.Add(ambL);

            // 3. Ánh sáng tự nhiên hắt qua cửa sổ 2 bên (Window Sunlight)
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject winLight = new GameObject("WindowSunlight_" + (side < 0 ? "Left" : "Right"));
                winLight.transform.SetParent(root.transform, false);
                winLight.transform.localPosition = new Vector3(side * 2.3f, 1.35f, 0);
                Light wl = winLight.AddComponent<Light>();
                wl.type = LightType.Point;
                wl.color = new Color(1.0f, 0.98f, 0.92f);
                wl.range = 10.0f;
                wl.intensity = 1.1f;
                cabinLightList.Add(wl);
            }

            // 4. Đèn Báo Động Đỏ Khẩn Cấp (Warning Lights)
            for (int i = 0; i < 4; i++)
            {
                float z = -4.0f + (i * 3.0f);
                GameObject wObj = new GameObject("WarningLight_" + i);
                wObj.transform.SetParent(root.transform, false);
                wObj.transform.localPosition = new Vector3(0, cabinHeight - 0.20f, z);
                Light wL = wObj.AddComponent<Light>();
                wL.type = LightType.Point;
                wL.color = new Color(1.0f, 0.08f, 0.03f);
                wL.range = 8.0f;
                wL.intensity = 0f;
                wL.shadows = LightShadows.Hard;
                warningLightList.Add(wL);
            }

            // ══════════════════════════════════════════════
            // H. NGOẠI CẢNH MÁY BAY: CÁNH, ĐỘNG CƠ, ĐÈN HÀNG KHÔNG & MÂY TRÔI
            // ══════════════════════════════════════════════
            BuildAirplaneExterior(root, matWing, matChrome, matEngine, matSpinner, matCloud, matSky, matNavRed, matStrobe);

            // ══════════════════════════════════════════════
            // I. ĐỊNH VỊ VỊ TRÍ GÓC NHÌN CHUẨN XÁC: GHẾ 12A (SÁT CỬA SỔ BÊN TRÁI)
            // ══════════════════════════════════════════════
            GameObject viewAnchor = new GameObject("PlayerCutsceneSeat");
            viewAnchor.transform.SetParent(root.transform, false);
            viewAnchor.transform.localPosition = new Vector3(-1.62f, 0.15f, -2.75f);
            viewAnchor.transform.localRotation = Quaternion.identity; // Hướng thẳng về buồng lái (+Z)

            // ══════════════════════════════════════════════
            // J. TỰ ĐỘNG LIÊN KẾT VÀ CẤU HÌNH CUTSCENE MANAGER
            // ══════════════════════════════════════════════
            AirplaneCrashCutscene cutscene = FindObjectOfType<AirplaneCrashCutscene>();
            if (cutscene != null)
            {
                Undo.RecordObject(cutscene, "Configure Cutscene to Image 2 Style");
                cutscene.airplaneCabin = root;
                cutscene.cabinLights   = cabinLightList.ToArray();
                cutscene.warningLights = warningLightList.ToArray();
                cutscene.oxygenMasks   = oxygenMaskList.ToArray();

                PlayerController player = cutscene.playerController;
                if (player == null) player = FindObjectOfType<PlayerController>();

                if (player != null)
                {
                    Undo.RecordObject(player.transform, "Seat Player in Window Seat 12A");

                    CharacterController cc = player.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;

                    Camera cam = player.GetComponentInChildren<Camera>();
                    if (cam != null)
                    {
                        // Đặt vị trí player sao cho Camera nằm ĐÚNG TẦM MẮT (Y=1.10m), không bị vách chặn
                        Vector3 targetCamPos = viewAnchor.transform.position + new Vector3(0, 0.95f, 0);
                        Vector3 offset = player.transform.position - cam.transform.position;
                        player.transform.position = targetCamPos + offset;
                        player.transform.rotation = viewAnchor.transform.rotation;

                        cutscene.cutsceneCamera = cam.transform;
                        // Góc nhìn ban đầu: nhìn chếch sang cửa sổ bên trái, hơi chúc xuống
                        cam.transform.localRotation = Quaternion.Euler(6.0f, -65.0f, 0f);
                        cam.fieldOfView = 60f;
                    }
                    else
                    {
                        player.transform.position = viewAnchor.transform.position;
                        player.transform.rotation = viewAnchor.transform.rotation;
                    }
                }

                // Cân chỉnh góc pan máy quay tự nhiên (khi không dùng Free Look)
                cutscene.cameraLookAngles = new Vector3[]
                {
                    new Vector3(-1.5f, -20.0f, 0f), // 1. Liếc nhìn cánh máy bay, mây trôi và động cơ qua cửa sổ bên trái
                    new Vector3( 1.5f,  12.0f, 0f), // 2. Liếc nhìn sang hàng ghế đối diện bên phải lối đi
                    new Vector3( 0.0f,   0.0f, 0f)  // 3. Hướng mắt thẳng phía trước khi máy bay bắt đầu rung lắc
                };

                EditorUtility.SetDirty(cutscene);
            }

            // Lưu trực tiếp Scene hiện tại để không bị mất thay đổi
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Selection.activeGameObject = root;
            Debug.Log("🎉 ĐÃ DỰNG THÀNH CÔNG VÀ LƯU KHOANG MÁY BAY CHUẨN 100% THEO ẢNH 2!");
        }

        // ══════════════════════════════════════════════
        // CÁC HÀM XÂY DỰNG NỘI THẤT CHI TIẾT
        // ══════════════════════════════════════════════

        private static void BuildCurvedCabinWalls(Transform parent, float width, float length, float height,
            Material matWall, Material matFrame, Material matGlass)
        {
            float halfW = width * 0.5f;

            for (int side = -1; side <= 1; side += 2)
            {
                bool isLeft = (side < 0);
                string sideName = isLeft ? "Wall_Left" : "Wall_Right";
                GameObject wall = new GameObject(sideName);
                wall.transform.SetParent(parent, false);

                float posX = side * halfW;

                // 1. Tấm ốp chân vách tường (từ sàn Y=0 đến bậu cửa sổ Y=0.82m)
                float lowerH = 0.82f;
                CreateBox("LowerWall", wall.transform, new Vector3(posX, lowerH * 0.5f, 0), new Vector3(0.08f, lowerH, length), matWall);

                // 2. Vách tường phía trên cửa sổ (từ đỉnh bậu cửa sổ Y=1.60m lên trần Y=height)
                float bayH = 0.78f; // Chiều cao khoang cửa sổ từ 0.82m đến 1.60m
                float winCenterY = lowerH + bayH * 0.5f; // = 1.21m (ngang tầm mắt người ngồi)
                float upperH = height - (lowerH + bayH); // = 2.45 - 1.60 = 0.85m
                float upperCenterY = lowerH + bayH + upperH * 0.5f;
                CreateBox("UpperWall", wall.transform, new Vector3(posX, upperCenterY, 0), new Vector3(0.08f, upperH, length), matWall);

                // 3. Vách đầu và vách cuối ngoài phạm vi 9 cửa sổ
                float bayW = 1.35f;
                float firstBayZ = -5.4f - bayW * 0.5f;
                float aftZ = -length * 0.5f;
                CreateBox("WallPillar_Aft", wall.transform, new Vector3(posX, winCenterY, (aftZ + firstBayZ) * 0.5f), new Vector3(0.08f, bayH, Mathf.Abs(firstBayZ - aftZ)), matWall);

                float lastBayZ = 5.4f + bayW * 0.5f;
                float fwdZ = length * 0.5f;
                CreateBox("WallPillar_Fwd", wall.transform, new Vector3(posX, winCenterY, (fwdZ + lastBayZ) * 0.5f), new Vector3(0.08f, bayH, Mathf.Abs(fwdZ - lastBayZ)), matWall);

                // 4. Dựng 9 khoang vách cửa sổ bầu dục liền khối (Seamless Oval Window Bays)
                for (int w = 0; w < 9; w++)
                {
                    float z = -5.4f + (w * bayW);
                    Vector3 bayPos = new Vector3(posX, winCenterY, z);
                    CreateProceduralWindowBay("OvalBay_" + w, wall.transform, bayPos, isLeft, bayW, bayH, matWall, matFrame, matGlass, matWall);
                }
            }
        }

        private static GameObject CreateProceduralWindowBay(string name, Transform parent, Vector3 localPos, bool isLeftSide,
            float bayWidth, float bayHeight, Material matWall, Material matBezel, Material matGlass, Material matShade)
        {
            GameObject bayObj = new GameObject(name);
            bayObj.transform.SetParent(parent, false);
            bayObj.transform.localPosition = localPos;

            float wallSurfaceX = isLeftSide ? 0.04f : -0.04f; 
            float wallThickness = 0.08f;
            float bevelDepth = 0.075f;

            // Kích thước lỗ chữ nhật trên vách (Rộng 36cm, Cao 52cm)
            float holeW = 0.36f;
            float holeH = 0.52f;

            // 1. Dựng 4 tấm vách xung quanh lỗ cửa sổ bằng Box (Đảm bảo kín 100%, không bị lỗi mesh)
            float topH = (bayHeight - holeH) * 0.5f;
            float sideW = (bayWidth - holeW) * 0.5f;

            CreateBox("Wall_Top", bayObj.transform, 
                new Vector3(wallSurfaceX, holeH * 0.5f + topH * 0.5f, 0), 
                new Vector3(wallThickness, topH, bayWidth), matWall);
            
            CreateBox("Wall_Bottom", bayObj.transform, 
                new Vector3(wallSurfaceX, -(holeH * 0.5f + topH * 0.5f), 0), 
                new Vector3(wallThickness, topH, bayWidth), matWall);

            CreateBox("Wall_Left", bayObj.transform, 
                new Vector3(wallSurfaceX, 0, -(holeW * 0.5f + sideW * 0.5f)), 
                new Vector3(wallThickness, holeH, sideW), matWall);

            CreateBox("Wall_Right", bayObj.transform, 
                new Vector3(wallSurfaceX, 0, (holeW * 0.5f + sideW * 0.5f)), 
                new Vector3(wallThickness, holeH, sideW), matWall);

            // 2. VÀNH ĐÚC KHUÔN VÁT MÉP SÂU 3D (Molded Bezel)
            int segments = 32;
            float semiW = 0.16f; // Rộng 32cm
            float semiH = 0.24f; // Cao 48cm
            float p = 3.2f;

            Vector2[] ovalPts = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float c = Mathf.Cos(angle);
                float s = Mathf.Sin(angle);
                ovalPts[i] = new Vector2(
                    Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 2f / p) * semiW,
                    Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), 2f / p) * semiH
                );
            }

            Vector2[] rectPts = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float tz = Mathf.Abs(ovalPts[i].x) > 0.0001f ? ((holeW*0.5f) / Mathf.Abs(ovalPts[i].x)) : 1000f;
                float ty = Mathf.Abs(ovalPts[i].y) > 0.0001f ? ((holeH*0.5f) / Mathf.Abs(ovalPts[i].y)) : 1000f;
                float t = Mathf.Min(tz, ty);
                rectPts[i] = new Vector2(ovalPts[i].x * t, ovalPts[i].y * t);
            }

            Mesh bezelMesh = new Mesh();
            bezelMesh.name = "MoldedBezelMesh";
            Vector3[] bVerts = new Vector3[segments * 2];
            Vector3[] bNorms = new Vector3[segments * 2];
            Vector2[] bUVs   = new Vector2[segments * 2];
            int[] bTris      = new int[segments * 6 * 2];

            float innerX = isLeftSide ? (wallSurfaceX - bevelDepth) : (wallSurfaceX + bevelDepth);
            Vector3 inwardNorm = isLeftSide ? Vector3.right : Vector3.left;

            for (int i = 0; i < segments; i++)
            {
                // Vành ngoài (Khớp với mép lỗ chữ nhật)
                bVerts[i] = new Vector3(wallSurfaceX, rectPts[i].y, rectPts[i].x);
                bNorms[i] = inwardNorm;
                bUVs[i]   = new Vector2((float)i / segments, 1f);

                // Vành trong (Lõm sâu vào và hình bầu dục)
                bVerts[i + segments] = new Vector3(innerX, ovalPts[i].y, ovalPts[i].x);
                bNorms[i + segments] = (inwardNorm + new Vector3(0, -ovalPts[i].y, -ovalPts[i].x) * 1.5f).normalized;
                bUVs[i + segments]   = new Vector2((float)i / segments, 0f);
            }

            int tIdx = 0;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int r0 = i, r1 = next, o1 = next + segments, o0 = i + segments;

                bTris[tIdx++] = r0; bTris[tIdx++] = o1; bTris[tIdx++] = r1;
                bTris[tIdx++] = r0; bTris[tIdx++] = o0; bTris[tIdx++] = o1;

                bTris[tIdx++] = r0; bTris[tIdx++] = r1; bTris[tIdx++] = o1;
                bTris[tIdx++] = r0; bTris[tIdx++] = o1; bTris[tIdx++] = o0;
            }

            bezelMesh.vertices = bVerts;
            bezelMesh.normals = bNorms;
            bezelMesh.uv = bUVs;
            bezelMesh.triangles = bTris;

            GameObject bezelObj = new GameObject("MoldedBezel");
            bezelObj.transform.SetParent(bayObj.transform, false);
            bezelObj.AddComponent<MeshFilter>().sharedMesh = bezelMesh;
            bezelObj.AddComponent<MeshRenderer>().sharedMaterial = matBezel;

            // 3. KÍNH MICA TRONG SUỐT (AcrylicGlass)
            Mesh glassMesh = new Mesh();
            glassMesh.name = "AcrylicGlassMesh";
            Vector3[] gVerts = new Vector3[segments + 1];
            Vector3[] gNorms = new Vector3[segments + 1];
            Vector2[] gUVs   = new Vector2[segments + 1];
            int[] gTris = new int[segments * 3 * 2];

            float glassX = isLeftSide ? (innerX - 0.005f) : (innerX + 0.005f);
            gVerts[0] = new Vector3(glassX, 0, 0);
            gNorms[0] = inwardNorm;
            gUVs[0]   = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < segments; i++)
            {
                gVerts[i + 1] = new Vector3(glassX, ovalPts[i].y * 0.90f, ovalPts[i].x * 0.90f);
                gNorms[i + 1] = inwardNorm;
                gUVs[i + 1]   = new Vector2((ovalPts[i].x / semiW) * 0.5f + 0.5f, (ovalPts[i].y / semiH) * 0.5f + 0.5f);
            }

            tIdx = 0;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                gTris[tIdx++] = 0; gTris[tIdx++] = next + 1; gTris[tIdx++] = i + 1;
                gTris[tIdx++] = 0; gTris[tIdx++] = i + 1; gTris[tIdx++] = next + 1;
            }

            glassMesh.vertices = gVerts;
            glassMesh.normals = gNorms;
            glassMesh.uv = gUVs;
            glassMesh.triangles = gTris;

            GameObject glassObj = new GameObject("AcrylicGlass");
            glassObj.transform.SetParent(bayObj.transform, false);
            glassObj.AddComponent<MeshFilter>().sharedMesh = glassMesh;
            glassObj.AddComponent<MeshRenderer>().sharedMaterial = matGlass;

            // Lỗ thông áp vi mô
            CreateBox("BreatherPinhole", glassObj.transform,
                new Vector3(isLeftSide ? 0.002f : -0.002f, -semiH * 0.60f, 0),
                new Vector3(0.003f, 0.008f, 0.008f), matBezel);

            return bayObj;
        }

        private static void BuildCeilingAndAmberLights(Transform parent, float width, float length, float height,
            Material matCeiling, Material matAmber, List<Light> lightList)
        {
            GameObject ceiling = new GameObject("CabinCeiling");
            ceiling.transform.SetParent(parent, false);

            float halfW = width * 0.5f; // = 2.05m

            // ═══════════════════════════════════════════════════════════
            // TRẦN CHÍNH: Phủ KÍN toàn bộ chiều rộng cabin (width + margin)
            // Dày hơn (0.10m) để không bị light leak qua khe
            // ═══════════════════════════════════════════════════════════
            CreateBox("CeilingArch", ceiling.transform,
                new Vector3(0, height, 0),
                new Vector3(width + 0.20f, 0.10f, length + 0.20f), matCeiling);

            // ═══════════════════════════════════════════════════════════
            // 2 TẤM TRẦN VÁT NGHIÊNG 2 BÊN: Nối từ mép trần trung tâm
            // xuống đỉnh vách tường, tạo hình vòm cong thân máy bay
            // Dùng nhiều lát (slats) xếp kề nhau tạo đường cong mượt
            // ═══════════════════════════════════════════════════════════
            int slatCount = 6; // Số lát nghiêng mỗi bên
            float archEdgeX = width * 0.34f; // Mép ngoài tấm trần trung tâm = halfW * 0.68
            float wallTopX = halfW + 0.04f;  // Mép ngoài vách tường (có thêm margin)
            float archY = height - 0.05f;    // Đáy tấm trần trung tâm
            float wallTopY = height - 0.08f; // Đỉnh vách tường (hơi thấp hơn trần)

            for (int side = -1; side <= 1; side += 2)
            {
                string sideName = side < 0 ? "L" : "R";

                for (int s = 0; s < slatCount; s++)
                {
                    float t0 = (float)s / slatCount;
                    float t1 = (float)(s + 1) / slatCount;
                    float tMid = (t0 + t1) * 0.5f;

                    // Nội suy X từ mép trần ra vách
                    float x0 = Mathf.Lerp(archEdgeX, wallTopX, t0);
                    float x1 = Mathf.Lerp(archEdgeX, wallTopX, t1);
                    float xMid = (x0 + x1) * 0.5f;
                    float slatW = x1 - x0 + 0.02f; // Rộng hơn chút để không hở khe

                    // Nội suy Y từ trần xuống đỉnh vách (đường cong nhẹ)
                    float curve = Mathf.Sin(tMid * Mathf.PI * 0.5f);
                    float yMid = Mathf.Lerp(archY, wallTopY, curve);

                    CreateBox("CeilingSlat_" + sideName + "_" + s, ceiling.transform,
                        new Vector3(side * xMid, yMid, 0),
                        new Vector3(slatW, 0.08f, length + 0.10f), matCeiling);
                }
            }

            // ═══════════════════════════════════════════════════════════
            // Các cụm đèn Amber ấm áp trên trần (đặc trưng nổi bật của Ảnh 2)
            // ═══════════════════════════════════════════════════════════
            for (int i = 0; i < 9; i++)
            {
                float z = -5.4f + (i * 1.35f);

                // Cụm đèn tròn Amber bên trái và bên phải máng trần
                CreateBox("AmberLight_L_" + i, ceiling.transform, new Vector3(-0.35f, height - 0.025f, z), new Vector3(0.14f, 0.02f, 0.10f), matAmber);
                CreateBox("AmberLight_R_" + i, ceiling.transform, new Vector3( 0.35f, height - 0.025f, z), new Vector3(0.14f, 0.02f, 0.10f), matAmber);

                // Đèn Point Light ánh sáng Amber hắt xuống ghế
                GameObject amberPoint = new GameObject("AmberGlowPoint_" + i);
                amberPoint.transform.SetParent(ceiling.transform, false);
                amberPoint.transform.localPosition = new Vector3(0, height - 0.10f, z);
                Light apl = amberPoint.AddComponent<Light>();
                apl.type = LightType.Point;
                apl.color = new Color(1.0f, 0.72f, 0.15f);
                apl.range = 2.8f;
                apl.intensity = 0.85f;
                lightList.Add(apl);

                // Cửa gió điều hòa tròn (Vents) giữa 2 đèn
                CreateBox("CeilingVent_" + i, ceiling.transform, new Vector3(0, height - 0.022f, z), new Vector3(0.16f, 0.015f, 0.16f), matCeiling);
            }
        }

        private static void BuildContinuousOverheadBins(Transform parent, float length, float height,
            Material matBin, Material matShell)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                bool isLeft = (side < 0);
                string binName = isLeft ? "OverheadBins_Left" : "OverheadBins_Right";
                GameObject binGroup = new GameObject(binName);
                binGroup.transform.SetParent(parent, false);

                // Đặt hộc hành lý nằm sát lối đi trung tâm (posX = 0.72m),
                // TRÁNH XA cửa sổ để hành khách ghế A nhìn ra ngoài không bị che khuất
                float posX = side * 0.72f;

                // Thân hộc uốn lượn liên tục — thu hẹp lại 0.55m để không lan ra phía cửa sổ
                GameObject mainBin = CreateBox("MainBinBody", binGroup.transform, new Vector3(posX, height - 0.32f, 0), new Vector3(0.55f, 0.38f, length), matBin);

                // Máng cong phía dưới hộc
                GameObject binLip = CreateBox("BinLowerLip", binGroup.transform, new Vector3(posX - side * 0.10f, height - 0.48f, 0), new Vector3(0.26f, 0.06f, length), matShell);

                // Các đường viền ngăn khoang và tay nắm cửa hộc từng hàng
                for (int i = 0; i < 9; i++)
                {
                    float z = -5.4f + (i * 1.35f);
                    CreateBox("BinHandle_" + i, binGroup.transform, new Vector3(posX - side * 0.22f, height - 0.42f, z), new Vector3(0.04f, 0.05f, 0.22f), matShell);
                    CreateBox("BinSeam_" + i, binGroup.transform, new Vector3(posX, height - 0.32f, z + 0.67f), new Vector3(0.57f, 0.39f, 0.02f), matShell);
                }
            }
        }

        private static void BuildBulkheads(Transform parent, float width, float length, float height,
            Material matWall, Material matDoor, Material matMetal, Material matSigns)
        {
            // Vách ngăn buồng lái phía trước
            float fwdZ = length * 0.5f;
            GameObject fwd = new GameObject("Bulkhead_Forward");
            fwd.transform.SetParent(parent, false);
            fwd.transform.localPosition = new Vector3(0, 0, fwdZ);

            // Vách trái và vách phải
            CreateBox("FwdWall_L", fwd.transform, new Vector3(-1.40f, height * 0.5f, 0), new Vector3(1.30f, height, 0.12f), matWall);
            CreateBox("FwdWall_R", fwd.transform, new Vector3( 1.40f, height * 0.5f, 0), new Vector3(1.30f, height, 0.12f), matWall);
            CreateBox("FwdLintel", fwd.transform, new Vector3(0, height - 0.25f, 0), new Vector3(1.50f, 0.50f, 0.12f), matWall);

            // Cửa buồng lái bọc kim loại kiên cố
            CreateBox("CockpitDoor", fwd.transform, new Vector3(0, (height - 0.50f) * 0.5f, 0.02f), new Vector3(1.05f, height - 0.50f, 0.06f), matDoor);

            // Bảng hiệu thoát hiểm EXIT sáng rực phía trên cửa
            CreateBox("ExitSign", fwd.transform, new Vector3(0, height - 0.30f, -0.07f), new Vector3(0.55f, 0.20f, 0.04f), matSigns);

            // Vách sau cabin
            float aftZ = -length * 0.5f;
            GameObject aft = new GameObject("Bulkhead_Aft");
            aft.transform.SetParent(parent, false);
            aft.transform.localPosition = new Vector3(0, 0, aftZ);
            CreateBox("AftWall", aft.transform, new Vector3(0, height * 0.5f, 0), new Vector3(width, height, 0.12f), matWall);
        }

        // ══════════════════════════════════════════════
        // CỤM 3 GHẾ ECONOMY COMFORT (CHUẨN 100% THEO ẢNH 2)
        // ══════════════════════════════════════════════

        private static GameObject BuildTripleSeatRow(Transform parent, Vector3 localPos, int rowNumber, bool isLeftSide,
            Material matBlue, Material matShell, Material matHeadrest, Material matArmPad, Material matMetal)
        {
            string rowName = (isLeftSide ? "TripleSeat_L_Row_" : "TripleSeat_R_Row_") + rowNumber;
            GameObject rowObj = new GameObject(rowName);
            rowObj.transform.SetParent(parent, false);
            rowObj.transform.localPosition = localPos;

            float spacing = 0.50f;

            // Thứ tự ghế:
            // Bên trái (-X): Ghế A (Sát cửa sổ), Ghế B (Giữa), Ghế C (Lối đi)
            // Bên phải (+X): Ghế D (Lối đi), Ghế E (Giữa), Ghế F (Sát cửa sổ)
            string[] names = isLeftSide ? new string[] { "Seat_A", "Seat_B", "Seat_C" } : new string[] { "Seat_D", "Seat_E", "Seat_F" };
            float[] offsets = new float[] { -spacing, 0, spacing };

            for (int i = 0; i < 3; i++)
            {
                BuildSingleEconomySeat(rowObj.transform, new Vector3(offsets[i], 0, 0), names[i],
                    matBlue, matShell, matHeadrest, matArmPad);
            }

            // Chân ghế hợp kim gắn ray sàn
            CreateBox("SeatLeg_L", rowObj.transform, new Vector3(-0.45f, 0.18f, 0), new Vector3(0.04f, 0.36f, 0.36f), matMetal);
            CreateBox("SeatLeg_R", rowObj.transform, new Vector3( 0.45f, 0.18f, 0), new Vector3(0.04f, 0.36f, 0.36f), matMetal);
            CreateBox("SeatCrossBar", rowObj.transform, new Vector3(0, 0.28f, 0), new Vector3(1.35f, 0.04f, 0.04f), matMetal);

            // 4 Tay vịn màu trắng với đệm xanh phía trên
            float[] armX = new float[] { -spacing * 1.5f + 0.02f, -spacing * 0.5f, spacing * 0.5f, spacing * 1.5f - 0.02f };
            for (int a = 0; a < 4; a++)
            {
                GameObject arm = CreateBox("Armrest_" + a, rowObj.transform, new Vector3(armX[a], 0.50f, 0.02f), new Vector3(0.05f, 0.10f, 0.42f), matShell);
                // Đệm êm xanh trên tay vịn
                CreateBox("Pad", arm.transform, new Vector3(0, 0.046f, 0), new Vector3(0.046f, 0.016f, 0.40f), matArmPad);
            }

            return rowObj;
        }

        private static void BuildSingleEconomySeat(Transform parent, Vector3 localPos, string name,
            Material matBlue, Material matShell, Material matHeadrest, Material matArmPad)
        {
            GameObject seat = new GameObject(name);
            seat.transform.SetParent(parent, false);
            seat.transform.localPosition = localPos;

            // 1. ỐP LƯNG / KHUNG VỎ BẢO VỆ MÀU TRẮNG ĐÚC KHUÔN (WHITE SHELL)
            // Kích thước mỏng, sát thân ghế — không phình ra
            CreateBox("Shell_SeatPan", seat.transform, new Vector3(0, 0.32f, 0), new Vector3(0.44f, 0.04f, 0.44f), matShell);
            GameObject shellBack = CreateBox("Shell_Backrest", seat.transform, new Vector3(0, 0.72f, -0.22f), new Vector3(0.43f, 0.68f, 0.03f), matShell);
            shellBack.transform.localRotation = Quaternion.Euler(8f, 0, 0);

            // Khay ăn gập gọn phía sau ghế (Tray Table)
            CreateBox("TrayTable", shellBack.transform, new Vector3(0, -0.05f, -0.020f), new Vector3(0.34f, 0.24f, 0.012f), matShell);
            CreateBox("TrayLatch", shellBack.transform, new Vector3(0, 0.11f, -0.022f), new Vector3(0.05f, 0.018f, 0.012f), matArmPad);

            // Túi đựng tạp chí / an toàn bay bên dưới
            CreateBox("SeatPocket", shellBack.transform, new Vector3(0, -0.25f, -0.020f), new Vector3(0.36f, 0.16f, 0.015f), matBlue);

            // 2. ĐỆM NGỒI VẢI XANH HOÀNG GIA (ROYAL BLUE CUSHION) — mỏng 5cm như thật
            CreateBox("SeatCushion_Blue", seat.transform, new Vector3(0, 0.36f, 0.01f), new Vector3(0.42f, 0.05f, 0.42f), matBlue);

            // 3. TỰA LƯNG VẢI XANH HOÀNG GIA NGẢ CÔNG THÁI HỌC — mỏng 3cm
            GameObject backrestBlue = CreateBox("Backrest_Blue", seat.transform, new Vector3(0, 0.73f, -0.20f), new Vector3(0.40f, 0.66f, 0.03f), matBlue);
            backrestBlue.transform.localRotation = Quaternion.Euler(8f, 0, 0);

            // 4. KHĂN PHỦ TỰA ĐẦU "ECONOMY COMFORT" MÀU CAM ĐẤT (ĐẶC TRƯNG CHÍNH ẢNH 2)
            GameObject headrestCloth = CreateBox("EconomyComfort_Headrest", backrestBlue.transform, new Vector3(0, 0.22f, 0.018f), new Vector3(0.28f, 0.20f, 0.005f), matHeadrest);

            // 5. Đai an toàn màu đen
            CreateBox("Seatbelt", seat.transform, new Vector3(0, 0.40f, 0.04f), new Vector3(0.36f, 0.012f, 0.03f), matArmPad);
        }

        private static GameObject BuildOxygenUnit(Transform parent, Vector3 localPos, Material matMask, Material matTube)
        {
            GameObject unit = new GameObject("OxygenMask_Unit");
            unit.transform.SetParent(parent, false);
            unit.transform.localPosition = localPos;

            CreateBox("Hatch", unit.transform, Vector3.zero, new Vector3(0.35f, 0.02f, 0.22f), matMask);
            CreateBox("Tube", unit.transform, new Vector3(0, -0.30f, 0), new Vector3(0.015f, 0.55f, 0.015f), matTube);
            CreateBox("MaskCup", unit.transform, new Vector3(0, -0.65f, 0.03f), new Vector3(0.14f, 0.16f, 0.12f), matMask);
            return unit;
        }

        // ══════════════════════════════════════════════
        // TIỆN ÍCH TẠO MESH VÀ VẬT LIỆU PBR
        // ══════════════════════════════════════════════

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            if (mat != null)
            {
                Renderer r = go.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = mat;
            }
            return go;
        }

        private static Material GetOrCreateMaterial(string matName, Color albedoColor, float metallic, float smoothness,
            Texture2D mainTex = null, bool isEmission = false, Color emissionColor = default(Color), Vector2 uvTiling = default(Vector2))
        {
            string path = MATERIALS_PATH + "/" + matName + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = albedoColor;
            if (mainTex != null)
            {
                mat.mainTexture = mainTex;
                if (uvTiling != default(Vector2))
                {
                    mat.mainTextureScale = uvTiling;
                }
            }

            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Glossiness", smoothness);

            if (isEmission)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateGlassMaterial(string matName, Color color, float metallic, float smoothness)
        {
            string path = MATERIALS_PATH + "/" + matName + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetFloat("_Mode", 3); // Transparent
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

        private static void BuildAirplaneExterior(GameObject root,
            Material matWing, Material matChrome, Material matEngine, Material matSpinner,
            Material matCloud, Material matSky, Material matNavRed, Material matStrobe)
        {
            GameObject exteriorObj = new GameObject("Airplane_Exterior");
            exteriorObj.transform.SetParent(root.transform, false);
            exteriorObj.transform.localPosition = Vector3.zero;

            AirplaneCabinExterior exteriorComp = exteriorObj.AddComponent<AirplaneCabinExterior>();

            // ═════════════════════════════════════════════════════════════════
            // 1. CÁNH MÁY BAY BÊN TRÁI (LEFT AIRPLANE WING)
            // ═════════════════════════════════════════════════════════════════
            GameObject wing = new GameObject("Wing_Left");
            wing.transform.SetParent(exteriorObj.transform, false);

            // Gốc cánh (Wing Root) vươn từ sát thân máy bay ra ngoài, cao Y = 0.88m (ngay dưới bậu cửa)
            GameObject wRoot = CreateBox("WingSection_Root", wing.transform, new Vector3(-3.0f, 0.88f, -2.5f), new Vector3(2.4f, 0.22f, 4.4f), matWing);
            wRoot.transform.localRotation = Quaternion.Euler(0, -12f, 3.5f);

            // Thân cánh giữa (Mid Wing - nơi gắn pylon động cơ)
            GameObject wMid = CreateBox("WingSection_Mid", wing.transform, new Vector3(-5.5f, 1.05f, -3.5f), new Vector3(3.2f, 0.18f, 3.4f), matWing);
            wMid.transform.localRotation = Quaternion.Euler(0, -15f, 4.0f);

            // Đầu cánh ngoài (Outer Wing)
            GameObject wOuter = CreateBox("WingSection_Outer", wing.transform, new Vector3(-8.8f, 1.22f, -4.6f), new Vector3(3.5f, 0.15f, 2.4f), matWing);
            wOuter.transform.localRotation = Quaternion.Euler(0, -19f, 4.5f);

            // Mép trước cánh mạ Crom sáng bóng (Leading Edge Chrome Slat)
            GameObject slat = CreateBox("LeadingEdge_Chrome", wing.transform, new Vector3(-5.8f, 1.02f, -1.8f), new Vector3(8.5f, 0.10f, 0.25f), matChrome);
            slat.transform.localRotation = Quaternion.Euler(0, -15.5f, 4.0f);

            // Cánh nhỏ Sharklet / Winglet cong vút lên trời ở đầu cánh
            GameObject winglet = CreateBox("Sharklet_Winglet", wing.transform, new Vector3(-10.6f, 1.85f, -5.2f), new Vector3(0.12f, 1.40f, 0.90f), matWing);
            winglet.transform.localRotation = Quaternion.Euler(0, -21f, 75f);

            // ═════════════════════════════════════════════════════════════════
            // 2. ĐỘNG CƠ PHẢN LỰC CFM56 (JET ENGINE - TO LỚN, RÕ NÉT NGOÀI CỬA SỔ)
            // ═════════════════════════════════════════════════════════════════
            // Đặt tại X = -3.6m, Y = 0.62m, Z = -1.6m -> Nhìn qua cửa sổ ghế 12A là THẤY TRỌN VẸN!
            GameObject engineObj = new GameObject("JetEngine_Left");
            engineObj.transform.SetParent(exteriorObj.transform, false);
            engineObj.transform.localPosition = new Vector3(-3.6f, 0.62f, -1.6f);
            engineObj.transform.localRotation = Quaternion.Euler(0, -2.0f, 0);

            // Trụ treo động cơ (Pylon) nối vào cánh
            CreateBox("Pylon", engineObj.transform, new Vector3(0, 0.48f, 0), new Vector3(0.20f, 0.42f, 2.0f), matWing);

            // Thân vỏ động cơ (Nacelle Cowling) - đỉnh đạt Y = 0.62 + 0.72 = 1.34m (ngang tầm mắt!)
            CreateBox("NacelleBody", engineObj.transform, new Vector3(0, 0, 0), new Vector3(1.45f, 1.45f, 2.4f), matEngine);

            // Vành miệng hút gió mạ Crom sáng loáng (Intake Chrome Lip)
            CreateBox("IntakeLip_Chrome", engineObj.transform, new Vector3(0, 0, 1.22f), new Vector3(1.48f, 1.48f, 0.14f), matChrome);

            // Lòng ống hút gió
            CreateBox("IntakeDuct", engineObj.transform, new Vector3(0, 0, 0.75f), new Vector3(1.20f, 1.20f, 0.80f), matSpinner);

            // Cánh quạt turbine titan (Turbine Fan Blades)
            CreateBox("TurbineFanBlades", engineObj.transform, new Vector3(0, 0, 0.40f), new Vector3(1.16f, 1.16f, 0.05f), matChrome);

            // Nón xoay Spinner ở tâm động cơ mang hoa văn xoắn ốc (xoay tít 1200 RPM)
            GameObject spinner = CreateBox("SpinnerBullet", engineObj.transform, new Vector3(0, 0, 0.46f), new Vector3(0.38f, 0.38f, 0.42f), matSpinner);
            exteriorComp.engineSpinner = spinner.transform;

            // Ống xả phản lực phía sau
            CreateBox("ExhaustNozzle", engineObj.transform, new Vector3(0, 0, -1.25f), new Vector3(1.10f, 1.10f, 0.25f), matChrome);

            // Đèn ánh lửa hỏng hóc khi tai nạn
            GameObject engGlow = new GameObject("EngineGlowLight");
            engGlow.transform.SetParent(engineObj.transform, false);
            engGlow.transform.localPosition = new Vector3(0, 0, -1.0f);
            Light engGL = engGlow.AddComponent<Light>();
            engGL.type = LightType.Point;
            engGL.color = new Color(1f, 0.35f, 0f);
            engGL.range = 8.0f;
            engGL.intensity = 0f;
            exteriorComp.engineGlowLight = engGL;

            // ═════════════════════════════════════════════════════════════════
            // 3. ĐÈN HÀNG KHÔNG ĐẦU CÁNH (AVIATION LIGHTS)
            // ═════════════════════════════════════════════════════════════════
            // Đèn định vị đỏ (Port Nav Light - Red)
            GameObject navRedObj = CreateBox("NavLight_Red", wing.transform, new Vector3(-11.0f, 1.35f, -4.5f), new Vector3(0.12f, 0.12f, 0.16f), matNavRed);
            Light navRedL = navRedObj.AddComponent<Light>();
            navRedL.type = LightType.Point;
            navRedL.color = Color.red;
            navRedL.range = 2.5f;
            navRedL.intensity = 2.5f;
            exteriorComp.wingNavLight = navRedL;

            // Đèn chớp trắng chống va chạm cực mạnh (Anti-Collision Xenon Strobe)
            GameObject strobeObj = CreateBox("StrobeLight_White", wing.transform, new Vector3(-11.0f, 1.85f, -5.3f), new Vector3(0.12f, 0.12f, 0.16f), matStrobe);
            Light strobeL = strobeObj.AddComponent<Light>();
            strobeL.type = LightType.Point;
            strobeL.color = Color.white;
            strobeL.range = 2.5f;
            strobeL.intensity = 0f;
            exteriorComp.wingStrobeLight = strobeL;
            exteriorComp.wingStrobeRenderer = strobeObj.GetComponent<Renderer>();

            // ═════════════════════════════════════════════════════════════════
            // 4. MÂY CUỘN TRÔI DƯỚI CÁNH & ÁNH NẮNG (KHÔNG DÙNG HỘP XANH BỊT KÍN)
            // ═════════════════════════════════════════════════════════════════
            // Ánh nắng tự nhiên chiếu qua cửa sổ
            GameObject sunLightObj = new GameObject("WindowSunDirectional");
            sunLightObj.transform.SetParent(exteriorObj.transform, false);
            sunLightObj.transform.localPosition = new Vector3(-15f, 18f, 0);
            sunLightObj.transform.localRotation = Quaternion.Euler(24f, -118f, 0);
            Light sunDirL = sunLightObj.AddComponent<Light>();
            sunDirL.type = LightType.Directional;
            sunDirL.color = new Color(1.0f, 0.97f, 0.90f);
            sunDirL.intensity = 1.25f;
            exteriorComp.sunDirectionalLight = sunDirL;

            // Đèn chớp sét khi bão
            GameObject lightnObj = new GameObject("LightningFlashLight");
            lightnObj.transform.SetParent(exteriorObj.transform, false);
            lightnObj.transform.localPosition = new Vector3(-8f, 4f, 0);
            Light lightnL = lightnObj.AddComponent<Light>();
            lightnL.type = LightType.Point;
            lightnL.color = new Color(0.85f, 0.95f, 1.0f);
            lightnL.range = 50f;
            lightnL.intensity = 0f;
            exteriorComp.lightningFlashLight = lightnL;

            // 6 tầng mây cuộn trôi vùn vụt dưới cánh (Y = -0.5m đến +0.3m)
            List<Transform> clouds = new List<Transform>();
            Vector3[] cloudCoords = new Vector3[]
            {
                new Vector3(-6.0f,  -0.5f, -24f),
                new Vector3(-10.0f, -0.2f, -8f),
                new Vector3(-5.0f,   0.2f,  10f),
                new Vector3(-12.0f, -0.4f,  28f),
                new Vector3(-4.5f,   0.0f,  -4f),
                new Vector3(-9.5f,   0.1f,  16f)
            };
            Vector3[] cloudSizes = new Vector3[]
            {
                new Vector3(18f, 0.05f, 24f),
                new Vector3(22f, 0.05f, 28f),
                new Vector3(18f, 0.05f, 22f),
                new Vector3(24f, 0.05f, 30f),
                new Vector3(12f, 0.05f, 16f),
                new Vector3(14f, 0.05f, 18f)
            };

            for (int c = 0; c < cloudCoords.Length; c++)
            {
                GameObject cloudLayer = CreateBox("CloudLayer_" + c, exteriorObj.transform, cloudCoords[c], cloudSizes[c], matCloud);
                clouds.Add(cloudLayer.transform);
            }

            exteriorComp.cloudLayers = clouds.ToArray();
            exteriorComp.cloudMoveSpeed = 48f;
            exteriorComp.cloudStartZ = -40f;
            exteriorComp.cloudResetZ = 40f;
        }

        private static Material GetOrCreateTransparentMaterial(string matName, Texture2D tex, Color color)
        {
            string path = MATERIALS_PATH + "/" + matName + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

            mat.color = color;
            if (tex != null)
            {
                mat.mainTexture = tex;
            }
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Glossiness", 0.1f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void EnsureFoldersExist()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Flooded_Grounds/Materials"))
                AssetDatabase.CreateFolder("Assets/Flooded_Grounds", "Materials");
            if (!AssetDatabase.IsValidFolder(MATERIALS_PATH))
                AssetDatabase.CreateFolder("Assets/Flooded_Grounds/Materials", "Cabin");
            if (!AssetDatabase.IsValidFolder(TEXTURES_PATH))
                AssetDatabase.CreateFolder(MATERIALS_PATH, "Textures");
        }
    }
}
#endif

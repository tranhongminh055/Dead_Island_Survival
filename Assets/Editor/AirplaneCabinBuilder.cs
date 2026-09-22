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

        [MenuItem("Horror Game/✈️ Dựng Toàn Bộ Máy Bay Hoàn Chỉnh (Full Commercial Airliner)")]
        public static void BuildCompleteCommercialAirliner()
        {
            BuildRealisticCabin();
        }

        [MenuItem("Horror Game/💺 Cho Nhân Vật Ngồi Ngay Ngắn Ghế 12A (Seat Player Properly)")]
        public static void SeatCharacterNow()
        {
            GameObject cabin = GameObject.Find("Realistic_Airplane_Cabin");
            if (cabin == null) cabin = GameObject.Find("Temp_AirplaneCabin");
            if (cabin == null)
            {
                EditorUtility.DisplayDialog("Thông báo", "Vui lòng dựng máy bay trước bằng menu 'Horror Game/✈️ Dựng Toàn Bộ Máy Bay Hoàn Chỉnh'!", "OK");
                return;
            }

            PlayerController player = Object.FindObjectOfType<PlayerController>();
            if (player == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy PlayerController trong Scene!", "OK");
                return;
            }

            AlignPlayerToSeat12A(cabin, player);
        }

        public static void AlignPlayerToSeat12A(GameObject cabin, PlayerController player)
        {
            if (cabin == null || player == null) return;

            Undo.RecordObject(player.transform, "Seat Player in Window Seat 12A");

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            Transform anchor = cabin.transform.Find("PlayerCutsceneSeat");
            if (anchor == null)
            {
                GameObject viewAnchor = new GameObject("PlayerCutsceneSeat");
                viewAnchor.transform.SetParent(cabin.transform, false);
                viewAnchor.transform.localPosition = new Vector3(-1.70f, 0.42f, -2.70f);
                viewAnchor.transform.localRotation = Quaternion.identity;
                anchor = viewAnchor.transform;
            }

            // 1. Kích hoạt toàn bộ hierarchy của Player
            player.gameObject.SetActive(true);
            Transform[] allChildren = player.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allChildren)
            {
                if (t == null) continue;
                if (t.name.Contains("Gun") || t.name.Contains("Weapon") || t.name.Contains("Axe") || t.name.Contains("Placeholder"))
                {
                    t.gameObject.SetActive(false);
                    continue;
                }
                t.gameObject.SetActive(true);
            }

            // 2. Reset vị trí model con Ch15_nonPBR về đúng tâm gốc Player (sửa lỗi lệch sang ghế B)
            Transform ch15 = player.transform.Find("Ch15_nonPBR");
            if (ch15 != null)
            {
                Undo.RecordObject(ch15, "Reset Ch15_nonPBR Transform");
                ch15.localPosition = new Vector3(0f, -0.84f, 0f);
                ch15.localRotation = Quaternion.identity;
                ch15.localScale = Vector3.one;
            }

            // 3. Tắt Animator để không can thiệp hay ghi đè lên tư thế ngồi
            Animator anim = player.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                Undo.RecordObject(anim, "Disable Animator While Seated");
                anim.enabled = false;
            }

            // 4. Đặt Player đúng hướng nhìn về phía trước máy bay (+Z)
            player.transform.rotation = cabin.transform.rotation;

            // 5. Đặt Player tại vị trí chuẩn xác tuyệt đối để Hips nằm ngay trên mặt đệm ghế 12A
            // Ghế 12A: local X = -1.70f (sát cửa sổ bên trái), Y = +0.33m (mặt đệm 0.44m - hips offset 0.11m), Z = -2.75m (ngồi tựa lưng ghế)
            Vector3 targetPlayerPos = cabin.transform.TransformPoint(new Vector3(-1.70f, 0.33f, -2.75f));
            player.transform.position = targetPlayerPos;

            // 6. Gắn và cấu hình bộ tư thế AirplanePassengerSeatPose
            AirplanePassengerSeatPose seatPose = player.GetComponent<AirplanePassengerSeatPose>();
            if (seatPose == null) seatPose = player.gameObject.AddComponent<AirplanePassengerSeatPose>();
            seatPose.seatAnchor = anchor;
            seatPose.isSeated = true;
            seatPose.lookAtWindow = true;
            seatPose.thighPitch = 75f;
            seatPose.kneePitch = 80f;  // Đầu gối gập 80 độ buông cẳng chân thẳng xuống sàn (KHÔNG PHẢI -80 độ khiến chân ngược lên ngực!)
            seatPose.footPitch = -10f; // Bàn chân đặt phẳng trên mặt sàn thảm cabin
            seatPose.spineRecline = -6f;
            seatPose.EnsureMeshVisible();
            seatPose.CacheBones();

            // Nạp mocap animation Idle để cánh tay xuôi tự nhiên, không bị giơ ngang
            Object[] idleAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/Flooded_Grounds/Character No Animation/Animation Character/Ch15_nonPBR@Idle.fbx");
            AnimationClip idleClip = null;
            if (idleAssets != null)
            {
                foreach (var a in idleAssets)
                {
                    if (a is AnimationClip && !a.name.StartsWith("__preview__"))
                    {
                        idleClip = a as AnimationClip;
                        break;
                    }
                }
            }
            if (idleClip != null)
            {
                idleClip.SampleAnimation(player.gameObject, 0f);
            }

            // Gập nhẹ cẳng tay để 2 bàn tay đặt êm ái trên đùi
            if (seatPose.leftForeArm != null)
                seatPose.leftForeArm.localRotation = seatPose.leftForeArm.localRotation * Quaternion.Euler(-25f, 15f, 0f);
            if (seatPose.rightForeArm != null)
                seatPose.rightForeArm.localRotation = seatPose.rightForeArm.localRotation * Quaternion.Euler(-25f, -15f, 0f);

            seatPose.CaptureArmPoseFromCurrent();
            seatPose.ApplyPose();

            // 7. TẮT HOÀN TOÀN MeshRenderer dạng Capsule tạm bợ trên chính root Player
            MeshRenderer rootMr = player.GetComponent<MeshRenderer>();
            if (rootMr != null)
            {
                rootMr.enabled = false;
            }

            // 8. Đảm bảo toàn bộ renderer của nhân vật luôn hiển thị rõ ràng, không bị culling
            var renderers = player.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                // Không bao giờ bật MeshRenderer của root Player (đó là capsule placeholder!)
                if (r.gameObject == player.gameObject)
                {
                    r.enabled = false;
                    continue;
                }

                // Không bật renderer của vũ khí
                if (r.name.Contains("Gun") || r.name.Contains("Weapon") || r.name.Contains("Axe") || r.name.Contains("Placeholder"))
                {
                    r.enabled = false;
                    continue;
                }

                r.enabled = true;
                SkinnedMeshRenderer smr = r as SkinnedMeshRenderer;
                if (smr != null) smr.updateWhenOffscreen = true;
            }

            // 9. Căn chỉnh Camera ngang tầm mắt nhìn tự nhiên của nhân vật
            Camera cam = player.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                Undo.RecordObject(cam.transform, "Align Camera to Passenger Eyes");
                cam.transform.position = cabin.transform.TransformPoint(new Vector3(-1.15f, 1.15f, -2.85f));
                cam.transform.localRotation = Quaternion.Euler(2.0f, -38.0f, 0f);
                cam.nearClipPlane = 0.03f;
                cam.fieldOfView = 60f;
            }

            EditorUtility.SetDirty(player.gameObject);
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);

            // 10. Chọn vào model nhân vật và phóng to Scene View góc nhìn trực diện
            GameObject focusTarget = ch15 != null ? ch15.gameObject : player.gameObject;
            Selection.activeGameObject = focusTarget;
            EditorGUIUtility.PingObject(focusTarget);
            if (SceneView.lastActiveSceneView != null)
            {
                Vector3 seatCenter = cabin.transform.TransformPoint(new Vector3(-1.70f, 0.75f, -2.60f));
                SceneView.lastActiveSceneView.Frame(new Bounds(seatCenter, new Vector3(1.6f, 1.6f, 1.6f)), false);
            }

            Debug.Log("<color=green><b>[SUCCESS]</b> Nhân vật đã ngồi ngay ngắn, chuẩn mực trên ghế 12A máy bay!</color>");
        }

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
            Material matWing        = GetOrCreateMaterial("M_Plane_Wing", new Color(0.97f, 0.98f, 0.99f), 0.02f, 0.25f);
            Material matChrome      = GetOrCreateMaterial("M_Plane_Chrome", new Color(0.95f, 0.96f, 0.98f), 0.85f, 0.80f);
            Material matEngine      = GetOrCreateMaterial("M_Plane_EngineNacelle", new Color(0.98f, 0.98f, 0.98f), 0.02f, 0.20f);
            Material matSpinner     = GetOrCreateMaterial("M_Plane_SpinnerSpiral", Color.white, 0.10f, 0.40f, texSpinnerSpiral);
            Material matCloud       = GetOrCreateTransparentMaterial("M_Plane_CloudSoft", texCloudSoft, new Color(1.0f, 1.0f, 1.0f, 0.85f));
            Material matSky         = GetOrCreateMaterial("M_Plane_SkyBackdrop", new Color(0.18f, 0.44f, 0.82f), 0.0f, 0.10f);
            Material matNavRed        = GetOrCreateMaterial("M_Plane_NavRed", Color.red, 0f, 0.6f, null, true, Color.red * 4f);
            Material matNavGreen      = GetOrCreateMaterial("M_Plane_NavGreen", new Color(0.05f, 1f, 0.2f), 0f, 0.6f, null, true, new Color(0.05f, 1f, 0.2f) * 4f);
            Material matStrobe        = GetOrCreateMaterial("M_Plane_StrobeWhite", Color.white, 0f, 0.9f, null, true, Color.white * 5f);
            Material matBeaconRed     = GetOrCreateMaterial("M_Plane_BeaconRed", Color.red, 0f, 0.8f, null, true, Color.red * 5f);
            Material matFuselageWhite = GetOrCreateMaterial("M_Plane_FuselageWhite", new Color(0.98f, 0.98f, 0.98f), 0.02f, 0.20f);
            Material matLiveryBlue    = GetOrCreateMaterial("M_Plane_LiveryBlue", new Color(0.08f, 0.22f, 0.55f), 0.05f, 0.35f);
            Material matLiveryGold    = GetOrCreateMaterial("M_Plane_LiveryGold", new Color(0.92f, 0.68f, 0.18f), 0.15f, 0.45f);
            Material matCockpitGlass  = GetOrCreateGlassMaterial("M_Plane_CockpitGlass", new Color(0.08f, 0.12f, 0.16f, 0.88f), 0.85f, 0.95f);
            Material matTireRubber    = GetOrCreateMaterial("M_Plane_TireRubber", new Color(0.12f, 0.12f, 0.12f), 0.0f, 0.20f);
            Material matGearMetal     = GetOrCreateMaterial("M_Plane_GearMetal", new Color(0.85f, 0.87f, 0.90f), 0.85f, 0.70f);

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
            CreateBox("CabinFloor", root.transform, new Vector3(0, 0, 0), new Vector3(3.30f, 0.08f, cabinLength - 0.20f), matCarpet);

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
            // H. TOÀN BỘ MÔ HÌNH MÁY BAY HOÀN CHỈNH 100% (FULL COMMERCIAL AIRLINER)
            // ══════════════════════════════════════════════
            BuildFullAirplaneModel(root, cabinWidth, cabinLength, cabinHeight,
                matFuselageWhite, matLiveryBlue, matLiveryGold, matCockpitGlass,
                matWing, matChrome, matEngine, matSpinner, matTireRubber, matGearMetal,
                matCloud, matSky, matNavRed, matNavGreen, matStrobe, matBeaconRed);

            // ══════════════════════════════════════════════
            // I. ĐỊNH VỊ VỊ TRÍ GÓC NHÌN CHUẨN XÁC: GHẾ 12A (SÁT CỬA SỔ BÊN TRÁI)
            // ══════════════════════════════════════════════
            GameObject viewAnchor = new GameObject("PlayerCutsceneSeat");
            viewAnchor.transform.SetParent(root.transform, false);
            viewAnchor.transform.localPosition = new Vector3(-1.70f, 0.42f, -2.70f); // Tọa độ mặt đệm ghế 12A
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
                    AlignPlayerToSeat12A(root, player);
                    Camera cam = player.GetComponentInChildren<Camera>();
                    if (cam != null) cutscene.cutsceneCamera = cam.transform;
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

                // 1. Tấm ốp chân vách tường nội thất (chỉ trong phạm vi 9 cửa sổ, không đâm xuyên ra ngoài)
                float lowerH = 0.82f;
                float winSpan = 11.5f;
                CreateBox("LowerWall", wall.transform, new Vector3(posX * 0.96f, lowerH * 0.5f, 0), new Vector3(0.04f, lowerH, winSpan), matWall);

                // 2. Vách tường phía trên cửa sổ (nằm gọn bên trong vòm nóc)
                float bayH = 0.78f;
                float bayW = 1.35f;
                float winCenterY = lowerH + bayH * 0.5f; // = 1.21m
                float upperH = height - (lowerH + bayH);
                float upperCenterY = lowerH + bayH + upperH * 0.5f;
                CreateBox("UpperWall", wall.transform, new Vector3(posX * 0.94f, upperCenterY, 0), new Vector3(0.04f, upperH, winSpan), matWall);

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
                new Vector3(2.60f, 0.08f, length - 0.20f), matCeiling);

            // ═══════════════════════════════════════════════════════════
            // 2 TẤM TRẦN VÁT NGHIÊNG 2 BÊN: Nối từ mép trần trung tâm
            // xuống đỉnh vách tường, tạo hình vòm cong thân máy bay
            // Dùng nhiều lát (slats) xếp kề nhau tạo đường cong mượt
            // ═══════════════════════════════════════════════════════════
            int slatCount = 6; // Số lát nghiêng mỗi bên
            float archEdgeX = width * 0.30f; // Mép ngoài tấm trần trung tâm
            float wallTopX = halfW - 0.12f;  // Mép vách tường nằm gọn trong thân trụ tròn
            float archY = height - 0.05f;    // Đáy tấm trần trung tâm
            float wallTopY = height - 0.08f; // Đỉnh vách tường

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
                        new Vector3(slatW, 0.08f, length - 0.20f), matCeiling);
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
                CreateBox("MainBinBody", binGroup.transform, new Vector3(posX, height - 0.32f, 0), new Vector3(0.55f, 0.38f, length), matBin);

                // Máng cong phía dưới hộc
                CreateBox("BinLowerLip", binGroup.transform, new Vector3(posX - side * 0.10f, height - 0.48f, 0), new Vector3(0.26f, 0.06f, length), matShell);

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

            // Vách trái và vách phải (nằm gọn bên trong thân trụ tròn)
            CreateBox("FwdWall_L", fwd.transform, new Vector3(-1.15f, height * 0.5f, 0), new Vector3(1.05f, height, 0.08f), matWall);
            CreateBox("FwdWall_R", fwd.transform, new Vector3( 1.15f, height * 0.5f, 0), new Vector3(1.05f, height, 0.08f), matWall);
            CreateBox("FwdLintel", fwd.transform, new Vector3(0, height - 0.25f, 0), new Vector3(1.30f, 0.50f, 0.08f), matWall);

            // Cửa buồng lái bọc kim loại kiên cố
            CreateBox("CockpitDoor", fwd.transform, new Vector3(0, (height - 0.50f) * 0.5f, 0.02f), new Vector3(1.05f, height - 0.50f, 0.06f), matDoor);

            // Bảng hiệu thoát hiểm EXIT sáng rực phía trên cửa
            CreateBox("ExitSign", fwd.transform, new Vector3(0, height - 0.30f, -0.07f), new Vector3(0.55f, 0.20f, 0.04f), matSigns);

            // Vách sau cabin
            float aftZ = -length * 0.5f;
            GameObject aft = new GameObject("Bulkhead_Aft");
            aft.transform.SetParent(parent, false);
            aft.transform.localPosition = new Vector3(0, 0, aftZ);
            CreateBox("AftWall", aft.transform, new Vector3(0, height * 0.5f, 0), new Vector3(3.30f, height, 0.08f), matWall);
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

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 localPos, Vector3 size, Quaternion localRot, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
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

        private struct CrossSection
        {
            public float z;
            public float rx;
            public float ry;
            public float cy;
            public CrossSection(float z, float rx, float ry, float cy)
            {
                this.z = z;
                this.rx = rx;
                this.ry = ry;
                this.cy = cy;
            }
        }

        private static Mesh BuildLoftedTubeMesh(string name, CrossSection[] rings, int radialSegments, bool capStart, bool capEnd)
        {
            Mesh mesh = new Mesh();
            mesh.name = name;

            int ringCount = rings.Length;
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            float minZ = rings[0].z;
            float maxZ = rings[ringCount - 1].z;

            for (int r = 0; r < ringCount; r++)
            {
                CrossSection cs = rings[r];
                float v = Mathf.InverseLerp(minZ, maxZ, cs.z);

                for (int i = 0; i <= radialSegments; i++)
                {
                    float u = (float)i / radialSegments;
                    float angle = u * Mathf.PI * 2f;
                    float x = Mathf.Cos(angle) * cs.rx;
                    float y = Mathf.Sin(angle) * cs.ry + cs.cy;

                    verts.Add(new Vector3(x, y, cs.z));
                    uvs.Add(new Vector2(u, v));
                }
            }

            List<int> tris = new List<int>();

            for (int r = 0; r < ringCount - 1; r++)
            {
                int row0 = r * (radialSegments + 1);
                int row1 = (r + 1) * (radialSegments + 1);

                for (int i = 0; i < radialSegments; i++)
                {
                    int r0_0 = row0 + i;
                    int r0_1 = row0 + i + 1;
                    int r1_0 = row1 + i;
                    int r1_1 = row1 + i + 1;

                    // Mặt ngoài hướng chuẩn ra ngoài (Outward facing normal)
                    tris.Add(r0_0); tris.Add(r1_0); tris.Add(r0_1);
                    tris.Add(r0_1); tris.Add(r1_0); tris.Add(r1_1);
                }
            }

            if (capStart)
            {
                int capIdx = verts.Count;
                verts.Add(new Vector3(0, rings[0].cy, rings[0].z));
                uvs.Add(new Vector2(0.5f, 0f));
                int row0 = 0;
                for (int i = 0; i < radialSegments; i++)
                {
                    tris.Add(capIdx); tris.Add(row0 + i + 1); tris.Add(row0 + i);
                }
            }

            if (capEnd)
            {
                int capIdx = verts.Count;
                int lastR = ringCount - 1;
                verts.Add(new Vector3(0, rings[lastR].cy, rings[lastR].z));
                uvs.Add(new Vector2(0.5f, 1f));
                int rowEnd = lastR * (radialSegments + 1);
                for (int i = 0; i < radialSegments; i++)
                {
                    tris.Add(capIdx); tris.Add(rowEnd + i); tris.Add(rowEnd + i + 1);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildLoftedArcMesh(string name, float zStart, float zEnd, int zSteps,
            float startAngleDeg, float endAngleDeg, int radialSteps,
            float rx, float ry, float cy)
        {
            Mesh mesh = new Mesh();
            mesh.name = name;

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            for (int zIdx = 0; zIdx <= zSteps; zIdx++)
            {
                float tZ = (float)zIdx / zSteps;
                float z = Mathf.Lerp(zStart, zEnd, tZ);

                for (int r = 0; r <= radialSteps; r++)
                {
                    float tR = (float)r / radialSteps;
                    float angleDeg = Mathf.Lerp(startAngleDeg, endAngleDeg, tR);
                    float rad = angleDeg * Mathf.Deg2Rad;

                    float x = Mathf.Cos(rad) * rx;
                    float y = Mathf.Sin(rad) * ry + cy;

                    verts.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(tR, tZ * 3f));
                }
            }

            for (int zIdx = 0; zIdx < zSteps; zIdx++)
            {
                int row0 = zIdx * (radialSteps + 1);
                int row1 = (zIdx + 1) * (radialSteps + 1);

                for (int r = 0; r < radialSteps; r++)
                {
                    int r0_0 = row0 + r;
                    int r0_1 = row0 + r + 1;
                    int r1_0 = row1 + r;
                    int r1_1 = row1 + r + 1;

                    // Mặt ngoài hướng chuẩn ra ngoài (Outward facing normal)
                    tris.Add(r0_0); tris.Add(r1_0); tris.Add(r0_1);
                    tris.Add(r0_1); tris.Add(r1_0); tris.Add(r1_1);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildSweptWingMesh(string name, bool isRight)
        {
            Mesh mesh = new Mesh();
            mesh.name = name;

            float dir = isRight ? 1f : -1f;

            float[] posX  = new float[] { 2.05f * dir, 7.50f * dir, 15.50f * dir, 15.80f * dir };
            float[] posY  = new float[] { 0.85f,       1.18f,       1.65f,        3.15f };
            float[] leadZ = new float[] { 0.60f,      -1.80f,      -4.80f,       -5.30f };
            float[] traiZ = new float[] {-4.50f,      -5.40f,      -6.50f,       -6.40f };
            float[] thick = new float[] { 0.42f,       0.28f,       0.14f,        0.08f };

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            for (int s = 0; s < 4; s++)
            {
                float midZ = Mathf.Lerp(leadZ[s], traiZ[s], 0.35f);
                float x = posX[s];
                float y = posY[s];
                float t = thick[s];

                verts.Add(new Vector3(x, y, leadZ[s]));
                uvs.Add(new Vector2(0f, (float)s / 3f));

                verts.Add(new Vector3(x, y + t * 0.65f, midZ));
                uvs.Add(new Vector2(0.33f, (float)s / 3f));

                verts.Add(new Vector3(x, y, traiZ[s]));
                uvs.Add(new Vector2(0.66f, (float)s / 3f));

                verts.Add(new Vector3(x, y - t * 0.35f, midZ));
                uvs.Add(new Vector2(1f, (float)s / 3f));
            }

            List<int> tris = new List<int>();

            for (int s = 0; s < 3; s++)
            {
                int r0 = s * 4;
                int r1 = (s + 1) * 4;

                for (int e = 0; e < 4; e++)
                {
                    int nextE = (e + 1) % 4;
                    int a = r0 + e;
                    int b = r0 + nextE;
                    int c = r1 + nextE;
                    int d = r1 + e;

                    if (isRight)
                    {
                        tris.Add(a); tris.Add(b); tris.Add(c);
                        tris.Add(a); tris.Add(c); tris.Add(d);
                    }
                    else
                    {
                        tris.Add(a); tris.Add(c); tris.Add(b);
                        tris.Add(a); tris.Add(d); tris.Add(c);
                    }
                }
            }

            int tipBase = 3 * 4;
            if (isRight)
            {
                tris.Add(tipBase + 0); tris.Add(tipBase + 1); tris.Add(tipBase + 2);
                tris.Add(tipBase + 0); tris.Add(tipBase + 2); tris.Add(tipBase + 3);
            }
            else
            {
                tris.Add(tipBase + 0); tris.Add(tipBase + 2); tris.Add(tipBase + 1);
                tris.Add(tipBase + 0); tris.Add(tipBase + 3); tris.Add(tipBase + 2);
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildVerticalFinMesh(string name)
        {
            Mesh mesh = new Mesh();
            mesh.name = name;

            float[] posY  = new float[] {  2.45f,   5.20f,   8.15f };
            float[] leadZ = new float[] {-12.20f, -14.30f, -16.20f };
            float[] traiZ = new float[] {-16.80f, -17.50f, -18.20f };
            float[] thick = new float[] {  0.36f,   0.22f,   0.10f };

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            for (int l = 0; l < 3; l++)
            {
                float midZ = Mathf.Lerp(leadZ[l], traiZ[l], 0.32f);
                float y = posY[l];
                float halfT = thick[l] * 0.5f;

                verts.Add(new Vector3(0, y, leadZ[l]));
                uvs.Add(new Vector2(0f, (float)l / 2f));

                verts.Add(new Vector3(-halfT, y, midZ));
                uvs.Add(new Vector2(0.33f, (float)l / 2f));

                verts.Add(new Vector3(0, y, traiZ[l]));
                uvs.Add(new Vector2(0.66f, (float)l / 2f));

                verts.Add(new Vector3(halfT, y, midZ));
                uvs.Add(new Vector2(1f, (float)l / 2f));
            }

            List<int> tris = new List<int>();

            for (int l = 0; l < 2; l++)
            {
                int r0 = l * 4;
                int r1 = (l + 1) * 4;

                for (int e = 0; e < 4; e++)
                {
                    int nextE = (e + 1) % 4;
                    int a = r0 + e;
                    int b = r0 + nextE;
                    int c = r1 + nextE;
                    int d = r1 + e;

                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(a); tris.Add(c); tris.Add(d);
                }
            }

            int tipBase = 2 * 4;
            tris.Add(tipBase + 0); tris.Add(tipBase + 1); tris.Add(tipBase + 2);
            tris.Add(tipBase + 0); tris.Add(tipBase + 2); tris.Add(tipBase + 3);

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildHorizontalStabMesh(string name, bool isRight)
        {
            Mesh mesh = new Mesh();
            mesh.name = name;

            float dir = isRight ? 1f : -1f;

            float[] posX  = new float[] { 0.70f * dir, 5.40f * dir };
            float[] posY  = new float[] { 1.85f,       2.20f };
            float[] leadZ = new float[] {-15.80f,     -17.50f };
            float[] traiZ = new float[] {-18.00f,     -18.80f };
            float[] thick = new float[] {  0.18f,       0.08f };

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            for (int s = 0; s < 2; s++)
            {
                float midZ = Mathf.Lerp(leadZ[s], traiZ[s], 0.35f);
                float x = posX[s];
                float y = posY[s];
                float t = thick[s];

                verts.Add(new Vector3(x, y, leadZ[s]));
                uvs.Add(new Vector2(0f, (float)s));

                verts.Add(new Vector3(x, y + t * 0.5f, midZ));
                uvs.Add(new Vector2(0.33f, (float)s));

                verts.Add(new Vector3(x, y, traiZ[s]));
                uvs.Add(new Vector2(0.66f, (float)s));

                verts.Add(new Vector3(x, y - t * 0.5f, midZ));
                uvs.Add(new Vector2(1f, (float)s));
            }

            List<int> tris = new List<int>();

            int r0 = 0;
            int r1 = 4;

            for (int e = 0; e < 4; e++)
            {
                int nextE = (e + 1) % 4;
                int a = r0 + e;
                int b = r0 + nextE;
                int c = r1 + nextE;
                int d = r1 + e;

                if (isRight)
                {
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(a); tris.Add(c); tris.Add(d);
                }
                else
                {
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(a); tris.Add(d); tris.Add(c);
                }
            }

            if (isRight)
            {
                tris.Add(r1 + 0); tris.Add(r1 + 1); tris.Add(r1 + 2);
                tris.Add(r1 + 0); tris.Add(r1 + 2); tris.Add(r1 + 3);
            }
            else
            {
                tris.Add(r1 + 0); tris.Add(r1 + 2); tris.Add(r1 + 1);
                tris.Add(r1 + 0); tris.Add(r1 + 3); tris.Add(r1 + 2);
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ═════════════════════════════════════════════════════════════════
        // HỆ THỐNG DỰNG NGOẠI THẤT MÁY BAY HOÀN CHỈNH 100% (SEAMLESS AIRLINER)
        // ═════════════════════════════════════════════════════════════════

        private static void BuildFullAirplaneModel(GameObject root, float cabinWidth, float cabinLength, float cabinHeight,
            Material matFuselageWhite, Material matLiveryBlue, Material matLiveryGold, Material matCockpitGlass,
            Material matWing, Material matChrome, Material matEngine, Material matSpinner,
            Material matTireRubber, Material matGearMetal,
            Material matCloud, Material matSky, Material matNavRed, Material matNavGreen, Material matStrobe, Material matBeaconRed)
        {
            GameObject exteriorObj = new GameObject("Airplane_Exterior");
            exteriorObj.transform.SetParent(root.transform, false);
            exteriorObj.transform.localPosition = Vector3.zero;

            AirplaneCabinExterior exteriorComp = exteriorObj.AddComponent<AirplaneCabinExterior>();

            // 1. MŨI MÁY BAY LIỀN KHỐI KHÍ ĐỘNG HỌC (SEAMLESS NOSE)
            CrossSection[] noseRings = new CrossSection[]
            {
                new CrossSection( 8.75f, 2.05f, 1.25f, 1.225f), // Khớp 100% tuyệt đối với đầu cabin, không hở 1 khe!
                new CrossSection(10.50f, 2.02f, 1.24f, 1.220f),
                new CrossSection(12.00f, 1.92f, 1.20f, 1.180f),
                new CrossSection(13.50f, 1.70f, 1.10f, 1.100f),
                new CrossSection(14.80f, 1.35f, 0.92f, 1.000f),
                new CrossSection(15.80f, 0.90f, 0.68f, 0.920f),
                new CrossSection(16.60f, 0.45f, 0.38f, 0.880f),
                new CrossSection(17.10f, 0.08f, 0.08f, 0.860f)
            };
            Mesh noseMesh = BuildLoftedTubeMesh("NoseConeMesh", noseRings, 24, false, true);
            GameObject noseObj = new GameObject("Fuselage_NoseCone");
            noseObj.transform.SetParent(exteriorObj.transform, false);
            noseObj.AddComponent<MeshFilter>().sharedMesh = noseMesh;
            noseObj.AddComponent<MeshRenderer>().sharedMaterial = matFuselageWhite;

            // Kính Buồng Lái Khí Động Học (Cockpit Windshield Visor) ôm sát sống mũi
            GameObject windshieldGroup = new GameObject("Cockpit_Windshield_Visor");
            windshieldGroup.transform.SetParent(exteriorObj.transform, false);

            GameObject winL = CreateBox("Win_Center_L", windshieldGroup.transform, new Vector3(-0.48f, 2.10f, 13.45f), new Vector3(0.65f, 0.42f, 0.04f), matCockpitGlass);
            winL.transform.localRotation = Quaternion.Euler(28f, -12f, 0);

            GameObject winR = CreateBox("Win_Center_R", windshieldGroup.transform, new Vector3( 0.48f, 2.10f, 13.45f), new Vector3(0.65f, 0.42f, 0.04f), matCockpitGlass);
            winR.transform.localRotation = Quaternion.Euler(28f,  12f, 0);

            GameObject winSideL = CreateBox("Win_Side_L", windshieldGroup.transform, new Vector3(-1.18f, 1.98f, 12.85f), new Vector3(0.04f, 0.38f, 0.70f), matCockpitGlass);
            winSideL.transform.localRotation = Quaternion.Euler(14f, -22f, 0);

            GameObject winSideR = CreateBox("Win_Side_R", windshieldGroup.transform, new Vector3( 1.18f, 1.98f, 12.85f), new Vector3(0.04f, 0.38f, 0.70f), matCockpitGlass);
            winSideR.transform.localRotation = Quaternion.Euler(14f,  22f, 0);

            CreateBox("Pitot_L", exteriorObj.transform, new Vector3(-0.55f, 0.88f, 16.4f), new Vector3(0.03f, 0.03f, 0.42f), matChrome);
            CreateBox("Pitot_R", exteriorObj.transform, new Vector3( 0.55f, 0.88f, 16.4f), new Vector3(0.03f, 0.03f, 0.42f), matChrome);

            // Càng Đáp Mũi (Nose Landing Gear)
            GameObject noseGear = new GameObject("Nose_Landing_Gear");
            noseGear.transform.SetParent(exteriorObj.transform, false);
            float gearZ = 13.5f;

            CreateBox("Oleo_Strut", noseGear.transform, new Vector3(0, -0.70f, gearZ), new Vector3(0.12f, 1.45f, 0.12f), matGearMetal);
            CreateCylinder("Tire_L", noseGear.transform, new Vector3(-0.20f, -1.45f, gearZ), new Vector3(0.68f, 0.08f, 0.68f), Quaternion.Euler(0, 0, 90f), matTireRubber);
            CreateCylinder("Rim_L", noseGear.transform, new Vector3(-0.20f, -1.45f, gearZ), new Vector3(0.38f, 0.085f, 0.38f), Quaternion.Euler(0, 0, 90f), matGearMetal);
            CreateCylinder("Tire_R", noseGear.transform, new Vector3( 0.20f, -1.45f, gearZ), new Vector3(0.68f, 0.08f, 0.68f), Quaternion.Euler(0, 0, 90f), matTireRubber);
            CreateCylinder("Rim_R", noseGear.transform, new Vector3( 0.20f, -1.45f, gearZ), new Vector3(0.38f, 0.085f, 0.38f), Quaternion.Euler(0, 0, 90f), matGearMetal);

            GameObject taxiL = CreateBox("TaxiLight", noseGear.transform, new Vector3(0, -0.92f, gearZ + 0.10f), new Vector3(0.18f, 0.10f, 0.08f), matChrome);
            Light tl = taxiL.AddComponent<Light>();
            tl.type = LightType.Point;
            tl.color = new Color(1.0f, 0.98f, 0.92f);
            tl.range = 8.0f;
            tl.intensity = 2.0f;

            // 2. THÂN SAU & ĐUÔI MÁY BAY LIỀN KHỐI (SEAMLESS AFT FUSELAGE & TAIL CONE)
            CrossSection[] tailRings = new CrossSection[]
            {
                new CrossSection(-8.75f,  2.05f, 1.25f, 1.225f), // Khớp 100% tuyệt đối với đuôi cabin!
                new CrossSection(-10.50f, 2.00f, 1.24f, 1.280f),
                new CrossSection(-12.50f, 1.82f, 1.18f, 1.380f),
                new CrossSection(-14.50f, 1.52f, 1.05f, 1.520f),
                new CrossSection(-16.20f, 1.15f, 0.85f, 1.680f),
                new CrossSection(-17.50f, 0.72f, 0.58f, 1.800f),
                new CrossSection(-18.60f, 0.32f, 0.28f, 1.880f)
            };
            Mesh tailMesh = BuildLoftedTubeMesh("AftTailMesh", tailRings, 24, false, false);
            GameObject tailObj = new GameObject("Fuselage_TailSection");
            tailObj.transform.SetParent(exteriorObj.transform, false);
            tailObj.AddComponent<MeshFilter>().sharedMesh = tailMesh;
            tailObj.AddComponent<MeshRenderer>().sharedMaterial = matFuselageWhite;

            // Ống xả APU bằng Titan ở chóp đuôi
            CreateCylinder("APU_Nozzle", tailObj.transform, new Vector3(0, 1.88f, -18.75f), new Vector3(0.34f, 0.12f, 0.34f), Quaternion.Euler(90f, 0, 0), matChrome);

            // 3. CÁNH ĐUÔI ĐỨNG KHỔNG LỒ 6M LIỀN KHỐI (VERTICAL STABILIZER)
            Mesh finMesh = BuildVerticalFinMesh("VerticalFinMesh");
            GameObject finObj = new GameObject("Vertical_Stabilizer");
            finObj.transform.SetParent(exteriorObj.transform, false);
            finObj.AddComponent<MeshFilter>().sharedMesh = finMesh;
            finObj.AddComponent<MeshRenderer>().sharedMaterial = matLiveryBlue;

            // Đèn Beacon đỏ trên chóp đuôi đứng
            GameObject beaconObj = CreateBox("Tail_Beacon_Red", finObj.transform, new Vector3(0, 8.25f, -17.4f), new Vector3(0.12f, 0.14f, 0.16f), matBeaconRed);
            Light beaconL = beaconObj.AddComponent<Light>();
            beaconL.type = LightType.Point;
            beaconL.color = Color.red;
            beaconL.range = 8.0f;
            beaconL.intensity = 2.5f;
            exteriorComp.tailBeaconLight = beaconL;

            // 4. HAI CÁNH ĐUÔI NGANG THĂNG BẰNG (HORIZONTAL STABILIZERS)
            Mesh hMeshL = BuildHorizontalStabMesh("HorizStab_L", false);
            GameObject hObjL = new GameObject("HorizontalStabilizer_Left");
            hObjL.transform.SetParent(exteriorObj.transform, false);
            hObjL.AddComponent<MeshFilter>().sharedMesh = hMeshL;
            hObjL.AddComponent<MeshRenderer>().sharedMaterial = matWing;

            Mesh hMeshR = BuildHorizontalStabMesh("HorizStab_R", true);
            GameObject hObjR = new GameObject("HorizontalStabilizer_Right");
            hObjR.transform.SetParent(exteriorObj.transform, false);
            hObjR.AddComponent<MeshFilter>().sharedMesh = hMeshR;
            hObjR.AddComponent<MeshRenderer>().sharedMaterial = matWing;

            // 5. ĐẦY ĐỦ 2 CÁNH CHÍNH KHÍ ĐỘNG HỌC LIỀN MẠCH (FULL SWEPT WINGS)
            Mesh wingMeshL = BuildSweptWingMesh("Wing_Left", false);
            GameObject wingL = new GameObject("Wing_Left");
            wingL.transform.SetParent(exteriorObj.transform, false);
            wingL.AddComponent<MeshFilter>().sharedMesh = wingMeshL;
            wingL.AddComponent<MeshRenderer>().sharedMaterial = matWing;

            Mesh wingMeshR = BuildSweptWingMesh("Wing_Right", true);
            GameObject wingR = new GameObject("Wing_Right");
            wingR.transform.SetParent(exteriorObj.transform, false);
            wingR.AddComponent<MeshFilter>().sharedMesh = wingMeshR;
            wingR.AddComponent<MeshRenderer>().sharedMaterial = matWing;

            // 3 Ốp thoi Flap Track Fairings dưới mỗi cánh & Đèn dẫn đường
            for (int side = -1; side <= 1; side += 2)
            {
                bool isLeft = (side < 0);
                string sideCode = isLeft ? "L" : "R";
                Transform wParent = isLeft ? wingL.transform : wingR.transform;

                CreateBox("Flap_Fairing_1", wParent, new Vector3(side * 4.2f, 0.75f, -4.5f), new Vector3(0.22f, 0.25f, 1.6f), matWing);
                CreateBox("Flap_Fairing_2", wParent, new Vector3(side * 6.5f, 0.95f, -5.2f), new Vector3(0.20f, 0.22f, 1.4f), matWing);
                CreateBox("Flap_Fairing_3", wParent, new Vector3(side * 8.8f, 1.15f, -5.8f), new Vector3(0.18f, 0.20f, 1.2f), matWing);

                // Đèn định vị hàng không (Nav Lights: Trái ĐỎ, Phải XANH LÁ)
                Material navMat = isLeft ? matNavRed : matNavGreen;
                Color navCol = isLeft ? Color.red : new Color(0.05f, 1f, 0.2f);
                GameObject navObj = CreateBox("NavLight_" + sideCode, wParent, new Vector3(side * 15.6f, 1.70f, -4.9f), new Vector3(0.12f, 0.12f, 0.16f), navMat);
                Light navL = navObj.AddComponent<Light>();
                navL.type = LightType.Point;
                navL.color = navCol;
                navL.range = 2.5f;
                navL.intensity = 2.5f;

                if (isLeft) exteriorComp.wingNavLight = navL;
                else exteriorComp.wingNavLightRight = navL;

                // Đèn chớp Strobe trắng
                GameObject strobeObj = CreateBox("StrobeLight_" + sideCode, wParent, new Vector3(side * 15.6f, 1.70f, -5.8f), new Vector3(0.12f, 0.12f, 0.16f), matStrobe);
                Light strobeL = strobeObj.AddComponent<Light>();
                strobeL.type = LightType.Point;
                strobeL.color = Color.white;
                strobeL.range = 2.5f;
                strobeL.intensity = 0f;

                if (isLeft)
                {
                    exteriorComp.wingStrobeLight = strobeL;
                    exteriorComp.wingStrobeRenderer = strobeObj.GetComponent<Renderer>();
                }
                else
                {
                    exteriorComp.wingStrobeLightRight = strobeL;
                    exteriorComp.wingStrobeRendererRight = strobeObj.GetComponent<Renderer>();
                }

                // C. CỤM CÀNG ĐÁP CHÍNH DƯỚI BỤNG CÁNH (MAIN GEAR)
                GameObject mainGear = new GameObject("Main_Gear_" + sideCode);
                mainGear.transform.SetParent(exteriorObj.transform, false);
                float gearX = side * 2.85f;

                CreateBox("Oleo", mainGear.transform, new Vector3(gearX, -0.85f, -2.8f), new Vector3(0.18f, 1.75f, 0.18f), matGearMetal);
                CreateCylinder("MainTire_1", mainGear.transform, new Vector3(side * 2.65f, -1.55f, -2.8f), new Vector3(1.02f, 0.12f, 1.02f), Quaternion.Euler(0, 0, 90f), matTireRubber);
                CreateCylinder("MainRim_1",  mainGear.transform, new Vector3(side * 2.65f, -1.55f, -2.8f), new Vector3(0.55f, 0.125f, 0.55f), Quaternion.Euler(0, 0, 90f), matGearMetal);
                CreateCylinder("MainTire_2", mainGear.transform, new Vector3(side * 3.05f, -1.55f, -2.8f), new Vector3(1.02f, 0.12f, 1.02f), Quaternion.Euler(0, 0, 90f), matTireRubber);
                CreateCylinder("MainRim_2",  mainGear.transform, new Vector3(side * 3.05f, -1.55f, -2.8f), new Vector3(0.55f, 0.125f, 0.55f), Quaternion.Euler(0, 0, 90f), matGearMetal);
            }

            // 6. ĐÔI ĐỘNG CƠ TURBOFAN CFM56 (DUAL ENGINES)
            for (int side = -1; side <= 1; side += 2)
            {
                bool isLeft = (side < 0);
                string sideName = isLeft ? "Left" : "Right";

                GameObject engObj = new GameObject("JetEngine_" + sideName);
                engObj.transform.SetParent(exteriorObj.transform, false);
                engObj.transform.localPosition = new Vector3(side * 4.2f, 0.40f, -1.8f);

                // Pylon treo cánh
                CreateBox("Pylon", engObj.transform, new Vector3(0, 0.55f, 0), new Vector3(0.22f, 0.45f, 2.4f), matWing);

                // Thân động cơ Nacelle bo tròn
                CreateCylinder("NacelleBody", engObj.transform, Vector3.zero, new Vector3(1.70f, 1.4f, 1.70f), Quaternion.Euler(90f, 0, 0), matEngine);

                // Vành miệng hút gió mạ Crom
                CreateCylinder("ChromeLip", engObj.transform, new Vector3(0, 0, 1.42f), new Vector3(1.74f, 0.08f, 1.74f), Quaternion.Euler(90f, 0, 0), matChrome);

                // Quạt turbine titan
                CreateBox("TurbineFan", engObj.transform, new Vector3(0, 0, 0.55f), new Vector3(1.35f, 1.35f, 0.05f), matChrome);

                // Nón xoay Spinner hoa văn xoắn ốc (xoay 1200 RPM)
                GameObject spinner = CreateCylinder("SpinnerBullet", engObj.transform, new Vector3(0, 0, 0.62f), new Vector3(0.42f, 0.26f, 0.42f), Quaternion.Euler(90f, 0, 0), matSpinner);
                if (isLeft) exteriorComp.engineSpinner = spinner.transform;
                else exteriorComp.engineSpinnerRight = spinner.transform;

                // Ống xả phía sau
                CreateCylinder("ExhaustNozzle", engObj.transform, new Vector3(0, 0, -1.45f), new Vector3(1.30f, 0.18f, 1.30f), Quaternion.Euler(90f, 0, 0), matChrome);

                // Đèn lửa sự cố
                GameObject engGlow = new GameObject("EngineGlowLight");
                engGlow.transform.SetParent(engObj.transform, false);
                engGlow.transform.localPosition = new Vector3(0, 0, -1.2f);
                Light engGL = engGlow.AddComponent<Light>();
                engGL.type = LightType.Point;
                engGL.color = new Color(1f, 0.35f, 0f);
                engGL.range = 8.0f;
                engGL.intensity = 0f;

                if (isLeft) exteriorComp.engineGlowLight = engGL;
                else exteriorComp.engineGlowLightRight = engGL;
            }

            // 7. VỎ THÂN MÁY BAY ĐOÀN GIỮA ĐỒNG BỘ 100% VỚI MŨI VÀ ĐUÔI (UNIFIED CYLINDER)
            GameObject midShell = new GameObject("Fuselage_Middle_Shell");
            midShell.transform.SetParent(exteriorObj.transform, false);

            // A. Đoạn ống trụ tròn kín hoàn toàn phía trước cửa sổ (Z = +5.40m đến +8.75m, khớp 100% với mũi!)
            CrossSection[] midFwdRings = new CrossSection[]
            {
                new CrossSection(5.40f, 2.05f, 1.25f, 1.225f),
                new CrossSection(8.75f, 2.05f, 1.25f, 1.225f)
            };
            Mesh midFwdMesh = BuildLoftedTubeMesh("MidFuselageFwdMesh", midFwdRings, 24, false, false);
            GameObject midFwdObj = new GameObject("Fuselage_Mid_Fwd");
            midFwdObj.transform.SetParent(midShell.transform, false);
            midFwdObj.AddComponent<MeshFilter>().sharedMesh = midFwdMesh;
            midFwdObj.AddComponent<MeshRenderer>().sharedMaterial = matFuselageWhite;

            // B. Đoạn ống trụ tròn kín hoàn toàn phía sau cửa sổ (Z = -8.75m đến -5.40m, khớp 100% với đuôi!)
            CrossSection[] midAftRings = new CrossSection[]
            {
                new CrossSection(-8.75f, 2.05f, 1.25f, 1.225f),
                new CrossSection(-5.40f, 2.05f, 1.25f, 1.225f)
            };
            Mesh midAftMesh = BuildLoftedTubeMesh("MidFuselageAftMesh", midAftRings, 24, false, false);
            GameObject midAftObj = new GameObject("Fuselage_Mid_Aft");
            midAftObj.transform.SetParent(midShell.transform, false);
            midAftObj.AddComponent<MeshFilter>().sharedMesh = midAftMesh;
            midAftObj.AddComponent<MeshRenderer>().sharedMaterial = matFuselageWhite;

            // C. Vòm trần khí động học uốn cong che nóc khoang cửa sổ (Z = -5.40m đến +5.40m)
            Mesh crownMesh = BuildLoftedArcMesh("MidFuselageCrown", -5.40f, 5.40f, 12, 15f, 165f, 24, 2.05f, 1.25f, 1.225f);
            GameObject crownObj = new GameObject("Fuselage_Crown_Roof");
            crownObj.transform.SetParent(midShell.transform, false);
            crownObj.AddComponent<MeshFilter>().sharedMesh = crownMesh;
            crownObj.AddComponent<MeshRenderer>().sharedMaterial = matFuselageWhite;

            // D. Vòm bụng khí động học uốn cong che đáy khoang cửa sổ (Z = -5.40m đến +5.40m)
            Mesh bellyMesh = BuildLoftedArcMesh("MidFuselageBelly", -5.40f, 5.40f, 12, 195f, 345f, 24, 2.05f, 1.25f, 1.225f);
            GameObject bellyObj = new GameObject("Fuselage_Belly_Hull");
            bellyObj.transform.SetParent(midShell.transform, false);
            bellyObj.AddComponent<MeshFilter>().sharedMesh = bellyMesh;
            bellyObj.AddComponent<MeshRenderer>().sharedMaterial = matFuselageWhite;

            // E. Bụng phình Wing-Body Fairing chứa hệ thống điều hòa & hộc bánh xe
            CreateBox("Wing_Belly_Fairing", midShell.transform, new Vector3(0, -0.22f, -2.5f), new Vector3(cabinWidth * 0.92f, 0.40f, 6.8f), matFuselageWhite);

            // Dải sơn Livery Hàng Không (Royal Blue & Gold cheatlines) chạy suốt thân
            for (int side = -1; side <= 1; side += 2)
            {
                string sideCode = side < 0 ? "L" : "R";
                float posX = side * (cabinWidth * 0.5f + 0.045f);

                // Dải xanh hoàng gia chạy từ mũi qua thân cabin tới đuôi
                CreateBox("Livery_Blue_" + sideCode, midShell.transform, new Vector3(posX, 0.55f, 0), new Vector3(0.015f, 0.26f, cabinLength + 0.05f), matLiveryBlue);
                CreateBox("Livery_Gold_" + sideCode, midShell.transform, new Vector3(posX + side * 0.001f, 0.38f, 0), new Vector3(0.015f, 0.05f, cabinLength + 0.05f), matLiveryGold);
            }

            // Vòm ăng-ten vệ tinh Wi-Fi SatCom & Ăng-ten VHF vây cá
            CreateBox("SatCom_Dome", midShell.transform, new Vector3(0, cabinHeight + 0.18f, -2.0f), new Vector3(0.70f, 0.18f, 2.2f), matFuselageWhite);
            CreateBox("VHF_Antenna_Fwd", midShell.transform, new Vector3(0, cabinHeight + 0.25f, 4.5f), new Vector3(0.05f, 0.30f, 0.45f), matFuselageWhite);
            CreateBox("VHF_Antenna_Aft", midShell.transform, new Vector3(0, cabinHeight + 0.25f, -5.0f), new Vector3(0.05f, 0.30f, 0.45f), matFuselageWhite);

            // 8. MÂY TRÔI & ÁNH SÁNG
            SetupAtmosphereAndSky(exteriorObj.transform, exteriorComp, matCloud);
        }

        private static void SetupAtmosphereAndSky(Transform parent, AirplaneCabinExterior comp, Material matCloud)
        {
            GameObject skyEnv = new GameObject("Atmosphere_Environment");
            skyEnv.transform.SetParent(parent, false);

            List<Transform> cloudList = new List<Transform>();
            List<Renderer> cloudRends = new List<Renderer>();

            for (int i = 0; i < 4; i++)
            {
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Quad);
                c.name = "CloudSheet_" + i;
                c.transform.SetParent(skyEnv.transform, false);
                c.transform.localPosition = new Vector3(-12.0f - i * 4.0f, -1.0f + i * 0.8f, -20.0f + i * 14.0f);
                c.transform.localRotation = Quaternion.Euler(15f, -90f, 0f);
                c.transform.localScale = new Vector3(25f, 10f, 1f);
                Renderer r = c.GetComponent<Renderer>();
                if (r != null && matCloud != null)
                {
                    r.sharedMaterial = matCloud;
                    cloudRends.Add(r);
                }
                cloudList.Add(c.transform);
            }

            if (comp != null)
            {
                comp.cloudLayers = cloudList.ToArray();
                comp.cloudRenderers = cloudRends.ToArray();
            }
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

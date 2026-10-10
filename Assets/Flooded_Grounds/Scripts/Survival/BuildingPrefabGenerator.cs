using UnityEngine;
using HorrorGame.Environment;

namespace HorrorGame.Survival
{
    /// <summary>
    /// Tạo mô hình các công trình sinh tồn phong cách The Forest.
    /// Thiết kế rỗng bên trong với cửa ra vào thực sự đi vào được (không bị tàng hình chặn cửa),
    /// tích hợp Nơi Nghỉ Ngơi (ShelterRest), Bếp Nướng Thịt (CookingStation) và Rương Đồ (LootChest).
    /// </summary>
    public static class BuildingPrefabGenerator
    {
        // ═════════════════════════════════════════════════════
        // 1. NHÀ GỖ (WOOD CABIN) - Cửa đi vào được, có giường ngủ
        // ═════════════════════════════════════════════════════
        public static GameObject CreateWoodHouse()
        {
            GameObject house = new GameObject("WoodHouse");

            Material woodMat = GetMaterial("BLD_Cabins", new Color(0.55f, 0.35f, 0.16f));
            Material roofMat = GetMaterial("BLD_Cabins_Mossy", new Color(0.38f, 0.24f, 0.10f));
            Material floorMat = GetMaterial("BLD_Cabins", new Color(0.42f, 0.26f, 0.12f));

            float wallHeight = 3.2f; // Tăng lên 3.2m để không gian trần nhà cao ráo
            float wallWidth = 4.4f;  // Rộng 4.4m
            float wallThick = 0.18f;

            // === SÀN NHÀ ===
            CreatePrimitive(house, PrimitiveType.Cube, new Vector3(0, 0.05f, 0),
                new Vector3(wallWidth, 0.10f, wallWidth), floorMat, "Floor", true);

            // DỐC BƯỚC VÀO CỬA (Ngưỡng cửa vát dốc giúp người chơi bước vào êm ái, không bị khựng)
            GameObject doorRamp = CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(0, 0.02f, wallWidth / 2f + 0.35f),
                new Vector3(2.0f, 0.04f, 0.7f), floorMat, "DoorRamp", true);
            doorRamp.transform.localRotation = Quaternion.Euler(3f, 0, 0);

            // === TƯỜNG TRƯỚC (CỬA RỘNG 2.0M, CAO 2.6M - NGƯỜI CHƠI ĐI QUA THOẢI MÁI) ===
            float doorWidth = 2.0f;
            float doorHeight = 2.6f;
            float sideWallWidth = (wallWidth - doorWidth) / 2f; // 1.2m

            // Mảng tường trái cửa
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(-1.6f, wallHeight / 2f, wallWidth / 2f),
                new Vector3(sideWallWidth, wallHeight, wallThick), woodMat, "FrontWall_L", true);

            // Mảng tường phải cửa
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(1.6f, wallHeight / 2f, wallWidth / 2f),
                new Vector3(sideWallWidth, wallHeight, wallThick), woodMat, "FrontWall_R", true);

            // Xà ngang trên cửa (đáy xà ở cao độ 2.6m, cách đỉnh đầu người chơi tới 60cm!)
            float topWallHeight = wallHeight - doorHeight; // 0.6m
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(0, doorHeight + topWallHeight / 2f, wallWidth / 2f),
                new Vector3(doorWidth, topWallHeight, wallThick), woodMat, "FrontWall_Top", true);

            // === TƯỜNG SAU ===
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(0, wallHeight / 2f, -wallWidth / 2f),
                new Vector3(wallWidth, wallHeight, wallThick), woodMat, "BackWall", true);

            // === TƯỜNG TRÁI ===
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(-wallWidth / 2f, wallHeight / 2f, 0),
                new Vector3(wallThick, wallHeight, wallWidth), woodMat, "LeftWall", true);

            // === TƯỜNG PHẢI (CÓ CỬA SỔ NHÌN RA NGOÀI) ===
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(wallWidth / 2f, 0.5f, 0),
                new Vector3(wallThick, 1.0f, wallWidth), woodMat, "RightWall_Bottom", true);

            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(wallWidth / 2f, wallHeight - 0.3f, 0),
                new Vector3(wallThick, 0.6f, wallWidth), woodMat, "RightWall_Top", true);

            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(wallWidth / 2f, 1.8f, -1.4f),
                new Vector3(wallThick, 1.6f, 1.6f), woodMat, "RightWall_BackPart", true);

            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(wallWidth / 2f, 1.8f, 1.4f),
                new Vector3(wallThick, 1.6f, 1.6f), woodMat, "RightWall_FrontPart", true);

            // === MÁI NHÀ CHỮ A ===
            GameObject roofL = CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(-1.35f, wallHeight + 0.7f, 0),
                new Vector3(3.1f, 0.12f, wallWidth + 0.4f), roofMat, "Roof_L", true);
            roofL.transform.localRotation = Quaternion.Euler(0, 0, 24f);

            GameObject roofR = CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(1.35f, wallHeight + 0.7f, 0),
                new Vector3(3.1f, 0.12f, wallWidth + 0.4f), roofMat, "Roof_R", true);
            roofR.transform.localRotation = Quaternion.Euler(0, 0, -24f);

            // === GIƯỜNG NGỦ NGHỈ NGƠI TRONG GÓC NHÀ ===
            GameObject bed = CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(-1.3f, 0.25f, -1.3f),
                new Vector3(1.4f, 0.35f, 1.8f), woodMat, "Bed_Wood", true);

            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(-1.3f, 0.45f, -1.3f),
                new Vector3(1.2f, 0.15f, 1.6f), GetMaterial("NAT_CobbleRocks1", new Color(0.85f, 0.8f, 0.7f)), "Mattress", true);

            // Gắn ShelterRest lên giường ngủ
            ShelterRest rest = bed.AddComponent<ShelterRest>();
            rest.shelterName = "Nhà Gỗ";
            rest.healAmount = 25f;

            return house;
        }

        // ═════════════════════════════════════════════════════
        // 2. NHÀ ĐÁ KIÊN CỐ (STONE HOUSE)
        // ═════════════════════════════════════════════════════
        public static GameObject CreateStoneHouse()
        {
            GameObject house = new GameObject("StoneHouse");

            Material stoneMat = GetMaterial("NAT_CobbleRocks1", new Color(0.55f, 0.55f, 0.52f));
            Material stoneDark = GetMaterial("NAT_Rocks1", new Color(0.38f, 0.38f, 0.36f));
            Material roofMat = GetMaterial("BLD_Cabins_Mossy", new Color(0.42f, 0.28f, 0.12f));

            float wallHeight = 3.4f; // Cao 3.4m kiên cố và rộng rãi
            float wallWidth = 4.8f;  // Rộng 4.8m
            float wallThick = 0.25f;

            // === SÀN ĐÁ ===
            CreatePrimitive(house, PrimitiveType.Cube, new Vector3(0, 0.05f, 0),
                new Vector3(wallWidth, 0.10f, wallWidth), stoneDark, "Floor", true);

            // Dốc ngưỡng cửa đá
            GameObject doorRamp = CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(0, 0.02f, wallWidth / 2f + 0.35f),
                new Vector3(2.0f, 0.04f, 0.7f), stoneDark, "DoorRamp", true);
            doorRamp.transform.localRotation = Quaternion.Euler(3f, 0, 0);

            // === TƯỜNG TRƯỚC (CÓ CỬA VÀO RỘNG 2.0M, CAO 2.7M) ===
            float doorWidth = 2.0f;
            float doorHeight = 2.7f;
            float sideWallWidth = (wallWidth - doorWidth) / 2f; // 1.4m

            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(-1.7f, wallHeight / 2f, wallWidth / 2f),
                new Vector3(sideWallWidth, wallHeight, wallThick), stoneMat, "FrontWall_L", true);

            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(1.7f, wallHeight / 2f, wallWidth / 2f),
                new Vector3(sideWallWidth, wallHeight, wallThick), stoneMat, "FrontWall_R", true);

            float topWallHeight = wallHeight - doorHeight; // 0.7m
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(0, doorHeight + topWallHeight / 2f, wallWidth / 2f),
                new Vector3(doorWidth, topWallHeight, wallThick), stoneMat, "FrontWall_Top", true);

            // === 3 TƯỜNG ĐÁ BẢO VỆ ===
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(0, wallHeight / 2f, -wallWidth / 2f),
                new Vector3(wallWidth, wallHeight, wallThick), stoneMat, "BackWall", true);

            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(-wallWidth / 2f, wallHeight / 2f, 0),
                new Vector3(wallThick, wallHeight, wallWidth), stoneMat, "LeftWall", true);

            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(wallWidth / 2f, wallHeight / 2f, 0),
                new Vector3(wallThick, wallHeight, wallWidth), stoneMat, "RightWall", true);

            // === MÁI GỖ DỐC ===
            GameObject roofL = CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(-1.45f, wallHeight + 0.65f, 0),
                new Vector3(3.3f, 0.14f, wallWidth + 0.4f), roofMat, "Roof_L", true);
            roofL.transform.localRotation = Quaternion.Euler(0, 0, 18f);

            GameObject roofR = CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(1.45f, wallHeight + 0.65f, 0),
                new Vector3(3.3f, 0.14f, wallWidth + 0.4f), roofMat, "Roof_R", true);
            roofR.transform.localRotation = Quaternion.Euler(0, 0, -18f);

            // Bệ nghỉ ngơi bằng đá bên trong
            GameObject restBench = CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(-1.4f, 0.3f, -1.4f),
                new Vector3(1.6f, 0.45f, 1.8f), stoneDark, "StoneBed", true);

            ShelterRest rest = restBench.AddComponent<ShelterRest>();
            rest.shelterName = "Nhà Đá Kiên Cố";
            rest.healAmount = 35f;

            return house;
        }

        // ═════════════════════════════════════════════════════
        // 3. LỀU LÁ SINH TỒN (LEAF SHELTER - THE FOREST)
        // ═════════════════════════════════════════════════════
        public static GameObject CreateLeafShelter()
        {
            GameObject shelter = new GameObject("LeafShelter");

            Material woodMat = GetMaterial("BLD_Cabins", new Color(0.48f, 0.32f, 0.14f));
            Color leafColor = new Color(0.20f, 0.52f, 0.14f);
            Color leafDark = new Color(0.14f, 0.40f, 0.08f);

            float length = 3.4f;
            float height = 2.5f;

            // Cọc nóc chính
            GameObject ridge = CreatePrimitive(shelter, PrimitiveType.Cylinder,
                new Vector3(0, height, 0),
                new Vector3(0.08f, length / 2f, 0.08f), woodMat, "Ridge", true);
            ridge.transform.localRotation = Quaternion.Euler(90, 0, 0);

            // Khung cọc trước (TẮT collider để người chơi đi thẳng vào lều không bị kẹt)
            GameObject poleFL = CreatePrimitive(shelter, PrimitiveType.Cylinder,
                new Vector3(-1.05f, height / 2f, length / 2f),
                new Vector3(0.06f, height / 1.3f, 0.06f), woodMat, "FrontPole_L", false);
            poleFL.transform.localRotation = Quaternion.Euler(0, 0, 32f);

            GameObject poleFR = CreatePrimitive(shelter, PrimitiveType.Cylinder,
                new Vector3(1.05f, height / 2f, length / 2f),
                new Vector3(0.06f, height / 1.3f, 0.06f), woodMat, "FrontPole_R", false);
            poleFR.transform.localRotation = Quaternion.Euler(0, 0, -32f);

            // Khung cọc sau
            GameObject poleBL = CreatePrimitive(shelter, PrimitiveType.Cylinder,
                new Vector3(-1.05f, height / 2f, -length / 2f),
                new Vector3(0.06f, height / 1.3f, 0.06f), woodMat, "BackPole_L", true);
            poleBL.transform.localRotation = Quaternion.Euler(0, 0, 32f);

            GameObject poleBR = CreatePrimitive(shelter, PrimitiveType.Cylinder,
                new Vector3(1.05f, height / 2f, -length / 2f),
                new Vector3(0.06f, height / 1.3f, 0.06f), woodMat, "BackPole_R", true);
            poleBR.transform.localRotation = Quaternion.Euler(0, 0, -32f);

            // Mái lá trái dốc
            GameObject leafL = CreatePrimitive(shelter, PrimitiveType.Cube,
                new Vector3(-1.1f, height * 0.55f, 0),
                new Vector3(2.4f, 0.08f, length + 0.2f), null, "LeafRoof_L", true);
            SetColor(leafL, leafColor);
            leafL.transform.localRotation = Quaternion.Euler(0, 0, 42f);

            // Mái lá phải dốc
            GameObject leafR = CreatePrimitive(shelter, PrimitiveType.Cube,
                new Vector3(1.1f, height * 0.55f, 0),
                new Vector3(2.4f, 0.08f, length + 0.2f), null, "LeafRoof_R", true);
            SetColor(leafR, leafColor);
            leafR.transform.localRotation = Quaternion.Euler(0, 0, -42f);

            // Tường lá phía sau che gió
            GameObject backWall = CreatePrimitive(shelter, PrimitiveType.Cube,
                new Vector3(0, height * 0.45f, -length / 2f),
                new Vector3(2.1f, height * 0.9f, 0.08f), null, "BackLeafWall", true);
            SetColor(backWall, leafDark);

            // Đệm lá nằm ngủ bên trong
            GameObject leafBed = CreatePrimitive(shelter, PrimitiveType.Cube,
                new Vector3(0, 0.1f, -0.2f),
                new Vector3(1.3f, 0.15f, 1.9f), null, "LeafBed", true);
            SetColor(leafBed, new Color(0.35f, 0.65f, 0.22f));

            ShelterRest rest = leafBed.AddComponent<ShelterRest>();
            rest.shelterName = "Lều Lá";
            rest.healAmount = 20f;

            return shelter;
        }

        // ═════════════════════════════════════════════════════
        // 4. HÀNG RÀO PHÒNG THỦ (WOOD FENCE)
        // ═════════════════════════════════════════════════════
        public static GameObject CreateWoodFence()
        {
            GameObject fence = new GameObject("WoodFence");
            Material woodMat = GetMaterial("BLD_Cabins", new Color(0.48f, 0.32f, 0.14f));

            float fenceHeight = 1.6f;
            float fenceWidth = 3.2f;

            // 3 cọc đứng
            for (int i = 0; i < 3; i++)
            {
                float x = -fenceWidth / 2f + (fenceWidth / 2f) * i;
                CreatePrimitive(fence, PrimitiveType.Cylinder,
                    new Vector3(x, fenceHeight / 2f, 0),
                    new Vector3(0.1f, fenceHeight / 2f, 0.1f), woodMat, "Post_" + i, true);
            }

            // 2 thanh giằng ngang
            CreatePrimitive(fence, PrimitiveType.Cube,
                new Vector3(0, fenceHeight * 0.72f, 0),
                new Vector3(fenceWidth, 0.09f, 0.08f), woodMat, "Rail_Top", true);

            CreatePrimitive(fence, PrimitiveType.Cube,
                new Vector3(0, fenceHeight * 0.28f, 0),
                new Vector3(fenceWidth, 0.09f, 0.08f), woodMat, "Rail_Bottom", true);

            // Collider cản đường người chơi & zombie
            BoxCollider col = fence.AddComponent<BoxCollider>();
            col.center = new Vector3(0, fenceHeight / 2f, 0);
            col.size = new Vector3(fenceWidth, fenceHeight, 0.35f);

            return fence;
        }

        // ═════════════════════════════════════════════════════
        // 5. LỬA TRẠI NẤU ĂN (CAMPFIRE + COOKING STATION)
        // ═════════════════════════════════════════════════════
        public static GameObject CreateCampfire()
        {
            GameObject campfire = new GameObject("Campfire");

            Material stoneMat = GetMaterial("NAT_CobbleRocks1", new Color(0.45f, 0.45f, 0.42f));
            Material woodMat = GetMaterial("BLD_Cabins", new Color(0.40f, 0.25f, 0.10f));

            // Vòng 8 viên đá
            int stoneCount = 8;
            float ringRadius = 0.55f;
            for (int i = 0; i < stoneCount; i++)
            {
                float angle = (360f / stoneCount) * i * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * ringRadius;
                float z = Mathf.Sin(angle) * ringRadius;

                GameObject stone = CreatePrimitive(campfire, PrimitiveType.Sphere,
                    new Vector3(x, 0.10f, z),
                    new Vector3(0.24f, 0.18f, 0.24f), stoneMat, "Stone_" + i, false);
                stone.transform.localRotation = Random.rotation;
            }

            // Tro than đen
            GameObject ash = CreatePrimitive(campfire, PrimitiveType.Cylinder,
                new Vector3(0, 0.03f, 0),
                new Vector3(0.85f, 0.03f, 0.85f), null, "AshBase", false);
            SetColor(ash, new Color(0.18f, 0.16f, 0.15f));

            // 3 khúc củi chéo
            GameObject log1 = CreatePrimitive(campfire, PrimitiveType.Cylinder,
                new Vector3(0, 0.16f, 0), new Vector3(0.08f, 0.42f, 0.08f), woodMat, "Log1", false);
            log1.transform.localRotation = Quaternion.Euler(55, 0, 0);

            GameObject log2 = CreatePrimitive(campfire, PrimitiveType.Cylinder,
                new Vector3(0, 0.16f, 0), new Vector3(0.08f, 0.42f, 0.08f), woodMat, "Log2", false);
            log2.transform.localRotation = Quaternion.Euler(55, 90, 0);

            GameObject log3 = CreatePrimitive(campfire, PrimitiveType.Cylinder,
                new Vector3(0, 0.16f, 0), new Vector3(0.07f, 0.38f, 0.07f), woodMat, "Log3", false);
            log3.transform.localRotation = Quaternion.Euler(55, 45, 0);

            // Ánh lửa bập bùng
            GameObject lightObj = new GameObject("FireLight");
            lightObj.transform.SetParent(campfire.transform);
            lightObj.transform.localPosition = new Vector3(0, 0.55f, 0);
            Light fireLight = lightObj.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.65f, 0.2f);
            fireLight.range = 12f;
            fireLight.intensity = 2.2f;

            // BoxCollider tương tác
            BoxCollider col = campfire.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.3f, 0);
            col.size = new Vector3(1.3f, 0.6f, 1.3f);

            // Gắn CookingStation để nướng thịt sống thành thịt chín!
            CookingStation cooker = campfire.AddComponent<CookingStation>();
            cooker.cookTime = 12f;
            cooker.burnTime = 28f;
            cooker.fireLight = fireLight;

            return campfire;
        }

        // ═════════════════════════════════════════════════════
        // 6. RƯƠNG ĐỒ CHỨA VẬT PHẨM (STORAGE CHEST)
        // ═════════════════════════════════════════════════════
        public static GameObject CreateWoodChest()
        {
            GameObject chestObj = new GameObject("WoodChest");
            Material woodMat = GetMaterial("BLD_Cabins", new Color(0.48f, 0.32f, 0.14f));
            Material metalMat = GetMaterial("NAT_Rocks1", new Color(0.25f, 0.25f, 0.28f));

            // Thùng gỗ chính
            CreatePrimitive(chestObj, PrimitiveType.Cube,
                new Vector3(0, 0.32f, 0),
                new Vector3(1.1f, 0.62f, 0.75f), woodMat, "ChestBody", true);

            // Nắp thùng
            CreatePrimitive(chestObj, PrimitiveType.Cube,
                new Vector3(0, 0.66f, 0),
                new Vector3(1.14f, 0.10f, 0.79f), woodMat, "ChestLid", true);

            // Khóa sắt
            CreatePrimitive(chestObj, PrimitiveType.Cube,
                new Vector3(0, 0.50f, 0.39f),
                new Vector3(0.12f, 0.16f, 0.05f), metalMat, "MetalLock", false);

            BoxCollider col = chestObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.35f, 0);
            col.size = new Vector3(1.15f, 0.72f, 0.8f);

            // Gắn LootChest làm rương chứa đồ
            LootChest chest = chestObj.AddComponent<LootChest>();
            chest.promptText = "[E] Mở Rương Chứa Đồ";
            chest.gunAmount = 0;
            chest.ammoAmount = 0;

            return chestObj;
        }

        // ═════════════════════════════════════════════════════
        // HELPER FUNCTIONS
        // ═════════════════════════════════════════════════════
        private static GameObject CreatePrimitive(GameObject parent, PrimitiveType type,
            Vector3 localPos, Vector3 localScale, Material mat, string name, bool enableCollider = true)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent.transform, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;

            Renderer r = obj.GetComponent<Renderer>();
            if (r != null && mat != null)
            {
                r.sharedMaterial = mat;
            }

            Collider col = obj.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = enableCollider;
            }

            return obj;
        }

        private static void SetColor(GameObject obj, Color color)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r != null)
            {
                Shader s = Shader.Find("Standard");
                if (s == null) s = Shader.Find("Diffuse");
                Material m = new Material(s);
                m.color = color;
                r.material = m;
            }
        }

        private static Material GetMaterial(string matName, Color fallbackColor)
        {
            Material mat = null;
#if UNITY_EDITOR
            mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Flooded_Grounds/Content/Materials/" + matName + ".mat");
#endif
            if (mat == null)
            {
                Shader s = Shader.Find("Standard");
                if (s == null) s = Shader.Find("Mobile/Diffuse");
                if (s == null) s = Shader.Find("Diffuse");
                mat = new Material(s);
                mat.color = fallbackColor;
            }
            return mat;
        }
    }
}

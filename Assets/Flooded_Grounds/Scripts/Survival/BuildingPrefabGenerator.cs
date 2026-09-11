using UnityEngine;

namespace HorrorGame.Survival
{
    /// <summary>
    /// Tạo các công trình bằng Primitive shapes (Cube, Cylinder, Sphere).
    /// Khi có model 3D thật, chỉ cần thay prefab trong BuildingRecipe.
    /// </summary>
    public static class BuildingPrefabGenerator
    {
        // =====================================================
        // NHÀ GỖ (4 tường gỗ + sàn + mái dốc)
        // =====================================================
        public static GameObject CreateWoodHouse()
        {
            GameObject house = new GameObject("WoodHouse");

            Color woodColor = new Color(0.55f, 0.35f, 0.15f);       // Nâu gỗ
            Color woodDark = new Color(0.4f, 0.25f, 0.1f);          // Nâu đậm
            Color roofColor = new Color(0.35f, 0.22f, 0.08f);       // Nâu mái

            float wallHeight = 2.5f;
            float wallWidth = 4f;
            float wallThick = 0.15f;

            // === SÀN ===
            CreatePrimitive(house, PrimitiveType.Cube, Vector3.zero,
                new Vector3(wallWidth, 0.1f, wallWidth), woodDark, "Floor");

            // === 4 TƯỜNG ===
            // Tường trước (có lỗ cửa)
            // Phần trái cửa
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(-1.3f, wallHeight / 2f, wallWidth / 2f),
                new Vector3(1.4f, wallHeight, wallThick), woodColor, "FrontWall_L");
            // Phần phải cửa
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(1.3f, wallHeight / 2f, wallWidth / 2f),
                new Vector3(1.4f, wallHeight, wallThick), woodColor, "FrontWall_R");
            // Phần trên cửa
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(0, wallHeight - 0.3f, wallWidth / 2f),
                new Vector3(1.2f, 0.6f, wallThick), woodColor, "FrontWall_Top");

            // Tường sau
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(0, wallHeight / 2f, -wallWidth / 2f),
                new Vector3(wallWidth, wallHeight, wallThick), woodColor, "BackWall");

            // Tường trái
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(-wallWidth / 2f, wallHeight / 2f, 0),
                new Vector3(wallThick, wallHeight, wallWidth), woodColor, "LeftWall");

            // Tường phải (có cửa sổ)
            // Phần dưới
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(wallWidth / 2f, 0.5f, 0),
                new Vector3(wallThick, 1f, wallWidth), woodColor, "RightWall_Bottom");
            // Phần trên
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(wallWidth / 2f, wallHeight - 0.3f, 0),
                new Vector3(wallThick, 1.1f, wallWidth), woodColor, "RightWall_Top");
            // Phần trái cửa sổ
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(wallWidth / 2f, 1.3f, -1.3f),
                new Vector3(wallThick, 1.1f, 1.4f), woodColor, "RightWall_WL");
            // Phần phải cửa sổ
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(wallWidth / 2f, 1.3f, 1.3f),
                new Vector3(wallThick, 1.1f, 1.4f), woodColor, "RightWall_WR");

            // === MÁI NHÀ (2 mặt dốc chữ A) ===
            // Mái bên trái
            GameObject roofL = CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(-1.2f, wallHeight + 0.6f, 0),
                new Vector3(2.8f, 0.1f, wallWidth + 0.4f), roofColor, "Roof_L");
            roofL.transform.localRotation = Quaternion.Euler(0, 0, 25f);

            // Mái bên phải
            GameObject roofR = CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(1.2f, wallHeight + 0.6f, 0),
                new Vector3(2.8f, 0.1f, wallWidth + 0.4f), roofColor, "Roof_R");
            roofR.transform.localRotation = Quaternion.Euler(0, 0, -25f);

            // === THANH NÓC MÁI ===
            CreatePrimitive(house, PrimitiveType.Cylinder,
                new Vector3(0, wallHeight + 1.2f, 0),
                new Vector3(0.08f, (wallWidth + 0.4f) / 2f, 0.08f), woodDark, "RoofRidge");
            house.transform.Find("RoofRidge").localRotation = Quaternion.Euler(90, 0, 0);

            // Thêm collider bao ngoài
            BoxCollider mainCol = house.AddComponent<BoxCollider>();
            mainCol.center = new Vector3(0, wallHeight / 2f + 0.5f, 0);
            mainCol.size = new Vector3(wallWidth + 0.2f, wallHeight + 1.5f, wallWidth + 0.2f);

            return house;
        }

        // =====================================================
        // NHÀ ĐÁ (4 tường đá xám + mái gỗ)
        // =====================================================
        public static GameObject CreateStoneHouse()
        {
            GameObject house = new GameObject("StoneHouse");

            Color stoneColor = new Color(0.55f, 0.55f, 0.52f);     // Xám đá
            Color stoneDark = new Color(0.4f, 0.4f, 0.38f);        // Xám đậm
            Color woodColor = new Color(0.45f, 0.3f, 0.12f);       // Nâu gỗ mái

            float wallHeight = 2.8f;
            float wallWidth = 4.5f;
            float wallThick = 0.25f;

            // === SÀN ĐÁ ===
            CreatePrimitive(house, PrimitiveType.Cube, Vector3.zero,
                new Vector3(wallWidth, 0.15f, wallWidth), stoneDark, "Floor");

            // === 4 TƯỜNG ĐÁ ===
            // Tường trước (lỗ cửa)
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(-1.5f, wallHeight / 2f, wallWidth / 2f),
                new Vector3(1.5f, wallHeight, wallThick), stoneColor, "FrontWall_L");
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(1.5f, wallHeight / 2f, wallWidth / 2f),
                new Vector3(1.5f, wallHeight, wallThick), stoneColor, "FrontWall_R");
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(0, wallHeight - 0.3f, wallWidth / 2f),
                new Vector3(1.5f, 0.6f, wallThick), stoneColor, "FrontWall_Top");

            // Tường sau
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(0, wallHeight / 2f, -wallWidth / 2f),
                new Vector3(wallWidth, wallHeight, wallThick), stoneColor, "BackWall");

            // Tường trái
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(-wallWidth / 2f, wallHeight / 2f, 0),
                new Vector3(wallThick, wallHeight, wallWidth), stoneColor, "LeftWall");

            // Tường phải
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(wallWidth / 2f, wallHeight / 2f, 0),
                new Vector3(wallThick, wallHeight, wallWidth), stoneColor, "RightWall");

            // === MÁI GỖ (phẳng hơi dốc) ===
            GameObject roofL = CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(-1.3f, wallHeight + 0.5f, 0),
                new Vector3(3f, 0.12f, wallWidth + 0.5f), woodColor, "Roof_L");
            roofL.transform.localRotation = Quaternion.Euler(0, 0, 20f);

            GameObject roofR = CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(1.3f, wallHeight + 0.5f, 0),
                new Vector3(3f, 0.12f, wallWidth + 0.5f), woodColor, "Roof_R");
            roofR.transform.localRotation = Quaternion.Euler(0, 0, -20f);

            // Viền đá trang trí quanh chân tường
            CreatePrimitive(house, PrimitiveType.Cube,
                new Vector3(0, 0.15f, 0),
                new Vector3(wallWidth + 0.3f, 0.3f, wallWidth + 0.3f), stoneDark, "Foundation");

            BoxCollider mainCol = house.AddComponent<BoxCollider>();
            mainCol.center = new Vector3(0, wallHeight / 2f + 0.5f, 0);
            mainCol.size = new Vector3(wallWidth + 0.3f, wallHeight + 1.5f, wallWidth + 0.3f);

            return house;
        }

        // =====================================================
        // LỀU LÁ KIỂU THE FOREST (khung chữ A + lá phủ)
        // =====================================================
        public static GameObject CreateLeafShelter()
        {
            GameObject shelter = new GameObject("LeafShelter");

            Color woodColor = new Color(0.5f, 0.32f, 0.13f);    // Cọc gỗ
            Color leafColor = new Color(0.2f, 0.5f, 0.12f);     // Xanh lá
            Color leafDark = new Color(0.15f, 0.4f, 0.08f);     // Xanh lá đậm

            float length = 3f;
            float height = 2f;

            // === KHUNG GỖ CHỮ A ===
            // Thanh nóc chính (ngang dọc theo chiều dài)
            CreatePrimitive(shelter, PrimitiveType.Cylinder,
                new Vector3(0, height, 0),
                new Vector3(0.06f, length / 2f, 0.06f), woodColor, "Ridge");
            shelter.transform.Find("Ridge").localRotation = Quaternion.Euler(90, 0, 0);

            // Cọc chữ A - phía trước (2 cọc chéo)
            GameObject frontL = CreatePrimitive(shelter, PrimitiveType.Cylinder,
                new Vector3(-0.6f, height / 2f, length / 2f),
                new Vector3(0.05f, height / 1.4f, 0.05f), woodColor, "FrontPole_L");
            frontL.transform.localRotation = Quaternion.Euler(0, 0, 25f);

            GameObject frontR = CreatePrimitive(shelter, PrimitiveType.Cylinder,
                new Vector3(0.6f, height / 2f, length / 2f),
                new Vector3(0.05f, height / 1.4f, 0.05f), woodColor, "FrontPole_R");
            frontR.transform.localRotation = Quaternion.Euler(0, 0, -25f);

            // Cọc chữ A - phía sau (2 cọc chéo)
            GameObject backL = CreatePrimitive(shelter, PrimitiveType.Cylinder,
                new Vector3(-0.6f, height / 2f, -length / 2f),
                new Vector3(0.05f, height / 1.4f, 0.05f), woodColor, "BackPole_L");
            backL.transform.localRotation = Quaternion.Euler(0, 0, 25f);

            GameObject backR = CreatePrimitive(shelter, PrimitiveType.Cylinder,
                new Vector3(0.6f, height / 2f, -length / 2f),
                new Vector3(0.05f, height / 1.4f, 0.05f), woodColor, "BackPole_R");
            backR.transform.localRotation = Quaternion.Euler(0, 0, -25f);

            // Thanh ngang phụ (giữ lá)
            CreatePrimitive(shelter, PrimitiveType.Cylinder,
                new Vector3(-0.8f, height * 0.4f, 0),
                new Vector3(0.04f, length / 2f, 0.04f), woodColor, "SideBar_L");
            shelter.transform.Find("SideBar_L").localRotation = Quaternion.Euler(90, 0, 0);

            CreatePrimitive(shelter, PrimitiveType.Cylinder,
                new Vector3(0.8f, height * 0.4f, 0),
                new Vector3(0.04f, length / 2f, 0.04f), woodColor, "SideBar_R");
            shelter.transform.Find("SideBar_R").localRotation = Quaternion.Euler(90, 0, 0);

            // === LỚP LÁ PHỦ ===
            // Mái lá bên trái (dốc)
            GameObject leafL = CreatePrimitive(shelter, PrimitiveType.Cube,
                new Vector3(-0.75f, height * 0.65f, 0),
                new Vector3(1.8f, 0.08f, length + 0.2f), leafColor, "LeafRoof_L");
            leafL.transform.localRotation = Quaternion.Euler(0, 0, 40f);

            // Mái lá bên phải (dốc)
            GameObject leafR = CreatePrimitive(shelter, PrimitiveType.Cube,
                new Vector3(0.75f, height * 0.65f, 0),
                new Vector3(1.8f, 0.08f, length + 0.2f), leafColor, "LeafRoof_R");
            leafR.transform.localRotation = Quaternion.Euler(0, 0, -40f);

            // Lớp lá thêm (tạo độ dày, xịn hơn)
            GameObject leafL2 = CreatePrimitive(shelter, PrimitiveType.Cube,
                new Vector3(-0.7f, height * 0.6f, 0),
                new Vector3(1.6f, 0.06f, length - 0.2f), leafDark, "LeafLayer2_L");
            leafL2.transform.localRotation = Quaternion.Euler(0, 0, 40f);

            GameObject leafR2 = CreatePrimitive(shelter, PrimitiveType.Cube,
                new Vector3(0.7f, height * 0.6f, 0),
                new Vector3(1.6f, 0.06f, length - 0.2f), leafDark, "LeafLayer2_R");
            leafR2.transform.localRotation = Quaternion.Euler(0, 0, -40f);

            // Tường sau bằng lá (chắn gió)
            CreatePrimitive(shelter, PrimitiveType.Cube,
                new Vector3(0, height * 0.45f, -length / 2f - 0.05f),
                new Vector3(1.6f, height * 0.9f, 0.06f), leafDark, "BackLeafWall");

            BoxCollider mainCol = shelter.AddComponent<BoxCollider>();
            mainCol.center = new Vector3(0, height / 2f, 0);
            mainCol.size = new Vector3(2f, height, length);

            return shelter;
        }

        // =====================================================
        // HÀNG RÀO GỖ (3 cọc + 2 thanh ngang)
        // =====================================================
        public static GameObject CreateWoodFence()
        {
            GameObject fence = new GameObject("WoodFence");

            Color woodColor = new Color(0.5f, 0.33f, 0.14f);
            Color woodDark = new Color(0.38f, 0.24f, 0.1f);

            float fenceHeight = 1.5f;
            float fenceWidth = 3f;

            // 3 cọc đứng
            for (int i = 0; i < 3; i++)
            {
                float xPos = -fenceWidth / 2f + (fenceWidth / 2f) * i;
                CreatePrimitive(fence, PrimitiveType.Cylinder,
                    new Vector3(xPos, fenceHeight / 2f, 0),
                    new Vector3(0.08f, fenceHeight / 2f, 0.08f), woodColor, "Post_" + i);

                // Đầu nhọn cọc
                CreatePrimitive(fence, PrimitiveType.Cube,
                    new Vector3(xPos, fenceHeight + 0.05f, 0),
                    new Vector3(0.1f, 0.1f, 0.1f), woodDark, "PostTop_" + i);
            }

            // 2 thanh ngang
            CreatePrimitive(fence, PrimitiveType.Cube,
                new Vector3(0, fenceHeight * 0.7f, 0),
                new Vector3(fenceWidth, 0.08f, 0.06f), woodDark, "Rail_Top");

            CreatePrimitive(fence, PrimitiveType.Cube,
                new Vector3(0, fenceHeight * 0.3f, 0),
                new Vector3(fenceWidth, 0.08f, 0.06f), woodDark, "Rail_Bottom");

            BoxCollider col = fence.AddComponent<BoxCollider>();
            col.center = new Vector3(0, fenceHeight / 2f, 0);
            col.size = new Vector3(fenceWidth, fenceHeight, 0.3f);

            return fence;
        }

        // =====================================================
        // LỬA TRẠI (vòng đá + thanh gỗ chéo + ánh lửa)
        // =====================================================
        public static GameObject CreateCampfire()
        {
            GameObject campfire = new GameObject("Campfire");

            Color stoneColor = new Color(0.45f, 0.45f, 0.42f);
            Color woodColor = new Color(0.45f, 0.28f, 0.1f);
            Color ashColor = new Color(0.25f, 0.22f, 0.2f);

            // Vòng đá (8 viên đá xếp vòng tròn)
            int stoneCount = 8;
            float ringRadius = 0.5f;
            for (int i = 0; i < stoneCount; i++)
            {
                float angle = (360f / stoneCount) * i * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * ringRadius;
                float z = Mathf.Sin(angle) * ringRadius;

                GameObject stone = CreatePrimitive(campfire, PrimitiveType.Sphere,
                    new Vector3(x, 0.08f, z),
                    new Vector3(0.2f, 0.15f, 0.2f), stoneColor, "Stone_" + i);
                stone.transform.localRotation = Random.rotation;
            }

            // Nền tro
            CreatePrimitive(campfire, PrimitiveType.Cylinder,
                new Vector3(0, 0.02f, 0),
                new Vector3(0.8f, 0.02f, 0.8f), ashColor, "AshBase");

            // Thanh gỗ chéo (kiểu chữ X)
            GameObject log1 = CreatePrimitive(campfire, PrimitiveType.Cylinder,
                new Vector3(0, 0.15f, 0),
                new Vector3(0.06f, 0.4f, 0.06f), woodColor, "Log1");
            log1.transform.localRotation = Quaternion.Euler(60, 0, 0);

            GameObject log2 = CreatePrimitive(campfire, PrimitiveType.Cylinder,
                new Vector3(0, 0.15f, 0),
                new Vector3(0.06f, 0.4f, 0.06f), woodColor, "Log2");
            log2.transform.localRotation = Quaternion.Euler(60, 90, 0);

            GameObject log3 = CreatePrimitive(campfire, PrimitiveType.Cylinder,
                new Vector3(0, 0.15f, 0),
                new Vector3(0.05f, 0.35f, 0.05f), woodColor, "Log3");
            log3.transform.localRotation = Quaternion.Euler(60, 45, 0);

            // Ánh lửa (Point Light)
            GameObject lightObj = new GameObject("FireLight");
            lightObj.transform.SetParent(campfire.transform);
            lightObj.transform.localPosition = new Vector3(0, 0.5f, 0);
            Light fireLight = lightObj.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.6f, 0.15f);
            fireLight.range = 10f;
            fireLight.intensity = 2f;

            BoxCollider col = campfire.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.2f, 0);
            col.size = new Vector3(1.2f, 0.5f, 1.2f);

            return campfire;
        }

        // =====================================================
        // HELPER: Tạo Primitive có vị trí, scale, màu sắc
        // =====================================================
        private static GameObject CreatePrimitive(GameObject parent, PrimitiveType type,
            Vector3 localPos, Vector3 localScale, Color color, string name)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent.transform);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;

            // Đặt màu sắc
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = color;
            }

            // Tắt Collider con (để không va chạm nội bộ)
            Collider col = obj.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            return obj;
        }
    }
}

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using HorrorGame.Inventory;

namespace HorrorGame.EditorTools
{
    public class IslandLootSpawner : EditorWindow
    {
        private const string ITEMS_DIR = "Assets/Flooded_Grounds/Resources/Items";
        private const string ICONS_DIR = "Assets/Flooded_Grounds/Resources/Items/Icons";
        private const string PREFABS_DIR = "Assets/Flooded_Grounds/Prefabs/Pickups";
        private const string MATERIALS_DIR = "Assets/Flooded_Grounds/Materials/Loot";

        [MenuItem("Horror Game/📦 Rải Vật Phẩm Khắp Đảo (Spawn Island Loot)")]
        public static void GenerateAndSpawnIslandLoot()
        {
            EnsureDirectories();

            // 1. Tạo hoặc tải 5 ItemData ScriptableObjects
            ItemData medkitData     = GetOrCreateItemData("medkit_01", "Băng cứu thương", "Hộp sơ cứu chứa băng gạc và thuốc sát trùng. Hồi phục +35 Máu.", ItemType.Consumable, true, 5, "Icon_Medkit.png", new Color(0.9f, 0.2f, 0.2f));
            ItemData ammoData       = GetOrCreateItemData("ammo_box_01", "Hộp đạn quân dụng", "Hộp đạn chứa 30 viên đạn tiêu chuẩn súng trường.", ItemType.Ammunition, true, 4, "Icon_Ammo.png", new Color(0.85f, 0.7f, 0.1f));
            ItemData flashlightData = GetOrCreateItemData("flashlight_01", "Đèn pin chiến thuật", "Đèn pin LED quân sự chiếu xa. Bấm phím [F] để bật/tắt soi đường ban đêm.", ItemType.KeyItem, false, 1, "Icon_Flashlight.png", new Color(0.3f, 0.75f, 1f));
            ItemData grenadeData    = GetOrCreateItemData("grenade_m67", "Lựu đạn nổ M67", "Lựu đạn nổ tầm xa sát thương diện rộng.", ItemType.Weapon, true, 3, "Icon_Grenade.png", new Color(0.2f, 0.5f, 0.2f));
            ItemData foodData       = GetOrCreateItemData("food_ration_01", "Hộp lương thực dự trữ", "Hộp thịt bò hầm giàu dinh dưỡng. Hồi phục +40 Đói và +25 Thể lực.", ItemType.Consumable, true, 6, "Icon_Food.png", new Color(0.85f, 0.45f, 0.15f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 2. Tạo 5 Prefab 3D Pickup đại diện trực quan
            GameObject medkitPrefab     = GetOrCreatePickupPrefab("Pickup_Medkit", medkitData, 1, CreateMedkitVisual());
            GameObject ammoPrefab       = GetOrCreatePickupPrefab("Pickup_AmmoBox", ammoData, 30, CreateAmmoBoxVisual());
            GameObject flashlightPrefab = GetOrCreatePickupPrefab("Pickup_Flashlight", flashlightData, 1, CreateFlashlightVisual());
            GameObject grenadePrefab    = GetOrCreatePickupPrefab("Pickup_Grenade", grenadeData, 1, CreateGrenadeVisual());
            GameObject foodPrefab       = GetOrCreatePickupPrefab("Pickup_FoodRation", foodData, 1, CreateFoodVisual());

            // 3. Rải vật phẩm khắp đảo
            ScatterLootOnIsland(medkitPrefab, ammoPrefab, flashlightPrefab, grenadePrefab, foodPrefab);
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(ITEMS_DIR)) Directory.CreateDirectory(ITEMS_DIR);
            if (!Directory.Exists(ICONS_DIR)) Directory.CreateDirectory(ICONS_DIR);
            if (!Directory.Exists(PREFABS_DIR)) Directory.CreateDirectory(PREFABS_DIR);
            if (!Directory.Exists(MATERIALS_DIR)) Directory.CreateDirectory(MATERIALS_DIR);
            AssetDatabase.Refresh();
        }

        // ──────────────────────────────────────────────
        // 1. TẠO HOẶC TẢI ITEM DATA SCRIPTABLE OBJECT
        // ──────────────────────────────────────────────
        private static ItemData GetOrCreateItemData(string id, string name, string desc, ItemType type, bool stackable, int maxStack, string iconName, Color iconColor)
        {
            string path = ITEMS_DIR + "/" + id + ".asset";
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);

            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                item.itemID = id;
                item.itemName = name;
                item.description = desc;
                item.itemType = type;
                item.isStackable = stackable;
                item.maxStack = maxStack;

                // Tạo icon 2D Sprite màu sắc sắc nét
                Sprite icon = CreateOrLoadIcon(iconName, iconColor, id);
                item.icon = icon;

                AssetDatabase.CreateAsset(item, path);
            }
            else
            {
                item.itemName = name;
                item.description = desc;
                item.itemType = type;
                item.isStackable = stackable;
                item.maxStack = maxStack;
                if (item.icon == null)
                {
                    item.icon = CreateOrLoadIcon(iconName, iconColor, id);
                }
                EditorUtility.SetDirty(item);
            }

            return item;
        }

        private static Sprite CreateOrLoadIcon(string iconName, Color mainColor, string typeId)
        {
            string path = ICONS_DIR + "/" + iconName;
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color bgColor = new Color(0.12f, 0.14f, 0.18f, 0.95f);
            Color borderColor = new Color(mainColor.r * 1.2f, mainColor.g * 1.2f, mainColor.b * 1.2f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Viền bo góc
                    bool isBorder = (x < 4 || x >= size - 4 || y < 4 || y >= size - 4);
                    if (isBorder)
                    {
                        tex.SetPixel(x, y, borderColor);
                    }
                    else
                    {
                        // Họa tiết theo loại item
                        bool isGraphic = false;
                        int cx = x - size / 2;
                        int cy = y - size / 2;

                        if (typeId.Contains("medkit"))
                        {
                            // Dấu chữ thập y tế (+)
                            isGraphic = (Mathf.Abs(cx) < 10 && Mathf.Abs(cy) < 32) || (Mathf.Abs(cy) < 10 && Mathf.Abs(cx) < 32);
                        }
                        else if (typeId.Contains("ammo"))
                        {
                            // 3 viên đạn song song
                            isGraphic = (Mathf.Abs(cx) < 26 && Mathf.Abs(cy) < 28) && (x % 18 > 4);
                        }
                        else if (typeId.Contains("flashlight"))
                        {
                            // Hình chóp chiếu sáng
                            isGraphic = (cy > -20 && cy < 25 && Mathf.Abs(cx) < 8) || (cy >= 25 && Mathf.Abs(cx) < (cy - 20) * 1.2f);
                        }
                        else if (typeId.Contains("grenade"))
                        {
                            // Quả lựu đạn tròn có vân
                            float dist = Mathf.Sqrt(cx * cx + cy * cy);
                            isGraphic = (dist < 30f) || (cy > 25 && cy < 40 && Mathf.Abs(cx) < 8);
                        }
                        else if (typeId.Contains("food"))
                        {
                            // Hộp thịt hộp tròn
                            isGraphic = (Mathf.Abs(cx) < 28 && Mathf.Abs(cy) < 24);
                        }

                        if (isGraphic)
                            tex.SetPixel(x, y, mainColor);
                        else
                            tex.SetPixel(x, y, bgColor);
                    }
                }
            }

            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ──────────────────────────────────────────────
        // 2. TẠO PREFAB 3D PICKUP
        // ──────────────────────────────────────────────
        private static GameObject GetOrCreatePickupPrefab(string prefabName, ItemData itemData, int defaultAmount, GameObject visualModel)
        {
            string path = PREFABS_DIR + "/" + prefabName + ".prefab";

            GameObject root = new GameObject(prefabName);
            root.tag = "Untagged";

            // Box Collider để người chơi có thể va chạm / nhìn thấy
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(0.6f, 0.5f, 0.6f);
            col.center = new Vector3(0, 0.25f, 0);

            // Script PickupItem
            PickupItem pickup = root.AddComponent<PickupItem>();
            pickup.itemData = itemData;
            pickup.amount = defaultAmount;
            pickup.interactDistance = 2.5f;
            pickup.autoRotate = true;
            pickup.hoverEffect = true;

            // Đèn phát sáng dịu nhẹ (Soft Point Light) để người chơi dễ tìm trong bóng tối/bụi cỏ
            GameObject lightObj = new GameObject("GlowLight");
            lightObj.transform.SetParent(root.transform);
            lightObj.transform.localPosition = new Vector3(0, 0.35f, 0);
            Light pLight = lightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.range = 2.5f;
            pLight.intensity = 1.2f;
            pLight.color = GetGlowColor(itemData.itemID);
            pLight.shadows = LightShadows.None;

            // Gắn Visual Model vào
            if (visualModel != null)
            {
                visualModel.transform.SetParent(root.transform, false);
            }

            // Lưu thành Prefab
            GameObject prefab = PrefabUtility.CreatePrefab(path, root, ReplacePrefabOptions.ReplaceNameBased);
            DestroyImmediate(root);

            return prefab;
        }

        private static Color GetGlowColor(string id)
        {
            if (id.Contains("medkit")) return new Color(1f, 0.3f, 0.3f);
            if (id.Contains("ammo")) return new Color(1f, 0.8f, 0.2f);
            if (id.Contains("flashlight")) return new Color(0.3f, 0.8f, 1f);
            if (id.Contains("grenade")) return new Color(0.4f, 1f, 0.3f);
            return new Color(1f, 0.6f, 0.2f);
        }

        // ── 3D VISUAL BUILDERS ──

        private static GameObject CreateMedkitVisual()
        {
            Material matWhite = GetOrCreateMaterial("M_Loot_Medkit_White", new Color(0.95f, 0.95f, 0.95f), 0.3f);
            Material matRed = GetOrCreateMaterial("M_Loot_Medkit_Red", new Color(0.85f, 0.1f, 0.1f), 0.2f);

            GameObject model = new GameObject("Visual");

            // Hộp cứu thương màu trắng
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Case";
            box.transform.SetParent(model.transform);
            box.transform.localPosition = new Vector3(0, 0.15f, 0);
            box.transform.localScale = new Vector3(0.35f, 0.14f, 0.25f);
            box.GetComponent<Renderer>().material = matWhite;
            DestroyImmediate(box.GetComponent<Collider>());

            // Dấu chữ thập đỏ (+) trên nắp hộp
            GameObject crossH = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossH.name = "CrossH";
            crossH.transform.SetParent(model.transform);
            crossH.transform.localPosition = new Vector3(0, 0.222f, 0);
            crossH.transform.localScale = new Vector3(0.18f, 0.01f, 0.06f);
            crossH.GetComponent<Renderer>().material = matRed;
            DestroyImmediate(crossH.GetComponent<Collider>());

            GameObject crossV = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossV.name = "CrossV";
            crossV.transform.SetParent(model.transform);
            crossV.transform.localPosition = new Vector3(0, 0.222f, 0);
            crossV.transform.localScale = new Vector3(0.06f, 0.01f, 0.18f);
            crossV.GetComponent<Renderer>().material = matRed;
            DestroyImmediate(crossV.GetComponent<Collider>());

            return model;
        }

        private static GameObject CreateAmmoBoxVisual()
        {
            Material matArmyGreen = GetOrCreateMaterial("M_Loot_Ammo_Green", new Color(0.18f, 0.26f, 0.15f), 0.4f);
            Material matYellow = GetOrCreateMaterial("M_Loot_Ammo_Yellow", new Color(0.9f, 0.75f, 0.1f), 0.3f);

            GameObject model = new GameObject("Visual");

            // Thùng sắt đạn màu xanh lục quân đội
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "AmmoTin";
            box.transform.SetParent(model.transform);
            box.transform.localPosition = new Vector3(0, 0.15f, 0);
            box.transform.localScale = new Vector3(0.32f, 0.20f, 0.16f);
            box.GetComponent<Renderer>().material = matArmyGreen;
            DestroyImmediate(box.GetComponent<Collider>());

            // Nắp thùng viền vàng
            GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lid.name = "LidStripe";
            lid.transform.SetParent(model.transform);
            lid.transform.localPosition = new Vector3(0, 0.255f, 0);
            lid.transform.localScale = new Vector3(0.34f, 0.03f, 0.18f);
            lid.GetComponent<Renderer>().material = matYellow;
            DestroyImmediate(lid.GetComponent<Collider>());

            return model;
        }

        private static GameObject CreateFlashlightVisual()
        {
            Material matMetal = GetOrCreateMaterial("M_Loot_Flashlight_Metal", new Color(0.15f, 0.15f, 0.16f), 0.6f);
            Material matGlass = GetOrCreateMaterial("M_Loot_Flashlight_Glass", new Color(0.8f, 0.95f, 1.0f), 0.1f, true, new Color(0.4f, 0.8f, 1f) * 1.5f);

            GameObject model = new GameObject("Visual");

            // Thân đèn hình trụ
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "Body";
            cylinder.transform.SetParent(model.transform);
            cylinder.transform.localPosition = new Vector3(0, 0.12f, 0);
            cylinder.transform.localScale = new Vector3(0.06f, 0.12f, 0.06f);
            cylinder.transform.localRotation = Quaternion.Euler(75f, 0, 0); // Đặt nằm hơi nghiêng
            cylinder.GetComponent<Renderer>().material = matMetal;
            DestroyImmediate(cylinder.GetComponent<Collider>());

            // Đầu pha đèn
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            head.name = "Head";
            head.transform.SetParent(model.transform);
            head.transform.localPosition = new Vector3(0, 0.16f, 0.12f);
            head.transform.localScale = new Vector3(0.10f, 0.03f, 0.10f);
            head.transform.localRotation = Quaternion.Euler(75f, 0, 0);
            head.GetComponent<Renderer>().material = matGlass;
            DestroyImmediate(head.GetComponent<Collider>());

            return model;
        }

        private static GameObject CreateGrenadeVisual()
        {
            Material matGrenadeGreen = GetOrCreateMaterial("M_Loot_Grenade_Green", new Color(0.15f, 0.32f, 0.15f), 0.2f);
            Material matSilver = GetOrCreateMaterial("M_Loot_Grenade_Silver", new Color(0.7f, 0.7f, 0.75f), 0.8f);

            GameObject model = new GameObject("Visual");

            // Quả lựu đạn tròn
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Body";
            sphere.transform.SetParent(model.transform);
            sphere.transform.localPosition = new Vector3(0, 0.10f, 0);
            sphere.transform.localScale = new Vector3(0.16f, 0.20f, 0.16f);
            sphere.GetComponent<Renderer>().material = matGrenadeGreen;
            DestroyImmediate(sphere.GetComponent<Collider>());

            // Kíp nổ & cần gạt sắt phía trên
            GameObject fuse = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fuse.name = "Fuse";
            fuse.transform.SetParent(model.transform);
            fuse.transform.localPosition = new Vector3(0, 0.22f, 0);
            fuse.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);
            fuse.GetComponent<Renderer>().material = matSilver;
            DestroyImmediate(fuse.GetComponent<Collider>());

            return model;
        }

        private static GameObject CreateFoodVisual()
        {
            Material matTin = GetOrCreateMaterial("M_Loot_Food_Tin", new Color(0.75f, 0.75f, 0.78f), 0.7f);
            Material matLabel = GetOrCreateMaterial("M_Loot_Food_Label", new Color(0.85f, 0.25f, 0.15f), 0.1f);

            GameObject model = new GameObject("Visual");

            // Hộp đồ hộp hình trụ
            GameObject can = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            can.name = "Can";
            can.transform.SetParent(model.transform);
            can.transform.localPosition = new Vector3(0, 0.10f, 0);
            can.transform.localScale = new Vector3(0.18f, 0.09f, 0.18f);
            can.GetComponent<Renderer>().material = matLabel;
            DestroyImmediate(can.GetComponent<Collider>());

            // Nắp thiếc trên
            GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lid.name = "Lid";
            lid.transform.SetParent(model.transform);
            lid.transform.localPosition = new Vector3(0, 0.19f, 0);
            lid.transform.localScale = new Vector3(0.182f, 0.008f, 0.182f);
            lid.GetComponent<Renderer>().material = matTin;
            DestroyImmediate(lid.GetComponent<Collider>());

            return model;
        }

        private static Material GetOrCreateMaterial(string matName, Color color, float smoothness, bool emission = false, Color emColor = default(Color))
        {
            string path = MATERIALS_DIR + "/" + matName + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                mat.SetFloat("_Glossiness", smoothness);
                mat.SetFloat("_Metallic", 0.2f);
                if (emission)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emColor);
                }
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        // ──────────────────────────────────────────────
        // 3. RẢI VẬT PHẨM KHẮP ĐẢO (RAYCAST TERRAIN)
        // ──────────────────────────────────────────────
        private static void ScatterLootOnIsland(GameObject medkitPrefab, GameObject ammoPrefab, GameObject flashlightPrefab, GameObject grenadePrefab, GameObject foodPrefab)
        {
            // 1. Xóa container cũ nếu đã tồn tại
            GameObject oldContainer = GameObject.Find("[ISLAND_LOOT_CONTAINER]");
            if (oldContainer != null)
            {
                Undo.DestroyObjectImmediate(oldContainer);
            }

            GameObject container = new GameObject("[ISLAND_LOOT_CONTAINER]");
            Undo.RegisterCreatedObjectUndo(container, "Spawn Island Loot");

            Terrain terrain = Terrain.activeTerrain;
            Vector3 terrainPos = terrain != null ? terrain.transform.position : Vector3.zero;
            Vector3 terrainSize = terrain != null ? terrain.terrainData.size : new Vector3(1000f, 100f, 1000f);

            // Tìm vị trí người chơi hồi sinh (ForestSpawnPoint hoặc Player)
            Vector3 spawnOrigin = new Vector3(537f, 17f, 526f);
            GameObject spawnPointObj = GameObject.Find("ForestSpawnPoint");
            if (spawnPointObj != null) spawnOrigin = spawnPointObj.transform.position;

            List<GameObject> allPrefabs = new List<GameObject>()
            {
                medkitPrefab, ammoPrefab, flashlightPrefab, grenadePrefab, foodPrefab
            };

            int spawnedCount = 0;

            // ── CỤM 1: KHU VỰC RƠI MÁY BAY & GẦN NGƯỜI CHƠI (Bán kính 10m - 40m) ──
            // Nơi có nhiều hộp sơ cứu, lương thực và đèn pin sơ cấp
            int nearCrashCount = 14;
            for (int i = 0; i < nearCrashCount; i++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist = Random.Range(10f, 42f);
                float x = spawnOrigin.x + Mathf.Cos(angle) * dist;
                float z = spawnOrigin.z + Mathf.Sin(angle) * dist;

                GameObject chosenPrefab = foodPrefab;
                if (i % 3 == 0) chosenPrefab = medkitPrefab;
                else if (i % 3 == 1) chosenPrefab = flashlightPrefab;

                if (TrySpawnAtPosition(chosenPrefab, x, z, terrain, container.transform))
                {
                    spawnedCount++;
                }
            }

            // ── CỤM 2: KHU VỰC ĐƯỜNG MÒN & CÔNG TRÌNH (Bán kính 40m - 150m) ──
            int midAreaCount = 26;
            for (int i = 0; i < midAreaCount; i++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist = Random.Range(40f, 140f);
                float x = spawnOrigin.x + Mathf.Cos(angle) * dist;
                float z = spawnOrigin.z + Mathf.Sin(angle) * dist;

                GameObject chosenPrefab = allPrefabs[Random.Range(0, allPrefabs.Count)];
                if (TrySpawnAtPosition(chosenPrefab, x, z, terrain, container.transform))
                {
                    spawnedCount++;
                }
            }

            // ── CỤM 3: RỪNG SÂU & KHẮP BẢN ĐỒ ĐẢO (Bán kính rộng 100m - 350m) ──
            int farIslandCount = 30;
            for (int i = 0; i < farIslandCount; i++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist = Random.Range(120f, 320f);
                float x = spawnOrigin.x + Mathf.Cos(angle) * dist;
                float z = spawnOrigin.z + Mathf.Sin(angle) * dist;

                GameObject chosenPrefab = allPrefabs[Random.Range(0, allPrefabs.Count)];
                if (TrySpawnAtPosition(chosenPrefab, x, z, terrain, container.transform))
                {
                    spawnedCount++;
                }
            }

            Debug.Log(string.Format("<color=green><b>[ISLAND LOOT SUCCESS]</b></color> Đã tạo và rải thành công <b>{0}</b> vật phẩm sinh tồn khắp hòn đảo!", spawnedCount));
            EditorUtility.DisplayDialog("Thành Công!", string.Format("Đã rải thành công {0} vật phẩm (Băng cứu thương, Hộp đạn, Đèn pin, Lựu đạn, Hộp lương thực) khắp hòn đảo!\n\nTất cả nằm trong nhóm: [ISLAND_LOOT_CONTAINER]", spawnedCount), "Tuyệt Vời");
        }

        private static bool TrySpawnAtPosition(GameObject prefab, float x, float z, Terrain terrain, Transform parent)
        {
            if (prefab == null) return false;

            float sampleY = 20f;
            if (terrain != null)
            {
                sampleY = terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y;
            }

            // Bỏ qua nếu rơi xuống dưới mực nước biển (mặt nước khoảng 11 - 12m)
            if (sampleY < 12.2f) return false;

            // Bắn Raycast xuống để tìm vị trí mặt đất chính xác (Terrain hoặc Props/Sàn nhà)
            RaycastHit hit;
            Vector3 rayStart = new Vector3(x, sampleY + 25f, z);
            if (Physics.Raycast(rayStart, Vector3.down, out hit, 60f))
            {
                // Bỏ qua dốc đứng quá 40 độ để không bị rơi xuống vực
                if (Vector3.Angle(hit.normal, Vector3.up) > 40f) return false;

                Vector3 spawnPos = hit.point + Vector3.up * 0.05f;
                Quaternion spawnRot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance != null)
                {
                    instance.transform.position = spawnPos;
                    instance.transform.rotation = spawnRot;
                    instance.transform.SetParent(parent);
                    return true;
                }
            }

            return false;
        }
    }
}
#endif

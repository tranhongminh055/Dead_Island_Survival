using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using HorrorGame.Cave;
using HorrorGame.Enemy;
using HorrorGame.Environment;
using HorrorGame.Player;

namespace HorrorGame.Editor
{
    /// <summary>
    /// CaveSystemSetupEditor — Công cụ Editor tự động thiết lập và dựng các hang động trong Scene:
    /// 1. Tạo [CaveManager] và liên kết tự động ánh sáng mặt trời/mặt trăng.
    /// 2. Dựng hoàn chỉnh toàn bộ 6 vị trí hang động The Forest (CaveZone 1 -> 6) trên toàn bộ hòn đảo.
    ///    Mỗi hang có Cửa Hang Vòm Đá 3D, Hố đen hun hút, Ván gỗ, Dây thừng leo núi, Cột khói đỏ HUD,
    ///    Ánh sáng nội thất, Âm thanh, Rương kho báu, Ghi chú Lore và Kẻ thù bên trong.
    /// 3. Dịch chuyển nhanh Player tới từng cửa hang trong 6 hang để Playtest ngay lập tức.
    /// </summary>
    public class CaveSystemSetupEditor
    {
        [MenuItem("Horror Game/⛰️ Cài Đặt Hệ Thống Hang Động (Cave System)/1. 🌟 Thiết Lập Toàn Bộ 6 Hang Động The Forest", false, 10)]
        public static void SetupFullCaveSystem()
        {
            // ═══════════════════════════════════════════════════════════════
            // 1. TẠO HOẶC LẤY [CaveManager]
            // ═══════════════════════════════════════════════════════════════
            CaveManager manager = Object.FindObjectOfType<CaveManager>();
            GameObject managerObj;

            if (manager == null)
            {
                managerObj = new GameObject("[CaveSystem_Manager]");
                Undo.RegisterCreatedObjectUndo(managerObj, "Create Cave Manager");
                manager = managerObj.AddComponent<CaveManager>();
                managerObj.AddComponent<CaveMinimap>();
            }
            else
            {
                managerObj = manager.gameObject;
            }

            // Tự động tìm Sun Light và Moon Light trong Scene
            if (manager.sunLight == null)
            {
                var dayNight = Object.FindObjectOfType<DayNightCycle>();
                if (dayNight != null)
                {
                    manager.sunLight = dayNight.sunLight;
                    manager.moonLight = dayNight.MoonLight;
                }
                else
                {
                    Light[] lights = Object.FindObjectsOfType<Light>();
                    foreach (var l in lights)
                    {
                        if (l.type == LightType.Directional)
                        {
                            if (manager.sunLight == null) manager.sunLight = l;
                            else if (manager.moonLight == null) manager.moonLight = l;
                        }
                    }
                }
            }

            // ═══════════════════════════════════════════════════════════════
            // 2. TẠO ROOT [Cave_Zones]
            // ═══════════════════════════════════════════════════════════════
            GameObject cavesRoot = GameObject.Find("[Cave_Zones]");
            if (cavesRoot == null)
            {
                cavesRoot = new GameObject("[Cave_Zones]");
                Undo.RegisterCreatedObjectUndo(cavesRoot, "Create Cave Zones Root");
            }

            manager.allCaves.Clear();

            // Định nghĩa 6 hang động theo bản đồ trải dài khắp hòn đảo
            CaveSetupDef[] caveDefs = new CaveSetupDef[]
            {
                new CaveSetupDef(1, "Hang Sụp Đổ (The Collapsed Den)", new Vector3(385f, 21.5f, 575f), 1,
                    new Color(0.95f, 0.85f, 0.12f), "Bản Vẽ Rìu Đá Nâng Cấp (Upgraded Axe Blueprint)",
                    "Nhật ký hành khách ghế 14B", "Hành khách sống sót",
                    "Chiếc máy bay bị gãy đôi trên không trung. Tôi tỉnh dậy thấy mình rơi xuống một hòn đảo kỳ lạ.\nBan đêm rừng cây phát ra tiếng hú rợn người. Tôi trốn vào hang này... ĐỪNG TẮT ĐÈN PIN!",
                    "Hang động nhỏ ẩn sâu trong rừng rậm Tây Bắc theo phong cách The Forest. Nơi bắt đầu hành trình sinh tồn dưới lòng đất."),

                new CaveSetupDef(2, "Hang Rễ Cây (The Root Cavern)", new Vector3(320f, 22.5f, 660f), 2,
                    new Color(0.95f, 0.65f, 0.15f), "Đèn Pin Thợ Lặn Chống Nước (Diver Flashlight)",
                    "Bút ký của nhà thực vật học", "Tiến sĩ Robert (Năm 1998)",
                    "Những chiếc rễ cây cổ thụ ở đây hút chất dinh dưỡng từ xác chết. Mê cung rễ ngầm dẫn sâu xuống 2 tầng hang ngập nước. Cần có đèn chống nước mới đi sâu được.",
                    "Hang sâu dưới gốc cây cổ thụ khổng lồ phía Bắc đảo. Mê cung rễ cây chằng chịt và suối ngầm 2 tầng."),

                new CaveSetupDef(3, "Hang Thủy Triều (The Tidal Grotto)", new Vector3(450f, 18.0f, 725f), 3,
                    new Color(0.20f, 0.85f, 0.95f), "Bình Dưỡng Khí Lặn Biển Rebreather (Cổ Vật)",
                    "Lời trăn trối của thuyền trưởng tàu đắm", "Thuyền trưởng Vane",
                    "Thủy triều ban đêm dâng ngập toàn bộ hang vách đá này trong tích tắc! Nếu không có bình dưỡng khí Rebreather, ngươi sẽ chết chìm trong bóng tối vĩnh hằng.",
                    "Hang vách đá biển phía Bắc đảo. Mực nước dâng theo thủy triều ban đêm và có bẫy nước ngập."),

                new CaveSetupDef(4, "Hang Đầm Lầy (The Swamp Abyss)", new Vector3(630f, 18.5f, 490f), 4,
                    new Color(0.95f, 0.25f, 0.15f), "Cung Tên Thợ Săn Thổ Dân & Mũi Tên Độc (Cannibal Hunter Bow)",
                    "Bản ghi cảnh báo Thổ Dân Ăn Thịt Người", "Trinh sát biệt kích",
                    "Cảnh giác! Đây là sào huyệt tổ của bộ tộc thổ dân ăn thịt người. Chúng treo đầu lâu quanh cửa hang và giăng cọc nhọn bẫy sập khắp các ngách hầm bí mật.",
                    "Tổ lớn của bộ tộc Thổ Dân Ăn Thịt Người phía Đông. Đầy cạm bẫy cọc nhọn và lối ngầm bí mật."),

                new CaveSetupDef(5, "Hang Nham Thạch (The Volcanic Vent)", new Vector3(550f, 32.0f, 360f), 4,
                    new Color(1.00f, 0.55f, 0.10f), "Bản Vẽ Rèn Kiếm Nhật Katana Rỉ Sét (Ancient Forge Blueprint)",
                    "Bí kíp rèn đúc của Thợ Rèn Cổ Đại", "Thợ rèn Muramasa",
                    "Nhiệt độ dung nham ở hang này đủ nung chảy mọi kim loại cứng nhất hòn đảo. Kích hoạt Lò Rèn Cổ Đại ở buồng trung tâm để rèn thanh kiếm chém quái tối thượng.",
                    "Hang dung nham nóng rực sườn núi lửa phía Nam. Chứa Lò Rèn Cổ Đại để chế tạo vũ khí tối thượng."),

                new CaveSetupDef(6, "Lõi Ký Sinh Ngầm (The Parasite Core - BOSS)", new Vector3(580f, 19.5f, 420f), 5,
                    new Color(0.25f, 0.95f, 0.45f), "Huyết Thanh Kháng Ký Sinh Tối Thượng (Cure Vaccine)",
                    "Hồ sơ nghiên cứu tuyệt mật Dự án Ký Sinh", "Giám đốc Viện Nghiên Cứu Đảo",
                    "Vật chủ mẹ - Nữ Hoàng Ký Sinh (Parasite Queen) - ngự trị dưới đáy vực thẳm hang này. Mọi sinh vật trên đảo đều bị bào tử của ả thao túng. Tiêu diệt ả để tìm lại sự sống!",
                    "Căn cứ sâu nhất dưới lòng thung lũng bí ẩn. Nơi trú ngụ của Trùm Cuối: Nữ Hoàng Ký Sinh (Parasite Queen).")
            };

            for (int k = 0; k < caveDefs.Length; k++)
            {
                CaveSetupDef def = caveDefs[k];
                string zoneName = "Cave" + def.id + "_" + def.name.Split(' ')[1];
                Transform existingZone = cavesRoot.transform.Find(zoneName);
                GameObject zoneObj;

                Vector3 targetPos = def.pos;
                if (Terrain.activeTerrain != null)
                {
                    targetPos.y = Mathf.Max(Terrain.activeTerrain.SampleHeight(targetPos) + Terrain.activeTerrain.transform.position.y, 16.2f);
                }

                if (existingZone == null)
                {
                    zoneObj = new GameObject(zoneName);
                    zoneObj.transform.SetParent(cavesRoot.transform);
                    zoneObj.transform.position = targetPos;
                    Undo.RegisterCreatedObjectUndo(zoneObj, "Create " + zoneName);
                }
                else
                {
                    zoneObj = existingZone.gameObject;
                    zoneObj.transform.position = targetPos;
                }

                CaveZone zone = zoneObj.GetComponent<CaveZone>();
                if (zone == null) zone = zoneObj.AddComponent<CaveZone>();

                zone.caveID = def.id;
                zone.caveName = def.name;
                zone.caveDescription = def.desc;
                zone.difficulty = def.diff;

                manager.allCaves.Add(zone);

                // DỰNG ĐẦY ĐỦ CẤU TRÚC 3D THE FOREST CHO TỪNG HANG
                BuildCompleteCaveStructure(zoneObj, zone, def);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = managerObj;

            Debug.Log("🎉 [CaveSetup] ĐÃ DỰNG THÀNH CÔNG TOÀN BỘ 6 CỬA HANG THE FOREST 3D TRÊN KHẮP HÒN ĐẢO!");
            Debug.Log("👉 Mỗi hang đều có Vòm đá cuội rêu phong ~2.5m (Ảnh 2), Hố đen, Ván gỗ, Dây thừng màu riêng, Cột khói đỏ HUD, Rương báu và Kẻ thù!");
        }

        /// <summary>
        /// Dựng cấu trúc hoàn chỉnh chuẩn phong cách The Forest cho bất kỳ Hang nào
        /// </summary>
        private static void BuildCompleteCaveStructure(GameObject caveRoot, CaveZone zone, CaveSetupDef def)
        {
            // 1. DỰNG CỬA HANG 3D THE FOREST (Vòm đá tảng, hố đen sâu thẳm, ván gỗ, dây thừng, cây cối hoa dại)
            Transform existingEntrance = caveRoot.transform.Find("TheForest_CaveEntrance_3D");
            GameObject forestObj;
            TheForestCaveEntrance forestCave;

            if (existingEntrance == null)
            {
                forestObj = new GameObject("TheForest_CaveEntrance_3D");
                forestObj.transform.SetParent(caveRoot.transform);
                forestObj.transform.localPosition = Vector3.zero;
                forestCave = forestObj.AddComponent<TheForestCaveEntrance>();
                Undo.RegisterCreatedObjectUndo(forestObj, "Create The Forest Cave Entrance");
            }
            else
            {
                forestObj = existingEntrance.gameObject;
                forestCave = forestObj.GetComponent<TheForestCaveEntrance>();
                if (forestCave == null) forestCave = forestObj.AddComponent<TheForestCaveEntrance>();
            }

            forestCave.caveID = def.id;
            forestCave.caveName = def.name;
            forestCave.entranceWorldPosition = caveRoot.transform.position;
            forestCave.ropeColor = def.ropeColor;
            forestCave.BuildForestCaveEntranceModel();

            // 2. HỆ THỐNG ÁNH SÁNG NỘI THẤT (CaveLighting)
            Transform lightsTrans = caveRoot.transform.Find("Cave_LightingSystem");
            if (lightsTrans == null)
            {
                GameObject lightsObj = new GameObject("Cave_LightingSystem");
                lightsObj.transform.SetParent(caveRoot.transform);
                lightsObj.transform.localPosition = Vector3.zero;

                CaveLighting lighting = lightsObj.AddComponent<CaveLighting>();
                lighting.lightType = (def.id == 5) ? CaveLightType.LavaGlow : CaveLightType.Bioluminescent;
                zone.caveLights = lightsObj;

                GameObject pointLightObj = new GameObject("Interior_AmbienceLight");
                pointLightObj.transform.SetParent(lightsObj.transform);
                pointLightObj.transform.localPosition = new Vector3(0f, 2f, 20f);
                Light pl = pointLightObj.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.color = def.ropeColor;
                pl.range = 16f;
                pl.intensity = 1.25f;
            }

            // 3. HỆ THỐNG ÂM THANH (CaveAmbience)
            Transform ambienceTrans = caveRoot.transform.Find("Cave_AmbienceSystem");
            if (ambienceTrans == null)
            {
                GameObject ambObj = new GameObject("Cave_AmbienceSystem");
                ambObj.transform.SetParent(caveRoot.transform);
                ambObj.transform.localPosition = Vector3.zero;

                CaveAmbience ambience = ambObj.AddComponent<CaveAmbience>();
                switch (def.id)
                {
                    case 3:
                        ambience.ambienceType = CaveAmbienceType.Underwater;
                        break;
                    case 4:
                        ambience.ambienceType = CaveAmbienceType.Swamp;
                        break;
                    case 5:
                        ambience.ambienceType = CaveAmbienceType.Volcanic;
                        break;
                    case 6:
                        ambience.ambienceType = CaveAmbienceType.Parasite;
                        break;
                    default:
                        ambience.ambienceType = CaveAmbienceType.DarkWet;
                        break;
                }
            }

            // 4. KẺ THÙ TRONG HANG (CaveEnemySpawner)
            Transform spawnerTrans = caveRoot.transform.Find("Cave_EnemySpawner");
            if (spawnerTrans == null)
            {
                GameObject spawnerObj = new GameObject("Cave_EnemySpawner");
                spawnerObj.transform.SetParent(caveRoot.transform);
                spawnerObj.transform.localPosition = Vector3.zero;

                CaveEnemySpawner spawner = spawnerObj.AddComponent<CaveEnemySpawner>();
                spawner.parentCaveZone = zone;

                for (int i = 1; i <= 3; i++)
                {
                    GameObject pt = new GameObject("EnemySpawnPoint_" + i);
                    pt.transform.SetParent(spawnerObj.transform);
                    pt.transform.localPosition = new Vector3((i - 2) * 4f, 0f, 15f + i * 8f);

                    var config = new CaveEnemySpawner.EnemySpawnConfig
                    {
                        enemyName = "Cave Monster #" + i,
                        spawnPoint = pt.transform,
                        respawnDelay = 300f
                    };
                    spawner.spawnConfigs.Add(config);
                }
            }

            // 5. RƯƠNG KHO BÁU ĐỘC QUYỀN (CaveLootChest)
            Transform chestTrans = caveRoot.transform.Find("CaveLootChest_" + def.id);
            if (chestTrans == null)
            {
                GameObject chestObj = new GameObject("CaveLootChest_" + def.id);
                chestObj.transform.SetParent(caveRoot.transform);
                chestObj.transform.localPosition = new Vector3(2f, 0f, 30f);

                BoxCollider col = chestObj.AddComponent<BoxCollider>();
                col.size = new Vector3(1.5f, 1f, 1.5f);

                CaveLootChest chest = chestObj.AddComponent<CaveLootChest>();
                chest.caveID = def.id;
                chest.uniqueTreasureName = def.treasure;
                zone.lootChests = new GameObject[] { chestObj };
            }

            // 6. GHI CHÚ LORE CỐT TRUYỆN (LoreNote)
            Transform noteTrans = caveRoot.transform.Find("LoreNote_Cave" + def.id);
            if (noteTrans == null)
            {
                GameObject noteObj = new GameObject("LoreNote_Cave" + def.id);
                noteObj.transform.SetParent(caveRoot.transform);
                noteObj.transform.localPosition = new Vector3(-2.5f, 0.4f, 28f);

                BoxCollider col = noteObj.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(1.2f, 1f, 1.2f);

                LoreNote note = noteObj.AddComponent<LoreNote>();
                note.noteID = def.id;
                note.noteTitle = def.noteTitle;
                note.noteAuthor = def.noteAuthor;
                note.noteContent = def.noteContent;
                zone.loreNotes = new GameObject[] { noteObj };
            }

            // 7. TÍNH NĂNG ĐẶC BIỆT RIÊNG TỪNG HANG
            if (def.id == 3 && caveRoot.GetComponent<CaveWaterLevel>() == null)
            {
                caveRoot.AddComponent<CaveWaterLevel>();
            }
            if (def.id == 5)
            {
                Transform forgeTrans = caveRoot.transform.Find("AncientForge");
                if (forgeTrans == null)
                {
                    GameObject forgeObj = new GameObject("AncientForge");
                    forgeObj.transform.SetParent(caveRoot.transform);
                    forgeObj.transform.localPosition = new Vector3(0, 0, 15f);
                    forgeObj.AddComponent<CaveCraftingStation>();
                }
            }
        }

        // =========================================================================
        // DỊCH CHUYỂN PLAYER TỚI TỪNG CỬA HANG ĐỂ PLAYTEST
        // =========================================================================
        private static void TeleportPlayerToCoordinates(Vector3 targetPos, string caveNameLabel)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                var pc = Object.FindObjectOfType<PlayerController>();
                if (pc != null) player = pc.gameObject;
            }

            if (player == null)
            {
                Debug.LogError("❌ Không tìm thấy GameObject của Player trong Scene!");
                return;
            }

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Vector3 spawnP = targetPos + new Vector3(0f, 0.5f, -3.8f);
            if (Terrain.activeTerrain != null)
            {
                spawnP.y = Terrain.activeTerrain.SampleHeight(spawnP) + Terrain.activeTerrain.transform.position.y + 1f;
            }

            player.transform.position = spawnP;
            player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            if (cc != null) cc.enabled = true;

            Selection.activeGameObject = player;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }
            Debug.Log(string.Format("🚀 [CaveTeleport] Đã dịch chuyển người chơi tới ngay trước Cửa {0} tại {1}!", caveNameLabel, spawnP));
        }

        [MenuItem("Horror Game/⛰️ Cài Đặt Hệ Thống Hang Động (Cave System)/2. 🚀 Dịch Chuyển Đến Hang 1 (Rừng Tây Bắc - Hang Sụp Đổ)", false, 20)]
        public static void TeleportPlayerToCave1()
        {
            TeleportPlayerToCoordinates(new Vector3(385f, 21.5f, 575f), "Hang 1 (Hang Sụp Đổ - Rừng Tây Bắc)");
        }

        [MenuItem("Horror Game/⛰️ Cài Đặt Hệ Thống Hang Động (Cave System)/3. 🚀 Dịch Chuyển Đến Hang 2 (Rừng Cổ Thụ - Hang Rễ Cây)", false, 21)]
        public static void TeleportPlayerToCave2()
        {
            TeleportPlayerToCoordinates(new Vector3(320f, 22.5f, 660f), "Hang 2 (Hang Rễ Cây - Rừng Sâu Cổ Thụ)");
        }

        [MenuItem("Horror Game/⛰️ Cài Đặt Hệ Thống Hang Động (Cave System)/4. 🚀 Dịch Chuyển Đến Hang 3 (Vách Biển Bắc - Hang Thủy Triều)", false, 22)]
        public static void TeleportPlayerToCave3()
        {
            TeleportPlayerToCoordinates(new Vector3(450f, 18.0f, 725f), "Hang 3 (Hang Thủy Triều - Vách Đá Biển Bắc)");
        }

        [MenuItem("Horror Game/⛰️ Cài Đặt Hệ Thống Hang Động (Cave System)/5. 🚀 Dịch Chuyển Đến Hang 4 (Rừng Đầm Lầy - Hang Đầm Lầy)", false, 23)]
        public static void TeleportPlayerToCave4()
        {
            TeleportPlayerToCoordinates(new Vector3(630f, 18.5f, 490f), "Hang 4 (Hang Đầm Lầy - Tổ Thổ Dân Phía Đông)");
        }

        [MenuItem("Horror Game/⛰️ Cài Đặt Hệ Thống Hang Động (Cave System)/6. 🚀 Dịch Chuyển Đến Hang 5 (Sườn Núi Lửa - Hang Nham Thạch)", false, 24)]
        public static void TeleportPlayerToCave5()
        {
            TeleportPlayerToCoordinates(new Vector3(550f, 32.0f, 360f), "Hang 5 (Hang Nham Thạch - Sườn Núi Lửa)");
        }

        [MenuItem("Horror Game/⛰️ Cài Đặt Hệ Thống Hang Động (Cave System)/7. 🚀 Dịch Chuyển Đến Hang 6 (Thung Lũng - Lõi Ký Sinh Trùm Cuối)", false, 25)]
        public static void TeleportPlayerToCave6()
        {
            TeleportPlayerToCoordinates(new Vector3(580f, 19.5f, 420f), "Hang 6 (Lõi Ký Sinh Ngầm - Sào Huyệt Trùm Cuối)");
        }

        [MenuItem("Horror Game/🌅 Bình Minh & Thời Gian/1. Đặt Giờ Về 05:15 Sáng (Bình Minh Lên Từ Từ)", false, 30)]
        public static void SetTimeDawn()
        {
            var dayNight = Object.FindObjectOfType<DayNightCycle>();
            if (dayNight != null)
            {
                dayNight.startHour = 5.25f;
                dayNight.currentHour = 5.25f;
                dayNight.dayDurationInMinutes = 12f;
                EditorUtility.SetDirty(dayNight);
                EditorSceneManager.MarkSceneDirty(dayNight.gameObject.scene);
                Debug.Log("🌅 [DayNight] Đã lưu thiết lập: Giờ = 05:15 Sáng (Chu trình Bình Minh chân thực)!");
            }
        }

        [MenuItem("Horror Game/🌅 Bình Minh & Thời Gian/2. Đặt Giờ Về 16:45 Chiều (Hoàng Hôn Lặn Từ Từ)", false, 31)]
        public static void SetTimeSunset()
        {
            var dayNight = Object.FindObjectOfType<DayNightCycle>();
            if (dayNight != null)
            {
                dayNight.startHour = 16.75f;
                dayNight.currentHour = 16.75f;
                dayNight.dayDurationInMinutes = 12f;
                EditorUtility.SetDirty(dayNight);
                EditorSceneManager.MarkSceneDirty(dayNight.gameObject.scene);
                Debug.Log("🌇 [DayNight] Đã lưu thiết lập: Giờ = 16:45 Chiều (Chu trình Hoàng Hôn chân thực)!");
            }
        }

        [MenuItem("Horror Game/🌅 Bình Minh & Thời Gian/3. Đặt Giờ Về 20:00 Tối (Đêm Hẳn - Trăng Sáng Sói Hú)", false, 32)]
        public static void SetTimeNight()
        {
            var dayNight = Object.FindObjectOfType<DayNightCycle>();
            if (dayNight != null)
            {
                dayNight.startHour = 20.0f;
                dayNight.currentHour = 20.0f;
                dayNight.dayDurationInMinutes = 12f;
                EditorUtility.SetDirty(dayNight);
                EditorSceneManager.MarkSceneDirty(dayNight.gameObject.scene);
                Debug.Log("🌙 [DayNight] Đã lưu thiết lập: Giờ = 20:00 Tối (Đêm Hẳn chân thực)!");
            }
        }

        private class CaveSetupDef
        {
            public int id;
            public string name;
            public Vector3 pos;
            public int diff;
            public Color ropeColor;
            public string treasure;
            public string noteTitle;
            public string noteAuthor;
            public string noteContent;
            public string desc;

            public CaveSetupDef(int i, string n, Vector3 p, int d, Color ropeCol, string tr, string nt, string na, string nc, string ds)
            {
                id = i;
                name = n;
                pos = p;
                diff = d;
                ropeColor = ropeCol;
                treasure = tr;
                noteTitle = nt;
                noteAuthor = na;
                noteContent = nc;
                desc = ds;
            }
        }
    }
}

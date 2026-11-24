using System.Collections.Generic;
using HorrorGame.Cave;
using HorrorGame.Enemy;
using HorrorGame.Environment;
using HorrorGame.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
        /// <summary>
        /// Tìm bãi đất khô ráo cao trên mặt nước (Y > 18.0m) và tránh xa cầu gỗ / nhà cửa / nước
        /// </summary>
        private static Vector3 FindDryOpenTerrainSpot(Vector3 preferredPos)
        {
            Terrain t = Terrain.activeTerrain;
            if (t == null) return preferredPos;

            float waterLevel = 17.5f;
            if (IsGoodDrySpot(t, preferredPos, waterLevel))
            {
                preferredPos.y = t.SampleHeight(preferredPos) + t.transform.position.y;
                return preferredPos;
            }

            // Quét xoắn ốc tìm bãi đất khô ráo thoáng gần nhất
            Vector3 bestSpot = preferredPos;
            float bestHeight = -999f;

            for (float r = 15f; r <= 260f; r += 15f)
            {
                for (int deg = 0; deg < 360; deg += 30)
                {
                    float rad = deg * Mathf.Deg2Rad;
                    Vector3 cand = new Vector3(preferredPos.x + Mathf.Cos(rad) * r, 0f, preferredPos.z + Mathf.Sin(rad) * r);
                    if (IsGoodDrySpot(t, cand, waterLevel))
                    {
                        float h = t.SampleHeight(cand) + t.transform.position.y;
                        if (h > bestHeight)
                        {
                            bestHeight = h;
                            bestSpot = new Vector3(cand.x, h, cand.z);
                        }
                    }
                }
                if (bestHeight > waterLevel + 1.5f) break;
            }

            if (bestHeight > -900f) return bestSpot;

            // Fallback: dùng khu vực đồi khô quanh ForestSpawnPoint (544, 539)
            Vector3 fallback = new Vector3(544f, 18.5f, 539f);
            fallback.y = Mathf.Max(t.SampleHeight(fallback) + t.transform.position.y, 18.5f);
            return fallback;
        }

        private static bool IsGoodDrySpot(Terrain t, Vector3 pos, float minDryY)
        {
            Vector3 tPos = t.transform.position;
            Vector3 tSize = t.terrainData.size;
            if (pos.x < tPos.x + 30f || pos.x > tPos.x + tSize.x - 30f) return false;
            if (pos.z < tPos.z + 30f || pos.z > tPos.z + tSize.z - 30f) return false;

            float h = t.SampleHeight(pos) + tPos.y;
            if (h < minDryY) return false;

            // Tránh xa khu vực máy bay rơi, lều trại, điểm spawn Player ít nhất 100m
            GameObject airplane = GameObject.Find("[C400_AIRPLANE_CABIN]");
            if (airplane != null && Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(airplane.transform.position.x, airplane.transform.position.z)) < 100f)
                return false;

            GameObject spawnPt = GameObject.Find("ForestSpawnPoint");
            if (spawnPt != null && Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(spawnPt.transform.position.x, spawnPt.transform.position.z)) < 100f)
                return false;

            if (Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(480f, 495f)) < 95f)
                return false;

            // Kiểm tra bán kính rộng 26m để đảm bảo Hố Sâu + Vành Đồi hoàn toàn ở rừng trống, KHÔNG dính tường gạch/nhà cửa/cầu/nước/máy bay
            Collider[] cols = Physics.OverlapSphere(new Vector3(pos.x, h + 1f, pos.z), 26f);
            foreach (var c in cols)
            {
                if (c == null || c.GetComponent<Terrain>() != null) continue;
                string n = c.gameObject.name.ToLower();
                string pName = c.transform.parent != null ? c.transform.parent.name.ToLower() : "";
                if (n.Contains("villa") || n.Contains("house") || n.Contains("wall") || n.Contains("brick") ||
                    n.Contains("woodboard") || n.Contains("bridge") || n.Contains("water") || n.Contains("cabin") ||
                    n.Contains("handrail") || n.Contains("fence") || n.Contains("struct") || n.Contains("building") ||
                    n.Contains("column") || n.Contains("floor") || n.Contains("roof") || n.Contains("airplane") || n.Contains("c400") ||
                    pName.Contains("villa") || pName.Contains("house") || pName.Contains("building") || pName.Contains("airplane"))
                {
                    return false;
                }
            }
            return true;
        }

        [MenuItem("Horror Game/⛰️ Cài Đặt Hệ Thống Hang Động (Cave System)/0. 🕳️ Tạo Hang 1 (Hố Sâu + Hầm Mỏ Mine) Trên Bãi Đất Khô", false, 1)]
        public static void ClearRockAndCreateHoleCave1()
        {
            // Quét sâu vào rừng xa chỗ máy bay rơi & khu nhà (ít nhất 100m)
            Vector3 dryPos = FindDryOpenTerrainSpot(new Vector3(680f, 19f, 670f));
            CreateCave1AtPosition(dryPos);
        }

        [MenuItem("Horror Game/⛰️ Cài Đặt Hệ Thống Hang Động (Cave System)/0b. 📍 Tạo Hang 1 NGAY TẠI CHỖ CAMERA SCENE VIEW ĐANG NHÌN", false, 2)]
        public static void CreateCave1AtSceneViewPivot()
        {
            Vector3 targetPos = new Vector3(545f, 19f, 540f);
            if (SceneView.lastActiveSceneView != null)
            {
                targetPos = SceneView.lastActiveSceneView.pivot;
                if (Terrain.activeTerrain != null)
                {
                    targetPos.y = Terrain.activeTerrain.SampleHeight(targetPos) + Terrain.activeTerrain.transform.position.y;
                }
            }
            CreateCave1AtPosition(targetPos);
        }

        private static void CreateCave1AtPosition(Vector3 entrancePos)
        {
            // 1. Đảm bảo [CaveSystem_Manager] tồn tại trong Scene
            CaveManager manager = Object.FindObjectOfType<CaveManager>();
            if (manager == null)
            {
                GameObject managerObj = new GameObject("[CaveSystem_Manager]");
                Undo.RegisterCreatedObjectUndo(managerObj, "Create Cave Manager");
                manager = managerObj.AddComponent<CaveManager>();
                managerObj.AddComponent<CaveMinimap>();
            }

            // 2. Tìm hoặc tạo Cave1_Sụp tại đúng vị trí đất khô ráo mới
            GameObject cavesRoot = GameObject.Find("[Cave_Zones]");
            if (cavesRoot == null) cavesRoot = new GameObject("[Cave_Zones]");

            Transform cave1Zone = cavesRoot.transform.Find("Cave1_Sụp");
            if (cave1Zone == null) cave1Zone = cavesRoot.transform.Find("Cave1_Sup");

            if (cave1Zone != null)
            {
                List<GameObject> toDel = new List<GameObject>();
                for (int i = 0; i < cave1Zone.childCount; i++)
                {
                    toDel.Add(cave1Zone.GetChild(i).gameObject);
                }
                foreach (var g in toDel)
                {
                    if (g != null) Undo.DestroyObjectImmediate(g);
                }
                cave1Zone.position = entrancePos;
            }
            else
            {
                GameObject newZone = new GameObject("Cave1_Sụp");
                newZone.transform.SetParent(cavesRoot.transform);
                newZone.transform.position = entrancePos;
                cave1Zone = newZone.transform;
            }

            CaveZone zone = cave1Zone.GetComponent<CaveZone>() ?? cave1Zone.gameObject.AddComponent<CaveZone>();
            zone.caveID = 1;
            zone.caveName = "Hang Sụp Đổ (The Collapsed Den)";
            zone.caveDescription = "Hố sâu dẫn xuống hầm mỏ cổ bỏ hoang dưới lòng đất.";
            zone.difficulty = 1;
            if (!manager.allCaves.Contains(zone)) manager.allCaves.Add(zone);

            // 3. Dựng Hố Sâu Dẫn Xuống Hang Động trên mặt đất khô ráo
            GameObject entranceObj = new GameObject("TheForest_CaveEntrance_3D");
            entranceObj.transform.SetParent(cave1Zone);
            entranceObj.transform.localPosition = Vector3.zero;
            entranceObj.transform.localRotation = Quaternion.identity;

            TheForestCaveEntrance entrance = entranceObj.AddComponent<TheForestCaveEntrance>();
            entrance.caveID = 1;
            entrance.caveName = "Hang Sụp Đổ (The Collapsed Den)";
            entrance.entranceWorldPosition = entrancePos;
            entrance.ropeColor = new Color(0.95f, 0.85f, 0.12f);
            entrance.BuildForestCaveEntranceModel(true);

            // 4. Dựng Hầm Mỏ Ngầm 3 tầng bằng package Mine tại độ sâu -60m
            GameObject mineObj = new GameObject("TheForest_MineCavern");
            mineObj.transform.SetParent(cave1Zone);
            TheForestMineCavern mine = mineObj.AddComponent<TheForestMineCavern>();
            mine.caveID = 1;
            mine.caveName = "Hang Sụp Đổ (The Collapsed Den)";
            mine.entranceSurfacePos = entrancePos;
            mine.subterraneanDepthY = -60f;
            mine.themeColor = new Color(0.95f, 0.85f, 0.12f);
            mine.parentZone = zone;
            mine.BuildMineCavern("Bản Vẽ Rìu Đá Nâng Cấp (Upgraded Axe Blueprint)", "Nhật ký thợ mỏ 14B", "Hành khách sống sót", "ĐỪNG TẮT ĐÈN PIN!");

            // 5. Liên kết hoàn chỉnh 2 chiều
            entrance.LinkToMineCavern(mine, zone);

            // 6. Tự động đưa Player ra đứng ngay đầu dốc nhìn thẳng xuống hố sâu!
            TeleportPlayerToCoordinates(entrance.transform.position, "Hang 1 (Hố Sâu + Hầm Mỏ Mine)");

            Selection.activeGameObject = entranceObj;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log(string.Format("🎉 [CaveSetup] ĐÃ DỰNG HOÀN CHỈNH HỐ SÂU + HẦM MỎ NGẦM 3 TẦNG TẠI ĐẤT KHÔ {0}!", entrance.transform.position));
        }

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

            // Định nghĩa 6 hang động theo bản đồ trải dài khắp hòn đảo (Hang 1 nằm ở rừng rậm thoáng đãng Tây Bắc)
            CaveSetupDef[] caveDefs = new CaveSetupDef[]
            {
                new CaveSetupDef(1, "Hang Sụp Đổ (The Collapsed Den)", new Vector3(310f, 17.5f, 500f), 1,
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
                if (existingZone != null && existingZone.position != Vector3.zero)
                {
                    targetPos = existingZone.position;
                }
                else if (Terrain.activeTerrain != null)
                {
                    targetPos.y = Mathf.Max(Terrain.activeTerrain.SampleHeight(targetPos) + Terrain.activeTerrain.transform.position.y, 16.2f);
                }

                // Vị trí cửa hang (BuildForestCaveEntranceModel sẽ dọn đá & đào hố gọn gàng đúng 1 lần)
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

                if (def.id == 1)
                {
                    zoneObj.transform.rotation = Quaternion.Euler(0f, 75f, 0f);
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

            Debug.Log("🎉 [CaveSetup] ĐÃ DỰNG THÀNH CÔNG TOÀN BỘ 6 CỬA HANG THE FOREST (LỖ HỐ SỤT & DÂY THỪNG) TRÊN KHẮP HÒN ĐẢO!");
            Debug.Log("👉 Mỗi hang là một Miệng Hố Sụt chìm dưới lòng đất với Dây Thừng leo xuống, Hố đen hun hút, Ván gỗ, Cột khói đỏ HUD và Hầm Mỏ Ngầm sâu -60m!");
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
            forestCave.BuildForestCaveEntranceModel(true);

            // 2. DỰNG HẦM MỎ NGẦM ĐA TẦNG BẰNG ASSET MINE (KHỚP 100% MODULAR GRID)
            Transform existingMine = caveRoot.transform.Find("TheForest_MineCavern");
            GameObject mineObj;
            TheForestMineCavern mineCavern;

            if (existingMine == null)
            {
                mineObj = new GameObject("TheForest_MineCavern");
                mineObj.transform.SetParent(caveRoot.transform);
                mineCavern = mineObj.AddComponent<TheForestMineCavern>();
                Undo.RegisterCreatedObjectUndo(mineObj, "Create The Forest Mine Cavern");
            }
            else
            {
                mineObj = existingMine.gameObject;
                mineCavern = mineObj.GetComponent<TheForestMineCavern>();
                if (mineCavern == null) mineCavern = mineObj.AddComponent<TheForestMineCavern>();
            }

            mineCavern.caveID = def.id;
            mineCavern.caveName = def.name;
            mineCavern.entranceSurfacePos = caveRoot.transform.position;
            mineCavern.subterraneanDepthY = -60f;
            mineCavern.themeColor = def.ropeColor;
            mineCavern.parentZone = zone;
            mineCavern.BuildMineCavern(def.treasure, def.noteTitle, def.noteAuthor, def.noteContent);

            // Liên kết hoàn chỉnh 2 chiều (nút [E] + rơi xuống hố <-> thang trèo lên mặt đất)
            forestCave.LinkToMineCavern(mineCavern, zone);

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

            Vector3 spawnP = targetPos + new Vector3(0f, 0.5f, -14.0f);
            if (Terrain.activeTerrain != null)
            {
                spawnP.y = Terrain.activeTerrain.SampleHeight(spawnP) + Terrain.activeTerrain.transform.position.y + 1.0f;
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

        [MenuItem("Horror Game/⛰️ Cài Đặt Hệ Thống Hang Động (Cave System)/🗑️ XÓA SẠCH TOÀN BỘ HANG ĐỘNG & SAN BẰNG ĐỊA HÌNH", false, 0)]
        public static void RemoveAllCavesAndRestoreTerrain()
        {
            // 1. Thu thập tất cả vị trí hang cũ để san bằng lại địa hình Terrain
            List<Vector3> spotsToFlatten = new List<Vector3>();
            spotsToFlatten.Add(new Vector3(545f, 18f, 540f));
            spotsToFlatten.Add(new Vector3(310f, 18f, 500f));

            foreach (var ent in Object.FindObjectsOfType<TheForestCaveEntrance>())
            {
                if (ent != null) spotsToFlatten.Add(ent.transform.position);
            }
            foreach (var cz in Object.FindObjectsOfType<CaveZone>())
            {
                if (cz != null) spotsToFlatten.Add(cz.transform.position);
            }

            foreach (var p in spotsToFlatten)
            {
                FlattenTerrainArea(p, 28f);
            }

            // 2. Xóa toàn bộ GameObject liên quan đến hang động trong Scene
            GameObject cavesRoot = GameObject.Find("[Cave_Zones]");
            if (cavesRoot != null) Undo.DestroyObjectImmediate(cavesRoot);

            GameObject mgrRoot = GameObject.Find("[CaveSystem_Manager]");
            if (mgrRoot != null) Undo.DestroyObjectImmediate(mgrRoot);

            foreach (var m in Object.FindObjectsOfType<CaveManager>())
                if (m != null) Undo.DestroyObjectImmediate(m.gameObject);
            foreach (var mm in Object.FindObjectsOfType<CaveMinimap>())
                if (mm != null) Undo.DestroyObjectImmediate(mm.gameObject);
            foreach (var cz in Object.FindObjectsOfType<CaveZone>())
                if (cz != null) Undo.DestroyObjectImmediate(cz.gameObject);
            foreach (var old in Object.FindObjectsOfType<TheForestCaveEntrance>())
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            foreach (var old in Object.FindObjectsOfType<TheForestMineCavern>())
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            foreach (var s in Object.FindObjectsOfType<CaveSignalSmoke>())
                if (s != null) Undo.DestroyObjectImmediate(s.gameObject);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("🗑️ [CaveSetup] ĐÃ XÓA SẠCH 100% HANG ĐỘNG, KHÓI ĐỎ VÀ SAN BẰNG LẠI ĐỊA HÌNH!");
        }

        [MenuItem("Horror Game/⛰️ Cài Đặt Hệ Thống Hang Động (Cave System)/2. 🧹 Xóa Toàn Bộ Cấu Trúc Hang Cũ Để Dựng Lại Mới", false, 11)]
        public static void ClearOldCaveStructures()
        {
            GameObject cavesRoot = GameObject.Find("[Cave_Zones]");
            if (cavesRoot != null)
            {
                Undo.DestroyObjectImmediate(cavesRoot);
                Debug.Log("🧹 [CaveSetup] Đã xóa sạch thư mục gốc [Cave_Zones] cũ!");
            }
            CaveManager manager = Object.FindObjectOfType<CaveManager>();
            if (manager != null)
            {
                manager.allCaves.Clear();
            }
            foreach (var cz in Object.FindObjectsOfType<CaveZone>())
            {
                if (cz != null) Undo.DestroyObjectImmediate(cz.gameObject);
            }
            foreach (var old in Object.FindObjectsOfType<TheForestCaveEntrance>())
            {
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            }
            foreach (var old in Object.FindObjectsOfType<TheForestMineCavern>())
            {
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            }
            foreach (var s in Object.FindObjectsOfType<CaveSignalSmoke>())
            {
                if (s != null) Undo.DestroyObjectImmediate(s.gameObject);
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("✨ [CaveSetup] Toàn bộ hang cũ đã được dọn sạch sẽ! Bạn hãy bấm lại mục 1 để dựng hệ thống Hầm Mỏ Mine mới.");
        }

        [MenuItem("Horror Game/⛰️ Cài Đặt Hệ Thống Hang Động (Cave System)/2b. 🚜 San Bằng Lại Mô Đất Cũ Gần Máy Bay & Nhà (545, 540)", false, 12)]
        public static void FlattenOldHillNearAirplane()
        {
            FlattenTerrainArea(new Vector3(545f, 18f, 540f), 28f);
            Debug.Log("🚜 [CaveSetup] Đã san bằng hoàn toàn mô đất cũ gần máy bay & khu nhà (545, 540)!");
        }

        private static void FlattenTerrainArea(Vector3 worldCenter, float radiusMeters)
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null || terrain.terrainData == null) return;

            TerrainData td = terrain.terrainData;
            Vector3 tPos = terrain.transform.position;
            Vector3 tSize = td.size;
            int hmWidth = td.heightmapWidth;
            int hmHeight = td.heightmapHeight;

            Vector3 rel = worldCenter - tPos;
            int centerX = Mathf.RoundToInt((rel.x / tSize.x) * (hmWidth - 1));
            int centerZ = Mathf.RoundToInt((rel.z / tSize.z) * (hmHeight - 1));

            int radX = Mathf.Max(4, Mathf.CeilToInt((radiusMeters / tSize.x) * (hmWidth - 1)));
            int radZ = Mathf.Max(4, Mathf.CeilToInt((radiusMeters / tSize.z) * (hmHeight - 1)));

            int startX = Mathf.Clamp(centerX - radX, 0, hmWidth - 1);
            int startZ = Mathf.Clamp(centerZ - radZ, 0, hmHeight - 1);
            int sizeX = Mathf.Clamp(centerX + radX, 0, hmWidth - 1) - startX + 1;
            int sizeZ = Mathf.Clamp(centerZ + radZ, 0, hmHeight - 1) - startZ + 1;

            if (sizeX <= 0 || sizeZ <= 0) return;

            Undo.RegisterCompleteObjectUndo(td, "Flatten Old Cave Hill");
            float[,] heights = td.GetHeights(startX, startZ, sizeX, sizeZ);

            // Lấy cao độ mặt đất tự nhiên trung bình ở 4 góc rìa ngoài vùng san bằng
            float targetH = (heights[0, 0] + heights[0, sizeX - 1] + heights[sizeZ - 1, 0] + heights[sizeZ - 1, sizeX - 1]) * 0.25f;

            for (int z = 0; z < sizeZ; z++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    float wx = ((float)(startX + x) / (hmWidth - 1)) * tSize.x + tPos.x;
                    float wz = ((float)(startZ + z) / (hmHeight - 1)) * tSize.z + tPos.z;
                    float dist = Vector2.Distance(new Vector2(wx, wz), new Vector2(worldCenter.x, worldCenter.z));
                    if (dist < radiusMeters)
                    {
                        float t = Mathf.Clamp01((dist - radiusMeters * 0.65f) / (radiusMeters * 0.35f));
                        float smooth = t * t * (3f - 2f * t);
                        heights[z, x] = Mathf.Lerp(targetH, heights[z, x], smooth);
                    }
                }
            }

            td.SetHeights(startX, startZ, heights);
            EditorUtility.SetDirty(td);
        }

        [MenuItem("Horror Game/⛰️ Cài Đặt Hệ Thống Hang Động (Cave System)/3. 🚀 Dịch Chuyển Đến Hang 1 (Hố Sâu + Hầm Mỏ Mine)", false, 20)]
        public static void TeleportPlayerToCave1()
        {
            Vector3 target = new Vector3(545f, 19f, 540f);
            foreach (var ent in Object.FindObjectsOfType<TheForestCaveEntrance>())
            {
                if (ent != null && ent.caveID == 1)
                {
                    target = ent.transform.position;
                    break;
                }
            }
            TeleportPlayerToCoordinates(target, "Hang 1 (Hố Sâu + Hầm Mỏ Mine)");
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

        [MenuItem("Horror Game/⛰️ Cài Đặt Hệ Thống Hang Động (Cave System)/8. 🔍 Xem Bên Trong Hầm Mỏ Ngầm (Dưới Lòng Đất -60m)", false, 28)]
        public static void ViewUndergroundMineInScene()
        {
            TheForestMineCavern mine = Object.FindObjectOfType<TheForestMineCavern>();
            if (mine != null)
            {
                Selection.activeGameObject = mine.gameObject;
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.FrameSelected();
                }
                Debug.Log("🔍 [CaveSetup] Đã zoom SceneView xuống ngay bên trong Hầm Mỏ Ngầm ở độ sâu -60m!");
            }
            else
            {
                Debug.LogWarning("⚠️ Không tìm thấy Hầm Mỏ TheForestMineCavern trong Scene!");
            }
        }

        [MenuItem("Horror Game/⛰️ Cài Đặt Hệ Thống Hang Động (Cave System)/9. 🚀 Dịch Chuyển Player Xuống Đáy Hang 1 (-60m)", false, 29)]
        public static void TeleportPlayerInsideCave1()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                var pc = Object.FindObjectOfType<HorrorGame.Player.PlayerController>();
                if (pc != null) player = pc.gameObject;
            }

            if (player == null)
            {
                Debug.LogError("❌ Không tìm thấy GameObject của Player trong Scene!");
                return;
            }

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Vector3 spawnP = new Vector3(310f, -59.8f, 501.2f);
            TheForestMineCavern mine = Object.FindObjectOfType<TheForestMineCavern>();
            if (mine != null && mine.interiorSpawnPoint != null)
            {
                spawnP = mine.interiorSpawnPoint.position;
            }
            player.transform.position = spawnP;
            player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            if (cc != null) cc.enabled = true;

            Selection.activeGameObject = player;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }
            Debug.Log("🚀 [CaveTeleport] Đã đưa Player xuống thẳng phòng đón tiếp dưới đáy Hang 1 (-60m)!");
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

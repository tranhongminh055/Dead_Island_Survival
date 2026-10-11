using UnityEngine;
using System.Collections.Generic;

namespace HorrorGame.Environment
{
    /// <summary>
    /// Tự động rải xác các hành khách tử nạn quanh hiện trường máy bay rơi:
    /// - Quanh bán kính 8m - 35m của xác máy bay.
    /// - Tự động bám sát mặt đất (Raycast/Terrain height).
    /// </summary>
    public class CrashPassengerSpawner : MonoBehaviour
    {
        public static readonly Vector3 CRASH_SITE = new Vector3(537f, 17.6f, 545f);

        [Header("── Thiết Lập Rải Thi Thể ──")]
        public int corpseCount = 8;
        public float minRadius = 6f;
        public float maxRadius = 30f;

        private static bool hasSpawned = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName.ToLower().Contains("mainmenu")) return;

            if (FindObjectOfType<CrashPassengerSpawner>() == null)
            {
                GameObject spawnerObj = new GameObject("[CrashPassengerSpawner]");
                spawnerObj.AddComponent<CrashPassengerSpawner>();
            }
        }

        void Start()
        {
            if (hasSpawned) return;
            hasSpawned = true;

            SpawnPassengerCorpses();
        }

        public void SpawnPassengerCorpses()
        {
            // Kiểm tra xem đã có PassengerCorpse nào trong Scene chưa
            if (FindObjectsOfType<PassengerCorpse>().Length >= 4)
            {
                Debug.Log("[CrashPassengerSpawner] Đã có thi thể hành khách trong Scene, không tạo trùng.");
                return;
            }

            string[] names = new string[]
            {
                "Doanh nhân (Ghế 04A)",
                "Nữ hành khách (Ghế 12B)",
                "Tiếp viên trưởng hàng không",
                "Hành khách du lịch (Ghế 18F)",
                "Bác sĩ gia đình (Ghế 08C)",
                "Phi công phụ (Buồng lái)",
                "Hành khách thanh niên (Ghế 21D)",
                "Nữ sinh viên (Ghế 15A)"
            };

            // Vị trí tương đối quanh xác máy bay
            Vector2[] offsets = new Vector2[]
            {
                new Vector2(-6.5f, 5.2f),
                new Vector2(8.2f, -4.5f),
                new Vector2(-12.0f, -8.0f),
                new Vector2(14.5f, 9.0f),
                new Vector2(-4.0f, 15.2f),
                new Vector2(5.5f, -16.0f),
                new Vector2(-18.5f, 6.0f),
                new Vector2(11.0f, 19.5f)
            };

            for (int i = 0; i < Mathf.Min(corpseCount, offsets.Length); i++)
            {
                Vector3 spawnPos = CRASH_SITE + new Vector3(offsets[i].x, 0f, offsets[i].y);

                // Tìm mặt đất chính xác
                float groundY = CRASH_SITE.y;
                RaycastHit hit;
                if (Physics.Raycast(spawnPos + Vector3.up * 30f, Vector3.down, out hit, 60f, ~0, QueryTriggerInteraction.Ignore))
                {
                    groundY = hit.point.y;
                }
                else if (Terrain.activeTerrain != null)
                {
                    groundY = Terrain.activeTerrain.SampleHeight(spawnPos);
                }

                spawnPos.y = groundY;

                GameObject corpseObj = new GameObject("PassengerCorpse_" + (i + 1));
                corpseObj.transform.position = spawnPos;
                corpseObj.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                PassengerCorpse corpse = corpseObj.AddComponent<PassengerCorpse>();
                corpse.passengerName = (i < names.Length) ? names[i] : ("Hành khách xấu số #" + (i + 1));
                corpse.corpseType = i % 3;

                Debug.Log("[CrashPassengerSpawner] Đã đặt thi thể: " + corpse.passengerName + " tại " + spawnPos);
            }
        }
    }
}

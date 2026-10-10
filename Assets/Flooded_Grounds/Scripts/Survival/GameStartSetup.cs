using UnityEngine;
using HorrorGame.Inventory;
using HorrorGame.Survival;

namespace HorrorGame.Survival
{
    /// <summary>
    /// Gắn lên Player. Sau khi cutscene kết thúc, tự động:
    /// 1. Cho rìu vào inventory
    /// 2. Khởi tạo các hệ thống sinh tồn (Animal Spawner, v.v.)
    /// 3. Đảm bảo mọi hệ thống sẵn sàng hoạt động
    /// </summary>
    public class GameStartSetup : MonoBehaviour
    {
        [Header("── Items Khởi Đầu ──")]
        [Tooltip("ItemData của Rìu. Nếu để trống sẽ tự tạo.")]
        public ItemData axeItem;

        [Header("── Animal Spawner ──")]
        [Tooltip("Số lượng thú tối đa trong map")]
        public int maxAnimals = 15;
        [Tooltip("Bán kính spawn quanh player")]
        public float spawnRadius = 80f;

        private bool hasSetup = false;

        void Update()
        {
            // Chờ cutscene kết thúc rồi mới setup
            if (hasSetup) return;

            // 1. Tuyệt đối không setup khi đang ở Main Menu!
            if (HorrorGame.UI.GameMenuManager.Instance != null && HorrorGame.UI.GameMenuManager.Instance.isMainMenuScene)
            {
                return;
            }

            // 2. Nếu có Cutscene máy bay trong Scene -> BẮT BUỘC phải chờ Cutscene chạy xong hoàn toàn!
            if (FindObjectOfType<HorrorGame.Cutscenes.AirplaneCrashCutscene>() != null)
            {
                if (!HorrorGame.Cutscenes.AirplaneCrashCutscene.HasCutsceneFinished)
                {
                    return;
                }
            }

            // 3. Đảm bảo cutscene không còn active
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive) return;

            // Đợi thêm 1 frame để đảm bảo mọi thứ đã sẵn sàng
            hasSetup = true;
            Invoke("SetupGameplay", 0.5f);
        }

        private void SetupGameplay()
        {
            Debug.Log("🎮 [GameStartSetup] Cutscene kết thúc! Khởi tạo gameplay...");

            // ═══════════════════════════════════════════════════════
            // 1. CHO RÌU VÀO INVENTORY
            // ═══════════════════════════════════════════════════════
            GiveStartingAxe();

            // ═══════════════════════════════════════════════════════
            // 2. KHỞI TẠO ANIMAL SPAWNER
            // ═══════════════════════════════════════════════════════
            SetupAnimalSpawner();

            // ═══════════════════════════════════════════════════════
            // 3. KHỞI TẠO HỆ THỐNG MƯA & SẤM CHỚP (WEATHER SYSTEM)
            // ═══════════════════════════════════════════════════════
            SetupWeatherSystem();

            Debug.Log("🎮 [GameStartSetup] Setup hoàn tất! Chúc sinh tồn vui vẻ!");
        }

        private void GiveStartingAxe()
        {
            InventoryManager inv = InventoryManager.Instance;
            if (inv == null)
            {
                Debug.LogWarning("[GameStartSetup] Không tìm thấy InventoryManager!");
                return;
            }

            // Tự load axe item nếu chưa có
            if (axeItem == null)
            {
                axeItem = Resources.Load<ItemData>("Items/AxeItem");
                if (axeItem == null)
                    axeItem = Resources.Load<ItemData>("Items/Axe");
                if (axeItem == null)
                    axeItem = Resources.Load<ItemData>("AxeItem");
            }

            // Nếu vẫn không có, tạo runtime
            if (axeItem == null)
            {
                axeItem = ScriptableObject.CreateInstance<ItemData>();
                axeItem.itemID = "axe";
                axeItem.itemName = "Rìu";
                axeItem.description = "Rìu sinh tồn. Dùng để chặt cây lấy gỗ.";
                axeItem.itemType = ItemType.Tool;
                axeItem.isStackable = false;
                axeItem.maxStack = 1;
            }

            // Kiểm tra đã có rìu chưa
            if (!inv.HasItem(axeItem))
            {
                inv.AddItem(axeItem, 1);
                Debug.Log("🪓 [GameStartSetup] Đã thêm Rìu vào inventory!");
            }
        }

        private void SetupAnimalSpawner()
        {
            // Tìm hoặc tạo AnimalSpawner
            AnimalSpawner spawner = FindObjectOfType<AnimalSpawner>();
            if (spawner == null)
            {
                GameObject spawnerObj = new GameObject("[AnimalSpawner]");
                spawner = spawnerObj.AddComponent<AnimalSpawner>();
                spawner.maxAnimals = maxAnimals;
                spawner.spawnRadius = spawnRadius;
                spawner.playerTransform = this.transform;
                Debug.Log("🦌 [GameStartSetup] Đã tạo AnimalSpawner!");
            }
        }

        private void SetupWeatherSystem()
        {
            if (HorrorGame.Environment.Weather.WeatherSystem.Instance == null)
            {
                GameObject weatherObj = new GameObject("[WeatherSystem]");
                weatherObj.AddComponent<HorrorGame.Environment.Weather.WeatherSystem>();
                Debug.Log("⛈️ [GameStartSetup] Đã tự động kích hoạt WeatherSystem tự nhiên (Mưa & Sấm Chớp)!");
            }

            // Kích hoạt trận mưa bão sấm chớp mở đầu
            if (HorrorGame.Environment.Weather.WeatherSystem.Instance != null)
            {
                HorrorGame.Environment.Weather.WeatherSystem.Instance.TriggerPostTrailerStorm();
            }

            if (GetComponent<HorrorGame.Environment.Weather.PlayerWetness>() == null)
            {
                gameObject.AddComponent<HorrorGame.Environment.Weather.PlayerWetness>();
            }
        }
    }
}

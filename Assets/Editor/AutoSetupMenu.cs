using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

namespace HorrorGame.Editor
{
    [InitializeOnLoad]
    public class AutoSetupMenu
    {
        private const string KEY_SETUP_MENU = "AutoSetupMenu_Done_V5";

        static AutoSetupMenu()
        {
            EditorApplication.delayCall += SetupMenuSystem;
        }

        [MenuItem("Horror Game/🎮 CHẠY GAME (Từ Main Menu)", false, 1)]
        public static void PlayFromMainMenu()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            // Lưu scene đang mở nếu có thay đổi
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            string mainMenuScenePath = "Assets/Flooded_Grounds/Scenes/MainMenu.unity";
            if (!File.Exists(mainMenuScenePath))
            {
                CreateMainMenuScene(mainMenuScenePath);
            }

            // Mở Main Menu scene và ấn Play ngay lập tức
            EditorSceneManager.OpenScene(mainMenuScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Horror Game/🗺 Mở Scene Main Menu", false, 2)]
        public static void OpenMainMenuScene()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            string mainMenuScenePath = "Assets/Flooded_Grounds/Scenes/MainMenu.unity";
            if (!File.Exists(mainMenuScenePath))
            {
                CreateMainMenuScene(mainMenuScenePath);
            }
            EditorSceneManager.OpenScene(mainMenuScenePath, OpenSceneMode.Single);
            Debug.Log("✅ [GameMenu] Đã mở Scene Main Menu!");
        }

        [MenuItem("Horror Game/🌲 Mở Scene Game Chơi (Scene_A)", false, 3)]
        public static void OpenGameplayScene()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            string gameplayScenePath = "Assets/Flooded_Grounds/Scenes/Scene_A.unity";
            if (File.Exists(gameplayScenePath))
            {
                EditorSceneManager.OpenScene(gameplayScenePath, OpenSceneMode.Single);
                Debug.Log("✅ [GameMenu] Đã mở Scene Game Chơi (Scene_A)!");
            }
            else
            {
                Debug.LogWarning("Không tìm thấy: " + gameplayScenePath);
            }
        }

        [MenuItem("Horror Game/🔨 Kích Hoạt Hệ Thống Xây Dựng (Phím B)", false, 10)]
        public static void ForceSetupBuildingSystem()
        {
            EnsureBuildingSystemOnPlayer();
        }

        [MenuItem("Horror Game/🦌 Kích Hoạt Thú Rừng (Animal Spawner)", false, 11)]
        public static void ForceSetupAnimalSpawner()
        {
            EnsureAnimalSpawnerInCurrentScene();
        }

        [MenuItem("Horror Game/⛈️ Kích Hoạt Hệ Thống Mưa & Sấm Chớp (Weather System)", false, 12)]
        public static void ForceSetupWeatherSystem()
        {
            EnsureWeatherSystemInCurrentScene();
        }

        [MenuItem("Horror Game/⚙ Cài Đặt Hệ Thống Menu (Main Menu + Pause)", false, 20)]
        public static void ForceSetupMenu()
        {
            EditorPrefs.DeleteKey(KEY_SETUP_MENU);
            SetupMenuSystem();
        }

        private static void SetupMenuSystem()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            string mainMenuScenePath = "Assets/Flooded_Grounds/Scenes/MainMenu.unity";
            string gameplayScenePath = "Assets/Flooded_Grounds/Scenes/Scene_A.unity";

            // 1. Tạo Scene MainMenu.unity nếu chưa có, hoặc cập nhật ảnh nền nếu đã có
            if (!File.Exists(mainMenuScenePath))
            {
                CreateMainMenuScene(mainMenuScenePath);
            }
            else
            {
                UpdateExistingMainMenuScene(mainMenuScenePath);
            }

            // 2. Cập nhật Build Settings: MainMenu ở index 0, Scene_A ở index 1
            UpdateBuildSettings(mainMenuScenePath, gameplayScenePath);

            // 3. Đảm bảo Scene hiện tại có GameObject [GameMenuManager] và ảnh nền
            EnsurePauseMenuInCurrentScene();

            // 4. Đảm bảo Player có hệ thống Xây Dựng (BuildingSystem)
            EnsureBuildingSystemOnPlayer();

            // 5. Đảm bảo Scene có hệ thống Thú Rừng (AnimalSpawner)
            EnsureAnimalSpawnerInCurrentScene();

            // 6. Đảm bảo Scene có hệ thống Thời Tiết (WeatherSystem)
            EnsureWeatherSystemInCurrentScene();

            EditorPrefs.SetBool(KEY_SETUP_MENU, true);
        }

        private static void CreateMainMenuScene(string scenePath)
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();

            // Tạo Scene mới chế độ Additive để không đóng scene đang mở
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            // Tạo Main Camera cho menu
            GameObject camObj = new GameObject("Main Camera");
            SceneManager.MoveGameObjectToScene(camObj, newScene);
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.04f, 0.06f, 1f);
            camObj.AddComponent<AudioListener>();
            camObj.tag = "MainCamera";

            // Tạo ánh sáng mờ ảo kinh dị
            GameObject lightObj = new GameObject("Directional Light");
            SceneManager.MoveGameObjectToScene(lightObj, newScene);
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.3f, 0.4f, 0.55f);
            light.intensity = 0.4f;
            lightObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            // Tạo GameMenuManager cho Main Menu
            GameObject menuObj = new GameObject("[GameMenuManager]");
            SceneManager.MoveGameObjectToScene(menuObj, newScene);
            var menu = menuObj.AddComponent<HorrorGame.UI.GameMenuManager>();
            menu.isMainMenuScene = true;
            menu.showMenuOnPlayInGameScene = true;
            menu.gameplaySceneName = "Scene_A";
            menu.mainMenuSceneName = "MainMenu";

            // Gán ảnh nền Avatar chính thức
            Texture2D avatarTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Flooded_Grounds/avata game/avata chinh thuc.png");
            if (avatarTex != null) menu.menuBackgroundTexture = avatarTex;

            // Gán nhạc nền kinh dị cho Menu (ưu tiên thư mục "sound main menu")
            AudioClip bgSound = null;
            string[] musicGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Flooded_Grounds/sound main menu" });
            if (musicGuids.Length > 0)
            {
                bgSound = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(musicGuids[0]));
            }
            if (bgSound == null) bgSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/Background.mp3");
            if (bgSound == null) bgSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/WindHowl.mp3");
            if (bgSound != null)
            {
                menu.menuMusic = bgSound;
            }

            // Lưu scene MainMenu
            EditorSceneManager.SaveScene(newScene, scenePath);
            EditorSceneManager.CloseScene(newScene, true);

            Debug.Log("✅ [GameMenu] Đã tạo thành công Scene Main Menu với ảnh nền 'avata chinh thuc.png' tại: " + scenePath);
        }

        private static void UpdateExistingMainMenuScene(string scenePath)
        {
            try
            {
                Texture2D avatarTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Flooded_Grounds/avata game/avata chinh thuc.png");
                // Ưu tiên nhạc kinh dị từ thư mục "sound main menu"
                AudioClip bgSound = null;
                string[] mGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Flooded_Grounds/sound main menu" });
                if (mGuids.Length > 0)
                {
                    bgSound = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(mGuids[0]));
                }
                if (bgSound == null) bgSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/Background.mp3");
                if (bgSound == null) bgSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/WindHowl.mp3");

                Scene s = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                var menu = Object.FindObjectOfType<HorrorGame.UI.GameMenuManager>();
                if (menu != null)
                {
                    bool dirty = false;
                    if (menu.menuBackgroundTexture == null && avatarTex != null)
                    {
                        menu.menuBackgroundTexture = avatarTex;
                        dirty = true;
                    }
                    if (menu.menuMusic == null && bgSound != null)
                    {
                        menu.menuMusic = bgSound;
                        dirty = true;
                    }
                    if (dirty)
                    {
                        EditorUtility.SetDirty(menu);
                        EditorSceneManager.SaveScene(s);
                        Debug.Log("✅ [GameMenu] Đã cập nhật ảnh nền và âm thanh u tối vào MainMenu.unity!");
                    }
                }
                EditorSceneManager.CloseScene(s, true);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[GameMenu] UpdateExistingMainMenuScene: " + e.Message);
            }
        }

        private static void UpdateBuildSettings(string mainMenuPath, string gameplayPath)
        {
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(mainMenuPath, true),
                new EditorBuildSettingsScene(gameplayPath, true)
            };

            EditorBuildSettings.scenes = scenes;
            Debug.Log("✅ [GameMenu] Đã cài đặt Build Settings: Index 0 = MainMenu, Index 1 = Scene_A");
        }

        private static void EnsurePauseMenuInCurrentScene()
        {
            Texture2D avatarTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Flooded_Grounds/avata game/avata chinh thuc.png");
            AudioClip bgSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/Background.mp3");
            if (bgSound == null) bgSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/WindHowl.mp3");

            var existing = Object.FindObjectOfType<HorrorGame.UI.GameMenuManager>();
            if (existing == null)
            {
                GameObject pauseMenuObj = new GameObject("[GameMenuManager]");
                var menu = pauseMenuObj.AddComponent<HorrorGame.UI.GameMenuManager>();
                menu.isMainMenuScene = false; // Chế độ Pause Menu trong lúc chơi
                menu.showMenuOnPlayInGameScene = true;
                menu.gameplaySceneName = "Scene_A";
                menu.mainMenuSceneName = "MainMenu";
                if (avatarTex != null) menu.menuBackgroundTexture = avatarTex;
                if (bgSound != null) menu.menuMusic = bgSound;

                EditorUtility.SetDirty(pauseMenuObj);
                EditorSceneManager.MarkSceneDirty(pauseMenuObj.scene);
                Debug.Log("✅ [GameMenu] Đã thêm [GameMenuManager] vào Scene_A với ảnh nền 'avata chinh thuc.png'! Nhớ Ctrl+S lưu scene.");
            }
            else
            {
                bool dirty = false;
                if (existing.menuBackgroundTexture == null && avatarTex != null)
                {
                    existing.menuBackgroundTexture = avatarTex;
                    dirty = true;
                }
                if (existing.menuMusic == null && bgSound != null)
                {
                    existing.menuMusic = bgSound;
                    dirty = true;
                }
                if (dirty)
                {
                    EditorUtility.SetDirty(existing);
                    EditorSceneManager.MarkSceneDirty(existing.gameObject.scene);
                    Debug.Log("✅ [GameMenu] Đã cập nhật ảnh nền và âm thanh vào [GameMenuManager] của Scene hiện tại!");
                }
            }
        }

        private static void EnsureBuildingSystemOnPlayer()
        {
            var player = Object.FindObjectOfType<HorrorGame.Player.PlayerController>();
            if (player != null)
            {
                var bs = player.GetComponent<HorrorGame.Survival.BuildingSystem>();
                if (bs == null)
                {
                    bs = player.gameObject.AddComponent<HorrorGame.Survival.BuildingSystem>();
                    EditorUtility.SetDirty(player.gameObject);
                    EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
                    Debug.Log("✅ [BuildingSystem] Đã tự động gắn BuildingSystem lên Player! Nhấn phím [B] trong game để mở menu xây dựng.");
                }
            }
        }

        private static void EnsureAnimalSpawnerInCurrentScene()
        {
            var spawner = Object.FindObjectOfType<HorrorGame.Survival.AnimalSpawner>();
            if (spawner == null)
            {
                GameObject spawnerObj = new GameObject("[AnimalSpawner]");
                spawner = spawnerObj.AddComponent<HorrorGame.Survival.AnimalSpawner>();
                EditorUtility.SetDirty(spawnerObj);
                EditorSceneManager.MarkSceneDirty(spawnerObj.scene);
                Debug.Log("✅ [AnimalSpawner] Đã tự động tạo [AnimalSpawner] trong Scene! Hươu, Thỏ, Lợn rừng đã sẵn sàng.");
            }
        }

        private static void EnsureWeatherSystemInCurrentScene()
        {
            HorrorGame.Editor.WeatherSystemSetup.SetupWeatherInScene();
        }
    }
}

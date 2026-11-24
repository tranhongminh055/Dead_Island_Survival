using UnityEngine;
using UnityEditor;

namespace HorrorGame.Editor
{
    [InitializeOnLoad]
    public class AutoFixGun
    {
        static AutoFixGun()
        {
            EditorApplication.delayCall += FixIt;
        }

        static void FixIt()
        {
            // Không sửa trong lúc đang Play (thay đổi sẽ bị mất khi Stop)
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            // Tìm script FPSWeapon trong scene hiện tại
            var weapon = Object.FindObjectOfType<FPSWeapon>();
            if (weapon != null)
            {
                bool changed = false;

                // Tự động thu nhỏ súng
                if (weapon.gunScale.x >= 0.3f)
                {
                    weapon.gunScale = new Vector3(0.05f, 0.05f, 0.05f);
                    changed = true;
                }


                // Chỉnh lại vị trí cho gọn vào góc
                if (weapon.gunPosition.x >= 0.3f)
                {
                    weapon.gunPosition = new Vector3(0.25f, -0.2f, 0.5f);
                    changed = true;
                }

                // Nâng súng lên cho nằm gọn trong màn hình (chỉ áp dụng 1 lần, sau đó bạn tự chỉnh thoải mái)
                const string key = "AutoFixGun_PosV2";
                if (!EditorPrefs.GetBool(key, false))
                {
                    weapon.gunPosition = new Vector3(0.2f, 0.05f, 0.5f);
                    EditorPrefs.SetBool(key, true);
                    changed = true;
                }

                // Xoay súng để nòng chĩa thẳng về phía trước (chỉ áp dụng 1 lần)
                const string keyRot = "AutoFixGun_RotV3";
                if (!EditorPrefs.GetBool(keyRot, false))
                {
                    weapon.gunRotation = new Vector3(0f, 180f, 0f);
                    weapon.gunPosition = new Vector3(0.2f, -0.05f, 0.5f);
                    EditorPrefs.SetBool(keyRot, true);
                    changed = true;
                }

                // Chế độ Auto-Fit: súng tự xoay thẳng, chỉ cần đặt vị trí góc phải-dưới (1 lần)
                const string keyFit = "AutoFixGun_AutoFitV4";
                if (!EditorPrefs.GetBool(keyFit, false))
                {
                    weapon.autoFitGun = true;
                    weapon.gunRotation = Vector3.zero;
                    weapon.gunPosition = new Vector3(0.18f, -0.17f, 0.45f);
                    EditorPrefs.SetBool(keyFit, true);
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(weapon);
                    Debug.Log("✅ [Antigravity AI] Đã tự động dùng phép thuật sửa lại Kích thước và Trục xoay của súng giúp bạn!");
                }
            }

            // ===== RÌU =====
            var axe = Object.FindObjectOfType<HorrorGame.Survival.AxeController>();
            const string keyAxe = "AutoFixAxe_AutoFitV1";
            if (axe != null && !EditorPrefs.GetBool(keyAxe, false))
            {
                axe.autoFitAxe = true;
                axe.forceCameraAttachment = true;
                axe.axePosition = new Vector3(0.28f, -0.3f, 0.55f);
                axe.axeRotation = new Vector3(10f, 0f, 12f);
                EditorPrefs.SetBool(keyAxe, true);
                EditorUtility.SetDirty(axe);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(axe.gameObject.scene);
                Debug.Log("✅ [Antigravity AI] Đã tự động căn chỉnh cây rìu giúp bạn! Nhớ Ctrl+S để lưu scene.");
            }

            // ===== ZOMBIE SPAWNER (HORDE EVERYWHERE) =====
            var spawner = Object.FindObjectOfType<HorrorGame.Enemy.ZombieSpawner>();
            const string keySpawner = "AutoFixZombie_HordeEverywhereV2";
            if (spawner != null && !EditorPrefs.GetBool(keySpawner, false))
            {
                spawner.crashSafeRadius = 10f; // Chỉ 10m sát đám lửa
                spawner.spawnNearPlayer = true;
                spawner.minPlayerDistance = 15f;
                spawner.maxPlayerDistance = 55f;
                spawner.despawnDistance = 140f;
                spawner.maxZombies = 80; // 80 con Zombie đông nghẹt!
                spawner.spawnInterval = 1.0f; // Cứ 1 giây đẻ 1 con
                spawner.initialHordeCount = 30; // 30 con ngay từ đầu
                spawner.autoScatterOverMap = true;
                EditorPrefs.SetBool(keySpawner, true);
                EditorUtility.SetDirty(spawner);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
                Debug.Log("✅ [Antigravity AI] Đã kích hoạt chế độ ĐẠI DỊCH ZOMBIE: 80 con Zombie tràn ngập toàn map! Nhớ Ctrl+S để lưu scene.");
            }
        }
    }
}

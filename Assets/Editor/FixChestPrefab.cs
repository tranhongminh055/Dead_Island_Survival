using UnityEngine;
using UnityEditor;

public class FixChestPrefab : MonoBehaviour
{
    [MenuItem("Tools/Sửa Lỗi Rương Dựng Đứng (Fix Chest)")]
    public static void FixChest()
    {
        // Tìm Prefab LootChest trong dự án
        string[] guids = AssetDatabase.FindAssets("LootChest t:Prefab");
        if (guids.Length == 0)
        {
            Debug.LogError("Không tìm thấy Prefab LootChest!");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (prefabAsset != null)
        {
            // Bắt buộc phải Instantiate ra Scene để sửa Prefab an toàn trong Unity cũ
            GameObject instance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            
            // Reset thằng cha về 0
            instance.transform.localRotation = Quaternion.identity;

            // Tìm thằng con chứa mô hình 3D
            Transform childMesh = instance.transform.Find("USSR war box metal");
            if (childMesh != null)
            {
                childMesh.localRotation = Quaternion.Euler(-90, 0, 0);
            }
            else
            {
                foreach (Transform child in instance.transform)
                {
                    if (child.name.Contains("USSR") || child.name.Contains("war") || child.name.Contains("box"))
                    {
                        child.localRotation = Quaternion.Euler(-90, 0, 0);
                        break;
                    }
                }
            }
            
            // Lưu đè lại vào Prefab
            PrefabUtility.ReplacePrefab(instance, prefabAsset, ReplacePrefabOptions.ConnectToPrefab);
            
            // Xoá bản sao nháp trên Scene
            DestroyImmediate(instance);
            
            Debug.Log("Đã ép thằng cha về 0 và bẻ thằng con -90 thành công! Hãy kiểm tra lại Preview!");
        }
    }
}

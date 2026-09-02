#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using HorrorGame.Inventory;
using System.IO;

public class CreateAmmoItem
{
    [MenuItem("Horror Game/Tạo Đạn (Create Ammo)")]
    public static void CreateAmmo()
    {
        ItemData ammo = ScriptableObject.CreateInstance<ItemData>();
        ammo.itemID = "ammo_9mm";
        ammo.itemName = "Đạn 9mm";
        ammo.description = "Hộp đạn 9mm dùng cho súng ngắn.";
        ammo.itemType = ItemType.Ammunition;
        ammo.isStackable = true;
        ammo.maxStack = 60;

        string path = "Assets/Flooded_Grounds/Resources";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        // Tạo file asset
        AssetDatabase.CreateAsset(ammo, path + "/AmmoItem.asset");
        AssetDatabase.SaveAssets();

        Debug.Log("Đã tạo thành công Data Đạn tại: " + path + "/AmmoItem.asset");
        
        // Highlight file vừa tạo lên cho người dùng thấy
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = ammo;
    }
}
#endif

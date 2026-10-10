using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ListSeatPositions : EditorWindow
{
    [MenuItem("Horror Game/🔍 Liệt Kê Vị Trí Ghế C400")]
    public static void ListPositions()
    {
        GameObject c400 = GameObject.Find("[C400_AIRPLANE_CABIN]");
        if (c400 == null)
        {
            // Tự load C400 vào
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/c-400/source/c-400.fbx");
            if (prefab != null)
            {
                c400 = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                c400.name = "[C400_AIRPLANE_CABIN]";
                c400.transform.position = new Vector3(0f, 1000f, 0f);
                c400.transform.localScale = Vector3.one * 10f;
            }
        }
        
        if (c400 == null)
        {
            Debug.LogError("❌ Không tìm thấy C400!");
            return;
        }
        
        // Tìm CargoSpace và CargoChairs
        Transform[] all = c400.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (t.name == "CargoSpace" || t.name == "CargoChairs" || 
                t.name == "CargoChairsCushionsL" || t.name == "CargoChairsCushionsR")
            {
                // Lấy bounds của mesh
                Renderer r = t.GetComponent<Renderer>();
                string boundsInfo = "No Renderer";
                if (r != null)
                {
                    boundsInfo = "Bounds center=" + r.bounds.center + " size=" + r.bounds.size + 
                                 " min=" + r.bounds.min + " max=" + r.bounds.max;
                }
                
                Debug.Log("📍 " + t.name + 
                    "\n   WorldPos=" + t.position + 
                    "\n   LocalPos=" + t.localPosition + 
                    "\n   LocalRot=" + t.localEulerAngles +
                    "\n   LossyScale=" + t.lossyScale +
                    "\n   " + boundsInfo);
            }
        }
        
        // Tìm bounds tổng thể của CargoChairs
        Transform cargoChairs = null;
        foreach (Transform t in all)
        {
            if (t.name == "CargoChairs") { cargoChairs = t; break; }
        }
        
        if (cargoChairs != null)
        {
            Renderer chairRenderer = cargoChairs.GetComponent<Renderer>();
            if (chairRenderer != null)
            {
                Bounds b = chairRenderer.bounds;
                Debug.Log("🪑 CargoChairs Bounds:\n" +
                    "   Center=" + b.center + "\n" +
                    "   Size=" + b.size + "\n" +
                    "   Min=" + b.min + "\n" +
                    "   Max=" + b.max + "\n" +
                    "   Khoảng Z (dọc thân máy bay): " + b.min.z + " → " + b.max.z + " = " + b.size.z + "m");
            }
        }
    }
}

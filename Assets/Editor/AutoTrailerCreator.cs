/*
using UnityEngine;
using UnityEditor;
// using Cinemachine; // Gây lỗi vì bạn chưa cài Cinemachine từ Asset Store

public class AutoTrailerCreator : EditorWindow
{
    private GameObject airplaneModel;
    private float flightSpeed = 10f;

    [MenuItem("Tools/Auto Trailer Setup")]
    public static void ShowWindow()
    {
        GetWindow<AutoTrailerCreator>("Auto Trailer Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Trailer Setup Settings", EditorStyles.boldLabel);
        
        airplaneModel = (GameObject)EditorGUILayout.ObjectField("Airplane Model", airplaneModel, typeof(GameObject), true);
        flightSpeed = EditorGUILayout.FloatField("Flight Speed", flightSpeed);

        if (GUILayout.Button("Create Trailer Setup"))
        {
            SetupTrailer();
        }
    }

    private void SetupTrailer()
    {
        if (airplaneModel == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign the Airplane Model in the window first.", "OK");
            return;
        }

        // 1. Create a Smooth Path object
        // GameObject pathObj = new GameObject("Cinemachine Trailer Path");
        // CinemachineSmoothPath smoothPath = pathObj.AddComponent<CinemachineSmoothPath>();
        
        // Setup a few default waypoints to form a trajectory
        // ... (đã ẩn code) ...

        Debug.Log("Auto Trailer Setup Complete! A smooth path was created and the Dolly Cart was attached to the airplane.");
    }
}
*/

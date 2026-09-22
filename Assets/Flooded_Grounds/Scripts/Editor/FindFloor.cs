using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FindFloor
{
    [MenuItem("Horror Game/Debug/Find Floor")]
    public static void DoFindFloor()
    {
        GameObject c400 = GameObject.Find("[C400_AIRPLANE_CABIN]");
        if (c400 == null) return;
        
        Renderer[] renderers = c400.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        
        List<MeshCollider> addedColliders = new List<MeshCollider>();
        foreach(var r in renderers) {
            if (r.gameObject.GetComponent<Collider>() == null) {
                var mc = r.gameObject.AddComponent<MeshCollider>();
                addedColliders.Add(mc);
            }
        }
        
        Vector3 rayStart = bounds.center;
        RaycastHit hit;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 1000f)) {
            Debug.Log("Hit Floor at Y: " + hit.point.y + " (Distance from center: " + (bounds.center.y - hit.point.y) + ")");
            Debug.Log("Offset from transform.position.y: " + (hit.point.y - c400.transform.position.y));
        } else {
            Debug.Log("Raycast missed.");
        }
        
        foreach(var mc in addedColliders) {
            Object.DestroyImmediate(mc);
        }
    }
}

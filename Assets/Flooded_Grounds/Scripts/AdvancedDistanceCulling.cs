using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace HorrorGame.Optimization
{
    public class AdvancedDistanceCulling : MonoBehaviour
    {
        [Header("Player Reference")]
        public Transform player;

        [Header("Culling Settings")]
        [Tooltip("Khoảng cách (mét) mà vật thể sẽ bị đóng băng (ẩn đi)")]
        public float cullDistance = 60f; 
        [Tooltip("Bao lâu thì quét 1 lần? (Giây) - Để 0.5s cho nhẹ máy")]
        public float scanInterval = 0.5f; 

        [Header("Tags To Optimize")]
        [Tooltip("Gắn tag 'Enemy' cho Zombie, 'Loot' cho hòm đồ...")]
        public string[] tagsToCull = { "Enemy", "Loot", "Prop" };

        private List<GameObject> managedObjects = new List<GameObject>();

        void Start()
        {
            if (player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }

            // Tìm tất cả các vật thể trên bản đồ có Tag cần tối ưu
            RefreshObjectsList();

            // Chạy vòng lặp kiểm tra
            StartCoroutine(CullRoutine());
        }

        // Hàm này cho phép bạn gọi lại nếu có sinh thêm Zombie mới (Spawner)
        public void RefreshObjectsList()
        {
            managedObjects.Clear();
            foreach (string tag in tagsToCull)
            {
                GameObject[] objs = GameObject.FindGameObjectsWithTag(tag);
                managedObjects.AddRange(objs);
            }
            Debug.Log("[DistanceCulling] Đang quản lý và tối ưu: " + managedObjects.Count + " vật thể.");
        }

        IEnumerator CullRoutine()
        {
            while (true)
            {
                if (player == null) yield return new WaitForSeconds(scanInterval);

                // Quét qua toàn bộ vật thể
                for (int i = managedObjects.Count - 1; i >= 0; i--)
                {
                    GameObject obj = managedObjects[i];
                    
                    // Nếu vật thể đã bị xoá (ví dụ zombie chết), xoá khỏi list
                    if (obj == null) 
                    {
                        managedObjects.RemoveAt(i);
                        continue;
                    }

                    // Tính khoảng cách (dùng sqrMagnitude để CPU tính toán siêu nhanh thay vì Distance)
                    Vector3 offset = obj.transform.position - player.position;
                    float sqrLen = offset.sqrMagnitude;

                    // So sánh bình phương khoảng cách
                    if (sqrLen > cullDistance * cullDistance)
                    {
                        // Ở quá xa -> Tắt vật thể
                        if (obj.activeSelf) obj.SetActive(false);
                    }
                    else
                    {
                        // Ở gần -> Bật vật thể
                        if (!obj.activeSelf) obj.SetActive(true);
                    }
                }

                yield return new WaitForSeconds(scanInterval); // Nghỉ 0.5s rồi mới quét tiếp (Rất nhẹ CPU)
            }
        }
    }
}

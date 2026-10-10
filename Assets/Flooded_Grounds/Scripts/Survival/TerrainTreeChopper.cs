using UnityEngine;
using System.Collections.Generic;

namespace HorrorGame.Survival
{
    /// <summary>
    /// Cho phép chặt các cây được vẽ bằng Terrain (Paint Trees).
    /// Cây Terrain KHÔNG có GameObject và (trong dự án này) KHÔNG có Collider,
    /// nên Raycast của rìu xuyên qua mà không trúng gì.
    ///
    /// Cách hoạt động:
    /// 1. Dò tìm cây Terrain nằm trên hướng nhìn của người chơi (tính toán hình học, không cần Collider).
    /// 2. Xóa cây đó khỏi Terrain và thay bằng một GameObject thật (prefab cây gốc) có ChoppableTree + Collider.
    /// 3. GameObject đó rung khi bị chém, đổ xuống khi hết HP và rơi gỗ/lá.
    ///
    /// AN TOÀN: Danh sách cây gốc của TerrainData được backup và KHÔI PHỤC khi thoát Play Mode,
    /// để không làm mất cây vĩnh viễn trong asset Scene_A_Terrain.
    /// </summary>
    public class TerrainTreeChopper : MonoBehaviour
    {
        private static TerrainTreeChopper instance;

        [Tooltip("Bán kính thân cây cơ bản (mét), nhân với widthScale của từng cây")]
        public float baseTrunkRadius = 0.35f;
        [Tooltip("Sai số cho phép khi ngắm (mét) - càng lớn càng dễ chém trúng")]
        public float aimTolerance = 0.6f;
        [Tooltip("Chiều cao thân cây có thể chém (mét), nhân với heightScale")]
        public float choppableTrunkHeight = 5f;

        private readonly Dictionary<TerrainData, TreeInstance[]> backups = new Dictionary<TerrainData, TreeInstance[]>();

        public static TerrainTreeChopper Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("[TerrainTreeChopper]");
                    instance = go.AddComponent<TerrainTreeChopper>();
                }
                return instance;
            }
        }

        void Awake()
        {
            if (instance == null) instance = this;
            else if (instance != this) Destroy(this);
        }

        /// <summary>
        /// Tìm cây Terrain gần nhất nằm trên tia ngắm trong tầm chặt.
        /// </summary>
        public bool FindTreeAlongRay(Ray ray, float range, out Terrain terrain, out int treeIndex, out Vector3 hitPoint, out float distance)
        {
            terrain = null;
            treeIndex = -1;
            hitPoint = Vector3.zero;
            distance = float.MaxValue;

            // Hướng ngang của tia (bỏ trục Y)
            Vector2 dirXZ = new Vector2(ray.direction.x, ray.direction.z);
            float horizMag = dirXZ.magnitude;
            if (horizMag < 0.01f) return false; // Đang nhìn thẳng lên trời / xuống đất
            Vector2 h = dirXZ / horizMag;

            int totalTreesScanned = 0;
            int closestCandidateIdx = -1;
            float closestCandidateDist = float.MaxValue;

            foreach (Terrain t in Terrain.activeTerrains)
            {
                if (t == null || t.terrainData == null) continue;
                TerrainData data = t.terrainData;
                TreeInstance[] trees = data.treeInstances;
                if (trees == null || trees.Length == 0) continue;

                Vector3 size = data.size;
                Vector3 origin = t.transform.position;
                float searchBox = range + 5f;
                totalTreesScanned += trees.Length;

                for (int i = 0; i < trees.Length; i++)
                {
                    Vector3 p = Vector3.Scale(trees[i].position, size) + origin;

                    float dx = p.x - ray.origin.x;
                    float dz = p.z - ray.origin.z;
                    if (Mathf.Abs(dx) > searchBox || Mathf.Abs(dz) > searchBox) continue; // Loại nhanh

                    float radius = baseTrunkRadius * Mathf.Max(0.3f, trees[i].widthScale);

                    // Khoảng cách theo hướng nhìn (along) và lệch ngang (perp) trên mặt phẳng XZ
                    float along = dx * h.x + dz * h.y;
                    if (along <= 0f) continue; // Cây ở phía sau
                    float perpX = dx - h.x * along;
                    float perpZ = dz - h.y * along;
                    float perp = Mathf.Sqrt(perpX * perpX + perpZ * perpZ);
                    if (perp > radius + aimTolerance) continue; // Không ngắm vào cây

                    float alongToSurface = Mathf.Max(0f, along - radius);
                    float dist3D = alongToSurface / horizMag;
                    if (dist3D > range) continue; // Quá xa

                    // Độ cao của tia khi chạm thân cây phải nằm trong đoạn thân
                    float rayY = ray.origin.y + ray.direction.y * dist3D;
                    float trunkTop = p.y + choppableTrunkHeight * Mathf.Max(0.3f, trees[i].heightScale);
                    if (rayY < p.y - 1f || rayY > trunkTop) continue;

                    if (dist3D < distance)
                    {
                        distance = dist3D;
                        terrain = t;
                        treeIndex = i;
                        hitPoint = ray.origin + ray.direction * dist3D;
                        closestCandidateIdx = i;
                        closestCandidateDist = dist3D;
                    }
                }
            }

            if (terrain != null)
                Debug.Log(string.Format("[TerrainTreeChopper] Tìm thấy cây Terrain: index={0}, dist={1:F2}m, totalScanned={2}", closestCandidateIdx, closestCandidateDist, totalTreesScanned));
            else
                Debug.Log(string.Format("[TerrainTreeChopper] Không tìm thấy cây nào trong tầm. totalScanned={0}, range={1}", totalTreesScanned, range));

            return terrain != null;
        }

        /// <summary>
        /// Thay cây Terrain bằng GameObject thật có ChoppableTree để rung/đổ được.
        /// </summary>
        public ChoppableTree ConvertToChoppable(Terrain terrain, int treeIndex)
        {
            TerrainData data = terrain.terrainData;
            TreeInstance[] trees = data.treeInstances;
            if (treeIndex < 0 || treeIndex >= trees.Length) return null;

            // Backup danh sách cây gốc (chỉ lần đầu) để khôi phục khi thoát game
            if (!backups.ContainsKey(data))
            {
                backups[data] = (TreeInstance[])trees.Clone();
            }

            TreeInstance ti = trees[treeIndex];
            Vector3 worldPos = Vector3.Scale(ti.position, data.size) + terrain.transform.position;
            Quaternion rot = Quaternion.Euler(0f, ti.rotation * Mathf.Rad2Deg, 0f);

            GameObject prefab = null;
            TreePrototype[] protos = data.treePrototypes;
            if (ti.prototypeIndex >= 0 && ti.prototypeIndex < protos.Length)
            {
                prefab = protos[ti.prototypeIndex].prefab;
            }

            GameObject treeObj;
            if (prefab != null)
            {
                treeObj = Instantiate(prefab, worldPos, rot);
                Vector3 baseScale = prefab.transform.localScale;
                treeObj.transform.localScale = new Vector3(
                    baseScale.x * ti.widthScale,
                    baseScale.y * ti.heightScale,
                    baseScale.z * ti.widthScale);
                treeObj.name = "ChoppableTree_" + prefab.name;
            }
            else
            {
                // Không có prefab -> dựng thân cây đơn giản
                treeObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                treeObj.transform.position = worldPos + Vector3.up * 3f;
                treeObj.transform.localScale = new Vector3(0.5f, 3f, 0.5f);
                treeObj.name = "ChoppableTree_Placeholder";
            }

            // Collider thân cây (prefab cây TreeCreator không có collider)
            if (treeObj.GetComponentInChildren<Collider>() == null)
            {
                CapsuleCollider cap = treeObj.AddComponent<CapsuleCollider>();
                float sy = Mathf.Max(0.01f, treeObj.transform.localScale.y);
                float sx = Mathf.Max(0.01f, treeObj.transform.localScale.x);
                cap.direction = 1; // Trục Y
                cap.height = 10f / sy * Mathf.Max(0.4f, ti.heightScale);
                cap.center = new Vector3(0f, cap.height * 0.5f, 0f);
                cap.radius = Mathf.Max(0.55f, baseTrunkRadius * Mathf.Max(0.4f, ti.widthScale) / sx);
            }

            // Xóa cây khỏi Terrain (sau khi đã spawn bản thay thế ở đúng vị trí -> không bị nháy)
            List<TreeInstance> list = new List<TreeInstance>(trees);
            list.RemoveAt(treeIndex);
            data.treeInstances = list.ToArray();

            // Gắn script chặt cây (Awake chạy ngay -> có HP lập tức)
            ChoppableTree choppable = treeObj.AddComponent<ChoppableTree>();
            choppable.trunkRadius = baseTrunkRadius * Mathf.Max(0.5f, ti.widthScale);
            choppable.treeHeight = 12f * Mathf.Max(0.5f, ti.heightScale);
            Debug.Log("[TerrainTreeChopper] Đã chuyển cây Terrain thành cây chặt được: " + treeObj.name);
            return choppable;
        }

        void RestoreTerrains()
        {
            foreach (var kv in backups)
            {
                if (kv.Key != null) kv.Key.treeInstances = kv.Value;
            }
            backups.Clear();
        }

        void OnApplicationQuit()
        {
            RestoreTerrains();
        }

        void OnDestroy()
        {
            RestoreTerrains();
            if (instance == this) instance = null;
        }
    }
}

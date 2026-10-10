using UnityEngine;
using HorrorGame.Inventory;

namespace HorrorGame.Survival
{
    /// <summary>
    /// Hệ thống Lửa Trại (Campfire) kiểu The Forest.
    /// Player đặt lửa trại bằng BuildingSystem → nhìn vào lửa → nhấn E để nấu.
    /// 
    /// Quy trình nấu:
    /// 1. Player nhìn vào lửa trại + nhấn E → đặt thịt sống lên
    /// 2. Thịt nấu trong cookTime giây → chuyển thành thịt chín
    /// 3. Nhấn E để lấy thịt chín
    /// 4. Nếu để quá lâu (burnTime) → thịt cháy
    /// 
    /// Gắn component này lên bất kỳ GameObject lửa trại nào trong scene.
    /// </summary>
    public class CookingStation : MonoBehaviour
    {
        [Header("── Cấu Hình Nấu ──")]
        public float cookTime = 15f;        // Thời gian nấu chín (giây)
        public float burnTime = 30f;        // Thời gian cháy (giây) tính từ khi bắt đầu nấu
        public int maxSlots = 3;            // Số thịt nấu cùng lúc

        [Header("── Hiệu Ứng ──")]
        public Light fireLight;
        public ParticleSystem fireParticles;

        // State
        private CookingSlot[] cookingSlots;
        private bool playerLooking = false;
        private Transform playerTransform;

        // UI
        private GUIStyle titleStyle;
        private GUIStyle normalStyle;
        private GUIStyle shadowStyle;
        private GUIStyle cookingStyle;
        private GUIStyle cookedStyle;
        private GUIStyle burntStyle;

        private struct CookingSlot
        {
            public bool hasItem;
            public string itemType;     // "raw_meat"
            public float cookTimer;
            public CookState state;
        }

        public enum CookState
        {
            Empty,
            Cooking,    // Đang nấu
            Cooked,     // Đã chín
            Burnt       // Đã cháy
        }

        void Start()
        {
            cookingSlots = new CookingSlot[maxSlots];
            for (int i = 0; i < maxSlots; i++)
            {
                cookingSlots[i] = new CookingSlot { hasItem = false, state = CookState.Empty };
            }

            // Tìm player
            var pc = FindObjectOfType<HorrorGame.Player.PlayerController>();
            if (pc != null) playerTransform = pc.transform;

            // Tạo hiệu ứng lửa nếu chưa có
            if (fireLight == null)
                CreateFireEffects();
        }

        void Update()
        {
            // Cập nhật timer nấu
            for (int i = 0; i < cookingSlots.Length; i++)
            {
                if (!cookingSlots[i].hasItem) continue;

                if (cookingSlots[i].state == CookState.Cooking)
                {
                    cookingSlots[i].cookTimer += Time.deltaTime;

                    if (cookingSlots[i].cookTimer >= burnTime)
                    {
                        cookingSlots[i].state = CookState.Burnt;
                    }
                    else if (cookingSlots[i].cookTimer >= cookTime)
                    {
                        cookingSlots[i].state = CookState.Cooked;
                    }
                }
            }

            // Kiểm tra player nhìn vào
            CheckPlayerLooking();

            // Xử lý input
            if (playerLooking && Input.GetKeyDown(KeyCode.E))
            {
                HandleInteract();
            }

            // Nhấp nháy lửa
            AnimateFireLight();
        }

        private void CheckPlayerLooking()
        {
            playerLooking = false;
            Camera cam = Camera.main;
            if (cam == null) return;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 4f))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    playerLooking = true;
                }
            }
        }

        private void HandleInteract()
        {
            InventoryManager inv = InventoryManager.Instance;
            if (inv == null) return;

            // Ưu tiên 1: Lấy thịt chín/cháy ra
            for (int i = 0; i < cookingSlots.Length; i++)
            {
                if (cookingSlots[i].state == CookState.Cooked)
                {
                    // Thêm thịt chín vào inventory
                    ItemData cookedMeat = GetOrCreateItemData("cooked_meat", "Thịt Chín",
                        "Thịt đã nấu chín. Ăn để hồi 30 độ no và 10 HP.",
                        ItemType.Consumable, true, 10);
                    inv.AddItem(cookedMeat, 1);
                    cookingSlots[i] = new CookingSlot { hasItem = false, state = CookState.Empty };
                    Debug.Log("🍖 Lấy thịt chín!");
                    return;
                }
                else if (cookingSlots[i].state == CookState.Burnt)
                {
                    // Thêm thịt cháy vào inventory
                    ItemData burntMeat = GetOrCreateItemData("burnt_meat", "Thịt Cháy",
                        "Thịt bị cháy đen. Ăn bị trừ 5 HP nhưng hồi 10 độ no.",
                        ItemType.Consumable, true, 10);
                    inv.AddItem(burntMeat, 1);
                    cookingSlots[i] = new CookingSlot { hasItem = false, state = CookState.Empty };
                    Debug.Log("🔥 Lấy thịt cháy!");
                    return;
                }
            }

            // Ưu tiên 2: Đặt thịt sống lên nấu
            if (inv.GetItemCountByID("raw_meat") > 0)
            {
                // Tìm slot trống
                for (int i = 0; i < cookingSlots.Length; i++)
                {
                    if (!cookingSlots[i].hasItem)
                    {
                        inv.RemoveItemByID("raw_meat", 1);
                        cookingSlots[i] = new CookingSlot
                        {
                            hasItem = true,
                            itemType = "raw_meat",
                            cookTimer = 0f,
                            state = CookState.Cooking
                        };
                        Debug.Log("🥩 Đặt thịt sống lên lửa trại!");
                        return;
                    }
                }
                Debug.Log("Lửa trại đã đầy! (Tối đa " + maxSlots + " miếng)");
            }
        }

        // ═══════════════════════════════════════════════════════
        // HIỆU ỨNG LỬA
        // ═══════════════════════════════════════════════════════

        private void CreateFireEffects()
        {
            // Tạo ánh sáng lửa
            GameObject lightObj = new GameObject("FireLight");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = new Vector3(0, 0.5f, 0);
            fireLight = lightObj.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.6f, 0.2f);
            fireLight.intensity = 2f;
            fireLight.range = 8f;

            // Tạo particle lửa
            GameObject particleObj = new GameObject("FireParticles");
            particleObj.transform.SetParent(transform, false);
            particleObj.transform.localPosition = new Vector3(0, 0.3f, 0);
            fireParticles = particleObj.AddComponent<ParticleSystem>();

            var main = fireParticles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.5f, 0f, 1f),
                new Color(1f, 0.2f, 0f, 0.5f));
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = fireParticles.emission;
            emission.rateOverTime = 20f;

            var shape = fireParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.2f;

            var sizeOverLifetime = fireParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Material
            Shader std = Shader.Find("Particles/Standard Unlit");
            if (std == null) std = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (std != null)
            {
                Material mat = new Material(std);
                mat.color = new Color(1f, 0.5f, 0f, 0.8f);
                fireParticles.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            }

            fireParticles.Play();
        }

        private void AnimateFireLight()
        {
            if (fireLight == null) return;
            fireLight.intensity = 1.5f + Mathf.PerlinNoise(Time.time * 3f, 0f) * 1.5f;
            fireLight.color = Color.Lerp(new Color(1f, 0.4f, 0.1f), new Color(1f, 0.7f, 0.3f),
                Mathf.PerlinNoise(Time.time * 2f, 0.5f));
        }

        // ═══════════════════════════════════════════════════════
        // UI
        // ═══════════════════════════════════════════════════════

        void OnGUI()
        {
            if (!playerLooking) return;

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.fontSize = 20;
                titleStyle.fontStyle = FontStyle.Bold;
                titleStyle.alignment = TextAnchor.MiddleCenter;
                titleStyle.normal.textColor = new Color(1f, 0.7f, 0.2f);

                normalStyle = new GUIStyle(GUI.skin.label);
                normalStyle.fontSize = 16;
                normalStyle.alignment = TextAnchor.MiddleCenter;
                normalStyle.normal.textColor = Color.white;

                shadowStyle = new GUIStyle(normalStyle);
                shadowStyle.normal.textColor = Color.black;

                cookingStyle = new GUIStyle(normalStyle);
                cookingStyle.normal.textColor = new Color(1f, 0.8f, 0.3f);

                cookedStyle = new GUIStyle(normalStyle);
                cookedStyle.normal.textColor = new Color(0.3f, 1f, 0.3f);

                burntStyle = new GUIStyle(normalStyle);
                burntStyle.normal.textColor = new Color(0.6f, 0.3f, 0.1f);
            }

            float cx = Screen.width / 2;
            float cy = Screen.height / 2;
            float startY = cy + 50;

            // Tiêu đề
            GUI.Label(new Rect(cx - 152, startY - 1, 304, 30), "🔥 Lửa Trại", shadowStyle);
            GUI.Label(new Rect(cx - 150, startY, 300, 30), "🔥 Lửa Trại", titleStyle);
            startY += 30;

            // Hiển thị từng slot
            for (int i = 0; i < cookingSlots.Length; i++)
            {
                string slotText;
                GUIStyle style;

                if (!cookingSlots[i].hasItem)
                {
                    slotText = "Slot " + (i + 1) + ": [Trống]";
                    style = normalStyle;
                }
                else
                {
                    switch (cookingSlots[i].state)
                    {
                        case CookState.Cooking:
                            float remaining = cookTime - cookingSlots[i].cookTimer;
                            slotText = "Slot " + (i + 1) + ": 🥩 Đang nấu... (" + Mathf.CeilToInt(remaining) + "s)";
                            style = cookingStyle;
                            break;
                        case CookState.Cooked:
                            slotText = "Slot " + (i + 1) + ": 🍖 Thịt Chín! [E] để lấy";
                            style = cookedStyle;
                            break;
                        case CookState.Burnt:
                            slotText = "Slot " + (i + 1) + ": 💀 Thịt Cháy [E] để lấy";
                            style = burntStyle;
                            break;
                        default:
                            slotText = "Slot " + (i + 1) + ": ???";
                            style = normalStyle;
                            break;
                    }
                }

                GUI.Label(new Rect(cx - 200, startY, 400, 25), slotText, style);
                startY += 25;
            }

            // Hướng dẫn
            startY += 5;
            InventoryManager inv = InventoryManager.Instance;
            int rawCount = inv != null ? inv.GetItemCountByID("raw_meat") : 0;

            bool hasEmptySlot = false;
            bool hasCookedOrBurnt = false;
            for (int i = 0; i < cookingSlots.Length; i++)
            {
                if (!cookingSlots[i].hasItem) hasEmptySlot = true;
                if (cookingSlots[i].state == CookState.Cooked || cookingSlots[i].state == CookState.Burnt)
                    hasCookedOrBurnt = true;
            }

            if (hasCookedOrBurnt)
            {
                GUI.Label(new Rect(cx - 200, startY, 400, 25), "[E] Lấy thịt", normalStyle);
            }
            else if (hasEmptySlot && rawCount > 0)
            {
                GUI.Label(new Rect(cx - 200, startY, 400, 25), "[E] Đặt thịt sống lên nấu (" + rawCount + " miếng)", normalStyle);
            }
            else if (!hasEmptySlot)
            {
                GUI.Label(new Rect(cx - 200, startY, 400, 25), "Lửa trại đã đầy!", normalStyle);
            }
            else
            {
                GUI.Label(new Rect(cx - 200, startY, 400, 25), "Cần Thịt Sống để nấu (săn thú rừng)", normalStyle);
            }
        }

        private ItemData GetOrCreateItemData(string id, string name, string desc, ItemType type, bool stackable, int maxStack)
        {
            ItemData item = Resources.Load<ItemData>("Items/" + id);
            if (item != null) return item;
            item = Resources.Load<ItemData>(id);
            if (item != null) return item;

            item = ScriptableObject.CreateInstance<ItemData>();
            item.itemID = id;
            item.itemName = name;
            item.description = desc;
            item.itemType = type;
            item.isStackable = stackable;
            item.maxStack = maxStack;
            return item;
        }
    }
}

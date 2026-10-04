using UnityEngine;

namespace HorrorGame.Survival
{
    /// <summary>
    /// Hệ thống Rìu FPS — Gắn vào Camera (cùng vị trí với FPSWeapon).
    /// Phím 2 để rút/cất rìu. Click trái để chặt cây (ChoppableTree).
    /// Tự tạo model rìu placeholder nếu chưa có prefab.
    /// </summary>
    public class AxeController : MonoBehaviour
    {
        [Header("=== KÉO MODEL RÌU VÀO ĐÂY ===")]
        [Tooltip("Kéo thả model rìu 3D vào ô này. Nếu để trống sẽ tạo rìu tạm.")]
        public GameObject axeModelPrefab;

        [Header("=== HOẶC KÉO RÌU ĐÃ GẮN SẴN TỪ EDITOR ===")]
        [Tooltip("Nếu bạn đã tự gắn rìu vào xương tay trong Hierarchy, hãy kéo object đó vào đây. Code sẽ dùng nó luôn và không spawn đè.")]
        public GameObject attachedAxeVisual;

        [Header("Chế độ hiển thị")]
        [Tooltip("Nếu tích chọn, rìu luôn gắn vào Camera (FPS) để luôn nhìn thấy trước mặt. Bỏ chọn để gắn vào xương tay (cần có animation).")]
        public bool forceCameraAttachment = false;

        [Header("Vị trí rìu trên màn hình")]
        public Vector3 axePosition = new Vector3(0.4f, -0.3f, 0.5f);
        public Vector3 axeRotation = new Vector3(0, 180, 0);
        public Vector3 axeScale = new Vector3(0.35f, 0.35f, 0.35f);

        [Header("Tự động căn rìu thẳng như đang cầm (khuyên dùng)")]
        [Tooltip("Tự đo model rìu, dựng cán rìu hướng lên, lưỡi hướng về phía trước, tự chỉnh kích thước và gắn vào Camera. Khi bật, Axe Scale bị bỏ qua; Axe Rotation dùng để nghiêng thêm.")]
        public bool autoFitAxe = true;
        [Tooltip("Chiều dài cây rìu (mét). Tăng = rìu to hơn")]
        public float axeLength = 0.6f;
        [Tooltip("Tick nếu lưỡi rìu bị nằm dưới (rìu bị cầm ngược)")]
        public bool flipAxeUpsideDown = false;
        [Tooltip("Xoay rìu quanh cán: thử 90, 180, -90 nếu lưỡi không hướng về phía trước")]
        public float axeRoll = 0f;

        [Header("Thông số chặt cây")]
        public float chopDamage = 25f;      // Sát thương mỗi nhát chặt
        public float chopRange = 3f;        // Tầm chặt
        public float chopCooldown = 0.8f;   // Thời gian giữa mỗi nhát (giây)

        [Header("Tích hợp Túi Đồ")]
        [Tooltip("Kéo ItemData của Rìu vào đây. Nếu để trống sẽ tự load từ Resources.")]
        public HorrorGame.Inventory.ItemData axeItemData;

        [Header("Phím tắt")]
        public KeyCode equipKey = KeyCode.Alpha2; // Phím 2 để rút rìu

        [Header("Âm thanh")]
        public AudioClip chopSound;   // Tiếng chặt gỗ
        public AudioClip equipSound;  // Tiếng rút rìu

        // Private
        private GameObject axeInstance;
        private bool isEquipped = false;
        private float nextChopTime = 0f;
        private AudioSource audioSource;
        private bool isAttachedToHand = false; // true khi gắn vào xương tay

        // Procedural Animation Bones
        private Transform rightArmBone;
        private Transform rightForeArmBone;

        // Swing animation state
        private Vector3 currentAxePosition;
        private Vector3 currentAxeRotation;
        private bool isSwinging = false;
        private float swingTimer = 0f;
        private float swingDuration = 0.4f;

        // Helper: trả về vị trí/góc gốc tùy theo gắn vào tay hay camera
        private Vector3 BasePosition { get { return isAttachedToHand ? handAxePosition : axePosition; } }
        private Vector3 BaseRotation { get { return isAttachedToHand ? handAxeRotation : axeRotation; } }

        // UI
        private string displayText = "";
        private float displayTimer = 0f;
        private GUIStyle textStyle;
        private GUIStyle shadowStyle;

        private bool axeVisualCreated = false;

        void Start()
        {
            // Tự động load ItemData rìu từ Resources nếu chưa gán
            if (axeItemData == null)
            {
                axeItemData = Resources.Load<HorrorGame.Inventory.ItemData>("AxeItem");
                if (axeItemData == null)
                    axeItemData = Resources.Load<HorrorGame.Inventory.ItemData>("Items/Axe");
            }

            // Nếu vẫn không tìm thấy từ Resources, tạo runtime instance
            // QUAN TRỌNG: itemID phải là "axe" — trùng với GameStartSetup
            if (axeItemData == null)
            {
                axeItemData = ScriptableObject.CreateInstance<HorrorGame.Inventory.ItemData>();
                axeItemData.itemID = "axe";
                axeItemData.itemName = "Rìu";
                axeItemData.description = "Rìu sinh tồn. Dùng để chặt cây lấy gỗ.";
                axeItemData.itemType = HorrorGame.Inventory.ItemType.Tool;
                axeItemData.isStackable = false;
                axeItemData.maxStack = 1;
                Debug.Log("[AxeController] Tạo runtime AxeItemData (itemID='axe')");
            }
            else
            {
                Debug.Log("[AxeController] Đã load AxeItemData từ Resources, itemID='" + axeItemData.itemID + "'");
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            if (attachedAxeVisual != null && autoFitAxe && forceCameraAttachment)
            {
                // Dùng lại model rìu đã gắn tay, nhưng chuyển lên Camera và tự căn thẳng
                axeInstance = CreateAutoFitAxe(attachedAxeVisual);
                axeVisualCreated = true;
                isAttachedToHand = false;
                currentAxePosition = BasePosition;
                currentAxeRotation = BaseRotation;
                axeInstance.SetActive(false);
                Debug.Log("[AxeController] Đã chuyển rìu lên Camera và tự căn thẳng (auto-fit).");
            }
            else if (attachedAxeVisual != null)
            {
                // [THỦ CÔNG BẰNG ASSETS] - Bỏ qua sinh code, xài hàng thật bạn setup ở Editor
                axeInstance = attachedAxeVisual;
                axeVisualCreated = true;
                isAttachedToHand = true;
                
                // Lấy vị trí/góc đã setup bằng tay chuẩn trong Editor làm mốc (để lúc vung xong rìu lùi về chuẩn)
                handAxePosition = axeInstance.transform.localPosition;
                handAxeRotation = axeInstance.transform.localEulerAngles;
                
                currentAxePosition = BasePosition;
                currentAxeRotation = BaseRotation;
                
                // Dò tìm xương cánh tay để làm Procedural Animation
                rightHandBone = attachedAxeVisual.transform.parent;
                if (rightHandBone != null)
                {
                    rightForeArmBone = rightHandBone.parent;
                    if (rightForeArmBone != null) rightArmBone = rightForeArmBone.parent;
                }
                
                axeInstance.SetActive(false);
                Debug.Log("[AxeController] Đã sử dụng rìu gắn tay thủ công từ Editor.");
            }
            else
            {
                // Tìm xương tay phải của nhân vật để gắn rìu
                FindRightHand();

                // Thử tạo rìu ngay, nếu Camera.main chưa sẵn sàng sẽ thử lại trong Update
                TryCreateAxeVisual();

                currentAxePosition = BasePosition;
                currentAxeRotation = BaseRotation;
            }

            Debug.Log("=== HỆ THỐNG RÌU ĐÃ SẴN SÀNG! Bấm phím 2 để rút rìu ===");
        }

        void TryCreateAxeVisual()
        {
            if (axeVisualCreated) return;

            // Nếu có xương tay → không cần Camera.main
            // Nếu không có xương tay → cần Camera.main làm fallback
            if (rightHandBone == null)
            {
                Camera cam = Camera.main;
                if (cam == null)
                {
                    Debug.LogWarning("[AxeController] Chưa có rightHandBone và Camera.main cũng chưa sẵn sàng, sẽ thử lại...");
                    return;
                }
            }

            CreateAxeVisual();
            axeVisualCreated = true;

            // Ẩn rìu ban đầu
            if (axeInstance != null) axeInstance.SetActive(false);
        }

        private Transform rightHandBone;

        void FindRightHand()
        {
            // Tìm Animator trên player hoặc children
            Animator anim = GetComponent<Animator>();
            if (anim == null) anim = GetComponentInChildren<Animator>();

            if (anim != null && anim.isHuman)
            {
                rightHandBone = anim.GetBoneTransform(HumanBodyBones.RightHand);
                if (rightHandBone != null)
                {
                    Debug.Log("[AxeController] Tìm thấy xương tay phải: " + rightHandBone.name);
                    return;
                }
            }

            // Nếu không tìm được qua Animator, tìm bằng tên xương
            string[] handBoneNames = new string[] {
                "RightHand", "Right Hand", "Hand_R", "Hand.R", "hand_r",
                "mixamorig:RightHand", "Bip01 R Hand", "R Hand", "Right_Hand",
                "J_Bip_R_Hand", "right_hand", "RHand"
            };

            Transform[] allBones = GetComponentsInChildren<Transform>(true);
            foreach (Transform bone in allBones)
            {
                foreach (string name in handBoneNames)
                {
                    if (bone.name.Equals(name, System.StringComparison.OrdinalIgnoreCase) ||
                        bone.name.Contains("RightHand") || bone.name.Contains("Right Hand") ||
                        bone.name.Contains("Hand_R") || bone.name.Contains("hand_r"))
                    {
                        rightHandBone = bone;
                        Debug.Log("[AxeController] Tìm thấy xương tay phải (by name): " + bone.name);
                        return;
                    }
                }
            }

            // Fallback: gắn vào camera
            rightHandBone = null;
            Debug.LogWarning("[AxeController] Không tìm thấy xương tay phải! Rìu sẽ gắn vào Camera.");
        }

        [Header("Vị trí rìu khi gắn vào tay")]
        public Vector3 handAxePosition = new Vector3(0.05f, 0.1f, 0.05f); // Chỉnh cao hơn 1 chút
        public Vector3 handAxeRotation = new Vector3(0, 90, -90);
        public Vector3 handAxeScale = new Vector3(1.5f, 1.5f, 1.5f); // Tăng kích thước lên to để dễ nhìn

        void CreateAxeVisual()
        {
            // ƯU TIÊN gắn rìu vào xương tay phải của player (nếu không forceCameraAttachment)
            // Chỉ fallback sang Camera nếu không tìm thấy xương tay
            Transform parentBone;
            bool attachedToHand = false;

            if (!forceCameraAttachment && rightHandBone != null)
            {
                parentBone = rightHandBone;
                attachedToHand = true;
                isAttachedToHand = true; // Lưu trạng thái cho BasePosition/BaseRotation
                Debug.Log("[AxeController] Gắn rìu vào xương tay phải: " + rightHandBone.name);
            }
            else
            {
                Camera cam = Camera.main;
                parentBone = cam != null ? cam.transform : transform;
                isAttachedToHand = false;
                Debug.LogWarning("[AxeController] Đã gắn rìu vào Camera (ForceCamera = " + forceCameraAttachment + ")");
            }

            if (!attachedToHand && autoFitAxe)
            {
                GameObject model = axeModelPrefab != null ? Instantiate(axeModelPrefab) : CreatePlaceholderAxe();
                axeInstance = CreateAutoFitAxe(model);
                axeInstance.transform.localPosition = axePosition;
                axeInstance.transform.localRotation = Quaternion.Euler(axeRotation);
                Debug.Log("[AxeController] Đã tạo rìu (auto-fit) trên Camera");
                return;
            }

            if (axeModelPrefab != null)
            {
                axeInstance = Instantiate(axeModelPrefab, parentBone);
            }
            else
            {
                // Tạo rìu placeholder bằng Primitive
                axeInstance = CreatePlaceholderAxe();
                axeInstance.transform.SetParent(parentBone, false);
            }

            // Đặt vị trí tùy theo gắn vào tay hay camera
            if (attachedToHand)
            {
                axeInstance.transform.localPosition = handAxePosition;
                axeInstance.transform.localRotation = Quaternion.Euler(handAxeRotation);
                axeInstance.transform.localScale = handAxeScale;
            }
            else
            {
                // FPS camera style - hiển thị rìu trước mặt người chơi
                axeInstance.transform.localPosition = axePosition;
                axeInstance.transform.localRotation = Quaternion.Euler(axeRotation);
                axeInstance.transform.localScale = axeScale;
            }

            // Tắt Collider trên rìu
            Collider[] cols = axeInstance.GetComponentsInChildren<Collider>();
            foreach (Collider c in cols) c.enabled = false;

            Debug.Log("[AxeController] Đã tạo rìu, gắn vào: " + parentBone.name + " (hand=" + attachedToHand + ")");
        }

        /// <summary>
        /// Đưa model rìu vào 1 "AxeHolder" con của Camera: trục dài nhất (cán) hướng lên (Y),
        /// trục dài thứ hai (bề ngang lưỡi) hướng về phía trước (Z), rồi chỉnh kích thước và căn giữa.
        /// </summary>
        GameObject CreateAutoFitAxe(GameObject model)
        {
            // Ưu tiên gắn vào CÙNG camera với súng (Camera.main lúc Start có thể là camera cutscene)
            Transform camT = null;
            FPSWeapon fps = Object.FindObjectOfType<FPSWeapon>();
            if (fps != null) camT = fps.transform;
            if (camT == null)
            {
                Camera childCam = GetComponentInChildren<Camera>();
                if (childCam != null) camT = childCam.transform;
            }
            if (camT == null && Camera.main != null) camT = Camera.main.transform;
            if (camT == null) camT = transform;

            Camera cam = camT.GetComponent<Camera>();
            if (cam != null && cam.nearClipPlane > 0.05f) cam.nearClipPlane = 0.02f;

            GameObject holder = new GameObject("AxeHolder");
            holder.transform.SetParent(camT, false);
            holder.layer = camT.gameObject.layer;

            model.SetActive(true);
            model.transform.SetParent(holder.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            // Nếu model không có mesh nào -> dùng rìu tạm để chắc chắn nhìn thấy
            if (model.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                Debug.LogWarning("[AxeController] Model rìu không có Renderer nào! Dùng rìu tạm.");
                model.SetActive(false);
                model = CreatePlaceholderAxe();
                model.transform.SetParent(holder.transform, false);
            }

            // Bật toàn bộ con + renderer, đưa về layer mà camera nhìn thấy
            int visibleLayer = camT.gameObject.layer;
            if (cam != null && (cam.cullingMask & (1 << visibleLayer)) == 0) visibleLayer = 0;
            foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.SetActive(true);
                t.gameObject.layer = visibleLayer;
            }
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true)) r.enabled = true;

            Bounds b = FPSWeapon.GetLocalBounds(model, holder.transform);
            if (b.size != Vector3.zero)
            {
                Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
                float[] len = { b.size.x, b.size.y, b.size.z };
                int longest = 0;
                for (int i = 1; i < 3; i++) if (len[i] > len[longest]) longest = i;
                int second = -1;
                for (int i = 0; i < 3; i++)
                {
                    if (i == longest) continue;
                    if (second < 0 || len[i] > len[second]) second = i;
                }

                // R sao cho: R * cán = up, R * lưỡi = forward
                Quaternion align = Quaternion.Inverse(Quaternion.LookRotation(axes[second], axes[longest]));
                Quaternion fix = Quaternion.Euler(0f, 0f, flipAxeUpsideDown ? 180f : 0f) * Quaternion.Euler(0f, axeRoll, 0f);
                model.transform.localRotation = fix * align;

                b = FPSWeapon.GetLocalBounds(model, holder.transform);
                float k = axeLength / Mathf.Max(b.size.y, 0.0001f);
                model.transform.localScale = Vector3.one * k;

                b = FPSWeapon.GetLocalBounds(model, holder.transform);
                model.transform.localPosition = -b.center;
            }

            foreach (Collider c in model.GetComponentsInChildren<Collider>()) c.enabled = false;
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            holder.transform.localPosition = axePosition;
            holder.transform.localRotation = Quaternion.Euler(axeRotation);

            Bounds fb = FPSWeapon.GetLocalBounds(model, holder.transform);
            Debug.Log(string.Format("[AxeController] AxeHolder gắn vào '{0}', model='{1}', renderers={2}, kích thước={3}, layer={4}, vị trí={5}",
                camT.name, model.name, model.GetComponentsInChildren<Renderer>(true).Length, fb.size, visibleLayer, axePosition));
            return holder;
        }

        GameObject CreatePlaceholderAxe()
        {
            GameObject axe = new GameObject("PlaceholderAxe");

            // Cán rìu (Cylinder dài) - dài ~70cm
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.transform.SetParent(axe.transform);
            handle.transform.localPosition = new Vector3(0, 0, 0);
            handle.transform.localRotation = Quaternion.Euler(0, 0, 90);
            handle.transform.localScale = new Vector3(0.035f, 0.35f, 0.035f);
            SetMaterialColor(handle, new Color(0.45f, 0.3f, 0.15f)); // Nâu gỗ

            // Lưỡi rìu (Cube dẹp) - to và rõ ràng
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.transform.SetParent(axe.transform);
            blade.transform.localPosition = new Vector3(0.32f, 0, 0);
            blade.transform.localRotation = Quaternion.Euler(0, 0, 15);
            blade.transform.localScale = new Vector3(0.18f, 0.02f, 0.22f);
            SetMaterialColor(blade, new Color(0.7f, 0.7f, 0.72f)); // Bạc kim loại

            // Phần nêm kẹp lưỡi rìu vào cán
            GameObject wedge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wedge.transform.SetParent(axe.transform);
            wedge.transform.localPosition = new Vector3(0.24f, 0, 0);
            wedge.transform.localScale = new Vector3(0.07f, 0.06f, 0.09f);
            SetMaterialColor(wedge, new Color(0.5f, 0.5f, 0.52f)); // Xám

            return axe;
        }

        void SetMaterialColor(GameObject obj, Color color)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                r.sharedMaterial = mat;
            }
        }

        void Update()
        {
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive)
            {
                if (axeInstance != null && axeInstance.activeSelf) axeInstance.SetActive(false);
                isEquipped = false;
                return;
            }

            // Nếu chưa tạo được rìu (Camera.main chưa sẵn sàng ở Start), thử lại
            if (!axeVisualCreated)
            {
                TryCreateAxeVisual();
            }

            // Không nhận input khi túi đồ đang mở
            if (Inventory.InventoryManager.Instance != null && Inventory.InventoryManager.Instance.isInventoryOpen) return;

            // Phím 2 để rút/cất rìu
            if (Input.GetKeyDown(equipKey))
            {
                Debug.Log("[AxeController] Phím 2 được nhấn! axeInstance=" + (axeInstance != null) + ", axeVisualCreated=" + axeVisualCreated);
                TryToggleAxe();
            }

            if (!isEquipped) return;

            // Click trái để chặt
            if (Input.GetMouseButtonDown(0) && Time.time >= nextChopTime && !isSwinging)
            {
                nextChopTime = Time.time + chopCooldown;
                PerformChop();
            }

            // Animation vung rìu
            UpdateSwingAnimation();

            // Giảm timer hiển thị text
            if (displayTimer > 0) displayTimer -= Time.deltaTime;
        }

        void TryToggleAxe()
        {
            // Kiểm tra có rìu trong inventory không
            if (axeItemData != null && Inventory.InventoryManager.Instance != null)
            {
                Debug.Log("[AxeController] Kiểm tra inventory... axeItemData.itemID='" + axeItemData.itemID + "'");

                // Debug: in ra inventory hiện tại
                Inventory.InventoryManager.Instance.DebugLogAllItems();

                if (Inventory.InventoryManager.Instance.HasItem(axeItemData))
                {
                    Debug.Log("[AxeController] Tìm thấy rìu trong inventory! Đang toggle...");
                    ToggleAxe();
                }
                else
                {
                    displayText = "Không có rìu trong túi!";
                    displayTimer = 2f;
                    Debug.LogWarning("[AxeController] Không có rìu trong túi! (itemID='" + axeItemData.itemID + "')");
                }
            }
            else
            {
                // Nếu chưa liên kết inventory, cho rút tự do
                Debug.Log("[AxeController] Không có inventory system, rút rìu tự do");
                ToggleAxe();
            }
        }

        void ToggleAxe()
        {
            isEquipped = !isEquipped;

            if (axeInstance != null) axeInstance.SetActive(isEquipped);

            // Khi rút rìu, cất súng (nếu có FPSWeapon)
            if (isEquipped)
            {
                // Tìm FPSWeapon trên cùng object hoặc parent/children
                FPSWeapon fpsWeapon = GetComponent<FPSWeapon>();
                if (fpsWeapon == null) fpsWeapon = GetComponentInChildren<FPSWeapon>();
                if (fpsWeapon == null) fpsWeapon = GetComponentInParent<FPSWeapon>();
                if (fpsWeapon == null) fpsWeapon = Object.FindObjectOfType<FPSWeapon>();

                if (fpsWeapon != null && fpsWeapon.IsEquipped)
                {
                    fpsWeapon.ToggleWeapon(); // Cất súng đi
                    Debug.Log("[AxeController] Đã cất súng FPSWeapon khi rút rìu");
                }

                // Cất súng qua WeaponManager (nếu có)
                var weaponMgr = Object.FindObjectOfType<HorrorGame.Player.WeaponManager>();
                if (weaponMgr != null && weaponMgr.IsArmed())
                {
                    // WeaponManager sẽ tự cất khi không nhận Alpha2
                }

                displayText = "Rìu";
                displayTimer = 2f;

                if (equipSound != null) audioSource.PlayOneShot(equipSound);

                Debug.Log("[AxeController] Đã rút rìu");
            }
            else
            {
                displayText = "Cất rìu";
                displayTimer = 2f;
                Debug.Log("[AxeController] Đã cất rìu");
            }

            // Reset animation
            currentAxePosition = BasePosition;
            currentAxeRotation = BaseRotation;
            isSwinging = false;
        }

        void PerformChop()
        {
            // Bắt đầu animation vung rìu
            isSwinging = true;
            swingTimer = 0f;

            // Phát âm thanh chặt
            if (chopSound != null) audioSource.PlayOneShot(chopSound);

            // Raycast kiểm tra có trúng cây không
            RaycastHit hit;
            Ray ray = new Ray(transform.position, transform.forward);

            if (Physics.Raycast(ray, out hit, chopRange))
            {
                // Kiểm tra có phải ChoppableTree không
                ChoppableTree tree = hit.transform.GetComponent<ChoppableTree>();
                if (tree == null) tree = hit.transform.GetComponentInParent<ChoppableTree>();

                if (tree != null)
                {
                    tree.TakeChopDamage(chopDamage, hit.point);
                    Debug.Log(string.Format("[AxeController] Chặt trúng cây! Damage: {0}, HP còn: {1}", chopDamage, tree.CurrentHealth));
                }
                else
                {
                    // Kiểm tra có phải thú rừng không
                    AnimalAI animal = hit.transform.GetComponent<AnimalAI>();
                    if (animal == null) animal = hit.transform.GetComponentInParent<AnimalAI>();

                    if (animal != null)
                    {
                        animal.TakeDamage(chopDamage);
                        displayText = "Đánh trúng " + animal.animalType + "!";
                        displayTimer = 2f;
                    }
                    else
                    {
                        Debug.Log("[AxeController] Chặt trúng: " + hit.transform.name + " (không phải cây/thú)");
                    }
                }
            }
        }

        void LateUpdate()
        {
            if (!isEquipped || !isSwinging || !isAttachedToHand || axeInstance == null || forceCameraAttachment) return;

            // Procedural Arm Animation: Can thiệp vào Animator để vung cánh tay
            if (rightForeArmBone != null && rightArmBone != null)
            {
                float t = swingTimer / swingDuration;
                float armLift = 0f;
                float elbowBend = 0f;

                if (t < 0.4f)
                {
                    float phase1 = t / 0.4f;
                    armLift = Mathf.Lerp(0, -35f, phase1); // Vung bắp tay lên
                    elbowBend = Mathf.Lerp(0, -25f, phase1); // Co khuỷu tay lại
                }
                else if (t < 0.7f)
                {
                    float phase2 = (t - 0.4f) / 0.3f;
                    armLift = Mathf.Lerp(-35f, 45f, phase2); // Bổ mạnh bắp tay xuống
                    elbowBend = Mathf.Lerp(-25f, 15f, phase2); // Duỗi thẳng khuỷu tay
                }
                else
                {
                    float phase3 = (t - 0.7f) / 0.3f;
                    armLift = Mathf.Lerp(45f, 0f, phase3); // Thu bắp tay về
                    elbowBend = Mathf.Lerp(15f, 0f, phase3); // Thu khuỷu tay về
                }

                Camera cam = Camera.main;
                if (cam != null)
                {
                    // Dùng RotateAround theo trục ngang (Right) của Camera 
                    // Để đảm bảo cánh tay luôn chém dọc từ trên xuống màn hình, bất chấp trục xoay của xương tay bị lệch
                    rightArmBone.RotateAround(rightArmBone.position, cam.transform.right, armLift);
                    rightForeArmBone.RotateAround(rightForeArmBone.position, cam.transform.right, elbowBend);
                }
            }
        }

        void UpdateSwingAnimation()
        {
            if (axeInstance == null) return;

            Vector3 basePos = BasePosition;
            Vector3 baseRot = BaseRotation;

            if (isSwinging)
            {
                swingTimer += Time.deltaTime;
                float t = swingTimer / swingDuration;

                if (t < 0.4f)
                {
                    // Pha 1: Vung rìu lên (giơ cao)
                    float phase1 = t / 0.4f;
                    currentAxeRotation = Vector3.Lerp(baseRot, baseRot + new Vector3(-45f, 0, 20f), phase1);
                    currentAxePosition = Vector3.Lerp(basePos, basePos + new Vector3(0, 0.15f, -0.1f), phase1);
                }
                else if (t < 0.7f)
                {
                    // Pha 2: Chém xuống mạnh
                    float phase2 = (t - 0.4f) / 0.3f;
                    currentAxeRotation = Vector3.Lerp(baseRot + new Vector3(-45f, 0, 20f), baseRot + new Vector3(60f, 0, -10f), phase2);
                    currentAxePosition = Vector3.Lerp(basePos + new Vector3(0, 0.15f, -0.1f), basePos + new Vector3(0, -0.2f, 0.1f), phase2);
                }
                else
                {
                    // Pha 3: Hồi lại vị trí ban đầu
                    float phase3 = (t - 0.7f) / 0.3f;
                    currentAxeRotation = Vector3.Lerp(baseRot + new Vector3(60f, 0, -10f), baseRot, phase3);
                    currentAxePosition = Vector3.Lerp(basePos + new Vector3(0, -0.2f, 0.1f), basePos, phase3);
                }

                if (t >= 1f)
                {
                    isSwinging = false;
                    currentAxeRotation = baseRot;
                    currentAxePosition = basePos;
                }
            }
            else
            {
                // Smooth lerp về vị trí gốc
                currentAxePosition = Vector3.Lerp(currentAxePosition, basePos, Time.deltaTime * 8f);
                currentAxeRotation = Vector3.Lerp(currentAxeRotation, baseRot, Time.deltaTime * 8f);
            }

            axeInstance.transform.localPosition = currentAxePosition;
            axeInstance.transform.localRotation = Quaternion.Euler(currentAxeRotation);
        }

        // === UI: Hiển thị tên công cụ và tâm ngắm khi trang bị ===
        void OnGUI()
        {
            if (!isEquipped) return;

            // Tâm ngắm nhỏ (dot)
            float cx = Screen.width / 2f;
            float cy = Screen.height / 2f;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(cx - 2, cy - 2, 4, 4), Texture2D.whiteTexture);

            // Hiển thị tên rìu
            if (displayTimer > 0)
            {
                if (textStyle == null)
                {
                    textStyle = new GUIStyle();
                    textStyle.fontSize = 24;
                    textStyle.fontStyle = FontStyle.Bold;
                    textStyle.normal.textColor = Color.white;
                    textStyle.alignment = TextAnchor.MiddleCenter;

                    shadowStyle = new GUIStyle(textStyle);
                    shadowStyle.normal.textColor = Color.black;
                }

                float alpha = Mathf.Clamp01(displayTimer);
                textStyle.normal.textColor = new Color(1, 1, 1, alpha);
                shadowStyle.normal.textColor = new Color(0, 0, 0, alpha);

                float tx = cx - 150f;
                float ty = Screen.height - 130f;

                GUI.Label(new Rect(tx + 2, ty + 2, 300, 40), displayText, shadowStyle);
                GUI.Label(new Rect(tx, ty, 300, 40), displayText, textStyle);
            }
        }

        /// <summary>
        /// Kiểm tra rìu có đang được trang bị không (dùng bởi BuildingSystem để tránh xung đột)
        /// </summary>
        public bool IsEquipped { get { return isEquipped; } }

        /// <summary>
        /// Cất rìu (gọi từ bên ngoài, ví dụ khi vào build mode)
        /// </summary>
        public void ForceUnequip()
        {
            if (isEquipped) ToggleAxe();
        }
    }
}

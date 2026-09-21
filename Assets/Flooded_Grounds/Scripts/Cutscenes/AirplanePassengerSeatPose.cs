using UnityEngine;

namespace HorrorGame.Cutscenes
{
    /// <summary>
    /// Định hình tư thế ngồi ngay ngắn, tự nhiên 100% cho nhân vật (Humanoid / Mixamo rig)
    /// trên ghế hành khách máy bay (Seat 12A sát cửa sổ).
    /// </summary>
    [ExecuteInEditMode]
    public class AirplanePassengerSeatPose : MonoBehaviour
    {
        [Header("── Trạng Thái ──")]
        public bool isSeated = true;
        public Transform seatAnchor;

        [Header("── Góc Khớp Ngồi (Mixamo Euler) ──")]
        [Range(40f, 95f)]  public float thighPitch = 75f;
        [Range(40f, 105f)] public float kneePitch = 80f;
        [Range(-30f, 30f)] public float footPitch = -10f;
        [Range(-25f, 5f)]  public float spineRecline = -6f;
        public bool lookAtWindow = true;

        [Header("── Hoạt Hình (Procedural Animation) ──")]
        public bool enableBreathing = true;
        [Range(0.5f, 5f)] public float breatheSpeed = 2.0f;
        [Range(0.1f, 3f)] public float breatheMagnitude = 1.5f;

        [Header("── Tư Thế Cánh Tay Tự Nhiên ──")]
        public bool hasCustomArmPose = false;
        public Quaternion leftArmRot;
        public Quaternion rightArmRot;
        public Quaternion leftForeArmRot;
        public Quaternion rightForeArmRot;
        public Quaternion leftHandRot;
        public Quaternion rightHandRot;

        [HideInInspector] public Transform hips;
        [HideInInspector] public Transform leftUpLeg, rightUpLeg;
        [HideInInspector] public Transform leftLeg, rightLeg;
        [HideInInspector] public Transform leftFoot, rightFoot;
        [HideInInspector] public Transform leftToe, rightToe;
        [HideInInspector] public Transform spine, spine1, spine2;
        [HideInInspector] public Transform neck, head;
        [HideInInspector] public Transform leftArm, rightArm;
        [HideInInspector] public Transform leftForeArm, rightForeArm;
        [HideInInspector] public Transform leftHand, rightHand;

        private bool bonesCached = false;

        public void CaptureArmPoseFromCurrent()
        {
            if (leftArm != null) leftArmRot = leftArm.localRotation;
            if (rightArm != null) rightArmRot = rightArm.localRotation;
            if (leftForeArm != null) leftForeArmRot = leftForeArm.localRotation;
            if (rightForeArm != null) rightForeArmRot = rightForeArm.localRotation;
            if (leftHand != null) leftHandRot = leftHand.localRotation;
            if (rightHand != null) rightHandRot = rightHand.localRotation;
            hasCustomArmPose = true;
        }

        private void OnEnable()
        {
            EnsureMeshVisible();
            CacheBones();
            ApplyPose();
        }

        private void Start()
        {
            EnsureMeshVisible();
            CacheBones();
            ApplyPose();
        }

        private void Update()
        {
            if (isSeated)
            {
                ApplyPose();
            }
        }

        private void LateUpdate()
        {
            if (isSeated)
            {
                ApplyPose();
            }
        }

        public void EnsureMeshVisible()
        {
            gameObject.SetActive(true);
            Transform[] all = GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                if (t == null) continue;
                // Tuyệt đối không kích hoạt các vũ khí (Gun, Weapon, Axe, Placeholder) khi ngồi trên ghế
                if (t.name.Contains("Gun") || t.name.Contains("Weapon") || t.name.Contains("Axe") || t.name.Contains("Placeholder"))
                {
                    t.gameObject.SetActive(false);
                    continue;
                }
                t.gameObject.SetActive(true);
            }

            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null && isSeated)
            {
                anim.enabled = false;
            }

            // Tắt vĩnh viễn MeshRenderer dạng Capsule tạm bợ trên chính root Player
            MeshRenderer rootMr = GetComponent<MeshRenderer>();
            if (rootMr != null)
            {
                rootMr.enabled = false;
            }

            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                // Không bao giờ bật MeshRenderer của root Player (đó là capsule placeholder)
                if (r.gameObject == gameObject)
                {
                    r.enabled = false;
                    continue;
                }

                r.enabled = true;
                SkinnedMeshRenderer smr = r as SkinnedMeshRenderer;
                if (smr != null)
                {
                    smr.updateWhenOffscreen = true; // Không bao giờ bị culling
                }
            }
        }

        public void CacheBones()
        {
            Transform[] all = GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                string n = t.name;
                if (n.EndsWith("Hips")) hips = t;
                else if (n.EndsWith("LeftUpLeg")) leftUpLeg = t;
                else if (n.EndsWith("RightUpLeg")) rightUpLeg = t;
                else if (n.EndsWith("LeftLeg")) leftLeg = t;
                else if (n.EndsWith("RightLeg")) rightLeg = t;
                else if (n.EndsWith("LeftFoot")) leftFoot = t;
                else if (n.EndsWith("RightFoot")) rightFoot = t;
                else if (n.EndsWith("LeftToeBase") || n.EndsWith("LeftToe_End")) leftToe = t;
                else if (n.EndsWith("RightToeBase") || n.EndsWith("RightToe_End")) rightToe = t;
                else if (n.EndsWith("Spine")) spine = t;
                else if (n.EndsWith("Spine1")) spine1 = t;
                else if (n.EndsWith("Spine2")) spine2 = t;
                else if (n.EndsWith("Neck")) neck = t;
                else if (n.EndsWith("Head")) head = t;
                else if (n.EndsWith("LeftArm")) leftArm = t;
                else if (n.EndsWith("RightArm")) rightArm = t;
                else if (n.EndsWith("LeftForeArm")) leftForeArm = t;
                else if (n.EndsWith("RightForeArm")) rightForeArm = t;
                else if (n.EndsWith("LeftHand")) leftHand = t;
                else if (n.EndsWith("RightHand")) rightHand = t;
            }

            bonesCached = (hips != null && leftUpLeg != null && rightUpLeg != null);
        }

        /// <summary>
        /// Áp dụng tư thế ngồi chuẩn xác, ổn định 100%
        /// </summary>
        public void ApplyPose()
        {
            if (!bonesCached) CacheBones();
            if (hips == null) return;

            if (isSeated)
            {
                Animator anim = GetComponentInChildren<Animator>();
                if (anim != null && anim.enabled) anim.enabled = false;
            }

            // 1. ĐÙI: Gập ngang ra trước nằm trên mặt đệm ghế (+X 75 độ)
            if (leftUpLeg != null)  leftUpLeg.localRotation  = Quaternion.Euler(thighPitch, -3f, 0f);
            if (rightUpLeg != null) rightUpLeg.localRotation = Quaternion.Euler(thighPitch, 3f, 0f);

            // 2. ĐẦU GỐI: Gập góc 80 độ buông xuống sàn cabin (-X 80 độ)
            if (leftLeg != null)  leftLeg.localRotation  = Quaternion.Euler(kneePitch, 0f, 0f);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(kneePitch, 0f, 0f);

            // 3. BÀN CHÂN: Đặt phẳng trên mặt sàn (+X 12 độ)
            if (leftFoot != null)  leftFoot.localRotation  = Quaternion.Euler(footPitch, 0f, 0f);
            if (rightFoot != null) rightFoot.localRotation = Quaternion.Euler(footPitch, 0f, 0f);

            // 4. CỘT SỐNG: Tựa nhẹ nhàng vào lưng ghế ngả 6 độ
            if (spine != null)  spine.localRotation  = Quaternion.Euler(spineRecline, 0f, 0f);
            if (spine1 != null) spine1.localRotation = Quaternion.Euler(spineRecline * 0.4f, 0f, 0f);
            if (spine2 != null) spine2.localRotation = Quaternion.identity;

            // 5. HAI CÁNH TAY: Sử dụng tư thế tự nhiên chuẩn Mocap
            if (hasCustomArmPose)
            {
                if (leftArm != null)      leftArm.localRotation      = leftArmRot;
                if (rightArm != null)     rightArm.localRotation     = rightArmRot;
                if (leftForeArm != null)  leftForeArm.localRotation  = leftForeArmRot;
                if (rightForeArm != null) rightForeArm.localRotation = rightForeArmRot;
                if (leftHand != null)     leftHand.localRotation     = leftHandRot;
                if (rightHand != null)    rightHand.localRotation    = rightHandRot;
            }
            else
            {
                // Mặc định đặt tay lên đùi để không bị giơ lên trời (T-Pose)
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(70f, -10f, 0f);
                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(70f, 10f, 0f);
                if (leftForeArm != null) leftForeArm.localRotation = Quaternion.Euler(25f, 0f, 0f);
                if (rightForeArm != null) rightForeArm.localRotation = Quaternion.Euler(25f, 0f, 0f);
                if (leftHand != null) leftHand.localRotation = Quaternion.Euler(0f, 0f, 0f);
                if (rightHand != null) rightHand.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }

            // 6. ĐẦU: Hơi nghiêng sang trái nhìn cửa sổ
            if (head != null)
            {
                head.localRotation = lookAtWindow ? Quaternion.Euler(0f, -28f, 0f) : Quaternion.identity;
            }

            // 7. HOẠT HÌNH THỞ TỰ NHIÊN (Procedural Animation)
            if (enableBreathing && Application.isPlaying)
            {
                float t = Time.time * breatheSpeed;
                float breathSpine = Mathf.Sin(t) * breatheMagnitude;
                float breathChest = Mathf.Sin(t + 0.5f) * (breatheMagnitude * 1.5f);
                float breathArm = Mathf.Cos(t) * (breatheMagnitude * 0.5f);

                if (spine != null) spine.localRotation *= Quaternion.Euler(breathSpine, 0f, 0f);
                if (spine1 != null) spine1.localRotation *= Quaternion.Euler(breathChest, 0f, 0f);
                if (spine2 != null) spine2.localRotation *= Quaternion.Euler(breathChest * 0.5f, 0f, 0f);

                if (leftArm != null) leftArm.localRotation *= Quaternion.Euler(0f, 0f, breathArm);
                if (rightArm != null) rightArm.localRotation *= Quaternion.Euler(0f, 0f, -breathArm);
                
                if (head != null) head.localRotation *= Quaternion.Euler(-breathSpine * 0.5f, 0f, 0f);
            }
        }

        public void StandUp()
        {
            isSeated = false;
            
            // Xóa dáng ngồi của đùi/gối/chân/lưng để đứng thẳng
            if (leftUpLeg != null) leftUpLeg.localRotation = Quaternion.identity;
            if (rightUpLeg != null) rightUpLeg.localRotation = Quaternion.identity;
            if (leftLeg != null) leftLeg.localRotation = Quaternion.identity;
            if (rightLeg != null) rightLeg.localRotation = Quaternion.identity;
            if (leftFoot != null) leftFoot.localRotation = Quaternion.identity;
            if (rightFoot != null) rightFoot.localRotation = Quaternion.identity;
            if (spine != null) spine.localRotation = Quaternion.identity;
            if (spine1 != null) spine1.localRotation = Quaternion.identity;
            if (head != null) head.localRotation = Quaternion.identity;

            // Đặt tay xuôi tự nhiên xuống hai bên hông
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(70f, 0f, 0f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(70f, 0f, 0f);
            if (leftForeArm != null) leftForeArm.localRotation = Quaternion.identity;
            if (rightForeArm != null) rightForeArm.localRotation = Quaternion.identity;
            
            // Tắt Animator để giữ nguyên tư thế đứng tự nhiên này, tránh bị reset về T-Pose
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null) anim.enabled = false;
            
            enabled = false;
        }
    }
}

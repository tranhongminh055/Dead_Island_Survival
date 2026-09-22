using UnityEngine;

namespace HorrorGame.Player
{
    /// <summary>
    /// Điều khiển Đèn pin chiến thuật: Bật/tắt bằng phím F hoặc từ Túi đồ.
    /// Tự động tạo Spotlight chiếu xa góc nhìn thứ nhất.
    /// </summary>
    public class PlayerFlashlight : MonoBehaviour
    {
        public static PlayerFlashlight Instance;

        public Light flashlightLight;
        public bool hasFlashlight = false;
        public bool isOn = false;
        public AudioClip toggleSound;

        private AudioSource audioSource;
        private GUIStyle promptStyle;
        private float noticeTimer = 0f;
        private string noticeMessage = "";

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
        }

        private void Start()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;

            // Tự động tạo nguồn sáng Đèn pin nếu chưa gán
            if (flashlightLight == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                Transform targetTransform = cam != null ? cam.transform : transform;

                GameObject flObj = new GameObject("TacticalFlashlight_Beam");
                flObj.transform.SetParent(targetTransform, false);
                flObj.transform.localPosition = new Vector3(0.25f, -0.15f, 0.2f);
                flObj.transform.localRotation = Quaternion.identity;

                flashlightLight = flObj.AddComponent<Light>();
                flashlightLight.type = LightType.Spot;
                flashlightLight.range = 40f;
                flashlightLight.spotAngle = 50f;
                flashlightLight.color = new Color(0.96f, 0.98f, 1.0f); // Ánh sáng LED trắng dịu
                flashlightLight.intensity = 2.8f;
                flashlightLight.shadows = LightShadows.Soft;
                flashlightLight.enabled = false;
            }
        }

        private void Update()
        {
            if (noticeTimer > 0f)
                noticeTimer -= Time.unscaledDeltaTime;

            // Bấm phím F để bật/tắt đèn pin
            if (Input.GetKeyDown(KeyCode.F))
            {
                ToggleFlashlight();
            }
        }

        public void ToggleFlashlight()
        {
            if (!hasFlashlight)
            {
                ShowNotice("Bạn chưa có Đèn pin! Hãy đi tìm nhặt trên đảo.");
                return;
            }

            isOn = !isOn;
            if (flashlightLight != null)
                flashlightLight.enabled = isOn;

            if (toggleSound != null && audioSource != null)
                audioSource.PlayOneShot(toggleSound, 0.8f);

            ShowNotice(isOn ? "🔦 Đèn pin: BẬT" : "🔦 Đèn pin: TẮT");
        }

        public void ShowNotice(string msg)
        {
            noticeMessage = msg;
            noticeTimer = 2.5f;
        }

        private void OnGUI()
        {
            if (noticeTimer > 0f && !string.IsNullOrEmpty(noticeMessage))
            {
                if (promptStyle == null)
                {
                    promptStyle = new GUIStyle(GUI.skin.box);
                    promptStyle.fontSize = 18;
                    promptStyle.fontStyle = FontStyle.Bold;
                    promptStyle.alignment = TextAnchor.MiddleCenter;
                    promptStyle.normal.textColor = Color.yellow;
                }

                float width = 380f;
                float height = 36f;
                float x = (Screen.width - width) * 0.5f;
                float y = Screen.height * 0.72f;

                GUI.Box(new Rect(x, y, width, height), noticeMessage, promptStyle);
            }
        }
    }
}

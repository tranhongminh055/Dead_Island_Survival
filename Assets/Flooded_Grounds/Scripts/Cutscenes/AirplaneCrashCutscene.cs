using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using HorrorGame.Player;
using HorrorGame.Environment;

namespace HorrorGame.Cutscenes
{
    public class AirplaneCrashCutscene : MonoBehaviour
    {
        [Header("Setup Chung")]
        public Image blackScreenUI;
        public PlayerController playerController;
        public MonoBehaviour playerWeapon;
        public CameraShake cameraShake;

        [Header("Môi trường Máy Bay 3D")]
        [Tooltip("Kéo cụm Model Máy Bay (chứa thân vỏ, ghế) vào đây")]
        public GameObject airplaneCabin;
        [Tooltip("Kéo các bóng đèn Point Light màu đỏ trên máy bay vào đây để làm chớp nháy")]
        public Light[] warningLights;
        [Tooltip("Kéo vị trí bắt đầu dưới mặt đất khu rừng (nơi nhân vật sẽ thức dậy)")]
        public Transform forestSpawnPoint;

        [Header("Âm thanh")]
        public AudioSource audioSource;
        public AudioClip airplaneFlightClip; // Tiếng bay bình thường
        public AudioClip airplaneAlarmClip;  // Tiếng còi báo động réo
        public AudioClip planeCrashClip;     // Tiếng đâm sầm nổ lớn

        [Header("Kịch bản Thời Gian (Giây)")]
        public float flyingDuration = 5f;       // Thời gian ngồi bay bình thường
        public float turbulenceDuration = 4f;   // Thời gian xóc nảy, chớp đèn báo động
        
        [Tooltip("Thời gian nằm bất tỉnh trên màn hình đen. Bạn ghi 1 phút thì để 60, nhưng khuyên dùng 5-10s để game thủ không tưởng game bị treo!")]
        public float unconsciousDuration = 10f; 
        
        public float wakeUpFadeDuration = 5f;   // Thời gian từ từ mở mắt

        private void Start()
        {
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            // Bắt đầu nhắm mắt
            if (blackScreenUI != null)
            {
                blackScreenUI.color = new Color(0, 0, 0, 1f);
                blackScreenUI.gameObject.SetActive(true);
            }

            // Khoá người chơi
            if (playerController != null) playerController.enabled = false;
            if (playerWeapon != null) playerWeapon.enabled = false;

            StartCoroutine(PlayCutscene());
        }

        private IEnumerator PlayCutscene()
        {
            // === 1. MỞ MẮT TRÊN MÁY BAY ===
            yield return new WaitForSeconds(1f); // Chờ game load xong

            // Mở mắt (Fade In) để thấy cảnh ngồi trên máy bay
            float fade = 0f;
            while (fade < 2f)
            {
                fade += Time.deltaTime;
                blackScreenUI.color = new Color(0, 0, 0, Mathf.Lerp(1f, 0f, fade / 2f));
                yield return null;
            }

            // Tiếng máy bay bay bình thường
            if (airplaneFlightClip != null)
            {
                audioSource.clip = airplaneFlightClip;
                audioSource.loop = true;
                audioSource.Play();
            }

            // Nhìn xung quanh máy bay trong vài giây
            yield return new WaitForSeconds(flyingDuration);

            // === 2. SỰ CỐ & BÁO ĐỘNG ĐỎ ===
            // Đổi âm thanh báo động
            if (airplaneAlarmClip != null)
            {
                audioSource.PlayOneShot(airplaneAlarmClip);
            }
            
            // Bắt đầu rung lắc
            if (cameraShake != null)
            {
                cameraShake.StartShake(turbulenceDuration, 0.4f, 2f);
            }

            // Bật hiệu ứng đèn đỏ chớp tắt
            float tTime = 0;
            bool lightOn = false;
            while (tTime < turbulenceDuration)
            {
                tTime += Time.deltaTime;
                
                // Cứ mỗi 0.2s chớp đèn 1 lần
                if (Mathf.FloorToInt(tTime * 5f) % 2 == 0 != lightOn)
                {
                    lightOn = !lightOn;
                    foreach (Light l in warningLights)
                    {
                        if (l != null) l.intensity = lightOn ? 3f : 0f;
                    }
                }
                yield return null;
            }

            // === 3. VA CHẠM (CRASH) ===
            audioSource.Stop();
            if (planeCrashClip != null)
            {
                audioSource.PlayOneShot(planeCrashClip);
            }

            if (cameraShake != null)
            {
                cameraShake.StartShake(1.5f, 3f, 4f); // Rung cực mạnh
            }

            // Chờ chưa tới nửa giây rồi TỐI SẦM MÀN HÌNH lại ngay lập tức (giống The Forest)
            yield return new WaitForSeconds(0.3f);
            blackScreenUI.color = new Color(0, 0, 0, 1f);

            // === 4. DỊCH CHUYỂN & DỌN DẸP MÁY BAY ===
            // Tắt máy bay đi để đỡ nặng game
            if (airplaneCabin != null) airplaneCabin.SetActive(false);

            // Teleport người chơi xuống khu rừng (vị trí Spawn)
            if (playerController != null && forestSpawnPoint != null)
            {
                playerController.transform.position = forestSpawnPoint.position;
                playerController.transform.rotation = forestSpawnPoint.rotation;
                
                // Đồng bộ góc nhìn Camera cho đúng hướng ngã
                playerController.GetComponentInChildren<Camera>().transform.localRotation = Quaternion.Euler(60f, 0, 0); // Giả bộ đang nằm sấp nhìn xuống đất
            }

            // Tua nhanh thời gian trong game đi vài tiếng (Ví dụ: 1 phút ngoài đời = rơi lúc 6h sáng, dậy lúc 10h trưa)
            if (DayNightCycle.Instance != null)
            {
                // Cộng thêm 3 giờ trong game
                DayNightCycle.Instance.currentHour += 3f; 
            }

            // === 5. BẤT TỈNH NHÂN SỰ ===
            // Chờ đúng thời gian bất tỉnh bạn set (1 phút = 60s)
            yield return new WaitForSeconds(unconsciousDuration);

            // === 6. TỈNH DẬY (WAKE UP) ===
            float wakeTime = 0f;
            while (wakeTime < wakeUpFadeDuration)
            {
                wakeTime += Time.deltaTime;
                
                // Từ từ từ từ mở mắt, kèm theo hiệu ứng nhấp nháy 1-2 lần giống chớp mắt
                float alpha = Mathf.Lerp(1f, 0f, wakeTime / wakeUpFadeDuration);
                blackScreenUI.color = new Color(0, 0, 0, alpha);

                // Trả camera về góc nhìn thẳng đứng
                if (playerController != null)
                {
                    Camera cam = playerController.GetComponentInChildren<Camera>();
                    cam.transform.localRotation = Quaternion.Slerp(cam.transform.localRotation, Quaternion.identity, Time.deltaTime * 0.5f);
                }

                yield return null;
            }
            blackScreenUI.gameObject.SetActive(false);

            // TRẢ LẠI ĐIỀU KHIỂN
            if (playerController != null) playerController.enabled = true;
            if (playerWeapon != null) playerWeapon.enabled = true;

            Destroy(gameObject);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using HorrorGame.Player; // Dùng để truy cập PlayerController nếu cần

namespace HorrorGame.Cutscenes
{
    public class IntroCutsceneManager : MonoBehaviour
    {
        [Header("UI Màn hình đen")]
        [Tooltip("Kéo thẻ Image màu đen toàn màn hình vào đây")]
        public Image blackScreenUI;
        
        [Header("Player Settings")]
        [Tooltip("Kéo Player (chứa PlayerController) vào đây")]
        public PlayerController playerController;
        [Tooltip("Kéo Gun hoặc FPSWeapon vào đây để tắt súng lúc bắt đầu")]
        public MonoBehaviour playerWeapon;

        [Header("Camera Shake")]
        [Tooltip("Kéo Camera của Player vào đây (cần có script CameraShake)")]
        public CameraShake cameraShake;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip airplaneFlightClip; // Tiếng bay rì rì
        public AudioClip airplaneTurbulenceClip; // Tiếng rung lắc, xóc
        public AudioClip planeCrashClip; // Tiếng nổ lớn lúc rớt

        [Header("Thời gian (giây)")]
        public float flyingDuration = 4f; // Bay bình thường
        public float turbulenceDuration = 3f; // Rung lắc
        public float blackScreenHoldAfterCrash = 3f; // Nằm im trong bóng tối sau khi rớt
        public float wakeUpFadeDuration = 4f; // Thời gian mở mắt (phai dần màn hình đen)

        void Start()
        {
            // Tự động tìm Player nếu quên gán
            if (playerController == null)
            {
                playerController = FindObjectOfType<PlayerController>();
            }
            if (cameraShake == null && playerController != null)
            {
                cameraShake = playerController.GetComponentInChildren<CameraShake>();
            }
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Bắt đầu chuỗi Cutscene
            StartCoroutine(PlayIntroCutscene());
        }

        IEnumerator PlayIntroCutscene()
        {
            // BƯỚC 1: KHỞI TẠO (Màn hình đen thui, khoá người chơi)
            if (blackScreenUI != null)
            {
                blackScreenUI.color = new Color(0, 0, 0, 1f); // Đen 100%
                blackScreenUI.gameObject.SetActive(true);
            }

            if (playerController != null)
            {
                playerController.enabled = false; // Khoá di chuyển và quay chuột
            }

            if (playerWeapon != null)
            {
                playerWeapon.enabled = false; // Khoá súng
            }

            // BƯỚC 2: MÁY BAY BAY BÌNH THƯỜNG
            if (airplaneFlightClip != null)
            {
                audioSource.clip = airplaneFlightClip;
                audioSource.loop = true;
                audioSource.Play();
            }
            yield return new WaitForSeconds(flyingDuration);

            // BƯỚC 3: MÁY BAY RUNG LẮC (TURBULENCE)
            if (airplaneTurbulenceClip != null)
            {
                audioSource.PlayOneShot(airplaneTurbulenceClip);
            }
            if (cameraShake != null)
            {
                // Rung nhẹ
                cameraShake.StartShake(turbulenceDuration, 0.5f, 1.5f);
            }
            yield return new WaitForSeconds(turbulenceDuration);

            // BƯỚC 4: MÁY BAY RƠI & VA CHẠM (CRASH)
            audioSource.Stop(); // Tắt tiếng động cơ
            if (planeCrashClip != null)
            {
                audioSource.PlayOneShot(planeCrashClip);
            }
            if (cameraShake != null)
            {
                // Rung cực mạnh
                cameraShake.StartShake(2f, 2f, 3f);
            }

            // Chờ cho bớt dư âm vụ nổ
            yield return new WaitForSeconds(blackScreenHoldAfterCrash);

            // BƯỚC 5: TỈNH DẬY (MỜ MÀN HÌNH ĐEN)
            if (blackScreenUI != null)
            {
                float elapsedTime = 0f;
                while (elapsedTime < wakeUpFadeDuration)
                {
                    elapsedTime += Time.deltaTime;
                    float alpha = Mathf.Lerp(1f, 0f, elapsedTime / wakeUpFadeDuration);
                    blackScreenUI.color = new Color(0, 0, 0, alpha);
                    yield return null;
                }
                blackScreenUI.gameObject.SetActive(false);
            }

            // BƯỚC 6: TRẢ LẠI QUYỀN ĐIỀU KHIỂN CHO NGƯỜI CHƠI
            if (playerController != null)
            {
                playerController.enabled = true; // Cho phép đi lại, quay chuột
            }

            if (playerWeapon != null)
            {
                playerWeapon.enabled = true; // Cho phép dùng súng
            }
            
            // Xong Cutscene, tự xoá script này để nhẹ máy
            Destroy(this.gameObject);
        }
    }
}

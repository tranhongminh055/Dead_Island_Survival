using UnityEngine;
using HorrorGame.Player;

namespace HorrorGame.Effects
{
    public class SanityEffects : MonoBehaviour
    {
        public PlayerStats stats;
        
        [Header("Audio Dị Tượng (Paranoia)")]
        public AudioSource paranoiaAudioSource; // Kéo thả cái Audio Source chứa tiếng lầm bầm vào đây
        public float audioTriggerChance = 0.5f; // 50% cơ hội phát tiếng rùng rợn mỗi 5 giây

        [Header("Hiệu ứng Chóng Mặt (Dizziness)")]
        public Transform playerCamera; // Kéo thả PlayerCamera vào đây
        public float maxTiltAngle = 10f; // Góc nghiêng tối đa
        public float tiltSpeed = 2f; // Tốc độ chao đảo

        private float nextAudioCheckTime = 0f;

        void Start()
        {
            if (stats == null) stats = FindObjectOfType<PlayerStats>();
            if (playerCamera == null) playerCamera = Camera.main.transform;
        }

        void LateUpdate()
        {
            if (stats == null || playerCamera == null) return;

            // Nếu Sanity < 30 (tức là 30%), bắt đầu bị ảo giác
            if (stats.currentSanity < 30f)
            {
                HandleDizziness();
                HandleParanoiaAudio();
            }
            else
            {
                // Phục hồi Camera nếu Sanity ổn định (được ăn uống lại)
                RecoverCamera();
            }
        }

        private void HandleDizziness()
        {
            // Tính toán mức độ say sóng dựa trên lượng Sanity bị mất (Sanity càng sát 0 thì càng say mạnh)
            float intensity = 1f - (stats.currentSanity / 30f); 
            
            // Dùng sóng Sin để tạo cảm giác xoay vòng, chao đảo từ trái sang phải
            float zTilt = Mathf.Sin(Time.time * tiltSpeed) * (maxTiltAngle * intensity);
            
            // Bẻ cong góc nhìn Camera (trục Z) mà không làm ảnh hưởng việc ngước lên/xuống (Trục X)
            Vector3 rot = playerCamera.localEulerAngles;
            rot.z = zTilt;
            playerCamera.localEulerAngles = rot;
        }

        private void RecoverCamera()
        {
            // Trả từ từ Camera về vị trí thăng bằng
            float currentZ = playerCamera.localEulerAngles.z;
            if (currentZ > 180f) currentZ -= 360f; // Chuẩn hóa góc

            if (Mathf.Abs(currentZ) > 0.1f)
            {
                float newZ = Mathf.Lerp(currentZ, 0f, Time.deltaTime * 5f);
                Vector3 rot = playerCamera.localEulerAngles;
                rot.z = newZ;
                playerCamera.localEulerAngles = rot;
            }
        }

        private void HandleParanoiaAudio()
        {
            if (paranoiaAudioSource == null || paranoiaAudioSource.clip == null) return;

            // Kiểm tra mỗi 5 giây
            if (Time.time >= nextAudioCheckTime)
            {
                nextAudioCheckTime = Time.time + 5f;
                
                // Trả về ngẫu nhiên 0 đến 1. Nếu nhỏ hơn xác suất -> Bắt đầu phát tiếng thì thầm
                if (Random.value < audioTriggerChance && !paranoiaAudioSource.isPlaying)
                {
                    paranoiaAudioSource.pitch = Random.Range(0.8f, 1.2f); // Bóp méo giọng để nghe ghê rợn và méo mó hơn
                    paranoiaAudioSource.Play();
                }
            }
        }
    }
}

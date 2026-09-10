using UnityEngine;

namespace HorrorGame.Environment
{
    /// <summary>
    /// Điều khiển hiệu ứng tại Hiện trường Xác Máy Bay Rơi:
    /// - Cột sáng trắng khẩn cấp chiếu thẳng lên trời (Sky Beacon)
    /// - Đèn chớp cảnh báo cứu nạn (ELT Strobe)
    /// - Lửa tàn dư bập bùng trong thân xác máy bay
    /// - Âm thanh cứu nạn phát ra từ hộp đen
    /// </summary>
    public class AirplaneCrashSite : MonoBehaviour
    {
        [Header("── Cột Sáng Trắng Cứu Hộ (Sky Beacon) ──")]
        public Transform beaconBeamTransform;
        public Light beaconSkyLight; // Spotlight cực mạnh chiếu thẳng lên trời
        public Light beaconStrobeLight; // Đèn nháy chớp đỏ/trắng tại chân cột phát
        public float beamRotateSpeed = 12f;
        public float strobeInterval = 1.2f;

        [Header("── Lửa & Tàn Dư Máy Bay ──")]
        public Light[] fireFlickerLights;
        public float fireFlickerSpeed = 15f;

        [Header("── Âm Thanh Cứu Hộ & Tàn Dư ──")]
        public AudioSource audioSource;
        public AudioClip emergencyBeepClip;
        public AudioClip fireCrackleClip;

        private float strobeTimer = 0f;
        private float[] fireBaseIntensities;

        private void Start()
        {
            // Lưu độ sáng ban đầu của các đèn lửa
            if (fireFlickerLights != null && fireFlickerLights.Length > 0)
            {
                fireBaseIntensities = new float[fireFlickerLights.Length];
                for (int i = 0; i < fireFlickerLights.Length; i++)
                {
                    if (fireFlickerLights[i] != null)
                        fireBaseIntensities[i] = fireFlickerLights[i].intensity;
                }
            }

            // Thiết lập âm thanh cứu hộ
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (audioSource != null)
            {
                audioSource.spatialBlend = 1.0f; // 3D Audio
                audioSource.minDistance = 5f;
                audioSource.maxDistance = 80f;
                audioSource.loop = true;
                audioSource.volume = 0.65f;

                if (emergencyBeepClip != null)
                {
                    audioSource.clip = emergencyBeepClip;
                    audioSource.Play();
                }
            }
        }

        private void Update()
        {
            // 1. Cột sáng xoay tròn nhẹ trên bầu trời
            if (beaconBeamTransform != null)
            {
                beaconBeamTransform.Rotate(Vector3.up, beamRotateSpeed * Time.deltaTime, Space.World);
            }

            // 2. Nhịp thở sáng nhẹ của đèn chiếu trời
            if (beaconSkyLight != null)
            {
                float pulse = 0.85f + Mathf.Sin(Time.time * 2f) * 0.15f;
                beaconSkyLight.intensity = 6.0f * pulse;
            }

            // 3. Đèn chớp cứu nạn (Strobe Flash)
            strobeTimer += Time.deltaTime;
            if (strobeTimer >= strobeInterval)
            {
                strobeTimer = 0f;
                if (beaconStrobeLight != null)
                {
                    StartCoroutine(FlashStrobe());
                }
            }

            // 4. Hiệu ứng lửa bập bùng ngẫu nhiên (Fire flicker)
            if (fireFlickerLights != null)
            {
                for (int i = 0; i < fireFlickerLights.Length; i++)
                {
                    if (fireFlickerLights[i] != null && fireBaseIntensities != null && i < fireBaseIntensities.Length)
                    {
                        float noise = Mathf.PerlinNoise(Time.time * fireFlickerSpeed, i * 7.7f);
                        fireFlickerLights[i].intensity = fireBaseIntensities[i] * (0.65f + noise * 0.7f);
                    }
                }
            }
        }

        private System.Collections.IEnumerator FlashStrobe()
        {
            if (beaconStrobeLight == null) yield break;

            beaconStrobeLight.enabled = true;
            beaconStrobeLight.intensity = 8.0f;
            yield return new WaitForSeconds(0.08f);
            beaconStrobeLight.intensity = 0f;
            yield return new WaitForSeconds(0.06f);
            beaconStrobeLight.intensity = 6.0f;
            yield return new WaitForSeconds(0.08f);
            beaconStrobeLight.intensity = 0f;
            beaconStrobeLight.enabled = false;
        }
    }
}

using UnityEngine;

namespace HorrorGame.Cutscenes
{
    /// <summary>
    /// Điều khiển hiệu ứng sống động bên ngoài cửa sổ khoang máy bay:
    /// - Cuộn mây trôi tốc độ cao (800 km/h) trên tầng bình lưu
    /// - Đèn chớp chống va chạm (Anti-Collision Strobe) chớp kép chu kỳ chuẩn hàng không
    /// - Nón turbine động cơ phản lực xoay đều
    /// - Hiệu ứng bão, sét và động cơ bốc khói/lửa khi máy bay gặp sự cố rơi
    /// </summary>
    public class AirplaneCabinExterior : MonoBehaviour
    {
        [Header("── Mây Trôi Ngoài Cửa Sổ ──")]
        public Renderer[] cloudRenderers;
        public float cloudScrollSpeed = 0.85f;
        public Transform[] cloudLayers;
        public float cloudMoveSpeed = 45f;
        public float cloudResetZ = 30f;
        public float cloudStartZ = -30f;

        [Header("── Đèn Hàng Không (Aviation Lights) ──")]
        public Light wingStrobeLight;        // Đèn chớp trắng cực mạnh ở đầu cánh
        public Renderer wingStrobeRenderer;  // Bóng đèn phát sáng
        public Light wingNavLight;           // Đèn đỏ định vị mạn trái (Port Red)
        public float strobeCycle = 1.2f;     // Chu kỳ chớp đôi chuẩn FAA

        [Header("── Động Cơ Phản Lực (Jet Engine) ──")]
        public Transform engineSpinner;      // Nón xoay giữa cánh quạt turbine
        public float engineSpinSpeed = 1200f;
        public Light engineGlowLight;        // Ánh lửa động cơ khi hỏng hóc

        [Header("── Hiệu Ứng Khẩn Cấp / Rơi Máy Bay ──")]
        public Light sunDirectionalLight;    // Ánh nắng chiếu qua cửa sổ
        public Light lightningFlashLight;    // Chớp sét khi lao vào bão

        private float strobeTimer = 0f;
        private bool isEmergency = false;
        private float lightningTimer = 0f;

        private void Start()
        {
            if (wingStrobeLight != null)
            {
                // Giới hạn tầm phát sáng trong bán kính 2.5m tại đầu cánh.
                // Vì đầu cánh cách thân cabin hơn 8.5m nên ánh sáng chớp chỉ hoạt động bên ngoài trời,
                // tuyệt đối không lọt vào hay rọi sáng nhấp nháy bên trong khoang cabin.
                wingStrobeLight.range = 2.5f;
            }

            if (wingNavLight != null)
            {
                wingNavLight.color = new Color(1f, 0.05f, 0.05f);
                wingNavLight.intensity = 2.0f;
                wingNavLight.range = 2.5f;
            }

            if (engineGlowLight != null)
            {
                engineGlowLight.intensity = 0f;
            }

            if (lightningFlashLight != null)
            {
                lightningFlashLight.intensity = 0f;
            }
        }

        public void TriggerEmergency()
        {
            isEmergency = true;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 1. Mây trôi liên tục tạo cảm giác bay 800 km/h
            UpdateClouds(dt);

            // 2. Xoay nón turbine động cơ
            if (engineSpinner != null)
            {
                engineSpinner.Rotate(Vector3.forward, engineSpinSpeed * dt, Space.Self);
            }

            // 3. Đèn Strobe chớp kép chuẩn hàng không: Chớp 1 - nghỉ 0.1s - Chớp 2 - nghỉ 1.0s
            UpdateAviationStrobe(dt);

            // 4. Theo dõi trạng thái cutscene chuyển sang khẩn cấp
            CheckEmergencyState(dt);
        }

        private void UpdateClouds(float dt)
        {
            // Cuộn texture UV nếu vật liệu hỗ trợ
            if (cloudRenderers != null)
            {
                for (int i = 0; i < cloudRenderers.Length; i++)
                {
                    if (cloudRenderers[i] != null && cloudRenderers[i].sharedMaterial != null)
                    {
                        Vector2 offset = cloudRenderers[i].sharedMaterial.mainTextureOffset;
                        offset.x += cloudScrollSpeed * dt * (1f + i * 0.3f);
                        cloudRenderers[i].sharedMaterial.mainTextureOffset = offset;
                    }
                }
            }

            // Di chuyển các tấm mây 3D về phía sau rồi lặp lại
            if (cloudLayers != null)
            {
                for (int i = 0; i < cloudLayers.Length; i++)
                {
                    if (cloudLayers[i] != null)
                    {
                        cloudLayers[i].Translate(Vector3.back * cloudMoveSpeed * dt, Space.World);
                        if (cloudLayers[i].localPosition.z < cloudStartZ)
                        {
                            Vector3 p = cloudLayers[i].localPosition;
                            p.z = cloudResetZ;
                            cloudLayers[i].localPosition = p;
                        }
                    }
                }
            }
        }

        private void UpdateAviationStrobe(float dt)
        {
            strobeTimer += dt;
            if (strobeTimer >= strobeCycle)
            {
                strobeTimer -= strobeCycle;
            }

            // Mẫu chớp FAA: 0.00-0.06s (Flash 1), 0.14-0.20s (Flash 2), còn lại Tắt
            bool isFlash = (strobeTimer < 0.07f) || (strobeTimer > 0.14f && strobeTimer < 0.21f);

            // Khi xảy ra tai nạn khẩn cấp: Đèn chớp loạn xạ
            if (isEmergency)
            {
                isFlash = (Mathf.PerlinNoise(Time.time * 25f, 0f) > 0.45f);
            }

            if (wingStrobeLight != null)
            {
                wingStrobeLight.range = 2.5f;
                wingStrobeLight.intensity = isFlash ? 3.0f : 0f;
            }

            if (wingStrobeRenderer != null)
            {
                Material mat = Application.isPlaying ? wingStrobeRenderer.material : wingStrobeRenderer.sharedMaterial;
                if (mat != null)
                {
                    if (isFlash)
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", Color.white * 4f);
                    }
                    else
                    {
                        mat.SetColor("_EmissionColor", Color.black);
                    }
                }
            }
        }

        private void CheckEmergencyState(float dt)
        {
            // Kiểm tra cờ toàn cục của Cutscene
            if (!isEmergency && AirplaneCrashCutscene.IsCutsceneActive)
            {
                // Tìm xem đèn báo động đã bật chưa
                AirplaneCrashCutscene cutscene = FindObjectOfType<AirplaneCrashCutscene>();
                if (cutscene != null && cutscene.warningLights != null && cutscene.warningLights.Length > 0)
                {
                    if (cutscene.warningLights[0] != null && cutscene.warningLights[0].intensity > 0.5f)
                    {
                        isEmergency = true;
                    }
                }
            }

            if (isEmergency)
            {
                // Ánh sáng bầu trời tối dần
                if (sunDirectionalLight != null && sunDirectionalLight.intensity > 0.1f)
                {
                    sunDirectionalLight.intensity = Mathf.Lerp(sunDirectionalLight.intensity, 0.1f, dt * 1.5f);
                }

                // Động cơ bốc lửa đỏ cam chập chờn
                if (engineGlowLight != null)
                {
                    float fireNoise = Mathf.PerlinNoise(Time.time * 30f, 10f);
                    engineGlowLight.intensity = Mathf.Lerp(2.0f, 6.5f, fireNoise);
                    engineGlowLight.color = Color.Lerp(new Color(1f, 0.35f, 0f), new Color(1f, 0.7f, 0.1f), fireNoise);
                }

                // Sấm chớp ngoài cửa sổ
                lightningTimer -= dt;
                if (lightningTimer <= 0f)
                {
                    lightningTimer = Random.Range(1.2f, 3.5f);
                    StartCoroutine(DoLightningFlash());
                }
            }
        }

        private System.Collections.IEnumerator DoLightningFlash()
        {
            if (lightningFlashLight == null) yield break;

            for (int k = 0; k < 2; k++)
            {
                lightningFlashLight.intensity = Random.Range(3.5f, 6.0f);
                yield return new WaitForSeconds(0.04f);
                lightningFlashLight.intensity = 0f;
                yield return new WaitForSeconds(0.06f);
            }
        }
    }
}

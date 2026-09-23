using UnityEngine;
using UnityEngine.UI;

namespace HorrorGame.Cutscenes
{
    /// <summary>
    /// Điều khiển hiệu ứng sống động bên ngoài cửa sổ khoang máy bay:
    /// - Cuộn mây trôi tốc độ cao (800 km/h) với hiệu ứng parallax nhiều tầng
    /// - Đèn chớp chống va chạm (Anti-Collision Strobe) chớp kép chu kỳ chuẩn FAA
    /// - Nón turbine động cơ phản lực xoay đều
    /// - Hiệu ứng bão (mưa xiên, sét chớp liên tục) khi máy bay gặp sự cố
    /// - Particle tia lửa từ động cơ bốc cháy
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
        public Light wingStrobeLight;        // Đèn chớp trắng cực mạnh ở đầu cánh trái
        public Renderer wingStrobeRenderer;  // Bóng đèn phát sáng cánh trái
        public Light wingStrobeLightRight;   // Đèn chớp trắng ở đầu cánh phải
        public Renderer wingStrobeRendererRight; // Bóng đèn phát sáng cánh phải
        public Light wingNavLight;           // Đèn đỏ định vị mạn trái (Port Red)
        public Light wingNavLightRight;      // Đèn xanh lá định vị mạn phải (Starboard Green)
        public Light tailBeaconLight;        // Đèn chớp đỏ đỉnh cánh đuôi đứng
        public float strobeCycle = 1.2f;     // Chu kỳ chớp đôi chuẩn FAA

        [Header("── Động Cơ Phản Lực (Jet Engine) ──")]
        public Transform engineSpinner;      // Nón xoay động cơ trái
        public Transform engineSpinnerRight; // Nón xoay động cơ phải
        public float engineSpinSpeed = 1200f;
        public Light engineGlowLight;        // Ánh lửa động cơ trái khi hỏng hóc
        public Light engineGlowLightRight;   // Ánh lửa động cơ phải

        [Header("── Hiệu Ứng Khẩn Cấp / Rơi Máy Bay ──")]
        public Light sunDirectionalLight;    // Ánh nắng chiếu qua cửa sổ
        public Light lightningFlashLight;    // Chớp sét khi lao vào bão

        private float strobeTimer = 0f;
        private int strobePhase = 0; // 0: flash1, 1: gap, 2: flash2, 3: longGap
        private bool isEmergency = false;
        private float lightningTimer = 0f;
        private float emergencyElapsed = 0f;

        // Runtime-created particle systems
        private ParticleSystem rainParticleSystem;
        private ParticleSystem engineFireTrailPS;
        private ParticleSystem engineSparksPS;

        private void Start()
        {
            if (wingStrobeLight != null)
            {
                wingStrobeLight.range = 2.5f;
                wingStrobeLight.intensity = 0f;
            }
            if (wingStrobeLightRight != null)
            {
                wingStrobeLightRight.range = 2.5f;
                wingStrobeLightRight.intensity = 0f;
            }

            if (wingNavLight != null)
            {
                wingNavLight.color = new Color(1f, 0.05f, 0.05f);
                wingNavLight.intensity = 2.0f;
                wingNavLight.range = 2.5f;
            }
            if (wingNavLightRight != null)
            {
                wingNavLightRight.color = new Color(0.05f, 1f, 0.2f);
                wingNavLightRight.intensity = 2.0f;
                wingNavLightRight.range = 2.5f;
            }
            if (tailBeaconLight != null)
            {
                tailBeaconLight.color = Color.red;
                tailBeaconLight.intensity = 2.5f;
                tailBeaconLight.range = 8.0f;
            }

            if (engineGlowLight != null)
            {
                engineGlowLight.intensity = 0f;
            }
            if (engineGlowLightRight != null)
            {
                engineGlowLightRight.intensity = 0f;
            }

            if (lightningFlashLight != null)
            {
                lightningFlashLight.intensity = 0f;
            }
        }

        public void TriggerEmergency()
        {
            isEmergency = true;
            emergencyElapsed = 0f;

            // Tạo particle mưa xiên khi lao vào bão
            CreateRainParticles();

            // Tạo particle tia lửa động cơ bốc cháy
            CreateEngineFireTrail();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 1. Mây trôi liên tục tạo cảm giác bay 800 km/h với parallax
            UpdateCloudsParallax(dt);

            // 2. Xoay nón turbine động cơ (cả 2 động cơ trái & phải đồng bộ)
            if (engineSpinner != null)
            {
                engineSpinner.Rotate(Vector3.forward, engineSpinSpeed * dt, Space.Self);
            }
            if (engineSpinnerRight != null)
            {
                engineSpinnerRight.Rotate(Vector3.forward, engineSpinSpeed * dt, Space.Self);
            }

            // 3. Đèn Strobe chớp kép chuẩn FAA
            UpdateAviationStrobe(dt);

            // 4. Theo dõi trạng thái cutscene chuyển sang khẩn cấp
            CheckEmergencyState(dt);
        }

        // ──────────────────────────────────────────────
        // MÂY PARALLAX NHIỀU TẦNG
        // ──────────────────────────────────────────────
        private void UpdateCloudsParallax(float dt)
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

            // Di chuyển các tấm mây 3D về phía sau — với tốc độ parallax (gần nhanh, xa chậm)
            if (cloudLayers != null)
            {
                for (int i = 0; i < cloudLayers.Length; i++)
                {
                    if (cloudLayers[i] != null)
                    {
                        // Parallax factor: tầng đầu tiên nhanh nhất, tầng xa chậm hơn
                        float parallaxFactor = 1f - (float)i / (cloudLayers.Length + 1) * 0.6f;
                        float speed = cloudMoveSpeed * parallaxFactor;

                        // Khi khẩn cấp: mây trôi nhanh hơn (máy bay lao nhanh xuống)
                        if (isEmergency)
                        {
                            speed *= Mathf.Lerp(1f, 2.5f, Mathf.Clamp01(emergencyElapsed / 10f));
                        }

                        cloudLayers[i].Translate(Vector3.back * speed * dt, Space.World);

                        if (cloudLayers[i].localPosition.z < cloudStartZ)
                        {
                            Vector3 p = cloudLayers[i].localPosition;
                            p.z = cloudResetZ + Random.Range(-3f, 3f); // Thêm random offset cho tự nhiên
                            p.y += Random.Range(-0.5f, 0.5f); // Lắc nhẹ chiều cao
                            cloudLayers[i].localPosition = p;

                            // Random scale khi reset để mây không lặp lại đều đặn
                            float s = Random.Range(0.8f, 1.3f);
                            Vector3 sc = cloudLayers[i].localScale;
                            cloudLayers[i].localScale = new Vector3(sc.x * s, sc.y, sc.z * s);
                        }
                    }
                }
            }
        }

        // ──────────────────────────────────────────────
        // ĐÈN STROBE CHỚP ĐÔI CHUẨN FAA
        // ──────────────────────────────────────────────
        private void UpdateAviationStrobe(float dt)
        {
            if (wingStrobeLight == null && tailBeaconLight == null) return;

            strobeTimer += dt;

            // Chu kỳ chớp đôi chuẩn FAA:
            // Flash 1 (0.05s) → Gap (0.05s) → Flash 2 (0.05s) → Long Gap (~1.05s) → Lặp lại
            float flash1End = 0.05f;
            float gap1End   = 0.10f;
            float flash2End = 0.15f;

            bool strobeOn = false;

            if (strobeTimer < flash1End)
            {
                strobeOn = true;
            }
            else if (strobeTimer < gap1End)
            {
                strobeOn = false;
            }
            else if (strobeTimer < flash2End)
            {
                strobeOn = true;
            }
            else if (strobeTimer >= strobeCycle)
            {
                strobeTimer = 0f;
            }

            // Đèn strobe trắng cánh
            float strobeIntensity = strobeOn ? 8f : 0f;
            if (wingStrobeLight != null) wingStrobeLight.intensity = strobeIntensity;
            if (wingStrobeLightRight != null) wingStrobeLightRight.intensity = strobeIntensity;

            // Renderer glow (emissive material)
            if (wingStrobeRenderer != null)
            {
                Material mat = wingStrobeRenderer.material;
                if (mat != null)
                {
                    mat.SetColor("_EmissionColor", strobeOn ? Color.white * 5f : Color.black);
                }
            }
            if (wingStrobeRendererRight != null)
            {
                Material mat = wingStrobeRendererRight.material;
                if (mat != null)
                {
                    mat.SetColor("_EmissionColor", strobeOn ? Color.white * 5f : Color.black);
                }
            }

            // Đèn đỏ beacon đuôi: chớp đơn, lệch pha so với strobe cánh
            if (tailBeaconLight != null)
            {
                float beaconPhase = (strobeTimer + strobeCycle * 0.5f) % strobeCycle;
                bool beaconOn = beaconPhase < 0.08f;
                tailBeaconLight.intensity = beaconOn ? 4f : 0f;
            }
        }

        // ──────────────────────────────────────────────
        // TRẠNG THÁI KHẨN CẤP
        // ──────────────────────────────────────────────
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
                        TriggerEmergency();
                    }
                }
            }

            if (isEmergency)
            {
                emergencyElapsed += dt;

                // Ánh sáng bầu trời tối dần (bão kéo đến)
                if (sunDirectionalLight != null && sunDirectionalLight.intensity > 0.08f)
                {
                    sunDirectionalLight.intensity = Mathf.Lerp(sunDirectionalLight.intensity, 0.08f, dt * 1.2f);
                    // Chuyển màu ánh sáng sang xám tối (bão)
                    sunDirectionalLight.color = Color.Lerp(sunDirectionalLight.color, new Color(0.4f, 0.42f, 0.5f), dt * 0.8f);
                }

                // Động cơ bốc lửa đỏ cam chập chờn — dùng multi-frequency noise
                if (engineGlowLight != null)
                {
                    float fireNoise1 = Mathf.PerlinNoise(Time.time * 30f, 10f);
                    float fireNoise2 = Mathf.PerlinNoise(Time.time * 12f, 50f);
                    float combined = fireNoise1 * 0.7f + fireNoise2 * 0.3f;

                    // Cường độ tăng dần theo thời gian khẩn cấp
                    float emergencyProgress = Mathf.Clamp01(emergencyElapsed / 12f);
                    float baseIntensity = Mathf.Lerp(2.0f, 4.0f, emergencyProgress);
                    float peakIntensity = Mathf.Lerp(6.5f, 10f, emergencyProgress);

                    engineGlowLight.intensity = Mathf.Lerp(baseIntensity, peakIntensity, combined);
                    engineGlowLight.color = Color.Lerp(
                        new Color(1f, 0.35f, 0f),   // Cam đỏ
                        new Color(1f, 0.7f, 0.1f),   // Vàng sáng
                        combined
                    );
                    // Range tăng dần
                    engineGlowLight.range = Mathf.Lerp(8f, 15f, emergencyProgress);
                }

                // Sấm chớp ngoài cửa sổ — tần suất tăng dần
                lightningTimer -= dt;
                if (lightningTimer <= 0f)
                {
                    float minInterval = Mathf.Lerp(2.5f, 0.6f, Mathf.Clamp01(emergencyElapsed / 12f));
                    float maxInterval = Mathf.Lerp(4.0f, 1.2f, Mathf.Clamp01(emergencyElapsed / 12f));
                    lightningTimer = Random.Range(minInterval, maxInterval);
                    StartCoroutine(DoLightningFlash());
                }

                // Tốc độ quay turbine giảm dần (động cơ hỏng)
                engineSpinSpeed = Mathf.Lerp(engineSpinSpeed, 200f, dt * 0.3f);
            }
        }

        private System.Collections.IEnumerator DoLightningFlash()
        {
            if (lightningFlashLight == null) yield break;

            // Triple flash với cường độ và timing ngẫu nhiên — realistic hơn
            int flashes = Random.Range(2, 4);
            for (int k = 0; k < flashes; k++)
            {
                float intensity = Random.Range(3.5f, 8.0f);
                lightningFlashLight.intensity = intensity;
                lightningFlashLight.color = Color.Lerp(Color.white, new Color(0.8f, 0.85f, 1f), Random.value);
                yield return new WaitForSeconds(Random.Range(0.02f, 0.06f));
                lightningFlashLight.intensity = Random.Range(0f, intensity * 0.3f);
                yield return new WaitForSeconds(Random.Range(0.03f, 0.08f));
            }
            lightningFlashLight.intensity = 0f;
        }

        // ──────────────────────────────────────────────
        // PARTICLE EFFECTS — MƯA BÃO + LỬA ĐỘNG CƠ
        // ──────────────────────────────────────────────

        private void CreateRainParticles()
        {
            if (rainParticleSystem != null) return;

            GameObject rainObj = new GameObject("Emergency_RainStreaks");
            rainObj.transform.SetParent(transform, false);
            rainObj.transform.localPosition = new Vector3(-3f, 3f, 0);
            rainObj.transform.localRotation = Quaternion.Euler(-60f, -25f, 0); // Mưa xiên theo hướng bay

            rainParticleSystem = rainObj.AddComponent<ParticleSystem>();
            rainParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = rainParticleSystem.main;
            main.duration = 30f;
            main.loop = true;
            main.startLifetime = 0.4f;
            main.startSpeed = 60f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startColor = new Color(0.7f, 0.75f, 0.85f, 0.5f);
            main.maxParticles = 500;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = rainParticleSystem.emission;
            emission.rateOverTime = 300f;

            var shape = rainParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(12f, 0.1f, 8f);
            
            // Tìm và gán material cho mưa
            Shader rainShader = Shader.Find("Particles/Standard Unlit");
            if (rainShader == null) rainShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (rainShader == null) rainShader = Shader.Find("Mobile/Particles/Alpha Blended");
            if (rainShader != null)
            {
                Material rainMat = new Material(rainShader);
                rainMat.color = new Color(0.7f, 0.78f, 0.9f, 0.4f);
                var psr = rainObj.GetComponent<ParticleSystemRenderer>();
                if (psr != null)
                {
                    psr.sharedMaterial = rainMat;
                    psr.renderMode = ParticleSystemRenderMode.Stretch;
                    psr.lengthScale = 8f;
                }
            }

            rainParticleSystem.Play();
        }

        private void CreateEngineFireTrail()
        {
            if (engineFireTrailPS != null) return;
            if (engineSpinner == null) return;

            // Tia lửa phụt từ đuôi động cơ
            Transform engineRoot = engineSpinner.parent != null ? engineSpinner.parent : engineSpinner;

            // ── Fire Trail ──
            GameObject fireObj = new GameObject("Engine_FireTrail");
            fireObj.transform.SetParent(engineRoot, false);
            fireObj.transform.localPosition = new Vector3(0, 0, -1.5f); // Phía sau động cơ

            engineFireTrailPS = fireObj.AddComponent<ParticleSystem>();
            engineFireTrailPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = engineFireTrailPS.main;
            main.duration = 30f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(15f, 30f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.6f, 0.1f, 0.9f),
                new Color(1f, 0.3f, 0f, 0.7f)
            );
            main.maxParticles = 200;

            var emission = engineFireTrailPS.emission;
            emission.rateOverTime = 120f;

            var shape = engineFireTrailPS.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.4f;

            // Color over lifetime: vàng cam → đỏ → đen
            var col = engineFireTrailPS.colorOverLifetime;
            col.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0f),
                    new GradientColorKey(new Color(1f, 0.3f, 0f), 0.4f),
                    new GradientColorKey(new Color(0.2f, 0.05f, 0f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0.6f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = gradient;

            // Size over lifetime: nhỏ → lớn (lan tỏa)
            var sizeOverLife = engineFireTrailPS.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.3f),
                new Keyframe(0.3f, 1f),
                new Keyframe(1f, 2.5f)
            ));

            // Noise module cho chuyển động lửa organic
            var noise = engineFireTrailPS.noise;
            noise.enabled = true;
            noise.strength = 1.5f;
            noise.frequency = 3f;
            noise.scrollSpeed = 2f;

            Shader fireShader = Shader.Find("Particles/Standard Unlit");
            if (fireShader == null) fireShader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (fireShader == null) fireShader = Shader.Find("Mobile/Particles/Additive");
            if (fireShader != null)
            {
                Material fireMat = new Material(fireShader);
                fireMat.color = new Color(1f, 0.5f, 0.1f, 0.8f);
                var psr = fireObj.GetComponent<ParticleSystemRenderer>();
                if (psr != null) psr.sharedMaterial = fireMat;
            }

            engineFireTrailPS.Play();

            // ── Metal Sparks ──
            GameObject sparksObj = new GameObject("Engine_MetalSparks");
            sparksObj.transform.SetParent(engineRoot, false);
            sparksObj.transform.localPosition = new Vector3(0, 0, -1.0f);

            engineSparksPS = sparksObj.AddComponent<ParticleSystem>();
            engineSparksPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var sMain = engineSparksPS.main;
            sMain.duration = 30f;
            sMain.loop = true;
            sMain.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            sMain.startSpeed = new ParticleSystem.MinMaxCurve(8f, 20f);
            sMain.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            sMain.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.9f, 0.4f, 1f),
                new Color(1f, 0.6f, 0.1f, 1f)
            );
            sMain.maxParticles = 100;
            sMain.gravityModifier = 2f; // Sparks rơi xuống theo trọng lực

            var sEmission = engineSparksPS.emission;
            sEmission.rateOverTime = 40f;

            var sShape = engineSparksPS.shape;
            sShape.shapeType = ParticleSystemShapeType.Cone;
            sShape.angle = 25f;
            sShape.radius = 0.3f;

            Shader sparkShader = Shader.Find("Particles/Standard Unlit");
            if (sparkShader == null) sparkShader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (sparkShader == null) sparkShader = Shader.Find("Mobile/Particles/Additive");
            if (sparkShader != null)
            {
                Material sparkMat = new Material(sparkShader);
                sparkMat.color = new Color(1f, 0.85f, 0.3f, 1f);
                sparkMat.EnableKeyword("_EMISSION");
                sparkMat.SetColor("_EmissionColor", new Color(1f, 0.7f, 0.2f) * 3f);
                var psr = sparksObj.GetComponent<ParticleSystemRenderer>();
                if (psr != null) psr.sharedMaterial = sparkMat;
            }

            engineSparksPS.Play();
        }
    }
}


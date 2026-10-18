using UnityEngine;

namespace HorrorGame.Cave
{
    /// <summary>
    /// CaveSignalSmoke — Cột Khói Đỏ Báo Hiệu Cửa Hang Động (Emergency Red Signal Smoke).
    /// - Bốc lên trời cao 60-80 mét, cuộn đỏ rực rỡ, nhìn thấy được từ mọi nơi trên hòn đảo.
    /// - Có nguồn sáng đỏ rực rỡ (Signal Flare Light) chiếu sáng khu vực cửa hang ban đêm.
    /// - Hiển thị khoảng cách và vị trí trực quan giúp người chơi dễ dàng tìm đường tới hang động.
    /// - Tự động kích hoạt khi vào game để người chơi thấy ngay lập tức.
    /// </summary>
    [ExecuteInEditMode]
    public class CaveSignalSmoke : MonoBehaviour
    {
        public static CaveSignalSmoke Instance;

        [Header("── Vị Trí Cửa Hang ──")]
        [Tooltip("Tên hang động được đánh dấu")]
        public string caveName = "Hang 1 - Hang Sụp Đổ (The Collapsed Den)";
        
        [Tooltip("Tọa độ cửa hang mặc định (sâu trong rừng)")]
        public Vector3 caveEntrancePos = new Vector3(385f, 21.5f, 575f);

        [Header("── Tùy Chỉnh Khói Đỏ ──")]
        public Color primarySmokeColor = new Color(1.0f, 0.12f, 0.08f, 0.85f);
        public Color secondarySmokeColor = new Color(0.65f, 0.05f, 0.05f, 0.4f);
        public float smokeHeight = 85f;
        public float smokeSize = 3.5f;

        [Header("── Đèn Cảnh Báo (Signal Flare Light) ──")]
        public bool enableFlareLight = true;
        public Color flareLightColor = new Color(1.0f, 0.2f, 0.15f);
        public float flareIntensity = 3.5f;
        public float flareRange = 25f;

        [Header("── Hiển Thị Khoảng Cách (HUD) ──")]
        public bool showDistanceMarker = true;
        public float maxDisplayDistance = 600f;

        // Nội bộ
        private ParticleSystem mainPlumePS;
        private ParticleSystem groundSmokePS;
        private ParticleSystem sparksPS;
        private Light flareLight;
        private Transform playerTransform;
        private Camera mainCamera;
        private Material particleMat;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            SetupSmokeSystems();
            FindPlayerAndCamera();
        }

        private void Update()
        {
            if (flareLight != null)
            {
                // Nhịp thở nhấp nháy cho đèn flare đỏ
                float flicker = Mathf.PerlinNoise(Time.time * 6f, 0f) * 0.8f;
                flareLight.intensity = flareIntensity + flicker;
            }

            if (mainPlumePS != null)
            {
                mainPlumePS.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            }

            if (sparksPS != null)
            {
                sparksPS.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            }

            if (playerTransform == null || mainCamera == null)
            {
                FindPlayerAndCamera();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Tránh can thiệp cấu trúc GameObject trong OnValidate
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && flareLight != null)
                {
                    flareLight.color = flareLightColor;
                    flareLight.intensity = flareIntensity;
                    flareLight.range = flareRange;
                }
            };
        }
#endif

        private void FindPlayerAndCamera()
        {
            if (mainCamera == null) mainCamera = Camera.main;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                var pc = FindObjectOfType<Player.PlayerController>();
                if (pc != null) playerTransform = pc.transform;
            }
        }

        /// <summary>
        /// Khởi tạo toàn bộ hiệu ứng cột khói đỏ
        /// </summary>
        public void SetupSmokeSystems()
        {
            // Đặt vị trí
            transform.position = caveEntrancePos;

            particleMat = GetOrCreateParticleMaterial();

            // 1. Cột khói đỏ chính (Main Plume)
            Transform plumeTrans = transform.Find("RedSmoke_MainPlume");
            if (plumeTrans == null)
            {
                GameObject plumeObj = new GameObject("RedSmoke_MainPlume");
                plumeObj.transform.SetParent(transform);
                plumeObj.transform.localPosition = Vector3.zero;
                plumeObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // Hướng thẳng lên trời

                mainPlumePS = plumeObj.AddComponent<ParticleSystem>();
                plumeTrans = plumeObj.transform;
            }
            else
            {
                mainPlumePS = plumeTrans.GetComponent<ParticleSystem>();
            }

            ConfigureMainPlume(mainPlumePS);

            // 2. Khói đỏ là là mặt đất (Ground Smoke)
            Transform groundTrans = transform.Find("RedSmoke_Ground");
            if (groundTrans == null)
            {
                GameObject groundObj = new GameObject("RedSmoke_Ground");
                groundObj.transform.SetParent(transform);
                groundObj.transform.localPosition = new Vector3(0, 0.2f, 0);
                groundObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

                groundSmokePS = groundObj.AddComponent<ParticleSystem>();
                groundTrans = groundObj.transform;
            }
            else
            {
                groundSmokePS = groundTrans.GetComponent<ParticleSystem>();
            }

            ConfigureGroundSmoke(groundSmokePS);

            // 3. Tàn lửa đỏ bốc lên (Sparks)
            Transform sparkTrans = transform.Find("RedSmoke_Sparks");
            if (sparkTrans == null)
            {
                GameObject sparkObj = new GameObject("RedSmoke_Sparks");
                sparkObj.transform.SetParent(transform);
                sparkObj.transform.localPosition = new Vector3(0, 0.3f, 0);
                sparkObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

                sparksPS = sparkObj.AddComponent<ParticleSystem>();
                sparkTrans = sparkObj.transform;
            }
            else
            {
                sparksPS = sparkTrans.GetComponent<ParticleSystem>();
            }

            ConfigureSparks(sparksPS);

            // 4. Đèn tín hiệu màu đỏ (Signal Flare Light)
            Transform lightTrans = transform.Find("RedSmoke_FlareLight");
            if (lightTrans == null)
            {
                GameObject lightObj = new GameObject("RedSmoke_FlareLight");
                lightObj.transform.SetParent(transform);
                lightObj.transform.localPosition = new Vector3(0, 1.2f, 0);

                flareLight = lightObj.AddComponent<Light>();
                lightTrans = lightObj.transform;
            }
            else
            {
                flareLight = lightTrans.GetComponent<Light>();
            }

            if (flareLight != null)
            {
                flareLight.type = LightType.Point;
                flareLight.color = flareLightColor;
                flareLight.intensity = flareIntensity;
                flareLight.range = flareRange;
                flareLight.shadows = LightShadows.Soft;
            }

            // 5. Cọc que pháo sáng tín hiệu (Flare Stick Prop)
            Transform stickTrans = transform.Find("RedSmoke_FlareStick");
            if (stickTrans == null)
            {
                GameObject stickObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stickObj.name = "RedSmoke_FlareStick";
                stickObj.transform.SetParent(transform);
                stickObj.transform.localPosition = new Vector3(0, 0.35f, 0);
                stickObj.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);

                // Tắt Collider để không cản trở di chuyển của người chơi (không dùng DestroyImmediate để tránh lỗi OnValidate)
                Collider c = stickObj.GetComponent<Collider>();
                if (c != null)
                {
                    c.enabled = false;
                }

                Renderer r = stickObj.GetComponent<Renderer>();
                if (r != null)
                {
                    Shader sh = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                    if (sh != null)
                    {
                        Material sm = new Material(sh);
                        sm.color = new Color(0.9f, 0.1f, 0.1f);
                        if (sm.HasProperty("_EmissionColor"))
                        {
                            sm.EnableKeyword("_EMISSION");
                            sm.SetColor("_EmissionColor", new Color(1f, 0.2f, 0.1f) * 2f);
                        }
                        r.sharedMaterial = sm;
                    }
                }
            }
        }

        private void ConfigureMainPlume(ParticleSystem ps)
        {
            if (ps == null) return;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // Đảm bảo luôn hướng thẳng đứng 100% lên bầu trời (không bị nghiêng theo địa hình)
            ps.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

            var main = ps.main;
            main.loop = true;
            main.prewarm = true; // Khói bốc sẵn lên trời ngay khi mở game
            main.startLifetime = 11f;
            main.startSpeed = 16f; // Bắn vọt thẳng đứng lên trời rất cao và nhanh
            main.startSize = new ParticleSystem.MinMaxCurve(smokeSize * 0.9f, smokeSize * 1.25f);
            main.startColor = primarySmokeColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 650;

            var emission = ps.emission;
            emission.rateOverTime = 45f; // Mật độ dày đặc liên tục tạo thành một cột thẳng tắp

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 0f; // 0 ĐỘ: CỘT HÌNH TRỤ THẲNG TẮP HOÀN TOÀN, KHÔNG BỊ TÒE HAY NGHIÊNG
            shape.radius = 0.45f;

            // Triệt tiêu hoàn toàn độ dạt ngang (X và Z = 0), chỉ tăng tốc thẳng đứng theo trục Y
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0f);
            vel.z = new ParticleSystem.MinMaxCurve(0f);
            vel.y = new ParticleSystem.MinMaxCurve(5f); // Lực đẩy gia tốc tiếp tục hướng thẳng lên trời

            // Giữ kích thước cột khói đều đặn từ gốc lên ngọn như một cột trụ tín hiệu
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0.0f, 0.75f);
            curve.AddKey(0.2f, 1.0f);
            curve.AddKey(0.8f, 1.35f);
            curve.AddKey(1.0f, 1.5f); // Không phình to quá mức, giữ dáng cột thẳng tắp
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            // Chuyển màu và làm mờ dần khi chạm tầng mây
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.35f, 0.1f), 0.0f),  // Gốc cam đỏ sáng
                    new GradientColorKey(primarySmokeColor, 0.2f),            // Thân cột đỏ rực rỡ
                    new GradientColorKey(secondarySmokeColor, 0.75f),         // Đỏ sẫm trên cao
                    new GradientColorKey(new Color(0.3f, 0.05f, 0.05f), 1.0f) // Xám đỏ loãng
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.95f, 0.0f),
                    new GradientAlphaKey(0.9f, 0.35f),
                    new GradientAlphaKey(0.6f, 0.8f),
                    new GradientAlphaKey(0.0f, 1.0f)                          // Mờ dần khi tan vào mây
                }
            );
            colorOverLifetime.color = grad;

            // Gán material
            if (particleMat != null)
            {
                ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMat;
            }

            ps.Play();
        }

        private void ConfigureGroundSmoke(ParticleSystem ps)
        {
            if (ps == null) return;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 5f;
            main.startSpeed = 1.8f;
            main.startSize = new ParticleSystem.MinMaxCurve(2.0f, 3.5f);
            main.startColor = new Color(primarySmokeColor.r, primarySmokeColor.g, primarySmokeColor.b, 0.5f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 100;

            var emission = ps.emission;
            emission.rateOverTime = 12f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 2.0f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.5f);
            curve.AddKey(1f, 2.5f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(primarySmokeColor, 0f),
                    new GradientColorKey(secondarySmokeColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.6f, 0f),
                    new GradientAlphaKey(0.0f, 1f)
                }
            );
            colorOverLifetime.color = grad;

            if (particleMat != null)
            {
                ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMat;
            }

            ps.Play();
        }

        private void ConfigureSparks(ParticleSystem ps)
        {
            if (ps == null) return;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ps.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

            var main = ps.main;
            main.loop = true;
            main.startLifetime = 3f;
            main.startSpeed = 12.0f;
            main.startSize = 0.25f;
            main.startColor = new Color(1.0f, 0.8f, 0.3f, 1f); // Tàn lửa vàng cam
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;

            var emission = ps.emission;
            emission.rateOverTime = 25f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 0f; // Bắn thẳng đứng lên trời
            shape.radius = 0.2f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0f);
            vel.z = new ParticleSystem.MinMaxCurve(0f);
            vel.y = new ParticleSystem.MinMaxCurve(4f);

            if (particleMat != null)
            {
                ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMat;
            }

            ps.Play();
        }

        private Material GetOrCreateParticleMaterial()
        {
            Shader shader = Shader.Find("Particles/Alpha Blended");
            if (shader == null) shader = Shader.Find("Mobile/Particles/Alpha Blended");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = shader != null ? new Material(shader) : Resources.GetBuiltinResource<Material>("Default-Particle.mat");
            if (mat != null)
            {
                Texture2D tex = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                        float alpha = Mathf.Clamp01(1f - (dist / 15.5f));
                        alpha = alpha * alpha * (3f - 2f * alpha); // Smoothstep làm mềm mép hạt
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
                tex.Apply();
                mat.mainTexture = tex;
            }
            return mat;
        }

        // =========================================================================
        // HIỂN THỊ ĐIỂM ĐÁNH DẤU TRÊN MÀN HÌNH (HUD MARKER)
        // =========================================================================
        private void OnGUI()
        {
            if (!showDistanceMarker) return;
            if (mainCamera == null || playerTransform == null) return;

            // Tính khoảng cách
            float dist = Vector3.Distance(playerTransform.position, transform.position);
            if (dist > maxDisplayDistance) return;

            // Tính vị trí trên màn hình
            Vector3 targetScreenPoint = transform.position + new Vector3(0f, 12f, 0f); // Đặt marker ngang lưng chừng cột khói
            Vector3 screenPos = mainCamera.WorldToScreenPoint(targetScreenPoint);

            // Kiểm tra điểm có nằm phía trước Camera không
            if (screenPos.z > 0)
            {
                float x = screenPos.x;
                float y = Screen.height - screenPos.y;

                string text = string.Format("🔴 CỘT KHÓI ĐỎ: {0}\n[Cách {1:0} mét]", caveName, dist);

                GUIStyle shadowStyle = new GUIStyle(GUI.skin.label);
                shadowStyle.alignment = TextAnchor.MiddleCenter;
                shadowStyle.fontSize = 13;
                shadowStyle.fontStyle = FontStyle.Bold;
                shadowStyle.normal.textColor = Color.black;

                GUIStyle textStyle = new GUIStyle(GUI.skin.label);
                textStyle.alignment = TextAnchor.MiddleCenter;
                textStyle.fontSize = 13;
                textStyle.fontStyle = FontStyle.Bold;
                textStyle.normal.textColor = dist <= 40f ? Color.green : new Color(1f, 0.4f, 0.4f);

                float width = 320f;
                float height = 45f;
                Rect rect = new Rect(x - (width / 2), y - (height / 2), width, height);

                // Bóng đổ
                GUI.Label(new Rect(rect.x + 1, rect.y + 1, width, height), text, shadowStyle);
                // Chữ chính
                GUI.Label(rect, text, textStyle);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureSignalSmokeRuntime()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu") return;

            Vector3 forestPos = new Vector3(385f, 21.5f, 575f) + new Vector3(2.5f, 0f, -1.2f);
            if (Terrain.activeTerrain != null)
            {
                forestPos.y = Terrain.activeTerrain.SampleHeight(forestPos) + Terrain.activeTerrain.transform.position.y;
            }

            CaveSignalSmoke existing = Object.FindObjectOfType<CaveSignalSmoke>();
            if (existing == null && Object.FindObjectOfType<TheForestCaveEntrance>() == null)
            {
                GameObject smokeObj = new GameObject("[Cave_RedSmokeSignal]");
                smokeObj.transform.position = forestPos;
                var comp = smokeObj.AddComponent<CaveSignalSmoke>();
                comp.caveEntrancePos = forestPos;
                Debug.Log("🔥 [CaveSignalSmoke] Đã tự động kích hoạt Cột Khói Đỏ báo hiệu Cửa Hang 1 tại rừng rậm: " + forestPos);
            }
            else
            {
                // Nếu khói cũ đang ở gần nhà (480, 492) -> dời ngay vào rừng
                if (Vector3.Distance(existing.transform.position, new Vector3(480f, 15f, 492f)) < 25f ||
                    Vector3.Distance(existing.caveEntrancePos, new Vector3(480f, 15f, 492f)) < 25f)
                {
                    existing.transform.position = forestPos;
                    existing.caveEntrancePos = forestPos;
                    existing.SetupSmokeSystems();
                    Debug.Log("🔥 [CaveSignalSmoke] Đã di dời Cột Khói Đỏ vào sâu trong rừng rậm tại " + forestPos);
                }
            }
        }
    }
}

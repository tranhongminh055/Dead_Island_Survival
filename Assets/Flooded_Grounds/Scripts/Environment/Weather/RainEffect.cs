using UnityEngine;

namespace HorrorGame.Environment.Weather
{
    /// <summary>
    /// Quản lý hiệu ứng hạt mưa rơi, bọt nước đập mặt đất và sương mù mưa rơi quanh người chơi.
    /// Tự động di chuyển bám theo Camera/Người chơi để tối ưu hiệu năng tuyệt đối.
    /// </summary>
    public class RainEffect : MonoBehaviour
    {
        [Header("── Tham Chiếu Particle Systems ──")]
        public ParticleSystem rainParticles;
        public ParticleSystem splashParticles;
        public ParticleSystem mistParticles;

        [Header("── Vật Liệu (Tự động nạp nếu trống) ──")]
        public Material rainMaterial;
        public Material splashMaterial;
        public Material mistMaterial;

        [Header("── Thông Số Mưa ──")]
        public float rainHeightAbovePlayer = 15f;
        public float rainAreaSize = 35f;
        public float maxRainRate = 2200f;
        public float maxSplashRate = 350f;
        public float maxMistRate = 40f;

        [Header("── Tốc Độ Rơi & Hướng Gió ──")]
        public float fallSpeed = 28f;
        public Vector2 windDirection = new Vector2(1f, 0.5f);

        private Transform targetTransform;
        private ParticleSystem.EmissionModule rainEmission;
        private ParticleSystem.EmissionModule splashEmission;
        private ParticleSystem.EmissionModule mistEmission;
        private ParticleSystem.VelocityOverLifetimeModule rainVelocity;

        private void Awake()
        {
            EnsureMaterials();
            EnsureParticleSystems();
        }

        private void Start()
        {
            FindTarget();
            StopAllRainImmediate();
        }

        private void FindTarget()
        {
            if (targetTransform != null) return;
            if (Camera.main != null)
            {
                targetTransform = Camera.main.transform;
            }
            else
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) targetTransform = p.transform;
            }
        }

        private void LateUpdate()
        {
            if (targetTransform == null)
            {
                FindTarget();
                if (targetTransform == null) return;
            }

            Vector3 targetPos = targetTransform.position;

            // Đặt vị trí mưa ở ngay trên đầu Camera
            if (rainParticles != null)
            {
                rainParticles.transform.position = new Vector3(targetPos.x, targetPos.y + rainHeightAbovePlayer, targetPos.z);
            }

            // Đặt vị trí tia nước văng đập mặt đất ngay dưới chân Camera
            if (splashParticles != null)
            {
                splashParticles.transform.position = new Vector3(targetPos.x, targetPos.y, targetPos.z);
            }

            // Đặt sương mù mưa quanh người chơi
            if (mistParticles != null)
            {
                mistParticles.transform.position = new Vector3(targetPos.x, targetPos.y + 1f, targetPos.z);
            }
        }

        /// <summary>
        /// Dừng hoàn toàn và xóa sạch mọi hạt mưa ngay lập tức
        /// </summary>
        public void StopAllRainImmediate()
        {
            if (rainParticles != null)
            {
                rainEmission.rateOverTime = 0f;
                rainParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (splashParticles != null)
            {
                splashEmission.rateOverTime = 0f;
                splashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (mistParticles != null)
            {
                mistEmission.rateOverTime = 0f;
                mistParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        /// <summary>
        /// Cập nhật cường độ mưa từ WeatherSystem (0.0 -> 1.0)
        /// </summary>
        public void UpdateRain(float intensity, float windIntensity, bool isSheltered)
        {
            if (rainParticles == null) return;

            if (intensity <= 0.005f)
            {
                StopAllRainImmediate();
                return;
            }

            // Nếu người chơi đang đứng dưới mái che kín (trong nhà/máy bay), giảm 90% hạt mưa quanh camera
            float shelterFactor = isSheltered ? 0.08f : 1.0f;

            float rainRate = intensity * maxRainRate * shelterFactor;
            float splashRate = intensity * maxSplashRate * (isSheltered ? 0f : 1f);
            float mistRate = intensity * maxMistRate * shelterFactor;

            if (!rainParticles.isPlaying) rainParticles.Play();
            if (!splashParticles.isPlaying) splashParticles.Play();

            if (intensity > 0.4f)
            {
                if (mistParticles != null && !mistParticles.isPlaying) mistParticles.Play();
            }
            else
            {
                if (mistParticles != null && mistParticles.isPlaying) mistParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            // Bật/tắt emission mượt mà
            rainEmission.rateOverTime = rainRate;
            splashEmission.rateOverTime = splashRate;
            mistEmission.rateOverTime = (intensity > 0.4f) ? mistRate : 0f;

            // Hướng gió đẩy hạt mưa nghiêng
            Vector2 normWind = windDirection.normalized * windIntensity;
            rainVelocity.x = new ParticleSystem.MinMaxCurve(normWind.x * 6f, normWind.x * 10f);
            rainVelocity.z = new ParticleSystem.MinMaxCurve(normWind.y * 6f, normWind.y * 10f);
            rainVelocity.y = new ParticleSystem.MinMaxCurve(-fallSpeed - (windIntensity * 4f));
        }

        /// <summary>
        /// Tự động tạo Particle System hoàn chỉnh bằng code nếu Scene chưa có sẵn
        /// </summary>
        private void EnsureParticleSystems()
        {
            // 1. RAIN PARTICLES
            if (rainParticles == null)
            {
                GameObject rainObj = new GameObject("Rain_Drops_Emitter");
                rainObj.transform.SetParent(this.transform);
                rainParticles = rainObj.AddComponent<ParticleSystem>();
                var main = rainParticles.main;
                main.loop = true;
                main.playOnAwake = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 4000;
                main.startLifetime = 0.8f;
                main.startSpeed = fallSpeed;
                main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
                main.startColor = new Color(0.85f, 0.92f, 1f, 0.5f);

                var shape = rainParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(rainAreaSize, rainAreaSize, 3f);
                // Xoay hộp để phát hạt hướng xuống dưới
                shape.rotation = new Vector3(90f, 0f, 0f);

                rainVelocity = rainParticles.velocityOverLifetime;
                rainVelocity.enabled = true;
                rainVelocity.space = ParticleSystemSimulationSpace.World;
                rainVelocity.y = new ParticleSystem.MinMaxCurve(-fallSpeed);

                var renderer = rainObj.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = 2.5f;
                renderer.velocityScale = -0.04f;
                renderer.material = rainMaterial;

                rainEmission = rainParticles.emission;
                rainEmission.rateOverTime = 0f;
                rainParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            else
            {
                rainEmission = rainParticles.emission;
                rainVelocity = rainParticles.velocityOverLifetime;
            }

            // 2. SPLASH PARTICLES
            if (splashParticles == null)
            {
                GameObject splashObj = new GameObject("Rain_Splash_Emitter");
                splashObj.transform.SetParent(this.transform);
                splashParticles = splashObj.AddComponent<ParticleSystem>();
                var main = splashParticles.main;
                main.loop = true;
                main.playOnAwake = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 800;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.8f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
                main.startColor = new Color(0.9f, 0.95f, 1f, 0.4f);

                var shape = splashParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 16f;
                shape.rotation = new Vector3(90f, 0f, 0f);

                var renderer = splashObj.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.material = splashMaterial;

                splashEmission = splashParticles.emission;
                splashEmission.rateOverTime = 0f;
                splashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            else
            {
                splashEmission = splashParticles.emission;
            }

            // 3. MIST PARTICLES
            if (mistParticles == null)
            {
                GameObject mistObj = new GameObject("Rain_Mist_Emitter");
                mistObj.transform.SetParent(this.transform);
                mistParticles = mistObj.AddComponent<ParticleSystem>();
                var main = mistParticles.main;
                main.loop = true;
                main.playOnAwake = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 120;
                main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
                main.startSize = new ParticleSystem.MinMaxCurve(4f, 8f);
                main.startColor = new Color(0.85f, 0.9f, 0.95f, 0.12f);

                var shape = mistParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(25f, 25f, 2f);
                shape.rotation = new Vector3(90f, 0f, 0f);

                var renderer = mistObj.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.material = mistMaterial;

                mistEmission = mistParticles.emission;
                mistEmission.rateOverTime = 0f;
                mistParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            else
            {
                mistEmission = mistParticles.emission;
            }
        }

        private void EnsureMaterials()
        {
            if (rainMaterial == null)
            {
                rainMaterial = CreateParticleMaterial("Weather_Rain_Mat", "RainDrop.png", true);
            }
            if (splashMaterial == null)
            {
                splashMaterial = CreateParticleMaterial("Weather_Splash_Mat", "WaterSplash.png", false);
            }
            if (mistMaterial == null)
            {
                mistMaterial = CreateParticleMaterial("Weather_Mist_Mat", "RainMist.png", false);
            }
        }

        private Material CreateParticleMaterial(string matName, string texFileName, bool isAdditive)
        {
            Shader shader = Shader.Find("Particles/Alpha Blended");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            Material mat = new Material(shader);
            mat.name = matName;

#if UNITY_EDITOR
            string texPath = "Assets/Flooded_Grounds/Content/Textures/Weather/" + texFileName;
            Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.mainTexture = tex;
                return mat;
            }
#endif
            // Nếu không load được texture editor, tự tạo Texture 32x32 an toàn chống lỗi màn hình hồng
            Texture2D fallbackTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                    float alpha = Mathf.Clamp01(1f - (dist / 15.5f));
                    fallbackTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            fallbackTex.Apply();
            mat.mainTexture = fallbackTex;
            return mat;
        }
    }
}

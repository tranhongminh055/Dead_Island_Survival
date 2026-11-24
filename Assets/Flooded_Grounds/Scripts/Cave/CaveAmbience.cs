using UnityEngine;
using System.Collections;

namespace HorrorGame.Cave
{
    /// <summary>
    /// CaveAmbience — Hệ thống âm thanh và hiệu ứng môi trường trong hang.
    /// Tạo ra bầu không khí rùng rợn phong cách The Forest:
    /// - Tiếng nước nhỏ giọt từ trần hang (random interval)
    /// - Gió hú trong hành lang hẹp
    /// - Tiếng rên rỉ không rõ nguồn gốc (creepy ambient)
    /// - Particle bụi bay lơ lửng trong ánh sáng
    /// - Sương mù dày đặc (Volumetric Fog mô phỏng)
    /// </summary>
    public class CaveAmbience : MonoBehaviour
    {
        [Header("── Loại Bầu Không Khí ──")]
        public CaveAmbienceType ambienceType = CaveAmbienceType.DarkWet;

        [Header("── Âm Thanh Nền (Ambient Loop) ──")]
        [Tooltip("Âm thanh nền liên tục trong hang")]
        public AudioClip ambientLoop;

        [Tooltip("Âm lượng nền")]
        [Range(0f, 1f)]
        public float ambientVolume = 0.3f;

        [Header("── Âm Thanh Ngẫu Nhiên (Random SFX) ──")]
        [Tooltip("Tiếng nước nhỏ giọt")]
        public AudioClip dripClip;

        [Tooltip("Tiếng rên rỉ bí ẩn")]
        public AudioClip creepyClip;

        [Tooltip("Tiếng đá rơi nhỏ")]
        public AudioClip pebbleClip;

        [Tooltip("Khoảng cách tối thiểu giữa 2 tiếng ngẫu nhiên (giây)")]
        public float minRandomInterval = 3f;

        [Tooltip("Khoảng cách tối đa giữa 2 tiếng ngẫu nhiên (giây)")]
        public float maxRandomInterval = 12f;

        [Header("── Particle Effects ──")]
        [Tooltip("Bật particle bụi bay")]
        public bool enableDustParticles = true;

        [Tooltip("Bật particle sương mù")]
        public bool enableFogParticles = true;

        [Tooltip("Bật particle bào tử (Hang 6)")]
        public bool enableSporeParticles = false;

        [Tooltip("Bật particle hơi nước/hơi nóng")]
        public bool enableSteamParticles = false;

        [Header("── Cài Đặt Fog Riêng ──")]
        [Tooltip("Màu sương mù trong hang")]
        public Color fogColor = new Color(0.02f, 0.02f, 0.04f);

        [Tooltip("Mật độ sương mù")]
        public float fogDensity = 0.05f;

        // Components
        private AudioSource ambientSource;
        private AudioSource sfxSource;
        private float nextRandomSfxTime = 0f;

        // Particles
        private ParticleSystem dustPS;
        private ParticleSystem fogPS;
        private ParticleSystem sporePS;
        private ParticleSystem steamPS;

        // ═══════════════════════════════════════════════════════════════
        // KHỞI TẠO
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            SetupAudio();
            SetupParticles();
            ApplyAmbiencePreset();
        }

        private void SetupAudio()
        {
            // Ambient loop source
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.clip = ambientLoop;
            ambientSource.loop = true;
            ambientSource.volume = ambientVolume;
            ambientSource.spatialBlend = 0.3f; // Hơi spatial
            ambientSource.playOnAwake = false;
            ambientSource.priority = 100;

            // SFX one-shot source
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0.6f;
            sfxSource.playOnAwake = false;
            sfxSource.volume = 0.5f;

            // Tự động load sounds nếu chưa gán
            if (ambientLoop == null)
                ambientLoop = Resources.Load<AudioClip>("Sounds/Cave/cave_ambient");
            if (dripClip == null)
                dripClip = Resources.Load<AudioClip>("Sounds/Cave/cave_drip");
            if (creepyClip == null)
                creepyClip = Resources.Load<AudioClip>("Sounds/Cave/cave_creature");
            if (pebbleClip == null)
                pebbleClip = Resources.Load<AudioClip>("Sounds/Cave/cave_pebble");

            if (ambientLoop != null)
            {
                ambientSource.clip = ambientLoop;
            }

            nextRandomSfxTime = Time.time + Random.Range(minRandomInterval, maxRandomInterval);
        }

        private void SetupParticles()
        {
            if (enableDustParticles) CreateDustParticles();
            if (enableFogParticles) CreateFogParticles();
            if (enableSporeParticles) CreateSporeParticles();
            if (enableSteamParticles) CreateSteamParticles();
        }

        private void ApplyAmbiencePreset()
        {
            switch (ambienceType)
            {
                case CaveAmbienceType.DarkWet: // Hang tối ẩm ướt (Hang 1, 2)
                    fogColor = new Color(0.02f, 0.02f, 0.04f);
                    fogDensity = 0.04f;
                    enableDustParticles = true;
                    enableFogParticles = true;
                    break;

                case CaveAmbienceType.Underwater: // Hang biển (Hang 3)
                    fogColor = new Color(0.01f, 0.04f, 0.08f);
                    fogDensity = 0.06f;
                    enableDustParticles = false;
                    enableSteamParticles = false;
                    break;

                case CaveAmbienceType.Swamp: // Hang đầm lầy (Hang 4)
                    fogColor = new Color(0.03f, 0.04f, 0.02f);
                    fogDensity = 0.08f;
                    enableFogParticles = true;
                    enableSteamParticles = true;
                    break;

                case CaveAmbienceType.Volcanic: // Hang nham thạch (Hang 5)
                    fogColor = new Color(0.06f, 0.02f, 0.01f);
                    fogDensity = 0.05f;
                    enableSteamParticles = true;
                    enableDustParticles = false;
                    break;

                case CaveAmbienceType.Parasite: // Hang ký sinh (Hang 6)
                    fogColor = new Color(0.01f, 0.03f, 0.01f);
                    fogDensity = 0.07f;
                    enableSporeParticles = true;
                    enableFogParticles = true;
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // ENABLE / DISABLE
        // ═══════════════════════════════════════════════════════════════

        private void OnEnable()
        {
            if (ambientSource != null && ambientSource.clip != null)
                ambientSource.Play();
        }

        private void OnDisable()
        {
            if (ambientSource != null)
                ambientSource.Stop();
        }

        // ═══════════════════════════════════════════════════════════════
        // UPDATE — RANDOM SFX
        // ═══════════════════════════════════════════════════════════════

        private void Update()
        {
            // Phát âm thanh ngẫu nhiên
            if (Time.time >= nextRandomSfxTime)
            {
                PlayRandomSFX();
                nextRandomSfxTime = Time.time + Random.Range(minRandomInterval, maxRandomInterval);
            }
        }

        private void PlayRandomSFX()
        {
            if (sfxSource == null) return;

            // Chọn ngẫu nhiên 1 clip
            AudioClip clip = null;
            float rand = Random.value;

            if (rand < 0.5f && dripClip != null)
                clip = dripClip;
            else if (rand < 0.8f && pebbleClip != null)
                clip = pebbleClip;
            else if (creepyClip != null)
                clip = creepyClip;

            if (clip != null)
            {
                // Phát ở vị trí ngẫu nhiên xung quanh (tạo cảm giác không gian)
                sfxSource.pitch = Random.Range(0.85f, 1.15f);
                sfxSource.volume = Random.Range(0.2f, 0.5f);
                sfxSource.PlayOneShot(clip);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PARTICLE CREATION
        // ═══════════════════════════════════════════════════════════════

        private void CreateDustParticles()
        {
            GameObject dustObj = new GameObject("Cave_DustParticles");
            dustObj.transform.SetParent(transform, false);
            dustObj.transform.localPosition = Vector3.up * 2f;

            dustPS = dustObj.AddComponent<ParticleSystem>();
            var main = dustPS.main;
            main.startLifetime = 6f;
            main.startSpeed = 0.05f;
            main.startSize = 0.03f;
            main.startColor = new Color(0.6f, 0.55f, 0.5f, 0.4f);
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;

            var emission = dustPS.emission;
            emission.rateOverTime = 15f;

            var shape = dustPS.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(10f, 4f, 10f);

            var noise = dustPS.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.5f;

            SetupParticleRenderer(dustPS, new Color(0.7f, 0.65f, 0.6f, 0.3f));
        }

        private void CreateFogParticles()
        {
            GameObject fogObj = new GameObject("Cave_FogParticles");
            fogObj.transform.SetParent(transform, false);
            fogObj.transform.localPosition = Vector3.up * 0.5f;

            fogPS = fogObj.AddComponent<ParticleSystem>();
            var main = fogPS.main;
            main.startLifetime = 10f;
            main.startSpeed = 0.1f;
            main.startSize = 3f;
            main.startColor = new Color(fogColor.r, fogColor.g, fogColor.b, 0.15f);
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;

            var emission = fogPS.emission;
            emission.rateOverTime = 3f;

            var shape = fogPS.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(15f, 1f, 15f);

            var sizeOverLifetime = fogPS.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.5f),
                    new Keyframe(0.5f, 1f),
                    new Keyframe(1f, 0.3f)));

            SetupParticleRenderer(fogPS, new Color(0.5f, 0.5f, 0.5f, 0.08f));
        }

        private void CreateSporeParticles()
        {
            GameObject sporeObj = new GameObject("Cave_SporeParticles");
            sporeObj.transform.SetParent(transform, false);
            sporeObj.transform.localPosition = Vector3.up * 1f;

            sporePS = sporeObj.AddComponent<ParticleSystem>();
            var main = sporePS.main;
            main.startLifetime = 5f;
            main.startSpeed = 0.15f;
            main.startSize = 0.06f;
            main.startColor = new Color(0.2f, 1f, 0.3f, 0.7f);
            main.maxParticles = 150;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.gravityModifier = -0.02f; // Bay lên nhẹ

            var emission = sporePS.emission;
            emission.rateOverTime = 20f;

            var shape = sporePS.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(8f, 3f, 8f);

            var noise = sporePS.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 1f;

            SetupParticleRenderer(sporePS, new Color(0.3f, 1f, 0.4f, 0.6f));
        }

        private void CreateSteamParticles()
        {
            GameObject steamObj = new GameObject("Cave_SteamParticles");
            steamObj.transform.SetParent(transform, false);
            steamObj.transform.localPosition = Vector3.zero;

            steamPS = steamObj.AddComponent<ParticleSystem>();
            var main = steamPS.main;
            main.startLifetime = 4f;
            main.startSpeed = 0.5f;
            main.startSize = 1f;
            main.startColor = new Color(0.8f, 0.8f, 0.8f, 0.2f);
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.gravityModifier = -0.1f; // Bốc lên

            var emission = steamPS.emission;
            emission.rateOverTime = 8f;

            var shape = steamPS.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 2f;

            var sizeOverLifetime = steamPS.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.3f),
                    new Keyframe(0.3f, 1f),
                    new Keyframe(1f, 1.5f)));

            SetupParticleRenderer(steamPS, new Color(0.9f, 0.9f, 0.9f, 0.15f));
        }

        private void SetupParticleRenderer(ParticleSystem ps, Color color)
        {
            ParticleSystemRenderer rend = ps.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                Shader shader = Shader.Find("Particles/Additive");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    rend.material = new Material(shader);
                    rend.material.color = color;
                }
            }
        }
    }

    /// <summary>Loại bầu không khí hang</summary>
    public enum CaveAmbienceType
    {
        DarkWet,    // Tối ẩm ướt (Hang 1, 2)
        Underwater, // Hang biển (Hang 3)
        Swamp,      // Hang đầm lầy (Hang 4)
        Volcanic,   // Hang nham thạch (Hang 5)
        Parasite    // Hang ký sinh (Hang 6)
    }
}

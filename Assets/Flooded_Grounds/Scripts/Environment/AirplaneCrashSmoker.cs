using UnityEngine;

namespace HorrorGame.Environment
{
    /// <summary>
    /// Điều khiển Cột Khói & Tàn Lửa Nghi Ngút tại Hiện Trường Xác Máy Bay (Airplane Crash Smoker):
    /// - Quản lý hệ thống khói cuộn đen dày đặc bốc lên bầu trời (Billowing Smoke Plume)
    /// - Khói tản sát mặt đất (Ground Lingering Smoke)
    /// - Hạt tàn lửa đỏ lơ lửng bốc theo luồng nhiệt (Fire Embers & Sparks)
    /// - Mô phỏng gió thổi dạt khói tự nhiên (Wind Drift Simulation)
    /// - Nhịp thở mật độ khói theo nhiên liệu máy bay cháy (Perlin Noise Smoke Pulsing)
    /// - Tự động tối ưu hóa hiệu năng (LOD Distance Culling) khi người chơi ở xa
    /// - Âm thanh lửa cháy bập bùng và tiếng khói nghi ngút
    /// </summary>
    [DisallowMultipleComponent]
    public class AirplaneCrashSmoker : MonoBehaviour
    {
        [Header("── Hệ Thống Hạt Khói (Particle Systems) ──")]
        [Tooltip("Hệ thống khói chính bốc cao từ thân/động cơ máy bay")]
        public ParticleSystem mainSmokePlume;
        
        [Tooltip("Lớp khói loãng tỏa sát mặt đất xung quanh hiện trường")]
        public ParticleSystem groundSmoke;
        
        [Tooltip("Tàn lửa đỏ bốc theo cột nhiệt khí nóng")]
        public ParticleSystem fireEmbers;

        [Header("── Điều Khiển & Hiệu Ứng Gió (Wind & Dynamics) ──")]
        [Tooltip("Bật mô phỏng gió thổi dạt cột khói")]
        public bool enableWindDrift = true;
        
        [Tooltip("Hướng gió thổi dạt")]
        public Vector3 windDirection = new Vector3(0.6f, 0f, 0.4f);
        
        [Tooltip("Cường độ gió thổi")]
        [Range(0f, 10f)]
        public float windStrength = 2.5f;

        [Tooltip("Độ cuồn cuộn không đều của khói cháy (Pulsing noise)")]
        [Range(0f, 2f)]
        public float smokeTurbulence = 0.8f;

        [Header("── Âm Thanh Khói & Lửa (Smoke & Fire Audio) ──")]
        public AudioSource audioSource;
        public AudioClip fireCracklingClip;
        [Range(0f, 1f)]
        public float audioVolume = 0.7f;
        public float soundMaxDistance = 70f;

        [Header("── Vùng Khói Độc / Nhiệt Độ (Danger Zone) ──")]
        [Tooltip("Gây ho / mất máu nhẹ nếu người chơi đi xuyên tâm đám cháy khói đặc")]
        public bool enableSmokeInhalationDamage = false;
        public float dangerRadius = 2.8f;
        public float damagePerSecond = 2f;

        [Header("── Tối Ưu Hiệu Năng (LOD Optimization) ──")]
        [Tooltip("Khoảng cách tối đa nhìn thấy hạt chi tiết")]
        public float maxDetailDistance = 180f;
        [Tooltip("Tự động giảm tỷ lệ phát hạt khi ở xa để tiết kiệm FPS")]
        public bool enableDistanceLOD = true;

        // Nội bộ
        private Transform playerTransform;
        private float baseMainEmissionRate = 25f;
        private float baseGroundEmissionRate = 12f;
        private float baseEmbersEmissionRate = 30f;
        private float noiseTimer = 0f;

        private void Awake()
        {
            // Tự động tìm hạt con nếu chưa kéo thả vào Inspector
            AutoDetectComponents();
        }

        private void Start()
        {
            // Lưu lại tỷ lệ phát hạt chuẩn
            if (mainSmokePlume != null)
                baseMainEmissionRate = mainSmokePlume.emission.rateOverTime.constant;
            if (groundSmoke != null)
                baseGroundEmissionRate = groundSmoke.emission.rateOverTime.constant;
            if (fireEmbers != null)
                baseEmbersEmissionRate = fireEmbers.emission.rateOverTime.constant;

            // Thiết lập âm thanh 3D
            SetupAudio();

            // Tìm người chơi
            FindPlayer();
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
            }

            // 1. Mô phỏng luồng khói cuồn cuộn tự nhiên bằng Perlin Noise
            UpdateSmokeTurbulence();

            // 2. Tối ưu khoảng cách (LOD) theo vị trí người chơi
            if (enableDistanceLOD && playerTransform != null)
            {
                UpdateDistanceLOD();
            }

            // 3. Vùng khói sát thương (nếu kích hoạt)
            if (enableSmokeInhalationDamage && playerTransform != null)
            {
                CheckSmokeDamage();
            }
        }

        /// <summary>
        /// Mô phỏng nhịp khói bốc bất định khi dầu máy bay cháy
        /// </summary>
        private void UpdateSmokeTurbulence()
        {
            noiseTimer += Time.deltaTime * 0.5f;
            float pulse = Mathf.PerlinNoise(noiseTimer, 0.42f) * smokeTurbulence;

            if (mainSmokePlume != null && enableWindDrift)
            {
                var vel = mainSmokePlume.velocityOverLifetime;
                if (vel.enabled)
                {
                    Vector3 currentWind = windDirection.normalized * (windStrength * (0.8f + pulse * 0.4f));
                    vel.x = new ParticleSystem.MinMaxCurve(currentWind.x);
                    vel.z = new ParticleSystem.MinMaxCurve(currentWind.z);
                }
            }
        }

        /// <summary>
        /// Giảm tỷ lệ hạt khi người chơi ở rất xa để tránh tụt FPS
        /// </summary>
        private void UpdateDistanceLOD()
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);

            if (dist > maxDetailDistance * 1.5f)
            {
                // Quá xa: tạm dừng phát các hạt chi tiết nhỏ (tàn lửa, khói đất)
                SetEmissionRate(groundSmoke, 0f);
                SetEmissionRate(fireEmbers, 0f);
                SetEmissionRate(mainSmokePlume, baseMainEmissionRate * 0.35f);
            }
            else if (dist > maxDetailDistance)
            {
                // Trung bình xa: phát khói mỏng
                SetEmissionRate(groundSmoke, baseGroundEmissionRate * 0.5f);
                SetEmissionRate(fireEmbers, baseEmbersEmissionRate * 0.3f);
                SetEmissionRate(mainSmokePlume, baseMainEmissionRate * 0.7f);
            }
            else
            {
                // Ở cự ly gần: hiển thị 100% hiệu ứng cuộn khói & tàn lửa
                SetEmissionRate(groundSmoke, baseGroundEmissionRate);
                SetEmissionRate(fireEmbers, baseEmbersEmissionRate);
                SetEmissionRate(mainSmokePlume, baseMainEmissionRate);
            }
        }

        private void SetEmissionRate(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            var emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(rate);
        }

        /// <summary>
        /// Kiểm tra nếu người chơi bước quá gần ngọn khói đang cháy
        /// </summary>
        private void CheckSmokeDamage()
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= dangerRadius)
            {
                var stats = playerTransform.GetComponent<HorrorGame.Player.PlayerStats>();
                if (stats != null)
                {
                    stats.TakeDamage(damagePerSecond * Time.deltaTime);
                }
            }
        }

        private void SetupAudio()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (audioSource != null && fireCracklingClip != null)
            {
                audioSource.clip = fireCracklingClip;
                audioSource.loop = true;
                audioSource.volume = audioVolume;
                audioSource.spatialBlend = 1.0f; // 3D Audio
                audioSource.minDistance = 4f;
                audioSource.maxDistance = soundMaxDistance;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                }
            }
        }

        private void FindPlayer()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                playerTransform = p.transform;
            }
        }

        /// <summary>
        /// Tự động rà soát các ParticleSystem con nếu người dùng chưa gán bằng tay
        /// </summary>
        public void AutoDetectComponents()
        {
            if (mainSmokePlume == null)
            {
                // Tìm ParticleSystem trên chính mình hoặc con
                var systems = GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in systems)
                {
                    string n = ps.gameObject.name.ToLower();
                    if (n.Contains("ember") || n.Contains("spark"))
                    {
                        if (fireEmbers == null) fireEmbers = ps;
                    }
                    else if (n.Contains("ground") || n.Contains("fog"))
                    {
                        if (groundSmoke == null) groundSmoke = ps;
                    }
                    else
                    {
                        if (mainSmokePlume == null) mainSmokePlume = ps;
                    }
                }
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Vẽ vòng tròn cảnh báo vùng khói nguy hiểm trong Scene Editor
            if (enableSmokeInhalationDamage)
            {
                Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.35f);
                Gizmos.DrawWireSphere(transform.position, dangerRadius);
            }

            // Vẽ hướng gió
            if (enableWindDrift)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position + Vector3.up * 2f, windDirection.normalized * 5f);
            }
        }
    }
}

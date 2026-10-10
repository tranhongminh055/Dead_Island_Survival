using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace HorrorGame.Environment.Weather
{
    /// <summary>
    /// Hệ thống sấm sét chân thực:
    /// - Tạo tia sét hình học 3D phân nhánh ngoằn ngoèo bằng LineRenderer
    /// - Ánh chớp nhấp nháy đa xung (multi-pulse flash) chân thực
    /// - Mô phỏng vật lý tốc độ âm thanh (343m/s): Thấy chớp trước, nghe sấm sau tùy khoảng cách
    /// - Rung lắc màn hình (Camera Shake) khi sét đánh gần
    /// </summary>
    public class LightningSystem : MonoBehaviour
    {
        [Header("── Nguồn Sáng Chớp Sét ──")]
        public Light flashLight;
        public Color flashColor = new Color(0.85f, 0.92f, 1f);
        public float maxFlashIntensity = 3.5f;

        [Header("── Âm Thanh Sấm ──")]
        public AudioClip[] thunderCloseClips;
        public AudioClip[] thunderMediumClips;
        public AudioClip[] thunderFarClips;
        public AudioSource thunderAudioSource;

        [Header("── Rung Lắc Màn Hình (Camera Shake) ──")]
        [Tooltip("Mặc định TẮT rung lắc để tránh làm rung giật bản đồ và góc nhìn người chơi")]
        public bool enableCameraShake = false;
        [Tooltip("Độ rung siêu vi mô chỉ khi sét đánh sát bên cạnh < 60m (vài milimet, tuyệt đối không rung giật bản đồ)")]
        public float maxShakeMagnitude = 0.005f;

        [Header("── Thiết Lập Khoảng Cách & Tần Suất ──")]
        [Tooltip("Khoảng cách đánh sét tối thiểu tới người chơi (mét)")]
        public float minStrikeDistance = 150f;
        [Tooltip("Khoảng cách đánh sét tối đa tới người chơi (mét)")]
        public float maxStrikeDistance = 1400f;
        [Tooltip("Độ cao mây trời bắt đầu phóng tia sét")]
        public float cloudHeight = 90f;

        // Internal
        private Transform playerTransform;
        private Camera targetCamera;
        private List<LineRenderer> activeBoltRenderers = new List<LineRenderer>();
        private Material boltMaterial;
        private Coroutine activeFlashRoutine;
        private Coroutine activeShakeRoutine;

        private void Awake()
        {
            EnsureLightningLight();
            EnsureAudioSources();
            EnsureBoltMaterial();
        }

        private void Start()
        {
            FindPlayerAndCamera();
        }

        private void FindPlayerAndCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null) targetCamera = FindObjectOfType<Camera>();
            }
            if (playerTransform == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
                else
                {
                    var pc = FindObjectOfType<HorrorGame.Player.PlayerController>();
                    if (pc != null) playerTransform = pc.transform;
                    else if (targetCamera != null) playerTransform = targetCamera.transform;
                }
            }
        }

        public bool IsBlockedByMenuOrTrailer()
        {
            if (HorrorGame.UI.GameMenuManager.Instance != null && HorrorGame.UI.GameMenuManager.Instance.isMainMenuScene)
                return true;
            if (HorrorGame.Cutscenes.AirplaneCrashCutscene.IsCutsceneActive)
                return true;
            if (!HorrorGame.Cutscenes.AirplaneCrashCutscene.HasCutsceneFinished &&
                FindObjectOfType<HorrorGame.Cutscenes.AirplaneCrashCutscene>() != null)
                return true;
            return false;
        }

        /// <summary>
        /// Kích hoạt một lần sét đánh ngẫu nhiên
        /// </summary>
        public void StrikeRandom()
        {
            // Tuyệt đối không đánh sét khi đang ở Main Menu hoặc đang trong Trailer máy bay
            if (IsBlockedByMenuOrTrailer())
            {
                return;
            }

            FindPlayerAndCamera();
            Vector3 center = (playerTransform != null) ? playerTransform.position : Vector3.zero;

            // Góc ngẫu nhiên và khoảng cách ngẫu nhiên từ người chơi
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            // 70% đánh ở xa (350-1200m), 30% đánh ở cự ly gần giật mình (150-350m)
            float dist;
            if (Random.value < 0.28f)
            {
                dist = Random.Range(minStrikeDistance, 350f);
            }
            else
            {
                dist = Random.Range(350f, maxStrikeDistance);
            }

            Vector3 strikePos = center + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

            // Bắn raycast tìm mặt đất tại vị trí đánh
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(strikePos.x, 300f, strikePos.z), Vector3.down, out hit, 500f))
            {
                strikePos.y = hit.point.y;
            }
            else
            {
                strikePos.y = center.y;
            }

            StrikeAtPosition(strikePos);
        }

        /// <summary>
        /// Kích hoạt sét đánh tại một tọa độ cụ thể
        /// </summary>
        public void StrikeAtPosition(Vector3 groundTarget)
        {
            // Không đánh sét trong lúc xem trailer máy bay hoặc ở menu
            if (IsBlockedByMenuOrTrailer())
            {
                return;
            }

            FindPlayerAndCamera();
            Vector3 playerPos = (playerTransform != null) ? playerTransform.position : Vector3.zero;
            float distance = Vector3.Distance(playerPos, groundTarget);

            // 1. Điểm khởi đầu tia sét từ trên mây (lệch một chút so với điểm tiếp đất)
            Vector3 startPos = groundTarget + new Vector3(Random.Range(-40f, 40f), cloudHeight, Random.Range(-40f, 40f));

            // 2. Vẽ tia sét 3D ngoằn ngoèo bằng LineRenderer
            StartCoroutine(GenerateProceduralLightningBolt(startPos, groundTarget));

            // 3. Chớp sáng đa xung
            if (activeFlashRoutine != null) StopCoroutine(activeFlashRoutine);
            activeFlashRoutine = StartCoroutine(MultiPulseFlash(groundTarget, distance));

            // 4. Tính toán độ trễ âm thanh vật lý theo vận tốc âm thanh v = 343 m/s
            float soundDelay = Mathf.Clamp(distance / 343f, 0.05f, 6.0f);
            StartCoroutine(DelayedThunderSound(distance, soundDelay, groundTarget));

            Debug.Log(string.Format("⚡ [Lightning] Sét đánh cách {0:F0}m! Âm thanh sấm truyền sau {1:F2}s", distance, soundDelay));
        }

        // =========================================================================
        // HIỆU ỨNG TIA SÉT 3D (PROCEDURAL FRACTAL BOLT)
        // =========================================================================
        private IEnumerator GenerateProceduralLightningBolt(Vector3 start, Vector3 end)
        {
            GameObject boltObj = new GameObject("Bolt_FX");
            boltObj.transform.SetParent(this.transform);
            LineRenderer lr = boltObj.AddComponent<LineRenderer>();
            lr.material = boltMaterial;
            lr.startColor = Color.white;
            lr.endColor = new Color(0.7f, 0.85f, 1f, 0.8f);
            lr.startWidth = Random.Range(0.6f, 1.2f);
            lr.endWidth = 0.25f;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 4;

            // Thuật toán Midpoint Displacement fractal tạo tia ngoằn ngoèo
            List<Vector3> points = new List<Vector3>();
            points.Add(start);
            points.Add(end);

            int subdivisions = 5; // 32 đoạn cong
            for (int step = 0; step < subdivisions; step++)
            {
                List<Vector3> newPoints = new List<Vector3>();
                for (int i = 0; i < points.Count - 1; i++)
                {
                    Vector3 pA = points[i];
                    Vector3 pB = points[i + 1];
                    Vector3 mid = (pA + pB) * 0.5f;

                    // Độ lệch ngẫu nhiên vuông góc
                    float offsetMagnitude = Vector3.Distance(pA, pB) * 0.28f;
                    Vector3 randomOffset = Random.insideUnitSphere * offsetMagnitude;
                    mid += randomOffset;

                    newPoints.Add(pA);
                    newPoints.Add(mid);
                }
                newPoints.Add(points[points.Count - 1]);
                points = newPoints;
            }

            lr.positionCount = points.Count;
            lr.SetPositions(points.ToArray());

            // Tạo thêm 1-2 nhánh phụ (Fork branches)
            CreateForkBranch(points[Random.Range(5, points.Count / 2)], boltObj.transform);
            if (Random.value < 0.6f)
            {
                CreateForkBranch(points[Random.Range(points.Count / 2, points.Count - 5)], boltObj.transform);
            }

            // Nhấp nháy 2 lần trước khi biến mất
            float boltLifetime = Random.Range(0.08f, 0.16f);
            float elapsed = 0f;
            while (elapsed < boltLifetime)
            {
                elapsed += Time.deltaTime;
                lr.enabled = (Random.value > 0.15f); // Nháy chập chờn
                yield return null;
            }

            Destroy(boltObj);
        }

        private void CreateForkBranch(Vector3 branchStart, Transform parent)
        {
            GameObject forkObj = new GameObject("Fork_Branch");
            forkObj.transform.SetParent(parent);
            LineRenderer flr = forkObj.AddComponent<LineRenderer>();
            flr.material = boltMaterial;
            flr.startColor = new Color(0.8f, 0.9f, 1f, 0.7f);
            flr.endColor = new Color(0.6f, 0.75f, 1f, 0.3f);
            flr.startWidth = 0.45f;
            flr.endWidth = 0.08f;

            Vector3 forkEnd = branchStart + new Vector3(Random.Range(-30f, 30f), -Random.Range(20f, 40f), Random.Range(-30f, 30f));
            List<Vector3> pts = new List<Vector3> { branchStart, (branchStart + forkEnd) * 0.5f + Random.insideUnitSphere * 8f, forkEnd };
            flr.positionCount = pts.Count;
            flr.SetPositions(pts.ToArray());
        }

        // =========================================================================
        // ÁNH CHỚP NHẤP NHÁY ĐA XUNG (MULTI-PULSE FLASH)
        // =========================================================================
        private IEnumerator MultiPulseFlash(Vector3 strikePos, float distance)
        {
            if (flashLight == null) yield break;

            flashLight.transform.position = strikePos + Vector3.up * 40f;
            flashLight.enabled = true;

            // Xung 1: Chớp đầu nhẹ
            flashLight.intensity = maxFlashIntensity * 0.5f;
            yield return new WaitForSeconds(0.035f);

            // Chìm nhẹ
            flashLight.intensity = maxFlashIntensity * 0.15f;
            yield return new WaitForSeconds(0.025f);

            // Xung 2: Bùng nổ sáng lóa mắt
            flashLight.intensity = maxFlashIntensity;
            yield return new WaitForSeconds(0.065f);

            // Xung 3 (tùy biến)
            if (Random.value < 0.5f)
            {
                flashLight.intensity = maxFlashIntensity * 0.35f;
                yield return new WaitForSeconds(0.04f);
            }

            // Tắt dần mượt mà
            float fadeTime = 0.12f;
            float t = 0f;
            float startInt = flashLight.intensity;
            while (t < fadeTime)
            {
                t += Time.deltaTime;
                flashLight.intensity = Mathf.Lerp(startInt, 0f, t / fadeTime);
                yield return null;
            }

            flashLight.intensity = 0f;
            flashLight.enabled = false;
        }

        // =========================================================================
        // ÂM THANH SẤM TRUYỀN TỚI & RUNG MÀN HÌNH
        // =========================================================================
        private IEnumerator DelayedThunderSound(float distance, float delay, Vector3 strikePos)
        {
            yield return new WaitForSeconds(delay);

            // Kiểm tra nếu trong lúc delay mà vào menu hoặc trailer thì không phát
            if (IsBlockedByMenuOrTrailer())
            {
                yield break;
            }

            AudioClip clipToPlay = null;
            float volume = 1f;

            if (distance < 160f)
            {
                // Sét đánh gần (< 160m): Sấm nổ to
                if (thunderCloseClips != null && thunderCloseClips.Length > 0)
                {
                    clipToPlay = thunderCloseClips[Random.Range(0, thunderCloseClips.Length)];
                }
                volume = 1f;

                // Chỉ rung siêu vi mô nếu bật và khoảng cách cực sát người chơi (< 60m)
                if (enableCameraShake && distance < 60f)
                {
                    TriggerCameraShake(maxShakeMagnitude, 0.25f);
                }
            }
            else if (distance < 550f)
            {
                // Sét đánh tầm trung: Sấm rền vang, KHÔNG RUNG MÀN HÌNH
                if (thunderMediumClips != null && thunderMediumClips.Length > 0)
                {
                    clipToPlay = thunderMediumClips[Random.Range(0, thunderMediumClips.Length)];
                }
                volume = 0.85f;
            }
            else
            {
                // Sét đánh ở xa: Sấm rền trầm xa xăm, KHÔNG RUNG MÀN HÌNH
                if (thunderFarClips != null && thunderFarClips.Length > 0)
                {
                    clipToPlay = thunderFarClips[Random.Range(0, thunderFarClips.Length)];
                }
                volume = Mathf.Lerp(0.7f, 0.3f, (distance - 550f) / 1000f);
            }

            if (clipToPlay != null && thunderAudioSource != null)
            {
                thunderAudioSource.PlayOneShot(clipToPlay, volume);
            }
        }

        public void TriggerCameraShake(float magnitude, float duration)
        {
            if (!enableCameraShake) return;
            if (activeShakeRoutine != null) StopCoroutine(activeShakeRoutine);
            activeShakeRoutine = StartCoroutine(CameraShakeRoutine(magnitude, duration));
        }

        private IEnumerator CameraShakeRoutine(float magnitude, float duration)
        {
            FindPlayerAndCamera();
            if (targetCamera == null) yield break;

            Transform camTransform = targetCamera.transform;
            Vector3 previousOffset = Vector3.zero;
            float elapsed = 0f;

            // Rung vị trí vi mô cục bộ vài milimet, tuyệt đối KHÔNG can thiệp rotation chuột để tránh giật bản đồ
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float currentMag = Mathf.Lerp(magnitude, 0f, elapsed / duration);

                // Gỡ bỏ offset của frame trước
                camTransform.localPosition -= previousOffset;

                // Offset siêu nhỏ (chỉ vài mm)
                previousOffset = new Vector3(
                    Random.Range(-1f, 1f) * currentMag,
                    Random.Range(-1f, 1f) * currentMag * 0.6f,
                    0f
                );

                camTransform.localPosition += previousOffset;
                yield return null;
            }

            // Hoàn trả vị trí gốc
            camTransform.localPosition -= previousOffset;
        }

        // =========================================================================
        // KHỞI TẠO TỰ ĐỘNG (AUTO SETUP)
        // =========================================================================
        private void EnsureLightningLight()
        {
            if (flashLight == null)
            {
                GameObject lightObj = new GameObject("Lightning_Flash_Light");
                lightObj.transform.SetParent(this.transform);
                flashLight = lightObj.AddComponent<Light>();
                flashLight.type = LightType.Directional;
                flashLight.color = flashColor;
                flashLight.intensity = 0f;
                flashLight.enabled = false;
                flashLight.shadows = LightShadows.None;
                flashLight.transform.rotation = Quaternion.Euler(75f, 30f, 0f);
            }
        }

        private void EnsureAudioSources()
        {
            if (thunderAudioSource == null)
            {
                thunderAudioSource = gameObject.AddComponent<AudioSource>();
                thunderAudioSource.playOnAwake = false;
                thunderAudioSource.spatialBlend = 0f; // 2D âm thanh chân thực bao quanh tai người chơi
                thunderAudioSource.volume = 1f;
            }

#if UNITY_EDITOR
            // Tự nạp các file âm thanh sấm đã tạo trong project
            string basePath = "Assets/Flooded_Grounds/Content/Sounds/Weather/";
            if (thunderCloseClips == null || thunderCloseClips.Length == 0)
            {
                var c1 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(basePath + "Thunder_Close_1.wav");
                var c2 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(basePath + "Thunder_Close_2.wav");
                if (c1 != null && c2 != null) thunderCloseClips = new AudioClip[] { c1, c2 };
            }
            if (thunderMediumClips == null || thunderMediumClips.Length == 0)
            {
                var m1 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(basePath + "Thunder_Medium_1.wav");
                var m2 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(basePath + "Thunder_Medium_2.wav");
                if (m1 != null && m2 != null) thunderMediumClips = new AudioClip[] { m1, m2 };
            }
            if (thunderFarClips == null || thunderFarClips.Length == 0)
            {
                var f1 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(basePath + "Thunder_Far_1.wav");
                var f2 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(basePath + "Thunder_Far_2.wav");
                if (f1 != null && f2 != null) thunderFarClips = new AudioClip[] { f1, f2 };
            }
#endif
        }

        private void EnsureBoltMaterial()
        {
            if (boltMaterial == null)
            {
                Shader shader = Shader.Find("Particles/Additive");
                if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
                if (shader == null) shader = Shader.Find("Sprites/Default");

                boltMaterial = new Material(shader);
                boltMaterial.name = "Weather_Bolt_Mat";
                boltMaterial.color = Color.white;
            }
        }
    }
}

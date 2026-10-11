using UnityEngine;
using HorrorGame.Player;
using System.Collections;

namespace HorrorGame.Effects
{
    /// <summary>
    /// Hệ thống Ảo Giác & Phát Điên phong cách Green Hell (Sanity Hallucinations System):
    /// - Mức 1 (Sanity < 50%): Ảo thanh thì thầm xa xăm, nhịp tim hồi hộp khi ở trong bóng tối
    /// - Mức 2 (Sanity < 35%): Say sóng, chóng mặt chao đảo góc nhìn (Dizziness Tilt), viền màn hình tối sẫm
    /// - Mức 3 (Sanity < 25%): Ảo giác bóng ma thổ dân (Phantom Shadows) thoắt ẩn thoắt hiện trong rặng cây
    /// - Mức 4 (Sanity < 15%): Hoảng loạn cực độ, màn hình run rẩy méo mó, bóng ma lao đến rồi tan biến
    /// - Khi nghỉ ngơi ở Giường/Lều hoặc sưởi ấm bên Lửa Trại -> Sanity hồi phục và ảo giác biến mất!
    /// </summary>
    public class SanityEffects : MonoBehaviour
    {
        public PlayerStats stats;

        [Header("── Âm Thanh Dị Tượng & Thì Thầm (Paranoia Audio) ──")]
        public AudioSource paranoiaAudioSource;
        public AudioClip whisperClip;
        public AudioClip heartbeatClip;
        public float audioTriggerChance = 0.6f;

        [Header("── Hiệu Ứng Chao Đảo & Rung Giật (Dizziness & Jitter) ──")]
        public Transform playerCamera;
        public float maxTiltAngle = 14f;
        public float tiltSpeed = 2.2f;

        // Trạng thái ảo giác
        private float nextAudioCheckTime = 0f;
        private float nextPhantomCheckTime = 0f;
        private GameObject currentPhantomObj = null;
        private Texture2D vignetteTexture;
        private bool warnedLowSanity = false;

        void Start()
        {
            if (stats == null) stats = GetComponent<PlayerStats>();
            if (stats == null) stats = FindObjectOfType<PlayerStats>();

            if (playerCamera == null)
            {
                Camera cam = Camera.main;
                if (cam != null) playerCamera = cam.transform;
                else
                {
                    var pc = FindObjectOfType<PlayerController>();
                    if (pc != null && pc.playerCamera != null) playerCamera = pc.playerCamera;
                }
            }

            SetupAudio();
            CreateVignetteTexture();
        }

        private void SetupAudio()
        {
            if (paranoiaAudioSource == null)
            {
                paranoiaAudioSource = gameObject.AddComponent<AudioSource>();
                paranoiaAudioSource.playOnAwake = false;
                paranoiaAudioSource.spatialBlend = 0f; // 2D âm thanh vang trong đầu người chơi
                paranoiaAudioSource.volume = 0.5f;
            }

            // Tự load clip kinh dị nếu chưa có
            if (whisperClip == null)
            {
                whisperClip = Resources.Load<AudioClip>("Sounds/WindHowl");
                if (whisperClip == null)
                {
#if UNITY_EDITOR
                    whisperClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/WindHowl.mp3");
                    if (whisperClip == null)
                        whisperClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/DeepRattle.mp3");
#endif
                }
            }
        }

        private void CreateVignetteTexture()
        {
            // Tạo texture viền đen mịn để vẽ vignette
            int size = 128;
            vignetteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxDist = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                    float alpha = Mathf.Clamp01(Mathf.Pow(dist, 2.5f));
                    colors[y * size + x] = new Color(0f, 0f, 0f, alpha);
                }
            }
            vignetteTexture.SetPixels(colors);
            vignetteTexture.Apply();
        }

        void LateUpdate()
        {
            if (stats == null) return;
            if (playerCamera == null)
            {
                Camera cam = Camera.main;
                if (cam != null) playerCamera = cam.transform;
                else return;
            }

            float sanity = stats.currentSanity;

            // 1. CHAO ĐẢO GÓC NHÌN & RUNG GIẬT
            if (sanity < 45f)
            {
                HandleDizziness(sanity);
            }
            else
            {
                RecoverCamera();
            }

            // 2. ẢO THANH THÌ THẦM
            if (sanity < 55f)
            {
                HandleParanoiaAudio(sanity);
            }

            // 3. ẢO GIÁC BÓNG MA THỔ DÂN (PHANTOM SHADOWS)
            if (sanity < 30f)
            {
                HandlePhantomSpawning();
                if (!warnedLowSanity)
                {
                    warnedLowSanity = true;
                    stats.ShowStatusNotice("⚠️ Tinh thần đang suy sụp! Bạn bắt đầu bị ảo giác. Hãy tìm lửa trại hoặc giường ngủ!");
                }
            }
            else
            {
                warnedLowSanity = false;
            }
        }

        /// <summary>
        /// Xoay nghiêng chao đảo camera theo hàm sin và rung giật hoảng loạn
        /// </summary>
        private void HandleDizziness(float sanity)
        {
            float intensity = 1f - (sanity / 45f); // 0 -> 1

            // Nghiêng chao đảo trục Z
            float zTilt = Mathf.Sin(Time.time * tiltSpeed) * (maxTiltAngle * intensity);

            // Thêm rung giật jitter nhẹ nếu Sanity dưới 15% (hoảng loạn cực độ)
            float xJitter = 0f;
            float yJitter = 0f;
            if (sanity < 15f)
            {
                float panicIntensity = 1f - (sanity / 15f);
                xJitter = (Mathf.PerlinNoise(Time.time * 12f, 0f) - 0.5f) * 1.5f * panicIntensity;
                yJitter = (Mathf.PerlinNoise(0f, Time.time * 12f) - 0.5f) * 1.5f * panicIntensity;
            }

            Vector3 rot = playerCamera.localEulerAngles;
            rot.z = zTilt;
            rot.x += xJitter;
            rot.y += yJitter;
            playerCamera.localEulerAngles = rot;
        }

        private void RecoverCamera()
        {
            float currentZ = playerCamera.localEulerAngles.z;
            if (currentZ > 180f) currentZ -= 360f;

            if (Mathf.Abs(currentZ) > 0.05f)
            {
                float newZ = Mathf.Lerp(currentZ, 0f, Time.deltaTime * 4f);
                Vector3 rot = playerCamera.localEulerAngles;
                rot.z = newZ;
                playerCamera.localEulerAngles = rot;
            }
        }

        /// <summary>
        /// Phát tiếng thì thầm ma quái bóp méo âm thanh
        /// </summary>
        private void HandleParanoiaAudio(float sanity)
        {
            if (paranoiaAudioSource == null) return;

            if (Time.time >= nextAudioCheckTime)
            {
                // Sanity càng thấp, tần suất tiếng thì thầm càng dày
                float checkInterval = Mathf.Lerp(3f, 8f, sanity / 55f);
                nextAudioCheckTime = Time.time + checkInterval;

                if (Random.value < audioTriggerChance && !paranoiaAudioSource.isPlaying)
                {
                    if (whisperClip != null)
                    {
                        paranoiaAudioSource.clip = whisperClip;
                        paranoiaAudioSource.pitch = Random.Range(0.75f, 1.25f);
                        paranoiaAudioSource.volume = Mathf.Lerp(0.6f, 0.2f, sanity / 55f);
                        paranoiaAudioSource.Play();
                    }
                }
            }
        }

        /// <summary>
        /// Sinh ra bóng ma thổ dân mờ ảo (Phantom Native Hallucination) phong cách Green Hell
        /// </summary>
        private void HandlePhantomSpawning()
        {
            if (currentPhantomObj != null) return; // Đang có 1 ảo ảnh thì chờ nó biến mất

            if (Time.time >= nextPhantomCheckTime)
            {
                // Cứ mỗi 25-45s thử tạo 1 bóng ma
                float interval = Random.Range(22f, 40f);
                nextPhantomCheckTime = Time.time + interval;

                SpawnPhantomShadow();
            }
        }

        private void SpawnPhantomShadow()
        {
            if (playerCamera == null) return;

            // Chọn vị trí ở rìa tầm nhìn người chơi (khoảng cách 12 - 16m)
            Vector3 camForward = playerCamera.forward;
            camForward.y = 0;
            camForward.Normalize();

            // Góc lệch ngẫu nhiên khoảng 25 đến 55 độ sang trái hoặc phải
            float sideAngle = Random.Range(25f, 55f) * (Random.value > 0.5f ? 1f : -1f);
            Vector3 spawnDir = Quaternion.Euler(0, sideAngle, 0) * camForward;
            float distance = Random.Range(12f, 16f);

            Vector3 spawnPos = playerCamera.position + spawnDir * distance;
            // Bám sát mặt đất
            RaycastHit hit;
            if (Physics.Raycast(spawnPos + Vector3.up * 8f, Vector3.down, out hit, 20f))
            {
                spawnPos = hit.point;
            }

            // Tạo GameObject bóng ma thổ dân
            currentPhantomObj = new GameObject("[PhantomHallucination_Native]");
            currentPhantomObj.transform.position = spawnPos;

            // Xoay mặt nhìn chằm chằm về phía người chơi
            Vector3 lookDir = playerCamera.position - spawnPos;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
                currentPhantomObj.transform.rotation = Quaternion.LookRotation(lookDir);

            // Dựng hình bóng người đen tuyền mờ ảo (Silhouette)
            CreatePhantomMesh(currentPhantomObj);

            // Bắt đầu Coroutine theo dõi và tự tan biến sau 3.5 giây hoặc khi người chơi nhìn thẳng
            StartCoroutine(PhantomLifecycleRoutine(currentPhantomObj));
        }

        private void CreatePhantomMesh(GameObject root)
        {
            // Material đen bóng mờ ma quái
            Material ghostMat = new Material(Shader.Find("Standard"));
            ghostMat.color = new Color(0.04f, 0.04f, 0.05f, 0.65f); // Đen mờ sương khói
            ghostMat.SetFloat("_Mode", 3); // Transparent
            ghostMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            ghostMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            ghostMat.SetInt("_ZWrite", 0);
            ghostMat.renderQueue = 3000;

            // Thân người (Cylinder)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "PhantomBody";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0, 0.95f, 0);
            body.transform.localScale = new Vector3(0.55f, 0.75f, 0.35f);
            Destroy(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().material = ghostMat;

            // Đầu (Sphere)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "PhantomHead";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0, 1.8f, 0);
            head.transform.localScale = new Vector3(0.38f, 0.42f, 0.38f);
            Destroy(head.GetComponent<Collider>());
            head.GetComponent<Renderer>().material = ghostMat;

            // Mắt phát sáng đỏ mờ ma mị (Green Hell Native look)
            for (int eye = -1; eye <= 1; eye += 2)
            {
                GameObject eyeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eyeObj.name = "PhantomEye";
                eyeObj.transform.SetParent(root.transform, false);
                eyeObj.transform.localPosition = new Vector3(eye * 0.08f, 1.83f, 0.17f);
                eyeObj.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                Destroy(eyeObj.GetComponent<Collider>());

                Material eyeMat = new Material(Shader.Find("Standard"));
                eyeMat.color = new Color(1f, 0.15f, 0.1f, 0.85f);
                eyeMat.EnableKeyword("_EMISSION");
                eyeMat.SetColor("_EmissionColor", new Color(1f, 0.1f, 0.05f) * 1.5f);
                eyeObj.GetComponent<Renderer>().material = eyeMat;
            }
        }

        private IEnumerator PhantomLifecycleRoutine(GameObject phantom)
        {
            float lifetime = 3.6f;
            float timer = 0f;

            while (timer < lifetime && phantom != null)
            {
                timer += Time.deltaTime;

                // Nếu người chơi quay camera nhìn thẳng vào bóng ma (Dot product > 0.88f)
                if (playerCamera != null)
                {
                    Vector3 toPhantom = (phantom.transform.position + Vector3.up * 1.5f - playerCamera.position).normalized;
                    float dot = Vector3.Dot(playerCamera.forward, toPhantom);
                    float dist = Vector3.Distance(playerCamera.position, phantom.transform.position);

                    // Khi người chơi nhìn thẳng hoặc tiến lại gần < 6m: Bóng ma giật mình tan biến!
                    if (dot > 0.88f || dist < 6.0f)
                    {
                        yield return new WaitForSeconds(0.4f); // Thoáng thấy trong 0.4s rồi tan biến
                        break;
                    }
                }

                yield return null;
            }

            if (phantom != null)
            {
                // Hiệu ứng âm thanh tan biến nhẹ
                if (paranoiaAudioSource != null && !paranoiaAudioSource.isPlaying && whisperClip != null)
                {
                    paranoiaAudioSource.PlayOneShot(whisperClip, 0.4f);
                }
                Destroy(phantom);
            }
            currentPhantomObj = null;
        }

        void OnGUI()
        {
            if (stats == null) return;
            float sanity = stats.currentSanity;

            // Vẽ viền đen Vignette khi Sanity < 35%
            if (sanity < 35f && vignetteTexture != null)
            {
                float intensity = 1f - (sanity / 35f);
                // Nhịp thở tối dần theo hàm Sin (Breathing Vignette)
                float pulse = 0.8f + Mathf.Sin(Time.time * 2.5f) * 0.2f;
                float alpha = Mathf.Clamp01(intensity * 0.75f * pulse);

                Color oldColor = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, alpha);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignetteTexture, ScaleMode.StretchToFill);
                GUI.color = oldColor;
            }
        }
    }
}

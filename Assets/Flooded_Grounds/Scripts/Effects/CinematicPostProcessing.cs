using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace HorrorGame.Effects
{
    /// <summary>
    /// Quản lý tất cả hiệu ứng post-processing cinematic cho cutscene mà không cần Unity Post-Processing Stack.
    /// Tạo toàn bộ overlay UI tự động trong Runtime.
    /// </summary>
    public class CinematicPostProcessing : MonoBehaviour
    {
        // ──────────────────────────────────────────────
        // SINGLETON
        // ──────────────────────────────────────────────
        public static CinematicPostProcessing Instance { get; private set; }

        // ──────────────────────────────────────────────
        // OVERLAY REFERENCES (auto-created)
        // ──────────────────────────────────────────────
        private Canvas ppCanvas;
        private Image vignetteImage;
        private Image colorTintImage;
        private Image filmGrainImage;
        private Image screenDamageImage;

        // Chromatic Aberration: 3 layers RGB tách biệt
        private Image chromR, chromG, chromB;

        // Letterbox bars
        private Image letterboxTop, letterboxBottom;

        // ──────────────────────────────────────────────
        // STATE
        // ──────────────────────────────────────────────
        private float vignetteAlpha = 0f;
        private Color vignetteColor = Color.black;
        private float tintAlpha = 0f;
        private Color tintColor = Color.clear;
        private float chromAberration = 0f;
        private float grainAlpha = 0f;
        private float grainSpeed = 8f;
        private float damageAlpha = 0f;
        private float letterboxAmount = 0f; // 0 = no bars, 1 = full cinematic

        // Film grain texture
        private Texture2D grainTexture;
        private float grainTimer = 0f;

        // ──────────────────────────────────────────────
        // UNITY LIFECYCLE
        // ──────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            CreateOverlayCanvas();
            GenerateGrainTexture();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            UpdateVignette();
            UpdateColorTint();
            UpdateChromaticAberration();
            UpdateFilmGrain();
            UpdateScreenDamage();
            UpdateLetterbox();
        }

        // ──────────────────────────────────────────────
        // PUBLIC API
        // ──────────────────────────────────────────────

        /// <summary>Bật/tắt vignette (tối viền) với màu và cường độ chỉ định</summary>
        public void SetVignette(float alpha, Color color)
        {
            vignetteAlpha = Mathf.Clamp01(alpha);
            vignetteColor = color;
        }

        /// <summary>Fade vignette mượt mà</summary>
        public Coroutine FadeVignette(float targetAlpha, Color color, float duration)
        {
            return StartCoroutine(FadeVignetteRoutine(targetAlpha, color, duration));
        }

        /// <summary>Đặt color tint overlay (ví dụ warm, cold, red danger)</summary>
        public void SetColorTint(Color color, float alpha)
        {
            tintColor = color;
            tintAlpha = Mathf.Clamp01(alpha);
        }

        /// <summary>Fade color tint mượt mà</summary>
        public Coroutine FadeColorTint(Color targetColor, float targetAlpha, float duration)
        {
            return StartCoroutine(FadeColorTintRoutine(targetColor, targetAlpha, duration));
        }

        /// <summary>Đặt chromatic aberration (tách RGB) — 0 = tắt, 1 = max</summary>
        public void SetChromaticAberration(float intensity)
        {
            chromAberration = Mathf.Clamp01(intensity);
        }

        /// <summary>Pulse chromatic aberration (flash lên rồi giảm dần)</summary>
        public Coroutine PulseChromaticAberration(float peakIntensity, float duration)
        {
            return StartCoroutine(PulseChromRoutine(peakIntensity, duration));
        }

        /// <summary>Bật/tắt film grain (nhiễu hạt phim)</summary>
        public void SetFilmGrain(float alpha, float speed = 8f)
        {
            grainAlpha = Mathf.Clamp01(alpha);
            grainSpeed = speed;
        }

        /// <summary>Hiển thị vết nứt/damage trên màn hình — 0 = ẩn, 1 = rõ nét</summary>
        public void SetScreenDamage(float alpha)
        {
            damageAlpha = Mathf.Clamp01(alpha);
        }

        /// <summary>Fade screen damage mượt mà</summary>
        public Coroutine FadeScreenDamage(float targetAlpha, float duration)
        {
            return StartCoroutine(FadeScreenDamageRoutine(targetAlpha, duration));
        }

        /// <summary>Đặt mức letterbox — 0 = không có, 1 = dải đen cinematic 2.39:1</summary>
        public void SetLetterbox(float amount)
        {
            letterboxAmount = Mathf.Clamp01(amount);
        }

        /// <summary>Animate letterbox mở ra/đóng lại</summary>
        public Coroutine AnimateLetterbox(float targetAmount, float duration)
        {
            return StartCoroutine(AnimateLetterboxRoutine(targetAmount, duration));
        }

        /// <summary>Reset tất cả hiệu ứng về mặc định</summary>
        public void ResetAll()
        {
            vignetteAlpha = 0f;
            tintAlpha = 0f;
            chromAberration = 0f;
            grainAlpha = 0f;
            damageAlpha = 0f;
            letterboxAmount = 0f;
        }

        // ──────────────────────────────────────────────
        // CANVAS & OVERLAY CREATION
        // ──────────────────────────────────────────────
        private void CreateOverlayCanvas()
        {
            // Tạo Canvas riêng biệt render trên tất cả
            GameObject canvasObj = new GameObject("CinematicPP_Canvas");
            canvasObj.transform.SetParent(transform, false);
            ppCanvas = canvasObj.AddComponent<Canvas>();
            ppCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            ppCanvas.sortingOrder = 9990; // Dưới Cutscene Canvas (9999) nhưng trên tất cả UI khác
            canvasObj.AddComponent<CanvasScaler>();

            // ── Vignette ──
            vignetteImage = CreateFullscreenImage("PP_Vignette", ppCanvas.transform);
            ApplyRadialGradient(vignetteImage, Color.black);
            vignetteImage.color = new Color(0, 0, 0, 0);

            // ── Color Tint ──
            colorTintImage = CreateFullscreenImage("PP_ColorTint", ppCanvas.transform);
            colorTintImage.color = Color.clear;

            // ── Chromatic Aberration (3 RGB layers) ──
            chromR = CreateFullscreenImage("PP_Chrom_R", ppCanvas.transform);
            chromG = CreateFullscreenImage("PP_Chrom_G", ppCanvas.transform);
            chromB = CreateFullscreenImage("PP_Chrom_B", ppCanvas.transform);
            chromR.color = new Color(1, 0, 0, 0);
            chromG.color = new Color(0, 1, 0, 0);
            chromB.color = new Color(0, 0, 1, 0);

            // ── Film Grain ──
            filmGrainImage = CreateFullscreenImage("PP_FilmGrain", ppCanvas.transform);
            filmGrainImage.color = new Color(1, 1, 1, 0);

            // ── Screen Damage Overlay ──
            screenDamageImage = CreateFullscreenImage("PP_ScreenDamage", ppCanvas.transform);
            ApplyScreenDamagePattern(screenDamageImage);
            screenDamageImage.color = new Color(1, 1, 1, 0);

            // ── Letterbox Bars ──
            letterboxTop = CreateLetterboxBar("PP_Letterbox_Top", ppCanvas.transform, true);
            letterboxBottom = CreateLetterboxBar("PP_Letterbox_Bottom", ppCanvas.transform, false);
        }

        private Image CreateFullscreenImage(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image img = obj.AddComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        private Image CreateLetterboxBar(string name, Transform parent, bool isTop)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();

            if (isTop)
            {
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
            }
            else
            {
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(0.5f, 0);
            }
            rt.sizeDelta = new Vector2(0, 0);

            Image img = obj.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;
            return img;
        }

        // ──────────────────────────────────────────────
        // TEXTURE GENERATION
        // ──────────────────────────────────────────────

        /// <summary>Tạo gradient hình tròn cho vignette (tối ở viền, trong suốt ở trung tâm)</summary>
        private void ApplyRadialGradient(Image img, Color edgeColor)
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Smooth vignette curve: trong suốt ở tâm, đậm dần ra viền
                    float alpha = Mathf.SmoothStep(0.35f, 1.2f, dist);
                    alpha = Mathf.Clamp01(alpha);

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();

            img.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
        }

        /// <summary>Tạo pattern vết nứt kính / damage overlay procedural</summary>
        private void ApplyScreenDamagePattern(Image img)
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size * 0.5f;

            // Tạo pattern vết nứt tỏa từ nhiều tâm
            Vector2[] crackCenters = new Vector2[]
            {
                new Vector2(0.35f, 0.55f),
                new Vector2(0.7f, 0.4f),
                new Vector2(0.5f, 0.75f),
                new Vector2(0.25f, 0.3f),
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = (float)x / size;
                    float py = (float)y / size;
                    float alpha = 0f;

                    // Vignette viền damage
                    float edgeDist = Mathf.Min(px, 1f - px, py, 1f - py);
                    alpha += Mathf.SmoothStep(0.15f, 0f, edgeDist) * 0.4f;

                    // Vết nứt tỏa ra từ các tâm
                    foreach (var cc in crackCenters)
                    {
                        float dist = Vector2.Distance(new Vector2(px, py), cc);
                        // Tạo đường nứt bằng high-frequency noise
                        float noise = Mathf.PerlinNoise(px * 50f + cc.x * 100f, py * 50f + cc.y * 100f);
                        float crackLine = Mathf.Abs(noise - 0.5f);
                        float crackAlpha = Mathf.SmoothStep(0.08f, 0.01f, crackLine) * Mathf.SmoothStep(0.5f, 0.05f, dist);
                        alpha += crackAlpha * 0.6f;
                    }

                    alpha = Mathf.Clamp01(alpha);
                    // Màu trắng xước + đỏ viền
                    float r = Mathf.Lerp(1f, 0.8f, alpha);
                    float g = Mathf.Lerp(1f, 0.2f, alpha * 0.5f);
                    float b = Mathf.Lerp(1f, 0.15f, alpha * 0.5f);
                    tex.SetPixel(x, y, new Color(r, g, b, alpha));
                }
            }
            tex.Apply();

            img.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
        }

        /// <summary>Tạo texture noise cho film grain</summary>
        private void GenerateGrainTexture()
        {
            int size = 128;
            grainTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            grainTexture.filterMode = FilterMode.Point;
            RegenerateGrainPixels();
        }

        private void RegenerateGrainPixels()
        {
            if (grainTexture == null) return;
            int size = grainTexture.width;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float v = Random.Range(0f, 1f);
                    grainTexture.SetPixel(x, y, new Color(v, v, v, 0.15f));
                }
            }
            grainTexture.Apply();
        }

        // ──────────────────────────────────────────────
        // UPDATE METHODS
        // ──────────────────────────────────────────────

        private void UpdateVignette()
        {
            if (vignetteImage == null) return;
            vignetteImage.gameObject.SetActive(vignetteAlpha > 0.01f);
            if (vignetteAlpha > 0.01f)
            {
                vignetteImage.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, vignetteAlpha);
            }
        }

        private void UpdateColorTint()
        {
            if (colorTintImage == null) return;
            colorTintImage.gameObject.SetActive(tintAlpha > 0.01f);
            if (tintAlpha > 0.01f)
            {
                colorTintImage.color = new Color(tintColor.r, tintColor.g, tintColor.b, tintAlpha);
            }
        }

        private void UpdateChromaticAberration()
        {
            bool active = chromAberration > 0.01f;
            if (chromR != null) chromR.gameObject.SetActive(active);
            if (chromG != null) chromG.gameObject.SetActive(active);
            if (chromB != null) chromB.gameObject.SetActive(active);

            if (active)
            {
                float offset = chromAberration * 8f; // Pixel offset
                float alpha = chromAberration * 0.15f;

                // Offset RectTransform của mỗi channel
                if (chromR != null)
                {
                    RectTransform rt = chromR.GetComponent<RectTransform>();
                    rt.offsetMin = new Vector2(-offset, -offset * 0.5f);
                    rt.offsetMax = new Vector2(offset, offset * 0.5f);
                    chromR.color = new Color(1, 0, 0, alpha);
                }
                if (chromG != null)
                {
                    // Green ở giữa, nhẹ
                    chromG.color = new Color(0, 1, 0, alpha * 0.5f);
                }
                if (chromB != null)
                {
                    RectTransform rt = chromB.GetComponent<RectTransform>();
                    rt.offsetMin = new Vector2(offset * 0.5f, offset);
                    rt.offsetMax = new Vector2(-offset * 0.5f, -offset);
                    chromB.color = new Color(0, 0, 1, alpha);
                }
            }
        }

        private void UpdateFilmGrain()
        {
            if (filmGrainImage != null)
            {
                filmGrainImage.gameObject.SetActive(false);
            }
        }

        private void UpdateScreenDamage()
        {
            if (screenDamageImage == null) return;
            screenDamageImage.gameObject.SetActive(damageAlpha > 0.01f);
            if (damageAlpha > 0.01f)
            {
                screenDamageImage.color = new Color(1, 1, 1, damageAlpha);
            }
        }

        private void UpdateLetterbox()
        {
            if (letterboxTop == null || letterboxBottom == null) return;
            bool active = letterboxAmount > 0.01f;
            letterboxTop.gameObject.SetActive(active);
            letterboxBottom.gameObject.SetActive(active);

            if (active)
            {
                // Tỉ lệ 2.39:1 cinematic → mỗi bar chiếm ~12% chiều cao màn hình
                float barHeight = Screen.height * 0.12f * letterboxAmount;
                RectTransform rtTop = letterboxTop.GetComponent<RectTransform>();
                RectTransform rtBot = letterboxBottom.GetComponent<RectTransform>();
                rtTop.sizeDelta = new Vector2(0, barHeight);
                rtBot.sizeDelta = new Vector2(0, barHeight);
            }
        }

        // ──────────────────────────────────────────────
        // COROUTINE HELPERS
        // ──────────────────────────────────────────────

        private IEnumerator FadeVignetteRoutine(float targetAlpha, Color targetColor, float duration)
        {
            float startAlpha = vignetteAlpha;
            Color startColor = vignetteColor;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, t / duration);
                vignetteAlpha = Mathf.Lerp(startAlpha, targetAlpha, p);
                vignetteColor = Color.Lerp(startColor, targetColor, p);
                yield return null;
            }
            vignetteAlpha = targetAlpha;
            vignetteColor = targetColor;
        }

        private IEnumerator FadeColorTintRoutine(Color targetColor, float targetAlpha, float duration)
        {
            float startAlpha = tintAlpha;
            Color startColor = tintColor;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, t / duration);
                tintAlpha = Mathf.Lerp(startAlpha, targetAlpha, p);
                tintColor = Color.Lerp(startColor, targetColor, p);
                yield return null;
            }
            tintAlpha = targetAlpha;
            tintColor = targetColor;
        }

        private IEnumerator PulseChromRoutine(float peakIntensity, float duration)
        {
            float startIntensity = chromAberration;
            // Attack (30%)
            float attackDur = duration * 0.3f;
            float t = 0f;
            while (t < attackDur)
            {
                t += Time.deltaTime;
                chromAberration = Mathf.Lerp(startIntensity, peakIntensity, t / attackDur);
                yield return null;
            }
            // Decay (70%)
            float decayDur = duration * 0.7f;
            t = 0f;
            while (t < decayDur)
            {
                t += Time.deltaTime;
                float p = t / decayDur;
                chromAberration = Mathf.Lerp(peakIntensity, 0f, p * p); // Quadratic decay
                yield return null;
            }
            chromAberration = 0f;
        }

        private IEnumerator FadeScreenDamageRoutine(float targetAlpha, float duration)
        {
            float startAlpha = damageAlpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, t / duration);
                damageAlpha = Mathf.Lerp(startAlpha, targetAlpha, p);
                yield return null;
            }
            damageAlpha = targetAlpha;
        }

        private IEnumerator AnimateLetterboxRoutine(float targetAmount, float duration)
        {
            float startAmount = letterboxAmount;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, t / duration);
                letterboxAmount = Mathf.Lerp(startAmount, targetAmount, p);
                yield return null;
            }
            letterboxAmount = targetAmount;
        }
    }
}

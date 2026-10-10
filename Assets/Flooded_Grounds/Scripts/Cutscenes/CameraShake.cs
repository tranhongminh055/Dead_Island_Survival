using UnityEngine;
using System.Collections;

namespace HorrorGame.Cutscenes
{
    public class CameraShake : MonoBehaviour
    {
        private Vector3 originalPos;
        private Quaternion originalRot;

        private float currentShakeDuration = 0f;
        private float currentShakeMagnitude = 0.7f;
        private float currentShakeRoughness = 1f;
        private float shakeElapsed = 0f;
        private float totalShakeDuration = 0f;

        private bool isShaking = false;

        // ── Directional Shake ──
        private bool isDirectionalShake = false;
        private Vector3 directionalAxis = Vector3.up;

        // ── Perlin Noise Seeds (unique per shake instance) ──
        private float noiseSeedX;
        private float noiseSeedY;
        private float noiseSeedZ;
        private float noiseSeedRX;
        private float noiseSeedRY;
        private float noiseSeedRZ;

        /// <summary>Khi bật (ví dụ người chơi tự do lia chuột trong cabin máy bay), CameraShake sẽ không ghi đè trực tiếp localRotation</summary>
        public bool preventRotationOverride = false;
        [HideInInspector]
        public Vector3 currentRotJitter = Vector3.zero;

        void OnEnable()
        {
            originalPos = transform.localPosition;
            originalRot = transform.localRotation;
            RandomizeSeeds();
        }

        private void RandomizeSeeds()
        {
            noiseSeedX  = Random.Range(0f, 1000f);
            noiseSeedY  = Random.Range(0f, 1000f);
            noiseSeedZ  = Random.Range(0f, 1000f);
            noiseSeedRX = Random.Range(0f, 1000f);
            noiseSeedRY = Random.Range(0f, 1000f);
            noiseSeedRZ = Random.Range(0f, 1000f);
        }

        /// <summary>Multi-octave Perlin noise cho chuyển động organic, mượt mà</summary>
        private float PerlinOctaves(float seed, float time, float frequency, int octaves = 3)
        {
            float value = 0f;
            float amplitude = 1f;
            float totalAmplitude = 0f;
            float freq = frequency;

            for (int i = 0; i < octaves; i++)
            {
                // Perlin trả về [0,1], ta shift về [-1,1]
                value += (Mathf.PerlinNoise(seed + time * freq, seed * 0.5f) * 2f - 1f) * amplitude;
                totalAmplitude += amplitude;
                amplitude *= 0.5f;
                freq *= 2f;
            }

            return value / totalAmplitude;
        }

        void Update()
        {
            if (isShaking)
            {
                if (currentShakeDuration > 0)
                {
                    shakeElapsed += Time.deltaTime;

                    // Decay envelope: rung mạnh dần rồi tắt dần — giống sóng xung thực tế
                    float progress = totalShakeDuration > 0f ? (shakeElapsed / totalShakeDuration) : 0f;
                    float envelope = EaseOutEnvelope(progress);

                    float mag = currentShakeMagnitude * envelope;
                    float freq = currentShakeRoughness * 4f; // Base frequency

                    // ── Position Shake (Perlin Noise) ──
                    Vector3 posOffset;
                    if (isDirectionalShake)
                    {
                        // Directional: chủ yếu rung theo trục chỉ định (ví dụ Y khi rơi)
                        float mainAxis = PerlinOctaves(noiseSeedX, shakeElapsed, freq * 1.5f) * mag * 0.04f;
                        float crossA   = PerlinOctaves(noiseSeedY, shakeElapsed, freq * 0.8f) * mag * 0.012f;
                        float crossB   = PerlinOctaves(noiseSeedZ, shakeElapsed, freq * 0.6f) * mag * 0.012f;

                        // Tạo 2 vector vuông góc với trục chính
                        Vector3 perpA = Vector3.Cross(directionalAxis, Vector3.right).normalized;
                        if (perpA.sqrMagnitude < 0.01f) perpA = Vector3.Cross(directionalAxis, Vector3.forward).normalized;
                        Vector3 perpB = Vector3.Cross(directionalAxis, perpA).normalized;

                        posOffset = directionalAxis * mainAxis + perpA * crossA + perpB * crossB;
                    }
                    else
                    {
                        // Omni-directional: rung đều tất cả hướng
                        posOffset = new Vector3(
                            PerlinOctaves(noiseSeedX, shakeElapsed, freq) * mag * 0.02f,
                            PerlinOctaves(noiseSeedY, shakeElapsed, freq * 1.1f) * mag * 0.02f,
                            PerlinOctaves(noiseSeedZ, shakeElapsed, freq * 0.9f) * mag * 0.015f
                        );
                    }

                    // ── Rotation Shake (Perlin Noise) ──
                    currentRotJitter = new Vector3(
                        PerlinOctaves(noiseSeedRX, shakeElapsed, freq * 0.8f) * mag * 5f,   // Pitch
                        PerlinOctaves(noiseSeedRY, shakeElapsed, freq * 0.7f) * mag * 5f,   // Yaw
                        PerlinOctaves(noiseSeedRZ, shakeElapsed, freq * 0.5f) * mag * 1.8f  // Roll (nhẹ hơn)
                    );

                    // Smooth interpolation để tránh nhảy đột ngột
                    float lerpSpeed = currentShakeRoughness * 12f;
                    transform.localPosition = Vector3.Lerp(transform.localPosition, originalPos + posOffset, Time.deltaTime * lerpSpeed);

                    if (!preventRotationOverride)
                    {
                        Quaternion targetRot = originalRot * Quaternion.Euler(currentRotJitter);
                        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * lerpSpeed);
                    }

                    currentShakeDuration -= Time.deltaTime;
                }
                else
                {
                    // Smooth return to origin thay vì snap đột ngột
                    currentShakeDuration = 0f;
                    isShaking = false;
                    isDirectionalShake = false;
                    currentRotJitter = Vector3.zero;
                    transform.localPosition = originalPos;
                    if (!preventRotationOverride)
                    {
                        transform.localRotation = originalRot;
                    }
                }
            }
            else
            {
                currentRotJitter = Vector3.zero;
            }
        }

        /// <summary>Đường cong decay: bùng mạnh rồi giảm dần — giống rung chấn thực tế</summary>
        private float EaseOutEnvelope(float t)
        {
            if (t < 0.15f)
            {
                // Attack: tăng nhanh lên đỉnh trong 15% đầu
                return Mathf.SmoothStep(0f, 1f, t / 0.15f);
            }
            else
            {
                // Decay: giảm dần mượt mà
                float decayT = (t - 0.15f) / 0.85f;
                return 1f - (decayT * decayT); // Quadratic ease-out
            }
        }

        public void ResetOriginFromCurrent()
        {
            originalPos = transform.localPosition;
            originalRot = transform.localRotation;
        }

        public void StartShake(float duration, float magnitude, float roughness = 1f)
        {
            // Update origins when a new shake starts
            originalPos = transform.localPosition;
            originalRot = transform.localRotation;

            currentShakeDuration = duration;
            totalShakeDuration = duration;
            shakeElapsed = 0f;
            currentShakeMagnitude = magnitude;
            currentShakeRoughness = roughness;
            isShaking = true;
            isDirectionalShake = false;
            RandomizeSeeds();
        }

        /// <summary>
        /// Rung lắc theo hướng cụ thể — dùng cho hiệu ứng va đập (rơi = Y, va bên = X/Z)
        /// Trục chính rung gấp ~3.3x so với trục vuông góc
        /// </summary>
        public void StartDirectionalShake(float duration, float magnitude, Vector3 direction, float roughness = 1f)
        {
            originalPos = transform.localPosition;
            originalRot = transform.localRotation;

            currentShakeDuration = duration;
            totalShakeDuration = duration;
            shakeElapsed = 0f;
            currentShakeMagnitude = magnitude;
            currentShakeRoughness = roughness;
            isShaking = true;
            isDirectionalShake = true;
            directionalAxis = direction.normalized;
            RandomizeSeeds();
        }

        public void StopShake()
        {
            currentShakeDuration = 0f;
            isShaking = false;
            isDirectionalShake = false;
            currentRotJitter = Vector3.zero;
            transform.localPosition = originalPos;
            transform.localRotation = originalRot;
        }
    }
}

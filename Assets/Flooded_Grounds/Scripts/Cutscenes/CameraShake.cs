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

        private bool isShaking = false;

        /// <summary>Khi bật (ví dụ người chơi tự do lia chuột trong cabin máy bay), CameraShake sẽ không ghi đè trực tiếp localRotation</summary>
        public bool preventRotationOverride = false;
        [HideInInspector]
        public Vector3 currentRotJitter = Vector3.zero;

        void OnEnable()
        {
            originalPos = transform.localPosition;
            originalRot = transform.localRotation;
        }

        void Update()
        {
            if (isShaking)
            {
                if (currentShakeDuration > 0)
                {
                    // Giảm dao động vị trí xuống mức 2% để Camera không văng xa khỏi người
                    Vector3 randomPos = originalPos + Random.insideUnitSphere * (currentShakeMagnitude * 0.02f);
                    
                    // Thêm rung lắc ngẫu nhiên vào góc xoay
                    currentRotJitter = new Vector3(
                        Random.Range(-currentShakeMagnitude, currentShakeMagnitude) * 5f,
                        Random.Range(-currentShakeMagnitude, currentShakeMagnitude) * 5f,
                        Random.Range(-currentShakeMagnitude, currentShakeMagnitude) * 1.5f // Bóp nhỏ độ nghiêng trục Z (Roll)
                    );

                    transform.localPosition = Vector3.Lerp(transform.localPosition, randomPos, Time.deltaTime * currentShakeRoughness * 10f);

                    if (!preventRotationOverride)
                    {
                        Quaternion randomRot = originalRot * Quaternion.Euler(currentRotJitter);
                        transform.localRotation = Quaternion.Lerp(transform.localRotation, randomRot, Time.deltaTime * currentShakeRoughness * 10f);
                    }

                    currentShakeDuration -= Time.deltaTime;
                }
                else
                {
                    currentShakeDuration = 0f;
                    isShaking = false;
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
            currentShakeMagnitude = magnitude;
            currentShakeRoughness = roughness;
            isShaking = true;
        }

        public void StopShake()
        {
            currentShakeDuration = 0f;
            isShaking = false;
            transform.localPosition = originalPos;
            transform.localRotation = originalRot;
        }
    }
}

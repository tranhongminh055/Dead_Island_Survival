using UnityEngine;
using System.Collections.Generic;

namespace HorrorGame.Cave
{
    /// <summary>
    /// CaveLighting — Hệ thống ánh sáng trong hang động.
    /// - Tạo bóng tối hoàn toàn (ambient darkness)
    /// - Quản lý nguồn sáng cục bộ: nấm phát quang, đuốc treo tường, ánh nham thạch
    /// - Hiệu ứng nhấp nháy đuốc (torch flicker)
    /// - Hỗ trợ đèn pin player (tăng tầm quan trọng PlayerFlashlight)
    /// - Volumetric light từ khe nứt trần hang
    /// </summary>
    public class CaveLighting : MonoBehaviour
    {
        [Header("── Loại Ánh Sáng Hang ──")]
        public CaveLightType lightType = CaveLightType.Bioluminescent;

        [Header("── Cài Đặt Chung ──")]
        [Tooltip("Cường độ sáng cơ bản")]
        public float baseIntensity = 0.5f;

        [Tooltip("Màu ánh sáng")]
        public Color lightColor = new Color(0.3f, 0.9f, 0.4f); // Xanh lục nấm phát quang

        [Tooltip("Tầm chiếu sáng")]
        public float lightRange = 8f;

        [Header("── Nhấp Nháy (Flicker) ──")]
        [Tooltip("Bật hiệu ứng nhấp nháy (cho đuốc, lửa)")]
        public bool enableFlicker = false;

        [Tooltip("Tốc độ nhấp nháy")]
        public float flickerSpeed = 8f;

        [Tooltip("Biên độ nhấp nháy (0-1)")]
        [Range(0f, 1f)]
        public float flickerAmount = 0.3f;

        [Header("── Nhịp Thở Ánh Sáng (Pulse) ──")]
        [Tooltip("Bật hiệu ứng sáng/tối nhịp nhàng (cho nấm phát quang)")]
        public bool enablePulse = true;

        [Tooltip("Tốc độ nhịp thở")]
        public float pulseSpeed = 1.5f;

        [Tooltip("Biên độ nhịp (0-1)")]
        [Range(0f, 1f)]
        public float pulseAmount = 0.2f;

        [Header("── Emission Material ──")]
        [Tooltip("Renderer để điều chỉnh emission (nấm, san hô phát quang)")]
        public Renderer glowRenderer;

        [Tooltip("Màu emission")]
        public Color emissionColor = new Color(0.2f, 0.8f, 0.3f);

        [Tooltip("Cường độ emission")]
        public float emissionIntensity = 2f;

        // Components
        private Light pointLight;
        private float randomPhase; // Mỗi đèn có phase riêng → không đồng bộ

        private void Start()
        {
            randomPhase = Random.Range(0f, Mathf.PI * 2f);

            // Tạo Point Light nếu chưa có
            pointLight = GetComponent<Light>();
            if (pointLight == null)
            {
                pointLight = gameObject.AddComponent<Light>();
            }

            pointLight.type = LightType.Point;
            pointLight.range = lightRange;
            pointLight.color = lightColor;
            pointLight.intensity = baseIntensity;
            pointLight.shadows = LightShadows.Soft;
            pointLight.shadowStrength = 0.7f;

            // Tối ưu: đèn nhỏ trong hang không cần shadow xa
            pointLight.shadowBias = 0.05f;
            pointLight.renderMode = LightRenderMode.Auto;

            // Setup emission material (nấm phát quang)
            if (glowRenderer != null)
            {
                SetupEmission();
            }

            // Theo loại ánh sáng, đặt preset
            ApplyLightTypePreset();
        }

        private void ApplyLightTypePreset()
        {
            switch (lightType)
            {
                case CaveLightType.Bioluminescent: // Nấm phát quang
                    lightColor = new Color(0.3f, 0.9f, 0.4f);
                    baseIntensity = 0.5f;
                    lightRange = 6f;
                    enablePulse = true;
                    enableFlicker = false;
                    pulseSpeed = 1.2f;
                    break;

                case CaveLightType.BioluminescentBlue: // Thạch nhũ phát quang xanh
                    lightColor = new Color(0.2f, 0.5f, 0.95f);
                    baseIntensity = 0.6f;
                    lightRange = 8f;
                    enablePulse = true;
                    enableFlicker = false;
                    pulseSpeed = 0.8f;
                    break;

                case CaveLightType.CoralPink: // San hô tím hồng
                    lightColor = new Color(0.9f, 0.3f, 0.7f);
                    baseIntensity = 0.4f;
                    lightRange = 5f;
                    enablePulse = true;
                    enableFlicker = false;
                    pulseSpeed = 1.0f;
                    break;

                case CaveLightType.Torch: // Đuốc treo tường
                    lightColor = new Color(1f, 0.65f, 0.2f);
                    baseIntensity = 1.2f;
                    lightRange = 10f;
                    enablePulse = false;
                    enableFlicker = true;
                    flickerSpeed = 12f;
                    flickerAmount = 0.35f;
                    break;

                case CaveLightType.Campfire: // Lửa trại cannibal
                    lightColor = new Color(1f, 0.5f, 0.1f);
                    baseIntensity = 1.8f;
                    lightRange = 14f;
                    enablePulse = false;
                    enableFlicker = true;
                    flickerSpeed = 8f;
                    flickerAmount = 0.25f;
                    break;

                case CaveLightType.LavaGlow: // Ánh nham thạch
                    lightColor = new Color(1f, 0.3f, 0.05f);
                    baseIntensity = 2f;
                    lightRange = 12f;
                    enablePulse = true;
                    enableFlicker = true;
                    pulseSpeed = 0.5f;
                    pulseAmount = 0.1f;
                    flickerSpeed = 3f;
                    flickerAmount = 0.1f;
                    break;

                case CaveLightType.CrackLight: // Ánh sáng chiếu qua khe nứt trần
                    lightColor = new Color(0.9f, 0.95f, 1f);
                    baseIntensity = 1f;
                    lightRange = 15f;
                    enablePulse = false;
                    enableFlicker = false;
                    pointLight.type = LightType.Spot;
                    pointLight.spotAngle = 30f;
                    break;

                case CaveLightType.ParasiteGlow: // Bào tử ký sinh phát sáng (Hang 6)
                    lightColor = new Color(0.1f, 0.9f, 0.15f);
                    baseIntensity = 0.4f;
                    lightRange = 5f;
                    enablePulse = true;
                    pulseSpeed = 2f;
                    pulseAmount = 0.4f;
                    enableFlicker = true;
                    flickerSpeed = 15f;
                    flickerAmount = 0.15f;
                    break;
            }

            // Apply sau khi set preset
            pointLight.color = lightColor;
            pointLight.intensity = baseIntensity;
            pointLight.range = lightRange;
        }

        private void SetupEmission()
        {
            if (glowRenderer == null) return;

            Material mat = glowRenderer.material;
            if (mat == null) return;

            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor * emissionIntensity);
        }

        private void Update()
        {
            if (pointLight == null) return;

            float currentIntensity = baseIntensity;

            // Hiệu ứng nhịp thở (pulse)
            if (enablePulse)
            {
                float pulse = Mathf.Sin((Time.time + randomPhase) * pulseSpeed) * 0.5f + 0.5f;
                currentIntensity += (pulse - 0.5f) * baseIntensity * pulseAmount * 2f;
            }

            // Hiệu ứng nhấp nháy (flicker) — dùng Perlin noise cho tự nhiên hơn
            if (enableFlicker)
            {
                float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + randomPhase, randomPhase);
                currentIntensity += (noise - 0.5f) * baseIntensity * flickerAmount * 2f;
            }

            pointLight.intensity = Mathf.Max(0f, currentIntensity);

            // Cập nhật emission nếu có
            if (glowRenderer != null && (enablePulse || enableFlicker))
            {
                float emFactor = currentIntensity / Mathf.Max(baseIntensity, 0.01f);
                Material mat = glowRenderer.material;
                if (mat != null && mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", emissionColor * emissionIntensity * emFactor);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // STATIC UTILITY: Tạo nhanh nguồn sáng trong code
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Tạo nhanh một nguồn sáng nấm phát quang tại vị trí chỉ định
        /// </summary>
        public static CaveLighting CreateBioluminescentLight(Vector3 position, Transform parent = null)
        {
            GameObject obj = new GameObject("BioluminescentLight");
            obj.transform.position = position;
            if (parent != null) obj.transform.SetParent(parent, true);

            CaveLighting cl = obj.AddComponent<CaveLighting>();
            cl.lightType = CaveLightType.Bioluminescent;
            return cl;
        }

        /// <summary>
        /// Tạo nhanh một đuốc treo tường tại vị trí chỉ định
        /// </summary>
        public static CaveLighting CreateTorchLight(Vector3 position, Transform parent = null)
        {
            GameObject obj = new GameObject("TorchLight");
            obj.transform.position = position;
            if (parent != null) obj.transform.SetParent(parent, true);

            CaveLighting cl = obj.AddComponent<CaveLighting>();
            cl.lightType = CaveLightType.Torch;
            return cl;
        }

        // ═══════════════════════════════════════════════════════════════
        // GIZMOS
        // ═══════════════════════════════════════════════════════════════

        private void OnDrawGizmos()
        {
            Color gizmoColor = lightColor;
            gizmoColor.a = 0.3f;
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, lightRange * 0.5f);

            Gizmos.color = lightColor;
            Gizmos.DrawSphere(transform.position, 0.15f);
        }
    }

    /// <summary>Loại nguồn sáng trong hang</summary>
    public enum CaveLightType
    {
        Bioluminescent,      // Nấm phát quang xanh lục
        BioluminescentBlue,  // Thạch nhũ phát quang xanh dương
        CoralPink,           // San hô tím hồng (Hang 3)
        Torch,               // Đuốc treo tường
        Campfire,            // Lửa trại cannibal (Hang 4)
        LavaGlow,            // Ánh nham thạch (Hang 5)
        CrackLight,          // Ánh sáng chiếu qua khe nứt trần
        ParasiteGlow         // Bào tử ký sinh phát sáng (Hang 6)
    }
}

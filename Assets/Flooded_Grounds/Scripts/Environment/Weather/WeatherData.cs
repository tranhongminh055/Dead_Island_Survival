using UnityEngine;

namespace HorrorGame.Environment.Weather
{
    [System.Serializable]
    public class WeatherData
    {
        public WeatherType weatherType;
        public string displayName;

        [Header("Mưa & Gió")]
        [Range(0f, 1f)]
        public float rainIntensity = 0f;      // Cường độ mưa (0 = tạnh, 0.35 = nhỏ, 1.0 = bão)
        [Range(0f, 1f)]
        public float windIntensity = 0.1f;    // Cường độ gió

        [Header("Khí Quyển & Ánh Sáng")]
        [Tooltip("Hệ số làm mờ ánh nắng/trăng (1.0 = bình thường, 0.1 = tối sầm)")]
        [Range(0f, 1f)]
        public float sunLightMultiplier = 1f;

        [Tooltip("Hệ số ánh sáng môi trường (Ambient)")]
        [Range(0f, 1f)]
        public float ambientMultiplier = 1f;

        [Tooltip("Hệ số độ sáng bầu trời (Skybox exposure)")]
        [Range(0.05f, 1f)]
        public float skyboxExposureMultiplier = 1f;

        [Header("Sương Mù (Fog)")]
        [Tooltip("Màu sương mù tương ứng thời tiết")]
        public Color fogColor = new Color(0.5f, 0.55f, 0.6f);
        [Tooltip("Hệ số độ dày sương mù (1.0 = mặc định, 3.0 = dày đặc)")]
        [Range(1f, 5f)]
        public float fogDensityMultiplier = 1f;

        [Header("Sấm Sét")]
        [Tooltip("Tần suất sét đánh (lần/phút). 0 = không có sét")]
        public float lightningFrequency = 0f;

        [Header("Thời Lượng Tự Động (Phút)")]
        public float minDurationMinutes = 3f;
        public float maxDurationMinutes = 8f;

        public WeatherData(WeatherType type, string name, float rain, float wind, float sunMul, float ambMul, float skyMul, Color fogCol, float fogDensity, float lightningFreq, float minDur, float maxDur)
        {
            weatherType = type;
            displayName = name;
            rainIntensity = rain;
            windIntensity = wind;
            sunLightMultiplier = sunMul;
            ambientMultiplier = ambMul;
            skyboxExposureMultiplier = skyMul;
            fogColor = fogCol;
            fogDensityMultiplier = fogDensity;
            lightningFrequency = lightningFreq;
            minDurationMinutes = minDur;
            maxDurationMinutes = maxDur;
        }

        public static WeatherData GetDefaultPreset(WeatherType type)
        {
            switch (type)
            {
                case WeatherType.Clear:
                    return new WeatherData(
                        WeatherType.Clear, "Trời Nắng Ráo",
                        0f, 0.1f, 1.0f, 1.0f, 1.0f,
                        new Color(0.6f, 0.7f, 0.8f), 1.0f, 0f, 6f, 12f
                    );
                case WeatherType.Cloudy:
                    return new WeatherData(
                        WeatherType.Cloudy, "Nhiều Mây / Âm U",
                        0f, 0.3f, 0.65f, 0.75f, 0.6f,
                        new Color(0.45f, 0.5f, 0.55f), 1.3f, 0f, 4f, 8f
                    );
                case WeatherType.LightRain:
                    return new WeatherData(
                        WeatherType.LightRain, "Mưa Nhỏ Phùn",
                        0.35f, 0.45f, 0.45f, 0.6f, 0.45f,
                        new Color(0.35f, 0.42f, 0.48f), 1.8f, 0f, 3f, 6f
                    );
                case WeatherType.HeavyRain:
                    return new WeatherData(
                        WeatherType.HeavyRain, "Mưa Rào Nặng Hạt",
                        0.8f, 0.75f, 0.25f, 0.4f, 0.3f,
                        new Color(0.25f, 0.32f, 0.4f), 2.5f, 1.5f, 3f, 6f
                    );
                case WeatherType.Thunderstorm:
                    return new WeatherData(
                        WeatherType.Thunderstorm, "Bão Táp Sấm Sét",
                        1.0f, 0.95f, 0.12f, 0.22f, 0.18f,
                        new Color(0.18f, 0.22f, 0.28f), 3.2f, 7.5f, 2.5f, 5f
                    );
                default:
                    return new WeatherData(WeatherType.Clear, "Trời Nắng", 0f, 0f, 1f, 1f, 1f, Color.gray, 1f, 0f, 5f, 10f);
            }
        }
    }
}

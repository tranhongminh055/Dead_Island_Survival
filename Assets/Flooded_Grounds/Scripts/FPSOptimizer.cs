using UnityEngine;
using System.Collections;

namespace HorrorGame.Optimization
{
    public class FPSOptimizer : MonoBehaviour
    {
        [Header("Target Settings")]
        public int targetFPS = 60;
        public bool disableVSync = true;

        [Header("Aggressive FPS Boost")]
        [Tooltip("Nếu FPS rớt dưới 30, tự động giảm độ phân giải màn hình để cứu FPS")]
        public bool enableResolutionScaling = true;
        [Tooltip("Giảm tối đa bao nhiêu % (ví dụ 0.5 là giảm còn một nửa độ phân giải)")]
        public float minResolutionScale = 0.5f;

        [Header("Quality Tweaks")]
        public bool autoLowerShadows = true;
        public float maxShadowDistance = 30f;
        
        [Tooltip("Tự động ép đồ hoạ xuống mức Thấp nhất nếu máy quá yếu")]
        public bool forceLowQualityOnLowFPS = true;

        private float deltaTime = 0.0f;
        private float checkInterval = 2f; // Kiểm tra mỗi 2 giây thay vì mỗi frame
        private float currentScale = 1.0f;
        private int originalWidth;
        private int originalHeight;

        void Start()
        {
            Application.targetFrameRate = targetFPS;

            if (disableVSync)
            {
                QualitySettings.vSyncCount = 0;
            }

            if (autoLowerShadows && QualitySettings.shadowDistance > maxShadowDistance)
            {
                QualitySettings.shadowDistance = maxShadowDistance;
            }

            originalWidth = Screen.currentResolution.width;
            originalHeight = Screen.currentResolution.height;
            if (originalWidth == 0) originalWidth = Screen.width;
            if (originalHeight == 0) originalHeight = Screen.height;

            StartCoroutine(AggressiveOptimizeRoutine());
        }

        void Update()
        {
            // Tính toán FPS (Làm mượt)
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        }

        // Chạy kiểm tra mỗi 2 giây để không làm nặng thêm CPU
        IEnumerator AggressiveOptimizeRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(checkInterval);
                float currentFPS = 1.0f / deltaTime;

                if (currentFPS < 30f)
                {
                    // 1. Hạ độ phân giải màn hình (Vũ khí mạnh nhất để tăng FPS)
                    if (enableResolutionScaling && currentScale > minResolutionScale)
                    {
                        currentScale -= 0.1f;
                        if (currentScale < minResolutionScale) currentScale = minResolutionScale;
                        
                        int newWidth = Mathf.RoundToInt(originalWidth * currentScale);
                        int newHeight = Mathf.RoundToInt(originalHeight * currentScale);
                        Screen.SetResolution(newWidth, newHeight, Screen.fullScreen);
                        Debug.Log(string.Format("[FPSOptimizer] Đã giảm độ phân giải xuống {0}x{1} để cứu FPS.", newWidth, newHeight));
                    }

                    // 2. Ép Quality xuống mức Fast/Fastest
                    if (forceLowQualityOnLowFPS && QualitySettings.GetQualityLevel() > 1)
                    {
                        QualitySettings.SetQualityLevel(1, true); // 1 = Fast, 0 = Fastest
                        Debug.Log("[FPSOptimizer] Đã ép Quality xuống mức Fast.");
                    }

                    // 3. Tắt luôn bóng đổ nếu dưới 20 FPS
                    if (currentFPS < 20f && QualitySettings.shadows != ShadowQuality.Disable)
                    {
                        QualitySettings.shadows = ShadowQuality.Disable;
                        Debug.Log("[FPSOptimizer] Cảnh báo cực độ: Đã TẮT TOÀN BỘ bóng đổ vì FPS dưới 20.");
                    }
                }
                else if (currentFPS >= 55f)
                {
                    // Nếu máy mượt trở lại, có thể phục hồi nhẹ (tuỳ chọn)
                    // (Bạn có thể bỏ trống ở đây để giữ nguyên mức tối ưu)
                }
            }
        }

        void OnGUI()
        {
            float fps = 1.0f / deltaTime;
            string text = string.Format("FPS: {0} | Tỉ lệ Phân giải: {1}%", Mathf.RoundToInt(fps), Mathf.RoundToInt(currentScale * 100));

            GUIStyle style = new GUIStyle();
            Rect rect = new Rect(10, 10, 400, 50);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = 24;
            style.fontStyle = FontStyle.Bold;
            
            // Viền chữ để dễ nhìn
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(11, 11, 400, 50), text, style);
            
            style.normal.textColor = fps < 30 ? Color.red : (fps < 50 ? Color.yellow : Color.green);
            GUI.Label(rect, text, style);
        }
    }
}

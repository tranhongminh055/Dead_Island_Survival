using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CinematicEffects : MonoBehaviour
{
    [Header("Screen Fade")]
    public Image fadeImage;
    public float fadeSpeed = 1.0f;

    [Header("Camera Shake")]
    public Transform cameraTransform;
    public float shakeDuration = 0f;
    public float shakeMagnitude = 0.7f;
    public float decreaseFactor = 1.0f;

    private Vector3 originalPos;
    private Coroutine currentFade;

    void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Start()
    {
        if (cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                cameraTransform = cam.transform;
            else
                Debug.LogError(">>> LỖI: Không tìm thấy Camera.main!");
        }

        if (cameraTransform != null)
            originalPos = cameraTransform.localPosition;

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            Debug.Log(">>> fadeImage OK, đã set alpha = 0");
        }
        else
        {
            Debug.LogError(">>> LỖI: fadeImage chưa được gán trong Inspector!");
        }

        // Không tự động chạy PlayCinematicRoutine nữa để tránh xung đột với AirplaneCrashCutscene
        // StartCoroutine(PlayCinematicRoutine());
    }

    private IEnumerator PlayCinematicRoutine()
    {
        Debug.Log(">>> CINEMATIC BẮT ĐẦU - Chờ 7 giây cho cảnh máy bay...");

        // 1. Chờ 7 giây: xem cảnh máy bay bay + cơ trưởng nói
        yield return new WaitForSeconds(7f);

        // 2. Mờ đen trong 1.5 giây
        Debug.Log(">>> GIÂY 7: Bắt đầu MỜ ĐEN!");
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(DoFade(0f, 1f, 1.5f));
        yield return currentFade;
        Debug.Log(">>> MỜ ĐEN XONG - màn hình đen hoàn toàn.");

        // 3. Dịch chuyển xuống đảo trong khi màn hình đen
        TeleportToIsland();

        // 4. Chờ 3 giây trong bóng tối
        yield return new WaitForSeconds(3f);

        // 5. Sáng dần lên trong 2 giây
        Debug.Log(">>> GIÂY 11.5: Bắt đầu SÁNG DẦN!");
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(DoFade(1f, 0f, 2f));
        yield return currentFade;
        Debug.Log(">>> SÁNG LÊN XONG - nhìn thấy đảo hoang!");

        // 6. Tắt hẳn tấm màn đen
        if (fadeImage != null)
            fadeImage.gameObject.SetActive(false);
    }

    // Hàm fade thủ công - CHẮC CHẮN hoạt động, không dùng CrossFadeAlpha
    private IEnumerator DoFade(float fromAlpha, float toAlpha, float duration)
    {
        if (fadeImage == null) yield break;

        fadeImage.gameObject.SetActive(true);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);

            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;

            yield return null;
        }

        // Đảm bảo giá trị cuối cùng chính xác
        Color final_c = fadeImage.color;
        final_c.a = toAlpha;
        fadeImage.color = final_c;
    }

    public void TeleportToIsland()
    {
        if (cameraTransform == null)
        {
            Debug.LogError(">>> LỖI: cameraTransform null, không thể dịch chuyển!");
            return;
        }

        // Dừng tất cả Timeline (PlayableDirector) để không khoá vị trí Camera sau khi dịch chuyển
        UnityEngine.Playables.PlayableDirector[] directors = FindObjectsOfType<UnityEngine.Playables.PlayableDirector>();
        foreach (var dir in directors)
        {
            if (dir.state == UnityEngine.Playables.PlayState.Playing)
            {
                dir.Stop();
                dir.enabled = false;
            }
        }

        Transform player = cameraTransform.root;
        if (player == null || player.name.ToLower().Contains("airplane")) 
        {
            // Tách khỏi máy bay để tránh dịch chuyển cả máy bay hoặc bị dính với máy bay
            cameraTransform.SetParent(null);
            player = cameraTransform;
        }

        Vector3 targetPos = new Vector3(0, 2f, 0);

        GameObject native = GameObject.Find("Tho_Dan_Gia");
        if (native != null && native.transform != null)
        {
            targetPos = native.transform.position + native.transform.forward * 3f + new Vector3(0, 1.5f, 0);
            player.position = targetPos;

            // LookAt an toàn
            Vector3 lookTarget = native.transform.position + new Vector3(0, 1.5f, 0);
            if ((lookTarget - player.position).sqrMagnitude > 0.01f)
                player.LookAt(lookTarget);

            Debug.Log(">>> Đã dịch chuyển Camera xuống Đảo! Y = " + targetPos.y);
        }
        else
        {
            player.position = targetPos;
            Debug.LogWarning(">>> Không tìm thấy Tho_Dan_Gia, hạ cánh tại (0,2,0)");
        }
    }

    // Các hàm public để Timeline gọi (nếu cần)
    public void FadeToBlack()
    {
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(DoFade(0f, 1f, 1f));
    }

    public void FadeToClear()
    {
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(DoFade(1f, 0f, 2f));
    }

    void Update()
    {
        if (cameraTransform == null) return;

        if (shakeDuration > 0)
        {
            cameraTransform.localPosition = originalPos + Random.insideUnitSphere * shakeMagnitude;
            shakeDuration -= Time.deltaTime * decreaseFactor;
        }
        else
        {
            shakeDuration = 0f;
            cameraTransform.localPosition = originalPos;
        }
    }

    public void TriggerCameraShake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }
}

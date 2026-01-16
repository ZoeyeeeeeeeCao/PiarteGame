using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LoadingScreen : MonoBehaviour
{
    public Image progressBar;
    public TextMeshProUGUI loadingText;
    public Canvas canvas;
    public Image steeringWheel; // ADD THIS LINE

    public static string sceneToLoad;

    private Coroutine dotAnimationCoroutine;
    private Coroutine wheelAnimationCoroutine; // ADD THIS LINE
    private CanvasGroup canvasGroup;

    void Start()
    {
        // Get or add CanvasGroup for fading
        if (canvas != null)
        {
            canvasGroup = canvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        progressBar.fillAmount = 0f;
        dotAnimationCoroutine = StartCoroutine(AnimateLoadingText());
        wheelAnimationCoroutine = StartCoroutine(AnimateSteeringWheel()); // ADD THIS LINE
        StartCoroutine(LoadSceneAsync());
    }

    IEnumerator LoadSceneAsync()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);
        operation.allowSceneActivation = false;

        float targetProgress = 0f;

        while (!operation.isDone)
        {
            targetProgress = Mathf.Clamp01(operation.progress / 0.9f);

            while (progressBar.fillAmount < targetProgress)
            {
                progressBar.fillAmount += Time.deltaTime * 0.5f;
                yield return null;
            }

            if (operation.progress >= 0.9f)
            {
                while (progressBar.fillAmount < 1f)
                {
                    progressBar.fillAmount += Time.deltaTime * 0.5f;
                    yield return null;
                }

                progressBar.fillAmount = 1f;

                if (dotAnimationCoroutine != null)
                {
                    StopCoroutine(dotAnimationCoroutine);
                }

                // ADD THESE LINES
                if (wheelAnimationCoroutine != null)
                {
                    StopCoroutine(wheelAnimationCoroutine);
                }

                loadingText.text = "Complete!";

                yield return new WaitForSeconds(0.5f);

                operation.allowSceneActivation = true;
                yield return null;

                if (canvasGroup != null)
                {
                    float fadeTime = 1f;
                    float elapsed = 0f;

                    while (elapsed < fadeTime)
                    {
                        elapsed += Time.deltaTime;
                        canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                        yield return null;
                    }

                    canvasGroup.alpha = 0f;
                }
            }

            yield return null;
        }
    }

    IEnumerator AnimateLoadingText()
    {
        int dotCount = 0;

        while (true)
        {
            dotCount = (dotCount + 1) % 4;
            loadingText.text = "Loading" + new string('.', dotCount);
            yield return new WaitForSeconds(0.5f);
        }
    }

    // ADD THIS ENTIRE FUNCTION
    IEnumerator AnimateSteeringWheel()
    {
        if (steeringWheel == null) yield break;

        float currentRotation = 0f;
        float maxRotation = 750f;
        bool turningRight = true;

        while (true)
        {
            float startRotation = currentRotation;
            float targetRotation = turningRight ? maxRotation : 0f;

            // --- ADJUST DURATION HERE ---
            // Increase this number to make the whole movement take longer (e.g., 2.5f or 3.0f)
            float duration = 3.0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // --- IMPROVED EASE IN-OUT (Cubic) ---
                // This curve is steeper in the middle and slower at the ends than the previous version
                float easedT = t < 0.5f
                    ? 4f * t * t * t
                    : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

                currentRotation = Mathf.Lerp(startRotation, targetRotation, easedT);
                steeringWheel.transform.rotation = Quaternion.Euler(0f, 0f, -currentRotation);

                yield return null;
            }

            currentRotation = targetRotation;
            steeringWheel.transform.rotation = Quaternion.Euler(0f, 0f, -currentRotation);

            turningRight = !turningRight;

            // Optional: Increase this pause to let the "Ease Out" settle
            yield return new WaitForSeconds(0.3f);
        }
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private bool fadeOnStart = true;

    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = fadeOnStart ? 1f : 0f;

            if (fadeOnStart)
            {
                StartCoroutine(FadeRoutine(0f));
            }
        }
    }

    public void LoadScene(string sceneName)
    {
        if (isTransitioning || string.IsNullOrEmpty(sceneName))
            return;

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isTransitioning = true;

        // Fade to black
        if (fadeCanvasGroup != null)
        {
            yield return FadeRoutine(1f);
        }

        // Load scene
        yield return SceneManager.LoadSceneAsync(sceneName);

        // Fade from black
        if (fadeCanvasGroup != null)
        {
            yield return FadeRoutine(0f);
        }

        isTransitioning = false;
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        if (fadeCanvasGroup == null)
            yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }
}

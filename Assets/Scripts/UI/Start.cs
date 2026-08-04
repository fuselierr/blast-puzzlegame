using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject titleText;
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject loginPanel;

    [Header("Animation")]
    [SerializeField, Min(0.05f)] private float slideDuration = 0.4f;
    [SerializeField, Min(0f)] private float offscreenPadding = 100f;
    [SerializeField] private LeanTweenType slideInEase =
        LeanTweenType.easeOutQuart;
    [SerializeField] private LeanTweenType slideOutEase =
        LeanTweenType.easeInQuad;

    private bool isTransitioning;

    private async void Start()
    {
        SetInitialVisibility();

        if (SupabaseManager.Instance == null)
        {
            Debug.LogWarning(
                "SupabaseManager was not bootstrapped. Showing login panel.",
                this);
            ShowLoginPanel();
            return;
        }

        try
        {
            await SupabaseManager.Instance.EnsureInitialized();

            if (this == null)
            {
                return;
            }

            bool isLoggedIn =
                SupabaseManager.Instance.SupabaseClient?.Auth?.CurrentUser != null;

            if (isLoggedIn)
            {
                ShowStartButtonImmediately();
            }
            else
            {
                ShowLoginPanel();
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                "Could not check the saved Supabase login session: " +
                exception,
                this);

            ShowLoginPanel();
        }
    }

    private void SetInitialVisibility()
    {
        if (startButton != null)
        {
            startButton.SetActive(false);
        }

        if (loginPanel != null)
        {
            loginPanel.SetActive(false);
        }
    }

    private void ShowLoginPanel()
    {
        if (startButton != null)
        {
            startButton.SetActive(false);
        }

        if (loginPanel != null)
        {
            loginPanel.SetActive(true);
        }
    }

    private void ShowStartButtonImmediately()
    {
        if (loginPanel != null)
        {
            loginPanel.SetActive(false);
        }

        if (startButton != null)
        {
            startButton.SetActive(true);
        }
    }

    // Call this only after your login controller successfully authenticates.
    public void HandleLoginSucceeded()
    {
        if (isTransitioning)
        {
            return;
        }

        if (loginPanel == null)
        {
            AnimateStartButtonIn();
            return;
        }

        RectTransform loginRect =
            loginPanel.GetComponent<RectTransform>();

        if (loginRect == null)
        {
            loginPanel.SetActive(false);
            AnimateStartButtonIn();
            return;
        }

        isTransitioning = true;
        LeanTween.cancel(loginPanel);

        Vector3 target = GetOffscreenPosition(loginRect, aboveScreen: false);
        LeanTween.move(loginPanel, target, slideDuration)
            .setEase(slideOutEase)
            .setIgnoreTimeScale(true)
            .setOnComplete(() =>
            {
                if (loginPanel != null)
                {
                    loginPanel.SetActive(false);
                }

                isTransitioning = false;
                AnimateStartButtonIn();
            });
    }

    private void AnimateStartButtonIn()
    {
        if (startButton == null)
        {
            return;
        }

        startButton.SetActive(true);
        Canvas.ForceUpdateCanvases();

        RectTransform buttonRect =
            startButton.GetComponent<RectTransform>();

        if (buttonRect == null)
        {
            return;
        }

        Vector3 destination = buttonRect.position;
        Vector3 startPosition =
            GetOffscreenPosition(buttonRect, aboveScreen: false);

        buttonRect.position = startPosition;
        LeanTween.cancel(startButton);

        LeanTween.move(startButton, destination, slideDuration)
            .setEase(slideInEase)
            .setIgnoreTimeScale(true);
    }

    public void EnterMenu()
    {
        if (isTransitioning || startButton == null)
        {
            return;
        }

        isTransitioning = true;

        UIButtonPressEffect buttonEffect =
            startButton.GetComponent<UIButtonPressEffect>();
        if (buttonEffect != null)
        {
            Destroy(buttonEffect);
        }

        LeanTween.cancel(startButton);
        if (titleText != null)
        {
            LeanTween.cancel(titleText);

            RectTransform titleRect =
                titleText.GetComponent<RectTransform>();
            if (titleRect != null)
            {
                Vector3 titleTarget =
                    GetOffscreenPosition(titleRect, aboveScreen: true);

                LeanTween.move(titleText, titleTarget, slideDuration)
                    .setEase(slideOutEase)
                    .setIgnoreTimeScale(true);
            }
        }

        RectTransform buttonRect =
            startButton.GetComponent<RectTransform>();
        if (buttonRect == null)
        {
            SceneManager.LoadScene("MainMenu");
            return;
        }

        Vector3 buttonTarget =
            GetOffscreenPosition(buttonRect, aboveScreen: false);

        LeanTween.move(startButton, buttonTarget, slideDuration)
            .setEase(slideOutEase)
            .setIgnoreTimeScale(true)
            .setOnComplete(() => SceneManager.LoadScene("MainMenu"));
    }

    private Vector3 GetOffscreenPosition(
        RectTransform element,
        bool aboveScreen)
    {
        Canvas canvas = element.GetComponentInParent<Canvas>();
        RectTransform parentRect = element.parent as RectTransform;

        if (canvas == null || parentRect == null)
        {
            float direction = aboveScreen ? 1f : -1f;
            return element.position +
                Vector3.up * direction *
                (Screen.height + offscreenPadding);
        }

        Canvas rootCanvas = canvas.rootCanvas;
        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;

        Vector2 elementScreenPosition =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                element.position);

        Vector2 offscreenPoint = new Vector2(
            elementScreenPosition.x,
            aboveScreen
                ? Screen.height + offscreenPadding
                : -offscreenPadding);

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                parentRect,
                offscreenPoint,
                uiCamera,
                out Vector3 worldPosition))
        {
            return worldPosition;
        }

        float fallbackDirection = aboveScreen ? 1f : -1f;
        return element.position +
            Vector3.up * fallbackDirection *
            (Screen.height + offscreenPadding);
    }

    private void OnDisable()
    {
        if (titleText != null)
        {
            LeanTween.cancel(titleText);
        }

        if (startButton != null)
        {
            LeanTween.cancel(startButton);
        }

        if (loginPanel != null)
        {
            LeanTween.cancel(loginPanel);
        }
    }
}
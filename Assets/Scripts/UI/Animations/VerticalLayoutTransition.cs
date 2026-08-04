using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(VerticalLayoutGroup))]
public class VerticalLayoutTransition : MonoBehaviour
{
    public enum TransitionMode
    {
        SlideInFromBottom,
        SlideInFromTop,
        SlideOutDown,
        SlideOutUp
    }

    [Header("Direction")]
    [SerializeField] private TransitionMode mode =
        TransitionMode.SlideInFromBottom;

    [Header("Timing")]
    [SerializeField, Min(0.05f)] private float moveDuration = 0.35f;
    [SerializeField, Min(0f)] private float staggerDelay = 0.08f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float offscreenPadding = 100f;

    [Header("Playback")]
    [SerializeField] private bool playOnEnable = true;

    private VerticalLayoutGroup layoutGroup;
    private RectTransform layoutRect;
    private readonly List<RectTransform> children =
        new List<RectTransform>();
    private readonly List<Vector3> finalWorldPositions =
        new List<Vector3>();

    private Coroutine animationRoutine;
    private int animationVersion;

    private void Awake()
    {
        layoutGroup = GetComponent<VerticalLayoutGroup>();
        layoutRect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    [ContextMenu("Play Transition")]
    public void Play()
    {
        StopAndRestore();
        animationRoutine = StartCoroutine(PlayAfterLayout());
    }

    private IEnumerator PlayAfterLayout()
    {
        // Wait for the panel and its layout hierarchy to become active.
        yield return null;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRect);

        CaptureActiveChildren();
        if (children.Count == 0)
        {
            animationRoutine = null;
            yield break;
        }

        // Freeze the calculated layout while LeanTween moves the children.
        // Re-enabling it afterward makes Unity authoritative again.
        layoutGroup.enabled = false;

        int version = ++animationVersion;
        int completed = 0;
        bool isEntrance = IsEntranceMode();
        bool offscreenIsAbove = OffscreenIsAbove();

        for (int i = 0; i < children.Count; i++)
        {
            RectTransform child = children[i];
            Vector3 destination = finalWorldPositions[i];
            Vector3 offscreenPosition = GetOffscreenPosition(
                child,
                destination,
                offscreenIsAbove);

            Vector3 start = isEntrance
                ? offscreenPosition
                : destination;

            Vector3 target = isEntrance
                ? destination
                : offscreenPosition;

            child.position = start;

            float delay = i == 0 ? 0f : (i - 1) * staggerDelay;
            LeanTweenType easing = i == 0
                ? LeanTweenType.easeInQuad
                : LeanTweenType.easeOutQuart;

            LeanTween.move(
                    child.gameObject,
                    target,
                    moveDuration)
                .setDelay(delay)
                .setEase(easing)
                .setOnComplete(() =>
                {
                    if (version != animationVersion)
                    {
                        return;
                    }

                    completed++;
                    if (completed == children.Count)
                    {
                        FinishAnimation(isEntrance);
                    }
                });
        }

        animationRoutine = null;
    }

    private void CaptureActiveChildren()
    {
        children.Clear();
        finalWorldPositions.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform child =
                transform.GetChild(i) as RectTransform;

            if (child == null || !child.gameObject.activeSelf)
            {
                continue;
            }

            children.Add(child);
            finalWorldPositions.Add(child.position);
        }
    }

    private bool IsEntranceMode()
    {
        return mode == TransitionMode.SlideInFromBottom ||
               mode == TransitionMode.SlideInFromTop;
    }

    private bool OffscreenIsAbove()
    {
        return mode == TransitionMode.SlideInFromTop ||
               mode == TransitionMode.SlideOutUp;
    }

    private Vector3 GetOffscreenPosition(
        RectTransform child,
        Vector3 destination,
        bool aboveScreen)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform parentRect = child.parent as RectTransform;

        if (canvas == null || parentRect == null)
        {
            float fallbackDirection = aboveScreen ? 1f : -1f;
            return destination +
                Vector3.up * fallbackDirection * (Screen.height + offscreenPadding);
        }

        Canvas rootCanvas = canvas.rootCanvas;
        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;

        Vector2 destinationScreen =
            RectTransformUtility.WorldToScreenPoint(uiCamera, destination);
        Vector2 startScreen = new Vector2(
            destinationScreen.x,
            aboveScreen
                ? Screen.height + offscreenPadding
                : -offscreenPadding);

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                parentRect,
                startScreen,
                uiCamera,
                out Vector3 startWorld))
        {
            return startWorld;
        }

        float direction = aboveScreen ? 1f : -1f;
        return destination +
            Vector3.up * direction * (Screen.height + offscreenPadding);
    }

    private void FinishAnimation(bool isEntrance)
    {
        if (isEntrance)
        {
            RestoreFinalPositions();
            layoutGroup.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRect);
        }
    }

    public void SetMode(TransitionMode newMode)
    {
        mode = newMode;
    }

    public void PlaySlideInFromBottom()
    {
        mode = TransitionMode.SlideInFromBottom;
        Play();
    }

    public void PlaySlideInFromTop()
    {
        mode = TransitionMode.SlideInFromTop;
        Play();
    }

    public void PlaySlideOutDown()
    {
        mode = TransitionMode.SlideOutDown;
        Play();
    }

    public void PlaySlideOutUp()
    {
        mode = TransitionMode.SlideOutUp;
        Play();
    }

    private void StopAndRestore()
    {
        animationVersion++;

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] != null)
            {
                LeanTween.cancel(children[i].gameObject);
            }
        }

        RestoreFinalPositions();

        if (layoutGroup != null)
        {
            layoutGroup.enabled = true;
        }

        if (layoutRect != null && gameObject.activeInHierarchy)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRect);
        }
    }

    private void RestoreFinalPositions()
    {
        int count = Mathf.Min(children.Count, finalWorldPositions.Count);
        for (int i = 0; i < count; i++)
        {
            if (children[i] != null)
            {
                children[i].position = finalWorldPositions[i];
            }
        }
    }

    private void OnDisable()
    {
        StopAndRestore();
    }
}
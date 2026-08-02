using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class UIButtonPressEffect : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    [SerializeField] private RectTransform target;
    [SerializeField] private TMP_Text text;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color pressedColor = Color.red;

    [SerializeField, Range(0.5f, 1f)] private float pressedScale = 0.94f;
    [SerializeField] private float speed = 12f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private Color targetColor;
    private bool initialized;

    void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();

        originalScale = target.localScale;
        targetScale = originalScale;
        targetColor = normalColor;
        initialized = true;
    }

    void Update()
    {
        // Never change anchoredPosition here. A LayoutGroup owns that value.
        target.localScale = Vector3.Lerp(
            target.localScale,
            targetScale,
            Time.unscaledDeltaTime * speed);

        if (text != null)
        {
            text.color = Color.Lerp(
                text.color,
                targetColor,
                Time.unscaledDeltaTime * speed);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = originalScale * pressedScale;
        targetColor = pressedColor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Release();
    }

    private void OnDisable()
    {
        if (initialized && target != null)
        {
            target.localScale = originalScale;
        }

        if (text != null)
        {
            text.color = normalColor;
        }
    }

    private void Release()
    {
        targetScale = originalScale;
        targetColor = normalColor;
    }
}
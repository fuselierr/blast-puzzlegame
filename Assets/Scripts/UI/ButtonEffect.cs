using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class UIButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform target; // usually the button RectTransform
    [SerializeField] private TMP_Text text;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color pressedColor = Color.red;

    [SerializeField] private float pressedY = -5f;
    [SerializeField] private float speed = 12f;

    private Vector2 originalPos;
    private Vector2 targetPos;
    private Color targetColor;

    void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();

        originalPos = target.anchoredPosition;
        targetPos = originalPos;
        targetColor = normalColor;
    }

    void Update()
    {
        target.anchoredPosition = Vector2.Lerp(target.anchoredPosition, targetPos, Time.unscaledDeltaTime * speed);
        text.color = Color.Lerp(text.color, targetColor, Time.unscaledDeltaTime * speed);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetPos = originalPos + new Vector2(0, pressedY);
        targetColor = pressedColor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetPos = originalPos;
        targetColor = normalColor;
    }
}
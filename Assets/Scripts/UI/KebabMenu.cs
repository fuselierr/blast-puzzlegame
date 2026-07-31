using UnityEngine;
using UnityEngine.UI;

public class KebabMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform kebabButton;
    [SerializeField] private RectTransform menuPanel;
    [SerializeField] private RectTransform popupLayer;
    [SerializeField] private GameObject clickBlocker;
    [SerializeField] private Canvas rootCanvas;

    [Header("Position")]
    [SerializeField] private float horizontalGap = 8f;
    [SerializeField] private float verticalGap = 4f;
    [SerializeField] private float screenPadding = 8f;

    private void Awake()
    {
        menuPanel.gameObject.SetActive(false);
        clickBlocker.SetActive(false);
    }

    public void Toggle()
    {
        if (menuPanel.gameObject.activeSelf)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void Open()
    {
        clickBlocker.SetActive(true);
        menuPanel.gameObject.SetActive(true);

        // Blocker stays behind the popup.
        clickBlocker.transform.SetAsLastSibling();
        menuPanel.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();

        PositionNextToButton();
    }

    public void Close()
    {
        menuPanel.gameObject.SetActive(false);
        clickBlocker.SetActive(false);
    }

    private void PositionNextToButton()
    {
        Camera uiCamera = rootCanvas.renderMode ==
                          RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;

        Vector3[] buttonCorners = new Vector3[4];
        kebabButton.GetWorldCorners(buttonCorners);

        // Corner 0 is the button's bottom-left corner.
        Vector2 buttonScreenPosition =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                buttonCorners[0]
            );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            popupLayer,
            buttonScreenPosition,
            uiCamera,
            out Vector2 localPosition
        );

        menuPanel.anchorMin = new Vector2(0.5f, 0.5f);
        menuPanel.anchorMax = new Vector2(0.5f, 0.5f);

        // Top-right of the menu is positioned beside the button.
        menuPanel.pivot = new Vector2(1f, 1f);

        menuPanel.anchoredPosition = localPosition +
            new Vector2(-horizontalGap, -verticalGap);

        ClampInsidePopupLayer();
    }

    private void ClampInsidePopupLayer()
    {
        Rect parentRect = popupLayer.rect;
        Rect menuRect = menuPanel.rect;
        Vector2 pivot = menuPanel.pivot;
        Vector2 position = menuPanel.anchoredPosition;

        float minimumX =
            parentRect.xMin +
            menuRect.width * pivot.x +
            screenPadding;

        float maximumX =
            parentRect.xMax -
            menuRect.width * (1f - pivot.x) -
            screenPadding;

        float minimumY =
            parentRect.yMin +
            menuRect.height * pivot.y +
            screenPadding;

        float maximumY =
            parentRect.yMax -
            menuRect.height * (1f - pivot.y) -
            screenPadding;

        position.x = Mathf.Clamp(position.x, minimumX, maximumX);
        position.y = Mathf.Clamp(position.y, minimumY, maximumY);

        menuPanel.anchoredPosition = position;
    }
}
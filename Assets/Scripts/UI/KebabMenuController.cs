using UnityEngine;
using UnityEngine.UI;

public class KebabMenuController : MonoBehaviour
{
    [Header("Popup References")]
    [SerializeField] private RectTransform popupLayer;
    [SerializeField] private RectTransform menuPanel;
    [SerializeField] private Button clickBlocker;
    [SerializeField] private Canvas rootCanvas;

    [Header("Menu Buttons")]
    [SerializeField] private Button editButton;
    [SerializeField] private Button renameButton;
    [SerializeField] private Button duplicateButton;
    [SerializeField] private Button uploadButton;
    [SerializeField] private Button deleteButton;
    
    [Header("Popups")]
    [SerializeField] private LevelNamePopupController namePopup;

    [Header("Positioning")]
    [SerializeField] private float horizontalGap = 8f;
    [SerializeField] private float verticalGap = 4f;
    [SerializeField] private float screenPadding = 8f;

    private MyLevelEntryUI selectedEntry;

    private void Awake()
    {
        // Hide these first so an Inspector reference problem cannot leave the
        // blocker or menu visible when the scene opens.
        Close();

        if (!ReferencesAreValid())
        {
            enabled = false;
            return;
        }

        clickBlocker.onClick.AddListener(Close);
        editButton.onClick.AddListener(EditSelected);
        renameButton.onClick.AddListener(RenameSelected);
        duplicateButton.onClick.AddListener(DuplicateSelected);
        uploadButton.onClick.AddListener(UploadSelected);
        deleteButton.onClick.AddListener(DeleteSelected);
    }

    public void Open(
        RectTransform kebabButton,
        MyLevelEntryUI entry)
    {
        Debug.Log($"KebabMenuController.Open invoked. kebabButton={(kebabButton!=null)}, entry={(entry!=null)}", this);

        if (kebabButton == null || entry == null)
        {
            Debug.LogWarning($"KebabMenuController.Open called with null parameter: kebabButton={(kebabButton==null)}, entry={(entry==null)}", this);
            return;
        }

        selectedEntry = entry;

        // Ensure the shared popup layer is active so child panels are visible.
        if (popupLayer != null && !popupLayer.gameObject.activeSelf)
        {
            popupLayer.gameObject.SetActive(true);
        }

        if (clickBlocker != null)
        {
            clickBlocker.gameObject.SetActive(true);
        }

        if (menuPanel != null)
        {
            menuPanel.gameObject.SetActive(true);
        }

        // Keep the blocker behind the menu.
        if (clickBlocker != null) clickBlocker.transform.SetAsLastSibling();
        if (menuPanel != null) menuPanel.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();

        PositionBeside(kebabButton);
    }

    public void Close()
    {
        selectedEntry = null;

        if (menuPanel != null)
        {
            menuPanel.gameObject.SetActive(false);
        }

        if (clickBlocker != null)
        {
            clickBlocker.gameObject.SetActive(false);
        }
    }

    public void EditSelected()
    {
        if (selectedEntry == null)
        {
            return;
        }

        MyLevelEntryUI entry = selectedEntry;
        Close();
        entry.EditLevel();
    }

    public void RenameSelected()
    {
        if (selectedEntry == null)
        {
            return;
        }

        MyLevelEntryUI entry = selectedEntry;
        Close();

        if (namePopup == null)
        {
            // Fallback to the existing hookup if no popup is provided.
            entry.RenameLevel();
            return;
        }

        string starting = entry.Level != null && !string.IsNullOrWhiteSpace(entry.Level.levelName)
            ? entry.Level.levelName
            : "Untitled";

        namePopup.Show(starting, newName =>
        {
            if (entry.Level == null) return;

            entry.Level.levelName = newName;
            CustomLevelStorage.Save(entry.Level);
            entry.Setup(entry.Level, this);
        });
    }

    public void DuplicateSelected()
    {
        if (selectedEntry == null)
        {
            return;
        }

        MyLevelEntryUI entry = selectedEntry;
        Close();
        entry.DuplicateLevel();
    }

    public void UploadSelected()
    {
        if (selectedEntry == null)
        {
            return;
        }

        MyLevelEntryUI entry = selectedEntry;
        Close();
        entry.UploadLevel();
    }

    public void DeleteSelected()
    {
        if (selectedEntry == null)
        {
            return;
        }

        MyLevelEntryUI entry = selectedEntry;
        Close();
        entry.DeleteLevel();
    }

    private void PositionBeside(RectTransform kebabButton)
    {
        Camera uiCamera =
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;

        Vector3[] corners = new Vector3[4];
        kebabButton.GetWorldCorners(corners);

        // Get screen positions for both sides of the kebab button.
        Vector2 bottomLeftScreen =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                corners[0]
            );

        Vector2 bottomRightScreen =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                corners[3]
            );

        float requiredWidth =
            menuPanel.rect.width * rootCanvas.scaleFactor +
            horizontalGap +
            screenPadding;

        float availableOnRight =
            Screen.width - bottomRightScreen.x;

        bool openOnRight = availableOnRight >= requiredWidth;

        Vector2 anchorScreenPoint =
            openOnRight
                ? bottomRightScreen
                : bottomLeftScreen;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            popupLayer,
            anchorScreenPoint,
            uiCamera,
            out Vector2 localPoint
        );

        menuPanel.anchorMin = new Vector2(0.5f, 0.5f);
        menuPanel.anchorMax = new Vector2(0.5f, 0.5f);
        menuPanel.localScale = Vector3.one;

        if (openOnRight)
        {
            // Top-left of menu beside bottom-right of button.
            menuPanel.pivot = new Vector2(0f, 1f);

            SetMenuLocalPivotPosition(
                localPoint + new Vector2(horizontalGap, -verticalGap)
            );
        }
        else
        {
            // Top-right of menu beside bottom-left of button.
            menuPanel.pivot = new Vector2(1f, 1f);

            SetMenuLocalPivotPosition(
                localPoint + new Vector2(-horizontalGap, -verticalGap)
            );
        }

        ClampInsidePopupLayer();
    }

    private void SetMenuLocalPivotPosition(Vector2 localPivotPosition)
    {
        // anchoredPosition is relative to the anchors, while localPoint is
        // relative to popupLayer's pivot. Account for that difference.
        Rect layerRect = popupLayer.rect;
        Vector2 anchorReference = new Vector2(
            Mathf.Lerp(layerRect.xMin, layerRect.xMax, menuPanel.anchorMin.x),
            Mathf.Lerp(layerRect.yMin, layerRect.yMax, menuPanel.anchorMin.y)
        );

        menuPanel.anchoredPosition = localPivotPosition - anchorReference;
    }

    private void ClampInsidePopupLayer()
    {
        Rect layerRect = popupLayer.rect;
        Rect menuRect = menuPanel.rect;
        Vector2 pivot = menuPanel.pivot;
        Vector2 anchorReference = new Vector2(
            Mathf.Lerp(layerRect.xMin, layerRect.xMax, menuPanel.anchorMin.x),
            Mathf.Lerp(layerRect.yMin, layerRect.yMax, menuPanel.anchorMin.y)
        );
        Vector2 localPivotPosition =
            anchorReference + menuPanel.anchoredPosition;

        float minimumX =
            layerRect.xMin +
            menuRect.width * pivot.x +
            screenPadding;

        float maximumX =
            layerRect.xMax -
            menuRect.width * (1f - pivot.x) -
            screenPadding;

        float minimumY =
            layerRect.yMin +
            menuRect.height * pivot.y +
            screenPadding;

        float maximumY =
            layerRect.yMax -
            menuRect.height * (1f - pivot.y) -
            screenPadding;

        localPivotPosition.x = Mathf.Clamp(
            localPivotPosition.x,
            minimumX,
            maximumX
        );

        localPivotPosition.y = Mathf.Clamp(
            localPivotPosition.y,
            minimumY,
            maximumY
        );

        menuPanel.anchoredPosition =
            localPivotPosition - anchorReference;
    }

    private bool ReferencesAreValid()
    {
        System.Text.StringBuilder missing = new System.Text.StringBuilder();

        void Check(object field, string name)
        {
            if (field == null)
            {
                if (missing.Length > 0) missing.Append(", ");
                missing.Append(name);
            }
        }

        Check(popupLayer, nameof(popupLayer));
        Check(menuPanel, nameof(menuPanel));
        Check(clickBlocker, nameof(clickBlocker));
        Check(rootCanvas, nameof(rootCanvas));
        Check(editButton, nameof(editButton));
        Check(renameButton, nameof(renameButton));
        Check(duplicateButton, nameof(duplicateButton));
        Check(uploadButton, nameof(uploadButton));
        Check(deleteButton, nameof(deleteButton));

        if (missing.Length > 0)
        {
            Debug.LogError($"KebabMenuController missing Inspector references: {missing}", this);
            return false;
        }

        return true;
    }

    private void OnDestroy()
    {
        if (clickBlocker != null)
        {
            clickBlocker.onClick.RemoveListener(Close);
        }

        if (editButton != null)
        {
            editButton.onClick.RemoveListener(EditSelected);
        }

        if (renameButton != null)
        {
            renameButton.onClick.RemoveListener(RenameSelected);
        }

        if (duplicateButton != null)
        {
            duplicateButton.onClick.RemoveListener(DuplicateSelected);
        }

        if (uploadButton != null)
        {
            uploadButton.onClick.RemoveListener(UploadSelected);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveListener(DeleteSelected);
        }
    }
}
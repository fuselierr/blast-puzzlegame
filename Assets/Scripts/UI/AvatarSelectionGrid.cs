using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AvatarSelectionGrid : MonoBehaviour
{
    [Serializable]
    public class AvatarSelectedEvent : UnityEvent<int> { }

    [Header("Grid")]
    [Tooltip("Usually the RectTransform that has the GridLayoutGroup.")]
    [SerializeField] private RectTransform gridContent;
    [Tooltip("A UI Button prefab whose root Image will display the avatar.")]
    [SerializeField] private Button avatarButtonPrefab;

    [Header("Avatar Sprites")]
    [SerializeField] private ProfileAvatarCatalog avatarCatalog;

    [Header("Current Selection")]
    [SerializeField] private Image currentProfileImage;
    [Tooltip("Frame/checkmark drawn over the currently selected avatar.")]
    [SerializeField] private Sprite selectionSprite;
    [SerializeField] private Color selectionSpriteColor = Color.white;
    [SerializeField] private int selectedAvatarId;
    [SerializeField] private AvatarSelectedEvent onAvatarSelected;

    private Sprite[] avatars;
    private readonly List<Button> generatedButtons = new List<Button>();
    private readonly List<Image> selectionOverlays = new List<Image>();
    private bool selectionRequestRunning;

    public int SelectedAvatarId => selectedAvatarId;
    public event Action<int, Sprite> AvatarSelected;

    private async void Start()
    {
        PopulateGrid();

        int savedAvatarId =
            await SupabaseGameService.GetCurrentProfileAvatarId();
        SetSelectedAvatarWithoutNotification(savedAvatarId);
    }

    public void PopulateGrid()
    {
        if (gridContent == null || avatarButtonPrefab == null ||
            avatarCatalog == null)
        {
            Debug.LogError(
                "AvatarSelectionGrid is missing its grid content, button " +
                "prefab, or avatar catalog.",
                this);
            return;
        }

        avatars = new Sprite[avatarCatalog.Count];
        for (int index = 0; index < avatars.Length; index++)
        {
            avatars[index] = avatarCatalog.GetAvatar(index);
        }

        if (avatars.Length == 0)
        {
            Debug.LogError(
                "The assigned ProfileAvatarCatalog contains no avatars.",
                this);
            return;
        }

        ClearGeneratedButtons();

        for (int avatarId = 0; avatarId < avatars.Length; avatarId++)
        {
            int capturedAvatarId = avatarId;
            Sprite avatar = avatars[avatarId];

            Button button = Instantiate(
                avatarButtonPrefab,
                gridContent,
                false);

            button.name = $"AvatarButton_{capturedAvatarId:00}";
            generatedButtons.Add(button);

            Image avatarImage = button.image;
            if (avatarImage == null)
            {
                avatarImage = button.GetComponent<Image>();
            }

            if (avatarImage != null)
            {
                avatarImage.sprite = avatar;
                avatarImage.preserveAspect = true;
                avatarImage.color = Color.white;
            }

            Image selectionOverlay = CreateSelectionOverlay(button.transform);
            selectionOverlays.Add(selectionOverlay);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(
                () => SelectAvatar(capturedAvatarId));
        }

        SelectAvatarLocally(
            Mathf.Clamp(selectedAvatarId, 0, avatars.Length - 1));
        RefreshSelectionOverlays();
    }

    public async void SelectAvatar(int avatarId)
    {
        if (selectionRequestRunning || avatarId == selectedAvatarId)
        {
            return;
        }

        int previousAvatarId = selectedAvatarId;
        if (!SelectAvatarLocally(avatarId))
        {
            return;
        }

        RefreshSelectionOverlays();
        selectionRequestRunning = true;
        SetButtonsInteractable(false);

        bool saved =
            await SupabaseGameService.SetProfileAvatar(avatarId);

        selectionRequestRunning = false;
        SetButtonsInteractable(true);

        if (!saved)
        {
            SelectAvatarLocally(previousAvatarId);
            RefreshSelectionOverlays();
            Debug.LogError("Could not save the selected profile avatar.", this);
            return;
        }

        onAvatarSelected?.Invoke(avatarId);
        AvatarSelected?.Invoke(avatarId, avatars[avatarId]);
    }

    public void SetSelectedAvatarWithoutNotification(int avatarId)
    {
        selectedAvatarId = avatarId;

        if (avatars != null && avatars.Length > 0)
        {
            SelectAvatarLocally(avatarId);
            RefreshSelectionOverlays();
        }
    }

    public Sprite GetAvatar(int avatarId)
    {
        if (avatars == null || avatars.Length == 0)
        {
            if (avatarCatalog == null)
            {
                return null;
            }

            avatars = new Sprite[avatarCatalog.Count];
            for (int index = 0; index < avatars.Length; index++)
            {
                avatars[index] = avatarCatalog.GetAvatar(index);
            }
        }

        return avatarId >= 0 && avatarId < avatars.Length
            ? avatars[avatarId]
            : avatars.Length > 0 ? avatars[0] : null;
    }

    private bool SelectAvatarLocally(int avatarId)
    {
        if (avatars == null || avatarId < 0 || avatarId >= avatars.Length)
        {
            return false;
        }

        selectedAvatarId = avatarId;

        if (currentProfileImage != null)
        {
            currentProfileImage.sprite = avatars[avatarId];
            currentProfileImage.preserveAspect = true;
        }

        return true;
    }

    private void ClearGeneratedButtons()
    {
        generatedButtons.Clear();
        selectionOverlays.Clear();

        for (int index = gridContent.childCount - 1; index >= 0; index--)
        {
            Destroy(gridContent.GetChild(index).gameObject);
        }
    }

    private Image CreateSelectionOverlay(Transform parent)
    {
        GameObject overlayObject = new GameObject(
            "SelectionOverlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        RectTransform overlayRect =
            overlayObject.GetComponent<RectTransform>();
        overlayRect.SetParent(parent, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayRect.SetAsLastSibling();

        Image overlay = overlayObject.GetComponent<Image>();
        overlay.sprite = selectionSprite;
        overlay.color = selectionSpriteColor;
        overlay.preserveAspect = true;
        overlay.raycastTarget = false;
        overlay.enabled = false;
        return overlay;
    }

    private void RefreshSelectionOverlays()
    {
        for (int index = 0; index < selectionOverlays.Count; index++)
        {
            if (selectionOverlays[index] != null)
            {
                selectionOverlays[index].enabled =
                    selectionSprite != null && index == selectedAvatarId;
            }
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        foreach (Button button in generatedButtons)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }

}
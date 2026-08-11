using System;
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
    [SerializeField] private int selectedAvatarId;
    [SerializeField] private AvatarSelectedEvent onAvatarSelected;

    private Sprite[] avatars;

    public int SelectedAvatarId => selectedAvatarId;
    public event Action<int, Sprite> AvatarSelected;

    private void Start()
    {
        PopulateGrid();
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

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(
                () => SelectAvatar(capturedAvatarId));
        }

        SelectAvatarLocally(
            Mathf.Clamp(selectedAvatarId, 0, avatars.Length - 1));
    }

    public void SelectAvatar(int avatarId)
    {
        if (!SelectAvatarLocally(avatarId))
        {
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
        for (int index = gridContent.childCount - 1; index >= 0; index--)
        {
            Destroy(gridContent.GetChild(index).gameObject);
        }
    }

}
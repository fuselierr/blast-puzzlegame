using UnityEngine;

[CreateAssetMenu(
    fileName = "ProfileAvatarCatalog",
    menuName = "Blast/Profile Avatar Catalog")]
public class ProfileAvatarCatalog : ScriptableObject
{
    [SerializeField] private Sprite[] avatars;

    public int Count => avatars?.Length ?? 0;

    public Sprite GetAvatar(int avatarId)
    {
        if (avatars == null || avatars.Length == 0)
        {
            return null;
        }

        return avatars[Mathf.Clamp(
            avatarId,
            0,
            avatars.Length - 1)];
    }
}
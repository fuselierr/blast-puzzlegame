using UnityEngine;
using UnityEngine.UI;
using TMPro;
 
public class LeaderboardEntryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private Image profileImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text scoreText;
 
    [Header("Optional")]
    [SerializeField] private Sprite defaultProfileSprite; // fallback if none provided
    [SerializeField] private GameObject highlightBackground; // e.g. to highlight "you"
 
    public void Setup(
        LeaderboardEntryData data,
        bool isCurrentPlayer,
        ProfileAvatarCatalog avatarCatalog)
    {
        rankText.text = FormatRank(data.rank);
        nameText.text = data.playerName;
        scoreText.text = data.score.ToString("N0"); // e.g. 12,340
 
        Sprite avatarSprite = avatarCatalog != null
            ? avatarCatalog.GetAvatar(data.avatarId)
            : null;

        profileImage.sprite = avatarSprite != null
            ? avatarSprite
            : data.profileImage != null
                ? data.profileImage
                : defaultProfileSprite;
        profileImage.preserveAspect = true;
 
        if (highlightBackground != null)
            highlightBackground.SetActive(isCurrentPlayer);
    }
 
    // Adds medal-style formatting for top 3, plain number otherwise.
    private string FormatRank(int rank)
    {
        switch (rank)
        {
            case 1:
                rankText.color = Color.yellow; // gold
                nameText.color = Color.yellow;
                scoreText.color = Color.yellow;
                return "1.";
            case 2: 
                rankText.color = Color.gray; // silver
                nameText.color = Color.gray;
                scoreText.color = Color.gray;
                return "2.";
            case 3: 
                rankText.color = new Color(0.8f, 0.5f, 0.2f); // bronze
                nameText.color = new Color(0.8f, 0.5f, 0.2f);
                scoreText.color = new Color(0.8f, 0.5f, 0.2f);
                return "3.";
            default: return $"{rank}";
        }
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelButton : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    public int levelIndex;

    public void LoadLevel()
    {
        GameManager.SelectedMode = GameMode.Campaign;
        GameManager.IsDailyChallenge = false;
        GameManager.SelectedCustomLevel = null;
        GameManager.SelectedCampaignLevelIndex = levelIndex - 1;

        if (BGPersist.Instance != null)
        {
            BGPersist.Instance.AnimateExitThenLoad("Game");
        }
        else
        {
            SceneManager.LoadScene("Game");
        }
    }

    void Start()
    {
        levelText.text = levelIndex.ToString();
    }

    public void SetTextColour(Color color)
    {
        levelText.color = color;
    }
}

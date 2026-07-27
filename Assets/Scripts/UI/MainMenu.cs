using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject campaignPanel;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private static bool attemptedDailyChallenge = false;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private GameObject leaderboardPanel;
    
    private int score;

    public void PlayDailyChallenge()
    {
        if (attemptedDailyChallenge)
        {
            return;
        }
        GameManager.SelectedMode = GameMode.Freeplay;
        GameManager.IsDailyChallenge = true;
        attemptedDailyChallenge = true;
        SceneManager.LoadScene("Game");
    }

    public void PlayFreeplay()
    {
        GameManager.SelectedMode = GameMode.Freeplay;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
    }

    public void PlayCampaign()
    {
        mainMenuPanel.SetActive(false);
        campaignPanel.SetActive(true);
    }

    public void PlayClassic()
    {
        GameManager.SelectedMode = GameMode.Classic;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
    }

    public void CheckStats()
    {
        mainMenuPanel.SetActive(false);
        statsPanel.SetActive(true);
    }

    public void CheckSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void ReturnToMainMenu()
    {
        campaignPanel.SetActive(false);
        statsPanel.SetActive(false);
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void OpenLeaderboard()
    {
        leaderboardPanel.SetActive(true);
    }

    public void CloseLeaderboard()
    {
        leaderboardPanel.SetActive(false);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        campaignPanel.SetActive(false);
        statsPanel.SetActive(false);
        settingsPanel.SetActive(false);
        leaderboardPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        if (attemptedDailyChallenge)
        {
            // load daily challenge playerdata
            score = SaveManager.Instance.LoadDailyChallengeScore();
            scoreText.text = score.ToString();
        } else
        {
            scoreText.text = "N/A";
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public enum MainMenuPage
{
    Main,
    Campaign,
    Community,
    Stats,
    Settings
}

public enum CommunityTab
{
    MyLevels,
    Discover
}

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject campaignPanel;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject communityPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject myLevelsPanel;
    [SerializeField] private GameObject discoverPanel;
    [SerializeField] private TabController communityTabController;
    [SerializeField] private int myLevelsTabIndex = 0;
    [SerializeField] private int discoverTabIndex = 1;
    [SerializeField] private static bool attemptedDailyChallenge = false;
    [SerializeField] private TMP_Text scoreText;

    [SerializeField] private GameObject leaderboardPanel;
    
    private int score;

    public static MainMenuPage PageToOpen = MainMenuPage.Main;
    public static CommunityTab CommunityTabToOpen = CommunityTab.MyLevels;

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
        GameManager.IsDailyChallenge = false;
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
        HideAllPanels();
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

    public void OpenCommunity()
    {
        mainMenuPanel.SetActive(false);
        communityPanel.SetActive(true);
        OpenCommunityTab(CommunityTab.MyLevels);
    }

    public void OpenCommunityTab(CommunityTab tab)
    {
        if (communityPanel == null || myLevelsPanel == null || discoverPanel == null)
        {
            return;
        }

        communityPanel.SetActive(true);
        myLevelsPanel.SetActive(tab == CommunityTab.MyLevels);
        discoverPanel.SetActive(tab == CommunityTab.Discover);

        if (communityTabController != null)
        {
            communityTabController.SelectTab(
                tab == CommunityTab.MyLevels ? myLevelsTabIndex : discoverTabIndex);
        }
    }

    public void OpenLevelEditor()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("LevelEditor");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        HideAllPanels();

        switch (PageToOpen)
        {
            case MainMenuPage.Campaign:
            campaignPanel.SetActive(true);
            break;

            case MainMenuPage.Community:
                communityPanel.SetActive(true);
                OpenCommunityTab(CommunityTabToOpen);
                break;

            case MainMenuPage.Stats:
                statsPanel.SetActive(true);
                break;

            case MainMenuPage.Settings:
                settingsPanel.SetActive(true);
                break;

            case MainMenuPage.Main:
            default:
                mainMenuPanel.SetActive(true);
                break;
        }

        PageToOpen = MainMenuPage.Main;

        if (attemptedDailyChallenge)
        {
            // load daily challenge playerdata
            score = SaveManager.Instance.LoadDailyChallengeScore();
            scoreText.text = score.ToString();
        }
        else
        {
            scoreText.text = "N/A";
        }
    }

    private void HideAllPanels()
    {
        mainMenuPanel.SetActive(false);
        campaignPanel.SetActive(false);
        communityPanel.SetActive(false);
        statsPanel.SetActive(false);
        settingsPanel.SetActive(false);
        leaderboardPanel.SetActive(false);
    }
}

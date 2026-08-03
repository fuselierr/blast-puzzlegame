using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public enum MainMenuPage
{
    Main,
    Campaign,
    Classic,
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

    [SerializeField]
    private GameObject classicProgressPanel;
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

    [SerializeField] private LeaderboardController leaderboardController;

    private static bool dailyChallengeRequestRunning;
    private int score;

    public static MainMenuPage PageToOpen = MainMenuPage.Main;
    public static CommunityTab CommunityTabToOpen = CommunityTab.MyLevels;

    public async void PlayDailyChallenge()
    {
        if (dailyChallengeRequestRunning)
        {
            return;
        }

        dailyChallengeRequestRunning = true;

        bool started =
            await SupabaseGameService
                .TryStartDailyChallenge();

        if (!started)
        {
            int? savedScore =
                await SupabaseGameService
                    .GetTodayDailyScore();

            if (
                savedScore.HasValue &&
                scoreText != null
            )
            {
                scoreText.text =
                    savedScore.Value.ToString();
            }

            dailyChallengeRequestRunning = false;
            return;
        }

        GameManager.SelectedMode =
            GameMode.Freeplay;

        GameManager.IsDailyChallenge = true;
        attemptedDailyChallenge = true;
        dailyChallengeRequestRunning = false;

        LoadSceneWithBackgroundExit("Game");
    }
    public void PlayFreeplay()
    {
        GameManager.SelectedMode = GameMode.Freeplay;
        LoadSceneWithBackgroundExit("Game");
    }

    public void PlayCampaign()
    {
        mainMenuPanel.SetActive(false);
        campaignPanel.SetActive(true);
    }

    public void PlayClassic()
    {
        HideAllPanels();

        if (classicProgressPanel != null)
        {
            classicProgressPanel.SetActive(true);
        }
    }

    public void StartClassicRun()
    {
        PlayClassic();
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
        if (leaderboardController != null)
        {
            leaderboardController.LoadDailyLeaderboard();
        }
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
        LoadSceneWithBackgroundExit("LevelEditor");
    }

    private void LoadSceneWithBackgroundExit(string sceneName)
    {
        if (BGPersist.Instance != null)
        {
            BGPersist.Instance.AnimateExitThenLoad(sceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private async void Start()
    {
        dailyChallengeRequestRunning = false;
        HideAllPanels();

        switch (PageToOpen)
        {
            case MainMenuPage.Campaign:
                campaignPanel.SetActive(true);
                break;

            case MainMenuPage.Classic:
                if (classicProgressPanel != null)
                {
                    classicProgressPanel.SetActive(true);
                }
                else
                {
                    mainMenuPanel.SetActive(true);
                }
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

        attemptedDailyChallenge = await SupabaseGameService.HasAttemptedDailyChallengeToday();

        int? savedDailyScore = await SupabaseGameService.GetTodayDailyScore();

        if (scoreText != null)
        {
            scoreText.text =
                savedDailyScore.HasValue
                    ? savedDailyScore.Value.ToString()
                    : "N/A";
        }
    }

    private void HideAllPanels()
    {
        mainMenuPanel.SetActive(false);
        campaignPanel.SetActive(false);

        if (classicProgressPanel != null)
        {
            classicProgressPanel.SetActive(false);
        }

        communityPanel.SetActive(false);
        statsPanel.SetActive(false);
        settingsPanel.SetActive(false);
        leaderboardPanel.SetActive(false);
    }
}

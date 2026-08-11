using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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

[Serializable]
public class ModeLockView
{
    public Button button;
    public GameObject lockImage;
    [Tooltip("Images/text to tint grey while this mode is locked.")]
    public Graphic[] graphics;

    [NonSerialized] public Color[] originalColors;
}

public class MainMenu : MonoBehaviour
{
    private const int RequiredCampaignCompletions = 5;

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

    [Header("Mode Unlocks")]
    [Tooltip("Add Daily Challenge, Classic, Freeplay, and Community here.")]
    [SerializeField] private ModeLockView[] campaignLockedModes;
    [SerializeField] private Color lockedModeColor =
        new Color(0.35f, 0.35f, 0.35f, 1f);

    private static bool dailyChallengeRequestRunning;
    private int score;
    private bool extraModesUnlocked;

    public static MainMenuPage PageToOpen = MainMenuPage.Main;
    public static CommunityTab CommunityTabToOpen = CommunityTab.MyLevels;

    public async void PlayDailyChallenge()
    {
        if (!extraModesUnlocked || dailyChallengeRequestRunning)
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
        if (!extraModesUnlocked)
        {
            return;
        }

        GameManager.SelectedMode = GameMode.Freeplay;
        GameManager.IsDailyChallenge = false;
        GameManager.SelectedCustomLevel = null;
        LoadSceneWithBackgroundExit("Game");
    }

    public void PlayCampaign()
    {
        mainMenuPanel.SetActive(false);
        campaignPanel.SetActive(true);
    }

    public void PlayClassic()
    {
        if (!extraModesUnlocked)
        {
            return;
        }

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
        if (!extraModesUnlocked)
        {
            return;
        }

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
    private void Awake()
    {
        CaptureModeColors();
        SetExtraModesUnlocked(false);
    }

    private async void Start()
    {
        dailyChallengeRequestRunning = false;
        MainMenuPage requestedPage = PageToOpen;
        PageToOpen = MainMenuPage.Main;

        HideAllPanels();
        mainMenuPanel.SetActive(true);

        List<CampaignLevelProgressRow> campaignProgress =
            await SupabaseGameService.GetCampaignProgress();

        int completedCampaignLevels = campaignProgress
            .Where(row => row != null && row.CompletedAt.HasValue)
            .Select(row => row.LevelNumber)
            .Distinct()
            .Count();

        SetExtraModesUnlocked(
            completedCampaignLevels >= RequiredCampaignCompletions);

        HideAllPanels();

        switch (requestedPage)
        {
            case MainMenuPage.Campaign:
                campaignPanel.SetActive(true);
                break;

            case MainMenuPage.Classic:
                if (extraModesUnlocked && classicProgressPanel != null)
                {
                    classicProgressPanel.SetActive(true);
                }
                else
                {
                    mainMenuPanel.SetActive(true);
                }
                break;

            case MainMenuPage.Community:
                if (extraModesUnlocked)
                {
                    communityPanel.SetActive(true);
                    OpenCommunityTab(CommunityTabToOpen);
                }
                else
                {
                    mainMenuPanel.SetActive(true);
                }
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

    private void CaptureModeColors()
    {
        if (campaignLockedModes == null)
        {
            return;
        }

        foreach (ModeLockView mode in campaignLockedModes)
        {
            if (mode == null || mode.graphics == null)
            {
                continue;
            }

            mode.originalColors = mode.graphics
                .Select(graphic => graphic != null ? graphic.color : Color.white)
                .ToArray();
        }
    }

    private void SetExtraModesUnlocked(bool unlocked)
    {
        extraModesUnlocked = unlocked;

        if (campaignLockedModes == null)
        {
            return;
        }

        foreach (ModeLockView mode in campaignLockedModes)
        {
            if (mode == null)
            {
                continue;
            }

            if (mode.button != null)
            {
                mode.button.interactable = unlocked;
            }

            if (mode.lockImage != null)
            {
                mode.lockImage.SetActive(!unlocked);
            }

            if (mode.graphics == null)
            {
                continue;
            }

            for (int index = 0; index < mode.graphics.Length; index++)
            {
                Graphic graphic = mode.graphics[index];
                if (graphic == null)
                {
                    continue;
                }

                Color original = mode.originalColors != null &&
                    index < mode.originalColors.Length
                        ? mode.originalColors[index]
                        : Color.white;

                graphic.color = unlocked ? original : lockedModeColor;
            }
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
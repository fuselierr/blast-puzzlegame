using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq.Expressions;

public class UIHandler : MonoBehaviour
{
    [SerializeField] private Timer timer;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private MineInventory inventory;

    [SerializeField] private TMP_Text standardText;
    [SerializeField] private TMP_Text crossText;
    [SerializeField] private TMP_Text diagonalText;
    [SerializeField] private TMP_Text doubleText;
    [SerializeField] private ToggleGroup mineSelectToggleGroup;
    [SerializeField] private Toggle standardMineToggle;
    [SerializeField] private Toggle crossMineToggle;
    [SerializeField] private Toggle diagonalMineToggle;
    [SerializeField] private Toggle doubleMineToggle;

    [SerializeField] private TMP_Text levelText; // e.g. "Level 4/10", hidden if totalLevels <= 1

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverMessageText;
    [SerializeField] private GameObject gameWonPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private TMP_Text gameWonScoreCalc;
    [SerializeField] private TMP_Text gameWonFinalScore;
    [SerializeField] private float gameOverDelay = 1f;

    [Header("Level Messages")]
    [SerializeField] private GameObject levelClearPanel;
    [SerializeField] private TMP_Text levelClearMessageText;
    [SerializeField] private TMP_Text levelClearScoreText;
    [SerializeField] private TMP_Text advanceButtonText;

    [SerializeField] private Button advanceButton;

    private bool showingCampaignTutorial;

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLevelStart += HandleLevelStart;
            GameManager.Instance.OnLevelCleared += HandleLevelCleared;
            GameManager.Instance.OnGameWon += HandleGameWon;
            GameManager.Instance.OnGameLost += HandleGameLost;
            GameManager.Instance.OnSelectedMineChange += HandleSelectedMineChange;
            GameManager.Instance.OnCampaignTutorial += HandleCampaignTutorial;

            standardMineToggle.onValueChanged.AddListener((isOn) => { GameManager.Instance.SelectedMineType = MineType.Standard; });
            crossMineToggle.onValueChanged.AddListener((isOn) => { GameManager.Instance.SelectedMineType = MineType.Cross; });
            diagonalMineToggle.onValueChanged.AddListener((isOn) => { GameManager.Instance.SelectedMineType = MineType.Diagonal; });
            doubleMineToggle.onValueChanged.AddListener((isOn) => { GameManager.Instance.SelectedMineType = MineType.Double; });

            HandleSelectedMineChange(GameManager.Instance.SelectedMineType);
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLevelStart -= HandleLevelStart;
            GameManager.Instance.OnLevelCleared -= HandleLevelCleared;
            GameManager.Instance.OnGameWon -= HandleGameWon;
            GameManager.Instance.OnGameLost -= HandleGameLost;
            GameManager.Instance.OnSelectedMineChange -= HandleSelectedMineChange;
            GameManager.Instance.OnCampaignTutorial -= HandleCampaignTutorial;

            standardMineToggle.onValueChanged.RemoveListener((isOn) => { GameManager.Instance.SelectedMineType = MineType.Standard; });
            crossMineToggle.onValueChanged.RemoveListener((isOn) => { GameManager.Instance.SelectedMineType = MineType.Cross; });
            diagonalMineToggle.onValueChanged.RemoveListener((isOn) => { GameManager.Instance.SelectedMineType = MineType.Diagonal; });
            doubleMineToggle.onValueChanged.RemoveListener((isOn) => { GameManager.Instance.SelectedMineType = MineType.Double; });
        }
    }

    void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (gameWonPanel != null)
        {
            gameWonPanel.SetActive(false);
        }

        if (levelClearPanel != null && !showingCampaignTutorial)
        {
            levelClearPanel.SetActive(false);
        }
        settingsPanel?.SetActive(false);

        ConfigurePanelText(gameOverMessageText);
        ConfigurePanelText(gameWonScoreCalc);
        ConfigurePanelText(gameWonFinalScore);
    }
    private void Update()
    {
        if (timer != null && timerText != null)
        {
            timerText.text = timer.GetFormattedTime();
        }

        if (inventory != null)
        {
            SetMineCountText(standardText, MineType.Standard);
            SetMineCountText(crossText, MineType.Cross);
            SetMineCountText(diagonalText, MineType.Diagonal);
            SetMineCountText(doubleText, MineType.Double);
        }
    }

    // Hooked up to the Detonate button. GameManager routes this to
    // whichever IGameMode is active.
    public void Detonate()
    {
        GameManager.Instance?.Detonate();
    }

    public void RestartGame()
    {
        GameManager.Instance?.ResetGame();
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        if (gameWonPanel != null)
        {
            gameWonPanel.SetActive(false);
        }
        if (levelClearPanel != null)
        {
            levelClearPanel.SetActive(false);
        }
    }

    public void ReturnToMainMenu()
    {
        bool wasClassicLevel = GameManager.SelectedMode == GameMode.Classic;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StopGame();
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        bool wasCustomLevel = GameManager.SelectedCustomLevel != null;
        GameManager.SelectedCustomLevel = null;

        MainMenu.PageToOpen = wasCustomLevel
            ? MainMenuPage.Community
            : wasClassicLevel
                ? MainMenuPage.Classic
            : MainMenuPage.Main;

        if (BGPersist.Instance != null)
        {
            BGPersist.Instance.AnimateExitThenLoad("MainMenu");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    private void HandleLevelStart(int levelNumber, int totalLevels)
    {
        if (levelText == null)
        {
            return;
        }
        levelText.text = totalLevels > 1 ? $"Level {levelNumber}/{totalLevels}" : string.Empty;
    }

    private void HandleLevelCleared(int levelNumber, int totalLevels)
    {
        if (levelText != null)
        {
            levelText.text = $"Level {levelNumber} Clear!";
        }

        if (levelClearMessageText != null)
        {
            levelClearMessageText.text =
                $"Good Job!\nLevel {levelNumber} Cleared";
        }

        if (levelClearScoreText != null)
        {
            int score = GameManager.Instance.CalculateScore();
            levelClearScoreText.text = $"Current Score: {score}";
        }

        if (advanceButtonText != null)
        {
            if (GameManager.SelectedMode == GameMode.Classic)
            {
                advanceButtonText.text = "Back to Levels";
            }
            else
            {
                advanceButtonText.text =
                    levelNumber >= totalLevels
                        ? "Finish"
                        : $"Advance to Level {levelNumber + 1}";
            }
        }

        if (levelClearPanel != null)
        {
            levelClearPanel.SetActive(true);
        }
    }

    public void AdvanceClassicLevel()
    {
        if (levelClearPanel != null)
        {
            levelClearPanel.SetActive(false);
        }

        if (showingCampaignTutorial)
        {
            showingCampaignTutorial = false;
            GameManager.Instance?.DismissCampaignTutorial();
            return;
        }

        GameManager.Instance?.AdvanceClassicLevel();
    }

    private void HandleCampaignTutorial(string title, string message)
    {
        showingCampaignTutorial = true;

        if (levelClearMessageText != null)
        {
            levelClearMessageText.text = title;
        }

        if (levelClearScoreText != null)
        {
            levelClearScoreText.text = message;
        }

        if (advanceButtonText != null)
        {
            advanceButtonText.text = "Start Level";
        }

        if (levelClearPanel != null)
        {
            levelClearPanel.SetActive(true);
        }
    }

    private async void HandleGameWon()
    {
        if (levelClearPanel != null)
        {
            levelClearPanel.SetActive(false);
        }

        if (gameWonScoreCalc != null)
        {
            if (GameManager.SelectedMode == GameMode.Classic)
            {
                gameWonScoreCalc.text = "remaining time: 100 points per second\n" + "unused mines: 200 points each";
            }
            else
            {
                gameWonScoreCalc.text = $"mines remaining: {inventory.GetCount(MineType.Standard)} * 200\ntime: {timer.GetFormattedTime()} * -1";
            }
        }
        if (gameWonFinalScore != null)
        {
            int finalScore = GameManager.Instance.CalculateScore();
            gameWonFinalScore.text = $"score: {finalScore}";
            // update playerdata
            if (GameManager.SelectedMode == GameMode.Freeplay && GameManager.IsDailyChallenge)
            {
                if (SaveManager.Instance != null)
                {
                    SaveManager.Instance
                        .SaveDailyChallengeScore(
                            finalScore
                        );
                }

                bool saved =
                    await SupabaseGameService
                        .SubmitDailyChallengeScore(
                            finalScore
                        );

                if (!saved)
                {
                    Debug.LogError(
                        "Daily Challenge score was not uploaded."
                    );
                }
            }
        }
        if (gameWonPanel != null)
        {
            StartCoroutine(ShowPanelAfterDelay(gameWonPanel));
        }
    }

    private void HandleGameLost()
    {
        if (gameOverMessageText != null)
        {
            if (GameManager.SelectedMode == GameMode.Classic)
            {
                gameOverMessageText.text = "Stage Failed!\nRestart Level";
            }
            else
            {
                gameOverMessageText.text = "Fail!";
            }
        }
        if (gameOverPanel != null)
        {
            StartCoroutine(ShowPanelAfterDelay(gameOverPanel));
        }
    }

    private void HandleSelectedMineChange(MineType mineType)
    {
        switch (mineType)
        {
            // Setting Toggle.isOn directly invokes onValueChanged callback. Using SetIsOnWithoutNotify to avoid circling calls. 
            case MineType.Standard:
                standardMineToggle.SetIsOnWithoutNotify(true);
                break;
            case MineType.Cross:
                crossMineToggle.SetIsOnWithoutNotify(true);
                break;
            case MineType.Diagonal:
                diagonalMineToggle.SetIsOnWithoutNotify(true);
                break;
            case MineType.Double:
                doubleMineToggle.SetIsOnWithoutNotify(true);
                break;
        }
    }

    // private void SetMineSelectCallbacks()
    // {
    //     standardMineToggle.onValueChanged.AddListener((isOn) => {GameManager.Instance.SelectedMineType = MineType.Standard;});
    //     crossMineToggle.onValueChanged.AddListener((isOn) => {GameManager.Instance.SelectedMineType = MineType.Cross;});
    //     diagonalMineToggle.onValueChanged.AddListener((isOn) => {GameManager.Instance.SelectedMineType = MineType.Diagonal;});
    //     doubleMineToggle.onValueChanged.AddListener((isOn) => {GameManager.Instance.SelectedMineType = MineType.Double;});
    // }

    private void SetMineCountText(TMP_Text text, MineType type)
    {
        if (text == null)
        {
            return;
        }
        int count = inventory.GetCount(type);
        text.text = count < 0 ? "\u221e" : count.ToString(); // ∞ for infinite (Campaign)
    }

    private IEnumerator ShowPanelAfterDelay(GameObject panel)
    {
        yield return new WaitForSeconds(gameOverDelay);
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    private void ConfigurePanelText(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 50f;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.Center;
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }
}

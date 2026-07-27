using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIHandler : MonoBehaviour
{
    [SerializeField] private Timer timer;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private MineInventory inventory;

    [SerializeField] private TMP_Text standardText;
    [SerializeField] private TMP_Text crossText;
    [SerializeField] private TMP_Text diagonalText;
    [SerializeField] private TMP_Text doubleText;

    [SerializeField] private TMP_Text levelText; // e.g. "Level 4/10", hidden if totalLevels <= 1

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverMessageText;
    [SerializeField] private GameObject gameWonPanel;
    [SerializeField] private TMP_Text gameWonScoreCalc;
    [SerializeField] private TMP_Text gameWonFinalScore;
    [SerializeField] private float gameOverDelay = 1f;

    [Header("Classic Level Clear")]
    [SerializeField] private GameObject levelClearPanel;
    [SerializeField] private TMP_Text levelClearMessageText;
    [SerializeField] private TMP_Text levelClearScoreText;
    [SerializeField] private TMP_Text advanceButtonText;

    [SerializeField] private Button advanceButton;



    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLevelStart += HandleLevelStart;
            GameManager.Instance.OnLevelCleared += HandleLevelCleared;
            GameManager.Instance.OnGameWon += HandleGameWon;
            GameManager.Instance.OnGameLost += HandleGameLost;
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

        if (levelClearPanel != null)
        {
            levelClearPanel.SetActive(false);
        }

        ConfigurePanelText(gameOverMessageText);
        ConfigurePanelText(gameWonScoreCalc);
        ConfigurePanelText(gameWonFinalScore);
        if (advanceButton != null)
        {
            advanceButton.onClick.RemoveListener(
                AdvanceClassicLevel
            );

            advanceButton.onClick.AddListener(
                AdvanceClassicLevel
            );
        }
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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.gameStarted = false;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
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
            advanceButtonText.text =
                levelNumber >= totalLevels
                    ? "Finish"
                    : $"Advance to Level {levelNumber + 1}";
        }

        if (levelClearPanel != null)
        {
            levelClearPanel.SetActive(true);
        }
    }

    public void AdvanceClassicLevel()
    {
        Debug.Log("Advance button clicked successfully");
        if (levelClearPanel != null)
        {
            levelClearPanel.SetActive(false);
        }

        GameManager.Instance?.AdvanceClassicLevel();
    }

    private void HandleGameWon()
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
                SaveManager.Instance.SaveDailyChallengeScore(finalScore);
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
                gameOverMessageText.text = "Stage Failed!\nRestart from Level 1";
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
}

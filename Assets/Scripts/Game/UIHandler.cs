using UnityEngine;
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

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLevelStart += HandleLevelStart;
            GameManager.Instance.OnGameWon += HandleGameWon;
            GameManager.Instance.OnGameLost += HandleGameLost;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLevelStart -= HandleLevelStart;
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

    private void HandleGameWon()
    {
        if (gameWonScoreCalc != null)
        {
            gameWonScoreCalc.text = $"mines remaining: {inventory.GetCount(MineType.Standard)} * 200\ntime: {timer.GetFormattedTime()} * -1";
        }
        if (gameWonFinalScore != null)
        {
            int finalScore = GameManager.Instance.CalculateScore();
            gameWonFinalScore.text = $"score: {finalScore}";
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
            gameOverMessageText.text = "Fail!";
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
}
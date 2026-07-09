using UnityEngine;
using TMPro;
using System.Collections;

public class UIHandler : MonoBehaviour
{
    [SerializeField] private GridSpawner gridSpawner;
    [SerializeField] private Timer timer;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private MineInventory inventory;
    [SerializeField] private ClassicLevelManager classicLevelManager;

    [SerializeField] private TMP_Text standardText;
    [SerializeField] private TMP_Text crossText;
    [SerializeField] private TMP_Text diagonalText;
    [SerializeField] private TMP_Text doubleText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private float gameOverDelay = 5f;

    void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
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

    public void Detonate()
    {
        if (classicLevelManager != null)
        {
            classicLevelManager.DetonateMines();
        }
        else if (gridSpawner != null)
        {
            gridSpawner.DetonateMines();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StopGame();
        }

        if (gameOverPanel != null)
        {
            StartCoroutine(ShowGameOverPanelAfterDelay());
        }
    }

    public void RestartGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGame();
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
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

    private void SetMineCountText(TMP_Text text, MineType type)
    {
        if (text != null)
        {
            text.text = inventory.GetCount(type).ToString();
        }
    }

    private IEnumerator ShowGameOverPanelAfterDelay()
    {
        yield return new WaitForSeconds(gameOverDelay);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }
}

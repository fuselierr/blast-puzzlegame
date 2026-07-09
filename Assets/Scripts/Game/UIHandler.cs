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
            standardText.text = inventory.GetCount(MineType.Standard).ToString();
            crossText.text = inventory.GetCount(MineType.Cross).ToString();
            diagonalText.text = inventory.GetCount(MineType.Diagonal).ToString();
            doubleText.text = inventory.GetCount(MineType.Double).ToString();
        }
    }

    public void Detonate()
    {
        // Classic Mode
        if (classicLevelManager != null)
        {
            classicLevelManager.DetonateMines();
        }

        // Freeplay / Daily Challenge
        if (gridSpawner != null)
        {
            gridSpawner.DetonateMines();
        }
        gridSpawner.DetonateMines();
        GameManager.Instance.StopGame();
        StartCoroutine(ShowGameOverPanelAfterDelay());
    }

    public void RestartGame()
    {
        GameManager.Instance.ResetGame();
        gameOverPanel.SetActive(false);
    }

    public void ReturnToMainMenu()
    {
        GameManager.Instance.gameStarted = false;
        gameOverPanel.SetActive(false);
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator ShowGameOverPanelAfterDelay()
    {
        yield return new WaitForSeconds(gameOverDelay);
        gameOverPanel.SetActive(true);
    }
}

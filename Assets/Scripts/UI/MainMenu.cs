using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject campaignPanel;
    [SerializeField] private static bool attemptedDailyChallenge = false;
    [SerializeField] private TMP_Text scoreText;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        campaignPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
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

    // Update is called once per frame
    void Update()
    {

    }
}

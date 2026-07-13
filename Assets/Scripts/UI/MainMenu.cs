using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject campaignPanel;

    public void PlayFreeplay()
    {
        GameManager.SelectedMode = GameMode.Freeplay; 
        SceneManager.LoadScene("Game");
    }

    public void PlayCampaign()
    {
        mainMenuPanel.SetActive(false);
        campaignPanel.SetActive(true);
    }

    public void PlayClassic()
    {
        // SceneManager.LoadScene("Classic");
        GameManager.SelectedMode = GameMode.Classic; 
        SceneManager.LoadScene("Game");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        campaignPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

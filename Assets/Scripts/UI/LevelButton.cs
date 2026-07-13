using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelButton : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    public int levelIndex;

    public void LoadLevel()
    {
        GameManager.SelectedMode = GameMode.Campaign;
        SceneManager.LoadScene("Game");
    }

    void Start()
    {
        levelText.text = levelIndex.ToString();
    }

    public void SetTextColour(Color color)
    {
        levelText.color = color;
    }
}
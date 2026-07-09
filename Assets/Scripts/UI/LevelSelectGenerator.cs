using UnityEngine;

public class LevelSelectGenerator : MonoBehaviour
{
    public GameObject levelButtonPrefab;
    public Transform content;
    public int numberOfLevels = 30;

    void Start()
    {
        GenerateLevels();
    }

    void GenerateLevels()
    {
        for (int i = 1; i <= numberOfLevels; i++)
        {
            GameObject button = Instantiate(
                levelButtonPrefab,
                content
            );

            button.GetComponent<LevelButton>().levelIndex = i;
            button.GetComponentInChildren<TMPro.TMP_Text>().text = i.ToString();
        }
    }
}
using UnityEngine;

public class LevelSelectGenerator : MonoBehaviour
{
    public GameObject levelButtonPrefab;
    public Transform content;
    public int numberOfLevels = 12;

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

            if (SaveManager.Instance?.CurrentData?.completedCampaignLevels?.Contains(i - 1) == true)
            {
                button.GetComponent<UnityEngine.UI.Image>().color = new Color32(55, 190, 80, 255);
            }
        }
    }
}

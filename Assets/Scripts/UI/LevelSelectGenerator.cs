using System.Collections.Generic;
using UnityEngine;

public class LevelSelectGenerator : MonoBehaviour
{
    public GameObject levelButtonPrefab;
    public Transform content;
    public int numberOfLevels = 12;

    async void Start()
    {
        List<CampaignLevelProgressRow> progress =
            await SupabaseGameService.GetCampaignProgress();

        GenerateLevels(progress);
    }

    void GenerateLevels(List<CampaignLevelProgressRow> progress)
    {
        for (int i = 1; i <= numberOfLevels; i++)
        {
            GameObject button = Instantiate(
                levelButtonPrefab,
                content
            );

            button.GetComponent<LevelButton>().levelIndex = i;
            button.GetComponentInChildren<TMPro.TMP_Text>().text = i.ToString();

            if (progress.Exists(row =>
                row.LevelNumber == i && row.CompletedAt.HasValue))
            {
                button.GetComponent<UnityEngine.UI.Image>().color = new Color32(55, 190, 80, 255);
            }
        }
    }
}

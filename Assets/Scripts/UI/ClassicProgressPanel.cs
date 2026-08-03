using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClassicProgressPanel : MonoBehaviour
{
    private const int ClassicLevelCount = 10;

    [SerializeField]
    private Transform contentParent;

    [SerializeField]
    private ClassicLevelStatusUI statusPrefab;

    [SerializeField]
    private TMP_Text statusText;

    private readonly List<ClassicLevelStatusUI>
        spawnedStatuses =
            new List<ClassicLevelStatusUI>();

    private async void OnEnable()
    {
        await Refresh();
    }

    public async System.Threading.Tasks.Task Refresh()
    {
        if (statusPrefab == null || contentParent == null)
        {
            Debug.LogWarning("ClassicProgressPanel is missing" + "its prefab or content parent."); //if i forget
            return;
        }

        SetStatus("Loading Classic progress...");

        ClearGeneratedStatuses();

        HashSet<int> completedLevels =
            await SupabaseGameService.GetCompletedClassicLevels();

        for (int level = 1; level <= ClassicLevelCount; level++)
        {
            ClassicLevelStatusUI status = Instantiate(statusPrefab, contentParent);
            int levelNumber = level;
            bool completed = completedLevels.Contains(levelNumber);
            bool unlocked = levelNumber == 1 || completedLevels.Contains(levelNumber - 1);
            status.Setup(levelNumber, completed, unlocked, () => LaunchClassicLevel(levelNumber));
            spawnedStatuses.Add(status);
        }

        SetStatus($"{completedLevels.Count}/" + $"{ClassicLevelCount} levels completed");
    }

    private void ClearGeneratedStatuses()
    {
        foreach(ClassicLevelStatusUI status in spawnedStatuses)
        {
            if (status != null)
            {
                Destroy(status.gameObject);
            }
        }
        spawnedStatuses.Clear();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void LaunchClassicLevel(int levelNumber)
    {
        GameManager.SelectedMode = GameMode.Classic;
        GameManager.SelectedClassicLevel = levelNumber;
        GameManager.IsDailyChallenge = false;
        GameManager.SelectedCustomLevel = null;

        if (BGPersist.Instance != null)
        {
            BGPersist.Instance.AnimateExitThenLoad("Game");
        }
        else
        {
            SceneManager.LoadScene("Game");
        }
    }
}

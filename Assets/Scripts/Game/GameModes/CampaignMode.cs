using UnityEngine;

// Many hand-placed levels (ScriptableObjects), played in order.
// Infinite mines, no timer — pure puzzle pacing.
public class CampaignMode : GameModeBase
{
    private int levelIndex;
    private bool waitingForTutorial;

    public override void Initialize(GameManager manager)
    {
        base.Initialize(manager);
        // Lets a save system resume mid-campaign later; defaults to level 1.
        levelIndex = manager.CampaignStartIndex;
    }

    public override void StartGame()
    {
        mineInventory.Infinite = true;
        BeginLevel();
    }

    public override void ResetGame()
    {
        // Retry the current level, not the whole campaign.
        BeginLevel();
    }

    private void BeginLevel()
    {
        CampaignLevelData[] levels = manager.CampaignLevels;
        if (levels == null || levels.Length == 0)
        {
            Debug.LogWarning("CampaignMode: no campaign levels assigned on GameManager.");
            return;
        }

        levelIndex = Mathf.Clamp(levelIndex, 0, levels.Length - 1);
        CampaignLevelData data = levels[levelIndex];

        gridSpawner.LoadLevel(data);

        timer.Configure(0f, countDown: false);
        timer.PauseTimer(); // no timer in campaign

        manager.NotifyLevelStart(levelIndex + 1, levels.Length);

        waitingForTutorial = !string.IsNullOrWhiteSpace(data.tutorialMessage);
        if (waitingForTutorial)
        {
            string tutorialTitle = string.IsNullOrWhiteSpace(data.levelName)
                ? $"Level {levelIndex + 1}"
                : data.levelName;
            manager.NotifyCampaignTutorial(tutorialTitle, data.tutorialMessage);
        }
    }

    public override void OnDetonate()
    {
        if (waitingForTutorial)
        {
            return;
        }

        gridSpawner.DetonateMinesAndResolve(HandleDetonationResolved);
    }

    public void DismissTutorial()
    {
        waitingForTutorial = false;
    }

    public override int calculateScore()
    {
        return 0;
    }

    private void HandleDetonationResolved(bool debrisCleared)
    {
        manager.NotifyDetonateComplete(debrisCleared);

        if (!debrisCleared)
        {
            return;
        }

        MarkLevelCompleted(levelIndex);

        levelIndex++;
        if (levelIndex >= manager.CampaignLevels.Length)
        {
            manager.NotifyGameWon();
        }
        else
        {
            BeginLevel();
        }
    }

    private void MarkLevelCompleted(int completedLevelIndex)
    {
        if (SaveManager.Instance == null || SaveManager.Instance.CurrentData == null)
        {
            return;
        }

        PlayerData data = SaveManager.Instance.CurrentData;
        if (!data.completedCampaignLevels.Contains(completedLevelIndex))
        {
            data.completedCampaignLevels.Add(completedLevelIndex);
            SaveManager.Instance.SaveData();
        }
    }

    public override bool TryPlaceMine(MineType mineType = MineType.Standard) => !waitingForTutorial;
    public override void ReturnMine(MineType mineType = MineType.Standard) { }
}

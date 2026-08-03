using UnityEngine;

// Many hand-placed levels (ScriptableObjects), played in order.
public class CampaignMode : GameModeBase
{
    private int levelIndex;
    private bool waitingForTutorial;
    private bool waitingForAdvance;

    public override void Initialize(GameManager manager)
    {
        base.Initialize(manager);
        // Lets a save system resume mid-campaign later; defaults to level 1.
        levelIndex = manager.CampaignStartIndex;
    }

    public override void StartGame()
    {
        mineInventory.Infinite = false;
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
        ConfigureMineInventory(data);

        timer.Configure(0f, countDown: false);
        timer.PauseTimer();

        manager.NotifyLevelStart(levelIndex + 1, levels.Length);

        waitingForAdvance = false;
        waitingForTutorial = !string.IsNullOrWhiteSpace(data.tutorialMessage);
        if (waitingForTutorial)
        {
            string tutorialTitle = string.IsNullOrWhiteSpace(data.levelName)
                ? $"Level {levelIndex + 1}"
                : data.levelName;
            manager.NotifyCampaignTutorial(tutorialTitle, data.tutorialMessage);
        }
        else
        {
            timer.ResumeTimer();
        }
    }

    public override void OnDetonate()
    {
        if (waitingForTutorial || waitingForAdvance)
        {
            return;
        }

        gridSpawner.DetonateMinesAndResolve(HandleDetonationResolved);
    }

    public void DismissTutorial()
    {
        waitingForTutorial = false;
        timer.ResumeTimer();
    }

    private void ConfigureMineInventory(CampaignLevelData data)
    {
        mineInventory.ResetInventory();
        mineInventory.SetMineCount(MineType.Standard, data.standardMines);
        mineInventory.SetMineCount(MineType.Cross, data.crossMines);
        mineInventory.SetMineCount(MineType.Diagonal, data.diagonalMines);
        mineInventory.SetMineCount(MineType.Double, data.doubleMines);
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

        timer.PauseTimer();
        MarkLevelCompleted(levelIndex, timer.TimeValue);
        waitingForAdvance = true;
        manager.gameStarted = false;
        manager.NotifyLevelCleared(levelIndex + 1, manager.CampaignLevels.Length);
    }

    public void AdvanceToNextLevel()
    {
        if (!waitingForAdvance)
        {
            return;
        }

        waitingForAdvance = false;
        levelIndex++;

        if (levelIndex >= manager.CampaignLevels.Length)
        {
            manager.NotifyGameWon();
            return;
        }

        manager.gameStarted = true;
        BeginLevel();
    }

    private void MarkLevelCompleted(int completedLevelIndex, float elapsedSeconds)
    {
        if (SaveManager.Instance == null || SaveManager.Instance.CurrentData == null)
        {
            return;
        }

        PlayerData data = SaveManager.Instance.CurrentData;
        bool saveChanged = false;

        if (data.completedCampaignLevels == null)
        {
            data.completedCampaignLevels = new System.Collections.Generic.List<int>();
        }

        if (!data.completedCampaignLevels.Contains(completedLevelIndex))
        {
            data.completedCampaignLevels.Add(completedLevelIndex);
            saveChanged = true;
        }

        if (data.campaignLevelResults == null)
        {
            data.campaignLevelResults = new System.Collections.Generic.List<CampaignLevelResult>();
        }

        CampaignLevelResult result = data.campaignLevelResults.Find(
            item => item.levelIndex == completedLevelIndex);

        if (result == null)
        {
            data.campaignLevelResults.Add(
                new CampaignLevelResult(completedLevelIndex, elapsedSeconds));
            saveChanged = true;
        }
        else if (elapsedSeconds < result.bestTimeSeconds)
        {
            result.bestTimeSeconds = elapsedSeconds;
            saveChanged = true;
        }

        if (saveChanged)
        {
            SaveManager.Instance.SaveData();
        }
    }

    public override bool TryPlaceMine(MineType mineType = MineType.Standard)
    {
        return !waitingForTutorial
            && !waitingForAdvance
            && base.TryPlaceMine(mineType);
    }

    public override void ReturnMine(MineType mineType = MineType.Standard)
    {
        base.ReturnMine(mineType);
    }
}

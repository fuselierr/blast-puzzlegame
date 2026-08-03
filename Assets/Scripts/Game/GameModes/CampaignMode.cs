using UnityEngine;

// Many hand-placed levels (ScriptableObjects), played in order.
public class CampaignMode : GameModeBase
{
    private enum BeginnerTutorialStep
    {
        None,
        PlaceCenterMine,
        Detonate
    }

    private int levelIndex;
    private bool waitingForTutorial;
    private bool waitingForAdvance;
    private bool hasDetonated;
    private BeginnerTutorialStep beginnerTutorialStep;
    private Vector2Int tutorialCenterPosition;

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
        SelectFirstAvailableMine(data);

        timer.Configure(0f, countDown: false);
        timer.PauseTimer();

        manager.NotifyLevelStart(levelIndex + 1, levels.Length);

        waitingForAdvance = false;
        hasDetonated = false;
        beginnerTutorialStep = BeginnerTutorialStep.None;
        waitingForTutorial = !data.interactiveBeginnerTutorial
            && !string.IsNullOrWhiteSpace(data.tutorialMessage);

        if (data.interactiveBeginnerTutorial)
        {
            tutorialCenterPosition = new Vector2Int(data.width / 2, data.height / 2);
            beginnerTutorialStep = BeginnerTutorialStep.PlaceCenterMine;
            gridSpawner.SetTutorialHighlight(tutorialCenterPosition, true);
            manager.NotifyCampaignGuidance(
                "Step 1: Tap the highlighted center tile to place your Standard mine.",
                highlightDetonate: false);
            timer.ResumeTimer();
        }
        else
        {
            manager.NotifyCampaignGuidance(string.Empty, highlightDetonate: false);
        }

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
        if (waitingForTutorial || waitingForAdvance || hasDetonated)
        {
            return;
        }

        if (beginnerTutorialStep == BeginnerTutorialStep.PlaceCenterMine)
        {
            manager.NotifyCampaignGuidance(
                "Place the Standard mine on the highlighted center tile first.",
                highlightDetonate: false);
            return;
        }

        if (beginnerTutorialStep == BeginnerTutorialStep.Detonate)
        {
            beginnerTutorialStep = BeginnerTutorialStep.None;
            manager.NotifyCampaignGuidance(string.Empty, highlightDetonate: false);
        }

        hasDetonated = true;
        timer.PauseTimer();
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

    private void SelectFirstAvailableMine(CampaignLevelData data)
    {
        if (data.standardMines > 0)
        {
            manager.SelectedMineType = MineType.Standard;
        }
        else if (data.crossMines > 0)
        {
            manager.SelectedMineType = MineType.Cross;
        }
        else if (data.diagonalMines > 0)
        {
            manager.SelectedMineType = MineType.Diagonal;
        }
        else if (data.doubleMines > 0)
        {
            manager.SelectedMineType = MineType.Double;
        }
    }

    public void HandleMinePlaced(Vector2Int position, MineType mineType)
    {
        if (beginnerTutorialStep != BeginnerTutorialStep.PlaceCenterMine)
        {
            return;
        }

        if (position != tutorialCenterPosition || mineType != MineType.Standard)
        {
            manager.NotifyCampaignGuidance(
                "Tap that mine again to pick it up, then place the Standard mine on the highlighted center tile.",
                highlightDetonate: false);
            return;
        }

        gridSpawner.SetTutorialHighlight(tutorialCenterPosition, false);
        beginnerTutorialStep = BeginnerTutorialStep.Detonate;
        manager.NotifyCampaignGuidance(
            "Step 2: Great! Now press the highlighted DETONATE button.",
            highlightDetonate: true);
    }

    public void HandleMineReturned(Vector2Int position, MineType mineType)
    {
        if (beginnerTutorialStep == BeginnerTutorialStep.Detonate
            && position == tutorialCenterPosition)
        {
            beginnerTutorialStep = BeginnerTutorialStep.PlaceCenterMine;
            gridSpawner.SetTutorialHighlight(tutorialCenterPosition, true);
            manager.NotifyCampaignGuidance(
                "Step 1: Tap the highlighted center tile to place your Standard mine.",
                highlightDetonate: false);
        }
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
            timer.PauseTimer();
            manager.NotifyCampaignGuidance(string.Empty, highlightDetonate: false);
            manager.NotifyGameLost();
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

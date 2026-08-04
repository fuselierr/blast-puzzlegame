using System.Collections.Generic;
using UnityEngine;

// Many hand-placed levels (ScriptableObjects), played in order.
public class CampaignMode : GameModeBase
{
    private enum TutorialStep
    {
        None,
        PlaceRequiredMine,
        PlaceMultipleMines,
        SelectCrossMine,
        SelectDiagonalMine,
        SelectDoubleMine
    }

    private int levelIndex;
    private bool waitingForAdvance;
    private bool hasDetonated;
    private bool tutorialActive;
    private bool tutorialReadyToStart;
    private TutorialStep tutorialStep;
    private CampaignLevelData currentLevel;
    private Vector2Int requiredMinePosition;
    private MineType requiredMineType = MineType.Standard;
    private Vector2Int? highlightedPosition;
    private readonly HashSet<Vector2Int> tutorialMinePositions =
        new HashSet<Vector2Int>();

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
        BeginLevel(forceTutorial: false);
    }

    private void BeginLevel(bool forceTutorial = false)
    {
        CampaignLevelData[] levels = manager.CampaignLevels;
        if (levels == null || levels.Length == 0)
        {
            Debug.LogWarning("CampaignMode: no campaign levels assigned on GameManager.");
            return;
        }

        levelIndex = Mathf.Clamp(levelIndex, 0, levels.Length - 1);
        CampaignLevelData data = levels[levelIndex];
        currentLevel = data;

        ClearTutorialHighlight();
        gridSpawner.LoadLevel(data);
        ConfigureMineInventory(data);
        SelectFirstAvailableMine(data);

        timer.Configure(0f, countDown: false);
        timer.PauseTimer();

        manager.NotifyLevelStart(levelIndex + 1, levels.Length);

        waitingForAdvance = false;
        hasDetonated = false;
        tutorialStep = TutorialStep.None;
        tutorialActive = false;
        tutorialReadyToStart = false;
        tutorialMinePositions.Clear();
        manager.NotifyCampaignMineSelectorHint(null);

        bool tutorialAvailable = data.tutorialType != CampaignTutorialType.None;
        bool shouldStartTutorial = tutorialAvailable
            && (forceTutorial || !HasCompletedTutorial(data.tutorialType));

        manager.NotifyCampaignTutorialControls(
            tutorialAvailable,
            shouldStartTutorial);

        if (shouldStartTutorial)
        {
            StartTutorial(data);
            return;
        }

        manager.NotifyCampaignGuidance(string.Empty, highlightDetonate: false);
        timer.ResumeTimer();
    }

    public override void OnDetonate()
    {
        if (waitingForAdvance || hasDetonated)
        {
            return;
        }

        if (tutorialActive)
        {
            manager.NotifyCampaignGuidance(
                "Complete these tutorial steps or tap Skip Tutorial before detonating.",
                highlightDetonate: false);
            return;
        }

        hasDetonated = true;
        timer.PauseTimer();
        gridSpawner.DetonateMinesAndResolve(HandleDetonationResolved);
    }

    public void DismissTutorial()
    {
        SkipTutorial();
    }

    public void SkipTutorial()
    {
        if (!tutorialActive)
        {
            return;
        }

        FinishTutorial(resumeTimer: true);
    }

    public void ReplayTutorial()
    {
        if (currentLevel == null
            || currentLevel.tutorialType == CampaignTutorialType.None
            || waitingForAdvance
            || gridSpawner.IsDetonating)
        {
            return;
        }

        manager.gameStarted = true;
        BeginLevel(forceTutorial: true);
    }

    public void StartLevelAfterTutorial()
    {
        if (!tutorialReadyToStart)
        {
            return;
        }

        tutorialReadyToStart = false;
        manager.gameStarted = true;
        manager.NotifyCampaignGuidance(string.Empty, highlightDetonate: false);
        manager.NotifyCampaignTutorialControls(
            available: true,
            active: false,
            readyToStart: false);
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
        if (!tutorialActive)
        {
            return;
        }

        if (tutorialStep == TutorialStep.PlaceMultipleMines)
        {
            tutorialMinePositions.Add(position);
            if (tutorialMinePositions.Count >= 2)
            {
                CompleteTutorial(
                    "Both mines are ready. Press START when you are ready, then DETONATE once to fire them together.");
            }
            else
            {
                manager.NotifyCampaignGuidance(
                    "Good. Place one more mine before detonating.",
                    highlightDetonate: false);
            }
            return;
        }

        if (tutorialStep != TutorialStep.PlaceRequiredMine)
        {
            return;
        }

        if (position != requiredMinePosition || mineType != requiredMineType)
        {
            manager.NotifyCampaignGuidance(
                $"Tap that mine again to pick it up, then place a {requiredMineType} mine on the highlighted tile.",
                highlightDetonate: false);
            return;
        }

        switch (currentLevel.tutorialType)
        {
            case CampaignTutorialType.BasicControls:
                ClearTutorialHighlight();
                CompleteTutorial(
                    "The mine fires in all eight directions. Press START when you are ready, then press DETONATE.");
                break;

            case CampaignTutorialType.BlastDirections:
                ClearTutorialHighlight();
                CompleteTutorial(
                    "The centre mine lines up with every debris tile horizontally, vertically, and diagonally. Press START when you are ready, then DETONATE.");
                break;

            case CampaignTutorialType.Walls:
                CompleteTutorial(
                    "That mine approaches two debris tiles without crossing a wall. Press START when ready, then find one more open angle.");
                break;

            case CampaignTutorialType.StrongDebris:
                CompleteTutorial(
                    "The centre mine gives each strong debris one hit. Press START when ready, then add another mine aligned with both.");
                break;

            case CampaignTutorialType.CrossMine:
                CompleteTutorial(
                    "Cross mines fire only up, down, left, and right. Press START when ready, then DETONATE.");
                break;

            case CampaignTutorialType.DiagonalMine:
                CompleteTutorial(
                    "Diagonal mines fire toward all four corners. Press START when ready, then DETONATE.");
                break;

            case CampaignTutorialType.DoubleMine:
                CompleteTutorial(
                    "Double mines send two blasts in every direction, clearing strong debris in one detonation. Press START when ready.");
                break;
        }
    }

    public void HandleMineReturned(Vector2Int position, MineType mineType)
    {
        tutorialMinePositions.Remove(position);

        if (!tutorialActive)
        {
            return;
        }

        if (tutorialStep == TutorialStep.PlaceMultipleMines)
        {
            manager.NotifyCampaignGuidance(
                tutorialMinePositions.Count == 0
                    ? "Place two mines on different empty tiles before detonating."
                    : "Place one more mine before detonating.",
                highlightDetonate: false);
        }
    }

    public void HandleMineSelected(MineType mineType)
    {
        if (!tutorialActive
            || currentLevel == null
            || !IsSpecialMineTutorial(currentLevel.tutorialType))
        {
            return;
        }

        if (tutorialStep == TutorialStep.SelectCrossMine
            && mineType == MineType.Cross)
        {
            BeginSpecialMinePlacement(
                MineType.Cross,
                "Cross mines fire up, down, left, and right. Place one on the highlighted centre tile.");
        }
        else if (tutorialStep == TutorialStep.SelectDiagonalMine
            && mineType == MineType.Diagonal)
        {
            BeginSpecialMinePlacement(
                MineType.Diagonal,
                "Diagonal mines fire only along the four diagonals. Place one on the highlighted centre tile.");
        }
        else if (tutorialStep == TutorialStep.SelectDoubleMine
            && mineType == MineType.Double)
        {
            BeginSpecialMinePlacement(
                MineType.Double,
                "Double mines fire twice in all eight directions. Place one on the highlighted centre tile.");
        }
    }

    private void StartTutorial(CampaignLevelData data)
    {
        tutorialActive = true;
        tutorialMinePositions.Clear();
        timer.PauseTimer();

        switch (data.tutorialType)
        {
            case CampaignTutorialType.BasicControls:
                requiredMineType = MineType.Standard;
                requiredMinePosition =
                    new Vector2Int(data.width / 2, data.height / 2);
                tutorialStep = TutorialStep.PlaceRequiredMine;
                SetTutorialHighlight(requiredMinePosition);
                manager.NotifyCampaignGuidance(
                    "Step 1: Tap the highlighted centre tile to place your Standard mine.",
                    highlightDetonate: false);
                break;

            case CampaignTutorialType.BlastDirections:
                requiredMineType = MineType.Standard;
                requiredMinePosition =
                    new Vector2Int(data.width / 2, data.height / 2);
                tutorialStep = TutorialStep.PlaceRequiredMine;
                SetTutorialHighlight(requiredMinePosition);
                manager.NotifyCampaignGuidance(
                    "A Standard mine fires horizontally, vertically, and diagonally. Place one on the highlighted centre tile.",
                    highlightDetonate: false);
                break;

            case CampaignTutorialType.MultipleMines:
                tutorialStep = TutorialStep.PlaceMultipleMines;
                manager.NotifyCampaignGuidance(
                    "Some layouts need several mines. Place two mines on different empty tiles before pressing DETONATE.",
                    highlightDetonate: false);
                break;

            case CampaignTutorialType.Walls:
                requiredMineType = MineType.Standard;
                requiredMinePosition =
                    new Vector2Int(0, data.height - 1);
                tutorialStep = TutorialStep.PlaceRequiredMine;
                SetTutorialHighlight(requiredMinePosition);
                manager.NotifyCampaignGuidance(
                    "Walls stop blasts completely. Place a Standard mine on the highlighted upper-left tile to approach debris from open directions.",
                    highlightDetonate: false);
                break;

            case CampaignTutorialType.StrongDebris:
                requiredMineType = MineType.Standard;
                requiredMinePosition =
                    new Vector2Int(data.width / 2, data.height / 2);
                tutorialStep = TutorialStep.PlaceRequiredMine;
                SetTutorialHighlight(requiredMinePosition);
                manager.NotifyCampaignGuidance(
                    "Strong debris needs two hits. Place a Standard mine on the highlighted centre tile for the first hit on both strong pieces.",
                    highlightDetonate: false);
                break;

            case CampaignTutorialType.CrossMine:
                tutorialStep = TutorialStep.SelectCrossMine;
                manager.SelectedMineType = MineType.Standard;
                manager.NotifyCampaignMineSelectorHint(MineType.Cross);
                manager.NotifyCampaignGuidance(
                    "Cross mines use a straight-line blast pattern. Start by tapping the highlighted Cross mine selector.",
                    highlightDetonate: false);
                break;

            case CampaignTutorialType.DiagonalMine:
                tutorialStep = TutorialStep.SelectDiagonalMine;
                manager.SelectedMineType = MineType.Standard;
                manager.NotifyCampaignMineSelectorHint(MineType.Diagonal);
                manager.NotifyCampaignGuidance(
                    "Diagonal mines reach targets on diagonal paths. Start by tapping the highlighted Diagonal mine selector.",
                    highlightDetonate: false);
                break;

            case CampaignTutorialType.DoubleMine:
                tutorialStep = TutorialStep.SelectDoubleMine;
                manager.SelectedMineType = MineType.Standard;
                manager.NotifyCampaignMineSelectorHint(MineType.Double);
                manager.NotifyCampaignGuidance(
                    "Double mines fire twice and are ideal for strong debris. Start by tapping the highlighted Double mine selector.",
                    highlightDetonate: false);
                break;

            default:
                FinishTutorial(resumeTimer: true);
                break;
        }
    }

    private void CompleteTutorial(string message)
    {
        if (currentLevel != null)
        {
            MarkTutorialCompleted(currentLevel.tutorialType);
        }

        tutorialActive = false;
        tutorialReadyToStart = true;
        tutorialStep = TutorialStep.None;
        ClearTutorialHighlight();
        manager.NotifyCampaignMineSelectorHint(null);
        manager.gameStarted = false;
        manager.NotifyCampaignTutorialControls(
            available: true,
            active: false,
            readyToStart: true);
        manager.NotifyCampaignGuidance(message, highlightDetonate: false);
    }

    private void FinishTutorial(bool resumeTimer)
    {
        if (currentLevel != null)
        {
            MarkTutorialCompleted(currentLevel.tutorialType);
        }

        tutorialActive = false;
        tutorialReadyToStart = false;
        tutorialStep = TutorialStep.None;
        ClearTutorialHighlight();
        manager.NotifyCampaignMineSelectorHint(null);
        manager.NotifyCampaignGuidance(string.Empty, highlightDetonate: false);
        manager.NotifyCampaignTutorialControls(
            currentLevel != null
                && currentLevel.tutorialType != CampaignTutorialType.None,
            active: false,
            readyToStart: false);

        if (resumeTimer)
        {
            manager.gameStarted = true;
            timer.ResumeTimer();
        }
    }

    private bool HasCompletedTutorial(CampaignTutorialType tutorialType)
    {
        PlayerData data = SaveManager.Instance != null
            ? SaveManager.Instance.CurrentData
            : null;

        return data != null
            && data.completedCampaignTutorials != null
            && data.completedCampaignTutorials.Contains((int)tutorialType);
    }

    private void MarkTutorialCompleted(CampaignTutorialType tutorialType)
    {
        if (tutorialType == CampaignTutorialType.None
            || SaveManager.Instance == null
            || SaveManager.Instance.CurrentData == null)
        {
            return;
        }

        PlayerData data = SaveManager.Instance.CurrentData;
        if (data.completedCampaignTutorials == null)
        {
            data.completedCampaignTutorials = new List<int>();
        }

        int tutorialId = (int)tutorialType;
        if (!data.completedCampaignTutorials.Contains(tutorialId))
        {
            data.completedCampaignTutorials.Add(tutorialId);
            SaveManager.Instance.SaveData();
        }
    }

    private void SetTutorialHighlight(Vector2Int position)
    {
        ClearTutorialHighlight();
        highlightedPosition = position;
        gridSpawner.SetTutorialHighlight(position, true);
    }

    private void ClearTutorialHighlight()
    {
        if (highlightedPosition.HasValue)
        {
            gridSpawner.SetTutorialHighlight(
                highlightedPosition.Value,
                false);
            highlightedPosition = null;
        }
    }

    private bool IsMineSelectionTutorialStep()
    {
        return tutorialStep == TutorialStep.SelectCrossMine
            || tutorialStep == TutorialStep.SelectDiagonalMine
            || tutorialStep == TutorialStep.SelectDoubleMine;
    }

    private bool IsSpecialMineTutorial(CampaignTutorialType tutorialType)
    {
        return tutorialType == CampaignTutorialType.CrossMine
            || tutorialType == CampaignTutorialType.DiagonalMine
            || tutorialType == CampaignTutorialType.DoubleMine;
    }

    private void BeginSpecialMinePlacement(
        MineType mineType,
        string message)
    {
        requiredMineType = mineType;
        requiredMinePosition = new Vector2Int(
            currentLevel.width / 2,
            currentLevel.height / 2);
        tutorialStep = TutorialStep.PlaceRequiredMine;
        manager.NotifyCampaignMineSelectorHint(null);
        SetTutorialHighlight(requiredMinePosition);
        manager.NotifyCampaignGuidance(message, highlightDetonate: false);
    }

    public override int calculateScore()
    {
        return 0;
    }

    private void HandleDetonationResolved(bool debrisCleared)
    {
        manager.NotifyDetonateComplete(debrisCleared);
        manager.NotifyCampaignTutorialControls(available: false, active: false);

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
        return !waitingForAdvance
            && !(tutorialActive && IsMineSelectionTutorialStep())
            && base.TryPlaceMine(mineType);
    }

    public override void ReturnMine(MineType mineType = MineType.Standard)
    {
        base.ReturnMine(mineType);
    }
}

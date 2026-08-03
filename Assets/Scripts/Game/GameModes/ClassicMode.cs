using UnityEngine;
using UnityEngine.SceneManagement;

// 10 auto-generated levels played back to back, no level select.
// Timer counts down (~1 minute, shrinking slightly each level).
// Clear all debris before time runs out to advance; finishing level 10 wins.
// Running out of time fails the whole run and restarts from level 1.
// Classic now starts the selected level and returns to the level panel after clear.
public class ClassicMode : GameModeBase
{
    private const int MaxLevel = 10;
    private const int GridWidth = 10;
    private const int GridHeight = 14;
    private const float LevelTime = 60f;
    private const float LevelClearDelay = 1.25f;

    private const int TimeScorePerSecond = 10;
    private const int MineScoreValue = 200;

    private float accumulatedRemainingTime;
    private int accumulatedRemainingMines;
    private bool hasDetonated;
    private bool waitingForAdvance;



    private const int BaseGridSize = 6;
    private const float BaseDebrisChance = 0.15f;
    private const float BaseLevelTime = 60f;

    private int currentLevel = 1;

    public override void Initialize(GameManager manager)
    {
        base.Initialize(manager);
        timer.OnTimerFinished += HandleTimeUp;
    }

    public override void StartGame()
    {
        currentLevel = Mathf.Clamp(GameManager.SelectedClassicLevel, 1, MaxLevel);
        accumulatedRemainingTime = 0f;
        accumulatedRemainingMines = 0;
        BeginLevel();
    }

    public override void ResetGame()
    {
        // A loss (or manual restart) sends the player back to level 1.
        GameManager.SelectedClassicLevel = Mathf.Clamp(GameManager.SelectedClassicLevel, 1, MaxLevel);
        StartGame();
    }

    public override void StopGame()
    {
        base.StopGame();
        timer.OnTimerFinished -= HandleTimeUp;
    }

    private void BeginLevel()
    {
        hasDetonated = false;
        int mineCount = GetMineCountForLevel();
        int debrisCount = GetDebrisCountForLevel();



        mineInventory.Infinite = false;
        mineInventory.ResetInventory();
        mineInventory.SetMineCount(MineType.Standard, mineCount);
        gridSpawner.Configure(GridWidth, GridHeight, 0f);
        gridSpawner.GenerateGrid();
        bool generated = gridSpawner.GenerateSolvableDebris(debrisCount, mineCount);

        if (!generated)
        {
            Debug.LogError("Could not generate a solveable level");
            manager.NotifyGameLost();
            return;
        }

        timer.Configure(LevelTime, countDown: true);
        timer.ResumeTimer();

        manager.NotifyLevelStart(currentLevel, MaxLevel);
        waitingForAdvance = false;
    }

    private int GetMineCountForLevel()
    {
        // Idea is easier levels have more mines, so the mine count will decreases every other level
        return 5;
    }

    private int GetDebrisCountForLevel()
    {
        // Harder levels have more debris
        return 6 + ((currentLevel - 1) * 2);
    }

    public override void OnDetonate()
    {
        // only allow one detonation attempt per stage
        if (hasDetonated || gridSpawner.IsDetonating)
        {
            return;
        }
        hasDetonated = true;
        timer.PauseTimer();

        gridSpawner.DetonateMinesAndResolve(HandleDetonationResolved);
    }

    public override int calculateScore()
    {
        // Scoring is time remaining multiplied by 10 
        // added to mineScore which is number of mines remaining times mineScoreValue of 200
        int timeScore = Mathf.RoundToInt(accumulatedRemainingTime * TimeScorePerSecond);
        int mineScore = accumulatedRemainingMines * MineScoreValue;
        return timeScore + mineScore;
    }

    private async void HandleDetonationResolved(bool debrisCleared)
    {
        manager.NotifyDetonateComplete(debrisCleared);

        if (!debrisCleared)
        {
            manager.NotifyGameLost();
            return;
        }

        timer.PauseTimer();
        accumulatedRemainingTime += Mathf.Max(0f, timer.TimeValue);
        accumulatedRemainingMines += Mathf.Max(0, mineInventory.GetCount(MineType.Standard));

        await SupabaseGameService.MarkClassicLevelCompleted(currentLevel);
        manager.NotifyLevelCleared(currentLevel, MaxLevel);
        waitingForAdvance = true;
        manager.gameStarted = false;
    }
    public void AdvanceToNextLevel()
    {
        if (!waitingForAdvance)
        {
            return;
        }

        waitingForAdvance = false;
        manager.StopGame();
        MainMenu.PageToOpen = MainMenuPage.Classic;

        if (BGPersist.Instance != null)
        {
            BGPersist.Instance.AnimateExitThenLoad("MainMenu");
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
    private void HandleTimeUp()
    {
        OnDetonate();
    }
}

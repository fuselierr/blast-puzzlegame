using UnityEngine;

// 10 auto-generated levels played back to back, no level select.
// Timer counts down (~1 minute, shrinking slightly each level).
// Clear all debris before time runs out to advance; finishing level 10 wins.
// Running out of time fails the whole run and restarts from level 1.
public class ClassicMode : GameModeBase
{
    private const int MaxLevel = 10;
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
        currentLevel = 1;
        BeginLevel();
    }

    public override void ResetGame()
    {
        // A loss (or manual restart) sends the player back to level 1.
        currentLevel = 1;
        BeginLevel();
    }

    public override void StopGame()
    {
        base.StopGame();
        timer.OnTimerFinished -= HandleTimeUp;
    }

    private void BeginLevel()
    {
        mineInventory.Infinite = false;
        mineInventory.ResetInventory();
        mineInventory.SetMineCount(MineType.Standard, Mathf.Max(3, 8 - currentLevel / 2));

        int size = BaseGridSize + currentLevel / 3;
        float debrisChance = BaseDebrisChance + currentLevel * 0.015f;
        gridSpawner.Configure(size, size, debrisChance);
        gridSpawner.GenerateGrid();
        gridSpawner.GenerateDebris();

        float levelTime = Mathf.Max(30f, BaseLevelTime - currentLevel * 2f);
        timer.Configure(levelTime, countDown: true);
        timer.ResumeTimer();

        manager.NotifyLevelStart(currentLevel, MaxLevel);
    }

    public override void OnDetonate()
    {
        gridSpawner.DetonateMinesAndResolve(HandleDetonationResolved);
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

        if (currentLevel >= MaxLevel)
        {
            manager.NotifyGameWon();
        }
        else
        {
            currentLevel++;
            BeginLevel();
        }
    }

    private void HandleTimeUp()
    {
        manager.NotifyGameLost();
    }
}
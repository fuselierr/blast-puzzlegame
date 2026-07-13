using UnityEngine;

// One level, hard difficulty, timer counts up, mines are limited.
// No win condition — the player detonates whenever they want and keeps going.
public class FreeplayMode : GameModeBase
{
    private const int GridWidth = 10;
    private const int GridHeight = 14;
    private const float DebrisChance = 0.15f;
    private const int MineBudget = 8;

    public override void StartGame()
    {
        mineInventory.Infinite = false;
        mineInventory.ResetInventory();
        mineInventory.SetMineCount(MineType.Standard, MineBudget);

        gridSpawner.Configure(GridWidth, GridHeight, DebrisChance);
        gridSpawner.GenerateGrid();
        gridSpawner.GenerateDebris();

        timer.Configure(0f, countDown: false);
        timer.ResumeTimer();

        manager.NotifyLevelStart(1, 1);
    }

    public override void OnDetonate()
    {
        gridSpawner.DetonateMinesAndResolve(HandleDetonationResolved);
    }

    private void HandleDetonationResolved(bool debrisCleared)
    {
        manager.NotifyDetonateComplete(debrisCleared);
    }
}
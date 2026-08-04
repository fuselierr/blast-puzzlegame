using System.Collections.Generic;
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

        if (GameManager.SelectedCustomLevel != null)
        {
            CustomLevelData playableLevel = GameManager.SelectedCustomLevel.CreatePlayableCopy(out Dictionary<MineType, int> mineCounts);

            foreach (var mineCount in mineCounts)
            {
                mineInventory.SetMineCount(mineCount.Key, mineCount.Value);
            }

            gridSpawner.LoadLevel(playableLevel);
        }
        else
        {
            mineInventory.SetMineCount(MineType.Standard, MineBudget);

            gridSpawner.Configure(GridWidth, GridHeight, DebrisChance);
            gridSpawner.GenerateGrid();
            gridSpawner.GenerateDebris(isDailyChallenge: GameManager.IsDailyChallenge);
        }

        timer.Configure(0f, countDown: false);
        timer.ResumeTimer();

        manager.NotifyLevelStart(1, 1);
    }

    public override void OnDetonate()
    {
        timer.PauseTimer();

        gridSpawner.DetonateMinesAndResolve(HandleDetonationResolved);
    }

    public override int calculateScore()
    {
        float timeElapsed = timer.TimeValue;
        int minesRemaining = mineInventory.GetCount(MineType.Standard);
        int score = Mathf.RoundToInt(500f - timeElapsed + (minesRemaining * 200f));
        return score;
    }

    private void HandleDetonationResolved(bool debrisCleared)
    {
        manager.NotifyDetonateComplete(debrisCleared);

        if (!debrisCleared)
        {
            manager.NotifyGameLost();
            return;
        }

        int finalScore = calculateScore();

        if (GameManager.SelectedCustomLevel != null)
        {
            manager.RecordPlayerStat(PlayerStat.CustomLevelWins);
        }
        else if (GameManager.IsDailyChallenge)
        {
            manager.RecordHighScore(
                PlayerHighScore.DailyChallenge,
                finalScore);
        }
        else
        {
            manager.RecordPlayerStat(PlayerStat.FreeplayWins);
            manager.RecordHighScore(
                PlayerHighScore.Freeplay,
                finalScore);
        }

        manager.NotifyGameWon();
    }
}
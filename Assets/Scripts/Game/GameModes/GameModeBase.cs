using UnityEngine;

public abstract class GameModeBase : IGameMode
{
    protected GameManager manager;

    protected GridSpawner gridSpawner => manager.GridSpawner;
    protected MineInventory mineInventory => manager.MineInventory;
    protected Timer timer => manager.Timer;

    public virtual void Initialize(GameManager manager)
    {
        this.manager = manager;
    }

    public abstract void StartGame();

    public virtual void StopGame()
    {
        timer.PauseTimer();
    }

    public virtual void ResetGame()
    {
        StartGame();
    }

    public virtual bool TryPlaceMine(MineType mineType = MineType.Standard)
    {
        if (mineInventory == null)
        {
            Debug.LogError($"{GetType().Name}: MineInventory is not assigned on GameManager.");
            return false;
        }

        bool minePlaced = mineInventory.UseMine(mineType);
        SoundManager soundManager = SoundManager.GetOrCreate();

        if (minePlaced)
        {
            soundManager?.PlayMinePlaced();
        }
        else
        {
            soundManager?.PlayMineBlocked();
        }

        return minePlaced;
    }

    public virtual void ReturnMine(MineType mineType = MineType.Standard)
    {
        SoundManager.GetOrCreate()?.PlayMineReturned();

        if (mineInventory == null)
        {
            Debug.LogError($"{GetType().Name}: MineInventory is not assigned on GameManager.");
            return;
        }

        mineInventory.AddMine(mineType, 1);
    }

    public virtual int calculateScore()
    {
        return 0;
    }

    public abstract void OnDetonate();
}

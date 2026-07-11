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

    public virtual bool TryPlaceMine()
    {
        return mineInventory.UseMine(MineType.Standard);
    }

    public virtual void ReturnMine()
    {
        mineInventory.AddMine(MineType.Standard, 1);
    }

    public abstract void OnDetonate();
}
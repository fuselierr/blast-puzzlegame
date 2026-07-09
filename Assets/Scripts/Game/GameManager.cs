using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private bool limitedMines = true;
    [SerializeField] private MineInventory mineInventory;
    [SerializeField] private MineType activeMineType = MineType.Standard;
    [SerializeField] private GridSpawner gridSpawner;
    [SerializeField] private Timer timer;

    public int CurrentMines => mineInventory?.GetCount(activeMineType) ?? 0;
    public MineInventory MineInventory => mineInventory;
    public MineType ActiveMineType => activeMineType;
    public bool gameStarted = false;

    public void ResetGame()
    {
        if (mineInventory != null)
        {
            mineInventory.ResetInventory();
        }
        gameStarted = true;
        if (gridSpawner != null)
        {
            gridSpawner.GenerateGrid();
            gridSpawner.GenerateDebris();
        }
        if (timer != null)
        {
            timer.ResetTimer();
            timer.ResumeTimer();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (mineInventory == null)
        {
            mineInventory = GetComponent<MineInventory>();
        }
    }

    private void Start()
    {
        if (mineInventory == null)
        {
            Debug.LogError("GameManager requires a MineInventory reference.");
        }
        gameStarted = true;
    }

    public bool TryPlaceMine()
    {
        if (!limitedMines || mineInventory == null)
        {
            return true;
        }

        if (mineInventory.GetCount(activeMineType) <= 0)
        {
            return false;
        }

        return mineInventory.UseMine(activeMineType);
    }

    public void ReturnMine()
    {
        if (!limitedMines || mineInventory == null)
        {
            return;
        }

        mineInventory.AddMine(activeMineType, 1);
    }

    public void StopGame()
    {
        gameStarted = false;
        if (timer != null)
        {
            timer.PauseTimer();
        }
    }
}

using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private bool limitedMines = true;
    [SerializeField] private MineInventory mineInventory;
    [SerializeField] private MineType activeMineType = MineType.Standard;

    public int CurrentMines => mineInventory?.GetCount(activeMineType) ?? 0;
    public MineInventory MineInventory => mineInventory;
    public MineType ActiveMineType => activeMineType;

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
}

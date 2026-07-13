using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Set this (e.g. from a main-menu button) before loading the gameplay
    // scene. Since one scene now handles every mode, this is the only thing
    // that needs to change between "campaign", "freeplay" and "classic".
    public static GameMode SelectedMode = GameMode.Freeplay;

    public static bool IsDailyChallenge = false;

    [Header("Shared Services (all live in this one scene)")]
    [SerializeField] private GridSpawner gridSpawner;
    [SerializeField] private MineInventory mineInventory;
    [SerializeField] private Timer timer;
    [SerializeField] private UIHandler uiHandler;

    [Header("Campaign Data")]
    [SerializeField] private CampaignLevelData[] campaignLevels;

    public GridSpawner GridSpawner => gridSpawner;
    public MineInventory MineInventory => mineInventory;
    public Timer Timer => timer;
    public CampaignLevelData[] CampaignLevels => campaignLevels;
    public int CampaignStartIndex = 0;

    public bool gameStarted { get; set; }

    private IGameMode currentMode;

    // UIHandler subscribes to these instead of reaching into mode-specific
    // managers (e.g. the old ClassicLevelManager reference).
    public event Action<int, int> OnLevelStart; // (levelNumber, totalLevels)
    public event Action OnGameWon;
    public event Action OnGameLost;
    public event Action<bool> OnDetonateComplete; // debrisCleared

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        BeginSelectedMode();
    }

    public void BeginSelectedMode()
    {
        currentMode = CreateMode(SelectedMode);
        currentMode.Initialize(this);
        currentMode.StartGame();
        gameStarted = true;
    }

    private IGameMode CreateMode(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Campaign:
                return new CampaignMode();
            case GameMode.Classic:
                return new ClassicMode();
            case GameMode.Freeplay:
            default:
                return new FreeplayMode();
        }
    }

    public bool TryPlaceMine()
    {
        return gameStarted && currentMode != null && currentMode.TryPlaceMine();
    }

    public void ReturnMine()
    {
        currentMode?.ReturnMine();
    }

    public void StopGame()
    {
        currentMode?.StopGame();
        gameStarted = false;
    }

    public void ResetGame()
    {
        currentMode?.ResetGame();
        gameStarted = true;
    }

    public void Detonate()
    {
        if (!gameStarted || currentMode == null || gridSpawner.IsDetonating)
        {
            return;
        }
        currentMode.OnDetonate();
    }

    // --- Called by mode implementations to report state changes ---

    public void NotifyLevelStart(int levelNumber, int totalLevels)
    {
        OnLevelStart?.Invoke(levelNumber, totalLevels);
    }

    public void NotifyGameWon()
    {
        gameStarted = false;
        OnGameWon?.Invoke();
    }

    public void NotifyGameLost()
    {
        gameStarted = false;
        OnGameLost?.Invoke();
    }

    public void NotifyDetonateComplete(bool debrisCleared)
    {
        OnDetonateComplete?.Invoke(debrisCleared);
    }
}
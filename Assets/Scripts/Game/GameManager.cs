using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Set this (e.g. from a main-menu button) before loading the gameplay
    // scene. Since one scene now handles every mode, this is the only thing
    // that needs to change between "campaign", "freeplay" and "classic".
    public static GameMode SelectedMode = GameMode.Freeplay;

    public static int SelectedCampaignLevelIndex = 0;

    public static int SelectedClassicLevel = 1;

    public static CustomLevelData SelectedCustomLevel;

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
    private MineType _SelectedMineType = MineType.Standard;
    public MineType SelectedMineType {
        get => _SelectedMineType;
        set
        {
            _SelectedMineType = value;
            OnSelectedMineChange?.Invoke(value);
            if (currentMode is CampaignMode campaignMode)
            {
                campaignMode.HandleMineSelected(value);
            }
            Debug.Log($"SelectedMineType set to {value}");
        }
    }
    public Timer Timer => timer;
    public CampaignLevelData[] CampaignLevels => campaignLevels;
    public int CampaignStartIndex => SelectedCampaignLevelIndex;

    public bool gameStarted { get; set; }

    private IGameMode currentMode;

    // UIHandler subscribes to these instead of reaching into mode-specific
    // managers (e.g. the old ClassicLevelManager reference).
    public event Action<int, int> OnLevelStart; // (levelNumber, totalLevels)
    public event Action<int, int> OnLevelCleared;
    public event Action OnGameWon;
    public event Action OnGameLost;
    public event Action<bool> OnDetonateComplete; // debrisCleared
    public event Action<MineType> OnSelectedMineChange;
    public event Action<string, string> OnCampaignTutorial; // title, message
    public event Action<string, bool> OnCampaignGuidance; // message, highlight detonate
    public event Action<bool, bool, bool> OnCampaignTutorialControls; // available, active, ready to start
    public event Action<MineType?> OnCampaignMineSelectorHint;

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

    public bool TryPlaceMine(MineType mineType = MineType.Standard)
    {
        return gameStarted && currentMode != null && currentMode.TryPlaceMine(mineType);
    }

    public void ReturnMine(MineType mineType = MineType.Standard)
    {
        currentMode?.ReturnMine(mineType);
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

    public int CalculateScore()
    {
        return currentMode?.calculateScore() ?? 0;
    }

    public void AdvanceClassicLevel()
    {
        if (currentMode is ClassicMode classicMode)
        {
            classicMode.AdvanceToNextLevel();
        }
    }

    public void AdvanceCampaignLevel()
    {
        if (currentMode is CampaignMode campaignMode)
        {
            campaignMode.AdvanceToNextLevel();
        }
    }

    public void DismissCampaignTutorial()
    {
        if (currentMode is CampaignMode campaignMode)
        {
            campaignMode.DismissTutorial();
        }
    }

    public void SkipCampaignTutorial()
    {
        if (currentMode is CampaignMode campaignMode)
        {
            campaignMode.SkipTutorial();
        }
    }

    public void ReplayCampaignTutorial()
    {
        if (currentMode is CampaignMode campaignMode)
        {
            campaignMode.ReplayTutorial();
        }
    }

    public void StartCampaignLevelAfterTutorial()
    {
        if (currentMode is CampaignMode campaignMode)
        {
            campaignMode.StartLevelAfterTutorial();
        }
    }

    public void NotifyMinePlaced(Vector2Int position, MineType mineType)
    {
        if (currentMode is CampaignMode campaignMode)
        {
            campaignMode.HandleMinePlaced(position, mineType);
        }
    }

    public void NotifyMineReturned(Vector2Int position, MineType mineType)
    {
        if (currentMode is CampaignMode campaignMode)
        {
            campaignMode.HandleMineReturned(position, mineType);
        }
    }


    // --- Called by mode implementations to report state changes ---

    public void NotifyLevelStart(int levelNumber, int totalLevels)
    {
        OnLevelStart?.Invoke(levelNumber, totalLevels);
    }

    public void NotifyLevelCleared(int levelNumber, int totalLevels)
    {
        OnLevelCleared?.Invoke(levelNumber, totalLevels);
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

    public void NotifyCampaignTutorial(string title, string message)
    {
        OnCampaignTutorial?.Invoke(title, message);
    }

    public void NotifyCampaignGuidance(string message, bool highlightDetonate)
    {
        OnCampaignGuidance?.Invoke(message, highlightDetonate);
    }

    public void NotifyCampaignTutorialControls(
        bool available,
        bool active,
        bool readyToStart = false)
    {
        OnCampaignTutorialControls?.Invoke(
            available,
            active,
            readyToStart);
    }

    public void NotifyCampaignMineSelectorHint(MineType? mineType)
    {
        OnCampaignMineSelectorHint?.Invoke(mineType);
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq.Expressions;

public class UIHandler : MonoBehaviour
{
    [SerializeField] private Timer timer;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private MineInventory inventory;

    [SerializeField] private TMP_Text standardText;
    [SerializeField] private TMP_Text crossText;
    [SerializeField] private TMP_Text diagonalText;
    [SerializeField] private TMP_Text doubleText;
    [SerializeField] private ToggleGroup mineSelectToggleGroup;
    [SerializeField] private Toggle standardMineToggle;
    [SerializeField] private Toggle crossMineToggle;
    [SerializeField] private Toggle diagonalMineToggle;
    [SerializeField] private Toggle doubleMineToggle;

    [SerializeField] private TMP_Text levelText; // e.g. "Level 4/10", hidden if totalLevels <= 1

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverMessageText;
    [SerializeField] private GameObject gameWonPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private TMP_Text gameWonScoreCalc;
    [SerializeField] private TMP_Text gameWonFinalScore;
    [SerializeField] private float gameOverDelay = 1f;

    [Header("Level Messages")]
    [SerializeField] private GameObject levelClearPanel;
    [SerializeField] private TMP_Text levelClearMessageText;
    [SerializeField] private TMP_Text levelClearScoreText;
    [SerializeField] private TMP_Text advanceButtonText;

    [SerializeField] private Button advanceButton;

    private bool showingCampaignTutorial;
    private Button campaignMainMenuButton;
    private Vector2 advanceButtonOriginalPosition;
    private Vector2 advanceButtonOriginalSize;
    private GameObject campaignGuidancePanel;
    private TMP_Text campaignGuidanceText;
    private Button detonateButton;
    private Button skipTutorialButton;
    private Button startTutorialLevelButton;
    private Button tutorialHelpButton;
    private GameObject mineSelectorHint;
    private Color detonateButtonOriginalColor = Color.white;

    private void Awake()
    {
        ConfigureLevelMessageLayout();
        CreateCampaignMainMenuButton();
        CreateCampaignGuidancePanel();

        GameObject detonateObject = GameObject.Find("DetonatorButton");
        if (detonateObject != null)
        {
            detonateButton = detonateObject.GetComponent<Button>();
            Image detonateImage = detonateButton != null
                ? detonateButton.GetComponent<Image>()
                : null;
            if (detonateImage != null)
            {
                detonateButtonOriginalColor = detonateImage.color;
            }
        }

        CreateMineSelectorHint();
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLevelStart += HandleLevelStart;
            GameManager.Instance.OnLevelCleared += HandleLevelCleared;
            GameManager.Instance.OnGameWon += HandleGameWon;
            GameManager.Instance.OnGameLost += HandleGameLost;
            GameManager.Instance.OnSelectedMineChange += HandleSelectedMineChange;
            GameManager.Instance.OnCampaignTutorial += HandleCampaignTutorial;
            GameManager.Instance.OnCampaignGuidance += HandleCampaignGuidance;
            GameManager.Instance.OnCampaignTutorialControls +=
                HandleCampaignTutorialControls;
            GameManager.Instance.OnCampaignMineSelectorHint +=
                HandleCampaignMineSelectorHint;

            standardMineToggle.onValueChanged.AddListener(HandleStandardMineToggle);
            crossMineToggle.onValueChanged.AddListener(HandleCrossMineToggle);
            diagonalMineToggle.onValueChanged.AddListener(HandleDiagonalMineToggle);
            doubleMineToggle.onValueChanged.AddListener(HandleDoubleMineToggle);

            HandleSelectedMineChange(GameManager.Instance.SelectedMineType);
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLevelStart -= HandleLevelStart;
            GameManager.Instance.OnLevelCleared -= HandleLevelCleared;
            GameManager.Instance.OnGameWon -= HandleGameWon;
            GameManager.Instance.OnGameLost -= HandleGameLost;
            GameManager.Instance.OnSelectedMineChange -= HandleSelectedMineChange;
            GameManager.Instance.OnCampaignTutorial -= HandleCampaignTutorial;
            GameManager.Instance.OnCampaignGuidance -= HandleCampaignGuidance;
            GameManager.Instance.OnCampaignTutorialControls -=
                HandleCampaignTutorialControls;
            GameManager.Instance.OnCampaignMineSelectorHint -=
                HandleCampaignMineSelectorHint;

            standardMineToggle.onValueChanged.RemoveListener(HandleStandardMineToggle);
            crossMineToggle.onValueChanged.RemoveListener(HandleCrossMineToggle);
            diagonalMineToggle.onValueChanged.RemoveListener(HandleDiagonalMineToggle);
            doubleMineToggle.onValueChanged.RemoveListener(HandleDoubleMineToggle);
        }
    }

    void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (gameWonPanel != null)
        {
            gameWonPanel.SetActive(false);
        }

        if (levelClearPanel != null && !showingCampaignTutorial)
        {
            levelClearPanel.SetActive(false);
        }
        settingsPanel?.SetActive(false);

        ConfigurePanelText(gameOverMessageText);
        ConfigurePanelText(gameWonScoreCalc);
        ConfigurePanelText(gameWonFinalScore);
    }
    private void Update()
    {
        if (timer != null && timerText != null)
        {
            timerText.text = timer.GetFormattedTime();
        }

        if (inventory != null)
        {
            SetMineCountText(standardText, MineType.Standard);
            SetMineCountText(crossText, MineType.Cross);
            SetMineCountText(diagonalText, MineType.Diagonal);
            SetMineCountText(doubleText, MineType.Double);
        }
    }

    // Hooked up to the Detonate button. GameManager routes this to
    // whichever IGameMode is active.
    public void Detonate()
    {
        GameManager.Instance?.Detonate();
    }

    public void RestartGame()
    {
        HideCampaignGuidance();
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        if (gameWonPanel != null)
        {
            gameWonPanel.SetActive(false);
        }
        if (levelClearPanel != null)
        {
            levelClearPanel.SetActive(false);
        }

        GameManager.Instance?.ResetGame();
    }

    public void ReturnToMainMenu()
    {
        bool wasClassicLevel = GameManager.SelectedMode == GameMode.Classic;
        bool wasCampaignLevel = GameManager.SelectedMode == GameMode.Campaign;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StopGame();
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        bool wasCustomLevel = GameManager.SelectedCustomLevel != null;
        GameManager.SelectedCustomLevel = null;

        MainMenu.PageToOpen = wasCustomLevel
            ? MainMenuPage.Community
            : wasClassicLevel
                ? MainMenuPage.Classic
            : wasCampaignLevel
                ? MainMenuPage.Campaign
                : MainMenuPage.Main;

        if (BGPersist.Instance != null)
        {
            BGPersist.Instance.AnimateExitThenLoad("MainMenu");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    private void HandleLevelStart(int levelNumber, int totalLevels)
    {
        if (levelText == null)
        {
            return;
        }
        levelText.text = totalLevels > 1 ? $"Level {levelNumber}/{totalLevels}" : string.Empty;
    }

    private void HandleLevelCleared(int levelNumber, int totalLevels)
    {
        showingCampaignTutorial = false;
        HideCampaignGuidance();
        HideCampaignTutorialControls();

        if (levelText != null)
        {
            levelText.text = $"Level {levelNumber} Clear!";
        }

        if (levelClearMessageText != null)
        {
            levelClearMessageText.text =
                $"Good Job!\nLevel {levelNumber} Cleared";
        }

        if (levelClearScoreText != null)
        {
            if (GameManager.SelectedMode == GameMode.Campaign)
            {
                levelClearScoreText.text = $"Time: {timer.GetFormattedTime()}";
            }
            else
            {
                int score = GameManager.Instance.CalculateScore();
                levelClearScoreText.text = $"Current Score: {score}";
            }
        }

        if (advanceButtonText != null)
        {
            if (GameManager.SelectedMode == GameMode.Classic)
            {
                advanceButtonText.text = "Back to Levels";
            }
            else if (GameManager.SelectedMode == GameMode.Campaign)
            {
                advanceButtonText.text = levelNumber >= totalLevels
                    ? "Finish"
                    : "Next Level";
            }
        }

        if (levelClearPanel != null)
        {
            levelClearPanel.SetActive(true);
        }

        SetCampaignClearButtons(GameManager.SelectedMode == GameMode.Campaign);
    }

    public void AdvanceClassicLevel()
    {
        // The scene's shared advance button already points to this legacy
        // method name, so route both Classic and Campaign from here.
        if (levelClearPanel != null)
        {
            levelClearPanel.SetActive(false);
        }

        if (showingCampaignTutorial)
        {
            showingCampaignTutorial = false;
            GameManager.Instance?.DismissCampaignTutorial();
            return;
        }

        if (GameManager.SelectedMode == GameMode.Campaign)
        {
            GameManager.Instance?.AdvanceCampaignLevel();
        }
        else
        {
            GameManager.Instance?.AdvanceClassicLevel();
        }
    }

    private void HandleCampaignTutorial(string title, string message)
    {
        showingCampaignTutorial = true;
        HideCampaignGuidance();
        SetCampaignClearButtons(false);

        if (levelClearMessageText != null)
        {
            levelClearMessageText.text = title;
        }

        if (levelClearScoreText != null)
        {
            levelClearScoreText.text = message;
        }

        if (advanceButtonText != null)
        {
            advanceButtonText.text = "Start Level";
        }

        if (levelClearPanel != null)
        {
            levelClearPanel.SetActive(true);
        }
    }

    private void CreateCampaignGuidancePanel()
    {
        if (levelClearPanel == null || levelClearScoreText == null)
        {
            return;
        }

        Transform canvasTransform = levelClearPanel.transform.parent;
        GameObject panelObject = new GameObject(
            "CampaignGuidancePanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        panelObject.transform.SetParent(canvasTransform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -280f);
        panelRect.sizeDelta = new Vector2(960f, 220f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
        panelImage.raycastTarget = false;

        campaignGuidanceText = Instantiate(levelClearScoreText, panelObject.transform);
        campaignGuidanceText.name = "CampaignGuidanceText";
        campaignGuidanceText.raycastTarget = false;
        campaignGuidanceText.color = Color.white;
        ConfigurePanelText(campaignGuidanceText);
        campaignGuidanceText.fontSizeMin = 24f;
        campaignGuidanceText.fontSizeMax = 44f;

        RectTransform textRect = campaignGuidanceText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = new Vector2(0.78f, 1f);
        textRect.anchoredPosition = new Vector2(20f, 0f);
        textRect.sizeDelta = new Vector2(-30f, -24f);
        textRect.localScale = Vector3.one;

        campaignGuidancePanel = panelObject;
        CreateCampaignTutorialControls(canvasTransform, panelObject.transform);
        campaignGuidancePanel.SetActive(false);
    }

    private void CreateCampaignTutorialControls(
        Transform canvasTransform,
        Transform guidanceTransform)
    {
        skipTutorialButton = CreateTutorialButton(
            "SkipTutorialButton",
            guidanceTransform,
            "SKIP >>");

        if (skipTutorialButton != null)
        {
            RectTransform skipRect =
                skipTutorialButton.GetComponent<RectTransform>();
            skipRect.anchorMin = new Vector2(1f, 0.5f);
            skipRect.anchorMax = new Vector2(1f, 0.5f);
            skipRect.pivot = new Vector2(1f, 0.5f);
            skipRect.anchoredPosition = new Vector2(-24f, 0f);
            skipRect.sizeDelta = new Vector2(170f, 70f);
            skipTutorialButton.onClick.AddListener(
                () => GameManager.Instance?.SkipCampaignTutorial());
            skipTutorialButton.gameObject.SetActive(false);
        }

        startTutorialLevelButton = CreateTutorialButton(
            "StartTutorialLevelButton",
            guidanceTransform,
            "START");

        if (startTutorialLevelButton != null)
        {
            RectTransform startRect =
                startTutorialLevelButton.GetComponent<RectTransform>();
            startRect.anchorMin = new Vector2(1f, 0.5f);
            startRect.anchorMax = new Vector2(1f, 0.5f);
            startRect.pivot = new Vector2(1f, 0.5f);
            startRect.anchoredPosition = new Vector2(-24f, 0f);
            startRect.sizeDelta = new Vector2(170f, 70f);
            startTutorialLevelButton.onClick.AddListener(
                () => GameManager.Instance?.StartCampaignLevelAfterTutorial());
            startTutorialLevelButton.gameObject.SetActive(false);
        }

        tutorialHelpButton = CreateTutorialButton(
            "CampaignTutorialHelpButton",
            canvasTransform,
            "?");

        if (tutorialHelpButton != null)
        {
            RectTransform helpRect =
                tutorialHelpButton.GetComponent<RectTransform>();
            RectTransform settingsRect = null;
            GameObject settingsObject = GameObject.Find("SettingsButton");
            if (settingsObject != null)
            {
                settingsRect = settingsObject.GetComponent<RectTransform>();
            }

            if (settingsRect != null)
            {
                helpRect.anchorMin = settingsRect.anchorMin;
                helpRect.anchorMax = settingsRect.anchorMax;
                helpRect.pivot = settingsRect.pivot;
                helpRect.anchoredPosition = settingsRect.anchoredPosition
                    + Vector2.down * 125f;
            }
            else
            {
                helpRect.anchorMin = Vector2.one;
                helpRect.anchorMax = Vector2.one;
                helpRect.pivot = Vector2.one;
                helpRect.anchoredPosition = new Vector2(-30f, -170f);
            }

            helpRect.sizeDelta = new Vector2(80f, 70f);
            tutorialHelpButton.onClick.AddListener(
                () => GameManager.Instance?.ReplayCampaignTutorial());
            tutorialHelpButton.gameObject.SetActive(false);
        }
    }

    private Button CreateTutorialButton(
        string objectName,
        Transform parent,
        string label)
    {
        if (advanceButton == null || advanceButtonText == null)
        {
            return null;
        }

        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image sourceImage = advanceButton.GetComponent<Image>();
        Image buttonImage = buttonObject.GetComponent<Image>();
        if (sourceImage != null)
        {
            buttonImage.sprite = sourceImage.sprite;
            buttonImage.color = sourceImage.color;
            buttonImage.material = sourceImage.material;
            buttonImage.type = sourceImage.type;
        }

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        button.transition = advanceButton.transition;
        button.colors = advanceButton.colors;

        TMP_Text buttonText = Instantiate(
            advanceButtonText,
            buttonObject.transform);
        buttonText.name = $"{objectName}Text";
        buttonText.text = label;
        buttonText.raycastTarget = false;

        RectTransform textRect = buttonText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;

        return button;
    }

    private void CreateMineSelectorHint()
    {
        if (standardMineToggle == null)
        {
            return;
        }

        mineSelectorHint = new GameObject(
            "MineSelectorTutorialArrow",
            typeof(RectTransform));
        mineSelectorHint.transform.SetParent(
            standardMineToggle.transform,
            false);

        RectTransform hintRect =
            mineSelectorHint.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 1f);
        hintRect.anchorMax = new Vector2(0.5f, 1f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, 8f);
        hintRect.sizeDelta = new Vector2(36f, 24f);

        CreateChevronSegment(
            mineSelectorHint.transform,
            "Left",
            new Vector2(-6f, 4f),
            -35f);
        CreateChevronSegment(
            mineSelectorHint.transform,
            "Right",
            new Vector2(6f, 4f),
            35f);

        mineSelectorHint.SetActive(false);
    }

    private void CreateChevronSegment(
        Transform parent,
        string segmentName,
        Vector2 position,
        float rotation)
    {
        GameObject segment = new GameObject(
            $"MineSelectorArrow{segmentName}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        segment.transform.SetParent(parent, false);

        RectTransform segmentRect = segment.GetComponent<RectTransform>();
        segmentRect.anchorMin = new Vector2(0.5f, 0.5f);
        segmentRect.anchorMax = new Vector2(0.5f, 0.5f);
        segmentRect.pivot = new Vector2(0.5f, 0.5f);
        segmentRect.anchoredPosition = position;
        segmentRect.sizeDelta = new Vector2(20f, 5f);
        segmentRect.localRotation = Quaternion.Euler(0f, 0f, rotation);

        Image segmentImage = segment.GetComponent<Image>();
        segmentImage.color = new Color(1f, 0.82f, 0.2f, 1f);
        segmentImage.raycastTarget = false;
    }

    private void ConfigureLevelMessageLayout()
    {
        if (levelClearMessageText != null)
        {
            RectTransform titleRect = levelClearMessageText.rectTransform;
            titleRect.anchoredPosition = new Vector2(0f, 140f);
            titleRect.sizeDelta = new Vector2(650f, 90f);
            ConfigurePanelText(levelClearMessageText);
            levelClearMessageText.fontSizeMin = 20f;
            levelClearMessageText.fontSizeMax = 44f;
        }

        if (levelClearScoreText != null)
        {
            RectTransform bodyRect = levelClearScoreText.rectTransform;
            bodyRect.anchoredPosition = new Vector2(0f, -10f);
            bodyRect.sizeDelta = new Vector2(650f, 170f);
            ConfigurePanelText(levelClearScoreText);
            levelClearScoreText.fontSizeMin = 16f;
            levelClearScoreText.fontSizeMax = 30f;
            levelClearScoreText.margin = new Vector4(20f, 10f, 20f, 10f);
        }

        if (advanceButton != null)
        {
            RectTransform buttonRect = advanceButton.GetComponent<RectTransform>();
            buttonRect.anchoredPosition = new Vector2(0f, -190f);
            buttonRect.sizeDelta = new Vector2(360f, 80f);
        }
    }

    private void HandleCampaignGuidance(string message, bool highlightDetonate)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            HideCampaignGuidance();
            return;
        }

        if (campaignGuidanceText != null)
        {
            campaignGuidanceText.text =
                $"<color=#70C46B><b>TUTORIAL</b></color>\n{message}";
        }
        campaignGuidancePanel?.SetActive(true);
        SetDetonateHighlight(highlightDetonate);
    }

    private void HandleCampaignTutorialControls(
        bool available,
        bool active,
        bool readyToStart)
    {
        bool isCampaign = GameManager.SelectedMode == GameMode.Campaign;
        skipTutorialButton?.gameObject.SetActive(
            isCampaign && available && active);
        startTutorialLevelButton?.gameObject.SetActive(
            isCampaign && available && readyToStart);
        tutorialHelpButton?.gameObject.SetActive(
            isCampaign && available && !active && !readyToStart);
    }

    private void HandleCampaignMineSelectorHint(MineType? mineType)
    {
        if (mineSelectorHint == null || !mineType.HasValue)
        {
            mineSelectorHint?.SetActive(false);
            return;
        }

        Toggle targetToggle = null;
        switch (mineType.Value)
        {
            case MineType.Standard:
                targetToggle = standardMineToggle;
                break;
            case MineType.Cross:
                targetToggle = crossMineToggle;
                break;
            case MineType.Diagonal:
                targetToggle = diagonalMineToggle;
                break;
            case MineType.Double:
                targetToggle = doubleMineToggle;
                break;
        }

        if (targetToggle == null)
        {
            mineSelectorHint.SetActive(false);
            return;
        }

        mineSelectorHint.transform.SetParent(targetToggle.transform, false);
        RectTransform hintRect =
            mineSelectorHint.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 1f);
        hintRect.anchorMax = new Vector2(0.5f, 1f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, 8f);
        hintRect.localScale = Vector3.one;
        mineSelectorHint.transform.SetAsLastSibling();
        mineSelectorHint.SetActive(true);
    }

    private void HideCampaignGuidance()
    {
        campaignGuidancePanel?.SetActive(false);
        SetDetonateHighlight(false);
    }

    private void SetDetonateHighlight(bool highlighted)
    {
        Image detonateImage = detonateButton != null
            ? detonateButton.GetComponent<Image>()
            : null;
        if (detonateImage != null)
        {
            detonateImage.color = highlighted
                ? new Color(1f, 0.82f, 0.2f, 1f)
                : detonateButtonOriginalColor;
        }
    }

    private void CreateCampaignMainMenuButton()
    {
        if (advanceButton == null || advanceButtonText == null)
        {
            return;
        }

        RectTransform advanceRect = advanceButton.GetComponent<RectTransform>();
        advanceButtonOriginalPosition = advanceRect.anchoredPosition;
        advanceButtonOriginalSize = advanceRect.sizeDelta;

        GameObject buttonObject = new GameObject(
            "CampaignMainMenuButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(advanceButton.transform.parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = advanceRect.anchorMin;
        buttonRect.anchorMax = advanceRect.anchorMax;
        buttonRect.pivot = advanceRect.pivot;
        buttonRect.anchoredPosition = advanceButtonOriginalPosition;
        buttonRect.sizeDelta = advanceButtonOriginalSize;

        Image sourceImage = advanceButton.GetComponent<Image>();
        Image buttonImage = buttonObject.GetComponent<Image>();
        if (sourceImage != null)
        {
            buttonImage.sprite = sourceImage.sprite;
            buttonImage.color = sourceImage.color;
            buttonImage.material = sourceImage.material;
            buttonImage.type = sourceImage.type;
        }

        campaignMainMenuButton = buttonObject.GetComponent<Button>();
        campaignMainMenuButton.targetGraphic = buttonImage;
        campaignMainMenuButton.transition = advanceButton.transition;
        campaignMainMenuButton.colors = advanceButton.colors;
        campaignMainMenuButton.onClick.AddListener(ReturnToMainMenu);

        TMP_Text buttonText = Instantiate(advanceButtonText, buttonObject.transform);
        buttonText.name = "CampaignMainMenuButtonText";
        buttonText.text = "Main Menu";
        buttonText.raycastTarget = false;

        RectTransform textRect = buttonText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;

        campaignMainMenuButton.gameObject.SetActive(false);
    }

    private void SetCampaignClearButtons(bool showMainMenuButton)
    {
        if (advanceButton == null)
        {
            return;
        }

        RectTransform advanceRect = advanceButton.GetComponent<RectTransform>();
        if (campaignMainMenuButton == null || !showMainMenuButton)
        {
            advanceRect.anchoredPosition = advanceButtonOriginalPosition;
            advanceRect.sizeDelta = advanceButtonOriginalSize;
            campaignMainMenuButton?.gameObject.SetActive(false);
            return;
        }

        const float horizontalOffset = 180f;
        Vector2 splitSize = new Vector2(330f, advanceButtonOriginalSize.y);
        advanceRect.anchoredPosition = advanceButtonOriginalPosition + Vector2.right * horizontalOffset;
        advanceRect.sizeDelta = splitSize;

        RectTransform menuRect = campaignMainMenuButton.GetComponent<RectTransform>();
        menuRect.anchoredPosition = advanceButtonOriginalPosition + Vector2.left * horizontalOffset;
        menuRect.sizeDelta = splitSize;
        campaignMainMenuButton.gameObject.SetActive(true);
    }

    private async void HandleGameWon()
    {
        HideCampaignGuidance();
        HideCampaignTutorialControls();
        if (levelClearPanel != null)
        {
            levelClearPanel.SetActive(false);
        }

        if (gameWonScoreCalc != null)
        {
            if (GameManager.SelectedMode == GameMode.Classic)
            {
                gameWonScoreCalc.text = "remaining time: 100 points per second\n" + "unused mines: 200 points each";
            }
            else if (GameManager.SelectedMode == GameMode.Campaign)
            {
                gameWonScoreCalc.text = "Campaign complete!";
            }
            else
            {
                gameWonScoreCalc.text = $"mines remaining: {inventory.GetCount(MineType.Standard)} * 200\ntime: {timer.GetFormattedTime()} * -1";
            }
        }
        if (gameWonFinalScore != null)
        {
            if (GameManager.SelectedMode == GameMode.Campaign)
            {
                gameWonFinalScore.text = "All campaign levels cleared";
            }
            else
            {
                int finalScore = GameManager.Instance.CalculateScore();
                gameWonFinalScore.text = $"score: {finalScore}";
            }

            // update playerdata
            if (GameManager.SelectedMode == GameMode.Freeplay && GameManager.IsDailyChallenge)
            {
                int finalScore = GameManager.Instance.CalculateScore();
                if (SaveManager.Instance != null)
                {
                    SaveManager.Instance
                        .SaveDailyChallengeScore(
                            finalScore
                        );
                }

                bool saved =
                    await SupabaseGameService
                        .SubmitDailyChallengeScore(
                            finalScore
                        );

                if (!saved)
                {
                    Debug.LogError(
                        "Daily Challenge score was not uploaded."
                    );
                }
            }
        }
        if (gameWonPanel != null)
        {
            StartCoroutine(ShowPanelAfterDelay(gameWonPanel));
        }
    }

    private void HandleGameLost()
    {
        HideCampaignGuidance();
        HideCampaignTutorialControls();
        if (gameOverMessageText != null)
        {
            if (GameManager.SelectedMode == GameMode.Classic)
            {
                gameOverMessageText.text = "Stage Failed!\nRestart Level";
            }
            else if (GameManager.SelectedMode == GameMode.Campaign)
            {
                gameOverMessageText.text = "Debris remains!\nRetry Level";
            }
            else
            {
                gameOverMessageText.text = "Fail!";
            }
        }
        if (gameOverPanel != null)
        {
            StartCoroutine(ShowPanelAfterDelay(gameOverPanel));
        }
    }

    private void HandleSelectedMineChange(MineType mineType)
    {
        switch (mineType)
        {
            // Setting Toggle.isOn directly invokes onValueChanged callback. Using SetIsOnWithoutNotify to avoid circling calls. 
            case MineType.Standard:
                standardMineToggle.SetIsOnWithoutNotify(true);
                break;
            case MineType.Cross:
                crossMineToggle.SetIsOnWithoutNotify(true);
                break;
            case MineType.Diagonal:
                diagonalMineToggle.SetIsOnWithoutNotify(true);
                break;
            case MineType.Double:
                doubleMineToggle.SetIsOnWithoutNotify(true);
                break;
        }
    }

    private void HandleStandardMineToggle(bool isOn)
    {
        if (isOn && GameManager.Instance != null)
        {
            GameManager.Instance.SelectedMineType = MineType.Standard;
        }
    }

    private void HandleCrossMineToggle(bool isOn)
    {
        if (isOn && GameManager.Instance != null)
        {
            GameManager.Instance.SelectedMineType = MineType.Cross;
        }
    }

    private void HandleDiagonalMineToggle(bool isOn)
    {
        if (isOn && GameManager.Instance != null)
        {
            GameManager.Instance.SelectedMineType = MineType.Diagonal;
        }
    }

    private void HandleDoubleMineToggle(bool isOn)
    {
        if (isOn && GameManager.Instance != null)
        {
            GameManager.Instance.SelectedMineType = MineType.Double;
        }
    }

    private void HideCampaignTutorialControls()
    {
        skipTutorialButton?.gameObject.SetActive(false);
        startTutorialLevelButton?.gameObject.SetActive(false);
        tutorialHelpButton?.gameObject.SetActive(false);
        mineSelectorHint?.SetActive(false);
    }

    // private void SetMineSelectCallbacks()
    // {
    //     standardMineToggle.onValueChanged.AddListener((isOn) => {GameManager.Instance.SelectedMineType = MineType.Standard;});
    //     crossMineToggle.onValueChanged.AddListener((isOn) => {GameManager.Instance.SelectedMineType = MineType.Cross;});
    //     diagonalMineToggle.onValueChanged.AddListener((isOn) => {GameManager.Instance.SelectedMineType = MineType.Diagonal;});
    //     doubleMineToggle.onValueChanged.AddListener((isOn) => {GameManager.Instance.SelectedMineType = MineType.Double;});
    // }

    private void SetMineCountText(TMP_Text text, MineType type)
    {
        if (text == null)
        {
            return;
        }
        int count = inventory.GetCount(type);
        text.text = count < 0 ? "\u221e" : count.ToString(); // ∞ for infinite (Campaign)
    }

    private IEnumerator ShowPanelAfterDelay(GameObject panel)
    {
        yield return new WaitForSeconds(gameOverDelay);
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    private void ConfigurePanelText(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 50f;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.Center;
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }
}

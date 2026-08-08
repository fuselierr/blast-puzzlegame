using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button), typeof(Image))]
public class EditorTile : MonoBehaviour
{
    [Header("Input")]
    [SerializeField, Min(0.1f)] private float doubleClickWindow = 0.3f;

    public Vector2Int GridPosition { get; private set; }
    public TileType Type { get; private set; }
    public MineType MineType { get; private set; }

    private Globals globals;
    private Button button;
    private Image tileImage;
    private float lastClickTime = float.NegativeInfinity;

    public event Action<EditorTile> Clicked;
    public event Action<EditorTile> DoubleClicked;

    private void Awake()
    {
        button = GetComponent<Button>();

        // The grid cell's visual is always its root Image. This replaces the
        // prefab sprite instead of layering a child icon over it.
        tileImage = GetComponent<Image>();

        tileImage.raycastTarget = true;
        button.targetGraphic = tileImage;
        button.interactable = true;
        button.onClick.AddListener(HandleClick);
    }

    public void Initialize(
        Globals sharedGlobals,
        Vector2Int position,
        TileType initialType = TileType.Tile,
        MineType initialMineType = MineType.Standard)
    {
        globals = sharedGlobals;
        GridPosition = position;
        name = $"EditorTile_{position.x}_{position.y}";

        SetTile(initialType, initialMineType);
    }

    public void SetTile(
        TileType newType,
        MineType newMineType = MineType.Standard)
    {
        Type = newType;
        MineType = newMineType;
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (tileImage == null || globals == null)
        {
            return;
        }

        Sprite sprite = GetCurrentSprite();
        tileImage.sprite = sprite;
        tileImage.type = Image.Type.Simple;
        tileImage.preserveAspect = true;

        // Keep the transparent Image enabled so a None cell can still be clicked.
        tileImage.color = Type == TileType.None
            ? new Color(1f, 1f, 1f, 0f)
            : Color.white;
    }

    private Sprite GetCurrentSprite()
    {
        switch (Type)
        {
            case TileType.Tile:
                return globals.tileSprite;
            case TileType.Wall:
                return globals.wallSprite;
            case TileType.Mirror:
                return globals.mirrorSprite;
            case TileType.Teleporter:
                return globals.teleporterSprite;
            case TileType.Splitter:
                return globals.splitterSprite;
            case TileType.Debris:
                return globals.debrisSprite;
            case TileType.StrongDebris:
                return globals.strongDebrisSprite;
            case TileType.Mine:
                return globals.GetMineSprite(MineType);
            case TileType.None:
            default:
                return null;
        }
    }

    public string GetTileToken()
    {
        switch (Type)
        {
            case TileType.None:
                return "_";
            case TileType.Tile:
                return "-";
            case TileType.Wall:
                return "W";
            case TileType.Splitter:
                return "S";
            case TileType.Teleporter:
                // EditorManager supplies the actual pair number when saving.
                return "T1";
            case TileType.Debris:
                return "D";
            case TileType.StrongDebris:
                return "H";
            case TileType.Mirror:
                return GetMirrorToken();
            case TileType.Mine:
                return GetMineToken();
            default:
                return "_";
        }
    }

    private string GetMirrorToken()
    {
        return MineType switch
        {
            MineType.Standard => "MH",
            MineType.Cross => "MV",
            MineType.Diagonal => "MR",
            MineType.Double => "ML",
            _ => "MH"
        };
    }

    private string GetMineToken()
    {
        return MineType switch
        {
            MineType.Standard => "NS",
            MineType.Cross => "NC",
            MineType.Diagonal => "NG",
            MineType.Double => "ND",
            _ => "NS"
        };
    }

    private void HandleClick()
    {
        float clickTime = Time.unscaledTime;
        bool isDoubleClick =
            clickTime - lastClickTime <= doubleClickWindow;

        if (isDoubleClick)
        {
            // Reset this so a rapid third click begins a new click sequence.
            lastClickTime = float.NegativeInfinity;

            Debug.Log(
                $"GRID CELL DOUBLE-CLICKED: {GridPosition}",
                gameObject
            );

            DoubleClicked?.Invoke(this);
            return;
        }

        lastClickTime = clickTime;

        Debug.Log(
            $"GRID CELL CLICKED: {GridPosition}",
            gameObject
        );
        Clicked?.Invoke(this);
    }
}
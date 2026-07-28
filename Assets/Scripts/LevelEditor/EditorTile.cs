using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button), typeof(Image))]
public class EditorTile : MonoBehaviour
{
    [SerializeField] private Image tileImage;

    public Vector2Int GridPosition { get; private set; }
    public TileType Type { get; private set; }
    public MineType MineType { get; private set; }

    private EditorGridSpawner gridSpawner;
    private Globals globals;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (tileImage == null)
        {
            tileImage = GetComponent<Image>();
        }

        button.onClick.AddListener(HandleClick);
    }

    public void Initialize(
        EditorGridSpawner owner,
        Globals sharedGlobals,
        Vector2Int position,
        TileType initialType = TileType.Tile,
        MineType initialMineType = MineType.Standard)
    {
        gridSpawner = owner;
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

        if (sprite != null)
        {
            tileImage.SetNativeSize();
        }

        tileImage.rectTransform.localScale = Vector3.one;
        tileImage.rectTransform.sizeDelta = Vector2.zero;

        // Keep the transparent Image enabled so a None cell can still be clicked.
        tileImage.color = Type == TileType.None
            ? new Color(1f, 1f, 1f, 0f)
            : Color.white;

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
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

    private void HandleClick()
    {
        Debug.Log(
            $"GRID CELL CLICKED: {GridPosition}",
            gameObject
        );
        gridSpawner?.ApplySelectedTile(this);
    }
}
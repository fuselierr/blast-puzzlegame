using UnityEngine;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour, IPointerClickHandler
{
    public Vector2Int GridPosition;

    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Flag Overlay")]
    [SerializeField] private SpriteRenderer flagRenderer;
    [SerializeField] private Sprite flagSprite;

    [SerializeField] private Globals globals;

    public TileType Type { get; private set; } = TileType.Tile;
    public MineType mineType { get; private set; } = MineType.Standard;
    public string TeleporterPairId { get; private set; }
    public Tile TeleporterPartner { get; private set; }
    // public DebrisType debrisType { get; private set; } = DebrisType.Standard;
    public bool IsFlagged { get; private set; }

    public void SetTileType(TileType type)
    {
        Type = type;
        if (type != TileType.Teleporter)
        {
            TeleporterPairId = null;
            TeleporterPartner = null;
        }
        SetFlagged(false);
        spriteRenderer.sprite = globals.GetSprite(type);
        // warning if null
        if (spriteRenderer.sprite == null)
        {
            Debug.LogWarning($"Tile: no sprite for {type}.");
        }
        return;
    }

    // Mirror orientation is encoded using the same four-way variant enum.
    public void SetTileType(TileType type, MineType variant)
    {
        mineType = variant;
        SetTileType(type);
    }

    public void SetTeleporterPair(string pairId, Tile partner)
    {
        Type = TileType.Teleporter;
        TeleporterPairId = pairId;
        TeleporterPartner = partner;
        SetFlagged(false);
        spriteRenderer.sprite = globals.GetSprite(TileType.Teleporter);
    }

    // overload for specific MineType
    public void SetTileType(MineType type)
    {
        Type = TileType.Mine;
        mineType = type;
        SetFlagged(false);
        spriteRenderer.sprite = globals.GetSprite(type);
        // warning if null
        if (spriteRenderer.sprite == null)
        {
            Debug.LogWarning($"Tile: no sprite for {type}.");
        }
    }

    public void PlaceDebris()
    {
        Type = TileType.Debris;
        SetFlagged(false);
        spriteRenderer.sprite = globals != null ? globals.GetSprite(TileType.Debris) : null;
    }

    public void ExplodeDebris()
    {
        if (Type == TileType.Debris)
        {
            Type = TileType.Tile;
            SetFlagged(false);
            spriteRenderer.sprite = globals != null ? globals.GetSprite(TileType.Tile) : null;
            SoundManager.GetOrCreate()?.PlayDebrisHit();
        }
        else if (Type == TileType.StrongDebris)
        {
            bool wasFlagged = IsFlagged;
            SetTileType(TileType.Debris);
            SetFlagged(wasFlagged);
            SoundManager.GetOrCreate()?.PlayDebrisHit();
        }
    }

    public void ToggleFlag()
    {
        if (Type != TileType.Debris)
        {
            return;
        }
        SoundManager.GetOrCreate()?.PlayFlagPlaced();
        SetFlagged(!IsFlagged);
    }

    public void SetTutorialHighlight(bool highlighted)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = highlighted
                ? new Color(1f, 0.82f, 0.2f, 1f)
                : Color.white;
        }
    }

    private void SetFlagged(bool flagged)
    {
        IsFlagged = flagged;

        if (flagRenderer != null)
        {
            flagRenderer.enabled = flagged;
        }
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (flagRenderer != null)
        {
            flagRenderer.sprite = flagSprite;
            flagRenderer.enabled = false;
        }
    }

    // Using this instead of OnMouseDown() as it works with the new Unity event system raycasting which stops clicks from bleeding through UI panels. 
    public void OnPointerClick(PointerEventData eventData)
    {
        GameManager gameManager = GameManager.Instance;

        if (gameManager == null || !gameManager.gameStarted)
        {
            return;
        }

        if (Type == TileType.Tile)
        {
            MineType selectedMineType = gameManager.SelectedMineType;
            bool placedMine = gameManager.TryPlaceMine(selectedMineType);

            if (placedMine)
            {
                SetTileType(selectedMineType);
                gameManager.NotifyMinePlaced(GridPosition, selectedMineType);
            }
        }
        else if (Type == TileType.Mine)
        {
            MineType prevMineType = mineType;
            SetTileType(TileType.Tile);
            gameManager.ReturnMine(prevMineType);
            gameManager.NotifyMineReturned(GridPosition, prevMineType);
        }
        else if (Type == TileType.Debris)
        {
            ToggleFlag();
        }
    }
}

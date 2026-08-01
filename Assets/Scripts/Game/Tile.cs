using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int GridPosition;

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite tileSprite;
    [SerializeField] private Sprite debrisSprite;
    [SerializeField] private Sprite toughDebrisSprite;
    [SerializeField] private Sprite mineSprite;
    [SerializeField] private Sprite wallSprite;

    [Header("Flag Overlay")]
    [SerializeField] private SpriteRenderer flagRenderer;
    [SerializeField] private Sprite flagSprite;

    [SerializeField] private Globals globals;

    public TileType Type { get; private set; } = TileType.Tile;
    public MineType mineType { get; private set; } = MineType.Standard;
    // public DebrisType debrisType { get; private set; } = DebrisType.Standard;
    public bool IsFlagged { get; private set; }

    public void SetTileType(TileType type)
    {
        Type = type;
        SetFlagged(false);
        spriteRenderer.sprite = globals.GetSprite(type);
        // warning if null
        if (spriteRenderer.sprite == null)
        {
            Debug.LogWarning($"Tile: no sprite for {type}.");
        }
        return;
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
        spriteRenderer.sprite = debrisSprite;
    }

    public void ExplodeDebris()
    {
        if (Type == TileType.Debris)
        {
            Type = TileType.Tile;
            SetFlagged(false);
            spriteRenderer.sprite = tileSprite;
            SoundManager.Instance.PlayDebrisHit();
        }
        else if (Type == TileType.StrongDebris)
        {
            bool wasFlagged = IsFlagged;
            SetTileType(TileType.Debris);
            SetFlagged(wasFlagged);
            SoundManager.Instance.PlayDebrisHit();
        }
    }

    public void ToggleFlag()
    {
        if (Type != TileType.Debris)
        {
            return;
        }
        SoundManager.Instance.PlayFlagPlaced();
        SetFlagged(!IsFlagged);
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

    private void OnMouseDown()
    {
        if (GameManager.Instance == null || !GameManager.Instance.gameStarted)
        {
            return;
        }
        if (Type == TileType.Tile)
        {
            MineType selectedMineType = GameManager.Instance.SelectedMineType; 
            bool placedMine = GameManager.Instance == null || GameManager.Instance.TryPlaceMine(selectedMineType);
            if (placedMine)
            {
                SetTileType(selectedMineType);
            }
        }
        else if (Type == TileType.Mine)
        {
            MineType prevMineType = mineType; 
            SetTileType(TileType.Tile);
            GameManager.Instance?.ReturnMine(prevMineType);
        }
        else if (Type == TileType.Debris)
        {
            ToggleFlag();
        }
    }
}
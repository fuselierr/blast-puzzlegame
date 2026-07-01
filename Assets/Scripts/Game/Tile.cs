using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int GridPosition;

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite tileSprite;
    [SerializeField] private Sprite debrisSprite;
    [SerializeField] private Sprite mineSprite;

    public TileType Type { get; private set; } = TileType.Tile;

    public void SetTileType(TileType type)
    {
        Type = type;

        switch (type)
        {
            case TileType.Tile:
                spriteRenderer.sprite = tileSprite;
                break;
            case TileType.Debris:
                spriteRenderer.sprite = debrisSprite;
                break;
            case TileType.Mine:
                spriteRenderer.sprite = mineSprite;
                break;
        }
    }

    public void PlaceDebris()
    {
        Type = TileType.Debris;
        spriteRenderer.sprite = debrisSprite;
    }

    public void ExplodeDebris()
    {
        if (Type == TileType.Debris)
        {
            Type = TileType.Tile;
            spriteRenderer.sprite = tileSprite;
        }
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnMouseDown()
    {
        if (Type == TileType.Tile)
        {
            bool placedMine = GameManager.Instance == null || GameManager.Instance.TryPlaceMine();
            if (placedMine)
            {
                SetTileType(TileType.Mine);
            }
        }
        else if (Type == TileType.Mine)
        {
            SetTileType(TileType.Tile);
            GameManager.Instance?.ReturnMine();
        }
    }
}

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
        spriteRenderer.sprite = debrisSprite;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
}

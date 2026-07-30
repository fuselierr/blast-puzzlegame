using UnityEngine;

public enum TileType
{
    Tile,
    Wall,
    Mirror,
    Teleporter,
    Debris,
    StrongDebris,
    ToughDebris, 
    Mine,
    None
}

public enum MineType
{
    Standard,
    Cross,
    Diagonal,
    Double
}

public enum GameMode
{
    Campaign,
    Freeplay,
    Classic
}

[CreateAssetMenu(fileName = "Globals", menuName = "Blast/Globals")]
public class Globals : ScriptableObject
{
    [Header("Tile Sprites")]
    public Sprite tileSprite;
    public Sprite wallSprite;
    public Sprite mirrorSprite;
    public Sprite teleporterSprite;

    [Header("Mine Sprites")]
    public Sprite standardMineSprite;
    public Sprite crossMineSprite;
    public Sprite diagonalMineSprite;
    public Sprite doubleMineSprite;

    [Header("Debris Sprites")]
    public Sprite debrisSprite;
    public Sprite strongDebrisSprite;

    public Sprite GetMineSprite(MineType mineType)
    {
        switch (mineType)
        {
            case MineType.Standard:
                return standardMineSprite;
            case MineType.Cross:
                return crossMineSprite;
            case MineType.Diagonal:
                return diagonalMineSprite;
            case MineType.Double:
                return doubleMineSprite;
            default:
                return null;
        }
    }
}
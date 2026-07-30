using UnityEngine;

public enum TileType
{
    Tile,
    Debris,
    ToughDebris, 
    Wall, 
    Mine
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
using UnityEngine;

[CreateAssetMenu(fileName = "ClassicLevelData", menuName = "Classic/Level Data")]
public class ClassicLevelData : ScriptableObject
{
    // Which classic level this data is for
    public int levelNumber;

    // How many tiles across the grid is 
    public int width = 10;

    // How many tiles down the grid is
    public int height = 14;

    public int minesAvailable = 3;

    // The level is written as text
    // . = normal tile
    // S = stone tile
    [TextArea(10, 20)]
    public string layout;
}
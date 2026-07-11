using UnityEngine;

// A single hand-authored campaign level. Create these via
// Assets > Create > Minesweeper > Campaign Level, then set tileLayout
// (row-major, length must equal width * height) in the inspector.
[CreateAssetMenu(fileName = "CampaignLevel", menuName = "Minesweeper/Campaign Level")]
public class CampaignLevelData : ScriptableObject
{
    public string levelName;
    public int width = 8;
    public int height = 8;

    // Row-major: index = y * width + x. Debris/Tile only — Mine tiles are
    // placed by the player, not authored here.
    public TileType[] tileLayout;

    public TileType GetTile(int x, int y)
    {
        int index = y * width + x;
        if (tileLayout == null || index < 0 || index >= tileLayout.Length)
        {
            return TileType.Tile;
        }
        return tileLayout[index];
    }
}
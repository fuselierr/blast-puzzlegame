using System.Collections.Generic;
using UnityEngine;

public class MineInventory : MonoBehaviour
{
    public Dictionary<MineType, int> mines = new Dictionary<MineType, int>();

    // Modes can set this true so UseMine always succeeds and the UI can
    // show "∞" instead of a number.
    public bool Infinite { get; set; } = false;

    void Awake()
    {
        ResetInventory();
    }

    public void SetMineCount(MineType type, int amount)
    {
        if (!mines.ContainsKey(type))
        {
            mines[type] = 0;
        }
        mines[type] = amount;
    }

    public int GetCount(MineType type)
    {
        if (Infinite)
        {
            return -1; // UI convention: negative = infinite
        }
        return mines.TryGetValue(type, out int value) ? value : 0;
    }

    public bool UseMine(MineType type)
    {
        if (Infinite)
        {
            return true;
        }
        if (GetCount(type) <= 0)
        {
            return false;
        }

        mines[type]--;
        return true;
    }

    public void AddMine(MineType type, int amount)
    {
        if (Infinite)
        {
            return;
        }
        if (!mines.ContainsKey(type))
        {
            mines[type] = 0;
        }

        mines[type] += amount;
    }

    public void ResetInventory()
    {
        mines[MineType.Standard] = 0;
        mines[MineType.Cross] = 0;
        mines[MineType.Diagonal] = 0;
        mines[MineType.Double] = 0;
    }
}

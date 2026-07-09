using System.Collections.Generic;
using UnityEngine;

public class MineInventory : MonoBehaviour
{
    public Dictionary<MineType, int> mines = new Dictionary<MineType, int>();

    void Awake()
    {
        mines[MineType.Standard] = 5;
        mines[MineType.Cross] = 0;
        mines[MineType.Diagonal] = 0;
        mines[MineType.Double] = 0;
    }

    public int GetCount(MineType type)
    {
        return mines.TryGetValue(type, out int value) ? value : 0;
    }

    public bool UseMine(MineType type)
    {
        if (GetCount(type) <= 0) return false;

        mines[type]--;
        return true;
    }

    public void AddMine(MineType type, int amount)
    {
        if (!mines.ContainsKey(type))
            mines[type] = 0;

        mines[type] += amount;
    }

    public void ResetInventory()
    {
        mines[MineType.Standard] = 5;
        mines[MineType.Cross] = 0;
        mines[MineType.Diagonal] = 0;
        mines[MineType.Double] = 0;
    }
}
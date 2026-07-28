using System;
using UnityEngine;

public class EditorManager : MonoBehaviour
{
    [Header("Initial Selection")]
    [SerializeField] private TileType selectedTileType = TileType.Tile;
    [SerializeField] private MineType selectedMineType = MineType.Standard;

    public TileType SelectedTileType => selectedTileType;
    public MineType SelectedMineType => selectedMineType;

    public event Action<TileType, MineType> SelectionChanged;

    public void SelectTileType(
        TileType tileType,
        MineType mineType = MineType.Standard)
    {
        selectedTileType = tileType;
        selectedMineType = mineType;

        Debug.Log(
            $"EDITOR MANAGER SELECTED: {selectedTileType}, {selectedMineType}",
            gameObject
        );

        SelectionChanged?.Invoke(selectedTileType, selectedMineType);
    }
}
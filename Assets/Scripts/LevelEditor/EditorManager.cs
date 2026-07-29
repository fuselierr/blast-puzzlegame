using System;
using UnityEngine;

public class EditorManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject filePanel;

    [Header("Grid")]
    [SerializeField] private EditorGridSpawner gridSpawner;

    [Header("Initial Selection")]
    [SerializeField] private TileType selectedTileType = TileType.Tile;
    [SerializeField] private MineType selectedMineType = MineType.Standard;

    public TileType SelectedTileType => selectedTileType;
    public MineType SelectedMineType => selectedMineType;

    public event Action<TileType, MineType> SelectionChanged;

    private void Start()
    {
        if (filePanel != null)
        {
            filePanel.SetActive(false);
        }

        if (gridSpawner == null)
        {
            gridSpawner = FindObjectOfType<EditorGridSpawner>();
        }

        if (gridSpawner == null)
        {
            Debug.LogError("EditorManager has no EditorGridSpawner!", this);
            return;
        }

        gridSpawner.TileClicked += PaintTile;
        Debug.Log("EDITOR MANAGER CONNECTED TO GRID", this);
    }

    private void OnDestroy()
    {
        if (gridSpawner != null)
        {
            gridSpawner.TileClicked -= PaintTile;
        }
    }

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

    private void PaintTile(EditorTile tile)
    {
        Debug.Log(
            $"PAINTING {tile.GridPosition} WITH " +
            $"{selectedTileType}, {selectedMineType}",
            this
        );

        tile.SetTile(selectedTileType, selectedMineType);
    }

    public void OpenFilePanel()
    {
        if (filePanel != null)
        {
            filePanel.SetActive(true);
        }
    }

    public void CloseFilePanel()
    {
        if (filePanel != null)
        {
            filePanel.SetActive(false);
        }
    }

}
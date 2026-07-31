using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TileButton : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] private TileType tileType = TileType.Tile;
    [SerializeField] private MineType mineType = MineType.Standard;

    [Header("References")]
    [SerializeField] private EditorManager editorManager;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image selectionOverlay;

    private Button button;

    public void Configure(
        EditorManager manager,
        TileType newTileType,
        MineType newMineType,
        Sprite icon)
    {
        if (editorManager != null && isActiveAndEnabled)
        {
            editorManager.SelectionChanged -= RefreshSelection;
        }

        editorManager = manager;
        tileType = newTileType;
        mineType = newMineType;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (editorManager != null && isActiveAndEnabled)
        {
            editorManager.SelectionChanged -= RefreshSelection;
            editorManager.SelectionChanged += RefreshSelection;
            RefreshSelection(
                editorManager.SelectedTileType,
                editorManager.SelectedMineType);
        }

        gameObject.name =
            tileType == TileType.Mine
                ? $"TileButton_{tileType}_{mineType}"
                : $"TileButton_{tileType}";
    }

    private void Awake()
    {
        if (editorManager == null)
        {
            editorManager = FindObjectOfType<EditorManager>();
        }

        button = GetComponent<Button>();
        button.onClick.AddListener(SelectThisTile);

        if (selectionOverlay != null)
        {
            selectionOverlay.raycastTarget = false;
        }
    }

    private void OnEnable()
    {
        if (editorManager != null)
        {
            editorManager.SelectionChanged += RefreshSelection;
            RefreshSelection(
                editorManager.SelectedTileType,
                editorManager.SelectedMineType);
        }
    }

    private void OnDisable()
    {
        if (editorManager != null)
        {
            editorManager.SelectionChanged -= RefreshSelection;
        }
    }

    private void SelectThisTile()
    {
        Debug.Log(
            $"NAVBAR CLICKED: {tileType}, mine: {mineType}",
            gameObject
        );

        if (editorManager == null)
        {
            Debug.LogError("Navbar button has no EditorManager!", gameObject);
            return;
        }

        editorManager.SelectTileType(tileType, mineType);
    }

    private void RefreshSelection(
        TileType selectedTileType,
        MineType selectedMineType)
    {
        if (selectionOverlay == null)
        {
            return;
        }

        bool matchesTile = tileType == selectedTileType;
        bool matchesMine =
            tileType != TileType.Mine ||
            mineType == selectedMineType;

        selectionOverlay.gameObject.SetActive(matchesTile && matchesMine);
    }
}
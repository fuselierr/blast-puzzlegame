using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class TabPopulator : MonoBehaviour
{
    [Serializable]
    private class TileButtonDefinition
    {
        public TileType tileType = TileType.Tile;
        public MineType mineType = MineType.Standard;
        public Sprite icon;
    }

    [Header("References")]
    [SerializeField] private TileButton tileButtonPrefab;
    [SerializeField] private EditorManager editorManager;

    [Header("Buttons")]
    [SerializeField]
    private List<TileButtonDefinition> buttons =
        new List<TileButtonDefinition>();

    [Header("Generation")]
    [SerializeField] private bool populateOnStart = true;
    [SerializeField] private bool clearExistingChildren = true;

    private void Start()
    {
        if (populateOnStart)
        {
            Populate();
        }
    }

    [ContextMenu("Populate Tile Buttons")]
    public void Populate()
    {
        if (tileButtonPrefab == null)
        {
            Debug.LogError(
                "TileButtonGridPopulator has no TileButton prefab.",
                this);
            return;
        }

        if (editorManager == null)
        {
            editorManager = FindObjectOfType<EditorManager>();
        }

        if (editorManager == null)
        {
            Debug.LogError(
                "TileButtonGridPopulator could not find an EditorManager.",
                this);
            return;
        }

        if (clearExistingChildren)
        {
            ClearChildren();
        }

        foreach (TileButtonDefinition definition in buttons)
        {
            if (definition == null)
            {
                continue;
            }

            TileButton tileButton =
                Instantiate(tileButtonPrefab, transform, false);

            tileButton.Configure(
                editorManager,
                definition.tileType,
                definition.mineType,
                definition.icon);
        }
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;

            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }
}
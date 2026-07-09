using UnityEngine;
using TMPro;

public class UIHandler : MonoBehaviour
{
    [SerializeField] private GridSpawner gridSpawner;
    [SerializeField] private Timer timer;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private MineInventory inventory;
    [SerializeField] private ClassicLevelManager classicLevelManager;

    [SerializeField] private TMP_Text standardText;
    [SerializeField] private TMP_Text crossText;
    [SerializeField] private TMP_Text diagonalText;
    [SerializeField] private TMP_Text doubleText;

    private void Update()
    {
        if (timer != null && timerText != null)
        {
            timerText.text = timer.GetFormattedTime();
        }

        if (inventory != null)
        {
            standardText.text = inventory.GetCount(MineType.Standard).ToString();
            crossText.text = inventory.GetCount(MineType.Cross).ToString();
            diagonalText.text = inventory.GetCount(MineType.Diagonal).ToString();
            doubleText.text = inventory.GetCount(MineType.Double).ToString();
        }
    }

    public void Detonate()
    {
        if (classicLevelManager != null)
        {
            classicLevelManager.DetonateMines();
        }
        if (gridSpawner != null)
        {
            gridSpawner.DetonateMines();
        }
    }
}

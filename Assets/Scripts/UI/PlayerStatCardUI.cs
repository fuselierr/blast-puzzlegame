using TMPro;
using UnityEngine;

public class PlayerStatCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text statNameText;
    [SerializeField] private TMP_Text statValueText;

    public void SetStat(string statName, int statValue)
    {
        if (statNameText != null)
        {
            statNameText.text = statName;
        }

        if (statValueText != null)
        {
            statValueText.text = statValue.ToString("N0");
        }
    }
}
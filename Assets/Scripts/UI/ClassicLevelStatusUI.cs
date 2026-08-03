using UnityEngine.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ClassicLevelStatusUI: MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button button;

    [Header("Status Colours")]
    [SerializeField]
    private Color incompleteColour = new Color32(120,120,120,255);

    [Header("Status Colours")]
    [SerializeField]
    private Color completedColour = new Color32(55,190,80,255);

    [SerializeField]
    private Color textColour = Color.white;

    [SerializeField]
    private float lockedAlpha = 0.35f;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    public void Setup(int levelNumber, bool completed, bool unlocked, UnityAction onClick)
    {
        if (levelText != null)
        {
            levelText.text = levelNumber.ToString();
            Color resolvedTextColour = textColour;
            resolvedTextColour.a = unlocked ? textColour.a : lockedAlpha;
            levelText.color = resolvedTextColour;
        }
        if (backgroundImage != null)
        {
            Color resolvedColour = completed ? completedColour : incompleteColour;
            resolvedColour.a = unlocked ? resolvedColour.a : lockedAlpha;
            backgroundImage.color = resolvedColour;
        }

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = unlocked;
            if (unlocked && onClick != null)
            {
                button.onClick.AddListener(onClick);
            }
        }
    }

}

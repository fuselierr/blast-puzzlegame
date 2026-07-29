using UnityEngine;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    [System.Serializable]
    private class Tab
    {
        public Button button;
        public GameObject panel;
        public Image buttonBackground;
    }

    [SerializeField] private Tab[] tabs;
    [SerializeField] private int startingTab;

    [Header("Colours")]
    [SerializeField] private Color selectedColour = Color.white;
    [SerializeField] private Color unselectedColour = Color.gray;

    private int currentTab = -1;

    private void Awake()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            int tabIndex = i;
            tabs[i].button.onClick.AddListener(() => SelectTab(tabIndex));
        }

        SelectTab(startingTab);
    }

    public void SelectTab(int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= tabs.Length)
        {
            return;
        }

        currentTab = selectedIndex;

        for (int i = 0; i < tabs.Length; i++)
        {
            bool isSelected = i == selectedIndex;

            tabs[i].panel.SetActive(isSelected);

            if (tabs[i].buttonBackground != null)
            {
                tabs[i].buttonBackground.color =
                    isSelected ? selectedColour : unselectedColour;
            }

            tabs[i].button.interactable = !isSelected;
        }
    }
}
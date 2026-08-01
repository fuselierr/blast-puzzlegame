using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelNamePopupController : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject popupLayer;
    [SerializeField] private GameObject nameLevelPanel;

    [Header("Controls")]
    [SerializeField] private TMP_InputField levelNameInput;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button doneButton;

    private Action<string> onDone;

    private void Awake()
    {
        cancelButton.onClick.AddListener(Cancel);
        doneButton.onClick.AddListener(Confirm);

        // Ensure the popup layer is hidden at startup, but don't let
        // Hide() toggle the shared popup layer off — other popups may
        // share the same layer (e.g. the kebab menu). We'll only hide
        // the name panel here.
        if (popupLayer != null)
        {
            popupLayer.SetActive(false);
        }

        if (nameLevelPanel != null)
        {
            nameLevelPanel.SetActive(false);
        }
    }

    public void Show(
        string startingName = "Untitled",
        Action<string> doneCallback = null)
    {
        onDone = doneCallback;

        // The parent must be active before its child can appear.
        popupLayer.SetActive(true);
        nameLevelPanel.SetActive(true);

        levelNameInput.text = startingName;
        levelNameInput.Select();
        levelNameInput.ActivateInputField();
    }

    public void Hide()
    {
        // Only hide the name-level panel here. Do not deactivate
        // the shared popup layer because other UI (kebab menu) may
        // also be using it.
        if (nameLevelPanel != null)
        {
            nameLevelPanel.SetActive(false);
        }

        onDone = null;
    }

    private void Cancel()
    {
        Hide();
    }

    private void Confirm()
    {
        string levelName = levelNameInput.text.Trim();

        if (string.IsNullOrWhiteSpace(levelName))
        {
            levelNameInput.ActivateInputField();
            return;
        }

        Action<string> callback = onDone;
        Hide();
        callback?.Invoke(levelName);
    }

    private void OnDestroy()
    {
        cancelButton.onClick.RemoveListener(Cancel);
        doneButton.onClick.RemoveListener(Confirm);
    }
}
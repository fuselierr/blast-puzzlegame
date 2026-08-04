using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class FeedbackManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TMP_InputField messageInputField;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Google Web App URL")]
    [Tooltip("Paste your deployed Google Apps Script Web App URL here")]
    [SerializeField] private string webAppUrl = "";

    private string currentCategory = "feedback";

    private void Start()
    {
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);
    }

    public void OpenFeedbackModal()
    {
        currentCategory = "feedback";
        headerText.text = "SEND FEEDBACK";
        messageInputField.text = "";
        statusText.text = "";
        feedbackPanel.SetActive(true);
    }

    public void OpenBugReportModal()
    {
        currentCategory = "bug";
        headerText.text = "REPORT A BUG";
        messageInputField.text = "";
        statusText.text = "";
        feedbackPanel.SetActive(true);
    }

    public void CloseModal()
    {
        feedbackPanel.SetActive(false);
    }

    public void OnClickSubmit()
    {
        if (string.IsNullOrWhiteSpace(messageInputField.text))
        {
            statusText.text = "Please enter a message.";
            return;
        }

        StartCoroutine(SendFeedbackCoroutine());
    }

    private IEnumerator SendFeedbackCoroutine()
    {
        statusText.text = "Sending...";

        FeedbackData data = new FeedbackData
        {
            category = currentCategory,
            message = messageInputField.text
        };

        string jsonPayload = JsonUtility.ToJson(data);

        using (UnityWebRequest request = new UnityWebRequest(webAppUrl, "POST"))
        {
            request.redirectLimit = 10;
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                statusText.text = "Submitted! Thank you.";
                yield return new WaitForSeconds(1.2f);
                CloseModal();
            }
            else
            {
                statusText.text = "Failed to send. Try again.";
                Debug.LogError("Feedback submit error: " + request.error);
            }
        }
    }

    [System.Serializable]
    private class FeedbackData
    {
        public string category;
        public string message;
    }
}
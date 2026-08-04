using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LogoutController : MonoBehaviour
{
    [SerializeField] private Button logoutButton;
    [SerializeField] private string startSceneName = "Start";

    private bool isLoggingOut;

    private void Awake()
    {
        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveListener(Logout);
            logoutButton.onClick.AddListener(Logout);
        }
    }

    public async void Logout()
    {
        if (isLoggingOut)
        {
            return;
        }

        isLoggingOut = true;

        if (logoutButton != null)
        {
            logoutButton.interactable = false;
        }

        try
        {
            if (SupabaseManager.Instance == null)
            {
                throw new System.InvalidOperationException(
                    "SupabaseManager was not bootstrapped.");
            }

            await SupabaseManager.Instance.EnsureInitialized();

            await SupabaseManager.Instance
                .SupabaseClient
                .Auth
                .SignOut();

            SceneManager.LoadScene(startSceneName);
        }
        catch (System.Exception exception)
        {
            Debug.LogError("Logout failed: " + exception, this);

            if (logoutButton != null)
            {
                logoutButton.interactable = true;
            }

            isLoggingOut = false;
        }
    }

    private void OnDestroy()
    {
        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveListener(Logout);
        }
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading.Tasks;

public class AuthLauncher : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TextMeshProUGUI statusText;

    private async void Start()
    {
        if (SupabaseManager.Instance != null)
        {
            statusText.text = "Connecting...";
            await SupabaseManager.Instance.EnsureInitialized();

            var currentSession = SupabaseManager.Instance.SupabaseClient.Auth.CurrentSession;
            if (currentSession?.User != null && !string.IsNullOrEmpty(currentSession.AccessToken))
            {
                statusText.text = "Welcome back! Verifying profile...";

                // Extract username from email fallback (e.g. "testplayer@game.local" -> "testplayer")
                string username = currentSession.User.Email.Split('@')[0];
                await EnsureUserDataExists(currentSession.User.Id, username);

                LoadMainMenu();
            }
            else
            {
                statusText.text = "Please sign in or create an account.";
            }
        }
    }

    public async void OnClickSignUp()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (username.Length < 5 || username.Length > 15)
        {
            statusText.text = "Username must be 5 to 15 characters.";
            return;
        }

        if (password.Length < 6 || password.Length > 12)
        {
            statusText.text = "Password must be 6 to 12 characters.";
            return;
        }

        string email = $"{username.ToLower()}@game.local";
        statusText.text = "Creating account...";

        try
        {
            var session = await SupabaseManager.Instance.SupabaseClient.Auth.SignUp(email, password);
            if (session?.User != null)
            {
                statusText.text = "Setting up profile...";
                await EnsureUserDataExists(session.User.Id, username);

                statusText.text = "Account created! Loading game...";
                LoadMainMenu();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Sign Up Error: " + ex);
            statusText.text = ex.Message.Contains("already registered")
                ? "Username is already taken."
                : "Sign Up Failed. Check connection & try again.";
        }
    }

    public async void OnClickLogin()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Please enter username and password.";
            return;
        }

        string email = $"{username.ToLower()}@game.local";
        statusText.text = "Logging in...";

        try
        {
            var session = await SupabaseManager.Instance.SupabaseClient.Auth.SignIn(email, password);
            if (session?.User != null)
            {
                statusText.text = "Verifying profile...";
                await EnsureUserDataExists(session.User.Id, username);

                statusText.text = "Success! Loading Main Menu...";
                LoadMainMenu();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Login Error: " + ex);
            statusText.text = "Login Failed: Invalid username or password.";
        }
    }

    /// <summary>
    /// Checks if profile & progress records exist for the given user ID. 
    /// Creates them automatically if missing.
    /// </summary>
    private async Task EnsureUserDataExists(string userId, string username)
    {
        try
        {
            // Check Profile
            var profileResult = await SupabaseManager.Instance.SupabaseClient
                .From<Profile>()
                .Where(p => p.Id == userId)
                .Get();

            if (profileResult.Models.Count == 0)
            {
                var newProfile = new Profile { Id = userId, Username = username, Points = 0 };
                await SupabaseManager.Instance.SupabaseClient.From<Profile>().Insert(newProfile);
            }

            // Check Progress
            var progressResult = await SupabaseManager.Instance.SupabaseClient
                .From<Progress>()
                .Where(p => p.Id == userId)
                .Get();

            if (progressResult.Models.Count == 0)
            {
                var newProgress = new Progress { Id = userId, Level = 1 };
                await SupabaseManager.Instance.SupabaseClient.From<Progress>().Insert(newProgress);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error ensuring user database rows: " + ex.Message);
        }
    }

    private void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
using UnityEngine;
using TMPro;
using System.Threading.Tasks;

public class AuthLauncher : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private StartGame startGame;

    private bool isSubmitting;

    private void Awake()
    {
        if (startGame == null)
        {
            startGame = FindObjectOfType<StartGame>();
        }
    }

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
                string username = GetUsernameFromEmail(
                    currentSession.User.Email);
                await EnsureUserDataExists(currentSession.User.Id, username);

                // StartGame detects this restored session and shows the Start
                // button immediately. Do not skip directly to MainMenu.
                statusText.text = string.Empty;
            }
            else
            {
                statusText.text = "Please sign in or create an account.";
            }
        }
    }

    public async void OnClickSignUp()
    {
        if (isSubmitting)
        {
            return;
        }

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
        isSubmitting = true;

        try
        {
            await SupabaseManager.Instance.EnsureInitialized();

            var session = await SupabaseManager.Instance.SupabaseClient.Auth.SignUp(email, password);
            if (session?.User != null &&
                !string.IsNullOrWhiteSpace(session.AccessToken))
            {
                statusText.text = "Setting up profile...";
                await SupabaseManager.Instance.SupabaseClient.Auth.SetSession(
                    session.AccessToken,
                    session.RefreshToken);
                SupabaseManager.Instance.SaveCurrentSession();
                await EnsureUserDataExists(session.User.Id, username);

                statusText.text = "Account created!";
                NotifyAuthenticationSucceeded();
            }
            else
            {
                statusText.text =
                    "Account created. Confirm your email before signing in.";
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Sign Up Error: " + ex);
            statusText.text = ex.Message.Contains("already registered")
                ? "Username is already taken."
                : "Sign Up Failed. Check connection & try again.";
        }
        finally
        {
            isSubmitting = false;
        }
    }

    public async void OnClickLogin()
    {
        if (isSubmitting)
        {
            return;
        }

        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Please enter username and password.";
            return;
        }

        string email = $"{username.ToLower()}@game.local";
        statusText.text = "Logging in...";
        isSubmitting = true;

        try
        {
            await SupabaseManager.Instance.EnsureInitialized();

            var session = await SupabaseManager.Instance.SupabaseClient.Auth.SignIn(email, password);
            if (session?.User != null &&
                !string.IsNullOrWhiteSpace(session.AccessToken))
            {
                statusText.text = "Verifying profile...";
                await SupabaseManager.Instance.SupabaseClient.Auth.SetSession(
                    session.AccessToken,
                    session.RefreshToken);
                SupabaseManager.Instance.SaveCurrentSession();
                await EnsureUserDataExists(session.User.Id, username);

                statusText.text = "Login successful!";
                NotifyAuthenticationSucceeded();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Login Error: " + ex);
            statusText.text = "Login Failed: Invalid username or password.";
        }
        finally
        {
            isSubmitting = false;
        }
    }

    /// <summary>
    /// Checks if profile & progress records exist for the given user ID. 
    /// Creates them automatically if missing.
    /// </summary>
    private async Task EnsureUserDataExists(string userId, string username)
    {
        // Every call below is awaited. If an insert fails, let the exception
        // reach the sign-up/login handler instead of reporting success.
        var profileResult = await SupabaseManager.Instance.SupabaseClient
            .From<Profile>()
            .Where(p => p.Id == userId)
            .Get();

        if (profileResult.Models.Count == 0)
        {
            var newProfile = new Profile
            {
                Id = userId,
                Username = username,
                Points = 0
            };

            await SupabaseManager.Instance.SupabaseClient
                .From<Profile>()
                .Insert(newProfile);
        }

        var progressResult = await SupabaseManager.Instance.SupabaseClient
            .From<Progress>()
            .Where(p => p.Id == userId)
            .Get();

        if (progressResult.Models.Count == 0)
        {
            var newProgress = new Progress
            {
                Id = userId,
                Level = 1
            };

            await SupabaseManager.Instance.SupabaseClient
                .From<Progress>()
                .Insert(newProgress);
        }
    }

    private void NotifyAuthenticationSucceeded()
    {
        if (startGame == null)
        {
            startGame = FindObjectOfType<StartGame>();
        }

        if (startGame == null)
        {
            Debug.LogError(
                "AuthLauncher could not find StartGame.",
                this);
            return;
        }

        startGame.HandleLoginSucceeded();
    }

    private static string GetUsernameFromEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "Player";
        }

        int separatorIndex = email.IndexOf('@');
        return separatorIndex > 0
            ? email.Substring(0, separatorIndex)
            : email;
    }
}

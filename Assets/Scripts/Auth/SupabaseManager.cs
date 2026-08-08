using UnityEngine;
using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class SupabaseManager : MonoBehaviour
{
    public static SupabaseManager Instance { get; private set; }
    public Supabase.Client SupabaseClient { get; private set; }

    private TaskCompletionSource<bool> initTcs = new TaskCompletionSource<bool>();
    private static readonly UnitySupabaseSessionHandler sessionHandler =
        new UnitySupabaseSessionHandler();

    private string supabaseUrl = "https://bxmxhkkfqkfkzewpbgxs.supabase.co";
    private string supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImJ4bXhoa2tmcWtma3pld3BiZ3hzIiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODUwOTMzMDMsImV4cCI6MjEwMDY2OTMwM30.DZl_rqN7d97_rB0zqapddpRNF-sFV1vU1hhjBPeeH4U";

    private async void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false,
                SessionHandler = sessionHandler
            };

            try
            {
                SupabaseClient = new Supabase.Client(
                    supabaseUrl,
                    supabaseKey,
                    options);
                await SupabaseClient.InitializeAsync();
                initTcs.TrySetResult(true);
            }
            catch (System.Exception exception)
            {
                Debug.LogError("Supabase initialization failed: " + exception.Message);
                initTcs.TrySetException(exception);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async Task EnsureInitialized()
    {
        await initTcs.Task;
    }

    public void SaveCurrentSession()
    {
        Session session = SupabaseClient?.Auth?.CurrentSession;

        if (session == null)
        {
            return;
        }

        sessionHandler.SaveSession(session);
    }

    public void ClearSavedSession()
    {
        sessionHandler.DestroySession();
    }

    public async Task<CustomLevelDataRow> PublishCustomLevel(
        CustomLevelData level,
        bool published = true)
    {
        await EnsureInitialized();
        string userId = GetCurrentUserId();

        if (level == null)
            throw new ArgumentNullException(nameof(level));

        // Do not upload malformed layouts to the community table.
        if (!level.TryToTileGrid(out _, out string layoutError))
            throw new ArgumentException(layoutError, nameof(level));

        DateTime now = DateTime.UtcNow;
        var row = new CustomLevelDataRow
        {
            Id = Guid.NewGuid().ToString(),
            AuthorId = userId,
            LevelName = level.levelName?.Trim(),
            Width = level.width,
            Height = level.height,
            Layout = level.layout,
            Verified = level.verified,
            Published = published,
            LikeCount = 0,
            DownloadCount = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        var response = await SupabaseClient
            .From<CustomLevelDataRow>()
            .Insert(row);

        return response.Models.FirstOrDefault();
    }

    public async Task<List<CustomLevelDataRow>> GetCommunityLevels()
    {
        await EnsureInitialized();

        var response = await SupabaseClient
            .From<CustomLevelDataRow>()
            .Where(level => level.Published == true)
            .Get();

        return response.Models
            .OrderByDescending(level => level.CreatedAt)
            .ToList();
    }

    public async Task<string> GetUsername(string userId)
    {
        await EnsureInitialized();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return "Unknown Creator";
        }

        Profile profile = await SupabaseClient
            .From<Profile>()
            .Where(row => row.Id == userId)
            .Single();

        return profile != null && !string.IsNullOrWhiteSpace(profile.Username)
            ? profile.Username
            : "Unknown Creator";
    }

    public async Task<List<CustomLevelDataRow>> GetMyCustomLevels()
    {
        await EnsureInitialized();
        string userId = GetCurrentUserId();

        var response = await SupabaseClient
            .From<CustomLevelDataRow>()
            .Where(level => level.AuthorId == userId)
            .Get();

        return response.Models
            .OrderByDescending(level => level.UpdatedAt)
            .ToList();
    }

    public async Task<CustomLevelDataRow> GetCustomLevel(string levelId)
    {
        await EnsureInitialized();
        RequireId(levelId, nameof(levelId));

        var response = await SupabaseClient
            .From<CustomLevelDataRow>()
            .Where(level => level.Id == levelId)
            .Single();

        return response;
    }

    public async Task<CustomLevelDataRow> UpdateCustomLevel(
        string levelId,
        CustomLevelData level,
        bool published)
    {
        await EnsureInitialized();
        string userId = GetCurrentUserId();
        RequireId(levelId, nameof(levelId));

        if (level == null)
            throw new ArgumentNullException(nameof(level));

        if (!level.TryToTileGrid(out _, out string layoutError))
            throw new ArgumentException(layoutError, nameof(level));

        var existing = await GetCustomLevel(levelId);
        if (existing == null || existing.AuthorId != userId)
            throw new InvalidOperationException("You can only update your own level.");

        existing.LevelName = level.levelName?.Trim();
        existing.Width = level.width;
        existing.Height = level.height;
        existing.Layout = level.layout;
        existing.Verified = level.verified;
        existing.Published = published;
        existing.UpdatedAt = DateTime.UtcNow;

        var response = await SupabaseClient
            .From<CustomLevelDataRow>()
            .Where(row => row.Id == levelId)
            .Update(existing);

        return response.Models.FirstOrDefault();
    }

    public async Task DeleteCustomLevel(string levelId)
    {
        await EnsureInitialized();
        GetCurrentUserId();
        RequireId(levelId, nameof(levelId));

        await SupabaseClient
            .From<CustomLevelDataRow>()
            .Where(level => level.Id == levelId)
            .Delete();
    }

    public async Task<bool> HasLikedCustomLevel(string levelId)
    {
        await EnsureInitialized();
        string userId = GetCurrentUserId();
        RequireId(levelId, nameof(levelId));

        var response = await SupabaseClient
            .From<CustomLevelDataLikeRow>()
            .Where(like => like.LevelId == levelId && like.UserId == userId)
            .Get();

        return response.Models.Count > 0;
    }

    public async Task<List<string>> GetLikedCustomLevelIds()
    {
        await EnsureInitialized();
        string userId = GetCurrentUserId();

        var response = await SupabaseClient
            .From<CustomLevelDataLikeRow>()
            .Where(like => like.UserId == userId)
            .Get();

        return response.Models
            .Where(like => !string.IsNullOrWhiteSpace(like.LevelId))
            .Select(like => like.LevelId)
            .Distinct()
            .ToList();
    }

    public async Task<bool> LikeCustomLevel(string levelId)
    {
        await EnsureInitialized();
        string userId = GetCurrentUserId();
        RequireId(levelId, nameof(levelId));

        if (await HasLikedCustomLevel(levelId))
            return false;

        await SupabaseClient
            .From<CustomLevelDataLikeRow>()
            .Insert(new CustomLevelDataLikeRow
            {
                LevelId = levelId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });

        return true;
    }

    public async Task<bool> UnlikeCustomLevel(string levelId)
    {
        await EnsureInitialized();
        string userId = GetCurrentUserId();
        RequireId(levelId, nameof(levelId));

        if (!await HasLikedCustomLevel(levelId))
            return false;

        await SupabaseClient
            .From<CustomLevelDataLikeRow>()
            .Where(like => like.LevelId == levelId && like.UserId == userId)
            .Delete();

        return true;
    }

    public async Task RegisterCustomLevelDownload(string levelId)
    {
        await EnsureInitialized();
        GetCurrentUserId();
        RequireId(levelId, nameof(levelId));

        await SupabaseClient.Rpc(
            "increment_custom_level_download",
            new { p_level_id = levelId });
    }

    public async Task<PlayerStats> GetPlayerStats()
    {
        await EnsureInitialized();
        string userId = GetCurrentUserId();

        // Defensive: the database trigger normally creates this row.
        await SupabaseClient.Rpc(
            "ensure_player_stats",
            new Dictionary<string, object>());

        Profile profile = await SupabaseClient
            .From<Profile>()
            .Where(row => row.Id == userId)
            .Single();

        PlayerStatsRow stats = await SupabaseClient
            .From<PlayerStatsRow>()
            .Where(row => row.UserId == userId)
            .Single();

        List<CustomLevelDataRow> ownedLevels = await GetMyCustomLevels();
        long totalLikes = ownedLevels.Sum(level => (long)level.LikeCount);
        long totalPlays = ownedLevels.Sum(level => (long)level.DownloadCount);

        return new PlayerStats
        {
            userName = profile?.Username ?? "Unknown Player",
            minesPlaced = stats?.MinesPlaced ?? 0,
            debrisBroken = stats?.DebrisBroken ?? 0,
            freeplayWins = stats?.FreeplayWins ?? 0,
            classicWins = stats?.ClassicWins ?? 0,
            customLevelWins = stats?.CustomLevelWins ?? 0,
            dailyChallengeHighScore = stats?.DailyChallengeHighScore ?? 0,
            freeplayHighScore = stats?.FreeplayHighScore ?? 0,
            classicHighScore = stats?.ClassicHighScore ?? 0,
            customLevelLikes = ClampToInt(totalLikes),
            customLevelPlays = ClampToInt(totalPlays)
        };
    }

    public async Task IncrementPlayerStat(PlayerStat stat, int amount = 1)
    {
        await EnsureInitialized();
        GetCurrentUserId();

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");

        await SupabaseClient.Rpc(
            "increment_player_stat",
            new
            {
                p_stat = GetStatColumn(stat),
                p_amount = amount
            });
    }

    public async Task SubmitPlayerHighScore(PlayerHighScore mode, int score)
    {
        await EnsureInitialized();
        GetCurrentUserId();

        if (score < 0)
            throw new ArgumentOutOfRangeException(nameof(score), "Score cannot be negative.");

        await SupabaseClient.Rpc(
            "submit_player_high_score",
            new
            {
                p_mode = GetHighScoreColumn(mode),
                p_score = score
            });
    }

    private static string GetStatColumn(PlayerStat stat)
    {
        switch (stat)
        {
            case PlayerStat.MinesPlaced: return "mines_placed";
            case PlayerStat.DebrisBroken: return "debris_broken";
            case PlayerStat.FreeplayWins: return "freeplay_wins";
            case PlayerStat.ClassicWins: return "classic_wins";
            case PlayerStat.CustomLevelWins: return "custom_level_wins";
            default: throw new ArgumentOutOfRangeException(nameof(stat));
        }
    }

    private static string GetHighScoreColumn(PlayerHighScore mode)
    {
        switch (mode)
        {
            case PlayerHighScore.DailyChallenge: return "daily_challenge_high_score";
            case PlayerHighScore.Freeplay: return "freeplay_high_score";
            case PlayerHighScore.Classic: return "classic_high_score";
            default: throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    private static int ClampToInt(long value)
    {
        return value > int.MaxValue ? int.MaxValue : (int)value;
    }

    private string GetCurrentUserId()
    {
        string userId = SupabaseClient?.Auth?.CurrentUser?.Id;
        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException(
                "The player must be signed in before using community levels.");

        return userId;
    }

    private static void RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A level ID is required.", parameterName);
    }

    private sealed class UnitySupabaseSessionHandler :
        IGotrueSessionPersistence<Session>
    {
        private const string SessionPrefsKey =
            "Blast.Supabase.Session";

        public void SaveSession(Session session)
        {
            if (session == null)
            {
                return;
            }

            try
            {
                PlayerPrefs.SetString(
                    SessionPrefsKey,
                    JsonConvert.SerializeObject(session));
                PlayerPrefs.Save();
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Could not save Supabase session: " +
                    exception.Message);
            }
        }

        public Session LoadSession()
        {
            if (!PlayerPrefs.HasKey(SessionPrefsKey))
            {
                return null;
            }

            try
            {
                string sessionJson =
                    PlayerPrefs.GetString(SessionPrefsKey);

                if (string.IsNullOrWhiteSpace(sessionJson))
                {
                    return null;
                }

                return JsonConvert.DeserializeObject<Session>(
                    sessionJson);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Could not restore Supabase session: " +
                    exception.Message);
                DestroySession();
                return null;
            }
        }

        public void DestroySession()
        {
            if (!PlayerPrefs.HasKey(SessionPrefsKey))
            {
                return;
            }

            PlayerPrefs.DeleteKey(SessionPrefsKey);
            PlayerPrefs.Save();
        }
    }
}
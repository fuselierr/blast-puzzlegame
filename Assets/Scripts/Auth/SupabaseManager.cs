using UnityEngine;
using Supabase;
using System.Threading.Tasks;

public class SupabaseManager : MonoBehaviour
{
    public static SupabaseManager Instance { get; private set; }
    public Client SupabaseClient { get; private set; }

    private TaskCompletionSource<bool> initTcs = new TaskCompletionSource<bool>();

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
                AutoConnectRealtime = true
            };

            SupabaseClient = new Client(supabaseUrl, supabaseKey, options);
            await SupabaseClient.InitializeAsync();
            initTcs.TrySetResult(true);
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
}
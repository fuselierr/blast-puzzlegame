using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private const string MasterVolumeKey = "MasterVolume";
    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SfxVolume";
    private const string MusicEnabledKey = "MusicEnabled";

    private string SavePath => Path.Combine(Application.persistentDataPath, "playerdata.json");

    public PlayerData CurrentData { get; private set; }

    public static SaveManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        Instance = FindObjectOfType<SaveManager>();
        if (Instance != null)
            return Instance;

        GameObject saveObject = new GameObject("SaveManager");
        Instance = saveObject.AddComponent<SaveManager>();
        return Instance;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CurrentData = LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ---------- JSON FILE (structured data) ----------

    public void SaveData()
    {
        CurrentData.lastSaveTime = System.DateTime.Now.ToString();
        string json = JsonUtility.ToJson(CurrentData, true);

        try
        {
            File.WriteAllText(SavePath, json);
            Debug.Log($"Saved to: {SavePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Save failed: {e.Message}");
        }
    }

    public PlayerData LoadData()
    {
        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                return JsonUtility.FromJson<PlayerData>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Load failed: {e.Message}");
                return new PlayerData();
            }
        }
        return new PlayerData(); // no save exists yet — return defaults
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
        CurrentData = new PlayerData();
    }

    // ---------- PLAYERPREFS (simple settings) ----------

    [System.Obsolete("Use SaveMasterVolume instead.")]
    public void SaveVolume(float volume) => SaveMasterVolume(volume);

    [System.Obsolete("Use LoadMasterVolume instead.")]
    public float LoadVolume() => LoadMasterVolume();

    public void SaveMasterVolume(float volume)
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, Mathf.Clamp01(volume));
        PlayerPrefs.Save();
    }

    public float LoadMasterVolume()
    {
        return PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
    }

    public void SaveMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(volume));
        PlayerPrefs.Save();
    }

    public float LoadMusicVolume()
    {
        return PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
    }

    public void SaveSfxVolume(float volume)
    {
        PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(volume));
        PlayerPrefs.Save();
    }

    public float LoadSfxVolume()
    {
        return PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
    }

    public void SaveMusicEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool LoadMusicEnabled()
    {
        return PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
    }

    public void SaveDailyChallengeScore(int score)
    {
        if (CurrentData != null)
        {
            CurrentData.dailyChallengeScore = score;
            SaveData();
        }
    }

    public int LoadDailyChallengeScore()
    {
        if (CurrentData != null)
        {
            int score = CurrentData.dailyChallengeScore;
            Debug.Log($"Loaded daily challenge score: {score}");
        }

        return CurrentData != null ? CurrentData.dailyChallengeScore : 0;
    }

    // ---------- AUTO-SAVE ON APP PAUSE/QUIT ----------

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) // app is going to background — critical on Android
            SaveData();
    }

    void OnApplicationQuit()
    {
        SaveData();
    }
}
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private string SavePath => Path.Combine(Application.persistentDataPath, "playerdata.json");

    public PlayerData CurrentData { get; private set; }

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

    public void SaveVolume(float volume)
    {
        PlayerPrefs.SetFloat("Volume", volume);
        PlayerPrefs.Save();
    }

    public float LoadVolume()
    {
        return PlayerPrefs.GetFloat("Volume", 1f); // default = 1
    }

    public void SaveMusicEnabled(bool enabled)
    {
        PlayerPrefs.SetInt("MusicEnabled", enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool LoadMusicEnabled()
    {
        return PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
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
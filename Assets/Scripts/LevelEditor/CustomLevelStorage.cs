using System.IO;
using UnityEngine;

public static class CustomLevelStorage
{
    public static void Save(CustomLevelData level)
    {
        string safeName = string.IsNullOrWhiteSpace(level.levelName)
            ? "custom_level"
            : level.levelName.Replace(" ", "_");

        string path = Path.Combine(Application.persistentDataPath, safeName + ".json");
        string json = JsonUtility.ToJson(level, true);

        File.WriteAllText(path, json);
        Debug.Log("Saved custom level to: " + path);
    }

    public static CustomLevelData Load(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (!File.Exists(path))
            return null;

        return JsonUtility.FromJson<CustomLevelData>(File.ReadAllText(path));
    }
}
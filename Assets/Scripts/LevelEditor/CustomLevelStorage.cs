using System;
using System.IO;
using UnityEngine;

public static class CustomLevelStorage
{
    private static CustomLevelData pendingEditLevel;

    public static void BeginEdit(CustomLevelData level)
    {
        pendingEditLevel = level;
    }

    public static CustomLevelData TakePendingEdit()
    {
        CustomLevelData level = pendingEditLevel;
        pendingEditLevel = null;
        return level;
    }

    public static void Save(CustomLevelData level)
    {
        if (level == null)
        {
            return;
        }

        string fileName = level.fileName;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            string safeName = string.IsNullOrWhiteSpace(level.levelName)
                ? "custom_level"
                : level.levelName.Replace(" ", "_");

            string uniqueId = Guid.NewGuid().ToString("N");
            fileName = safeName + "_" + uniqueId + ".json";
            level.fileName = fileName;
        }

        string path = Path.Combine(Application.persistentDataPath, fileName);
        string json = JsonUtility.ToJson(level, true);

        File.WriteAllText(path, json);
        Debug.Log("Saved custom level to: " + path);
    }

    public static CustomLevelData Load(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return null;
        }

        string path = Path.Combine(Application.persistentDataPath, identifier);
        if (File.Exists(path))
        {
            return LoadFromPath(path);
        }

        string searchPattern = identifier.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? identifier
            : identifier + ".json";

        string[] files = Directory.GetFiles(Application.persistentDataPath, "*.json");
        foreach (string filePath in files)
        {
            string fileName = Path.GetFileName(filePath);
            if (!string.Equals(fileName, searchPattern, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return LoadFromPath(filePath);
        }

        foreach (string filePath in files)
        {
            CustomLevelData level = LoadFromPath(filePath);
            if (level == null)
            {
                continue;
            }

            if (string.Equals(level.levelName, identifier, StringComparison.OrdinalIgnoreCase)
                || string.Equals(level.fileName, identifier, StringComparison.OrdinalIgnoreCase))
            {
                return level;
            }
        }

        return null;
    }

    private static CustomLevelData LoadFromPath(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            CustomLevelData level =
                JsonUtility.FromJson<CustomLevelData>(File.ReadAllText(path));

            if (level != null)
            {
                level.fileName = Path.GetFileName(path);
            }

            return level;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
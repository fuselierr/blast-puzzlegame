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

    // Begin creating a fresh custom level with the provided display name.
    // This will generate a safe filename (ensuring no collisions) and store
    // the newly-created level as the pending edit so the LevelEditor can
    // pick it up via TakePendingEdit(). Returns the created level.
    public static CustomLevelData BeginCreateNew(string levelName)
    {
        if (string.IsNullOrWhiteSpace(levelName))
        {
            levelName = "custom_level";
        }

        string safeName = levelName.Trim().Replace(" ", "_");
        if (!safeName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            safeName += ".json";
        }

        string path = Path.Combine(Application.persistentDataPath, safeName);
        int suffix = 1;
        string baseName = Path.GetFileNameWithoutExtension(safeName);
        string ext = Path.GetExtension(safeName);

        while (File.Exists(path))
        {
            string candidate = $"{baseName}_{suffix}{ext}";
            path = Path.Combine(Application.persistentDataPath, candidate);
            suffix++;
        }

        string finalFileName = Path.GetFileName(path);

        CustomLevelData level = new CustomLevelData();
        level.levelName = levelName;
        level.fileName = finalFileName;
        level.width = 8;
        level.height = 8;
        level.layout = string.Empty;

        Debug.Log($"BeginCreateNew: created pending level with name='{level.levelName}' file='{level.fileName}'");
        BeginEdit(level);
        return level;
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
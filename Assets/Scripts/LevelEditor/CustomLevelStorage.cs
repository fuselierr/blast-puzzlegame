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
        level.author = string.Empty;

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

    public static CustomLevelData Duplicate(CustomLevelData source)
    {
        if (source == null)
        {
            return null;
        }

        // JSON round-tripping gives the duplicate its own data object instead
        // of sharing the entry's in-memory instance.
        CustomLevelData copy = JsonUtility.FromJson<CustomLevelData>(
            JsonUtility.ToJson(source));

        if (copy == null)
        {
            return null;
        }

        copy.levelName = GetUniqueCopyName(source.levelName);
        copy.fileName = GetUniqueFileName(copy.levelName);

        // Keep the preview, but give the duplicate its own image file so
        // deleting either level cannot break the other one's thumbnail.
        copy.previewImage = CopyPreview(source.previewImage, copy.fileName);

        // A duplicate is a new local level with no community engagement yet.
        copy.likes = 0;
        copy.downloads = 0;

        Save(copy);
        return copy;
    }

    public static bool Delete(CustomLevelData level)
    {
        if (level == null || string.IsNullOrWhiteSpace(level.fileName))
        {
            return false;
        }

        try
        {
            string levelPath = Path.Combine(
                Application.persistentDataPath,
                Path.GetFileName(level.fileName));

            if (!File.Exists(levelPath))
            {
                return false;
            }

            File.Delete(levelPath);

            if (!string.IsNullOrWhiteSpace(level.previewImage))
            {
                string previewPath = Path.Combine(
                    Application.persistentDataPath,
                    Path.GetFileName(level.previewImage));

                if (File.Exists(previewPath))
                {
                    File.Delete(previewPath);
                }
            }

            Debug.Log("Deleted custom level: " + levelPath);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("Could not delete custom level: " + exception.Message);
            return false;
        }
    }

    private static string GetUniqueCopyName(string sourceName)
    {
        string baseName = string.IsNullOrWhiteSpace(sourceName)
            ? "Untitled Level Copy"
            : sourceName.Trim() + " Copy";

        string candidate = baseName;
        int suffix = 2;

        while (LevelNameExists(candidate))
        {
            candidate = $"{baseName} {suffix}";
            suffix++;
        }

        return candidate;
    }

    private static bool LevelNameExists(string levelName)
    {
        foreach (string path in Directory.GetFiles(
                     Application.persistentDataPath,
                     "*.json"))
        {
            CustomLevelData existing = LoadFromPath(path);
            if (existing != null && string.Equals(
                    existing.levelName,
                    levelName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetUniqueFileName(string levelName)
    {
        string safeName = string.IsNullOrWhiteSpace(levelName)
            ? "custom_level"
            : levelName.Trim().Replace(" ", "_");

        foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
        {
            safeName = safeName.Replace(invalidCharacter, '_');
        }

        string candidate = safeName + ".json";
        int suffix = 2;

        while (File.Exists(Path.Combine(
                   Application.persistentDataPath,
                   candidate)))
        {
            candidate = $"{safeName}_{suffix}.json";
            suffix++;
        }

        return candidate;
    }

    private static string CopyPreview(
        string sourcePreviewFileName,
        string duplicateLevelFileName)
    {
        if (string.IsNullOrWhiteSpace(sourcePreviewFileName))
        {
            return string.Empty;
        }

        string sourcePath = Path.Combine(
            Application.persistentDataPath,
            Path.GetFileName(sourcePreviewFileName));

        if (!File.Exists(sourcePath))
        {
            return string.Empty;
        }

        string extension = Path.GetExtension(sourcePreviewFileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".png";
        }

        string duplicatePreviewFileName =
            Path.GetFileNameWithoutExtension(duplicateLevelFileName) +
            "_preview" + extension;

        string destinationPath = Path.Combine(
            Application.persistentDataPath,
            duplicatePreviewFileName);

        File.Copy(sourcePath, destinationPath, true);
        return duplicatePreviewFileName;
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
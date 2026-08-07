using System;

[Serializable]
public class PlayerData
{
    public int currentSceneIndex = 0;
    public int dailyChallengeScore = 0;
    public int freeplayHighScore = 0;
    public int classicHighScore = 0;

    public string lastSaveTime = string.Empty;
}

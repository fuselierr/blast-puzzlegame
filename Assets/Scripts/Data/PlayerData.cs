using System;
using System.Collections.Generic;

[Serializable]
public class PlayerData
{
    public int currentSceneIndex = 0;
    public int dailyChallengeScore = 0;
    public int freeplayHighScore = 0;
    public int classicHighScore = 0;

    public List<int> completedCampaignLevels = new List<int>();
    public string lastSaveTime = string.Empty;
}
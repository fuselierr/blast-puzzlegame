using System;
using System.Collections.Generic;

[Serializable]
public class CampaignLevelResult
{
    public int levelIndex;
    public float bestTimeSeconds;

    public CampaignLevelResult(int levelIndex, float bestTimeSeconds)
    {
        this.levelIndex = levelIndex;
        this.bestTimeSeconds = bestTimeSeconds;
    }
}

[Serializable]
public class PlayerData
{
    public int currentSceneIndex = 0;
    public int dailyChallengeScore = 0;
    public int freeplayHighScore = 0;
    public int classicHighScore = 0;

    public List<int> completedCampaignLevels = new List<int>();
    public List<int> completedCampaignTutorials = new List<int>();
    public List<CampaignLevelResult> campaignLevelResults = new List<CampaignLevelResult>();
    public string lastSaveTime = string.Empty;
}


[Serializable]
public class PlayerStats
{
    public string userName;
    public int minesPlaced;
    public int debrisBroken;
    public int freeplayWins;
    public int classicWins;
    public int customLevelWins;
    public int dailyChallengeHighScore;
    public int freeplayHighScore;
    public int classicHighScore;
    public int customLevelLikes;
    public int customLevelPlays;
}

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

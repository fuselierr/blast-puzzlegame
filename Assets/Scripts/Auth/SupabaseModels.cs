using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

[Table("profiles")]
public class Profile : BaseModel
{
    [PrimaryKey("id", true)]
    public string Id { get; set; }

    [Column("username")]
    public string Username { get; set; }

    [Column("points")]
    public int Points { get; set; }
}

[Table("progress")]
public class Progress : BaseModel
{
    [PrimaryKey("id", true)]
    public string Id { get; set; }

    [Column("level")]
    public int Level { get; set; }
}

[Table("classic_level_progress")]
public class ClassicLevelProgressRow: BaseModel
{
    [PrimaryKey("id", false)]
    public long id {get; set;}

    [Column("user_id")]
    public string UserId {get; set;}

    [Column("level_number")]
    public int LevelNumber {get; set;}

    [Column("completed_at")]
    public DateTime CompletedAt {get; set;}
}

[Table("daily_challenge_scores")]
public class DailyChallengeScoreRow: BaseModel
{
    [PrimaryKey("id", false)]
    public long Id {get; set;}

    [Column("user_id")]
    public string UserId {get; set;}

    [Column("challenge_date")]
    public string ChallengeDate {get; set;}

    [Column("score")]
    public long? Score {get; set;}

    [Column("attempted_at")]
    public DateTime AttemptedAt {get; set;}

    [Column("completed_at")]
    public DateTime? CompletedAt {get; set;}
}

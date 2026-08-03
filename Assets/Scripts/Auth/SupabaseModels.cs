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

[Table("custom_level_data")]
public class CustomLevelDataRow : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; }

    [Column("author_id")]
    public string AuthorId { get; set; }

    [Column("level_name")]
    public string LevelName { get; set; }

    [Column("width")]
    public int Width { get; set; }

    [Column("height")]
    public int Height { get; set; }

    [Column("layout")]
    public string Layout { get; set; }

    [Column("verified")]
    public bool Verified { get; set; }

    [Column("published")]
    public bool Published { get; set; }

    [Column("like_count")]
    public int LikeCount { get; set; }

    [Column("download_count")]
    public int DownloadCount { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public CustomLevelData ToCustomLevelData()
    {
        return new CustomLevelData
        {
            levelName = LevelName,
            fileName = Id,
            width = Width,
            height = Height,
            layout = Layout,
            author = AuthorId,
            verified = Verified,
            likes = LikeCount,
            downloads = DownloadCount,
            previewImage = null,
            createdAt = CreatedAt.ToString("O")
        };
    }
}

[Table("custom_level_data_likes")]
public class CustomLevelDataLikeRow : BaseModel
{
    [PrimaryKey("level_id", false)]
    public string LevelId { get; set; }

    [Column("user_id")]
    public string UserId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

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
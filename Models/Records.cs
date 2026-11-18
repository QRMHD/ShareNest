using SQLite;

namespace ShareNest.Models;

[Table("History")]
public class HistoryEntry
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Text { get; set; } = "";
    public DateTime CreatedOn { get; set; } = DateTime.Now;
}

[Table("Users")]
public class AppUser
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string FullName { get; set; } = "";
    [Unique] public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedOn { get; set; } = DateTime.Now;
}

[Table("Settings")]
public class Setting
{
    [PrimaryKey] public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

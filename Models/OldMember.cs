namespace JKTankDataMigration.Models;

public class OldMember
{
    public long Id { get; set; }
    public string DisplayName { get; set; } = default!;
    public bool? AvatarStatus { get; set; }
    public string Email { get; set; } = default!;
    public long RegDate { get; set; }
    public string? RegIp { get; set; }
    public long? GroupId { get; set; }
    public string? Privacy { get; set; }
}
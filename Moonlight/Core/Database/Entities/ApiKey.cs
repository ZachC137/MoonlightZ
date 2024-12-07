namespace Moonlight.Core.Database.Entities;

public class ApiKey
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Description { get; set; } = "";
    public System.DateTime CreatedAt { get; set; } = System.DateTime.UtcNow;
    public System.DateTime ExpiresAt { get; set; } = System.DateTime.UtcNow.AddDays(14);
    public string PermissionJson { get; set; } = "[]";
}

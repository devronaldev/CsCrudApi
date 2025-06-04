using System.ComponentModel.DataAnnotations.Schema;

namespace CsCrudApi.Models.UserRelated;

[Table("RefreshTokens")]
public class RefreshToken
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int UserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);
    public bool IsRevoked { get; set; }
    public string? ReplacedByTokenId { get; set; }
}
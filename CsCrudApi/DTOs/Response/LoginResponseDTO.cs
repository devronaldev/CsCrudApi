namespace CsCrudApi.DTOs;

public record LoginResponseDTO
{
    public string AccessToken { get; set; }
    public DateTime AccessExpiresAt { get; init; } = DateTime.Now;
    public DateTime RefreshExpiresAt { get; init; } = DateTime.Now;
    public int UserId { get; set; }
    public string TokenType { get; set; } = "Bearer";
}
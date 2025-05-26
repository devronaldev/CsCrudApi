namespace CsCrudApi.DTOs;

public record SearchedUserInfo
{
    public int UserId { get; set; }
    public string Name { get; set; }
    public string ProfilePicture { get; set; }
}
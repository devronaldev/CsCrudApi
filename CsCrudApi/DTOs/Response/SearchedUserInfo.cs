using CsCrudApi.Models.UserRelated;

namespace CsCrudApi.DTOs;

public record SearchedUserInfo
{
    public int UserId { get; set; }
    public string Name { get; set; }
    public string ProfilePicture { get; set; }

    public SearchedUserInfo(User user)
    {
        UserId = user.UserId;
        Name = user.NmSocial;
        ProfilePicture = user.ProfilePictureUrl;
    }
}
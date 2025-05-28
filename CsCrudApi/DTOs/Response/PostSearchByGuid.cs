using CsCrudApi.Models.PostRelated;
using CsCrudApi.Models.UserRelated;

namespace CsCrudApi.DTOs;

public record PostSearchByGuidDTO
{
    public int UserId { get; set; }
    public string Name { get; set; }
    public EGrauEscolaridade GrauEscolaridade { get; set; }
    public string ProfilePicture { get; set; }
    public string Instituicao { get; set; }
    public PostResponseDTO Post { get; set; }

    public PostSearchByGuidDTO(CampusResponseDTO c, User u, PostResponseDTO p)
    {
        UserId = u.UserId;
        Name = u.NmSocial;
        GrauEscolaridade = u.GrauEscolaridade;
        ProfilePicture = u.ProfilePictureUrl;
        Instituicao = $"{c.SgCampus} - {c.Name}";
        Post = p;
    }
};
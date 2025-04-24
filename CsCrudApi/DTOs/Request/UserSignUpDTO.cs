using CsCrudApi.Models.UserRelated;

namespace CsCrudApi.DTOs;

public record UserSignUpDTO
{ 
    public int CampusId { get; init; }
    public string Name { get; init; }
    public string Email { get; init; }
    public string Password { get; init; }
    public DateTime Birthday { get; init; }
    public ETipoInteresse Interest { get; init; }
    public EGrauEscolaridade Schoolarity { get; init; }
    public string SocialName { get; init; }
    public int CityId { get; init; }
    public EUserStudyStatus CourseStatus { get; init; }
    public int CourseId { get; init; }

    public static explicit operator User(UserSignUpDTO dto)
    {
        return new User
        {
            UserId = 0,
            Name = dto.Name,
            Email = dto.Email,
            Password = dto.Password,
            CdCampus = dto.CampusId,
            DtNasc = dto.Birthday,
            TipoInteresse = dto.Interest,
            GrauEscolaridade = dto.Schoolarity,
            NmSocial = dto.SocialName,
            TpColor = EColor.White,
            CdCidade = dto.CityId,
            IsEmailVerified = false,
            StatusCourse = dto.CourseStatus,
            CursoId = dto.CourseId,
            ProfilePictureUrl = string.Empty
        };
    }
};
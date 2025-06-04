using CsCrudApi.Models.UserRelated;
using CsCrudApi.Models.UserRelated.CollegeRelated;

namespace CsCrudApi.DTOs;

/// <summary>
/// Representa os dados de perfil de um usuário retornados pela API.
/// </summary>
/// <remarks>
/// Este DTO agrega informações essenciais do perfil de um usuário,
/// incluindo dados pessoais, de localização e de interação social (seguidores/seguindo).
/// </remarks>
public record UserProfileDTO
{
    /// <summary>
    /// O ID único do usuário.
    /// </summary>
    public int IdUsuario { get; init; }

    /// <summary>
    /// O nome social ou de exibição do usuário. Pode ser nulo.
    /// </summary>
    public string? SocialName { get; init; }

    /// <summary>
    /// O endereço de e-mail do usuário.
    /// </summary>
    public string Email { get; init; }

    /// <summary>
    /// A data de nascimento do usuário.
    /// </summary>
    public DateTime Birthday { get; init; }

    /// <summary>
    /// URL da imagem de perfil do usuário. Pode ser uma string vazia se não houver imagem.
    /// </summary>
    public string? ProfilePictureUrl { get; init; } = string.Empty;

    /// <summary>
    /// O tipo de preferência ou interesse principal do usuário.
    /// </summary>
    public ETipoInteresse TpPreferencia { get; init; }

    /// <summary>
    /// O ID do curso ao qual o usuário está associado.
    /// </summary>
    public int IdCurso { get; init; }

    /// <summary>
    /// O grau de escolaridade do usuário.
    /// </summary>
    public EGrauEscolaridade GrauEscolaridade { get; init; }

    /// <summary>
    /// O ID do campus/instituição de ensino ao qual o usuário está associado.
    /// </summary>
    public int IdCampus { get; init; }

    /// <summary>
    /// O nome completo do campus/instituição de ensino (ex: "SG - Nome do Campus").
    /// </summary>
    public string NomeCampus { get; init; }

    /// <summary>
    /// O número de usuários que seguem este perfil.
    /// </summary>
    public int Followers { get; init; }

    /// <summary>
    /// O número de usuários que este perfil está seguindo.
    /// </summary>
    public int Following { get; init; }

    /// <summary>
    /// O nome da cidade associada ao usuário.
    /// </summary>
    public string Cidade { get; init; }

    /// <summary>
    /// Inicializa uma nova instância do <see cref="UserProfileDTO"/>.
    /// </summary>
    /// <param name="user">O objeto de entidade <see cref="User"/> contendo os dados básicos do usuário.</param>
    /// <param name="cidade">O objeto de entidade <see cref="Cidade"/> contendo os dados da cidade do usuário.</param>
    /// <param name="campus">O objeto de entidade <see cref="Campus"/> contendo os dados do campus do usuário.</param>
    /// <param name="followers">A contagem de seguidores do usuário.</param>
    /// <param name="following">A contagem de usuários que o usuário está seguindo.</param>
    public UserProfileDTO(User user, Cidade cidade, Campus campus, int followers, int following)
    {
        IdUsuario = user.UserId;
        SocialName = user.NmSocial;
        Email = user.Email;
        Birthday = user.DtNasc;
        ProfilePictureUrl = user.ProfilePictureUrl;
        TpPreferencia = user.TipoInteresse;
        IdCurso = user.CursoId;
        GrauEscolaridade = user.GrauEscolaridade;
        // Assume-se que IdCampus e NomeCampus vêm do objeto Campus para consistência.
        // Se user.CdCampus e user.CdCidade forem os IDs diretos, então:
        IdCampus = campus.Id; // Usando o ID do objeto campus
        NomeCampus = $"{campus.SgCampus} - {campus.CampusName}";
        
        Followers = followers;
        Following = following;
        Cidade = cidade.Name;
    }
};
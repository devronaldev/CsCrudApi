using CsCrudApi.Models.PostRelated;

namespace CsCrudApi.DTOs;

/// <summary>
/// Representa um comentário completo retornado pela API, incluindo informações do usuário.
/// </summary>
public record CommentDetailsDTO
{
    /// <summary>
    /// Informações do usuário que criou o comentário.
    /// </summary>
    public SearchedUserInfo User { get; set; }

    /// <summary>
    /// Detalhes do comentário.
    /// </summary>
    public Commentary Comment { get; set; } // Pode ser um DTO mais simplificado se não quiser retornar todos os campos da entidade
}
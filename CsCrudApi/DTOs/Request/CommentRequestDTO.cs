using System.ComponentModel.DataAnnotations;

namespace CsCrudApi.DTOs;

/// <summary>
/// Representa um comentário a ser criado ou atualizado.
/// </summary>
public class CommentRequestDTO
{
    /// <summary>
    /// O GUID do post ao qual o comentário pertence.
    /// </summary>
    /// <example>a1b2c3d4-e5f6-7890-1234-567890abcdef</example>
    [Required(ErrorMessage = "O GUID do post é obrigatório.")]
    public string PostGUID { get; set; }

    /// <summary>
    /// O texto do comentário.
    /// </summary>
    /// <example>Este é um ótimo post!</example>
    [Required(ErrorMessage = "O texto do comentário é obrigatório.")]
    [MinLength(3, ErrorMessage = "O comentário precisa ter texto igual ou superior a 3 letras.")]
    public string Text { get; set; }

    // Adicionar Id se for usado para PUT/DELETE, ou criar outro DTO específico
    /// <summary>
    /// O ID do comentário. Necessário apenas para operações de atualização e exclusão.
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; } // Adicionado para uso em PUT/DELETE, se Commentary já não tiver
}
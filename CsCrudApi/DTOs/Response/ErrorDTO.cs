namespace CsCrudApi.DTOs;
/// <summary>
/// Objeto de Transferência de Dados (DTO) padrão para mensagens de erro da API.
/// </summary>
public record ErrorDTO
{
    /// <summary>
    /// Código de erro único para identificação programática.
    /// </summary>
    /// <example>BR400NULL</example>
    public string ErrorCode { get; set; }

    /// <summary>
    /// Mensagem resumida do erro, para o usuário.
    /// </summary>
    /// <example>O e-mail não pode estar vazio.</example>
    public string Message { get; set; }

    /// <summary>
    /// Descrição detalhada do erro, para o desenvolvedor ou suporte.
    /// </summary>
    /// <example>Verifique se o e-mail foi enviado na requisição.</example>
    public string ErrorDescription { get; set; }
}
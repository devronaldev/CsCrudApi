namespace CsCrudApi.DTOs;
/// <summary>
/// Representa um erro ocorrido durante o processamento da requisição.
/// </summary>
public record ErrorDTO
{
    /// <summary>
    /// Mensagem amigável explicando o erro.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Código interno do erro, útil para tratamento no front-end ou logs.
    /// </summary>
    public string ErrorCode { get; init; }

    /// <summary>
    /// Descrição mais técnica e detalhada do erro (opcional).
    /// </summary>
    public string? ErrorDescription { get; init; }
}
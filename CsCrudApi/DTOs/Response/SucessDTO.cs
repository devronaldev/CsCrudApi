namespace CsCrudApi.DTOs
{
    /// <summary>
    /// Representa uma resposta de sucesso padronizada para as requisições da API.
    /// </summary>
    public record SuccessDTO
    {
        /// <summary>
        /// Nome que representa semanticamente o tipo da mensagem.
        /// Exemplo: para um token, o valor seria "Token - Authorization".
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// Mensagem de sucesso da operação. Sempre presente.
        /// </summary>
        public string Message { get; init; }

        /// <summary>
        /// Data e hora do evento ou resposta. Pode ser nula.
        /// </summary>
        public DateTime? Date { get; init; }
        
        public SuccessDTO(string message, string? name = null, DateTime? date = null)
        {
            Message = message;
            Name = name;
            Date = date ?? DateTime.UtcNow;
        }
    }
}
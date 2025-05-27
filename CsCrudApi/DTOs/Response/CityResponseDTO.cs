using CsCrudApi.Models.UserRelated;

namespace CsCrudApi.DTOs
{
    /// <summary>
    /// Representa os dados de uma cidade para exposição via API.
    /// </summary>
    public record CityResponseDTO
    {
        /// <summary>
        /// O ID único da cidade.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// O nome da cidade.
        /// </summary>
        /// <example>Santos</example>
        public string Nome { get; set; }

        /// <summary>
        /// O estado (UF) ao qual a cidade pertence.
        /// </summary>
        public EEstados Estado { get; set; }

        /// <summary>
        /// Inicializa uma nova instância do <see cref="CityResponseDTO"/> a partir de uma entidade Cidade.
        /// </summary>
        /// <param name="cidade">A entidade Cidade a ser mapeada.</param>
        public CityResponseDTO(Cidade cidade)
        {
            Id = cidade.IdCidade;
            Nome = cidade.Name;
            Estado = cidade.Estado;
        }
    }
}


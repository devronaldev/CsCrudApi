using CsCrudApi.Models.UserRelated.CollegeRelated;

namespace CsCrudApi.DTOs;

/// <summary>
/// Representa os dados de um campus para exposição via API.
/// </summary>
public record CampusResponseDTO
{
    /// <summary>
    /// O ID único do campus.
    /// </summary>
    /// <example>1</example>
    public int IdCampus { get; set; }

    /// <summary>
    /// A sigla do campus.
    /// </summary>
    /// <example>IFSP-CBT</example>
    public string SgCampus { get; set; }

    /// <summary>
    /// O nome completo do campus.
    /// </summary>
    /// <example>Instituto Federal de Ciências e Tecnologia - campus Cubatão</example>
    public string Name { get; set; }

    /// <summary>
    /// O ID da cidade à qual o campus pertence.
    /// </summary>
    /// <example>25108</example>
    public int IdCidade { get; set; }

    /// <summary>
    /// Inicializa uma nova instância do <see cref="CampusResponseDTO"/> a partir de uma entidade Campus.
    /// </summary>
    /// <param name="campus">A entidade Campus a ser mapeada.</param>
    public CampusResponseDTO(Campus campus)
    {
        IdCampus = campus.Id;
        SgCampus = campus.SgCampus;
        Name = campus.CampusName;
        IdCidade = campus.CdCidade;
    }
}
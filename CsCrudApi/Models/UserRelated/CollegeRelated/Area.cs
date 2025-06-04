using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CsCrudApi.Models.UserRelated.CollegeRelated
{
    /// <summary>
    /// Representa uma área de estudo dentro do sistema acadêmico.
    /// </summary>
    /// <remarks>
    /// Esta entidade é utilizada para categorizar cursos e programas de estudo em grandes domínios do conhecimento,
    /// como Saúde e Estética, Ciências Humanas, Engenharia, etc.
    /// </remarks>
    [Table("area")]
    public class Area
    {
        /// <summary>
        /// O ID único da área de estudo. É a chave primária na tabela 'area'.
        /// </summary>
        /// <example>1</example>
        [Key]
        [Required]
        [Column("id_area")]
        public int AreaId { get; set; }

        /// <summary>
        /// O nome descritivo da área de estudo.
        /// </summary>
        /// <example>Saúde e Estética</example>
        [Required]
        [Column("desc_area")]
        public string AreaName { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CsCrudApi.Models.UserRelated.CollegeRelated
{
    [Table("curso")]
    public class Curso
    {
        [Key]
        [Column("id_curso")]
        public int IdCourse { get; set; }

        [Required]
        [Column("cd_area")]
        public int CdArea { get; set; }

        [Required]
        [Column("nome_curso")]
        [MaxLength(128)]
        public string NmCourse { get; set; }
    }
}

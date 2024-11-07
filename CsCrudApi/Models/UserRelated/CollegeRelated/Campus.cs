using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CsCrudApi.Models.UserRelated.CollegeRelated
{
    [Table("campus")]
    public class Campus
    {
        [Key]
        [Required]
        [Column("id_campus")]
        public int Id { get; set; }

        [Required]
        [Column("sg_campus")]
        public string SgCampus { get; set; }

        [Required]
        [Column("desc_campus")]
        public string CampusName { get; set; }

        [Required]
        [Column("end_campus")]
        public string EndCampus { get; set; }

        [Required]
        [Column("email_campus")]
        public string EmailCampus { get; set; }

        [Required]
        [Column("cd_cidade")]
        public int CdCidade { get; set; }
    }
}

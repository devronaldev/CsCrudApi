using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CsCrudApi.Models
{
    [Table("campus")]
    public class Campus
    {
        [Key] 
        [Required]
        [Column("id_campus")]
        public int Id { get; set; }

        [Column("sg_campus")]
        public string SgCampus {  get; set; }

        [Column("desc_campus")]
        public string CampusName { get; set; }

        [Column("end_campus")]
        public string EndCampus { get; set; }

        [Column("email_campus")]
        public string EmailCampus { get; set; }

        [Column("cd_cidade")]
        public int CdCidade { get; set; }
    }
}

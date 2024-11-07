using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CsCrudApi.Models.UserRelated.CollegeRelated
{
    [Table("area")]
    public class Area
    {
        [Key]
        [Required]
        [Column("id_area")]
        public int AreaId { get; set; }

        [Required]
        [Column("desc_area")]
        public string AreaName { get; set; }
    }
}

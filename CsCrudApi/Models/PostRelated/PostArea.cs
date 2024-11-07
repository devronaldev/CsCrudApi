using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CsCrudApi.Models.PostRelated
{
    [Table("area_post")]
    public class PostArea
    {
        [Required]
        [Column("id_area")]
        public int AreaId { get; set; }

        [Required]
        [StringLength(32)]
        [Column("guid_post")]
        public string GuidPost { get; set; }
    }
}

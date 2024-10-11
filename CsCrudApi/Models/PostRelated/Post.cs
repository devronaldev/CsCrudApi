using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CsCrudApi.Models.PostRelated
{
    [Table("Post")]
    public class Post
    {
        [Key]
        [Required]
        [StringLength(32)]
        [Column("guid")]
        public string Guid { get; set; }

        [Column("data_post")]
        [Required]
        public DateTime PostDate {  get; set; }

        [Column("desc_post")]
        [Required]
        [MaxLength(16000000)]
        public string TextPost { get; set; }

        [Column("qt_like")]
        [Required]
        public int QuantityLikes { get; set; }

        [Column("tipo_post")]
        [Required]
        public int Type { get; set; }
    }
}

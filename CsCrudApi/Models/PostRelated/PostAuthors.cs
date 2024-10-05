using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace CsCrudApi.Models.PostRelated
{
    [Table("post_autores")]
    public class PostAuthors
    {
        [Required]
        [Key]
        [Column("cd_usuario")]
        public int CdUser { get; set; }

        [Required]
        [Key]
        [StringLength(32)]
        [Column("guid_post")]
        public string GuidPost { get; set; }
    }
}

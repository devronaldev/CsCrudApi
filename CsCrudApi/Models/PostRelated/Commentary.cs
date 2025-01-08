using CsCrudApi.Models.UserRelated;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CsCrudApi.Models.PostRelated
{
    [Table("comentario")]
    public class Commentary
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("cd_user")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        [Column("cd_post")]
        [MaxLength(32)]
        public string PostGUID { get; set; }

        [ForeignKey("PostGUID")]
        public virtual Post Post { get; set; } 

        [Column("cd_comentario_pai")]
        public int? ParentCommentaryId { get; set; } 

        [ForeignKey("ParentCommentaryId")]
        public virtual Commentary ParentCommentary { get; set; }

        [Required]
        [Column("texto")]
        [StringLength(255, MinimumLength = 3)]
        public string Text { get; set; }

        [Required]
        [Column("criado_em")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("atualizado_em")]
        public DateTime LastUpdatedAt { get; set; }

        public virtual ICollection<Commentary> Replies { get; set; }
    }

}

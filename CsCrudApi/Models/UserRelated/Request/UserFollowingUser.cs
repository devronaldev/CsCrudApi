using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CsCrudApi.Models.UserRelated.Request
{
    [Table("usuario_seguindo_usuario")]
    public class UserFollowingUser
    {
        [Key]
        [Required]
        [Column("cd_usuario_seguidor")]
        public int CdFollower { get; set; }

        [Key]
        [Required]
        [Column("cd_usuario_seguido")]
        public int CdFollowed { get; set; }

        [Required]
        [Column("seguido_em")]
        public DateTime CreatedAt { get; set; }

        [Required]
        [Column("ultima_atualizacao")]
        public DateTime LastUpdatedAt { get; set; }

        [Required]
        [Column("status")]
        public bool Status { get; set; }
    }
}

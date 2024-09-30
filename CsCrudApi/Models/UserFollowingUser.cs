using System.ComponentModel.DataAnnotations.Schema;

namespace CsCrudApi.Models
{
    [Table("usuario_seguindo_usuario")]
    public class UserFollowingUser
    {
        [Column("cd_usuario_seguidor")]
        public int CdFollower {  get; set; }

        [Column("cd_usuario_seguido")]
        public int CdFollowed { get; set; }
    }
}

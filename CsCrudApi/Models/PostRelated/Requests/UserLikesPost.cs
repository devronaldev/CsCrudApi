using System.ComponentModel.DataAnnotations.Schema;

namespace CsCrudApi.Models.PostRelated.Requests
{
    [Table("usuario_curte_post")]
    public class UserLikesPost
    {
        [Column("id_usuario")]
        public int UserId { get; set; }

        [Column("guid_post")]
        public string PostGuid { get; set; }

        [Column("criado_em")]
        public DateTime CreatedAt { get; set; }

        [Column("ultima_mudanca")]
        public DateTime UpdatedAt { get; set; }

        [Column("esta_ativo")]
        public bool IsActive { get; set; }
    }
}

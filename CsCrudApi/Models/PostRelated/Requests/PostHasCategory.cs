using System.ComponentModel.DataAnnotations.Schema;

namespace CsCrudApi.Models.PostRelated.Requests
{
    [Table("post_tem_categoria")]
    public class PostHasCategory
    {
        [Column("guid_post")]
        public string PostGUID { get; set; }

        [Column("id_categoria")]
        public int CategoryID { get; set; }
    }
}

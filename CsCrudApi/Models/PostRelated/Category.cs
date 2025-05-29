using System.ComponentModel.DataAnnotations.Schema;

namespace CsCrudApi.Models.PostRelated
{
    [Table("categorias")]
    public class Category
    {
        public int Id { get; set; }

        [Column("nome_categoria")]
        public string Name { get; set; }

        [Column("descricao")]
        public string Description { get; set; }

        public int Quantity { get ; set; }

        public Category(string hashtag)
        {
            Name = string.Join(" ", hashtag.Split('-'));
            Description = hashtag;
            Quantity = 0;
        }

        public Category()
        {
            
        }
    }
}
